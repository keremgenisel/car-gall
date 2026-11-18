import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  deletePage,
  fetchAdminPage,
  savePage,
  setPagePublication,
  type AdminPage,
  type PageTextInput,
} from '../../../shared/api/contentAdmin'
import { ApiError } from '../../../shared/api/client'
import { adminPath } from '../../../shared/lib/route'
import { Badge, Button, Dialog, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { fieldErrorKey, fieldErrorsOf, isConflict } from './contentErrors'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'missing' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly page: AdminPage }

type Banner =
  | { readonly kind: 'none' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed' }

/**
 * `0017` R1–R10 — bir içerik sayfasının panel düzenlemesi. Her etkin dil kendi alanında;
 * kaydet bütün dilleri tek istekte yazar, yayına alma ve kaldırma dil başınadır. Sürüm
 * çakışmasında (`409`) panel uyarır ve güncel hâli yeniden yükler (R10, AC-31). Silme yalnız
 * serbest sayfada vardır ve onay ister (R8).
 */
export function ContentEditPage({ pageId }: { readonly pageId: string }) {
  const { locale, t } = useLocale()
  const [load, setLoad] = useState<Load>({ kind: 'loading' })
  const [texts, setTexts] = useState<readonly PageTextInput[]>([])
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [banner, setBanner] = useState<Banner>({ kind: 'none' })
  const [busy, setBusy] = useState(false)
  const [confirming, setConfirming] = useState(false)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchAdminPage(pageId, locale, controller.signal)
      .then((page) => {
        setLoad({ kind: 'ready', page })
        setTexts(
          page.locales.map(({ locale: l, title, body, slug }) => ({
            locale: l,
            title,
            body,
            slug,
          })),
        )
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setLoad(
          cause instanceof ApiError && cause.status === 404
            ? { kind: 'missing' }
            : { kind: 'error' },
        )
      })

    return () => controller.abort()
  }, [pageId, locale, reload])

  const title = t('content.edit.title')

  if (load.kind === 'loading') {
    return (
      <div className="admin-page" data-testid="content-edit-loading" role="status">
        <h1 className="admin-page__title">{title}</h1>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'missing') {
    return (
      <div className="admin-page">
        <h1 className="admin-page__title">{title}</h1>
        <EmptyState
          testId="content-edit-missing"
          title={t('content.edit.missing.title')}
          description={t('content.edit.missing.body')}
          action={
            <a className="ui-button ui-button--secondary" href={adminPath('content')}>
              {t('content.edit.back')}
            </a>
          }
        />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <div className="admin-page">
        <h1 className="admin-page__title">{title}</h1>
        <ErrorState
          testId="content-edit-error"
          title={t('content.admin.error.title')}
          description={t('content.admin.error.body')}
          retryLabel={t('content.admin.error.retry')}
          onRetry={() => setReload((value) => value + 1)}
        />
      </div>
    )
  }

  const { page } = load

  async function run(action: () => Promise<number>) {
    setBusy(true)
    setErrors({})
    setBanner({ kind: 'none' })

    try {
      await action()
      setBanner({ kind: 'saved' })
      setReload((value) => value + 1)
    } catch (cause) {
      if (isConflict(cause)) {
        // R10 — başkası değiştirdi: uyar ve güncel hâli yeniden yükle (AC-31).
        setBanner({ kind: 'conflict' })
        setReload((value) => value + 1)
      } else {
        const fields = fieldErrorsOf(cause)
        setErrors(fields)
        setBanner(Object.keys(fields).length === 0 ? { kind: 'failed' } : { kind: 'none' })
      }
    } finally {
      setBusy(false)
    }
  }

  function update(target: string, patch: Partial<PageTextInput>) {
    setTexts((current) =>
      current.map((item) => (item.locale === target ? { ...item, ...patch } : item)),
    )
  }

  return (
    <div className="admin-page" data-testid="content-edit">
      <div className="admin-page__header">
        <h1 className="admin-page__title">
          {page.systemKey === null ? title : t(`content.system.${page.systemKey}`)}
        </h1>
        <a className="ui-button ui-button--text" href={adminPath('content')}>
          {t('content.edit.back')}
        </a>
      </div>

      {page.missingLocales.length === 0 ? null : (
        <p className="admin-form__hint" data-testid="content-missing-locales">
          {t('content.admin.missing', {
            locales: page.missingLocales.map((item) => t(`app.locale.${item}`)).join(', '),
          })}
        </p>
      )}

      {banner.kind === 'conflict' ? (
        <div className="admin-form__error" role="alert" data-testid="content-conflict">
          <p className="admin-form__error-body">{t('content.edit.conflict')}</p>
        </div>
      ) : null}
      {banner.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="content-save-failed">
          <p className="admin-form__error-body">{t('content.edit.failed')}</p>
        </div>
      ) : null}
      {banner.kind === 'saved' ? (
        <div className="admin-form__notice" role="status" data-testid="content-saved">
          <p className="admin-form__notice-body">{t('content.edit.saved')}</p>
        </div>
      ) : null}

      <form
        className="admin-form content-edit"
        noValidate
        onSubmit={(event) => {
          event.preventDefault()
          void run(() => savePage(page.id, page.version, texts, locale))
        }}
      >
        {texts.map((text) => {
          const saved = page.locales.find((item) => item.locale === text.locale)!
          const published = saved.status === 'published'
          const prefix = `content-${text.locale}`

          return (
            <fieldset
              className="content-edit__locale"
              key={text.locale}
              data-testid={prefix}
              lang={text.locale}
            >
              <legend className="content-edit__legend">
                {t(`app.locale.${text.locale}`)}{' '}
                <Badge>{t(published ? 'content.status.published' : 'content.status.draft')}</Badge>
              </legend>

              <Field
                id={`${prefix}-title`}
                label={t('content.field.title')}
                error={errors[`${text.locale}.title`]}
              >
                {(props) => (
                  <input
                    {...props}
                    className="admin-form__control"
                    dir="auto"
                    value={text.title}
                    onChange={(event) => update(text.locale, { title: event.target.value })}
                  />
                )}
              </Field>

              <Field
                id={`${prefix}-slug`}
                label={t('content.field.slug')}
                hint={
                  published && text.slug !== saved.slug
                    ? t('content.edit.slugWarning')
                    : t('content.field.slugHint')
                }
                error={errors[`${text.locale}.slug`]}
              >
                {(props) => (
                  <input
                    {...props}
                    className="admin-form__control"
                    dir="ltr"
                    value={text.slug}
                    onChange={(event) => update(text.locale, { slug: event.target.value })}
                  />
                )}
              </Field>

              <Field
                id={`${prefix}-body`}
                label={t('content.field.body')}
                hint={t('content.field.bodyHint')}
                error={errors[`${text.locale}.body`]}
              >
                {(props) => (
                  <textarea
                    {...props}
                    className="admin-form__control content-edit__body"
                    dir="auto"
                    rows={10}
                    value={text.body}
                    onChange={(event) => update(text.locale, { body: event.target.value })}
                  />
                )}
              </Field>

              <Button
                variant="secondary"
                disabled={busy}
                data-testid={`${prefix}-${published ? 'unpublish' : 'publish'}`}
                onClick={() =>
                  void run(() =>
                    setPagePublication(page.id, page.version, text.locale, !published, locale),
                  )
                }
              >
                {t(published ? 'content.action.unpublish' : 'content.action.publish')}
              </Button>
            </fieldset>
          )
        })}

        <div className="admin-form__actions">
          <Button type="submit" loading={busy} data-testid="content-save">
            {t('content.action.save')}
          </Button>
          {page.systemKey === null ? (
            <Button
              variant="text"
              disabled={busy}
              data-testid="content-delete"
              onClick={() => setConfirming(true)}
            >
              {t('content.action.delete')}
            </Button>
          ) : null}
        </div>
      </form>

      {/* R8 — silme kalıcıdır ve onay ister; vazgeçilirse hiçbir şey olmaz (AC-26). */}
      <Dialog
        open={confirming}
        onClose={() => setConfirming(false)}
        label={t('content.delete.title')}
        testId="content-delete-dialog"
      >
        <div className="content-dialog">
          <h2 className="content-dialog__title">{t('content.delete.title')}</h2>
          <p>{t('content.delete.body')}</p>
          <div className="admin-form__actions">
            <Button
              variant="secondary"
              data-testid="content-delete-cancel"
              onClick={() => setConfirming(false)}
            >
              {t('content.delete.cancel')}
            </Button>
            <Button
              data-testid="content-delete-confirm"
              onClick={async () => {
                setConfirming(false)

                try {
                  await deletePage(page.id, page.version, locale)
                  window.location.assign(adminPath('content'))
                } catch (cause) {
                  setBanner(isConflict(cause) ? { kind: 'conflict' } : { kind: 'failed' })
                  setReload((value) => value + 1)
                }
              }}
            >
              {t('content.delete.confirm')}
            </Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

/** Etiketli alan; hata kodu katalogdan çevrilir ve alan `aria-invalid` taşır (C5). */
function Field({
  id,
  label,
  hint,
  error,
  children,
}: {
  readonly id: string
  readonly label: string
  readonly hint?: string
  readonly error: string | undefined
  readonly children: (props: {
    readonly id: string
    readonly 'aria-invalid': boolean | undefined
    readonly 'aria-describedby': string | undefined
  }) => React.ReactNode
}) {
  const { t } = useLocale()
  const describedBy = [
    hint === undefined ? null : `${id}-hint`,
    error === undefined ? null : `${id}-error`,
  ]
    .filter((value) => value !== null)
    .join(' ')

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      {children({
        id,
        'aria-invalid': error === undefined ? undefined : true,
        'aria-describedby': describedBy || undefined,
      })}
      {hint === undefined ? null : (
        <span className="admin-form__hint" id={`${id}-hint`}>
          {hint}
        </span>
      )}
      {error === undefined ? null : (
        <span className="admin-form__field-error" id={`${id}-error`} data-testid={`${id}-error`}>
          {t(fieldErrorKey(error))}
        </span>
      )}
    </div>
  )
}
