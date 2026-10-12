import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'

/**
 * `0012c` — toplu eylemin uçtan uca akışı.
 *
 * <p>Birim testleri sonucun nasıl BASILDIĞINI ölçüyor; buradaki akış sunucunun gerçekten
 * **kısmen** uyguladığını ölçüyor. İkisi farklı şeyler: sahte bir yanıtla çizilen kısmi
 * başarı kutusu, işlem sınırının ilan başına olduğunu kanıtlamaz.</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

async function yoneticiOlarakGirAsync(page: Page) {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await page.getByLabel('E-posta', { exact: true }).fill(SEED_KIMLIK)
  await page.getByLabel('Parola', { exact: true }).fill(YONETICI_PAROLA)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  await expect(page.getByTestId('admin-shell')).toBeVisible()
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
  // Kilometre BİLEREK 51.300: `0010a`/AC-11 panel satırını "48.500 km" metniyle buluyor
  // ve o değeri üç spec birden yazıyor. Aynı değeri dördüncü kez yazmak, o testin
  // bulduğu satırın hangi koşuya ait olduğunu kuma yazmak olurdu.
  await page.getByLabel('Kilometre', { exact: true }).fill('51300')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
}

async function aracKimligiAsync(page: Page, sase: string): Promise<string> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  return arac!.vehicleId
}

test('0012c/AC-1, 0012c/AC-38: iki araç seçilir, biri uygulanır ve diğeri gerekçesiyle bildirilir', async ({
  page,
}) => {
  const birinci = benzersizSase('K12CA1')
  const ikinci = benzersizSase('K12CA2')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, birinci)
  await aracKaydetAsync(page, ikinci)

  const birinciId = await aracKimligiAsync(page, birinci)
  const ikinciId = await aracKimligiAsync(page, ikinci)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-select-${birinciId}`).check()
  await page.getByTestId(`vehicle-select-${ikinciId}`).check()

  await expect(page.getByTestId('vehicle-bulk-selection')).toContainText('2 araç seçildi')

  // İkisi de TASLAK ve ikisinin de fotoğrafı yok: arşivleme ikisinde de geçerli.
  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()

  await expect(page.getByTestId('vehicle-bulk-success')).toContainText('2 ilan arşivlendi')

  // Arşivlenen araçlar varsayılan listeden düştü (`0012b` D19).
  await expect(page.getByTestId(`vehicle-select-${birinciId}`)).toHaveCount(0)
})

test('0012c/AC-38: kısmi başarı — geçerli satır uygulanır, geçersiz satır gerekçesiyle kalır', async ({
  page,
}) => {
  const yayinlanamaz = benzersizSase('K12CB1')
  const arsivlenebilir = benzersizSase('K12CB2')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, yayinlanamaz)
  await aracKaydetAsync(page, arsivlenebilir)

  const yayinlanamazId = await aracKimligiAsync(page, yayinlanamaz)
  const arsivlenebilirId = await aracKimligiAsync(page, arsivlenebilir)

  // İkinci araç önce arşivleniyor: artık yayına alınamaz (A3) ve toplu YAYINLAMA onu
  // reddedecek. Birincisi de fotoğrafsız, yani ikisi de reddedilecek — bu yüzden
  // birincisini kasten BAŞARILI kılmak için arşivleme eylemi seçiliyor.
  await page.goto(`/admin/vehicles?page=1&sort=recently-added`)
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-select-${arsivlenebilirId}`).check()
  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()
  await expect(page.getByTestId('vehicle-bulk-success')).toBeVisible()

  // Şimdi arşiv AÇIK: biri arşivde (arşivleme reddedilecek), biri taslak (uygulanacak).
  await page.goto('/admin/vehicles?page=1&sort=recently-added&archived=true')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-select-${yayinlanamazId}`).check()
  await page.getByTestId(`vehicle-select-${arsivlenebilirId}`).check()

  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()

  const kutu = page.getByTestId('vehicle-bulk-partial')

  await expect(kutu).toContainText('1 ilan uygulandı')
  await expect(kutu).toContainText('1 ilan uygulanamadı')

  // Uygulanamayan araç ADIYLA görünüyor ve hiçbir teknik detay sızmıyor (AC-39).
  await expect(page.getByTestId('vehicle-bulk-failure')).toHaveCount(1)
  await expect(page.locator('body')).not.toContainText('listing.invalid_status_transition')
  await expect(page.locator('body')).not.toContainText('409')
})

test('0012c/AC-32: öne çıkarılan araç listede rozetle ayırt edilir', async ({ page }) => {
  const sase = benzersizSase('K12C32')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)

  const aracId = await aracKimligiAsync(page, sase)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  const satir = page.locator('tbody tr').filter({ has: page.locator(`a[href$="${aracId}"]`) })

  await expect(satir.getByTestId('vehicle-featured')).toHaveCount(0)

  await page.getByTestId(`vehicle-feature-${aracId}`).click()

  // Rozet olmadan öne çıkarma yalnızca YAZAN bir düğme olurdu (**D50**).
  await expect(satir.getByTestId('vehicle-featured')).toContainText('Öne çıkarıldı')
})

test('0012c/AC-43, 0012c/AC-44: arşivlenmiş satırda düğme yok; red satırda gerekçesiyle görünür', async ({
  page,
}) => {
  const sase = benzersizSase('K12C43')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)

  const aracId = await aracKimligiAsync(page, sase)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  // Önce GERÇEKTEN arşivleniyor: sahte bir durum kodu, satırın hangi düğmeyi
  // taşıdığını değil testin kurduğu veriyi ölçerdi.
  await page.getByTestId(`vehicle-select-${aracId}`).check()
  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()
  await expect(page.getByTestId('vehicle-bulk-success')).toBeVisible()

  await page.goto('/admin/vehicles?page=1&sort=recently-added&archived=true')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  const satir = page.locator('tbody tr').filter({ has: page.locator(`a[href$="${aracId}"]`) })

  await expect(satir).toHaveCount(1)
  await expect(page.getByTestId(`vehicle-feature-${aracId}`)).toHaveCount(0)

  // Düğmenin kalan her başarısızlık yolu da sessiz değil. Red SAHTELENİYOR çünkü
  // arşivlenmiş satırda düğme artık hiç yok ve `concurrency.lost` gerçek bir yarışla
  // üretilemez; ölçülen şey sunucunun kararı değil (onu AC-19 ölçtü), EKRANIN tepkisi.
  await page.route(`**/api/admin/vehicles/*/feature*`, async (route) => {
    await route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 409, errorCode: 'concurrency.lost' }),
    })
  })

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  await page.getByTestId(`vehicle-feature-${await ilkAracIdAsync(page)}`).click()

  const hata = page.getByTestId(/vehicle-feature-error-/)

  await expect(hata).toContainText('başka bir değişiklik oldu')
  await expect(page.locator('body')).not.toContainText('concurrency.lost')
})

/** Listedeki ilk satırın araç kimliği: seçim kutusunun `data-testid`'inden okunuyor. */
async function ilkAracIdAsync(page: Page): Promise<string> {
  const kutu = page.locator('tbody tr [data-testid^="vehicle-select-"]').first()
  const testId = await kutu.getAttribute('data-testid')

  expect(testId).not.toBeNull()

  return testId!.replace('vehicle-select-', '')
}
