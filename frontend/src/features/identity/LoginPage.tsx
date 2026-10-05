import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { login } from '../../shared/api/identity'
import type { CurrentSession } from '../../shared/api/types'
import { Button, Card, Field } from '../../shared/ui'
import {
  EMPTY_LOGIN_FORM,
  afterFailedLogin,
  messageKeyFor,
  shouldShowLockHint,
  type LoginFormValues,
} from './loginFormState'
import { LoginError } from './states/LoginError'
import { LoginLocked } from './states/LoginLocked'

interface Props {
  readonly onSignedIn: (session: CurrentSession) => void
}

type Phase =
  | { readonly kind: 'editing' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'failed'; readonly messageKey: string }

export function LoginPage({ onSignedIn }: Props) {
  const { locale, t } = useLocale()
  const [values, setValues] = useState<LoginFormValues>(EMPTY_LOGIN_FORM)
  const [phase, setPhase] = useState<Phase>({ kind: 'editing' })
  const [failedAttempts, setFailedAttempts] = useState(0)

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setPhase({ kind: 'submitting' })

    try {
      const session = await login(values, locale)

      if (session !== null) {
        onSignedIn(session)
      }
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      // AC-57 — kimlik kalır, parola temizlenir.
      setValues(afterFailedLogin(values))
      setFailedAttempts((count) => count + 1)
      setPhase({ kind: 'failed', messageKey: messageKeyFor(errorCode, 'login.error', locale) })
    }
  }

  return (
    <div className="admin-page admin-page--narrow">
      <Card as="div" testId="login-form">
        <h1 className="admin-form__title">{t('login.title')}</h1>

        {phase.kind === 'failed' ? <LoginError messageKey={phase.messageKey} /> : null}
        {shouldShowLockHint(failedAttempts) ? <LoginLocked /> : null}

        <form className="admin-form" onSubmit={onSubmit} noValidate>
          <Field
            id="login-id"
            label={t('login.field.loginId')}
            type="email"
            autoComplete="username"
            value={values.loginId}
            onChange={(loginId) => setValues({ ...values, loginId })}
          />

          <Field
            id="login-password"
            label={t('login.field.password')}
            type="password"
            autoComplete="current-password"
            value={values.password}
            onChange={(password) => setValues({ ...values, password })}
          />

          <Button type="submit" loading={phase.kind === 'submitting'}>
            {phase.kind === 'submitting' ? t('login.submitting') : t('login.submit')}
          </Button>
        </form>
      </Card>
    </div>
  )
}
