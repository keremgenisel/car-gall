import type { AppLocale } from '../../shared/lib/locale'

/** `frontend-003` S1 — karşılaştırmaya en çok üç araç. */
export const MAX_COMPARED = 3

/** `0002` E9 · `#137` — slug biçimi: `[a-z0-9-]`, 1–160 karakter (`ListingSlug.MaxLength`). */
const SLUG = /^[a-z0-9-]{1,160}$/

/**
 * `frontend-003` **D3** — karşılaştırma adresinin `listing` değerleri: biçime uymayan (büyük harf,
 * boşluk, bozuk kodlama) yok sayılır, yinelenen ikinci kez sayılmaz, ilk görülme sırası korunur ve
 * ilk üç geçerli değer alınır.
 */
export function comparedSlugs(search: string): readonly string[] {
  const slugs: string[] = []

  for (const value of new URLSearchParams(search).getAll('listing')) {
    if (slugs.length < MAX_COMPARED && SLUG.test(value) && !slugs.includes(value)) {
      slugs.push(value)
    }
  }

  return slugs
}

/**
 * Okunan kümenin adresi: `listing` değerleri baştadır, öbür parametreler (`utm_source`) olduğu gibi
 * korunur (D3 madde 4, B11).
 */
export function compareSearch(search: string, slugs: readonly string[]): string {
  const others = new URLSearchParams(search)
  others.delete('listing')
  const parts = [
    ...slugs.map((slug) => `listing=${encodeURIComponent(slug)}`),
    others.toString(),
  ].filter((part) => part !== '')

  return parts.length === 0 ? '' : `?${parts.join('&')}`
}

/** `/{locale}/compare?listing=…` — favoriler ekranının "Karşılaştır" eyleminin hedefi (D6). */
export function comparePath(locale: AppLocale, slugs: readonly string[]): string {
  // `compare` route.ts'teki `COMPARE_SEGMENT`'tir; tembel parça ilk paketin bir bağını daha
  // dışa açtırmasın diye burada yazılı (D18). Ayna testi: `FavoriKarsilastirmaBelgeTests`.
  return `/${locale}/compare${compareSearch('', slugs)}`
}
