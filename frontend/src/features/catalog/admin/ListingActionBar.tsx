import { useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import { applyListingAction } from '../../../shared/api/catalogAdmin'
import type { ListingAction } from '../../../shared/api/types'
import { Badge, Button } from '../../../shared/ui'
import { codeKey } from './vehicleFormState'
import {
  availableActions,
  messageKey,
  missingFieldKeys,
  publishRejectionKeys,
} from './listingActionState'

type Durum =
  | { readonly kind: 'idle' }
  | { readonly kind: 'working'; readonly action: ListingAction }
  | { readonly kind: 'done' }
  | {
      readonly kind: 'failed'
      readonly action: ListingAction
      readonly messageKeys: readonly string[]
      readonly fieldKeys: readonly string[]
      /**
       * **D26**'nın ikinci yarısı: panelin yapacağı iş REDDE GÖRE değişiyor. `422`
       * koşullar eksik → personel onları düzeltip **tekrar dener**. `409` ilan o durumda
       * değil → tekrar denemek tanımı gereği yine başarısız olur; yapılacak şey satırı
       * **tazelemek**. `0007`/AC-12'nin kuralı burada da geçerli: kural gereği reddedilene
       * tekrar denemesini söylemek, sonsuza kadar denemesini söylemektir.
       */
      readonly conflict: boolean
    }

/**
 * `0012b` — bir ilanın durum eylemleri (**R1**, **R5**–**R7**).
 *
 * <p><b>Yalnız A3'ün izin verdiği eylemler basılıyor</b> (`listingActionState`): taslak
 * bir ilanda "rezerve et" düğmesi, sunucunun `409`'una yürüyen bir davet olurdu. Sunucu
 * yine de reddediyor — bu liste reddi önlemek için değil, olmayan bir düğmeyi
 * göstermemek için.</p>
 *
 * <p><b>Yayın reddi bir LİSTEDİR</b> (**D2**): "en az bir fotoğraf hazır olmalı" ile
 * "zorunlu alan boş" aynı anda doğru olabilir ve personel ikisini birden görmeli — tek
 * kod dönmek onu tek tek denemeye zorlardı.</p>
 *
 * <p><b>Rol ayrımı YOK</b> (**D22**): eylemler oturum açmış her personele açık.</p>
 */
export function ListingActionBar({
  vehicleId,
  listingStatus,
  awaitingBranchAssignment,
  onApplied,
}: {
  readonly vehicleId: string
  readonly listingStatus: string
  readonly awaitingBranchAssignment: boolean
  readonly onApplied: () => void
}) {
  const { locale, t } = useLocale()
  const [durum, setDurum] = useState<Durum>({ kind: 'idle' })

  const eylemler = availableActions(listingStatus)

  async function uygula(action: ListingAction) {
    setDurum({ kind: 'working', action })

    try {
      await applyListingAction(vehicleId, action, locale)
      setDurum({ kind: 'done' })
      onApplied()
    } catch (cause) {
      const hata = cause instanceof ApiError ? cause : null

      setDurum({
        kind: 'failed',
        action,
        // D2 — üst kod `vehicle.cannot_publish`, alt kodlar `reasons` parametresinde.
        // Üst kod tek başına "neden olmadı" sorusunu cevaplamıyor.
        messageKeys:
          hata?.errorCode === 'vehicle.cannot_publish'
            ? publishRejectionKeys(hata.parameters?.['reasons'], locale)
            : [messageKey(hata?.errorCode ?? 'api.unavailable', locale)],
        fieldKeys: missingFieldKeys(hata?.parameters?.['fields']),
        conflict: hata?.status === 409,
      })
    }
  }

  return (
    <section className="vehicle-actions" data-testid="vehicle-actions">
      <h2 className="admin-form__label">{t('vehicle.action.title')}</h2>

      <p className="vehicle-actions__status">
        <Badge>{t(codeKey('vehicle.status', listingStatus))}</Badge>
        <span className="admin-form__hint">
          {t('vehicle.action.current', { status: t(codeKey('vehicle.status', listingStatus)) })}
        </span>
      </p>

      {/*
        R13 — şubesi kapatılmış araç ayırt ediliyor ve NEDEN yayına alınamayacağı
        düğmeye basılmadan söyleniyor. Sunucu da reddediyor (AC-29); buradaki amaç
        personeli boşuna denemekten kurtarmak.
      */}
      {awaitingBranchAssignment ? (
        <p className="admin-form__field-error" data-testid="vehicle-awaiting-branch">
          {t('vehicle.badge.awaitingBranch.hint')}
        </p>
      ) : null}

      {eylemler.length === 0 ? (
        <p className="admin-form__hint" data-testid="vehicle-actions-empty">
          {t('vehicle.action.none')}
        </p>
      ) : (
        <div className="vehicle-actions__buttons">
          {eylemler.map((action) => (
            <Button
              key={action}
              variant={action === 'publish' ? 'primary' : 'secondary'}
              loading={durum.kind === 'working' && durum.action === action}
              onClick={() => void uygula(action)}
              data-testid={`vehicle-action-${action}`}
            >
              {durum.kind === 'working' && durum.action === action
                ? t('vehicle.action.working')
                : t(`vehicle.action.${action}`)}
            </Button>
          ))}
        </div>
      )}

      {/* A4 — `Satıldı` terminaldir; "geri al" düğmesi kuralı delerdi ve ekran bunu söylüyor. */}
      {listingStatus === 'Sold' ? (
        <p className="admin-form__hint" data-testid="vehicle-sold-terminal">
          {t('vehicle.action.terminal')}
        </p>
      ) : null}

      {durum.kind === 'done' ? (
        <p className="admin-form__notice-body" role="status" data-testid="vehicle-action-done">
          {t('vehicle.action.done')}
        </p>
      ) : null}

      {durum.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="vehicle-action-error">
          {/*
            Başlık YAPILAN EYLEME bağlı. Sabit bir "Araç yayına alınamadı" başlığı, altı
            eylemin beşinde yanlış bir olguyu bildiriyordu: personel "satıldı işaretle"ye
            basıyor, ekran yayından söz ediyordu.
          */}
          <p className="admin-form__error-title">
            {t(`vehicle.action.error.title.${durum.action}`)}
          </p>
          <ul className="vehicle-actions__reasons">
            {durum.messageKeys.map((key) => (
              <li key={key}>
                {t(key, {
                  fields: durum.fieldKeys.map((field) => t(field)).join(', '),
                })}
              </li>
            ))}
          </ul>
          <div className="admin-form__banner-action">
            {durum.conflict ? (
              <Button variant="secondary" onClick={onApplied} data-testid="vehicle-action-refresh">
                {t('vehicle.action.refresh')}
              </Button>
            ) : (
              <Button
                variant="secondary"
                onClick={() => void uygula(durum.action)}
                data-testid="vehicle-action-retry"
              >
                {t('vehicle.action.retry')}
              </Button>
            )}
          </div>
        </div>
      ) : null}
    </section>
  )
}
