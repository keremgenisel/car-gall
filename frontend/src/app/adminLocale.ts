import { isAppLocale, type AppLocale } from '../shared/lib/locale'

/**
 * Panelin dili SUNUCUDAN gelir: kabuk `<html lang dir>`'i personelin tercih ettiği
 * dille basar (0006 R16, AC-54, AC-55). Uç onu yeniden HESAPLAMAZ, yalnız okur —
 * `docs/frontend.md` § 6 yönün uçta hesaplanmasını yasaklıyor.
 *
 * Vitrin dili yol önekinden gelir; panel dili oturumdan. İkisi ayrı eksendir ve
 * karıştırmak, Arapça vitrinden panele geçen Türk danışmanın paneli Arapça görmesine
 * yol açar.
 */
export function adminLocaleFromDocument(fallback: AppLocale): AppLocale {
  const lang = document.documentElement.lang

  return isAppLocale(lang) ? lang : fallback
}
