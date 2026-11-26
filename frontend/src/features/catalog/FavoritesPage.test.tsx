import { StrictMode } from 'react'
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import { FAVORITES_KEY, resetFavoritesForTests } from '../../shared/lib/favorites'
import { FavoritesPage } from './favoritePages'

/**
 * `frontend-003` **D10** · **D6** — favoriler ekranı; gerçek depo, sahte detay okuması. Sahte
 * yanıt sunucunun gerçek biçimindedir (`ShowcaseListingDetail`): 404 `null`'a, öbür hata atışa
 * döner (`fetchListingDetail`). Gerçek tarayıcı yarısı `e2e/favoriler.spec.ts`'te.
 */
vi.mock('../../shared/api/catalog', () => ({ fetchListingDetail: vi.fn() }))

const { fetchListingDetail } = await import('../../shared/api/catalog')

function detay(
  slug: string,
  overrides: Partial<ShowcaseListingDetail> = {},
): ShowcaseListingDetail {
  return {
    slug,
    isReserved: false,
    images: [
      {
        thumbnail: { url: `/m/${slug}-400.webp`, width: 400, height: 300 },
        display: { url: `/m/${slug}-800.webp`, width: 800, height: 600 },
        large: { url: `/m/${slug}-1200.webp`, width: 1200, height: 900 },
      },
    ],
    modelName: 'Toyota Corolla',
    modelYear: 2019,
    mileageKilometers: 125000,
    price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
    branchName: 'Merkez Şube',
    attributes: [{ key: 'fuel_type', label: 'Dizel', value: 'Dizel' }],
    ...overrides,
  }
}

/** Depo kaydı; dizideki sıra eklenme sırasıdır (eski önce). */
function favoriler(...items: [slug: string, model: string, year: number][]) {
  localStorage.setItem(
    FAVORITES_KEY,
    JSON.stringify({
      v: 1,
      items: items.map(([slug, modelName, modelYear], index) => ({
        slug,
        addedAt: `2026-11-2${index}T10:00:00.000Z`,
        modelName,
        modelYear,
      })),
    }),
  )
}

function kayitliSluglar(): string[] {
  return (
    JSON.parse(localStorage.getItem(FAVORITES_KEY) ?? '{"items":[]}') as {
      items: { slug: string }[]
    }
  ).items.map((item) => item.slug)
}

function ekran(locale: 'tr' | 'ar' = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <FavoritesPage />
    </LocaleProvider>,
  )
}

const NOT = 'Favorileriniz bu tarayıcıda saklanır; tarayıcı verisi silinirse kaybolur.'
const A = 'audi-a3-2020-aaaaaa'
const B = 'bmw-320i-2021-bbbbbb'
const C = 'citroen-c3-2022-cccccc'

beforeEach(() => {
  localStorage.clear()
  resetFavoritesForTests()
  vi.mocked(fetchListingDetail).mockImplementation((slug) =>
    Promise.resolve(detay(slug, { modelName: slug.split('-')[0]! })),
  )
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.clearAllMocks()
})

describe('favoriler ekranı', () => {
  it("frontend-003/AC-33, AC-34, AC-35: Son eklenen önce; güncel fiyat, yıl, km, şube, rezerve rozeti; her okuma yalnız kendi slug'ı", async () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021])
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      Promise.resolve(
        slug === A
          ? detay(A, {
              modelName: 'Audi A3',
              modelYear: 2020,
              isReserved: true,
              price: {
                onRequest: false,
                amount: 999000,
                currencyCode: 'TRY',
                minorUnitExponent: 2,
              },
            })
          : detay(B, { modelName: 'BMW 320i', modelYear: 2021 }),
      ),
    )
    ekran()

    const liste = await screen.findByTestId('favorites-list')
    const kartlar = within(liste).getAllByTestId('listing-card')
    expect(kartlar.map((kart) => within(kart).getAllByRole('link')[0]!.textContent)).toEqual([
      'BMW 320i',
      'Audi A3',
    ])
    const audi = kartlar[1]!
    expect(within(audi).getByText('₺999.000,00')).toBeTruthy()
    expect(within(audi).getByText(/2020 · 125\.000 km/)).toBeTruthy()
    expect(within(audi).getByText('Merkez Şube')).toBeTruthy()
    expect(within(audi).getByText('Rezerve')).toBeTruthy()
    expect(within(audi).getByRole('link', { name: 'Audi A3' }).getAttribute('href')).toBe(
      `/tr/cars/${A}`,
    )

    expect(
      vi
        .mocked(fetchListingDetail)
        .mock.calls.map((call) => call[0])
        .sort(),
    ).toEqual([A, B])
  })

  it('frontend-003/AC-36, AC-37: Satılan favori saklanan adla, bağlantısız görünür; açmak silmez; tek tek ve "hepsini kaldır" ile kalkar', async () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021], [C, 'Citroen C3', 2022])
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      Promise.resolve(slug === B ? detay(B) : null),
    )
    ekran()

    const dusenler = await screen.findByTestId('favorites-gone')
    expect(within(dusenler).getByText('Citroen C3 2022')).toBeTruthy()
    expect(within(dusenler).queryAllByRole('link')).toHaveLength(0)
    expect(kayitliSluglar()).toEqual([A, B, C])

    fireEvent.click(
      within(dusenler).getByRole('button', { name: 'Favorilerden kaldır: Audi A3 2020' }),
    )
    expect(kayitliSluglar()).toEqual([B, C])

    fireEvent.click(screen.getByRole('button', { name: 'Hepsini kaldır' }))
    expect(kayitliSluglar()).toEqual([B])
    expect(screen.queryByTestId('favorites-gone')).toBeNull()
  })

  it('frontend-003/AC-39: 404 dışı hata veren favori kendi satırında; düşmüş değil; "tekrar dene" yalnız onu okur', async () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021])
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      slug === A
        ? Promise.reject(new ApiError('api.unavailable', null, 500))
        : Promise.resolve(detay(B)),
    )
    ekran()

    const hatalar = await screen.findByTestId('favorites-failed')
    expect(within(hatalar).getByText('Audi A3 2020 yüklenemedi.')).toBeTruthy()
    expect(screen.queryByTestId('favorites-gone')).toBeNull()

    vi.mocked(fetchListingDetail).mockClear()
    vi.mocked(fetchListingDetail).mockImplementation(() =>
      Promise.resolve(detay(A, { modelName: 'Audi A3' })),
    )
    fireEvent.click(within(hatalar).getByRole('button', { name: 'Tekrar dene' }))

    await waitFor(() => expect(screen.queryByTestId('favorites-failed')).toBeNull())
    expect(vi.mocked(fetchListingDetail).mock.calls.map((call) => call[0])).toEqual([A])
    expect(
      within(screen.getByTestId('favorites-list')).getAllByTestId('listing-card'),
    ).toHaveLength(2)
  })

  it('frontend-003/AC-40, AC-44: Hiç favori yok — boş durum, vitrine götüren tek eylem ve S2 notu', () => {
    ekran()

    const bos = screen.getByTestId('favorites-empty')
    expect(within(bos).getByRole('heading').textContent).toBe('Henüz favoriniz yok')
    expect(within(bos).getByRole('link', { name: 'Araçlara göz atın' }).getAttribute('href')).toBe(
      '/tr/cars',
    )
    expect(screen.getByText(NOT)).toBeTruthy()
    expect(vi.mocked(fetchListingDetail)).not.toHaveBeenCalled()
  })

  it('frontend-003/AC-41, AC-44: Hepsi düşmüş — AC-40\'tan farklı boş durum, "hepsini kaldır" ve S2 notu', async () => {
    favoriler([A, 'Audi A3', 2020])
    vi.mocked(fetchListingDetail).mockResolvedValue(null)
    ekran()

    const bos = await screen.findByTestId('favorites-all-gone')
    expect(within(bos).getByRole('heading').textContent).toBe(
      'Favorilerinizdeki araçların hiçbiri artık vitrinde değil',
    )
    expect(screen.queryByTestId('favorites-empty')).toBeNull()
    expect(screen.getByText(NOT)).toBeTruthy()

    fireEvent.click(screen.getByRole('button', { name: 'Hepsini kaldır' }))
    expect(await screen.findByTestId('favorites-empty')).toBeTruthy()
  })

  it('frontend-003/AC-42, AC-44: Bütün okumalar 404 dışı hata — hata durumu ve "tekrar dene"; teknik metin yok; S2 notu var', async () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021])
    vi.mocked(fetchListingDetail).mockRejectedValue(new ApiError('api.unavailable', 'trace-1', 503))
    ekran()

    const hata = await screen.findByTestId('favorites-error')
    expect(within(hata).getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()
    expect(hata.textContent).not.toMatch(/503|api\.|trace/)
    expect(screen.getByText(NOT)).toBeTruthy()
  })

  it('frontend-003/AC-43 (birim): Okumalar sürerken favori sayısı kadar iskelet kart; S2 notu yükleniyorken de var', () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021])
    vi.mocked(fetchListingDetail).mockReturnValue(new Promise(() => {}))
    ekran()

    expect(screen.getByTestId('favorites-skeleton').children).toHaveLength(2)
    expect(screen.getByText(NOT)).toBeTruthy()
  })

  it('frontend-003/AC-45: Karttaki düğmeyle çıkarılan favori ekrandan kalkar; son favori çıkınca boş durum', async () => {
    favoriler([A, 'Audi A3', 2020])
    ekran()

    fireEvent.click(await screen.findByRole('button', { name: 'Favori: audi 2019' }))

    expect(await screen.findByTestId('favorites-empty')).toBeTruthy()
    expect(kayitliSluglar()).toEqual([])
  })

  it('frontend-003/AC-46, AC-47, AC-48: Yalnız görünür favoride seçim; dördüncü seçilmez ve bölge sınırı söyler; "Karşılaştır" 2–3 seçimde ekran sırasıyla gider', async () => {
    const D = 'dacia-duster-2023-dddddd'
    favoriler(
      [A, 'Audi A3', 2020],
      [B, 'BMW 320i', 2021],
      [C, 'Citroen C3', 2022],
      [D, 'Dacia Duster', 2023],
      ['eski-2010-eeeeee', 'Eski', 2010],
    )
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      Promise.resolve(
        slug.startsWith('eski') ? null : detay(slug, { modelName: slug.split('-')[0]! }),
      ),
    )
    const gidis = vi.fn()
    vi.spyOn(window, 'location', 'get').mockReturnValue({ ...window.location, assign: gidis })
    ekran()

    await screen.findByTestId('favorites-list')
    const secimler = screen.getAllByRole('checkbox')
    expect(secimler).toHaveLength(4)
    expect(within(screen.getByTestId('favorites-gone')).queryAllByRole('checkbox')).toHaveLength(0)

    const karsilastir = screen.getByRole('button', { name: 'Karşılaştır' })
    expect(karsilastir.hasAttribute('disabled')).toBe(true)
    expect(screen.getByText('Karşılaştırmak için iki ya da üç araç seçin.')).toBeTruthy()

    // Ekran sırası: dacia, citroen, bmw, audi (son eklenen önce). Ters sırayla seçilir.
    fireEvent.click(screen.getByRole('checkbox', { name: 'Karşılaştırmaya seç: Audi A3 2020' }))
    expect(karsilastir.hasAttribute('disabled')).toBe(true)
    fireEvent.click(screen.getByRole('checkbox', { name: 'Karşılaştırmaya seç: Citroen C3 2022' }))
    fireEvent.click(
      screen.getByRole('checkbox', { name: 'Karşılaştırmaya seç: Dacia Duster 2023' }),
    )
    const dorduncu = screen.getByRole('checkbox', { name: 'Karşılaştırmaya seç: BMW 320i 2021' })
    fireEvent.click(dorduncu)

    expect((dorduncu as HTMLInputElement).checked).toBe(false)
    expect(
      screen
        .getAllByRole('status')
        .some((bolge) => bolge.textContent === 'En fazla 3 araç karşılaştırılabilir.'),
    ).toBe(true)

    fireEvent.click(karsilastir)
    expect(gidis).toHaveBeenCalledWith(`/tr/compare?listing=${D}&listing=${C}&listing=${A}`)
  })

  it('frontend-003/AC-16 (birim): Depo kullanılamazken "bu tarayıcı favori saklamaya izin vermiyor"; S2 notu yok', () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('dolu', 'QuotaExceededError')
    })
    ekran()

    expect(
      within(screen.getByTestId('favorites-unavailable')).getByRole('heading').textContent,
    ).toBe('Bu tarayıcı favori saklamaya izin vermiyor')
    expect(screen.queryByText(NOT)).toBeNull()
  })
  it("frontend-003 QA 2 BULGU-1: StrictMode'da (çift etki, iptale saygılı okuma) iskelet kalmaz, kartlar gelir", async () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021])
    vi.mocked(fetchListingDetail).mockImplementation(
      (slug, _locale, signal) =>
        new Promise((resolve, reject) => {
          const zaman = setTimeout(
            () => resolve(detay(slug, { modelName: slug.split('-')[0]! })),
            5,
          )
          signal?.addEventListener('abort', () => {
            clearTimeout(zaman)
            reject(new DOMException('iptal', 'AbortError'))
          })
        }),
    )
    render(
      <StrictMode>
        <LocaleProvider locale="tr">
          <FavoritesPage />
        </LocaleProvider>
      </StrictMode>,
    )

    expect(await screen.findByTestId('favorites-list')).toBeTruthy()
    expect(screen.queryByTestId('favorites-skeleton')).toBeNull()
    expect(
      within(screen.getByTestId('favorites-list')).getAllByTestId('listing-card'),
    ).toHaveLength(2)
  })

  it('frontend-003 QA 2 BULGU-5: Bir satırın "tekrar dene"si ekranı iskelete düşürmez; öbür kartlar yerinde; çözülünce odak o kartın bağlantısına, yine hatada düğmede kalır', async () => {
    favoriler([A, 'Audi A3', 2020], [B, 'BMW 320i', 2021])
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      slug === A
        ? Promise.reject(new ApiError('api.unavailable', null, 500))
        : Promise.resolve(detay(B, { modelName: 'BMW 320i' })),
    )
    ekran()

    const hatalar = await screen.findByTestId('favorites-failed')
    const dugme = within(hatalar).getByRole('button', { name: 'Tekrar dene' })
    dugme.focus()

    // 1) Yine hata: satır yerinde, odak aynı düğmede.
    let reddet: (sebep: unknown) => void = () => {}
    vi.mocked(fetchListingDetail).mockImplementation(
      () => new Promise((_coz, red) => (reddet = red)),
    )
    fireEvent.click(dugme)
    expect(screen.queryByTestId('favorites-skeleton')).toBeNull()
    expect(
      within(screen.getByTestId('favorites-list')).getAllByTestId('listing-card'),
    ).toHaveLength(1)
    expect(within(hatalar).getByRole('alert').getAttribute('aria-busy')).toBe('true')
    await act(async () => reddet(new ApiError('api.unavailable', null, 500)))
    expect(document.activeElement).toBe(
      within(screen.getByTestId('favorites-failed')).getByRole('button', { name: 'Tekrar dene' }),
    )

    // 2) Bu kez çözülür: kart gelir, odak onun model bağlantısına taşınır.
    let coz: (deger: ShowcaseListingDetail) => void = () => {}
    vi.mocked(fetchListingDetail).mockImplementation(() => new Promise((c) => (coz = c)))
    fireEvent.click(
      within(screen.getByTestId('favorites-failed')).getByRole('button', { name: 'Tekrar dene' }),
    )
    await act(async () => coz(detay(A, { modelName: 'Audi A3' })))

    await waitFor(() =>
      expect(
        within(screen.getByTestId('favorites-list')).getAllByTestId('listing-card'),
      ).toHaveLength(2),
    )
    expect((document.activeElement as HTMLAnchorElement).getAttribute('href')).toBe(`/tr/cars/${A}`)
  })
  it('frontend-003 QA 2 BULGU-4: Depoda 30 öğe varken ekran en çok 24 okuma yapar', async () => {
    favoriler(
      ...Array.from(
        { length: 30 },
        (_, index) =>
          [`a-2020-${String(index).padStart(6, '0')}`, 'A', 2020] as [string, string, number],
      ),
    )
    ekran()

    await screen.findByTestId('favorites-list')
    expect(vi.mocked(fetchListingDetail).mock.calls.length).toBeLessThanOrEqual(24)
    expect(
      within(screen.getByTestId('favorites-list')).getAllByTestId('listing-card'),
    ).toHaveLength(24)
  })
})
