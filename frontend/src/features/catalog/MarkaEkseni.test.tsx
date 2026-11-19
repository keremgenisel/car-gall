import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { PagedResult, ShowcaseListing } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { HomeCatalog } from './home/HomeCatalog'
import { NarrowingForm } from './home/NarrowingForm'
import { parseListState, toCanonicalSearch } from './listUrlState'
import { ShowcaseListPage } from './ShowcaseListPage'

/**
 * `0021` R11–R14 · D8, D9, D18, D20–D22 — vitrinin marka ekseni (liste) ve ana sayfa formunun
 * marka alanı. Stoklu markaların hesabı ve süzme sunucuda (`MarkaEkseniTests`); burada
 * ölçülen ekranın davranışı ve adresin hâli.
 */
vi.mock('../../shared/api/catalog', () => ({
  fetchShowcaseListings: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
  fetchReferenceTerms: vi.fn(),
  fetchShowcaseBrands: vi.fn(),
  fetchHomeBrands: vi.fn(),
  fetchShowcaseTotal: vi.fn(),
  fetchFeaturedListings: vi.fn(),
}))

const api = await import('../../shared/api/catalog')

const ilan: ShowcaseListing = {
  listingId: '0192c000-0000-7000-8000-000000000001',
  slug: 'toyota-corolla-2019-0192c0',
  isReserved: false,
  coverImageUrl: '/seed-media/cover-0.svg',
  modelName: 'Toyota Corolla',
  modelYear: 2019,
  mileageKilometers: 125000,
  price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
  branchName: 'Merkez Şube',
}

function sayfa(totalCount: number): PagedResult<ShowcaseListing> {
  return { items: totalCount === 0 ? [] : [ilan], page: 1, pageSize: 24, totalCount, totalPages: 1 }
}

const MARKALAR = [
  { slug: 'honda', label: 'Honda' },
  { slug: 'toyota', label: 'Toyota' },
]

function liste(adres = '/tr/cars', locale: AppLocale = 'tr') {
  window.history.replaceState(null, '', adres)
  return render(
    <LocaleProvider locale={locale}>
      <ShowcaseListPage />
    </LocaleProvider>,
  )
}

function anaSayfa(locale: AppLocale = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <HomeCatalog />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  vi.mocked(api.fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: true }],
  })
  vi.mocked(api.fetchBodyTypes).mockResolvedValue([
    { slug: 'sedan', label: 'Sedan' },
    { slug: 'suv', label: 'SUV' },
  ])
  vi.mocked(api.fetchReferenceTerms).mockResolvedValue([])
  vi.mocked(api.fetchShowcaseBrands).mockResolvedValue(MARKALAR)
  vi.mocked(api.fetchHomeBrands).mockResolvedValue(MARKALAR)
  vi.mocked(api.fetchShowcaseListings).mockResolvedValue(sayfa(12))
  vi.mocked(api.fetchShowcaseTotal).mockResolvedValue(128)
  vi.mocked(api.fetchFeaturedListings).mockResolvedValue([])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0021/AC-35: Liste filtresinde marka ekseni yalnız en az bir yayındaki ilanı olan markaları sunar.', () => {
  it('eksen sunucunun döndürdüğü markaları çip olarak gösterir', async () => {
    liste()

    const eksen = await screen.findByTestId('brands-filter')
    expect(
      within(eksen)
        .getAllByRole('checkbox')
        .map((kutu) => kutu.closest('label')?.textContent),
    ).toEqual(['Honda', 'Toyota'])
    expect(api.fetchShowcaseBrands).toHaveBeenCalledWith([], 'tr', expect.anything())
  })
})

describe('0021/AC-36: Toyota seçildiğinde liste yalnız Toyota ilanlarını gösterir ve toplam sayı ona göre değişir.', () => {
  it('Toyota çipi listeyi brand=toyota ile yeniden ister ve yeni toplamı yazar', async () => {
    liste()
    vi.mocked(api.fetchShowcaseListings).mockResolvedValue(sayfa(3))

    fireEvent.click(within(await screen.findByTestId('brands-filter')).getByLabelText('Toyota'))

    await waitFor(() => expect(screen.getByTestId('result-count').textContent).toContain('3'))
    expect(vi.mocked(api.fetchShowcaseListings).mock.lastCall?.[0].brands).toEqual(['toyota'])
  })
})

describe('0021/AC-37: Toyota ve Honda birlikte seçildiğinde liste iki markanın ilanlarını gösterir.', () => {
  it('iki marka tekrarlı parametre olarak gider; eksen yeniden okunmaz', async () => {
    liste('/tr/cars?brand=toyota')

    fireEvent.click(within(await screen.findByTestId('brands-filter')).getByLabelText('Honda'))

    await waitFor(() => expect(window.location.search).toBe('?brand=toyota&brand=honda'))
    expect(vi.mocked(api.fetchShowcaseListings).mock.lastCall?.[0].brands).toEqual([
      'toyota',
      'honda',
    ])
    expect(api.fetchShowcaseBrands).toHaveBeenCalledTimes(1)
  })
})

describe('0021/AC-38: Marka ve kasa tipi birlikte seçildiğinde liste ikisine de uyan ilanları gösterir.', () => {
  it('iki eksen aynı istekte', async () => {
    liste('/tr/cars?bodyType=suv&brand=toyota')

    await screen.findByTestId('brands-filter')
    expect(vi.mocked(api.fetchShowcaseListings).mock.lastCall?.[0]).toMatchObject({
      bodyTypes: ['suv'],
      brands: ['toyota'],
    })
  })
})

describe('0021/AC-39: Seçim adres satırında `brand=toyota` olarak görünür; bu adres yeni bir sekmede açıldığında aynı daraltma gelir.', () => {
  it('adres yazılır ve aynı adres aynı durumu üretir', async () => {
    liste()

    fireEvent.click(within(await screen.findByTestId('brands-filter')).getByLabelText('Toyota'))

    await waitFor(() => expect(window.location.search).toBe('?brand=toyota'))
    expect(parseListState('?brand=toyota').brands).toEqual(['toyota'])
    expect(toCanonicalSearch(parseListState('?brand=toyota&bodyType=suv'))).toBe(
      '?bodyType=suv&brand=toyota',
    )
    cleanup()

    liste('/tr/cars?brand=toyota')
    expect(
      within(await screen.findByTestId('brands-filter')).getByLabelText<HTMLInputElement>('Toyota')
        .checked,
    ).toBe(true)
  })
})

describe('0021/AC-40: Bilinmeyen bir marka değeri (`brand=yok`) ve büyük harfli bir değer (`brand=Toyota`) yok sayılır ve liste hata vermez (D20).', () => {
  it('uç değeri elemez, sunucuya gönderir; liste hata göstermez', async () => {
    liste('/tr/cars?brand=yok&brand=Toyota')

    expect(await screen.findByTestId('result-count')).toBeTruthy()
    expect(vi.mocked(api.fetchShowcaseListings).mock.lastCall?.[0].brands).toEqual([
      'yok',
      'Toyota',
    ])
    expect(screen.queryByRole('alert')).toBeNull()
  })
})

describe('0021/AC-41: Adresle gelen, var olan ama yayında ilanı kalmamış bir marka uygulanır, liste boş sonucu gösterir ve marka eksende işaretli görünür; işareti kaldırınca liste genişler (D21).', () => {
  it('stoksuz seçili marka işaretli görünür; kaldırınca liste daraltmasız okunur', async () => {
    vi.mocked(api.fetchShowcaseBrands).mockResolvedValue([
      { slug: 'fiat', label: 'Fiat' },
      ...MARKALAR,
    ])
    vi.mocked(api.fetchShowcaseListings).mockImplementation((query) =>
      Promise.resolve(sayfa((query.brands ?? []).length > 0 ? 0 : 12)),
    )
    liste('/tr/cars?brand=fiat')

    const fiat = within(
      await screen.findByTestId('brands-filter'),
    ).getByLabelText<HTMLInputElement>('Fiat')
    expect(fiat.checked).toBe(true)
    expect(api.fetchShowcaseBrands).toHaveBeenCalledWith(['fiat'], 'tr', expect.anything())
    expect(await screen.findByTestId('empty-by-filter')).toBeTruthy()

    fireEvent.click(fiat)

    await waitFor(() => expect(screen.getByTestId('result-count').textContent).toContain('12'))
    expect(window.location.search).toBe('')
  })
})

describe('0021/AC-43: Marka ekseninin yüklenme ve hata hâli diğer eksenlerinkiyle aynıdır (D22).', () => {
  it('yüklenirken çok değerli eksenler basılmaz, aralıklar basılır', async () => {
    vi.mocked(api.fetchShowcaseBrands).mockReturnValue(new Promise(() => {}))
    liste()

    expect(await screen.findByTestId('year-summary')).toBeTruthy()
    expect(screen.getByTestId('list-filters').getAttribute('aria-busy')).toBe('true')
    expect(screen.queryByTestId('brands-filter')).toBeNull()
    expect(screen.queryByTestId('body-type-filter')).toBeNull()
  })

  it('okunamazsa marka ekseni diğer çok değerli eksenlerle birlikte gizlenir; teknik ayrıntı yok', async () => {
    vi.mocked(api.fetchShowcaseBrands).mockRejectedValue(
      new ApiError('api.unavailable', 'trace-1', 503),
    )
    liste()

    await waitFor(() =>
      expect(screen.getByTestId('list-filters').getAttribute('aria-busy')).toBe('false'),
    )
    expect(screen.queryByTestId('brands-filter')).toBeNull()
    expect(screen.queryByTestId('fuels-filter')).toBeNull()
    expect(screen.getByTestId('year-summary')).toBeTruthy()
    expect(document.body.textContent).not.toContain('trace-1')
  })
})

describe('0021/AC-44: Arapça listede marka ekseni Arapça görünen adları gösterir ve sağdan sola düzende çalışır.', () => {
  it('eksen adı ve değerleri Arapça; ad yönü kendinden', async () => {
    vi.mocked(api.fetchShowcaseBrands).mockResolvedValue([{ slug: 'toyota', label: 'تويوتا' }])
    liste('/ar/cars', 'ar')

    const eksen = await screen.findByTestId('brands-filter')
    expect(within(eksen).getByText('العلامة التجارية')).toBeTruthy()
    expect(within(eksen).getByText('تويوتا').tagName).toBe('BDI')
    expect(api.fetchShowcaseBrands).toHaveBeenCalledWith([], 'ar', expect.anything())
  })
})

describe('0021/AC-45: "Aracınızı bulun" formu yalnız "ana sayfada göster" işaretli ve en az bir yayındaki ilanı olan markaları sunar.', () => {
  it('form sunucunun ana sayfa markalarını sunar', async () => {
    anaSayfa()

    const alan = await screen.findByTestId('narrowing-form-brands')
    expect(within(alan).getAllByRole('checkbox')).toHaveLength(2)
    expect(api.fetchHomeBrands).toHaveBeenCalledWith('tr', expect.anything())
  })
})

describe('0021/AC-48: Formda Toyota seçilip düğmeye tıklanınca `/{locale}/cars?brand=toyota` açılır; düğmedeki sayı seçimle değişmez (`0014` AC-41, D18).', () => {
  it('adres ve değişmeyen sayı', () => {
    const git = vi.fn()
    render(
      <LocaleProvider locale="tr">
        <NarrowingForm
          total={128}
          options={{ filterable: true, bodyTypes: [{ slug: 'suv', label: 'SUV' }] }}
          brands={MARKALAR}
          onNavigate={git}
        />
      </LocaleProvider>,
    )

    const dugme = screen.getByRole('button', { name: '128 aracı gör' })
    fireEvent.click(screen.getByLabelText('Toyota'))
    expect(dugme.textContent).toBe('128 aracı gör')
    fireEvent.click(dugme)

    expect(git).toHaveBeenCalledWith('/tr/cars?brand=toyota')
  })
})

describe('0021/AC-49: Formda marka ve kasa tipi birlikte seçildiğinde liste ikisiyle açılır.', () => {
  it('iki eksen kanonik sırada', () => {
    const git = vi.fn()
    render(
      <LocaleProvider locale="tr">
        <NarrowingForm
          total={128}
          options={{ filterable: true, bodyTypes: [{ slug: 'suv', label: 'SUV' }] }}
          brands={MARKALAR}
          onNavigate={git}
        />
      </LocaleProvider>,
    )

    fireEvent.click(screen.getByLabelText('Toyota'))
    fireEvent.click(screen.getByLabelText('Honda'))
    fireEvent.click(screen.getByLabelText('SUV'))
    fireEvent.click(screen.getByRole('button', { name: '128 aracı gör' }))

    expect(git).toHaveBeenCalledWith('/tr/cars?bodyType=suv&brand=honda&brand=toyota')
  })
})

describe('0021/AC-50: Gösterilecek marka yoksa formun marka alanı hiç görünmez ve form kasa tipiyle çalışır.', () => {
  it('marka alanı yok, kasa tipi var', async () => {
    vi.mocked(api.fetchHomeBrands).mockResolvedValue([])
    anaSayfa()

    expect(await screen.findByTestId('narrowing-form-body-types')).toBeTruthy()
    expect(screen.queryByTestId('narrowing-form-brands')).toBeNull()
    expect(screen.getByRole('button', { name: '128 aracı gör' })).toBeTruthy()
  })
})

describe('0021/AC-51: Formun marka okuması düştüğünde marka alanı görünmez, teknik ayrıntı gösterilmez ve form kasa tipiyle çalışır (D22).', () => {
  it('hata formu düşürmez', async () => {
    vi.mocked(api.fetchHomeBrands).mockRejectedValue(
      new ApiError('api.unavailable', 'trace-xyz', 500),
    )
    anaSayfa()

    expect(await screen.findByTestId('narrowing-form-body-types')).toBeTruthy()
    expect(screen.queryByTestId('narrowing-form-brands')).toBeNull()
    expect(screen.queryByTestId('narrowing-form-error')).toBeNull()
    expect(document.body.textContent).not.toContain('trace-xyz')
    expect(document.body.textContent).not.toContain('500')
  })
})
