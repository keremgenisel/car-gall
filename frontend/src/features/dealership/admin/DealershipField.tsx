import type { ReactNode } from 'react'
import { useLocale } from '../../../app/localeContext'
import { fieldErrorKey } from './dealershipForm'

export interface ControlProps {
  readonly id: string
  readonly 'aria-invalid': boolean | undefined
  readonly 'aria-describedby': string | undefined
}

/**
 * `0020` — galeri ve şube formlarının etiketli alanı. Hata KODU sunucudan gelir ve burada
 * çevrilir (C5); alan `aria-invalid` taşır ve hata metni alanın hemen altındadır (AC-3, AC-24).
 *
 * <b>`shared/ui/Field` neden yetmedi:</b> o alan yalnız `<input>` basıyor ve yönü tipten
 * çıkarıyor; burada saat dilimi bir `<select>`, adres bir `<textarea>` ve telefon, e-posta,
 * kısaltma Arapça panelde de soldan sağa yazılmalı (AC-73). Kontrolü çağıran verir; bu bileşen
 * yalnız etiket, ipucu ve hata bağını kurar — `ContentEditPage`'in yerel alanıyla aynı desen.
 */
export function DealershipField({
  id,
  label,
  hint,
  error,
  children,
}: {
  readonly id: string
  readonly label: string
  readonly hint?: string
  readonly error: string | undefined
  readonly children: (props: ControlProps) => ReactNode
}) {
  const { locale, t } = useLocale()
  const describedBy = [
    hint === undefined ? null : `${id}-hint`,
    error === undefined ? null : `${id}-error`,
  ]
    .filter((value) => value !== null)
    .join(' ')

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      {children({
        id,
        'aria-invalid': error === undefined ? undefined : true,
        'aria-describedby': describedBy.length > 0 ? describedBy : undefined,
      })}
      {hint === undefined ? null : (
        <span className="admin-form__hint" id={`${id}-hint`}>
          {hint}
        </span>
      )}
      {error === undefined ? null : (
        <span className="admin-form__field-error" id={`${id}-error`} data-testid={`${id}-error`}>
          {t(fieldErrorKey(error, locale))}
        </span>
      )}
    </div>
  )
}
