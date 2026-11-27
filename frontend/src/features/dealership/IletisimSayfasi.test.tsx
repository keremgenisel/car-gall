import { StrictMode } from 'react'
import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ContactRoute } from '../../app/ContactRoute'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { fetchGallery } from '../../shared/api/dealership'
import type {
  OpeningInterval,
  PublicBranch,
  PublicGallery,
  PublicOpenNow,
} from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import { ContactBranches } from './contactPage'

/**
 * `0028` — iletişim sayfası: şube bölümleri (D3), durumlar (D4), e-posta (D8), harita kutusu (D6, D15,
 * D16) ve `0025`'in altlıktan taşınan haftalık listesi (AC-44). Sunucunun adresleri (`linkUrl`,
 * `embedUrl`) Host'ta ölçülür (`IletisimZinciriTests`); burada ölçülen, sayfanın o yanıta verdiği cevap.
 */
vi.mock('../../shared/api/dealership', () => ({ fetchGallery: vi.fn() }))

afterEach(() => {
  cleanup()
  vi.useRealTimers()
  vi.mocked(fetchGallery).mockReset()
})

const GUNLER = ['monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday']
const HAFTA_ICI = { start: '09:00', end: '18:00' }

function program(gunler: Readonly<Record<string, readonly OpeningInterval[]>>) {
  return Object.fromEntries(GUNLER.map((gun) => [gun, gunler[gun] ?? []]))
}

/** "Programlı şube": Pzt–Cum 09:00–18:00, Cumartesi 10:00–14:00, Pazar kapalı. */
const PROGRAMLI = program({
  monday: [HAFTA_ICI],
  tuesday: [HAFTA_ICI],
  wednesday: [HAFTA_ICI],
  thursday: [HAFTA_ICI],
  friday: [HAFTA_ICI],
  saturday: [{ start: '10:00', end: '14:00' }],
})

function durum(open: boolean, refreshInSeconds = 21600): PublicOpenNow {
  return open
    ? { open, today: '2026-12-04', next: { date: '2026-12-04', time: '18:00' }, refreshInSeconds }
    : { open, today: '2026-12-04', next: { date: '2026-12-05', time: '10:00' }, refreshInSeconds }
}

const KADIKOY_EMBED =
  'https://www.google.com/maps/embed/v1/place?key=test-anahtar&q=40.98765%2C29.0275&language=tr'
const KADIKOY_LINK = 'https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275'

/** Spec'in Kadıköy'ü: programlı, koordinatlı (gömme adresi var), e-postalı, bir özel günlü. */
function kadikoy(extra: Partial<PublicBranch> = {}): PublicBranch {
  return {
    name: 'Kadıköy Şubesi',
    address: 'Bağdat Caddesi 1',
    phone: '+902165550001',
    openingHours: PROGRAMLI,
    openNow: durum(true),
    specialHours: [{ from: '2026-12-31', through: '2026-12-31', intervals: [] }],
    email: 'kadikoy@otogaleri.com.tr',
    map: { linkUrl: KADIKOY_LINK, embedUrl: KADIKOY_EMBED },
    ...extra,
  }
}

/** Bostancı: programsız, koordinatsız, e-postasız; anahtarlı kurulumda gömme adresi var. */
function bostanci(extra: Partial<PublicBranch> = {}): PublicBranch {
  return {
    name: 'Bostancı Şubesi',
    address: 'Bağdat Cad. No:5',
    phone: '+902165550002',
    openingHours: null,
    openNow: null,
    specialHours: [],
    email: null,
    map: {
      linkUrl: 'https://www.google.com/maps/search/?api=1&query=Ba%C4%9Fdat%20Cad.%20No%3A5',
      embedUrl:
        'https://www.google.com/maps/embed/v1/place?key=test-anahtar&q=Ba%C4%9Fdat%20Cad.%20No%3A5&language=tr',
    },
    ...extra,
  }
}

function galeri(...branches: PublicBranch[]): PublicGallery {
  return { displayName: 'Oto Galeri', branches, socialLinks: [] }
}

function ekran(gallery: PublicGallery, locale: AppLocale = 'tr', onRetry = () => {}) {
  return render(
    <LocaleProvider locale={locale}>
      <ContactBranches
        load={{ kind: 'ready', gallery, receivedAt: performance.now() }}
        onRetry={onRetry}
      />
    </LocaleProvider>,
  )
}

const bolumler = () => screen.getAllByTestId('contact-branch')

/** `0027` D11 — gün adı kendi öğesinden okunur ("Bugün" etiketi `dt`'de ayrı öğedir). */
function satirlar(oge: HTMLElement) {
  return within(within(oge).getByTestId('branch-hours'))
    .getAllByRole('term')
    .map((dt) => [
      dt.querySelector('.branch-clock__day')?.textContent,
      dt.nextElementSibling?.textContent,
    ])
}

// ----- `0025` AC-44–AC-48: haftalık liste iletişim sayfasında (AC-44) -----

describe('0028/AC-44, 0025/AC-44–AC-48: haftalık liste iletişim sayfasının şube bölümünde; beklentiler altlıktakiyle aynı', () => {
  it('0025/AC-44: yedi satır ve sırası; Pzt–Cum 09:00–18:00, Cumartesi 10:00–14:00, Pazar Kapalı', () => {
    ekran(galeri(kadikoy()))

    expect(satirlar(bolumler()[0]!)).toEqual([
      ['Pazartesi', '09:00–18:00'],
      ['Salı', '09:00–18:00'],
      ['Çarşamba', '09:00–18:00'],
      ['Perşembe', '09:00–18:00'],
      ['Cuma', '09:00–18:00'],
      ['Cumartesi', '10:00–14:00'],
      ['Pazar', 'Kapalı'],
    ])
    expect(screen.getByTestId('branch-hours').getAttribute('aria-label')).toBe('Çalışma saatleri')
  })

  it('0025/AC-45: iki aralıklı gün iki aralığı bu sırayla gösterir', () => {
    ekran(
      galeri(
        kadikoy({
          openingHours: program({
            monday: [
              { start: '09:00', end: '13:00' },
              { start: '16:00', end: '21:00' },
            ],
          }),
        }),
      ),
    )

    const pazartesi = within(screen.getByTestId('branch-hours')).getAllByRole('definition')[0]!
    expect([...pazartesi.querySelectorAll('span')].map((span) => span.textContent)).toEqual([
      '09:00–13:00',
      '16:00–21:00',
    ])
  })

  it('0025/AC-47: ar gün adları ve "Kapalı" Arapça; saatler Latin rakam, aralık soldan sağa', () => {
    ekran(galeri(kadikoy()), 'ar')

    const satir = satirlar(bolumler()[0]!)
    expect(satir[0]![0]).toMatch(/\p{Script=Arabic}/u)
    expect(satir[6]![1]).toBe('مغلق')
    expect(satir[0]![1]).toBe('09:00–18:00')
    expect(screen.getByTestId('branch-hours').querySelector('dd span')!.getAttribute('dir')).toBe(
      'ltr',
    )
  })

  it('0025/AC-48: en gün adları İngilizce ve 24 saat biçimi', () => {
    ekran(galeri(kadikoy()), 'en')

    const tablo = satirlar(bolumler()[0]!)
    expect(tablo[0]).toEqual(['Monday', '09:00–18:00'])
    expect(tablo[6]).toEqual(['Sunday', 'Closed'])
  })
})

// ----- Bölümler (D3, D4, D8) -----

describe('0028 — şube bölümleri', () => {
  it("0028/AC-32: iki şubede iki bölüm; her bölümün `<h2>`'si şubenin adı, sıra yanıtın sırası", () => {
    ekran(galeri(bostanci(), kadikoy()))

    expect(
      bolumler().map((bolum) => within(bolum).getByRole('heading', { level: 2 }).textContent),
    ).toEqual(['Bostancı Şubesi', 'Kadıköy Şubesi'])
  })

  it('0028/AC-33: Kadıköy bölümünde sıra: ad · "şu an" · adres · telefon · e-posta · harita kutusu · "Google Haritalar\'da aç" · haftalık liste · "Özel günler"', () => {
    ekran(galeri(kadikoy()))
    const bolum = bolumler()[0]!
    const sira = [
      within(bolum).getByRole('heading', { level: 2 }),
      within(bolum).getByTestId('branch-now'),
      within(bolum).getByText('Bağdat Caddesi 1'),
      within(bolum).getByTestId('branch-phone'),
      within(bolum).getByTestId('branch-email'),
      within(bolum).getByTestId('map-box'),
      within(bolum).getByTestId('map-link'),
      within(bolum).getByTestId('branch-hours'),
      within(bolum).getByTestId('branch-special'),
    ]

    for (const [onceki, sonraki] of sira
      .slice(0, -1)
      .map((oge, i) => [oge, sira[i + 1]!] as const)) {
      expect(
        onceki.compareDocumentPosition(sonraki) & Node.DOCUMENT_POSITION_FOLLOWING,
      ).toBeTruthy()
    }
    expect(within(bolum).getByTestId('branch-now').textContent).toBe('Şu an açık · Kapanış 18:00')
  })

  it('0028/AC-34, AC-35, AC-43 (birim yarısı): telefon `tel:{E.164}`, e-posta `mailto:` ve metni adres; ikisi de `dir="ltr"`, ar\'da da', () => {
    ekran(galeri(kadikoy()), 'ar')

    const telefon = screen.getByTestId('branch-phone')
    const eposta = screen.getByTestId('branch-email')
    expect(telefon.getAttribute('href')).toBe('tel:+902165550001')
    expect(telefon.getAttribute('dir')).toBe('ltr')
    expect(eposta.getAttribute('href')).toBe('mailto:kadikoy@otogaleri.com.tr')
    expect(eposta.textContent).toBe('kadikoy@otogaleri.com.tr')
    expect(eposta.getAttribute('dir')).toBe('ltr')
  })

  it('0028/AC-36, AC-37: e-postasız Bostancı\'da e-posta satırı yok; programsız şubede "şu an", haftalık liste ve "Özel günler" yok, ad/adres/telefon/"Google Haritalar\'da aç" var', () => {
    ekran(galeri(bostanci()))
    const bolum = bolumler()[0]!

    expect(within(bolum).queryByTestId('branch-email')).toBeNull()
    expect(bolum.querySelector('a[href^="mailto:"]')).toBeNull()
    for (const yok of ['branch-now', 'branch-hours', 'branch-special']) {
      expect(within(bolum).queryByTestId(yok)).toBeNull()
    }
    expect(within(bolum).getByRole('heading', { level: 2 }).textContent).toBe('Bostancı Şubesi')
    expect(within(bolum).getByText('Bağdat Cad. No:5')).toBeTruthy()
    expect(within(bolum).getByTestId('branch-phone')).toBeTruthy()
    expect(within(bolum).getByTestId('map-link').textContent).toBe("Google Haritalar'da aç")
  })

  it('0028/AC-40: aktif şube yokken çevrilmiş boş durum; bölüm ve harita yok', () => {
    ekran(galeri())

    expect(
      within(screen.getByTestId('contact-empty')).getByText('İletişim bilgisi henüz eklenmedi'),
    ).toBeTruthy()
    expect(screen.queryByTestId('contact-branch')).toBeNull()
    expect(screen.queryByTestId('map-box')).toBeNull()
  })
})

// ----- Rota kabuğu (D4, D14) -----

describe('0028 — rota kabuğu: `<h1>`, iskelet, hata ve tekrar', () => {
  it('0028/AC-38, AC-1 (uç yarısı): okuma sürerken tek `<h1>` "İletişim" ve `role="status"` iskelet; ar ve en başlığı', async () => {
    vi.mocked(fetchGallery).mockReturnValue(new Promise(() => {}))

    for (const [locale, baslik] of [
      ['tr', 'İletişim'],
      ['ar', 'اتصل بنا'],
      ['en', 'Contact'],
    ] as const) {
      const { unmount } = render(
        <LocaleProvider locale={locale}>
          <ContactRoute />
        </LocaleProvider>,
      )
      expect(screen.getAllByRole('heading', { level: 1 }).map((h) => h.textContent)).toEqual([
        baslik,
      ])
      expect(screen.getByTestId('contact-skeleton').getAttribute('role')).toBe('status')
      unmount()
    }
  })

  it('0028/AC-39: okuma 503 → çevrilmiş hata ve "Tekrar dene"; basınca okuma bir kez tekrarlanır, 200 ile şubeler basılır', async () => {
    vi.mocked(fetchGallery)
      .mockRejectedValueOnce(new ApiError('service.unavailable', null, 503, null))
      .mockResolvedValueOnce(galeri(kadikoy()))
    render(
      <LocaleProvider locale="tr">
        <ContactRoute />
      </LocaleProvider>,
    )

    const hata = await screen.findByTestId('contact-error')
    expect(within(hata).getByText('İletişim bilgileri yüklenemedi')).toBeTruthy()
    expect(fetchGallery).toHaveBeenCalledTimes(1)

    fireEvent.click(within(hata).getByRole('button', { name: 'Tekrar dene' }))
    expect(await screen.findByTestId('contact-branch')).toBeTruthy()
    expect(fetchGallery).toHaveBeenCalledTimes(2)
    expect(screen.queryByTestId('contact-error')).toBeNull()
  })
})

// ----- Harita (D6, D7, D15, D16) -----

describe('0028 — tıkla-yükle harita', () => {
  it.each<[AppLocale, string, string]>([
    ['tr', 'Haritayı göster', "Harita Google Haritalar'dan yüklenir. Google çerez kullanabilir."],
    [
      'ar',
      'اعرض الخريطة',
      'تُحمَّل الخريطة من خرائط Google. قد تستخدم Google ملفات تعريف الارتباط.',
    ],
    ['en', 'Show map', 'The map loads from Google Maps. Google may use cookies.'],
  ])(
    '0028/AC-46, AC-47, AC-48 (%s): yer tutucuda çevrili düğme ve bilgi metni; `<button type="button">`; iframe ve görsel yok',
    (locale, dugme, bilgi) => {
      ekran(galeri(kadikoy()), locale)
      const kutu = screen.getByTestId('map-box')

      expect(within(kutu).getByRole('button', { name: dugme }).getAttribute('type')).toBe('button')
      expect(within(kutu).getByText(bilgi)).toBeTruthy()
      expect(kutu.querySelector('iframe, img')).toBeNull()
    },
  )

  it("0028/AC-50, AC-51, AC-52: düğmeye basınca kutuda iframe; `src` = `embedUrl`, `referrerpolicy` strict-origin-when-cross-origin, çevrili `title`; `sandbox`/`allow`/`allowfullscreen`/`loading` yok; odak iframe'de", () => {
    ekran(galeri(kadikoy()))
    fireEvent.click(screen.getByRole('button', { name: 'Haritayı göster' }))

    const cerceve = within(screen.getByTestId('map-box')).getByTestId('map-frame')
    expect(cerceve.tagName).toBe('IFRAME')
    expect(cerceve.getAttribute('src')).toBe(KADIKOY_EMBED)
    expect(cerceve.getAttribute('referrerpolicy')).toBe('strict-origin-when-cross-origin')
    expect(cerceve.getAttribute('title')).toBe('Kadıköy Şubesi — Google Haritalar haritası')
    for (const yok of ['sandbox', 'allow', 'allowfullscreen', 'loading']) {
      expect(cerceve.hasAttribute(yok)).toBe(false)
    }
    expect(document.activeElement).toBe(cerceve)
  })

  it.each<[AppLocale, string]>([
    ['ar', 'Kadıköy Şubesi — خريطة من خرائط Google'],
    ['en', 'Kadıköy Şubesi — Google Maps map'],
  ])('0028/AC-51 (%s): iframe başlığı çevrili ve şube adını taşır', (locale, baslik) => {
    ekran(galeri(kadikoy()), locale)
    fireEvent.click(within(screen.getByTestId('map-box')).getByRole('button'))

    expect(screen.getByTestId('map-frame').getAttribute('title')).toBe(baslik)
  })

  it("0028/AC-54: iki şubede Kadıköy'ün düğmesi yalnız Kadıköy'ün kutusunu açar", () => {
    ekran(galeri(kadikoy(), bostanci()))
    const [k, b] = bolumler()

    fireEvent.click(within(k!).getByRole('button', { name: 'Haritayı göster' }))

    expect(within(k!).getByTestId('map-frame')).toBeTruthy()
    expect(within(b!).queryByTestId('map-frame')).toBeNull()
    expect(within(b!).getByRole('button', { name: 'Haritayı göster' })).toBeTruthy()
  })

  it('0028/AC-55, AC-56, AC-81 (birim yarısı): `embedUrl` yokken kutu ve düğme yok, bağlantı var; bağlantı `href` = `linkUrl`, `target` yok, üç dilde çevrili', () => {
    for (const [locale, metin] of [
      ['tr', "Google Haritalar'da aç"],
      ['ar', 'افتح في خرائط Google'],
      ['en', 'Open in Google Maps'],
    ] as const) {
      const { unmount } = ekran(galeri(kadikoy({ map: { linkUrl: KADIKOY_LINK } })), locale)

      expect(screen.queryByTestId('map-box')).toBeNull()
      expect(screen.queryByRole('button')).toBeNull()
      const baglanti = screen.getByTestId('map-link')
      expect(baglanti.getAttribute('href')).toBe(KADIKOY_LINK)
      expect(baglanti.hasAttribute('target')).toBe(false)
      expect(baglanti.textContent).toBe(metin)
      unmount()
    }
  })

  // Çerez yarısı e2e'de (`context.cookies()`): oturum kapısı (`check:session`) çerezin JS'ten okunmasını yasaklar.
  it("0028/AC-57 (birim yarısı): haritayı açmak `localStorage`'a ya da `sessionStorage`'a yazmaz; yeniden kurulunca yer tutucu", () => {
    const yazma = vi.spyOn(Storage.prototype, 'setItem')

    const { unmount } = ekran(galeri(kadikoy()))
    fireEvent.click(screen.getByRole('button', { name: 'Haritayı göster' }))
    expect(screen.getByTestId('map-frame')).toBeTruthy()
    unmount()

    ekran(galeri(kadikoy()))
    expect(screen.queryByTestId('map-frame')).toBeNull()
    expect(screen.getByRole('button', { name: 'Haritayı göster' })).toBeTruthy()
    expect(yazma).not.toHaveBeenCalled()
    yazma.mockRestore()
  })

  it('0028/AC-92 (birim yarısı): `map` null olan şubede bağlantı, kutu ve düğme yok; adres metin olarak var', () => {
    ekran(galeri(bostanci({ map: null, address: '–'.repeat(300) })))

    expect(screen.queryByTestId('map-link')).toBeNull()
    expect(screen.queryByTestId('map-box')).toBeNull()
    expect(screen.getByText('–'.repeat(300))).toBeTruthy()
  })
})

// ----- CLARIFY B2: harita durumu yenilemeden bağımsız -----

describe('0028/AC-88, AC-89: StrictMode ve sahte zamanlayıcıyla, açık haritanın iframe düğümü `refreshInSeconds` = 2 yenilemesinde ve sonrasında AYNI düğüm, `src` aynı, kutu aynı; "şu an" satırı yeni durumu basar', () => {
  beforeEach(() => {
    Object.defineProperty(document, 'visibilityState', { configurable: true, get: () => 'visible' })
    vi.useFakeTimers({ toFake: ['setTimeout', 'clearTimeout', 'Date', 'performance'] })
  })

  it('yenileme', async () => {
    vi.mocked(fetchGallery).mockResolvedValue(galeri(kadikoy({ openNow: durum(false) })))
    render(
      <StrictMode>
        <LocaleProvider locale="tr">
          <ContactBranches
            load={{
              kind: 'ready',
              gallery: galeri(kadikoy({ openNow: durum(true, 2) })),
              receivedAt: performance.now(),
            }}
            onRetry={() => {}}
          />
        </LocaleProvider>
      </StrictMode>,
    )
    fireEvent.click(screen.getByRole('button', { name: 'Haritayı göster' }))
    const kutu = screen.getByTestId('map-box')
    const cerceve = screen.getByTestId('map-frame')
    const kutuBicimi = kutu.getAttribute('class')
    expect(screen.getByTestId('branch-now').textContent).toBe('Şu an açık · Kapanış 18:00')

    for (const ms of [2000, 5000]) {
      await act(async () => {
        await vi.advanceTimersByTimeAsync(ms)
      })
      expect(screen.getByTestId('map-frame').isSameNode(cerceve)).toBe(true)
      expect(screen.getByTestId('map-box').isSameNode(kutu)).toBe(true)
      expect(cerceve.getAttribute('src')).toBe(KADIKOY_EMBED)
      expect(kutu.getAttribute('class')).toBe(kutuBicimi)
    }

    expect(fetchGallery).toHaveBeenCalledTimes(1)
    expect(screen.getByTestId('branch-now').textContent).toBe('Şu an kapalı · Açılış yarın 10:00')
  })
})
