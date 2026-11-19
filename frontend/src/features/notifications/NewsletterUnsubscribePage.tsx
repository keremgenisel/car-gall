import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { LINK_INVALID, unsubscribeFromNewsletter } from '../../shared/api/notifications'
import { newsletterSectionPath, newsletterTokenOf } from '../../shared/lib/route'
import { Button, Skeleton } from '../../shared/ui'
import { NewsletterLinkState } from './NewsletterLinkState'

type Result = 'unsubscribed' | 'invalid' | 'failed'

/**
 * `0018` R5 · **D6** · **D8** — çıkış sayfası: bağlantı AÇILDIĞI ANDA çıkarır; düğme ya da
 * "emin misiniz" yok (AC-38, Bilinen tuzak 3). Aynı bağlantının ikinci açılışı aynı mesajı
 * gösterir (AC-41).
 *
 * <p>İstek sürerken iskelet (AC-45), geçersiz bağlantıda "geçerli değil" (AC-44), sunucu
 * hatasında teknik ayrıntısız hata ve "tekrar dene" (AC-45). Başarıda yeniden abone olma yolu
 * (AC-43 — **D6**'nın bedeli: bağlantıları açan tarayıcı aboneyi istemeden çıkarabilir).</p>
 *
 * <p>Yükleniyor hâli efektte senkron setState ile kurulmaz; deneme anahtarından türetilir
 * (`HomeCatalog` deseni).</p>
 */
export function NewsletterUnsubscribePage() {
  const { locale, t } = useLocale()
  const token = newsletterTokenOf(window.location.search)
  const [attempt, setAttempt] = useState(0)
  const [result, setResult] = useState<{ readonly attempt: number; readonly value: Result } | null>(
    token.length === 0 ? { attempt: 0, value: 'invalid' } : null,
  )

  useEffect(() => {
    if (token.length === 0) {
      return
    }

    const controller = new AbortController()

    unsubscribeFromNewsletter(token, locale, controller.signal)
      .then(() => setResult({ attempt, value: 'unsubscribed' }))
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setResult({
            attempt,
            value:
              cause instanceof ApiError && cause.errorCode === LINK_INVALID ? 'invalid' : 'failed',
          })
        }
      })

    return () => controller.abort()
  }, [token, locale, attempt])

  const current =
    result !== null && (result.attempt === attempt || token.length === 0) ? result.value : null
  const resubscribe = <a href={newsletterSectionPath(locale)}>{t('newsletter.resubscribe')}</a>

  if (current === null) {
    return (
      <section
        className="ui-state newsletter-page"
        aria-busy="true"
        data-testid="newsletter-unsubscribing"
      >
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </section>
    )
  }

  if (current === 'unsubscribed') {
    return (
      <NewsletterLinkState
        testId="newsletter-unsubscribed"
        title={t('newsletter.unsubscribed.title')}
        body={t('newsletter.unsubscribed.body')}
        action={resubscribe}
      />
    )
  }

  if (current === 'invalid') {
    return (
      <NewsletterLinkState
        testId="newsletter-link-invalid"
        title={t('newsletter.invalid.title')}
        body={t('newsletter.invalid.unsubscribeBody')}
        action={resubscribe}
      />
    )
  }

  return (
    <NewsletterLinkState
      testId="newsletter-unsubscribe-error"
      alert
      title={t('newsletter.error.title')}
      body={t('newsletter.error.body')}
      action={
        <Button onClick={() => setAttempt((value) => value + 1)}>{t('newsletter.retry')}</Button>
      }
    />
  )
}
