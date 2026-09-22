import { describe, expect, it } from 'vitest'
import {
  EMPTY_LOGIN_FORM,
  LOCK_HINT_THRESHOLD,
  afterFailedLogin,
  messageKeyFor,
  shouldShowLockHint,
} from './loginFormState'

describe('AC-57: Giriş başarısız olduğunda girilen kimlik kaybolmaz, parola alanı temizlenir', () => {
  it('kimlik kalır, parola temizlenir', () => {
    expect(afterFailedLogin({ loginId: 'ayse@ornek.com', password: 'gizli' })).toEqual({
      loginId: 'ayse@ornek.com',
      password: '',
    })
  })

  it('başlangıç formu boştur', () => {
    expect(EMPTY_LOGIN_FORM).toEqual({ loginId: '', password: '' })
  })
})

describe('AC-59: Hiçbir hata durumunda HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('bilinen kod kendi anahtarına eşlenir', () => {
    expect(messageKeyFor('identity.invalid_credentials', 'login.error', 'tr')).toBe(
      'login.error.invalid_credentials',
    )
  })

  it('bilinmeyen kod genel mesaja düşer; kodun kendisi anahtar olmaz', () => {
    expect(messageKeyFor('identity.hic_boyle_bir_kod_yok', 'login.error', 'tr')).toBe(
      'login.error.generic',
    )
  })

  it('ağ hatası da genel mesaja düşer', () => {
    expect(messageKeyFor('api.unavailable', 'login.error', 'tr')).toBe('login.error.generic')
  })
})

describe('AC-58: Hesap kilitlendiğinde ekran, ne olduğunu ve ne zaman tekrar denenebileceğini söyler', () => {
  it('eşiğin altında ipucu gösterilmez', () => {
    expect(shouldShowLockHint(LOCK_HINT_THRESHOLD - 1)).toBe(false)
  })

  it('eşikte ipucu gösterilir', () => {
    expect(shouldShowLockHint(LOCK_HINT_THRESHOLD)).toBe(true)
  })
})
