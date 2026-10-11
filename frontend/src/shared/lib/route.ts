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

/**
 * 0010a — panele araç yüzeyi eklendi. `vehicles` liste, `vehicle-new` kayıt formu;
 * düzenleme adresi `0010b`'nin işi (X18).
 */
export type AdminView =
  | 'staff'
  | 'password'
  | 'vehicles'
  | 'vehicle-new'
  | 'vehicle-edit'
  /** 0012a **D17** — aracın medya yüzeyi: `/admin/vehicles/{id}/media`. */
  | 'vehicle-media'

export type Route =
  | { readonly kind: 'list' }
  | { readonly kind: 'detail'; readonly slug: string }
  /**
   * 0010b **D29** — düzenleme adresi `/admin/vehicles/{id}`: liste onun üstü, kayıt
   * onun altı. Panel rotası artık kimlik taşıyor.
   */
  | { readonly kind: 'admin'; readonly view: AdminView; readonly vehicleId?: string }

/**
 * Rota adresten okunur; router kütüphanesi yoktur (0001'den gelen yapı).
 *
 * Tanınmayan bir alt yol liste sayfasını gösterir; bu 0001'in bugünkü davranışıdır
 * (locale önekinden sonrası yok sayılıyordu). 0002 onu değiştirmez: değiştirmek yeni
 * bir "sayfa bulunamadı" davranışı tanımlamak olurdu ve hiçbir kriteri yok.
 */
/** Tanınmayan alt yol personel listesine düşer; 0001'den gelen davranış korunuyor. */
function adminViewOf(segment: string | undefined): AdminView {
  switch (segment) {
    case 'password':
      return 'password'
    case 'vehicles':
      return 'vehicles'
    case 'vehicle-new':
      return 'vehicle-new'
    default:
      return 'staff'
  }
}

export function parseRoute(pathname: string): Route {
  const [localeSegment, sectionSegment, slugSegment, tailSegment] = pathname
    .split('/')
    .filter((segment) => segment.length > 0)

  // Panel locale önekinin DIŞINDADIR; bu dal locale kontrolünden önce gelmek zorunda.
  if (localeSegment === ADMIN_SEGMENT) {
    // `/admin/vehicles/{id}` düzenleme, `/admin/vehicles` liste. Üçüncü parça yalnız
    // araç yüzeyinde anlamlı; başka bir bölümde yok sayılır.
    if (sectionSegment === 'vehicles' && slugSegment !== undefined) {
      /*
       * 0012a **D17** — dördüncü parça medya yüzeyi: `/admin/vehicles/{id}/media`.
       * Ayrı ekran, çünkü düzenleme ekranı 0010b ile zaten büyük ve medya yüzeyinin kendi
       * üç durumu ve kendi kaydetme akışı var; sekme yapmak iki kaydetme akışını
       * birbirine bağlardı.
       *
       * Tanınmayan bir dördüncü parça DÜZENLEMEye düşüyor — 0001'den gelen "tanınmayan
       * alt yol daha temel olana düşer" davranışı; yeni bir "sayfa bulunamadı" tanımlamak
       * bu spec'in işi değil.
       */
      const vehicleId = decodeURIComponent(slugSegment)

      return tailSegment === 'media'
        ? { kind: 'admin', view: 'vehicle-media', vehicleId }
        : { kind: 'admin', view: 'vehicle-edit', vehicleId }
    }

    return { kind: 'admin', view: adminViewOf(sectionSegment) }
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

/** Düzenleme adresi (D29). Listeye dönüş `adminPath('vehicles')`. */
export function vehicleEditPath(vehicleId: string): string {
  return `/${ADMIN_SEGMENT}/vehicles/${encodeURIComponent(vehicleId)}`
}

/** 0012a **D17** — medya yüzeyinin adresi; düzenlemenin bir alt yolu. */
export function vehicleMediaPath(vehicleId: string): string {
  return `${vehicleEditPath(vehicleId)}/media`
}
