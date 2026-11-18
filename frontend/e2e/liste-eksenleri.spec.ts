import { expect, test, type Page } from '@playwright/test'

/**
 * 0015 — liste ekranının eksenleri ve sıralaması, gerçek Host'a karşı: adresin biçimi,
 * paylaşılabilirlik, geri tuşu ve aralık uygulaması. Sorgunun doğruluğu xUnit'te
 * (`VitrinEksenleriTests`, `VitrinSiralamaTests`); burada ölçülen tarayıcının gördüğü.
 */
async function aralik(page: Page, eksen: string, enAz: string, enCok: string) {
  const alan = page.getByTestId(`${eksen}-filter`)
  await alan.getByLabel('En az').fill(enAz)
  await alan.getByLabel('En çok').fill(enCok)
  await alan.getByRole('button', { name: 'Uygula' }).click()
}

test.describe('Liste eksenleri', () => {
  test('0015/AC-27: bütün eksenler ve "fiyat artan" seçildiğinde adres C12 biçimindedir', async ({
    page,
  }) => {
    await page.goto('/tr/cars')
    await page.getByTestId('fuels-filter').getByRole('checkbox', { name: 'Dizel' }).check()
    await page.getByTestId('fuels-filter').getByRole('checkbox', { name: 'Hibrit' }).check()
    await page
      .getByTestId('transmissions-filter')
      .getByRole('checkbox', { name: 'Otomatik' })
      .check()
    await aralik(page, 'year', '2018', '2021')
    await aralik(page, 'mileage', '', '80000')
    await aralik(page, 'price', '500000', '1500000')
    await page.getByTestId('sort-select').selectOption('price-asc')

    await expect
      .poll(() => new URL(page.url()).search)
      .toBe(
        '?fuel=diesel&fuel=hybrid&transmission=automatic&yearMin=2018&yearMax=2021&mileageMax=80000&priceMin=500000&priceMax=1500000&sort=price-asc',
      )
  })

  test('0015/AC-28: AC-27 adresi yeni bir sekmede açıldığında aynı seçimler işaretli ve aynı liste görünür', async ({
    page,
    context,
  }) => {
    const adres =
      '/tr/cars?fuel=diesel&fuel=hybrid&transmission=automatic&yearMin=2016&yearMax=2024&mileageMax=300000&priceMin=500000&priceMax=5000000&sort=price-asc'
    await page.goto(adres)
    await expect(page.getByTestId('result-count')).toBeVisible()
    const ilk = await page.getByTestId('result-count').textContent()
    const kartlar = await page.getByTestId('listing-card').allTextContents()

    const yeni = await context.newPage()
    await yeni.goto(adres)

    await expect(
      yeni.getByTestId('fuels-filter').getByRole('checkbox', { name: 'Dizel' }),
    ).toBeChecked()
    await expect(
      yeni.getByTestId('transmissions-filter').getByRole('checkbox', { name: 'Otomatik' }),
    ).toBeChecked()
    await expect(yeni.getByTestId('year-summary')).toHaveText('2016–2024')
    await expect(yeni.getByTestId('sort-select')).toHaveValue('price-asc')
    await expect(yeni.getByTestId('result-count')).toHaveText(ilk ?? '')
    expect(await yeni.getByTestId('listing-card').allTextContents()).toEqual(kartlar)
  })

  test("0015/AC-29: 0001'in biçimindeki /tr/cars?bodyType=suv&page=2 aynı listeyi açmaya devam eder", async ({
    page,
  }) => {
    await page.goto('/tr/cars?bodyType=suv&page=2')

    await expect(page.getByRole('checkbox', { name: 'SUV' })).toBeChecked()
    expect(new URL(page.url()).search).toBe('?bodyType=suv&page=2')
    await expect(page.getByTestId('listing-card').first()).toBeVisible()
  })

  test('0015/AC-31: bir daraltmadan sonra geri demek önceki daraltma hâlini ve listesini geri getirir', async ({
    page,
  }) => {
    await page.goto('/tr/cars?fuel=diesel')
    await expect(page.getByTestId('result-count')).toBeVisible()
    const dizel = await page.getByTestId('result-count').textContent()

    await page
      .getByTestId('transmissions-filter')
      .getByRole('checkbox', { name: 'Otomatik' })
      .check()
    await expect.poll(() => new URL(page.url()).search).toBe('?fuel=diesel&transmission=automatic')

    await page.goBack()

    await expect.poll(() => new URL(page.url()).search).toBe('?fuel=diesel')
    await expect(
      page.getByTestId('transmissions-filter').getByRole('checkbox', { name: 'Otomatik' }),
    ).not.toBeChecked()
    await expect(page.getByTestId('result-count')).toHaveText(dizel ?? '')
  })

  test('0015/AC-32: aralık alanına yazarken adres değişmez; Enter ile uygulanır ve geçmişe tek kayıt ekler', async ({
    page,
  }) => {
    await page.goto('/tr/cars?fuel=diesel')
    await expect(page.getByTestId('result-count')).toBeVisible()
    const uzunluk = await page.evaluate(() => window.history.length)

    const yil = page.getByTestId('year-filter')
    await yil.getByLabel('En az').fill('2018')
    await yil.getByLabel('En çok').fill('2021')
    expect(new URL(page.url()).search).toBe('?fuel=diesel')

    await yil.getByLabel('En çok').press('Enter')

    await expect
      .poll(() => new URL(page.url()).search)
      .toBe('?fuel=diesel&yearMin=2018&yearMax=2021')
    expect(await page.evaluate(() => window.history.length)).toBe(uzunluk + 1)

    // QA BULGU-1 — adresi değiştirmeyen ikinci gönderim geçmişe kayıt EKLEMEZ.
    await yil.getByRole('button', { name: 'Uygula' }).click()
    expect(await page.evaluate(() => window.history.length)).toBe(uzunluk + 1)

    // Tek "geri" önceki daraltma hâline döner (AC-31).
    await page.goBack()
    await expect.poll(() => new URL(page.url()).search).toBe('?fuel=diesel')
  })

  test('0015/AC-14, AC-15, AC-16: geçersiz aralık ve tanınmayan sort adresten kalkar; tanınmayan slug kalır', async ({
    page,
  }) => {
    await page.goto('/tr/cars?fuel=kerosene&yearMin=2021&yearMax=2018&sort=cheapest')
    await expect(page.getByTestId('result-count')).toBeVisible()

    await expect.poll(() => new URL(page.url()).search).toBe('?fuel=kerosene')
    await expect(page.getByTestId('sort-select')).toHaveValue('default')
  })

  test('0015/AC-40: /ar/cars sayfasında eksenler Arapça ve panel sağdan sola akar', async ({
    page,
  }) => {
    await page.goto('/ar/cars')

    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    const yakit = page.getByTestId('fuels-filter')
    await expect(yakit.locator('legend')).toHaveText('الوقود')
    await expect(yakit.getByRole('checkbox', { name: 'ديزل' })).toBeVisible()

    // Aynalanma: eksen başlığının METNİ satırın başında, yani sağda durur. Tek sütunda
    // `fieldset` panelin tamamını kaplar; kutuyu ölçmek hizayı ölçmez (0014 AC-57 dersi).
    const { metinSol, metinSag, kutuSol, kutuSag } = await yakit
      .locator('legend')
      .evaluate((legend) => {
        const aralik = document.createRange()
        aralik.selectNodeContents(legend)
        const metin = aralik.getBoundingClientRect()
        const kutu = legend.closest('fieldset')!.getBoundingClientRect()
        return {
          metinSol: metin.left,
          metinSag: metin.right,
          kutuSol: kutu.left,
          kutuSag: kutu.right,
        }
      })
    expect(kutuSag - metinSag).toBeLessThan(metinSol - kutuSol)
  })
})
