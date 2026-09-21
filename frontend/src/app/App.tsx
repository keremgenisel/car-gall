import { AppShell } from './AppShell'
import { LocaleProvider } from './LocaleProvider'
import { ListingDetailPage } from '../features/catalog/ListingDetailPage'
import { ShowcaseListPage } from '../features/catalog/ShowcaseListPage'
import type { AppLocale } from '../shared/lib/locale'
import { parseRoute } from '../shared/lib/route'

/**
 * Rota adresten okunur; router kütüphanesi yoktur. Detay adresine gidiş tam sayfa
 * gezinmedir, bu yüzden rota bir kez render anında çözülür — istemci tarafı geçiş
 * olmadığı için popstate dinlemesine gerek yok.
 */
export function App({ locale }: { readonly locale: AppLocale }) {
  const route = parseRoute(window.location.pathname)

  return (
    <LocaleProvider locale={locale}>
      <AppShell>
        {route.kind === 'detail' ? <ListingDetailPage slug={route.slug} /> : <ShowcaseListPage />}
      </AppShell>
    </LocaleProvider>
  )
}
