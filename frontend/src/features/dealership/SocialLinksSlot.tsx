import { createElement, useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import type { PublicSocialLink } from '../../shared/api/types'
import { loadSocialLinks, type SocialIconLinks } from './socialLinksLoader'
import { SOCIAL_PLATFORM_NAMES } from './socialPlatforms'
import type { GalleryLoad } from './useGallery'

/**
 * `0024` R4 · D6 · D9(b) — altlığın "Bizi takip edin" yuvası. Girişteki TEK karar: en az bir hesap
 * varsa tembel parçayı iste (AC-68). Simgeli bağlantıları parça kurar; giriş yalnız kabuğu (başlık,
 * liste) ve parça reddedildiğinde gereken metin yedeğini taşır (QA 2 BULGU-1).
 *
 * Hesap yokken, okuma sürerken ya da hata verdiğinde blok başlığıyla birlikte hiç basılmaz ve hata
 * metni yoktur (AC-40–AC-42). Parça gelene kadar da blok yoktur: altlık ilk ekranın altındadır
 * (AC-80) ve blok geldiğinde bağlantılar son boyutlarıyla bir kez basılır (AC-70). Parça
 * reddedilirse her bağlantı adını görünür metin olarak gösterir, hedef aynıdır ve hata yakalanır
 * (AC-69, AC-79). LinkedIn'in simgesi yoktur, adıyla gösterilir (D13, AC-81). D1 dışı kod sunucuda
 * süzülür (D3); burada ikinci süzgeç yok.
 *
 * Durum: `undefined` bekliyor, işlev geldi, `null` reddedildi.
 */
export function SocialLinksSlot({
  load,
  loadBlock = loadSocialLinks,
}: {
  readonly load: GalleryLoad
  readonly loadBlock?: () => Promise<SocialIconLinks>
}) {
  const { t } = useLocale()
  const [render, setRender] = useState<SocialIconLinks | null>()
  const links = load.kind === 'ready' ? load.gallery.socialLinks : []
  const needed = links.length > 0

  useEffect(() => {
    if (!needed) {
      return
    }

    let active = true

    loadBlock().then(
      (loaded) => active && setRender(() => loaded),
      () => active && setRender(null),
    )

    return () => {
      active = false
    }
  }, [needed, loadBlock])

  if (!needed || render === undefined) {
    return null
  }

  const text = (link: PublicSocialLink) => (
    <li key={link.platform}>
      <a className="social-link social-link--text" href={link.url} rel="me">
        {SOCIAL_PLATFORM_NAMES[link.platform] ?? link.platform}
      </a>
    </li>
  )

  return (
    <nav className="home-footer__nav" aria-labelledby="social-title" data-testid="footer-social">
      <h2 className="home-footer__heading" id="social-title">
        {t('home.footer.follow')}
      </h2>
      <ul className="social-links">
        {render === null
          ? links.map(text)
          : render(createElement, links, SOCIAL_PLATFORM_NAMES, text)}
      </ul>
    </nav>
  )
}
