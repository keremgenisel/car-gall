import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { confirmTwoFactorEnrollment, startTwoFactorEnrollment } from '../../shared/api/identity'
import type { RecoveryCodes, TwoFactorEnrollmentStart } from '../../shared/api/types'
import { Button, Field } from '../../shared/ui'
import { QrCode } from './QrCode'
import { RecoveryCodeList } from './RecoveryCodeList'
import { twoFactorInputError, twoFactorMessageKey } from './twoFactorState'

type Phase =
  | { readonly kind: 'idle' }
  | { readonly kind: 'working' }
  | { readonly kind: 'started'; readonly enrollment: TwoFactorEnrollmentStart }
  | { readonly kind: 'confirming'; readonly enrollment: TwoFactorEnrollmentStart }
  | { readonly kind: 'done'; readonly codes: RecoveryCodes }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * R3 — kurulum: sistem bir sır üretir, personel onu doğrulama uygulamasına ekler ve **bir
 * kod girerek kurulumu doğrular**. Doğrulanmamış kurulum etkinleşmez.
 *
 * Sır personele **iki biçimde birlikte** sunulur (D17): taranabilir kare kod **ve** her
 * zaman görünür duran elle giriş metni. Elle girişin gizlenmiş bir bağlantının arkasına
 * konmaması hem erişilebilirlik hem RTL borcu, hem de kamerayı aynı cihazda kullanan
 * personelin tek yolu.
 */
export function TwoFactorSetupPanel({ onEnrolled }: { readonly onEnrolled: () => void }) {
  const { locale, t } = useLocale()
  const [phase, setPhase] = useState<Phase>({ kind: 'idle' })
  const [code, setCode] = useState('')
  const [fieldError, setFieldError] = useState<string | null>(null)

  async function start() {
    setPhase({ kind: 'working' })

    try {
      const enrollment = await startTwoFactorEnrollment(locale)

      setPhase(enrollment === null ? { kind: 'idle' } : { kind: 'started', enrollment })
    } catch (cause) {
      setPhase({ kind: 'failed', messageKey: hataAnahtari(cause) })
    }
  }

  async function confirm(enrollment: TwoFactorEnrollmentStart) {
    const error = twoFactorInputError('code', code)

    if (error !== null) {
      setFieldError(error)
      return
    }

    setFieldError(null)
    setPhase({ kind: 'confirming', enrollment })

    try {
      const codes = await confirmTwoFactorEnrollment(code.trim(), locale)

      // D18 — onay başarısız olursa kurulum ETKİNLEŞMEZ; bekleyen kayıt süresi dolana
      // kadar durur ve yeniden başlatmak YENİ sır üretir.
      setPhase(codes === null ? { kind: 'started', enrollment } : { kind: 'done', codes })
      setCode('')
    } catch (cause) {
      setPhase({ kind: 'failed', messageKey: hataAnahtari(cause) })
    }
  }

  function hataAnahtari(cause: unknown): string {
    const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

    return twoFactorMessageKey(errorCode, 'twoFactor.error', locale)
  }

  if (phase.kind === 'done') {
    return (
      <>
        <RecoveryCodeList codes={phase.codes.codes} />

        <Button variant="secondary" onClick={onEnrolled} data-testid="two-factor-enrolled-continue">
          {t('twoFactor.setup.continue')}
        </Button>
      </>
    )
  }

  const enrollment =
    phase.kind === 'started' || phase.kind === 'confirming' ? phase.enrollment : null

  return (
    <>
      <p className="admin-form__intro">{t('twoFactor.setup.intro')}</p>

      {phase.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="two-factor-error">
          <p className="admin-form__error-title">{t('twoFactor.error.title')}</p>
          <p className="admin-form__error-body">{t(phase.messageKey)}</p>
        </div>
      ) : null}

      {enrollment === null ? (
        <Button
          loading={phase.kind === 'working'}
          onClick={() => void start()}
          data-testid="two-factor-start"
        >
          {phase.kind === 'working' ? t('twoFactor.setup.starting') : t('twoFactor.setup.start')}
        </Button>
      ) : (
        <>
          <QrCode value={enrollment.secretUri} label={t('twoFactor.setup.scan')} />

          <p className="admin-form__intro">{t('twoFactor.setup.scan')}</p>

          <p className="two-factor__manual">
            <span className="two-factor__manual-label">{t('twoFactor.setup.manual')}</span>{' '}
            <code data-testid="two-factor-manual-secret">{enrollment.secret}</code>
          </p>

          <Field
            id="two-factor-confirm-code"
            label={t('twoFactor.setup.field.code')}
            autoComplete="one-time-code"
            hint={t('twoFactor.setup.hint')}
            errorKey={fieldError ?? undefined}
            value={code}
            onChange={setCode}
          />

          <Button
            loading={phase.kind === 'confirming'}
            onClick={() => void confirm(enrollment)}
            data-testid="two-factor-confirm"
          >
            {phase.kind === 'confirming'
              ? t('twoFactor.setup.confirming')
              : t('twoFactor.setup.confirm')}
          </Button>
        </>
      )}
    </>
  )
}
