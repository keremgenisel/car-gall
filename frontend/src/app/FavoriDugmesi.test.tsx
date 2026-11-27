import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from './LocaleProvider'
import { TopBar } from './TopBar'
import { ListingCard } from '../features/catalog/ListingCard'
import { ListingDetailPage } from '../features/catalog/ListingDetailPage'
import type { ShowcaseListing, ShowcaseListingDetail } from '../shared/api/types'
import { createTranslator } from '../shared/i18n/i18n'
import { FAVORITES_KEY, resetFavoritesForTests } from '../shared/lib/favorites'
import type { AppLocale } from '../shared/lib/locale'

/**
 * `frontend-003` **D7** · **D8** — favori düğmesi (kart, detay) ve üst çubuktaki sayaç; gerçek depo
 * (jsdom `localStorage`). Öğeler rolleri ve adlarıyla bulunur. Gerçek tarayıcı yarısı (ilk boyama,
 * ağ isteği, odak, depo taklitleri) `e2e/favoriler.spec.ts`'te.
 */
vi.mock('../shared/api/catalog', () => ({ fetchListingDetail: vi.fn() }))

const { fetchListingDetail } = await import('../shared/api/catalog')

const ilan = (slug: string, modelName: string, modelYear: number): ShowcaseListing => ({
  listingId: '0192c000-0000-7000-8000-000000000001',
  slug,
  isReserved: false,
  coverImageUrl: null,
  modelName,
  modelYear,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Merkez Şube',
})

const COROLLA = ilan('toyota-corolla-2019-0192c0', 'Toyota Corolla', 2019)
const CIVIC = ilan('honda-civic-2021-0192c1', 'Honda Civic', 2021)

const detay: ShowcaseListingDetail = {
  slug: COROLLA.slug,
  isReserved: false,
  images: [],
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Merkez Şube',
  attributes: [{ key: 'fuel_type', label: 'Dizel', value: 'Dizel' }],
}

function kartlar(locale: AppLocale = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <TopBar />
      <ul>
        <ListingCard listing={COROLLA} aboveTheFold />
        <ListingCard listing={CIVIC} aboveTheFold={false} />
      </ul>
    </LocaleProvider>,
  )
}

function kayitliSluglar(): string[] {
  const kayit = JSON.parse(localStorage.getItem(FAVORITES_KEY) ?? '{"items":[]}') as {
    items: { slug: string }[]
  }
  return kayit.items.map((item) => item.slug)
}

const sayac = () => screen.getByRole('link', { name: /Favorilerim/ })

beforeEach(() => {
  localStorage.clear()
  resetFavoritesForTests()
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
})

describe('favori düğmesi ve sayaç', () => {
  it('frontend-003/AC-21, AC-31: Kart düğmesi ekler ve çıkarır; sayaç yenilemeden değişir; durum "yenilemeden sonra" korunur', () => {
    kartlar()
    const dugme = screen.getByRole('button', { name: 'Favori: Toyota Corolla 2019' })
    expect(within(sayac()).getByText('0')).toBeTruthy()

    fireEvent.click(dugme)
    expect(kayitliSluglar()).toEqual([COROLLA.slug])
    expect(within(sayac()).getByText('1')).toBeTruthy()

    // "Sayfa yenilenir": bellek sıfırlanır, durum depodan gelir.
    cleanup()
    resetFavoritesForTests()
    kartlar()
    const yeniden = screen.getByRole('button', { name: 'Favori: Toyota Corolla 2019' })
    expect(yeniden.getAttribute('aria-pressed')).toBe('true')

    fireEvent.click(yeniden)
    expect(kayitliSluglar()).toEqual([])
    expect(within(sayac()).getByText('0')).toBeTruthy()
  })

  it('frontend-003/AC-22: Kartta düğme model adı bağlantısının içinde değildir; iç içe etkileşimli öğe yoktur', () => {
    const { container } = kartlar()

    expect(container.querySelectorAll('a button, button a')).toHaveLength(0)
    expect(screen.getAllByRole('button', { name: /^Favori: / })).toHaveLength(2)
  })

  it('frontend-003/AC-23: Kart düğmelerinin adı model adı ve yılını içerir ve birbirinden farklıdır; detay düğmesinin görünür metni duruma göre değişir', async () => {
    kartlar()
    const adlar = screen
      .getAllByRole('button', { name: /^Favori: / })
      .map((dugme) => dugme.getAttribute('aria-label'))
    expect(adlar).toEqual(['Favori: Toyota Corolla 2019', 'Favori: Honda Civic 2021'])
    cleanup()

    vi.mocked(fetchListingDetail).mockResolvedValue(detay)
    render(
      <LocaleProvider locale="tr">
        <ListingDetailPage slug={detay.slug} />
      </LocaleProvider>,
    )
    const dugme = await screen.findByRole('button', { name: 'Favorilere ekle' })
    fireEvent.click(dugme)
    expect(screen.getByRole('button', { name: 'Favorilerde' }).getAttribute('aria-pressed')).toBe(
      'true',
    )
  })

  it('frontend-003/AC-24: Düğme `aria-pressed` ile durumunu bildirir; kibar canlı bölge "eklendi" / "çıkarıldı" der', () => {
    kartlar()
    const dugme = screen.getByRole('button', { name: 'Favori: Honda Civic 2021' })
    const bolge = screen.getByRole('status')
    expect(dugme.getAttribute('aria-pressed')).toBe('false')
    expect(bolge.textContent).toBe('')

    fireEvent.click(dugme)
    expect(dugme.getAttribute('aria-pressed')).toBe('true')
    expect(bolge.textContent).toBe('Favorilere eklendi')

    fireEvent.click(dugme)
    expect(dugme.getAttribute('aria-pressed')).toBe('false')
    expect(bolge.textContent).toBe('Favorilerden çıkarıldı')
  })

  it('frontend-003/AC-18 (canlı bölge): 24 favoriyle 25. ekleme basılı hâle geçmez; bölge sınırı sayıyla söyler', () => {
    localStorage.setItem(
      FAVORITES_KEY,
      JSON.stringify({
        v: 1,
        items: Array.from({ length: 24 }, (_, index) => ({
          slug: `a-2020-${String(index).padStart(6, '0')}`,
          addedAt: 'x',
          modelName: 'A',
          modelYear: 2020,
        })),
      }),
    )
    kartlar()
    const dugme = screen.getByRole('button', { name: 'Favori: Toyota Corolla 2019' })

    fireEvent.click(dugme)

    expect(dugme.getAttribute('aria-pressed')).toBe('false')
    expect(screen.getByRole('status').textContent).toBe('Favori sınırı: 24')
    expect(within(sayac()).getByText('24')).toBeTruthy()
  })

  it('frontend-003/AC-28 (birim), AC-29: Sayaç sıfırda da görünür, favoriler ekranına götürür ve ilk render depodaki sayıyı gösterir', () => {
    localStorage.setItem(
      FAVORITES_KEY,
      JSON.stringify({
        v: 1,
        items: ['a', 'b', 'c'].map((harf) => ({
          slug: `${harf}-2020-aaaaaa`,
          addedAt: 'x',
          modelName: harf,
          modelYear: 2020,
        })),
      }),
    )
    kartlar('ar')

    const baglanti = screen.getByRole('link', { name: /مفضلتي/ })
    expect(baglanti.getAttribute('href')).toBe('/ar/favorites')
    expect(within(baglanti).getByText('3')).toBeTruthy()
  })

  it('frontend-003/AC-30: Sayacın erişilebilir adı ICU çoğuluyla kurulur; ar için 0, 1, 2, 3, 11 ve 100 doğru biçimdedir', () => {
    const t = createTranslator('ar')
    expect(t('favorites.counter', { count: 0 })).toBe('مفضلتي، 0 سيارة')
    expect(t('favorites.counter', { count: 1 })).toBe('مفضلتي، سيارة واحدة')
    expect(t('favorites.counter', { count: 2 })).toBe('مفضلتي، سيارتان')
    expect(t('favorites.counter', { count: 3 })).toBe('مفضلتي، 3 سيارات')
    expect(t('favorites.counter', { count: 11 })).toBe('مفضلتي، 11 سيارة')
    expect(t('favorites.counter', { count: 100 })).toBe('مفضلتي، 100 سيارة')
    expect(createTranslator('tr')('favorites.counter', { count: 3 })).toBe('Favorilerim, 3 araç')
    expect(createTranslator('en')('favorites.counter', { count: 1 })).toBe('My favourites, 1 car')
  })

  it('frontend-003/AC-15 (birim): Depo kullanılamazken kartta düğme, üst çubukta sayaç ve canlı bölge yoktur', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('dolu', 'QuotaExceededError')
    })
    kartlar()

    expect(screen.queryAllByRole('button')).toHaveLength(0)
    expect(screen.queryByRole('link', { name: /Favorilerim/ })).toBeNull()
    expect(screen.queryByRole('status')).toBeNull()
    expect(screen.getAllByRole('link', { name: /Toyota Corolla|Honda Civic/ })).toHaveLength(2)
  })

  it('frontend-003/AC-17 (birim): Sayfa açıkken yazma kota hatası verirse düğme basılı olmaz ve bölge nedeni söyler', async () => {
    kartlar()
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('dolu', 'QuotaExceededError')
    })
    const dugme = screen.getByRole('button', { name: 'Favori: Toyota Corolla 2019' })

    await act(async () => {
      fireEvent.click(dugme)
    })

    await waitFor(() => expect(screen.getByRole('status').textContent).toBe('Favori kaydedilemedi'))
    expect(dugme.getAttribute('aria-pressed')).toBe('false')
  })

  it('frontend-003/AC-49: İlan detayında "Karşılaştır" düğmesi yoktur', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue(detay)
    render(
      <LocaleProvider locale="tr">
        <ListingDetailPage slug={detay.slug} />
      </LocaleProvider>,
    )

    await screen.findByRole('button', { name: 'Favorilere ekle' })
    expect(screen.queryByRole('button', { name: /Karşılaştır/ })).toBeNull()
    expect(screen.queryByRole('link', { name: /Karşılaştır/ })).toBeNull()
  })
})
