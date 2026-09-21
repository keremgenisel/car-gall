import js from '@eslint/js'
import globals from 'globals'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import tseslint from 'typescript-eslint'
import prettier from 'eslint-config-prettier'

/**
 * docs/frontend.md § 8 — lint hatasız olmalı; UYARI DA KABUL EDİLMEZ.
 * Sessiz bırakılan uyarı, bir sonraki gerçek uyarıyı gizler.
 *
 * eslint-config-prettier en sonda: biçimle ilgili kuralları kapatır, biçim Prettier'ın işi.
 */
export default tseslint.config(
  { ignores: ['dist', 'node_modules', '../src/Host/wwwroot'] },

  {
    files: ['src/**/*.{ts,tsx}'],
    extends: [
      js.configs.recommended,
      ...tseslint.configs.recommended,
      reactHooks.configs.flat['recommended-latest'],
      reactRefresh.configs.vite,
      prettier,
    ],
    languageOptions: {
      ecmaVersion: 2023,
      globals: globals.browser,
    },
    rules: {
      // docs/frontend.md § 8: console üretime gitmez.
      'no-console': 'error',
      // docs/frontend.md § 1: any yasak, bilinmeyen için unknown + daraltma.
      '@typescript-eslint/no-explicit-any': 'error',
      '@typescript-eslint/consistent-type-imports': ['error', { prefer: 'type-imports' }],
      // Uyarı kabul edilmediği için varsayılan warn seviyeleri error'a çekiliyor.
      'react-refresh/only-export-components': 'error',
    },
  },

  {
    // Kalite kapısı betikleri Node'da koşar ve çıktı yazmak zorundadır.
    files: ['scripts/**/*.mjs', 'e2e/**/*.ts', '*.config.{ts,js}'],
    extends: [js.configs.recommended, ...tseslint.configs.recommended, prettier],
    languageOptions: { globals: globals.node },
    rules: { 'no-console': 'off' },
  },
)
