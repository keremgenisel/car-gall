import { useEffect, useState } from 'react'
import { useLocale } from '../localeContext'
import { ApiError } from '../../shared/api/client'
import { fetchCurrentSession, logout } from '../../shared/api/identity'
import type { CurrentSession } from '../../shared/api/types'
import { adminPath, type AdminView } from '../../shared/lib/route'
import { VehicleEditPage } from '../../features/catalog/admin/VehicleEditPage'
import { VehicleFormPage } from '../../features/catalog/admin/VehicleFormPage'
import { VehicleListPage } from '../../features/catalog/admin/VehicleListPage'
import { VehicleMediaPage } from '../../features/catalog/admin/VehicleMediaPage'
import { ForcePasswordChangePage } from '../../features/identity/ForcePasswordChangePage'
import { LoginPage } from '../../features/identity/LoginPage'
import { StaffListPage } from '../../features/identity/StaffListPage'
import { TwoFactorPage } from '../../features/identity/TwoFactorPage'
import { EmptyState } from '../../shared/ui'

type Gate =
  | { readonly kind: 'checking' }
  | { readonly kind: 'signedOut' }
  | { readonly kind: 'mustChangePassword' }
  /**
   * 0013 D13/D36 — kurulumu olmayan zorunlu rol. Kabuk bunu `/me`'nin `403`'ünden
   * öğreniyor: kapının beyaz listesi D13'ün üç yolu olarak kaldı ve `/me` orada yok.
   * Desen `mustChangePassword`'ün aynısı.
   */
  | { readonly kind: 'mustEnrollTwoFactor' }
  | { readonly kind: 'signedIn'; readonly session: CurrentSession }

/**
 * Panelin korumalı kabuğu (0006 R7).
 *
 * Oturum durumu SUNUCUDAN öğrenilir; uç oturum çerezini okuyamaz (C17, AC-29) ve
 * "giriş yapmış mıyım" sorusunun cevabı yalnız bir istekle alınabilir.
 *
 * `401` giriş ekranına götürür; `403` GÖTÜRMEZ (C19, AC-27) — yetkisi olmayan personel
 * aksi hâlde giriş–yönlendirme döngüsüne girerdi. Tek istisna
 * `identity.password_change_required`: o bir yetki reddi değil, zorunlu bir adımdır
 * (R10) ve kendi ekranına götürür.
 */
export function AdminApp({
  view = 'staff',
  vehicleId,
}: {
  readonly view?: AdminView
  readonly vehicleId?: string
}) {
  const { locale, t } = useLocale()
  const [gate, setGate] = useState<Gate>({ kind: 'checking' })

  useEffect(() => {
    const controller = new AbortController()

    fetchCurrentSession(locale, controller.signal)
      .then((session) =>
        setGate(
          session.mustChangePassword
            ? { kind: 'mustChangePassword' }
            : { kind: 'signedIn', session },
        ),
      )
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        if (cause instanceof ApiError && cause.errorCode === 'identity.password_change_required') {
          setGate({ kind: 'mustChangePassword' })
          return
        }

        if (cause instanceof ApiError && cause.errorCode === 'identity.two_factor_setup_required') {
          setGate({ kind: 'mustEnrollTwoFactor' })
          return
        }

        setGate({ kind: 'signedOut' })
      })

    return () => controller.abort()
  }, [locale])

  if (gate.kind === 'checking') {
    return (
      <div className="admin-page admin-page--narrow" data-testid="admin-loading">
        <p>{t('admin.loading')}</p>
      </div>
    )
  }

  if (gate.kind === 'signedOut') {
    return (
      <LoginPage
        onSignedIn={() => {
          // 0013 D14 — ikinci adım giriş ekranının bir FAZI; buraya ancak adım geçildikten
          // (ya da kurulumsuz hesapta kapılı oturum verildikten) sonra geliniyor.
          // TAM GEZİNME, istemci tarafı geçiş değil: panelin dili `<html lang dir>` ile
          // SUNUCUDAN basılır (R16, AC-54, AC-55) ve o ancak oturum varken personelin
          // tercihine göre çözülebilir. İstemci tarafında geçilseydi panel, giriş
          // ekranının dilinde — yani kurulum varsayılanında — kalırdı ve uçta yeniden
          // hesaplamak `docs/frontend.md` § 6'yı ihlal ederdi.
          window.location.assign(adminPath())
        }}
      />
    )
  }

  if (gate.kind === 'mustChangePassword') {
    return <ForcePasswordChangePage />
  }

  // D6/D13 — SIRA: parola kapısı yukarıda, kurulum kapısı burada. İkisi birden bekleyen
  // personel önce parolasını değiştirir; sunucudaki iki ara katman da bu sırada çalışıyor.
  if (gate.kind === 'mustEnrollTwoFactor') {
    return <TwoFactorPage />
  }

  return (
    <div className="admin-shell" data-testid="admin-shell">
      <header className="admin-shell__bar">
        <span className="admin-shell__brand">{t('admin.title')}</span>
        <span className="admin-shell__user" dir="auto">
          {gate.session.displayName}
        </span>
        <button
          type="button"
          className="ui-button ui-button--text"
          onClick={async () => {
            await logout(locale)

            // Çıkışta da tam gezinme: oturum gittiği için kabuk artık kurulum
            // varsayılan diliyle basılmalı.
            window.location.assign(adminPath())
          }}
        >
          {t('admin.logout')}
        </button>
      </header>

      {/*
        0010a — panelin ikinci yüzeyi geldi ve gezinme artık gerekiyor: araç kaydı
        ekranından listeye dönecek bir yol olmadan iki ekran birbirine bakmaz.

        Personel yönetimi bağlantısı YALNIZ Yöneticiye basılır (0008/AC-4): yetkisi
        olmayana basılan bir bağlantı, sunucunun `403`'üne yürüyen bir davet olurdu.
        Araç yüzeyi her role açık — R1 "oturum açmış personel" diyor, rol ayırmıyor.
      */}
      <nav className="admin-shell__nav" aria-label={t('admin.nav.label')}>
        {gate.session.role === 'manager' ? (
          <a
            className="admin-shell__nav-link"
            href={adminPath('staff')}
            aria-current={view === 'staff' ? 'page' : undefined}
          >
            {t('admin.nav.staff')}
          </a>
        ) : null}
        {/* 0013 D44 — her role basılıyor: ikinci adım opsiyonel rolde de kurulabilir. */}
        <a
          className="admin-shell__nav-link"
          href={adminPath('two-factor')}
          aria-current={view === 'two-factor' ? 'page' : undefined}
        >
          {t('admin.nav.twoFactor')}
        </a>
        <a
          className="admin-shell__nav-link"
          href={adminPath('vehicles')}
          aria-current={
            view === 'vehicles' ||
            view === 'vehicle-new' ||
            view === 'vehicle-edit' ||
            view === 'vehicle-media'
              ? 'page'
              : undefined
          }
        >
          {t('admin.nav.vehicles')}
        </a>
      </nav>

      <main className="admin-shell__content">
        {view === 'vehicles' ? <VehicleListPage /> : null}
        {view === 'vehicle-new' ? <VehicleFormPage /> : null}
        {view === 'vehicle-edit' && vehicleId !== undefined ? (
          <VehicleEditPage vehicleId={vehicleId} />
        ) : null}
        {/* 0012a D17 — medya yüzeyi düzenlemenin alt yolu; kendi ekranı, kendi kaydetme akışı. */}
        {view === 'vehicle-media' && vehicleId !== undefined ? (
          <VehicleMediaPage vehicleId={vehicleId} />
        ) : null}
        {view === 'password' ? <ForcePasswordChangePage /> : null}
        {view === 'two-factor' ? <TwoFactorPage /> : null}

        {/*
          R8 — erişilebilen işlemler ROLE göre belirlenir. Personel yönetimi yalnız
          Yöneticinindir (R9); başka rollerin bu ekranda yapabileceği bir şey yok.

          Ekranı yine de basıp sunucunun `403`'üne güvenmek YANLIŞ olurdu: liste ucu
          reddedince ekran "getirilemedi, tekrar dene" derdi ve bu yalan olurdu — istek
          başarısız olmadı, izin yok. `docs/frontend.md` § 3 boş durumun NEDENİNİ ve
          kullanıcının ne yapabileceğini söylemeyi zorunlu kılıyor.

          Bu bir güvenlik sınırı DEĞİL, dürüstlük meselesidir: sınır sunucuda ve
          AC-25/AC-26 onu orada sınıyor.
        */}
        {view === 'staff' && gate.session.role === 'manager' ? (
          <StaffListPage session={gate.session} />
        ) : null}

        {view === 'staff' && gate.session.role !== 'manager' ? (
          <div className="admin-page">
            <EmptyState
              title={t('admin.noAccess.title')}
              description={t('admin.noAccess.body')}
              testId="admin-no-access"
            />
          </div>
        ) : null}
      </main>
    </div>
  )
}
