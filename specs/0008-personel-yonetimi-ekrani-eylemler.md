# 0008 — Personel Yönetimi Ekranı: Eylemler ve Çakışma Davranışı

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` personel yönetimi ekranı · `Identity` (var olan uçlar + `/me` yanıtına `staffId` — bkz. **D1**) |
| **Bağımlı spec'ler** | `0006` (kimlik, tamamlandı) · `0007` (eşzamanlılık — sunucu tarafı koruma ve hata sözleşmesi burada üretildi) |

---

## Intent

Galeri yöneticisi bugün panelde personel **ekleyebiliyor** ama işten ayrılan birini
**pasifleştiremiyor**, rolünü değiştiremiyor, parolasını sıfırlayamıyor. Liste salt
okunur. Bir personel ayrıldığında galeri yöneticisinin elinde hiçbir şey yok — hesabı
kapatmak için kimseyi arayacağı bir yer de yok.

Sunucu tarafı bu işlemleri **zaten yapabiliyor**: uçlar `0006`'da yazıldı, korumaları
`0007`'de eşzamanlılığa karşı sağlamlaştırıldı ve iki red türünün üç dildeki metni de
hazır. Eksik olan tek şey, o uçlara basacak ekran.

Bu iş bittiğinde galeri yöneticisi personel listesinden doğrudan pasifleştirme, rol
değiştirme ve parola sıfırlama yapabilir; bir işlem başka birinin aynı anda yaptığı
değişiklik yüzünden geçmediğinde ekran **ne olduğunu** söyler ve elindeki veriyi
kaybettirmez.

---

## Requirements

- **R1.** Yönetici, personel listesinden bir personeli **pasifleştirebilir**.
- **R2.** Yönetici, bir personelin **rolünü değiştirebilir**.
- **R3.** Yönetici, bir personelin **parolasını sıfırlayabilir**: yeni parolayı **kendisi
  belirler**, ekran kaydetmeden önce bir kez doğrulatır ve kaydettikten sonra parolayı
  ekranda tutmaz. Personelden ilk girişinde değiştirmesi istenir (`0006` R10).
  *(Sistem tarafından üretilen parola bu spec'te yoktur → X5. Gerekçe: var olan uç parolayı
  Yöneticiden alır ve gövdesiz `204` döner; üretilmiş bir parolayı göstermek yeni bir yanıt
  gövdesi gerektirir ve `0006` C6 parolanın API yanıtında bulunmasını yasaklar.)*
- **R4.** Sonucu ağır olan işlemler **onay ister** ve onay metni ne olacağını açıkça yazar:
  pasifleştirme, parola sıfırlama ve **Yöneticinin kendi rolünü düşürmesi**.
  *(Gerekçe: ölçüt "geri alınamaz" değil, "sonucu ağır"dır — rol düşürme geri alınabilir ama
  kendini kilitleyen kişi geri alacak yetkiyi de kaybeder.)*
- **R5.** Bir işlem reddedildiğinde ekran **üç yolu** ayırt eder:
  - **Kural gereği red** (`identity.last_active_manager`) — kalıcı; metin tekrar denemeyi
    **önermez**.
  - **Eşzamanlılık kaybı** (`concurrency.lost`) — geçici; metin tekrar denemeyi **önerir** ve
    eylem tekrar denenebilir kalır.
  - **Diğer her hata** — genel mesaj + tekrar dene. Oturum düştüğünde (`401`) giriş ekranına
    yönlendirilir (`0006` AC-24).
- **R6.** **Başarılı** bir işlemden sonra liste tazelenir; ziyaretçi sayfayı elle yenilemek
  zorunda kalmaz. Tazelemeden sonra odak işlem yapılan satıra döner ve tablonun kaydırma
  konumu korunur.
- **R7.** **Kural gereği reddedilen** bir işlemden sonra liste **tazelenmez**; ziyaretçinin
  seçimi ve konumu korunur, liste başa sarmaz.
- **R8.** **Eşzamanlılık kaybında** liste tazelenir — çünkü ekrandaki veri artık bayattır —
  ama mesaj görünür kalır, seçim ve odak korunur.
  *(Gerekçe: iki red türü iki farklı davranış ister. Biri "veriniz bayat", diğeri
  "kural böyle"; R5 bu ayrımı metinde yapıyordu, R7–R8 davranışta yapar.)*
- **R9.** Bir işlem sürerken eylemin düğmesi yükleniyor hâline geçer ve **aynı satır için
  ikinci istek gönderilmez**.
- **R10.** Onay, **odak yönetimi olan** bir iletişim kutusudur: açıldığında odak içine girer,
  Esc ile kapanır, kapandığında odak tetikleyen düğmeye döner ve içindeki her etkileşimli öğe
  klavyeyle çalışır (`docs/frontend.md` § 7 — odak tuzağı bırakılmaz).
- **R11.** Bu ekran **Yöneticiye özeldir.** Satış Danışmanı ve İçerik Editörü ne listeyi ne
  eylemleri görür; ekrana gittiklerinde erişim yok durumu gösterilir ve giriş ekranına
  **yönlendirilmezler** (`0006` AC-27). Panel navigasyonunda bağlantı onlara gösterilmez.
- **R12.** Ekran, listedeki **kendi satırını** ayırt eder ve Yöneticinin kendisini
  pasifleştirmesi veya rolünü düşürmesi hâlinde onay metni panele erişemeyeceğini söyler.
  Bunun için oturum bilgisi personelin kimliğini taşır (**D1**).

---

## Constraints

### Uyulması zorunlu

- **C1.** Yeni sunucu ucu **yazılmaz**; `0006`'nın dört ucu yeterlidir. **Tek istisna
  D1'dedir** ve yetkilendirilmiştir: `GET /api/admin/me` yanıtına `staffId` alanı eklenir —
  yeni bir uç değil, var olan bir yanıtın alanı. Bunun dışında eksik bir uç bulunursa bu spec
  **durur ve önerilir**; sessizce uç eklenmez.
- **C2.** Ekran **yükleniyor** ve **hata** durumlarını taşır (`docs/frontend.md` § 3).
  **Boş durum bu ekranda uygulanabilir değildir**: `docs/domain.md` **S1** her zaman en az
  bir aktif Yönetici garanti ettiği için liste hiç boş olamaz. Mevcut `StaffListEmpty`
  bileşeni savunma amaçlı korunur ama kriteri yoktur.
- **C3.** Ziyaretçiye teknik ayrıntı gösterilmez: durum kodu, `errorCode`, `traceId`,
  endpoint yolu, istisna metni (`docs/frontend.md` § 3.1).
- **C4.** Metin koda gömülmez; TR/AR/EN üçünde de anahtar bulunur (Yasak Listesi 24).
- **C5.** **Hata kodu → anahtar eşlemesi eylem başına ve ULAŞILABİLİRLİĞE göre kurulur.**
  `messageKeyFor` önek bazlıdır ve bilinmeyen kodda `<önek>.generic`e düşer; eksik anahtar
  **kırmızı test üretmez**, yalnız yanlış mesaj gösterir (Tuzak 4). Her eylem için yalnızca
  o eylemin gerçekten döndürebileceği kodlar yazılır:

  | Önek | Anahtarlar |
  |---|---|
  | `staff.deactivate.error` | `last_active_manager`, `already_deactivated`, `concurrency_lost`, `generic` |
  | `staff.role.error` | `last_active_manager`, `concurrency_lost`, `generic` |
  | `staff.password.error` | `password_too_short`, `generic`, **`required`**, **`mismatch`** |

  **11 anahtar × 3 dil = 33.** Son ikisi sunucu kodu değil, **uç tarafı doğrulama**
  anahtarıdır: AC-12 eşleşmeyen parolanın hatasını alanda göstermeyi istiyor ve o mesajın
  da kataloğu var. Parola sıfırlamada `concurrency_lost` **yoktur**: o uç
  `InvariantGuard`'dan geçmiyor ve kodu hiç üretemiyor — ulaşılamayan koda çeviri yazmak,
  ölü kodun katalog hâlidir (`0007`'de tek `concurrency.lost` kodunu aynı gerekçeyle
  seçmiştik). Buna karşılık `password_too_short` **zorunludur**: AC-14 onu ziyaretçinin
  dilinde istiyor. AC-27'nin testi bu tabloyu tek kaynaktan okur, sabit sayı saymaz.
- **C6.** RTL: Arapça tercih eden personel için ekran **ve onay kutusu** `dir="rtl"` ile
  çalışır (Yasak Listesi 25 — fiziksel CSS yönü yok).
- **C7.** Her etkileşimli öğe klavyeyle (Enter/Space) çalışır (`docs/frontend.md` § 7).

### KAPSAM DIŞI

- **X1.** Personel **silme** — *neden:* `0006` D4 pasifleştirmeyi kalıcı iz bırakan tek
  yol olarak seçti; silme o kararı deler.
- **X2.** Toplu işlem (çoklu seçim) — *neden:* iki–üç kişilik bir ekipte tek tek yapılır;
  toplu işlem, onay ekranını da eşzamanlılık davranışını da katmerler.
- **X3.** Denetim kaydı ekranı ("kim ne zaman pasifleştirdi") — *neden:* `Audit` modülü
  henüz yok; kendi spec'ini hak ediyor.
- **X4.** Şube kapatma ekranı — *neden:* `0007` D2 komutu yazdı ama HTTP ucu açmadı;
  `Dealership`'in yazma yüzeyi kendi spec'ine ait (`0003` D1).
- **X5.** **Sistem tarafından üretilen parola** ("rastgele parola oluştur ve bir kez göster")
  — *neden:* var olan uç parolayı Yöneticiden alır ve gövdesiz `204` döner; üretilmiş parolayı
  göstermek yeni bir yanıt gövdesi gerektirir ve `0006` C6/AC-50 parolanın API yanıtında
  bulunmasını yasaklar. İstenirse kendi spec'i olur ve o spec C6'yı yeniden tartışır.

---

## Context

### Mevcut durum — doğrulandı

| Uç | İstek / yanıt | Ekranda |
|---|---|---|
| `POST /api/admin/staff` | `AddStaffRequest` → `201` + `AddStaffResponse(StaffId)` | **var** (`AddStaffForm`) |
| `POST /api/admin/staff/{id}/deactivate` | gövdesiz → `204` | **YOK** |
| `POST /api/admin/staff/{id}/role` | `ChangeRoleRequest(Role)` → `204` | **YOK** |
| `POST /api/admin/staff/{id}/password-reset` | `ChangeRoleRequest` değil — **`ResetPasswordRequest(NewPassword)`** → `204`, gövdesiz | **YOK** |
| `GET /api/admin/me` | → `CurrentSessionResponse(Name, Role, PreferredLocale, MustChangePassword)` | var |
| `GET /api/admin/staff` | → `StaffListItem(StaffId, DisplayName, LoginId, Role, Status, PreferredLocale, BranchId)` | var |

Kök `/api/admin` ve grubun tamamı oturum gerektiriyor; `staff` uçları ayrıca `ManagerPolicy`
taşıyor ([AdminEndpoints.cs:38-76](src/Modules/Identity/Identity/Endpoints/AdminEndpoints.cs#L38-L76)).
[StaffListPage.tsx](frontend/src/features/identity/StaffListPage.tsx) salt okunur bir tablo;
yanında `AddStaffForm` duruyor.

**İki önemli tespit:**

1. **Parola sıfırlama parolayı Yöneticiden alır.** `ResetPasswordRequest(string? NewPassword)`
   ve yanıt gövdesizdir. Gösterilecek bir parola **yoktur** — R3 buna göre yazıldı, X5 de
   bundan doğdu.
2. **`/me` yanıtı `staffId` taşımıyor.** Sunucuda kimlik `ClaimTypes.NameIdentifier` olarak
   var ama yanıta konmamış; `StaffListItem` de "bu benim" bayrağı taşımıyor ve `DisplayName`
   tekil değil. Ekran kendi satırını güvenilir biçimde tanıyamaz → **D1**.

### `0007`'nin bıraktığı zemin

Sunucu iki red türünü ayrı kodlarla döndürüyor ve ikisi de `409`:

| Kod | Nerede tanımlı | Anlamı | Ekran ne demeli |
|---|---|---|---|
| `identity.last_active_manager` | `Identity/Domain/IdentityErrors.cs` | Kural gereği red — **kalıcı** | Tekrar denemeyi önerme |
| `concurrency.lost` | `BuildingBlocks/Concurrency/ConcurrencyErrors.cs` | Eşzamanlılık kaybı — **geçici** | Tekrar denemeyi öner |

**Çeviriler hazır DEĞİL — doğrulandı.** Katalogda bugün yalnızca **ekleme formunun** öneki
var: `staff.add.error.last_active_manager` ve `staff.add.error.concurrency_lost`.
`messageKeyFor(errorCode, önek, locale)` önek bazlı çalışır ve bilinmeyen kodda
`<önek>.generic`e düşer
([loginFormState.ts:24-29](frontend/src/features/identity/loginFormState.ts#L24-L29)).
Dolayısıyla üç yeni eylemin her biri kendi önekini ister: **3 eylem × 3 anahtar × 3 dil**.
Anahtar eklenmezse ekran sessizce genel mesaja düşer ve **R5'in ayrımı kaybolur** — yani bu
spec'in ana davranışı.

### Bilinen tuzaklar

1. **Naif tazeleme.** `0007` § X5 bu ekranı kapsam dışı bırakırken şunu yazdı: kaybeden
   istekten sonra listenin tazelenmesi ve seçimin korunması **kendi kriterlerini hak ediyor**.
   "Hata aldık, listeyi baştan çek" davranışı ziyaretçinin yerini ve seçimini kaybettirir —
   ve bu, kuralın kendisinden daha sık karşılaşılan bir sorundur. R6–R8 sıralamayı kurar.
2. **Kendini kilitlemek.** Bir Yönetici kendi hesabını pasifleştirebilir veya rolünü
   düşürebilir — son aktif Yönetici olmadığı sürece (`docs/domain.md` **S1**). Onay metni
   bunu açıkça söylemeli; söyleyebilmesi için ekranın kendi satırını tanıması gerekir (D1).
3. **Bayat liste.** `concurrency.lost` "karara giren sayı değişti" demektir; ekrandaki liste
   o anda yanlış olabilir. Tazelememek veriyi bayat bırakır, tazelemek konumu bozar — R8
   ikisini ayırır.
4. **Önek tuzağı.** `messageKeyFor` sessizce `generic`e düşer; eksik anahtar **kırmızı test
   üretmez**, yalnızca yanlış mesaj gösterir. C5 ve AC-27 bunun kapısıdır.

### Verilen kararlar

İkisi de Takım Yöneticisi tarafından onaylandı.

**D1 — `GET /api/admin/me` yanıtına `staffId` eklenir.**
*Gerekçe:* Tuzak 2'nin uyarısı ("bu işlemden sonra panele erişemeyeceksiniz") ekranın kendi
satırını tanımasını gerektiriyor; bugün tanıyamıyor. `DisplayName` tekil değil, `LoginId`
`/me`'de yok. Kimlik sunucuda zaten claim olarak duruyor; yanıta konması yeni bir uç değil,
var olan bir yanıtın alanıdır. *Bilerek ödenen bedel:* `0006`'nın oturum sözleşmesine
dokunulur — bu yüzden `0006`'nın oturum testleri kesintisiz yeşil kalmak zorundadır (DoD).
*Elenen alternatif:* tuzağı kapsam dışına almak — o zaman `0006` R12'nin bilinçli olarak
mümkün kıldığı "kendini pasifleştirme" bilinçsiz bir kazaya dönüşür.

**D2 — Parolayı Yönetici belirler; sistem üretmez.**
*Gerekçe:* Var olan uç parolayı Yöneticiden alıyor ve gövdesiz `204` dönüyor; üretilmiş bir
parolayı "bir kez göstermek" yeni bir yanıt gövdesi ister ve `0006` C6/AC-50 parolanın API
yanıtında bulunmasını yasaklar. *Bilerek ödenen bedel:* Yönetici parola uydurmak zorunda
kalır ve zayıf parola seçebilir — karşılığında asgari uzunluk kuralı (`0006` AC-36) sunucuda
zaten duruyor. *Elenen alternatif:* üretilmiş parola → X5, kendi spec'i.

**D3 — Onay kutusu `features/identity` içinde kalır; `shared/ui`'ye girmez.**
*Gerekçe:* `docs/frontend.md` § 4.2 ortaklığın ölçütünü "kaç ekran" diye koyuyor, "kaç
çağrı" diye değil — bu kutu üç eylemde kullanılıyor ama **tek ekranda**. Erken terfi,
ortak bileşeni tek bir ekranın ihtiyaçlarına göre şekillendirir ve ikinci kullanıcı
geldiğinde API'si yanlış çıkar. *Tetikleyici:* ikinci bir ekran (araç silme, şube kapatma)
onay isterse `shared/ui/ConfirmDialog` olarak terfi önerilir. *Elenen alternatif:* baştan
`shared/ui`'ye koymak — 0005'te form alanları için aynı karar verilmiş ve doğru çıkmıştı.

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler arayüz kriteridir ve `docs/testing.md` § 3 uyarınca görsel
> kanıt gerektirir. Boş durum bu ekranda uygulanabilir değildir (**C2**).

**Eylemlerin görünürlüğü ve yetki**

- **AC-1.** `[görsel]` Yönetici, personel listesinde her satır için pasifleştirme, rol
  değiştirme ve parola sıfırlama eylemlerini görür.
- **AC-2.** Satış Danışmanı rolündeki personel bu ekrana gittiğinde liste gelmez; ekran
  erişim yok durumunu gösterir ve giriş ekranına yönlendirilmez.
- **AC-3.** İçerik Editörü rolündeki personel için AC-2 ile aynı davranış geçerlidir.
- **AC-4.** Yönetici olmayan personelin panelinde personel yönetimine götüren hiçbir
  bağlantı veya eylem bulunmaz.
  *(Kriter NEGATİF bir iddiadır ve `[görsel]` DEĞİLDİR: panelde bugün navigasyon yok —
  üst bar yalnız marka, ad ve çıkış taşıyor. Olmayan bir bağlantının ekran görüntüsü
  hiçbir şey kanıtlamaz. Panel ikinci ekranını aldığında kriterin görsel yarısı ayrıca
  yazılır.)*

**Pasifleştirme**

- **AC-5.** `[görsel]` Pasifleştirme eylemi onay ister; onay metni hangi personelin
  pasifleştirileceğini adıyla yazar.
- **AC-6.** Onay verildiğinde personel pasifleşir ve liste personelin yeni durumunu gösterir.
- **AC-7.** Onay iptal edildiğinde hiçbir istek gönderilmez ve liste değişmez.

**Rol değiştirme**

- **AC-8.** Yönetici bir personelin rolünü değiştirdiğinde liste yeni rolü gösterir.
- **AC-9.** `[görsel]` Yönetici **kendi** rolünü düşürmeye çalıştığında onay istenir ve onay
  metni işlemden sonra panele erişemeyeceğini söyler.
- **AC-10.** Tanınmayan bir rol değeriyle gönderilen istek reddedilir ve ekran genel hata
  mesajını gösterir; hata kodu ziyaretçiye görünmez.

**Parola sıfırlama**

- **AC-11.** `[görsel]` Parola sıfırlama, Yöneticinin belirlediği yeni parolayı ister ve
  kaydetmeden önce aynı parolayı bir kez daha doğrulatır.
- **AC-12.** İki parola alanı eşleşmediğinde istek gönderilmez ve hata alanda gösterilir.
- **AC-13.** İşlem kaydedildikten sonra yeni parola ekranda tutulmaz.
- **AC-14.** Yapılandırılan asgari uzunluktan kısa bir parola ile işlem reddedilir ve hata
  ziyaretçinin dilinde gösterilir.

**Kendi hesabı**

- **AC-15.** `[görsel]` Ekran, listedeki kendi satırını ayırt eder.
- **AC-16.** `[görsel]` Yönetici kendi hesabını pasifleştirmeye çalıştığında onay metni
  işlemden sonra panele erişemeyeceğini söyler.
- **AC-17.** `GET /api/admin/me` yanıtı personelin kimliğini taşır (D1); `0006`'nın oturum
  kriterleri bu değişiklikten sonra da karşılanır.

**Üç red yolu**

- **AC-18.** `[görsel]` `identity.last_active_manager` döndüğünde ekran kural gereği red
  metnini gösterir ve tekrar denemeyi **önermez**.
- **AC-19.** `[görsel]` `concurrency.lost` döndüğünde ekran eşzamanlılık metnini gösterir,
  tekrar denemeyi **önerir** ve eylem tekrar denenebilir kalır.
- **AC-20.** AC-18 ve AC-19'un metinleri birbirinden farklıdır ve hiçbiri hata kodunu,
  durum kodunu, `traceId`'yi, endpoint yolunu veya istisna metnini içermez.
- **AC-21.** `409` dışında bir hata geldiğinde ekran genel hata mesajı ve **tekrar dene**
  eylemi gösterir.
- **AC-22.** Oturum düştüğünde (`401`) ziyaretçi giriş ekranına yönlendirilir.

**Liste tazeleme, seçim ve konum**

- **AC-23.** Başarılı bir işlemden sonra liste tazelenir; odak işlem yapılan satıra döner ve
  tablonun kaydırma konumu korunur.
- **AC-24.** Kural gereği reddedilen bir işlemden sonra liste **tazelenmez**; seçim ve
  kaydırma konumu korunur, liste başa sarmaz.
- **AC-25.** Eşzamanlılık kaybında liste tazelenir, hata mesajı görünür kalır ve seçim ile
  odak korunur.

**Çift gönderim**

- **AC-26.** Bir işlem sürerken o eylemin düğmesi yükleniyor hâline geçer ve aynı satır için
  ikinci istek gönderilmez.

**Çeviri**

- **AC-27.** Üç eylemin **her biri** için `last_active_manager`, `concurrency_lost` ve
  `generic` anahtarları TR, AR ve EN kataloglarında bulunur; hiçbir locale'de çeviri anahtarı
  ham hâliyle görünmez.

- **AC-33.** Aynı personel başka biri tarafından zaten pasifleştirilmişken gelen istek
  (`identity.already_deactivated`) kendi mesajını gösterir, tekrar denemeyi **önermez** ve
  liste tazelenir.

**Onay kutusu ve erişilebilirlik**

- **AC-28.** Onay kutusu açıldığında odak kutunun içine girer.
- **AC-29.** Onay kutusu Esc tuşuyla kapanır ve kapandığında odak onu açan düğmeye döner.
- **AC-30.** Onay kutusundaki her etkileşimli öğeye Tab ile ulaşılır ve Enter/Space ile
  çalıştırılır; odak halkası görünürdür.

**RTL ve üç durum**

- **AC-31.** `[görsel]` Arapça tercih eden Yönetici için ekran **ve onay kutusu** `dir="rtl"`
  ile çalışır; eylem düğmeleri ve onay metni sağdan sola hizalanır.
- **AC-32.** `[görsel]` Liste yüklenirken iskelet gösterilir; yüklenemediğinde **tekrar dene**
  eylemi sunulur.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] `[görsel]` işaretli **her** kriterin görsel kanıtı eklendi
      (`docs/evidence/0008-ac<n>.png`), hata durumları dâhil; AC-31 için Arapça görüntü de var.
- [ ] **D1'in sözleşme değişikliği dışında yeni sunucu ucu eklenmedi** (C1); `/me` yanıtına
      yalnızca `staffId` eklendi.
- [ ] **`0006`'nın oturum ve yetki testleri kesintisiz yeşil kaldı** — D1 öncesi ve sonrası
      koşu çıktıları PR'da (AC-17).
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`) — yeni uç yok; `/me` sözleşme
      testi güncellendi.
- [ ] Mimari testler yeşil — modül sınırı ihlali yok; yönetim rota kökünü tarayan test
      (`0006` C18) hâlâ yeşil.
- [ ] Yeni migration **yok**.
- [ ] C5'in tablosundaki 11 anahtar üç dilde eklendi (33); eksik çeviri anahtarı kontrolü yeşil (AC-27).
- [ ] Yeni domain terimi **yok**.
- [ ] Mimari karar verilmedi; `docs/architecture.md` § Karar Defteri değişmedi (doğrulandı).
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? |
| **Bulgu gerçek/gürültü oranı** | / | Gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. |

**Bu spec'ten çıkarılan ders:**
