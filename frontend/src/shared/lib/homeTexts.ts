/**
 * `0022` **D6** · **R2** — ana sayfa metinleri. Sunucu ziyaretçinin dilinde KAYDEDİLMİŞ her
 * alanı `index.html`'e `<meta name="home-text-…">` olarak basar (`IndexHtmlRenderer`); uç onu
 * ilk boyamada senkron okur. Önce varsayılan metni gösterip sonra değiştirmez ve ayrı bir
 * istek yapmaz (R7) — sayfa kaymaz.
 *
 * Etiket yoksa (galeri bu dilde yazmadı, okuma düştü, geliştirme sunucusu, birim testi)
 * çağıranın verdiği varsayılan metin döner: o dilin çeviri kataloğundaki bugünkü metin (D3).
 * Değer `getAttribute` ile okunur; tarayıcı HTML kodlamasını çözmüştür ve çağıran onu METİN
 * olarak basar (R3, C5). Boş olmayan değerin uçları kırpılmaz (D14).
 */
export type HomeTextField =
  'intro-title' | 'intro-body' | 'form-title' | 'featured-title' | 'empty-title' | 'empty-body'

export function homeText(field: HomeTextField, fallback: string, doc: Document = document): string {
  const content = doc.querySelector(`meta[name="home-text-${field}"]`)?.getAttribute('content')

  return content === null || content === undefined || content.trim().length === 0
    ? fallback
    : content
}
