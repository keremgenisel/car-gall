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
  /**
   * 0013 **D44** — personelin kendi ikinci adım ekranı: `/admin/two-factor`. Gezinmede
   * her role basılıyor; yalnız kapılı durumda basılsaydı opsiyonel rolün kendi isteğiyle
   * açması (R15) ve kurtarma kodu yenilemesi (D24) ulaşılamaz kalırdı.
   */
  | 'two-factor'
  /** `0017` R24 — "İçerik" bölümü: sayfa listesi ve SSS (`/admin/content`). */
  | 'content'
  /** `0017` — bir içerik sayfasının düzenlemesi (`/admin/content/{id}`). */
  | 'content-edit'

export type Route =
  /** 0014 **D9** — `/{locale}/` artık ana sayfadır; liste `/{locale}/cars`'a taşındı. */
  | { readonly kind: 'home' }
  | { readonly kind: 'list' }
  | { readonly kind: 'detail'; readonly slug: string }
  /**
   * `0017` **D9** — tek parçalı tanınmayan yol (`/{locale}/{slug}`) bir içerik sayfası
   * adayıdır; yayında bir sayfaya çözülmezse "bulunamadı" ekranı basılır, listeye düşmez.
   */
  | { readonly kind: 'content'; readonly slug: string }
  /**
   * 0010b **D29** — düzenleme adresi `/admin/vehicles/{id}`: liste onun üstü, kayıt
   * onun altı. Panel rotası artık kimlik taşıyor.
   */
  | {
      readonly kind: 'admin'
      readonly view: AdminView
      readonly vehicleId?: string
      /** `0017` — `content-edit` görünümünün sayfa kimliği. */
      readonly pageId?: string
    }

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
    case 'two-factor':
      return 'two-factor'
    // `0017` R24 — panelin "İçerik" bölümü.
    case 'content':
      return 'content'
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

    if (sectionSegment === 'content' && slugSegment !== undefined) {
      return { kind: 'admin', view: 'content-edit', pageId: decodeURIComponent(slugSegment) }
    }

    return { kind: 'admin', view: adminViewOf(sectionSegment) }
  }

  if (localeSegment === undefined || !isAppLocale(localeSegment)) {
    return { kind: 'list' }
  }

  // 0014 D9 — `/{locale}/` ana sayfa. Eski liste adresinin parametreli hâli (`?bodyType=…`)
  // buraya hiç gelmez: sunucu onu 301 ile `/{locale}/cars`'a yönlendirir (D21).
  if (sectionSegment === undefined) {
    return { kind: 'home' }
  }

  // `0017` D9 — `0014` D28'in "tanınmayan alt yol listeye düşer" davranışı TEK parçalı yol
  // için kalkar: `/tr/foo` bir içerik sayfası adayıdır (AC-37). İki parçalı tanınmayan yol
  // (`/tr/foo/bar`) eskisi gibi listeye düşer (AC-38).
  if (sectionSegment !== LISTING_SEGMENT && slugSegment === undefined) {
    return { kind: 'content', slug: decodeURIComponent(sectionSegment) }
  }

  if (sectionSegment !== LISTING_SEGMENT || slugSegment === undefined) {
    return { kind: 'list' }
  }

  return { kind: 'detail', slug: decodeURIComponent(slugSegment) }
}

/** 0014 **D9** — araç listesinin adresi. "Vitrine dön" bağlantıları buraya gider (AC-9–AC-11). */
export function listPath(locale: AppLocale): string {
  return `/${locale}/${LISTING_SEGMENT}`
}

/** 0014 — ana sayfanın adresi; altlığın dil bağlantıları (AC-46). */
export function homePath(locale: AppLocale): string {
  return `/${locale}/`
}

/** `0017` R11 — içerik sayfasının adresi: `/{locale}/{slug}`. */
export function contentPath(locale: AppLocale, slug: string): string {
  return `/${locale}/${encodeURIComponent(slug)}`
}

/** `0017` — içerik sayfasının panel düzenleme adresi. */
export function contentEditPath(pageId: string): string {
  return `/${ADMIN_SEGMENT}/content/${encodeURIComponent(pageId)}`
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
