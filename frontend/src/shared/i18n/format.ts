import { intlLocale, type AppLocale } from '../lib/locale'

export interface MoneyView {
  readonly onRequest: boolean
  readonly amount: number | null
  readonly currencyCode: string | null
  readonly minorUnitExponent: number | null
}

/**
 * Biçimlendirme UÇTA yapılır; sunucu ham sayı + para birimi kodu döner
 * (docs/conventions.md § 7). Ondalık basamak sayısı minor unit üssünden gelir:
 * KWD üç ondalıklıdır ve iki ondalıkla göstermek sessiz bir hatadır (AC-22).
 */
export function formatMoney(money: MoneyView, locale: AppLocale): string | null {
  if (money.onRequest || money.amount === null || money.currencyCode === null) {
    return null
  }

  const fractionDigits = money.minorUnitExponent ?? 0

  return new Intl.NumberFormat(intlLocale(locale), {
    style: 'currency',
    currency: money.currencyCode,
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(money.amount)
}

/** Kilometre locale'e göre gruplama ayracı alır: TR 125.000 · EN 125,000 (AC-20). */
export function formatMileage(kilometers: number, locale: AppLocale): string {
  return new Intl.NumberFormat(intlLocale(locale)).format(kilometers)
}

/** Yıl hiçbir locale'de gruplama ayracı almaz: 2019, "2.019" değil (AC-20). */
export function formatYear(year: number, locale: AppLocale): string {
  return new Intl.NumberFormat(intlLocale(locale), { useGrouping: false }).format(year)
}

/**
 * Son güncelleme zamanı (0010b R13). Sunucu ISO 8601 döner; biçimlendirme UÇTA yapılır
 * (`docs/conventions.md` § 7).
 *
 * <p>Tarih ve saat BİRLİKTE: "bugün 14:05" ile "bugün" arasındaki fark, aynı gün iki kez
 * düzenlenen bir araçta personelin tek ipucu.</p>
 */
export function formatDateTime(iso: string, locale: AppLocale): string {
  const value = new Date(iso)

  // Geçersiz tarih ziyaretçiye `Invalid Date` olarak GÖSTERİLMEZ; boş metin, yanlış
  // metinden iyidir ve sütun zaten ikincil bilgi taşıyor.
  if (Number.isNaN(value.getTime())) {
    return ''
  }

  return new Intl.DateTimeFormat(intlLocale(locale), {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(value)
}
