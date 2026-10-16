import { expect, test, type Page } from '@playwright/test'
import { kanit, kanitSayfa } from './kanit'
import { YONETICI_PAROLA } from './global-setup'
import { pngUret } from './gorsel'
import { girisYap } from './giris'

/**
 * `0013` D28/D46 — giriş yolu ORTAK yardımcıya taşındı (`e2e/giris.ts`): D30 Yönetici'de
 * ikinci adımı zorunlu kıldığı için her giriş artık iki fazlı ve aynı mantığı on dört
 * dosyada ayrı ayrı yazmak on dört kopya demekti. Bu dosyanın iddiaları DEĞİŞMEDİ.
 */

/**
 * `0012b`'nin görsel kanıtı. Yazma AÇIKÇA istenir (`npm run evidence`,
 * `docs/testing.md` § 3); normal e2e koşusunda buradaki `expect`'ler çalışır ama dosya
 * yazılmaz.
 *
 * <p><b>İki kanıt STUB'lu</b> ve gerekçesi ölçülmüş bir imkânsızlık: "lokasyon ataması
 * bekliyor" hâli ancak bir şube KAPATILARAK doğar ve şube kapatmanın HTTP ucu yok
 * (`0003` § D1 — o modülün korumasız yazma ucu olamaz); okuma hatası da istenen anda
 * üretilemez. `0010b` ve `0012a`'nın kanıt spec'leri aynı kararı verdi: <i>ekran gerçek,
 * veri sahte.</i></p>
 *
 * <p>Arşivli liste bu sayının DIŞINDA: araç gerçekten kaydedilip gerçekten arşivleniyor
 * ve satırın kendisi iddia ediliyor (AC-51).</p>
 */

const SEED_KIMLIK = 'yonetici@cargallery.local'

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

function benzersizKimlik(onek: string) {
  return `${onek}-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@ornek.com`
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

async function aracaGitAsync(page: Page, sase: string): Promise<string> {
  const yanit = await page.request.get(
    '/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added',
  )

  expect(yanit.ok()).toBe(true)

  const govde = (await yanit.json()) as { items: { vehicleId: string; vin: string }[] }
  const arac = govde.items.find((item) => item.vin === sase)

  expect(arac, `şase ${sase} listede bulunamadı`).toBeTruthy()

  await page.goto(`/admin/vehicles/${arac!.vehicleId}`)
  await expect(page.getByTestId('vehicle-edit-form')).toBeVisible()

  return arac!.vehicleId
}

async function hazirFotografAsync(page: Page, vehicleId: string) {
  await page.goto(`/admin/vehicles/${vehicleId}/media`)
  await expect(page.getByTestId('vehicle-media-upload')).toBeVisible()

  await page.getByLabel('Fotoğraf ekle', { exact: true }).setInputFiles({
    name: 'arac.png',
    mimeType: 'image/png',
    buffer: pngUret(),
  })

  await page.getByTestId('vehicle-media-upload-submit').click()
  await expect(page.getByTestId(/^vehicle-media-item-/)).toHaveCount(1)

  await page.getByTestId('vehicle-media-save').click()
  await expect(page.getByTestId('vehicle-media-saved')).toBeVisible()

  await expect(async () => {
    await page.reload()
    await expect(page.getByText('Hazır', { exact: true })).toHaveCount(1)
  }).toPass({ timeout: 60_000 })
}

test('0012b/AC-1, 0012b/AC-54: taslakta eylem yüzeyi ve başarı bildirimi', async ({ page }) => {
  const sase = benzersizSase('K12BA1')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const vehicleId = await aracaGitAsync(page, sase)

  const eylemler = page.getByTestId('vehicle-actions')

  // A3 — taslakta YALNIZ iki eylem var; "rezerve et" HİÇ basılmıyor.
  await expect(eylemler).toContainText('Yayına al')
  await expect(eylemler).toContainText('Arşivle')
  await expect(page.getByTestId('vehicle-action-reserve')).toHaveCount(0)

  await kanit(page, eylemler, '0012b-ac1.png')

  await hazirFotografAsync(page, vehicleId)
  await page.goto(`/admin/vehicles/${vehicleId}`)

  await page.getByTestId('vehicle-action-publish').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()

  await kanit(page, page.getByTestId('vehicle-actions'), '0012b-ac54.png')
})

test('0012b/AC-9: yayın reddi eksiklerin HEPSİNİ birlikte gösterir ve kod basmaz', async ({
  page,
}) => {
  const sase = benzersizSase('K12BA9')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  await aracaGitAsync(page, sase)

  await page.getByTestId('vehicle-action-publish').click()

  const red = page.getByTestId('vehicle-action-error')

  await expect(red).toContainText('En az bir fotoğrafın hazır olması gerekiyor')

  // AC-55 — hiçbir teknik detay yok.
  const govde = page.locator('body')

  await expect(govde).not.toContainText('vehicle.cannot_publish')
  await expect(govde).not.toContainText('422')

  await kanit(page, red, '0012b-ac9.png')
})

test('0012b/AC-14: `Satıldı` terminaldir ve ekran bunu söyler', async ({ page }) => {
  const sase = benzersizSase('K12B14')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const vehicleId = await aracaGitAsync(page, sase)
  await hazirFotografAsync(page, vehicleId)

  await page.goto(`/admin/vehicles/${vehicleId}`)
  await page.getByTestId('vehicle-action-publish').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()

  await page.getByTestId('vehicle-action-sold').click()
  await expect(page.getByTestId('vehicle-sold-terminal')).toBeVisible()

  await expect(page.getByTestId('vehicle-action-publish')).toHaveCount(0)

  await kanit(page, page.getByTestId('vehicle-actions'), '0012b-ac14.png')
})

test('0012b/AC-50, 0012b/AC-51: arşiv varsayılanda gizli, açık filtreyle görünür', async ({
  page,
}) => {
  const sase = benzersizSase('K12B51')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const vehicleId = await aracaGitAsync(page, sase)

  // Kanıt GERÇEK bir arşivli araç gösteriyor: önceki hâli yalnız onay kutusunun
  // işaretini ölçüyordu ve "arşivli bir araç GÖRÜNDÜ" iddiası hiç kurulmuyordu —
  // filtre hiçbir satır getirmese de yeşil kalırdı.
  await page.getByTestId('vehicle-action-archive').click()
  await expect(page.getByTestId('vehicle-action-done')).toBeVisible()

  const satir = page.locator(`[data-testid="vehicle-row"] a[href="/admin/vehicles/${vehicleId}"]`)

  await page.goto('/admin/vehicles?page=1&sort=recently-added')

  await expect(page.getByTestId('vehicle-table')).toBeVisible()
  await expect(page.getByLabel('Arşivi göster')).not.toBeChecked()

  // AC-50 — arşivlenen araç varsayılan listede YOK.
  await expect(satir).toHaveCount(0)

  await kanitSayfa(page, '0012b-ac50.png')

  await page.getByLabel('Arşivi göster').check()

  await expect(page.getByLabel('Arşivi gizle')).toBeChecked()
  await expect(page).toHaveURL(/archived=true/)

  // AC-51 — AÇIK filtreyle aynı araç göründü ve durumu `Arşiv`. İddia aynı zamanda
  // kanıtın bir sonraki satırını `loading` hâlinde yakalamasını da engelliyor:
  // satır ancak yükleme bittikten sonra var.
  await expect(satir).toHaveCount(1)
  await expect(satir).toBeVisible()
  await expect(page.getByTestId('vehicle-skeleton')).toHaveCount(0)

  const hucreler = page
    .locator('[data-testid="vehicle-table"] tbody tr')
    .filter({ has: page.locator(`a[href="/admin/vehicles/${vehicleId}"]`) })

  await expect(hucreler).toContainText('Arşiv')

  await kanitSayfa(page, '0012b-ac51.png')
})

test('0012b/AC-53: lokasyon ataması bekleyen araç listede ayırt edilir', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  // Şube kapatmanın HTTP ucu YOK (`0003` § D1): bayraklı araç isteğe göre üretilemez.
  // Ekran gerçek, veri sahte.
  await page.route('**/api/admin/vehicles?*', async (route) => {
    const yanit = await route.fetch()
    const govde = (await yanit.json()) as {
      items: { awaitingBranchAssignment: boolean }[]
    }

    if (govde.items.length > 0) {
      govde.items[0].awaitingBranchAssignment = true
    }

    await route.fulfill({ response: yanit, json: govde })
  })

  await page.goto('/admin/vehicles?page=1&sort=recently-added')

  await expect(page.getByTestId('vehicle-awaiting-branch').first()).toContainText('Şube bekliyor')

  await kanitSayfa(page, '0012b-ac53.png')
})

test('0012b/AC-54: okuma hatası tekrar dene sunar ve teknik detay sızdırmaz', async ({ page }) => {
  await yoneticiOlarakGirAsync(page)

  await page.route('**/api/admin/vehicles?*', async (route) => {
    await route.fulfill({
      status: 503,
      contentType: 'application/problem+json',
      body: JSON.stringify({
        type: 'about:blank',
        status: 503,
        errorCode: 'api.unavailable',
        traceId: 'kanit-0012b',
      }),
    })
  })

  await page.goto('/admin/vehicles')

  const hata = page.getByTestId('vehicle-error')

  await expect(hata).toContainText('Araç listesi getirilemedi')
  await expect(hata).toContainText('Tekrar dene')

  const govde = page.locator('body')

  await expect(govde).not.toContainText('503')
  await expect(govde).not.toContainText('kanit-0012b')
  await expect(govde).not.toContainText('/api/admin/')

  await kanit(page, hata, '0012b-ac55.png')
})

test('0012b/AC-56, 0012b/AC-57: Arapça personelde eylem yüzeyi RTL ve metinler Arapça', async ({
  page,
}) => {
  const kimlik = benzersizKimlik('arapca-yayin')
  const ilkParola = 'ilk-parola-1234'
  const yeniParola = 'arapca-parola-12'
  const sase = benzersizSase('K12B57')

  await yoneticiOlarakGirAsync(page)
  await aracKaydetAsync(page, sase)
  const vehicleId = await aracaGitAsync(page, sase)

  await page.goto('/admin')
  await page.getByLabel('Ad soyad', { exact: true }).fill('مدير النشر')
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

  await page.goto(`/admin/vehicles/${vehicleId}`)

  // Yön SUNUCUDAN basılır, uçta hesaplanmaz (`docs/frontend.md` § 6).
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  await expect(page.getByTestId('vehicle-actions')).toContainText('نشر')
  await expect(page.locator('body')).not.toContainText('vehicle.action')

  await kanit(page, page.getByTestId('vehicle-actions'), '0012b-ac57-ar.png')
})
