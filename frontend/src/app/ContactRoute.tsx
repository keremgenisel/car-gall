import { Suspense, lazy, useState } from 'react'
import { useGallery } from '../features/dealership/useGallery'
import { Skeleton } from '../shared/ui'
import { AppShell } from './AppShell'
import { useLocale } from './localeContext'

/**
 * `0028` **D11** · **D14** — sayfanın gövdesi, harita kutusu, saat kodu ve metinleri TEK tembel
 * parçadadır; ana sayfa, liste ve detay onu istemez (AC-75).
 */
const contactPage = () => import('../features/dealership/contactPage')
const ContactBranches = lazy(() =>
  contactPage().then((module) => ({ default: module.ContactBranches })),
)

/**
 * `0028` **D4** · **D14** · **P6** — iletişim sayfasının rota kabuğu, ilk pakette: `<h1>` ve iskelet
 * parçayı ve okumayı beklemez (CLARIFY B5, AC-38). Parçanın `import()`'u ve galeri okuması AYNI
 * render'da başlar; okuma ilk paketteki `fetchGallery` iledir, parça okumayı başlatmaz (AC-95).
 * Okuma ya da parça sürerken iskelet görünür; ikisi hazır olunca şube bölümleri bir kerede basılır
 * (evre B yok, D11).
 */
export function ContactRoute() {
  const { t } = useLocale()
  const [attempt, setAttempt] = useState(0)
  const gallery = useGallery(attempt)

  // Tek seferlik başlangıç: tembel bileşen basılmadan parça isteği yola çıkar (AC-95).
  useState(() => void contactPage())

  const skeleton = (
    <div
      className="contact-page__skeleton"
      role="status"
      aria-live="polite"
      data-testid="contact-skeleton"
    >
      <span className="visually-hidden">{t('home.footer.loading')}</span>
      <Skeleton shape="line" />
      <Skeleton shape="line" />
      <Skeleton shape="line" />
    </div>
  )

  return (
    <AppShell>
      <div className="page contact-page">
        <h1 className="page__title">{t('contact.title')}</h1>
        {gallery.kind === 'loading' ? (
          skeleton
        ) : (
          <Suspense fallback={skeleton}>
            <ContactBranches load={gallery} onRetry={() => setAttempt((value) => value + 1)} />
          </Suspense>
        )}
      </div>
    </AppShell>
  )
}
