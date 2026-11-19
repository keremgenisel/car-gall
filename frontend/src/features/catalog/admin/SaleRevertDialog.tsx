import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { fetchSaleReversals, revertListingSale } from '../../../shared/api/catalogAdmin'
import type { SaleReversalOverview, SaleRevertTarget } from '../../../shared/api/types'
import { Badge, Button, Dialog, Skeleton } from '../../../shared/ui'
import { messageKey, missingFieldKeys } from './listingActionState'
import {
  canConfirm,
  isSaleRevertTarget,
  MAX_REASON_LENGTH,
  MIN_REASON_LENGTH,
  reasonLength,
  revertFailure,
} from './saleReversalState'
import { codeKey } from './vehicleFormState'

type Preview =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly overview: SaleReversalOverview
      readonly target: SaleRevertTarget
    }

type Submission =
  | { readonly kind: 'idle' }
  | { readonly kind: 'submitting' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * `0023` **R9** — satışı geri almanın onay penceresi. Yönetici ilanın HANGİ duruma
 * döneceğini görür ve gerekçeyi aynı pencerede yazar; istek o hedefi taşır (**D16**).
 *
 * <p><b>Gerekçe hiçbir hata dalında kaybolmaz</b> (**AC-31**): metin bileşenin kendi
 * durumunda ve önizlemenin yeniden okunması ya da sunucunun reddi onu sıfırlamıyor.</p>
 *
 * <p><b>Hedef değiştiyse</b> (`409`, **AC-7**) pencere kapanmaz: güncel hedef gösterilir,
 * neden değiştiği söylenir ve yönetici yeniden onaylar. Sessizce yeni hedefle göndermek,
 * onun görmediği bir sonucu onaylamış saymak olurdu.</p>
 */
export function SaleRevertDialog({
  vehicleId,
  open,
  onClose,
  onReverted,
}: {
  readonly vehicleId: string
  readonly open: boolean
  readonly onClose: () => void
  readonly onReverted: () => void
}) {
  const { t } = useLocale()

  return (
    <Dialog
      open={open}
      onClose={onClose}
      label={t('vehicle.saleRevert.title')}
      testId="sale-revert-dialog"
    >
      <SaleRevertForm vehicleId={vehicleId} onClose={onClose} onReverted={onReverted} />
    </Dialog>
  )
}

function SaleRevertForm({
  vehicleId,
  onClose,
  onReverted,
}: {
  readonly vehicleId: string
  readonly onClose: () => void
  readonly onReverted: () => void
}) {
  const { locale, t } = useLocale()
  const [preview, setPreview] = useState<Preview>({ kind: 'loading' })
  const [reloadToken, setReloadToken] = useState(0)
  const [reason, setReason] = useState('')
  const [submission, setSubmission] = useState<Submission>({ kind: 'idle' })
  const [targetChanged, setTargetChanged] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    fetchSaleReversals(vehicleId, locale, controller.signal)
      .then((overview) => {
        setPreview(
          isSaleRevertTarget(overview.targetStatus)
            ? { kind: 'ready', overview, target: overview.targetStatus }
            : { kind: 'error' },
        )
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setPreview({ kind: 'error' })
      })

    return () => controller.abort()
  }, [vehicleId, locale, reloadToken])

  async function confirm(target: SaleRevertTarget) {
    setSubmission({ kind: 'submitting' })

    try {
      await revertListingSale(vehicleId, reason.trim(), target, locale)
      setSubmission({ kind: 'idle' })
      onReverted()
    } catch (cause) {
      const failure = revertFailure(cause, locale)

      if (failure.kind === 'target-changed') {
        // Önizleme YENİDEN okunuyor: hedefle birlikte "neden Taslak" listesi de değişmiş
        // olabilir. Gerekçe ayrı durumda ve dokunulmuyor.
        setTargetChanged(true)
        setSubmission({ kind: 'idle' })
        setReloadToken((value) => value + 1)
        return
      }

      setSubmission({ kind: 'failed', messageKey: failure.messageKey })
    }
  }

  const length = reasonLength(reason)
  const submitting = submission.kind === 'submitting'
  const fields =
    preview.kind === 'ready' ? missingFieldKeys(preview.overview.missingFields.join(',')) : []

  return (
    <div className="sale-revert" data-testid="sale-revert-form">
      <h2 className="sale-revert__title">{t('vehicle.saleRevert.title')}</h2>
      <p className="admin-form__hint">{t('vehicle.saleRevert.body')}</p>

      {preview.kind === 'loading' ? (
        <div data-testid="sale-revert-loading">
          <Skeleton shape="line" />
        </div>
      ) : null}

      {preview.kind === 'error' ? (
        <div className="admin-form__error" role="alert" data-testid="sale-revert-preview-error">
          <p className="admin-form__error-title">{t('vehicle.saleRevert.preview.error')}</p>
          <div className="admin-form__banner-action">
            <Button variant="secondary" onClick={() => setReloadToken((value) => value + 1)}>
              {t('vehicle.action.retry')}
            </Button>
          </div>
        </div>
      ) : null}

      {preview.kind === 'ready' ? (
        <div className="sale-revert__target" data-testid="sale-revert-target">
          {targetChanged ? (
            <p
              className="admin-form__field-error"
              role="alert"
              data-testid="sale-revert-target-changed"
            >
              {t('vehicle.saleRevert.targetChanged')}
            </p>
          ) : null}
          <p className="vehicle-actions__status">
            <span>{t('vehicle.saleRevert.target')}</span>
            <Badge>{t(codeKey('vehicle.status', preview.target))}</Badge>
          </p>
          {preview.target === 'Draft' && preview.overview.draftReasons.length > 0 ? (
            <div data-testid="sale-revert-draft-reasons">
              <p className="admin-form__hint">{t('vehicle.saleRevert.draftBecause')}</p>
              <ul className="vehicle-actions__reasons">
                {preview.overview.draftReasons.map((code) => (
                  <li key={code}>
                    {t(messageKey(code, locale), {
                      fields: fields.map((field) => t(field)).join(', '),
                    })}
                  </li>
                ))}
              </ul>
            </div>
          ) : null}
        </div>
      ) : null}

      <div className="admin-form__field">
        <label className="admin-form__label" htmlFor="sale-revert-reason">
          {t('vehicle.saleRevert.reason.label')}
        </label>
        <p className="admin-form__hint" id="sale-revert-reason-hint">
          {t('vehicle.saleRevert.reason.hint', { min: MIN_REASON_LENGTH, max: MAX_REASON_LENGTH })}
        </p>
        <textarea
          id="sale-revert-reason"
          className="admin-form__control sale-revert__reason"
          dir="auto"
          rows={4}
          maxLength={MAX_REASON_LENGTH + 50}
          value={reason}
          aria-describedby="sale-revert-reason-hint sale-revert-reason-count"
          aria-invalid={length > MAX_REASON_LENGTH ? true : undefined}
          data-testid="sale-revert-reason"
          onChange={(event) => setReason(event.target.value)}
        />
        <p
          className="admin-form__hint sale-revert__count"
          id="sale-revert-reason-count"
          aria-live="polite"
        >
          {t('vehicle.saleRevert.reason.count', { count: length, max: MAX_REASON_LENGTH })}
        </p>
      </div>

      {submission.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="sale-revert-error">
          <p className="admin-form__error-title">{t('vehicle.saleRevert.error.title')}</p>
          <p>{t(submission.messageKey)}</p>
        </div>
      ) : null}

      <div className="admin-form__actions">
        <Button
          variant="secondary"
          onClick={onClose}
          disabled={submitting}
          data-testid="sale-revert-cancel"
        >
          {t('vehicle.saleRevert.cancel')}
        </Button>
        <Button
          loading={submitting}
          disabled={preview.kind !== 'ready' || !canConfirm(reason)}
          onClick={() => {
            if (preview.kind === 'ready') {
              void confirm(preview.target)
            }
          }}
          data-testid="sale-revert-confirm"
        >
          {submitting ? t('vehicle.action.working') : t('vehicle.saleRevert.confirm')}
        </Button>
      </div>
    </div>
  )
}
