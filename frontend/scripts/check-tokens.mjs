import { readFileSync, readdirSync } from 'node:fs'
import { join } from 'node:path'

/**
 * CI kapısı: token KATMANININ kendi kuralları (frontend-002 AC-4, AC-8).
 *
 * İki kural birden, çünkü ikisi de aynı katmana ait ve ikisi de lint'in göremediği şeyler:
 *
 * 1. **Token adları ANLAMSALDIR** (C6, AC-4). `--color-lemon` yazılırsa müşterinin rengi
 *    mavi olduğu gün ad yalan söyler ve kimse fark etmez — ad değişmediği için.
 * 2. **Yazı tipleri uygulamanın kendi kaynağından gelir** (D8, R12, AC-8). Üçüncü taraf
 *    font sağlayıcısı, vitrini açan her ziyaretçinin IP adresini o sağlayıcıya gönderir.
 *    Bu projede KVKK bir ek değil, ürünün içinde.
 */
const designDir = new URL('../src/shared/design/', import.meta.url).pathname
const srcDir = new URL('../src/', import.meta.url).pathname
const indexHtml = new URL('../index.html', import.meta.url).pathname

/** Değer temelli adlar. Ad listesi kısa tutuldu: kapı gürültülüyse kapattığından çok şey açar. */
const colorWords = [
  'lemon',
  'olive',
  'blue',
  'red',
  'green',
  'yellow',
  'orange',
  'purple',
  'pink',
  'teal',
  'cyan',
  'magenta',
  'grey',
  'gray',
  'black',
  'white',
  'navy',
  'beige',
]

/** Üçüncü taraf yazı tipi sağlayıcıları. */
const fontHosts = [
  'fonts.googleapis.com',
  'fonts.gstatic.com',
  'use.typekit.net',
  'fonts.bunny.net',
  'cdn.jsdelivr.net',
  'unpkg.com',
]

let failed = false

// --- AC-4
const tokenFiles = readdirSync(designDir).filter((name) => name.endsWith('.css'))

if (tokenFiles.length === 0) {
  console.error('Token dosyası bulunamadı; kapı hiçbir şey denetlemiyor.')
  process.exit(1)
}

for (const name of tokenFiles) {
  const lines = readFileSync(join(designDir, name), 'utf8').split('\n')

  lines.forEach((line, index) => {
    const declaration = /^\s*(--[\w-]+)\s*:/.exec(line)

    if (declaration === null) {
      return
    }

    const tokenName = declaration[1]
    const offender = colorWords.find((word) => tokenName.toLowerCase().includes(word))

    if (offender !== undefined) {
      failed = true
      console.error(
        `${name}:${index + 1} token adı renk adı içeriyor ('${offender}'): ${tokenName} — adlar anlamsaldır (frontend-002 C6, AC-4).`,
      )
    }
  })
}

// --- AC-8
function walk(dir) {
  const entries = readdirSync(dir, { withFileTypes: true })

  return entries.flatMap((entry) =>
    entry.isDirectory() ? walk(join(dir, entry.name)) : [join(dir, entry.name)],
  )
}

for (const file of [...walk(srcDir), indexHtml]) {
  const text = readFileSync(file, 'utf8')
  const offender = fontHosts.find((host) => text.includes(host))

  if (offender !== undefined) {
    failed = true
    console.error(
      `${file} üçüncü taraf kaynağa atıf içeriyor ('${offender}') — yazı tipleri kendi kaynağımızdan servis edilir (frontend-002 D8, AC-8).`,
    )
  }
}

if (failed) {
  process.exit(1)
}

console.log(
  `Token kapısı yeşil: ${tokenFiles.length} token dosyası, üçüncü taraf font kaynağı yok.`,
)
