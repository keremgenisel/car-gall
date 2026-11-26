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
  /** `0022` **D9** — İçerik bölümündeki ana sayfa metinleri (`/admin/content/home`). */
  | 'content-home'
  /** `0020` R1 — galeri profili (`/admin/gallery`); yalnız Yönetici (D2). */
  | 'gallery'
  /** `0020` R8 — şubeler listesi (`/admin/branches`). */
  | 'branches'
  /** `0020` R9 — yeni şube (`/admin/branches/new`). */
  | 'branch-new'
  /** `0020` R10–R12 — bir şubenin düzenlemesi ve durum eylemleri (`/admin/branches/{id}`). */
  | 'branch-edit'
  /** `0021` R5 — markalar ve modeller (`/admin/brands`); yalnız Yönetici (D4). */
  | 'brands'
  /** `0021` — bir markanın ekranı ve modelleri (`/admin/brands/{id}`). */
  | 'brand-edit'

/**
 * `0018` **D26** — bülten teyit ve çıkış sayfalarının yol parçası:
 * `/{locale}/newsletter/confirm?token=…` · `/{locale}/newsletter/unsubscribe?token=…`.
 * Bültendeki mutlak bağlantı ve Host'un `noindex` kararı aynı kalıbı okur (AC-51).
 */
export const NEWSLETTER_SEGMENT = 'newsletter'

export type Route =
  /** 0014 **D9** — `/{locale}/` artık ana sayfadır; liste `/{locale}/cars`'a taşındı. */
  | { readonly kind: 'home' }
  /** `0018` — teyit sayfası; abonelik yalnız sayfadaki düğmeyle etkinleşir (D6). */
  | { readonly kind: 'newsletter-confirm' }
  /** `0018` — çıkış sayfası; açıldığı anda çıkarır (D6). */
  | { readonly kind: 'newsletter-unsubscribe' }
  | { readonly kind: 'list' }
  | { readonly kind: 'detail'; readonly slug: string }
  /**
   * `0017` **D9** — tek parçalı tanınmayan yol (`/{locale}/{slug}`) bir içerik sayfası
   * adayıdır; yayında bir sayfaya çözülmezse "bulunamadı" ekranı basılır, listeye düşmez.
   */
  | { readonly kind: 'content'; readonly slug: string }
  /**
   * `0004` **D8** · **P7** — tanınmayan vitrin yolu: iki ve daha çok parçalı yol, ilanın altındaki
   * yol, ayrılmış ad. Sunucu bu yolları 404 ile verir; uç aynı "bulunamadı" ekranını basar
   * (tuzak 9: durum kodu ile ekran çelişmesin).
   */
  | { readonly kind: 'not-found' }
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
      /** `0020` — `branch-edit` görünümünün şube kimliği. */
      readonly branchId?: string
      /** `0021` — `brand-edit` görünümünün marka kimliği. */
      readonly brandId?: string
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
    // `0020` — galeri profili ve şubeler.
    case 'gallery':
      return 'gallery'
    case 'branches':
      return 'branches'
    // `0021` — markalar ve modeller.
    case 'brands':
      return 'brands'
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

    // `0020` — `/admin/branches/new` yeni şube, `/admin/branches/{id}` düzenleme.
    if (sectionSegment === 'branches' && slugSegment !== undefined) {
      return slugSegment === 'new'
        ? { kind: 'admin', view: 'branch-new' }
        : { kind: 'admin', view: 'branch-edit', branchId: decodeURIComponent(slugSegment) }
    }

    // `0021` — `/admin/brands/{id}` bir markanın ekranı.
    if (sectionSegment === 'brands' && slugSegment !== undefined) {
      return { kind: 'admin', view: 'brand-edit', brandId: decodeURIComponent(slugSegment) }
    }

    // `0022` D9 — `home` bir sayfa kimliği değil (kimlikler Guid): ana sayfa metinleri ekranı.
    if (sectionSegment === 'content' && slugSegment === 'home') {
      return { kind: 'admin', view: 'content-home' }
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

  // `0018` D26 — `newsletter` ayrılmış addır (`ContentReservedSlugs`); içerik sayfası olamaz.
  if (sectionSegment === NEWSLETTER_SEGMENT && tailSegment === undefined) {
    if (slugSegment === 'confirm') {
      return { kind: 'newsletter-confirm' }
    }

    if (slugSegment === 'unsubscribe') {
      return { kind: 'newsletter-unsubscribe' }
    }
  }

  if (sectionSegment === LISTING_SEGMENT) {
    if (slugSegment === undefined) {
      return { kind: 'list' }
    }

    // `0004` D8 — ilanın altındaki yol (`/tr/cars/{slug}/x`) detay değildir.
    return tailSegment === undefined
      ? { kind: 'detail', slug: decodeSegment(slugSegment) }
      : { kind: 'not-found' }
  }

  // `0017` D9 — tek parçalı yol bir içerik sayfası adayıdır (AC-37); ayrılmış ad değilse.
  // `0004` D8 — `0017` AC-38'in "iki parçalı tanınmayan yol listeye düşer" davranışı kalktı:
  // `/tr/foo/bar`, `/tr/newsletter`, `/tr/newsletter/foo`, `/tr/admin` "bulunamadı"dır.
  if (slugSegment === undefined && !RESERVED_SEGMENTS.has(sectionSegment)) {
    return { kind: 'content', slug: decodeSegment(sectionSegment) }
  }

  return { kind: 'not-found' }
}

/**
 * `Content.Contracts`'taki `ContentReservedSlugs`'un ayna listesi (`0017` C11, `0018` D26): bu adlar
 * içerik sayfası olamaz. Sunucu aynı listeyle 404 verir (`SeoPage`).
 */
const RESERVED_SEGMENTS: ReadonlySet<string> = new Set([
  LISTING_SEGMENT,
  ADMIN_SEGMENT,
  'api',
  'assets',
  NEWSLETTER_SEGMENT,
])

/**
 * Triyaj BULGU-3 — bozuk kodlanmış parça (`%`, `%E0`) `decodeURIComponent`'i fırlatır ve sayfa
 * boş kalırdı. Çözülemeyen parça ham hâliyle döner: uç onu bulamaz, ekran "bulunamadı" olur.
 */
function decodeSegment(segment: string): string {
  try {
    return decodeURIComponent(segment)
  } catch {
    return segment
  }
}

/** 0014 **D9** — araç listesinin adresi. "Vitrine dön" bağlantıları buraya gider (AC-9–AC-11). */
export function listPath(locale: AppLocale): string {
  return `/${locale}/${LISTING_SEGMENT}`
}

/** `0018` AC-43 — ana sayfadaki bülten bölümü; yeniden abone olma bağlantısının hedefi. */
export function newsletterSectionPath(locale: AppLocale): string {
  return `/${locale}/#${NEWSLETTER_SEGMENT}`
}

/** `0018` **D26** — bağlantının anahtarı sorgu parametresindedir; yoksa boş metin. */
export function newsletterTokenOf(search: string): string {
  return new URLSearchParams(search).get('token') ?? ''
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
  if (view === 'branch-new') {
    return `/${ADMIN_SEGMENT}/branches/new`
  }

  if (view === 'content-home') {
    return `/${ADMIN_SEGMENT}/content/home`
  }

  return view === 'staff' ? `/${ADMIN_SEGMENT}` : `/${ADMIN_SEGMENT}/${view}`
}

/** `0020` — bir şubenin panel düzenleme adresi. */
export function branchEditPath(branchId: string): string {
  return `/${ADMIN_SEGMENT}/branches/${encodeURIComponent(branchId)}`
}

/** `0021` — bir markanın panel adresi. */
export function brandEditPath(brandId: string): string {
  return `/${ADMIN_SEGMENT}/brands/${encodeURIComponent(brandId)}`
}

/** Düzenleme adresi (D29). Listeye dönüş `adminPath('vehicles')`. */
export function vehicleEditPath(vehicleId: string): string {
  return `/${ADMIN_SEGMENT}/vehicles/${encodeURIComponent(vehicleId)}`
}

/** 0012a **D17** — medya yüzeyinin adresi; düzenlemenin bir alt yolu. */
export function vehicleMediaPath(vehicleId: string): string {
  return `${vehicleEditPath(vehicleId)}/media`
}
