import { describe, expect, it } from 'vitest'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'
import { LEAD_TYPES } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'

const LOCALES: readonly AppLocale[] = ['tr', 'ar', 'en']

/** Ziyaretçiye görünen her talep metni. Eksik anahtar boş ekran demektir. */
const GORUNEN_ANAHTARLAR = [
  'lead.title',
  'lead.intro',
  'lead.field.type',
  'lead.field.fullName',
  'lead.field.phone',
  'lead.field.phoneHint',
  'lead.field.email',
  'lead.field.message',
  'lead.field.messageOptional',
  'lead.consent.label',
  'lead.consent.text',
  'lead.submit',
  'lead.submitting',
  'lead.closed.title',
  'lead.closed.body',
  ...LEAD_TYPES.map((type) => `lead.type.${type}`),
]

/**
 * AC-43 — onay ve hata metinleri üç dilde YAZILI ve her birinde BİR SONRAKİ EYLEMİ
 * söyler. "Bir sonraki eylem" burada bir eylem anahtarının varlığı olarak ölçülüyor:
 * metnin tonu testle ölçülemez, ama eylemsiz bırakılmış bir ekran ölçülebilir.
 */
const EYLEMLI_METINLER: readonly { readonly body: string; readonly action: string }[] = [
  { body: 'lead.success.body', action: 'lead.success.action' },
  { body: 'lead.error.generic', action: 'lead.error.retry' },
  { body: 'lead.error.listing_unavailable', action: 'lead.error.listing_unavailable.action' },
  { body: 'lead.error.rate_limited', action: 'lead.error.retry' },
]

/** Sunucunun döndürebileceği her `errorCode`'un uçta karşılığı olmalıdır. */
const SUNUCU_HATA_KODLARI = [
  'lead.listing_unavailable',
  'lead.listing_reserved',
  'lead.rate_limited',
  'lead.missing_name',
  'lead.name_too_long',
  'lead.missing_contact_channel',
  'lead.invalid_phone',
  'lead.invalid_email',
  'lead.email_too_long',
  'lead.message_too_long',
]

describe('AC-41: Form etiketleri, talep tipi adları ve onay metni TR, AR ve EN locale lerinde kendi dilinde görünür', () => {
  it.each(LOCALES)('%s katalogunda görünen anahtarların hepsi dolu', (locale) => {
    const t = createTranslator(locale)

    for (const key of GORUNEN_ANAHTARLAR) {
      expect(t(key), `${locale}: ${key}`).not.toBe('')
    }
  })

  it('aynı metin üç locale de aynı değildir', () => {
    const [tr, ar, en] = LOCALES.map((locale) => createTranslator(locale)('lead.submit'))

    expect(new Set([tr, ar, en]).size).toBe(3)
  })
})

describe('AC-43: Onay ve hata metinleri üç etkin dilin hepsinde yazılıdır ve her birinde bir sonraki eylemi söyler', () => {
  it.each(LOCALES)('%s: onay ve hata metinleri dolu ve eylem anahtarı var', (locale) => {
    const t = createTranslator(locale)

    expect(t('lead.success.title')).not.toBe('')
    expect(t('lead.error.title')).not.toBe('')

    for (const { body, action } of EYLEMLI_METINLER) {
      expect(t(body), `${locale}: ${body}`).not.toBe('')
      expect(t(action), `${locale}: ${action}`).not.toBe('')
    }
  })

  it.each(LOCALES)('%s: sunucunun her hata kodunun karşılığı var', (locale) => {
    const keys = catalogKeys(locale)
    const t = createTranslator(locale)

    for (const code of SUNUCU_HATA_KODLARI) {
      const key = `lead.error.${code.replace(/^lead\./, '')}`

      expect(keys, `${locale}: ${key}`).toContain(key)
      expect(t(key)).not.toBe('')
    }
  })
})

describe('AC-37: Talep başarıyla kaydedildiğinde ziyaretçiye talebinin alındığı bildirilir; galerinin kendisine ulaşacağına dair bir söz verilmez', () => {
  /**
   * Metnin söz VERMEDİĞİ otomatik ölçülemez — bu, gözden geçirmeyle doğrulanan bir
   * şeydir (Karar D6). Ölçülebilen şey: onay metni ziyaretçiye bir ZAMAN ya da
   * GERİ DÖNÜŞ taahhüdü içeren kalıpları kullanmıyor mu.
   */
  it.each<[AppLocale, readonly RegExp[]]>([
    ['tr', [/ulaşacak/i, /dönecek/i, /arayacak/i, /saat içinde/i]],
    ['en', [/will contact/i, /will get back/i, /will call/i, /within \d/i]],
    ['ar', [/سيتواصل/, /سنتواصل/, /خلال \d/]],
  ])('%s: onay metni ulaşma sözü vermez', (locale, yasakKaliplar) => {
    const metin = createTranslator(locale)('lead.success.body')

    for (const kalip of yasakKaliplar) {
      expect(metin, `${locale}: "${metin}"`).not.toMatch(kalip)
    }
  })
})
