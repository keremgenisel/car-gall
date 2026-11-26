import { useLocale } from './localeContext'
import { LocaleSwitcher } from './LocaleSwitcher'
import { galleryDisplayName, galleryLogo } from '../shared/lib/galleryName'
import { homePath } from '../shared/lib/route'

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
  const { locale } = useLocale()
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
        <LocaleSwitcher />
      </div>
    </header>
  )
}
