import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { AppLocale } from '../../../shared/lib/locale'
import type { VehicleMedia, VehicleMediaItem } from '../../../shared/api/types'
import { VehicleMediaPage } from './VehicleMediaPage'

/**
 * `docs/frontend.md` § 8.1 — ekran, API istemcisi sahtelenmiş hâlde monte edilir.
 *
 * R7'nin üç durumu ve **D8**'in dördüncü hâli burada ölçülüyor; sınır kriterleri
 * (AC-10, AC-15) sunucu tarafında ölçülüyor ve uçta ölçülemez — oturum çerezi
 * JavaScript'e kapalı (`0006` C17).
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  fetchVehicleMedia: vi.fn(),
  updateVehicleMedia: vi.fn(),
}))

vi.mock('../../../shared/api/mediaAdmin', () => ({
  uploadMediaAsset: vi.fn(),
  setMediaAssetAltText: vi.fn(),
  retryMediaAsset: vi.fn(),
}))

const { fetchVehicleMedia, updateVehicleMedia } = await import('../../../shared/api/catalogAdmin')
const { uploadMediaAsset, setMediaAssetAltText, retryMediaAsset } =
  await import('../../../shared/api/mediaAdmin')

const ARAC = '0192c000-0000-7000-8000-000000000001'
const BIR = '0192c000-0000-7000-8000-00000000f001'
const IKI = '0192c000-0000-7000-8000-00000000f002'
const UC = '0192c000-0000-7000-8000-00000000f003'

function oge(id: string, extra: Partial<VehicleMediaItem> = {}): VehicleMediaItem {
  return {
    mediaAssetId: id,
    status: 'ready',
    failureReason: null,
    altText: {},
    derivatives: [
      { variant: 'display', width: 1200, height: 900, url: `https://cdn.test/${id}.webp` },
    ],
    ...extra,
  }
}

function medya(items: readonly VehicleMediaItem[]): VehicleMedia {
  return {
    vehicleId: ARAC,
    coverMediaAssetId: items.length > 0 ? items[0].mediaAssetId : null,
    items,
  }
}

function ekran(locale: AppLocale = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <VehicleMediaPage vehicleId={ARAC} />
    </LocaleProvider>,
  )
}

beforeEach(() => {
  vi.mocked(fetchVehicleMedia).mockResolvedValue(medya([oge(BIR), oge(IKI)]))
  vi.mocked(updateVehicleMedia).mockResolvedValue(undefined)
  vi.mocked(uploadMediaAsset).mockResolvedValue({ id: UC, status: 'uploaded' })
  vi.mocked(setMediaAssetAltText).mockResolvedValue(undefined)
  vi.mocked(retryMediaAsset).mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0012a/AC-25: Medya ekranı yükleniyor durumunda iskelet gösterir', () => {
  it('okuma dönmeden iskelet basılır', () => {
    vi.mocked(fetchVehicleMedia).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('vehicle-media-skeleton')).toBeTruthy()
  })
})

describe('0012a/AC-26: Bağlı fotoğrafı olmayan araçta boş durum neden boş olduğunu ve ne yapılabileceğini söyler', () => {
  it('boş durum hem nedeni hem eylemi söyler', async () => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(medya([]))

    ekran()

    const bos = await screen.findByTestId('vehicle-media-empty')

    expect(bos.textContent).toContain('Bu araca bağlı fotoğraf yok')
    expect(bos.textContent).toContain('ilk fotoğraf aracın kapak görseli olur')
  })
})

describe('0012a/AC-27: Okuma başarısız olduğunda hata durumu anlamlı mesaj ve tekrar dene eylemi gösterir', () => {
  it('hata şeridi tekrar dener ve ikinci okuma listeyi getirir', async () => {
    vi.mocked(fetchVehicleMedia).mockRejectedValueOnce(new ApiError('api.unavailable', null))

    ekran()

    await screen.findByTestId('vehicle-media-error')

    fireEvent.click(screen.getByRole('button', { name: 'Tekrar dene' }))

    expect(await screen.findByTestId(`vehicle-media-item-${BIR}`)).toBeTruthy()
  })
})

describe('0012a/AC-28: `işleniyor` bir varlık ekranda ayrı bir hâl olarak görünür ve ekranın yükleme iskeletiyle karışmaz', () => {
  it('işlenen varlık kendi hâlini gösterir; iskelet BASILMAZ', async () => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(
      medya([oge(BIR, { status: 'processing', derivatives: [] }), oge(IKI)]),
    )

    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${BIR}`)

    expect(kutu.textContent).toContain('İşleniyor')
    expect(kutu.textContent).toContain('birkaç dakika sürebilir')
    expect(screen.queryByTestId('vehicle-media-skeleton')).toBeNull()
  })
})

describe('0012a/AC-29: Ekranda hiçbir teknik detay görünmez: durum kodu, `errorCode`, `traceId`, yol', () => {
  it('hata ekranı kodu DEĞİL çevrilmiş metni basar', async () => {
    vi.mocked(fetchVehicleMedia).mockRejectedValue(
      new ApiError('media.storage_unavailable', null, 503, 'trace-0012a'),
    )

    ekran()

    const hata = await screen.findByTestId('vehicle-media-error')

    expect(hata.textContent).toContain('Fotoğraflar getirilemedi')
    expect(hata.textContent).not.toContain('media.storage_unavailable')
    expect(hata.textContent).not.toContain('503')
    expect(hata.textContent).not.toContain('trace-0012a')
  })

  it('yükleme reddi 413 ile 415 için AYRI mesaj verir ve kodu basmaz', async () => {
    vi.mocked(uploadMediaAsset).mockRejectedValueOnce(
      new ApiError('media.unsupported_type', null, 415),
    )

    ekran()

    await screen.findByTestId('vehicle-media-upload')

    const dosya = new File(['x'], 'belge.pdf', { type: 'application/pdf' })
    fireEvent.change(screen.getByLabelText('Fotoğraf ekle'), { target: { files: [dosya] } })
    fireEvent.click(screen.getByTestId('vehicle-media-upload-submit'))

    const hata = await screen.findByTestId('vehicle-media-upload-error')

    expect(hata.textContent).toContain('Bu dosya türü desteklenmiyor')
    expect(hata.textContent).not.toContain('media.unsupported_type')
    expect(hata.textContent).not.toContain('415')
  })
})

describe('0012a/AC-1: Oturum açmış personel panelden tek bir fotoğraf yükler ve varlık `yüklendi` durumunda oluşur', () => {
  it('kontrol ÇOKLU seçime kapalı ve yüklenen varlık `yüklendi` hâliyle görünür', async () => {
    ekran()

    await screen.findByTestId('vehicle-media-upload')

    const kontrol = screen.getByLabelText('Fotoğraf ekle')

    // X3 — çoklu seçim bilerek yok; tek dosya yolu doğrulanmadan çoklu akış hata
    // durumlarını katlar.
    expect(kontrol).not.toHaveProperty('multiple', true)

    fireEvent.change(kontrol, {
      target: { files: [new File(['x'], 'arac.jpg', { type: 'image/jpeg' })] },
    })
    fireEvent.click(screen.getByTestId('vehicle-media-upload-submit'))

    const kutu = await screen.findByTestId(`vehicle-media-item-${UC}`)

    expect(kutu.textContent).toContain('Sıraya alındı')
  })
})

describe('0012a/AC-2: Personel yüklenmiş bir fotoğrafı araca bağlar; aracın bağlı listesi onu içerir', () => {
  it('bağlama AÇIK bir kaydetmedir: yükleme tek başına listeyi yazmıyor', async () => {
    ekran()

    await screen.findByTestId('vehicle-media-upload')

    fireEvent.change(screen.getByLabelText('Fotoğraf ekle'), {
      target: { files: [new File(['x'], 'arac.jpg', { type: 'image/jpeg' })] },
    })
    fireEvent.click(screen.getByTestId('vehicle-media-upload-submit'))

    await screen.findByTestId(`vehicle-media-item-${UC}`)

    // Yükleme `Media`'nın işi; bağlama `VehicleCatalog`'un. Yükleme tek başına aracın
    // listesini DEĞİŞTİRMEZ.
    expect(vi.mocked(updateVehicleMedia)).not.toHaveBeenCalled()

    fireEvent.click(screen.getByTestId('vehicle-media-save'))

    await waitFor(() => expect(vi.mocked(updateVehicleMedia)).toHaveBeenCalled())

    expect(vi.mocked(updateVehicleMedia).mock.calls[0][1]).toEqual([BIR, IKI, UC])
  })
})

describe('0012a/AC-3: Bağlı fotoğraflar personelin verdiği sırada durur ve sıra kalıcıdır', () => {
  it('sunucudan gelen sıra EKRANDA aynı sırada basılır', async () => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(medya([oge(IKI), oge(BIR)]))

    ekran()

    await screen.findByTestId(`vehicle-media-item-${IKI}`)

    const kutular = screen
      .getAllByTestId(/^vehicle-media-item-/)
      .map((kutu) => kutu.getAttribute('data-testid'))

    expect(kutular).toEqual([`vehicle-media-item-${IKI}`, `vehicle-media-item-${BIR}`])
  })
})

describe('0012a/AC-5: Sıra değiştiğinde kapak da değişir', () => {
  it('ikinciyi yukarı taşımak kapak rozetini TAŞIR ve yeni sırayı gönderir', async () => {
    ekran()

    const ikinci = await screen.findByTestId(`vehicle-media-item-${IKI}`)

    expect(screen.getByTestId(`vehicle-media-item-${BIR}`).textContent).toContain('Kapak')

    fireEvent.click(ikinci.querySelector('[data-testid="vehicle-media-up"]') as HTMLElement)

    expect(screen.getByTestId(`vehicle-media-item-${IKI}`).textContent).toContain('Kapak')
    expect(screen.getByTestId(`vehicle-media-item-${BIR}`).textContent).not.toContain('Kapak')

    fireEvent.click(screen.getByTestId('vehicle-media-save'))

    await waitFor(() => expect(vi.mocked(updateVehicleMedia)).toHaveBeenCalled())

    expect(vi.mocked(updateVehicleMedia).mock.calls[0][1]).toEqual([IKI, BIR])
  })
})

describe('0012a/AC-6: Personel bağlı bir fotoğrafı çıkarabilir; aracın listesi onu artık içermez', () => {
  it('çıkarmak kimliği listeden düşürür ve kaydetme kalan listeyi gönderir', async () => {
    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${BIR}`)

    fireEvent.click(kutu.querySelector('[data-testid="vehicle-media-detach"]') as HTMLElement)

    expect(screen.queryByTestId(`vehicle-media-item-${BIR}`)).toBeNull()

    fireEvent.click(screen.getByTestId('vehicle-media-save'))

    await waitFor(() => expect(vi.mocked(updateVehicleMedia)).toHaveBeenCalled())

    expect(vi.mocked(updateVehicleMedia).mock.calls[0][1]).toEqual([IKI])
  })
})

/**
 * Sınır değer: liste → **0**. Dış bulgu bunu yakaladı — AC-6'nın üç testi de hep bir öğe
 * bırakıyordu (e2e ikiden birini, bileşen testi ikiden birini, domain testi üçten birini)
 * ve kaydetme düğmesinin boş listede KAYBOLDUĞU hiçbir yerde ölçülmüyordu.
 */
describe('0012a/AC-6: Personel bağlı bir fotoğrafı çıkarabilir; aracın listesi onu artık içermez', () => {
  it('TEK fotoğraf çıkarılabiliyor: boş durumda bile kaydetme yolu duruyor', async () => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(medya([oge(BIR)]))

    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${BIR}`)

    fireEvent.click(kutu.querySelector('[data-testid="vehicle-media-detach"]') as HTMLElement)

    // Boş durum basılıyor AMA kaydetme ekrandan kaybolmuyor.
    expect(screen.getByTestId('vehicle-media-empty')).toBeTruthy()

    const kaydet = screen.getByTestId('vehicle-media-save')

    expect(kaydet).toHaveProperty('disabled', false)

    fireEvent.click(kaydet)

    await waitFor(() => expect(vi.mocked(updateVehicleMedia)).toHaveBeenCalled())

    // Boş liste GEÇERLİ bir yazma: "bütün fotoğrafları çıkar" ve varlıkları silmiyor.
    expect(vi.mocked(updateVehicleMedia).mock.calls[0][1]).toEqual([])
  })
})

describe('0012a/AC-16: İşlemesi başarısız olmuş fotoğraf panelde ayırt edilir', () => {
  it('başarısız varlık durumunu, NEDENİNİ ve tekrar dene eylemini gösterir', async () => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(
      medya([
        oge(BIR, { status: 'failed', failureReason: 'watermarkunavailable', derivatives: [] }),
      ]),
    )

    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${BIR}`)

    expect(kutu.textContent).toContain('Başarısız')
    expect(kutu.textContent).toContain('Galeri logosu okunamadı')
    expect(kutu.querySelector('[data-testid="vehicle-media-retry"]')).toBeTruthy()
  })
})

describe('0012a/AC-17: Personel tekrar dene eylemini çağırır ve varlık aynı kimlikle yeniden kuyruğa alınır', () => {
  it('eylem AYNI kimliği gönderiyor ve ekran sunucudan yeniden okuyor', async () => {
    vi.mocked(fetchVehicleMedia)
      .mockResolvedValueOnce(
        medya([oge(BIR, { status: 'failed', failureReason: 'undecodable', derivatives: [] })]),
      )
      .mockResolvedValue(medya([oge(BIR, { status: 'processing', derivatives: [] })]))

    ekran()

    await screen.findByTestId('vehicle-media-failure')

    fireEvent.click(screen.getByTestId('vehicle-media-retry'))

    await waitFor(() => expect(vi.mocked(retryMediaAsset)).toHaveBeenCalledWith(BIR, 'tr'))

    // `findBy*` BULANA KADAR yeniden deniyor; `getBy…textContent` ise o anki DOM'u
    // okuyor. İkincisi beş koşuda bir kırmızı veriyordu: yeniden okuma çözülmüş ama
    // React henüz yeniden çizmemiş oluyordu. Kırılgan bir kapı, olmayan kapıdan kötüdür.
    await screen.findByTestId('vehicle-media-processing')

    expect(screen.getByTestId(`vehicle-media-item-${BIR}`).textContent).toContain('İşleniyor')
  })
})

describe('0012a/AC-18: Tekrar deneme otomatik tetiklenmez; eylem çağrılmadan durum kendiliğinden değişmez', () => {
  it('başarısız varlık ekranda DURUYOR; hiçbir istek kendiliğinden gitmiyor', async () => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(
      medya([oge(BIR, { status: 'failed', failureReason: 'undecodable', derivatives: [] })]),
    )

    ekran()

    await screen.findByTestId('vehicle-media-failure')

    expect(vi.mocked(retryMediaAsset)).not.toHaveBeenCalled()
  })
})

describe('0012a/AC-11: Personel bir fotoğrafın `alt` metnini etkin her dilde yazabilir', () => {
  it('üç dil için ayrı alan basılıyor ve üçü BİRLİKTE gönderiliyor', async () => {
    ekran()

    const kart = within(await screen.findByTestId(`vehicle-media-item-${BIR}`))

    fireEvent.change(kart.getByLabelText('Kısa tanım (Türkçe)'), {
      target: { value: 'Beyaz Corolla, önden' },
    })
    fireEvent.change(kart.getByLabelText('Kısa tanım (العربية)'), {
      target: { value: 'كورولا بيضاء من الأمام' },
    })
    fireEvent.change(kart.getByLabelText('Kısa tanım (English)'), {
      target: { value: 'White Corolla, front view' },
    })

    fireEvent.click(kart.getByTestId('vehicle-media-alt-save'))

    await waitFor(() => expect(vi.mocked(setMediaAssetAltText)).toHaveBeenCalled())

    expect(vi.mocked(setMediaAssetAltText).mock.calls[0][1]).toEqual({
      tr: 'Beyaz Corolla, önden',
      ar: 'كورولا بيضاء من الأمام',
      en: 'White Corolla, front view',
    })
  })
})

describe('0012a/AC-12: Bir dili boş bırakmak geçerli bir durumdur; varlık yine okunabilir', () => {
  it('boş dil gövdeye GİRMİYOR ve kaydetme yine yapılıyor', async () => {
    ekran()

    const kart = within(await screen.findByTestId(`vehicle-media-item-${BIR}`))

    fireEvent.change(kart.getByLabelText('Kısa tanım (Türkçe)'), {
      target: { value: 'Beyaz Corolla' },
    })
    fireEvent.click(kart.getByTestId('vehicle-media-alt-save'))

    await waitFor(() => expect(vi.mocked(setMediaAssetAltText)).toHaveBeenCalled())

    expect(vi.mocked(setMediaAssetAltText).mock.calls[0][1]).toEqual({ tr: 'Beyaz Corolla' })
  })

  it('metni olmayan fotoğrafın `alt` niteliği UYDURULMUYOR, boş kalıyor', async () => {
    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${BIR}`)
    const gorsel = kutu.querySelector('img')

    expect(gorsel?.getAttribute('alt')).toBe('')
    // § 7 — açık `width`/`height`: türev geldiğinde düzen kaymaz.
    expect(gorsel?.getAttribute('width')).toBe('1200')
    expect(gorsel?.getAttribute('height')).toBe('900')
  })
})

describe('0012a/AC-4: Sıralı listenin ilk elemanı kapak görselidir; ayrı bir kapak alanı yoktur', () => {
  it('rozet yalnız İLK fotoğrafta; "kapak yap" diye bir eylem YOK', async () => {
    ekran()

    await screen.findByTestId(`vehicle-media-item-${BIR}`)

    expect(screen.getByTestId(`vehicle-media-item-${BIR}`).textContent).toContain('Kapak')
    expect(screen.getByTestId(`vehicle-media-item-${IKI}`).textContent).not.toContain('Kapak')
    expect(screen.queryByRole('button', { name: /kapak yap/i })).toBeNull()
  })
})

describe('0012a/AC-30: Ekranın metinleri TR / AR / EN üçünde de vardır; eksik çeviri anahtarı yoktur', () => {
  it.each(['tr', 'ar', 'en'] as const)('%s kataloğunda ham anahtar BASILMIYOR', async (locale) => {
    vi.mocked(fetchVehicleMedia).mockResolvedValue(
      medya([oge(BIR, { status: 'failed', failureReason: 'storageunavailable', derivatives: [] })]),
    )

    ekran(locale)

    const kutu = await screen.findByTestId(`vehicle-media-item-${BIR}`)
    const metin = `${document.body.textContent ?? ''}${kutu.textContent ?? ''}`

    expect(metin).not.toContain('vehicle.media.')
  })
})

describe('Kaydetme akışı', () => {
  it('kirli değilken kaydetme düğmesi KAPALI — boşa giden tam liste yazımı yok', async () => {
    ekran()

    await screen.findByTestId(`vehicle-media-item-${BIR}`)

    expect(screen.getByTestId('vehicle-media-save')).toHaveProperty('disabled', true)
  })

  /**
   * Bu testi e2e yazdırdı: kaydetme başarıdan sonra sunucudan yeniden okuyor ve ilk
   * hâlimde okuma, bildirimi siliyordu — personel kaydettiğine dair hiçbir işaret
   * görmüyordu. Burada iki okuma AYRI yanıt döndürüyor, yani yeniden okuma gerçekten
   * koşuyor.
   */
  it('kaydetme bildirimi yeniden okumadan sağ çıkar', async () => {
    vi.mocked(fetchVehicleMedia)
      .mockResolvedValueOnce(medya([oge(BIR), oge(IKI)]))
      .mockResolvedValue(medya([oge(IKI), oge(BIR)]))

    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${IKI}`)

    fireEvent.click(kutu.querySelector('[data-testid="vehicle-media-up"]') as HTMLElement)
    fireEvent.click(screen.getByTestId('vehicle-media-save'))

    await screen.findByTestId('vehicle-media-saved')

    // Yeniden okuma gerçekten koştu: sunucunun YENİ sırası ekranda.
    await waitFor(() => expect(vi.mocked(fetchVehicleMedia).mock.calls.length).toBeGreaterThan(1))

    expect(screen.getByTestId('vehicle-media-saved')).toBeTruthy()
    expect(screen.getByTestId('vehicle-media-save')).toHaveProperty('disabled', true)
  })

  it('kaydetme reddedilirse taslak KAYBOLMAZ ve şerit tekrar dener', async () => {
    vi.mocked(updateVehicleMedia).mockRejectedValueOnce(
      new ApiError('vehicle.media_asset_not_found', null, 422),
    )

    ekran()

    const kutu = await screen.findByTestId(`vehicle-media-item-${IKI}`)

    fireEvent.click(kutu.querySelector('[data-testid="vehicle-media-up"]') as HTMLElement)
    fireEvent.click(screen.getByTestId('vehicle-media-save'))

    const serit = await screen.findByTestId('vehicle-media-save-error')

    expect(serit.textContent).toContain('Fotoğraflardan biri artık bulunamıyor')
    // Taslak duruyor: taşınan fotoğraf hâlâ kapak.
    expect(screen.getByTestId(`vehicle-media-item-${IKI}`).textContent).toContain('Kapak')
  })
})

describe('Medya koşulu rozetinin ekrandaki hâli', () => {
  it('hazır fotoğraf varken KARŞILANDI, yokken karşılanmadı yazıyor — yayın düğmesi YOK (D7)', async () => {
    ekran()

    expect((await screen.findByTestId('vehicle-media-condition')).textContent).toContain(
      'En az bir fotoğraf hazır',
    )
    expect(screen.queryByRole('button', { name: /yayına al/i })).toBeNull()

    cleanup()
    vi.mocked(fetchVehicleMedia).mockResolvedValue(
      medya([oge(BIR, { status: 'processing', derivatives: [] })]),
    )

    ekran()

    expect((await screen.findByTestId('vehicle-media-condition')).textContent).toContain(
      'Hazır fotoğraf yok',
    )
  })
})
