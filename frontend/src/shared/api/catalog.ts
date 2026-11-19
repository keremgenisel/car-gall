import type { AppLocale } from '../lib/locale'
import { ApiError, getJson } from './client'
import type {
  BodyType,
  FieldSettings,
  PagedResult,
  ReferenceTerm,
  ShowcaseListing,
  ShowcaseListingDetail,
  ShowcaseBrand,
} from './types'

const NOT_FOUND = 404

export interface ShowcaseRange {
  readonly min: number | null
  readonly max: number | null
}

/**
 * Vitrin listesinin sorgusu. `0015` C12'nin parametre adlarıyla birebir; yeni alanlar
 * opsiyonel — verilmeyen eksen sorguya yazılmaz.
 */
export interface ShowcaseQuery {
  readonly bodyTypes: readonly string[]
  readonly page: number
  readonly fuels?: readonly string[]
  readonly transmissions?: readonly string[]
  readonly year?: ShowcaseRange
  readonly mileage?: ShowcaseRange
  readonly price?: ShowcaseRange
  readonly sort?: string
  /** `0021` R11 — marka ekseni (`brand`, tekrarlı kısaltma). */
  readonly brands?: readonly string[]
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

  for (const fuel of query.fuels ?? []) {
    params.append('fuel', fuel)
  }

  for (const transmission of query.transmissions ?? []) {
    params.append('transmission', transmission)
  }

  for (const brand of query.brands ?? []) {
    params.append('brand', brand)
  }

  appendRange(params, 'year', query.year)
  appendRange(params, 'mileage', query.mileage)
  appendRange(params, 'price', query.price)

  if (query.sort !== undefined) {
    params.set('sort', query.sort)
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

function appendRange(
  params: URLSearchParams,
  name: string,
  range: ShowcaseRange | undefined,
): void {
  if (range?.min !== null && range?.min !== undefined) {
    params.set(`${name}Min`, String(range.min))
  }

  if (range?.max !== null && range?.max !== undefined) {
    params.set(`${name}Max`, String(range.max))
  }
}

/**
 * `0015` D17 — yakıt ve vitesin değer listesi `0010a`'dan beri var olan public uçlardan gelir;
 * türün TÜM değerleri, kanonik ada göre sıralı, etkin locale'in adıyla. Değer başına sayı
 * yoktur (X11).
 */
export function fetchReferenceTerms(
  segment: 'fuel-types' | 'transmission-types',
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly ReferenceTerm[]> {
  return getJson<readonly ReferenceTerm[]>(
    `/api/reference/${segment}?locale=${locale}`,
    locale,
    signal,
  )
}

/**
 * 0014 **D3** — ana sayfanın öne çıkan bölümü: galerinin öne çıkardığı görünür ilanlar,
 * listeyle aynı sırada, en çok altı tane. Boş dizi bir hata değildir (AC-21).
 */
/**
 * `0021` R11 · D21 — liste marka ekseninin seçenekleri: stoğu olan markalar ve adresle gelen,
 * var olan ama stoğu kalmamış seçili markalar (ziyaretçi seçimi kaldırabilsin).
 */
export function fetchShowcaseBrands(
  selected: readonly string[],
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly ShowcaseBrand[]> {
  const params = new URLSearchParams()

  for (const brand of selected) {
    params.append('brand', brand)
  }

  params.set('locale', locale)

  return getJson<readonly ShowcaseBrand[]>(
    `/api/catalog/showcase-brands?${params.toString()}`,
    locale,
    signal,
  )
}

/** `0021` R13 — ana sayfa formunun markaları: "ana sayfada göster" işaretli ve stoğu olanlar. */
export function fetchHomeBrands(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly ShowcaseBrand[]> {
  return getJson<readonly ShowcaseBrand[]>(
    `/api/catalog/showcase-brands?scope=home&locale=${locale}`,
    locale,
    signal,
  )
}

export function fetchFeaturedListings(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly ShowcaseListing[]> {
  return getJson<readonly ShowcaseListing[]>(
    `/api/catalog/featured-listings?locale=${locale}`,
    locale,
    signal,
  )
}

/**
 * 0014 **D27** — görünür ilanların toplam sayısı (R6). Yeni bir uç açılmadı: liste ucu
 * `totalCount`'u zaten döndürüyor ve tek kartlık sayfa istemek en ucuz okumadır. Boş
 * vitrin kararı bu sayıdan verilir (D12).
 */
export async function fetchShowcaseTotal(locale: AppLocale, signal?: AbortSignal): Promise<number> {
  const page = await getJson<PagedResult<ShowcaseListing>>(
    `/api/catalog/showcase-listings?pageSize=1&locale=${locale}`,
    locale,
    signal,
  )

  return page.totalCount
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
