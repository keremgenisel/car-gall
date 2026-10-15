/**
 * Daraltma çubuğu, alan ayarları yanıtı geldikten sonra görünür. Yer ayrılmazsa çubuk
 * belirdiği anda listeyi aşağı iter ve düzen kayar — ölçümde CLS 0,24 olarak göründü
 * (AC-25 eşiği 0,1).
 *
 * Ayrılan yükseklik artık YALNIZ burada değil: `.filter-bar` kuralı aynı token'ı
 * okuyor, yani gerçek çubuk da aynı kutuyu tutuyor. Tek taraflı ayırma kusurun kendisiydi
 * — 2026-10-15'te ölçüldü: iskelet 132px tutuyordu, gerçek çubuk 238px geliyordu ve
 * ızgara +111px kayıyordu (CLS 0,08). Bu bileşenin işi o kutuyu veri gelmeden de
 * göstermek; yüksekliği kendisi SAYMIYOR.
 */
export function FilterBarSkeleton() {
  return <div className="filter-bar filter-bar--skeleton" aria-hidden="true" />
}
