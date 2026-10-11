import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
import { YONETICI_PAROLA } from './global-setup'
import { pngUret } from './gorsel'

/**
 * `0012a`'nın görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * `docs/testing.md` § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 *
 * <p><b>Üç kanıt STUB'lu ve gerekçesi ölçülmüş bir imkânsızlık:</b> "işleniyor" ve
 * "başarısız" hâlleri (AC-16, AC-28) ve hata durumu (AC-27), geliştirme ortamında
 * ISTEĞE GÖRE üretilemez — işleme hattı saniyeler içinde bitiyor ve bir fotoğrafı
 * bilerek bozmanın panelden bir yolu yok (olması da istenmez). `0010b`'nin kanıt
 * spec'i aynı kararı verdi: <i>ekran gerçek, veri sahte.</i> Davranışın kendisi
 * entegrasyon ve birim testlerinde gerçek veriyle kuruluyor.</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

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

async function aracKaydetAsync(page: Page, sase: string) {
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  for (const alan of ['Şube', 'Model', 'Kasa tipi', 'Donanım paketi', 'Yakıt', 'Vites', 'Çekiş']) {
    const secim = page.getByLabel(alan, { exact: true })
    const deger = await secim.locator('option').nth(1).getAttribute('value')

    expect(deger, alan).not.toBeNull()
    await secim.selectOption(deger!)
  }

  await page.getByLabel('Model yılı', { exact: true }).fill('2021')
  await page.getByLabel('Kilometre', { exact: true }).fill('48500')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
}

async function medyaEkraninaGitAsync(page: Page, sase: string): Promise<string> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  await page.goto(`/admin/vehicles/${arac!.vehicleId}/media`)
  await expect(page.getByTestId('vehicle-media-upload')).toBeVisible()

  return arac!.vehicleId
}

async function fotografYukleAsync(page: Page, ad: string) {
  await page.getByLabel('Fotoğraf ekle', { exact: true }).setInputFiles({
    name: ad,
    mimeType: 'image/png',
    buffer: pngUret(),
  })

  const oncesi = await page.getByTestId(/^vehicle-media-item-/).count()

  await page.getByTestId('vehicle-media-upload-submit').click()
  await expect(page.getByTestId(/^vehicle-media-item-/)).toHaveCount(oncesi + 1)
}

/**
 * İşleme hattı beş saniyelik turlarla çalışıyor; kanıt görüntüsünde GERÇEK fotoğrafın
 * görünmesi için varlığın `Hazır` olması gerekiyor. Sabit bir bekleme makinenin hızına
 * göre kırılgan olurdu — ilk kanıt turumda kartlar "Sıraya alındı" hâlinde yakalandı ve
 * kapak görseli boş bir kutuydu.
 */
async function hazirBekleAsync(page: Page, adet: number) {
  await expect(async () => {
    await page.reload()
    await expect(page.getByText('Hazır', { exact: true })).toHaveCount(adet)
  }).toPass({ timeout: 60_000 })
}

/** Birleşik okumanın SAHTE yanıtı: ekran gerçek, veri sahte. */
function medyaYaniti(arac: string, ogeler: readonly object[]) {
  return {
    vehicleId: arac,
    coverMediaAssetId:
      ogeler.length > 0 ? (ogeler[0] as { mediaAssetId: string }).mediaAssetId : null,
    items: ogeler,
  }
}

test('0012a/AC-1, 0012a/AC-26: yükleme kontrolü ve boş durum', async ({ page }) => {
  const sase = benzersizSase('K12AC1')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await medyaEkraninaGitAsync(page, sase)

  // Tek dosya: `multiple` niteliği YOK (X3).
  const kontrol = page.getByLabel('Fotoğraf ekle', { exact: true })

  await expect(kontrol).toBeVisible()
  await expect(kontrol).not.toHaveAttribute('multiple', '')

  const bos = page.getByTestId('vehicle-media-empty')

  await expect(bos).toContainText('Bu araca bağlı fotoğraf yok')
  await expect(bos).toContainText('kapak görseli olur')

  await kanit(page, page.getByTestId('vehicle-media-upload'), '0012a-ac1.png')
  await kanit(page, bos, '0012a-ac26.png')
})

test('0012a/AC-4, 0012a/AC-5: kapak sıranın ilk elemanı ve sırayla taşınır', async ({ page }) => {
  const sase = benzersizSase('K12AC4')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await medyaEkraninaGitAsync(page, sase)

  await fotografYukleAsync(page, 'birinci.png')
  await fotografYukleAsync(page, 'ikinci.png')
  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  // Kanıt görüntüsünde fotoğrafın KENDİSİ görünmeli: türevler hazır olana kadar bekle.
  await hazirBekleAsync(page, 2)

  const kartlar = page.getByTestId(/^vehicle-media-item-/)

  await expect(kartlar.nth(0)).toContainText('Kapak')
  await kanit(page, page.locator('.vehicle-media__list'), '0012a-ac4.png')

  await kartlar.nth(1).getByTestId('vehicle-media-up').click()
  await expect(page.getByTestId(/^vehicle-media-item-/).nth(0)).toContainText('Kapak')

  await kanit(page, page.locator('.vehicle-media__list'), '0012a-ac5.png')
})

test('0012a/AC-11: `alt` metni etkin her dilde yazılır', async ({ page }) => {
  const sase = benzersizSase('K12A11')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await medyaEkraninaGitAsync(page, sase)

  await fotografYukleAsync(page, 'birinci.png')
  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  await hazirBekleAsync(page, 1)

  const kart = page.getByTestId(/^vehicle-media-item-/).first()

  await kart.getByLabel('Kısa tanım (Türkçe)', { exact: true }).fill('Gece mavisi araç, önden')
  await kart.getByLabel('Kısa tanım (العربية)', { exact: true }).fill('سيارة كحلية من الأمام')
  await kart.getByLabel('Kısa tanım (English)', { exact: true }).fill('Navy car, front view')
  await kart.getByTestId('vehicle-media-alt-save').click()

  await expect(kart).toContainText('Tanımlar kaydedildi.')
  await kanit(page, kart, '0012a-ac11.png')
})

test('0012a/AC-16: başarısız fotoğraf ayırt edilir ve tekrar dene sunar', async ({ page }) => {
  const sase = benzersizSase('K12A16')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const arac = await medyaEkraninaGitAsync(page, sase)

  // Bir fotoğrafı bilerek bozmanın panelden yolu YOK; yanıt sahteleniyor, ekran gerçek.
  await page.route(`**/api/admin/vehicles/${arac}/media`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        medyaYaniti(arac, [
          {
            mediaAssetId: '0192c000-0000-7000-8000-00000000f001',
            status: 'failed',
            failureReason: 'watermarkunavailable',
            altText: {},
            derivatives: [],
          },
        ]),
      ),
    })
  })

  await page.reload()

  const hata = page.getByTestId('vehicle-media-failure')

  await expect(hata).toContainText('Galeri logosu okunamadı')
  await expect(hata.getByTestId('vehicle-media-retry')).toBeVisible()
  await expect(page.locator('body')).not.toContainText('watermarkunavailable')

  await kanit(page, page.getByTestId(/^vehicle-media-item-/).first(), '0012a-ac16.png')
})

/**
 * 0012a/AC-17'nin TARAYICIDAKİ kapısı: düğmeye gerçekten tıklanıyor ve yanıt gerçek bir
 * `Response` üzerinden gerçek `sendJson`'a gidiyor.
 *
 * <p><b>Dış bulgunun yazdırdığı test.</b> AC-17'nin tek testi bileşen testiydi ve orada
 * `mediaAdmin` modülünün tamamı sahtelenmişti — `sendJson` hiç koşmuyordu. Uç
 * `202 Accepted` + <b>boş gövde</b> dönüyor; istemci yalnız `204`'ü gövdesiz saydığı
 * için `response.json()` patlıyor, çağıran onu `api.unavailable`'a çeviriyor ve
 * BAŞARILI bir tekrar deneme personele "İşlem şu anda tamamlanamadı" olarak
 * görünüyordu.</p>
 *
 * <p>Burada yalnız VERİ sahte: varlığın durumu ve ucun yanıtı. Yolun geri kalanı —
 * düğme, `retryMediaAsset`, `sendJson`, `onRefresh`, yeniden okuma — gerçek. Sunucu
 * tarafının aynı sözleşmeyi verdiği `Host.Tests/AracMedyasiZinciriTests`'te ölçülüyor.</p>
 */
test('0012a/AC-17: tekrar dene düğmesi tıklanınca varlık yeniden kuyruğa alınıyor', async ({
  page,
}) => {
  const sase = benzersizSase('K12A17')
  const varlik = '0192c000-0000-7000-8000-00000000f001'

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const arac = await medyaEkraninaGitAsync(page, sase)

  let durum = 'failed'
  let tekrarCagrildi = false

  await page.route(`**/api/admin/vehicles/${arac}/media`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        medyaYaniti(arac, [
          {
            mediaAssetId: varlik,
            status: durum,
            failureReason: durum === 'failed' ? 'watermarkunavailable' : null,
            altText: {},
            derivatives: [],
          },
        ]),
      ),
    })
  })

  // Ucun GERÇEK sözleşmesi: 202 + boş gövde. Gövdeyi JSON diye okuyan bir istemci
  // burada patlar — kusur tam olarak buydu.
  await page.route(`**/api/admin/media/assets/${varlik}/retry`, async (route) => {
    tekrarCagrildi = true
    durum = 'processing'

    await route.fulfill({ status: 202, body: '' })
  })

  await page.reload()
  await page.getByTestId('vehicle-media-retry').click()

  // Ekran sunucudan yeniden okuyup varlığı İŞLENİYOR hâlinde gösteriyor.
  await expect(page.getByTestId('vehicle-media-processing')).toBeVisible()
  expect(tekrarCagrildi).toBe(true)

  // Ve hiçbir yerde hata basılmıyor: başarılı tekrar deneme başarılı görünüyor.
  await expect(page.getByTestId('vehicle-media-failure')).toHaveCount(0)
  await expect(page.locator('body')).not.toContainText('İşlem şu anda tamamlanamadı')
})

test('0012a/AC-28: `işleniyor` varlığın hâli; ekranın iskeletiyle karışmaz', async ({ page }) => {
  const sase = benzersizSase('K12A28')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const arac = await medyaEkraninaGitAsync(page, sase)

  // İşleme hattı saniyeler içinde bitiyor; "işleniyor" hâlini isteğe göre yakalamanın
  // yolu yok. Yanıt sahteleniyor.
  await page.route(`**/api/admin/vehicles/${arac}/media`, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(
        medyaYaniti(arac, [
          {
            mediaAssetId: '0192c000-0000-7000-8000-00000000f002',
            status: 'processing',
            failureReason: null,
            altText: {},
            derivatives: [],
          },
        ]),
      ),
    })
  })

  await page.reload()

  await expect(page.getByTestId('vehicle-media-processing')).toContainText('birkaç dakika')
  // İskelet BASILMIYOR: "işleniyor" ekranın değil varlığın durumu (D8).
  await expect(page.getByTestId('vehicle-media-skeleton')).toHaveCount(0)

  await kanit(page, page.getByTestId(/^vehicle-media-item-/).first(), '0012a-ac28.png')
})

test('0012a/AC-27, 0012a/AC-29: okuma hatası tekrar dene sunar ve teknik detay sızdırmaz', async ({
  page,
}) => {
  const sase = benzersizSase('K12A27')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const arac = await medyaEkraninaGitAsync(page, sase)

  await page.route(`**/api/admin/vehicles/${arac}/media`, async (route) => {
    await route.fulfill({
      status: 503,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'about:blank',
        status: 503,
        errorCode: 'media.storage_unavailable',
        traceId: 'kanit-0012a',
      }),
    })
  })

  await page.reload()

  const hata = page.getByTestId('vehicle-media-error')

  await expect(hata).toContainText('Fotoğraflar getirilemedi')
  await expect(hata).toContainText('Tekrar dene')

  // AC-29 — ne kod, ne durum, ne `traceId`, ne yol.
  const govde = page.locator('body')

  await expect(govde).not.toContainText('media.storage_unavailable')
  await expect(govde).not.toContainText('503')
  await expect(govde).not.toContainText('kanit-0012a')
  await expect(govde).not.toContainText('/api/admin/')

  await kanit(page, hata, '0012a-ac27.png')
})

test('0012a/AC-30, 0012a/AC-31: Arapça personelde ekran RTL ve metinler Arapça', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('arapca-medya')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'
  const sase = benzersizSase('K12A31')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const arac = await medyaEkraninaGitAsync(page, sase)

  await fotografYukleAsync(page, 'birinci.png')
  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  await hazirBekleAsync(page, 1)

  // Panelin dili personelin TERCİHİNDEN gelir (0006 R16); adreste locale öneki yok.
  await page.goto('/admin')
  await page.getByLabel('Ad soyad', { exact: true }).fill('محرر الوسائط')
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

  await page.goto(`/admin/vehicles/${arac}/media`)

  // Yön SUNUCUDAN basılır, uçta hesaplanmaz (`docs/frontend.md` § 6).
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  await expect(page.getByLabel('إضافة صورة', { exact: true })).toBeVisible()
  await expect(page.getByTestId(/^vehicle-media-item-/).first()).toContainText('الغلاف')

  // AC-30 — ham çeviri anahtarı YOK.
  await expect(page.locator('body')).not.toContainText('vehicle.media.')

  await kanitSayfa(page, '0012a-ac31-ar.png')
})
