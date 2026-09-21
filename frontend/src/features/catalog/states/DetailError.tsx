import { useLocale } from '../../../app/localeContext'
import { ErrorState } from '../../../shared/ui'

/**
 * ErrorState bilerek yalnız çevrilmiş metin kabul eder; errorCode/traceId/durum kodu
 * alan bir prop'u yoktur, dolayısıyla teknik detay yanlışlıkla ekrana basılamaz
 * (docs/frontend.md § 3.1, AC-19, AC-20).
 */
export function DetailError({ onRetry }: { readonly onRetry: () => void }) {
  const { t } = useLocale()

  return (
    <ErrorState
      testId="detail-error"
      title={t('detail.error.title')}
      description={t('detail.error.body')}
      retryLabel={t('detail.error.retry')}
      onRetry={onRetry}
    />
  )
}
