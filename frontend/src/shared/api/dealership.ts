import type { AppLocale } from '../lib/locale'
import { getJson } from './client'
import type { PublicGallery } from './types'

/**
 * 0014 **D6**, **D15** — galerinin ziyaretçiye dönük okuması: ad ve aktif şubeler. Oturum
 * gerektirmez. Şubeler sunucuda süzülür ve sıralanır; uç yalnız gösterir (AC-48, AC-66).
 */
export function fetchGallery(locale: AppLocale, signal?: AbortSignal): Promise<PublicGallery> {
  return getJson<PublicGallery>(`/api/dealership/gallery?locale=${locale}`, locale, signal)
}
