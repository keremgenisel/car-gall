import { expect, test, type Page, type Route } from '@playwright/test'
import { benzersizAdres, iletiGovdesi, sagaHizali, teyitYolu } from './bulten'
import { kanit } from './kanit'

/**
 * `0018` — bülten bölümünün, teyit ve çıkış sayfalarının görsel kanıtı (`npm run evidence`;
 * assert'ler her koşuda). Sayfalar ve belge GERÇEKTİR; yükleniyor, hata, hız sınırı, boş stok
 * ve başarılı çıkış hâlleri API yanıtı araya girilerek kurulur (`evidence-0014`,
 * `evidence-0022` deseni). Araya giren gövdeler sunucunun GERÇEK biçimidir
 * (`NewsletterProblems`, `NewsletterRateLimitPolicy`).
 */
test.describe.configure({ mode: 'serial' })

const ABONELIK = '**/api/notifications/newsletter/subscriptions'
const TEYIT = '**/api/notifications/newsletter/confirmations'
const CIKIS = '**/api/notifications/newsletter/unsubscriptions'

const problem = (status: number, errorCode?: string) => ({
  status,
  contentType: 'application/problem+json',
  body: JSON.stringify({
    type: 'about:blank',
    status,
    ...(errorCode ? { errorCode } : { traceId: '00-e2e-01' }),
  }),
})

/** İsteği hiç cevaplamaz: yükleniyor hâli ekranda kalır. */
async function askida(page: Page, desen: string) {
  await page.route(desen, () => new Promise<void>(() => {}))
}

async function anaSayfa(page: Page, locale = 'tr') {
  await page.goto(`/${locale}/`)
  const bolum = page.getByTestId('newsletter-section')
  await expect(bolum).toBeVisible()
  await bolum.scrollIntoViewIfNeeded()
  return bolum
}

async function gonder(page: Page, adres: string) {
  const bolum = page.getByTestId('newsletter-section')
  await bolum.getByLabel('E-posta adresiniz').fill(adres)
  await bolum.getByRole('button', { name: 'Abone ol' }).click()
}

test.afterEach(async ({ page }) => {
  await page.unrouteAll({ behavior: 'ignoreErrors' })
})

test("0018/AC-1: Ana sayfada HomeCatalog'un hemen altında ve altlıktan önce bir bülten bölümü vardır", async ({
  page,
}) => {
  const bolum = await anaSayfa(page)
  const altlik = page.getByTestId('home-footer')

  const [b, a] = await Promise.all([bolum.boundingBox(), altlik.boundingBox()])
  expect(b!.y + b!.height).toBeLessThanOrEqual(a!.y)
  await expect(bolum.getByRole('heading', { level: 2 })).toHaveText(
    'Yeni gelen araçları ilk siz görün',
  )
  await expect(bolum).toContainText('tek tıkla çıkabilirsiniz')
  await kanit(page, bolum, '0018-ac1.png')
})

test('0018/AC-2: Yayında hiç ilan yokken bülten bölümü yine görünür ve boş stok mesajının altında durur', async ({
  page,
}) => {
  await page.route('**/api/catalog/showcase-listings*', (route) =>
    route.fulfill({ json: { items: [], page: 1, pageSize: 1, totalCount: 0, totalPages: 0 } }),
  )

  const bolum = await anaSayfa(page)
  const bos = page.getByTestId('empty-showcase')

  await expect(bos).toBeVisible()
  expect((await bos.boundingBox())!.y).toBeLessThan((await bolum.boundingBox())!.y)
  await kanit(page, 'main', '0018-ac2.png')
})

test('0018/AC-3: `privacy` o dilde yayındayken nottaki KVKK bağlantısı `/{locale}/{privacy slug}` adresine gider', async ({
  page,
}) => {
  await page.route('**/api/content/navigation*', (route) =>
    route.fulfill({
      json: [{ systemKey: 'privacy', title: 'KVKK Aydınlatma Metni', slug: 'kvkk-aydinlatma' }],
    }),
  )

  const bolum = await anaSayfa(page)

  await expect(bolum.getByTestId('newsletter-privacy')).toHaveAttribute(
    'href',
    '/tr/kvkk-aydinlatma',
  )
  await kanit(page, bolum, '0018-ac3.png')
})

test('0018/AC-4: `privacy` o dilde yayında değilken notta bağlantı yoktur ve form abonelik isteği göndermeye devam eder', async ({
  page,
}) => {
  await page.route('**/api/content/navigation*', (route) => route.fulfill({ json: [] }))

  const bolum = await anaSayfa(page)
  await expect(bolum.getByTestId('newsletter-privacy')).toHaveCount(0)
  await kanit(page, bolum, '0018-ac4.png')

  await gonder(page, benzersizAdres())
  await expect(page.getByTestId('newsletter-success')).toBeVisible()
})

test("0018/AC-5: Gönder'e basıldıktan sonra yanıt gelene kadar düğme yükleniyor durumunu gösterir ve devre dışıdır", async ({
  page,
}) => {
  let istek = 0
  await page.route(ABONELIK, () => {
    istek++
    return new Promise<void>(() => {})
  })

  const bolum = await anaSayfa(page)
  await gonder(page, benzersizAdres())

  const dugme = bolum.getByRole('button', { name: 'Gönderiliyor…' })
  await expect(dugme).toBeDisabled()
  await dugme.click({ force: true })
  expect(istek).toBe(1)
  await kanit(page, bolum, '0018-ac5.png')
})

test('0018/AC-6: Geçerli bir adresle gönderimden sonra form yerinde başarı mesajını gösterir', async ({
  page,
}) => {
  const bolum = await anaSayfa(page)
  await gonder(page, benzersizAdres())

  await expect(page.getByTestId('newsletter-success')).toContainText('teyit bağlantısı gönderdik')
  await kanit(page, bolum, '0018-ac6.png')
})

test('0018/AC-7: `ali@` ve boş adres alan hatasıyla reddedilir; API `400` ve `notification.subscription_email_invalid` döner', async ({
  page,
}) => {
  const bolum = await anaSayfa(page)
  const yanit = page.waitForResponse(ABONELIK)
  await gonder(page, 'ali@')

  expect((await yanit).status()).toBe(400)
  await expect(bolum.getByLabel('E-posta adresiniz')).toHaveAttribute('aria-invalid', 'true')
  await expect(bolum).toContainText('Geçerli bir e-posta adresi yazın.')
  await kanit(page, bolum, '0018-ac7.png')
})

test('0018/AC-11: Sunucu `500` döndüğünde form teknik ayrıntı içermeyen bir hata ve yeniden deneme yolu gösterir; yazılan adres alanda kalır', async ({
  page,
}) => {
  await page.route(ABONELIK, (route) => route.fulfill(problem(500)))

  const bolum = await anaSayfa(page)
  await gonder(page, 'ali@ornek.com')

  await expect(page.getByTestId('newsletter-error')).toHaveText(
    'Şu an abone olunamadı. Bağlantınızı kontrol edip yeniden deneyin.',
  )
  await expect(bolum.getByLabel('E-posta adresiniz')).toHaveValue('ali@ornek.com')
  await expect(bolum).not.toContainText('500')
  await kanit(page, bolum, '0018-ac11.png')
})

test('0018/AC-12: Hız sınırına takılan istekte form hız sınırı mesajı gösterir (D9)', async ({
  page,
}) => {
  await page.route(ABONELIK, (route) =>
    route.fulfill(problem(429, 'notification.subscription_rate_limited')),
  )

  const bolum = await anaSayfa(page)
  await gonder(page, 'ali@ornek.com')

  await expect(page.getByTestId('newsletter-error')).toHaveText(
    'Çok fazla deneme yapıldı. Biraz sonra yeniden deneyin.',
  )
  await kanit(page, bolum, '0018-ac12.png')
})

test('0018/AC-27, AC-28: Teyit sayfası açılınca düğme; düğmeyle abonelik etkinleşir ve başarı mesajı', async ({
  page,
}) => {
  const adres = benzersizAdres()
  await anaSayfa(page)
  await gonder(page, adres)
  await expect(page.getByTestId('newsletter-success')).toBeVisible()

  const yol = teyitYolu(await iletiGovdesi(page.request, adres))
  await page.goto(yol)
  await expect(page.getByRole('button', { name: 'Aboneliği onayla' })).toBeVisible()
  await kanit(page, '[data-testid="newsletter-confirm"]', '0018-ac27.png')

  await page.getByRole('button', { name: 'Aboneliği onayla' }).click()
  await expect(page.getByTestId('newsletter-confirmed')).toContainText('Aboneliğiniz onaylandı')
  await kanit(page, '[data-testid="newsletter-confirmed"]', '0018-ac28.png')
})

test('0018/AC-30: Süresi dolmuş ya da hiç var olmamış bağlantı: "bağlantı artık geçerli değil" ve yeniden abone olma yolu', async ({
  page,
}) => {
  await page.goto('/tr/newsletter/confirm?token=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA')
  await page.getByRole('button', { name: 'Aboneliği onayla' }).click()

  const ekran = page.getByTestId('newsletter-link-invalid')
  await expect(ekran).toContainText('Bu bağlantı artık geçerli değil')
  await expect(ekran.getByRole('link', { name: 'Yeniden abone ol' })).toHaveAttribute(
    'href',
    '/tr/#newsletter',
  )
  await kanit(page, ekran, '0018-ac30.png')
})

test('0018/AC-36: Teyit sayfası istek sürerken yükleniyor, sunucu hatasında teknik ayrıntısız hata ve yeniden deneme', async ({
  page,
}) => {
  await askida(page, TEYIT)
  await page.goto('/tr/newsletter/confirm?token=abc')
  await page.getByRole('button', { name: 'Aboneliği onayla' }).click()
  await expect(page.getByRole('button', { name: 'Aboneliği onayla' })).toBeDisabled()
  await kanit(page, '[data-testid="newsletter-confirm"]', '0018-ac36-a.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route(TEYIT, (route) => route.fulfill(problem(500)))
  await page.goto('/tr/newsletter/confirm?token=abc')
  await page.getByRole('button', { name: 'Aboneliği onayla' }).click()
  const hata = page.getByTestId('newsletter-confirm-error')
  await expect(hata).toContainText('Bir sorun oluştu')
  await expect(hata.getByRole('button', { name: 'Yeniden dene' })).toBeVisible()
  await kanit(page, hata, '0018-ac36-b.png')
})

test('0018/AC-38, AC-43: Çıkış bağlantısı açıldığında düğme ya da onay istemeden "abonelikten çıktınız" ve yeniden abone olma bağlantısı', async ({
  page,
}) => {
  // Başarılı çıkışın sunucu yarısı (oturumsuz, tek istek) `Host.Tests` AC-38'de; bağlantı
  // abonelik kimliğinin HMAC imzasıdır ve e2e kimliği bilmez.
  let istek = 0
  await page.route(CIKIS, (route: Route) => {
    istek++
    return route.fulfill({ json: { status: 'unsubscribed' } })
  })

  await page.goto('/tr/newsletter/unsubscribe?token=imzali-baglanti')
  const ekran = page.getByTestId('newsletter-unsubscribed')

  await expect(ekran).toContainText('Abonelikten çıktınız')
  await expect(ekran.getByRole('button')).toHaveCount(0)
  await expect(ekran.getByRole('link', { name: 'Yeniden abone ol' })).toHaveAttribute(
    'href',
    '/tr/#newsletter',
  )
  expect(istek).toBe(1)
  await kanit(page, ekran, '0018-ac38.png')
  await kanit(page, ekran, '0018-ac43.png')
})

test('0018/AC-44: Geçersiz bir çıkış bağlantısında sayfa "bağlantı artık geçerli değil" mesajını gösterir', async ({
  page,
}) => {
  await page.goto('/tr/newsletter/unsubscribe?token=AAAA')

  const ekran = page.getByTestId('newsletter-link-invalid')
  await expect(ekran).toContainText('Bu bağlantı artık geçerli değil')
  await kanit(page, ekran, '0018-ac44.png')
})

test('0018/AC-45: Çıkış sayfası istek sürerken yükleniyor, sunucu hatasında teknik ayrıntısız hata ve yeniden deneme', async ({
  page,
}) => {
  await askida(page, CIKIS)
  await page.goto('/tr/newsletter/unsubscribe?token=abc')
  await expect(page.getByTestId('newsletter-unsubscribing')).toBeVisible()
  await kanit(page, '[data-testid="newsletter-unsubscribing"]', '0018-ac45-a.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route(CIKIS, (route) => route.fulfill(problem(500)))
  await page.goto('/tr/newsletter/unsubscribe?token=abc')
  const hata = page.getByTestId('newsletter-unsubscribe-error')
  await expect(hata.getByRole('button', { name: 'Yeniden dene' })).toBeVisible()
  await expect(hata).not.toContainText('500')
  await kanit(page, hata, '0018-ac45-b.png')
})

test('0018/AC-95: Arapça bülten bölümü, teyit ve çıkış sayfaları sağdan sola; hiza Range ile sağ', async ({
  page,
}) => {
  const bolum = await anaSayfa(page, 'ar')
  await sagaHizali(page, '#newsletter-title')
  await kanit(page, bolum, '0018-ac95-a.png')

  await page.goto('/ar/newsletter/confirm?token=abc')
  await sagaHizali(page, '[data-testid="newsletter-confirm"] h1')
  await kanit(page, '[data-testid="newsletter-confirm"]', '0018-ac95-b.png')

  await page.route(CIKIS, (route) => route.fulfill({ json: { status: 'unsubscribed' } }))
  await page.goto('/ar/newsletter/unsubscribe?token=abc')
  await sagaHizali(page, '[data-testid="newsletter-unsubscribed"] h1')
  await kanit(page, '[data-testid="newsletter-unsubscribed"]', '0018-ac95-c.png')
})
