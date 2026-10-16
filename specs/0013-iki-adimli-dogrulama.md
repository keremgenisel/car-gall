# 0013 — İki Adımlı Doğrulama

| | |
|---|---|
| **Durum** | **Onaylandı** (2026-10-15) |
| **Sahip** | Developer |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Identity` · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0006` (oturum ve roller — tamamlandı) · `0009` (bildirim — tamamlandı) |
| **Dayanak** | ADR 0003 unsur 2; Ek 1 sıra #6 |
| **Karar bekliyor** | **Yok.** Beş sorunun hepsi kapandı: **S2** birinci CLARIFY turunda (**D8**, ölçümle), **S1 · S3 · S4 · S5** ikinci turda (**D30–D33**, Takım Yöneticisi onayı 2026-10-15). |
| **Kararlar** | **D1–D50**: iki CLARIFY turu bugünkü `main` üzerinde **ölçülerek** verdi (**D1–D12** → `c5143b6`, **D13–D33** → `1e7c8d2`), **D34–D47** bu kriter turunda (uygulama planının ölçümleri, Takım Yöneticisi onayı 2026-10-15), **D48–D50** dış gözden geçirmenin bulgularında (2026-10-16). Yedisi spec'i yazılı hâliyle **uygulanamaz** kılıyordu. |
| **Kriter sayısı** | **63** |

> **R/C/AC bu PR'da `main`'e giriyor ve orada donuyor** (ADR `0008` / K2, `0012b`
> **D38**'in donma anı cümlesi). Bu commit'ten sonra Requirements, Constraints veya
> Acceptance Criteria'da yapılan her değişiklik **sayılan bir revizyondur** ve kendi
> commit'ine girer.
>
> **Kriter turu, 2026-10-15.** Altmış üç kriter yazıldı ve uygulama planının ölçümleri
> on dört karar daha ekledi (**D34–D47**). Dördü spec'i yazılı hâliyle uygulanamaz ya
> da eksik ölçülmüş kılıyordu: **D34** (D28 yalnız e2e'yi saymıştı, `Host.Tests`'in
> dokuz zinciri de kırılıyor), **D36** (D13'ün beyaz listesi kabuğun ilk isteğini
> kapatıyordu), **D37** (D32'nin *"son çıkış kapısı"* bugün uygulanamaz) ve **D47**
> (D19'un deneme sayısı, R8'in yasakladığı ikinci eşiğe dönüşmek üzereydi).
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
- **X7.** **Şifreleme anahtarının döndürülmesi (rotation)** — *neden:* döndürme, kurulmuş
  **her** kaydın yeniden şifrelenmesini ve iki anahtarın bir süre bir arada yaşamasını
  ister; yani bir anahtar sürümü alanı, bir geçiş yolu ve bir geri alma planı. Bu bir
  bakım işi kartıdır, ikinci adımın kurulumu değil (**D3**'ün bıraktığı borç, **D39**).
- **X8.** **Kayıt tablosunun ve ara durum satırlarının saklama/temizlik politikası** —
  *neden:* ölçüldü, süresi dolmuş `Session` satırlarını temizleyen bir yol **bugün de
  yok**; yani borç bu kartın yarattığı bir soru değil ve temizlik üç tabloyu birden
  kapsayan ayrı bir karardır. Ara durum satırı başarıda ve *"kullanıcı değiştir"*de
  **silindiği** için yalnız terk edilmiş satırlar birikir (**D12** ve **D19**'un
  bıraktığı borç, **D39**).

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

### Verilen kararlar — kriter turu, 2026-10-15

*Her madde uygulama planının bugünkü `main` + #74 ağacı üzerindeki ölçümünden geldi
(Takım Yöneticisi onayı 2026-10-15). Dördü (**D34**, **D36**, **D37**, **D47**) spec'i
yazılı hâliyle uygulanamaz ya da eksik ölçülmüş kılıyordu.*

**D34 — D28'in ölçümü eksikti: `Host.Tests`'in dokuz zinciri de kırılıyor.** D28 giriş
yapan **14 e2e** dosyasını saydı ve yalnız onları. Ölçüldü: `Host.Tests`'te **dokuz**
dosya `/api/identity/sessions`'a kurulum **Yöneticisi** ile giriyor ve ardından yönetim
uçlarını çağırıyor — `AracKaydiZinciri`, `AracMedyasiZinciri`, `DuyuruYazimiZinciri`,
`MedyaYuklemeZinciri`, `OturumBoruHatti`, `SubeliPersonelBoruHatti`, `TopluEylemZinciri`,
`YayinZinciri`, `GirisHizSiniri`. D30 Yönetici'yi zorunlu kıldığı ve D13 kurulumsuz
hesabı kapıladığı için dokuzu da `403` alır. Karar: D28'in kardeşi olarak
`tests/Host.Tests` içinde **ortak bir kurulum yardımcısı** (ikinci adımı kur + kod üret)
yazılır ve dokuz dosya onu kullanır; kriteri **AC-63**. *Gerekçe:* "sonra bakarız" aynı
bedeli ikinci kez ödetir ve dokuz dosyanın aynı anda kırmızıya düşmesi, kusuru değil
kararı gösterir. *Bedel:* dokuz test dosyası bu kartta değişir.

**D35 — Kurulum durumu istek başına KAYNAKTAN çözülür ve claim'e yazılır.** D26 *"`/me`
bir indeksli okuma kazanır"* diyordu. Ölçüldü: aynı bilgiye **kapı** da muhtaç (D13) ve
kapı **her** yönetim isteğinde çalışıyor; `/me`'ye ayrı bir okuma koymak aynı gerçeği bir
istekte iki kez sormak olurdu. Ayrıca personel satırı **zaten** her istekte okunuyor
(`StaffSessionQueries.cs:49`, C16). Karar: durum o okumaya eklenir, `StaffSessionDto` ile
taşınır ve istek başına üretilen bir claim'e yazılır; `/me` ve kapı o claim'i okur.
*Gerekçe:* D26'nın yasakladığı şey **girişte pişirilmiş** claim'dir — oturumun kalan
otuz dakikası boyunca bayat kalan. İstek başına tazelenen claim, rol ve hesap durumuyla
**aynı** mekanizmadır ve davranış birebir aynıdır; **AC-53** bunu "kurulumu biten
personel aynı oturumda doğru durumu görür" diye ölçüyor. *Bedel:* D26'nın harfi değil
gerekçesi uygulanıyor ve bu satır o farkı yazılı tutuyor.

**D36 — Kapının beyaz listesi D13'ün üç yolu kalır; kabuk durumu `403`'ün KODUNDAN
öğrenir.** Ölçüldü: panel kabuğu ilk iş olarak `GET /api/admin/me` çağırıyor
(`AdminApp.tsx:46`) — kapı onu reddederse kabuk `signedOut`'a düşer ve personel
**giriş–yönlendirme döngüsüne** girer; yani D13'ün üç yollu listesi yazıldığı hâliyle
akışı kilitliyordu. Karar: liste **tam olarak** D13'ün saydığı üç yol kalır
(`me/password`, `me/two-factor*`, çıkış) ve kabuk kurulum borcunu
`403 identity.two_factor_setup_required` kodundan öğrenip kurulum ekranını basar.
*Gerekçe:* desen zaten depoda ve bu kart onu kopyalıyor —
`identity.password_change_required` kodu `AdminApp.tsx:59`'da tam olarak böyle ele
alınıyor. Beyaz listeyi büyütmek, D13'ün *"izin verilenler açıkça sayılır"* gerekçesini
her yeni ekranda yeniden pazarlık konusu yapardı. *Bedel:* kabuk bir hata kodunu akış
sinyali olarak okuyor; bu, `PasswordChangeGate`'in `403`'ü için verilmiş kararın aynısı.

**D37 — D32'nin "son çıkış kapısı" BUGÜN YOK; bedel yazılı, bayrak kapsam dışı.**
D32 telefonunu kaybeden tek Yönetici için çıkış yolunu *"`0006` D2'nin kurulum hesabı"*
diye yazdı. Ölçüldü: `SeedAdministrator.EnsureAsync` **idempotent** ve kendi yorumu
söylüyor — *"hesap varsa hiç dokunulmaz, parolası da sıfırlanmaz"*
(`SeedAdministrator.cs:22`, `:44-51`). Kurulum hesabı pratikte **tek Yöneticinin kendi
hesabıdır**; yapılandırmadaki parolayı değiştirmek hiçbir şeyi açmaz, çünkü engel parola
değil **ikinci adımdır**. Karar: bedel dürüstçe yazılır — *tek Yönetici ikinci adımını
kaybederse çıkış yolu yapılandırma değil, **veritabanı müdahalesidir*** — ve bunu
kapatacak bir kurulum bayrağı (`Identity:SeedAdmin:ResetTwoFactor`) **bu kartın kapsamına
girmez**. *Gerekçe:* o bayrak yeni bir güvenlik kapısıdır ve kapsam kararı Takım
Yöneticisi'nindir; ama yazılı olmayan bir "son kapı", okuyana kutuya güvenmemeyi öğretir
(**D27**'nin aynı dersi). *Bedel, açıkça:* bu kart "tek Yönetici kilitlenmez" sözünü
VERMİYOR; R7'nin *"en az iki Yönetici zorunlu"* hâli hâlâ ayrı bir karardır.

**D38 — Kurtarma kodu doğrulamasının iterasyon sayısı: 10.000 (yapılandırmadan).**

*Uygulama turunda ÖLÇÜLDÜ ve D2'nin tahmini yüksekti* (2026-10-16, bu makine, gerçek kod
yolu — `PasswordHash.Matches`, PBKDF2-SHA256, .NET 10): on kodu **210.000** iterasyonla
karşılaştırmak **295 ms**, **10.000** ile **13 ms**, **1.000** ile **1 ms** sürüyor. D2
*"tek girişte saniyeler"* diyordu; doğrusu **çeyrek saniye** — kararı değiştirmiyor ama
gerekçeyi doğru yere oturtuyor: 295 ms **başarısız her denemenin** sunucuya yüklediği CPU
maliyetidir ve paylaşılan hız sınırı (D5) o maliyeti sınırlayan tek şeydir. Sayı burada
düzeltiliyor, çünkü ölçülmeyen bir sayı spec'ten spec'e farklı şey ölçer. D2
sayıyı kriter turuna bırakmıştı. Karar: varsayılan **10.000** ve değer yapılandırmadan
okunur (**AC-37**). *Gerekçe:* kurtarma kodu **sistem üretimi, yüksek entropili** bir
dizedir, insan parolası değil — ve deponun kendi emsali bunu zaten yazıyor:
`Session.HashToken` SHA-256 kullanıyor ve yorumu *"burada yavaş bir özet gerekmez; token
yüksek entropili rastgele bir değerdir, parola gibi tahmin edilebilir değildir"* diyor
(`Session.cs:30-33`). On kodu 210.000 iterasyonla karşılaştırmak tek girişte **saniyeler**
sürerdi (D2'nin ölçümü); 10.000 ile aynı iş onlarca milisaniyedir. *Bedel:* sızan bir
yedekte çevrimdışı deneme maliyeti düşer — o maliyeti kodun **entropisi** taşıyor.

**D39 — İki açık borç kapsam dışına yazıldı: X7 ve X8.** D3 anahtar döndürmeyi, D12 ve
D19 saklama/temizliği kriter turuna bırakmıştı. Karar: ikisi de § KAPSAM DIŞI'na
gerekçesiyle yazıldı (**X7**, **X8**). *Gerekçe:* ikisi de bu kartın **yarattığı** değil
**devraldığı** sorular — temizlik yolu bugün `Session` için de yok — ve her ikisi de
kendi geçiş planını isteyen bakım kartlarıdır.

**D40 — Tek yeni frontend bağımlılığı: `qrcode.react@4.2.0`.** D17 kare kodun **uçta**
çizilmesini kararlaştırmış ve bedelini *"bir frontend bağımlılığı"* diye yazmıştı.
Ölçüldü: paketin peer aralığı React 19'u kapsıyor (`^16 || ^17 || ^18 || ^19`), yayın
tarihi 2024-12-11 ve çıktısı **SVG** — canvas yok, jsdom'da test edilebilir, çalışma
anında dışarıdan varlık indirmiyor. *Gerekçe:* kendimiz yazmak hata düzeltme matematiği
demek ve depoda emsali yok (D17). *Bedel:* `package.json` bir satır büyüyor; backend
tarafında **yeni paket yok** (D16, HMAC BCL'de).

**D41 — DoD'un "gerçek doğrulama uygulaması" maddesi İKİYE ayrıldı.** Madde
*"üretilen adres en az bir yaygın uygulamada okundu ve ürettiği kod kabul edildi"*
diyor; bunu yazan ajan bir telefon uygulaması çalıştıramaz (ölçüldü: makinede `oathtool`
ve `pyotp` da yok). Karar: (a) **otomatik yarı** — RFC 6238 Ek B'nin **yayınlanmış test
vektörleri** teste girer ve uygulamanın standarda uyduğu bağımsız bir gerçeğe karşı
ölçülür (**AC-2**); (b) **insana kalan yarı** — kare kodu gerçek bir uygulamaya okutmak
QA ya da Takım Yöneticisi'nin adımıdır ve kutu o satır PR'a yazılmadan `[x]` olmaz.
*Gerekçe:* kutunun kendi gerekçesi *"hiçbir birim testi bunu yakalamaz"*; yapılmamış bir
ölçümü yapılmış göstermek tam olarak o kutunun uyardığı şeydir.

**D42 — Oturum öncesi köke iki uç geliyor; rota taraması KAPI hâline getirilir.**
Ölçüldü: `Host.Tests/YonetimYuzeyiKorumasiTests` yönetim kökünün altındaki her ucun
`401` döndüğünü **tarayarak** denetliyor ve kardeş testi *"giriş ucu kökün dışındadır"*
diyor. Bu kart o köke iki uç daha ekliyor: `POST /api/identity/sessions/two-factor` ve
`DELETE /api/identity/sessions/pending` (C14 gereği `/api/admin` dışında). Karar: kardeş
test, oturumsuz çalışması **meşru** uçları **adıyla sayan** bir kapıya çevrilir; sayılmayan
her oturumsuz uç kırmızı verir (**AC-31**). *Gerekçe:* beyaz listeyi büyütmek yerine
listeyi kapı yapmak, bir sonraki spec'in sessizce üçüncü bir uç açmasını engeller.

**D43 — `login()` ayrıştırıcılı bir birleşim döndürür; `CurrentSession` yalnız `/me`'nin
tipi kalır.** D15 gövdenin iki dallı olduğunu söylemişti; uçtaki tipin ne olacağı
yazılmamıştı. Karar: `login()` üç dallı ayrıştırıcılı bir sonuç döndürür
(`completed` · `two_factor_required` · `two_factor_setup_required`) ve kabuk **yalnız
tamamlanmış dalda** gezinir; `/me` bugünkü gövdeyi döndürmeye devam eder, üstüne yeni
alanı ekler. *Gerekçe:* `docs/frontend.md` § 2 — tip ile sözleşmenin ayrıştığı yer sessiz
hata üreten yerdir; bekleyen dalı bugünkü tipe sığdırmak, taşımadığı alanları taşıyormuş
gibi gösterirdi. *Bedel:* `0006`'nın giriş kriterleri 2FA'sız personel için aynen doğru
kalıyor ama tip ve imza değişiyor; PR bunu adıyla yazar.

**D44 — İkinci adımın kendi panel rotası var: `/admin/two-factor`, her role bağlantılı.**
R15, R16 ve D24 personele kendi ekranını borçlu kılıyor; ekranın nereye asılacağı yazılı
değildi. Karar: `AdminView`'e `two-factor` eklenir, adres `/admin/two-factor` olur ve
kabuktaki gezinmeye **her rol** için bir bağlantı basılır. *Gerekçe:* kurulum ekranını
yalnız **kapılı** durumda basmak, opsiyonel rolün R15'ini (kendi isteğiyle açma) ve
D24'ün yenilemesini ulaşılamaz kılardı. *Bedel:* panel gezinmesi bir bağlantı büyüyor.

**D45 — Yeniden oynatma reddi ile tolerans birlikte bir sürtünme üretiyor; kabul
edilir.** D11 kabul edilen adıma **eşit veya küçük** olanı reddediyor, D10 ise ±1 adım
kabul ediyor. Sonuç: aynı pencerede ikinci kez giriş yapan personel (örneğin iki sekme)
*"geçersiz kod"* görür ve otuz saniye bekler. Karar: davranış **aynen D11** kalır, ekran
bu durumda **taze kod** isteyen metni gösterir (**D20**'nin zaten ayırdığı kod).
*Gerekçe:* pencere içinde aynı kodu ikinci kez kabul etmek D11'in var olma nedenini
siler. *Bedel, açıkça:* nadir ama gerçek bir sürtünme; ekranın metni onu anlaşılır kılar.

*Uygulama turunda ÖLÇÜLDÜ — bedel bir TAVAN:* D11 ile D10 birlikte hesap başına
**otuz saniyede bir giriş** veriyor (ilk pencerede iki: `N` ve `N+1`; üçüncüsü pencerenin
dönmesini bekler). İnsan kullanımında görünmez — personel günde birkaç kez giriyor — ama
**e2e süiti** kurulum hesabıyla altmıştan fazla kez giriş yapıyor ve her biri gerçek bir
ikinci adım geçerse süite yarım saati pencere beklemekle geçirir. Ölçüm bu yüzden e2e
tarafında bir karara dönüştü (**D46**'nın ikinci yarısı): gerçek giriş `globalSetup`'ta
**bir kez** yapılıp oturum çerezi paylaşılıyor; giriş AKIŞINI ölçen testler gerçek girişi
kullanmaya devam ediyor. *Tavanın kendisi bir kusur değil* — tek kullanım kuralının
kaçınılmaz sonucu — ama yazılı olmadığı sürece "testler yavaş" diye görünür.

**D46 — e2e'nin giriş yolu ortak bir yardımcıya çıkar (`e2e/giris.ts`).** Ölçüldü: giriş
yapan 14 dosyanın **dokuzu** `girisYap`'ı kendi içinde **yeniden tanımlıyor**, beşi
doldurmayı satır içinde yapıyor. İkinci adımı on dört yerde ayrı ayrı yazmak aynı
mantığın on dört kopyası demek. Karar: ortak yardımcı çıkarılır (`girisYap`,
`yoneticiOlarakGirAsync`, `ikinciAdimKodu` — `node:crypto`, **yeni bağımlılık yok**) ve on
dört dosya ona geçer. *Gerekçe:* D28'in kendi gerekçesi — on dört dosyayı aynı anda
kırmızıya çeviren karar, aynı anda düzeltilebilir olmalı. *Bedel, açıkça:* kapanmış
spec'lerin test dosyalarına dokunuluyor; **iddiaları değişmez**, yalnız giriş adımı
ortaklaşır ve PR her dosyayı adıyla yazar.

*Uygulama turunda ÖLÇÜLDÜ — ortak yardımcı tek başına yetmedi.* İlk hâli her girişte
gerçek bir ikinci adım geçiyordu ve iki duvara çarptı: (1) D11'in tavanı — hesap başına
otuz saniyede bir giriş (**D45**'in ölçümü) ve Playwright'ın test zaman aşımı **30
saniye**, yani pencereyi beklemek testi düşürüyor; (2) R8 — yanlış kod **paylaşılan
kilitlenme sayacını** artırıyor, yani "sıradaki adımı dene" bir başarısız giriş demek ve
üç deneme kurulum hesabını eşiğe taşıyıp **on beş dakika** kilitliyordu. Ölçüm: 246
e2e vakasının **105'i** bu yüzden düştü ve kusurun yeri hiç ikinci adımda değildi.
Karar üçe ayrıldı: **(a)** gerçek giriş `globalSetup`'ta bir kez yapılır ve oturum çerezi
paylaşılır — panele girmesi yeterli olan testler onu kullanır; **(b)** giriş AKIŞINI
ölçen testler **kendi** hesabını kurar (kendi sayacı, kendi adım geçmişi); **(c)**
yardımcı reddedileceğini bildiği kodu hiç göndermez — kullanılan adımı hatırlar ve
süreçler arası çakışma için **tek** bir yeniden deneme bırakır.

**D47 — Ara durum satırındaki deneme sayısı İKİNCİ BİR EŞİK DEĞİLDİR.** D19 satırın
"deneme sayısı" taşıdığını söylüyor; R8 ve § Bilinen tuzaklar 5 ise sayacı ayırmayı
yasaklıyor — yani iki madde birlikte okunduğunda ikinci bir eşik doğuyordu. Karar: eşik
**tek** ve paylaşılan `login_attempts` sayacıdır; ara durum satırındaki sayı yalnız
**ara durumun silinmesini** tetikler ve personel parola fazına döner (**AC-35**).
*Gerekçe:* iki eşik Tuzak 5'in tam olarak uyardığı şeydir; sıfır sayaç ise D19'un yazdığı
alanı ölü bırakırdı. *Bedel:* kilit anında personel parolasını yeniden girer — ipucu
metnini zaten görüyor (**D21**).

### Dış gözden geçirmenin üç kararı (D48–D50)

**D48 — Üç "tek kullanımlık" alan EŞZAMANLILIK BELİRTECİDİR; doğrulama yolu karamsar
kilitle serileştirilmez.** Dış gözden geçirme ölçtü ve kusur gerçekti: yol kilitsiz
oku → kontrol et → işaretle idi ve üçü de eşzamanlı iki istekte iki kez kullanılıyordu —
tek ara durum + tek kod **iki** geçerli oturum üretiyordu (**AC-15**/**AC-23**), tek
kurtarma kodu iki ara durumla **iki** oturum veriyordu (**R4**/**AC-36**). Domain'deki
`??=` korumaları yalnız aynı NESNE örneğini koruyor; iki istek iki ayrı bağlamda iki ayrı
örnek okuyor. Karar: `pending_logins.consumed_at`, `recovery_codes.used_at` ve
`two_factor_enrollments.last_accepted_step` **eşzamanlılık belirteci** olur; üçü de aynı
`SaveChanges` biriminde yazıldığı için tek atomik karar. *Gerekçe:* D11'in kuralı
toleransın açtığı 90 saniyelik pencereyi kapatmak için yazıldı ve tam o pencerede —
kodu gözlemleyen birinin kullanacağı anda — çalışmıyordu. *Neden `FOR UPDATE` değil:*
karamsar kilit bu yola `concurrency.lost` kodunu ve onunla birlikte ikinci adım ekranına
yeni bir hata durumunu + üç dilde çeviriyi getirirdi; belirteç çakışmayı **zaten var olan
iki koda** çeviriyor (**D20**) ve kaybeden isteğe doğru şeyi söylüyor. *Bedel:* şema
değişmiyor — belirteç yalnız `UPDATE`'in `WHERE`'ine giriyor; migration'ın SQL'i birebir
aynı kaldı, yalnız model anlık görüntüsü güncellendi.

**D49 — Kurtarma kodu aramasının önünde BİÇİM elemesi var.** Eleme yalnız boş girdiyi
düşürüyordu, yani altı haneli her yanlış zamana bağlı kod **on** PBKDF2 hesabı ödetiyordu:
D2/D38'in ölçüp kabul ettiği maliyetin hiç gerekmediği yerde ödenmesi. Karar:
`RecoveryCodeText.IsWellFormed` — 16 simge ve alfabe — aramanın önüne geçer.
*Gerekçe:* altı haneli bir kod 16 simgelik bir koda asla eşit olamaz ve biçim bilgisi
zaten tek bir yerde tanımlı; eleme yeni bir bilgi istemiyor. *Bedel:* yok — testin
ağırlık merkezi "hiçbir geçerli kod elenmiyor" tarafında (200 üretilmiş kod + elle
yazılmış dört biçim).

**D50 — Yorum kapısı `tests/` ağacını da tarar ve betiklerin çalıştırma biti kapı
altındadır.** İki kapı, iki ölçülmüş bedel. Yorum/üye kusuru (`docs/conventions.md` § 13)
`0012a`–`0012c`'de dört kez tekrarladı ve #66 onun için bir kapı kurdu; kapı yalnız `src/`
altına bakıyordu ve kusur **beşinci** kez tam o kapının görmediği klasörde döndü
(`tests/Identity.Tests`). Kapı iki ağaca açıldığında **altıncı** bir vaka da çıktı —
`0011`'den (#49) kalan, bu kartla ilgisi olmayan bir satır. İkincisi: `scripts/dev-up.sh`
bir commit'te çalıştırma bitini kaybetti (`100755` → `100644`) ve `README.md`'nin yazdığı
komut — bir sonraki kişinin depodaki ilk komutu — `permission denied` ile düştü.
*Gerekçe:* bu deponun kendi dersi, kapının kendi yorumunda yazılı — *"kusuru düzeltmek
dönüşünü engellemiyor; yalnızca kapı engelliyor."* Görmediği yerde yeşil kalan bir kapı,
var olmayan bir kaliteyi ölçüyor.

*Üçüncü bedel, D48'in kendisi yüzünden:* `0007`/AC-9 (*"satır kilidi yalnız
`BuildingBlocks/Concurrency/` altında yazılır"*) ham metin tarıyordu ve `TwoFactorLogin`'i
**ihlal sandı** — dosya satır kilidi yazmıyor, *neden yazmadığını* yazıyor. Kural kod
satırlarına indirildi ve yorum atlama deseni `SolutionLayout.CodeLines`'a taşındı;
`InvariantGuardSecenekleriTests` aynı tuzağı daha önce kendi özel kopyasıyla çözmüştü ve
ikinci kopya tam olarak bu deponun iki kez ödediği şeydi. Sondayla doğrulandı: kapı kodda
yazılan `FOR UPDATE`'i hâlâ yakalıyor, yani kural zayıflamadı — gerekçesini anlatan bir
dosyayı cezalandıran kural ise gerekçenin hiç yazılmamasını öğretirdi.

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
2026-10-15). **Kabul kriterlerini bekleten bir karar kalmadı** ve kriterler bu turda
yazıldı: **63 kriter**, § Acceptance Criteria.

---

## Acceptance Criteria

> **Altmış üç kriter.** Kümeler iki CLARIFY turunun ve kriter turunun kararlarıyla
> (**D1–D47**) kuruldu. Numaralar **ekleme yoluyla** büyür: araya kriter eklemek,
> testlerdeki `0013/AC-n` etiketlerini yanlış kriterlere bağlar — `0012b`'nin kaçan
> hatası birebir buydu.

### Kurulum

- **AC-1.** Kurulum başlatıldığında sunucu bir doğrulama sırrı üretir ve yanıt `otpauth://` adresini **ve** Base32 sırrı **metin olarak** döner; yanıtta görsel bulunmaz.
- **AC-2.** `otpauth://` adresi dondurulmuş parametre kümesini taşır: `algorithm=SHA1`, `digits=6`, `period=30` ve yapılandırmadan gelen `issuer`.
- **AC-3.** Doğrulanmamış kurulum etkinleşmez: kurulumu başlatmış ama kodla onaylamamış personel kurulumsuz sayılır ve girişinde ikinci adım istenmez.
- **AC-4.** Kurulum onayı geçerli bir kod ister; yanlış kodla gönderilen onay kurulumu etkinleştirmez.
- **AC-5.** Bekleyen kurulumun ömrü yapılandırmadan gelir; süresi dolmuş bekleyen kurulumun onayı reddedilir.
- **AC-6.** Kurulum yeniden başlatıldığında yeni bir sır üretilir ve önceki sırrın ürettiği kod artık kabul edilmez.
- **AC-7.** Kurtarma kodları kurulum onayında **bir kez** döner; aynı kümeyi yeniden okutan bir yol yoktur.
- **AC-8.** Kurtarma kodu sayısı yapılandırmadan gelir ve üretilen küme yapılandırılan sayıda kod taşır.
- **AC-9.** Kurulumu zaten olan personelin yeni kurulum isteği `409` ve `identity.two_factor_already_enrolled` ile reddedilir.
- **AC-10.** Kurulum ekranı kare kodu **uçta** çizer ve elle giriş metnini **her zaman görünür** tutar; sır sunucuda görsele gömülmez.

### Giriş akışı

- **AC-11.** Kurulumu olan personel doğru parolayı girdiğinde **oturum çerezi yazılmaz**; yalnız ara durum çerezi yazılır.
- **AC-12.** Bekleyen giriş yanıtı `200` döner, yalnız hangi adımın geldiğini söyler ve personelin adını, rolünü, dilini ve kimliğini **taşımaz**.
- **AC-13.** Ara durum çerezi `HttpOnly`'dir ve satırda tokenin kendisi değil **SHA-256 özeti** durur.
- **AC-14.** Ara durum yapılandırılan süre sonunda düşer; düşmüş ara durumla yapılan doğrulama `identity.two_factor_pending_expired` döner.
- **AC-15.** Ara durum **tek kullanımlıktır**: başarılı doğrulamadan sonra aynı çerezle yapılan ikinci doğrulama reddedilir.
- **AC-16.** Ara durum oturum değildir: ara durum çerezini taşıyan hiçbir yönetim isteği çalışmaz, `401` döner.
- **AC-17.** *"Kullanıcı değiştir"* ara durumu **siler**; silinmiş ara durumla yapılan doğrulama reddedilir.
- **AC-18.** Başarılı doğrulamada oturum çerezi yazılır ve yanıt tamamlanmış dalı — bugünkü oturum gövdesini — döner.
- **AC-19.** İkinci adım giriş ekranının bir **fazıdır**: adım geçilmeden panele gezinme olmaz.

### Kod doğrulama

- **AC-20.** İçinde bulunulan adımın kodu kabul edilir ve oturum açılır.
- **AC-21.** Yanlış kod `401` ve `identity.two_factor_invalid` ile reddedilir.
- **AC-22.** Penceresi dolmuş kod reddedilir.
- **AC-23.** Kabul edilen bir kod **ikinci kez** kabul edilmez.
- **AC-24.** Saat kayması toleransı **±1 adımdır**: önceki ve sonraki adımın kodu kabul edilir, iki adım önceki kod reddedilir.
- **AC-25.** Yanlış ikinci adım kodu parola denemesiyle **aynı** kilitlenme sayacını artırır; eşiğe ulaşıldığında hesap kilitlenir ve kilitliyken doğru kod da reddedilir.
- **AC-26.** İkinci adım ucu giriş ucunun **hız sınırını paylaşır**: sınır aşıldığında `429` döner.
- **AC-27.** Kilitlenmiş hesapta ikinci adım reddi **ayırt edilemez**: kilit için ayrı bir hata kodu dönmez.
- **AC-28.** Kilitlenme ipucunun deneme sayacı iki fazda **tektir**: parola fazındaki ve kod fazındaki başarısız denemeler aynı sayaca eklenir.

### Zorunluluk ve kapı

- **AC-29.** Zorunlu rolde kurulumu olmayan personel giriş yaptığında yanıt kurulum gerektiğini söyler ve ona **kapılı** bir oturum verilir.
- **AC-30.** Kapılı oturumda izin verilen **üç yol** çalışır: kendi parolasını değiştirme, kendi ikinci adım kurulumu ve çıkış.
- **AC-31.** Kapılı oturumda izin verilenler dışındaki her yönetim isteği `403` ve `identity.two_factor_setup_required` ile reddedilir; oturumsuz çalışan uçların **adıyla sayıldığı** rota kapısı yeşil kalır.
- **AC-32.** İlk girişte sıra **parola değişimi, sonra kurulumdur**: ikisini birden bekleyen personel önce parola kapısını görür.
- **AC-33.** Opsiyonel rolde kurulumu olmayan personel yönetim yüzeyinde **kısıtsız** çalışır.
- **AC-34.** Opsiyonel roldeki personel ikinci adımı **kendi isteğiyle** kurar.
- **AC-35.** İkinci adım denemeleri kilitlenme eşiğine ulaştığında ara durum **silinir** ve personel parola fazına döner.

### Kurtarma ve sıfırlama

- **AC-36.** Kodunu üretemeyen personel bir kurtarma koduyla giriş yapar; kullanılan kod bir daha kabul edilmez.
- **AC-37.** Kurtarma kodu doğrulaması **kendi** iterasyon sayısını yapılandırmadan okur.
- **AC-38.** Personel kalan kurtarma kodu sayısını kendi ekranında görür.
- **AC-39.** Kurtarma kodu kümesinin yenilenmesi oturum **ve** taze bir kod ister; ikisinden biri eksikse yenileme reddedilir.
- **AC-40.** Yenileme kalan kodların **hepsini** düşürür ve yeni kümeyi **bir kez** gösterir.
- **AC-41.** Opsiyonel roldeki personel ikinci adımı **mevcut parolası ve geçerli bir kodla** kapatır; ikisinden biri eksik veya yanlışsa kapatma reddedilir.
- **AC-42.** Zorunlu rolde kapatma **yoktur**: istek reddedilir ve tek yol Yöneticinin sıfırlamasıdır.
- **AC-43.** Yönetici başka bir personelin ikinci adımını sıfırlar; sıfırlanan personel bir sonraki girişinde yeniden kurar.
- **AC-44.** Sıfırlama bir **güvenlik işlemidir**: personelin açık oturumları düşer ve kalan kurtarma kodları geçersizleşir.
- **AC-45.** Yönetici **kendi** ikinci adımını da sıfırlayabilir.
- **AC-46.** Yönetici olmayan rolün sıfırlama isteği `403` ile reddedilir.

### Sızıntı

- **AC-47.** Doğrulama sırrı ve kurtarma kodları **hiçbir log kaydında** görünmez.
- **AC-48.** Doğrulama sırrı veritabanında **açık metin durmaz**.
- **AC-49.** Şifreleme anahtarı dağıtım yapılandırmasından gelir: anahtar yoksa uygulama **ayağa kalkmaz** ve anahtar yapılandırma dosyasında bulunmaz.
- **AC-50.** Kurtarma kodları veritabanında **tek yönlü özetlenmiş** durur.
- **AC-51.** Doğrulama sırrı ve kurtarma kodu hiçbir hata yanıtında bulunmaz.

### Yüzey ve ekran

- **AC-52.** Personel listesi yanıtı her satırda ikinci adım durumunu taşır.
- **AC-53.** İkinci adım durumu her istekte **kaynaktan** okunur: kurulumu biten personel **aynı oturumda** doğru durumu görür.
- **AC-54.** Yönetici personel listesinde her satırın ikinci adım durumunu **rozetle** görür.
- **AC-55.** İkinci adım ekranı **yükleniyor**, **hata**, **ara durumun süresi doldu** ve **kilitlenmiş hesap** durumlarını ele alır.
- **AC-56.** İkinci adım ekranı personelin tercih ettiği dilde çalışır ve Arapça'da **RTL**'dir.
- **AC-57.** Kalan kurtarma kodu sayısı **ICU çoğul biçimiyle** söylenir ve Arapça mesaj **altı biçimi** tanımlar.

### Kayıt

- **AC-58.** Kurulum, **kapatma**, sıfırlama ve kurtarma kodu kullanımı kayda yazılır.
- **AC-59.** Başarısız denemeler kayda **girmez**.
- **AC-60.** Kayıt tablosu yalnız `identity` şemasına dokunur.
- **AC-61.** Kayıt satırı çevrilmiş metin değil **kod** saklar.

### Regresyon altyapısı

- **AC-62.** e2e kurulumu kurulum hesabının ikinci adımını **bir kez** kurar; giriş yapan **on dört** spec dosyasının hepsi yeşil kalır ve yeni e2e bağımlılığı eklenmez.
- **AC-63.** `Host.Tests`'in giriş yapan **dokuz** zinciri ortak bir kurulum yardımcısıyla yeşil kalır.

---

## Definition of Done

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
      (Beş sorunun hepsi: **S2** → D8; **S1 · S3 · S4 · S5** → **D30–D33**, 2026-10-15.)
- [x] **Kabul kriterleri yazıldı ve `main`'e implementasyondan ÖNCE girdi** (ADR `0008` /
      K2): **63** kriter, kendi `docs/` PR'ında. Kriter turunun on dört kararı
      (**D34–D47**) Context'e işlendi.
- [x] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0013-ac<n>.png`), hata ve
      kilitlenme durumları dâhil, RTL için Arapça görüntü de var.
- [x] Sırrın ve kurtarma kodlarının yanıtta, logda ve hata metninde görünmediği testle
      gösterildi (C2).
- [x] <!-- DoD:T4 --> Mimari testler yeşil — `Identity` sınırları korundu.
- [x] <!-- DoD:T5 --> Yeni migration yalnızca `identity` şemasına dokunuyor.
- [x] Mockup'taki iki çelişkili metin — *"Sahip hesabı"* (**D30**: rol yok, zorunluluk
      **Yönetici**'de) ve *"8 hatalı deneme"* (**D8**: eşik **5**) — tasarım tarafında
      güncellendi veya PR'da **açıkça** uyumsuz olarak not edildi.
- [x] **`0008`'in görsel kanıtı yenilendi ve kriterinin iddiası dokunulmadan kaldı**
      (**D27**, birinci turun ölçümünü düzeltir): `0008-ac1.png` dördüncü eylemle
      yeniden üretildi · `evidence-0008.spec.ts:56` ve `StaffRowActions.test.tsx:71`'in
      *"üç eylem"* diyen **başlıkları** güncellendi · PR'da **hiçbir testin kırılmadığı**
      ve `0008`/AC-1'in hâlâ doğru olduğu gösterildi. Kırmızı test **beklenmiyor**;
      beklenirse ölçüm yine yanlış demektir.
- [x] **`0008`'in personel listesi sözleşmesine eklenen alan PR'da adıyla yazıldı**
      (`StaffListItem` + yönetim listesi yanıtı); rozet veri olmadan basılamaz.
- [x] **Doğrulama sırrının veritabanında açık metin DURMADIĞI** testle gösterildi ve
      şifreleme anahtarının dağıtım yapılandırmasından geldiği doğrulandı (**D3**).
      Anahtar döndürme **§ X7**'de gerekçesiyle kapsam dışı (**D39**).
- [x] **Aynı zamana bağlı kodun iki kez kabul edilmediği** testle gösterildi (**D11**),
      saat kayması toleransı ±1 adım olarak ölçüldü (**D10**).
- [x] **Kurtarma kodu doğrulamasının maliyeti ölçüldü** ve iterasyon sayısının
      yapılandırmadan geldiği gösterildi (**D2**) — on özetlemenin üretim değeriyle
      saniyeler sürdüğü hesabı PR'da yazılı.
- [x] **Kayıt tablosunun yalnız `identity` şemasına dokunduğu** ve kaydın kurulum,
      **kapatma**, sıfırlama ve kurtarma kodu kullanımını kapsadığı gösterildi
      (**D12**/R12); başarısız denemelerin kayda **girmediği** gösterildi. Saklama süresi
      **§ X8**'de gerekçesiyle kapsam dışı (**D39**).
- [x] **Kurulumsuz hesabın kapısı tek tek sınandı** (**D13**): izin verilen üç yol
      (kendi parolası, kendi kurulumu, çıkış) çalışıyor ve **dördüncü bir yönetim isteği
      reddediliyor**; `0006` C18/AC-23'ün rota taraması hâlâ yeşil, yani oturumsuz köke
      yönetim ucu **eklenmedi**.
- [x] **Bekleyen giriş yanıtının kimlik taşımadığı** testle gösterildi (**D15**/C13):
      gövdede ad, rol, dil ve kimlik **yok**. `CurrentSession` tipinin ve `login()`
      imzasının değiştiği PR'da adıyla yazıldı.
- [x] **İkinci adımın giriş ekranının bir fazı olduğu** görsel kanıtla gösterildi
      (**D14**): adım geçilmeden gezinme olmuyor, *"Kullanıcı değiştir"* ara durumu
      siliyor ve **F5'in adımı kaybettirdiği** bedel PR'da açıkça yazılı.
- [x] **Parametre kümesi bağımsız bir gerçeğe karşı ölçüldü** (**D16**/**D41**, otomatik
      yarı): RFC 6238 Ek B'nin **yayınlanmış test vektörleri** teste girdi ve geçti
      (**AC-2**). *Neden:* kendi uygulamasını kendi ürettiği kodla sınayan test hiçbir
      şey kanıtlamaz.
- [ ] **Kare kod gerçek bir doğrulama uygulamasında okundu** (**D16**/**D41**, insana
      kalan yarı): QA ya da Takım Yöneticisi adresi en az bir yaygın uygulamaya okuttu,
      onun ürettiği kod kabul edildi ve bunu PR'a **yazdı**. *Neden:* hane/pencere/
      algoritma kümesinin yanlışlığı yalnız "doğru kod reddediliyor" olarak görünür ve
      hiçbir birim testi bunu yakalamaz; **bu kutuyu uygulamayı yazan ajan kapatamaz.**
- [x] **Sırrın sunucuda görsele gömülmediği** (C15/**D17**) ve **elle giriş metninin her
      zaman görünür** olduğu gösterildi — kare kod uçta çizildi, görsel kanıt RTL'de de
      var.
- [x] **Ara durumun ömrü, tek kullanımlığı ve silinmesi** testle gösterildi (**D19**);
      çerezin `HttpOnly` olduğu ve satırda tokenin **özetinin** durduğu doğrulandı.
      Saklama/temizlik **§ X8**'de gerekçesiyle kapsam dışı (**D39**).
- [x] **İki ayrı hata kodu ve karşılıkları** testle gösterildi (**D20**); **kilitlenmiş
      ikinci adımın ayırt edilemez** kaldığı (`0006` R3/AC-5 suskunluğu) sınandı.
- [x] **Sıfırlamanın açık oturumları düşürdüğü ve kalan kurtarma kodlarını
      geçersizleştirdiği** testle gösterildi (**D22**).
- [x] **Kapatma ve yenileme sınandı** (**D23**, **D24**): kapatma mevcut parola **ve**
      geçerli kod istiyor, zorunlu rolde kapatma yok; yenileme kalan kodların **hepsini**
      düşürüyor ve kalan sayı ekranda görünüyor.
- [x] **`twoFactorEnrolled` iki yüzeyde de doğru** (**D25**, **D26**): personel
      listesinde eklemeli alan olarak var ve `/me` onu **kaynaktan** okuyor — kurulumu
      biten personelin aynı oturumda doğru durumu gördüğü testle gösterildi.
- [x] **e2e kurulumu ikinci adımı bir kez kuruyor** (**D28**, **AC-62**): `global-setup`
      kurulum hesabını kaydediyor, ortak `e2e/giris.ts` yardımcısı kodu `node:crypto` ile
      üretiyor ve **giriş yapan on dört spec dosyasının hepsi yeşil**. Yeni e2e bağımlılığı
      eklenmedi; dokunulan dosyalar PR'da **adıyla** yazıldı ve **hiçbirinin iddiası
      değişmedi** (**D46**).
- [x] **`Host.Tests`'in dokuz zinciri ortak yardımcıya geçti ve yeşil** (**D34**,
      **AC-63**): dosyalar PR'da adıyla yazıldı. *Neden:* D28 yalnız e2e'yi saymıştı;
      ölçüm dokuz backend zincirinin de aynı kararla kırıldığını gösterdi.
- [x] **Tek Yöneticinin kilitlenmesi bedeli PR'da AÇIKÇA yazıldı** (**D37**): `0006` D2'nin
      kurulum hesabı yolu ikinci adım için **çalışmıyor** (`SeedAdministrator` idempotent);
      çıkış yolu yapılandırma değil veritabanı müdahalesidir ve kurulum bayrağı bu kartın
      kapsamında **değildir**.
- [x] **Oturumsuz çalışan uçlar adıyla sayan bir kapıya bağlandı** (**D42**, **AC-31**):
      iki yeni oturum öncesi uç listeye girdi ve listede olmayan her oturumsuz uç kırmızı
      veriyor.
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi.
- [x] <!-- DoD:T7 --> **Üç yeni domain terimi `docs/domain.md`'ye eklendi** (**D29**):
      İkinci Adım (`TwoFactorEnrollment`) · Kurtarma Kodu (`RecoveryCode`) · Doğrulama
      Sırrı (`TwoFactorSecret`). Kod adları sözlükteki karşılıklarıyla birebir aynı.
- [x] <!-- DoD:T8 --> `docs/architecture.md` § Karar Defteri güncellendi (**2FA
      kapsamda** satırı). *"Oturum öncesi ara durum"* satırı ikinci CLARIFY turunda
      zaten eklendi (**D13**/**D19**); kalan borç kapsam satırıdır.
- [x] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4). **#76**'nın ilk
      koşusunda dört işin dördü de **koştu** ve yeşil — atlanan iş yok: Backend `229s`,
      Frontend `54s`, E2E `427s`, Değişen yollar `4s`; ayrı `Performans` koşusunda C3
      eşikleri `128s`. Dış gözden geçirmenin düzeltmeleri aynı ölçümü **ikinci kez**
      geçti (son kod değişikliği `48060af`): Backend `302s`, Frontend `50s`, E2E `434s`,
      Değişen yollar `5s`, C3 eşikleri yeşil. Bundan sonraki commit'ler yalnız bu kutunun
      metni; koşunun `success` demesi yetmediği için iş iş okundu (metin değişikliğinde
      Frontend ve E2E bilerek atlanıyor — #68 — ve atlanan iş de koşuyu yeşil bırakır).
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [x] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
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
