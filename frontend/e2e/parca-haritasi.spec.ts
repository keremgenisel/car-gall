import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { girisYap, oturumuBirakAsync, yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'
import { modeliOlanMarkayiSecAsync } from './marka'

/**
 * 0016a — panelde parça haritası, gerçek Host'a karşı: 13 kutu, yarım haritanın reddi ve
 * eksiklerin işaretlenmesi, düzeltip kaydetme, temizleme. Sunucu kuralları xUnit'te
 * (`ParcaHaritasiTests`); burada ölçülen tarayıcının gördüğü.
 */
const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

async function aracKaydetVeDuzenlemeyeGitAsync(page: Page, onek: string): Promise<void> {
  const sase = benzersizSase(onek)
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()
  for (const alan of [
    'Şube',
    'Marka',
    'Model',
    'Kasa tipi',
    'Donanım paketi',
    'Yakıt',
    'Vites',
    'Çekiş',
  ]) {
    if (alan === 'Marka') {
      await modeliOlanMarkayiSecAsync(page)
      continue
    }

    const secim = page.getByLabel(alan, { exact: true })
    await secim.selectOption((await secim.locator('option').nth(1).getAttribute('value'))!)
  }
  await page.getByLabel('Model yılı', { exact: true }).fill('2021')
  await page.getByLabel('Kilometre', { exact: true }).fill('48500')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
  await duzenlemeyeGitAsync(page, sase)
}

async function duzenlemeyeGitAsync(page: Page, sase: string): Promise<void> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )
  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)
  expect(arac, `şase ${sase} listede yok`).toBeTruthy()
  await page.goto(`/admin/vehicles/${arac!.vehicleId}`)
  await expect(page.getByTestId('vehicle-edit-form')).toBeVisible()
}

function kutular(page: Page) {
  return page.getByTestId('paint-change-map-input').locator('select')
}

async function onBirini(page: Page, durum: string): Promise<void> {
  const sayi = await kutular(page).count()
  for (let i = 0; i < sayi; i += 1) {
    const kutu = kutular(page).nth(i)
    const parca = await kutu.getAttribute('data-part')
    if (parca !== 'roof' && parca !== 'right_rear_door') {
      await kutu.selectOption(durum)
    }
  }
}

test('0016a/AC-29, AC-30, AC-31: yarım harita reddedilir ve eksikler işaretlenir; düzeltilip kaydedilir; temizlenince form boş açılır', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  await aracKaydetVeDuzenlemeyeGitAsync(page, 'E2EPH')

  await expect(kutular(page)).toHaveCount(13)
  await onBirini(page, 'painted')
  await page.getByTestId('vehicle-edit-submit').click()

  // AC-29 — eksikler adıyla, C10 sırasıyla; yalnız iki kutu hatalı.
  await expect(page.getByTestId('paint-change-missing')).toHaveText(
    'Harita yarım kaldı. Şu parçalar için durum seçin: Tavan ve Sağ arka kapı.',
  )
  await expect(
    page.getByTestId('paint-change-map-input').locator('select[aria-invalid="true"]'),
  ).toHaveCount(2)
  await kanit(page, '[data-testid="paint-change-map-input"]', '0016a-ac29.png')

  // AC-30 — seçimler korunur; iki kutu doldurulunca kabul.
  const korunan = await kutular(page).evaluateAll(
    (liste) => liste.filter((kutu) => (kutu as HTMLSelectElement).value === 'painted').length,
  )
  expect(korunan).toBe(11)
  await page.getByLabel('Tavan', { exact: true }).selectOption('original')
  await page.getByLabel('Sağ arka kapı', { exact: true }).selectOption('replaced')
  await page.getByTestId('vehicle-edit-submit').click()
  await expect(page.getByTestId('vehicle-edit-saved')).toBeVisible()

  await page.reload()
  await expect(page.getByLabel('Ön tampon', { exact: true })).toHaveValue('painted')
  await expect(page.getByLabel('Sağ arka kapı', { exact: true })).toHaveValue('replaced')
  await kanit(page, '[data-testid="paint-change-map-input"]', '0016a-ac26.png')

  // AC-31 — 13 kutu boşaltılıp kaydedilince temizlenir ve form boş açılır.
  for (let i = 0; i < 13; i += 1) {
    await kutular(page).nth(i).selectOption('')
  }
  await page.getByTestId('vehicle-edit-submit').click()
  await expect(page.getByTestId('vehicle-edit-saved')).toBeVisible()

  await page.reload()
  for (let i = 0; i < 13; i += 1) {
    await expect(kutular(page).nth(i)).toHaveValue('')
  }
})

test('0016a/AC-32: /ar/ panelinde parça adları, durum adları ve eksik parça hatası Arapça; form sağdan sola akar', async ({
  page,
}, testInfo) => {
  test.skip(
    testInfo.project.name !== 'masaustu',
    'Kanıt tek profilde üretilir; Arapça personel her koşuda yeniden kurulur.',
  )

  const kimlik = `ph-arapca-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
  const ilk = 'ilk-parola-1234'
  const yeni = 'arapca-parola-12'

  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  await aracKaydetVeDuzenlemeyeGitAsync(page, 'E2EPHAR')
  const adres = new URL(page.url()).pathname

  await page.goto('/admin')
  await page.getByLabel('Ad soyad').fill('موظف الخريطة')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilk)
  await page.getByLabel('Panel dili').selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()
  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilk)
  await page.getByLabel('كلمة مرورك الحالية').fill(ilk)
  await page.getByLabel('كلمة مرورك الجديدة').fill(yeni)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()
  await girisYap(page, kimlik, yeni)

  await page.goto(adres)
  await expect(page.getByTestId('vehicle-edit-form')).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByLabel('المصد الأمامي', { exact: true })).toBeVisible()
  await expect(page.getByLabel('المصد الأمامي', { exact: true }).locator('option')).toHaveText([
    '— لم يُحدَّد —',
    'أصلي',
    'مطلي جزئياً',
    'مطلي',
    'مُستبدَل',
  ])

  await onBirini(page, 'original')
  await page.getByTestId('vehicle-edit-submit').click()
  await expect(page.getByTestId('paint-change-missing')).toContainText('السقف')
  await expect(page.getByTestId('paint-change-missing')).toContainText('الباب الخلفي الأيمن')

  await kanit(page, '[data-testid="paint-change-map-input"]', '0016a-ac32-ar.png')
})
