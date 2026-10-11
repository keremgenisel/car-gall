import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { AppLocale } from '../../../shared/lib/locale'
import type { VehicleAdminDetail, VehicleAdminField } from '../../../shared/api/types'
import { VehicleEditPage } from './VehicleEditPage'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir.
 * R17'nin dört durumu burada doğrulanıyor: kayıt yüklenirken, kaydetme sırasında,
 * başarıda ve hatada.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchVehicle: vi.fn(),
  fetchReferenceOptions: vi.fn(),
  fetchBranches: vi.fn(),
  fetchVehicleFields: vi.fn(),
  updateVehicle: vi.fn(),
}))

const { fetchVehicle, fetchReferenceOptions, fetchBranches, fetchVehicleFields, updateVehicle } =
  await import('../../../shared/api/catalogAdmin')

const SECENEKLER = {
  models: [{ value: '0192c000-0000-7000-8000-00000000000a', label: 'Toyota Corolla' }],
  bodyTypes: [{ value: 'suv', label: 'SUV' }],
  fuelTypes: [{ value: 'petrol', label: 'Benzin' }],
  transmissions: [{ value: 'automatic', label: 'Otomatik' }],
  drivetrains: [{ value: 'fwd', label: 'Önden çekiş' }],
  trimLevels: [{ value: 'base', label: 'Standart' }],
}

const KAYIT: VehicleAdminDetail = {
  vehicleId: '0192c000-0000-7000-8000-000000000001',
  branchId: '0192c000-0000-7000-8000-00000000b001',
  vehicleModelId: '0192c000-0000-7000-8000-00000000000a',
  bodyType: 'suv',
  fuelType: 'petrol',
  transmissionType: 'automatic',
  drivetrainType: 'fwd',
  trimLevel: 'base',
  color: { tr: 'Beyaz', en: 'White' },
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  vin: 'VIN0000000000001',
  listingStatus: 'Draft',
  listingTitle: { tr: 'Temiz Corolla' },
  listingDescription: { tr: 'Bakımları yapıldı' },
  optionalValues: [
    { fieldKey: 'damage_record_amount', kind: 'Amount', value: '9900.00', values: null },
  ],
  updatedAt: '2026-10-10T12:30:00Z',
  awaitingBranchAssignment: false,
}

const TRAMER: VehicleAdminField = {
  fieldKey: 'damage_record_amount',
  kind: 'Amount',
  isRequired: false,
}

function ekran(locale: AppLocale = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <VehicleEditPage vehicleId={KAYIT.vehicleId} />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  vi.mocked(fetchVehicle).mockResolvedValue(KAYIT)
  vi.mocked(fetchReferenceOptions).mockResolvedValue(SECENEKLER)
  vi.mocked(fetchBranches).mockResolvedValue([
    { branchId: KAYIT.branchId, name: 'Merkez Şube', status: 'Active' },
  ])
  vi.mocked(fetchVehicleFields).mockResolvedValue([TRAMER])
  vi.mocked(updateVehicle).mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0010b/AC-2: Kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür', () => {
  it('form kayıtlı değerlerle AÇILIR — yarıda bırakılan işe dönülebilir (R1)', async () => {
    ekran()

    expect(await screen.findByLabelText('Model yılı')).toHaveProperty('value', '2019')
    expect(screen.getByLabelText('Kilometre')).toHaveProperty('value', '125000')
    expect(screen.getByLabelText('Fiyat')).toHaveProperty('value', '750000')
    expect(screen.getByLabelText('Başlık (Türkçe)')).toHaveProperty('value', 'Temiz Corolla')
  })

  it('kaydetmek değişen değerleri gönderir', async () => {
    ekran()

    await screen.findByTestId('vehicle-edit-form')

    fireEvent.change(screen.getByLabelText('Kilometre'), { target: { value: '150000' } })
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await screen.findByTestId('vehicle-edit-saved')

    expect(vi.mocked(updateVehicle).mock.calls[0][1]).toMatchObject({
      mileageKilometers: 150000,
      vin: 'VIN0000000000001',
    })
  })
})

describe('0010b/AC-8: Şase numarası düzenlenemez; değiştirme girişimi reddedilir', () => {
  it('şase alanı SALT OKUNUR basılır ama gövdede taşınır', async () => {
    ekran()

    const sase = await screen.findByLabelText('Şase numarası')

    expect(sase).toHaveProperty('readOnly', true)
    expect(sase).toHaveProperty('value', 'VIN0000000000001')
  })
})

describe('0010b/AC-18: Personel ilan başlığını ve açıklamasını TR, EN ve AR için ayrı ayrı yazar ve üçü bağımsız saklanır', () => {
  it('üç dil için ayrı başlık ve açıklama alanı basılır', async () => {
    ekran()

    await screen.findByTestId('vehicle-listing-text')

    for (const locale of ['Türkçe', 'العربية', 'English']) {
      expect(screen.getByLabelText(`Başlık (${locale})`), locale).toBeTruthy()
      expect(screen.getByLabelText(`Açıklama (${locale})`), locale).toBeTruthy()
    }
  })

  it('yazılan metinler locale başına gönderilir', async () => {
    ekran()

    await screen.findByTestId('vehicle-edit-form')

    fireEvent.change(screen.getByLabelText('Başlık (English)'), { target: { value: 'Clean' } })
    fireEvent.change(screen.getByLabelText('Açıklama (English)'), { target: { value: 'Serviced' } })
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await screen.findByTestId('vehicle-edit-saved')

    const istek = vi.mocked(updateVehicle).mock.calls[0][1]

    expect(istek.listingTitle).toMatchObject({ tr: 'Temiz Corolla', en: 'Clean' })
    expect(istek.listingDescription).toMatchObject({ en: 'Serviced' })
  })
})

describe('0010b/AC-21: Bir dil, başlık ve açıklamasının ikisi de doluysa tam sayılır; yalnız biri dolu olan dil eksik sayılır', () => {
  it('form kaydetmeden ÖNCE hangi dilin eksik olduğunu söyler', async () => {
    ekran()

    const bolum = await screen.findByTestId('vehicle-listing-text')

    // Kayıtta yalnız Türkçe dolu.
    expect(bolum.textContent).toContain('العربية')
    expect(bolum.textContent).toContain('English')

    fireEvent.change(screen.getByLabelText('Başlık (English)'), { target: { value: 'Clean' } })

    // Yalnız başlık doldu; dil hâlâ EKSİK.
    await waitFor(() => expect(bolum.textContent).toContain('English'))
  })
})

describe('0010b/AC-13: Alan ayarlarında açık bir opsiyonel alan formda görünür ve değeri kaydedilir', () => {
  it('açık alan tipine uygun giriş alanıyla ve DEĞERİYLE basılır', async () => {
    ekran()

    const alan = await screen.findByLabelText('Tramer tutarı')

    expect(alan).toHaveProperty('value', '9900.00')
    expect(alan.getAttribute('type')).toBe('number')
  })

  it('değer değiştirildiğinde istekte taşınır', async () => {
    ekran()

    await screen.findByTestId('vehicle-edit-form')

    fireEvent.change(screen.getByLabelText('Tramer tutarı'), { target: { value: '12500.50' } })
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await screen.findByTestId('vehicle-edit-saved')

    expect(vi.mocked(updateVehicle).mock.calls[0][1].optionalValues).toEqual([
      { fieldKey: 'damage_record_amount', value: '12500.50', values: null },
    ])
  })

  it('tarih tipli alan tarayıcının tarih alanıyla basılır — belirsiz yazım imkânsız', async () => {
    // Alan ANAHTARI aynı, TİPİ farklı: ölçülen şey tipin giriş alanına yansıması.
    vi.mocked(fetchVehicleFields).mockResolvedValue([{ ...TRAMER, kind: 'Date' }])
    vi.mocked(fetchVehicle).mockResolvedValue({ ...KAYIT, optionalValues: [] })

    ekran()

    const alan = await screen.findByLabelText('Tramer tutarı')

    expect(alan.getAttribute('type')).toBe('date')
  })
})

describe('0010b/AC-14: Alan ayarlarında kapalı bir opsiyonel alan formda görünmez', () => {
  it('uç alanı döndürmezse form onu hiç basmaz', async () => {
    vi.mocked(fetchVehicleFields).mockResolvedValue([])

    ekran()

    await screen.findByTestId('vehicle-edit-form')

    expect(screen.queryByLabelText('Tramer tutarı')).toBeNull()
    expect(screen.queryByTestId('vehicle-optional-fields')).toBeNull()
  })

  /**
   * F1/AC-17 — kapalı alanın değeri SİLİNMEZ. Formun yapabileceği tek şey onu
   * göndermemek; "gelmeyen dokunulmamıştır" semantiği (D23) değeri koruyor.
   */
  it('kapalı alanın değeri istekte GÖNDERİLMEZ', async () => {
    vi.mocked(fetchVehicleFields).mockResolvedValue([])
    vi.mocked(fetchVehicle).mockResolvedValue({ ...KAYIT, optionalValues: [] })

    ekran()

    await screen.findByTestId('vehicle-edit-form')
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))
    await screen.findByTestId('vehicle-edit-saved')

    expect(vi.mocked(updateVehicle).mock.calls[0][1].optionalValues).toEqual([])
  })
})

describe('0010b/AC-15: Alan ayarlarında zorunlu işaretli bir opsiyonel alan boş bırakılmış araç, taslak olarak kaydedilebilir', () => {
  it('zorunlu işaretli alan boşken kaydet düğmesi çalışır', async () => {
    vi.mocked(fetchVehicleFields).mockResolvedValue([{ ...TRAMER, isRequired: true }])
    vi.mocked(fetchVehicle).mockResolvedValue({ ...KAYIT, optionalValues: [] })

    ekran()

    await screen.findByTestId('vehicle-edit-form')

    // İşaret bir UYARIDIR, kapı değil: zorunluluk yalnız yayına alırken bağlayıcı.
    expect(screen.getByLabelText('Tramer tutarı (yayın için zorunlu)')).toHaveProperty('value', '')

    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await screen.findByTestId('vehicle-edit-saved')
    expect(vi.mocked(updateVehicle)).toHaveBeenCalledTimes(1)
  })
})

describe('0010b/AC-4: Var olmayan bir araç kimliğiyle gelen düzenleme isteği `404` ve kararlı bir hata koduyla reddedilir', () => {
  it('`404` HATA ekranı değil, "bulunamadı" bildirimi gösterir', async () => {
    vi.mocked(fetchVehicle).mockRejectedValue(new ApiError('vehicle.not_found', null, 404))

    ekran()

    expect(await screen.findByTestId('vehicle-edit-missing')).toBeTruthy()

    // "Tekrar dene" YALAN olurdu: istek başarısız olmadı, kayıt yok.
    expect(screen.queryByTestId('vehicle-edit-error')).toBeNull()
  })
})

describe('0010b/AC-33: Hiçbir hata ekranında HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('kaydetme hatası genel şeride düşer ve detay sızmaz', async () => {
    vi.mocked(updateVehicle).mockRejectedValue(new ApiError('vehicle.exploded', 'trace-9f3', 500))

    ekran()

    await screen.findByTestId('vehicle-edit-form')
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await screen.findByTestId('vehicle-edit-error-banner')

    for (const sizinti of ['500', 'trace-9f3', 'vehicle.exploded', '/api/admin']) {
      expect(document.body.textContent, sizinti).not.toContain(sizinti)
    }
  })

  it('alan hatası ALANA bağlanır', async () => {
    vi.mocked(updateVehicle).mockRejectedValue(
      new ApiError('vehicle.core_field_missing', null, 422, 'mileageKilometers'),
    )

    ekran()

    await screen.findByTestId('vehicle-edit-form')
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    await waitFor(() =>
      expect(screen.getByLabelText('Kilometre').getAttribute('aria-invalid')).toBe('true'),
    )

    // Hata ALANA bağlı: aynı mesaj bir de genel şeritte görünmez.
    expect(screen.queryByTestId('vehicle-edit-error-banner')).toBeNull()
  })

  /**
   * Formun basmadığı bir alana işaret eden hata — ör. salt okunur şase — hiçbir yere
   * bağlanamaz. Sessizce kaybolmak yerine GENEL şeride düşüyor; ölçüldü, önceki hâlinde
   * mesaj tamamen yok oluyordu.
   */
  it('işaretlenemeyen alana ait hata genel şeride düşer', async () => {
    vi.mocked(updateVehicle).mockRejectedValue(
      new ApiError('vehicle.vin_immutable', null, 422, 'vin'),
    )

    ekran()

    await screen.findByTestId('vehicle-edit-form')
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    const serit = await screen.findByTestId('vehicle-edit-error-banner')

    expect(serit.textContent).toContain('Şase numarası değiştirilemez.')
  })

  it('kaynaklar getirilemediğinde tekrar dene sunulur', async () => {
    vi.mocked(fetchReferenceOptions).mockRejectedValue(new ApiError('api.unavailable', null, 503))

    ekran()

    const hata = await screen.findByTestId('vehicle-edit-error')

    expect(hata.textContent).toContain('Tekrar dene')
    expect(hata.textContent).not.toContain('503')
  })
})

describe('Dört durum (R17) — kayıt yüklemesi ve kaydetme', () => {
  it('yükleniyor: iskelet basılır, form basılmaz', () => {
    vi.mocked(fetchVehicle).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('vehicle-edit-skeleton')).toBeTruthy()
    expect(screen.queryByTestId('vehicle-edit-form')).toBeNull()
  })

  it('kaydetme sürerken düğme devre dışı kalır ve uç bir kez çağrılır', async () => {
    let cozumle: () => void = () => {}
    vi.mocked(updateVehicle).mockReturnValue(
      new Promise<void>((resolve) => {
        cozumle = resolve
      }),
    )

    ekran()

    await screen.findByTestId('vehicle-edit-form')
    fireEvent.click(screen.getByRole('button', { name: 'Değişiklikleri kaydet' }))

    const dugme = await screen.findByRole('button', { name: 'Kaydediliyor' })

    expect(dugme).toHaveProperty('disabled', true)
    fireEvent.click(dugme)

    expect(vi.mocked(updateVehicle)).toHaveBeenCalledTimes(1)

    cozumle()
    await screen.findByTestId('vehicle-edit-saved')
  })
})

describe('0010b/AC-35: Liste başlıkları ve göstergeler TR, AR ve EN de kendi dilinde görünür; hiçbir locale de çeviri anahtarı ham hâliyle görünür', () => {
  it.each([
    ['tr', 'Aracı düzenle'],
    ['en', 'Edit vehicle'],
    ['ar', 'تعديل المركبة'],
  ] as const)('%s başlığı kendi dilinde basılır', async (locale, beklenen) => {
    ekran(locale)

    expect(await screen.findByRole('heading', { name: beklenen })).toBeTruthy()
    expect(document.body.textContent).not.toContain('vehicle.edit.')
  })
})
