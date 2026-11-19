import { useLocale } from '../../../app/localeContext'
import type { BodyType, ReferenceTerm, ShowcaseBrand } from '../../../shared/api/types'
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
  /**
   * `0021` R11 · D9, D21 — stoğu olan markalar ve adresle gelen, var olan ama stoğu kalmamış
   * seçili markalar. Liste boşsa eksen basılmaz (seçilecek bir şey yok).
   */
  readonly brands: readonly ShowcaseBrand[]
}

interface Props {
  readonly state: ListState
  /**
   * `'loading'` = değer listeleri yolda; `null` = okunamadı. İkisinde de çok değerli eksenler
   * basılmaz, aralık eksenleri basılır.
   */
  readonly options: FilterOptions | null | 'loading'
  readonly onChange: (next: ListState) => void
}

/**
 * `0015` R1, R10, D20 — liste ekranının filtre paneli: çok değerli eksenler çip, her aralık
 * iki sayı girişi. Yüksekliği `--list-filters-min-block-size` ile AYRILIR: değer listeleri
 * gelince altındaki liste kaymaz (AC-46).
 */
export function ListFilters({ state, options, onChange }: Props) {
  const { locale, t } = useLocale()

  // Kasa tipi `0001`'in düğme adını korur; diğerleri eksenin adını taşır ("Yakıt seçimini temizle").
  const clearLabel = (axis: MultiAxis | RangeAxisName) =>
    axis === 'bodyTypes'
      ? t('showcase.filter.clear')
      : t('showcase.filter.clearAxis', { axis: axisName(axis, t) })

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
      clearLabel={clearLabel(axis)}
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
      clearLabel={clearLabel(axis)}
    />
  )

  const ready = options !== null && options !== 'loading' ? options : null

  /*
   * SIRA BİLİNÇLİ: veri beklemeyen aralık eksenleri ÖNCE, değer listesi bekleyen çip
   * eksenleri SONRA. Ölçüldü (2026-11-18, Lighthouse mobil): panel mobilde ekranın tamamını
   * kaplıyor ve en büyük metin öğesi panelin bir düğmesi oluyor; panel değer listelerini
   * beklediğinde `/tr/cars`'ın LCP'si 2107 → 2403 ms'e çıktı (en kötü koşu 2477, eşik 2500).
   * Aralıklar ilk render'da çizilir; çipler sona eklenir — önlerindeki hiçbir şey yer
   * değiştirmez ve panelin yüksekliği zaten ayrılmıştır (CLS).
   */
  return (
    <div className="list-filters" data-testid="list-filters" aria-busy={options === 'loading'}>
      {range('year')}
      {range('mileage')}
      {range('price')}
      {ready?.bodyTypeFilterable === true
        ? chip(
            'bodyTypes',
            ready.bodyTypes,
            'body-type-filter',
            'selected-count',
            'showcase.filter.selectedCount',
          )
        : null}
      {ready !== null
        ? chip(
            'fuels',
            ready.fuels,
            'fuels-filter',
            'fuels-summary',
            'showcase.filter.selectedValues',
          )
        : null}
      {ready !== null
        ? chip(
            'transmissions',
            ready.transmissions,
            'transmissions-filter',
            'transmissions-summary',
            'showcase.filter.selectedValues',
          )
        : null}
      {/* `0021` — marka ekseni SONDA: önceki eksenlerin yeri değişmez (D22, AC-43). */}
      {ready !== null && ready.brands.length > 0
        ? chip(
            'brands',
            ready.brands,
            'brands-filter',
            'brands-summary',
            'showcase.filter.selectedValues',
          )
        : null}
    </div>
  )
}
