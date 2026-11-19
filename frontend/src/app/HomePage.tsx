import { HomeCatalog } from '../features/catalog/home/HomeCatalog'
import { GalleryName } from '../features/dealership/GalleryName'
import { NewsletterSection } from '../features/notifications/NewsletterSection'
import type { GalleryLoad } from '../features/dealership/useGallery'
import { homeText } from '../shared/lib/homeTexts'
import { useLocale } from './localeContext'

/**
 * 0014 — ana sayfa. İki modülün yüzeyi burada BİRLEŞİR (D26): öne çıkanlar ve form
 * `features/catalog`'dan, galeri adı `features/dealership`'ten, bülten bölümü (`0018`)
 * `features/notifications`'tan. Bir feature diğerini import
 * edemez (docs/frontend.md § 1); ikisini birlikte tanıyabilen tek katman `app/`.
 *
 * Giriş bölümünün metni galerinin panelden kaydettiği metindir; o dilde kayıt yoksa çeviri
 * kataloğundaki varsayılan (`0022` R2 · D6). Bu sürümde GÖRSEL YOKTUR (D18, AC-71).
 */
export function HomePage({
  gallery,
  privacyHref,
}: {
  readonly gallery: GalleryLoad
  /** `0018` **D15** — KVKK aydınlatma metninin adresi; o dilde yayında değilse `null`. */
  readonly privacyHref: string | null
}) {
  const { t } = useLocale()

  return (
    <div className="page home">
      <section className="home-hero" data-testid="home-intro">
        <GalleryName load={gallery} />
        <h1 className="home-hero__title">{homeText('intro-title', t('home.intro.title'))}</h1>
        <p className="home-hero__body">{homeText('intro-body', t('home.intro.body'))}</p>
      </section>

      <HomeCatalog />

      {/* `0018` D16 — `HomeCatalog`'un hemen altında, altlıktan önce; vitrin boşken de. */}
      <NewsletterSection privacyHref={privacyHref} />
    </div>
  )
}
