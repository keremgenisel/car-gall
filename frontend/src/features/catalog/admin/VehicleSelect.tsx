import { useLocale } from '../../../app/localeContext'
import type { ReferenceOption } from '../../../shared/api/types'

interface Props {
  readonly id: string
  readonly label: string
  readonly options: readonly ReferenceOption[]
  readonly value: string
  readonly errorKey?: string
  /**
   * `0020` P7 — listede görünen ama SEÇİLEMEYEN değerler: aracın kayıtlı şubesi kapatılmışsa
   * seçili kalır, başka bir değerden ona dönülemez.
   */
  readonly disabledValues?: readonly string[]
  /** Alanın altındaki uyarının katalog anahtarı (`0020` AC-58: "başka bir şube seçin"). */
  readonly hintKey?: string
  /** `0021` AC-23 — model seçicisi marka seçilene kadar devre dışıdır. */
  readonly disabled?: boolean
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
export function VehicleSelect({
  id,
  label,
  options,
  value,
  errorKey,
  disabledValues = [],
  hintKey,
  disabled = false,
  onChange,
}: Props) {
  const { t } = useLocale()
  const errorId = errorKey === undefined ? undefined : `${id}-error`
  const hintId = hintKey === undefined ? undefined : `${id}-hint`
  const describedBy = [hintId, errorId].filter((item) => item !== undefined).join(' ')

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      <select
        id={id}
        className="admin-form__control"
        value={value}
        disabled={disabled}
        aria-describedby={describedBy.length > 0 ? describedBy : undefined}
        aria-invalid={errorKey === undefined ? undefined : true}
        onChange={(event) => onChange(event.target.value)}
      >
        <option value="">{t('vehicle.form.choose')}</option>
        {options.map((option) => (
          <option
            key={option.value}
            value={option.value}
            disabled={disabledValues.includes(option.value)}
          >
            {option.label}
          </option>
        ))}
      </select>
      {hintKey === undefined ? null : (
        <p className="admin-form__hint" id={hintId} data-testid={`${id}-hint`}>
          {t(hintKey)}
        </p>
      )}
      {errorKey === undefined ? null : (
        <p className="admin-form__field-error" id={errorId} role="alert">
          {t(errorKey)}
        </p>
      )}
    </div>
  )
}
