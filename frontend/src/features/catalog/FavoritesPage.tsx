import { useState } from 'react'
import { useLocale } from '../../app/localeContext'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import {
  favoritesAvailable,
  setFavorite,
  useFavorites,
  type Favorite,
} from '../../shared/lib/favorites'
import { listPath } from '../../shared/lib/route'
import { Card, EmptyState, ErrorState, Skeleton } from '../../shared/ui'
import { ListingCard } from './ListingCard'
import { comparePath, MAX_COMPARED } from './compareAddress'
import { useDetailReads, useRetryFocus } from './detailReads'

/**
 * `frontend-003` **D10** · **D6** — favoriler ekranı. Her favori mevcut detay okumasıyla, YALNIZ
 * kendi slug'ıyla okunur (D14, D15); sıra son eklenen önce. 404 dönen favori "artık vitrinde
 * olmayanlar"dadır ve kendiliğinden silinmez; 404 dışı hata kendi satırında "tekrar dene"yle
 * görünür ve düşmüş sayılmaz (tuzak 3). Karşılaştırma seçimi saklanmaz: adresin kendisidir (C5).
 */
export function FavoritesPage() {
  const { locale, t } = useLocale()
  const favorites = useFavorites()
  const ordered = [...favorites].reverse()
  const [selected, setSelected] = useState<readonly string[]>([])
  const [limitNotice, setLimitNotice] = useState(false)
  const { results, retry, readers } = useDetailReads(ordered.map((item) => item.slug))
  const focusAfterRetry = useRetryFocus(
    results,
    () => document.querySelector('[data-testid="favorites-list"]'),
    () => document.getElementById('favorites-remove-all'),
  )

  const title = <h1 className="page__title">{t('favoritesPage.title')}</h1>

  if (!favoritesAvailable()) {
    return (
      <div className="page favorites-page">
        {title}
        <EmptyState
          title={t('favoritesPage.unavailable.title')}
          description={t('favoritesPage.unavailable.body')}
          testId="favorites-unavailable"
        />
      </div>
    )
  }

  /** `frontend-003` S2 — kalıcı tek satır; uyarı kutusu değil (AC-44). */
  const note = <p className="favorites-page__note">{t('favoritesPage.note')}</p>

  function retryRows(slugs: readonly string[]) {
    focusAfterRetry(slugs)
    retry(slugs)
  }

  if (ordered.length === 0) {
    return (
      <div className="page favorites-page">
        {title}
        {note}
        <EmptyState
          title={t('favoritesPage.empty.title')}
          description={t('favoritesPage.empty.body')}
          testId="favorites-empty"
          action={
            <a className="ui-button ui-button--primary" href={listPath(locale)}>
              {t('favoritesPage.empty.action')}
            </a>
          }
        />
      </div>
    )
  }

  // Okumalar bitene kadar iskelet: içerik TEK geçişte gelir, kartlar tek tek oynamaz (AC-43).
  if (ordered.some((item) => results[item.slug] === undefined)) {
    return (
      <div className="page favorites-page">
        {title}
        {note}
        {readers}
        <FavoritesSkeleton count={ordered.length} />
      </div>
    )
  }

  const visible = ordered.filter((item) => typeof results[item.slug] === 'object')
  const gone = ordered.filter((item) => results[item.slug] === 'gone')
  // Yeniden denenen satır hatalı satırların arasında kalır (aria-busy): ekran iskelete düşmez.
  const failed = ordered.filter(
    (item) => results[item.slug] === 'error' || results[item.slug] === 'retrying',
  )

  if (failed.length === ordered.length) {
    return (
      <div className="page favorites-page">
        {title}
        {note}
        {readers}
        <ErrorState
          title={t('favoritesPage.error.title')}
          description={t('favoritesPage.error.body')}
          retryLabel={t('favoritesPage.retry')}
          onRetry={() => retryRows(failed.map((item) => item.slug))}
          testId="favorites-error"
        />
      </div>
    )
  }

  // Seçim ekrandaki sırayla taşınır (D6); görünmeyen favori seçili kalmaz.
  const chosen = visible.map((item) => item.slug).filter((slug) => selected.includes(slug))

  function toggle(slug: string, on: boolean) {
    if (on && chosen.length >= MAX_COMPARED) {
      setLimitNotice(true)
      return
    }

    setLimitNotice(false)
    setSelected(on ? [...chosen, slug] : chosen.filter((item) => item !== slug))
  }

  return (
    <div className="page favorites-page">
      {title}
      {note}
      {readers}

      {visible.length === 0 ? (
        <EmptyState
          title={t('favoritesPage.gone.title')}
          description={t('favoritesPage.gone.body')}
          testId="favorites-all-gone"
        />
      ) : (
        <>
          <div className="favorites-page__compare">
            <p id="favorites-compare-hint">{t('favoritesPage.compareHint')}</p>
            <button
              type="button"
              className="ui-button ui-button--primary"
              aria-describedby="favorites-compare-hint"
              disabled={chosen.length < 2}
              onClick={() => window.location.assign(comparePath(locale, chosen))}
            >
              {t('favoritesPage.compare')}
            </button>
            <p className="visually-hidden" role="status">
              {limitNotice ? t('favoritesPage.selectLimit', { count: MAX_COMPARED }) : ''}
            </p>
          </div>
          <ul className="listing-grid" data-testid="favorites-list">
            {visible.map((item, index) => {
              const detail = results[item.slug] as ShowcaseListingDetail

              return (
                <ListingCard
                  key={item.slug}
                  aboveTheFold={index < 3}
                  listing={{
                    listingId: detail.slug,
                    slug: detail.slug,
                    isReserved: detail.isReserved,
                    coverImageUrl: detail.images[0]?.display.url ?? null,
                    modelName: detail.modelName,
                    modelYear: detail.modelYear,
                    mileageKilometers: detail.mileageKilometers,
                    price: detail.price,
                    branchName: detail.branchName,
                  }}
                  footer={
                    <label className="favorites-page__select">
                      <input
                        type="checkbox"
                        checked={chosen.includes(item.slug)}
                        onChange={(event) => toggle(item.slug, event.target.checked)}
                        aria-label={t('favoritesPage.select', label(item))}
                      />
                      <span aria-hidden="true">{t('favoritesPage.compare')}</span>
                    </label>
                  }
                />
              )
            })}
          </ul>
        </>
      )}

      {failed.length === 0 ? null : (
        <ul className="favorites-page__rows" data-testid="favorites-failed">
          {failed.map((item) => (
            <li
              key={item.slug}
              className="favorites-page__row"
              role="alert"
              aria-busy={results[item.slug] === 'retrying'}
            >
              <span>{t('favoritesPage.rowError', label(item))}</span>
              <button
                type="button"
                className="ui-button ui-button--secondary"
                onClick={() => retryRows([item.slug])}
              >
                {t('favoritesPage.retry')}
              </button>
            </li>
          ))}
        </ul>
      )}

      {gone.length === 0 ? null : (
        <section className="favorites-page__gone" aria-labelledby="favorites-gone-title">
          <div className="favorites-page__gone-head">
            <h2 id="favorites-gone-title">{t('favoritesPage.goneSection')}</h2>
            <button
              type="button"
              id="favorites-remove-all"
              className="ui-button ui-button--secondary"
              onClick={() => {
                for (const item of gone) {
                  setFavorite(false, item)
                }
              }}
            >
              {t('favoritesPage.removeAll')}
            </button>
          </div>
          <ul className="favorites-page__rows" data-testid="favorites-gone">
            {gone.map((item) => (
              <li key={item.slug} className="favorites-page__row">
                {/* Saklanan özet; bağlantı yok — ilan artık açılmaz (R10). */}
                <bdi>{t('favoritesPage.goneItem', label(item))}</bdi>
                <button
                  type="button"
                  className="ui-button ui-button--secondary"
                  aria-label={t('favoritesPage.remove', label(item))}
                  onClick={() => setFavorite(false, item)}
                >
                  {t('favoritesPage.removeOne')}
                </button>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  )
}

/** Model adı ve yılı; yıl hazır dizgedir (ICU onu sayı gibi gruplamasın: "2.020"). */
function label(item: Favorite) {
  return { model: item.modelName, year: String(item.modelYear) }
}

/** İskelet gerçek kartın kutusunu taklit eder; kart sayısı favori sayısıdır (AC-43). */
function FavoritesSkeleton({ count }: { readonly count: number }) {
  return (
    <div className="listing-grid" role="status" aria-busy="true" data-testid="favorites-skeleton">
      {Array.from({ length: count }, (_, index) => (
        <Card as="div" key={index}>
          <div className="listing-card__media">
            <Skeleton shape="media" />
          </div>
          <div className="listing-card__body">
            <p className="listing-card__model">
              <Skeleton shape="line" />
            </p>
            <p className="listing-card__meta">
              <Skeleton shape="line" />
            </p>
            <p className="listing-card__price">
              <Skeleton shape="line" />
            </p>
            <p className="listing-card__branch">
              <Skeleton shape="line" />
            </p>
          </div>
        </Card>
      ))}
    </div>
  )
}
