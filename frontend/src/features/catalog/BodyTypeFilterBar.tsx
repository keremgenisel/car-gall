import { useLocale } from '../../app/localeContext'
import type { BodyType } from '../../api/types'

interface Props {
  readonly bodyTypes: readonly BodyType[]
  readonly selected: readonly string[]
  readonly onToggle: (slug: string) => void
  readonly onClear: () => void
}

/**
 * R8: bu spec'te daraltmanın tek ekseni kasa tipidir. Alan ayarları eksen ÜRETMEZ;
 * çubuğun gösterilip gösterilmeyeceğinin kapısıdır (AC-11, AC-12). Kapı kararı
 * çağıranda verilir; bu bileşen yalnız verilen ekseni çizer.
 */
export function BodyTypeFilterBar({ bodyTypes, selected, onToggle, onClear }: Props) {
  const { t } = useLocale()

  return (
    <fieldset className="filter-bar" data-testid="body-type-filter">
      <legend className="filter-bar__legend">{t('showcase.filter.legend')}</legend>

      <div className="filter-bar__options">
        {bodyTypes.map((bodyType) => {
          const isSelected = selected.includes(bodyType.slug)

          return (
            <label
              key={bodyType.slug}
              className={isSelected ? 'filter-chip filter-chip--selected' : 'filter-chip'}
            >
              <input
                type="checkbox"
                checked={isSelected}
                onChange={() => onToggle(bodyType.slug)}
                value={bodyType.slug}
              />
              {bodyType.label}
            </label>
          )
        })}
      </div>

      <div className="filter-bar__summary">
        <span data-testid="selected-count">
          {t('showcase.filter.selectedCount', { count: selected.length })}
        </span>
        <button
          type="button"
          className="link-button"
          onClick={onClear}
          disabled={selected.length === 0}
        >
          {t('showcase.filter.clear')}
        </button>
      </div>
    </fieldset>
  )
}
