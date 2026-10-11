import { describe, expect, it } from 'vitest'
import {
  availableActions,
  canAct,
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
