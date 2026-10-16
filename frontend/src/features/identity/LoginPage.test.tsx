import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { LoginPage } from './LoginPage'
import type { AppLocale } from '../../shared/lib/locale'

vi.mock('../../shared/api/identity', () => ({
  login: vi.fn(),
  verifyTwoFactor: vi.fn(),
  discardPendingLogin: vi.fn(),
}))

const { login, verifyTwoFactor, discardPendingLogin } = await import('../../shared/api/identity')

const ETIKET = {
  tr: { kimlik: 'E-posta', parola: 'Parola', gonder: 'Giriş yap' },
  en: { kimlik: 'Email', parola: 'Password', gonder: 'Sign in' },
  ar: { kimlik: 'البريد الإلكتروني', parola: 'كلمة المرور', gonder: 'تسجيل الدخول' },
} as const

function ekran(locale: AppLocale = 'tr', onSignedIn = vi.fn()) {
  render(
    <LocaleProvider locale={locale}>
      <LoginPage onSignedIn={onSignedIn} />
    </LocaleProvider>,
  )

  return onSignedIn
}

function doldur(locale: AppLocale = 'tr') {
  fireEvent.change(screen.getByLabelText(ETIKET[locale].kimlik), {
    target: { value: 'ayse@ornek.com' },
  })
  fireEvent.change(screen.getByLabelText(ETIKET[locale].parola), {
    target: { value: 'cok-guclu-parola' },
  })
}

function gonder(locale: AppLocale = 'tr') {
  fireEvent.click(screen.getByRole('button', { name: ETIKET[locale].gonder }))
}

afterEach(() => {
  // vitest globals kapalı; Testing Library'nin otomatik cleanup'ı koşmaz.
  cleanup()
  vi.clearAllMocks()
})

describe('AC-56: Giriş yapılırken giriş düğmesi yükleniyor hâline geçer ve form iki kez gönderilemez', () => {
  it('gönderim sırasında düğme devre dışı ve aria-busy olur', async () => {
    vi.mocked(login).mockReturnValue(new Promise(() => {}))

    ekran()
    doldur()
    gonder()

    const dugme = await screen.findByRole('button', { name: 'Giriş yapılıyor' })

    expect(dugme).toHaveProperty('disabled', true)
    expect(dugme.getAttribute('aria-busy')).toBe('true')
  })

  it('ikinci tıklama yeni istek üretmez', async () => {
    vi.mocked(login).mockReturnValue(new Promise(() => {}))

    ekran()
    doldur()
    gonder()

    const dugme = await screen.findByRole('button', { name: 'Giriş yapılıyor' })
    fireEvent.click(dugme)
    fireEvent.click(dugme)

    expect(vi.mocked(login)).toHaveBeenCalledTimes(1)
  })
})

describe('AC-57: Giriş başarısız olduğunda ekran çeviri kataloğundaki hata mesajını gösterir; girilen kimlik kaybolmaz, parola alanı temizlenir', () => {
  it('hata mesajı çevrilmiş gelir, kimlik kalır, parola temizlenir', async () => {
    vi.mocked(login).mockRejectedValue(new ApiError('identity.invalid_credentials', null, 401))

    ekran()
    doldur()
    gonder()

    expect(await screen.findByTestId('login-error')).toBeTruthy()
    expect(screen.getByText(/E-posta veya parola hatalı/)).toBeTruthy()

    expect(screen.getByLabelText('E-posta')).toHaveProperty('value', 'ayse@ornek.com')
    expect(screen.getByLabelText('Parola')).toHaveProperty('value', '')
  })
})

describe('AC-58: Hesap kilitlendiğinde ekran, ne olduğunu ve ne zaman tekrar denenebileceğini söyler', () => {
  it('eşik kadar başarısız denemeden sonra kilitlenme metni çıkar', async () => {
    vi.mocked(login).mockRejectedValue(new ApiError('identity.invalid_credentials', null, 401))

    ekran()

    for (let deneme = 0; deneme < 5; deneme++) {
      doldur()
      gonder()
      await screen.findByTestId('login-error')
    }

    expect(await screen.findByTestId('login-locked')).toBeTruthy()
    expect(screen.getByText(/geçici olarak giriş denemesine kapandı/)).toBeTruthy()
  })
})

describe('AC-59: Hiçbir hata durumunda HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('teknik ayrıntı ekrana basılmaz', async () => {
    vi.mocked(login).mockRejectedValue(
      new ApiError('identity.invalid_credentials', 'trace-abc-123', 401),
    )

    ekran()
    doldur()
    gonder()

    await screen.findByTestId('login-error')
    const metin = document.body.textContent ?? ''

    for (const isaret of [
      '401',
      'identity.invalid_credentials',
      'trace-abc-123',
      '/api/identity',
    ]) {
      expect(metin).not.toContain(isaret)
    }
  })
})

describe('AC-61: Giriş ekranındaki tüm metinler TR, AR ve EN locale lerinde kendi dilinde görünür', () => {
  it.each<[AppLocale]>([['tr'], ['en'], ['ar']])('%s locale: etiketler kendi dilinde', (locale) => {
    ekran(locale)

    expect(screen.getByLabelText(ETIKET[locale].kimlik)).toBeTruthy()
    expect(screen.getByRole('button', { name: ETIKET[locale].gonder })).toBeTruthy()
  })

  it('hiçbir locale de ham çeviri anahtarı görünmez', () => {
    for (const locale of ['tr', 'en', 'ar'] as const) {
      cleanup()
      ekran(locale)

      expect(document.body.textContent ?? '').not.toMatch(/(login|admin|staff|password)\.[a-z_.]+/)
    }
  })
})

describe('AC-1: Geçerli kimlik ve parolayla giriş yapan personel yönetim paneline erişir', () => {
  it('başarılı girişte oturum yukarı bildirilir', async () => {
    // 0013 D15/D43 — yanıt artık AYRIŞTIRICILI; tamamlanmış dal bugünkü gövdeyi
    // `session` altında taşıyor ve kriterin ölçtüğü şey (oturumun yukarı bildirilmesi)
    // değişmedi.
    vi.mocked(login).mockResolvedValue({
      status: 'completed',
      session: {
        staffId: '0192c000-0000-7000-8000-00000000a001',
        displayName: 'Ayşe Yılmaz',
        role: 'manager',
        preferredLocale: 'tr',
        mustChangePassword: false,
        twoFactorEnrolled: false,
      },
    })

    const onSignedIn = ekran()
    doldur()
    gonder()

    await waitFor(() => expect(onSignedIn).toHaveBeenCalledTimes(1))
  })
})

/**
 * `0013` — ikinci adım giriş ekranının bir FAZI (R14/D14). Bu blok fazlar arası geçişi
 * ve D21'in **tek sayacını** ölçüyor; ikisi de bileşenin değil AKIŞIN davranışı.
 */
describe('0013/AC-19: İkinci adım giriş ekranının bir **fazıdır**: adım geçilmeden panele gezinme olmaz', () => {
  it('bekleyen dalda ekran ikinci adım fazına geçer ve oturum YUKARI BİLDİRİLMEZ', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })

    const onSignedIn = ekran()
    doldur()
    gonder()

    expect(await screen.findByLabelText('Doğrulama kodu')).not.toBeNull()
    expect(onSignedIn).not.toHaveBeenCalled()

    // Parola alanı artık ekranda değil: faz değişti, ikinci bir ekran açılmadı.
    expect(screen.queryByLabelText('Parola')).toBeNull()
  })

  it('kod doğrulanınca oturum yukarı bildirilir', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    vi.mocked(verifyTwoFactor).mockResolvedValue({
      status: 'completed',
      session: {
        staffId: '0192c000-0000-7000-8000-00000000a001',
        displayName: 'Ayşe Yılmaz',
        role: 'manager',
        preferredLocale: 'tr',
        mustChangePassword: false,
        twoFactorEnrolled: true,
      },
    })

    const onSignedIn = ekran()
    doldur()
    gonder()

    fireEvent.change(await screen.findByLabelText('Doğrulama kodu'), {
      target: { value: '123456' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))

    await waitFor(() => expect(onSignedIn).toHaveBeenCalledTimes(1))
    expect(vi.mocked(verifyTwoFactor).mock.calls[0]?.[0]).toBe('123456')
  })

  it('kurulum gereken dalda kabuğa gidilir: kurulum ekranını kabuk basar', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_setup_required' })

    const onSignedIn = ekran()
    doldur()
    gonder()

    await waitFor(() => expect(onSignedIn).toHaveBeenCalledTimes(1))
  })
})

describe('0013/AC-28: Kilitlenme ipucunun deneme sayacı iki fazda **tektir**: parola fazındaki ve kod fazındaki başarısız denemeler aynı sayaca eklenir', () => {
  it('üç parola + iki kod hatasından sonra kilitlenme ipucu basılır', async () => {
    vi.mocked(login).mockRejectedValue(new ApiError('identity.invalid_credentials', null, 401))

    ekran()

    for (let deneme = 0; deneme < 3; deneme += 1) {
      doldur()
      gonder()
      await screen.findByTestId('login-error')
    }

    // Üç denemede ipucu YOK: eşik beş.
    expect(screen.queryByTestId('login-locked')).toBeNull()

    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    doldur()
    gonder()

    const kod = await screen.findByLabelText('Doğrulama kodu')

    vi.mocked(verifyTwoFactor).mockRejectedValue(
      new ApiError('identity.two_factor_invalid', null, 401),
    )

    for (let deneme = 0; deneme < 2; deneme += 1) {
      fireEvent.change(kod, { target: { value: '000000' } })
      fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))
      await waitFor(() => expect(vi.mocked(verifyTwoFactor)).toHaveBeenCalledTimes(deneme + 1))
    }

    // Sayaç PAYLAŞILIYOR: parola fazındaki üç deneme + kod fazındaki iki deneme = eşik.
    expect(await screen.findByTestId('login-locked')).not.toBeNull()
  })
})

describe('0013/AC-17: *"Kullanıcı değiştir"* ara durumu **siler**; silinmiş ara durumla yapılan doğrulama reddedilir', () => {
  it('kullanıcı değiştir ara durumu silip parola fazına döner', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    vi.mocked(discardPendingLogin).mockResolvedValue(undefined)

    ekran()
    doldur()
    gonder()

    fireEvent.click(await screen.findByTestId('two-factor-switch-user'))

    await waitFor(() => expect(vi.mocked(discardPendingLogin)).toHaveBeenCalledTimes(1))
    expect(await screen.findByLabelText('Parola')).not.toBeNull()
  })
})

describe('0013/AC-55: İkinci adım ekranı **yükleniyor**, **hata**, **ara durumun süresi doldu** ve **kilitlenmiş hesap** durumlarını ele alır', () => {
  it('ara durum düşünce PAROLA fazına dönülür ve sebebi orada yazılır', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    vi.mocked(verifyTwoFactor).mockRejectedValue(
      new ApiError('identity.two_factor_pending_expired', null, 401),
    )

    ekran()
    doldur()
    gonder()

    fireEvent.change(await screen.findByLabelText('Doğrulama kodu'), {
      target: { value: '123456' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))

    // Parola fazı geri geldi ve mesaj orada: "taze kod yaz" ile "parolanı yeniden gir"
    // ayrı şeylerdir (D20).
    expect(await screen.findByLabelText('Parola')).not.toBeNull()
    expect(screen.getByTestId('login-error').textContent).toContain('Parolanızı yeniden girip')
  })

  it('kod hatasında faz KORUNUR ve taze kod istenir', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    vi.mocked(verifyTwoFactor).mockRejectedValue(
      new ApiError('identity.two_factor_invalid', null, 401),
    )

    ekran()
    doldur()
    gonder()

    fireEvent.change(await screen.findByLabelText('Doğrulama kodu'), {
      target: { value: '123456' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))

    expect((await screen.findByTestId('login-error')).textContent).toContain('taze kod')
    expect(screen.getByLabelText('Doğrulama kodu')).not.toBeNull()
  })

  it('yükleniyor hâlinde düğme devre dışı ve ekranda ham kod yok', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    vi.mocked(verifyTwoFactor).mockReturnValue(new Promise(() => {}))

    ekran()
    doldur()
    gonder()

    fireEvent.change(await screen.findByLabelText('Doğrulama kodu'), {
      target: { value: '123456' },
    })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))

    const dugme = await screen.findByRole('button', { name: 'Doğrulanıyor' })

    expect(dugme).toHaveProperty('disabled', true)
    expect(document.body.textContent ?? '').not.toMatch(/identity\.|errorCode|401/)
  })
})

describe('0013/AC-36: Kodunu üretemeyen personel bir kurtarma koduyla giriş yapar; kullanılan kod bir daha kabul edilmez', () => {
  it('kurtarma kodu moduna geçilir ve altı hane kuralı uygulanmaz', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })
    vi.mocked(verifyTwoFactor).mockResolvedValue({
      status: 'completed',
      session: {
        staffId: '0192c000-0000-7000-8000-00000000a001',
        displayName: 'Ayşe Yılmaz',
        role: 'manager',
        preferredLocale: 'tr',
        mustChangePassword: false,
        twoFactorEnrolled: true,
      },
    })

    ekran()
    doldur()
    gonder()

    fireEvent.click(await screen.findByTestId('two-factor-toggle-input'))

    const alan = screen.getByLabelText('Kurtarma kodu')

    fireEvent.change(alan, { target: { value: 'ABCD-EFGH-JKLM-NPQR' } })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))

    await waitFor(() => expect(vi.mocked(verifyTwoFactor)).toHaveBeenCalledTimes(1))
    expect(vi.mocked(verifyTwoFactor).mock.calls[0]?.[0]).toBe('ABCD-EFGH-JKLM-NPQR')
  })

  it('kod fazında biçimi bozuk değer için İSTEK GÖNDERİLMEZ', async () => {
    vi.mocked(login).mockResolvedValue({ status: 'two_factor_required' })

    ekran()
    doldur()
    gonder()

    fireEvent.change(await screen.findByLabelText('Doğrulama kodu'), { target: { value: '123' } })
    fireEvent.click(screen.getByRole('button', { name: 'Doğrula' }))

    await waitFor(() => expect(screen.getByText('Kod altı haneli olmalı.')).not.toBeNull())
    expect(vi.mocked(verifyTwoFactor)).not.toHaveBeenCalled()
  })
})
