import { expect, test, type Page } from '@playwright/test'

/** frontend-001 kanıt üretimi. Yalnız `--project=masaustu` ile çağrılır. */
const KANIT = '../docs/evidence'

async function kanit(page: Page, selector: string, dosya: string) {
  await page
    .locator(selector)
    .first()
    .screenshot({ path: `${KANIT}/${dosya}` })
}

test('AC-1: üst bar ve içerik alanı', async ({ page }) => {
  await page.goto('/tr/')
  await expect(page.locator('main h1')).toBeVisible()
  await kanit(page, 'header.top-bar', 'frontend-001-ac1.png')
})

test('AC-2: dil değiştirici üç locale sunar', async ({ page }) => {
  await page.goto('/tr/')
  await kanit(page, '.top-bar__locales', 'frontend-001-ac2.png')
})

test('AC-4: Arapça üst bar sağdan sola', async ({ page }) => {
  await page.goto('/ar/')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await kanit(page, 'header.top-bar', 'frontend-001-ac4-ar.png')
})

test('AC-6: kart alanları', async ({ page }) => {
  await page.goto('/tr/')
  await kanit(page, '[data-testid="listing-card"]', 'frontend-001-ac6.png')
})

test('AC-7: rezerve rozeti', async ({ page }) => {
  await page.goto('/tr/')
  const kart = page.getByTestId('listing-card').filter({ hasText: 'Rezerve' }).first()
  await expect(kart).toBeVisible()
  await kart.screenshot({ path: `${KANIT}/frontend-001-ac7.png` })
})

test('AC-8: sayfalama kontrolü', async ({ page }) => {
  await page.goto('/tr/')
  const sayfalama = page.locator('.pagination')
  await sayfalama.scrollIntoViewIfNeeded()
  await kanit(page, '.pagination', 'frontend-001-ac8.png')
})

test('AC-10: iskelet', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 2000))
    await route.continue()
  })
  await page.goto('/tr/')
  await expect(page.getByTestId('list-skeleton')).toBeVisible()
  await kanit(page, '[data-testid="list-skeleton"]', 'frontend-001-ac10.png')
})

test('AC-11: boş katalog', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    await route.fulfill({
      json: { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 },
    })
  })
  await page.goto('/tr/')
  await kanit(page, '[data-testid="empty-showcase"]', 'frontend-001-ac11.png')
})

test('AC-12, AC-13: hata durumu — üç locale', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', async (route) => {
    await route.fulfill({
      status: 500,
      json: { errorCode: 'catalog.unavailable', traceId: '00-abc-1' },
    })
  })

  for (const locale of ['tr', 'en', 'ar'] as const) {
    await page.goto(`/${locale}/`)
    const hata = page.getByTestId('list-error')
    await expect(hata).toBeVisible()

    const govde = await page.locator('body').innerText()
    for (const sizinti of ['500', 'catalog.unavailable', '00-abc-1', '/api/catalog']) {
      expect(govde).not.toContain(sizinti)
    }

    await hata.screenshot({ path: `${KANIT}/frontend-001-ac13-${locale}.png` })
  }
})
