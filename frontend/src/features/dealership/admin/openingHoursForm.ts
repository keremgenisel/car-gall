import type { OpeningInterval } from '../../../shared/api/types'
import { OPENING_DAYS, type OpeningDay } from '../openingDays'

/**
 * `0025` D5 — şube formunun "Çalışma saatleri" bölümünün SAF mantığı: sunucu biçiminden form
 * değerine, eylemler (aralık ekle/kaldır, önceki günle aynı) ve form değerinden gövdeye. Yan
 * etkisizdir ve kendi birim testi vardır (docs/frontend.md § 8.1).
 */
export type Schedule = Readonly<Record<OpeningDay, readonly OpeningInterval[]>>

/** D3 — günde en çok üç aralık; "Aralık ekle" üçüncüden sonra görünmez (AC-5). */
export const MAX_INTERVALS = 3

/** Sunucunun programı (yoksa `null`) → yedi günlük form değeri; programsız şube yedi gün kapalı (AC-2). */
export function scheduleOf(
  hours: Readonly<Record<string, readonly OpeningInterval[]>> | null,
): Schedule {
  return Object.fromEntries(OPENING_DAYS.map((day) => [day, hours?.[day] ?? []])) as Schedule
}

export function addInterval(schedule: Schedule, day: OpeningDay): Schedule {
  return schedule[day].length >= MAX_INTERVALS
    ? schedule
    : { ...schedule, [day]: [...schedule[day], { start: '', end: '' }] }
}

/** Son aralık kaldırılınca gün "Kapalı" olur (AC-6). */
export function removeInterval(schedule: Schedule, day: OpeningDay, index: number): Schedule {
  return { ...schedule, [day]: schedule[day].filter((_, i) => i !== index) }
}

export function changeInterval(
  schedule: Schedule,
  day: OpeningDay,
  index: number,
  change: Partial<OpeningInterval>,
): Schedule {
  return {
    ...schedule,
    [day]: schedule[day].map((interval, i) =>
      i === index ? { ...interval, ...change } : interval,
    ),
  }
}

/** D5 · AC-7 — önceki günün aralıkları bu güne kopyalanır; yalnız formda, kaydetme ayrıca gerekir. */
export function copyPrevious(schedule: Schedule, day: OpeningDay): Schedule {
  const index = OPENING_DAYS.indexOf(day)
  return index <= 0 ? schedule : { ...schedule, [day]: schedule[OPENING_DAYS[index - 1]!] }
}

/**
 * `0025` D11 · P8 — gövde HER ZAMAN yedi günün hepsini taşır. Hiç aralık yoksa yedi boş gün:
 * kayıtlı programı siler, programsız şubede programsız bırakır (AC-24, AC-72).
 */
export function schedulePayload(schedule: Schedule): Record<OpeningDay, OpeningInterval[]> {
  return Object.fromEntries(OPENING_DAYS.map((day) => [day, [...schedule[day]]])) as Record<
    OpeningDay,
    OpeningInterval[]
  >
}
