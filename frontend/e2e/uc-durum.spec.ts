import { expect, test } from '@playwright/test'

/**
 * Boş ve hata durumları en sık atlanan ve en sık bozuk olanlardır (docs/testing.md § 3).
 * "Vitrinde hiç ilan yok" ve "veri alınamadı" durumları gerçek veriyle üretilemediği için
 * API yanıtı araya girilerek kurulur; ekranın kendisi gerçek bileşendir.
 */
test.describe('Üç durum', () => {
  test('AC-25: Liste verisi yüklenirken ekranda iskelet gösterilir', async ({ page }) => {
    await page.route('**/api/catalog/showcase-listings*', async (route) => {
      await new Promise((resolve) => setTimeout(resolve, 1500))
      await route.continue()
    })

    await page.goto('/tr/')
    await expect(page.getByTestId('list-skeleton')).toBeVisible()
  })

  test('AC-26: Daraltma sonucu boşken ekran seçime uyan ilan olmadığını bildirir ve seçimi temizle eylemini sunar', async ({
    page,
  }) => {
    await page.route('**/api/catalog/showcase-listings*', async (route) => {
      const url = new URL(route.request().url())
      const daraltilmis = url.searchParams.getAll('bodyType').length > 0

      await route.fulfill({
        json: daraltilmis
          ? { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 }
          : { items: [], page: 1, pageSize: 24, totalCount: 42, totalPages: 2 },
      })
    })

    await page.goto('/tr/?bodyType=suv')

    await expect(page.getByTestId('empty-by-filter')).toBeVisible()
    await expect(page.getByTestId('empty-showcase')).toHaveCount(0)
    // Eylem panelin İÇİNDE sunulur; filtre çubuğundaki aynı adlı düğmeyle karışmasın.
    await expect(
      page.getByTestId('empty-by-filter').getByRole('button', { name: 'Seçimi temizle' }),
    ).toBeVisible()
  })

  test('AC-27: Vitrinde hiç yayında ilan yokken — daraltma seçili olsa da — ekran araç bulunmadığını bildirir ve seçimi temizle eylemi gösterilmez', async ({
    page,
  }) => {
    await page.route('**/api/catalog/showcase-listings*', async (route) => {
      await route.fulfill({
        json: { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 },
      })
    })

    await page.goto('/tr/?bodyType=suv')

    // İkisi aynı anda doğru: (b) kazanır (R12).
    await expect(page.getByTestId('empty-showcase')).toBeVisible()
    await expect(page.getByTestId('empty-by-filter')).toHaveCount(0)
  })

  test('AC-28: Liste verisi alınamadığında ekran çeviri kataloğundaki mesajı ve tekrar dene eylemini gösterir; teknik detay göstermez', async ({
    page,
  }) => {
    await page.route('**/api/catalog/showcase-listings*', async (route) => {
      await route.fulfill({
        status: 500,
        json: { errorCode: 'catalog.unavailable', traceId: '00-abc-123-01' },
      })
    })

    await page.goto('/tr/')

    const hata = page.getByTestId('list-error')
    await expect(hata).toBeVisible()
    await expect(page.getByRole('button', { name: 'Tekrar dene' })).toBeVisible()

    const govde = await page.locator('body').innerText()
    for (const sizinti of ['500', 'catalog.unavailable', '00-abc-123-01', '/api/catalog']) {
      expect(govde).not.toContain(sizinti)
    }
  })
})
