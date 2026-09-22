import { describe, expect, it } from 'vitest'
import { catalogKeys, createTranslator } from '../../shared/i18n/i18n'
import { STAFF_ROLES } from '../../shared/api/types'
import type { AppLocale } from '../../shared/lib/locale'

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
