import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { ShowcaseListing } from '../../../shared/api/types'
import type { AppLocale } from '../../../shared/lib/locale'
import { HomeCatalog } from './HomeCatalog'
import { NarrowingForm } from './NarrowingForm'

/**
 * 0014 — ana sayfanın Catalog bölümleri: öne çıkanlar, daraltma formu ve durumları. Gerçek
 * API client'ı sahtelenmiş hâlde monte edilir (docs/frontend.md § 8.1). Sıra, üst sınır ve
 * görünürlük sunucudadır ve `OneCikanIlanSorgusuTests`'te ölçülür; burada ölçülen,
 * sunucunun verdiğinin DOĞRU GÖSTERİLMESİ.
 */
vi.mock('../../../shared/api/catalog', () => ({
  fetchFeaturedListings: vi.fn(),
  fetchShowcaseTotal: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
  // `0021` — marka okuması; varsayılan: gösterilecek marka yok.
  fetchHomeBrands: vi.fn().mockResolvedValue([]),
}))

const { fetchFeaturedListings, fetchShowcaseTotal, fetchBodyTypes, fetchFieldSettings } =
  await import('../../../shared/api/catalog')

function ilan(no: number, degis: Partial<ShowcaseListing> = {}): ShowcaseListing {
  return {
    listingId: `0192c000-0000-7000-8000-00000000000${no}`,
    slug: `toyota-corolla-2019-${no}`,
    isReserved: false,
    coverImageUrl: `/seed-media/cover-${no}.svg`,
    modelName: `Model ${no}`,
    modelYear: 2019,
    mileageKilometers: 125000,
    price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
    branchName: 'Merkez Şube',
    ...degis,
  }
}

const KASA_TIPLERI = [
  { slug: 'sedan', label: 'Sedan' },
  { slug: 'suv', label: 'SUV' },
  { slug: 'hatchback', label: 'Hatchback' },
]

function filtrelenebilir(acik: boolean) {
  vi.mocked(fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: acik }],
  })
}

function ekran(locale: AppLocale = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <HomeCatalog />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  filtrelenebilir(true)
  vi.mocked(fetchBodyTypes).mockResolvedValue(KASA_TIPLERI)
  vi.mocked(fetchShowcaseTotal).mockResolvedValue(128)
  vi.mocked(fetchFeaturedListings).mockResolvedValue([ilan(1), ilan(2)])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

const sunucuHatasi = () => new ApiError('api.unavailable', 'trace-xyz-789', 500)

describe('0014 — öne çıkan araçlar (R3)', () => {
  it('0014/AC-17: 8 öne çıkarılmış görünür ilan varken bölüm tam altı kart gösterir (uç yarısı: sunucunun altısı olduğu gibi basılır)', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([1, 2, 3, 4, 5, 6].map((no) => ilan(no)))

    ekran()

    const bolum = await screen.findByTestId('featured-section')
    expect(within(bolum).getAllByTestId('listing-card')).toHaveLength(6)
    // frontend-003 AC-21 — her kartta favori düğmesi (tablodaki satır: yalnız varlığı eklenir).
    expect(within(bolum).getAllByRole('button', { name: /^Favori: / })).toHaveLength(6)
  })

  it('0014/AC-18: kartlar sunucunun verdiği sırada basılır (uç sırayı değiştirmez)', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([ilan(3), ilan(1), ilan(2)])

    ekran()

    const bolum = await screen.findByTestId('featured-section')
    expect(
      within(bolum)
        .getAllByRole('link')
        .map((link) => link.textContent),
    ).toEqual(['Model 3', 'Model 1', 'Model 2'])
  })

  it('0014/AC-21: Hiç öne çıkarılmış görünür ilan yokken bölüm başlığıyla birlikte hiç görünmez; yerinde boş durum veya hata durumu da yoktur', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([])

    ekran()

    await screen.findByTestId('narrowing-form')
    await waitFor(() => expect(screen.queryByTestId('featured-skeleton')).toBeNull())
    expect(screen.queryByTestId('featured-section')).toBeNull()
    expect(screen.queryByText('Öne çıkan araçlar')).toBeNull()
    expect(screen.queryByTestId('featured-error')).toBeNull()
    expect(screen.queryByTestId('empty-showcase')).toBeNull()
  })

  it('0014/AC-22: Öne çıkarılmış bir Rezerve ilan bölümde Rezerve rozetiyle görünür', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([ilan(1, { isReserved: true })])

    ekran()

    const bolum = await screen.findByTestId('featured-section')
    expect(within(bolum).getByText('Rezerve')).toBeTruthy()
  })

  it('0014/AC-24: "Fiyat sorunuz" ilanın kartı fiyat yerine "fiyat sorunuz" metnini gösterir; 0 göstermez', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([
      ilan(1, {
        price: { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null },
      }),
    ])

    ekran()

    const kart = await screen.findByTestId('listing-card')
    expect(within(kart).getByText('Fiyat sorunuz')).toBeTruthy()
    expect(kart.textContent).not.toMatch(/₺\s?0|0,00/)
  })

  it('0014/AC-25: Para birimi KWD olan kurulumda 12345.500 tutarlı ilanın kartı tutarı üç ondalıkla gösterir', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([
      ilan(1, {
        price: { onRequest: false, amount: 12345.5, currencyCode: 'KWD', minorUnitExponent: 3 },
      }),
    ])

    ekran('en')

    const kart = await screen.findByTestId('listing-card')
    expect(kart.textContent).toContain('12,345.500')
  })

  it('0014/AC-26: Karta tıklamak o ilanın detayına (/{locale}/cars/{slug}) gider', async () => {
    ekran('ar')

    const bolum = await screen.findByTestId('featured-section')
    expect(within(bolum).getByRole('link', { name: 'Model 1' }).getAttribute('href')).toBe(
      '/ar/cars/toyota-corolla-2019-1',
    )
  })

  it('0014/AC-27: İlk kartın kapak görseli loading="lazy" taşımaz; diğer kartlarınki taşır', async () => {
    vi.mocked(fetchFeaturedListings).mockResolvedValue([ilan(1), ilan(2), ilan(3)])

    ekran()

    const bolum = await screen.findByTestId('featured-section')
    const gorseller = within(bolum).getAllByRole('img')
    expect(gorseller[0]?.getAttribute('loading')).not.toBe('lazy')
    expect(gorseller.slice(1).map((img) => img.getAttribute('loading'))).toEqual(['lazy', 'lazy'])
  })

  it('0014/AC-28: Her kartın kapak görseli açık width ve height taşır', async () => {
    ekran()

    const bolum = await screen.findByTestId('featured-section')
    for (const img of within(bolum).getAllByRole('img')) {
      expect(Number(img.getAttribute('width'))).toBeGreaterThan(0)
      expect(Number(img.getAttribute('height'))).toBeGreaterThan(0)
    }
  })
})

describe('0014 — daraltma formu (R5, R6)', () => {
  it("0014/AC-37: Form, liste ekranının kasa tipi filtre çubuğuyla aynı kasa tiplerini, aynı sırada, etkin locale'in adıyla listeler", async () => {
    ekran('ar')

    const eksen = await screen.findByTestId('narrowing-form-body-types')
    expect(
      within(eksen)
        .getAllByRole('checkbox')
        .map((kutu) => kutu.closest('label')?.textContent),
    ).toEqual(['Sedan', 'SUV', 'Hatchback'])
    // Aynı kaynak, aynı dil: liste çubuğunun okuduğu çağrılar bu locale ile yapıldı.
    expect(vi.mocked(fetchBodyTypes)).toHaveBeenCalledWith('ar', expect.anything())
  })

  it('0014/AC-38: sedan ve suv seçilip gönderilince ziyaretçi /{locale}/cars?bodyType=sedan&bodyType=suv adresine gider', () => {
    const git = vi.fn()
    render(
      <LocaleProvider locale="tr">
        <NarrowingForm
          total={128}
          options={{ filterable: true, bodyTypes: KASA_TIPLERI }}
          onNavigate={git}
        />
      </LocaleProvider>,
    )

    // Tıklama sırası ters: adres çubuğun sırasını izler, tıklamanın değil.
    fireEvent.click(screen.getByRole('checkbox', { name: 'SUV' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Sedan' }))
    fireEvent.click(screen.getByRole('button', { name: '128 aracı gör' }))

    expect(git).toHaveBeenCalledWith('/tr/cars?bodyType=sedan&bodyType=suv')
  })

  it("0014/AC-39: Hiçbir şey seçilmeden gönderilince ziyaretçi parametresiz /{locale}/cars'a gider", () => {
    const git = vi.fn()
    render(
      <LocaleProvider locale="en">
        <NarrowingForm
          total={5}
          options={{ filterable: true, bodyTypes: KASA_TIPLERI }}
          onNavigate={git}
        />
      </LocaleProvider>,
    )

    fireEvent.click(screen.getByRole('button', { name: 'See 5 cars' }))

    expect(git).toHaveBeenCalledWith('/en/cars')
  })

  it('0014/AC-40: Gönderme düğmesi görünür ilanların toplam sayısını ICU ile gösterir: 128 görünür ilanla Türkçe sayfada "128 aracı gör"', async () => {
    ekran()

    expect(await screen.findByRole('button', { name: '128 aracı gör' })).toBeTruthy()
  })

  it('0014/AC-41: Düğmedeki sayı kasa tipi seçimiyle değişmez', async () => {
    ekran()

    await screen.findByRole('button', { name: '128 aracı gör' })
    fireEvent.click(screen.getByRole('checkbox', { name: 'SUV' }))
    fireEvent.click(screen.getByRole('checkbox', { name: 'Sedan' }))

    expect(screen.getByRole('button', { name: '128 aracı gör' })).toBeTruthy()
    expect(vi.mocked(fetchShowcaseTotal)).toHaveBeenCalledTimes(1)
  })

  it('0014/AC-74: Kasa tipi alanı filtrelenemez iken form kasa tipi seçeneği göstermez; "N aracı gör" düğmesi görünür ve parametresiz /{locale}/cars\'a gider', async () => {
    filtrelenebilir(false)

    ekran()

    const dugme = await screen.findByRole('button', { name: '128 aracı gör' })
    expect(screen.queryByTestId('narrowing-form-body-types')).toBeNull()
    expect(screen.queryAllByRole('checkbox')).toHaveLength(0)

    const git = vi.fn()
    cleanup()
    render(
      <LocaleProvider locale="tr">
        <NarrowingForm
          total={128}
          options={{ filterable: false, bodyTypes: KASA_TIPLERI }}
          onNavigate={git}
        />
      </LocaleProvider>,
    )
    fireEvent.click(screen.getByRole('button', { name: '128 aracı gör' }))
    expect(git).toHaveBeenCalledWith('/tr/cars')
    expect(dugme).toBeTruthy()
  })
})

describe('0014 — durumlar (R8)', () => {
  it('0014/AC-49: Veri gelene kadar öne çıkanlar ve form kendi iskeletini gösterir', () => {
    vi.mocked(fetchFeaturedListings).mockReturnValue(new Promise(() => {}))
    vi.mocked(fetchShowcaseTotal).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('featured-skeleton')).toBeTruthy()
    expect(screen.getByTestId('narrowing-form-skeleton')).toBeTruthy()
  })

  it('0014/AC-50: Görünür ilanların toplam sayısı 0 iken öne çıkanlar ve form yerine tek bir boş durum görünür ve "0 aracı gör" hiçbir yerde yazmaz', async () => {
    vi.mocked(fetchShowcaseTotal).mockResolvedValue(0)
    // Öne çıkan okuması bir şey dönse bile karar TOPLAMDAN verilir (D12).
    vi.mocked(fetchFeaturedListings).mockResolvedValue([ilan(1)])

    const { container } = ekran()

    expect(await screen.findAllByTestId('empty-showcase')).toHaveLength(1)
    expect(screen.queryByTestId('narrowing-form')).toBeNull()
    expect(screen.queryByTestId('featured-section')).toBeNull()
    expect(container.textContent).not.toContain('0 aracı gör')
  })

  it('0014/AC-62: Toplam 1 görünür ilan varken ve hiç öne çıkarılmış ilan yokken boş durum görünmez; form "1 aracı gör" ile görünür, öne çıkan bölümü görünmez', async () => {
    vi.mocked(fetchShowcaseTotal).mockResolvedValue(1)
    vi.mocked(fetchFeaturedListings).mockResolvedValue([])

    ekran()

    expect(await screen.findByRole('button', { name: '1 aracı gör' })).toBeTruthy()
    await waitFor(() => expect(screen.queryByTestId('featured-skeleton')).toBeNull())
    expect(screen.queryByTestId('featured-section')).toBeNull()
    expect(screen.queryByTestId('empty-showcase')).toBeNull()
  })

  it('0014/AC-52: Toplam sayı okuması başarısız, öne çıkan okuması başarılıyken öne çıkanlar görünür; form kendi hata durumunu ve tekrar dene eylemini gösterir ve boş durum gösterilmez', async () => {
    vi.mocked(fetchShowcaseTotal).mockRejectedValue(sunucuHatasi())

    ekran()

    const hata = await screen.findByTestId('narrowing-form-error')
    expect(within(hata).getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()
    expect(await screen.findByTestId('featured-section')).toBeTruthy()
    expect(screen.queryByTestId('empty-showcase')).toBeNull()
  })

  it('0014/AC-63: Kasa tipi okuması başarısız, toplam sayı ve öne çıkan okuması başarılıyken öne çıkanlar görünür; form kendi hata durumunu ve tekrar dene eylemini gösterir', async () => {
    vi.mocked(fetchBodyTypes).mockRejectedValue(sunucuHatasi())

    ekran()

    const hata = await screen.findByTestId('narrowing-form-error')
    expect(within(hata).getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()
    expect(await screen.findByTestId('featured-section')).toBeTruthy()
  })

  it('0014/AC-53: Öne çıkan okuması başarısız, formun okumaları başarılıyken form görünür; öne çıkanlar bölümü kendi hata durumunu ve tekrar dene eylemini gösterir', async () => {
    vi.mocked(fetchFeaturedListings).mockRejectedValue(sunucuHatasi())

    ekran()

    const hata = await screen.findByTestId('featured-error')
    expect(within(hata).getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()
    expect(await screen.findByTestId('narrowing-form')).toBeTruthy()
  })

  it('0014/AC-55: Öne çıkanların tekrar dene eylemi yalnız o bölümün verisini yeniden ister', async () => {
    vi.mocked(fetchFeaturedListings).mockRejectedValueOnce(sunucuHatasi())

    ekran()

    const hata = await screen.findByTestId('featured-error')
    await screen.findByTestId('narrowing-form')
    fireEvent.click(within(hata).getByRole('button', { name: 'Tekrar dene' }))

    expect(await screen.findByTestId('featured-section')).toBeTruthy()
    expect(vi.mocked(fetchFeaturedListings)).toHaveBeenCalledTimes(2)
    expect(vi.mocked(fetchShowcaseTotal)).toHaveBeenCalledTimes(1)
    expect(vi.mocked(fetchBodyTypes)).toHaveBeenCalledTimes(1)
  })

  it('0014/AC-64: Formun tekrar dene eylemi toplam sayıyı ve kasa tiplerini yeniden ister, öne çıkan okumasını istemez', async () => {
    vi.mocked(fetchShowcaseTotal).mockRejectedValueOnce(sunucuHatasi())

    ekran()

    const hata = await screen.findByTestId('narrowing-form-error')
    await screen.findByTestId('featured-section')
    fireEvent.click(within(hata).getByRole('button', { name: 'Tekrar dene' }))

    expect(await screen.findByRole('button', { name: '128 aracı gör' })).toBeTruthy()
    expect(vi.mocked(fetchShowcaseTotal)).toHaveBeenCalledTimes(2)
    expect(vi.mocked(fetchBodyTypes)).toHaveBeenCalledTimes(2)
    expect(vi.mocked(fetchFieldSettings)).toHaveBeenCalledTimes(2)
    expect(vi.mocked(fetchFeaturedListings)).toHaveBeenCalledTimes(1)
  })

  it('0014/AC-56: Sunucu 500 döndüğünde hiçbir bölüm teknik ayrıntı (durum kodu, iz kimliği, istisna metni) göstermez', async () => {
    vi.mocked(fetchShowcaseTotal).mockRejectedValue(sunucuHatasi())
    vi.mocked(fetchFeaturedListings).mockRejectedValue(sunucuHatasi())

    const { container } = ekran()

    await screen.findByTestId('narrowing-form-error')
    await screen.findByTestId('featured-error')
    for (const isaret of ['500', 'trace-xyz-789', 'api.unavailable', '/api/']) {
      expect(container.textContent).not.toContain(isaret)
    }
  })
})
