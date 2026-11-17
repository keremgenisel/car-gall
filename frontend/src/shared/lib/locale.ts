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

/**
 * 0014 **D22** — kurulumda ETKİN locale'ler. Sunucu yapılandırmadaki listeyi
 * `<meta name="enabled-locales">` olarak basar; uç dil listesini koda gömülü üç dilden
 * değil buradan türetir. İki dilli bir kurulumda üçüncü dile bağlantı 404'e giderdi (D14).
 *
 * Etiket yoksa (geliştirme sunucusu, birim testi) desteklenen dillerin tamamı döner:
 * bugünkü davranış budur ve sunucusuz ortamda başka bir kaynak yoktur.
 */
export function enabledLocales(doc: Document = document): readonly AppLocale[] {
  const content = doc.querySelector('meta[name="enabled-locales"]')?.getAttribute('content')

  if (content === null || content === undefined) {
    return SUPPORTED_LOCALES
  }

  const listed = content.split(/\s+/).filter(isAppLocale)
  return listed.length === 0 ? SUPPORTED_LOCALES : listed
}
