import { readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

/**
 * CI kapısı: tr/ar/en katalogları anahtar bazında EŞİT olmalı (docs/testing.md § 3).
 * Eksik anahtar ziyaretçiye boş metin veya ham anahtar gösterir; ikisi de kabul edilemez.
 */
const localesDir = new URL('../src/shared/i18n/locales/', import.meta.url).pathname
const files = readdirSync(localesDir).filter((name) => name.endsWith('.json'))

const catalogs = new Map(
  files.map((name) => [
    name.replace('.json', ''),
    JSON.parse(readFileSync(join(localesDir, name), 'utf8')),
  ]),
)

const allKeys = new Set()
for (const catalog of catalogs.values()) {
  for (const key of Object.keys(catalog)) {
    allKeys.add(key)
  }
}

let failed = false

for (const [locale, catalog] of catalogs) {
  const missing = [...allKeys].filter((key) => !(key in catalog)).sort()
  const empty = Object.entries(catalog)
    .filter(([, value]) => typeof value !== 'string' || value.trim() === '')
    .map(([key]) => key)

  if (missing.length > 0) {
    failed = true
    console.error(`[${locale}] eksik anahtar (${missing.length}): ${missing.join(', ')}`)
  }

  if (empty.length > 0) {
    failed = true
    console.error(`[${locale}] boş değer (${empty.length}): ${empty.join(', ')}`)
  }
}

/**
 * İkinci kapı: **Arapça'da altı çoğul biçimi var** (`docs/frontend.md` § 5,
 * `docs/conventions.md` § 11 · C5). Anahtar kümelerini karşılaştırmak bunu GÖRMEZ —
 * bir dış bulgu tam buradan geçti: `vehicle.bulk.success.*`'ın beşi ve kısmi başarı
 * paneli yalnız `other` taşıyordu, yani 1, 2 ve 3-10 için dilbilgisi hatalı bir cümle
 * kuruyordu. "386 anahtar eşit" diyen kapı yeşildi.
 *
 * İstenen biçimler `one`, `two`, `few`, `many`, `other`. `zero` ve `=0` isteğe bağlı:
 * sıfır için ayrı bir cümle kurmak bir ÜSLUP kararı, dilbilgisi zorunluluğu değil.
 */
const ZORUNLU_BICIMLER = ['one', 'two', 'few', 'many', 'other']

/** Bir mesajdaki her `plural` argümanının tanımladığı biçim adları. */
function pluralBicimleri(message) {
  const bicimler = []

  for (
    let index = message.indexOf('plural');
    index !== -1;
    index = message.indexOf('plural', index + 1)
  ) {
    const virgul = message.indexOf(',', index)

    if (virgul === -1) {
      continue
    }

    // Argümanın gövdesi: `plural,` sonrasından onu açan süslü parantezin eşine kadar.
    let derinlik = 1
    let konum = virgul + 1
    const secililer = []
    let sozcuk = ''

    while (konum < message.length && derinlik > 0) {
      const karakter = message[konum]

      if (karakter === '{') {
        if (derinlik === 1 && sozcuk.trim() !== '') {
          secililer.push(sozcuk.trim())
        }

        sozcuk = ''
        derinlik += 1
      } else if (karakter === '}') {
        derinlik -= 1
        sozcuk = ''
      } else if (derinlik === 1) {
        sozcuk += karakter
      }

      konum += 1
    }

    bicimler.push(secililer)
  }

  return bicimler
}

const arapca = catalogs.get('ar')

if (arapca !== undefined) {
  const eksikBicimler = []

  for (const [key, value] of Object.entries(arapca)) {
    if (typeof value !== 'string' || !value.includes('plural')) {
      continue
    }

    for (const secililer of pluralBicimleri(value)) {
      const eksik = ZORUNLU_BICIMLER.filter((bicim) => !secililer.includes(bicim))

      if (eksik.length > 0) {
        eksikBicimler.push(`${key} → eksik: ${eksik.join(', ')}`)
      }
    }
  }

  if (eksikBicimler.length > 0) {
    failed = true
    console.error(
      `[ar] çoğul biçimi eksik (${eksikBicimler.length}): Arapça'da altı çoğul biçimi var ` +
        `ve yalnız "other" taşıyan bir mesaj 1, 2 ve 3-10 için yanlış cümle kurar:\n  ` +
        eksikBicimler.join('\n  '),
    )
  }
}

if (failed) {
  process.exit(1)
}

console.log(`${catalogs.size} katalog, ${allKeys.size} anahtar — hepsi eşit.`)
