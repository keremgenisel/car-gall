import { useMemo, type ReactNode } from 'react'
import { createTranslator } from '../shared/i18n/i18n'
import type { AppLocale } from '../shared/lib/locale'
import { LocaleContext, type LocaleContextValue } from './localeContext'

export function LocaleProvider({
  locale,
  children,
}: {
  readonly locale: AppLocale
  readonly children: ReactNode
}) {
  const value = useMemo<LocaleContextValue>(
    () => ({ locale, t: createTranslator(locale) }),
    [locale],
  )

  return <LocaleContext.Provider value={value}>{children}</LocaleContext.Provider>
}
