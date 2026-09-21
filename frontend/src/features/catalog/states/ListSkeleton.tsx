import { useLocale } from '../../../app/localeContext'
import { Card, Skeleton } from '../../../shared/ui'

/** Sayfa başına 24 ilan (0001 R6); iskelet de o kadar kart tutar. */
const PAGE_SIZE = 24

/**
 * İskelet GERÇEK kartın kutu modelini taklit eder: aynı `Card` kutusu, aynı medya
 * en-boy oranı, aynı tipografi sınıfları. Yer tutucu satırları gerçek satırların
 * sınıflarının İÇİNDE durur, böylece yükseklikleri font boyutundan türer ve veri
 * gelince düzen kaymaz (docs/frontend.md § 3).
 */
export function ListSkeleton() {
  const { t } = useLocale()

  return (
    <div className="listing-grid" role="status" aria-live="polite" data-testid="list-skeleton">
      <span className="visually-hidden">{t('showcase.loading')}</span>
      {Array.from({ length: PAGE_SIZE }, (_, index) => (
        <Card as="div" key={index}>
          <div className="listing-card__media">
            <Skeleton shape="media" />
          </div>
          <div className="listing-card__body">
            <p className="listing-card__model">
              <Skeleton shape="line" />
            </p>
            <p className="listing-card__meta">
              <Skeleton shape="line" />
            </p>
            <p className="listing-card__price">
              <Skeleton shape="line" />
            </p>
            <p className="listing-card__branch">
              <Skeleton shape="line" />
            </p>
          </div>
        </Card>
      ))}
    </div>
  )
}
