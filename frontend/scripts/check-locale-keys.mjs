import { existsSync, readFileSync, readdirSync } from 'node:fs'
import { dirname, join, resolve } from 'node:path'
import ts from 'typescript'

/**
 * CI kapısı: tr/ar/en katalogları anahtar bazında EŞİT olmalı (docs/testing.md § 3).
 * Eksik anahtar ziyaretçiye boş metin veya ham anahtar gösterir; ikisi de kabul edilemez.
 *
 * ADR `0013` — katalog kümeleri: vitrin (`locales/*.json`, ilk pakete girer), panel
 * (`locales/panel/*.json`, panelin lazy paketiyle iner), SEO (Host okur) ve `frontend-003` **D13**
 * ile favoriler (`locales/favorites/*.json`, favoriler ve karşılaştırma ekranlarının tembel
 * parçasıyla iner). Eşitlik HER KÜME İÇİN ayrı ölçülür: vitrin kümesi eşit ama tembel bir küme
 * eksikse o ekran boş metin basar.
 *
 * `frontend-003` **B3** — tembel her küme dört denetimin HER BİRİNE girer: (1) üç dil eşitliği,
 * (2) Arapça çoğul biçimleri, (3) başka kümede tekrar yok, (4) vitrin sınırı (küme modülü ve
 * anahtarları vitrinin statik ağacından ulaşılmaz). Küme adı yalnız `TEMBEL_KUMELER`'e eklenir.
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
  // `0004` **D12** · **P2** — belge başlığı ve açıklamasının SEO'ya özgü metinleri. Ön yüz onları
  // hiç içe aktarmaz (vitrin paketine girmez); Host dağıtım paketinde okur.
  ['seo', kumeyiOku(join(localesDir, 'seo'))],
  // `frontend-003` D13 — favoriler ve karşılaştırma ekranlarının metinleri.
  ['favoriler', kumeyiOku(join(localesDir, 'favorites'))],
])

/** İlk pakete girmeyen ve ön yüzün tembel bir parçayla indirdiği kümeler; kayıt modülleriyle. */
const TEMBEL_KUMELER = new Map([
  ['panel', 'shared/i18n/panel.ts'],
  ['favoriler', 'shared/i18n/favoritesCatalog.ts'],
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

for (const kume of ['panel', 'seo', 'favoriler']) {
  const diller = [...kumeler.get(kume).keys()].sort().join(',')

  if (vitrinDilleri !== diller) {
    failed = true
    console.error(`dil listeleri farklı — vitrin: ${vitrinDilleri} · ${kume}: ${diller}`)
  }
}

// `0004` D12 — SEO kümesi yalnız `seo.` önekli anahtar taşır ve başka kümede tekrarlanmaz: ekranda
// zaten görünen metin mevcut vitrin anahtarından okunur, kopyası yazılmaz.
{
  const seoAnahtarlari = Object.keys(kumeler.get('seo').get('tr') ?? {})
  const digerleri = new Set(
    ['vitrin', ...TEMBEL_KUMELER.keys()].flatMap((kume) =>
      Object.keys(kumeler.get(kume).get('tr') ?? {}),
    ),
  )
  const onekDisi = seoAnahtarlari.filter((key) => !key.startsWith('seo.'))
  const tekrar = seoAnahtarlari.filter((key) => digerleri.has(key))
  const disaridaOnekli = [...digerleri].filter((key) => key.startsWith('seo.'))

  if (onekDisi.length + tekrar.length + disaridaOnekli.length > 0) {
    failed = true
    console.error(
      `seo kümesi — önek dışı: ${onekDisi.join(', ')} · başka kümede de var: ${tekrar.join(', ')} · ` +
        `başka kümede seo. önekli: ${disaridaOnekli.join(', ')}`,
    )
  }
}

// Bir anahtar TEK kümede yaşar: ikisinde birden olursa hangisinin kazandığı kayıt sırasına
// bağlı kalır ve iki çeviri sessizce ayrışır.
// `frontend-003` B3 — vitrin ve bütün tembel kümeler çift çift karşılaştırılır.
const ekranKumeleri = ['vitrin', ...TEMBEL_KUMELER.keys()]

for (const [index, birinci] of ekranKumeleri.entries()) {
  const anahtarlar = new Set(Object.keys(kumeler.get(birinci).get('tr') ?? {}))

  for (const ikinci of ekranKumeleri.slice(index + 1)) {
    const ikiKumede = Object.keys(kumeler.get(ikinci).get('tr') ?? {}).filter((key) =>
      anahtarlar.has(key),
    )

    if (ikiKumede.length > 0) {
      failed = true
      console.error(
        `iki kümede birden — ${birinci} ve ${ikinci} (${ikiKumede.length}): ${ikiKumede.join(', ')}`,
      )
    }
  }
}

/** Vitrinin KULLANAMAYACAĞI anahtarlar: tembel kümelerin hepsi (vitrin sınırı aşağıda). */
const tembelAnahtarlar = new Set(
  [...TEMBEL_KUMELER.keys()].flatMap((kume) => Object.keys(kumeler.get(kume).get('tr') ?? {})),
)

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

const arapca = Object.assign({}, ...ekranKumeleri.map((kume) => kumeler.get(kume).get('ar') ?? {}))

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
 *    kataloğu yükler. Şablon anahtarlar (`lead.type.${type}`) sabit önekleriyle, `.` ile
 *    biten dizgeler (`'paintChange.input.' + part`) önek olarak ölçülür.
 *
 * Kaynak `typescript`'in ayrıştırıcısıyla okunur, düzenli ifadeyle DEĞİL: proje noktalı
 * virgül kullanmıyor ve ifade bir yan etki importunu (`import './panel'`) sonraki satırın
 * `from`'uyla birleştirip kaçırıyordu (QA BULGU-1, #109). Yorumlar ayrıştırıcıda zaten
 * düğüm değildir. Kapının hâlâ görmediği biçimler ADR `0013` § Kapı'da.
 */
const girdi = join(srcDir, 'main.tsx')

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

  const kaynak = ts.createSourceFile(
    dosya,
    readFileSync(dosya, 'utf8'),
    ts.ScriptTarget.Latest,
    true,
    dosya.endsWith('.tsx') ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
  )
  const goreli = dosya.slice(srcDir.length)

  const onekIhlali = (onek) =>
    onek.includes('.') && [...tembelAnahtarlar].find((key) => key.startsWith(onek))

  const dolas = (dugum) => {
    // Statik import/export: `import x from`, `import './yan-etki'`, `export … from`.
    // `import type` çalışma zamanında silinir; `import()` bir çağrıdır, bu dala girmez.
    if (
      (ts.isImportDeclaration(dugum) || ts.isExportDeclaration(dugum)) &&
      dugum.moduleSpecifier !== undefined &&
      ts.isStringLiteral(dugum.moduleSpecifier) &&
      !(ts.isImportDeclaration(dugum) && dugum.importClause?.isTypeOnly) &&
      !(ts.isExportDeclaration(dugum) && dugum.isTypeOnly)
    ) {
      const belirtec = dugum.moduleSpecifier.text

      if (belirtec.startsWith('.')) {
        const hedef = coz(dosya, belirtec)

        if (hedef !== undefined) {
          yigin.push(hedef)
        }
      }

      return
    }

    if (ts.isStringLiteral(dugum) || ts.isNoSubstitutionTemplateLiteral(dugum)) {
      const metin = dugum.text
      const ihlal = metin.endsWith('.') ? onekIhlali(metin) : tembelAnahtarlar.has(metin)

      if (ihlal) {
        vitrinIhlalleri.push(`src/${goreli} → ${metin.endsWith('.') ? `${metin}…` : metin}`)
      }
    } else if (ts.isTemplateExpression(dugum)) {
      if (onekIhlali(dugum.head.text)) {
        vitrinIhlalleri.push(`src/${goreli} → ${dugum.head.text}\${…}`)
      }
    }

    ts.forEachChild(dugum, dolas)
  }

  dolas(kaynak)
}

for (const [kume, modul] of TEMBEL_KUMELER) {
  if (vitrinModulleri.has(join(srcDir, modul))) {
    failed = true
    console.error(
      `[vitrin sınırı] src/${modul} vitrinin statik import ağacında: ${kume} kataloğu ilk ` +
        'pakete girer. Yalnız o kümenin tembel parçası onu içe aktarır.',
    )
  }
}

if (vitrinIhlalleri.length > 0) {
  failed = true
  console.error(
    `[vitrin sınırı] vitrin tembel bir kümenin anahtarını kullanıyor (${vitrinIhlalleri.length}) — ` +
      `ziyaretçi boş metin görür; anahtar vitrin kataloğuna (locales/*.json) taşınır:\n  ` +
      vitrinIhlalleri.join('\n  '),
  )
}

// Kapının kendisi çalışıyor mu: girdi bulunamazsa sınır sıfır modülle sessizce "yeşil" olurdu.
if (vitrinModulleri.size < 10) {
  failed = true
  console.error(`[vitrin sınırı] yalnız ${vitrinModulleri.size} modül tarandı; girdi okunamadı.`)
}

// `0004` D17 (b) — SEO kümesini hiçbir ön yüz modülü içe aktarmaz; aktarırsa metinleri vitrinin
// ilk paketine girer. Yorum dışındaki import belirteçleri ayrıştırıcıyla okunur.
{
  const seoIthalatlari = []

  for (const dosya of readdirSync(srcDir, { recursive: true })) {
    if (!/\.tsx?$/.test(dosya)) {
      continue
    }

    const kaynak = ts.createSourceFile(
      dosya,
      readFileSync(join(srcDir, dosya), 'utf8'),
      ts.ScriptTarget.Latest,
    )

    for (const dugum of kaynak.statements) {
      const belirtec =
        (ts.isImportDeclaration(dugum) || ts.isExportDeclaration(dugum)) && dugum.moduleSpecifier
          ? dugum.moduleSpecifier.text
          : undefined

      if (belirtec?.includes('locales/seo')) {
        seoIthalatlari.push(`src/${dosya} → ${belirtec}`)
      }
    }
  }

  if (seoIthalatlari.length > 0) {
    failed = true
    console.error(
      `seo kümesi ön yüzde içe aktarılıyor (${seoIthalatlari.length}):\n  ${seoIthalatlari.join('\n  ')}`,
    )
  }
}

/**
 * `frontend-003` **D19** — anahtar VARLIĞI. `src` altındaki her `t('…')` çağrısının sabit anahtarı
 * en az bir kümede bulunur; şablonlu anahtarın (`` t(`app.locale.${x}`) ``) sabit önekiyle başlayan
 * en az bir anahtar vardır. Koşullu ifadenin (`t(a ? 'x' : 'y')`) iki dalı da ölçülür. Eksik
 * anahtar ziyaretçiye BOŞ metin gösterir ve hiçbir birim testi bunu görmez (`0025`'in boş
 * `aria-label`'ı). Test dosyaları da taranır: olmayan anahtarı arayan bir test, olmayan metni
 * doğrular.
 */
{
  const butunAnahtarlar = new Set(
    [...kumeler.values()].flatMap((catalogs) => Object.keys(catalogs.get('tr') ?? {})),
  )
  const eksikler = []

  const anahtarlar = (dugum) => {
    if (ts.isStringLiteral(dugum) || ts.isNoSubstitutionTemplateLiteral(dugum)) {
      return [{ anahtar: dugum.text, onek: false }]
    }

    // Sabit öneki olmayan şablon (`` t(`${prefix}.x`) ``) statik olarak ölçülemez; atlanır.
    if (ts.isTemplateExpression(dugum)) {
      return dugum.head.text === '' ? [] : [{ anahtar: dugum.head.text, onek: true }]
    }

    if (ts.isConditionalExpression(dugum)) {
      return [...anahtarlar(dugum.whenTrue), ...anahtarlar(dugum.whenFalse)]
    }

    if (ts.isParenthesizedExpression(dugum)) {
      return anahtarlar(dugum.expression)
    }

    return []
  }

  for (const dosya of readdirSync(srcDir, { recursive: true })) {
    if (!/\.tsx?$/.test(dosya)) {
      continue
    }

    const kaynak = ts.createSourceFile(
      dosya,
      readFileSync(join(srcDir, dosya), 'utf8'),
      ts.ScriptTarget.Latest,
      true,
      dosya.endsWith('.tsx') ? ts.ScriptKind.TSX : ts.ScriptKind.TS,
    )

    const dolas = (dugum) => {
      if (
        ts.isCallExpression(dugum) &&
        ts.isIdentifier(dugum.expression) &&
        dugum.expression.text === 't' &&
        dugum.arguments.length > 0
      ) {
        for (const { anahtar, onek } of anahtarlar(dugum.arguments[0])) {
          const var_ = onek
            ? [...butunAnahtarlar].some((key) => key.startsWith(anahtar))
            : butunAnahtarlar.has(anahtar)

          if (!var_) {
            const { line } = kaynak.getLineAndCharacterOfPosition(dugum.getStart())
            eksikler.push(`src/${dosya}:${line + 1} → ${onek ? `${anahtar}\${…}` : anahtar}`)
          }
        }
      }

      ts.forEachChild(dugum, dolas)
    }

    dolas(kaynak)
  }

  if (eksikler.length > 0) {
    failed = true
    console.error(
      `[anahtar varlığı] hiçbir kümede olmayan anahtar (${eksikler.length}) — ziyaretçi boş metin ` +
        `görür:\n  ${eksikler.join('\n  ')}`,
    )
  }
}

if (failed) {
  process.exit(1)
}

console.log(
  `${ozet.join(' · ')} — hepsi eşit. Vitrin sınırı: ${vitrinModulleri.size} modül, tembel küme anahtarı yok. Anahtar varlığı: eksik yok.`,
)
