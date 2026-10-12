import { describe, expect, it } from 'vitest'
import type { BulkListingRow } from '../../../shared/api/types'
import { retainVisible, selectionOf, summarizeBulkResult } from './bulkActionState'

function row(overrides: Partial<BulkListingRow> & { readonly vehicleId: string }): BulkListingRow {
  return {
    applied: true,
    errorCode: null,
    parameters: null,
    listingStatus: 'Draft',
    ...overrides,
  }
}

describe('toplu eylem sonucu', () => {
  it('0012c/AC-37: hepsi uygulandığında sonuç başarılıdır ve uygulanan sayısı sayılır', () => {
    const özet = summarizeBulkResult([row({ vehicleId: 'a' }), row({ vehicleId: 'b' })], 'tr')

    expect(özet.outcome).toBe('success')
    expect(özet.appliedCount).toBe(2)
    expect(özet.failures).toHaveLength(0)
  })

  it('0012c/AC-38: bir satır bile uygulanmadıysa sonuç KISMİ BAŞARIDIR; başarılıya yuvarlanmaz', () => {
    const özet = summarizeBulkResult(
      [
        row({ vehicleId: 'a' }),
        row({
          vehicleId: 'b',
          applied: false,
          errorCode: 'listing.invalid_status_transition',
          listingStatus: 'Sold',
        }),
      ],
      'tr',
    )

    expect(özet.outcome).toBe('partial')
    expect(özet.appliedCount).toBe(1)
    expect(özet.failures).toHaveLength(1)

    // Güncel durum satırda duruyor: "zaten o durumda" ile "koşulu sağlamıyor" ancak
    // onunla ayrılır (**D45**).
    expect(özet.failures[0]?.listingStatus).toBe('Sold')
  })

  it('0012c/AC-13: hepsi başarısız olsa da sonuç okunur ve kısmi başarı olarak basılır', () => {
    const özet = summarizeBulkResult(
      [
        row({
          vehicleId: 'a',
          applied: false,
          errorCode: 'vehicle.not_found',
          listingStatus: null,
        }),
        row({
          vehicleId: 'b',
          applied: false,
          errorCode: 'vehicle.not_found',
          listingStatus: null,
        }),
      ],
      'tr',
    )

    expect(özet.outcome).toBe('partial')
    expect(özet.appliedCount).toBe(0)
    expect(özet.failures).toHaveLength(2)
  })

  it('0012c/AC-12: yayın reddinin gerekçeleri tekil uçtakiyle aynı anahtarlara düşer', () => {
    const özet = summarizeBulkResult(
      [
        row({
          vehicleId: 'a',
          applied: false,
          errorCode: 'vehicle.cannot_publish',
          parameters: {
            reasons: 'vehicle.cannot_publish.no_media,vehicle.cannot_publish.required_field',
            fields: 'damage_record_amount',
          },
        }),
      ],
      'tr',
    )

    expect(özet.failures[0]?.messageKeys).toEqual([
      'vehicle.action.error.cannot_publish_no_media',
      'vehicle.action.error.cannot_publish_required_field',
    ])
    expect(özet.failures[0]?.fieldKeys).toEqual(['vehicle.field.damage_record_amount'])
  })

  it('0012c/AC-39: bilinmeyen kod GENEL mesaja düşer; kodun kendisi anahtar olmaz', () => {
    const özet = summarizeBulkResult(
      [row({ vehicleId: 'a', applied: false, errorCode: 'listing.bilinmeyen_kod' })],
      'tr',
    )

    expect(özet.failures[0]?.messageKeys).toEqual(['vehicle.action.error.generic'])
  })

  it('0012c/AC-18: seçim listesi SAYFA sırasını korur, tıklama sırasını değil', () => {
    const seçim = selectionOf(['a', 'b', 'c', 'd'], new Set(['d', 'b']))

    expect(seçim).toEqual(['b', 'd'])
  })

  it('0012c/AC-36: sayfada olmayan kimlik seçimde kalmaz', () => {
    const kalan = retainVisible(new Set(['a', 'b', 'z']), ['a', 'b', 'c'])

    expect([...kalan].sort()).toEqual(['a', 'b'])
  })
})
