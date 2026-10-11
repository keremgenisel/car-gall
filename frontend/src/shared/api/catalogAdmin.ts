import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'
import type {
  BranchSummary,
  CreateVehicleRequest,
  PagedResult,
  ReferenceOption,
  UpdateVehicleRequest,
  VehicleAdminDetail,
  VehicleAdminField,
  VehicleAdminListItem,
  VehicleAdminSummary,
  VehicleMedia,
  VehicleSort,
} from './types'

/**
 * Panelin araç yüzeyi. Endpoint yolları YALNIZCA burada yazılır; bileşende URL dizesi
 * bulunmaz (docs/frontend.md § 2).
 *
 * Yönetim kökü oturum ister (0006 C18); referans okuma uçları istemez — referans veri
 * gizli değil ve vitrin de kasa tiplerini oradan okuyor.
 */
const ADMIN_VEHICLES = '/api/admin/vehicles'
const ADMIN_BRANCHES = '/api/admin/branches'
const ADMIN_VEHICLE_FIELDS = '/api/admin/vehicle-fields'
const REFERENCE = '/api/reference'

/**
 * 0010b — liste artık sayfalı ve sıralı. Sayfa ve sıralama ADRESTEN geliyor; ikisi de
 * ekranın değil ziyaretçinin durumu ve paylaşılabilir olmalı.
 */
export function fetchVehicles(
  locale: AppLocale,
  page: number,
  sort: VehicleSort,
  signal?: AbortSignal,
): Promise<PagedResult<VehicleAdminListItem>> {
  return getJson<PagedResult<VehicleAdminListItem>>(
    `${ADMIN_VEHICLES}?locale=${locale}&page=${page}&sort=${sort}`,
    locale,
    signal,
  )
}

/** AC-23 — sayı TÜM araçlar üzerinden; bu yüzden sayfadan ayrı bir okuma. */
export function fetchVehicleSummary(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<VehicleAdminSummary> {
  return getJson<VehicleAdminSummary>(`${ADMIN_VEHICLES}/summary`, locale, signal)
}

export function fetchVehicle(
  vehicleId: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<VehicleAdminDetail> {
  return getJson<VehicleAdminDetail>(
    `${ADMIN_VEHICLES}/${encodeURIComponent(vehicleId)}`,
    locale,
    signal,
  )
}

/** 0012a/AC-24 — aracın sıralı medyası, `Media`'nın durumlarıyla birleştirilmiş hâlde. */
export function fetchVehicleMedia(
  vehicleId: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<VehicleMedia> {
  return getJson<VehicleMedia>(
    `${ADMIN_VEHICLES}/${encodeURIComponent(vehicleId)}/media`,
    locale,
    signal,
  )
}

/**
 * 0012a **D12** — sıralı kimlik listesinin TAMAMINI yerine koyar: bağlama, sıralama ve
 * çıkarma tek istek. Kapak sıranın SONUCU (**D2**), ayrı bir eylem değil.
 *
 * Boş liste geçerli: "bütün fotoğrafları çıkar" demek ve varlıkları silmiyor (R3).
 */
export async function updateVehicleMedia(
  vehicleId: string,
  mediaAssetIds: readonly string[],
  locale: AppLocale,
): Promise<void> {
  await sendJson<null>(
    'PUT',
    `${ADMIN_VEHICLES}/${encodeURIComponent(vehicleId)}/media`,
    { mediaAssetIds },
    locale,
  )
}

/** R7 — formun basacağı AÇIK opsiyonel alanlar ve tipleri. */
export function fetchVehicleFields(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly VehicleAdminField[]> {
  return getJson<readonly VehicleAdminField[]>(ADMIN_VEHICLE_FIELDS, locale, signal)
}

export async function updateVehicle(
  vehicleId: string,
  request: UpdateVehicleRequest,
  locale: AppLocale,
): Promise<void> {
  await sendJson<null>('PUT', `${ADMIN_VEHICLES}/${encodeURIComponent(vehicleId)}`, request, locale)
}

export async function createVehicle(
  request: CreateVehicleRequest,
  locale: AppLocale,
): Promise<string | null> {
  const created = await sendJson<{ readonly vehicleId: string }>(
    'POST',
    ADMIN_VEHICLES,
    request,
    locale,
  )

  // Uç `201` ile gövde döner; gövdesiz bir yanıt da başarıdır ve ekran için fark etmez.
  return created === null ? null : created.vehicleId
}

/**
 * Şube listesi `Dealership`'in ucundan gelir. Yönetim kökü altında ve oturum ister;
 * referans uçlarının aksine panel verisidir.
 */
export function fetchBranches(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly BranchSummary[]> {
  return getJson<readonly BranchSummary[]>(`${ADMIN_BRANCHES}?locale=${locale}`, locale, signal)
}

/**
 * Altı referans kümesi. Model ve kasa tipi kendi biçimlerini taşıyor; dört terim türü
 * aynı şekle sahip olduğu için tek dönüştürücüden geçiyor — dört ayrı eşleme dört ayrı
 * yerde bakım demekti.
 */
export async function fetchReferenceOptions(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<Record<ReferenceSetName, readonly ReferenceOption[]>> {
  const [models, bodyTypes, fuelTypes, transmissions, drivetrains, trimLevels] = await Promise.all([
    getJson<readonly { id: string; label: string }[]>(
      `${REFERENCE}/models?locale=${locale}`,
      locale,
      signal,
    ),
    getJson<readonly { slug: string; label: string }[]>(
      `${REFERENCE}/body-types?locale=${locale}`,
      locale,
      signal,
    ),
    terms('fuel-types', locale, signal),
    terms('transmission-types', locale, signal),
    terms('drivetrain-types', locale, signal),
    terms('trim-levels', locale, signal),
  ])

  return {
    models: models.map((model) => ({ value: model.id, label: model.label })),
    bodyTypes: bodyTypes.map((type) => ({ value: type.slug, label: type.label })),
    fuelTypes,
    transmissions,
    drivetrains,
    trimLevels,
  }
}

export type ReferenceSetName =
  'models' | 'bodyTypes' | 'fuelTypes' | 'transmissions' | 'drivetrains' | 'trimLevels'

async function terms(
  segment: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly ReferenceOption[]> {
  const rows = await getJson<readonly { slug: string; label: string }[]>(
    `${REFERENCE}/${segment}?locale=${locale}`,
    locale,
    signal,
  )

  return rows.map((row) => ({ value: row.slug, label: row.label }))
}
