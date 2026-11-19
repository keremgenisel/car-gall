import { useState, type FormEvent } from 'react'
import { useLocale } from '../../../app/localeContext'
import { homeText } from '../../../shared/lib/homeTexts'
import { listPath } from '../../../shared/lib/route'
import { Button } from '../../../shared/ui'
import type { ShowcaseBrand } from '../../../shared/api/types'
import type { BodyTypeOptions } from '../bodyTypeOptions'
import { toCanonicalSearch } from '../listUrlState'

interface Props {
  /** Görünür ilanların toplamı; seçimle DEĞİŞMEZ (AC-41). */
  readonly total: number
  readonly options: BodyTypeOptions
  /**
   * `0021` R13, R14 — "ana sayfada göster" işaretli ve stoğu olan markalar. Boşsa marka alanı
   * hiç basılmaz (AC-50, AC-51).
   */
  readonly brands?: readonly ShowcaseBrand[]
  /** Tam sayfa gezinme. Test bir casus verir; tarayıcıda `location.assign`. */
  readonly onNavigate?: (url: string) => void
}

/**
 * 0014 R5, R6 · D1, D19, D25 — ana sayfanın daraltma formu. Bu sürümde tek eksen kasa
 * tipidir (X10). Gönderim listenin KANONİK adresini üretir (`0001` C13): çoklu seçim
 * tekrarlı parametre, seçim yoksa parametresiz `/{locale}/cars` (AC-38, AC-39). Yerel
 * `GET` gönderimi kullanılmadı: hiç seçim yokken `/tr/cars?` üretir.
 */
export function NarrowingForm({ total, options, brands = [], onNavigate = navigate }: Props) {
  const { locale, t } = useLocale()
  const [selected, setSelected] = useState<readonly string[]>([])
  const [selectedBrands, setSelectedBrands] = useState<readonly string[]>([])

  const toggle = (slug: string) =>
    setSelected((current) =>
      current.includes(slug) ? current.filter((item) => item !== slug) : [...current, slug],
    )

  const toggleBrand = (slug: string) =>
    setSelectedBrands((current) =>
      current.includes(slug) ? current.filter((item) => item !== slug) : [...current, slug],
    )

  const onSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    // Sıra filtre çubuğunun sırasıdır, tıklama sırası değil: aynı seçim aynı adresi üretir.
    const ordered = options.bodyTypes
      .map((item) => item.slug)
      .filter((slug) => selected.includes(slug))
    // `0021` AC-48, AC-49 — markalar da formdaki sırayla `brand` parametresine yazılır.
    const orderedBrands = brands
      .map((item) => item.slug)
      .filter((slug) => selectedBrands.includes(slug))
    onNavigate(
      `${listPath(locale)}${toCanonicalSearch({ bodyTypes: ordered, brands: orderedBrands, page: 1 })}`,
    )
  }

  return (
    <form className="narrowing-form" data-testid="narrowing-form" onSubmit={onSubmit}>
      <h2 className="narrowing-form__title">{homeText('form-title', t('home.form.title'))}</h2>

      {/* D25 — alan filtrelenemezse liste çubuğu göstermiyor; form da ekseni göstermez. */}
      {options.filterable ? (
        <fieldset className="narrowing-form__axis" data-testid="narrowing-form-body-types">
          <legend className="filter-bar__legend">{t('home.form.bodyType')}</legend>
          <div className="filter-bar__options">
            {options.bodyTypes.map((bodyType) => {
              const isSelected = selected.includes(bodyType.slug)

              return (
                <label
                  key={bodyType.slug}
                  className={isSelected ? 'filter-chip filter-chip--selected' : 'filter-chip'}
                >
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => toggle(bodyType.slug)}
                    value={bodyType.slug}
                  />
                  {bodyType.label}
                </label>
              )
            })}
          </div>
        </fieldset>
      ) : null}

      {brands.length > 0 ? (
        <fieldset className="narrowing-form__axis" data-testid="narrowing-form-brands">
          <legend className="filter-bar__legend">{t('home.form.brand')}</legend>
          <div className="filter-bar__options">
            {brands.map((brand) => {
              const isSelected = selectedBrands.includes(brand.slug)

              return (
                <label
                  key={brand.slug}
                  className={isSelected ? 'filter-chip filter-chip--selected' : 'filter-chip'}
                >
                  <input
                    type="checkbox"
                    checked={isSelected}
                    onChange={() => toggleBrand(brand.slug)}
                    value={brand.slug}
                  />
                  <bdi>{brand.label}</bdi>
                </label>
              )
            })}
          </div>
        </fieldset>
      ) : null}

      {/* AC-41 · `0021` D18 — düğmedeki sayı toplam yayındaki araçtır; seçimle değişmez. */}
      <Button type="submit">{t('home.form.submit', { count: total })}</Button>
    </form>
  )
}

function navigate(url: string): void {
  window.location.assign(url)
}
