import { useSyncExternalStore } from 'react'
import { useLocale } from '../../app/localeContext'
import { fetchGallery } from '../../shared/api/dealership'
import type {
  OpeningInterval,
  PublicBranch,
  PublicGallery,
  PublicOpenNow,
} from '../../shared/api/types'
import { weekdayName } from '../../shared/i18n/format'
import type { AppLocale } from '../../shared/lib/locale'
import { branchKey, dateRange, dayMonth, daysBetween, weekdayIndex } from './branchDates'
import { OPENING_DAYS } from './openingDays'
import './branchClock.css'

/**
 * `0027` **D15** — şubenin saat bloğu: "şu an" satırı, haftalık liste (`0025` D7) ve özel günler (D11).
 * `0028` **D11** — altlıktan iletişim sayfasına taşındı. Bu modülü yalnız iletişim sayfasının tembel
 * parçası (`contactPage`) STATİK içe aktarır: ayrı bir `branchClock-*` isteği yoktur (AC-45) ve evre B
 * yoktur. Davranış (D9 yenileyicisi dahil) `0027`'deki gibidir (C6).
 */

// ----- D9: tek yenileyici (modül düzeyinde bir depo, bütün şubeler için) -----

const RETRY_SECONDS = 60
const MAX_SECONDS = 90_000

/** D9 — pozitif tam sayı değilse 60 sn; 90000'den büyükse 90000. */
function seconds(value: unknown): number {
  return typeof value === 'number' && Number.isInteger(value) && value > 0
    ? Math.min(value, MAX_SECONDS)
    : RETRY_SECONDS
}

interface Now {
  /** `null`: süre doldu ve tekrar okuma başarısız ya da şube yanıtta yok — metin kaldırılır. */
  readonly openNow: PublicOpenNow | null
  readonly today: string | null
}

class BranchClockStore {
  private readonly listeners = new Set<() => void>()
  private readonly state = new Map<string, Now>()
  private controller: AbortController | null = null
  private deadline: number | null = null
  private timer: ReturnType<typeof setTimeout> | undefined
  private lastRead = Number.NEGATIVE_INFINITY
  private reading = false
  private version = 0

  readonly gallery: PublicGallery
  private readonly locale: AppLocale

  constructor(gallery: PublicGallery, locale: AppLocale, receivedAt: number) {
    this.gallery = gallery
    this.locale = locale
    this.accept(gallery, receivedAt)
    document.addEventListener('visibilitychange', this.onVisible)
  }

  readonly subscribe = (listener: () => void) => {
    this.listeners.add(listener)
    return () => {
      this.listeners.delete(listener)

      // QA 2 BULGU-3 — StrictMode aboneliği kaldırıp HEMEN yeniden kurar (aynı depo, aynı
      // `subscribe`). Anında `dispose` zamanlayıcıyı söküyor ve satır hiç yenilenmiyordu
      // (`frontend-003` BULGU-1'in sınıfı: tekil depo + StrictMode). Söküm bir mikro görev
      // ertelenir ve abone o zaman hâlâ yoksa yapılır.
      queueMicrotask(() => {
        if (this.listeners.size === 0) {
          this.dispose()
        }
      })
    }
  }

  readonly snapshot = () => this.version

  get(key: string): Now {
    return this.state.get(key) ?? { openNow: null, today: null }
  }

  private accept(gallery: PublicGallery, at: number) {
    const keys = new Set<string>()
    let delay: number | null = null

    for (const branch of gallery.branches) {
      keys.add(branchKey(branch))
      this.state.set(branchKey(branch), {
        openNow: branch.openNow,
        today: branch.openNow?.today ?? null,
      })

      if (branch.openNow !== null) {
        delay = Math.min(delay ?? MAX_SECONDS, seconds(branch.openNow.refreshInSeconds))
      }
    }

    // Yeni yanıtta olmayan şubenin metni kaldırılır, "Bugün"ü kalır (D9).
    for (const [key, now] of this.state) {
      if (!keys.has(key)) {
        this.state.set(key, { openNow: null, today: now.today })
      }
    }

    this.plan(delay === null ? null : at + delay * 1000)
  }

  private fail(at: number) {
    for (const [key, now] of this.state) {
      this.state.set(key, { openNow: null, today: now.today })
    }

    this.plan(at + RETRY_SECONDS * 1000)
  }

  private plan(deadline: number | null) {
    clearTimeout(this.timer)
    // D9 — ardışık iki otomatik okuma arasında en az 1 sn.
    this.deadline = deadline === null ? null : Math.max(deadline, this.lastRead + 1000)

    if (this.deadline !== null) {
      this.timer = setTimeout(this.due, Math.max(0, this.deadline - performance.now()))
    }
  }

  /** Gizli sekmede okuma yapılmaz; görünür olunca süre dolmuşsa okunur (AC-60, AC-80). */
  private readonly due = () => {
    if (document.visibilityState !== 'hidden') {
      void this.read()
    }
  }

  private readonly onVisible = () => {
    if (
      document.visibilityState !== 'hidden' &&
      this.deadline !== null &&
      performance.now() >= this.deadline
    ) {
      void this.read()
    }
  }

  private async read() {
    if (this.reading) {
      return
    }

    this.reading = true
    this.deadline = null
    this.lastRead = performance.now()

    try {
      // Okuma sürerken eski durum ekranda kalır (AC-61): depo yanıt gelene kadar değişmez.
      // Okuma başına bir `AbortController`: söküm yalnız süren okumayı iptal eder.
      this.controller = new AbortController()
      const gallery = await fetchGallery(this.locale, this.controller.signal)
      this.accept(gallery, performance.now())
    } catch (cause) {
      if (cause instanceof DOMException && cause.name === 'AbortError') {
        return
      }

      this.fail(performance.now())
    } finally {
      this.reading = false
    }

    this.version += 1
    this.listeners.forEach((listener) => listener())
  }

  dispose() {
    clearTimeout(this.timer)
    this.controller?.abort()
    document.removeEventListener('visibilitychange', this.onVisible)

    if (current === this) {
      current = null
    }
  }
}

let current: BranchClockStore | null = null

function storeFor(gallery: PublicGallery, locale: AppLocale, receivedAt: number | undefined) {
  if (current?.gallery !== gallery) {
    current?.dispose()
    current = new BranchClockStore(gallery, locale, receivedAt ?? performance.now())
  }

  return current
}

function useNow(gallery: PublicGallery, key: string, receivedAt: number | undefined): Now {
  const { locale } = useLocale()
  const store = storeFor(gallery, locale, receivedAt)
  useSyncExternalStore(store.subscribe, store.snapshot)
  return store.get(key)
}

// ----- Bileşenler -----

interface BranchClockProps {
  readonly gallery: PublicGallery
  readonly branch: PublicBranch
  readonly receivedAt: number | undefined
}

/**
 * D10 — "Şu an açık · Kapanış {gün} {saat}". Durum metinle verilir (renk ek işaret); `aria-live`
 * yok. Kutu girişte, iki satır yüksekliğinde sabit (D11); metin kaldırılsa da kutu kalır (AC-79).
 */
export function NowLine({ gallery, branch, receivedAt }: BranchClockProps) {
  const { locale, t } = useLocale()
  const { openNow } = useNow(gallery, branchKey(branch), receivedAt)

  if (openNow === null) {
    return null
  }

  const next = openNow.next
  const gap = next === null ? 0 : daysBetween(openNow.today, next.date)
  const day =
    next === null || gap === 0
      ? ''
      : gap === 1
        ? t('contact.tomorrow')
        : gap < 7
          ? weekdayName(weekdayIndex(next.date), locale)
          : dayMonth(next.date, locale)

  return (
    <>
      <strong className={openNow.open ? 'branch-clock__now--open' : 'branch-clock__now--closed'}>
        {t(openNow.open ? 'contact.openNow' : 'contact.closedNow')}
      </strong>
      {next === null ? null : (
        <>
          {' · '}
          {t(openNow.open ? 'contact.closes' : 'contact.opens')}
          {day === '' ? null : ` ${day}`} <span dir="ltr">{next.time}</span>
        </>
      )}
    </>
  )
}

function Intervals({ intervals }: { readonly intervals: readonly OpeningInterval[] }) {
  const { t } = useLocale()

  return (
    <dd>
      {intervals.length === 0
        ? t('contact.closed')
        : intervals.map((interval) => (
            <span key={interval.start} dir="ltr">
              {interval.start}–{interval.end}
            </span>
          ))}
    </dd>
  )
}

function TodayLabel() {
  const { t } = useLocale()
  return <span className="branch-clock__today">{t('contact.today')}</span>
}

/**
 * `0025` D7'nin yedi günü ve D11'in "Özel günler"i. Listeler ilk okumadaki hâliyle sabittir; D9
 * yenilemesi yalnız "Bugün" vurgusunu taşır (AC-105). Her aralık kendi satırındadır, yani bloğun
 * yüksekliği veriden bellidir ve girişin iskeleti onu önceden ayırır (D15).
 */
export function BranchHours({ gallery, branch, receivedAt }: BranchClockProps) {
  const { locale, t } = useLocale()
  const { today } = useNow(gallery, branchKey(branch), receivedAt)
  const hours = branch.openingHours ?? {}
  const special =
    today === null
      ? undefined
      : branch.specialHours.find((day) => day.from <= today && today <= day.through)
  const weekday = today === null || special !== undefined ? -1 : weekdayIndex(today)

  return (
    <>
      <dl
        className="branch-clock__hours"
        aria-label={t('contact.hours')}
        data-testid="branch-hours"
      >
        {OPENING_DAYS.map((day, index) => (
          <div
            key={day}
            className="branch-clock__hours-row"
            aria-current={index === weekday ? 'date' : undefined}
          >
            <dt>
              <span className="branch-clock__day">{weekdayName(index, locale)}</span>
              {index === weekday ? <TodayLabel /> : null}
            </dt>
            <Intervals intervals={hours[day] ?? []} />
          </div>
        ))}
      </dl>
      {branch.specialHours.length === 0 ? null : (
        <div className="branch-clock__special" data-testid="branch-special">
          <p className="branch-clock__special-title" id={`special-${branchKey(branch)}`}>
            {t('contact.specialDays')}
          </p>
          <dl className="branch-clock__hours" aria-labelledby={`special-${branchKey(branch)}`}>
            {branch.specialHours.map((day) => (
              <div
                key={day.from}
                className="branch-clock__hours-row"
                aria-current={day === special ? 'date' : undefined}
              >
                <dt>
                  <span className="branch-clock__day">
                    {dateRange(day.from, day.through, locale)}
                  </span>
                  {day === special ? <TodayLabel /> : null}
                </dt>
                <Intervals intervals={day.intervals} />
              </div>
            ))}
          </dl>
        </div>
      )}
    </>
  )
}
