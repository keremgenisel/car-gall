import { cleanup, render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { StaffListPage } from './StaffListPage'
import type { CurrentSession, StaffListItem } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'

vi.mock('../../shared/api/identity', () => ({
  fetchStaff: vi.fn(),
  addStaff: vi.fn(),
  deactivateStaff: vi.fn(),
  changeStaffRole: vi.fn(),
  resetStaffPassword: vi.fn(),
}))

const { fetchStaff } = await import('../../shared/api/identity')

const personel: StaffListItem = {
  staffId: '0192c000-0000-7000-8000-000000000001',
  displayName: 'Ayşe Yılmaz',
  loginId: 'ayse@ornek.com',
  role: 'manager',
  status: 'active',
  preferredLocale: 'tr',
  branchId: null,
  twoFactorEnrolled: true,
}

/** 0008 D1 — ekran kendi satırını oturumdaki kimlikten tanır. */
const oturum: CurrentSession = {
  staffId: '0192c000-0000-7000-8000-000000000001',
  displayName: 'Ayşe Yılmaz',
  role: 'manager',
  preferredLocale: 'tr',
  mustChangePassword: false,
  twoFactorEnrolled: true,
}

function ekran(locale: AppLocale = 'tr', session: CurrentSession = oturum) {
  render(
    <LocaleProvider locale={locale}>
      <StaffListPage session={session} />
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('AC-60: Personel listesi yüklenirken iskelet gösterilir; yüklenemediğinde tekrar dene eylemi sunulur', () => {
  it('yükleniyor durumunda iskelet gösterir', () => {
    vi.mocked(fetchStaff).mockReturnValue(new Promise(() => {}))

    ekran()

    expect(screen.getByTestId('staff-skeleton')).toBeTruthy()
  })

  it('hata durumunda tekrar dene sunar', async () => {
    vi.mocked(fetchStaff).mockRejectedValue(new ApiError('api.unavailable', null))

    ekran()

    expect(await screen.findByTestId('staff-error')).toBeTruthy()
    expect(screen.getByRole('button', { name: 'Tekrar dene' })).toBeTruthy()
  })

  it('boş listede ne yapılabileceğini söyler', async () => {
    vi.mocked(fetchStaff).mockResolvedValue([])

    ekran()

    expect(await screen.findByTestId('staff-empty')).toBeTruthy()
    expect(screen.getByText(/Ekibinizden birini ekleyerek/)).toBeTruthy()
  })
})

describe('AC-31: Yönetici bir personel eklediğinde personel listede görünür', () => {
  it('mutlu yolda personel satırı görünür', async () => {
    vi.mocked(fetchStaff).mockResolvedValue([personel])

    ekran()

    expect(await screen.findByTestId('staff-table')).toBeTruthy()
    expect(screen.getByText('Ayşe Yılmaz')).toBeTruthy()
    expect(screen.getByText('ayse@ornek.com')).toBeTruthy()
  })
})

describe('AC-52: Veritabanında rol ve durum kod olarak saklanır; çevrilmiş adları saklanmaz', () => {
  /**
   * Kriterin uç tarafındaki sonucu: sunucu KOD gönderir, görünen ad uçta çevrilir.
   * Ekranda kodun kendisi görünseydi hem çeviri kaybolur hem de kuralın anlamı kalmazdı.
   */
  it.each<[AppLocale, string, string]>([
    ['tr', 'Yönetici', 'Aktif'],
    ['en', 'Manager', 'Active'],
    ['ar', 'مدير', 'نشط'],
  ])('%s locale: rol ve durum kendi dilinde görünür', async (locale, rol, durum) => {
    vi.mocked(fetchStaff).mockResolvedValue([personel])

    ekran(locale)

    // Sorgu TABLOYLA sınırlı: aynı rol adı ekleme formunun seçeneklerinde de geçiyor
    // ve ikisi farklı şeyleri gösteriyor.
    const tablo = within(await screen.findByTestId('staff-table'))

    expect(tablo.getByText(rol)).toBeTruthy()
    expect(tablo.getByText(durum)).toBeTruthy()

    // Ham kod ekranda GÖRÜNMEZ — ne tabloda ne de seçeneklerin metninde.
    expect(document.body.textContent ?? '').not.toContain('sales_advisor')
    expect(document.body.textContent ?? '').not.toContain('content_editor')
    expect(tablo.queryByText('manager')).toBeNull()
  })
})

describe('AC-59: Hiçbir hata durumunda HTTP durum kodu, hata kodu, traceId, endpoint yolu veya istisna metni görünmez', () => {
  it('liste hatasında teknik ayrıntı basılmaz', async () => {
    vi.mocked(fetchStaff).mockRejectedValue(new ApiError('identity.no_session', 'trace-x', 401))

    ekran()
    await screen.findByTestId('staff-error')

    const metin = document.body.textContent ?? ''

    for (const isaret of ['401', 'identity.no_session', 'trace-x', '/api/admin']) {
      expect(metin).not.toContain(isaret)
    }
  })
})

describe('0008/AC-15: Ekran, listedeki kendi satırını ayırt eder', () => {
  it('kendi satırı işaretlenir, başkasınınki işaretlenmez', async () => {
    const baskasi: StaffListItem = {
      ...personel,
      staffId: '0192c000-0000-7000-8000-00000000b002',
      displayName: 'Mehmet Demir',
      loginId: 'mehmet@ornek.com',
    }

    vi.mocked(fetchStaff).mockResolvedValue([personel, baskasi])

    ekran()

    const kendi = await screen.findByTestId('staff-row-self')

    // Ayrım KİMLİKTEN geliyor, addan değil: ad tekil değil (D1).
    expect(within(kendi).getByText('Ayşe Yılmaz')).not.toBeNull()
    expect(screen.getAllByTestId('staff-row-self')).toHaveLength(1)

    // Renk tek başına anlam taşımaz: satır ayrıca METİNLE de işaretli.
    expect(within(kendi).getByText('Siz')).not.toBeNull()
  })
})

describe('0013/AC-54: Yönetici personel listesinde her satırın ikinci adım durumunu **rozetle** görür', () => {
  it('kurulumu olan ve olmayan satır farklı rozet taşır', async () => {
    vi.mocked(fetchStaff).mockResolvedValue([
      personel,
      {
        ...personel,
        staffId: '0192c000-0000-7000-8000-000000000002',
        displayName: 'Mehmet Demir',
        loginId: 'mehmet@ornek.com',
        twoFactorEnrolled: false,
      },
    ])

    ekran()

    await screen.findByTestId('staff-table')

    expect(screen.getByTestId(`staff-two-factor-${personel.staffId}`).textContent).toBe(
      'İki adım açık',
    )

    // Zorunlu rolde kurulumu olmayan satır ÜÇÜNCÜ hâli gösteriyor; rozet iki alandan
    // kuruluyor (D25) ve sözleşmede üçüncü bir alan yok.
    expect(
      screen.getByTestId('staff-two-factor-0192c000-0000-7000-8000-000000000002').textContent,
    ).toBe('İki adım gerekli')
  })
})
