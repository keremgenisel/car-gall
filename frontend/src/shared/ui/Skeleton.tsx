interface Props {
  /** `media`: gerçek görselin en-boy oranını tutar. `line`: bir metin satırının yerini tutar. */
  readonly shape: 'media' | 'line'
}

/**
 * docs/frontend.md § 3 — yer tutucunun işi boşluğu doldurmak değil, DOĞRU boşluğu
 * tutmaktır. İskelet gerçek içeriğin kutu modelini taklit eder; yanlış yükseklik
 * veri gelince düzeni kaydırır (CLS).
 */
export function Skeleton({ shape }: Props) {
  return <div className={`ui-skeleton ui-skeleton--${shape}`} aria-hidden="true" />
}
