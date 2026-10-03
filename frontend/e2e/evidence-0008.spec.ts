import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
import { YONETICI_PAROLA } from './global-setup'

const SEED_KIMLIK = 'yonetici@cargallery.local'

/**
 * 0008'in görsel kanıtı ve gerçek tarayıcıda ölçülebilen iki kriter.
 *
 * Yazma AÇIKÇA istenir (`npm run evidence`, docs/testing.md § 3); normal e2e koşusunda
 * buradaki `expect`'ler çalışır ama dosya yazılmaz.
 */

async function girisYap(page: Page, kimlik: string, parola: string) {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await page.getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('Parola').fill(parola)
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

/**
 * Kendi satırı dışında bir satır — testin KENDİ kurduğu.
 *
 * Seed veritabanında yalnız kurulum hesabı var; "listede başka biri de vardır" varsayımı
 * paylaşılan duruma bağımlılık olurdu ve `docs/testing.md` § 4 bunu yasaklıyor. Her test
 * kendi personelini ekliyor ve kendi satırını geri alıyor.
 */
async function baskaSatirAsync(page: Page) {
  const kimlik = `kanit-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`

  await page.getByLabel('Ad soyad').fill('Kanıt Personeli')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill('ilk-parola-1234')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')

  const satir = page.getByTestId('staff-table').locator('tbody tr').filter({ hasText: kimlik })

  await expect(satir).toBeVisible()

  return satir
}

test('0008/AC-1, 0008/AC-15: her satırda üç eylem ve kendi satırı işaretli', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  const tablo = page.getByTestId('staff-table')

  await expect(tablo).toBeVisible()
  await expect(page.getByTestId('staff-row-self')).toHaveCount(1)

  await kanit(page, '[data-testid="staff-table"]', '0008-ac1.png')
  await kanit(page, '[data-testid="staff-row-self"]', '0008-ac15.png')
})

test('0008/AC-5: pasifleştirme onay ister ve personelin adını yazar', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  const satir = await baskaSatirAsync(page)
  const ad = ((await satir.locator('td').first().textContent()) ?? '').trim()

  await satir.getByRole('button', { name: 'Pasifleştir' }).press('Enter')

  const kutu = page.getByTestId('confirm-deactivate')

  await expect(kutu).toBeVisible()
  await expect(kutu).toContainText(ad)

  await kanit(page, '[data-testid="confirm-deactivate"]', '0008-ac5.png')
})

test('0008/AC-16: kendi hesabını pasifleştirme onayı erişim uyarısı taşır', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page
    .getByTestId('staff-row-self')
    .getByRole('button', { name: 'Pasifleştir' })
    .press('Enter')

  await expect(page.getByTestId('confirm-self-warning')).toBeVisible()
  await kanit(page, '[data-testid="confirm-deactivate"]', '0008-ac16.png')
})

test('0008/AC-9: kendi rolünü düşürme onayı erişim uyarısı taşır', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page
    .getByTestId('staff-row-self')
    .getByRole('button', { name: 'Rolü değiştir' })
    .press('Enter')

  const kutu = page.getByTestId('confirm-role')

  await expect(kutu).toBeVisible()

  // Yönetici kaldığı sürece uyarı yok; sonucu ağır olan, rolden çıkmak.
  await expect(page.getByTestId('confirm-self-warning')).toHaveCount(0)

  await kutu.getByLabel('Rol').selectOption('content_editor')

  await expect(page.getByTestId('confirm-self-warning')).toBeVisible()
  await kanit(page, '[data-testid="confirm-role"]', '0008-ac9.png')
})

test('0008/AC-11: parola sıfırlama iki alan ister', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  const satir = await baskaSatirAsync(page)

  await satir.getByRole('button', { name: 'Parolayı sıfırla' }).press('Enter')

  const kutu = page.getByTestId('confirm-password')

  await expect(kutu.getByLabel('Yeni parola', { exact: true })).toBeVisible()
  await expect(kutu.getByLabel('Yeni parola (tekrar)')).toBeVisible()

  await kanit(page, '[data-testid="confirm-password"]', '0008-ac11.png')
})

/**
 * Kural gereği red GERÇEK bir yarışla üretilemez: kurulum hesabı tek Yönetici olmayabilir
 * ve kanıt koşusu veriye bağımlı hâle gelirdi. Yanıt sahteleniyor — ölçülen şey sunucunun
 * kararı değil (onu 0006 ve 0007 ölçtü), EKRANIN o koda verdiği tepki.
 */
test('0008/AC-18: kural gereği red tekrar denemeyi önermez', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/staff/*/deactivate', async (route) => {
    await route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 409, errorCode: 'identity.last_active_manager' }),
    })
  })

  const satir = await baskaSatirAsync(page)

  await satir.getByRole('button', { name: 'Pasifleştir' }).press('Enter')
  await page.getByTestId('confirm-deactivate-submit').press('Enter')

  const hata = page.getByTestId('staff-action-error')

  await expect(hata).toBeVisible()
  await expect(hata).not.toContainText('tekrar')

  await kanit(page, '[data-testid="staff-action-error"]', '0008-ac18.png')
})

test('0008/AC-19: eşzamanlılık kaybı tekrar denemeyi önerir', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/staff/*/deactivate', async (route) => {
    await route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 409, errorCode: 'concurrency.lost' }),
    })
  })

  const satir = await baskaSatirAsync(page)

  await satir.getByRole('button', { name: 'Pasifleştir' }).press('Enter')
  await page.getByTestId('confirm-deactivate-submit').press('Enter')

  const hata = page.getByTestId('staff-action-error')

  await expect(hata).toBeVisible()
  await expect(hata).toContainText('tekrar deneyin')

  await kanit(page, '[data-testid="staff-action-error"]', '0008-ac19.png')
})

/**
 * AC-23'ün jsdom'da ölçülemeyen yarısı: kaydırma konumu. jsdom'un yerleşimi yok ve
 * `scrollTop` orada her zaman sıfır; bu iddia ancak gerçek tarayıcıda kurulabilir.
 */
test('0008/AC-23: başarılı işlemden sonra kaydırma konumu korunur', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await expect(page.getByTestId('staff-table')).toBeVisible()

  await page.route('**/api/admin/staff/*/deactivate', async (route) => {
    await route.fulfill({ status: 204, body: '' })
  })

  const satir = await baskaSatirAsync(page)

  /*
   * Görünüm alanı KASTEN kısaltılıyor. İlk hâli sayfanın uzun olmasına güveniyordu ve
   * benim geliştirme veritabanım birikmiş satırlarla uzundu; CI'ın temiz veritabanında
   * tablo ekrana sığıyor, `scrollY` hiç sıfırdan çıkmıyor ve test "kaydırılamıyor" diye
   * düşüyordu. Testin kaydırılabilir bir sayfaya ihtiyacı var ama o sayfanın KAÇ SATIR
   * içerdiğine ihtiyacı yok (docs/testing.md § 4).
   */
  await page.setViewportSize({ width: 900, height: 300 })

  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight))
  await satir.getByRole('button', { name: 'Pasifleştir' }).press('Enter')
  await expect(page.getByTestId('confirm-deactivate')).toBeVisible()

  /*
   * Ölçüm kutu AÇILDIKTAN SONRA alınıyor. Sebep testin kendisinde: Playwright `press()`
   * öncesinde hedefi görünür alana çekiyor ve sayfayı kaydırıyor (ölçüldü: 2279 → 1436).
   * O hareket ÜRÜNÜN davranışı değil, sürücünün. Kriterin iddiası "konum eylem boyunca
   * KORUNUR"; başlangıcı eylemin hemen öncesinde sabitlemek o iddiayı tam olarak ölçer.
   */
  const once = await page.evaluate(() => window.scrollY)

  // Sayfa kaydırılamıyorsa iddia bedava doğru olurdu.
  expect(once).toBeGreaterThan(0)

  await page.getByTestId('confirm-deactivate-submit').press('Enter')

  await expect(page.getByTestId('staff-table')).toBeVisible()
  await expect(page.getByTestId('confirm-deactivate')).toHaveCount(0)

  expect(await page.evaluate(() => window.scrollY)).toBe(once)
})

test('0008/AC-32: liste yüklenirken iskelet, yüklenemediğinde tekrar dene', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/staff', async (route) => {
    await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{}' })
  })

  await page.reload()

  await expect(page.getByTestId('staff-error')).toBeVisible()
  await kanit(page, '[data-testid="staff-error"]', '0008-ac32.png')
})

test('0008/AC-4: Yönetici olmayan personel personel yönetimine götüren hiçbir şey görmez', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page)

  const kimlik = `danisman-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'danisman-parola-12'

  await page.getByLabel('Ad soyad').fill('Satış Danışmanı')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('Mevcut parolanız').fill(ilkParola)
  await page.getByLabel('Yeni parolanız').fill(yeniParola)
  await page.getByRole('button', { name: 'Parolamı değiştir' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)

  await expect(page.getByTestId('admin-no-access')).toBeVisible()
  await expect(page.getByTestId('staff-table')).toHaveCount(0)
  await expect(page.getByTestId('add-staff-form')).toHaveCount(0)
  await expect(page.locator('[data-testid^="deactivate-"]')).toHaveCount(0)
})

test('0008/AC-31: Arapça tercih eden Yönetici için ekran ve onay kutusu RTL', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  const kimlik = `arapca-yonetici-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await page.getByLabel('Ad soyad').fill('مدير عربي')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  // `getByLabel` alt dize eşleştirir ve 'Rol', 'İlk parola' içinde geçer.
  await page.getByLabel('Rol', { exact: true }).selectOption('manager')
  await page.getByLabel('Panel dili').selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('كلمة مرورك الحالية').fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة').fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)

  // Yön locale'in özelliğidir ve SUNUCUDAN basılır (docs/frontend.md § 6).
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByTestId('staff-table')).toBeVisible()

  await page.getByTestId('staff-row-self').getByRole('button', { name: 'إيقاف' }).press('Enter')

  await expect(page.getByTestId('confirm-deactivate')).toBeVisible()
  await kanitSayfa(page, '0008-ac31.png')
})
