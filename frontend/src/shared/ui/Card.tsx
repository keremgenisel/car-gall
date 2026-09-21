import type { ReactNode } from 'react'

interface Props {
  /** Kartın semantik öğesi: ızgara içindeyse `li`, tek başınaysa `article`. */
  readonly as?: 'article' | 'li' | 'div'
  readonly children: ReactNode
  readonly testId?: string
}

/** docs/frontend.md § 4.2 — içerik kutusu: kenarlık, köşe, iç boşluk. */
export function Card({ as: Element = 'article', children, testId }: Props) {
  return (
    <Element className="ui-card" data-testid={testId}>
      {children}
    </Element>
  )
}
