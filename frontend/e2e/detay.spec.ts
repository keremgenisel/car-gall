import { expect, test, type Page } from '@playwright/test'

/**
 * İlan detayı (spec 0002). Kritik akış: ziyaretçi listeden bir karta tıklar, detaya
 * gider, adresi paylaşır. Görünmeyen ilan ile hiç var olmayan ilan AYNI cevabı alır;
 * ayrımın sızmadığı burada da doğrulanır (C13, AC-8).
 */

/** Listedeki ilk kartın detay adresi; slug seed'e göre değişir, sabitlenmez. */
async function ilkIlaninAdresi(page: Page, locale = 'tr') {
  await page.goto(`/${locale}/`)
  const href = await page.getByTestId('listing-card').first().getByRole('link').getAttribute('href')
  expect(href).not.toBeNull()
  return href as string
}

test.describe('İlan detayı', () => {
  test('0002/AC-2: Vitrin listesindeki ilan kartı, detay adresine giden bir bağlantı taşır', async ({
    page,
  }) => {
    const href = await ilkIlaninAdresi(page)
    expect(href).toMatch(/^\/tr\/cars\/[a-z0-9-]+$/)
  })

  test('0002/AC-1: Yayında durumundaki bir ilanın adresi istendiğinde o ilanın detayı döner', async ({
    page,
  }) => {
    await page.goto('/tr/')
    const bag = page.getByTestId('listing-card').first().getByRole('link')
    const model = ((await bag.textContent()) ?? '').trim()

    await bag.click()

    await expect(page.getByTestId('listing-detail')).toBeVisible()
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(model)
  })

  test('0002/AC-3: Detay adresi yeni bir oturumda doğrudan açıldığında aynı ilan görünür', async ({
    page,
    context,
  }) => {
    const href = await ilkIlaninAdresi(page)

    const yeniSayfa = await context.newPage()
    await yeniSayfa.goto(href)
    await expect(yeniSayfa.getByTestId('listing-detail')).toBeVisible()
    await yeniSayfa.close()
  })

  test('0002/AC-4: Üç locale aynı Latin slug ile aynı ilana gider, yalnız dil öneki değişir', async ({
    page,
  }) => {
    const href = await ilkIlaninAdresi(page)
    const slug = href.split('/').pop() as string

    for (const locale of ['tr', 'ar', 'en'] as const) {
      await page.goto(`/${locale}/cars/${slug}`)
      await expect(page.getByTestId('listing-detail')).toBeVisible()
      expect(new URL(page.url()).pathname).toBe(`/${locale}/cars/${slug}`)
    }
  })

  test('0002/AC-5, 0002/AC-8: Var olmayan ilan bulunamadı ekranı verir ve nedenini söylemez', async ({
    page,
  }) => {
    await page.goto('/tr/cars/boyle-bir-ilan-yok-000000')

    const ekran = page.getByTestId('detail-not-found')
    await expect(ekran).toBeVisible()

    // Gizli ilan ile hiç var olmayan ilan aynı metni alır: ayrım sızdırılamaz (C13).
    const metin = (await ekran.textContent()) ?? ''
    for (const sizinti of ['taslak', 'arşiv', 'satıldı', 'Taslak', 'Arşiv', 'Satıldı']) {
      expect(metin).not.toContain(sizinti)
    }
  })

  test('0002/AC-17: Detay verisi yüklenirken ekranda iskelet gösterilir', async ({ page }) => {
    const href = await ilkIlaninAdresi(page)

    await page.route('**/api/catalog/showcase-listings/*', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 1500))
      await route.continue()
    })

    await page.goto(href)
    await expect(page.getByTestId('detail-skeleton')).toBeVisible()
  })

  test('0002/AC-19, 0002/AC-20: Detay alınamadığında tekrar dene sunulur, teknik detay gösterilmez', async ({
    page,
  }) => {
    const href = await ilkIlaninAdresi(page)

    await page.route('**/api/catalog/showcase-listings/*', async (route) => {
      await route.fulfill({
        status: 500,
        json: { errorCode: 'catalog.unavailable', traceId: '00-abc-1' },
      })
    })

    await page.goto(href)

    const ekran = page.getByTestId('detail-error')
    await expect(ekran).toBeVisible()
    await expect(ekran.getByRole('button')).toBeVisible()

    const metin = (await ekran.textContent()) ?? ''
    for (const teknik of ['catalog.unavailable', '00-abc-1', '500', 'traceId']) {
      expect(metin).not.toContain(teknik)
    }
  })

  test('0002/AC-22: Arapça detay sayfası rtl yönü ve ar dili ile basılır', async ({ page }) => {
    const href = await ilkIlaninAdresi(page, 'ar')
    await page.goto(href)

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
    await expect(page.getByTestId('listing-detail')).toBeVisible()
  })

  test('0002/AC-23: Arapça gövdede Latin model adı karışık yönlü metin olarak işaretlenir', async ({
    page,
  }) => {
    const href = await ilkIlaninAdresi(page, 'ar')
    await page.goto(href)

    // dir="auto" olmadan Latin model adı RTL akışta ters sırayla görünür; noktalama
    // ve rakamlar yanlış tarafa kayar (docs/frontend.md § 6).
    await expect(page.getByRole('heading', { level: 1 })).toHaveAttribute('dir', 'auto')

    const deger = page.getByTestId('detail-attributes').locator('dd').first()
    await expect(deger).toHaveAttribute('dir', 'auto')
  })

  test('0002/AC-21: Detay etiketleri üç locale de kendi dilinde görünür, ham anahtar görünmez', async ({
    page,
  }) => {
    const href = await ilkIlaninAdresi(page)
    const slug = href.split('/').pop() as string

    for (const locale of ['tr', 'ar', 'en'] as const) {
      await page.goto(`/${locale}/cars/${slug}`)
      const metin = (await page.getByTestId('detail-attributes').textContent()) ?? ''
      expect(metin.trim().length).toBeGreaterThan(0)
      expect(metin).not.toContain('detail.field.')
    }
  })

  test('0002/AC-25: Kapak görselinin alt metni ziyaretçinin dilinden üretilir', async ({
    page,
  }) => {
    const href = await ilkIlaninAdresi(page)
    await page.goto(href)

    const gorsel = page.getByTestId('listing-detail').locator('img').first()
    const alt = await gorsel.getAttribute('alt')
    expect((alt ?? '').trim().length).toBeGreaterThan(0)
  })
})
