import { describe, expect, it } from 'vitest'
import {
  CODE_LENGTH,
  disableFormError,
  isPendingExpired,
  statusBadgeKey,
  twoFactorInputError,
  twoFactorMessageKey,
} from './twoFactorState'

describe('0013/AC-21: Yanlış kod `401` ve `identity.two_factor_invalid` ile reddedilir', () => {
  it('biçimi bozuk değer UÇTA yakalanır; istek gönderilmez', () => {
    expect(twoFactorInputError('code', '')).toBe('login.twoFactor.error.required')
    expect(twoFactorInputError('code', '   ')).toBe('login.twoFactor.error.required')
    expect(twoFactorInputError('code', '12345')).toBe('login.twoFactor.error.format')
    expect(twoFactorInputError('code', '1234567')).toBe('login.twoFactor.error.format')
    expect(twoFactorInputError('code', '12a456')).toBe('login.twoFactor.error.format')
  })

  it('altı hane kabul edilir ve boşluklar kırpılır', () => {
    expect(twoFactorInputError('code', '123456')).toBeNull()
    expect(twoFactorInputError('code', ' 123456 ')).toBeNull()
    expect(CODE_LENGTH).toBe(6)
  })

  /**
   * Kurtarma kodu altı hane DEĞİL: gruplanmış bir dize. Tek kurala sıkıştırmak,
   * personelin elindeki geçerli kodu "biçimi bozuk" diye reddetmek olurdu.
   */
  it('kurtarma kodunda altı hane kuralı uygulanmaz', () => {
    expect(twoFactorInputError('recovery', 'ABCD-EFGH-JKLM-NPQR')).toBeNull()
    expect(twoFactorInputError('recovery', '')).toBe('login.twoFactor.error.required')
  })
})

describe('0013/AC-14: Ara durum yapılandırılan süre sonunda düşer; düşmüş ara durumla yapılan doğrulama `identity.two_factor_pending_expired` döner', () => {
  it('iki red kodu ayrılır: biri parola fazına döner, öteki taze kod ister', () => {
    expect(isPendingExpired('identity.two_factor_pending_expired')).toBe(true)
    expect(isPendingExpired('identity.two_factor_invalid')).toBe(false)
    expect(isPendingExpired('api.unavailable')).toBe(false)
  })
})

describe('0013/AC-41: Opsiyonel roldeki personel ikinci adımı **mevcut parolası ve geçerli bir kodla** kapatır; ikisinden biri eksik veya yanlışsa kapatma reddedilir', () => {
  it('iki alandan biri eksikse istek gönderilmez', () => {
    expect(disableFormError('', '123456')).toBe('twoFactor.disable.error.password')
    expect(disableFormError('parola', '')).toBe('twoFactor.disable.error.code')
    expect(disableFormError('parola', '123')).toBe('twoFactor.disable.error.code')
    expect(disableFormError('parola', '123456')).toBeNull()
  })
})

describe('0013/AC-54: Yönetici personel listesinde her satırın ikinci adım durumunu **rozetle** görür', () => {
  /**
   * Üç hâl İKİ alandan türüyor (D25): kurulum durumu satırda, zorunluluk ROLDE.
   * Sözleşmeye üçüncü bir alan eklemek politikayı API'ye dondururdu.
   */
  it('rozet üç hâli iki alandan kurar', () => {
    expect(statusBadgeKey(true, true)).toBe('twoFactor.status.enrolled')
    expect(statusBadgeKey(true, false)).toBe('twoFactor.status.enrolled')
    expect(statusBadgeKey(false, true)).toBe('twoFactor.status.missing')
    expect(statusBadgeKey(false, false)).toBe('twoFactor.status.optional')
  })
})

describe('0013/AC-51: Doğrulama sırrı ve kurtarma kodu hiçbir hata yanıtında bulunmaz', () => {
  it('bilinmeyen kod GENEL mesaja düşer; ham kod anahtara girmez', () => {
    expect(twoFactorMessageKey('identity.two_factor_invalid', 'twoFactor.error', 'tr')).toBe(
      'twoFactor.error.two_factor_invalid',
    )
    expect(twoFactorMessageKey('identity.bilinmeyen_kod', 'twoFactor.error', 'tr')).toBe(
      'twoFactor.error.generic',
    )
    expect(twoFactorMessageKey('api.unavailable', 'twoFactor.error', 'tr')).toBe(
      'twoFactor.error.generic',
    )
  })
})
