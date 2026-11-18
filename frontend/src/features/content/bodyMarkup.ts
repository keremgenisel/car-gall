/**
 * `0017` R16, R17 · D19, D26 — gövdenin CommonMark ALT KÜMESİNİ bir sözdizimi ağacına
 * çevirir. Ağaç yalnız düz metin ve izinli öğe türleri taşır; çizici onu doğrudan arayüz
 * öğelerine çevirir ve hiçbir aşamada HTML dizgesi üretilmez (`ContentBody.tsx`). Ham HTML
 * bu yüzden YAPI GEREĞİ metindir: `<script>` bir `text` düğümünden başka bir şeye dönüşemez.
 *
 * Desteklenen: `##`/`###` başlık, paragraf, sırasız (`-`, `*`) ve sıralı (`1.`) liste,
 * `[metin](hedef)` bağlantı, `**kalın**`, `*italik*`. Gerisi (ör. `#`, `>`, `` ` ``, `![]`,
 * `---`) düz metin olarak kalır.
 */

export type Inline =
  | { readonly kind: 'text'; readonly text: string }
  | { readonly kind: 'strong'; readonly children: readonly Inline[] }
  | { readonly kind: 'em'; readonly children: readonly Inline[] }
  | {
      readonly kind: 'link'
      readonly href: string
      readonly external: boolean
      readonly children: readonly Inline[]
    }

export type Block =
  | { readonly kind: 'heading'; readonly level: 2 | 3; readonly children: readonly Inline[] }
  | { readonly kind: 'paragraph'; readonly children: readonly Inline[] }
  | {
      readonly kind: 'list'
      readonly ordered: boolean
      readonly items: readonly (readonly Inline[])[]
    }

const UNORDERED = /^[-*] +(.*)$/
const ORDERED = /^\d+\. +(.*)$/
const HEADING = /^(#{2,3}) +(.*)$/

/** Gövdeyi bloklara ayırır. */
export function parseBody(source: string): readonly Block[] {
  const blocks: Block[] = []
  const lines = source.replace(/\r\n?/g, '\n').split('\n')
  let index = 0

  while (index < lines.length) {
    const line = lines[index]!

    if (line.trim().length === 0) {
      index += 1
      continue
    }

    const heading = HEADING.exec(line)

    if (heading !== null) {
      blocks.push({
        kind: 'heading',
        level: heading[1]!.length === 2 ? 2 : 3,
        children: parseInline(heading[2]!),
      })
      index += 1
      continue
    }

    const listPattern = UNORDERED.test(line) ? UNORDERED : ORDERED.test(line) ? ORDERED : null

    if (listPattern !== null) {
      const items: Inline[][] = []

      while (index < lines.length && listPattern.test(lines[index]!)) {
        items.push(parseInline(listPattern.exec(lines[index]!)![1]!))
        index += 1
      }

      blocks.push({ kind: 'list', ordered: listPattern === ORDERED, items })
      continue
    }

    const paragraph: string[] = []

    while (
      index < lines.length &&
      lines[index]!.trim().length > 0 &&
      !HEADING.test(lines[index]!) &&
      !UNORDERED.test(lines[index]!) &&
      !ORDERED.test(lines[index]!)
    ) {
      paragraph.push(lines[index]!.trim())
      index += 1
    }

    blocks.push({ kind: 'paragraph', children: parseInline(paragraph.join(' ')) })
  }

  return blocks
}

const LINK = /\[([^\]]*)\]\(([^)]*)\)/
const STRONG = /\*\*(.+?)\*\*/
const EM = /\*(.+?)\*/

/** Satır içi öğeler; en erken başlayan eşleşme önce. */
export function parseInline(source: string): Inline[] {
  const result: Inline[] = []
  let rest = source

  while (rest.length > 0) {
    const candidates = [
      { kind: 'link' as const, match: LINK.exec(rest) },
      { kind: 'strong' as const, match: STRONG.exec(rest) },
      { kind: 'em' as const, match: EM.exec(rest) },
    ].filter((candidate) => candidate.match !== null)

    if (candidates.length === 0) {
      pushText(result, rest)
      break
    }

    // Aynı yerde başlıyorlarsa sıra: bağlantı, kalın, italik (`**` `*`'dan önce okunur).
    const first = candidates.reduce((best, candidate) =>
      candidate.match!.index < best.match!.index ? candidate : best,
    )
    const match = first.match!

    // `![metin](adres)` bir GÖRSEL sözdizimidir ve desteklenmez: düz metin kalır (AC-65).
    if (first.kind === 'link' && match.index > 0 && rest[match.index - 1] === '!') {
      pushText(result, rest.slice(0, match.index + match[0].length))
      rest = rest.slice(match.index + match[0].length)
      continue
    }

    pushText(result, rest.slice(0, match.index))

    if (first.kind === 'link') {
      const target = match[2]!.trim()

      if (isAllowedTarget(target)) {
        result.push({
          kind: 'link',
          href: target,
          // Triyaj BULGU-2 — `//kok` ve `\\kok` (tarayıcı http(s)'te `\` işaretini `/` sayar)
          // şemasız ama BAŞKA köke gider: R17'nin dış bağlantısıdır.
          external: /^https?:/i.test(target) || /^[/\\]{2}/.test(target),
          children: parseInline(match[1]!),
        })
      } else {
        // R17 — izinsiz şema bağlantı OLMAZ: kaynak metin olduğu gibi görünür (AC-69).
        pushText(result, match[0])
      }
    } else {
      result.push({ kind: first.kind, children: parseInline(match[1]!) })
    }

    rest = rest.slice(match.index + match[0].length)
  }

  return result
}

/**
 * R17 — izinli hedef: `http`, `https`, `mailto`, `tel` ya da şemasız (göreli) adres.
 * Sunucunun kuralıyla AYNI (`ContentRules.IsAllowedTarget`): boşluk, kontrol karakterleri ve
 * açılı ayraçlar atılarak bakılır; şema büyük-küçük harfe duyarsızdır. İçinde boşluk olan
 * hedef bağlantı sayılmaz — tarayıcının URL çözümleyicisi sekmeyi atar ve `java\tscript:`
 * bir şemaya dönüşebilirdi.
 */
export function isAllowedTarget(target: string): boolean {
  if (target.length === 0 || /\s/.test(target)) {
    return false
  }

  const clean = Array.from(target)
    .filter((char) => char > ' ' && char !== '\u007f' && char !== '<' && char !== '>')
    .join('')
  const scheme = /^([a-zA-Z][a-zA-Z0-9+.-]*):/.exec(clean)

  return scheme === null || ['http', 'https', 'mailto', 'tel'].includes(scheme[1]!.toLowerCase())
}

function pushText(result: Inline[], text: string) {
  if (text.length > 0) {
    result.push({ kind: 'text', text })
  }
}
