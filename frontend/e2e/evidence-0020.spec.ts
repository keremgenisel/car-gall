import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0020` — galeri profili ve şubeler, gerçek Host'a karşı: vitrinin üst çubuğu, panelin galeri
 * ve şubeler ekranları, araç formunun şube seçicisi. Görsel kanıt `npm run evidence` ile
 * yazılır (docs/testing.md § 3); assert'ler her koşuda çalışır.
 *
 * <b>Paylaşılan durum.</b> Galeri profili GLOBALDİR ve `kabuk.spec.ts` Türkçe üst çubukta
 * kurulumun adını ("Oto Galeri") bekliyor. Bu dosya yalnız İNGİLİZCE görünen adı değiştirir
 * ve her testin sonunda geri yazar; Türkçe ad ve ticari unvan hiç değişmez. Şubeler bu dosyanın
 * kendi açtıklarıdır; kurulumun iki şubesine dokunulmaz.
 *
 * <b>Yetkisiz rol ve boş, hata, yükleniyor hâlleri</b> gerçek veriyle kurulamadığı için API
 * yanıtı araya girilerek kurulur (`uc-durum.spec.ts` ve `evidence-0014.spec.ts` deseni);
 * ekranın kendisi gerçek bileşendir. <b>Arapça panel</b> personelin tercihinden basılır
 * (`0006` R16) ve kurulum Yöneticisinin tercihi Türkçedir; Arapça görüntü için panel
 * belgesinin `<html lang dir>`'i yanıt araya girilerek `ar`/`rtl` yapılır — panelin dili
 * oradan okunur (`main.tsx`), ekranın kendisi yine gerçek bileşendir.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const API = '/api/admin'
const OZEL_AD = '<b>Galeri & "Oto"</b>'

interface Galeri {
  readonly commercialName: string
  readonly taxNumber: string | null
  readonly tradeRegistryNumber: string | null
  readonly displayNames: Record<string, string>
  readonly version: number
}

interface Sube {
  readonly branchId: string
  readonly version: number
  readonly status: string
  readonly slug: string
}

function benzersiz(onek: string) {
  return `${onek}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`
}

async function galeri(api: APIRequestContext): Promise<Galeri> {
  const yanit = await api.get(`${API}/gallery`)
  expect(yanit.ok(), 'GET gallery').toBe(true)
  return (await yanit.json()) as Galeri
}

/** Yalnız İngilizce görünen adı yazar; `null` İngilizce adı kaldırır. */
async function ingilizceAd(api: APIRequestContext, ad: string | null) {
  const simdiki = await galeri(api)
  const adlar = { ...simdiki.displayNames }

  if (ad === null) {
    delete adlar['en']
  } else {
    adlar['en'] = ad
  }

  const yanit = await api.put(`${API}/gallery`, {
    data: {
      version: simdiki.version,
      commercialName: simdiki.commercialName,
      taxNumber: simdiki.taxNumber ?? '',
      tradeRegistryNumber: simdiki.tradeRegistryNumber ?? '',
      displayNames: adlar,
    },
  })
  expect(yanit.ok(), `PUT gallery → ${yanit.status()} ${await yanit.text()}`).toBe(true)
}

async function subeAc(
  api: APIRequestContext,
  ad: string,
  telefon = '+902161230000',
): Promise<Sube> {
  const slug = benzersiz('e2e-sube')
  const yanit = await api.post(`${API}/branches`, {
    data: {
      names: { tr: ad, ar: `فرع ${ad}` },
      addresses: { tr: `${ad} Caddesi 1` },
      phone: telefon,
      timeZone: 'Europe/Istanbul',
      slug,
    },
  })
  expect(yanit.status(), `POST branch → ${await yanit.text()}`).toBe(201)
  const { id } = (await yanit.json()) as { id: string }
  return sube(api, id)
}

async function sube(api: APIRequestContext, id: string): Promise<Sube> {
  const yanit = await api.get(`${API}/branches/${id}`)
  expect(yanit.ok()).toBe(true)
  return (await yanit.json()) as Sube
}

async function eylem(
  api: APIRequestContext,
  id: string,
  ad: 'close-temporarily' | 'reopen' | 'close',
) {
  const { version } = await sube(api, id)
  const yanit = await api.post(`${API}/branches/${id}/${ad}`, { data: { version } })
  expect(yanit.ok(), `${ad} → ${yanit.status()} ${await yanit.text()}`).toBe(true)
}

/** Panel belgesini Arapça basılmış gibi döndürür (dosya yorumundaki gerekçe). */
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

/** `/me` yanıtını verilen rolle döndürür: yetkisiz rolün gördüğü ekran için. */
async function rol(page: Page, role: string) {
  await page.route('**/api/admin/me', async (route) => {
    if (route.request().method() !== 'GET') {
      return route.fallback()
    }

    const yanit = await route.fetch()
    return route.fulfill({ response: yanit, json: { ...(await yanit.json()), role } })
  })
}

test.beforeEach(async ({ page }, testInfo) => {
  test.skip(
    testInfo.project.name !== 'masaustu',
    'Galeri profili global; testler tek profilde sırayla koşar.',
  )
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
})

test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.project.name === 'masaustu') {
    await page.unrouteAll({ behavior: 'ignoreErrors' })
    await ingilizceAd(page.request, null)
  }
})

// ----- Vitrin -----

test('0020/AC-9: dört vitrin sayfasında üst çubuk görünen adı gösterir', async ({ page }) => {
  const icerik = await page.request.post('/api/admin/content/pages', {
    data: {
      locales: [
        {
          locale: 'tr',
          title: 'Galeri Notu',
          body: 'Bir içerik sayfası.',
          slug: benzersiz('galeri-notu'),
        },
      ],
    },
  })
  expect(icerik.status()).toBe(201)
  const { id } = (await icerik.json()) as { id: string }
  const sayfa = (await (await page.request.get(`/api/admin/content/pages/${id}`)).json()) as {
    version: number
    locales: { locale: string; slug: string }[]
  }
  await page.request.post(`/api/admin/content/pages/${id}/publish`, {
    data: { version: sayfa.version, locale: 'tr' },
  })
  const icerikSlug = sayfa.locales.find((item) => item.locale === 'tr')!.slug

  const liste = (await (
    await page.request.get('/api/catalog/showcase-listings?locale=tr&page=1&pageSize=1')
  ).json()) as {
    items: { slug: string }[]
  }

  const adresler: [string, string][] = [
    ['/tr/', 'a'],
    ['/tr/cars', 'b'],
    [`/tr/cars/${liste.items[0]!.slug}`, 'c'],
    [`/tr/${icerikSlug}`, 'd'],
  ]

  for (const [adres, ek] of adresler) {
    await page.goto(adres)
    await expect(page.getByTestId('top-bar-brand')).toHaveText('Oto Galeri')
    await kanit(page, 'header.top-bar', `0020-ac9-${ek}.png`)
  }

  await page.request.delete(`/api/admin/content/pages/${id}?version=${sayfa.version + 1}`)
})

test('0020/AC-10, AC-14: İngilizce ad yokken /en/ varsayılan dildeki adı; kaydedilince ilk sayfa yeni adı gösterir', async ({
  page,
}) => {
  await ingilizceAd(page.request, null)
  await page.goto('/en/cars')
  await expect(page.getByTestId('top-bar-brand')).toHaveText('Oto Galeri')
  await kanit(page, 'header.top-bar', '0020-ac10.png')

  await page.goto('/admin/gallery')
  await page.getByTestId('gallery-display-names').getByLabel('English').fill('Oto Gallery Showroom')
  await page.getByTestId('gallery-save').click()
  await expect(page.getByTestId('dealership-saved')).toBeVisible()

  await page.goto('/en/cars')
  await expect(page.getByTestId('top-bar-brand')).toHaveText('Oto Gallery Showroom')
  await kanit(page, 'header.top-bar', '0020-ac14.png')
})

test('0020/AC-11, AC-12: Arapça sayfada ad Arapça ve satırın başında (sağda); tıklamak /ar/ ana sayfasına götürür', async ({
  page,
}) => {
  await page.goto('/ar/cars')
  const ad = page.getByTestId('top-bar-brand')
  await expect(ad).toHaveText('Oto Galeri')

  // Seed'in Arapça görünen adı yok: kurulum yalnız varsayılan dili doldurur (C4). Arapça ad
  // panelden yazılıp sayfa yeniden açılır; test sonunda Arapça ad kaldırılır.
  const simdiki = await galeri(page.request)
  await page.request.put(`${API}/gallery`, {
    data: {
      ...simdiki,
      taxNumber: simdiki.taxNumber ?? '',
      tradeRegistryNumber: simdiki.tradeRegistryNumber ?? '',
      displayNames: { ...simdiki.displayNames, ar: 'أوتو غاليري' },
    },
  })

  try {
    await page.goto('/ar/cars')
    await expect(ad).toHaveText('أوتو غاليري')

    // `0020` AC-12 — hiza Range ile ölçülür: blok kutu satırı kaplar ve hizayı göstermez
    // (`kabuk.spec.ts`'in kutu ölçümü yön için yeterli, hiza için değil).
    const olcum = await ad.evaluate((element) => {
      const range = document.createRange()
      range.selectNodeContents(element)
      const metin = range.getBoundingClientRect()
      const satir = element.parentElement!.getBoundingClientRect()
      const dolgu = parseFloat(getComputedStyle(element.parentElement!).paddingInlineStart)
      return { metinSag: metin.right, satirSag: satir.right - dolgu }
    })
    expect(Math.abs(olcum.metinSag - olcum.satirSag)).toBeLessThan(2)
    await kanit(page, 'header.top-bar', '0020-ac12-ar.png')

    await ad.click()
    await expect(page).toHaveURL(/\/ar\/$/)
    await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
    await kanit(page, 'header.top-bar', '0020-ac11-ar.png')

    // AC-13 — giriş satırı da görünen adı gösterir; ticari unvan değil.
    await expect(page.getByTestId('gallery-name')).toHaveText('أوتو غاليري')
    await kanit(page, '[data-testid="home-intro"]', '0020-ac13-ar.png')
  } finally {
    const son = await galeri(page.request)
    const adlar = { ...son.displayNames }
    delete adlar['ar']
    await page.request.put(`${API}/gallery`, {
      data: {
        ...son,
        taxNumber: son.taxNumber ?? '',
        tradeRegistryNumber: son.tradeRegistryNumber ?? '',
        displayNames: adlar,
      },
    })
  }
})

test('0020/AC-13, AC-16: Türkçe giriş satırı görünen adı gösterir; unvan ve sicil hiçbir vitrin yanıtında yok', async ({
  page,
}) => {
  await page.goto('/tr/')
  await expect(page.getByTestId('gallery-name')).toHaveText('Oto Galeri')
  await kanit(page, '[data-testid="home-intro"]', '0020-ac13.png')

  const yanit = (await (
    await page.request.get('/api/dealership/gallery?locale=tr')
  ).json()) as Record<string, unknown>
  expect(Object.keys(yanit).sort()).toEqual(['branches', 'displayName'])
  const { taxNumber } = await galeri(page.request)
  if (taxNumber !== null) {
    expect(await page.content()).not.toContain(taxNumber)
  }
})

test('0020/AC-15: üst çubuk ilk boyamada adla gelir; çubukta düzen kayması yok', async ({
  page,
}) => {
  await page.addInitScript(() => {
    ;(window as unknown as { kaymalar: number[] }).kaymalar = []
    new PerformanceObserver((liste) => {
      for (const kayit of liste.getEntries() as unknown as {
        value: number
        sources?: { node?: Node }[]
      }[]) {
        if (
          (kayit.sources ?? []).some(
            (kaynak) =>
              kaynak.node instanceof Element && kaynak.node.closest('header.top-bar') !== null,
          )
        ) {
          ;(window as unknown as { kaymalar: number[] }).kaymalar.push(kayit.value)
        }
      }
    }).observe({ type: 'layout-shift', buffered: true })
  })

  for (const adres of ['/tr/', '/tr/cars']) {
    await page.goto(adres, { waitUntil: 'domcontentloaded' })
    // Ad sunucunun bastığı etiketten gelir; ilk boyamada DOM'da hazırdır.
    await expect(page.getByTestId('top-bar-brand')).toHaveText('Oto Galeri', { timeout: 1000 })
    await page.waitForLoadState('networkidle')
    expect(
      await page.evaluate(() => (window as unknown as { kaymalar: number[] }).kaymalar),
    ).toEqual([])
  }
})

test('0020/AC-17: ad okunamadığında üst çubukta yer boş; sayfa çalışır', async ({ page }) => {
  await page.route('**/tr/cars', async (route) => {
    const yanit = await route.fetch()
    const html = (await yanit.text()).replace(/<meta name="gallery-display-name"[^>]*>/, '')
    return route.fulfill({ response: yanit, body: html })
  })
  await page.goto('/tr/cars')

  await expect(page.getByTestId('top-bar-brand')).toHaveText('')
  await expect(page.getByTestId('result-count')).toBeVisible()
  await expect(page.locator('header.top-bar')).not.toContainText('Oto Galeri')
  await kanit(page, 'header.top-bar', '0020-ac17.png')
})

test('0020/AC-18: özel karakterli ad üst çubukta ve giriş satırında metin olarak', async ({
  page,
}) => {
  await ingilizceAd(page.request, OZEL_AD)
  await page.goto('/en/')

  await expect(page.getByTestId('top-bar-brand')).toHaveText(OZEL_AD)
  await expect(page.getByTestId('gallery-name')).toHaveText(OZEL_AD)
  await expect(page.locator('header.top-bar b')).toHaveCount(0)
  await kanit(page, 'header.top-bar', '0020-ac18-a.png')
  await kanit(page, '[data-testid="home-intro"]', '0020-ac18-b.png')
})

// ----- Panel: galeri profili -----

test('0020/AC-1, AC-3, AC-5, AC-8: galeri ekranı; boş unvan, boş varsayılan ad ve sınır aşımı alanın yanında', async ({
  page,
}) => {
  await page.goto('/admin/gallery')
  await expect(page.getByTestId('gallery-profile')).toBeVisible()
  await expect(page.getByLabel('Ticari unvan')).toHaveValue('Oto Galeri')
  await kanit(page, '[data-testid="gallery-profile"]', '0020-ac1.png')

  await page.getByLabel('Ticari unvan').fill('   ')
  await page.getByTestId('gallery-display-names').getByLabel('Türkçe').fill('')
  await page.getByLabel('Vergi numarası').fill('1'.repeat(41))
  await page.getByTestId('gallery-save').click()

  await expect(page.getByTestId('gallery-commercial-name-error')).toHaveText(
    'Ticari unvan boş bırakılamaz.',
  )
  await expect(page.getByTestId('gallery-display-name-tr-error')).toHaveText(
    'Varsayılan dildeki görünen ad boş bırakılamaz.',
  )
  await expect(page.getByTestId('gallery-tax-number-error')).toHaveText(
    'Vergi numarası en fazla 40 karakter olabilir.',
  )
  await kanit(page, '[data-testid="gallery-profile"]', '0020-ac3.png')
  await kanit(page, '[data-testid="gallery-display-names"]', '0020-ac5.png')
  await kanit(page, '[data-testid="gallery-profile"]', '0020-ac8.png')

  // Kayıtlı değer değişmedi.
  expect((await galeri(page.request)).commercialName).toBe('Oto Galeri')
})

test('0020/AC-2, AC-4, AC-6: unvan değişikliği, boşaltılan sicil ve boş bırakılan dil kaydedilir', async ({
  page,
}) => {
  const once = await galeri(page.request)

  try {
    await page.goto('/admin/gallery')
    await page.getByLabel('Ticari unvan').fill('Oto Galeri Otomotiv Ticaret A.Ş.')
    await page.getByLabel('Vergi numarası').fill('')
    await page.getByLabel('Ticaret sicil numarası').fill('')
    await page.getByTestId('gallery-display-names').getByLabel('English').fill('')
    await page.getByTestId('gallery-save').click()
    await expect(page.getByTestId('dealership-saved')).toBeVisible()

    // Ekran YENİDEN AÇILIR: değer sunucudan okunur, formun belleğinden değil.
    await page.goto('/admin/gallery')
    await expect(page.getByLabel('Ticari unvan')).toHaveValue('Oto Galeri Otomotiv Ticaret A.Ş.')
    await expect(page.getByLabel('Vergi numarası')).toHaveValue('')
    await expect(page.getByLabel('Ticaret sicil numarası')).toHaveValue('')
    await expect(page.getByTestId('gallery-display-names').getByLabel('English')).toHaveValue('')
    await kanit(page, '[data-testid="gallery-profile"]', '0020-ac2.png')
    await kanit(page, '[data-testid="gallery-profile"]', '0020-ac4.png')
    await kanit(page, '[data-testid="gallery-display-names"]', '0020-ac6.png')

    const sonra = await galeri(page.request)
    expect(sonra.taxNumber).toBeNull()
    expect(sonra.tradeRegistryNumber).toBeNull()
    expect(sonra.displayNames['en']).toBeUndefined()
  } finally {
    const simdi = await galeri(page.request)
    await page.request.put(`${API}/gallery`, {
      data: {
        version: simdi.version,
        commercialName: once.commercialName,
        taxNumber: once.taxNumber ?? '',
        tradeRegistryNumber: once.tradeRegistryNumber ?? '',
        displayNames: once.displayNames,
      },
    })
  }
})

test('0020/AC-69, AC-70, AC-71: yükleniyor, okuma hatası ve kaydetme hatası (değerler kalır)', async ({
  page,
}) => {
  await page.route('**/api/admin/gallery', (route) =>
    route.request().method() === 'GET' ? new Promise(() => {}) : route.fallback(),
  )
  await page.goto('/admin/gallery')
  await expect(page.getByTestId('gallery-loading')).toBeVisible()
  await kanit(page, '[data-testid="gallery-loading"]', '0020-ac69.png')
  await page.unrouteAll({ behavior: 'ignoreErrors' })

  await page.route('**/api/admin/branches?*', () => new Promise(() => {}))
  await page.goto('/admin/branches')
  await expect(page.getByTestId('branches-loading')).toBeVisible()
  await kanit(page, '[data-testid="branches-loading"]', '0020-ac69-b.png')
  await page.unrouteAll({ behavior: 'ignoreErrors' })

  await page.route('**/api/admin/branches?*', (route) =>
    route.fulfill({
      status: 500,
      json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-0020' },
    }),
  )
  await page.goto('/admin/branches')
  await expect(page.getByTestId('branches-error')).toBeVisible()
  await expect(page.getByTestId('branches-error')).not.toContainText('trace-e2e-0020')
  await kanit(page, '[data-testid="branches-error"]', '0020-ac70.png')
  await page.unrouteAll({ behavior: 'ignoreErrors' })

  await page.route('**/api/admin/gallery', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill({
          status: 500,
          json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-0020' },
        })
      : route.fallback(),
  )
  await page.goto('/admin/gallery')
  await expect(page.getByTestId('gallery-error')).toBeVisible()
  await expect(page.getByTestId('gallery-error')).not.toContainText('trace-e2e-0020')
  await kanit(page, '[data-testid="gallery-error"]', '0020-ac70-b.png')
  await page.unrouteAll({ behavior: 'ignoreErrors' })

  // Boş liste B5 yüzünden gerçek veriyle kurulamaz; ekranın ne söylediği yanıt araya
  // girilerek görülür (docs/frontend.md § 3).
  await page.route('**/api/admin/branches?*', (route) => route.fulfill({ status: 200, json: [] }))
  await page.goto('/admin/branches')
  await expect(page.getByTestId('branches-empty')).toBeVisible()
  await kanit(page, '[data-testid="branches-empty"]', '0020-ac19-bos.png')
  await page.unrouteAll({ behavior: 'ignoreErrors' })

  await page.route('**/api/admin/gallery', (route) =>
    route.request().method() === 'PUT'
      ? route.fulfill({
          status: 500,
          json: { errorCode: 'api.unavailable', traceId: 'trace-e2e-0020' },
        })
      : route.fallback(),
  )
  await page.goto('/admin/gallery')
  await page.getByLabel('Vergi numarası').fill('9988776655')
  await page.getByTestId('gallery-save').click()
  await expect(page.getByTestId('dealership-save-failed')).toBeVisible()
  await expect(page.getByLabel('Vergi numarası')).toHaveValue('9988776655')
  await kanit(page, '[data-testid="gallery-profile"]', '0020-ac71.png')
})

// ----- Panel: şubeler -----

test('0020/AC-19, AC-21, AC-23, AC-24, AC-26, AC-28, AC-29, AC-31: şube açma, doğrulama ve liste', async ({
  page,
}) => {
  const ad = `E2E Moda ${Date.now().toString(36)}`
  const slug = benzersiz('e2e-moda')

  await page.goto('/admin/branches/new')
  await page.getByLabel('Telefon').fill('02165554433')
  await page.getByLabel('E-posta').fill('kadikoy@')
  await page.getByLabel('Adres kısaltması').fill('kadıköy-şubesi')
  await page.getByTestId('branch-save').click()

  await expect(page.getByTestId('branch-name-tr-error')).toHaveText(
    'Varsayılan dildeki şube adı boş bırakılamaz.',
  )
  await expect(page.getByTestId('branch-phone-error')).toBeVisible()
  await expect(page.getByTestId('branch-email-error')).toBeVisible()
  await expect(page.getByTestId('branch-slug-error')).toBeVisible()
  await expect(page.getByTestId('branch-time-zone-error')).toHaveText('Bir saat dilimi seçin.')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac24.png')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac26.png')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac29.png')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac31.png')

  // AC-28 — saat dilimi bir liste; serbest metin yazılamaz.
  const saat = page.getByLabel('Saat dilimi')
  await expect(saat).toHaveJSProperty('tagName', 'SELECT')
  await saat.selectOption('Europe/Istanbul')
  await kanit(page, '#branch-time-zone', '0020-ac28.png')

  const adlar = page.locator('fieldset.admin-form__group').nth(0)
  const adresler = page.locator('fieldset.admin-form__group').nth(1)
  await adlar.getByLabel('Türkçe').fill(ad)
  await adresler.getByLabel('Türkçe').fill('Moda Caddesi 9')
  await page.getByLabel('Telefon').fill('+902161119988')
  await page.getByLabel('E-posta').fill('')
  await page.getByLabel('Adres kısaltması').fill(slug)
  await page.getByTestId('branch-save').click()

  await expect(page).toHaveURL(/\/admin\/branches$/)
  const satir = page.getByTestId('branch-row').filter({ hasText: ad })
  await expect(satir).toContainText('Açık')
  await kanit(page, '[data-testid="branches"]', '0020-ac21.png')

  await page.goto('/tr/')
  const altlik = page.getByTestId('branch').filter({ hasText: ad })
  await expect(altlik).toContainText('Moda Caddesi 9')
  await expect(altlik).toContainText('+902161119988')
  await kanit(page, '[data-testid="branch-list"]', '0020-ac23.png')

  // AC-19 — üç durum bir arada: bu şubeyi geçici kapat, bir tane daha açıp kalıcı kapat.
  const listede = (await (await page.request.get('/api/admin/branches?locale=tr')).json()) as {
    branchId: string
    name: string
  }[]
  const id = listede.find((item) => item.name === ad)!.branchId
  await eylem(page.request, id, 'close-temporarily')
  const kapanacak = await subeAc(page.request, `E2E Liman ${Date.now().toString(36)}`)
  await eylem(page.request, kapanacak.branchId, 'close')

  await page.goto('/admin/branches')
  await expect(page.getByTestId('branch-row').filter({ hasText: ad })).toContainText(
    'Geçici olarak kapalı',
  )
  await expect(
    page.getByTestId('branch-row').filter({ hasText: 'E2E Liman' }).first(),
  ).toContainText('Kapalı')
  await kanit(page, '[data-testid="branches"]', '0020-ac19.png')

  await eylem(page.request, id, 'close')
})

test('0020/AC-30, AC-33: kullanılmış kısaltma ve sınır aşımı alan hatası', async ({ page }) => {
  const var_olan = await subeAc(page.request, `E2E Kısaltma ${Date.now().toString(36)}`)

  await page.goto('/admin/branches/new')
  const adlar = page.locator('fieldset.admin-form__group').nth(0)
  await adlar.getByLabel('Türkçe').fill('a'.repeat(121))
  await page.locator('fieldset.admin-form__group').nth(1).getByLabel('Türkçe').fill('b'.repeat(301))
  await page.getByLabel('Telefon').fill('+902161119988')
  await page.getByLabel('Saat dilimi').selectOption('Europe/Istanbul')
  await page.getByLabel('Adres kısaltması').fill(var_olan.slug)
  await page.getByTestId('branch-save').click()

  await expect(page.getByTestId('branch-name-tr-error')).toHaveText(
    'Şube adı en fazla 120 karakter olabilir.',
  )
  await expect(page.getByTestId('branch-address-tr-error')).toHaveText(
    'Adres en fazla 300 karakter olabilir.',
  )
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac33.png')

  await adlar.getByLabel('Türkçe').fill('Kısaltması Alınmış')
  await page.locator('fieldset.admin-form__group').nth(1).getByLabel('Türkçe').fill('Adres 1')
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('branch-slug-error')).toHaveText(
    'Bu kısaltma zaten başka bir şube tarafından kullanılıyor. Başka bir kısaltma yazın.',
  )
  await kanit(page, '#branch-slug-error', '0020-ac30.png')

  await eylem(page.request, var_olan.branchId, 'close')
})

test('0020/AC-39, AC-43, AC-46, AC-47, AC-48: opsiyonelleri boşaltma, geçici kapatma, yeniden açma ve onaylı kalıcı kapatma', async ({
  page,
}) => {
  const ad = `E2E Durum ${Date.now().toString(36)}`
  const { branchId } = await subeAc(page.request, ad)
  const { version } = await sube(page.request, branchId)
  await page.request.put(`${API}/branches/${branchId}`, {
    data: {
      version,
      names: { tr: ad },
      addresses: { tr: `${ad} Caddesi 1` },
      phone: '+902161230000',
      whatsApp: '+905321112233',
      email: 'durum@ornek.com',
      timeZone: 'Europe/Istanbul',
      slug: (await sube(page.request, branchId)).slug,
      latitude: 40.98,
      longitude: 29.06,
    },
  })

  await page.goto(`/admin/branches/${branchId}`)
  await page.getByLabel('WhatsApp numarası').fill('')
  await page.getByLabel('E-posta').fill('')
  await page.getByLabel('Enlem').fill('')
  await page.getByLabel('Boylam').fill('')
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('dealership-saved')).toBeVisible()
  await expect(page.getByLabel('WhatsApp numarası')).toHaveValue('')
  await expect(page.getByLabel('E-posta')).toHaveValue('')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac39.png')

  await page.getByTestId('branch-close-temporarily').click()
  await expect(page.getByTestId('branch-status')).toContainText('Geçici olarak kapalı')
  await page.goto('/admin/branches')
  await expect(page.getByTestId('branch-row').filter({ hasText: ad })).toContainText(
    'Geçici olarak kapalı',
  )
  await kanit(page, page.getByTestId('branch-row').filter({ hasText: ad }), '0020-ac43.png')
  await page.goto('/tr/')
  await expect(page.getByTestId('branch').filter({ hasText: ad })).toHaveCount(0)

  await page.goto(`/admin/branches/${branchId}`)
  await page.getByTestId('branch-reopen').click()
  await expect(page.getByTestId('branch-status')).toContainText('Açık')
  await page.goto('/tr/')
  await expect(page.getByTestId('branch').filter({ hasText: ad })).toHaveCount(1)
  await kanit(page, page.getByTestId('branch').filter({ hasText: ad }), '0020-ac46.png')

  await page.goto(`/admin/branches/${branchId}`)
  await page.getByTestId('branch-close').click()
  await expect(page.getByTestId('branch-close-body')).toHaveText(
    'Bu işlem geri alınamaz. Bu şubedeki araçlar vitrinden kalkar ve şube ataması bekler.',
  )
  await kanit(page, '[data-testid="branch-close-dialog"]', '0020-ac47.png')
  await page.getByTestId('branch-close-cancel').click()
  await expect(page.getByTestId('branch-status')).toContainText('Açık')

  await page.getByTestId('branch-close').click()
  await page.getByTestId('branch-close-confirm').click()
  await expect(page.getByTestId('branch-status')).toContainText('Kapalı')
  await expect(page.getByTestId('branch-closed-note')).toBeVisible()
  await expect(page.getByTestId('branch-actions')).toHaveCount(0)
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac48.png')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac42.png')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac54.png')
})

test('0020/AC-50: son aktif şubenin reddi nedeniyle söylenir', async ({ page }) => {
  const { branchId } = await subeAc(page.request, `E2E Son ${Date.now().toString(36)}`)

  // Galeride başka aktif şube yokken kalıcı kapatmanın reddi; kurulumun şubelerini kapatmamak
  // için reddin kendisi araya girilerek kurulur. Sunucunun kararı `Host.Tests`'te ölçülür.
  await page.route(`**/api/admin/branches/${branchId}/close`, (route) =>
    route.fulfill({
      status: 409,
      json: { type: 'about:blank', status: 409, errorCode: 'branch.last_active_branch' },
    }),
  )
  await page.goto(`/admin/branches/${branchId}`)
  await page.getByTestId('branch-close').click()
  await page.getByTestId('branch-close-confirm').click()

  await expect(page.getByTestId('dealership-save-failed')).toContainText(
    'başka aktif şubesi olmadığı için',
  )
  await expect(page.getByTestId('branch-status')).toContainText('Açık')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac50.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await eylem(page.request, branchId, 'close')
})

test('0020/AC-63: başka sekmede kaydedilen şube eski sürümle kaydedilince uyarı; yazılan değer kalır', async ({
  page,
  context,
}) => {
  const { branchId } = await subeAc(page.request, `E2E Sekme ${Date.now().toString(36)}`)
  const ikinci = await context.newPage()

  await page.goto(`/admin/branches/${branchId}`)
  await ikinci.goto(`/admin/branches/${branchId}`)

  await ikinci.getByLabel('Telefon').fill('+902169990001')
  await ikinci.getByTestId('branch-save').click()
  await expect(ikinci.getByTestId('dealership-saved')).toBeVisible()

  await page.getByLabel('Telefon').fill('+902169990002')
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('dealership-conflict')).toBeVisible()
  await expect(page.getByLabel('Telefon')).toHaveValue('+902169990002')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac63.png')

  await page.getByTestId('dealership-conflict-reload').click()
  await expect(page.getByLabel('Telefon')).toHaveValue('+902169990001')

  await ikinci.close()
  await eylem(page.request, branchId, 'close')
})

test('0020/AC-58: aracın kapatılmış şubesi seçilemez gösterilir ve başka şube istenir', async ({
  page,
}) => {
  const kapanacak = await subeAc(page.request, `E2E Araç Şubesi ${Date.now().toString(36)}`)
  const arac = await page.request.post('/api/admin/vehicles', {
    data: {
      branchId: kapanacak.branchId,
      vehicleModelId: '0192c000-0000-7000-8000-00000000000a',
      bodyType: 'suv',
      fuelType: 'diesel',
      transmissionType: 'automatic',
      drivetrainType: 'front-wheel-drive',
      trimLevel: 'comfort',
      color: { tr: 'Beyaz' },
      modelYear: 2019,
      mileageKilometers: 45000,
      priceAmount: 750000,
      priceOnRequest: false,
      vin: `E2E${Date.now().toString().slice(-14)}`,
    },
  })
  expect(arac.status(), await arac.text()).toBe(201)
  const { vehicleId } = (await arac.json()) as { vehicleId: string }
  await eylem(page.request, kapanacak.branchId, 'close')

  await page.goto(`/admin/vehicles/${vehicleId}`)
  const secici = page.getByLabel('Şube', { exact: true })
  await expect(secici.locator('option:checked')).toHaveAttribute('disabled', '')
  await expect(page.getByTestId('vehicle-branch-hint')).toHaveText(
    'Bu aracın şubesi kalıcı olarak kapatılmış. Başka bir şube seçin.',
  )
  await kanit(page, page.locator('.admin-form__field').filter({ has: secici }), '0020-ac58.png')

  await page.goto('/admin/vehicle-new')
  await expect(
    page.getByLabel('Şube', { exact: true }).locator('option', { hasText: 'E2E Araç Şubesi' }),
  ).toHaveCount(0)
})

// ----- Erişim ve dil -----

test('0020/AC-64, AC-65: Satış Danışmanı bağlantı görmez; adresi açtığında "erişim yok"', async ({
  page,
}) => {
  await page.goto('/admin/vehicles')
  await expect(page.getByRole('link', { name: 'Galeri' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Şubeler' })).toBeVisible()

  await rol(page, 'sales_advisor')
  await page.goto('/admin/vehicles')
  await expect(page.locator('.admin-shell__nav')).toBeVisible()
  await expect(page.getByRole('link', { name: 'Galeri' })).toHaveCount(0)
  await expect(page.getByRole('link', { name: 'Şubeler' })).toHaveCount(0)
  await kanit(page, '.admin-shell__nav', '0020-ac64.png')

  await page.goto('/admin/branches')
  await expect(page.getByTestId('admin-no-access')).toBeVisible()
  await expect(page.getByTestId('branches-error')).toHaveCount(0)
  await kanit(page, '.admin-shell', '0020-ac65.png')
})

test('0020/AC-20, AC-73: Arapça panelde liste Arapça adla; form sağdan sola, telefon ve kısaltma soldan sağa', async ({
  page,
}) => {
  const ad = `E2E Arapça ${Date.now().toString(36)}`
  const { branchId } = await subeAc(page.request, ad)
  await arapcaPanel(page)

  await page.goto('/admin/branches')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByTestId('branch-row').filter({ hasText: `فرع ${ad}` })).toBeVisible()
  await kanit(page, '[data-testid="branches"]', '0020-ac20-ar.png')

  await page.goto(`/admin/branches/${branchId}`)
  await expect(page.getByLabel('الهاتف', { exact: true })).toHaveAttribute('dir', 'ltr')
  await expect(page.getByLabel('الاختصار في العنوان')).toHaveAttribute('dir', 'ltr')
  await expect(page.getByLabel('البريد الإلكتروني')).toHaveAttribute('dir', 'ltr')
  await kanit(page, '[data-testid="branch-edit"]', '0020-ac73-ar.png')

  await page.goto('/admin/gallery')
  await expect(page.getByTestId('gallery-profile')).toBeVisible()
  await kanit(page, '[data-testid="gallery-profile"]', '0020-ac73-ar-b.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await eylem(page.request, branchId, 'close')
})
