import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { AdminGallery } from '../../shared/api/dealershipAdmin'
import type { MediaAssetState } from '../../shared/api/mediaAdmin'
import type { CurrentSession } from '../../shared/api/types'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'
import vitrinTr from '../../shared/i18n/locales/tr.json'
import type { AppLocale } from '../../shared/lib/locale'
import { AdminApp } from './AdminApp'

/**
 * `0026` D8 · D12 · D13 — galeri profili ekranının "Logo" bölümü, gerçek kabuğun içinde; yalnız API
 * istemcileri sahtelenir (docs/frontend.md § 8.1). Uçların kendisi `Host.Tests/GaleriLogosuZinciriTests`
 * ve `LogoYuklemeUcuTests`'te.
 */
vi.mock('../../shared/api/identity', () => ({
  fetchCurrentSession: vi.fn(),
  fetchStaff: vi.fn(),
  addStaff: vi.fn(),
  logout: vi.fn(),
  fetchTwoFactorStatus: vi.fn(),
  startTwoFactorEnrollment: vi.fn(),
  confirmTwoFactorEnrollment: vi.fn(),
  disableTwoFactor: vi.fn(),
  regenerateRecoveryCodes: vi.fn(),
  resetStaffTwoFactor: vi.fn(),
}))

vi.mock('../../shared/api/dealershipAdmin', () => ({
  fetchGalleryProfile: vi.fn(),
  saveGalleryProfile: vi.fn(),
  assignGalleryLogo: vi.fn(),
  removeGalleryLogo: vi.fn(),
  fetchBranchList: vi.fn(),
  fetchAdminBranch: vi.fn(),
  openBranch: vi.fn(),
  saveBranch: vi.fn(),
  runBranchAction: vi.fn(),
}))

vi.mock('../../shared/api/mediaAdmin', () => ({
  uploadMediaAsset: vi.fn(),
  fetchMediaAsset: vi.fn(),
  retryMediaAsset: vi.fn(),
  setMediaAssetAltText: vi.fn(),
}))

const { fetchCurrentSession } = await import('../../shared/api/identity')
const api = await import('../../shared/api/dealershipAdmin')
const medya = await import('../../shared/api/mediaAdmin')

beforeAll(() => {
  Object.defineProperty(window, 'location', {
    configurable: true,
    value: { ...window.location, assign: vi.fn(), pathname: '/admin' },
  })
})

const GALERI: AdminGallery = {
  commercialName: 'Oto Galeri Ticaret A.Ş.',
  taxNumber: null,
  tradeRegistryNumber: null,
  displayNames: { tr: 'Oto Galeri' },
  socialLinks: {},
  version: 4,
  logoMediaAssetId: null,
}

const KUCUK = {
  variant: 'logo-small',
  width: 320,
  height: 80,
  url: 'https://cdn.test/l/logo-small.webp',
}

function durum(
  status: MediaAssetState['status'],
  failureReason: string | null = null,
): MediaAssetState {
  return {
    status,
    failureReason,
    derivatives:
      status === 'ready'
        ? [KUCUK, { ...KUCUK, variant: 'logo-large', width: 1200, height: 300 }]
        : [],
  }
}

function dosya(ad = 'logo.png') {
  return new File([new Uint8Array([1, 2, 3])], ad, { type: 'image/png' })
}

async function panel(role = 'manager', locale: AppLocale = 'tr') {
  vi.mocked(fetchCurrentSession).mockResolvedValue({
    staffId: '0192c000-0000-7000-8000-00000000a001',
    displayName: 'Ayşe Yılmaz',
    role,
    preferredLocale: locale,
    mustChangePassword: false,
    twoFactorEnrolled: true,
  } as CurrentSession)
  render(
    <LocaleProvider locale={locale}>
      <AdminApp view="gallery" />
    </LocaleProvider>,
  )
  await screen.findByRole('navigation')
}

function bolum() {
  return within(screen.getByTestId('gallery-logo'))
}

beforeEach(() => {
  vi.mocked(api.fetchGalleryProfile).mockResolvedValue(GALERI)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
  vi.useRealTimers()
})

describe('0026 — panelin "Logo" bölümü', () => {
  it('0026/AC-19: Logosuz galeride "Logo yükle" seçicisi ve D12\'nin yardım metni; seçici yalnız kabul edilen türleri önerir', async () => {
    await panel()
    await screen.findByTestId('gallery-logo')

    expect(bolum().getByLabelText('Logo yükle').getAttribute('accept')).toBe(
      'image/jpeg,image/png,image/webp',
    )
    expect(bolum().getByText(/Saydam zeminli PNG, en az 640 piksel genişlik önerilir/)).toBeTruthy()
    expect(bolum().queryByTestId('gallery-logo-remove')).toBeNull()
  })

  it('0026/AC-20: Dosya seçilince `kind=logo` ile yüklenir, kimlik hemen okunan sürümle atanır, "işleniyor" görünür; işleme bitince önizleme ekran yeniden açılmadan gelir', async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true })
    vi.mocked(medya.uploadMediaAsset).mockResolvedValue({ id: 'logo-1', status: 'uploaded' })
    vi.mocked(api.assignGalleryLogo).mockResolvedValue(5)
    vi.mocked(medya.fetchMediaAsset)
      .mockResolvedValueOnce(durum('processing'))
      .mockResolvedValue(durum('ready'))
    await panel()
    await screen.findByTestId('gallery-logo')

    fireEvent.change(bolum().getByLabelText('Logo yükle'), { target: { files: [dosya()] } })

    expect(await screen.findByTestId('gallery-logo-processing')).toBeTruthy()
    expect(vi.mocked(medya.uploadMediaAsset).mock.calls[0]![3]).toBe('logo')
    expect(vi.mocked(api.assignGalleryLogo).mock.calls[0]!.slice(0, 2)).toEqual(['logo-1', 4])

    await act(async () => {
      await vi.advanceTimersByTimeAsync(2100)
    })

    expect(await screen.findByTestId('gallery-logo-preview')).toBeTruthy()
    expect(vi.mocked(api.fetchGalleryProfile)).toHaveBeenCalledTimes(1)
  })

  it('0026/AC-21: Hazır logonun önizlemesi `logo-small`; "Değiştir" ve "Kaldır" görünür; kaldırmak sürümle gider', async () => {
    vi.mocked(api.fetchGalleryProfile).mockResolvedValue({ ...GALERI, logoMediaAssetId: 'logo-1' })
    vi.mocked(medya.fetchMediaAsset).mockResolvedValue(durum('ready'))
    vi.mocked(api.removeGalleryLogo).mockResolvedValue(5)
    await panel()

    const onizleme = await screen.findByTestId('gallery-logo-preview')
    expect(onizleme.querySelector('img')!.getAttribute('src')).toBe(KUCUK.url)
    expect(bolum().getByLabelText('Değiştir')).toBeTruthy()

    fireEvent.click(bolum().getByTestId('gallery-logo-remove'))
    await waitFor(() => expect(vi.mocked(api.removeGalleryLogo)).toHaveBeenCalledWith(4, 'tr'))
    expect(await bolum().findByLabelText('Logo yükle')).toBeTruthy()
  })

  it('0026/AC-22: Başarısız logoda çevrilmiş neden ve "Tekrar dene"; tekrar deneme "işleniyor"a döndürür', async () => {
    vi.mocked(api.fetchGalleryProfile).mockResolvedValue({ ...GALERI, logoMediaAssetId: 'logo-1' })
    vi.mocked(medya.fetchMediaAsset)
      .mockResolvedValueOnce(durum('failed', 'undecodable'))
      .mockResolvedValue(durum('processing'))
    vi.mocked(medya.retryMediaAsset).mockResolvedValue()
    await panel()

    const hata = await screen.findByTestId('gallery-logo-failed')
    expect(hata.textContent).toContain(createTranslator('tr')('vehicle.media.failure.undecodable'))

    fireEvent.click(bolum().getByTestId('gallery-logo-retry'))

    expect(await screen.findByTestId('gallery-logo-processing')).toBeTruthy()
    expect(vi.mocked(medya.retryMediaAsset)).toHaveBeenCalledWith('logo-1', 'tr')
  })

  it.each<[number, string, string]>([
    [415, 'media.unsupported_type', 'vehicle.media.problem.unsupported_type'],
    [413, 'media.too_large', 'vehicle.media.problem.too_large'],
  ])(
    '0026/AC-23, AC-24: Reddedilen dosya (%i) çevrilmiş hatayı gösterir; atama yapılmaz',
    async (status, kod, anahtar) => {
      vi.mocked(medya.uploadMediaAsset).mockRejectedValue(new ApiError(kod, null, status))
      await panel()
      await screen.findByTestId('gallery-logo')

      fireEvent.change(bolum().getByLabelText('Logo yükle'), { target: { files: [dosya()] } })

      expect((await screen.findByTestId('gallery-logo-error')).textContent).toBe(
        createTranslator('tr')(anahtar),
      )
      expect(vi.mocked(api.assignGalleryLogo)).not.toHaveBeenCalled()
    },
  )

  it('0026/AC-25, AC-26: Kaydedilmemiş görünen ad değişikliği logo atamasından sonra formda kalır ve kaydetme atamanın döndürdüğü sürümle gider', async () => {
    vi.mocked(medya.uploadMediaAsset).mockResolvedValue({ id: 'logo-1', status: 'uploaded' })
    vi.mocked(api.assignGalleryLogo).mockResolvedValue(5)
    vi.mocked(medya.fetchMediaAsset).mockResolvedValue(durum('ready'))
    vi.mocked(api.saveGalleryProfile).mockResolvedValue(6)
    await panel()
    await screen.findByTestId('gallery-logo')

    const ad = within(screen.getByTestId('gallery-display-names')).getByLabelText('Türkçe')
    fireEvent.change(ad, { target: { value: 'Yeni Görünen Ad' } })
    fireEvent.change(bolum().getByLabelText('Logo yükle'), { target: { files: [dosya()] } })
    await screen.findByTestId('gallery-logo-preview')

    expect(
      within(screen.getByTestId('gallery-display-names')).getByLabelText('Türkçe'),
    ).toHaveProperty('value', 'Yeni Görünen Ad')

    fireEvent.click(screen.getByTestId('gallery-save'))
    await waitFor(() => expect(vi.mocked(api.saveGalleryProfile)).toHaveBeenCalled())
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0]![0]).toBe(5)
  })

  it('0026/AC-73, AC-74 (QA 2 BULGU-1): Profil kaydı dönen sürümü hemen benimser; yeniden okuma dönmeden biten logo ataması yeni sürümle gider, `409` yok; kayıt ve okuma sürerken logo denetimleri kapalı', async () => {
    let yuklemeBitti: (value: { id: string; status: 'uploaded' }) => void = () => {}
    vi.mocked(medya.uploadMediaAsset).mockReturnValue(
      new Promise((resolve) => {
        yuklemeBitti = resolve
      }),
    )
    // Sunucu kuralı: güncel sürüm 5; eski sürümle atama `409`.
    vi.mocked(api.assignGalleryLogo).mockImplementation((_id, surum) =>
      surum === 5
        ? Promise.resolve(6)
        : Promise.reject(new ApiError('dealership.concurrency_conflict', null, 409)),
    )
    vi.mocked(medya.fetchMediaAsset).mockResolvedValue(durum('processing'))
    vi.mocked(api.saveGalleryProfile).mockResolvedValue(5)
    await panel()
    await screen.findByTestId('gallery-logo')

    // Yükleme başlar (henüz dönmedi), bu arada profil kaydedilir.
    fireEvent.change(bolum().getByLabelText('Logo yükle'), { target: { files: [dosya()] } })
    let okumaBitti: (value: AdminGallery) => void = () => {}
    vi.mocked(api.fetchGalleryProfile).mockReturnValue(
      new Promise((resolve) => {
        okumaBitti = resolve
      }),
    )
    fireEvent.click(screen.getByTestId('gallery-save'))
    await waitFor(() => expect(vi.mocked(api.fetchGalleryProfile)).toHaveBeenCalledTimes(2))
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0]![0]).toBe(4)

    // Yeniden okuma bekliyor: seçici kapalı.
    expect((bolum().getByTestId('gallery-logo-file') as HTMLInputElement).disabled).toBe(true)

    await act(async () => {
      yuklemeBitti({ id: 'logo-1', status: 'uploaded' })
    })
    await waitFor(() => expect(vi.mocked(api.assignGalleryLogo)).toHaveBeenCalledTimes(1))
    expect(vi.mocked(api.assignGalleryLogo).mock.calls[0]!.slice(0, 2)).toEqual(['logo-1', 5])
    expect(bolum().queryByTestId('gallery-logo-error')).toBeNull()

    await act(async () => {
      okumaBitti({ ...GALERI, version: 6, logoMediaAssetId: 'logo-1' })
    })
    await waitFor(() =>
      expect((bolum().getByTestId('gallery-logo-file') as HTMLInputElement).disabled).toBe(false),
    )
    expect(bolum().getByTestId('gallery-logo-remove').hasAttribute('disabled')).toBe(false)
  })

  it('0026/AC-73, AC-74 (QA 2 BULGU-1): Profil kaydı ve ardından gelen yeniden okuma sürerken "Değiştir" seçicisi ve "Kaldır" kapalı; okuma dönünce açılır', async () => {
    vi.mocked(api.fetchGalleryProfile).mockResolvedValueOnce({
      ...GALERI,
      logoMediaAssetId: 'logo-1',
    })
    vi.mocked(medya.fetchMediaAsset).mockResolvedValue(durum('ready'))
    let kayitBitti: (value: number) => void = () => {}
    vi.mocked(api.saveGalleryProfile).mockReturnValue(
      new Promise((resolve) => {
        kayitBitti = resolve
      }),
    )
    let okumaBitti: (value: AdminGallery) => void = () => {}
    vi.mocked(api.fetchGalleryProfile).mockReturnValueOnce(
      new Promise((resolve) => {
        okumaBitti = resolve
      }),
    )
    await panel()
    await screen.findByTestId('gallery-logo-preview')
    const secici = () => bolum().getByTestId('gallery-logo-file') as HTMLInputElement
    const kaldir = () => bolum().getByTestId('gallery-logo-remove')
    expect(secici().disabled).toBe(false)

    fireEvent.click(screen.getByTestId('gallery-save'))
    await waitFor(() => expect(secici().disabled).toBe(true))
    expect(kaldir().hasAttribute('disabled')).toBe(true)

    await act(async () => {
      kayitBitti(5)
    })
    await waitFor(() => expect(vi.mocked(api.fetchGalleryProfile)).toHaveBeenCalledTimes(2))
    expect(secici().disabled).toBe(true)
    expect(kaldir().hasAttribute('disabled')).toBe(true)

    await act(async () => {
      okumaBitti({ ...GALERI, version: 5, logoMediaAssetId: 'logo-1' })
    })
    await waitFor(() => expect(secici().disabled).toBe(false))
    expect(kaldir().hasAttribute('disabled')).toBe(false)
  })

  it('0026/AC-17, AC-27, AC-28: Erişim yok, yükleniyor ve hata durumlarında logo bölümü basılmaz', async () => {
    await panel('sales_advisor')
    expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
    expect(screen.queryByTestId('gallery-logo')).toBeNull()
    cleanup()

    vi.mocked(api.fetchGalleryProfile).mockReturnValue(new Promise(() => {}))
    await panel()
    expect(await screen.findByTestId('gallery-loading')).toBeTruthy()
    expect(screen.queryByTestId('gallery-logo')).toBeNull()
    cleanup()

    vi.mocked(api.fetchGalleryProfile).mockRejectedValueOnce(
      new ApiError('api.unavailable', null, 500),
    )
    await panel()
    expect(await screen.findByTestId('gallery-error')).toBeTruthy()
    expect(screen.queryByTestId('gallery-logo')).toBeNull()
  })

  it('0026/AC-29, AC-61: ar panelinde bölüm metinleri Arapça; yedi metin üç dilde panel kataloğunda, vitrin kataloğunda değil', async () => {
    await panel('manager', 'ar')
    await screen.findByTestId('gallery-logo')
    const ar = createTranslator('ar')
    expect(bolum().getByText(ar('dealership.gallery.logo.title')).tagName).toBe('LEGEND')
    expect(ar('dealership.gallery.logo.hint')).toMatch(/\p{Script=Arabic}/u)
    expect(bolum().getByLabelText(ar('dealership.gallery.logo.upload'))).toBeTruthy()

    const anahtarlar = ['title', 'hint', 'upload', 'processing', 'change', 'remove', 'retry'].map(
      (ad) => `dealership.gallery.logo.${ad}`,
    )
    for (const locale of ['tr', 'ar', 'en'] as const) {
      for (const anahtar of anahtarlar) {
        expect(catalogKeys(locale)).toContain(anahtar)
        expect(createTranslator(locale)(anahtar).trim()).not.toBe('')
      }
    }
    expect(Object.keys(vitrinTr).filter((k) => k.includes('logo'))).toEqual([])
  })
})
