import { useLocale } from '../../app/localeContext'
import { contactPath } from '../../shared/lib/route'
import { Skeleton } from '../../shared/ui'
import type { GalleryLoad } from './useGallery'

/**
 * 0014 R7 · D8, D13 — altlığın şube bloğu: aktif şubelerin adı, adresi ve telefonu.
 * Süzme ve sıra sunucudadır (AC-48, AC-66). Aktif şube yoksa blok HİÇ basılmaz — boş
 * durum değil, yokluk (AC-65). Galeri okuması düşerse yer boş kalır ve hata metni
 * basılmaz (AC-54): altlık bir hata ekranının yeri değildir.
 *
 * `0028` **D9** — saatler, "şu an" satırı ve özel günler iletişim sayfasına taşındı (SS4): altlık
 * saat parçasını istemez ve galeriyi yenilemez (AC-58, AC-60, AC-61). Bloğun SONUNDA, son şubenin
 * ardından tek bir "İletişim ve çalışma saatleri" bağlantısı vardır (CLARIFY B7(c), AC-59).
 */
export function BranchList({ load }: { readonly load: GalleryLoad }) {
  const { locale, t } = useLocale()

  if (load.kind === 'loading') {
    return (
      <div
        className="home-footer__branches"
        role="status"
        aria-live="polite"
        data-testid="branch-skeleton"
      >
        <span className="visually-hidden">{t('home.footer.loading')}</span>
        <Skeleton shape="line" />
        <Skeleton shape="line" />
      </div>
    )
  }

  if (load.kind === 'error' || load.gallery.branches.length === 0) {
    return null
  }

  return (
    <section
      className="home-footer__branches"
      data-testid="branch-list"
      aria-labelledby="branches-title"
    >
      <h2 className="home-footer__heading" id="branches-title">
        {t('home.footer.contact')}
      </h2>
      <ul className="home-footer__branch-items">
        {load.gallery.branches.map((branch) => (
          <li
            key={`${branch.name}|${branch.phone}`}
            className="home-footer__branch"
            data-testid="branch"
          >
            <p className="home-footer__branch-name" dir="auto">
              {branch.name}
            </p>
            <p className="home-footer__branch-address" dir="auto">
              {branch.address}
            </p>
            {/* AC-45 — E.164 numara `tel:` hedefidir. Numara her dilde soldan sağa okunur. */}
            <a className="home-footer__phone" href={`tel:${branch.phone}`} dir="ltr">
              {branch.phone}
            </a>
          </li>
        ))}
      </ul>
      <a
        className="home-footer__contact-link"
        href={contactPath(locale)}
        data-testid="footer-contact-link"
      >
        {t('home.footer.contactLink')}
      </a>
    </section>
  )
}
