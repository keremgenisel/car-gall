import type { ReactNode } from 'react'

/**
 * `0018` — teyit ve çıkış sayfalarının ortak kutusu: sayfanın TEK `<h1>`'i, açıklama ve en
 * çok bir eylem. Hata durumunda `role="alert"`; teknik ayrıntı yok (docs/frontend.md § 3.1).
 */
export function NewsletterLinkState({
  title,
  body,
  action,
  alert = false,
  testId,
}: {
  readonly title: string
  readonly body: string
  readonly action?: ReactNode
  readonly alert?: boolean
  readonly testId: string
}) {
  return (
    <section
      className="ui-state newsletter-page"
      role={alert ? 'alert' : undefined}
      data-testid={testId}
    >
      <h1 className="ui-state__title">{title}</h1>
      <p className="ui-state__body">{body}</p>
      {action}
    </section>
  )
}
