import { useLocale } from '../../../app/localeContext'
import { EmptyState, ErrorState, Skeleton } from '../../../shared/ui'

/**
 * AC-60 — liste yüklenirken iskelet, yüklenemediğinde **tekrar dene**.
 * İskelet gerçek içeriğin kutu modelini taklit eder: yanlış yükseklik veri gelince
 * düzeni kaydırır (docs/frontend.md § 3).
 */
export function StaffListSkeleton() {
  const { t } = useLocale()

  return (
    <div className="staff-table" data-testid="staff-skeleton" aria-label={t('staff.loading')}>
      {[0, 1, 2, 3].map((row) => (
        <div className="staff-table__row" key={row}>
          <Skeleton shape="line" />
        </div>
      ))}
    </div>
  )
}

export function StaffListEmpty() {
  const { t } = useLocale()

  return (
    <EmptyState
      title={t('staff.empty.title')}
      description={t('staff.empty.body')}
      testId="staff-empty"
    />
  )
}

export function StaffListError({ onRetry }: { readonly onRetry: () => void }) {
  const { t } = useLocale()

  return (
    <ErrorState
      title={t('staff.error.title')}
      description={t('staff.error.body')}
      retryLabel={t('staff.error.retry')}
      onRetry={onRetry}
      testId="staff-error"
    />
  )
}
