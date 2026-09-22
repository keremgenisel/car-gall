import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'

/**
 * 0006'nın kritik akışı: personel giriş yapar → korumalı panel açılır
 * (docs/testing.md § 3, akış 3'ün ilk yarısı). Araç ekleme yarısı kendi spec'ine aittir.
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

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
  await dugmeyeBasAsync(page, 'Parolamı değiştir')

  await expect(page.getByTestId('password-changed')).toBeVisible()
}

/**
 * Çalışan bir Yönetici oturumu açar.
 *
 * Parola `global-setup.ts`'te, testler başlamadan önce ve TEK SEFERDE çalışır hâle
 * getirildi. Buradaki fallback'i (önce dene, olmazsa değiştir) kaldırmak zorundaydık:
 * her deneme bir BAŞARISIZ GİRİŞ sayılıyor ve eşiğe ulaşınca hesap kilitleniyordu
 * (R6). İki profil × on sekiz çağrı, CI'da tam olarak bunu yaptı.
 */
/**
 * Düğmeyi KLAVYEYLE etkinleştirir.
 *
 * Mobil emülasyonda Playwright'ın tıklama koordinatı sayfanın kendi koordinatlarıyla
 * ayrışıyor: isabet testi her seferinde düğmenin ÜSTÜNDEKİ başka bir öğeyi buluyor
 * (`#staff-role`, sonra `label[for=staff-locale]`). Ölçüldü — kutular çakışmıyor ve
 * `window.innerHeight` (998) yapılandırılan viewport'tan (915) farklı. Yani bu bir
 * yerleşim hatası DEĞİL, mobil emülasyonun viewport farkıdır.
 *
 * Klavyeyle etkinleştirme koordinata hiç bağlı değil ve uydurma bir yol da değil:
 * `docs/frontend.md` § 7 her etkileşimli öğenin Enter/Space ile çalışmasını ZORUNLU
 * kılıyor. Yani test, kaçındığı sorunun yerine gerçek bir gereksinimi doğruluyor.
 */
async function dugmeyeBasAsync(page: Page, ad: string) {
  await page.getByRole('button', { name: ad }).press('Enter')
}

async function yoneticiOlarakGirAsync(page: Page) {
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
  await dugmeyeBasAsync(page, 'Personeli ekle')

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
  await dugmeyeBasAsync(page, 'Personeli ekle')
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
  await dugmeyeBasAsync(page, 'غيّر كلمة مروري')
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
