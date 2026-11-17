import type { ReactNode } from 'react'
import { enabledLocales } from '../shared/lib/locale'
import { homePath, listPath } from '../shared/lib/route'
import { useLocale } from './localeContext'

/**
 * 0014 R7 · D5, D14 — ana sayfanın altlığı. Bağlantılar YALNIZ var olan hedeflere gider:
 * araç listesi, ETKİN locale'lerin ana sayfaları ve şube telefonları (AC-46). Kurumsal
 * sayfalar yok (`Content` yok, X6); dil listesi koddan değil kurulumdan gelir (AC-67).
 *
 * `id="contact"` boş vitrin durumunun "Galeriye ulaşın" eyleminin hedefidir.
 */
export function HomeFooter({ branches }: { readonly branches: ReactNode }) {
  const { locale: current, t } = useLocale()

  return (
    <footer className="home-footer" id="contact" data-testid="home-footer">
      <div className="home-footer__inner">
        {branches}

        <nav className="home-footer__nav" aria-label={t('home.footer.label')}>
          <h2 className="home-footer__heading">{t('home.footer.explore')}</h2>
          <a className="home-footer__link" href={listPath(current)}>
            {t('home.footer.cars')}
          </a>
        </nav>

        <nav className="home-footer__nav" aria-label={t('home.footer.languages')}>
          <h2 className="home-footer__heading">{t('home.footer.languages')}</h2>
          {enabledLocales().map((locale) => (
            <a
              key={locale}
              className="home-footer__link"
              href={homePath(locale)}
              lang={locale}
              aria-current={locale === current ? 'page' : undefined}
            >
              {t(`app.locale.${locale}`)}
            </a>
          ))}
        </nav>
      </div>
    </footer>
  )
}
