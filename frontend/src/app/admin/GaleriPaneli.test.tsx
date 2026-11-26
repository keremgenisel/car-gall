import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { AdminBranch, AdminGallery } from '../../shared/api/dealershipAdmin'
import type { CurrentSession } from '../../shared/api/types'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'
import type { AppLocale } from '../../shared/lib/locale'
import type { AdminView } from '../../shared/lib/route'
import { AdminApp } from './AdminApp'
import vitrinAr from '../../shared/i18n/locales/ar.json'
import vitrinEn from '../../shared/i18n/locales/en.json'
import vitrinTr from '../../shared/i18n/locales/tr.json'

const vitrin = { tr: vitrinTr, ar: vitrinAr, en: vitrinEn } as const

/**
 * `0020` — panelin galeri ve şubeler ekranları gerçek kabuğun içinde; yalnız API istemcileri
 * sahtelenir (docs/frontend.md § 8.1). Sunucunun kuralları (alan kodları, `409`, `403`) kendi
 * testlerinde ölçülür; burada ölçülen, ekranın onlara verdiği cevap.
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
  fetchBranchList: vi.fn(),
  fetchAdminBranch: vi.fn(),
  openBranch: vi.fn(),
  saveBranch: vi.fn(),
  runBranchAction: vi.fn(),
}))

const { fetchCurrentSession } = await import('../../shared/api/identity')
const api = await import('../../shared/api/dealershipAdmin')

const assign = vi.fn()

beforeAll(() => {
  HTMLDialogElement.prototype.showModal = function showModal(this: HTMLDialogElement) {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close = function close(this: HTMLDialogElement) {
    this.removeAttribute('open')
    this.dispatchEvent(new Event('close'))
  }
  Object.defineProperty(window, 'location', {
    configurable: true,
    value: { ...window.location, assign, pathname: '/admin' },
  })
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

const GALERI: AdminGallery = {
  commercialName: 'Oto Galeri Ticaret A.Ş.',
  taxNumber: '1234567890',
  tradeRegistryNumber: 'TR-123456',
  displayNames: { tr: 'Oto Galeri', ar: 'أوتو غاليري' },
  // `0024` — panel okumasının yeni alanı; hesapsız kurulum, 0020'nin beklentileri değişmez.
  socialLinks: {},
  version: 4,
}

const SUBE: AdminBranch = {
  branchId: 'b-1',
  names: { tr: 'Kadıköy Şubesi', ar: 'فرع كاديكوي' },
  addresses: { tr: 'Bağdat Caddesi 1' },
  phone: '+902165554433',
  whatsApp: '+905321112233',
  email: 'kadikoy@ornek.com',
  timeZone: 'Europe/Istanbul',
  slug: 'kadikoy-subesi',
  latitude: 40.9833,
  longitude: 29.0667,
  status: 'Active',
  version: 2,
  // `0025` — panel okumasının yeni alanı; programsız şube, `0020`'nin beklentileri değişmez.
  openingHours: null,
}

const LISTE = [
  { branchId: 'b-1', name: 'Kadıköy Şubesi', status: 'Active' },
  { branchId: 'b-2', name: 'Moda Şubesi', status: 'TemporarilyClosed' },
  { branchId: 'b-3', name: 'Eski Liman Şubesi', status: 'Closed' },
]

async function panel(
  view: AdminView,
  role = 'manager',
  locale: AppLocale = 'tr',
  branchId?: string,
) {
  vi.mocked(fetchCurrentSession).mockResolvedValue(oturum(role, locale))
  render(
    <LocaleProvider locale={locale}>
      <AdminApp view={view} branchId={branchId} />
    </LocaleProvider>,
  )
  await screen.findByRole('navigation')
}

beforeEach(() => {
  vi.mocked(api.fetchGalleryProfile).mockResolvedValue(GALERI)
  vi.mocked(api.fetchBranchList).mockResolvedValue(LISTE)
  vi.mocked(api.fetchAdminBranch).mockResolvedValue(SUBE)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0020/AC-1: Yönetici galeri ekranında ticari unvanı, vergi numarasını, ticaret sicil numarasını ve etkin her dil için görünen adı görür.', () => {
  it('profil ekranı dört alanı ve etkin her dil için görünen adı gösterir', async () => {
    await panel('gallery')

    expect(await screen.findByLabelText('Ticari unvan')).toHaveProperty(
      'value',
      'Oto Galeri Ticaret A.Ş.',
    )
    expect(screen.getByLabelText('Vergi numarası')).toHaveProperty('value', '1234567890')
    expect(screen.getByLabelText('Ticaret sicil numarası')).toHaveProperty('value', 'TR-123456')

    const adlar = within(screen.getByTestId('gallery-display-names'))
    expect(adlar.getByLabelText('Türkçe')).toHaveProperty('value', 'Oto Galeri')
    expect(adlar.getByLabelText('العربية')).toHaveProperty('value', 'أوتو غاليري')
    // Eksik dil boş görünür; varsayılan dile düşmez (AC-7).
    expect(adlar.getByLabelText('English')).toHaveProperty('value', '')
  })
})

describe('0020/AC-2: Yönetici ticari unvanı değiştirip kaydettiğinde ekran yeniden açıldığında yeni unvan görünür.', () => {
  it('kaydetme ekrandaki unvanı gönderir ve ekran yeniden okunur', async () => {
    vi.mocked(api.saveGalleryProfile).mockResolvedValue(5)
    await panel('gallery')

    fireEvent.change(await screen.findByLabelText('Ticari unvan'), {
      target: { value: 'Yeni Unvan Ltd' },
    })
    vi.mocked(api.fetchGalleryProfile).mockResolvedValue({
      ...GALERI,
      commercialName: 'Yeni Unvan Ltd',
      version: 5,
    })
    fireEvent.click(screen.getByTestId('gallery-save'))

    await screen.findByTestId('dealership-saved')
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0][0]).toBe(4)
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0][1]).toMatchObject({
      commercialName: 'Yeni Unvan Ltd',
    })
    await waitFor(() => expect(vi.mocked(api.fetchGalleryProfile)).toHaveBeenCalledTimes(2))
    expect(screen.getByLabelText('Ticari unvan')).toHaveProperty('value', 'Yeni Unvan Ltd')
  })
})

describe('0020/AC-3: Boş ya da yalnız boşluktan oluşan bir ticari unvan reddedilir ve hata alanın yanında gösterilir; kayıtlı unvan değişmez.', () => {
  it('sunucunun alan hatası unvan alanının yanında çevrilmiş metinle gösterilir', async () => {
    vi.mocked(api.saveGalleryProfile).mockRejectedValue(
      new ApiError('dealership.validation_failed', null, 422, null, {
        fields: 'commercialName',
        commercialName: 'gallery.commercial_name_required',
      }),
    )
    await panel('gallery')

    fireEvent.change(await screen.findByLabelText('Ticari unvan'), { target: { value: '   ' } })
    fireEvent.click(screen.getByTestId('gallery-save'))

    const hata = await screen.findByTestId('gallery-commercial-name-error')
    expect(hata.textContent).toBe('Ticari unvan boş bırakılamaz.')
    expect(screen.getByLabelText('Ticari unvan').getAttribute('aria-invalid')).toBe('true')
    expect(screen.getByLabelText('Ticari unvan').getAttribute('aria-describedby')).toContain(
      'gallery-commercial-name-error',
    )
  })
})

describe('0020/AC-4: Vergi numarası ve ticaret sicil numarası boşaltılıp kaydedildiğinde ikisi de boş kaydedilir.', () => {
  it('boşaltılan vergi ve sicil numarası boş metin olarak gönderilir', async () => {
    vi.mocked(api.saveGalleryProfile).mockResolvedValue(5)
    await panel('gallery')

    fireEvent.change(await screen.findByLabelText('Vergi numarası'), { target: { value: '' } })
    fireEvent.change(screen.getByLabelText('Ticaret sicil numarası'), { target: { value: '' } })
    fireEvent.click(screen.getByTestId('gallery-save'))

    await screen.findByTestId('dealership-saved')
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0][1]).toMatchObject({
      taxNumber: '',
      tradeRegistryNumber: '',
    })
  })
})

describe('0020/AC-6: Varsayılan olmayan bir dildeki görünen ad boş bırakılarak kaydedilebilir.', () => {
  it('boş bırakılan Arapça görünen ad gönderilmez; sunucu onu boş kaydeder', async () => {
    vi.mocked(api.saveGalleryProfile).mockResolvedValue(5)
    await panel('gallery')

    fireEvent.change(
      within(await screen.findByTestId('gallery-display-names')).getByLabelText('العربية'),
      { target: { value: ' ' } },
    )
    fireEvent.click(screen.getByTestId('gallery-save'))

    await screen.findByTestId('dealership-saved')
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0][1].displayNames).toEqual({
      tr: 'Oto Galeri',
    })
  })
})

describe('0020/AC-19: Şubeler ekranı aktif, geçici kapalı ve kapatılmış şubelerin hepsini, her birinin adı ve durumuyla listeler.', () => {
  it('liste üç durumu da adı ve durum metniyle gösterir', async () => {
    await panel('branches')

    const satirlar = await screen.findAllByTestId('branch-row')
    expect(satirlar.map((satir) => satir.textContent)).toEqual([
      'Kadıköy ŞubesiAçık',
      'Moda ŞubesiGeçici olarak kapalı',
      'Eski Liman ŞubesiKapalı',
    ])
    expect(within(satirlar[0]).getByRole('link').getAttribute('href')).toBe('/admin/branches/b-1')
  })
})

describe('0020/AC-20: Listedeki şube adı yöneticinin panel dilindeki karşılığıdır; yoksa varsayılan dildekidir.', () => {
  it('liste panel dilinde istenir ve sunucunun çözdüğü ad basılır', async () => {
    vi.mocked(api.fetchBranchList).mockResolvedValue([
      { branchId: 'b-1', name: 'فرع كاديكوي', status: 'Active' },
    ])
    await panel('branches', 'manager', 'ar')

    expect(await screen.findByText('فرع كاديكوي')).toBeTruthy()
    expect(vi.mocked(api.fetchBranchList).mock.calls[0][0]).toBe('ar')
  })
})

describe('0020/AC-21: Yönetici zorunlu alanları doldurup kaydettiğinde yeni şube listede **aktif** olarak görünür.', () => {
  it('yeni şube formu zorunlu alanları gönderir ve listeye döner', async () => {
    vi.mocked(api.openBranch).mockResolvedValue('b-9')
    await panel('branch-new')

    const adlar = screen.getAllByRole('group')[1]
    fireEvent.change(within(adlar).getByLabelText('Türkçe'), { target: { value: 'Moda Şubesi' } })
    fireEvent.change(screen.getAllByLabelText('Türkçe')[1], { target: { value: 'Moda Caddesi 9' } })
    fireEvent.change(screen.getByLabelText('Telefon'), { target: { value: '+902161112233' } })
    fireEvent.change(screen.getByLabelText('Saat dilimi'), { target: { value: 'Europe/Istanbul' } })
    fireEvent.change(screen.getByLabelText('Adres kısaltması'), {
      target: { value: 'moda-subesi' },
    })
    fireEvent.click(screen.getByTestId('branch-save'))

    await waitFor(() => expect(assign).toHaveBeenCalledWith('/admin/branches'))
    expect(vi.mocked(api.openBranch).mock.calls[0][0]).toMatchObject({
      names: { tr: 'Moda Şubesi' },
      addresses: { tr: 'Moda Caddesi 9' },
      phone: '+902161112233',
      timeZone: 'Europe/Istanbul',
      slug: 'moda-subesi',
      latitude: null,
      longitude: null,
    })
  })
})

describe('0020/AC-24: Varsayılan dildeki adı boş olan şube açılamaz ve hata ad alanının yanında gösterilir.', () => {
  it('varsayılan dildeki ad hatası ad alanının yanında gösterilir; yazılanlar kalır', async () => {
    vi.mocked(api.openBranch).mockRejectedValue(
      new ApiError('dealership.validation_failed', null, 422, null, {
        fields: 'name.tr',
        'name.tr': 'branch.default_locale_name_required',
      }),
    )
    await panel('branch-new')

    fireEvent.change(screen.getByLabelText('Telefon'), { target: { value: '+902161112233' } })
    fireEvent.click(screen.getByTestId('branch-save'))

    expect((await screen.findByTestId('branch-name-tr-error')).textContent).toBe(
      'Varsayılan dildeki şube adı boş bırakılamaz.',
    )
    expect(screen.getByLabelText('Telefon')).toHaveProperty('value', '+902161112233')
    expect(assign).not.toHaveBeenCalled()
  })
})

describe('0020/AC-28: Panelin saat dilimi alanı serbest metin değil, bölge kimliklerinin listesidir.', () => {
  it('saat dilimi alanı bir listedir ve IANA kimliklerini sunar', async () => {
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    const alan = await screen.findByLabelText('Saat dilimi')
    expect(alan.tagName).toBe('SELECT')
    expect((alan as HTMLSelectElement).value).toBe('Europe/Istanbul')
    const secenekler = within(alan)
      .getAllByRole('option')
      .map((option) => option.getAttribute('value'))
    expect(secenekler).toContain('Asia/Kuwait')
    expect(secenekler).not.toContain('+03:00')
  })
})

describe('0020/AC-39: WhatsApp numarası, e-posta ve koordinat boşaltılıp kaydedildiğinde üçü de boş kaydedilir.', () => {
  it('boşaltılan WhatsApp, e-posta ve koordinat boş olarak gönderilir', async () => {
    vi.mocked(api.saveBranch).mockResolvedValue(3)
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    fireEvent.change(await screen.findByLabelText('WhatsApp numarası'), { target: { value: '' } })
    fireEvent.change(screen.getByLabelText('E-posta'), { target: { value: '' } })
    fireEvent.change(screen.getByLabelText('Enlem'), { target: { value: '' } })
    fireEvent.change(screen.getByLabelText('Boylam'), { target: { value: '' } })
    fireEvent.click(screen.getByTestId('branch-save'))

    await screen.findByTestId('dealership-saved')
    expect(vi.mocked(api.saveBranch).mock.calls[0][2]).toMatchObject({
      whatsApp: '',
      email: '',
      latitude: null,
      longitude: null,
    })
  })
})

describe('0020/AC-42: Kapatılmış bir şube düzenlenemez: ekran alanları salt okunur gösterir ve uç reddeder (R10, D21).', () => {
  it('kapatılmış şubenin alanları salt okunur, kaydet ve eylemler yok', async () => {
    vi.mocked(api.fetchAdminBranch).mockResolvedValue({ ...SUBE, status: 'Closed' })
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    expect(await screen.findByTestId('branch-closed-note')).toBeTruthy()
    expect((screen.getByTestId('branch-fields') as HTMLFieldSetElement).disabled).toBe(true)
    expect(screen.queryByTestId('branch-save')).toBeNull()
  })
})

describe('0020/AC-43: Aktif bir şube geçici kapatıldığında listede **geçici kapalı** görünür ve altlıkta görünmez.', () => {
  it('geçici kapatma eylemi okunan sürümle gönderilir ve ekran güncel durumu gösterir', async () => {
    vi.mocked(api.runBranchAction).mockResolvedValue(3)
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    vi.mocked(api.fetchAdminBranch).mockResolvedValue({
      ...SUBE,
      status: 'TemporarilyClosed',
      version: 3,
    })
    fireEvent.click(await screen.findByTestId('branch-close-temporarily'))

    await waitFor(() =>
      expect(
        within(screen.getByTestId('branch-status')).getByText('Geçici olarak kapalı'),
      ).toBeTruthy(),
    )
    expect(vi.mocked(api.runBranchAction).mock.calls[0].slice(0, 3)).toEqual([
      'b-1',
      2,
      'close-temporarily',
    ])
    expect(await screen.findByTestId('branch-reopen')).toBeTruthy()
  })
})

describe('0020/AC-47: Kalıcı kapatma, işlem yapılmadan önce D18\'deki metni — "geri alınamaz" ve "bu şubedeki araçlar vitrinden kalkar ve şube ataması bekler" — gösteren bir onay ister; onaydan vazgeçmek şubeyi değiştirmez.', () => {
  it('kalıcı kapatma D18 metniyle onay ister; vazgeçmek hiçbir istek göndermez', async () => {
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    fireEvent.click(await screen.findByTestId('branch-close'))

    expect(screen.getByTestId('branch-close-body').textContent).toBe(
      'Bu işlem geri alınamaz. Bu şubedeki araçlar vitrinden kalkar ve şube ataması bekler.',
    )
    fireEvent.click(screen.getByTestId('branch-close-cancel'))

    expect(vi.mocked(api.runBranchAction)).not.toHaveBeenCalled()
    expect(within(screen.getByTestId('branch-status')).getByText('Açık')).toBeTruthy()
  })

  it('onaylanan kalıcı kapatma gönderilir', async () => {
    vi.mocked(api.runBranchAction).mockResolvedValue(3)
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    fireEvent.click(await screen.findByTestId('branch-close'))
    fireEvent.click(screen.getByTestId('branch-close-confirm'))

    await waitFor(() =>
      expect(vi.mocked(api.runBranchAction).mock.calls[0].slice(0, 3)).toEqual(['b-1', 2, 'close']),
    )
  })
})

describe('0020/AC-50: Galerinin son aktif şubesini kalıcı kapatma reddedilir; panel bunun nedenini söyler ve şube aktif kalır.', () => {
  it('son aktif şubenin reddi nedeniyle söylenir', async () => {
    vi.mocked(api.runBranchAction).mockRejectedValue(
      new ApiError('branch.last_active_branch', null, 409),
    )
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    fireEvent.click(await screen.findByTestId('branch-close'))
    fireEvent.click(screen.getByTestId('branch-close-confirm'))

    expect((await screen.findByTestId('dealership-save-failed')).textContent).toBe(
      'Galerinin başka aktif şubesi olmadığı için bu şube kalıcı olarak kapatılamaz. Önce başka bir şubeyi açın ya da yeniden açın.',
    )
    expect(within(screen.getByTestId('branch-status')).getByText('Açık')).toBeTruthy()
  })
})

describe('0020/AC-54: Kapatılmış bir şube için yeniden açma ve geçici kapatma eylemleri sunulmaz ve uç onları reddeder.', () => {
  it('kapatılmış şubede yeniden açma ve geçici kapatma sunulmaz', async () => {
    vi.mocked(api.fetchAdminBranch).mockResolvedValue({ ...SUBE, status: 'Closed' })
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    await screen.findByTestId('branch-closed-note')
    expect(screen.queryByTestId('branch-reopen')).toBeNull()
    expect(screen.queryByTestId('branch-close-temporarily')).toBeNull()
    expect(screen.queryByTestId('branch-close')).toBeNull()
  })
})

describe('0020/AC-63: Reddedilen eski sürüm kaydetmesinde panel bir uyarı gösterir ve güncel hâli yükleme yolu sunar; yöneticinin yazdığı değerler uyarıyla birlikte ekranda kalır.', () => {
  it('eski sürüm reddinde uyarı çıkar, yazılan değer ekranda kalır ve güncel hâl yüklenebilir', async () => {
    vi.mocked(api.saveBranch).mockRejectedValue(new ApiError('concurrency.lost', null, 409))
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    fireEvent.change(await screen.findByLabelText('Telefon'), {
      target: { value: '+902160000000' },
    })
    fireEvent.click(screen.getByTestId('branch-save'))

    expect(await screen.findByTestId('dealership-conflict')).toBeTruthy()
    expect(screen.getByLabelText('Telefon')).toHaveProperty('value', '+902160000000')
    expect(vi.mocked(api.fetchAdminBranch)).toHaveBeenCalledTimes(1)

    vi.mocked(api.fetchAdminBranch).mockResolvedValue({
      ...SUBE,
      phone: '+902169999999',
      version: 7,
    })
    fireEvent.click(screen.getByTestId('dealership-conflict-reload'))

    await waitFor(() =>
      expect(screen.getByLabelText('Telefon')).toHaveProperty('value', '+902169999999'),
    )
    expect(screen.queryByTestId('dealership-conflict')).toBeNull()
  })

  it('galeri profilinde de aynı: uyarı ve değerler birlikte', async () => {
    vi.mocked(api.saveGalleryProfile).mockRejectedValue(new ApiError('concurrency.lost', null, 409))
    await panel('gallery')

    fireEvent.change(await screen.findByLabelText('Ticari unvan'), {
      target: { value: 'Benim Yazdığım' },
    })
    fireEvent.click(screen.getByTestId('gallery-save'))

    expect(await screen.findByTestId('dealership-conflict')).toBeTruthy()
    expect(screen.getByLabelText('Ticari unvan')).toHaveProperty('value', 'Benim Yazdığım')
  })
})

describe('0020/AC-64: Satış Danışmanı ve İçerik Editörü panelde galeri ve şubeler ekranına giden bir bağlantı görmez.', () => {
  it.each(['sales_advisor', 'content_editor'])(
    '%s gezinmesinde Galeri ve Şubeler bağlantısı yoktur',
    async (role) => {
      await panel('vehicles', role)

      expect(screen.queryByRole('link', { name: 'Galeri' })).toBeNull()
      expect(screen.queryByRole('link', { name: 'Şubeler' })).toBeNull()
    },
  )

  it('Yönetici gezinmesinde iki bağlantı da vardır', async () => {
    await panel('vehicles', 'manager')

    expect(screen.getByRole('link', { name: 'Galeri' }).getAttribute('href')).toBe('/admin/gallery')
    expect(screen.getByRole('link', { name: 'Şubeler' }).getAttribute('href')).toBe(
      '/admin/branches',
    )
  })
})

describe('0020/AC-65: Satış Danışmanı galeri ya da şubeler ekranının adresini doğrudan açtığında "erişim yok" ekranını görür; giriş ekranına yönlendirilmez ve "getirilemedi" hatası görmez (D23).', () => {
  it.each<AdminView>(['gallery', 'branches', 'branch-new', 'branch-edit'])(
    'Satış Danışmanı %s adresinde "erişim yok" görür; istek gitmez, hata ekranı yok',
    async (view) => {
      await panel(view, 'sales_advisor', 'tr', 'b-1')

      expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
      expect(screen.queryByTestId('gallery-error')).toBeNull()
      expect(screen.queryByTestId('branches-error')).toBeNull()
      expect(vi.mocked(api.fetchGalleryProfile)).not.toHaveBeenCalled()
      expect(vi.mocked(api.fetchBranchList)).not.toHaveBeenCalled()
      expect(vi.mocked(api.fetchAdminBranch)).not.toHaveBeenCalled()
    },
  )
})

describe('0020/AC-69: Galeri ve şubeler ekranı veri gelirken yükleniyor durumunu gösterir.', () => {
  it.each<[AdminView, string]>([
    ['gallery', 'gallery-loading'],
    ['branches', 'branches-loading'],
    ['branch-edit', 'branch-loading'],
  ])('%s veri gelirken yükleniyor durumunu gösterir', async (view, testId) => {
    vi.mocked(api.fetchGalleryProfile).mockReturnValue(new Promise(() => {}))
    vi.mocked(api.fetchBranchList).mockReturnValue(new Promise(() => {}))
    vi.mocked(api.fetchAdminBranch).mockReturnValue(new Promise(() => {}))
    await panel(view, 'manager', 'tr', 'b-1')

    expect(await screen.findByTestId(testId)).toBeTruthy()
  })
})

describe('0020/AC-70: Veri okunamadığında ekran teknik ayrıntı göstermeyen bir hata durumu ve yeniden deneme yolu gösterir.', () => {
  it.each<[AdminView, string, string]>([
    ['gallery', 'gallery-error', 'gallery-profile'],
    ['branches', 'branches-error', 'branches'],
    ['branch-edit', 'branch-error', 'branch-edit'],
  ])(
    '%s okunamadığında teknik ayrıntısız hata ve yeniden deneme gösterir',
    async (view, testId, hazir) => {
      const hata = new ApiError('api.unavailable', 'trace-123', 500)
      const okuma =
        view === 'gallery'
          ? api.fetchGalleryProfile
          : view === 'branches'
            ? api.fetchBranchList
            : api.fetchAdminBranch
      vi.mocked(okuma).mockRejectedValueOnce(hata)
      await panel(view, 'manager', 'tr', 'b-1')

      const durum = await screen.findByTestId(testId)
      expect(durum.textContent).not.toMatch(/500|trace-123|api\.unavailable/)

      await act(async () => {
        fireEvent.click(within(durum).getByRole('button', { name: 'Yeniden dene' }))
      })
      expect(await screen.findByTestId(hazir)).toBeTruthy()
    },
  )
})

describe('0020/AC-71: Kaydetme sunucu hatasıyla düştüğünde yöneticinin yazdığı değerler kaybolmaz.', () => {
  it('sunucu hatasında yazılan değerler kaybolmaz', async () => {
    vi.mocked(api.saveBranch).mockRejectedValue(new ApiError('api.unavailable', 'trace-1', 500))
    await panel('branch-edit', 'manager', 'tr', 'b-1')

    fireEvent.change(await screen.findByLabelText('Adres kısaltması'), {
      target: { value: 'yeni-kisaltma' },
    })
    fireEvent.click(screen.getByTestId('branch-save'))

    expect(await screen.findByTestId('dealership-save-failed')).toBeTruthy()
    expect(screen.getByLabelText('Adres kısaltması')).toHaveProperty('value', 'yeni-kisaltma')
    expect(screen.getByTestId('dealership-save-failed').textContent).not.toMatch(/500|trace/)
  })

  it('galeri profilinde de aynı', async () => {
    vi.mocked(api.saveGalleryProfile).mockRejectedValue(new ApiError('api.unavailable', null, 500))
    await panel('gallery')

    fireEvent.change(await screen.findByLabelText('Vergi numarası'), { target: { value: '5555' } })
    fireEvent.click(screen.getByTestId('gallery-save'))

    expect(await screen.findByTestId('dealership-save-failed')).toBeTruthy()
    expect(screen.getByLabelText('Vergi numarası')).toHaveProperty('value', '5555')
  })
})

describe('0020/AC-72: Ekranların bütün metinleri TR/AR/EN çeviri kataloğundan gelir; eksik anahtar kontrolü yeşildir.', () => {
  it('ekranların anahtarları üç katalogda da var ve boş değil', () => {
    const tr = catalogKeys('tr').filter(
      (key) =>
        key.startsWith('dealership.') ||
        key === 'admin.nav.gallery' ||
        key === 'admin.nav.branches',
    )

    expect(tr.length).toBeGreaterThan(50)

    for (const locale of ['tr', 'ar', 'en'] as const) {
      const keys = catalogKeys(locale)
      const t = createTranslator(locale)

      for (const key of tr) {
        expect(keys).toContain(key)
        expect(t(key).trim()).not.toBe('')
      }
    }
  })
})

describe('0020/AC-73: Arapça panelde galeri ve şube formları sağdan sola düzende çalışır; telefon, e-posta ve kısaltma alanları soldan sağa yazılır.', () => {
  it('Arapça panelde telefon, e-posta, kısaltma ve koordinat soldan sağa; ad ve adres içerikten', async () => {
    await panel('branch-edit', 'manager', 'ar', 'b-1')

    for (const label of [
      'الهاتف',
      'رقم واتساب',
      'البريد الإلكتروني',
      'الاختصار في العنوان',
      'خط العرض',
      'خط الطول',
      'المنطقة الزمنية',
    ]) {
      expect((await screen.findByLabelText(label)).getAttribute('dir')).toBe('ltr')
    }

    for (const field of screen.getAllByLabelText('العربية')) {
      expect(field.getAttribute('dir')).toBe('auto')
    }
  })
})

// ── 0024 — sosyal hesaplar bölümü (R1, R8 · D5) ──────────────────────────────

const PLATFORMLAR = ['Instagram', 'Facebook', 'YouTube', 'TikTok', 'X', 'LinkedIn']

function sosyal() {
  return within(screen.getByTestId('gallery-social-links'))
}

describe('0024/AC-1, AC-2: "Sosyal hesaplar" bölümü D1 sırasıyla altı adres alanı gösterir; etiket platform adıdır, kayıtlı hesap dolu, olmayan boş gelir', () => {
  it('altı alan, sıra ve değerler', async () => {
    vi.mocked(api.fetchGalleryProfile).mockResolvedValue({
      ...GALERI,
      socialLinks: {
        youtube: 'https://www.youtube.com/@oto',
        instagram: 'https://www.instagram.com/oto',
      },
    })
    await panel('gallery')
    await screen.findByTestId('gallery-social-links')

    const alanlar = sosyal().getAllByRole('textbox')
    expect(
      alanlar.map((alan) => document.querySelector(`label[for="${alan.id}"]`)?.textContent),
    ).toEqual(PLATFORMLAR)
    expect(alanlar.map((alan) => (alan as HTMLInputElement).value)).toEqual([
      'https://www.instagram.com/oto',
      '',
      'https://www.youtube.com/@oto',
      '',
      '',
      '',
    ])
    for (const alan of alanlar) {
      expect(alan.getAttribute('type')).toBe('url')
      expect(alan.getAttribute('inputmode')).toBe('url')
    }
  })
})

describe('0024/AC-3, AC-5: Kaydetme altı kodun hepsini gönderir; yazılan adres gider, boşaltılan alan boş değer olarak gider (sunucu siler)', () => {
  it('küme bütünüyle gönderilir', async () => {
    vi.mocked(api.saveGalleryProfile).mockResolvedValue(5)
    vi.mocked(api.fetchGalleryProfile).mockResolvedValue({
      ...GALERI,
      socialLinks: { youtube: 'https://www.youtube.com/@silinecek' },
    })
    await panel('gallery')
    await screen.findByTestId('gallery-social-links')

    fireEvent.change(sosyal().getByLabelText('Instagram'), {
      target: { value: 'https://www.instagram.com/yeni' },
    })
    fireEvent.change(sosyal().getByLabelText('YouTube'), { target: { value: '' } })
    fireEvent.click(screen.getByTestId('gallery-save'))

    await screen.findByTestId('dealership-saved')
    expect(vi.mocked(api.saveGalleryProfile).mock.calls[0][1].socialLinks).toEqual({
      instagram: 'https://www.instagram.com/yeni',
      facebook: '',
      youtube: '',
      tiktok: '',
      x: '',
      linkedin: '',
    })
  })
})

describe('0024/AC-19, AC-18: Alan hatası ilgili adres alanının altında, ekranın dilinde görünür; yazılan değer alanda kalır', () => {
  it.each<[AppLocale, string, string]>([
    ['tr', 'Instagram', 'Adres bu platforma ait bir hesap adresi değil.'],
    ['ar', 'Instagram', 'العنوان ليس عنوان حساب على هذه المنصة.'],
  ])('%s', async (locale, etiket, baslangic) => {
    vi.mocked(api.saveGalleryProfile).mockRejectedValue(
      new ApiError('dealership.validation_failed', null, 422, null, {
        fields: 'socialLinks.instagram,socialLinks.x',
        'socialLinks.instagram': 'gallery.social_link_invalid',
        'socialLinks.x': 'gallery.social_link_too_long',
      }),
    )
    await panel('gallery', 'manager', locale)
    await screen.findByTestId('gallery-social-links')

    fireEvent.change(sosyal().getByLabelText(etiket), {
      target: { value: 'http://www.instagram.com/x' },
    })
    fireEvent.click(screen.getByTestId('gallery-save'))

    const hata = await screen.findByTestId('gallery-social-instagram-error')
    expect(hata.textContent?.startsWith(baslangic)).toBe(true)
    expect(screen.getByTestId('gallery-social-x-error').textContent).toBe(
      createTranslator(locale)('dealership.fieldError.gallery_social_link_too_long'),
    )
    expect(sosyal().getByLabelText(etiket)).toHaveProperty('value', 'http://www.instagram.com/x')
    expect(sosyal().getByLabelText(etiket).getAttribute('aria-invalid')).toBe('true')
  })
})

describe('0024/AC-27, AC-28, AC-29: Erişim yok, yükleniyor ve hata durumlarında sosyal hesap alanları basılmaz', () => {
  it('Satış Danışmanı "erişim yok" görür', async () => {
    await panel('gallery', 'sales_advisor')

    expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
    expect(screen.queryByTestId('gallery-social-links')).toBeNull()
  })

  it('okunurken yükleniyor durumu', async () => {
    vi.mocked(api.fetchGalleryProfile).mockReturnValue(new Promise(() => {}))
    await panel('gallery')

    expect(await screen.findByTestId('gallery-loading')).toBeTruthy()
    expect(screen.queryByTestId('gallery-social-links')).toBeNull()
  })

  it('okuma hata verince hata ve yeniden deneme', async () => {
    vi.mocked(api.fetchGalleryProfile).mockRejectedValueOnce(
      new ApiError('api.unavailable', null, 500),
    )
    await panel('gallery')

    const hata = await screen.findByTestId('gallery-error')
    expect(within(hata).getByRole('button')).toBeTruthy()
    expect(screen.queryByTestId('gallery-social-links')).toBeNull()
  })
})

describe('0024/AC-30, AC-31: Hesapsız galeride altı boş alan ve yardım metni; ar panelinde başlık ve yardım Arapça, adres alanları soldan sağa', () => {
  it.each<AppLocale>(['tr', 'ar', 'en'])('%s', async (locale) => {
    await panel('gallery', 'manager', locale)
    await screen.findByTestId('gallery-social-links')
    const t = createTranslator(locale)

    expect(sosyal().getByText(t('dealership.gallery.social.title')).tagName).toBe('LEGEND')
    expect(sosyal().getByText(t('dealership.gallery.social.hint'))).toBeTruthy()
    const alanlar = sosyal().getAllByRole('textbox')
    expect(alanlar).toHaveLength(6)
    expect(alanlar.every((alan) => (alan as HTMLInputElement).value === '')).toBe(true)
    expect(alanlar.every((alan) => alan.getAttribute('dir') === 'ltr')).toBe(true)
  })

  it("tr yardım metni AC-30'un cümlesidir, ar metni Arapçadır", () => {
    expect(createTranslator('tr')('dealership.gallery.social.hint')).toContain(
      'Boş bırakılan platform vitrinde görünmez',
    )
    expect(createTranslator('ar')('dealership.gallery.social.title')).toMatch(/\p{Script=Arabic}/u)
    expect(createTranslator('ar')('dealership.gallery.social.hint')).toMatch(/\p{Script=Arabic}/u)
  })
})

describe('0024/AC-57, AC-59: Panel metinleri panel kataloğunda ve üç dilde; vitrin kataloğuna yalnız blok başlığı eklendi', () => {
  it('anahtarlar', () => {
    const panelAnahtarlari = [
      'dealership.gallery.social.title',
      'dealership.gallery.social.hint',
      'dealership.fieldError.gallery_social_link_invalid',
      'dealership.fieldError.gallery_social_link_too_long',
      'dealership.fieldError.gallery_social_platform_unknown',
    ]
    for (const locale of ['tr', 'ar', 'en'] as const) {
      for (const anahtar of [...panelAnahtarlari, 'home.footer.follow']) {
        expect(catalogKeys(locale)).toContain(anahtar)
      }
      // ADR 0013 — vitrin kataloğu panel metni taşımaz; vitrine eklenen tek anahtar blok başlığı.
      for (const anahtar of panelAnahtarlari) {
        expect(Object.keys(vitrin[locale])).not.toContain(anahtar)
      }
      expect(
        Object.keys(vitrin[locale]).filter((anahtar) => /social|follow/.test(anahtar)),
      ).toEqual(['home.footer.follow'])
    }
  })
})

// ── 0025 — şube formunun çalışma saatleri (R1, R6, R11 · D5) ─────────────────

function saatler() {
  return within(screen.getByTestId('branch-opening-hours'))
}

describe('0025/AC-1, AC-2: Bölüm yedi günü Pazartesi → Pazar ekranın dilinde gösterir; programsız şubede yedi gün "Kapalı" ve yardım metni', () => {
  it.each<[AppLocale, string, string]>([
    ['tr', 'Pazartesi', 'Pazar'],
    ['en', 'Monday', 'Sunday'],
  ])('%s', async (locale, ilk, son) => {
    await panel('branch-edit', 'manager', locale, 'b-1')
    await screen.findByTestId('branch-opening-hours')
    const t = createTranslator(locale)

    const adlar = saatler()
      .getAllByText((_, el) => el?.classList.contains('opening-hours__name') === true)
      .map((el) => el.textContent)
    expect(adlar).toHaveLength(7)
    expect([adlar[0], adlar[6]]).toEqual([ilk, son])
    expect(saatler().getAllByText(t('dealership.branch.hours.closed'))).toHaveLength(7)
    expect(saatler().getByText(t('dealership.branch.hours.hint'))).toBeTruthy()
  })
})

describe('0025/AC-3, AC-4, AC-19: Pazartesiye aralık girip kaydetmek yedi günü gönderir; kayıtlı aralıklar sırayla görünür', () => {
  it('gövde ve görünüm', async () => {
    vi.mocked(api.saveBranch).mockResolvedValue(3)
    vi.mocked(api.fetchAdminBranch).mockResolvedValue({
      ...SUBE,
      openingHours: {
        monday: [],
        tuesday: [
          { start: '09:00', end: '13:00' },
          { start: '16:00', end: '21:00' },
        ],
        wednesday: [],
        thursday: [],
        friday: [],
        saturday: [],
        sunday: [],
      },
    })
    await panel('branch-edit', 'manager', 'tr', 'b-1')
    await screen.findByTestId('branch-opening-hours')

    const sali = within(screen.getByTestId('hours-tuesday'))
    expect(sali.getAllByLabelText(/Açılış/).map((el) => (el as HTMLInputElement).value)).toEqual([
      '09:00',
      '16:00',
    ])

    fireEvent.click(screen.getByTestId('hours-add-monday'))
    const pazartesi = within(screen.getByTestId('hours-monday'))
    fireEvent.change(pazartesi.getByLabelText(/Açılış/), { target: { value: '09:00' } })
    fireEvent.change(pazartesi.getByLabelText(/Kapanış/), { target: { value: '18:00' } })
    expect(pazartesi.getByLabelText(/Açılış/).getAttribute('type')).toBe('time')
    fireEvent.click(screen.getByTestId('branch-save'))

    await screen.findByTestId('dealership-saved')
    const govde = vi.mocked(api.saveBranch).mock.calls[0]![2].openingHours
    expect(Object.keys(govde)).toEqual([
      'monday',
      'tuesday',
      'wednesday',
      'thursday',
      'friday',
      'saturday',
      'sunday',
    ])
    expect(govde['monday']).toEqual([{ start: '09:00', end: '18:00' }])
    expect(govde['tuesday']).toHaveLength(2)
  })
})

describe('0025/AC-5, AC-7: Üç aralıklı günde "Aralık ekle" yok; Salı\'nın "Önceki günle aynı"sı Pazartesiyi formda kopyalar, kaydetme istemez', () => {
  it('eylemler', async () => {
    await panel('branch-edit', 'manager', 'tr', 'b-1')
    await screen.findByTestId('branch-opening-hours')

    for (let i = 0; i < 3; i += 1) {
      fireEvent.click(screen.getByTestId('hours-add-monday'))
    }
    expect(screen.queryByTestId('hours-add-monday')).toBeNull()
    expect(screen.queryByTestId('hours-copy-monday')).toBeNull()

    fireEvent.click(screen.getByTestId('hours-copy-tuesday'))
    expect(within(screen.getByTestId('hours-tuesday')).getAllByLabelText(/Açılış/)).toHaveLength(3)
    expect(vi.mocked(api.saveBranch)).not.toHaveBeenCalled()
  })
})

describe('0025/AC-8: Yeni şube formunda bölüm var ve açma gövdesi programı taşır', () => {
  it('açma', async () => {
    vi.mocked(api.openBranch).mockResolvedValue({ id: 'yeni', version: 1 } as never)
    await panel('branch-new')
    expect(await screen.findByTestId('branch-opening-hours')).toBeTruthy()

    fireEvent.click(screen.getByTestId('hours-add-saturday'))
    const cumartesi = within(screen.getByTestId('hours-saturday'))
    fireEvent.change(cumartesi.getByLabelText(/Açılış/), { target: { value: '10:00' } })
    fireEvent.change(cumartesi.getByLabelText(/Kapanış/), { target: { value: '14:00' } })
    fireEvent.change(screen.getByLabelText('Türkçe', { selector: '#branch-name-tr' }), {
      target: { value: 'Yeni Şube' },
    })
    fireEvent.click(screen.getByTestId('branch-save'))

    await waitFor(() => expect(vi.mocked(api.openBranch)).toHaveBeenCalled())
    expect(vi.mocked(api.openBranch).mock.calls[0]![0].openingHours['saturday']).toEqual([
      { start: '10:00', end: '14:00' },
    ])
  })
})

describe('0025/AC-22: Alan hatası ilgili aralığın ya da günün yanında, ekranın dilinde; girilen değer formda kalır', () => {
  it.each<AppLocale>(['tr', 'ar'])('%s', async (locale) => {
    vi.mocked(api.saveBranch).mockRejectedValue(
      new ApiError('dealership.validation_failed', null, 422, null, {
        fields: 'openingHours.monday.0.end,openingHours.wednesday',
        'openingHours.monday.0.end': 'opening_hours.end_not_after_start',
        'openingHours.wednesday': 'opening_hours.overlap',
      }),
    )
    await panel('branch-edit', 'manager', locale, 'b-1')
    await screen.findByTestId('branch-opening-hours')
    const t = createTranslator(locale)

    fireEvent.click(screen.getByTestId('hours-add-monday'))
    // Saat alanının ARIA rolü yok (`type="time"`); alan kendi etiket bağıyla seçilir.
    const acilis = () =>
      screen
        .getByTestId('hours-monday')
        .querySelector<HTMLInputElement>('input[aria-labelledby$="-start"]')!
    fireEvent.change(acilis(), { target: { value: '18:00' } })
    fireEvent.click(screen.getByTestId('branch-save'))

    expect((await screen.findByTestId('error-openingHours.monday.0.end')).textContent).toBe(
      t('dealership.fieldError.opening_hours_end_not_after_start'),
    )
    expect(
      within(screen.getByTestId('hours-wednesday')).getByTestId('error-openingHours.wednesday')
        .textContent,
    ).toBe(t('dealership.fieldError.opening_hours_overlap'))
    expect(acilis().value).toBe('18:00')
  })
})

describe('0025/AC-35, AC-37, AC-38: Erişim yok, yükleniyor ve hata durumlarında çalışma saatleri bölümü basılmaz', () => {
  it('Satış Danışmanı', async () => {
    await panel('branch-edit', 'sales_advisor', 'tr', 'b-1')
    expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
    expect(screen.queryByTestId('branch-opening-hours')).toBeNull()
  })

  it('yükleniyor', async () => {
    vi.mocked(api.fetchAdminBranch).mockReturnValue(new Promise(() => {}))
    await panel('branch-edit', 'manager', 'tr', 'b-1')
    expect(await screen.findByTestId('branch-loading')).toBeTruthy()
    expect(screen.queryByTestId('branch-opening-hours')).toBeNull()
  })

  it('hata', async () => {
    vi.mocked(api.fetchAdminBranch).mockRejectedValueOnce(
      new ApiError('api.unavailable', null, 500),
    )
    await panel('branch-edit', 'manager', 'tr', 'b-1')
    expect(await screen.findByTestId('branch-error')).toBeTruthy()
    expect(screen.queryByTestId('branch-opening-hours')).toBeNull()
  })
})

describe('0025/AC-39, AC-63, AC-65: ar bölümü Arapça; panel metinleri panel kataloğunda üç dilde; vitrine yalnız iki metin', () => {
  it('ar metinleri ve katalog', async () => {
    await panel('branch-edit', 'manager', 'ar', 'b-1')
    await screen.findByTestId('branch-opening-hours')
    const ar = createTranslator('ar')
    expect(saatler().getByText(ar('dealership.branch.hours.title')).textContent).toMatch(
      /\p{Script=Arabic}/u,
    )
    expect(saatler().getAllByText(ar('dealership.branch.hours.closed'))).toHaveLength(7)
    expect(
      saatler().getAllByText((_, el) => el?.classList.contains('opening-hours__name') === true)[0]!
        .textContent,
    ).toMatch(/\p{Script=Arabic}/u)

    const panelAnahtarlari = [
      'dealership.branch.hours.title',
      'dealership.branch.hours.hint',
      'dealership.branch.hours.add',
      'dealership.branch.hours.remove',
      'dealership.branch.hours.copyPrevious',
      'dealership.branch.hours.closed',
      'dealership.fieldError.opening_hours_invalid_time',
      'dealership.fieldError.opening_hours_end_not_after_start',
      'dealership.fieldError.opening_hours_too_many_intervals',
      'dealership.fieldError.opening_hours_overlap',
      'dealership.fieldError.opening_hours_unknown_day',
      'dealership.fieldError.opening_hours_missing_day',
    ]
    for (const locale of ['tr', 'ar', 'en'] as const) {
      for (const anahtar of [...panelAnahtarlari, 'home.footer.closed', 'home.footer.hours']) {
        expect(catalogKeys(locale)).toContain(anahtar)
      }
      for (const anahtar of panelAnahtarlari) {
        expect(Object.keys(vitrin[locale])).not.toContain(anahtar)
      }
      expect(
        Object.keys(vitrin[locale]).filter((k) => k.startsWith('home.') && /closed|hours/.test(k)),
      ).toEqual(['home.footer.closed', 'home.footer.hours'])
    }
  })
})
