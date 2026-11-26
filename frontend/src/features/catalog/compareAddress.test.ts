import { describe, expect, it } from 'vitest'
import { compareSearch, comparePath, comparedSlugs } from './compareAddress'

describe('karşılaştırma adresi (frontend-003 D3)', () => {
  it('frontend-003/AC-8: yinelenen, büyük harfli, boşluklu ve dördüncü geçerli değer yok sayılır; sıra ilk görülme sırasıdır; utm korunur', () => {
    const search =
      '?listing=b-2020-bbbbbb&listing=a-2020-aaaaaa&listing=b-2020-bbbbbb&listing=Abc-2020-cccccc' +
      '&listing=c-2020-cccccc&listing=d-2020-dddddd&listing=x%20y&utm_source=z'
    const slugs = comparedSlugs(search)

    expect(slugs).toEqual(['b-2020-bbbbbb', 'a-2020-aaaaaa', 'c-2020-cccccc'])
    expect(compareSearch(search, slugs)).toBe(
      '?listing=b-2020-bbbbbb&listing=a-2020-aaaaaa&listing=c-2020-cccccc&utm_source=z',
    )
  })

  it('frontend-003/AC-9: geçerli değeri ikiden az olan adresler', () => {
    expect(comparedSlugs('')).toEqual([])
    expect(comparedSlugs('?listing=a-2020-aaaaaa')).toEqual(['a-2020-aaaaaa'])
    expect(comparedSlugs('?listing=%%%')).toEqual([])
  })

  it('frontend-003/AC-48: "Karşılaştır" adresi seçimleri verilen sırayla taşır', () => {
    expect(comparePath('ar', ['b-2020-bbbbbb', 'a-2020-aaaaaa'])).toBe(
      '/ar/compare?listing=b-2020-bbbbbb&listing=a-2020-aaaaaa',
    )
  })
})
