/// <reference types="node" />
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { createElement } from 'react'
import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ContentBody } from './ContentBody'

/**
 * Dosya `.ts`: enjeksiyon girdileri (`<script>alert(1)</script>`) TEST VERİSİDİR ve gömülü
 * metin kapısı yalnız `.tsx` bileşenlerdeki arayüz metnini tarar; JSX yerine `createElement`.
 *
 * `0017` R16, R17 · D26 — gövde çizicisi. Ayrıştırıcı HTML üretmez; çizici yalnız metin
 * düğümü ve izinli öğe türleri basar. Enjeksiyon kriterleri ÇİZİLEN DOM'da ölçülür.
 */
afterEach(() => cleanup())

function govde(source: string) {
  render(createElement(ContentBody, { source, testId: 'govde' }))
  return screen.getByTestId('govde')
}

describe('0017 — gövde: desteklenen öğeler', () => {
  it('0017/AC-63: örnek gövde h2, h3, iki paragraf, strong, em, iki maddeli ul ve ol ve https bağlantısı üretir', () => {
    const kok = govde(
      [
        '## Garanti',
        '### Kapsam',
        'Birinci paragraf.',
        '',
        'İkinci paragraf **kalın** ve *italik*.',
        '',
        '- motor',
        '- şanzıman',
        '',
        '1. başvur',
        '2. bekle',
        '',
        '[Bize yazın](https://example.com)',
      ].join('\n'),
    )

    expect(kok.querySelector('h2')?.textContent).toBe('Garanti')
    expect(kok.querySelector('h3')?.textContent).toBe('Kapsam')
    expect(kok.querySelector('strong')?.textContent).toBe('kalın')
    expect(kok.querySelector('em')?.textContent).toBe('italik')
    expect(kok.querySelectorAll('ul > li')).toHaveLength(2)
    expect(kok.querySelectorAll('ol > li')).toHaveLength(2)
    expect(kok.querySelector('a')?.getAttribute('href')).toBe('https://example.com')
    // "Birinci paragraf." ve "İkinci paragraf…" + bağlantının paragrafı.
    expect(Array.from(kok.querySelectorAll('p')).map((p) => p.textContent)).toEqual([
      'Birinci paragraf.',
      'İkinci paragraf kalın ve italik.',
      'Bize yazın',
    ])
  })

  it('0017/AC-64 (gövde yarısı): # Büyük başlık h1 üretmez; düz metin olarak görünür', () => {
    const kok = govde('# Büyük başlık')
    expect(kok.querySelector('h1')).toBeNull()
    expect(kok.textContent).toBe('# Büyük başlık')
  })

  it('0017/AC-65: alıntı, kod, görsel ve yatay çizgi düz metin olarak basılır', () => {
    const kok = govde(
      ['> alıntı', '', '`kod`', '', '![resim](https://example.com/a.png)', '', '---'].join('\n'),
    )

    expect(kok.querySelector('blockquote')).toBeNull()
    expect(kok.querySelector('code')).toBeNull()
    expect(kok.querySelector('img')).toBeNull()
    expect(kok.querySelector('hr')).toBeNull()
    expect(kok.textContent).toContain('> alıntı')
    expect(kok.textContent).toContain('`kod`')
    expect(kok.textContent).toContain('![resim](https://example.com/a.png)')
    expect(kok.textContent).toContain('---')
  })
})

describe('0017 — gövde: enjeksiyon', () => {
  it('0017/AC-66: <script>alert(1)</script> metin olarak görünür; script öğesi yok, alert çağrılmaz', () => {
    const alert = vi.spyOn(window, 'alert').mockImplementation(() => {})
    const kok = govde('<script>alert(1)</script>')

    expect(kok.textContent).toBe('<script>alert(1)</script>')
    expect(kok.querySelector('script')).toBeNull()
    expect(alert).not.toHaveBeenCalled()
    alert.mockRestore()
  })

  it('0017/AC-67: <img src=x onerror=alert(1)> metin olarak görünür; img öğesi yok', () => {
    const kok = govde('<img src=x onerror=alert(1)>')

    expect(kok.textContent).toBe('<img src=x onerror=alert(1)>')
    expect(kok.querySelector('img')).toBeNull()
  })

  it('0017/AC-68 (çizici yarısı): <b>kalın</b> "<b>kalın</b>" metni olarak basılır; b öğesi yok', () => {
    const kok = govde('<b>kalın</b>')
    expect(kok.textContent).toBe('<b>kalın</b>')
    expect(kok.querySelector('b')).toBeNull()
  })

  it.each([
    '[tıkla](javascript:alert(1))',
    '[tıkla](JavaScript:alert(1))',
    '[tıkla](data:text/html,x)',
    '[tıkla](vbscript:x)',
    '[tıkla]( javascript:alert(1))',
    '[tıkla](<javascript:alert(1)>)',
  ])('0017/AC-69: kayıtlı veride bulunsa bile %s bağlantı olarak basılmaz', (kaynak) => {
    const kok = govde(kaynak)

    expect(kok.querySelector('a')).toBeNull()
    for (const oge of Array.from(kok.querySelectorAll('[href]'))) {
      expect(oge.getAttribute('href')!.toLowerCase()).not.toMatch(/^\s*(javascript|data|vbscript):/)
    }
  })

  it('0017/AC-71 (çizici yarısı): http, https, mailto, tel ve göreli bağlantılar a öğesi olarak basılır', () => {
    const kok = govde(
      [
        '[a](http://example.com)',
        '[b](https://example.com)',
        '[c](mailto:info@example.com)',
        '[d](tel:+905551112233)',
        '[e](/tr/cars)',
      ].join('\n\n'),
    )

    expect(Array.from(kok.querySelectorAll('a')).map((a) => a.getAttribute('href'))).toEqual([
      'http://example.com',
      'https://example.com',
      'mailto:info@example.com',
      'tel:+905551112233',
      '/tr/cars',
    ])
  })

  it('0017/AC-72, triyaj BULGU-2: http, https ve şemasız başka köke giden (//, \\\\) bağlantılar rel="noopener noreferrer" taşır', () => {
    const kok = govde(
      '[a](http://example.com) [b](https://example.com) [c](//evil.example/x) [d](\\\\evil.example/x)',
    )
    expect(kok.querySelectorAll('a')).toHaveLength(4)

    for (const a of Array.from(kok.querySelectorAll('a'))) {
      expect(a.getAttribute('rel')).toBe('noopener noreferrer')
    }
  })

  it('triyaj BULGU-2 (sınır): tek / ile başlayan göreli bağlantı aynı köktedir ve rel taşımaz', () => {
    expect(govde('[sss](/tr/sss)').querySelector('a')?.getAttribute('rel')).toBeNull()
  })

  it('0017/AC-73: gövdeyi basan ziyaretçi kodunda dangerouslySetInnerHTML ve innerHTML geçmez', () => {
    for (const dosya of ['ContentBody.tsx', 'bodyMarkup.ts', 'ContentPage.tsx']) {
      const kaynak = readFileSync(join(process.cwd(), 'src/features/content', dosya), 'utf8')
        .split('\n')
        .filter((satir) => !satir.trim().startsWith('*') && !satir.trim().startsWith('//'))
        .join('\n')

      expect(kaynak).not.toMatch(/dangerouslySetInnerHTML|innerHTML/)
    }
  })

  it('0017/AC-74 (çizici yarısı): SSS cevabı **24 ay** garanti strong öğesiyle basılır', () => {
    const kok = govde('**24 ay** garanti')
    expect(kok.querySelector('strong')?.textContent).toBe('24 ay')
  })
})
