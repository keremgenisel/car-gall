import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { createTranslator } from '../shared/i18n/i18n'
import { App } from './App'

/**
 * `0018` **D16** · **D36** — bülten bölümünün ana sayfadaki YERİ ve KVKK bağlantısının
 * kaynağı. Bölümün kendi durumları `features/notifications/NewsletterSection.test.tsx`'te.
 */
vi.mock('../shared/api/catalog', () => ({
  fetchFeaturedListings: vi.fn(),
  fetchShowcaseTotal: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
  fetchShowcaseListings: vi.fn(),
  fetchListingDetail: vi.fn(),
  fetchHomeBrands: vi.fn().mockResolvedValue([]),
}))
vi.mock('../shared/api/dealership', () => ({ fetchGallery: vi.fn() }))
vi.mock('../shared/api/content', () => ({
  fetchContentNavigation: vi.fn(),
  fetchContentPage: vi.fn(),
}))

const catalog = await import('../shared/api/catalog')
const { fetchGallery } = await import('../shared/api/dealership')
const { fetchContentNavigation } = await import('../shared/api/content')

const t = createTranslator('tr')

function anaSayfa() {
  window.history.replaceState(null, '', '/tr/')
  return render(<App locale="tr" />)
}

beforeEach(() => {
  vi.mocked(catalog.fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: true }],
  })
  vi.mocked(catalog.fetchBodyTypes).mockResolvedValue([{ slug: 'suv', label: 'SUV' }])
  vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(12)
  vi.mocked(catalog.fetchFeaturedListings).mockResolvedValue([])
  vi.mocked(fetchGallery).mockResolvedValue({
    displayName: 'Örnek Otomotiv',
    branches: [],
    socialLinks: [],
  })
  vi.mocked(fetchContentNavigation).mockResolvedValue([])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

/** Belge sırasında `a`, `b`'den önce mi? */
function once(a: Element, b: Element): boolean {
  return (a.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING) !== 0
}

describe("0018/AC-1: Ana sayfada `HomeCatalog`'un hemen altında ve altlıktan önce bir bülten bölümü vardır: başlık, haftalık yeni araç özeti alınacağını söyleyen açıklama, e-posta alanı, gönder düğmesi ve tek tıkla çıkılabileceğini söyleyen not", () => {
  it('bölüm öne çıkanlardan sonra, altlıktan önce', async () => {
    anaSayfa()

    const bolum = await screen.findByTestId('newsletter-section')
    const katalog = await screen.findByTestId('narrowing-form')
    const altlik = screen.getByTestId('home-footer')

    expect(once(katalog, bolum)).toBe(true)
    expect(once(bolum, altlik)).toBe(true)
    expect(bolum.closest('main')).not.toBeNull()
    expect(bolum.textContent).toContain(t('newsletter.note'))
  })
})

describe('0018/AC-2: Yayında hiç ilan yokken bülten bölümü yine görünür ve boş stok mesajının altında durur', () => {
  it('boş vitrin: boş stok mesajı, altında bülten', async () => {
    vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(0)
    anaSayfa()

    const bos = await screen.findByTestId('empty-showcase')
    const bolum = screen.getByTestId('newsletter-section')

    expect(once(bos, bolum)).toBe(true)
    expect(screen.getByRole('button', { name: t('newsletter.submit') })).toBeTruthy()
  })
})

describe('0018/AC-3: `privacy` o dilde yayındayken nottaki KVKK bağlantısı `/{locale}/{privacy slug}` adresine gider', () => {
  it('bağlantı altlıkla AYNI okumadan gelir (D36)', async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([
      { systemKey: 'about', title: 'Hakkımızda', slug: 'hakkimizda' },
      { systemKey: 'privacy', title: 'KVKK', slug: 'kvkk-aydinlatma' },
    ])
    anaSayfa()

    expect((await screen.findByTestId('newsletter-privacy')).getAttribute('href')).toBe(
      '/tr/kvkk-aydinlatma',
    )
    expect(vi.mocked(fetchContentNavigation)).toHaveBeenCalledTimes(1)
  })
})

describe('0018/AC-4: `privacy` o dilde yayında değilken notta bağlantı yoktur ve form abonelik isteği göndermeye devam eder', () => {
  it('privacy yok → bağlantı yok', async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([
      { systemKey: 'about', title: 'Hakkımızda', slug: 'hakkimizda' },
    ])
    anaSayfa()

    await screen.findByTestId('footer-corporate')

    expect(screen.queryByTestId('newsletter-privacy')).toBeNull()
  })
})
