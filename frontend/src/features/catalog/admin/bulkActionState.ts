import type { AppLocale } from '../../../shared/lib/locale'
import type { BulkListingRow } from '../../../shared/api/types'
import { messageKey, missingFieldKeys, publishRejectionKeys } from './listingActionState'

/**
 * Toplu eylemin SAF mantığı: sonuç listesi nasıl okunur, hangi ekran durumu doğar ve
 * başarısız satır hangi çeviri anahtarlarına düşer.
 *
 * <p>Bileşenin içinde karar vermek yerine burada, çünkü hepsi birim testle ölçülebilir
 * (`docs/frontend.md` § 8). Kısmi başarı kararının tamamı bu dosyada yaşıyor.</p>
 */

/**
 * `0012c` **D54** — **eylem sonucunun** iki durumu bu spec'in borcu. Listenin
 * yükleniyor/boş/hata durumları `0010b`'de kurulu ve burada yeniden tanımlanmıyor.
 */
export type BulkOutcome = 'success' | 'partial'

/** Başarısız bir satırın ekranda görünen hâli: hangi araç, hangi gerekçeler. */
export interface BulkFailure {
  readonly vehicleId: string
  /** Kaydın güncel durum kodu; bulunamayan araçta `null` (**D45**). */
  readonly listingStatus: string | null
  /** Çeviri anahtarları — kodun kendisi personele ASLA gösterilmez (§ 3.1). */
  readonly messageKeys: readonly string[]
  /** Eksik alan anahtarları; yayın reddinde dolu olur. */
  readonly fieldKeys: readonly string[]
}

export interface BulkSummary {
  readonly outcome: BulkOutcome
  readonly appliedCount: number
  readonly failures: readonly BulkFailure[]
}

/**
 * Sonuç listesini ekranın okuyabileceği hâle getirir.
 *
 * <p><b>Yuvarlama YOK</b> (`docs/frontend.md` § 3): yirmi satırın on yedisi uygulandıysa
 * sonuç "başarılı" değildir. Kısmi başarı kendi durumudur ve uygulanmayanlar tek tek
 * gerekçesiyle listelenir.</p>
 *
 * <p>Boş liste <b>başarılı</b> sayılıyor ve bu bir varsayım değil sözleşme: boş seçim
 * istek düzeyinde reddediliyor (**D5**), yani buraya hiç boş liste gelmez.</p>
 */
export function summarizeBulkResult(
  rows: readonly BulkListingRow[],
  locale: AppLocale,
): BulkSummary {
  const failures = rows.filter((row) => !row.applied).map((row) => toFailure(row, locale))

  return {
    outcome: failures.length === 0 ? 'success' : 'partial',
    appliedCount: rows.length - failures.length,
    failures,
  }
}

/**
 * Başarısız satırın gerekçesi, tekil uçtakinin **aynısı** (**D45**). Yayın reddi tek bir
 * hata ve gerekçeleri `reasons` parametresinde; panel zaten bu çeviriciyi kullanıyor ve
 * toplu yüzey için yeni bir çeviri anahtarı doğmuyor.
 */
function toFailure(row: BulkListingRow, locale: AppLocale): BulkFailure {
  const code = row.errorCode ?? 'api.unavailable'

  return {
    vehicleId: row.vehicleId,
    listingStatus: row.listingStatus,
    messageKeys:
      code === 'vehicle.cannot_publish'
        ? publishRejectionKeys(row.parameters?.['reasons'], locale)
        : [messageKey(code, locale)],
    fieldKeys: missingFieldKeys(row.parameters?.['fields']),
  }
}

/**
 * Seçilen araçların kimliği — satır sırasında. Sıra korunuyor çünkü sunucu satırları
 * **gönderildiği sırada** işliyor (**D47**) ve sonuç listesi aynı sırayı taşıyor; ekranın
 * seçim sırası ile sunucunun işleme sırası ayrışırsa kısmi başarı listesi okunamaz hâle
 * gelir.
 */
export function selectionOf(
  vehicleIds: readonly string[],
  selected: ReadonlySet<string>,
): readonly string[] {
  return vehicleIds.filter((id) => selected.has(id))
}

/**
 * `0012c` **D43** — seçim **sayfaya aittir**. Sayfadaki araçların dışında kalan hiçbir
 * kimlik seçimde kalamaz; bu fonksiyon seçim kümesini sayfaya **kırpıyor**.
 *
 * <p>Gerekçe kararın kendisi: üst sınır zaten sayfanın üst sınırı (**D4**) ve sayfalar
 * arası biriken bir seçim personelin <b>görmediği</b> araçlara eylem uygular.</p>
 */
export function retainVisible(
  selected: ReadonlySet<string>,
  vehicleIds: readonly string[],
): ReadonlySet<string> {
  const görünür = new Set(vehicleIds)

  return new Set([...selected].filter((id) => görünür.has(id)))
}
