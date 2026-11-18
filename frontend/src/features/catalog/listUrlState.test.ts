import { describe, expect, it } from 'vitest'
import {
  activeAxes,
  clearAxis,
  clearBodyTypes,
  clearFilters,
  isCanonicalSearch,
  parseListState,
  toCanonicalSearch,
  toggleBodyType,
  toggleValue,
  withPage,
  withRange,
  withSort,
  type ListState,
} from './listUrlState'

describe('C13: kanonik URL biçimi', () => {
  it('çoklu seçimi tekrarlı parametreyle yazar, virgüllü liste kullanmaz', () => {
    expect(toCanonicalSearch({ bodyTypes: ['sedan', 'suv'], page: 1 })).toBe(
      '?bodyType=sedan&bodyType=suv',
    )
  })

  it('page=1 i URL ye yazmaz', () => {
    expect(toCanonicalSearch({ bodyTypes: [], page: 1 })).toBe('')
    expect(toCanonicalSearch({ bodyTypes: [], page: 2 })).toBe('?page=2')
  })

  it('boş bodyType parametresini yazmaz', () => {
    expect(toCanonicalSearch(parseListState('?bodyType=&page=1'))).toBe('')
  })

  it('kanonik olmayan adresi tanır', () => {
    expect(isCanonicalSearch('?page=1')).toBe(false)
    expect(isCanonicalSearch('?bodyType=suv')).toBe(true)
    expect(isCanonicalSearch('')).toBe(true)
  })

  it('tanınmayan değeri uçta elemez; kanonik kümeyi sunucu bilir', () => {
    expect(parseListState('?bodyType=SUV').bodyTypes).toEqual(['SUV'])
  })

  it('tekrarlanan aynı değeri bir kez taşır', () => {
    expect(parseListState('?bodyType=suv&bodyType=suv').bodyTypes).toEqual(['suv'])
  })
})

describe('AC-8: İkinci sayfadayken bir kasa tipi seçildiğinde liste ilk sayfadan başlar', () => {
  it('daraltma değişince sayfa 1 e döner', () => {
    const ikinciSayfa = { bodyTypes: [] as string[], page: 2 }

    expect(toggleBodyType(ikinciSayfa, 'suv')).toEqual({ bodyTypes: ['suv'], page: 1 })
  })

  it('seçim temizlenince de sayfa 1 e döner', () => {
    expect(clearBodyTypes()).toEqual({ bodyTypes: [], page: 1 })
  })
})

describe('R5: geçmiş kaydı tanesi', () => {
  it('daraltma değişikliği tek bir sonuç durumu üretir; sayfa sıfırlaması ayrı adım değildir', () => {
    // İki ayrı güncelleme yapılsaydı geri tuşu ziyaretçiyi "suv seçili, sayfa 2" gibi
    // hiç görmediği bir ara hâle düşürürdü (AC-19).
    const sonuc = toggleBodyType({ bodyTypes: ['sedan'], page: 3 }, 'suv')

    expect(sonuc).toEqual({ bodyTypes: ['sedan', 'suv'], page: 1 })
  })

  it('sayfa değişimi daraltmayı korur', () => {
    expect(withPage({ bodyTypes: ['suv'], page: 1 }, 2)).toEqual({ bodyTypes: ['suv'], page: 2 })
  })

  it('geçersiz sayfa numarasını 1 e normalize eder', () => {
    expect(parseListState('?page=0').page).toBe(1)
    expect(parseListState('?page=-3').page).toBe(1)
    expect(parseListState('?page=abc').page).toBe(1)
  })
})

describe('0015 — adres sözleşmesi (C12)', () => {
  const tam =
    '?fuel=diesel&fuel=hybrid&transmission=automatic&yearMin=2018&yearMax=2021&mileageMax=80000&priceMin=500000&priceMax=1500000&sort=price-asc'

  it('0015/AC-27: bütün eksenler ve sıralama C12 adlarıyla, tekrarlı parametreyle ve sabit sırayla yazılır; boş parametre ve page=1 yok', () => {
    const durum = parseListState(tam)

    expect(toCanonicalSearch(durum)).toBe(tam)
    expect(isCanonicalSearch(tam)).toBe(true)
    // Sırası karışık aynı adres kanonik DEĞİLDİR; uç onu düzeltir.
    expect(isCanonicalSearch('?sort=price-asc&fuel=diesel')).toBe(false)
  })

  it('0015/AC-28: kanonik adres okunup yeniden yazıldığında aynı seçimleri taşır', () => {
    const durum = parseListState(tam)

    expect(durum.fuels).toEqual(['diesel', 'hybrid'])
    expect(durum.transmissions).toEqual(['automatic'])
    expect(durum.year).toEqual({ min: 2018, max: 2021 })
    expect(durum.mileage).toEqual({ min: null, max: 80000 })
    expect(durum.price).toEqual({ min: 500000, max: 1500000 })
    expect(durum.sort).toBe('price-asc')
  })

  it.each([
    ['?yearMin=2021&yearMax=2018&fuel=diesel', '?fuel=diesel'],
    ['?mileageMax=abc', ''],
    ['?mileageMax=-5', ''],
    ['?priceMax=500000.5&yearMin=2019', '?yearMin=2019'],
  ])(
    '0015/AC-14: geçersiz aralık %s uçta düşer; geçerli diğer parametreler kalır',
    (adres, beklenen) => {
      expect(isCanonicalSearch(adres)).toBe(false)
      expect(toCanonicalSearch(parseListState(adres))).toBe(beklenen)
    },
  )

  it('0015/AC-15: tanınmayan sort uçta düşer (varsayılan sıralama)', () => {
    expect(parseListState('?sort=cheapest').sort).toBeUndefined()
    expect(toCanonicalSearch(parseListState('?sort=cheapest&page=2'))).toBe('?page=2')
  })

  it('0015/AC-16: tanınmayan slug uçta ELENMEZ; adreste kalır (D18)', () => {
    expect(isCanonicalSearch('?fuel=kerosene')).toBe(true)
    expect(parseListState('?fuel=kerosene&fuel=Diesel').fuels).toEqual(['kerosene', 'Diesel'])
  })

  it('0015/AC-26: varsayılan sıralama adrese yazılmaz', () => {
    expect(toCanonicalSearch(withSort({ bodyTypes: [], page: 1, sort: 'newest' }, undefined))).toBe(
      '',
    )
  })

  it('0015/AC-30: herhangi bir eksen veya sıralama değişince sayfa 1 e döner, tek durum üretir', () => {
    const ikinci: ListState = { bodyTypes: [], page: 2, fuels: ['diesel'] }

    expect(toggleValue(ikinci, 'fuels', 'petrol').page).toBe(1)
    expect(withRange(ikinci, 'year', { min: 2018, max: null }).page).toBe(1)
    expect(withSort(ikinci, 'year-desc').page).toBe(1)
    expect(clearAxis(ikinci, 'fuels').page).toBe(1)
  })

  it('0015/AC-36: bir eksenin temizle eylemi yalnız o ekseni kaldırır; diğer eksenler ve sıralama korunur', () => {
    const durum = parseListState('?fuel=diesel&transmission=manual&yearMin=2018&sort=year-asc')

    const sonuc = clearAxis(durum, 'fuels')

    expect(toCanonicalSearch(sonuc)).toBe('?transmission=manual&yearMin=2018&sort=year-asc')
    expect(toCanonicalSearch(clearAxis(durum, 'year'))).toBe(
      '?fuel=diesel&transmission=manual&sort=year-asc',
    )
  })

  it('0015/AC-38: tüm daraltmaları temizlemek sıralamayı korur', () => {
    const sonuc = clearFilters(
      parseListState('?bodyType=suv&fuel=diesel&priceMax=1&sort=newest&page=3'),
    )

    expect(toCanonicalSearch(sonuc)).toBe('?sort=newest')
    expect(activeAxes(sonuc)).toEqual([])
  })
})
