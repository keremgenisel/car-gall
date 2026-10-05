import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { addStaff } from '../../shared/api/identity'
import { STAFF_ROLES, type AddStaffRequest, type StaffRole } from '../../shared/api/types'
import { SUPPORTED_LOCALES } from '../../shared/lib/locale'
import { Button, Card, Field } from '../../shared/ui'
import { messageKeyFor } from './loginFormState'

const EMPTY: AddStaffRequest = {
  displayName: '',
  loginId: '',
  initialPassword: '',
  role: 'sales_advisor',
  preferredLocale: 'tr',
  branchId: null,
}

/**
 * R9/R10 — Yönetici personeli ekler ve ilk parolayı belirler; parolayı personele sistem
 * DIŞINDAN iletir (D1: `Notifications` yok ve Yasak Listesi 17 istisnasız).
 */
export function AddStaffForm({ onAdded }: { readonly onAdded: () => void }) {
  const { locale, t } = useLocale()
  const [values, setValues] = useState<AddStaffRequest>({ ...EMPTY, preferredLocale: locale })
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<{
    readonly messageKey: string
    readonly field: string | null
  } | null>(null)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)

    try {
      await addStaff(values, locale)
      setValues({ ...EMPTY, preferredLocale: locale })
      onAdded()
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setError({
        messageKey: messageKeyFor(errorCode, 'staff.add.error', locale),
        field: cause instanceof ApiError ? cause.field : null,
      })
    } finally {
      setSubmitting(false)
    }
  }

  const errorKeyFor = (field: string) =>
    error !== null && error.field === field ? error.messageKey : undefined

  return (
    <Card as="div" testId="add-staff-form">
      <h2 className="admin-form__title">{t('staff.add.title')}</h2>
      <p className="admin-form__intro">{t('staff.add.hint')}</p>

      {error !== null && error.field === null ? (
        <div className="admin-form__error" role="alert" data-testid="add-staff-error">
          <p className="admin-form__error-body">{t(error.messageKey)}</p>
        </div>
      ) : null}

      <form className="admin-form" onSubmit={onSubmit} noValidate>
        <Field
          id="staff-name"
          label={t('staff.add.field.displayName')}
          value={values.displayName}
          errorKey={errorKeyFor('displayName')}
          onChange={(displayName) => setValues({ ...values, displayName })}
        />

        <Field
          id="staff-login"
          label={t('staff.add.field.loginId')}
          type="email"
          value={values.loginId}
          errorKey={errorKeyFor('loginId')}
          onChange={(loginId) => setValues({ ...values, loginId })}
        />

        <Field
          id="staff-password"
          label={t('staff.add.field.initialPassword')}
          type="password"
          autoComplete="new-password"
          value={values.initialPassword}
          errorKey={errorKeyFor('password')}
          onChange={(initialPassword) => setValues({ ...values, initialPassword })}
        />

        <div className="admin-form__field">
          <label className="admin-form__label" htmlFor="staff-role">
            {t('staff.add.field.role')}
          </label>
          <select
            id="staff-role"
            className="admin-form__control"
            value={values.role}
            onChange={(event) => setValues({ ...values, role: event.target.value as StaffRole })}
          >
            {STAFF_ROLES.map((role) => (
              <option key={role} value={role}>
                {t(`staff.role.${role}`)}
              </option>
            ))}
          </select>
        </div>

        <div className="admin-form__field">
          <label className="admin-form__label" htmlFor="staff-locale">
            {t('staff.add.field.preferredLocale')}
          </label>
          <select
            id="staff-locale"
            className="admin-form__control"
            value={values.preferredLocale}
            onChange={(event) => setValues({ ...values, preferredLocale: event.target.value })}
          >
            {SUPPORTED_LOCALES.map((item) => (
              <option key={item} value={item}>
                {t(`app.locale.${item}`)}
              </option>
            ))}
          </select>
        </div>

        <Button type="submit" loading={submitting}>
          {submitting ? t('staff.add.submitting') : t('staff.add.submit')}
        </Button>
      </form>
    </Card>
  )
}
