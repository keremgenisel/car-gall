import { useLocale } from '../../app/localeContext'
import type { ShowcaseListing } from '../../api/types'
import { formatMoney } from '../../lib/format'

export function ListingCard({ listing }: { readonly listing: ShowcaseListing }) {
  const { locale, t } = useLocale()
  const price = formatMoney(listing.price, locale)

  return (
    <li className="listing-card">
      <div className="listing-card__media">
        {listing.coverImageUrl === null ? null : (
          <img
            className="listing-card__image"
            src={listing.coverImageUrl}
            /* alt metni locale'de ICU ile üretilir; ham anahtar görünmez (AC-32). */
            alt={t('showcase.card.imageAlt', { model: listing.modelName, year: listing.modelYear })}
            width={480}
            height={360}
            loading="lazy"
          />
        )}
        {listing.isReserved ? (
          <span className="listing-card__badge">{t('showcase.card.reserved')}</span>
        ) : null}
      </div>

      <div className="listing-card__body">
        {/* Arapça metin içinde Latin model adı karışık yönlüdür (docs/frontend.md § 6). */}
        <p className="listing-card__model" dir="auto">
          {listing.modelName}
        </p>
        <p className="listing-card__meta">
          {t('showcase.card.year', { year: listing.modelYear })}
          {' · '}
          {t('showcase.card.mileage', { kilometers: listing.mileageKilometers })}
        </p>
        <p className="listing-card__price">
          {price ?? t('showcase.card.priceOnRequest')}
        </p>
        <p className="listing-card__branch" dir="auto">
          {listing.branchName}
        </p>
      </div>
    </li>
  )
}
