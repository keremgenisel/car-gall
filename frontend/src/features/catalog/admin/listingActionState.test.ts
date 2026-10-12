import { describe, expect, it } from 'vitest'
import {
  availableActions,
  canAct,
  canFeature,
  messageKey,
  missingFieldKeys,
  publishRejectionKeys,
} from './listingActionState'

describe('listingActionState', () => {
  it('0012b/AC-19: A3'.concat(' dışındaki eylem ekranda hiç gösterilmiyor'), () => {
    // Taslakta "rezerve et" YOK: sunucunun `409`'una yürüyen bir davet olurdu.
    expect(availableActions('Draft')).toEqual(['publish', 'archive'])
    expect(availableActions('Published')).toEqual(['reserve', 'sold', 'unpublish', 'archive'])
    expect(availableActions('Reserved')).toEqual(['unreserve', 'sold', 'unpublish'])
  })

  it('0012b/AC-14: `Satıldı` terminaldir — yalnız arşivlenebilir', () => {
    expect(availableActions('Sold')).toEqual(['archive'])
  })

  it('`Arşiv` hiçbir eylem sunmaz; tanınmayan durum da', () => {
    expect(availableActions('Archived')).toEqual([])
    expect(availableActions('Bilinmeyen')).toEqual([])
  })

  it('0012c/AC-43: arşivlenmiş satırda öne çıkarma düğmesi YOKTUR', () => {
    // Tek ön koşul arşiv (**D36**) ve iki yön de reddediliyor: arşivlenmiş bir ilanda
    // "öne çıkarmayı kaldır" da `listing.cannot_feature.archived` alır.
    expect(canFeature('Archived')).toBe(false)
  })

  it('0012c/AC-29: öne çıkarma arşiv dışındaki her durumda çağrılabilir', () => {
    for (const status of ['Draft', 'Published', 'Reserved', 'Sold']) {
      expect(canFeature(status)).toBe(true)
    }
  })

  it('0012b/AC-58: rol ayrımı yok — oturum açmış her personel her eylemi çağırabilir', () => {
    expect(canAct()).toBe(true)
  })

  it('0012b/AC-9: red kod LİSTESİ çeviri anahtarlarına dönüşüyor', () => {
    expect(
      publishRejectionKeys(
        'vehicle.cannot_publish.no_media,vehicle.cannot_publish.required_field',
        'tr',
      ),
    ).toEqual([
      'vehicle.action.error.cannot_publish_no_media',
      'vehicle.action.error.cannot_publish_required_field',
    ])
  })

  it('bilinmeyen kod GENEL mesaja düşer; kodun kendisi görünmez', () => {
    expect(messageKey('vehicle.bilinmeyen_kod', 'tr')).toBe('vehicle.action.error.generic')
    expect(publishRejectionKeys(undefined, 'tr')).toEqual(['vehicle.action.error.generic'])
  })

  it('0012b/AC-4: eksik alan anahtarları kendi çeviri anahtarlarına dönüşüyor', () => {
    expect(missingFieldKeys('damage_record_amount,warranty_status')).toEqual([
      'vehicle.field.damage_record_amount',
      'vehicle.field.warranty_status',
    ])
    expect(missingFieldKeys(undefined)).toEqual([])
  })
})
