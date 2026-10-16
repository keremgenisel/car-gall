import { createHmac } from 'node:crypto'
import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { expect, type Page } from '@playwright/test'

/**
 * `0013` D28 · D46 — giriş yolunun **ortak** yeri.
 *
 * <b>Neden ortak:</b> ölçüldü, giriş yapan on dört spec dosyasının dokuzu `girisYap`'ı
 * kendi içinde yeniden tanımlıyordu. D30 Yönetici'de ikinci adımı zorunlu kıldığı için
 * her giriş artık iki fazlı; aynı mantığı on dört yerde yazmak on dört kopya demekti.
 *
 * <b>Yeni bağımlılık YOK</b> (D28): kod `node:crypto`'nun HMAC'ıyla hesaplanıyor ve
 * Base32 çözücü yirmi satır. Parametre kümesi `TimeBasedCode`'un dondurulmuş kümesiyle
 * birebir aynı olmak zorunda (C12/D16) — burası onun **bağımsız ikinci uygulaması** ve
 * ikisinin aynı sonucu vermesi kümenin doğruluğunu bir kez daha ölçüyor.
 */

const PERIYOT_SANIYE = 30
const HANE = 6
const ALFABE = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'

/**
 * Kurulum hesabının sırrı süreçler arasında paylaşılıyor: `globalSetup` ayrı bir
 * süreçte koşuyor ve işçiler onu yalnız dosyadan okuyabilir. Devralınan bir
 * veritabanında sır dosyası kayıpsa `globalSetup` **yüksek sesle** düşer — sessizce
 * 401 almak, kusuru ikinci adımın kendisine yazdırırdı.
 */
const SIR_DOSYASI = join(tmpdir(), 'cargall-e2e-ikinci-adim.txt')

export function ikinciAdimSirriniYaz(sir: string): void {
  writeFileSync(SIR_DOSYASI, sir, 'utf8')
}

export function ikinciAdimSirriniOku(): string | null {
  return existsSync(SIR_DOSYASI) ? readFileSync(SIR_DOSYASI, 'utf8').trim() : null
}

export function adimSimdi(): number {
  return Math.floor(Date.now() / 1000 / PERIYOT_SANIYE)
}

/** RFC 6238: adım sayacının big-endian HMAC-SHA1'i, dinamik kırpma, `10^6` modu. */
export function ikinciAdimKodu(sirBase32: string, adim: number = adimSimdi()): string {
  const sayac = Buffer.alloc(8)
  sayac.writeBigUInt64BE(BigInt(adim))

  const mac = createHmac('sha1', base32Coz(sirBase32)).update(sayac).digest()
  const ofset = mac[mac.length - 1]! & 0x0f
  const ikili =
    ((mac[ofset]! & 0x7f) << 24) |
    (mac[ofset + 1]! << 16) |
    (mac[ofset + 2]! << 8) |
    mac[ofset + 3]!

  return String(ikili % 10 ** HANE).padStart(HANE, '0')
}

function base32Coz(deger: string): Buffer {
  const baytlar: number[] = []
  let tampon = 0
  let bit = 0

  for (const simge of deger.trim().toUpperCase()) {
    tampon = (tampon << 5) | ALFABE.indexOf(simge)
    bit += 5

    if (bit >= 8) {
      bit -= 8
      baytlar.push((tampon >> bit) & 0xff)
    }
  }

  return Buffer.from(baytlar)
}

/**
 * Parola fazı + (gerekiyorsa) ikinci adım fazı.
 *
 * Başarılı giriş TAM GEZİNME yapıyor (panelin dili sunucudan basılıyor); beklemeden bir
 * sonraki `goto` o gezinmeyi yarıda keser ve `ERR_ABORTED` verir. Bu yüzden dört sondan
 * biri beklenir: kabuk · parola ekranı · kurulum ekranı · giriş hatası.
 */
export async function girisYap(
  page: Page,
  kimlik: string,
  parola: string,
  sir: string | null = null,
): Promise<void> {
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()

  await page.getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('Parola', { exact: true }).fill(parola)
  await page.getByRole('button', { name: 'Giriş yap' }).click()

  // Yardımcı ÜÇ DİLDE çalışmak zorunda: panel personelin tercih ettiği dilde basılıyor
  // (`0006` R16) ve etiketle arama yalnız Türkçe koşuda çalışırdı. Testlerin kendi
  // iddiaları etiketle sorgulamaya devam ediyor (`docs/frontend.md` § 8.1); burada
  // seçilen şey alanın KİMLİĞİ, çünkü bu bir yardımcı, bir kriter değil.
  const kodAlani = page.locator('#two-factor-code')

  // İkinci adım fazı yalnız kurulumu OLAN hesapta geliyor (D4); opsiyonel roldeki
  // personel bu dalı hiç görmüyor (D30).
  if (await kodAlani.isVisible({ timeout: 2_000 }).catch(() => false)) {
    await ikinciAdimiGecAsync(page, sir ?? zorunluSir())
  }

  const kabuk = page.getByTestId('admin-shell')
  const parolaEkrani = page.getByTestId('password-form')
  const kurulumEkrani = page.getByTestId('two-factor-page')
  const hata = page.getByTestId('login-error')

  await expect(kabuk.or(parolaEkrani).or(kurulumEkrani).or(hata)).toBeVisible()
}

/**
 * D11/D45 — kabul edilen adım bir daha kabul edilmez: aynı 30 saniyelik pencerede ikinci
 * bir giriş sıradaki adımın kodunu kullanmak zorunda. Paralel koşan iki işçi aynı hesaba
 * girdiğinde bu gerçekten oluyor, bu yüzden üç adım deneniyor ve gerekirse pencerenin
 * açılması beklenir — kırmızı vermek yerine beklemek testin ölçtüğü şeyi değiştirmiyor.
 */
async function ikinciAdimiGecAsync(page: Page, sir: string): Promise<void> {
  const kodAlani = page.locator('#two-factor-code')
  let adim = adimSimdi()

  for (let deneme = 0; deneme < 3; deneme += 1) {
    const bekleme = (adim - 1 - adimSimdi()) * PERIYOT_SANIYE

    if (bekleme > 0) {
      await page.waitForTimeout((bekleme + 1) * 1_000)
    }

    await kodAlani.fill(ikinciAdimKodu(sir, adim))
    await page.locator('form.admin-form button[type="submit"]').click()

    if (!(await kodAlani.isVisible({ timeout: 3_000 }).catch(() => false))) {
      return
    }

    adim += 1
  }

  throw new Error('İkinci adım üç denemede de geçilemedi; kod hesabı ya da tolerans bozuk.')
}

/** Kurulum hesabıyla giriş: çalışan bir Yönetici oturumu. */
export async function yoneticiOlarakGirAsync(page: Page, kimlik: string, parola: string) {
  await girisYap(page, kimlik, parola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()
}

/**
 * Kapılı oturumda kurulumu tamamlar ve sırrı döndürür (D13'ün izin verdiği üç yoldan
 * biri). Kurulum ekranı sırrı **elle giriş metni** olarak da gösteriyor (D17) ve test
 * onu tam oradan okuyor — kare kodu çözmek, ekranın sözünü dolaylı ölçmek olurdu.
 */
export async function ikinciAdimKurAsync(page: Page): Promise<string> {
  await page.getByTestId('two-factor-start').click()

  const sir = (await page.getByTestId('two-factor-manual-secret').innerText()).trim()

  await page.locator('#two-factor-confirm-code').fill(ikinciAdimKodu(sir))
  await page.getByTestId('two-factor-confirm').click()

  await expect(page.getByTestId('recovery-codes')).toBeVisible()

  return sir
}

function zorunluSir(): string {
  const sir = ikinciAdimSirriniOku()

  if (sir === null) {
    throw new Error(
      'Kurulum hesabının ikinci adım sırrı bulunamadı. `globalSetup` onu kuruyor ve ' +
        'geçici dosyaya yazıyor; devralınan bir veritabanıyla koşuyorsan temiz bir ' +
        'yığınla başla (scripts/dev-up.sh).',
    )
  }

  return sir
}
