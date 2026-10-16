import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchTwoFactorStatus } from '../../shared/api/identity'
import type { TwoFactorStatus } from '../../shared/api/types'
import { Card, ErrorState, Skeleton } from '../../shared/ui'
import { TwoFactorManagePanel } from './TwoFactorManagePanel'
import { TwoFactorSetupPanel } from './TwoFactorSetupPanel'

type LoadState =
  | { readonly kind: 'loading' }
  | { readonly kind: 'error' }
  | { readonly kind: 'ready'; readonly status: TwoFactorStatus }

/**
 * Personelin kendi ikinci adım ekranı (R15–R17/D44). Adres `/admin/two-factor` ve
 * gezinmede **her role** basılıyor: kurulum yalnız kapılı durumda basılsaydı opsiyonel
 * rolün R15'i (kendi isteğiyle açma) ve D24'ün yenilemesi ulaşılamaz kalırdı.
 *
 * Veri çeken her ekran gibi üç durumu da ele alıyor (`docs/frontend.md` § 3).
 */
export function TwoFactorPage() {
  const { locale, t } = useLocale()
  const [state, setState] = useState<LoadState>({ kind: 'loading' })
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()

    // İSKELETE GERİ DÜŞÜLMÜYOR: yalnız ilk yükleme iskelet gösteriyor. Bir işlemden
    // sonraki tazelemede paneli kaldırmak sayfayı kısaltır ve ziyaretçi yerini kaybeder
    // (`StaffListPage` aynı kararı ölçümle yazıyor).
    fetchTwoFactorStatus(locale, controller.signal)
      .then((status) => setState({ kind: 'ready', status }))
      .catch((cause: unknown) => {
        if (cause instanceof DOMException && cause.name === 'AbortError') {
          return
        }

        setState({ kind: 'error' })
      })

    return () => controller.abort()
  }, [locale, reloadToken])

  return (
    <div className="admin-page admin-page--narrow">
      <Card as="div" testId="two-factor-page">
        <h1 className="admin-form__title">{t('twoFactor.title')}</h1>

        {state.kind === 'loading' ? (
          <div data-testid="two-factor-loading">
            <Skeleton shape="line" />
            <Skeleton shape="line" />
            <Skeleton shape="media" />
          </div>
        ) : null}

        {state.kind === 'error' ? (
          <ErrorState
            title={t('twoFactor.error.title')}
            description={t('twoFactor.error.body')}
            retryLabel={t('twoFactor.error.retry')}
            onRetry={() => setReloadToken((token) => token + 1)}
            testId="two-factor-load-error"
          />
        ) : null}

        {state.kind === 'ready' && state.status.enrolled ? (
          <TwoFactorManagePanel
            status={state.status}
            onChanged={() => setReloadToken((token) => token + 1)}
          />
        ) : null}

        {state.kind === 'ready' && !state.status.enrolled ? (
          <TwoFactorSetupPanel onEnrolled={() => setReloadToken((token) => token + 1)} />
        ) : null}
      </Card>
    </div>
  )
}
