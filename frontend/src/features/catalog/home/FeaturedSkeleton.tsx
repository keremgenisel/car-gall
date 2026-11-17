import { useLocale } from '../../../app/localeContext'
import { Card, Skeleton } from '../../../shared/ui'

/** Kaç kart geleceği bilinmiyor (1–6); bir satırlık yer ayrılır. */
const PLACEHOLDER_COUNT = 3

/** `ListSkeleton` ile aynı ilke: gerçek kartın kutu modeli taklit edilir (AC-49). */
export function FeaturedSkeleton() {
  const { t } = useLocale()

  return (
    <section
      className="home-section"
      role="status"
      aria-live="polite"
      data-testid="featured-skeleton"
    >
      <span className="visually-hidden">{t('home.featured.loading')}</span>
      <div className="listing-grid">
        {Array.from({ length: PLACEHOLDER_COUNT }, (_, index) => (
          <Card as="div" key={index}>
            <div className="listing-card__media">
              <Skeleton shape="media" />
            </div>
            <div className="listing-card__body">
              <p className="listing-card__model">
                <Skeleton shape="line" />
              </p>
              <p className="listing-card__price">
                <Skeleton shape="line" />
              </p>
            </div>
          </Card>
        ))}
      </div>
    </section>
  )
}
