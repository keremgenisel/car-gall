import type { ReactNode } from 'react'
import { TopBar } from './TopBar'

/** Üst bar + içerik alanı. İçerik `<main>`'dir; sayfanın tek `<h1>`'i oraya aittir (AC-1). */
export function AppShell({
  children,
  footer = null,
}: {
  readonly children: ReactNode
  /** 0014 — altlık `<main>`'in DIŞINDADIR; yalnız ana sayfa verir. */
  readonly footer?: ReactNode
}) {
  return (
    <>
      <TopBar />
      <main className="app-content">{children}</main>
      {footer}
    </>
  )
}
