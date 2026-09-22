import { useLocale } from '../../app/localeContext'

interface Props {
  readonly id: string
  readonly label: string
  readonly type?: 'text' | 'email' | 'password'
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
 * Bu alan `shared/ui` envanterine GİRMİYOR: bugün yalnız panel ekranlarında kullanılıyor
 * ve tek feature'da kullanılan şey ortak bileşen değildir (§ 4.2). Vitrin tarafında bir
 * form doğduğunda terfi önerilecek.
 */
export function AdminField({
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
