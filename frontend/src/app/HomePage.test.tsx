import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../shared/api/client'
import type { PublicGallery } from '../shared/api/types'
import { catalogKeys, createTranslator } from '../shared/i18n/i18n'
import type { AppLocale } from '../shared/lib/locale'
import { App } from './App'

/**
 * 0014 — ana sayfanın tamamı, `App`'in rotasından: galeri adı, giriş bölümü, altlık ve
 * bölümlerin birbirinden bağımsız düşmesi. Catalog bölümlerinin ayrıntısı
 * `HomeCatalog.test.tsx`'te.
 */
vi.mock('../shared/api/catalog', () => ({
  fetchFeaturedListings: vi.fn(),
  fetchShowcaseTotal: vi.fn(),
  fetchBodyTypes: vi.fn(),
  fetchFieldSettings: vi.fn(),
  fetchShowcaseListings: vi.fn(),
  fetchListingDetail: vi.fn(),
}))
vi.mock('../shared/api/dealership', () => ({ fetchGallery: vi.fn() }))

const catalog = await import('../shared/api/catalog')
const { fetchGallery } = await import('../shared/api/dealership')

const GALERI: PublicGallery = {
  commercialName: 'Örnek Otomotiv',
  branches: [
    { name: 'Ataşehir Şubesi', address: 'Ataşehir Bulvarı 5', phone: '+902165550001' },
    { name: 'Kadıköy Şubesi', address: 'Bağdat Caddesi 1', phone: '+902165550002' },
  ],
}

function ekran(locale: AppLocale = 'tr') {
  window.history.replaceState(null, '', `/${locale}/`)
  return render(<App locale={locale} />)
}

function etkinDiller(icerik: string | null) {
  document.head.querySelector('meta[name="enabled-locales"]')?.remove()
  if (icerik !== null) {
    const meta = document.createElement('meta')
    meta.name = 'enabled-locales'
    meta.content = icerik
    document.head.append(meta)
  }
}

beforeEach(() => {
  etkinDiller('tr ar en')
  vi.mocked(catalog.fetchFieldSettings).mockResolvedValue({
    fields: [{ fieldKey: 'body_type', isOpen: true, isPublic: true, isFilterable: true }],
  })
  vi.mocked(catalog.fetchBodyTypes).mockResolvedValue([{ slug: 'suv', label: 'SUV' }])
  vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(12)
  vi.mocked(catalog.fetchFeaturedListings).mockResolvedValue([])
  vi.mocked(fetchGallery).mockResolvedValue(GALERI)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  etkinDiller(null)
})

describe('0014 — adres ve giriş (R10, R1, R2)', () => {
  it.each(['tr', 'ar', 'en'] as const)(
    '0014/AC-1: /%s/ o dilde ana sayfayı gösterir',
    async (locale) => {
      ekran(locale)

      expect(await screen.findByTestId('home-intro')).toBeTruthy()
      expect(screen.getByRole('heading', { level: 1 }).textContent).toBe(
        createTranslator(locale)('home.intro.title'),
      )
      expect(vi.mocked(catalog.fetchShowcaseListings)).not.toHaveBeenCalled()
    },
  )

  it('0014/AC-12: Ana sayfa galeri profilindeki ticari unvanı gösterir: profil "Örnek Otomotiv" iken sayfada bu ad görünür', async () => {
    ekran()

    expect((await screen.findByTestId('gallery-name')).textContent).toBe('Örnek Otomotiv')
  })

  it('0014/AC-13: Profilin ticari unvanı "Başka Ad" yapıldığında ana sayfa bir sonraki yüklemede yeni adı gösterir — ad çeviri kataloğundan gelmez', async () => {
    ekran()
    await screen.findByText('Örnek Otomotiv')
    cleanup()

    vi.mocked(fetchGallery).mockResolvedValue({ ...GALERI, commercialName: 'Başka Ad' })
    ekran()

    expect((await screen.findByTestId('gallery-name')).textContent).toBe('Başka Ad')
    // Katalogdaki marka adı ("Oto Galeri") ana sayfanın adı DEĞİLDİR.
    expect(screen.getByTestId('gallery-name').textContent).not.toBe(
      createTranslator('tr')('app.brandName'),
    )
  })

  it("0014/AC-16: Giriş başlığı ve açıklaması etkin locale'in kataloğundan gelir; tr, ar, en kataloglarının üçünde de iki anahtar dolu", async () => {
    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(catalogKeys(locale)).toEqual(
        expect.arrayContaining(['home.intro.title', 'home.intro.body']),
      )
      expect(createTranslator(locale)('home.intro.title').trim()).not.toBe('')
      expect(createTranslator(locale)('home.intro.body').trim()).not.toBe('')
    }

    ekran('en')
    expect((await screen.findByTestId('home-intro')).textContent).toContain(
      createTranslator('en')('home.intro.body'),
    )
  })

  it('0014/AC-71: Giriş bölümü bir <img> öğesi veya arka plan görseli taşımaz', async () => {
    ekran()

    const giris = await screen.findByTestId('home-intro')
    await screen.findByTestId('gallery-name')
    expect(giris.querySelector('img, picture, svg, [style*="background"]')).toBeNull()
  })

  it('0014/AC-73: Galeri adı Arapça harfli iken ad <bdi> içinde basılır ve zorunlu dir="ltr" taşımaz', async () => {
    vi.mocked(fetchGallery).mockResolvedValue({ ...GALERI, commercialName: 'معرض المثال' })

    ekran('ar')

    const ad = await screen.findByTestId('gallery-name')
    expect(ad.tagName).toBe('BDI')
    expect(ad.getAttribute('dir')).toBeNull()
    expect(ad.closest('[dir="ltr"]')).toBeNull()
  })
})

describe('0014 — altlık (R7)', () => {
  it('0014/AC-42: İki aktif şube varken altlık her ikisinin adını, adresini ve telefonunu gösterir', async () => {
    ekran()

    const subeler = await screen.findAllByTestId('branch')
    expect(subeler).toHaveLength(2)
    expect(subeler[0]?.textContent).toContain('Ataşehir Şubesi')
    expect(subeler[0]?.textContent).toContain('Ataşehir Bulvarı 5')
    expect(subeler[0]?.textContent).toContain('+902165550001')
    expect(subeler[1]?.textContent).toContain('Kadıköy Şubesi')
  })

  it('0014/AC-45: Şube telefonu E.164 biçimindeki numaraya giden bir tel: bağlantısıdır', async () => {
    ekran()

    const sube = (await screen.findAllByTestId('branch'))[0]!
    expect(within(sube).getByRole('link').getAttribute('href')).toBe('tel:+902165550001')
  })

  it("0014/AC-46: Altlığın bağlantıları yalnız /{locale}/cars, etkin locale'lerin ana sayfaları ve şube telefonlarıdır", async () => {
    ekran()

    const altlik = await screen.findByTestId('home-footer')
    await within(altlik).findAllByTestId('branch')
    expect(
      within(altlik)
        .getAllByRole('link')
        .map((link) => link.getAttribute('href')),
    ).toEqual(['tel:+902165550001', 'tel:+902165550002', '/tr/cars', '/tr/', '/ar/', '/en/'])
  })

  it('0014/AC-67: Kurulumda yalnız tr ve en etkinken altlıkta /ar/ bağlantısı yoktur', async () => {
    etkinDiller('tr en')

    ekran()

    const altlik = await screen.findByTestId('home-footer')
    const hedefler = within(altlik)
      .getAllByRole('link')
      .map((link) => link.getAttribute('href'))
    expect(hedefler).toContain('/en/')
    expect(hedefler).not.toContain('/ar/')
  })

  it('0014/AC-47: Etkin dilin bağlantısı aria-current ile işaretlidir', async () => {
    ekran('ar')

    const altlik = await screen.findByTestId('home-footer')
    const isaretli = within(altlik)
      .getAllByRole('link')
      .filter((link) => link.getAttribute('aria-current') !== null)
    expect(isaretli.map((link) => link.getAttribute('href'))).toEqual(['/ar/'])
  })

  it('0014/AC-65: Hiç aktif şube yokken altlığın şube bloğu görünmez; yerinde boş durum veya hata durumu yoktur ve altlığın bağlantıları görünmeye devam eder', async () => {
    vi.mocked(fetchGallery).mockResolvedValue({ ...GALERI, branches: [] })

    ekran()

    await screen.findByTestId('gallery-name')
    const altlik = screen.getByTestId('home-footer')
    expect(within(altlik).queryByTestId('branch-list')).toBeNull()
    expect(within(altlik).queryByTestId('branch-skeleton')).toBeNull()
    expect(within(altlik).queryByRole('alert')).toBeNull()
    expect(within(altlik).getByRole('link', { name: 'Tüm araçlar' }).getAttribute('href')).toBe(
      '/tr/cars',
    )
  })
})

describe('0014 — durumlar (R8)', () => {
  it('0014/AC-49: Veri gelene kadar altlığın şube bloğu kendi iskeletini gösterir', () => {
    vi.mocked(fetchGallery).mockReturnValue(new Promise(() => {}))
    vi.mocked(catalog.fetchShowcaseTotal).mockReturnValue(new Promise(() => {}))
    vi.mocked(catalog.fetchFeaturedListings).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('branch-skeleton')).toBeTruthy()
    expect(screen.getByTestId('featured-skeleton')).toBeTruthy()
    expect(screen.getByTestId('narrowing-form-skeleton')).toBeTruthy()
  })

  it('0014/AC-51: Toplam sayı 0 iken giriş bölümü ve altlık görünmeye devam eder', async () => {
    vi.mocked(catalog.fetchShowcaseTotal).mockResolvedValue(0)

    ekran()

    expect(await screen.findByTestId('empty-showcase')).toBeTruthy()
    expect(screen.getByTestId('home-intro')).toBeTruthy()
    expect(screen.getByTestId('home-footer')).toBeTruthy()
    expect(await screen.findAllByTestId('branch')).toHaveLength(2)
  })

  it('0014/AC-54: Galeri okuması başarısızken sayfanın geri kalanı görünür; adın ve şube bloğunun yeri boş kalır ve hata metni basılmaz', async () => {
    vi.mocked(fetchGallery).mockRejectedValue(new ApiError('api.unavailable', 'trace-gal-1', 500))

    ekran()

    expect(await screen.findByRole('button', { name: '12 aracı gör' })).toBeTruthy()
    await waitFor(() => expect(screen.queryByTestId('branch-skeleton')).toBeNull())
    expect(screen.getByTestId('gallery-name-slot').textContent).toBe('')
    expect(screen.queryByTestId('branch-list')).toBeNull()
    const altlik = screen.getByTestId('home-footer')
    expect(altlik.textContent).not.toMatch(/hata|yüklenemedi|500|trace-gal-1/i)
    expect(screen.getByTestId('home-intro').textContent).not.toMatch(
      /hata|yüklenemedi|500|trace-gal-1/i,
    )
  })

  it('0014/AC-55: Formun tekrar dene eylemi galeri okumasını yeniden istemez', async () => {
    vi.mocked(catalog.fetchShowcaseTotal).mockRejectedValueOnce(
      new ApiError('api.unavailable', null, 500),
    )

    ekran()

    const hata = await screen.findByTestId('narrowing-form-error')
    await screen.findByTestId('gallery-name')
    fireEvent.click(within(hata).getByRole('button', { name: 'Tekrar dene' }))

    expect(await screen.findByRole('button', { name: '12 aracı gör' })).toBeTruthy()
    expect(vi.mocked(fetchGallery)).toHaveBeenCalledTimes(1)
    expect(vi.mocked(catalog.fetchFeaturedListings)).toHaveBeenCalledTimes(1)
  })
})
