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

if (failed) {
  process.exit(1)
}

console.log(`${catalogs.size} katalog, ${allKeys.size} anahtar — hepsi eşit.`)
