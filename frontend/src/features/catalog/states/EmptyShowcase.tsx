import { useLocale } from '../../../app/localeContext'
import { EmptyState } from '../../../shared/ui'

/** 0001 R12(b): ikisi aynı anda doğruysa BU kazanır (AC-27). */
export function EmptyShowcase() {
  const { t } = useLocale()

  return (
    <EmptyState
      testId="empty-showcase"
      title={t('showcase.empty.showcase.title')}
      description={t('showcase.empty.showcase.body')}
      action={
        <a className="ui-button ui-button--text" href="#contact">
          {t('showcase.empty.showcase.action')}
        </a>
      }
    />
  )
}
