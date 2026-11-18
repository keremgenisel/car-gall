import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { HomeFooter } from '../../app/HomeFooter'
import { LocaleProvider } from '../../app/LocaleProvider'
import { LocaleSwitcher } from '../../app/LocaleSwitcher'
import type {
  ContentNavigationLink,
  ContentPage as ContentPageData,
} from '../../shared/api/content'
import { ApiError } from '../../shared/api/client'
import type { AppLocale } from '../../shared/lib/locale'
import { parseRoute } from '../../shared/lib/route'
import { LeadForm } from '../leads/LeadForm'
import { ContentPage } from './ContentPage'

/**
 * `0017` — ziyaretçi tarafı: içerik sayfası ve üç durumu, SSS, dil değiştirici, altlığın
 * Kurumsal kolonu ve talep formunun KVKK bağlantısı. Yalnız API istemcisi sahtelenir.
 */
vi.mock('../../shared/api/content', () => ({
  fetchContentPage: vi.fn(),
  fetchContentNavigation: vi.fn(),
}))

const { fetchContentPage, fetchContentNavigation } = await import('../../shared/api/content')

const ABOUT_TR: ContentPageData = {
  systemKey: 'about',
  title: 'Hakkımızda',
  body: 'Biz bir galeriyiz.\n\n- güven\n- şeffaflık',
  alternates: [{ locale: 'en', slug: 'about-us' }],
  faqEntries: [],
}

const NAV_TR: ContentNavigationLink[] = [
  { systemKey: 'about', title: 'Hakkımızda', slug: 'hakkimizda' },
  { systemKey: 'faq', title: 'Sıkça Sorulan Sorular', slug: 'sss' },
  { systemKey: 'privacy', title: 'KVKK Aydınlatma Metni', slug: 'kvkk' },
  { systemKey: 'cookies', title: 'Çerez Politikası', slug: 'cerezler' },
]

function sayfa(slug: string, locale: AppLocale = 'tr') {
  return render(
    <LocaleProvider locale={locale}>
      <LocaleSwitcher />
      <ContentPage slug={slug} />
    </LocaleProvider>,
  )
}

function dilBaglantisi(ad: string): string | null {
  return screen.getByRole('link', { name: ad }).getAttribute('href')
}

beforeEach(() => {
  vi.mocked(fetchContentNavigation).mockResolvedValue([])
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  window.history.replaceState(null, '', '/')
})

describe('0017 — rota', () => {
  it('0017/AC-37 (rota yarısı): /tr/foo artık liste değil bir içerik sayfası adayıdır (D9)', () => {
    expect(parseRoute('/tr/foo')).toEqual({ kind: 'content', slug: 'foo' })
  })

  it('0017/AC-38 (rota yarısı): iki parçalı tanınmayan yol /tr/foo/bar listeye düşer', () => {
    expect(parseRoute('/tr/foo/bar')).toEqual({ kind: 'list' })
  })

  it('0017/AC-39 (rota yarısı): /tr/, /tr/cars ve /tr/cars/{slug} eski rotalarını korur', () => {
    expect(parseRoute('/tr/')).toEqual({ kind: 'home' })
    expect(parseRoute('/tr/cars')).toEqual({ kind: 'list' })
    expect(parseRoute('/tr/cars/toyota-corolla-2019-abc')).toEqual({
      kind: 'detail',
      slug: 'toyota-corolla-2019-abc',
    })
  })
})

describe('0017 — içerik sayfası', () => {
  it('0017/AC-34: yayındaki sayfa başlığını h1 olarak ve gövdesini basar', async () => {
    vi.mocked(fetchContentPage).mockResolvedValue(ABOUT_TR)
    sayfa('hakkimizda')

    await screen.findByTestId('content-page')
    expect(screen.getByRole('heading', { level: 1 }).textContent).toBe('Hakkımızda')
    expect(screen.getByTestId('content-body').textContent).toContain('Biz bir galeriyiz.')
    expect(screen.getAllByRole('heading', { level: 1 })).toHaveLength(1)
  })

  it('0017/AC-35, AC-101: uç 404 dönünce bulunamadı ekranı görünür ve yeniden deneme düğmesi yoktur', async () => {
    vi.mocked(fetchContentPage).mockResolvedValue(null)
    sayfa('hic-olmayan')

    expect(await screen.findByTestId('content-not-found')).toBeTruthy()
    expect(screen.queryByRole('button', { name: 'Tekrar dene' })).toBeNull()
    expect(screen.queryByTestId('content-error')).toBeNull()
  })

  it("0017/AC-40 (bileşen yarısı): bulunamadı ekranı 0002 R5'in ekranıyla aynı bileşen ve desende basılır", async () => {
    vi.mocked(fetchContentPage).mockResolvedValue(null)
    sayfa('hic-olmayan')

    const ekran = await screen.findByTestId('content-not-found')
    // `0002`'nin "bulunamadı"sı (`DetailNotFound`) da `EmptyState`'tir: aynı kök sınıf, aynı
    // başlık + açıklama + tek eylem düzeni. Metin içeriğe göredir (araç değil, sayfa).
    expect(ekran.className).toBe('ui-state')
    expect(within(ekran).getAllByRole('link')).toHaveLength(1)
    expect(within(ekran).getByRole('link').getAttribute('href')).toBe('/tr/')
  })

  it('0017/AC-98: istek yanıtlanana kadar iskelet görünür; bulunamadı ve hata görünmez', () => {
    vi.mocked(fetchContentPage).mockReturnValue(new Promise(() => {}))
    sayfa('hakkimizda')

    expect(screen.getByTestId('content-skeleton')).toBeTruthy()
    expect(screen.queryByTestId('content-not-found')).toBeNull()
    expect(screen.queryByTestId('content-error')).toBeNull()
  })

  it('0017/AC-99, AC-100: uç 500 dönünce yeniden denemeli hata ekranı; yeniden deneme ucu yeniden çağırır ve sayfa basılır', async () => {
    vi.mocked(fetchContentPage)
      .mockRejectedValueOnce(new ApiError('api.unavailable', null, 500))
      .mockResolvedValueOnce(ABOUT_TR)
    sayfa('hakkimizda')

    fireEvent.click(await screen.findByRole('button', { name: 'Tekrar dene' }))

    await screen.findByTestId('content-page')
    expect(screen.getByRole('heading', { level: 1 }).textContent).toBe('Hakkımızda')
    expect(vi.mocked(fetchContentPage)).toHaveBeenCalledTimes(2)
    expect(screen.queryByTestId('content-not-found')).toBeNull()
  })

  it('0017/AC-75 (çizici yarısı), AC-81: SSS girdileri sırasıyla ve gövdenin son öğesinden SONRA basılır', async () => {
    vi.mocked(fetchContentPage).mockResolvedValue({
      systemKey: 'faq',
      title: 'Sıkça Sorulan Sorular',
      body: 'Sorularınız.',
      alternates: [],
      faqEntries: [
        { question: 'Takas yapıyor musunuz?', answer: 'Evet.' },
        { question: 'Ekspertiz raporu veriyor musunuz?', answer: 'Evet, **ücretsiz**.' },
      ],
    })
    sayfa('sss')

    const sorular = await screen.findAllByTestId('content-faq-item')
    expect(sorular.map((item) => item.querySelector('h2')?.textContent)).toEqual([
      'Takas yapıyor musunuz?',
      'Ekspertiz raporu veriyor musunuz?',
    ])
    const govdeSon = screen.getByTestId('content-body').lastElementChild!
    expect(
      govdeSon.compareDocumentPosition(sorular[0]!) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy()
    expect(sorular[1]!.querySelector('strong')?.textContent).toBe('ücretsiz')
  })

  it("0017/AC-126 (sayfa yarısı): /en/ içerik sayfasının arayüz metinleri İngilizce'dir", async () => {
    vi.mocked(fetchContentPage).mockResolvedValue(null)
    sayfa('nope', 'en')

    const ekran = await screen.findByTestId('content-not-found')
    expect(ekran.textContent).not.toMatch(/[çğıöşüÇĞİÖŞÜ]|[؀-ۿ]/)
    expect(ekran.textContent).toContain('This page could not be found')
  })
})

describe('0017 — dil değiştirici', () => {
  it("0017/AC-59: /tr/hakkimizda'da English hedefi /en/about-us olur", async () => {
    window.history.replaceState(null, '', '/tr/hakkimizda')
    vi.mocked(fetchContentPage).mockResolvedValue(ABOUT_TR)
    sayfa('hakkimizda')

    await screen.findByTestId('content-page')
    await waitFor(() => expect(dilBaglantisi('English')).toBe('/en/about-us'))
  })

  it('0017/AC-60, AC-61 (değiştirici yarısı): sayfa o dilde yayında değilse hedef o dilin ana sayfasıdır', async () => {
    window.history.replaceState(null, '', '/tr/hakkimizda')
    vi.mocked(fetchContentPage).mockResolvedValue(ABOUT_TR)
    sayfa('hakkimizda')

    await screen.findByTestId('content-page')
    await waitFor(() => expect(dilBaglantisi('العربية')).toBe('/ar/'))
    expect(dilBaglantisi('العربية')).not.toBe('/ar/hakkimizda')
  })

  it('0017/AC-62: içerik sayfası dışındaki ekranlarda değiştirici bugünkü davranışını korur', () => {
    window.history.replaceState(null, '', '/tr/cars')
    render(
      <LocaleProvider locale="tr">
        <LocaleSwitcher />
      </LocaleProvider>,
    )

    expect(dilBaglantisi('English')).toBe('/en/cars')
  })
})

describe('0017 — altlığın Kurumsal kolonu', () => {
  async function altlik(locale: AppLocale = 'tr') {
    render(
      <LocaleProvider locale={locale}>
        <HomeFooter branches={null} />
      </LocaleProvider>,
    )
    await act(async () => {})
  }

  it('0017/AC-86, AC-87 (çizici yarısı): kolon sunucunun sırasıyla dört bağlantıyı başlıklarıyla gösterir', async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue(NAV_TR)
    await altlik()

    const kolon = await screen.findByTestId('footer-corporate')
    expect(within(kolon).getByRole('heading').textContent).toBe('Kurumsal')
    expect(
      within(kolon)
        .getAllByRole('link')
        .map((a) => [a.textContent, a.getAttribute('href')]),
    ).toEqual([
      ['Hakkımızda', '/tr/hakkimizda'],
      ['Sıkça Sorulan Sorular', '/tr/sss'],
      ['KVKK Aydınlatma Metni', '/tr/kvkk'],
      ['Çerez Politikası', '/tr/cerezler'],
    ])
  })

  it('0017/AC-88: yalnız about ve cookies yayındayken kolon iki bağlantı gösterir', async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([NAV_TR[0]!, NAV_TR[3]!])
    await altlik()

    expect(
      within(await screen.findByTestId('footer-corporate'))
        .getAllByRole('link')
        .map((a) => a.textContent),
    ).toEqual(['Hakkımızda', 'Çerez Politikası'])
  })

  it('0017/AC-89: hiçbiri yayında değilken Kurumsal başlığı ve kolonu yoktur; diğer kolonlar durur', async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([])
    await altlik()

    expect(screen.queryByTestId('footer-corporate')).toBeNull()
    expect(screen.queryByText('Kurumsal')).toBeNull()
    expect(screen.getByRole('link', { name: 'Tüm araçlar' }).getAttribute('href')).toBe('/tr/cars')
  })

  it("0017/AC-90 (çizici yarısı): /ar/ altlığı yalnız ar'da yayındaki privacy bağlantısını gösterir", async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([
      { systemKey: 'privacy', title: 'سياسة الخصوصية', slug: 'al-khususiya' },
    ])
    await altlik('ar')

    const kolon = await screen.findByTestId('footer-corporate')
    expect(
      within(kolon)
        .getAllByRole('link')
        .map((a) => [a.textContent, a.getAttribute('href')]),
    ).toEqual([['سياسة الخصوصية', '/ar/al-khususiya']])
    expect(vi.mocked(fetchContentNavigation).mock.calls[0]![0]).toBe('ar')
  })

  it("0017/AC-92 (çizici yarısı): bağlantı sunucunun verdiği GÜNCEL slug'a gider", async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([
      { systemKey: 'about', title: 'Hakkımızda', slug: 'biz-kimiz' },
    ])
    await altlik()

    expect(
      within(await screen.findByTestId('footer-corporate'))
        .getByRole('link')
        .getAttribute('href'),
    ).toBe('/tr/biz-kimiz')
  })
})

describe('0017 — talep formunun KVKK bağlantısı', () => {
  function form(locale: AppLocale = 'tr') {
    render(
      <LocaleProvider locale={locale}>
        <LeadForm listingSlug="toyota-corolla-2019-abc" isReserved={false} />
      </LocaleProvider>,
    )
  }

  it("0017/AC-93: privacy tr'de yayındayken izin satırında /tr/kvkk'ya giden bağlantı vardır", async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue(NAV_TR)
    form()

    expect((await screen.findByTestId('lead-privacy-link')).getAttribute('href')).toBe('/tr/kvkk')
  })

  it("0017/AC-94: privacy ar'da yayında değilken izin satırında hiç bağlantı yoktur; metin ve kutu görünür", async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([
      { systemKey: 'about', title: 'من نحن', slug: 'man-nahnu' },
    ])
    form('ar')
    await act(async () => {})

    expect(screen.queryByTestId('lead-privacy-link')).toBeNull()
    const izin = document.querySelector('.lead-form__consent')!
    expect(izin.querySelectorAll('a')).toHaveLength(0)
    expect(izin.querySelector('input[type="checkbox"]')).toBeTruthy()
  })

  it('0017/AC-96 (çizici yarısı): slug değişince bağlantı doğrudan güncel adrese gider', async () => {
    vi.mocked(fetchContentNavigation).mockResolvedValue([
      { systemKey: 'privacy', title: 'KVKK', slug: 'kvkk-aydinlatma' },
    ])
    form()

    expect((await screen.findByTestId('lead-privacy-link')).getAttribute('href')).toBe(
      '/tr/kvkk-aydinlatma',
    )
  })

  it('0017/AC-97 (çizici yarısı): navigasyon okunamazsa bağlantı basılmaz', async () => {
    vi.mocked(fetchContentNavigation).mockRejectedValue(new ApiError('api.unavailable', null, 500))
    form()
    await act(async () => {})

    expect(screen.queryByTestId('lead-privacy-link')).toBeNull()
  })
})
