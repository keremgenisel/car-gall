import { expect, test, type Page } from '@playwright/test'
import { kanit } from './kanit'

/** Listedeki ilk ilanın slug'ı; seed'e göre değişir, sabitlenmez. */
async function ilkSlug(page: Page, locale = 'tr') {
  await page.goto(`/${locale}/`)
  const href = await page.getByTestId('listing-card').first().getByRole('link').getAttribute('href')
  return (href as string).split('/').pop() as string
}

test('0002/AC-2: karttan detaya giden bağlantı', async ({ page }) => {
  await page.goto('/tr/')
  await kanit(page, '[data-testid="listing-card"]', '0002-ac2.png')
})

test('0002/AC-10, 0002/AC-11: detay alanları ve çözülmüş etiketler', async ({ page }) => {
  const slug = await ilkSlug(page)
  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('listing-detail')).toBeVisible()
  await kanit(page, '[data-testid="listing-detail"]', '0002-ac10.png')
})

test('0002/AC-9: rezerve rozeti detayda', async ({ page }) => {
  const slug = await ilkSlug(page)

  await page.route(`**/api/catalog/showcase-listings/${slug}*`, async (route) => {
    const govde = await (await route.fetch()).json()
    await route.fulfill({ json: { ...govde, isReserved: true } })
  })

  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('listing-detail')).toBeVisible()
  await kanit(page, '[data-testid="listing-detail"]', '0002-ac9.png')
})

test('0002/AC-15: fiyat sorunuz', async ({ page }) => {
  const slug = await ilkSlug(page)

  await page.route(`**/api/catalog/showcase-listings/${slug}*`, async (route) => {
    const govde = await (await route.fetch()).json()
    await route.fulfill({
      json: {
        ...govde,
        price: { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null },
      },
    })
  })

  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('detail-price')).toBeVisible()
  await kanit(page, '[data-testid="detail-price"]', '0002-ac15.png')
})

test('0002/AC-16: KWD üç ondalık — üç locale', async ({ page }) => {
  const slug = await ilkSlug(page)

  await page.route(`**/api/catalog/showcase-listings/${slug}*`, async (route) => {
    const govde = await (await route.fetch()).json()
    await route.fulfill({
      json: {
        ...govde,
        price: { onRequest: false, amount: 1250.5, currencyCode: 'KWD', minorUnitExponent: 3 },
      },
    })
  })

  for (const locale of ['tr', 'ar', 'en'] as const) {
    await page.goto(`/${locale}/cars/${slug}`)
    await expect(page.getByTestId('detail-price')).toBeVisible()
    await kanit(page, '[data-testid="detail-price"]', `0002-ac16-${locale}.png`)
  }
})

test('0002/AC-17: yükleniyor iskeleti', async ({ page }) => {
  const slug = await ilkSlug(page)

  await page.route('**/api/catalog/showcase-listings/*', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 2000))
    await route.continue()
  })

  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('detail-skeleton')).toBeVisible()
  await kanit(page, '[data-testid="detail-skeleton"]', '0002-ac17.png')
})

test('0002/AC-18: bulunamadı — üç locale', async ({ page }) => {
  for (const locale of ['tr', 'ar', 'en'] as const) {
    await page.goto(`/${locale}/cars/boyle-bir-ilan-yok-000000`)
    await expect(page.getByTestId('detail-not-found')).toBeVisible()
    await kanit(page, '[data-testid="detail-not-found"]', `0002-ac18-${locale}.png`)
  }
})

test('0002/AC-19, 0002/AC-20: hata durumu — üç locale', async ({ page }) => {
  const slug = await ilkSlug(page)

  await page.route('**/api/catalog/showcase-listings/*', async (route) => {
    await route.fulfill({
      status: 500,
      json: { errorCode: 'catalog.unavailable', traceId: '00-abc-1' },
    })
  })

  for (const locale of ['tr', 'ar', 'en'] as const) {
    await page.goto(`/${locale}/cars/${slug}`)
    await expect(page.getByTestId('detail-error')).toBeVisible()
    await kanit(page, '[data-testid="detail-error"]', `0002-ac19-${locale}.png`)
  }
})

test('0002/AC-21, 0002/AC-22, 0002/AC-23: Arapça detay — RTL ve karışık yönlü model adı', async ({
  page,
}) => {
  const slug = await ilkSlug(page, 'ar')
  await page.goto(`/ar/cars/${slug}`)

  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByTestId('listing-detail')).toBeVisible()
  await kanit(page, '[data-testid="listing-detail"]', '0002-ac22-ar.png')
})

test('0002/AC-21: detay etiketleri — üç locale', async ({ page }) => {
  const slug = await ilkSlug(page)

  for (const locale of ['tr', 'ar', 'en'] as const) {
    await page.goto(`/${locale}/cars/${slug}`)
    await expect(page.getByTestId('detail-attributes')).toBeVisible()
    await kanit(page, '[data-testid="detail-attributes"]', `0002-ac21-${locale}.png`)
  }
})
