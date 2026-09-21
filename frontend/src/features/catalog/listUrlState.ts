/**
 * Daraltma ve sayfalama durumu URL'de yaşar; sunucu verisi global store'a kopyalanmaz
 * (C8, docs/frontend.md § 8). Kanonik biçim C13'te sabittir:
 *   - çoklu seçim tekrarlı parametredir (`?bodyType=sedan&bodyType=suv`), virgüllü liste değil
 *   - `page=1` URL'ye yazılmaz
 *   - boş `bodyType` parametresi yazılmaz
 */
export interface ListState {
  readonly bodyTypes: readonly string[]
  readonly page: number
}

export const BODY_TYPE_PARAM = 'bodyType'
export const PAGE_PARAM = 'page'

export function parseListState(search: string): ListState {
  const params = new URLSearchParams(search)
  const bodyTypes: string[] = []

  for (const value of params.getAll(BODY_TYPE_PARAM)) {
    // Tanınmayan değeri uç ELEMEZ: kanonik kümeyi sunucu bilir ve yok sayar (AC-17).
    // Uç yalnız boşu ve tekrarı temizler.
    if (value !== '' && !bodyTypes.includes(value)) {
      bodyTypes.push(value)
    }
  }

  const rawPage = params.get(PAGE_PARAM)
  const parsedPage = rawPage === null ? 1 : Number.parseInt(rawPage, 10)
  const page = Number.isFinite(parsedPage) && parsedPage >= 1 ? parsedPage : 1

  return { bodyTypes, page }
}

export function toCanonicalSearch(state: ListState): string {
  const params = new URLSearchParams()

  for (const bodyType of state.bodyTypes) {
    params.append(BODY_TYPE_PARAM, bodyType)
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

/**
 * Daraltma değiştiğinde sayfa 1'e döner (AC-8) ve bu TEK bir geçmiş kaydıdır: sayfa
 * sıfırlaması ayrı bir kayıt üretmez (R5). İki ayrı state güncellemesi yapmak, geri
 * tuşunun ziyaretçiyi ara bir hâle düşürmesine yol açardı (AC-19).
 */
export function withBodyTypes(bodyTypes: readonly string[]): ListState {
  return { bodyTypes: [...bodyTypes], page: 1 }
}

export function toggleBodyType(state: ListState, bodyType: string): ListState {
  const next = state.bodyTypes.includes(bodyType)
    ? state.bodyTypes.filter((value) => value !== bodyType)
    : [...state.bodyTypes, bodyType]

  return withBodyTypes(next)
}

export function clearBodyTypes(): ListState {
  return withBodyTypes([])
}

/** Sayfa değişimi de tek kayıttır; daraltma korunur (R5). */
export function withPage(state: ListState, page: number): ListState {
  return { bodyTypes: [...state.bodyTypes], page: page < 1 ? 1 : page }
}

function normalizeSearch(search: string): string {
  return search === '' || search === '?' ? '' : search.startsWith('?') ? search : `?${search}`
}
