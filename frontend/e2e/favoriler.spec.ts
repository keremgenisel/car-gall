import { expect, test, type Browser, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { pngUret } from './gorsel'
import { kanit, kanitSayfa } from './kanit'

/**
 * `frontend-003` — favoriler ve karşılaştırma, GERÇEK Host'a karşı: gerçek tarayıcı deposu, gerçek
 * ilk boyama, gerçek ağ istekleri, gerçek yerleşim. Birim testlerinin göremediği şeyler burada:
 * depo taklitleri (`addInitScript`), sekmeler arası yazma, istek sayımı, 320/360 px ölçüleri,
 * iskelet → içerik CLS'i, satış → geri alma zinciri.
 *
 * Testler tek profilde (masaüstü cihazı; genişlik testte ayarlanır) koşar: satış zinciri sunucu
 * verisini değiştirir ve sonunda geri alır.
 */
test.describe.configure({ mode: 'serial' })

test.beforeEach(async ({ page }, testInfo) => {
  void page
  test.skip(testInfo.project.name !== 'masaustu', 'Genişlik testte ayarlanır; tek profil.')
})

const KEY = 'cargallery.favorites'

interface Ilan {
  readonly slug: string
  readonly modelName: string
  readonly modelYear: number
}

async function ilanlarAsync(page: Page, sayi: number): Promise<Ilan[]> {
  const yanit = await page.request.get(`/api/catalog/showcase-listings?locale=tr&pageSize=${sayi}`)
  return ((await yanit.json()) as { items: Ilan[] }).items
}

/** Depoya sayfa açılmadan yazılan kayıt; eklenme sırası dizinin sırasıdır. */
async function favorilerleAc(page: Page, ilanlar: readonly Ilan[]) {
  await page.addInitScript(
    ([key, items]) => {
      if (sessionStorage.getItem('kuruldu') === null) {
        localStorage.setItem(key, JSON.stringify({ v: 1, items }))
        sessionStorage.setItem('kuruldu', '1')
      }
    },
    [
      KEY,
      ilanlar.map((ilan, index) => ({
        slug: ilan.slug,
        addedAt: `2026-11-26T10:0${index}:00.000Z`,
        modelName: ilan.modelName,
        modelYear: ilan.modelYear,
      })),
    ] as const,
  )
}

async function kayit(page: Page): Promise<string[]> {
  return page.evaluate(
    (key) =>
      (
        (JSON.parse(localStorage.getItem(key) ?? '{"items":[]}') as { items: { slug: string }[] })
          .items ?? []
      ).map((item) => item.slug),
    KEY,
  )
}

const sayac = (page: Page) =>
  page.getByRole('link', { name: /^(Favorilerim|My favourites|مفضلتي)/ })

test('frontend-003/AC-1, AC-2, AC-28: İki ekran 200 ve kendi ekranını basar; alt yolları "bulunamadı"; sayaç bütün vitrin sayfalarında, panelde yok', async ({
  page,
}) => {
  const [ilan] = await ilanlarAsync(page, 1)

  for (const [adres, baslik] of [
    ['/tr/favorites', 'Favorilerim'],
    ['/ar/favorites', 'مفضلتي'],
    ['/en/favorites', 'My favourites'],
    ['/tr/compare?listing=a-2020-aaaaaa&listing=b-2021-bbbbbb', 'Araç karşılaştırma'],
  ] as const) {
    const yanit = await page.goto(adres)
    expect(yanit!.status(), adres).toBe(200)
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(baslik)
  }

  for (const [adres, dosya] of [
    ['/tr/favorites/x', 'frontend-003-ac2.png'],
    ['/tr/compare/x', null],
    ['/tr/compare/x/y', null],
    ['/ar/favorites/x', 'frontend-003-ac2-ar.png'],
  ] as const) {
    const yanit = await page.goto(adres)
    expect(yanit!.status(), adres).toBe(404)
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex')
    await expect(page.getByTestId('content-not-found')).toBeVisible()
    if (dosya !== null) {
      await kanitSayfa(page, dosya)
    }
  }

  const footer = await (await page.goto('/tr/'))!.text()
  const icerik = /href="(\/tr\/[a-z0-9-]+)"/.exec(await page.locator('footer').innerHTML())?.[1]
  expect(footer.length).toBeGreaterThan(0)

  for (const adres of [
    '/tr/',
    '/tr/cars',
    `/tr/cars/${ilan!.slug}`,
    ...(icerik === undefined ? [] : [icerik]),
    '/tr/newsletter/unsubscribe?token=x',
    '/tr/favorites',
    '/tr/compare',
  ]) {
    await page.goto(adres)
    await expect(sayac(page), adres).toBeVisible()
    await expect(sayac(page)).toHaveAttribute('href', '/tr/favorites')
  }

  await yoneticiOlarakGirAsync(page, 'yonetici@cargallery.local', YONETICI_PAROLA)
  await page.goto('/admin/vehicles')
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
  await expect(sayac(page)).toHaveCount(0)
})

test('frontend-003/AC-21, AC-27, AC-31, AC-13: Kartta ekle/çıkar ağ isteği üretmez; sayaç yenilemeden değişir; durum yenilemede ve üç dilde aynı', async ({
  page,
}) => {
  await page.goto('/tr/cars')
  const kart = page.getByTestId('listing-card').first()
  const dugme = kart.getByRole('button', { name: /^Favori: / })
  await expect(sayac(page)).toHaveText('0')
  // Sayfanın kendi yüklemesi (tembel kapaklar, yazı tipi) bitsin; sayılan yalnız düğmenin ürettiği.
  await page.waitForLoadState('networkidle')

  const istekler: string[] = []
  page.on('request', (istek) => istekler.push(`${istek.method()} ${istek.url()}`))

  await dugme.click()
  await expect(dugme).toHaveAttribute('aria-pressed', 'true')
  await expect(sayac(page)).toHaveText('1')
  await dugme.click()
  await expect(dugme).toHaveAttribute('aria-pressed', 'false')
  await dugme.click()
  await expect(sayac(page)).toHaveText('1')
  expect(istekler).toEqual([])

  const [slug] = await kayit(page)
  await page.reload()
  await expect(
    page
      .getByTestId('listing-card')
      .first()
      .getByRole('button', { name: /^Favori: / }),
  ).toHaveAttribute('aria-pressed', 'true')

  for (const dil of ['ar', 'en']) {
    await page.goto(`/${dil}/favorites`)
    await expect(sayac(page)).toHaveText('1')
    await expect(page.getByTestId('favorites-list').getByRole('link').first()).toHaveAttribute(
      'href',
      `/${dil}/cars/${slug}`,
    )
  }
})

test('frontend-003/AC-12: İki sekme — bayat sekme öbürünün eklediğini silmez; görünen "ekle" no-op birleşir; bayat çıkarma yalnız kendisini çıkarır', async ({
  context,
}) => {
  const a = await context.newPage()
  const b = await context.newPage()
  await a.goto('/tr/cars')
  await b.goto('/tr/cars')
  const kartA = a.getByTestId('listing-card')
  const kartB = b.getByTestId('listing-card')
  const x = (kart: typeof kartA) => kart.nth(0).getByRole('button', { name: /^Favori: / })
  const y = (kart: typeof kartA) => kart.nth(1).getByRole('button', { name: /^Favori: / })

  // (i) AC-12'nin kendi sırası: A'da X; B bayat görüntüyle Y ekler → X SİLİNMEZ; B sayaç 2 ve
  // X'i artık basılı çizer (D4: eylemden sonra kayıttan yeniden çizilir).
  await x(kartA).click()
  await y(kartB).click()
  expect(await kayit(b)).toHaveLength(2)
  await expect(sayac(b)).toHaveText('2')
  await expect(x(kartB)).toHaveAttribute('aria-pressed', 'true')
  const [xslug, yslug] = await kayit(b)

  // (iii) A bayat görüntüyle (sayaç 1) X'i çıkarır → yalnız Y kalır.
  await expect(sayac(a)).toHaveText('1')
  await x(kartA).click()
  expect(await kayit(a)).toEqual([yslug])

  // (ii) Bayat "ekle": B kayıtta X yokken yenilenir (X basılı değil); A X'i ekler; B bayat görüntüyle
  // X'e basar → no-op birleşme, X tek kez; B X'i basılı çizer.
  await b.reload()
  await expect(x(kartB)).toHaveAttribute('aria-pressed', 'false')
  await x(kartA).click()
  await x(kartB).click()
  expect((await kayit(b)).filter((slug) => slug === xslug)).toHaveLength(1)
  await expect(x(kartB)).toHaveAttribute('aria-pressed', 'true')
})

for (const [ad, betik] of [
  [
    'erişici atar',
    () =>
      Object.defineProperty(window, 'localStorage', {
        get() {
          throw new DOMException('kapalı', 'SecurityError')
        },
      }),
  ],
  [
    'yazma kota hatası atar',
    () => {
      Storage.prototype.setItem = () => {
        throw new DOMException('dolu', 'QuotaExceededError')
      }
    },
  ],
  [
    'okuma ve yazma atar',
    () => {
      Storage.prototype.getItem = () => {
        throw new DOMException('kapalı', 'SecurityError')
      }
      Storage.prototype.setItem = () => {
        throw new DOMException('kapalı', 'SecurityError')
      }
    },
  ],
] as const) {
  test(`frontend-003/AC-15, AC-16: Depo kullanılamazken (${ad}) liste, ana sayfa ve detay hatasız; düğme ve sayaç yok; favoriler ekranı nedeni söyler; karşılaştırma düğmesiz çalışır`, async ({
    page,
  }) => {
    const ilanlar = await ilanlarAsync(page, 2)
    const hatalar: string[] = []
    page.on('pageerror', (hata) => hatalar.push(hata.message))
    await page.addInitScript(betik)

    for (const adres of ['/tr/cars', '/tr/', `/tr/cars/${ilanlar[0]!.slug}`]) {
      await page.goto(adres)
      await expect(page.locator('main')).not.toBeEmpty()
      await expect(page.getByRole('button', { name: /^Favori|Favorilere ekle/ })).toHaveCount(0)
      await expect(sayac(page)).toHaveCount(0)
    }

    await page.goto('/tr/favorites')
    await expect(page.getByTestId('favorites-unavailable')).toBeVisible()
    await expect(page.getByText('Favorileriniz bu tarayıcıda saklanır')).toHaveCount(0)

    await page.goto(`/tr/compare?listing=${ilanlar[0]!.slug}&listing=${ilanlar[1]!.slug}`)
    await expect(page.getByRole('columnheader')).toHaveCount(2)
    await expect(page.getByRole('button', { name: /^Favori: / })).toHaveCount(0)
    expect(hatalar).toEqual([])
  })
}

test('frontend-003/AC-29: Üç favoriyle açılan listede sayacın aldığı metinler tam olarak ["3"] (0 → 3 geçişi yok)', async ({
  page,
}) => {
  await favorilerleAc(page, await ilanlarAsync(page, 3))
  await page.addInitScript(() => {
    const kayitlar: string[] = []
    ;(window as unknown as { sayacMetinleri: string[] }).sayacMetinleri = kayitlar
    new MutationObserver(() => {
      const baglanti = document.querySelector('.top-bar__favorites')
      const metin = baglanti?.textContent ?? null
      if (metin !== null && kayitlar.at(-1) !== metin) {
        kayitlar.push(metin)
      }
    }).observe(document, { subtree: true, childList: true, characterData: true })
  })

  await page.goto('/tr/cars')
  await expect(sayac(page)).toHaveText('3')
  expect(
    await page.evaluate(() => (window as unknown as { sayacMetinleri: string[] }).sayacMetinleri),
  ).toEqual(['3'])
})

test('frontend-003/AC-24, AC-25, AC-26: Tab ile ulaşılır, Enter ve Space çalışır, odak halkası görünür; 44×44 hedef; dolu/boş gri tonlamada ayırt edilir', async ({
  page,
}) => {
  const [ilan] = await ilanlarAsync(page, 1)
  await page.goto(`/tr/cars/${ilan!.slug}`)
  const dugme = page.getByRole('button', { name: 'Favorilere ekle' })
  await expect(dugme).toBeVisible()

  for (
    let adim = 0;
    adim < 40 && !(await dugme.evaluate((el) => el === document.activeElement));
    adim++
  ) {
    await page.keyboard.press('Tab')
  }
  await expect(dugme).toBeFocused()
  expect(await dugme.evaluate((el) => getComputedStyle(el).outlineStyle)).toBe('solid')
  const kutu = (await dugme.boundingBox())!
  expect(kutu.width).toBeGreaterThanOrEqual(44)
  expect(kutu.height).toBeGreaterThanOrEqual(44)
  await kanit(page, dugme, 'frontend-003-ac25.png')

  await page.keyboard.press('Enter')
  await expect(page.getByRole('button', { name: 'Favorilerde' })).toHaveAttribute(
    'aria-pressed',
    'true',
  )
  await expect(page.getByRole('status').filter({ hasText: 'Favorilere eklendi' })).toHaveCount(1)
  await page.keyboard.press('Space')
  await expect(page.getByRole('button', { name: 'Favorilere ekle' })).toHaveAttribute(
    'aria-pressed',
    'false',
  )
  await expect(page.getByRole('status').filter({ hasText: 'Favorilerden çıkarıldı' })).toHaveCount(
    1,
  )

  // AC-26 — renk dışı fark: dolgu. Basılı ve basılı değil simgenin `fill`'i farklıdır.
  const dolgu = () => dugme.locator('svg path').evaluate((el) => getComputedStyle(el).fill)
  const bos = await dolgu()
  await dugme.click()
  const dolu = await page
    .getByRole('button', { name: 'Favorilerde' })
    .locator('svg path')
    .evaluate((el) => getComputedStyle(el).fill)
  expect(bos).toBe('none')
  expect(dolu).not.toBe('none')
  await page.addStyleTag({ content: 'html { filter: grayscale(1) }' })
  await kanit(page, page.getByRole('button', { name: 'Favorilerde' }), 'frontend-003-ac26-dolu.png')
  await page.getByRole('button', { name: 'Favorilerde' }).click()
  await kanit(
    page,
    page.getByRole('button', { name: 'Favorilere ekle' }),
    'frontend-003-ac26-bos.png',
  )
})

/** Üst çubuğun fikstürü: belge sunucudan gelir, ad etiketi ve (varsa) logo etiketi değiştirilir. */
async function ustCubukFiksturu(page: Page, logolu: boolean) {
  const ad = 'Kırk Karakterlik Uzun Galeri Adı Otomotiv'
  await page.route('**/logo-fikstur.png', (route) =>
    route.fulfill({ contentType: 'image/png', body: pngUret(320, 80) }),
  )
  await page.route(/\/(tr|ar|en)\/cars$/, async (route) => {
    const yanit = await route.fetch()
    let html = (await yanit.text()).replace(
      /<meta name="gallery-display-name" content="[^"]*" \/>/,
      `<meta name="gallery-display-name" content="${ad}" />`,
    )
    if (logolu) {
      html = html.replace(
        '</head>',
        '<meta name="gallery-logo" content="/logo-fikstur.png" data-width="320" data-height="80" /></head>',
      )
    }
    return route.fulfill({ response: yanit, body: html })
  })
  return ad
}

async function genislikAsync(browser: Browser, genislik: number) {
  const context = await browser.newContext({ viewport: { width: genislik, height: 720 } })
  return { context, page: await context.newPage() }
}

/**
 * Üst çubuğun satır sayısı: satır sonu grubu markanın ALTINDA başlıyorsa iki satır. Logosuz ad
 * kendi içinde kırılabilir (0026 AC-34) ve markayı uzatır; bu tek satırda dikey örtüşmedir.
 */
async function satirSayisi(page: Page): Promise<number> {
  return page.evaluate(() => {
    const marka = document.querySelector('[data-testid="top-bar-brand"]')!.getBoundingClientRect()
    const son = document.querySelector('.top-bar__end')!.getBoundingClientRect()
    return son.top >= marka.bottom - 1 ? 2 : 1
  })
}

const SAYAC_HALLERI = [
  ['sayaç yok (depo kapalı)', -1],
  ['1 favori', 1],
  ['24 favori', 24],
] as const

// `0028` D10 · AC-68 — üst çubuğa "İletişim" girdi, eşik yeniden ölçüldü: 40rem → 42rem (672 px).
// Döngü eşiğin bir altını ve kendisini taşır (0028 Context tablosu, CLARIFY B6).
for (const genislik of [320, 360, 671, 672]) {
  for (const dil of ['tr', 'ar', 'en']) {
    test(`frontend-003/AC-32, 0026/AC-34, 0028/AC-68: ${genislik} px, ${dil} — satır sayısı içerikten bağımsız (42rem altı 2, üstü 1); yatay taşma yok; logoluda adın ≥ 6 karakteri görünür; sayaç ve dil bağlantıları tam görünür ve tıklanabilir (dar ekranda ≥ 44 px); logosuz ad kırılabilir ve CLS ≤ 0.1`, async ({
      browser,
    }) => {
      const beklenenSatir = genislik < 672 ? 2 : 1

      for (const logolu of [false, true]) {
        for (const [hal, sayi] of SAYAC_HALLERI) {
          const { context, page } = await genislikAsync(browser, genislik)
          await page.addInitScript(
            ([n]) => {
              if (n < 0) {
                Storage.prototype.setItem = () => {
                  throw new DOMException('dolu', 'QuotaExceededError')
                }
              } else {
                localStorage.setItem(
                  'cargallery.favorites',
                  JSON.stringify({
                    v: 1,
                    items: Array.from({ length: n }, (_, i) => ({
                      slug: `a-2020-${String(i).padStart(6, '0')}`,
                      addedAt: 'x',
                      modelName: 'A',
                      modelYear: 2020,
                    })),
                  }),
                )
              }
              // Üst çubuğun CLS'i (AC-32 i): kaynaklarından biri üst çubuğun içinde olan kaymalar.
              // Sayfanın geri kalanının font kayması bu kriterin konusu değil; ayrıca sayılır.
              const w = window as unknown as { cls: number; sayfaCls: number }
              w.cls = 0
              w.sayfaCls = 0
              const ustte = (dugum: Node | null | undefined) =>
                (dugum instanceof Element ? dugum : dugum?.parentElement)?.closest(
                  'header.top-bar',
                ) != null
              new PerformanceObserver((liste) => {
                for (const kayit of liste.getEntries() as unknown as {
                  value: number
                  hadRecentInput: boolean
                  sources: {
                    node?: Node | null
                    previousRect: DOMRectReadOnly
                    currentRect: DOMRectReadOnly
                  }[]
                }[]) {
                  if (kayit.hadRecentInput) {
                    continue
                  }
                  w.sayfaCls += kayit.value
                  // Yalnız üst çubuğun öğelerinin payı: CLS'in tanımı (etki alanı × uzaklık oranı),
                  // aynı karede kayan sayfa içeriği bu paya girmez.
                  const alan = innerWidth * innerHeight
                  const enUzun = Math.max(innerWidth, innerHeight)
                  for (const { node, previousRect: a, currentRect: b } of kayit.sources) {
                    if (!ustte(node)) {
                      continue
                    }
                    const birlesim =
                      a.width * a.height +
                      b.width * b.height -
                      Math.max(0, Math.min(a.right, b.right) - Math.max(a.left, b.left)) *
                        Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top))
                    const uzaklik = Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
                    w.cls += (birlesim / alan) * (uzaklik / enUzun)
                  }
                }
              }).observe({ type: 'layout-shift', buffered: true })
            },
            [sayi] as const,
          )
          const ad = await ustCubukFiksturu(page, logolu)
          const etiket = `logolu=${logolu}, ${hal}`
          // Web fontları bekletilir: "fonttan önce" ölçümü gerçekten yedek fontla alınır (QA 2 BULGU-6).
          let fontuBirak: () => void = () => {}
          const font = new Promise<void>((resolve) => (fontuBirak = resolve))
          await page.route('**/*.woff2', async (route) => {
            await font
            await route.continue()
          })
          await page.goto(`/${dil}/cars`, { waitUntil: 'domcontentloaded' })
          await expect(page.locator('.top-bar__end')).toBeVisible()
          expect(await page.evaluate(() => document.fonts.status), etiket).toBe('loading')
          // Satır sayısı yedek fontla ve web fontu geldikten sonra aynı (içerikten bağımsız kırılma).
          expect(await satirSayisi(page), etiket).toBe(beklenenSatir)
          fontuBirak()
          await page.evaluate(() => document.fonts.ready)
          await page.waitForLoadState('networkidle')
          expect(await satirSayisi(page), etiket).toBe(beklenenSatir)

          const kaydirma = await page.evaluate(() => {
            const kok = document.scrollingElement!
            return kok.scrollWidth - kok.clientWidth
          })
          expect(kaydirma, etiket).toBeLessThanOrEqual(0)

          const hedefler = [
            ...(sayi < 0 ? [] : [sayac(page)]),
            ...(await page.locator('.top-bar__locales a').all()),
          ]
          await expect(sayac(page)).toHaveCount(sayi < 0 ? 0 : 1)
          for (const hedef of hedefler) {
            await expect(hedef, etiket).toBeInViewport({ ratio: 1 })
            const kutu = (await hedef.boundingBox())!
            if (genislik < 672) {
              expect(kutu.height, etiket).toBeGreaterThanOrEqual(44)
              expect(kutu.width, etiket).toBeGreaterThanOrEqual(44)
            }
            const ustteki = await page.evaluate(
              ([x, y]) => document.elementFromPoint(x!, y!)?.closest('a')?.outerHTML ?? '',
              [kutu.x + kutu.width / 2, kutu.y + kutu.height / 2],
            )
            expect(ustteki, etiket).toBe(await hedef.evaluate((el) => el.outerHTML))
          }

          if (logolu) {
            // Görünen karakter: adın metin düğümünde kutunun içine tam sığanlar; üç nokta bir karakter yer.
            const gorunen = await page
              .locator('[data-testid="top-bar-brand"] bdi')
              .evaluate((bdi) => {
                const kutu = bdi.getBoundingClientRect()
                const metin = bdi.firstChild!
                let n = 0
                for (let i = 0; i < (metin.textContent ?? '').length; i++) {
                  const range = document.createRange()
                  range.setStart(metin, i)
                  range.setEnd(metin, i + 1)
                  const r = range.getBoundingClientRect()
                  if (r.width > 0 && r.left >= kutu.left - 0.5 && r.right <= kutu.right + 0.5) {
                    n++
                  }
                }
                return n
              })
            expect(gorunen - 1, `${etiket} — görünen karakter (${ad})`).toBeGreaterThanOrEqual(6)
          } else {
            expect(
              await page.evaluate(() => (window as unknown as { cls: number }).cls),
              etiket,
            ).toBeLessThanOrEqual(0.1)
          }

          if (sayi === 24 && dil !== 'en' && (genislik === 320 || genislik === 639)) {
            await kanit(
              page,
              'header.top-bar',
              `frontend-003-ac32-${genislik}-${dil}-${logolu ? 'logolu' : 'logosuz'}.png`,
            )
          }
          await context.close()
        }
      }
    })
  }
}

for (const dil of ['tr', 'ar', 'en']) {
  for (const logolu of [false, true]) {
    test(`frontend-003/AC-32, 0026/AC-34: ${dil}, ${logolu ? 'logolu' : 'logosuz'} — 320→1280 px (16 px adım), sayaç yok ve 24, fontlar bekletilerek: CLS ≤ 0.1, üst çubuk yüksekliği font öncesi = sonrası, yatay taşma yok; 320 px'te adın ≥ 6 karakteri görünür ve "…" adın sonunda`, async ({
      browser,
    }) => {
      test.setTimeout(600_000)
      const sorunlar: string[] = []

      for (let genislik = 320; genislik <= 1280; genislik += 16) {
        for (const sayi of [-1, 24]) {
          const { context, page } = await genislikAsync(browser, genislik)
          await page.addInitScript(
            ([n]) => {
              if (n < 0) {
                Storage.prototype.setItem = () => {
                  throw new DOMException('dolu', 'QuotaExceededError')
                }
              } else {
                localStorage.setItem(
                  'cargallery.favorites',
                  JSON.stringify({
                    v: 1,
                    items: Array.from({ length: n }, (_, i) => ({
                      slug: `a-2020-${String(i).padStart(6, '0')}`,
                      addedAt: 'x',
                      modelName: 'A',
                      modelYear: 2020,
                    })),
                  }),
                )
              }
              // Üst çubuğun CLS'i (AC-32 i): kaynaklarından biri üst çubuğun içinde olan kaymalar.
              // Sayfanın geri kalanının font kayması bu kriterin konusu değil; ayrıca sayılır.
              const w = window as unknown as { cls: number; sayfaCls: number }
              w.cls = 0
              w.sayfaCls = 0
              const ustte = (dugum: Node | null | undefined) =>
                (dugum instanceof Element ? dugum : dugum?.parentElement)?.closest(
                  'header.top-bar',
                ) != null
              new PerformanceObserver((liste) => {
                for (const kayit of liste.getEntries() as unknown as {
                  value: number
                  hadRecentInput: boolean
                  sources: {
                    node?: Node | null
                    previousRect: DOMRectReadOnly
                    currentRect: DOMRectReadOnly
                  }[]
                }[]) {
                  if (kayit.hadRecentInput) {
                    continue
                  }
                  w.sayfaCls += kayit.value
                  // Yalnız üst çubuğun öğelerinin payı: CLS'in tanımı (etki alanı × uzaklık oranı),
                  // aynı karede kayan sayfa içeriği bu paya girmez.
                  const alan = innerWidth * innerHeight
                  const enUzun = Math.max(innerWidth, innerHeight)
                  for (const { node, previousRect: a, currentRect: b } of kayit.sources) {
                    if (!ustte(node)) {
                      continue
                    }
                    const birlesim =
                      a.width * a.height +
                      b.width * b.height -
                      Math.max(0, Math.min(a.right, b.right) - Math.max(a.left, b.left)) *
                        Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top))
                    const uzaklik = Math.max(Math.abs(a.x - b.x), Math.abs(a.y - b.y))
                    w.cls += (birlesim / alan) * (uzaklik / enUzun)
                  }
                }
              }).observe({ type: 'layout-shift', buffered: true })
            },
            [sayi] as const,
          )
          await ustCubukFiksturu(page, logolu)
          let fontuBirak: () => void = () => {}
          const font = new Promise<void>((resolve) => (fontuBirak = resolve))
          await page.route('**/*.woff2', async (route) => {
            await font
            await route.continue()
          })
          await page.goto(`/${dil}/cars`, { waitUntil: 'domcontentloaded' })
          await page.locator('.top-bar__end').waitFor()
          const etiket = `${genislik} px, sayaç ${sayi < 0 ? 'yok' : sayi}`
          const once = await page
            .locator('header.top-bar')
            .evaluate((el) => el.getBoundingClientRect().height)
          fontuBirak()
          await page.evaluate(() => document.fonts.ready)
          await page.waitForLoadState('networkidle')
          const sonra = await page
            .locator('header.top-bar')
            .evaluate((el) => el.getBoundingClientRect().height)
          const olcu = await page.evaluate(() => ({
            cls: (window as unknown as { cls: number }).cls,
            tasma: document.scrollingElement!.scrollWidth - document.scrollingElement!.clientWidth,
          }))

          if (olcu.cls > 0.1) sorunlar.push(`${etiket}: CLS ${olcu.cls.toFixed(3)}`)
          if (Math.abs(once - sonra) > 0.5) sorunlar.push(`${etiket}: yükseklik ${once} → ${sonra}`)
          if (olcu.tasma > 0) sorunlar.push(`${etiket}: yatay taşma ${olcu.tasma}`)

          if (genislik === 320) {
            // Görünen karakterler adın BAŞINDAN başlar (kesilme sonda) ve en az 6'dır.
            const gorunen = await page
              .locator('[data-testid="top-bar-brand"] bdi')
              .evaluate((bdi) => {
                const kutu = bdi.getBoundingClientRect()
                const metin = bdi.firstChild!
                const tam: number[] = []
                for (let i = 0; i < (metin.textContent ?? '').length; i++) {
                  const range = document.createRange()
                  range.setStart(metin, i)
                  range.setEnd(metin, i + 1)
                  const r = range.getBoundingClientRect()
                  if (r.width > 0 && r.left >= kutu.left - 0.5 && r.right <= kutu.right + 0.5) {
                    tam.push(i)
                  }
                }
                return { sayi: tam.length, ilk: tam[0] ?? -1, metin: metin.textContent }
              })
            if (gorunen.sayi - 1 < 6) sorunlar.push(`${etiket}: görünen karakter ${gorunen.sayi}`)
            if (gorunen.ilk !== 0)
              sorunlar.push(`${etiket}: kesilme baştan (ilk görünen ${gorunen.ilk})`)
            // Erişilebilir ad tam addır (görünen metin kesik olsa da).
            await expect(page.getByTestId('top-bar-brand')).toHaveAccessibleName(
              'Kırk Karakterlik Uzun Galeri Adı Otomotiv',
            )
            if (sayi === 24) {
              await kanit(
                page,
                'header.top-bar',
                `frontend-003-ac32-320-${dil}-${logolu ? 'logolu' : 'logosuz'}-font.png`,
              )
            }
          }
          await context.close()
        }
      }

      expect(sorunlar).toEqual([])
    })
  }
}

test('frontend-003/AC-33, AC-34, AC-35, AC-44, AC-46, AC-48, AC-65: Favoriler son eklenen önce; her okuma tek slug; seçim → karşılaştırma ekran sırasıyla; oturum boyunca yazma isteği yok ve birden çok slug taşıyan tek istek karşılaştırma belgesi (≤ 3)', async ({
  page,
}) => {
  const istekler: { method: string; url: string; type: string }[] = []
  page.on('request', (istek) =>
    istekler.push({ method: istek.method(), url: istek.url(), type: istek.resourceType() }),
  )

  await page.goto('/tr/cars')
  for (let i = 0; i < 4; i++) {
    await page
      .getByTestId('listing-card')
      .nth(i)
      .getByRole('button', { name: /^Favori: / })
      .click()
  }
  const sirali = await kayit(page)

  await page.goto('/tr/favorites')
  const liste = page.getByTestId('favorites-list')
  await expect(liste.getByTestId('listing-card')).toHaveCount(4)
  const gorunen = await liste
    .getByTestId('listing-card')
    .evaluateAll((kartlar) =>
      kartlar.map((kart) => kart.querySelector('a')!.getAttribute('href')!.split('/').pop()!),
    )
  expect(gorunen).toEqual([...sirali].reverse())
  await expect(
    page.getByText('Favorileriniz bu tarayıcıda saklanır; tarayıcı verisi silinirse kaybolur.'),
  ).toBeVisible()
  // AC-34 — gerçek Host, gerçek veri: her kart detay okumasının BUGÜNKÜ değerlerini taşır.
  for (const slug of gorunen) {
    const detay = (await (
      await page.request.get(`/api/catalog/showcase-listings/${slug}?locale=tr`)
    ).json()) as {
      modelName: string
      modelYear: number
      mileageKilometers: number
      branchName: string
      isReserved: boolean
      price: { onRequest: boolean; amount: number; currencyCode: string; minorUnitExponent: number }
    }
    const kart = liste
      .getByTestId('listing-card')
      .filter({ has: page.locator(`a[href="/tr/cars/${slug}"]`) })
    const sayi = (deger: number) => new Intl.NumberFormat('tr-u-nu-latn').format(deger)
    const fiyat = detay.price.onRequest
      ? 'Fiyat sorunuz'
      : new Intl.NumberFormat('tr-u-nu-latn', {
          style: 'currency',
          currency: detay.price.currencyCode,
          minimumFractionDigits: detay.price.minorUnitExponent,
          maximumFractionDigits: detay.price.minorUnitExponent,
        }).format(detay.price.amount)
    await expect(kart.getByRole('link', { name: detay.modelName })).toBeVisible()
    await expect(kart).toContainText(`${detay.modelYear} · ${sayi(detay.mileageKilometers)} km`)
    await expect(kart).toContainText(fiyat)
    await expect(kart).toContainText(detay.branchName)
    await expect(kart.getByText('Rezerve', { exact: true })).toHaveCount(detay.isReserved ? 1 : 0)
  }
  await kanitSayfa(page, 'frontend-003-ac33.png')

  await page.getByRole('checkbox').nth(2).check()
  await page.getByRole('checkbox').nth(0).check()
  await page.getByRole('button', { name: 'Karşılaştır' }).click()
  await page.waitForURL(/\/tr\/compare\?/)
  expect(new URL(page.url()).searchParams.getAll('listing')).toEqual([gorunen[0], gorunen[2]])
  await expect(page.getByRole('columnheader')).toHaveCount(2)

  // AC-65 — yazma isteği yok; birden çok favori slug'ı taşıyan istek yalnız karşılaştırma belgesi.
  expect(istekler.filter((istek) => istek.method !== 'GET')).toEqual([])
  const cokSluglu = istekler.filter(
    (istek) => sirali.filter((slug) => istek.url.includes(slug)).length > 1,
  )
  expect(
    cokSluglu.every((istek) => istek.type === 'document' && istek.url.includes('/tr/compare?')),
  ).toBe(true)
  expect(
    cokSluglu.every((istek) => new URL(istek.url).searchParams.getAll('listing').length <= 3),
  ).toBe(true)
  expect(
    istekler
      .filter((istek) => istek.url.includes('/api/'))
      .every((istek) => sirali.filter((slug) => istek.url.includes(slug)).length <= 1),
  ).toBe(true)
  expect(istekler.some((istek) => istek.url.includes('/api/catalog/showcase-listings/'))).toBe(true)
})

test("frontend-003/AC-10, AC-55 (c), AC-63: Dil değişince karşılaştırma aynı slug'ları aynı sırayla taşır; satırlar gerçek detay yanıtının anahtar kümesi; ar'da ilk araç sağda", async ({
  page,
}) => {
  const [x, y] = await ilanlarAsync(page, 2)
  await page.goto(`/tr/compare?listing=${x!.slug}&listing=${y!.slug}&utm_source=e2e`)
  await expect(page.getByRole('columnheader')).toHaveCount(2)

  const detay = (await (
    await page.request.get(`/api/catalog/showcase-listings/${x!.slug}?locale=tr`)
  ).json()) as {
    attributes: { key: string }[]
  }
  const detay2 = (await (
    await page.request.get(`/api/catalog/showcase-listings/${y!.slug}?locale=tr`)
  ).json()) as {
    attributes: { key: string }[]
  }
  const anahtarlar = new Set([...detay.attributes, ...detay2.attributes].map((item) => item.key))
  // Fiyat, model yılı, kilometre, şube + yanıttaki nitelikler; başka satır yok.
  await expect(page.getByRole('rowheader')).toHaveCount(4 + anahtarlar.size)
  const ADLAR: Record<string, string> = {
    body_type: 'Kasa tipi',
    fuel_type: 'Yakıt',
    transmission_type: 'Vites',
    drivetrain_type: 'Çekiş',
    trim_level: 'Donanım paketi',
    color: 'Renk',
  }
  // Satır adları da: çekirdek dört, sonra niteliklerin vitrin adları ilk görülme sırasıyla (AC-56).
  expect(await page.getByRole('rowheader').allTextContents()).toEqual([
    'Fiyat',
    'Model yılı',
    'Kilometre',
    'Şube',
    ...[...anahtarlar].map((anahtar) => ADLAR[anahtar] ?? anahtar),
  ])

  await page.locator('nav.top-bar__locales').getByRole('link', { name: 'العربية' }).click()
  await page.waitForURL(/\/ar\/compare\?/)
  expect(new URL(page.url()).searchParams.getAll('listing')).toEqual([x!.slug, y!.slug])
  expect(new URL(page.url()).searchParams.get('utm_source')).toBe('e2e')

  const sutunlar = page.getByRole('columnheader')
  const ilk = (await sutunlar.nth(0).boundingBox())!
  const ikinci = (await sutunlar.nth(1).boundingBox())!
  expect(ilk.x).toBeGreaterThan(ikinci.x)
  await expect(sutunlar.nth(0).getByRole('link')).toHaveAttribute('href', `/ar/cars/${x!.slug}`)
  // Hücreler sayfanın yönünde (rakamlı değer de): hiza karışmaz; değer `<bdi>` içinde yalıtılır.
  const yonler = await page
    .locator('td')
    .evaluateAll((hucreler) => hucreler.map((td) => getComputedStyle(td).direction))
  expect(new Set(yonler)).toEqual(new Set(['rtl']))
  await kanitSayfa(page, 'frontend-003-ac63-ar.png')
})

test("frontend-003/AC-62: En kötü fikstürle 360 px'te üç sütun iç kaydırmasız; 320 px'te sayfa kaymaz; kap odaklanabilir bölge; uzun belirteç hücrede kırılır", async ({
  browser,
}) => {
  const uzunAd = `${'M'.repeat(80)} ${'Modelmodelmodel'.repeat(8)}`
  const fikstur = (slug: string) => ({
    slug,
    isReserved: true,
    images: [],
    modelName: uzunAd,
    modelYear: 2024,
    mileageKilometers: 999999,
    price: { onRequest: false, amount: 123456789012, currencyCode: 'TRY', minorUnitExponent: 2 },
    branchName: 'Çok Uzun Bir Şube Adı Büyükşehir Merkez Kampüsü Satış Ofisi',
    attributes: [
      { key: 'body_type', label: 'Hatchback', value: 'Hatchback' },
      { key: 'color', label: 'X', value: 'Kırmızımetalikinciparlaklıkta' },
    ],
  })

  for (const [genislik, dil] of [
    [360, 'tr'],
    [360, 'ar'],
    [360, 'en'],
    [320, 'tr'],
    [320, 'ar'],
    [320, 'en'],
  ] as const) {
    const { context, page } = await genislikAsync(browser, genislik)
    await page.route('**/api/catalog/showcase-listings/*', (route) =>
      route.fulfill({ json: fikstur(new URL(route.request().url()).pathname.split('/').pop()!) }),
    )
    await page.goto(
      `/${dil}/compare?listing=a-2024-aaaaaa&listing=b-2024-bbbbbb&listing=c-2024-cccccc`,
    )
    const kap = page.getByRole('region', { name: /./ })
    await expect(kap.getByRole('columnheader')).toHaveCount(3)
    await expect(kap).toHaveAttribute('tabindex', '0')

    const olcu = await kap.evaluate((el) => ({
      tablo: el.querySelector('table')!.scrollWidth,
      kap: el.clientWidth,
      sayfa: document.scrollingElement!.scrollWidth - document.scrollingElement!.clientWidth,
    }))
    if (genislik === 360) {
      expect(olcu.tablo).toBeLessThanOrEqual(olcu.kap)
    }
    expect(olcu.sayfa).toBeLessThanOrEqual(0)
    await kanitSayfa(page, `frontend-003-ac62-${genislik}-${dil}.png`)
    await context.close()
  }
})

test('frontend-003/AC-43: Yavaş okumada iskeletten içeriğe geçişte CLS < 0.1', async ({ page }) => {
  await favorilerleAc(page, await ilanlarAsync(page, 4))
  await page.route('**/api/catalog/showcase-listings/*', async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 800))
    await route.continue()
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

  await page.goto('/tr/favorites')
  await expect(page.getByTestId('favorites-skeleton')).toBeVisible()
  await expect(page.getByTestId('favorites-list').getByTestId('listing-card')).toHaveCount(4)
  await page.waitForLoadState('networkidle')
  expect(await page.evaluate(() => (window as unknown as { cls: number }).cls)).toBeLessThan(0.1)
})

test('frontend-003/AC-74, AC-40, AC-41: ar favoriler ekranı RTL — boş, hepsi düşmüş, dolu ve mobil', async ({
  browser,
  page,
}) => {
  await page.goto('/ar/favorites')
  await expect(page.getByTestId('favorites-empty')).toBeVisible()
  expect(await page.locator('html').getAttribute('dir')).toBe('rtl')
  await kanitSayfa(page, 'frontend-003-ac74-bos-ar.png')

  const ilanlar = await ilanlarAsync(page, 3)
  const { context, page: mobil } = await genislikAsync(browser, 390)
  await favorilerleAc(mobil, [
    { slug: 'satilmis-2019-ffffff', modelName: 'Fiat Egea', modelYear: 2019 },
    ...ilanlar,
  ])
  await mobil.goto('/ar/favorites')
  await expect(mobil.getByTestId('favorites-list').getByTestId('listing-card')).toHaveCount(3)
  await expect(mobil.getByTestId('favorites-gone')).toContainText('Fiat Egea 2019')
  await kanitSayfa(mobil, 'frontend-003-ac74-dolu-ar-mobil.png')
  await context.close()

  const { context: c2, page: dusmus } = await genislikAsync(browser, 1280)
  await favorilerleAc(dusmus, [
    { slug: 'satilmis-2019-ffffff', modelName: 'Fiat Egea', modelYear: 2019 },
  ])
  await dusmus.goto('/ar/favorites')
  await expect(dusmus.getByTestId('favorites-all-gone')).toBeVisible()
  const hepsini = dusmus.getByRole('button', { name: 'إزالة الكل' })
  const baslik = dusmus.locator('#favorites-gone-title')
  // RTL: "hepsini kaldır" başlığın satır sonu tarafında, yani solunda.
  expect((await hepsini.boundingBox())!.x).toBeLessThan((await baslik.boundingBox())!.x)
  await kanitSayfa(dusmus, 'frontend-003-ac74-dusmus-ar.png')
  await c2.close()
})

/** Kendi aracımız: yönetim ucundan kayıt, hazır görsel, yayın; vitrindeki slug'ı döner. */
async function yayindaAracAsync(
  page: Page,
  kilometre: number,
): Promise<{ vehicleId: string; slug: string }> {
  const olustur = await page.request.post('/api/admin/vehicles', {
    data: {
      branchId: '00000000-0000-7000-8000-00000000b001',
      vehicleModelId: '0192c000-0000-7000-8000-00000000000a',
      bodyType: 'suv',
      fuelType: 'diesel',
      transmissionType: 'automatic',
      drivetrainType: 'front-wheel-drive',
      trimLevel: 'comfort',
      color: { tr: 'Beyaz' },
      modelYear: 2019,
      mileageKilometers: kilometre,
      priceAmount: 750000,
      priceOnRequest: false,
      vin: `FAV${kilometre}`.padEnd(17, '0'),
    },
  })
  expect(olustur.status()).toBe(201)
  const { vehicleId } = (await olustur.json()) as { vehicleId: string }

  const yukle = await page.request.post('/api/admin/media/assets', {
    multipart: { file: { name: 'arac.png', mimeType: 'image/png', buffer: pngUret() } },
  })
  expect(yukle.status()).toBe(202)
  const { id } = (await yukle.json()) as { id: string }
  await expect(async () => {
    const varlik = (await (await page.request.get(`/api/admin/media/assets/${id}`)).json()) as {
      status: number
    }
    expect(varlik.status).toBe(3)
  }).toPass({ timeout: 60_000 })

  expect(
    (
      await page.request.put(`/api/admin/vehicles/${vehicleId}/media`, {
        data: { mediaAssetIds: [id] },
      })
    ).status(),
  ).toBe(204)
  expect((await page.request.post(`/api/admin/vehicles/${vehicleId}/publish`)).status()).toBe(204)

  const vitrin = (await (
    await page.request.get(
      `/api/catalog/showcase-listings?locale=tr&mileageMin=${kilometre}&mileageMax=${kilometre}`,
    )
  ).json()) as { items: { slug: string }[] }
  return { vehicleId, slug: vitrin.items[0]!.slug }
}

test('frontend-003/AC-36, AC-37, AC-38: Satılan favori "artık vitrinde değil"de; açmak silmez; satış geri alınınca görünür favori olarak döner', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page, 'yonetici@cargallery.local', YONETICI_PAROLA)
  const kilometre = 900_000 + (Date.now() % 90_000)
  const { vehicleId, slug } = await yayindaAracAsync(page, kilometre)

  await page.goto(`/tr/cars/${slug}`)
  await page.getByRole('button', { name: 'Favorilere ekle' }).click()
  expect((await page.request.post(`/api/admin/vehicles/${vehicleId}/sold`)).status()).toBe(204)

  await page.goto('/tr/favorites')
  await expect(page.getByTestId('favorites-all-gone')).toBeVisible()
  await expect(page.getByTestId('favorites-gone')).toContainText('2019')
  await page.reload()
  await expect(page.getByTestId('favorites-gone')).toBeVisible()
  expect(await kayit(page)).toEqual([slug])
  await kanitSayfa(page, 'frontend-003-ac41.png')

  expect(
    (
      await page.request.post(`/api/admin/vehicles/${vehicleId}/revert-sale`, {
        data: {
          reason: 'Yanlış araca tıklandı, satış başka araçtaydı.',
          expectedStatus: 'Published',
        },
      })
    ).status(),
  ).toBe(204)
  await page.reload()
  await expect(page.getByTestId('favorites-list').getByTestId('listing-card')).toHaveCount(1)
  await expect(page.getByTestId('favorites-gone')).toHaveCount(0)

  // AC-37 — kaldırınca sayaç azalır.
  await page
    .getByTestId('favorites-list')
    .getByRole('button', { name: /^Favori: / })
    .click()
  await expect(sayac(page)).toHaveText('0')
  await expect(page.getByTestId('favorites-empty')).toBeVisible()
  await kanitSayfa(page, 'frontend-003-ac40.png')
})
