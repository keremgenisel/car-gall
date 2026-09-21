import { useCallback, useEffect, useState } from 'react'
import {
  isCanonicalSearch,
  parseListState,
  toCanonicalSearch,
  type ListState,
} from './listUrlState'

/**
 * Durum URL'de yaşar. Her daraltma değişikliği geçmişe TEK kayıt ekler; daraltma
 * değiştiğinde yapılan sayfa sıfırlaması ayrı kayıt üretmez (R5, AC-19).
 *
 * Kanonik olmayan adres replaceState ile düzeltilir — bu düzeltme geçmişe yeni kayıt
 * EKLEMEZ, yoksa geri tuşu ziyaretçiyi düzeltilmemiş hâle geri fırlatırdı (C14, AC-14).
 */
export function useListUrlState(): [ListState, (next: ListState) => void] {
  const [state, setState] = useState<ListState>(() => parseListState(window.location.search))

  useEffect(() => {
    const onPopState = () => setState(parseListState(window.location.search))
    window.addEventListener('popstate', onPopState)
    return () => window.removeEventListener('popstate', onPopState)
  }, [])

  useEffect(() => {
    if (!isCanonicalSearch(window.location.search)) {
      const canonical = `${window.location.pathname}${toCanonicalSearch(parseListState(window.location.search))}`
      window.history.replaceState(null, '', canonical)
    }
  }, [])

  const push = useCallback((next: ListState) => {
    const url = `${window.location.pathname}${toCanonicalSearch(next)}`
    window.history.pushState(null, '', url)
    setState(next)
  }, [])

  return [state, push]
}

/** Sunucu son sayfanın ötesini ilk sayfaya normalize eder; adres çubuğu sessizce düzeltilir. */
export function replaceCanonicalPage(state: ListState): void {
  const url = `${window.location.pathname}${toCanonicalSearch(state)}`
  window.history.replaceState(null, '', url)
}
