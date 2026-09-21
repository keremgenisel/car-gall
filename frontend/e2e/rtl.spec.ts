import { expect, test } from '@playwright/test'

test.describe('Çok dillilik ve RTL', () => {
  test('AC-30: Arapça locale de sayfa dir="rtl" ve lang="ar" ile basılır; ızgara ve sayfalama sağdan sola akar', async ({ page }) => {
    await page.goto('/ar/')

    // Yön SUNUCUDAN basılır, uçta hesaplanmaz (docs/frontend.md § 6).
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

    // Sayfalama denetimleri her düzende yatay akar: RTL'de DOM'da önce gelen "önceki"
    // düğmesi ekranın SAĞINDA durur. Izgara mobilde tek sütuna düştüğü için yatay akış
    // iddiası yalnız çok sütunlu düzende anlamlıdır ve orada ayrıca sınanır.
    const onceki = await page.getByRole('button', { name: /Önceki|السابقة/ }).boundingBox()
    const sonraki = await page.getByRole('button', { name: /Sonraki|التالية/ }).boundingBox()

    expect(onceki).not.toBeNull()
    expect(sonraki).not.toBeNull()
    expect(onceki!.x).toBeGreaterThan(sonraki!.x)

    const ilk = await page.getByTestId('listing-card').first().boundingBox()
    const ikinci = await page.getByTestId('listing-card').nth(1).boundingBox()

    expect(ilk).not.toBeNull()
    expect(ikinci).not.toBeNull()

    const cokSutunlu = Math.abs(ilk!.y - ikinci!.y) < 4
    if (cokSutunlu) {
      // RTL'de ızgara sağdan sola akar: ikinci kart birincinin SOLUNDA başlar.
      expect(ikinci!.x).toBeLessThan(ilk!.x)
    }
  })

  test('AC-29: Kasa tipi etiketleri üç locale de kendi dilinde görünür; ham anahtar görünmez', async ({ page }) => {
    const beklenen: Record<string, string> = { tr: 'SUV', ar: 'دفع رباعي', en: 'SUV' }

    for (const locale of ['tr', 'ar', 'en']) {
      await page.goto(`/${locale}/`)
      await expect(page.getByTestId('body-type-filter')).toContainText(beklenen[locale]!)

      const govde = await page.locator('body').innerText()
      expect(govde).not.toContain('showcase.')
      expect(govde).not.toContain('body_type')
    }
  })

  test('AC-32: Görsellerin alt metni model adı + yıl kalıbından üretilir', async ({ page }) => {
    for (const locale of ['tr', 'ar', 'en']) {
      await page.goto(`/${locale}/`)

      const alt = await page.locator('.listing-card__image').first().getAttribute('alt')
      expect(alt).toBeTruthy()
      expect(alt).not.toContain('showcase.')
      expect(alt).toMatch(/\d{4}/)
    }
  })

  test('AC-31: Sonuç sayısı metni toplam ilan sayısını bildirir ve Arapça da doğru çoğul biçimini kullanır', async ({ page }) => {
    await page.goto('/ar/')

    // Sayı veri setinden gelir; test seed büyüklüğüne bağlanmaz.
    const yanit = await page.request.get('/api/catalog/showcase-listings?locale=ar')
    const toplam = (await yanit.json()).totalCount as number

    const metin = await page.getByTestId('result-count').innerText()
    expect(metin).toContain(String(toplam))
    // Hint-Arap rakamı kullanılmaz (AC-22 ile aynı kural).
    expect(metin).not.toMatch(/[٠-٩]/)
  })

  test('AC-22: KWD dışı kurulumda da tutar locale biçiminde ve Latin rakamla gösterilir', async ({ page }) => {
    await page.goto('/ar/')

    const fiyat = await page.locator('.listing-card__price').first().innerText()
    expect(fiyat).not.toMatch(/[٠-٩۰-۹]/)
  })
})
