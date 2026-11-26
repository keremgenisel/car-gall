import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { HomeFooter } from './HomeFooter'
import { LocaleProvider } from './LocaleProvider'
import { TopBar } from './TopBar'
import { galleryLogo } from '../shared/lib/galleryName'
import type { AppLocale } from '../shared/lib/locale'

/**
 * `0026` D5 — üst çubuk ve altlık logosu. Bilgi sunucunun bastığı `<meta name="gallery-logo">`'den
 * gelir; test o etiketi ve görünen adın etiketini belgeye kendisi koyar. Sunucunun etiketi yalnız
 * hazır, logo türünde varlıkta basması `Host.Tests/GaleriLogosuBelgeTests`'te ölçülür.
 */
const LOGO = 'https://cdn.galeri.test/media/public/assets/l/logo-small.webp'

function etiketler(ad: string | null, logo: string | null, width = '320', height = '80') {
  document.head.querySelector('meta[name="gallery-display-name"]')?.remove()
  document.head.querySelector('meta[name="gallery-logo"]')?.remove()

  if (ad !== null) {
    const meta = document.createElement('meta')
    meta.name = 'gallery-display-name'
    meta.content = ad
    document.head.appendChild(meta)
  }

  if (logo !== null) {
    const meta = document.createElement('meta')
    meta.name = 'gallery-logo'
    meta.content = logo
    meta.setAttribute('data-width', width)
    meta.setAttribute('data-height', height)
    document.head.appendChild(meta)
  }
}

function ciz(locale: AppLocale = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <TopBar />
      <HomeFooter branches={<section data-testid="branch-list" />} corporate={[]} />
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  etiketler(null, null)
})

describe('0026 — üst çubuk ve altlık logosu (D5)', () => {
  it('0026/AC-30, AC-32, AC-42: Üst çubukta logo adın önünde, `alt` boş, `width`/`height` türevin boyutu, `loading` ve `fetchpriority` yok; bağlantının adı görünen addır', () => {
    etiketler('Oto Galeri', LOGO)
    ciz()

    const baglanti = screen.getByRole('link', { name: 'Oto Galeri' })
    const logo = screen.getByTestId('top-bar-logo')
    expect(baglanti.firstElementChild).toBe(logo)
    expect(logo.getAttribute('src')).toBe(LOGO)
    expect(logo.getAttribute('alt')).toBe('')
    expect([logo.getAttribute('width'), logo.getAttribute('height')]).toEqual(['320', '80'])
    expect(logo.hasAttribute('loading')).toBe(false)
    expect(logo.hasAttribute('fetchpriority')).toBe(false)
  })

  it('0026/AC-62, AC-63, AC-64: Altlığın ilk öğesi logodur, şube bloğundan önce; aynı adres, `alt` görünen ad, bağlantı içinde değil, `loading="lazy"`, `width`/`height` türevin boyutu', () => {
    etiketler('Oto Galeri', LOGO)
    ciz()

    const altlik = screen.getByTestId('home-footer').querySelector('.home-footer__inner')!
    const logo = screen.getByTestId('footer-logo')
    expect(altlik.firstElementChild).toBe(logo)
    expect(logo.nextElementSibling?.getAttribute('data-testid')).toBe('branch-list')
    expect(logo.getAttribute('src')).toBe(screen.getByTestId('top-bar-logo').getAttribute('src'))
    expect(logo.getAttribute('alt')).toBe('Oto Galeri')
    expect(logo.closest('a')).toBeNull()
    expect(logo.getAttribute('loading')).toBe('lazy')
    expect([logo.getAttribute('width'), logo.getAttribute('height')]).toEqual(['320', '80'])
  })

  it('0026/AC-34, AC-66: Logo bilgisi yokken ya da görünen ad okunamadığında üst çubukta ve altlıkta logo yoktur; üst çubuk bu karttan önceki sınıfla', () => {
    etiketler('Oto Galeri', null)
    ciz()
    expect(screen.queryByTestId('top-bar-logo')).toBeNull()
    expect(screen.queryByTestId('footer-logo')).toBeNull()
    expect(screen.getByTestId('top-bar-brand').className).toBe('top-bar__brand')
    cleanup()

    etiketler(null, LOGO)
    ciz()
    expect(screen.queryByTestId('top-bar-logo')).toBeNull()
    expect(screen.queryByTestId('footer-logo')).toBeNull()
  })

  it('0026/AC-65: ar altlığında logonun `alt`ı Arapça görünen addır', () => {
    etiketler('معرض أوتو', LOGO)
    ciz('ar')
    expect(screen.getByTestId('footer-logo').getAttribute('alt')).toBe('معرض أوتو')
  })

  it('0026 (D3): boyutu okunamayan ya da adresi boş logo bilgisi yok sayılır', () => {
    etiketler('Oto Galeri', LOGO, '0', '80')
    expect(galleryLogo()).toBeNull()
    etiketler('Oto Galeri', ' ', '320', '80')
    expect(galleryLogo()).toBeNull()
    etiketler('Oto Galeri', LOGO)
    expect(galleryLogo()).toEqual({ url: LOGO, width: 320, height: 80 })
  })
})
