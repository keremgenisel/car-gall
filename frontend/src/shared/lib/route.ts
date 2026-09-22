import { isAppLocale, type AppLocale } from './locale'

/**
 * İlan adresinin yol parçası. docs/architecture.md § 3 bu biçimi veriyor:
 * `/ar/cars/toyota-camry-2022-a1b2c3`. Parça her dilde AYNIdır ve çevrilmez —
 * çevrilseydi aynı ilanın üç ayrı adresi olurdu ve AC-4'ün "yalnızca dil öneki
 * farklıdır" kuralı bozulurdu.
 */
export const LISTING_SEGMENT = 'cars'

/**
 * Yönetim panelinin kökü (0006 C13: ayrı bir uygulama değil, aynı Vite uygulamasında
 * `/admin` rota ağacı, lazy-load). Locale ÖNEKİ TAŞIMAZ: panelin dili URL'den değil
 * personelin tercihinden gelir (0006 R16) ve sunucu `<html lang>` ile basar.
 */
export const ADMIN_SEGMENT = 'admin'

export type AdminView = 'staff' | 'password'

export type Route =
  | { readonly kind: 'list' }
  | { readonly kind: 'detail'; readonly slug: string }
  | { readonly kind: 'admin'; readonly view: AdminView }

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

  // Panel locale önekinin DIŞINDADIR; bu dal locale kontrolünden önce gelmek zorunda.
  if (localeSegment === ADMIN_SEGMENT) {
    return { kind: 'admin', view: sectionSegment === 'password' ? 'password' : 'staff' }
  }

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

/** Panel içindeki adres. Giriş ekranının ayrı bir adresi yoktur: oturumsuz `/admin` odur. */
export function adminPath(view: AdminView = 'staff'): string {
  return view === 'staff' ? `/${ADMIN_SEGMENT}` : `/${ADMIN_SEGMENT}/${view}`
}
