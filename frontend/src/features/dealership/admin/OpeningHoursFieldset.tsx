import { useLocale } from '../../../app/localeContext'
import { weekdayName } from '../../../shared/i18n/format'
import { Button } from '../../../shared/ui'
import { OPENING_DAYS } from '../openingDays'
import { fieldErrorKey } from './dealershipForm'
import {
  addInterval,
  changeInterval,
  copyPrevious,
  MAX_INTERVALS,
  removeInterval,
  type Schedule,
} from './openingHoursForm'

/**
 * `0025` R1 · D5 — şube formunun "Çalışma saatleri" bölümü. Yedi gün D6 sırasıyla, gün adı
 * ekranın dilinde `Intl`'den. Gün ya "Kapalı"dır ya da 1–3 aralık taşır; saat alanı
 * `type="time"`, değeri her dilde `HH:mm`'dir (tuzak 4). Hata ilgili aralığın ya da günün
 * yanında çevrilmiş metinle (AC-22). Kapatılmış şubede salt okunur (`0020` D21).
 */
export function OpeningHoursFieldset({
  schedule,
  errors,
  disabled,
  onChange,
}: {
  readonly schedule: Schedule
  readonly errors: Readonly<Record<string, string>>
  readonly disabled: boolean
  readonly onChange: (schedule: Schedule) => void
}) {
  const { locale, t } = useLocale()
  // Hata metninin kimliği; alan onu `aria-describedby` ile açıklama olarak taşır (`0020`
  // `DealershipField` deseni, QA 2 BULGU-2). Noktalar kimlikte sorun değil ama seçicide kaçış
  // ister; kimlik alan anahtarından türetilir ve tirelidir.
  const errorId = (field: string) => `error-${field.replace(/\./g, '-')}`
  const describedBy = (field: string) => (errors[field] === undefined ? undefined : errorId(field))
  const error = (field: string) => {
    const code = errors[field]
    return code === undefined ? null : (
      <span className="admin-form__field-error" id={errorId(field)} data-testid={`error-${field}`}>
        {t(fieldErrorKey(code, locale))}
      </span>
    )
  }

  return (
    <fieldset className="admin-form__group opening-hours" data-testid="branch-opening-hours">
      <legend className="admin-form__label">{t('dealership.branch.hours.title')}</legend>
      <p className="admin-form__hint">{t('dealership.branch.hours.hint')}</p>
      {OPENING_DAYS.map((day, dayIndex) => {
        const name = weekdayName(dayIndex, locale)
        const intervals = schedule[day]

        return (
          <div key={day} className="opening-hours__day" data-testid={`hours-${day}`}>
            <p className="opening-hours__name" id={`hours-${day}-name`}>
              {name}
            </p>
            {intervals.length === 0 ? (
              <p className="opening-hours__closed">{t('dealership.branch.hours.closed')}</p>
            ) : (
              intervals.map((interval, index) => {
                const field = `openingHours.${day}.${index}`
                // Erişilebilir ad: gün adı + aralık sırası + "Açılış"/"Kapanış" (parametresiz
                // anahtarlar; `aria-labelledby` parçaları birleştirir).
                const id = `hours-${day}-${index}`
                return (
                  <div key={index} className="opening-hours__interval">
                    <span id={`${id}-n`} className="visually-hidden">
                      {index + 1}
                    </span>
                    <span id={`${id}-start`} className="visually-hidden">
                      {t('dealership.branch.hours.opens')}
                    </span>
                    <span id={`${id}-end`} className="visually-hidden">
                      {t('dealership.branch.hours.closes')}
                    </span>
                    <input
                      type="time"
                      className="admin-form__control"
                      aria-labelledby={`hours-${day}-name ${id}-n ${id}-start`}
                      value={interval.start}
                      disabled={disabled}
                      aria-invalid={errors[`${field}.start`] === undefined ? undefined : true}
                      aria-describedby={describedBy(`${field}.start`)}
                      onChange={(event) =>
                        onChange(
                          changeInterval(schedule, day, index, { start: event.target.value }),
                        )
                      }
                    />
                    <input
                      type="time"
                      className="admin-form__control"
                      aria-labelledby={`hours-${day}-name ${id}-n ${id}-end`}
                      value={interval.end}
                      disabled={disabled}
                      aria-invalid={errors[`${field}.end`] === undefined ? undefined : true}
                      aria-describedby={describedBy(`${field}.end`)}
                      onChange={(event) =>
                        onChange(changeInterval(schedule, day, index, { end: event.target.value }))
                      }
                    />
                    {disabled ? null : (
                      // QA 2 BULGU-2 — ad parametresiz parçalardan: düğmenin kendi metni ("Kaldır"),
                      // gün adı ve aralık sırası. 21 düğme 21 ayrı ad taşır.
                      <Button
                        variant="secondary"
                        id={`${id}-remove`}
                        aria-labelledby={`${id}-remove hours-${day}-name ${id}-n`}
                        onClick={() => onChange(removeInterval(schedule, day, index))}
                      >
                        {t('dealership.branch.hours.remove')}
                      </Button>
                    )}
                    {error(`${field}.start`)}
                    {error(`${field}.end`)}
                  </div>
                )
              })
            )}
            {disabled ? null : (
              <div className="opening-hours__actions">
                {intervals.length >= MAX_INTERVALS ? null : (
                  <Button
                    variant="secondary"
                    data-testid={`hours-add-${day}`}
                    onClick={() => onChange(addInterval(schedule, day))}
                  >
                    {t('dealership.branch.hours.add')}
                  </Button>
                )}
                {dayIndex === 0 ? null : (
                  <Button
                    variant="secondary"
                    data-testid={`hours-copy-${day}`}
                    onClick={() => onChange(copyPrevious(schedule, day))}
                  >
                    {t('dealership.branch.hours.copyPrevious')}
                  </Button>
                )}
              </div>
            )}
            {error(`openingHours.${day}`)}
          </div>
        )
      })}
    </fieldset>
  )
}
