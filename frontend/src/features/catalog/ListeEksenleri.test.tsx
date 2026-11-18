import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import type { PagedResult, ShowcaseListing } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { ShowcaseListPage } from './ShowcaseListPage'

/**
 * 0015 — liste ekranının eksenleri, özetleri, sıralama seçicisi ve boş sonuç ekranı.
 * Sorgu ve sıralamanın kendisi sunucuda (`VitrinEksenleriTests`, `VitrinSiralamaTests`);
 * burada ölçülen ekranın davranışı ve adresin hâli.
 */
vi.mock('../../shared/api/catalog', () => ({
  fetchShowcaseListings: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
  fetchReferenceTerms: vi.fn(),
}))

const { fetchShowcaseListings, fetchBodyTypes, fetchFieldSettings, fetchReferenceTerms } =
  await import('../../shared/api/catalog')

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

function ekran(adres = '/tr/cars', locale: AppLocale = 'tr') {
  window.history.replaceState(null, '', adres)
  return render(
    <LocaleProvider locale={locale}>
      <ShowcaseListPage />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  vi.mocked(fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: true }],
  })
  vi.mocked(fetchBodyTypes).mockResolvedValue([{ slug: 'suv', label: 'SUV' }])
  vi.mocked(fetchReferenceTerms).mockImplementation((segment, locale) =>
    Promise.resolve(
      segment === 'fuel-types'
        ? [
            { kind: 'FuelType', slug: 'diesel', label: locale === 'ar' ? 'ديزل' : 'Dizel' },
            { kind: 'FuelType', slug: 'electric', label: 'Elektrik' },
            { kind: 'FuelType', slug: 'hybrid', label: 'Hibrit' },
            { kind: 'FuelType', slug: 'petrol', label: 'Benzin' },
          ]
        : [
            { kind: 'TransmissionType', slug: 'automatic', label: 'Otomatik' },
            { kind: 'TransmissionType', slug: 'manual', label: 'Manuel' },
          ],
    ),
  )
  vi.mocked(fetchShowcaseListings).mockResolvedValue(sayfa(12))
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0015 — eksenler ve özetler (R10, R16)', () => {
  it("0015/AC-33: Filtre çubuğu yakıt ve vitesin tüm değerlerini etkin locale'in adıyla ve uçtan gelen sırada çip olarak gösterir; çiplerin yanında sayı yoktur", async () => {
    ekran()

    const yakit = await screen.findByTestId('fuels-filter')
    const etiketler = within(yakit)
      .getAllByRole('checkbox')
      .map((kutu) => kutu.closest('label')?.textContent)
    expect(etiketler).toEqual(['Dizel', 'Elektrik', 'Hibrit', 'Benzin'])
    expect(etiketler.join(' ')).not.toMatch(/\d/)
    expect(
      within(screen.getByTestId('transmissions-filter')).getAllByRole('checkbox'),
    ).toHaveLength(2)
    expect(vi.mocked(fetchReferenceTerms)).toHaveBeenCalledWith(
      'fuel-types',
      'tr',
      expect.anything(),
    )
  })

  it('0015/AC-34: Çok değerli bir eksende 2 değer seçiliyken eksenin özeti seçili değer sayısını ICU ile söyler', async () => {
    ekran('/tr/cars?fuel=diesel&fuel=hybrid')

    expect((await screen.findByTestId('fuels-summary')).textContent).toBe('2 seçili')
    expect(screen.getByTestId('transmissions-summary').textContent).toBe('Seçim yok')
  })

  it('0015/AC-35: Aralık ekseninin özeti seçilen sınırlardır: yıl 2018–2021 için "2018–2021", yalnız üst fiyat sınırı 500.000 için "≤ 500.000"', async () => {
    ekran('/tr/cars?yearMin=2018&yearMax=2021&priceMax=500000')

    expect((await screen.findByTestId('year-summary')).textContent).toBe('2018–2021')
    expect(screen.getByTestId('price-summary').textContent).toBe('≤ 500.000')
    expect(screen.getByTestId('mileage-summary').textContent).toBe('Tümü')
  })

  it('0015/AC-36: Bir eksenin temizle eylemi yalnız o eksenin değerlerini kaldırır; diğer eksenler ve sıralama korunur', async () => {
    ekran('/tr/cars?fuel=diesel&transmission=manual&yearMin=2018&sort=year-asc')

    const yakit = await screen.findByTestId('fuels-filter')
    fireEvent.click(within(yakit).getByRole('button', { name: 'Yakıt seçimini temizle' }))

    expect(window.location.search).toBe('?transmission=manual&yearMin=2018&sort=year-asc')
  })
})

describe('0015 — aralık uygulaması ve adres (R11, R12, D19)', () => {
  it('0015/AC-32: Bir aralık alanına yazarken adres değişmez; aralık Enter\'a basınca veya "Uygula"ya tıklanınca uygulanır ve geçmişe tek kayıt ekler', async () => {
    const pushState = vi.spyOn(window.history, 'pushState')
    ekran('/tr/cars?fuel=diesel')

    const yil = await screen.findByTestId('year-filter')
    fireEvent.change(within(yil).getByLabelText('En az'), { target: { value: '2018' } })
    fireEvent.change(within(yil).getByLabelText('En çok'), { target: { value: '2021' } })

    expect(window.location.search).toBe('?fuel=diesel')
    expect(pushState).not.toHaveBeenCalled()

    // Enter, formun yerel gönderimidir.
    fireEvent.submit(within(yil).getByLabelText('En çok').closest('form')!)

    expect(window.location.search).toBe('?fuel=diesel&yearMin=2018&yearMax=2021')
    expect(pushState).toHaveBeenCalledTimes(1)

    const fiyat = screen.getByTestId('price-filter')
    fireEvent.change(within(fiyat).getByLabelText('En çok'), { target: { value: '500000' } })
    fireEvent.click(within(fiyat).getByRole('button', { name: 'Uygula' }))

    expect(window.location.search).toBe('?fuel=diesel&yearMin=2018&yearMax=2021&priceMax=500000')
    expect(pushState).toHaveBeenCalledTimes(2)

    // QA BULGU-1 — adresi DEĞİŞTİRMEYEN gönderim geçmişe kayıt eklemez: aynı aralığı ikinci
    // kez "Uygula" (Enter'dan sonra Uygula olağan bir akış) ve boş eksene geçersiz değer.
    fireEvent.click(within(fiyat).getByRole('button', { name: 'Uygula' }))
    const kilometre = screen.getByTestId('mileage-filter')
    fireEvent.change(within(kilometre).getByLabelText('En az'), { target: { value: 'abc' } })
    fireEvent.click(within(kilometre).getByRole('button', { name: 'Uygula' }))

    expect(window.location.search).toBe('?fuel=diesel&yearMin=2018&yearMax=2021&priceMax=500000')
    expect(pushState).toHaveBeenCalledTimes(2)
    pushState.mockRestore()
  })

  it('0015/AC-30: 2. sayfadayken bir eksen veya sıralama değiştiğinde liste 1. sayfadan başlar ve bu tek bir geçmiş kaydıdır', async () => {
    // Sunucu istenen sayfayı döndürür; aksi hâlde `0001`'in "son sayfanın ötesi" kuralı
    // adresi tıklamadan ÖNCE 1. sayfaya düzeltir ve test sıfırlamayı ölçmez.
    vi.mocked(fetchShowcaseListings).mockImplementation((query) =>
      Promise.resolve({ ...sayfa(60), page: query.page, totalPages: 3 }),
    )
    const pushState = vi.spyOn(window.history, 'pushState')
    ekran('/tr/cars?page=2')
    await screen.findByTestId('result-count')
    expect(window.location.search).toBe('?page=2')

    fireEvent.click(await screen.findByRole('checkbox', { name: 'Dizel' }))
    expect(window.location.search).toBe('?fuel=diesel')
    expect(pushState).toHaveBeenCalledTimes(1)

    fireEvent.change(screen.getByTestId('sort-select'), { target: { value: 'price-asc' } })
    expect(window.location.search).toBe('?fuel=diesel&sort=price-asc')
    expect(pushState).toHaveBeenCalledTimes(2)
    pushState.mockRestore()
  })

  it.each([
    ['/tr/cars?yearMin=2021&yearMax=2018&fuel=diesel', '?fuel=diesel'],
    ['/tr/cars?mileageMax=abc', ''],
    ['/tr/cars?priceMax=500000.5&transmission=manual', '?transmission=manual'],
  ])(
    '0015/AC-14: Uç geçersiz aralık parametresini (%s) adresten replaceState ile kaldırır; geçmişe kayıt eklenmez',
    async (adres, beklenen) => {
      const pushState = vi.spyOn(window.history, 'pushState')

      ekran(adres)
      await screen.findByTestId('result-count')

      expect(window.location.search).toBe(beklenen)
      expect(pushState).not.toHaveBeenCalled()
      pushState.mockRestore()
    },
  )

  it('0015/AC-15: ?sort=cheapest varsayılan sıralamayı uygular ve uç sort parametresini adresten kaldırır', async () => {
    ekran('/tr/cars?sort=cheapest')
    await screen.findByTestId('result-count')

    expect(window.location.search).toBe('')
    expect((screen.getByTestId('sort-select') as HTMLSelectElement).value).toBe('default')
    expect(vi.mocked(fetchShowcaseListings).mock.calls[0]?.[0].sort).toBeUndefined()
  })

  it('0015/AC-16: ?fuel=kerosene parametresi adreste kalır ve sunucuya gönderilir (sunucu yok sayar)', async () => {
    ekran('/tr/cars?fuel=kerosene')
    await screen.findByTestId('result-count')

    expect(window.location.search).toBe('?fuel=kerosene')
    expect(vi.mocked(fetchShowcaseListings).mock.calls[0]?.[0].fuels).toEqual(['kerosene'])
  })
})

describe('0015 — sıralama seçici ve boş sonuç (R5, R13)', () => {
  it("0015/AC-37: Sıralama seçici varsayılanla birlikte yedi seçeneği etkin locale'in metniyle sunar ve seçili olanı gösterir", async () => {
    ekran('/tr/cars?sort=year-desc')

    const secici = (await screen.findByTestId('sort-select')) as HTMLSelectElement
    expect(Array.from(secici.options).map((option) => option.textContent)).toEqual([
      'Önerilen',
      'En yeni ilan',
      'Fiyat: düşükten yükseğe',
      'Fiyat: yüksekten düşüğe',
      'Kilometre: düşükten yükseğe',
      'Yıl: yeniden eskiye',
      'Yıl: eskiden yeniye',
    ])
    expect(secici.value).toBe('year-desc')
  })

  it('0015/AC-38: Seçili eksenlerle hiç sonuç yokken ekran etkin eksenlerin adlarını söyler ve tüm daraltmaları kaldıran bir temizle eylemi sunar; sıralama korunur', async () => {
    vi.mocked(fetchShowcaseListings).mockImplementation((query) =>
      Promise.resolve(sayfa((query.fuels?.length ?? 0) > 0 || query.year !== undefined ? 0 : 12)),
    )
    ekran('/tr/cars?fuel=diesel&yearMin=2030&sort=newest')

    const bos = await screen.findByTestId('empty-by-filter')
    expect(bos.textContent).toContain('Yakıt ve Yıl')

    fireEvent.click(within(bos).getByRole('button', { name: 'Seçimi temizle' }))

    expect(window.location.search).toBe('?sort=newest')
  })

  it('0015/AC-39: Vitrinde hiç görünür ilan yokken "hiç yayında araç yok" durumu görünür, AC-38\'in ekranı görünmez', async () => {
    vi.mocked(fetchShowcaseListings).mockResolvedValue(sayfa(0))
    ekran('/tr/cars?fuel=diesel')

    expect(await screen.findByTestId('empty-showcase')).toBeTruthy()
    expect(screen.queryByTestId('empty-by-filter')).toBeNull()
  })

  it("0015/AC-40: /ar/cars sayfasında eksen adları, değerleri, özetler ve sıralama seçenekleri Arapça'dır", async () => {
    ekran('/ar/cars?fuel=diesel', 'ar')

    const yakit = await screen.findByTestId('fuels-filter')
    expect(yakit.querySelector('legend')?.textContent).toBe('الوقود')
    expect(within(yakit).getByRole('checkbox', { name: 'ديزل' })).toBeTruthy()
    expect(screen.getByTestId('fuels-summary').textContent).toBe('قيمة واحدة محددة')
    expect((screen.getByTestId('sort-select') as HTMLSelectElement).options[0]?.textContent).toBe(
      'المقترح',
    )
  })
})

describe('0015 — değer listeleri okunamazsa', () => {
  it('çok değerli eksenler gizlenir; aralık eksenleri ve sıralama çalışmaya devam eder', async () => {
    vi.mocked(fetchReferenceTerms).mockRejectedValue(new Error('ağ'))

    ekran()

    await waitFor(() => expect(screen.getByTestId('list-filters')).toBeTruthy())
    expect(screen.queryByTestId('fuels-filter')).toBeNull()
    expect(screen.getByTestId('year-filter')).toBeTruthy()
    expect(screen.getByTestId('sort-select')).toBeTruthy()
  })
})
