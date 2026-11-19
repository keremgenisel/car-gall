import { useLocale } from '../../../app/localeContext'
import { ReferenceField } from './ReferenceField'

/**
 * `0021` R1, R2 · R3 — dil başına görünen ad; etkin her dil bir alan. Boş bırakılan dilde
 * vitrin kanonik adı gösterir (AC-30). Alan kendi dilinin yönündedir.
 */
export function DisplayNameFields({
  idPrefix,
  locales,
  values,
  errors,
  onChange,
}: {
  readonly idPrefix: string
  readonly locales: readonly string[]
  readonly values: Readonly<Record<string, string>>
  readonly errors: Readonly<Record<string, string>>
  readonly onChange: (next: Readonly<Record<string, string>>) => void
}) {
  const { t } = useLocale()

  return (
    <fieldset className="admin-form__group" data-testid={`${idPrefix}s`}>
      <legend className="admin-form__label">{t('reference.field.displayNames')}</legend>
      <p className="admin-form__hint">{t('reference.field.displayNamesHint')}</p>
      {locales.map((code) => (
        <ReferenceField
          key={code}
          id={`${idPrefix}-${code}`}
          label={t(`app.locale.${code}`)}
          messageKey={errors[`displayNames.${code}`]}
          value={values[code] ?? ''}
          dir="auto"
          lang={code}
          onChange={(value) => onChange({ ...values, [code]: value })}
        />
      ))}
    </fieldset>
  )
}
