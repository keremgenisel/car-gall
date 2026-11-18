import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { girisYap, oturumuBirakAsync, yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0017` — içerik sayfaları gerçek Host'a karşı: adresler, "bulunamadı", eski slug'ın 301'i,
 * dil değiştirici, SSS, altlığın Kurumsal kolonu, talep formunun KVKK bağlantısı, gövdenin
 * enjeksiyona karşı çizimi ve panelin "İçerik" bölümü.
 *
 * Sistem sayfaları ve SSS GLOBAL'dir: onlara dokunan testler yalnız masaüstü profilinde ve
 * SIRAYLA koşar; her test istediği durumu kendisi kurar. Hazırlık panelin API'siyle yapılır,
 * iddialar ziyaretçinin tarayıcısında ölçülür.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const KOK = '/api/admin/content'
const ID = {
  about: '0192c000-0000-7000-8000-0000c0a70001',
  privacy: '0192c000-0000-7000-8000-0000c0a70002',
  cookies: '0192c000-0000-7000-8000-0000c0a70003',
  faq: '0192c000-0000-7000-8000-0000c0a70004',
} as const

interface PanelSayfa {
  readonly version: number
  readonly locales: readonly {
    readonly locale: string
    readonly status: string
    readonly slug: string
  }[]
}

async function surum(api: APIRequestContext, id: string): Promise<PanelSayfa> {
  const yanit = await api.get(`${KOK}/pages/${id}`)
  expect(yanit.ok(), `GET ${id}`).toBe(true)
  return (await yanit.json()) as PanelSayfa
}

async function yaz(
  api: APIRequestContext,
  id: string,
  locale: string,
  title: string,
  body: string,
  slug: string,
) {
  const { version } = await surum(api, id)
  const yanit = await api.put(`${KOK}/pages/${id}`, {
    data: { version, locales: [{ locale, title, body, slug }] },
  })
  expect(yanit.ok(), `PUT ${id} ${locale} → ${yanit.status()} ${await yanit.text()}`).toBe(true)
}

async function yayin(api: APIRequestContext, id: string, locale: string, publish: boolean) {
  const { version } = await surum(api, id)
  const yanit = await api.post(`${KOK}/pages/${id}/${publish ? 'publish' : 'unpublish'}`, {
    data: { version, locale },
  })
  expect(yanit.ok(), `${publish ? 'publish' : 'unpublish'} ${id} ${locale}`).toBe(true)
}

async function yazVeYayinla(
  api: APIRequestContext,
  id: string,
  locale: string,
  title: string,
  body: string,
  slug: string,
) {
  await yaz(api, id, locale, title, body, slug)
  await yayin(api, id, locale, true)
}

/** Sistem sayfalarının bütün dillerini yayından kaldırır ve SSS girdilerini siler. */
async function sifirla(api: APIRequestContext) {
  for (const id of Object.values(ID)) {
    const sayfa = await surum(api, id)
    for (const satir of sayfa.locales.filter((item) => item.status === 'published')) {
      await yayin(api, id, satir.locale, false)
    }
  }

  const girdiler = (await (await api.get(`${KOK}/faq`)).json()) as { id: string; version: number }[]
  for (const girdi of girdiler) {
    await api.delete(`${KOK}/faq/${girdi.id}?version=${girdi.version}`)
  }
}

async function sss(
  api: APIRequestContext,
  metinler: { tr: [string, string]; ar?: [string, string] },
  yayinda: string[],
) {
  const locales = [{ locale: 'tr', question: metinler.tr[0], answer: metinler.tr[1] }]
  if (metinler.ar) {
    locales.push({ locale: 'ar', question: metinler.ar[0], answer: metinler.ar[1] })
  }

  const yanit = await api.post(`${KOK}/faq`, { data: { locales } })
  expect(yanit.status()).toBe(201)
  const { id, version: ilkSurum } = (await yanit.json()) as { id: string; version: number }
  let version = ilkSurum

  for (const locale of yayinda) {
    const yayinYaniti = await api.post(`${KOK}/faq/${id}/publish`, { data: { version, locale } })
    expect(yayinYaniti.ok()).toBe(true)
    version = ((await yayinYaniti.json()) as { version: number }).version
  }

  return id
}

function benzersiz(onek: string) {
  return `${onek}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 5)}`
}

async function gir(page: Page) {
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
}

test.beforeEach(async ({ page }, testInfo) => {
  test.skip(
    testInfo.project.name !== 'masaustu',
    'Sistem sayfaları global; içerik testleri tek profilde ve sırayla koşar.',
  )
  await gir(page)
})

test.afterAll(async ({ browser }, testInfo) => {
  if (testInfo.project.name !== 'masaustu') {
    return
  }

  const page = await browser.newPage()
  await gir(page)
  await sifirla(page.request)
  await page.close()
})

test('0017/AC-37, AC-38, AC-40, AC-41: /tr/foo listeyi değil bulunamadı ekranını gösterir; /tr/foo/bar listeye düşer; belge isteği 200', async ({
  page,
}) => {
  const belge = await page.goto('/tr/foo')
  expect(belge?.status()).toBe(200)
  await expect(page.getByTestId('content-not-found')).toBeVisible()
  await expect(page.getByTestId('result-count')).toHaveCount(0)
  await kanit(page, '[data-testid="content-not-found"]', '0017-ac40.png')

  await page.goto('/tr/foo/bar')
  await expect(page.getByTestId('result-count')).toBeVisible()
  await expect(page.getByTestId('content-not-found')).toHaveCount(0)
})

test('0017/AC-34, AC-58, AC-59, AC-60: yayındaki sayfa basılır; dil değiştirici yayındaki dile slug ile, yayında olmayana ana sayfaya gider', async ({
  page,
}) => {
  await sifirla(page.request)
  await yazVeYayinla(
    page.request,
    ID.about,
    'tr',
    'Hakkımızda',
    'Biz bir galeriyiz.\n\n- güven\n- şeffaflık',
    'hakkimizda',
  )
  await yazVeYayinla(page.request, ID.about, 'en', 'About us', 'We are a gallery.', 'about-us')
  await yaz(page.request, ID.about, 'ar', 'من نحن', 'نحن معرض.', 'man-nahnu')

  await page.goto('/tr/hakkimizda')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Hakkımızda')
  await expect(page.getByTestId('content-body')).toContainText('Biz bir galeriyiz.')
  await kanit(page, '[data-testid="content-page"]', '0017-ac34.png')

  await expect(page.getByRole('link', { name: 'English' })).toHaveAttribute('href', '/en/about-us')
  await expect(page.getByRole('link', { name: 'العربية' })).toHaveAttribute('href', '/ar/')

  await page.getByRole('link', { name: 'English' }).click()
  await expect(page).toHaveURL(/\/en\/about-us$/)
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('About us')
})

test('0017/AC-121, AC-126: /ar/ içerik sayfası RTL akar; /en/ sayfası İngilizce arayüz metni taşır', async ({
  page,
}) => {
  await sifirla(page.request)
  await yazVeYayinla(
    page.request,
    ID.about,
    'ar',
    'من نحن',
    '## تاريخنا\n\nنحن معرض سيارات.\n\n- الثقة\n- الشفافية',
    'man-nahnu',
  )
  await yazVeYayinla(page.request, ID.about, 'en', 'About us', 'We are a gallery.', 'about-us')

  await page.goto('/ar/man-nahnu')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('من نحن')
  expect(
    await page.getByTestId('content-body').evaluate((oge) => getComputedStyle(oge).direction),
  ).toBe('rtl')
  await kanit(page, '[data-testid="content-page"]', '0017-ac121-ar.png')

  await page.goto('/en/about-us')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('About us')
  await expect(page.locator('html')).toHaveAttribute('lang', 'en')
  // Arayüz metni: dil değiştiricinin adı. Galerinin adı veridir; dil adları bilerek kendi
  // dillerinde yazılır ("Türkçe", "العربية") ve arayüz metni sayılmaz.
  const etiket = (await page.locator('header nav').getAttribute('aria-label')) ?? ''
  expect(etiket.length).toBeGreaterThan(0)
  expect(etiket).not.toMatch(/[çğıöşüÇĞİÖŞÜ]|[\u0600-\u06FF]/)
})

test('0017/AC-51, AC-52, AC-56 (tarayıcı): yayındaki slug değişince eski adres yeni adrese yönlenir; başka dilde yönlenmez', async ({
  page,
}) => {
  await sifirla(page.request)
  await yazVeYayinla(
    page.request,
    ID.privacy,
    'tr',
    'KVKK Aydınlatma Metni',
    'Kişisel verileriniz…',
    'kvkk',
  )
  await yaz(
    page.request,
    ID.privacy,
    'tr',
    'KVKK Aydınlatma Metni',
    'Kişisel verileriniz…',
    'kvkk-aydinlatma',
  )

  await page.goto('/tr/kvkk')
  await expect(page).toHaveURL(/\/tr\/kvkk-aydinlatma$/)
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('KVKK Aydınlatma Metni')

  await page.goto('/en/kvkk')
  await expect(page).toHaveURL(/\/en\/kvkk$/)
  await expect(page.getByTestId('content-not-found')).toBeVisible()

  // Geri al: diğer testler `kvkk` slug'ını bekler.
  await yaz(page.request, ID.privacy, 'tr', 'KVKK Aydınlatma Metni', 'Kişisel verileriniz…', 'kvkk')
})

test('0017/AC-75, AC-81, AC-82, AC-84: SSS dil başına yayındaki girdileri gövdenin altında gösterir; girdisiz SSS altlıkta görünmez', async ({
  page,
}) => {
  await sifirla(page.request)
  await yazVeYayinla(
    page.request,
    ID.faq,
    'tr',
    'Sıkça Sorulan Sorular',
    'Galerimizle ilgili sorular.',
    'sss',
  )
  await yazVeYayinla(page.request, ID.faq, 'ar', 'الأسئلة الشائعة', 'أسئلة حول معرضنا.', 'al-asila')

  // AC-82 — girdi yokken SSS altlıkta yok.
  await page.goto('/tr/')
  await expect(page.getByTestId('home-footer')).toBeVisible()
  await expect(page.getByTestId('footer-corporate')).toHaveCount(0)

  await sss(
    page.request,
    { tr: ['Takas yapıyor musunuz?', 'Evet.'], ar: ['هل تقبلون المقايضة؟', 'نعم.'] },
    ['tr', 'ar'],
  )
  await sss(
    page.request,
    {
      tr: ['Kredi imkânı var mı?', 'Evet, **anlaşmalı bankalarla**.'],
      ar: ['هل يوجد تمويل؟', 'نعم.'],
    },
    ['tr'],
  )
  await sss(
    page.request,
    { tr: ['Ekspertiz raporu veriyor musunuz?', 'Evet.'], ar: ['هل تقدمون تقرير فحص؟', 'نعم.'] },
    ['tr', 'ar'],
  )

  await page.goto('/tr/sss')
  const sorular = page.getByTestId('content-faq-item').locator('h2')
  await expect(sorular).toHaveText([
    'Takas yapıyor musunuz?',
    'Kredi imkânı var mı?',
    'Ekspertiz raporu veriyor musunuz?',
  ])
  await kanit(page, '[data-testid="content-page"]', '0017-ac75.png')

  await page.goto('/ar/al-asila')
  await expect(page.getByTestId('content-faq-item').locator('h2')).toHaveText([
    'هل تقبلون المقايضة؟',
    'هل تقدمون تقرير فحص؟',
  ])

  await page.goto('/tr/')
  await expect(page.getByTestId('footer-corporate').getByRole('link')).toHaveText([
    'Sıkça Sorulan Sorular',
  ])
})

test('0017/AC-86, AC-89, AC-90, AC-122: Kurumsal kolonu sabit sırada; hiçbiri yokken kolon yok; dil başına; /ar/ kolonu RTL', async ({
  page,
}) => {
  await sifirla(page.request)
  await page.goto('/tr/')
  await expect(page.getByTestId('home-footer')).toBeVisible()
  await expect(page.getByTestId('footer-corporate')).toHaveCount(0)

  await yazVeYayinla(page.request, ID.cookies, 'tr', 'Çerez Politikası', 'Çerezler…', 'cerezler')
  await yazVeYayinla(
    page.request,
    ID.privacy,
    'tr',
    'KVKK Aydınlatma Metni',
    'Kişisel verileriniz…',
    'kvkk',
  )
  await yazVeYayinla(page.request, ID.faq, 'tr', 'Sıkça Sorulan Sorular', 'Sorular.', 'sss')
  await sss(page.request, { tr: ['Takas yapıyor musunuz?', 'Evet.'] }, ['tr'])
  await yazVeYayinla(page.request, ID.about, 'tr', 'Hakkımızda', 'Biz.', 'hakkimizda')
  await yazVeYayinla(page.request, ID.privacy, 'ar', 'سياسة الخصوصية', 'بياناتكم…', 'al-khususiya')

  await page.goto('/tr/')
  const kolon = page.getByTestId('footer-corporate')
  await expect(kolon.getByRole('link')).toHaveText([
    'Hakkımızda',
    'Sıkça Sorulan Sorular',
    'KVKK Aydınlatma Metni',
    'Çerez Politikası',
  ])
  await expect(kolon.getByRole('link').first()).toHaveAttribute('href', '/tr/hakkimizda')
  await kanit(page, '[data-testid="home-footer"]', '0017-ac86.png')

  await page.goto('/ar/')
  const arKolon = page.getByTestId('footer-corporate')
  await expect(arKolon.getByRole('link')).toHaveText(['سياسة الخصوصية'])
  expect(await arKolon.evaluate((oge) => getComputedStyle(oge).direction)).toBe('rtl')
  await kanit(page, '[data-testid="home-footer"]', '0017-ac122-ar.png')
})

test('0017/AC-93, AC-94, AC-95, AC-97: talep formunun izin satırı privacy yayındayken bağlantı taşır; değilken hiç bağlantı yok', async ({
  page,
}) => {
  await sifirla(page.request)
  await yazVeYayinla(
    page.request,
    ID.privacy,
    'tr',
    'KVKK Aydınlatma Metni',
    'Kişisel verileriniz…',
    'kvkk',
  )

  const liste = (await (
    await page.request.get('/api/catalog/showcase-listings?locale=tr&page=1&pageSize=1')
  ).json()) as {
    items: { slug: string }[]
  }
  const ilan = liste.items[0]!.slug

  await page.goto(`/tr/cars/${ilan}`)
  await expect(page.getByTestId('lead-privacy-link')).toHaveAttribute('href', '/tr/kvkk')
  await kanit(page, '.lead-form__consent', '0017-ac93.png')

  await page.goto(`/ar/cars/${ilan}`)
  await expect(page.locator('.lead-form__consent input[type="checkbox"]')).toBeVisible()
  await expect(page.locator('.lead-form__consent a')).toHaveCount(0)

  // AC-95 — bağlantı yokken de form doldurulup gönderilir ve talep kaydedilir (`0005`).
  const form = page.getByTestId('lead-form')
  await form.getByLabel(/اسمك/).fill('زائر الاختبار')
  await form.getByLabel(/^الهاتف$/).fill('+905321112244')
  await form.locator('.lead-form__consent input[type="checkbox"]').check()
  await form.locator('button[type="submit"]').click()
  await expect(page.getByTestId('lead-success')).toBeVisible()

  await yayin(page.request, ID.privacy, 'tr', false)
  await page.goto(`/tr/cars/${ilan}`)
  await expect(page.locator('.lead-form__consent input[type="checkbox"]')).toBeVisible()
  await expect(page.locator('.lead-form__consent a')).toHaveCount(0)
})

test('0017/AC-66, AC-67: ham HTML ve onerror girdisi gövdede metin olarak görünür; script/img oluşmaz ve alert çağrılmaz', async ({
  page,
}) => {
  const slug = benzersiz('enjeksiyon')
  const olustur = await page.request.post(`${KOK}/pages`, {
    data: {
      locales: [
        {
          locale: 'tr',
          title: 'Enjeksiyon',
          body: '<script>alert(1)</script>\n\n<img src=x onerror=alert(1)>',
          slug,
        },
      ],
    },
  })
  expect(olustur.status()).toBe(201)
  const { id } = (await olustur.json()) as { id: string }
  await yayin(page.request, id, 'tr', true)

  let alertCagrildi = false
  page.on('dialog', async (dialog) => {
    alertCagrildi = true
    await dialog.dismiss()
  })

  await page.goto(`/tr/${slug}`)
  const govde = page.getByTestId('content-body')
  await expect(govde).toContainText('<script>alert(1)</script>')
  await expect(govde).toContainText('<img src=x onerror=alert(1)>')
  await expect(govde.locator('script, img')).toHaveCount(0)
  expect(alertCagrildi).toBe(false)
  await kanit(page, '[data-testid="content-page"]', '0017-ac66.png')

  const { version } = await surum(page.request, id)
  await page.request.delete(`${KOK}/pages/${id}?version=${version}`)
})

test('0017/AC-98, AC-99, AC-100: yükleniyor iskeleti ve 500 hatası yeniden deneme düğmesiyle; yeniden deneme sayfayı basar', async ({
  page,
}) => {
  await sifirla(page.request)
  await yazVeYayinla(page.request, ID.about, 'tr', 'Hakkımızda', 'Biz.', 'hakkimizda')

  let ilk = true
  await page.route('**/api/content/pages/hakkimizda*', async (route) => {
    if (ilk) {
      ilk = false
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: '{"errorCode":"api.unavailable"}',
      })
      return
    }

    await route.continue()
  })

  await page.goto('/tr/hakkimizda')
  await expect(page.getByTestId('content-error')).toBeVisible()
  await expect(page.getByTestId('content-not-found')).toHaveCount(0)
  await kanit(page, '[data-testid="content-error"]', '0017-ac99.png')

  await page.getByRole('button', { name: 'Tekrar dene' }).click()
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Hakkımızda')
})

test('0017/AC-26, AC-27: panelde silme onay penceresi; vazgeçince sayfa kalır, onaylayınca adresi bulunamaz', async ({
  page,
}) => {
  const slug = benzersiz('silinecek')
  const olustur = await page.request.post(`${KOK}/pages`, {
    data: { locales: [{ locale: 'tr', title: 'Silinecek Sayfa', body: 'Geçici.', slug }] },
  })
  const { id } = (await olustur.json()) as { id: string }
  await yayin(page.request, id, 'tr', true)

  await page.goto(`/admin/content/${id}`)
  await page.getByTestId('content-delete').click()
  await expect(page.getByTestId('content-delete-dialog')).toBeVisible()
  await kanit(page, '[data-testid="content-delete-dialog"]', '0017-ac26.png')
  await page.getByTestId('content-delete-cancel').click()
  await expect(page.getByTestId('content-delete-dialog')).not.toBeVisible()
  expect((await page.request.get(`/api/content/pages/${slug}?locale=tr`)).status()).toBe(200)

  await page.getByTestId('content-delete').click()
  await page.getByTestId('content-delete-confirm').click()
  await expect(page).toHaveURL(/\/admin\/content$/)

  await page.goto(`/tr/${slug}`)
  await expect(page.getByTestId('content-not-found')).toBeVisible()
})

test('0017/AC-31: iki sekmede açılan sayfada ikincinin kaydı uyarı verir ve form güncel başlığı yükler', async ({
  page,
  context,
}) => {
  const slug = benzersiz('cakisma')
  const olustur = await page.request.post(`${KOK}/pages`, {
    data: { locales: [{ locale: 'tr', title: 'İlk', body: 'x', slug }] },
  })
  const { id } = (await olustur.json()) as { id: string }

  const ikinci = await context.newPage()
  await page.goto(`/admin/content/${id}`)
  await ikinci.goto(`/admin/content/${id}`)

  await page.locator('#content-tr-title').fill('Garanti')
  await page.getByTestId('content-save').click()
  await expect(page.getByTestId('content-saved')).toBeVisible()

  await ikinci.locator('#content-tr-title').fill('Garanti Şartları')
  await ikinci.getByTestId('content-save').click()
  await expect(ikinci.getByTestId('content-conflict')).toContainText(
    'başkası tarafından değiştirildi',
  )
  await expect(ikinci.locator('#content-tr-title')).toHaveValue('Garanti')
  await kanit(ikinci, '[data-testid="content-edit"]', '0017-ac31.png')
  await ikinci.close()
})

test('0017/AC-9, AC-106: Yönetici gezinmesinde İçerik var; yayına alma reddi alanların yanında gösterilir', async ({
  page,
}) => {
  const slug = benzersiz('eksik')
  const olustur = await page.request.post(`${KOK}/pages`, {
    data: { locales: [{ locale: 'tr', title: 'Garanti Koşulları', body: '', slug: '' }] },
  })
  const { id } = (await olustur.json()) as { id: string }
  void slug

  await page.goto('/admin/vehicles')
  await expect(page.getByRole('link', { name: 'İçerik' })).toBeVisible()

  await page.goto(`/admin/content/${id}`)
  await page.getByTestId('content-tr-publish').click()
  await expect(page.getByTestId('content-tr-body-error')).toHaveText(
    'Bu alan yayına almak için zorunlu.',
  )
  await expect(page.getByTestId('content-tr-slug-error')).toBeVisible()
  await expect(page.getByTestId('content-tr-title-error')).toHaveCount(0)
  await kanit(page, '[data-testid="content-tr"]', '0017-ac9.png')

  const { version } = await surum(page.request, id)
  await page.request.delete(`${KOK}/pages/${id}?version=${version}`)
})

test('0017/AC-76, AC-78, AC-80: SSS sırası yalnız klavyeyle değişir; uçtaki düğme devre dışı', async ({
  page,
}) => {
  await sifirla(page.request)
  await sss(page.request, { tr: ['A sorusu', 'a'] }, ['tr'])
  await sss(page.request, { tr: ['B sorusu', 'b'] }, ['tr'])
  await sss(page.request, { tr: ['C sorusu', 'c'] }, ['tr'])

  await page.goto('/admin/content')
  const girdiler = page.getByTestId('content-faq-entry')
  await expect(girdiler).toHaveCount(3)
  await expect(girdiler.nth(0).getByTestId('content-faq-up')).toBeDisabled()
  await expect(girdiler.nth(2).getByTestId('content-faq-down')).toBeDisabled()

  // AC-80 — C'nin "yukarı" düğmesine Tab ile ulaşılır ve Enter ile etkinleştirilir.
  // Son girdinin "aşağı" düğmesi devre dışı ve odak sırasında yok: soru alanından geri Tab.
  const hedef = girdiler.nth(2).getByTestId('content-faq-up')
  await girdiler.nth(2).locator('input[id$="-tr-q"]').focus()
  await page.keyboard.press('Shift+Tab')
  await expect(hedef).toBeFocused()
  await page.keyboard.press('Enter')

  await expect
    .poll(() =>
      page
        .getByTestId('content-faq-entry')
        .locator('input[id$="-tr-q"]')
        .evaluateAll((ogeler) => ogeler.map((oge) => (oge as HTMLInputElement).value)),
    )
    .toEqual(['A sorusu', 'C sorusu', 'B sorusu'])
  await kanit(page, '[data-testid="content-faq-editor"]', '0017-ac76.png')
})

test('0017/AC-123: Arapça arayüzlü personel için İçerik bölümü RTL akar ve arayüz metinleri Arapça', async ({
  page,
}) => {
  const kimlik = `icerik-arapca-${Date.now()}-${Math.random().toString(36).slice(2, 6)}@ornek.com`

  await page.goto('/admin')
  await page.getByLabel('Ad soyad').fill('محرر المحتوى')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill('ilk-parola-1234')
  await page.getByLabel('Rol', { exact: true }).selectOption('content_editor')
  await page.getByLabel('Panel dili').selectOption('ar')
  await page.getByRole('button', { name: 'Personeli ekle' }).press('Enter')
  await expect(page.getByText(kimlik)).toBeVisible()
  await oturumuBirakAsync(page)

  await girisYap(page, kimlik, 'ilk-parola-1234')
  await page.getByLabel('كلمة مرورك الحالية').fill('ilk-parola-1234')
  await page.getByLabel('كلمة مرورك الجديدة').fill('arapca-parola-12')
  await page.getByRole('button', { name: 'غيّر كلمة مروري' }).press('Enter')
  await expect(page.getByTestId('password-changed')).toBeVisible()
  await girisYap(page, kimlik, 'arapca-parola-12')

  await page.goto('/admin/content')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('المحتوى')
  await expect(page.getByRole('link', { name: 'المحتوى' })).toBeVisible()
  await kanit(page, '[data-testid="content-admin"]', '0017-ac123-ar.png')
})
