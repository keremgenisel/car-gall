import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  createFaqEntry,
  createPage,
  deleteFaqEntry,
  fetchAdminFaq,
  fetchAdminPages,
  moveFaqEntry,
  saveFaqEntry,
  setFaqPublication,
  type AdminFaqEntry,
  type AdminPage,
  type FaqTextInput,
} from '../../../shared/api/contentAdmin'
import { adminPath, contentEditPath } from '../../../shared/lib/route'
import { Badge, Button, Dialog, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { fieldErrorKey, fieldErrorsOf, isConflict } from './contentErrors'

type Load<T> =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly value: T }

/**
 * `0017` R24 — panelin "İçerik" bölümü: dört sistem sayfası, serbest sayfalar ve SSS. Her
 * listenin kendi yükleniyor, boş ve hata durumu vardır (R23, AC-102–AC-105).
 */
export function ContentAdminPage() {
  const { locale, t } = useLocale()
  const [pages, setPages] = useState<Load<readonly AdminPage[]>>({ kind: 'loading' })
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchAdminPages(locale, controller.signal)
      .then((value) => setPages({ kind: 'ready', value }))
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setPages({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [locale, reload])

  return (
    <div className="admin-page" data-testid="content-admin">
      <div className="admin-page__header">
        <h1 className="admin-page__title">{t('content.admin.title')}</h1>
        <Button
          variant="secondary"
          data-testid="content-new-page"
          onClick={async () => {
            const id = await createPage([], locale)
            window.location.assign(contentEditPath(id))
          }}
        >
          {t('content.action.newPage')}
        </Button>
      </div>

      {/* `0022` D9 — ana sayfa metinleri sayfaların ve SSS'nin yanında bir girdi. */}
      <section className="content-admin__section" aria-labelledby="content-home-heading">
        <h2 className="content-admin__heading" id="content-home-heading">
          {t('content.home.section')}
        </h2>
        <ul className="content-admin__list">
          <li className="content-admin__row">
            <a
              className="content-admin__name"
              href={adminPath('content-home')}
              data-testid="content-home-link"
            >
              {t('content.home.entry')}
            </a>
            <span className="admin-form__hint">{t('content.home.entryHint')}</span>
          </li>
        </ul>
      </section>

      <PageList load={pages} onRetry={() => setReload((value) => value + 1)} />
      <FaqEditor />
    </div>
  )
}

function PageList({
  load,
  onRetry,
}: {
  readonly load: Load<readonly AdminPage[]>
  readonly onRetry: () => void
}) {
  const { t } = useLocale()

  if (load.kind === 'loading') {
    return (
      <div role="status" data-testid="content-pages-loading">
        <span className="visually-hidden">{t('content.admin.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <ErrorState
        testId="content-pages-error"
        title={t('content.admin.error.title')}
        description={t('content.admin.error.body')}
        retryLabel={t('content.admin.error.retry')}
        onRetry={onRetry}
      />
    )
  }

  const system = load.value.filter((page) => page.systemKey !== null)
  const free = load.value.filter((page) => page.systemKey === null)

  return (
    <>
      <section className="content-admin__section" aria-labelledby="content-system-heading">
        <h2 className="content-admin__heading" id="content-system-heading">
          {t('content.admin.system')}
        </h2>
        <ul className="content-admin__list" data-testid="content-system-pages">
          {system.map((page) => (
            <PageRow key={page.id} page={page} name={t(`content.system.${page.systemKey}`)} />
          ))}
        </ul>
      </section>

      <section className="content-admin__section" aria-labelledby="content-free-heading">
        <h2 className="content-admin__heading" id="content-free-heading">
          {t('content.admin.free')}
        </h2>
        {free.length === 0 ? (
          <p className="admin-form__hint" data-testid="content-free-empty">
            {t('content.admin.freeEmpty')}
          </p>
        ) : (
          <ul className="content-admin__list" data-testid="content-free-pages">
            {free.map((page) => (
              <PageRow
                key={page.id}
                page={page}
                name={
                  page.locales.find((item) => item.title.length > 0)?.title ??
                  t('content.admin.untitled')
                }
              />
            ))}
          </ul>
        )}
      </section>
    </>
  )
}

function PageRow({ page, name }: { readonly page: AdminPage; readonly name: string }) {
  const { t } = useLocale()

  return (
    <li
      className="content-admin__row"
      data-testid="content-page-row"
      data-system-key={page.systemKey ?? undefined}
    >
      <a className="content-admin__name" href={contentEditPath(page.id)} dir="auto">
        {name}
      </a>
      <span className="content-admin__statuses">
        {page.locales.map((item) => (
          <Badge key={item.locale}>
            {t(`app.locale.${item.locale}`)}:{' '}
            {t(item.status === 'published' ? 'content.status.published' : 'content.status.draft')}
          </Badge>
        ))}
      </span>
      {/* R9 — eksik diller yalnız etkin diller arasında sayılır (D29). */}
      {page.missingLocales.length === 0 ? null : (
        <span className="admin-form__hint" data-testid="content-row-missing">
          {t('content.admin.missing', {
            locales: page.missingLocales.map((item) => t(`app.locale.${item}`)).join(', '),
          })}
        </span>
      )}
    </li>
  )
}

/** `0017` R18 — SSS girdileri: dil başına metin ve yayın, global sıra, yukarı/aşağı. */
function FaqEditor() {
  const { locale, t } = useLocale()
  const [load, setLoad] = useState<Load<readonly AdminFaqEntry[]>>({ kind: 'loading' })
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchAdminFaq(locale, controller.signal)
      .then((value) => setLoad({ kind: 'ready', value }))
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setLoad({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [locale, reload])

  const refresh = () => setReload((value) => value + 1)

  return (
    <section
      className="content-admin__section"
      aria-labelledby="content-faq-heading"
      data-testid="content-faq-editor"
    >
      <div className="admin-page__header">
        <h2 className="content-admin__heading" id="content-faq-heading">
          {t('content.faq.title')}
        </h2>
        <Button
          variant="secondary"
          data-testid="content-faq-new"
          onClick={async () => {
            await createFaqEntry([], locale)
            refresh()
          }}
        >
          {t('content.faq.new')}
        </Button>
      </div>

      {load.kind === 'loading' ? (
        <div role="status" data-testid="content-faq-loading">
          <span className="visually-hidden">{t('content.admin.loading')}</span>
          <Skeleton shape="line" />
        </div>
      ) : load.kind === 'error' ? (
        <ErrorState
          testId="content-faq-error"
          title={t('content.admin.error.title')}
          description={t('content.admin.error.body')}
          retryLabel={t('content.admin.error.retry')}
          onRetry={refresh}
        />
      ) : load.value.length === 0 ? (
        <EmptyState
          testId="content-faq-empty"
          title={t('content.faq.emptyTitle')}
          description={t('content.faq.empty')}
        />
      ) : (
        <ol className="content-faq-editor" data-testid="content-faq-entries">
          {load.value.map((entry, index) => (
            <FaqEntryRow
              // Sürüm değişince (kayıt ya da çakışma sonrası yeniden yükleme) satır güncel
              // metinle yeniden kurulur; yalnız sıra değişince kurulmaz.
              key={`${entry.id}-${entry.version}`}
              entry={entry}
              first={index === 0}
              last={index === load.value.length - 1}
              onChanged={refresh}
            />
          ))}
        </ol>
      )}
    </section>
  )
}

function FaqEntryRow({
  entry,
  first,
  last,
  onChanged,
}: {
  readonly entry: AdminFaqEntry
  readonly first: boolean
  readonly last: boolean
  readonly onChanged: () => void
}) {
  const { locale, t } = useLocale()
  const [texts, setTexts] = useState<readonly FaqTextInput[]>(() =>
    entry.locales.map(({ locale: l, question, answer }) => ({ locale: l, question, answer })),
  )
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [conflict, setConflict] = useState(false)
  const [confirming, setConfirming] = useState(false)

  async function run(action: () => Promise<unknown>) {
    setErrors({})
    setConflict(false)

    try {
      await action()
      onChanged()
    } catch (cause) {
      if (isConflict(cause)) {
        setConflict(true)
        onChanged()
      } else {
        setErrors(fieldErrorsOf(cause))
      }
    }
  }

  const label =
    entry.locales.find((item) => item.question.length > 0)?.question ?? t('content.admin.untitled')

  return (
    <li
      className="content-faq-editor__entry"
      data-testid="content-faq-entry"
      data-entry-id={entry.id}
    >
      <div className="content-faq-editor__order">
        {/* D36 — uçtaki düğme devre dışı; sıra dönmez (AC-78). Klavyeyle erişilir (AC-80). */}
        <Button
          variant="text"
          disabled={first}
          aria-label={t('content.faq.up', { question: label })}
          data-testid="content-faq-up"
          onClick={() => void run(() => moveFaqEntry(entry.id, 'up', locale))}
        >
          ↑
        </Button>
        <Button
          variant="text"
          disabled={last}
          aria-label={t('content.faq.down', { question: label })}
          data-testid="content-faq-down"
          onClick={() => void run(() => moveFaqEntry(entry.id, 'down', locale))}
        >
          ↓
        </Button>
      </div>

      {conflict ? (
        <p className="admin-form__error-body" role="alert" data-testid="content-faq-conflict">
          {t('content.edit.conflict')}
        </p>
      ) : null}

      {entry.missingLocales.length === 0 ? null : (
        <span className="admin-form__hint" data-testid="content-faq-missing">
          {t('content.admin.missing', {
            locales: entry.missingLocales.map((item) => t(`app.locale.${item}`)).join(', '),
          })}
        </span>
      )}

      {texts.map((text) => {
        const saved = entry.locales.find((item) => item.locale === text.locale)!
        const published = saved.status === 'published'
        const id = `faq-${entry.id}-${text.locale}`

        return (
          <fieldset className="content-edit__locale" key={text.locale} lang={text.locale}>
            <legend className="content-edit__legend">
              {t(`app.locale.${text.locale}`)}{' '}
              <Badge>{t(published ? 'content.status.published' : 'content.status.draft')}</Badge>
            </legend>
            <label className="admin-form__label" htmlFor={`${id}-q`}>
              {t('content.field.question')}
            </label>
            <input
              id={`${id}-q`}
              className="admin-form__control"
              dir="auto"
              value={text.question}
              aria-invalid={errors[`${text.locale}.question`] === undefined ? undefined : true}
              onChange={(event) =>
                setTexts((items) =>
                  items.map((item) =>
                    item.locale === text.locale ? { ...item, question: event.target.value } : item,
                  ),
                )
              }
            />
            {errors[`${text.locale}.question`] === undefined ? null : (
              <span className="admin-form__field-error">
                {t(fieldErrorKey(errors[`${text.locale}.question`]!))}
              </span>
            )}
            <label className="admin-form__label" htmlFor={`${id}-a`}>
              {t('content.field.answer')}
            </label>
            <textarea
              id={`${id}-a`}
              className="admin-form__control"
              dir="auto"
              rows={3}
              value={text.answer}
              aria-invalid={errors[`${text.locale}.answer`] === undefined ? undefined : true}
              onChange={(event) =>
                setTexts((items) =>
                  items.map((item) =>
                    item.locale === text.locale ? { ...item, answer: event.target.value } : item,
                  ),
                )
              }
            />
            {errors[`${text.locale}.answer`] === undefined ? null : (
              <span className="admin-form__field-error">
                {t(fieldErrorKey(errors[`${text.locale}.answer`]!))}
              </span>
            )}
            <Button
              variant="text"
              onClick={() =>
                void run(() =>
                  setFaqPublication(entry.id, entry.version, text.locale, !published, locale),
                )
              }
            >
              {t(published ? 'content.action.unpublish' : 'content.action.publish')}
            </Button>
          </fieldset>
        )
      })}

      <div className="admin-form__actions">
        <Button
          variant="secondary"
          data-testid="content-faq-save"
          onClick={() => void run(() => saveFaqEntry(entry.id, entry.version, texts, locale))}
        >
          {t('content.action.save')}
        </Button>
        <Button variant="text" data-testid="content-faq-delete" onClick={() => setConfirming(true)}>
          {t('content.action.delete')}
        </Button>
      </div>

      <Dialog
        open={confirming}
        onClose={() => setConfirming(false)}
        label={t('content.delete.faqTitle')}
      >
        <div className="content-dialog">
          <h2 className="content-dialog__title">{t('content.delete.faqTitle')}</h2>
          <p>{t('content.delete.body')}</p>
          <div className="admin-form__actions">
            <Button variant="secondary" onClick={() => setConfirming(false)}>
              {t('content.delete.cancel')}
            </Button>
            <Button
              data-testid="content-faq-delete-confirm"
              onClick={() => {
                setConfirming(false)
                void run(() => deleteFaqEntry(entry.id, entry.version, locale))
              }}
            >
              {t('content.delete.confirm')}
            </Button>
          </div>
        </div>
      </Dialog>
    </li>
  )
}
