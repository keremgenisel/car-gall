import { catalogKeys } from '../../shared/i18n/i18n'
import type { AppLocale } from '../../shared/lib/locale'

/**
 * İkinci adım ekranlarının saf mantığı. Yan etkisizdir ve kendi birim testi vardır
 * (docs/frontend.md § 8.1).
 */

/**
 * Girilen değerin türü. Tek bir alan iki şey kabul ediyor ve ne beklendiğini **ekran**
 * söylüyor: kod altı hane, kurtarma kodu gruplanmış bir dize. Tek moda sıkıştırmak,
 * yanlış biçimi "geçersiz kod" diye göstermek olurdu — oysa personelin yapacağı şey
 * farklı.
 */
export type TwoFactorInput = 'code' | 'recovery'

/** D16 — hane sayısı DONDURULMUŞ bir sözleşmedir; uç da onu bilir. */
export const CODE_LENGTH = 6

/**
 * Uç tarafı doğrulama. Biçimi bozuk bir değer için istek GÖNDERİLMEZ: gönderilse
 * paylaşılan kilitlenme sayacını (R8) bir yazım hatası yüzünden tüketirdi.
 */
export function twoFactorInputError(input: TwoFactorInput, value: string): string | null {
  const trimmed = value.trim()

  if (trimmed.length === 0) {
    return 'login.twoFactor.error.required'
  }

  if (input === 'code' && !new RegExp(`^[0-9]{${CODE_LENGTH}}$`).test(trimmed)) {
    return 'login.twoFactor.error.format'
  }

  return null
}

/**
 * D20 — ara durumun düşmesi ayrı bir koddur ve ekranın yapacağı şey farklıdır: personel
 * **parola fazına döner**. Kodun kendi penceresinin dolması bir hata durumudur ve taze
 * bir kod istenir.
 */
export function isPendingExpired(errorCode: string): boolean {
  return errorCode === 'identity.two_factor_pending_expired'
}

/**
 * R15/D23 — kapatma iki şey ister ve ikisi de eksiksiz olmadan istek gönderilmez.
 * Zorunlu rolde kapatma hiç yoktur; ekran o düğmeyi basmaz ve sunucu da reddeder.
 */
export function disableFormError(currentPassword: string, code: string): string | null {
  if (currentPassword.length === 0) {
    return 'twoFactor.disable.error.password'
  }

  return twoFactorInputError('code', code) === null ? null : 'twoFactor.disable.error.code'
}

/**
 * Durum rozetinin anahtarı. Üç hâl var ve ikisi aynı veriden türüyor: zorunluluğu ROL
 * söylüyor (D25), kurulumu `twoFactorEnrolled`. *"Zorunlu ama kurmamış"* ekranın
 * elindeki iki alandan kurduğu üçüncü hâldir.
 */
export function statusBadgeKey(enrolled: boolean, required: boolean): string {
  if (enrolled) {
    return 'twoFactor.status.enrolled'
  }

  return required ? 'twoFactor.status.missing' : 'twoFactor.status.optional'
}

/**
 * Hata kodunu çeviri anahtarına çevirir ve bilinmeyen kodu GENEL mesaja düşürür.
 * `loginFormState.messageKeyFor` ile aynı sözleşme; burada yeniden yazılmıyor, yalnız
 * ikinci adımın öneki veriliyor.
 */
export function twoFactorMessageKey(errorCode: string, prefix: string, locale: AppLocale): string {
  const suffix = errorCode.replace(/^identity\./, '').replace(/\./g, '_')
  const candidate = `${prefix}.${suffix}`

  return catalogKeys(locale).includes(candidate) ? candidate : `${prefix}.generic`
}
