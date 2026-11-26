import type { socialIconLinks } from './socialIconLinks'

export type SocialIconLinks = typeof socialIconLinks

let shared: Promise<SocialIconLinks> | null = null

/**
 * `0024` D9(b) · QA 2 BULGU-1 — simgeli bağlantıları kuran işlev ve yol verisi TEMBEL parçadadır
 * (`social-icons`); giriş yalnız bu yükleyiciyi, "en az bir hesap varsa yükle" kararını ve parça
 * reddedildiğinde gereken metin yedeğini taşır. Söz modül düzeyinde tutulur: sayfa başına tek
 * istek (AC-78). Tip içe aktarması derlemede silinir; giriş parçayı statik içe aktarmaz.
 */
export function loadSocialLinks(): Promise<SocialIconLinks> {
  shared ??= import('./socialIconLinks').then((module) => module.socialIconLinks)
  return shared
}
