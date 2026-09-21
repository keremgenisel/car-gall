import { useLocale } from '../../../app/localeContext'

/**
 * R12(b): vitrinde hiç yayında ilan yok. İkisi aynı anda doğruysa BU kazanır —
 * aksi hâlde "seçimi temizle" ziyaretçiyi olmayan bir sonuca yollar (AC-27).
 */
export function EmptyShowcase() {
  const { t } = useLocale()

  return (
    <div className="state-panel" data-testid="empty-showcase">
      <h2>{t('showcase.empty.showcase.title')}</h2>
      <p>{t('showcase.empty.showcase.body')}</p>
      <a className="link-button" href="#contact">
        {t('showcase.empty.showcase.action')}
      </a>
    </div>
  )
}
