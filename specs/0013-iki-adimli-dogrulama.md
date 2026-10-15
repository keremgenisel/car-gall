# 0013 — İki Adımlı Doğrulama

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Identity` · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0006` (oturum ve roller — tamamlandı) · `0009` (bildirim — tamamlandı) |
| **Dayanak** | ADR 0003 unsur 2; Ek 1 sıra #6 |
| **Karar bekliyor** | **S1, S3, S4, S5** — § Açık sorular (**4** soru, dördünde de öneri yazılı). **S2 kapandı** (CLARIFY turu, **D8** — ölçümle). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |
| **Kararlar** | **D1–D12**, 2026-10-15 CLARIFY turunda bugünkü `main` (`c5143b6`) üzerinde **ölçülerek** verildi. Üçü spec'i yazılı hâliyle **uygulanamaz** kılıyordu. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — dört açık sorunun cevabı
> onları belirliyor. CLARIFY turu (2026-10-15) on iki karar ekledi ve R8, C2, R12'yi
> ölçümle **düzeltti**; kriter turu bu kararların üzerine kurulacak.

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
  kod belirli bir süre geçerlidir ve süresi dolunca kabul edilmez.
- **R3.** Personel ikinci adımı **kurar**: sistem bir sır üretir, personel bunu kendi
  doğrulama uygulamasına ekler ve **bir kod girerek kurulumu doğrular**. Doğrulanmamış
  kurulum etkinleşmez.
- **R4.** Kurulum sırasında personele **kurtarma kodları** verilir. Her kurtarma kodu **tek
  kullanımlıktır**.
- **R5.** Personel, kodunu üretemediğinde bir kurtarma koduyla giriş yapabilir; kullanılan kod
  bir daha kabul edilmez.
- **R6.** Kurtarma kodu da kalmayan personelin ikinci adımı bir **Yönetici** tarafından
  sıfırlanır; sıfırlanan personel bir sonraki girişinde yeniden kurar.
- **R7.** İkinci adımın hangi roller için **zorunlu** olduğu bellidir ve zorunlu bir rolde
  kurulumu olmayan personel panele **giremez**; önce kurar. → Açık soru **S1**.
- **R8.** Yanlış girilen ikinci adım kodu, parola denemesiyle **aynı kilitlenme sayacına**
  ve **aynı hız sınırına** tabidir. Eşik **5**'tir (**D8**); hız sınırı giriş ucunun
  politikasıdır (**D5**).
- **R9.** Kodun hangisinin yanlış olduğu — parola mı ikinci adım mı — giriş ekranında
  **söylenmez** ötesinde: ikinci adım ekranına geçilmiş olması, parolanın doğru olduğunu
  zaten ifşa eder. → Açık soru **S5**.
- **R10.** İkinci adım ekranı **yükleniyor**, **hata**, **süresi doldu** ve **kilitlenmiş
  hesap** durumlarını ele alır. → `docs/frontend.md` § 3.
- **R11.** Ekran personelin tercih ettiği dilde çalışır; Arapça'da RTL. → `0006` R15.
- **R12.** İkinci adımın kurulması, sıfırlanması ve kurtarma kodunun kullanılması
  **kaydedilir** — ve kaydın nerede yaşadığı **D12**'de tanımlıdır: bu depoda bugün
  denetim kaydı altyapısı YOK (ölçüldü).

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
  kilitlemesine yol açamaz. → `0006` D3 ile tutarlı, Açık soru **S4**.
- **C11.** Her kabul kriteri en az bir testle karşılanır. → Altın Kural 5.

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
  seçenek açıyor (Açık soru **S3**).
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
| `0008`'in kapanmış kriteri | `evidence-0008.spec.ts:56` *"her satırda üç eylem"* diyor (`0008`/AC-1, AC-15) → dördüncü eylem onu kıracak |

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

### CLARIFY turunda ölçülen bedel: kapanmış bir spec'in kriteri etkilenecek

**`0008`'in yüzeyi iki yerden değişiyor ve ikisi de adıyla yazılı:**

1. **Personel listesi sözleşmesi eklemeli bir alan kazanır.** Ölçüldü: `StaffListItem`
   yedi alan taşıyor (`StaffCommands.cs:17`) ve ikinci adım durumu **yok**. Mockup üç
   panelde "2FA açık" rozeti çiziyor; rozet veri olmadan basılamaz. (`0012c` D50'nin
   emsali: kapanmış sözleşmeye eklemeli alan.)
2. **`0008`'in kapanmış bir kriteri KIRILACAK.** Ölçüldü: `evidence-0008.spec.ts:56`
   satırındaki test `0008/AC-1` ve `0008/AC-15` için *"her satırda **üç eylem**"*
   iddiasında bulunuyor. İkinci adım sıfırlama dördüncü eylemdir. Bu, `0012c`'de
   `0010b`/AC-24'ün başına gelenin aynısı — orada locator gevşek eşleşmeden `exact`'e
   çekilmişti. Uygulama PR'ı bu testi **adıyla** güncellemek ve kriterin iddiasının
   zayıflamadığını göstermek zorundadır.

### Bilinen tuzaklar

1. **Kurtarma kodunu tek kullanımlık yapmamak.** Yeniden kullanılabilir kod, yazılı bir
   yedek paroladır ve ikinci adımın değerini sıfırlar.
2. **Sırrı yanıtta veya logda göstermek.** `0006` C6 parolayı yasakladı; sır ve kurtarma
   kodu aynı sınıftır (C2).
3. **Tek Yöneticinin kilitlenmesi.** Sıfırlama yetkisi yalnızca Yöneticideyse ve tek Yönetici
   telefonunu kaybederse sistem **tamamen kapanır**. `0006` D3 "son aktif Yönetici korunur"
   kuralını kurdu; bu, onun ikinci adım tarafındaki karşılığıdır (S4).
4. **Saat kayması.** Zamana bağlı kod, sunucu ile cihaz saati arasındaki küçük farka
   dayanıklı olmalıdır; yoksa doğru kod reddedilir ve personel sebebini anlamaz (C5).
5. **Kilitlenme sayacını ayırmak.** İkinci adım için ayrı bir sayaç tutmak, saldırganın
   parolayı doğruladıktan sonra kodu sınırsız denemesine yol açar (R8).

---

## Açık sorular

**S1. Hangi roller için zorunlu?**
Mockup "Sahip ve yönetici" diyor; "Sahip" rolü yok (C3).
*Öneri:* **Yönetici** rolü için **zorunlu**, Satış Danışmanı ve İçerik Editörü için
**opsiyonel** (personel kendi açabilir).
*Gerekçe:* Yönetici, personel ekleyip rol değiştirebilen tek roldür (`0006` R9); o hesabın
ele geçmesi sistemin tamamını verir. Danışmanı zorunlu tutmak, `0006`'nın "küçük ekipte
sürtünme" gerekçesini tümden yok sayar.
*Bedel:* danışman hesabı tek parolayla korunmuş kalır — ve o hesap da talep kişisel
verilerini görür. Daha sıkı istiyorsanız "tüm roller zorunlu" demek tek satırlık bir değişiklik.
*CLARIFY turunun ölçümü (2026-10-15):* roller gerçekten **üç** ve "Sahip" yok —
`StaffRole` enum'u `Manager`, `SalesAdvisor`, `ContentEditor` (`StaffRole.cs:12`). Yani
mockup'ın "Sahip ve yönetici" metni uygulanabilir bir role karşılık gelmiyor ve C3 ile
çelişkisi **doğrulandı**. Ayrıca ölçüldü: depoda ikinci adıma ait **hiçbir kod yok**
(`totp|twofactor|2fa|authenticator` taraması → sıfır dosya), yani bu soru mevcut bir
davranışı değiştirmiyor; temiz sayfada bir kapsam kararı.

**S2. ~~5 deneme mi 8 deneme mi?~~ → KAPANDI (CLARIFY turu, D8)**
Karar: **5**. Bu bir ölçüm olduğu için açık soru olarak bırakılmadı —
`IdentityOptions.DefaultFailedAttemptThreshold = 5` ve değer yapılandırmadan okunuyor;
mockup'ın 8'i hiçbir yerde uygulanmadı. Değişen taraf, maliyeti olmayan taraf: mockup
metni. DoD'un ilgili maddesi bunu şart koşuyor.

**S3. Kurtarma kodları personele nasıl verilir?**
`0006` D1 e-posta yolunu kapatmıştı ama `Notifications` artık var.
*Öneri:* kurulum anında **ekranda bir kez gösterilir**; e-posta ile gönderilmez.
*Gerekçe:* kurtarma kodunu e-postaya göndermek, ikinci adımı e-posta hesabının güvenliğine
indirger — ki o hesap bizim kontrolümüzde değil. Ekranda bir kez gösterme, kodun yalnızca
personelin kendi aldığı yere gitmesini sağlar.
*Bedel:* personel kodları kaydetmezse kaybeder ve R6'nın sıfırlama yoluna düşer.
*CLARIFY turunun ölçümü:* `Notifications`'ın **tek** kanalı var — `NotificationChannel`
enum'unda yalnız `Email = 1` (`NotificationStatus.cs:20`). Yani "gönderelim" denirse tek
seçenek e-postadır ve önerinin gerekçesi (ikinci adımı e-posta hesabının güvenliğine
indirgemek) tam olarak o seçeneğe karşı. Kararın teknik maliyeti düşük, güvenlik maliyeti
yüksek — ikisi bu soruda ters yönde.

**S4. Bir Yönetici başka bir Yöneticinin ikinci adımını sıfırlayabilir mi? Tek Yönetici
kendi telefonunu kaybederse ne olur?**
*Öneri:* Yönetici, **kendisi dâhil** başka bir personelin ikinci adımını sıfırlayabilir —
ama **kendi** adımını sıfırlamak için zaten giriş yapmış olması gerekir, dolayısıyla tek
Yönetici telefonunu kaybederse **kilitlenir**. Bu durum için `0006` D2'nin kurulum
hesabı yolu açık tutulur: dağıtım yapılandırmasındaki kurulum hesabı son çıkış kapısıdır.
*Gerekçe:* başka bir kurtarma kapısı açmak (örneğin e-posta ile sıfırlama), S3'ün
gerekçesini delerdi.
*Bedel:* tek Yöneticili bir galeride telefon kaybı, dağıtım yapılandırmasına erişim
gerektirir — yani bize. Bunu kabul etmiyorsanız R7 "en az iki Yönetici zorunlu" hâline
gelmelidir ve bu ayrı bir karardır.
*CLARIFY turunun ölçümü:* önerinin dayandığı iki mekanizma **gerçekten var**:
(1) son aktif Yönetici koruması kurulu ve yeniden kullanılabilir —
`IdentityErrors.LastActiveManager`, `Staff.cs:119`, `StaffCommands.cs:104` ve `:143`
(rol düşürme ve pasifleştirme için); (2) kurulum hesabı yolu (`0006` D2) duruyor ve
uygulama parola yapılandırılmadan **hiç başlamıyor** (`IdentityModule.cs:36`).
**Ama bedeli 2026-10-15'te arttı:** ADR `0011` artık depoda yayınlanmış üç geliştirme
parolasını da koşulsuz reddediyor, yani "son çıkış kapısı" gerçek bir yapılandırma
sırrı gerektiriyor — unutulmuş bir varsayılanla açılamaz.

**S5. İkinci adım ekranına geçmek parolanın doğru olduğunu ifşa eder; kabul mü?**
`0006` AC-4 parolayla kullanıcı adını ayırt edilemez kılmıştı.
*Öneri:* **kabul edilir.** İkinci adım ekranı yalnızca parola doğruysa gösterilir.
*Gerekçe:* alternatif, yanlış parolada da sahte bir kod ekranı göstermektir; bu, personelin
kendi hatasını anlamasını engeller ve destek yükü üretir. Parolanın doğruluğunu bilmek,
ikinci adım olmadan saldırgana bir şey kazandırmaz — ikinci adımın varlık nedeni tam budur.
*Bedel:* geçerli kullanıcı adı + parola çiftinin doğruluğu saldırgan tarafından
öğrenilebilir; R8'in paylaşılan kilitlenme sayacı bunu sınırlar.
*CLARIFY turunun ölçümü:* R8 artık yalnız kilitlenme sayacını değil **hız sınırını** da
paylaşıyor (**D5**) — giriş ucu `RequireRateLimiting` taşıyor ve sınır
yapılandırmadan (`LoginRequestLimit` 20 / 10 dk). Yani bu sorunun bedelini sınırlayan şey
iki mekanizma oldu, bir değil. Ayrıca **D4** gereği ikinci adım geçilmeden oturum çerezi
hiç yazılmıyor: "parola doğru" bilgisi saldırgana oturum vermiyor.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> kurulum ve doğrulanmamış kurulumun etkinleşmemesi · zorunlu roldeki kurulumsuz personel ·
> geçerli/geçersiz/süresi dolmuş kod · saat kayması toleransı · kurtarma kodunun tek
> kullanımlığı · sıfırlama ve son Yönetici koruması · paylaşılan kilitlenme sayacı ·
> sırrın yanıtta ve logda bulunmaması · üç durum + süresi doldu + kilitlenmiş ekranları ·
> çok dil ve RTL · kayıt tutma.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0013-ac<n>.png`), hata ve
      kilitlenme durumları dâhil, RTL için Arapça görüntü de var.
- [ ] Sırrın ve kurtarma kodlarının yanıtta, logda ve hata metninde görünmediği testle
      gösterildi (C2).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — `Identity` sınırları korundu.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `identity` şemasına dokunuyor.
- [ ] Mockup'taki iki çelişkili metin (S1 "Sahip", S2 "8 deneme") tasarım tarafında
      güncellendi veya PR'da **açıkça** uyumsuz olarak not edildi.
- [ ] **`0008`'in kapanmış kriteri adıyla yazıldı ve zayıflatılmadı:**
      `evidence-0008.spec.ts`in *"her satırda üç eylem"* iddiası (`0008`/AC-1, AC-15)
      dördüncü eylemle birlikte güncellendi ve iddianın ne kaybettiği — ya da
      kaybetmediği — PR'da gösterildi (**CLARIFY turunun ölçümü**).
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
      sıfırlama ve kurtarma kodu kullanımını kapsadığı gösterildi (**D12**/R12);
      saklama süresi kapsamda değilse gerekçesiyle kapsam dışına yazıldı.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi.
- [ ] <!-- DoD:T7 --> Yeni domain terimi yok.
- [ ] <!-- DoD:T8 --> `docs/architecture.md` § Karar Defteri güncellendi (2FA kapsamda).
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
