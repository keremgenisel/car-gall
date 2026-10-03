import type { AppLocale } from '../../shared/lib/locale'
import { messageKeyFor } from './loginFormState'

/**
 * Personel eylemlerinin saf mantığı (docs/frontend.md § 8.1). Yan etkisizdir ve kendi
 * birim testi vardır.
 *
 * Bu spec'in ana davranışı — üç red yolunun ayrımı — bileşende değil BURADA yaşıyor:
 * bileşen testi aynı şeyi ölçebilirdi ama daha pahalı ve daha bulanık olurdu. Karar
 * tablosu bir veri yapısı olduğunda okunabiliyor.
 */

/** Her eylem kendi çeviri önekini taşır: `messageKeyFor` önek bazlı çalışır. */
export const ACTION_PREFIX = {
  deactivate: 'staff.deactivate.error',
  role: 'staff.role.error',
  password: 'staff.password.error',
} as const

export type StaffAction = keyof typeof ACTION_PREFIX

/**
 * Eylem başına GERÇEKTEN dönebilen hata kodları (0008 C5).
 *
 * Liste ulaşılabilirliğe göre kuruldu, "her eyleme aynı üç anahtar" diye değil. Parola
 * sıfırlamada `concurrency.lost` YOKTUR: o uç `InvariantGuard`'dan geçmiyor ve kodu hiç
 * üretemiyor. Buna karşılık `password_too_short` oradadır, çünkü AC-14 onu ziyaretçinin
 * dilinde istiyor.
 *
 * `generic` her eylemde var: `messageKeyFor` bilinmeyen kodda ona düşer.
 */
export const ACTION_ERROR_CODES: Readonly<Record<StaffAction, readonly string[]>> = {
  deactivate: ['last_active_manager', 'already_deactivated', 'concurrency_lost', 'generic'],
  role: ['last_active_manager', 'concurrency_lost', 'generic'],
  password: ['password_too_short', 'generic'],
}

/**
 * Bir reddin ekrana yansıması.
 *
 * `refresh` ve `retryable` ayrı iki sorudur ve birlikte hareket etmezler:
 * `already_deactivated`'da liste tazelenir (ekrandaki veri bayat) ama tekrar denemek
 * anlamsızdır (işlem zaten olmuş).
 */
export interface ActionRejection {
  /** Çeviri kataloğu anahtarı; ham hata kodu ziyaretçiye ASLA gösterilmez. */
  readonly messageKey: string
  /** Liste yeniden çekilsin mi? */
  readonly refresh: boolean
  /** Ziyaretçiye tekrar denemesi önerilsin mi? */
  readonly retryable: boolean
  /** Oturum düştü; giriş ekranına gidilir (0006/AC-24). */
  readonly signedOut: boolean
}

const LAST_ACTIVE_MANAGER = 'identity.last_active_manager'
const ALREADY_DEACTIVATED = 'identity.already_deactivated'
const CONCURRENCY_LOST = 'concurrency.lost'
const NO_SESSION = 'identity.no_session'

/**
 * Üç red yolu (R5) + AC-33.
 *
 * | Kod | Tazele | Tekrar dene | Neden |
 * |---|---|---|---|
 * | `last_active_manager` | hayır | **hayır** | Kural KALICI; tekrar denemek asla işe yaramaz ve ziyaretçinin konumu korunur (AC-24). |
 * | `concurrency.lost` | **evet** | evet | Karara giren sayı değişti; ekrandaki liste artık bayat (AC-25). |
 * | `already_deactivated` | **evet** | hayır | İşlem zaten olmuş; veri bayat ama tekrar denemek anlamsız (AC-33). |
 * | diğer | hayır | evet | Ne olduğunu bilmiyoruz; genel mesaj + tekrar dene (AC-21). |
 *
 * `401` ayrı bir yol: oturum yoksa gösterilecek ekran da yok.
 */
export function rejectionFor(
  action: StaffAction,
  errorCode: string,
  status: number | null,
  locale: AppLocale,
): ActionRejection {
  const prefix = ACTION_PREFIX[action]
  const messageKey = messageKeyFor(errorCode, prefix, locale)

  if (status === 401 || errorCode === NO_SESSION) {
    return { messageKey, refresh: false, retryable: false, signedOut: true }
  }

  if (errorCode === LAST_ACTIVE_MANAGER) {
    return { messageKey, refresh: false, retryable: false, signedOut: false }
  }

  if (errorCode === CONCURRENCY_LOST) {
    return { messageKey, refresh: true, retryable: true, signedOut: false }
  }

  if (errorCode === ALREADY_DEACTIVATED) {
    return { messageKey, refresh: true, retryable: false, signedOut: false }
  }

  return { messageKey, refresh: false, retryable: true, signedOut: false }
}

/**
 * AC-12 — iki parola eşleşmeden istek gönderilmez. Uzunluk kontrolü BURADA YAPILMAZ:
 * asgari uzunluk yapılandırmadan gelir ve yalnız sunucu bilir (0006 AC-36). Uçta
 * ikinci bir sayı tutmak, yapılandırma değiştiğinde sessizce yanlış olurdu.
 */
export function passwordFormError(newPassword: string, confirmation: string): string | null {
  if (newPassword.length === 0) {
    return 'staff.password.error.required'
  }

  return newPassword === confirmation ? null : 'staff.password.error.mismatch'
}
