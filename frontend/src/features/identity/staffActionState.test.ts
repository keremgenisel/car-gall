import { describe, expect, it } from 'vitest'
import { ACTION_ERROR_CODES, passwordFormError, rejectionFor } from './staffActionState'

/**
 * Üç red yolu saf mantıkta ölçülüyor: bileşen testi aynı şeyi ölçebilirdi ama her vaka
 * için bir render, bir tıklama ve bir sahte istemci gerektirirdi — ve karar tablosunun
 * kendisi görünmez kalırdı.
 */
describe('0008/AC-18: `identity.last_active_manager` döndüğünde ekran kural gereği red metnini gösterir ve tekrar denemeyi önermez', () => {
  it('kural gereği red tekrar denemeyi ÖNERMEZ', () => {
    const red = rejectionFor('deactivate', 'identity.last_active_manager', 409, 'tr')

    expect(red.retryable).toBe(false)
    expect(red.messageKey).toBe('staff.deactivate.error.last_active_manager')
  })

  it('rol düşürmede de aynı yol izlenir, kendi önekiyle', () => {
    const red = rejectionFor('role', 'identity.last_active_manager', 409, 'tr')

    expect(red.retryable).toBe(false)
    expect(red.messageKey).toBe('staff.role.error.last_active_manager')
  })
})

describe('0008/AC-19: `concurrency.lost` döndüğünde ekran eşzamanlılık metnini gösterir, tekrar denemeyi önerir ve eylem tekrar denenebilir kalır', () => {
  it('eşzamanlılık kaybı tekrar denemeyi ÖNERİR', () => {
    const red = rejectionFor('deactivate', 'concurrency.lost', 409, 'tr')

    expect(red.retryable).toBe(true)
    expect(red.messageKey).toBe('staff.deactivate.error.concurrency_lost')
  })
})

describe('0008/AC-24: Kural gereği reddedilen bir işlemden sonra liste tazelenmez; seçim ve kaydırma konumu korunur, liste başa sarmaz', () => {
  it('kural gereği red listeyi TAZELEMEZ', () => {
    expect(rejectionFor('deactivate', 'identity.last_active_manager', 409, 'tr').refresh).toBe(false)
  })
})

describe('0008/AC-25: Eşzamanlılık kaybında liste tazelenir, hata mesajı görünür kalır ve seçim ile odak korunur', () => {
  it('eşzamanlılık kaybı listeyi TAZELER', () => {
    expect(rejectionFor('role', 'concurrency.lost', 409, 'tr').refresh).toBe(true)
  })

  /** İki red aynı durum kodunu (409) paylaşıyor; ayrım YALNIZCA hata kodundan geliyor. */
  it('aynı durum kodunda bile iki yol farklı davranır', () => {
    const kural = rejectionFor('deactivate', 'identity.last_active_manager', 409, 'tr')
    const kayip = rejectionFor('deactivate', 'concurrency.lost', 409, 'tr')

    expect(kural.refresh).not.toBe(kayip.refresh)
    expect(kural.retryable).not.toBe(kayip.retryable)
    expect(kural.messageKey).not.toBe(kayip.messageKey)
  })
})

describe('0008/AC-21: `409` dışında bir hata geldiğinde ekran genel hata mesajı ve tekrar dene eylemi gösterir', () => {
  it('bilinmeyen kod genel mesaja düşer ve tekrar denenebilir', () => {
    const red = rejectionFor('deactivate', 'api.unavailable', 500, 'tr')

    expect(red.messageKey).toBe('staff.deactivate.error.generic')
    expect(red.retryable).toBe(true)
    expect(red.refresh).toBe(false)
  })

  it('404 de genel yola düşer', () => {
    expect(rejectionFor('role', 'identity.staff_not_found', 404, 'tr').messageKey).toBe(
      'staff.role.error.generic',
    )
  })
})

describe('0008/AC-22: Oturum düştüğünde (`401`) ziyaretçi giriş ekranına yönlendirilir', () => {
  it('401 oturum düştü işaretini taşır', () => {
    expect(rejectionFor('deactivate', 'identity.no_session', 401, 'tr').signedOut).toBe(true)
  })

  it('401 dışındaki hiçbir yol oturumu düşürmez', () => {
    for (const [kod, durum] of [
      ['identity.last_active_manager', 409],
      ['concurrency.lost', 409],
      ['identity.already_deactivated', 422],
      ['api.unavailable', 500],
    ] as const) {
      expect(rejectionFor('deactivate', kod, durum, 'tr').signedOut, kod).toBe(false)
    }
  })
})

describe('0008/AC-33: Aynı personel başka biri tarafından zaten pasifleştirilmişken gelen istek kendi mesajını gösterir, tekrar denemeyi önermez ve liste tazelenir', () => {
  it('zaten pasifleştirilmiş: tazele, tekrar deneme', () => {
    const red = rejectionFor('deactivate', 'identity.already_deactivated', 422, 'tr')

    expect(red.messageKey).toBe('staff.deactivate.error.already_deactivated')
    expect(red.refresh).toBe(true)
    expect(red.retryable).toBe(false)
  })
})

describe('0008/AC-12: İki parola alanı eşleşmediğinde istek gönderilmez ve hata alanda gösterilir', () => {
  it('eşleşmeyen parolalar hata döndürür', () => {
    expect(passwordFormError('yeni-parola-1234', 'baska-parola-1234')).toBe(
      'staff.password.error.mismatch',
    )
  })

  it('eşleşen parolalar hata döndürmez', () => {
    expect(passwordFormError('yeni-parola-1234', 'yeni-parola-1234')).toBeNull()
  })

  it('boş parola hata döndürür', () => {
    expect(passwordFormError('', '')).toBe('staff.password.error.required')
  })

  /**
   * Asgari uzunluk YAPILANDIRMADAN gelir ve yalnız sunucu bilir (0006 AC-36). Uçta ikinci
   * bir sayı tutmak, yapılandırma değiştiğinde sessizce yanlış olurdu — bu yüzden kısa ama
   * eşleşen bir parola uçta GEÇER ve reddi sunucu verir (AC-14).
   */
  it('uzunluk kontrolü uçta YAPILMAZ', () => {
    expect(passwordFormError('kisa', 'kisa')).toBeNull()
  })
})

describe('0008/AC-27: Üç eylemin her biri için anahtarlar kataloglarda bulunur', () => {
  /** Ulaşılamayan koda çeviri yazmak, ölü kodun katalog hâlidir. */
  it('parola sıfırlamada `concurrency_lost` BULUNMAZ — o uç kodu üretemez', () => {
    expect(ACTION_ERROR_CODES.password).not.toContain('concurrency_lost')
    expect(ACTION_ERROR_CODES.deactivate).toContain('concurrency_lost')
    expect(ACTION_ERROR_CODES.role).toContain('concurrency_lost')
  })

  it('her eylemde `generic` vardır: bilinmeyen kod ona düşer', () => {
    for (const kodlar of Object.values(ACTION_ERROR_CODES)) {
      expect(kodlar).toContain('generic')
    }
  })
})
