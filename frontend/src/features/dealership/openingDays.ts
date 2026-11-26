/**
 * `0025` D6 — gün kodları ve sırası her dilde Pazartesi → Pazar (ISO 8601); sunucunun kodlarıyla
 * aynı. Gün ADLARI burada değil: ekranın dilinde `Intl`'den gelir (`weekdayName`), katalogda yok.
 */
export const OPENING_DAYS = [
  'monday',
  'tuesday',
  'wednesday',
  'thursday',
  'friday',
  'saturday',
  'sunday',
] as const

export type OpeningDay = (typeof OPENING_DAYS)[number]
