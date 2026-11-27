import type { PublicBranch } from '../../shared/api/types'
import { intlLocale, type AppLocale } from '../../shared/lib/locale'

/** `0027` — altlığın tarih yardımcıları; tembel saat parçası kullanır (`branchClock.tsx`). */

// ----- Tarih (P8 · AC-102): süreç saat diliminden bağımsız, yalnız UTC gün sayısı -----

function utcDay(date: string): number {
  const [year, month, day] = date.split('-').map(Number)
  return Date.UTC(year!, month! - 1, day!) / 86_400_000
}

/** `"YYYY-MM-DD"`'nin haftadaki sırası, 0 = Pazartesi (`0025` D6). */
export function weekdayIndex(date: string): number {
  return (new Date(utcDay(date) * 86_400_000).getUTCDay() + 6) % 7
}

export function daysBetween(from: string, to: string): number {
  return Math.round(utcDay(to) - utcDay(from))
}

/** "21 Ocak" — Miladi takvim, Latin rakam (D10, tuzak 8); gün önce, üç dilde aynı kalıp. */
export function dayMonth(date: string, locale: AppLocale): string {
  const month = new Intl.DateTimeFormat(intlLocale(locale), {
    month: 'long',
    timeZone: 'UTC',
    calendar: 'gregory',
  }).format(new Date(utcDay(date) * 86_400_000))
  return `${Number(date.slice(8, 10))} ${month}`
}

/** D11 — tek gün "1 Ocak", aynı ayda aralık "10–12 Mart", ay aşan aralık "28 Şubat–2 Mart". */
export function dateRange(from: string, through: string, locale: AppLocale): string {
  if (from === through) {
    return dayMonth(from, locale)
  }

  return from.slice(0, 7) === through.slice(0, 7)
    ? `${Number(from.slice(8, 10))}–${dayMonth(through, locale)}`
    : `${dayMonth(from, locale)}–${dayMonth(through, locale)}`
}

export function branchKey(branch: PublicBranch): string {
  return `${branch.name}|${branch.phone}`
}
