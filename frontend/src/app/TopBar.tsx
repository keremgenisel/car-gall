import { useLocale } from './localeContext'
import { FavoritesCounter } from './FavoritesCounter'
import { LocaleSwitcher } from './LocaleSwitcher'
import { galleryDisplayName, galleryLogo } from '../shared/lib/galleryName'
import { contactPath, homePath, parseRoute } from '../shared/lib/route'

/**
 * frontend-001 R1 — her vitrin sayfası bu kabuğun içinde gelir; sonraki dilimler
 * içerik alanına yerleşir ve kendi başlığını kurmaz.
 *
 * `0020` R4 · R5 · R5a — ad galerinin ziyaretçinin dilindeki GÖRÜNEN adıdır; çeviri
 * kataloğundaki yazılım adı değil. Sunucunun bastığı etiketten ilk boyamada okunur (P1, C6).
 * Ad, ziyaretçinin o anki dilindeki ana sayfaya götürür (D5): dil seçimi korunur.
 *
 * Ad okunamazsa yer BOŞ kalır (AC-17) ve boş bir bağlantı basılmaz — etiketsiz bir bağlantı
 * ekran okuyucuya adı olmayan bir hedef okuturdu. Ad JSX metni olarak basılır; içindeki `<`,
 * `"`, `&` işaretleme olarak yorumlanmaz (AC-18). `<bdi>` adı komşusundan yalıtır ve yönü
 * içerikten çözer: Latin ad Arapça sayfada da doğru dizilir (AC-12).
 */
export function TopBar() {
  const { locale, t } = useLocale()
  const name = galleryDisplayName()
  const logo = galleryLogo()

  return (
    <header className="top-bar">
      <div className="top-bar__inner">
        {name === null ? (
          <span className="top-bar__brand" data-testid="top-bar-brand" />
        ) : (
          <a
            className={logo === null ? 'top-bar__brand' : 'top-bar__brand top-bar__brand--logo'}
            href={homePath(locale)}
            data-testid="top-bar-brand"
          >
            {/*
              `0026` D5 — logo adın satır başı tarafında (ar'da sağda), dekoratif (`alt=""`):
              bağlantının adı görünen addır. `width`/`height` türevin gerçek boyutu ve yeri ayırır
              (CLS yok). Ön yükleme, `loading` ve `fetchpriority` YOK: listede LCP öğesi olabilir
              ve geç inmemeli (CLARIFY B2).
            */}
            {logo === null ? null : (
              <img
                className="top-bar__logo"
                src={logo.url}
                width={logo.width}
                height={logo.height}
                alt=""
                decoding="async"
                data-testid="top-bar-logo"
              />
            )}
            <bdi>{name}</bdi>
          </a>
        )}
        {/*
          `0028` D10 — her vitrin sayfasında iletişim bağlantısı: geniş ekranda metin, sayaçtan önce;
          dar ekranda 1. satırın sonunda yalnız simge (metin `visually-hidden`, erişilebilir ad tam
          metindir). Panelin kabuğu ayrı, orada yok (AC-65).
        */}
        <a
          className="top-bar__contact"
          href={contactPath(locale)}
          aria-current={
            parseRoute(window.location.pathname).kind === 'contact' ? 'page' : undefined
          }
          data-testid="top-bar-contact"
        >
          <svg
            className="top-bar__contact-icon"
            viewBox="0 0 24 24"
            width="24"
            height="24"
            aria-hidden="true"
            focusable="false"
          >
            <path d="M12 21s-7-6.2-7-11.5a7 7 0 0 1 14 0C19 14.8 12 21 12 21Z" />
            <circle cx="12" cy="9.5" r="2.5" />
          </svg>
          <span className="top-bar__contact-text">{t('contact.title')}</span>
        </a>
        {/* `frontend-003` D8 — sayaç dil değiştiricinin yanında, satır sonu tarafında. */}
        <div className="top-bar__end">
          <FavoritesCounter />
          <LocaleSwitcher />
        </div>
      </div>
    </header>
  )
}
