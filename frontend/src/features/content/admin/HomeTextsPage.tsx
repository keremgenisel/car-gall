import { useEffect, useState, type ReactNode } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  fetchHomeTexts,
  saveHomeTexts,
  type AdminHomeTexts,
  type HomeTextFieldKey,
  type HomeTextLocale,
} from '../../../shared/api/contentAdmin'
import { createTranslator } from '../../../shared/i18n/i18n'
import { directionOf, isAppLocale } from '../../../shared/lib/locale'
import { adminPath } from '../../../shared/lib/route'
import { Button, ErrorState, Skeleton } from '../../../shared/ui'
import { fieldErrorKey, fieldErrorsOf, isConflict } from './contentErrors'
import { codePointLength } from './homeTextCount'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly texts: AdminHomeTexts }

type Banner =
  | { readonly kind: 'none' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed' }

/** R1'in sırasıyla altı alan. */
const HOME_TEXT_FIELDS: readonly HomeTextFieldKey[] = [
  'introTitle',
  'introBody',
  'formTitle',
  'featuredTitle',
  'emptyTitle',
  'emptyBody',
]

/** R4 · D5 — başlık 120, açıklama 500 kod noktası; sunucunun sınırıyla aynı. */
const LIMITS: Readonly<Record<HomeTextFieldKey, number>> = {
  introTitle: 120,
  introBody: 500,
  formTitle: 120,
  featuredTitle: 120,
  emptyTitle: 120,
  emptyBody: 500,
}

/**
 * R2 — boş alanın ipucu o DİLİN varsayılan metnidir: vitrin kataloğundaki bugünkü metin (C2).
 * Anahtarlar vitrinin okuduğu anahtarlarla aynı (`HomePage`, `NarrowingForm`, `EmptyShowcase`).
 */
const DEFAULT_KEYS: Readonly<Record<HomeTextFieldKey, string>> = {
  introTitle: 'home.intro.title',
  introBody: 'home.intro.body',
  formTitle: 'home.form.title',
  featuredTitle: 'home.featured.title',
  emptyTitle: 'showcase.empty.showcase.title',
  emptyBody: 'showcase.empty.showcase.body',
}

const DESCRIPTIONS: ReadonlySet<HomeTextFieldKey> = new Set(['introBody', 'emptyBody'])

/**
 * `0022` R1–R10 — panelin İçerik bölümündeki ana sayfa metinleri. Etkin her dil kendi
 * alanında (sunucu yalnız etkin dilleri döner — AC-36); kaydet bütün dilleri tek istekte
 * yazar ve kaydetmek yayındır (D10). Her alan opsiyoneldir; boş alanın yerinde o dilin
 * varsayılan metni ipucu olarak durur (R2).
 *
 * Sayaç sunucuyla AYNI kuralla sayar — Unicode kod noktası (D15) — ve tarayıcının yerel
 * `maxLength`'i kullanılmaz: o UTF-16 birimi sayar ve emojili bir metni erken keserdi.
 *
 * Sürüm çakışmasında (`409`) ve sunucu hatasında yazılanlar ekranda KALIR (AC-26, AC-33);
 * güncel hâli yüklemek editörün açık seçimidir — `0017`'nin otomatik yeniden yüklemesi
 * burada yazılanları silerdi.
 */
export function HomeTextsPage() {
  const { locale, t } = useLocale()
  const [load, setLoad] = useState<Load>({ kind: 'loading' })
  const [texts, setTexts] = useState<readonly HomeTextLocale[]>([])
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [banner, setBanner] = useState<Banner>({ kind: 'none' })
  const [busy, setBusy] = useState(false)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchHomeTexts(locale, controller.signal)
      .then((value) => {
        setLoad({ kind: 'ready', texts: value })
        setTexts(value.locales)
      })
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setLoad({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [locale, reload])

  const title = t('content.home.title')
  const header = (
    <div className="admin-page__header">
      <h1 className="admin-page__title">{title}</h1>
      <a className="ui-button ui-button--text" href={adminPath('content')}>
        {t('content.edit.back')}
      </a>
    </div>
  )

  if (load.kind === 'loading') {
    return (
      <div className="admin-page" data-testid="home-texts-loading" role="status">
        {header}
        <span className="visually-hidden">{t('content.home.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <div className="admin-page">
        {header}
        <ErrorState
          testId="home-texts-error"
          title={t('content.home.error.title')}
          description={t('content.home.error.body')}
          retryLabel={t('content.admin.error.retry')}
          onRetry={() => setReload((value) => value + 1)}
        />
      </div>
    )
  }

  const version = load.texts.version

  async function save() {
    setBusy(true)
    setErrors({})
    setBanner({ kind: 'none' })

    try {
      await saveHomeTexts(version, texts, locale)
      setBanner({ kind: 'saved' })
      setReload((value) => value + 1)
    } catch (cause) {
      if (isConflict(cause)) {
        setBanner({ kind: 'conflict' })
      } else {
        const fields = fieldErrorsOf(cause)
        setErrors(fields)
        setBanner(Object.keys(fields).length === 0 ? { kind: 'failed' } : { kind: 'none' })
      }
    } finally {
      setBusy(false)
    }
  }

  function update(target: string, field: HomeTextFieldKey, value: string) {
    setTexts((current) =>
      current.map((item) =>
        item.locale === target ? { ...item, values: { ...item.values, [field]: value } } : item,
      ),
    )
  }

  return (
    <div className="admin-page" data-testid="home-texts">
      {header}
      <p className="admin-form__intro">{t('content.home.intro')}</p>

      {banner.kind === 'conflict' ? (
        <div className="admin-form__error" role="alert" data-testid="home-texts-conflict">
          <p className="admin-form__error-body">{t('content.home.conflict')}</p>
          <div className="admin-form__banner-action">
            <Button
              variant="secondary"
              data-testid="home-texts-reload"
              onClick={() => {
                setBanner({ kind: 'none' })
                setReload((value) => value + 1)
              }}
            >
              {t('content.home.reload')}
            </Button>
          </div>
        </div>
      ) : null}
      {banner.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="home-texts-failed">
          <p className="admin-form__error-body">{t('content.edit.failed')}</p>
        </div>
      ) : null}
      {banner.kind === 'saved' ? (
        <div className="admin-form__notice" role="status" data-testid="home-texts-saved">
          <p className="admin-form__notice-body">{t('content.edit.saved')}</p>
        </div>
      ) : null}

      <form
        className="admin-form content-edit"
        noValidate
        onSubmit={(event) => {
          event.preventDefault()
          void save()
        }}
      >
        {texts.map((text) => {
          const fieldLocale = isAppLocale(text.locale) ? text.locale : null
          const defaults = fieldLocale === null ? null : createTranslator(fieldLocale)
          const prefix = `home-texts-${text.locale}`

          return (
            <fieldset
              className="content-edit__locale"
              key={text.locale}
              data-testid={prefix}
              lang={text.locale}
              dir={fieldLocale === null ? undefined : directionOf(fieldLocale)}
            >
              <legend className="content-edit__legend">{t(`app.locale.${text.locale}`)}</legend>

              {HOME_TEXT_FIELDS.map((field) => (
                <HomeTextInput
                  key={field}
                  id={`${prefix}-${field}`}
                  label={t(`content.home.field.${field}`)}
                  placeholder={defaults === null ? '' : defaults(DEFAULT_KEYS[field])}
                  description={DESCRIPTIONS.has(field)}
                  limit={LIMITS[field]}
                  value={text.values[field]}
                  dir={fieldLocale === null ? 'auto' : directionOf(fieldLocale)}
                  error={errors[`${text.locale}.${field}`]}
                  onChange={(value) => update(text.locale, field, value)}
                />
              ))}
            </fieldset>
          )
        })}

        <div className="admin-form__actions">
          <Button type="submit" loading={busy} data-testid="home-texts-save">
            {t('content.action.save')}
          </Button>
        </div>
      </form>
    </div>
  )
}

/**
 * Etiketli alan: ipucu (varsayılan metin + boş bırakma açıklaması), kod noktası sayacı ve
 * sunucunun alan hatası. Hata kodu katalogdan çevrilir; alan `aria-invalid` taşır.
 */
function HomeTextInput({
  id,
  label,
  placeholder,
  description,
  limit,
  value,
  dir,
  error,
  onChange,
}: {
  readonly id: string
  readonly label: string
  readonly placeholder: string
  readonly description: boolean
  readonly limit: number
  readonly value: string
  readonly dir: 'ltr' | 'rtl' | 'auto'
  readonly error: string | undefined
  readonly onChange: (value: string) => void
}) {
  const { t } = useLocale()
  const count = codePointLength(value)
  const over = count > limit
  const describedBy = [`${id}-hint`, `${id}-counter`, error === undefined ? null : `${id}-error`]
    .filter((item) => item !== null)
    .join(' ')

  const common = {
    id,
    className: description ? 'admin-form__control content-edit__body' : 'admin-form__control',
    dir,
    value,
    placeholder,
    'aria-invalid': error === undefined && !over ? undefined : true,
    'aria-describedby': describedBy,
  } as const

  let control: ReactNode

  if (description) {
    control = <textarea {...common} rows={5} onChange={(event) => onChange(event.target.value)} />
  } else {
    control = <input {...common} onChange={(event) => onChange(event.target.value)} />
  }

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      {control}
      <span className="admin-form__hint" id={`${id}-hint`}>
        {t('content.home.emptyHint')}
      </span>
      <span
        className="admin-form__hint"
        id={`${id}-counter`}
        data-testid={`${id}-counter`}
        data-over={over || undefined}
      >
        {t('content.home.counter', { count, limit })}
      </span>
      {error === undefined ? null : (
        <span className="admin-form__field-error" id={`${id}-error`} data-testid={`${id}-error`}>
          {t(fieldErrorKey(error))}
        </span>
      )}
    </div>
  )
}
