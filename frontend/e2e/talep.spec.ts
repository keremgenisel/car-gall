import { expect, test, type Page } from '@playwright/test'

/**
 * 0005'in kritik akışı: ziyaretçi bir ilan detayından talep bırakır
 * (docs/testing.md § 3, akış 2). Panelde görünme yarısı bu spec'te YOKTUR — D1 talep
 * okuma yüzeyini kapsam dışı bıraktı.
 */

/** Listedeki ilk ilanın slug'ı; seed'e göre değişir, sabitlenmez. */
async function ilkSlug(page: Page, locale = 'tr') {
  await page.goto(`/${locale}/`)
  const href = await page.getByTestId('listing-card').first().getByRole('link').getAttribute('href')
  return (href as string).split('/').pop() as string
}

/** Her testin kendi anahtarı: aynı anahtar ikinci kayıt üretmez (R17). */
function benzersizAnahtar() {
  return `e2e-${Date.now()}-${Math.random().toString(36).slice(2)}`
}

async function doldur(page: Page, ad: string, telefon: string) {
  await page
    .getByTestId('lead-form')
    .getByLabel(/Adınız|Your name|اسمك/)
    .fill(ad)
  await page
    .getByTestId('lead-form')
    .getByLabel(/^Telefon$|^Phone$|^الهاتف$/)
    .fill(telefon)
}

test('ziyaretçi ilan detayından talep bırakır ve talebinin alındığı bildirilir', async ({
  page,
}) => {
  const slug = await ilkSlug(page)
  const anahtar = benzersizAnahtar()

  // Anahtar uçta üretiliyor; testin kontrol edebilmesi için istekte sabitleniyor.
  await page.route('**/api/leads', async (route) => {
    const headers = { ...route.request().headers(), 'idempotency-key': anahtar }
    await route.continue({ headers })
  })

  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('lead-form')).toBeVisible()

  await doldur(page, 'Ayşe Yılmaz', '+905321112233')
  await page.getByRole('button', { name: 'Talebimi gönder' }).click()

  await expect(page.getByTestId('lead-success')).toBeVisible()
  await expect(page.getByText('Talebiniz alındı')).toBeVisible()
})

test('AC-42: Arapça locale de form dir="rtl" ile basılır', async ({ page }) => {
  const slug = await ilkSlug(page, 'ar')

  await page.goto(`/ar/cars/${slug}`)
  await expect(page.getByTestId('lead-form')).toBeVisible()

  // Yön locale'in özelliğidir ve SUNUCUDAN basılır; uçta yeniden hesaplanmaz.
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

  const etiket = page.getByTestId('lead-form').getByLabel(/اسمك/)
  await expect(etiket).toBeVisible()

  // Etiket sağa hizalı: RTL akışında yazı başlangıcı sağdadır.
  const yon = await page
    .getByTestId('lead-form')
    .evaluate((element) => getComputedStyle(element).direction)

  expect(yon).toBe('rtl')
})

test('AC-15: sayfa açıldıktan sonra ilan vitrinden düşerse bilgiler kaybolmaz', async ({
  page,
}) => {
  const slug = await ilkSlug(page)

  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('lead-form')).toBeVisible()

  // İlan sayfa açıkken vitrinden düştü.
  await page.route('**/api/leads', async (route) => {
    await route.fulfill({
      status: 422,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 422, errorCode: 'lead.listing_unavailable' }),
    })
  })

  await doldur(page, 'Mehmet Demir', '+905321112233')
  await page.getByRole('button', { name: 'Talebimi gönder' }).click()

  await expect(page.getByTestId('lead-error')).toBeVisible()
  await expect(page.getByText(/artık vitrinde değil/)).toBeVisible()
  await expect(page.getByRole('link', { name: 'Vitrine dön' })).toBeVisible()

  // Girdiği bilgiler KAYBOLMAZ.
  await expect(page.getByTestId('lead-form').getByLabel('Adınız')).toHaveValue('Mehmet Demir')
})

test('AC-11: rezerve ilanda form gösterilmez, yerinde neden kapalı olduğu yazar', async ({
  page,
}) => {
  const slug = await ilkSlug(page)

  await page.route(`**/api/catalog/showcase-listings/${slug}*`, async (route) => {
    const govde = await (await route.fetch()).json()
    await route.fulfill({ json: { ...govde, isReserved: true } })
  })

  await page.goto(`/tr/cars/${slug}`)

  await expect(page.getByTestId('lead-closed')).toBeVisible()
  await expect(page.getByTestId('lead-form')).toHaveCount(0)
})
