import { useMemo, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { captureLead } from '../../shared/api/leads'
import { ApiError } from '../../shared/api/client'
import { LEAD_TYPES, type LeadType } from '../../shared/api/types'
import { catalogKeys } from '../../shared/i18n/i18n'
import { Button, Card } from '../../shared/ui'
import { LeadFields } from './LeadFields'
import {
  EMPTY_LEAD_FORM,
  messageKeyFor,
  toRequest,
  validate,
  type LeadFormField,
  type LeadFormValues,
} from './leadFormState'
import { LeadClosed } from './states/LeadClosed'
import { LeadSubmitError } from './states/LeadSubmitError'
import { LeadSuccess } from './states/LeadSuccess'

interface Props {
  readonly listingSlug: string
  /** R2/AC-11 — `Rezerve` ilanda form hiç basılmaz. */
  readonly isReserved: boolean
}

type Phase =
  | { readonly kind: 'editing' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'submitted' }
  | { readonly kind: 'failed'; readonly messageKey: string; readonly listingGone: boolean }

/** İlanın artık çözülemediğini söyleyen kod; ekran buna ayrı bir eylem sunar (AC-15). */
const LISTING_UNAVAILABLE = 'lead.listing_unavailable'

export function LeadForm({ listingSlug, isReserved }: Props) {
  const { locale, t } = useLocale()
  const [values, setValues] = useState<LeadFormValues>(EMPTY_LEAD_FORM)
  const [invalidField, setInvalidField] = useState<{
    readonly field: LeadFormField
    readonly code: string
  } | null>(null)
  const [phase, setPhase] = useState<Phase>({ kind: 'editing' })

  /**
   * R17/Bilinen tuzak 8 — anahtar formun ÖMRÜ boyunca sabittir, her gönderimde
   * yenilenmez. Ağ hatası isteğin sunucuya ulaşmasından sonra olduysa "tekrar dene"
   * aynı anahtarla gider ve ikinci bir kayıt oluşmaz. Başarılı gönderimden sonra form
   * zaten kapanıyor, yani anahtarın yenilenmesi gereken bir durum kalmıyor.
   */
  const idempotencyKey = useMemo(() => crypto.randomUUID(), [])

  const messageKeys = useMemo(() => catalogKeys(locale), [locale])

  if (isReserved) {
    return <LeadClosed />
  }

  if (phase.kind === 'submitted') {
    return <LeadSuccess homeHref={`/${locale}/`} />
  }

  function onChange(next: LeadFormValues) {
    setValues(next)
    setInvalidField(null)
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()

    const problem = validate(values)

    if (problem !== null) {
      setInvalidField(problem)
      return
    }

    setInvalidField(null)
    setPhase({ kind: 'submitting' })

    try {
      await captureLead(toRequest(values, listingSlug, locale), locale, idempotencyKey)
      setPhase({ kind: 'submitted' })
    } catch (cause) {
      if (cause instanceof ApiError && cause.field !== null) {
        // Sunucu hangi ALANIN hatalı olduğunu yapısal olarak söyledi (AC-40); metni
        // uç üretir, sunucudan çevrilmiş metin gelmez.
        setInvalidField({ field: cause.field as LeadFormField, code: cause.errorCode })
      }

      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setPhase({
        kind: 'failed',
        messageKey: messageKeyFor(errorCode, messageKeys),
        listingGone: errorCode === LISTING_UNAVAILABLE,
      })
    }
  }

  return (
    <Card as="div" testId="lead-form">
      <h2 className="lead-form__title">{t('lead.title')}</h2>
      <p className="lead-form__intro">{t('lead.intro')}</p>

      {phase.kind === 'failed' ? (
        <LeadSubmitError
          messageKey={phase.messageKey}
          onRetry={() => setPhase({ kind: 'editing' })}
          homeHref={phase.listingGone ? `/${locale}/` : undefined}
        />
      ) : null}

      <form className="lead-form__form" onSubmit={onSubmit} noValidate>
        <LeadFields
          values={values}
          types={LEAD_TYPES as readonly LeadType[]}
          invalidField={invalidField}
          onChange={onChange}
        />

        <Button type="submit" loading={phase.kind === 'submitting'}>
          {phase.kind === 'submitting' ? t('lead.submitting') : t('lead.submit')}
        </Button>
      </form>
    </Card>
  )
}
