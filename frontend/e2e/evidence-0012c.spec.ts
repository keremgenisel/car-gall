import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
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

/**
 * `0012c`'nin görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * `docs/testing.md` § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 *
 * <p><b>STUB yok.</b> Sekiz kanıtın sekizi de gerçek veriyle üretiliyor: araçlar
 * gerçekten kaydediliyor, gerçekten seçiliyor ve toplu eylem gerçekten uygulanıyor.
 * Kısmi başarı da sahte bir yanıtla değil, <b>gerçekten reddedilen</b> bir satırla
 * doğuyor.</p>
 *
 * <p><b>Tek istisna AC-44</b> ve gerekçesi yazılı: arşivlenmiş satırda düğme artık hiç
 * basılmıyor (**AC-43**) ve kalan başarısızlık yolları — `concurrency.lost`, ağ hatası —
 * gerçek bir yarışla üretilemez. O kanıtta yanıt sahteleniyor; ölçülen şey sunucunun
 * kararı değil, <b>ekranın o koda verdiği tepki</b> (`0008`/AC-18'in aynı gerekçesi).</p>
 *
 * <p><b>Seçim sınırının görsel kanıtı YOK</b> ve bu bir eksiklik değil karar (**D52**):
 * istemci `pageSize` göndermiyor, yani sınır panelden tetiklenemiyor. AC-5, AC-6 ve AC-7
 * API düzeyinde doğrulanıyor.</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

function benzersizKimlik(onek: string) {
  return `${onek}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
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
  // Kilometre BİLEREK 51.300: `0010a`/AC-11 panel satırını "48.500 km" metniyle buluyor
  // ve o değeri üç spec birden yazıyor. Aynı değeri dördüncü kez yazmak, o testin
  // bulduğu satırın hangi koşuya ait olduğunu kuma yazmak olurdu.
  await page.getByLabel('Kilometre', { exact: true }).fill('51300')
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
}

async function aracKimligiAsync(page: Page, sase: string): Promise<string> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  return arac!.vehicleId
}

test('0012c/AC-1: seçim yapılan listede toplu eylem yüzeyi görünür', async ({ page }) => {
  const birinci = benzersizSase('K12CK1')
  const ikinci = benzersizSase('K12CK2')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, birinci)
  await aracKaydetAsync(page, ikinci)

  const birinciId = await aracKimligiAsync(page, birinci)
  const ikinciId = await aracKimligiAsync(page, ikinci)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-select-${birinciId}`).check()
  await page.getByTestId(`vehicle-select-${ikinciId}`).check()

  await expect(page.getByTestId('vehicle-bulk-selection')).toContainText('2 araç seçildi')

  await kanitSayfa(page, '0012c-ac1.png')
})

test('0012c/AC-37: hepsi uygulandığında başarı durumu basılır', async ({ page }) => {
  const birinci = benzersizSase('K12CK3')
  const ikinci = benzersizSase('K12CK4')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, birinci)
  await aracKaydetAsync(page, ikinci)

  const birinciId = await aracKimligiAsync(page, birinci)
  const ikinciId = await aracKimligiAsync(page, ikinci)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-select-${birinciId}`).check()
  await page.getByTestId(`vehicle-select-${ikinciId}`).check()

  await page.getByTestId('vehicle-bulk-operation').selectOption('feature')
  await page.getByTestId('vehicle-bulk-apply').click()

  await expect(page.getByTestId('vehicle-bulk-success')).toContainText('2 ilan öne çıkarıldı')

  await kanit(page, page.getByTestId('vehicle-bulk'), '0012c-ac37.png')
})

test('0012c/AC-38: kısmi başarı dördüncü ekran durumu olarak basılır', async ({ page }) => {
  const uygulanacak = benzersizSase('K12CK5')
  const reddedilecek = benzersizSase('K12CK6')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, uygulanacak)
  await aracKaydetAsync(page, reddedilecek)

  const uygulanacakId = await aracKimligiAsync(page, uygulanacak)
  const reddedilecekId = await aracKimligiAsync(page, reddedilecek)

  // İkinci araç ÖNCE arşivleniyor: arşivlenmiş bir ilan yeniden arşivlenemez (A3) ve
  // toplu arşivleme onu reddedecek. Birinci araç taslak, yani uygulanacak.
  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  await page.getByTestId(`vehicle-select-${reddedilecekId}`).check()
  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()
  await expect(page.getByTestId('vehicle-bulk-success')).toBeVisible()

  await page.goto('/admin/vehicles?page=1&sort=recently-added&archived=true')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-select-${uygulanacakId}`).check()
  await page.getByTestId(`vehicle-select-${reddedilecekId}`).check()
  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()

  const kutu = page.getByTestId('vehicle-bulk-partial')

  await expect(kutu).toContainText('1 ilan uygulandı')
  await expect(page.getByTestId('vehicle-bulk-failure')).toHaveCount(1)

  // AC-39 — hiçbir teknik detay yok.
  await expect(page.locator('body')).not.toContainText('listing.invalid_status_transition')
  await expect(page.locator('body')).not.toContainText('409')

  await kanit(page, kutu, '0012c-ac38.png')
})

test('0012c/AC-32: öne çıkarılan araç listede rozetle ayırt edilir', async ({ page }) => {
  const sase = benzersizSase('K12CK7')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)

  const aracId = await aracKimligiAsync(page, sase)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-feature-${aracId}`).click()

  const satir = page.locator('tbody tr').filter({ has: page.locator(`a[href$="${aracId}"]`) })

  await expect(satir.getByTestId('vehicle-featured')).toContainText('Öne çıkarıldı')

  await kanitSayfa(page, '0012c-ac32.png')
})

test('0012c/AC-40: Arapça personelde toplu eylem yüzeyi RTL ve metinler Arapça', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('arapca-toplu')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'
  const sase = benzersizSase('K12CK8')
  const ikinciSase = benzersizSase('K12CK9')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await aracKaydetAsync(page, ikinciSase)

  const aracId = await aracKimligiAsync(page, sase)
  const ikinciAracId = await aracKimligiAsync(page, ikinciSase)

  await page.goto('/admin')
  await page.getByLabel('Ad soyad', { exact: true }).fill('مدير المخزون')
  await page.getByTestId('add-staff-form').getByLabel('E-posta', { exact: true }).fill(kimlik)
  await page.getByLabel('İlk parola', { exact: true }).fill(ilkParola)
  await page.getByLabel('Panel dili', { exact: true }).selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('كلمة مرورك الحالية', { exact: true }).fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة', { exact: true }).fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  // Yön SUNUCUDAN basılır, uçta hesaplanmaz (`docs/frontend.md` § 6).
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  await page.getByTestId(`vehicle-select-${aracId}`).check()

  const cubuk = page.getByTestId('vehicle-bulk')

  await expect(cubuk).toContainText('إجراء جماعي')
  await expect(page.locator('body')).not.toContainText('vehicle.bulk')

  await kanit(page, cubuk, '0012c-ac40.png')

  // AC-37, Arapçada: İKİ kayıt **ikil** biçimi istiyor ve bu bir dış bulgunun bedeli —
  // altı başarı mesajının beşi yalnız `other` taşıyordu, yani 1, 2 ve 3-10 için
  // dilbilgisi hatalı cümle basıyordu. `check:locales` o gün "386 anahtar eşit" diyordu.
  await page.getByTestId(`vehicle-select-${ikinciAracId}`).check()
  await page.getByTestId('vehicle-bulk-operation').selectOption('feature')
  await page.getByTestId('vehicle-bulk-apply').click()

  const basari = page.getByTestId('vehicle-bulk-success')

  // İkil biçimde sayı RAKAMLA yazılmaz: ikil kelimenin kendisinde yaşıyor.
  await expect(basari).toContainText('إعلانين')
  await expect(basari).not.toContainText('2')

  await kanit(page, cubuk, '0012c-ac37-ar.png')
})

test('0012c/AC-43: arşivlenmiş satırda öne çıkarma düğmesi yok', async ({ page }) => {
  const sase = benzersizSase('K12CKA')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)

  const aracId = await aracKimligiAsync(page, sase)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  // Araç GERÇEKTEN arşivleniyor; durum sahtelenmiyor.
  await page.getByTestId(`vehicle-select-${aracId}`).check()
  await page.getByTestId('vehicle-bulk-operation').selectOption('archive')
  await page.getByTestId('vehicle-bulk-apply').click()
  await expect(page.getByTestId('vehicle-bulk-success')).toBeVisible()

  await page.goto('/admin/vehicles?page=1&sort=recently-added&archived=true')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  const satir = page.locator('tbody tr').filter({ has: page.locator(`a[href$="${aracId}"]`) })

  await expect(satir).toHaveCount(1)
  await expect(page.getByTestId(`vehicle-feature-${aracId}`)).toHaveCount(0)

  // Durum sütunu "Arşiv" diyor: kanıt hangi satıra baktığını kendi içinde taşıyor.
  await expect(satir).toContainText('Arşiv')

  await kanit(page, satir, '0012c-ac43.png')
})

test('0012c/AC-44: satır başına öne çıkarmanın reddi satırda gerekçesiyle görünür', async ({
  page,
}) => {
  const sase = benzersizSase('K12CKB')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)

  const aracId = await aracKimligiAsync(page, sase)

  await page.route(`**/api/admin/vehicles/${aracId}/feature*`, async (route) => {
    await route.fulfill({
      status: 409,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 409, errorCode: 'listing.cannot_feature.archived' }),
    })
  })

  await page.goto('/admin/vehicles?page=1&sort=recently-added')
  await expect(page.getByTestId('vehicle-table')).toBeVisible()

  await page.getByTestId(`vehicle-feature-${aracId}`).click()

  const satir = page.locator('tbody tr').filter({ has: page.locator(`a[href$="${aracId}"]`) })
  const hata = satir.getByTestId(`vehicle-feature-error-${aracId}`)

  await expect(hata).toContainText('Arşivlenmiş bir ilan öne çıkarılamaz')

  // AC-39 — hata kodu, HTTP durumu ve rota yolu ekranda yok.
  await expect(page.locator('body')).not.toContainText('listing.cannot_feature')
  await expect(page.locator('body')).not.toContainText('409')

  await kanit(page, satir, '0012c-ac44.png')
})
