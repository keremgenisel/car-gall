import type { SOCIAL_ICON_PATHS } from './socialIcons'

export type IconPaths = typeof SOCIAL_ICON_PATHS

let shared: Promise<IconPaths> | null = null

/**
 * `0024` D9(b) — simge parçası (`social-icons`) sayfa başına TEK istekle gelir (AC-78): söz
 * modül düzeyinde tutulur. Yalnız en az bir hesap varken çağrılır (AC-68). Tip içe aktarması
 * derlemede silinir; giriş paketi yol verisini taşımaz (AC-66).
 */
export function loadSocialIcons(): Promise<IconPaths> {
  shared ??= import('./socialIcons').then((module) => module.SOCIAL_ICON_PATHS)
  return shared
}
