import { expect, test, type APIRequestContext, type Page } from '@playwright/test'
import { YONETICI_PAROLA } from './global-setup'
import { yoneticiOlarakGirAsync } from './giris'
import { kanit } from './kanit'

/**
 * `0021` — marka ve model, gerçek Host'a karşı: panelin marka/model ekranları, araç formunda
 * marka → model, vitrinde kart/detay adı, liste marka ekseni ve ana sayfa formunun markaları.
 * Görsel kanıt `npm run evidence` ile yazılır (docs/testing.md § 3); assert'ler her koşuda
 * çalışır.
 *
 * <b>Paylaşılan durum.</b> Marka adları galeri genelinde tekildir ve veritabanı koşular arasında
 * kalabilir; bu dosyanın markaları (Fiat, Alfa Romeo, Rolls-Royce &amp; Co., Lancia, Dacia, Saab)
 * BUL-YA-DA-EKLE ile kurulur ve kurulumun dokuz markasına yalnız okunarak dokunulur — tek
 * istisna Toyota'nın Arapça adı (AC-31), test sonunda geri yazılır. Tohumun "ana sayfada göster"
 * işaretli markaları Honda, Renault ve Toyota'dır.
 *
 * <b>Yayın.</b> Yeni ilanın yayına alınması hazır bir fotoğraf ister; tohum bayrağı açıkken
 * tohumun görsel varlıkları `Media`'da hazır sayılır ve yeni araca bağlanabilir — dosya
 * yüklemeden yayın (`SeedAwareMediaAssetQueries`).
 *
 * <b>Yetkisiz rol, boş, hata, yükleniyor</b> hâlleri gerçek veriyle kurulamadığından yanıt
 * araya girilerek kurulur; <b>Arapça panel</b> panel belgesinin `<html lang dir>`'i araya
 * girilerek basılır (`evidence-0020` deseni). Ekranlar gerçek bileşendir.
 */
test.describe.configure({ mode: 'serial' })

const SEED_KIMLIK = 'yonetici@cargallery.local'
const REF = '/api/admin/reference'

interface Marka {
  readonly id: string
  readonly canonicalName: string
  readonly slug: string
  readonly isActive: boolean
  readonly showOnHome: boolean
  readonly version: number
  readonly displayNames: Record<string, string>
}

interface Model {
  readonly id: string
  readonly canonicalName: string
  readonly isActive: boolean
  readonly version: number
}

async function markalar(api: APIRequestContext): Promise<Marka[]> {
  const yanit = await api.get(`${REF}/brands`)
  expect(yanit.ok(), 'GET brands').toBe(true)
  return (await yanit.json()) as Marka[]
}

async function marka(api: APIRequestContext, ad: string): Promise<Marka> {
  const var_ = (await markalar(api)).find((item) => item.canonicalName === ad)
  if (var_ !== undefined) {
    return var_
  }

  const yanit = await api.post(`${REF}/brands`, { data: { canonicalName: ad } })
  expect(yanit.status(), `POST brand ${ad} → ${await yanit.text()}`).toBe(201)
  return (await markalar(api)).find((item) => item.canonicalName === ad)!
}

async function modeller(api: APIRequestContext, markaId: string): Promise<Model[]> {
  const yanit = await api.get(`${REF}/brands/${markaId}`)
  expect(yanit.ok()).toBe(true)
  return ((await yanit.json()) as { models: Model[] }).models
}

async function model(api: APIRequestContext, markaId: string, ad: string): Promise<Model> {
  const var_ = (await modeller(api, markaId)).find((item) => item.canonicalName === ad)
  if (var_ !== undefined) {
    return var_
  }

  const yanit = await api.post(`${REF}/brands/${markaId}/models`, { data: { canonicalName: ad } })
  expect(yanit.status(), `POST model ${ad} → ${await yanit.text()}`).toBe(201)
  return (await modeller(api, markaId)).find((item) => item.canonicalName === ad)!
}

async function markaDurumu(api: APIRequestContext, ad: string, aktif: boolean, anaSayfa?: boolean) {
  let m = await marka(api, ad)
  if (m.isActive !== aktif) {
    const yanit = await api.post(`${REF}/brands/${m.id}/${aktif ? 'activate' : 'deactivate'}`, {
      data: { version: m.version },
    })
    expect(yanit.ok()).toBe(true)
    m = await marka(api, ad)
  }

  if (anaSayfa !== undefined && m.showOnHome !== anaSayfa) {
    const yanit = await api.put(`${REF}/brands/${m.id}/show-on-home`, {
      data: { version: m.version, showOnHome: anaSayfa },
    })
    expect(yanit.ok()).toBe(true)
  }
}

async function markaAdlari(
  api: APIRequestContext,
  ad: string,
  displayNames: Record<string, string>,
) {
  const m = await marka(api, ad)
  const yanit = await api.put(`${REF}/brands/${m.id}`, {
    data: { version: m.version, canonicalName: m.canonicalName, displayNames },
  })
  expect(yanit.ok(), `PUT brand ${ad} → ${await yanit.text()}`).toBe(true)
}

function benzersizSase(onek: string) {
  return `${onek}${Date.now()}${Math.random().toString(36).slice(2, 6)}`.slice(0, 32)
}

async function aracEkle(api: APIRequestContext, modelId: string, onek: string): Promise<string> {
  const yanit = await api.post('/api/admin/vehicles', {
    data: {
      branchId: '00000000-0000-7000-8000-00000000b001',
      vehicleModelId: modelId,
      bodyType: 'suv',
      fuelType: 'diesel',
      transmissionType: 'automatic',
      drivetrainType: 'front-wheel-drive',
      trimLevel: 'comfort',
      color: { tr: 'Beyaz' },
      modelYear: 2021,
      mileageKilometers: 30_000,
      priceAmount: 900_000,
      priceOnRequest: false,
      vin: benzersizSase(onek),
    },
  })
  expect(yanit.status(), `POST vehicle → ${await yanit.text()}`).toBe(201)
  return ((await yanit.json()) as { vehicleId: string }).vehicleId
}

/** Tohumun hazır bir görselini yeni araca bağlar ve ilanı yayına alır; adresini döner. */
async function yayinla(api: APIRequestContext, aracId: string): Promise<string> {
  const liste = (await (
    await api.get('/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=recently-added')
  ).json()) as { items: { vehicleId: string; vin: string }[] }
  const tohum =
    liste.items.find((item) => item.vin.startsWith('SEED')) ??
    (
      (await (
        await api.get('/api/admin/vehicles?locale=tr&page=1&pageSize=100&sort=last-updated')
      ).json()) as { items: { vehicleId: string; vin: string }[] }
    ).items.find((item) => item.vin.startsWith('SEED'))
  expect(tohum, 'tohum aracı').toBeTruthy()
  const medya = (await (await api.get(`/api/admin/vehicles/${tohum!.vehicleId}/media`)).json()) as {
    items: { mediaAssetId: string; status: string }[]
  }
  const hazir = medya.items.find((item) => item.status === 'ready')
  expect(hazir, 'hazır tohum görseli').toBeTruthy()

  expect(
    (
      await api.put(`/api/admin/vehicles/${aracId}/media`, {
        data: { mediaAssetIds: [hazir!.mediaAssetId] },
      })
    ).status(),
  ).toBe(204)
  expect((await api.post(`/api/admin/vehicles/${aracId}/publish`)).status()).toBe(204)

  const vitrin = (await (
    await api.get('/api/catalog/showcase-listings?brand=fiat&locale=tr&pageSize=24')
  ).json()) as {
    items: { slug: string }[]
  }
  return vitrin.items[0]?.slug ?? ''
}

/** Fiat markası, Egea modeli ve yayında bir Fiat Egea ilanı (yoksa kurar). */
async function fiatEgea(api: APIRequestContext): Promise<string> {
  const fiat = await marka(api, 'Fiat')
  await markaDurumu(api, 'Fiat', true)
  const egea = await model(api, fiat.id, 'Egea')
  const var_ = (await (
    await api.get('/api/catalog/showcase-listings?brand=fiat&locale=tr')
  ).json()) as {
    items: { slug: string }[]
  }
  if (var_.items.length > 0) {
    return var_.items[0]!.slug
  }

  return yayinla(api, await aracEkle(api, egea.id, 'FIATEGEA'))
}

async function arapcaPanel(page: Page) {
  await page.route(/\/admin\//, async (route) => {
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

async function rol(page: Page, role: string) {
  await page.route('**/api/admin/me', async (route) => {
    if (route.request().method() !== 'GET') {
      return route.fallback()
    }

    const yanit = await route.fetch()
    return route.fulfill({ response: yanit, json: { ...(await yanit.json()), role } })
  })
}

async function secenekler(page: Page, etiket: string): Promise<string[]> {
  return page.getByLabel(etiket, { exact: true }).locator('option').allTextContents()
}

test.beforeEach(async ({ page }, testInfo) => {
  test.skip(
    testInfo.project.name !== 'masaustu',
    'Marka adları global; testler tek profilde sırayla koşar.',
  )
  await yoneticiOlarakGirAsync(page, SEED_KIMLIK, YONETICI_PAROLA)
})

test.afterEach(async ({ page }, testInfo) => {
  if (testInfo.project.name === 'masaustu') {
    await page.unrouteAll({ behavior: 'ignoreErrors' })
  }
})

// ----- Kurulum güncellemesi -----

test('0021/AC-5: vitrin tam adı üç dilde gösterir', async ({ page }) => {
  await page.goto('/tr/cars?brand=bmw')
  await expect(page.locator('.listing-card__model').first()).toHaveText('BMW 3 Serisi')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac5-a.png')

  await page.goto('/en/cars?brand=bmw')
  await expect(page.locator('.listing-card__model').first()).toHaveText('BMW 3 Series')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac5-b.png')

  await page.goto('/ar/cars?brand=toyota')
  await expect(
    page.locator('.listing-card__model', { hasText: 'تويوتا كورولا' }).first(),
  ).toBeVisible()
  await kanit(
    page,
    page.getByTestId('listing-card').filter({ hasText: 'تويوتا كورولا' }).first(),
    '0021-ac5-c.png',
  )

  await page.goto('/ar/cars?brand=mercedes-benz')
  await expect(page.locator('.listing-card__model').first()).toHaveText('مرسيدس بنز الفئة C')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac5-d.png')
})

// ----- Marka yönetimi -----

test('0021/AC-6, AC-7, AC-11: marka ekranı, Fiat eklenir; kısaltmalar kuraldan', async ({
  page,
}) => {
  const api = page.request
  const fiatVardi = (await markalar(api)).some((item) => item.canonicalName === 'Fiat')
  await marka(api, 'Alfa Romeo')
  await marka(api, 'Rolls-Royce & Co.')
  await markaDurumu(api, 'Lancia', false)

  await page.goto('/admin/brands')
  await expect(page.getByTestId('brands-table')).toBeVisible()

  if (!fiatVardi) {
    await page.getByTestId('brand-new-form').getByLabel('Kanonik ad (Latin harfleri)').fill('Fiat')
    await page.getByTestId('brand-new-submit').click()
    await expect(page.getByTestId('reference-saved')).toBeVisible()
  }

  const fiat = page
    .getByTestId('brand-row')
    .filter({ has: page.getByRole('link', { name: 'Fiat', exact: true }) })
  await expect(fiat).toContainText('fiat')
  await expect(fiat).toContainText('Aktif')
  await expect(page.getByTestId('brand-row').filter({ hasText: 'Lancia' })).toContainText('Pasif')
  await expect(
    page.getByTestId('brand-row').filter({ hasText: 'Toyota' }).getByTestId('brand-home-toggle'),
  ).toBeChecked()
  await kanit(page, page.getByTestId('brands-table'), '0021-ac6.png')
  await kanit(page, fiat, '0021-ac7.png')

  await expect(page.getByTestId('brand-row').filter({ hasText: 'Alfa Romeo' })).toContainText(
    'alfa-romeo',
  )
  await expect(
    page.getByTestId('brand-row').filter({ hasText: 'Rolls-Royce & Co.' }),
  ).toContainText('rolls-royce-co')
  await kanit(page, page.getByTestId('brands-table'), '0021-ac11.png')
})

test('0021/AC-8, AC-9, AC-10: ad, kısaltma, ASCII ve boş ad reddi', async ({ page }) => {
  await marka(page.request, 'Alfa Romeo')
  await page.goto('/admin/brands')
  const form = page.getByTestId('brand-new-form')
  const ad = form.getByLabel('Kanonik ad (Latin harfleri)')

  await ad.fill('toyota')
  await page.getByTestId('brand-new-submit').click()
  await expect(page.getByTestId('brand-new-name-error')).toHaveText('Bu adla bir marka zaten var.')
  await kanit(page, form, '0021-ac8.png')

  await ad.fill('Alfa-Romeo')
  await page.getByTestId('brand-new-submit').click()
  await expect(page.getByTestId('brand-new-name-error')).toContainText('adres kısaltmasını')
  await kanit(page, form, '0021-ac9.png')

  await ad.fill('Škoda')
  await page.getByTestId('brand-new-submit').click()
  await expect(page.getByTestId('brand-new-name-error')).toContainText('Latin harfleri')
  await kanit(page, form, '0021-ac10-a.png')

  await ad.fill('')
  await page.getByTestId('brand-new-submit').click()
  await expect(page.getByTestId('brand-new-name-error')).toContainText('boş bırakılamaz')
  await kanit(page, form, '0021-ac10-b.png')
})

test('0021/AC-12: kanonik ad düzeltilince kısaltma değişmez', async ({ page }) => {
  const alfa = await marka(page.request, 'Alfa Romeo')
  await page.goto(`/admin/brands/${alfa.id}`)

  const form = page.getByTestId('brand-form')
  await form.getByLabel('Kanonik ad (Latin harfleri)').fill('Alfa Romeo SpA')
  await page.getByTestId('brand-save').click()
  await expect(page.getByTestId('reference-saved')).toBeVisible()
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Alfa Romeo SpA')
  await expect(page.getByTestId('brand-slug')).toContainText('alfa-romeo')
  await kanit(page, page.getByTestId('brand-page'), '0021-ac12.png')

  await form.getByLabel('Kanonik ad (Latin harfleri)').fill('Alfa Romeo')
  await page.getByTestId('brand-save').click()
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Alfa Romeo')
})

test('0021/AC-13, AC-30, AC-31, AC-32: Fiat Egea; Arapça ad vitrinde, Türkçe ve İngilizce kanonik', async ({
  page,
}) => {
  const api = page.request
  const adres = await fiatEgea(api)
  expect(adres).toMatch(/^fiat-egea-2021-/)

  await markaAdlari(api, 'Fiat', {})
  await page.goto(`/tr/cars/${adres}`)
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Fiat Egea')
  await kanit(page, page.getByRole('heading', { level: 1 }), '0021-ac32.png')

  await page.goto('/en/cars?brand=fiat')
  await expect(page.locator('.listing-card__model').first()).toHaveText('Fiat Egea')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac30.png')

  const fiat = await marka(api, 'Fiat')
  await page.goto(`/admin/brands/${fiat.id}`)
  await page.getByTestId('brand-display-names').getByLabel('العربية').fill('فيات')
  await page.getByTestId('brand-save').click()
  await expect(page.getByTestId('reference-saved')).toBeVisible()
  await kanit(page, page.getByTestId('brand-form'), '0021-ac13-a.png')

  await page.goto('/ar/cars?brand=fiat')
  await expect(page.locator('.listing-card__model').first()).toHaveText('فيات Egea')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac13-b.png')
  await page.goto('/tr/cars?brand=fiat')
  await expect(page.locator('.listing-card__model').first()).toHaveText('Fiat Egea')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac13-c.png')

  // AC-31 — görünen ad değişince vitrin yeni adı gösterir.
  await markaAdlari(api, 'Fiat', { ar: 'فيات الإيطالية' })
  await page.goto('/ar/cars?brand=fiat')
  await expect(page.locator('.listing-card__model').first()).toHaveText('فيات الإيطالية Egea')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac31.png')
  await markaAdlari(api, 'Fiat', { ar: 'فيات' })
})

test('0021/AC-14, AC-16, AC-22: pasif marka ve modelleri formda yok; yeniden etkinleşince var', async ({
  page,
}) => {
  const api = page.request
  const saab = await marka(api, 'Saab')
  await model(api, saab.id, '9-5')
  await markaDurumu(api, 'Saab', false)

  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()
  expect(await secenekler(page, 'Marka')).not.toContain('Saab')
  expect(await secenekler(page, 'Marka')).not.toContain('Lancia')
  await kanit(page, page.getByTestId('vehicle-form'), '0021-ac14.png')

  // AC-22 — pasif markanın aktif modeli de yok: model listesi yalnız aktif markalardan.
  const modelYaniti = (await (await api.get('/api/reference/models?locale=tr')).json()) as {
    label: string
  }[]
  expect(modelYaniti.map((item) => item.label)).not.toContain('9-5')

  await markaDurumu(api, 'Saab', true)
  await page.goto(`/admin/brands/${saab.id}`)
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Saab')
  await page.goto('/admin/vehicle-new')
  await expect(page.getByTestId('vehicle-form')).toBeVisible()
  expect(await secenekler(page, 'Marka')).toContain('Saab')
  await page.getByLabel('Marka', { exact: true }).selectOption({ label: 'Saab' })
  expect(await secenekler(page, 'Model')).toContain('9-5')
  await kanit(page, page.getByTestId('vehicle-form'), '0021-ac16.png')
})

test('0021/AC-17, AC-20, AC-21: model eklenir; markası değiştirilemez; pasif model formda yok', async ({
  page,
}) => {
  const api = page.request
  const fiat = await marka(api, 'Fiat')
  await model(api, fiat.id, 'Egea')

  await page.goto(`/admin/brands/${fiat.id}`)
  const satir = page.getByTestId('model-row').filter({ hasText: 'Egea' })
  await expect(satir).toContainText('Aktif')
  await kanit(page, page.getByTestId('models-section'), '0021-ac17.png')

  await satir.getByTestId('model-edit').click()
  const form = page.getByTestId('model-form')
  await expect(form.getByRole('combobox')).toHaveCount(0)
  await expect(form.getByTestId('model-brand-fixed')).toContainText('değiştirilemez')
  await kanit(page, form, '0021-ac20.png')

  const tipo = await model(api, fiat.id, 'Tipo')
  await api
    .post(`${REF}/models/${tipo.id}/deactivate`, { data: { version: tipo.version } })
    .catch(() => undefined)
  await page.goto('/admin/vehicle-new')
  await page.getByLabel('Marka', { exact: true }).selectOption({ label: 'Fiat' })
  expect(await secenekler(page, 'Model')).toContain('Egea')
  expect(await secenekler(page, 'Model')).not.toContain('Tipo')
  await kanit(page, page.getByTestId('vehicle-form'), '0021-ac21.png')
})

// ----- Araç formu -----

test('0021/AC-23, AC-24, AC-25, AC-28: marka → model', async ({ page }) => {
  await markaDurumu(page.request, 'Dacia', true)
  await page.goto('/admin/vehicle-new')
  const form = page.getByTestId('vehicle-form')
  await expect(form).toBeVisible()

  await expect(page.getByLabel('Model', { exact: true })).toBeDisabled()
  await expect(page.getByTestId('vehicle-model-hint')).toContainText('önce markayı seçin')
  await kanit(page, form, '0021-ac23.png')

  await page.getByLabel('Marka', { exact: true }).selectOption({ label: 'Toyota' })
  expect((await secenekler(page, 'Model')).slice(1)).toEqual(['Corolla', 'Yaris'])
  await page.getByLabel('Model', { exact: true }).selectOption({ label: 'Corolla' })
  await kanit(page, form, '0021-ac24.png')

  await page.getByLabel('Marka', { exact: true }).selectOption({ label: 'Honda' })
  await expect(page.getByLabel('Model', { exact: true })).toHaveValue('')
  await kanit(page, form, '0021-ac25.png')

  // Şube seçili: kaydı durduran tek eksik model olsun ve hata onun altında görünsün.
  const sube = await page
    .getByLabel('Şube', { exact: true })
    .locator('option')
    .nth(1)
    .getAttribute('value')
  await page.getByLabel('Şube', { exact: true }).selectOption(sube!)
  await page.getByLabel('Marka', { exact: true }).selectOption({ label: 'Dacia' })
  await expect(page.getByTestId('vehicle-model-hint')).toContainText('seçilebilecek model yok')
  await expect(page.getByLabel('Model', { exact: true })).toBeDisabled()
  await page.getByRole('button', { name: 'Aracı kaydet' }).click()
  await expect(page.getByTestId('vehicle-saved')).toHaveCount(0)
  await expect(page.locator('#vehicle-model-error')).toBeVisible()
  await kanit(page, form, '0021-ac28.png')
})

test('0021/AC-26: pasif modelli aracın düzenleme formu marka ve modeli seçili gösterir ve kaydedilir', async ({
  page,
}) => {
  const api = page.request
  const saab = await marka(api, 'Saab')
  await markaDurumu(api, 'Saab', true)
  const m = await model(api, saab.id, '9-3')
  if (!m.isActive) {
    await api.post(`${REF}/models/${m.id}/activate`, { data: { version: m.version } })
  }
  const arac = await aracEkle(api, m.id, 'SAAB93')
  const guncel = (await modeller(api, saab.id)).find((item) => item.id === m.id)!
  expect(
    (
      await api.post(`${REF}/models/${m.id}/deactivate`, { data: { version: guncel.version } })
    ).ok(),
  ).toBe(true)

  await page.goto(`/admin/vehicles/${arac}`)
  await expect(page.getByLabel('Marka', { exact: true })).toHaveValue(saab.id)
  await expect(page.getByLabel('Model', { exact: true })).toHaveValue(m.id)
  await page.getByLabel('Kilometre', { exact: true }).fill('31000')
  await page.getByTestId('vehicle-edit-submit').click()
  await expect(page.getByTestId('vehicle-edit-saved')).toBeVisible()
  await kanit(page, page.getByTestId('vehicle-edit-form'), '0021-ac26.png')
})

// ----- Vitrin -----

test('0021/AC-29, AC-34: kart, detay, öne çıkanlar ve panel listesi marka + model', async ({
  page,
}) => {
  await page.goto('/tr/cars?brand=toyota')
  const kart = page.getByTestId('listing-card').first()
  await expect(kart.locator('.listing-card__model')).toHaveText(/^Toyota (Corolla|Yaris)$/)
  await kanit(page, kart, '0021-ac29-a.png')

  await kart.locator('a').first().click()
  await expect(page.getByRole('heading', { level: 1 })).toHaveText(/^Toyota (Corolla|Yaris)$/)
  await kanit(page, page.getByRole('heading', { level: 1 }), '0021-ac29-b.png')

  await page.goto('/tr/')
  const oneCikan = page.getByTestId('featured-section')
  await expect(oneCikan.locator('.listing-card__model').first()).toHaveText(/^\S+( \S+)+$/)
  await kanit(page, oneCikan, '0021-ac29-c.png')

  await page.goto('/admin/vehicles')
  await expect(page.getByTestId('vehicle-table')).toContainText(
    /(Toyota|Honda|BMW|Renault|Volkswagen|Ford|Hyundai|Nissan|Mercedes-Benz|Fiat|Saab) \S+/,
  )
  await kanit(page, page.getByTestId('vehicle-table'), '0021-ac34.png')
})

// ----- Liste marka ekseni -----

test('0021/AC-35, AC-36, AC-37, AC-38, AC-39, AC-42: liste marka ekseni', async ({
  page,
  context,
}) => {
  await page.goto('/tr/cars')
  const eksen = page.getByTestId('brands-filter')
  await expect(eksen).toBeVisible()
  await expect(eksen.getByRole('checkbox', { name: 'Lancia' })).toHaveCount(0)
  await expect(eksen.getByRole('checkbox', { name: 'Dacia' })).toHaveCount(0)
  await kanit(page, eksen, '0021-ac35.png')

  const hepsi = await page.getByTestId('result-count').textContent()
  await eksen.getByRole('checkbox', { name: 'Toyota' }).check()
  await expect(page).toHaveURL(/\/tr\/cars\?brand=toyota$/)
  await expect(page.getByTestId('result-count')).not.toHaveText(hepsi ?? '')
  await expect(page.locator('.listing-card__model').first()).toHaveText(/^Toyota /)
  await kanit(page, page.locator('.page'), '0021-ac36.png')
  await kanit(page, page.getByTestId('listing-card').first(), '0021-ac42.png')

  await eksen.getByRole('checkbox', { name: 'Honda' }).check()
  await expect(page).toHaveURL(/brand=toyota&brand=honda$/)
  await kanit(page, eksen, '0021-ac37.png')

  await page.goto('/tr/cars?bodyType=suv&brand=toyota')
  await expect(
    page.getByTestId('body-type-filter').getByRole('checkbox', { name: 'SUV' }),
  ).toBeChecked()
  await expect(
    page.getByTestId('brands-filter').getByRole('checkbox', { name: 'Toyota' }),
  ).toBeChecked()
  await kanit(page, page.locator('.page'), '0021-ac38.png')

  const yeni = await context.newPage()
  await yeni.goto('/tr/cars?brand=toyota')
  await expect(
    yeni.getByTestId('brands-filter').getByRole('checkbox', { name: 'Toyota' }),
  ).toBeChecked()
  await kanit(yeni, yeni.getByTestId('list-filters'), '0021-ac39.png')
  await yeni.close()
})

test('0021/AC-40, AC-41: bilinmeyen ve büyük harfli değer yok sayılır; stoksuz seçili marka eksende', async ({
  page,
}) => {
  await markaDurumu(page.request, 'Lancia', false)
  await page.goto('/tr/cars?brand=yok&brand=Toyota')
  await expect(page.getByTestId('result-count')).toBeVisible()
  await expect(page.getByTestId('listing-card').first()).toBeVisible()
  await kanit(page, page.locator('.page'), '0021-ac40.png')

  await page.goto('/tr/cars?brand=lancia')
  const lancia = page.getByTestId('brands-filter').getByRole('checkbox', { name: 'Lancia' })
  await expect(lancia).toBeChecked()
  await expect(page.getByTestId('empty-by-filter')).toBeVisible()
  await kanit(page, page.locator('.page'), '0021-ac41-a.png')

  await lancia.uncheck()
  await expect(page.getByTestId('listing-card').first()).toBeVisible()
  await kanit(page, page.locator('.page'), '0021-ac41-b.png')
})

test('0021/AC-43: marka ekseninin yükleniyor ve hata hâli diğer eksenlerle aynı', async ({
  page,
}) => {
  await page.route('**/api/catalog/showcase-brands**', () => new Promise(() => {}))
  await page.goto('/tr/cars')
  await expect(page.getByTestId('list-filters')).toHaveAttribute('aria-busy', 'true')
  await expect(page.getByTestId('brands-filter')).toHaveCount(0)
  await expect(page.getByTestId('fuels-filter')).toHaveCount(0)
  await kanit(page, page.getByTestId('list-filters'), '0021-ac43-a.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route('**/api/catalog/showcase-brands**', (route) =>
    route.fulfill({ status: 500, json: { errorCode: 'api.unavailable', traceId: 'trace-0021' } }),
  )
  await page.goto('/tr/cars')
  await expect(page.getByTestId('list-filters')).toHaveAttribute('aria-busy', 'false')
  await expect(page.getByTestId('brands-filter')).toHaveCount(0)
  await expect(page.getByTestId('fuels-filter')).toHaveCount(0)
  await expect(page.locator('body')).not.toContainText('trace-0021')
  await kanit(page, page.getByTestId('list-filters'), '0021-ac43-b.png')
})

test('0021/AC-44: Arapça listede marka ekseni', async ({ page }) => {
  await page.goto('/ar/cars')
  const eksen = page.getByTestId('brands-filter')
  await expect(eksen).toContainText('العلامة التجارية')
  await expect(eksen).toContainText('تويوتا')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await eksen.getByRole('checkbox', { name: 'تويوتا' }).check()
  await expect(page).toHaveURL(/brand=toyota/)
  await kanit(page, eksen, '0021-ac44.png')
})

// ----- Ana sayfa formu -----

test('0021/AC-45, AC-46, AC-47, AC-48: ana sayfa formunun markaları', async ({ page }) => {
  await markaDurumu(page.request, 'Dacia', true, true)
  await page.goto('/tr/')
  const alan = page.getByTestId('narrowing-form-brands')
  await expect(alan).toBeVisible()
  const etiketler = await alan.locator('label').allTextContents()
  expect(etiketler).toEqual(['Honda', 'Renault', 'Toyota'])
  expect(etiketler).not.toContain('Dacia')
  expect(etiketler).not.toContain('BMW')
  await kanit(page, page.getByTestId('narrowing-form'), '0021-ac45.png')
  await kanit(page, page.getByTestId('narrowing-form'), '0021-ac46.png')
  await kanit(page, page.getByTestId('narrowing-form'), '0021-ac47.png')

  const dugme = page.getByTestId('narrowing-form').getByRole('button')
  const once = await dugme.textContent()
  await alan.getByRole('checkbox', { name: 'Toyota' }).check()
  await expect(dugme).toHaveText(once ?? '')
  await kanit(page, page.getByTestId('narrowing-form'), '0021-ac48.png')
  await dugme.click()
  await expect(page).toHaveURL(/\/tr\/cars\?brand=toyota$/)
  await markaDurumu(page.request, 'Dacia', true, false)
})

test('0021/AC-49: marka ve kasa tipiyle liste açılır', async ({ page }) => {
  await page.goto('/tr/')
  await page.getByTestId('narrowing-form-brands').getByRole('checkbox', { name: 'Honda' }).check()
  await page.getByTestId('narrowing-form-body-types').getByRole('checkbox', { name: 'SUV' }).check()
  await page.getByTestId('narrowing-form').getByRole('button').click()
  await expect(page).toHaveURL(/\/tr\/cars\?bodyType=suv&brand=honda$/)
  await expect(
    page.getByTestId('brands-filter').getByRole('checkbox', { name: 'Honda' }),
  ).toBeChecked()
  await kanit(page, page.getByTestId('list-filters'), '0021-ac49.png')
})

test('0021/AC-50, AC-51: marka yokken ve okuma düşünce alan görünmez', async ({ page }) => {
  await page.route('**/api/catalog/showcase-brands**', (route) => route.fulfill({ json: [] }))
  await page.goto('/tr/')
  await expect(page.getByTestId('narrowing-form-body-types')).toBeVisible()
  await expect(page.getByTestId('narrowing-form-brands')).toHaveCount(0)
  await kanit(page, page.getByTestId('narrowing-form'), '0021-ac50.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route('**/api/catalog/showcase-brands**', (route) =>
    route.fulfill({ status: 500, json: { errorCode: 'api.unavailable', traceId: 'trace-0021' } }),
  )
  await page.goto('/tr/')
  await expect(page.getByTestId('narrowing-form-body-types')).toBeVisible()
  await expect(page.getByTestId('narrowing-form-brands')).toHaveCount(0)
  await expect(page.locator('body')).not.toContainText('trace-0021')
  await kanit(page, page.getByTestId('narrowing-form'), '0021-ac51.png')
})

// ----- Erişim ve durumlar -----

test('0021/AC-52: Satış Danışmanı bağlantı görmez ve "erişim yok" görür', async ({ page }) => {
  await rol(page, 'sales_advisor')
  await page.goto('/admin/vehicles')
  await expect(page.getByRole('navigation', { name: 'Panel bölümleri' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Markalar ve modeller' })).toHaveCount(0)
  await kanit(page, page.getByRole('navigation', { name: 'Panel bölümleri' }), '0021-ac52-a.png')

  await page.goto('/admin/brands')
  await expect(page.getByTestId('admin-no-access')).toBeVisible()
  await kanit(page, page.getByTestId('admin-no-access'), '0021-ac52-b.png')
})

test('0021/AC-56: yükleniyor, hata ve boş durum', async ({ page }) => {
  await page.route('**/api/admin/reference/brands', () => new Promise(() => {}))
  await page.goto('/admin/brands')
  await expect(page.getByTestId('brands-loading')).toBeVisible()
  await kanit(page, page.getByTestId('brands-page'), '0021-ac56-a.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route('**/api/admin/reference/brands', (route) =>
    route.fulfill({ status: 500, json: { errorCode: 'api.unavailable', traceId: 'trace-0021' } }),
  )
  await page.goto('/admin/brands')
  await expect(page.getByTestId('brands-error')).toBeVisible()
  await expect(page.locator('body')).not.toContainText('trace-0021')
  await kanit(page, page.getByTestId('brands-page'), '0021-ac56-b.png')

  await page.unrouteAll({ behavior: 'ignoreErrors' })
  await page.route('**/api/admin/reference/brands', (route) => route.fulfill({ json: [] }))
  await page.goto('/admin/brands')
  await expect(page.getByTestId('brands-empty')).toBeVisible()
  await kanit(page, page.getByTestId('brands-page'), '0021-ac56-c.png')
})

test('0021/AC-58: Arapça panelde marka ekranı sağdan sola; kanonik ad soldan sağa', async ({
  page,
}) => {
  const toyota = await marka(page.request, 'Toyota')
  await arapcaPanel(page)
  await page.goto('/admin/brands')
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl')
  await expect(page.getByTestId('brands-page')).toContainText('العلامات التجارية والطرازات')
  await kanit(page, page.getByTestId('brands-page'), '0021-ac58-a.png')

  await page.goto(`/admin/brands/${toyota.id}`)
  const ad = page.getByTestId('brand-form').getByLabel('الاسم الأساسي (بأحرف لاتينية)')
  await expect(ad).toHaveAttribute('dir', 'ltr')
  await kanit(page, page.getByTestId('brand-page'), '0021-ac58-b.png')
})
