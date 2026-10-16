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
          // 0008 — onay metinleri personelin adını taşıyor (AC-5).
          name: 'Ayşe Yılmaz',
          // 0010a — şube etiketi kapatılmış şubede durumu da taşıyor.
          status: 'Kapalı',
          // 0010b — liste özeti, eksik dil rozeti ve alan etiketleri.
          total: 12,
          locales: 'العربية',
          label: 'Tramer tutarı',
          current: 'Beyaz',
          locale: 'Türkçe',
          // 0012a — medya kartı fotoğrafın sırasını söylüyor (AC-4: "ilk eleman kapaktır").
          position: 1,
          // 0012b — yayın reddi hangi ALANLARIN eksik olduğunu da söylüyor (D24) ve
          // durumu adıyla anıyor.
          fields: 'Tramer tutarı',
          // 0012c — toplu eylemin sayıları ve seçim sınırı (**D29**, **D45**).
          limit: 100,
          applied: 2,
          failed: 1,
        })
        expect(value).not.toBe(key)
        expect(value.trim()).not.toBe('')
      }
    }
  })
})

describe('0012c/AC-37: toplu eylemin sayısı Arapçada da doğru çoğul biçimini kullanır', () => {
  /**
   * Bu test bir dış bulgunun bedeli. Altı başarı mesajının **beşi** ve kısmi başarı
   * paneli yalnız `other` biçimini taşıyordu: aynı ekranda "yayına al" doğru cümle
   * kurarken "arşivle" 1, 2 ve 3-10 için dilbilgisi hatalı cümle basıyordu — yani
   * gerçekçi her toplu eylem boyutunda. `check:locales` o gün "386 anahtar eşit" diyordu,
   * çünkü anahtar kümelerini karşılaştırıyor, biçim kapsamını değil.
   */
  const anahtarlar = [
    'vehicle.bulk.selected',
    'vehicle.bulk.success.publish',
    'vehicle.bulk.success.unpublish',
    'vehicle.bulk.success.sold',
    'vehicle.bulk.success.archive',
    'vehicle.bulk.success.feature',
    'vehicle.bulk.success.unfeature',

    // 0013/AC-57 — kalan kurtarma kodu sayısı. Sayı gerçekçi olarak 1, 2 ve 3–10
    // aralığında dolaşıyor (varsayılan küme on kod): Arapçanın üç ayrı biçimi tam o
    // aralıkta gerekiyor.
    'twoFactor.codes.remaining',
  ] as const

  it.each(anahtarlar)('%s — 1, 2 ve 3 ayrı cümleler', (key) => {
    const t = createTranslator('ar')
    const ciktilar = [1, 2, 3].map((count) => t(key, { count }))

    // Tekil · ikil · çoğul: üçü de birbirinden farklı olmak zorunda. `other`'a düşen bir
    // mesaj üçünde de aynı cümleyi basar ve kapı tam burada açılırdı.
    expect(new Set(ciktilar).size).toBe(3)

    // İkil biçimde sayı RAKAMLA yazılmaz: Arapçada ikil kelimenin kendisinde yaşıyor.
    expect(ciktilar[1]).not.toContain('2')
  })

  it('kısmi başarı paneli iki sayısını da ayrı ayrı çekimliyor', () => {
    const t = createTranslator('ar')

    expect(t('vehicle.bulk.partial', { applied: 1, failed: 2 })).not.toBe(
      t('vehicle.bulk.partial', { applied: 2, failed: 1 }),
    )
    expect(t('vehicle.bulk.partial', { applied: 3, failed: 11 })).not.toContain('plural')
  })
})
