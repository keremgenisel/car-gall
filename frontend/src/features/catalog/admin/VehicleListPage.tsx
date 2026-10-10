import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { fetchVehicleSummary, fetchVehicles } from '../../../shared/api/catalogAdmin'
import type {
  PagedResult,
  VehicleAdminListItem,
  VehicleAdminSummary,
  VehicleSort,
} from '../../../shared/api/types'
import { formatDateTime, formatMoney, formatYear } from '../../../shared/i18n/format'
import { adminPath, vehicleEditPath } from '../../../shared/lib/route'
import { Badge, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { codeKey } from './vehicleFormState'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | {
      readonly kind: 'ready'
      readonly page: PagedResult<VehicleAdminListItem>
      readonly summary: VehicleAdminSummary
    }

/**
 * 0010b/R13 — kayıtlı araçların listesi: araç tanımı, fiyat, kilometre, yayın durumu,
 * eksik dil göstergesi, eksik bilgi göstergesi ve son güncelleme zamanı.
 *
 * <p>Sayfa ve sıralama ADRESTEN okunur ve adrese yazılır: ikisi de ekranın değil
 * personelin durumu; sekme yenilendiğinde kaybolmamalı ve bir meslektaşa
 * gönderilebilmeli.</p>
 *
 * <p>Üç durum da ele alınıyor (docs/frontend.md § 3): yükleniyor, boş, hata.</p>
 */
export function VehicleListPage() {
  const { locale, t } = useLocale()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [reloadToken, setReloadToken] = useState(0)
  const [page, setPage] = useState(() => readNumber('page', 1))
  const [sort, setSort] = useState<VehicleSort>(() => readSort())

  const requestKey = `${locale}|${page}|${sort}|${reloadToken}`
  const state: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetchVehicles(locale, page, sort, controller.signal),
      fetchVehicleSummary(locale, controller.signal),
    ])
      .then(([vehicles, summary]) =>
        setResult({ key: requestKey, value: { kind: 'ready', page: vehicles, summary } }),
      )
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setResult({ key: requestKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [locale, page, sort, reloadToken, requestKey])

  /** Adres ekranla birlikte değişir; tam gezinme YOK, yalnız adres çubuğu güncellenir. */
  function navigate(nextPage: number, nextSort: VehicleSort) {
    const query = new URLSearchParams()
    query.set('page', String(nextPage))
    query.set('sort', nextSort)
    window.history.replaceState(null, '', `${adminPath('vehicles')}?${query.toString()}`)

    setPage(nextPage)
    setSort(nextSort)
  }

  return (
    <div className="admin-page">
      <div className="admin-page__header">
        <h1 className="admin-page__title">{t('vehicle.list.title')}</h1>

        <a className="ui-button ui-button--primary" href={adminPath('vehicle-new')}>
          {t('vehicle.list.add')}
        </a>
      </div>

      <div className="vehicle-list__toolbar">
        <div className="admin-form__field">
          <label className="admin-form__label" htmlFor="vehicle-sort">
            {t('vehicle.list.sort')}
          </label>
          <select
            id="vehicle-sort"
            className="admin-form__control"
            value={sort}
            onChange={(event) => navigate(1, event.target.value as VehicleSort)}
          >
            <option value="last-updated">{t('vehicle.list.sort.lastUpdated')}</option>
            <option value="recently-added">{t('vehicle.list.sort.recentlyAdded')}</option>
          </select>
        </div>

        {/*
          AC-23 — sayı TÜM araçlar üzerinden ve o anki sayfaya bağlı değil. Sayfaya bağlı
          bir sayı, ikinci sayfaya geçince değişir ve personel "eksikler azaldı" sanırdı.
        */}
        {state.kind === 'ready' ? (
          <p className="vehicle-list__summary" data-testid="vehicle-summary">
            {t('vehicle.list.summary.translation', {
              count: state.summary.translationIncompleteCount,
              total: state.summary.totalCount,
            })}
          </p>
        ) : null}
      </div>

      {state.kind === 'loading' ? (
        <div className="vehicle-table-skeleton" data-testid="vehicle-skeleton">
          {[0, 1, 2, 3].map((row) => (
            <Skeleton key={row} shape="line" />
          ))}
        </div>
      ) : null}

      {state.kind === 'error' ? (
        <ErrorState
          title={t('vehicle.list.error.title')}
          description={t('vehicle.list.error.body')}
          retryLabel={t('vehicle.list.error.retry')}
          onRetry={() => setReloadToken((token) => token + 1)}
          testId="vehicle-error"
        />
      ) : null}

      {state.kind === 'ready' && state.page.items.length === 0 ? (
        <EmptyState
          title={t('vehicle.list.empty.title')}
          description={t('vehicle.list.empty.body')}
          action={
            <a className="ui-button ui-button--primary" href={adminPath('vehicle-new')}>
              {t('vehicle.list.add')}
            </a>
          }
          testId="vehicle-empty"
        />
      ) : null}

      {state.kind === 'ready' && state.page.items.length > 0 ? (
        <>
          <table className="admin-table" data-testid="vehicle-table">
            <thead>
              <tr>
                <th scope="col">{t('vehicle.column.vehicle')}</th>
                <th scope="col">{t('vehicle.column.price')}</th>
                <th scope="col">{t('vehicle.column.mileage')}</th>
                <th scope="col">{t('vehicle.column.status')}</th>
                <th scope="col">{t('vehicle.column.completeness')}</th>
                <th scope="col">{t('vehicle.column.updatedAt')}</th>
              </tr>
            </thead>
            <tbody>
              {state.page.items.map((vehicle) => {
                // Biçimlendirme UÇTA: sunucu ham sayı + para kodu + minor unit üssü
                // döner (docs/conventions.md § 7).
                const price = formatMoney(vehicle.price, locale)

                return (
                  <tr key={vehicle.vehicleId}>
                    <td dir="auto" data-testid="vehicle-row">
                      <a href={vehicleEditPath(vehicle.vehicleId)}>
                        {t('vehicle.list.definition', {
                          model: vehicle.modelName,
                          year: formatYear(vehicle.modelYear, locale),
                        })}
                      </a>
                    </td>
                    <td>{price ?? t('vehicle.price.onRequest')}</td>
                    <td>{t('vehicle.mileage.value', { kilometers: vehicle.mileageKilometers })}</td>
                    {/*
                      Durum KODU sunucudan geliyor ve çeviri burada yapılıyor (C11, Yasak
                      Listesi 22): veritabanında çevrilmiş durum adı saklanmaz.
                    */}
                    <td>{t(codeKey('vehicle.status', vehicle.listingStatus))}</td>
                    <td>
                      {/*
                        Göstergeler renkle değil METİNLE anlatılıyor (docs/frontend.md § 7):
                        renk tek başına anlam taşımaz.
                      */}
                      {vehicle.missingLocales.length > 0 ? (
                        <span data-testid="vehicle-missing-locales">
                          <Badge>
                            {t('vehicle.badge.missingLocales', {
                              locales: vehicle.missingLocales
                                .map((code) => t(`app.locale.${code}`))
                                .join(', '),
                            })}
                          </Badge>
                        </span>
                      ) : null}
                      {vehicle.hasMissingRequiredInfo ? (
                        <span data-testid="vehicle-missing-info">
                          <Badge>{t('vehicle.badge.missingInfo')}</Badge>
                        </span>
                      ) : null}
                      {vehicle.missingLocales.length === 0 && !vehicle.hasMissingRequiredInfo
                        ? t('vehicle.badge.complete')
                        : null}
                    </td>
                    <td>{formatDateTime(vehicle.updatedAt, locale)}</td>
                  </tr>
                )
              })}
            </tbody>
          </table>

          {state.page.totalPages > 1 ? (
            <nav className="vehicle-list__pager" aria-label={t('vehicle.list.pager')}>
              <button
                type="button"
                className="ui-button ui-button--secondary"
                disabled={state.page.page <= 1}
                onClick={() => navigate(state.page.page - 1, sort)}
              >
                {t('vehicle.list.previous')}
              </button>

              <span data-testid="vehicle-page-indicator">
                {t('vehicle.list.pageOf', {
                  page: state.page.page,
                  totalPages: state.page.totalPages,
                })}
              </span>

              <button
                type="button"
                className="ui-button ui-button--secondary"
                disabled={state.page.page >= state.page.totalPages}
                onClick={() => navigate(state.page.page + 1, sort)}
              >
                {t('vehicle.list.next')}
              </button>
            </nav>
          ) : null}
        </>
      ) : null}
    </div>
  )
}

function readNumber(key: string, fallback: number): number {
  const raw = new URLSearchParams(window.location.search).get(key)
  const value = raw === null ? Number.NaN : Number.parseInt(raw, 10)

  return Number.isFinite(value) && value > 0 ? value : fallback
}

/** Tanınmayan sıralama varsayılana düşer; adres çubuğu hata ekranının sebebi olamaz. */
function readSort(): VehicleSort {
  return new URLSearchParams(window.location.search).get('sort') === 'recently-added'
    ? 'recently-added'
    : 'last-updated'
}
