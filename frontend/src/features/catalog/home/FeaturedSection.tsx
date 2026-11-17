import { useLocale } from '../../../app/localeContext'
import type { ShowcaseListing } from '../../../shared/api/types'
import { ErrorState } from '../../../shared/ui'
import { ListingCard } from '../ListingCard'
import { FeaturedSkeleton } from './FeaturedSkeleton'

export type FeaturedLoad =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly items: readonly ShowcaseListing[] }

/**
 * 0014 R3 · D3, D4 — öne çıkan araçlar. Sıra ve üst sınır sunucudadır; burada yalnız
 * gösterilir. Öne çıkarılmış ilan yoksa bölüm başlığıyla birlikte HİÇ basılmaz: bu bir
 * boş durum değil, bölümün yokluğudur (AC-21).
 */
export function FeaturedSection({
  load,
  onRetry,
}: {
  readonly load: FeaturedLoad
  readonly onRetry: () => void
}) {
  const { t } = useLocale()

  if (load.kind === 'loading') {
    return <FeaturedSkeleton />
  }

  if (load.kind === 'error') {
    return (
      <section className="home-section">
        <ErrorState
          testId="featured-error"
          title={t('home.featured.error.title')}
          description={t('home.featured.error.body')}
          retryLabel={t('home.featured.error.retry')}
          onRetry={onRetry}
        />
      </section>
    )
  }

  if (load.items.length === 0) {
    return null
  }

  return (
    <section
      className="home-section"
      data-testid="featured-section"
      aria-labelledby="featured-title"
    >
      <h2 className="home-section__title" id="featured-title">
        {t('home.featured.title')}
      </h2>
      <ul className="listing-grid">
        {load.items.map((listing, index) => (
          // AC-27 — yalnız İLK kartın kapağı katlama üstünde sayılır; diğerleri lazy.
          <ListingCard key={listing.listingId} listing={listing} aboveTheFold={index === 0} />
        ))}
      </ul>
    </section>
  )
}
