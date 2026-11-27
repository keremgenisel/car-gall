import type { OpeningInterval } from '../../../shared/api/types'
import { MAX_INTERVALS } from './openingHoursForm'

/** `0027` D13 — formdaki bir özel gün. Aralıksız kayıt "Tam gün kapalı"dır (ayrı bayrak yok). */
export interface SpecialEntry {
  readonly from: string
  readonly through: string
  readonly intervals: readonly OpeningInterval[]
}

/** `0027` D3 — kayıt sınırı; "Özel gün ekle" bu sayıda kayıttan sonra görünmez (AC-6). */
export const MAX_SPECIAL_DAYS = 12

/**
 * `0027` AC-9 — kayıtlar `from` sırasıyla görünür; boş başlangıç sona. Sıralama kararlıdır, yani
 * gönderilen dizi de bu sıradadır ve sunucunun `specialHours.<i>` indeksi ekrandaki sıradır.
 */
export function ordered(entries: readonly SpecialEntry[]): readonly SpecialEntry[] {
  return entries
    .map((entry, index) => ({ entry, index }))
    .sort(
      (a, b) =>
        (a.entry.from === '' ? 1 : 0) - (b.entry.from === '' ? 1 : 0) ||
        (a.entry.from < b.entry.from ? -1 : a.entry.from > b.entry.from ? 1 : 0) ||
        a.index - b.index,
    )
    .map((pair) => pair.entry)
}

export function entriesOf(
  days:
    readonly { from: string; through: string; intervals: readonly OpeningInterval[] }[] | undefined,
): readonly SpecialEntry[] {
  return ordered((days ?? []).map((day) => ({ ...day, intervals: [...day.intervals] })))
}

export function addEntry(entries: readonly SpecialEntry[]): readonly SpecialEntry[] {
  return entries.length >= MAX_SPECIAL_DAYS
    ? entries
    : [...entries, { from: '', through: '', intervals: [] }]
}

export function removeEntry(
  entries: readonly SpecialEntry[],
  index: number,
): readonly SpecialEntry[] {
  return entries.filter((_, i) => i !== index)
}

export function changeEntry(
  entries: readonly SpecialEntry[],
  index: number,
  change: Partial<SpecialEntry>,
): readonly SpecialEntry[] {
  return entries.map((entry, i) => (i === index ? { ...entry, ...change } : entry))
}

/** "Tam gün kapalı" seçimi aralıkları boşaltır; kaldırılması tek boş aralık açar (D13). */
export function setClosedAllDay(entry: SpecialEntry, closed: boolean): Partial<SpecialEntry> {
  return {
    intervals: closed
      ? []
      : entry.intervals.length > 0
        ? entry.intervals
        : [{ start: '', end: '' }],
  }
}

export function addEntryInterval(entry: SpecialEntry): Partial<SpecialEntry> {
  return entry.intervals.length >= MAX_INTERVALS
    ? {}
    : { intervals: [...entry.intervals, { start: '', end: '' }] }
}

/**
 * Gövde: boş "Bitiş" `null` gider, yani başlangıçla aynı gün (D2, AC-3). Boş METİN sunucuda
 * `special_hours.invalid_date`'tir; panel onu hiç göndermez.
 */
export function specialPayload(
  entries: readonly SpecialEntry[],
): { from: string; through: string | null; intervals: OpeningInterval[] }[] {
  return ordered(entries).map((entry) => ({
    from: entry.from,
    through: entry.through === '' ? null : entry.through,
    intervals: [...entry.intervals],
  }))
}
