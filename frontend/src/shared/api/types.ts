import type { PaintChangeEntry } from '../lib/paintChange'
/** Backend DTO'ları tek yerde tanımlanır; bileşenler bu tipleri kullanır (docs/frontend.md § 2). */

export interface ShowcasePrice {
  readonly onRequest: boolean
  readonly amount: number | null
  readonly currencyCode: string | null
  readonly minorUnitExponent: number | null
}

export interface ShowcaseListing {
  readonly listingId: string
  /** İlanın kalıcı detay adresi (0002/E5). */
  readonly slug: string
  readonly isReserved: boolean
  readonly coverImageUrl: string | null
  readonly modelName: string
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly price: ShowcasePrice
  readonly branchName: string
}

/** Etiketi çözülmüş nitelik satırı: ziyaretçi ham slug görmez. */
export interface ShowcaseAttribute {
  readonly key: string
  readonly label: string
  readonly value: string
}

/**
 * Kapalı veya `Gizli` işaretli alanlar bu yanıtta BULUNMAZ — boş değer olarak da
 * bulunmaz (F3, AC-12, AC-13). Şase, plaka ve iç notlar hiç taşınmaz (F4, AC-14).
 * Gizleme sunucuda uygulanır; uç yalnız geleni gösterir.
 */
/** `0016b` D18 — bir türevin adresi ve GERÇEK boyutu. */
export interface ShowcaseImageVariant {
  readonly url: string
  readonly width: number
  readonly height: number
}

/** `0016b` C12 — galerinin bir görseli: üç türev; orijinal dosya yok (C7). */
export interface ShowcaseImage {
  readonly thumbnail: ShowcaseImageVariant
  readonly display: ShowcaseImageVariant
  readonly large: ShowcaseImageVariant
}

/** `0016b` C12 — haritanın bir parçası; yalnız kod (`0016a` C10). */
export interface ShowcasePaintChange {
  readonly part: string
  readonly state: string
}

export interface ShowcaseListingDetail {
  readonly slug: string
  readonly isReserved: boolean
  /** `0016b` D14 — yalnız hazır görseller, medya sırasıyla. Tek kapak adresi kaldırıldı (D22). */
  readonly images: readonly ShowcaseImage[]
  readonly modelName: string
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly price: ShowcasePrice
  readonly branchName: string
  readonly attributes: readonly ShowcaseAttribute[]
  /**
   * `0016b` D9 — alan açık, public ve değerliyken VAR; aksi hâlde anahtar hiç gelmez
   * (`null` olarak da). Uç süzme yapmaz: yoksa bölüm basılmaz.
   */
  readonly paintChangeMap?: readonly ShowcasePaintChange[]
}

export interface PagedResult<TItem> {
  readonly items: readonly TItem[]
  readonly page: number
  readonly pageSize: number
  readonly totalCount: number
  readonly totalPages: number
}

export interface BodyType {
  readonly slug: string
  readonly label: string
}

/** `0015` D17 — yakıt ve vites değerleri; şekli kasa tipiyle aynı, ek olarak tür. */
export interface ReferenceTerm {
  readonly kind: string
  readonly slug: string
  readonly label: string
}

export interface FieldSetting {
  readonly fieldKey: string
  readonly isOpen: boolean
  readonly isPublic: boolean
  readonly isFilterable: boolean
}

export interface FieldSettings {
  readonly fields: readonly FieldSetting[]
}

/**
 * Rol kodları backend sözleşmesinin kapalı kümesidir (0006 R2). Kod ÇEVRİLMEZ; görünen
 * ad çeviri kataloğundan gelir (C7, Yasak Listesi 22).
 */
export const STAFF_ROLES = ['manager', 'sales_advisor', 'content_editor'] as const

export type StaffRole = (typeof STAFF_ROLES)[number]

export interface LoginRequest {
  readonly loginId: string
  readonly password: string
}

/** Oturum bilgisi. Token BULUNMAZ: o yalnız HttpOnly çerezde taşınır (0006 C17, AC-29). */
export interface CurrentSession {
  /** 0008 D1 — ekran listedeki kendi satırını bununla tanır; ad tekil değil. */
  readonly staffId: string
  readonly displayName: string
  readonly role: StaffRole
  readonly preferredLocale: string
  readonly mustChangePassword: boolean
  /** 0013 D26 — ikinci adım durumu; her istekte kaynaktan okunuyor. */
  readonly twoFactorEnrolled: boolean
}

/**
 * 0013 D15 · D43 — girişin **ayrıştırıcılı** sonucu. Bekleyen dallar kimlik TAŞIMAZ
 * (C13): elinde yalnız parola olan kişi personelin adını, rolünü ve dilini öğrenmez.
 *
 * Dal bir HATA DEĞİLDİR ve `4xx` ile gelmez: uç 2xx dışını `ApiError`'a çeviriyor ve
 * ekran normal bir adımı hata olarak basardı.
 */
export type LoginOutcome =
  | { readonly status: 'completed'; readonly session: CurrentSession }
  /** Kurulumu olan hesap: ikinci adım bekleniyor, oturum çerezi YAZILMADI (D4). */
  | { readonly status: 'two_factor_required' }
  /** Zorunlu rolde kurulumsuz hesap: kapılı bir oturum verildi (D13). */
  | { readonly status: 'two_factor_setup_required' }

/** Kurulum başlangıcı: kare kodu UÇ çizer, sunucu yalnız metin döner (D17/C15). */
export interface TwoFactorEnrollmentStart {
  readonly secretUri: string
  readonly secret: string
}

/** Kurtarma kodu kümesi; **bir kez** gösterilir (D31). */
export interface RecoveryCodes {
  readonly codes: readonly string[]
}

export interface TwoFactorStatus {
  readonly enrolled: boolean
  /** Zorunluluk ROLDEN türetiliyor (D25); sözleşme politikayı dondurmuyor. */
  readonly required: boolean
  readonly remainingRecoveryCodes: number
}

export interface StaffListItem {
  readonly staffId: string
  readonly displayName: string
  readonly loginId: string
  readonly role: StaffRole
  readonly status: 'active' | 'deactivated'
  readonly preferredLocale: string
  readonly branchId: string | null
  /** 0013 D25 — kapanmış sözleşmeye eklemeli alan; rozet veri olmadan basılamaz. */
  readonly twoFactorEnrolled: boolean
}

export interface AddStaffRequest {
  readonly displayName: string
  readonly loginId: string
  readonly initialPassword: string
  readonly role: StaffRole
  readonly preferredLocale: string
  readonly branchId: string | null
}

/**
 * Talep tipleri backend sözleşmesinin kapalı kümesidir (0005 R3). Uç kendi listesini
 * uydurmaz: burada olmayan bir değer sunucuda `lead.invalid_type` ile reddedilir.
 *
 * `trade_in` BİLEREK yok: takas sistemde tanınan bir tiptir ama bu spec'te kabul
 * edilmez (AC-10) ve ziyaretçiye seçtirilecek bir şey değildir.
 */
export const LEAD_TYPES = [
  'information_request',
  'call_back',
  'test_drive',
  'price_offer',
  'financing_consultation',
] as const

export type LeadType = (typeof LEAD_TYPES)[number]

export interface CaptureLeadRequest {
  readonly listingSlug: string
  readonly leadType: LeadType
  readonly locale: string
  readonly fullName: string
  readonly phone: string | null
  readonly email: string | null
  readonly message: string | null
  readonly consentGiven: boolean
}

export interface CaptureLeadResponse {
  readonly leadId: string
}

/** 0010a — referans veri seçenekleri. Etiket personelin locale'inde gelir (AC-13). */
export interface ReferenceOption {
  readonly value: string
  readonly label: string
}

/** `0021` — araç formunun model seçeneği; marka seçimine göre süzülür (R8). */
export interface ModelOption extends ReferenceOption {
  readonly brandId: string
}

/** `0021` — vitrin marka ekseninin seçeneği: adres kısaltması ve ziyaretçinin dilindeki ad. */
export interface ShowcaseBrand {
  readonly slug: string
  readonly label: string
}

/** `0021` D24 — aracın kayıtlı modelinin marka kimliği ve iki parçanın adı (pasif olabilir). */
export interface VehicleAdminModel {
  readonly brandId: string
  readonly brandLabel: string
  readonly modelLabel: string
}

export interface VehicleAdminPrice {
  readonly onRequest: boolean
  readonly amount: number | null
  readonly currencyCode: string | null
  readonly minorUnitExponent: number | null
}

export interface VehicleAdminListItem {
  readonly vehicleId: string
  readonly modelName: string
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly price: VehicleAdminPrice
  readonly listingStatus: string
  readonly vin: string
  /** 0010b — listenin "son güncellenen" ekseni; ISO 8601, biçimlendirme uçta. */
  readonly updatedAt: string
  /** Metni eksik olan diller. KOD taşır; görünen ad çeviri kataloğundan gelir (C11). */
  readonly missingLocales: readonly string[]
  /**
   * 0012b **R13** · **D15** — araç lokasyon ataması bekliyor: şubesi kapatıldı ve yenisi
   * atanmadı. Liste bunu ayırt eder; şubesi atanmadan yayına alınamaz.
   */
  readonly awaitingBranchAssignment: boolean
  /** Zorunlu işaretli bir opsiyonel alanı boş olan araç. Sunucuda hesaplanır (C21). */
  readonly hasMissingRequiredInfo: boolean
  /**
   * 0012c **D50** — ilan öne çıkarılmış mı. Eylemin değiştirdiği durum, eylemin yaşadığı
   * yüzeyde görünür olmalı: bu alan olmadan öne çıkarma yalnızca **yazan** bir düğme
   * olurdu ve "zaten öne çıkarılmış" cevabı ekranda gösterilemezdi.
   */
  readonly isFeatured: boolean
}

/** R15/AC-23 — çeviri eksik ilan sayısı TÜM araçlar üzerinden; sayfaya bağlı değil. */
export interface VehicleAdminSummary {
  readonly totalCount: number
  readonly translationIncompleteCount: number
}

/** 0010b R14 — liste sıralaması. Değerler adres çubuğundaki yazımlarıdır. */
export const VEHICLE_SORTS = ['last-updated', 'recently-added'] as const

export type VehicleSort = (typeof VEHICLE_SORTS)[number]

/**
 * 0012b — bir ilan üzerinde çalışan altı durum geçişi. Değerler **rota parçalarıdır**:
 * fiil rotada, çünkü bunlar gerçek durum geçişleri (`docs/conventions.md` § 2'nin saydığı
 * istisna).
 */
export const LISTING_ACTIONS = [
  'publish',
  'unpublish',
  'reserve',
  'unreserve',
  'sold',
  'archive',
] as const

export type ListingAction = (typeof LISTING_ACTIONS)[number]

/**
 * 0023 — satışı geri almanın dönebileceği üç durum. Değerler sunucunun durum KODLARIDIR
 * (`listingStatus` ile aynı sözlük); ekran onları `vehicle.status.*` anahtarlarından çevirir.
 */
export const SALE_REVERT_TARGETS = ['Published', 'Reserved', 'Draft'] as const

export type SaleRevertTarget = (typeof SALE_REVERT_TARGETS)[number]

/** 0023 **R6** — tek bir geri alma kaydı. Kayıt salt okunurdur (AC-21). */
export interface SaleReversalRecord {
  readonly id: string
  readonly staffDisplayName: string
  readonly revertedAt: string
  readonly reason: string
  readonly restoredStatus: string
}

/**
 * 0023 — geri alma penceresinin önizlemesi ve ilanın kayıtları (**R7**, **R9**).
 *
 * <p>`targetStatus` yalnız satılmış ilanda dolu. `draftReasons` hedef Taslak olduğunda
 * NEDENİ taşır ve yayın reddinin kodlarıdır (`vehicle.cannot_publish.*`); ekran onları
 * aynı anahtarlardan çevirir.</p>
 */
export interface SaleReversalOverview {
  readonly listingStatus: string
  readonly targetStatus: string | null
  readonly draftReasons: readonly string[]
  readonly missingFields: readonly string[]
  readonly records: readonly SaleReversalRecord[]
}

/**
 * 0012c — öne çıkarmanın iki yönü. `ListingAction`'dan **AYRI** tutuldu ve bu kararın
 * kendisi: öne çıkarma bir durum geçişi değil, bir **sıralama** eylemidir. Aynı listeye
 * konsaydı `listingActionState`'in A3 tablosu onu bir geçiş gibi göstermeye çalışırdı.
 */
export const LISTING_FEATURE_ACTIONS = ['feature', 'unfeature'] as const

export type ListingFeatureAction = (typeof LISTING_FEATURE_ACTIONS)[number]

/**
 * 0012c **D18** — toplu yüzeyin **altı** eylemi. Rezerve etme ve rezervasyonu kaldırma
 * burada **yok**: rezervasyon bir müşteriye aittir.
 */
export const BULK_LISTING_OPERATIONS = [
  'publish',
  'unpublish',
  'sold',
  'archive',
  'feature',
  'unfeature',
] as const

export type BulkListingOperation = (typeof BULK_LISTING_OPERATIONS)[number]

/**
 * 0012c **D45** — araç başına sonuç satırı. Şekil **tek**; doluluk değişir.
 *
 * <p>`listingStatus` başarılı satırda da geliyor: panel listeyi yeniden çekmeden satırı
 * boyayabiliyor. `null` yalnız bulunamayan araçta — onun durumu yok.</p>
 *
 * <p>`parameters` tekil ucun gerekçesinin **aynısı**: yayın reddinde `reasons` ve eksik
 * alan anahtarları burada duruyor ve panel onları mevcut çeviricilerle okuyor.</p>
 */
export interface BulkListingRow {
  readonly vehicleId: string
  readonly applied: boolean
  readonly errorCode: string | null
  readonly parameters: Record<string, string> | null
  readonly listingStatus: string | null
}

/** Sonuç **zarf** içinde: çıplak dizi, yanıta bir gün alan eklemeyi kırıcı yapardı. */
export interface BulkListingResult {
  readonly results: readonly BulkListingRow[]
}

/**
 * Opsiyonel alan tanımı. Tip KODDAN gelir (D21), zorunluluk ayardan. Kapalı alanlar bu
 * listede BULUNMAZ (R7) — form onları hiç basmaz.
 */
export interface VehicleAdminField {
  readonly fieldKey: string
  readonly kind:
    | 'Text'
    | 'LocalizedText'
    | 'Integer'
    | 'Amount'
    | 'Boolean'
    | 'Date'
    | 'Option'
    /** `0016a` C11 — parça haritası: 13 parça × 4 durum, yalnız kod. */
    | 'PartMap'
  readonly isRequired: boolean
}

export interface VehicleAdminOptionalValue {
  readonly fieldKey: string
  readonly kind: string
  readonly value: string | null
  readonly values: Record<string, string> | null
  /** `0016a` — yalnız `PartMap` tipinde dolu; C10 sırasıyla 13 kod çifti. */
  readonly partMap?: readonly PaintChangeEntry[] | null
}

/** Düzenleme ekranının okuduğu tam kayıt. Yalnız AÇIK alanların değerleri gelir (F3). */
export interface VehicleAdminDetail {
  readonly vehicleId: string
  readonly branchId: string
  readonly vehicleModelId: string
  readonly bodyType: string
  readonly fuelType: string
  readonly transmissionType: string
  readonly drivetrainType: string
  readonly trimLevel: string
  readonly color: Record<string, string>
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly price: VehicleAdminPrice
  readonly vin: string
  readonly listingStatus: string
  readonly listingTitle: Record<string, string>
  readonly listingDescription: Record<string, string>
  readonly optionalValues: readonly VehicleAdminOptionalValue[]
  readonly updatedAt: string
  /**
   * 0012b **R13** · **D15** — araç lokasyon ataması bekliyor. Eylem yüzeyi bunu düğmeye
   * basılmadan söylüyor; sunucu da reddediyor (AC-29).
   */
  readonly awaitingBranchAssignment: boolean
  /** `0021` D24 — aracın kendi marka ve modeli; yoksa (bilinmeyen kimlik) `null`. */
  readonly model?: VehicleAdminModel | null
}

/**
 * Düzenleme isteği. Çekirdek alanların TAMAMINI taşır (C25) ve şaseyi de taşır — şase
 * değiştirilemez ama gövdede bulunur ki değiştirme girişimi reddedilebilsin (D24).
 */
export interface UpdateVehicleRequest {
  readonly branchId: string
  readonly vehicleModelId: string
  readonly bodyType: string
  readonly fuelType: string
  readonly transmissionType: string
  readonly drivetrainType: string
  readonly trimLevel: string
  readonly color: Record<string, string>
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly priceAmount: number | null
  readonly priceOnRequest: boolean
  readonly vin: string
  readonly listingTitle: Record<string, string>
  readonly listingDescription: Record<string, string>
  readonly optionalValues: readonly {
    readonly fieldKey: string
    readonly value: string | null
    readonly values: Record<string, string> | null
    /**
     * `0016a` **D27** — parça haritasının yapılandırılmış alanı. Yoksa haritaya dokunulmaz;
     * boş liste TEMİZLER (**D32**).
     */
    readonly partMap?: readonly PaintChangeEntry[]
  }[]
}

/** Para BİRİMİ yok: kurulum başına sabit ve panelden değiştirilemez (C3, P3). */
export interface CreateVehicleRequest {
  readonly branchId: string
  readonly vehicleModelId: string
  readonly bodyType: string
  readonly fuelType: string
  readonly transmissionType: string
  readonly drivetrainType: string
  readonly trimLevel: string
  readonly color: Record<string, string>
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly priceAmount: number | null
  readonly priceOnRequest: boolean
  readonly vin: string
}

/**
 * 0010a — formun şube seçimi. Durum KOD olarak gelir (C11, Yasak Listesi 22) ve
 * kapatılmış şubeler de listede: ayıklama çağıranın işi, sunucu yalnız olguyu söyler.
 */
export interface BranchSummary {
  readonly branchId: string
  readonly name: string
  readonly status: string
}

/**
 * 0012a — bir medya varlığının işleme durumu. `docs/domain.md` **M1**'in dört hâli; KOD
 * olarak geliyor ve ekran onu çeviri kataloğundan çeviriyor (Yasak Listesi 22–23).
 */
export type MediaProcessingStatus = 'uploaded' | 'processing' | 'ready' | 'failed'

/** Yüklemenin yanıtı (AC-1): kimlik ve işleme durumu. */
export interface MediaAssetUploaded {
  readonly id: string
  readonly status: MediaProcessingStatus
}

/** Türevin boyutu ve erişim yolu. `docs/frontend.md` § 7 açık `width`/`height` istiyor. */
export interface VehicleMediaDerivative {
  readonly variant: string
  readonly width: number
  readonly height: number
  readonly url: string
}

/**
 * Aracın sıralı listesindeki bir fotoğraf, `Media`'nın hâliyle birlikte. Sıra listenin
 * İNDEKSİNDE taşınıyor; ayrı bir "sıra" alanı aynı olguyu iki yerde tutardı.
 */
export interface VehicleMediaItem {
  readonly mediaAssetId: string
  readonly status: MediaProcessingStatus
  /** Yalnız `failed` durumda dolu; kapalı bir kümeden bir kod (`0011`/AC-33). */
  readonly failureReason: string | null
  readonly altText: Readonly<Record<string, string>>
  readonly derivatives: readonly VehicleMediaDerivative[]
}

/**
 * Aracın medya yüzeyi. `coverMediaAssetId` **türetilmiş**: sıralı listenin ilk elemanı
 * (**D2**); liste boşsa `null`.
 */
export interface VehicleMedia {
  readonly vehicleId: string
  readonly coverMediaAssetId: string | null
  readonly items: readonly VehicleMediaItem[]
}

/** 0014 **D15** — izin listesi: sunucu yalnız bu alanları gönderir (AC-14). */
export interface PublicBranch {
  readonly name: string
  readonly address: string
  /** E.164 (`+902165550001`); `tel:` bağlantısının hedefi (AC-45). */
  readonly phone: string
}

/** `0024` R6 · D7 — platform KODU ve kayıtlı adres; başka alan yok. */
export interface PublicSocialLink {
  readonly platform: string
  readonly url: string
}

export interface PublicGallery {
  /** `0020` D15 — ziyaretçinin dilindeki görünen ad; ticari unvan public yanıtta YOKTUR. */
  readonly displayName: string
  readonly branches: readonly PublicBranch[]
  /** `0024` D7 — D1'in sırasıyla; hiç hesap yoksa boş dizi (null değil). */
  readonly socialLinks: readonly PublicSocialLink[]
}
