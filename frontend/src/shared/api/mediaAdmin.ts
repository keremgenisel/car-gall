import type { AppLocale } from '../lib/locale'
import { getJson, sendFile, sendJson } from './client'
import type { MediaAssetUploaded, MediaProcessingStatus, VehicleMediaDerivative } from './types'

/**
 * `Media` modülünün yönetim uçları. Yol yalnız bu klasörde yazılır
 * (`docs/frontend.md` § 2) ve hepsi `0006`'nın yönetim kökünde — oturum zorunlu.
 */
const ADMIN_MEDIA_ASSETS = '/api/admin/media/assets'

/** 0012a **D9** · AC-1 — tek dosya yüklemesi; varlık `uploaded` durumunda oluşur. */
export function uploadMediaAsset(
  file: File,
  locale: AppLocale,
  signal?: AbortSignal,
  kind?: 'logo',
): Promise<MediaAssetUploaded> {
  return sendFile<MediaAssetUploaded>(
    ADMIN_MEDIA_ASSETS,
    file,
    locale,
    signal,
    kind === undefined ? {} : { kind },
  )
}

/**
 * `0026` D8 · D9 — tek varlığın durumu ve türevleri (panelin logo önizlemesi). Sunucu durumu ve
 * hata nedenini SAYI olarak döner (`Media.Contracts` enum değerleri; uç bu biçimi `0011`'den beri
 * koruyor); burada `MediaProcessingStatus` koduna ve nedenin adına çevrilir.
 */
export async function fetchMediaAsset(
  mediaAssetId: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<MediaAssetState> {
  const ham = await getJson<{
    readonly status: number
    readonly failureReason: number | null
    readonly derivatives: readonly VehicleMediaDerivative[]
  }>(`${ADMIN_MEDIA_ASSETS}/${encodeURIComponent(mediaAssetId)}`, locale, signal)

  return {
    status: STATUS[ham.status] ?? 'failed',
    failureReason: ham.failureReason === null ? null : (FAILURE[ham.failureReason] ?? 'generic'),
    derivatives: ham.derivatives,
  }
}

/** `MediaProcessingStatus`: Uploaded = 1, Processing = 2, Ready = 3, Failed = 4. */
const STATUS: Readonly<Record<number, MediaProcessingStatus>> = {
  1: 'uploaded',
  2: 'processing',
  3: 'ready',
  4: 'failed',
}

/** `MediaFailureReason`: Undecodable = 1, WatermarkUnavailable = 2, StorageUnavailable = 3. */
const FAILURE: Readonly<Record<number, string>> = {
  1: 'undecodable',
  2: 'watermarkunavailable',
  3: 'storageunavailable',
}

/** `0026` — panelin logo bölümünün okuduğu varlık hâli. */
export interface MediaAssetState {
  readonly status: MediaProcessingStatus
  readonly failureReason: string | null
  readonly derivatives: readonly VehicleMediaDerivative[]
}

/**
 * 0012a **D16** · AC-11 — `alt` metnini BÜTÜNÜYLE yerine koyar. Eksik locale boş sayılır ve
 * bir dili boş bırakmak geçerlidir (R4); bu yüzden ekran her kaydetmede üç dili birlikte
 * gönderiyor.
 */
export async function setMediaAssetAltText(
  mediaAssetId: string,
  altText: Readonly<Record<string, string>>,
  locale: AppLocale,
): Promise<void> {
  await sendJson<null>(
    'PUT',
    `${ADMIN_MEDIA_ASSETS}/${encodeURIComponent(mediaAssetId)}/alt-text`,
    { altText },
    locale,
  )
}

/**
 * 0012a **D4** · AC-17 — tekrar deneme. Komut ve uç `0011`'de zaten vardı; eksik olan
 * ekrandı (`0011` X8). Varlık **aynı kimlikle** yeniden kuyruğa alınır.
 */
export async function retryMediaAsset(mediaAssetId: string, locale: AppLocale): Promise<void> {
  await sendJson<null>(
    'POST',
    `${ADMIN_MEDIA_ASSETS}/${encodeURIComponent(mediaAssetId)}/retry`,
    undefined,
    locale,
  )
}
