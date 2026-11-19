import { describe, expect, it } from 'vitest'
import { ApiError } from '../../../shared/api/client'
import { catalogKeys } from '../../../shared/i18n/i18n'
import panelAr from '../../../shared/i18n/locales/panel/ar.json'
import panelEn from '../../../shared/i18n/locales/panel/en.json'
import panelTr from '../../../shared/i18n/locales/panel/tr.json'
import vitrinAr from '../../../shared/i18n/locales/ar.json'
import vitrinEn from '../../../shared/i18n/locales/en.json'
import vitrinTr from '../../../shared/i18n/locales/tr.json'
import { displayNamesOf, displayNamesPayload, fieldErrorKey, rejectionOf } from './referenceForm'

/** docs/frontend.md § 8.1 — saf mantığın kendi birim testi vardır. */

describe('rejectionOf', () => {
  it('alan kodunu alana bağlar ve katalog anahtarına çevirir', () => {
    expect(rejectionOf(new ApiError('brand.slug_taken', null, 422, 'canonicalName'), 'tr')).toEqual(
      {
        kind: 'field',
        field: 'canonicalName',
        messageKey: 'reference.fieldError.brand_slug_taken',
      },
    )
  })

  it('eski sürüm çakışma şerididir', () => {
    expect(rejectionOf(new ApiError('concurrency.lost', null, 409), 'tr')).toEqual({
      kind: 'conflict',
    })
  })

  it('bilinmeyen kod ve ağ hatası genel mesaja düşer; kod gösterilmez', () => {
    expect(fieldErrorKey('brand.something_new', 'tr')).toBe('reference.fieldError.generic')
    expect(rejectionOf(new Error('boom'), 'tr')).toEqual({
      kind: 'failed',
      messageKey: 'reference.error.generic',
    })
  })
})

describe('displayNamesPayload', () => {
  it('etkin her dili gönderir; boş değer boş metin olarak gider ve sunucu o dili siler', () => {
    expect(displayNamesPayload({ tr: '  ', ar: ' فيات ' }, ['tr', 'ar', 'en'])).toEqual({
      tr: '',
      ar: 'فيات',
      en: '',
    })
    expect(displayNamesOf({ ar: 'فيات', de: 'X' }, ['tr', 'ar'])).toEqual({ tr: '', ar: 'فيات' })
  })
})

/**
 * Yeni anahtarların listesi: vitrin kümesi ziyaretçinin gördüğü iki eksen adı, kalanı panel
 * kümesinde (ADR 0013). Eşitliği `check:locales` ölçer; bu test anahtarların ÜÇÜNDE DE var
 * olduğunu ölçer — kontrolün göremediği "üçünde de eksik" hâli (docs/testing.md § 3).
 */
const VITRIN_ANAHTARLARI = ['showcase.filter.axis.brands', 'home.form.brand']
const PANEL_ANAHTARLARI = [
  'admin.nav.brands',
  'vehicle.form.field.brand',
  'vehicle.form.model.chooseBrandFirst',
  'vehicle.form.model.noneForBrand',
  'vehicle.form.error.model_inactive',
  'reference.brands.title',
  'reference.brands.modelCount',
  'reference.brand.home',
  'reference.model.brandFixed',
  'reference.fieldError.brand_name_taken',
  'reference.fieldError.brand_slug_taken',
  'reference.fieldError.brand_canonical_name_invalid',
  'reference.fieldError.vehicle_model_name_taken',
  'reference.conflict.body',
]

describe('0021/AC-57: Yeni metinler TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.', () => {
  it('vitrin anahtarları vitrin kümesinin üç dilinde dolu', () => {
    for (const katalog of [vitrinTr, vitrinAr, vitrinEn] as Record<string, string>[]) {
      for (const anahtar of VITRIN_ANAHTARLARI) {
        expect(katalog[anahtar]?.trim(), anahtar).toBeTruthy()
      }
    }
  })

  it('panel anahtarları panel kümesinin üç dilinde dolu ve vitrin kümesinde yok', () => {
    for (const katalog of [panelTr, panelAr, panelEn] as Record<string, string>[]) {
      for (const anahtar of PANEL_ANAHTARLARI) {
        expect(katalog[anahtar]?.trim(), anahtar).toBeTruthy()
        expect((vitrinTr as Record<string, string>)[anahtar], anahtar).toBeUndefined()
      }
    }

    expect(catalogKeys('ar')).toEqual(
      expect.arrayContaining([...VITRIN_ANAHTARLARI, ...PANEL_ANAHTARLARI]),
    )
  })
})
