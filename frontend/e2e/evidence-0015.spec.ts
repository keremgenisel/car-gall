import { expect, test } from '@playwright/test'
import { kanit } from './kanit'

/** 0015 — görsel kanıt (DoD T2): boş sonuç ve Arapça dâhil. */
test('0015/AC-33, AC-34, AC-35: filtre paneli, özetler (Türkçe)', async ({ page }) => {
  await page.goto(
    '/tr/cars?fuel=diesel&fuel=hybrid&yearMin=2018&yearMax=2021&priceMax=1500000&sort=price-asc',
  )
  await expect(page.getByTestId('fuels-summary')).toHaveText('2 seçili')
  await kanit(page, '[data-testid="list-filters"]', '0015-ac33.png')
  await kanit(page, '.list-toolbar', '0015-ac37.png')
})

test('0015/AC-38: boş sonuç etkin eksenleri adlarıyla söyler', async ({ page }) => {
  await page.goto('/tr/cars?fuel=electric&yearMin=2030')
  await expect(page.getByTestId('empty-by-filter')).toContainText('Yakıt ve Yıl')
  await kanit(page, '[data-testid="empty-by-filter"]', '0015-ac38.png')
})

test('0015/AC-40: Arapça panel', async ({ page }) => {
  await page.goto('/ar/cars?fuel=diesel&yearMin=2018&yearMax=2021&sort=year-desc')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await kanit(page, 'main', '0015-ac40-ar.png')
})
