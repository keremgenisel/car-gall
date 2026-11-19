import { useLocale } from '../../../app/localeContext'
import { Button } from '../../../shared/ui'

export type BannerState =
  | { readonly kind: 'none' }
  | { readonly kind: 'saved'; readonly messageKey: string }
  | { readonly kind: 'conflict' }
  | { readonly kind: 'failed'; readonly messageKey: string }

/**
 * `0021` — yazmanın sonucu. Eski sürüm reddi bir UYARIDIR ve güncel hâli yükleme yolu sunar;
 * genel ret teknik ayrıntı göstermez (docs/frontend.md § 3.1). `0020`'nin şeridiyle aynı desen.
 */
export function ReferenceBanner({
  state,
  onReload,
}: {
  readonly state: BannerState
  readonly onReload: () => void
}) {
  const { t } = useLocale()

  if (state.kind === 'conflict') {
    return (
      <div className="admin-form__error" role="alert" data-testid="reference-conflict">
        <p className="admin-form__error-body">{t('reference.conflict.body')}</p>
        <div className="admin-form__banner-action">
          <Button variant="secondary" data-testid="reference-conflict-reload" onClick={onReload}>
            {t('reference.conflict.reload')}
          </Button>
        </div>
      </div>
    )
  }

  if (state.kind === 'failed') {
    return (
      <div className="admin-form__error" role="alert" data-testid="reference-failed">
        <p className="admin-form__error-body">{t(state.messageKey)}</p>
      </div>
    )
  }

  if (state.kind === 'saved') {
    return (
      <div className="admin-form__notice" role="status" data-testid="reference-saved">
        <p className="admin-form__notice-body">{t(state.messageKey)}</p>
      </div>
    )
  }

  return null
}
