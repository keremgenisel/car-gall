import { useLocale } from '../../../app/localeContext'
import { EmptyState } from '../../../shared/ui'

/**
 * Bulunamadı, hata DEĞİLDİR: ziyaretçi yanlış bir şey yapmadı ve "tekrar dene" işe
 * yaramaz. Bu yüzden ErrorState değil EmptyState kullanılır ve eylem vitrine dönüştür
 * (AC-18).
 *
 * Metin, ilanın neden görünmediğini SÖYLEMEZ — satıldı mı, taslak mı, hiç var olmadı mı
 * ayrımı sızdırılamaz (C13, AC-8).
 *
 * Bağlantı, `EmptyShowcase`'teki desenle aynı: `Button` bir <button> üretir, bu eylem
 * ise gerçek bir gezinmedir.
 */
export function DetailNotFound({ homeHref }: { readonly homeHref: string }) {
  const { t } = useLocale()

  return (
    <EmptyState
      testId="detail-not-found"
      title={t('detail.notFound.title')}
      description={t('detail.notFound.body')}
      action={
        <a className="ui-button ui-button--secondary" href={homeHref}>
          {t('detail.notFound.action')}
        </a>
      }
    />
  )
}
