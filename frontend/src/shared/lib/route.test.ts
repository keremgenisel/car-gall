import { describe, expect, it } from 'vitest'
import { listingPath, parseRoute, vehicleEditPath, vehicleMediaPath } from './route'

describe('parseRoute', () => {
  it('AC-1: /{locale}/cars/{slug} detay rotasıdır', () => {
    expect(parseRoute('/tr/cars/toyota-camry-2019-a1b2c3')).toEqual({
      kind: 'detail',
      slug: 'toyota-camry-2019-a1b2c3',
    })
  })

  it('AC-4: üç locale aynı slug ile aynı ilana gider', () => {
    const slug = 'toyota-camry-2019-a1b2c3'

    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(parseRoute(`/${locale}/cars/${slug}`)).toEqual({ kind: 'detail', slug })
      expect(listingPath(locale, slug)).toBe(`/${locale}/cars/${slug}`)
    }
  })

  it('locale kökü liste rotasıdır', () => {
    expect(parseRoute('/tr/')).toEqual({ kind: 'list' })
    expect(parseRoute('/ar')).toEqual({ kind: 'list' })
  })

  it('slug eksikse liste rotasıdır', () => {
    expect(parseRoute('/tr/cars')).toEqual({ kind: 'list' })
    expect(parseRoute('/tr/cars/')).toEqual({ kind: 'list' })
  })

  it('tanınmayan bölüm liste rotasıdır — 0001 davranışı korunur', () => {
    expect(parseRoute('/tr/baska-bir-sey/x')).toEqual({ kind: 'list' })
  })

  it('etkin olmayan locale öneki uçta rotaya dönüşmez — sunucu zaten 404 verir', () => {
    expect(parseRoute('/de/cars/x')).toEqual({ kind: 'list' })
  })
})

/**
 * Panelin rota dalı. **D17'nin bedeli burada ödeniyor:** karar *"`0010b`'nin rota
 * testleri gözden geçirilir"* diyordu; gözden geçirince `0010b`'nin üçüncü parçasının
 * (`/admin/vehicles/{id}`) hiç rota testi OLMADIĞI görüldü. Dördüncü parçayı test
 * etmek için üçüncüsünü de yazmak gerekti — eksikliği kapatmak, onu bir dilim daha
 * taşımaktan ucuz.
 */
describe('parseRoute — panel (0010b D29, 0012a D17)', () => {
  it('panel kökü locale öneki TAŞIMAZ', () => {
    expect(parseRoute('/admin')).toEqual({ kind: 'admin', view: 'staff' })
  })

  it('/admin/vehicles liste, /admin/vehicles/{id} düzenleme rotasıdır', () => {
    expect(parseRoute('/admin/vehicles')).toEqual({ kind: 'admin', view: 'vehicles' })
    expect(parseRoute('/admin/vehicles/abc')).toEqual({
      kind: 'admin',
      view: 'vehicle-edit',
      vehicleId: 'abc',
    })
  })

  it('0012a D17: dördüncü parça `media` medya yüzeyidir', () => {
    expect(parseRoute('/admin/vehicles/abc/media')).toEqual({
      kind: 'admin',
      view: 'vehicle-media',
      vehicleId: 'abc',
    })
  })

  it('tanınmayan dördüncü parça DÜZENLEMEye düşer — yeni bir "bulunamadı" tanımlanmıyor', () => {
    expect(parseRoute('/admin/vehicles/abc/baska')).toEqual({
      kind: 'admin',
      view: 'vehicle-edit',
      vehicleId: 'abc',
    })
  })

  it('kimlik yolda kodlanır ve okunurken çözülür', () => {
    expect(vehicleMediaPath('a b')).toBe('/admin/vehicles/a%20b/media')
    expect(parseRoute(vehicleMediaPath('a b'))).toEqual({
      kind: 'admin',
      view: 'vehicle-media',
      vehicleId: 'a b',
    })
    expect(vehicleMediaPath('abc')).toBe(`${vehicleEditPath('abc')}/media`)
  })
})
