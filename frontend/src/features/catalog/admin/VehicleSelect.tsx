import { useLocale } from '../../../app/localeContext'
import type { ReferenceOption } from '../../../shared/api/types'

interface Props {
  readonly id: string
  readonly label: string
  readonly options: readonly ReferenceOption[]
  readonly value: string
  readonly errorKey?: string
  readonly onChange: (value: string) => void
}

/**
 * Referans veri seçimi. Etiket personelin locale'inde gelir ve ham slug GÖSTERİLMEZ
 * (R4, AC-13) — bu yüzden seçenek metni `label`, değeri `value`.
 *
 * <b>`shared/ui`'ye girmez</b> (docs/frontend.md § 4.2): bugün yalnız bu feature'ın kayıt
 * formunda kullanılıyor ve tek ekranda kullanılan şey ortak bileşen değildir. İkinci bir
 * feature'ın seçim alanına ihtiyacı doğduğunda terfi önerilecek — `ConfirmDialog` ile aynı
 * gerekçe.
 *
 * Boş ilk seçenek VAR: seçimsiz hâl geçerli bir başlangıçtır ve ilk seçeneği sessizce
 * seçmek, personelin hiç bakmadığı bir değeri onaylamış gibi göstermek olurdu.
 */
export function VehicleSelect({ id, label, options, value, errorKey, onChange }: Props) {
  const { t } = useLocale()
  const errorId = errorKey === undefined ? undefined : `${id}-error`

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      <select
        id={id}
        className="admin-form__control"
        value={value}
        aria-describedby={errorId}
        aria-invalid={errorKey === undefined ? undefined : true}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">{t('vehicle.form.choose')}</option>
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {errorKey === undefined ? null : (
        <p className="admin-form__field-error" id={errorId} role="alert">
          {t(errorKey)}
        </p>
      )}
    </div>
  )
}
