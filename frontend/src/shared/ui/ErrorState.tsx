interface Props {
  readonly title: string
  readonly description: string
  readonly retryLabel: string
  readonly onRetry: () => void
  readonly testId?: string
}

/**
 * docs/frontend.md § 3.1 — kullanıcıya teknik detay sızmaz. Bileşen bilerek yalnız
 * ÇEVRİLMİŞ metin kabul eder: `errorCode`, `traceId` veya durum kodu alan bir prop'u
 * yoktur, dolayısıyla yanlışlıkla ekrana basılamaz.
 * `onRetry` zorunludur: eylem önermeyen hata ekranı yasak.
 */
export function ErrorState({ title, description, retryLabel, onRetry, testId }: Props) {
  return (
    <div className="ui-state" role="alert" data-testid={testId}>
      <h2 className="ui-state__title">{title}</h2>
      <p className="ui-state__body">{description}</p>
      <button type="button" className="ui-button ui-button--secondary" onClick={onRetry}>
        {retryLabel}
      </button>
    </div>
  )
}
