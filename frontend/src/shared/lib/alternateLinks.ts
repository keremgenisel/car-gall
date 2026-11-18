import { useSyncExternalStore } from 'react'

/**
 * `0017` R15 · D14 — bulunulan sayfanın DİĞER dillerdeki adresi. İçerik sayfası slug'ı dil
 * başına olduğu için dil değiştirici hedefi adresten türetemez; sayfa yanıtının taşıdığı
 * slug'ları buraya yazar, değiştirici buradan okur. Kayıt yokken (`null`) değiştirici
 * bugünkü davranışını korur (AC-62).
 *
 * Değer: dil kodu → adres. Listede olmayan dil "o dilde yayında değil" demektir ve
 * değiştirici o dilin ana sayfasına gider (AC-60).
 */
export type AlternateLinks = Readonly<Record<string, string>>

let current: AlternateLinks | null = null
const listeners = new Set<() => void>()

export function setAlternateLinks(links: AlternateLinks | null): void {
  current = links
  listeners.forEach((listener) => listener())
}

export function useAlternateLinks(): AlternateLinks | null {
  return useSyncExternalStore(
    (listener) => {
      listeners.add(listener)
      return () => listeners.delete(listener)
    },
    () => current,
  )
}
