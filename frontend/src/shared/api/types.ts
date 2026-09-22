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

/** Oturum bilgisi. Token BULUNMAZ: o yalnız HttpOnly çerezde taşınır (C17, AC-29). */
export interface CurrentSession {
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
