import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { fetchGallery } from '../../shared/api/dealership'
import type { OpeningInterval, PublicBranch, PublicOpenNow } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { BranchList } from './BranchList'
import type { GalleryLoad } from './useGallery'

// `0027` D9 — yenileyici galeri okumasını tekrarlar; yalnız istemci sahtelenir.
vi.mock('../../shared/api/dealership', () => ({ fetchGallery: vi.fn() }))

afterEach(() => {
  cleanup()
  vi.mocked(fetchGallery).mockReset()
})

const GUNLER = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']

function program(
  gunler: Readonly<Record<string, readonly OpeningInterval[]>>,
): Readonly<Record<string, readonly OpeningInterval[]>> {
  return Object.fromEntries(GUNLER.map((gun) => [gun, gunler[gun] ?? []]))
}

const HAFTA_ICI = { start: '09:00', end: '18:00' }

/** AC'lerin "programlı şube"si: Pzt–Cum 09:00–18:00, Cumartesi 10:00–14:00, Pazar kapalı. */
const PROGRAMLI = program({
  monday: [HAFTA_ICI],
  tuesday: [HAFTA_ICI],
  wednesday: [HAFTA_ICI],
  thursday: [HAFTA_ICI],
  friday: [HAFTA_ICI],
  saturday: [{ start: '10:00', end: '14:00' }],
})

/** `0027` — programlı şubenin varsayılan durumu (AC-30): Cuma 12:00, açık, kapanış 18:00. */
const ACIK: PublicOpenNow = {
  open: true,
  today: '2026-12-04',
  next: { date: '2026-12-04', time: '18:00' },
  refreshInSeconds: 21600,
}

function sube(
  openingHours: PublicBranch['openingHours'],
  name = 'Kadıköy Şubesi',
  extra: Partial<PublicBranch> = {},
): PublicBranch {
  return {
    name,
    address: 'Bağdat Caddesi 1',
    phone: '+902165550002',
    openingHours,
    openNow: openingHours === null ? null : ACIK,
    specialHours: [],
    email: null,
    map: null,
    ...extra,
  }
}

function ekran(load: GalleryLoad, locale: AppLocale = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <BranchList load={load} />
    </LocaleProvider>,
  )
}

function hazir(...branches: PublicBranch[]): GalleryLoad {
  return { kind: 'ready', gallery: { displayName: 'Oto Galeri', branches, socialLinks: [] } }
}

describe('0025 · 0028 — altlığın şube öğesi', () => {
  it('0028/AC-58, AC-59: Programlı ve özel günlü şubenin öğesi yalnız ad, adres, telefon; "şu an", haftalık liste, "Özel günler" ve saat iskeleti yok; bloğun sonunda tek "İletişim ve çalışma saatleri" bağlantısı', () => {
    ekran(
      hazir(
        sube(PROGRAMLI, 'Kadıköy Şubesi', {
          specialHours: [{ from: '2026-12-31', through: '2026-12-31', intervals: [] }],
        }),
        sube(PROGRAMLI, 'Bostancı Şubesi'),
      ),
    )

    for (const oge of screen.getAllByTestId('branch')) {
      expect([...oge.children].map((el) => el.tagName)).toEqual(['P', 'P', 'A'])
    }
    for (const yok of ['branch-now', 'branch-hours', 'branch-special', 'branch-hours-skeleton']) {
      expect(screen.queryByTestId(yok)).toBeNull()
    }
    expect(screen.queryByText(/Şu an|Özel günler|Pazartesi/u)).toBeNull()

    const baglanti = screen.getByTestId('footer-contact-link')
    expect(screen.getAllByRole('link', { name: 'İletişim ve çalışma saatleri' })).toEqual([
      baglanti,
    ])
    expect(baglanti.getAttribute('href')).toBe('/tr/contact')
    // Son şubenin ARDINDAN: listeden sonra gelen tek öğe.
    expect(baglanti.previousElementSibling?.tagName).toBe('UL')
    expect(baglanti.nextElementSibling).toBeNull()
  })

  it.each<[AppLocale, string, string]>([
    ['ar', '/ar/contact', 'التواصل وساعات العمل'],
    ['en', '/en/contact', 'Contact and opening hours'],
  ])('0028/AC-59: %s bağlantısı', (locale, href, metin) => {
    ekran(hazir(sube(PROGRAMLI)), locale)

    const baglanti = screen.getByTestId('footer-contact-link')
    expect(baglanti.getAttribute('href')).toBe(href)
    expect(baglanti.textContent).toBe(metin)
  })

  it('0028/AC-61: Ana sayfa galeriyi yenilemez: `refreshInSeconds` = 2 olan yanıttan 5 sn sonra okuma yok', () => {
    vi.useFakeTimers()
    try {
      ekran(hazir(sube(PROGRAMLI, 'Kadıköy Şubesi', { openNow: { ...ACIK, refreshInSeconds: 2 } })))
      vi.advanceTimersByTime(5000)
      expect(fetchGallery).not.toHaveBeenCalled()
    } finally {
      vi.useRealTimers()
    }
  })

  it('0025/AC-46: Programsız şubede saat listesi yok; ad, adres ve telefon bugünkü gibi', () => {
    ekran(hazir(sube(null)))

    const oge = screen.getByTestId('branch')
    expect(within(oge).queryByTestId('branch-hours')).toBeNull()
    expect([...oge.children].map((el) => el.tagName)).toEqual(['P', 'P', 'A'])
  })

  it('0025/AC-50, AC-51, 0028/AC-62: Okuma sürerken bugünkü iskelet, ayrı bir saat göstergesi yok; hata verdiğinde ve aktif şube yokken blok, bağlantı ve hata metni yok', () => {
    const { unmount } = ekran({ kind: 'loading' })
    expect(screen.getByTestId('branch-skeleton')).toBeTruthy()
    expect(screen.queryByTestId('branch-hours')).toBeNull()
    unmount()

    const { container, unmount: kaldir } = ekran({ kind: 'error' })
    expect(container.innerHTML).toBe('')
    kaldir()

    expect(ekran(hazir()).container.innerHTML).toBe('')
  })
})
