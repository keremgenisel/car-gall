# 0006 — Personel Kimliği ve Oturum

| | |
|---|---|
| **Durum** | Geliştiriliyor |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Identity` (yeni — bugün hiç yok) · `frontend/` yönetim paneli kabuğu |
| **Bağımlı spec'ler** | `0003` (şubeler, tamamlandı) — şube kimliği doğrulaması için. `0005` bu spec'i **bekler**, önkoşulu değildir. |

---

## Intent

Bu sistemde bugün **hiç kimse giriş yapamıyor** — kimlik doğrulama hiçbir yerde yok.
Bunun bedeli üç spec'te üst üste yazıldı ve her seferinde aynı nedenle ertelendi:
`0003` şube yazma uçlarını, `0005` talep panelini ve danışman atamasını kapsam dışı bıraktı;
üçü de "`Identity` yok" dedi.

Şu an en görünür sonucu şudur: **ziyaretçiler talep bırakıyor ve o talepleri kimse
göremiyor.** `0005` kayıtları doğru biçimde topluyor, ama galeride onlara bakabilecek bir
kullanıcı hesabı yok.

**Galeri yöneticisi** ekibinin sisteme kendi hesabıyla girmesini istiyor; **ekip** ise
korumalı bir yönetim yüzeyinin üzerine talep, araç ve içerik ekranlarını koyabileceği bir
zemin istiyor.

Bu iş bittiğinde galeride çalışan kişilerin kendi hesapları olur, rolleriyle giriş yaparlar,
panel onların kendi dilinde açılır ve bir personel işten ayrıldığında sistem bunu **duyurur**.
Başarı, "giriş ekranı var" değil: **korumasız kalmış tek bir yönetim yüzeyi kalmaması.**

---

## Requirements

- **R1.** Sistem, galeride çalışan kişileri **personel** olarak tutar: ad, giriş kimliği
  (e-posta), parola, rol, tercih ettiği dil, durum ve opsiyonel olarak bağlı olduğu şube.
  Giriş kimliği **kültüre duyarsız biçimde küçük harfe normalize edilerek** saklanır;
  tekillik bu normalize değer üzerinden kurulur ve giriş sırasındaki eşleştirme **tam ve
  harfe duyarlı** yapılır. Geçerli biçimde olmayan bir giriş kimliği kabul edilmez.
  *(Gerekçe: Türkçe collation altında `I/ı/i/İ` sessizce yanlış eşleşir — Yasak Listesi 27.
  Burada bedeli bir liste hatası değil, iki kişinin aynı hesaba düşmesidir.)*
- **R2.** Bir personelin rolü şu üçünden biridir: **Yönetici**, **Satış Danışmanı**,
  **İçerik Editörü**. → `docs/architecture.md` § 1.
- **R3.** Personel, giriş kimliği ve parolasıyla oturum açar. **Başarısız her giriş denemesi
  ayırt edilemez bir karşılık üretir:** kimliğin kayıtlı olup olmadığı, parolanın yanlış
  olduğu, hesabın pasif olduğu veya hesabın kilitli olduğu bilgisi ziyaretçiye sızmaz —
  kilitlenme karşılığı dâhil. Kayıtlı olmayan kimlikler için de sayaç işletilir.
- **R4.** Oturum bir süre sonra kendiliğinden sona erer; personel çalışmaya devam ederken
  oturum sessizce yenilenir. Süreler **C20**'dedir.
- **R5.** Personel oturumunu kapattığında **o oturum** geçersizleşir ve yenilenemez.
  Bir personel aynı anda birden çok cihazda oturum açabilir; çıkış yalnızca o cihazın
  oturumunu kapatır.
- **R6.** Arka arkaya başarısız giriş denemesinden sonra hesap geçici olarak giriş denemesine
  kapanır. Sayaç **hesap ve kaynak IP birlikte** anahtarlanır; farklı bir kaynaktan gelen
  doğru parola, o kaynak kilitli değilse kabul edilir. Ayrıca giriş ucu **kaynak IP bazlı
  hız sınırına** tabidir. Eşikler ve süreler **C20**'dedir.
  *(Gerekçe: yalnız hesaba bağlı kilit, e-postasını bilen birinin o kişiyi kasten dışarıda
  tutmasına izin verir — son Yönetici dâhil.)*
- **R7.** Yönetim yüzeyinin tamamı oturum gerektirir. Oturumu olmayan bir kişi yönetim
  yüzeyine erişemez ve giriş ekranına yönlendirilir. Yönetim yüzeyinin sınırı **C18**'dedir.
- **R8.** Bir personelin erişebildiği yönetim işlemleri **rolüne** göre belirlenir.
- **R9.** Yönetici; personel ekleyebilir, personelin rolünü değiştirebilir, personeli
  **pasifleştirebilir** ve bir personelin **parolasını sıfırlayabilir**. Diğer roller bunu
  yapamaz. Sıfırlanan parola personelin ilk girişinde değiştirilir.
- **R10.** Bir personel eklendiğinde ilk parolası belirlenir ve personele **sistem dışından**
  iletilir. Personel ilk girişinde parolasını değiştirir. Zorunlu parola değişimi bekleyen
  personelin yalnızca **parola değiştirme** ve **çıkış** istekleri çalışır; diğer her yönetim
  isteği reddedilir.
- **R11.** Personel kendi parolasını, **mevcut parolasını vererek** istediği zaman
  değiştirebilir. Parola değiştiğinde o personelin **tüm** yenileme hakları geçersizleşir.
  *(Gerekçe: aksi hâlde parolası sızmış personel parolasını değiştirse bile saldırganın
  yenileme hakkı yaşamaya devam eder ve parola değiştirmek hiçbir şey çözmez.)*
- **R12.** Galeride her zaman **en az bir aktif Yönetici** bulunur: son aktif Yönetici
  pasifleştirilemez ve rolü düşürülemez. **"Aktif" durumu aktif olmak demektir**; geçici
  kilitlenme aktifliği etkilemez. Bir Yönetici kendi hesabını pasifleştirebilir ve kendi
  rolünü düşürebilir — son aktif Yönetici olmadığı sürece.
- **R13.** Bir personel pasifleştirildiğinde sistem bunu **duyurur**; duyuru personelin
  kimliğini ve duyuruya ait benzersiz bir kimliği taşır.
  → `docs/architecture.md` § 1 (`Identity → StaffDeactivated`).
- **R14.** Pasifleştirilen personelin açık oturumları **derhal geçersizleşir**.
- **R15.** Personel bir şubeye bağlanabilir; bağlanırken şube kimliğinin geçerli olduğu
  doğrulanır. Şubesiz personel geçerli bir durumdur.
- **R16.** Yönetim paneli, personelin **kendi tercih ettiği dilde** açılır; ziyaretçi
  locale'inden bağımsızdır. Tercih edilen dil **etkin locale listesinden** olmak zorundadır;
  verilmediğinde kurulum varsayılanı atanır, etkin olmayan bir dille personel eklenemez.
  → `docs/architecture.md` § 3.
- **R17.** Giriş ekranı **yükleniyor**, **hata** ve **kilitlenmiş hesap** durumlarını ele alır.
  → `docs/frontend.md` § 3.
- **R18.** Personel yönetimi ekranı **yükleniyor**, **boş** ve **hata** durumlarını ele alır.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Identity`, personel verisinin **tek yazma sahibidir.** Başka modüller personelin
  kim olduğunu bilmez; yalnızca `StaffId` taşırlar.
  → `docs/architecture.md` § 1, Yasak Listesi 1, 6–9.
- **C2.** **Şube bazlı yetkilendirme yoktur.** Rol modeli bir izin motoruna dönüştürülmez;
  "X şubesi danışmanı Y stoğunu düzenleyemesin" V1'de karşılanmaz.
  → `docs/architecture.md` § 6 ve § 7 (Karar Defteri: "Şube bazlı yetki | Yok (V1)").
  Personelin `BranchId`'si bir **yetki sınırı değil**, bir bağlantı bilgisidir.
- **C3.** Sistemden dışarı giden e-posta / SMS / WhatsApp yalnızca `Notifications` üzerinden
  gider. **İstisna yoktur.** → Yasak Listesi 17. *(Sonucu için bkz. Karar D1.)*
- **C4.** `Identity → Dealership` senkron çağrısı yalnızca **şube kimliği geçerli mi**
  doğrulaması içindir; tek atlamadır ve `X.Contracts` üzerinden yapılır.
  → `docs/architecture.md` § 2.A. Zincirleme çağrı (`A → B → C`) yasaktır (Yasak Listesi 13).
- **C5.** `StaffDeactivated` bir entegrasyon olayıdır: geçmiş zaman adı, record, yalnız
  primitive ve kendi DTO'ları; outbox ile, en-az-bir-kez, idempotent.
  → `docs/architecture.md` § 2.B, § 7. Yayıncı abonelerini bilmez (Yasak Listesi 18).
- **C6.** Parola hiçbir yerde geri döndürülebilir biçimde saklanmaz, loglanmaz ve API
  yanıtında bulunmaz. Saklama **hesaba özel tuz** ile ve **yavaş, parolaya özel** bir
  özetleme algoritmasıyla yapılır; algoritma ve maliyet parametresi dağıtım
  yapılandırmasından okunur. *(Gerekçe: "geri döndürülemez" tek başına hızlı bir özeti
  dışlamaz; sızan bir veritabanında fark tam olarak budur. Algoritma seçimi Developer'ın,
  sınıfı spec'in kararıdır.)*
- **C7.** Veritabanında çevrilmiş rol veya durum adı saklanmaz; kod saklanır, uçta çevrilir.
  → Yasak Listesi 22.
- **C8.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner, çeviri uçta yapılır.
  → `AGENTS.md` Altın Kural 6, Yasak Listesi 23–24, `docs/frontend.md` § 3.1.
- **C9.** Zaman damgaları ve süre hesapları UTC'dir; `DateTime.Now` veya sunucu yerel saati
  kullanılmaz. → Yasak Listesi 30.
- **C10.** `TenantId` benzeri kiracı alanı eklenmez. → Yasak Listesi 36.
- **C11.** CSS'te fiziksel yön özellikleri kullanılmaz; logical properties zorunludur.
  Panel de RTL'dir. → Yasak Listesi 25, `docs/frontend.md` § 6.
- **C12.** Ekranlar `docs/frontend.md` § 4.2 bileşen envanterini kullanır; § 4.1 gereği ham
  piksel, hex renk veya ham font boyutu eklenmez.
- **C13.** Yönetim paneli ayrı bir uygulama değildir: aynı Vite uygulamasında `/admin` rota
  ağacı, lazy-load. → `docs/architecture.md` § 7 (Karar Defteri: "Admin paneli").
- **C14.** Yeni migration yalnızca `Identity` şemasına dokunur. → Yasak Listesi 3.
- **C15.** Her kabul kriteri en az bir testle karşılanır; test `DisplayName`'i
  `AC-<n>: <kriterin metni>` biçimindedir ve metin bu spec'ten **kopyalanır**.
  → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 1.
- **C16.** **Bayat yetki penceresi yoktur.** Hesabın durumu ve rolü **her istekte kaynaktan**
  doğrulanır. Pasifleştirme, rol değişikliği ve parola değişikliği bir sonraki istekte
  etkilidir; erişim oturumunun kalan süresi bunu geciktiremez.
  *(Gerekçe: R14 "derhal" diyor; kendi kendini doğrulayan bir oturum bunu yapılandırılan
  süre boyunca yalan hâline getirir.)*
- **C17.** **Oturum taşıyıcısı ve çapraz köken.** Oturum bilgisi, sayfadaki JavaScript'ten
  okunabilir bir yerde tutulmaz. Durum değiştiren bir yönetim isteği başka bir kökenden
  tetiklendiğinde reddedilir. *(Mekanizma Developer'ın kararıdır; bu iki davranış
  gözlemlenebilir ve kriterle doğrulanır.)*
- **C18.** **Yönetim yüzeyinin sınırı bir rota kökü ile tanımlıdır** (kökün adı Developer'ın
  kararıdır, `docs/conventions.md` § 2). O kökün altındaki **her** endpoint oturum gerektirir
  ve bu, tek tek uçları sayan bir testle değil, **rotaları tarayan** bir host testiyle
  doğrulanır. *(Gerekçe: Intent'in başarı ölçüsü "korumasız kalmış tek bir yönetim yüzeyi
  kalmaması"dır; sayılamayan bir ifade bir sonraki panel spec'inde hiçbir testi kırmaz.)*
- **C19.** **Kimlik ile yetki ayrılır.** Oturumsuz istek `401`, oturumu olup yetkisi
  yetmeyen istek `403` döner (`docs/conventions.md` § 5). Uç yalnızca `401`'de giriş ekranına
  yönlendirir; `403`'te yönlendirmez. *(Gerekçe: aksi hâlde yetkisi olmayan personel
  giriş–yönlendirme döngüsüne girer.)*
- **C20.** **Süreler ve eşikler dağıtım yapılandırmasından okunur**; kabul kriterleri
  **yapılandırılan** değeri doğrular, koda gömülü bir sabiti değil. Varsayılan değerler
  D4'tedir ve değişmeleri spec revizyonu **değildir**.

### KAPSAM DIŞI

- **X1.** **Davet e-postası ve parola sıfırlama e-postası** — *neden:* `Notifications` modülü
  yoktur ve Yasak Listesi 17 istisnasızdır. Yerine geçen akış R10'dadır. → Karar D1.
- **X2.** **İki aşamalı doğrulama (2FA), SSO, OAuth, harici kimlik sağlayıcı** — *neden:*
  2–3 kişilik bir galeri ekibi için sürtünme üretir ve ayrı bir güvenlik değerlendirmesidir.
- **X3.** **Şube bazlı yetkilendirme ve kayıt seviyesi izin** — *neden:*
  `docs/architecture.md` § 6'da bilinçli kapsam dışıdır; tetikleyicisi orada yazılıdır.
- **X4.** **Public üyelik / ziyaretçi hesabı** — *neden:* § 6'da kapsam dışıdır; sistemde
  kullanıcı = personeldir (`docs/domain.md` § 3 Terim Tuzakları).
- **X5.** **Talep paneli, araç paneli, içerik paneli ekranları** — *neden:* bu spec korumalı
  **kabuğu** ve personel yönetimini üretir; her iş ekranı kendi spec'ine aittir
  (talep paneli için bkz. `0005` § X5).
- **X6.** **`Leads`'te açık taleplerin yeniden atanması** (`docs/domain.md` **L5**) —
  *neden:* atama `0005` § X2'de kapsam dışıdır; atama yoksa yeniden atama da yoktur.
  Bu spec `StaffDeactivated`'ı **yayar**, tüketicisi atama spec'iyle gelir.
  **Sonucu:** duyurunun *alıcı tarafındaki* yinelenen işleme koruması bu spec'te
  doğrulanamaz — doğrulayacak bir alıcı yoktur. Bu spec yayıncı tarafını doğrular
  (duyuru benzersiz kimlik taşır, AC-49); alıcı tarafı atama spec'ine düşer.
- **X7.** **`Audit` modülü** — *neden:* ayrı bir destek modülüdür; `StaffDeactivated`'ın
  jenerik abonesi olarak kendi spec'inde ele alınır.
- **X8.** **Personel profil fotoğrafı** — *neden:* `Media` modülü bugün yalnızca
  `Contracts`'tan ibarettir.
- **X9.** **Parola politikası zorlaması** (karmaşıklık kuralları, geçmiş parola kontrolü,
  süreli zorunlu değişim) — *neden:* asgari uzunluk dışındaki kurallar ayrı bir karardır ve
  küçük ekipte kilitlenme riski üretir.
- **X10.** Para — *neden:* bu spec'te para taşıyan hiçbir alan yoktur; `specs/TEMPLATE.md`
  § Acceptance Criteria'nın KWD kriteri bu spec için **uygulanabilir değildir.**

---

## Context

### Mevcut davranış — doğrulandı

- **`Identity` modülü yoktur.** `src/Modules/` altında `Dealership`, `Leads`, `Media`,
  `VehicleCatalog`, `VehicleReference` var.
- **Hiçbir yerde kimlik doğrulama yoktur.** `src/Host` içinde `Authentication` / `Authorize`
  eşleşmesi yok; `Program.cs`'te yalnızca `app.UseRateLimiter()` var ve o da `0005`'in
  talep formu koruması için eklenmiş (`Leads:RateLimit:*` yapılandırması,
  [Program.cs:72](src/Host/Program.cs#L72)).
- **`StaffId`, `StaffDeactivated` ve `SalesAdvisor` kodda hiç geçmiyor.** Yani bu spec
  hem kavramı hem de olayı sıfırdan var eder — `0003`'ün tersi bir durum: burada
  **konuşan yazılacak, dinleyen henüz yok** (X6).
- **Şube tarafı hazır:** `0003` tamamlandı; `Branch` ve `BranchId` gerçek. R14'ün
  doğrulaması dayanak bulur.
- **Outbox altyapısı hazır:** `0003`'ün D2 kararıyla kuruldu (`OutboxWriter`,
  `OutboxDispatcher`, `IdempotentIntegrationEventHandler`). C5 için yeniden icat gerekmez.
- **`0005` şu an `Geliştiriliyor`** ve `Leads` modülü yazılıyor. Bu spec `0005`'i bloke
  etmez; `0005`'in **değerini** açar.
- **Seed migration deseni depoda iki kez kullanılmış:** `SeedHiddenOptionalField`
  (`VehicleCatalog`) ve `SeedInitialDealership` (`Dealership`). D2'nin önerdiği yol yeni bir
  icat değildir.

### Geçerli kurallar ve kararlar

- `docs/architecture.md` § 1 — `Identity`'nin sahip olduğu veri: personel hesabı, roller,
  oturum & refresh token, davet akışı, `PreferredLocale`, opsiyonel `BranchId`.
- `docs/architecture.md` § 1 (olay akışı) —
  `Identity → StaffDeactivated → Leads (açık talepleri yeniden ata), Audit`.
- `docs/architecture.md` § 2.A — senkron çağrının üç koşulu; `Identity → Dealership`
  (şube kimliği geçerli mi) açıkça izinli örnek olarak yazılı.
- `docs/architecture.md` § 3 — personel locale'i `Identity`'de saklanır, ziyaretçi
  locale'inden bağımsızdır.
- `docs/architecture.md` § 6 / § 7 — şube bazlı yetki V1'de yok; admin paneli tek Vite
  uygulamasında `/admin` rota ağacı.
- `docs/domain.md` — **Personel** (`Staff`), **Danışman** (`SalesAdvisor`), **L5**.

### Bilinen tuzaklar

1. **Yumurta-tavuk kilidi.** İlk yönetici hesabını oluşturacak bir hesap yoksa hiç kimse
   giriş yapamaz ve sistem kalıcı olarak kapalı kalır. → Karar D2.
2. **Son yöneticinin düşmesi.** Son aktif Yöneticinin pasifleştirilmesi veya rolünün
   düşürülmesi, galeriyi yönetilemez bırakır — `docs/domain.md` **B5**'in ("son aktif şube
   kapatılamaz") kimlik tarafındaki karşılığıdır. R11 bunu yasaklar.
3. **Hangisinin yanlış olduğunu söylemek.** "Böyle bir kullanıcı yok" cevabı, hangi
   e-postaların sistemde kayıtlı olduğunu sızdırır. R3 bunu yasaklar; `0002` § C13'ün aynı
   mantığının kimlik tarafındaki karşılığıdır.
4. **Pasifleştirmenin oturumu kapatmaması.** Personel pasifleştirildiği hâlde açık
   oturumuyla çalışmaya devam ederse pasifleştirme bir güvenlik önlemi olmaktan çıkar (R13).
5. **Panelin ziyaretçi diliyle açılması.** Personel locale'i ayrı bir eksendir; karıştırmak
   Arapça vitrinden panele geçen Türk danışmanın paneli Arapça görmesine yol açar (R15).
6. **Rol adını veritabanında çevirmek.** Yasak Listesi 22; kod saklanır, uçta çevrilir (C7).
7. **Sızıntıyı kilitlenme kanalından geri açmak.** "Hangisi yanlış söylenmez" kuralı tek
   uçta tutulursa, kilitlenme cevabı hangi e-postaların kayıtlı olduğunu tam olarak
   sızdırır. Ayırt edilemezlik **tüm yanıt yüzeyinde** geçerlidir (R3).
8. **Bayat yetki penceresi.** Pasifleştirme ve rol düşürme birer güvenlik işlemidir;
   erişim oturumu kendi kendini doğruluyorsa bu işlemler süresi dolana kadar etkisiz
   kalır ve R14'ün "derhal" sözü yalan olur (C16).
9. **Giriş kimliğini kültüre duyarlı biçimde küçültmek.** `lower()` Türkçe altında
   `I → ı` üretir; `ALI@x.com` ile `ali@x.com` farklı hesaplara düşer veya çakışır (R1).

### Verilen kararlar

Dördü de Takım Yöneticisi tarafından onaylandı. Bundan sonra değişmeleri **spec
revizyonudur** (AP-08) ve SCORECARD'ın "spec revizyon sayısı" satırına yazılır.

**D1 — Davet ve parola sıfırlama e-postası yoktur; ilk parola sistem dışından iletilir.**
Yönetici personeli panelden ekler ve ilk parolayı belirler; personele bu parolayı telefon
veya WhatsApp ile iletir; personel ilk girişte parolasını değiştirir (R10).
*Gerekçe:* `Notifications` yok ve Yasak Listesi 17 istisnasız. Ayrıca bu, galerinin zaten
çalıştığı biçimdir — `docs/architecture.md` § 6 mesajlaşmayı kapsam dışı bırakırken
gerekçesini *"telefon + WhatsApp derin bağlantısı, galerinin gerçek çalışma biçimi"* diye
yazmıştı. 2–3 kişilik bir ekipte davet e-postası altyapısı, çözdüğünden fazla iş üretir.
*Bilerek ödenen bedel:* parolasını unutan personel, Yöneticiden yeni parola ister; Yönetici
yoksa D2'nin seed hesabı devreye girer.
*Elenen alternatif:* davet akışını şimdi yapıp e-postayı sonra bağlamak — yarım bir akış,
kullanılamayan bir ekran ve `Notifications` gelince yeniden yazılacak bir kod demektir.

**D2 — İlk Yönetici hesabı kurulum migration'ı ile gelir.**
*Gerekçe:* aksi hâlde hiç kimse giriş yapamaz (Tuzak 1). Seed migration deseni depoda zaten
iki kez kullanılmış. Hesabın ilk parolası dağıtım yapılandırmasından okunur ve ilk girişte
değiştirilmesi zorunludur. **Başlangıç parolası yapılandırmada yoksa uygulama başlamaz;
sessiz bir varsayılan parola üretilmez.** *Bilerek ödenen bedel:* dağıtım yapılandırmasında
bir başlangıç parolası bulunur; değiştirilmediği sürece bilinen bir parola olarak kalır —
bu yüzden ilk girişte değiştirme zorunluluğu bu kararın ayrılmaz parçasıdır.
*Elenen alternatif:* yapılandırma eksikken üretilmiş bir varsayılanla başlamak — bilinen
parolalı bir Yönetici hesabı demektir.

**D3 — Son aktif Yönetici korunur.**
Son aktif Yönetici pasifleştirilemez ve rolü düşürülemez (R11).
*Gerekçe:* Tuzak 2. Bu kural dokümanlarda **yazılı değildir**; `docs/domain.md` **B5**'in
kimlik tarafındaki karşılığı olarak öneriyorum. *Bilerek ödenen bedel:* tek yöneticili bir
galeride o kişinin hesabı hiç kapatılamaz; kapatmak için önce ikinci bir Yönetici atanmalıdır.

**D4 — Süreler ve eşikler.**
Erişim oturumu **30 dakika**, sessiz yenileme **14 gün**; çıkışta yenileme hakkı geçersizleşir.
Başarısız giriş denemesi **5**'i bulduğunda hesap **15 dakika** giriş denemesine kapanır.
Asgari parola uzunluğu **12 karakter**.
*Gerekçe:* "oturum makul sürede dolar" bir kabul kriteri değildir; sayılar olmadan R4 ve R6
test edilemez. Bu değerler yaygın ve ölçülebilir; asıl değerleri, **sayı olmaları**.
*Bilerek ödenen bedel:* 30 dakika kısa bulunursa panel gün içinde birkaç kez yenilenir;
değerler dağıtım yapılandırmasından ayarlanabilir kalmalıdır.
*Not:* bu değerler dağıtım yapılandırmasından okunur; değişmeleri spec revizyonu değildir.
Bu yüzden **kriter metinleri sayı içermez** — "yapılandırılan eşik / yapılandırılan süre"
derler ve testler yapılandırmayı sabitleyip doğrular (**C20**). Aksi hâlde C15 gereği test
adına kopyalanan sabit sayı, yapılandırma değiştiği gün yalan söylerdi.

---

## Acceptance Criteria

> Süre ve eşik içeren kriterler **yapılandırılan** değeri doğrular, koda gömülü bir sabiti
> değil (**C20**). `[görsel]` işaretli kriterler arayüz kriteridir ve `docs/testing.md` § 3
> uyarınca görsel kanıt gerektirir.

**Oturum açma**

- **AC-1.** Geçerli kimlik ve parolayla giriş yapan personel yönetim paneline erişir.
- **AC-2.** Kayıtlı olmayan bir kimlikle giriş denendiğinde giriş reddedilir.
- **AC-3.** Kayıtlı bir kimlikle yanlış parola girildiğinde giriş reddedilir.
- **AC-4.** Pasifleştirilmiş bir personel giriş yapamaz.
- **AC-5.** AC-2, AC-3 ve AC-4'te dönen cevap **birbirinden ayırt edilemez**: aynı hata
  kodu, aynı gövde.
- **AC-6.** Yapılandırılan eşik kadar arka arkaya başarısız giriş denemesinden sonra hesap
  yapılandırılan süre boyunca giriş denemesine kapanır; bu süre içinde doğru parola girilse
  de giriş reddedilir.
- **AC-7.** Kilitlenme süresi dolduktan sonra doğru parolayla giriş başarılı olur.
- **AC-8.** Kayıtlı **olmayan** bir kimlikle eşik sayıda başarısız denemeden sonra dönen
  cevap, kayıtlı bir kimlik için dönenle ayırt edilemez.
- **AC-9.** Kilitlenme sayacı hesap ve kaynak IP birlikte anahtarlanır: bir kaynaktan
  kilitlenen hesaba, kilitli olmayan başka bir kaynaktan doğru parolayla giriş yapılabilir.
- **AC-10.** Giriş ucuna aynı kaynak IP'den yapılandırılan hız sınırının üzerinde istek
  geldiğinde sonraki istekler reddedilir.

**Giriş kimliği**

- **AC-11.** `Ali@x.com` kimliğiyle eklenen personel `ali@x.com` yazarak giriş yapar.
- **AC-12.** `Ali@x.com` kayıtlıyken `ali@x.com` ile ikinci bir personel eklenemez.
- **AC-13.** `İ` içeren bir giriş kimliği kendi yazımıyla eşleşir ve `i` içeren başka bir
  hesaba düşmez.
- **AC-14.** Geçerli biçimde olmayan bir e-posta ile personel eklenemez.

**Oturumun sürmesi ve bitmesi**

- **AC-15.** Erişim oturumu yapılandırılan süre sonunda geçerliliğini yitirir.
- **AC-16.** Süresi dolmuş erişim oturumu, geçerli bir yenileme hakkı varken sessizce
  yenilenir ve personel işini kaybetmez.
- **AC-17.** Yenileme hakkı yapılandırılan süre sonunda geçerliliğini yitirir; personel
  yeniden giriş yapar.
- **AC-18.** Personel çıkış yaptığında o oturumun yenileme hakkı geçersizleşir ve aynı hakla
  yeni oturum alınamaz.
- **AC-19.** Çıkış yalnızca o oturumu kapatır; aynı personelin başka bir cihazdaki oturumu
  çalışmaya devam eder.
- **AC-20.** Bir personel pasifleştirildiğinde açık oturumu derhal geçersizleşir; sonraki ilk
  istekte yönetim yüzeyine erişemez.
- **AC-21.** Bir personelin rolü düşürüldüğünde eski rolün yetkisi sonraki ilk istekte
  çalışmaz.
- **AC-22.** Bir personelin parolası değiştiğinde o personelin **tüm** yenileme hakları
  geçersizleşir; başka cihazdaki oturumu da yenilenemez.

**Erişim koruması**

- **AC-23.** Yönetim rota kökü altındaki **her** endpoint, oturumsuz istekte `401` döner;
  bu, rotaları tarayan bir testle doğrulanır.
- **AC-24.** `[görsel]` Oturumu olmayan biri yönetim ekranına gittiğinde giriş ekranına
  yönlendirilir.
- **AC-25.** Satış Danışmanı rolündeki personel, personel ekleme ve rol değiştirme
  işlemlerini yapamaz; istek `403` ile reddedilir.
- **AC-26.** İçerik Editörü rolündeki personel, personel ekleme ve rol değiştirme
  işlemlerini yapamaz; istek `403` ile reddedilir.
- **AC-27.** `403` alan uç giriş ekranına yönlendirmez; personel bulunduğu ekranda kalır.
- **AC-28.** Yönetici rolündeki personel, personel ekleme ve rol değiştirme işlemlerini yapar.

**Oturum taşıyıcısı ve çapraz köken**

- **AC-29.** Oturum bilgisi, sayfadaki JavaScript'ten okunabilir bir yerde tutulmaz.
- **AC-30.** Durum değiştiren bir yönetim isteği başka bir kökenden tetiklendiğinde
  reddedilir.

**Personel yönetimi**

- **AC-31.** Yönetici bir personel eklediğinde personel listede görünür ve verilen kimlikle
  giriş yapabilir.
- **AC-32.** `[görsel]` İlk parolasıyla giriş yapan personelden parolasını değiştirmesi
  istenir; değiştirmeden yönetim yüzeyinde çalışamaz.
- **AC-33.** Zorunlu parola değişimi bekleyen personelin yalnızca parola değiştirme ve çıkış
  istekleri çalışır; diğer her yönetim isteği reddedilir.
- **AC-34.** Personel kendi parolasını mevcut parolasını vererek değiştirir; mevcut parola
  yanlış verildiğinde değişiklik reddedilir.
- **AC-35.** Yönetici bir personelin parolasını sıfırlar; personel sıfırlanan parolayla giriş
  yaptığında kendisinden parola değiştirmesi istenir.
- **AC-36.** Yapılandırılan asgari uzunluktan kısa bir parola kabul edilmez.
- **AC-37.** Aynı parolayı kullanan iki personelin saklanan parola özetleri birbirinden
  farklıdır.
- **AC-38.** Personel bir şubeye bağlanarak eklenebilir; var olmayan bir şube kimliğiyle
  eklenemez.
- **AC-39.** Şubesiz personel eklenebilir; şube zorunlu değildir.
- **AC-40.** Etkin locale listesinde olmayan bir tercih edilen dille personel eklenemez.
- **AC-41.** Tercih edilen dil verilmeden eklenen personele kurulum varsayılan dili atanır.

**Son Yönetici koruması**

- **AC-42.** Galerideki son aktif Yönetici pasifleştirilemez; girişim reddedilir.
- **AC-43.** Galerideki son aktif Yöneticinin rolü başka bir role düşürülemez; girişim
  reddedilir.
- **AC-44.** İki aktif Yöneticiden biri pasifleştirildiğinde işlem başarılı olur ve geriye bir
  aktif Yönetici kalır.
- **AC-45.** Geçici olarak kilitlenmiş bir Yönetici **aktif** sayılır: iki Yöneticiden biri
  kilitliyken diğeri pasifleştirilemez.
- **AC-46.** Bir Yönetici kendi hesabını pasifleştirebilir ve kendi rolünü düşürebilir —
  son aktif Yönetici olmadığı sürece.

**Duyuru**

- **AC-47.** Bir personel pasifleştirildiğinde `StaffDeactivated` duyurusu üretilir ve
  personelin kimliğini taşır.
- **AC-48.** Personel değişikliği kaydedildiğinde duyurusu da kaydedilmiştir; değişiklik geri
  alındığında kaydedilmiş bir duyuru kalmaz.
- **AC-49.** Üretilen her duyuru benzersiz bir duyuru kimliği taşır.

**Parola ve sızıntı**

- **AC-50.** Personel kaydının döndüğü hiçbir yanıt parolayı veya parola özetini içermez.
- **AC-51.** Parola hiçbir log kaydında görünmez.
- **AC-52.** Veritabanında rol ve durum kod olarak saklanır; çevrilmiş adları saklanmaz.

**Kurulum**

- **AC-53.** Başlangıç Yönetici parolası dağıtım yapılandırmasında yokken uygulama başlamaz;
  üretilmiş bir varsayılan parolayla açılmaz.

**Panel: dil, durum ve RTL**

- **AC-54.** `[görsel]` Yönetim paneli, giriş yapan personelin tercih ettiği dilde açılır;
  ziyaretçinin o anki vitrin dili paneli etkilemez.
- **AC-55.** `[görsel]` Arapça tercih eden personel için panel `dir="rtl"` ile basılır.
- **AC-56.** `[görsel]` Giriş yapılırken giriş düğmesi yükleniyor hâline geçer ve form iki kez
  gönderilemez.
- **AC-57.** `[görsel]` Giriş başarısız olduğunda ekran, çeviri kataloğundaki hata mesajı
  anahtarının karşılığını gösterir; girilen kimlik kaybolmaz, parola alanı temizlenir.
- **AC-58.** `[görsel]` Hesap kilitlendiğinde ekran, ne olduğunu ve ne zaman tekrar
  denenebileceğini söyler. *(Bu metin AC-8 gereği kayıtlı olmayan kimlikler için de aynı
  şekilde çıkar, dolayısıyla ayırt edici değildir.)*
- **AC-59.** Giriş ve personel yönetimi ekranlarının hiçbir hata durumunda HTTP durum kodu,
  hata kodu, `traceId`, endpoint yolu veya istisna metni görünmez.
- **AC-60.** `[görsel]` Personel listesi yüklenirken iskelet gösterilir; yüklenemediğinde
  **tekrar dene** eylemi sunulur.
- **AC-61.** `[görsel]` Giriş ekranı ve personel yönetimi ekranındaki tüm metinler TR, AR ve
  EN locale'lerinde kendi dilinde görünür; hiçbir locale'de çeviri anahtarı ham hâliyle
  görünmez.

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] `[görsel]` işaretli **her** kriterin görsel kanıtı eklendi
      (`docs/evidence/0006-ac<n>.png`), hata ve kilitlenme durumları dâhil; AC-55 için
      Arapça görüntü de var.
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — `Identity` başka modülün tablosuna, internal tipine veya
      `DbContext`'ine erişmiyor; `Identity → Dealership` tek atlama.
- [x] Yeni migration yalnızca `Identity` şemasına dokunuyor.
- [x] Seed edilen ilk Yönetici hesabının parolası dağıtım yapılandırmasından geliyor ve ilk
      girişte değiştirilmesi zorunlu (D2).
- [x] Parolanın yanıtta, logda ve hata metninde görünmediği testle gösterildi (AC-50, AC-51).
- [x] Yönetim rota kökü altındaki her endpoint'in korumalı olduğu, rotaları **tarayan** bir
      testle gösterildi (C18, AC-23) — tek tek sayılan uçlarla değil.
- [x] Süre ve eşik değerleri dağıtım yapılandırmasından okunuyor; hiçbiri koda gömülü değil
      (C20).
- [x] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [x] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır.
- [x] Ekranlar `docs/frontend.md` § 4.2 envanterini kullanıyor; ham piksel/hex eklenmedi.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **1** | 2026-09-22, AP-08. Uygulama başlamadan önceki CLARIFY turu: 19 açık soru, hepsi onaylanıp işlendi. Kriter sayısı 41 → 61. Altısının sonucu doğrudan güvenlikti; en pahalısı, ayırt edilemezlik kuralının kilitlenme kanalından geri açılmasıydı. |
| **Düzeltme turu sayısı** | **2** | (1) CI'ın E2E ve frontend işleri kırmızıydı: kurulum parolası iki yerde ayrı ayrı yazılmıştı ve ölü fonksiyon temizliği çalışan iki yardımcıyı da silmişti. (2) Triyajdan dört gerçek bulgu geldi — hepsi düzeltildi. |
| **Bulgu gerçek/gürültü oranı** | 11 / 0 | Uygulama sırasında çıkan altı bulgunun altısı da gerçekti: (1) `/admin` bugüne kadar `404` dönüyordu — locale ara katmanı ilk segmenti dil sanıyordu; (2) giriş istemci tarafında olduğu için panel personelin dilinde AÇILMIYORDU; (3) Satış Danışmanı panelde "getirilemedi, tekrar dene" hatası görüyor ve personel ekleme formu ona da basılıyordu; (4) `TypedResults.Problem` gövdeye çevrilmiş İngilizce `title` koyuyordu; (5) kurulum Yönetici hesabını migration'ın İÇİNE koymak teknik olarak imkânsızdı (migration çalışma anı yapılandırmasını okuyamaz); (6) E2E yardımcılarında iki yarış — `isVisible()` beklemiyor ve `page.goto` tam gezinmeyi yarıda kesiyordu; (7) "Performans" iş akışı 0006'nın değiştirdiği başlangıç sözleşmesi yüzünden hiç tamamlanmıyordu ve sınırsız bekleme döngüsü bunu SESSİZ bırakıyordu. **Triyajdan dört bulgu daha, dördü de gerçek:** (8) Host `dealership` şemasını hiç uygulamıyordu — şubeli personel ekleme temiz kurulumda 500 veriyordu ve AC-38'in iki testi de sahte doğrulayıcı kullandığı için bunu göremiyordu; (9) son aktif Yönetici koruması eşzamanlı iki isteğe açıktı — ikisi de geçebiliyor ve galeri kalıcı olarak kapanabiliyordu; (10) personel listesi bütün `Staff` entity'sini, parola özeti dâhil, belleğe çekiyordu; (11) `PasswordChangeGate`'te hiçbir zaman çalışmayan bir koşul, yorumun vaat ettiği korumayla çelişiyordu. |
| **Regresyon sayısı** | 1 | `LocaleYonlendirmeTests` kırıldı: Host artık başlangıçta `identity` şemasını uygulayıp kurulum hesabını garanti ediyor ve o testler veritabanına hiç dokunmuyordu. Teste özel bir bayrakla atlamak yerine fabrikaya gerçek veritabanı verildi — bayrak, üretimde kapalı kalabilecek bir yol açardı. |
| **Kaçan hata** | - | Henüz bilinmiyor. |

**Bu spec'ten çıkarılan ders:**
**Bir güvenlik kuralı, kablolandığı yerde test edilmezse yalnızca niyettir.** 0006'da koruma
iki parçaya bölündü: şema ve politikalar modülde, boru hattı Host'ta. Modül testleri
şemanın doğru olduğunu gösteriyordu ve hepsi yeşildi — ama `UseAuthentication()` satırı
silinse yönetim yüzeyinin tamamı korumasız kalırdı ve hiçbiri kırmızıya dönmezdi. Bunu
gösteren tek test, rotaları tarayıp gerçek boru hattına istek atan Host testi oldu; sonda
olarak gruba takılmamış korumasız bir uç eklendiğinde de yalnızca o kırmızı verdi.

Üçüncü yarısını triyaj yazdırdı ve aynı cümlenin tersi: **sahte bir komşu, kabloyu test
etmez.** AC-38'in iki testi de `FakeBranchValidator` kullanıyordu; sözleşmeyi doğruluyor
ama `Identity` → `Dealership` atlamasını hiç geçmiyorlardı. Şemanın üretim yolunda hiç
uygulanmadığı, ancak gerçek boru hattına gerçek bir istek atıldığında görüldü — ve o istek
`42P01` ile çöküyordu. Modül sınırı testte sahtelemeyi ZORUNLU kılıyor (`docs/testing.md`
§ 1); o zaman kabloyu sınayan testin `Host.Tests`'te yaşaması da zorunlu.

İkinci yarısı aynı derse ait: **sayılamayan bir ifade hiçbir testi kırmaz.** Intent'in
ölçüsü "korumasız kalmış tek bir yönetim yüzeyi kalmaması"ydı ve bu, uçları elle sayan
bir testle karşılanamazdı — bir sonraki panel spec'i yeni bir uç eklediğinde o test
görmeden yeşil kalırdı. Tarama, hiç uç bulamadığında da yeşil kalacağı için bulduğu uç
sayısının sıfır olmadığını da doğrulamak zorunda.
