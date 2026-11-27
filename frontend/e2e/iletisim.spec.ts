import {
  expect,
  test,
  type APIRequestContext,
  type BrowserContext,
  type Page,
  type Request,
} from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { pngUret } from './gorsel'
import { kanit } from './kanit'

/**
 * `0028` — iletişim sayfası, sadeleşen altlık ve üst çubuğun bağlantısı, gerçek Host'a karşı.
 *
 * <b>Google'a gidilmez.</b> CI'ın e2e Host'u sahte bir anahtarla kurulur (`ci.yml`,
 * `GoogleMaps__EmbedApiKey: e2e-sahte-anahtar`): sunucu gerçek biçimli `embedUrl` döndürür ve
 * `https://www.google.com/maps/**` bağlamın `route`'uyla yerel bir taslak sayfaya yönlenir. Ağ
 * denetimi yine de o adrese giden İSTEĞİ görür (istek yola çıkar, yanıtı taslaktır).
 *
 * <b>Paylaşılan durum.</b> Şube açan testler sonunda KALICI kapatır; şubeler global olduğu için
 * yalnız masaüstü profilinde ve sırayla koşar.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const API = '/api/admin/branches'
const ANAHTAR = 'e2e-sahte-anahtar'
const GUNLER = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']
const HAFTA_ICI = { start: '09:00', end: '18:00' }
const PROGRAMLI = Object.fromEntries(
  GUNLER.map((gun) => [
    gun,
    gun === 'saturday' ? [{ start: '10:00', end: '14:00' }] : gun === 'sunday' ? [] : [HAFTA_ICI],
  ]),
)

type Harita = { linkUrl: string; embedUrl?: string }
type Sube = {
  name: string
  address: string
  email: string | null
  map: Harita | null
  openNow: {
    open: boolean
    today: string
    next: { date: string; time: string } | null
    refreshInSeconds: number
  } | null
  openingHours: unknown
}
type Galeri = { displayName: string; branches: Sube[]; socialLinks: unknown[] }

const acilanlar: string[] = []

function benzersiz(onek: string) {
  return `${onek} ${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`
}

function koken(page: Page) {
  return new URL(page.url()).origin
}

async function subeAc(api: APIRequestContext, ad: string, alanlar: Record<string, unknown>) {
  const yanit = await api.post(API, {
    data: {
      names: { tr: ad, ar: `فرع ${ad}`, en: `${ad} Branch` },
      addresses: { tr: `${ad} Caddesi 1` },
      phone: '+902161230000',
      timeZone: 'Europe/Istanbul',
      slug: benzersiz('e2e-iletisim').replace(/\s/g, '-').toLowerCase(),
      ...alanlar,
    },
  })
  expect(yanit.status(), `POST branch → ${await yanit.text()}`).toBe(201)
  const { id } = (await yanit.json()) as { id: string }
  acilanlar.push(id)
  return id
}

async function kapat(api: APIRequestContext) {
  while (acilanlar.length > 0) {
    const id = acilanlar.pop()!
    const { version, status } = (await (await api.get(`${API}/${id}`)).json()) as {
      version: number
      status: string
    }
    if (status !== 'Closed') {
      await api.post(`${API}/${id}/close`, { data: { version } })
    }
  }
}

/** Galeri yanıtını GERÇEK yanıttan değiştirir: şekil sunucununki kalır, yalnız `degistir` dokunur. */
async function galeriyiDegistir(hedef: Page | BrowserContext, degistir: (galeri: Galeri) => void) {
  await hedef.route('**/api/dealership/gallery**', async (route) => {
    const yanit = await route.fetch()
    const govde = (await yanit.json()) as Galeri
    degistir(govde)
    await route.fulfill({ response: yanit, json: govde })
  })
}

/** Gömme adresi ve "daha büyük haritada aç"ın hedefi yerel taslak sayfadır; Google'a gidilmez. */
async function googleTaslagi(baglam: BrowserContext) {
  await baglam.route('https://www.google.com/maps/embed/**', (route) =>
    route.fulfill({
      contentType: 'text/html',
      body: '<!doctype html><title>harita</title><a id="buyuk" target="_blank" href="https://www.google.com/maps/place/taslak">Daha büyük haritada aç</a>',
    }),
  )
  await baglam.route('https://www.google.com/maps/place/**', (route) =>
    route.fulfill({ contentType: 'text/html', body: '<!doctype html><title>yer</title>' }),
  )
}

/**
 * Sayfanın KENDİ aldığı galeri yanıtı (yönlendirilmişse yönlendirilmiş hâli). Şubeler global: paralel bir
 * spec şubesini açıp kapatabilir, sayfadan sonra yapılan ayrı bir okuma başka bir küme döndürür
 * (QA 2 sonrası tam koşuda ölçüldü).
 */
async function gitVeGaleri(page: Page, adres: string): Promise<Galeri> {
  const yanit = page.waitForResponse((r) => r.url().includes('/api/dealership/gallery'))
  await page.goto(adres)
  return (await (await yanit).json()) as Galeri
}

/** Fontlar bekletilir; `birak()` serbest bırakır ve yüklenmelerini bekler (`frontend-003` dersi). */
async function fontlariBeklet(page: Page) {
  let birak: () => void = () => {}
  const font = new Promise<void>((resolve) => (birak = resolve))
  await page.route('**/*.woff2', async (route) => {
    await font
    await route.continue()
  })
  return async () => {
    birak()
    await page.evaluate(() => document.fonts.ready)
  }
}

/** Kadıköy tohum şubesidir: koordinatlı ve e-postalı (geliştirme tohumu). */
function bolum(page: Page, ad: string) {
  return page.getByTestId('contact-branch').filter({ hasText: ad })
}

test.beforeEach(() => {
  test.skip(
    test.info().project.name !== 'masaustu',
    'Şubeler global; testler tek profilde sırayla.',
  )
})

test.afterEach(async ({ page }) => {
  await page.unrouteAll({ behavior: 'ignoreErrors' })
  if (acilanlar.length > 0) {
    await kapat(page.request)
  }
})

// ----- Adres ve durumlar (D2, D4) -----

test('0028/AC-1, AC-2, AC-43: `/{l}/contact` tek `<h1>`, ar sağdan sola; `/tr/contact/x` 404 ve "bulunamadı"', async ({
  page,
}) => {
  for (const [dil, baslik] of [
    ['tr', 'İletişim'],
    ['ar', 'اتصل بنا'],
    ['en', 'Contact'],
  ] as const) {
    const yanit = await page.goto(`/${dil}/contact`)
    expect(yanit!.status()).toBe(200)
    await expect(page.getByRole('heading', { level: 1 })).toHaveText(baslik)
    await expect(page.getByRole('heading', { level: 1 })).toHaveCount(1)
    await expect(page.getByTestId('contact-branch').first()).toBeVisible()
    if (dil === 'ar') {
      expect(await page.evaluate(() => document.documentElement.dir)).toBe('rtl')
      const telefon = page.getByTestId('branch-phone').first()
      expect(await telefon.evaluate((el) => getComputedStyle(el).direction)).toBe('ltr')
      await expect(telefon).toHaveText(/^\+[0-9]+$/)
    }
  }

  for (const adres of ['/tr/contact/x', '/tr/contact/kadikoy']) {
    expect((await page.goto(adres))!.status(), adres).toBe(404)
    await expect(page.getByTestId('content-not-found')).toBeVisible()
  }
})

test('0028/AC-38, AC-39, AC-40: okuma sürerken `<h1>` ve `role="status"` iskelet; 503\'te çevrilmiş hata ve "Tekrar dene" (bir tekrar okuma); şube yoksa boş durum', async ({
  page,
}) => {
  let birak: () => void = () => {}
  const bekle = new Promise<void>((resolve) => (birak = resolve))
  let okuma = 0
  await page.route('**/api/dealership/gallery**', async (route) => {
    okuma += 1
    if (okuma === 1) {
      await bekle
      return route.fulfill({ status: 503, json: { errorCode: 'service.unavailable' } })
    }
    return route.fallback()
  })
  await page.goto('/tr/contact')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('İletişim')
  await expect(page.getByTestId('contact-skeleton')).toHaveAttribute('role', 'status')
  await kanit(page, '.contact-page', '0028-ac38.png')
  birak()

  const hata = page.getByTestId('contact-error')
  await expect(hata).toContainText('İletişim bilgileri yüklenemedi')
  await kanit(page, hata, '0028-ac39.png')
  await hata.getByRole('button', { name: 'Tekrar dene' }).click()
  await expect(page.getByTestId('contact-branch').first()).toBeVisible()
  expect(okuma).toBe(2)

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await galeriyiDegistir(page, (galeri) => (galeri.branches = []))
  await page.goto('/tr/contact')
  await expect(page.getByTestId('contact-empty')).toContainText('İletişim bilgisi henüz eklenmedi')
  await expect(page.getByTestId('contact-branch')).toHaveCount(0)
  await expect(page.getByTestId('map-box')).toHaveCount(0)
  await kanit(page, page.getByTestId('contact-empty'), '0028-ac40.png')
})

// ----- Şube bölümleri (D3, D8, D11) -----

test('0028/AC-32, AC-33, AC-34, AC-35, AC-36, AC-37: iki bölüm yanıt sırasıyla; programlı, koordinatlı, e-postalı şubede tam sıra; e-postasız ve programsız şubede o satırlar yok', async ({
  page,
}) => {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  const ad = benzersiz('İletişim Tam')
  const yalin = benzersiz('İletişim Yalın')
  await subeAc(page.request, ad, {
    email: 'tam@otogaleri.com.tr',
    latitude: 40.98765,
    longitude: 29.0275,
    openingHours: PROGRAMLI,
  })
  await subeAc(page.request, yalin, {})
  // Pencerenin içinde bir özel gün: bugünden 3 gün sonra.
  const galeri = (await (
    await page.request.get('/api/dealership/gallery?locale=tr')
  ).json()) as Galeri
  const bugun = galeri.branches.find((b) => b.name === ad)!.openNow!.today
  const [y, a, g] = bugun.split('-').map(Number)
  const gun = new Date(Date.UTC(y!, a! - 1, g! + 3)).toISOString().slice(0, 10)
  const id = acilanlar[0]!
  const govde = (await (await page.request.get(`${API}/${id}`)).json()) as Record<string, unknown>
  expect(
    (
      await page.request.put(`${API}/${id}`, {
        data: { ...govde, specialHours: [{ from: gun, intervals: [] }] },
      })
    ).status(),
  ).toBe(200)

  await page.goto('/tr/contact')
  const sira = galeri.branches.map((b) => b.name)
  await expect(page.getByTestId('contact-branch').locator('h2')).toHaveText(sira)

  const tam = bolum(page, ad)
  await expect(tam.getByTestId('branch-hours')).toBeVisible()
  expect(
    await tam.evaluate((el) =>
      [
        ...el.querySelectorAll(
          'h2, [data-testid="branch-now"], .contact-branch__address, [data-testid="branch-phone"], [data-testid="branch-email"], [data-testid="map-box"], [data-testid="map-link"], [data-testid="branch-hours"], [data-testid="branch-special"]',
        ),
      ].map((oge) => oge.getAttribute('data-testid') ?? oge.tagName),
    ),
  ).toEqual([
    'H2',
    'branch-now',
    'P',
    'branch-phone',
    'branch-email',
    'map-box',
    'map-link',
    'branch-hours',
    'branch-special',
  ])
  await expect(tam.getByTestId('branch-phone')).toHaveAttribute('href', 'tel:+902161230000')
  await expect(tam.getByTestId('branch-email')).toHaveAttribute(
    'href',
    'mailto:tam@otogaleri.com.tr',
  )
  await expect(tam.getByTestId('branch-email')).toHaveText('tam@otogaleri.com.tr')
  await expect(tam.getByTestId('branch-email')).toHaveAttribute('dir', 'ltr')
  await kanit(page, tam, '0028-ac33.png')

  const sade = bolum(page, yalin)
  await expect(sade.locator('a[href^="mailto:"]')).toHaveCount(0)
  for (const yok of ['branch-now', 'branch-hours', 'branch-special']) {
    await expect(sade.getByTestId(yok)).toHaveCount(0)
  }
  await expect(sade.getByTestId('map-link')).toHaveText("Google Haritalar'da aç")
  await kanit(page, page.locator('.contact-page'), '0028-ac32.png')

  await page.goto('/ar/contact')
  await expect(bolum(page, `فرع ${ad}`).getByTestId('branch-hours')).toBeVisible()
  await kanit(page, page.locator('.contact-page'), '0028-ac43-ar.png')
})

test("0028/AC-41, AC-42: 320 px'te tr/ar/en yatay kayma yok; 1280 px'te harita kutusu bilgi sütununun satır sonunda (tr/en sağda, ar solda)", async ({
  page,
}) => {
  for (const dil of ['tr', 'ar', 'en'] as const) {
    await page.setViewportSize({ width: 320, height: 800 })
    await page.goto(`/${dil}/contact`)
    await expect(page.getByTestId('contact-branch').first()).toBeVisible()
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      ),
      `${dil} 320`,
    ).toBe(true)
    if (dil === 'ar') {
      await kanit(page, page.locator('.contact-page'), '0028-ac41-ar-320.png')
    }

    await page.setViewportSize({ width: 1280, height: 900 })
    const bilgi = (await page.locator('.contact-branch__info').first().boundingBox())!
    const kutu = (await page.getByTestId('map-box').first().boundingBox())!
    if (dil === 'ar') {
      expect(kutu.x + kutu.width, dil).toBeLessThanOrEqual(bilgi.x + 1)
    } else {
      expect(kutu.x, dil).toBeGreaterThanOrEqual(bilgi.x + bilgi.width - 1)
    }
  }
  await kanit(page, page.locator('.contact-page'), '0028-ac42-en-1280.png')
})

test('0028/AC-41, QA 2 BULGU-2: boşluksuz 300 karakterlik adres iletişim sayfasını ve altlığı yatay kaydırmaz (320 ve 1280 px, tr/en/ar; her öğe görünüm alanının içinde)', async ({
  page,
}) => {
  const adres = '–'.repeat(300)
  await galeriyiDegistir(page, (galeri) => {
    galeri.branches[0]!.address = adres
  })
  // ar'da `scrollWidth` sola taşmayı saymaz: ölçü her öğenin kutusunun görünüm alanında kalmasıdır.
  const tasanlar = (kapsam: string) =>
    page.evaluate((secici) => {
      const genislik = document.documentElement.clientWidth
      return [...document.querySelectorAll(`${secici} *`)]
        .filter((el) => {
          const r = el.getBoundingClientRect()
          // Kutu ya da içeriği taşar: metin kutusundan dışarı akınca kutu yerinde kalır (scrollWidth yakalar).
          return (
            r.width > 0 &&
            (r.left < -1 || r.right > genislik + 1 || el.scrollWidth > el.clientWidth + 1)
          )
        })
        .map((el) => el.className || el.tagName)
    }, kapsam)
  for (const dil of ['tr', 'en', 'ar'] as const) {
    for (const genislik of [320, 1280]) {
      await page.setViewportSize({ width: genislik, height: 900 })
      await page.goto(`/${dil}/contact`)
      await expect(page.getByText(adres).first()).toBeVisible()
      expect(await tasanlar('.contact-page'), `iletişim ${dil} ${genislik}`).toEqual([])

      await page.goto(`/${dil}/`)
      await expect(page.getByTestId('branch-list')).toBeVisible()
      expect(await tasanlar('[data-testid="home-footer"]'), `altlık ${dil} ${genislik}`).toEqual([])
      expect(
        await page.evaluate(
          () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
        ),
        `altlık ${dil} ${genislik}`,
      ).toBe(true)
    }
  }
})

// ----- Tıkla-yükle harita (D6, D15, D16) -----

test('0028/AC-46, AC-47, AC-53, AC-55 (anahtarlı): kutu 4:3 (±1 px), yer tutucuda düğme ve bilgi metni, iframe ve görsel yok; tıklamadan önce ve sonra yükseklik aynı (320 ve 1280 px)', async ({
  page,
  context,
}) => {
  await googleTaslagi(context)
  for (const genislik of [320, 1280]) {
    await page.setViewportSize({ width: genislik, height: 900 })
    await page.goto('/tr/contact')
    const kutu = page.getByTestId('map-box').first()
    await expect(kutu.getByRole('button', { name: 'Haritayı göster' })).toBeVisible()
    await expect(kutu).toContainText(
      "Harita Google Haritalar'dan yüklenir. Google çerez kullanabilir.",
    )
    expect(await kutu.locator('iframe, img').count()).toBe(0)
    const once = (await kutu.boundingBox())!
    expect(
      Math.abs(once.width * 3 - once.height * 4),
      `${genislik}: ${once.width}×${once.height}`,
    ).toBeLessThanOrEqual(4)
    if (genislik === 1280) {
      await kanit(page, page.getByTestId('contact-branch').first(), '0028-ac46.png')
    }

    await kutu.getByRole('button').click()
    await expect(kutu.getByTestId('map-frame')).toBeVisible()
    const sonra = (await kutu.boundingBox())!
    expect(Math.abs(sonra.height - once.height), String(genislik)).toBeLessThanOrEqual(1)
  }
})

test('0028/AC-48, AC-50, AC-51, AC-52, AC-54: Enter ve Space iframe\'i açar; iframe öznitelikleri tam; başlık şube adını taşır; odak iframe\'de; yalnız o şubenin kutusu açılır; "daha büyük haritada aç" yeni sekme açar', async ({
  page,
  context,
}) => {
  await googleTaslagi(context)
  const galeri = await gitVeGaleri(page, '/tr/contact')
  const [ilk, ikinci] = [
    page.getByTestId('contact-branch').nth(0),
    page.getByTestId('contact-branch').nth(1),
  ]
  const ad = (await ilk.locator('h2').textContent())!
  const embed = galeri.branches.find((b) => b.name === ad)!.map!.embedUrl!

  const dugme = ilk.getByRole('button', { name: 'Haritayı göster' })
  await expect(dugme).toHaveAttribute('type', 'button')
  await dugme.focus()
  await page.keyboard.press('Enter')
  const cerceve = ilk.getByTestId('map-frame')
  await expect(cerceve).toBeVisible()
  expect(await page.evaluate(() => document.activeElement?.tagName)).toBe('IFRAME')
  await expect(cerceve).toHaveAttribute('src', embed)
  await expect(cerceve).toHaveAttribute('referrerpolicy', 'strict-origin-when-cross-origin')
  await expect(cerceve).toHaveAttribute('title', `${ad} — Google Haritalar haritası`)
  for (const yok of ['sandbox', 'allow', 'allowfullscreen', 'loading']) {
    expect(await cerceve.getAttribute(yok), yok).toBeNull()
  }
  await expect(ikinci.getByTestId('map-frame')).toHaveCount(0)
  await expect(ikinci.getByRole('button', { name: 'Haritayı göster' })).toBeVisible()
  await kanit(page, ilk, '0028-ac50.png')

  // Space — ikinci şubenin düğmesi.
  await ikinci.getByRole('button').focus()
  await page.keyboard.press('Space')
  await expect(ikinci.getByTestId('map-frame')).toBeVisible()

  // AC-50 — "daha büyük haritada aç" engellenmez: taslak sayfadaki `target="_blank"` yeni sekme açar.
  const sekme = context.waitForEvent('page')
  await page.frameLocator('[data-testid="map-frame"] >> nth=0').locator('#buyuk').click()
  expect((await sekme).url()).toBe('https://www.google.com/maps/place/taslak')

  await page.goto('/en/contact')
  await page.getByTestId('map-box').first().getByRole('button').click()
  await expect(page.getByTestId('map-frame').first()).toHaveAttribute('title', /— Google Maps map$/)
})

/**
 * QA 2 BULGU-1 · AC-49 (AP-08 revizyonu) — sitenin ve kurulumun KENDİ kökenleri: belgenin kökeni ve medya
 * kovasının kökeni. Kova kökeni sabit yazılmaz: Host'un `Media__PublicBaseUrl`'i (`ci.yml`'da tek kaynak,
 * yerelde geliştirme betiği) `E2E_MEDIA_PUBLIC_BASE_URL` olarak verilir. Logo ve ilan görselleri oradan gelir.
 */
function medyaKoku(): string {
  const medya = process.env['E2E_MEDIA_PUBLIC_BASE_URL']
  expect(medya, "E2E_MEDIA_PUBLIC_BASE_URL (Host'un Media__PublicBaseUrl'i) verilmeli").toBeTruthy()
  return medya!
}

/** Logolu kurulum: belge medya kovasındaki bir logoyu ilan eder (paralel `galeri-logosu`'nun durumu). */
async function kovadanLogo(page: Page, medya: string) {
  const logo = new URL('public/assets/e2e-iletisim/logo-small.png', medya).href
  await page.route(logo, (route) =>
    route.fulfill({ contentType: 'image/png', body: pngUret(320, 80) }),
  )
  await page.route(/\/(tr|ar|en)\/contact$/, async (route) => {
    const yanit = await route.fetch()
    const html = (await yanit.text()).replace(
      '</head>',
      `<meta name="gallery-logo" content="${logo}" data-width="320" data-height="80" /></head>`,
    )
    return route.fulfill({ response: yanit, body: html })
  })
}

for (const logolu of [false, true]) {
  test(`0028/AC-49, AC-84, QA 2 BULGU-1 (${logolu ? 'logolu: logo medya kovasından' : 'logosuz'}): düğmeden önce (sona kaydırılıp 5 sn) sitenin ve kurulumun medya kökeni dışına istek 0, preconnect/dns-prefetch yok; basınca ilk dış istek embedUrl'e giden iframe gezinmesi; dış her istek iframe'in çerçevesinden`, async ({
    page,
    context,
  }) => {
    await googleTaslagi(context)
    const medya = medyaKoku()
    if (logolu) {
      await kovadanLogo(page, medya)
    }
    const istekler: { url: string; anaCerceve: boolean; gezinme: boolean }[] = []
    context.on('request', (istek: Request) => {
      istekler.push({
        url: istek.url(),
        anaCerceve: istek.frame() === page.mainFrame(),
        gezinme: istek.isNavigationRequest(),
      })
    })
    const galeri = await gitVeGaleri(page, '/tr/contact')
    await expect(page.getByTestId('map-box').first()).toBeVisible()
    await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight))
    await page.waitForTimeout(5000)

    const kendi = new Set([koken(page), new URL(medya).origin])
    const disari = () => istekler.filter((i) => !kendi.has(new URL(i.url).origin))
    if (logolu) {
      // Durum gerçekten kuruldu: logo kovadan istendi ve "kendi" sayıldı.
      expect(istekler.some((i) => new URL(i.url).origin === new URL(medya).origin)).toBe(true)
    }
    expect(disari()).toEqual([])
    expect(await page.locator('link[rel="preconnect"], link[rel="dns-prefetch"]').count()).toBe(0)

    const ad = (await page.getByTestId('contact-branch').first().locator('h2').textContent())!
    const embed = galeri.branches.find((b) => b.name === ad)!.map!.embedUrl!

    await page.getByTestId('map-box').first().getByRole('button').click()
    await expect(page.getByTestId('map-frame')).toBeVisible()
    await page.waitForTimeout(1000)

    const sonra = disari()
    expect(sonra.length).toBeGreaterThan(0)
    expect(sonra[0]).toEqual({ url: embed, anaCerceve: false, gezinme: true })
    expect(sonra.filter((i) => i.anaCerceve)).toEqual([])
  })
}

test('0028/AC-55, AC-56, AC-81 (anahtarsız yarı): `embedUrl` yokken kutu ve düğme yok, "Google Haritalar\'da aç" var; `href` = `linkUrl`, `target` yok; tr/ar/en çevrili', async ({
  page,
}) => {
  // Anahtarsız sunucunun şekli: `map`'in anahtar kümesi tam `linkUrl` (`embedUrl` anahtarı HİÇ yok).
  // Bu, Host testinin assert ettiği kümedir (`IletisimZinciriTests.Okuma_Anahtarsiz`, AC-28/AC-81);
  // "ağdaki yanıtta `embedUrl` dizesi geçmez" sunucu yarısı orada, gerçek anahtarsız kurulumda ölçülür.
  await galeriyiDegistir(page, (galeri) => {
    for (const sube of galeri.branches) {
      if (sube.map !== null) {
        sube.map = { linkUrl: sube.map.linkUrl }
      }
    }
  })
  for (const [dil, metin] of [
    ['tr', "Google Haritalar'da aç"],
    ['ar', 'افتح في خرائط Google'],
    ['en', 'Open in Google Maps'],
  ] as const) {
    await page.goto(`/${dil}/contact`)
    await expect(page.getByTestId('contact-branch').first()).toBeVisible()
    await expect(page.getByTestId('map-box')).toHaveCount(0)
    await expect(
      page.getByRole('button', { name: /Haritayı göster|اعرض الخريطة|Show map/u }),
    ).toHaveCount(0)
    const baglantilar = page.getByTestId('map-link')
    // Şubeler global (başka spec'ler aynı anda açabilir): sayı ayrı okumadan değil, basılan bölümlerden.
    await expect(baglantilar).toHaveCount(await page.getByTestId('contact-branch').count())
    await expect(baglantilar.first()).toHaveText(metin)
    expect(await baglantilar.first().getAttribute('target')).toBeNull()
  }
  const galeri = await gitVeGaleri(page, '/tr/contact')
  const ilk = page.getByTestId('contact-branch').first()
  const ad = (await ilk.locator('h2').textContent())!
  await expect(ilk.getByTestId('map-link')).toHaveAttribute(
    'href',
    galeri.branches.find((b) => b.name === ad)!.map!.linkUrl,
  )
  await kanit(page, ilk, '0028-ac55.png')
})

test('0028/AC-57: haritayı açıp yenileyince kutu yine yer tutucuda; tıklama `localStorage`, `sessionStorage` ve çereze yazılmaz', async ({
  page,
  context,
}) => {
  await googleTaslagi(context)
  await page.goto('/tr/contact')
  const depo = () =>
    page.evaluate(() => [
      JSON.stringify({ ...localStorage }),
      JSON.stringify({ ...sessionStorage }),
    ])
  const onceDepo = await depo()
  const onceCerez = await context.cookies()

  await page.getByTestId('map-box').first().getByRole('button').click()
  await expect(page.getByTestId('map-frame')).toBeVisible()
  expect(await depo()).toEqual(onceDepo)
  expect(await context.cookies(koken(page))).toEqual(
    onceCerez.filter((c) => c.domain.includes('localhost')),
  )

  await page.reload()
  await expect(page.getByTestId('map-box').first().getByRole('button')).toBeVisible()
  await expect(page.getByTestId('map-frame')).toHaveCount(0)
})

test('0028/AC-90: harita açıkken D9 yenilemesi gerçekleştikten sonra 5 sn içinde gömme adresine ikinci belge isteği yok; iframe yerinde', async ({
  page,
  context,
}) => {
  await googleTaslagi(context)
  // İlk yanıt 2 sn'lik yenileme süresi taşır (programsız tohum şubesine programlı bir "şu an").
  let okuma = 0
  await galeriyiDegistir(page, (galeri) => {
    okuma += 1
    const sube = galeri.branches[0]!
    sube.openingHours = PROGRAMLI
    sube.openNow = {
      open: true,
      today: '2030-01-07',
      next: { date: '2030-01-07', time: '18:00' },
      refreshInSeconds: okuma === 1 ? 2 : 3600,
    }
  })
  const gomme: string[] = []
  context.on('request', (istek) => {
    if (istek.url().includes('google.com/maps/embed')) gomme.push(istek.url())
  })
  await page.goto('/tr/contact')
  await page.getByTestId('map-box').first().getByRole('button').click()
  const cerceve = page.getByTestId('map-frame')
  await expect(cerceve).toBeVisible()
  await cerceve.evaluate((el) => ((el as unknown as { isaret: number }).isaret = 7))

  await expect.poll(() => okuma, { timeout: 10_000 }).toBe(2)
  await page.waitForTimeout(5000)
  expect(gomme).toHaveLength(1)
  expect(await cerceve.evaluate((el) => (el as unknown as { isaret?: number }).isaret)).toBe(7)
})

test('0028/AC-45, AC-95: `/tr/contact` açılışında `branchClock-*` isteği yok; parça 2 sn bekletilirken galeri okuması parçanın yanıtı bitmeden başlar', async ({
  page,
}) => {
  const olaylar: { url: string; tur: 'basla' | 'bitti'; an: number }[] = []
  page.on('request', (istek) => olaylar.push({ url: istek.url(), tur: 'basla', an: Date.now() }))
  page.on('requestfinished', (istek) =>
    olaylar.push({ url: istek.url(), tur: 'bitti', an: Date.now() }),
  )
  await page.route(/\/assets\/contactPage-[^/]+\.js$/, async (route) => {
    await new Promise((resolve) => setTimeout(resolve, 2000))
    await route.continue()
  })
  await page.goto('/tr/contact')
  await expect(page.getByTestId('contact-branch').first()).toBeVisible()

  expect(olaylar.filter((o) => /branchClock-/.test(o.url))).toEqual([])
  const galeri = olaylar.find(
    (o) => o.tur === 'basla' && o.url.includes('/api/dealership/gallery'),
  )!
  const parca = olaylar.find((o) => o.tur === 'bitti' && /contactPage-[^/]+\.js$/.test(o.url))!
  expect(galeri.an).toBeLessThan(parca.an - 1000)
})

test("0028/AC-77 (AP-08): fontlar bekletilip serbest bırakılınca 320 ve 1280 px'te, tr/ar/en'de şube bölümlerinde ve harita kutusunda layout-shift kaynağı yok (bölümler yüzler yüklenince basılır)", async ({
  page,
}) => {
  await page.addInitScript(() => {
    const w = window as unknown as { kaymalar: string[] }
    w.kaymalar = []
    new PerformanceObserver((liste) => {
      for (const girdi of liste.getEntries() as unknown as { sources: { node: Node | null }[] }[]) {
        for (const kaynak of girdi.sources) {
          const el = kaynak.node instanceof Element ? kaynak.node : kaynak.node?.parentElement
          if (el?.closest('[data-testid="contact-branch"], [data-testid="map-box"]')) {
            w.kaymalar.push(el.className || el.tagName)
          }
        }
      }
    }).observe({ type: 'layout-shift', buffered: true })
  })
  for (const dil of ['tr', 'ar', 'en'] as const) {
    for (const genislik of [320, 1280]) {
      await page.setViewportSize({ width: genislik, height: 900 })
      await page.unroute('**/*.woff2')
      const fontuBirak = await fontlariBeklet(page)
      await page.goto(`/${dil}/contact`, { waitUntil: 'domcontentloaded' })
      await expect(page.getByTestId('contact-skeleton')).toBeVisible()
      expect(await page.evaluate(() => document.fonts.status)).toBe('loading')
      // Yüzler bekletilirken bölümler basılmaz (üst süre 1500 ms dolmadan).
      await expect(page.getByTestId('contact-branch')).toHaveCount(0)
      await fontuBirak()
      await expect(page.getByTestId('contact-branch').first()).toBeVisible()
      await page.evaluate(() => window.scrollTo(0, document.documentElement.scrollHeight))
      await page.waitForTimeout(500)
      expect(
        await page.evaluate(() => (window as unknown as { kaymalar: string[] }).kaymalar),
        `${dil} ${genislik}`,
      ).toEqual([])
    }
  }
})

test('0028/AC-77 (AP-08 şartı 3): font istekleri başarısız olunca (abort) bölümler beklemeden basılır', async ({
  page,
}) => {
  // İki biçim de kesilir: yalnız `.woff2` kesilince tarayıcı `.woff` kaynağına düşüyor ve yüz yükleniyor
  // (ölçüldü). Yalnız bekleme kapısının 1500 ms'lik zamanlayıcısı etkisiz kılınır (sayfanın öbür
  // zamanlayıcıları çalışır): bölümler yine basılıyorsa bekleme font hatasıyla bitmiştir, süreyle değil.
  await page.route(/\.woff2?$/, (route) => route.abort('internetdisconnected'))
  await page.addInitScript(() => {
    const ozgun = window.setTimeout
    window.setTimeout = ((isleyici: TimerHandler, ms?: number, ...ek: unknown[]) =>
      ms === 1500 ? 0 : ozgun(isleyici, ms, ...ek)) as typeof window.setTimeout
  })
  await page.goto('/tr/contact', { waitUntil: 'domcontentloaded' })
  await expect(page.getByTestId('contact-branch').first()).toBeVisible()
})

// ----- Anahtarın gizliliği (PM şartı (a)) -----

test('0028/AC-82 (e2e ikizi): sahte anahtar galeri okumasında var, `/tr/`, `/tr/cars`, görünür bir ilanın detayı, `/tr/contact`, `/ar/contact` ve 404 belgesinde YOK', async ({
  page,
}) => {
  // Kurulumun anahtarlı olduğu ölçülür: anahtar yalnız gömme adresinde (ölçü boş bir kümede yapılmıyor).
  expect(await (await page.request.get('/api/dealership/gallery?locale=tr')).text()).toContain(
    ANAHTAR,
  )
  const liste = (await (
    await page.request.get('/api/catalog/showcase-listings?pageSize=1&locale=tr')
  ).json()) as { items: { slug: string }[] }
  for (const adres of [
    '/tr/',
    '/tr/cars',
    `/tr/cars/${liste.items[0]!.slug}`,
    '/tr/contact',
    '/ar/contact',
    '/tr/yok/boyle/bir/yol',
  ]) {
    const yanit = await page.request.get(adres)
    expect(await yanit.text(), adres).not.toContain(ANAHTAR)
    expect(JSON.stringify(yanit.headers()), adres).not.toContain(ANAHTAR)
  }
})

// ----- Altlık (D9, D17) -----

test('0028/AC-58, AC-59, AC-60, AC-61, AC-63: altlıkta şube öğesi ad/adres/telefon; bloğun sonunda tek bağlantı; ana sayfa saat ya da iletişim parçasını istemez ve galeriyi yenilemez; `id="contact"` yok', async ({
  page,
}) => {
  let okuma = 0
  await galeriyiDegistir(page, (galeri) => {
    okuma += 1
    const sube = galeri.branches[0]!
    sube.openingHours = PROGRAMLI
    sube.openNow = { open: true, today: '2030-01-07', next: null, refreshInSeconds: 2 }
  })
  const parcalar: string[] = []
  page.on('request', (istek) => {
    if (/\/assets\/(branchClock|contactPage)-/.test(istek.url())) parcalar.push(istek.url())
  })
  await page.goto('/tr/')
  const blok = page.getByTestId('branch-list')
  await expect(blok).toBeVisible()
  for (const oge of await page.getByTestId('branch').all()) {
    expect(await oge.evaluate((li) => [...li.children].map((el) => el.tagName))).toEqual([
      'P',
      'P',
      'A',
    ])
  }
  for (const yok of ['branch-now', 'branch-hours', 'branch-special', 'branch-hours-skeleton']) {
    await expect(page.getByTestId(yok)).toHaveCount(0)
  }
  const baglanti = blok.getByTestId('footer-contact-link')
  await expect(baglanti).toHaveText('İletişim ve çalışma saatleri')
  await expect(baglanti).toHaveAttribute('href', '/tr/contact')
  await expect(blok.getByRole('link', { name: 'İletişim ve çalışma saatleri' })).toHaveCount(1)
  await expect(page.locator('#contact')).toHaveCount(0)
  await kanit(page, page.getByTestId('home-footer'), '0028-ac58.png')

  await page.waitForTimeout(5000)
  expect(okuma).toBe(1)
  expect(parcalar).toEqual([])

  for (const [dil, metin] of [
    ['ar', 'التواصل وساعات العمل'],
    ['en', 'Contact and opening hours'],
  ] as const) {
    await page.goto(`/${dil}/`)
    await expect(page.getByTestId('footer-contact-link')).toHaveText(metin)
    await expect(page.getByTestId('footer-contact-link')).toHaveAttribute('href', `/${dil}/contact`)
  }
  await kanit(page, page.getByTestId('home-footer'), '0028-ac59-en.png')
})

test('0028/AC-62: galeri okuması hata verirse altlıkta şube bloğu ve bağlantısı yok', async ({
  page,
}) => {
  await page.route('**/api/dealership/gallery**', (route) =>
    route.fulfill({ status: 503, json: { errorCode: 'service.unavailable' } }),
  )
  await page.goto('/tr/')
  await expect(page.getByTestId('home-footer')).toBeVisible()
  await expect(page.getByTestId('branch-list')).toHaveCount(0)
  await expect(page.getByTestId('footer-contact-link')).toHaveCount(0)
})

test('0028/AC-75: liste ve görünür bir ilanın detayı iletişim parçasını istemez', async ({
  page,
}) => {
  const parcalar: string[] = []
  page.on('request', (istek) => {
    if (/\/assets\/contactPage-/.test(istek.url())) parcalar.push(istek.url())
  })
  await page.goto('/tr/cars')
  await page.locator('a[href^="/tr/cars/"]').first().click()
  await page.waitForLoadState('networkidle')
  expect(parcalar).toEqual([])
})

/** Beş bloklu altlık: logolu, iki şubeli, kurumsal sayfa ve sosyal hesap (gerçek yanıtlardan). */
async function besBlok(page: Page) {
  await page.route('**/logo-fikstur.png', (route) =>
    route.fulfill({ contentType: 'image/png', body: pngUret(320, 80) }),
  )
  await page.route(/\/(tr|ar|en)\/$/, async (route) => {
    const yanit = await route.fetch()
    const html = (await yanit.text()).replace(
      '</head>',
      '<meta name="gallery-logo" content="/logo-fikstur.png" data-width="320" data-height="80" /></head>',
    )
    return route.fulfill({ response: yanit, body: html })
  })
  await page.route('**/api/content/navigation**', (route) =>
    route.fulfill({ json: [{ systemKey: 'about', title: 'Hakkımızda', slug: 'hakkimizda' }] }),
  )
  await galeriyiDegistir(page, (galeri) => {
    galeri.socialLinks = [{ platform: 'instagram', url: 'https://www.instagram.com/otogaleri' }]
  })
}

const BLOKLAR = [
  '[data-testid="branch-list"]',
  '.home-footer__nav:not([data-testid])',
  '[data-testid="footer-corporate"]',
  '[data-testid="footer-social"]',
  '.home-footer__nav:last-child',
]

test("0028/AC-85, AC-86 (PM şartı (c)): 1280 px'te beş blok tek satırda (üst kenarlar ±1 px), şube bloğu satır başında (ar'da sağda), logo üstte; 320 px'te DOM sırasıyla tek sütun ve yatay kayma yok", async ({
  page,
}) => {
  await besBlok(page)
  for (const dil of ['tr', 'ar', 'en'] as const) {
    await page.setViewportSize({ width: 1280, height: 900 })
    await page.goto(`/${dil}/`)
    await expect(page.getByTestId('footer-social')).toBeVisible()
    const kutular = await Promise.all(
      BLOKLAR.map(
        async (secici) => (await page.locator(`.home-footer ${secici}`).first().boundingBox())!,
      ),
    )
    const ustler = kutular.map((k) => k.y)
    expect(Math.max(...ustler) - Math.min(...ustler), `${dil}: ${ustler}`).toBeLessThanOrEqual(1)
    const xler = kutular.map((k) => k.x)
    if (dil === 'ar') {
      expect(xler[0], dil).toBe(Math.max(...xler))
    } else {
      expect(xler[0], dil).toBe(Math.min(...xler))
    }
    const logo = (await page.getByTestId('footer-logo').boundingBox())!
    expect(logo.y + logo.height, dil).toBeLessThanOrEqual(Math.min(...ustler))
    await kanit(page, page.getByTestId('home-footer'), `0028-ac85-${dil}.png`)

    await page.setViewportSize({ width: 320, height: 800 })
    await page.evaluate(() => window.scrollTo(0, 0))
    const dar = await Promise.all(
      BLOKLAR.map(
        async (secici) => (await page.locator(`.home-footer ${secici}`).first().boundingBox())!,
      ),
    )
    for (let i = 1; i < dar.length; i += 1) {
      expect(dar[i]!.y, `${dil} 320: ${i}`).toBeGreaterThan(dar[i - 1]!.y)
    }
    expect(
      await page.evaluate(
        () => document.documentElement.scrollWidth <= document.documentElement.clientWidth,
      ),
    ).toBe(true)
    if (dil === 'ar') {
      await kanit(page, page.getByTestId('home-footer'), '0028-ac86-ar.png')
    }
  }
})

test("0028/AC-87: ana sayfa fontlar bekletilip serbest bırakılarak açılır ve altlığa kaydırılır; 320 ve 1280 px'te altlıkta ve bloklarında layout-shift kaynağı yok", async ({
  page,
}) => {
  await besBlok(page)
  await page.addInitScript(() => {
    const w = window as unknown as { kaymalar: string[] }
    w.kaymalar = []
    new PerformanceObserver((liste) => {
      for (const girdi of liste.getEntries() as unknown as { sources: { node: Node | null }[] }[]) {
        for (const kaynak of girdi.sources) {
          const el = kaynak.node instanceof Element ? kaynak.node : kaynak.node?.parentElement
          if (el?.closest('[data-testid="home-footer"]')) {
            w.kaymalar.push(el.className || el.tagName)
          }
        }
      }
    }).observe({ type: 'layout-shift', buffered: true })
  })
  for (const genislik of [320, 1280]) {
    await page.setViewportSize({ width: genislik, height: 900 })
    await page.unroute('**/*.woff2')
    const fontuBirak = await fontlariBeklet(page)
    await page.goto('/tr/', { waitUntil: 'domcontentloaded' })
    await expect(page.getByTestId('home-footer')).toBeAttached()
    expect(await page.evaluate(() => document.fonts.status)).toBe('loading')
    await fontuBirak()
    await page.getByTestId('home-footer').scrollIntoViewIfNeeded()
    await expect(page.getByTestId('footer-social')).toBeVisible()
    await page.waitForTimeout(500)
    expect(
      await page.evaluate(() => (window as unknown as { kaymalar: string[] }).kaymalar),
      String(genislik),
    ).toEqual([])
  }
})

test('0028/AC-72: boş vitrinde "Galeriye ulaşın" ana sayfada ve listede `/{l}/contact`\'a gider; metin değişmez', async ({
  page,
}) => {
  await page.route('**/api/catalog/showcase-listings*', (route) =>
    route.fulfill({ json: { items: [], page: 1, pageSize: 1, totalCount: 0, totalPages: 0 } }),
  )
  for (const adres of ['/tr/', '/tr/cars']) {
    await page.goto(adres)
    const baglanti = page.getByTestId('empty-showcase').getByRole('link')
    await expect(baglanti, adres).toHaveText('Galeriye ulaşın')
    await expect(baglanti, adres).toHaveAttribute('href', '/tr/contact')
  }
  await page.goto('/ar/cars')
  await expect(page.getByTestId('empty-showcase').getByRole('link')).toHaveAttribute(
    'href',
    '/ar/contact',
  )
})

// ----- Üst çubuk (D10) -----

test('0028/AC-64, AC-65, AC-69: bağlantı her vitrin sayfasında, adı "İletişim", hedefi `/tr/contact`; yalnız iletişim sayfasında `aria-current="page"`; panelde yok', async ({
  page,
}) => {
  const liste = (await (
    await page.request.get('/api/catalog/showcase-listings?pageSize=1&locale=tr')
  ).json()) as { items: { slug: string }[] }
  for (const adres of [
    '/tr/',
    '/tr/cars',
    `/tr/cars/${liste.items[0]!.slug}`,
    '/tr/kvkk',
    '/tr/favorites',
    '/tr/compare',
    '/tr/newsletter/confirm?token=x',
    '/tr/yok/boyle',
    '/tr/contact',
  ]) {
    await page.goto(adres)
    const baglanti = page.getByRole('banner').getByRole('link', { name: 'İletişim', exact: true })
    await expect(baglanti, adres).toHaveAttribute('href', '/tr/contact')
    expect(await baglanti.getAttribute('aria-current'), adres).toBe(
      adres === '/tr/contact' ? 'page' : null,
    )
  }

  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
  for (const adres of ['/admin', '/admin/branches', '/admin/gallery']) {
    await page.goto(adres)
    await expect(page.getByRole('navigation').first()).toBeVisible()
    await expect(page.locator('a[href$="/contact"]'), adres).toHaveCount(0)
  }
})

test("0028/AC-66, AC-67, AC-71: 1280 px'te metinli ve sayaçtan önce; 320 px'te 1. satırda yalnız simge, ad tam metin, kutu ≥ 44×44; ar'da sol uçta, tr'de sağ uçta", async ({
  browser,
}) => {
  for (const dil of ['tr', 'ar'] as const) {
    for (const genislik of [1280, 320]) {
      const baglam = await browser.newContext({ viewport: { width: genislik, height: 800 } })
      const page = await baglam.newPage()
      await page.addInitScript(() =>
        localStorage.setItem(
          'cargallery.favorites',
          JSON.stringify({
            v: 1,
            items: [{ slug: 'a-2020-000001', addedAt: 'x', modelName: 'A', modelYear: 2020 }],
          }),
        ),
      )
      await page.goto(`/${dil}/cars`)
      const ad = dil === 'tr' ? 'İletişim' : 'اتصل بنا'
      const baglanti = page.getByRole('banner').getByRole('link', { name: ad, exact: true })
      const kutu = (await baglanti.boundingBox())!
      const sayac = (await page.locator('.top-bar__favorites').boundingBox())!
      const marka = (await page.getByTestId('top-bar-brand').boundingBox())!
      const metin = page.locator('.top-bar__contact-text')
      const simge = page.locator('.top-bar__contact-icon')
      if (genislik === 1280) {
        await expect(metin).toBeVisible()
        await expect(simge).toBeHidden()
        expect(
          Math.abs(kutu.y + kutu.height / 2 - (sayac.y + sayac.height / 2)),
          dil,
        ).toBeLessThanOrEqual(2)
        // Sayaçtan önce: tr'de solunda, ar'da sağında.
        if (dil === 'tr') expect(kutu.x + kutu.width).toBeLessThanOrEqual(sayac.x + 1)
        else expect(kutu.x).toBeGreaterThanOrEqual(sayac.x + sayac.width - 1)
        await kanit(page, 'header.top-bar', `0028-ac66-${dil}.png`)
      } else {
        await expect(simge).toBeVisible()
        expect(await metin.evaluate((el) => el.getBoundingClientRect().width)).toBeLessThanOrEqual(
          1,
        )
        expect(kutu.width).toBeGreaterThanOrEqual(44)
        expect(kutu.height).toBeGreaterThanOrEqual(44)
        // 1. satırda: markayla aynı satır, sayaç/dil satırının üstünde.
        expect(kutu.y + kutu.height, dil).toBeLessThanOrEqual(sayac.y + 1)
        expect(kutu.y, dil).toBeLessThan(marka.y + marka.height)
        const cubuk = (await page.locator('.top-bar__inner').boundingBox())!
        if (dil === 'ar') expect(kutu.x - cubuk.x, dil).toBeLessThanOrEqual(17)
        else expect(cubuk.x + cubuk.width - (kutu.x + kutu.width), dil).toBeLessThanOrEqual(17)
        await kanit(page, 'header.top-bar', `0028-ac67-${dil}.png`)
      }
      await baglam.close()
    }
  }
})
