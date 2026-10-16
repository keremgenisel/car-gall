import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { disableTwoFactor, regenerateRecoveryCodes } from '../../shared/api/identity'
import type { RecoveryCodes, TwoFactorStatus } from '../../shared/api/types'
import { Badge, Button, Field } from '../../shared/ui'
import { RecoveryCodeList } from './RecoveryCodeList'
import {
  disableFormError,
  statusBadgeKey,
  twoFactorInputError,
  twoFactorMessageKey,
} from './twoFactorState'

interface Props {
  readonly status: TwoFactorStatus
  readonly onChanged: () => void
}

type Phase =
  | { readonly kind: 'idle' }
  | { readonly kind: 'working' }
  | { readonly kind: 'codes'; readonly codes: RecoveryCodes }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * Kurulumu olan personelin kendi ekranı: kalan kurtarma kodu sayısı (D24), kümeyi
 * **yenileme** ve — yalnız opsiyonel rolde — **kapatma** (R15/D23).
 *
 * Kapatma zorunlu rolde hiç basılmaz: oradaki tek yol Yöneticinin sıfırlamasıdır (R6).
 * Ekranı basıp sunucunun `403`'üne güvenmek yanlış olurdu — `docs/frontend.md` § 3 boş
 * ya da yapılamaz durumun NEDENİNİ söylemeyi zorunlu kılıyor.
 */
export function TwoFactorManagePanel({ status, onChanged }: Props) {
  const { locale, t } = useLocale()
  const [phase, setPhase] = useState<Phase>({ kind: 'idle' })
  const [code, setCode] = useState('')
  const [password, setPassword] = useState('')
  const [fieldError, setFieldError] = useState<string | null>(null)

  function hataAnahtari(cause: unknown): string {
    const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

    return twoFactorMessageKey(errorCode, 'twoFactor.error', locale)
  }

  async function regenerate() {
    const error = twoFactorInputError('code', code)

    if (error !== null) {
      setFieldError(error)
      return
    }

    setFieldError(null)
    setPhase({ kind: 'working' })

    try {
      const codes = await regenerateRecoveryCodes(code.trim(), locale)

      setCode('')
      setPhase(codes === null ? { kind: 'idle' } : { kind: 'codes', codes })
      onChanged()
    } catch (cause) {
      setPhase({ kind: 'failed', messageKey: hataAnahtari(cause) })
    }
  }

  async function disable() {
    const error = disableFormError(password, code)

    if (error !== null) {
      setFieldError(error)
      return
    }

    setFieldError(null)
    setPhase({ kind: 'working' })

    try {
      await disableTwoFactor(password, code.trim(), locale)
      setPassword('')
      setCode('')
      setPhase({ kind: 'idle' })
      onChanged()
    } catch (cause) {
      setPhase({ kind: 'failed', messageKey: hataAnahtari(cause) })
    }
  }

  return (
    <>
      <p className="two-factor__status" data-testid="two-factor-status">
        <Badge>{t(statusBadgeKey(status.enrolled, status.required))}</Badge>
      </p>

      {/* D24 — kalan sayı ICU çoğul biçimiyle; Arapça altı biçim tanımlıyor. */}
      <p className="admin-form__intro" data-testid="two-factor-remaining">
        {t('twoFactor.codes.remaining', { count: status.remainingRecoveryCodes })}
      </p>

      {phase.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="two-factor-error">
          <p className="admin-form__error-title">{t('twoFactor.error.title')}</p>
          <p className="admin-form__error-body">{t(phase.messageKey)}</p>
        </div>
      ) : null}

      {phase.kind === 'codes' ? <RecoveryCodeList codes={phase.codes.codes} /> : null}

      <Field
        id="two-factor-manage-code"
        label={t('twoFactor.manage.field.code')}
        autoComplete="one-time-code"
        hint={t('twoFactor.manage.hint')}
        errorKey={fieldError ?? undefined}
        value={code}
        onChange={setCode}
      />

      <Button
        loading={phase.kind === 'working'}
        onClick={() => void regenerate()}
        data-testid="two-factor-regenerate"
      >
        {t('twoFactor.codes.regenerate')}
      </Button>

      {status.required ? (
        <p className="admin-form__intro" data-testid="two-factor-disable-blocked">
          {t('twoFactor.disable.notAllowed')}
        </p>
      ) : (
        <section className="two-factor__disable">
          <h2 className="admin-form__subtitle">{t('twoFactor.disable.title')}</h2>
          <p className="admin-form__intro">{t('twoFactor.disable.body')}</p>

          <Field
            id="two-factor-disable-password"
            label={t('twoFactor.disable.field.password')}
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={setPassword}
          />

          <Button
            variant="secondary"
            loading={phase.kind === 'working'}
            onClick={() => void disable()}
            data-testid="two-factor-disable"
          >
            {t('twoFactor.disable.submit')}
          </Button>
        </section>
      )}
    </>
  )
}
