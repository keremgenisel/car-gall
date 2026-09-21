import { existsSync, readdirSync, readFileSync } from 'node:fs'

/**
 * CI kapısı: docs/frontend.md § 1 klasör yapısı.
 * "Komutu olmayan kapı, kapı değildir" (§ 8) — yapısal kriterler de denetlenebilir olmalı.
 */
const src = new URL('../src/', import.meta.url).pathname

const zorunlu = [
  'features',
  'shared/api',
  'shared/ui',
  'shared/design',
  'shared/i18n',
  'shared/lib',
]
const yasak = ['api', 'components', 'lib', 'locales']

const hatalar = []

for (const yol of zorunlu) {
  if (!existsSync(`${src}${yol}`)) {
    hatalar.push(`eksik klasör: src/${yol}`)
  }
}

for (const yol of yasak) {
  if (existsSync(`${src}${yol}`)) {
    hatalar.push(`kaldırılmamış eski klasör: src/${yol} — içeriği src/shared/ altına taşınır`)
  }
}

// Bir feature başka bir feature'ın iç dosyasını import etmez; shared/ features/ten import etmez.
const features = existsSync(`${src}features`) ? readdirSync(`${src}features`) : []
for (const dosya of walk(`${src}shared`)) {
  if (/from '.*features\//.test(read(dosya))) {
    hatalar.push(`shared/ içinden features/ import ediliyor: ${kisa(dosya)}`)
  }
}
for (const feature of features) {
  for (const dosya of walk(`${src}features/${feature}`)) {
    const metin = read(dosya)
    for (const digeri of features.filter((f) => f !== feature)) {
      if (metin.includes(`features/${digeri}/`)) {
        hatalar.push(`feature sınırı ihlali: ${kisa(dosya)} → features/${digeri}/`)
      }
    }
  }
}

if (hatalar.length > 0) {
  console.error('docs/frontend.md § 1 ihlalleri:')
  for (const h of hatalar) console.error(`  ${h}`)
  process.exit(1)
}

console.log('Klasör yapısı docs/frontend.md § 1 ile uyumlu.')

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

function read(yol) {
  return readFileSync(yol, 'utf8')
}

function kisa(yol) {
  return yol.slice(yol.indexOf('/src/') + 1)
}
