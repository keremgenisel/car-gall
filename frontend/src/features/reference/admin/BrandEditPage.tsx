import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  fetchAdminBrand,
  runBrandAction,
  saveBrand,
  setBrandShowOnHome,
  type AdminBrandDetail,
} from '../../../shared/api/referenceAdmin'
import { enabledLocales } from '../../../shared/lib/locale'
import { adminPath } from '../../../shared/lib/route'
import { Badge, Button, ErrorState, Skeleton } from '../../../shared/ui'
import { DisplayNameFields } from './DisplayNameFields'
import { ModelSection } from './ModelSection'
import { ReferenceBanner, type BannerState } from './ReferenceBanner'
import { ReferenceField } from './ReferenceField'
import { displayNamesOf, displayNamesPayload, rejectionOf } from './referenceForm'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly detail: AdminBrandDetail }

interface Values {
  readonly canonicalName: string
  readonly displayNames: Readonly<Record<string, string>>
}

/**
 * `0021` R1, R5–R7 — bir markanın ekranı: kanonik ad ve dil başına görünen ad (AC-12, AC-13),
 * pasif yapma ve yeniden etkinleştirme (AC-14, AC-16), "ana sayfada göster" (R7) ve markanın
 * modelleri (AC-17…AC-22). Kısaltma salt okunur basılır: kanonik addan bir kez üretilir ve
 * ad düzeltilince değişmez (D10).
 *
 * Kaydetme reddedilirse yazılan değerler ekranda kalır; eski sürüm reddinde panel güncel hâli
 * yükleme yolunu sunar ama kendiliğinden yüklemez (`0020` deseni).
 */
export function BrandEditPage({ brandId }: { readonly brandId: string }) {
  const { locale, t } = useLocale()
  const locales = enabledLocales()
  const [load, setLoad] = useState<Load>({ kind: 'loading' })
  const [values, setValues] = useState<Values | null>(null)
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [banner, setBanner] = useState<BannerState>({ kind: 'none' })
  const [busy, setBusy] = useState(false)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchAdminBrand(brandId, locale, controller.signal)
      .then((detail) => {
        setLoad({ kind: 'ready', detail })
        setValues({
          canonicalName: detail.brand.canonicalName,
          displayNames: displayNamesOf(detail.brand.displayNames, enabledLocales()),
        })
      })
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setLoad({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [brandId, locale, reload])

  const refresh = () => setReload((value) => value + 1)
  const back = (
    <a className="admin-page__back" href={adminPath('brands')}>
      {t('reference.back')}
    </a>
  )

  if (load.kind === 'error') {
    return (
      <div className="admin-page admin-page--narrow">
        {back}
        <ErrorState
          testId="brand-error"
          title={t('reference.error.load.title')}
          description={t('reference.error.load.body')}
          retryLabel={t('reference.error.load.retry')}
          onRetry={() => {
            setLoad({ kind: 'loading' })
            refresh()
          }}
        />
      </div>
    )
  }

  if (load.kind === 'loading' || values === null) {
    return (
      <div className="admin-page admin-page--narrow" data-testid="brand-loading" role="status">
        {back}
        <span className="visually-hidden">{t('reference.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  const { brand, models } = load.detail
  const current = values

  async function run(action: () => Promise<unknown>, field?: boolean) {
    setBusy(true)
    setErrors({})
    setBanner({ kind: 'none' })

    try {
      await action()
      setBanner({ kind: 'saved', messageKey: 'reference.saved' })
      refresh()
    } catch (cause) {
      const rejection = rejectionOf(cause, locale)
      if (rejection.kind === 'field' && field === true) {
        setErrors({ [rejection.field]: rejection.messageKey })
      } else {
        setBanner(
          rejection.kind === 'field'
            ? { kind: 'failed', messageKey: rejection.messageKey }
            : rejection,
        )
      }
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="admin-page admin-page--narrow" data-testid="brand-page">
      {back}
      <div className="admin-page__header">
        <h1 className="admin-page__title" dir="ltr">
          {brand.canonicalName}
        </h1>
        <Badge>{t(brand.isActive ? 'reference.status.active' : 'reference.status.inactive')}</Badge>
      </div>
      <p className="admin-form__hint" data-testid="brand-slug">
        {t('reference.brand.slug', { slug: brand.slug })}
      </p>

      <ReferenceBanner
        state={banner}
        onReload={() => {
          setBanner({ kind: 'none' })
          setErrors({})
          refresh()
        }}
      />

      <form
        className="admin-form"
        noValidate
        data-testid="brand-form"
        onSubmit={(event) => {
          event.preventDefault()
          void run(
            () =>
              saveBrand(
                brand.id,
                brand.version,
                {
                  canonicalName: current.canonicalName,
                  displayNames: displayNamesPayload(current.displayNames, locales),
                },
                locale,
              ),
            true,
          )
        }}
      >
        <ReferenceField
          id="brand-name"
          label={t('reference.field.canonicalName')}
          hint={t('reference.field.brandCanonicalNameHint')}
          messageKey={errors['canonicalName']}
          value={current.canonicalName}
          dir="ltr"
          onChange={(canonicalName) => setValues({ ...current, canonicalName })}
        />
        <DisplayNameFields
          idPrefix="brand-display-name"
          locales={locales}
          values={current.displayNames}
          errors={errors}
          onChange={(displayNames) => setValues({ ...current, displayNames })}
        />
        <div className="admin-form__actions">
          <Button type="submit" loading={busy} data-testid="brand-save">
            {t('reference.brand.save')}
          </Button>
        </div>
      </form>

      <div className="admin-form__field">
        <label className="reference-check">
          <input
            type="checkbox"
            checked={brand.showOnHome}
            disabled={busy}
            data-testid="brand-home"
            onChange={() =>
              void run(() => setBrandShowOnHome(brand.id, brand.version, !brand.showOnHome, locale))
            }
          />
          {t('reference.brand.home')}
        </label>
        <span className="admin-form__hint">{t('reference.brand.homeHint')}</span>
      </div>

      <div className="admin-form__actions">
        <Button
          variant="secondary"
          loading={busy}
          data-testid="brand-toggle-active"
          onClick={() =>
            void run(() =>
              runBrandAction(
                brand.id,
                brand.version,
                brand.isActive ? 'deactivate' : 'activate',
                locale,
              ),
            )
          }
        >
          {t(brand.isActive ? 'reference.action.deactivate' : 'reference.action.activate')}
        </Button>
      </div>

      <ModelSection
        brandId={brand.id}
        models={models}
        locales={locales}
        onChanged={refresh}
        onRejected={setBanner}
      />
    </div>
  )
}
