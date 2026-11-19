import { describe, expect, it } from 'vitest'
import { formatMileage, formatMoney, formatYear } from './format'
import fikstur from './format.fixtures.json'

/**
 * `0018` **D24** · **D32** — fikstür vitrinin biçim çıktısıdır ve bülten (backend) aynı
 * dosyaya karşı ölçülür (`BultenGonderimTests.AyniBaytBayt_VitrininBicimiyle`). Ön yüz biçimi
 * değişirse önce BU test kırmızıya döner; fikstür güncellenince backend testi.
 */
const DILLER = ['tr', 'ar', 'en'] as const

describe("0018/AC-77: TRY ve KWD fiyatı, km ve model yılı için bültenin biçimi, aynı değerlerle vitrinin `formatMoney`, `formatMileage` ve `formatYear` çıktısıyla TR, AR ve EN'de bayt bayt aynıdır; Arapça'da rakam kümesi dâhil (D24)", () => {
  it('fikstür boş değil ve iki para birimini taşıyor', () => {
    expect(fikstur.money.map((item) => item.currencyCode)).toEqual(
      expect.arrayContaining(['TRY', 'KWD']),
    )
    expect(fikstur.mileage.length).toBeGreaterThan(0)
    expect(fikstur.year.length).toBeGreaterThan(0)
  })

  it.each(fikstur.money.flatMap((item) => DILLER.map((dil) => [dil, item] as const)))(
    'formatMoney %s %o',
    (dil, item) => {
      expect(
        formatMoney(
          {
            onRequest: false,
            amount: Number(item.amount),
            currencyCode: item.currencyCode,
            minorUnitExponent: item.minorUnitExponent,
          },
          dil,
        ),
      ).toBe(item[dil])
    },
  )

  it.each(fikstur.mileage.flatMap((item) => DILLER.map((dil) => [dil, item] as const)))(
    'formatMileage %s %o',
    (dil, item) => expect(formatMileage(item.value, dil)).toBe(item[dil]),
  )

  it.each(fikstur.year.flatMap((item) => DILLER.map((dil) => [dil, item] as const)))(
    'formatYear %s %o',
    (dil, item) => expect(formatYear(item.value, dil)).toBe(item[dil]),
  )

  it('Arapçada rakamlar Batı Arap rakamıdır (U+0030–U+0039)', () => {
    for (const item of fikstur.money) {
      expect(item.ar).not.toMatch(/[٠-٩۰-۹]/)
    }
  })
})
