import type { AppLocale } from '../lib/locale'
import { postJson } from './client'
import type { CaptureLeadRequest, CaptureLeadResponse } from './types'

/**
 * Endpoint yolu YALNIZCA burada yazılır; bileşende URL dizesi bulunmaz
 * (docs/frontend.md § 2).
 */
export function captureLead(
  request: CaptureLeadRequest,
  locale: AppLocale,
  idempotencyKey: string,
  signal?: AbortSignal,
): Promise<CaptureLeadResponse> {
  return postJson<CaptureLeadResponse>('/api/leads', request, locale, idempotencyKey, signal)
}
