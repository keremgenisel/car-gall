import { catalogKeys } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'

/**
 * Araç kaydı formunun SAF mantığı. Yan etkisizdir ve kendi birim testi vardır
 * (docs/frontend.md § 8.1).
 */

/**
 * Hata kodunu çeviri anahtarına çevirir. Bilinmeyen kod için GENEL mesaja düşülür ve
 * kodun kendisi personele ASLA gösterilmez (docs/frontend.md § 3.1, AC-47).
 *
 * `vehicle.` öneki düşer; kalan noktalar alt çizgiye döner — `concurrency.lost` modül
 * önekli değil ve `concurrency_lost` olarak kalır.
 *
 * <b>Neden `features/identity` içindekinin kopyası.</b> İki feature birbirinin iç
 * dosyasını import edemez (docs/frontend.md § 1) ve kapı bunu denetliyor. Ortak bir
 * yere terfi ettirmek, kodun kendi modül önekini sıyırmasını genelleştirmeyi
 * gerektirirdi; `identity.` ile `vehicle.` arasında ortak olan tek şey bu önekin ne
 * olduğunu ÇAĞIRANIN bilmesi.
 */
export function vehicleMessageKey(errorCode: string, locale: AppLocale): string {
  const suffix = errorCode.replace(/^vehicle\./, '').replace(/\./g, '_')
  const candidate = `vehicle.form.error.${suffix}`

  return catalogKeys(locale).includes(candidate) ? candidate : 'vehicle.form.error.generic'
}

/**
 * Sunucudan gelen KOD için çeviri anahtarı: `TemporarilyClosed` → `branch.status.temporarily_closed`.
 * Çevrilmiş ad veritabanında saklanmaz (Yasak Listesi 22); dönüşüm uçta yapılır.
 */
export function codeKey(prefix: string, code: string): string {
  const snake = code
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/[\s-]+/g, '_')
    .toLowerCase()

  return `${prefix}.${snake}`
}

export interface VehicleFormValues {
  readonly branchId: string
  readonly vehicleModelId: string
  readonly bodyType: string
  readonly fuelType: string
  readonly transmissionType: string
  readonly drivetrainType: string
  readonly trimLevel: string
  readonly color: string
  readonly modelYear: string
  readonly mileageKilometers: string
  readonly priceAmount: string
  readonly priceOnRequest: boolean
  readonly vin: string
}

export const EMPTY_VEHICLE_FORM: VehicleFormValues = {
  branchId: '',
  vehicleModelId: '',
  bodyType: '',
  fuelType: '',
  transmissionType: '',
  drivetrainType: '',
  trimLevel: '',
  color: '',
  modelYear: '',
  mileageKilometers: '',
  priceAmount: '',
  priceOnRequest: false,
  vin: '',
}

/**
 * R10 — çekirdek alanlar kayıt için zorunludur. Bu denetim SUNUCUNUN yerine geçmez: sunucu
 * aynı isteği zaten reddediyor ve AC-3…AC-8 bunu orada sınıyor. Buradaki iş, personele
 * eksik alanı istek atılmadan göstermek ve boş bir `Guid` dizesini gövdeye koymamak —
 * model kimliği boş giderse bağlayıcı `400` üretir ve `errorCode` taşımayan bir yanıt
 * forma hangi alanın düştüğünü söyleyemez.
 *
 * <b>Renk listede YOK</b> ve bu bilinçli: renk çok dilli metindir ve R10 çok dilli metni
 * "yarıda bırakıp dönmek" muafiyetine açıkça dâhil ediyor. Sunucu da boş rengi kabul
 * ediyor; uçta zorunlu kılmak, olmayan bir kuralı uydurmak olurdu.
 */
const CORE_FIELDS: readonly (keyof VehicleFormValues)[] = [
  'branchId',
  'vehicleModelId',
  'bodyType',
  'fuelType',
  'transmissionType',
  'drivetrainType',
  'trimLevel',
  'modelYear',
  'mileageKilometers',
  'vin',
]

export function firstMissingCoreField(values: VehicleFormValues): string | null {
  for (const field of CORE_FIELDS) {
    if (String(values[field]).trim() === '') {
      return field
    }
  }

  // P4 — "fiyat sorunuz" tutarı olmayan AYRI bir durumdur; işaretli değilse tutar gerekir.
  return !values.priceOnRequest && values.priceAmount.trim() === '' ? 'price' : null
}
