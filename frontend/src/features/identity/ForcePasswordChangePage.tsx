import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { ApiError } from '../../shared/api/client'
import { changeOwnPassword } from '../../shared/api/identity'
import { Button, Card, EmptyState } from '../../shared/ui'
import { AdminField } from './AdminField'
import { messageKeyFor } from './loginFormState'

/** Sunucunun yapılandırılan asgari uzunluğu; uç yalnız ipucu metninde kullanır. */
const MINIMUM_PASSWORD_LENGTH = 12

type Phase =
  | { readonly kind: 'editing' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'changed' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * R10/AC-32 — ilk parolasıyla giriş yapan personelden parolasını değiştirmesi istenir ve
 * değiştirmeden yönetim yüzeyinde çalışamaz.
 *
 * Değişimden sonra personel GİRİŞ EKRANINA döner: parola değiştiğinde o personelin TÜM
 * oturumları geçersizleşir (R11, AC-22) — buna değişikliği yapan cihaz da dâhildir.
 * Mevcut oturumu ayrıcalıklı tutmak, sızan bir parolayla açılmış oturumun hayatta
 * kalmasına kapı açardı.
 */
export function ForcePasswordChangePage() {
  const { locale, t } = useLocale()
  const [current, setCurrent] = useState('')
  const [next, setNext] = useState('')
  const [phase, setPhase] = useState<Phase>({ kind: 'editing' })

  if (phase.kind === 'changed') {
    return (
      <div className="admin-page admin-page--narrow">
        <EmptyState
          title={t('password.changed.title')}
          description={t('password.changed.body')}
          action={
            <a className="ui-button ui-button--secondary" href="/admin">
              {t('password.changed.action')}
            </a>
          }
          testId="password-changed"
        />
      </div>
    )
  }

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault()
    setPhase({ kind: 'submitting' })

    try {
      await changeOwnPassword(current, next, locale)
      setPhase({ kind: 'changed' })
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setPhase({ kind: 'failed', messageKey: messageKeyFor(errorCode, 'password.error', locale) })
    }
  }

  return (
    <div className="admin-page admin-page--narrow">
      <Card as="div" testId="password-form">
        <h1 className="admin-form__title">{t('password.title')}</h1>
        <p className="admin-form__intro">{t('password.intro')}</p>

        {phase.kind === 'failed' ? (
          <div className="admin-form__error" role="alert" data-testid="password-error">
            <p className="admin-form__error-title">{t('password.error.title')}</p>
            <p className="admin-form__error-body">{t(phase.messageKey)}</p>
          </div>
        ) : null}

        <form className="admin-form" onSubmit={onSubmit} noValidate>
          <AdminField
            id="password-current"
            label={t('password.field.current')}
            type="password"
            autoComplete="current-password"
            value={current}
            onChange={setCurrent}
          />

          <AdminField
            id="password-new"
            label={t('password.field.new')}
            type="password"
            autoComplete="new-password"
            hint={t('password.hint', { count: MINIMUM_PASSWORD_LENGTH })}
            value={next}
            onChange={setNext}
          />

          <Button type="submit" loading={phase.kind === 'submitting'}>
            {phase.kind === 'submitting' ? t('password.submitting') : t('password.submit')}
          </Button>
        </form>
      </Card>
    </div>
  )
}
