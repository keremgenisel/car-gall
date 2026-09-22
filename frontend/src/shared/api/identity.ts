import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'
import type { AddStaffRequest, CurrentSession, LoginRequest, StaffListItem } from './types'

/**
 * Endpoint yolları YALNIZCA burada yazılır; bileşende URL dizesi bulunmaz
 * (docs/frontend.md § 2).
 *
 * Yönetim kökü ile giriş kökü AYRIDIR ve bu bilinçlidir: giriş oturumsuz çalışmak
 * zorunda, oysa yönetim kökünün altındaki her uç oturum ister (0006 C18).
 */
const SESSIONS = '/api/identity/sessions'
const ADMIN = '/api/admin'

export function login(request: LoginRequest, locale: AppLocale): Promise<CurrentSession | null> {
  return sendJson<CurrentSession>('POST', SESSIONS, request, locale)
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
