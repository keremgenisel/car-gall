/**
 * Daraltma çubuğu, alan ayarları yanıtı geldikten sonra görünür. Yer ayrılmazsa çubuk
 * belirdiği anda listeyi aşağı iter ve düzen kayar — ölçümde CLS 0,24 olarak göründü
 * (AC-25 eşiği 0,1). İskelet, çubuğun kapladığı alanı baştan tutar.
 */
export function FilterBarSkeleton() {
  return <div className="filter-bar filter-bar--skeleton" aria-hidden="true" />
}
