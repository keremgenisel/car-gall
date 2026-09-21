/** Backend DTO'ları tek yerde tanımlanır; bileşenler bu tipleri kullanır (docs/frontend.md § 2). */

export interface ShowcasePrice {
  readonly onRequest: boolean
  readonly amount: number | null
  readonly currencyCode: string | null
  readonly minorUnitExponent: number | null
}

export interface ShowcaseListing {
  readonly listingId: string
  readonly isReserved: boolean
  readonly coverImageUrl: string | null
  readonly modelName: string
  readonly modelYear: number
  readonly mileageKilometers: number
  readonly price: ShowcasePrice
  readonly branchName: string
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
