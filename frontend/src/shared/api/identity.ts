import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'
import type {
  AddStaffRequest,
  CurrentSession,
  LoginOutcome,
  LoginRequest,
  RecoveryCodes,
  StaffListItem,
  TwoFactorEnrollmentStart,
  TwoFactorStatus,
} from './types'

/**
 * Endpoint yolları YALNIZCA burada yazılır; bileşende URL dizesi bulunmaz
 * (docs/frontend.md § 2).
 *
 * Yönetim kökü ile giriş kökü AYRIDIR ve bu bilinçlidir: giriş oturumsuz çalışmak
 * zorunda, oysa yönetim kökünün altındaki her uç oturum ister (0006 C18).
 */
const SESSIONS = '/api/identity/sessions'
const ADMIN = '/api/admin'

/**
 * 0013 D15 · D43 — yanıt artık ayrıştırıcılı: ya oturum açıldı ya bir adım daha var.
 * İmza bu yüzden `CurrentSession` döndürmüyor; `0006`'nın giriş kriterleri 2FA'sız
 * personel için aynen doğru kalıyor ama tipin kendisi değişti.
 */
export function login(request: LoginRequest, locale: AppLocale): Promise<LoginOutcome | null> {
  return sendJson<LoginOutcome>('POST', SESSIONS, request, locale)
}

/** Girişin ikinci adımı: zamana bağlı kod ya da kurtarma kodu (R1/R5). */
export function verifyTwoFactor(code: string, locale: AppLocale): Promise<LoginOutcome | null> {
  return sendJson<LoginOutcome>('POST', `${SESSIONS}/two-factor`, { code }, locale)
}

/**
 * D14 — *"Kullanıcı değiştir"* ara durumu SİLER. Çerezi yalnız atmak yetmez: satır
 * yaşadığı sürece elinde kopya kalan biri doğrulamayı sürdürebilirdi.
 */
export function discardPendingLogin(locale: AppLocale): Promise<unknown> {
  return sendJson('DELETE', `${SESSIONS}/pending`, undefined, locale)
}

export function logout(locale: AppLocale): Promise<unknown> {
  return sendJson('DELETE', `${SESSIONS}/current`, undefined, locale)
}

export function fetchCurrentSession(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<CurrentSession> {
  return getJson<CurrentSession>(`${ADMIN}/me`, locale, signal)
}

export function changeOwnPassword(
  currentPassword: string,
  newPassword: string,
  locale: AppLocale,
): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/me/password`, { currentPassword, newPassword }, locale)
}

export function fetchTwoFactorStatus(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<TwoFactorStatus> {
  return getJson<TwoFactorStatus>(`${ADMIN}/me/two-factor`, locale, signal)
}

export function startTwoFactorEnrollment(
  locale: AppLocale,
): Promise<TwoFactorEnrollmentStart | null> {
  return sendJson<TwoFactorEnrollmentStart>('POST', `${ADMIN}/me/two-factor`, undefined, locale)
}

export function confirmTwoFactorEnrollment(
  code: string,
  locale: AppLocale,
): Promise<RecoveryCodes | null> {
  return sendJson<RecoveryCodes>('POST', `${ADMIN}/me/two-factor/confirm`, { code }, locale)
}

/** R15/D23 — kapatma mevcut parola **ve** geçerli bir kod ister. */
export function disableTwoFactor(
  currentPassword: string,
  code: string,
  locale: AppLocale,
): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/me/two-factor/disable`, { currentPassword, code }, locale)
}

/** R16/D24 — yenileme kalan kodların hepsini düşürür ve yeni kümeyi bir kez gösterir. */
export function regenerateRecoveryCodes(
  code: string,
  locale: AppLocale,
): Promise<RecoveryCodes | null> {
  return sendJson<RecoveryCodes>('POST', `${ADMIN}/me/two-factor/recovery-codes`, { code }, locale)
}

/** R6/D22 — Yönetici sıfırlar; personelin oturumları düşer ve kodları geçersizleşir. */
export function resetStaffTwoFactor(staffId: string, locale: AppLocale): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/staff/${staffId}/two-factor-reset`, undefined, locale)
}

export function fetchStaff(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly StaffListItem[]> {
  return getJson<readonly StaffListItem[]>(`${ADMIN}/staff`, locale, signal)
}

export function addStaff(request: AddStaffRequest, locale: AppLocale): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/staff`, request, locale)
}

export function changeStaffRole(
  staffId: string,
  role: string,
  locale: AppLocale,
): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/staff/${staffId}/role`, { role }, locale)
}

export function deactivateStaff(staffId: string, locale: AppLocale): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/staff/${staffId}/deactivate`, undefined, locale)
}

/**
 * Parolayı YÖNETİCİ belirler; sistem üretmez (0008 D2). Uç gövdesiz `204` döner —
 * gösterilecek bir parola yoktur ve `0006` C6 parolanın API yanıtında bulunmasını
 * zaten yasaklıyor.
 */
export function resetStaffPassword(
  staffId: string,
  newPassword: string,
  locale: AppLocale,
): Promise<unknown> {
  return sendJson('POST', `${ADMIN}/staff/${staffId}/password-reset`, { newPassword }, locale)
}
