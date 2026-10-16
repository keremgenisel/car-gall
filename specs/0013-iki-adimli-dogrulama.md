# 0013 — İki Adımlı Doğrulama

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Identity` · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0006` (oturum ve roller — tamamlandı) · `0009` (bildirim — tamamlandı) |
| **Dayanak** | ADR 0003 unsur 2; Ek 1 sıra #6 |
| **Karar bekliyor** | **Yok.** Beş sorunun hepsi kapandı: **S2** birinci CLARIFY turunda (**D8**, ölçümle), **S1 · S3 · S4 · S5** ikinci turda (**D30–D33**, Takım Yöneticisi onayı 2026-10-15). |
| **Kararlar** | **D1–D33**, iki CLARIFY turunda bugünkü `main` üzerinde **ölçülerek** verildi (**D1–D12** → `c5143b6`, **D13–D33** → `1e7c8d2`). Beşi spec'i yazılı hâliyle **uygulanamaz** kılıyordu. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı; **sıradaki tur kriter
> turudur** ve bu kararların üzerine kurulacak.
>
> İki CLARIFY turu geçti. **Birincisi** (2026-10-15, `c5143b6`) on iki karar ekledi ve
> R8, C2, R12'yi ölçümle düzeltti — sırrın saklanmasını, eşiği, toleransı ve kaydın
> yerini çözdü. **İkincisi** (2026-10-15, `1e7c8d2`) yirmi bir karar ekledi ve
> **akışın kendisini** kurdu: ara durumun taşınması, ikinci adım ekranına ulaşılması,
> giriş yanıtının şekli ve kurulum zorunluluğunun mevcut yönetim yüzeyiyle ilişkisi.
> İkinci turun iki bulgusu spec'i uygulanamaz kılıyordu (**D13**, **D15**) ve biri
> **birinci turun bir ölçümünü düzeltti** (**D27**).
>
> *Numaralandırma:* kararlar tek dizi hâlinde sürüyor (**D1–D33**); kart kendi
> kararlarına çıplak numarayla atıf yapıyor ve ikinci turu D1'den başlatmak aynı
> numarayı iki ayrı karara verirdi.

---

## Intent

`0006` panelin kapısını kurdu: personel kendi hesabıyla giriyor ve yönetim yüzeyinin tamamı
oturum istiyor. Ama o kapının tek kilidi **bir parola** ve o parola `0006` D1 gereği
**sistem dışından** (telefon/WhatsApp) iletiliyor; yani parola, sistemin hiç görmediği bir
kanalda dolaşıyor.

Panelin arkasında duran şey artık önemsiz değil: galeri stoğunun tamamı, fiyatlar ve
**ziyaretçilerin kişisel verileri** (`0005`'in topladığı ad, telefon, e-posta). Tek parolayla
korunan bir yüzey, o verinin tamamını tek bir sızıntıya bağlar.

**Galeri sahibi** kendi ve yöneticilerinin hesabına ikinci bir adım istiyor; **ekip** ise
telefonunu kaybeden personelin panele kilitlenmemesini istiyor.

Bu iş bittiğinde yetkili hesaplar girişte ikinci bir adım geçer, telefonunu kaybeden personel
kurtarma koduyla girer, ikisi de yoksa bir Yönetici onun ikinci adımını sıfırlar. Başarı
ölçütü: **panele parolası sızmış biri tek başına giremez.**

---

## Requirements

- **R1.** İki adımlı doğrulaması etkin bir personel, parolasını doğru girdikten sonra
  **ikinci bir adım** geçmek zorundadır; bu adım geçilmeden oturum açılmaz.
- **R2.** İkinci adım, personelin kendi cihazında üretilen **zamana bağlı bir koddur**;
  kod belirli bir süre geçerlidir ve süresi dolunca kabul edilmez. Kodun **hane sayısı,
  penceresi ve algoritması** yaygın doğrulama uygulamalarının okuduğu kümedir ve
  **D16**'da sayıyla yazılıdır; bu küme kurulmuş kayıtları geçersizleştirdiği için
  dondurulmuştur (**C12**).
- **R3.** Personel ikinci adımı **kurar**: sistem bir sır üretir, personel bunu kendi
  doğrulama uygulamasına ekler ve **bir kod girerek kurulumu doğrular**. Doğrulanmamış
  kurulum etkinleşmez. Sır personele **iki biçimde birlikte** sunulur: taranabilir bir
  kare kod **ve** her zaman görünür duran elle giriş metni (**D17**). Doğrulanmamış
  kurulum **süresi dolar** ve kurulum yeniden başlatıldığında **yeni bir sır** üretilir
  (**D18**) — "bir kez gösterilir" kuralı ancak böyle doğru kalır.
- **R4.** Kurulum sırasında personele **kurtarma kodları** verilir. Her kurtarma kodu **tek
  kullanımlıktır**. Kod sayısı yapılandırmadan gelir (**D9**).
- **R5.** Personel, kodunu üretemediğinde bir kurtarma koduyla giriş yapabilir; kullanılan kod
  bir daha kabul edilmez. Personel **kalan kod sayısını** kendi ekranında görür ve kümeyi
  **yenileyebilir**; yenileme kalanların **hepsini** düşürür (**R16**, **D24**).
- **R6.** Kurtarma kodu da kalmayan personelin ikinci adımı bir **Yönetici** tarafından
  sıfırlanır; sıfırlanan personel bir sonraki girişinde yeniden kurar. Sıfırlama bir
  **güvenlik işlemidir**: personelin açık oturumları **derhal düşer** ve kalan kurtarma
  kodları geçersizleşir (**D22**, `0006` R11/R14 deseni).
- **R7.** İkinci adımın hangi roller için **zorunlu** olduğu bellidir ve zorunlu bir rolde
  kurulumu olmayan personel yönetim yüzeyinde **çalışamaz**; önce kurar (**D30**:
  Yönetici zorunlu, diğer iki rol opsiyonel). Kısıtlama bir **kapı** ile uygulanır ve
  izin verilen yollar **açıkça sayılır**: kendi parolası, kendi ikinci adım kurulumu ve
  çıkış (**D13**).
- **R8.** Yanlış girilen ikinci adım kodu, parola denemesiyle **aynı kilitlenme sayacına**
  ve **aynı hız sınırına** tabidir. Eşik **5**'tir (**D8**); hız sınırı giriş ucunun
  politikasıdır (**D5**). Sayaç **uçta da tektir**: kilitlenme ipucunu basan deneme
  sayacı iki fazın **ortak** sayacıdır (**D21**) — sunucu kilitlenmeyi söylemiyor
  (`0006` AC-58) ve ekranın iki ayrı sayaç tutması personele yanlış an söylerdi.
- **R9.** Kodun hangisinin yanlış olduğu — parola mı ikinci adım mı — giriş ekranında
  **söylenmez** ötesinde: ikinci adım ekranına geçilmiş olması, parolanın doğru olduğunu
  zaten ifşa eder — ve bu **kabul edilmiştir** (**D33**).
- **R10.** İkinci adım ekranı **yükleniyor**, **hata**, **süresi doldu** ve **kilitlenmiş
  hesap** durumlarını ele alır. → `docs/frontend.md` § 3. *"Süresi doldu"* **ara durumun**
  süresidir ve personeli parola fazına geri döndürür; kodun kendi penceresinin dolması
  **hata** durumudur ve taze bir kod istenir. İkisi ayrı hata kodu taşır (**D20**),
  çünkü personelin yapacağı şey farklıdır.
- **R11.** Ekran personelin tercih ettiği dilde çalışır; Arapça'da RTL. → `0006` R15.
- **R12.** İkinci adımın kurulması, **kapatılması**, sıfırlanması ve kurtarma kodunun
  kullanılması **kaydedilir** — ve kaydın nerede yaşadığı **D12**'de tanımlıdır: bu
  depoda bugün denetim kaydı altyapısı YOK (ölçüldü). Kayıt **başarısız denemeleri
  tutmaz**: onları kilitlenme sayacı zaten taşıyor ve her denemeyi yazmak tabloyu kaba
  kuvvetin büyüteci yapardı.
- **R13.** Parolası doğrulanmış ama ikinci adımı geçmemiş personelin **ara durumu**
  sunucuda yaşar, **kısa ömürlüdür** ve **tek kullanımlıktır**: başarılı doğrulamada
  tükenir. Ara durum **oturum değildir** — hiçbir yönetim isteği onunla çalışmaz
  (**D4**, **D19**).
- **R14.** Oturum öncesi akış **tek ekrandır**: ikinci adım, giriş ekranının bir
  **fazıdır** ve adım geçilmeden panele gezinme olmaz. Mockup'ın *"Kullanıcı değiştir"*
  eylemi ara durumu **siler** (**D14**).
- **R15.** İkinci adım **opsiyonel** olan bir rolde personel tarafından **kapatılabilir**;
  kapatma **mevcut parola ve geçerli bir kod** ister. Zorunlu bir rolde kapatma yoktur;
  oradaki tek yol R6'nın sıfırlamasıdır (**D23**).
- **R16.** Personel kurtarma kodu kümesini **yenileyebilir**: yenileme oturum **ve**
  taze bir kod ister, kalan kodların hepsini düşürür ve yeni kümeyi **bir kez** gösterir
  (**D24**).
- **R17.** İkinci adım durumu **görünürdür**: Yönetici personel listesinde her satırın
  durumunu, personel de kendi durumunu görür (**D25**, **D26**). Mockup'ın "2FA açık"
  rozeti veri olmadan basılamaz.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Identity`, personel kimlik verisinin tek yazma sahibidir. → `docs/architecture.md` § 1.
- **C2.** **İKİYE AYRILDI (D1)** — bu madde yazıldığı hâliyle R2'yi ve R3'ü
  **uygulanamaz** kılıyordu:
  - **C2a — doğrulama sırrı** geri döndürülebilir biçimde saklanmak **ZORUNDADIR**: zamana
    bağlı kodu doğrulamanın tek yolu aynı sırrı kullanarak kodu yeniden üretmektir. Sır
    **loglanmaz**, API yanıtında **yalnız kurulum anında bir kez** görünür ve veritabanında
    **açık metin durmaz** (**D3**).
  - **C2b — kurtarma kodları** tek yönlü özetlenerek saklanır (`0006` C6 deseni, mevcut
    `IPasswordHasher`), loglanmaz ve kurulum anında bir kez gösterilir (**D2**).
- **C3.** **Roller üç tanedir**; "Sahip" diye dördüncü bir rol eklenmez. → ADR 0003 unsur 3.
- **C4.** Dışarıya giden mesaj yalnızca `Notifications` üzerinden gider. → Yasak Listesi 17.
- **C5.** Zaman hesapları UTC ve saat soyutlaması üzerinden; `DateTime.Now` kullanılmaz.
  Zamana bağlı kod doğrulaması saat kaymasına karşı dayanıklı olmalıdır. → Yasak Listesi 30.
- **C6.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner. → Altın Kural 6.
- **C7.** Veritabanında çevrilmiş durum adı saklanmaz. → Yasak Listesi 22.
- **C8.** CSS'te fiziksel yön özellikleri kullanılmaz. → Yasak Listesi 25.
- **C9.** Yeni migration yalnızca `identity` şemasına dokunur. → Yasak Listesi 3.
- **C10.** **Son aktif Yönetici korunur:** sıfırlama yetkisi tek Yöneticinin kendi hesabını
  kilitlemesine yol açamaz. → `0006` D3 ile tutarlı; sıfırlama yetkisinin sınırı **D32**'de.
- **C11.** Her kabul kriteri en az bir testle karşılanır. → Altın Kural 5.
- **C12.** **Kodun parametre kümesi dondurulmuştur** (**D16**): hane sayısı, pencere ve
  algoritma kurulmuş her kaydı bağlar; değiştirmek **bütün** kurulumları geçersizleştirir.
  Bu yüzden değerler yapılandırmadan okunan bir eşik DEĞİL, spec'te sayıyla yazılı bir
  sözleşmedir — `0006` D4'ün eşiklerinden bilinçli olarak ayrılıyor.
- **C13.** **Bekleyen yanıt kimlik taşımaz** (**D15**): ikinci adım geçilmeden dönen
  gövdede personelin adı, rolü, dili ve kimliği **bulunmaz**. Elinde yalnız parola olan
  kişi, ikinci adımı geçmeden personelin kim olduğunu öğrenmez.
- **C14.** **Yönetim kökü bölünmez:** ikinci adımın kurulum ve kapatma uçları
  `/api/admin` altında kalır ve oturum ister (`0006` C18/AC-23, rotaları tarayan host
  testi). Oturumsuz köke ikinci bir yönetim yüzeyi açılmaz (**D13**).
- **C15.** **Sır sunucuda görsele gömülmez:** kare kodu uç çizer; sır yanıtta yalnız
  metin olarak geçer (**D17**). Sunucuda üretilen bir görsel, sırrı önbellek ve log
  yüzeylerine taşırdı (§ Bilinen tuzaklar 2).

### KAPSAM DIŞI

- **X1.** **SMS ile doğrulama kodu** — *neden:* `0009` § X1 SMS kanalını kapsam dışı bıraktı;
  ayrıca SMS, cihazda üretilen koddan zayıftır (SIM takası).
- **X2.** **Donanım anahtarı / WebAuthn / passkey** — *neden:* ayrı bir tarayıcı API'si ve
  kurtarma modeli; 2–3 kişilik ekip için V1'de gereksiz.
- **X3.** **SSO ve harici kimlik sağlayıcı** — *neden:* `0006` § X2'de kapsam dışı.
- **X4.** **"Bu cihazı hatırla"** — *neden:* cihaz güveni ayrı bir model (cihaz kaydı, süre,
  iptal) ve ikinci adımın değerini azaltan ilk yerdir.
- **X5.** **Ziyaretçi tarafı** — *neden:* public üyelik yok (§ 6); sistemde kullanıcı =
  personeldir.
- **X6.** **Parola politikasının genişletilmesi** — *neden:* `0006` § X9'da kapsam dışı.

---

## Context

### Mevcut davranış — doğrulandı

- **`0006` 2FA'yı bilinçli olarak kapsam dışı bırakmıştı** (§ X2) ve gerekçesi *"2–3 kişilik
  bir galeri ekibi için sürtünme üretir"*di. **ADR 0003 unsur 2 bu kararı açtı** ve 2FA'yı
  `Notifications` sonrasına koydu — `0009` tamamlandığı için önkoşul karşılandı.
- **`0006` giriş akışını kurdu:** `POST /api/identity/sessions` (giriş),
  `DELETE /api/identity/sessions/current` (çıkış), `POST .../me/password`,
  `POST .../staff/{id}/password-reset` uçları mevcut.
- **`0006` D4 sayıları sabitledi:** oturum 30 dk, yenileme 14 gün, **5 hatalı denemede
  15 dakika kilit**, asgari parola 12 karakter.
- **`0006` D1:** davet ve parola sıfırlama **e-postası yok**; ilk parola sistem dışından
  iletilir. `Notifications` o zaman yoktu, **şimdi var** — bu, kurtarma akışı için yeni bir
  seçenek açıyor — ve o seçenek **D31** ile reddedildi.
- **Mockup iki ekran çiziyor.** `PanelGiris`: *"Sahip ve yönetici hesaplarında ikinci adımda
  doğrulama kodu istenir. **8 hatalı** denemeden sonra giriş 15 dakika kilitlenir."*
  `Panel2FA`: 6 haneli kod, 30 saniyede yenilenir, kalan süre sayacı, "Kullanıcı değiştir",
  "Kurtarma kodu kullan" ve *"Kurtarma kodu da yoksa **sahip hesabı** senin iki adımlı
  doğrulamanı panelden sıfırlayabilir."* Üç panelde "2FA açık" rozeti var.
- **İki çelişki:** mockup'ın **8 denemesi** `0006` D4'ün **5**'iyle çelişiyor (S2);
  mockup'ın **"Sahip" rolü** ADR 0003 unsur 3 ile çelişiyor (C3, S1).

**2026-10-15'te yeniden ölçülenler** (CLARIFY turu, `main` = `c5143b6`):

| Ne ölçüldü | Sonuç |
|---|---|
| İkinci adıma ait mevcut kod | **YOK** — `totp\|twofactor\|2fa\|authenticator` taraması sıfır dosya |
| Roller | **üç**: `Manager`, `SalesAdvisor`, `ContentEditor` (`StaffRole.cs:12`); "Sahip" yok → S1'in çelişkisi doğrulandı |
| Kilitlenme eşiği | **5**, yapılandırmadan (`IdentityOptions.cs:15`) → S2 kapandı (**D8**) |
| Kilitlenme durumu | `LockoutState` + `RegisterFailure(now, eşik, süre)`, `Login.cs:73`'ten çağrılıyor → R8 aynı sayacı kullanabilir |
| Hız sınırı | giriş ucunda `RequireRateLimiting`, `LoginRequestLimit` 20 / 10 dk → **D5** |
| Oturum token'ı | **yalnız çerez**, gövdeye hiç girmiyor (`SessionEndpoints.cs:80`) → **D4**'ün dayanağı |
| Yarım-oturum deseni | `MustChangePasswordClaim` + `PasswordChangeGate` **var** → **D4**'te elenen alternatif |
| Özetleme / şifreleme | `Pbkdf2PasswordHasher` var; **geri döndürülebilir şifreleme YOK** (`DataProtection`/`Aes` sıfır sonuç) → **D1**, **D3** |
| Parola iterasyonu | **210.000** (üretim, `appsettings.json`) → **D2**'nin maliyet notu |
| Saat soyutlaması | `IClock` var, `clock.UtcNow` kullanılıyor → C5 karşılanabilir, **D10** |
| `Notifications` kanalları | **tek**: `Email = 1` (`NotificationStatus.cs:20`) → S3'ün tek alternatifi |
| Son aktif Yönetici koruması | **var** ve yeniden kullanılabilir (`Staff.cs:119`, `StaffCommands.cs:104`, `:143`) → S4, C10 |
| Denetim kaydı altyapısı | **YOK**; `StaffDeactivated`ın alıcısı yok (kendi yorumu söylüyor) → **D12** |
| Yönetim uçlarının biçimi | `staff/{id}/role`, `/deactivate`, `/password-reset` → **D7** |
| Personel listesi sözleşmesi | **yedi alan**, ikinci adım durumu yok (`StaffCommands.cs:17`) → eklemeli alan gerekiyor |
| `0008`'in kapanmış kriteri | `evidence-0008.spec.ts:56` *"her satırda üç eylem"* diyor (`0008`/AC-1, AC-15) → dördüncü eylem onu kıracak — **bu ölçüm yanlıştı; bkz. D27** |

**İkinci CLARIFY turunda ölçülenler** (2026-10-15, `main` = `1e7c8d2`):

| Ne ölçüldü | Sonuç |
|---|---|
| Yönetim kökünün kapısı | grup düzeyinde `RequireAuthorization` (`AdminEndpoints.cs:40-42`) + rotaları TARAYAN host testi (`0006` C18/AC-23) → C14 |
| D7'nin iki ucu | `{admin}/me/password` ve `{admin}/me/two-factor` **ikisi de oturum ister** → **D13**'ün kilidi |
| Girişin başarı yolu | `window.location.assign(adminPath())` — **tam gezinme**, `<html lang dir>` sunucudan basıldığı için bilinçli (`AdminApp.tsx:82-87`) → **D14** |
| Kabuğun tek bilgi kaynağı | `GET /api/admin/me`; oturum çerezi yoksa `401` → `signedOut` → **giriş formu yeniden basılır** → **D14** |
| İstemci depolaması | `check:session` kapısı `localStorage`/`sessionStorage`'da oturum anahtarını tarıyor (`check-no-session-storage.mjs`) → ara durum uçta saklanamaz |
| Giriş yanıtının gövdesi | `200` + `CurrentSessionResponse(StaffId, DisplayName, Role, PreferredLocale, MustChangePassword)` (`SessionEndpoints.cs:83-88`) → C13, **D15** |
| Uçun hata davranışı | `sendJson` 2xx dışını `ApiError`'a çeviriyor → `4xx` normal bir adımı hata olarak bastırırdı (**D15**'te elenen alternatif) |
| Zamana bağlı kod kütüphanesi | **iki tarafta da YOK** — backend paketleri: EF · Npgsql · MailKit; frontend: react · intl-messageformat · fontsource |
| HMAC | **BCL'de var** ve zaten kullanılıyor (`System.Security.Cryptography`, `Session.cs:1`) → **D16** yeni backend bağımlılığı gerektirmiyor |
| Kare kod çizici | frontend'de **YOK** → **D17**'nin bedeli: bir frontend bağımlılığı |
| Ara durum için emsal | `Session` deseni: çerezte opak dize, satırda **SHA-256 özeti** (`Session.cs:30-33`) → **D19** |
| Süresi dolmuş oturum satırlarının temizliği | **YOK** (hiçbir temizlik yolu ölçülmedi) → **D19**'un saklama borcu bu spec'in yarattığı bir soru değil |
| İkinci adıma ait hata kodu | `IdentityErrors`'ta **15 kod, hiçbiri 2FA** → **D20** |
| Kilitlenme ipucu | **uçta** sayılıyor (`LOCK_HINT_THRESHOLD = 5`, `loginFormState.ts:47`) ve `LoginPage`'in **kendi state'inde** duruyor → **D21** |
| Sıfırlamanın emsali | `ResetPassword` ve `Deactivate` damgayı döndürüp oturumları **iptal ediyor** — *"iki katman, çünkü bu bir güvenlik işlemidir"* (`StaffCommands.cs:172-175`) → **D22** |
| `/me`'nin yanıtı | **yalnız claim'lerden** kuruluyor, **sıfır sorgu** (`AdminEndpoints.cs:82-90`) → **D26** |
| "üç eylem" iddiası | e2e'nin **başlığında** (`evidence-0008.spec.ts:56`); gövdesi eylem **saymıyor**. Gerçek sayım `StaffRowActions.test.tsx:71` ve o da üç `data-testid`'in **varlığını** denetliyor → **D27** |
| Giriş yapan e2e dosyası | **14** spec dosyası; `global-setup.ts` kurulum parolasını zaten **bir kez, idempotent** kuruyor → **D28** |
| Domain sözlüğü | `ikinci adım\|kurtarma kodu\|doğrulama sırrı\|iki adımlı\|2fa` taraması `docs/domain.md`'de **sıfır satır** → **D29** |

### Verilen kararlar — CLARIFY turu, 2026-10-15

Her madde bugünkü `main` (`c5143b6`) üzerinde **ölçülerek** verildi. Üçü (**D1**, **D4**,
**D12**) spec'i yazılı hâliyle uygulanamaz ya da tanımsız kılıyordu.

**D1 — C2 ikiye ayrıldı: doğrulama sırrı tek yönlü saklanamaz.** C2 *"doğrulama sırrı ve
kurtarma kodları geri döndürülebilir biçimde saklanmaz"* diyordu. Zamana bağlı kodu
doğrulamanın tek yolu **aynı sırla kodu yeniden üretmek**; tek yönlü özetlenmiş bir sırla
bu yapılamaz. Yani C2, R2 ve R3'ü imkânsız kılıyordu. Ölçüldü: depoda tek yönlü özetleme
var (`Pbkdf2PasswordHasher`, `IPasswordHasher`), **geri döndürülebilir şifreleme altyapısı
YOK** — `DataProtection`, `IDataProtector`, `Aes` taraması sıfır sonuç. Madde C2a (sır) ve
C2b (kurtarma kodları) olarak ayrıldı.

**D2 — Kurtarma kodları mevcut özetleyiciyle saklanır; doğrulama maliyeti yazılı.** C2b
`IPasswordHasher`'ı kullanır. **Bedel ölçüldü ve küçük değil:** iterasyon sayısı
yapılandırmadan geliyor ve üretim değeri **210.000** (`appsettings.json`). Girilen bir
kurtarma kodunu saklanan on özetin hepsiyle karşılaştırmak **on özetleme** demek — tek
girişte saniyeler. Bu yüzden kurtarma kodu doğrulaması **kendi iterasyon sayısını** alır
ve sayı yapılandırmadan okunur; kriter turu değeri belirler. *Elenen alternatif:* kodu
açık saklamak — yazılı bir yedek parolayı veritabanına koymak olurdu (§ Bilinen tuzaklar 1).

**D3 — Sır veritabanında açık metin durmaz; şifreleme anahtarı dağıtım
yapılandırmasından gelir.** Üç seçenek ölçüldü: (a) açık kolon — veritabanı erişimi
ikinci adımın tamamını atlatır, (b) uygulama düzeyinde şifreleme + anahtar
yapılandırmadan, (c) ASP.NET `DataProtection` anahtar halkası. Bugün hiçbiri kurulu değil;
yani **hangi yol seçilirse yeni altyapı** demek. Karar **(b)**: `Media` ve `Notifications`
zaten sırlarını (`Media__Storage__SecretKey`, SMTP parolası) dağıtım yapılandırmasından
alıyor ve `docs/conventions.md` § 11 bunu kural yapıyor; (c) ayrıca anahtar halkasının
nerede saklanacağı sorusunu açar. *Bedel:* anahtar döndürme (rotation) tanımsız kalıyor ve
bu **ayrı bir karardır** — kriter turunda ya kapsama girer ya § KAPSAM DIŞI'na yazılır.

**D4 — Ara durum (parola doğru, ikinci adım bekleniyor) nasıl taşınır.** Spec bu durumdan
hiç söz etmiyordu, oysa iki adımlı bir girişin **tamamı** bu duruma dayanıyor. Ölçüldü:
giriş ucu oturum token'ını **yalnız çereze** yazıyor ve gövdeye hiç koymuyor (`0006` C17 /
AC-29, `SessionEndpoints.cs:80`). Ayrıca yarım-oturum deseni zaten var:
`MustChangePasswordClaim` + `PasswordChangeGate` — gerçek bir oturum veriliyor ve bir kapı
yüzeyi kısıtlıyor. Karar: **ikinci adım geçilmeden oturum çerezi YAZILMAZ**; ara durum
kısa ömürlü, `HttpOnly`, kendi adı olan **ayrı bir çerezle** taşınır ve yalnız doğrulama
ucu onu okur. *Elenen alternatif:* `PasswordChangeGate` desenini kopyalamak — ucuz olurdu
(kapı, claim ve çerez mekanizması hazır) ama o yolda parola **tek başına** geçerli bir
oturum üretir ve kapıdaki tek kusur özelliğin tamamını geçersiz kılar. İki adımlı
doğrulamanın varlık nedeni tam olarak "parola tek başına yetmez"dir; o cümleyi bir kapının
doğruluğuna emanet etmiyoruz. *Bedel:* yeni bir çerez ve kısa ömürlü bir durum kaydı.

**D5 — Doğrulama ucu giriş ucunun hız sınırını paylaşır.** R8 yalnız kilitlenme
sayacından söz ediyordu. Ölçüldü: giriş ucu `RequireRateLimiting(RateLimitPolicy)` taşıyor
ve sınır yapılandırmadan geliyor (`LoginRequestLimit` 20 / 10 dk). Doğrulama ucuna ayrı
(ya da hiç) sınır koymak, parolayı ele geçirmiş saldırgana **kod denemek için ikinci bir
bütçe** verirdi. R8 bu kararla genişletildi.

**D6 — İlk girişte sıra: parola değişimi, sonra kurulum.** `0006` R10 ilk girişte parola
değişimini zorunlu kılıyor; zorunlu rolde ikinci adım kurulumu da gerekiyor (R7). Karar:
**önce parola değişimi, sonra kurulum.** *Gerekçe:* kurulum ekranı sırrı gösterir ve o
sırrın, parolayı sistem dışından alan kişinin elinde olması gerekir.

*Ve bu kararın kapatmadığı bir açık yazılı kalıyor:* ilk parola `0006` D1 gereği **sistem
dışından** (telefon/WhatsApp) iletiliyor. O parolayı ele geçiren biri, personelden önce
giriş yapıp **kendi doğrulama uygulamasını kaydedebilir**. İlk kurulum, tanımı gereği ilk
parola kanalından güçlü olamaz. Azaltıcı tek şey kayıt tutmadır (R12/**D12**):
beklenmeyen bir kurulum **görünür** olur. Bunu kapatmak `0006` D1'i değiştirmeyi
gerektirir ve bu **ayrı bir karardır** — kriter turunda ya kapsama girer ya kapsam dışına
gerekçesiyle yazılır.

**D7 — Kurulum ve sıfırlama uçları mevcut yüzeyin biçimini izler.** Ölçüldü: yönetim
uçları `POST {admin}/staff/{staffId}/role`, `.../deactivate`, `.../password-reset`
kalıbında. İkinci adımın sıfırlanması `.../two-factor-reset` olur; personelin kendi
kurulumu `{admin}/me/...` altına girer (`me/password` emsali).

**D8 — S2 KAPANDI: eşik 5 (ölçümle).** Mockup 8 diyor, `0006` D4 **5** diyor. Ölçüldü:
`IdentityOptions.DefaultFailedAttemptThreshold = 5` ve değer yapılandırmadan okunuyor;
mockup'ın 8'i **hiçbir yerde uygulanmadı**. İki metinden biri değişecekse maliyeti olmayan
taraf değişir — mockup metni. Karar bir ölçümdür, bu yüzden açık soru olarak bırakılmadı.

**D9 — Kurtarma kodu sayısı yapılandırmadan gelir.** Spec sayı vermiyordu. `0006` D4'ün
emsali: eşikler koda gömülmez, dağıtım yapılandırmasından gelir ve kriter **yapılandırılan
değeri** doğrular. Varsayılan **10**; kriter turu doğrulayacak.

**D10 — Saat kayması toleransı: ±1 adım.** C5 "dayanıklı olmalı" diyordu, sayı yoktu.
Karar: 30 saniyelik adımda **önceki, içinde bulunulan ve sonraki** adım kabul edilir
(±30 sn). Ölçüldü: saat soyutlaması var (`IClock`, `clock.UtcNow` — `Login.cs:73`), yani
C5'in "DateTime.Now kullanılmaz" kuralı zaten karşılanabilir. *Bedel:* kabul penceresi
90 saniyeye çıkar; D11 bunu tek kullanımla sınırlar.

**D11 — Aynı kod iki kez kabul edilmez (yeniden oynatma).** Spec'te yoktu: zamana bağlı
kod 30 saniye geçerli olduğu için aynı kod pencere içinde **tekrar** kullanılabilirdi.
Karar: kabul edilen son adım sayacı personel başına saklanır ve ona **eşit veya daha
küçük** bir adım reddedilir. Bu, kurtarma kodunun tek kullanımlığının (R5) zamana bağlı
koddaki karşılığıdır.

**D12 — Kayıt nereye yazılır: `identity` şemasında bir tablo.** R12 "kaydedilir" diyordu,
yer tanımsızdı. Ölçüldü: bu depoda **denetim kaydı altyapısı yok**.
`docs/architecture.md` § 1'in olay haritası `StaffDeactivated → … , Audit` diyor ama
`StaffEvents.cs` kendi yorumunda *"bugün hiçbir alıcısı yok"* yazıyor. Üç seçenek: (a)
entegrasyon olayı yayınlamak — **tüketicisi olmayan dördüncü olay** demek olurdu
(`0012b` D11 iki tanesini zaten borç olarak taşıyor), (b) yapılandırılmış log — kalıcı ve
sorgulanabilir değil, ayrıca kişisel veri log parametresine giremez (mimari kapı bunu
denetliyor), (c) `identity` şemasında bir kayıt tablosu. Karar **(c)**: C9 yeni
migration'ı `identity` şemasına sınırlıyor ve kayıt zaten `Identity`'nin verisidir (C1).
*Bedel:* tablo büyür ve saklama süresi tanımsız kalır — kriter turunda ya kapsama girer ya
kapsam dışına yazılır.

### Verilen kararlar — ikinci CLARIFY turu, 2026-10-15

*Her madde bugünkü `main` (`1e7c8d2`) üzerinde **ölçülerek** verildi. İkisi (**D13**,
**D15**) spec'i yazılı hâliyle uygulanamaz ya da güvensiz kılıyordu; biri (**D27**)
birinci turun bir ölçümünü düzeltiyor.*

**D13 — D4 yalnız kurulumu OLAN hesap için geçerli; kurulumsuz hesap kapılı bir oturum
alır.** D4, D6, D7 ve R7 birlikte **kilitliydi**: ölçüldü, `/api/admin` kökünün altındaki
her uç grup düzeyinde oturum istiyor ve bunu rotaları tarayan bir host testi denetliyor
(`0006` C18/AC-23, `AdminEndpoints.cs:40-42`). D7 kurulumu `{admin}/me/...` altına koydu,
D6 önce parola değişimini şart koştu — o da `{admin}/me/password`. Ama D4 "ikinci adım
geçilmeden oturum çerezi yazılmaz" diyor ve kurulumu olmayan personelin geçebileceği bir
ikinci adım **yok**. Sonuç: parola doğru → oturum yok → ne parola değiştirilebilir ne
kurulum yapılabilir → R7'nin *"önce kurar"*ı hiçbir yoldan yapılamaz. Karar: **kurulumu
olmayan hesapta gerçek oturum verilir** ve `PasswordChangeGate`'ten **sonra** çalışan
ikinci bir kapı yüzeyi `me/password`, `me/two-factor*` ve çıkışa daraltır; izin verilenler
**açıkça sayılır** (`PasswordChangeGate`'in kendi gerekçesi: tersini saymak bir sonraki
ucu sessizce açık bırakır). *Gerekçe:* kurulumu olmayan hesapta parola **bugün zaten tek
faktör**; kapılı oturum mevcut duruma göre hiçbir şeyi zayıflatmıyor ve D6'nın yazılı
açığı bunu kabul ediyor (*"ilk kurulum, ilk parola kanalından güçlü olamaz"*). D4'ün reddi
**kurulumu olan** hesap için aynen duruyor — asıl gerekçesi orada. *Elenen alternatif:*
parola değişimi ve kurulum uçlarını oturumsuz köke kopyalamak — aynı işlem iki yüzeyde
yaşar, C18'in rota taraması ikincisini **görmez** ve parola kuralı iki yerde durur
(`CLAUDE.md`, tek gerçek kaynağı). *Bedel:* iki kapı sırayla çalışır ve ikincisinin izin
listesi kriter turunda tek tek yazılır.

**D14 — İkinci adım giriş ekranının bir FAZIDIR; adım geçilmeden gezinme olmaz.** Spec
ekrana nasıl ulaşıldığını hiç söylemiyordu ve ölçüm yolun **kapalı** olduğunu gösterdi:
giriş başarısında uç `window.location.assign(adminPath())` yapıyor — tam gezinme, çünkü
panelin dili `<html lang dir>` ile sunucudan basılıyor (`AdminApp.tsx:82-87`) — ve
kabuğun tek bilgi kaynağı `GET /api/admin/me`. D4 gereği oturum çerezi yoksa o uç `401`
döner, kabuk `signedOut`'a düşer ve **giriş formu yeniden basılır**. Ara durumu istemcide
saklamak da yasak: `check:session` kapısı oturum benzeri anahtarları tarıyor. Karar:
oturum öncesi akış **tek ekrandır**, ikinci adım onun fazıdır ve başarılı doğrulamaya
kadar gezinme yoktur; *"Kullanıcı değiştir"* ara durumu **kendi ucuyla siler**. *Gerekçe:*
dil sorunu bu fazda yok — ikinci adım ekranı da giriş ekranı gibi oturum öncesidir ve
kurulum varsayılan dilinde basılır (R11). *Bedel, açıkça:* **F5 adımı kaybettirir** ve
personel parolayı yeniden girer; bunu kurtarmak yalnız oturum öncesi köke bir okuma ucu
daha açarak olurdu.

**D15 — Bekleyen giriş yanıtı `200` + ayrıştırıcılı gövdedir ve KİMLİK TAŞIMAZ.** Spec
yanıtın şeklinden hiç söz etmiyordu. Ölçüldü: giriş bugün `200` ile
`CurrentSessionResponse(StaffId, DisplayName, Role, PreferredLocale, MustChangePassword)`
döndürüyor (`SessionEndpoints.cs:83-88`). İkinci adım beklenirken aynı gövde dönse, elinde
**yalnız parola** olan kişi personelin adını ve rolünü öğrenirdi. Karar: iki dallı gövde —
bekleyen dal yalnız hangi adımın geldiğini söyler (`two_factor_required` ·
`two_factor_setup_required`) ve **hiçbir kimlik alanı taşımaz** (C13); tamamlanan dal
bugünkü gövdedir. *Gerekçe:* `4xx` olmaz, çünkü `sendJson` 2xx dışını `ApiError`'a
çeviriyor ve ekran **normal bir adımı hata olarak** basardı; `401` ayrıca kabuğu giriş
ekranına yollar (C19/AC-27). Bir adım, bir hata değildir. *Bedel:* `CurrentSession` tipi
ve `login()` imzası değişir; `0006`'nın giriş kriterleri 2FA'sız personel için aynen
doğru kalır ve ek kardeş kriter gerekir.

**D16 — Kodun parametreleri: HMAC-SHA1 · 6 hane · 30 saniye · Base32 · `otpauth://`.**
R2 "zamana bağlı bir kod" diyordu; hane sayısı, pencere, algoritma ve sırrın kodlanışı
**hiç yazılmamıştı** — "6 hane" yalnız mockup'ta, "30 saniye" yalnız D10'un gerekçesinde
geçiyordu. Karar RFC 6238 varsayılanları: **HMAC-SHA1**, **6 hane**, **30 sn**, Base32
sır ve
`otpauth://totp/{issuer}:{loginId}?secret=…&issuer={issuer}&digits=6&period=30`;
`issuer` dağıtım yapılandırmasından gelir (`docs/conventions.md` § 11). *Gerekçe:* yaygın
doğrulama uygulamalarının **tamamının** okuduğu tek küme bu; SHA256 ya da 8 hane bazı
uygulamalarda sessizce desteklenmiyor ve kusurun görüntüsü *"doğru kod reddediliyor"*
oluyor — D10'un yazılma sebebinin aynısı, ama teşhis edilemeyen hâli. Ölçüldü: iki tarafta
da kütüphane **yok**, ama HMAC BCL'de var ve `Session.cs` onu zaten kullanıyor; Base32
yirmi satır. **Yeni backend bağımlılığı gerekmiyor.** *Bedel:* küme dondurulmuştur (C12) —
sonradan değişirse kurulmuş **bütün** kayıtlar geçersizleşir.

**D17 — Sır metin olarak döner, kare kodu UÇ çizer; elle giriş her zaman görünür.** R3
*"personel bunu kendi doğrulama uygulamasına ekler"* diyordu ve bu hâliyle ölçülemezdi.
Karar: API `otpauth://` adresini **ve** Base32 sırrı **metin** olarak döner; kare kodu uç
o adresten yerel olarak çizer; elle giriş gizlenmiş bir bağlantının arkasında değil
**her zaman görünür** durur. *Gerekçe:* kare kod sunucuda üretilirse sır bir **görsele**
girer ve önbellek/log yüzeyine taşınır (§ Bilinen tuzaklar 2, C15). Elle girişin görünür
kalması hem erişilebilirlik hem RTL borcu, hem de kamerayı aynı cihazda kullanan
personelin tek yolu. *Bedel:* **bir frontend bağımlılığı** (kare kod çizici; ölçüldü,
depoda yok). Kendimiz yazmak hata düzeltme matematiği demek ve depoda emsali yok.

**D18 — Doğrulanmamış kurulumun ömrü var ve yeniden başlatmak YENİ sır üretir.** R3
doğrulanmamış kurulumun etkinleşmediğini söylüyordu; sırrın o aralıkta nerede durduğu ve
ne kadar yaşadığı yazılı değildi. Karar: bekleyen kurulum **kendi kısa ömürlü kaydıdır**,
süre yapılandırmadan gelir (varsayılan **10 dk**) ve kurulum yeniden başlatıldığında
**yeni bir sır** üretilir, eskisi düşer. *Gerekçe:* *"bir kez gösterilir"* kuralı, aynı
sır ikinci kez gösterilebildiği anda yalandır; süresiz bekleyen bir sır ise uyuyan
**ikinci bir kimlik bilgisidir**. Kurulum onayındaki yanlış koda **ayrı sayaç gerekmez**:
kendi bekleyen sırrını tahmin etmek saldırgana bir şey kazandırmaz — kontrol, süre ve
değiştirmedir. *Bedel:* sekmeyi kapatan personel kurulumu baştan yapar.

**D19 — Ara durum: `Session` deseni, 5 dakika, tek kullanım.** D4 "kısa ömürlü, `HttpOnly`,
ayrı bir çerez" demişti; **sayı yoktu** ve çerezin içinde ne taşındığı yazılı değildi.
Karar: çerezte **opak rastgele dize**, satırda **SHA-256 özeti** — `Session`'ın deseninin
aynısı (`Session.cs:30-33`); ömür yapılandırmadan, varsayılan **5 dk**; başarıda tüketilir,
*"kullanıcı değiştir"*de silinir; satır personel kimliğini ve deneme sayısını taşır.
*Gerekçe:* kendi kendini doğrulayan imzalı bir çerez, D3'ün sır için yeni kurduğu
altyapının **bir benzerini daha** ister (imza anahtarı) — hem de zaten sunucuda duran bir
değer için. *Bedel:* ikinci bir tablo. **Saklama/temizlik bu spec'in yarattığı bir soru
değil:** ölçüldü, süresi dolmuş `Session` satırlarını temizleyen hiçbir yol yok; kriter
turu bu borcu üstlenir ya da gerekçesiyle kapsam dışına yazar.

**D20 — Reddin iki kodu var, bir değil.** C6 *"`errorCode` döner"* diyordu; kod listesi
yoktu ve ölçüldü: `IdentityErrors`'ta 15 kod var, **hiçbiri** ikinci adıma ait değil.
Karar: `identity.two_factor_invalid` (yanlış · süresi dolmuş · yeniden oynatılmış kod;
**401**) ve `identity.two_factor_pending_expired` (ara durum düştü; **401**, ekran parola
fazına döner). Ayrıca `identity.two_factor_not_enrolled` ve
`identity.two_factor_already_enrolled` (**409**). *Gerekçe:* `docs/conventions.md` § 5'in
kendi mantığı — kod, **kullanıcının yapacağı şey farklıysa** ayrılır (*"taze kod yaz"* ile
*"parolanı yeniden gir"*) ve R10'un "süresi doldu" durumu tam bunu istiyor. **Yeniden
oynatmaya ayrı kod verilmez:** saldırgana yakaladığı kodun bir zamanlar geçerli olduğunu
söylerdi. Kilitlenmiş ikinci adım da `two_factor_invalid` döner — `0006`'nın suskunluk
kuralı (R3/AC-5, § Bilinen tuzaklar) aynen sürer.

**D21 — Kilitlenme ipucunun sayacı AKIŞA aittir, ekrana değil.** Ölçüldü: kilitlenme metni
uçta tutulan bir sayaçla basılıyor (`LOCK_HINT_THRESHOLD = 5`, `loginFormState.ts:47`)
çünkü sunucu kilitlenmeyi bilerek söylemiyor (`0006` AC-58) — ve sayaç `LoginPage`'in
kendi state'inde duruyor. İkinci adım ayrı bir ekran olsa ya da yeniden mount olsa sayaç
**sıfırlanır** ve R8'in "tek sayaç"ı uçta **iki** olurdu. Karar: başarısız deneme sayısı
oturum öncesi akışın malıdır; iki faz da aynı sayacı besler, eşik tek sabit kalır.
*Bedel:* `loginFormState` fazı öğrenir ve birim testleri büyür.

**D22 — Sıfırlama oturumları düşürür ve kalan kurtarma kodlarını geçersizleştirir.** R6
sıfırlamanın açık oturumlara etkisinden hiç söz etmiyordu. Ölçüldü: `ResetPassword` ve
`Deactivate` **hem** damgayı döndürüyor **hem** oturum satırlarını iptal ediyor ve yorumu
gerekçeyi yazıyor: *"iki katman, çünkü pasifleştirme bir güvenlik işlemidir ve tek bir
yolun unutulması onu sessizce etkisiz bırakır"* (`StaffCommands.cs:172-175`). Karar:
ikinci adım sıfırlaması **aynısını** yapar; kalan kurtarma kodları da düşer ve yeni
kurulum yeni küme üretir. *Gerekçe:* sıfırlamanın normal sebebi **kaybolan telefondur**;
o telefonun açık oturumunu yaşatmak `SecurityStamp`'in var olma sebebinin tam tersidir.
*Bedel:* sıfırlanan personel her yerden çıkmış olur — istenen şey bu.

**D23 — Kapatma: opsiyonel rolde parola + kod ile; zorunlu rolde yok.** S1'in cevabı iki
rol için "opsiyonel" diyor ama **kapatma** hiçbir maddede yoktu. Karar: opsiyonel roldeki
personel ikinci adımı **mevcut parolası ve geçerli bir kodla** kapatır; zorunlu rolde
kapatma yoktur ve tek yol R6'nın sıfırlamasıdır. Kapatma **kaydedilir** (R12'nin dördüncü
olayı). *Gerekçe:* yalnız açık oturumla kapatılabilen bir anahtar, çalınmış bir oturumu
faktörün **kalıcı** olarak kaldırılmasına çevirir; `0006` R11'in "mevcut parolasını
vererek" deseni zaten bu sınıf işlemler için kurulmuştu.

**D24 — Kurtarma kodları yenilenebilir ve kalan sayı görünür.** R4/R5 yeni küme almaktan
hiç söz etmiyordu. Karar: personel kümeyi kendi ekranından, oturum **ve** taze bir kodla
yeniler; yenileme kalanların **hepsini** düşürür, yeni küme **bir kez** gösterilir ve
ekran kalan sayıyı yazar. *Gerekçe:* yenileme olmadan ilk kullanım tek yönlü bir geri
sayım başlatır ve tek çıkışı R6'nın Yönetici sıfırlamasıdır; D9'un varsayılan 10'u bunu
yalnız geciktirir. *Bedel:* bir uç ve bir ekran bölgesi — ikisi de kurulum gösteriminin
aynısını kullanır.

**D25 — Personel listesi tek eklemeli boolean kazanır: `twoFactorEnrolled`.** Birinci tur
"eklemeli bir alan" demiş, hangisi olduğunu bırakmamıştı. Karar: **üç durumlu değil, tek
boolean.** *Gerekçe:* zorunluluğu **rol** söylüyor ve satır rolü zaten taşıyor
(`StaffListItem`, yedi alan); ikinci bir alan S1'in cevabını **API sözleşmesine
dondururdu** ve zorunluluk politikası değiştiğinde sözleşme de değişmek zorunda kalırdı.
"Zorunlu ama kurmamış" rozetini ekran, elindeki iki alandan kurar.

**D26 — `/me` ikinci adım durumunu KAYNAKTAN okur, claim'den değil.** Mockup üç panelde
"2FA açık" rozeti çiziyor ve opsiyonel rolün aç/kapat ekranı da bu bilgiye dayanıyor.
Ölçüldü: `/me` yanıtını **yalnız claim'lerden** kuruyor ve **sıfır sorgu** yapıyor
(`AdminEndpoints.cs:82-90`). Karar: alan eklenir ve kaynaktan okunur. *Gerekçe:* `0006`
C16'nın kendi ilkesi — *"hesabın durumu ve rolü her istekte KAYNAKTAN okunur"*; girişte
pişirilmiş bir claim, kurulumu biten personele oturumun kalan otuz dakikası boyunca
"kurulmamış" derdi. *Bedel:* `/me` bir indeksli okuma kazanır.

**D27 — BİRİNCİ TURUN ÖLÇÜMÜ YANLIŞTI: dördüncü eylem hiçbir testi kırmıyor.** Birinci
tur *"`0008`'in kapanmış bir kriteri KIRILACAK"* diye yazdı ve dayanak olarak
`evidence-0008.spec.ts:56`'yı gösterdi. Bugün ölçüldü: o satır testin **başlığı** ve
gövdesi **eylem saymıyor** — yalnız tablonun görünürlüğüne ve `staff-row-self` sayısının
`1` olduğuna bakıyor. Gerçek sayım iddiası `StaffRowActions.test.tsx:71`'de ve o da üç
`data-testid`'in **varlığını** denetliyor (`getByTestId` + `not.toBeNull()`), **sayı
eşitliğini değil** — dördüncü düğme onu da kırmaz. `0008`/AC-1 ise üç eylemi sayıyor ve
dördüncüsü eklenince **hâlâ doğrudur** (ekleme iddiayı zayıflatmıyor). Karar: DoD maddesi
kalır ama **gerçekten borçlu olduğu şeye** yazılır — `0008-ac1.png` görsel kanıtı
yenilenir (o bir **tablo** ekran görüntüsü ve dördüncü düğmeyle değişir), iki bayat başlık
güncellenir ve PR'da AC-1'in iddiasının **dokunulmadığı** gösterilir. *Gerekçe:* kırmızı
test öngören bir DoD kutusu yeşil test getirince, okuyana kutuya güvenmemeyi öğretir —
`0012b`'nin **D37**'sinde aynı desen bir kez ödendi.

**D28 — e2e kurulumu ikinci adımı BİR KEZ kurar; aksi hâlde on dört dosya birlikte
kırmızıya düşer.** Ölçüldü: **14** e2e spec dosyası giriş yapıyor ve hepsi kurulum
Yöneticisini kullanıyor; `global-setup.ts` kurulum parolasını zaten **bir kez,
idempotent** biçimde çalışır hâle getiriyor ve gerekçesi yazılı (her testin kendi başına
denemesi hesabı kilitliyordu). D30 Yönetici'yi zorunlu kıldığı için R7'nin kapısı
depodaki **her** e2e akışının önüne geçiyor. Karar: `global-setup` kurulum hesabının
ikinci adımını da bir kez kurar ve sırrı dışa verir; giriş yardımcıları kodu `node:crypto`
HMAC'ıyla hesaplar (**yeni bağımlılık yok**). Bu, **bu spec'in kriterine ve DoD'una**
yazılır. *Gerekçe:* "sonra bakarız", on dört dosyayı aynı anda kırmızıya çeviren tek
karardır.

**D29 — Üç domain terimi `docs/domain.md`'ye eklenir; DoD'un "yeni terim yok" maddesi
yanlıştı.** Ölçüldü: `ikinci adım`, `kurtarma kodu`, `doğrulama sırrı`,
`iki adımlı` ve `2fa` taraması `docs/domain.md`'de **sıfır satır** veriyor. Karar: sözlüğe
üç satır eklenir — **İkinci Adım** (`TwoFactorEnrollment`), **Kurtarma Kodu**
(`RecoveryCode`), **Doğrulama Sırrı** (`TwoFactorSecret`) — ve DoD maddesi buna döner.
*Gerekçe:* sözlüğün işi Türkçe terim ile kod adının ayrışmasını önlemek (C7, Yasak
Listesi 22); üç yeni kavramın sözlüksüz girmesi ayrışmanın tam olarak başladığı yerdir.

**D30 — S1 KAPANDI: zorunluluk Yönetici rolünde; diğer iki rolde opsiyonel.** Yönetici,
personel ekleyip rol değiştirebilen tek roldür (`0006` R9) ve o hesabın ele geçmesi
sistemin tamamını verir. Satış Danışmanı ve İçerik Editörü kendi isteğiyle açabilir
(**R15** kapatmayı da onlara verir). Ölçüldü: roller gerçekten **üç** ve "Sahip" yok
(`StaffRole.cs:12`) — mockup'ın *"Sahip ve yönetici"* metni uygulanabilir bir role
karşılık gelmiyor ve C3 ile çelişkisi doğrulandı. *Bedel, açıkça:* danışman hesabı tek
parolayla korunmuş kalır ve o hesap da talep kişisel verilerini görür; "tüm roller
zorunlu" demek tek satırlık bir değişikliktir.

**D31 — S3 KAPANDI: kurtarma kodları yalnız ekranda, bir kez gösterilir.** E-posta ile
gönderilmez. *Gerekçe:* kurtarma kodunu e-postaya göndermek ikinci adımı **e-posta
hesabının** güvenliğine indirger ve o hesap bizim kontrolümüzde değil. Ölçüldü:
`Notifications`'ın tek kanalı var (`Email = 1`, `NotificationStatus.cs:20`) — yani
"gönderelim" denirse tek seçenek tam olarak bu. *Bedel:* personel kodları kaydetmezse
kaybeder ve R6'nın sıfırlama yoluna düşer; **D24** bu yolu yenilemeyle kısaltıyor.

**D32 — S4 KAPANDI: Yönetici kendisi dâhil sıfırlar; tek Yönetici kilitlenirse son kapı
kurulum hesabıdır.** Kendi adımını sıfırlamak için zaten giriş yapmış olmak gerekir,
dolayısıyla telefonunu kaybeden **tek** Yönetici kilitlenir ve çıkış yolu `0006` D2'nin
kurulum hesabıdır. *Gerekçe:* ikinci bir kurtarma kapısı (örneğin e-posta ile sıfırlama)
D31'in gerekçesini delerdi. Ölçüldü: dayandığı iki mekanizma var — son aktif Yönetici
koruması (`Staff.cs:119`, `StaffCommands.cs:104`, `:143`) ve kurulum hesabı yolu
(`IdentityModule.cs:36`, uygulama parola yapılandırılmadan hiç başlamıyor). *Bedel
2026-10-15'te arttı:* ADR `0011` depoda yayınlanmış üç geliştirme parolasını koşulsuz
reddediyor, yani "son çıkış kapısı" gerçek bir yapılandırma sırrı gerektiriyor. Kabul
edilmezse R7 *"en az iki Yönetici zorunlu"* hâline gelir ve bu **ayrı bir karardır**.

**D33 — S5 KAPANDI: ikinci adım ekranına geçmenin parolayı ifşa etmesi kabul edilir.**
Alternatif, yanlış parolada da sahte bir kod ekranı göstermekti; bu personelin kendi
hatasını anlamasını engeller ve destek yükü üretir. Parolanın doğruluğunu bilmek, ikinci
adım olmadan saldırgana bir şey kazandırmaz — ikinci adımın varlık nedeni tam budur.
Bedeli **üç** mekanizma sınırlıyor: paylaşılan kilitlenme sayacı (R8), paylaşılan hız
sınırı (**D5**) ve **D15** — bekleyen yanıt kimlik taşımadığı için saldırgan "parola
doğru"dan personelin kim olduğunu bile öğrenmiyor.

### CLARIFY turlarında ölçülen bedel: kapanmış bir spec'in yüzeyi etkilenecek

**`0008`'in yüzeyi iki yerden değişiyor, `0006`'nın yüzeyi bir yerden — üçü de adıyla
yazılı. İkinci maddenin ilk ölçümü YANLIŞTI ve düzeltilmiş hâli burada (D27).**

1. **Personel listesi sözleşmesi eklemeli bir alan kazanır.** Ölçüldü: `StaffListItem`
   yedi alan taşıyor (`StaffCommands.cs:17`) ve ikinci adım durumu **yok**. Mockup üç
   panelde "2FA açık" rozeti çiziyor; rozet veri olmadan basılamaz. (`0012c` D50'nin
   emsali: kapanmış sözleşmeye eklemeli alan.) Alanın şekli **D25**'te: tek boolean.
2. **`0008`'in görsel kanıtı yenilenir; kapanmış kriteri KIRILMIYOR.** Birinci tur bunu
   *"kriter kırılacak"* diye yazmıştı; **D27** ölçtü ve öyle değil.
   `evidence-0008.spec.ts:56` bir **başlıktır** ve gövdesi eylem saymıyor; gerçek sayım
   `StaffRowActions.test.tsx:71`'de ve o da üç `data-testid`'in **varlığını** denetliyor.
   Dördüncü eylem ikisini de kırmaz ve `0008`/AC-1 doğru kalır. Borç şu: **tablo ekran
   görüntüsü `0008-ac1.png` yenilenir** (dördüncü düğme onu değiştirir) ve iki bayat
   başlık güncellenir.
3. **`0006`'nın giriş yanıtı yeni bir dal kazanır.** Ölçüldü: giriş `200` +
   `CurrentSessionResponse` döndürüyor (`SessionEndpoints.cs:83-88`). **D15** gereği
   bekleyen dal kimlik taşımayan **ayrı** bir gövdedir; `0006`'nın giriş kriterleri
   2FA'sız personel için aynen doğru kalır ama tipin kendisi (`CurrentSession`,
   `login()`) değişir ve uygulama PR'ı bunu adıyla yazar.

### Bilinen tuzaklar

1. **Kurtarma kodunu tek kullanımlık yapmamak.** Yeniden kullanılabilir kod, yazılı bir
   yedek paroladır ve ikinci adımın değerini sıfırlar.
2. **Sırrı yanıtta veya logda göstermek.** `0006` C6 parolayı yasakladı; sır ve kurtarma
   kodu aynı sınıftır (C2).
3. **Tek Yöneticinin kilitlenmesi.** Sıfırlama yetkisi yalnızca Yöneticideyse ve tek Yönetici
   telefonunu kaybederse sistem **tamamen kapanır**. `0006` D3 "son aktif Yönetici korunur"
   kuralını kurdu; bu, onun ikinci adım tarafındaki karşılığıdır (**D32**).
4. **Saat kayması.** Zamana bağlı kod, sunucu ile cihaz saati arasındaki küçük farka
   dayanıklı olmalıdır; yoksa doğru kod reddedilir ve personel sebebini anlamaz (C5).
5. **Kilitlenme sayacını ayırmak.** İkinci adım için ayrı bir sayaç tutmak, saldırganın
   parolayı doğruladıktan sonra kodu sınırsız denemesine yol açar (R8). Sayacın **uçtaki**
   yarısını ayırmak da aynı hatanın yarısıdır (**D21**).
6. **Ara durumu oturum sanmak.** Parola doğruysa "yarım oturum" vermek ucuzdur ve deseni
   depoda hazırdır (`PasswordChangeGate`), ama o yolda parola **tek başına** geçerli bir
   oturum üretir ve kapıdaki tek kusur özelliğin tamamını geçersiz kılar (**D4**). Kapılı
   oturum **yalnız** kurulumu olmayan hesapta meşrudur (**D13**) — orada parola bugün
   zaten tek faktör.
7. **Bekleyen yanıta kimlik koymak.** "Hangi personel bekliyor" bilgisini gövdeye koymak
   ekranı kolaylaştırır ve elinde yalnız parola olan kişiye personelin adını, rolünü ve
   dilini verir (**D15**, C13).
8. **Parametreleri sonradan değiştirmek.** Hane sayısı, pencere veya algoritma
   değiştirildiğinde kurulmuş **bütün** kayıtlar sessizce geçersizleşir ve kusurun
   görüntüsü *"doğru kod reddediliyor"* olur (**D16**, C12).

---

## Açık sorular

**Yok.** Beş sorunun hepsi kapandı ve gerekçeleri § Context'teki kararlarda yaşıyor
(tek gerçek kaynağı — burada ikinci kez yazılmıyor):

| Soru | Kapanış | Karar |
|---|---|---|
| ~~**S1.** Hangi roller için zorunlu?~~ | **Yönetici** zorunlu; Satış Danışmanı ve İçerik Editörü **opsiyonel** | **D30** |
| ~~**S2.** 5 deneme mi 8 mi?~~ | **5** — ölçümle (`IdentityOptions`); mockup'ın 8'i hiçbir yerde uygulanmadı | **D8** |
| ~~**S3.** Kurtarma kodları nasıl verilir?~~ | **Ekranda bir kez**; e-posta ile gönderilmez | **D31** |
| ~~**S4.** Yönetici kendi adımını sıfırlayabilir mi?~~ | **Evet, kendisi dâhil**; tek Yönetici telefonunu kaybederse son kapı kurulum hesabıdır | **D32** |
| ~~**S5.** İkinci adım ekranı parolayı ifşa ediyor; kabul mü?~~ | **Kabul** — bedeli üç mekanizma sınırlıyor (R8 · D5 · D15) | **D33** |

S2 birinci CLARIFY turunda, diğer dördü ikinci turda kapandı (Takım Yöneticisi onayı
2026-10-15). **Kabul kriterlerini bekleten bir karar kalmadı;** kriterler sıradaki turda
yazılacak.

---

## Acceptance Criteria

> **Sıradaki tur kriter turudur** — açık soru kalmadı, kararlar donmuş (D1–D33) ve kriter
> sayısı bu kartta bilerek **0**. Kümeler iki turun kararlarıyla büyüdü; en az şunları
> kapsayacak:
>
> **Kurulum.** Sır üretimi · doğrulanmamış kurulumun etkinleşmemesi · bekleyen kurulumun
> süresi ve yeniden başlatmanın **yeni sır** üretmesi (D18) · kare kod **ve** elle giriş
> metninin birlikte sunulması (D17) · `otpauth://` adresinin parametreleri (D16).
>
> **Giriş akışı.** Parola doğru + kurulumu olan hesap → **oturum çerezi yazılmaz** (D4) ·
> bekleyen yanıtın **kimlik taşımaması** (D15/C13) · ara durumun ömrü, tek kullanımlığı ve
> *"kullanıcı değiştir"*le silinmesi (D14/D19) · ikinci adım geçilmeden hiçbir yönetim
> isteğinin çalışmaması (R13).
>
> **Kod doğrulama.** Geçerli · geçersiz · penceresi dolmuş · **yeniden oynatılmış** kod
> (D11) · saat kayması toleransının ±1 adım olması (D10) · iki ayrı hata kodu ve
> karşılıkları (D20) · paylaşılan kilitlenme sayacı ve **paylaşılan hız sınırı** (R8/D5).
>
> **Zorunluluk ve kapı.** Zorunlu roldeki kurulumsuz personelin kapılı oturumu ve izin
> verilen üç yol (D13) · ilk girişte sıranın **parola → kurulum** olması (D6) · opsiyonel
> rolde kurulumun kendi isteğiyle açılması (D30).
>
> **Kurtarma ve sıfırlama.** Kurtarma kodunun tek kullanımlığı · kümenin yenilenmesi ve
> kalanların düşmesi (D24) · kalan sayının görünmesi · sıfırlamanın **oturumları
> düşürmesi** ve kalan kodları geçersizleştirmesi (D22) · opsiyonel rolde kapatmanın
> parola + kod istemesi (D23) · son aktif Yönetici koruması ve kurulum hesabı yolu (D32).
>
> **Sızıntı.** Sırrın ve kurtarma kodunun yanıtta, logda ve hata metninde bulunmaması
> (C2) · sırrın veritabanında **açık metin durmaması** (D3) · sırrın sunucuda **görsele
> gömülmemesi** (C15).
>
> **Yüzey ve ekran.** `twoFactorEnrolled` alanının listede (D25) ve `/me`'de **kaynaktan**
> okunması (D26) · üç durum + *süresi doldu* + *kilitlenmiş hesap* (R10) · ipucu
> sayacının iki fazda **tek** olması (D21) · çok dil ve RTL (R11).
>
> **Kayıt.** Kurulum · kapatma · sıfırlama · kurtarma kodu kullanımı (R12/D12) ve
> başarısız denemelerin **tutulmaması**.

---

## Definition of Done

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
      (Beş sorunun hepsi: **S2** → D8; **S1 · S3 · S4 · S5** → **D30–D33**, 2026-10-15.)
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0013-ac<n>.png`), hata ve
      kilitlenme durumları dâhil, RTL için Arapça görüntü de var.
- [ ] Sırrın ve kurtarma kodlarının yanıtta, logda ve hata metninde görünmediği testle
      gösterildi (C2).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — `Identity` sınırları korundu.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `identity` şemasına dokunuyor.
- [ ] Mockup'taki iki çelişkili metin — *"Sahip hesabı"* (**D30**: rol yok, zorunluluk
      **Yönetici**'de) ve *"8 hatalı deneme"* (**D8**: eşik **5**) — tasarım tarafında
      güncellendi veya PR'da **açıkça** uyumsuz olarak not edildi.
- [ ] **`0008`'in görsel kanıtı yenilendi ve kriterinin iddiası dokunulmadan kaldı**
      (**D27**, birinci turun ölçümünü düzeltir): `0008-ac1.png` dördüncü eylemle
      yeniden üretildi · `evidence-0008.spec.ts:56` ve `StaffRowActions.test.tsx:71`'in
      *"üç eylem"* diyen **başlıkları** güncellendi · PR'da **hiçbir testin kırılmadığı**
      ve `0008`/AC-1'in hâlâ doğru olduğu gösterildi. Kırmızı test **beklenmiyor**;
      beklenirse ölçüm yine yanlış demektir.
- [ ] **`0008`'in personel listesi sözleşmesine eklenen alan PR'da adıyla yazıldı**
      (`StaffListItem` + yönetim listesi yanıtı); rozet veri olmadan basılamaz.
- [ ] **Doğrulama sırrının veritabanında açık metin DURMADIĞI** testle gösterildi ve
      şifreleme anahtarının dağıtım yapılandırmasından geldiği doğrulandı (**D3**).
      Anahtar döndürme kapsamda değilse § KAPSAM DIŞI'na gerekçesiyle yazıldı.
- [ ] **Aynı zamana bağlı kodun iki kez kabul edilmediği** testle gösterildi (**D11**),
      saat kayması toleransı ±1 adım olarak ölçüldü (**D10**).
- [ ] **Kurtarma kodu doğrulamasının maliyeti ölçüldü** ve iterasyon sayısının
      yapılandırmadan geldiği gösterildi (**D2**) — on özetlemenin üretim değeriyle
      saniyeler sürdüğü hesabı PR'da yazılı.
- [ ] **Kayıt tablosunun yalnız `identity` şemasına dokunduğu** ve kaydın kurulum,
      **kapatma**, sıfırlama ve kurtarma kodu kullanımını kapsadığı gösterildi
      (**D12**/R12); başarısız denemelerin kayda **girmediği** ve saklama süresi kapsamda
      değilse gerekçesiyle kapsam dışına yazıldığı gösterildi.
- [ ] **Kurulumsuz hesabın kapısı tek tek sınandı** (**D13**): izin verilen üç yol
      (kendi parolası, kendi kurulumu, çıkış) çalışıyor ve **dördüncü bir yönetim isteği
      reddediliyor**; `0006` C18/AC-23'ün rota taraması hâlâ yeşil, yani oturumsuz köke
      yönetim ucu **eklenmedi**.
- [ ] **Bekleyen giriş yanıtının kimlik taşımadığı** testle gösterildi (**D15**/C13):
      gövdede ad, rol, dil ve kimlik **yok**. `CurrentSession` tipinin ve `login()`
      imzasının değiştiği PR'da adıyla yazıldı.
- [ ] **İkinci adımın giriş ekranının bir fazı olduğu** görsel kanıtla gösterildi
      (**D14**): adım geçilmeden gezinme olmuyor, *"Kullanıcı değiştir"* ara durumu
      siliyor ve **F5'in adımı kaybettirdiği** bedel PR'da açıkça yazılı.
- [ ] **Parametre kümesi gerçek bir doğrulama uygulamasıyla denendi** (**D16**):
      üretilen `otpauth://` adresi en az bir yaygın uygulamada okundu, onun ürettiği kod
      kabul edildi ve kanıt PR'da. *Neden:* hane/pencere/algoritma kümesinin yanlışlığı
      yalnız "doğru kod reddediliyor" olarak görünür ve hiçbir birim testi bunu yakalamaz.
- [ ] **Sırrın sunucuda görsele gömülmediği** (C15/**D17**) ve **elle giriş metninin her
      zaman görünür** olduğu gösterildi — kare kod uçta çizildi, görsel kanıt RTL'de de
      var.
- [ ] **Ara durumun ömrü, tek kullanımlığı ve silinmesi** testle gösterildi (**D19**);
      çerezin `HttpOnly` olduğu ve satırda tokenin **özetinin** durduğu doğrulandı.
      Saklama/temizlik kapsamda değilse gerekçesiyle kapsam dışına yazıldı.
- [ ] **İki ayrı hata kodu ve karşılıkları** testle gösterildi (**D20**); **kilitlenmiş
      ikinci adımın ayırt edilemez** kaldığı (`0006` R3/AC-5 suskunluğu) sınandı.
- [ ] **Sıfırlamanın açık oturumları düşürdüğü ve kalan kurtarma kodlarını
      geçersizleştirdiği** testle gösterildi (**D22**).
- [ ] **Kapatma ve yenileme sınandı** (**D23**, **D24**): kapatma mevcut parola **ve**
      geçerli kod istiyor, zorunlu rolde kapatma yok; yenileme kalan kodların **hepsini**
      düşürüyor ve kalan sayı ekranda görünüyor.
- [ ] **`twoFactorEnrolled` iki yüzeyde de doğru** (**D25**, **D26**): personel
      listesinde eklemeli alan olarak var ve `/me` onu **kaynaktan** okuyor — kurulumu
      biten personelin aynı oturumda doğru durumu gördüğü testle gösterildi.
- [ ] **e2e kurulumu ikinci adımı bir kez kuruyor** (**D28**): `global-setup` kurulum
      hesabını kaydediyor, giriş yardımcıları kodu `node:crypto` ile üretiyor ve **giriş
      yapan on dört spec dosyasının hepsi yeşil**. Yeni e2e bağımlılığı eklenmedi.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi.
- [ ] <!-- DoD:T7 --> **Üç yeni domain terimi `docs/domain.md`'ye eklendi** (**D29**):
      İkinci Adım (`TwoFactorEnrollment`) · Kurtarma Kodu (`RecoveryCode`) · Doğrulama
      Sırrı (`TwoFactorSecret`). Kod adları sözlükteki karşılıklarıyla birebir aynı.
- [ ] <!-- DoD:T8 --> `docs/architecture.md` § Karar Defteri güncellendi (**2FA
      kapsamda** satırı). *"Oturum öncesi ara durum"* satırı ikinci CLARIFY turunda
      zaten eklendi (**D13**/**D19**); kalan borç kapsam satırıdır.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

<!--
İş bittikten SONRA doldurulur. Amaç suçlu bulmak değil, sürecin nerede sızdırdığını görmek.
Dürüst doldurulmayan scorecard işe yaramaz — abartılmış iyi sayılar en pahalı yalandır.

Sayılar ancak TANIMLI oldukları sürece karşılaştırılabilir. Tanımsız bir sayı, spec'ten
spec'e farklı şey ölçer ve birikince eğilim değil gürültü üretir. Bu yüzden her ölçütün
neyi saydığı satırında yazılıdır; bir ölçüt belirsiz geldiyse sayıyı tahmin etme,
tanımın netleştirilmesini öner.
-->

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | evet / hayır | R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır — düşük sayı iyi spec değil, karşılaştırılacak tabanı olmayan spec demektir. |
| **Kriter sayısı: önce → bugün** | → | O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. İkisi arasındaki fark, revizyon sayısının söylemediği şeyi söyler: `0008` bu ölçümde **2 → 33** çıktı ve kartı revizyon **1** yazıyordu. |
| **Spec revizyon sayısı** | | Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? Açıklayıcı not eklemek de sayılır: nete ihtiyaç duyan kural, eksik yazılmış kuraldır. Intent, Context, kapsam dışı gerekçeleri ve SCORECARD değişiklikleri **sayılmaz**. Yüksekse spec eksik yazılmıştır. **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3) — ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve kriter sayısının iki katına çıkması bunun en net cevabıdır. Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. **`0` ile `—` aynı şey değildir:** `0`, gözden geçirildi ve hiç tur dönmedi demektir; gözden geçirme hiç yapılmadıysa `—` yazılır. İkisini aynı göstermek, ölçülmemiş işi temiz işmiş gibi gösterir. **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). Bu, "gözden geçirildi"yi var olan bir kutuya bağlar: kural eskiden kimin gözden geçirdiğini söylemiyordu ve üç kart (`0002`, `0007`, `frontend-002`) tam o boşlukta `0` yazdı. |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz — kendi kodunu okuyan kişi kendi körlüğünü paylaşır. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. **Ölçtüğü şey: spec ve kod ne kadar açık yazıldı.** |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı — kendi testi kırmızı verdiği, kendi kanıtına baktığı veya kendi sondası yakaladığı için. Dış bulguyla aynı sütunda toplanmaz: ikisi **farklı şey** ölçer. **Ölçtüğü şey: yazar kendi işini ne kadar sorguladı.** Sıfır, "hiç hata yapmadım" değil "hiçbirini kendim bulmadım" demektir ve dış bulgu yüksekse bu bir uyarıdır. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
