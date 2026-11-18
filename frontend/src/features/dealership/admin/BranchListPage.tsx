import { useEffect, useState } from 'react'
import { useLocale } from '../../../app/localeContext'
import { fetchBranchList } from '../../../shared/api/dealershipAdmin'
import type { BranchSummary } from '../../../shared/api/types'
import { adminPath, branchEditPath } from '../../../shared/lib/route'
import { Badge, EmptyState, ErrorState, Skeleton } from '../../../shared/ui'
import { statusKey } from './dealershipForm'

type Load =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly branches: readonly BranchSummary[] }

/**
 * `0020` R8 — galerinin BÜTÜN şubeleri: aktif, geçici kapalı ve kapatılmış, durumlarıyla
 * (AC-19). Ad yöneticinin panel dilindedir, yoksa varsayılan dildeki (AC-20) — çözüm
 * sunucuda. Liste araç formunun okuduğu listenin aynısıdır (P5, AC-68).
 *
 * Durum yalnız renkle anlatılmaz; rozet metni taşır (docs/frontend.md § 7).
 */
export function BranchListPage() {
  const { locale, t } = useLocale()
  const [load, setLoad] = useState<Load>({ kind: 'loading' })
  const [reload, setReload] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    fetchBranchList(locale, controller.signal)
      .then((branches) => setLoad({ kind: 'ready', branches }))
      .catch((cause: unknown) => {
        if (!(cause instanceof DOMException && cause.name === 'AbortError')) {
          setLoad({ kind: 'error' })
        }
      })

    return () => controller.abort()
  }, [locale, reload])

  const header = (
    <div className="admin-page__header">
      <h1 className="admin-page__title">{t('dealership.branches.title')}</h1>
      <a
        className="ui-button ui-button--primary"
        href={adminPath('branch-new')}
        data-testid="branch-new-link"
      >
        {t('dealership.branches.new')}
      </a>
    </div>
  )

  if (load.kind === 'loading') {
    return (
      <div className="admin-page" data-testid="branches-loading" role="status">
        {header}
        <span className="visually-hidden">{t('dealership.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'error') {
    return (
      <div className="admin-page">
        {header}
        <ErrorState
          testId="branches-error"
          title={t('dealership.error.load.title')}
          description={t('dealership.error.load.body')}
          retryLabel={t('dealership.error.load.retry')}
          onRetry={() => {
            setLoad({ kind: 'loading' })
            setReload((value) => value + 1)
          }}
        />
      </div>
    )
  }

  if (load.branches.length === 0) {
    // B5 bu hâli normalde imkânsız kılar; yine de ekran neden boş olduğunu ve ne
    // yapılabileceğini söyler (docs/frontend.md § 3).
    return (
      <div className="admin-page">
        {header}
        <EmptyState
          testId="branches-empty"
          title={t('dealership.branches.empty.title')}
          description={t('dealership.branches.empty.body')}
        />
      </div>
    )
  }

  return (
    <div className="admin-page" data-testid="branches">
      {header}
      <ul className="dealership-branches">
        {load.branches.map((branch) => (
          <li className="dealership-branches__row" key={branch.branchId} data-testid="branch-row">
            <a
              className="dealership-branches__name"
              href={branchEditPath(branch.branchId)}
              dir="auto"
            >
              {branch.name}
            </a>
            <Badge>{t(statusKey(branch.status))}</Badge>
          </li>
        ))}
      </ul>
    </div>
  )
}
