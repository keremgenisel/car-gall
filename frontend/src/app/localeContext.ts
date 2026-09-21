import { createContext, useContext } from 'react'
import type { Translator } from '../lib/i18n'
import type { AppLocale } from '../lib/locale'

export interface LocaleContextValue {
  readonly locale: AppLocale
  readonly t: Translator
}

export const LocaleContext = createContext<LocaleContextValue | null>(null)

export function useLocale(): LocaleContextValue {
  const value = useContext(LocaleContext)

  if (value === null) {
    throw new Error('useLocale must be used inside LocaleProvider')
  }

  return value
}
