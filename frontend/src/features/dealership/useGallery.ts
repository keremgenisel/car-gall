import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchGallery } from '../../shared/api/dealership'
import type { PublicGallery } from '../../shared/api/types'

export type GalleryLoad =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly gallery: PublicGallery }

/**
 * 0014 — galeri adı ve şube bloğu TEK okumadan beslenir. Önbellek yok: ad değiştiğinde
 * bir sonraki yükleme yeni adı gösterir (AC-13).
 */
export function useGallery(): GalleryLoad {
  const { locale } = useLocale()
  const [result, setResult] = useState<{
    readonly key: string
    readonly value: GalleryLoad
  } | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    fetchGallery(locale, controller.signal)
      .then((gallery) => setResult({ key: locale, value: { kind: 'ready', gallery } }))
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setResult({ key: locale, value: { kind: 'error' } })
        }
      })

    return () => controller.abort()
  }, [locale])

  return result?.key === locale ? result.value : { kind: 'loading' }
}
