import { useLocale } from '../../../app/localeContext'
import { Button, EmptyState } from '../../../shared/ui'

/** 0001 R12(a): daraltma sonucu boş. Ziyaretçiye ne yapabileceği söylenir. */
export function EmptyByFilter({ onClear }: { readonly onClear: () => void }) {
  const { t } = useLocale()

  return (
    <EmptyState
      testId="empty-by-filter"
      title={t('showcase.empty.filtered.title')}
      description={t('showcase.empty.filtered.body')}
      action={
        <Button variant="text" onClick={onClear}>
          {t('showcase.filter.clear')}
        </Button>
      }
    />
  )
}
