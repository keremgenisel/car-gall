import type { AppLocale } from '../lib/locale'
import { getJson, sendJson } from './client'

const ROOT = '/api/admin/content'

/** Yayın durumu KODU (C3). */
export type PublicationStatus = 'draft' | 'published'

/** Panelin bir sayfanın bir dildeki metni. */
export interface AdminPageLocale {
  readonly locale: string
  readonly title: string
  readonly body: string
  readonly slug: string
  readonly status: PublicationStatus
}

/** `0017` — panelin sayfası; `version` iyimser eşzamanlılık içindir (R10). */
export interface AdminPage {
  readonly id: string
  readonly systemKey: string | null
  readonly version: number
  readonly locales: readonly AdminPageLocale[]
  readonly missingLocales: readonly string[]
}

/** Panelin SSS girdisinin bir dildeki metni. */
export interface AdminFaqLocale {
  readonly locale: string
  readonly question: string
  readonly answer: string
  readonly status: PublicationStatus
}

/** `0017` — panelin SSS girdisi; liste global sırayla gelir (R18). */
export interface AdminFaqEntry {
  readonly id: string
  readonly version: number
  readonly locales: readonly AdminFaqLocale[]
  readonly missingLocales: readonly string[]
}

/** Bir dilin sayfa metni girdisi. */
export interface PageTextInput {
  readonly locale: string
  readonly title: string
  readonly body: string
  readonly slug: string
}

/** Bir dilin SSS metni girdisi. */
export interface FaqTextInput {
  readonly locale: string
  readonly question: string
  readonly answer: string
}

export function fetchAdminPages(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly AdminPage[]> {
  return getJson<readonly AdminPage[]>(`${ROOT}/pages`, locale, signal)
}

export function fetchAdminPage(
  id: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<AdminPage> {
  return getJson<AdminPage>(`${ROOT}/pages/${encodeURIComponent(id)}`, locale, signal)
}

export async function createPage(
  texts: readonly PageTextInput[],
  locale: AppLocale,
): Promise<string> {
  const created = await sendJson<{ readonly id: string }>(
    'POST',
    `${ROOT}/pages`,
    { locales: texts },
    locale,
  )
  return created?.id ?? ''
}

/** Kaydeder; yeni sürümü döner. Eski sürümle `409` (R10). */
export async function savePage(
  id: string,
  version: number,
  texts: readonly PageTextInput[],
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/pages/${encodeURIComponent(id)}`,
    { version, locales: texts },
    locale,
  )
  return result?.version ?? version
}

/** Dili yayına alır ya da yayından kaldırır; yeni sürümü döner. */
export async function setPagePublication(
  id: string,
  version: number,
  pageLocale: string,
  publish: boolean,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'POST',
    `${ROOT}/pages/${encodeURIComponent(id)}/${publish ? 'publish' : 'unpublish'}`,
    { version, locale: pageLocale },
    locale,
  )
  return result?.version ?? version
}

export async function deletePage(id: string, version: number, locale: AppLocale): Promise<void> {
  await sendJson<null>(
    'DELETE',
    `${ROOT}/pages/${encodeURIComponent(id)}?version=${version}`,
    undefined,
    locale,
  )
}

export function fetchAdminFaq(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly AdminFaqEntry[]> {
  return getJson<readonly AdminFaqEntry[]>(`${ROOT}/faq`, locale, signal)
}

export async function createFaqEntry(
  texts: readonly FaqTextInput[],
  locale: AppLocale,
): Promise<void> {
  await sendJson<null>('POST', `${ROOT}/faq`, { locales: texts }, locale)
}

export async function saveFaqEntry(
  id: string,
  version: number,
  texts: readonly FaqTextInput[],
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'PUT',
    `${ROOT}/faq/${encodeURIComponent(id)}`,
    { version, locales: texts },
    locale,
  )
  return result?.version ?? version
}

export async function setFaqPublication(
  id: string,
  version: number,
  entryLocale: string,
  publish: boolean,
  locale: AppLocale,
): Promise<number> {
  const result = await sendJson<{ readonly version: number }>(
    'POST',
    `${ROOT}/faq/${encodeURIComponent(id)}/${publish ? 'publish' : 'unpublish'}`,
    { version, locale: entryLocale },
    locale,
  )
  return result?.version ?? version
}

export async function deleteFaqEntry(
  id: string,
  version: number,
  locale: AppLocale,
): Promise<void> {
  await sendJson<null>(
    'DELETE',
    `${ROOT}/faq/${encodeURIComponent(id)}?version=${version}`,
    undefined,
    locale,
  )
}

export async function moveFaqEntry(
  id: string,
  direction: 'up' | 'down',
  locale: AppLocale,
): Promise<void> {
  await sendJson<null>('POST', `${ROOT}/faq/${encodeURIComponent(id)}/move`, { direction }, locale)
}
