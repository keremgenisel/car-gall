import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { fetchSaleReversals } from '../../../shared/api/catalogAdmin'
import type { SaleReversalRecord } from '../../../shared/api/types'
import { formatDateTime } from '../../../shared/i18n/format'
import { Badge, Button, Skeleton } from '../../../shared/ui'
import { codeKey } from './vehicleFormState'

type State =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly records: readonly SaleReversalRecord[] }

/**
 * `0023` **R7** — ilanın satış geri alma kayıtları, aracın panel ekranında. Yalnız Yöneticiye
 * basılır; uç başka role `403` döner ve ekran bu okumayı ona hiç yapmaz.
 *
 * <p>Sıra SUNUCUDAN gelir (en yenisi üstte, **AC-20**); ekran yeniden sıralamaz — iki yerde
 * sıralamak, ikisinin bir gün ayrışması demekti.</p>
 *
 * <p>Üç durum (`docs/frontend.md` § 3): yükleniyor, boş ("bu ilanın satışı hiç geri
 * alınmadı"), hata + tekrar dene. Kayıt yalnız OKUNUR: düzenleme ya da silme düğmesi yok
 * (**AC-21**).</p>
 */
export function SaleReversalHistory({
  vehicleId,
  refreshToken,
}: {
  readonly vehicleId: string
  /** Geri alma başarılı olunca artar; liste yeniden okunur. */
  readonly refreshToken: number
}) {
  const { locale, t } = useLocale()
  const [loaded, setLoaded] = useState<{ readonly key: string; readonly value: State } | null>(null)
  const [retryToken, setRetryToken] = useState(0)

  // Tazeleme sırasında eldeki liste gösterilmeye devam eder; yalnız araç ya da dil
  // değişince iskelet basılır (`VehicleEditPage`'in aynı kararı).
  const dataKey = `${locale}|${vehicleId}`
  const state: State =
    loaded !== null && loaded.key === dataKey ? loaded.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    fetchSaleReversals(vehicleId, locale, controller.signal)
      .then((overview) =>
        setLoaded({ key: dataKey, value: { kind: 'ready', records: overview.records } }),
      )
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setLoaded({ key: dataKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [vehicleId, locale, dataKey, refreshToken, retryToken])

  return (
    <section
      className="vehicle-actions sale-reversals"
      data-testid="sale-reversals"
      aria-labelledby="sale-reversals-title"
    >
      <h2 className="admin-form__label" id="sale-reversals-title">
        {t('vehicle.saleRevert.history.title')}
      </h2>

      {state.kind === 'loading' ? (
        <div data-testid="sale-reversals-loading">
          <Skeleton shape="line" />
        </div>
      ) : null}

      {state.kind === 'error' ? (
        <div className="admin-form__error" role="alert" data-testid="sale-reversals-error">
          <p className="admin-form__error-title">{t('vehicle.saleRevert.history.error')}</p>
          <div className="admin-form__banner-action">
            <Button variant="secondary" onClick={() => setRetryToken((value) => value + 1)}>
              {t('vehicle.action.retry')}
            </Button>
          </div>
        </div>
      ) : null}

      {state.kind === 'ready' && state.records.length === 0 ? (
        <p className="admin-form__hint" data-testid="sale-reversals-empty">
          {t('vehicle.saleRevert.history.empty')}
        </p>
      ) : null}

      {state.kind === 'ready' && state.records.length > 0 ? (
        <ol className="sale-reversals__list">
          {state.records.map((record) => (
            <li key={record.id} className="sale-reversals__item" data-testid="sale-reversal-record">
              <p className="sale-reversals__meta">
                <span className="sale-reversals__staff" dir="auto">
                  {record.staffDisplayName}
                </span>
                <time dateTime={record.revertedAt}>
                  {formatDateTime(record.revertedAt, locale)}
                </time>
                <Badge>
                  {t('vehicle.saleRevert.history.restored', {
                    status: t(codeKey('vehicle.status', record.restoredStatus)),
                  })}
                </Badge>
              </p>
              <p className="sale-reversals__reason" dir="auto">
                {record.reason}
              </p>
            </li>
          ))}
        </ol>
      ) : null}
    </section>
  )
}
