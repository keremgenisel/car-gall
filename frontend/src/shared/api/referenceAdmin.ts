import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'

/**
 * `0021` — marka ve model yönetimi. Uçlar yalnız Yöneticiye açık (D4); başka rol `403` alır ve
 * panel bu okumaları ona hiç yapmaz. Endpoint yolları YALNIZCA burada yazılır
 * (docs/frontend.md § 2).
 */
const ROOT = '/api/admin/reference'

/** `0021` AC-6 — yönetim listesinin satırı; görünen ad dil → metin haritasıdır. */
export interface AdminBrand {
  readonly id: string
  readonly canonicalName: string
  readonly slug: string
  readonly isActive: boolean
  readonly showOnHome: boolean
  readonly version: number
  readonly displayNames: Readonly<Record<string, string>>
  readonly modelCount: number
}

export interface AdminModel {
  readonly id: string
  readonly brandId: string
  readonly canonicalName: string
  readonly isActive: boolean
  readonly version: number
  readonly displayNames: Readonly<Record<string, string>>
}

export interface AdminBrandDetail {
  readonly brand: AdminBrand
  readonly models: readonly AdminModel[]
}

/**
 * Marka ya da model adı girdisi. Görünen adlarda boş metin "bu dilde ad yok" demektir ve
 * sunucu o dili siler (R3: o dilde kanonik ad gösterilir).
 */
export interface ReferenceNameInput {
  readonly canonicalName: string
  readonly displayNames?: Readonly<Record<string, string>>
}

export type ReferenceAction = 'activate' | 'deactivate'

export function fetchAdminBrands(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly AdminBrand[]> {
  return getJson<readonly AdminBrand[]>(`${ROOT}/brands`, locale, signal)
}

export function fetchAdminBrand(
  brandId: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<AdminBrandDetail> {
  return getJson<AdminBrandDetail>(`${ROOT}/brands/${encodeURIComponent(brandId)}`, locale, signal)
}

/** Yeni marka; kimliğini döner (AC-7). */
export async function createBrand(input: ReferenceNameInput, locale: AppLocale): Promise<string> {
  const created = await sendJson<{ readonly id: string }>('POST', `${ROOT}/brands`, input, locale)
  return created?.id ?? ''
}

/** Kaydeder; yeni sürümü döner. Eski sürümle `409`. */
export async function saveBrand(
  brandId: string,
  version: number,
  input: ReferenceNameInput,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/brands/${encodeURIComponent(brandId)}`,
    { version, ...input },
    locale,
  )
  return result?.version ?? version
}

export async function runBrandAction(
  brandId: string,
  version: number,
  action: ReferenceAction,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'POST',
    `${ROOT}/brands/${encodeURIComponent(brandId)}/${action}`,
    { version },
    locale,
  )
  return result?.version ?? version
}

/** R7 — "ana sayfada göster" işareti (D7). */
export async function setBrandShowOnHome(
  brandId: string,
  version: number,
  showOnHome: boolean,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/brands/${encodeURIComponent(brandId)}/show-on-home`,
    { version, showOnHome },
    locale,
  )
  return result?.version ?? version
}

/** Markaya yeni model (AC-17). */
export async function createModel(
  brandId: string,
  input: ReferenceNameInput,
  locale: AppLocale,
): Promise<string> {
  const created = await sendJson<{ readonly id: string }>(
    'POST',
    `${ROOT}/brands/${encodeURIComponent(brandId)}/models`,
    input,
    locale,
  )
  return created?.id ?? ''
}

/** Model kaydı; marka gövdede YOKTUR — modelin markası değiştirilemez (AC-20). */
export async function saveModel(
  modelId: string,
  version: number,
  input: ReferenceNameInput,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/models/${encodeURIComponent(modelId)}`,
    { version, ...input },
    locale,
  )
  return result?.version ?? version
}

export async function runModelAction(
  modelId: string,
  version: number,
  action: ReferenceAction,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'POST',
    `${ROOT}/models/${encodeURIComponent(modelId)}/${action}`,
    { version },
    locale,
  )
  return result?.version ?? version
}
