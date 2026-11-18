import type { AppLocale } from '../lib/locale'
import { ApiError, getJson } from './client'

/** `0017` R15 — sayfanın yayında olduğu BAŞKA bir dildeki slug'ı. */
export interface ContentAlternate {
  readonly locale: string
  readonly slug: string
}

/** `0017` R19 — SSS sayfasında o dilde yayındaki girdi. */
export interface ContentFaqItem {
  readonly question: string
  readonly answer: string
}

/**
 * `0017` C4 — ziyaretçiye dönük sayfa: yalnız yayındaki dil. Gövde ham alt kümedir (R16);
 * HTML değildir ve ekranda arındırılarak çizilir (R17, D26).
 */
export interface ContentPage {
  readonly systemKey: string | null
  readonly title: string
  readonly body: string
  readonly alternates: readonly ContentAlternate[]
  readonly faqEntries: readonly ContentFaqItem[]
}

/** `0017` R21/R22 — altlık ve talep formu için sistem sayfası bağlantısı; kimlikle bulunur (C13). */
export interface ContentNavigationLink {
  readonly systemKey: string
  readonly title: string
  readonly slug: string
}

/** Sayfa; o dilde yayında değilse ya da yoksa `null` (R12). */
export async function fetchContentPage(
  slug: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<ContentPage | null> {
  try {
    return await getJson<ContentPage>(
      `/api/content/pages/${encodeURIComponent(slug)}?locale=${locale}`,
      locale,
      signal,
    )
  } catch (cause) {
    if (cause instanceof ApiError && cause.status === 404) {
      return null
    }

    throw cause
  }
}

/** Sistem sayfası bağlantıları, sabit sırada, yalnız o dilde yayında olanlar (R21). */
export function fetchContentNavigation(
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<readonly ContentNavigationLink[]> {
  return getJson<readonly ContentNavigationLink[]>(
    `/api/content/navigation?locale=${locale}`,
    locale,
    signal,
  )
}
