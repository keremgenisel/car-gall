import { useLocale } from '../../../app/localeContext'

/**
 * AC-58 — ne olduğunu ve ne zaman tekrar denenebileceğini söyler.
 *
 * Bu metin AC-8 gereği kayıtlı OLMAYAN kimlikler için de aynı şekilde çıkar; dolayısıyla
 * ayırt edici değildir ve hangi e-postaların kayıtlı olduğunu sızdırmaz. Sunucudan
 * "kilitlendin" diye bir sinyal gelmiyor — gelseydi sızıntı tam da o kanaldan olurdu
 * (Tuzak 7).
 */
export function LoginLocked() {
  const { t } = useLocale()

  return (
    <div className="admin-form__notice" role="status" data-testid="login-locked">
      <p className="admin-form__error-title">{t('login.locked.title')}</p>
      <p className="admin-form__error-body">{t('login.locked.body')}</p>
    </div>
  )
}
