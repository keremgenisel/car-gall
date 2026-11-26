import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0024` — galerinin sosyal hesapları, gerçek Host'a karşı: panelin "Sosyal hesaplar" bölümü,
 * ana sayfa altlığının "Bizi takip edin" bloğu ve tembel simge parçası. jsdom'un taşımadığı
 * şeyler burada ölçülür: gerçek yerleşim (44×44, RTL sırası, kayma), gerçek ağ isteği (parça
 * kaç kez, hangi sayfada) ve gerçek odak göstergesi.
 *
 * <b>Paylaşılan durum.</b> Sosyal hesaplar GLOBALDİR ve `0014`'ün altlık kanıtları hesapsız
 * altlığı bekler. Bu dosya sırayla, tek profilde koşar ve her testin sonunda kümeyi BOŞALTIR.
 * Gövde `0020`'nin alanlarını okunan hâliyle geri yazar: görünen ad ve unvan değişmez.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const PARCA = /\/assets\/social-icons-[^/]+\.js$/

const UC = {
  instagram: 'https://www.instagram.com/e2e.otogaleri',
  youtube: 'https://www.youtube.com/@e2e-otogaleri',
  linkedin: 'https://www.linkedin.com/company/e2e-otogaleri',
} as const

const ALTI = {
  instagram: 'https://www.instagram.com/e2e.otogaleri',
  facebook: 'https://www.facebook.com/profile.php?id=100012345',
  youtube: 'https://www.youtube.com/@e2e-otogaleri',
  tiktok: 'https://www.tiktok.com/@e2e.otogaleri',
  x: 'https://x.com/e2e_otogaleri',
  linkedin: 'https://www.linkedin.com/company/e2e-otogaleri',
} as const

interface Galeri {
  readonly commercialName: string
  readonly taxNumber: string | null
  readonly tradeRegistryNumber: string | null
  readonly displayNames: Record<string, string>
  readonly socialLinks: Record<string, string>
  readonly version: number
}

async function hesaplar(api: APIRequestContext, kume: Record<string, string>) {
  const simdiki = (await (await api.get('/api/admin/gallery')).json()) as Galeri
  const yanit = await api.put('/api/admin/gallery', {
    data: {
      version: simdiki.version,
      commercialName: simdiki.commercialName,
      taxNumber: simdiki.taxNumber ?? '',
      tradeRegistryNumber: simdiki.tradeRegistryNumber ?? '',
      displayNames: simdiki.displayNames,
      socialLinks: kume,
    },
  })
  expect(yanit.ok(), `PUT gallery → ${yanit.status()} ${await yanit.text()}`).toBe(true)
}

/** Parça isteklerini sayar; `goto`'dan önce kurulur. */
function parcaSayaci(page: Page) {
  const sayac = { deger: 0 }
  page.on('request', (istek) => {
    if (PARCA.test(new URL(istek.url()).pathname)) {
      sayac.deger += 1
    }
  })
  return sayac
}

async function anaSayfa(page: Page, locale = 'tr') {
  await page.goto(`/${locale}/`)
  await expect(page.getByTestId('home-footer')).toBeVisible()
}

function blok(page: Page) {
  return page.getByTestId('footer-social')
}

test.beforeEach(async ({ page }, testInfo) => {
  test.skip(
    testInfo.project.name !== 'masaustu',
    'Sosyal hesaplar global; testler tek profilde sırayla koşar.',
  )
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
})

test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.project.name === 'masaustu') {
    await page.unrouteAll({ behavior: 'ignoreErrors' })
    await hesaplar(page.request, {})
  }
})

test("0024/AC-1, AC-3, AC-30: Panelde altı alan D1 sırasıyla; Instagram'a yazılan adres kaydedilir, ekran yeniden açılınca alandadır ve vitrinde görünür", async ({
  page,
}) => {
  await hesaplar(page.request, {})
  await page.goto('/admin/gallery')
  const bolum = page.getByTestId('gallery-social-links')
  await expect(bolum).toBeVisible()
  await expect(bolum.locator('label')).toHaveText([
    'Instagram',
    'Facebook',
    'YouTube',
    'TikTok',
    'X',
    'LinkedIn',
  ])
  await expect(bolum).toContainText('Boş bırakılan platform vitrinde görünmez')
  await kanit(page, bolum, '0024-ac30.png')

  await bolum.getByLabel('Instagram').fill(`  ${UC.instagram}  `)
  await page.getByTestId('gallery-save').click()
  await expect(page.getByTestId('dealership-saved')).toBeVisible()

  await page.goto('/admin/gallery')
  await expect(page.getByTestId('gallery-social-links').getByLabel('Instagram')).toHaveValue(
    UC.instagram,
  )
  await kanit(page, page.getByTestId('gallery-social-links'), '0024-ac3.png')

  await anaSayfa(page)
  await expect(blok(page).getByRole('link', { name: 'Instagram' })).toHaveAttribute(
    'href',
    UC.instagram,
  )
})

test('0024/AC-19, AC-31: Hatalı adres alanın altında çevrilmiş hatayla; ar panelinde bölüm sağdan sola, adres alanları soldan sağa', async ({
  page,
}) => {
  await page.goto('/admin/gallery')
  const bolum = page.getByTestId('gallery-social-links')
  await bolum.getByLabel('Instagram').fill('https://instagram.com.kotu.site/x')
  await page.getByTestId('gallery-save').click()
  await expect(page.getByTestId('gallery-social-instagram-error')).toContainText(
    'Adres bu platforma ait bir hesap adresi değil.',
  )
  await expect(bolum.getByLabel('Instagram')).toHaveValue('https://instagram.com.kotu.site/x')
  await kanit(page, bolum, '0024-ac19.png')

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
  await page.goto('/admin/gallery')
  const arBolum = page.getByTestId('gallery-social-links')
  await expect(arBolum.locator('legend')).toHaveText('الحسابات الاجتماعية')
  await expect(arBolum).toContainText('المنصة المتروكة فارغة لا تظهر في الموقع.')
  expect(await arBolum.evaluate((el) => getComputedStyle(el).direction)).toBe('rtl')
  for (const alan of await arBolum.locator('input').all()) {
    expect(await alan.evaluate((el) => getComputedStyle(el).direction)).toBe('ltr')
  }
  await kanit(page, arBolum, '0024-ac31-ar.png')
})

test('0024/AC-37, AC-39, AC-60, AC-61, AC-62, AC-63, AC-64, AC-81: Üç hesaplı altlık — sıra, simge, rel="me", target yok, ad, 44×44, currentColor ve görünür odak', async ({
  page,
}) => {
  await hesaplar(page.request, UC)
  await anaSayfa(page)

  const baglantilar = blok(page).getByRole('link')
  await expect(blok(page).getByRole('heading')).toHaveText('Bizi takip edin')
  await expect(baglantilar).toHaveCount(3)
  await expect(blok(page).locator('svg')).toHaveCount(2)
  expect(await baglantilar.evaluateAll((els) => els.map((el) => el.getAttribute('href')))).toEqual(
    Object.values(UC),
  )

  for (const [ad, link] of [
    ['Instagram', baglantilar.nth(0)],
    ['YouTube', baglantilar.nth(1)],
    ['LinkedIn', baglantilar.nth(2)],
  ] as const) {
    await expect(link).toHaveAccessibleName(ad)
    await expect(link).toHaveAttribute('rel', 'me')
    expect(await link.getAttribute('target')).toBeNull()
    const kutu = (await link.boundingBox())!
    expect(kutu.width, `${ad} genişlik`).toBeGreaterThanOrEqual(44)
    expect(kutu.height, `${ad} yükseklik`).toBeGreaterThanOrEqual(44)
  }

  // AC-61 — simgeli bağlantının görünür metni yok; SVG odaklanamaz.
  await expect(baglantilar.nth(0)).toHaveText('')
  await expect(baglantilar.nth(0).locator('svg')).toHaveAttribute('aria-hidden', 'true')
  // AC-81 — LinkedIn simgesiz, görünür metni adı.
  await expect(baglantilar.nth(2)).toHaveText('LinkedIn')
  await expect(baglantilar.nth(2).locator('svg')).toHaveCount(0)

  // AC-63 — simgenin boyası bağlantının metin rengidir.
  const renkler = await baglantilar.nth(0).evaluate((el) => ({
    metin: getComputedStyle(el).color,
    simge: getComputedStyle(el.querySelector('path')!).fill,
  }))
  expect(renkler.simge).toBe(renkler.metin)

  // AC-64 — klavyeyle odak: tarayıcı `:focus-visible`'ı yalnız klavye etkileşiminden sonra uygular.
  await page.keyboard.press('Tab')
  await baglantilar.nth(0).focus()
  const odak = await baglantilar.nth(0).evaluate((el) => ({
    gorunur: el.matches(':focus-visible'),
    stil: getComputedStyle(el).outlineStyle,
    kalinlik: parseFloat(getComputedStyle(el).outlineWidth),
  }))
  expect(odak.gorunur).toBe(true)
  expect(odak.stil).not.toBe('none')
  expect(odak.kalinlik).toBeGreaterThan(0)
  await kanit(page, blok(page), '0024-ac64.png')
  await page.mouse.click(1, 1)
  await kanit(page, page.getByTestId('home-footer'), '0024-ac37.png')
})

test('0024/AC-43: /ar/ altlığında başlık Arapça, belge sırası D1 ve Instagram satırın sağ ucunda; /en/ başlığı İngilizce', async ({
  page,
}) => {
  await hesaplar(page.request, UC)
  await anaSayfa(page, 'ar')

  await expect(blok(page).getByRole('heading')).toHaveText('تابعونا')
  const baglantilar = blok(page).getByRole('link')
  for (const [i, ad] of ['Instagram', 'YouTube', 'LinkedIn'].entries()) {
    await expect(baglantilar.nth(i)).toHaveAccessibleName(ad)
  }
  const kutular = await Promise.all(
    [0, 1, 2].map(async (i) => (await baglantilar.nth(i).boundingBox())!),
  )
  expect(kutular[0]!.x).toBeGreaterThan(kutular[1]!.x)
  expect(kutular[1]!.x).toBeGreaterThan(kutular[2]!.x)
  await kanit(page, page.getByTestId('home-footer'), '0024-ac43-ar.png')

  await anaSayfa(page, 'en')
  await expect(blok(page).getByRole('heading')).toHaveText('Follow us')
})

test('0024/AC-40, AC-44, AC-68, AC-78: Parça yalnız hesaplı ana sayfada ve tam bir kez istenir; hesapsız ana sayfada blok da istek de yok; liste, detay ve içerik sayfalarında hesap bağlantısı yok', async ({
  page,
}) => {
  await hesaplar(page.request, {})
  const sayac = parcaSayaci(page)
  await anaSayfa(page)
  await expect(
    page.getByTestId('footer-corporate').or(page.locator('nav[aria-label="Diller"]')).first(),
  ).toBeVisible()
  await expect(blok(page)).toHaveCount(0)
  await page.waitForLoadState('networkidle')
  expect(sayac.deger, 'hesapsız ana sayfa').toBe(0)
  await kanit(page, page.getByTestId('home-footer'), '0024-ac40.png')

  await hesaplar(page.request, UC)
  await anaSayfa(page)
  await expect(blok(page).locator('svg')).toHaveCount(2)
  await page.waitForLoadState('networkidle')
  expect(sayac.deger, 'hesaplı ana sayfa').toBe(1)

  const liste = (await (
    await page.request.get('/api/catalog/showcase-listings?locale=tr&page=1&pageSize=1')
  ).json()) as { items: { slug: string }[] }
  // Tohumda yayında içerik sayfası yok: test kendi sayfasını açar ve sonunda siler (`0020` AC-9 deseni).
  const acilan = await page.request.post('/api/admin/content/pages', {
    data: {
      locales: [
        {
          locale: 'tr',
          title: 'Sosyal Hesap Notu',
          body: 'Bir içerik sayfası.',
          slug: `e2e-sosyal-${Date.now().toString(36)}`,
        },
      ],
    },
  })
  expect(acilan.status()).toBe(201)
  const { id } = (await acilan.json()) as { id: string }
  const sayfa = (await (await page.request.get(`/api/admin/content/pages/${id}`)).json()) as {
    version: number
    locales: { locale: string; slug: string }[]
  }
  expect(
    (
      await page.request.post(`/api/admin/content/pages/${id}/publish`, {
        data: { version: sayfa.version, locale: 'tr' },
      })
    ).ok(),
  ).toBe(true)
  const icerik = `/tr/${sayfa.locales.find((item) => item.locale === 'tr')!.slug}`

  const adresler = ['/tr/cars', `/tr/cars/${liste.items[0]!.slug}`, icerik]

  const once = sayac.deger
  for (const adres of adresler) {
    await page.goto(adres)
    await page.waitForLoadState('networkidle')
    await expect(page.locator('a[rel="me"]')).toHaveCount(0)
    for (const url of Object.values(UC)) {
      await expect(page.locator(`a[href="${url}"]`)).toHaveCount(0)
    }
  }
  expect(sayac.deger - once, adresler.join(', ')).toBe(0)
  await expect(page.getByTestId('content-page')).toBeVisible()

  await page.request.delete(`/api/admin/content/pages/${id}?version=${sayfa.version + 1}`)
})

test('0024/AC-69, AC-79: Parçanın içe aktarması reddedilince bağlantılar adını görünür metin olarak gösterir, hedef değişmez, yakalanmamış hata yok ve diğer bloklar görünür', async ({
  page,
}) => {
  await hesaplar(page.request, UC)
  const hatalar: string[] = []
  page.on('pageerror', (hata) => hatalar.push(`pageerror: ${hata.message}`))
  page.on('console', (mesaj) => {
    // Reddedilen dosyanın ağ kaydı ("Failed to load resource") tarayıcının günlüğüdür, kodun
    // yakalanmamış hatası değil; AC-79 ikincisini ölçer.
    if (mesaj.type() === 'error' && !mesaj.text().startsWith('Failed to load resource')) {
      hatalar.push(`console: ${mesaj.text()}`)
    }
  })
  await page.route(PARCA, (route) => route.abort())

  await anaSayfa(page)

  const baglantilar = blok(page).getByRole('link')
  await expect(baglantilar).toHaveText(['Instagram', 'YouTube', 'LinkedIn'])
  expect(await baglantilar.evaluateAll((els) => els.map((el) => el.getAttribute('href')))).toEqual(
    Object.values(UC),
  )
  await expect(blok(page).locator('svg')).toHaveCount(0)
  await expect(page.getByRole('link', { name: 'Tüm araçlar' })).toBeVisible()
  await expect(page.getByTestId('home-footer').locator('a[lang="ar"]')).toBeVisible()
  await page.waitForLoadState('networkidle')
  expect(hatalar).toEqual([])
  await kanit(page, page.getByTestId('home-footer'), '0024-ac69.png')
})

test('0024/AC-70: Parça geldiğinde simgeli bağlantıların konumu ve boyutu gelmeden öncekiyle aynıdır', async ({
  page,
}) => {
  await hesaplar(page.request, ALTI)
  let birak: () => void = () => {}
  const bekle = new Promise<void>((resolve) => {
    birak = resolve
  })
  await page.route(PARCA, async (route) => {
    await bekle
    await route.continue()
  })

  await anaSayfa(page)
  const simgeli = blok(page).locator('a.social-link:not(.social-link--text)')
  await expect(simgeli).toHaveCount(5)
  await expect(blok(page).locator('svg')).toHaveCount(0)
  const once = await simgeli.evaluateAll((els) =>
    els.map((el) => {
      const k = el.getBoundingClientRect()
      return [k.x, k.y, k.width, k.height]
    }),
  )

  birak()
  await expect(blok(page).locator('svg')).toHaveCount(5)
  const sonra = await simgeli.evaluateAll((els) =>
    els.map((el) => {
      const k = el.getBoundingClientRect()
      return [k.x, k.y, k.width, k.height]
    }),
  )
  expect(sonra).toEqual(once)
})

test("0024/AC-80: C3'ün mobil görüntü alanında altı hesaplı /tr/ ana sayfanın altlığı ilk ekranın altındadır", async ({
  page,
  browser,
}) => {
  await hesaplar(page.request, ALTI)
  // Lighthouse 13'ün mobil öykünmesi: 412×823, DPR 1.75 (C3).
  const mobil = await browser.newContext({
    viewport: { width: 412, height: 823 },
    deviceScaleFactor: 1.75,
    isMobile: true,
    hasTouch: true,
  })
  try {
    const sayfa = await mobil.newPage()
    await sayfa.goto('/tr/')
    await expect(sayfa.getByTestId('footer-social').locator('svg')).toHaveCount(5)
    const ust = await sayfa
      .getByTestId('home-footer')
      .evaluate((el) => el.getBoundingClientRect().top)
    expect(ust).toBeGreaterThan(823)
    test.info().annotations.push({
      type: 'altlığın üst kenarı',
      description: `${ust} px (görüntü alanı 823 px)`,
    })
  } finally {
    await mobil.close()
  }
})
