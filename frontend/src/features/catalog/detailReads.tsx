import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import { DetailRead } from './DetailRead'

/**
 * Bir slug'ın bugünkü okuması: görünür ilan, vitrinden düşmüş (404), okunamamış ya da yeniden
 * deneniyor. `retrying` yalnız "tekrar dene"den sonra olur; ilk yükleme `undefined`'dır.
 */
export type DetailResult = ShowcaseListingDetail | 'gone' | 'error' | 'retrying'

/**
 * `frontend-003` QA 2 BULGU-1 · BULGU-5 — favoriler ve karşılaştırma ekranının detay okumaları.
 * Her slug'ı KENDİ okuyucusu okur; okuyucunun anahtarı yeniden deneme sayacını taşır:
 *
 * - React'in bağla/temizle sırası her okumayı kendi `AbortController`'ıyla yönetir. StrictMode'un
 *   çift etkisi ilk okumayı iptal eder, ikincisi yeniden okur — "istendi" diye tutulan bir küme
 *   yoktur (önceki hâli ikinci koşuyu atlıyordu ve iskelet sonsuza kadar kalıyordu).
 * - Favori çıkarılınca yalnız onun okuyucusu kalkar; öbürleri yeniden okunmaz (AC-35).
 * - "Tekrar dene" yalnız o slug'ın okuyucusunu yeniden bağlar; o slug `retrying` olur, ekranın
 *   geri kalanı yerinde kalır (iskelet yalnız ilk yüklemede).
 */
export function useDetailReads(slugs: readonly string[]): {
  readonly results: Readonly<Record<string, DetailResult>>
  readonly retry: (slugs: readonly string[]) => void
  readonly readers: ReactNode
} {
  const [results, setResults] = useState<Readonly<Record<string, DetailResult>>>({})
  const [tokens, setTokens] = useState<Readonly<Record<string, number>>>({})

  const onResult = useCallback((slug: string, result: DetailResult) => {
    setResults((current) => ({ ...current, [slug]: result }))
  }, [])

  const retry = useCallback((retried: readonly string[]) => {
    setResults((current) => ({
      ...current,
      ...Object.fromEntries(retried.map((slug) => [slug, 'retrying' as const])),
    }))
    setTokens((current) => ({
      ...current,
      ...Object.fromEntries(retried.map((slug) => [slug, (current[slug] ?? 0) + 1])),
    }))
  }, [])

  return {
    results,
    retry,
    readers: slugs.map((slug) => (
      <DetailRead key={`${slug}:${tokens[slug] ?? 0}`} slug={slug} onResult={onResult} />
    )),
  }
}

/**
 * BULGU-5 — yeniden denenen öğe çözülünce odak kaybolmasın: çözülen ilk öğenin yeni denetimine
 * (görünür kartın model bağlantısı) taşınır. Hâlâ hatalıysa "tekrar dene" düğmesi aynı öğedir ve
 * odak zaten oradadır; düşmüşse `fallback`'e gider.
 */
export function useRetryFocus(
  results: Readonly<Record<string, DetailResult>>,
  scope: () => HTMLElement | null,
  fallback: () => HTMLElement | null,
): (slugs: readonly string[]) => void {
  const pending = useRef<readonly string[]>([])

  useEffect(() => {
    const bekleyen = pending.current

    if (bekleyen.length === 0 || bekleyen.some((slug) => results[slug] === 'retrying')) {
      return
    }

    pending.current = []
    const resolved = bekleyen.find((slug) => typeof results[slug] === 'object')
    const target =
      resolved === undefined
        ? bekleyen.some((slug) => results[slug] === 'gone')
          ? fallback()
          : null
        : (scope()?.querySelector<HTMLElement>(`a[href$="/cars/${resolved}"]`) ?? null)

    target?.focus()
  }, [results, scope, fallback])

  return useCallback((slugs: readonly string[]) => {
    pending.current = slugs
  }, [])
}
