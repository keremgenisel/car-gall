import { useLocale } from '../../../app/localeContext'
import { homeText } from '../../../shared/lib/homeTexts'
import { Card, Skeleton } from '../../../shared/ui'

/** Kaç kart geleceği bilinmiyor (1–6); bir satırlık yer ayrılır. */
const PLACEHOLDER_COUNT = 3

/**
 * `ListSkeleton` ile aynı ilke: gerçek kartın kutu modeli taklit edilir (AC-49). `0022` D12 —
 * bölüm başlığı da gerçek metinle basılır; başlıksız iskelet, başlık gelince altını kaydırırdı.
 */
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
      <h2 className="home-section__title" aria-hidden="true">
        {homeText('featured-title', t('home.featured.title'))}
      </h2>
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
