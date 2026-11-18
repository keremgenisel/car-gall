import { describe, expect, it } from 'vitest'
import { ApiError } from '../../../shared/api/client'
import {
  actionErrorKey,
  fieldErrorKey,
  fieldErrorsOf,
  isConflict,
  localizedPayload,
  parseCoordinate,
  statusKey,
  timeZoneOptions,
} from './dealershipForm'

/** `0020` — galeri ve şube formlarının saf mantığı (docs/frontend.md § 8.1). */
describe('dealershipForm', () => {
  it('alan hatalarını `fields` listesinden okur; başka kodda boş döner', () => {
    const hata = new ApiError('dealership.validation_failed', null, 422, null, {
      fields: 'name.tr,slug',
      'name.tr': 'branch.name_too_long',
      slug: 'branch.slug_taken',
    })

    expect(fieldErrorsOf(hata)).toEqual({
      'name.tr': 'branch.name_too_long',
      slug: 'branch.slug_taken',
    })
    expect(fieldErrorsOf(new ApiError('concurrency.lost', null, 409))).toEqual({})
    expect(fieldErrorsOf(new Error('x'))).toEqual({})
  })

  it('çakışmayı yalnız `concurrency.lost` ile tanır', () => {
    expect(isConflict(new ApiError('concurrency.lost', null, 409))).toBe(true)
    expect(isConflict(new ApiError('branch.last_active_branch', null, 409))).toBe(false)
  })

  it('bilinmeyen kod genel anahtara düşer; kodun kendisi anahtar olmaz', () => {
    expect(fieldErrorKey('branch.slug_taken', 'tr')).toBe('dealership.fieldError.branch_slug_taken')
    expect(fieldErrorKey('branch.something_new', 'tr')).toBe('dealership.fieldError.generic')
    expect(actionErrorKey(new ApiError('branch.something_new', null, 409), 'ar')).toBe(
      'dealership.error.generic',
    )
    expect(actionErrorKey(new ApiError('branch.last_active_branch', null, 409), 'ar')).toBe(
      'dealership.error.branch_last_active_branch',
    )
  })

  it('durum kodunu katalog anahtarına çevirir', () => {
    expect(statusKey('TemporarilyClosed')).toBe('branch.status.temporarily_closed')
    expect(statusKey('Active')).toBe('branch.status.active')
  })

  it('saat dilimi listesi bölge kimlikleridir; kayıtlı değer listede yoksa yine sunulur', () => {
    const sahte = {
      supportedValuesOf: () => ['UTC', 'Europe/Istanbul', 'Asia/Kuwait'],
    } as unknown as typeof Intl

    expect(timeZoneOptions('', sahte)).toEqual(['Asia/Kuwait', 'Europe/Istanbul'])
    expect(timeZoneOptions('Asia/Istanbul', sahte)).toEqual([
      'Asia/Istanbul',
      'Asia/Kuwait',
      'Europe/Istanbul',
    ])
  })

  it('boşluktan ibaret dil değerlerini düşürür', () => {
    expect(localizedPayload({ tr: 'Ad', ar: '  ', en: '' })).toEqual({ tr: 'Ad' })
  })

  it('koordinatı ayrıştırır: boş null, sayı sayı, geçersiz undefined', () => {
    expect(parseCoordinate(' ')).toBeNull()
    expect(parseCoordinate('40,98')).toBe(40.98)
    expect(parseCoordinate('-29.06')).toBe(-29.06)
    expect(parseCoordinate('kuzey')).toBeUndefined()
  })
})
