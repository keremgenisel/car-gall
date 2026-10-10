import { useLocale } from '../../../app/localeContext'
import type { VehicleAdminField } from '../../../shared/api/types'
import { SUPPORTED_LOCALES } from '../../../shared/lib/locale'
import { Field } from '../../../shared/ui'

interface Props {
  readonly field: VehicleAdminField
  readonly value: string
  readonly localizedValue: Readonly<Record<string, string>>
  readonly errorKey?: string
  readonly onChange: (value: string) => void
  readonly onLocalizedChange: (locale: string, value: string) => void
}

/**
 * Opsiyonel alanın TİPİNE göre giriş alanı (0010b R6, D17).
 *
 * <p>Alan adı çeviri kataloğundan gelir: `vehicle.field.<anahtar>`. Sunucu alan ADINI
 * göndermez, yalnız anahtarını — çevrilmiş ad veritabanında saklanmaz (Yasak Listesi 22,
 * 23).</p>
 *
 * <p><b>Zorunlu işaretli alan burada da ZORUNLU DEĞİL</b> (R8, AC-15): zorunluluk yalnız
 * yayına alırken bağlayıcı ve yayın `0012`'nin işi. İşaret bir uyarıdır, kapı değil.</p>
 */
export function OptionalFieldInput({
  field,
  value,
  localizedValue,
  errorKey,
  onChange,
  onLocalizedChange,
}: Props) {
  const { t } = useLocale()
  const label = t(`vehicle.field.${field.fieldKey}`)
  const labelWithHint = field.isRequired ? t('vehicle.form.field.requiredHint', { label }) : label

  if (field.kind === 'LocalizedText') {
    return (
      <fieldset className="admin-form__group">
        <legend className="admin-form__label">{labelWithHint}</legend>
        {SUPPORTED_LOCALES.map((locale) => (
          <Field
            key={locale}
            id={`vehicle-field-${field.fieldKey}-${locale}`}
            label={t('vehicle.form.field.inLocale', { locale: t(`app.locale.${locale}`) })}
            value={localizedValue[locale] ?? ''}
            errorKey={errorKey}
            onChange={(next) => onLocalizedChange(locale, next)}
          />
        ))}
      </fieldset>
    )
  }

  if (field.kind === 'Boolean') {
    return (
      <div className="admin-form__field">
        <label className="admin-form__check" htmlFor={`vehicle-field-${field.fieldKey}`}>
          <input
            id={`vehicle-field-${field.fieldKey}`}
            type="checkbox"
            checked={value === 'true'}
            onChange={(event) => onChange(event.target.checked ? 'true' : 'false')}
          />
          {labelWithHint}
        </label>
      </div>
    )
  }

  return (
    <Field
      id={`vehicle-field-${field.fieldKey}`}
      label={labelWithHint}
      // `tarih` ISO 8601 ister (`yyyy-MM-dd`); tarayıcının tarih alanı tam o biçimi
      // üretiyor ve belirsiz yazımı (15/03/2024) baştan imkânsız kılıyor.
      type={inputType(field.kind)}
      hint={field.kind === 'Amount' ? t('vehicle.form.field.amountHint') : undefined}
      value={value}
      errorKey={errorKey}
      onChange={onChange}
    />
  )
}

function inputType(kind: VehicleAdminField['kind']): 'text' | 'number' | 'date' {
  switch (kind) {
    case 'Amount':
    case 'Integer':
      return 'number'
    case 'Date':
      return 'date'
    default:
      return 'text'
  }
}
