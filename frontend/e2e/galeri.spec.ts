import { expect, test, type APIRequestContext, type Page, type TestInfo } from '@playwright/test'
import { kanit } from './kanit'

/**
 * `0016b` — detay galerisi, büyütülmüş görünüm ve parça haritası şeması, gerçek Host'a
 * karşı. jsdom'un taşımadığı davranışlar burada ölçülür: yerel `<dialog>`'un Esc'i, odak
 * döngüsü ve arka planın etkileşimsizliği; gerçek ağ istekleri; gerçek yerleşim ve RTL.
 *
 * İlanlar tohumun `0016b` hâllerinden **detay yanıtına bakılarak** bulunur (görsel sayısı,
 * harita); adresleri her kurulumda farklıdır ve paylaşılan sıraya bağlanılmaz.
 */

interface Detay {
  readonly slug: string
  readonly images: readonly unknown[]
  readonly paintChangeMap?: readonly { part: string; state: string }[]
}

const onbellek = new Map<string, Promise<readonly Detay[]>>()

/** Vitrindeki bütün ilanların detayı; bir kez okunur. */
function detaylar(request: APIRequestContext): Promise<readonly Detay[]> {
  const anahtar = 'tumu'
  if (!onbellek.has(anahtar)) {
    onbellek.set(
      anahtar,
      (async () => {
        const sonuc: Detay[] = []
        for (let sayfa = 1; sayfa <= 12; sayfa += 1) {
          const liste = (await (
            await request.get(`/api/catalog/showcase-listings?locale=tr&page=${sayfa}&pageSize=24`)
          ).json()) as { items: { slug: string }[] }
          for (const item of liste.items) {
            sonuc.push(
              (await (
                await request.get(`/api/catalog/showcase-listings/${item.slug}?locale=tr`)
              ).json()) as Detay,
            )
          }
          if (liste.items.length < 24) {
            break
          }
        }
        return sonuc
      })(),
    )
  }
  return onbellek.get(anahtar)!
}

async function ilan(page: Page, kosul: (detay: Detay) => boolean, ne: string): Promise<string> {
  const bulunan = (await detaylar(page.request)).find(kosul)
  expect(bulunan, `tohumda ${ne} yok`).toBeTruthy()
  return bulunan!.slug
}

const otuzGorselli = (page: Page) => ilan(page, (d) => d.images.length === 30, '30 görselli ilan')
const ucGorselli = (page: Page) => ilan(page, (d) => d.images.length === 3, '3 görselli (404) ilan')
const tekGorselli = (page: Page) =>
  ilan(page, (d) => d.images.length === 1 && d.paintChangeMap === undefined, 'tek görselli ilan')
const gorselsiz = (page: Page) => ilan(page, (d) => d.images.length === 0, 'görselsiz ilan')
const tamamiOrijinal = (page: Page) =>
  ilan(
    page,
    (d) => d.paintChangeMap?.every((item) => item.state === 'original') === true,
    'tamamı orijinal haritalı ilan',
  )
const dortDurumlu = (page: Page) =>
  ilan(
    page,
    (d) => new Set(d.paintChangeMap?.map((item) => item.state)).size === 4,
    'dört durumlu haritalı ilan',
  )

async function ac(page: Page, slug: string, locale = 'tr') {
  await page.goto(`/${locale}/cars/${slug}`)
  await expect(page.getByTestId('listing-detail')).toBeVisible()
}

const masaustu = (testInfo: TestInfo) => testInfo.project.name === 'masaustu'
const sayac = (page: Page) => page.getByTestId('gallery-counter')
const kutu = (page: Page) => page.getByTestId('lightbox')

test.describe('0016b — galeri', () => {
  test('0016b/AC-64, AC-63: 30 hazır görselli ilan açıldığında, etkileşim olmadan, galeri görsellerinden yalnız 1 istek çıkar; ilk görsel eager ve yüksek öncelikli', async ({
    page,
  }) => {
    const slug = await otuzGorselli(page)
    const istekler: string[] = []
    page.on('request', (istek) => {
      if (istek.url().includes('/seed-media/frame-')) {
        istekler.push(istek.url())
      }
    })

    await ac(page, slug)
    await page.waitForLoadState('networkidle')

    expect(istekler).toHaveLength(1)
    const ilk = page.getByTestId('gallery-image')
    await expect(ilk).toHaveAttribute('fetchpriority', 'high')
    expect(await ilk.getAttribute('loading')).toBeNull()
  })

  test('0016b/AC-62: 4:3 yatay, 3:4 dikey ve 16:9 görseller arasında gezinirken galeri kutusunun boyutu değişmez ve altındaki içerik kaymaz', async ({
    page,
  }) => {
    await ac(page, await otuzGorselli(page))
    const olc = async () => ({
      kutu: await page.locator('.gallery__frame').boundingBox(),
      baslik: await page.getByRole('heading', { level: 1 }).boundingBox(),
    })

    const once = await olc()
    for (const beklenen of ['2 / 30', '3 / 30']) {
      await page.getByTestId('gallery-next').first().click()
      await expect(sayac(page)).toHaveText(beklenen)
      await expect(page.getByTestId('gallery-image')).toHaveJSProperty('complete', true)
      expect(await olc()).toEqual(once)
    }

    if (masaustu(test.info())) {
      await kanit(page, '.gallery', '0016b-ac62-dikey-16-9.png')
    }
  })

  test('0016b/AC-42, AC-41: Sonraki ile son görsele gelindiğinde düğme devre dışı kalır ve odak galerinin içinde kalır; ilk görselde önceki devre dışıdır', async ({
    page,
  }) => {
    await ac(page, await ucGorselli(page))
    await expect(page.getByTestId('gallery-previous').first()).toBeDisabled()

    const sonraki = page.getByTestId('gallery-next').first()
    await sonraki.focus()
    await page.keyboard.press('Enter')
    await page.keyboard.press('Enter')

    await expect(sayac(page)).toHaveText('3 / 3')
    await expect(sonraki).toBeDisabled()
    expect(
      await page.evaluate(() =>
        document.querySelector('[data-testid="gallery"]')!.contains(document.activeElement),
      ),
    ).toBe(true)
  })

  test("0016b/AC-43: /tr/'de → sonraki, ← önceki görseldir", async ({ page }) => {
    await ac(page, await otuzGorselli(page))
    await page.getByTestId('gallery-enlarge').focus()
    await page.keyboard.press('ArrowRight')
    await expect(sayac(page)).toHaveText('2 / 30')
    await page.keyboard.press('ArrowLeft')
    await expect(sayac(page)).toHaveText('1 / 30')
  })

  test("0016b/AC-44: /ar/'de ← sonraki, → önceki görseldir", async ({ page }) => {
    await ac(page, await otuzGorselli(page), 'ar')
    await page.getByTestId('gallery-enlarge').focus()
    await page.keyboard.press('ArrowLeft')
    await expect(sayac(page)).toHaveText('2 / 30')
    await page.keyboard.press('ArrowRight')
    await expect(sayac(page)).toHaveText('1 / 30')
  })

  test("0016b/AC-43, AC-44 (dokunmatik): TR'de sağdan sola, AR'da soldan sağa kaydırma sonraki görseldir", async ({
    page,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== 'mobil',
      'Dokunma olayı yalnız dokunmatik profilde üretilir.',
    )

    const kaydir = (bas: number, son: number) =>
      page.evaluate(
        ([x0, x1]) => {
          const hedef = document.querySelector('[data-testid="gallery"]')!
          const dokunus = (x: number) =>
            new Touch({ identifier: 1, target: hedef, clientX: x, clientY: 120 })
          hedef.dispatchEvent(
            new TouchEvent('touchstart', {
              touches: [dokunus(x0)],
              changedTouches: [dokunus(x0)],
              bubbles: true,
            }),
          )
          hedef.dispatchEvent(
            new TouchEvent('touchend', {
              touches: [],
              changedTouches: [dokunus(x1)],
              bubbles: true,
            }),
          )
        },
        [bas, son] as const,
      )

    const slug = await otuzGorselli(page)
    await ac(page, slug, 'tr')
    await kaydir(300, 60)
    await expect(sayac(page)).toHaveText('2 / 30')

    await ac(page, slug, 'ar')
    await kaydir(60, 300)
    await expect(sayac(page)).toHaveText('2 / 30')
  })

  test('0016b/AC-45: Galerinin her etkileşimli öğesine yalnız klavyeyle ulaşılır ve odaktayken görünür bir odak halkası taşır', async ({
    page,
  }, testInfo) => {
    test.skip(testInfo.project.name !== 'masaustu', 'Klavye gezinmesi masaüstü profilinde ölçülür.')

    await ac(page, await otuzGorselli(page))
    const ulasilan = new Set<string>()

    for (let i = 0; i < 40 && ulasilan.size < 3; i += 1) {
      await page.keyboard.press('Tab')
      const bilgi = await page.evaluate(() => {
        const oge = document.activeElement as HTMLElement
        const stil = getComputedStyle(oge)
        return {
          kimlik: oge.dataset['testid'] ?? '',
          halka: stil.outlineStyle !== 'none' && stil.outlineWidth !== '0px',
        }
      })
      if (['gallery-enlarge', 'gallery-previous', 'gallery-next'].includes(bilgi.kimlik)) {
        expect(bilgi.halka, `${bilgi.kimlik} odak halkası`).toBe(true)
        ulasilan.add(bilgi.kimlik)
        if (bilgi.kimlik === 'gallery-next') {
          await kanit(page, '.gallery__frame', '0016b-ac45-odak.png')
        }
      }
    }

    expect([...ulasilan].sort()).toEqual(['gallery-enlarge', 'gallery-next', 'gallery-previous'])
  })

  test('0016b/AC-69: 2. görselin adresi 404 döndüğünde yer tutucu gösterilir (kırık görsel simgesi yok); sayaç "2 / 3" ve sonraki ile 3. görsele geçilir', async ({
    page,
  }) => {
    await ac(page, await ucGorselli(page))
    await page.getByTestId('gallery-next').first().click()

    await expect(page.getByTestId('gallery-placeholder')).toBeVisible()
    await expect(page.locator('.gallery img')).toHaveCount(0)
    await expect(sayac(page)).toHaveText('2 / 3')
    if (masaustu(test.info())) {
      await kanit(page, '.gallery', '0016b-ac69.png')
    }

    await page.getByTestId('gallery-next').first().click()
    await expect(sayac(page)).toHaveText('3 / 3')
    await expect(page.getByTestId('gallery-image')).toBeVisible()
  })

  test('0016b/AC-65, AC-66: Hiç hazır görseli olmayan ilanda görsel alanı, sayaç, düğmeler ve büyütme yoktur; model, fiyat ve nitelikler görünür', async ({
    page,
  }) => {
    await ac(page, await gorselsiz(page))
    await expect(page.getByTestId('gallery')).toHaveCount(0)
    await expect(page.getByTestId('listing-detail').locator('img')).toHaveCount(0)
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible()
    await expect(page.getByTestId('detail-price')).toBeVisible()
    if (masaustu(test.info())) {
      await kanit(page, '[data-testid="listing-detail"]', '0016b-ac65.png')
    }
  })

  test('0016b/AC-67, AC-68: Tek görselde görsel ve büyütme var, sayaç ve düğme yok; büyütülmüş görünümde de yok', async ({
    page,
  }) => {
    await ac(page, await tekGorselli(page))
    await expect(page.getByTestId('gallery-image')).toBeVisible()
    await expect(page.getByTestId('gallery-counter')).toHaveCount(0)
    await expect(page.getByTestId('gallery-next')).toHaveCount(0)
    if (masaustu(test.info())) {
      await kanit(page, '.gallery', '0016b-ac67.png')
    }

    await page.getByTestId('gallery-enlarge').click()
    await expect(page.getByTestId('lightbox-image')).toBeVisible()
    await expect(kutu(page).getByTestId('lightbox-counter')).toHaveCount(0)
    await expect(kutu(page).getByTestId('gallery-next')).toHaveCount(0)
  })
})

test.describe('0016b — büyütülmüş görünüm', () => {
  test('0016b/AC-46, AC-48, AC-52, AC-53: büyütme large türevini gösterir; Esc kapatır, odak açan öğeye döner; adres ve geçmiş değişmez', async ({
    page,
  }) => {
    await ac(page, await otuzGorselli(page))
    const adres = page.url()
    const gecmis = await page.evaluate(() => history.length)

    const buyut = page.getByTestId('gallery-enlarge')
    await buyut.focus()
    await page.keyboard.press('Enter')

    await expect(page.getByTestId('lightbox-image')).toHaveAttribute(
      'src',
      /frame-0-1200x900\.jpg$/,
    )
    if (masaustu(test.info())) {
      await kanit(page, '[data-testid="lightbox"]', '0016b-ac46.png')
    }

    await kutu(page).getByTestId('gallery-next').click()
    await page.keyboard.press('Escape')

    await expect(kutu(page)).not.toBeVisible()
    await expect(buyut).toBeFocused()
    expect(page.url()).toBe(adres)
    expect(await page.evaluate(() => history.length)).toBe(gecmis)
  })

  test('0016b/AC-49, AC-54: kapatma düğmesiyle kapanır; büyütmede 3. görsele gidilip kapatılınca satır içi galeri 3. görseldedir', async ({
    page,
  }) => {
    await ac(page, await otuzGorselli(page))
    await page.getByTestId('gallery-enlarge').click()
    await kutu(page).getByTestId('gallery-next').click()
    await kutu(page).getByTestId('gallery-next').click()
    await expect(kutu(page).getByTestId('lightbox-counter')).toHaveText('3 / 30')

    await page.getByTestId('lightbox-close').click()

    await expect(kutu(page)).not.toBeVisible()
    await expect(sayac(page)).toHaveText('3 / 30')
  })

  test('0016b/AC-47: büyütülmüş görünümde ok tuşları R3 gibi çalışır ve uçta dönmez', async ({
    page,
  }) => {
    await ac(page, await ucGorselli(page))
    await page.getByTestId('gallery-enlarge').click()
    await page.keyboard.press('ArrowLeft')
    await expect(kutu(page).getByTestId('lightbox-counter')).toHaveText('1 / 3')
    await page.keyboard.press('ArrowRight')
    await page.keyboard.press('ArrowRight')
    await page.keyboard.press('ArrowRight')
    await expect(kutu(page).getByTestId('lightbox-counter')).toHaveText('3 / 3')
  })

  test("0016b/AC-50: görünüm açıkken Tab'a art arda 15 kez basıldığında odak her seferinde görünümün içindedir", async ({
    page,
  }, testInfo) => {
    test.skip(testInfo.project.name !== 'masaustu', 'Klavye gezinmesi masaüstü profilinde ölçülür.')

    await ac(page, await otuzGorselli(page))
    await page.getByTestId('gallery-enlarge').click()
    await page.keyboard.press('ArrowRight') // önceki düğmesi de etkin olsun

    for (let i = 0; i < 15; i += 1) {
      await page.keyboard.press('Tab')
      expect(
        await page.evaluate(() =>
          document.querySelector('[data-testid="lightbox"]')!.contains(document.activeElement),
        ),
        `Tab #${i + 1}`,
      ).toBe(true)
    }
  })

  test('0016b/AC-51: görünüm açıkken arka plan etkileşimsizdir: bağlantıya tıklamak sayfayı değiştirmez ve arka plan erişilebilirlik ağacından gizlidir', async ({
    page,
  }, testInfo) => {
    test.skip(
      testInfo.project.name !== 'masaustu',
      'CDP erişilebilirlik ağacı masaüstü Chromium profilinde okunur.',
    )

    await ac(page, await otuzGorselli(page))
    const baglanti = page.locator('header a').first()
    const kutuOnce = await baglanti.boundingBox()
    const adres = page.url()

    await page.getByTestId('gallery-enlarge').click()
    await expect(kutu(page)).toBeVisible()

    await page.mouse.click(kutuOnce!.x + kutuOnce!.width / 2, kutuOnce!.y + kutuOnce!.height / 2)
    expect(page.url()).toBe(adres)

    const cdp = await page.context().newCDPSession(page)
    const { nodes } = (await cdp.send('Accessibility.getFullAXTree')) as {
      nodes: { ignored: boolean; role?: { value: string }; name?: { value: string } }[]
    }
    const baslik = nodes.filter((node) => node.role?.value === 'heading' && !node.ignored)
    // Görünür başlık yalnız görünümün içinden olabilir; detay başlığı (h1) ağaçta yok.
    expect(baslik.map((node) => node.name?.value)).not.toContain('Toyota Corolla')
  })
})

test.describe('0016b — parça haritası', () => {
  test('0016b/AC-22, AC-23, AC-26: dört durumlu ilanın şeması her parçayı durumuyla basar; gri tonlamada desen ayırt ettirir', async ({
    page,
  }) => {
    const slug = await dortDurumlu(page)
    await ac(page, slug)
    const detay = (await detaylar(page.request)).find((d) => d.slug === slug)!

    for (const { part, state } of detay.paintChangeMap!) {
      await expect(
        page.locator(`[data-testid="paint-map-schema"] [data-part="${part}"]`),
      ).toHaveAttribute('data-state', state)
    }

    if (masaustu(test.info())) {
      await kanit(page, '[data-testid="paint-map"]', '0016b-ac23.png')
      await page.addStyleTag({ content: '[data-testid="paint-map"] { filter: grayscale(1); }' })
      await kanit(page, '[data-testid="paint-map"]', '0016b-ac26-gri.png')
    }
  })

  test('0016b/AC-30: Tab şemanın hiçbir parçasına uğramaz', async ({ page }, testInfo) => {
    test.skip(testInfo.project.name !== 'masaustu', 'Klavye gezinmesi masaüstü profilinde ölçülür.')

    await ac(page, await dortDurumlu(page))
    for (let i = 0; i < 40; i += 1) {
      await page.keyboard.press('Tab')
      expect(
        await page.evaluate(() =>
          document
            .querySelector('[data-testid="paint-map-schema"]')!
            .contains(document.activeElement),
        ),
      ).toBe(false)
    }
  })

  test('0016b/AC-31: tamamı orijinal ilanda şema basılır ve "13 parçanın tamamı orijinal" cümlesi görünür', async ({
    page,
  }) => {
    await ac(page, await tamamiOrijinal(page))
    await expect(page.getByTestId('paint-map-schema')).toBeVisible()
    await expect(page.getByTestId('paint-map-all-original')).toHaveText(
      '13 parçanın tamamı orijinal',
    )
    await expect(page.getByTestId('paint-map-list')).toHaveCount(0)
    if (masaustu(test.info())) {
      await kanit(page, '[data-testid="paint-map"]', '0016b-ac31.png')
    }
  })

  test('0016b/AC-32: haritasız ilanda bölüm hiç basılmaz', async ({ page }) => {
    await ac(page, await tekGorselli(page))
    await expect(page.getByTestId('paint-map')).toHaveCount(0)
    await expect(page.getByText('13 parçanın tamamı orijinal')).toHaveCount(0)
  })

  test("0016b/AC-71, AC-72, AC-73: /ar/'de lejant, liste ve notlar sağdan sola akar; galeri denetimleri aynalanır; araç çizimi aynalanmaz", async ({
    page,
  }) => {
    const slug = await dortDurumlu(page)

    const konumlar = async () =>
      page.evaluate(() => {
        const sema = document
          .querySelector('[data-testid="paint-map-schema"]')!
          .getBoundingClientRect()
        const merkez = (part: string) => {
          const kutu = document.querySelector(`[data-part="${part}"]`)!.getBoundingClientRect()
          return (kutu.left + kutu.width / 2 - sema.left) / sema.width
        }
        return {
          sol: merkez('left_front_door'),
          sag: merkez('right_front_door'),
          hepsi: [...document.querySelectorAll('[data-testid="paint-map-schema"] [data-part]')].map(
            (oge) => {
              const kutu = oge.getBoundingClientRect()
              return [
                oge.getAttribute('data-part'),
                Math.round(((kutu.left - sema.left) / sema.width) * 1000),
                Math.round(((kutu.top - sema.top) / sema.height) * 1000),
              ]
            },
          ),
        }
      })

    await ac(page, slug, 'tr')
    const tr = await konumlar()
    const trOnceki = (await page.getByTestId('gallery-previous').first().boundingBox())!
    const trSonraki = (await page.getByTestId('gallery-next').first().boundingBox())!
    expect(trOnceki.x).toBeLessThan(trSonraki.x)

    await ac(page, slug, 'ar')
    const ar = await konumlar()

    // AC-73 — çizim aynalanmaz.
    expect(tr.sol).toBeLessThan(0.5)
    expect(tr.sag).toBeGreaterThan(0.5)
    expect(ar.hepsi).toEqual(tr.hepsi)

    // AC-72 — "önceki" sağ kenarda, "sonraki" sol kenarda.
    const arOnceki = (await page.getByTestId('gallery-previous').first().boundingBox())!
    const arSonraki = (await page.getByTestId('gallery-next').first().boundingBox())!
    expect(arOnceki.x).toBeGreaterThan(arSonraki.x)

    // AC-71 — metin akışı sağdan sola.
    for (const testId of ['paint-map-legend', 'paint-map-list', 'paint-map-notes']) {
      expect(
        await page.getByTestId(testId).evaluate((oge) => getComputedStyle(oge).direction),
      ).toBe('rtl')
    }

    if (masaustu(test.info())) {
      await kanit(page, '[data-testid="paint-map"]', '0016b-ac71-ar.png')
      await kanit(page, '.gallery', '0016b-ac72-ar.png')
    }
  })

  test("0016b/AC-76: /en/ detayında parça haritası bölümünün ve galerinin görünen metinleri İngilizce'dir", async ({
    page,
  }) => {
    await ac(page, await dortDurumlu(page), 'en')
    const metin = [
      await page.getByTestId('paint-map').innerText(),
      await page.getByTestId('gallery-counter').innerText(),
      (await page.getByTestId('gallery-image').getAttribute('alt')) ?? '',
      (await page.getByTestId('gallery-next').first().getAttribute('aria-label')) ?? '',
    ].join('\n')

    expect(metin).not.toMatch(/[çğıöşüÇĞİÖŞÜ]/)
    expect(metin).not.toMatch(/[؀-ۿ]/)
    expect(metin).toContain('Paint and replaced parts')
  })
})
