import { useLocale } from '../../../app/localeContext'

interface Props {
  readonly id: string
  readonly label: string
  readonly hint?: string
  readonly messageKey?: string
  readonly value: string
  /**
   * Kanonik ad Latin ve ASCII'dir (C3); Arapça panelde de soldan sağa yazılır (AC-58). Görünen
   * ad dilin kendi yönündedir.
   */
  readonly dir: 'ltr' | 'auto'
  readonly lang?: string
  readonly onChange: (value: string) => void
}

/**
 * `0021` — marka ve model formlarının etiketli metin alanı. Hata KODU sunucudan gelir ve
 * çağıran onu katalog anahtarına çevirir; alan `aria-invalid` taşır ve hata metni alanın
 * altındadır.
 *
 * <b>`shared/ui/Field` neden yetmedi:</b> o alan metin tipinde yönü `auto` yapıyor; kanonik ad
 * Arapça panelde de soldan sağa yazılmak zorunda (AC-58). `DealershipField` aynı gerekçeyle
 * kendi feature'ında duruyor ve feature'lar birbirinin iç dosyasını içe aktaramaz.
 */
export function ReferenceField({ id, label, hint, messageKey, value, dir, lang, onChange }: Props) {
  const { t } = useLocale()
  const describedBy = [
    hint === undefined ? null : `${id}-hint`,
    messageKey === undefined ? null : `${id}-error`,
  ]
    .filter((item) => item !== null)
    .join(' ')

  return (
    <div className="admin-form__field">
      <label className="admin-form__label" htmlFor={id}>
        {label}
      </label>
      <input
        id={id}
        className="admin-form__control"
        dir={dir}
        lang={lang}
        value={value}
        aria-invalid={messageKey === undefined ? undefined : true}
        aria-describedby={describedBy.length > 0 ? describedBy : undefined}
        onChange={(event) => onChange(event.target.value)}
      />
      {hint === undefined ? null : (
        <span className="admin-form__hint" id={`${id}-hint`}>
          {hint}
        </span>
      )}
      {messageKey === undefined ? null : (
        <span className="admin-form__field-error" id={`${id}-error`} data-testid={`${id}-error`}>
          {t(messageKey)}
        </span>
      )}
    </div>
  )
}
