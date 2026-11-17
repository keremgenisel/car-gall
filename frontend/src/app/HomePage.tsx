import { HomeCatalog } from '../features/catalog/home/HomeCatalog'
import { GalleryName } from '../features/dealership/GalleryName'
import type { GalleryLoad } from '../features/dealership/useGallery'
import { useLocale } from './localeContext'

/**
 * 0014 — ana sayfa. İki modülün yüzeyi burada BİRLEŞİR (D26): öne çıkanlar ve form
 * `features/catalog`'dan, galeri adı `features/dealership`'ten. Bir feature diğerini import
 * edemez (docs/frontend.md § 1); ikisini birlikte tanıyabilen tek katman `app/`.
 *
 * Giriş bölümünün metni çeviri kataloğundan gelir (D2) ve bu sürümde GÖRSEL YOKTUR (D18,
 * AC-71).
 */
export function HomePage({ gallery }: { readonly gallery: GalleryLoad }) {
  const { t } = useLocale()

  return (
    <div className="page home">
      <section className="home-hero" data-testid="home-intro">
        <GalleryName load={gallery} />
        <h1 className="home-hero__title">{t('home.intro.title')}</h1>
        <p className="home-hero__body">{t('home.intro.body')}</p>
      </section>

      <HomeCatalog />
    </div>
  )
}
