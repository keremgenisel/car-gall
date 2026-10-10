import { useLocale } from '../../app/localeContext'

interface Props {
  readonly id: string
  readonly label: string
  readonly type?: 'text' | 'email' | 'password' | 'number' | 'date'
  readonly autoComplete?: string
  readonly hint?: string
  /** Sunucunun işaretlediği hata kodu; metin uçta üretilir (docs/conventions.md § 5). */
  readonly errorKey?: string
  readonly value: string
  readonly onChange: (value: string) => void
}

/**
 * Her alanın `<label>`'ı vardır; yalnız `placeholder` etiket değildir
 * (docs/frontend.md § 7). Hatalı alan `aria-invalid` ile işaretlenir.
 *
 * <b>0010a'da `features/identity/AdminField` buraya TERFİ ETTİ</b> (§ 4.2, öneri kuralı):
 * araç kaydı formu aynı alan davranışına ihtiyaç duyuyor ve iki feature birbirinin iç
 * dosyasını import edemez (§ 1). Kopyalamak, hata metninin `aria-describedby` bağını iki
 * yerde bakmak demekti. Terfiyle gelen tek davranış `type="number"`: model yılı ve
 * kilometre sayıdır ve mobil klavyenin sayı düzenine geçmesi `inputMode` olmadan olmaz.
 */
export function Field({
  id,
  label,
  type = 'text',
  autoComplete,
  hint,
  errorKey,
  value,
  onChange,
}: Props) {
  const { t } = useLocale()
  const hintId = hint === undefined ? undefined : `${id}-hint`
  const errorId = errorKey === undefined ? undefined : `${id}-error`
  const describedBy = [hintId, errorId].filter((item) => item !== undefined).join(' ')

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      {hint === undefined ? null : (
        <p className="admin-form__hint" id={hintId}>
          {hint}
        </p>
      )}
      <input
        id={id}
        className="admin-form__control"
        type={type}
        inputMode={type === 'number' ? 'numeric' : undefined}
        value={value}
        autoComplete={autoComplete}
        dir={type === 'text' ? 'auto' : 'ltr'}
        aria-describedby={describedBy.length > 0 ? describedBy : undefined}
        aria-invalid={errorKey === undefined ? undefined : true}
        onChange={(event) => onChange(event.target.value)}
      />
      {errorKey === undefined ? null : (
        <p className="admin-form__field-error" id={errorId} role="alert">
          {t(errorKey)}
        </p>
      )}
    </div>
  )
}
