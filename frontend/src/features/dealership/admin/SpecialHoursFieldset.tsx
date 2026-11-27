import { useLocale } from '../../../app/localeContext'
import { Button } from '../../../shared/ui'
import { fieldErrorKey } from './dealershipForm'
import { MAX_INTERVALS } from './openingHoursForm'
import {
  addEntry,
  addEntryInterval,
  changeEntry,
  MAX_SPECIAL_DAYS,
  removeEntry,
  setClosedAllDay,
  type SpecialEntry,
} from './specialHoursForm'

/**
 * `0027` **D13** — "Özel günler ve tatiller": "Çalışma saatleri"nin ardından, şubeyle birlikte
 * kaydedilir. Her kayıtta "Başlangıç"/"Bitiş" (`type="date"`), "Tam gün kapalı" ya da 1–3 aralık
 * (`0025` D5'in "Aralık ekle"/"Kaldır" eylemleri ve sınıfları). Hata kaydın ya da aralığın yanında,
 * sunucunun alan anahtarıyla (`specialHours.<i>…`); `<i>` ekrandaki sıradır (`ordered`).
 */
export function SpecialHoursFieldset({
  entries,
  errors,
  disabled,
  onChange,
}: {
  readonly entries: readonly SpecialEntry[]
  readonly errors: Readonly<Record<string, string>>
  readonly disabled: boolean
  readonly onChange: (entries: readonly SpecialEntry[]) => void
}) {
  const { locale, t } = useLocale()
  const errorId = (field: string) => `error-${field.replace(/\./g, '-')}`
  const describedBy = (field: string) => (errors[field] === undefined ? undefined : errorId(field))
  const invalid = (field: string) => (errors[field] === undefined ? undefined : true)
  const error = (field: string) => {
    const code = errors[field]
    return code === undefined ? null : (
      <span className="admin-form__field-error" id={errorId(field)} data-testid={`error-${field}`}>
        {t(fieldErrorKey(code, locale))}
      </span>
    )
  }
  const change = (index: number, entry: Partial<SpecialEntry>) =>
    onChange(changeEntry(entries, index, entry))

  return (
    <fieldset className="admin-form__group opening-hours" data-testid="branch-special-hours">
      <legend className="admin-form__label">{t('dealership.branch.special.title')}</legend>
      <p className="admin-form__hint">{t('dealership.branch.special.hint')}</p>
      {entries.map((entry, index) => {
        const field = `specialHours.${index}`
        const id = `special-${index}`
        const closed = entry.intervals.length === 0

        return (
          <div key={index} className="opening-hours__day" data-testid={`special-${index}`}>
            <p className="opening-hours__name" id={`${id}-name`}>
              {t('dealership.branch.special.entry')} {index + 1}
            </p>
            <div className="opening-hours__interval">
              <label className="admin-form__label" htmlFor={`${id}-from`}>
                {t('dealership.branch.special.from')}
              </label>
              <input
                id={`${id}-from`}
                type="date"
                className="admin-form__control"
                data-testid={`special-${index}-from`}
                value={entry.from}
                disabled={disabled}
                aria-invalid={invalid(`${field}.from`)}
                aria-describedby={describedBy(`${field}.from`)}
                onChange={(event) => change(index, { from: event.target.value })}
              />
              <label className="admin-form__label" htmlFor={`${id}-through`}>
                {t('dealership.branch.special.through')}
              </label>
              <input
                id={`${id}-through`}
                type="date"
                className="admin-form__control"
                data-testid={`special-${index}-through`}
                value={entry.through}
                disabled={disabled}
                aria-invalid={invalid(`${field}.through`)}
                aria-describedby={describedBy(`${field}.through`)}
                onChange={(event) => change(index, { through: event.target.value })}
              />
              {error(`${field}.from`)}
              {error(`${field}.through`)}
            </div>
            <label className="opening-hours__closed">
              <input
                type="checkbox"
                data-testid={`special-${index}-closed`}
                checked={closed}
                disabled={disabled}
                onChange={(event) => change(index, setClosedAllDay(entry, event.target.checked))}
              />{' '}
              {t('dealership.branch.special.closedAllDay')}
            </label>
            {entry.intervals.map((interval, j) => {
              const at = `${field}.intervals.${j}`
              const row = `${id}-${j}`
              return (
                <div key={j} className="opening-hours__interval">
                  <span id={`${row}-n`} className="visually-hidden">
                    {j + 1}
                  </span>
                  <span id={`${row}-start`} className="visually-hidden">
                    {t('dealership.branch.hours.opens')}
                  </span>
                  <span id={`${row}-end`} className="visually-hidden">
                    {t('dealership.branch.hours.closes')}
                  </span>
                  <input
                    type="time"
                    className="admin-form__control"
                    data-testid={`special-${index}-${j}-start`}
                    aria-labelledby={`${id}-name ${row}-n ${row}-start`}
                    value={interval.start}
                    disabled={disabled}
                    aria-invalid={invalid(`${at}.start`)}
                    aria-describedby={describedBy(`${at}.start`)}
                    onChange={(event) =>
                      change(index, {
                        intervals: entry.intervals.map((item, k) =>
                          k === j ? { ...item, start: event.target.value } : item,
                        ),
                      })
                    }
                  />
                  <input
                    type="time"
                    className="admin-form__control"
                    data-testid={`special-${index}-${j}-end`}
                    aria-labelledby={`${id}-name ${row}-n ${row}-end`}
                    value={interval.end}
                    disabled={disabled}
                    aria-invalid={invalid(`${at}.end`)}
                    aria-describedby={describedBy(`${at}.end`)}
                    onChange={(event) =>
                      change(index, {
                        intervals: entry.intervals.map((item, k) =>
                          k === j ? { ...item, end: event.target.value } : item,
                        ),
                      })
                    }
                  />
                  {disabled ? null : (
                    <Button
                      variant="secondary"
                      id={`${row}-remove`}
                      aria-labelledby={`${row}-remove ${id}-name ${row}-n`}
                      onClick={() =>
                        change(index, { intervals: entry.intervals.filter((_, k) => k !== j) })
                      }
                    >
                      {t('dealership.branch.hours.remove')}
                    </Button>
                  )}
                  {error(at)}
                  {error(`${at}.start`)}
                  {error(`${at}.end`)}
                </div>
              )
            })}
            {disabled ? null : (
              <div className="opening-hours__actions">
                {closed || entry.intervals.length >= MAX_INTERVALS ? null : (
                  <Button
                    variant="secondary"
                    data-testid={`special-${index}-add-interval`}
                    onClick={() => change(index, addEntryInterval(entry))}
                  >
                    {t('dealership.branch.hours.add')}
                  </Button>
                )}
                <Button
                  variant="secondary"
                  id={`${id}-remove`}
                  data-testid={`special-${index}-remove`}
                  aria-labelledby={`${id}-remove ${id}-name`}
                  onClick={() => onChange(removeEntry(entries, index))}
                >
                  {t('dealership.branch.special.remove')}
                </Button>
              </div>
            )}
            {error(field)}
            {error(`${field}.intervals`)}
          </div>
        )
      })}
      {disabled || entries.length >= MAX_SPECIAL_DAYS ? null : (
        <div className="opening-hours__actions">
          <Button
            variant="secondary"
            data-testid="special-add"
            onClick={() => onChange(addEntry(entries))}
          >
            {t('dealership.branch.special.add')}
          </Button>
        </div>
      )}
      {error('specialHours')}
    </fieldset>
  )
}
