import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'

/**
 * CI kapısı: tr/ar/en katalogları anahtar bazında EŞİT olmalı (docs/testing.md § 3).
 * Eksik anahtar ziyaretçiye boş metin veya ham anahtar gösterir; ikisi de kabul edilemez.
 *
 * ADR `0013` — İKİ katalog kümesi var: vitrin (`locales/*.json`, ilk pakete girer) ve
 * panel (`locales/panel/*.json`, panelin lazy paketiyle iner). Eşitlik HER KÜME İÇİN
 * ayrı ölçülür: vitrin kümesi eşit ama panel kümesi eksikse panel boş metin basar.
 */
const localesDir = new URL('../src/shared/i18n/locales/', import.meta.url).pathname
const srcDir = new URL('../src/', import.meta.url).pathname

function kumeyiOku(dizin) {
  return new Map(
    readdirSync(dizin)
      .filter((name) => name.endsWith('.json'))
      .map((name) => [
        name.replace('.json', ''),
        JSON.parse(readFileSync(join(dizin, name), 'utf8')),
      ]),
  )
}

const kumeler = new Map([
  ['vitrin', kumeyiOku(localesDir)],
  ['panel', kumeyiOku(join(localesDir, 'panel'))],
])

let failed = false
const ozet = []

for (const [kume, catalogs] of kumeler) {
  const allKeys = new Set()
  for (const catalog of catalogs.values()) {
    for (const key of Object.keys(catalog)) {
      allKeys.add(key)
    }
  }

  for (const [locale, catalog] of catalogs) {
    const missing = [...allKeys].filter((key) => !(key in catalog)).sort()
    const empty = Object.entries(catalog)
      .filter(([, value]) => typeof value !== 'string' || value.trim() === '')
      .map(([key]) => key)

    if (missing.length > 0) {
      failed = true
      console.error(`[${kume}/${locale}] eksik anahtar (${missing.length}): ${missing.join(', ')}`)
    }

    if (empty.length > 0) {
      failed = true
      console.error(`[${kume}/${locale}] boş değer (${empty.length}): ${empty.join(', ')}`)
    }
  }

  ozet.push(`${kume}: ${catalogs.size} katalog, ${allKeys.size} anahtar`)
}

// İki kümenin dil listesi aynı olmalı: bir dilin panel kataloğu yoksa panel o dilde boş kalır.
const vitrinDilleri = [...kumeler.get('vitrin').keys()].sort().join(',')
const panelDilleri = [...kumeler.get('panel').keys()].sort().join(',')

if (vitrinDilleri !== panelDilleri) {
  failed = true
  console.error(`dil listeleri farklı — vitrin: ${vitrinDilleri} · panel: ${panelDilleri}`)
}

// Bir anahtar TEK kümede yaşar: ikisinde birden olursa hangisinin kazandığı kayıt sırasına
// bağlı kalır ve iki çeviri sessizce ayrışır.
const panelAnahtarlari = new Set(Object.keys(kumeler.get('panel').get('tr') ?? {}))
const ikiKumede = Object.keys(kumeler.get('vitrin').get('tr') ?? {}).filter((key) =>
  panelAnahtarlari.has(key),
)

if (ikiKumede.length > 0) {
  failed = true
  console.error(`iki kümede birden (${ikiKumede.length}): ${ikiKumede.join(', ')}`)
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

const arapca = { ...kumeler.get('vitrin').get('ar'), ...kumeler.get('panel').get('ar') }

{
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

/**
 * Üçüncü kapı — VİTRİN SINIRI (ADR `0013`). Vitrin `src/main.tsx`'ten STATİK import ile
 * ulaşılan modüllerdir; panel `import()` arkasındadır. İki şey ölçülür:
 *
 * 1. Vitrin panel kataloğunu içe aktarmaz (`shared/i18n/panel.ts`). Aktarırsa panel metinleri
 *    ilk pakete geri döner ve ölçülen LCP payı sessizce kaybolur.
 * 2. Vitrin bir panel anahtarı KULLANMAZ. Kullanırsa anahtar ziyaretçinin çevirmeninde yoktur
 *    ve `t()` BOŞ DİZE döner — hiçbir birim testi bunu görmez, çünkü testler birleşik
 *    kataloğu yükler. Şablon anahtarlar (`lead.type.${type}`) sabit önekleriyle ölçülür.
 */
const girdi = join(srcDir, 'main.tsx')
const panelModulu = join(srcDir, 'shared/i18n/panel.ts')

function coz(kaynak, belirtec) {
  const temel = resolve(dirname(kaynak), belirtec)
  return [temel, `${temel}.ts`, `${temel}.tsx`, join(temel, 'index.ts')].find(
    (aday) => /\.tsx?$/.test(aday) && existsSync(aday),
  )
}

const vitrinModulleri = new Set()
const yigin = [girdi]
const vitrinIhlalleri = []

while (yigin.length > 0) {
  const dosya = yigin.pop()

  if (vitrinModulleri.has(dosya)) {
    continue
  }

  vitrinModulleri.add(dosya)

  // Yorumlar atılır: gerekçe metnindeki bir anahtar adı kullanım değildir.
  const kod = readFileSync(dosya, 'utf8')
    .replace(/\/\*[\s\S]*?\*\//g, '')
    .replace(/^\s*\/\/.*$/gm, '')

  for (const eslesme of kod.matchAll(
    /(?:^|\n)\s*(?:import|export)\s(?:[^;]*?\sfrom\s)?['"](\.[^'"]+)['"]/g,
  )) {
    const hedef = coz(dosya, eslesme[1])

    if (hedef !== undefined) {
      yigin.push(hedef)
    }
  }

  const goreli = dosya.slice(srcDir.length)

  for (const eslesme of kod.matchAll(/['"`]([A-Za-z][\w-]*(?:\.[\w-]+)*\.?)(['"`]|\$\{)/g)) {
    const [, metin, son] = eslesme
    const ihlal =
      son === '${'
        ? metin.includes('.') && [...panelAnahtarlari].find((key) => key.startsWith(metin))
        : panelAnahtarlari.has(metin) && metin

    if (ihlal) {
      vitrinIhlalleri.push(`src/${goreli} → ${son === '${' ? `${metin}\${…}` : metin}`)
    }
  }
}

if (vitrinModulleri.has(panelModulu)) {
  failed = true
  console.error(
    '[vitrin sınırı] src/shared/i18n/panel.ts vitrinin statik import ağacında: panel ' +
      'kataloğu ilk pakete girer. Yalnız panelin lazy paketi (AdminApp) onu içe aktarır.',
  )
}

if (vitrinIhlalleri.length > 0) {
  failed = true
  console.error(
    `[vitrin sınırı] vitrin panel anahtarı kullanıyor (${vitrinIhlalleri.length}) — ziyaretçi ` +
      `boş metin görür; anahtar vitrin kataloğuna (locales/*.json) taşınır:\n  ` +
      vitrinIhlalleri.join('\n  '),
  )
}

// Kapının kendisi çalışıyor mu: girdi bulunamazsa sınır sıfır modülle sessizce "yeşil" olurdu.
if (vitrinModulleri.size < 10) {
  failed = true
  console.error(`[vitrin sınırı] yalnız ${vitrinModulleri.size} modül tarandı; girdi okunamadı.`)
}

if (failed) {
  process.exit(1)
}

console.log(
  `${ozet.join(' · ')} — hepsi eşit. Vitrin sınırı: ${vitrinModulleri.size} modül, panel anahtarı yok.`,
)
