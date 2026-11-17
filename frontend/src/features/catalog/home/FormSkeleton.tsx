import { useLocale } from '../../../app/localeContext'
import { Skeleton } from '../../../shared/ui'

/** Form iskeleti formun kutusunda durur; veri gelince düzen kaymaz (AC-49). */
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
      <p className="narrowing-form__title">
        <Skeleton shape="line" />
      </p>
      <p>
        <Skeleton shape="line" />
      </p>
    </div>
  )
}
