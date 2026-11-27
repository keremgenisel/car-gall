import '../../shared/i18n/contactCatalog'
import { use, useEffect, useRef, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import type { PublicBranch, PublicGallery } from '../../shared/api/types'
import { EmptyState, ErrorState } from '../../shared/ui'
import { BranchHours, NowLine } from './branchClock'
import { branchKey } from './branchDates'
import { contactFonts } from './contactFonts'
import type { GalleryLoad } from './useGallery'
import './contactPage.css'

/**
 * `0028` **D3** · **D4** · **D11** — iletişim sayfasının tembel parçası: şube bölümleri, durumlar,
 * harita kutusu ve saat kodu (`branchClock`, statik: ayrı istek yok, AC-45). `<h1>` ve iskelet rota
 * kabuğundadır (`app/ContactRoute`); bu bileşen okuma bittikten sonra basılır.
 */
export function ContactBranches({
  load,
  onRetry,
}: {
  readonly load: Exclude<GalleryLoad, { readonly kind: 'loading' }>
  readonly onRetry: () => void
}) {
  const { locale, t } = useLocale()
  // AC-77 — bölümler kullandıkları yazı tipi yüzleriyle basılır (en çok ~1500 ms; `contactFonts`).
  // Beklerken rota kabuğunun iskeleti görünür (Suspense).
  use(contactFonts(locale))

  // D4 — sayfanın tek içeriği şubeler: altlıktan farklı olarak boş yer değil hata gösterilir (AC-39).
  if (load.kind === 'error') {
    return (
      <ErrorState
        testId="contact-error"
        title={t('contact.error.title')}
        description={t('contact.error.body')}
        retryLabel={t('contact.error.retry')}
        onRetry={onRetry}
      />
    )
  }

  if (load.gallery.branches.length === 0) {
    return (
      <EmptyState
        testId="contact-empty"
        title={t('contact.empty.title')}
        description={t('contact.empty.body')}
      />
    )
  }

  return (
    <div className="contact-page__branches" data-testid="contact-branches">
      {load.gallery.branches.map((branch) => (
        <BranchSection
          key={branchKey(branch)}
          gallery={load.gallery}
          branch={branch}
          receivedAt={load.receivedAt}
        />
      ))}
    </div>
  )
}

/**
 * D3 — bölüm içi sıra: ad · "şu an" · adres · telefon · e-posta · harita kutusu · "Google
 * Haritalar'da aç" · haftalık liste · özel günler (AC-33). Programsız şubede saat öğeleri yoktur
 * (AC-37), e-postasız şubede e-posta satırı yoktur (AC-36).
 */
function BranchSection({
  gallery,
  branch,
  receivedAt,
}: {
  readonly gallery: PublicGallery
  readonly branch: PublicBranch
  readonly receivedAt: number | undefined
}) {
  const { t } = useLocale()
  const id = `contact-branch-${branchKey(branch)}`

  return (
    <section className="contact-branch" aria-labelledby={id} data-testid="contact-branch">
      <div className="contact-branch__info">
        <h2 className="contact-branch__name" id={id} dir="auto">
          {branch.name}
        </h2>
        {/* `0027` D11 — iki satırlık sabit kutu; metin kaldırılsa da kutu kalır (AC-79). */}
        {branch.openingHours === null ? null : (
          <p className="contact-branch__now" data-testid="branch-now">
            <NowLine gallery={gallery} branch={branch} receivedAt={receivedAt} />
          </p>
        )}
        <p className="contact-branch__address" dir="auto">
          {branch.address}
        </p>
        <p className="contact-branch__line">
          <a href={`tel:${branch.phone}`} dir="ltr" data-testid="branch-phone">
            {branch.phone}
          </a>
        </p>
        {branch.email === null ? null : (
          <p className="contact-branch__line">
            <a href={`mailto:${branch.email}`} dir="ltr" data-testid="branch-email">
              {branch.email}
            </a>
          </p>
        )}
      </div>
      {/* D7 — sınırı aşan adreste `map` yoktur: kutu ve bağlantı basılmaz, adres metin kalır (AC-92). */}
      {branch.map === null ? null : (
        <div className="contact-branch__map">
          {branch.map.embedUrl === undefined ? null : (
            <MapBox name={branch.name} embedUrl={branch.map.embedUrl} />
          )}
          {/* D6 — aynı sekmede, `target` yok (`0024`'ün sosyal bağlantılarıyla aynı, AC-56). */}
          <a className="contact-branch__map-link" href={branch.map.linkUrl} data-testid="map-link">
            {t('contact.map.open')}
          </a>
        </div>
      )}
      {branch.openingHours === null ? null : (
        <div className="contact-branch__hours">
          <BranchHours gallery={gallery} branch={branch} receivedAt={receivedAt} />
        </div>
      )}
    </section>
  )
}

/**
 * `0028` **D6** · **D15** · **D16** — tıkla-yükle harita. Yer tutucu yalnız yerel CSS'tir: görsel,
 * iframe, dış istek yok (AC-47, AC-49). Düğmeye basınca AYNI kutuda iframe açılır; kutu 4:3 ve
 * boyutu değişmez (AC-46, AC-53), odak iframe'e geçer (AC-52, tuzak 8).
 *
 * CLARIFY B2 — açık/kapalı durumu BU bileşenin durumudur, yenilenen depoda değildir: D9 yenilemesi
 * yalnız "şu an" satırını ve "Bugün"ü değiştirir; bu bileşenin props'u ilk okumadan gelir, iframe
 * düğümü yerinde kalır ve `src`'si değişmez (AC-88–AC-90). PM şartı (b) — onay kalıcı değildir:
 * hiçbir depoya yazılmaz, sayfa yenilenince kutu yer tutucuya döner (AC-57, X7).
 */
function MapBox({ name, embedUrl }: { readonly name: string; readonly embedUrl: string }) {
  const { t } = useLocale()
  const [open, setOpen] = useState(false)
  const frame = useRef<HTMLIFrameElement>(null)

  useEffect(() => {
    if (open) {
      frame.current?.focus()
    }
  }, [open])

  return (
    <div className="contact-branch__map-box" data-testid="map-box">
      {open ? (
        <iframe
          ref={frame}
          className="contact-branch__map-frame"
          src={embedUrl}
          title={t('contact.map.frameTitle', { name })}
          referrerPolicy="strict-origin-when-cross-origin"
          data-testid="map-frame"
        />
      ) : (
        <div className="contact-branch__map-placeholder">
          <button
            type="button"
            className="ui-button ui-button--secondary"
            onClick={() => setOpen(true)}
            data-testid="map-show"
          >
            {t('contact.map.show')}
          </button>
          <p className="contact-branch__map-notice">{t('contact.map.notice')}</p>
        </div>
      )}
    </div>
  )
}
