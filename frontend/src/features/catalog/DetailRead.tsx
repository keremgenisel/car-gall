import { useEffect } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchListingDetail } from '../../shared/api/catalog'
import type { DetailResult } from './detailReads'

/**
 * Bir slug'ın detay okuması; ekrana bir şey basmaz (`useDetailReads`). İptal edilen okuma sonuç
 * yazmaz: StrictMode'un ilk etkisi iptal edilir, ikinci etki yeniden okur.
 */
export function DetailRead({
  slug,
  onResult,
}: {
  readonly slug: string
  readonly onResult: (slug: string, result: DetailResult) => void
}) {
  const { locale } = useLocale()

  useEffect(() => {
    const controller = new AbortController()

    fetchListingDetail(slug, locale, controller.signal).then(
      (detail) => onResult(slug, detail ?? 'gone'),
      (cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          onResult(slug, 'error')
        }
      },
    )

    return () => controller.abort()
  }, [slug, locale, onResult])

  return null
}
