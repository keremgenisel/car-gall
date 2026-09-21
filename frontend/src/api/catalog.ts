import type { AppLocale } from '../lib/locale'
import { getJson } from './client'
import type { BodyType, FieldSettings, PagedResult, ShowcaseListing } from './types'

export interface ShowcaseQuery {
  readonly bodyTypes: readonly string[]
  readonly page: number
}

export function fetchShowcaseListings(
  query: ShowcaseQuery,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<PagedResult<ShowcaseListing>> {
  const params = new URLSearchParams()

  for (const bodyType of query.bodyTypes) {
    params.append('bodyType', bodyType)
  }

  if (query.page > 1) {
    params.set('page', String(query.page))
  }

  params.set('locale', locale)

  return getJson<PagedResult<ShowcaseListing>>(
    `/api/catalog/showcase-listings?${params.toString()}`,
    locale,
    signal,
  )
}

export function fetchBodyTypes(locale: AppLocale, signal?: AbortSignal): Promise<readonly BodyType[]> {
  return getJson<readonly BodyType[]>(`/api/reference/body-types?locale=${locale}`, locale, signal)
}

export function fetchFieldSettings(locale: AppLocale, signal?: AbortSignal): Promise<FieldSettings> {
  return getJson<FieldSettings>('/api/catalog/field-settings', locale, signal)
}
