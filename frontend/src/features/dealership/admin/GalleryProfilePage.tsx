import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import {
  fetchGalleryProfile,
  saveGalleryProfile,
  type AdminGallery,
} from '../../../shared/api/dealershipAdmin'
import { enabledLocales } from '../../../shared/lib/locale'
import { SOCIAL_PLATFORM_NAMES } from '../socialPlatforms'
import { Button, ErrorState, Skeleton } from '../../../shared/ui'
import { DealershipField } from './DealershipField'
import { fieldErrorsOf, isConflict, localizedPayload } from './dealershipForm'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly gallery: AdminGallery }

type Banner =
  | { readonly kind: 'none' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed' }

interface Values {
  readonly commercialName: string
  readonly taxNumber: string
  readonly tradeRegistryNumber: string
  readonly displayNames: Readonly<Record<string, string>>
  readonly socialLinks: Readonly<Record<string, string>>
}

function valuesOf(gallery: AdminGallery, locales: readonly string[]): Values {
  return {
    commercialName: gallery.commercialName,
    taxNumber: gallery.taxNumber ?? '',
    tradeRegistryNumber: gallery.tradeRegistryNumber ?? '',
    displayNames: Object.fromEntries(
      locales.map((code) => [code, gallery.displayNames[code] ?? '']),
    ),
    // `0024` AC-2 — kayıtlı hesap dolu, olmayan boş gelir; altı alan her zaman var (D5).
    socialLinks: Object.fromEntries(
      Object.keys(SOCIAL_PLATFORM_NAMES).map((code) => [code, gallery.socialLinks[code] ?? '']),
    ),
  }
}

/**
 * `0020` R1–R3a, R16, R18 — galeri profili. Ticari unvan tek dilli ve zorunlu; vergi ve sicil
 * numarası boşaltılabilir; görünen ad etkin her dil için ayrı bir alandır (AC-1).
 *
 * Kaydetme sunucu hatasıyla düşerse yazılan değerler EKRANDA KALIR (AC-71); eski sürüm
 * reddinde panel uyarır ve güncel hâli yükleme yolunu sunar, ama kendiliğinden yüklemez —
 * yöneticinin yazdığı değerler uyarıyla birlikte durur (AC-63).
 */
export function GalleryProfilePage() {
  const { locale, t } = useLocale()
  const locales = enabledLocales()
  const [load, setLoad] = useState<Load>({ kind: 'loading' })
  const [values, setValues] = useState<Values | null>(null)
  const [errors, setErrors] = useState<Readonly<Record<string, string>>>({})
  const [banner, setBanner] = useState<Banner>({ kind: 'none' })
  const [busy, setBusy] = useState(false)
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchGalleryProfile(locale, controller.signal)
      .then((gallery) => {
        setLoad({ kind: 'ready', gallery })
        setValues(valuesOf(gallery, enabledLocales()))
      })
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setLoad({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [locale, reload])

  const title = t('dealership.gallery.title')

  if (load.kind === 'error') {
    return (
      <GalleryError
        title={title}
        onRetry={() => {
          setLoad({ kind: 'loading' })
          setReload((value) => value + 1)
        }}
      />
    )
  }

  if (load.kind === 'loading' || values === null) {
    return (
      <div className="admin-page admin-page--narrow" data-testid="gallery-loading" role="status">
        <h1 className="admin-page__title">{title}</h1>
        <span className="visually-hidden">{t('dealership.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  const { gallery } = load
  const current = values

  async function save() {
    setBusy(true)
    setErrors({})
    setBanner({ kind: 'none' })

    try {
      await saveGalleryProfile(
        gallery.version,
        {
          commercialName: current.commercialName,
          taxNumber: current.taxNumber,
          tradeRegistryNumber: current.tradeRegistryNumber,
          displayNames: localizedPayload(current.displayNames),
          socialLinks: current.socialLinks,
        },
        locale,
      )
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

  return (
    <div className="admin-page admin-page--narrow" data-testid="gallery-profile">
      <h1 className="admin-page__title">{title}</h1>
      <p className="admin-form__intro">{t('dealership.gallery.intro')}</p>

      <Banners
        banner={banner}
        onReload={() => {
          setBanner({ kind: 'none' })
          setErrors({})
          setReload((value) => value + 1)
        }}
      />

      <form
        className="admin-form"
        noValidate
        onSubmit={(event) => {
          event.preventDefault()
          void save()
        }}
      >
        <DealershipField
          id="gallery-commercial-name"
          label={t('dealership.gallery.field.commercialName')}
          hint={t('dealership.gallery.field.commercialNameHint')}
          error={errors['commercialName']}
        >
          {(props) => (
            <input
              {...props}
              className="admin-form__control"
              dir="auto"
              value={current.commercialName}
              onChange={(event) => setValues({ ...current, commercialName: event.target.value })}
            />
          )}
        </DealershipField>

        <DealershipField
          id="gallery-tax-number"
          label={t('dealership.gallery.field.taxNumber')}
          error={errors['taxNumber']}
        >
          {(props) => (
            <input
              {...props}
              className="admin-form__control"
              dir="ltr"
              value={current.taxNumber}
              onChange={(event) => setValues({ ...current, taxNumber: event.target.value })}
            />
          )}
        </DealershipField>

        <DealershipField
          id="gallery-trade-registry-number"
          label={t('dealership.gallery.field.tradeRegistryNumber')}
          error={errors['tradeRegistryNumber']}
        >
          {(props) => (
            <input
              {...props}
              className="admin-form__control"
              dir="ltr"
              value={current.tradeRegistryNumber}
              onChange={(event) =>
                setValues({ ...current, tradeRegistryNumber: event.target.value })
              }
            />
          )}
        </DealershipField>

        <fieldset className="admin-form__group" data-testid="gallery-display-names">
          <legend className="admin-form__label">{t('dealership.gallery.field.displayName')}</legend>
          <p className="admin-form__hint">{t('dealership.gallery.field.displayNameHint')}</p>
          {locales.map((code) => (
            <DealershipField
              key={code}
              id={`gallery-display-name-${code}`}
              label={t(`app.locale.${code}`)}
              error={errors[`displayName.${code}`]}
            >
              {(props) => (
                <input
                  {...props}
                  className="admin-form__control"
                  dir="auto"
                  lang={code}
                  value={current.displayNames[code] ?? ''}
                  onChange={(event) =>
                    setValues({
                      ...current,
                      displayNames: { ...current.displayNames, [code]: event.target.value },
                    })
                  }
                />
              )}
            </DealershipField>
          ))}
        </fieldset>

        {/*
          `0024` R1 · D5 — sosyal hesaplar: D1'in sırasıyla altı alan, etiket platformun adı
          (çevrilmez). "Ekle"/"kaldır" yok: alanı boşaltmak siler. Adres soldan sağa yazılır ve
          adres klavyesi açar; hata alanın altında, çevrilmiş metinle (AC-19).
        */}
        <fieldset className="admin-form__group" data-testid="gallery-social-links">
          <legend className="admin-form__label">{t('dealership.gallery.social.title')}</legend>
          <p className="admin-form__hint">{t('dealership.gallery.social.hint')}</p>
          {Object.entries(SOCIAL_PLATFORM_NAMES).map(([code, name]) => (
            <DealershipField
              key={code}
              id={`gallery-social-${code}`}
              label={name}
              error={errors[`socialLinks.${code}`]}
            >
              {(props) => (
                <input
                  {...props}
                  className="admin-form__control"
                  type="url"
                  inputMode="url"
                  dir="ltr"
                  autoComplete="url"
                  value={current.socialLinks[code] ?? ''}
                  onChange={(event) =>
                    setValues({
                      ...current,
                      socialLinks: { ...current.socialLinks, [code]: event.target.value },
                    })
                  }
                />
              )}
            </DealershipField>
          ))}
        </fieldset>

        <div className="admin-form__actions">
          <Button type="submit" loading={busy} data-testid="gallery-save">
            {t('dealership.action.save')}
          </Button>
        </div>
      </form>
    </div>
  )
}

function GalleryError({
  title,
  onRetry,
}: {
  readonly title: string
  readonly onRetry: () => void
}) {
  const { t } = useLocale()

  return (
    <div className="admin-page admin-page--narrow">
      <h1 className="admin-page__title">{title}</h1>
      <ErrorState
        testId="gallery-error"
        title={t('dealership.error.load.title')}
        description={t('dealership.error.load.body')}
        retryLabel={t('dealership.error.load.retry')}
        onRetry={onRetry}
      />
    </div>
  )
}

/**
 * Kaydetmenin sonucu. Çakışma bir UYARIDIR ve güncel hâli yükleme yolu sunar; form değerleri
 * ancak yönetici o yolu seçerse değişir (AC-63).
 */
function Banners({ banner, onReload }: { readonly banner: Banner; readonly onReload: () => void }) {
  const { t } = useLocale()

  if (banner.kind === 'conflict') {
    return (
      <div className="admin-form__error" role="alert" data-testid="dealership-conflict">
        <p className="admin-form__error-body">{t('dealership.conflict.body')}</p>
        <div className="admin-form__banner-action">
          <Button variant="secondary" data-testid="dealership-conflict-reload" onClick={onReload}>
            {t('dealership.conflict.reload')}
          </Button>
        </div>
      </div>
    )
  }

  if (banner.kind === 'failed') {
    return (
      <div className="admin-form__error" role="alert" data-testid="dealership-save-failed">
        <p className="admin-form__error-body">{t('dealership.error.save')}</p>
      </div>
    )
  }

  if (banner.kind === 'saved') {
    return (
      <div className="admin-form__notice" role="status" data-testid="dealership-saved">
        <p className="admin-form__notice-body">{t('dealership.saved')}</p>
      </div>
    )
  }

  return null
}
