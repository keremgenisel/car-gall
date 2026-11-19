import { Suspense, lazy } from 'react'
import { AppShell } from './AppShell'
import { HomeFooter } from './HomeFooter'
import { HomePage } from './HomePage'
import { BranchList } from '../features/dealership/BranchList'
import { useGallery } from '../features/dealership/useGallery'
import { LocaleProvider } from './LocaleProvider'
import { ListingDetailPage } from '../features/catalog/ListingDetailPage'
import { ContentPage } from '../features/content/ContentPage'
import { ShowcaseListPage } from '../features/catalog/ShowcaseListPage'
import { LeadForm } from '../features/leads/LeadForm'
import type { AppLocale } from '../shared/lib/locale'
import { parseRoute } from '../shared/lib/route'

/**
 * Yönetim paneli AYRI BİR UYGULAMA DEĞİLDİR: aynı Vite uygulamasında `/admin` rota
 * ağacı, lazy-load (0006 C13). Ziyaretçi vitrini açtığında panelin kodu hiç inmez.
 */
const AdminApp = lazy(() =>
  import('./admin/AdminApp').then((module) => ({ default: module.AdminApp })),
)

/**
 * Rota adresten okunur; router kütüphanesi yoktur. Detay adresine gidiş tam sayfa
 * gezinmedir, bu yüzden rota bir kez render anında çözülür — istemci tarafı geçiş
 * olmadığı için popstate dinlemesine gerek yok.
 */
export function App({ locale }: { readonly locale: AppLocale }) {
  const route = parseRoute(window.location.pathname)

  // Panel kendi kabuğunu taşır: vitrinin üst barı ve dil değiştiricisi orada yanlış
  // olurdu — panelin dili personelin tercihidir, ziyaretçinin seçimi değil (R16).
  if (route.kind === 'admin') {
    return (
      <LocaleProvider locale={locale}>
        <Suspense fallback={<div className="admin-page admin-page--narrow" />}>
          {/* Panelin alt yüzeyi adresten geliyor; kabuk onu propla alıyor (0010a). */}
          <AdminApp
            view={route.view}
            vehicleId={route.vehicleId}
            pageId={route.pageId}
            branchId={route.branchId}
            brandId={route.brandId}
          />
        </Suspense>
      </LocaleProvider>
    )
  }

  if (route.kind === 'home') {
    return (
      <LocaleProvider locale={locale}>
        <HomeRoute />
      </LocaleProvider>
    )
  }

  return (
    <LocaleProvider locale={locale}>
      <AppShell>
        {route.kind === 'detail' ? (
          <ListingDetailPage
            slug={route.slug}
            /*
              Talep formunu KABUK bağlar: `features/catalog` ile `features/leads`
              birbirini import edemez (docs/frontend.md § 1). İki feature'ı birlikte
              tanıyabilen tek yer burasıdır.
            */
            renderLead={(detail) => (
              <LeadForm listingSlug={detail.slug} isReserved={detail.isReserved} />
            )}
          />
        ) : route.kind === 'content' ? (
          /* `0017` R11 — tek parçalı adres bir içerik sayfası adayıdır (D9). */
          <ContentPage slug={route.slug} />
        ) : (
          <ShowcaseListPage />
        )}
      </AppShell>
    </LocaleProvider>
  )
}

/**
 * 0014 — galeri okuması BİR KEZ yapılır ve iki yere dağıtılır: adı giriş bölümüne, şubeleri
 * altlığa. Okuma `LocaleProvider`'ın içinde olmalı; bu yüzden ayrı bir bileşen.
 */
function HomeRoute() {
  const gallery = useGallery()

  return (
    <AppShell footer={<HomeFooter branches={<BranchList load={gallery} />} />}>
      <HomePage gallery={gallery} />
    </AppShell>
  )
}
