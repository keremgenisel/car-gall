import { useLocale } from '../../../app/localeContext'

/**
 * Boş ekran veya ani sıçrama yok; iskelet tercih edilir (docs/frontend.md § 3).
 * Kartlarla aynı en-boy oranını tutar, böylece veri gelince düzen kaymaz (AC-25, CLS).
 */
export function ListSkeleton() {
  const { t } = useLocale()

  return (
    <div className="skeleton-grid" role="status" aria-live="polite" data-testid="list-skeleton">
      <span className="visually-hidden">{t('showcase.loading')}</span>
      {Array.from({ length: 8 }, (_, index) => (
        <div key={index} className="skeleton-card">
          <div className="skeleton-card__media" />
        </div>
      ))}
    </div>
  )
}
