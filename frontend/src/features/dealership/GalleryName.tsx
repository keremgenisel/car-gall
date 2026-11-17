import type { GalleryLoad } from './useGallery'

/**
 * 0014 R1 · D6, D20, D24 — galerinin ticari unvanı, profilden; çeviri kataloğundan DEĞİL
 * (AC-13). Ad tek dillidir ve çevrilmez. `<bdi>` onu yanındaki metinden yalıtır ve yönü
 * içerikten çözer: Latin ad soldan sağa, Arapça ad sağdan sola (AC-72, AC-73). Zorunlu
 * `dir="ltr"` Arapça harfli bir adı ters dizerdi.
 *
 * Yüklenirken ve hatada YER boş kalır, metin basılmaz (AC-54); yer ayrılmıştır, düzen
 * kaymaz.
 */
export function GalleryName({ load }: { readonly load: GalleryLoad }) {
  return (
    <p className="home-hero__gallery" data-testid="gallery-name-slot">
      {load.kind === 'ready' ? (
        <bdi data-testid="gallery-name">{load.gallery.commercialName}</bdi>
      ) : null}
    </p>
  )
}
