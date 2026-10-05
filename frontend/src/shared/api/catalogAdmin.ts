import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'
import type {
  CreateVehicleRequest,
  ReferenceOption,
  VehicleAdminListItem,
} from './types'

/**
 * Panelin araç yüzeyi. Endpoint yolları YALNIZCA burada yazılır; bileşende URL dizesi
 * bulunmaz (docs/frontend.md § 2).
 *
 * Yönetim kökü oturum ister (0006 C18); referans okuma uçları istemez — referans veri
 * gizli değil ve vitrin de kasa tiplerini oradan okuyor.
 */
const ADMIN_VEHICLES = '/api/admin/vehicles'
const REFERENCE = '/api/reference'

export function fetchVehicles(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly VehicleAdminListItem[]> {
  return getJson<readonly VehicleAdminListItem[]>(`${ADMIN_VEHICLES}?locale=${locale}`, locale, signal)
}

export function createVehicle(
  request: CreateVehicleRequest,
  locale: AppLocale,
): Promise<{ readonly vehicleId: string }> {
  return sendJson<{ readonly vehicleId: string }>('POST', ADMIN_VEHICLES, request, locale)
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
    getJson<readonly { id: string; label: string }[]>(`${REFERENCE}/models?locale=${locale}`, locale, signal),
    getJson<readonly { slug: string; label: string }[]>(`${REFERENCE}/body-types?locale=${locale}`, locale, signal),
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
  | 'models'
  | 'bodyTypes'
  | 'fuelTypes'
  | 'transmissions'
  | 'drivetrains'
  | 'trimLevels'

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
