export const SUPPORTED_LOCALES = ['tr', 'ar', 'en'] as const

export type AppLocale = (typeof SUPPORTED_LOCALES)[number]

export function isAppLocale(value: string): value is AppLocale {
  return (SUPPORTED_LOCALES as readonly string[]).includes(value)
}

export function directionOf(locale: AppLocale): 'ltr' | 'rtl' {
  return locale === 'ar' ? 'rtl' : 'ltr'
}

/**
 * Rakam sistemi AÇIKÇA sabitlenir. `ar` locale'inin ICU varsayılanı ortama göre
 * Hint-Arap rakamı (١٢٥٠) olabilir; AC-22 üç locale'de de Batı Arap rakamı istiyor.
 * Varsayılana güvenmek, hatayı yalnızca belirli bir tarayıcıda görünür kılardı.
 */
export function intlLocale(locale: AppLocale): string {
  return `${locale}-u-nu-latn`
}

/**
 * Locale URL yol önekinden gelir (/tr, /ar, /en). Accept-Language ile otomatik
 * yönlendirme YAPILMAZ (Yasak Listesi 28, docs/frontend.md § 5).
 */
export function localeFromPath(pathname: string): AppLocale | null {
  const first = pathname.replace(/^\//, '').split('/')[0] ?? ''
  return isAppLocale(first) ? first : null
}
