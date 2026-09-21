import type { ReactNode } from 'react'
import { TopBar } from './TopBar'

/** Üst bar + içerik alanı. İçerik `<main>`'dir; sayfanın tek `<h1>`'i oraya aittir (AC-1). */
export function AppShell({ children }: { readonly children: ReactNode }) {
  return (
    <>
      <TopBar />
      <main className="app-content">{children}</main>
    </>
  )
}
