import { useLocale } from '../../../app/localeContext'
import { Button } from '../../../shared/ui'

export interface ChipOption {
  readonly slug: string
  readonly label: string
}

interface Props {
  readonly legend: string
  readonly options: readonly ChipOption[]
  readonly selected: readonly string[]
  /** Özet metni; çağıran ICU anahtarını seçer (`0015` R10). */
  readonly summary: string
  readonly onToggle: (slug: string) => void
  readonly onClear: () => void
  /**
   * Test kimlikleri. Kasa tipi ekseni `0001`'in kimliklerini (`body-type-filter`,
   * `selected-count`) KORUR: sayfada tek olmaları gerekir ve `0001`'in testleri onlara
   * bakıyor.
   */
  readonly testId: string
  readonly summaryTestId: string
}

/**
 * Çok değerli eksen: çipler, seçim özeti ve eksenin temizle eylemi (`0015` R10, D20).
 * `0001`'in kasa tipi çubuğundan genelleştirildi; çip GERÇEK bir onay kutusudur, seçili
 * hâl renksiz de okunur (docs/frontend.md § 7). Değerin yanında sonuç sayısı yoktur (X11).
 */
export function ChipAxis({
  legend,
  options,
  selected,
  summary,
  onToggle,
  onClear,
  testId,
  summaryTestId,
}: Props) {
  const { t } = useLocale()

  return (
    <fieldset className="filter-axis" data-testid={testId}>
      <legend className="filter-bar__legend">{legend}</legend>

      <div className="filter-bar__options">
        {options.map((option) => {
          const isSelected = selected.includes(option.slug)

          return (
            <label
              key={option.slug}
              className={isSelected ? 'filter-chip filter-chip--selected' : 'filter-chip'}
            >
              <input
                type="checkbox"
                checked={isSelected}
                onChange={() => onToggle(option.slug)}
                value={option.slug}
              />
              {option.label}
            </label>
          )
        })}
      </div>

      <div className="filter-bar__summary">
        <span data-testid={summaryTestId}>{summary}</span>
        <Button variant="text" onClick={onClear} disabled={selected.length === 0}>
          {t('showcase.filter.clear')}
        </Button>
      </div>
    </fieldset>
  )
}
