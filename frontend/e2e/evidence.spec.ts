import { expect, test, type Page } from '@playwright/test'

/**
 * Kanıt üretimi (docs/testing.md § 3). Görüntü KRİTERİN karşılandığını gösterir,
 * ekranın genel görünümünü değil; bu yüzden çoğu görüntü ilgili öğenin kendisidir.
 * Yalnız masaüstü profilinde koşar — mobil profil aynı dosyaları üzerine yazardı.
 */
const KANIT = '../docs/evidence'

// Yalnız `--project=masaustu` ile çağrılır; iki profilde koşmak aynı dosyaları üzerine yazardı.

async function kanit(page: Page, selector: string, dosya: string) {
  await page
    .locator(selector)
    .first()
    .screenshot({ path: `${KANIT}/${dosya}` })
}

test('AC-2: rezerve rozeti', async ({ page }) => {
  await page.goto('/tr/')
  const rezerveKart = page.getByTestId('listing-card').filter({ hasText: 'Rezerve' }).first()
  await expect(rezerveKart).toBeVisible()
  await rezerveKart.screenshot({ path: `${KANIT}/0001-ac2.png` })
})

test('AC-10: seçili sayı ve seçimi temizle — üç locale', async ({ page }) => {
  for (const [locale, suv] of [
    ['tr', 'SUV'],
    ['en', 'SUV'],
    ['ar', 'دفع رباعي'],
  ] as const) {
    await page.goto(`/${locale}/`)
    await page.getByRole('checkbox', { name: suv }).check()
    await kanit(page, '[data-testid="body-type-filter"]', `0001-ac10-${locale}.png`)
  }
})

test('AC-12: kasa tipi public değilken çubuk gösterilmez', async ({ page }) => {
  await page.route('**/api/catalog/field-settings', async (route) => {
    await route.fulfill({
      json: {
        fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: false, isFilterable: false }],
      },
    })
  })
  await page.goto('/tr/')
  await expect(page.getByTestId('listing-card').first()).toBeVisible()
  await page.locator('.page').screenshot({ path: `${KANIT}/0001-ac12.png` })
})

test('AC-20: kart alanları ve sayı biçimlendirmesi — üç locale', async ({ page }) => {
  for (const locale of ['tr', 'en', 'ar'] as const) {
    await page.goto(`/${locale}/`)
    await kanit(page, '[data-testid="listing-card"]', `0001-ac20-${locale}.png`)
  }
})

test('AC-21: fiyat sorunuz', async ({ page }) => {
  await page.goto('/tr/')
  const kart = page.getByTestId('listing-card').filter({ hasText: 'Fiyat sorunuz' }).first()
  await expect(kart).toBeVisible()
  await kart.screenshot({ path: `${KANIT}/0001-ac21.png` })
})

test('AC-22: KWD üç ondalık — üç locale', async ({ page }) => {
  // Kurulum para birimi dağıtım yapılandırmasıdır; KWD kurulumu API yanıtı üzerinden kurulur.
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    const response = await route.fetch()
    const body = await response.json()
    body.items = body.items.map((item: Record<string, unknown>) => ({
      ...item,
      price: { onRequest: false, amount: 1250.5, currencyCode: 'KWD', minorUnitExponent: 3 },
    }))
    await route.fulfill({ response, json: body })
  })

  for (const locale of ['tr', 'en', 'ar'] as const) {
    await page.goto(`/${locale}/`)
    const fiyat = page.locator('.listing-card__price').first()
    await expect(fiyat).toContainText('250')
    await kanit(page, '[data-testid="listing-card"]', `0001-ac22-${locale}.png`)
  }
})

test('AC-25: yükleniyor iskeleti', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 2000))
    await route.continue()
  })
  await page.goto('/tr/')
  await expect(page.getByTestId('list-skeleton')).toBeVisible()
  await kanit(page, '[data-testid="list-skeleton"]', '0001-ac25.png')
})

test('AC-26: daraltma sonucu boş', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    const daraltilmis = new URL(route.request().url()).searchParams.getAll('bodyType').length > 0
    await route.fulfill({
      json: daraltilmis
        ? { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 }
        : { items: [], page: 1, pageSize: 24, totalCount: 42, totalPages: 2 },
    })
  })
  await page.goto('/tr/?bodyType=suv')
  await kanit(page, '[data-testid="empty-by-filter"]', '0001-ac26.png')
})

test('AC-27: vitrinde hiç ilan yok', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    await route.fulfill({
      json: { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 },
    })
  })
  await page.goto('/tr/?bodyType=suv')
  await kanit(page, '[data-testid="empty-showcase"]', '0001-ac27.png')
})

test('AC-28: hata durumu', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    await route.fulfill({
      status: 500,
      json: { errorCode: 'catalog.unavailable', traceId: '00-abc-123-01' },
    })
  })
  await page.goto('/tr/')
  await kanit(page, '[data-testid="list-error"]', '0001-ac28.png')
})

test('AC-29: kasa tipi etiketleri — üç locale', async ({ page }) => {
  for (const locale of ['tr', 'en', 'ar'] as const) {
    await page.goto(`/${locale}/`)
    await kanit(page, '[data-testid="body-type-filter"]', `0001-ac29-${locale}.png`)
  }
})

test('AC-30: Arapça RTL — ızgara ve sayfalama', async ({ page }) => {
  await page.goto('/ar/')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')

  // Kriter ızgarayı VE sayfalama denetimlerini kapsıyor; tek kadraj ikisini birden
  // almıyordu (QA D11). İki kanıt üretilir.
  await page.screenshot({ path: `${KANIT}/0001-ac30-ar.png`, fullPage: false })

  const sayfalama = page.getByRole('navigation')
  await sayfalama.scrollIntoViewIfNeeded()
  await expect(sayfalama).toBeVisible()
  await sayfalama.screenshot({ path: `${KANIT}/0001-ac30-ar-b.png` })
})

test('AC-31: sonuç sayısı ICU çoğul — Arapça', async ({ page }) => {
  await page.goto('/ar/')
  await kanit(page, '[data-testid="result-count"]', '0001-ac31-ar.png')
})

test('AC-32: görsel alt metni', async ({ page }) => {
  await page.goto('/ar/')
  const alt = await page.locator('.listing-card__image').first().getAttribute('alt')
  expect(alt).toBeTruthy()

  // alt metni görüntüde görünmez; kanıt için görselin üstüne basılır.
  await page
    .getByTestId('listing-card')
    .first()
    .evaluate((node, metin) => {
      const etiket = node.ownerDocument.createElement('p')
      etiket.textContent = `alt="${metin}"`
      etiket.style.cssText =
        'margin:0;padding:8px;font:14px monospace;background:#16191d;color:#fff'
      node.prepend(etiket)
    }, alt)

  await kanit(page, '[data-testid="listing-card"]', '0001-ac32-ar.png')
})
