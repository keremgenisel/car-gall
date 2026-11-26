import { useEffect, useState } from 'react'
import { useLocale } from '../../app/localeContext'
import type { PublicSocialLink } from '../../shared/api/types'
import { loadSocialIcons, type IconPaths } from './socialIconLoader'
import { socialPlatformName } from './socialPlatforms'
import type { GalleryLoad } from './useGallery'

type IconState =
  | { readonly kind: 'pending' }
  | { readonly kind: 'ready'; readonly paths: IconPaths }
  | { readonly kind: 'failed' }

/**
 * `0024` R4 · D6 — ana sayfa altlığının "Bizi takip edin" bloğu. Bağlantılar sunucunun verdiği
 * sırayladır (D1); hiç hesap yoksa, okuma sürerken ya da hata verdiğinde blok başlığıyla birlikte
 * HİÇ basılmaz ve hata metni yoktur (AC-40–AC-42).
 *
 * Bağlantının erişilebilir adı platformun adıdır (AC-60). Simge parçası gelene kadar kutu son
 * boyutundadır (AC-70); parça reddedilirse ad görünür metin olur ve hedef değişmez (AC-69, AC-79).
 * LinkedIn'in simgesi yoktur, adıyla gösterilir (D13, AC-81).
 */
export function SocialLinks({
  load,
  loadIcons = loadSocialIcons,
}: {
  readonly load: GalleryLoad
  readonly loadIcons?: () => Promise<IconPaths>
}) {
  const { t } = useLocale()
  const [icons, setIcons] = useState<IconState>({ kind: 'pending' })
  const links =
    load.kind === 'ready'
      ? load.gallery.socialLinks.filter((link) => socialPlatformName(link.platform) !== undefined)
      : []
  const needed = links.length > 0

  useEffect(() => {
    if (!needed) {
      return
    }

    let active = true

    loadIcons().then(
      (paths) => {
        if (active) {
          setIcons({ kind: 'ready', paths })
        }
      },
      () => {
        if (active) {
          setIcons({ kind: 'failed' })
        }
      },
    )

    return () => {
      active = false
    }
  }, [needed, loadIcons])

  if (!needed) {
    return null
  }

  return (
    <nav className="home-footer__nav" aria-labelledby="social-title" data-testid="footer-social">
      <h2 className="home-footer__heading" id="social-title">
        {t('home.footer.follow')}
      </h2>
      <ul className="social-links">
        {links.map((link) => (
          <li key={link.platform}>
            <SocialLink link={link} icons={icons} />
          </li>
        ))}
      </ul>
    </nav>
  )
}

function SocialLink({
  link,
  icons,
}: {
  readonly link: PublicSocialLink
  readonly icons: IconState
}) {
  const name = socialPlatformName(link.platform) ?? link.platform
  const path =
    icons.kind === 'ready' && link.platform in icons.paths
      ? icons.paths[link.platform as keyof IconPaths]
      : null

  // D13 · AC-69 — simgesi olmayan platform ve reddedilen parça: ad görünür metindir.
  if (link.platform === 'linkedin' || icons.kind === 'failed') {
    return (
      <a
        className="social-link social-link--text"
        href={link.url}
        rel="me"
        data-platform={link.platform}
      >
        {name}
      </a>
    )
  }

  return (
    <a
      className="social-link"
      href={link.url}
      rel="me"
      aria-label={name}
      data-platform={link.platform}
    >
      {path === null ? null : (
        <svg
          className="social-link__icon"
          viewBox="0 0 24 24"
          fill="currentColor"
          aria-hidden="true"
          focusable="false"
        >
          <path d={path} />
        </svg>
      )}
    </a>
  )
}
