import { fetchBodyTypes, fetchFieldSettings } from '../../shared/api/catalog'
import type { BodyType } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'

const BODY_TYPE_FIELD = 'body_type'

export interface BodyTypeOptions {
  /** Kasa tipi alanı daraltma ekseni olarak açık mı (`0001` R8, AC-12). */
  readonly filterable: boolean
  readonly bodyTypes: readonly BodyType[]
}

/**
 * Kasa tipi ekseninin TEK kaynağı. Liste ekranının filtre çubuğu ve ana sayfanın daraltma
 * formu buradan okur: `0014` **D19** ikisinin aynı kasa tiplerini aynı sırada
 * göstermesini, **D25** aynı kapıdan geçmesini istiyor. Hata yutulmaz; ne yapılacağına
 * çağıran karar verir (liste gizler, form hata durumu gösterir — D25).
 */
export async function loadBodyTypeOptions(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<BodyTypeOptions> {
  const [settings, bodyTypes] = await Promise.all([
    fetchFieldSettings(locale, signal),
    fetchBodyTypes(locale, signal),
  ])
  const field = settings.fields.find((item) => item.fieldKey === BODY_TYPE_FIELD)

  return { filterable: field?.isFilterable === true, bodyTypes }
}
