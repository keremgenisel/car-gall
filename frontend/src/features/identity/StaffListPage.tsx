import { useEffect, useRef, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchStaff } from '../../shared/api/identity'
import type { CurrentSession, StaffListItem } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { adminPath } from '../../shared/lib/route'
import { Badge } from '../../shared/ui'
import { AddStaffForm } from './AddStaffForm'
import { StaffRowActions, type ActionOutcome } from './StaffRowActions'
import { statusBadgeKey } from './twoFactorState'
import { StaffListEmpty, StaffListError, StaffListSkeleton } from './states/StaffListStates'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly staff: readonly StaffListItem[] }

export function StaffListPage({ session }: { readonly session: CurrentSession }) {
  const { locale, t } = useLocale()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [reloadToken, setReloadToken] = useState(0)

  /** Son eylemin mesajı. Tazeleme onu SİLMEZ (R8/AC-25): veri bayattı, mesaj değil. */
  const [actionMessage, setActionMessage] = useState<string | null>(null)

  /**
   * AC-23 — tazelemeden sonra odak işlem yapılan SATIRA döner.
   *
   * Satır kimliği tutuluyor, düğme referansı değil: tazeleme satırları yeniden basıyor ve
   * eski referans o anda DOM'dan düşmüş oluyor. Kimlikten yeniden bulmak, hangi düğmenin
   * hangi satıra ait olduğunu bilmekle aynı şey.
   */
  const focusTarget = useRef<string | null>(null)

  /**
   * Son BAŞARILI liste, ait olduğu locale ile birlikte.
   *
   * İskelete düşmek yalnız İLK yüklemede doğru. Bir eylemden sonraki tazelemede tabloyu
   * kaldırmak sayfayı kısaltıyor, tarayıcı kaydırmayı sıfırlıyor ve ziyaretçi yerini
   * kaybediyor — AC-23'ün "kaydırma konumu korunur" yarısı tam olarak böyle düşüyordu.
   *
   * Locale ile BİRLİKTE tutuluyor: dil değiştiğinde elimizdeki liste artık o dile ait
   * değil ve iskelet doğru olan. Ayrı bir efektle temizlemek yerine anahtarlamak, bir
   * render turu daha kazandırıyor.
   */
  const [retained, setRetained] = useState<{
    readonly locale: AppLocale
    readonly staff: readonly StaffListItem[]
  } | null>(null)

  const requestKey = `${locale}|${reloadToken}`
  const fresh: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  const retainedStaff = retained !== null && retained.locale === locale ? retained.staff : null

  const load: LoadState =
    fresh.kind === 'loading' && retainedStaff !== null
      ? { kind: 'ready', staff: retainedStaff }
      : fresh

  useEffect(() => {
    const controller = new AbortController()

    fetchStaff(locale, controller.signal)
      .then((staff) => {
        setRetained({ locale, staff })
        setResult({ key: requestKey, value: { kind: 'ready', staff } })
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setResult({ key: requestKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [locale, reloadToken, requestKey])

  // Liste basıldıktan SONRA odak iade edilir; önce denenirse hedef henüz DOM'da değildir.
  //
  // Bağımlılık `load` DEĞİL `load.kind`: `load` her render'da türetilen yeni bir nesne ve
  // onu bağımlılık yapmak efekti her render'da koşturur. Odak iadesi yalnız durum
  // 'ready'ye döndüğünde gerekiyor.
  const loadKind = load.kind

  useEffect(() => {
    if (loadKind !== 'ready' || focusTarget.current === null) {
      return
    }

    // `preventScroll`: `focus()` varsayılan olarak öğeyi görünür alana ÇEKER ve
    // ziyaretçinin kaydırma konumunu ezer. AC-23 ikisini birden istiyor — odak satıra
    // dönsün AMA konum korunsun — ve konum ziyaretçinin seçimidir. Ölçüldü: bu bayrak
    // olmadan kaydırma 1242 → 0 düşüyordu.
    document
      .querySelector<HTMLElement>(`[data-testid="deactivate-${focusTarget.current}"]`)
      ?.focus({ preventScroll: true })
    focusTarget.current = null
  }, [loadKind, reloadToken])

  function onOutcome(staffId: string, outcome: ActionOutcome) {
    if (outcome.signedOut) {
      // AC-22 — oturum düştüyse gösterilecek bir ekran kalmadı. Tam gezinme, çünkü panelin
      // dili sunucudan basılıyor (0006 R16).
      window.location.assign(adminPath())
      return
    }

    setActionMessage(outcome.messageKey)
    focusTarget.current = staffId

    // R7 — KURAL GEREĞİ redde liste tazelenmez; ziyaretçinin konumu korunur.
    if (outcome.refresh) {
      setReloadToken((token) => token + 1)
    }
  }

  return (
    <div className="admin-page">
      <h1 className="admin-page__title">{t('staff.title')}</h1>

      {actionMessage === null ? null : (
        <div className="admin-form__error" role="alert" data-testid="staff-action-error">
          <p className="admin-form__error-body">{t(actionMessage)}</p>
        </div>
      )}

      {load.kind === 'loading' ? <StaffListSkeleton /> : null}
      {load.kind === 'error' ? (
        <StaffListError onRetry={() => setReloadToken((token) => token + 1)} />
      ) : null}

      {load.kind === 'ready' && load.staff.length === 0 ? <StaffListEmpty /> : null}

      {load.kind === 'ready' && load.staff.length > 0 ? (
        <table className="staff-table" data-testid="staff-table">
          <thead>
            <tr>
              <th scope="col">{t('staff.column.name')}</th>
              <th scope="col">{t('staff.column.loginId')}</th>
              <th scope="col">{t('staff.column.role')}</th>
              <th scope="col">{t('staff.column.status')}</th>
              {/* 0013 R17/D25 — mockup üç panelde "2FA açık" rozeti çiziyor. */}
              <th scope="col">{t('staff.column.twoFactor')}</th>
              <th scope="col">{t('staff.column.actions')}</th>
            </tr>
          </thead>
          <tbody>
            {load.staff.map((member) => {
              // AC-15 — kendi satırı KİMLİKTEN tanınır; ad tekil değil (D1).
              const isSelf = member.staffId === session.staffId

              return (
                <tr
                  key={member.staffId}
                  className={
                    isSelf ? 'staff-table__row staff-table__row--self' : 'staff-table__row'
                  }
                  data-testid={isSelf ? 'staff-row-self' : undefined}
                >
                  <td dir="auto">
                    {member.displayName}
                    {isSelf ? (
                      <span className="staff-table__self-badge">{t('staff.action.self')}</span>
                    ) : null}
                  </td>
                  <td dir="ltr">{member.loginId}</td>
                  {/* Rol ve durum KOD olarak gelir; görünen ad uçta çevrilir (0006 C7). */}
                  <td>{t(`staff.role.${member.role}`)}</td>
                  <td>{t(`staff.status.${member.status}`)}</td>

                  {/*
                    Rozet İKİ alandan kuruluyor: kurulum durumu satırda, zorunluluk ise
                    ROLDE (D25). Sözleşmeye üçüncü bir alan eklemek, zorunluluk politikasını
                    API'ye dondururdu.
                  */}
                  <td data-testid={`staff-two-factor-${member.staffId}`}>
                    <Badge>
                      {t(statusBadgeKey(member.twoFactorEnrolled, member.role === 'manager'))}
                    </Badge>
                  </td>

                  <td>
                    <StaffRowActions
                      member={member}
                      isSelf={isSelf}
                      onOutcome={(outcome) => onOutcome(member.staffId, outcome)}
                    />
                  </td>
                </tr>
              )
            })}
          </tbody>
        </table>
      ) : null}

      <AddStaffForm onAdded={() => setReloadToken((token) => token + 1)} />
    </div>
  )
}
