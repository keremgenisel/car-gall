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
