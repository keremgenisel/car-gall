import type { AppLocale } from '../lib/locale'
import { ApiError, getJson } from './client'
import type {
  BodyType,
  FieldSettings,
  PagedResult,
  ShowcaseListing,
  ShowcaseListingDetail,
} from './types'

const NOT_FOUND = 404

export interface ShowcaseQuery {
  readonly bodyTypes: readonly string[]
  readonly page: number
}

export function fetchShowcaseListings(
  query: ShowcaseQuery,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<PagedResult<ShowcaseListing>> {
  const params = new URLSearchParams()

  for (const bodyType of query.bodyTypes) {
    params.append('bodyType', bodyType)
  }

  if (query.page > 1) {
    params.set('page', String(query.page))
  }

  params.set('locale', locale)

  return getJson<PagedResult<ShowcaseListing>>(
    `/api/catalog/showcase-listings?${params.toString()}`,
    locale,
    signal,
  )
}

/**
 * `null` = ilan vitrinde yok. "Gizli" ile "hiç yok" ayrımı burada da YAPILMAZ: sunucu
 * ikisine de aynı karşılığı verir (E7, C13, AC-8) ve uç ikisini ayıracak bir bilgi almaz.
 *
 * Bulunamadı ile hata AYRI durumlardır (AC-18 / AC-19); ayrım api/ katmanında kalır,
 * ekran yalnız `null` görür ve durum kodunu hiç bilmez.
 */
export async function fetchListingDetail(
  slug: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<ShowcaseListingDetail | null> {
  try {
    return await getJson<ShowcaseListingDetail>(
      `/api/catalog/showcase-listings/${encodeURIComponent(slug)}?locale=${locale}`,
      locale,
      signal,
    )
  } catch (cause) {
    if (cause instanceof ApiError && cause.status === NOT_FOUND) {
      return null
    }

    throw cause
  }
}

export function fetchBodyTypes(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly BodyType[]> {
  return getJson<readonly BodyType[]>(`/api/reference/body-types?locale=${locale}`, locale, signal)
}

export function fetchFieldSettings(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<FieldSettings> {
  return getJson<FieldSettings>('/api/catalog/field-settings', locale, signal)
}
