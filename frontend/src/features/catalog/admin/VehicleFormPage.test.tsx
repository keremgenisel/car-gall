import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { AppLocale } from '../../../shared/lib/locale'
import { VehicleFormPage } from './VehicleFormPage'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir.
 * R22'nin DÖRT durumu burada doğrulanıyor: seçenekler yüklenirken, kayıt sırasında,
 * başarıda ve hatada (AC-40…AC-43).
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchReferenceOptions: vi.fn(),
  fetchBranches: vi.fn(),
  createVehicle: vi.fn(),
}))

const { fetchReferenceOptions, fetchBranches, createVehicle } =
  await import('../../../shared/api/catalogAdmin')

/** Altı referans kümesi; etiketler personelin locale'inde gelir (AC-13). */
const SECENEKLER = {
  models: [{ value: '0192c000-0000-7000-8000-00000000000a', label: 'Toyota Corolla' }],
  bodyTypes: [{ value: 'suv', label: 'SUV' }],
  fuelTypes: [{ value: 'petrol', label: 'Benzin' }],
  transmissions: [{ value: 'automatic', label: 'Otomatik' }],
  drivetrains: [{ value: 'fwd', label: 'Önden çekiş' }],
  trimLevels: [{ value: 'base', label: 'Standart' }],
}

function ekran(locale: AppLocale = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <VehicleFormPage />
    </LocaleProvider>,
  )
}

function yaz(etiket: string, deger: string) {
  fireEvent.change(screen.getByLabelText(etiket), { target: { value: deger } })
}

/** Çekirdek alanların hepsini doldurur: R10 eksik alanda isteği hiç atmıyor. */
function formuDoldur() {
  yaz('Şube', '0192c000-0000-7000-8000-00000000b001')
  yaz('Model', SECENEKLER.models[0].value)
  yaz('Kasa tipi', 'suv')
  yaz('Donanım paketi', 'base')
  yaz('Yakıt', 'petrol')
  yaz('Vites', 'automatic')
  yaz('Çekiş', 'fwd')
  yaz('Model yılı', '2019')
  yaz('Kilometre', '125000')
  yaz('Renk', 'Beyaz')
  yaz('Şase numarası', 'VIN0000000000001')
  yaz('Fiyat', '750000')
}

function kaydet() {
  fireEvent.click(screen.getByRole('button', { name: 'Aracı kaydet' }))
}

beforeEach(() => {
  vi.mocked(fetchReferenceOptions).mockResolvedValue(SECENEKLER)
  vi.mocked(fetchBranches).mockResolvedValue([
    { branchId: '0192c000-0000-7000-8000-00000000b001', name: 'Merkez Şube', status: 'Active' },
    { branchId: '0192c000-0000-7000-8000-00000000b002', name: 'Liman Şube', status: 'Closed' },
  ])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0010a/AC-12: Panel; model, kasa tipi, donanım paketi, yakıt, vites ve çekiş için referans veriden gelen seçenekleri listeler', () => {
  it('altı kümenin hepsi kendi seçim alanında basılır', async () => {
    ekran()

    for (const etiket of ['Model', 'Kasa tipi', 'Donanım paketi', 'Yakıt', 'Vites', 'Çekiş']) {
      expect(await screen.findByLabelText(etiket)).toBeTruthy()
    }

    expect(await screen.findByRole('option', { name: 'Toyota Corolla' })).toBeTruthy()
    expect(screen.getByRole('option', { name: 'SUV' })).toBeTruthy()
    expect(screen.getByRole('option', { name: 'Benzin' })).toBeTruthy()
    expect(screen.getByRole('option', { name: 'Otomatik' })).toBeTruthy()
    expect(screen.getByRole('option', { name: 'Önden çekiş' })).toBeTruthy()
    expect(screen.getByRole('option', { name: 'Standart' })).toBeTruthy()
  })
})

describe('0010a/AC-13: Referans listeleri personelin locale indeki etiketlerle gelir; hiçbir yerde ham slug gösterilmez', () => {
  it('seçeneklerin metni etikettir, değeri slug', async () => {
    ekran()

    const kasa = await screen.findByLabelText('Kasa tipi')
    const secenek = screen.getByRole('option', { name: 'SUV' }) as HTMLOptionElement

    expect(secenek.value).toBe('suv')
    expect(kasa.textContent).not.toContain('suv')
  })

  it('kapatılmış şube durumuyla etiketlenir; ham kod görünmez', async () => {
    ekran()

    expect(await screen.findByRole('option', { name: 'Liman Şube (Kapalı)' })).toBeTruthy()
    expect(screen.getByLabelText('Şube').textContent).not.toContain('Closed')
  })
})

describe('0010a/AC-40: Araç kaydedilirken kaydet düğmesi yükleniyor hâline geçer ve form iki kez gönderilemez', () => {
  it('gönderim sürerken düğme devre dışı kalır ve uç bir kez çağrılır', async () => {
    let cozumle: (value: string | null) => void = () => {}
    vi.mocked(createVehicle).mockReturnValue(
      new Promise<string | null>((resolve) => {
        cozumle = resolve
      }),
    )

    ekran()
    await screen.findByTestId('vehicle-form')
    formuDoldur()

    kaydet()

    const dugme = await screen.findByRole('button', { name: 'Kaydediliyor' })

    expect(dugme).toHaveProperty('disabled', true)
    expect(dugme.getAttribute('aria-busy')).toBe('true')

    // İkinci tıklama: devre dışı düğme çağrı üretmez.
    fireEvent.click(dugme)

    expect(vi.mocked(createVehicle)).toHaveBeenCalledTimes(1)

    cozumle('0192c000-0000-7000-8000-000000000001')
    await screen.findByTestId('vehicle-saved')
  })
})

describe('0010a/AC-41: Araç kaydedildiğinde personele kaydedildiği bildirilir', () => {
  it('başarı bildirimi basılır ve listeye giden eylem sunulur', async () => {
    vi.mocked(createVehicle).mockResolvedValue('0192c000-0000-7000-8000-000000000001')

    ekran()
    await screen.findByTestId('vehicle-form')
    formuDoldur()
    kaydet()

    const bildirim = await screen.findByTestId('vehicle-saved')

    expect(bildirim.textContent).toContain('Araç kaydedildi')
    expect(bildirim.getAttribute('role')).toBe('status')
    expect(screen.getByRole('link', { name: 'Araç listesine git' })).toBeTruthy()
  })
})

describe('0010a/AC-42: Doğrulama hatası olan alan işaretlenir ve hata personelin dilinde yazılır', () => {
  it('sunucunun işaretlediği alan aria-invalid alır ve mesaj personelin dilinde basılır', async () => {
    vi.mocked(createVehicle).mockRejectedValue(
      new ApiError('vehicle.duplicate_vin', null, 409, 'vin'),
    )

    ekran()
    await screen.findByTestId('vehicle-form')
    formuDoldur()
    kaydet()

    const sase = await screen.findByLabelText('Şase numarası')

    expect(sase.getAttribute('aria-invalid')).toBe('true')
    expect(await screen.findByText('Bu şase numarasıyla kayıtlı bir araç zaten var.')).toBeTruthy()

    // Hata ALANA bağlı: genel şerit basılmaz, aksi hâlde aynı hata iki yerde görünürdü.
    expect(screen.queryByTestId('vehicle-form-error')).toBeNull()
  })

  it('renk boş bırakılırsa alan işaretlenir ve istek atılmaz (R2, AC-6)', async () => {
    ekran()
    await screen.findByTestId('vehicle-form')

    formuDoldur()
    yaz('Renk', '')
    kaydet()

    expect(screen.getByLabelText('Renk').getAttribute('aria-invalid')).toBe('true')
    expect(vi.mocked(createVehicle)).not.toHaveBeenCalled()
  })

  it('renk personelin KENDİ locale anahtarıyla gönderilir', async () => {
    vi.mocked(createVehicle).mockResolvedValue('0192c000-0000-7000-8000-000000000001')

    ekran('en')
    await screen.findByTestId('vehicle-form')

    // Etiketler İngilizce; alanlar da öyle sorgulanıyor.
    fireEvent.change(screen.getByLabelText('Branch'), {
      target: { value: '0192c000-0000-7000-8000-00000000b001' },
    })
    fireEvent.change(screen.getByLabelText('Model'), {
      target: { value: SECENEKLER.models[0].value },
    })
    fireEvent.change(screen.getByLabelText('Body type'), { target: { value: 'suv' } })
    fireEvent.change(screen.getByLabelText('Trim level'), { target: { value: 'base' } })
    fireEvent.change(screen.getByLabelText('Fuel'), { target: { value: 'petrol' } })
    fireEvent.change(screen.getByLabelText('Transmission'), { target: { value: 'automatic' } })
    fireEvent.change(screen.getByLabelText('Drivetrain'), { target: { value: 'fwd' } })
    fireEvent.change(screen.getByLabelText('Model year'), { target: { value: '2019' } })
    fireEvent.change(screen.getByLabelText('Mileage'), { target: { value: '125000' } })
    fireEvent.change(screen.getByLabelText('Colour'), { target: { value: 'White' } })
    fireEvent.change(screen.getByLabelText('VIN'), { target: { value: 'VIN0000000000001' } })
    fireEvent.change(screen.getByLabelText('Price'), { target: { value: '750000' } })

    fireEvent.click(screen.getByRole('button', { name: 'Save vehicle' }))

    await screen.findByTestId('vehicle-saved')

    expect(vi.mocked(createVehicle).mock.calls[0][0].color).toEqual({ en: 'White' })
  })

  it('eksik çekirdek alan istek atılmadan işaretlenir (R10)', async () => {
    ekran()
    await screen.findByTestId('vehicle-form')

    kaydet()

    expect(screen.getByLabelText('Şube').getAttribute('aria-invalid')).toBe('true')
    expect(vi.mocked(createVehicle)).not.toHaveBeenCalled()
  })
})

describe('0010a/AC-43: Kayıt başarısız olduğunda girilen bilgiler kaybolmaz ve tekrar dene eylemi sunulur', () => {
  it('değerler yerinde kalır ve tekrar dene aynı isteği yeniden gönderir', async () => {
    vi.mocked(createVehicle).mockRejectedValue(new ApiError('api.unavailable', null))

    ekran()
    await screen.findByTestId('vehicle-form')
    formuDoldur()
    kaydet()

    const serit = await screen.findByTestId('vehicle-form-error')

    expect(serit.textContent).toContain('Kayıt şu anda tamamlanamadı')
    expect(screen.getByLabelText('Şase numarası')).toHaveProperty('value', 'VIN0000000000001')
    expect(screen.getByLabelText('Kilometre')).toHaveProperty('value', '125000')

    vi.mocked(createVehicle).mockResolvedValue('0192c000-0000-7000-8000-000000000001')
    fireEvent.click(screen.getByRole('button', { name: 'Tekrar dene' }))

    await screen.findByTestId('vehicle-saved')

    expect(vi.mocked(createVehicle)).toHaveBeenCalledTimes(2)
    expect(vi.mocked(createVehicle).mock.calls[1][0]).toMatchObject({
      vin: 'VIN0000000000001',
      mileageKilometers: 125000,
    })
  })
})

describe('0010a/AC-47: Hiçbir hata ekranında HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('bilinmeyen kod genel mesaja düşer; kod, durum ve traceId ekranda yok', async () => {
    vi.mocked(createVehicle).mockRejectedValue(
      new ApiError('vehicle.exploded_in_production', 'trace-9f3', 500),
    )

    ekran()
    await screen.findByTestId('vehicle-form')
    formuDoldur()
    kaydet()

    await screen.findByTestId('vehicle-form-error')

    for (const sizinti of ['500', 'trace-9f3', 'vehicle.exploded_in_production', '/api/admin']) {
      expect(document.body.textContent, sizinti).not.toContain(sizinti)
    }
  })

  it('seçenekler getirilemediğinde tekrar dene sunulur ve detay sızmaz', async () => {
    vi.mocked(fetchBranches).mockRejectedValue(new ApiError('api.unavailable', 'trace-77', 503))

    ekran()

    const hata = await screen.findByTestId('vehicle-form-sources-error')

    expect(hata.textContent).toContain('Tekrar dene')
    expect(hata.textContent).not.toContain('503')
    expect(hata.textContent).not.toContain('trace-77')
  })
})

describe('Üç durum (docs/frontend.md § 3) — seçenek yüklemesi', () => {
  it('yükleniyor: iskelet basılır, form basılmaz', () => {
    vi.mocked(fetchReferenceOptions).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('vehicle-form-skeleton')).toBeTruthy()
    expect(screen.queryByTestId('vehicle-form')).toBeNull()
  })
})

describe('0010a/AC-50: Form etiketleri ve alan adları TR, AR ve EN de kendi dilinde görünür; hiçbir locale de çeviri anahtarı ham hâliyle görünür', () => {
  it.each([
    ['tr', 'Şase numarası'],
    ['en', 'VIN'],
    ['ar', 'رقم الهيكل'],
  ] as const)('%s etiketleri kendi dilinde basılır', async (locale, beklenen) => {
    ekran(locale)

    expect(await screen.findByLabelText(beklenen)).toBeTruthy()
    expect(document.body.textContent).not.toContain('vehicle.form.field')
  })
})

describe('P4 — "fiyat sorunuz" tutarla birlikte gönderilmez', () => {
  it('işaret konduğunda tutar alanı kalkar ve istek tutar taşımaz', async () => {
    vi.mocked(createVehicle).mockResolvedValue(null)

    ekran()
    await screen.findByTestId('vehicle-form')
    formuDoldur()

    fireEvent.click(screen.getByLabelText('Fiyat sorunuz olarak işaretle'))

    await waitFor(() => expect(screen.queryByLabelText('Fiyat')).toBeNull())

    kaydet()
    await screen.findByTestId('vehicle-saved')

    expect(vi.mocked(createVehicle).mock.calls[0][0]).toMatchObject({
      priceOnRequest: true,
      priceAmount: null,
    })
  })
})
