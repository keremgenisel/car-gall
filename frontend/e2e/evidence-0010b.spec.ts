import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
import { YONETICI_PAROLA } from './global-setup'

/**
 * 0010b'nin görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * docs/testing.md § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 *
 * <p><b>Ayar değişikliği isteyen kanıtlar STUB'lu</b> ve bu C24'ün doğrudan sonucu:
 * alan ayarlarını yazan hiçbir uç yok (X21). Ekran gerçek, veri sahte — davranışın
 * kendisi entegrasyon testlerinde veri katmanından kuruluyor.</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizKimlik(onek: string) {
  return `${onek}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
}

async function girisYap(page: Page, kimlik: string, parola: string) {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await page.getByLabel('E-posta', { exact: true }).fill(kimlik)
  await page.getByLabel('Parola', { exact: true }).fill(parola)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  const shell = page.getByTestId('admin-shell')
  const password = page.getByTestId('password-form')
  const error = page.getByTestId('login-error')

  await expect(shell.or(password).or(error)).toBeVisible()
}

async function yoneticiOlarakGirAsync(page: Page) {
  await girisYap(page, SEED_KIMLIK, YONETICI_PAROLA)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
}

/** Listedeki ilk aracın düzenleme ekranı. */
async function ilkAracinDuzenlemeEkraniAsync(page: Page) {
  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  await page.getByTestId('vehicle-table').locator('tbody tr').first().getByRole('link').click()
  await expect(page.getByTestId('vehicle-edit-form')).toBeVisible()
}

test('0010b/AC-24: liste yedi sütunuyla', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicles?page=1&sort=last-updated')

  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  await expect(page.getByTestId('vehicle-summary')).toBeVisible()

  // `exact` ZORUNLU oldu: `0012c` listeye bir seçim sütunu ekledi ve onun başlığı
  // "Sayfadaki araçların hepsini seç". Gevşek eşleşme "Araç" için iki başlık buluyor ve
  // locator katı kipte patlıyor. Kriterin iddiası değişmedi — altı başlık hâlâ yerinde.
  for (const baslik of ['Araç', 'Fiyat', 'Kilometre', 'Durum', 'Tamlık', 'Son güncelleme']) {
    await expect(page.getByRole('columnheader', { name: baslik, exact: true })).toBeVisible()
  }

  await kanitSayfa(page, '0010b-ac24.png')
})

test('0010b/AC-22, 0010b/AC-23: eksik dil göstergesi ve çeviri özeti', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicles?page=1&sort=last-updated')

  // Seed her beşinci araçta Arapça metni BİLEREK eksik bırakıyor; gösterge gerçek
  // veriyle görünür.
  await expect(page.getByTestId('vehicle-missing-locales').first()).toBeVisible()
  await expect(page.getByTestId('vehicle-summary')).toContainText('çeviri eksik')

  await kanit(page, page.getByTestId('vehicle-table'), '0010b-ac22.png')
})

test('0010b/AC-13: açık opsiyonel alan formda görünür', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await ilkAracinDuzenlemeEkraniAsync(page)

  const alanlar = page.getByTestId('vehicle-optional-fields')

  await expect(alanlar).toBeVisible()
  await expect(page.getByLabel('Tramer tutarı', { exact: true })).toBeVisible()

  await kanit(page, alanlar, '0010b-ac13.png')
})

test('0010b/AC-14: kapalı opsiyonel alan formda görünmez', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  // Alanı KAPATAN bir uç yok (X21); ayar yanıtı sahteleniyor, ekran gerçek (C24).
  await page.route('**/api/admin/vehicle-fields*', async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' })
  })

  await ilkAracinDuzenlemeEkraniAsync(page)

  await expect(page.getByTestId('vehicle-optional-fields')).toHaveCount(0)
  await expect(page.getByLabel('Tramer tutarı', { exact: true })).toHaveCount(0)

  await kanit(page, page.getByTestId('vehicle-edit-form'), '0010b-ac14.png')
})

test('0010b/AC-16: zorunlu alanı boş olan araç listede eksik bilgi olarak işaretlenir', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page)

  // Alanı ZORUNLU yapan bir uç yok (X21). Göstergenin kendisi entegrasyon testinde
  // veri katmanından kuruluyor; buradaki kanıt ekranın o bayrağı nasıl gösterdiği.
  await page.route('**/api/admin/vehicles?*', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    const yanit = await route.fetch()
    const govde = (await yanit.json()) as {
      items: { hasMissingRequiredInfo: boolean; missingLocales: string[] }[]
    }

    govde.items = govde.items.slice(0, 3).map((item, index) => ({
      ...item,
      hasMissingRequiredInfo: index === 0,
      missingLocales: index === 1 ? ['ar'] : [],
    }))

    await route.fulfill({ response: yanit, json: govde })
  })

  await page.goto('/admin/vehicles?page=1&sort=last-updated')

  await expect(page.getByTestId('vehicle-missing-info')).toBeVisible()
  await expect(page.getByTestId('vehicle-missing-info')).toContainText('Eksik bilgi')

  await kanit(page, page.getByTestId('vehicle-table'), '0010b-ac16.png')
})

test('0010b/AC-30: liste yüklenirken iskelet', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/vehicles?*', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    await new Promise((resolve) => setTimeout(resolve, 2_000))
    await route.fallback()
  })

  await page.goto('/admin/vehicles')

  await expect(page.getByTestId('vehicle-skeleton')).toBeVisible()
  await kanitSayfa(page, '0010b-ac30.png')

  await expect(page.getByTestId('vehicle-table')).toBeVisible({ timeout: 10_000 })
})

test('0010b/AC-31: hiç araç yokken boş durum ve araç ekle eylemi', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  // Geliştirme veritabanında seed araçları var; BOŞ durum ancak uç boş dönerse görülür.
  await page.route('**/api/admin/vehicles?*', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 1 }),
    })
  })

  await page.goto('/admin/vehicles')

  const bos = page.getByTestId('vehicle-empty')

  await expect(bos).toBeVisible()
  await expect(bos.getByRole('link', { name: 'Araç ekle' })).toBeVisible()

  await kanit(page, bos, '0010b-ac31.png')
})

test('0010b/AC-32, 0010b/AC-33: liste hatası tekrar dene sunar ve teknik detay sızdırmaz', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/vehicles?*', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    await route.fulfill({
      status: 500,
      contentType: 'application/problem+json',
      body: JSON.stringify({ errorCode: 'vehicle.exploded', traceId: 'trace-0010b' }),
    })
  })

  await page.goto('/admin/vehicles')

  const hata = page.getByTestId('vehicle-error')

  await expect(hata).toBeVisible()
  await expect(hata).toContainText('Tekrar dene')

  for (const sizinti of ['500', 'trace-0010b', 'vehicle.exploded', '/api/admin']) {
    await expect(page.locator('body'), sizinti).not.toContainText(sizinti)
  }

  await kanit(page, hata, '0010b-ac32.png')
})

test('0010b/AC-34, 0010b/AC-35: Arapça personel için düzenleme formu ve liste RTL', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('arapca-duzenleme')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad', { exact: true }).fill('محرر المركبات')
  await page.getByTestId('add-staff-form').getByLabel('E-posta', { exact: true }).fill(kimlik)
  await page.getByLabel('İlk parola', { exact: true }).fill(ilkParola)
  await page.getByLabel('Panel dili', { exact: true }).selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('كلمة مرورك الحالية', { exact: true }).fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة', { exact: true }).fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  // Liste: başlıklar Arapça, yön sunucudan.
  await page.goto('/admin/vehicles')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
  await expect(page.getByRole('columnheader', { name: 'آخر تحديث' })).toBeVisible()
  await expect(page.locator('body')).not.toContainText('vehicle.column')

  await kanitSayfa(page, '0010b-ac35-ar.png')

  // Düzenleme formu.
  await page.getByTestId('vehicle-table').locator('tbody tr').first().getByRole('link').click()
  await expect(page.getByTestId('vehicle-edit-form')).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByLabel('رقم الهيكل', { exact: true })).toBeVisible()
  await expect(page.locator('body')).not.toContainText('vehicle.edit.')

  await kanitSayfa(page, '0010b-ac34.png')
})

test('0010b/AC-35: liste başlıkları İngilizce tercih eden personelde İngilizce', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('ingilizce-duzenleme')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'ingilizce-parola-1'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad', { exact: true }).fill('Vehicle Editor')
  await page.getByTestId('add-staff-form').getByLabel('E-posta', { exact: true }).fill(kimlik)
  await page.getByLabel('İlk parola', { exact: true }).fill(ilkParola)
  await page.getByLabel('Panel dili', { exact: true }).selectOption('en')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('Your current password', { exact: true }).fill(ilkParola)
  await page.getByLabel('Your new password', { exact: true }).fill(yeniParola)
  await page.getByRole('button', { name: 'Change my password' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await page.goto('/admin/vehicles')

  await expect(page.getByRole('columnheader', { name: 'Last updated' })).toBeVisible()
  await expect(page.locator('body')).not.toContainText('vehicle.column')

  await kanitSayfa(page, '0010b-ac35-en.png')
})
