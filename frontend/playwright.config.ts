import { defineConfig, devices } from '@playwright/test'

/**
 * Kanıt görüntüleri ve kritik akış testleri çalışan Host'a karşı koşar.
 * Host'u `scripts/dev-up.sh` ayağa kaldırır (Postgres + seed + frontend build).
 */
export default defineConfig({
  testDir: './e2e',
  /**
   * Kurulum hesabının parolasını bir kez çalışır hâle getirir. Testler bunu kendi
   * başlarına yapsaydı her deneme bir başarısız giriş sayılır ve hesap kilitlenirdi
   * (0006 R6) — CI'da tam olarak bu oldu.
   */
  globalSetup: './e2e/global-setup.ts',
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
    { name: 'mobil', use: { ...devices['Pixel 7'] }, testIgnore: /evidence.*\.spec\.ts/ },
    { name: 'masaustu', use: { ...devices['Desktop Chrome'] } },
  ],
})
