import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import type { OpeningInterval, PublicBranch } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { BranchList } from './BranchList'
import type { GalleryLoad } from './useGallery'

afterEach(cleanup)

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

function sube(openingHours: PublicBranch['openingHours'], name = 'Kadıköy Şubesi'): PublicBranch {
  return { name, address: 'Bağdat Caddesi 1', phone: '+902165550002', openingHours }
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

function satirlar(oge: HTMLElement) {
  return within(within(oge).getByTestId('branch-hours'))
    .getAllByRole('term')
    .map((dt) => [dt.textContent, dt.nextElementSibling?.textContent])
}

describe('0025 — altlıkta şubenin çalışma saatleri', () => {
  it('0025/AC-44: Programlı şubede adres ve telefondan sonra yedi satır; Pzt–Cum 09:00–18:00, Cumartesi 10:00–14:00, Pazar Kapalı', () => {
    ekran(hazir(sube(PROGRAMLI)))

    const oge = screen.getByTestId('branch')
    const cocuklar = [...oge.children].map((el) => el.tagName)
    expect(cocuklar).toEqual(['P', 'P', 'A', 'DL'])
    expect(satirlar(oge)).toEqual([
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

  it('0025/AC-45: İki aralıklı gün iki aralığı bu sırayla gösterir', () => {
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

    const pazartesi = within(screen.getByTestId('branch-hours')).getAllByRole('definition')[0]!
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

  it('0025/AC-47: ar altlığında gün adları ve "Kapalı" Arapça; saatler Latin rakam ve aralık soldan sağa', () => {
    ekran(hazir(sube(PROGRAMLI)), 'ar')

    const satir = satirlar(screen.getByTestId('branch'))
    expect(satir[0]![0]).toMatch(/\p{Script=Arabic}/u)
    expect(satir[6]![1]).toBe('مغلق')
    expect(satir[0]![1]).toBe('09:00–18:00')
    const aralik = screen.getByTestId('branch-hours').querySelector('span')!
    expect(aralik.getAttribute('dir')).toBe('ltr')
  })

  it('0025/AC-48: en altlığında gün adları İngilizce ve 24 saat biçimi', () => {
    ekran(hazir(sube(PROGRAMLI)), 'en')

    expect(satirlar(screen.getByTestId('branch'))[0]).toEqual(['Monday', '09:00–18:00'])
    expect(satirlar(screen.getByTestId('branch'))[6]).toEqual(['Sunday', 'Closed'])
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
