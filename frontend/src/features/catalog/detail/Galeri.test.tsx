import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeAll, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import type { ShowcaseImage, ShowcaseListingDetail } from '../../../shared/api/types'
import type { AppLocale } from '../../../shared/lib/locale'
import { ListingDetailPage } from '../ListingDetailPage'

/**
 * `0016b` R1–R7, R20 — detay galerisi ve büyütülmüş görünüm, gerçek detay ekranı içinde;
 * yalnız API istemcisi sahtelenir. Yerel `<dialog>`'un tarayıcıya ait davranışları (Esc,
 * arka planın etkileşimsizliği, odak döngüsü) jsdom'da yoktur ve e2e'de ölçülür
 * (`e2e/galeri.spec.ts`); burada ölçülen, bileşenin kendi kuralları.
 */
vi.mock('../../../shared/api/catalog', () => ({
  fetchListingDetail: vi.fn(),
}))

const { fetchListingDetail } = await import('../../../shared/api/catalog')

beforeAll(() => {
  // jsdom `showModal`/`close` taşımıyor; tarayıcının açma ve kapama olayı taklit edilir.
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.removeAttribute('open')
    this.dispatchEvent(new Event('close'))
  }
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

/** Kaynağı `en`×`boy` olan görsel; türevler `Media`'nın kuralıyla (en uzun kenar 400/800/≤1600). */
function gorsel(n: number, en = 1200, boy = 900): ShowcaseImage {
  const turev = (ad: string, uzun: number) => {
    const sinir = Math.min(uzun, Math.max(en, boy))
    const [w, h] = en >= boy ? [sinir, (sinir * boy) / en] : [(sinir * en) / boy, sinir]
    return { url: `/m/${n}/${ad}.webp`, width: w, height: h }
  }
  return {
    thumbnail: turev('thumbnail', 400),
    display: turev('display', 800),
    large: turev('large', 1600),
  }
}

function detay(images: readonly ShowcaseImage[]): ShowcaseListingDetail {
  return {
    slug: 'toyota-corolla-2019-abc',
    isReserved: false,
    images,
    modelName: 'Toyota Corolla',
    modelYear: 2019,
    mileageKilometers: 125000,
    price: { onRequest: false, amount: 750000, currencyCode: 'TRY', minorUnitExponent: 2 },
    branchName: 'Merkez Şube',
    attributes: [],
  }
}

async function ekran(images: readonly ShowcaseImage[], locale: AppLocale = 'tr') {
  vi.mocked(fetchListingDetail).mockResolvedValue(detay(images))
  const sonuc = render(
    <LocaleProvider locale={locale}>
      <ListingDetailPage slug="toyota-corolla-2019-abc" />
    </LocaleProvider>,
  )
  await screen.findByTestId('listing-detail')
  return sonuc
}

const uc = [gorsel(1), gorsel(2), gorsel(3)]
const sayac = () => screen.getByTestId('gallery-counter').textContent
const resim = () => screen.getByTestId('gallery-image') as HTMLImageElement
const galeri = () => screen.getByTestId('gallery')

function kaydir(hedef: HTMLElement, baslangic: number, bitis: number) {
  fireEvent.touchStart(hedef, { touches: [{ clientX: baslangic, clientY: 100 }] })
  fireEvent.touchEnd(hedef, { changedTouches: [{ clientX: bitis, clientY: 100 }] })
}

describe('0016b — galeri: içerik, sayaç ve gezinme', () => {
  it('0016b/AC-2: AC-1\'in ilanında sayaç ilk görselde "1 / 3" gösterir; toplam 5 değildir', async () => {
    // Sunucu yalnız hazır görselleri gönderir (AC-1, xUnit); sayaç listenin uzunluğunu sayar.
    await ekran(uc)
    expect(sayac()).toBe('1 / 3')
  })

  it('0016b/AC-40: 3 görselli galeride "sonraki" düğmesi "1 / 3"ten "2 / 3"e geçirir; "önceki" düğmesi "1 / 3"e geri getirir', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    expect(sayac()).toBe('2 / 3')
    fireEvent.click(screen.getByRole('button', { name: 'Önceki görsel' }))
    expect(sayac()).toBe('1 / 3')
  })

  it('0016b/AC-41: İlk görselde "önceki" düğmesi devre dışıdır ve etkinleştirilmeye çalışılınca galeri son görsele dönmez', async () => {
    await ekran(uc)
    const onceki = screen.getByRole('button', { name: 'Önceki görsel' })
    expect(onceki.getAttribute('aria-disabled')).toBe('true')
    fireEvent.click(onceki)
    expect(sayac()).toBe('1 / 3')
  })

  it('0016b/AC-42: "Sonraki" ile son görsele gelindiğinde "sonraki" düğmesi devre dışı kalır ve odak galerinin içinde kalır', async () => {
    await ekran(uc)
    const sonraki = screen.getByRole('button', { name: 'Sonraki görsel' })
    sonraki.focus()
    fireEvent.click(sonraki)
    fireEvent.click(sonraki)

    expect(sayac()).toBe('3 / 3')
    expect(sonraki.getAttribute('aria-disabled')).toBe('true')
    // Yerel `disabled` değil: odaklanabilir kalır ve odak gövdeye düşmez.
    expect(sonraki.hasAttribute('disabled')).toBe(false)
    expect(document.activeElement).toBe(sonraki)
    fireEvent.click(sonraki)
    expect(sayac()).toBe('3 / 3')
  })

  it("0016b/AC-43: /tr/'de → tuşu sonraki, ← tuşu önceki görsele geçer; parmağın sağdan sola kaydırılması sonraki görseldir", async () => {
    await ekran(uc, 'tr')
    fireEvent.keyDown(galeri(), { key: 'ArrowRight' })
    expect(sayac()).toBe('2 / 3')
    fireEvent.keyDown(galeri(), { key: 'ArrowLeft' })
    expect(sayac()).toBe('1 / 3')
    kaydir(galeri(), 300, 100)
    expect(sayac()).toBe('2 / 3')
    kaydir(galeri(), 100, 300)
    expect(sayac()).toBe('1 / 3')
  })

  it("0016b/AC-44: /ar/'de ← tuşu sonraki, → tuşu önceki görsele geçer; parmağın soldan sağa kaydırılması sonraki görseldir", async () => {
    await ekran(uc, 'ar')
    fireEvent.keyDown(galeri(), { key: 'ArrowLeft' })
    expect(sayac()).toBe('2 / 3')
    fireEvent.keyDown(galeri(), { key: 'ArrowRight' })
    expect(sayac()).toBe('1 / 3')
    kaydir(galeri(), 100, 300)
    expect(sayac()).toBe('2 / 3')
    kaydir(galeri(), 300, 100)
    expect(sayac()).toBe('1 / 3')
  })

  it('kısa bir dokunuş (eşik altı kaydırma) gezinme sayılmaz', async () => {
    await ekran(uc)
    kaydir(galeri(), 200, 180)
    expect(sayac()).toBe('1 / 3')
  })
})

describe('0016b — büyütülmüş görünüm', () => {
  it('0016b/AC-46: Satır içi görselden büyütme açıldığında büyütülmüş görünüm aynı görselin large türevinin adresini gösterir', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    fireEvent.click(screen.getByTestId('gallery-enlarge'))

    expect(screen.getByTestId('lightbox').hasAttribute('open')).toBe(true)
    expect((screen.getByTestId('lightbox-image') as HTMLImageElement).getAttribute('src')).toBe(
      '/m/2/large.webp',
    )
  })

  it('0016b/AC-47: Büyütülmüş görünümde düğmeler, ok tuşları ve kaydırma R3 gibi çalışır; uçlarda dönmez', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByTestId('gallery-enlarge'))
    const kutu = within(screen.getByTestId('lightbox'))
    const lightboxSayac = () => kutu.getByTestId('lightbox-counter').textContent

    fireEvent.click(kutu.getByRole('button', { name: 'Önceki görsel' }))
    expect(lightboxSayac()).toBe('1 / 3')
    fireEvent.click(kutu.getByRole('button', { name: 'Sonraki görsel' }))
    expect(lightboxSayac()).toBe('2 / 3')
    fireEvent.keyDown(kutu.getByTestId('lightbox-image'), { key: 'ArrowRight' })
    expect(lightboxSayac()).toBe('3 / 3')
    fireEvent.keyDown(kutu.getByTestId('lightbox-image'), { key: 'ArrowRight' })
    expect(lightboxSayac()).toBe('3 / 3')
    kaydir(kutu.getByTestId('lightbox-image'), 100, 300)
    expect(lightboxSayac()).toBe('2 / 3')
  })

  it('0016b/AC-49: Büyütülmüş görünüm kapatma düğmesiyle kapanır', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByTestId('gallery-enlarge'))
    fireEvent.click(screen.getByRole('button', { name: 'Kapat' }))
    expect(screen.getByTestId('lightbox').hasAttribute('open')).toBe(false)
    expect(screen.queryByTestId('lightbox-image')).toBeNull()
  })

  it('0016b/AC-52: Görünüm kapandığında odak görünümü açan öğeye döner', async () => {
    await ekran(uc)
    const buyut = screen.getByTestId('gallery-enlarge')
    buyut.focus()
    fireEvent.click(buyut)
    screen.getByRole('button', { name: 'Kapat' }).focus()
    fireEvent.click(screen.getByRole('button', { name: 'Kapat' }))
    expect(document.activeElement).toBe(buyut)
  })

  it('0016b/AC-53: Görünüm açılıp kapatıldığında ve içinde gezinildiğinde adres değişmez ve history.length artmaz', async () => {
    await ekran(uc)
    const adres = window.location.href
    const gecmis = window.history.length

    fireEvent.click(screen.getByTestId('gallery-enlarge'))
    fireEvent.click(
      within(screen.getByTestId('lightbox')).getByRole('button', { name: 'Sonraki görsel' }),
    )
    fireEvent.click(screen.getByRole('button', { name: 'Kapat' }))

    expect(window.location.href).toBe(adres)
    expect(window.history.length).toBe(gecmis)
  })

  it('0016b/AC-54: Büyütülmüş görünümde 3. görsele gidilip görünüm kapatıldığında satır içi galeri 3. görseli gösterir ve sayaç "3 / N" olur', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByTestId('gallery-enlarge'))
    const kutu = within(screen.getByTestId('lightbox'))
    fireEvent.click(kutu.getByRole('button', { name: 'Sonraki görsel' }))
    fireEvent.click(kutu.getByRole('button', { name: 'Sonraki görsel' }))
    fireEvent.click(screen.getByRole('button', { name: 'Kapat' }))

    expect(sayac()).toBe('3 / 3')
    expect(resim().getAttribute('src')).toBe('/m/3/display.webp')
  })

  it('0016b/AC-55: 3. görseldeyken sayfa yeniden yüklendiğinde galeri 1. görselden başlar', async () => {
    const { unmount } = await ekran(uc)
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    expect(sayac()).toBe('3 / 3')

    unmount()
    await ekran(uc)
    expect(sayac()).toBe('1 / 3')
  })
})

describe('0016b — alt metni', () => {
  const onSekiz = Array.from({ length: 18 }, (_, i) => gorsel(i + 1))

  it('0016b/AC-56: 18 hazır görselli Toyota Corolla 2019 ilanının /tr/ detayında 3. görselin alt metni "Toyota Corolla 2019 — görsel 3/18"dir', async () => {
    await ekran(onSekiz)
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    expect(resim().alt).toBe('Toyota Corolla 2019 — görsel 3/18')
  })

  it.each(['ar', 'en'] as const)(
    "0016b/AC-57: /%s/ detayında alt metni o locale'in şablonundan üretilir ve Türkçe şablonu içermez",
    async (locale) => {
      await ekran(onSekiz, locale)
      expect(resim().alt).not.toContain('görsel')
      expect(resim().alt).toContain('Toyota Corolla 2019')
      expect(resim().alt).toContain('1/18')
    },
  )

  it('0016b/AC-58: Media\'daki alt metni "Ön görünüm" iken detaydaki alt metni bu metni içermez; yalnız R5\'in şablonundan gelir', async () => {
    // Sözleşmede Media'nın alt metni YOK (X7): sunucu onu taşımaz, uç onu bilemez.
    await ekran(uc)
    expect(Object.keys(uc[0]!)).not.toContain('altText')
    expect(resim().alt).toBe('Toyota Corolla 2019 — görsel 1/3')
    expect(resim().alt).not.toContain('Ön görünüm')
  })

  it('0016b/AC-59: Büyütülmüş görünümdeki görselin alt metni aynı şablondan, aynı sıra ve toplamla üretilir', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    fireEvent.click(screen.getByTestId('gallery-enlarge'))
    expect((screen.getByTestId('lightbox-image') as HTMLImageElement).alt).toBe(
      'Toyota Corolla 2019 — görsel 2/3',
    )
  })
})

describe('0016b — yükleme ve yerleşim', () => {
  it("0016b/AC-60: Satır içi görselin srcset'i üç türevi gerçek genişlikleriyle listeler: 1200×900 kaynak için 400w, 800w ve 1200w", async () => {
    await ekran([gorsel(1, 1200, 900)])
    expect(resim().getAttribute('srcset')).toBe(
      '/m/1/thumbnail.webp 400w, /m/1/display.webp 800w, /m/1/large.webp 1200w',
    )
  })

  it('0016b/AC-61: display türevi 600×800 (dikey) olan görsel width="600" height="800" ile basılır; 960×720 yazılmaz', async () => {
    await ekran([gorsel(1, 900, 1200)])
    expect(resim().getAttribute('width')).toBe('600')
    expect(resim().getAttribute('height')).toBe('800')
  })

  it('0016b/AC-63: İlk görsel loading="lazy" taşımaz ve fetchpriority="high" taşır; diğer görsellerin her biri loading="lazy" taşır', async () => {
    await ekran(uc)
    expect(resim().getAttribute('loading')).toBeNull()
    expect(resim().getAttribute('fetchpriority')).toBe('high')

    for (let i = 1; i < uc.length; i += 1) {
      fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
      expect(resim().getAttribute('loading')).toBe('lazy')
      expect(resim().getAttribute('fetchpriority')).toBeNull()
    }
  })

  it('0016b/AC-64 (bileşen yarısı): etkileşimden önce galeride yalnız BİR görsel öğesi vardır', async () => {
    // Ağ kaydının ölçümü e2e'de (`galeri.spec.ts`); burada yalnız ilk görselin basıldığı.
    await ekran(Array.from({ length: 30 }, (_, i) => gorsel(i + 1)))
    expect(galeri().querySelectorAll('img')).toHaveLength(1)
  })
})

describe('0016b — durumlar', () => {
  it('0016b/AC-65: Hiç hazır görseli olmayan ilanın detayında görsel alanı, sayaç, gezinme düğmeleri ve büyütme yoktur; model, fiyat ve nitelikler görünür', async () => {
    await ekran([])
    expect(screen.queryByTestId('gallery')).toBeNull()
    expect(screen.queryByTestId('gallery-counter')).toBeNull()
    expect(screen.queryByRole('button', { name: 'Sonraki görsel' })).toBeNull()
    expect(screen.queryByTestId('gallery-enlarge')).toBeNull()
    expect(screen.getByRole('heading', { level: 1, name: 'Toyota Corolla' })).toBeTruthy()
    expect(screen.getByTestId('detail-price').textContent).toBe('₺750.000,00')
    expect(screen.getByTestId('detail-attributes')).toBeTruthy()
  })

  it('0016b/AC-66: Medya sırası A (işleniyor), B (başarısız) olan ilan AC-65 ile aynı biçimde basılır', async () => {
    // Sunucu hazır olmayanları göndermez (xUnit AC-1/AC-4): yanıtın görsel listesi boştur.
    await ekran([])
    expect(screen.queryByTestId('gallery')).toBeNull()
    expect(document.querySelector('.listing-detail__media img')).toBeNull()
  })

  it('0016b/AC-67: Tek görselde görsel ve büyütme vardır; sayaç ve gezinme düğmesi yoktur', async () => {
    await ekran([gorsel(2)])
    expect(resim().getAttribute('src')).toBe('/m/2/display.webp')
    expect(screen.getByTestId('gallery-enlarge')).toBeTruthy()
    expect(screen.queryByTestId('gallery-counter')).toBeNull()
    expect(screen.queryByRole('button', { name: 'Sonraki görsel' })).toBeNull()
    expect(screen.queryByRole('button', { name: 'Önceki görsel' })).toBeNull()
  })

  it('0016b/AC-68: Tek görselli ilanda büyütülmüş görünüm açılır; görünümde sayaç ve gezinme düğmesi yoktur', async () => {
    await ekran([gorsel(2)])
    fireEvent.click(screen.getByTestId('gallery-enlarge'))
    const kutu = within(screen.getByTestId('lightbox'))
    expect(kutu.getByTestId('lightbox-image').getAttribute('src')).toBe('/m/2/large.webp')
    expect(kutu.queryByTestId('lightbox-counter')).toBeNull()
    expect(kutu.queryByRole('button', { name: 'Sonraki görsel' })).toBeNull()
  })

  it('0016b/AC-69: 2. görsel yüklenemediğinde o yuvada yer tutucu gösterilir; sayaç "2 / 3" gösterir ve "sonraki" ile 3. görsele geçilir', async () => {
    await ekran(uc)
    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    act(() => {
      fireEvent.error(resim())
    })

    expect(screen.getByTestId('gallery-placeholder').getAttribute('aria-label')).toBe(
      'Toyota Corolla 2019 — görsel 2/3',
    )
    expect(screen.queryByTestId('gallery-image')).toBeNull()
    expect(sayac()).toBe('2 / 3')

    fireEvent.click(screen.getByRole('button', { name: 'Sonraki görsel' }))
    expect(sayac()).toBe('3 / 3')
    expect(resim().getAttribute('src')).toBe('/m/3/display.webp')
  })

  it('0016b/AC-70: Galeri ve şema detay yanıtıyla gelir: detay isteğinden ayrı bir veri isteği yapılmaz ve galeriye özgü yükleniyor ya da hata göstergesi yoktur', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch')
    await ekran(uc)

    expect(vi.mocked(fetchListingDetail)).toHaveBeenCalledTimes(1)
    expect(fetchSpy).not.toHaveBeenCalled()
    expect(within(galeri()).queryByRole('status')).toBeNull()
    expect(within(galeri()).queryByRole('alert')).toBeNull()
    fetchSpy.mockRestore()
  })
})
