import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchContentPage, type ContentPage as ContentPageData } from '../../shared/api/content'
import { setAlternateLinks } from '../../shared/lib/alternateLinks'
import { contentPath, homePath } from '../../shared/lib/route'
import { Card, EmptyState, ErrorState, Skeleton } from '../../shared/ui'
import { ContentBody } from './ContentBody'

const SKELETON_LINES = 4

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'notFound' }
  | { readonly kind: 'ready'; readonly page: ContentPageData }

/**
 * `0017` R11, R12, R19, R23 — ziyaretçinin içerik sayfası. Üç durum `0002`'nin desenindedir:
 * iskelet, "bulunamadı" (yeniden deneme YOK — R12) ve yeniden denemeli hata. "Bulunamadı"
 * ile "hata" ayrı ekranlardır: uç `404` derse sayfa yoktur, başka bir hata ise geçicidir.
 *
 * SSS (`faq`) sayfasında girdiler gövdenin ALTINDA, sırasıyla basılır (AC-81).
 */
export function ContentPage({ slug }: { readonly slug: string }) {
  const { locale, t } = useLocale()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [reloadToken, setReloadToken] = useState(0)

  const requestKey = `${locale}|${slug}|${reloadToken}`
  const load: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    fetchContentPage(slug, locale, controller.signal)
      .then((page) =>
        setResult({
          key: requestKey,
          value: page === null ? { kind: 'notFound' } : { kind: 'ready', page },
        }),
      )
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setResult({ key: requestKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [locale, slug, reloadToken, requestKey])

  // R15 — dil değiştirici, sayfanın yayında olduğu diğer dillerin slug'larına gider; sayfa
  // yoksa ya da yüklenmediyse o dillerin ana sayfasına (sayfa ayrılınca kayıt silinir).
  const alternates = load.kind === 'ready' ? load.page.alternates : null

  useEffect(() => {
    setAlternateLinks(
      Object.fromEntries(
        (alternates ?? []).map((item) => [
          item.locale,
          contentPath(item.locale as typeof locale, item.slug),
        ]),
      ),
    )
    return () => setAlternateLinks(null)
  }, [alternates])

  if (load.kind === 'loading') {
    // AC-98 — `0002`'nin iskelet deseni: aynı bileşen, durum bölgesi ve gizli metin.
    return (
      <div className="page">
        <Card as="div" testId="content-skeleton">
          <div className="content-page" role="status" aria-live="polite">
            <span className="visually-hidden">{t('content.loading')}</span>
            <h1 className="content-page__title">
              <Skeleton shape="line" />
            </h1>
            {Array.from({ length: SKELETON_LINES }, (_, index) => (
              <p key={index}>
                <Skeleton shape="line" />
              </p>
            ))}
          </div>
        </Card>
      </div>
    )
  }

  if (load.kind === 'notFound') {
    return (
      <div className="page">
        {/* AC-40 — `0002` R5'in "bulunamadı" ekranıyla AYNI bileşen (`EmptyState`) ve desen. */}
        <EmptyState
          testId="content-not-found"
          title={t('content.notFound.title')}
          description={t('content.notFound.body')}
          action={
            <a className="ui-button ui-button--secondary" href={homePath(locale)}>
              {t('content.notFound.action')}
            </a>
          }
        />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <div className="page">
        <ErrorState
          testId="content-error"
          title={t('content.error.title')}
          description={t('content.error.body')}
          retryLabel={t('content.error.retry')}
          onRetry={() => setReloadToken((token) => token + 1)}
        />
      </div>
    )
  }

  const { page } = load

  return (
    <div className="page">
      <Card as="article" testId="content-page">
        <div className="content-page">
          {/* Başlık Latin olabilir, gövde Arapça: karışık yönlü metin. */}
          <h1 className="content-page__title" dir="auto">
            {page.title}
          </h1>

          <ContentBody source={page.body} testId="content-body" />

          {page.faqEntries.length === 0 ? null : (
            <section className="content-faq" aria-label={page.title} data-testid="content-faq">
              {page.faqEntries.map((entry, index) => (
                <div className="content-faq__item" key={index} data-testid="content-faq-item">
                  <h2 className="content-faq__question" dir="auto">
                    {entry.question}
                  </h2>
                  <ContentBody source={entry.answer} />
                </div>
              ))}
            </section>
          )}
        </div>
      </Card>
    </div>
  )
}
