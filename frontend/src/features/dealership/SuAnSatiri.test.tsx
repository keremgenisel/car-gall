import { act, cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { fetchGallery } from '../../shared/api/dealership'
import type {
  OpeningInterval,
  PublicBranch,
  PublicGallery,
  PublicOpenNow,
  PublicSpecialDay,
} from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { BranchList } from './BranchList'
import { daysBetween, weekdayIndex } from './branchDates'

/**
 * `0027` — altlığın "şu an" satırı, "Bugün" vurgusu, özel günler ve D9'un yenileyicisi. Sunucunun
 * hesabı (`openNow`) Host'ta ölçülür (`OzelGunZinciriTests`); burada ölçülen, altlığın o yanıta
 * verdiği cevap. Zaman sahte: `setTimeout` ve `performance.now()` birlikte, `Date` ayrı (AC-100).
 */
vi.mock('../../shared/api/dealership', () => ({ fetchGallery: vi.fn() }))

const GUNLER = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']
const HAFTA_ICI = { start: '09:00', end: '18:00' }
const PROGRAMLI: Readonly<Record<string, readonly OpeningInterval[]>> = Object.fromEntries(
  GUNLER.map((gun) => [
    gun,
    gun === 'saturday' ? [{ start: '10:00', end: '14:00' }] : gun === 'sunday' ? [] : [HAFTA_ICI],
  ]),
)

function durum(
  open: boolean,
  today: string,
  next: string | null,
  refreshInSeconds: unknown = 21600,
): PublicOpenNow {
  return {
    open,
    today,
    next: next === null ? null : { date: next.slice(0, 10), time: next.slice(11) },
    refreshInSeconds: refreshInSeconds as number,
  }
}

const ACIK = durum(true, '2026-12-04', '2026-12-04 18:00')
const KAPALI = durum(false, '2026-12-04', '2026-12-05 10:00')

function sube(openNow: PublicOpenNow | null, specialHours: PublicSpecialDay[] = []): PublicBranch {
  return {
    name: 'Kadıköy Şubesi',
    address: 'Bağdat Caddesi 1',
    phone: '+902165550002',
    openingHours: openNow === null ? null : PROGRAMLI,
    openNow,
    specialHours,
  }
}

function galeri(...branches: PublicBranch[]): PublicGallery {
  return { displayName: 'Oto Galeri', branches, socialLinks: [] }
}

async function ekran(gallery: PublicGallery, locale: AppLocale = 'tr') {
  const sonuc = render(
    <LocaleProvider locale={locale}>
      <BranchList load={{ kind: 'ready', gallery, receivedAt: performance.now() }} />
    </LocaleProvider>,
  )
  // Tembel parça mikro görevlerle çözülür; sahte zamanlayıcı onu beklemez.
  await act(async () => {
    await import('./branchClock')
  })
  return sonuc
}

const satir = () => screen.getByTestId('branch-now').textContent

async function ilerle(ms: number) {
  await act(async () => {
    await vi.advanceTimersByTimeAsync(ms)
  })
}

let gorunurluk: DocumentVisibilityState = 'visible'

function sekme(durumu: DocumentVisibilityState) {
  gorunurluk = durumu
  act(() => {
    document.dispatchEvent(new Event('visibilitychange'))
  })
}

beforeEach(() => {
  gorunurluk = 'visible'
  Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => gorunurluk })
  vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'Date', 'performance'] })
})

afterEach(() => {
  cleanup()
  vi.useRealTimers()
  vi.mocked(fetchGallery).mockReset()
})

// ----- Metinler (D10) -----

describe('0027/AC-48, AC-49, AC-50, AC-51: "Şu an" satırının metni üç dilde; {gün} kuralı; Latin rakam, Miladi takvim', () => {
  it.each<[string, AppLocale, PublicOpenNow, string]>([
    ['AC-48 açık', 'tr', ACIK, 'Şu an açık · Kapanış 18:00'],
    ['AC-48 kapalı', 'tr', KAPALI, 'Şu an kapalı · Açılış yarın 10:00'],
    [
      'AC-49 iki gün',
      'tr',
      durum(false, '2026-12-05', '2026-12-07 09:00'),
      'Şu an kapalı · Açılış Pazartesi 09:00',
    ],
    [
      'AC-49 altı gün',
      'tr',
      durum(false, '2026-12-01', '2026-12-07 09:00'),
      'Şu an kapalı · Açılış Pazartesi 09:00',
    ],
    [
      'AC-49 yedi gün',
      'tr',
      durum(false, '2027-01-14', '2027-01-21 09:00'),
      'Şu an kapalı · Açılış 21 Ocak 09:00',
    ],
    [
      'AC-49 ar tarih',
      'ar',
      durum(false, '2027-01-14', '2027-01-21 09:00'),
      'مغلق الآن · يفتح 21 يناير 09:00',
    ],
    ['AC-50', 'tr', durum(true, '2026-12-04', null), 'Şu an açık'],
    ['AC-51 en açık', 'en', ACIK, 'Open now · Closes 18:00'],
    ['AC-51 en kapalı', 'en', KAPALI, 'Closed now · Opens tomorrow 10:00'],
    ['AC-51 ar açık', 'ar', ACIK, 'مفتوح الآن · يغلق 18:00'],
    ['AC-51 ar kapalı', 'ar', KAPALI, 'مغلق الآن · يفتح غدًا 10:00'],
    [
      'D11 en uzun',
      'en',
      durum(false, '2027-09-10', '2027-09-21 09:00'),
      'Closed now · Opens 21 September 09:00',
    ],
  ])('%s', async (_, locale, openNow, beklenen) => {
    await ekran(galeri(sube(openNow)), locale)

    expect(satir()).toBe(beklenen)
    // Saat soldan sağa okunur (D10); Arapçada satır sağdan sola dizilir, saat `dir="ltr"`dir.
    if (openNow.next !== null) {
      expect(screen.getByTestId('branch-now').querySelector('span[dir="ltr"]')!.textContent).toBe(
        openNow.next.time,
      )
    }
    expect(satir()).not.toMatch(/[٠-٩]/u)
  })
})

describe('0027/AC-52: Açık ve kapalı satırın metni farklı; satır `aria-live` taşımaz', () => {
  it('renksiz okunur', async () => {
    const { unmount } = await ekran(galeri(sube(ACIK)))
    const acik = satir()
    expect(screen.getByTestId('branch-now').closest('[aria-live]')).toBeNull()
    unmount()

    await ekran(galeri(sube(KAPALI)))
    expect(satir()).not.toBe(acik)
    expect(screen.getByTestId('branch-now').closest('[aria-live]')).toBeNull()
  })
})

// ----- "Bugün" ve özel günler (D11) -----

function bugunler() {
  return [...document.querySelectorAll('[aria-current="date"]')].map(
    (el) => el.querySelector('.home-footer__day')?.textContent,
  )
}

describe('0027/AC-53, AC-54: "Bugün" yalnız bir satırda: özel gün değilse `today`\'in günü, özel günün içindeyse o özel gün', () => {
  it('AC-53 haftalık', async () => {
    await ekran(galeri(sube(ACIK)))

    expect(bugunler()).toEqual(['Cuma'])
    const bugun = document.querySelector('[aria-current="date"]')!
    expect(within(bugun as HTMLElement).getByText('Bugün').className).toBe('home-footer__today')
    expect(screen.getAllByText('Bugün')).toHaveLength(1)
  })

  it('AC-54 özel gün', async () => {
    await ekran(
      galeri(
        sube(durum(false, '2027-03-11', '2027-03-13 10:00'), [
          { from: '2027-03-10', through: '2027-03-12', intervals: [] },
        ]),
      ),
    )

    expect(bugunler()).toEqual(['10–12 Mart'])
    expect(within(screen.getByTestId('branch-hours')).queryByText('Bugün')).toBeNull()
  })
})

describe('0027/AC-55, AC-56, AC-57: "Özel günler" listesi `from` sırasıyla; kayıt yoksa liste ve başlık yok; programsız şubede satır ve liste yok', () => {
  it('AC-55', async () => {
    await ekran(
      galeri(
        sube(durum(false, '2027-02-25', '2027-02-26 09:00'), [
          { from: '2027-03-01', through: '2027-03-01', intervals: [] },
          { from: '2027-03-10', through: '2027-03-12', intervals: [] },
          {
            from: '2027-03-15',
            through: '2027-03-15',
            intervals: [{ start: '10:00', end: '16:00' }],
          },
        ]),
      ),
    )

    const liste = within(screen.getByTestId('branch-special'))
    expect(liste.getByText('Özel günler')).toBeTruthy()
    expect(
      liste
        .getAllByRole('term')
        .map((dt) => [
          dt.querySelector('.home-footer__day')!.textContent,
          dt.nextElementSibling!.textContent,
        ]),
    ).toEqual([
      ['1 Mart', 'Kapalı'],
      ['10–12 Mart', 'Kapalı'],
      ['15 Mart', '10:00–16:00'],
    ])
  })

  it('AC-55 ar: aynı aydaki aralığın sayıları soldan sağa yalıtılır ("4–2" görünmez)', async () => {
    await ekran(
      galeri(
        sube(durum(false, '2027-03-01', '2027-03-02 09:00'), [
          { from: '2027-03-10', through: '2027-03-12', intervals: [] },
          { from: '2027-02-27', through: '2027-03-02', intervals: [] },
        ]),
      ),
      'ar',
    )

    const gunler = [
      ...screen.getByTestId('branch-special').querySelectorAll('.home-footer__day'),
    ].map((el) => el.textContent)
    expect(gunler).toEqual(['\u206610–12\u2069 مارس', '27 فبراير–2 مارس'])
  })

  it('AC-56', async () => {
    await ekran(galeri(sube(ACIK)))
    expect(screen.queryByTestId('branch-special')).toBeNull()
    expect(screen.queryByText('Özel günler')).toBeNull()
  })

  it('AC-57', async () => {
    await ekran(galeri(sube(null)))
    const oge = screen.getByTestId('branch')
    expect(screen.queryByTestId('branch-now')).toBeNull()
    expect(screen.queryByTestId('branch-special')).toBeNull()
    expect([...oge.children].map((el) => el.tagName)).toEqual(['P', 'P', 'A'])
  })
})

// ----- Evre B (D15) -----

describe('0027/AC-68 (birim yarısı): Parça yüklenmeden ad, adres, telefon ve "şu an" kutusu basılır; saat yerinde veriden yükseklikli iskelet', () => {
  it('evre B', async () => {
    // Taze modüller: aynı `lazy` örneği önceki testlerde çözüldüyse ilk boyama zaten parçalı olurdu.
    vi.resetModules()
    const { BranchList: TazeListe } = await import('./BranchList')
    const { LocaleProvider: TazeDil } = await import('../../app/LocaleProvider')
    const iki = [
      { start: '09:00', end: '13:00' },
      { start: '16:00', end: '21:00' },
    ]
    const ozel = Array.from({ length: 12 }, (_, i) => ({
      from: `2027-01-${String(2 * i + 1).padStart(2, '0')}`,
      through: `2027-01-${String(2 * i + 1).padStart(2, '0')}`,
      intervals: iki,
    }))
    const branch = {
      ...sube(ACIK, ozel),
      openingHours: Object.fromEntries(GUNLER.map((gun) => [gun, iki])),
    }
    render(
      <TazeDil locale="tr">
        <TazeListe load={{ kind: 'ready', gallery: galeri(branch) }} />
      </TazeDil>,
    )

    const oge = screen.getByTestId('branch')
    expect(within(oge).getByText('Kadıköy Şubesi')).toBeTruthy()
    expect(within(oge).getByText('Bağdat Caddesi 1')).toBeTruthy()
    expect(within(oge).getByText('+902165550002')).toBeTruthy()
    expect(screen.getByTestId('branch-now').textContent).toBe('')
    expect(screen.queryByTestId('branch-skeleton')).toBeNull()
    const iskelet = screen.getByTestId('branch-hours-skeleton')
    expect(iskelet.style.getPropertyValue('--hours-lines')).toBe('14')
    expect(iskelet.style.getPropertyValue('--special-on')).toBe('1')
    expect(iskelet.style.getPropertyValue('--special-lines')).toBe('24')
  })
})

// ----- D9: yenileyici -----

function yanit(openNow: PublicOpenNow | null, specialHours: PublicSpecialDay[] = []) {
  return galeri(sube(openNow, specialHours))
}

describe('0027/AC-59: `refreshInSeconds` = 2 → en geç 3 sn içinde tam bir okuma ve yeni satır', () => {
  it('iki saniye', async () => {
    vi.mocked(fetchGallery).mockResolvedValue(yanit(KAPALI))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 2)))

    await ilerle(1999)
    expect(fetchGallery).not.toHaveBeenCalled()
    await ilerle(1001)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
    expect(satir()).toBe('Şu an kapalı · Açılış yarın 10:00')
  })
})

describe('0027/AC-60: Süresi dolmuş yanıtla gizli kalan sekme görünür olunca bir okuma; dolmamışsa okuma yok', () => {
  it('doldu', async () => {
    vi.mocked(fetchGallery).mockResolvedValue(yanit(KAPALI))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 60)))

    sekme('hidden')
    await ilerle(120_000)
    expect(fetchGallery).not.toHaveBeenCalled()

    sekme('visible')
    await ilerle(0)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
    expect(satir()).toBe('Şu an kapalı · Açılış yarın 10:00')
  })

  it('dolmadı', async () => {
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 60)))

    sekme('hidden')
    await ilerle(30_000)
    sekme('visible')
    await ilerle(0)
    expect(fetchGallery).not.toHaveBeenCalled()
  })
})

describe('0027/AC-61: Tekrar okuma 5 sn sonuçlanmazken satır ilk yanıtınkidir, iskelet yok', () => {
  it('bekleyen okuma', async () => {
    vi.mocked(fetchGallery).mockReturnValue(new Promise(() => {}))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 2)))

    await ilerle(2000)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
    await ilerle(5000)
    expect(satir()).toBe('Şu an açık · Kapanış 18:00')
    expect(screen.queryByTestId('branch-hours-skeleton')).toBeNull()
    expect(screen.queryByTestId('branch-skeleton')).toBeNull()
  })
})

describe("0027/AC-79, AC-80: Başarısız tekrar okumada metin kalkar, kutu ve listeler kalır, hata metni yok; görünürken 60 sn'de bir yeniden; gizliyken deneme yok", () => {
  it('503 ve yeniden deneme', async () => {
    vi.mocked(fetchGallery)
      .mockRejectedValueOnce(new ApiError('service.unavailable', null, 503, null))
      .mockResolvedValueOnce(yanit(KAPALI))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 2)))

    await ilerle(2000)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
    const kutu = screen.getByTestId('branch-now')
    expect(kutu.textContent).toBe('')
    expect(kutu.className).toBe('home-footer__now')
    expect(screen.getByTestId('branch-hours')).toBeTruthy()
    expect(bugunler()).toEqual(['Cuma'])
    expect(screen.queryByRole('alert')).toBeNull()
    expect(screen.getByTestId('branch').textContent).not.toMatch(/hata|error/iu)

    await ilerle(59_999)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
    await ilerle(1)
    expect(fetchGallery).toHaveBeenCalledTimes(2)
    expect(satir()).toBe('Şu an kapalı · Açılış yarın 10:00')
  })

  it('gizliyken deneme yok', async () => {
    vi.mocked(fetchGallery).mockRejectedValue(new ApiError('service.unavailable', null, 503, null))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 2)))

    await ilerle(2000)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
    sekme('hidden')
    await ilerle(600_000)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
  })
})

describe('0027/AC-81, AC-82: Geçersiz süre 60 sn; 200000 → 90000 sn', () => {
  it.each<[string, unknown]>([
    ['0', 0],
    ['-5', -5],
    ['null', null],
    ['"abc"', 'abc'],
  ])('AC-81 %s', async (_, deger) => {
    vi.mocked(fetchGallery).mockResolvedValue(yanit(ACIK))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', deger)))

    await ilerle(59_000)
    expect(fetchGallery).not.toHaveBeenCalled()
    await ilerle(1000)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
  })

  it('AC-82', async () => {
    vi.mocked(fetchGallery).mockResolvedValue(yanit(ACIK))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 200_000)))

    await ilerle(89_999_000)
    expect(fetchGallery).not.toHaveBeenCalled()
    await ilerle(1000)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
  })
})

describe('0027/AC-100: Süre monoton saatle ölçülür; cihaz saati (`Date`) ileri ya da geri alınsa da okuma 60. saniyede', () => {
  it.each<[string, number]>([
    ['1 saat ileri', 3_600_000],
    ['1 saat geri', -3_600_000],
  ])('%s', async (_, kayma) => {
    vi.mocked(fetchGallery).mockResolvedValue(yanit(ACIK))
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 60)))

    await ilerle(10_000)
    vi.setSystemTime(Date.now() + kayma)
    await ilerle(49_999)
    expect(fetchGallery).not.toHaveBeenCalled()
    await ilerle(1)
    expect(fetchGallery).toHaveBeenCalledTimes(1)
  })
})

describe('0027/AC-105: Yenileme özel gün listesini değiştirmez (iki satır kalır); satır yeni durumu basar', () => {
  it('liste sabit', async () => {
    const iki = [
      { from: '2026-12-10', through: '2026-12-10', intervals: [] },
      { from: '2026-12-20', through: '2026-12-20', intervals: [] },
    ]
    vi.mocked(fetchGallery).mockResolvedValue(
      yanit(KAPALI, [...iki, { from: '2026-12-25', through: '2026-12-25', intervals: [] }]),
    )
    await ekran(yanit(durum(true, '2026-12-04', '2026-12-04 18:00', 2), iki))

    await ilerle(2000)
    expect(satir()).toBe('Şu an kapalı · Açılış yarın 10:00')
    expect(within(screen.getByTestId('branch-special')).getAllByRole('term')).toHaveLength(2)
  })
})

// ----- P8 -----

describe('0027/AC-102: Tarih dizgesinin haftanın günü ve gün farkı süreç saat diliminden bağımsız', () => {
  const asil = process.env['TZ']
  afterEach(() => {
    process.env['TZ'] = asil
  })

  it.each(['America/Los_Angeles', 'Pacific/Kiritimati', 'UTC'])('%s', (bolge) => {
    process.env['TZ'] = bolge
    expect(weekdayIndex('2026-12-04')).toBe(4)
    expect(daysBetween('2026-12-04', '2026-12-14')).toBe(10)
  })
})
