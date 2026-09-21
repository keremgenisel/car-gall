import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// Build çıktısı doğrudan Host'un wwwroot'una gider: host index.html'i servis ederken
// locale'e göre lang/dir enjekte eder (docs/architecture.md § 3, tam SSR yok).
export default defineConfig({
  plugins: [react()],
  base: '/',
  build: {
    outDir: '../src/Host/wwwroot',
    emptyOutDir: true,
  },
  server: {
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
  test: {
    environment: 'jsdom',
    include: ['src/**/*.test.{ts,tsx}'],
  },
})
