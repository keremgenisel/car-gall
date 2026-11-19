/**
 * Daraltma, sıralama ve sayfalama durumu URL'de yaşar; sunucu verisi global store'a
 * kopyalanmaz (C8, docs/frontend.md § 8). Kanonik biçim `0001` C13'te sabittir ve `0015` C12
 * onu genişletir:
 *   - çoklu seçim tekrarlı parametredir (`?bodyType=sedan&bodyType=suv`), virgüllü liste değil
 *   - `page=1`, boş parametre ve VARSAYILAN sıralama URL'ye yazılmaz (D9)
 *   - parametre sırası sabittir: eksenler, sıralama, sayfa
 *
 * `0015` D18 — geçersiz ARALIK ve tanınmayan `sort` uçta düşürülür (kural sözdizimseldir);
 * tanınmayan SLUG uçta elenmez, kanonik kümeyi sunucu bilir (`0001` AC-17).
 *
 * Yeni alanlar OPSİYONELDİR ve boşken nesnede hiç bulunmaz: bir eksen yoksa durumun şekli
 * `0001`'deki `{ bodyTypes, page }` ile aynıdır.
 */
export interface ListRange {
  readonly min: number | null
  readonly max: number | null
}

export const LIST_SORTS = [
  'newest',
  'price-asc',
  'price-desc',
  'mileage-asc',
  'year-desc',
  'year-asc',
] as const

export type ListSort = (typeof LIST_SORTS)[number]

/** `0021` R11 — marka ekseni SONA eklendi: kanonik adreste var olan eksenlerin sırası değişmez. */
export const MULTI_AXES = ['bodyTypes', 'fuels', 'transmissions', 'brands'] as const
export type MultiAxis = (typeof MULTI_AXES)[number]

export const RANGE_AXES = ['year', 'mileage', 'price'] as const
export type RangeAxis = (typeof RANGE_AXES)[number]

export type FilterAxis = MultiAxis | RangeAxis

export interface ListState {
  readonly bodyTypes: readonly string[]
  readonly page: number
  readonly fuels?: readonly string[]
  readonly transmissions?: readonly string[]
  /** `0021` R12 — `brand` (tekrarlı kısaltma); tanınmayan değeri sunucu yok sayar (D20). */
  readonly brands?: readonly string[]
  readonly year?: ListRange
  readonly mileage?: ListRange
  readonly price?: ListRange
  readonly sort?: ListSort
}

export const BODY_TYPE_PARAM = 'bodyType'
export const PAGE_PARAM = 'page'

const MULTI_PARAMS: Record<MultiAxis, string> = {
  bodyTypes: BODY_TYPE_PARAM,
  fuels: 'fuel',
  transmissions: 'transmission',
  brands: 'brand',
}

const RANGE_PARAMS: Record<RangeAxis, readonly [string, string]> = {
  year: ['yearMin', 'yearMax'],
  mileage: ['mileageMin', 'mileageMax'],
  price: ['priceMin', 'priceMax'],
}

const SORT_PARAM = 'sort'

export function parseListState(search: string): ListState {
  const params = new URLSearchParams(search)

  let state: ListState = { bodyTypes: parseValues(params.getAll(BODY_TYPE_PARAM)), page: 1 }

  for (const axis of ['fuels', 'transmissions', 'brands'] as const) {
    const values = parseValues(params.getAll(MULTI_PARAMS[axis]))
    if (values.length > 0) {
      state = { ...state, [axis]: values }
    }
  }

  for (const axis of RANGE_AXES) {
    const [minParam, maxParam] = RANGE_PARAMS[axis]
    const range = parseRange(params.get(minParam), params.get(maxParam))
    if (range !== undefined) {
      state = { ...state, [axis]: range }
    }
  }

  const sort = params.get(SORT_PARAM)
  if (sort !== null && isListSort(sort)) {
    state = { ...state, sort }
  }

  const rawPage = params.get(PAGE_PARAM)
  const parsedPage = rawPage === null ? 1 : Number.parseInt(rawPage, 10)
  const page = Number.isFinite(parsedPage) && parsedPage >= 1 ? parsedPage : 1

  return { ...state, page }
}

export function toCanonicalSearch(state: ListState): string {
  const params = new URLSearchParams()

  for (const axis of MULTI_AXES) {
    for (const value of valuesOf(state, axis)) {
      params.append(MULTI_PARAMS[axis], value)
    }
  }

  for (const axis of RANGE_AXES) {
    const range = state[axis]
    const [minParam, maxParam] = RANGE_PARAMS[axis]
    if (range?.min !== null && range?.min !== undefined) {
      params.set(minParam, String(range.min))
    }
    if (range?.max !== null && range?.max !== undefined) {
      params.set(maxParam, String(range.max))
    }
  }

  if (state.sort !== undefined) {
    params.set(SORT_PARAM, state.sort)
  }

  if (state.page > 1) {
    params.set(PAGE_PARAM, String(state.page))
  }

  const query = params.toString()
  return query === '' ? '' : `?${query}`
}

export function isCanonicalSearch(search: string): boolean {
  return toCanonicalSearch(parseListState(search)) === normalizeSearch(search)
}

export function isListSort(value: string): value is ListSort {
  return (LIST_SORTS as readonly string[]).includes(value)
}

export function valuesOf(state: ListState, axis: MultiAxis): readonly string[] {
  return state[axis] ?? []
}

/**
 * `0015` R15 — sınırlar işaretsiz tam sayıdır. Verilmiş bir sınır geçersizse ya da alt
 * sınır üstten büyükse EKSENİN TAMAMI düşer (sunucunun kuralıyla aynı, D10).
 */
export function parseRange(minRaw: string | null, maxRaw: string | null): ListRange | undefined {
  const min = parseBound(minRaw)
  const max = parseBound(maxRaw)

  if (min === undefined || max === undefined) {
    return undefined
  }

  if (min === null && max === null) {
    return undefined
  }

  return min !== null && max !== null && min > max ? undefined : { min, max }
}

/**
 * Daraltma değiştiğinde sayfa 1'e döner (AC-8, `0015` R11) ve bu TEK bir geçmiş kaydıdır:
 * sayfa sıfırlaması ayrı bir kayıt üretmez (R5). İki ayrı state güncellemesi yapmak, geri
 * tuşunun ziyaretçiyi ara bir hâle düşürmesine yol açardı (AC-19).
 */
export function withBodyTypes(bodyTypes: readonly string[]): ListState {
  return { bodyTypes: [...bodyTypes], page: 1 }
}

export function toggleBodyType(state: ListState, bodyType: string): ListState {
  return toggleValue(state, 'bodyTypes', bodyType)
}

export function clearBodyTypes(): ListState {
  return withBodyTypes([])
}

export function toggleValue(state: ListState, axis: MultiAxis, value: string): ListState {
  const current = valuesOf(state, axis)
  const next = current.includes(value)
    ? current.filter((item) => item !== value)
    : [...current, value]

  return withValues(state, axis, next)
}

/** `0015` AC-36 — yalnız O eksen temizlenir; diğer eksenler ve sıralama korunur. */
export function clearAxis(state: ListState, axis: FilterAxis): ListState {
  return isMultiAxis(axis) ? withValues(state, axis, []) : withRange(state, axis, undefined)
}

export function withRange(
  state: ListState,
  axis: RangeAxis,
  range: ListRange | undefined,
): ListState {
  const rest = without(state, axis)
  return range === undefined || (range.min === null && range.max === null)
    ? { ...rest, page: 1 }
    : { ...rest, [axis]: range, page: 1 }
}

/** `0015` R7 — `undefined` varsayılan sıralamadır ve adrese yazılmaz. */
export function withSort(state: ListState, sort: ListSort | undefined): ListState {
  const rest = without(state, 'sort')
  return sort === undefined ? { ...rest, page: 1 } : { ...rest, sort, page: 1 }
}

/** `0015` AC-38 — bütün daraltmalar kalkar; sıralama korunur. */
export function clearFilters(state: ListState): ListState {
  return state.sort === undefined
    ? { bodyTypes: [], page: 1 }
    : { bodyTypes: [], page: 1, sort: state.sort }
}

/** Etkin eksenler, çubuktaki sırayla. */
export function activeAxes(state: ListState): FilterAxis[] {
  return [
    ...MULTI_AXES.filter((axis) => valuesOf(state, axis).length > 0),
    ...RANGE_AXES.filter((axis) => state[axis] !== undefined),
  ]
}

/** Sayfa değişimi de tek kayıttır; daraltma ve sıralama korunur (R5). */
export function withPage(state: ListState, page: number): ListState {
  return { ...state, bodyTypes: [...state.bodyTypes], page: page < 1 ? 1 : page }
}

function withValues(state: ListState, axis: MultiAxis, values: readonly string[]): ListState {
  if (axis === 'bodyTypes') {
    return { ...state, bodyTypes: [...values], page: 1 }
  }

  const rest = without(state, axis)
  return values.length === 0 ? { ...rest, page: 1 } : { ...rest, [axis]: [...values], page: 1 }
}

function isMultiAxis(axis: FilterAxis): axis is MultiAxis {
  return (MULTI_AXES as readonly string[]).includes(axis)
}

function without(state: ListState, key: keyof ListState): ListState {
  const copy: Record<string, unknown> = { ...state }
  delete copy[key]
  return copy as unknown as ListState
}

function parseValues(raw: readonly string[]): string[] {
  const values: string[] = []

  for (const value of raw) {
    // Tanınmayan değeri uç ELEMEZ: kanonik kümeyi sunucu bilir ve yok sayar (AC-17, D18).
    // Uç yalnız boşu ve tekrarı temizler.
    if (value !== '' && !values.includes(value)) {
      values.push(value)
    }
  }

  return values
}

/** `undefined` = geçersiz · `null` = verilmedi. */
function parseBound(raw: string | null): number | null | undefined {
  if (raw === null || raw === '') {
    return null
  }

  if (!/^\d+$/.test(raw)) {
    return undefined
  }

  const value = Number(raw)
  return Number.isSafeInteger(value) && value <= 2_147_483_647 ? value : undefined
}

function normalizeSearch(search: string): string {
  return search === '' || search === '?' ? '' : search.startsWith('?') ? search : `?${search}`
}
