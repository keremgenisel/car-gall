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
 * 0010a'nın görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * docs/testing.md § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}`.slice(0, 32)
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

/** Seçenekler SIRAYLA seçiliyor; etiketleri seed'e bağlamak kırılgan test üretirdi. */
async function formuDoldurAsync(page: Page, sase: string, alanlar: readonly string[]) {
  for (const alan of alanlar) {
    const secim = page.getByLabel(alan, { exact: true })
    const deger = await secim.locator('option').nth(1).getAttribute('value')

    expect(deger, alan).not.toBeNull()
    await secim.selectOption(deger!)
  }

  await page.getByLabel('Model yılı', { exact: true }).fill('2021')
  await page.getByLabel('Kilometre', { exact: true }).fill('48500')
  // Renk de çekirdek alan (R2, AC-6): personelin kendi dilinde dolu olmalı.
  await page.getByLabel('Renk', { exact: true }).fill('Gece Mavisi')
  await page.getByLabel('Şase numarası', { exact: true }).fill(sase)
}

const TR_ALANLAR = ['Şube', 'Model', 'Kasa tipi', 'Donanım paketi', 'Yakıt', 'Vites', 'Çekiş']

test('0010a/AC-1, AC-12: kayıt formu ve altı referans kümesi', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicle-new')

  const form = page.getByTestId('vehicle-form')

  await expect(form).toBeVisible()

  // AC-12 — altı kümenin hepsi kendi seçim alanında; etiketler personelin dilinde.
  for (const alan of TR_ALANLAR) {
    await expect(page.getByLabel(alan, { exact: true })).toBeVisible()
  }

  await kanit(page, form, '0010a-ac12.png')

  await formuDoldurAsync(page, benzersizSase('KANIT01'), TR_ALANLAR)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  await kanit(page, form, '0010a-ac1.png')

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')

  // AC-41 — kaydedildiği personele bildirilir.
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()
  await kanit(page, page.getByTestId('vehicle-saved'), '0010a-ac41.png')
})

test('0010a/AC-26: sade liste araç tanımı, fiyat ve kilometreyle', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await formuDoldurAsync(page, benzersizSase('KANIT26'), TR_ALANLAR)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  await page.goto('/admin/vehicles')

  const tablo = page.getByTestId('vehicle-table')

  await expect(tablo).toBeVisible()

  // Liste araç kimliğine göre AZALAN sıralı (UUID v7 = zaman sıralı), yani yeni kayıt
  // ilk satırdır. Kanıt görünür alanın kendisi: tablonun tamamını çekmek 200 seed
  // satırını da kadraja alıyordu ve 1,4 MB'lık bir görsel üretiyordu.
  await expect(tablo.locator('tbody tr').first()).toContainText('48.500 km')

  await kanitSayfa(page, '0010a-ac26.png')
})

test('0010a/AC-42, AC-43: doğrulama hatası alanı işaretler ve girilenler kaybolmaz', async ({
  page,
}) => {
  const sase = benzersizSase('KANIT42')

  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await formuDoldurAsync(page, sase, TR_ALANLAR)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')
  await expect(page.getByTestId('vehicle-saved')).toBeVisible()

  // Aynı şase ikinci kez: alan işaretlenir, mesaj personelin dilinde.
  await formuDoldurAsync(page, sase, TR_ALANLAR)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')
  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')

  const sasePlus = page.getByLabel('Şase numarası', { exact: true })

  await expect(sasePlus).toHaveAttribute('aria-invalid', 'true')

  // AC-43 — girilen bilgiler yerinde.
  await expect(page.getByLabel('Kilometre', { exact: true })).toHaveValue('48500')
  await expect(sasePlus).toHaveValue(sase)

  await kanit(page, page.getByTestId('vehicle-form'), '0010a-ac42.png')

  // AC-43'ün tekrar dene yarısı GENEL hatada sunuluyor; alan hatasında eylem kaydet
  // düğmesinin kendisidir ve form hâlâ gönderilebilir.
  await expect(page.getByRole('button', { name: 'Aracı kaydet' })).toBeEnabled()
  await kanitSayfa(page, '0010a-ac43.png')
})

test('0010a/AC-40: kaydet düğmesi yükleniyor hâline geçer', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await formuDoldurAsync(page, benzersizSase('KANIT40'), TR_ALANLAR)
  await page.getByLabel('Fiyat', { exact: true }).fill('1250000')

  // Yanıt BEKLETİLİYOR: yükleniyor hâli ancak istek uçarken görülebilir ve gerçek
  // sunucu yanıtı o kadar hızlı ki kanıt alınamıyor.
  await page.route('**/api/admin/vehicles', async (route) => {
    if (route.request().method() !== 'POST') {
      await route.fallback()
      return
    }

    await new Promise((resolve) => setTimeout(resolve, 2_000))
    await route.fallback()
  })

  await page.getByRole('button', { name: 'Aracı kaydet' }).press('Enter')

  const dugme = page.getByRole('button', { name: 'Kaydediliyor' })

  await expect(dugme).toBeVisible()
  await expect(dugme).toBeDisabled()
  await kanit(page, page.getByTestId('vehicle-form'), '0010a-ac40.png')

  await expect(page.getByTestId('vehicle-saved')).toBeVisible({ timeout: 10_000 })
})

test('0010a/AC-48, AC-49, AC-50: panel personelin dilinde açılır, form dir="rtl" ve etiketler kendi dilinde', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('arapca-arac')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad', { exact: true }).fill('موظف المركبات')
  await page.getByTestId('add-staff-form').getByLabel('E-posta', { exact: true }).fill(kimlik)
  await page.getByLabel('İlk parola', { exact: true }).fill(ilkParola)
  await page.getByLabel('Panel dili', { exact: true }).selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  // İlk giriş zorunlu parola değişimine düşer; panel o ekranı da personelin diliyle basar.
  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('كلمة مرورك الحالية', { exact: true }).fill(ilkParola)
  await page.getByLabel('كلمة مرورك الجديدة', { exact: true }).fill(yeniParola)
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  // AC-48 — panel, giriş yapan personelin TERCİH ETTİĞİ dilde açılıyor: bu personelin
  // tercihi Arapça ve araç yüzeyi `lang="ar"` ile basılıyor. Ziyaretçinin vitrin dili
  // burayı etkilemiyor (0006 R16).
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  // AC-49 — yön locale'in özelliğidir ve SUNUCUDAN basılır.
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

  // AC-50 — etiketler Arapça; hiçbir yerde ham çeviri anahtarı yok.
  await expect(page.getByLabel('رقم الهيكل', { exact: true })).toBeVisible()
  await expect(page.getByLabel('سنة الصنع', { exact: true })).toBeVisible()
  await expect(page.locator('body')).not.toContainText('vehicle.form.field')

  await kanitSayfa(page, '0010a-ac49.png')
  await kanit(page, page.getByTestId('vehicle-form'), '0010a-ac50-ar.png')
})

test('0010a/AC-50: form etiketleri İngilizce tercih eden personelde İngilizce', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('ingilizce-arac')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'ingilizce-parola-1'

  await yoneticiOlarakGirAsync(page)

  await page.getByLabel('Ad soyad', { exact: true }).fill('Vehicle Staff')
  await page.getByTestId('add-staff-form').getByLabel('E-posta', { exact: true }).fill(kimlik)
  await page.getByLabel('İlk parola', { exact: true }).fill(ilkParola)
  await page.getByLabel('Panel dili', { exact: true }).selectOption('en')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()

  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('Your current password', { exact: true }).fill(ilkParola)
  await page.getByLabel('Your new password', { exact: true }).fill(yeniParola)
  await page.getByRole('button', { name: 'Change my password' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()

  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()

  await expect(page.getByLabel('VIN', { exact: true })).toBeVisible()
  await expect(page.getByLabel('Model year', { exact: true })).toBeVisible()
  await expect(page.locator('body')).not.toContainText('vehicle.form.field')

  await kanit(page, page.getByTestId('vehicle-form'), '0010a-ac50-en.png')
})

test('0010a/AC-26: boş liste nedenini ve ne yapılabileceğini söyler', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  // Geliştirme veritabanında seed araçları var; BOŞ durum ancak uç boş dönerse görülür.
  // Kanıt gerçek ekranın boş hâlidir ve uydurma bir bileşen değil — yanıt boşaltılıyor.
  await page.route('**/api/admin/vehicles?*', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    // 0010b — liste yanıtı artık `PagedResult` zarfında. `0010a`'nın kriteri değişmedi;
    // sahte yanıt yeni sözleşmeye uyduruldu.
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: [], page: 1, pageSize: 25, totalCount: 0, totalPages: 1 }),
    })
  })

  await page.goto('/admin/vehicles')

  const bos = page.getByTestId('vehicle-empty')

  await expect(bos).toBeVisible()
  await expect(bos).toContainText('Henüz araç kaydı yok')

  await kanit(page, bos, '0010a-ac26-bos.png')
})

test('0010a/AC-47: liste hatası tekrar dene sunar ve teknik detay sızdırmaz', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/vehicles?*', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.fallback()
      return
    }

    await route.fulfill({
      status: 500,
      contentType: 'application/problem+json',
      body: JSON.stringify({ errorCode: 'vehicle.exploded', traceId: 'trace-kanit' }),
    })
  })

  await page.goto('/admin/vehicles')

  const hata = page.getByTestId('vehicle-error')

  await expect(hata).toBeVisible()
  await expect(hata).toContainText('Tekrar dene')
  await expect(page.locator('body')).not.toContainText('trace-kanit')
  await expect(page.locator('body')).not.toContainText('vehicle.exploded')
  await expect(page.locator('body')).not.toContainText('500')

  await kanit(page, hata, '0010a-ac47.png')
})
