import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { ApiError } from '../../../shared/api/client'
import { fetchVehicleMedia, updateVehicleMedia } from '../../../shared/api/catalogAdmin'
import type { VehicleMedia, VehicleMediaItem } from '../../../shared/api/types'
import { adminPath, vehicleEditPath } from '../../../shared/lib/route'
import { Badge, Button, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { MediaAssetCard } from './MediaAssetCard'
import { MediaUploadField } from './MediaUploadField'
import {
  altTextChanged,
  altTextRequest,
  attach,
  coverOf,
  detach,
  mediaConditionMet,
  mediaMessageKey,
  move,
  orderChanged,
  toDraft,
  withAltText,
  type MediaDraft,
} from './vehicleMediaState'

type Source =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'missing' }
  | { readonly kind: 'ready'; readonly media: VehicleMedia }

type Save =
  | { readonly kind: 'idle' }
  | { readonly kind: 'saving' }
  | { readonly kind: 'saved' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * 0012a **D17** — aracın medya yüzeyi: `/admin/vehicles/{id}/media`.
 *
 * <p><b>Neden ayrı ekran, sekme değil:</b> düzenleme ekranı `0010b` ile zaten büyük ve bu
 * yüzeyin kendi üç durumu ve kendi kaydetme akışı var. Sekme yapmak iki kaydetme akışını
 * birbirine bağlar — personel renk alanını değiştirip medya sekmesine geçtiğinde hangi
 * düğmenin neyi kaydettiği belirsizleşirdi.</p>
 *
 * <p><b>Yazma tek istek</b> (**D12**): bağlama, sıralama ve çıkarma sıralı kimlik
 * listesinin TAMAMINI yerine koyan tek idempotent `PUT`. Kapak ayrı bir eylem değil,
 * sıranın sonucu (**D2**).</p>
 *
 * <p><b>Yayına alma YOK</b> (**D7**, **D18**): ekran medya koşulunu <i>ölçer</i> ve
 * rozetle gösterir, aracı yayına <i>almaz</i>. Yayın eylemi `0012b`'nin işi. Rozetin
 * burada olması bedelin görünür olması içindir: bu dilim bittiğinde galeri hâlâ hiçbir
 * aracı vitrine çıkaramaz.</p>
 *
 * <p>Dört durum da ele alınıyor (R7, `docs/frontend.md` § 3): iskelet, boş, hata ve —
 * dördüncüsü, ekranın değil VARLIĞIN durumu — "işleniyor" (**D8**). Hiçbirinde durum
 * kodu, `errorCode`, `traceId` veya yol basılmıyor (AC-29).</p>
 */
export function VehicleMediaPage({ vehicleId }: { readonly vehicleId: string }) {
  const { locale, t } = useLocale()
  const [loaded, setLoaded] = useState<{ readonly key: string; readonly value: Source } | null>(
    null,
  )
  const [sourceToken, setSourceToken] = useState(0)
  const [draft, setDraft] = useState<MediaDraft | null>(null)
  /**
   * Yüklenmiş ama HENÜZ BAĞLANMAMIŞ varlıklar. Yükleme `Media`'nın işi, bağlama
   * `VehicleCatalog`'un; ikisi ayrı istek ve ayrı modül. Yüklenen fotoğrafı sunucudan
   * gelen listeye karıştırmak yerine burada tutmak, "bağla" eyleminin AÇIK kalmasını
   * sağlıyor — AC-2 personelin bağladığını söylüyor, yüklemenin bağladığını değil.
   */
  const [pending, setPending] = useState<readonly VehicleMediaItem[]>([])
  const [save, setSave] = useState<Save>({ kind: 'idle' })

  /**
   * Veri anahtarı TOKEN TAŞIMAZ ve bu bilinçli: yeniden okuma sırasında ekran ELDEKİ
   * veriyi göstermeye devam ediyor, iskelete DÜŞMÜYOR. Araç ya da dil değiştiğinde
   * anahtar değişir ve iskelet basılır — o veri gerçekten başka bir aracın verisidir.
   *
   * <p>Bu da e2e'nin yazdırdığı bir düzeltme: token'ı anahtara koyan ilk hâlimde her
   * tazeleme bütün listeyi SÖKÜYORDU, kartlar yeniden monte oluyor ve kartın kendi
   * durumu ("tanımlar kaydedildi") onunla birlikte kayboluyordu.</p>
   */
  const dataKey = `${locale}|${vehicleId}`
  const source: Source =
    loaded !== null && loaded.key === dataKey ? loaded.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    fetchVehicleMedia(vehicleId, locale, controller.signal)
      .then((media) => {
        setDraft(toDraft(media))
        setPending([])

        /*
          Kaydetme bildirimi yeniden okumadan SAĞ ÇIKIYOR. İlk hâlim burada koşulsuz
          `idle` yazıyordu ve bildirim hiç görünmüyordu: kaydetme başarıdan sonra
          sunucudan yeniden okuyor, okuma da bildirimi siliyordu. e2e yakaladı — birim
          testlerinde `fetch` anında çözüldüğü için iki `setState` aynı turda birleşiyor
          ve bildirim görünür kalıyordu.
        */
        setSave((current) => (current.kind === 'saved' ? current : { kind: 'idle' }))
        setLoaded({ key: dataKey, value: { kind: 'ready', media } })
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        // Olmayan araç bir HATA değil: "tekrar dene" onu getirmez.
        const missing = cause instanceof ApiError && cause.status === 404

        setLoaded({ key: dataKey, value: { kind: missing ? 'missing' : 'error' } })
      })

    return () => controller.abort()
  }, [locale, vehicleId, sourceToken, dataKey])

  function reload() {
    setSourceToken((token) => token + 1)
  }

  async function saveOrder() {
    if (draft === null) {
      return
    }

    setSave({ kind: 'saving' })

    try {
      await updateVehicleMedia(vehicleId, draft.order, locale)
      setSave({ kind: 'saved' })

      // Sunucudan yeniden okunuyor: bağlanan varlığın durumu, türevleri ve `alt` metni
      // `Media`'dan geliyor ve istemcide uydurulamaz.
      reload()
    } catch (cause) {
      const errorCode = cause instanceof ApiError ? cause.errorCode : 'api.unavailable'

      setSave({ kind: 'failed', messageKey: mediaMessageKey(errorCode, locale) })
    }
  }

  const title = t('vehicle.media.title')
  const back = (
    <a className="ui-button ui-button--text" href={vehicleEditPath(vehicleId)}>
      {t('vehicle.media.back')}
    </a>
  )

  if (source.kind === 'loading') {
    return (
      <div className="admin-page" data-testid="vehicle-media-skeleton">
        <h1 className="admin-page__title">{title}</h1>
        <ul className="vehicle-media__list">
          {[0, 1, 2].map((row) => (
            <li className="ui-card" key={row}>
              <Skeleton shape="media" />
              <Skeleton shape="line" />
            </li>
          ))}
        </ul>
      </div>
    )
  }

  if (source.kind === 'missing') {
    return (
      <div className="admin-page">
        <h1 className="admin-page__title">{title}</h1>
        <div className="admin-form__notice" role="status" data-testid="vehicle-media-missing">
          <p className="admin-form__notice-body">{t('vehicle.edit.missing')}</p>
          <div className="admin-form__banner-action">
            <a className="ui-button ui-button--secondary" href={adminPath('vehicles')}>
              {t('vehicle.form.saved.action')}
            </a>
          </div>
        </div>
      </div>
    )
  }

  if (source.kind === 'error') {
    return (
      <div className="admin-page">
        <h1 className="admin-page__title">{title}</h1>
        <ErrorState
          title={t('vehicle.media.error.title')}
          description={t('vehicle.media.error.body')}
          retryLabel={t('vehicle.media.error.retry')}
          onRetry={reload}
          testId="vehicle-media-error"
        />
      </div>
    )
  }

  if (draft === null) {
    return null
  }

  const kimlige = new Map<string, VehicleMediaItem>()

  for (const item of source.media.items) {
    kimlige.set(item.mediaAssetId, item)
  }

  for (const item of pending) {
    kimlige.set(item.mediaAssetId, item)
  }

  const ogeler = draft.order
    .map((id) => kimlige.get(id))
    .filter((item): item is VehicleMediaItem => item !== undefined)

  const cover = coverOf(draft)
  const dirty = orderChanged(draft, source.media)
  const condition = mediaConditionMet(source.media)

  return (
    <div className="admin-page">
      <div className="admin-page__header">
        <h1 className="admin-page__title">{title}</h1>
        {back}
      </div>

      {/*
        R6 — medya koşulu ÖLÇÜLEBİLİR hâle geldi; rozet onu gösteriyor. Yayına alma
        düğmesi bilerek YOK (D7): bu dilim koşulu ölçer, aracı yayına almaz.
      */}
      <p className="vehicle-media__condition" data-testid="vehicle-media-condition">
        <Badge>
          {condition ? t('vehicle.media.condition.met') : t('vehicle.media.condition.unmet')}
        </Badge>
        <span className="admin-form__hint">{t('vehicle.media.condition.hint')}</span>
      </p>

      <MediaUploadField
        onUploaded={(mediaAssetId) => {
          setPending((items) => [
            ...items,
            {
              mediaAssetId,
              // Yükleme yanıtı durumu veriyor; türev ve `alt` henüz YOK ve uydurulmuyor.
              status: 'uploaded',
              failureReason: null,
              altText: {},
              derivatives: [],
            },
          ])
          setDraft((value) => (value === null ? value : attach(value, mediaAssetId)))
          setSave({ kind: 'idle' })
        }}
      />

      {save.kind === 'failed' ? (
        <div className="admin-form__error" role="alert" data-testid="vehicle-media-save-error">
          <p className="admin-form__error-body">{t(save.messageKey)}</p>
          <div className="admin-form__banner-action">
            <Button variant="secondary" onClick={() => void saveOrder()}>
              {t('vehicle.media.save.retry')}
            </Button>
          </div>
        </div>
      ) : null}

      {save.kind === 'saved' && !dirty ? (
        <div className="admin-form__notice" role="status" data-testid="vehicle-media-saved">
          <p className="admin-form__notice-body">{t('vehicle.media.save.done')}</p>
        </div>
      ) : null}

      {ogeler.length === 0 ? (
        <EmptyState
          title={t('vehicle.media.empty.title')}
          description={t('vehicle.media.empty.body')}
          testId="vehicle-media-empty"
        />
      ) : (
        <ul className="vehicle-media__list">
          {ogeler.map((item, index) => (
            <MediaAssetCard
              key={item.mediaAssetId}
              item={item}
              draft={draft}
              index={index}
              total={ogeler.length}
              cover={item.mediaAssetId === cover}
              altTextDirty={altTextChanged(draft, source.media, item.mediaAssetId)}
              onMove={(delta) =>
                setDraft((value) =>
                  value === null ? value : move(value, item.mediaAssetId, delta),
                )
              }
              onDetach={() =>
                setDraft((value) => (value === null ? value : detach(value, item.mediaAssetId)))
              }
              onAltTextChange={(code, value) =>
                setDraft((current) =>
                  current === null ? current : withAltText(current, item.mediaAssetId, code, value),
                )
              }
              altTextRequest={() => altTextRequest(draft, item.mediaAssetId)}
              onRefresh={reload}
            />
          ))}
        </ul>
      )}

      {/*
        Kaydetme listenin DIŞINDA ve bu bir dış bulgunun bedeli. Düğme önce listeyle
        aynı dalın içindeydi: personel son fotoğrafı çıkarınca liste boşalıyor, boş
        durum basılıyor ve kaydetme yolu ekrandan KAYBOLUYORDU — taslak kirliyken.
        "Yanlış fotoğrafı yükledim, kaldırayım" tek fotoğraflı bir araçta en sık
        yapılacak iş ve panelden yapılamıyordu.

        Boş liste geçerli bir yazmadır ve sunucu tarafı onu zaten kabul ediyor
        (`UpdateVehicleMediaRequest`: "bütün fotoğrafları çıkar"); eksik olan yalnız
        bu düğmeydi.
      */}
      <div className="vehicle-media__actions">
        <Button
          loading={save.kind === 'saving'}
          disabled={!dirty}
          onClick={() => void saveOrder()}
          data-testid="vehicle-media-save"
        >
          {save.kind === 'saving' ? t('vehicle.media.save.sending') : t('vehicle.media.save')}
        </Button>
        <p className="admin-form__hint">{t('vehicle.media.save.hint')}</p>
      </div>
    </div>
  )
}
