import { useEffect, useState, type ReactNode } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchListingDetail } from '../../shared/api/catalog'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import { formatMoney, formatYear } from '../../shared/i18n/format'
import { Badge, Card } from '../../shared/ui'
import { DetailError } from './states/DetailError'
import { DetailNotFound } from './states/DetailNotFound'
import { DetailSkeleton } from './states/DetailSkeleton'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'notFound' }
  | { readonly kind: 'ready'; readonly detail: ShowcaseListingDetail }

interface Props {
  readonly slug: string
  /**
   * Talep formunun yeri. Form `features/leads`'e aittir ve bu feature onu IMPORT EDEMEZ:
   * bir feature başka bir feature'ın iç dosyasını import etmez (docs/frontend.md § 1,
   * `scripts/check-structure.mjs` bunu CI'da kırar). Slot'u kabuk (`app/`) doldurur.
   *
   * Render prop, `detail` yüklendikten SONRA çağrılır: form `isReserved`'a ihtiyaç duyar
   * (AC-11) ve o bilgi bu sayfanın zaten yaptığı istekten gelir — ikinci bir istek
   * gerekmez.
   */
  readonly renderLead?: (detail: ShowcaseListingDetail) => ReactNode
}

export function ListingDetailPage({ slug, renderLead }: Props) {
  const { locale, t } = useLocale()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [reloadToken, setReloadToken] = useState(0)

  // Yükleniyor durumu efekt içinde setState ile kurulmaz, istek anahtarından TÜRETİLİR:
  // senkron setState zincirleme render tetikler ve iskelet bir kare geç görünür (AC-17, CLS).
  const requestKey = `${locale}|${slug}|${reloadToken}`
  const load: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    fetchListingDetail(slug, locale, controller.signal)
      .then((detail) =>
        setResult({
          key: requestKey,
          value: detail === null ? { kind: 'notFound' } : { kind: 'ready', detail },
        }),
      )
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setResult({ key: requestKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [locale, slug, reloadToken, requestKey])

  if (load.kind === 'loading') {
    return (
      <div className="page">
        <DetailSkeleton />
      </div>
    )
  }

  if (load.kind === 'notFound') {
    return (
      <div className="page">
        <DetailNotFound homeHref={`/${locale}/`} />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <div className="page">
        <DetailError onRetry={() => setReloadToken((token) => token + 1)} />
      </div>
    )
  }

  const { detail } = load
  const price = formatMoney(detail.price, locale)

  return (
    <div className="page">
      <Card as="article" testId="listing-detail">
        <div className="listing-detail__media">
          {detail.coverImageUrl === null ? null : (
            <img
              className="listing-detail__image"
              src={detail.coverImageUrl}
              /* `alt` ziyaretçinin locale'inden üretilir; sunucudan çevrilmiş metin
                 gelmez (AC-25, Yasak Listesi 23). */
              alt={t('detail.imageAlt', { model: detail.modelName, year: detail.modelYear })}
              width={960}
              height={720}
              /* Detayın kapak görseli katlama üstündedir ve LCP adayıdır. */
              fetchPriority="high"
            />
          )}
          {detail.isReserved ? <Badge floating>{t('detail.reserved')}</Badge> : null}
        </div>

        <div className="listing-detail__body">
          {/* Arapça gövde içinde Latin model adı karışık yönlüdür (AC-23). */}
          <h1 className="listing-detail__model" dir="auto">
            {detail.modelName}
          </h1>

          <p className="listing-detail__price" data-testid="detail-price">
            {price ?? t('detail.priceOnRequest')}
          </p>

          <dl className="listing-detail__attributes" data-testid="detail-attributes">
            <Attribute
              label={t('detail.field.modelYear')}
              value={formatYear(detail.modelYear, locale)}
            />
            <Attribute
              label={t('detail.field.mileage')}
              value={t('detail.mileageValue', { kilometers: detail.mileageKilometers })}
            />
            <Attribute label={t('detail.field.branch')} value={detail.branchName} />

            {/*
              Sunucu yalnız açık ve public alanları gönderir; kapalı veya gizli alan
              yanıtta HİÇ bulunmaz (F3, AC-12, AC-13). Uç burada süzme yapmaz — yapsaydı
              gizleme sunum katmanına kaymış olurdu (Yasak Listesi 41).
              Etiket sunucudan çözülmüş gelir: ziyaretçi ham slug görmez.
            */}
            {detail.attributes.map((attribute) => (
              <Attribute key={attribute.key} label={attribute.label} value={attribute.value} />
            ))}
          </dl>
        </div>
      </Card>

      {renderLead === undefined ? null : renderLead(detail)}
    </div>
  )
}

function Attribute({ label, value }: { readonly label: string; readonly value: string }) {
  return (
    <div className="listing-detail__attribute">
      <dt className="listing-detail__label">{label}</dt>
      {/* Değer Latin bir model/donanım adı olabilir: karışık yönlü metin (AC-23). */}
      <dd className="listing-detail__value" dir="auto">
        {value}
      </dd>
    </div>
  )
}
