import { useEffect, useState } from 'react'
import { useLocale } from '../localeContext'
import { ApiError } from '../../shared/api/client'
import { fetchCurrentSession, logout } from '../../shared/api/identity'
import type { CurrentSession } from '../../shared/api/types'
import { ForcePasswordChangePage } from '../../features/identity/ForcePasswordChangePage'
import { LoginPage } from '../../features/identity/LoginPage'
import { StaffListPage } from '../../features/identity/StaffListPage'

type Gate =
  | { readonly kind: 'checking' }
  | { readonly kind: 'signedOut' }
  | { readonly kind: 'mustChangePassword' }
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
export function AdminApp() {
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
        onSignedIn={(session) =>
          setGate(
            session.mustChangePassword
              ? { kind: 'mustChangePassword' }
              : { kind: 'signedIn', session },
          )
        }
      />
    )
  }

  if (gate.kind === 'mustChangePassword') {
    return <ForcePasswordChangePage />
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
            setGate({ kind: 'signedOut' })
          }}
        >
          {t('admin.logout')}
        </button>
      </header>

      <main className="admin-shell__content">
        <StaffListPage />
      </main>
    </div>
  )
}
