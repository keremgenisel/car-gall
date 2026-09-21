import { describe, expect, it } from 'vitest'
import { catalogKeys, createTranslator } from './i18n'

describe('AC-31: sonuç sayısı ICU mesaj formatıyla üretilir', () => {
  it('Arapça da 1, 2, 3, 11 ve 100 için farklı çoğul biçimleri kullanır', () => {
    const t = createTranslator('ar')
    const outputs = [1, 2, 3, 11, 100].map((count) => t('showcase.resultCount', { count }))

    // Altı çoğul biçimi olan bir dilde "count > 1" mantığı sessizce yanlış cümle üretir;
    // ICU kullanıldığının kanıtı biçimlerin birbirinden farklı olmasıdır.
    expect(new Set(outputs).size).toBeGreaterThanOrEqual(4)
    for (const output of outputs) {
      expect(output.trim()).not.toBe('')
    }
  })

  it('Türkçe ve İngilizce de sayıyı cümleye gömer', () => {
    expect(createTranslator('tr')('showcase.resultCount', { count: 12 })).toContain('12')
    expect(createTranslator('en')('showcase.resultCount', { count: 12 })).toContain('12')
  })
})

describe('AC-32: görsel alt metni model adı + yıl kalıbından üretilir', () => {
  it('üç locale de model adını ve gruplamasız yılı taşır', () => {
    for (const locale of ['tr', 'ar', 'en'] as const) {
      const alt = createTranslator(locale)('showcase.card.imageAlt', {
        model: 'Toyota Corolla',
        year: 2019,
      })
      expect(alt).toContain('Toyota Corolla')
      expect(alt).toContain('2019')
    }
  })
})

describe('AC-29: hiçbir locale de ham çeviri anahtarı görünmez', () => {
  it('her anahtar her locale de boş olmayan bir metin üretir', () => {
    const keys = catalogKeys('tr')

    for (const locale of ['tr', 'ar', 'en'] as const) {
      const t = createTranslator(locale)

      for (const key of keys) {
        const value = t(key, {
          count: 3,
          model: 'X',
          year: 2020,
          kilometers: 1000,
          page: 1,
          totalPages: 2,
        })
        expect(value).not.toBe(key)
        expect(value.trim()).not.toBe('')
      }
    }
  })
})
