import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
import { YONETICI_PAROLA } from './global-setup'
import {
  girisYap,
  ikinciAdimKurAsync,
  oturumuBirakAsync,
  yeniKurulmusYoneticiAsync,
  yoneticiOlarakGirAsync,
} from './giris'

/**
 * `0013`'ün görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * `docs/testing.md` § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 *
 * <p><b>Sahtelenen iki kanıt ve gerekçesi:</b> *yükleniyor* durumu gerçek bir istekle
 * yakalanamaz (sunucu milisaniyelerde cevaplıyor) ve *kilitlenmiş hesap* ipucu gerçek beş
 * başarısız denemeyle üretilirse hesabı on beş dakika kilitler — aynı koşudaki başka
 * testler o hesapla giriş yapıyor. İkisinde de ölçülen şey sunucunun kararı değil,
 * <b>ekranın o duruma verdiği tepki</b> (`0008`/AC-18'in aynı gerekçesi). Kalan altı
 * kanıt gerçek veriyle üretiliyor.</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'
/*
 * `0013` — bu dosyanın testleri KENDİ hesabını kuruyor: personel ekleme, ilk parola
 * değişimi, kurulum ve iki fazlı giriş. Varsayılan 30 saniyelik test zaman aşımı bu
 * zincire yetmiyor (ölçüldü: mobil profilde 30 sn aşıldı) ve süreyi uzatmak kriteri
 * değiştirmiyor — kendi hesabı kullanmanın sebebi D45'in ölçülmüş tavanı.
 */
test.describe.configure({ timeout: 90_000 })

function benzersizKimlik(onek: string) {
  return `${onek}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
}

/**
 * Giriş AKIŞINI gösteren kanıtlar **kendi** hesabıyla üretiliyor: kurulum hesabıyla
 * yapılan her gerçek giriş D11'in otuz saniyelik penceresine ve R8'in paylaşılan
 * kilitlenme sayacına yazıyor (D45/D46'nın uygulama turundaki ölçümü).
 */
async function akisHesabiAsync(page: Page) {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)

  const kimlik = benzersizKimlik('kanit-akis')
  const parola = 'kanit-akis-parola1'
  const sir = await yeniKurulmusYoneticiAsync(page, kimlik, 'ilk-parola-1234', parola)

  return { kimlik, parola, sir }
}

async function parolaFaziniGecAsync(page: Page, kimlik: string, parola: string) {
  await page.goto('/admin')
  await page.getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('Parola', { exact: true }).fill(parola)
  await page.getByRole('button', { name: 'Giriş yap' }).click()
}

test('0013/AC-19: İkinci adım giriş ekranının bir fazıdır (kanıt)', async ({ page }) => {
  const hesap = await akisHesabiAsync(page)

  await parolaFaziniGecAsync(page, hesap.kimlik, hesap.parola)

  await expect(page.getByLabel('Doğrulama kodu')).toBeVisible()
  await expect(page.getByTestId('two-factor-switch-user')).toBeVisible()

  await kanit(page, page.getByTestId('login-form'), '0013-ac19.png')
})

test('0013/AC-21: Yanlış kod reddedilir; faz korunur (kanıt)', async ({ page }) => {
  const hesap = await akisHesabiAsync(page)

  await parolaFaziniGecAsync(page, hesap.kimlik, hesap.parola)

  await page.getByLabel('Doğrulama kodu').fill('000000')
  await page.getByRole('button', { name: 'Doğrula', exact: true }).click()

  await expect(page.getByTestId('login-error')).toBeVisible()

  await kanit(page, page.getByTestId('login-form'), '0013-ac21.png')
})

test('0013/AC-55: Ara durum düşünce parola fazına dönülür (kanıt)', async ({ page }) => {
  const hesap = await akisHesabiAsync(page)

  await parolaFaziniGecAsync(page, hesap.kimlik, hesap.parola)

  await expect(page.getByLabel('Doğrulama kodu')).toBeVisible()

  // Ara durumun düşmesi beş dakika beklemeyi gerektirir; ölçülen şey EKRANIN tepkisi,
  // bu yüzden yanıt sahteleniyor (D20'nin ikinci kodu).
  await page.route('**/api/identity/sessions/two-factor', (route) =>
    route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 401, errorCode: 'identity.two_factor_pending_expired' }),
    }),
  )

  await page.getByLabel('Doğrulama kodu').fill('123456')
  await page.getByRole('button', { name: 'Doğrula', exact: true }).click()

  // Parola fazı geri geldi ve sebebi orada yazılı.
  await expect(page.getByLabel('Parola', { exact: true })).toBeVisible()
  await expect(page.getByTestId('login-error')).toContainText('Parolanızı yeniden girip')

  await kanit(page, page.getByTestId('login-form'), '0013-ac55-suresi-doldu.png')
})

test('0013/AC-55: Kilitlenmiş hesap ipucu ve yükleniyor hâli (kanıt)', async ({ page }) => {
  // Kilitlenme ipucu UÇTA sayılıyor (`0006` AC-58, D21): sunucu kilitlenmeyi söylemiyor.
  // Gerçek beş deneme hesabı on beş dakika kilitler ve aynı koşudaki başka testleri
  // düşürürdü; bu yüzden red sahteleniyor ve ölçülen şey ipucunun BASILMASI.
  await page.route('**/api/identity/sessions', (route) =>
    route.fulfill({
      status: 401,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 401, errorCode: 'identity.invalid_credentials' }),
    }),
  )

  await page.goto('/admin')

  for (let deneme = 0; deneme < 5; deneme += 1) {
    await page.getByLabel('E-posta').fill(SEED_KIMLIK)
    await page.getByLabel('Parola', { exact: true }).fill('kesinlikle-yanlis-parola')
    await page.getByRole('button', { name: 'Giriş yap' }).click()
    await expect(page.getByTestId('login-error')).toBeVisible()
  }

  await expect(page.getByTestId('login-locked')).toBeVisible()

  await kanit(page, page.getByTestId('login-form'), '0013-ac55-kilitli.png')
})

test('0013/AC-10, 0013/AC-38: Kurulum ekranı ve kalan kurtarma kodu sayısı (kanıt)', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)

  const kimlik = benzersizKimlik('kanit-danismani')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'kanit-parolasi-12'

  await personelEkleAsync(page, kimlik, ilkParola, 'sales_advisor')
  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('Mevcut parolanız').fill(ilkParola)
  await page.getByLabel('Yeni parolanız').fill(yeniParola)
  await page.getByRole('button', { name: 'Parolamı değiştir' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await page.goto('/admin/two-factor')

  // AC-10 — kurulum başlatıldığında kare kod UÇTA çiziliyor ve elle giriş metni aynı
  // anda görünüyor; ikisi birlikte tek bir görüntüde.
  await page.getByTestId('two-factor-start').click()

  await expect(page.getByTestId('two-factor-qr').locator('svg')).toBeVisible()
  await expect(page.getByTestId('two-factor-manual-secret')).toBeVisible()

  await kanitSayfa(page, '0013-ac10.png')

  // Yükleniyor hâli: durum isteği askıda bırakılıyor ve iskelet basılıyor.
  await page.route('**/api/admin/me/two-factor', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    await new Promise((cozul) => setTimeout(cozul, 2_000))
    await route.fallback()
  })

  await page.goto('/admin/two-factor')
  await expect(page.getByTestId('two-factor-loading')).toBeVisible()
  await kanit(page, page.getByTestId('two-factor-page'), '0013-ac55-yukleniyor.png')

  await page.unrouteAll()
  await page.goto('/admin/two-factor')

  // Kurulumu tamamla ve kalan sayıyı göster (AC-38).
  const sir = await ikinciAdimKurAsync(page)

  expect(sir.length).toBeGreaterThan(0)

  await kanit(page, page.getByTestId('recovery-codes'), '0013-ac7.png')

  await page.getByTestId('two-factor-enrolled-continue').click()

  await expect(page.getByTestId('two-factor-remaining')).toBeVisible()
  await kanitSayfa(page, '0013-ac38.png')
})

test('0013/AC-54: Personel listesinde ikinci adım rozeti (kanıt)', async ({ page }) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)

  await expect(page.getByTestId('staff-table')).toBeVisible()
  await expect(page.getByTestId('staff-row-self').getByText('İki adım açık')).toBeVisible()

  await kanit(page, page.getByTestId('staff-table'), '0013-ac54.png')
})

test('0013/AC-56: Arapça tercih eden personelin kurulum ekranı RTL (kanıt)', async ({ page }) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)

  const kimlik = benzersizKimlik('arapca-danisman')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await personelEkleAsync(page, kimlik, ilkParola, 'sales_advisor', 'ar')
  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('كلمة مرورك الحالية').fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة').fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await page.goto('/admin/two-factor')

  // Yön locale'in özelliğidir ve SUNUCUDAN basılır (docs/frontend.md § 6).
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

  await page.getByTestId('two-factor-start').click()
  await expect(page.getByTestId('two-factor-manual-secret')).toBeVisible()

  await kanitSayfa(page, '0013-ac56-ar.png')
})

async function personelEkleAsync(
  page: Page,
  kimlik: string,
  parola: string,
  rol: string,
  locale = 'tr',
) {
  await page.goto('/admin')
  await page.getByLabel('Ad soyad').fill('Kanıt Personeli')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(parola)
  await page.getByLabel('Rol', { exact: true }).selectOption(rol)
  await page.getByLabel('Panel dili').selectOption(locale)
  await page.getByRole('button', { name: 'Personeli ekle' }).click()

  await expect(page.getByText(kimlik)).toBeVisible()
}
