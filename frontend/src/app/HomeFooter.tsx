import type { ReactNode } from 'react'
import type { ContentNavigationLink } from '../shared/api/content'
import { galleryDisplayName, galleryLogo } from '../shared/lib/galleryName'
import { enabledLocales } from '../shared/lib/locale'
import { contentPath, homePath, listPath } from '../shared/lib/route'
import { useLocale } from './localeContext'

/**
 * 0014 R7 · D5, D14 — ana sayfanın altlığı. Bağlantılar YALNIZ var olan hedeflere gider:
 * araç listesi, ETKİN locale'lerin ana sayfaları ve şube telefonları (AC-46). Kurumsal
 * sayfalar yok (`Content` yok, X6); dil listesi koddan değil kurulumdan gelir (AC-67).
 *
 * `0028` **D17** — `id="contact"` kalktı: "Galeriye ulaşın" iletişim sayfasına gider (AC-63).
 */
export function HomeFooter({
  branches,
  corporate,
  social,
}: {
  readonly branches: ReactNode
  /**
   * `0018` **D36** — kurumsal bağlantılar ÇAĞIRANDAN gelir: bülten notunun KVKK bağlantısı da
   * aynı okumadan beslenir ve ana sayfaya ikinci bir `Content` isteği eklenmez.
   */
  readonly corporate: readonly ContentNavigationLink[]
  /**
   * `0024` D6 — "Bizi takip edin" bloğu; şube bloğu gibi ÇAĞIRANDAN gelir ve aynı galeri
   * okumasından beslenir (D7: ikinci istek yok). "Kurumsal"dan sonra, "Diller"den önce.
   */
  readonly social?: ReactNode
}) {
  const { locale: current, t } = useLocale()
  const name = galleryDisplayName()
  const logo = galleryLogo()

  return (
    <footer className="home-footer" data-testid="home-footer">
      <div className="home-footer__inner">
        {/*
          `0026` D5 · R4a — altlığın başında, şube bloğundan önce; üst çubukla AYNI `logo-small`
          (ikinci indirme yok). Yanında ad yazmaz, bu yüzden `alt` görünen addır; bağlantı değildir.
          Katlamanın altında: `loading="lazy"`, LCP adayı değil. Ad yoksa logo da yok.
        */}
        {logo === null || name === null ? null : (
          <img
            className="home-footer__logo"
            src={logo.url}
            width={logo.width}
            height={logo.height}
            alt={name}
            loading="lazy"
            decoding="async"
            data-testid="footer-logo"
          />
        )}
        {branches}

        <nav className="home-footer__nav" aria-label={t('home.footer.label')}>
          <h2 className="home-footer__heading">{t('home.footer.explore')}</h2>
          <a className="home-footer__link" href={listPath(current)}>
            {t('home.footer.cars')}
          </a>
        </nav>

        {/*
          `0017` R21 — "Kurumsal" kolonu: sistem sayfaları sabit sırada (about, faq, privacy,
          cookies), yalnız bu dilde yayında olanlar; hiçbiri yoksa kolon HİÇ basılmaz
          (AC-89). Bağlantı metni sayfanın bu dildeki başlığıdır (D35); adres güncel slug'a
          gider, eski adrese ve 301'e uğramaz (AC-92). Okuma başarısız olursa kolon basılmaz:
          altlık bir ikincil gezinmedir ve hata ekranı gerektirmez.
        */}
        {corporate.length === 0 ? null : (
          <nav
            className="home-footer__nav"
            aria-label={t('home.footer.corporate')}
            data-testid="footer-corporate"
          >
            <h2 className="home-footer__heading">{t('home.footer.corporate')}</h2>
            {corporate.map((link) => (
              <a
                key={link.systemKey}
                className="home-footer__link"
                href={contentPath(current, link.slug)}
                dir="auto"
              >
                {link.title}
              </a>
            ))}
          </nav>
        )}

        {social}

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
