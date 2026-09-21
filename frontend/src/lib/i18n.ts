import IntlMessageFormat from 'intl-messageformat'
import ar from '../locales/ar.json'
import en from '../locales/en.json'
import tr from '../locales/tr.json'
import { intlLocale, type AppLocale } from './locale'

type Catalog = Record<string, string>

const CATALOGS: Record<AppLocale, Catalog> = { tr, ar, en }

export type TranslationValues = Record<string, string | number>

export type Translator = (key: string, values?: TranslationValues) => string

const cache = new Map<string, IntlMessageFormat>()

/**
 * ICU mesaj formatı ZORUNLUDUR; dize birleştirmeyle cümle kurulmaz. Arapça'da altı çoğul
 * biçimi vardır ve `count > 1` mantığı sessizce yanlış cümle üretir
 * (Yasak Listesi 31, C5).
 */
export function createTranslator(locale: AppLocale): Translator {
  const catalog = CATALOGS[locale]

  return (key, values) => {
    const message = catalog[key]

    if (message === undefined) {
      // Ham anahtar ziyaretçiye GÖSTERİLMEZ (AC-29, AC-32); eksik anahtar CI'da kırılır.
      return ''
    }

    const cacheKey = `${locale}:${key}`
    let formatter = cache.get(cacheKey)

    if (formatter === undefined) {
      formatter = new IntlMessageFormat(message, intlLocale(locale))
      cache.set(cacheKey, formatter)
    }

    return String(formatter.format(values ?? {}))
  }
}

export function catalogKeys(locale: AppLocale): string[] {
  return Object.keys(CATALOGS[locale])
}
