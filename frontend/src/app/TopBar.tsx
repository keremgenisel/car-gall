import { useLocale } from './localeContext'
import { LocaleSwitcher } from './LocaleSwitcher'

/**
 * frontend-001 R1 — her vitrin sayfası bu kabuğun içinde gelir; sonraki dilimler
 * içerik alanına yerleşir ve kendi başlığını kurmaz.
 *
 * Marka adı şimdilik çeviri kataloğundan geliyor. Doğru kaynağı `Dealership` modülünün
 * galeri profilidir (docs/architecture.md § 1); o modül geldiğinde buraya oradan beslenir.
 */
export function TopBar() {
  const { t } = useLocale()

  return (
    <header className="top-bar">
      <div className="top-bar__inner">
        <span className="top-bar__brand">{t('app.brandName')}</span>
        <LocaleSwitcher />
      </div>
    </header>
  )
}
