import { useEffect, useRef, type KeyboardEvent, type ReactNode } from 'react'

interface Props {
  /** Görünümün açık olup olmadığı; kaynak durum çağırandadır. */
  readonly open: boolean
  /** Esc, kapatma düğmesi ya da tarayıcının kapatması — hepsi buradan geçer. */
  readonly onClose: () => void
  /** Erişilebilir ad; çağıran çeviri kataloğundan verir (§ 5). */
  readonly label: string
  readonly className?: string
  readonly testId?: string
  readonly children: ReactNode
}

/**
 * Modal görünüm — tarayıcının yerel `<dialog>` öğesiyle (`0016b` D28).
 *
 * `showModal()` üç şeyi tarayıcıdan getirir: arka plan **etkileşimsiz** ve erişilebilirlik
 * ağacından gizli (`inert`), **Esc** `cancel` → `close` olayını üretir, görünüm üst
 * katmanda (z-index yarışı yok). Burada AÇIKÇA yapılan iki şey var:
 *
 * - **Odak döngüsü.** Yerel modal, son öğeden sonra Tab'ı tarayıcının kendi arayüzüne
 *   bırakabilir; o an `document.activeElement` gövdedir. Görünüm içinde döngü bu yüzden
 *   elle kuruluyor (`0016b` AC-50; `identity`'nin onay kutusunun deseni).
 * - **Odağın dönüşü.** Kapanınca odak görünümü AÇAN öğeye döner (AC-52). Tarayıcıların
 *   bir kısmı bunu kendisi yapıyor, bir kısmı yapmıyor; kural tek yerde yaşıyor.
 *
 * İçerik yalnız açıkken basılır: kapalı bir görünümün `large` görseli indirilmez.
 */
export function Dialog({ open, onClose, label, className, testId, children }: Props) {
  const dialog = useRef<HTMLDialogElement>(null)
  const opener = useRef<Element | null>(null)

  // Dinleyici BİR KEZ bağlanır ve çağıranın en son `onClose`'unu ref'ten okur. Her render'da
  // yeniden bağlansaydı, `close()`'un aynı commit'te ürettiği olay eski dinleyici sökülmüş,
  // yenisi henüz bağlanmamışken gelebilirdi ve odak açan öğeye dönmezdi.
  const latestOnClose = useRef(onClose)

  useEffect(() => {
    latestOnClose.current = onClose
  }, [onClose])

  useEffect(() => {
    const element = dialog.current

    if (element === null) {
      return
    }

    if (open && !element.open) {
      opener.current = document.activeElement
      element.showModal()
    } else if (!open && element.open) {
      element.close()
    }
  }, [open])

  useEffect(() => {
    const element = dialog.current

    if (element === null) {
      return
    }

    function onNativeClose() {
      const target = opener.current
      opener.current = null

      if (target instanceof HTMLElement) {
        target.focus({ preventScroll: true })
      }

      latestOnClose.current()
    }

    element.addEventListener('close', onNativeClose)

    return () => element.removeEventListener('close', onNativeClose)
  }, [])

  function onKeyDown(event: KeyboardEvent<HTMLDialogElement>) {
    if (event.key !== 'Tab') {
      return
    }

    const focusable = focusableIn(dialog.current)

    if (focusable.length === 0) {
      event.preventDefault()
      return
    }

    const first = focusable[0]!
    const last = focusable[focusable.length - 1]!

    if (event.shiftKey && document.activeElement === first) {
      event.preventDefault()
      last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
      event.preventDefault()
      first.focus()
    }
  }

  return (
    <dialog
      ref={dialog}
      className={['ui-dialog', className].filter(Boolean).join(' ')}
      aria-label={label}
      data-testid={testId}
      onKeyDown={onKeyDown}
    >
      {open ? children : null}
    </dialog>
  )
}

const FOCUSABLE = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'

function focusableIn(root: HTMLElement | null): readonly HTMLElement[] {
  if (root === null) {
    return []
  }

  return [...root.querySelectorAll<HTMLElement>(FOCUSABLE)].filter(
    (element) => !element.hasAttribute('disabled'),
  )
}
