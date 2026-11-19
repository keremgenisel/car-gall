import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../../app/LocaleProvider'
import { ApiError } from '../../../shared/api/client'
import type { SaleReversalOverview } from '../../../shared/api/types'
import { catalogKeys } from '../../../shared/i18n/i18n'
import { SUPPORTED_LOCALES, type AppLocale } from '../../../shared/lib/locale'
import { BulkActionBar } from './BulkActionBar'
import { ListingActionBar } from './ListingActionBar'
import { SaleReversalHistory } from './SaleReversalHistory'
import { canConfirm, canRevertSale, revertFailure } from './saleReversalState'

/**
 * `0023` — satışı geri almanın ekranı: eylemin görünürlüğü (R12), onay penceresi (R9),
 * hata durumu (AC-31) ve kayıt listesi (R7). `docs/frontend.md` § 8.1: gerçek API
 * istemcisi sahtelenmiş hâlde monte ediliyor.
 */
vi.mock('../../../shared/api/catalogAdmin', () => ({
  applyListingAction: vi.fn(),
  applyBulkListingAction: vi.fn(),
  fetchSaleReversals: vi.fn(),
  revertListingSale: vi.fn(),
}))

const { fetchSaleReversals, revertListingSale } = await import('../../../shared/api/catalogAdmin')

const ARAC = '0192c000-0000-7000-8000-000000000001'
const GEREKCE = 'Yanlış araca tıklandı, satış başka araçtaydı.'

function onizleme(overrides: Partial<SaleReversalOverview> = {}): SaleReversalOverview {
  return {
    listingStatus: 'Sold',
    targetStatus: 'Reserved',
    draftReasons: [],
    missingFields: [],
    records: [],
    ...overrides,
  }
}

function cubuk(role: string, options: { locale?: AppLocale; status?: string } = {}) {
  const uygulandi = vi.fn()

  render(
    <LocaleProvider locale={options.locale ?? 'tr'}>
      <ListingActionBar
        vehicleId={ARAC}
        listingStatus={options.status ?? 'Sold'}
        awaitingBranchAssignment={false}
        staffRole={role}
        onApplied={uygulandi}
      />
    </LocaleProvider>,
  )

  return uygulandi
}

async function pencereyiAc() {
  fireEvent.click(screen.getByTestId('vehicle-action-revert-sale'))

  return await screen.findByTestId('sale-revert-target')
}

function gerekceYaz(value: string) {
  fireEvent.change(screen.getByTestId('sale-revert-reason'), { target: { value } })
}

function onayDugmesi(): HTMLButtonElement {
  return screen.getByTestId('sale-revert-confirm') as HTMLButtonElement
}

beforeEach(() => {
  // jsdom `showModal`'ı uygulamıyor; yerel davranışın taklidi `open` özniteliği.
  HTMLDialogElement.prototype.showModal ??= function showModal(this: HTMLDialogElement) {
    this.setAttribute('open', '')
  }
  HTMLDialogElement.prototype.close ??= function close(this: HTMLDialogElement) {
    this.removeAttribute('open')
    this.dispatchEvent(new Event('close'))
  }

  vi.mocked(fetchSaleReversals).mockResolvedValue(onizleme())
  vi.mocked(revertListingSale).mockResolvedValue(undefined)
})

afterEach(() => {
  cleanup()
  vi.clearAllMocks()
})

describe('0023/AC-6: Geri alınan ilan, onay penceresinde gösterilen duruma döner', () => {
  it('istek pencerenin gösterdiği hedefi taşır', async () => {
    const uygulandi = cubuk('manager')

    const hedef = await pencereyiAc()
    gerekceYaz(GEREKCE)
    fireEvent.click(onayDugmesi())

    expect(hedef.textContent).toContain('Rezerve')
    await waitFor(() =>
      expect(vi.mocked(revertListingSale)).toHaveBeenCalledWith(ARAC, GEREKCE, 'Reserved', 'tr'),
    )
    expect(uygulandi).toHaveBeenCalled()
    expect(await screen.findByTestId('vehicle-action-done')).toBeTruthy()
  })
})

describe('0023/AC-7: Onay penceresi açıldıktan sonra aracın şubesi kalıcı kapatılırsa, pencerenin gösterdiği "Yayında" hedefiyle gönderilen geri alma `409` ile reddedilir, ilan Satıldı kalır ve kayıt yazılmaz (D16)', () => {
  it('pencere kapanmaz, güncel hedefi ve nedenini gösterir, gerekçe kaybolmaz', async () => {
    vi.mocked(fetchSaleReversals)
      .mockResolvedValueOnce(onizleme({ targetStatus: 'Published' }))
      .mockResolvedValueOnce(
        onizleme({
          targetStatus: 'Draft',
          draftReasons: ['vehicle.cannot_publish.branch_unassigned'],
        }),
      )
    vi.mocked(revertListingSale)
      .mockRejectedValueOnce(
        new ApiError('listing.sale_reversal.target_changed', null, 409, null, {
          targetStatus: 'Draft',
        }),
      )
      .mockResolvedValueOnce(undefined)
    const uygulandi = cubuk('manager')

    expect((await pencereyiAc()).textContent).toContain('Yayında')
    gerekceYaz(GEREKCE)
    fireEvent.click(onayDugmesi())

    expect(await screen.findByTestId('sale-revert-target-changed')).toBeTruthy()
    await waitFor(() =>
      expect(screen.getByTestId('sale-revert-target').textContent).toContain('Taslak'),
    )
    expect(screen.getByTestId('sale-revert-draft-reasons').textContent).toContain(
      'şubesi kapatılmış',
    )
    expect((screen.getByTestId('sale-revert-reason') as HTMLTextAreaElement).value).toBe(GEREKCE)
    expect(uygulandi).not.toHaveBeenCalled()

    fireEvent.click(onayDugmesi())

    await waitFor(() =>
      expect(vi.mocked(revertListingSale)).toHaveBeenLastCalledWith(ARAC, GEREKCE, 'Draft', 'tr'),
    )
  })
})

describe('0023/AC-13: Geri alma onay penceresi ilanın döneceği durumu ve bir gerekçe alanı gösterir; gerekçesiz onay düğmesi etkin değildir', () => {
  it('hedef durum ve etiketli gerekçe alanı basılır; boş, boşluk ve kısa gerekçede düğme kapalı', async () => {
    cubuk('manager')

    const hedef = await pencereyiAc()

    expect(hedef.textContent).toContain('Rezerve')
    expect(screen.getByLabelText('Gerekçe')).toBe(screen.getByTestId('sale-revert-reason'))
    expect(onayDugmesi().disabled).toBe(true)

    gerekceYaz('          ')
    expect(onayDugmesi().disabled).toBe(true)

    gerekceYaz('dokuz kar')
    expect(onayDugmesi().disabled).toBe(true)

    gerekceYaz(GEREKCE)
    expect(onayDugmesi().disabled).toBe(false)
  })

  it('sınırlar sunucuyla aynı ve kırpılmış metne uygulanır', () => {
    expect(canConfirm('')).toBe(false)
    expect(canConfirm(' '.repeat(20))).toBe(false)
    expect(canConfirm('  123456789  ')).toBe(false)
    expect(canConfirm('0123456789')).toBe(true)
    expect(canConfirm('a'.repeat(500))).toBe(true)
    expect(canConfirm('a'.repeat(501))).toBe(false)
  })
})

describe('0023/AC-17: Onaydan vazgeçmek ilanı değiştirmez ve kayıt yazmaz', () => {
  it('vazgeç pencereyi kapatır ve hiçbir istek göndermez', async () => {
    const uygulandi = cubuk('manager')

    await pencereyiAc()
    gerekceYaz(GEREKCE)
    fireEvent.click(screen.getByTestId('sale-revert-cancel'))

    await waitFor(() => expect(screen.queryByTestId('sale-revert-form')).toBeNull())
    expect(vi.mocked(revertListingSale)).not.toHaveBeenCalled()
    expect(uygulandi).not.toHaveBeenCalled()
  })
})

describe('0023/AC-20: Yönetici ilanın panel ekranında geri alma kayıtlarını en yenisi üstte, personel adı, an, gerekçe ve dönülen durumla görür', () => {
  it('kayıtlar sunucunun sırasıyla, dört bilgiyle basılır', async () => {
    vi.mocked(fetchSaleReversals).mockResolvedValue(
      onizleme({
        listingStatus: 'Reserved',
        targetStatus: null,
        records: [
          {
            id: 'k2',
            staffDisplayName: 'Ayşe Yönetici',
            revertedAt: '2026-11-18T16:00:00Z',
            reason: 'İkinci yanlış tıklama.',
            restoredStatus: 'Reserved',
          },
          {
            id: 'k1',
            staffDisplayName: 'Mehmet Yönetici',
            revertedAt: '2026-11-18T14:00:00Z',
            reason: 'Birinci yanlış tıklama.',
            restoredStatus: 'Published',
          },
        ],
      }),
    )

    render(
      <LocaleProvider locale="tr">
        <SaleReversalHistory vehicleId={ARAC} refreshToken={0} />
      </LocaleProvider>,
    )

    const kayitlar = await screen.findAllByTestId('sale-reversal-record')

    expect(kayitlar).toHaveLength(2)
    expect(kayitlar[0]!.textContent).toContain('Ayşe Yönetici')
    expect(kayitlar[0]!.textContent).toContain('İkinci yanlış tıklama.')
    expect(kayitlar[0]!.textContent).toContain('Rezerve')
    expect(
      within(kayitlar[0]!)
        .getByText(/2026|18/u, { selector: 'time' })
        .getAttribute('dateTime'),
    ).toBe('2026-11-18T16:00:00Z')
    expect(kayitlar[1]!.textContent).toContain('Mehmet Yönetici')
    expect(kayitlar[1]!.textContent).toContain('Yayında')
  })

  it('boş ve hata durumları ayrı basılır; hatada tekrar dene var', async () => {
    vi.mocked(fetchSaleReversals).mockResolvedValueOnce(onizleme({ targetStatus: null }))

    render(
      <LocaleProvider locale="tr">
        <SaleReversalHistory vehicleId={ARAC} refreshToken={0} />
      </LocaleProvider>,
    )

    expect((await screen.findByTestId('sale-reversals-empty')).textContent).toContain(
      'hiç geri alınmadı',
    )
    cleanup()

    vi.mocked(fetchSaleReversals).mockRejectedValueOnce(
      new ApiError('api.unavailable', 'trace-1', 500),
    )

    render(
      <LocaleProvider locale="tr">
        <SaleReversalHistory vehicleId={ARAC} refreshToken={0} />
      </LocaleProvider>,
    )

    const hata = await screen.findByTestId('sale-reversals-error')
    expect(hata.textContent).not.toContain('api.unavailable')
    expect(hata.textContent).not.toContain('trace-1')
    expect(within(hata).getByRole('button')).toBeTruthy()
  })
})

describe('0023/AC-28: Satış Danışmanı ve İçerik Editörü satılmış ilanda "Satışı geri al" eylemini görmez', () => {
  it.each(['sales_advisor', 'content_editor'])(
    '%s düğmeyi görmez, kimin yapabileceği söylenir',
    (role) => {
      cubuk(role)

      expect(screen.queryByTestId('vehicle-action-revert-sale')).toBeNull()
      expect(screen.queryByText('Satışı geri al')).toBeNull()
      expect(screen.getByTestId('vehicle-sold-terminal').textContent).toContain('Yönetici')
      expect(vi.mocked(fetchSaleReversals)).not.toHaveBeenCalled()
    },
  )

  it('Yönetici satılmış ilanda görür; satılmamış ilanda kimse görmez', () => {
    expect(canRevertSale('manager', 'Sold')).toBe(true)
    expect(canRevertSale('manager', 'Published')).toBe(false)
    expect(canRevertSale('manager', 'Archived')).toBe(false)
    expect(canRevertSale('sales_advisor', 'Sold')).toBe(false)
    expect(canRevertSale('content_editor', 'Sold')).toBe(false)

    cubuk('manager')

    expect(screen.getByTestId('vehicle-action-revert-sale').textContent).toBe('Satışı geri al')
  })
})

describe('0023/AC-30: Toplu eylem çubuğunda "Satışı geri al" seçeneği yoktur', () => {
  it('toplu eylem seçeneklerinde geri alma yok', () => {
    render(
      <LocaleProvider locale="tr">
        <BulkActionBar
          selection={[ARAC]}
          labelOf={() => 'Toyota Corolla · 2019'}
          onApplied={vi.fn()}
        />
      </LocaleProvider>,
    )

    const secenekler = [
      ...(screen.getByTestId('vehicle-bulk-operation') as HTMLSelectElement).options,
    ]

    expect(secenekler.length).toBeGreaterThan(0)
    expect(secenekler.map((option) => option.value)).not.toContain('revert-sale')
    expect(secenekler.map((option) => option.textContent)).not.toContain('Satışı geri al')
  })
})

describe('0023/AC-31: Geri alma sunucu hatasıyla düştüğünde pencere teknik ayrıntı göstermeyen bir hata gösterir ve yazılan gerekçe kaybolmaz', () => {
  it('500 hatası çevrilmiş genel mesajla basılır; kod ve iz kimliği yok, gerekçe duruyor', async () => {
    vi.mocked(revertListingSale).mockRejectedValueOnce(
      new ApiError('api.unavailable', 'trace-xyz', 500),
    )
    cubuk('manager')

    await pencereyiAc()
    gerekceYaz(GEREKCE)
    fireEvent.click(onayDugmesi())

    const hata = await screen.findByTestId('sale-revert-error')

    expect(hata.getAttribute('role')).toBe('alert')
    expect(hata.textContent).toContain('tamamlanamadı')
    expect(hata.textContent).not.toContain('api.unavailable')
    expect(hata.textContent).not.toContain('trace-xyz')
    expect(hata.textContent).not.toContain('500')
    expect((screen.getByTestId('sale-revert-reason') as HTMLTextAreaElement).value).toBe(GEREKCE)
    expect(onayDugmesi().disabled).toBe(false)
  })

  it('bilinmeyen kod genel mesaja düşer, bilinen kod kendi anahtarına', () => {
    expect(
      revertFailure(new ApiError('listing.sale_reversal.reason_too_short', null, 422), 'tr'),
    ).toEqual({
      kind: 'failed',
      messageKey: 'vehicle.saleRevert.error.reason_too_short',
    })
    expect(revertFailure(new ApiError('something.unknown', null, 422), 'tr')).toEqual({
      kind: 'failed',
      messageKey: 'vehicle.saleRevert.error.generic',
    })
    expect(revertFailure(new TypeError('Failed to fetch'), 'tr')).toEqual({
      kind: 'failed',
      messageKey: 'vehicle.saleRevert.error.generic',
    })
  })
})

describe('0023/AC-32: Yeni metinler TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil', () => {
  /**
   * `check:locales` katalogları birbirine karşı ölçüyor ve ÜÇÜNDE de eksik bir anahtarı
   * görmez (`docs/testing.md` § 3). Liste kaynak kodun kullandığı anahtarlardan okunuyor.
   */
  it('kaynakta kullanılan her saleRevert anahtarı üç katalogda da var', () => {
    const kaynak = [
      'SaleRevertDialog.tsx',
      'SaleReversalHistory.tsx',
      'ListingActionBar.tsx',
      'saleReversalState.ts',
    ]
      .map((dosya) =>
        readFileSync(join(process.cwd(), 'src/features/catalog/admin', dosya), 'utf8'),
      )
      .join('\n')
    const kullanilan = new Set(
      [...kaynak.matchAll(/'(vehicle\.saleRevert\.[A-Za-z.]+)'/gu)].map((m) => m[1]!),
    )
    const hatalar = [
      'reason_required',
      'reason_too_short',
      'reason_too_long',
      'expected_status_invalid',
      'invalid_status_transition',
      'not_found',
      'lost',
    ].map((kod) => `vehicle.saleRevert.error.${kod}`)

    expect(kullanilan.size).toBeGreaterThan(10)

    for (const locale of SUPPORTED_LOCALES) {
      const katalog = catalogKeys(locale)

      for (const anahtar of [...kullanilan, ...hatalar, 'vehicle.action.terminal']) {
        expect(katalog, `${locale}: ${anahtar}`).toContain(anahtar)
      }
    }
  })
})

describe('0023/AC-33: Arapça panelde onay penceresi ve kayıt listesi sağdan sola çalışır', () => {
  it('pencere ve liste Arapça basılır; hiçbir öğe yönü LTR\'ye zorlamaz, serbest metin dir="auto" taşır', async () => {
    vi.mocked(fetchSaleReversals).mockResolvedValue(
      onizleme({
        records: [
          {
            id: 'k1',
            staffDisplayName: 'Ayşe Yönetici',
            revertedAt: '2026-11-18T14:00:00Z',
            reason: 'تم النقر على السيارة الخطأ.',
            restoredStatus: 'Published',
          },
        ],
      }),
    )
    cubuk('manager', { locale: 'ar' })

    await pencereyiAc()
    const pencere = screen.getByTestId('sale-revert-form')

    expect(pencere.textContent).toContain('التراجع عن البيع')
    expect(pencere.textContent).toContain('محجوزة')
    expect(pencere.querySelector('[dir="ltr"]')).toBeNull()
    expect(screen.getByTestId('sale-revert-reason').getAttribute('dir')).toBe('auto')
    cleanup()

    render(
      <LocaleProvider locale="ar">
        <SaleReversalHistory vehicleId={ARAC} refreshToken={0} />
      </LocaleProvider>,
    )

    const kayit = await screen.findByTestId('sale-reversal-record')
    const liste = screen.getByTestId('sale-reversals')

    expect(liste.textContent).toContain('سجلات التراجع عن البيع')
    expect(kayit.textContent).toContain('عاد إلى: منشورة')
    expect(liste.querySelector('[dir="ltr"]')).toBeNull()
    expect(kayit.querySelector('.sale-reversals__reason')!.getAttribute('dir')).toBe('auto')
  })
})
