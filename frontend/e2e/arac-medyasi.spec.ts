import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { pngUret } from './gorsel'
import { yoneticiOlarakGirAsync as ortakYoneticiOlarakGirAsync } from './giris'

/**
 * `0012a`'nın kritik akışı: personel bir fotoğraf yükler, araca bağlar, sıralar, kapağı
 * sıranın ilk elemanı olarak görür ve `alt` metnini yazar (`docs/testing.md` § 3).
 *
 * <p>Bu akış bu dilimden önce HİÇBİR yoldan koşturulamıyordu: panelde ne yükleme kontrolü
 * vardı ne de bağlama ekranı (**D9**'un ölçümü).</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

/**
 * Şase galeri genelinde TEKİL (A5) ve iki profil AYRI worker'larda aynı anda koşuyor;
 * `0010b`'nin e2e'si aynı bedeli ödedi.
 */
function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

/**
 * `0013` D28/D46 — giriş yolu ortak yardımcıya taşındı (`e2e/giris.ts`): Yönetici'de
 * ikinci adım zorunlu (D30) ve giriş artık iki fazlı. Bu dosyanın iddiaları DEĞİŞMEDİ.
 */
async function yoneticiOlarakGirAsync(page: Page) {
  await ortakYoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
}

async function aracKaydetAsync(page: Page, sase: string) {
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  for (const alan of ['Şube', 'Model', 'Kasa tipi', 'Donanım paketi', 'Yakıt', 'Vites', 'Çekiş']) {
    const secim = page.getByLabel(alan, { exact: true })
    const deger = await secim.locator('option').nth(1).getAttribute('value')

    expect(deger, alan).not.toBeNull()
    await secim.selectOption(deger!)
  }

  await page.getByLabel('Model yılı', { exact: true }).fill('2021')
  await page.getByLabel('Kilometre', { exact: true }).fill('48500')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
}

/**
 * Aracın MEDYA ekranına gider. Araç kendi şasesiyle bulunuyor: "listenin ilk satırı
 * benim aracım" varsayımı paylaşılan veritabanında yanlış (`docs/testing.md` § 4).
 */
async function medyaEkraninaGitAsync(page: Page, sase: string): Promise<string> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  await page.goto(`/admin/vehicles/${arac!.vehicleId}/media`)
  await expect(page.getByTestId('vehicle-media-upload')).toBeVisible()

  return arac!.vehicleId
}

/** Tek dosya yükler ve varlığın kartı görünene kadar bekler (AC-1). */
async function fotografYukleAsync(page: Page, ad: string) {
  await page.getByLabel('Fotoğraf ekle', { exact: true }).setInputFiles({
    name: ad,
    mimeType: 'image/png',
    buffer: pngUret(),
  })

  const oncesi = await page.getByTestId(/^vehicle-media-item-/).count()

  await page.getByTestId('vehicle-media-upload-submit').click()

  await expect(page.getByTestId(/^vehicle-media-item-/)).toHaveCount(oncesi + 1)
}

test('0012a/AC-1, 0012a/AC-2, 0012a/AC-3, 0012a/AC-4: fotoğraf yüklenir, araca bağlanır, sıralanır ve ilki kapaktır', async ({
  page,
}) => {
  const sase = benzersizSase('E2EMED')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await medyaEkraninaGitAsync(page, sase)

  // AC-26 — yeni araçta bağlı fotoğraf yok ve boş durum NEDENİNİ söylüyor.
  await expect(page.getByTestId('vehicle-media-empty')).toContainText('Bu araca bağlı fotoğraf yok')

  await fotografYukleAsync(page, 'birinci.png')
  await fotografYukleAsync(page, 'ikinci.png')

  // Bağlama AÇIK bir kaydetmedir: yükleme tek başına aracın listesini yazmıyor.
  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  // AC-3 — sıra KALICI: sayfa yeniden yüklendiğinde iki kart duruyor.
  await page.reload()
  await expect(page.getByTestId(/^vehicle-media-item-/)).toHaveCount(2)

  // AC-4 — kapak rozeti yalnız İLK kartta.
  const kartlar = page.getByTestId(/^vehicle-media-item-/)

  await expect(kartlar.nth(0)).toContainText('Kapak')
  await expect(kartlar.nth(1)).not.toContainText('Kapak')

  // AC-5 — ikinciyi yukarı taşımak kapağı TAŞIR ve kaydetmeden sonra kalıcıdır.
  const ikinciKimlik = await kartlar.nth(1).getAttribute('data-testid')

  await kartlar.nth(1).getByTestId('vehicle-media-up').click()
  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  await page.reload()
  await expect(page.getByTestId(/^vehicle-media-item-/).nth(0)).toHaveAttribute(
    'data-testid',
    ikinciKimlik!,
  )
  await expect(page.getByTestId(/^vehicle-media-item-/).nth(0)).toContainText('Kapak')
})

test('0012a/AC-6, 0012a/AC-11: fotoğraf araçtan çıkarılır ve `alt` metni üç dilde yazılır', async ({
  page,
}) => {
  const sase = benzersizSase('E2EALT')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await medyaEkraninaGitAsync(page, sase)

  await fotografYukleAsync(page, 'birinci.png')
  await fotografYukleAsync(page, 'ikinci.png')

  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  const kart = page.getByTestId(/^vehicle-media-item-/).first()

  // AC-11 — üç dil ayrı ayrı; biri boş bırakılabilir (AC-12) ama burada üçü de yazılıyor.
  await kart.getByLabel('Kısa tanım (Türkçe)', { exact: true }).fill('Gece mavisi araç, önden')
  await kart.getByLabel('Kısa tanım (العربية)', { exact: true }).fill('سيارة كحلية من الأمام')
  await kart.getByLabel('Kısa tanım (English)', { exact: true }).fill('Navy car, front view')
  await kart.getByTestId('vehicle-media-alt-save').click()

  // Kaydetmenin BİTTİĞİNİ beklemek zorunlu: ilk hâlimde tıklamadan hemen sonra
  // yeniden yüklüyordum ve test, isteğin yarısında kesilmiş bir yazmayı ölçüyordu.
  await expect(kart).toContainText('Tanımlar kaydedildi.')

  await page.reload()

  await expect(
    page
      .getByTestId(/^vehicle-media-item-/)
      .first()
      .getByLabel('Kısa tanım (Türkçe)', { exact: true }),
  ).toHaveValue('Gece mavisi araç, önden')

  // AC-6 — çıkarmak listeden düşürüyor ve kaydetmeden sonra kalıcı.
  await page
    .getByTestId(/^vehicle-media-item-/)
    .first()
    .getByTestId('vehicle-media-detach')
    .click()
  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  await page.reload()
  await expect(page.getByTestId(/^vehicle-media-item-/)).toHaveCount(1)
})
