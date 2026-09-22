import { expect, test, type Page } from '@playwright/test'

/**
 * Spec 0006'nın görsel kanıtları (docs/testing.md § 3). Görüntü KRİTERİN karşılandığını
 * gösterir, ekranın genel görünümünü değil. Yalnız masaüstü profilinde koşar — mobil
 * profil aynı dosyaları üzerine yazardı (playwright.config.ts testIgnore).
 */
const KANIT = '../docs/evidence'

const SEED_KIMLIK = 'yonetici@cargallery.local'
const SEED_PAROLA = 'gelistirme-parolasi'
const YONETICI_PAROLA = 'e2e-yonetici-parolasi'

async function kanit(page: Page, selector: string, dosya: string) {
  await page
    .locator(selector)
    .first()
    .screenshot({ path: `${KANIT}/${dosya}` })
}

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

async function yoneticiOlarakGirAsync(page: Page) {
  await girisYap(page, SEED_KIMLIK, YONETICI_PAROLA)

  if (await page.getByTestId('admin-shell').isVisible()) {
    return
  }

  await girisYap(page, SEED_KIMLIK, SEED_PAROLA)

  if (await page.getByTestId('password-form').isVisible()) {
    await page.getByLabel('Mevcut parolanız').fill(SEED_PAROLA)
    await page.getByLabel('Yeni parolanız').fill(YONETICI_PAROLA)
    await page.getByRole('button', { name: 'Parolamı değiştir' }).click()
    await expect(page.getByTestId('password-changed')).toBeVisible()
  }

  await girisYap(page, SEED_KIMLIK, YONETICI_PAROLA)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
}

test('AC-24, AC-61: oturumsuz yönetim ekranı giriş ekranına düşer (TR)', async ({ page }) => {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await kanit(page, '[data-testid="login-form"]', '0006-ac24.png')
  await kanit(page, '[data-testid="login-form"]', '0006-ac61-tr.png')
})

test('AC-56: giriş yapılırken düğme yükleniyor hâlinde', async ({ page }) => {
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

test('AC-57: giriş başarısız olduğunda kimlik kalır, parola temizlenir', async ({ page }) => {
  await girisYap(page, SEED_KIMLIK, 'kesinlikle-yanlis-parola')

  await expect(page.getByTestId('login-error')).toBeVisible()
  await expect(page.getByLabel('E-posta')).toHaveValue(SEED_KIMLIK)
  await expect(page.getByLabel('Parola')).toHaveValue('')

  await kanit(page, '[data-testid="login-form"]', '0006-ac57.png')
})

test('AC-58: eşik kadar başarısız denemeden sonra kilitlenme metni çıkar', async ({ page }) => {
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

test('AC-32: zorunlu parola değişimi ekranı', async ({ page }) => {
  const kimlik = `zorunlu-${Date.now()}@ornek.com`

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('Yeni Personel')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill('ilk-parola-1234')
  await page.getByRole('button', { name: 'Personeli ekle' }).click()
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, 'ilk-parola-1234')

  await expect(page.getByTestId('password-form')).toBeVisible()
  await kanit(page, '[data-testid="password-form"]', '0006-ac32.png')
})

test('AC-54, AC-60, AC-61: panel personelin dilinde, personel listesiyle (TR)', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page)

  await expect(page.locator('html')).toHaveAttribute('lang', 'tr')
  await expect(page.getByTestId('staff-table')).toBeVisible()

  await kanit(page, '[data-testid="admin-shell"]', '0006-ac54.png')
  await kanit(page, '[data-testid="staff-table"]', '0006-ac60.png')
  await kanit(page, '[data-testid="add-staff-form"]', '0006-ac61-tr-panel.png')
})

test('AC-55, AC-61: Arapça tercih eden personel için panel RTL (AR)', async ({ page }) => {
  const kimlik = `arapca-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('موظف عربي')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByLabel('Panel dili').selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).click()
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, ilkParola)
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
  await kanit(page, '[data-testid="password-form"]', '0006-ac61-ar.png')

  await page.getByLabel('كلمة مرورك الحالية').fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة').fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

  // R8/R9 — Satış Danışmanının açabileceği bir yönetim ekranı henüz yok (X5). Ekran
  // bunu SÖYLER; "getirilemedi, tekrar dene" demez.
  await expect(page.getByTestId('admin-no-access')).toBeVisible()

  await kanit(page, '[data-testid="admin-shell"]', '0006-ac55.png')
})

test('AC-61: giriş ekranı İngilizce', async ({ page }) => {
  // Panel dili oturumdan gelir; oturumsuzken kurulum varsayılanıdır. İngilizce kanıt
  // için Kabul-Dili değil, İNGİLİZCE tercih eden bir personelin oturumu gerekir.
  const kimlik = `ingilizce-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad').fill('English Staff')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByLabel('Panel dili').selectOption('en')
  await page.getByRole('button', { name: 'Personeli ekle' }).click()
  await expect(page.getByText(kimlik)).toBeVisible()

  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()

  await girisYap(page, kimlik, ilkParola)
  await expect(page.locator('html')).toHaveAttribute('lang', 'en')

  await kanit(page, '[data-testid="password-form"]', '0006-ac61-en.png')
})
