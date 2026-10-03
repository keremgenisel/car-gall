import { useState } from 'react'
import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { LocaleProvider } from '../../app/LocaleProvider'
import { createTranslator } from '../../shared/i18n/i18n'
import { Button } from '../../shared/ui'
import { ConfirmDialog } from './ConfirmDialog'

const t = createTranslator('tr')

afterEach(cleanup)

/**
 * Kutu, ekrandan BAĞIMSIZ sınanıyor: odak davranışı listeye, isteğe veya role bağlı
 * değil. Ekranla birlikte sınasaydık her odak iddiası bir sahte istemciye ve bir listeye
 * bağımlı olurdu.
 *
 * **jsdom'un sınırı açıkça çiziliyor.** jsdom Tab tuşuna yerel odak hareketi UYGULAMAZ;
 * yani "Tab sırayla ilerler" burada kanıtlanamaz — o tarayıcının işi. Kanıtlanabilen ve
 * bu bileşene ait olan şey SINIRDAKİ davranıştır: son öğedeyken Tab başa döner, ilk
 * öğedeyken Shift+Tab sona döner. Odağın kutudan kaçmamasını sağlayan tam olarak bu kod.
 */
function ekran(onCancel = vi.fn()) {
  render(
    <LocaleProvider locale="tr">
      <ConfirmDialog
        titleKey="staff.confirm.deactivate.title"
        onCancel={onCancel}
        testId="confirm"
        footer={
          <Button data-testid="confirm-submit">{t('staff.confirm.deactivate.submit')}</Button>
        }
      >
        <p>{t('staff.confirm.deactivate.body', { name: 'Ayşe' })}</p>
      </ConfirmDialog>
    </LocaleProvider>,
  )

  return { onCancel }
}

describe('0008/AC-28: Onay kutusu açıldığında odak kutunun içine girer', () => {
  it('odak kutunun içindeki ilk öğededir', () => {
    ekran()

    const kutu = screen.getByTestId('confirm')

    expect(kutu.contains(document.activeElement)).toBe(true)
    expect(document.activeElement).toBe(screen.getByTestId('confirm-cancel'))
  })
})

describe('0008/AC-29: Onay kutusu Esc tuşuyla kapanır ve kapandığında odak onu açan düğmeye döner', () => {
  it('Esc iptali tetikler', () => {
    const { onCancel } = ekran()

    fireEvent.keyDown(screen.getByTestId('confirm'), { key: 'Escape' })

    expect(onCancel).toHaveBeenCalledTimes(1)
  })

  it('kutu kaldırıldığında odak tetikleyen düğmeye döner', () => {
    function Durum() {
      const [acik, setAcik] = useState(false)

      return (
        <LocaleProvider locale="tr">
          <button type="button" data-testid="tetikleyici" onClick={() => setAcik(true)}>
            {t('staff.action.deactivate')}
          </button>
          {acik ? (
            <ConfirmDialog
              titleKey="staff.confirm.deactivate.title"
              onCancel={() => setAcik(false)}
              testId="confirm"
              footer={
                <Button data-testid="confirm-submit">{t('staff.confirm.deactivate.submit')}</Button>
              }
            >
              <p>{t('staff.confirm.deactivate.body', { name: 'Ayşe' })}</p>
            </ConfirmDialog>
          ) : null}
        </LocaleProvider>
      )
    }

    render(<Durum />)

    const tetikleyici = screen.getByTestId('tetikleyici')
    tetikleyici.focus()
    fireEvent.click(tetikleyici)

    expect(screen.queryByTestId('confirm')).not.toBeNull()
    expect(document.activeElement).not.toBe(tetikleyici)

    fireEvent.keyDown(screen.getByTestId('confirm'), { key: 'Escape' })

    expect(screen.queryByTestId('confirm')).toBeNull()
    expect(document.activeElement).toBe(tetikleyici)
  })
})

describe('0008/AC-30: Onay kutusundaki her etkileşimli öğeye Tab ile ulaşılır ve Enter/Space ile çalıştırılır; odak halkası görünürdür', () => {
  it('odak kutunun dışına KAÇMAZ: son öğeden Tab başa döner', () => {
    ekran()

    const kutu = screen.getByTestId('confirm')
    const iptal = screen.getByTestId('confirm-cancel')
    const onayla = screen.getByTestId('confirm-submit')

    onayla.focus()
    fireEvent.keyDown(kutu, { key: 'Tab' })

    expect(document.activeElement).toBe(iptal)
  })

  it('ilk öğeden Shift+Tab sona döner', () => {
    ekran()

    const kutu = screen.getByTestId('confirm')
    const iptal = screen.getByTestId('confirm-cancel')
    const onayla = screen.getByTestId('confirm-submit')

    iptal.focus()
    fireEvent.keyDown(kutu, { key: 'Tab', shiftKey: true })

    expect(document.activeElement).toBe(onayla)
  })

  /**
   * Enter/Space ile çalıştırma yerel `<button>` davranışıdır; jsdom onu simüle etmez ve
   * elle `click` tetiklemek jsdom'u sınamak olurdu. Bu yüzden kanıtlanan şey ÖNKOŞUL:
   * kutudaki her etkileşimli öğe gerçek bir `<button>`. `docs/frontend.md` § 7 zaten bunu
   * istiyor — "tıklanabilir `<div>` yazılmaz". Gerçek tuş vuruşu e2e'de koşuyor.
   */
  it('kutudaki her etkileşimli öğe gerçek bir düğmedir', () => {
    ekran()

    const kutu = screen.getByTestId('confirm')
    const etkilesimli = [...kutu.querySelectorAll('button, [href], input, select, textarea')]

    expect(etkilesimli.length).toBeGreaterThan(0)

    for (const element of etkilesimli) {
      expect(element.tagName, element.outerHTML).toBe('BUTTON')
    }
  })
})
