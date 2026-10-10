import type { UpdateVehicleRequest, VehicleAdminDetail } from '../../../shared/api/types'
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
 * <b>Renk de listede</b> (R2, AC-6). Ölçüt "üç dilde de dolu" DEĞİL, personelin kendi
 * dilinde dolu: R10 çok dilli metni yarıda bırakmaya izin veriyor, yani kalan diller
 * düzenleme ekranında (`0010b`) doldurulabilir. Sunucu da aynı ölçütü uyguluyor — bu
 * denetim onun yerine geçmiyor, yalnız istek atılmadan alanı işaretliyor.
 */
const CORE_FIELDS: readonly (keyof VehicleFormValues)[] = [
  'branchId',
  'vehicleModelId',
  'bodyType',
  'fuelType',
  'transmissionType',
  'drivetrainType',
  'trimLevel',
  'color',
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

/**
 * Düzenleme formunun değerleri (0010b R1).
 *
 * <p>İlan metni ve opsiyonel değerler SÖZLÜK olarak tutuluyor: etkin dil sayısı ve alan
 * kümesi kurulumdan geliyor, formda sabit alan listesi yok.</p>
 */
export interface VehicleEditValues extends VehicleFormValues {
  /** locale → başlık. Her etkin dil için bir anahtar; boş dize "yazılmadı" demek. */
  readonly listingTitle: Readonly<Record<string, string>>
  readonly listingDescription: Readonly<Record<string, string>>
  /** fieldKey → ham değer. Locale'li alanlar `locale|değer` değil, ayrı sözlükte. */
  readonly optionalValues: Readonly<Record<string, string>>
  readonly optionalLocalizedValues: Readonly<Record<string, Readonly<Record<string, string>>>>
}

/**
 * Sunucudan gelen kaydı form değerlerine çevirir.
 *
 * <p><b>Sayılar metne çevriliyor</b> çünkü form alanları metin taşır; geri dönüşte
 * `Number` ile ayrıştırılıyor. Tutar sunucudan ZATEN para biriminin ondalık ölçeğiyle
 * geliyor (`9900.00`), yani personel gördüğü değeri olduğu gibi geri gönderiyor.</p>
 */
export function toEditValues(
  detail: VehicleAdminDetail,
  enabledLocales: readonly string[],
): VehicleEditValues {
  const optionalValues: Record<string, string> = {}
  const optionalLocalizedValues: Record<string, Record<string, string>> = {}

  for (const item of detail.optionalValues) {
    if (item.values !== null) {
      optionalLocalizedValues[item.fieldKey] = { ...item.values }
    } else if (item.value !== null) {
      optionalValues[item.fieldKey] = item.value
    }
  }

  return {
    branchId: detail.branchId,
    vehicleModelId: detail.vehicleModelId,
    bodyType: detail.bodyType,
    fuelType: detail.fuelType,
    transmissionType: detail.transmissionType,
    drivetrainType: detail.drivetrainType,
    trimLevel: detail.trimLevel,
    // Renk çok dilli; form personelin KENDİ dilini düzenletiyor, kalanı korunuyor.
    color: '',
    modelYear: String(detail.modelYear),
    mileageKilometers: String(detail.mileageKilometers),
    priceAmount: detail.price.amount === null ? '' : String(detail.price.amount),
    priceOnRequest: detail.price.onRequest,
    vin: detail.vin,
    listingTitle: fillLocales(detail.listingTitle, enabledLocales),
    listingDescription: fillLocales(detail.listingDescription, enabledLocales),
    optionalValues,
    optionalLocalizedValues,
  }
}

/** Etkin her dil için bir anahtar: formda üç kutu da basılmalı (R10). */
function fillLocales(
  values: Record<string, string>,
  enabledLocales: readonly string[],
): Record<string, string> {
  const filled: Record<string, string> = {}

  for (const locale of enabledLocales) {
    filled[locale] = values[locale] ?? ''
  }

  return filled
}

/**
 * Form değerlerini düzenleme isteğine çevirir.
 *
 * <p><b>Renk BİRLEŞTİRİLİR, değiştirilmez:</b> form yalnız personelin kendi dilini
 * düzenletiyor ve diğer dillerdeki renk metni kaybolmamalı. Sunucu rengi tam değiştirme
 * olarak alıyor, yani eksik gönderilen dil SİLİNİR — bu yüzden eski değerler burada
 * korunuyor.</p>
 *
 * <p><b>Boş bırakılan opsiyonel alan gönderilmez</b> (D23): "gelmeyen" dokunulmamış
 * sayılıyor ve kapalı alanın değeri böyle korunuyor (AC-17).</p>
 */
export function toUpdateRequest(
  values: VehicleEditValues,
  existingColor: Readonly<Record<string, string>>,
  locale: string,
): UpdateVehicleRequest {
  const color: Record<string, string> = { ...existingColor }

  if (values.color.trim() !== '') {
    color[locale] = values.color.trim()
  }

  const optionalValues = [
    ...Object.entries(values.optionalValues)
      .filter(([, value]) => value.trim() !== '')
      .map(([fieldKey, value]) => ({ fieldKey, value: value.trim(), values: null })),
    ...Object.entries(values.optionalLocalizedValues)
      .filter(([, map]) => Object.values(map).some((text) => text.trim() !== ''))
      .map(([fieldKey, map]) => ({ fieldKey, value: null, values: { ...map } })),
  ]

  return {
    branchId: values.branchId,
    vehicleModelId: values.vehicleModelId,
    bodyType: values.bodyType,
    fuelType: values.fuelType,
    transmissionType: values.transmissionType,
    drivetrainType: values.drivetrainType,
    trimLevel: values.trimLevel,
    color,
    modelYear: Number(values.modelYear),
    mileageKilometers: Number(values.mileageKilometers),
    priceAmount: values.priceOnRequest ? null : Number(values.priceAmount),
    priceOnRequest: values.priceOnRequest,
    vin: values.vin,
    listingTitle: { ...values.listingTitle },
    listingDescription: { ...values.listingDescription },
    optionalValues,
  }
}

/**
 * R12/AC-21 — bir dil, başlık VE açıklamasının ikisi de doluysa tamdır. Uç bu hesabı
 * KENDİ formunda da yapıyor: personel kaydetmeden önce hangi dilin eksik kalacağını
 * görmeli. Sunucudaki hesap bunun yerine geçmez, ikisi aynı kuralı söyler.
 */
export function missingLocalesOf(
  values: VehicleEditValues,
  enabledLocales: readonly string[],
): readonly string[] {
  return enabledLocales.filter(
    (locale) =>
      (values.listingTitle[locale] ?? '').trim() === '' ||
      (values.listingDescription[locale] ?? '').trim() === '',
  )
}
