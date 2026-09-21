import { cleanup, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ListingDetailPage } from './ListingDetailPage'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir ve
 * DÖRT DURUMUN DÖRDÜ de doğrulanır: yükleniyor, bulunamadı, hata, mutlu yol.
 *
 * "Bulunamadı" ile "hata" ayrı ekranlardır (AC-18 / AC-19); ikisini tek durumda
 * toplamak, ziyaretçiye işe yaramayan bir "tekrar dene" sunmak olurdu.
 */
vi.mock('../../shared/api/catalog', () => ({
  fetchListingDetail: vi.fn(),
}))

const { fetchListingDetail } = await import('../../shared/api/catalog')

const SLUG = 'toyota-corolla-2019-0192c0'

const detay: ShowcaseListingDetail = {
  slug: SLUG,
  isReserved: false,
  coverImageUrl: '/seed-media/cover-0.svg',
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Merkez Şube',
  attributes: [{ key: 'fuel_type', label: 'Yakıt', value: 'Dizel' }],
}

function ekran(locale: AppLocale = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <ListingDetailPage slug={SLUG} />
    </LocaleProvider>,
  )
}

afterEach(() => {
  // vitest globals kapalı olduğu için Testing Library'nin otomatik cleanup'ı koşmaz.
  cleanup()
  vi.clearAllMocks()
})

describe('AC-17, AC-18, AC-19: İlan detayı ekranının smoke testi', () => {
  it('AC-17: yükleniyor durumunda iskelet gösterir', () => {
    vi.mocked(fetchListingDetail).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('detail-skeleton')).toBeTruthy()
  })

  it('mutlu yolda ilanın alanlarını gösterir', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue(detay)

    ekran()

    await waitFor(() => expect(screen.getByTestId('listing-detail')).toBeTruthy())
    expect(screen.getByRole('heading', { level: 1, name: 'Toyota Corolla' })).toBeTruthy()
    expect(screen.getByText('₺750.000,00')).toBeTruthy()
    expect(screen.getByText('Merkez Şube')).toBeTruthy()
    expect(screen.getByText('125.000 km')).toBeTruthy()
  })

  it('AC-11: sunucudan gelen opsiyonel alan, çözülmüş etiketiyle görünür', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue(detay)

    ekran()

    await waitFor(() => expect(screen.getByText('Yakıt')).toBeTruthy())
    expect(screen.getByText('Dizel')).toBeTruthy()
  })

  it('AC-12, AC-13: sunucu göndermeyen alan ekranda hiç bulunmaz', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue({ ...detay, attributes: [] })

    ekran()

    await waitFor(() => expect(screen.getByTestId('listing-detail')).toBeTruthy())
    expect(screen.queryByText('Yakıt')).toBeNull()
  })

  it('AC-15: fiyat sorunuz işaretli ilanda tutar yerine karşılığı gösterilir', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue({
      ...detay,
      price: { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null },
    })

    ekran()

    await waitFor(() => expect(screen.getByTestId('detail-price')).toBeTruthy())
    expect(screen.getByTestId('detail-price').textContent).toBe('Fiyat sorunuz')
  })

  it('AC-16: KWD tutarı üç ondalıkla gösterilir', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue({
      ...detay,
      price: { onRequest: false, amount: 1250.5, currencyCode: 'KWD', minorUnitExponent: 3 },
    })

    ekran('en')

    // Üç ondalık: iki ondalıkla gösterilseydi 1.250,50 olur ve 500 fils sessizce kaybolurdu.
    await waitFor(() =>
      expect(screen.getByTestId('detail-price').textContent).toContain('1,250.500'),
    )
  })

  it('AC-9: rezerve ilanın detayında rozet bulunur', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue({ ...detay, isReserved: true })

    ekran()

    await waitFor(() => expect(screen.getByText('Rezerve')).toBeTruthy())
  })

  it('AC-18: ilan bulunamadığında vitrine dönüş öneren ekran gösterilir', async () => {
    vi.mocked(fetchListingDetail).mockResolvedValue(null)

    ekran()

    await waitFor(() => expect(screen.getByTestId('detail-not-found')).toBeTruthy())
    expect(screen.getByRole('link', { name: 'Vitrine dön' }).getAttribute('href')).toBe('/tr/')
    // Bulunamadı bir hata değildir: "tekrar dene" sunulmaz.
    expect(screen.queryByText('Tekrar dene')).toBeNull()
  })

  it('AC-19, AC-20: hata durumunda tekrar dene sunulur ve teknik detay sızmaz', async () => {
    vi.mocked(fetchListingDetail).mockRejectedValue(new Error('catalog.unavailable'))

    ekran()

    await waitFor(() => expect(screen.getByTestId('detail-error')).toBeTruthy())
    expect(screen.getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()

    const metin = screen.getByTestId('detail-error').textContent ?? ''
    for (const teknik of ['catalog.unavailable', '500', '404', 'traceId', 'Error']) {
      expect(metin).not.toContain(teknik)
    }
  })

  it('AC-24: bulunamadı ve hata metinleri üç dilde de yazılıdır', async () => {
    for (const locale of ['tr', 'ar', 'en'] as const) {
      vi.mocked(fetchListingDetail).mockResolvedValue(null)
      ekran(locale)
      await waitFor(() => expect(screen.getByTestId('detail-not-found')).toBeTruthy())
      expect(
        (screen.getByTestId('detail-not-found').textContent ?? '').trim().length,
      ).toBeGreaterThan(0)
      cleanup()

      vi.mocked(fetchListingDetail).mockRejectedValue(new Error('x'))
      ekran(locale)
      await waitFor(() => expect(screen.getByTestId('detail-error')).toBeTruthy())
      expect((screen.getByTestId('detail-error').textContent ?? '').trim().length).toBeGreaterThan(
        0,
      )
      cleanup()
    }
  })
})
