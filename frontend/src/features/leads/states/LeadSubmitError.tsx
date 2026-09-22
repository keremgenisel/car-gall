import { useLocale } from '../../../app/localeContext'

interface Props {
  /** ÇEVRİLMİŞ metnin anahtarı; `errorCode`'un kendisi buraya hiç gelmez (AC-39). */
  readonly messageKey: string
  readonly onRetry: () => void
  /** İlan vitrinden düştüyse ziyaretçiye listeye dönme eylemi de sunulur (AC-15). */
  readonly homeHref?: string
}

/**
 * Form KAYBOLMAZ: hata formun üstünde durur, girilen bilgiler yerinde kalır (AC-15,
 * AC-38). `ErrorState` bileşeni envanterde var ama burada kullanılmıyor — o, bölgenin
 * TAMAMININ yerine geçen bir durumdur; burada form ayakta kalmak zorunda.
 */
export function LeadSubmitError({ messageKey, onRetry, homeHref }: Props) {
  const { t } = useLocale()

  return (
    <div className="lead-form__error" role="alert" data-testid="lead-error">
      <p className="lead-form__error-title">{t('lead.error.title')}</p>
      <p className="lead-form__error-body">{t(messageKey)}</p>
      <div className="lead-form__error-actions">
        <button type="button" className="ui-button ui-button--secondary" onClick={onRetry}>
          {t('lead.error.retry')}
        </button>
        {homeHref === undefined ? null : (
          <a className="ui-button ui-button--text" href={homeHref}>
            {t('lead.error.listing_unavailable.action')}
          </a>
        )}
      </div>
    </div>
  )
}
