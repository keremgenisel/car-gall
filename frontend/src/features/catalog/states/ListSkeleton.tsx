import { useLocale } from '../../../app/localeContext'

/** Sayfa başına 24 ilan (R6); iskelet de o kadar kart tutar. */
const PAGE_SIZE = 24

/**
 * Boş ekran veya ani sıçrama yok; iskelet tercih edilir (docs/frontend.md § 3).
 *
 * İskelet kartı GERÇEK kartın kutu modelini birebir taklit eder: aynı medya en-boy oranı,
 * aynı gövde satır yükseklikleri, aynı sayıda kart. Ölçümde iskelet 320 px, gerçek kart
 * 392 px idi; takas anında altındaki her şey kayıyor ve CLS 0,069'a çıkıyordu (AC-25,
 * eşik 0,1). Yer tutucunun işi boşluğu doldurmak değil, DOĞRU boşluğu tutmaktır.
 */
export function ListSkeleton() {
  const { t } = useLocale()

  return (
    <div className="listing-grid" role="status" aria-live="polite" data-testid="list-skeleton">
      <span className="visually-hidden">{t('showcase.loading')}</span>
      {Array.from({ length: PAGE_SIZE }, (_, index) => (
        <div key={index} className="listing-card skeleton-card" aria-hidden="true">
          <div className="listing-card__media skeleton-card__media" />
          <div className="listing-card__body">
            <p className="listing-card__model skeleton-card__line" />
            <p className="listing-card__meta skeleton-card__line" />
            <p className="listing-card__price skeleton-card__line" />
            <p className="listing-card__branch skeleton-card__line" />
          </div>
        </div>
      ))}
    </div>
  )
}
