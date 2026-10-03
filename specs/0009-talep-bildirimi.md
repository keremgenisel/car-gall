# 0009 — Talep Bildirimi

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Notifications` (yeni) · `Leads.Contracts` (okuma yüzeyi — bkz. Açık Soru 1) · `Dealership.Contracts` (şube iletişimi — bkz. Açık Soru 2) |
| **Bağımlı spec'ler** | `0005` (talep oluşturma — tamamlandı) · `0003` (şubeler — tamamlandı) |

> **Bu bir Analist taslağıdır.** Requirements, Constraints ve Context doğrulanmış
> bilgiden yazıldı. **Acceptance Criteria yalnızca iskelet** — dört açık sorunun cevabı
> onları belirliyor. Cevaplar gelmeden spec onaya sunulmaz.

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
- **R2.** Bildirim, talebin hangi şubeye ait olduğunu ve hangi ilanla ilgili olduğunu
  taşır.
- **R3.** Ziyaretçiye, talebinin alındığını bildiren bir karşılık gider — **ve bu
  karşılık ziyaretçinin talebi bıraktığı dilde olur.** → `docs/domain.md` **L1**
- **R4.** Ziyaretçiye giden karşılık **söz vermez**: ne zaman dönüleceğine dair taahhüt
  içermez. → `0005` Karar **D6**
- **R5.** Dışarıya giden her mesaj `Notifications` modülünden geçer. Başka hiçbir modül
  doğrudan e-posta/SMS/WhatsApp göndermez. → Yasak Listesi **17**
- **R6.** Şablonlar **(anahtar × locale)** olarak tutulur. İstenen dilde şablon yoksa
  varsayılan dile düşülür ve bu durum loglanır. → `docs/architecture.md` § 3
- **R7.** Gönderim başarısız olduğunda tekrar denenir; belirli bir denemeden sonra
  **başarısız** olarak işaretlenir ve sessizce kaybolmaz.
- **R8.** Gönderim geçmişi tutulur: ne, kime, hangi dilde, hangi sonuçla gitti.
- **R9.** İletişim izni (`ConsentGiven`) olmayan ziyaretçiye **pazarlama** mesajı
  gönderilmez; talebin kendisine dair **işlemsel** karşılık gönderilir.
  → `docs/domain.md` **L7**
- **R10.** Bildirim gönderimi talebin kaydedilmesini **engellemez**: bildirim
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
- **C5.** **Loga kişisel veri yazılmaz:** telefon, e-posta, ad. Gönderim geçmişinde
  alıcı adresi tutulmak zorundaysa bu bir kayıt alanıdır, log değildir.
  → `docs/conventions.md` § 12
- **C6.** Kullanıcıya görünen hiçbir metin koda gömülmez; mesaj gövdeleri şablondan
  üretilir. → `AGENTS.md` Altın Kural 6
- **C7.** Sayı veya çoğul içeren cümleler dize birleştirmeyle kurulmaz; ICU mesaj
  formatı zorunludur. → Yasak Listesi **31**
- **C8.** Sağlayıcı kimlik bilgileri (SMTP parolası, API anahtarı) repoda durmaz;
  yapılandırmadan gelir ve eksikse uygulama **ayağa kalkmaz**.
  → `docs/conventions.md` § 11
- **C9.** `Notifications` bir **yaprak** modüldür: hiçbir modül ona senkron bağımlı
  olmaz ve o da hiçbir modüle olay yayınlamaz. → `docs/architecture.md` § 1
- **C10.** Şema `notifications`; migration yalnız o şemaya dokunur.

### KAPSAM DIŞI

- **X1.** SMS ve WhatsApp kanalları — *neden:* her kanal kendi sağlayıcı sözleşmesini,
  kendi hata biçimini ve kendi maliyetini getirir. Kanal soyutlaması bu spec'te kurulur
  ama V1'de **yalnız e-posta** uygulanır.
- **X2.** Abonelikten çıkma (unsubscribe) akışı ve bağlantısı — *neden:* yalnız işlemsel
  mesaj gönderildiği sürece (R9) gerekli değil; pazarlama mesajı girdiği gün zorunlu
  hâle gelir ve o zaman kendi spec'ini hak eder.
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
- **Olay kişisel veri taşımıyor** ve bu bilinçli görünüyor: taşısaydı kişisel veri
  outbox tablosunda ve olay günlüğünde dururdu.
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

## Açık Sorular

Her biri kendi önerisi ve gerekçesiyle. **Kararı Takım Yöneticisi verir.**

### Soru 1 — `Notifications` mesaj içeriğini nereden alacak?

| | Yol | Bedel |
|---|---|---|
| **a** | `LeadCaptured` olayı büyür: iletişim bilgisi ve araç anlık görüntüsü taşır | **Kişisel veri outbox'ta ve olay günlüğünde durur.** `C5`'in ruhuna ters; KVKK silme talebinde olay kayıtları da temizlenmek zorunda kalır |
| **b** | `Leads.Contracts` bir sorgu arayüzü kazanır; `Notifications` gönderim anında senkron okur | Tek atlama, salt okuma — `C4`'e uygun. `Leads`'in public yüzeyi büyür; kişisel veri yalnız gönderim anında ve bellekte |
| **c** | Mesaj kişisel veri taşımaz: galeriye *"yeni talep var, panele bak"* gider | En güvenli ama **R2'yi karşılamaz** ve ziyaretçiye karşılık (R3) hiç gönderilemez — e-posta adresi gerekiyor |

**Önerim: (b).** *Gerekçe:* Kişisel veriyi olay günlüğüne yazmak geri alınamaz bir karar;
bir kez outbox'a girdi mi KVKK silme talebi olay geçmişini de kapsar. Senkron okuma ise
`architecture.md` § 2.A'nın tam olarak izin verdiği şey: salt okuma, tek atlama, aynı
istek içinde. *Bedel:* `Leads.Contracts` büyür ve `Notifications → Leads` bağımlılığı
doğar — ama bağımlılık haritası bunu zaten öngörüyor.

### Soru 2 — Galeri tarafında bildirim kime gider?

| | Yol | Bedel |
|---|---|---|
| **a** | Kurulum yapılandırmasındaki tek bir galeri adresi | Hemen yapılabilir; çok şubeli galeride herkes her talebi görür |
| **b** | Talebin şubesinin e-postası — `Dealership.Contracts` bir alan kazanır | `R2`'nin ruhuna daha yakın; `Dealership.Contracts` değişikliği gerekir |
| **c** | Şubenin danışmanları | **Yapılamaz:** atama yok (`X3`) |

**Önerim: (b), (a)'ya düşerek.** Şube e-postası varsa ona, yoksa kurulum adresine.
*Gerekçe:* `0003` şubeye e-posta alanını zaten koydu, yalnız dışa açmadı; açmamak bu
spec'i çok şubeli galeride yanlış çalıştırır. Fallback, tek şubeli kurulumda ve e-postası
girilmemiş şubede sistemi sessiz bırakmamak için. *Bedel:* iki modülün contract'ı büyür.

### Soru 3 — Ziyaretçiye karşılık V1'de var mı?

**Önerim: Evet.** *Gerekçe:* Intent'in ikinci yarısı bu; sessiz kalan galeri unutulur.
Ayrıca teknik olarak galeri bildirimiyle aynı yoldan geçiyor, ek maliyeti düşük.
*Bedel:* Ziyaretçinin e-posta adresi `Notifications`'a ulaşmak zorunda — Soru 1'in
cevabını (b)'ye kilitler. *Karşı argüman:* "yalnız galeri tarafı" V1'i küçültür ve
Soru 1'i (c) ile çözüp kişisel veriyi sınırın hiç ötesine geçirmemeyi mümkün kılar.

### Soru 4 — E-posta sağlayıcısı

| | Yol | Bedel |
|---|---|---|
| **a** | SMTP (galerinin kendi hesabı) | Ek servis yok, ek ücret yok; teslim oranı ve itibar galerinin hesabına bağlı |
| **b** | İşlemsel e-posta servisi (Postmark/SES/Resend gibi) | Teslim oranı ve geri bildirim iyi; dış bağımlılık, ücret ve kurulum başına hesap |

**Önerim: (a) arkasında bir soyutlama, (b)'ye geçiş açık.** *Gerekçe:* Kurulum başına
ayrı dağıtım yapıyoruz; her galeriye ayrı bir servis hesabı açmak operasyon yükü. SMTP
ile başlamak, `IEmailSender` arkasında kalırsa geçişi tek sınıfa indirir. *Bedel:* SMTP
ile teslim oranı daha düşüktür ve spam klasörü riski gerçektir.

**Not:** Bu bir mimari karar; `docs/git.md` § 4.1 gereği **ADR'ye** bağlanabilir ve
spec'ten ayrı gidebilir.

---

## Acceptance Criteria

> **İskelet.** Dört sorunun cevabı gelmeden tamamlanamaz: Soru 1 AC-3/AC-4'ü,
> Soru 2 AC-1'i, Soru 3 AC-6…AC-8'i, Soru 4 AC-11'i belirliyor.

**Galeri tarafı**

- **AC-1.** Bir talep oluşturulduğunda galeri tarafına bir e-posta gönderilir.
- **AC-2.** Gönderilen e-posta talebin şubesini ve ilgili ilanı tanımlayan bilgiyi taşır.
- **AC-3.** *(Soru 1'e bağlı)*
- **AC-4.** *(Soru 1'e bağlı)*

**Ziyaretçi tarafı** *(Soru 3'e bağlı)*

- **AC-5.** Ziyaretçiye talebinin alındığını bildiren bir e-posta gönderilir.
- **AC-6.** Bu e-posta ziyaretçinin **talebi bıraktığı dilde** gönderilir (TR/AR/EN).
- **AC-7.** E-posta hiçbir dilde dönüş süresi taahhüdü içermez.
- **AC-8.** *(Soru 3'e bağlı)*

**Dayanıklılık**

- **AC-9.** Aynı `LeadCaptured` olayı iki kez geldiğinde ikinci mesaj gönderilmez.
- **AC-10.** Gönderim başarısız olduğunda tekrar denenir; tanımlı denemeden sonra
  kayıt **başarısız** olarak işaretlenir.
- **AC-11.** Sağlayıcı erişilemezken talep kaydı etkilenmez; talep oluşturma başarılı
  döner.
- **AC-12.** Gönderim geçmişinde her mesajın şablon anahtarı, locale'i ve sonucu durur.

**Dil ve gizlilik**

- **AC-13.** İstenen dilde şablon yoksa varsayılan dile düşülür ve bu durum loglanır.
- **AC-14.** Hiçbir log satırında alıcı adı, e-postası veya telefonu bulunmaz.
- **AC-15.** `ConsentGiven` false olan ziyaretçiye de **işlemsel** karşılık gönderilir;
  pazarlama mesajı gönderilmez.

**Sınır**

- **AC-16.** Mimari test: `Notifications` dışındaki hiçbir modülde e-posta gönderen
  çağrı bulunmaz.
- **AC-17.** Mimari test: `Notifications` başka bir modülün şemasına dokunmaz.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriteri **yok**; görsel kanıt bu spec için uygulanabilir değildir (X5).
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — modül sınırı ihlali yok (AC-16, AC-17).
- [ ] Yeni migration yalnızca `notifications` şemasına dokunuyor.
- [ ] Şablon metinleri TR/AR/EN üçünde de var; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi
      (Soru 4 ADR'ye bağlanırsa `docs/decisions/` altına yazıldı).
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] **Dış gözden geçirme yapıldı:** PR açılmadan önce bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır).

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | |
| **Düzeltme turu sayısı** | | |
| **Bulgu gerçek/gürültü oranı** | / | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | |

**Bu spec'ten çıkarılan ders:**
