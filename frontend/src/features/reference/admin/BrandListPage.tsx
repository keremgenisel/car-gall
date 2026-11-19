import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  createBrand,
  fetchAdminBrands,
  setBrandShowOnHome,
  type AdminBrand,
} from '../../../shared/api/referenceAdmin'
import { brandEditPath } from '../../../shared/lib/route'
import { Badge, Button, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { ReferenceBanner, type BannerState } from './ReferenceBanner'
import { ReferenceField } from './ReferenceField'
import { rejectionOf } from './referenceForm'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly brands: readonly AdminBrand[] }

/**
 * `0021` R1, R5, R7 · AC-6 — bütün markalar, aktif ve pasif, kanonik adları, durumları ve
 * "ana sayfada göster" işaretleriyle. Yeni marka bu ekrandan eklenir (AC-7); ad ve modeller
 * markanın kendi ekranında düzenlenir.
 *
 * Durum yalnız renkle anlatılmaz; rozet metni taşır (docs/frontend.md § 7). Kanonik ad ve
 * kısaltma Latin'dir ve Arapça panelde de soldan sağa basılır (AC-58).
 */
export function BrandListPage() {
  const { locale, t } = useLocale()
  const [load, setLoad] = useState<Load>({ kind: 'loading' })
  const [reload, setReload] = useState(0)
  const [name, setName] = useState('')
  const [nameError, setNameError] = useState<string | undefined>()
  const [banner, setBanner] = useState<BannerState>({ kind: 'none' })
  const [busy, setBusy] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    fetchAdminBrands(locale, controller.signal)
      .then((brands) => setLoad({ kind: 'ready', brands }))
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setLoad({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [locale, reload])

  const refresh = () => setReload((value) => value + 1)

  async function add() {
    setBusy(true)
    setNameError(undefined)
    setBanner({ kind: 'none' })

    try {
      await createBrand({ canonicalName: name }, locale)
      setName('')
      setBanner({ kind: 'saved', messageKey: 'reference.brands.added' })
      refresh()
    } catch (cause) {
      const rejection = rejectionOf(cause, locale)
      if (rejection.kind === 'field') {
        setNameError(rejection.messageKey)
      } else {
        setBanner(rejection)
      }
    } finally {
      setBusy(false)
    }
  }

  async function toggleHome(brand: AdminBrand) {
    setBanner({ kind: 'none' })

    try {
      await setBrandShowOnHome(brand.id, brand.version, !brand.showOnHome, locale)
      refresh()
    } catch (cause) {
      const rejection = rejectionOf(cause, locale)
      setBanner(
        rejection.kind === 'field'
          ? { kind: 'failed', messageKey: rejection.messageKey }
          : rejection,
      )
    }
  }

  return (
    <div className="admin-page" data-testid="brands-page">
      <h1 className="admin-page__title">{t('reference.brands.title')}</h1>
      <p className="admin-form__intro">{t('reference.brands.intro')}</p>

      <ReferenceBanner
        state={banner}
        onReload={() => {
          setBanner({ kind: 'none' })
          refresh()
        }}
      />

      <form
        className="admin-form reference-new"
        noValidate
        data-testid="brand-new-form"
        onSubmit={(event) => {
          event.preventDefault()
          void add()
        }}
      >
        <h2 className="admin-form__label">{t('reference.brands.new.title')}</h2>
        <ReferenceField
          id="brand-new-name"
          label={t('reference.field.canonicalName')}
          hint={t('reference.field.brandCanonicalNameHint')}
          messageKey={nameError}
          value={name}
          dir="ltr"
          onChange={setName}
        />
        <div className="admin-form__actions">
          <Button type="submit" loading={busy} data-testid="brand-new-submit">
            {t('reference.brands.new.submit')}
          </Button>
        </div>
      </form>

      <BrandList
        load={load}
        onRetry={() => {
          setLoad({ kind: 'loading' })
          refresh()
        }}
        onToggleHome={(brand) => void toggleHome(brand)}
      />
    </div>
  )
}

function BrandList({
  load,
  onRetry,
  onToggleHome,
}: {
  readonly load: Load
  readonly onRetry: () => void
  readonly onToggleHome: (brand: AdminBrand) => void
}) {
  const { t } = useLocale()

  if (load.kind === 'loading') {
    return (
      <div data-testid="brands-loading" role="status">
        <span className="visually-hidden">{t('reference.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <ErrorState
        testId="brands-error"
        title={t('reference.error.load.title')}
        description={t('reference.error.load.body')}
        retryLabel={t('reference.error.load.retry')}
        onRetry={onRetry}
      />
    )
  }

  if (load.brands.length === 0) {
    return (
      <EmptyState
        testId="brands-empty"
        title={t('reference.brands.empty.title')}
        description={t('reference.brands.empty.body')}
      />
    )
  }

  return (
    <div className="admin-table__scroll">
      <table className="admin-table" data-testid="brands-table">
        <thead>
          <tr>
            <th scope="col">{t('reference.column.name')}</th>
            <th scope="col">{t('reference.column.slug')}</th>
            <th scope="col">{t('reference.column.status')}</th>
            <th scope="col">{t('reference.column.home')}</th>
            <th scope="col">{t('reference.column.models')}</th>
          </tr>
        </thead>
        <tbody>
          {load.brands.map((brand) => (
            <tr key={brand.id} data-testid="brand-row">
              <td>
                <a href={brandEditPath(brand.id)} dir="ltr" data-testid="brand-link">
                  {brand.canonicalName}
                </a>
              </td>
              <td dir="ltr" className="reference-slug">
                {brand.slug}
              </td>
              <td>
                <Badge>
                  {t(brand.isActive ? 'reference.status.active' : 'reference.status.inactive')}
                </Badge>
              </td>
              <td>
                <input
                  type="checkbox"
                  checked={brand.showOnHome}
                  aria-label={t('reference.brands.homeToggle', { name: brand.canonicalName })}
                  data-testid="brand-home-toggle"
                  onChange={() => onToggleHome(brand)}
                />
              </td>
              <td>{t('reference.brands.modelCount', { count: brand.modelCount })}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
