import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchStaff } from '../../shared/api/identity'
import type { StaffListItem } from '../../shared/api/types'
import { AddStaffForm } from './AddStaffForm'
import { StaffListEmpty, StaffListError, StaffListSkeleton } from './states/StaffListStates'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly staff: readonly StaffListItem[] }

export function StaffListPage() {
  const { locale, t } = useLocale()
  const [result, setResult] = useState<{ readonly key: string; readonly value: LoadState } | null>(
    null,
  )
  const [reloadToken, setReloadToken] = useState(0)

  // Yükleniyor durumu efekt içinde setState ile kurulmaz, istek anahtarından TÜRETİLİR:
  // senkron setState zincirleme render tetikler ve iskelet bir kare geç görünür.
  const requestKey = `${locale}|${reloadToken}`
  const load: LoadState =
    result !== null && result.key === requestKey ? result.value : { kind: 'loading' }

  useEffect(() => {
    const controller = new AbortController()

    fetchStaff(locale, controller.signal)
      .then((staff) => setResult({ key: requestKey, value: { kind: 'ready', staff } }))
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
      <h1 className="admin-page__title">{t('staff.title')}</h1>

      {load.kind === 'loading' ? <StaffListSkeleton /> : null}
      {load.kind === 'error' ? (
        <StaffListError onRetry={() => setReloadToken((token) => token + 1)} />
      ) : null}

      {load.kind === 'ready' && load.staff.length === 0 ? <StaffListEmpty /> : null}

      {load.kind === 'ready' && load.staff.length > 0 ? (
        <table className="staff-table" data-testid="staff-table">
          <thead>
            <tr>
              <th scope="col">{t('staff.column.name')}</th>
              <th scope="col">{t('staff.column.loginId')}</th>
              <th scope="col">{t('staff.column.role')}</th>
              <th scope="col">{t('staff.column.status')}</th>
            </tr>
          </thead>
          <tbody>
            {load.staff.map((member) => (
              <tr key={member.staffId}>
                <td dir="auto">{member.displayName}</td>
                <td dir="ltr">{member.loginId}</td>
                {/* Rol ve durum KOD olarak gelir; görünen ad uçta çevrilir (C7). */}
                <td>{t(`staff.role.${member.role}`)}</td>
                <td>{t(`staff.status.${member.status}`)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      ) : null}

      <AddStaffForm onAdded={() => setReloadToken((token) => token + 1)} />
    </div>
  )
}
