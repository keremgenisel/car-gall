import { useLocale } from '../../../app/localeContext'
import { LIST_SORTS, isListSort, type ListSort } from '../listUrlState'

const DEFAULT_VALUE = 'default'

/**
 * `0015` R5, R7 — sıralama seçici. Varsayılan sıralama ayrı bir seçenektir ve adrese
 * yazılmaz (D9); seçimli sıralamalar öne çıkarmayı yok sayar (D7, sunucuda).
 */
export function SortSelect({
  sort,
  onChange,
}: {
  readonly sort: ListSort | undefined
  readonly onChange: (sort: ListSort | undefined) => void
}) {
  const { t } = useLocale()

  return (
    <label className="sort-select" htmlFor="list-sort">
      <span className="sort-select__label">{t('showcase.sort.label')}</span>
      <select
        id="list-sort"
        className="sort-select__control"
        data-testid="sort-select"
        value={sort ?? DEFAULT_VALUE}
        onChange={(event) => {
          const value = event.target.value
          onChange(isListSort(value) ? value : undefined)
        }}
      >
        <option value={DEFAULT_VALUE}>{t('showcase.sort.default')}</option>
        {LIST_SORTS.map((value) => (
          <option key={value} value={value}>
            {t(`showcase.sort.${value}`)}
          </option>
        ))}
      </select>
    </label>
  )
}
