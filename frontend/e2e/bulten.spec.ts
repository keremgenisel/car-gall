import { expect, test } from '@playwright/test'
import { benzersizAdres, iletiGovdesi, sagaHizali, teyitYolu } from './bulten'

/**
 * `0018` — bülten aboneliği gerçek Host'a karşı: abone ol → teyit iletisi GERÇEKTEN gider
 * (Mailpit) → bağlantı açılır → düğmeyle teyit. Sayfanın açılması teyit değildir (D6).
 * Çıkışın oturumsuz tek istekte çalıştığı `Host.Tests/BultenZinciriTests`'te; görsel kanıt
 * `evidence-0018.spec.ts`'te.
 */

test('0018/AC-6: Geçerli bir adresle gönderimden sonra form yerinde başarı mesajını gösterir: teyit bağlantısının gönderildiği ve gelmezse istenmeyen e-posta klasörüne bakılması', async ({
  page,
}) => {
  await page.goto('/tr/')
  const bolum = page.getByTestId('newsletter-section')

  await bolum.getByLabel('E-posta adresiniz').fill(benzersizAdres())
  await bolum.getByRole('button', { name: 'Abone ol' }).click()

  await expect(page.getByTestId('newsletter-success')).toContainText('istenmeyen e-posta klasörüne')
  await expect(bolum.getByLabel('E-posta adresiniz')).toHaveCount(0)
})

test('0018/AC-28: Düğmeye basıldığında kayıt etkin olur, teyit zamanı UTC olarak yazılır, bir teyit izin olayı eklenir ve sayfa başarı mesajını gösterir', async ({
  page,
}) => {
  const adres = benzersizAdres()

  await page.goto('/en/')
  await page.getByTestId('newsletter-section').getByLabel('Your email address').fill(adres)
  await page.getByRole('button', { name: 'Subscribe' }).click()
  await expect(page.getByTestId('newsletter-success')).toBeVisible()

  // AC-26 — ileti abonenin dilinde, mutlak bağlantıyla.
  const govde = await iletiGovdesi(page.request, adres)
  const yol = teyitYolu(govde)
  expect(yol).toMatch(/^\/en\/newsletter\/confirm\?token=/)

  // AC-27 — açılış teyit değildir: düğme görünür.
  await page.goto(yol)
  const dugme = page.getByRole('button', { name: 'Confirm subscription' })
  await expect(dugme).toBeVisible()

  await dugme.click()
  await expect(page.getByTestId('newsletter-confirmed')).toContainText(
    'Your subscription is confirmed',
  )

  // AC-32 — aynı bağlantının ikinci kullanımı "geçerli değil".
  await page.goto(yol)
  await page.getByRole('button', { name: 'Confirm subscription' }).click()
  await expect(page.getByTestId('newsletter-link-invalid')).toContainText(
    'This link is no longer valid',
  )
})

test('0018/AC-44: Geçersiz bir çıkış bağlantısı hiçbir kaydı değiştirmez ve sayfa "bağlantı artık geçerli değil" mesajını gösterir', async ({
  page,
}) => {
  await page.goto('/tr/newsletter/unsubscribe?token=AAAA')

  await expect(page.getByTestId('newsletter-link-invalid')).toContainText(
    'Bu bağlantı artık geçerli değil',
  )
  await expect(page.getByRole('link', { name: 'Yeniden abone ol' })).toHaveAttribute(
    'href',
    '/tr/#newsletter',
  )
})

test('0018/AC-51: Teyit bağlantısı `{PublicBaseUrl}/{locale}/newsletter/confirm?token=…`, çıkış bağlantısı `{PublicBaseUrl}/{locale}/newsletter/unsubscribe?token=…` kalıbındadır ve ön yüz iki yolu da ilgili sayfayla açar', async ({
  page,
}) => {
  await page.goto('/tr/newsletter/confirm?token=abc')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Aboneliğinizi onaylayın')

  await page.goto('/tr/newsletter/unsubscribe?token=abc')
  await expect(page.getByTestId('newsletter-link-invalid')).toBeVisible()
})

test('0018/AC-53: Teyit ve çıkış sayfaları `noindex` taşır', async ({ page }) => {
  for (const yol of ['/tr/newsletter/confirm?token=abc', '/ar/newsletter/unsubscribe?token=abc']) {
    const yanit = await page.goto(yol)

    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex')
    expect(yanit?.headers()['referrer-policy']).toBe('no-referrer')
  }

  await page.goto('/tr/')
  await expect(page.locator('meta[name="robots"]')).toHaveCount(0)
})

test('0018/AC-95: Arapça ana sayfadaki bülten bölümü ile Arapça teyit ve çıkış sayfaları sağdan sola düzendedir; metnin hizası Range ölçümüyle sağdır', async ({
  page,
}) => {
  await page.goto('/ar/')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByTestId('newsletter-section')).toBeVisible()
  await sagaHizali(page, '#newsletter-title')
  await sagaHizali(page, '.newsletter__body')

  await page.goto('/ar/newsletter/confirm?token=abc')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('أكّد اشتراكك')
  await sagaHizali(page, '[data-testid="newsletter-confirm"] h1')

  await page.goto('/ar/newsletter/unsubscribe?token=abc')
  await expect(page.getByTestId('newsletter-link-invalid')).toBeVisible()
  await sagaHizali(page, '[data-testid="newsletter-link-invalid"] h1')
})
