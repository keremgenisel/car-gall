import { act, cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import type { PublicSocialLink } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { SOCIAL_ICON_PATHS, socialIconLinks } from './socialIconLinks'
import { SocialLinksSlot } from './SocialLinksSlot'
import type { GalleryLoad } from './useGallery'

const UC: readonly PublicSocialLink[] = [
  { platform: 'instagram', url: 'https://www.instagram.com/otogaleri' },
  { platform: 'youtube', url: 'https://www.youtube.com/@otogaleri' },
  { platform: 'linkedin', url: 'https://www.linkedin.com/company/otogaleri' },
]

const ALTI: readonly PublicSocialLink[] = [
  { platform: 'instagram', url: 'https://www.instagram.com/a' },
  { platform: 'facebook', url: 'https://www.facebook.com/b' },
  { platform: 'youtube', url: 'https://www.youtube.com/@c' },
  { platform: 'tiktok', url: 'https://www.tiktok.com/@d' },
  { platform: 'x', url: 'https://x.com/e' },
  { platform: 'linkedin', url: 'https://www.linkedin.com/company/f' },
]

function hazir(socialLinks: readonly PublicSocialLink[]): GalleryLoad {
  return { kind: 'ready', gallery: { displayName: 'Oto Galeri', branches: [], socialLinks } }
}

afterEach(cleanup)

const basarili = () => Promise.resolve(socialIconLinks)

function ekran(
  load: GalleryLoad,
  loadBlock: () => Promise<typeof socialIconLinks> = basarili,
  locale: AppLocale = 'tr',
) {
  return render(
    <LocaleProvider locale={locale}>
      <SocialLinksSlot load={load} loadBlock={loadBlock} />
    </LocaleProvider>,
  )
}

async function simgelerGeldi() {
  await act(async () => {
    await Promise.resolve()
  })
}

describe('0024 — altlığın "Bizi takip edin" bloğu', () => {
  it('0024/AC-37, AC-39: Instagram, YouTube ve LinkedIn hesaplı galeride blok üç bağlantı taşır; sıra D1, simge satır içi SVG, hedef kayıtlı adres, rel="me", target yok', async () => {
    ekran(hazir(UC))
    await simgelerGeldi()

    const blok = screen.getByTestId('footer-social')
    expect(within(blok).getByRole('heading', { level: 2 }).textContent).toBe('Bizi takip edin')
    const baglantilar = within(blok).getAllByRole('link')
    expect(baglantilar.map((link) => link.getAttribute('href'))).toEqual(
      UC.map((hesap) => hesap.url),
    )
    for (const link of baglantilar) {
      expect(link.getAttribute('rel')).toBe('me')
      expect(link.hasAttribute('target')).toBe(false)
    }
    expect(baglantilar[0]!.querySelector('svg path')!.getAttribute('d')).toBe(
      SOCIAL_ICON_PATHS.instagram,
    )
    expect(baglantilar[1]!.querySelector('svg path')!.getAttribute('d')).toBe(
      SOCIAL_ICON_PATHS.youtube,
    )
  })

  it('0024/AC-40, AC-41, AC-42: Hesap yokken, okuma sürerken ve okuma hata verdiğinde blok ve başlığı yoktur; hata metni de yoktur', () => {
    for (const load of [hazir([]), { kind: 'loading' }, { kind: 'error' }] as const) {
      const yukleyici = vi.fn(basarili)
      const { container, unmount } = ekran(load, yukleyici)

      expect(container.innerHTML).toBe('')
      // AC-68 — hesapsız sayfada parça istenmez.
      expect(yukleyici).not.toHaveBeenCalled()
      unmount()
    }
  })

  it('0024/AC-43: ar ve en başlıkları kendi dilindedir; belgedeki sıra her dilde D1', async () => {
    for (const [locale, baslik] of [
      ['ar', 'تابعونا'],
      ['en', 'Follow us'],
    ] as const) {
      const { unmount } = ekran(hazir(UC), basarili, locale)
      await simgelerGeldi()

      const blok = screen.getByTestId('footer-social')
      expect(within(blok).getByRole('heading').textContent).toBe(baslik)
      expect(
        within(blok)
          .getAllByRole('link')
          .map((link) => link.getAttribute('href')),
      ).toEqual(UC.map((hesap) => hesap.url))
      unmount()
    }
  })

  it('0024/AC-60: Her bağlantının erişilebilir adı platformun adıdır ve tr, ar, en’de aynıdır', async () => {
    for (const locale of ['tr', 'ar', 'en'] as const) {
      const { unmount } = ekran(hazir(ALTI), basarili, locale)
      await simgelerGeldi()

      expect(
        within(screen.getByTestId('footer-social'))
          .getAllByRole('link')
          .map((link) => link.getAttribute('aria-label') ?? link.textContent),
      ).toEqual(['Instagram', 'Facebook', 'YouTube', 'TikTok', 'X', 'LinkedIn'])
      unmount()
    }
  })

  it('0024/AC-61, AC-63: Parça geldikten sonra LinkedIn dışındaki bağlantıların görünür metni yoktur; SVG aria-hidden, odaklanamaz ve sabit renk taşımaz', async () => {
    ekran(hazir(ALTI))
    await simgelerGeldi()

    const baglantilar = within(screen.getByTestId('footer-social')).getAllByRole('link')
    for (const link of baglantilar.filter(
      (item) => !item.getAttribute('href')!.includes('linkedin'),
    )) {
      expect(link.textContent).toBe('')
      const svg = link.querySelector('svg')!
      expect(svg.getAttribute('aria-hidden')).toBe('true')
      expect(svg.getAttribute('focusable')).toBe('false')
      expect(svg.getAttribute('fill')).toBe('currentColor')
      for (const oge of [svg, ...svg.querySelectorAll('*')]) {
        for (const ad of ['fill', 'stroke', 'color', 'style', 'stop-color']) {
          const deger = oge.getAttribute(ad)
          expect(deger === null || deger === 'currentColor').toBe(true)
        }
      }
    }
  })

  it('0024/AC-81: LinkedIn simge taşımaz; görünür metni ve erişilebilir adı "LinkedIn"', async () => {
    ekran(hazir(UC))
    await simgelerGeldi()

    const linkedin = screen.getByRole('link', { name: 'LinkedIn' })
    expect(linkedin.querySelector('svg')).toBeNull()
    expect(linkedin.textContent).toBe('LinkedIn')
    expect(linkedin.hasAttribute('aria-label')).toBe(false)
  })

  it('0024/AC-69, AC-79: Parçanın dinamik içe aktarması reddedilince her bağlantı adını görünür metin olarak gösterir, hedef değişmez ve yakalanmamış hata düşmez', async () => {
    const yakalanmamis = vi.fn()
    window.addEventListener('unhandledrejection', yakalanmamis)
    ekran(hazir(UC), () =>
      Promise.reject(new TypeError('Failed to fetch dynamically imported module')),
    )
    await simgelerGeldi()
    await simgelerGeldi()

    const baglantilar = within(screen.getByTestId('footer-social')).getAllByRole('link')
    expect(baglantilar.map((link) => link.textContent)).toEqual([
      'Instagram',
      'YouTube',
      'LinkedIn',
    ])
    expect(baglantilar.map((link) => link.getAttribute('href'))).toEqual(
      UC.map((hesap) => hesap.url),
    )
    expect(baglantilar.every((link) => link.querySelector('svg') === null)).toBe(true)
    expect(yakalanmamis).not.toHaveBeenCalled()
    window.removeEventListener('unhandledrejection', yakalanmamis)
  })

  it('0024/AC-70: Parça gelene kadar blok yoktur (yer ayrılmaz, altlık ilk ekranın altında); gelince bağlantılar son hâlleriyle bir kez basılır', async () => {
    let coz: (render: typeof socialIconLinks) => void = () => {}
    const { container } = ekran(
      hazir(UC),
      () =>
        new Promise((resolve) => {
          coz = resolve
        }),
    )

    expect(container.innerHTML).toBe('')

    await act(async () => {
      coz(socialIconLinks)
      await Promise.resolve()
    })

    const instagram = screen.getByRole('link', { name: 'Instagram' })
    expect(instagram.className).toBe('social-link')
    expect(instagram.querySelector('svg')).not.toBeNull()
  })

  it('0024/AC-78: Yükleyici aynı sayfada tek söz döner (tek istek)', async () => {
    const { loadSocialLinks } = await import('./socialLinksLoader')
    expect(loadSocialLinks()).toBe(loadSocialLinks())
  })
})
