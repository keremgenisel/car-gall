import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0026` — galeri logosu, gerçek Host'a karşı: panelden yükleme (`kind=logo`), `Media`'nın gerçek
 * işleme hattı (filigransız, saydam), atama, vitrinin üst çubuğu ve altlığı. jsdom'un taşımadığı
 * şeyler burada: gerçek işleme ve yoklama, gerçek yerleşim (logonun adın hangi yanında olduğu),
 * gerçek ağ isteği sayısı.
 *
 * <b>Paylaşılan durum.</b> Logo galeri GENELİDİR ve `0020`'nin üst çubuk ölçümleri logosuz hâli
 * bekler. Bu dosya sırayla, tek profilde koşar ve her testin sonunda logoyu KALDIRIR.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const GALERI = '/api/admin/gallery'

async function profil(api: APIRequestContext) {
  return (await (await api.get(GALERI)).json()) as {
    version: number
    logoMediaAssetId: string | null
  }
}

async function logoyuKaldir(api: APIRequestContext) {
  const { version, logoMediaAssetId } = await profil(api)
  if (logoMediaAssetId !== null) {
    expect((await api.delete(`${GALERI}/logo?version=${version}`)).ok()).toBe(true)
  }
}

/** Saydam zeminli PNG: tarayıcının kendi tuvali; sol üst köşe tamamen saydam. */
async function saydamPng(page: Page, genislik = 800, yukseklik = 200): Promise<Buffer> {
  const dataUrl = await page.evaluate(
    ([g, y]) => {
      const tuval = document.createElement('canvas')
      tuval.width = g!
      tuval.height = y!
      const c = tuval.getContext('2d')!
      c.fillStyle = '#284678'
      c.fillRect(g! / 4, 0, (g! * 3) / 4, y!)
      return tuval.toDataURL('image/png')
    },
    [genislik, yukseklik],
  )
  return Buffer.from(dataUrl.split(',')[1]!, 'base64')
}

/** Panelden logo yükler ve hazır olmasını bekler; varlık kimliğini döner. */
async function logoYukle(page: Page): Promise<string> {
  await page.goto('/admin/gallery')
  const bolum = page.getByTestId('gallery-logo')
  await expect(bolum).toBeVisible()
  await bolum.locator('input[type="file"]').setInputFiles({
    name: 'logo.png',
    mimeType: 'image/png',
    buffer: await saydamPng(page),
  })
  await expect(page.getByTestId('gallery-logo-preview')).toBeVisible({ timeout: 30_000 })
  return (await profil(page.request)).logoMediaAssetId!
}

test.beforeEach(async ({ page }, testInfo) => {
  test.skip(
    testInfo.project.name !== 'masaustu',
    'Logo galeri genelidir; testler tek profilde sırayla.',
  )
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  await logoyuKaldir(page.request)
})

test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.project.name === 'masaustu') {
    await page.unrouteAll({ behavior: 'ignoreErrors' })
    await logoyuKaldir(page.request)
  }
})

test('0026/AC-19, AC-20, AC-21, AC-1, AC-5: Panelden yüklenen PNG gerçek hatta işlenir; işleniyor, sonra önizleme; türevler iki WebP logo türevi', async ({
  page,
}) => {
  await page.goto('/admin/gallery')
  const bolum = page.getByTestId('gallery-logo')
  await expect(bolum).toContainText('Saydam zeminli PNG, en az 640 piksel genişlik önerilir')
  await expect(bolum.locator('input[type="file"]')).toHaveAttribute(
    'accept',
    'image/jpeg,image/png,image/webp',
  )
  await kanit(page, bolum, '0026-ac19.png')

  await bolum.locator('input[type="file"]').setInputFiles({
    name: 'logo.png',
    mimeType: 'image/png',
    buffer: await saydamPng(page),
  })
  await expect(
    page.getByTestId('gallery-logo-processing').or(page.getByTestId('gallery-logo-preview')),
  ).toBeVisible()
  if (await page.getByTestId('gallery-logo-processing').isVisible()) {
    await kanit(page, bolum, '0026-ac20.png')
  }
  await expect(page.getByTestId('gallery-logo-preview')).toBeVisible({ timeout: 30_000 })
  await expect(bolum.getByTestId('gallery-logo-remove')).toBeVisible()
  await kanit(page, bolum, '0026-ac21.png')

  const { logoMediaAssetId } = await profil(page.request)
  const varlik = (await (
    await page.request.get(`/api/admin/media/assets/${logoMediaAssetId}`)
  ).json()) as {
    kind: string
    derivatives: { variant: string; width: number; url: string }[]
  }
  expect(varlik.kind).toBe('logo')
  expect(varlik.derivatives.map((turev) => turev.variant).sort()).toEqual([
    'logo-large',
    'logo-small',
  ])
  const kucuk = varlik.derivatives.find((turev) => turev.variant === 'logo-small')!
  expect(kucuk.width).toBe(320)

  // Türev gerçekten depodan WebP olarak sunuluyor. Saydam pikselin kendisi (AC-4) gerçek Skia
  // çıktısıyla `Media.Tests/LogoIslemeTests`'te ölçülür: depo başka bir kökende, tarayıcı CORS
  // yüzünden pikseli okuyamıyor.
  const turev = await page.request.get(kucuk.url)
  expect(turev.ok()).toBe(true)
  expect((await turev.body()).subarray(8, 12).toString('ascii')).toBe('WEBP')
})

test('0026/AC-30, AC-33, AC-42, AC-62, AC-63, AC-64, AC-65, AC-67: Üst çubukta logo adın satır başında (tr solda, ar sağda), altlığın ilk öğesi; tek ağ isteği', async ({
  page,
}) => {
  await logoYukle(page)

  const istekler: string[] = []
  page.on('request', (istek) => {
    if (istek.url().includes('logo-small')) {
      istekler.push(istek.url())
    }
  })
  await page.goto('/tr/')
  const marka = page.getByTestId('top-bar-brand')
  const logo = page.getByTestId('top-bar-logo')
  await expect(logo).toBeVisible()
  await expect(logo).toHaveAttribute('alt', '')
  expect(await logo.getAttribute('loading')).toBeNull()
  expect(await logo.getAttribute('fetchpriority')).toBeNull()
  const logoKutu = (await logo.boundingBox())!
  const adKutu = (await marka.locator('bdi').boundingBox())!
  expect(logoKutu.x + logoKutu.width).toBeLessThanOrEqual(adKutu.x + 1)
  expect(logoKutu.height).toBeLessThanOrEqual(40.5)
  await kanit(page, 'header.top-bar', '0026-ac30.png')

  const altlik = page.getByTestId('footer-logo')
  await altlik.scrollIntoViewIfNeeded()
  await expect(altlik).toBeVisible()
  await expect(altlik).toHaveAttribute('loading', 'lazy')
  expect(await altlik.getAttribute('alt')).not.toBe('')
  expect(await altlik.getAttribute('src')).toBe(await logo.getAttribute('src'))
  await page.waitForLoadState('networkidle')
  expect(istekler).toHaveLength(1)
  await kanit(page, page.getByTestId('home-footer'), '0026-ac62.png')

  await page.goto('/ar/')
  const arLogo = (await page.getByTestId('top-bar-logo').boundingBox())!
  const arAd = (await page.getByTestId('top-bar-brand').locator('bdi').boundingBox())!
  expect(arLogo.x).toBeGreaterThanOrEqual(arAd.x + arAd.width - 1)
  await kanit(page, 'header.top-bar', '0026-ac33-ar.png')

  const arAltlik = page.getByTestId('footer-logo')
  await arAltlik.scrollIntoViewIfNeeded()
  const kutu = (await arAltlik.boundingBox())!
  const ic = (await page.locator('.home-footer__inner').boundingBox())!
  expect(kutu.x + kutu.width).toBeGreaterThan(ic.x + ic.width - 40)
})

test('0026 (C6, C7): Logo inmeden önce de üst çubuk logosunun kutusu son boyutundadır (40 px yükseklik); gelince üst çubuk kaymaz', async ({
  page,
}) => {
  await logoYukle(page)
  let birak: () => void = () => {}
  const bekle = new Promise<void>((resolve) => {
    birak = resolve
  })
  await page.route('**/*logo-small*', async (route) => {
    await bekle
    await route.continue()
  })

  await page.goto('/tr/cars')
  const logo = page.getByTestId('top-bar-logo')
  await expect(logo).toBeAttached()
  const once = (await logo.boundingBox())!
  const cubukOnce = (await page.locator('header.top-bar').boundingBox())!
  expect(once.height).toBe(40)

  birak()
  await expect.poll(() => logo.evaluate((el) => (el as HTMLImageElement).complete)).toBe(true)
  expect(await logo.boundingBox()).toEqual(once)
  expect(await page.locator('header.top-bar').boundingBox()).toEqual(cubukOnce)
})

test('0026/AC-22, AC-29: Başarısız logoda çevrilmiş neden ve "Tekrar dene"; ar panelinde bölüm Arapça ve sağdan sola', async ({
  page,
}) => {
  const id = await logoYukle(page)

  // İşleme hatası gerçek veriyle kurulamıyor: varlığın okuması araya girilerek `failed` yapılır.
  await page.route(`**/api/admin/media/assets/${id}`, async (route) => {
    if (route.request().method() !== 'GET') {
      return route.fallback()
    }
    const yanit = await route.fetch()
    const govde = (await yanit.json()) as Record<string, unknown>
    return route.fulfill({
      response: yanit,
      json: { ...govde, status: 4, failureReason: 1, derivatives: [] },
    })
  })
  await page.route('**/admin/**', async (route) => {
    if (route.request().resourceType() !== 'document') {
      return route.fallback()
    }
    const yanit = await route.fetch()
    const html = (await yanit.text()).replace(
      /<html lang="[a-z]+" dir="(ltr|rtl)"/,
      '<html lang="ar" dir="rtl"',
    )
    return route.fulfill({ response: yanit, body: html })
  })

  await page.goto('/admin/gallery')
  const bolum = page.getByTestId('gallery-logo')
  await expect(page.getByTestId('gallery-logo-failed')).toBeVisible()
  await expect(page.getByTestId('gallery-logo-retry')).toBeVisible()
  await expect(bolum.locator('legend')).toHaveText('الشعار')
  expect(await bolum.evaluate((el) => getComputedStyle(el).direction)).toBe('rtl')
  await kanit(page, bolum, '0026-ac22-ar.png')
})
