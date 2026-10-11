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
export interface ShowcaseListingDetail {
  readonly slug: string
  readonly isReserved: boolean
  readonly coverImageUrl: string | null
  readonly modelName: string
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly price: ShowcasePrice
  readonly branchName: string
  readonly attributes: readonly ShowcaseAttribute[]
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
}

export interface StaffListItem {
  readonly staffId: string
  readonly displayName: string
  readonly loginId: string
  readonly role: StaffRole
  readonly status: 'active' | 'deactivated'
  readonly preferredLocale: string
  readonly branchId: string | null
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
 * Opsiyonel alan tanımı. Tip KODDAN gelir (D21), zorunluluk ayardan. Kapalı alanlar bu
 * listede BULUNMAZ (R7) — form onları hiç basmaz.
 */
export interface VehicleAdminField {
  readonly fieldKey: string
  readonly kind: 'Text' | 'LocalizedText' | 'Integer' | 'Amount' | 'Boolean' | 'Date' | 'Option'
  readonly isRequired: boolean
}

export interface VehicleAdminOptionalValue {
  readonly fieldKey: string
  readonly kind: string
  readonly value: string | null
  readonly values: Record<string, string> | null
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
