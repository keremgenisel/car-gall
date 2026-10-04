import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../LocaleProvider'
import { ApiError } from '../../shared/api/client'
import { AdminApp } from './AdminApp'
import type { CurrentSession } from '../../shared/api/types'

vi.mock('../../shared/api/identity', () => ({
  fetchCurrentSession: vi.fn(),
  fetchStaff: vi.fn(),
  addStaff: vi.fn(),
  logout: vi.fn(),
}))

const { fetchCurrentSession, fetchStaff } = await import('../../shared/api/identity')

function oturum(overrides: Partial<CurrentSession> = {}): CurrentSession {
  return {
    staffId: '0192c000-0000-7000-8000-00000000a001',
    displayName: 'Ayşe Yılmaz',
    role: 'manager',
    preferredLocale: 'tr',
    mustChangePassword: false,
    ...overrides,
  }
}

function ekran() {
  render(
    <LocaleProvider locale="tr">
      <AdminApp />
    </LocaleProvider>,
  )
}

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('AC-24: Oturumu olmayan biri yönetim ekranına gittiğinde giriş ekranına yönlendirilir', () => {
  it('401 giriş ekranını gösterir', async () => {
    vi.mocked(fetchCurrentSession).mockRejectedValue(new ApiError('identity.no_session', null, 401))

    ekran()

    expect(await screen.findByTestId('login-form')).toBeTruthy()
  })
})

describe('AC-27: `403` alan uç giriş ekranına yönlendirmez; personel bulunduğu ekranda kalır', () => {
  /**
   * C19'un ayrımı: kimlik VAR, yetki yok. `403` giriş ekranına götürseydi personel
   * giriş–yönlendirme döngüsüne girerdi — giriş yapar, yine 403 alır, yine giriş
   * ekranına düşerdi.
   */
  it('yetkisi olmayan rolde giriş ekranı GÖSTERİLMEZ', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ role: 'sales_advisor' }))
    vi.mocked(fetchStaff).mockRejectedValue(new ApiError('identity.no_session', null, 403))

    ekran()

    expect(await screen.findByTestId('admin-shell')).toBeTruthy()
    expect(screen.queryByTestId('login-form')).toBeNull()
  })

  it('yetkisi olmayan rol, hata değil yetki metni görür', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ role: 'content_editor' }))

    ekran()

    expect(await screen.findByTestId('admin-no-access')).toBeTruthy()

    // "Getirilemedi, tekrar dene" YALAN olurdu: istek başarısız olmadı, izin yok.
    expect(screen.queryByTestId('staff-error')).toBeNull()
    expect(screen.queryByTestId('add-staff-form')).toBeNull()
  })

  it('Yönetici personel yönetimini görür', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ role: 'manager' }))
    vi.mocked(fetchStaff).mockResolvedValue([])

    ekran()

    expect(await screen.findByTestId('admin-shell')).toBeTruthy()
    expect(screen.queryByTestId('admin-no-access')).toBeNull()
  })
})

describe('0008/AC-2: Satış Danışmanı rolündeki personel bu ekrana gittiğinde liste gelmez; ekran erişim yok durumunu gösterir ve giriş ekranına yönlendirilmez', () => {
  it('Satış Danışmanı listeyi görmez, giriş ekranına da gitmez', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ role: 'sales_advisor' }))

    ekran()

    expect(await screen.findByTestId('admin-no-access')).not.toBeNull()
    expect(screen.queryByTestId('staff-table')).toBeNull()
    expect(screen.queryByTestId('login-form')).toBeNull()
  })
})

describe('0008/AC-3: İçerik Editörü rolündeki personel için AC-2 ile aynı davranış geçerlidir', () => {
  it('İçerik Editörü listeyi görmez, giriş ekranına da gitmez', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ role: 'content_editor' }))

    ekran()

    expect(await screen.findByTestId('admin-no-access')).not.toBeNull()
    expect(screen.queryByTestId('staff-table')).toBeNull()
    expect(screen.queryByTestId('login-form')).toBeNull()
  })
})

describe('0008/AC-4: Yönetici olmayan personelin panelinde personel yönetimine götüren hiçbir bağlantı veya eylem bulunmaz', () => {
  /**
   * Kriter NEGATİF bir iddiadır: panelde bugün navigasyon yok, dolayısıyla "bağlantı
   * görünmez" tek başına boş bir kümeyi doğrulardı. Ölçülen şey, personel yönetimine
   * götüren HİÇBİR şeyin basılmaması — tablo, ekleme formu ve satır eylemleri dâhil.
   */
  it.each(['sales_advisor', 'content_editor'] as const)(
    '%s için personel yönetimine götüren hiçbir şey basılmaz',
    async (role) => {
      vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ role }))

      ekran()

      await screen.findByTestId('admin-no-access')

      expect(screen.queryByTestId('staff-table')).toBeNull()
      expect(screen.queryByTestId('add-staff-form')).toBeNull()
      expect(screen.queryByRole('link')).toBeNull()

      // Satır eylemleri `deactivate-`, `role-`, `password-` önekli test kimlikleri taşır.
      for (const prefix of ['deactivate-', 'role-', 'password-']) {
        expect(document.querySelector(`[data-testid^="${prefix}"]`), prefix).toBeNull()
      }
    },
  )
})

describe('AC-32: İlk parolasıyla giriş yapan personelden parolasını değiştirmesi istenir; değiştirmeden yönetim yüzeyinde çalışamaz', () => {
  it('zorunlu değişim bekleyen personel parola ekranını görür', async () => {
    vi.mocked(fetchCurrentSession).mockResolvedValue(oturum({ mustChangePassword: true }))

    ekran()

    expect(await screen.findByTestId('password-form')).toBeTruthy()
    expect(screen.queryByTestId('admin-shell')).toBeNull()
  })

  /**
   * Zorunlu değişim bekleyen personelin `/me` isteği de `403` alır (R10); uç bunu bir
   * yetki reddi değil, ZORUNLU ADIM olarak okur ve giriş ekranına DÜŞMEZ.
   */
  it('403 password_change_required parola ekranına götürür, giriş ekranına değil', async () => {
    vi.mocked(fetchCurrentSession).mockRejectedValue(
      new ApiError('identity.password_change_required', null, 403),
    )

    ekran()

    expect(await screen.findByTestId('password-form')).toBeTruthy()
    expect(screen.queryByTestId('login-form')).toBeNull()
  })
})
