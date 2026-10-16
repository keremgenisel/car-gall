import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
import { YONETICI_PAROLA } from './global-setup'
import { girisYap as ortakGirisYap, oturumuBirakAsync } from './giris'

const SEED_KIMLIK = 'yonetici@cargallery.local'

/**
 * frontend-002 — görsel dil ve kuruluma özel tema.
 *
 * Buradaki assert'ler HER koşuda çalışır; görüntüler yalnız `npm run evidence` ile
 * yazılır (docs/testing.md § 3). Yani kapı her zaman açık, gürültü yalnız istendiğinde.
 */

/**
 * `0013` D28/D46 — giriş yolu ortak yardımcıya taşındı; bu dosyanın iddiaları değişmedi.
 */
async function girisYap(page: Page) {
  await ortakGirisYap(page, SEED_KIMLIK, YONETICI_PAROLA)
}

/** Tarayıcının GERÇEKTEN hesapladığı değer; `var()` zincirini biz çözmüyoruz. */
async function hesaplanan(page: import('@playwright/test').Page, token: string): Promise<string> {
  return page.evaluate(
    (name) => getComputedStyle(document.documentElement).getPropertyValue(name).trim(),
    token,
  )
}

/**
 * **A grubu — bu spec'ten ÖNCE de yeşildi.** `dir="auto"` 0001/0002'de kuruldu; bu test
 * yeni davranışın kanıtı değil, font değişiminin onu bozmadığının kanıtı.
 *
 * Ölçtüğü iki şey: yön mekanizması yerinde mi (`dir="auto"`), ve Arapça sayfada fiyat
 * LATİN rakamla mı basılıyor. İkincisi `docs/frontend.md` § 5'in uyardığı yer — `ar`
 * locale'inin ICU varsayılanı ortama göre Hint-Arap rakamı olabilir ve yeni bir yazı tipi
 * yığını bu hatayı görünür kılacak ilk şeydir.
 */
test('frontend-002/AC-1: Latin marka adları ve fiyatlar Arapça metin içinde doğru yönde görünür', async ({
  page,
}) => {
  await page.goto('/ar/')

  const kart = page.getByTestId('listing-card').first()
  await expect(kart.locator('.listing-card__model')).toHaveAttribute('dir', 'auto')

  const fiyat = (await kart.locator('.listing-card__price').textContent())?.trim() ?? ''

  expect(fiyat).toMatch(/[0-9]/)
  // Hint-Arap rakamları (U+0660–U+0669) BULUNMAZ: rakam sistemi sabitlenmiş.
  expect(fiyat).not.toMatch(/[\u0660-\u0669]/)
})

test("frontend-002/AC-6: türetilmiş token'ların hesaplanan değeri", async ({ page }) => {
  await page.goto('/tr/')

  // Beyan ile hesaplanan arasındaki fark YALNIZ türetilmiş iki token'da; onları C# testi
  // değil tarayıcı doğrulamalı. Kendi `var()` çözücümle "hesaplanan" demek, tarayıcıyı
  // taklit etmek olurdu.
  expect(await hesaplanan(page, '--color-on-accent-fill')).toBe(
    await hesaplanan(page, '--color-ink'),
  )
  expect(await hesaplanan(page, '--color-focus-ring')).toBe(
    await hesaplanan(page, '--color-accent-text'),
  )
})

test('frontend-002/AC-8: yazı tipi istekleri üçüncü taraf alan adına gitmez', async ({ page }) => {
  const yabanciTipografi: string[] = []
  const ucuncuTarafSaglayici: string[] = []

  // Beklenen host SABİT: `page.url()` ile karşılaştırmak ilk denememin hatasıydı —
  // dinleyici takıldığında sayfa henüz `about:blank` ve belgenin KENDİSİ yabancı sayılıyor.
  const kendiHost = new URL(test.info().project.use.baseURL ?? 'http://localhost:5080').host

  /**
   * Tanınmış tipografi sağlayıcıları: kriterin yasakladığı şey bunlar. Ayrı bir liste
   * olarak duruyor çünkü "yabancı host" ile "üçüncü taraf yazı tipi" aynı şey değil.
   */
  const SAGLAYICILAR = ['fonts.googleapis.com', 'fonts.gstatic.com', 'use.typekit.net']

  page.on('request', (request) => {
    const url = request.url()
    const tip = request.resourceType()

    if (SAGLAYICILAR.some((saglayici) => url.includes(saglayici))) {
      ucuncuTarafSaglayici.push(url)
      return
    }

    // İddia TİPOGRAFİYE daraltılı: ilk yazımı SAYFANIN TÜM isteklerini sayıyordu ve
    // `0012c`'de kırmızıya döndü. Sebep bir kusur değil, yapılandırma: medya adresi
    // `Media__PublicBaseUrl` ile veriliyor ve üretimde zaten bir CDN — hem CI hem
    // geliştirme yığını onu ayrı bir port/host olarak kuruyor. Vitrinin ilk sayfasına
    // gerçek fotoğraflı bir ilan düştüğü an o istek "yabancı host" oluyordu, oysa
    // kriter yazı tipinden söz ediyor. Geniş iddia, kendi kriterinin dışını ölçüyordu.
    if ((tip === 'font' || tip === 'stylesheet') && new URL(url).host !== kendiHost) {
      yabanciTipografi.push(url)
    }
  })

  await page.goto('/tr/')
  await expect(page.locator('main h1')).toBeVisible()

  // Yazı tipi ve stil istekleri aynı origin'den gelir.
  expect(yabanciTipografi).toEqual([])

  // Üçüncü taraf sağlayıcıya HİÇ istek yok — hangi kaynak tipiyle olursa.
  expect(ucuncuTarafSaglayici).toEqual([])
})

/**
 * AC-7'nin iki yarısından biri. İkincisi — dolgu vurgusunun YALNIZ dolgu olarak
 * kullanılması — `check:tokens` kapısında: o bir kaynak kuralı ve tek bir ekranda
 * ölçülemez.
 */
test('frontend-002/AC-7: vitrin listesinde fiyat metni metin vurgusuyla basılır', async ({
  page,
}) => {
  await page.goto('/tr/')

  const fiyat = page.getByTestId('listing-card').first().locator('.listing-card__price')
  await expect(fiyat).toBeVisible()

  // Koyu zeytin, limon değil: limon beyaz zeminde metin olarak 1.26:1 verir.
  const renk = await fiyat.evaluate((el) => getComputedStyle(el).color)
  expect(renk).toBe('rgb(79, 107, 0)')

  await kanitSayfa(page, 'frontend-002-ac7.png')
})

test('frontend-002/AC-28: odak halkası hem zeminde hem limon dolgu üzerinde görünür', async ({
  page,
}) => {
  await page.goto('/tr/')

  // Zemin üzerindeki odak: ilk odaklanabilir öğe.
  await page.keyboard.press('Tab')
  await kanitSayfa(page, 'frontend-002-ac28-zemin.png')

  const halka = await page.evaluate(() =>
    getComputedStyle(document.documentElement).getPropertyValue('--color-focus-ring').trim(),
  )
  const dolgu = await page.evaluate(() =>
    getComputedStyle(document.documentElement).getPropertyValue('--color-accent-fill').trim(),
  )

  // Halka dolguya BAĞLI DEĞİL: aynı olsalar limon zeminde kaybolurdu (R11, tuzak 6).
  expect(halka).not.toBe(dolgu)

  // Limon dolgulu birincil düğme DETAY sayfasında: vitrin listesinde birincil düğme yok
  // (ölçtüm, sıfır) ve korumalı bir assert hiç koşmayan bir assert'tir.
  await page.getByTestId('listing-card').first().locator('a').first().click()
  await expect(page.locator('.listing-detail__price')).toBeVisible()

  const birincil = page.locator('.ui-button--primary').first()
  await expect(birincil).toBeVisible()

  const dolguRengi = await birincil.evaluate((el) => getComputedStyle(el).backgroundColor)
  const metinRengi = await birincil.evaluate((el) => getComputedStyle(el).color)

  // Dolgu limon, üzerindeki metin KOYU: beyaz metin 1.26:1 verirdi ve okunmazdı.
  expect(dolguRengi).toBe('rgb(205, 245, 100)')
  expect(metinRengi).toBe('rgb(11, 12, 14)')

  await birincil.focus()
  await kanit(page, birincil, 'frontend-002-ac28-dolgu.png')
})

test('frontend-002/AC-29: Arapça locale Arapça tipografi yığınıyla basılır', async ({ page }) => {
  await page.goto('/ar/')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  const aile = await page.locator('body').evaluate((el) => getComputedStyle(el).fontFamily)

  // Arapça sayfa Latin yığınına DÜŞMEZ: Space Grotesk'te Arapça karakter yok ve tarayıcı
  // sessizce başka bir fonta düşerdi (tuzak 3).
  expect(aile).toContain('Readex Pro')
  expect(aile).not.toContain('Space Grotesk')

  await kanitSayfa(page, 'frontend-002-ac29-ar.png')
})

test('frontend-002/AC-30: Arapça düzen RTL kalır, tema yönü bozmaz', async ({ page }) => {
  await page.goto('/ar/')

  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('main h1')).toBeVisible()

  await kanitSayfa(page, 'frontend-002-ac30-ar.png')
})

test('frontend-002/AC-27: seçili ve hatalı hâller renkten bağımsız da okunur', async ({ page }) => {
  await page.goto('/tr/')

  // Seçili daraltma: pilin içinde gerçek bir onay kutusu var; işaretli hâl renkten
  // bağımsız okunur ve ekran okuyucuya da geçer. Koruma YOK — korumalı bir assert hiç
  // koşmayabilir ve o hâlde hiçbir şey kanıtlamaz.
  const filtre = page.getByTestId('body-type-filter')
  await expect(filtre).toBeVisible()

  const pil = filtre.locator('.filter-chip input[type="checkbox"]').first()
  await pil.check()
  await expect(pil).toBeChecked()
  await kanit(page, filtre, 'frontend-002-ac27.png')

  // Rezerve hâli METİNLE anlatılıyor, yalnız renkle değil.
  const rozet = page.locator('.ui-badge').first()
  await expect(rozet).toBeVisible()
  expect((await rozet.textContent())?.trim()).not.toBe('')
})

/**
 * Onay anındaki sayım BEŞ ekran (spec AC-25). Üçü panelde ve giriş gerektiriyor; ikisini
 * ölçüp "her ekran" demek, sayımın üçte ikisini kanıtsız bırakmak olurdu.
 */
test('frontend-002/AC-25: onay anında sayılan her ekran yeni token değerleriyle açılır ve okunur durumdadır', async ({
  page,
}) => {
  // 1 — vitrin listesi
  await page.goto('/tr/')
  await expect(page.locator('main h1')).toBeVisible()
  await kanitSayfa(page, 'frontend-002-ac25-liste.png')

  // 2 — ilan detayı
  await page.getByTestId('listing-card').first().locator('a').first().click()
  await expect(page.locator('.listing-detail__price')).toBeVisible()
  await kanitSayfa(page, 'frontend-002-ac25-detay.png')

  // 3 — panel girişi
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()
  await kanitSayfa(page, 'frontend-002-ac25-giris.png')

  // 4 — personel listesi
  await girisYap(page)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
  await kanitSayfa(page, 'frontend-002-ac25-personel.png')

  // 5 — zorunlu parola değişimi. Bu ekran YALNIZ ilk parolasıyla giren bir personele
  // açılıyor; kurulum hesabı parolasını çoktan değiştirdiği için `/admin/password`
  // adresine gitmek yetmiyor (ölçtüm: form basılmıyor). O yüzden ekranı doğuran koşul
  // kuruluyor — yeni bir personel ve onun ilk parolası.
  const kimlik = `tema-${Date.now()}@ornek.com`
  const ilkParola = 'ilk-parola-1234'

  await page.getByLabel('Ad soyad').fill('Tema Kanıtı')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  await page.getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('Parola').fill(ilkParola)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  await expect(page.getByTestId('password-form')).toBeVisible()
  await kanitSayfa(page, 'frontend-002-ac25-parola.png')
})

test('frontend-002/AC-26: boş durum yeni palette okunur', async ({ page }) => {
  // Hiçbir sonuç döndürmeyen bir daraltma: boş durum metniyle birlikte görünür.
  await page.goto('/tr/?bodyType=olmayan-kasa-tipi')

  await expect(page.locator('main')).toBeVisible()
  await kanitSayfa(page, 'frontend-002-ac26-bos.png')
})
