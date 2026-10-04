import { readFileSync, readdirSync, statSync } from 'node:fs'
import { join } from 'node:path'

/**
 * CI kapısı: kullanıcıya görünen metin bileşene GÖMÜLMEZ — placeholder, aria-label,
 * title, boş durum metni ve buton etiketi dâhil (docs/frontend.md § 5, Yasak Listesi 24).
 *
 * İki şeyi arar:
 *   1. JSX gövdesinde harf içeren çıplak metin
 *   2. metin taşıyan özniteliklerde ("alt", "title", "placeholder", "aria-label") dize sabiti
 * Her ikisi de t(...) çağrısından gelmelidir.
 */
const root = new URL('../src/', import.meta.url).pathname

const TEXT_ATTRIBUTES = ['alt', 'title', 'placeholder', 'aria-label']
const violations = []

function walk(directory) {
  for (const entry of readdirSync(directory)) {
    const path = join(directory, entry)

    if (statSync(path).isDirectory()) {
      walk(path)
      continue
    }

    if (path.endsWith('.tsx')) {
      inspect(path)
    }
  }
}

function inspect(path) {
  const lines = readFileSync(path, 'utf8').split('\n')

  lines.forEach((line, index) => {
    const trimmed = line.trim()

    if (trimmed.startsWith('//') || trimmed.startsWith('*') || trimmed.startsWith('/*')) {
      return
    }

    for (const attribute of TEXT_ATTRIBUTES) {
      const match = new RegExp(`${attribute}="[^"]*\\p{L}[^"]*"`, 'u').exec(line)
      if (match !== null) {
        violations.push(`${path}:${index + 1}  ${match[0]}`)
      }
    }

    // >Metin< biçimindeki çıplak JSX metni.
    //
    // `=>` ARDINDAN gelen `>` sayılmaz: `(call: () => Promise<unknown>)` gibi bir tip
    // imzası JSX metni değildir ve kapı onu "gömülü metin" diye bildiriyordu. Yanlış
    // pozitif, kapıyı gürültüye boğar ve bir sonraki GERÇEK ihlali gizler.
    const bare = /(?<!=)>\s*[^<>{}\n]*\p{L}[^<>{}\n]*\s*</u.exec(line)
    if (bare !== null) {
      violations.push(`${path}:${index + 1}  ${bare[0].trim()}`)
    }
  })
}

walk(root)

if (violations.length > 0) {
  console.error('Kullanıcıya görünen metin koda gömülmüş (docs/frontend.md § 5):')
  for (const violation of violations) {
    console.error(`  ${violation}`)
  }
  process.exit(1)
}

console.log('Sert kodlanmış kullanıcı metni bulunmadı.')
