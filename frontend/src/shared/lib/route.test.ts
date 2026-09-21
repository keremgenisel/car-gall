import { describe, expect, it } from 'vitest'
import { listingPath, parseRoute } from './route'

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
