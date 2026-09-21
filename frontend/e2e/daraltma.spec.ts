import { expect, test } from '@playwright/test'

test.describe('Kasa tipine göre daraltma', () => {
  test('AC-10: Seçili kasa tipi sayısı ekranda görünür; seçimi temizle eylemi klavyeyle odaklanabilir', async ({ page }) => {
    await page.goto('/tr/')

    await expect(page.getByTestId('selected-count')).toContainText('Seçim yok')

    await page.getByRole('checkbox', { name: 'SUV' }).check()
    await expect(page.getByTestId('selected-count')).toContainText('1')

    await page.getByRole('checkbox', { name: 'Sedan' }).check()
    await expect(page.getByTestId('selected-count')).toContainText('2')

    const temizle = page.getByRole('button', { name: 'Seçimi temizle' })
    await temizle.focus()
    await expect(temizle).toBeFocused()

    await page.keyboard.press('Enter')
    await expect(page.getByTestId('selected-count')).toContainText('Seçim yok')
  })

  test('AC-11: Daraltma çubuğu yalnızca kasa tipi eksenini sunar', async ({ page }) => {
    await page.goto('/tr/')

    await expect(page.getByTestId('body-type-filter')).toBeVisible()
    // Tek eksen: sayfada başka bir filtre grubu yoktur (R8, X3).
    await expect(page.locator('fieldset')).toHaveCount(1)
  })

  test('AC-7: İki kasa tipi seçildiğinde liste, herhangi birine uyan ilanları gösterir', async ({ page }) => {
    await page.goto('/tr/')

    await page.getByRole('checkbox', { name: 'SUV' }).check()
    const suvSayisi = await okunanToplam(page)

    await page.getByRole('checkbox', { name: 'Sedan' }).check()
    const ikisi = await okunanToplam(page)

    expect(ikisi).toBeGreaterThan(suvSayisi)
    expect(new URL(page.url()).searchParams.getAll('bodyType')).toEqual(['suv', 'sedan'])
  })

  test('AC-9: Seçim temizlendiğinde liste daraltma öncesi kümeye döner', async ({ page }) => {
    await page.goto('/tr/')
    const tumu = await okunanToplam(page)

    await page.getByRole('checkbox', { name: 'SUV' }).check()
    expect(await okunanToplam(page)).toBeLessThan(tumu)

    await page.getByRole('button', { name: 'Seçimi temizle' }).click()
    expect(await okunanToplam(page)).toBe(tumu)
  })

  test('AC-12: Kasa tipi alanı public değilken daraltma çubuğu hiç gösterilmez; liste daraltmasız çalışır', async ({ page }) => {
    await page.route('**/api/catalog/field-settings', async (route) => {
      await route.fulfill({
        json: { fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: false, isFilterable: false }] },
      })
    })

    await page.goto('/tr/')

    await expect(page.getByTestId('body-type-filter')).toHaveCount(0)
    await expect(page.locator('.listing-card').first()).toBeVisible()
  })
})

async function okunanToplam(page: import('@playwright/test').Page): Promise<number> {
  const metin = await page.getByTestId('result-count').innerText()
  return Number.parseInt(metin.replace(/\D/g, ''), 10)
}
