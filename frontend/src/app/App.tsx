import { LocaleProvider } from './LocaleProvider'
import { ShowcaseListPage } from '../features/catalog/ShowcaseListPage'
import type { AppLocale } from '../shared/lib/locale'

export function App({ locale }: { readonly locale: AppLocale }) {
  return (
    <LocaleProvider locale={locale}>
      <ShowcaseListPage />
    </LocaleProvider>
  )
}
