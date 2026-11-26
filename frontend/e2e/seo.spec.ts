import { expect, test, type Page } from '@playwright/test'
import { kanit } from './kanit'

/**
 * `0004` **D7** · **D8** — gerçek HTTP 404'ün ekran yarısı: sunucu belgeyi 404 ile verir ve uç liste
 * ya da detay değil "bulunamadı" ekranını basar (tuzak 9: durum kodu ile ekran çelişmesin). Belge
 * etiketleri, sitemap ve robots.txt `Host.Tests`'te sunucu yanıtının kendisiyle ölçülür (C13).
 */

async function gorunurIlan(page: Page): Promise<string> {
  const liste = (await (
    await page.request.get('/api/catalog/showcase-listings?locale=tr&page=1&pageSize=1')
  ).json()) as { items: { slug: string }[] }

  return liste.items[0]!.slug
}

async function bulunamadi(page: Page, adres: string) {
  const belge = await page.goto(adres)

  expect(belge?.status(), adres).toBe(404)
  await expect(page.getByTestId('content-not-found')).toBeVisible()
  await expect(page.getByTestId('result-count')).toHaveCount(0)
  await expect(page.getByTestId('lead-form')).toHaveCount(0)
}

test('0004/AC-72: Hiç var olmamış bir slug\'ın `/tr/cars/{slug}` isteği 404 döner. Gövde kabuktur ve uç "bulunamadı" ekranını basar.', async ({
  page,
}) => {
  const belge = await page.goto('/tr/cars/hic-olmamis-ilan-2019-000000')

  expect(belge?.status()).toBe(404)
  await expect(page.getByTestId('detail-not-found')).toBeVisible()
  await kanit(page, 'main', '0004-ac72.png')

  await page.goto('/ar/cars/hic-olmamis-ilan-2019-000000')
  await expect(page.getByTestId('detail-not-found')).toBeVisible()
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await kanit(page, 'main', '0004-ac72-ar.png')
})

test('0004/AC-76: `/tr/foo/bar`, `/tr/cars/{görünür slug}/x`, `/tr/admin`, `/tr/newsletter` ve `/tr/newsletter/foo` istekleri 404 döner ve uç liste ya da detay değil "bulunamadı" ekranını basar (tr ve ar için görsel kanıt).', async ({
  page,
}) => {
  const slug = await gorunurIlan(page)

  for (const adres of [
    '/tr/foo/bar',
    `/tr/cars/${slug}/x`,
    '/tr/admin',
    '/tr/newsletter',
    '/tr/newsletter/foo',
  ]) {
    await bulunamadi(page, adres)
  }

  await bulunamadi(page, '/tr/foo/bar')
  await kanit(page, 'main', '0004-ac76.png')

  await bulunamadi(page, '/ar/foo/bar')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await kanit(page, 'main', '0004-ac76-ar.png')
})
