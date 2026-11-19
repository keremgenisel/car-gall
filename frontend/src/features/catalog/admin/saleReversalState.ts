import { catalogKeys } from '../../../shared/i18n/i18n'
import type { AppLocale } from '../../../shared/lib/locale'
import { ApiError } from '../../../shared/api/client'
import { SALE_REVERT_TARGETS, type SaleRevertTarget } from '../../../shared/api/types'

/**
 * `0023` — satışı geri almanın SAF mantığı: kim görür, gerekçe ne zaman yeterlidir ve
 * sunucunun reddi hangi ekrana düşer. Bileşenin içinde karar vermek yerine burada, çünkü
 * hepsi birim testle ölçülebilir (`docs/frontend.md` § 8).
 */

/** **D4** — sunucunun sınırlarıyla AYNI (`ListingSaleReversal`). */
export const MIN_REASON_LENGTH = 10
export const MAX_REASON_LENGTH = 500

/**
 * **R12** — "Satışı geri al" yalnız Yöneticiye ve yalnız `Satıldı` ilanda basılır. Bu bir
 * güvenlik sınırı DEĞİL: sınır sunucudaki `admin.manager` politikasıdır (AC-29). Burada
 * yalnız yetkisi olmayana `403`'e yürüyen bir düğme gösterilmez.
 */
export function canRevertSale(role: string, listingStatus: string): boolean {
  return role === 'manager' && listingStatus === 'Sold'
}

/**
 * Gerekçenin sayılan uzunluğu. Sunucu baştaki ve sondaki boşluğu kırpıp sayıyor
 * (**AC-16**); sayaç başka türlü sayarsa düğme etkin olur ve sunucu reddeder.
 */
export function reasonLength(reason: string): number {
  return reason.trim().length
}

/** Onay düğmesi ancak gerekçe sınırlar içindeyse etkindir (**AC-13**). */
export function canConfirm(reason: string): boolean {
  const length = reasonLength(reason)

  return length >= MIN_REASON_LENGTH && length <= MAX_REASON_LENGTH
}

/** Sunucudan gelen durum kodu geri almanın üç hedefinden biri mi? */
export function isSaleRevertTarget(value: string | null | undefined): value is SaleRevertTarget {
  return SALE_REVERT_TARGETS.some((target) => target === value)
}

/** Reddin ekrandaki karşılığı. */
export type RevertFailure =
  | {
      /** **D16** — pencere açıkken hedef değişti; yönetici güncel hedefi görür. */
      readonly kind: 'target-changed'
      readonly target: SaleRevertTarget
    }
  | {
      readonly kind: 'failed'
      readonly messageKey: string
    }

/**
 * Sunucunun reddini ekrana çevirir. Bilinmeyen kod GENEL mesaja düşer ve kodun kendisi
 * yöneticiye ASLA gösterilmez (`docs/frontend.md` § 3.1, **AC-31**).
 */
export function revertFailure(cause: unknown, locale: AppLocale): RevertFailure {
  const error = cause instanceof ApiError ? cause : null
  const target = error?.parameters['targetStatus']

  if (error?.errorCode === 'listing.sale_reversal.target_changed' && isSaleRevertTarget(target)) {
    return { kind: 'target-changed', target }
  }

  const code = error?.errorCode ?? 'api.unavailable'
  const suffix = code
    .replace(/^(listing\.sale_reversal|listing|vehicle|concurrency)\./, '')
    .replace(/\./g, '_')
  const candidate = `vehicle.saleRevert.error.${suffix}`

  return {
    kind: 'failed',
    messageKey: catalogKeys(locale).includes(candidate)
      ? candidate
      : 'vehicle.saleRevert.error.generic',
  }
}
