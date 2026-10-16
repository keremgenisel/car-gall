import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { login } from '../../shared/api/identity'
import { Button, Card, Field } from '../../shared/ui'
import {
  EMPTY_LOGIN_FORM,
  afterFailedLogin,
  messageKeyFor,
  shouldShowLockHint,
  type LoginFormValues,
} from './loginFormState'
import { LoginError } from './states/LoginError'
import { LoginLocked } from './states/LoginLocked'
import { TwoFactorPhase } from './TwoFactorPhase'

interface Props {
  /**
   * 0013 D43 — oturum GÖVDESİ artık burada okunmuyor: başarı tam gezinme yapıyor ve
   * kabuk durumu `GET /api/admin/me` ile sunucudan öğreniyor. Kurulum gereken dalda
   * gövde kimlik bile taşımıyor (C13), yani taşınacak bir oturum nesnesi yok.
   */
  readonly onSignedIn: () => void
}

type Phase =
  | { readonly kind: 'editing' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * Oturum öncesi akış **tek ekrandır** (R14/D14): ikinci adım bu ekranın bir fazıdır ve
 * adım geçilmeden panele gezinme olmaz.
 *
 * Başarıda TAM GEZİNME yapılır (`0006` AC-54): panelin dili `<html lang dir>` ile
 * sunucudan basılıyor ve o ancak oturum varken personelin tercihine göre çözülebilir.
 */
export function LoginPage({ onSignedIn }: Props) {
  const { locale, t } = useLocale()
  const [values, setValues] = useState<LoginFormValues>(EMPTY_LOGIN_FORM)
  const [phase, setPhase] = useState<Phase>({ kind: 'editing' })
  const [twoFactor, setTwoFactor] = useState(false)

  /**
   * D21 — sayaç AKIŞA aittir. Sunucu kilitlenmeyi bilerek söylemiyor (`0006` AC-58), bu
   * yüzden ipucunu uç kendi sayacıyla basıyor; iki faz **aynı** sayacı besliyor.
   */
  const [failedAttempts, setFailedAttempts] = useState(0)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setPhase({ kind: 'submitting' })

    try {
      const outcome = await login(values, locale)

      if (outcome === null) {
        return
      }

      // D15 — bekleyen dal bir HATA DEĞİL, bir adımdır: ekran fazını değiştiriyor.
      if (outcome.status === 'two_factor_required') {
        setPhase({ kind: 'editing' })
        setValues(afterFailedLogin(values))
        setTwoFactor(true)
        return
      }

      // `completed` ve `two_factor_setup_required` dallarında oturum çerezi YAZILDI;
      // kurulum borcunu kabuk kapının `403`'ünden öğreniyor (D13/D36).
      onSignedIn()
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      // AC-57 — kimlik kalır, parola temizlenir.
      setValues(afterFailedLogin(values))
      setFailedAttempts((count) => count + 1)
      setPhase({ kind: 'failed', messageKey: messageKeyFor(errorCode, 'login.error', locale) })
    }
  }

  return (
    <div className="admin-page admin-page--narrow">
      <Card as="div" testId="login-form">
        {twoFactor ? (
          <TwoFactorPhase
            onVerified={onSignedIn}
            onExpired={(messageKey) => {
              // D20 — ara durum düştü: personel PAROLA fazına döner ve sebebini orada
              // okur. "Taze kod yaz" ile "parolanı yeniden gir" ayrı şeylerdir.
              setTwoFactor(false)
              setPhase({ kind: 'failed', messageKey })
            }}
            onSwitchUser={() => {
              setTwoFactor(false)
              setValues(EMPTY_LOGIN_FORM)
              setPhase({ kind: 'editing' })
            }}
            failedAttempts={failedAttempts}
            onFailedAttempt={() => setFailedAttempts((count) => count + 1)}
          />
        ) : (
          <>
            <h1 className="admin-form__title">{t('login.title')}</h1>

            {phase.kind === 'failed' ? <LoginError messageKey={phase.messageKey} /> : null}
            {shouldShowLockHint(failedAttempts) ? <LoginLocked /> : null}

            <form className="admin-form" onSubmit={onSubmit} noValidate>
              <Field
                id="login-id"
                label={t('login.field.loginId')}
                type="email"
                autoComplete="username"
                value={values.loginId}
                onChange={(loginId) => setValues({ ...values, loginId })}
              />

              <Field
                id="login-password"
                label={t('login.field.password')}
                type="password"
                autoComplete="current-password"
                value={values.password}
                onChange={(password) => setValues({ ...values, password })}
              />

              <Button type="submit" loading={phase.kind === 'submitting'}>
                {phase.kind === 'submitting' ? t('login.submitting') : t('login.submit')}
              </Button>
            </form>
          </>
        )}
      </Card>
    </div>
  )
}
