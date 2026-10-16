import { describe, expect, it } from 'vitest'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'
import { STAFF_ROLES } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'
import {
  ACTION_ERROR_CODES,
  ACTION_PREFIX,
  CLIENT_ERROR_CODES,
  type StaffAction,
} from './staffActionState'

const LOCALES: readonly AppLocale[] = ['tr', 'ar', 'en']

/** Giriş ve personel yönetimi ekranlarında ziyaretçiye görünen her metin. */
const GORUNEN_ANAHTARLAR = [
  'admin.title',
  'admin.logout',
  'admin.loading',
  'login.title',
  'login.field.loginId',
  'login.field.password',
  'login.submit',
  'login.submitting',
  'login.error.title',
  'login.error.retry',
  'login.locked.title',
  'login.locked.body',
  'password.title',
  'password.intro',
  'password.field.current',
  'password.field.new',
  'password.submit',
  'password.changed.title',
  'password.changed.body',
  'password.changed.action',
  'password.error.title',
  'staff.title',
  'staff.column.name',
  'staff.column.role',
  'staff.column.status',
  'staff.empty.title',
  'staff.empty.body',
  'staff.error.title',
  'staff.error.body',
  'staff.error.retry',
  'staff.add.title',
  'staff.add.hint',
  'staff.add.submit',
  'staff.status.active',
  'staff.status.deactivated',

  // 0013 — ikinci adımın görünen metinleri. Kapı bunları da geziyor: üç katalogdan
  // BİRDEN silinen bir anahtar `check:locales`i geçer ve ekranda boş metin bırakır
  // (ölçüldü, `staff.password.error.required`).
  'admin.nav.twoFactor',
  'login.twoFactor.title',
  'login.twoFactor.intro',
  'login.twoFactor.field.code',
  'login.twoFactor.field.recovery',
  'login.twoFactor.hint',
  'login.twoFactor.recoveryHint',
  'login.twoFactor.submit',
  'login.twoFactor.submitting',
  'login.twoFactor.useRecovery',
  'login.twoFactor.useCode',
  'login.twoFactor.switchUser',
  'login.twoFactor.error.required',
  'login.twoFactor.error.format',
  'twoFactor.title',
  'twoFactor.setup.intro',
  'twoFactor.setup.start',
  'twoFactor.setup.starting',
  'twoFactor.setup.scan',
  'twoFactor.setup.manual',
  'twoFactor.setup.field.code',
  'twoFactor.setup.hint',
  'twoFactor.setup.confirm',
  'twoFactor.setup.confirming',
  'twoFactor.setup.continue',
  'twoFactor.codes.title',
  'twoFactor.codes.body',
  'twoFactor.codes.regenerate',
  'twoFactor.manage.field.code',
  'twoFactor.manage.hint',
  'twoFactor.disable.title',
  'twoFactor.disable.body',
  'twoFactor.disable.field.password',
  'twoFactor.disable.submit',
  'twoFactor.disable.notAllowed',
  'twoFactor.disable.error.password',
  'twoFactor.disable.error.code',
  'twoFactor.status.enrolled',
  'twoFactor.status.missing',
  'twoFactor.status.optional',
  'twoFactor.error.title',
  'twoFactor.error.body',
  'twoFactor.error.retry',
  'staff.column.twoFactor',
  'staff.action.twoFactorReset',
  'staff.confirm.twoFactorReset.title',
  'staff.confirm.twoFactorReset.submit',
  'staff.confirm.twoFactorReset.warning',
  'staff.confirm.twoFactorReset.self',
  ...STAFF_ROLES.map((role) => `staff.role.${role}`),
]

/** Sunucunun döndürebileceği her `errorCode`'un uçta karşılığı olmalıdır. */
const SUNUCU_HATA_KODLARI: readonly { readonly code: string; readonly prefix: string }[] = [
  { code: 'invalid_credentials', prefix: 'login.error' },
  { code: 'rate_limited', prefix: 'login.error' },
  { code: 'current_password_invalid', prefix: 'password.error' },
  { code: 'password_too_short', prefix: 'password.error' },
  { code: 'login_id_taken', prefix: 'staff.add.error' },
  { code: 'invalid_login_id', prefix: 'staff.add.error' },
  { code: 'branch_not_found', prefix: 'staff.add.error' },
  { code: 'locale_not_enabled', prefix: 'staff.add.error' },
  { code: 'last_active_manager', prefix: 'staff.add.error' },
  { code: 'concurrency_lost', prefix: 'staff.add.error' },

  // 0013 D20 — ikinci adımın iki red kodu AYRI karşılıklar ister: biri "taze kod yaz",
  // öteki "parolanı yeniden gir" diyor (R10).
  { code: 'two_factor_invalid', prefix: 'login.error' },
  { code: 'two_factor_pending_expired', prefix: 'login.error' },
  { code: 'two_factor_invalid', prefix: 'twoFactor.error' },
  { code: 'two_factor_already_enrolled', prefix: 'twoFactor.error' },
  { code: 'two_factor_not_enrolled', prefix: 'twoFactor.error' },
  { code: 'two_factor_disable_not_allowed', prefix: 'twoFactor.error' },
  { code: 'current_password_invalid', prefix: 'twoFactor.error' },
  { code: 'generic', prefix: 'twoFactor.error' },
]

/**
 * 0007 R6 — iki red türü ayrı yazılır. Kural gereği red KALICIDIR: o kullanıcıya tekrar
 * denemesi söylenirse sonsuza kadar denemesi söylenmiş olur. Eşzamanlılık kaybı geçicidir
 * ve tekrar denemek gerçekten işe yarar.
 */
const TEKRAR_DENE: Readonly<Record<AppLocale, RegExp>> = {
  tr: /tekrar dene/i,
  ar: /أعد المحاولة/,
  en: /try again/i,
}

/** Ziyaretçiye sızmaması gereken teknik izler (0007/AC-15). */
const TEKNIK_IZLER = [
  /\b[45]\d{2}\b/,
  /errorCode/i,
  /traceId/i,
  /\/api\//,
  /Exception/i,
  /\bSQL\b/i,
]

describe('AC-61: Giriş ekranı ve personel yönetimi ekranındaki tüm metinler TR, AR ve EN locale lerinde kendi dilinde görünür; hiçbir locale de çeviri anahtarı ham hâliyle görünmez', () => {
  it.each(LOCALES)('%s katalogunda görünen anahtarların hepsi dolu', (locale) => {
    const t = createTranslator(locale)

    for (const key of GORUNEN_ANAHTARLAR) {
      expect(t(key), `${locale}: ${key}`).not.toBe('')
    }
  })

  it('aynı metin üç locale de aynı değildir', () => {
    const cevriler = LOCALES.map((locale) => createTranslator(locale)('login.submit'))

    expect(new Set(cevriler).size).toBe(3)
  })

  it.each(LOCALES)('%s: sunucunun her hata kodunun karşılığı var', (locale) => {
    const keys = catalogKeys(locale)
    const t = createTranslator(locale)

    for (const { code, prefix } of SUNUCU_HATA_KODLARI) {
      const key = `${prefix}.${code}`

      expect(keys, `${locale}: ${key}`).toContain(key)
      expect(t(key)).not.toBe('')
    }
  })

  it.each(LOCALES)(
    '0007/AC-16 — %s: kural gereği red "tekrar deneyin" demez, eşzamanlılık kaybı der',
    (locale) => {
      const t = createTranslator(locale)

      expect(t('staff.add.error.last_active_manager')).not.toMatch(TEKRAR_DENE[locale])
      expect(t('staff.add.error.concurrency_lost')).toMatch(TEKRAR_DENE[locale])
    },
  )

  it.each(LOCALES)('0007/AC-15 — %s: gösterilen metinlerde teknik iz yok', (locale) => {
    const t = createTranslator(locale)

    for (const key of [
      ...GORUNEN_ANAHTARLAR,
      ...SUNUCU_HATA_KODLARI.map((h) => `${h.prefix}.${h.code}`),
    ]) {
      for (const iz of TEKNIK_IZLER) {
        expect(t(key), `${locale}: ${key}`).not.toMatch(iz)
      }
    }
  })

  /**
   * Her hata mesajı eylem önerili yazılır (docs/frontend.md § 3.1): ne olduğunu ve ne
   * yapılabileceğini söyler.
   */
  it.each(LOCALES)('%s: hata ekranlarının eylem metni var', (locale) => {
    const t = createTranslator(locale)

    expect(t('login.error.retry')).not.toBe('')
    expect(t('staff.error.retry')).not.toBe('')
    expect(t('password.changed.action')).not.toBe('')
  })
})

describe('0008/AC-27: Üç eylemin her biri için anahtarlar TR, AR ve EN kataloglarında bulunur; hiçbir locale de çeviri anahtarı ham hâliyle görünmez', () => {
  /** Tablo TEK KAYNAKTAN okunuyor; test sabit bir sayı saymıyor (C5). */
  it.each(LOCALES)('%s: her eylemin her hata anahtarı dolu', (locale) => {
    const keys = catalogKeys(locale)
    const t = createTranslator(locale)

    for (const [action, codes] of Object.entries(ACTION_ERROR_CODES)) {
      for (const code of codes) {
        const key = `${ACTION_PREFIX[action as StaffAction]}.${code}`

        expect(keys, `${locale}: ${key}`).toContain(key)
        expect(t(key, { name: 'Ayşe' }).trim(), `${locale}: ${key}`).not.toBe('')
      }
    }
  })

  /**
   * Uç tarafı doğrulama anahtarları da kapının içinde. Dışarıda bırakıldıklarında
   * `staff.password.error.required` üç katalogdan birden silinse bile hiçbir test
   * görmüyordu — ölçüldü.
   */
  it.each(LOCALES)('%s: uç tarafı doğrulama anahtarları dolu', (locale) => {
    const keys = catalogKeys(locale)
    const t = createTranslator(locale)

    for (const [action, codes] of Object.entries(CLIENT_ERROR_CODES)) {
      for (const code of codes) {
        const key = `${ACTION_PREFIX[action as StaffAction]}.${code}`

        expect(keys, `${locale}: ${key}`).toContain(key)
        expect(t(key).trim(), `${locale}: ${key}`).not.toBe('')
      }
    }
  })

  it.each(LOCALES)('%s: onay kutusunun metinleri dolu', (locale) => {
    const t = createTranslator(locale)

    for (const key of [
      'staff.column.actions',
      'staff.action.deactivate',
      'staff.action.role',
      'staff.action.password',
      'staff.action.self',
      'staff.action.working',
      'staff.confirm.cancel',
      'staff.confirm.deactivate.title',
      'staff.confirm.deactivate.submit',
      'staff.confirm.deactivate.self',
      'staff.confirm.role.title',
      'staff.confirm.role.submit',
      'staff.confirm.role.self',
      'staff.confirm.password.title',
      'staff.confirm.password.field.new',
      'staff.confirm.password.field.confirm',
      'staff.confirm.password.submit',
    ]) {
      expect(t(key, { name: 'Ayşe' }).trim(), `${locale}: ${key}`).not.toBe('')
    }
  })
})

describe('0008/AC-20: AC-18 ve AC-19`un metinleri birbirinden farklıdır ve hiçbiri hata kodunu, durum kodunu, `traceId`yi, endpoint yolunu veya istisna metnini içermez', () => {
  it.each(LOCALES)('%s: iki red metni FARKLIDIR', (locale) => {
    const t = createTranslator(locale)

    for (const prefix of ['staff.deactivate.error', 'staff.role.error']) {
      expect(t(`${prefix}.last_active_manager`)).not.toBe(t(`${prefix}.concurrency_lost`))
    }
  })

  it.each(LOCALES)('%s: 0008`in metinlerinde teknik iz yok', (locale) => {
    const t = createTranslator(locale)

    for (const [action, codes] of Object.entries(ACTION_ERROR_CODES)) {
      for (const code of codes) {
        const metin = t(`${ACTION_PREFIX[action as StaffAction]}.${code}`)

        for (const iz of TEKNIK_IZLER) {
          expect(metin, `${locale}: ${action}.${code}`).not.toMatch(iz)
        }
      }
    }
  })

  /**
   * R5/AC-16'nın metin yarısı: kural gereği red KALICIDIR ve o kullanıcıya tekrar
   * denemesi söylenirse sonsuza kadar denemesi söylenmiş olur.
   */
  it.each(LOCALES)(
    '%s: kural gereği red tekrar denemeyi önermez, eşzamanlılık kaybı önerir',
    (locale) => {
      const t = createTranslator(locale)

      for (const prefix of ['staff.deactivate.error', 'staff.role.error']) {
        expect(t(`${prefix}.last_active_manager`)).not.toMatch(TEKRAR_DENE[locale])
        expect(t(`${prefix}.concurrency_lost`)).toMatch(TEKRAR_DENE[locale])
      }

      // AC-33 — zaten pasifleştirilmiş de tekrar denemeyi önermez.
      expect(t('staff.deactivate.error.already_deactivated')).not.toMatch(TEKRAR_DENE[locale])
    },
  )
})
