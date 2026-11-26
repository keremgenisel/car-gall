import { describe, expect, it } from 'vitest'
import {
  homePath,
  listPath,
  listingPath,
  parseRoute,
  vehicleEditPath,
  vehicleMediaPath,
} from './route'

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

  // 0014 D9 — locale kökü artık ANA SAYFADIR; liste `/{locale}/cars`'ta. Bu test
  // önceden "locale kökü liste rotasıdır" diyordu ve D9 o davranışı bilerek değiştirdi.
  it('0014/AC-1: locale kökü ana sayfa rotasıdır', () => {
    expect(parseRoute('/tr/')).toEqual({ kind: 'home' })
    expect(parseRoute('/ar')).toEqual({ kind: 'home' })
    expect(parseRoute('/en/')).toEqual({ kind: 'home' })
  })

  it('0014/AC-5: /{locale}/cars araç listesidir (slug eksikse liste rotası)', () => {
    expect(parseRoute('/tr/cars')).toEqual({ kind: 'list' })
    expect(parseRoute('/tr/cars/')).toEqual({ kind: 'list' })
  })

  // `0004` D8 ile bilerek değişti: 0014 D28'in "tanınmayan bölüm listeye düşer" davranışı kalktı;
  // sunucu bu yolu 404 ile verir ve uç "bulunamadı" basar (AC-76).
  it('tanınmayan bölüm "bulunamadı" rotasıdır — 0004 D8', () => {
    expect(parseRoute('/tr/baska-bir-sey/x')).toEqual({ kind: 'not-found' })
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

describe('0014 — liste ve ana sayfa adresleri', () => {
  it('listPath /{locale}/cars, homePath /{locale}/ üretir', () => {
    expect(listPath('ar')).toBe('/ar/cars')
    expect(homePath('en')).toBe('/en/')
  })
})

describe('0004 — "bulunamadı" rotası (D8)', () => {
  it('0004/AC-76 (rota yarısı): /tr/foo/bar, /tr/cars/{slug}/x, /tr/admin, /tr/newsletter ve /tr/newsletter/foo "bulunamadı" rotasıdır', () => {
    for (const adres of [
      '/tr/foo/bar',
      '/tr/cars/toyota-corolla-2019-abc123/x',
      '/tr/admin',
      '/tr/newsletter',
      '/tr/newsletter/foo',
      '/ar/foo/bar',
    ]) {
      expect(parseRoute(adres), adres).toEqual({ kind: 'not-found' })
    }
  })

  it('QA 2 BULGU-2: bölüm adı çözülmüş hâliyle karşılaştırılır — /tr/c%61rs liste, /tr/c%61rs/{slug} detay (sunucuyla aynı)', () => {
    expect(parseRoute('/tr/c%61rs')).toEqual({ kind: 'list' })
    expect(parseRoute('/tr/c%61rs/toyota-corolla-2019-abc123')).toEqual({
      kind: 'detail',
      slug: 'toyota-corolla-2019-abc123',
    })
    expect(parseRoute('/tr/newsletter/c%6Fnfirm')).toEqual({ kind: 'newsletter-confirm' })
    expect(parseRoute('/tr/%61dmin')).toEqual({ kind: 'not-found' })
  })

  it('görünür rotalar değişmez: ana sayfa, liste, detay, içerik, bülten teyit/çıkış', () => {
    expect(parseRoute('/tr/')).toEqual({ kind: 'home' })
    expect(parseRoute('/tr/cars')).toEqual({ kind: 'list' })
    expect(parseRoute('/tr/cars/toyota-corolla-2019-abc123')).toEqual({
      kind: 'detail',
      slug: 'toyota-corolla-2019-abc123',
    })
    expect(parseRoute('/tr/kvkk')).toEqual({ kind: 'content', slug: 'kvkk' })
    expect(parseRoute('/tr/newsletter/confirm')).toEqual({ kind: 'newsletter-confirm' })
  })
})
