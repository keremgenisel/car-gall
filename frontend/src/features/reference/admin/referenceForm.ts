import { ApiError } from '../../../shared/api/client'
import { catalogKeys } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'

/**
 * `0021` — marka ve model ekranlarının SAF mantığı: sunucu reddini alana bağlama, hata kodunu
 * çeviri anahtarına çevirme, görünen ad gövdesi. Yan etkisizdir ve kendi birim testi vardır
 * (docs/frontend.md § 8.1).
 */

/** Ekranın bir reddi nasıl göstereceği: alanın altında, çakışma şeridinde ya da genel şeritte. */
export type Rejection =
  | { readonly kind: 'field'; readonly field: string; readonly messageKey: string }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * Sunucunun reddini okur. Alan kodu (`brand.name_taken`, `field: canonicalName`) katalogdaki
 * karşılığına çevrilir; karşılığı yoksa genel alan hatasına düşer — kodun kendisi ASLA
 * gösterilmez (docs/frontend.md § 3.1). Eski sürüm (`concurrency.lost`) ayrı bir şerittir:
 * güncel hâli yükleme yolu sunulur.
 */
export function rejectionOf(cause: unknown, locale: AppLocale): Rejection {
  if (!(cause instanceof ApiError)) {
    return { kind: 'failed', messageKey: 'reference.error.generic' }
  }

  if (cause.errorCode === 'concurrency.lost') {
    return { kind: 'conflict' }
  }

  if (cause.field !== null) {
    return { kind: 'field', field: cause.field, messageKey: fieldErrorKey(cause.errorCode, locale) }
  }

  return { kind: 'failed', messageKey: 'reference.error.generic' }
}

export function fieldErrorKey(code: string, locale: AppLocale): string {
  const candidate = `reference.fieldError.${code.replace(/\./g, '_')}`
  return catalogKeys(locale).includes(candidate) ? candidate : 'reference.fieldError.generic'
}

/**
 * Görünen adların gövdesi: etkin HER dil gönderilir ve boş değer boş metin olarak gider —
 * sunucu o dilin adını siler ve o dilde kanonik ad gösterilir (R3). Yalnız dolu dilleri
 * göndermek, yöneticinin sildiği adı sessizce yerinde bırakırdı.
 */
export function displayNamesPayload(
  values: Readonly<Record<string, string>>,
  locales: readonly string[],
): Readonly<Record<string, string>> {
  return Object.fromEntries(locales.map((code) => [code, (values[code] ?? '').trim()]))
}

/** Kayıtlı görünen adlardan formun başlangıç değerleri: etkin her dil bir alan. */
export function displayNamesOf(
  stored: Readonly<Record<string, string>>,
  locales: readonly string[],
): Readonly<Record<string, string>> {
  return Object.fromEntries(locales.map((code) => [code, stored[code] ?? '']))
}
