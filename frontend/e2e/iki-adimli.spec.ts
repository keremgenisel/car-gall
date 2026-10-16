import { expect, test, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import {
  girisYap,
  ikinciAdimKodu,
  ikinciAdimKurAsync,
  ikinciAdimSirriniOku,
  yoneticiOlarakGirAsync,
} from './giris'

/**
 * `0013`'ün kritik akışı: personel parolasını girer → **ikinci adımı geçer** → panel
 * açılır (docs/testing.md § 3). Bu akış kırıldığında panele kimse giremez, yani ürün
 * kullanılamaz hâle gelir.
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizKimlik(onek: string) {
  return `${onek}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
}

test('0013/AC-19: İkinci adım giriş ekranının bir fazıdır: adım geçilmeden panele gezinme olmaz', async ({
  page,
}) => {
  await page.goto('/admin')
  await page.getByLabel('E-posta').fill(SEED_KIMLIK)
  await page.getByLabel('Parola', { exact: true }).fill(YONETICI_PAROLA)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  // Faz değişti ama EKRAN aynı: kart hâlâ `login-form` ve panel kabuğu yok.
  await expect(page.getByLabel('Doğrulama kodu')).toBeVisible()
  await expect(page.getByTestId('login-form')).toBeVisible()
  await expect(page.getByTestId('admin-shell')).toHaveCount(0)

  // Adım geçilmeden panele gitmek de işe yaramıyor: oturum çerezi hiç yazılmadı (D4).
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()
  await expect(page.getByTestId('admin-shell')).toHaveCount(0)
})

test('0013/AC-21, 0013/AC-20: Yanlış kod reddedilir ve taze kod istenir; doğru kodla oturum açılır', async ({
  page,
}) => {
  const sir = ikinciAdimSirriniOku()!

  await page.goto('/admin')
  await page.getByLabel('E-posta').fill(SEED_KIMLIK)
  await page.getByLabel('Parola', { exact: true }).fill(YONETICI_PAROLA)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  const kod = page.getByLabel('Doğrulama kodu')

  await kod.fill('000000')
  await page.getByRole('button', { name: 'Doğrula' }).click()

  // Red FAZI KORUYOR: personel taze kodu aynı ekranda yazıyor.
  await expect(page.getByTestId('login-error')).toBeVisible()
  await expect(kod).toBeVisible()

  // Ekranda ne hata kodu ne durum kodu var (docs/frontend.md § 3.1).
  const govde = (await page.locator('body').innerText()).toLowerCase()

  expect(govde).not.toContain('identity.')
  expect(govde).not.toContain('401')

  await kod.fill(ikinciAdimKodu(sir))
  await page.getByRole('button', { name: 'Doğrula' }).click()

  await expect(page.getByTestId('admin-shell')).toBeVisible()
})

test('0013/AC-17: Kullanıcı değiştir ara durumu siler ve parola fazına döner', async ({ page }) => {
  await page.goto('/admin')
  await page.getByLabel('E-posta').fill(SEED_KIMLIK)
  await page.getByLabel('Parola', { exact: true }).fill(YONETICI_PAROLA)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  await page.getByTestId('two-factor-switch-user').click()

  // Parola fazı geri geldi ve alan BOŞ: ara durum silindi, kimlik de taşınmıyor.
  await expect(page.getByLabel('Parola', { exact: true })).toHaveValue('')
  await expect(page.getByLabel('E-posta')).toHaveValue('')
})

test('0013/AC-29, 0013/AC-30: Zorunlu rolde kurulumsuz personel kapılı oturum alır ve yalnız kurulumu yapabilir', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)

  const kimlik = benzersizKimlik('kapili-yonetici')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'kapili-parola-12'

  await personelEkleAsync(page, kimlik, ilkParola, 'manager')
  await cikisAsync(page)

  await girisYap(page, kimlik, ilkParola)

  // Önce PAROLA kapısı (D6): kurulum ekranı henüz yok.
  await page.getByLabel('Mevcut parolanız').fill(ilkParola)
  await page.getByLabel('Yeni parolanız').fill(yeniParola)
  await page.getByRole('button', { name: 'Parolamı değiştir' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)

  // Şimdi KURULUM kapısı: panel kabuğu yok, kurulum ekranı var ve gezinme basılmıyor.
  await expect(page.getByTestId('two-factor-page')).toBeVisible()
  await expect(page.getByTestId('admin-shell')).toHaveCount(0)
  await expect(page.getByRole('link', { name: 'Personel' })).toHaveCount(0)

  const sir = await ikinciAdimKurAsync(page)

  // Kurulum bitti: AYNI oturumla panel açılıyor (D35 — durum kaynaktan okunuyor).
  await page.goto('/admin')
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  // Ve sıradaki giriş artık ikinci adım istiyor.
  await cikisAsync(page)
  await girisYap(page, kimlik, yeniParola, sir)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
})

test('0013/AC-36: Kodunu üretemeyen personel kurtarma koduyla giriş yapar', async ({ page }) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)

  const kimlik = benzersizKimlik('kurtarma-danismani')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'danisman-parola-1'

  // Opsiyonel rol: kurulum ZORUNLU değil, personel kendi isteğiyle açıyor (D30).
  await personelEkleAsync(page, kimlik, ilkParola, 'sales_advisor')
  await cikisAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('Mevcut parolanız').fill(ilkParola)
  await page.getByLabel('Yeni parolanız').fill(yeniParola)
  await page.getByRole('button', { name: 'Parolamı değiştir' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  await page.goto('/admin/two-factor')
  await ikinciAdimKurAsync(page)

  // Kurtarma kodları BİR KEZ gösteriliyor; test onları tam o anda okuyor.
  const kodlar = await page.getByTestId('recovery-codes').locator('code').allInnerTexts()

  expect(kodlar.length).toBeGreaterThan(1)

  await cikisAsync(page)

  await page.goto('/admin')
  await page.getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('Parola', { exact: true }).fill(yeniParola)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  await page.getByTestId('two-factor-toggle-input').click()
  await page.getByLabel('Kurtarma kodu').fill(kodlar[0]!.trim())
  await page.getByRole('button', { name: 'Doğrula' }).click()

  await expect(page.getByTestId('admin-shell')).toBeVisible()

  // Kalan sayı düştü: kullanılan kod bir daha kabul edilmiyor (R5).
  await page.goto('/admin/two-factor')
  await expect(page.getByTestId('two-factor-remaining')).toContainText(String(kodlar.length - 1))
})

async function personelEkleAsync(page: Page, kimlik: string, parola: string, rol: string) {
  await page.goto('/admin')
  await page.getByLabel('Ad soyad').fill('Test Personeli')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(parola)
  await page.getByLabel('Rol', { exact: true }).selectOption(rol)
  await page.getByLabel('Panel dili').selectOption('tr')
  await page.getByRole('button', { name: 'Personeli ekle' }).click()

  await expect(page.getByText(kimlik)).toBeVisible()
}

async function cikisAsync(page: Page) {
  await page.getByRole('button', { name: 'Çıkış yap' }).click()
  await expect(page.getByTestId('login-form')).toBeVisible()
}
