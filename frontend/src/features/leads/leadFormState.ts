import { LEAD_TYPES, type CaptureLeadRequest, type LeadType } from '../../shared/api/types'

/**
 * Formun saf mantığı: alan durumu, istemci tarafı ön doğrulama ve gövdeye dönüşüm.
 * Yan etkisizdir ve kendi birim testi vardır (docs/frontend.md § 8.1).
 *
 * Buradaki doğrulama sunucunun doğrulamasının YERİNE GEÇMEZ, ondan önce davranır:
 * ziyaretçiye ağ turu beklemeden geri bildirim verir. Gerçek kural sunucudadır ve
 * ret her zaman oradan gelir (R4, AC-2..AC-6).
 */

export interface LeadFormValues {
  readonly leadType: LeadType
  readonly fullName: string
  readonly phone: string
  readonly email: string
  readonly message: string
  readonly consentGiven: boolean
}

/** Sunucunun `field` parametresiyle aynı adlar: eşleme tek yerde kalsın (AC-40). */
export type LeadFormField = 'fullName' | 'phone' | 'email' | 'message' | 'leadType' | 'locale'

export const EMPTY_LEAD_FORM: LeadFormValues = {
  leadType: LEAD_TYPES[0],
  fullName: '',
  phone: '',
  email: '',
  message: '',
  consentGiven: false,
}

export const MAX_NAME_LENGTH = 120
export const MAX_EMAIL_LENGTH = 254
export const MAX_MESSAGE_LENGTH = 2000

/** E.164: `+` ve ardından 8–15 rakam; ülke kodu `0` ile başlamaz. */
const E164 = /^\+[1-9]\d{7,14}$/

/** Dar ve okunabilir: tek `@`, iki yanında da içerik, alan adında nokta. */
const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

/**
 * İlk hatayı döner, hepsini değil: form tek tek doldurulur ve bir seferde beş uyarı
 * göstermek ziyaretçiye yardımcı olmaz.
 */
export function validate(values: LeadFormValues): { field: LeadFormField; code: string } | null {
  const fullName = values.fullName.trim()

  if (fullName.length === 0) {
    return { field: 'fullName', code: 'lead.missing_name' }
  }

  if (fullName.length > MAX_NAME_LENGTH) {
    return { field: 'fullName', code: 'lead.name_too_long' }
  }

  const phone = values.phone.trim()
  const email = values.email.trim()

  if (phone.length === 0 && email.length === 0) {
    return { field: 'phone', code: 'lead.missing_contact_channel' }
  }

  if (phone.length > 0 && !E164.test(phone)) {
    return { field: 'phone', code: 'lead.invalid_phone' }
  }

  if (email.length > MAX_EMAIL_LENGTH) {
    return { field: 'email', code: 'lead.email_too_long' }
  }

  if (email.length > 0 && !EMAIL.test(email)) {
    return { field: 'email', code: 'lead.invalid_email' }
  }

  if (values.message.trim().length > MAX_MESSAGE_LENGTH) {
    return { field: 'message', code: 'lead.message_too_long' }
  }

  return null
}

/** Boş bırakılan opsiyonel alanlar `null` gider, boş dize değil (R4, R5). */
export function toRequest(
  values: LeadFormValues,
  listingSlug: string,
  locale: string,
): CaptureLeadRequest {
  const phone = values.phone.trim()
  const email = values.email.trim()
  const message = values.message.trim()

  return {
    listingSlug,
    leadType: values.leadType,
    locale,
    fullName: values.fullName.trim(),
    phone: phone.length > 0 ? phone : null,
    email: email.length > 0 ? email : null,
    message: message.length > 0 ? message : null,
    consentGiven: values.consentGiven,
  }
}

/**
 * Hata kodunu çeviri anahtarına çevirir. Bilinmeyen kod için GENEL mesaja düşülür ve
 * kodun kendisi ziyaretçiye ASLA gösterilmez (docs/frontend.md § 3.1, AC-39).
 */
export function messageKeyFor(errorCode: string, catalogKeys: readonly string[]): string {
  const candidate = `lead.error.${errorCode.replace(/^lead\./, '')}`

  return catalogKeys.includes(candidate) ? candidate : 'lead.error.generic'
}
