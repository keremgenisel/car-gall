import type { ButtonHTMLAttributes, ReactNode } from 'react'

export type ButtonVariant = 'primary' | 'secondary' | 'text'

interface Props extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'className'> {
  readonly variant?: ButtonVariant
  readonly loading?: boolean
  readonly children: ReactNode
}

/**
 * docs/frontend.md § 4.2. Metin PROP olarak gelir; bileşen hiçbir kullanıcı metni
 * taşımaz (§ 5). Yükleniyor hâli düğmeyi devre dışı bırakır — çift gönderim,
 * kullanıcının iki kez tıklamasıyla değil, düğmenin iki kez tıklanabilir olmasıyla olur.
 */
export function Button({
  variant = 'primary',
  loading = false,
  disabled,
  children,
  ...rest
}: Props) {
  return (
    <button
      {...rest}
      type={rest.type ?? 'button'}
      className={`ui-button ui-button--${variant}`}
      disabled={disabled === true || loading}
      aria-busy={loading || undefined}
    >
      {children}
    </button>
  )
}
