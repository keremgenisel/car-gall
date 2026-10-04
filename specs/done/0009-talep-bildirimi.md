# 0009 — Talep Bildirimi

| | |
|---|---|
| **Durum** | **Tamamlandı** |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Notifications` (yeni) · `Leads.Contracts` (sorgu yüzeyi — **D1**) · `Dealership.Contracts` (şube iletişimi — **D2**) · `VehicleCatalog.Contracts` (ilan slug'ı — **D5**) |
| **Bağımlı spec'ler** | `0005` (talep oluşturma — tamamlandı) · `0003` (şubeler — tamamlandı) |

> **Bu bir Analist taslağıdır.** Requirements, Constraints ve Context doğrulanmış
> bilgiden yazıldı. Dört açık soru **D1–D4 olarak karara bağlandı** (Context § Verilen
> kararlar) ve kabul kriterleri tamamlandı.

---

## Intent

Bugün bir ziyaretçi vitrinde bir araç bulup talep bıraktığında, **galeride kimse bunu
bilmiyor.** Talep veritabanına doğru biçimde yazılıyor, olayı da yayılıyor — ama o olayın
abonesi yok. Galeri çalışanı talebi görmek için paneli açıp bakmak zorunda; bakmazsa talep
orada bekliyor.

İkinci yarısı ziyaretçi tarafında: bilgilerini bırakan kişi **hiçbir karşılık almıyor**.
Formu gönderdi mi, ulaştı mı, biri arayacak mı — bilmiyor. Araç almak, insanın birkaç gün
içinde birden fazla galeriye baktığı bir iş; sessiz kalan galeri unutulan galeridir.

Bu iş bittiğinde talep bırakılır bırakılmaz galeri haberdar olur ve ziyaretçi talebinin
alındığını görür. **Başarı ölçütü:** bir talebin bırakılmasından galerinin haberdar
olmasına kadar geçen süre, panelin elle açılmasına bağlı olmaktan çıkar.

Bu spec `0005`'in PR'ında açıkça yazılmış borcu kapatıyor: *"talepler doğru biçimde
birikiyor ama kimse okuyamıyor."*

---

## Requirements

- **R1.** Bir talep oluşturulduğunda galeri tarafına bildirim gider.
- **R2.** Bildirim, talebin **şubesinin adını**, aracın **model adını** ve ilanın **detay
  sayfasına giden bağlantıyı** taşır. *(Ham kimlik yeterli değildir: bir `Guid` galeri
  çalışanına hiçbir şey söylemez.)*

  **Üçünün kaynağı ayrıdır ve taslaktaki "üçü de D1'in sorgusundan gelir" cümlesi yanlıştı
  — doğrulandı:**

  | Bilgi | Kaynak | Durum |
  |---|---|---|
  | Şube adı | `Dealership` — `BranchSummaryDto.Name` | Bugün var |
  | Model adı | `Leads` — `VehicleSnapshot.CanonicalModelName` | D1'in sorgusundan |
  | Detay bağlantısı | `VehicleCatalog` — slug (**D5**) | Hiçbir yerde yoktu |

  Detay rotası slug tabanlı; `VehicleSnapshot` slug tutmuyor ve `ListingId` için *"bir OKUMA
  YOLU DEĞİLDİR"* diyor. Üçü de **tek atlama** okumadır; iki atlamalı zincir yoktur (C4).
- **R3.** Ziyaretçiye, talebinin alındığını bildiren bir karşılık gider — **ve bu
  karşılık ziyaretçinin talebi bıraktığı dilde olur.** → `docs/domain.md` **L1**
- **R4.** Ziyaretçiye giden karşılık **söz vermez**: ne zaman dönüleceğine dair taahhüt
  içermez. → `0005` Karar **D6**
- **R5.** Galeri tarafına giden bildirim **kurulum varsayılan dilinde** gönderilir.
  *(Alıcı bir adrestir, bir kişi değil — `PreferredLocale` yoktur. Atama geldiği gün
  danışmanın diline geçilir → X3.)*
- **R6.** Ziyaretçinin **e-posta adresi yoksa** — `0005` R4 telefonu tek iletişim kanalı
  olarak kabul ettiği için bu geçerli bir durumdur — ziyaretçi karşılığı **gönderilmez**
  ama gönderim geçmişine *"kanal yok"* sonucuyla **kaydedilir**. Galeri bildirimi normal
  gider. *(Bu bir hata değil, eksik kanaldır; kaydı tutulmazsa galeri "neden ulaşılmadı"
  sorusunu hiçbir yerden cevaplayamaz.)*
- **R7.** Galeri bildirimi ile ziyaretçi karşılığı **bağımsız** iki mesajdır; birinin
  başarısızlığı diğerinin gönderimini engellemez.
- **R8.** Dışarıya giden her mesaj `Notifications` modülünden geçer. Başka hiçbir modül
  doğrudan e-posta/SMS/WhatsApp göndermez. → Yasak Listesi **17**
- **R9.** Şablonlar **(anahtar × locale)** olarak tutulur. İstenen dilde şablon yoksa
  varsayılan dile düşülür ve bu durum loglanır. **Varsayılan dilde de şablon yoksa gönderim
  başarısız işaretlenir**; ham şablon anahtarı veya boş gövde **gönderilmez**.
  → `docs/architecture.md` § 3. *(Gerekçe: `lead.received` yazan bir e-posta,
  gönderilmemiş olmaktan kötüdür.)*
- **R10.** Mesajlar bir **gönderim kuyruğunda** yaşar ve kuyruğu bir **arka plan servisi**
  işler. Bekleyen gönderimler süreç yeniden başladıktan sonra kaybolmaz.
  *(Gerekçe: `Leads`'in outbox dağıtıcısı olayı bir kez teslim eder; beşinci denemeyi
  yapacak olan o değildir. Tekrar deneme bir kuyruk olmadan karşılanamaz.)*
- **R11.** Gönderim hataları **iki sınıfa** ayrılır:
  - **Geçici** (ağ, zaman aşımı, hız sınırı, sağlayıcı `5xx`, yapılandırılan zaman aşımının
    aşılması) → yapılandırılan sayıda, **artan aralıkla** yeniden denenir; denemeler
    tükendiğinde kayıt **başarısız** işaretlenir ve loglanır, sessizce düşmez.
  - **Kalıcı** (adres reddedildi veya geçersiz) → yeniden **denenmez**, kayıt derhal
    başarısız işaretlenir.

  *(Gerekçe: geçersiz bir adrese beş kez denemek kuyruğu kalıcı hatalarla doldurur ve
  gerçek geçici hataların denemelerini geciktirir.)*
- **R12.** Gönderim geçmişi her mesaj için **`LeadId`**, şablon anahtarı, locale, kanal,
  sonuç ve zaman tutar. **Alıcının adını, e-postasını veya telefonunu tutmaz** — bkz. C5.
- **R13.** İletişim izni (`ConsentGiven`) olmayan ziyaretçiye **pazarlama** mesajı
  gönderilmez; talebin kendisine dair **işlemsel** karşılık gönderilir.
  → `docs/domain.md` **L7**. *(Bu spec'te pazarlama şablonu zaten yoktur → X2.)*
- **R14.** Bildirim gönderimi talebin kaydedilmesini **engellemez**: bildirim
  başarısız olsa bile talep kaydı durur. → `docs/architecture.md` § 2.B

---

## Constraints

### Uyulması zorunlu

- **C1.** `Notifications` dışından doğrudan gönderim yasaktır — istisnası yoktur.
  → Yasak Listesi **17**
- **C2.** Modül sınırı: `Notifications`, `Leads` veya `Dealership` tablolarına
  erişemez. Erişim yalnız `X.Contracts` üzerinden. → Yasak Listesi **1, 7**
- **C3.** Tüketim **outbox üzerinden** ve **idempotent**'tir: aynı olay iki kez
  gelebilir ve ikinci gelişte ikinci mesaj gönderilmez. → `docs/architecture.md` § 2.C
- **C4.** Senkron çağrı yalnız **tek atlama** ve salt okuma olabilir.
  `Notifications → Leads → VehicleCatalog` zinciri **yasaktır**. → Yasak Listesi **13**
- **C5.** **Kişisel veri ne loga ne gönderim geçmişine yazılır.** Gönderim geçmişi alıcı
  adresini **tutmaz**; adres gönderim anında kaynaktan okunur ve kayda yazılmaz (R12).
  Ayrıca gönderim yolundaki log çağrıları alıcı adını, adresini veya telefonunu **parametre
  olarak almaz** — bu, çağrı yüzeyini tarayan bir testle doğrulanır.
  → `docs/conventions.md` § 12.
  *(Gerekçe: adres kayıtta tutulursa `Lead.Anonymize()` çalıştığı gün kişisel veri
  `notifications` şemasında durmaya devam eder ve `docs/domain.md` **L6** kâğıt üstünde
  kalır — `LeadCaptured`'ın doc comment'inin tam olarak uyardığı hata, başka bir modülde.
  Elenen alternatif: adresi tutup `LeadAnonymized` olayını dinlemek — öyle bir olay yok, X4.
  Bedel: "hangi adrese gitti" sorusu kayıttan değil, yalnız sağlayıcı günlüğünden
  cevaplanır.)*
- **C6.** Kullanıcıya görünen hiçbir metin koda gömülmez; mesaj gövdeleri şablondan
  üretilir. → `AGENTS.md` Altın Kural 6
- **C7.** Sayı veya çoğul içeren cümleler dize birleştirmeyle kurulmaz; ICU mesaj
  formatı zorunludur. → Yasak Listesi **31**
- **C8.** Sağlayıcı kimlik bilgileri (SMTP parolası, API anahtarı) repoda durmaz;
  yapılandırmadan gelir ve eksikse uygulama **ayağa kalkmaz**.
  → `docs/conventions.md` § 11
- **C9.** **Zorunlu yapılandırma.** Şunlar dağıtım yapılandırmasından okunur: kurulum
  bildirim adresi, bağlantıların **taban URL**'i, yeniden deneme sayısı ve aralığı, gönderim
  zaman aşımı. **Kurulum bildirim adresi veya taban URL eksikse uygulama ayağa kalkmaz.**
  Kabul kriterleri **yapılandırılan** değeri doğrular, koda gömülü bir sabiti değil
  (`0006` C20 deseni).
  *(Gerekçe: bildirim adresi yapılandırılmamışsa her talep sessizce kaybolur — bu spec'in
  varlık nedeni tam olarak o durumdur ve bu kez çalıştığı sanılır. Taban URL eksikse
  e-postadaki bağlantı göreli kalır ve tıklanamaz.)*
- **C10.** **Tam-bir-kez teslim vaat edilmez.** Mesaj **önce** kuyruğa kaydedilir, **sonra**
  gönderilir, **sonra** sonucu işaretlenir. Gönderim ile sonucun işaretlenmesi arasında
  süreç düşerse mesaj tekrar gönderilebilir; bu **bilerek kabul edilir**. Garanti edilen
  şey, aynı olayın ikinci işlenmesinde **ikinci bir gönderim kaydı oluşmamasıdır**.
  *(Gerekçe: en-az-bir-kez teslim ile dış yan etki birlikteyken tam-bir-kez imkânsızdır;
  kriterin tutamayacağı bir söz vermesi, tanım gereği kırılabilir bir kriter üretir.)*
- **C11.** `Notifications` bir **yaprak** modüldür: hiçbir modül ona senkron bağımlı
  olmaz ve o da hiçbir modüle olay yayınlamaz. → `docs/architecture.md` § 1
- **C12.** Şema `notifications`; migration yalnız o şemaya dokunur.
- **C13.** **Şablonlar kurulum verisidir** ve `notifications` şemasına **seed migration**
  ile gelir (0003/0006 deseni). Eksik şablon anahtarı kontrolü **backend tarafında bir
  testle** yapılır; frontend'in `check:locales` kapısı bu şablonları **görmez**
  (`docs/frontend.md` § 8 arayüz kataloglarını kapsar).

### KAPSAM DIŞI

- **X1.** SMS ve WhatsApp kanalları — *neden:* her kanal kendi sağlayıcı sözleşmesini,
  kendi hata biçimini ve kendi maliyetini getirir. Kanal soyutlaması bu spec'te kurulur
  ama V1'de **yalnız e-posta** uygulanır.
- **X2.** Abonelikten çıkma (unsubscribe) akışı ve bağlantısı — *neden:* yalnız işlemsel
  mesaj gönderildiği sürece (R13) gerekli değil; pazarlama mesajı girdiği gün zorunlu
  hâle gelir ve o zaman kendi spec'ini hak eder.
  **Sonucu:** bu spec'te **pazarlama şablonu yoktur.** Dolayısıyla "pazarlama mesajı
  gönderilmez" bir kabul kriteri değildir — doğrulanacak bir şey yoktur; kriter yalnız
  işlemsel karşılığın `ConsentGiven=false` iken de gittiğini doğrular (AC-10).
- **X3.** Danışmana kişisel bildirim ve talep ataması — *neden:* **talep ataması henüz
  yok.** `docs/domain.md` **L3** şubeye yönlendirmeyi söylüyor ama `0005` atamayı
  uygulamadı ve `LeadAssigned` olayı mevcut değil (doğrulandı).
- **X4.** `StaffDeactivated` ve talep durumu değişikliği bildirimleri — *neden:*
  `LeadAssigned` / `LeadStatusChanged` olayları yayınlanmıyor (doğrulandı). Bu spec
  yalnız **`LeadCaptured`** tüketir.
- **X5.** Bildirim şablonlarının panelden düzenlenmesi — *neden:* şablon düzenleme
  ekranı kendi yetki ve önizleme kriterlerini getirir; şablonlar V1'de kurulum verisidir.
- **X6.** Zengin HTML şablonu, marka tasarımı, e-posta istemci uyumluluk matrisi —
  *neden:* düz metin + asgari HTML, bildirimin işini yapar; tasarım ayrı bir iştir.
- **X7.** Teslim takibi (açıldı mı, tıklandı mı) — *neden:* izleme pikseli ve tıklama
  yönlendirmesi KVKK açısından ayrı bir karar.

---

## Context

### Doğrulanmış mevcut durum

- **`Leads` yalnız bir olay yayıyor:** `LeadCaptured(EventId, OccurredAt, LeadId,
  BranchId, ListingId, LeadType, SubmittedLocale)`
  ([LeadEvents.cs](src/Modules/Leads/Leads.Contracts/Events/LeadEvents.cs)).
  **Abonesi yok.**
- **`Leads.Contracts`'ta sorgu arayüzü YOK.** Public yüzeyde yalnız bu olay var —
  yani `Notifications` bugün talebin iletişim bilgisine veya araç anlık görüntüsüne
  **public sözleşme üzerinden erişemez**.
- **Olay kişisel veri taşımıyor ve bu YAZILI bir karar** — görünüş değil. `LeadEvents.cs`
  doc comment'i şöyle diyor: *"KİŞİSEL ALAN TAŞIMAZ — ve taşımamalıdır. Outbox satırı
  yayıncının kendi şemasında `jsonb` olarak KALICIDIR… Alan SONRADAN EKLEMEK ucuz, geçmişten
  SİLMEK imkânsızdır."* Ve tüketiciyi de öngörüyor: *"Geldiğinde talebi `LeadId` ile okur;
  kişisel veriyi olaydan değil kaynaktan alır."*
  **Sonucu:** D1'in (a) seçeneği — olayı büyütmek — zaten dışlanmış bir karardır; D1 o
  kararı yeniden vermiyor, uyguluyor.
- `Lead` agregatında şunlar var: `Contact`, `VehicleSnapshot`, `BranchId`,
  `SubmittedLocale`, `ConsentGiven`, `CapturedAt`, `Status` — hepsi `internal`.
- **`Dealership.Contracts` şube e-postası vermiyor:**
  `BranchSummaryDto(BranchId, Name, Status)` — iletişim alanı yok (doğrulandı).
  Şube kendi şemasında e-posta ve telefon tutuyor (`0003`), ama dışa açılmamış.
- `Identity` personelde `PreferredLocale` tutuyor ve `StaffDeactivated` yayıyor.
- Outbox altyapısı `0003` ile kuruldu ve `Leads`'te çalışıyor; dağıtıcı, tekrar deneme
  ve idempotentlik desenleri mevcut.

### Bu spec'in gerçek teslimatı

Modül kurmak kolay kısmı. Zor kısmı şu soru: **`Notifications` mesajı neyle yazacak?**
Olay yalnız kimlik taşıyor; mesaj ise insan okuyacak bir metin. Aradaki boşluğu kapatmanın
üç yolu var ve üçü farklı bedel ödüyor — **Açık Soru 1**.

---

## Verilen kararlar

Dördü de Takım Yöneticisi tarafından onaylandı. Bundan sonra değişmeleri **spec
revizyonudur** (AP-08).

### D1 — `Notifications` mesaj içeriğini `Leads.Contracts`'ın sorgu yüzeyinden okur

| | Yol | Bedel | Sonuç |
|---|---|---|---|
| a | `LeadCaptured` olayı büyür: iletişim bilgisi ve anlık görüntü taşır | Kişisel veri outbox'ta ve olay günlüğünde kalıcı olur | **Elendi — ve zaten yazılı bir kararla dışlanmıştı** (Context) |
| **b** | `Leads.Contracts` bir sorgu arayüzü kazanır; `Notifications` gönderim anında senkron okur | `Leads`'in public yüzeyi büyür; kişisel veri yalnız gönderim anında ve bellekte | **Seçildi** |
| c | Mesaj kişisel veri taşımaz: galeriye *"panele bak"* gider | R2'yi karşılamaz, R3 hiç gönderilemez | Elendi |

*Gerekçe:* Kişisel veriyi olay günlüğüne yazmak geri alınamaz; bir kez outbox'a girdi mi
KVKK silme talebi olay geçmişini de kapsar. Senkron okuma `architecture.md` § 2.A'nın tam
olarak izin verdiği şeydir: salt okuma, tek atlama, aynı istek içinde.
*Bilerek ödenen bedel:* `Leads.Contracts` büyür ve `Notifications → Leads` kenarı doğar —
bağımlılık haritasına yazılması gerekir (DoD).

### D2 — Galeri bildirimi şubenin e-postasına, yoksa kurulum adresine gider

`Dealership.Contracts` şube iletişim bilgisini veren bir alan kazanır; `0003` e-posta
alanını zaten koydu ama dışa açmadı. Şubenin e-postası yoksa kurulum bildirim adresine
düşülür (C9 bu adresi zorunlu kılar). Danışmana kişisel bildirim **yapılamaz** — atama yok
(X3). *Bedel:* iki modülün contract'ı büyür; `Notifications → Dealership` kenarı doğar.

### D3 — Ziyaretçiye karşılık V1'de **vardır**

*Gerekçe:* Intent'in ikinci yarısı bu; sessiz kalan galeri unutulur. Teknik olarak galeri
bildirimiyle aynı yoldan geçer, ek maliyeti düşük. *Sonucu:* ziyaretçinin e-posta adresi
`Notifications`'a ulaşmak zorunda — bu da D1'i (b)'ye kilitler. *Bedel:* e-postası olmayan
talep bir kenar durum olarak ele alınmak zorunda kalır (R6).

### D4 — SMTP ile başlanır, gönderici bir soyutlamanın arkasında durur

*Gerekçe:* Kurulum başına ayrı dağıtım yapıyoruz; her galeriye ayrı bir işlemsel e-posta
servisi hesabı açmak operasyon yüküdür. Gönderici soyutlamanın arkasında kalırsa servise
geçiş tek bir sınıfı değiştirir. *Bilerek ödenen bedel:* SMTP'de teslim oranı daha düşüktür
ve spam klasörü riski gerçektir. *Not:* bu bir mimari karardır ve `docs/git.md` § 4.1 gereği
ADR'ye bağlanabilir.

### D5 — İlan slug'ı `VehicleCatalog`'dan, gönderim anında okunur

| | Yol | Bedel |
|---|---|---|
| a | `Leads` slug'ı anlık görüntüde dondurur | Slug değişirse bağlantı **404**; `VehicleSnapshot` gerçeği dondurmak için var, gezinme için değil |
| **b** | `VehicleCatalog.Contracts` `FindSlugByIdAsync` kazanır; `Notifications` doğrudan sorar | Üçüncü kenar doğar — tek atlama, salt okuma, § 2.A'ya uygun |
| c | `Leads` slug'ı çözüp D1'in sorgusunda döndürür | `Notifications → Leads → VehicleCatalog` — Yasak Listesi **13**. Elendi |

*Gerekçe:* Bir bildirim bağlantısının işi **tıklandığında çalışmak**; e-posta günler sonra
açılabilir. Dondurulmuş slug, ilan yeniden adlandırıldığında sessizce 404 verir. Anlık
görüntü kuralının amacı kaydın yalan söylememesi, bağlantının amacı okuyanın gidebilmesi —
iki ayrı şey. `VehicleSnapshot`'ın kendi doc comment'i (a)'nın kapısını `Leads` içinde zaten
kapatmış. *Bilerek ödenen bedel:* üçüncü kenar ve ilan kaldırılmışsa slug çözülemez → AC-32.

### D6 — Gönderici **MailKit**; bir test gerçek SMTP sunucusuna karşı koşar

*Gerekçe:* Microsoft kendi belgesinde `System.Net.Mail.SmtpClient`'ı yeni geliştirme için
önermiyor; STARTTLS ve kimlik doğrulama davranışı MailKit'te açık ve test edilebilir. D4
göndericiyi zaten soyutlamanın arkasına koyuyor, yani seçim tek bir sınıfı bağlar.
*Bilerek ödenen bedel:* bu, backend'in **ilk üçüncü parti çalışma zamanı bağımlılığı**;
bugüne kadar `src/` altında yalnız `Microsoft.*` ve `Npgsql` vardı.

Testlerin ezici çoğunluğu sahte taşıyıcıyla koşar; **bir** entegrasyon testi Testcontainers
ile gerçek bir SMTP sunucusuna (Mailpit) karşı koşar ve taşıyıcının gerçekten SMTP
konuştuğunu kanıtlar. *Gerekçe:* sahtelenen altyapı yanlış güven verir — `docs/testing.md`
§ 1 "SQLite ile EF testi yapılmaz" derken aynı şeyi söylüyor.

### D7 — Yeni sözleşme yüzeyleri **dar** tutulur

`LeadNotificationDto` yalnız şunları taşır: ziyaretçi e-postası, bırakılan locale, kanonik
model adı, `BranchId`, `ListingId`. **Ad ve telefon yoktur.** Şube e-postası
`BranchSummaryDto`'ya **eklenmez**; ayrı ve dar bir `BranchNotificationContactDto` doğar.

*Gerekçe:* `ListingSnapshotDto`'nun doc comment'i evin kuralını yazmış: *"Her alanın bir
gerekçesi var; gerekçesi olmayan alan burada YOKTUR."* Ad hiçbir kriterde kullanılmıyor,
telefon X1 gereği hiç gerekmiyor. `BranchSummaryDto` bugün şube doğrulaması gibi yerlerde
kullanılıyor; oraya e-posta koymak şube iletişim bilgisini ihtiyacı olmayan her tüketiciye
açardı.

---

## Acceptance Criteria

> Süre, sayı ve adres içeren kriterler **yapılandırılan** değeri doğrular, koda gömülü bir
> sabiti değil (**C9**). Bu spec'te **arayüz kriteri yoktur**.

### A. Bugün karşılanıyor — regresyon koruması

- **AC-1.** Sağlayıcı erişilemezken talep kaydı etkilenmez; talep oluşturma başarılı döner.
  *(`LeadCaptured` outbox'tan asenkron dağıtıldığı için bu bugün de doğrudur; kriter
  korumanın kaybolmadığını gösterir, yeni davranışın kanıtı değildir.)*

### B. Galeri tarafı

- **AC-2.** Bir talep oluşturulduğunda galeri tarafına bir e-posta gönderilir.
- **AC-3.** Gönderilen e-posta talebin şubesinin adını, aracın model adını ve ilanın detay
  sayfasına giden **mutlak** bağlantıyı taşır.
- **AC-4.** Şubenin e-posta adresi varsa bildirim o adrese gider.
- **AC-5.** Şubenin e-posta adresi yoksa bildirim kurulum bildirim adresine gider.
- **AC-6.** Galeri bildirimi kurulum varsayılan dilinde gönderilir.

### C. Ziyaretçi tarafı

- **AC-7.** Ziyaretçiye talebinin alındığını bildiren bir e-posta gönderilir.
- **AC-8.** Bu e-posta ziyaretçinin **talebi bıraktığı dilde** gönderilir; Arapça bırakılan
  talebin karşılığı Arapça gider.
- **AC-9.** E-posta hiçbir dilde dönüş süresi taahhüdü içermez.
- **AC-10.** `ConsentGiven` **false** olan ziyaretçiye de bu işlemsel karşılık gönderilir.
- **AC-11.** E-posta adresi olmayan — yalnız telefon bırakılmış — bir talepte ziyaretçi
  karşılığı gönderilmez; gönderim geçmişine **"kanal yok"** sonucuyla kaydedilir.
- **AC-12.** Ziyaretçi karşılığının başarısız olması galeri bildirimini engellemez; galeri
  bildiriminin başarısız olması da ziyaretçi karşılığını engellemez.

### D. Kuyruk, tekrar deneme ve hata sınıfları

- **AC-13.** Mesaj önce kuyruğa kaydedilir, sonra gönderilir, sonra sonucu işaretlenir.
- **AC-14.** Bekleyen gönderimler süreç yeniden başladıktan sonra kaybolmaz ve işlenmeye
  devam eder.
- **AC-15.** Geçici bir hatada gönderim yapılandırılan sayıda yeniden denenir ve denemeler
  arasındaki aralık artar.
- **AC-16.** Denemeler tükendiğinde kayıt **başarısız** olarak işaretlenir ve loglanır;
  sessizce düşmez.
- **AC-17.** Kalıcı bir hatada — alıcı adresi reddedildiğinde — yeniden deneme yapılmaz;
  kayıt derhal başarısız işaretlenir.
- **AC-18.** Gönderim yapılandırılan zaman aşımını aştığında **geçici** hata sayılır ve
  yeniden denenir.
- **AC-19.** Aynı `LeadCaptured` olayı iki kez işlendiğinde **ikinci bir gönderim kaydı
  oluşmaz**.

### E. Şablon ve dil

- **AC-20.** İstenen dilde şablon yoksa varsayılan dile düşülür ve bu durum loglanır.
- **AC-21.** Varsayılan dilde de şablon yoksa gönderim başarısız işaretlenir; ham şablon
  anahtarı veya boş gövde **gönderilmez**.
- **AC-22.** Şablon metinleri TR, AR ve EN üçünde de vardır; eksik anahtar kontrolü
  **backend tarafında** bir testle doğrulanır.

### F. Gizlilik ve kayıt

- **AC-23.** Gönderim geçmişi her mesaj için `LeadId`, şablon anahtarı, locale, kanal,
  sonuç ve zaman tutar.
- **AC-24.** Gönderim geçmişi alıcının adını, e-posta adresini veya telefonunu **hiç
  tutmaz**.
- **AC-25.** Gönderim yolundaki log çağrıları alıcı adını, adresini veya telefonunu
  parametre olarak almaz; bunu çağrı yüzeyini tarayan bir test doğrular.

- **AC-32.** İlanın slug'ı çözülemediğinde — ilan kaldırılmışsa — galeri bildirimi
  **bağlantısız** gönderilir ve gönderim **başarısız sayılmaz**. *(Galeri yine de talepten
  haberdar olmalı; bağlantının yokluğu bildirimin kendisini değersizleştirmez.)*

### G. Yapılandırma

- **AC-26.** Kurulum bildirim adresi, taban URL, yeniden deneme sayısı ve gönderim zaman
  aşımı dağıtım yapılandırmasından okunur; hiçbiri koda gömülü değildir.
- **AC-27.** Kurulum bildirim adresi veya taban URL yapılandırmada yokken uygulama ayağa
  kalkmaz.
- **AC-28.** Sağlayıcı kimlik bilgileri yapılandırmada yokken uygulama ayağa kalkmaz.

### H. Sınır

- **AC-29.** Mimari test: `Notifications` dışındaki hiçbir modülde e-posta gönderen çağrı
  bulunmaz.
- **AC-30.** Mimari test: `Notifications` başka bir modülün şemasına dokunmaz.
- **AC-31.** Mimari test: `Notifications`'ın `Leads` ve `Dealership`'e erişimi yalnız
  `X.Contracts` üzerinden ve **tek atlamadır**; `Notifications → Leads → VehicleCatalog`
  zinciri bulunmaz.

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] **A grubundaki kriterin bu spec'ten önce de yeşil olduğu** PR'da açıkça yazıldı;
      yeni davranışın kanıtı B–H gruplarıdır.
- [x] Arayüz kriteri **yok**; görsel kanıt bu spec için uygulanabilir değildir (X5).
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — modül sınırı ihlali yok (AC-29, AC-30, AC-31).
- [x] Yeni migration yalnızca `notifications` şemasına dokunuyor.
- [x] Şablonlar seed migration ile geldi ve TR/AR/EN üçünde de var; eksik anahtar kontrolü
      **backend testiyle** yeşil (C13, AC-22). *(Frontend'in `check:locales` kapısı bu
      şablonları kapsamaz.)*
- [x] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [x] **`docs/architecture.md` § 1 bağımlılık haritası güncellendi:** `Notifications → Leads`
      `Notifications → Dealership` ve `Notifications → VehicleCatalog` kenarları yazıldı ve
      § 7 Karar Defteri'ne satır eklendi (D1, D2, D5). *(Harita bugün `Notifications`'ı ok çıkmayan bir yaprak olarak
      gösteriyor; güncellenmezse bir sonraki modül yanlış haritaya bakar.)*
- [x] D4 ADR'ye bağlandıysa `docs/decisions/` altına yazıldı.
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4). *PR #27'de dört kapının dördü de `pass`:
      Backend (build + xUnit + mimari testler + migration şema kontrolü), Frontend
      (lint + tip + test + build + çeviri anahtarları), E2E ve C3 eşikleri.*
- [x] **Dış gözden geçirme yapıldı:** PR açılmadan önce bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`).
      *Sıra tersine döndü — PR, Takım Yöneticisi'nin talimatıyla QA oturumundan ÖNCE
      açıldı — ama gözden geçirme YAPILDI ve bir kritik bulgu verdi: bozuk bir alıcı
      adresi kuyruğu kalıcı olarak tıkıyordu (`49e1e35`). Bulgu, yalnız sahte taşıyıcıyla
      koşan testlerin göremediği bir yerden geldi; dar bakışın değeri tam olarak bu.*
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
      *PR #27 açık ve `MERGEABLE / CLEAN`; CI'ın dört kapısı dalın ucunda (`a146cb0`)
      yeşil. `docs/git.md` § 4.6 gereği merge'i ajan yapamaz, bu yüzden kutu açık —
      spec, taşıma talimatıyla merge'den ÖNCE arşive alındı.*
- [x] **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır).

---

## SCORECARD

<!--
Bulgu ölçütü bu kartta İKİYE AYRILMIŞ hâlde: `specs/TEMPLATE.md`'de tek sütun olan
"Bulgu gerçek/gürültü oranı", Takım Yöneticisi'nin onayıyla "Dış bulgu" ve "Uygulama içi
bulgu" olarak ayrıldı (PR #26, henüz merge edilmedi). İkisi farklı şey ölçer: biri spec ve
kodun ne kadar açık yazıldığını, diğeri yazarın kendi işini ne kadar sorguladığını.
-->

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **1** | Onay anında: planın kararları (D1–D7) spec'e işlendi, R2'nin yanlış iddiası düzeltildi ve AC-32 eklendi — kriter sayısı **17 → 32**. Şablonun harfi "onaylandıktan SONRA" diyor ve bu değişiklik onayla aynı commit'te (`3af6905`); harfiyen okunursa **0** yazılırdı. **1** yazıyorum çünkü ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve kriter sayısının iki katına çıkması bunun en net cevabı. 0006 da onay öncesi CLARIFY turunu aynı şekilde saymıştı (41 → 61); iki kartın karşılaştırılabilir kalması için aynı okuma sürdürülüyor. |
| **Düzeltme turu sayısı** | **2** | (1) Triyaj turu (`d3ff5d5`): beş bulgu, beşi de kapatıldı — tur açık bir PR üzerinde değil **dal üzerinde** döndü. (2) QA turu (`49e1e35`): PR #27 açıldıktan sonra bir kritik bulgu geldi ve kapatıldı. İlk tur için şablonun "PR kaç kez geri döndü" ifadesi harfiyen karşılanmıyor ama işi geri gönderen bir gözden geçirme turuydu ve sayılması gerekiyor. |
| **Dış bulgu (gerçek/gürültü)** | **6 / —** | İki turda altı bulgu, altısı da gerçek. **QA turu (1):** `MailboxAddress.Parse` bozuk bir adreste fırlatıyor, istisna `Compose` çağrısı `try` dışında olduğu için taşıyıcıdan kaçıyor, gönderici onu yakalamıyor, `SaveChanges` hiç çalışmıyor ve parti `Queued`/`AttemptCount=0` kalıyordu — arka plan servisi aynı partiyi yeniden okuyup **kuyruğu kalıcı olarak tıkıyordu**. Giriş yolu teorik değil: 0005'in `EmailAddress` doğrulaması `<@ornek.com`'u geçiriyor. AC-12 doğrudan, AC-2/16/17 sonucu olarak ihlal ediliyordu. **Triyaj turu (5):** (1) mimari test kırmızıydı — `NotificationsDbContext` kurulum adımına kayıtlı değildi ve ben commit'ten önce yalnız `dotnet build` koşmuştum, test takımını koşmamıştım (`docs/git.md` § 4 bu vakayı zaten kodlamıştı); (2) dokuz test adı AC-3/4/5/11/19/24/32 iddia ediyordu ama testler o kriterleri kanıtlamıyordu; (3) `git add -A` yabancı dosyaları iki kez dala süpürdü; (4) şube iletişim sorgusu bütün entity'yi belleğe çekiyordu; (5) bir bakım bulgusu. **Gürültü sayısı bu kartta yazılamaz:** gürültü işaretini Takım Yöneticisi yaptı ve işaretlenenler bana hiç ulaşmadı — oranın bölenini bilmiyorum ve tahmin etmek ölçütün amacını bozar. |
| **Uygulama içi bulgu** | **9** | (1) Spec'in R2'si **olgusal olarak yanlıştı** — ilan slug'ı hiçbir yerde yoktu (`VehicleSnapshot` yalnız `ListingId` tutuyor ve kendisi "okuma yolu DEĞİLDİR" diyor); D5 ve AC-32 bu bulgudan doğdu. (2) `TestSchema`'ya `notifications` eklenmeyince Host.Tests'in 31 testi birden düştü. (3) Kriter ↔ test eşlemesini sayarken **AC-24'ün hiç kapısı olmadığını** gördüm. (4) AC-26'nın test adı spec metnini birebir kopyalamıyordu. (5) AC-30'un ilk hâli üç yanlış pozitif veriyordu. (6) Bir sonda `str.replace` ile yazılmıştı, hiçbir şey eşleşmedi ve ben başarı bastım — sonda "geçti" ama hiçbir şeyi değiştirmemişti. (7) `if (false)` sondası bu depoda **kullanılamaz**: CS0162 + `TreatWarningsAsErrors` build'i düşürüyor ve `--no-build` sonra bayat assembly'yi koşup "hiçbir şey bozulmadı" diye yanlış rapor veriyor. (8) İki sonda yorum satırı olarak yazıldı; her iki test de yorumları atlıyor ve sondalar **yanlış yeşil** verdi. (9) QA'in bulgusu için yazdığım ilk AC-12 iddiası **yanlıştı**: `Status != Queued` bekliyordum, ama geçici hatada deneme hakkı varken `Queued` kalmak doğru davranış — asıl ayırt edici `NextAttemptAt`'in ilerlemesi. *Sayılmayanlar:* MailKit 4.14.0'ın bilinen açığı (NU1902), seed migration'ının satır sonlarıyla bozulan raw-string'i ve CS9107 — üçü de derleme/bağımlılık sürtünmesi, iş ürününde kusur değil. |
| **Regresyon sayısı** | **1** | Host artık başlangıçta `notifications` şemasının güncel olduğunu da doğruluyor; `TestSchema` o modülü kurmadığı sürece daha önce yeşil olan **31 Host testi** kırmızıya döndü. Tek bir neden, tek bir başlangıç sözleşmesi değişikliği. Üretim davranışı hiç bozulmadı ve commit'ten önce yerelde yakalandı — 0006 `identity` ile tam olarak aynı şekli **1** saymıştı, aynı okuma sürdürülüyor. |
| **Kaçan hata** | **0** | *2026-10-04 itibarıyla* — **izleniyor**. Merge sonrası bulunan her kusur buraya tarihiyle eklenir (`specs/TEMPLATE.md` § SCORECARD). |

**Bu spec'ten çıkarılan ders:**
**Sonda da bir testtir — ve o da yanlış olabilir.** 0007'nin dersi "yeşil bir test,
ölçtüğünü sandığın şeyi ölçtüğü anlamına gelmez"di ve çözüm sondaydı: korumayı bilerek
bozup hangi kriterin kırmızıya döndüğüne bakmak. 0009 bir kat daha derinini gösterdi:
**sondanın kendisi de yeşil görünerek yalan söyleyebilir.** Bu spec'te üç kez oldu ve
üçü de farklı kılıkta geldi:

1. `str.replace` hiçbir şeyle eşleşmedi, sessizce hiçbir şeyi değiştirmedi ve ben
   başarıyı koşulsuz bastım. Sonda "geçti" — çünkü hiç uygulanmamıştı.
2. `if (false)` sondası CS0162 üretti, `TreatWarningsAsErrors` build'i düşürdü ve
   `--no-build` bayat assembly'yi koşup "hiçbir şey bozulmadı" dedi.
3. İki sonda **yorum satırı** olarak yazıldı; iki mimari test de yorumları bilerek
   atlıyor, dolayısıyla sondalar yanlış yeşil verdi.

Alınan ders yöntem düzeyinde: **sonda, uygulandığını KENDİSİ kanıtlamak zorundadır.**
Eşleşme sayısı doğrulanmadan `replace` çağrılmaz; sonda derlenebilir olmalıdır
(`== -1` gibi), build'i düşüren bir mutasyon sonda değil gürültüdür; ve sondanın
yerleştiği satır, testin baktığı satır olmalıdır.

Üçüncü ders QA'den geldi ve ilk ikisinin kör noktasını gösterdi: **sonda, korumayı
bozmayı dener — bağımlılığın sözleşmesini bozmayı denemez.** On sondamın hepsi aynı
kalıptaydı: üretim kodundan bir satır çıkar, hangi test kırmızıya dönüyor diye bak. Hiçbiri
şunu sormadı: *ya bağımlılık sonuç döndürmek yerine fırlatırsa?* Yazdığım dört sahte
taşıyıcının dördü de sözleşmeye UYUYORDU — biri bile `SendResult` döndürme sözünü
tutmazlık etmedi. Oysa gerçek taşıyıcı, ilk bozuk adreste tam olarak bunu yapıyordu ve
sonucu kuyruğun kalıcı olarak tıkanmasıydı.

Genelleştirmesi: **bir portun sözleşmesi, onu zorlayan bir şey olmadıkça bir varsayımdır.**
"Fırlatmaz" diye yazılmış bir port, fırlatan bir gerçekleştirimle birlikte yaşayabilir ve
sahte gerçekleştirimler bunu asla göstermez. Bundan sonra her port için en az bir test,
sahtenin sözleşmeyi İHLAL ETTİĞİ hâli ölçmeli.

İkinci ders ucuz ama pahalıya mal olurdu: **kriter ↔ test eşlemesi SAYILIR, varsayılmaz.**
32 kriterin 31'inin kapısı vardı ve bunu hiçbir şey söylemiyordu; AC-24'ün kapısız
olduğunu yalnızca kriterleri tek tek sayan bir komut gösterdi. Eşlemeyi gözle okumak,
olmayan satırı göremez.
