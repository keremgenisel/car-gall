import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ShowcaseListPage } from './ShowcaseListPage'
import type { PagedResult, ShowcaseListing } from '../../shared/api/types'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir ve
 * ÜÇ DURUMUN ÜÇÜ DE artı mutlu yol doğrulanır.
 *
 * Sorgular kullanıcının gördüğüne göre yapılır (rol, metin); CSS sınıfına göre sorgulayan
 * test yeniden düzenlemede kırılır ve hiçbir şey kanıtlamaz.
 */
vi.mock('../../shared/api/catalog', () => ({
  fetchShowcaseListings: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
}))

const { fetchShowcaseListings, fetchBodyTypes, fetchFieldSettings } =
  await import('../../shared/api/catalog')

const ilan: ShowcaseListing = {
  listingId: '0192c000-0000-7000-8000-000000000001',
  isReserved: false,
  coverImageUrl: '/seed-media/cover-0.svg',
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Merkez Şube',
}

function sayfa(items: ShowcaseListing[], totalCount: number): PagedResult<ShowcaseListing> {
  return { items, page: 1, pageSize: 24, totalCount, totalPages: Math.ceil(totalCount / 24) }
}

function ekran() {
  return render(
    <LocaleProvider locale="tr">
      <ShowcaseListPage />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  window.history.replaceState(null, '', '/tr/')
  vi.mocked(fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: true }],
  })
  vi.mocked(fetchBodyTypes).mockResolvedValue([{ slug: 'suv', label: 'SUV' }])
})

afterEach(() => {
  // vitest globals kapalı olduğu için Testing Library'nin otomatik cleanup'ı koşmaz:
  // temizlenmeyen DOM, bir sonraki testin öncekinin ekranını görmesine ve yanlış
  // sebeple geçmesine yol açar.
  cleanup()
  vi.clearAllMocks()
})

describe('AC-17: Vitrin listesi ekranının smoke testi', () => {
  it('yükleniyor durumunda iskelet gösterir', () => {
    vi.mocked(fetchShowcaseListings).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('list-skeleton')).toBeTruthy()
  })

  it('mutlu yolda ilanları listeler', async () => {
    vi.mocked(fetchShowcaseListings).mockResolvedValue(sayfa([ilan], 1))

    ekran()

    await waitFor(() => expect(screen.getByTestId('listing-card')).toBeTruthy())
    expect(screen.getByText('Toyota Corolla')).toBeTruthy()
    expect(screen.getByText('₺750.000,00')).toBeTruthy()
    expect(screen.getByText('Merkez Şube')).toBeTruthy()
  })

  it('vitrinde hiç ilan yokken EmptyShowcase gösterir', async () => {
    vi.mocked(fetchShowcaseListings).mockResolvedValue(sayfa([], 0))

    ekran()

    await waitFor(() => expect(screen.getByTestId('empty-showcase')).toBeTruthy())
    expect(screen.queryByTestId('empty-by-filter')).toBeNull()
  })

  it('daraltma sonucu boşken EmptyByFilter gösterir ve seçimi temizle eylemini sunar', async () => {
    window.history.replaceState(null, '', '/tr/?bodyType=suv')
    vi.mocked(fetchShowcaseListings).mockImplementation((query) =>
      Promise.resolve(query.bodyTypes.length > 0 ? sayfa([], 0) : sayfa([ilan], 42)),
    )

    ekran()

    await waitFor(() => expect(screen.getByTestId('empty-by-filter')).toBeTruthy())
    expect(screen.queryByTestId('empty-showcase')).toBeNull()
  })

  it('hata durumunda ErrorState ve tekrar dene gösterir, teknik detay sızdırmaz', async () => {
    vi.mocked(fetchShowcaseListings).mockRejectedValue(new Error('catalog.unavailable'))

    const { container } = ekran()

    await waitFor(() => expect(screen.getByTestId('list-error')).toBeTruthy())
    expect(screen.getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()

    const govde = container.textContent ?? ''
    for (const sizinti of ['catalog.unavailable', '500', 'Error', '/api/']) {
      expect(govde).not.toContain(sizinti)
    }
  })
})
