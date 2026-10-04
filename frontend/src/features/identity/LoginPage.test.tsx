import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { LoginPage } from './LoginPage'
import type { AppLocale } from '../../shared/lib/locale'

vi.mock('../../shared/api/identity', () => ({ login: vi.fn() }))

const { login } = await import('../../shared/api/identity')

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
    vi.mocked(login).mockResolvedValue({
      staffId: '0192c000-0000-7000-8000-00000000a001',
      displayName: 'Ayşe Yılmaz',
      role: 'manager',
      preferredLocale: 'tr',
      mustChangePassword: false,
    })

    const onSignedIn = ekran()
    doldur()
    gonder()

    await waitFor(() => expect(onSignedIn).toHaveBeenCalledTimes(1))
  })
})
