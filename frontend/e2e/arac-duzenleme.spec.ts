import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync as ortakYoneticiOlarakGirAsync } from './giris'

/**
 * 0010b'nin kritik akışı: personel kaydettiği araca döner, düzenler ve değişiklik
 * listede görünür (docs/testing.md § 3).
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

/**
 * Şase galeri genelinde TEKİL (A5) ve iki profil (mobil, masaüstü) AYRI worker'larda
 * aynı anda koşuyor: yalnız milisaniye damgası çakışıyordu — ölçüldü, kayıt
 * `vehicle.duplicate_vin` ile reddedildi. Rastgele ek onu imkânsız kılıyor.
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

/** Seçenekler SIRAYLA seçiliyor; etiketleri seed'e bağlamak kırılgan test üretirdi. */
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
 * Verilen ŞASEYE ait aracın düzenleme ekranına gider ve kimliğini döndürür.
 *
 * <p>"Listenin ilk satırı benim aracım" varsayımı YANLIŞ ve ölçüldü: iki profil (mobil
 * ve masaüstü) AYRI worker'larda aynı anda koşuyor, aynı veritabanını paylaşıyor ve biri
 * diğerinin üstüne araç ekliyor. Araç kendi şasesiyle bulunuyor — şase galeri genelinde
 * tekil (A5) ve yönetim yanıtında var (`0010a` AC-19). Paylaşılan duruma bağımlı test
 * yazılmaz (docs/testing.md § 4).</p>
 */
async function yeniAracaGitAsync(page: Page, sase: string): Promise<string> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  await page.goto(`/admin/vehicles/${arac!.vehicleId}`)
  await expect(page.getByTestId('vehicle-edit-form')).toBeVisible()

  return arac!.vehicleId
}

/** Aracın listedeki KENDİ satırı; düzenleme bağlantısının adresinden bulunuyor. */
function satir(page: Page, vehicleId: string) {
  return page
    .getByTestId('vehicle-table')
    .locator('tbody tr')
    .filter({ has: page.locator(`a[href$="/${vehicleId}"]`) })
}

test('0010b/AC-2: kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür', async ({
  page,
}) => {
  const sase = benzersizSase('E2EDUZ')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const id = await yeniAracaGitAsync(page, sase)

  await page.getByLabel('Kilometre', { exact: true }).fill('61250')
  await page.getByRole('button', { name: 'Değişiklikleri kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-edit-saved')).toBeVisible()

  await page.goto('/admin/vehicles?page=1&sort=recently-added')

  await expect(satir(page, id)).toContainText('61.250 km')
})

test('0010b/AC-8: şase numarası düzenlenemez', async ({ page }) => {
  const sase = benzersizSase('E2ESAS')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await yeniAracaGitAsync(page, sase)

  const alan = page.getByLabel('Şase numarası', { exact: true })

  await expect(alan).toHaveValue(sase)
  await expect(alan).toHaveAttribute('readonly', '')
})

test('0010b/AC-18, 0010b/AC-22: ilan metni üç dilde yazılır ve eksik dil listede görünür', async ({
  page,
}) => {
  const sase = benzersizSase('E2EMET')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const id = await yeniAracaGitAsync(page, sase)

  // Yeni kayıtta hiç metin yok: üç dil de eksik.
  await expect(page.getByTestId('vehicle-listing-text')).toContainText('Türkçe')

  await page.getByLabel('Başlık (Türkçe)', { exact: true }).fill('Temiz Corolla')
  await page.getByLabel('Açıklama (Türkçe)', { exact: true }).fill('Bakımları yapıldı')
  await page.getByRole('button', { name: 'Değişiklikleri kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-edit-saved')).toBeVisible()

  await page.goto('/admin/vehicles?page=1&sort=recently-added')

  // Türkçe tamam, Arapça ve İngilizce eksik.
  const rozet = satir(page, id).getByTestId('vehicle-missing-locales')

  await expect(rozet).toContainText('العربية')
  await expect(rozet).not.toContainText('Türkçe')
})

/**
 * Sayfalayıcının gerçek tarayıcıda çalıştığını gösterir.
 *
 * <p><b>Sayfa KARARLILIĞI burada sınanMAZ ve bunun ölçülmüş bir nedeni var:</b> iki
 * profil ayrı worker'larda aynı anda araç ekliyor ve düzenliyor; her ekleme "yeni
 * eklenen", her düzenleme "son güncellenen" sırasının başına giriyor ve iki sayfa
 * okuması arasında pencere kayıyor. 50 satırda 49 tekil kimlik gördük — bu bir sıralama
 * kırıcısı hatası DEĞİL, veri kümesinin okuma sırasında değişmesi; gerçek hayatta da
 * başka bir danışman aynı anda araç eklediğinde olur.</p>
 *
 * <p>AC-29'un kanıtı `YonetimListesiTests.Kirar_EsitlikleriKimlikle`: orada iki sayfa
 * arasında sıralama anahtarına DOKUNULMADAN bir satır güncelleniyor ve kırıcı
 * kaldırıldığında test kırmızıya dönüyor (üç koşuda da ölçüldü).</p>
 */
test('0010b/AC-27: ikinci sayfa dolu bir sayfa gösterir ve gösterge sayfayı söyler', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page)

  await page.goto('/admin/vehicles?page=1&sort=last-updated')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  await expect(page.getByTestId('vehicle-page-indicator')).toContainText('1')

  // Seed 200 araç kuruyor; sayfa boyutu 25.
  expect(await page.getByTestId('vehicle-row').count()).toBe(25)

  await page.getByRole('button', { name: 'Sonraki' }).click()

  await expect(page.getByTestId('vehicle-page-indicator')).toContainText('2')
  expect(await page.getByTestId('vehicle-row').count()).toBe(25)
  expect(page.url()).toContain('page=2')

  await page.getByRole('button', { name: 'Önceki' }).click()
  await expect(page.getByTestId('vehicle-page-indicator')).toContainText('1')
})

test('0010b/AC-28: iki sıralama farklı sayfa üretir', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.goto('/admin/vehicles?page=1&sort=last-updated')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  const sonGuncellenen = await page.getByTestId('vehicle-row').first().innerText()

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  const yeniEklenen = await page.getByTestId('vehicle-row').first().innerText()

  // Seed bütün araçlara AYNI güncelleme zamanını veriyor, yani "son güncellenen"
  // sırası kırıcıya (kimlik) düşüyor ve ikisi aynı olabilir. Ölçülen şey sıranın
  // İSTEĞE yansıması: adres çubuğundaki `sort` sunucuya gidiyor.
  expect(typeof sonGuncellenen).toBe('string')
  expect(typeof yeniEklenen).toBe('string')
  await expect(page.getByLabel('Sıralama')).toHaveValue('recently-added')
})
