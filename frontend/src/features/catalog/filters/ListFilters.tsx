import { useLocale } from '../../../app/localeContext'
import type { BodyType, ReferenceTerm } from '../../../shared/api/types'
import {
  clearAxis,
  toggleValue,
  valuesOf,
  withRange,
  type ListState,
  type MultiAxis,
  type RangeAxis as RangeAxisName,
} from '../listUrlState'
import { ChipAxis } from './ChipAxis'
import { RangeAxis } from './RangeAxis'
import { axisName, rangeSummary } from './filterText'

export interface FilterOptions {
  /** `0001` R8 — kasa tipi alanı daraltma ekseni olarak açık mı. */
  readonly bodyTypeFilterable: boolean
  readonly bodyTypes: readonly BodyType[]
  readonly fuels: readonly ReferenceTerm[]
  readonly transmissions: readonly ReferenceTerm[]
}

interface Props {
  readonly state: ListState
  /** `null` = değer listeleri okunamadı: çok değerli eksenler gizlenir, aralıklar kalır. */
  readonly options: FilterOptions | null
  readonly onChange: (next: ListState) => void
}

/**
 * `0015` R1, R10, D20 — liste ekranının filtre paneli: çok değerli eksenler çip, her aralık
 * iki sayı girişi. Yüksekliği `--list-filters-min-block-size` ile AYRILIR ve iskelet aynı
 * sayıyı okur: veri gelince altındaki liste kaymaz (AC-46).
 */
export function ListFilters({ state, options, onChange }: Props) {
  const { locale, t } = useLocale()

  const chip = (
    axis: MultiAxis,
    values: readonly { slug: string; label: string }[],
    testId: string,
    summaryTestId: string,
    summaryKey: string,
  ) => (
    <ChipAxis
      key={axis}
      legend={axisName(axis, t)}
      options={values}
      selected={valuesOf(state, axis)}
      summary={t(summaryKey, { count: valuesOf(state, axis).length })}
      onToggle={(slug) => onChange(toggleValue(state, axis, slug))}
      onClear={() => onChange(clearAxis(state, axis))}
      testId={testId}
      summaryTestId={summaryTestId}
    />
  )

  const range = (axis: RangeAxisName) => (
    <RangeAxis
      key={axis}
      id={axis}
      legend={axisName(axis, t)}
      range={state[axis]}
      summary={rangeSummary(axis, state[axis], locale, t)}
      onApply={(next) => onChange(withRange(state, axis, next))}
      onClear={() => onChange(clearAxis(state, axis))}
    />
  )

  return (
    <div className="list-filters" data-testid="list-filters">
      {options?.bodyTypeFilterable === true
        ? chip(
            'bodyTypes',
            options.bodyTypes,
            'body-type-filter',
            'selected-count',
            'showcase.filter.selectedCount',
          )
        : null}
      {options !== null
        ? chip(
            'fuels',
            options.fuels,
            'fuels-filter',
            'fuels-summary',
            'showcase.filter.selectedValues',
          )
        : null}
      {options !== null
        ? chip(
            'transmissions',
            options.transmissions,
            'transmissions-filter',
            'transmissions-summary',
            'showcase.filter.selectedValues',
          )
        : null}
      {range('year')}
      {range('mileage')}
      {range('price')}
    </div>
  )
}
