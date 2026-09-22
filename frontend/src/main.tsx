import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './app/App'
import { localeFromPath } from './shared/lib/locale'
import { ADMIN_SEGMENT } from './shared/lib/route'
import { adminLocaleFromDocument } from './app/adminLocale'
import './app/styles.css'

const container = document.getElementById('root')

// Panel locale öneki TAŞIMAZ (/admin): dili sunucunun bastığı `<html lang>`'den okunur
// (0006 R16). Bu dal olmasaydı `localeFromPath` null döner ve panel hiç render edilmezdi.
const isAdmin = window.location.pathname.split('/').filter(Boolean)[0] === ADMIN_SEGMENT

const locale = isAdmin ? adminLocaleFromDocument('tr') : localeFromPath(window.location.pathname)

if (container !== null && locale !== null) {
  // Geçersiz locale öneki sunucuda 404 olur; uç varsayılan bir dile DÜŞMEZ (C7).
  createRoot(container).render(
    <StrictMode>
      <App locale={locale} />
    </StrictMode>,
  )
}
