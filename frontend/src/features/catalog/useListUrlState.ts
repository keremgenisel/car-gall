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

    // Adresi DEĞİŞTİRMEYEN bir eylem geçmişe kayıt eklemez (`0015` QA BULGU-1): aynı aralığı
    // ikinci kez "Uygula", boş eksene geçersiz değer. Aksi hâlde aynı adres geçmişe birden
    // çok kez yazılır ve ziyaretçinin ilk "geri"leri hiçbir şey yapmıyor gibi görünür (AC-31).
    if (url === `${window.location.pathname}${window.location.search}`) {
      return
    }

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
