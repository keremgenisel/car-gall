import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { LINK_INVALID, confirmNewsletter } from '../../shared/api/notifications'
import { newsletterSectionPath, newsletterTokenOf } from '../../shared/lib/route'
import { Button } from '../../shared/ui'
import { NewsletterLinkState } from './NewsletterLinkState'

type Phase = 'idle' | 'confirming' | 'confirmed' | 'invalid' | 'failed'

/**
 * `0018` R2 · R14 · **D6** · **D7** — teyit sayfası. Sayfanın AÇILMASI teyit değildir: istek
 * yalnız "Aboneliği onayla" düğmesiyle gider (AC-27; kurumsal e-posta tarayıcıları
 * bağlantıları kendiliğinden açar — Bilinen tuzak 2).
 *
 * <p>Süresi dolmuş, kullanılmış ve hiç var olmamış bağlantı aynı ekranı alır ve yeniden abone
 * olma yolu sunar (AC-30, AC-35); sunucu hatası teknik ayrıntısız bir hata ve "tekrar dene"
 * (AC-36). Anahtarsız adres hiç istek göndermeden "geçerli değil"dir.</p>
 */
export function NewsletterConfirmPage() {
  const { locale, t } = useLocale()
  const token = newsletterTokenOf(window.location.search)
  const [phase, setPhase] = useState<Phase>(token.length === 0 ? 'invalid' : 'idle')

  async function confirm() {
    setPhase('confirming')

    try {
      await confirmNewsletter(token, locale)
      setPhase('confirmed')
    } catch (cause) {
      setPhase(cause instanceof ApiError && cause.errorCode === LINK_INVALID ? 'invalid' : 'failed')
    }
  }

  if (phase === 'confirmed') {
    return (
      <NewsletterLinkState
        testId="newsletter-confirmed"
        title={t('newsletter.confirmed.title')}
        body={t('newsletter.confirmed.body')}
      />
    )
  }

  if (phase === 'invalid') {
    return (
      <NewsletterLinkState
        testId="newsletter-link-invalid"
        title={t('newsletter.invalid.title')}
        body={t('newsletter.invalid.confirmBody')}
        action={<a href={newsletterSectionPath(locale)}>{t('newsletter.resubscribe')}</a>}
      />
    )
  }

  return (
    <NewsletterLinkState
      testId={phase === 'failed' ? 'newsletter-confirm-error' : 'newsletter-confirm'}
      alert={phase === 'failed'}
      title={t(phase === 'failed' ? 'newsletter.error.title' : 'newsletter.confirm.title')}
      body={t(phase === 'failed' ? 'newsletter.error.body' : 'newsletter.confirm.body')}
      action={
        <Button loading={phase === 'confirming'} onClick={() => void confirm()}>
          {t(phase === 'failed' ? 'newsletter.retry' : 'newsletter.confirm.action')}
        </Button>
      }
    />
  )
}
