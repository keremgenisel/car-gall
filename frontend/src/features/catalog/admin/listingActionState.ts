import { catalogKeys } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'
import type { ListingAction } from '../../../shared/api/types'

/**
 * Yayın eylemlerinin SAF mantığı: hangi eylem hangi durumda görünür, reddin kodu hangi
 * çeviri anahtarına düşer ve eksikler nasıl okunur.
 *
 * <p>Bileşenin içinde karar vermek yerine burada, çünkü hepsi birim testle ölçülebilir
 * (`docs/frontend.md` § 8).</p>
 */

/**
 * **A3**'ün geçiş tablosu, EKRANIN tarafında. Sunucu zaten reddediyor (AC-19) — bu liste
 * reddi önlemek için değil, personele **olmayan bir düğmeyi göstermemek** için var:
 * taslak bir ilanda "rezerve et" düğmesi, sunucunun `409`'una yürüyen bir davettir.
 *
 * <p>Tablo `docs/domain.md` A3'ün aynısı ve ikisinin ayrışması bir kusurdur; aynı
 * ayrışma sunucu tarafında bir testle (`GecisTablosuTests`) kapalı.</p>
 */
const TRANSITIONS: Record<string, readonly ListingAction[]> = {
  Draft: ['publish', 'archive'],
  Published: ['reserve', 'sold', 'unpublish', 'archive'],
  Reserved: ['unreserve', 'sold', 'unpublish'],
  Sold: ['archive'],
  Archived: [],
}

/** İlanın bugünkü durumunda çağrılabilen eylemler. Tanınmayan durum hiçbir eylem vermez. */
export function availableActions(listingStatus: string): readonly ListingAction[] {
  return TRANSITIONS[listingStatus] ?? []
}

/**
 * `0012c` **AC-43** — öne çıkarma bu durumda çağrılabilir mi? Tek ön koşul **arşiv**
 * (**D36**): arşivlenmiş ilan vitrinde değil ve onu öne çıkarmak hiçbir sıralamayı
 * değiştirmez, sunucu da `listing.cannot_feature.archived` ile reddeder.
 *
 * <p>`availableActions`'ın aynı gerekçesi: liste reddi ÖNLEMEK için değil, personele
 * <b>olmayan bir düğmeyi göstermemek</b> için. Arşivlenmiş satırda duran bir "öne çıkar"
 * düğmesi, sunucunun `409`'una yürüyen bir davettir.</p>
 *
 * <p><b>Kaldırma da reddediliyor</b> ve bu bilinçli: koşul ağırlığı YAZAN tek yerde
 * (`Listing.OneCikarmayiYaz`) ve iki yönü de kapsıyor. Yani arşivlenmiş ve öne çıkarılmış
 * bir ilanda rozet durur, düğme durmaz — rozet bir olguyu bildiriyor, düğme ise sunucunun
 * yapmayacağı bir şeyi vaat ederdi.</p>
 */
export function canFeature(listingStatus: string): boolean {
  return listingStatus !== 'Archived'
}

/**
 * **D22** — rol ayrımı YOK: eylemler oturum açmış her personele açık. Bu fonksiyonun
 * varlığı kararın kendisini görünür kılıyor; bir gün rol ayrımı gelirse değişecek tek
 * yer burası olur.
 */
export function canAct(): boolean {
  return true
}

/**
 * Yayın reddinin alt kodları. Sunucu onları `reasons` parametresinde **virgülle
 * ayrılmış** gönderiyor (**D2**, **D24**): liste bir dize olarak geliyor çünkü
 * `BuildingBlocks`'un `Error` tipi yapısal bir dizi taşımıyor ve onu zenginleştirmek
 * Yasak Listesi 11'e girerdi.
 */
export function publishRejectionKeys(
  reasons: string | undefined,
  locale: AppLocale,
): readonly string[] {
  if (reasons === undefined || reasons.length === 0) {
    return ['vehicle.action.error.generic']
  }

  return reasons
    .split(',')
    .map((code) => code.trim())
    .filter((code) => code.length > 0)
    .map((code) => messageKey(code, locale))
}

/**
 * Hata kodunu çeviri anahtarına çevirir. Bilinmeyen kod GENEL mesaja düşer ve kodun
 * kendisi personele ASLA gösterilmez (`docs/frontend.md` § 3.1, AC-55).
 */
export function messageKey(errorCode: string, locale: AppLocale): string {
  const suffix = errorCode.replace(/^(vehicle|listing|concurrency)\./, '').replace(/\./g, '_')
  const candidate = `vehicle.action.error.${suffix}`

  return catalogKeys(locale).includes(candidate) ? candidate : 'vehicle.action.error.generic'
}

/**
 * Eksik alanların anahtarları. Sunucu onları da virgülle ayırıyor (**D24**); uç her
 * anahtarı kendi kataloğundan çeviriyor — alan adı ASLA sunucudan metin olarak gelmez
 * (Yasak Listesi 23).
 */
export function missingFieldKeys(fields: string | undefined): readonly string[] {
  if (fields === undefined || fields.length === 0) {
    return []
  }

  return fields
    .split(',')
    .map((key) => key.trim())
    .filter((key) => key.length > 0)
    .map((key) => `vehicle.field.${key}`)
}
