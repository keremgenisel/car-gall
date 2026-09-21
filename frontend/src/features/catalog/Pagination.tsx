import { useLocale } from '../../app/localeContext'
import { Button } from '../../shared/ui'

interface Props {
  readonly page: number
  readonly totalPages: number
  readonly onChange: (page: number) => void
}

export function Pagination({ page, totalPages, onChange }: Props) {
  const { t } = useLocale()

  if (totalPages <= 1) {
    return null
  }

  return (
    <nav className="pagination" aria-label={t('showcase.pagination.label')}>
      <Button variant="secondary" onClick={() => onChange(page - 1)} disabled={page <= 1}>
        <span className="pagination__icon" aria-hidden="true">
          {'←'}
        </span>{' '}
        {t('showcase.pagination.previous')}
      </Button>

      <span data-testid="page-indicator">
        {t('showcase.pagination.page', { page, totalPages })}
      </span>

      <Button variant="secondary" onClick={() => onChange(page + 1)} disabled={page >= totalPages}>
        {t('showcase.pagination.next')}{' '}
        <span className="pagination__icon" aria-hidden="true">
          {'→'}
        </span>
      </Button>
    </nav>
  )
}
