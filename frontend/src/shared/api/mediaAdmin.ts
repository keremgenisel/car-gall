import type { AppLocale } from '../lib/locale'
import { sendFile, sendJson } from './client'
import type { MediaAssetUploaded } from './types'

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
): Promise<MediaAssetUploaded> {
  return sendFile<MediaAssetUploaded>(ADMIN_MEDIA_ASSETS, file, locale, signal)
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
