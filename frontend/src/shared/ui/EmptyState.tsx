import type { ReactNode } from 'react'

interface Props {
  readonly title: string
  readonly description: string
  /** Tek eylem: ziyaretçiye ne yapabileceğini söyler (§ 3). */
  readonly action?: ReactNode
  readonly testId?: string
  /**
   * `0022` D17 — açıklamadaki satır sonunu korur. Varsayılan KAPALI: yalnız galerinin yazdığı
   * çok satırlı metni basan kullanım açar, diğer kullanımların görünümü değişmez.
   */
  readonly preserveLineBreaks?: boolean
}

/**
 * docs/frontend.md § 3 — "Sonuç yok" yeterli değildir: neden boş ve kullanıcı ne
 * yapabilir söylenir. Bu yüzden `description` opsiyonel DEĞİLDİR.
 */
export function EmptyState({ title, description, action, testId, preserveLineBreaks }: Props) {
  return (
    <div className="ui-state" data-testid={testId}>
      <h2 className="ui-state__title">{title}</h2>
      <p className={preserveLineBreaks ? 'ui-state__body ui-state__body--lines' : 'ui-state__body'}>
        {description}
      </p>
      {action}
    </div>
  )
}
