import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { fetchVehicles } from '../../../shared/api/catalogAdmin'
import type { VehicleAdminListItem } from '../../../shared/api/types'
import { formatMoney, formatYear } from '../../../shared/i18n/format'
import { adminPath } from '../../../shared/lib/route'
import { EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { codeKey } from './vehicleFormState'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly vehicles: readonly VehicleAdminListItem[] }

/**
 * 0010a/R18 — SADE liste: araç tanımı, fiyat ve kilometre. Sayfalama, sıralama, eksik dil
 * ve eksik bilgi göstergeleri `0010b`'nin işi (X18); burası yalnız "kaydım göründü mü"
 * sorusunu cevaplar (AC-26).
 *
 * Üç durum da ele alınıyor (docs/frontend.md § 3): yükleniyor, boş, hata.
 */
export function VehicleListPage() {
  const { locale, t } = useLocale()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [reloadToken, setReloadToken] = useState(0)

  /**
   * Sonuç, ait olduğu İSTEĞİN anahtarıyla birlikte tutuluyor ve "yükleniyor" ondan
   * TÜRETİLİYOR; efektin içinde `setState` çağırmak yerine. İki nedeni var: dil
   * değiştiğinde elimizdeki liste artık o dile ait değil, ve efektin gövdesinde durumu
   * sıfırlamak basamaklı render üretiyor (`react-hooks/set-state-in-effect`).
   * `StaffListPage` aynı deseni kullanıyor.
   */
  const requestKey = `${locale}|${reloadToken}`
  const state: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    fetchVehicles(locale, controller.signal)
      .then((vehicles) => setResult({ key: requestKey, value: { kind: 'ready', vehicles } }))
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setResult({ key: requestKey, value: { kind: 'error' } })
      })

    return () => controller.abort()
  }, [locale, reloadToken, requestKey])

  return (
    <div className="admin-page">
      <div className="admin-page__header">
        <h1 className="admin-page__title">{t('vehicle.list.title')}</h1>

        {/*
          Kayıt formuna TAM GEZİNME ile gidilir: panelde router kütüphanesi yok ve rota
          adresten okunuyor (0002'den gelen yapı). Düğme değil bağlantı, çünkü hedefi
          olan bir eylem bağlantıdır — personel onu yeni sekmede de açabilir.
        */}
        <a className="ui-button ui-button--primary" href={adminPath('vehicle-new')}>
          {t('vehicle.list.add')}
        </a>
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

      {state.kind === 'ready' && state.vehicles.length === 0 ? (
        <EmptyState
          title={t('vehicle.list.empty.title')}
          description={t('vehicle.list.empty.body')}
          testId="vehicle-empty"
        />
      ) : null}

      {state.kind === 'ready' && state.vehicles.length > 0 ? (
        <table className="admin-table" data-testid="vehicle-table">
          <thead>
            <tr>
              <th scope="col">{t('vehicle.column.vehicle')}</th>
              <th scope="col">{t('vehicle.column.price')}</th>
              <th scope="col">{t('vehicle.column.mileage')}</th>
              <th scope="col">{t('vehicle.column.status')}</th>
            </tr>
          </thead>
          <tbody>
            {state.vehicles.map((vehicle) => {
              // Biçimlendirme UÇTA: sunucu ham sayı + para kodu + minor unit üssü döner
              // (docs/conventions.md § 7). Üç ondalıklı para birimi iki ondalıkla
              // gösterilirse bozulma SESSİZdir (P2).
              const price = formatMoney(vehicle.price, locale)

              return (
                <tr key={vehicle.vehicleId}>
                  <td dir="auto" data-testid="vehicle-row">
                    {t('vehicle.list.definition', {
                      model: vehicle.modelName,
                      year: formatYear(vehicle.modelYear, locale),
                    })}
                  </td>
                  <td>{price ?? t('vehicle.price.onRequest')}</td>
                  {/* Kilometre ICU'nun `number` biçimlendiricisinden geçer; vitrin
                      kartı da aynı yolu kullanıyor (docs/conventions.md § 7). */}
                  <td>{t('vehicle.mileage.value', { kilometers: vehicle.mileageKilometers })}</td>
                  {/*
                    Durum KODU sunucudan geliyor ve çeviri burada yapılıyor (C11, Yasak
                    Listesi 22): veritabanında çevrilmiş durum adı saklanmaz.
                  */}
                  <td>{t(codeKey('vehicle.status', vehicle.listingStatus))}</td>
                </tr>
              )
            })}
          </tbody>
        </table>
      ) : null}
    </div>
  )
}
