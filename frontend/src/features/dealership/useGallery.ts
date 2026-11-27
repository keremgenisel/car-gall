import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchGallery } from '../../shared/api/dealership'
import type { PublicGallery } from '../../shared/api/types'

export type GalleryLoad =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly gallery: PublicGallery
      /** `0027` D9 — yanıtın alındığı monoton an (`performance.now()`); süre buradan ölçülür. */
      readonly receivedAt?: number
    }

/**
 * 0014 — galeri adı ve şube bloğu TEK okumadan beslenir. Önbellek yok: ad değiştiğinde
 * bir sonraki yükleme yeni adı gösterir (AC-13).
 *
 * `0028` **D4** — `attempt` artınca okuma tekrarlanır: iletişim sayfasının "Tekrar dene"si (AC-39).
 * Ana sayfa vermez; okuma bir kez yapılır.
 */
export function useGallery(attempt = 0): GalleryLoad {
  const { locale } = useLocale()
  const [result, setResult] = useState<{
    readonly key: string
    readonly value: GalleryLoad
  } | null>(null)

  const key = `${locale}|${attempt}`

  useEffect(() => {
    const controller = new AbortController()

    fetchGallery(locale, controller.signal)
      .then((gallery) =>
        setResult({
          key,
          value: { kind: 'ready', gallery, receivedAt: performance.now() },
        }),
      )
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setResult({ key, value: { kind: 'error' } })
        }
      })

    return () => controller.abort()
  }, [key, locale])

  return result?.key === key ? result.value : { kind: 'loading' }
}
