import { useState, type FormEvent } from 'react'
import { useLocale } from '../../../app/localeContext'
import { Button } from '../../../shared/ui'
import { parseRange, type ListRange } from '../listUrlState'

interface Props {
  readonly id: string
  readonly legend: string
  readonly range: ListRange | undefined
  readonly summary: string
  readonly onApply: (range: ListRange | undefined) => void
  readonly onClear: () => void
  /** Eksenin adını taşır; altı aynı adlı düğme ayırt edilemezdi (bkz. `ChipAxis`). */
  readonly clearLabel: string
}

/**
 * Aralık ekseni (`0015` R3, R15). Yazarken adres DEĞİŞMEZ; aralık Enter'la veya "Uygula"
 * ile uygulanır ve geçmişe TEK kayıt ekler (D19, AC-32). Her tuş vuruşunu adrese yazmak
 * geçmişe kayıt yığar ve geri tuşunu bozardı.
 *
 * Kendi `<form>`'u vardır: Enter tarayıcının yerel gönderimiyle çalışır, ayrı bir klavye
 * dinleyicisi yazılmaz. Taslak, uygulanmış aralık değiştiğinde (geri tuşu, temizle) sıfırlanır.
 */
export function RangeAxis({ id, legend, range, summary, onApply, onClear, clearLabel }: Props) {
  const { t } = useLocale()
  const applied = `${range?.min ?? ''}|${range?.max ?? ''}`
  const [draft, setDraft] = useState({ key: applied, min: text(range?.min), max: text(range?.max) })
  const current =
    draft.key === applied ? draft : { key: applied, min: text(range?.min), max: text(range?.max) }

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    // Uçtaki kural sunucununkiyle aynı (R15): geçersiz sınır ya da alt > üst → eksen yok.
    onApply(parseRange(current.min.trim(), current.max.trim()))
  }

  return (
    <fieldset className="filter-axis" data-testid={`${id}-filter`}>
      <legend className="filter-bar__legend">{legend}</legend>

      <form className="range-axis" onSubmit={onSubmit}>
        <label className="range-axis__field" htmlFor={`${id}-min`}>
          <span className="range-axis__label">{t('showcase.filter.range.min')}</span>
          <input
            id={`${id}-min`}
            className="range-axis__input"
            inputMode="numeric"
            autoComplete="off"
            value={current.min}
            onChange={(event) => setDraft({ ...current, min: event.target.value })}
          />
        </label>
        <label className="range-axis__field" htmlFor={`${id}-max`}>
          <span className="range-axis__label">{t('showcase.filter.range.max')}</span>
          <input
            id={`${id}-max`}
            className="range-axis__input"
            inputMode="numeric"
            autoComplete="off"
            value={current.max}
            onChange={(event) => setDraft({ ...current, max: event.target.value })}
          />
        </label>
        <Button type="submit" variant="secondary">
          {t('showcase.filter.range.apply')}
        </Button>
      </form>

      <div className="filter-bar__summary">
        <span data-testid={`${id}-summary`}>{summary}</span>
        <Button variant="text" onClick={onClear} disabled={range === undefined}>
          {clearLabel}
        </Button>
      </div>
    </fieldset>
  )
}

function text(value: number | null | undefined): string {
  return value === null || value === undefined ? '' : String(value)
}
