import { useLocale } from '../../../app/localeContext'
import { EmptyState } from '../../../shared/ui'

/**
 * R2/AC-11 — `Rezerve` ilanda form GÖSTERİLMEZ ve yerinde neden kapalı olduğu yazar.
 * Boş bir alan bırakmak ziyaretçiye formun yüklenmediğini düşündürürdü;
 * docs/frontend.md § 3 boş durumun nedenini söylemeyi zorunlu kılıyor.
 */
export function LeadClosed() {
  const { t } = useLocale()

  return (
    <EmptyState
      title={t('lead.closed.title')}
      description={t('lead.closed.body')}
      testId="lead-closed"
    />
  )
}
