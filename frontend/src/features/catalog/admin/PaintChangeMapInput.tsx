import { useLocale } from '../../../app/localeContext'
import { intlLocale } from '../../../shared/lib/locale'
import { PAINT_CHANGE_PARTS, PAINT_CHANGE_STATES } from '../../../shared/lib/paintChange'

interface Props {
  readonly fieldKey: string
  readonly label: string
  /** parça kodu → durum kodu; boş dize "seçilmedi". */
  readonly value: Readonly<Record<string, string>>
  /** Sunucunun bildirdiği eksik parçalar (KOD, C10 sırasıyla) — yalnız ret sonrasında dolu. */
  readonly missingParts: readonly string[]
  readonly onChange: (part: string, state: string) => void
}

/**
 * `0016a` R12 — parça haritasının panel girişi: C10 sırasıyla **13 seçim kutusu**.
 *
 * <ul>
 * <li>Her kutu boş bir seçenek ve dört durumu sunar; hiçbiri önceden seçili gelmez —
 * "orijinal" varsayılan DEĞİLDİR (D7, AC-24, AC-25).</li>
 * <li>Yarım harita sunucuda reddedilir; ret sonrasında eksik kutular `aria-invalid` olur ve
 * eksik parçaların adı etkin locale'in liste biçimiyle yazılır (AC-29). Diğer seçimler
 * korunur (AC-30) — değerler formun durumunda yaşar, ret onlara dokunmaz.</li>
 * <li>Parça ve durum ADLARI çeviri kataloğundan; sunucu yalnız kod taşır (C5, C6).</li>
 * </ul>
 */
export function PaintChangeMapInput({ fieldKey, label, value, missingParts, onChange }: Props) {
  const { locale, t } = useLocale()
  const errorId = `vehicle-field-${fieldKey}-error`
  const missingNames = new Intl.ListFormat(intlLocale(locale), {
    style: 'long',
    type: 'conjunction',
  }).format(missingParts.map((part) => t(`paintChange.part.${part}`)))

  return (
    <fieldset className="admin-form__group paint-change-input" data-testid="paint-change-map-input">
      <legend className="admin-form__label">{label}</legend>
      <p className="admin-form__hint">{t('paintChange.input.hint')}</p>

      <div className="paint-change-input__grid">
        {PAINT_CHANGE_PARTS.map((part) => {
          const id = `vehicle-field-${fieldKey}-${part}`
          const invalid = missingParts.includes(part)

          return (
            <div className="admin-form__field" key={part}>
              <label className="admin-form__label" htmlFor={id}>
                {t(`paintChange.part.${part}`)}
              </label>
              <select
                id={id}
                className="admin-form__control"
                data-part={part}
                value={value[part] ?? ''}
                aria-invalid={invalid ? 'true' : undefined}
                aria-describedby={invalid ? errorId : undefined}
                onChange={(event) => onChange(part, event.target.value)}
              >
                <option value="">{t('paintChange.state.none')}</option>
                {PAINT_CHANGE_STATES.map((state) => (
                  <option key={state} value={state}>
                    {t(`paintChange.state.${state}`)}
                  </option>
                ))}
              </select>
            </div>
          )
        })}
      </div>

      {missingParts.length > 0 ? (
        <p
          className="admin-form__error-body"
          id={errorId}
          role="alert"
          data-testid="paint-change-missing"
        >
          {t('vehicle.form.error.paint_change_map_incomplete', { parts: missingNames })}
        </p>
      ) : null}
    </fieldset>
  )
}
