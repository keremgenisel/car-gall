import type { AppLocale } from '../lib/locale'
import { sendJson } from './client'

/**
 * `0018` — bülten aboneliğinin üç ucu. Endpoint yolu YALNIZCA burada yazılır
 * (docs/frontend.md § 2). Cevap gövdesi bilerek okunmaz: abonelik isteğinin cevabı her
 * durumda aynıdır (**D5**) ve uç adresin durumu hakkında hiçbir şey bilmez.
 */
const KOK = '/api/notifications/newsletter'

export async function subscribeToNewsletter(email: string, locale: AppLocale): Promise<void> {
  await sendJson('POST', `${KOK}/subscriptions`, { email, locale }, locale)
}

/** **D6** — teyit yalnız sayfadaki düğmeyle; sayfanın açılması teyit değildir. */
export async function confirmNewsletter(token: string, locale: AppLocale): Promise<void> {
  await sendJson('POST', `${KOK}/confirmations`, { token }, locale)
}

/** **D6** — çıkış bağlantının açılmasıyla; düğme ya da onay yok. */
export async function unsubscribeFromNewsletter(
  token: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<void> {
  await sendJson('POST', `${KOK}/unsubscriptions`, { token }, locale, signal)
}

/** **D7** · **D8** — geçersiz bağlantının TEK kodu; uç onu "geçerli değil" ekranına çevirir. */
export const LINK_INVALID = 'notification.subscription_link_invalid'
