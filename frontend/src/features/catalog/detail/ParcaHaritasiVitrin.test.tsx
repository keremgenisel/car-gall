/// <reference types="node" />
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import type { ShowcaseListingDetail, ShowcasePaintChange } from '../../../shared/api/types'
import { catalogKeys, createTranslator } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'
import { PAINT_CHANGE_PARTS, PAINT_CHANGE_STATES } from '../../../shared/lib/paintChange'
import { ListingDetailPage } from '../ListingDetailPage'
import { PaintChangeMapSection } from './PaintChangeMapSection'

/** Stil ve token dosyaları kaynaktan okunur: AC-25/28/74 DEĞERLERİ ve yazılı kuralları ölçer. */
const kaynak = (yol: string) => readFileSync(join(process.cwd(), 'src', yol), 'utf8')
const tokens = kaynak('shared/design/tokens.css')
const styles = kaynak('app/styles.css')
const uiStyles = kaynak('shared/ui/ui.css')

/**
 * `0016b` R13–R19, R21 — parça haritasının vitrin bölümü. Bölüm gerçek bileşenle,
 * etkin locale'in kataloğuyla basılır; sayfa düzeyi kriterler (bölümün hiç basılmaması)
 * gerçek detay ekranında, yalnız API istemcisi sahtelenerek ölçülür.
 */
vi.mock('../../../shared/api/catalog', () => ({
  fetchListingDetail: vi.fn(),
}))

const { fetchListingDetail } = await import('../../../shared/api/catalog')

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

/** C10 sırasında tam harita; verilmeyen parçalar `original`. */
function harita(farklar: Readonly<Record<string, string>> = {}): ShowcasePaintChange[] {
  return PAINT_CHANGE_PARTS.map((part) => ({ part, state: farklar[part] ?? 'original' }))
}

const AC34 = {
  right_front_fender: 'painted',
  trunk_lid: 'painted',
  left_rear_door: 'local_painted',
}

function bolum(map: readonly ShowcasePaintChange[], locale: AppLocale = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <PaintChangeMapSection map={map} />
    </LocaleProvider>,
  )
  return screen.getByTestId('paint-map')
}

function parca(part: string): SVGElement {
  return screen.getByTestId('paint-map-schema').querySelector<SVGElement>(`[data-part="${part}"]`)!
}

/** Parçanın dolgusunun başvurduğu desen öğesi. */
function desen(part: string): SVGPatternElement {
  const id = /url\(#(.+)\)/.exec(parca(part).getAttribute('fill') ?? '')![1]!
  return document.getElementById(id) as unknown as SVGPatternElement
}

async function sayfa(paintChangeMap: readonly ShowcasePaintChange[] | undefined) {
  const detay: ShowcaseListingDetail = {
    slug: 'toyota-corolla-2019-abc',
    isReserved: false,
    images: [],
    modelName: 'Toyota Corolla',
    modelYear: 2019,
    mileageKilometers: 125000,
    price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
    branchName: 'Merkez Şube',
    attributes: [],
    ...(paintChangeMap === undefined ? {} : { paintChangeMap }),
  }
  vi.mocked(fetchListingDetail).mockResolvedValue(detay)
  render(
    <LocaleProvider locale="tr">
      <ListingDetailPage slug={detay.slug} />
    </LocaleProvider>,
  )
  await screen.findByTestId('listing-detail')
}

describe('0016b — şema', () => {
  it("0016b/AC-22: Değerli ve public alanla detay sayfası araç şemasını basar; C10'un 13 parçasının her biri ayrı adreslenebilir bir öğedir ve kendi parça kodunu taşır", async () => {
    await sayfa(harita(AC34))
    const kodlar = [...screen.getByTestId('paint-map-schema').querySelectorAll('[data-part]')].map(
      (oge) => oge.getAttribute('data-part'),
    )
    expect([...kodlar].sort()).toEqual([...PAINT_CHANGE_PARTS].sort())
    expect(new Set(kodlar).size).toBe(13)
  })

  it('0016b/AC-23: 13 parçanın her biri dört durumun her birinde (52 birleşim) o durumun deseniyle basılır; şemadaki durum yanıttaki durumla eşleşir', () => {
    let birlesim = 0

    for (const part of PAINT_CHANGE_PARTS) {
      for (const state of PAINT_CHANGE_STATES) {
        bolum(harita({ [part]: state }))
        expect(parca(part).getAttribute('data-state')).toBe(state)
        expect(desen(part).getAttribute('data-state')).toBe(state)
        birlesim += 1
        cleanup()
      }
    }

    expect(birlesim).toBe(52)
  })

  it('0016b/AC-24: Dört durum dört farklı dolgu deseniyle basılır: original boş, local_painted seyrek tarama, painted yoğun tarama, replaced çapraz tarama; iki durum aynı deseni paylaşmaz', () => {
    bolum(harita({ hood: 'local_painted', roof: 'painted', trunk_lid: 'replaced' }))
    const imza = (part: string) => {
      const p = desen(part)
      return {
        cizgi: p.querySelectorAll('line').length,
        aralik: Number(p.getAttribute('data-gap')),
      }
    }

    const original = imza('front_bumper')
    const lokal = imza('hood')
    const boyali = imza('roof')
    const degisen = imza('trunk_lid')

    expect(original.cizgi).toBe(0) // boş
    expect(lokal.cizgi).toBe(1) // tek yön tarama
    expect(boyali.cizgi).toBe(1)
    expect(lokal.aralik).toBeGreaterThan(boyali.aralik) // seyrek > yoğun
    expect(degisen.cizgi).toBe(2) // çapraz
    expect(
      new Set([original, lokal, boyali, degisen].map((i) => `${i.cizgi}/${i.aralik}`)).size,
    ).toBe(4)
  })

  it("0016b/AC-25: Her durumun dolgu renginin şema zeminine karşı kontrast oranı, token katmanından hesaplandığında en az 3:1'dir", () => {
    const deger = (ad: string) => new RegExp(`--${ad}:\\s*(#[0-9a-fA-F]{6});`).exec(tokens)![1]!
    const zemin = deger('color-surface')
    const olcumler = ['original', 'local-painted', 'painted', 'replaced'].map((durum) =>
      kontrast(deger(`color-paint-${durum}`), zemin),
    )

    expect(olcumler).toHaveLength(4)
    for (const oran of olcumler) {
      expect(oran).toBeGreaterThanOrEqual(3)
    }
  })

  it("0016b/AC-27: Lejant dört durumu desen örneği ve etkin locale'deki adıyla gösterir; harita yalnız original ve painted içerse de lejant dördünü gösterir", () => {
    bolum(harita({ hood: 'painted' }))
    const lejant = screen.getByTestId('paint-map-legend')
    const ogeler = within(lejant).getAllByRole('listitem')

    expect(ogeler.map((oge) => oge.textContent)).toEqual([
      'Orijinal',
      'Lokal boyalı',
      'Boyalı',
      'Değişen',
    ])
    for (const [i, state] of PAINT_CHANGE_STATES.entries()) {
      const ornek = ogeler[i]!.querySelector('rect')!
      const id = /url\(#(.+)\)/.exec(ornek.getAttribute('fill')!)![1]!
      expect(document.getElementById(id)?.getAttribute('data-state')).toBe(state)
    }
  })

  it('0016b/AC-28: Şemanın işaretlemesinde ve stillerinde gömülü renk değeri (hex, rgb(), hsl()) yoktur; renkler token katmanından gelir', () => {
    bolum(harita(AC34))
    const isaretleme = screen.getByTestId('paint-map').outerHTML
    expect(isaretleme).not.toMatch(/#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(/)

    const stiller = styles
    const bolumStili = stiller.slice(stiller.indexOf('/* --- Galeri ve büyütme (0016b'))
    expect(bolumStili.length).toBeGreaterThan(0)
    expect(bolumStili).not.toMatch(/#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(/)
    expect(bolumStili).toMatch(/var\(--color-paint-painted\)/)
  })

  it('0016b/AC-29: Şema role="img" taşır ve erişilebilir adı, okunabilir listeye işaret eden, katalogdan gelen bir özettir', () => {
    bolum(harita(AC34))
    const sema = screen.getByRole('img', {
      name: 'Aracın üstten parça şeması; her parçanın durumu yandaki listede yazılı',
    })
    const liste = document.getElementById(sema.getAttribute('aria-describedby')!)!
    expect(within(liste).getByTestId('paint-map-list')).toBeTruthy()
  })

  it('0016b/AC-30 (bileşen yarısı): şemanın içinde tabindex ≥ 0 olan ya da odaklanabilir bir öğe yoktur', () => {
    bolum(harita(AC34))
    const sema = screen.getByTestId('paint-map-schema')
    expect(sema.getAttribute('focusable')).toBe('false')
    expect(sema.querySelectorAll('[tabindex], a, button, [href]')).toHaveLength(0)
  })
})

describe('0016b — tamamı orijinal ve değersiz', () => {
  it('0016b/AC-31: 13 parçası original olan ilanda şema basılır, 13 parçanın hepsi original deseniyle görünür ve "13 parçanın tamamı orijinal" cümlesi bulunur', () => {
    bolum(harita())
    for (const part of PAINT_CHANGE_PARTS) {
      expect(desen(part).getAttribute('data-state')).toBe('original')
    }
    expect(screen.getByTestId('paint-map-all-original').textContent).toBe(
      '13 parçanın tamamı orijinal',
    )
  })

  it('0016b/AC-32: Haritasız ilanın detay sayfasında parça haritası bölümü — şema, lejant, liste, notlar ve başlığı — hiç basılmaz', async () => {
    await sayfa(undefined)
    expect(screen.queryByTestId('paint-map')).toBeNull()
    expect(screen.queryByTestId('paint-map-schema')).toBeNull()
    expect(screen.queryByText('Boya ve değişen parçalar')).toBeNull()
    expect(screen.queryByText('Galeri beyanı')).toBeNull()
    expect(screen.queryByText('13 parçanın tamamı orijinal')).toBeNull()
  })

  it('0016b/AC-33: Alan Kapalı veya Gizli olan ilanın detay sayfasında parça haritası bölümü hiç basılmaz', async () => {
    // Kapalı/Gizli alanda yanıtta ANAHTAR yoktur (xUnit AC-16/AC-17); uç süzme yapmaz.
    await sayfa(undefined)
    expect(document.querySelector('.paint-map')).toBeNull()
  })
})

describe('0016b — okunabilir liste ve notlar', () => {
  it('0016b/AC-34: Liste orijinal olmayanları C10 sırasıyla yazar ("Bagaj kapağı — boyalı", "Sol arka kapı — lokal boyalı", "Sağ ön çamurluk — boyalı") ve ardından "diğer 10 parça orijinal" gelir', () => {
    // Girilen sıra kasten C10'dan farklı.
    const karisik = [...harita(AC34)].reverse()
    bolum(karisik)

    expect(
      within(screen.getByTestId('paint-map-list'))
        .getAllByRole('listitem')
        .map((li) => li.textContent),
    ).toEqual(['Bagaj kapağı — boyalı', 'Sol arka kapı — lokal boyalı', 'Sağ ön çamurluk — boyalı'])
    expect(screen.getByTestId('paint-map-others').textContent).toBe('diğer 10 parça orijinal')
  })

  it('0016b/AC-35: AC-34\'ün listesinde orijinal parçalar tek tek yazılmaz: "Ön tampon" listede yoktur', () => {
    bolum(harita(AC34))
    expect(screen.getByTestId('paint-map-list').textContent).not.toContain('Ön tampon')
  })

  it('0016b/AC-36: Yalnız hood\'un original olduğu haritada cümle "diğer 1 parça orijinal" olur; Arapça\'da sayının Arapça çoğul biçimi kullanılır', () => {
    const yalnizKaput = Object.fromEntries(
      PAINT_CHANGE_PARTS.filter((part) => part !== 'hood').map((part) => [part, 'painted']),
    )
    bolum(harita(yalnizKaput))
    expect(screen.getByTestId('paint-map-others').textContent).toBe('diğer 1 parça orijinal')
    cleanup()

    const ar = createTranslator('ar')
    const bir = ar('detail.paintMap.othersOriginal', { count: 1 })
    const iki = ar('detail.paintMap.othersOriginal', { count: 2 })
    const uc = ar('detail.paintMap.othersOriginal', { count: 3 })
    const on = ar('detail.paintMap.othersOriginal', { count: 11 })

    // ICU'nun Arapça çoğul kategorileri: one / two / few / many — dördü dört ayrı biçim.
    expect(new Set([bir, iki, uc.replace('3', '#'), on.replace('11', '#')]).size).toBe(4)

    bolum(harita(yalnizKaput), 'ar')
    expect(screen.getByTestId('paint-map-others').textContent).toBe(bir)
  })

  it('0016b/AC-37: Hiçbir parça original değilken liste 13 parçayı C10 sırasıyla yazar ve "diğer N parça orijinal" cümlesi basılmaz', () => {
    bolum(harita(Object.fromEntries(PAINT_CHANGE_PARTS.map((part) => [part, 'replaced']))))
    const satirlar = within(screen.getByTestId('paint-map-list')).getAllByRole('listitem')
    expect(satirlar.map((li) => li.getAttribute('data-part'))).toEqual([...PAINT_CHANGE_PARTS])
    expect(screen.queryByTestId('paint-map-others')).toBeNull()
  })

  it('0016b/AC-38: Tamamı orijinal ilanda liste yoktur; yerinde R15\'in cümlesi bulunur ve "diğer 13 parça orijinal" basılmaz', () => {
    bolum(harita())
    expect(screen.queryByTestId('paint-map-list')).toBeNull()
    expect(screen.queryByTestId('paint-map-others')).toBeNull()
    expect(screen.getByTestId('paint-map-all-original')).toBeTruthy()
  })

  it.each([
    ['tr', 'Galeri beyanı', 'Diyagramın üstü aracın ön kısmıdır'],
    ['en', "Dealer's declaration", 'The top of the diagram is the front of the car'],
    ['ar', 'إقرار المعرض', 'أعلى المخطط هو مقدمة السيارة'],
  ] as const)(
    "0016b/AC-39: Şemanın yanında iki not etkin locale'in (%s) kataloğundan gelen metinle görünür",
    (locale, beyan, yon) => {
      bolum(harita(AC34), locale)
      const notlar = within(screen.getByTestId('paint-map-notes')).getAllByRole('listitem')
      expect(notlar.map((li) => li.textContent)).toEqual([beyan, yon])
    },
  )
})

describe('0016b — locale ve RTL', () => {
  it("0016b/AC-73 (bileşen yarısı): 13 parçanın şemadaki koordinatları /tr/ ve /ar/'de aynıdır; left_front_door sol, right_front_door sağ yarıdadır", () => {
    const koordinatlar = (locale: AppLocale) => {
      bolum(harita(AC34), locale)
      const sonuc = Object.fromEntries(
        PAINT_CHANGE_PARTS.map((part) => [
          part,
          ['x', 'y', 'width', 'height'].map((a) => parca(part).getAttribute(a)),
        ]),
      )
      cleanup()
      return sonuc
    }

    const tr = koordinatlar('tr')
    expect(koordinatlar('ar')).toEqual(tr)
    // viewBox genişliği 220: orta 110.
    expect(Number(tr['left_front_door']![0]) + Number(tr['left_front_door']![2]) / 2).toBeLessThan(
      110,
    )
    expect(
      Number(tr['right_front_door']![0]) + Number(tr['right_front_door']![2]) / 2,
    ).toBeGreaterThan(110)
  })

  it("0016b/AC-74: Bu spec'in eklediği stillerde fiziksel yön özelliği yoktur", () => {
    const stiller = styles
    const eklenen = stiller.slice(stiller.indexOf('/* --- Galeri ve büyütme (0016b'))
    const ui = uiStyles
    const dialog = ui.slice(ui.indexOf('`0016b` D28'))

    for (const css of [eklenen, dialog]) {
      expect(css).not.toMatch(
        /(^|[\s;{])(margin|padding|border)-(left|right)\s*:|(^|[\s;{])(left|right)\s*:|text-align:\s*(left|right)|float:\s*(left|right)/m,
      )
    }
  })

  it('0016b/AC-75: Parça adları, durum adları, R15/R17/R18 cümleleri, lejant ve alt şablonu için anahtarlar üç katalogda da doludur', () => {
    const anahtarlar = [
      ...PAINT_CHANGE_PARTS.map((part) => `paintChange.part.${part}`),
      ...PAINT_CHANGE_STATES.map((state) => `paintChange.state.${state}`),
      ...PAINT_CHANGE_STATES.map((state) => `detail.paintMap.state.${state}`),
      'detail.paintMap.allOriginal',
      'detail.paintMap.othersOriginal',
      'detail.paintMap.entry',
      'detail.paintMap.note.declaration',
      'detail.paintMap.note.orientation',
      'detail.paintMap.legend',
      'detail.paintMap.schemaLabel',
      'detail.paintMap.title',
      'detail.gallery.alt',
    ]

    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(catalogKeys(locale)).toEqual(expect.arrayContaining(anahtarlar))
      const t = createTranslator(locale)
      for (const anahtar of anahtarlar) {
        expect(
          t(anahtar, {
            count: 2,
            part: 'p',
            state: 's',
            model: 'm',
            year: 2019,
            index: 1,
            total: 2,
          }).trim(),
        ).not.toBe('')
      }
    }
  })

  it("0016b/AC-76: /en/ detayında parça haritası bölümünün ve galerinin görünen bütün metinleri İngilizce'dir", () => {
    bolum(harita(AC34), 'en')
    const metin = screen.getByTestId('paint-map').textContent ?? ''
    const ad = screen.getByTestId('paint-map-schema').getAttribute('aria-label') ?? ''

    for (const parca of [metin, ad]) {
      expect(parca).not.toMatch(/[çğıöşüÇĞİÖŞÜ]/)
      expect(parca).not.toMatch(/[؀-ۿ]/)
    }
    expect(metin).toContain('Trunk lid — painted')
    expect(metin).toContain('the other 10 parts are original')
  })
})

/** WCAG göreli parlaklık oranı. */
function kontrast(a: string, b: string): number {
  const parlaklik = (hex: string) => {
    const [r, g, bl] = [1, 3, 5].map((i) => parseInt(hex.slice(i, i + 2), 16) / 255)
    const lin = (c: number) => (c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * lin(r!) + 0.7152 * lin(g!) + 0.0722 * lin(bl!)
  }
  const [acik, koyu] = [parlaklik(a), parlaklik(b)].sort((x, y) => y - x)
  return (acik! + 0.05) / (koyu! + 0.05)
}
