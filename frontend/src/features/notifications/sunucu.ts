import { vi } from 'vitest'

/**
 * `0018` birim testlerinin sahte sunucusu. **Gövdeler sunucunun GERÇEK biçimidir**
 * (`NewsletterEndpoints`, `NewsletterProblems`, `NewsletterRateLimitPolicy`): `errorCode`,
 * `field` ve `application/problem+json`. İstemci (`shared/api/client`) GERÇEKTİR; yalnız ağ
 * sahtedir — sahte bir API modülü, istemcinin gövdeyi ayrıştırma adımını hiç çalıştırmazdı.
 */
export const SUNUCU = {
  kabul: { status: 202, body: { status: 'accepted' }, type: 'application/json' },
  adresGecersiz: {
    status: 400,
    body: {
      type: 'about:blank',
      status: 400,
      errorCode: 'notification.subscription_email_invalid',
      field: 'email',
    },
    type: 'application/problem+json',
  },
  hizSiniri: {
    status: 429,
    body: { type: 'about:blank', status: 429, errorCode: 'notification.subscription_rate_limited' },
    type: 'application/problem+json',
  },
  baglantiGecersiz: {
    status: 404,
    body: { type: 'about:blank', status: 404, errorCode: 'notification.subscription_link_invalid' },
    type: 'application/problem+json',
  },
  teyit: { status: 200, body: { status: 'confirmed' }, type: 'application/json' },
  cikis: { status: 200, body: { status: 'unsubscribed' }, type: 'application/json' },
  hata: {
    status: 500,
    body: { type: 'about:blank', status: 500, traceId: '00-abc-def-01' },
    type: 'application/problem+json',
  },
} as const

export type Cevap = (typeof SUNUCU)[keyof typeof SUNUCU]

export function yanit(cevap: Cevap): Response {
  return new Response(JSON.stringify(cevap.body), {
    status: cevap.status,
    headers: { 'Content-Type': cevap.type },
  })
}

/** `fetch`'i sırayla verilen cevaplarla değiştirir; çağrıları döner. */
export function sahteFetch(...cevaplar: (Cevap | 'ag' | 'bekle')[]) {
  const fetchMock = vi.fn((): Promise<Response> => {
    const sira = cevaplar.length > 1 ? cevaplar.shift()! : cevaplar[0]!

    if (sira === 'ag') {
      return Promise.reject(new TypeError('Failed to fetch'))
    }

    if (sira === 'bekle') {
      return new Promise<Response>(() => {})
    }

    return Promise.resolve(yanit(sira))
  })

  vi.stubGlobal('fetch', fetchMock)

  return fetchMock
}
