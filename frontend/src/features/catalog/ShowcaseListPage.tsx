import { useCallback, useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import {
  fetchReferenceTerms,
  fetchShowcaseBrands,
  fetchShowcaseListings,
} from '../../shared/api/catalog'
import type { PagedResult, ShowcaseListing } from '../../shared/api/types'
import { ListingCard } from './ListingCard'
import { loadBodyTypeOptions } from './bodyTypeOptions'
import { ListFilters, type FilterOptions } from './filters/ListFilters'
import { SortSelect } from './filters/SortSelect'
import { Pagination } from './Pagination'
import { EmptyByFilter } from './states/EmptyByFilter'
import { EmptyShowcase } from './states/EmptyShowcase'
import { ListError } from './states/ListError'
import { ListSkeleton } from './states/ListSkeleton'
import {
  activeAxes,
  clearFilters,
  toCanonicalSearch,
  valuesOf,
  withPage,
  withSort,
} from './listUrlState'
import { replaceCanonicalPage, useListUrlState } from './useListUrlState'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly page: PagedResult<ShowcaseListing>
      readonly showcaseIsEmpty: boolean
    }

type OptionsLoad =
  { readonly kind: 'loading' } | { readonly kind: 'done'; readonly value: FilterOptions | null }

export function ShowcaseListPage() {
  const { locale, t } = useLocale()
  const [state, pushState] = useListUrlState()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [optionsResult, setOptionsResult] = useState<{
    readonly key: string
    readonly value: OptionsLoad
    /** `0021` D21 — okumanın sorduğu seçili markalar; eksen onları stoksuz olsalar da taşır. */
    readonly requestedBrands: readonly string[]
  } | null>(null)
  const [reloadToken, setReloadToken] = useState(0)

  // Yükleniyor durumu efekt içinde setState ile kurulmaz, istek anahtarından TÜRETİLİR:
  // senkron setState zincirleme render tetikler ve iskelet bir kare geç görünür (AC-25, CLS).
  const search = toCanonicalSearch(state)
  const requestKey = `${locale}|${search}|${reloadToken}`
  const load: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }
  // `0021` D21 — seçenekler yalnız dil değişince ya da adres eksende OLMAYAN bir marka
  // getirdiğinde (geri tuşu, paylaşılan bağlantı) yeniden okunur; eksenin kendi çipine basmak
  // okuma tetiklemez.
  const selectedBrands = valuesOf(state, 'brands')
  const optionsKey = brandOptionsKey(locale, selectedBrands, optionsResult)
  const options: OptionsLoad =
    optionsResult !== null && optionsResult.key === optionsKey
      ? optionsResult.value
      : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    // Kasa tipi ekseni alan ayarlarının kapısından geçer: alan public değilse eksen HİÇ
    // gösterilmez (`0001` R8, AC-12). Yakıt ve vites çekirdek alandır, her zaman görünür
    // (`0015` R8); değerleri mevcut public uçlardan gelir (D17). Okunamazlarsa çok değerli
    // eksenler gizlenir, aralık eksenleri ve sıralama çalışmaya devam eder.
    Promise.all([
      loadBodyTypeOptions(locale, controller.signal),
      fetchReferenceTerms('fuel-types', locale, controller.signal),
      fetchReferenceTerms('transmission-types', locale, controller.signal),
      fetchShowcaseBrands(selectedBrands, locale, controller.signal),
    ])
      .then(([bodyTypeOptions, fuels, transmissions, brands]) =>
        setOptionsResult({
          key: optionsKey,
          requestedBrands: selectedBrands,
          value: {
            kind: 'done',
            value: {
              bodyTypeFilterable: bodyTypeOptions.filterable,
              bodyTypes: bodyTypeOptions.bodyTypes,
              fuels,
              transmissions,
              brands,
            },
          },
        }),
      )
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setOptionsResult({
            key: optionsKey,
            requestedBrands: selectedBrands,
            value: { kind: 'done', value: null },
          })
        }
      })

    return () => controller.abort()
    // `selectedBrands` anahtarda (`optionsKey`) temsil ediliyor; dizi kimliği her render'da değişir.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [locale, optionsKey])

  useEffect(() => {
    const controller = new AbortController()

    fetchShowcaseListings(state, locale, controller.signal)
      .then(async (page) => {
        if (page.page !== state.page) {
          replaceCanonicalPage(withPage(state, page.page))
        }

        // İki boş durum ayrılır. Daraltma boş sonuç verdiyse vitrinde hiç ilan olup
        // olmadığı ayrıca sorulur; ikisi aynı anda doğruysa (b) kazanır (R12, AC-27).
        const filtered = activeAxes(state).length > 0
        let showcaseIsEmpty = page.totalCount === 0 && !filtered

        if (page.totalCount === 0 && filtered) {
          const unfiltered = await fetchShowcaseListings(
            { bodyTypes: [], page: 1 },
            locale,
            controller.signal,
          )
          showcaseIsEmpty = unfiltered.totalCount === 0
        }

        setResult({ key: requestKey, value: { kind: 'ready', page, showcaseIsEmpty } })
      })
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setResult({ key: requestKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
    // `state` istek anahtarında (`search`) temsil ediliyor; nesne kimliği her render'da değişir.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [locale, requestKey])

  const onClear = useCallback(() => pushState(clearFilters(state)), [pushState, state])
  const onPageChange = useCallback(
    (page: number) => pushState(withPage(state, page)),
    [pushState, state],
  )

  return (
    <div className="page">
      <h1 className="page__title">{t('showcase.title')}</h1>

      <ListFilters
        state={state}
        options={options.kind === 'loading' ? 'loading' : options.value}
        onChange={pushState}
      />

      <div className="list-toolbar">
        {load.kind === 'ready' ? (
          <p className="result-count" data-testid="result-count">
            {t('showcase.resultCount', { count: load.page.totalCount })}
          </p>
        ) : (
          <span />
        )}
        <SortSelect sort={state.sort} onChange={(sort) => pushState(withSort(state, sort))} />
      </div>

      {load.kind === 'loading' ? <ListSkeleton /> : null}
      {load.kind === 'error' ? (
        <ListError onRetry={() => setReloadToken((token) => token + 1)} />
      ) : null}

      {load.kind === 'ready' ? (
        load.page.totalCount === 0 ? (
          load.showcaseIsEmpty ? (
            <EmptyShowcase />
          ) : (
            <EmptyByFilter axes={activeAxes(state)} onClear={onClear} />
          )
        ) : (
          <>
            <ul className="listing-grid">
              {load.page.items.map((listing, index) => (
                <ListingCard
                  key={listing.listingId}
                  listing={listing}
                  /* Mobilde tek sütun, masaüstünde ilk satır: ilk dört kart her iki
                     düzende de katlama üstündedir. */
                  aboveTheFold={index < 4}
                />
              ))}
            </ul>
            <Pagination
              page={load.page.page}
              totalPages={load.page.totalPages}
              onChange={onPageChange}
            />
          </>
        )
      ) : null}
    </div>
  )
}

/**
 * `0021` D21 — marka seçeneklerinin okuma anahtarı. Seçili markaların hepsi son okumada ya
 * soruldu ya da döndüyse anahtar değişmez; değilse (adres eksende olmayan bir marka getirdi)
 * seçimin kendisi anahtara girer ve okuma yinelenir.
 */
function brandOptionsKey(
  locale: string,
  selected: readonly string[],
  last: {
    readonly key: string
    readonly value: OptionsLoad
    readonly requestedBrands: readonly string[]
  } | null,
): string {
  if (last !== null && last.key.startsWith(`${locale}|`)) {
    const known = new Set(last.requestedBrands)
    if (last.value.kind === 'done' && last.value.value !== null) {
      for (const brand of last.value.value.brands) {
        known.add(brand.slug)
      }
    }

    if (selected.every((slug) => known.has(slug))) {
      return last.key
    }
  }

  return `${locale}|${[...selected].sort().join(',')}`
}
