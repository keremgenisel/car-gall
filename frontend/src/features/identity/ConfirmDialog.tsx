import { useEffect, useRef } from 'react'
import { useLocale } from '../../app/localeContext'
import { Button } from '../../shared/ui'

/**
 * Onay kutusu (0008 R10, D3).
 *
 * **`shared/ui`'ye girmez.** `docs/frontend.md` § 4.2 ortaklığın ölçütünü "kaç ekran" diye
 * koyuyor, "kaç çağrı" diye değil: bu kutu üç eylemde kullanılıyor ama tek ekranda. Erken
 * terfi, ortak bileşeni tek bir ekranın ihtiyaçlarına göre şekillendirir ve ikinci kullanıcı
 * geldiğinde API'si yanlış çıkar. *Tetikleyici:* ikinci bir ekran onay isterse terfi önerilir.
 *
 * `<dialog>` yerine elle yazılmış bir katman: `showModal()` jsdom'da gerçek odak tuzağı
 * kurmuyor ve davranışın testi mümkün olmuyor. Odak yönetimi burada AÇIKÇA yapılıyor ve
 * AC-28/29/30 onu ölçüyor.
 */
export function ConfirmDialog({
  titleKey,
  onCancel,
  children,
  footer,
  testId,
}: {
  readonly titleKey: string
  readonly onCancel: () => void
  readonly children: React.ReactNode
  readonly footer: React.ReactNode
  readonly testId: string
}) {
  const { t } = useLocale()
  const panel = useRef<HTMLDivElement>(null)

  // Kutu kapandığında odak onu AÇAN düğmeye dönmeli (AC-29). Tetikleyiciyi kutunun
  // kendisi hatırlıyor: çağıran her yerde aynı şeyi tekrarlasaydı, bir yerde unutulurdu.
  const trigger = useRef<Element | null>(null)

  useEffect(() => {
    trigger.current = document.activeElement

    // AC-28 — odak kutunun İÇİNE girer. İlk odaklanabilir öğe seçiliyor; panelin kendisi
    // odaklansaydı Tab sırası kutunun dışından başlardı.
    ilkOdaklanabilir(panel.current)?.focus()

    return () => {
      if (trigger.current instanceof HTMLElement) {
        trigger.current.focus()
      }
    }
  }, [])

  /**
   * Odak tuzağı (docs/frontend.md § 7 — "odak tuzağı bırakılmaz" kuralının tersi değil:
   * orada kastedilen, kullanıcının KAÇAMADIĞI tuzaktır. Modal bir kutuda odağın içeride
   * dönmesi beklenen davranıştır ve Esc her zaman çıkış verir).
   */
  function onKeyDown(event: React.KeyboardEvent) {
    if (event.key === 'Escape') {
      event.stopPropagation()
      onCancel()
      return
    }

    if (event.key !== 'Tab') {
      return
    }

    const odaklanabilir = odaklanabilirler(panel.current)

    if (odaklanabilir.length === 0) {
      return
    }

    const ilk = odaklanabilir[0]!
    const son = odaklanabilir[odaklanabilir.length - 1]!

    if (event.shiftKey && document.activeElement === ilk) {
      event.preventDefault()
      son.focus()
    } else if (!event.shiftKey && document.activeElement === son) {
      event.preventDefault()
      ilk.focus()
    }
  }

  return (
    <div className="admin-dialog__backdrop">
      {/* Klavye olayları panelde dinleniyor; kutu içindeki her öğe onların yolundadır. */}
      <div
        ref={panel}
        className="admin-dialog"
        role="dialog"
        aria-modal="true"
        aria-labelledby={`${testId}-title`}
        data-testid={testId}
        onKeyDown={onKeyDown}
      >
        <h2 className="admin-dialog__title" id={`${testId}-title`}>
          {t(titleKey)}
        </h2>

        <div className="admin-dialog__body">{children}</div>

        <div className="admin-dialog__actions">
          <Button variant="text" onClick={onCancel} data-testid={`${testId}-cancel`}>
            {t('staff.confirm.cancel')}
          </Button>
          {footer}
        </div>
      </div>
    </div>
  )
}

const ODAKLANABILIR = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'

function odaklanabilirler(root: HTMLElement | null): readonly HTMLElement[] {
  if (root === null) {
    return []
  }

  return [...root.querySelectorAll<HTMLElement>(ODAKLANABILIR)].filter(
    (element) => !element.hasAttribute('disabled'),
  )
}

function ilkOdaklanabilir(root: HTMLElement | null): HTMLElement | undefined {
  return odaklanabilirler(root)[0]
}
