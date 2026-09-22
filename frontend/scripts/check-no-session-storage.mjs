import { existsSync, readFileSync, readdirSync } from 'node:fs'

/**
 * CI kapısı: 0006 C17 / AC-29 — **oturum bilgisi sayfadaki JavaScript'ten okunabilir bir
 * yerde tutulmaz.**
 *
 * Sunucu yarısı testle ölçülüyor (çerez `HttpOnly`, gövdede token yok). Uç yarısı ancak
 * kaynak taramasıyla ölçülebilir: bir gün birinin `document.cookie` okuması ya da
 * token'ı `localStorage`'a yazması, hiçbir testi kırmadan korumayı kaldırırdı.
 *
 * "Komutu olmayan kapı, kapı değildir" (docs/frontend.md § 8).
 */
const src = new URL('../src/', import.meta.url).pathname

const yasak = [
  { desen: /document\.cookie/, ad: 'document.cookie' },
  {
    desen: /localStorage\s*\.\s*(set|get)Item\s*\(\s*['"`][^'"`]*(token|session|auth)/i,
    ad: 'localStorage içinde oturum',
  },
  {
    desen: /sessionStorage\s*\.\s*(set|get)Item\s*\(\s*['"`][^'"`]*(token|session|auth)/i,
    ad: 'sessionStorage içinde oturum',
  },
]

const hatalar = []

for (const dosya of walk(src)) {
  const metin = readFileSync(dosya, 'utf8')

  for (const { desen, ad } of yasak) {
    if (desen.test(metin)) {
      hatalar.push(`${kisa(dosya)}: ${ad}`)
    }
  }
}

if (hatalar.length > 0) {
  console.error('Oturum bilgisi JavaScript ten okunabilir bir yerde tutulamaz (0006 C17):')
  for (const hata of hatalar) console.error(`  ${hata}`)
  process.exit(1)
}

console.log('Oturum bilgisi JavaScript e açık bir yerde tutulmuyor.')

function walk(kok) {
  if (!existsSync(kok)) return []
  const cikti = []
  for (const ad of readdirSync(kok, { withFileTypes: true })) {
    const tam = `${kok}/${ad.name}`
    if (ad.isDirectory()) cikti.push(...walk(tam))
    else if (/\.tsx?$/.test(ad.name)) cikti.push(tam)
  }
  return cikti
}

function kisa(yol) {
  return yol.slice(yol.indexOf('/src/') + 1)
}
