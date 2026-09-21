import { useLocale } from '../../../app/localeContext'

/**
 * Kullanıcıya teknik detay SIZMAZ: HTTP durum kodu, errorCode, traceId, endpoint yolu
 * ve istisna metni gösterilmez (docs/frontend.md § 4, AC-28). Mesaj çeviri
 * katalogundan gelir ve ne yapılabileceğini söyler.
 */
export function ListError({ onRetry }: { readonly onRetry: () => void }) {
  const { t } = useLocale()

  return (
    <div className="state-panel" role="alert" data-testid="list-error">
      <h2>{t('showcase.error.title')}</h2>
      <p>{t('showcase.error.body')}</p>
      <button type="button" className="pagination__button" onClick={onRetry}>
        {t('showcase.error.retry')}
      </button>
    </div>
  )
}
