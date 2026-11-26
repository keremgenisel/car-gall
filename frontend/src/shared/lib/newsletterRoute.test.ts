import { describe, expect, it } from 'vitest'
import { newsletterSectionPath, newsletterTokenOf, parseRoute } from './route'

describe('0018/AC-51: Teyit bağlantısı `{PublicBaseUrl}/{locale}/newsletter/confirm?token=…`, çıkış bağlantısı `{PublicBaseUrl}/{locale}/newsletter/unsubscribe?token=…` kalıbındadır ve ön yüz iki yolu da ilgili sayfayla açar', () => {
  it.each(['tr', 'ar', 'en'])('%s: iki yol iki sayfa', (locale) => {
    expect(parseRoute(`/${locale}/newsletter/confirm`)).toEqual({ kind: 'newsletter-confirm' })
    expect(parseRoute(`/${locale}/newsletter/unsubscribe`)).toEqual({
      kind: 'newsletter-unsubscribe',
    })
  })

  it('anahtar sorgu parametresinden; Base64Url karakterleri bozulmadan', () => {
    expect(newsletterTokenOf('?token=AbC-12_xyz')).toBe('AbC-12_xyz')
    expect(newsletterTokenOf('?token=a%2Bb')).toBe('a+b')
    expect(newsletterTokenOf('')).toBe('')
  })

  // `0004` D8 ile bilerek değişti: tanınmayan alt yol listeye değil "bulunamadı"ya düşer (AC-76).
  it('newsletter tek parçalı yolu içerik sayfası değildir; tanınmayan alt yolu "bulunamadı"dır', () => {
    expect(parseRoute('/tr/newsletter/baska')).toEqual({ kind: 'not-found' })
  })

  it('yeniden abone olma bağlantısı ana sayfanın bülten bölümüne', () => {
    expect(newsletterSectionPath('ar')).toBe('/ar/#newsletter')
  })
})
