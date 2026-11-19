import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { AppShell } from './AppShell'
import { LocaleProvider } from './LocaleProvider'
import { TopBar } from './TopBar'
import type { AppLocale } from '../shared/lib/locale'

/**
 * `0020` R4 · R5 · R5a · C6 — üst çubuktaki galeri adı. Ad sunucunun bastığı
 * `<meta name="gallery-display-name">`'den gelir (plan turu P1); test o etiketi belgeye kendisi
 * koyar. Sunucunun etiketi doğru basması `Host.Tests/GaleriYonetimiZinciriTests` ve
 * `GorunenAdEtiketiTests`'te ölçülür.
 */
/** `0020` AC-18'in adı; `<` kaçışlı yazılıyor ki sert kodlanmış metin kapısı onu JSX metni sanmasın. */
const OZEL_AD = '\u003cb>Galeri & "Oto"\u003c/b>'

function etiket(ad: string | null) {
  document.head.querySelector('meta[name="gallery-display-name"]')?.remove()

  if (ad !== null) {
    const meta = document.createElement('meta')
    meta.name = 'gallery-display-name'
    meta.content = ad
    document.head.appendChild(meta)
  }
}

function ciz(locale: AppLocale, icerik = false) {
  render(
    <LocaleProvider locale={locale}>
      {icerik ? <AppShell>{'içerik'}</AppShell> : <TopBar />}
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  etiket(null)
  vi.restoreAllMocks()
})

describe('0020/AC-9: Vitrinin ana sayfasında, araç listesinde, ilan detayında ve bir içerik sayfasında üst çubuk ziyaretçinin dilindeki görünen adı gösterir.', () => {
  it('her vitrin sayfasının kabuğu üst çubukta görünen adı taşır', () => {
    etiket('Örnek Oto')
    ciz('tr', true)

    expect(screen.getByTestId('top-bar-brand').textContent).toBe('Örnek Oto')
  })
})

describe('0020/AC-11: Üst çubuktaki ada tıklamak ziyaretçiyi `/{locale}/` ana sayfasına götürür ve `{locale}` ziyaretçinin o anki dilidir.', () => {
  it.each<AppLocale>(['tr', 'ar', 'en'])(
    '/%s/ sayfasında ad o dilin ana sayfasına bağlanır',
    (locale) => {
      etiket('Örnek Oto')
      ciz(locale)

      expect(screen.getByRole('link', { name: 'Örnek Oto' }).getAttribute('href')).toBe(
        `/${locale}/`,
      )
    },
  )
})

describe('0020/AC-12: Arapça sayfada üst çubuktaki ad Arapça görünen adı gösterir ve sağdan sola düzende doğru hizalanır.', () => {
  it('Arapça sayfada Arapça ad yalıtılmış (`bdi`) metin olarak basılır; yön içerikten çözülür', () => {
    etiket('أوتو غاليري')
    ciz('ar')

    const bag = screen.getByRole('link', { name: 'أوتو غاليري' })
    expect(bag.querySelector('bdi')?.textContent).toBe('أوتو غاليري')
    // Zorunlu `dir="ltr"` Arapça adı ters dizerdi (0014 AC-72'nin dersi).
    expect(bag.getAttribute('dir')).toBeNull()
  })
})

describe('0020/AC-15: Üst çubuk sayfanın ilk boyamasında görünen adla gelir: ad sonradan yüklenip üst çubuğu ya da içeriği kaydırmaz (ana sayfa ve liste için performans kapısının CLS eşiği yeşil kalır).', () => {
  it('ad ilk boyamada yerindedir: ayrı istek yok, yükleniyor hâli yok', () => {
    const istek = vi.spyOn(globalThis, 'fetch')
    etiket('Örnek Oto')
    ciz('tr')

    // `findBy` değil `getBy`: ad İLK çizimde vardır, sonradan gelmez.
    expect(screen.getByTestId('top-bar-brand').textContent).toBe('Örnek Oto')
    expect(istek).not.toHaveBeenCalled()
  })
})

describe('0020/AC-17: Görünen ad okunamadığında üst çubukta adın yeri boş kalır, yazılımın adı ya da teknik bir metin gösterilmez ve sayfanın geri kalanı çalışır (R5a, D16).', () => {
  it('ad okunamadığında yer boş kalır; yazılımın adı ya da bağlantı basılmaz, sayfa çalışır', () => {
    etiket(null)
    ciz('tr', true)

    const yer = screen.getByTestId('top-bar-brand')
    expect(yer.textContent).toBe('')
    expect(yer.tagName).toBe('SPAN')
    expect(screen.queryByText('Oto Galeri')).toBeNull()
    expect(screen.getByRole('main').textContent).toBe('içerik')
    expect(screen.getByRole('navigation', { name: 'Dil seçimi' })).toBeTruthy()
  })
})

describe('0020/AC-18: `\u003cb>Galeri & "Oto"\u003c/b>` görünen adı üst çubukta ve giriş satırında bu karakterlerle **metin olarak** görünür; işaretleme olarak yorumlanmaz (R5a, D16).', () => {
  it('özel karakterli ad üst çubukta metin olarak görünür', () => {
    etiket(OZEL_AD)
    ciz('tr')

    const yer = screen.getByTestId('top-bar-brand')
    expect(yer.textContent).toBe(OZEL_AD)
    expect(yer.querySelector('b')).toBeNull()
  })
})
