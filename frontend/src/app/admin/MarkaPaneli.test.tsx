import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../LocaleProvider'
import { ApiError } from '../../shared/api/client'
import type { AdminBrand, AdminBrandDetail } from '../../shared/api/referenceAdmin'
import type { CurrentSession } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import type { AdminView } from '../../shared/lib/route'
import { AdminApp } from './AdminApp'

/**
 * `0021` — panelin marka ve model ekranları gerçek kabuğun içinde; yalnız API istemcileri
 * sahtelenir (docs/frontend.md § 8.1). Sunucunun kuralları (tekillik, kısaltma, `403`, `409`)
 * kendi testlerinde gerçek PostgreSQL üzerinde ölçülür; burada ölçülen, ekranın onlara
 * verdiği cevap.
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

vi.mock('../../shared/api/referenceAdmin', () => ({
  fetchAdminBrands: vi.fn(),
  fetchAdminBrand: vi.fn(),
  createBrand: vi.fn(),
  saveBrand: vi.fn(),
  runBrandAction: vi.fn(),
  setBrandShowOnHome: vi.fn(),
  createModel: vi.fn(),
  saveModel: vi.fn(),
  runModelAction: vi.fn(),
}))

const { fetchCurrentSession } = await import('../../shared/api/identity')
const api = await import('../../shared/api/referenceAdmin')

beforeAll(() => {
  Object.defineProperty(window, 'location', {
    configurable: true,
    value: { ...window.location, assign: vi.fn(), pathname: '/admin' },
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

function marka(ad: string, slug: string, ek: Partial<AdminBrand> = {}): AdminBrand {
  return {
    id: `id-${slug}`,
    canonicalName: ad,
    slug,
    isActive: true,
    showOnHome: false,
    version: 1,
    displayNames: {},
    modelCount: 2,
    ...ek,
  }
}

const MARKALAR: AdminBrand[] = [
  marka('Fiat', 'fiat', { isActive: false, modelCount: 0 }),
  marka('Toyota', 'toyota', { showOnHome: true, displayNames: { ar: 'تويوتا' }, version: 3 }),
]

const TOYOTA: AdminBrandDetail = {
  brand: MARKALAR[1],
  models: [
    {
      id: 'm-1',
      brandId: 'id-toyota',
      canonicalName: 'Corolla',
      isActive: true,
      version: 1,
      displayNames: { ar: 'كورولا' },
    },
    {
      id: 'm-2',
      brandId: 'id-toyota',
      canonicalName: 'Yaris',
      isActive: false,
      version: 2,
      displayNames: {},
    },
  ],
}

async function panel(
  view: AdminView,
  role = 'manager',
  locale: AppLocale = 'tr',
  brandId?: string,
) {
  vi.mocked(fetchCurrentSession).mockResolvedValue(oturum(role, locale))
  render(
    <LocaleProvider locale={locale}>
      <AdminApp view={view} brandId={brandId} />
    </LocaleProvider>,
  )
  await screen.findByRole('navigation')
}

beforeEach(() => {
  vi.mocked(api.fetchAdminBrands).mockResolvedValue(MARKALAR)
  vi.mocked(api.fetchAdminBrand).mockResolvedValue(TOYOTA)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0021/AC-6: Yönetici marka ekranında bütün markaları — aktif ve pasif — kanonik adları, durumları ve "ana sayfada göster" işaretleriyle görür.', () => {
  it('her satır kanonik adı, kısaltmayı, durumu ve işareti taşır', async () => {
    await panel('brands')

    const satirlar = await screen.findAllByTestId('brand-row')
    expect(satirlar).toHaveLength(2)
    expect(within(satirlar[0]).getByText('Fiat')).toBeTruthy()
    expect(within(satirlar[0]).getByText('Pasif')).toBeTruthy()
    expect(within(satirlar[0]).getByTestId<HTMLInputElement>('brand-home-toggle').checked).toBe(
      false,
    )
    expect(within(satirlar[1]).getByText('Toyota')).toBeTruthy()
    expect(within(satirlar[1]).getByText('Aktif')).toBeTruthy()
    expect(within(satirlar[1]).getByTestId<HTMLInputElement>('brand-home-toggle').checked).toBe(
      true,
    )
    expect(within(satirlar[1]).getByText('toyota')).toBeTruthy()
  })

  it('işareti değiştirmek okunan sürümle kaydeder ve listeyi yeniler', async () => {
    vi.mocked(api.setBrandShowOnHome).mockResolvedValue(4)
    await panel('brands')

    fireEvent.click(
      within((await screen.findAllByTestId('brand-row'))[1]).getByTestId('brand-home-toggle'),
    )

    await waitFor(() =>
      expect(api.setBrandShowOnHome).toHaveBeenCalledWith('id-toyota', 3, false, 'tr'),
    )
    await waitFor(() => expect(api.fetchAdminBrands).toHaveBeenCalledTimes(2))
  })
})

describe('0021/AC-7: Yönetici kanonik adı "Fiat" olan bir marka eklediğinde marka listede aktif görünür ve kısaltması `fiat` olur.', () => {
  it('eklenen marka sunucudan yeniden okunan listede görünür', async () => {
    vi.mocked(api.fetchAdminBrands).mockResolvedValueOnce([MARKALAR[1]])
    vi.mocked(api.createBrand).mockResolvedValue('id-fiat')
    vi.mocked(api.fetchAdminBrands).mockResolvedValueOnce([
      marka('Fiat', 'fiat', { modelCount: 0 }),
      MARKALAR[1],
    ])
    await panel('brands')
    await screen.findAllByTestId('brand-row')

    fireEvent.change(screen.getByLabelText('Kanonik ad (Latin harfleri)'), {
      target: { value: 'Fiat' },
    })
    fireEvent.click(screen.getByTestId('brand-new-submit'))

    expect(await screen.findByTestId('reference-saved')).toBeTruthy()
    expect(api.createBrand).toHaveBeenCalledWith({ canonicalName: 'Fiat' }, 'tr')
    // Liste yeniden OKUNANA kadar beklenir: kayıt bildirimi okuma bitmeden görünür ve eski
    // satırlarla erken bir sorgu sıraya bağlı kırmızı üretiyordu (art arda koşuda yakalandı).
    const satir = await waitFor(() => {
      const bulunan = screen
        .getAllByTestId('brand-row')
        .find((row) => within(row).queryByText('fiat') !== null)
      expect(bulunan).toBeTruthy()
      return bulunan!
    })
    expect(within(satir).getByText('Aktif')).toBeTruthy()
  })
})

describe('0021/AC-8: Var olan bir markanın kanonik adıyla — büyük-küçük harf farkı gözetmeden ("toyota") — marka eklemek reddedilir ve hata adın zaten kullanıldığını söyler.', () => {
  it('ad alanının altında "zaten var" der, kodu göstermez', async () => {
    vi.mocked(api.createBrand).mockRejectedValue(
      new ApiError('brand.name_taken', 'trace-1', 422, 'canonicalName'),
    )
    await panel('brands')

    fireEvent.change(screen.getByLabelText('Kanonik ad (Latin harfleri)'), {
      target: { value: 'toyota' },
    })
    fireEvent.click(screen.getByTestId('brand-new-submit'))

    expect((await screen.findByTestId('brand-new-name-error')).textContent).toBe(
      'Bu adla bir marka zaten var.',
    )
    expect(document.body.textContent).not.toContain('brand.name_taken')
    expect(document.body.textContent).not.toContain('trace-1')
  })
})

describe('0021/AC-9: "Alfa Romeo" varken "Alfa-Romeo" eklemek, ad farklı olsa da kısaltma çakıştığı için ayrı bir hata koduyla reddedilir (D19).', () => {
  it('kısaltma çakışması ad çakışmasından ayrı bir metin gösterir', async () => {
    vi.mocked(api.createBrand).mockRejectedValue(
      new ApiError('brand.slug_taken', null, 422, 'canonicalName'),
    )
    await panel('brands')

    fireEvent.change(screen.getByLabelText('Kanonik ad (Latin harfleri)'), {
      target: { value: 'Alfa-Romeo' },
    })
    fireEvent.click(screen.getByTestId('brand-new-submit'))

    expect((await screen.findByTestId('brand-new-name-error')).textContent).toContain(
      'adres kısaltmasını',
    )
  })
})

describe('0021/AC-10: ASCII olmayan ya da boş bir kanonik ad ("Škoda", "") reddedilir.', () => {
  it.each([
    ['brand.canonical_name_invalid', 'Latin harfleri'],
    ['brand.canonical_name_required', 'boş bırakılamaz'],
  ])('%s alanın altında açıklanır', async (kod, metin) => {
    vi.mocked(api.createBrand).mockRejectedValue(new ApiError(kod, null, 422, 'canonicalName'))
    await panel('brands')

    fireEvent.click(screen.getByTestId('brand-new-submit'))

    expect((await screen.findByTestId('brand-new-name-error')).textContent).toContain(metin)
  })
})

describe('0021/AC-12: Markanın kanonik adı düzeltildiğinde kısaltması değişmez.', () => {
  it('marka ekranı kısaltmayı salt okunur gösterir ve kaydetme gövdesi kısaltma taşımaz', async () => {
    vi.mocked(api.saveBrand).mockResolvedValue(4)
    await panel('brand-edit', 'manager', 'tr', 'id-toyota')

    expect((await screen.findByTestId('brand-slug')).textContent).toContain('toyota')
    fireEvent.change(
      within(screen.getByTestId('brand-form')).getByLabelText('Kanonik ad (Latin harfleri)'),
      {
        target: { value: 'Toyota Motor' },
      },
    )
    fireEvent.click(screen.getByTestId('brand-save'))

    await waitFor(() =>
      expect(api.saveBrand).toHaveBeenCalledWith(
        'id-toyota',
        3,
        { canonicalName: 'Toyota Motor', displayNames: { tr: '', ar: 'تويوتا', en: '' } },
        'tr',
      ),
    )
  })
})

describe('0021/AC-13: Yönetici bir markaya Arapça görünen ad ("فيات") verdiğinde Arapça vitrin onu gösterir; Türkçe vitrin kanonik adı gösterir.', () => {
  it('etkin her dil için görünen ad alanı vardır ve Arapça değer kaydedilir', async () => {
    vi.mocked(api.saveBrand).mockResolvedValue(4)
    await panel('brand-edit', 'manager', 'tr', 'id-toyota')

    const arapca = await screen.findByLabelText('العربية')
    expect(arapca.getAttribute('lang')).toBe('ar')
    fireEvent.change(arapca, { target: { value: 'فيات' } })
    fireEvent.click(screen.getByTestId('brand-save'))

    await waitFor(() =>
      expect(vi.mocked(api.saveBrand).mock.calls[0]?.[2].displayNames).toEqual({
        tr: '',
        ar: 'فيات',
        en: '',
      }),
    )
  })
})

describe('0021/AC-17: Yönetici "Fiat" markasına "Egea" modelini eklediğinde model o markanın altında aktif görünür.', () => {
  it('model markanın kimliğiyle eklenir ve ekran yeniden okunur', async () => {
    vi.mocked(api.createModel).mockResolvedValue('m-3')
    await panel('brand-edit', 'manager', 'tr', 'id-toyota')

    const form = await screen.findByTestId('model-new-form')
    fireEvent.change(within(form).getByLabelText('Kanonik ad (Latin harfleri)'), {
      target: { value: 'Egea' },
    })
    fireEvent.click(within(form).getByTestId('model-new-submit'))

    await waitFor(() =>
      expect(api.createModel).toHaveBeenCalledWith('id-toyota', { canonicalName: 'Egea' }, 'tr'),
    )
    await waitFor(() => expect(api.fetchAdminBrand).toHaveBeenCalledTimes(2))
    const satirlar = screen.getAllByTestId('model-row')
    expect(within(satirlar[0]).getByText('Aktif')).toBeTruthy()
    expect(within(satirlar[1]).getByText('Pasif')).toBeTruthy()
  })
})

describe('0021/AC-20: Modelin markası değiştirilemez: düzenleme ekranında marka seçimi sunulmaz ve uç markayı değiştiren isteği reddeder.', () => {
  it('model düzenleme formunda marka seçicisi yoktur ve gövde marka taşımaz', async () => {
    vi.mocked(api.saveModel).mockResolvedValue(2)
    await panel('brand-edit', 'manager', 'tr', 'id-toyota')

    fireEvent.click((await screen.findAllByTestId('model-edit'))[0])
    const form = screen.getByTestId('model-form')
    expect(within(form).queryByRole('combobox')).toBeNull()
    expect(within(form).getByTestId('model-brand-fixed').textContent).toContain('değiştirilemez')
    fireEvent.click(within(form).getByTestId('model-save'))

    await waitFor(() => expect(api.saveModel).toHaveBeenCalled())
    expect(Object.keys(vi.mocked(api.saveModel).mock.calls[0]?.[2] ?? {})).not.toContain('brandId')
  })
})

describe('0021/AC-52: Satış Danışmanı ve İçerik Editörü panelde marka/model ekranına bağlantı görmez; adresi doğrudan açtıklarında "erişim yok" ekranını görür.', () => {
  it.each(['sales_advisor', 'content_editor'])(
    '%s bağlantıyı görmez ve "erişim yok" görür',
    async (rol) => {
      await panel('brands', rol)

      expect(screen.queryByRole('link', { name: 'Markalar ve modeller' })).toBeNull()
      expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
      expect(api.fetchAdminBrands).not.toHaveBeenCalled()
      cleanup()

      await panel('brand-edit', rol, 'tr', 'id-toyota')
      expect(await screen.findByTestId('admin-no-access')).toBeTruthy()
      expect(api.fetchAdminBrand).not.toHaveBeenCalled()
    },
  )

  it('Yönetici bağlantıyı görür', async () => {
    await panel('brands')

    expect(
      screen.getByRole('link', { name: 'Markalar ve modeller' }).getAttribute('aria-current'),
    ).toBe('page')
  })
})

describe('0021/AC-56: Marka/model ekranı veri gelirken yükleniyor, okunamazken teknik ayrıntı göstermeyen hata durumu ve yeniden deneme gösterir; hiç marka yokken boş durumu gösterir.', () => {
  it('yükleniyor', async () => {
    vi.mocked(api.fetchAdminBrands).mockReturnValue(new Promise(() => {}))
    await panel('brands')

    expect(await screen.findByTestId('brands-loading')).toBeTruthy()
  })

  it('hata ve tekrar dene; teknik ayrıntı yok', async () => {
    vi.mocked(api.fetchAdminBrands).mockRejectedValueOnce(
      new ApiError('api.unavailable', 'trace-9', 503),
    )
    await panel('brands')

    const hata = await screen.findByTestId('brands-error')
    expect(hata.textContent).not.toContain('503')
    expect(hata.textContent).not.toContain('trace-9')
    fireEvent.click(within(hata).getByRole('button', { name: 'Tekrar dene' }))
    expect(await screen.findAllByTestId('brand-row')).toHaveLength(2)
  })

  it('boş', async () => {
    vi.mocked(api.fetchAdminBrands).mockResolvedValue([])
    await panel('brands')

    expect(await screen.findByTestId('brands-empty')).toBeTruthy()
    expect(screen.getByTestId('brand-new-form')).toBeTruthy()
  })

  it('markanın modeli yokken boş durum', async () => {
    vi.mocked(api.fetchAdminBrand).mockResolvedValue({ brand: MARKALAR[0], models: [] })
    await panel('brand-edit', 'manager', 'tr', 'id-fiat')

    expect(await screen.findByTestId('models-empty')).toBeTruthy()
  })

  it('eski sürüm reddinde güncel hâli yükleme yolu sunar', async () => {
    vi.mocked(api.runBrandAction).mockRejectedValue(new ApiError('concurrency.lost', null, 409))
    await panel('brand-edit', 'manager', 'tr', 'id-toyota')

    fireEvent.click(await screen.findByTestId('brand-toggle-active'))

    expect(await screen.findByTestId('reference-conflict')).toBeTruthy()
  })
})

describe('0021/AC-58: Arapça panelde marka/model ekranı sağdan sola çalışır; kanonik ad alanı soldan sağa yazılır.', () => {
  it('Arapça metinler ve soldan sağa kanonik ad', async () => {
    await panel('brand-edit', 'manager', 'ar', 'id-toyota')

    const ad = within(await screen.findByTestId('brand-form')).getByLabelText(
      'الاسم الأساسي (بأحرف لاتينية)',
    )
    expect(ad.getAttribute('dir')).toBe('ltr')
    expect(screen.getByRole('heading', { level: 1 }).getAttribute('dir')).toBe('ltr')
    expect(screen.getByText('الطرازات')).toBeTruthy()
  })
})
