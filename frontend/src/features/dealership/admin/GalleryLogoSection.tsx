import { useEffect, useRef, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import { assignGalleryLogo, removeGalleryLogo } from '../../../shared/api/dealershipAdmin'
import {
  fetchMediaAsset,
  retryMediaAsset,
  uploadMediaAsset,
  type MediaAssetState,
} from '../../../shared/api/mediaAdmin'
import { catalogKeys } from '../../../shared/i18n/i18n'
import { Button, Skeleton } from '../../../shared/ui'
import { isConflict } from './dealershipForm'
import './galleryLogo.css'

/** Durum yoklamasının aralığı; işleme birkaç saniye sürer (D8: bölüm kendiliğinden yeniler). */
export const LOGO_POLL_MS = 2000

/** Okunan varlık hâli; hangi kimlik için okunduğuyla birlikte (eski kimliğin hâli gösterilmez). */
interface Read {
  readonly id: string
  readonly tick: number
  readonly state: MediaAssetState
}

/**
 * `0026` R1 · R3 · D8 · D12 · D13 — galeri profili ekranının "Logo" bölümü.
 *
 * - Logo yokken "Logo yükle" seçicisi (yalnız kabul edilen türler) ve D12'nin yardım metni.
 * - Dosya seçilince `kind=logo` ile yüklenir; `202`'den HEMEN sonra profile bağlanır, hazır olması
 *   beklenmez. İşlenirken "işleniyor" ve bölüm durumu kendiliğinden yoklar.
 * - Hazırken `logo-small` önizlemesi, "Değiştir" ve "Kaldır"; başarısızsa çevrilmiş neden ve
 *   "Tekrar dene".
 * - Atama ve kaldırma okunan sürümle gider; dönen sürüm `onVersion` ile ekranın sürümü olur, yani
 *   aynı ekrandaki profil kaydetmesi `409` vermez ve kaydedilmemiş form değerleri yerinde kalır.
 * - `0026` QA 2 BULGU-1 — sürüm bir ref'ten okunur: yükleme sürerken profil kaydedilirse atama
 *   yüklemenin BAŞLADIĞI andaki değil, o anki sürümle gider. Profil kaydı ve ardından gelen yeniden
 *   okuma sürerken (`disabled`) dosya seçici ve düğmeler kapalıdır.
 */
export function GalleryLogoSection({
  logoMediaAssetId,
  version,
  onChange,
  disabled = false,
}: {
  readonly logoMediaAssetId: string | null
  readonly version: number
  readonly onChange: (logoMediaAssetId: string | null, version: number) => void
  readonly disabled?: boolean
}) {
  const { locale, t } = useLocale()
  const input = useRef<HTMLInputElement>(null)
  const [read, setRead] = useState<Read | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [tick, setTick] = useState(0)
  const latestVersion = useRef(version)

  useEffect(() => {
    latestVersion.current = version
  }, [version])

  useEffect(() => {
    if (logoMediaAssetId === null) {
      return
    }

    const controller = new AbortController()
    let timer: ReturnType<typeof setTimeout> | undefined

    fetchMediaAsset(logoMediaAssetId, locale, controller.signal)
      .then((state) => {
        setRead({ id: logoMediaAssetId, tick, state })
        if (state.status === 'uploaded' || state.status === 'processing') {
          timer = setTimeout(() => setTick((value) => value + 1), LOGO_POLL_MS)
        }
      })
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setError('vehicle.media.problem.generic')
        }
      })

    return () => {
      controller.abort()
      clearTimeout(timer)
    }
  }, [logoMediaAssetId, locale, tick])

  function problem(cause: unknown): string {
    if (isConflict(cause)) {
      return 'dealership.conflict.body'
    }
    const code = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'
    const candidate = `vehicle.media.problem.${code.replace(/^media\./, '').replace(/\./g, '_')}`
    return catalogKeys(locale).includes(candidate) ? candidate : 'vehicle.media.problem.generic'
  }

  async function upload(file: File) {
    setBusy(true)
    setError(null)
    try {
      const uploaded = await uploadMediaAsset(file, locale, undefined, 'logo')
      const next = await assignGalleryLogo(uploaded.id, latestVersion.current, locale)
      onChange(uploaded.id, next)
    } catch (cause) {
      setError(problem(cause))
    } finally {
      setBusy(false)
      if (input.current !== null) {
        input.current.value = ''
      }
    }
  }

  async function remove() {
    setBusy(true)
    setError(null)
    try {
      onChange(null, await removeGalleryLogo(latestVersion.current, locale))
    } catch (cause) {
      setError(problem(cause))
    } finally {
      setBusy(false)
    }
  }

  async function retry() {
    if (logoMediaAssetId === null) {
      return
    }
    setBusy(true)
    setError(null)
    try {
      await retryMediaAsset(logoMediaAssetId, locale)
      setTick((value) => value + 1)
    } catch (cause) {
      setError(problem(cause))
    } finally {
      setBusy(false)
    }
  }

  // Kimlik değişince önceki hâl gösterilmez: yeni kimliğin ilk okuması gelene kadar iskelet.
  const state = logoMediaAssetId !== null && read?.id === logoMediaAssetId ? read.state : null
  const loading = logoMediaAssetId !== null && state === null
  const small = state?.derivatives.find((turev) => turev.variant === 'logo-small') ?? null
  const failureKey =
    state?.failureReason === null || state === null
      ? null
      : catalogKeys(locale).includes(`vehicle.media.failure.${state.failureReason}`)
        ? `vehicle.media.failure.${state.failureReason}`
        : 'vehicle.media.failure.generic'

  return (
    <fieldset className="admin-form__group gallery-logo" data-testid="gallery-logo">
      <legend className="admin-form__label">{t('dealership.gallery.logo.title')}</legend>
      <p className="admin-form__hint" id="gallery-logo-hint">
        {t('dealership.gallery.logo.hint')}
      </p>

      {loading ? <Skeleton shape="line" /> : null}

      {state !== null && (state.status === 'uploaded' || state.status === 'processing') ? (
        <p role="status" data-testid="gallery-logo-processing">
          {t('dealership.gallery.logo.processing')}
        </p>
      ) : null}

      {state?.status === 'ready' && small !== null ? (
        <div className="gallery-logo__preview" data-testid="gallery-logo-preview">
          <img
            className="gallery-logo__image"
            src={small.url}
            width={small.width}
            height={small.height}
            alt=""
          />
        </div>
      ) : null}

      {state?.status === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="gallery-logo-failed">
          <p className="admin-form__error-body">
            {t(failureKey ?? 'vehicle.media.failure.generic')}
          </p>
          <Button
            variant="secondary"
            loading={busy}
            disabled={disabled}
            onClick={() => void retry()}
            data-testid="gallery-logo-retry"
          >
            {t('dealership.gallery.logo.retry')}
          </Button>
        </div>
      ) : null}

      <div className="gallery-logo__actions">
        <label className="admin-form__label" htmlFor="gallery-logo-file">
          {t(
            logoMediaAssetId === null
              ? 'dealership.gallery.logo.upload'
              : 'dealership.gallery.logo.change',
          )}
        </label>
        <input
          ref={input}
          id="gallery-logo-file"
          className="admin-form__control"
          type="file"
          accept="image/jpeg,image/png,image/webp"
          aria-describedby="gallery-logo-hint"
          disabled={busy || disabled}
          data-testid="gallery-logo-file"
          onChange={(event) => {
            const file = event.target.files?.[0]
            if (file !== undefined) {
              void upload(file)
            }
          }}
        />
        {logoMediaAssetId === null ? null : (
          <Button
            variant="secondary"
            loading={busy}
            disabled={disabled}
            onClick={() => void remove()}
            data-testid="gallery-logo-remove"
          >
            {t('dealership.gallery.logo.remove')}
          </Button>
        )}
      </div>

      {error === null ? null : (
        <p className="admin-form__field-error" role="alert" data-testid="gallery-logo-error">
          {t(error)}
        </p>
      )}
    </fieldset>
  )
}
