import { isAppLocale, type AppLocale } from './locale'

/**
 * İlan adresinin yol parçası. docs/architecture.md § 3 bu biçimi veriyor:
 * `/ar/cars/toyota-camry-2022-a1b2c3`. Parça her dilde AYNIdır ve çevrilmez —
 * çevrilseydi aynı ilanın üç ayrı adresi olurdu ve AC-4'ün "yalnızca dil öneki
 * farklıdır" kuralı bozulurdu.
 */
export const LISTING_SEGMENT = 'cars'

export type Route = { readonly kind: 'list' } | { readonly kind: 'detail'; readonly slug: string }

/**
 * Rota adresten okunur; router kütüphanesi yoktur (0001'den gelen yapı).
 *
 * Tanınmayan bir alt yol liste sayfasını gösterir; bu 0001'in bugünkü davranışıdır
 * (locale önekinden sonrası yok sayılıyordu). 0002 onu değiştirmez: değiştirmek yeni
 * bir "sayfa bulunamadı" davranışı tanımlamak olurdu ve hiçbir kriteri yok.
 */
export function parseRoute(pathname: string): Route {
  const [localeSegment, sectionSegment, slugSegment] = pathname
    .split('/')
    .filter((segment) => segment.length > 0)

  if (localeSegment === undefined || !isAppLocale(localeSegment)) {
    return { kind: 'list' }
  }

  if (sectionSegment !== LISTING_SEGMENT || slugSegment === undefined) {
    return { kind: 'list' }
  }

  return { kind: 'detail', slug: decodeURIComponent(slugSegment) }
}

/** Karttan detaya giden adres (AC-2, AC-4). */
export function listingPath(locale: AppLocale, slug: string): string {
  return `/${locale}/${LISTING_SEGMENT}/${encodeURIComponent(slug)}`
}
