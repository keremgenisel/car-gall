import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { pngUret } from './gorsel'

/**
 * `0012b`'nin kritik akışı — `docs/testing.md` § 3'ün **üçüncü** akışı:
 * *"personel giriş yapar → araç ekler → **yayına alır → vitrinde görünür**"*.
 *
 * <p>Son iki adım bu spec'e kadar hiçbir yoldan koşturulamıyordu: `VehicleCatalog`'un
 * yazma ucu yoktu ve vitrin okuma modeline yazan tek üretim yolu geliştirme seed'iydi.</p>
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
  await page.getByLabel('Kilometre', { exact: true }).fill('48500')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
}

async function aracaGitAsync(page: Page, sase: string): Promise<string> {
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

/** Fotoğraf yükler, araca bağlar ve HAZIR olmasını bekler (0012a'nın yüzeyi). */
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

  // İşleme hattı turlarla çalışıyor; sabit bekleme makinenin hızına bağlı olurdu.
  await expect(async () => {
    await page.reload()
    await expect(page.getByText('Hazır', { exact: true })).toHaveCount(1)
  }).toPass({ timeout: 60_000 })
}

test('0012b/AC-1, 0012b/AC-41: araç yayına alınır ve vitrinde görünür', async ({ page }) => {
  const sase = benzersizSase('E2EYAY')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const vehicleId = await aracaGitAsync(page, sase)

  // Fotoğraf YOKKEN yayın reddediliyor ve eksikler LİSTE hâlinde görünüyor (AC-9).
  await page.getByTestId('vehicle-action-publish').click()

  const red = page.getByTestId('vehicle-action-error')

  await expect(red).toContainText('En az bir fotoğrafın hazır olması gerekiyor')
  await expect(page.locator('body')).not.toContainText('vehicle.cannot_publish')

  await hazirFotografAsync(page, vehicleId)

  await page.goto(`/admin/vehicles/${vehicleId}`)
  await page.getByTestId('vehicle-action-publish').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()

  // Ziyaretçi tarafı: oturum YOK ve ilan vitrinde.
  const vitrin = await page.request.get('/api/catalog/showcase-listings?locale=tr&pageSize=100')

  expect(vitrin.ok()).toBe(true)

  const sayfa = (await vitrin.json()) as { items: { slug: string }[] }

  expect(sayfa.items.some((item) => item.slug.startsWith('toyota-corolla-2021-'))).toBe(true)
})

test('0012b/AC-11, 0012b/AC-13: yayındaki ilan rezerve edilir ve satıldı işaretlenir', async ({
  page,
}) => {
  const sase = benzersizSase('E2EREZ')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const vehicleId = await aracaGitAsync(page, sase)
  await hazirFotografAsync(page, vehicleId)

  await page.goto(`/admin/vehicles/${vehicleId}`)
  await page.getByTestId('vehicle-action-publish').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()

  await page.getByTestId('vehicle-action-reserve').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()
  await expect(page.getByTestId('vehicle-actions')).toContainText('Rezerve')

  await page.getByTestId('vehicle-action-sold').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()

  // A4 — `Satıldı` terminaldir: yayına alma düğmesi HİÇ basılmıyor.
  await expect(page.getByTestId('vehicle-action-publish')).toHaveCount(0)
  await expect(page.getByTestId('vehicle-sold-terminal')).toBeVisible()
})
