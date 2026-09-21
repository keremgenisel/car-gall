import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './app/App'
import { localeFromPath } from './lib/locale'
import './app/styles.css'

const locale = localeFromPath(window.location.pathname)
const container = document.getElementById('root')

if (container !== null && locale !== null) {
  // Geçersiz locale öneki sunucuda 404 olur; uç varsayılan bir dile DÜŞMEZ (C7).
  createRoot(container).render(
    <StrictMode>
      <App locale={locale} />
    </StrictMode>,
  )
}
