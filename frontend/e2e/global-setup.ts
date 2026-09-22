import { request } from '@playwright/test'

/**
 * Kurulum hesabının parolasını **bir kez** çalışan hâle getirir.
 *
 * Neden gerekli: kurulum hesabı ilk girişte parola değiştirmek zorunda (D2, R10) ve
 * parola değiştiğinde o personelin TÜM oturumları düşer (R11). Bu ikisi birleşince,
 * her testin kendi başına "önce dene, olmazsa değiştir" yapması üç sorun üretiyordu:
 *
 * 1. Her deneme bir BAŞARISIZ GİRİŞ sayılıyordu ve eşiğe ulaşınca hesap kilitleniyordu
 *    (R6) — testler birbirini kilitliyordu.
 * 2. İki profil (mobil + masaüstü) aynı hesabın parolasını aynı anda değiştirmeye
 *    çalışıyordu.
 * 3. Bir testin parola değişimi ötekinin açık oturumunu düşürüyordu.
 *
 * Buradaki çözüm bunların üçünü de kaynağında kesiyor: parola testler başlamadan önce,
 * tek seferde ve İDEMPOTENT biçimde çalışır hâle getirilir; testler tek bir bilinen
 * parolayla giriş yapar ve hiç başarısız deneme üretmez.
 */
const SEED_KIMLIK = process.env.E2E_SEED_ADMIN_LOGIN ?? 'yonetici@cargallery.local'
const SEED_PAROLA = process.env.E2E_SEED_ADMIN_PASSWORD ?? 'gelistirme-parolasi'

export const YONETICI_PAROLA = 'e2e-yonetici-parolasi'

export default async function globalSetup() {
  const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:5080'
  const context = await request.newContext({ baseURL })

  try {
    // Kalıcı parola zaten geçerliyse (devralınan veritabanı) yapacak bir şey yok.
    const hazir = await context.post('/api/identity/sessions', {
      data: { loginId: SEED_KIMLIK, password: YONETICI_PAROLA },
    })

    if (hazir.ok()) {
      return
    }

    const ilk = await context.post('/api/identity/sessions', {
      data: { loginId: SEED_KIMLIK, password: SEED_PAROLA },
    })

    if (!ilk.ok()) {
      throw new Error(
        `E2E kurulum hesabıyla giriş yapılamadı (${ilk.status()}). ` +
          'Identity:SeedAdmin yapılandırması E2E ortamıyla uyuşmuyor olabilir.',
      )
    }

    const degisim = await context.post('/api/admin/me/password', {
      data: { currentPassword: SEED_PAROLA, newPassword: YONETICI_PAROLA },
    })

    if (!degisim.ok()) {
      throw new Error(`E2E kurulum hesabının parolası değiştirilemedi (${degisim.status()}).`)
    }
  } finally {
    await context.dispose()
  }
}
