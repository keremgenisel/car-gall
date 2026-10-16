import { request, type APIRequestContext } from '@playwright/test'
import {
  ikinciAdimKodu,
  ikinciAdimSirriniOku,
  ikinciAdimSirriniYaz,
  oturumCerezleriniYaz,
} from './giris'

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
// ADR 0011 — varsayılan YOK. Depoda sabit bir kurulum parolası bırakmak, depo herkese
// açık olduğu için parolası kamuya açık bir Yönetici hesabı demekti. Değer koşuyu
// başlatandan gelir: CI'da üretilip `$GITHUB_ENV`'e yazılır, geliştirmede
// `scripts/dev-up.sh` üretir ve ekrana yazar. Yoksa SESSİZCE 401 almak yerine burada
// yüksek sesle düşüyoruz.
const SEED_PAROLA = process.env.E2E_SEED_ADMIN_PASSWORD
if (!SEED_PAROLA) {
  throw new Error(
    'E2E_SEED_ADMIN_PASSWORD tanımlı değil. Host hangi kurulum parolasıyla başlatıldıysa ' +
      'aynısını ver (scripts/dev-up.sh onu ekrana yazıyor).',
  )
}

export const YONETICI_PAROLA = 'e2e-yonetici-parolasi'

export default async function globalSetup() {
  const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost:5080'
  const context = await request.newContext({ baseURL })

  try {
    // Kalıcı parola zaten geçerliyse (devralınan veritabanı) parola adımı atlanır.
    let durum = await girisDurumuAsync(context, YONETICI_PAROLA)

    if (durum === null) {
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

      // Parola değişimi TÜM oturumları düşürdü (0006 R11): yeniden giriş.
      durum = await girisDurumuAsync(context, YONETICI_PAROLA)
    }

    await ikinciAdimiKurAsync(context, durum)
    await paylasilanOturumuKurAsync(context)
  } finally {
    await context.dispose()
  }
}

/** Girişin hangi dalla döndüğü; başarısız girişte `null` (D15). */
async function girisDurumuAsync(
  context: APIRequestContext,
  parola: string,
): Promise<string | null> {
  const yanit = await context.post('/api/identity/sessions', {
    data: { loginId: SEED_KIMLIK, password: parola },
  })

  if (!yanit.ok()) {
    return null
  }

  return String(((await yanit.json()) as { status: string }).status)
}

/**
 * `0013` D28 — kurulum hesabının ikinci adımı **bir kez** burada kurulur.
 *
 * D30 Yönetici rolünde ikinci adımı zorunlu kıldı ve D13 kurulumsuz hesabı kapıladı;
 * yani R7'nin kapısı depodaki **her** e2e akışının önüne geçiyor. Her testin kendi
 * başına kurması parolada olduğu gibi üç sorun üretirdi (kilitlenme, iki profil, düşen
 * oturum) ve dördüncüsünü eklerdi: ikinci kurulum `409` ile düşer.
 *
 * <b>Devralınan veritabanı ÖLÇÜLDÜ ve ilk yazım yanlıştı:</b> kurulum yapıldıktan sonra
 * parola girişi artık oturum çerezi vermiyor (`two_factor_required`, D4), yani "zaten
 * kurulu mu" sorusunu `409` ile sormak **401** alıyordu. Doğru soru girişin hangi dalla
 * döndüğü; kurulum yalnız `two_factor_setup_required` dalında yapılabilir.
 *
 * Sır geçici bir dosyaya yazılıyor; işçiler onu oradan okuyor (`giris.ts`).
 */
async function ikinciAdimiKurAsync(
  context: APIRequestContext,
  durum: string | null,
): Promise<void> {
  // Kurulum ZATEN var: oturum çerezi hiç yazılmadı ve kurulacak bir şey yok. Sır dosyası
  // duruyorsa testler kodu hesaplayabilir; durmuyorsa geri dönüş yolu yok.
  if (durum === 'two_factor_required') {
    if (ikinciAdimSirriniOku() !== null) {
      return
    }

    throw new Error(
      'E2E kurulum hesabının ikinci adımı KURULU ama sırrı kayıp: devralınan bir ' +
        'veritabanıyla koşuyorsun ve geçici sır dosyası silinmiş. Temiz bir yığınla ' +
        'başla (scripts/dev-up.sh).',
    )
  }

  const baslat = await context.post('/api/admin/me/two-factor', { data: {} })

  if (!baslat.ok()) {
    throw new Error(
      `E2E kurulum hesabının ikinci adımı başlatılamadı (${baslat.status()}, dal: ${durum}).`,
    )
  }

  const sir = String(((await baslat.json()) as { secret: string }).secret)

  const onay = await context.post('/api/admin/me/two-factor/confirm', {
    data: { code: ikinciAdimKodu(sir) },
  })

  if (!onay.ok()) {
    throw new Error(`E2E kurulum hesabının ikinci adımı onaylanamadı (${onay.status()}).`)
  }

  ikinciAdimSirriniYaz(sir)
}

/**
 * `0013` D11'in ölçülmüş tavanı: bir hesap için **otuz saniyede bir giriş**. Depodaki
 * akışlar kurulum hesabıyla altmıştan fazla kez giriş yapıyor; her biri gerçek bir
 * ikinci adım geçseydi süite yarım saati pencere beklemekle geçirirdi.
 *
 * Bu yüzden gerçek giriş **bir kez** burada yapılıyor ve çerezi paylaşılıyor. Giriş
 * AKIŞINI ölçen testler gerçek girişi kullanmaya devam ediyor — ölçtükleri şey tam o.
 */
async function paylasilanOturumuKurAsync(context: APIRequestContext): Promise<void> {
  const sir = ikinciAdimSirriniOku()

  if (sir === null) {
    throw new Error('Paylaşılan oturum kurulamadı: ikinci adım sırrı yok.')
  }

  const giris = await context.post('/api/identity/sessions', {
    data: { loginId: SEED_KIMLIK, password: YONETICI_PAROLA },
  })

  if (!giris.ok()) {
    throw new Error(`Paylaşılan oturum için giriş yapılamadı (${giris.status()}).`)
  }

  const durum = String(((await giris.json()) as { status: string }).status)

  // Kurulumu olan hesapta dal `two_factor_required`; oturum çerezi ancak ikinci adımdan
  // sonra yazılıyor (D4).
  if (durum === 'two_factor_required') {
    let adim = Math.floor(Date.now() / 1000 / 30)
    let gecti = false

    for (let deneme = 0; deneme < 3 && !gecti; deneme += 1) {
      const yanit = await context.post('/api/identity/sessions/two-factor', {
        data: { code: ikinciAdimKodu(sir, adim) },
      })

      gecti = yanit.ok()
      adim += 1
    }

    if (!gecti) {
      throw new Error('Paylaşılan oturum için ikinci adım geçilemedi.')
    }
  }

  const { cookies } = await context.storageState()

  oturumCerezleriniYaz(cookies)
}
