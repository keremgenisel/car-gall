import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { AppLocale } from '../../../shared/lib/locale'
import { ListingActionBar } from './ListingActionBar'

/**
 * `0012b` — yayın eylemlerinin ekranı. `docs/frontend.md` § 8.1: gerçek API istemcisi
 * sahtelenmiş hâlde monte ediliyor.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  applyListingAction: vi.fn(),
}))

const { applyListingAction } = await import('../../../shared/api/catalogAdmin')

const ARAC = '0192c000-0000-7000-8000-000000000001'

function ekran(listingStatus: string, options: { locale?: AppLocale; bekliyor?: boolean } = {}) {
  const uygulandi = vi.fn()

  render(
    <LocaleProvider locale={options.locale ?? 'tr'}>
      <ListingActionBar
        vehicleId={ARAC}
        listingStatus={listingStatus}
        awaitingBranchAssignment={options.bekliyor ?? false}
        onApplied={uygulandi}
      />
    </LocaleProvider>,
  )

  return uygulandi
}

beforeEach(() => {
  vi.mocked(applyListingAction).mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0012b/AC-1: Oturum açmış personel, koşulları sağlanan bir taslak ilanı yayına alır ve ilan `Yayında` durumuna geçer', () => {
  it('taslakta yayına alma düğmesi var ve eylemi çağırıyor', async () => {
    const uygulandi = ekran('Draft')

    fireEvent.click(screen.getByTestId('vehicle-action-publish'))

    await waitFor(() =>
      expect(vi.mocked(applyListingAction)).toHaveBeenCalledWith(ARAC, 'publish', 'tr'),
    )

    expect(await screen.findByTestId('vehicle-action-done')).toBeTruthy()
    expect(uygulandi).toHaveBeenCalled()
  })
})

describe("0012b/AC-19: A3'te yazılı olmayan her geçiş `listing.invalid_status_transition` ile reddedilir", () => {
  it('taslakta "rezerve et" düğmesi HİÇ basılmıyor', () => {
    ekran('Draft')

    // Sunucu zaten reddediyor; buradaki amaç sunucunun `409`'una yürüyen bir daveti
    // ekrandan kaldırmak.
    expect(screen.queryByTestId('vehicle-action-reserve')).toBeNull()
    expect(screen.getByTestId('vehicle-action-publish')).toBeTruthy()
    expect(screen.getByTestId('vehicle-action-archive')).toBeTruthy()
  })

  it('geçiş reddi "listeyi tazele" diyor, "eksikleri göster" değil', async () => {
    vi.mocked(applyListingAction).mockRejectedValue(
      new ApiError('listing.invalid_status_transition', null, 409),
    )

    const uygulandi = ekran('Published')

    fireEvent.click(screen.getByTestId('vehicle-action-reserve'))

    const hata = await screen.findByTestId('vehicle-action-error')

    expect(hata.textContent).toContain('aracın şu anki durumunda yapılamaz')

    // BAŞLIK yapılan eylemi anlatıyor; sabit bir "yayına alınamadı" başlığı altı eylemin
    // beşinde yanlış bir olguyu bildirirdi.
    expect(hata.textContent).toContain('İlan rezerve edilemedi')
    expect(hata.textContent).not.toContain('yayına alınamadı')

    // D26 — `409`'da TEKRAR DENE YOK: kural gereği reddedilene tekrar denemesini
    // söylemek, sonsuza kadar denemesini söylemektir (0007/AC-12).
    expect(screen.queryByTestId('vehicle-action-retry')).toBeNull()

    fireEvent.click(screen.getByTestId('vehicle-action-refresh'))

    expect(uygulandi).toHaveBeenCalled()
  })

  it.each([
    ['sold', 'İlan satıldı olarak işaretlenemedi'],
    ['unpublish', 'İlan yayından kaldırılamadı'],
    ['archive', 'İlan arşivlenemedi'],
  ])('%s eyleminin hatası kendi başlığını taşıyor', async (eylem, baslik) => {
    vi.mocked(applyListingAction).mockRejectedValue(new ApiError('api.unavailable', null, 503))

    ekran('Published')

    fireEvent.click(screen.getByTestId(`vehicle-action-${eylem}`))

    const hata = await screen.findByTestId('vehicle-action-error')

    expect(hata.textContent).toContain(baslik)

    // `409` DEĞİL: burada tekrar denemek anlamlı ve düğme o.
    expect(screen.getByTestId('vehicle-action-retry')).toBeTruthy()
  })
})

describe('0012b/AC-14: `Satıldı` terminaldir: araç yeniden yayına alınamaz', () => {
  it('satıldıda yalnız arşivleme var ve kalıcılık söyleniyor', () => {
    ekran('Sold')

    expect(screen.queryByTestId('vehicle-action-publish')).toBeNull()
    expect(screen.getByTestId('vehicle-action-archive')).toBeTruthy()
    expect(screen.getByTestId('vehicle-sold-terminal').textContent).toContain('kalıcıdır')
  })
})

describe('0012b/AC-9: Reddedilen yayın isteği çevrilmiş metin değil **kod listesi** döner', () => {
  it('eksiklerin HEPSİ birlikte listeleniyor ve hiçbir kod basılmıyor', async () => {
    vi.mocked(applyListingAction).mockRejectedValue(
      new ApiError('vehicle.cannot_publish', null, 422, null, {
        reasons: 'vehicle.cannot_publish.no_media,vehicle.cannot_publish.required_field',
        fields: 'damage_record_amount',
      }),
    )

    ekran('Draft')

    fireEvent.click(screen.getByTestId('vehicle-action-publish'))

    const hata = await screen.findByTestId('vehicle-action-error')

    // D2 — tek kod dönmek personeli tek tek denemeye zorlardı; ikisi de görünüyor.
    expect(hata.textContent).toContain('En az bir fotoğrafın hazır olması gerekiyor')
    expect(hata.textContent).toContain('Zorunlu işaretli alanlar boş')

    // D24 — alan anahtarı UÇTA çevriliyor; sunucudan metin gelmiyor.
    expect(hata.textContent).toContain('Tramer tutarı')

    // Yayın reddinin başlığı KENDİ eylemini anlatıyor.
    expect(hata.textContent).toContain('Araç yayına alınamadı')

    // AC-55 — hiçbir teknik detay yok.
    expect(hata.textContent).not.toContain('vehicle.cannot_publish')
    expect(hata.textContent).not.toContain('422')
  })
})

describe('0012b/AC-55: Ekranda hiçbir teknik detay görünmez: durum kodu, `errorCode`, `traceId`, yol', () => {
  it('reddin kodu, durum kodu, traceId ve yol EKRANDA YOK', async () => {
    vi.mocked(applyListingAction).mockRejectedValue(
      new ApiError('concurrency.lost', 'trace-0012b', 409, null, {}),
    )

    ekran('Published')

    fireEvent.click(screen.getByTestId('vehicle-action-sold'))

    const hata = await screen.findByTestId('vehicle-action-error')

    expect(hata.textContent).toContain('başka bir değişiklik oldu')
    expect(hata.textContent).not.toContain('concurrency.lost')
    expect(hata.textContent).not.toContain('trace-0012b')
    expect(hata.textContent).not.toContain('409')
    expect(hata.textContent).not.toContain('/api/admin/')
  })
})

describe('0012b/AC-6: Lokasyon ataması bekleyen araç yayına alınamaz; yanıt `vehicle.cannot_publish.branch_unassigned` taşır', () => {
  it('bayraklı araçta neden düğmeye basılmadan söyleniyor', () => {
    ekran('Draft', { bekliyor: true })

    expect(screen.getByTestId('vehicle-awaiting-branch').textContent).toContain('Şubesi kapatıldı')
  })
})

describe('0012b/AC-54: Eylem ekranı yükleniyor, boş, başarılı ve hata durumlarını ele alır', () => {
  it('uygulanırken düğme kilitli, başarıda bildirim var', async () => {
    let cozumle: () => void = () => {}
    vi.mocked(applyListingAction).mockReturnValue(
      new Promise<void>((resolve) => {
        cozumle = resolve
      }),
    )

    ekran('Draft')

    fireEvent.click(screen.getByTestId('vehicle-action-publish'))

    expect(screen.getByTestId('vehicle-action-publish')).toHaveProperty('disabled', true)

    cozumle()

    expect(await screen.findByTestId('vehicle-action-done')).toBeTruthy()
  })

  it('arşivlenmiş ilanda BOŞ durum: yapılacak eylem kalmadığı söyleniyor', () => {
    ekran('Archived')

    expect(screen.getByTestId('vehicle-actions-empty').textContent).toContain('arşivlendi')
  })
})

describe('0012b/AC-56: Ekranın metinleri TR / AR / EN üçünde de vardır; eksik çeviri anahtarı yoktur', () => {
  it.each(['tr', 'ar', 'en'] as const)('%s kataloğunda ham anahtar BASILMIYOR', (locale) => {
    ekran('Published', { locale })

    expect(screen.getByTestId('vehicle-actions').textContent).not.toContain('vehicle.action')
  })
})

/**
 * `0012c` **D51** — öne çıkarma **yalnız liste ekranında** yaşar. Bu testin yeri de
 * kararın kendisi: düzenleme ekranının eylem çubuğunda bir SIRALAMA kontrolü olmamalı,
 * yoksa yüzey ikiye bölünür ve personel aynı işi iki yerde arar.
 */
describe('0012c: öne çıkarma eylem çubuğunda YOK', () => {
  it('0012c/AC-33: düzenleme ekranının eylem çubuğu öne çıkarma düğmesi basmaz', () => {
    ekran('Published')

    expect(screen.queryByTestId('vehicle-action-feature')).toBeNull()
    expect(screen.queryByTestId('vehicle-action-unfeature')).toBeNull()

    // Metin de yok: düğme testid'siz basılsaydı bu iddia yakalardı.
    expect(screen.getByTestId('vehicle-actions').textContent).not.toContain('Öne çıkar')
  })
})
