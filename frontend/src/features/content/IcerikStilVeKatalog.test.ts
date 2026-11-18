/// <reference types="node" />
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'

/** `0017` C8, R25 — eklenen stiller ve arayüz metinlerinin anahtarları. */
describe('0017 — stil ve katalog', () => {
  it("0017/AC-124: bu spec'in eklediği stillerde fiziksel yön özelliği ve gömülü renk yoktur", () => {
    const stiller = readFileSync(join(process.cwd(), 'src/app/styles.css'), 'utf8')
    const eklenen = stiller.slice(stiller.indexOf('/* --- İçerik sayfaları (0017'))

    expect(eklenen.length).toBeGreaterThan(0)
    expect(eklenen).not.toMatch(
      /(^|[\s;{])(margin|padding|border)-(left|right)\s*:|(^|[\s;{])(left|right)\s*:|text-align:\s*(left|right)|float:\s*(left|right)/m,
    )
    expect(eklenen).not.toMatch(/#[0-9a-fA-F]{3,8}\b|rgba?\(|hsla?\(/)
    expect(eklenen).toMatch(/var\(--color-/)
  })

  it('0017/AC-125: Kurumsal başlığı, İçerik bölümü, hata kodları, çakışma, onay penceresi, boş durumlar, eksik dil ve izin bağlantısı üç katalogda doludur', () => {
    const anahtarlar = [
      'home.footer.corporate',
      'admin.nav.content',
      'content.admin.title',
      'content.edit.conflict',
      'content.delete.title',
      'content.delete.body',
      'content.delete.confirm',
      'content.delete.cancel',
      'content.admin.freeEmpty',
      'content.faq.empty',
      'content.admin.missing',
      'lead.consent.privacyLink',
      'content.notFound.title',
      'content.error.title',
      ...[
        'field_required',
        'title_too_long',
        'body_too_long',
        'question_too_long',
        'answer_too_long',
        'slug_invalid',
        'slug_reserved',
        'slug_taken',
        'link_scheme_not_allowed',
        'locale_not_enabled',
      ].map((kod) => `content.fieldError.${kod}`),
    ]

    for (const locale of ['tr', 'ar', 'en'] as const) {
      expect(catalogKeys(locale)).toEqual(expect.arrayContaining(anahtarlar))
      const t = createTranslator(locale)
      for (const anahtar of anahtarlar) {
        expect(
          t(anahtar, { locales: 'x', question: 'q' }).trim(),
          `${locale}: ${anahtar}`,
        ).not.toBe('')
      }
    }
  })
})
