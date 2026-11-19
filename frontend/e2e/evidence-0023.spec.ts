import { expect, test, type Browser, type Page } from '@playwright/test'
import { kanit } from './kanit'
import { YONETICI_PAROLA } from './global-setup'
import { girisYap, ikinciAdimKurAsync, yoneticiOlarakGirAsync } from './giris'
import { pngUret } from './gorsel'

/**
 * `0023` — satışı geri almanın görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * `docs/testing.md` § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 *
 * <p><b>Her test kendi aracını kaydeder</b> ve fotoğrafını gerçekten yükler; yayın, rezerve
 * ve satış API'den yapılıyor — kanıtın konusu satış düğmesi değil, geri almanın kendisi.</p>
 *
 * <p><b>AC-7'nin kanıtı şube kapatarak değil fotoğrafları kaldırarak üretiliyor:</b>
 * `main`'de şube kapatmanın HTTP ucu yok (`0003` D1; `0020` getiriyor). Ekranın davranışı
 * — `409`, güncel hedef, korunmuş gerekçe — ikisinde de aynı yoldan geçiyor; şube
 * kapatmanın kendisi `Host.Tests/SatisGeriAlmaZinciriTests`'te gerçek `Dealership`
 * tablosuyla ölçülüyor.</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'
const GEREKCE = 'Yanlış araca tıklandı; satış aynı modelin başka ilanındaydı.'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 17).toUpperCase()
}

/**
 * Araç GERÇEKTEN kaydediliyor (`0012b` kanıtının deseni): vitrin tohumunun araçları
 * `Dealership`'te olmayan bir şubeye bağlı ve geri alma şubeyi kaynaktan okuduğu için
 * (D12) onları "şubesi bulunamayan araç" sayıp Taslak'a döndürür — kanıt yanlış hâli
 * gösterirdi.
 */
async function aracKaydetAsync(page: Page, sase: string, kilometre: number): Promise<string> {
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  for (const alan of ['Şube', 'Model', 'Kasa tipi', 'Donanım paketi', 'Yakıt', 'Vites', 'Çekiş']) {
    const secim = page.getByLabel(alan, { exact: true })
    const deger = await secim.locator('option').nth(1).getAttribute('value')

    expect(deger, alan).not.toBeNull()
    await secim.selectOption(deger!)
  }

  await page.getByLabel('Model yılı', { exact: true }).fill('2021')
  await page.getByLabel('Kilometre', { exact: true }).fill(String(kilometre))
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )
  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  return arac!.vehicleId
}

async function hazirFotografAsync(page: Page, vehicleId: string) {
  await page.goto(`/admin/vehicles/${vehicleId}/media`)
  await expect(page.getByTestId('vehicle-media-upload')).toBeVisible()

  await page.getByLabel('Fotoğraf ekle', { exact: true }).setInputFiles({
    name: 'arac.png',
    mimeType: 'image/png',
    buffer: pngUret(),
  })

  await page.getByTestId('vehicle-media-upload-submit').click()
  await expect(page.getByTestId(/^vehicle-media-item-/)).toHaveCount(1)

  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  await expect(async () => {
    await page.reload()
    await expect(page.getByText('Hazır', { exact: true })).toHaveCount(1)
  }).toPass({ timeout: 60_000 })
}

async function eylemAsync(page: Page, vehicleId: string, eylem: string) {
  const yanit = await page.request.post(`/api/admin/vehicles/${vehicleId}/${eylem}`)

  expect(yanit.status(), `${eylem}`).toBe(204)
}

/** Kaydet → fotoğraf → yayına al → (rezerve) → satıldı; sonra aracın panel ekranı. */
async function satilmisAracaGitAsync(
  page: Page,
  onek: string,
  rezerveden = false,
  kilometre = 40_000 + Math.floor(Math.random() * 50_000),
): Promise<string> {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  const vehicleId = await aracKaydetAsync(page, benzersizSase(onek), kilometre)

  await hazirFotografAsync(page, vehicleId)
  await eylemAsync(page, vehicleId, 'publish')

  if (rezerveden) {
    await eylemAsync(page, vehicleId, 'reserve')
  }

  await eylemAsync(page, vehicleId, 'sold')
  await page.goto(`/admin/vehicles/${vehicleId}`)
  await expect(page.getByTestId('vehicle-actions')).toContainText('Satıldı')

  return vehicleId
}

/**
 * Ziyaretçi vitrininde bu kilometredeki ilan sayısı. Vitrin yanıtı araç kimliği taşımıyor;
 * kilometre her testte rastgele seçiliyor ve süzgeç tam o değere daraltılıyor — paralel
 * koşan başka bir spec'in yayına aldığı araç sayıyı değiştirmesin diye.
 */
async function vitrinSayisiAsync(page: Page, kilometre: number): Promise<number> {
  const yanit = await page.request.get(
    `/api/catalog/showcase-listings?locale=tr&pageSize=1&mileageMin=${kilometre}&mileageMax=${kilometre}`,
  )

  expect(yanit.ok()).toBe(true)

  return ((await yanit.json()) as { totalCount: number }).totalCount
}

async function pencereyiAcAsync(page: Page) {
  await page.getByTestId('vehicle-action-revert-sale').click()
  await expect(page.getByTestId('sale-revert-target')).toBeVisible()
}

test('0023/AC-1, AC-13, AC-18, AC-20: geri alma penceresi, Yayında dönüş ve kayıt', async ({
  page,
}) => {
  const kilometre = 90_001 + 2 * Math.floor(Math.random() * 4_500)

  await satilmisAracaGitAsync(page, 'K23A1', false, kilometre)

  expect(await vitrinSayisiAsync(page, kilometre)).toBe(0)

  await pencereyiAcAsync(page)

  const pencere = page.getByTestId('sale-revert-form')

  await expect(pencere).toContainText('Yayında')
  await expect(page.getByTestId('sale-revert-confirm')).toBeDisabled()
  await kanit(page, pencere, '0023-ac13.png')

  await page.getByTestId('sale-revert-reason').fill(GEREKCE)
  await expect(page.getByTestId('sale-revert-confirm')).toBeEnabled()
  await page.getByTestId('sale-revert-confirm').click()

  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()
  await expect(page.getByTestId('vehicle-actions')).toContainText('Yayında')
  await kanit(page, page.getByTestId('vehicle-actions'), '0023-ac1.png')

  const kayit = page.getByTestId('sale-reversal-record')

  await expect(kayit).toHaveCount(1)
  await expect(kayit).toContainText(GEREKCE)
  await expect(kayit).toContainText('Döndü: Yayında')
  await kanit(page, page.getByTestId('sale-reversals'), '0023-ac20.png')

  // AC-1'in ikinci yarısı: satılmışken vitrinde YOKTU, geri alındıktan sonra VAR.
  expect(await vitrinSayisiAsync(page, kilometre)).toBe(1)
})

test('0023/AC-2, AC-6: rezerveden satılan ilan pencerenin gösterdiği Rezerve durumuna döner', async ({
  page,
}) => {
  await satilmisAracaGitAsync(page, 'K23A2', true)

  await pencereyiAcAsync(page)
  await expect(page.getByTestId('sale-revert-target')).toContainText('Rezerve')
  await page.getByTestId('sale-revert-reason').fill(GEREKCE)
  await kanit(page, page.getByTestId('sale-revert-form'), '0023-ac6.png')

  await page.getByTestId('sale-revert-confirm').click()

  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()
  await expect(page.getByTestId('vehicle-actions')).toContainText('Rezerve')
  await kanit(page, page.getByTestId('vehicle-actions'), '0023-ac2.png')
})

test('0023/AC-5, AC-7: pencere açıkken hedef değişirse 409, güncel hedef ve korunmuş gerekçe; sonra Taslak', async ({
  page,
}) => {
  const vehicleId = await satilmisAracaGitAsync(page, 'K23A7')

  await pencereyiAcAsync(page)
  await expect(page.getByTestId('sale-revert-target')).toContainText('Yayında')
  await page.getByTestId('sale-revert-reason').fill(GEREKCE)

  // Pencere AÇIKKEN araç vitrine çıkamaz hâle geliyor.
  const medya = await page.request.put(`/api/admin/vehicles/${vehicleId}/media`, {
    data: { mediaAssetIds: [] },
  })

  expect(medya.status()).toBe(204)

  await page.getByTestId('sale-revert-confirm').click()

  await expect(page.getByTestId('sale-revert-target-changed')).toBeVisible()
  await expect(page.getByTestId('sale-revert-target')).toContainText('Taslak')
  await expect(page.getByTestId('sale-revert-draft-reasons')).toContainText('fotoğraf')
  await expect(page.getByTestId('sale-revert-reason')).toHaveValue(GEREKCE)
  await expect(page.getByTestId('sale-revert-form')).not.toContainText('409')
  await kanit(page, page.getByTestId('sale-revert-form'), '0023-ac7.png')

  await page.getByTestId('sale-revert-confirm').click()

  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()
  await expect(page.getByTestId('vehicle-actions')).toContainText('Taslak')
  await kanit(page, page.getByTestId('vehicle-actions'), '0023-ac5.png')
})

test('0023/AC-17: vazgeçmek ilanı değiştirmez', async ({ page }) => {
  await satilmisAracaGitAsync(page, 'K23A17')

  await pencereyiAcAsync(page)
  await page.getByTestId('sale-revert-reason').fill(GEREKCE)
  await page.getByTestId('sale-revert-cancel').click()

  await expect(page.getByTestId('sale-revert-form')).toHaveCount(0)
  await page.reload()
  await expect(page.getByTestId('vehicle-actions')).toContainText('Satıldı')
  await expect(page.getByTestId('sale-reversals-empty')).toBeVisible()
  await kanit(page, page.getByTestId('vehicle-actions'), '0023-ac17.png')
})

test('0023/AC-31: sunucu hatasında teknik ayrıntısız hata ve korunmuş gerekçe', async ({
  page,
}) => {
  await satilmisAracaGitAsync(page, 'K23A31')

  await page.route('**/revert-sale', (route) =>
    route.fulfill({
      status: 500,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 500, errorCode: 'api.unavailable', traceId: 'iz-0023' }),
    }),
  )

  await pencereyiAcAsync(page)
  await page.getByTestId('sale-revert-reason').fill(GEREKCE)
  await page.getByTestId('sale-revert-confirm').click()

  const hata = page.getByTestId('sale-revert-error')

  await expect(hata).toBeVisible()
  await expect(hata).not.toContainText('api.unavailable')
  await expect(hata).not.toContainText('iz-0023')
  await expect(page.getByTestId('sale-revert-reason')).toHaveValue(GEREKCE)
  await kanit(page, page.getByTestId('sale-revert-form'), '0023-ac31.png')
})

/** Yönetici olmayan bir personel, kendi bağlamında (paylaşılan Yönetici oturumu bozulmaz). */
async function personelSayfasiAsync(
  browser: Browser,
  yonetici: Page,
  role: string,
  dil: 'tr' | 'ar',
): Promise<{ page: Page; sir: string | null }> {
  const kimlik = `geri-alma-${role}-${Date.now()}-${Math.random().toString(36).slice(2, 6)}@ornek.com`
  const eklendi = await yonetici.request.post('/api/admin/staff', {
    data: {
      displayName: dil === 'ar' ? 'مدير المعرض' : 'Galeri Personeli',
      loginId: kimlik,
      initialPassword: 'ilk-parola-1234',
      role,
      preferredLocale: dil,
      branchId: null,
    },
  })

  expect(eklendi.status()).toBe(201)

  const page = await (await browser.newContext()).newPage()

  await girisYap(page, kimlik, 'ilk-parola-1234')
  await page.locator('#password-current').fill('ilk-parola-1234')
  await page.locator('#password-new').fill('personel-parolasi-1')
  await page.getByTestId('password-form').locator('button[type="submit"]').click()
  await expect(page.getByTestId('password-changed')).toBeVisible()
  await girisYap(page, kimlik, 'personel-parolasi-1')

  let sir: string | null = null

  if (role === 'manager') {
    await expect(page.getByTestId('two-factor-page')).toBeVisible()
    sir = await ikinciAdimKurAsync(page)
    await page.context().clearCookies()
    await girisYap(page, kimlik, 'personel-parolasi-1', sir)
  }

  await expect(page.getByTestId('admin-shell')).toBeVisible()

  return { page, sir }
}

test('0023/AC-28: Satış Danışmanı satılmış ilanda "Satışı geri al" eylemini görmez', async ({
  page,
  browser,
}) => {
  const vehicleId = await satilmisAracaGitAsync(page, 'K23A28')
  const { page: danisman } = await personelSayfasiAsync(browser, page, 'sales_advisor', 'tr')

  await danisman.goto(`/admin/vehicles/${vehicleId}`)

  await expect(danisman.getByTestId('vehicle-actions')).toContainText('Satıldı')
  await expect(danisman.getByTestId('vehicle-action-revert-sale')).toHaveCount(0)
  await expect(danisman.getByTestId('sale-reversals')).toHaveCount(0)
  await expect(danisman.getByTestId('vehicle-sold-terminal')).toContainText('Yönetici')
  await kanit(danisman, danisman.getByTestId('vehicle-actions'), '0023-ac28.png')
})

test('0023/AC-33: Arapça panelde onay penceresi ve kayıt listesi sağdan sola çalışır', async ({
  page,
  browser,
}) => {
  const vehicleId = await satilmisAracaGitAsync(page, 'K23A33')
  const { page: yonetici } = await personelSayfasiAsync(browser, page, 'manager', 'ar')

  await yonetici.goto(`/admin/vehicles/${vehicleId}`)
  await expect(yonetici.locator('html')).toHaveAttribute('dir', 'rtl')

  await yonetici.getByTestId('vehicle-action-revert-sale').click()
  await expect(yonetici.getByTestId('sale-revert-target')).toContainText('منشورة')
  await yonetici.getByTestId('sale-revert-reason').fill('تم النقر على السيارة الخطأ عن طريق الخطأ.')

  const pencere = yonetici.getByTestId('sale-revert-form')

  await expect(pencere).toHaveCSS('direction', 'rtl')
  await kanit(yonetici, pencere, '0023-ac33-ar.png')

  await yonetici.getByTestId('sale-revert-confirm').click()
  await expect(yonetici.getByTestId('sale-reversal-record')).toHaveCount(1)

  const liste = yonetici.getByTestId('sale-reversals')

  await expect(liste).toHaveCSS('direction', 'rtl')
  await expect(liste).toContainText('عاد إلى: منشورة')
  await kanit(yonetici, liste, '0023-ac33-ar-b.png')
})
