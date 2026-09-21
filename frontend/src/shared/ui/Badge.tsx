import type { ReactNode } from 'react'

interface Props {
  readonly children: ReactNode
  /** Kartın medya alanı gibi bir kutunun köşesine oturur. */
  readonly floating?: boolean
}

/** docs/frontend.md § 4.2 — kısa durum etiketi. Renk tek başına anlam taşımaz (§ 7): metin zorunlu. */
export function Badge({ children, floating = false }: Props) {
  return <span className={floating ? 'ui-badge ui-badge--floating' : 'ui-badge'}>{children}</span>
}
