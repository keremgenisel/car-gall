import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { createTranslator } from '../../shared/i18n/i18n'
import { NewsletterSection } from './NewsletterSection'
import { SUNUCU, sahteFetch } from './sunucu'

const t = createTranslator('tr')

function bolum(privacyHref: string | null = null) {
  return render(
    <LocaleProvider locale="tr">
      <NewsletterSection privacyHref={privacyHref} />
    </LocaleProvider>,
  )
}

function yaz(adres: string) {
  fireEvent.change(screen.getByLabelText(t('newsletter.email')), { target: { value: adres } })
}

function gonder() {
  fireEvent.click(screen.getByRole('button', { name: t('newsletter.submit') }))
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

describe('0018/AC-1: Ana sayfada bülten bölümü: başlık, haftalık özet açıklaması, e-posta alanı, gönder düğmesi ve tek tıkla çıkış notu', () => {
  it('bölümün beş parçası vitrin kataloğundan', () => {
    bolum()

    const bolumOgesi = screen.getByTestId('newsletter-section')

    expect(screen.getByRole('heading', { level: 2, name: t('newsletter.title') })).toBeTruthy()
    expect(bolumOgesi.textContent).toContain(t('newsletter.body'))
    expect(screen.getByLabelText(t('newsletter.email')).getAttribute('type')).toBe('email')
    expect(screen.getByRole('button', { name: t('newsletter.submit') })).toBeTruthy()
    expect(bolumOgesi.textContent).toContain(t('newsletter.note'))
    expect(bolumOgesi.id).toBe('newsletter')
  })
})

describe('0018/AC-3: `privacy` o dilde yayındayken nottaki KVKK bağlantısı `/{locale}/{privacy slug}` adresine gider', () => {
  it('bağlantı çağıranın verdiği adrese gider', () => {
    bolum('/tr/kvkk-aydinlatma')

    const baglanti = screen.getByTestId('newsletter-privacy')

    expect(baglanti.getAttribute('href')).toBe('/tr/kvkk-aydinlatma')
    expect(baglanti.textContent).toBe(t('newsletter.privacy'))
  })
})

describe('0018/AC-4: `privacy` o dilde yayında değilken notta bağlantı yoktur ve form abonelik isteği göndermeye devam eder', () => {
  it('bağlantı yok, istek gider', async () => {
    const fetchMock = sahteFetch(SUNUCU.kabul)
    bolum(null)

    expect(screen.queryByTestId('newsletter-privacy')).toBeNull()

    yaz('ali@ornek.com')
    gonder()

    expect(await screen.findByTestId('newsletter-success')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})

describe("0018/AC-5: Gönder'e basıldıktan sonra yanıt gelene kadar düğme yükleniyor durumunu gösterir ve devre dışıdır; bu sürede ikinci basış ikinci istek göndermez", () => {
  it('yükleniyor, devre dışı, tek istek', async () => {
    const fetchMock = sahteFetch('bekle')
    bolum()

    yaz('ali@ornek.com')
    gonder()

    const dugme = await screen.findByRole('button', { name: t('newsletter.submitting') })

    expect((dugme as HTMLButtonElement).disabled).toBe(true)
    expect(dugme.getAttribute('aria-busy')).toBe('true')

    fireEvent.click(dugme)
    fireEvent.submit(dugme.closest('form')!)

    expect(fetchMock).toHaveBeenCalledTimes(1)
  })
})

describe('0018/AC-6: Geçerli bir adresle gönderimden sonra form yerinde başarı mesajını gösterir: teyit bağlantısının gönderildiği ve gelmezse istenmeyen e-posta klasörüne bakılması', () => {
  it('form başarı mesajına döner; isteğin gövdesi adres ve dil', async () => {
    const fetchMock = sahteFetch(SUNUCU.kabul)
    bolum()

    yaz('ali@ornek.com')
    gonder()

    expect((await screen.findByTestId('newsletter-success')).textContent).toBe(
      t('newsletter.success'),
    )
    expect(screen.queryByLabelText(t('newsletter.email'))).toBeNull()

    const [adres, istek] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(adres).toBe('/api/notifications/newsletter/subscriptions')
    expect(JSON.parse(istek.body as string)).toEqual({ email: 'ali@ornek.com', locale: 'tr' })
  })
})

describe('0018/AC-7: `ali@` ve boş adres alan hatasıyla reddedilir; API `400` ve `notification.subscription_email_invalid` döner', () => {
  it('sunucunun 400 cevabı alanı işaretler; adres alanda kalır', async () => {
    sahteFetch(SUNUCU.adresGecersiz)
    bolum()

    yaz('ali@')
    gonder()

    const alan = screen.getByLabelText(t('newsletter.email'))

    await waitFor(() => expect(alan.getAttribute('aria-invalid')).toBe('true'))
    expect(screen.getByText(t('newsletter.emailInvalid'))).toBeTruthy()
    expect((alan as HTMLInputElement).value).toBe('ali@')
  })

  it('boş adres istek göndermeden işaretlenir', () => {
    const fetchMock = sahteFetch(SUNUCU.kabul)
    bolum()

    gonder()

    expect(screen.getByLabelText(t('newsletter.email')).getAttribute('aria-invalid')).toBe('true')
    expect(fetchMock).not.toHaveBeenCalled()
  })
})

describe('0018/AC-11: Sunucu `500` döndüğünde ya da ağ koptuğunda form teknik ayrıntı içermeyen bir hata ve yeniden deneme yolu gösterir; yazılan adres alanda kalır', () => {
  it.each([
    ['500', SUNUCU.hata],
    ['ağ', 'ag' as const],
  ])('%s: hata metni, adres yerinde, yeniden gönderilebilir', async (_ad, cevap) => {
    const fetchMock = sahteFetch(cevap, SUNUCU.kabul)
    bolum()

    yaz('ali@ornek.com')
    gonder()

    const hata = await screen.findByTestId('newsletter-error')

    expect(hata.textContent).toBe(t('newsletter.failed'))
    expect(hata.textContent).not.toMatch(/500|traceId|00-abc|notification\./)
    expect((screen.getByLabelText(t('newsletter.email')) as HTMLInputElement).value).toBe(
      'ali@ornek.com',
    )

    gonder()

    expect(await screen.findByTestId('newsletter-success')).toBeTruthy()
    expect(fetchMock).toHaveBeenCalledTimes(2)
  })
})

describe('0018/AC-12: Hız sınırına takılan istek `429` ve `notification.subscription_rate_limited` alır ve form hız sınırı mesajı gösterir (D9)', () => {
  it('hız sınırı mesajı', async () => {
    sahteFetch(SUNUCU.hizSiniri)
    bolum()

    yaz('ali@ornek.com')
    gonder()

    expect((await screen.findByTestId('newsletter-error')).textContent).toBe(
      t('newsletter.rateLimited'),
    )
  })
})
