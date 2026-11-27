import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0025` — şube çalışma saatleri, gerçek Host'a karşı: panelin "Çalışma saatleri" bölümü ve ana
 * sayfa altlığının saat listesi. jsdom'un taşımadığı şeyler burada ölçülür: gerçek saat alanı
 * (`type="time"`), gerçek RTL yerleşimi, tarayıcının saat dilimi (AC-49).
 *
 * <b>Paylaşılan durum.</b> Testler kendi şubelerini açar ve sonunda KALICI kapatır: kurulumun
 * iki şubesine ve `0014`'ün altlık kanıtlarına dokunulmaz. Kapatılmış şube vitrinde görünmez.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const API = '/api/admin/branches'
const GUNLER = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']

type Program = Record<string, { start: string; end: string }[]>

function program(gunler: Program = {}): Program {
  return Object.fromEntries(GUNLER.map((gun) => [gun, gunler[gun] ?? []]))
}

const HAFTA_ICI = { start: '09:00', end: '18:00' }
const PROGRAMLI = program({
  monday: [HAFTA_ICI],
  tuesday: [HAFTA_ICI],
  wednesday: [HAFTA_ICI],
  thursday: [HAFTA_ICI],
  friday: [HAFTA_ICI],
  saturday: [{ start: '10:00', end: '14:00' }],
})

const acilanlar: string[] = []

function benzersiz(onek: string) {
  return `${onek} ${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`
}

async function subeAc(
  api: APIRequestContext,
  ad: string,
  saatler: Program | null,
  saatDilimi = 'Europe/Istanbul',
): Promise<string> {
  const yanit = await api.post(API, {
    data: {
      names: { tr: ad, ar: `فرع ${ad}`, en: `${ad} Branch` },
      addresses: { tr: `${ad} Caddesi 1` },
      phone: '+902161230000',
      timeZone: saatDilimi,
      slug: benzersiz('e2e-saat').replace(/\s/g, '-').toLowerCase(),
      ...(saatler === null ? {} : { openingHours: saatler }),
    },
  })
  expect(yanit.status(), `POST branch → ${await yanit.text()}`).toBe(201)
  const { id } = (await yanit.json()) as { id: string }
  acilanlar.push(id)
  return id
}

async function sube(api: APIRequestContext, id: string) {
  return (await (await api.get(`${API}/${id}`)).json()) as {
    version: number
    status: string
    openingHours: Program | null
  }
}

/** `0028` AC-44 — haftalık liste iletişim sayfasının şube bölümünde. */
function iletisimSubesi(page: Page, ad: string) {
  return page.getByTestId('contact-branch').filter({ hasText: ad })
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

test("0025/AC-1, AC-2, AC-3, AC-7: Bölüm yedi günü gösterir, programsızda hepsi Kapalı; Pazartesi girilir, Salı'ya kopyalanır, kaydedilir ve yeniden açılınca durur", async ({
  page,
}) => {
  const id = await subeAc(page.request, benzersiz('Saat Paneli'), null)
  await page.goto(`/admin/branches/${id}`)
  const bolum = page.getByTestId('branch-opening-hours')
  await expect(bolum).toBeVisible()
  await expect(bolum.locator('.opening-hours__name')).toHaveText([
    'Pazartesi',
    'Salı',
    'Çarşamba',
    'Perşembe',
    'Cuma',
    'Cumartesi',
    'Pazar',
  ])
  await expect(bolum.locator('.opening-hours__closed')).toHaveCount(7)
  await expect(bolum).toContainText('Hiç saat girilmezse vitrinde saat gösterilmez')
  await kanit(page, bolum, '0025-ac2.png')

  await page.getByTestId('hours-add-monday').click()
  const pazartesi = page.getByTestId('hours-monday')
  await pazartesi.locator('input[aria-labelledby$="-start"]').fill('09:00')
  await pazartesi.locator('input[aria-labelledby$="-end"]').fill('18:00')
  await page.getByTestId('hours-copy-tuesday').click()
  await expect(page.getByTestId('hours-tuesday').locator('input[type="time"]')).toHaveCount(2)
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('dealership-saved')).toBeVisible()

  await page.goto(`/admin/branches/${id}`)
  await expect(
    page.getByTestId('hours-monday').locator('input[aria-labelledby$="-start"]'),
  ).toHaveValue('09:00')
  await expect(
    page.getByTestId('hours-tuesday').locator('input[aria-labelledby$="-end"]'),
  ).toHaveValue('18:00')
  await expect(
    page.getByTestId('branch-opening-hours').locator('.opening-hours__closed'),
  ).toHaveCount(5)
  await kanit(page, page.getByTestId('branch-opening-hours'), '0025-ac3.png')
  expect((await sube(page.request, id)).openingHours?.['monday']).toEqual([HAFTA_ICI])
})

test('0025/AC-22, AC-39: Hatalı aralık alanın yanında çevrilmiş hatayla; ar panelinde bölüm Arapça ve sağdan sola, saat değerleri HH:mm', async ({
  page,
}) => {
  const id = await subeAc(page.request, benzersiz('Saat Hata'), program({ monday: [HAFTA_ICI] }))
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
  const bolum = page.getByTestId('branch-opening-hours')
  await expect(bolum.locator('legend')).toHaveText('ساعات العمل')
  await expect(bolum.locator('.opening-hours__name').first()).toHaveText(/\p{Script=Arabic}/u)
  expect(await bolum.evaluate((el) => getComputedStyle(el).direction)).toBe('rtl')
  await expect(
    page.getByTestId('hours-monday').locator('input[aria-labelledby$="-start"]'),
  ).toHaveValue('09:00')

  await page.getByTestId('hours-monday').locator('input[aria-labelledby$="-end"]').fill('08:00')
  await page.getByTestId('branch-save').click()
  await expect(page.getByTestId('error-openingHours.monday.0.end')).toHaveText(
    'يجب أن تكون النهاية بعد البداية.',
  )
  await expect(
    page.getByTestId('hours-monday').locator('input[aria-labelledby$="-end"]'),
  ).toHaveValue('08:00')
  await kanit(page, bolum, '0025-ac39-ar.png')
})

test('0028/AC-44, 0025/AC-44, AC-45, AC-46, AC-47, AC-48: İletişim sayfasında programlı şubenin yedi satırı tr/ar/en; iki aralık sırayla; programsız şubede liste yok', async ({
  page,
}) => {
  const ad = benzersiz('Saatli')
  const programsizAd = benzersiz('Saatsiz')
  await subeAc(page.request, ad, {
    ...PROGRAMLI,
    wednesday: [
      { start: '09:00', end: '13:00' },
      { start: '16:00', end: '21:00' },
    ],
  })
  await subeAc(page.request, programsizAd, null)

  await page.goto('/tr/contact')
  const tr = iletisimSubesi(page, ad)
  await expect(tr.getByTestId('branch-hours').locator('.branch-clock__day')).toHaveText([
    'Pazartesi',
    'Salı',
    'Çarşamba',
    'Perşembe',
    'Cuma',
    'Cumartesi',
    'Pazar',
  ])
  await expect(tr.getByTestId('branch-hours').locator('dd')).toHaveText([
    '09:00–18:00',
    '09:00–18:00',
    '09:00–13:0016:00–21:00',
    '09:00–18:00',
    '09:00–18:00',
    '10:00–14:00',
    'Kapalı',
  ])
  await expect(tr.getByTestId('branch-hours').locator('dd').nth(2).locator('span')).toHaveText([
    '09:00–13:00',
    '16:00–21:00',
  ])
  await expect(iletisimSubesi(page, programsizAd).getByTestId('branch-hours')).toHaveCount(0)
  await kanit(page, tr, '0025-ac44.png')

  await page.goto('/ar/contact')
  const ar = page.getByTestId('contact-branch').filter({ hasText: `فرع ${ad}` })
  await expect(ar.getByTestId('branch-hours').locator('dd').last()).toHaveText('مغلق')
  await expect(ar.getByTestId('branch-hours').locator('.branch-clock__day').first()).toHaveText(
    /\p{Script=Arabic}/u,
  )
  await expect(ar.getByTestId('branch-hours').locator('dd').first()).toHaveText('09:00–18:00')
  expect(
    await ar
      .locator('dd span')
      .first()
      .evaluate((el) => getComputedStyle(el).direction),
  ).toBe('ltr')
  await kanit(page, ar, '0025-ac47-ar.png')

  await page.goto('/en/contact')
  const en = iletisimSubesi(page, `${ad} Branch`)
  await expect(en.getByTestId('branch-hours').locator('.branch-clock__day').first()).toHaveText(
    'Monday',
  )
  await expect(en.getByTestId('branch-hours').locator('dd').first()).toHaveText('09:00–18:00')
  await expect(en.getByTestId('branch-hours').locator('dd').last()).toHaveText('Closed')
})

test('0028/AC-44, 0025/AC-49: Şube Asia/Kuwait, tarayıcı Europe/Istanbul iken iletişim sayfası Pazartesi için 09:00–18:00 gösterir', async ({
  page,
  browser,
}) => {
  const ad = benzersiz('Kuveyt')
  await subeAc(page.request, ad, program({ monday: [HAFTA_ICI] }), 'Asia/Kuwait')

  const istanbul = await browser.newContext({ timezoneId: 'Europe/Istanbul', locale: 'tr-TR' })
  try {
    const sayfa = await istanbul.newPage()
    await sayfa.goto('/tr/contact')
    const oge = sayfa.getByTestId('contact-branch').filter({ hasText: ad })
    await expect(oge.getByTestId('branch-hours').locator('.branch-clock__day').first()).toHaveText(
      'Pazartesi',
    )
    await expect(oge.getByTestId('branch-hours').locator('dd').first()).toHaveText('09:00–18:00')
  } finally {
    await istanbul.close()
  }
})
