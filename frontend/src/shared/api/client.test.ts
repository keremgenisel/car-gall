import { afterEach, describe, expect, it, vi } from 'vitest'
import { ApiError, sendJson } from './client'

/**
 * `sendJson`'ın gövde okuma sözleşmesi. Burada `fetch` sahteleniyor ama **gerçek**
 * `Response` nesneleri dönüyor: kusur tam olarak `Response.json()`'ın boş gövdedeki
 * davranışıydı ve onu taklit eden bir sahte, kusuru da taklit etmezdi.
 */
function yanit(status: number, govde?: string, contentType?: string): Response {
  return new Response(govde, {
    status,
    headers: contentType === undefined ? undefined : { 'Content-Type': contentType },
  })
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('sendJson — gövdesiz 2xx', () => {
  /**
   * 0012a/AC-17'nin kaçan kusuru: tekrar deneme ucu `202 Accepted` + boş gövde dönüyor
   * ve istemci onu JSON diye okumaya çalışıyordu. Sunucu işi kabul etmiş olmasına
   * rağmen personele "İşlem şu anda tamamlanamadı" basılıyordu.
   */
  it('202 + boş gövde istisna ATMAZ, `null` döner', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(yanit(202)))

    await expect(
      sendJson('POST', '/api/admin/media/assets/abc/retry', undefined, 'tr'),
    ).resolves.toBeNull()
  })

  it('204 yine `null` döner — eski davranış korunuyor', async () => {
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(yanit(204)))

    await expect(
      sendJson('PUT', '/api/admin/vehicles/abc/media', { a: 1 }, 'tr'),
    ).resolves.toBeNull()
  })

  it('`Content-Length: 0` taşıyan 200 de gövdesiz sayılır', async () => {
    const bos = new Response(null, { status: 200, headers: { 'Content-Length': '0' } })

    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(bos))

    await expect(sendJson('POST', '/api/admin/bir-sey', undefined, 'tr')).resolves.toBeNull()
  })

  it('JSON gövdesi VARSA okunur — kontrol gövdeyi yutmuyor', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(yanit(202, '{"id":"abc"}', 'application/json')),
    )

    await expect(sendJson('POST', '/api/admin/media/assets', undefined, 'tr')).resolves.toEqual({
      id: 'abc',
    })
  })

  it('hata yolu değişmedi: `errorCode` taşıyan 409 `ApiError` atar', async () => {
    vi.stubGlobal(
      'fetch',
      vi
        .fn()
        .mockResolvedValue(
          yanit(409, '{"errorCode":"media.not_retryable"}', 'application/problem+json'),
        ),
    )

    await expect(
      sendJson('POST', '/api/admin/media/assets/abc/retry', undefined, 'tr'),
    ).rejects.toMatchObject({ errorCode: 'media.not_retryable', status: 409 })

    await expect(
      sendJson('POST', '/api/admin/media/assets/abc/retry', undefined, 'tr'),
    ).rejects.toBeInstanceOf(ApiError)
  })
})
