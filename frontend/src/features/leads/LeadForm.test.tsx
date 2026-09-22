import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { LeadForm } from './LeadForm'
import type { AppLocale } from '../../shared/lib/locale'

/**
 * docs/frontend.md § 8.1 — ekran gerçek API client'ı sahtelenmiş hâlde monte edilir ve
 * ÜÇ DURUMUN ÜÇÜ de doğrulanır: yükleniyor (gönderiliyor), başarılı, hata.
 */
vi.mock('../../shared/api/leads', () => ({
  captureLead: vi.fn(),
}))

const { captureLead } = await import('../../shared/api/leads')

const SLUG = 'toyota-corolla-2019-0192c0'

function ekran(locale: AppLocale = 'tr', isReserved = false) {
  return render(
    <LocaleProvider locale={locale}>
      <LeadForm listingSlug={SLUG} isReserved={isReserved} />
    </LocaleProvider>,
  )
}

function doldur(ad = 'Ayşe Yılmaz', telefon = '+905321112233') {
  fireEvent.change(screen.getByLabelText('Adınız'), { target: { value: ad } })
  fireEvent.change(screen.getByLabelText('Telefon'), { target: { value: telefon } })
}

function gonder() {
  fireEvent.click(screen.getByRole('button', { name: 'Talebimi gönder' }))
}

afterEach(() => {
  // vitest globals kapalı olduğu için Testing Library'nin otomatik cleanup'ı koşmaz.
  cleanup()
  vi.clearAllMocks()
})

describe('AC-11: Rezerve durumundaki bir ilanın detay sayfasında talep formu gösterilmez', () => {
  it('rezerve ilanda form basılmaz, yerinde neden kapalı olduğu yazar', () => {
    ekran('tr', true)

    expect(screen.queryByTestId('lead-form')).toBeNull()
    expect(screen.getByTestId('lead-closed')).toBeTruthy()
    expect(screen.getByText('Bu araç için talep alınmıyor')).toBeTruthy()
  })

  it('rezerve olmayan ilanda form basılır', () => {
    ekran()

    expect(screen.getByTestId('lead-form')).toBeTruthy()
    expect(screen.queryByTestId('lead-closed')).toBeNull()
  })
})

describe('AC-26: Talep formunda iletişim izni kutusu önceden işaretli gelmez', () => {
  it('izin kutusu işaretsiz açılır', () => {
    ekran()

    expect(screen.getByRole('checkbox')).toHaveProperty('checked', false)
  })
})

describe('AC-27: İzin kutusunun yanında neye izin verildiğini söyleyen metin bulunur', () => {
  it.each<[AppLocale, string]>([
    ['tr', 'İletişim iznimi veriyorum'],
    ['en', 'I give my contact consent'],
    ['ar', 'أوافق على التواصل معي'],
  ])('%s locale: izin metni kendi dilinde görünür', (locale, etiket) => {
    ekran(locale)

    expect(screen.getByText(etiket)).toBeTruthy()
    // Metnin kendisi kutunun yanında ve kutuya bağlı: `aria-describedby`.
    expect(screen.getByRole('checkbox').getAttribute('aria-describedby')).toBeTruthy()
  })
})

describe('AC-36: Talep gönderilirken form gönderim düğmesi yükleniyor hâline geçer ve aynı talep iki kez gönderilemez', () => {
  it('gönderim sırasında düğme devre dışı ve aria-busy olur', async () => {
    vi.mocked(captureLead).mockReturnValue(new Promise(() => {}))

    ekran()
    doldur()
    gonder()

    const dugme = await screen.findByRole('button', { name: 'Gönderiliyor' })

    expect(dugme).toHaveProperty('disabled', true)
    expect(dugme.getAttribute('aria-busy')).toBe('true')
  })

  it('gönderim sırasında ikinci tıklama yeni istek üretmez', async () => {
    vi.mocked(captureLead).mockReturnValue(new Promise(() => {}))

    ekran()
    doldur()
    gonder()

    const dugme = await screen.findByRole('button', { name: 'Gönderiliyor' })
    fireEvent.click(dugme)
    fireEvent.click(dugme)

    expect(vi.mocked(captureLead)).toHaveBeenCalledTimes(1)
  })
})

describe('AC-37: Talep başarıyla kaydedildiğinde ziyaretçiye talebinin alındığı bildirilir', () => {
  it('başarı ekranı talebin alındığını söyler', async () => {
    vi.mocked(captureLead).mockResolvedValue({ leadId: '0192c000-0000-7000-8000-00000000aaaa' })

    ekran()
    doldur()
    gonder()

    expect(await screen.findByTestId('lead-success')).toBeTruthy()
    expect(screen.getByText('Talebiniz alındı')).toBeTruthy()
  })

  it('AC-35: aynı idempotentlik anahtarı formun ömrü boyunca değişmez', async () => {
    vi.mocked(captureLead).mockRejectedValueOnce(new ApiError('api.unavailable', null))
    vi.mocked(captureLead).mockResolvedValueOnce({ leadId: 'x' })

    ekran()
    doldur()
    gonder()

    await screen.findByTestId('lead-error')
    fireEvent.click(screen.getByRole('button', { name: 'Tekrar dene' }))
    gonder()

    await waitFor(() => expect(vi.mocked(captureLead)).toHaveBeenCalledTimes(2))

    const ilkAnahtar = vi.mocked(captureLead).mock.calls[0][2]
    const ikinciAnahtar = vi.mocked(captureLead).mock.calls[1][2]

    expect(ikinciAnahtar).toBe(ilkAnahtar)
  })
})

describe('AC-38: Talep kaydedilemediğinde ekran hata mesajını ve tekrar dene eylemini gösterir', () => {
  it('hata mesajı çeviri kataloğundan gelir ve girilen bilgiler kaybolmaz', async () => {
    vi.mocked(captureLead).mockRejectedValue(new ApiError('api.unavailable', null))

    ekran()
    doldur('Mehmet Demir', '+905321112233')
    gonder()

    expect(await screen.findByTestId('lead-error')).toBeTruthy()
    expect(screen.getByText('Talebiniz gönderilemedi')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()

    // Girilen bilgiler DURUYOR: form kaybolmadı, değerler yerinde.
    expect(screen.getByLabelText('Adınız')).toHaveProperty('value', 'Mehmet Demir')
    expect(screen.getByLabelText('Telefon')).toHaveProperty('value', '+905321112233')
  })
})

describe('AC-15: Sayfa açıldıktan sonra ilan vitrinden düştüğünde gönderilen talep reddedilir', () => {
  it('aracın artık mevcut olmadığı söylenir, bilgiler kalır ve listeye dönme eylemi sunulur', async () => {
    vi.mocked(captureLead).mockRejectedValue(new ApiError('lead.listing_unavailable', null, 422))

    ekran()
    doldur('Mehmet Demir')
    gonder()

    expect(await screen.findByTestId('lead-error')).toBeTruthy()
    expect(screen.getByText(/artık vitrinde değil/)).toBeTruthy()
    expect(screen.getByRole('link', { name: 'Vitrine dön' })).toBeTruthy()
    expect(screen.getByLabelText('Adınız')).toHaveProperty('value', 'Mehmet Demir')
  })
})

describe('AC-39: Hata ekranında HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('teknik ayrıntı ekrana basılmaz', async () => {
    vi.mocked(captureLead).mockRejectedValue(
      new ApiError('lead.listing_unavailable', 'trace-abc-123', 422),
    )

    ekran()
    doldur()
    gonder()

    await screen.findByTestId('lead-error')
    const metin = document.body.textContent ?? ''

    for (const isaret of ['422', 'lead.listing_unavailable', 'trace-abc-123', '/api/leads']) {
      expect(metin).not.toContain(isaret)
    }
  })
})

describe('AC-40: Doğrulama hatası olan alan ziyaretçiye işaretlenir ve hatanın ne olduğu ziyaretçinin dilinde yazılır', () => {
  it('istemci doğrulaması alanı işaretler ve mesajı kendi dilinde yazar', () => {
    ekran()
    doldur('Ayşe Yılmaz', '05321112233')
    gonder()

    const telefon = screen.getByLabelText('Telefon')

    expect(telefon.getAttribute('aria-invalid')).toBe('true')

    // Mesaj alanın KENDİ hata satırında; ipucu metniyle karıştırılmasın diye rolüyle
    // aranıyor. İkisi benzer okunur ama biri yönlendirme, öteki hatadır.
    expect(screen.getByRole('alert').textContent).toMatch(/ülke koduyla yazın/i)
    expect(vi.mocked(captureLead)).not.toHaveBeenCalled()
  })

  it('sunucunun işaretlediği alan da işaretlenir', async () => {
    vi.mocked(captureLead).mockRejectedValue(new ApiError('lead.invalid_email', null, 422, 'email'))

    ekran()
    doldur()
    fireEvent.change(screen.getByLabelText('E-posta'), { target: { value: 'ayse@ornek.com' } })
    gonder()

    await waitFor(() =>
      expect(screen.getByLabelText('E-posta').getAttribute('aria-invalid')).toBe('true'),
    )
  })
})

describe('AC-41: Form etiketleri, talep tipi adları ve onay metni üç locale de kendi dilinde görünür', () => {
  it.each<[AppLocale, string, string]>([
    ['tr', 'Adınız', 'Bilgi talebi'],
    ['en', 'Your name', 'Information request'],
    ['ar', 'اسمك', 'طلب معلومات'],
  ])('%s locale: etiketler ve tip adları kendi dilinde', (locale, adEtiketi, tipAdi) => {
    ekran(locale)

    expect(screen.getByLabelText(adEtiketi)).toBeTruthy()
    expect(screen.getByRole('option', { name: tipAdi })).toBeTruthy()
  })

  it('hiçbir locale de ham çeviri anahtarı görünmez', () => {
    for (const locale of ['tr', 'en', 'ar'] as const) {
      cleanup()
      ekran(locale)

      expect(document.body.textContent ?? '').not.toMatch(/lead\.[a-z_.]+/)
    }
  })
})
