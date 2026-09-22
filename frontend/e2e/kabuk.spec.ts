import { expect, test } from '@playwright/test'

test.describe('Uygulama kabuğu', () => {
  test("frontend-001/AC-1: Vitrin sayfasında üst bar görünür ve marka adını taşır; içerik alanı <main> öğesidir ve sayfanın tek <h1>'i içerik alanındadır", async ({
    page,
  }) => {
    await page.goto('/tr/')

    await expect(page.locator('header.top-bar')).toBeVisible()
    await expect(page.getByText('Oto Galeri')).toBeVisible()

    await expect(page.locator('main')).toHaveCount(1)
    await expect(page.locator('h1')).toHaveCount(1)
    await expect(page.locator('main h1')).toBeVisible()
  })

  test("frontend-001/AC-2: Üst bardaki dil değiştirici üç etkin locale'i sunar; /tr/ sayfasında Arapça seçildiğinde adres /ar/ önekine döner ve içerik Arapça gelir", async ({
    page,
  }) => {
    await page.goto('/tr/')

    const switcher = page.getByRole('navigation', { name: 'Dil seçimi' })
    await expect(switcher.getByRole('link')).toHaveCount(3)

    await switcher.getByRole('link', { name: 'العربية' }).click()

    await expect(page).toHaveURL(/\/ar\//)
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.getByTestId('body-type-filter')).toContainText('دفع رباعي')
  })

  test('frontend-001/AC-3: Dil değiştiriciyle locale değiştirildiğinde daraltma ve sayfa korunur', async ({
    page,
  }) => {
    await page.goto('/tr/?bodyType=suv&page=2')
    await expect(page.getByTestId('result-count')).toBeVisible()

    await page
      .getByRole('navigation', { name: 'Dil seçimi' })
      .getByRole('link', { name: 'العربية' })
      .click()

    const url = new URL(page.url())
    expect(url.pathname).toBe('/ar/')
    expect(url.searchParams.getAll('bodyType')).toEqual(['suv'])
    expect(url.searchParams.get('page')).toBe('2')
  })

  test("frontend-001/AC-4: Arapça locale'de üst bar sağdan sola akar: marka satırın başında (sağda), dil değiştirici sonunda (solda)", async ({
    page,
  }) => {
    await page.goto('/ar/')

    const marka = await page.locator('.top-bar__brand').boundingBox()
    const secici = await page.locator('.top-bar__locales').boundingBox()

    expect(marka).not.toBeNull()
    expect(secici).not.toBeNull()
    expect(marka!.x).toBeGreaterThan(secici!.x)
  })

  test('frontend-001/AC-5: Dil değiştirici çerez veya localStorage yazmaz; seçim yalnız adrese yansır', async ({
    page,
  }) => {
    await page.goto('/tr/')
    await page
      .getByRole('navigation', { name: 'Dil seçimi' })
      .getByRole('link', { name: 'العربية' })
      .click()
    await expect(page).toHaveURL(/\/ar\//)

    const cerez = await page.context().cookies()
    const depolama = await page.evaluate(() => ({
      local: Object.keys(window.localStorage).length,
      session: Object.keys(window.sessionStorage).length,
    }))

    expect(cerez).toHaveLength(0)
    expect(depolama.local).toBe(0)
    expect(depolama.session).toBe(0)
  })
})
