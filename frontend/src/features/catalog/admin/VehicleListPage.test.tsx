import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { VehicleAdminListItem } from '../../../shared/api/types'
import { VehicleListPage } from './VehicleListPage'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir ve
 * ÜÇ DURUMUN ÜÇÜ DE doğrulanır. Sorgular kullanıcının GÖRDÜĞÜNE göre yapılır.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchVehicles: vi.fn(),
}))

const { fetchVehicles } = await import('../../../shared/api/catalogAdmin')

const arac: VehicleAdminListItem = {
  vehicleId: '0192c000-0000-7000-8000-000000000001',
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  listingStatus: 'Draft',
  vin: 'VIN0000000000001',
}

function ekran(locale: 'tr' | 'ar' | 'en' = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <VehicleListPage />
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0010a/AC-26: Kaydedilen araç sade listede araç tanımı, fiyat ve kilometreyle görünür', () => {
  it('araç tanımı, fiyat ve kilometre basılır', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue([arac])

    ekran()

    const satir = await screen.findByTestId('vehicle-row')

    expect(satir.textContent).toContain('Toyota Corolla')
    expect(satir.textContent).toContain('2019')

    // Fiyat ve kilometre personelin locale'inde biçimlenir; ham sayı basılmaz.
    expect(screen.getByRole('table').textContent).toContain('750.000')
    expect(screen.getByRole('table').textContent).toContain('125.000 km')
  })

  it('yıl gruplama ayracı ALMAZ: 2019, "2.019" değil', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue([arac])

    ekran()

    const satir = await screen.findByTestId('vehicle-row')

    expect(satir.textContent).not.toContain('2.019')
  })

  it('"fiyat sorunuz" tutar yerine kendi metnini gösterir (P4)', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue([
      {
        ...arac,
        price: { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null },
      },
    ])

    ekran()

    expect(await screen.findByText('Fiyat sorunuz')).toBeTruthy()
    expect(screen.getByRole('table').textContent).not.toContain('0,00')
  })

  it('durum KODU çeviri kataloğundan basılır; ham kod görünmez (C11, Yasak Listesi 22)', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue([arac])

    ekran()

    expect(await screen.findByText('Taslak')).toBeTruthy()
    expect(screen.getByRole('table').textContent).not.toContain('Draft')
  })
})

describe('Üç durum (docs/frontend.md § 3)', () => {
  it('yükleniyor: iskelet basılır', () => {
    vi.mocked(fetchVehicles).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('vehicle-skeleton')).toBeTruthy()
  })

  it('boş: neden boş ve ne yapılabileceği yazılır', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue([])

    ekran()

    const bos = await screen.findByTestId('vehicle-empty')

    expect(bos.textContent).toContain('Henüz araç kaydı yok')
    expect(bos.textContent).toContain('İlk aracı ekleyerek')
  })

  it('hata: tekrar dene eylemi sunulur ve teknik detay sızmaz (AC-47)', async () => {
    vi.mocked(fetchVehicles).mockRejectedValue(new ApiError('vehicle.exploded', 'trace-123', 500))

    ekran()

    const hata = await screen.findByTestId('vehicle-error')

    expect(hata.textContent).toContain('Tekrar dene')
    expect(hata.textContent).not.toContain('500')
    expect(hata.textContent).not.toContain('trace-123')
    expect(hata.textContent).not.toContain('vehicle.exploded')
  })
})

describe('0010a/AC-50: Form etiketleri ve alan adları TR, AR ve EN de kendi dilinde görünür; hiçbir locale de çeviri anahtarı ham hâliyle görünür', () => {
  it.each([
    ['tr', 'Kilometre'],
    ['en', 'Mileage'],
    ['ar', 'المسافة المقطوعة'],
  ] as const)('%s başlıkları kendi dilinde basılır', async (locale, beklenen) => {
    vi.mocked(fetchVehicles).mockResolvedValue([arac])

    ekran(locale)

    expect(await screen.findByText(beklenen)).toBeTruthy()
    expect(document.body.textContent).not.toContain('vehicle.column')
  })
})
