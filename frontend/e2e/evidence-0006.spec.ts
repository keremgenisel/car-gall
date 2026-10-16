import { expect, test, type Page } from '@playwright/test'
import { kanit } from './kanit'
import { YONETICI_PAROLA } from './global-setup'
import {
  girisYap,
  yoneticiOlarakGirAsync as ortakYoneticiOlarakGirAsync,
  oturumuBirakAsync,
} from './giris'

/**
 * `0013` D28/D46 — giriş yolu ORTAK yardımcıya taşındı (`e2e/giris.ts`): D30 Yönetici'de
 * ikinci adımı zorunlu kıldığı için her giriş artık iki fazlı ve aynı mantığı on dört
 * dosyada ayrı ayrı yazmak on dört kopya demekti. Bu dosyanın iddiaları DEĞİŞMEDİ.
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

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

/**
 * `0013` D45'in ölçümü — panele girmesi yeterli olan testler **paylaşılan oturum
 * çerezini** kullanıyor. Her biri gerçek bir ikinci adım geçseydi hesap başına otuz
 * saniyelik pencere süiti yarım saat bekletirdi; dahası her kod denemesi R8'in
 * paylaşılan sayacını artırıyor. Giriş AKIŞINI ölçen testler gerçek girişi kullanıyor.
 */
async function yoneticiOlarakGirAsync(page: Page) {
  await ortakYoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
}

test('0006/AC-24, 0006/AC-61: oturumsuz yönetim ekranı giriş ekranına düşer (TR)', async ({
  page,
}) => {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await kanit(page, '[data-testid="login-form"]', '0006-ac24.png')
  await kanit(page, '[data-testid="login-form"]', '0006-ac61-tr.png')
})

test('0006/AC-56: giriş yapılırken düğme yükleniyor hâlinde', async ({ page }) => {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  // Yanıt bilerek geciktiriliyor: yükleniyor hâli ancak açıkken görüntülenebilir.
  await page.route('**/api/identity/sessions', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 3000))
    await route.abort()
  })

  await page.getByLabel('E-posta').fill(SEED_KIMLIK)
  await page.getByLabel('Parola').fill(YONETICI_PAROLA)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  await expect(page.getByRole('button', { name: 'Giriş yapılıyor' })).toBeDisabled()
  await kanit(page, '[data-testid="login-form"]', '0006-ac56.png')
})

test('0006/AC-57: giriş başarısız olduğunda kimlik kalır, parola temizlenir', async ({ page }) => {
  await girisYap(page, SEED_KIMLIK, 'kesinlikle-yanlis-parola')

  await expect(page.getByTestId('login-error')).toBeVisible()
  await expect(page.getByLabel('E-posta')).toHaveValue(SEED_KIMLIK)
  await expect(page.getByLabel('Parola')).toHaveValue('')

  await kanit(page, '[data-testid="login-form"]', '0006-ac57.png')
})

test('0006/AC-58: eşik kadar başarısız denemeden sonra kilitlenme metni çıkar', async ({
  page,
}) => {
  // Kayıtlı OLMAYAN bir kimlik kullanılıyor: metin AC-8 gereği onun için de aynı çıkar
  // ve gerçek bir hesabı kilitlemez.
  const kimlik = `hic-yok-${Date.now()}@ornek.com`

  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  for (let deneme = 0; deneme < 5; deneme++) {
    await page.getByLabel('E-posta').fill(kimlik)
    await page.getByLabel('Parola').fill('yanlis-parola-123')
    await page.getByRole('button', { name: 'Giriş yap' }).click()
    await expect(page.getByTestId('login-error')).toBeVisible()
  }

  await expect(page.getByTestId('login-locked')).toBeVisible()
  await kanit(page, '[data-testid="login-form"]', '0006-ac58.png')
})

test('0006/AC-32: zorunlu parola değişimi ekranı', async ({ page }) => {
  const kimlik = `zorunlu-${Date.now()}@ornek.com`

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('Yeni Personel')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill('ilk-parola-1234')
  await dugmeyeBasAsync(page, 'Personeli ekle')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, 'ilk-parola-1234')

  await expect(page.getByTestId('password-form')).toBeVisible()
  await kanit(page, '[data-testid="password-form"]', '0006-ac32.png')
})

test('0006/AC-54, 0006/AC-60, 0006/AC-61: panel personelin dilinde, personel listesiyle (TR)', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page)

  await expect(page.locator('html')).toHaveAttribute('lang', 'tr')
  await expect(page.getByTestId('staff-table')).toBeVisible()

  await kanit(page, '[data-testid="admin-shell"]', '0006-ac54.png')
  await kanit(page, '[data-testid="staff-table"]', '0006-ac60.png')
  await kanit(page, '[data-testid="add-staff-form"]', '0006-ac61-tr-panel.png')
})

test('0006/AC-55, 0006/AC-61: Arapça tercih eden personel için panel RTL (AR)', async ({
  page,
}) => {
  const kimlik = `arapca-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('موظف عربي')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByLabel('Panel dili').selectOption('ar')
  await dugmeyeBasAsync(page, 'Personeli ekle')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
  await kanit(page, '[data-testid="password-form"]', '0006-ac61-ar.png')

  await page.getByLabel('كلمة مرورك الحالية').fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة').fill(yeniParola)
  await dugmeyeBasAsync(page, 'غيّر كلمة مروري')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

  // R8/R9 — Satış Danışmanının açabileceği bir yönetim ekranı henüz yok (X5). Ekran
  // bunu SÖYLER; "getirilemedi, tekrar dene" demez.
  await expect(page.getByTestId('admin-no-access')).toBeVisible()

  await kanit(page, '[data-testid="admin-shell"]', '0006-ac55.png')
})

test('0006/AC-61: giriş ekranı İngilizce', async ({ page }) => {
  // Panel dili oturumdan gelir; oturumsuzken kurulum varsayılanıdır. İngilizce kanıt
  // için Kabul-Dili değil, İNGİLİZCE tercih eden bir personelin oturumu gerekir.
  const kimlik = `ingilizce-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('English Staff')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByLabel('Panel dili').selectOption('en')
  await dugmeyeBasAsync(page, 'Personeli ekle')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await expect(page.locator('html')).toHaveAttribute('lang', 'en')

  await kanit(page, '[data-testid="password-form"]', '0006-ac61-en.png')
})
