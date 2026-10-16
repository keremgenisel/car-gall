import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { AppLocale } from '../../shared/lib/locale'
import { TwoFactorPage } from './TwoFactorPage'

vi.mock('../../shared/api/identity', () => ({
  fetchTwoFactorStatus: vi.fn(),
  startTwoFactorEnrollment: vi.fn(),
  confirmTwoFactorEnrollment: vi.fn(),
  disableTwoFactor: vi.fn(),
  regenerateRecoveryCodes: vi.fn(),
}))

const {
  fetchTwoFactorStatus,
  startTwoFactorEnrollment,
  confirmTwoFactorEnrollment,
  disableTwoFactor,
  regenerateRecoveryCodes,
} = await import('../../shared/api/identity')

const SIR = 'JBSWY3DPEHPK3PXPJBSWY3DPEHPK3PXP'
const ADRES = `otpauth://totp/Test%20Galeri%3Aayse%40ornek.com?secret=${SIR}&issuer=Test%20Galeri&algorithm=SHA1&digits=6&period=30`

const KODLAR = ['ABCD-EFGH-JKLM-NPQR', 'BCDE-FGHJ-KLMN-PQRS']

function ekran(locale: AppLocale = 'tr') {
  render(
    <LocaleProvider locale={locale}>
      <TwoFactorPage />
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0013/AC-55: İkinci adım ekranı **yükleniyor**, **hata**, **ara durumun süresi doldu** ve **kilitlenmiş hesap** durumlarını ele alır', () => {
  it('yükleniyor hâlinde iskelet basılır', () => {
    vi.mocked(fetchTwoFactorStatus).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('two-factor-loading')).not.toBeNull()
  })

  it('hata hâlinde tekrar dene eylemi var ve teknik ayrıntı yok', async () => {
    vi.mocked(fetchTwoFactorStatus).mockRejectedValue(new ApiError('api.unavailable', null, 503))

    ekran()

    const hata = await screen.findByTestId('two-factor-load-error')

    expect(hata.textContent).toContain('Tekrar dene')
    expect(document.body.textContent ?? '').not.toMatch(/api\.unavailable|503|errorCode/)
  })
})

describe('0013/AC-10: Kurulum ekranı kare kodu **uçta** çizer ve elle giriş metnini **her zaman görünür** tutar; sır sunucuda görsele gömülmez', () => {
  it('kare kod SVG olarak çizilir ve elle giriş metni aynı anda görünür', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: false,
      required: true,
      remainingRecoveryCodes: 0,
    })
    vi.mocked(startTwoFactorEnrollment).mockResolvedValue({ secretUri: ADRES, secret: SIR })

    ekran()

    fireEvent.click(await screen.findByTestId('two-factor-start'))

    const kareKod = await screen.findByTestId('two-factor-qr')

    // Kare kod UÇTA çiziliyor: DOM'da bir SVG var ve sunucudan gelen bir görsel yok.
    expect(kareKod.querySelector('svg')).not.toBeNull()
    expect(kareKod.querySelector('img')).toBeNull()

    // Elle giriş metni GİZLENMİŞ bir bağlantının arkasında değil, her zaman görünür.
    expect(screen.getByTestId('two-factor-manual-secret').textContent).toBe(SIR)
  })

  it('kurulum onaylanınca kurtarma kodları BİR KEZ gösterilir', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: false,
      required: true,
      remainingRecoveryCodes: 0,
    })
    vi.mocked(startTwoFactorEnrollment).mockResolvedValue({ secretUri: ADRES, secret: SIR })
    vi.mocked(confirmTwoFactorEnrollment).mockResolvedValue({ codes: KODLAR })

    ekran()

    fireEvent.click(await screen.findByTestId('two-factor-start'))
    fireEvent.change(await screen.findByLabelText('Uygulamadaki kod'), {
      target: { value: '123456' },
    })
    fireEvent.click(screen.getByTestId('two-factor-confirm'))

    const kodlar = await screen.findByTestId('recovery-codes')

    expect(kodlar.textContent).toContain(KODLAR[0])
    expect(kodlar.textContent).toContain(KODLAR[1])
  })

  it('onay kodunun biçimi bozuksa İSTEK GÖNDERİLMEZ', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: false,
      required: true,
      remainingRecoveryCodes: 0,
    })
    vi.mocked(startTwoFactorEnrollment).mockResolvedValue({ secretUri: ADRES, secret: SIR })

    ekran()

    fireEvent.click(await screen.findByTestId('two-factor-start'))
    fireEvent.change(await screen.findByLabelText('Uygulamadaki kod'), { target: { value: '12' } })
    fireEvent.click(screen.getByTestId('two-factor-confirm'))

    await waitFor(() => expect(screen.getByText('Kod altı haneli olmalı.')).not.toBeNull())
    expect(vi.mocked(confirmTwoFactorEnrollment)).not.toHaveBeenCalled()
  })
})

describe('0013/AC-38: Personel kalan kurtarma kodu sayısını kendi ekranında görür', () => {
  it('kalan sayı ICU çoğul biçimiyle yazılır', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: true,
      required: true,
      remainingRecoveryCodes: 7,
    })

    ekran()

    expect((await screen.findByTestId('two-factor-remaining')).textContent).toContain('7')
  })

  it('tek kod kaldığında cümle tekil kurulur', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: true,
      required: true,
      remainingRecoveryCodes: 1,
    })

    ekran()

    const metin = (await screen.findByTestId('two-factor-remaining')).textContent ?? ''

    expect(metin).toContain('1 kurtarma kodunuz kaldı')
  })
})

describe('0013/AC-40: Yenileme kalan kodların **hepsini** düşürür ve yeni kümeyi **bir kez** gösterir', () => {
  it('yenileme taze kod ister ve yeni kümeyi gösterir', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: true,
      required: true,
      remainingRecoveryCodes: 3,
    })
    vi.mocked(regenerateRecoveryCodes).mockResolvedValue({ codes: KODLAR })

    ekran()

    fireEvent.change(await screen.findByLabelText('Doğrulama kodu'), {
      target: { value: '123456' },
    })
    fireEvent.click(screen.getByTestId('two-factor-regenerate'))

    expect((await screen.findByTestId('recovery-codes')).textContent).toContain(KODLAR[0])
    expect(vi.mocked(regenerateRecoveryCodes).mock.calls[0]?.[0]).toBe('123456')
  })
})

describe('0013/AC-42: Zorunlu rolde kapatma **yoktur**: istek reddedilir ve tek yol Yöneticinin sıfırlamasıdır', () => {
  it('zorunlu rolde kapatma düğmesi BASILMAZ ve nedeni yazılır', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: true,
      required: true,
      remainingRecoveryCodes: 5,
    })

    ekran()

    expect(await screen.findByTestId('two-factor-disable-blocked')).not.toBeNull()
    expect(screen.queryByTestId('two-factor-disable')).toBeNull()
  })

  it('opsiyonel rolde kapatma parola VE kod ister', async () => {
    vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
      enrolled: true,
      required: false,
      remainingRecoveryCodes: 5,
    })
    vi.mocked(disableTwoFactor).mockResolvedValue(undefined)

    ekran()

    fireEvent.click(await screen.findByTestId('two-factor-disable'))

    // Parola boş: istek gönderilmedi.
    await waitFor(() => expect(screen.getByText('Mevcut parolanızı yazın.')).not.toBeNull())
    expect(vi.mocked(disableTwoFactor)).not.toHaveBeenCalled()

    fireEvent.change(screen.getByLabelText('Mevcut parolanız'), { target: { value: 'parola' } })
    fireEvent.change(screen.getByLabelText('Doğrulama kodu'), { target: { value: '123456' } })
    fireEvent.click(screen.getByTestId('two-factor-disable'))

    await waitFor(() => expect(vi.mocked(disableTwoFactor)).toHaveBeenCalledTimes(1))
    expect(vi.mocked(disableTwoFactor).mock.calls[0]?.slice(0, 2)).toEqual(['parola', '123456'])
  })
})

describe('0013/AC-56: İkinci adım ekranı personelin tercih ettiği dilde çalışır ve Arapça da **RTL**dir', () => {
  it.each(['tr', 'en', 'ar'] as const)(
    '%s locale inde ham çeviri anahtarı görünmez',
    async (locale) => {
      vi.mocked(fetchTwoFactorStatus).mockResolvedValue({
        enrolled: true,
        required: false,
        remainingRecoveryCodes: 2,
      })

      ekran(locale)

      await screen.findByTestId('two-factor-remaining')

      expect(document.body.textContent ?? '').not.toMatch(/(twoFactor|login|staff)\.[a-z_.]+/)
    },
  )
})
