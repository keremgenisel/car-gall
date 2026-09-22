import { useLocale } from '../../../app/localeContext'
import { EmptyState } from '../../../shared/ui'

/**
 * AC-37/Karar D6 — onay metni talebin ALINDIĞINI bildirmekle sınırlıdır. "Galeri size
 * ulaşacak" demek, sistemin bugün yapısal olarak tutamayacağı bir sözdür: D1 talep okuma
 * yüzeyini, D2 bildirimi kapsam dışı bıraktı, yani bırakılan talebi bu aşamada kimse
 * görmüyor. Söz, `Identity` ve `Notifications` tamamlandığında eklenir (D5).
 */
export function LeadSuccess({ homeHref }: { readonly homeHref: string }) {
  const { t } = useLocale()

  return (
    <EmptyState
      title={t('lead.success.title')}
      description={t('lead.success.body')}
      action={
        <a className="ui-button ui-button--secondary" href={homeHref}>
          {t('lead.success.action')}
        </a>
      }
      testId="lead-success"
    />
  )
}
