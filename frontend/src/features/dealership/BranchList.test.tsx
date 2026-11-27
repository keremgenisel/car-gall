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

/**
 * `0027` D11 (PM şartı (a)) — gün adı kendi öğesinden okunur: "Bugün" etiketi `dt`'de ayrı öğedir
 * ve gerçek takvime göre bir satıra girer; `dt` metni okunsaydı sonuç takvime bağlanırdı.
 */
async function satirlar(oge: HTMLElement) {
  return within(await within(oge).findByTestId('branch-hours'))
    .getAllByRole('term')
    .map((dt) => [
      dt.querySelector('.home-footer__day')?.textContent,
      dt.nextElementSibling?.textContent,
    ])
}

describe('0025 — altlıkta şubenin çalışma saatleri', () => {
  it('0025/AC-44, 0027/AC-48: Programlı şubede ad, "şu an" satırı, adres, telefon, sonra yedi satır; Pzt–Cum 09:00–18:00, Cumartesi 10:00–14:00, Pazar Kapalı', async () => {
    ekran(hazir(sube(PROGRAMLI)))

    const oge = screen.getByTestId('branch')
    const tablo = await satirlar(oge)
    const cocuklar = [...oge.children].map((el) => el.tagName)
    // `0027` D11 (PM kararı, AP-08): ['P', 'P', 'A', 'DL'] → ['P', 'P', 'P', 'A', 'DL'].
    expect(cocuklar).toEqual(['P', 'P', 'P', 'A', 'DL'])
    expect(oge.children[1]!.getAttribute('data-testid')).toBe('branch-now')
    expect(oge.children[1]!.textContent).toBe('Şu an açık · Kapanış 18:00')
    expect(tablo).toEqual([
      ['Pazartesi', '09:00–18:00'],
      ['Salı', '09:00–18:00'],
      ['Çarşamba', '09:00–18:00'],
      ['Perşembe', '09:00–18:00'],
      ['Cuma', '09:00–18:00'],
      ['Cumartesi', '10:00–14:00'],
      ['Pazar', 'Kapalı'],
    ])
    expect(within(oge).getByTestId('branch-hours').getAttribute('aria-label')).toBe(
      'Çalışma saatleri',
    )
  })

  it('0025/AC-45: İki aralıklı gün iki aralığı bu sırayla gösterir', async () => {
    ekran(
      hazir(
        sube(
          program({
            monday: [
              { start: '09:00', end: '13:00' },
              { start: '16:00', end: '21:00' },
            ],
          }),
        ),
      ),
    )

    const pazartesi = within(await screen.findByTestId('branch-hours')).getAllByRole(
      'definition',
    )[0]!
    expect([...pazartesi.querySelectorAll('span')].map((span) => span.textContent)).toEqual([
      '09:00–13:00',
      '16:00–21:00',
    ])
  })

  it('0025/AC-46: Programsız şubede saat listesi yok; ad, adres ve telefon bugünkü gibi', () => {
    ekran(hazir(sube(null)))

    const oge = screen.getByTestId('branch')
    expect(within(oge).queryByTestId('branch-hours')).toBeNull()
    expect([...oge.children].map((el) => el.tagName)).toEqual(['P', 'P', 'A'])
  })

  it('0025/AC-47: ar altlığında gün adları ve "Kapalı" Arapça; saatler Latin rakam ve aralık soldan sağa', async () => {
    ekran(hazir(sube(PROGRAMLI)), 'ar')

    const satir = await satirlar(screen.getByTestId('branch'))
    expect(satir[0]![0]).toMatch(/\p{Script=Arabic}/u)
    expect(satir[6]![1]).toBe('مغلق')
    expect(satir[0]![1]).toBe('09:00–18:00')
    // `0027` D11 — `dt`'nin ilk `span`'i artık gün adı; aralık `dd`'dedir.
    const aralik = screen.getByTestId('branch-hours').querySelector('dd span')!
    expect(aralik.getAttribute('dir')).toBe('ltr')
  })

  it('0025/AC-48: en altlığında gün adları İngilizce ve 24 saat biçimi', async () => {
    ekran(hazir(sube(PROGRAMLI)), 'en')

    const tablo = await satirlar(screen.getByTestId('branch'))
    expect(tablo[0]).toEqual(['Monday', '09:00–18:00'])
    expect(tablo[6]).toEqual(['Sunday', 'Closed'])
  })

  it('0025/AC-50, AC-51: Okuma sürerken bugünkü iskelet, ayrı bir saat göstergesi yok; hata verdiğinde blok ve hata metni yok', () => {
    const { unmount } = ekran({ kind: 'loading' })
    expect(screen.getByTestId('branch-skeleton')).toBeTruthy()
    expect(screen.queryByTestId('branch-hours')).toBeNull()
    unmount()

    const { container } = ekran({ kind: 'error' })
    expect(container.innerHTML).toBe('')
  })
})
