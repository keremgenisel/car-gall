import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { AppLocale } from '../../../shared/lib/locale'
import { BulkActionBar } from './BulkActionBar'

/**
 * `0012c` **R6** · **D54** — eylem sonucunun iki durumu: **başarılı** ve **kısmi başarı**.
 * Listenin yükleniyor/boş/hata durumları `0010b`'de kurulu ve burada yeniden sınanmıyor.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  applyBulkListingAction: vi.fn(),
}))

const { applyBulkListingAction } = await import('../../../shared/api/catalogAdmin')

const BIRINCI = '0192c000-0000-7000-8000-000000000001'
const IKINCI = '0192c000-0000-7000-8000-000000000002'

function ekran(selection: readonly string[], locale: AppLocale = 'tr') {
  const uygulandi = vi.fn()

  render(
    <LocaleProvider locale={locale}>
      <BulkActionBar
        selection={selection}
        labelOf={(vehicleId) =>
          vehicleId === BIRINCI ? 'Toyota Corolla · 2019' : 'Honda Civic · 2021'
        }
        onApplied={uygulandi}
      />
    </LocaleProvider>,
  )

  return uygulandi
}

beforeEach(() => {
  vi.mocked(applyBulkListingAction).mockResolvedValue({ results: [] })
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('toplu eylem çubuğu', () => {
  it('0012c/AC-37: hepsi uygulandığında başarı durumu basılır ve sayı ICU ile söylenir', async () => {
    vi.mocked(applyBulkListingAction).mockResolvedValue({
      results: [
        {
          vehicleId: BIRINCI,
          applied: true,
          errorCode: null,
          parameters: null,
          listingStatus: 'Published',
        },
        {
          vehicleId: IKINCI,
          applied: true,
          errorCode: null,
          parameters: null,
          listingStatus: 'Published',
        },
      ],
    })

    ekran([BIRINCI, IKINCI])

    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    const bildirim = await screen.findByTestId('vehicle-bulk-success')

    expect(bildirim.textContent).toContain('2 ilan yayına alındı')

    // Kısmi başarı kutusu YOK: hepsi uygulandı.
    expect(screen.queryByTestId('vehicle-bulk-partial')).toBeNull()
  })

  it('0012c/AC-38: bir satır uygulanmadığında kısmi başarı basılır ve uygulanamayan tek tek gerekçesiyle listelenir', async () => {
    vi.mocked(applyBulkListingAction).mockResolvedValue({
      results: [
        {
          vehicleId: BIRINCI,
          applied: true,
          errorCode: null,
          parameters: null,
          listingStatus: 'Archived',
        },
        {
          vehicleId: IKINCI,
          applied: false,
          errorCode: 'vehicle.cannot_publish',
          parameters: { reasons: 'vehicle.cannot_publish.no_media' },
          listingStatus: 'Draft',
        },
      ],
    })

    ekran([BIRINCI, IKINCI])

    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    const kutu = await screen.findByTestId('vehicle-bulk-partial')

    // Sonuç "başarılı"ya YUVARLANMIYOR (`docs/frontend.md` § 3).
    expect(screen.queryByTestId('vehicle-bulk-success')).toBeNull()
    expect(kutu.textContent).toContain('1 ilan uygulandı')

    // Uygulanamayan araç ADIYLA ve gerekçesiyle görünüyor; kimlik gösterilmiyor.
    const satirlar = screen.getAllByTestId('vehicle-bulk-failure')

    expect(satirlar).toHaveLength(1)
    expect(satirlar[0]?.textContent).toContain('Honda Civic · 2021')
    expect(satirlar[0]?.textContent).toContain('En az bir fotoğrafın hazır olması gerekiyor')
  })

  it('0012c/AC-39: kısmi başarı hiçbir teknik detay göstermez', async () => {
    vi.mocked(applyBulkListingAction).mockResolvedValue({
      results: [
        {
          vehicleId: BIRINCI,
          applied: false,
          errorCode: 'listing.invalid_status_transition',
          parameters: null,
          listingStatus: 'Sold',
        },
      ],
    })

    ekran([BIRINCI])

    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    const kutu = await screen.findByTestId('vehicle-bulk-partial')

    expect(kutu.textContent).not.toContain('listing.invalid_status_transition')
    expect(kutu.textContent).not.toContain('409')
    expect(kutu.textContent).not.toContain('/api/')
  })

  it('0012c/AC-2: seçim boşken düğme kapalıdır ve istek hiç gönderilmez', () => {
    ekran([])

    const dugme = screen.getByTestId('vehicle-bulk-apply')

    expect(dugme.hasAttribute('disabled')).toBe(true)
    expect(screen.getByTestId('vehicle-bulk-selection').textContent).toContain(
      'Önce listeden araç seçin',
    )

    fireEvent.click(dugme)

    expect(applyBulkListingAction).not.toHaveBeenCalled()
  })

  it('0012c/AC-5: sınır aşımı istek düzeyi hata olarak basılır ve sınır sayısını söyler', async () => {
    vi.mocked(applyBulkListingAction).mockRejectedValue(
      new ApiError('listing.selection_limit_exceeded', null, 422, null, { limit: '100' }),
    )

    ekran([BIRINCI])

    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    const hata = await screen.findByTestId('vehicle-bulk-error')

    expect(hata.textContent).toContain('en çok 100 araç')

    // İstek düzeyi hata, SATIR düzeyindekinden ayrı bir kutuda: ikisini birleştirmek
    // kısmi başarı kararını silerdi.
    expect(screen.queryByTestId('vehicle-bulk-partial')).toBeNull()
  })

  it('0012c/AC-17: altı toplu eylemin hepsi seçilebilir; rezerve seçenekleri yok', () => {
    ekran([BIRINCI])

    const secenekler = [...screen.getByTestId('vehicle-bulk-operation').querySelectorAll('option')]

    expect(secenekler.map((secenek) => secenek.value)).toEqual([
      'publish',
      'unpublish',
      'sold',
      'archive',
      'feature',
      'unfeature',
    ])
  })

  it('0012c/AC-17: seçilen eylem sunucuya gönderilir', async () => {
    ekran([BIRINCI, IKINCI])

    fireEvent.change(screen.getByTestId('vehicle-bulk-operation'), {
      target: { value: 'feature' },
    })
    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    await waitFor(() =>
      expect(applyBulkListingAction).toHaveBeenCalledWith('feature', [BIRINCI, IKINCI], 'tr'),
    )
  })

  it('0012c/AC-40: Arapça personelde başarı cümlesi Arapça kurulur', async () => {
    vi.mocked(applyBulkListingAction).mockResolvedValue({
      results: [
        {
          vehicleId: BIRINCI,
          applied: true,
          errorCode: null,
          parameters: null,
          listingStatus: 'Archived',
        },
      ],
    })

    ekran([BIRINCI], 'ar')

    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    const bildirim = await screen.findByTestId('vehicle-bulk-success')

    expect(bildirim.textContent).toContain('تم نشر')
  })
})
