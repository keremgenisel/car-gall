import { describe, expect, it } from 'vitest'
import type {
  VehicleMedia,
  VehicleMediaItem,
  MediaProcessingStatus,
} from '../../../shared/api/types'
import {
  altTextChanged,
  altTextRequest,
  attach,
  coverOf,
  detach,
  isProcessing,
  mediaConditionMet,
  move,
  orderChanged,
  toDraft,
  withAltText,
} from './vehicleMediaState'

function oge(
  id: string,
  status: MediaProcessingStatus = 'ready',
  altText: Record<string, string> = {},
): VehicleMediaItem {
  return {
    mediaAssetId: id,
    status,
    failureReason: status === 'failed' ? 'watermarkunavailable' : null,
    altText,
    derivatives:
      status === 'ready'
        ? [{ variant: 'display', width: 800, height: 533, url: `https://cdn.test/${id}.webp` }]
        : [],
  }
}

function medya(items: VehicleMediaItem[]): VehicleMedia {
  return {
    vehicleId: 'arac-1',
    coverMediaAssetId: items.length > 0 ? items[0].mediaAssetId : null,
    items,
  }
}

describe('vehicleMediaState', () => {
  it('0012a/AC-4: kapak sıralı listenin ilk elemanı', () => {
    const draft = toDraft(medya([oge('a'), oge('b'), oge('c')]))

    expect(coverOf(draft)).toBe('a')
  })

  it('0012a/AC-5: sıra değişince kapak da değişir', () => {
    let draft = toDraft(medya([oge('a'), oge('b'), oge('c')]))

    draft = move(draft, 'c', -1)
    draft = move(draft, 'c', -1)

    expect(draft.order).toEqual(['c', 'a', 'b'])
    expect(coverOf(draft)).toBe('c')
  })

  it('sınır dışına taşıma listeyi bozmaz', () => {
    const draft = toDraft(medya([oge('a'), oge('b')]))

    expect(move(draft, 'a', -1).order).toEqual(['a', 'b'])
    expect(move(draft, 'b', 1).order).toEqual(['a', 'b'])
    expect(move(draft, 'yok', 1).order).toEqual(['a', 'b'])
  })

  it('0012a/AC-6: çıkarmak listeden düşürür', () => {
    const draft = detach(toDraft(medya([oge('a'), oge('b'), oge('c')])), 'b')

    expect(draft.order).toEqual(['a', 'c'])
  })

  it('kapak çıkarılınca sıradaki kapak olur', () => {
    const draft = detach(toDraft(medya([oge('a'), oge('b')])), 'a')

    expect(coverOf(draft)).toBe('b')
  })

  it('yeni yüklenen fotoğraf SONA ekleniyor; kapağı değiştirmiyor', () => {
    const draft = attach(toDraft(medya([oge('a')])), 'b')

    expect(draft.order).toEqual(['a', 'b'])
    expect(coverOf(draft)).toBe('a')
  })

  it('aynı varlık iki kez eklenmiyor', () => {
    const draft = attach(toDraft(medya([oge('a')])), 'a')

    expect(draft.order).toEqual(['a'])
  })

  it('0012a/AC-12: boş `alt` metni sunucuya gönderilmiyor', () => {
    let draft = toDraft(medya([oge('a')]))

    draft = withAltText(draft, 'a', 'tr', 'beyaz BMW, önden')
    draft = withAltText(draft, 'a', 'en', '   ')

    expect(altTextRequest(draft, 'a')).toEqual({ tr: 'beyaz BMW, önden' })
  })

  it('0012a/AC-11: üç dil birlikte gönderiliyor', () => {
    let draft = toDraft(medya([oge('a')]))

    draft = withAltText(draft, 'a', 'tr', 'önden')
    draft = withAltText(draft, 'a', 'ar', 'من الأمام')
    draft = withAltText(draft, 'a', 'en', 'front view')

    expect(altTextRequest(draft, 'a')).toEqual({
      tr: 'önden',
      ar: 'من الأمام',
      en: 'front view',
    })
  })

  it('kirlilik: sıra ve `alt` metni ayrı ayrı ölçülüyor', () => {
    const sunucu = medya([oge('a', 'ready', { tr: 'önden' }), oge('b')])
    const temiz = toDraft(sunucu)

    expect(orderChanged(temiz, sunucu)).toBe(false)
    expect(altTextChanged(temiz, sunucu, 'a')).toBe(false)

    expect(orderChanged(move(temiz, 'b', -1), sunucu)).toBe(true)
    expect(altTextChanged(withAltText(temiz, 'a', 'tr', 'arkadan'), sunucu, 'a')).toBe(true)

    // Yalnız boşluk eklemek değişiklik DEĞİL: kırpılmış hâlleri eşit.
    expect(altTextChanged(withAltText(temiz, 'a', 'tr', ' önden '), sunucu, 'a')).toBe(false)
  })

  it('0012a/AC-19: hiç bağlı varlığı olmayan araç medya koşulunu karşılamaz', () => {
    expect(mediaConditionMet(medya([]))).toBe(false)
  })

  it('0012a/AC-20: hepsi işleniyorsa karşılamaz', () => {
    expect(mediaConditionMet(medya([oge('a', 'processing'), oge('b', 'uploaded')]))).toBe(false)
  })

  it('0012a/AC-21: biri hazırsa KARŞILAR', () => {
    expect(
      mediaConditionMet(medya([oge('a', 'processing'), oge('b', 'ready'), oge('c', 'processing')])),
    ).toBe(true)
  })

  it('0012a/AC-22: hepsi başarısızsa karşılamaz', () => {
    expect(mediaConditionMet(medya([oge('a', 'failed'), oge('b', 'failed')]))).toBe(false)
  })

  it('0012a/AC-28: "işleniyor" varlığın durumu — yüklendi ve işleniyor ikisi de', () => {
    expect(isProcessing(oge('a', 'uploaded'))).toBe(true)
    expect(isProcessing(oge('a', 'processing'))).toBe(true)
    expect(isProcessing(oge('a', 'ready'))).toBe(false)
    expect(isProcessing(oge('a', 'failed'))).toBe(false)
  })
})
