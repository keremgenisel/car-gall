import { useLocale } from '../../../app/localeContext'
import { homeText } from '../../../shared/lib/homeTexts'
import { EmptyState } from '../../../shared/ui'

/**
 * 0001 R12(b): ikisi aynı anda doğruysa BU kazanır (AC-27).
 *
 * `0022` R6 · D7 — başlık ve açıklama galerinin kaydettiği metindir (yoksa katalogdaki
 * varsayılan) ve ana sayfa ile liste AYNI bileşenden geçtiği için ikisinde de aynıdır.
 * Açıklamanın satır sonu yalnız burada korunur (D17); bağlantı metni katalogda kalır (X6).
 */
export function EmptyShowcase() {
  const { t } = useLocale()

  return (
    <EmptyState
      testId="empty-showcase"
      title={homeText('empty-title', t('showcase.empty.showcase.title'))}
      description={homeText('empty-body', t('showcase.empty.showcase.body'))}
      preserveLineBreaks
      action={
        <a className="ui-button ui-button--text" href="#contact">
          {t('showcase.empty.showcase.action')}
        </a>
      }
    />
  )
}
