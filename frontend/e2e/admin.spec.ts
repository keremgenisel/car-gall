import { expect, test, type Page } from '@playwright/test'

/**
 * 0006'nın kritik akışı: personel giriş yapar → korumalı panel açılır
 * (docs/testing.md § 3, akış 3'ün ilk yarısı). Araç ekleme yarısı kendi spec'ine aittir.
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'
const SEED_PAROLA = 'gelistirme-parolasi'

/** Her koşu kendi hesabını kurar; paylaşılan duruma bağımlı test yazılmaz. */
function benzersizKimlik(onek: string) {
  return `${onek}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
}

/**
 * Girişten sonra ÜÇ sonuçtan biri gelir; hangisi olduğunu beklemek zorundayız.
 *
 * Başarılı giriş TAM GEZİNME yapar (panelin dili sunucudan basılır); beklemeden bir
 * sonraki `page.goto` çağrısı o gezinmeyi yarıda keser ve `ERR_ABORTED` verir.
 */
async function sonucBekle(page: Page) {
  const shell = page.getByTestId('admin-shell')
  const password = page.getByTestId('password-form')
  const error = page.getByTestId('login-error')

  await expect(shell.or(password).or(error)).toBeVisible()
}

async function girisYap(page: Page, kimlik: string, parola: string) {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await page.getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('Parola').fill(parola)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  await sonucBekle(page)
}

async function girisSonucuAsync(page: Page): Promise<'shell' | 'password' | 'error'> {
  if (await page.getByTestId('admin-shell').isVisible()) return 'shell'

  return (await page.getByTestId('password-form').isVisible()) ? 'password' : 'error'
}

/** Çıkış da tam gezinme yapar; giriş ekranı basılana kadar beklenir. */
async function cikisYap(page: Page) {
  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()
}

async function parolaDegistir(page: Page, mevcut: string, yeni: string) {
  await expect(page.getByTestId('password-form')).toBeVisible()

  await page.getByLabel('Mevcut parolanız').fill(mevcut)
  await page.getByLabel('Yeni parolanız').fill(yeni)
  await page.getByRole('button', { name: 'Parolamı değiştir' }).click()

  await expect(page.getByTestId('password-changed')).toBeVisible()
}

/** Kurulum hesabının parolası bir kez değişir ve sonraki testler bunu kullanır. */
const YONETICI_PAROLA = 'e2e-yonetici-parolasi'

/**
 * Çalışan bir Yönetici oturumu açar.
 *
 * Kurulum hesabı ilk girişte parola değiştirmek zorunda (D2, R10) ve bu bir kez olur.
 * Veritabanı koşular arasında yaşayabildiği için hangi parolanın geçerli olduğu önceden
 * bilinemez: önce kalıcı parola denenir, olmazsa kurulum parolasıyla girilip
 * değiştirilir. Böylece test hem taze hem devralınan veritabanında çalışır.
 */
async function yoneticiOlarakGirAsync(page: Page) {
  await girisYap(page, SEED_KIMLIK, YONETICI_PAROLA)

  if ((await girisSonucuAsync(page)) === 'shell') {
    return
  }

  await girisYap(page, SEED_KIMLIK, SEED_PAROLA)

  if ((await girisSonucuAsync(page)) === 'password') {
    await parolaDegistir(page, SEED_PAROLA, YONETICI_PAROLA)
  }

  await girisYap(page, SEED_KIMLIK, YONETICI_PAROLA)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
}

test('AC-24: oturumu olmayan biri yönetim ekranına gittiğinde giriş ekranı gelir', async ({
  page,
}) => {
  await page.goto('/admin')

  await expect(page.getByTestId('login-form')).toBeVisible()
  await expect(page.getByTestId('admin-shell')).toHaveCount(0)
})

test('AC-32: ilk parolasıyla giren personelden parolasını değiştirmesi istenir', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('yeni')
  const ilkParola = 'ilk-parola-1234'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('Yeni Personel')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByRole('button', { name: 'Personeli ekle' }).click()

  await expect(page.getByText(kimlik)).toBeVisible()

  await cikisYap(page)
  await girisYap(page, kimlik, ilkParola)

  // Değiştirmeden yönetim yüzeyinde çalışamaz: panel değil, parola ekranı açılır.
  await expect(page.getByTestId('password-form')).toBeVisible()
  await expect(page.getByTestId('admin-shell')).toHaveCount(0)
})

test('AC-1, AC-31: Yönetici giriş yapar ve personel listesini görür', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  const tablo = page.getByTestId('staff-table')

  await expect(tablo).toBeVisible()

  // Ad hem üst barda hem tabloda geçiyor; sorgu tabloyla sınırlı.
  await expect(tablo.getByRole('cell', { name: 'Kurulum Yöneticisi' })).toBeVisible()
})

test('AC-55: Arapça tercih eden personel için panel dir="rtl" ile basılır', async ({ page }) => {
  const kimlik = benzersizKimlik('arapca')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('موظف عربي')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByLabel('Panel dili').selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).click()
  await expect(page.getByText(kimlik)).toBeVisible()

  await cikisYap(page)

  // İlk giriş TÜRKÇE giriş ekranından yapılır (oturum yokken kurulum varsayılanı),
  // ama oturum açılınca panel personelin diline geçer.
  await girisYap(page, kimlik, ilkParola)

  // Giriş TAM GEZİNME yapıyor ve sunucu kabuğu personelin diliyle basıyor: zorunlu
  // parola ekranı da Arapça gelir. Kriterin söylediği şey tam olarak budur.
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  await page.getByLabel('كلمة مرورك الحالية').fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة').fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  // Yön locale'in özelliğidir ve SUNUCUDAN basılır.
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  // AC-25/AC-26'nın uç tarafındaki karşılığı: Satış Danışmanına personel yönetimi
  // gösterilmez ve ekran bunu bir HATA gibi sunmaz.
  await expect(page.getByTestId('admin-no-access')).toBeVisible()
  await expect(page.getByTestId('staff-error')).toHaveCount(0)
  await expect(page.getByTestId('add-staff-form')).toHaveCount(0)
})

test('AC-54: ziyaretçinin vitrin dili paneli etkilemez', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  // Arapça vitrine uğra, sonra panele dön.
  await page.goto('/ar/')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

  await page.goto('/admin')
  await expect(page.getByTestId('admin-shell')).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('lang', 'tr')
})
