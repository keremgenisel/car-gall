import type { AppLocale } from '../lib/locale'

/**
 * Tüm ağ istekleri bu client'tan geçer; bileşenin içinde fetch çağrısı yasaktır
 * (docs/frontend.md § 2). Endpoint yolları yalnız bu klasörde yazılır.
 */

export class ApiError extends Error {
  readonly errorCode: string
  readonly traceId: string | null

  constructor(errorCode: string, traceId: string | null) {
    // Mesaj yalnız geliştirici içindir; ziyaretçiye ASLA gösterilmez (docs/frontend.md § 4).
    super(errorCode)
    this.name = 'ApiError'
    this.errorCode = errorCode
    this.traceId = traceId
  }
}

const DEFAULT_ERROR_CODE = 'api.unavailable'

export async function getJson<TResponse>(
  path: string,
  locale: AppLocale,
  signal?: AbortSignal,
): Promise<TResponse> {
  let response: Response

  try {
    response = await fetch(path, {
      signal,
      headers: { Accept: 'application/json', 'Accept-Language': locale },
    })
  } catch (cause) {
    if (cause instanceof DOMException && cause.name === 'AbortError') {
      throw cause
    }

    throw new ApiError(DEFAULT_ERROR_CODE, null)
  }

  if (!response.ok) {
    throw await toApiError(response)
  }

  return (await response.json()) as TResponse
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as { errorCode?: string; traceId?: string }
    return new ApiError(problem.errorCode ?? DEFAULT_ERROR_CODE, problem.traceId ?? null)
  } catch {
    // Gövde okunamadıysa da kullanıcıya teknik detay sızmaz; genel kodla devam edilir.
    return new ApiError(DEFAULT_ERROR_CODE, null)
  }
}
