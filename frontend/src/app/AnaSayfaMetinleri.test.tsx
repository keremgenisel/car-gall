import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ShowcaseListPage } from '../features/catalog/ShowcaseListPage'
import { EmptyByFilter } from '../features/catalog/states/EmptyByFilter'
import type { PagedResult, ShowcaseListing } from '../shared/api/types'
import { createTranslator } from '../shared/i18n/i18n'
import { App } from './App'
import { LocaleProvider } from './LocaleProvider'

/**
 * `0022` — vitrinin ana sayfa metinleri: sunucunun bastığı `<meta name="home-text-…">`
 * etiketleri ilk render'da okunur, etiket yoksa kataloğun varsayılanı (R2, D6). Etiketin
 * sunucu tarafı (kodlama, önbellek, hangi belgeye basıldığı) `Host.Tests`'te; tarayıcıdaki
 * ilk boyama ve düzen kayması e2e'de (`evidence-0022.spec.ts`).
 */
vi.mock('../shared/api/catalog', () => ({
  fetchFeaturedListings: vi.fn(),
  fetchShowcaseTotal: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
  fetchShowcaseListings: vi.fn(),
  fetchListingDetail: vi.fn(),
  fetchReferenceTerms: vi.fn(),
}))
vi.mock('../shared/api/dealership', () => ({ fetchGallery: vi.fn() }))

const catalog = await import('../shared/api/catalog')
const { fetchGallery } = await import('../shared/api/dealership')
const tr = createTranslator('tr')

const ILAN: ShowcaseListing = {
  listingId: '0192c000-0000-7000-8000-000000000001',
  slug: 'toyota-corolla-2019-0192c0',
  isReserved: false,
  coverImageUrl: '/seed-media/cover-0.svg',
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Merkez Şube',
}

/** `<` kaçışlı: sert kodlanmış metin kapısı onu JSX metni sanmasın. */
const ISARETLEME = '\u003cb>Kalın\u003c/b> & "tırnak"'

function metin(alan: string, deger: string) {
  const meta = document.createElement('meta')
  meta.name = `home-text-${alan}`
  meta.content = deger
  meta.dataset['test'] = '0022'
  document.head.append(meta)
}

function anaSayfa() {
  window.history.replaceState(null, '', '/tr/')
  return render(<App locale="tr" />)
}

function liste() {
  window.history.replaceState(null, '', '/tr/cars')
  return render(
    <LocaleProvider locale="tr">
      <ShowcaseListPage />
    </LocaleProvider>,
  )
}

function sayfa(items: ShowcaseListing[], totalCount: number): PagedResult<ShowcaseListing> {
  return { items, page: 1, pageSize: 24, totalCount, totalPages: Math.ceil(totalCount / 24) }
}

beforeEach(() => {
  vi.mocked(catalog.fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: true }],
  })
  vi.mocked(catalog.fetchBodyTypes).mockResolvedValue([{ slug: 'suv', label: 'SUV' }])
  vi.mocked(catalog.fetchReferenceTerms).mockResolvedValue([])
  vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(12)
  vi.mocked(catalog.fetchFeaturedListings).mockResolvedValue([ILAN])
  vi.mocked(fetchGallery).mockResolvedValue({ displayName: 'Oto Galeri', branches: [] })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  document.head.querySelectorAll('meta[data-test="0022"]').forEach((meta) => meta.remove())
})

describe('0022 — kaydedilen metin ana sayfada (R2, R5)', () => {
  it('0022/AC-3: Türkçe giriş başlığı "Kadıköy\'ün güvenilir galerisi" olarak kaydedildikten **hemen sonraki ilk** Türkçe ana sayfa isteği bu başlığı gösterir (D20).', async () => {
    metin('intro-title', "Kadıköy'ün güvenilir galerisi")
    anaSayfa()

    expect((await screen.findByRole('heading', { level: 1 })).textContent).toBe(
      "Kadıköy'ün güvenilir galerisi",
    )
  })

  it('0022/AC-5: Kaydedilmiş bir alan boşaltılıp kaydedildiğinde ana sayfa o dilde yeniden varsayılan metni gösterir.', async () => {
    // Boşaltılan alan için sunucu etiket basmaz (Host.Tests); boşluktan ibaret bir etiket de
    // varsayılana düşer.
    metin('intro-body', '   ')
    anaSayfa()

    expect((await screen.findByRole('heading', { level: 1 })).textContent).toBe(
      tr('home.intro.title'),
    )
    expect(screen.getByTestId('home-intro').querySelector('p.home-hero__body')!.textContent).toBe(
      tr('home.intro.body'),
    )
  })

  it('0022/AC-7: Hiç kayıt yapılmamış bir kurulumda ana sayfanın altı metni bugünkü varsayılan metinlerle aynıdır.', async () => {
    anaSayfa()

    expect((await screen.findByRole('heading', { level: 1 })).textContent).toBe(
      tr('home.intro.title'),
    )
    expect(screen.getByText(tr('home.intro.body'))).toBeTruthy()
    expect((await screen.findByTestId('narrowing-form')).querySelector('h2')!.textContent).toBe(
      tr('home.form.title'),
    )
    expect((await screen.findByTestId('featured-section')).querySelector('h2')!.textContent).toBe(
      tr('home.featured.title'),
    )
    cleanup()

    vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(0)
    anaSayfa()
    const bos = await screen.findByTestId('empty-showcase')
    expect(within(bos).getByRole('heading').textContent).toBe(tr('showcase.empty.showcase.title'))
    expect(within(bos).getByText(tr('showcase.empty.showcase.body'))).toBeTruthy()
  })

  it('0022/AC-8: Yayında ilan ve öne çıkan ilan varken, "Aracınızı bulun" ve "Öne çıkan araçlar" başlıkları kaydedildikten sonra ana sayfada kaydedilen metinle görünür (D18).', async () => {
    metin('form-title', 'Size uygun aracı seçin')
    metin('featured-title', 'Haftanın seçkisi')
    anaSayfa()

    expect((await screen.findByTestId('narrowing-form')).querySelector('h2')!.textContent).toBe(
      'Size uygun aracı seçin',
    )
    expect((await screen.findByTestId('featured-section')).querySelector('h2')!.textContent).toBe(
      'Haftanın seçkisi',
    )
  })
})

describe('0022 — boş stok mesajı (R6)', () => {
  it('0022/AC-9: Yayında hiç ilan yokken ana sayfa, kaydedilmiş boş stok başlığını ve açıklamasını gösterir.', async () => {
    vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(0)
    metin('empty-title', 'Yeni araçlar yolda')
    metin('empty-body', 'Bu hafta yeni araçlar geliyor.')
    anaSayfa()

    const bos = await screen.findByTestId('empty-showcase')
    expect(within(bos).getByRole('heading').textContent).toBe('Yeni araçlar yolda')
    expect(within(bos).getByText('Bu hafta yeni araçlar geliyor.')).toBeTruthy()
  })

  it('0022/AC-10: Yayında hiç ilan yokken vitrin listesi aynı kaydedilmiş başlığı ve açıklamayı gösterir.', async () => {
    vi.mocked(catalog.fetchShowcaseListings).mockResolvedValue(sayfa([], 0))
    metin('empty-title', 'Yeni araçlar yolda')
    metin('empty-body', 'Bu hafta yeni araçlar geliyor.')
    liste()

    const bos = await screen.findByTestId('empty-showcase')
    expect(within(bos).getByRole('heading').textContent).toBe('Yeni araçlar yolda')
    expect(within(bos).getByText('Bu hafta yeni araçlar geliyor.')).toBeTruthy()
  })

  it('0022/AC-11: Boş stok mesajının bağlantı metni değişmez ("Galeriye ulaşın").', async () => {
    vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(0)
    metin('empty-title', 'Yeni araçlar yolda')
    anaSayfa()

    const bos = await screen.findByTestId('empty-showcase')
    expect(within(bos).getByRole('link').textContent).toBe('Galeriye ulaşın')
    expect(tr('showcase.empty.showcase.action')).toBe('Galeriye ulaşın')
  })
})

describe('0022 — düz metin (R3, C5)', () => {
  it('0022/AC-12: `\u003cb>Kalın\u003c/b> & "tırnak"` olarak kaydedilen giriş başlığı ana sayfada bu karakterlerle metin olarak görünür; kalın yazılmaz ve sayfada yeni bir öğe oluşmaz.', async () => {
    metin('intro-title', ISARETLEME)
    anaSayfa()

    const baslik = await screen.findByRole('heading', { level: 1 })
    expect(baslik.textContent).toBe(ISARETLEME)
    expect(baslik.children).toHaveLength(0)
    expect(document.querySelector('b')).toBeNull()
  })

  it('0022/AC-14: Giriş açıklamasına yazılan satır sonu ana sayfada yeni satır olarak görünür.', async () => {
    metin('intro-body', 'Birinci satır\nİkinci satır')
    anaSayfa()

    const govde = (await screen.findByTestId('home-intro')).querySelector('p.home-hero__body')!
    expect(govde.textContent).toBe('Birinci satır\nİkinci satır')
    // Satır sonunu gösteren CSS kuralı (jsdom stil dosyasını yüklemez; görsel ölçüm e2e'de).
    const stiller = readFileSync(join(process.cwd(), 'src/app/styles.css'), 'utf8')
    expect(stiller).toMatch(/\.home-hero__body \{[^}]*white-space: pre-line;/)
  })

  it("0022/AC-15: Boş stok açıklamasına yazılan satır sonu ana sayfada ve listede yeni satır olarak görünür; `EmptyState`'in diğer kullanımlarının görünümü değişmez (D17).", async () => {
    vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(0)
    vi.mocked(catalog.fetchShowcaseListings).mockResolvedValue(sayfa([], 0))
    metin('empty-body', 'Satır bir\nSatır iki')

    anaSayfa()
    const anaSayfadaki = (await screen.findByTestId('empty-showcase')).querySelector('p')!
    expect(anaSayfadaki.textContent).toBe('Satır bir\nSatır iki')
    expect(anaSayfadaki.className).toBe('ui-state__body ui-state__body--lines')
    cleanup()

    liste()
    const listedeki = (await screen.findByTestId('empty-showcase')).querySelector('p')!
    expect(listedeki.className).toBe('ui-state__body ui-state__body--lines')
    cleanup()

    render(
      <LocaleProvider locale="tr">
        <EmptyByFilter axes={[]} onClear={() => {}} />
      </LocaleProvider>,
    )
    expect(screen.getByTestId('empty-by-filter').querySelector('p')!.className).toBe(
      'ui-state__body',
    )
    const ui = readFileSync(join(process.cwd(), 'src/shared/ui/ui.css'), 'utf8')
    expect(ui).toMatch(/\.ui-state__body--lines \{[^}]*white-space: pre-line;/)
    expect(ui).not.toMatch(/\.ui-state__body \{[^}]*white-space/)
  })
})

describe('0022 — ilk boyama (R7, D6, D12)', () => {
  it("0022/AC-19: Kaydedilmiş bir giriş başlığı olan ana sayfada, gezinme başından itibaren `h1`'in aldığı her metin değeri kaydedilmiş başlığa eşittir; varsayılan başlık hiçbir an görünmez (D13).", () => {
    metin('intro-title', 'Kaydedilmiş başlık')
    anaSayfa()

    // İlk render senkron: hiçbir istek dönmeden h1 kaydedilmiş başlığı taşır.
    expect(screen.getByRole('heading', { level: 1 }).textContent).toBe('Kaydedilmiş başlık')
    expect(screen.queryByText(tr('home.intro.title'))).toBeNull()
  })

  it('0022/AC-22: En uzun form başlığıyla mobil görünümde form iskeletinden gerçek forma geçiş sayfayı kaydırmaz; CLS < 0,1 (D12).', async () => {
    const enUzun = 'Ş'.repeat(120)
    metin('form-title', enUzun)
    metin('featured-title', 'Haftanın seçkisi')
    let coz: (n: number) => void = () => {}
    vi.mocked(catalog.fetchShowcaseTotal).mockReturnValue(new Promise((r) => (coz = r)))
    anaSayfa()

    // D12 — iskelet GERÇEK başlığı basar; formla aynı öğe ve sınıf, yani aynı kutu.
    const iskelet = screen.getByTestId('narrowing-form-skeleton')
    const iskeletBasligi = iskelet.querySelector('h2.narrowing-form__title')!
    expect(iskeletBasligi.textContent).toBe(enUzun)
    expect(
      screen.getByTestId('featured-skeleton').querySelector('h2.home-section__title')!.textContent,
    ).toBe('Haftanın seçkisi')

    coz(12)
    const form = await screen.findByTestId('narrowing-form')
    expect(form.querySelector('h2.narrowing-form__title')!.textContent).toBe(enUzun)
  })
})
