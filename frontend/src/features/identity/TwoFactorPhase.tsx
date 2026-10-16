import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { discardPendingLogin, verifyTwoFactor } from '../../shared/api/identity'
import { Button, Field } from '../../shared/ui'
import { LoginError } from './states/LoginError'
import { LoginLocked } from './states/LoginLocked'
import { shouldShowLockHint } from './loginFormState'
import {
  isPendingExpired,
  twoFactorInputError,
  twoFactorMessageKey,
  type TwoFactorInput,
} from './twoFactorState'

interface Props {
  /** Doğrulama geçildi; kabuk tam gezinme yapacak (D14). */
  readonly onVerified: () => void
  /** Ara durum düştü: ekran PAROLA fazına döner ve sebebi orada yazılır (D20). */
  readonly onExpired: (messageKey: string) => void
  /** *"Kullanıcı değiştir"* — ara durum silindi, baştan başlanıyor (D14). */
  readonly onSwitchUser: () => void
  /**
   * D21 — başarısız deneme sayısı **akışın** malıdır, ekranın değil. İki faz aynı sayacı
   * besler; ayrı tutulsaydı faz değiştiğinde sayaç sıfırlanır ve R8'in "tek sayaç"ı uçta
   * iki olurdu.
   */
  readonly failedAttempts: number
  readonly onFailedAttempt: () => void
}

type Phase =
  | { readonly kind: 'editing' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * Girişin ikinci fazı (R14/D14): ikinci adım ayrı bir ekran DEĞİL, giriş ekranının
 * fazıdır ve adım geçilmeden panele gezinme olmaz.
 *
 * **F5 adımı kaybettirir** ve personel parolayı yeniden girer; bu bedel D14'te açıkça
 * yazılı. Kurtarmanın tek yolu oturum öncesi köke bir okuma ucu daha açmaktı ve o,
 * elinde yalnız çerez olan birine "hangi personel bekliyor" sorusunu cevaplardı.
 */
export function TwoFactorPhase({
  onVerified,
  onExpired,
  onSwitchUser,
  failedAttempts,
  onFailedAttempt,
}: Props) {
  const { locale, t } = useLocale()
  const [input, setInput] = useState<TwoFactorInput>('code')
  const [value, setValue] = useState('')
  const [phase, setPhase] = useState<Phase>({ kind: 'editing' })
  const [fieldError, setFieldError] = useState<string | null>(null)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()

    const error = twoFactorInputError(input, value)

    // Biçimi bozuk değer için istek GÖNDERİLMEZ: paylaşılan kilitlenme sayacını bir
    // yazım hatası tüketmesin (R8).
    if (error !== null) {
      setFieldError(error)
      return
    }

    setFieldError(null)
    setPhase({ kind: 'submitting' })

    try {
      await verifyTwoFactor(value.trim(), locale)
      onVerified()
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'
      const messageKey = twoFactorMessageKey(errorCode, 'login.error', locale)

      setValue('')
      onFailedAttempt()

      if (isPendingExpired(errorCode)) {
        onExpired(messageKey)
        return
      }

      setPhase({ kind: 'failed', messageKey })
    }
  }

  async function switchUser() {
    await discardPendingLogin(locale)
    onSwitchUser()
  }

  return (
    <>
      <h1 className="admin-form__title">{t('login.twoFactor.title')}</h1>
      <p className="admin-form__intro">{t('login.twoFactor.intro')}</p>

      {phase.kind === 'failed' ? <LoginError messageKey={phase.messageKey} /> : null}
      {shouldShowLockHint(failedAttempts) ? <LoginLocked /> : null}

      <form className="admin-form" onSubmit={onSubmit} noValidate>
        <Field
          id="two-factor-code"
          label={
            input === 'code' ? t('login.twoFactor.field.code') : t('login.twoFactor.field.recovery')
          }
          autoComplete="one-time-code"
          hint={input === 'code' ? t('login.twoFactor.hint') : t('login.twoFactor.recoveryHint')}
          errorKey={fieldError ?? undefined}
          value={value}
          onChange={setValue}
        />

        <Button type="submit" loading={phase.kind === 'submitting'}>
          {phase.kind === 'submitting'
            ? t('login.twoFactor.submitting')
            : t('login.twoFactor.submit')}
        </Button>
      </form>

      <div className="admin-form__actions">
        <button
          type="button"
          className="ui-button ui-button--text"
          onClick={() => {
            setInput(input === 'code' ? 'recovery' : 'code')
            setValue('')
            setFieldError(null)
          }}
          data-testid="two-factor-toggle-input"
        >
          {input === 'code' ? t('login.twoFactor.useRecovery') : t('login.twoFactor.useCode')}
        </button>

        <button
          type="button"
          className="ui-button ui-button--text"
          onClick={() => void switchUser()}
          data-testid="two-factor-switch-user"
        >
          {t('login.twoFactor.switchUser')}
        </button>
      </div>
    </>
  )
}
