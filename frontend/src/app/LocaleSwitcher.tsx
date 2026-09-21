import { useLocale } from './localeContext'
import { SUPPORTED_LOCALES, type AppLocale } from '../shared/lib/locale'

/**
 * Dil değiştirici bir GEZİNMEDİR, tercih kaydı değildir (frontend-001 C8):
 * çerez veya localStorage yazmaz. Locale'in tek kaynağı URL yol önekidir (0001 C7).
 *
 * Bağlantılar tam sayfa gezinmesi yapar ve bu ZORUNLUDUR: `<html lang dir>` sunucudan
 * basılıyor (0001 AC-33), istemci tarafı bir geçiş onu güncelleyemez ve sayfa yanlış
 * yönde kalırdı.
 *
 * Daraltma ve sayfa korunur (AC-3): adresin yalnız dil öneki değişir.
 */
export function LocaleSwitcher() {
  const { locale: current, t } = useLocale()

  return (
    <nav className="top-bar__locales" aria-label={t('app.localeSwitcher.label')}>
      {SUPPORTED_LOCALES.map((locale) => (
        <a
          key={locale}
          href={hrefFor(locale)}
          className={
            locale === current ? 'top-bar__locale top-bar__locale--current' : 'top-bar__locale'
          }
          aria-current={locale === current ? 'true' : undefined}
          lang={locale}
        >
          {t(`app.locale.${locale}`)}
        </a>
      ))}
    </nav>
  )
}

function hrefFor(locale: AppLocale): string {
  const rest = window.location.pathname.replace(/^\/[^/]*/, '')
  return `/${locale}${rest}${window.location.search}`
}
