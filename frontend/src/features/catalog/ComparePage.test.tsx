import { readFileSync } from 'node:fs'
import { StrictMode } from 'react'
import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import { FAVORITES_KEY, resetFavoritesForTests } from '../../shared/lib/favorites'
import { ComparePage } from './favoritePages'

/**
 * `frontend-003` **D3** · **D11** · **D12** — karşılaştırma ekranı; sahte detay okuması sunucunun
 * gerçek biçiminde. Adres `window.history` ile kurulur (jsdom). Gerçek tarayıcı yarısı (360/320 px,
 * ar yerleşimi, panelden alan kapatma) `e2e/favoriler.spec.ts`'te.
 */
vi.mock('../../shared/api/catalog', () => ({ fetchListingDetail: vi.fn() }))

const { fetchListingDetail } = await import('../../shared/api/catalog')

const A = 'audi-a3-2020-aaaaaa'
const B = 'bmw-320i-2021-bbbbbb'
const C = 'citroen-c3-2022-cccccc'

const AUDI: ShowcaseListingDetail = {
  slug: A,
  isReserved: true,
  images: [
    {
      thumbnail: { url: '/m/a-400.webp', width: 400, height: 300 },
      display: { url: '/m/a-800.webp', width: 800, height: 600 },
      large: { url: '/m/a-1200.webp', width: 1200, height: 900 },
    },
  ],
  modelName: 'Audi A3',
  modelYear: 2020,
  mileageKilometers: 45000,
  price: { onRequest: false, amount: 1250000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Kadıköy',
  attributes: [
    { key: 'body_type', label: 'Hatchback', value: 'Hatchback' },
    { key: 'fuel_type', label: 'Benzin', value: 'Benzin' },
  ],
  paintChangeMap: [{ part: 'hood', state: 'painted' }] as never,
}

const BMW: ShowcaseListingDetail = {
  slug: B,
  isReserved: false,
  images: [],
  modelName: 'BMW 320i',
  modelYear: 2021,
  mileageKilometers: 30000,
  price: { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null } as never,
  branchName: 'Merkez',
  attributes: [
    { key: 'fuel_type', label: 'Dizel', value: 'Dizel' },
    { key: 'color', label: 'Beyaz', value: 'Beyaz' },
  ],
}

function adres(search: string) {
  window.history.replaceState(null, '', `/tr/compare${search}`)
}

function ekran() {
  return render(
    <LocaleProvider locale="tr">
      <ComparePage />
    </LocaleProvider>,
  )
}

const tablo = () => screen.findByRole('table')

function satir(baslik: string) {
  return screen.getByRole('rowheader', { name: baslik }).closest('tr')!
}

const hucreler = (tr: HTMLElement) => [...tr.querySelectorAll('td')].map((td) => td.textContent)

beforeEach(() => {
  localStorage.clear()
  resetFavoritesForTests()
  vi.mocked(fetchListingDetail).mockImplementation((slug) =>
    Promise.resolve(slug === A ? AUDI : slug === B ? BMW : null),
  )
})

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.clearAllMocks()
})

describe('karşılaştırma ekranı', () => {
  it('frontend-003/AC-8 (birim): adres geçmişe kayıt eklemeden okunan kümeye düzeltilir; utm korunur', async () => {
    adres(`?listing=${B}&listing=Abc-2020-cccccc&listing=${A}&listing=${B}&utm_source=z`)
    const uzunluk = window.history.length
    ekran()

    await tablo()
    expect(window.location.search).toBe(`?listing=${B}&listing=${A}&utm_source=z`)
    expect(window.history.length).toBe(uzunluk)
  })

  it('frontend-003/AC-9: ikiden az geçerli değer — "en az iki araç" durumu ve favorilere götüren tek eylem', () => {
    for (const search of ['', `?listing=${A}`, '?listing=%%%']) {
      adres(search)
      ekran()
      const durum = screen.getByTestId('compare-too-few')
      expect(within(durum).getByRole('heading').textContent).toBe(
        'Karşılaştırmak için en az iki araç seçin',
      )
      expect(within(durum).getByRole('link').getAttribute('href')).toBe('/tr/favorites')
      cleanup()
    }
    expect(vi.mocked(fetchListingDetail)).not.toHaveBeenCalled()
  })

  it('frontend-003/AC-50, AC-61: Sütun başı görsel, detaya giden model adı, rezerve rozeti ve favori düğmesi; favorisiz tarayıcıda aynı sıra; başlıklar `scope="col"`/`scope="row"`', async () => {
    adres(`?listing=${B}&listing=${A}`)
    ekran()

    await tablo()
    const sutunlar = screen.getAllByRole('columnheader')
    expect(sutunlar).toHaveLength(2)
    expect(within(sutunlar[0]!).getByRole('link', { name: 'BMW 320i' }).getAttribute('href')).toBe(
      `/tr/cars/${B}`,
    )
    const audi = sutunlar[1]!
    expect(within(audi).getByRole('img').getAttribute('alt')).toBe('Audi A3 2020 — görsel 1/1')
    expect(within(audi).getByText('Rezerve')).toBeTruthy()
    expect(within(audi).getByRole('button', { name: 'Favori: Audi A3 2020' })).toBeTruthy()
    expect(sutunlar.every((th) => th.getAttribute('scope') === 'col')).toBe(true)
    expect(screen.getAllByRole('rowheader').every((th) => th.getAttribute('scope') === 'row')).toBe(
      true,
    )
  })

  it('frontend-003/AC-51, AC-52, AC-56: Satırlar fiyat, yıl, km, şube, nitelikler; değerler detayın biçimi ("Fiyat sorunuz"); olmayan nitelik "Belirtilmemiş"; başlık değer değil', async () => {
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    expect(screen.getAllByRole('rowheader').map((th) => th.textContent)).toEqual([
      'Fiyat',
      'Model yılı',
      'Kilometre',
      'Şube',
      'Kasa tipi',
      'Yakıt',
      'Renk',
    ])
    expect(hucreler(satir('Fiyat'))).toEqual(['₺1.250.000,00', 'Fiyat sorunuz'])
    expect(hucreler(satir('Model yılı'))).toEqual(['2020', '2021'])
    expect(hucreler(satir('Kilometre'))).toEqual(['45.000 km', '30.000 km'])
    expect(hucreler(satir('Kasa tipi'))).toEqual(['Hatchback', 'Belirtilmemiş'])
    expect(hucreler(satir('Renk'))).toEqual(['Belirtilmemiş', 'Beyaz'])
    for (const th of screen.getAllByRole('rowheader')) {
      expect(hucreler(th.closest('tr')!)).not.toContain(th.textContent)
    }
  })

  it('frontend-003/AC-52, AC-54: Hiçbir ilanda olmayan nitelik için satır yok; yanıtta olmayan anahtar basılmaz; kaynakta alan ayarı okuması yok', async () => {
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    expect(screen.queryByRole('rowheader', { name: 'Vites' })).toBeNull()
    expect(screen.queryByRole('rowheader', { name: 'Donanım paketi' })).toBeNull()

    const kaynak = readFileSync('src/features/catalog/ComparePage.tsx', 'utf8')
    expect(kaynak).not.toMatch(/field-?settings|FieldSetting/i)
  })

  it('frontend-003/AC-58: Parça haritası karşılaştırmada yok', async () => {
    adres(`?listing=${A}&listing=${B}`)
    const { container } = ekran()

    await tablo()
    expect(container.querySelector('[data-testid="paint-change-map"]')).toBeNull()
    expect(screen.queryByText(/hood|Kaput/)).toBeNull()
  })

  it('frontend-003/AC-59: 404 dönen sütun "artık vitrinde değil" yazar ve hücre basmaz; hepsi 404 ise ayrı durum ve vitrine götüren eylem', async () => {
    adres(`?listing=${A}&listing=${C}`)
    ekran()

    await tablo()
    expect(
      within(screen.getAllByRole('columnheader')[1]!).getByText('Bu araç artık vitrinde değil.'),
    ).toBeTruthy()
    expect(hucreler(satir('Fiyat'))).toEqual(['₺1.250.000,00', ''])
    cleanup()

    vi.mocked(fetchListingDetail).mockResolvedValue(null)
    adres(`?listing=${A}&listing=${C}`)
    ekran()
    const durum = await screen.findByTestId('compare-all-gone')
    expect(within(durum).getByRole('link').getAttribute('href')).toBe('/tr/cars')
  })

  it('frontend-003/AC-60: 404 dışı hata veren sütun hata ve "tekrar dene" gösterir, yalnız o sütun yeniden okunur; hepsi hata ise hata durumu', async () => {
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      slug === B
        ? Promise.reject(new ApiError('api.unavailable', null, 500))
        : Promise.resolve(AUDI),
    )
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    const sutun = screen.getAllByRole('columnheader')[1]!
    expect(within(sutun).getByText('Bu araç yüklenemedi.')).toBeTruthy()

    vi.mocked(fetchListingDetail).mockClear()
    vi.mocked(fetchListingDetail).mockResolvedValue(BMW)
    fireEvent.click(within(sutun).getByRole('button', { name: 'Tekrar dene' }))

    await waitFor(() => expect(screen.getByRole('link', { name: 'BMW 320i' })).toBeTruthy())
    expect(vi.mocked(fetchListingDetail).mock.calls.map((call) => call[0])).toEqual([B])
    cleanup()

    vi.mocked(fetchListingDetail).mockRejectedValue(new ApiError('api.unavailable', null, 503))
    adres(`?listing=${A}&listing=${B}`)
    ekran()
    expect(await screen.findByTestId('compare-error')).toBeTruthy()
  })

  it('frontend-003/AC-62 (birim), D12: Kayabilen kap `tabindex="0"`, `role="region"` ve erişilebilir ad taşır', async () => {
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    const bolge = screen.getByRole('region', { name: 'Araç karşılaştırma tablosu' })
    expect(bolge.getAttribute('tabindex')).toBe('0')
    expect(within(bolge).getByRole('table')).toBeTruthy()
  })

  it('frontend-003/AC-64: Sütundaki favori düğmesi ekler/çıkarır; adres değişmez', async () => {
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    fireEvent.click(screen.getByRole('button', { name: 'Favori: BMW 320i 2021' }))

    expect(
      JSON.parse(localStorage.getItem(FAVORITES_KEY)!).items.map(
        (item: { slug: string }) => item.slug,
      ),
    ).toEqual([B])
    expect(window.location.search).toBe(`?listing=${A}&listing=${B}`)
  })

  it('frontend-003/AC-16 (birim): Depo kullanılamazken karşılaştırma çalışır, sütunlarda favori düğmesi yoktur', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new DOMException('dolu', 'QuotaExceededError')
    })
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    expect(screen.getAllByRole('columnheader')).toHaveLength(2)
    expect(screen.queryAllByRole('button')).toHaveLength(0)
  })
  it("frontend-003 QA 2 BULGU-1: StrictMode'da (iptale saygılı okuma) karşılaştırma tablosu gelir", async () => {
    vi.mocked(fetchListingDetail).mockImplementation(
      (slug, _locale, signal) =>
        new Promise((resolve, reject) => {
          const zaman = setTimeout(() => resolve(slug === A ? AUDI : BMW), 5)
          signal?.addEventListener('abort', () => {
            clearTimeout(zaman)
            reject(new DOMException('iptal', 'AbortError'))
          })
        }),
    )
    adres(`?listing=${A}&listing=${B}`)
    render(
      <StrictMode>
        <LocaleProvider locale="tr">
          <ComparePage />
        </LocaleProvider>
      </StrictMode>,
    )

    expect(await screen.findByRole('table')).toBeTruthy()
    expect(screen.getAllByRole('columnheader')).toHaveLength(2)
  })

  it('frontend-003 QA 2 BULGU-5: Bir sütunun "tekrar dene"si tabloyu iskelete düşürmez; çözülünce odak o sütunun model bağlantısına', async () => {
    vi.mocked(fetchListingDetail).mockImplementation((slug) =>
      slug === B
        ? Promise.reject(new ApiError('api.unavailable', null, 500))
        : Promise.resolve(AUDI),
    )
    adres(`?listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    const dugme = within(screen.getAllByRole('columnheader')[1]!).getByRole('button', {
      name: 'Tekrar dene',
    })
    dugme.focus()
    let coz: (deger: ShowcaseListingDetail) => void = () => {}
    vi.mocked(fetchListingDetail).mockImplementation(() => new Promise((c) => (coz = c)))
    fireEvent.click(dugme)

    expect(screen.getByRole('table')).toBeTruthy()
    expect(screen.queryByTestId('compare-loading')).toBeNull()
    expect(screen.getByRole('link', { name: 'Audi A3' })).toBeTruthy()
    await act(async () => coz(BMW))

    await waitFor(() => expect(document.activeElement?.getAttribute('href')).toBe(`/tr/cars/${B}`))
  })

  it('frontend-003 D3.4: `listing` değerleri zaten okunan kümeyse adrese dokunulmaz; öbür parametrelerin sırası kalır', async () => {
    adres(`?utm_source=z&listing=${A}&listing=${B}`)
    ekran()

    await tablo()
    expect(window.location.search).toBe(`?utm_source=z&listing=${A}&listing=${B}`)
  })
})
