import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync as ortakYoneticiOlarakGirAsync } from './giris'

/**
 * 0010a'nın kritik akışı: personel giriş yapar → araç kaydeder → kayıt listede görünür
 * (docs/testing.md § 3, akış 3'ün ikinci yarısı — `admin.spec.ts` ilk yarısını koşuyor).
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

/** Her koşu KENDİ şasesini üretir: şase galeri genelinde tekil (A5) ve çakışan koşu reddedilir. */
function benzersizSase(onek: string) {
  return `${onek}${Date.now()}`.slice(0, 32)
}

/**
 * `0013` D28/D46 — giriş yolu ortak yardımcıya taşındı (`e2e/giris.ts`): Yönetici'de
 * ikinci adım zorunlu (D30) ve giriş artık iki fazlı. Bu dosyanın iddiaları DEĞİŞMEDİ.
 */
async function yoneticiOlarakGirAsync(page: Page) {
  await ortakYoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
}

/**
 * Formu çekirdek alanların hepsiyle doldurur.
 *
 * Seçenekler İSİMLE değil SIRAYLA seçiliyor: referans veri kurulumla gelen bir kümedir
 * ve etiketleri seed'e bağlamak, seed büyüdüğünde kırılan bir test üretirdi. Önemli olan
 * "personel listeden bir şey seçebiliyor mu" — hangi şeyi seçtiği değil.
 */
async function formuDoldurAsync(page: Page, sase: string) {
  for (const alan of ['Şube', 'Model', 'Kasa tipi', 'Donanım paketi', 'Yakıt', 'Vites', 'Çekiş']) {
    const secim = page.getByLabel(alan, { exact: true })
    await expect(secim).toBeVisible()

    const deger = await secim.locator('option').nth(1).getAttribute('value')

    expect(deger, alan).not.toBeNull()
    await secim.selectOption(deger!)
  }

  await page.getByLabel('Model yılı', { exact: true }).fill('2021')
  await page.getByLabel('Kilometre', { exact: true }).fill('48500')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')
}

test('0010a/AC-1, 0010a/AC-26: oturum açmış personel araç kaydeder ve kayıt listede görünür', async ({
  page,
}) => {
  const sase = benzersizSase('E2EAC01')

  await yoneticiOlarakGirAsync(page)

  await page.getByRole('link', { name: 'Araçlar' }).click()
  await expect(
    page.getByTestId('vehicle-table').or(page.getByTestId('vehicle-empty')),
  ).toBeVisible()

  await page.getByRole('link', { name: 'Araç ekle' }).click()
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await formuDoldurAsync(page, sase)

  // AC-41 — kaydedildiği personele bildirilir.
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  await page.getByRole('link', { name: 'Araç listesine git' }).click()

  const satir = page.getByTestId('vehicle-table').locator('tbody tr').first()

  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  // AC-26 — araç tanımı, fiyat ve kilometre. Liste en yeni kaydı önde göstermiyor
  // (sıralama 0010b'nin işi), bu yüzden kendi kaydımız METİNLE aranıyor.
  const bizim = page
    .getByTestId('vehicle-table')
    .locator('tbody tr')
    .filter({ hasText: '48.500 km' })
    .first()

  await expect(bizim).toBeVisible()
  await expect(bizim).toContainText('2021')
  await expect(satir).toBeVisible()
})

test('0010a/AC-16: galeride aynı şase numarasına sahip ikinci bir araç kaydedilemez', async ({
  page,
}) => {
  const sase = benzersizSase('E2EAC16')

  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await formuDoldurAsync(page, sase)
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  // İkinci kez AYNI şase: alan işaretlenir ve mesaj personelin dilinde yazılır (AC-42).
  await formuDoldurAsync(page, sase)
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')

  await expect(page.getByLabel('Şase numarası', { exact: true })).toHaveAttribute(
    'aria-invalid',
    'true',
  )
  await expect(page.getByText('Bu şase numarasıyla kayıtlı bir araç zaten var.')).toBeVisible()

  // AC-47 — teknik detay sızmaz.
  await expect(page.locator('body')).not.toContainText('409')
  await expect(page.locator('body')).not.toContainText('vehicle.duplicate_vin')
})

test('0010a/AC-11: taslak durumundaki ilan vitrin listesinde görünmez', async ({ page }) => {
  const sase = benzersizSase('E2EAC11')

  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await formuDoldurAsync(page, sase)
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  // Panelde TASLAK olarak duruyor. 48.500 km seed'in ürettiği hiçbir değere denk
  // gelmiyor (seed: 45.000 + index × 1.137), yani satır bu koşuya ait.
  await page.goto('/admin/vehicles')
  await expect(
    page.getByTestId('vehicle-table').locator('tbody tr').filter({ hasText: '48.500 km' }).first(),
  ).toContainText('Taslak')

  // Vitrinde YOK. Ölçüt sayfadaki kartlara bakmak değil — vitrin 200 ilanla sayfalanmış
  // ve "bu kartlar arasında yok" yalnız ilk sayfayı kanıtlardı. Vitrin ilan SAYISI
  // kayıttan sonra değişmiyorsa taslak o listeye hiç girmemiştir.
  const oncesi = await sayfalananToplamAsync(page)

  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()
  await formuDoldurAsync(page, benzersizSase('E2EAC11B'))
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  expect(await sayfalananToplamAsync(page)).toBe(oncesi)
})

/** Vitrin listesinin toplam ilan sayısı; taslak ilan buraya girmez (A3). */
async function sayfalananToplamAsync(page: Page): Promise<number> {
  const yanit = await page.request.get('/api/catalog/showcase-listings?page=1')

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { totalCount: number }

  return govde.totalCount
}
