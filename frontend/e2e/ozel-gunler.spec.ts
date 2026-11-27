import { expect, test, type APIRequestContext, type Page, type Route } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0027` — özel günler ve "şu an", gerçek Host'a karşı. Host testlerinin (sabit `IClock`) ikizi:
 * burada sunucunun saati GERÇEKTİR, yani beklenen metin o anda sunucunun döndüğü `openNow`'dan
 * türetilir; tarayıcının saati ve saat dilimi bilerek kaydırılır (AC-58, AC-101). Ölçüm
 * kriterleri (AC-68, AC-69, AC-71, AC-75–AC-77) fontlar bekletilip serbest bırakılarak alınır.
 *
 * <b>Paylaşılan durum.</b> Testler kendi şubelerini açar ve sonunda KALICI kapatır.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const API = '/api/admin/branches'
const GUNLER = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']
const GUN_TR = ['Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi', 'Pazar']
const AY_TR = [
  'Ocak',
  'Şubat',
  'Mart',
  'Nisan',
  'Mayıs',
  'Haziran',
  'Temmuz',
  'Ağustos',
  'Eylül',
  'Ekim',
  'Kasım',
  'Aralık',
]

type Aralik = { start: string; end: string }
type Program = Record<string, Aralik[]>
type Ozel = { from: string; through?: string | null; intervals: Aralik[] }
type SuAn = {
  open: boolean
  today: string
  next: { date: string; time: string } | null
  refreshInSeconds: number
}

const HAFTA_ICI = { start: '09:00', end: '18:00' }
const IKI = [
  { start: '09:00', end: '13:00' },
  { start: '16:00', end: '21:00' },
]

function program(gunler: Program = {}): Program {
  return Object.fromEntries(GUNLER.map((gun) => [gun, gunler[gun] ?? []]))
}

const PROGRAMLI = program({
  monday: [HAFTA_ICI],
  tuesday: [HAFTA_ICI],
  wednesday: [HAFTA_ICI],
  thursday: [HAFTA_ICI],
  friday: [HAFTA_ICI],
  saturday: [{ start: '10:00', end: '14:00' }],
})
const HER_GUN_IKI = Object.fromEntries(GUNLER.map((gun) => [gun, IKI]))

const acilanlar: string[] = []

function benzersiz(onek: string) {
  return `${onek} ${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`
}

/** `"YYYY-MM-DD"` + gün (UTC gün sayısıyla, süreç saat diliminden bağımsız). */
function ekle(tarih: string, gun: number): string {
  const [y, a, g] = tarih.split('-').map(Number)
  return new Date(Date.UTC(y!, a! - 1, g! + gun)).toISOString().slice(0, 10)
}

function fark(a: string, b: string): number {
  return Math.round((Date.parse(`${b}T00:00:00Z`) - Date.parse(`${a}T00:00:00Z`)) / 86_400_000)
}

function haftaGunu(tarih: string): number {
  return (new Date(`${tarih}T00:00:00Z`).getUTCDay() + 6) % 7
}

/** D10'un tr kalıbı, sunucunun `openNow`'undan — beklenen metin (AC-58, AC-101). */
function beklenenSatir(d: SuAn): string {
  const durum = d.open ? 'Şu an açık' : 'Şu an kapalı'
  if (d.next === null) {
    return durum
  }
  const g = fark(d.today, d.next.date)
  const gun =
    g === 0
      ? ''
      : g === 1
        ? 'yarın '
        : g < 7
          ? `${GUN_TR[haftaGunu(d.next.date)]} `
          : `${Number(d.next.date.slice(8))} ${AY_TR[Number(d.next.date.slice(5, 7)) - 1]} `
  return `${durum} · ${d.open ? 'Kapanış' : 'Açılış'} ${gun}${d.next.time}`
}

async function subeAc(
  api: APIRequestContext,
  ad: string,
  saatler: Program | null,
  ozel?: Ozel[],
  saatDilimi = 'Europe/Istanbul',
): Promise<string> {
  const yanit = await api.post(API, {
    data: {
      names: { tr: ad, ar: `فرع ${ad}`, en: `${ad} Branch` },
      addresses: { tr: `${ad} Caddesi 1` },
      phone: '+902161230000',
      timeZone: saatDilimi,
      slug: benzersiz('e2e-ozel').replace(/\s/g, '-').toLowerCase(),
      ...(saatler === null ? {} : { openingHours: saatler }),
      ...(ozel === undefined ? {} : { specialHours: ozel }),
    },
  })
  expect(yanit.status(), `POST branch → ${await yanit.text()}`).toBe(201)
  const { id } = (await yanit.json()) as { id: string }
  acilanlar.push(id)
  return id
}

async function sube(api: APIRequestContext, id: string) {
  return (await (await api.get(`${API}/${id}`)).json()) as Record<string, unknown> & {
    version: number
    status: string
    specialHours: Ozel[]
  }
}

async function ozelYaz(api: APIRequestContext, id: string, ozel: unknown) {
  const govde = await sube(api, id)
  return api.put(`${API}/${id}`, { data: { ...govde, specialHours: ozel } })
}

async function vitrinSubesi(api: APIRequestContext, ad: string, dil = 'tr') {
  const galeri = (await (await api.get(`/api/dealership/gallery?locale=${dil}`)).json()) as {
    branches: { name: string; openNow: SuAn | null; specialHours: Ozel[] }[]
  }
  return galeri.branches.find((b) => b.name === ad)!
}

/** Sunucunun bu şube için "bugün"ü (şubenin saat dilimi). */
async function bugun(api: APIRequestContext, ad: string): Promise<string> {
  return (await vitrinSubesi(api, ad)).openNow!.today
}

function altliktakiSube(page: Page, ad: string) {
  return page.getByTestId('branch').filter({ hasText: ad })
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

/** Saat parçası bekletilir (D15 evre B); `birak()` serbest bırakır. */
async function parcayiBeklet(page: Page) {
  let birak: () => void = () => {}
  const parca = new Promise<void>((resolve) => (birak = resolve))
  await page.route(/\/assets\/branchClock-[^/]+\.(js|css)$/, async (route: Route) => {
    await parca
    await route.continue()
  })
  return birak
}

test.beforeEach(async ({ page }, testInfo) => {
  test.skip(testInfo.project.name !== 'masaustu', 'Şubeler global; testler tek profilde sırayla.')
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
})

test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.project.name !== 'masaustu') {
    return
  }
  await page.unrouteAll({ behavior: 'ignoreErrors' })
  while (acilanlar.length > 0) {
    const id = acilanlar.pop()!
    const { version, status } = await sube(page.request, id)
    if (status !== 'Closed') {
      await page.request.post(`${API}/${id}/close`, { data: { version } })
    }
  }
})

// ----- Sunucu ikizleri (Host: OzelGunZinciriTests) -----

test('0027/AC-10, AC-11, AC-16, AC-23, AC-42, AC-43, AC-47 (ikiz): Hatalı özel gün 422 (NUL, satır sonu, 2027-02-30, [null] dâhil; 500 yok); geçmiş kayıt düşer; okuma biçimi ve no-store', async ({
  page,
}) => {
  const ad = benzersiz('Özel İkiz')
  const id = await subeAc(page.request, ad, PROGRAMLI, undefined, 'Asia/Tokyo')
  const bugunTokyo = await bugun(page.request, ad)

  for (const [liste, alan, kod] of [
    [[{ from: '2027-02-30', intervals: [] }], 'specialHours.0.from', 'special_hours.invalid_date'],
    [
      [{ from: '2027-01\u00000-01', intervals: [] }],
      'specialHours.0.from',
      'special_hours.invalid_date',
    ],
    [
      [{ from: '2027-01-01\n', intervals: [] }],
      'specialHours.0.from',
      'special_hours.invalid_date',
    ],
    [[null], 'specialHours.0', 'special_hours.invalid_entry'],
    [
      [{ from: ekle(bugunTokyo, 5) }],
      'specialHours.0.intervals',
      'special_hours.intervals_required',
    ],
    [
      [{ from: ekle(bugunTokyo, 5), through: ekle(bugunTokyo, 4), intervals: [] }],
      'specialHours.0.through',
      'special_hours.end_before_start',
    ],
    [
      Array.from({ length: 13 }, (_, i) => ({ from: ekle(bugunTokyo, i + 1), intervals: [] })),
      'specialHours',
      'special_hours.too_many',
    ],
  ] as const) {
    const yanit = await ozelYaz(page.request, id, liste)
    expect(yanit.status(), JSON.stringify(liste)).toBe(422)
    expect(((await yanit.json()) as Record<string, string>)[alan]).toBe(kod)
  }

  // AC-23 — geçmiş ama geçerli kayıt sessizce düşer.
  const kabul = await ozelYaz(page.request, id, [
    { from: ekle(bugunTokyo, -3), intervals: [] },
    { from: ekle(bugunTokyo, 4), intervals: [] },
  ])
  expect(kabul.status(), await kabul.text()).toBe(200)
  expect((await sube(page.request, id)).specialHours.map((k) => k.from)).toEqual([
    ekle(bugunTokyo, 4),
  ])

  const okuma = await page.request.get('/api/dealership/gallery?locale=tr')
  expect(okuma.headers()['cache-control']).toBe('no-store')
  const metin = await okuma.text()
  expect(metin).not.toContain('Asia/Tokyo')
  const oge = (JSON.parse(metin) as { branches: Record<string, unknown>[] }).branches.find(
    (b) => b['name'] === ad,
  )!
  expect(Object.keys(oge).sort()).toEqual([
    'address',
    'name',
    'openNow',
    'openingHours',
    'phone',
    'specialHours',
  ])
  expect(Object.keys(oge['openNow'] as object).sort()).toEqual([
    'next',
    'open',
    'refreshInSeconds',
    'today',
  ])
})

test('0027/AC-62, AC-65, AC-66 (ikiz): Kaydedilen özel gün ilk /tr/ belgesinin yapısal verisinde; belge "şu an" taşımaz', async ({
  page,
}) => {
  const ad = benzersiz('Yapısal')
  const id = await subeAc(page.request, ad, PROGRAMLI)
  const gun = ekle(await bugun(page.request, ad), 3)
  expect((await ozelYaz(page.request, id, [{ from: gun, intervals: [] }])).status()).toBe(200)

  const belge = await (await page.request.get('/tr/')).text()
  const ld = JSON.parse(
    /<script type="application\/ld\+json">([\s\S]*?)<\/script>/.exec(belge)![1]!,
  ) as { department: { name: string; specialOpeningHoursSpecification?: unknown[] }[] }
  expect(ld.department.find((d) => d.name === ad)!.specialOpeningHoursSpecification).toEqual([
    {
      '@type': 'OpeningHoursSpecification',
      validFrom: gun,
      validThrough: gun,
      opens: '00:00',
      closes: '00:00',
    },
  ])
  expect(belge).not.toContain('Şu an')
  expect(belge).not.toContain('refreshInSeconds')
})

// ----- Panel (D13) -----

test('0027/AC-1, AC-2, AC-3, AC-9, AC-22: Bölüm "Çalışma saatleri"nden sonra; kayıt girilir, from sırasıyla döner; hata kaydın yanında; ar panelinde sağdan sola', async ({
  page,
}) => {
  const ad = benzersiz('Özel Panel')
  const id = await subeAc(page.request, ad, PROGRAMLI)
  const gun = await bugun(page.request, ad)
  await page.goto(`/admin/branches/${id}`)
  const bolum = page.getByTestId('branch-special-hours')
  await expect(bolum.locator('legend')).toHaveText('Özel günler ve tatiller')
  await expect(bolum).toContainText('Bayram ve tatil tarihleri her yıl elle girilir.')
  expect(
    await page
      .getByTestId('branch-opening-hours')
      .evaluate(
        (saat, ozel) => !!(saat.compareDocumentPosition(ozel!) & Node.DOCUMENT_POSITION_FOLLOWING),
        await bolum.elementHandle(),
      ),
  ).toBe(true)
  await kanit(page, bolum, '0027-ac1.png')

  await page.getByTestId('special-add').click()
  await page.getByTestId('special-0-from').fill(ekle(gun, 20))
  await page.getByTestId('special-add').click()
  await page.getByTestId('special-1-from').fill(ekle(gun, 5))
  await page.getByTestId('special-1-closed').uncheck()
  await page.getByTestId('special-1-0-start').fill('10:00')
  await page.getByTestId('special-1-0-end').fill('13:00')
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('dealership-saved')).toBeVisible()

  await page.goto(`/admin/branches/${id}`)
  await expect(page.getByTestId('special-0-from')).toHaveValue(ekle(gun, 5))
  await expect(page.getByTestId('special-0-0-start')).toHaveValue('10:00')
  await expect(page.getByTestId('special-1-from')).toHaveValue(ekle(gun, 20))
  await expect(page.getByTestId('special-1-through')).toHaveValue(ekle(gun, 20))
  await expect(page.getByTestId('special-1-closed')).toBeChecked()
  await kanit(page, page.getByTestId('branch-special-hours'), '0027-ac2.png')

  await page.getByTestId('special-1-through').fill(ekle(gun, 19))
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('error-specialHours.1.through')).toHaveText(
    'Bitiş başlangıçtan önce olamaz.',
  )
  await expect(page.getByTestId('special-1-through')).toHaveValue(ekle(gun, 19))
  await kanit(page, page.getByTestId('special-1'), '0027-ac22.png')

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
  await page.goto(`/admin/branches/${id}`)
  const ar = page.getByTestId('branch-special-hours')
  await expect(ar.locator('legend')).toHaveText('الأيام الخاصة والعطلات')
  expect(await ar.evaluate((el) => getComputedStyle(el).direction)).toBe('rtl')
  await kanit(page, ar, '0027-ac1-ar.png')
})

test('0027/AC-22, QA 2 BULGU-1: Var olanın önüne sıralanan yeni kaydın hatası doğru kaydın yanında; çakışma sıralı kayıtta; odak hatalı alanda', async ({
  page,
}) => {
  const ad = benzersiz('Sıra')
  const id = await subeAc(page.request, ad, PROGRAMLI)
  const g = await bugun(page.request, ad)
  expect(
    (
      await ozelYaz(page.request, id, [{ from: ekle(g, 20), through: ekle(g, 25), intervals: [] }])
    ).status(),
  ).toBe(200)
  await page.goto(`/admin/branches/${id}`)
  await expect(page.getByTestId('special-0-from')).toHaveValue(ekle(g, 20))

  // Yeni kayıt başa sıralanır ve bitişi başlangıçtan önce: hata YENİ kaydın yanında.
  await page.getByTestId('special-add').click()
  await page.getByTestId('special-1-from').fill(ekle(g, 5))
  await page.getByTestId('special-1-through').fill(ekle(g, 4))
  await page.getByTestId('branch-save').click()
  const hata = page.getByTestId('error-specialHours.0.through')
  await expect(hata).toHaveText('Bitiş başlangıçtan önce olamaz.')
  await expect(page.getByTestId('special-0').getByTestId('special-0-from')).toHaveValue(ekle(g, 5))
  await expect(page.getByTestId('special-0-through')).toBeFocused()

  // Çakışma: yeni kayıt (g+10–g+21) var olanın (g+20–g+25) önünde; hata sıralı ikinci kayıtta.
  await page.getByTestId('special-0-from').fill(ekle(g, 10))
  await page.getByTestId('special-0-through').fill(ekle(g, 21))
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('error-specialHours.1.from')).toBeVisible()
  await expect(page.getByTestId('special-1').getByTestId('special-1-from')).toHaveValue(ekle(g, 20))
  await expect(page.getByTestId('special-1-from')).toBeFocused()
})

// ----- Altlık (D9–D11) -----

test('0027/AC-48, AC-53, AC-55, AC-56, AC-57: Altlıkta "şu an" satırı adın altında sunucunun durumuyla; "Bugün" tek satırda; "Özel günler" from sırasıyla; programsızda satır yok', async ({
  page,
}) => {
  const ad = benzersiz('Altlık')
  const yalin = benzersiz('Yalın')
  const programsiz = benzersiz('Programsız')
  const id = await subeAc(page.request, ad, PROGRAMLI)
  await subeAc(page.request, yalin, PROGRAMLI)
  await subeAc(page.request, programsiz, null)
  const g = await bugun(page.request, ad)
  expect(
    (
      await ozelYaz(page.request, id, [
        { from: ekle(g, 10), intervals: [{ start: '10:00', end: '16:00' }] },
        { from: ekle(g, 3), intervals: [] },
        { from: ekle(g, 5), through: ekle(g, 7), intervals: [] },
      ])
    ).status(),
  ).toBe(200)

  await page.goto('/tr/')
  const oge = altliktakiSube(page, ad)
  await expect(oge.getByTestId('branch-hours')).toBeVisible()
  const durum = (await vitrinSubesi(page.request, ad)).openNow!
  await expect(oge.getByTestId('branch-now')).toHaveText(beklenenSatir(durum))
  // D11 — sıra: ad · "şu an" · adres · telefon · haftalık · özel günler.
  expect(
    await oge.evaluate((li) =>
      [...li.children].map((el) => el.getAttribute('data-testid') ?? el.tagName),
    ),
  ).toEqual(['P', 'branch-now', 'P', 'A', 'branch-hours', 'branch-special'])
  expect(await oge.getByTestId('branch-now').getAttribute('aria-live')).toBeNull()

  const bugunler = oge.locator('[aria-current="date"]')
  await expect(bugunler).toHaveCount(1)
  await expect(bugunler.locator('.home-footer__today')).toHaveText('Bugün')

  const ozel = oge.getByTestId('branch-special')
  await expect(ozel.locator('p')).toHaveText('Özel günler')
  const tarih = (t: string) => `${Number(t.slice(8))} ${AY_TR[Number(t.slice(5, 7)) - 1]}`
  await expect(ozel.locator('dd')).toHaveText(['Kapalı', 'Kapalı', '10:00–16:00'])
  await expect(ozel.locator('.home-footer__day').first()).toHaveText(tarih(ekle(g, 3)))
  await kanit(page, oge, '0027-ac48.png')

  await expect(altliktakiSube(page, yalin).getByTestId('branch-special')).toHaveCount(0)
  await expect(altliktakiSube(page, programsiz).getByTestId('branch-now')).toHaveCount(0)
  await expect(altliktakiSube(page, programsiz).getByTestId('branch-hours')).toHaveCount(0)

  await page.goto('/ar/')
  const ar = page.getByTestId('branch').filter({ hasText: `فرع ${ad}` })
  await expect(ar.getByTestId('branch-now')).toHaveText(/^(مفتوح الآن|مغلق الآن)/u)
  expect(await ar.getByTestId('branch-now').evaluate((el) => getComputedStyle(el).direction)).toBe(
    'rtl',
  )
  await kanit(page, ar, '0027-ac51-ar.png')
})

test('0027/AC-58, AC-101: Tarayıcı Asia/Tokyo +3 gün ve America/Los_Angeles iken "şu an" satırı ve "Bugün" sunucunun yanıtını yazar', async ({
  page,
  browser,
}) => {
  const ad = benzersiz('Saat Farkı')
  await subeAc(page.request, ad, PROGRAMLI)

  for (const [bolge, kayma] of [
    ['Asia/Tokyo', 3 * 86_400_000],
    ['America/Los_Angeles', 0],
  ] as const) {
    const baglam = await browser.newContext({ timezoneId: bolge, locale: 'tr-TR' })
    try {
      const sayfa = await baglam.newPage()
      await sayfa.clock.install({ time: new Date(Date.now() + kayma) })
      await sayfa.goto('/tr/')
      const oge = sayfa.getByTestId('branch').filter({ hasText: ad })
      await expect(oge.getByTestId('branch-hours')).toBeVisible()
      const durum = (await vitrinSubesi(page.request, ad)).openNow!
      await expect(oge.getByTestId('branch-now'), bolge).toHaveText(beklenenSatir(durum))
      await expect(oge.locator('[aria-current="date"] .home-footer__day'), bolge).toHaveText(
        GUN_TR[haftaGunu(durum.today)]!,
      )
    } finally {
      await baglam.close()
    }
  }
})

// ----- Ölçümler (D11, D15) -----

/** Saat alanı: haftalık `dl`'nin üst kenarından (kenar boşluğu dâhil) son öğenin alt kenarına. */
async function saatAlani(page: Page, ad: string) {
  return altliktakiSube(page, ad).evaluate((li) => {
    const dl = li.querySelector<HTMLElement>('[data-testid="branch-hours"]')!
    const son = (
      li.querySelector<HTMLElement>('[data-testid="branch-special"]') ?? dl
    ).getBoundingClientRect()
    return (
      son.bottom -
      dl.getBoundingClientRect().top +
      parseFloat(getComputedStyle(dl).marginBlockStart)
    )
  })
}

async function iskelet(page: Page, ad: string) {
  return altliktakiSube(page, ad)
    .getByTestId('branch-hours-skeleton')
    .evaluate((el) => {
      const r = el.getBoundingClientRect()
      return r.height + parseFloat(getComputedStyle(el).marginBlockStart)
    })
}

for (const [etiket, dil, genislik, saatler, ozelSayisi] of [
  ['AC-75, AC-77 (en kötü, ar, 320 px)', 'ar', 320, HER_GUN_IKI, 12],
  // QA 2 BULGU-2 — en uzun tarih etiketi: İngilizce, ay aşan aralık + "Today", 320 px.
  ['AC-75, AC-77, QA 2 BULGU-2 (en kötü, en, 320 px)', 'en', 320, HER_GUN_IKI, 12],
  // 800 px'te altlık üç sütun, şube sütunu 245 px (1280 px'te 379): kat kararı görünümden değil sütundan.
  ['QA 2 BULGU-2 (en kötü, en, 800 px — 245 px sütun)', 'en', 800, HER_GUN_IKI, 12],
  ['AC-76 (programlı, tr, 1280 px)', 'tr', 1280, PROGRAMLI, 0],
  // Geniş ekranda iki aralık tek satıra sığar: "her aralık kendi satırında" kuralını yalnız bu ölçer.
  ['AC-75 ek (en kötü, tr, 1280 px)', 'tr', 1280, HER_GUN_IKI, 12],
] as const) {
  test(`0027/${etiket}, AC-68: Evre B iskeleti parça yüklenince açılan saat alanına ±1 px eşit; alttaki blok kaymaz; şube bloğundan layout-shift yok`, async ({
    page,
  }) => {
    const ad = benzersiz('Ölçü')
    const id = await subeAc(page.request, ad, saatler)
    const g = await bugun(page.request, ad)
    if (ozelSayisi > 0) {
      // İlk kayıt bugünü kapsar ve ay aşar (aralık etiketi + "Bugün" aynı satırda); kalanlar ardından.
      const [y, a] = g.split('-').map(Number)
      const ayAsan = new Date(Date.UTC(y!, a!, 2)).toISOString().slice(0, 10)
      const kayitlar = [
        { from: g, through: ayAsan, intervals: IKI },
        ...Array.from({ length: ozelSayisi - 1 }, (_, i) => ({
          from: ekle(ayAsan, 1 + 2 * i),
          intervals: IKI,
        })),
      ]
      expect((await ozelYaz(page.request, id, kayitlar)).status()).toBe(200)
    }

    await page.setViewportSize({ width: genislik, height: 900 })
    const fontuBirak = await fontlariBeklet(page)
    const parcayiBirak = await parcayiBeklet(page)
    await page.goto(`/${dil}/`, { waitUntil: 'domcontentloaded' })
    const adi = dil === 'ar' ? `فرع ${ad}` : ad
    const oge = altliktakiSube(page, adi)
    await expect(oge.getByTestId('branch-hours-skeleton')).toBeVisible()
    // AC-68 — evre B: ad, adres, telefon basılı; "şu an" kutusu yerinde.
    await expect(oge.locator('.home-footer__branch-address')).toBeVisible()
    await expect(oge.getByTestId('branch-now')).toBeVisible()
    await fontuBirak()
    await oge.scrollIntoViewIfNeeded()

    await page.evaluate(() => {
      const w = window as unknown as { kaymalar: string[] }
      w.kaymalar = []
      new PerformanceObserver((liste) => {
        for (const girdi of liste.getEntries() as unknown as {
          sources: { node: Node | null }[]
        }[]) {
          for (const kaynak of girdi.sources) {
            const el = kaynak.node instanceof Element ? kaynak.node : kaynak.node?.parentElement
            const blok = document.querySelector('[data-testid="branch-list"]')
            if (el && blok && (blok.contains(el) || blok.compareDocumentPosition(el) & 4)) {
              w.kaymalar.push(el.className || el.tagName)
            }
          }
        }
      }).observe({ type: 'layout-shift' })
    })

    const once = await iskelet(page, adi)
    const sonrakiBlok = page.locator('.home-footer__nav').first()
    const sonrakiOnce = await sonrakiBlok.evaluate((el) => el.getBoundingClientRect().top)
    const kutuOnce = await oge
      .getByTestId('branch-now')
      .evaluate((el) => el.getBoundingClientRect().height)

    parcayiBirak()
    await expect(oge.getByTestId('branch-hours')).toBeVisible()
    await expect(oge.getByTestId('branch-now')).not.toHaveText('')
    await page.waitForTimeout(300)

    // QA 2 BULGU-2 — hiçbir satırda tarih/gün ile aralık üst üste binmez; şube taşmaz.
    const binenler = await oge.evaluate((li) =>
      [...li.querySelectorAll('.home-footer__hours-row')].flatMap((row) => {
        // Kutu değil İÇERİK: `dt` 9rem'e kadar daralır ve `nowrap` metni kutusundan taşar; yalnız
        // kutular karşılaştırılırsa metin `dd`'nin üstüne binerken test yeşil kalıyordu (Range ile ölç).
        const icerik = document.createRange()
        icerik.selectNodeContents(row.querySelector('dt')!)
        const dt = icerik.getBoundingClientRect()
        const dd = row.querySelector('dd')!.getBoundingClientRect()
        const kesisir =
          dt.left < dd.right - 0.5 &&
          dd.left < dt.right - 0.5 &&
          dt.top < dd.bottom - 0.5 &&
          dd.top < dt.bottom - 0.5
        // Kutu şubenin içinde kalır (sağdan sola dahil); sığmayan satır taşar ve komşu sütuna biner.
        const kutu = li.getBoundingClientRect()
        const tasar = [dt, dd].some((r) => r.left < kutu.left - 1 || r.right > kutu.right + 1)
        return kesisir || tasar ? [row.textContent] : []
      }),
    )
    expect(binenler, etiket).toEqual([])
    expect(await oge.evaluate((li) => li.scrollWidth <= li.clientWidth + 1), etiket).toBe(true)
    if (ozelSayisi > 0) {
      await expect(oge.getByTestId('branch-special').locator('[aria-current="date"]')).toHaveCount(
        1,
      )
    }

    const sonra = await saatAlani(page, adi)
    expect(Math.abs(sonra - once), `${etiket}: iskelet ${once}, alan ${sonra}`).toBeLessThanOrEqual(
      1,
    )
    const sonrakiSonra = await sonrakiBlok.evaluate((el) => el.getBoundingClientRect().top)
    expect(Math.abs(sonrakiSonra - sonrakiOnce), etiket).toBeLessThanOrEqual(1)
    expect(
      await oge.getByTestId('branch-now').evaluate((el) => el.getBoundingClientRect().height),
    ).toBe(kutuOnce)
    expect(
      await page.evaluate(() => (window as unknown as { kaymalar: string[] }).kaymalar),
    ).toEqual([])
    if (ozelSayisi > 0 && genislik === 320) {
      await kanit(page, oge, `0027-ac75-${dil}.png`)
    }
  })
}

test('0027/AC-69: "Şu an" kutusu dokuz metinde 320 px\'te aynı yükseklikte, iki satır; metin kesilmez (fontlar bekletilip serbest bırakılarak; QA 2 BULGU-6)', async ({
  page,
}) => {
  const ad = benzersiz('Kutu')
  await subeAc(page.request, ad, PROGRAMLI)
  await page.setViewportSize({ width: 320, height: 800 })
  const metinler: Record<string, [SuAn, string][]> = {
    tr: [],
    en: [],
    ar: [],
  }
  const acik: SuAn = {
    open: true,
    today: '2027-08-18',
    next: { date: '2027-08-18', time: '18:00' },
    refreshInSeconds: 3600,
  }
  const carsamba: SuAn = {
    open: false,
    today: '2027-08-16',
    next: { date: '2027-08-18', time: '09:00' },
    refreshInSeconds: 3600,
  }
  const ileri: SuAn = {
    open: false,
    today: '2027-08-10',
    next: { date: '2027-08-21', time: '09:00' },
    refreshInSeconds: 3600,
  }
  const eylul: SuAn = {
    open: false,
    today: '2027-09-10',
    next: { date: '2027-09-21', time: '09:00' },
    refreshInSeconds: 3600,
  }
  metinler['tr'] = [
    [acik, 'Şu an açık · Kapanış 18:00'],
    [carsamba, 'Şu an kapalı · Açılış Çarşamba 09:00'],
    [ileri, 'Şu an kapalı · Açılış 21 Ağustos 09:00'],
  ]
  metinler['en'] = [
    [acik, 'Open now · Closes 18:00'],
    [carsamba, 'Closed now · Opens Wednesday 09:00'],
    [eylul, 'Closed now · Opens 21 September 09:00'],
  ]
  metinler['ar'] = [
    [acik, 'مفتوح الآن · يغلق 18:00'],
    [carsamba, 'مغلق الآن · يفتح الأربعاء 09:00'],
    [eylul, 'مغلق الآن · يفتح 21 سبتمبر 09:00'],
  ]

  const yukseklikler: number[] = []
  for (const [dil, liste] of Object.entries(metinler)) {
    for (const [durum, beklenen] of liste) {
      // Gerçek yanıt alınır, yalnız bu şubenin `openNow`'u değiştirilir (şekil gerçek kalır).
      await page.unroute('**/api/dealership/gallery**')
      await page.route('**/api/dealership/gallery**', async (route) => {
        const yanit = await route.fetch()
        const govde = (await yanit.json()) as { branches: { name: string; openNow: SuAn | null }[] }
        for (const b of govde.branches) {
          if (b.name.includes(ad)) b.openNow = durum
        }
        await route.fulfill({ response: yanit, json: govde })
      })
      // QA 2 BULGU-6 — font gerçekten bekletilir (yönlendirme HTTP önbelleğini kapatır), yüklenirken
      // doğrulanır, sonra serbest bırakılır: ölçüm web fontuyla, şansa bırakılmadan.
      await page.unroute('**/*.woff2')
      const fontuBirak = await fontlariBeklet(page)
      await page.goto(`/${dil}/`, { waitUntil: 'domcontentloaded' })
      await expect(page.getByTestId('home-footer')).toBeAttached()
      expect(await page.evaluate(() => document.fonts.status), beklenen).toBe('loading')
      await fontuBirak()
      const kutu = page.getByTestId('branch').filter({ hasText: ad }).getByTestId('branch-now')
      await expect(kutu).toHaveText(beklenen)
      const olcu = await kutu.evaluate((el) => ({
        h: el.getBoundingClientRect().height,
        satir: parseFloat(getComputedStyle(el).lineHeight),
        tasma: el.scrollHeight > el.clientHeight + 1,
      }))
      expect(olcu.tasma, beklenen).toBe(false)
      expect(Math.abs(olcu.h - 2 * olcu.satir), beklenen).toBeLessThanOrEqual(1)
      yukseklikler.push(olcu.h)
    }
  }
  expect(new Set(yukseklikler).size).toBe(1)
})

test('0027/AC-71: Ana sayfa fontlar bekletilerek açılır, altlığa kaydırılır; şube bloğu ve ardındakilerde layout-shift kaynağı yok', async ({
  page,
}) => {
  const ad = benzersiz('Kayma')
  await subeAc(page.request, ad, PROGRAMLI, [])
  await page.addInitScript(() => {
    const w = window as unknown as { kaymalar: string[] }
    w.kaymalar = []
    new PerformanceObserver((liste) => {
      for (const girdi of liste.getEntries() as unknown as { sources: { node: Node | null }[] }[]) {
        for (const kaynak of girdi.sources) {
          const el = kaynak.node instanceof Element ? kaynak.node : kaynak.node?.parentElement
          const blok = document.querySelector('[data-testid="branch-list"]')
          if (el && blok && (blok.contains(el) || blok.compareDocumentPosition(el) & 4)) {
            w.kaymalar.push(el.className || el.tagName)
          }
        }
      }
    }).observe({ type: 'layout-shift', buffered: true })
  })
  const fontuBirak = await fontlariBeklet(page)
  await page.goto('/tr/', { waitUntil: 'domcontentloaded' })
  await fontuBirak()
  await page.getByTestId('home-footer').scrollIntoViewIfNeeded()
  await expect(altliktakiSube(page, ad).getByTestId('branch-hours')).toBeVisible()
  await page.waitForTimeout(500)
  expect(await page.evaluate(() => (window as unknown as { kaymalar: string[] }).kaymalar)).toEqual(
    [],
  )
})

test('0027/AC-78: 412×823 ve 1350×940 görünümde altlığın üst kenarı ilk görünüm alanının altında', async ({
  page,
}) => {
  for (const [w, h] of [
    [412, 823],
    [1350, 940],
  ] as const) {
    await page.setViewportSize({ width: w, height: h })
    await page.goto('/tr/')
    await expect(page.getByTestId('branch-list')).toBeAttached()
    const ust = await page
      .getByTestId('home-footer')
      .evaluate((el) => el.getBoundingClientRect().top)
    expect(ust, `${w}×${h}`).toBeGreaterThan(h)
  }
})
