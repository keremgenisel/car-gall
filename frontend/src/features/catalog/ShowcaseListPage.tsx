import { useCallback, useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchBodyTypes, fetchFieldSettings, fetchShowcaseListings } from '../../api/catalog'
import type { BodyType, PagedResult, ShowcaseListing } from '../../api/types'
import { ListingCard } from './ListingCard'
import { BodyTypeFilterBar } from './BodyTypeFilterBar'
import { Pagination } from './Pagination'
import { EmptyByFilter } from './states/EmptyByFilter'
import { EmptyShowcase } from './states/EmptyShowcase'
import { ListError } from './states/ListError'
import { ListSkeleton } from './states/ListSkeleton'
import { FilterBarSkeleton } from './states/FilterBarSkeleton'
import { clearBodyTypes, toggleBodyType, withPage } from './listUrlState'
import { replaceCanonicalPage, useListUrlState } from './useListUrlState'

const BODY_TYPE_FIELD = 'body_type'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly page: PagedResult<ShowcaseListing>
      readonly showcaseIsEmpty: boolean
    }

export function ShowcaseListPage() {
  const { locale, t } = useLocale()
  const [state, pushState] = useListUrlState()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [bodyTypes, setBodyTypes] = useState<readonly BodyType[]>([])
  const [filterGate, setFilterGate] = useState<'loading' | 'visible' | 'hidden'>('loading')
  const [reloadToken, setReloadToken] = useState(0)

  // Yükleniyor durumu efekt içinde setState ile kurulmaz, istek anahtarından TÜRETİLİR:
  // senkron setState zincirleme render tetikler ve iskelet bir kare geç görünür (AC-25, CLS).
  const requestKey = `${locale}|${[...state.bodyTypes].join(',')}|${state.page}|${reloadToken}`
  const load: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    // Filtre çubuğu alan ayarlarının kapısından geçer: kasa tipi alanı public değilse
    // çubuk HİÇ gösterilmez ve liste daraltmasız çalışmaya devam eder (R8, AC-12).
    Promise.all([
      fetchFieldSettings(locale, controller.signal),
      fetchBodyTypes(locale, controller.signal),
    ])
      .then(([settings, types]) => {
        const bodyTypeField = settings.fields.find((field) => field.fieldKey === BODY_TYPE_FIELD)
        setFilterGate(bodyTypeField?.isFilterable === true ? 'visible' : 'hidden')
        setBodyTypes(types)
      })
      .catch(() => setFilterGate('hidden'))

    return () => controller.abort()
  }, [locale])

  useEffect(() => {
    const controller = new AbortController()

    fetchShowcaseListings(
      { bodyTypes: state.bodyTypes, page: state.page },
      locale,
      controller.signal,
    )
      .then(async (page) => {
        if (page.page !== state.page) {
          replaceCanonicalPage({ bodyTypes: state.bodyTypes, page: page.page })
        }

        // İki boş durum ayrılır. Daraltma boş sonuç verdiyse vitrinde hiç ilan olup
        // olmadığı ayrıca sorulur; ikisi aynı anda doğruysa (b) kazanır (R12, AC-27).
        let showcaseIsEmpty = page.totalCount === 0 && state.bodyTypes.length === 0

        if (page.totalCount === 0 && state.bodyTypes.length > 0) {
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
  }, [locale, state.bodyTypes, state.page, reloadToken, requestKey])

  const onToggle = useCallback(
    (slug: string) => pushState(toggleBodyType(state, slug)),
    [pushState, state],
  )
  const onClear = useCallback(() => pushState(clearBodyTypes()), [pushState])
  const onPageChange = useCallback(
    (page: number) => pushState(withPage(state, page)),
    [pushState, state],
  )

  return (
    <main className="page">
      <h1 className="page__title">{t('showcase.title')}</h1>

      {filterGate === 'loading' ? <FilterBarSkeleton /> : null}
      {filterGate === 'visible' ? (
        <BodyTypeFilterBar
          bodyTypes={bodyTypes}
          selected={state.bodyTypes}
          onToggle={onToggle}
          onClear={onClear}
        />
      ) : null}

      {load.kind === 'loading' ? <ListSkeleton /> : null}
      {load.kind === 'error' ? (
        <ListError onRetry={() => setReloadToken((token) => token + 1)} />
      ) : null}

      {load.kind === 'ready' ? (
        <>
          <p className="result-count" data-testid="result-count">
            {t('showcase.resultCount', { count: load.page.totalCount })}
          </p>

          {load.page.totalCount === 0 ? (
            load.showcaseIsEmpty ? (
              <EmptyShowcase />
            ) : (
              <EmptyByFilter onClear={onClear} />
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
          )}
        </>
      ) : null}
    </main>
  )
}
