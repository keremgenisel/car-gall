import type { ReactNode } from 'react'
import { useLocale } from '../../app/localeContext'
import type { ShowcaseListing } from '../../shared/api/types'
import { formatMoney } from '../../shared/i18n/format'
import { listingPath } from '../../shared/lib/route'
import { Badge, Card } from '../../shared/ui'
import { FavoriteButton } from './FavoriteButton'

interface Props {
  readonly listing: ShowcaseListing
  /**
   * İlk ekranda görünen kartlar. Kapak görseli LCP adayıdır; katlama üstündeki görseli
   * lazy yüklemek tarayıcının onu keşfetmesini geciktirir (docs/frontend.md § 7).
   */
  readonly aboveTheFold: boolean
  /** `frontend-003` D6 — favoriler ekranının "karşılaştırmaya seç" denetimi; kartın altında. */
  readonly footer?: ReactNode
}

export function ListingCard({ listing, aboveTheFold, footer }: Props) {
  const { locale, t } = useLocale()
  const price = formatMoney(listing.price, locale)

  return (
    <Card as="li" testId="listing-card">
      <div className="listing-card__media">
        {listing.coverImageUrl === null ? null : (
          <img
            className="listing-card__image"
            src={listing.coverImageUrl}
            alt={t('showcase.card.imageAlt', { model: listing.modelName, year: listing.modelYear })}
            width={480}
            height={360}
            loading={aboveTheFold ? 'eager' : 'lazy'}
            fetchPriority={aboveTheFold ? 'high' : 'auto'}
          />
        )}
        {listing.isReserved ? <Badge floating>{t('showcase.card.reserved')}</Badge> : null}
      </div>

      <div className="listing-card__body">
        {/*
          `frontend-003` D7 · P6 — favori düğmesi model adının satırında, bağlantının DIŞINDA:
          kartta iç içe etkileşim yoktur (AC-22).
        */}
        <div className="listing-card__head">
          {/* Arapça metin içinde Latin model adı karışık yönlüdür (docs/frontend.md § 6). */}
          <p className="listing-card__model" dir="auto">
            {/*
            Bağlantı model adını sarar: erişilebilir adı ilanın kendisidir, "devamını
            oku" değil. Kartın tamamını sarmak, kart içinde ileride başka bir eylem
            çıktığında iç içe etkileşim üretirdi (docs/frontend.md § 8).
          */}
            <a className="listing-card__link" href={listingPath(locale, listing.slug)}>
              {listing.modelName}
            </a>
          </p>
          <FavoriteButton listing={listing} />
        </div>
        <p className="listing-card__meta">
          {t('showcase.card.year', { year: listing.modelYear })}
          {' · '}
          {t('showcase.card.mileage', { kilometers: listing.mileageKilometers })}
        </p>
        <p className="listing-card__price">{price ?? t('showcase.card.priceOnRequest')}</p>
        <p className="listing-card__branch" dir="auto">
          {listing.branchName}
        </p>
      </div>
      {footer}
    </Card>
  )
}
