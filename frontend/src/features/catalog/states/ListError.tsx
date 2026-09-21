import { useLocale } from '../../../app/localeContext'
import { ErrorState } from '../../../shared/ui'

/**
 * ErrorState bilerek yalnız çevrilmiş metin kabul eder; errorCode/traceId alan bir
 * prop'u yoktur, dolayısıyla teknik detay yanlışlıkla ekrana basılamaz (§ 3.1).
 */
export function ListError({ onRetry }: { readonly onRetry: () => void }) {
  const { t } = useLocale()

  return (
    <ErrorState
      testId="list-error"
      title={t('showcase.error.title')}
      description={t('showcase.error.body')}
      retryLabel={t('showcase.error.retry')}
      onRetry={onRetry}
    />
  )
}
