import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { subscribeToNewsletter } from '../../shared/api/notifications'
import { NEWSLETTER_SEGMENT } from '../../shared/lib/route'
import { Button, Field } from '../../shared/ui'

type Phase = 'editing' | 'submitting' | 'submitted' | 'rateLimited' | 'failed'

const EMAIL_INVALID = 'notification.subscription_email_invalid'
const RATE_LIMITED = 'notification.subscription_rate_limited'

/**
 * `0018` R1 · R4 · R11 · **D16** — ana sayfanın bülten bölümü: `HomeCatalog`'un altında,
 * altlıktan önce; vitrin boşken de görünür (AC-2).
 *
 * <p>Beş durum (R11): yükleniyor (düğme devre dışı — ikinci basış ikinci istek göndermez,
 * AC-5), başarılı (form yerinde mesaja döner, AC-6), doğrulama hatası (alan işaretlenir,
 * AC-7), hız sınırı (AC-12) ve hata (teknik ayrıntı yok, adres alanda kalır, AC-11).</p>
 *
 * <p>KVKK bağlantısı ÇAĞIRANDAN gelir: altlığın kurumsal bağlantılarıyla aynı okuma (**D36**);
 * o dilde yayında değilse `null` ve not bağlantısız basılır (**D15**, AC-4).</p>
 */
export function NewsletterSection({ privacyHref }: { readonly privacyHref: string | null }) {
  const { locale, t } = useLocale()
  const [email, setEmail] = useState('')
  const [phase, setPhase] = useState<Phase>('editing')
  const [invalid, setInvalid] = useState(false)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()

    if (phase === 'submitting') {
      return
    }

    if (email.trim().length === 0) {
      setInvalid(true)
      return
    }

    setInvalid(false)
    setPhase('submitting')

    try {
      await subscribeToNewsletter(email, locale)
      setPhase('submitted')
    } catch (cause) {
      const code = cause instanceof ApiError ? cause.errorCode : ''

      setInvalid(code === EMAIL_INVALID)
      setPhase(
        code === EMAIL_INVALID ? 'editing' : code === RATE_LIMITED ? 'rateLimited' : 'failed',
      )
    }
  }

  return (
    <section
      className="home-section newsletter"
      id={NEWSLETTER_SEGMENT}
      aria-labelledby="newsletter-title"
      data-testid="newsletter-section"
    >
      <h2 className="home-section__title" id="newsletter-title">
        {t('newsletter.title')}
      </h2>
      <p className="newsletter__body">{t('newsletter.body')}</p>

      {phase === 'submitted' ? (
        <p className="newsletter__status" role="status" data-testid="newsletter-success">
          {t('newsletter.success')}
        </p>
      ) : (
        <form className="newsletter__form" onSubmit={onSubmit} noValidate>
          <Field
            id="newsletter-email"
            label={t('newsletter.email')}
            type="email"
            autoComplete="email"
            value={email}
            errorKey={invalid ? 'newsletter.emailInvalid' : undefined}
            onChange={setEmail}
          />
          <Button type="submit" loading={phase === 'submitting'}>
            {t(phase === 'submitting' ? 'newsletter.submitting' : 'newsletter.submit')}
          </Button>
          {phase === 'rateLimited' || phase === 'failed' ? (
            <p className="newsletter__error" role="alert" data-testid="newsletter-error">
              {t(phase === 'rateLimited' ? 'newsletter.rateLimited' : 'newsletter.failed')}
            </p>
          ) : null}
        </form>
      )}

      <p className="newsletter__note">
        {t('newsletter.note')}
        {privacyHref === null ? null : (
          <>
            {' '}
            <a href={privacyHref} data-testid="newsletter-privacy">
              {t('newsletter.privacy')}
            </a>
          </>
        )}
      </p>
    </section>
  )
}
