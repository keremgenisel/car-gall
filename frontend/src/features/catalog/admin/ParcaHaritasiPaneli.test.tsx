import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { VehicleAdminDetail, VehicleAdminField } from '../../../shared/api/types'
import { catalogKeys, createTranslator } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'
import { PAINT_CHANGE_PARTS, PAINT_CHANGE_STATES } from '../../../shared/lib/paintChange'
import { VehicleEditPage } from './VehicleEditPage'

/** `0016a` R12 — panelde parça haritası: 13 seçim kutusu, ret, temizleme. */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchVehicle: vi.fn(),
  fetchReferenceOptions: vi.fn(),
  fetchBranches: vi.fn(),
  fetchVehicleFields: vi.fn(),
  updateVehicle: vi.fn(),
}))

const { fetchVehicle, fetchReferenceOptions, fetchBranches, fetchVehicleFields, updateVehicle } =
  await import('../../../shared/api/catalogAdmin')

const HARITA_ALANI: VehicleAdminField = {
  fieldKey: 'paint_change_map',
  kind: 'PartMap',
  isRequired: false,
}
const TRAMER: VehicleAdminField = {
  fieldKey: 'damage_record_amount',
  kind: 'Amount',
  isRequired: false,
}

const AC5 = PAINT_CHANGE_PARTS.map((part) => ({
  part,
  state:
    part === 'front_bumper'
      ? 'painted'
      : part === 'trunk_lid'
        ? 'replaced'
        : part === 'left_rear_door'
          ? 'local_painted'
          : 'original',
}))

function kayit(partMap: typeof AC5 | null): VehicleAdminDetail {
  return {
    vehicleId: '0192c000-0000-7000-8000-000000000001',
    branchId: '0192c000-0000-7000-8000-00000000b001',
    vehicleModelId: '0192c000-0000-7000-8000-00000000000a',
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
    listingTitle: { tr: 'Temiz Corolla' },
    listingDescription: { tr: 'Bakımları yapıldı' },
    optionalValues:
      partMap === null
        ? []
        : [{ fieldKey: 'paint_change_map', kind: 'PartMap', value: null, values: null, partMap }],
    updatedAt: '2026-11-18T12:30:00Z',
    awaitingBranchAssignment: false,
  }
}

async function ekran(detay: VehicleAdminDetail, locale: AppLocale = 'tr') {
  vi.mocked(fetchVehicle).mockResolvedValue(detay)
  render(
    <LocaleProvider locale={locale}>
      <VehicleEditPage vehicleId={detay.vehicleId} staffRole="sales_advisor" />
    </LocaleProvider>,
  )
  return screen.findByTestId('paint-change-map-input')
}

function kutular(alan: HTMLElement): HTMLSelectElement[] {
  return within(alan).getAllByRole('combobox') as HTMLSelectElement[]
}

function gonderilenHarita(cagri = 0) {
  const istek = vi.mocked(updateVehicle).mock.calls[cagri]?.[1]
  return istek?.optionalValues.find((item) => item.fieldKey === 'paint_change_map')
}

beforeEach(() => {
  vi.mocked(fetchReferenceOptions).mockResolvedValue({
    models: [{ value: '0192c000-0000-7000-8000-00000000000a', label: 'Toyota Corolla' }],
    bodyTypes: [{ value: 'suv', label: 'SUV' }],
    fuelTypes: [{ value: 'petrol', label: 'Benzin' }],
    transmissions: [{ value: 'automatic', label: 'Otomatik' }],
    drivetrains: [{ value: 'fwd', label: 'Önden çekiş' }],
    trimLevels: [{ value: 'base', label: 'Standart' }],
  })
  vi.mocked(fetchBranches).mockResolvedValue([
    { branchId: '0192c000-0000-7000-8000-00000000b001', name: 'Merkez Şube', status: 'Active' },
  ])
  vi.mocked(fetchVehicleFields).mockResolvedValue([TRAMER, HARITA_ALANI])
  vi.mocked(updateVehicle).mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0016a — panelde parça haritası (R12)', () => {
  it("0016a/AC-23: Alan açıkken düzenleme formu 13 seçim kutusu gösterir; kutular C10 sırasındadır ve etiketleri etkin locale'in kataloğundan gelir", async () => {
    const alan = await ekran(kayit(null))

    const etiketler = kutular(alan).map(
      (kutu) => alan.querySelector(`label[for="${kutu.id}"]`)?.textContent,
    )
    expect(etiketler).toHaveLength(13)
    expect(etiketler[0]).toBe('Ön tampon')
    expect(etiketler[12]).toBe('Sağ arka çamurluk')
    expect(kutular(alan).map((kutu) => kutu.dataset['part'])).toEqual([...PAINT_CHANGE_PARTS])
  })

  it('0016a/AC-24: Her seçim kutusu dört durum seçeneğini katalogdan gelen adlarla ve ayrıca boş bir seçeneği sunar', async () => {
    const alan = await ekran(kayit(null))

    for (const kutu of kutular(alan)) {
      expect(Array.from(kutu.options).map((option) => option.value)).toEqual([
        '',
        ...PAINT_CHANGE_STATES,
      ])
      expect(
        Array.from(kutu.options)
          .slice(1)
          .map((option) => option.textContent),
      ).toEqual(['Orijinal', 'Lokal boyalı', 'Boyalı', 'Değişen'])
    }
  })

  it('0016a/AC-25: Haritası olmayan bir aracın formunda 13 kutunun hiçbiri seçili gelmez; "orijinal" önceden seçili değildir', async () => {
    const alan = await ekran(kayit(null))

    expect(kutular(alan).map((kutu) => kutu.value)).toEqual(Array(13).fill(''))
  })

  it('0016a/AC-26: Haritası kayıtlı bir aracın formu 13 kutuyu kayıtlı durumlarla açar', async () => {
    const alan = await ekran(kayit(AC5))

    const deger = (ad: string) => (within(alan).getByLabelText(ad) as HTMLSelectElement).value
    expect(deger('Ön tampon')).toBe('painted')
    expect(deger('Bagaj kapağı')).toBe('replaced')
    expect(deger('Sol arka kapı')).toBe('local_painted')
    expect(kutular(alan).filter((kutu) => kutu.value === 'original')).toHaveLength(10)
  })

  it('0016a/AC-27: Alan Kapalı iken düzenleme formunda parça haritası kutularının hiçbiri bulunmaz', async () => {
    vi.mocked(fetchVehicleFields).mockResolvedValue([TRAMER])
    vi.mocked(fetchVehicle).mockResolvedValue(kayit(null))
    render(
      <LocaleProvider locale="tr">
        <VehicleEditPage vehicleId={kayit(null).vehicleId} staffRole="sales_advisor" />
      </LocaleProvider>,
    )

    await screen.findByTestId('vehicle-edit-form')
    expect(screen.queryByTestId('paint-change-map-input')).toBeNull()
    expect(screen.queryByLabelText('Ön tampon')).toBeNull()
  })

  it('0016a/AC-28: Alan Opsiyonel + Gizli iken düzenleme formu 13 kutuyu gösterir (formun alan listesi public görünürlükten bağımsızdır)', async () => {
    // Yönetim formunun alan listesi görünürlük taşımaz: gizli ama açık bir alan listede
    // aynı şekilde gelir (sunucu yarısı `0016a/AC-19`).
    const alan = await ekran(kayit(null))
    expect(kutular(alan)).toHaveLength(13)
  })

  it('0016a/AC-29: 11 kutu seçilip Tavan ve Sağ arka kapı boş bırakıldığında kayıt reddedilir; form eksikleri adıyla ve C10 sırasıyla söyler ve yalnız bu iki kutu hatalı işaretlenir', async () => {
    vi.mocked(updateVehicle).mockRejectedValueOnce(
      new ApiError('vehicle.paint_change_map_incomplete', null, 422, 'paint_change_map', {
        parts: 'roof,right_rear_door',
      }),
    )
    const alan = await ekran(kayit(null))
    for (const kutu of kutular(alan)) {
      if (kutu.dataset['part'] !== 'roof' && kutu.dataset['part'] !== 'right_rear_door') {
        fireEvent.change(kutu, { target: { value: 'original' } })
      }
    }

    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    expect((await screen.findByTestId('paint-change-missing')).textContent).toBe(
      'Harita yarım kaldı. Şu parçalar için durum seçin: Tavan ve Sağ arka kapı.',
    )
    const hatali = kutular(alan).filter((kutu) => kutu.getAttribute('aria-invalid') === 'true')
    expect(hatali.map((kutu) => kutu.dataset['part'])).toEqual(['roof', 'right_rear_door'])
    // Gönderilen harita yalnız seçilen 11 parçayı taşıyordu — eksikleri sunucu söyledi.
    expect(gonderilenHarita()?.partMap).toHaveLength(11)
  })

  it('0016a/AC-30: Ret sonrasında diğer 11 seçim korunur; iki kutu doldurulup yeniden kaydedilince kayıt kabul edilir', async () => {
    vi.mocked(updateVehicle).mockRejectedValueOnce(
      new ApiError('vehicle.paint_change_map_incomplete', null, 422, 'paint_change_map', {
        parts: 'roof,right_rear_door',
      }),
    )
    const alan = await ekran(kayit(null))
    for (const kutu of kutular(alan)) {
      if (kutu.dataset['part'] !== 'roof' && kutu.dataset['part'] !== 'right_rear_door') {
        fireEvent.change(kutu, { target: { value: 'painted' } })
      }
    }
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))
    await screen.findByTestId('paint-change-missing')

    expect(kutular(alan).filter((kutu) => kutu.value === 'painted')).toHaveLength(11)

    fireEvent.change(within(alan).getByLabelText('Tavan'), { target: { value: 'original' } })
    fireEvent.change(within(alan).getByLabelText('Sağ arka kapı'), {
      target: { value: 'replaced' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await screen.findByTestId('vehicle-edit-saved')
    expect(gonderilenHarita(1)?.partMap).toHaveLength(13)
    expect(gonderilenHarita(1)?.partMap?.map((entry) => entry.part)).toEqual([
      ...PAINT_CHANGE_PARTS,
    ])
  })

  it('0016a/AC-31: 13 kutu boşaltılıp kaydedildiğinde boş harita (temizleme) gönderilir; form yeniden açıldığında 13 kutu boş gelir', async () => {
    const alan = await ekran(kayit(AC5))
    for (const kutu of kutular(alan)) {
      fireEvent.change(kutu, { target: { value: '' } })
    }
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))
    await screen.findByTestId('vehicle-edit-saved')

    expect(gonderilenHarita()?.partMap).toEqual([])

    cleanup()
    const yeniden = await ekran(kayit(null))
    expect(kutular(yeniden).map((kutu) => kutu.value)).toEqual(Array(13).fill(''))
  })

  it('0016a/AC-22 (uç yarısı): haritası olmayan araçta kutulara dokunulmadan kaydedilince harita isteğe yazılmaz', async () => {
    await ekran(kayit(null))
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))
    await screen.findByTestId('vehicle-edit-saved')

    expect(gonderilenHarita()).toBeUndefined()
  })

  it('0016a/AC-33: 13 parça adı, dört durum adı ve eksik parça hata metni üç katalogda da doludur', () => {
    const anahtarlar = [
      ...PAINT_CHANGE_PARTS.map((part) => `paintChange.part.${part}`),
      ...PAINT_CHANGE_STATES.map((state) => `paintChange.state.${state}`),
      'vehicle.form.error.paint_change_map_incomplete',
      'vehicle.field.paint_change_map',
    ]

    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(catalogKeys(locale)).toEqual(expect.arrayContaining(anahtarlar))
      const t = createTranslator(locale)
      for (const anahtar of anahtarlar) {
        expect(t(anahtar, { parts: 'x' }).trim()).not.toBe('')
      }
    }
  })
})
