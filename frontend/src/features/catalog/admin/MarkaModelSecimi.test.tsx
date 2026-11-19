import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import type { VehicleAdminDetail } from '../../../shared/api/types'
import { VehicleEditPage } from './VehicleEditPage'
import { VehicleFormPage } from './VehicleFormPage'

/**
 * `0021` R8, R9 · D22–D24 — araç formunda önce marka, sonra o markanın modeli. Seçenekler
 * oturumsuz referans uçlarından gelir ve yalnız AKTİF kayıtları içerir (sunucu tarafı
 * `VehicleReference.Tests`'te); burada ölçülen formun davranışı.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchVehicle: vi.fn(),
  fetchReferenceOptions: vi.fn(),
  fetchBranches: vi.fn(),
  fetchVehicleFields: vi.fn(),
  createVehicle: vi.fn(),
  updateVehicle: vi.fn(),
}))

const api = await import('../../../shared/api/catalogAdmin')

const TOYOTA = '0192c000-0000-7000-8000-0000000000b8'
const HONDA = '0192c000-0000-7000-8000-0000000000b3'
const FIAT = 'fiat-modelsiz'
const COROLLA = '0192c000-0000-7000-8000-00000000000a'
const YARIS = '0192c000-0000-7000-8000-0000000000a1'
const CIVIC = '0192c000-0000-7000-8000-0000000000a8'

const SECENEKLER = {
  brands: [
    { value: FIAT, label: 'Fiat' },
    { value: HONDA, label: 'Honda' },
    { value: TOYOTA, label: 'Toyota' },
  ],
  models: [
    { value: CIVIC, label: 'Civic', brandId: HONDA },
    { value: COROLLA, label: 'Corolla', brandId: TOYOTA },
    { value: YARIS, label: 'Yaris', brandId: TOYOTA },
  ],
  bodyTypes: [{ value: 'suv', label: 'SUV' }],
  fuelTypes: [{ value: 'petrol', label: 'Benzin' }],
  transmissions: [{ value: 'automatic', label: 'Otomatik' }],
  drivetrains: [{ value: 'fwd', label: 'Önden çekiş' }],
  trimLevels: [{ value: 'base', label: 'Standart' }],
}

function secenekler(select: HTMLElement): string[] {
  return within(select)
    .getAllByRole('option')
    .map((option) => option.textContent ?? '')
}

function yeniForm() {
  render(
    <LocaleProvider locale="tr">
      <VehicleFormPage />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  vi.mocked(api.fetchReferenceOptions).mockResolvedValue(SECENEKLER)
  vi.mocked(api.fetchBranches).mockResolvedValue([
    { branchId: '0192c000-0000-7000-8000-00000000b001', name: 'Merkez Şube', status: 'Active' },
  ])
  vi.mocked(api.fetchVehicleFields).mockResolvedValue([])
  vi.mocked(api.updateVehicle).mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0021/AC-23: Yeni araç formunda model seçicisi bir marka seçilene kadar devre dışıdır.', () => {
  it('model seçicisi devre dışı ve "önce markayı seçin" der', async () => {
    yeniForm()

    const model = await screen.findByLabelText<HTMLSelectElement>('Model')
    expect(model.disabled).toBe(true)
    expect(screen.getByTestId('vehicle-model-hint').textContent).toContain('önce markayı seçin')

    fireEvent.change(screen.getByLabelText('Marka'), { target: { value: TOYOTA } })

    expect(screen.getByLabelText<HTMLSelectElement>('Model').disabled).toBe(false)
  })
})

describe('0021/AC-24: Marka seçildiğinde model seçicisi yalnız o markanın aktif modellerini sunar.', () => {
  it('Toyota seçilince yalnız Toyota modelleri, markasız adlarıyla', async () => {
    yeniForm()

    fireEvent.change(await screen.findByLabelText('Marka'), { target: { value: TOYOTA } })

    expect(secenekler(screen.getByLabelText('Model')).slice(1)).toEqual(['Corolla', 'Yaris'])
  })
})

describe('0021/AC-25: Seçili model varken marka değiştirildiğinde model seçimi temizlenir.', () => {
  it('Corolla seçiliyken Honda seçilince model boşalır', async () => {
    yeniForm()

    fireEvent.change(await screen.findByLabelText('Marka'), { target: { value: TOYOTA } })
    fireEvent.change(screen.getByLabelText('Model'), { target: { value: COROLLA } })
    fireEvent.change(screen.getByLabelText('Marka'), { target: { value: HONDA } })

    expect(screen.getByLabelText<HTMLSelectElement>('Model').value).toBe('')
    expect(secenekler(screen.getByLabelText('Model')).slice(1)).toEqual(['Civic'])
  })
})

describe('0021/AC-28: Araç formunda aktif modeli olmayan bir marka seçildiğinde model seçicisi markada model olmadığını söyler ve kayıt yapılamaz (D22).', () => {
  it('"bu markada model yok" der ve gönderim istek atmaz', async () => {
    yeniForm()

    fireEvent.change(await screen.findByLabelText('Marka'), { target: { value: FIAT } })

    expect(screen.getByTestId('vehicle-model-hint').textContent).toContain(
      'seçilebilecek model yok',
    )
    expect(screen.getByLabelText<HTMLSelectElement>('Model').disabled).toBe(true)

    fireEvent.click(screen.getByRole('button', { name: 'Aracı kaydet' }))

    await waitFor(() => expect(screen.getByRole('alert')).toBeTruthy())
    expect(api.createVehicle).not.toHaveBeenCalled()
  })
})

describe('0021/AC-26: Pasif bir modelle kayıtlı aracın düzenleme formu markayı ve modeli seçili gösterir ve araç model değiştirilmeden kaydedilebilir.', () => {
  it('pasif marka ve model araç verisinden seçili gelir ve kaydetme aynı modeli gönderir', async () => {
    const PASIF_MARKA = 'pasif-marka'
    const PASIF_MODEL = 'pasif-model'
    const kayit = {
      vehicleId: '0192c000-0000-7000-8000-000000000001',
      branchId: '0192c000-0000-7000-8000-00000000b001',
      vehicleModelId: PASIF_MODEL,
      bodyType: 'suv',
      fuelType: 'petrol',
      transmissionType: 'automatic',
      drivetrainType: 'fwd',
      trimLevel: 'base',
      color: { tr: 'Beyaz' },
      modelYear: 2019,
      mileageKilometers: 125000,
      price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
      vin: 'VIN0000000000001',
      listingStatus: 'Draft',
      listingTitle: {},
      listingDescription: {},
      optionalValues: [],
      updatedAt: '2026-11-19T12:30:00Z',
      awaitingBranchAssignment: false,
      model: { brandId: PASIF_MARKA, brandLabel: 'Lancia', modelLabel: 'Delta' },
    } as VehicleAdminDetail
    vi.mocked(api.fetchVehicle).mockResolvedValue(kayit)

    render(
      <LocaleProvider locale="tr">
        <VehicleEditPage vehicleId={kayit.vehicleId} staffRole="sales_advisor" />
      </LocaleProvider>,
    )

    const marka = await screen.findByLabelText<HTMLSelectElement>('Marka')
    expect(marka.value).toBe(PASIF_MARKA)
    expect(marka.selectedOptions[0]?.textContent).toBe('Lancia')
    const model = screen.getByLabelText<HTMLSelectElement>('Model')
    expect(model.value).toBe(PASIF_MODEL)
    expect(model.selectedOptions[0]?.textContent).toBe('Delta')

    fireEvent.click(screen.getByTestId('vehicle-edit-submit'))

    await waitFor(() => expect(api.updateVehicle).toHaveBeenCalled())
    expect(vi.mocked(api.updateVehicle).mock.calls[0]?.[1]).toMatchObject({
      vehicleModelId: PASIF_MODEL,
    })
  })
})
