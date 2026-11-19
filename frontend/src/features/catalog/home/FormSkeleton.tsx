import { useLocale } from '../../../app/localeContext'
import { homeText } from '../../../shared/lib/homeTexts'
import { Skeleton } from '../../../shared/ui'

/**
 * Form iskeleti formun kutusunda durur; veri gelince düzen kaymaz (AC-49). `0022` D12 —
 * başlık ilk boyamada hazır olduğu için iskelet GERÇEK başlığı basar: uzun, iki satıra
 * taşan bir başlık da iskeletle formda aynı yüksekliği tutar.
 */
export function FormSkeleton() {
  const { t } = useLocale()

  return (
    <div
      className="narrowing-form"
      role="status"
      aria-live="polite"
      data-testid="narrowing-form-skeleton"
    >
      <span className="visually-hidden">{t('home.form.loading')}</span>
      <h2 className="narrowing-form__title">{homeText('form-title', t('home.form.title'))}</h2>
      <p>
        <Skeleton shape="line" />
      </p>
    </div>
  )
}
