import { useLocale } from '../../../app/localeContext'
import { Card, Skeleton } from '../../../shared/ui'

/** Nitelik listesinin tipik satır sayısı; iskelet gerçek kutunun yüksekliğini tutar. */
const ATTRIBUTE_ROWS = 6

/**
 * İskelet GERÇEK detayın kutu modelini taklit eder: aynı medya en-boy oranı, aynı
 * tipografi sınıfları, aynı nitelik ızgarası. Yer tutucular gerçek satırların
 * sınıflarının İÇİNDE durur; böylece yükseklik font boyutundan türer ve veri gelince
 * düzen kaymaz (docs/frontend.md § 3, AC-17).
 */
export function DetailSkeleton() {
  const { t } = useLocale()

  return (
    <Card as="div" testId="detail-skeleton">
      <div className="listing-detail__media">
        <Skeleton shape="media" />
      </div>

      <div className="listing-detail__body" role="status" aria-live="polite">
        <span className="visually-hidden">{t('detail.loading')}</span>

        <h1 className="listing-detail__model">
          <Skeleton shape="line" />
        </h1>
        <p className="listing-detail__price">
          <Skeleton shape="line" />
        </p>

        <dl className="listing-detail__attributes">
          {Array.from({ length: ATTRIBUTE_ROWS }, (_, index) => (
            <div className="listing-detail__attribute" key={index}>
              <dt className="listing-detail__label">
                <Skeleton shape="line" />
              </dt>
              <dd className="listing-detail__value">
                <Skeleton shape="line" />
              </dd>
            </div>
          ))}
        </dl>
      </div>
    </Card>
  )
}
