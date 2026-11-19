import IntlMessageFormat from 'intl-messageformat'
import ar from './locales/ar.json'
import en from './locales/en.json'
import tr from './locales/tr.json'
import { intlLocale, type AppLocale } from '../lib/locale'

type Catalog = Record<string, string>

/**
 * Vitrin kataloğu ilk pakete girer; panel kataloğu (`./panel.ts`) panelin lazy paketiyle
 * iner ve buraya EKLENİR (ADR `0013`). Kopyalar değiştirilebilir olsun diye: JSON modülünün
 * kendi nesnesine yazılmaz.
 */
const CATALOGS: Record<AppLocale, Catalog> = { tr: { ...tr }, ar: { ...ar }, en: { ...en } }

/**
 * Panel kataloğunu vitrin kataloğunun üstüne ekler. Çevirmenler kataloğu ÇAĞRI ANINDA
 * okuduğu için kayıttan önce kurulmuş bir çevirmen (`App`'in `LocaleProvider`'ı) panel
 * anahtarlarını da görür. Aynı anahtarın iki katalogda birden olması `check:locales`'te kırılır.
 */
export function registerCatalogs(extra: Readonly<Record<AppLocale, Catalog>>): void {
  for (const locale of Object.keys(CATALOGS) as AppLocale[]) {
    Object.assign(CATALOGS[locale], extra[locale])
  }
}

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
