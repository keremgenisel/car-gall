import { describe, expect, it } from 'vitest'
import { formatMileage, formatMoney, formatYear } from './format'

describe('AC-20: Kilometre locale gruplama ayracı alır, yıl hiçbir locale de almaz', () => {
  it('kilometreyi locale gruplamasıyla biçimler', () => {
    expect(formatMileage(125000, 'tr')).toBe('125.000')
    expect(formatMileage(125000, 'en')).toBe('125,000')
    expect(formatMileage(125000, 'ar')).toBe('125,000')
  })

  it('yıla gruplama ayracı koymaz', () => {
    expect(formatYear(2019, 'tr')).toBe('2019')
    expect(formatYear(2019, 'en')).toBe('2019')
    expect(formatYear(2019, 'ar')).toBe('2019')
  })
})

describe('AC-22: KWD tutarı üç ondalıkla ve Batı Arap rakamlarıyla gösterilir', () => {
  const kwd = { onRequest: false, amount: 1250.5, currencyCode: 'KWD', minorUnitExponent: 3 }

  it('TR de 1.250,500 içerir', () => {
    expect(formatMoney(kwd, 'tr')).toContain('1.250,500')
  })

  it('EN de 1,250.500 içerir', () => {
    expect(formatMoney(kwd, 'en')).toContain('1,250.500')
  })

  it('AR de 1,250.500 içerir', () => {
    expect(formatMoney(kwd, 'ar')).toContain('1,250.500')
  })

  it('hiçbir locale de Hint-Arap rakamı kullanmaz', () => {
    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(formatMoney(kwd, locale)).not.toMatch(/[٠-٩۰-۹]/)
    }
  })

  it('iki ondalıklı para biriminde iki ondalık gösterir', () => {
    const tryMoney = { onRequest: false, amount: 1250000, currencyCode: 'TRY', minorUnitExponent: 2 }
    expect(formatMoney(tryMoney, 'tr')).toContain('1.250.000,00')
  })
})

describe('AC-21: fiyat sorunuz tutar taşımaz', () => {
  it('null döner, 0 değil', () => {
    const onRequest = { onRequest: true, amount: null, currencyCode: null, minorUnitExponent: null }
    expect(formatMoney(onRequest, 'tr')).toBeNull()
  })
})
