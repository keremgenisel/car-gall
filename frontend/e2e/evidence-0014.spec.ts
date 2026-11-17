import { expect, test, type Page } from '@playwright/test'
import { kanit } from './kanit'

/**
 * 0014 — görsel kanıt (DoD T2): boş ve hata durumları ve Arapça dâhil. Boş ve hata hâlleri
 * gerçek veriyle üretilemediği için API yanıtı araya girilerek kurulur; ekranın kendisi
 * gerçek bileşendir (`uc-durum.spec.ts` deseni).
 */
async function galeriAdi(page: Page, ad: string) {
  await page.route('**/api/dealership/gallery*', async (route) => {
    const yanit = await route.fetch()
    await route.fulfill({ response: yanit, json: { ...(await yanit.json()), commercialName: ad } })
  })
}

test('0014/AC-12, AC-16, AC-40, AC-42: ana sayfanın tamamı (Türkçe)', async ({ page }) => {
  await galeriAdi(page, 'Örnek Otomotiv')
  await page.goto('/tr/')
  await expect(page.getByTestId('gallery-name')).toHaveText('Örnek Otomotiv')
  await expect(page.getByTestId('narrowing-form')).toBeVisible()
  await expect(page.getByTestId('branch-list')).toBeVisible()

  await kanit(page, 'main', '0014-ac12-tr.png')
  await kanit(page, '[data-testid="narrowing-form"]', '0014-ac40.png')
  await kanit(page, '[data-testid="home-footer"]', '0014-ac42.png')
})

test('0014/AC-17, AC-22: öne çıkan bölüm', async ({ page }) => {
  await page.goto('/tr/')
  await expect(page.getByTestId('featured-section')).toBeVisible()
  await kanit(page, '[data-testid="featured-section"]', '0014-ac17.png')
})

test('0014/AC-57, AC-72: Arapça ana sayfa ve yalıtılmış Latin galeri adı', async ({ page }) => {
  await galeriAdi(page, 'Örnek Otomotiv')
  await page.goto('/ar/')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByTestId('gallery-name')).toHaveText('Örnek Otomotiv')

  await kanit(page, 'main', '0014-ac57-ar.png')
  await kanit(page, '[data-testid="home-intro"]', '0014-ac72-ar.png')
  await kanit(page, '[data-testid="home-footer"]', '0014-ac47-ar.png')
})

test('0014/AC-73: Arapça harfli galeri adı', async ({ page }) => {
  await galeriAdi(page, 'معرض المثال')
  await page.goto('/ar/')
  await expect(page.getByTestId('gallery-name')).toHaveText('معرض المثال')
  await kanit(page, '[data-testid="home-intro"]', '0014-ac73-ar.png')
})

test('0014/AC-49: iskeletler', async ({ page }) => {
  for (const yol of [
    '**/api/catalog/featured-listings*',
    '**/api/catalog/showcase-listings*',
    '**/api/dealership/gallery*',
  ]) {
    await page.route(yol, () => new Promise(() => {}))
  }
  await page.goto('/tr/')
  await expect(page.getByTestId('featured-skeleton')).toBeVisible()
  await expect(page.getByTestId('narrowing-form-skeleton')).toBeVisible()
  await expect(page.getByTestId('branch-skeleton')).toBeAttached()
  await kanit(page, 'body', '0014-ac49.png')
})

test('0014/AC-50, AC-51: boş vitrin', async ({ page }) => {
  await page.route('**/api/catalog/showcase-listings*', (route) =>
    route.fulfill({ json: { items: [], page: 1, pageSize: 1, totalCount: 0, totalPages: 0 } }),
  )
  await page.goto('/tr/')
  await expect(page.getByTestId('empty-showcase')).toBeVisible()
  await expect(page.getByText('0 aracı gör')).toHaveCount(0)
  await expect(page.getByTestId('home-footer')).toBeVisible()
  await kanit(page, 'body', '0014-ac50.png')
})

test('0014/AC-52, AC-53: kısmi hata — her bölüm kendi hatasını taşır', async ({ page }) => {
  await page.route('**/api/catalog/featured-listings*', (route) =>
    route.fulfill({ status: 500, json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-1' } }),
  )
  await page.goto('/tr/')
  await expect(page.getByTestId('featured-error')).toBeVisible()
  await expect(page.getByTestId('narrowing-form')).toBeVisible()
  await expect(page.locator('body')).not.toContainText('trace-e2e-1')
  await kanit(page, 'main', '0014-ac53.png')

  await page.unrouteAll()
  await page.route('**/api/catalog/showcase-listings*', (route) =>
    route.fulfill({ status: 500, json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-2' } }),
  )
  await page.goto('/tr/')
  await expect(page.getByTestId('narrowing-form-error')).toBeVisible()
  await expect(page.getByTestId('empty-showcase')).toHaveCount(0)
  await kanit(page, 'main', '0014-ac52.png')
})

test('0014/AC-54: galeri okuması düşerken sayfa ayakta', async ({ page }) => {
  await page.route('**/api/dealership/gallery*', (route) =>
    route.fulfill({ status: 500, json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-3' } }),
  )
  await page.goto('/tr/')
  await expect(page.getByTestId('narrowing-form')).toBeVisible()
  await expect(page.getByTestId('branch-list')).toHaveCount(0)
  await expect(page.locator('body')).not.toContainText('trace-e2e-3')
  await kanit(page, 'body', '0014-ac54.png')
})
