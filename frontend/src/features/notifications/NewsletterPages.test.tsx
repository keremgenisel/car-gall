import { act, cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { createTranslator } from '../../shared/i18n/i18n'
import type { AppLocale } from '../../shared/lib/locale'
import { NewsletterConfirmPage } from './NewsletterConfirmPage'
import { NewsletterUnsubscribePage } from './NewsletterUnsubscribePage'
import { SUNUCU, sahteFetch } from './sunucu'

const t = createTranslator('tr')

function sayfa(Sayfa: () => React.ReactElement, adres: string, locale: AppLocale = 'tr') {
  window.history.replaceState(null, '', adres)

  return render(
    <LocaleProvider locale={locale}>
      <Sayfa />
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('0018/AC-27: Teyit bağlantısının açılması aboneliği etkinleştirmez; sayfa "Aboneliği onayla" düğmesini gösterir', () => {
  it('sayfa yüklenince istek GİTMEZ', async () => {
    const fetchMock = sahteFetch(SUNUCU.teyit)
    sayfa(NewsletterConfirmPage, '/tr/newsletter/confirm?token=abc')
    await act(async () => {})

    expect(screen.getByRole('button', { name: t('newsletter.confirm.action') })).toBeTruthy()
    expect(screen.getByRole('heading', { level: 1 }).textContent).toBe(
      t('newsletter.confirm.title'),
    )
    expect(fetchMock).not.toHaveBeenCalled()
  })
})

describe('0018/AC-28: Düğmeye basıldığında kayıt etkin olur ve sayfa başarı mesajını gösterir', () => {
  it('düğme anahtarı gönderir, başarı mesajı', async () => {
    const fetchMock = sahteFetch(SUNUCU.teyit)
    sayfa(NewsletterConfirmPage, '/tr/newsletter/confirm?token=abc-DEF_1')

    fireEvent.click(screen.getByRole('button', { name: t('newsletter.confirm.action') }))

    expect((await screen.findByTestId('newsletter-confirmed')).textContent).toContain(
      t('newsletter.confirmed.title'),
    )

    const [adres, istek] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(adres).toBe('/api/notifications/newsletter/confirmations')
    expect(JSON.parse(istek.body as string)).toEqual({ token: 'abc-DEF_1' })
  })
})

describe('0018/AC-30: Süresi dolmuş bağlantı: "bağlantı artık geçerli değil" mesajı ve yeniden abone olma yolu', () => {
  it('404 geçersiz bağlantı ekranı; yeniden abone ol ana sayfanın bülten bölümüne', async () => {
    sahteFetch(SUNUCU.baglantiGecersiz)
    sayfa(NewsletterConfirmPage, '/tr/newsletter/confirm?token=eski')

    fireEvent.click(screen.getByRole('button', { name: t('newsletter.confirm.action') }))

    const ekran = await screen.findByTestId('newsletter-link-invalid')

    expect(ekran.textContent).toContain(t('newsletter.invalid.title'))
    expect(ekran.textContent).toContain(t('newsletter.invalid.confirmBody'))
    expect(
      screen.getByRole('link', { name: t('newsletter.resubscribe') }).getAttribute('href'),
    ).toBe('/tr/#newsletter')
  })

  it('anahtarsız adres istek göndermeden geçersizdir', async () => {
    const fetchMock = sahteFetch(SUNUCU.teyit)
    sayfa(NewsletterConfirmPage, '/tr/newsletter/confirm')

    expect(await screen.findByTestId('newsletter-link-invalid')).toBeTruthy()
    expect(fetchMock).not.toHaveBeenCalled()
  })
})

describe('0018/AC-36: Teyit sayfası istek sürerken yükleniyor durumunu, sunucu hatasında teknik ayrıntı içermeyen bir hata ve yeniden deneme yolunu gösterir', () => {
  it('yükleniyor: düğme meşgul ve devre dışı', async () => {
    sahteFetch('bekle')
    sayfa(NewsletterConfirmPage, '/tr/newsletter/confirm?token=abc')

    fireEvent.click(screen.getByRole('button', { name: t('newsletter.confirm.action') }))
    await act(async () => {})

    const dugme = screen.getByRole('button', {
      name: t('newsletter.confirm.action'),
    }) as HTMLButtonElement
    expect(dugme.disabled).toBe(true)
    expect(dugme.getAttribute('aria-busy')).toBe('true')
  })

  it('hata ve yeniden dene', async () => {
    const fetchMock = sahteFetch(SUNUCU.hata, SUNUCU.teyit)
    sayfa(NewsletterConfirmPage, '/tr/newsletter/confirm?token=abc')

    fireEvent.click(screen.getByRole('button', { name: t('newsletter.confirm.action') }))

    const hata = await screen.findByTestId('newsletter-confirm-error')
    expect(hata.getAttribute('role')).toBe('alert')
    expect(hata.textContent).not.toMatch(/500|traceId|00-abc/)

    fireEvent.click(screen.getByRole('button', { name: t('newsletter.retry') }))

    expect(await screen.findByTestId('newsletter-confirmed')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})

describe('0018/AC-38: Çıkış bağlantısı açıldığında başka bir düğme ya da onay istemeden kaydı çıkmış yapar ve sayfa "abonelikten çıktınız" mesajını gösterir', () => {
  it('sayfa açılır açılmaz istek gider; düğme yok', async () => {
    const fetchMock = sahteFetch(SUNUCU.cikis)
    sayfa(NewsletterUnsubscribePage, '/tr/newsletter/unsubscribe?token=imzali')

    expect((await screen.findByTestId('newsletter-unsubscribed')).textContent).toContain(
      t('newsletter.unsubscribed.title'),
    )
    expect(screen.queryByRole('button')).toBeNull()

    const [adres, istek] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(adres).toBe('/api/notifications/newsletter/unsubscriptions')
    expect(JSON.parse(istek.body as string)).toEqual({ token: 'imzali' })
  })
})

describe('0018/AC-41: Aynı çıkış bağlantısının ikinci kez açılması aynı mesajı gösterir', () => {
  it('ikinci açılış aynı ekran', async () => {
    sahteFetch(SUNUCU.cikis)
    sayfa(NewsletterUnsubscribePage, '/tr/newsletter/unsubscribe?token=imzali')
    const ilk = (await screen.findByTestId('newsletter-unsubscribed')).textContent
    cleanup()

    sayfa(NewsletterUnsubscribePage, '/tr/newsletter/unsubscribe?token=imzali')

    expect((await screen.findByTestId('newsletter-unsubscribed')).textContent).toBe(ilk)
  })
})

describe('0018/AC-43: Çıkış sayfası ana sayfadaki bülten bölümüne giden bir yeniden abone olma bağlantısı gösterir', () => {
  it.each(['tr', 'ar', 'en'] as const)('%s', async (locale) => {
    sahteFetch(SUNUCU.cikis)
    sayfa(NewsletterUnsubscribePage, `/${locale}/newsletter/unsubscribe?token=imzali`, locale)
    await screen.findByTestId('newsletter-unsubscribed')

    expect(
      screen
        .getByRole('link', { name: createTranslator(locale)('newsletter.resubscribe') })
        .getAttribute('href'),
    ).toBe(`/${locale}/#newsletter`)
  })
})

describe('0018/AC-44: Geçersiz bir çıkış bağlantısında sayfa "bağlantı artık geçerli değil" mesajını gösterir', () => {
  it('404 → geçersiz bağlantı', async () => {
    sahteFetch(SUNUCU.baglantiGecersiz)
    sayfa(NewsletterUnsubscribePage, '/tr/newsletter/unsubscribe?token=bozuk')

    const ekran = await screen.findByTestId('newsletter-link-invalid')

    expect(ekran.textContent).toContain(t('newsletter.invalid.title'))
    expect(ekran.textContent).toContain(t('newsletter.invalid.unsubscribeBody'))
  })
})

describe('0018/AC-45: Çıkış sayfası istek sürerken yükleniyor durumunu, sunucu hatasında teknik ayrıntı içermeyen bir hata ve yeniden deneme yolunu gösterir', () => {
  it('yükleniyor: iskelet', () => {
    sahteFetch('bekle')
    sayfa(NewsletterUnsubscribePage, '/tr/newsletter/unsubscribe?token=imzali')

    expect(screen.getByTestId('newsletter-unsubscribing').getAttribute('aria-busy')).toBe('true')
  })

  it.each([
    ['500', SUNUCU.hata],
    ['ağ', 'ag' as const],
  ])('%s: hata, yeniden dene, sonra başarı', async (_ad, cevap) => {
    const fetchMock = sahteFetch(cevap, SUNUCU.cikis)
    sayfa(NewsletterUnsubscribePage, '/tr/newsletter/unsubscribe?token=imzali')

    const hata = await screen.findByTestId('newsletter-unsubscribe-error')
    expect(hata.textContent).not.toMatch(/500|traceId|00-abc|Failed/)

    fireEvent.click(screen.getByRole('button', { name: t('newsletter.retry') }))

    expect(await screen.findByTestId('newsletter-unsubscribed')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})
