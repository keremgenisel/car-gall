import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { AdminFaqEntry, AdminPage } from '../../shared/api/contentAdmin'
import type { CurrentSession } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import type { AdminView } from '../../shared/lib/route'
import { AdminApp } from './AdminApp'

/**
 * `0017` R23, R24, R8–R10, R18 — panelin "İçerik" bölümü gerçek kabuğun içinde; yalnız API
 * istemcileri sahtelenir. Yerel `<dialog>`'un tarayıcı davranışı e2e'de ölçülür.
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
  fetchAdminPage: vi.fn(),
  createPage: vi.fn(),
  savePage: vi.fn(),
  setPagePublication: vi.fn(),
  deletePage: vi.fn(),
  fetchAdminFaq: vi.fn(),
  createFaqEntry: vi.fn(),
  saveFaqEntry: vi.fn(),
  setFaqPublication: vi.fn(),
  deleteFaqEntry: vi.fn(),
  moveFaqEntry: vi.fn(),
}))

const { fetchCurrentSession } = await import('../../shared/api/identity')
const api = await import('../../shared/api/contentAdmin')

beforeAll(() => {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.removeAttribute('open')
    this.dispatchEvent(new Event('close'))
  }
})

function oturum(role: string, preferredLocale: AppLocale = 'tr'): CurrentSession {
  return {
    staffId: '0192c000-0000-7000-8000-00000000a001',
    displayName: 'Ayşe Yılmaz',
    role,
    preferredLocale,
    mustChangePassword: false,
    twoFactorEnrolled: true,
  } as CurrentSession
}

function sistem(key: string, overrides: Partial<AdminPage> = {}): AdminPage {
  return {
    id: `id-${key}`,
    systemKey: key,
    version: 1,
    locales: ['tr', 'ar', 'en'].map((locale) => ({
      locale,
      title: '',
      body: '',
      slug: '',
      status: 'draft' as const,
    })),
    missingLocales: ['tr', 'ar', 'en'],
    ...overrides,
  }
}

const SISTEM = ['about', 'faq', 'privacy', 'cookies'].map((key) => sistem(key))

const G: AdminPage = {
  id: 'id-g',
  systemKey: null,
  version: 3,
  locales: [
    {
      locale: 'tr',
      title: 'Garanti Koşulları',
      body: 'Araçlarımız 12 ay garantilidir.',
      slug: 'garanti-kosullari',
      status: 'published',
    },
    { locale: 'ar', title: '', body: '', slug: '', status: 'draft' },
    { locale: 'en', title: 'Warranty Terms', body: 'x', slug: 'warranty-terms', status: 'draft' },
  ],
  missingLocales: ['ar'],
}

function girdi(id: string, question: string): AdminFaqEntry {
  return {
    id,
    version: 1,
    locales: [
      { locale: 'tr', question, answer: `${question} — cevap`, status: 'published' },
      { locale: 'ar', question: '', answer: '', status: 'draft' },
      { locale: 'en', question: '', answer: '', status: 'draft' },
    ],
    missingLocales: ['ar', 'en'],
  }
}

async function panel(
  view: AdminView,
  role = 'content_editor',
  pageId?: string,
  locale: AppLocale = 'tr',
) {
  vi.mocked(fetchCurrentSession).mockResolvedValue(oturum(role, locale))
  render(
    <LocaleProvider locale={locale}>
      <AdminApp view={view} pageId={pageId} />
    </LocaleProvider>,
  )
  await screen.findByRole('navigation')
}

beforeEach(() => {
  vi.mocked(api.fetchAdminPages).mockResolvedValue([...SISTEM, G])
  vi.mocked(api.fetchAdminFaq).mockResolvedValue([])
  vi.mocked(api.fetchAdminPage).mockResolvedValue(G)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0017 — erişim', () => {
  it.each([
    ['content_editor', '0017/AC-106'],
    ['manager', '0017/AC-107'],
  ])('%s oturumuyla panel gezinmesinde "İçerik" bölümü vardır (%s)', async (role) => {
    await panel('vehicles', role)
    expect(screen.getByRole('link', { name: 'İçerik' }).getAttribute('href')).toBe('/admin/content')
  })

  it('0017/AC-108: Satış Danışmanı oturumuyla "İçerik" bölümü yoktur; adresine gitse de ekran basılmaz', async () => {
    await panel('content', 'sales_advisor')
    expect(screen.queryByRole('link', { name: 'İçerik' })).toBeNull()
    expect(screen.queryByTestId('content-admin')).toBeNull()
    expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
    expect(vi.mocked(api.fetchAdminPages)).not.toHaveBeenCalled()
  })

  it('0017/AC-111: İçerik Editörünün gezinmesi, "İçerik" dışında, önceki hâliyle aynıdır', async () => {
    await panel('vehicles', 'content_editor')
    const baglantilar = within(screen.getByRole('navigation'))
      .getAllByRole('link')
      .map((a) => a.textContent)
    expect(baglantilar.filter((ad) => ad !== 'İçerik')).toEqual(['İki adımlı doğrulama', 'Araçlar'])
  })
})

describe('0017 — İçerik bölümü: liste ve durumlar', () => {
  it('0017/AC-102: sayfa listesi yanıtlanana kadar yükleniyor göstergesi görünür', async () => {
    vi.mocked(api.fetchAdminPages).mockReturnValue(new Promise(() => {}))
    await panel('content')
    expect(await screen.findByTestId('content-pages-loading')).toBeTruthy()
  })

  it('0017/AC-103: serbest sayfa yokken dört sistem sayfası listelenir ve boş durum metni görünür', async () => {
    vi.mocked(api.fetchAdminPages).mockResolvedValue(SISTEM)
    await panel('content')

    const sistemListesi = await screen.findByTestId('content-system-pages')
    expect(
      within(sistemListesi)
        .getAllByRole('listitem')
        .map((li) => li.getAttribute('data-system-key')),
    ).toEqual(['about', 'faq', 'privacy', 'cookies'])
    expect(screen.getByTestId('content-free-empty').textContent).toContain(
      'Henüz serbest sayfa yok',
    )
  })

  it('0017/AC-104: hiç SSS girdisi yokken SSS alanı boş durum metni gösterir', async () => {
    await panel('content')
    expect((await screen.findByTestId('content-faq-empty')).textContent).toContain('Henüz soru yok')
  })

  it('0017/AC-105: liste isteği 500 döndüğünde hata durumu görünür; boş durum metni görünmez', async () => {
    vi.mocked(api.fetchAdminPages).mockRejectedValue(new ApiError('api.unavailable', null, 500))
    await panel('content')

    expect(await screen.findByTestId('content-pages-error')).toBeTruthy()
    expect(screen.queryByTestId('content-free-empty')).toBeNull()
  })

  it("0017/AC-13 (panel yarısı): ar'ı boş sayfanın satırı eksik dil olarak yalnız Arapça'yı gösterir", async () => {
    await panel('content')
    const satir = (await screen.findByTestId('content-free-pages')).querySelector(
      '[data-testid="content-row-missing"]',
    )!
    expect(satir.textContent).toBe('Eksik dil: العربية')
  })

  it('0017/AC-24 (liste yarısı): silme eylemi serbest sayfada ve SSS girdisinde var, sistem sayfasında yok', async () => {
    vi.mocked(api.fetchAdminFaq).mockResolvedValue([girdi('a', 'A')])
    vi.mocked(api.fetchAdminPage).mockResolvedValueOnce(sistem('about'))
    await panel('content-edit', 'content_editor', 'id-about')
    await screen.findByTestId('content-edit')
    expect(screen.queryByTestId('content-delete')).toBeNull()
    cleanup()

    await panel('content-edit', 'content_editor', 'id-g')
    expect(await screen.findByTestId('content-delete')).toBeTruthy()
    cleanup()

    await panel('content')
    expect(await screen.findByTestId('content-faq-delete')).toBeTruthy()
  })
})

describe('0017 — sayfa düzenleme', () => {
  it('0017/AC-9: yayına alma reddi gövde ve slug alanlarının yanında katalog metniyle; başlıkta hata yok; ar arayüzde Arapça', async () => {
    vi.mocked(api.setPagePublication).mockRejectedValue(
      new ApiError('content.validation_failed', null, 422, null, {
        fields: 'ar.body,ar.slug',
        'ar.body': 'content.field_required',
        'ar.slug': 'content.field_required',
      }),
    )
    await panel('content-edit', 'content_editor', 'id-g', 'ar')
    fireEvent.click(await screen.findByTestId('content-ar-publish'))

    expect((await screen.findByTestId('content-ar-body-error')).textContent).toBe(
      'هذا الحقل مطلوب للنشر.',
    )
    expect(screen.getByTestId('content-ar-slug-error').textContent).toBe('هذا الحقل مطلوب للنشر.')
    expect(screen.queryByTestId('content-ar-title-error')).toBeNull()
    expect(
      screen
        .getByLabelText('العنوان', { selector: '#content-ar-title' })
        .getAttribute('aria-invalid'),
    ).toBeNull()
  })

  it('0017/AC-31: çakışmada "sayfa başkası tarafından değiştirildi" uyarısı görünür ve form güncel hâli yeniden yükler', async () => {
    vi.mocked(api.savePage).mockRejectedValue(new ApiError('concurrency.lost', null, 409))
    vi.mocked(api.fetchAdminPage)
      .mockResolvedValueOnce(G)
      .mockResolvedValueOnce({
        ...G,
        version: 4,
        locales: G.locales.map((l) => (l.locale === 'tr' ? { ...l, title: 'Garanti' } : l)),
      })
    await panel('content-edit', 'content_editor', 'id-g')

    fireEvent.change(await screen.findByLabelText('Başlık', { selector: '#content-tr-title' }), {
      target: { value: 'Garanti Şartları' },
    })
    fireEvent.click(screen.getByTestId('content-save'))

    expect((await screen.findByTestId('content-conflict')).textContent).toContain(
      'Sayfa başkası tarafından değiştirildi',
    )
    await waitFor(() =>
      expect(
        (screen.getByLabelText('Başlık', { selector: '#content-tr-title' }) as HTMLInputElement)
          .value,
      ).toBe('Garanti'),
    )
  })

  it('triyaj BULGU-1 (R1, R6): kaydedilmemiş düzenleme varken dil başına yayın düğmeleri devre dışıdır ve eski metni yayına almaz; kayıttan sonra etkinleşir', async () => {
    vi.mocked(api.savePage).mockResolvedValue(4)
    await panel('content-edit', 'content_editor', 'id-g')

    const enBaslik = await screen.findByLabelText('Başlık', { selector: '#content-en-title' })
    fireEvent.change(enBaslik, { target: { value: 'Warranty Conditions' } })

    const enYayinla = screen.getByTestId('content-en-publish')
    expect(enYayinla).toHaveProperty('disabled', true)
    expect(screen.getByTestId('content-tr-unpublish')).toHaveProperty('disabled', true)
    expect(screen.getByTestId('content-save-first').textContent).toBe(
      'Yayın durumunu değiştirmeden önce değişiklikleri kaydedin.',
    )
    fireEvent.click(enYayinla)
    expect(vi.mocked(api.setPagePublication)).not.toHaveBeenCalled()
    expect(
      (screen.getByLabelText('Başlık', { selector: '#content-en-title' }) as HTMLInputElement)
        .value,
    ).toBe('Warranty Conditions')

    // Kaydedilince sunucu hâli formla aynıdır ve düğmeler yeniden etkinleşir.
    vi.mocked(api.fetchAdminPage).mockResolvedValueOnce({
      ...G,
      version: 4,
      locales: G.locales.map((l) =>
        l.locale === 'en' ? { ...l, title: 'Warranty Conditions' } : l,
      ),
    })
    fireEvent.click(screen.getByTestId('content-save'))
    await waitFor(() =>
      expect(screen.getByTestId('content-en-publish')).toHaveProperty('disabled', false),
    )
    expect(screen.queryByTestId('content-save-first')).toBeNull()
  })

  it('0017/AC-26: silme onay penceresinde vazgeçilirse silme isteği gönderilmez', async () => {
    await panel('content-edit', 'content_editor', 'id-g')
    fireEvent.click(await screen.findByTestId('content-delete'))

    expect(screen.getByTestId('content-delete-dialog').hasAttribute('open')).toBe(true)
    fireEvent.click(screen.getByTestId('content-delete-cancel'))

    expect(screen.getByTestId('content-delete-dialog').hasAttribute('open')).toBe(false)
    expect(vi.mocked(api.deletePage)).not.toHaveBeenCalled()
  })

  it('0017/AC-27 (panel yarısı): onay penceresinde onaylanınca silme isteği sayfanın sürümüyle gönderilir', async () => {
    const assign = vi.fn()
    vi.spyOn(window, 'location', 'get').mockReturnValue({ ...window.location, assign } as Location)
    vi.mocked(api.deletePage).mockResolvedValue()
    await panel('content-edit', 'content_editor', 'id-g')

    fireEvent.click(await screen.findByTestId('content-delete'))
    await act(async () => {
      fireEvent.click(screen.getByTestId('content-delete-confirm'))
    })

    expect(vi.mocked(api.deletePage)).toHaveBeenCalledWith('id-g', 3, 'tr')
    expect(assign).toHaveBeenCalledWith('/admin/content')
  })

  it('0017/AC-12 (panel yarısı), AC-119 (panel yarısı): iki alan hatası iki alanın yanında ayrı ayrı görünür', async () => {
    vi.mocked(api.savePage).mockRejectedValue(
      new ApiError('content.validation_failed', null, 422, null, {
        fields: 'tr.title,tr.slug',
        'tr.title': 'content.title_too_long',
        'tr.slug': 'content.slug_invalid',
      }),
    )
    await panel('content-edit', 'content_editor', 'id-g')
    fireEvent.click(await screen.findByTestId('content-save'))

    expect((await screen.findByTestId('content-tr-title-error')).textContent).toBe(
      'Başlık en fazla 120 karakter olabilir.',
    )
    expect(screen.getByTestId('content-tr-slug-error').textContent).toContain('2–80 karakter')
  })
})

describe('0017 — SSS sırası', () => {
  beforeEach(() => {
    vi.mocked(api.fetchAdminFaq).mockResolvedValue([
      girdi('a', 'A'),
      girdi('b', 'B'),
      girdi('c', 'C'),
    ])
    vi.mocked(api.moveFaqEntry).mockResolvedValue()
  })

  it('0017/AC-76, AC-77 (panel yarısı): "yukarı" ve "aşağı" o girdiyi o yönde taşıma isteği gönderir', async () => {
    await panel('content')
    const girdiler = await screen.findAllByTestId('content-faq-entry')

    fireEvent.click(within(girdiler[2]!).getByTestId('content-faq-up'))
    fireEvent.click(within(girdiler[0]!).getByTestId('content-faq-down'))

    await waitFor(() => expect(vi.mocked(api.moveFaqEntry)).toHaveBeenCalledTimes(2))
    expect(vi.mocked(api.moveFaqEntry).mock.calls.map((call) => [call[0], call[1]])).toEqual([
      ['c', 'up'],
      ['a', 'down'],
    ])
  })

  it('0017/AC-78 (panel yarısı): ilk girdinin "yukarı" ve son girdinin "aşağı" düğmesi devre dışıdır; basılınca istek gitmez', async () => {
    await panel('content')
    const girdiler = await screen.findAllByTestId('content-faq-entry')
    const ilkYukari = within(girdiler[0]!).getByTestId('content-faq-up')
    const sonAsagi = within(girdiler[2]!).getByTestId('content-faq-down')

    expect(ilkYukari).toHaveProperty('disabled', true)
    expect(sonAsagi).toHaveProperty('disabled', true)
    fireEvent.click(ilkYukari)
    expect(vi.mocked(api.moveFaqEntry)).not.toHaveBeenCalled()
  })

  it('0017/AC-80 (bileşen yarısı): düğmeler gerçek <button> öğesidir ve erişilebilir adları girdinin sorusunu taşır', async () => {
    await panel('content')
    const girdiler = await screen.findAllByTestId('content-faq-entry')
    const asagi = within(girdiler[1]!).getByTestId('content-faq-down')

    expect(asagi.tagName).toBe('BUTTON')
    expect(asagi.getAttribute('aria-label')).toBe('Aşağı taşı: B')
  })
})
