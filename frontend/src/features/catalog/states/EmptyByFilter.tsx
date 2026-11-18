import { useLocale } from '../../../app/localeContext'
import { Button, EmptyState } from '../../../shared/ui'
import type { FilterAxis } from '../listUrlState'
import { axisList } from '../filters/filterText'

/**
 * 0001 R12(a): daraltma sonucu boş. `0015` R13 — ekran hangi eksenlerin etkin olduğunu
 * ADLARIYLA söyler ve tüm daraltmaları kaldıran eylemi sunar; sıralama korunur (AC-38).
 */
export function EmptyByFilter({
  axes,
  onClear,
}: {
  readonly axes: readonly FilterAxis[]
  readonly onClear: () => void
}) {
  const { locale, t } = useLocale()

  return (
    <EmptyState
      testId="empty-by-filter"
      title={t('showcase.empty.filtered.title')}
      description={
        axes.length === 0
          ? t('showcase.empty.filtered.body')
          : t('showcase.empty.filtered.axes', { axes: axisList(axes, locale, t) })
      }
      action={
        <Button variant="text" onClick={onClear}>
          {t('showcase.filter.clear')}
        </Button>
      }
    />
  )
}
