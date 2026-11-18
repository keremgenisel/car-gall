import type { Translator } from '../../../shared/i18n/i18n'
import { formatYear } from '../../../shared/i18n/format'
import { intlLocale, type AppLocale } from '../../../shared/lib/locale'
import type { FilterAxis, ListRange, RangeAxis } from '../listUrlState'

/** Eksenin görünen adı (`0015` R14); çeviri kataloğundan. */
export function axisName(axis: FilterAxis, t: Translator): string {
  return t(`showcase.filter.axis.${axis}`)
}

/**
 * `0015` R10, D15 — aralık ekseninin özeti seçilen sınırlardır: "2018–2021", "≤ 500.000",
 * "≥ 2018". Yıl gruplanmaz (2.018 yazılmaz); kilometre ve fiyat etkin locale'in sayı
 * biçimiyle gruplanır. Cümle ICU ile kurulur, dize birleştirmeyle değil (C7).
 */
export function rangeSummary(
  axis: RangeAxis,
  range: ListRange | undefined,
  locale: AppLocale,
  t: Translator,
): string {
  if (range === undefined) {
    return t('showcase.filter.range.any')
  }

  const format = (value: number) =>
    axis === 'year'
      ? formatYear(value, locale)
      : new Intl.NumberFormat(intlLocale(locale)).format(value)

  if (range.min !== null && range.max !== null) {
    return t('showcase.filter.range.between', { min: format(range.min), max: format(range.max) })
  }

  return range.max !== null
    ? t('showcase.filter.range.atMost', { max: format(range.max) })
    : t('showcase.filter.range.atLeast', { min: format(range.min ?? 0) })
}

/** `0015` AC-38 — etkin eksenlerin adları, etkin locale'in liste biçimiyle ("Yakıt ve Yıl"). */
export function axisList(axes: readonly FilterAxis[], locale: AppLocale, t: Translator): string {
  return new Intl.ListFormat(intlLocale(locale), { style: 'long', type: 'conjunction' }).format(
    axes.map((axis) => axisName(axis, t)),
  )
}
