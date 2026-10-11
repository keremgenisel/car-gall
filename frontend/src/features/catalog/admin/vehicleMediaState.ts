import type { VehicleMedia, VehicleMediaItem } from '../../../shared/api/types'
import { catalogKeys } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'

/**
 * Medya ekranının SAF mantığı. Bileşenin içinde karar vermek yerine burada: sıra, kapak,
 * kirlilik ve `alt` metninin taslağı birim testle ölçülebilir olmalı
 * (`docs/frontend.md` § 8 — saf mantık içeren yardımcılar için birim testi yazılır).
 */

/** Ekranın düzenlediği taslak: sıralı kimlikler + locale başına `alt` metni. */
export interface MediaDraft {
  readonly order: readonly string[]
  readonly altText: Readonly<Record<string, Readonly<Record<string, string>>>>
}

export function toDraft(media: VehicleMedia): MediaDraft {
  const altText: Record<string, Record<string, string>> = {}

  for (const item of media.items) {
    altText[item.mediaAssetId] = { ...item.altText }
  }

  return { order: media.items.map((item) => item.mediaAssetId), altText }
}

/**
 * **D2** — kapak sıralı listenin ilk elemanı. Ayrı bir alan DEĞİL, türetilmiş: ikinci bir
 * "kapak" alanı aynı olguyu iki yerde tutar ve biri bayatlar.
 */
export function coverOf(draft: MediaDraft): string | null {
  return draft.order.length > 0 ? draft.order[0] : null
}

/**
 * Bir fotoğrafı bir adım yukarı/aşağı taşır. "Kapak yap" ayrı bir eylem değil, bir SIRA
 * işlemidir (**D2**) — ekran bunu böyle anlatmak zorunda.
 *
 * Sınır dışına taşıma AYNI taslağı döner: uçtaki öğenin düğmesi devre dışı olsa bile
 * klavyeyle tetiklenebilir ve sessizce listeyi bozmamalı.
 */
export function move(draft: MediaDraft, mediaAssetId: string, delta: -1 | 1): MediaDraft {
  const from = draft.order.indexOf(mediaAssetId)

  if (from < 0) {
    return draft
  }

  const to = from + delta

  if (to < 0 || to >= draft.order.length) {
    return draft
  }

  const order = [...draft.order]
  const [moved] = order.splice(from, 1)
  order.splice(to, 0, moved)

  return { ...draft, order }
}

/** R3 — çıkarmak varlığı SİLMEZ; yalnız aracın listesinden düşürür. */
export function detach(draft: MediaDraft, mediaAssetId: string): MediaDraft {
  return { ...draft, order: draft.order.filter((id) => id !== mediaAssetId) }
}

/** Yeni yüklenen fotoğraf listenin SONUNA ekleniyor: kapağı sessizce değiştirmemeli. */
export function attach(draft: MediaDraft, mediaAssetId: string): MediaDraft {
  if (draft.order.includes(mediaAssetId)) {
    return draft
  }

  return {
    order: [...draft.order, mediaAssetId],
    altText: { ...draft.altText, [mediaAssetId]: draft.altText[mediaAssetId] ?? {} },
  }
}

export function withAltText(
  draft: MediaDraft,
  mediaAssetId: string,
  locale: string,
  value: string,
): MediaDraft {
  return {
    ...draft,
    altText: {
      ...draft.altText,
      [mediaAssetId]: { ...(draft.altText[mediaAssetId] ?? {}), [locale]: value },
    },
  }
}

/** Sunucuya gidecek `alt` metni: boş diller DÜŞÜRÜLÜYOR — eksik locale boş sayılır (**D16**). */
export function altTextRequest(
  draft: MediaDraft,
  mediaAssetId: string,
): Readonly<Record<string, string>> {
  const taslak = draft.altText[mediaAssetId] ?? {}
  const sonuc: Record<string, string> = {}

  for (const [locale, metin] of Object.entries(taslak)) {
    const kirpilmis = metin.trim()

    if (kirpilmis.length > 0) {
      sonuc[locale] = kirpilmis
    }
  }

  return sonuc
}

/** Sıra değişti mi? Kaydet düğmesi bunu okuyor. */
export function orderChanged(draft: MediaDraft, media: VehicleMedia): boolean {
  const sunucu = media.items.map((item) => item.mediaAssetId)

  return (
    draft.order.length !== sunucu.length || draft.order.some((id, index) => id !== sunucu[index])
  )
}

/** Bir fotoğrafın `alt` metni değişti mi? Kaydetme varlık BAŞINA yapılıyor (**D16**). */
export function altTextChanged(
  draft: MediaDraft,
  media: VehicleMedia,
  mediaAssetId: string,
): boolean {
  const sunucu = media.items.find((item) => item.mediaAssetId === mediaAssetId)?.altText ?? {}
  const taslak = altTextRequest(draft, mediaAssetId)
  const anahtarlar = new Set([...Object.keys(sunucu), ...Object.keys(taslak)])

  for (const anahtar of anahtarlar) {
    if ((sunucu[anahtar] ?? '') !== (taslak[anahtar] ?? '')) {
      return true
    }
  }

  return false
}

/**
 * **R6** — aracın medya koşulu: en az bir bağlı fotoğraf `ready`. Ekran bunu bir rozet
 * olarak gösteriyor ve **D18** gereği yayın eylemi bu dilimde YOK — ölçüm var, eylem yok.
 *
 * Sunucudaki hâlden okunuyor, taslaktan değil: kaydedilmemiş bir sıra yayına uygunluğu
 * değiştirmez (sıra koşulu etkilemiyor, yalnız hangi fotoğrafın kapak olduğunu).
 */
export function mediaConditionMet(media: VehicleMedia): boolean {
  return media.items.some((item) => item.status === 'ready')
}

/** **D8** — "işleniyor" varlığın DURUMU; ekranın yükleme iskeletiyle karışmamalı. */
export function isProcessing(item: VehicleMediaItem): boolean {
  return item.status === 'uploaded' || item.status === 'processing'
}

/**
 * Hata kodunu çeviri anahtarına çevirir. **İki** modülün kodları buradan geçiyor:
 * `media.too_large` ve `vehicle.media_asset_not_found` aynı ekranda görünür, çünkü
 * ekranın tek bir işi var ve personel hangi modülün reddettiğini bilmek zorunda değil.
 *
 * Bilinmeyen kod GENEL mesaja düşer ve kodun kendisi personele ASLA gösterilmez
 * (`docs/frontend.md` § 3.1, AC-29).
 */
export function mediaMessageKey(errorCode: string, locale: AppLocale): string {
  const suffix = errorCode.replace(/^(media|vehicle)\./, '').replace(/\./g, '_')
  const candidate = `vehicle.media.problem.${suffix}`

  return catalogKeys(locale).includes(candidate) ? candidate : 'vehicle.media.problem.generic'
}

/**
 * Varlığın durum etiketinin çeviri anahtarı (AC-16, AC-28). `failed` için **neden**
 * de yazılıyor: `0011`/AC-33 kapalı bir kümeden kod döndürüyor ve personelin yapacağı
 * şey nedene göre değişir — filigran yoksa beklemek, dosya çözülemiyorsa başkasını
 * yüklemek.
 */
export function statusKey(item: VehicleMediaItem): string {
  return `vehicle.media.status.${item.status}`
}

export function failureKey(item: VehicleMediaItem, locale: AppLocale): string | null {
  if (item.failureReason === null) {
    return null
  }

  const candidate = `vehicle.media.failure.${item.failureReason.toLowerCase()}`

  return catalogKeys(locale).includes(candidate) ? candidate : 'vehicle.media.failure.generic'
}
