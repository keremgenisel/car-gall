import { describe, expect, it } from 'vitest'
import {
  codeKey,
  EMPTY_VEHICLE_FORM,
  firstMissingCoreField,
  vehicleMessageKey,
  type VehicleFormValues,
} from './vehicleFormState'

/** docs/frontend.md § 8.1 — saf mantığın kendi birim testi vardır. */

const DOLU: VehicleFormValues = {
  branchId: '0192c000-0000-7000-8000-00000000b001',
  vehicleModelId: '0192c000-0000-7000-8000-00000000000a',
  bodyType: 'suv',
  fuelType: 'petrol',
  transmissionType: 'automatic',
  drivetrainType: 'fwd',
  trimLevel: 'base',
  color: 'Beyaz',
  modelYear: '2019',
  mileageKilometers: '125000',
  priceAmount: '750000',
  priceOnRequest: false,
  vin: 'VIN0000000000001',
}

describe('vehicleMessageKey', () => {
  it('bilinen kodu kendi anahtarına çevirir', () => {
    expect(vehicleMessageKey('vehicle.duplicate_vin', 'tr')).toBe(
      'vehicle.form.error.duplicate_vin',
    )
  })

  it('modül öneki olmayan kodun noktaları alt çizgiye döner', () => {
    expect(vehicleMessageKey('concurrency.lost', 'tr')).toBe('vehicle.form.error.concurrency_lost')
  })

  it('bilinmeyen kod GENEL mesaja düşer; kodun kendisi anahtara girmez (AC-47)', () => {
    expect(vehicleMessageKey('vehicle.brand_new_failure', 'tr')).toBe('vehicle.form.error.generic')
  })

  it('üç locale de aynı anahtarı çözer', () => {
    for (const locale of ['tr', 'en', 'ar'] as const) {
      expect(vehicleMessageKey('vehicle.branch_not_found', locale)).toBe(
        'vehicle.form.error.branch_not_found',
      )
    }
  })
})

describe('codeKey', () => {
  it('tek kelimeli kodu küçültür', () => {
    expect(codeKey('vehicle.status', 'Draft')).toBe('vehicle.status.draft')
  })

  it('PascalCase kodu snake_case yapar', () => {
    expect(codeKey('branch.status', 'TemporarilyClosed')).toBe('branch.status.temporarily_closed')
  })
})

describe('firstMissingCoreField', () => {
  it('eksiksiz formda eksik alan yok', () => {
    expect(firstMissingCoreField(DOLU)).toBeNull()
  })

  it('boş formda İLK çekirdek alanı bildirir', () => {
    expect(firstMissingCoreField(EMPTY_VEHICLE_FORM)).toBe('branchId')
  })

  it('yalnız boşluk içeren alan dolu sayılmaz', () => {
    expect(firstMissingCoreField({ ...DOLU, vin: '   ' })).toBe('vin')
  })

  it('tutar yoksa ve "fiyat sorunuz" işaretli değilse fiyat eksiktir (P4)', () => {
    expect(firstMissingCoreField({ ...DOLU, priceAmount: '' })).toBe('price')
  })

  it('"fiyat sorunuz" işaretliyken tutar GEREKMEZ', () => {
    expect(firstMissingCoreField({ ...DOLU, priceAmount: '', priceOnRequest: true })).toBeNull()
  })

  /**
   * Renk R2'nin çekirdek listesinde ve AC-6 eksik çekirdek alanı reddediyor. R10'un çok
   * dilli metin muafiyeti KALAN dilleri kapsıyor: personelin kendi dilinde dolu olması
   * yetiyor, diğer ikisi `0010b`'de doldurulabilir.
   */
  it('renk boş bırakılamaz (R2, AC-6)', () => {
    expect(firstMissingCoreField({ ...DOLU, color: '' })).toBe('color')
  })

  it('yalnız boşluk içeren renk dolu sayılmaz', () => {
    expect(firstMissingCoreField({ ...DOLU, color: '   ' })).toBe('color')
  })
})
