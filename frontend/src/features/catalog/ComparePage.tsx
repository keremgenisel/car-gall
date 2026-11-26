import { useEffect, useState, type ReactNode } from 'react'
import { useLocale } from '../../app/localeContext'
import type { ShowcaseListingDetail } from '../../shared/api/types'
import { formatMoney } from '../../shared/i18n/format'
import { favoritesPath, listingPath, listPath } from '../../shared/lib/route'
import { Badge, EmptyState, ErrorState, Skeleton } from '../../shared/ui'
import { attributeName } from './attributeName'
import { compareSearch, comparedSlugs } from './compareAddress'
import { useDetailReads, useRetryFocus, type DetailResult } from './detailReads'
import { FavoriteButton } from './FavoriteButton'

type Result = DetailResult

/** Tablonun bir satırı: başlık ve sütun başına değer (`null` = o ilanda yok). */
interface Row {
  readonly label: string
  readonly values: readonly (string | null)[]
}

/**
 * `frontend-003` **D3** · **D11** · **D12** — karşılaştırma. Durum YALNIZ adrestedir (C5):
 * favorisi olmayan tarayıcı da aynı araçları aynı sırayla görür (R9). Her sütun mevcut detay
 * okumasıyla, kendi slug'ıyla okunur (D14). Satırlar fiyat, model yılı, kilometre, şube ve detay
 * yanıtındaki nitelikler; alan adı detayla AYNI eşlemeden (D11). Sunucu kapalı ve gizli alanı hiç
 * göndermez; bu ekran alan ayarı okumaz ve süzmez (C3, Yasak Listesi 41).
 */
export function ComparePage() {
  const { locale, t } = useLocale()
  // Adres bir kez okunur; değişmez (karşılaştırmadaki favori düğmesi adresi değiştirmez, AC-64).
  const [slugs] = useState(() => comparedSlugs(window.location.search))
  const { results, retry, readers } = useDetailReads(slugs.length < 2 ? [] : slugs)
  const focusAfterRetry = useRetryFocus(
    results,
    () => document.querySelector('.compare-table'),
    () => document.querySelector('.compare-table'),
  )

  // D3 madde 4 — okunan küme adresle aynı değilse adres geçmişe kayıt eklemeden düzeltilir.
  // `listing` değerleri zaten okunan kümeyse adrese dokunulmaz: öbür parametrelerin sırası da kalır.
  useEffect(() => {
    const { pathname, search, hash } = window.location
    const adresteki = new URLSearchParams(search).getAll('listing')
    const ayni =
      adresteki.length === slugs.length && adresteki.every((slug, index) => slug === slugs[index])

    if (!ayni) {
      const normalized = compareSearch(search, slugs)
      window.history.replaceState(window.history.state, '', `${pathname}${normalized}${hash}`)
    }
  }, [slugs])

  function retryColumns(retried: readonly string[]) {
    focusAfterRetry(retried)
    retry(retried)
  }

  const title = <h1 className="page__title">{t('compare.title')}</h1>

  if (slugs.length < 2) {
    return (
      <div className="page compare-page">
        {title}
        <EmptyState
          title={t('compare.tooFew.title')}
          description={t('compare.tooFew.body')}
          testId="compare-too-few"
          action={
            <a className="ui-button ui-button--primary" href={favoritesPath(locale)}>
              {t('compare.tooFew.action')}
            </a>
          }
        />
      </div>
    )
  }

  if (slugs.some((slug) => results[slug] === undefined)) {
    return (
      <div className="page compare-page">
        {title}
        {readers}
        <div className="ui-state" role="status" aria-busy="true" data-testid="compare-loading">
          <Skeleton shape="media" />
          <Skeleton shape="line" />
          <Skeleton shape="line" />
        </div>
      </div>
    )
  }

  const columns = slugs.map((slug) => results[slug]!)

  if (columns.every((column) => column === 'gone')) {
    return (
      <div className="page compare-page">
        {title}
        <EmptyState
          title={t('compare.allGone.title')}
          description={t('favoritesPage.gone.body')}
          testId="compare-all-gone"
          action={
            <a className="ui-button ui-button--primary" href={listPath(locale)}>
              {t('compare.allGone.action')}
            </a>
          }
        />
      </div>
    )
  }

  if (columns.every((column) => column === 'error' || column === 'retrying')) {
    return (
      <div className="page compare-page">
        {title}
        {readers}
        <ErrorState
          title={t('compare.error.title')}
          description={t('favoritesPage.error.body')}
          retryLabel={t('favoritesPage.retry')}
          onRetry={() => retryColumns(slugs)}
          testId="compare-error"
        />
      </div>
    )
  }

  const details = columns.map((column) => (typeof column === 'object' ? column : null))
  const value = (pick: (detail: ShowcaseListingDetail) => string) =>
    details.map((detail) => (detail === null ? null : pick(detail)))

  // Nitelik satırı yalnız karşılaştırılanlardan EN AZ BİRİNDE varsa; sıra sütunlarda ilk görülme.
  const keys: string[] = []
  for (const detail of details) {
    for (const attribute of detail?.attributes ?? []) {
      if (!keys.includes(attribute.key)) {
        keys.push(attribute.key)
      }
    }
  }

  const rows: Row[] = [
    {
      label: t('compare.field.price'),
      values: value((detail) => formatMoney(detail.price, locale) ?? t('detail.priceOnRequest')),
    },
    {
      label: t('detail.field.modelYear'),
      // Detaydaki `formatYear` ile aynı çıktı (Latin rakam, gruplamasız); ilk paketin bir bağını
      // daha dışa açtırmamak için (D18).
      values: value((detail) => String(detail.modelYear)),
    },
    {
      label: t('detail.field.mileage'),
      values: value((detail) => t('detail.mileageValue', { kilometers: detail.mileageKilometers })),
    },
    { label: t('detail.field.branch'), values: value((detail) => detail.branchName) },
    ...keys.map((key) => {
      const first = details
        .flatMap((detail) => detail?.attributes ?? [])
        .find((attribute) => attribute.key === key)!

      return {
        label: attributeName(key, t) ?? first.label,
        values: details.map((detail) =>
          detail === null
            ? null
            : (detail.attributes.find((attribute) => attribute.key === key)?.value ??
              t('compare.notSpecified')),
        ),
      }
    }),
  ]

  return (
    <div className="page compare-page">
      {title}
      {readers}
      {/* D12 · B17 — dar ekranda kendi içinde kayabilen kap: klavyeyle odaklanır, adı vardır. */}
      <div className="compare-table" role="region" aria-label={t('compare.table')} tabIndex={0}>
        <table>
          <thead>
            <tr>
              <td />
              {columns.map((column, index) => (
                <th key={slugs[index]} scope="col">
                  <Head column={column} onRetry={() => retryColumns([slugs[index]!])} />
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {rows.map((row) => (
              <tr key={row.label}>
                <th scope="row">{row.label}</th>
                {row.values.map((cell, index) => (
                  // Hücre sayfanın yönünde hizalanır; değer `<bdi>` ile yalıtılır (karışık yönlü
                  // metin, D12). `dir="auto"` hücreye konunca rakamlı değer hücreyi LTR yapıp
                  // ar'da hizayı bozuyordu.
                  <td key={slugs[index]}>{cell === null ? null : <bdi>{cell}</bdi>}</td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  )
}

/** Sütun başı: görsel, detaya giden model adı, rezerve rozeti, favori düğmesi (D11). */
function Head({ column, onRetry }: { readonly column: Result; readonly onRetry: () => void }) {
  const { locale, t } = useLocale()
  let content: ReactNode

  if (column === 'gone') {
    content = <p className="compare-table__note">{t('compare.gone')}</p>
  } else if (column === 'error' || column === 'retrying') {
    // Yeniden denenirken sütun yerinde kalır; düğme aynı öğedir, odak kaybolmaz (BULGU-5).
    content = (
      <div role="alert" aria-busy={column === 'retrying'}>
        <p className="compare-table__note">{t('compare.columnError')}</p>
        <button type="button" className="ui-button ui-button--secondary" onClick={onRetry}>
          {t('favoritesPage.retry')}
        </button>
      </div>
    )
  } else {
    const image = column.images[0]
    content = (
      <>
        {image === undefined ? null : (
          <img
            className="compare-table__image"
            src={image.thumbnail.url}
            width={image.thumbnail.width}
            height={image.thumbnail.height}
            alt={t('detail.gallery.alt', {
              model: column.modelName,
              year: column.modelYear,
              index: 1,
              total: column.images.length,
            })}
          />
        )}
        <a className="compare-table__model" href={listingPath(locale, column.slug)} dir="auto">
          {column.modelName}
        </a>
        {column.isReserved ? <Badge>{t('detail.reserved')}</Badge> : null}
        <FavoriteButton listing={column} />
      </>
    )
  }

  return <div className="compare-table__head">{content}</div>
}
