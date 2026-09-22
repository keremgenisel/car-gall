import { useId } from 'react'
import { useLocale } from '../../app/localeContext'
import type { LeadType } from '../../shared/api/types'
import {
  MAX_EMAIL_LENGTH,
  MAX_MESSAGE_LENGTH,
  MAX_NAME_LENGTH,
  type LeadFormField,
  type LeadFormValues,
} from './leadFormState'

interface Props {
  readonly values: LeadFormValues
  readonly types: readonly LeadType[]
  readonly invalidField: { readonly field: LeadFormField; readonly code: string } | null
  readonly onChange: (next: LeadFormValues) => void
}

/**
 * Her alanın `<label>`'ı vardır; yalnız `placeholder` etiket değildir
 * (docs/frontend.md § 7). Hatalı alan `aria-invalid` ile işaretlenir ve hatanın ne olduğu
 * alanın altında, ziyaretçinin dilinde yazar (AC-40).
 *
 * Bu alanlar `shared/ui` envanterine GİRMİYOR: bugün yalnız bu ekranda kullanılıyorlar
 * ve tek ekranda kullanılan şey ortak bileşen değildir (docs/frontend.md § 4.2). İkinci
 * bir form geldiğinde terfi önerilir.
 */
export function LeadFields({ values, types, invalidField, onChange }: Props) {
  const { t } = useLocale()
  const prefix = useId()

  const errorFor = (field: LeadFormField) =>
    invalidField !== null && invalidField.field === field ? invalidField.code : null

  return (
    <>
      <Field
        id={`${prefix}-type`}
        label={t('lead.field.type')}
        error={errorFor('leadType')}
        control={(id, describedBy, invalid) => (
          <select
            id={id}
            className="lead-form__control"
            value={values.leadType}
            aria-describedby={describedBy}
            aria-invalid={invalid}
            onChange={(event) => onChange({ ...values, leadType: event.target.value as LeadType })}
          >
            {types.map((type) => (
              <option key={type} value={type}>
                {t(`lead.type.${type}`)}
              </option>
            ))}
          </select>
        )}
      />

      <Field
        id={`${prefix}-name`}
        label={t('lead.field.fullName')}
        error={errorFor('fullName')}
        control={(id, describedBy, invalid) => (
          <input
            id={id}
            className="lead-form__control"
            type="text"
            value={values.fullName}
            maxLength={MAX_NAME_LENGTH}
            autoComplete="name"
            aria-describedby={describedBy}
            aria-invalid={invalid}
            onChange={(event) => onChange({ ...values, fullName: event.target.value })}
          />
        )}
      />

      <Field
        id={`${prefix}-phone`}
        label={t('lead.field.phone')}
        hint={t('lead.field.phoneHint')}
        error={errorFor('phone')}
        control={(id, describedBy, invalid) => (
          <input
            id={id}
            className="lead-form__control"
            type="tel"
            inputMode="tel"
            value={values.phone}
            autoComplete="tel"
            /* Latin rakam ve `+`: Arapça sayfada da numara Latin yazılır. */
            dir="ltr"
            aria-describedby={describedBy}
            aria-invalid={invalid}
            onChange={(event) => onChange({ ...values, phone: event.target.value })}
          />
        )}
      />

      <Field
        id={`${prefix}-email`}
        label={t('lead.field.email')}
        error={errorFor('email')}
        control={(id, describedBy, invalid) => (
          <input
            id={id}
            className="lead-form__control"
            type="email"
            value={values.email}
            maxLength={MAX_EMAIL_LENGTH}
            autoComplete="email"
            dir="ltr"
            aria-describedby={describedBy}
            aria-invalid={invalid}
            onChange={(event) => onChange({ ...values, email: event.target.value })}
          />
        )}
      />

      <Field
        id={`${prefix}-message`}
        label={t('lead.field.message')}
        hint={t('lead.field.messageOptional')}
        error={errorFor('message')}
        control={(id, describedBy, invalid) => (
          <textarea
            id={id}
            className="lead-form__control lead-form__control--message"
            value={values.message}
            maxLength={MAX_MESSAGE_LENGTH}
            rows={4}
            aria-describedby={describedBy}
            aria-invalid={invalid}
            onChange={(event) => onChange({ ...values, message: event.target.value })}
          />
        )}
      />

      {/*
        AC-26 — kutu ÖNCEDEN İŞARETLİ GELMEZ. L7 izni bir kayıt olarak ister; önceden
        işaretli kutu, alınmamış izni alınmış gibi gösterir (Bilinen tuzak 4).
      */}
      <div className="lead-form__consent">
        <input
          id={`${prefix}-consent`}
          className="lead-form__checkbox"
          type="checkbox"
          checked={values.consentGiven}
          aria-describedby={`${prefix}-consent-text`}
          onChange={(event) => onChange({ ...values, consentGiven: event.target.checked })}
        />
        <label className="lead-form__consent-label" htmlFor={`${prefix}-consent`}>
          {t('lead.consent.label')}
        </label>
        <p className="lead-form__consent-text" id={`${prefix}-consent-text`}>
          {t('lead.consent.text')}
        </p>
      </div>
    </>
  )
}

interface FieldProps {
  readonly id: string
  readonly label: string
  readonly hint?: string
  readonly error: string | null
  readonly control: (
    id: string,
    describedBy: string | undefined,
    invalid: boolean | undefined,
  ) => React.ReactNode
}

function Field({ id, label, hint, error, control }: FieldProps) {
  const { t } = useLocale()
  const hintId = hint === undefined ? undefined : `${id}-hint`
  const errorId = error === null ? undefined : `${id}-error`
  const describedBy = [hintId, errorId].filter((value) => value !== undefined).join(' ')

  return (
    <div className="lead-form__field">
      <label className="lead-form__label" htmlFor={id}>
        {label}
      </label>
      {hint === undefined ? null : (
        <p className="lead-form__hint" id={hintId}>
          {hint}
        </p>
      )}
      {control(
        id,
        describedBy.length > 0 ? describedBy : undefined,
        error === null ? undefined : true,
      )}
      {error === null ? null : (
        <p className="lead-form__field-error" id={errorId} role="alert">
          {t(`lead.error.${error.replace(/^lead\./, '')}`)}
        </p>
      )}
    </div>
  )
}
