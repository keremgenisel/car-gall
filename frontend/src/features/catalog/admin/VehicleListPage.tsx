import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import type { AppLocale } from '../../../shared/lib/locale'
import {
  applyListingFeature,
  fetchVehicleSummary,
  fetchVehicles,
} from '../../../shared/api/catalogAdmin'
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
import { BulkActionBar } from './BulkActionBar'
import { retainVisible, selectionOf } from './bulkActionState'

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
  /**
   * 0012b **D19** — arşiv AÇIK bir filtreyle görünür ve filtre ADRESTE yaşıyor: sayfa ve
   * sıralama gibi o da ekranın değil personelin durumu, sekme yenilendiğinde kaybolmamalı.
   */
  const [archived, setArchived] = useState(() => readArchived())
  /**
   * `0012c` **D43** — seçim **sayfaya aittir** ve bu, seçimin kendi EVRENİYLE birlikte
   * saklanmasıyla sağlanıyor: evren değişince eski seçim okunmaz hâle gelir.
   *
   * <p>Temizleme neden bir `useEffect` DEĞİL: efekt içinde `setState` çağırmak basamaklı
   * render üretiyor (lint kuralı) ve bir kare boyunca eski seçim ekranda kalıyordu.
   * Anahtarla karşılaştırmak aynı sonucu render sırasında, fazladan tur olmadan veriyor —
   * bu dosyanın `result` için zaten kullandığı desen.</p>
   *
   * <p>Adreste YAŞAMIYOR: sayfa, sıralama ve arşiv filtresi personelin paylaşılabilir
   * durumu; seçim ise o anki ekranın durumu ve bir bağlantıda taşınmaz.</p>
   */
  const [selected, setSelected] = useState<{
    readonly key: string
    readonly ids: ReadonlySet<string>
  }>(() => ({ key: '', ids: new Set() }))

  const requestKey = `${locale}|${page}|${sort}|${archived}|${reloadToken}`
  const state: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    Promise.all([
      fetchVehicles(locale, page, sort, controller.signal, archived),
      fetchVehicleSummary(locale, controller.signal, archived),
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
  }, [locale, page, sort, archived, reloadToken, requestKey])

  /**
   * Seçimin evreni: sayfa · sıralama · arşiv filtresi · dil. `reloadToken` BİLEREK dışarıda
   * — satır başına öne çıkarma listeyi tazeliyor ve o tazeleme seçimi düşürmemeli.
   */
  const evrenKey = `${locale}|${page}|${sort}|${archived}`

  const seciliKimlikler: ReadonlySet<string> =
    selected.key === evrenKey ? selected.ids : new Set<string>()

  /**
   * SON başarıyla yüklenen sayfa. Tazeleme sırasında `state` yükleniyor hâline dönüyor;
   * kısmi başarı listesi araç ADLARINI o anda da gösterebilmeli — kimlik personele bir
   * şey söylemez.
   */
  const sonYuklenen: readonly VehicleAdminListItem[] =
    result?.value.kind === 'ready' ? result.value.page.items : []

  const gorunenler: readonly string[] =
    state.kind === 'ready' ? state.page.items.map((item) => item.vehicleId) : []

  // Sayfada olmayan bir kimlik seçimde kalamaz: tazeleme sonrası kaybolan bir araç
  // seçimde asılı kalırsa toplu eylem personelin görmediği bir kayda uygulanır.
  const secim = selectionOf(gorunenler, retainVisible(seciliKimlikler, gorunenler))

  /**
   * Başlıktaki kutunun üçüncü hâli: bazıları seçili. Karşılaştırmanın iki yanı da `>`
   * ile yazılı — `A > 0 && A < B` biçimi gömülü metin kapısını (`check:text`) yanlış
   * tetikliyor, çünkü `>` ile `<` arasındaki her şey ona bir etiket içeriği gibi görünüyor.
   */
  const yarimSecim = secim.length > 0 && gorunenler.length > secim.length

  function yazSecim(ids: ReadonlySet<string>) {
    setSelected({ key: evrenKey, ids })
  }

  function degistirSecim(vehicleId: string, secili: boolean) {
    const sonraki = new Set(seciliKimlikler)

    if (secili) {
      sonraki.add(vehicleId)
    } else {
      sonraki.delete(vehicleId)
    }

    yazSecim(sonraki)
  }

  /** `0012c` **R5** — satır başına öne çıkarma anahtarı; eylem YALNIZ burada (**D51**). */
  async function oneCikar(vehicleId: string, featured: boolean) {
    await applyListingFeature(vehicleId, featured, locale)
    setReloadToken((token) => token + 1)
  }

  /** Adres ekranla birlikte değişir; tam gezinme YOK, yalnız adres çubuğu güncellenir. */
  function navigate(nextPage: number, nextSort: VehicleSort, nextArchived = archived) {
    const query = new URLSearchParams()
    query.set('page', String(nextPage))
    query.set('sort', nextSort)

    if (nextArchived) {
      query.set('archived', 'true')
    }

    window.history.replaceState(null, '', `${adminPath('vehicles')}?${query.toString()}`)

    setPage(nextPage)
    setSort(nextSort)
    setArchived(nextArchived)
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
          0012b D19 — arşiv filtresi. Varsayılan KAPALI: arşiv bu projenin "silme"sidir
          (`0010b` X19) ve varsayılan listede kalırsa liste sonsuz büyür, arşivleme eylemi
          de hiçbir şey yapmamış görünür.
        */}
        <div className="vehicle-list__filters">
          <label className="admin-form__check" htmlFor="vehicle-archived">
            <input
              id="vehicle-archived"
              type="checkbox"
              checked={archived}
              onChange={(event) => navigate(1, sort, event.target.checked)}
            />
            {archived ? t('vehicle.list.archived.hide') : t('vehicle.list.archived.show')}
          </label>
          <span className="admin-form__hint">{t('vehicle.list.archived.hint')}</span>
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

      {/*
        Toplu eylem çubuğu tablonun ÜSTÜNDE ve "hazır" dalının DIŞINDA.

        Dışında olması bir kusurun bedeli: içindeyken her tazeleme onu UNMOUNT ediyordu ve
        eylemin sonucu — başarı bildirimi ya da kısmi başarı listesi — ekrandan siliniyordu.
        Eylem listeyi tazeliyor, yani kutu tam olarak gösterilmesi gereken anda kayboluyordu.
        `0012b`'de aynı sınıftan bir kusur düzenleme ekranında yakalanmıştı ve e2e yakaladı.
      */}
      {sonYuklenen.length > 0 ? (
        <BulkActionBar
          selection={secim}
          labelOf={(vehicleId) => AracAdi(sonYuklenen, vehicleId, locale, t)}
          onApplied={() => {
            yazSecim(new Set())
            setReloadToken((token) => token + 1)
          }}
        />
      ) : null}

      {state.kind === 'ready' && state.page.items.length > 0 ? (
        <>
          {/*
            Telefon genişliğinde altı sütunluk tablo TAŞIYOR ve altındaki sayfalayıcıyı
            örtüyordu — e2e'de ölçüldü: "Sonraki" düğmesine yapılan tıklamayı tablonun
            hücreleri yakalıyordu. Taşma tablonun KENDİ kapsayıcısında kalıyor; sayfanın
            yatay kaydırması yok (docs/frontend.md § 7).
          */}
          <div className="admin-table__scroll">
            <table className="admin-table" data-testid="vehicle-table">
              <thead>
                <tr>
                  {/*
                    "Hepsini seç" SAYFADAKİLERİ seçer (**D43**): seçim sayfaya aittir ve
                    sayfa değişince temizlenir. Başlıktaki kutu üç hâl taşımıyor; yarım
                    seçimde `indeterminate` görünüyor ama değeri `false`.
                  */}
                  <th scope="col" className="admin-table__select">
                    <label className="admin-form__check">
                      <input
                        type="checkbox"
                        checked={secim.length > 0 && secim.length === gorunenler.length}
                        ref={(element) => {
                          if (element !== null) {
                            element.indeterminate = yarimSecim
                          }
                        }}
                        onChange={(event) =>
                          yazSecim(
                            event.target.checked
                              ? new Set(state.page.items.map((item) => item.vehicleId))
                              : new Set(),
                          )
                        }
                        data-testid="vehicle-select-all"
                      />
                      <span className="visually-hidden">{t('vehicle.bulk.selectAll')}</span>
                    </label>
                  </th>
                  <th scope="col">{t('vehicle.column.vehicle')}</th>
                  <th scope="col">{t('vehicle.column.price')}</th>
                  <th scope="col">{t('vehicle.column.mileage')}</th>
                  <th scope="col">{t('vehicle.column.status')}</th>
                  <th scope="col">{t('vehicle.column.completeness')}</th>
                  <th scope="col">{t('vehicle.column.updatedAt')}</th>
                  <th scope="col">{t('vehicle.column.featured')}</th>
                </tr>
              </thead>
              <tbody>
                {state.page.items.map((vehicle) => {
                  // Biçimlendirme UÇTA: sunucu ham sayı + para kodu + minor unit üssü
                  // döner (docs/conventions.md § 7).
                  const price = formatMoney(vehicle.price, locale)

                  return (
                    <tr key={vehicle.vehicleId}>
                      <td className="admin-table__select">
                        <label className="admin-form__check">
                          <input
                            type="checkbox"
                            checked={secim.includes(vehicle.vehicleId)}
                            onChange={(event) =>
                              degistirSecim(vehicle.vehicleId, event.target.checked)
                            }
                            data-testid={`vehicle-select-${vehicle.vehicleId}`}
                          />
                          <span className="visually-hidden">
                            {t('vehicle.bulk.select', {
                              model: vehicle.modelName,
                              year: formatYear(vehicle.modelYear, locale),
                            })}
                          </span>
                        </label>
                      </td>
                      <td dir="auto" data-testid="vehicle-row">
                        <a href={vehicleEditPath(vehicle.vehicleId)}>
                          {t('vehicle.list.definition', {
                            model: vehicle.modelName,
                            year: formatYear(vehicle.modelYear, locale),
                          })}
                        </a>
                      </td>
                      <td>{price ?? t('vehicle.price.onRequest')}</td>
                      <td>
                        {t('vehicle.mileage.value', { kilometers: vehicle.mileageKilometers })}
                      </td>
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
                        {/*
                          0012b R13 — şubesi kapatılmış araç AYIRT EDİLİYOR. Rozet renkle
                          değil METİNLE anlatıyor (§ 7); ne yapılacağını düzenleme ekranı
                          söylüyor — şube atanmadan yayına alınamaz.
                        */}
                        {vehicle.awaitingBranchAssignment ? (
                          <span data-testid="vehicle-awaiting-branch">
                            <Badge>{t('vehicle.badge.awaitingBranch')}</Badge>
                          </span>
                        ) : null}
                        {vehicle.missingLocales.length === 0 &&
                        !vehicle.hasMissingRequiredInfo &&
                        !vehicle.awaitingBranchAssignment
                          ? t('vehicle.badge.complete')
                          : null}
                      </td>
                      <td>{formatDateTime(vehicle.updatedAt, locale)}</td>
                      {/*
                        0012c D50 — eylemin değiştirdiği durum, eylemin yaşadığı yüzeyde
                        görünüyor. Rozet olmadan öne çıkarma yalnızca YAZAN bir düğme
                        olurdu: personel hangi satırın öne çıkarıldığını göremezdi.
                      */}
                      <td>
                        {vehicle.isFeatured ? (
                          <span data-testid="vehicle-featured">
                            <Badge>{t('vehicle.badge.featured')}</Badge>
                          </span>
                        ) : null}
                        <button
                          type="button"
                          className="ui-button ui-button--secondary vehicle-list__feature"
                          onClick={() => void oneCikar(vehicle.vehicleId, !vehicle.isFeatured)}
                          data-testid={`vehicle-feature-${vehicle.vehicleId}`}
                        >
                          {vehicle.isFeatured
                            ? t('vehicle.action.unfeature')
                            : t('vehicle.action.feature')}
                        </button>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>

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

/** Tanınmayan değer KAPALI'ya düşer; adres çubuğundan gelen bir ipucu hata ekranı üretmez. */
function readArchived(): boolean {
  return new URLSearchParams(window.location.search).get('archived') === 'true'
}

/**
 * Kısmi başarı listesindeki araç adı. Seçim sayfaya ait olduğu için (**D43**) adlar
 * zaten elimizde ve ikinci bir okuma gerekmiyor — kararın ölçülmüş yan faydası.
 *
 * <p>Araç listeden düşmüşse kimlik gösterilmiyor: personele bir `Guid` göstermek hiçbir
 * şey söylemez. Yerine genel bir ad basılıyor.</p>
 */
function AracAdi(
  items: readonly VehicleAdminListItem[],
  vehicleId: string,
  locale: AppLocale,
  t: (key: string, values?: Record<string, string | number>) => string,
): string {
  const arac = items.find((item) => item.vehicleId === vehicleId)

  return arac === undefined
    ? t('vehicle.bulk.unknown')
    : t('vehicle.list.definition', {
        model: arac.modelName,
        year: formatYear(arac.modelYear, locale),
      })
}
