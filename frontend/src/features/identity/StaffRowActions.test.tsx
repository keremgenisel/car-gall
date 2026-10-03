import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
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

const { fetchStaff, deactivateStaff, changeStaffRole, resetStaffPassword } =
  await import('../../shared/api/identity')

const BEN = '0192c000-0000-7000-8000-00000000a001'
const BASKASI = '0192c000-0000-7000-8000-00000000b002'

const oturum: CurrentSession = {
  staffId: BEN,
  displayName: 'Ayşe Yılmaz',
  role: 'manager',
  preferredLocale: 'tr',
  mustChangePassword: false,
}

function personel(staffId: string, displayName: string): StaffListItem {
  return {
    staffId,
    displayName,
    loginId: `${displayName.toLowerCase()}@ornek.com`,
    role: 'manager',
    status: 'active',
    preferredLocale: 'tr',
    branchId: null,
  }
}

const LISTE = [personel(BEN, 'Ayse'), personel(BASKASI, 'Mehmet')]

/**
 * Ekran gerçek API client'ı SAHTELENMİŞ hâlde monte ediliyor (docs/frontend.md § 8.1);
 * sorgular kullanıcının gördüğü şeye göre yapılıyor — rol, erişilebilir ad, metin.
 */
async function ekran(locale: AppLocale = 'tr') {
  vi.mocked(fetchStaff).mockResolvedValue(LISTE)

  render(
    <LocaleProvider locale={locale}>
      <StaffListPage session={oturum} />
    </LocaleProvider>,
  )

  await screen.findByTestId('staff-table')
}

function ac(testId: string) {
  fireEvent.click(screen.getByTestId(testId))
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0008/AC-1: Yönetici, personel listesinde her satır için pasifleştirme, rol değiştirme ve parola sıfırlama eylemlerini görür', () => {
  it('her satırda üç eylem bulunur', async () => {
    await ekran()

    for (const staffId of [BEN, BASKASI]) {
      expect(screen.getByTestId(`deactivate-${staffId}`)).not.toBeNull()
      expect(screen.getByTestId(`role-${staffId}`)).not.toBeNull()
      expect(screen.getByTestId(`password-${staffId}`)).not.toBeNull()
    }
  })
})

describe('0008/AC-5: Pasifleştirme eylemi onay ister; onay metni hangi personelin pasifleştirileceğini adıyla yazar', () => {
  it('onay kutusu personelin adını yazar', async () => {
    await ekran()

    ac(`deactivate-${BASKASI}`)

    const kutu = screen.getByTestId('confirm-deactivate')

    expect(within(kutu).getByText(/Mehmet/)).not.toBeNull()
    expect(vi.mocked(deactivateStaff)).not.toHaveBeenCalled()
  })
})

describe('0008/AC-6: Onay verildiğinde personel pasifleşir ve liste personelin yeni durumunu gösterir', () => {
  it('onaydan sonra istek gider ve liste tazelenir', async () => {
    await ekran()
    vi.mocked(deactivateStaff).mockResolvedValue(null)

    ac(`deactivate-${BASKASI}`)
    ac('confirm-deactivate-submit')

    await waitFor(() => expect(vi.mocked(deactivateStaff)).toHaveBeenCalledWith(BASKASI, 'tr'))
    await waitFor(() => expect(vi.mocked(fetchStaff).mock.calls.length).toBeGreaterThan(1))
  })
})

describe('0008/AC-7: Onay iptal edildiğinde hiçbir istek gönderilmez ve liste değişmez', () => {
  it('vazgeçmek istek üretmez', async () => {
    await ekran()

    const oncekiCekis = vi.mocked(fetchStaff).mock.calls.length

    ac(`deactivate-${BASKASI}`)
    ac('confirm-deactivate-cancel')

    expect(screen.queryByTestId('confirm-deactivate')).toBeNull()
    expect(vi.mocked(deactivateStaff)).not.toHaveBeenCalled()
    expect(vi.mocked(fetchStaff).mock.calls.length).toBe(oncekiCekis)
  })
})

describe('0008/AC-8: Yönetici bir personelin rolünü değiştirdiğinde liste yeni rolü gösterir', () => {
  it('seçilen rol istekte gider ve liste tazelenir', async () => {
    await ekran()
    vi.mocked(changeStaffRole).mockResolvedValue(null)

    ac(`role-${BASKASI}`)
    // Sayfada iki "Rol" etiketi var: kutununki ve ekleme formununki. Sorgu kutuyla sınırlı.
    fireEvent.change(within(screen.getByTestId('confirm-role')).getByLabelText('Rol'), {
      target: { value: 'sales_advisor' },
    })
    ac('confirm-role-submit')

    await waitFor(() =>
      expect(vi.mocked(changeStaffRole)).toHaveBeenCalledWith(BASKASI, 'sales_advisor', 'tr'),
    )
    await waitFor(() => expect(vi.mocked(fetchStaff).mock.calls.length).toBeGreaterThan(1))
  })
})

describe('0008/AC-9: Yönetici kendi rolünü düşürmeye çalıştığında onay istenir ve onay metni işlemden sonra panele erişemeyeceğini söyler', () => {
  it('kendi rolünü düşürürken uyarı çıkar', async () => {
    await ekran()

    ac(`role-${BEN}`)

    // Yönetici kaldığı sürece uyarı YOK: sonucu ağır olan, rolden çıkmak.
    expect(screen.queryByTestId('confirm-self-warning')).toBeNull()

    fireEvent.change(within(screen.getByTestId('confirm-role')).getByLabelText('Rol'), {
      target: { value: 'content_editor' },
    })

    expect(screen.getByTestId('confirm-self-warning').textContent).toContain('erişemeyeceksiniz')
  })

  it('başkasının rolünü düşürürken uyarı ÇIKMAZ', async () => {
    await ekran()

    ac(`role-${BASKASI}`)
    fireEvent.change(within(screen.getByTestId('confirm-role')).getByLabelText('Rol'), {
      target: { value: 'content_editor' },
    })

    expect(screen.queryByTestId('confirm-self-warning')).toBeNull()
  })
})

describe('0008/AC-10: Tanınmayan bir rol değeriyle gönderilen istek reddedilir ve ekran genel hata mesajını gösterir; hata kodu ziyaretçiye görünmez', () => {
  it('sunucu reddi genel mesaja düşer', async () => {
    await ekran()
    vi.mocked(changeStaffRole).mockRejectedValue(
      new ApiError('identity.staff_not_found', 'role', 404),
    )

    ac(`role-${BASKASI}`)
    ac('confirm-role-submit')

    const hata = await screen.findByTestId('staff-action-error')

    expect(hata.textContent).toContain('İşlem tamamlanamadı')
    expect(hata.textContent).not.toContain('identity.')
    expect(hata.textContent).not.toContain('404')
  })
})

describe('0008/AC-11: Parola sıfırlama, Yöneticinin belirlediği yeni parolayı ister ve kaydetmeden önce aynı parolayı bir kez daha doğrulatır', () => {
  it('iki parola alanı vardır ve eşleşenler gönderilir', async () => {
    await ekran()
    vi.mocked(resetStaffPassword).mockResolvedValue(null)

    ac(`password-${BASKASI}`)
    fireEvent.change(screen.getByLabelText('Yeni parola'), {
      target: { value: 'yeni-parola-1234' },
    })
    fireEvent.change(screen.getByLabelText('Yeni parola (tekrar)'), {
      target: { value: 'yeni-parola-1234' },
    })
    ac('confirm-password-submit')

    await waitFor(() =>
      expect(vi.mocked(resetStaffPassword)).toHaveBeenCalledWith(BASKASI, 'yeni-parola-1234', 'tr'),
    )
  })
})

describe('0008/AC-12: İki parola alanı eşleşmediğinde istek gönderilmez ve hata alanda gösterilir', () => {
  it('eşleşmeyen parolalar istek üretmez', async () => {
    await ekran()

    ac(`password-${BASKASI}`)
    fireEvent.change(screen.getByLabelText('Yeni parola'), { target: { value: 'birinci-1234' } })
    fireEvent.change(screen.getByLabelText('Yeni parola (tekrar)'), {
      target: { value: 'ikinci-1234' },
    })
    ac('confirm-password-submit')

    expect(vi.mocked(resetStaffPassword)).not.toHaveBeenCalled()
    expect(screen.getByText('İki parola aynı değil.')).not.toBeNull()
    expect(screen.getByLabelText('Yeni parola').getAttribute('aria-invalid')).toBe('true')
  })
})

describe('0008/AC-13: İşlem kaydedildikten sonra yeni parola ekranda tutulmaz', () => {
  it('başarıdan sonra kutu kapanır ve alanlar boşalır', async () => {
    await ekran()
    vi.mocked(resetStaffPassword).mockResolvedValue(null)

    ac(`password-${BASKASI}`)
    fireEvent.change(screen.getByLabelText('Yeni parola'), {
      target: { value: 'yeni-parola-1234' },
    })
    fireEvent.change(screen.getByLabelText('Yeni parola (tekrar)'), {
      target: { value: 'yeni-parola-1234' },
    })
    ac('confirm-password-submit')

    await waitFor(() => expect(screen.queryByTestId('confirm-password')).toBeNull())

    // Başarıdan sonra liste tazeleniyor; tablo yeniden basılana kadar beklenir.
    await screen.findByTestId(`password-${BASKASI}`)

    // Yeniden açıldığında alanlar boş: parola bellekte tutulmuyor.
    ac(`password-${BASKASI}`)
    expect((screen.getByLabelText('Yeni parola') as HTMLInputElement).value).toBe('')
  })
})

describe('0008/AC-14: Yapılandırılan asgari uzunluktan kısa bir parola ile işlem reddedilir ve hata ziyaretçinin dilinde gösterilir', () => {
  it('sunucunun kısa parola reddi ziyaretçinin dilinde gösterilir', async () => {
    await ekran()
    vi.mocked(resetStaffPassword).mockRejectedValue(
      new ApiError('identity.password_too_short', 'password', 422),
    )

    ac(`password-${BASKASI}`)
    fireEvent.change(screen.getByLabelText('Yeni parola'), { target: { value: 'kisa' } })
    fireEvent.change(screen.getByLabelText('Yeni parola (tekrar)'), { target: { value: 'kisa' } })
    ac('confirm-password-submit')

    const hata = await screen.findByTestId('staff-action-error')

    expect(hata.textContent).toContain('Parola çok kısa.')
  })
})

describe('0008/AC-16: Yönetici kendi hesabını pasifleştirmeye çalıştığında onay metni işlemden sonra panele erişemeyeceğini söyler', () => {
  it('kendi satırında uyarı çıkar, başkasının satırında çıkmaz', async () => {
    await ekran()

    ac(`deactivate-${BEN}`)
    expect(screen.getByTestId('confirm-self-warning').textContent).toContain('erişemeyeceksiniz')

    ac('confirm-deactivate-cancel')

    ac(`deactivate-${BASKASI}`)
    expect(screen.queryByTestId('confirm-self-warning')).toBeNull()
  })
})

describe('0008/AC-23: Başarılı bir işlemden sonra liste tazelenir; odak işlem yapılan satıra döner ve tablonun kaydırma konumu korunur', () => {
  it('odak işlem yapılan satırın eylemine döner', async () => {
    await ekran()
    vi.mocked(deactivateStaff).mockResolvedValue(null)

    ac(`deactivate-${BASKASI}`)
    ac('confirm-deactivate-submit')

    await waitFor(() =>
      expect(document.activeElement).toBe(screen.getByTestId(`deactivate-${BASKASI}`)),
    )
  })
})

describe('0008/AC-24: Kural gereği reddedilen bir işlemden sonra liste tazelenmez; seçim ve kaydırma konumu korunur, liste başa sarmaz', () => {
  it('kural gereği red listeyi yeniden ÇEKMEZ', async () => {
    await ekran()
    vi.mocked(deactivateStaff).mockRejectedValue(
      new ApiError('identity.last_active_manager', null, 409),
    )

    const oncekiCekis = vi.mocked(fetchStaff).mock.calls.length

    ac(`deactivate-${BEN}`)
    ac('confirm-deactivate-submit')

    const hata = await screen.findByTestId('staff-action-error')

    expect(hata.textContent).toContain('en az bir aktif Yönetici')
    expect(hata.textContent).not.toContain('tekrar')
    expect(vi.mocked(fetchStaff).mock.calls.length).toBe(oncekiCekis)
  })
})

describe('0008/AC-25: Eşzamanlılık kaybında liste tazelenir, hata mesajı görünür kalır ve seçim ile odak korunur', () => {
  it('eşzamanlılık kaybı listeyi tazeler ve mesaj görünür kalır', async () => {
    await ekran()
    vi.mocked(deactivateStaff).mockRejectedValue(new ApiError('concurrency.lost', null, 409))

    ac(`deactivate-${BASKASI}`)
    ac('confirm-deactivate-submit')

    const hata = await screen.findByTestId('staff-action-error')

    expect(hata.textContent).toContain('tekrar deneyin')
    await waitFor(() => expect(vi.mocked(fetchStaff).mock.calls.length).toBeGreaterThan(1))

    // Tazeleme mesajı SİLMEZ: bayat olan veriydi, mesaj değil.
    expect(screen.queryByTestId('staff-action-error')).not.toBeNull()
  })
})

describe('0008/AC-26: Bir işlem sürerken o eylemin düğmesi yükleniyor hâline geçer ve aynı satır için ikinci istek gönderilmez', () => {
  it('bekleyen istek sırasında ikinci gönderim olmaz', async () => {
    await ekran()

    let cozumle: (() => void) | undefined
    vi.mocked(deactivateStaff).mockReturnValue(
      new Promise((resolve) => {
        cozumle = () => resolve(null)
      }),
    )

    ac(`deactivate-${BASKASI}`)
    ac('confirm-deactivate-submit')

    await waitFor(() =>
      expect(screen.getByTestId(`deactivate-${BASKASI}`).getAttribute('aria-busy')).toBe('true'),
    )

    ac('confirm-deactivate-submit')
    ac('confirm-deactivate-submit')

    expect(vi.mocked(deactivateStaff)).toHaveBeenCalledTimes(1)

    cozumle?.()
  })
})

describe('0008/AC-33: Aynı personel başka biri tarafından zaten pasifleştirilmişken gelen istek kendi mesajını gösterir, tekrar denemeyi önermez ve liste tazelenir', () => {
  it('zaten pasifleştirilmiş: kendi mesajı, tazeleme var, tekrar dene yok', async () => {
    await ekran()
    vi.mocked(deactivateStaff).mockRejectedValue(
      new ApiError('identity.already_deactivated', null, 422),
    )

    ac(`deactivate-${BASKASI}`)
    ac('confirm-deactivate-submit')

    const hata = await screen.findByTestId('staff-action-error')

    expect(hata.textContent).toContain('zaten pasifleştirilmiş')
    expect(hata.textContent).not.toContain('tekrar')
    await waitFor(() => expect(vi.mocked(fetchStaff).mock.calls.length).toBeGreaterThan(1))
  })
})
