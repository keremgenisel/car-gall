/**
 * `0020` **P1** · **D16** — galerinin ziyaretçinin dilindeki görünen adı. Sunucu onu
 * `index.html`'e `<meta name="gallery-display-name">` olarak basar; üst çubuk adı ilk boyamada
 * senkron okur ve ayrı bir istek yapmaz — ad sonradan gelip üst çubuğu kaydırmaz (C6).
 *
 * Etiket yoksa (okuma düştü, geliştirme sunucusu, birim testi) `null` döner ve adın yeri boş
 * kalır (R5a): yazılımın adına ya da teknik bir metne düşülmez. Değer `getAttribute` ile
 * okunur, yani tarayıcı HTML kodlamasını zaten çözmüştür; çağıran onu METİN olarak basar.
 */
export function galleryDisplayName(doc: Document = document): string | null {
  const content = doc.querySelector('meta[name="gallery-display-name"]')?.getAttribute('content')
  const trimmed = content?.trim() ?? ''

  return trimmed.length === 0 ? null : trimmed
}

/** `0026` D5 — üst çubuk ve altlığın logosu: `logo-small`'un adresi ve GERÇEK boyutu. */
export interface GalleryLogo {
  readonly url: string
  readonly width: number
  readonly height: number
}

/**
 * `0026` **D3** · **D5** — sunucunun `<meta name="gallery-logo">` etiketinden, ilk boyamada ve ayrı
 * istek olmadan. Sunucu etiketi yalnız hazır, logo türünde ve görünen adı okunabilen kurulumda basar;
 * etiket yoksa ya da boyut okunamıyorsa `null` (logo yok, vitrin bu karttan önceki gibi).
 */
export function galleryLogo(doc: Document = document): GalleryLogo | null {
  const meta = doc.querySelector('meta[name="gallery-logo"]')
  const url = meta?.getAttribute('content')?.trim() ?? ''
  const width = Number(meta?.getAttribute('data-width'))
  const height = Number(meta?.getAttribute('data-height'))

  return url.length === 0 || !(width > 0) || !(height > 0) ? null : { url, width, height }
}
