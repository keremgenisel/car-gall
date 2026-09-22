import { describe, expect, it } from 'vitest'
import {
  EMPTY_LEAD_FORM,
  MAX_MESSAGE_LENGTH,
  MAX_NAME_LENGTH,
  messageKeyFor,
  toRequest,
  validate,
  type LeadFormValues,
} from './leadFormState'

function values(overrides: Partial<LeadFormValues> = {}): LeadFormValues {
  return { ...EMPTY_LEAD_FORM, fullName: 'Ayşe Yılmaz', phone: '+905321112233', ...overrides }
}

describe('AC-2: Ne telefon ne e-posta girilmiş bir talep kaydedilmez', () => {
  it('hiç iletişim kanalı yoksa hata verir', () => {
    expect(validate(values({ phone: '', email: '' }))).toEqual({
      field: 'phone',
      code: 'lead.missing_contact_channel',
    })
  })
})

describe('AC-3: Adı girilmemiş bir talep kaydedilmez', () => {
  it('ad boşsa hata verir', () => {
    expect(validate(values({ fullName: '   ' }))).toEqual({
      field: 'fullName',
      code: 'lead.missing_name',
    })
  })
})

describe('AC-4: E.164 biçiminde olmayan bir telefon numarasıyla talep kaydedilmez', () => {
  it.each(['05321112233', '+0532111223', '+90 532 111 22 33', '905321112233', '+90532'])(
    '%s reddedilir',
    (numara) => {
      expect(validate(values({ phone: numara }))).toEqual({
        field: 'phone',
        code: 'lead.invalid_phone',
      })
    },
  )

  it('E.164 numara kabul edilir', () => {
    expect(validate(values({ phone: '+905321112233' }))).toBeNull()
  })
})

describe('AC-5: Geçerli biçimde olmayan bir e-posta ile talep kaydedilmez', () => {
  it.each(['ayse', 'ayse@', '@ornek.com', 'ayse@ornek', 'ayse ornek@ornek.com'])(
    '%s reddedilir',
    (eposta) => {
      expect(validate(values({ phone: '', email: eposta }))).toEqual({
        field: 'email',
        code: 'lead.invalid_email',
      })
    },
  )

  it('yalnız e-posta ile talep geçerlidir', () => {
    expect(validate(values({ phone: '', email: 'ayse@ornek.com' }))).toBeNull()
  })
})

describe('AC-6: Ad, e-posta veya mesaj alanı üst uzunluk sınırını aşan bir talep kaydedilmez', () => {
  it('ad sınırı', () => {
    expect(validate(values({ fullName: 'a'.repeat(MAX_NAME_LENGTH + 1) }))?.code).toBe(
      'lead.name_too_long',
    )
  })

  it('e-posta sınırı', () => {
    expect(validate(values({ email: `${'a'.repeat(250)}@ornek.com` }))?.code).toBe(
      'lead.email_too_long',
    )
  })

  it('mesaj sınırı', () => {
    expect(validate(values({ message: 'm'.repeat(MAX_MESSAGE_LENGTH + 1) }))?.code).toBe(
      'lead.message_too_long',
    )
  })
})

describe('AC-7: Mesaj alanı boş bırakılarak bir talep kaydedilebilir', () => {
  it('boş mesaj geçerlidir ve gövdeye null gider', () => {
    expect(validate(values({ message: '' }))).toBeNull()
    expect(toRequest(values({ message: '  ' }), 'slug', 'tr').message).toBeNull()
  })
})

describe('AC-26: Talep formunda iletişim izni kutusu önceden işaretli gelmez', () => {
  it('başlangıç değeri işaretsizdir', () => {
    expect(EMPTY_LEAD_FORM.consentGiven).toBe(false)
  })
})

describe('AC-39: Hata ekranında HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('bilinmeyen kod genel mesaja düşer, kodun kendisi anahtar olmaz', () => {
    expect(messageKeyFor('lead.hic_boyle_bir_kod_yok', ['lead.error.generic'])).toBe(
      'lead.error.generic',
    )
  })

  it('bilinen kod kendi anahtarına eşlenir', () => {
    expect(messageKeyFor('lead.rate_limited', ['lead.error.rate_limited'])).toBe(
      'lead.error.rate_limited',
    )
  })
})
