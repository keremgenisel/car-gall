import { readFileSync } from 'node:fs'
import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0022` — ana sayfa metinleri, gerçek Host'a karşı: panelin ekranı, sunucunun belgeye bastığı
 * metnin vitrinde görünmesi, ilk boyama ve düzen kayması. Görsel kanıt `npm run evidence` ile
 * yazılır (docs/testing.md § 3); assert'ler her koşuda çalışır.
 *
 * <b>Paylaşılan durum.</b> Ana sayfa metinleri GLOBALDİR ve `ana-sayfa.spec.ts` ile
 * `evidence-0014.spec.ts` varsayılan metinleri bekliyor. Bu dosya her testin önünde ve
 * sonunda bütün alanları boşaltır: kurulum "hiç kayıt yok" hâline döner (C2).
 *
 * <b>Yetkisiz rol, boş stok, yükleniyor/hata hâlleri</b> API yanıtı araya girilerek kurulur
 * (`evidence-0014`, `evidence-0020` deseni); ekranın ve belgenin kendisi gerçektir — metin
 * her zaman sunucunun bastığı etiketten gelir.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const UC = '/api/admin/content/home'
const ALANLAR = [
  'introTitle',
  'introBody',
  'formTitle',
  'featuredTitle',
  'emptyTitle',
  'emptyBody',
] as const
type Alan = (typeof ALANLAR)[number]

/** Vitrin kataloğunun varsayılanları (C2): beklenen metin çeviri kataloğundan okunur. */
function katalog(locale: string): Record<string, string> {
  return JSON.parse(readFileSync(`src/shared/i18n/locales/${locale}.json`, 'utf8')) as Record<
    string,
    string
  >
}

const VARSAYILAN = { tr: katalog('tr'), ar: katalog('ar'), en: katalog('en') }

interface Metinler {
  readonly version: number
  readonly locales: { readonly locale: string; readonly values: Record<Alan, string> }[]
}

async function oku(api: APIRequestContext): Promise<Metinler> {
  const yanit = await api.get(UC)
  expect(yanit.ok(), `GET ${UC}`).toBe(true)
  return (await yanit.json()) as Metinler
}

/** Verilen dillerin verilen alanlarını yazar; diğer alanlar ve diller olduğu gibi kalır. */
async function yaz(
  api: APIRequestContext,
  degerler: Record<string, Partial<Record<Alan, string>>>,
) {
  const simdiki = await oku(api)
  const yanit = await api.put(UC, {
    data: {
      version: simdiki.version,
      locales: simdiki.locales.map(({ locale, values }) => ({
        locale,
        ...values,
        ...(degerler[locale] ?? {}),
      })),
    },
  })
  expect(yanit.ok(), `PUT ${UC} → ${yanit.status()} ${await yanit.text()}`).toBe(true)
}

/** Bütün alanları boşaltır: kurulum "hiç kayıt yok" hâline döner. */
async function sifirla(api: APIRequestContext) {
  const bos = Object.fromEntries(ALANLAR.map((alan) => [alan, ''])) as Record<Alan, string>
  await yaz(api, { tr: bos, ar: bos, en: bos })
}

/** Vitrinde yayında hiç ilan yokmuş gibi: toplam ve liste boş (`evidence-0014` deseni). */
async function stokBos(page: Page) {
  await page.route('**/api/catalog/showcase-listings*', (route) =>
    route.fulfill({ json: { items: [], page: 1, pageSize: 1, totalCount: 0, totalPages: 0 } }),
  )
}

/** `/me` yanıtını verilen rolle döndürür (`evidence-0020` deseni). */
async function rol(page: Page, role: string) {
  await page.route('**/api/admin/me', async (route) => {
    if (route.request().method() !== 'GET') {
      return route.fallback()
    }

    const yanit = await route.fetch()
    return route.fulfill({ response: yanit, json: { ...(await yanit.json()), role } })
  })
}

/** Panel belgesini Arapça basılmış gibi döndürür (`evidence-0020` deseni). */
async function arapcaPanel(page: Page) {
  await page.route('**/admin/**', async (route) => {
    if (route.request().resourceType() !== 'document') {
      return route.fallback()
    }

    const yanit = await route.fetch()
    const html = (await yanit.text()).replace(
      /<html lang="[a-z]+" dir="(ltr|rtl)"/,
      '<html lang="ar" dir="rtl"',
    )
    return route.fulfill({ response: yanit, body: html })
  })
}

/** Bir metin kutusunun kaç satıra yayıldığı: metnin satır kutularının farklı üst kenarları. */
async function satirSayisi(page: Page, secici: string): Promise<number> {
  return page.locator(secici).evaluate((element) => {
    const range = document.createRange()
    range.selectNodeContents(element)
    return new Set([...range.getClientRects()].map((kutu) => Math.round(kutu.top))).size
  })
}

function alan(page: Page, locale: string, field: Alan) {
  return page.locator(`#home-texts-${locale}-${field}`)
}

test.beforeEach(async ({ page }) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  await sifirla(page.request)
})

test.afterEach(async ({ page }) => {
  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await sifirla(page.request)
})

// ----- Ekran ve kaydetme -----

test('0022/AC-1, AC-35: İçerik bölümünün "Ana sayfa" girdisi ekranı açar; altı alan her dilde', async ({
  page,
}) => {
  await rol(page, 'content_editor')
  await page.goto('/admin/content')
  const girdi = page.getByTestId('content-home-link')
  await expect(girdi).toHaveText('Ana sayfa')
  await kanit(page, '[data-testid="content-admin"]', '0022-ac1-a.png')

  await girdi.click()
  await expect(page).toHaveURL(/\/admin\/content\/home$/)
  for (const locale of ['tr', 'ar', 'en']) {
    for (const field of ALANLAR) {
      await expect(alan(page, locale, field)).toBeVisible()
    }
  }
  await expect(page.getByLabel('Giriş başlığı').first()).toBeVisible()
  await kanit(page, '[data-testid="home-texts"]', '0022-ac1-b.png')
})

test('0022/AC-2: Boş bir alanın yerinde o dilin varsayılan metni ipucu olarak görünür.', async ({
  page,
}) => {
  await page.goto('/admin/content/home')

  for (const locale of ['tr', 'ar', 'en'] as const) {
    await expect(alan(page, locale, 'introTitle')).toHaveAttribute(
      'placeholder',
      VARSAYILAN[locale]['home.intro.title']!,
    )
    await expect(alan(page, locale, 'emptyBody')).toHaveAttribute(
      'placeholder',
      VARSAYILAN[locale]['showcase.empty.showcase.body']!,
    )
  }
  await kanit(page, '[data-testid="home-texts-tr"]', '0022-ac2.png')
  await kanit(page, '[data-testid="home-texts-ar"]', '0022-ac2-ar.png')
})

test('0022/AC-3, AC-27: Yönetici kaydeder; hemen sonraki ilk Türkçe ana sayfa isteği yeni başlığı gösterir', async ({
  page,
}) => {
  await page.goto('/admin/content/home')
  await alan(page, 'tr', 'introTitle').fill("Kadıköy'ün güvenilir galerisi")
  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-saved')).toBeVisible()
  await kanit(page, '[data-testid="home-texts"]', '0022-ac27.png')

  const yanit = await page.goto('/tr/')
  expect(yanit?.headers()['cache-control']).toBe('no-store')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText("Kadıköy'ün güvenilir galerisi")
  await kanit(page, '[data-testid="home-intro"]', '0022-ac3.png')
})

test('0022/AC-4: Türkçe giriş başlığı kaydedildiğinde İngilizce ve Arapça ana sayfa kendi varsayılan başlıklarını göstermeye devam eder.', async ({
  page,
}) => {
  await yaz(page.request, { tr: { introTitle: "Kadıköy'ün güvenilir galerisi" } })

  await page.goto('/en/')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    VARSAYILAN.en['home.intro.title']!,
  )
  await kanit(page, '[data-testid="home-intro"]', '0022-ac4-en.png')

  await page.goto('/ar/')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    VARSAYILAN.ar['home.intro.title']!,
  )
  await kanit(page, '[data-testid="home-intro"]', '0022-ac4-ar.png')
})

test('0022/AC-5: Kaydedilmiş bir alan boşaltılıp kaydedildiğinde ana sayfa o dilde yeniden varsayılan metni gösterir.', async ({
  page,
}) => {
  await yaz(page.request, { tr: { introTitle: 'Geçici başlık' } })
  await page.goto('/admin/content/home')
  await expect(alan(page, 'tr', 'introTitle')).toHaveValue('Geçici başlık')
  await alan(page, 'tr', 'introTitle').fill('')
  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-saved')).toBeVisible()

  await page.goto('/tr/')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    VARSAYILAN.tr['home.intro.title']!,
  )
  await kanit(page, '[data-testid="home-intro"]', '0022-ac5.png')
})

test('0022/AC-6: Yalnız boşluktan oluşan bir giriş başlığı (`"   "`) boş sayılır ve ana sayfa varsayılan başlığı gösterir (D14).', async ({
  page,
}) => {
  await yaz(page.request, { tr: { introTitle: '   ' } })

  await page.goto('/tr/')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    VARSAYILAN.tr['home.intro.title']!,
  )
  await kanit(page, '[data-testid="home-intro"]', '0022-ac6.png')
})

test('0022/AC-7: Hiç kayıt yapılmamış bir kurulumda ana sayfanın altı metni bugünkü varsayılan metinlerle aynıdır.', async ({
  page,
}) => {
  await page.goto('/tr/')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(
    VARSAYILAN.tr['home.intro.title']!,
  )
  await expect(page.locator('.home-hero__body')).toHaveText(VARSAYILAN.tr['home.intro.body']!)
  await expect(page.locator('.narrowing-form__title')).toHaveText(VARSAYILAN.tr['home.form.title']!)
  await expect(page.locator('#featured-title')).toHaveText(VARSAYILAN.tr['home.featured.title']!)
  await expect(page.locator('meta[name^="home-text-"]')).toHaveCount(0)
  await kanit(page, 'main', '0022-ac7-a.png')

  await stokBos(page)
  await page.goto('/tr/')
  const bos = page.getByTestId('empty-showcase')
  await expect(bos.getByRole('heading')).toHaveText(VARSAYILAN.tr['showcase.empty.showcase.title']!)
  await expect(bos.locator('p')).toHaveText(VARSAYILAN.tr['showcase.empty.showcase.body']!)
  await kanit(page, '[data-testid="empty-showcase"]', '0022-ac7-b.png')
})

test('0022/AC-8: Yayında ilan ve öne çıkan ilan varken, "Aracınızı bulun" ve "Öne çıkan araçlar" başlıkları kaydedildikten sonra ana sayfada kaydedilen metinle görünür (D18).', async ({
  page,
}) => {
  await yaz(page.request, {
    tr: { formTitle: 'Size uygun aracı seçin', featuredTitle: 'Haftanın seçkisi' },
  })

  await page.goto('/tr/')
  await expect(page.getByTestId('featured-section')).toBeVisible()
  await expect(page.locator('.narrowing-form__title')).toHaveText('Size uygun aracı seçin')
  await expect(page.locator('#featured-title')).toHaveText('Haftanın seçkisi')
  await kanit(page, 'main', '0022-ac8.png')
})

// ----- Boş stok mesajı -----

test('0022/AC-9, AC-11: Stok boşken ana sayfa kaydedilmiş başlığı ve açıklamayı gösterir; bağlantı metni değişmez', async ({
  page,
}) => {
  await yaz(page.request, {
    tr: { emptyTitle: 'Yeni araçlar yolda', emptyBody: 'Bu hafta yeni araçlar geliyor.' },
  })
  await stokBos(page)

  await page.goto('/tr/')
  const bos = page.getByTestId('empty-showcase')
  await expect(bos.getByRole('heading')).toHaveText('Yeni araçlar yolda')
  await expect(bos.locator('p')).toHaveText('Bu hafta yeni araçlar geliyor.')
  await expect(bos.getByRole('link')).toHaveText('Galeriye ulaşın')
  await kanit(page, '[data-testid="empty-showcase"]', '0022-ac9.png')
  await kanit(page, '[data-testid="empty-showcase"]', '0022-ac11.png')
})

test('0022/AC-10: Yayında hiç ilan yokken vitrin listesi aynı kaydedilmiş başlığı ve açıklamayı gösterir.', async ({
  page,
}) => {
  await yaz(page.request, {
    tr: { emptyTitle: 'Yeni araçlar yolda', emptyBody: 'Bu hafta yeni araçlar geliyor.' },
  })
  await stokBos(page)

  await page.goto('/tr/cars')
  const bos = page.getByTestId('empty-showcase')
  await expect(bos.getByRole('heading')).toHaveText('Yeni araçlar yolda')
  await expect(bos.locator('p')).toHaveText('Bu hafta yeni araçlar geliyor.')
  await kanit(page, '[data-testid="empty-showcase"]', '0022-ac10.png')
})

// ----- Düz metin ve sınırlar -----

test('0022/AC-12: `<b>Kalın</b> & "tırnak"` olarak kaydedilen giriş başlığı ana sayfada bu karakterlerle metin olarak görünür; kalın yazılmaz ve sayfada yeni bir öğe oluşmaz.', async ({
  page,
}) => {
  const metin = '<b>Kalın</b> & "tırnak"'
  await yaz(page.request, { tr: { introTitle: metin } })

  await page.goto('/tr/')
  const baslik = page.getByRole('heading', { level: 1 })
  await expect(baslik).toHaveText(metin)
  expect(await baslik.evaluate((element) => element.childElementCount)).toBe(0)
  await expect(page.locator('b')).toHaveCount(0)
  await kanit(page, '[data-testid="home-intro"]', '0022-ac12.png')
})

test('0022/AC-13: `</script></head><script>alert(1)</script>` içeren açıklama kodlanmış durur; `<head>` bozulmaz, betik çalışmaz', async ({
  page,
}) => {
  const saldiri = '</script></head><script>alert(1)</script>'
  await yaz(page.request, { tr: { introBody: `Önce ${saldiri} sonra`, emptyBody: saldiri } })

  const diyaloglar: string[] = []
  page.on('dialog', async (diyalog) => {
    diyaloglar.push(diyalog.message())
    await diyalog.dismiss()
  })

  const yanit = await page.goto('/tr/')
  const html = (await yanit!.text()) ?? ''
  expect(html).not.toContain(saldiri)
  expect(html).toContain('&lt;/script&gt;&lt;/head&gt;&lt;script&gt;alert(1)&lt;/script&gt;')

  await expect(page.locator('.home-hero__body')).toHaveText(`Önce ${saldiri} sonra`)
  const yapi = await page.evaluate(() => ({
    headMeta: document.head.querySelectorAll('meta[name^="home-text-"]').length,
    bodyMeta: document.body.querySelectorAll('meta').length,
    satirIciBetik: [...document.scripts].filter((betik) => betik.textContent?.includes('alert'))
      .length,
    kok: document.getElementById('root') !== null,
  }))
  expect(yapi).toEqual({ headMeta: 2, bodyMeta: 0, satirIciBetik: 0, kok: true })
  await page.waitForLoadState('networkidle')
  expect(diyaloglar).toEqual([])
  await kanit(page, '[data-testid="home-intro"]', '0022-ac13.png')
})

test('0022/AC-14: Giriş açıklamasına yazılan satır sonu ana sayfada yeni satır olarak görünür.', async ({
  page,
}) => {
  await yaz(page.request, { tr: { introBody: 'Birinci satır\nİkinci satır' } })

  await page.goto('/tr/')
  await expect(page.locator('.home-hero__body')).toHaveText('Birinci satır\nİkinci satır')
  expect(await satirSayisi(page, '.home-hero__body')).toBe(2)
  await kanit(page, '[data-testid="home-intro"]', '0022-ac14.png')
})

test("0022/AC-15: Boş stok açıklamasının satır sonu ana sayfada ve listede yeni satır; `EmptyState`'in diğer kullanımları değişmez", async ({
  page,
}) => {
  await yaz(page.request, { tr: { emptyBody: 'Satır bir\nSatır iki' } })
  await stokBos(page)

  for (const [adres, ek] of [
    ['/tr/', 'a'],
    ['/tr/cars', 'b'],
  ] as const) {
    await page.goto(adres)
    await expect(page.getByTestId('empty-showcase')).toBeVisible()
    expect(await satirSayisi(page, '[data-testid="empty-showcase"] p')).toBe(2)
    await kanit(page, '[data-testid="empty-showcase"]', `0022-ac15-${ek}.png`)
  }

  // Başka bir `EmptyState` kullanımı: daraltma sonucu boş. Satır sonu stili ona uygulanmaz.
  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route('**/api/catalog/showcase-listings*', (route) => {
    const url = new URL(route.request().url())
    return url.searchParams.has('bodyType')
      ? route.fulfill({ json: { items: [], page: 1, pageSize: 24, totalCount: 0, totalPages: 0 } })
      : route.fallback()
  })
  await page.goto('/tr/cars?bodyType=suv')
  const filtre = page.getByTestId('empty-by-filter').locator('p')
  await expect(filtre).toBeVisible()
  expect(await filtre.evaluate((element) => getComputedStyle(element).whiteSpace)).toBe('normal')
  await kanit(page, '[data-testid="empty-by-filter"]', '0022-ac15-c.png')
})

test('0022/AC-16: 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.', async ({
  page,
}) => {
  await yaz(page.request, { tr: { introTitle: 'Önceki başlık' } })
  await page.goto('/admin/content/home')

  await alan(page, 'tr', 'introTitle').fill('a'.repeat(121))
  await alan(page, 'tr', 'introBody').fill('b'.repeat(501))
  await page.getByTestId('home-texts-save').click()

  await expect(page.getByTestId('home-texts-tr-introTitle-error')).toHaveText(
    'Başlık en fazla 120 karakter olabilir.',
  )
  await expect(page.getByTestId('home-texts-tr-introBody-error')).toHaveText(
    'Açıklama en fazla 500 karakter olabilir.',
  )
  await kanit(page, '[data-testid="home-texts-tr"]', '0022-ac16.png')

  const kayitli = await oku(page.request)
  expect(kayitli.locales.find((item) => item.locale === 'tr')!.values.introTitle).toBe(
    'Önceki başlık',
  )
})

test('0022/AC-17: 120 karakterlik başlık ve 500 karakterlik açıklama kabul edilir.', async ({
  page,
}) => {
  await page.goto('/admin/content/home')
  await alan(page, 'tr', 'introTitle').fill('a'.repeat(120))
  await alan(page, 'tr', 'introBody').fill('b'.repeat(500))
  await expect(page.getByTestId('home-texts-tr-introTitle-counter')).toHaveText(
    '120 / 120 karakter',
  )
  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-saved')).toBeVisible()
  await kanit(page, '[data-testid="home-texts-tr"]', '0022-ac17.png')

  const kayitli = await oku(page.request)
  expect(kayitli.locales.find((item) => item.locale === 'tr')!.values.introBody).toHaveLength(500)
})

test('0022/AC-18: Arapça harf ve emoji içeren, tam 120 kod noktalı bir başlık kabul edilir; 121 kod noktalı olanı reddedilir; panelin sayacı iki durumda da sunucuyla aynı sayıyı gösterir (D15).', async ({
  page,
}) => {
  const parca = ['س', 'ي', 'ا', 'ر', 'ة', ' ', '🚗', '✨', 'م']
  const metin = (n: number) => Array.from({ length: n }, (_, i) => parca[i % parca.length]).join('')

  await page.goto('/admin/content/home')
  await alan(page, 'ar', 'introTitle').fill(metin(120))
  await expect(page.getByTestId('home-texts-ar-introTitle-counter')).toHaveText(
    '120 / 120 karakter',
  )
  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-saved')).toBeVisible()
  await kanit(page, '[data-testid="home-texts-ar"]', '0022-ac18-a.png')

  await alan(page, 'ar', 'introTitle').fill(metin(121))
  await expect(page.getByTestId('home-texts-ar-introTitle-counter')).toHaveText(
    '121 / 120 karakter',
  )
  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-ar-introTitle-error')).toBeVisible()
  await kanit(page, '[data-testid="home-texts-ar"]', '0022-ac18-b.png')

  const kayitli = await oku(page.request)
  expect(kayitli.locales.find((item) => item.locale === 'ar')!.values.introTitle).toBe(metin(120))
})

// ----- İlk boyama -----

/** Gezinme başından itibaren seçicinin aldığı HER metin değerini kaydeder (D13). */
async function metinGecmisi(page: Page, secici: string) {
  await page.addInitScript((hedef) => {
    const gecmis: string[] = []
    ;(window as unknown as { metinGecmisi: string[] }).metinGecmisi = gecmis
    const kaydet = () => {
      const element = document.querySelector(hedef)
      const deger = element?.textContent ?? null
      if (deger !== null && gecmis[gecmis.length - 1] !== deger) {
        gecmis.push(deger)
      }
    }
    new MutationObserver(kaydet).observe(document, {
      subtree: true,
      childList: true,
      characterData: true,
    })
  }, secici)
}

test("0022/AC-19: Kaydedilmiş bir giriş başlığı olan ana sayfada, gezinme başından itibaren `h1`'in aldığı her metin değeri kaydedilmiş başlığa eşittir; varsayılan başlık hiçbir an görünmez (D13).", async ({
  page,
}) => {
  await yaz(page.request, { tr: { introTitle: 'Kaydedilmiş başlık' } })
  await metinGecmisi(page, 'h1')

  await page.goto('/tr/')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Kaydedilmiş başlık')
  await page.waitForLoadState('networkidle')

  const gecmis = await page.evaluate(
    () => (window as unknown as { metinGecmisi: string[] }).metinGecmisi,
  )
  expect(gecmis).toEqual(['Kaydedilmiş başlık'])
  await kanit(page, '[data-testid="home-intro"]', '0022-ac19.png')
})

test('0022/AC-20: Aynı ölçüm, yayında ilan yokken boş stok başlığı için de geçerlidir (D13).', async ({
  page,
}) => {
  await yaz(page.request, { tr: { emptyTitle: 'Yeni araçlar yolda' } })
  await stokBos(page)
  await metinGecmisi(page, '[data-testid="empty-showcase"] h2')

  await page.goto('/tr/')
  await expect(page.getByTestId('empty-showcase').getByRole('heading')).toHaveText(
    'Yeni araçlar yolda',
  )
  await page.waitForLoadState('networkidle')

  const gecmis = await page.evaluate(
    () => (window as unknown as { metinGecmisi: string[] }).metinGecmisi,
  )
  expect(gecmis).toEqual(['Yeni araçlar yolda'])
  await kanit(page, '[data-testid="empty-showcase"]', '0022-ac20.png')
})

test.describe('mobil görünüm', () => {
  test.use({
    viewport: { width: 412, height: 915 },
    deviceScaleFactor: 2.625,
    isMobile: true,
    hasTouch: true,
  })

  test('0022/AC-22: En uzun form başlığıyla mobil görünümde form iskeletinden gerçek forma geçiş sayfayı kaydırmaz; CLS < 0,1 (D12).', async ({
    page,
  }) => {
    // D11'in tohumuyla aynı en kötü hâl: her alan sınırında, açıklamalar satır sonlu.
    const baslik = 'Geçmişi açık, bakımı kayıtlı ve fiyatı net araçlar galerimizde sizi bekliyor. '
      .repeat(2)
      .slice(0, 119)
      .concat('.')
    const aciklama = 'Geçmişi açık, bakımı kayıtlı ve fiyatı net araçlar.\n'
      .repeat(10)
      .slice(0, 499)
      .concat('.')
    expect([...baslik]).toHaveLength(120)
    expect([...aciklama]).toHaveLength(500)
    await yaz(page.request, {
      tr: {
        introTitle: baslik,
        introBody: aciklama,
        formTitle: baslik,
        featuredTitle: baslik,
        emptyTitle: baslik,
        emptyBody: aciklama,
      },
    })

    await page.addInitScript(() => {
      ;(window as unknown as { cls: number }).cls = 0
      new PerformanceObserver((liste) => {
        for (const kayit of liste.getEntries() as unknown as {
          value: number
          hadRecentInput: boolean
        }[]) {
          if (!kayit.hadRecentInput) {
            ;(window as unknown as { cls: number }).cls += kayit.value
          }
        }
      }).observe({ type: 'layout-shift', buffered: true })
    })

    // İskeletin görünür kaldığı bir an: formun okuması bir saniye geciktirilir.
    await page.route('**/api/catalog/showcase-listings*', async (route) => {
      await new Promise((r) => setTimeout(r, 1000))
      await route.fallback()
    })

    await page.goto('/tr/')
    const iskelet = page.getByTestId('narrowing-form-skeleton')
    await expect(iskelet.locator('h2')).toHaveText(baslik)
    const once = await iskelet.boundingBox()
    await kanit(page, '[data-testid="narrowing-form-skeleton"]', '0022-ac22-a.png')

    const form = page.getByTestId('narrowing-form')
    await expect(form.locator('h2')).toHaveText(baslik)
    const sonra = await form.boundingBox()
    await page.waitForLoadState('networkidle')
    await kanit(page, '[data-testid="narrowing-form"]', '0022-ac22-b.png')

    expect(sonra!.y).toBeCloseTo(once!.y, 0)
    const cls = await page.evaluate(() => (window as unknown as { cls: number }).cls)
    expect(cls).toBeLessThan(0.1)
  })
})

test('0022/AC-23: Ana sayfanın HTML yanıtı önbelleklenmeyi engelleyen bir başlık taşır ya da kayıt önbelleği geçersizler (D20).', async ({
  page,
}) => {
  for (const adres of ['/tr/', '/tr/cars']) {
    const yanit = await page.goto(adres)
    expect(yanit?.headers()['cache-control']).toBe('no-store')
  }
})

// ----- Eşzamanlılık -----

test('0022/AC-26: Reddedilen kaydetmede panel uyarı gösterir, güncel hâli yükleme yolu sunar ve editörün yazdıkları ekranda kalır.', async ({
  page,
}) => {
  await page.goto('/admin/content/home')
  await alan(page, 'tr', 'introTitle').fill('Benim başlığım')

  // Ekran açıkken başka bir editör kaydeder: okunan sürüm bayatlar.
  await yaz(page.request, { tr: { introTitle: 'Başkasının başlığı' } })

  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-conflict')).toBeVisible()
  await expect(alan(page, 'tr', 'introTitle')).toHaveValue('Benim başlığım')
  await kanit(page, 'main', '0022-ac26-a.png')

  await page.getByTestId('home-texts-reload').click()
  await expect(alan(page, 'tr', 'introTitle')).toHaveValue('Başkasının başlığı')
  await expect(page.getByTestId('home-texts-conflict')).toHaveCount(0)
  await kanit(page, '[data-testid="home-texts-tr"]', '0022-ac26-b.png')
})

// ----- Erişim -----

test('0022/AC-28: Satış Danışmanı\'nın panel gezinmesinde İçerik ve "Ana sayfa" bağlantısı yoktur; `/admin/content/home` adresini doğrudan açtığında "erişim yok" ekranını görür (D19).', async ({
  page,
}) => {
  await rol(page, 'sales_advisor')
  await page.goto('/admin/content/home')

  await expect(page.getByTestId('admin-no-access')).toBeVisible()
  const gezinme = page.getByRole('navigation')
  await expect(gezinme.getByText('İçerik')).toHaveCount(0)
  await expect(gezinme.getByText('Ana sayfa')).toHaveCount(0)
  await expect(page.getByTestId('home-texts')).toHaveCount(0)
  await kanit(page, '[data-testid="admin-shell"]', '0022-ac28.png')
})

// ----- Durumlar ve dil -----

test('0022/AC-31: Ekran veri gelirken yükleniyor durumunu gösterir.', async ({ page }) => {
  await page.route(`**${UC}`, () => new Promise(() => {}))
  await page.goto('/admin/content/home')
  await expect(page.getByTestId('home-texts-loading')).toBeVisible()
  await kanit(page, 'main', '0022-ac31.png')
})

test('0022/AC-32: Veri okunamadığında teknik ayrıntı göstermeyen bir hata ve yeniden deneme gösterilir.', async ({
  page,
}) => {
  let ilk = true
  await page.route(`**${UC}`, (route) => {
    if (ilk && route.request().method() === 'GET') {
      ilk = false
      return route.fulfill({
        status: 500,
        json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-0022' },
      })
    }
    return route.fallback()
  })

  await page.goto('/admin/content/home')
  const hata = page.getByTestId('home-texts-error')
  await expect(hata).toBeVisible()
  await expect(hata).not.toContainText('trace-e2e-0022')
  await expect(hata).not.toContainText('500')
  await kanit(page, 'main', '0022-ac32.png')

  await hata.getByRole('button', { name: 'Tekrar dene' }).click()
  await expect(page.getByTestId('home-texts')).toBeVisible()
})

test('0022/AC-33: Kaydetme sunucu hatasıyla düştüğünde yazılanlar kaybolmaz.', async ({ page }) => {
  await page.route(`**${UC}`, (route) =>
    route.request().method() === 'PUT'
      ? route.fulfill({
          status: 500,
          json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-0022' },
        })
      : route.fallback(),
  )

  await page.goto('/admin/content/home')
  await alan(page, 'en', 'emptyBody').fill('Line one\nLine two')
  await page.getByTestId('home-texts-save').click()

  await expect(page.getByTestId('home-texts-failed')).toBeVisible()
  await expect(page.getByTestId('home-texts-failed')).not.toContainText('trace-e2e-0022')
  await expect(alan(page, 'en', 'emptyBody')).toHaveValue('Line one\nLine two')
  await kanit(page, 'main', '0022-ac33.png')
})

test('0022/AC-34: Arapça alanlar sağdan sola yazılır; Arapça kaydedilen giriş başlığı Arapça ana sayfada sağdan sola düzende görünür.', async ({
  page,
}) => {
  await arapcaPanel(page)
  await page.goto('/admin/content/home')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  for (const field of ALANLAR) {
    await expect(alan(page, 'ar', field)).toHaveAttribute('dir', 'rtl')
  }
  await alan(page, 'ar', 'introTitle').fill('معرض موثوق في قاضي كوي')
  await page.getByTestId('home-texts-save').click()
  await expect(page.getByTestId('home-texts-saved')).toBeVisible()
  await kanit(page, '[data-testid="home-texts-ar"]', '0022-ac34-a.png')
  await kanit(page, 'main', '0022-ac34-b.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.goto('/ar/')
  const baslik = page.getByRole('heading', { level: 1 })
  await expect(baslik).toHaveText('معرض موثوق في قاضي كوي')
  expect(await baslik.evaluate((element) => getComputedStyle(element).direction)).toBe('rtl')

  // Hiza Range ile ölçülür: blok kutu satırı kaplar ve hizayı göstermez (`0020` AC-12).
  const olcum = await baslik.evaluate((element) => {
    const range = document.createRange()
    range.selectNodeContents(element)
    return {
      metinSag: range.getBoundingClientRect().right,
      kutuSag: element.getBoundingClientRect().right,
    }
  })
  expect(Math.abs(olcum.metinSag - olcum.kutuSag)).toBeLessThan(2)
  await kanit(page, '[data-testid="home-intro"]', '0022-ac34-c.png')
})

test('0022/AC-36: Etkin dil listesinden çıkarılmış bir dilin alanları ekranda gösterilmez.', async ({
  page,
}) => {
  // Etkin diller dağıtım yapılandırmasıdır; e2e Host'u üç dille kalkıyor. Sunucunun iki dilli
  // kurulumda yalnız etkin dilleri döndüğünü `Content.Tests` ölçüyor; burada ekranın yalnız
  // dönen dilleri çizdiği, gerçek yanıttan bir dil çıkarılarak gösterilir.
  await page.route(`**${UC}`, async (route) => {
    if (route.request().method() !== 'GET') {
      return route.fallback()
    }
    const yanit = await route.fetch()
    const govde = (await yanit.json()) as Metinler
    return route.fulfill({
      response: yanit,
      json: { ...govde, locales: govde.locales.filter((item) => item.locale !== 'ar') },
    })
  })

  await page.goto('/admin/content/home')
  await expect(page.getByTestId('home-texts-tr')).toBeVisible()
  await expect(page.getByTestId('home-texts-en')).toBeVisible()
  await expect(page.getByTestId('home-texts-ar')).toHaveCount(0)
  await kanit(page, '[data-testid="home-texts"]', '0022-ac36.png')
})
