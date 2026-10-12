import { existsSync, readFileSync, readdirSync } from 'node:fs'

/**
 * CI kapısı: bir blok yorumunun içinde `·` yerine `/` ayracı kullanmak yorumu **erken
 * kapatır**.
 *
 * `**R4**` ile `**D6**` arasına `/` koymak metinde `·` görüntüsü verir ama kaynakta
 * `**` + `/` üretir — yani blok yorumunun sonlandırıcısını. Yorumun kalanı koda dönüşür;
 * bazen derleme hatası olur, bazen sessizce geçer. Aynı kusur `0012a`, `0012b` ve
 * `0012c`'de **üç kez** tekrarladı ve her seferinde elle düzeltildi.
 *
 * Ayraç `·` olmalı (docs/frontend.md § 5). "Komutu olmayan kapı, kapı değildir" (§ 8).
 */
const kokler = [
  new URL('../src/', import.meta.url).pathname,
  new URL('../e2e/', import.meta.url).pathname,
]

const hatalar = []

for (const kok of kokler) {
  for (const dosya of walk(kok)) {
    let icinde = false

    for (const [sira, satir] of readFileSync(dosya, 'utf8').split('\n').entries()) {
      if (!icinde) {
        const basla = satir.indexOf('/**')
        if (basla >= 0 && !satir.slice(basla + 3).includes('*/')) icinde = true
        continue
      }

      // Sonlandırıcının kendisi: satırda yalnız yıldızlar ve `/` var.
      if (/^\s*\*+\/\s*$/.test(satir)) {
        icinde = false
        continue
      }

      if (satir.includes('**/')) {
        hatalar.push(`${kisa(dosya)}:${sira + 1}  ${satir.trim()}`)
      }

      if (satir.includes('*/')) icinde = false
    }
  }
}

if (hatalar.length > 0) {
  console.error(
    'Blok yorumunda `**/` yorumu erken kapatır; iki referansı `·` ile ayır (docs/frontend.md § 5):',
  )
  for (const hata of hatalar) console.error(`  ${hata}`)
  process.exit(1)
}

console.log('Blok yorumlarında erken kapatan ayraç yok.')

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
  const i = yol.indexOf('/frontend/')
  return (i >= 0 ? yol.slice(i + 1) : yol).replace(/\/{2,}/g, '/')
}
