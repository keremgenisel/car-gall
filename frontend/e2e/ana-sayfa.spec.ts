import { expect, test } from '@playwright/test'

/**
 * 0014 — ana sayfa, gerçek Host'a karşı: adresler, geri tuşu, form ve kartın gidişi.
 * Sunucu yarısı (`301`, hedef adres) `Host.Tests/AnaSayfaAdresTests`'te; burada ölçülen
 * tarayıcının gördüğü.
 */
test.describe('Ana sayfa', () => {
  for (const [locale, baslik] of [
    ['tr', 'Geçmişi gizlenmeyen araçlar.'],
    ['ar', 'سيارات لا يُخفى تاريخها.'],
    ['en', 'Cars with nothing to hide.'],
  ] as const) {
    test(`0014/AC-1: /${locale}/ o dilde ana sayfayı gösterir`, async ({ page }) => {
      await page.goto(`/${locale}/`)

      await expect(page.locator('html')).toHaveAttribute('lang', locale)
      await expect(page.locator('main h1')).toHaveText(baslik)
      await expect(page.getByTestId('narrowing-form')).toBeVisible()
      // Liste DEĞİL: liste ekranının sonuç sayacı burada yok.
      await expect(page.getByTestId('result-count')).toHaveCount(0)
    })
  }

  test('0014/AC-6: /tr/?bodyType=suv&page=2 açıldığında ziyaretçi /tr/cars?bodyType=suv&page=2 adresindeki listeyi görür', async ({
    page,
  }) => {
    await page.goto('/tr/?bodyType=suv&page=2')

    const url = new URL(page.url())
    expect(url.pathname).toBe('/tr/cars')
    expect(url.searchParams.getAll('bodyType')).toEqual(['suv'])
    await expect(page.getByTestId('result-count')).toBeVisible()
  })

  test("0014/AC-7: AC-6'daki geçişten sonra tarayıcıda geri demek ziyaretçiyi /tr/?bodyType=suv&page=2'ye değil, ondan önceki sayfaya götürür", async ({
    page,
  }) => {
    await page.goto('/en/')
    await expect(page.locator('main h1')).toBeVisible()

    await page.goto('/tr/?bodyType=suv&page=2')
    await expect(page).toHaveURL(/\/tr\/cars\?/)

    await page.goBack()

    // Ara kayıt olsaydı geri tuşu eski adrese, oradan da yeniden listeye düşerdi.
    expect(new URL(page.url()).pathname).toBe('/en/')
  })

  test('0014/AC-8: /tr/ liste parametresi taşımıyorsa yönlendirme olmaz; /tr/?utm_source=x de ana sayfayı gösterir', async ({
    page,
  }) => {
    await page.goto('/tr/?utm_source=x')

    const url = new URL(page.url())
    expect(url.pathname).toBe('/tr/')
    expect(url.searchParams.get('utm_source')).toBe('x')
    await expect(page.getByTestId('home-intro')).toBeVisible()
  })

  test('0014/AC-26: Karta tıklamak o ilanın detayına (/{locale}/cars/{slug}) gider', async ({
    page,
  }) => {
    await page.goto('/tr/')

    const bolum = page.getByTestId('featured-section')
    await expect(bolum).toBeVisible()
    const baglanti = bolum.getByRole('link').first()
    const hedef = await baglanti.getAttribute('href')

    await baglanti.click()

    expect(hedef).toMatch(/^\/tr\/cars\/[^/]+$/)
    expect(new URL(page.url()).pathname).toBe(hedef)
    await expect(page.getByTestId('detail-price')).toBeVisible()
  })

  test('0014/AC-38: sedan ve suv seçilip gönderilince ziyaretçi /{locale}/cars?bodyType=sedan&bodyType=suv adresine gider', async ({
    page,
  }) => {
    await page.goto('/en/')
    const form = page.getByTestId('narrowing-form')
    await expect(form).toBeVisible()

    // Tıklama sırası ters: adres kasa tiplerinin sırasını izler.
    await form.getByRole('checkbox', { name: 'SUV' }).check()
    await form.getByRole('checkbox', { name: 'Sedan' }).check()
    await form.getByRole('button').click()

    await expect(page).toHaveURL(/\/en\/cars\?/)
    expect(new URL(page.url()).search).toBe('?bodyType=sedan&bodyType=suv')
    await expect(page.getByTestId('result-count')).toBeVisible()
  })

  test("0014/AC-39: Hiçbir şey seçilmeden gönderilince ziyaretçi parametresiz /{locale}/cars'a gider", async ({
    page,
  }) => {
    await page.goto('/ar/')
    await page.getByTestId('narrowing-form').getByRole('button').click()

    await expect(page).toHaveURL(/\/ar\/cars$/)
  })

  test('0014/AC-57: /ar/ sayfası <html dir="rtl" lang="ar"> ile gelir ve bölümlerin hizası aynalanmış görünür', async ({
    page,
  }) => {
    await page.goto('/ar/')

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await expect(page.locator('html')).toHaveAttribute('lang', 'ar')

    // Aynalanma: başlık METNİ satırın başında, yani sağ kenarda durur. `h1` bir blok
    // öğedir ve kutusu satırın tamamını kaplar; kutuyu ölçmek hizayı ölçmez (ilk
    // yazımdaki hata: iki boşluk da 16px çıktı). Metnin kendi sınırı Range ile alınır.
    const { metinSol, metinSag, kutuSol, kutuSag } = await page
      .locator('main h1')
      .evaluate((h1) => {
        const aralik = document.createRange()
        aralik.selectNodeContents(h1)
        const metin = aralik.getBoundingClientRect()
        const kutu = h1.getBoundingClientRect()
        return {
          metinSol: metin.left,
          metinSag: metin.right,
          kutuSol: kutu.left,
          kutuSag: kutu.right,
        }
      })
    expect(kutuSag - metinSag).toBeLessThan(1)
    expect(metinSol - kutuSol).toBeGreaterThan(kutuSag - metinSag)
  })
})
