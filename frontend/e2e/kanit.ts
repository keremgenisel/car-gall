import type { Locator, Page } from '@playwright/test'

/**
 * Görsel kanıt üretimi (docs/testing.md § 3).
 *
 * **Yazma varsayılan olarak KAPALIDIR.** Sebep ölçülmüş bir gürültü: aynı Host, aynı
 * veritabanı ve hiç değişmeyen veriyle art arda koşulan iki e2e turunda 78 görselin
 * 8'i yine değişti. Kaynak seed verisi değil, tarayıcı render'ı ve PNG kodlaması —
 * sabit tohum bunu çözmez.
 *
 * Sonuç: her e2e koşusu çalışma ağacını kirletiyordu ve gerçek bir görsel değişikliği
 * bu gürültünün içinde görünmüyordu. Artık kanıt yalnız AÇIKÇA istendiğinde üretilir:
 *
 *     npm run evidence
 *
 * Testlerin assert'leri her koşuda çalışmaya devam eder; değişen tek şey dosyaya
 * yazılıp yazılmadığı. Yani kanıt üretimini kapatmak hiçbir kriteri kapısız bırakmaz.
 */
const YAZ = process.env['EVIDENCE_WRITE'] === '1'

const KANIT_DIZINI = '../docs/evidence'

/**
 * `hedef` bir seçici dizesi ya da hazır bir `Locator` olabilir: bazı kanıtlar
 * `filter()`/`getByTestId()` ile daraltılmış bir locator üzerinden alınıyor.
 *
 * Yazma kapalıyken hiçbir şey yapılmaz — bekleme de eklenmez. Testlerin kendi
 * `expect` çağrıları zaten öğenin görünürlüğünü doğruluyor; buraya ikinci bir
 * bekleme koymak, kanıt üretimini testin kapısı hâline getirirdi.
 */
export async function kanit(page: Page, hedef: string | Locator, dosya: string): Promise<void> {
  if (!YAZ) {
    return
  }

  const locator = typeof hedef === 'string' ? page.locator(hedef).first() : hedef

  await locator.screenshot({ path: `${KANIT_DIZINI}/${dosya}` })
}

/** Tüm görünür alanın kanıtı; tek bir öğeye indirgenemeyen düzen kriterleri için. */
export async function kanitSayfa(page: Page, dosya: string): Promise<void> {
  if (!YAZ) {
    return
  }

  await page.screenshot({ path: `${KANIT_DIZINI}/${dosya}`, fullPage: false })
}
