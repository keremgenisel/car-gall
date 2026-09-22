import { catalogKeys } from '../../shared/i18n/i18n'
import type { AppLocale } from '../../shared/lib/locale'

/**
 * Giriş ekranının saf mantığı. Yan etkisizdir ve kendi birim testi vardır
 * (docs/frontend.md § 8.1).
 */

export interface LoginFormValues {
  readonly loginId: string
  readonly password: string
}

export const EMPTY_LOGIN_FORM: LoginFormValues = { loginId: '', password: '' }

/**
 * Hata kodunu çeviri anahtarına çevirir. Bilinmeyen kod için GENEL mesaja düşülür ve
 * kodun kendisi ziyaretçiye ASLA gösterilmez (docs/frontend.md § 3.1, AC-59).
 */
export function messageKeyFor(errorCode: string, prefix: string, locale: AppLocale): string {
  const candidate = `${prefix}.${errorCode.replace(/^identity\./, '')}`

  return catalogKeys(locale).includes(candidate) ? candidate : `${prefix}.generic`
}

/**
 * AC-57 — giriş başarısız olduğunda girilen kimlik KAYBOLMAZ, parola alanı temizlenir.
 * Parolanın ekranda kalması, paylaşılan bir ekranda onu görünür bırakırdı.
 */
export function afterFailedLogin(values: LoginFormValues): LoginFormValues {
  return { loginId: values.loginId, password: '' }
}

/**
 * AC-58 — kilitlenme ekranı SUNUCUDAN gelen bir sinyale dayanamaz: sunucu kilitlenmeyi
 * söylemez, çünkü söylerse hangi e-postaların kayıtlı olduğu sızar (R3, AC-8, Tuzak 7).
 * Uç bu yüzden KENDİ deneme sayacını tutar ve eşiğe ulaşınca metni gösterir.
 *
 * Bu, kilitlenmenin gerçekten olduğunu KANITLAMAZ — yalnız ziyaretçiye ne olduğunu ve
 * ne yapabileceğini söyler. Ayırt edicilik sunucuda yoktur ve burada da yoktur.
 */
export const LOCK_HINT_THRESHOLD = 5

export function shouldShowLockHint(failedAttempts: number): boolean {
  return failedAttempts >= LOCK_HINT_THRESHOLD
}
