import { useLocale } from '../../../app/localeContext'

interface Props {
  /** ÇEVRİLMİŞ metnin anahtarı; `errorCode`'un kendisi buraya hiç gelmez (AC-59). */
  readonly messageKey: string
}

/**
 * Hata formun ÜSTÜNDE durur; form kaybolmaz ve girilen kimlik yerinde kalır (AC-57).
 * `ErrorState` envanterde var ama bölgenin tamamının yerine geçiyor — burada form
 * ayakta kalmak zorunda.
 */
export function LoginError({ messageKey }: Props) {
  const { t } = useLocale()

  return (
    <div className="admin-form__error" role="alert" data-testid="login-error">
      <p className="admin-form__error-title">{t('login.error.title')}</p>
      <p className="admin-form__error-body">{t(messageKey)}</p>
    </div>
  )
}
