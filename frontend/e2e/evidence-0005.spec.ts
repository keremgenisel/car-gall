import { expect, test, type Page } from '@playwright/test'

/**
 * Spec 0005'in görsel kanıtları (docs/testing.md § 3). Görüntü KRİTERİN karşılandığını
 * gösterir, ekranın genel görünümünü değil. Yalnız masaüstü profilinde koşar — mobil
 * profil aynı dosyaları üzerine yazardı (playwright.config.ts testIgnore).
 */
const KANIT = '../docs/evidence'

async function kanit(page: Page, selector: string, dosya: string) {
  await page
    .locator(selector)
    .first()
    .screenshot({ path: `${KANIT}/${dosya}` })
}

async function ilkSlug(page: Page, locale = 'tr') {
  await page.goto(`/${locale}/`)
  const href = await page.getByTestId('listing-card').first().getByRole('link').getAttribute('href')
  return (href as string).split('/').pop() as string
}

const ETIKET = {
  tr: { ad: 'Adınız', telefon: 'Telefon', gonder: 'Talebimi gönder' },
  en: { ad: 'Your name', telefon: 'Phone', gonder: 'Send my request' },
  ar: { ad: 'اسمك', telefon: 'الهاتف', gonder: 'أرسل طلبي' },
} as const

async function formaGit(page: Page, locale: keyof typeof ETIKET) {
  const slug = await ilkSlug(page, locale)
  await page.goto(`/${locale}/cars/${slug}`)
  await expect(page.getByTestId('lead-form')).toBeVisible()
}

test('0005/AC-26, 0005/AC-27, 0005/AC-41: form, işaretsiz izin kutusu ve izin metni (TR)', async ({
  page,
}) => {
  await formaGit(page, 'tr')

  await expect(page.getByRole('checkbox')).not.toBeChecked()
  await kanit(page, '[data-testid="lead-form"]', '0005-ac26.png')
  await kanit(page, '.lead-form__consent', '0005-ac27-tr.png')
  await kanit(page, '[data-testid="lead-form"]', '0005-ac41-tr.png')
})

test('0005/AC-27, 0005/AC-41: izin metni ve form (EN)', async ({ page }) => {
  await formaGit(page, 'en')

  await kanit(page, '.lead-form__consent', '0005-ac27-en.png')
  await kanit(page, '[data-testid="lead-form"]', '0005-ac41-en.png')
})

test('0005/AC-27, 0005/AC-41, 0005/AC-42: izin metni, form ve RTL (AR)', async ({ page }) => {
  await formaGit(page, 'ar')

  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await kanit(page, '.lead-form__consent', '0005-ac27-ar.png')
  await kanit(page, '[data-testid="lead-form"]', '0005-ac41-ar.png')
  await kanit(page, '[data-testid="lead-form"]', '0005-ac42.png')
})

test('0005/AC-11: rezerve ilanda form kapalı', async ({ page }) => {
  const slug = await ilkSlug(page)

  await page.route(`**/api/catalog/showcase-listings/${slug}*`, async (route) => {
    const govde = await (await route.fetch()).json()
    await route.fulfill({ json: { ...govde, isReserved: true } })
  })

  await page.goto(`/tr/cars/${slug}`)
  await expect(page.getByTestId('lead-closed')).toBeVisible()
  await kanit(page, '[data-testid="lead-closed"]', '0005-ac11.png')
})

test('0005/AC-36: gönderim sırasında yükleniyor hâli', async ({ page }) => {
  await formaGit(page, 'tr')

  // Yanıt bilerek geciktiriliyor: yükleniyor hâli ancak açıkken görüntülenebilir.
  await page.route('**/api/leads', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 3000))
    await route.fulfill({ status: 201, json: { leadId: '0192c000-0000-7000-8000-00000000aaaa' } })
  })

  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.ad).fill('Ayşe Yılmaz')
  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.telefon).fill('+905321112233')
  await page.getByRole('button', { name: ETIKET.tr.gonder }).click()

  await expect(page.getByRole('button', { name: 'Gönderiliyor' })).toBeDisabled()
  await kanit(page, '[data-testid="lead-form"]', '0005-ac36.png')
})

test('0005/AC-37: talebin alındığı bildirilir', async ({ page }) => {
  await formaGit(page, 'tr')

  await page.route('**/api/leads', async (route) => {
    await route.fulfill({ status: 201, json: { leadId: '0192c000-0000-7000-8000-00000000aaaa' } })
  })

  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.ad).fill('Ayşe Yılmaz')
  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.telefon).fill('+905321112233')
  await page.getByRole('button', { name: ETIKET.tr.gonder }).click()

  await expect(page.getByTestId('lead-success')).toBeVisible()
  await kanit(page, '[data-testid="lead-success"]', '0005-ac37.png')
})

test('0005/AC-38: hata durumu, tekrar dene ve korunan bilgiler', async ({ page }) => {
  await formaGit(page, 'tr')

  await page.route('**/api/leads', async (route) => {
    await route.fulfill({ status: 500, contentType: 'application/problem+json', body: '{}' })
  })

  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.ad).fill('Mehmet Demir')
  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.telefon).fill('+905321112233')
  await page.getByRole('button', { name: ETIKET.tr.gonder }).click()

  await expect(page.getByTestId('lead-error')).toBeVisible()
  await kanit(page, '[data-testid="lead-form"]', '0005-ac38.png')
})

test('0005/AC-15: ilan vitrinden düştüğünde bilgiler kalır, listeye dönme sunulur', async ({
  page,
}) => {
  await formaGit(page, 'tr')

  await page.route('**/api/leads', async (route) => {
    await route.fulfill({
      status: 422,
      contentType: 'application/problem+json',
      body: JSON.stringify({ status: 422, errorCode: 'lead.listing_unavailable' }),
    })
  })

  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.ad).fill('Mehmet Demir')
  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.telefon).fill('+905321112233')
  await page.getByRole('button', { name: ETIKET.tr.gonder }).click()

  await expect(page.getByTestId('lead-error')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Vitrine dön' })).toBeVisible()
  await kanit(page, '[data-testid="lead-form"]', '0005-ac15.png')
})

test('0005/AC-40: doğrulama hatası olan alan işaretlenir', async ({ page }) => {
  await formaGit(page, 'tr')

  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.ad).fill('Ayşe Yılmaz')
  await page.getByTestId('lead-form').getByLabel(ETIKET.tr.telefon).fill('05321112233')
  await page.getByRole('button', { name: ETIKET.tr.gonder }).click()

  await expect(page.getByTestId('lead-form').getByLabel(ETIKET.tr.telefon)).toHaveAttribute(
    'aria-invalid',
    'true',
  )
  await kanit(page, '[data-testid="lead-form"]', '0005-ac40.png')
})
