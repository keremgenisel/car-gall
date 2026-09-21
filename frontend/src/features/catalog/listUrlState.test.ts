import { describe, expect, it } from 'vitest'
import {
  clearBodyTypes,
  isCanonicalSearch,
  parseListState,
  toCanonicalSearch,
  toggleBodyType,
  withPage,
} from './listUrlState'

describe('C13: kanonik URL biçimi', () => {
  it('çoklu seçimi tekrarlı parametreyle yazar, virgüllü liste kullanmaz', () => {
    expect(toCanonicalSearch({ bodyTypes: ['sedan', 'suv'], page: 1 })).toBe('?bodyType=sedan&bodyType=suv')
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
