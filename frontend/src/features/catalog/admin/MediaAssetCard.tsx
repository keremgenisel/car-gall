import { useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import { retryMediaAsset, setMediaAssetAltText } from '../../../shared/api/mediaAdmin'
import type { VehicleMediaItem } from '../../../shared/api/types'
import { SUPPORTED_LOCALES } from '../../../shared/lib/locale'
import { Badge, Button, Card, Field } from '../../../shared/ui'
import {
  failureKey,
  isProcessing,
  mediaMessageKey,
  statusKey,
  type MediaDraft,
} from './vehicleMediaState'

type Action =
  | { readonly kind: 'idle' }
  | { readonly kind: 'busy' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'failed'; readonly messageKey: string }

interface Props {
  readonly item: VehicleMediaItem
  readonly draft: MediaDraft
  readonly index: number
  readonly total: number
  readonly cover: boolean
  readonly altTextDirty: boolean
  readonly onMove: (delta: -1 | 1) => void
  readonly onDetach: () => void
  readonly onAltTextChange: (locale: string, value: string) => void
  readonly altTextRequest: () => Readonly<Record<string, string>>
  readonly onRefresh: () => void
}

/**
 * Aracın sıralı listesindeki BİR fotoğraf.
 *
 * <p><b>Kapak ayrı bir eylem değil</b> (**D2**): "kapak yap" düğmesi yok, çünkü kapak
 * sıralı listenin ilk elemanıdır. Personel kapağı fotoğrafı yukarı taşıyarak belirler ve
 * kartın ipucu bunu böyle anlatıyor — arayüz, domaine uymak zorunda.</p>
 *
 * <p><b>`alt` metni ve tekrar deneme varlık BAŞINA</b> kaydedilir; sıra kaydetmeyle aynı
 * akışta değil. İkisi farklı modülün ucuna gidiyor (`Media` vs. `VehicleCatalog`) ve tek
 * düğmede toplamak, biri başarısız olduğunda ekranın ne söyleyeceğini belirsiz kılardı.</p>
 *
 * <p><b>İşleniyor hâli iskelet DEĞİL</b> (**D8**, AC-28): o varlığın durumudur. Dakikalarca
 * sürebilen bir işlemeyi "sayfa yükleniyor" iskeletiyle göstermek, personele ekranın
 * donduğunu söylerdi.</p>
 */
export function MediaAssetCard({
  item,
  draft,
  index,
  total,
  cover,
  altTextDirty,
  onMove,
  onDetach,
  onAltTextChange,
  altTextRequest,
  onRefresh,
}: Props) {
  const { locale, t } = useLocale()
  const [altAction, setAltAction] = useState<Action>({ kind: 'idle' })
  const [retryAction, setRetryAction] = useState<Action>({ kind: 'idle' })

  const display = item.derivatives.find((derivative) => derivative.variant === 'display')
  const failure = failureKey(item, locale)
  const processing = isProcessing(item)

  async function saveAltText() {
    setAltAction({ kind: 'busy' })

    try {
      await setMediaAssetAltText(item.mediaAssetId, altTextRequest(), locale)
      setAltAction({ kind: 'saved' })
      onRefresh()
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setAltAction({ kind: 'failed', messageKey: mediaMessageKey(errorCode, locale) })
    }
  }

  async function retry() {
    setRetryAction({ kind: 'busy' })

    try {
      await retryMediaAsset(item.mediaAssetId, locale)
      setRetryAction({ kind: 'idle' })
      onRefresh()
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setRetryAction({ kind: 'failed', messageKey: mediaMessageKey(errorCode, locale) })
    }
  }

  return (
    <Card as="li" testId={`vehicle-media-item-${item.mediaAssetId}`}>
      <div className="vehicle-media__item">
        <div className="vehicle-media__thumb">
          {display === undefined ? (
            /*
              Türevi olmayan varlık: işleniyor ya da başarısız. Kutu yine de yer tutuyor —
              `--media-aspect-ratio` ile — çünkü türev geldiğinde düzen kaymamalı.
            */
            <div className="vehicle-media__thumb-placeholder" aria-hidden="true" />
          ) : (
            <img
              className="vehicle-media__image"
              src={display.url}
              width={display.width}
              height={display.height}
              /*
                § 7 — `alt` metni LOCALE BAŞINA. Personelin dilinde yazılmış metin yoksa
                boş `alt`: uydurulmuş bir metin, ekran okuyucuya yanlış bilgi vermek
                olurdu ve R4 bir dili boş bırakmayı geçerli sayıyor.
              */
              alt={item.altText[locale] ?? ''}
            />
          )}

          {cover ? <Badge floating>{t('vehicle.media.cover')}</Badge> : null}
        </div>

        <div className="vehicle-media__body">
          <p className="vehicle-media__status" data-testid="vehicle-media-status">
            <Badge>{t(statusKey(item))}</Badge>
            {processing ? (
              <span className="vehicle-media__processing" data-testid="vehicle-media-processing">
                {t('vehicle.media.processing.note')}
              </span>
            ) : null}
          </p>

          {/* AC-16 — başarısız varlık ayırt ediliyor: durum etiketi + NEDEN + eylem. */}
          {failure === null ? null : (
            <div className="vehicle-media__failure" data-testid="vehicle-media-failure">
              <p className="admin-form__error-body">{t(failure)}</p>
              <Button
                variant="secondary"
                loading={retryAction.kind === 'busy'}
                onClick={() => void retry()}
                data-testid="vehicle-media-retry"
              >
                {t('vehicle.media.retry')}
              </Button>
              {retryAction.kind === 'failed' ? (
                <p className="admin-form__field-error" role="alert">
                  {t(retryAction.messageKey)}
                </p>
              ) : null}
            </div>
          )}

          <div className="vehicle-media__order">
            <p className="admin-form__hint" id={`vehicle-media-order-${item.mediaAssetId}`}>
              {t('vehicle.media.order.hint', { position: index + 1, total })}
            </p>
            <Button
              variant="secondary"
              disabled={index === 0}
              onClick={() => onMove(-1)}
              aria-describedby={`vehicle-media-order-${item.mediaAssetId}`}
              data-testid="vehicle-media-up"
            >
              {t('vehicle.media.moveUp')}
            </Button>
            <Button
              variant="secondary"
              disabled={index === total - 1}
              onClick={() => onMove(1)}
              aria-describedby={`vehicle-media-order-${item.mediaAssetId}`}
              data-testid="vehicle-media-down"
            >
              {t('vehicle.media.moveDown')}
            </Button>
            {/* R3 — çıkarmak SİLMEZ; ipucu bunu söylüyor, yoksa personel silmekten korkar. */}
            <Button variant="text" onClick={onDetach} data-testid="vehicle-media-detach">
              {t('vehicle.media.detach')}
            </Button>
          </div>

          {/* R4 / **D16** — `alt` metni etkin HER dilde; bir dili boş bırakmak geçerli. */}
          <fieldset className="admin-form__group">
            <legend className="admin-form__label">{t('vehicle.media.altText')}</legend>
            <p className="admin-form__hint">{t('vehicle.media.altText.hint')}</p>

            {SUPPORTED_LOCALES.map((code) => (
              <Field
                key={code}
                id={`vehicle-media-alt-${item.mediaAssetId}-${code}`}
                label={t('vehicle.media.altText.field', { locale: t(`app.locale.${code}`) })}
                value={draft.altText[item.mediaAssetId]?.[code] ?? ''}
                onChange={(value) => onAltTextChange(code, value)}
              />
            ))}

            <Button
              variant="secondary"
              loading={altAction.kind === 'busy'}
              disabled={!altTextDirty}
              onClick={() => void saveAltText()}
              data-testid="vehicle-media-alt-save"
            >
              {t('vehicle.media.altText.save')}
            </Button>

            {altAction.kind === 'saved' && !altTextDirty ? (
              <p className="admin-form__notice-body" role="status">
                {t('vehicle.media.altText.saved')}
              </p>
            ) : null}

            {altAction.kind === 'failed' ? (
              <p className="admin-form__field-error" role="alert">
                {t(altAction.messageKey)}
              </p>
            ) : null}
          </fieldset>
        </div>
      </div>
    </Card>
  )
}
