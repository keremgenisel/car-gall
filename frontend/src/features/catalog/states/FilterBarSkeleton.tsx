/**
 * Filtre paneli, değer listeleri geldikten sonra görünür. Yer ayrılmazsa panel belirdiği
 * anda listeyi aşağı iter ve düzen kayar — `0001`'de CLS 0,24 olarak ölçülmüştü.
 *
 * Ayrılan yükseklik YALNIZ burada değil: `.list-filters` kuralı aynı token'ı okuyor, yani
 * gerçek panel de aynı kutuyu tutuyor (`0015` D20). Bu bileşenin işi o kutuyu veri
 * gelmeden de göstermek; yüksekliği kendisi SAYMIYOR.
 */
export function FilterBarSkeleton() {
  return (
    <div
      className="list-filters list-filters--skeleton"
      aria-hidden="true"
      data-testid="filter-skeleton"
    />
  )
}
