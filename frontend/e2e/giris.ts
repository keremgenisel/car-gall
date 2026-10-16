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

/**
 * Kurulum hesabının **paylaşılan oturum çerezi**.
 *
 * <b>Neden paylaşılıyor — ölçülmüş bir tavan:</b> D11 kabul edilen adıma eşit veya küçük
 * olanı reddediyor ve tolerans yalnız bir adım ileriyi kabul ediyor (D10). Sonuç: bir
 * hesap için **otuz saniyede bir giriş** (ilk pencerede iki). Depodaki e2e akışları
 * kurulum hesabıyla **altmıştan fazla** kez giriş yapıyor; her biri gerçek bir ikinci
 * adım geçseydi süite yarım saati pencere beklemekle geçirirdi.
 *
 * Çözüm: `globalSetup` **bir kez** gerçek giriş yapıyor (parola + kod) ve çerezi burada
 * bırakıyor; panele girmesi yeterli olan testler onu kullanıyor. Giriş AKIŞINI ölçen
 * testler (`admin.spec`, `iki-adimli`, `evidence-0006`, `evidence-0013`) gerçek girişi
 * kullanmaya devam ediyor — ölçtükleri şey tam olarak o.
 *
 * Paylaşılan oturum bir testin çıkışıyla düşerse yardımcı **yeniden giriş yapıp** çerezi
 * tazeliyor; bedeli o testte bir pencere beklemesi.
 */
const OTURUM_DOSYASI = join(tmpdir(), 'cargall-e2e-oturum.json')

interface Cerez {
  readonly name: string
  readonly value: string
  readonly domain: string
  readonly path: string
  readonly expires: number
  readonly httpOnly: boolean
  readonly secure: boolean
  readonly sameSite: 'Strict' | 'Lax' | 'None'
}

export function oturumCerezleriniYaz(cerezler: readonly Cerez[]): void {
  writeFileSync(OTURUM_DOSYASI, JSON.stringify(cerezler), 'utf8')
}

export function oturumCerezleriniOku(): Cerez[] | null {
  if (!existsSync(OTURUM_DOSYASI)) {
    return null
  }

  return JSON.parse(readFileSync(OTURUM_DOSYASI, 'utf8')) as Cerez[]
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
 * <b>`isVisible()` BEKLEMEZ ve ilk yazım bunu varsaydı:</b> tıklamadan hemen sonra
 * sorulan `isVisible` yanıt gelmeden `false` döndü, ikinci adım fazı atlandı ve 105 test
 * "hiçbir son bulunamadı" diye düştü. Doğrusu önce **bir sona** beklemek, sonra dallanmak.
 *
 * Başarılı giriş TAM GEZİNME yapıyor (panelin dili sunucudan basılıyor); beklemeden bir
 * sonraki `goto` o gezinmeyi yarıda keser ve `ERR_ABORTED` verir. Bu yüzden beş sondan
 * biri beklenir: kabuk · parola ekranı · kurulum ekranı · giriş hatası · **kod alanı**.
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

  await expect(sonlar(page).or(kodAlani).first()).toBeVisible()

  // İkinci adım fazı yalnız kurulumu OLAN hesapta geliyor (D4); opsiyonel roldeki
  // personel bu dalı hiç görmüyor (D30).
  if (await kodAlani.isVisible()) {
    await ikinciAdimiGecAsync(page, sir ?? zorunluSir())
  }

  await expect(sonlar(page).first()).toBeVisible()
}

/** Oturum öncesi akışın olası sonları; kod alanı hariç. */
function sonlar(page: Page) {
  return page
    .getByTestId('admin-shell')
    .or(page.getByTestId('password-form'))
    .or(page.getByTestId('two-factor-page'))
    .or(page.getByTestId('login-error'))
}

/**
 * D11/D45 — kabul edilen adım bir daha kabul edilmez.
 *
 * <b>Yeniden deneme PAHALI ve ölçüldü:</b> R8 gereği yanlış kod **paylaşılan kilitlenme
 * sayacını** artırıyor, yani her "sıradaki adımı dene" bir başarısız giriş demek. İlk
 * yazım bunu üç kez yapıyordu ve kurulum hesabını eşiğe (5) taşıyıp **on beş dakika**
 * kilitliyordu; arkadan gelen bütün testler "e-posta veya parola hatalı" alıyordu ve
 * kusurun yeri hiç orada değildi.
 *
 * Doğrusu REDDEDİLECEĞİNİ BİLDİĞİMİZ kodu hiç göndermemek: süreç içinde kullanılan son
 * adım tutuluyor ve aynı adım ikinci kez istenirse pencerenin dönmesi beklenir. Süreçler
 * arası çakışma için **tek** bir yeniden deneme kalıyor — bir başarısız giriş, eşiğin
 * beşte biri.
 */
const sonKullanilanAdim = new Map<string, number>()

async function ikinciAdimiGecAsync(page: Page, sir: string): Promise<void> {
  const kodAlani = page.locator('#two-factor-code')
  const hata = page.getByTestId('login-error')

  for (let deneme = 0; deneme < 2; deneme += 1) {
    const adim = await bosAdimAsync(page, sir)

    await kodAlani.fill(ikinciAdimKodu(sir, adim))
    await page.locator('form.admin-form button[type="submit"]').click()

    // Ya bir son geldi (oturum açıldı) ya da red basıldı; ikisinden biri gelene kadar
    // beklenir — `isVisible` tek başına yanıtı beklemiyor.
    await expect(sonlar(page).first()).toBeVisible()

    if (!(await hata.isVisible())) {
      return
    }

    // Ara durum düştüyse kod denemenin anlamı yok: ekran parola fazına döndü (D20).
    if (!(await kodAlani.isVisible())) {
      throw new Error('Ara durum düştü (two_factor_pending_expired); parola fazına dönüldü.')
    }
  }

  throw new Error(
    'İkinci adım iki denemede de geçilemedi. Olası sebep: başka bir süreç aynı hesaba ' +
      'aynı pencerede girdi ya da hesap kilitlendi (R8).',
  )
}

/**
 * Bu süreçte henüz kullanılmamış bir adım döndürür; gerekirse pencerenin dönmesini
 * bekler. Beklemek, reddedilecek bir kod göndermekten ucuz: o kod sayaca yazılırdı.
 */
async function bosAdimAsync(page: Page, sir: string): Promise<number> {
  const son = sonKullanilanAdim.get(sir) ?? 0
  let adim = adimSimdi()

  if (adim <= son) {
    adim = son + 1

    // Tolerans yalnız BİR adım ileriyi kabul ediyor (D10): daha ileri bir adım için
    // pencerenin dönmesi beklenir.
    const bekleme = (adim - 1 - adimSimdi()) * PERIYOT_SANIYE

    if (bekleme > 0) {
      await page.waitForTimeout((bekleme + 1) * 1_000)
    }
  }

  sonKullanilanAdim.set(sir, adim)

  return adim
}

/**
 * Çalışan bir Yönetici oturumu — **paylaşılan çerezle**.
 *
 * Panele girmesi yeterli olan testler bunu kullanıyor: giriş akışını değil panelin
 * kendisini ölçüyorlar ve her biri gerçek bir ikinci adım geçseydi süite D11'in otuz
 * saniyelik penceresine takılırdı (bkz. `OTURUM_DOSYASI`).
 *
 * Çerez düşmüşse (bir test çıkış yaptı ya da parola değişti) yardımcı gerçek girişi
 * yapıp çerezi tazeliyor.
 */
export async function yoneticiOlarakGirAsync(page: Page, kimlik: string, parola: string) {
  const cerezler = oturumCerezleriniOku()

  if (cerezler !== null) {
    await page.context().addCookies(cerezler)
    await page.goto('/admin')

    const kabuk = page.getByTestId('admin-shell')

    await expect(kabuk.or(page.getByTestId('login-form')).first()).toBeVisible()

    if (await kabuk.isVisible()) {
      return
    }
  }

  await page.context().clearCookies()
  await girisYap(page, kimlik, parola)
  await expect(page.getByTestId('admin-shell')).toBeVisible()

  oturumCerezleriniYaz(await page.context().cookies())
}

/**
 * Kapılı oturumda kurulumu tamamlar ve sırrı döndürür (D13'ün izin verdiği üç yoldan
 * biri). Kurulum ekranı sırrı **elle giriş metni** olarak da gösteriyor (D17) ve test
 * onu tam oradan okuyor — kare kodu çözmek, ekranın sözünü dolaylı ölçmek olurdu.
 */
export async function ikinciAdimKurAsync(page: Page): Promise<string> {
  await page.getByTestId('two-factor-start').click()

  const sir = (await page.getByTestId('two-factor-manual-secret').innerText()).trim()
  const adim = adimSimdi()

  await page.locator('#two-factor-confirm-code').fill(ikinciAdimKodu(sir, adim))
  await page.getByTestId('two-factor-confirm').click()

  await expect(page.getByTestId('recovery-codes')).toBeVisible()

  // ONAY KODU da bir adım tüketti (D11): bunu kaydetmezsek hemen ardından gelen giriş
  // aynı adımı dener, reddedilir ve R8'in sayacına boşuna bir başarısızlık yazılır.
  sonKullanilanAdim.set(sir, adim)

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

/**
 * Giriş AKIŞINI ölçen testler için **kendi** hesabı: yeni bir Yönetici kaydeder, ilk
 * parolasını değiştirir, ikinci adımını kurar ve sırrı döndürür.
 *
 * <b>Neden kendi hesabı:</b> kurulum hesabıyla yapılan her gerçek giriş hem D11'in otuz
 * saniyelik penceresine hem R8'in paylaşılan kilitlenme sayacına yazıyor. Taze bir hesap
 * ikisini de kendi üzerinde taşır: testler birbirini ne bekletir ne kilitler.
 *
 * Çağıran, çalışan bir Yönetici oturumuyla gelmek zorundadır (`yoneticiOlarakGirAsync`).
 */
export async function yeniKurulmusYoneticiAsync(
  page: Page,
  kimlik: string,
  ilkParola: string,
  yeniParola: string,
): Promise<string> {
  await page.goto('/admin')
  await page.getByLabel('Ad soyad').fill('Akış Yöneticisi')
  await page.getByTestId('add-staff-form').getByLabel('E-posta').fill(kimlik)
  await page.getByLabel('İlk parola').fill(ilkParola)
  await page.getByLabel('Rol', { exact: true }).selectOption('manager')
  await page.getByLabel('Panel dili').selectOption('tr')
  await page.getByRole('button', { name: 'Personeli ekle' }).click()

  await expect(page.getByText(kimlik)).toBeVisible()

  // Oturumu BIRAKIYORUZ, kapatmıyoruz: çıkış paylaşılan oturumu sunucuda düşürür ve
  // arkadan gelen testlere bir pencere beklemesi ödetirdi (D45'in ölçümü).
  await page.context().clearCookies()

  await girisYap(page, kimlik, ilkParola)
  await page.getByLabel('Mevcut parolanız').fill(ilkParola)
  await page.getByLabel('Yeni parolanız').fill(yeniParola)
  await page.getByRole('button', { name: 'Parolamı değiştir' }).click()
  await expect(page.getByTestId('password-changed')).toBeVisible()

  // Parola değişti; Yönetici rolünde ikinci adım ZORUNLU, yani bu giriş kapılı bir
  // oturum veriyor (D13) ve kurulum ekranı açılıyor.
  await girisYap(page, kimlik, yeniParola)
  await expect(page.getByTestId('two-factor-page')).toBeVisible()

  const sir = await ikinciAdimKurAsync(page)

  await page.context().clearCookies()

  return sir
}

/**
 * Bu süreçte henüz kullanılmamış bir adımın kodu.
 *
 * Testler kodu **doğrudan** `ikinciAdimKodu` ile hesaplamamalı: kurulum onayı bir adım
 * tüketiyor (D11) ve aynı pencerede hesaplanan kod reddedilir. Ölçüldü — `AC-21`'in e2e
 * vakası tam bu yüzden kırmızı verdi.
 */
export async function tazeKodAsync(page: Page, sir: string): Promise<string> {
  return ikinciAdimKodu(sir, await bosAdimAsync(page, sir))
}

/**
 * Oturumu **bırakır**, kapatmaz: çerezi yalnız tarayıcıdan atar, sunucudaki satıra
 * dokunmaz.
 *
 * <b>Ölçülmüş sebep:</b> çıkış paylaşılan oturumu sunucuda düşürüyor ve arkadan gelen her
 * test gerçek girişe düşüyor; o girişler D11'in otuz saniyelik penceresinde çakışıyor,
 * her çakışma R8'in paylaşılan sayacına bir başarısızlık yazıyor ve beşincisinde kurulum
 * hesabı **on beş dakika** kilitleniyor. Ölçüldü: bir koşuda 246 vakanın 81'i bu
 * zincirden düştü.
 *
 * Kullanıcı DEĞİŞTİRMEK isteyen testler bunu kullanıyor; çıkışın KENDİSİ `0006`
 * AC-18/AC-19'un konusu ve onu `Host.Tests/OturumBoruHattiTests` sunucu tarafında
 * ölçüyor.
 */
export async function oturumuBirakAsync(page: Page): Promise<void> {
  await page.context().clearCookies()
  await page.goto('/admin')
  await expect(page.getByTestId('login-form')).toBeVisible()
}
