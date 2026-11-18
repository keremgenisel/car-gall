import { Fragment, type ReactNode } from 'react'
import { parseBody, type Block, type Inline } from './bodyMarkup'

/**
 * `0017` R16, R17 · D26 — gövdeyi arayüz öğelerine çizer. Metin YALNIZ React'in metin
 * düğümü olarak basılır; `dangerouslySetInnerHTML` ve `innerHTML` bu dosyada yoktur ve
 * olmamalıdır (AC-73). Sayfanın `h1`'i başlık alanıdır; gövde `h1` üretmez (AC-64).
 */
export function ContentBody({
  source,
  testId,
}: {
  readonly source: string
  readonly testId?: string
}) {
  return (
    <div className="content-body" data-testid={testId}>
      {parseBody(source).map((block, index) => (
        <BlockView key={index} block={block} />
      ))}
    </div>
  )
}

function BlockView({ block }: { readonly block: Block }) {
  switch (block.kind) {
    case 'heading':
      return block.level === 2 ? (
        <h2>{inlines(block.children)}</h2>
      ) : (
        <h3>{inlines(block.children)}</h3>
      )
    case 'list': {
      const items = block.items.map((item, index) => <li key={index}>{inlines(item)}</li>)
      return block.ordered ? <ol>{items}</ol> : <ul>{items}</ul>
    }
    default:
      return <p>{inlines(block.children)}</p>
  }
}

function inlines(nodes: readonly Inline[]): ReactNode {
  return nodes.map((node, index) => <Fragment key={index}>{inline(node)}</Fragment>)
}

function inline(node: Inline): ReactNode {
  switch (node.kind) {
    case 'text':
      return node.text
    case 'strong':
      return <strong>{inlines(node.children)}</strong>
    case 'em':
      return <em>{inlines(node.children)}</em>
    case 'link':
      // R17 — dış bağlantı açılan sayfaya erişim vermez ve yönlendireni sızdırmaz (AC-72).
      return (
        <a href={node.href} rel={node.external ? 'noopener noreferrer' : undefined}>
          {inlines(node.children)}
        </a>
      )
  }
}
