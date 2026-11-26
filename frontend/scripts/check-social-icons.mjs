import { readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'
import { gzipSync } from 'node:zlib'

/**
 * `0024` D9(b) · AC-66, AC-67 — derleme çıktısında platform simgelerinin yeri:
 *   1. adı `social-icons` ile başlayan TEK bir JS dosyası var ve `gzip -9` ile en çok 3072 B,
 *   2. beş simgenin yol verisinin hepsi o dosyada,
 *   3. başka hiçbir JS dosyası (giriş JS'i dâhil) yol verisini taşımıyor,
 *   4. parça TEMBEL: hiçbir dosya onu statik içe aktarmıyor ve `index.html` onu bağlamıyor ya da
 *      ön yüklemiyor (AC-68). Bu madde olmadan `manualChunks` adı verir ama statik bir içe
 *      aktarma parçayı her sayfaya indirir; ilk denemede kapı bunu GEÇİRDİ (negatif kanıt),
 *   5. kritik yolda TEK JS var (QA 2 BULGU-1, AC-55'in revize tanımı): `index.html` hiçbir
 *      `modulepreload` taşımaz (`main`'de yok) ve giriş JS'i başka bir dosyayı statik içe
 *      aktarmaz. `manualChunks` paylaşılan bir çalışma zamanı parçası üretip ikisini de yapıyordu
 *      ve "giriş JS + CSS" ölçümü onu görmüyordu.
 * Yollar kaynaktan (`socialIconLinks.ts`) okunur: kapı ölçtüğü şeyi kendisi uydurmaz.
 * Kullanım: `npm run build && npm run check:social-icons [çıktı dizini]`.
 */
const LIMIT = 3072
const outDir = process.argv[2] ?? new URL('../../src/Host/wwwroot/', import.meta.url).pathname
const source = readFileSync(
  new URL('../src/features/dealership/socialIconLinks.ts', import.meta.url),
  'utf8',
)
const block = /export const SOCIAL_ICON_PATHS = \{([\s\S]*?)\} as const/.exec(source)?.[1] ?? ''
const paths = [...block.matchAll(/^\s+(\w+):\s*'([^']+)',?$/gm)].map(([, name, d]) => ({ name, d }))
const failures = []

if (paths.length !== 5) {
  failures.push(`socialIconLinks.ts'te beş yol bekleniyordu, ${paths.length} bulundu`)
}

const assets = join(outDir, 'assets')
const scripts = readdirSync(assets).filter((name) => name.endsWith('.js'))
const chunks = scripts.filter((name) => name.startsWith('social-icons'))
const html = readFileSync(join(outDir, 'index.html'), 'utf8')
const entry = /<script[^>]+src="\/assets\/([^"]+\.js)"/.exec(html)?.[1]

if (html.includes('social-icons')) {
  failures.push('index.html simge parçasını bağlıyor ya da ön yüklüyor')
}

for (const preload of html.matchAll(/<link[^>]*rel="modulepreload"[^>]*>/g)) {
  failures.push(`index.html kritik yola ikinci JS ekliyor: ${preload[0]}`)
}

if (chunks.length !== 1) {
  failures.push(
    `adı social-icons ile başlayan tek JS dosyası bekleniyordu: ${chunks.join(', ') || 'yok'}`,
  )
}

if (entry === undefined || chunks.includes(entry)) {
  failures.push(`giriş JS'i bulunamadı ya da simge parçasının kendisi: ${entry ?? 'yok'}`)
}

for (const name of scripts) {
  const text = readFileSync(join(assets, name), 'utf8')
  const isChunk = chunks.includes(name)

  if (/(?:from\s*|import\s*)["'][^"']*social-icons-[^"']*\.js["']/.test(text)) {
    failures.push(`${name} simge parçasını STATİK içe aktarıyor`)
  }

  if (name === entry) {
    for (const statik of text.matchAll(/(?:from\s*|import\s*)["'](\.\/[^"']+\.js)["']/g)) {
      failures.push(
        `giriş JS'i ${statik[1]} dosyasını statik içe aktarıyor (kritik yolda ikinci JS)`,
      )
    }
  }

  for (const path of paths) {
    const found = text.includes(path.d)
    if (isChunk && !found) {
      failures.push(`${name} ${path.name} yolunu taşımıyor`)
    }
    if (!isChunk && found) {
      failures.push(`${name}${name === entry ? ' (giriş JS)' : ''} ${path.name} yolunu taşıyor`)
    }
  }

  if (isChunk) {
    const size = gzipSync(readFileSync(join(assets, name)), { level: 9 }).length
    console.log(`${name}: ${size} B gzip -9 (sınır ${LIMIT} B)`)
    if (size > LIMIT) {
      failures.push(`${name} ${size} B; sınır ${LIMIT} B`)
    }
  }
}

if (failures.length > 0) {
  console.error('Simge parçası denetimi (0024 D9(b)):')
  for (const failure of failures) {
    console.error(`  ${failure}`)
  }
  process.exit(1)
}

console.log(
  `Simgeler tek parçada; giriş JS'i (${entry}) ve diğer ${scripts.length - 1} dosya yol taşımıyor.`,
)
