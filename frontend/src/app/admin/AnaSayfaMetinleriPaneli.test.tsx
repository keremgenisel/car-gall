import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { AdminHomeTexts, HomeTextFieldKey } from '../../shared/api/contentAdmin'
import type { CurrentSession } from '../../shared/api/types'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'
import panelAr from '../../shared/i18n/locales/panel/ar.json'
import panelEn from '../../shared/i18n/locales/panel/en.json'
import panelTr from '../../shared/i18n/locales/panel/tr.json'
import type { AdminView } from '../../shared/lib/route'
import { codePointLength } from '../../features/content/admin/homeTextCount'
import { AdminApp } from './AdminApp'

/**
 * `0022` — panelin ana sayfa metinleri ekranı gerçek kabuğun içinde; yalnız API istemcileri
 * sahtelenir. Vitrin tarafı `HomePage.test.tsx`'te, sunucu tarafı `Host.Tests`'te.
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

vi.mock('../../shared/api/contentAdmin', () => ({
  fetchAdminPages: vi.fn(),
  fetchAdminFaq: vi.fn(),
  createPage: vi.fn(),
  createFaqEntry: vi.fn(),
  fetchHomeTexts: vi.fn(),
  saveHomeTexts: vi.fn(),
}))

const { fetchCurrentSession } = await import('../../shared/api/identity')
const api = await import('../../shared/api/contentAdmin')

const ALANLAR: readonly HomeTextFieldKey[] = [
  'introTitle',
  'introBody',
  'formTitle',
  'featuredTitle',
  'emptyTitle',
  'emptyBody',
]

const BOS = Object.fromEntries(ALANLAR.map((alan) => [alan, ''])) as Record<
  HomeTextFieldKey,
  string
>

function metinler(
  diller: readonly string[] = ['tr', 'ar', 'en'],
  doldur: Partial<Record<string, Partial<Record<HomeTextFieldKey, string>>>> = {},
  version = 3,
): AdminHomeTexts {
  return {
    version,
    locales: diller.map((locale) => ({ locale, values: { ...BOS, ...(doldur[locale] ?? {}) } })),
  }
}

function oturum(role: string): CurrentSession {
  return {
    staffId: '0192c000-0000-7000-8000-00000000a001',
    displayName: 'Ayşe Yılmaz',
    role,
    preferredLocale: 'tr',
    mustChangePassword: false,
    twoFactorEnrolled: true,
  } as CurrentSession
}

function ekran(view: AdminView = 'content-home') {
  return render(
    <LocaleProvider locale="tr">
      <AdminApp view={view} />
    </LocaleProvider>,
  )
}

function alan(locale: string, field: HomeTextFieldKey): HTMLInputElement | HTMLTextAreaElement {
  return screen.getByTestId(`home-texts-${locale}`).querySelector(`#home-texts-${locale}-${field}`)!
}

beforeEach(() => {
  vi.mocked(fetchCurrentSession).mockResolvedValue(oturum('content_editor'))
  vi.mocked(api.fetchAdminPages).mockResolvedValue([])
  vi.mocked(api.fetchAdminFaq).mockResolvedValue([])
  vi.mocked(api.fetchHomeTexts).mockResolvedValue(metinler())
  vi.mocked(api.saveHomeTexts).mockResolvedValue(4)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0022 — ekran ve kaydetme (R1, R2)', () => {
  it('0022/AC-1: İçerik Editörü panelin İçerik bölümünde "Ana sayfa" girdisinden metin ekranını açar; ekran altı alanı etkin her dil için ayrı ayrı gösterir.', async () => {
    ekran('content')
    const girdi = await screen.findByTestId('content-home-link')
    expect(girdi.textContent).toBe('Ana sayfa')
    expect(girdi.getAttribute('href')).toBe('/admin/content/home')
    cleanup()

    ekran('content-home')
    await screen.findByTestId('home-texts')
    for (const locale of ['tr', 'ar', 'en']) {
      const dil = screen.getByTestId(`home-texts-${locale}`)
      expect(within(dil).getAllByRole('textbox')).toHaveLength(6)
      for (const field of ALANLAR) {
        expect(alan(locale, field)).toBeTruthy()
      }
    }
    expect(screen.getByLabelText('Giriş başlığı', { selector: '#home-texts-tr-introTitle' }))
  })

  it('0022/AC-2: Boş bir alanın yerinde o dilin varsayılan metni ipucu olarak görünür.', async () => {
    ekran()
    await screen.findByTestId('home-texts')

    for (const locale of ['tr', 'ar', 'en'] as const) {
      const varsayilan = createTranslator(locale)
      expect(alan(locale, 'introTitle').placeholder).toBe(varsayilan('home.intro.title'))
      expect(alan(locale, 'introBody').placeholder).toBe(varsayilan('home.intro.body'))
      expect(alan(locale, 'formTitle').placeholder).toBe(varsayilan('home.form.title'))
      expect(alan(locale, 'featuredTitle').placeholder).toBe(varsayilan('home.featured.title'))
      expect(alan(locale, 'emptyTitle').placeholder).toBe(
        varsayilan('showcase.empty.showcase.title'),
      )
      expect(alan(locale, 'emptyBody').placeholder).toBe(varsayilan('showcase.empty.showcase.body'))
    }
    expect(alan('ar', 'introTitle').placeholder).not.toBe(alan('tr', 'introTitle').placeholder)
  })

  it('0022/AC-27: Yönetici de ekranı açar ve kaydeder.', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum('manager'))
    ekran()
    await screen.findByTestId('home-texts')

    fireEvent.change(alan('tr', 'introTitle'), {
      target: { value: "Kadıköy'ün güvenilir galerisi" },
    })
    fireEvent.click(screen.getByTestId('home-texts-save'))

    await screen.findByTestId('home-texts-saved')
    const [version, gonderilen] = vi.mocked(api.saveHomeTexts).mock.calls[0]!
    expect(version).toBe(3)
    expect(gonderilen.find((item) => item.locale === 'tr')!.values.introTitle).toBe(
      "Kadıköy'ün güvenilir galerisi",
    )
    expect(gonderilen.map((item) => item.locale)).toEqual(['tr', 'ar', 'en'])

    // Kayıttan sonra okuma tekrarlanmaz (yazmaya devam eden editörün metni ezilmez); ikinci
    // kayıt yanıttaki yeni sürümle gider.
    expect(vi.mocked(api.fetchHomeTexts)).toHaveBeenCalledTimes(1)
    fireEvent.change(alan('tr', 'formTitle'), { target: { value: 'Devam' } })
    fireEvent.click(screen.getByTestId('home-texts-save'))
    await waitFor(() => expect(vi.mocked(api.saveHomeTexts)).toHaveBeenCalledTimes(2))
    expect(vi.mocked(api.saveHomeTexts).mock.calls[1]![0]).toBe(4)
    expect(alan('tr', 'introTitle').value).toBe("Kadıköy'ün güvenilir galerisi")
  })
})

describe('0022 — sınırlar (R4)', () => {
  it('0022/AC-16: 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.', async () => {
    vi.mocked(api.saveHomeTexts).mockRejectedValue(
      new ApiError('content.validation_failed', null, 422, null, {
        fields: 'tr.introTitle,tr.introBody',
        'tr.introTitle': 'content.title_too_long',
        'tr.introBody': 'content.description_too_long',
      }),
    )
    ekran()
    await screen.findByTestId('home-texts')

    fireEvent.change(alan('tr', 'introTitle'), { target: { value: 'a'.repeat(121) } })
    fireEvent.change(alan('tr', 'introBody'), { target: { value: 'b'.repeat(501) } })
    fireEvent.click(screen.getByTestId('home-texts-save'))

    expect((await screen.findByTestId('home-texts-tr-introTitle-error')).textContent).toBe(
      'Başlık en fazla 120 karakter olabilir.',
    )
    expect(screen.getByTestId('home-texts-tr-introBody-error').textContent).toBe(
      'Açıklama en fazla 500 karakter olabilir.',
    )
    expect(alan('tr', 'introTitle').getAttribute('aria-invalid')).toBe('true')
    expect(alan('tr', 'introTitle').value).toBe('a'.repeat(121))
    expect(screen.queryByTestId('home-texts-saved')).toBeNull()
  })

  it('0022/AC-18: Arapça harf ve emoji içeren, tam 120 kod noktalı bir başlık kabul edilir; 121 kod noktalı olanı reddedilir; panelin sayacı iki durumda da sunucuyla aynı sayıyı gösterir (D15).', async () => {
    // Sunucunun testiyle (`AnaSayfaMetinleriTests.ArapcaVeEmoji`) aynı dizgi kuralı.
    const parca = ['س', 'ي', 'ا', 'ر', 'ة', ' ', '🚗', '✨', 'م']
    const metin = (n: number) =>
      Array.from({ length: n }, (_, i) => parca[i % parca.length]).join('')
    expect(metin(120).length).toBeGreaterThan(120)
    expect(codePointLength(metin(120))).toBe(120)

    ekran()
    await screen.findByTestId('home-texts')

    fireEvent.change(alan('ar', 'introTitle'), { target: { value: metin(120) } })
    const sayac = screen.getByTestId('home-texts-ar-introTitle-counter')
    expect(sayac.textContent).toBe('120 / 120 karakter')
    expect(sayac.hasAttribute('data-over')).toBe(false)
    expect(alan('ar', 'introTitle').hasAttribute('maxlength')).toBe(false)

    fireEvent.change(alan('ar', 'introTitle'), { target: { value: metin(121) } })
    expect(sayac.textContent).toBe('121 / 120 karakter')
    expect(sayac.getAttribute('data-over')).toBe('true')
    // Tarayıcı kesmez: 121'inci kod noktası alanda durur ve sunucunun reddi gösterilir.
    expect(codePointLength(alan('ar', 'introTitle').value)).toBe(121)
  })
})

describe('0022 — eşzamanlılık ve durumlar (R8, R10)', () => {
  it('0022/AC-26: Reddedilen kaydetmede panel uyarı gösterir, güncel hâli yükleme yolu sunar ve editörün yazdıkları ekranda kalır.', async () => {
    vi.mocked(api.saveHomeTexts).mockRejectedValue(new ApiError('concurrency.lost', null, 409))
    ekran()
    await screen.findByTestId('home-texts')

    fireEvent.change(alan('tr', 'introTitle'), { target: { value: 'Benim başlığım' } })
    fireEvent.click(screen.getByTestId('home-texts-save'))

    expect((await screen.findByTestId('home-texts-conflict')).getAttribute('role')).toBe('alert')
    expect(alan('tr', 'introTitle').value).toBe('Benim başlığım')
    expect(vi.mocked(api.fetchHomeTexts)).toHaveBeenCalledTimes(1)

    vi.mocked(api.fetchHomeTexts).mockResolvedValue(
      metinler(undefined, { tr: { introTitle: 'Başkasının başlığı' } }, 4),
    )
    fireEvent.click(screen.getByTestId('home-texts-reload'))

    await waitFor(() => expect(alan('tr', 'introTitle').value).toBe('Başkasının başlığı'))
    expect(screen.queryByTestId('home-texts-conflict')).toBeNull()
  })

  it('0022/AC-31: Ekran veri gelirken yükleniyor durumunu gösterir.', async () => {
    vi.mocked(api.fetchHomeTexts).mockReturnValue(new Promise(() => {}))
    ekran()

    const yukleniyor = await screen.findByTestId('home-texts-loading')
    expect(yukleniyor.getAttribute('role')).toBe('status')
    expect(within(yukleniyor).getByText('Ana sayfa metinleri yükleniyor')).toBeTruthy()
    expect(screen.queryByTestId('home-texts-save')).toBeNull()
  })

  it('0022/AC-32: Veri okunamadığında teknik ayrıntı göstermeyen bir hata ve yeniden deneme gösterilir.', async () => {
    vi.mocked(api.fetchHomeTexts).mockRejectedValueOnce(
      new ApiError('api.unavailable', 'trace-123', 500),
    )
    ekran()

    const hata = await screen.findByTestId('home-texts-error')
    expect(hata.textContent).not.toMatch(/500|api\.unavailable|trace-123/)
    expect(hata.textContent).toContain('Ana sayfa metinleri getirilemedi')

    fireEvent.click(within(hata).getByRole('button', { name: 'Tekrar dene' }))
    await screen.findByTestId('home-texts')
    expect(vi.mocked(api.fetchHomeTexts)).toHaveBeenCalledTimes(2)
  })

  it('0022/AC-33: Kaydetme sunucu hatasıyla düştüğünde yazılanlar kaybolmaz.', async () => {
    vi.mocked(api.saveHomeTexts).mockRejectedValue(new ApiError('api.unavailable', null, 500))
    ekran()
    await screen.findByTestId('home-texts')

    fireEvent.change(alan('en', 'emptyBody'), { target: { value: 'Line one\nLine two' } })
    fireEvent.click(screen.getByTestId('home-texts-save'))

    const hata = await screen.findByTestId('home-texts-failed')
    expect(hata.textContent).not.toMatch(/500|api\.unavailable/)
    expect(alan('en', 'emptyBody').value).toBe('Line one\nLine two')
  })

  it('0022/AC-34: Arapça alanlar sağdan sola yazılır; Arapça kaydedilen giriş başlığı Arapça ana sayfada sağdan sola düzende görünür.', async () => {
    ekran()
    await screen.findByTestId('home-texts')

    for (const field of ALANLAR) {
      expect(alan('ar', field).getAttribute('dir')).toBe('rtl')
      expect(alan('tr', field).getAttribute('dir')).toBe('ltr')
    }
    expect(alan('ar', 'introTitle').getAttribute('lang')).toBe('ar')
  })

  it('0022/AC-36: Etkin dil listesinden çıkarılmış bir dilin alanları ekranda gösterilmez.', async () => {
    vi.mocked(api.fetchHomeTexts).mockResolvedValue(metinler(['tr', 'en']))
    ekran()
    await screen.findByTestId('home-texts')

    expect(screen.queryByTestId('home-texts-ar')).toBeNull()
    expect(screen.getByTestId('home-texts-tr')).toBeTruthy()
    expect(screen.getByTestId('home-texts-en')).toBeTruthy()
  })
})

describe('0022 — erişim ve dil (R9, R10)', () => {
  it('0022/AC-28: Satış Danışmanı\'nın panel gezinmesinde İçerik ve "Ana sayfa" bağlantısı yoktur; `/admin/content/home` adresini doğrudan açtığında "erişim yok" ekranını görür (D19).', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum('sales_advisor'))
    ekran('content-home')

    await screen.findByTestId('admin-no-access')
    const gezinme = screen.getByRole('navigation')
    expect(within(gezinme).queryByText('İçerik')).toBeNull()
    expect(within(gezinme).queryByText('Ana sayfa')).toBeNull()
    expect(screen.queryByTestId('content-home-link')).toBeNull()
    expect(vi.mocked(api.fetchHomeTexts)).not.toHaveBeenCalled()
  })

  it('0022/AC-35: Yeni ekranın metinleri TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.', () => {
    const anahtarlar = [
      'content.home.section',
      'content.home.entry',
      'content.home.entryHint',
      'content.home.title',
      'content.home.intro',
      'content.home.loading',
      'content.home.error.title',
      'content.home.error.body',
      'content.home.conflict',
      'content.home.reload',
      'content.home.emptyHint',
      'content.home.counter',
      'content.fieldError.description_too_long',
      ...ALANLAR.map((field) => `content.home.field.${field}`),
    ]

    for (const katalog of [panelTr, panelAr, panelEn] as Record<string, string>[]) {
      for (const anahtar of anahtarlar) {
        expect(katalog[anahtar]?.trim(), anahtar).toBeTruthy()
      }
    }
    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(catalogKeys(locale)).toEqual(expect.arrayContaining(anahtarlar))
    }
  })
})
