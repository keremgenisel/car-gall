import { useRef, useState, type KeyboardEvent, type TouchEvent } from 'react'
import { useLocale } from '../../../app/localeContext'
import type { ShowcaseImage, ShowcaseImageVariant } from '../../../shared/api/types'
import { directionOf } from '../../../shared/lib/locale'
import { Dialog } from '../../../shared/ui'

interface Props {
  readonly images: readonly ShowcaseImage[]
  readonly modelName: string
  readonly modelYear: number
  /** Rezerve rozeti gibi görselin üstüne oturan öğeler; galeri yoksa çağıran kendi yerine koyar. */
  readonly overlay?: React.ReactNode
}

/** Bir kaydırmanın "gezinme" sayılması için parmağın yatayda alması gereken en kısa yol (CSS pikseli). */
const SWIPE_MIN = 40

/**
 * `0016b` R1–R7 — ilan detayının galerisi.
 *
 * - **Tek seçili görsel** (D35): satır içi galeri ve büyütülmüş görünüm AYNI sırayı
 *   paylaşır; büyütmede gezinip kapatınca satır içi galeri kalınan görseldedir (AC-54).
 *   Sıra adrese ve geçmişe yazılmaz (R4, AC-53, AC-55).
 * - **Uçlarda dönülmez** (D16). Uçtaki düğme `aria-disabled` olur, `disabled` değil:
 *   yerel `disabled`, odaktaki düğmeyi odaklanamaz yapar ve odak gövdeye düşerdi (AC-42).
 * - **Yön okuma yönüdür** (R3, D33): Arapça'da ← tuşu ve parmağın soldan sağa kayması
 *   *sonraki*dir.
 * - **Yalnız seçili görsel basılır.** Etkileşimden önce galeriden tek bir istek çıkar
 *   (AC-64); 4:3 çerçeve sabit olduğu için görsel değişince sayfa kaymaz (AC-62).
 *
 * Sıfır görselde galeri hiç basılmaz (D15) — bu kararı çağıran verir.
 */
export function Gallery({ images, modelName, modelYear, overlay }: Props) {
  const { locale, t } = useLocale()
  const [index, setIndex] = useState(0)
  const [enlarged, setEnlarged] = useState(false)
  const [broken, setBroken] = useState<ReadonlySet<number>>(() => new Set())

  const total = images.length
  const many = total > 1
  const rtl = directionOf(locale) === 'rtl'
  const image = images[Math.min(index, total - 1)]!

  function go(delta: number) {
    setIndex((current) => Math.min(Math.max(current + delta, 0), total - 1))
  }

  const alt = t('detail.gallery.alt', {
    model: modelName,
    year: modelYear,
    index: index + 1,
    total,
  })

  // İki AYRI dinleyici kümesi: satır içi galeri ve büyütülmüş görünüm kaydırmanın başlangıç
  // noktasını paylaşmaz. Görünüm `.gallery`'nin DIŞINDA basılır — içinde basıldığında
  // görünümdeki her tuş `.gallery`'ye de kabarcıklanıyor ve iki adım ilerliyordu
  // (triyaj BULGU-1; AC-47 uçta ölçüldüğü için kırpılan çift adım görünmüyordu).
  const inlineNavigation = useNavigation(many, rtl, go)
  const lightboxNavigation = useNavigation(many, rtl, go)

  return (
    <>
      <div className="gallery" data-testid="gallery" {...inlineNavigation}>
        <div className="gallery__frame">
          <button
            type="button"
            className="gallery__enlarge"
            aria-label={t('detail.gallery.enlarge')}
            data-testid="gallery-enlarge"
            onClick={() => setEnlarged(true)}
          >
            {broken.has(index) ? (
              <Placeholder label={alt} text={t('detail.gallery.unavailable')} />
            ) : (
              <img
                /* Görsel değişince öğe YENİDEN kurulur: eski görsel yeni adresin
                 yüklenmesini beklerken çerçevede kalmaz. */
                key={index}
                className="gallery__image"
                src={image.display.url}
                srcSet={srcSet(image)}
                sizes="(min-width: 1232px) 1200px, 100vw"
                width={image.display.width}
                height={image.display.height}
                alt={alt}
                /* İlk görsel LCP adayıdır (Tuzak 4); diğerleri yalnız gezinilince istenir. */
                loading={index === 0 ? undefined : 'lazy'}
                fetchPriority={index === 0 ? 'high' : undefined}
                data-testid="gallery-image"
                onError={() => setBroken((current) => new Set(current).add(index))}
              />
            )}
          </button>

          {overlay}

          {many ? (
            <Controls index={index} total={total} onPrevious={() => go(-1)} onNext={() => go(1)} />
          ) : null}
        </div>

        {many ? <Counter index={index} total={total} testId="gallery-counter" /> : null}
      </div>

      <Dialog
        open={enlarged}
        onClose={() => setEnlarged(false)}
        label={t('detail.gallery.dialog')}
        className="lightbox"
        testId="lightbox"
      >
        <div className="lightbox__content" {...lightboxNavigation}>
          <button
            type="button"
            className="lightbox__close"
            onClick={() => setEnlarged(false)}
            data-testid="lightbox-close"
          >
            {t('detail.gallery.close')}
          </button>

          <div className="lightbox__frame">
            {broken.has(index) ? (
              <Placeholder label={alt} text={t('detail.gallery.unavailable')} />
            ) : (
              <img
                key={index}
                className="lightbox__image"
                src={image.large.url}
                width={image.large.width}
                height={image.large.height}
                alt={alt}
                data-testid="lightbox-image"
                onError={() => setBroken((current) => new Set(current).add(index))}
              />
            )}

            {many ? (
              <Controls
                index={index}
                total={total}
                onPrevious={() => go(-1)}
                onNext={() => go(1)}
              />
            ) : null}
          </div>

          {many ? <Counter index={index} total={total} testId="lightbox-counter" /> : null}
        </div>
      </Dialog>
    </>
  )
}

/**
 * Ok tuşları ve kaydırma; satır içi galeri ve büyütülmüş görünüm AYNI kuralı kullanır
 * (AC-47). Fiziksel tuş, okuma yönüne göre "ileri" ya da "geri" olur.
 */
function useNavigation(enabled: boolean, rtl: boolean, go: (delta: number) => void) {
  const start = useRef<number | null>(null)

  return {
    onKeyDown(event: KeyboardEvent) {
      if (!enabled || (event.key !== 'ArrowRight' && event.key !== 'ArrowLeft')) {
        return
      }

      event.preventDefault()
      const forward = event.key === (rtl ? 'ArrowLeft' : 'ArrowRight')
      go(forward ? 1 : -1)
    },
    onTouchStart(event: TouchEvent) {
      start.current = event.touches[0]?.clientX ?? null
    },
    onTouchEnd(event: TouchEvent) {
      const endX = event.changedTouches[0]?.clientX

      const startX = start.current
      start.current = null

      if (!enabled || startX === null || endX === undefined) {
        return
      }

      const moved = endX - startX

      if (Math.abs(moved) < SWIPE_MIN) {
        return
      }

      // D33 — parmağın HAREKETİ: TR/EN'de sağdan sola (negatif), AR'da soldan sağa (pozitif)
      // kaydırma sonraki görseldir; içerik okuma yönünde ilerler.
      const forward = Math.sign(moved) === (rtl ? 1 : -1)
      go(forward ? 1 : -1)
    },
  }
}

function Controls({
  index,
  total,
  onPrevious,
  onNext,
}: {
  readonly index: number
  readonly total: number
  readonly onPrevious: () => void
  readonly onNext: () => void
}) {
  const { t } = useLocale()
  const atStart = index === 0
  const atEnd = index === total - 1

  return (
    <>
      <button
        type="button"
        className="gallery__nav gallery__nav--previous"
        aria-label={t('detail.gallery.previous')}
        aria-disabled={atStart || undefined}
        data-testid="gallery-previous"
        onClick={() => (atStart ? undefined : onPrevious())}
      >
        <Chevron />
      </button>
      <button
        type="button"
        className="gallery__nav gallery__nav--next"
        aria-label={t('detail.gallery.next')}
        aria-disabled={atEnd || undefined}
        data-testid="gallery-next"
        onClick={() => (atEnd ? undefined : onNext())}
      >
        <Chevron />
      </button>
    </>
  )
}

function Counter({
  index,
  total,
  testId,
}: {
  readonly index: number
  readonly total: number
  readonly testId: string
}) {
  const { t } = useLocale()

  return (
    <p className="gallery__counter" aria-live="polite" data-testid={testId}>
      {t('detail.gallery.counter', { index: index + 1, total })}
    </p>
  )
}

/** Yüklenemeyen görselin yeri: tarayıcının kırık görsel simgesi yerine (AC-69). */
function Placeholder({ label, text }: { readonly label: string; readonly text: string }) {
  return (
    <span
      className="gallery__placeholder"
      role="img"
      aria-label={label}
      data-testid="gallery-placeholder"
    >
      <span aria-hidden="true">{text}</span>
    </span>
  )
}

/** Yön işareti; "önceki"nin oku CSS'te okuma yönüne göre çevrilir. */
function Chevron() {
  return (
    <svg className="gallery__chevron" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
      <path d="M15 5 8 12l7 7" />
    </svg>
  )
}

/**
 * `0016b` D18/AC-60 — üç türev GERÇEK genişlikleriyle. Küçük kaynakta türevlerin
 * genişliği aynı olabilir; aynı tanımlayıcı iki kez yazılmaz.
 */
function srcSet(image: ShowcaseImage): string {
  const seen = new Set<number>()
  const variants: ShowcaseImageVariant[] = [image.thumbnail, image.display, image.large]

  return variants
    .filter((variant) => (seen.has(variant.width) ? false : (seen.add(variant.width), true)))
    .map((variant) => `${variant.url} ${variant.width}w`)
    .join(', ')
}
