import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type {
  PagedResult,
  VehicleAdminListItem,
  VehicleAdminSummary,
} from '../../../shared/api/types'
import { VehicleListPage } from './VehicleListPage'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir ve
 * ÜÇ DURUMUN ÜÇÜ DE doğrulanır. Sorgular kullanıcının GÖRDÜĞÜNE göre yapılır.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchVehicles: vi.fn(),
  fetchVehicleSummary: vi.fn(),
  applyListingFeature: vi.fn(),
  applyBulkListingAction: vi.fn(),
}))

const { fetchVehicles, fetchVehicleSummary, applyListingFeature, applyBulkListingAction } =
  await import('../../../shared/api/catalogAdmin')

const arac: VehicleAdminListItem = {
  vehicleId: '0192c000-0000-7000-8000-000000000001',
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  listingStatus: 'Draft',
  vin: 'VIN0000000000001',
  isFeatured: false,
  awaitingBranchAssignment: false,
  updatedAt: '2026-10-10T12:30:00Z',
  missingLocales: [],
  hasMissingRequiredInfo: false,
}

/** 0010b — liste yanıtı `PagedResult` zarfında (AC-25). */
function sayfa(
  items: readonly VehicleAdminListItem[],
  page = 1,
  pageSize = 25,
  totalCount = items.length,
): PagedResult<VehicleAdminListItem> {
  return {
    items,
    page,
    pageSize,
    totalCount,
    totalPages: Math.max(1, Math.ceil(totalCount / pageSize)),
  }
}

const ozet: VehicleAdminSummary = { totalCount: 1, translationIncompleteCount: 0 }

function ekran(locale: 'tr' | 'ar' | 'en' = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <VehicleListPage />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  window.history.replaceState(null, '', '/admin/vehicles')
  vi.mocked(fetchVehicleSummary).mockResolvedValue(ozet)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0010a/AC-26: Kaydedilen araç sade listede araç tanımı, fiyat ve kilometreyle görünür', () => {
  it('araç tanımı, fiyat ve kilometre basılır', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac]))

    ekran()

    const satir = await screen.findByTestId('vehicle-row')

    expect(satir.textContent).toContain('Toyota Corolla')
    expect(satir.textContent).toContain('2019')

    // Fiyat ve kilometre personelin locale'inde biçimlenir; ham sayı basılmaz.
    expect(screen.getByRole('table').textContent).toContain('750.000')
    expect(screen.getByRole('table').textContent).toContain('125.000 km')
  })

  it('yıl gruplama ayracı ALMAZ: 2019, "2.019" değil', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac]))

    ekran()

    const satir = await screen.findByTestId('vehicle-row')

    expect(satir.textContent).not.toContain('2.019')
  })

  it('"fiyat sorunuz" tutar yerine kendi metnini gösterir (P4)', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(
      sayfa([
        {
          ...arac,
          price: { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null },
        },
      ]),
    )

    ekran()

    expect(await screen.findByText('Fiyat sorunuz')).toBeTruthy()
    expect(screen.getByRole('table').textContent).not.toContain('0,00')
  })

  it('durum KODU çeviri kataloğundan basılır; ham kod görünmez (C11, Yasak Listesi 22)', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac]))

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
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([]))

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
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac]))

    ekran(locale)

    expect(await screen.findByText(beklenen)).toBeTruthy()
    expect(document.body.textContent).not.toContain('vehicle.column')
  })
})

describe('0010b/AC-24: Liste araç tanımını, fiyatı, kilometreyi, yayın durumunu, eksik dil göstergesini, eksik bilgi göstergesini ve son güncelleme zamanını gösterir', () => {
  it('yedi sütunun hepsi basılır', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(
      sayfa([{ ...arac, missingLocales: ['ar'], hasMissingRequiredInfo: true }]),
    )

    ekran()

    await screen.findByTestId('vehicle-table')

    for (const baslik of ['Araç', 'Fiyat', 'Kilometre', 'Durum', 'Tamlık', 'Son güncelleme']) {
      expect(screen.getByRole('columnheader', { name: baslik }), baslik).toBeTruthy()
    }

    // Son güncelleme UÇTA biçimleniyor; ISO dizesi ham hâliyle görünmez.
    expect(screen.getByRole('table').textContent).not.toContain('2026-10-10T12:30:00Z')
    expect(screen.getByRole('table').textContent).toContain('10.10.2026')
  })
})

describe('0010b/AC-22: Panel, metni eksik olan dilleri araç başına gösterir', () => {
  it('eksik dil rozeti dilin ADINI taşır, kodunu değil', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([{ ...arac, missingLocales: ['ar', 'en'] }]))

    ekran()

    const rozet = await screen.findByTestId('vehicle-missing-locales')

    // Dil adları KENDİ dillerinde yazılı (`app.locale.*`, 0001'den gelen desen); ham
    // locale kodu (`ar`, `en`) ziyaretçiye gösterilmez.
    expect(rozet.textContent).toContain('العربية')
    expect(rozet.textContent).toContain('English')
    expect(rozet.textContent).not.toContain('ar,')
  })

  it('eksiksiz araçta rozet yok', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac]))

    ekran()

    await screen.findByTestId('vehicle-table')

    expect(screen.queryByTestId('vehicle-missing-locales')).toBeNull()
    expect(screen.queryByTestId('vehicle-missing-info')).toBeNull()
    expect(screen.getByText('Tam')).toBeTruthy()
  })

  it('eksik bilgi göstergesi ayrı bir rozettir', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([{ ...arac, hasMissingRequiredInfo: true }]))

    ekran()

    expect((await screen.findByTestId('vehicle-missing-info')).textContent).toContain('Eksik bilgi')
  })
})

describe('0010b/AC-23: Liste, çeviri eksik ilan sayısını tüm araçlar üzerinden gösterir; gösterilen sayı o anki sayfanın içeriğine bağlı değildir', () => {
  it('özet SAYFADAN değil, özet ucundan gelir', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac], 1, 25, 40))
    vi.mocked(fetchVehicleSummary).mockResolvedValue({
      totalCount: 40,
      translationIncompleteCount: 7,
    })

    ekran()

    const ozetMetni = await screen.findByTestId('vehicle-summary')

    // Sayfada tek araç var ve o eksiksiz; sayı yine de 7.
    expect(ozetMetni.textContent).toContain('7')
    expect(ozetMetni.textContent).toContain('40')
  })
})

describe('0010b/AC-27: Yapılandırılan sayfa boyutunun bir fazlası kadar araç varken ikinci sayfa tam olarak bir araç gösterir', () => {
  it('sayfa göstergesi ve ileri düğmesi basılır', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac], 1, 25, 26))

    ekran()

    expect((await screen.findByTestId('vehicle-page-indicator')).textContent).toContain('1')
    expect(screen.getByRole('button', { name: 'Sonraki' })).toHaveProperty('disabled', false)

    // İlk sayfada geri gidilemez.
    expect(screen.getByRole('button', { name: 'Önceki' })).toHaveProperty('disabled', true)
  })

  it('tek sayfalık listede sayfalayıcı hiç basılmaz', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac], 1, 25, 1))

    ekran()

    await screen.findByTestId('vehicle-table')

    expect(screen.queryByTestId('vehicle-page-indicator')).toBeNull()
  })

  it('ileri gitmek SONRAKİ sayfayı ister ve adrese yazar', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac], 1, 25, 26))

    ekran()

    fireEvent.click(await screen.findByRole('button', { name: 'Sonraki' }))

    await waitFor(() => expect(vi.mocked(fetchVehicles).mock.calls.at(-1)?.[1]).toBe(2))
    expect(window.location.search).toContain('page=2')
  })
})

describe('0010b/AC-28: Ekleme ve güncelleme sıraları birbirinden farklı olan en az üç araçla, son güncellenen ve yeni eklenen sıralamaları farklı sıralar üretir', () => {
  it('sıralama seçimi istekte ve adreste görünür', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue(sayfa([arac]))

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.change(screen.getByLabelText('Sıralama'), { target: { value: 'recently-added' } })

    await waitFor(() =>
      expect(vi.mocked(fetchVehicles).mock.calls.at(-1)?.[2]).toBe('recently-added'),
    )
    expect(window.location.search).toContain('sort=recently-added')
  })
})

describe('0012b/AC-50: Arşivlenen araç panel listesinin varsayılan görünümünde yer almaz', () => {
  it('arşiv filtresi KAPALI başlıyor ve sunucuya öyle gidiyor', async () => {
    ekran()

    await screen.findByTestId('vehicle-table')

    // D19 — varsayılan KAPALI: arşiv bu projenin "silme"si ve varsayılan listede kalırsa
    // liste sonsuz büyür, arşivleme eylemi de hiçbir şey yapmamış görünür.
    expect(vi.mocked(fetchVehicles).mock.calls[0][4]).toBe(false)
    expect(screen.getByLabelText('Arşivi göster')).toHaveProperty('checked', false)
  })
})

describe('0012b/AC-51: Açık bir filtreyle arşivlenen araçlar görülebilir', () => {
  it('kutu işaretlenince istek arşivi de istiyor ve etiket değişiyor', async () => {
    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByLabelText('Arşivi göster'))

    await waitFor(() => expect(vi.mocked(fetchVehicles).mock.calls.at(-1)?.[4]).toBe(true))

    expect(screen.getByLabelText('Arşivi gizle')).toHaveProperty('checked', true)
  })

  it('0012b/AC-52: ÖZET sayacı da aynı bayrağı alıyor — liste ile aynı evren', async () => {
    ekran()

    await screen.findByTestId('vehicle-table')

    expect(vi.mocked(fetchVehicleSummary).mock.calls[0][2]).toBe(false)

    fireEvent.click(screen.getByLabelText('Arşivi göster'))

    await waitFor(() => expect(vi.mocked(fetchVehicleSummary).mock.calls.at(-1)?.[2]).toBe(true))
  })
})

describe('0012b/AC-53: Lokasyon ataması bekleyen araç panel listesinde ayırt edilir', () => {
  it('bayraklı araç kendi rozetini taşıyor ve "tam" sayılmıyor', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue({
      items: [{ ...arac, awaitingBranchAssignment: true }],
      page: 1,
      pageSize: 25,
      totalCount: 1,
      totalPages: 1,
    })

    ekran()

    expect((await screen.findByTestId('vehicle-awaiting-branch')).textContent).toContain(
      'Şube bekliyor',
    )
    // Sütun BAŞLIĞI "Tamlık" ve o da "Tam" içeriyor; iddia SATIRA daraltılıyor.
    const satir = screen.getByTestId('vehicle-table').querySelector('tbody')

    expect(satir?.textContent).not.toContain('Tam')
  })
})

/**
 * `0012c` — liste ekranının toplu eylem ve öne çıkarma yüzeyi.
 *
 * <p>Seçimin **sayfaya ait** olması (**D43**) ve öne çıkarmanın **yalnız burada**
 * yaşaması (**D51**) bu ekranın iki kararı; ikisi de bileşenin dışından görünmüyor ve
 * ancak ekranın kendisinden ölçülebiliyor.</p>
 */
describe('0012c: toplu eylem ve öne çıkarma yüzeyi', () => {
  const ikinci = { ...arac, vehicleId: '0192c000-0000-7000-8000-000000000002' }

  function ikiArac(overrides: Partial<VehicleAdminListItem> = {}) {
    vi.mocked(fetchVehicles).mockResolvedValue({
      items: [{ ...arac, ...overrides }, ikinci],
      page: 1,
      pageSize: 25,
      totalCount: 2,
      totalPages: 1,
    })
  }

  it('0012c/AC-1: seçilen araçlar toplu eyleme GÖNDERİLİR ve sıra sayfa sırasıdır', async () => {
    ikiArac()
    vi.mocked(applyBulkListingAction).mockResolvedValue({ results: [] })

    ekran()

    await screen.findByTestId('vehicle-table')

    // Tıklama sırası TERS: gönderilen sıra yine sayfa sırası olmalı (**D47**).
    fireEvent.click(screen.getByTestId(`vehicle-select-${ikinci.vehicleId}`))
    fireEvent.click(screen.getByTestId(`vehicle-select-${arac.vehicleId}`))
    fireEvent.click(screen.getByTestId('vehicle-bulk-apply'))

    await waitFor(() =>
      expect(applyBulkListingAction).toHaveBeenCalledWith(
        'publish',
        [arac.vehicleId, ikinci.vehicleId],
        'tr',
      ),
    )
  })

  it('0012c/AC-36: seçim SAYFAYA aittir; sayfa değişince temizlenir', async () => {
    vi.mocked(fetchVehicles).mockResolvedValue({
      items: [arac, ikinci],
      page: 1,
      pageSize: 25,
      totalCount: 50,
      totalPages: 2,
    })

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByTestId(`vehicle-select-${arac.vehicleId}`))

    expect(screen.getByTestId('vehicle-bulk-selection').textContent).toContain('1 araç seçildi')

    fireEvent.click(screen.getByText('Sonraki'))

    // Sayfalar arası biriken bir seçim, personelin GÖRMEDİĞİ araçlara eylem uygulardı ve
    // üst sınır (**D4**) o anda anlamını yitirirdi.
    await waitFor(() =>
      expect(screen.getByTestId('vehicle-bulk-selection').textContent).toContain(
        'Önce listeden araç seçin',
      ),
    )
  })

  it('0012c/AC-36: arşiv filtresi değişince de seçim temizlenir', async () => {
    ikiArac()

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByTestId('vehicle-select-all'))

    expect(screen.getByTestId('vehicle-bulk-selection').textContent).toContain('2 araç seçildi')

    // Arşiv filtresi listelenen EVRENİ değiştiriyor: sayfa numarası aynı kalsa bile
    // ekrandaki araçlar başka bir kümedir.
    fireEvent.click(screen.getByLabelText('Arşivi göster'))

    await waitFor(() =>
      expect(screen.getByTestId('vehicle-bulk-selection').textContent).toContain(
        'Önce listeden araç seçin',
      ),
    )
  })

  it('0012c/AC-32: öne çıkarılmış araç satırda rozetle ayırt edilir', async () => {
    ikiArac({ isFeatured: true })

    ekran()

    await screen.findByTestId('vehicle-table')

    const rozetler = screen.getAllByTestId('vehicle-featured')

    expect(rozetler).toHaveLength(1)
    expect(rozetler[0]?.textContent).toContain('Öne çıkarıldı')
  })

  it('0012c/AC-33: öne çıkarma satır başına da uygulanabilir ve durumun TERSİNİ gönderir', async () => {
    ikiArac({ isFeatured: true })
    vi.mocked(applyListingFeature).mockResolvedValue(undefined)

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByTestId(`vehicle-feature-${arac.vehicleId}`))

    await waitFor(() =>
      expect(applyListingFeature).toHaveBeenCalledWith(arac.vehicleId, false, 'tr'),
    )

    // Eylem listeyi TAZELİYOR: ikinci düğme yeni render'dan bekleniyor. `getBy…` ile
    // okumak o anki DOM'u okur ve tazeleme ortasında kırılgan bir test üretir.
    const ikinciDugme = await screen.findByTestId(`vehicle-feature-${ikinci.vehicleId}`)

    // Öne çıkarılmamış satırda düğme TERS yönü gönderiyor.
    fireEvent.click(ikinciDugme)

    await waitFor(() =>
      expect(applyListingFeature).toHaveBeenCalledWith(ikinci.vehicleId, true, 'tr'),
    )
  })

  it('0012c/AC-43: arşivlenmiş satırda öne çıkarma düğmesi yoktur', async () => {
    ikiArac({ listingStatus: 'Archived', isFeatured: true })

    ekran()

    await screen.findByTestId('vehicle-table')

    // Düğme YOK: sunucu arşivlenmiş ilanda iki yönü de reddediyor (**D36**) ve personele
    // olmayan bir düğme gösterilmez.
    expect(screen.queryByTestId(`vehicle-feature-${arac.vehicleId}`)).toBeNull()

    // Rozet DURUYOR: bir olguyu bildiriyor ve arşivlenen ilan öne çıkarılmış hâlde
    // kalabilir — düğmenin yokluğu rozeti de silmemeli.
    expect(screen.getByTestId('vehicle-featured').textContent).toContain('Öne çıkarıldı')

    // Arşivlenmemiş komşu satırda düğme yerinde.
    expect(screen.getByTestId(`vehicle-feature-${ikinci.vehicleId}`)).toBeTruthy()
  })

  it('0012c/AC-44: satır başına öne çıkarmanın reddi O SATIRDA gerekçesiyle görünür', async () => {
    ikiArac()
    vi.mocked(applyListingFeature).mockRejectedValue(new ApiError('concurrency.lost', null, 409))

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByTestId(`vehicle-feature-${arac.vehicleId}`))

    const kutu = await screen.findByTestId(`vehicle-feature-error-${arac.vehicleId}`)

    expect(kutu.textContent).toContain('başka bir değişiklik oldu')
    expect(kutu.getAttribute('role')).toBe('alert')

    // Gerekçe EYLEMİN YAŞADIĞI satırda: komşu satır temiz kalıyor.
    expect(screen.queryByTestId(`vehicle-feature-error-${ikinci.vehicleId}`)).toBeNull()

    // AC-39 — hata kodu ve HTTP durumu ekranda görünmez.
    expect(document.body.textContent).not.toContain('concurrency.lost')
    expect(document.body.textContent).not.toContain('409')
  })

  it('0012c/AC-44: ağ hatası da sessiz kalmıyor — genel mesaja düşüyor', async () => {
    ikiArac()
    // `ApiError` DEĞİL: `fetch` ağ seviyesinde düştüğünde kod yoktur ve ilk yazımda bu yol
    // da yutuluyordu. Yakalanmamış bir promise reddi kalıyor, ekranda hiçbir şey olmuyordu.
    vi.mocked(applyListingFeature).mockRejectedValue(new Error('ağ yok'))

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByTestId(`vehicle-feature-${arac.vehicleId}`))

    const kutu = await screen.findByTestId(`vehicle-feature-error-${arac.vehicleId}`)

    expect(kutu.textContent).toContain('tamamlanamadı')
  })

  it('0012c/AC-44: başarılı eylemden sonra önceki red mesajı kalmıyor', async () => {
    ikiArac()
    vi.mocked(applyListingFeature).mockRejectedValueOnce(
      new ApiError('vehicle.not_found', null, 404),
    )

    ekran()

    await screen.findByTestId('vehicle-table')

    fireEvent.click(screen.getByTestId(`vehicle-feature-${arac.vehicleId}`))
    await screen.findByTestId(`vehicle-feature-error-${arac.vehicleId}`)

    vi.mocked(applyListingFeature).mockResolvedValue(undefined)
    fireEvent.click(screen.getByTestId(`vehicle-feature-${arac.vehicleId}`))

    await waitFor(() =>
      expect(screen.queryByTestId(`vehicle-feature-error-${arac.vehicleId}`)).toBeNull(),
    )
  })
})
