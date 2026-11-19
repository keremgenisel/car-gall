import { useEffect, useState } from 'react'
import { fetchContentNavigation, type ContentNavigationLink } from '../shared/api/content'
import { useLocale } from './localeContext'

/**
 * `0017` R21 — o dilde yayında olan sistem sayfalarının bağlantıları. `0018` **D36**: ana
 * sayfada bir kez okunur (`HomeRoute`) ve altlık ile bülten notu arasında paylaşılır.
 */
export function useCorporateLinks(): readonly ContentNavigationLink[] {
  const { locale } = useLocale()
  const [links, setLinks] = useState<{
    readonly locale: string
    readonly items: readonly ContentNavigationLink[]
  }>({
    locale,
    items: [],
  })

  useEffect(() => {
    const controller = new AbortController()

    fetchContentNavigation(locale, controller.signal)
      .then((items) => setLinks({ locale, items }))
      .catch((cause: unknown) => {
        // İptal edilen eski istek yeni dilin bağlantılarını ezmesin.
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setLinks({ locale, items: [] })
      })

    return () => controller.abort()
  }, [locale])

  return links.locale === locale ? links.items : []
}
