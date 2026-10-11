import { deflateSync } from 'node:zlib'

/**
 * Test görseli: gerçek, geçerli bir PNG — gömülü base64 blob DEĞİL.
 *
 * <p><b>Neden üretiliyor:</b> yükleme ucu türü İÇERİKTEN belirliyor (`0011` AC-4), yani
 * sahte baytlarla `415` alırdık. Depoya binary bir test varlığı eklemek yerine PNG burada
 * kuruluyor: dosya okunabilir kalıyor, boyut ve içerik testin kontrolünde ve
 * <code>git diff</code> dört bin karakterlik bir blob göstermiyor.</p>
 *
 * <p>Düz renk yerine satır satır değişen bir gradyan: filigran uygulaması ve türev
 * üretimi gerçek piksellerle koşuyor, ve PNG hâlâ birkaç kilobayt.</p>
 */
export function pngUret(genislik = 800, yukseklik = 600): Buffer {
  const satirlar: Buffer[] = []

  for (let y = 0; y < yukseklik; y++) {
    // Her satır bir filtre baytıyla başlar (0 = filtre yok), sonra RGB üçlüleri.
    const satir = Buffer.alloc(1 + genislik * 3)

    for (let x = 0; x < genislik; x++) {
      satir[1 + x * 3] = (40 + (y * 3) / 7) % 256
      satir[2 + x * 3] = 70
      satir[3 + x * 3] = 120
    }

    satirlar.push(satir)
  }

  const ihdr = Buffer.alloc(13)
  ihdr.writeUInt32BE(genislik, 0)
  ihdr.writeUInt32BE(yukseklik, 4)
  ihdr[8] = 8 // bit derinliği
  ihdr[9] = 2 // renk tipi: truecolor

  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    parca('IHDR', ihdr),
    parca('IDAT', deflateSync(Buffer.concat(satirlar), { level: 9 })),
    parca('IEND', Buffer.alloc(0)),
  ])
}

function parca(tip: string, govde: Buffer): Buffer {
  const uzunluk = Buffer.alloc(4)
  uzunluk.writeUInt32BE(govde.length, 0)

  const etiketliGovde = Buffer.concat([Buffer.from(tip, 'ascii'), govde])
  const crc = Buffer.alloc(4)
  crc.writeUInt32BE(crc32(etiketliGovde), 0)

  return Buffer.concat([uzunluk, etiketliGovde, crc])
}

/** PNG'nin parça sağlaması. Node'un `zlib.crc32`'si yalnız 22.15'ten sonra var. */
function crc32(govde: Buffer): number {
  let crc = 0xffffffff

  for (const bayt of govde) {
    crc ^= bayt

    for (let bit = 0; bit < 8; bit++) {
      crc = crc & 1 ? (crc >>> 1) ^ 0xedb88320 : crc >>> 1
    }
  }

  return (crc ^ 0xffffffff) >>> 0
}
