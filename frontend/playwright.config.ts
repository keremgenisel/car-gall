import { defineConfig, devices } from '@playwright/test'

/**
 * Kanıt görüntüleri ve kritik akış testleri çalışan Host'a karşı koşar.
 * Host'u `scripts/dev-up.sh` ayağa kaldırır (Postgres + seed + frontend build).
 */
export default defineConfig({
  testDir: './e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  reporter: [['list']],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:5080',
    trace: 'off',
  },
  projects: [
    // Kanıt görüntüleri tek profilde üretilir; iki profil aynı dosyaları üzerine yazardı.
    { name: 'mobil', use: { ...devices['Pixel 7'] }, testIgnore: /evidence\.spec\.ts/ },
    { name: 'masaustu', use: { ...devices['Desktop Chrome'] } },
  ],
})
