import { useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import { applyBulkListingAction } from '../../../shared/api/catalogAdmin'
import { BULK_LISTING_OPERATIONS, type BulkListingOperation } from '../../../shared/api/types'
import { Button } from '../../../shared/ui'
import { messageKey } from './listingActionState'
import { summarizeBulkResult, type BulkSummary } from './bulkActionState'

type Durum =
  | { readonly kind: 'idle' }
  | { readonly kind: 'working' }
  | {
      readonly kind: 'done'
      readonly operation: BulkListingOperation
      readonly summary: BulkSummary
    }
  | {
      readonly kind: 'failed'
      /**
       * İSTEK düzeyi hata (**D5**): boş seçim ya da sınır aşımı. Satır düzeyindeki
       * başarısızlıklar burada değil, `summary.failures` içinde yaşıyor — ikisini aynı
       * kutuya koymak kısmi başarı kararını siler.
       */
      readonly messageKey: string
      readonly limit: string | undefined
    }

/**
 * `0012c` **R1** · **R6** — liste ekranının toplu eylem yüzeyi.
 *
 * <p><b>Dört ekran durumu:</b> boş seçim (düğme kapalı), uygulanıyor, **başarılı** ve
 * **kısmi başarı**. Son ikisi bu spec'in borcu (**D54**); listenin yükleniyor/boş/hata
 * durumları `0010b`'de kurulu.</p>
 *
 * <p><b>Kısmi başarı yuvarlanmıyor</b> (`docs/frontend.md` § 3): yirmi satırın on yedisi
 * uygulandıysa sonuç "başarılı" değildir. Uygulanmayanlar tek tek, <b>aracın adıyla ve
 * gerekçesiyle</b> listeleniyor.</p>
 *
 * <p><b>Altı eylem</b> (**D18**): rezerve etme ve rezervasyonu kaldırma burada yok —
 * rezervasyon bir müşteriye aittir.</p>
 */
export function BulkActionBar({
  selection,
  labelOf,
  onApplied,
}: {
  readonly selection: readonly string[]
  /** Kısmi başarı listesi aracın ADINI gösteriyor; kimlik personele bir şey söylemez. */
  readonly labelOf: (vehicleId: string) => string
  readonly onApplied: () => void
}) {
  const { locale, t } = useLocale()
  const [operation, setOperation] = useState<BulkListingOperation>('publish')
  const [durum, setDurum] = useState<Durum>({ kind: 'idle' })

  async function uygula() {
    setDurum({ kind: 'working' })

    try {
      const sonuc = await applyBulkListingAction(operation, selection, locale)

      setDurum({ kind: 'done', operation, summary: summarizeBulkResult(sonuc.results, locale) })
      onApplied()
    } catch (cause) {
      const hata = cause instanceof ApiError ? cause : null

      setDurum({
        kind: 'failed',
        messageKey: messageKey(hata?.errorCode ?? 'api.unavailable', locale),
        limit: hata?.parameters?.['limit'],
      })
    }
  }

  return (
    <section className="vehicle-bulk" data-testid="vehicle-bulk">
      <div className="vehicle-bulk__controls">
        <div className="admin-form__field">
          <label className="admin-form__label" htmlFor="vehicle-bulk-operation">
            {t('vehicle.bulk.operation')}
          </label>
          <select
            id="vehicle-bulk-operation"
            className="admin-form__control"
            value={operation}
            onChange={(event) => setOperation(event.target.value as BulkListingOperation)}
            data-testid="vehicle-bulk-operation"
          >
            {BULK_LISTING_OPERATIONS.map((item) => (
              <option key={item} value={item}>
                {t(`vehicle.action.${item}`)}
              </option>
            ))}
          </select>
        </div>

        <Button
          variant="primary"
          loading={durum.kind === 'working'}
          disabled={selection.length === 0}
          onClick={() => void uygula()}
          data-testid="vehicle-bulk-apply"
        >
          {durum.kind === 'working' ? t('vehicle.bulk.working') : t('vehicle.bulk.apply')}
        </Button>

        {/*
          Sayı ICU ile kuruluyor (C5, Yasak Listesi 31): Arapça'da altı çoğul biçimi var ve
          dize birleştirmeyle kurulan bir cümle orada yanlış olur.
        */}
        <p className="admin-form__hint" data-testid="vehicle-bulk-selection">
          {selection.length === 0
            ? t('vehicle.bulk.empty')
            : t('vehicle.bulk.selected', { count: selection.length })}
        </p>
      </div>

      {durum.kind === 'done' && durum.summary.outcome === 'success' ? (
        <p className="admin-form__notice-body" role="status" data-testid="vehicle-bulk-success">
          {t(`vehicle.bulk.success.${durum.operation}`, { count: durum.summary.appliedCount })}
        </p>
      ) : null}

      {durum.kind === 'done' && durum.summary.outcome === 'partial' ? (
        <div className="admin-form__error" role="status" data-testid="vehicle-bulk-partial">
          <p className="admin-form__error-title">
            {t('vehicle.bulk.partial', {
              applied: durum.summary.appliedCount,
              failed: durum.summary.failures.length,
            })}
          </p>

          <ul className="vehicle-bulk__failures">
            {durum.summary.failures.map((failure) => (
              <li key={failure.vehicleId} data-testid="vehicle-bulk-failure">
                <span className="vehicle-bulk__failure-name">{labelOf(failure.vehicleId)}</span>
                <span className="vehicle-bulk__failure-reason">
                  {failure.messageKeys
                    .map((key) =>
                      t(key, { fields: failure.fieldKeys.map((field) => t(field)).join(', ') }),
                    )
                    .join(' ')}
                </span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      {durum.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="vehicle-bulk-error">
          <p className="admin-form__error-title">{t('vehicle.bulk.error.title')}</p>
          <p>{t(durum.messageKey, { limit: durum.limit ?? '' })}</p>
        </div>
      ) : null}
    </section>
  )
}
