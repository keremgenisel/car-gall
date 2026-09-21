import { useLocale } from '../../../app/localeContext'

/**
 * R12(a): daraltma sonucu boş. "Sonuç yok" yeterli değildir; ziyaretçiye ne
 * yapabileceği söylenir (docs/frontend.md § 3).
 */
export function EmptyByFilter({ onClear }: { readonly onClear: () => void }) {
  const { t } = useLocale()

  return (
    <div className="state-panel" data-testid="empty-by-filter">
      <h2>{t('showcase.empty.filtered.title')}</h2>
      <p>{t('showcase.empty.filtered.body')}</p>
      <button type="button" className="link-button" onClick={onClear}>
        {t('showcase.filter.clear')}
      </button>
    </div>
  )
}
