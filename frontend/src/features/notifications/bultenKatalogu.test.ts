import { describe, expect, it } from 'vitest'
import ar from '../../shared/i18n/locales/ar.json'
import en from '../../shared/i18n/locales/en.json'
import panelAr from '../../shared/i18n/locales/panel/ar.json'
import panelEn from '../../shared/i18n/locales/panel/en.json'
import panelTr from '../../shared/i18n/locales/panel/tr.json'
import tr from '../../shared/i18n/locales/tr.json'

/**
 * `0018` — bülten metinleri VİTRİN kümesindedir (ADR `0013`): ziyaretçi onları ilk pakette
 * görür ve panel kümesinde değillerdir. `check:locales` kümeleri birbirine karşı ölçer;
 * üçünde de eksik olan bir anahtarı görmez (docs/testing.md § 3) — bu test bileşenlerin
 * kullandığı anahtar listesini ölçer.
 */
const ANAHTARLAR = [
  'newsletter.title',
  'newsletter.body',
  'newsletter.email',
  'newsletter.emailInvalid',
  'newsletter.submit',
  'newsletter.submitting',
  'newsletter.success',
  'newsletter.rateLimited',
  'newsletter.failed',
  'newsletter.note',
  'newsletter.privacy',
  'newsletter.confirm.title',
  'newsletter.confirm.body',
  'newsletter.confirm.action',
  'newsletter.confirmed.title',
  'newsletter.confirmed.body',
  'newsletter.invalid.title',
  'newsletter.invalid.confirmBody',
  'newsletter.invalid.unsubscribeBody',
  'newsletter.resubscribe',
  'newsletter.error.title',
  'newsletter.error.body',
  'newsletter.retry',
  'newsletter.unsubscribed.title',
  'newsletter.unsubscribed.body',
] as const

describe('0018/AC-94: Bülten bölümü, teyit sayfası ve çıkış sayfasının metinleri TR/AR/EN vitrin kataloglarında vardır; eksik anahtar kontrolü (`check:locales`) yeşildir', () => {
  it.each([
    ['tr', tr as Record<string, string>],
    ['ar', ar as Record<string, string>],
    ['en', en as Record<string, string>],
  ])('%s vitrin kataloğu bütün anahtarları dolu taşır', (_dil, katalog) => {
    for (const anahtar of ANAHTARLAR) {
      expect(katalog[anahtar]?.trim(), anahtar).toBeTruthy()
    }
  })

  it('anahtarlar panel kümesinde değildir', () => {
    for (const panel of [panelTr, panelAr, panelEn] as Record<string, string>[]) {
      for (const anahtar of ANAHTARLAR) {
        expect(panel[anahtar]).toBeUndefined()
      }
    }
  })
})
