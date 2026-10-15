# Mimari

**Modüler monolit.** Tek uygulama, tek veritabanı; içinde sınırları derleyici ve
veritabanı tarafından dayatılan modüller.

Kurulum modeli: **galeri başına ayrı kurulum + ayrı veritabanı** (white-label).
Kodda `TenantId` yoktur. Bir galerinin birden çok **şubesi** olabilir; şube kurulum
içindeki bir lokasyondur ve `BranchId` ile taşınır. Bu ikisi karıştırılmaz.

---

## 1. Modüller ve Sahiplik

Her modül: kendi PostgreSQL şeması, kendi `DbContext`'i, kendi migration zinciri,
kendi DB rolü. **"Sahip olduğu veri" = o veriyi yazma yetkisi yalnızca o modüldedir.**

Fiziksel yapı — modül başına iki proje:
- `Modules/X/X` — Domain + Application + Infrastructure + Endpoints. **Tamamı `internal`.**
- `Modules/X/X.Contracts` — tek public yüzey: DTO'lar, query/command arayüzleri,
  entegrasyon olayları. Başka bir modülün referans verebileceği **tek** proje budur.

### Ana veri modülleri (seyrek yazılır, çok okunur)

#### `Dealership` — Galeri Kimliği ve Şubeler
Galerinin kendisi ve fiziksel lokasyonları hakkındaki gerçeğin tek kaynağı.

Sahip olduğu:
- **Galeri profili** (tekil agregat): ticari unvan, logo, vergi/sicil bilgisi, sosyal hesaplar.
- **Şube** (1..N): görünen ad *(çevrilebilir)*, serbest metin adres *(çevrilebilir)*,
  koordinat, telefon (E.164), WhatsApp, e-posta, **saat dilimi**, çalışma saatleri,
  durum (aktif / geçici kapalı / kapatılmış), URL slug'ı, şube görselleri (`Media` referansı).

Sahip **olmadığı**: hangi aracın nerede olduğu (`VehicleCatalog`), hangi personelin
hangi şubede olduğu (`Identity`).

**Çalışma saatleri modeli** — ülkeye özgü sessiz hata kaynağı:
- Haftalık program: **gün → 0..N zaman aralığı.** Körfez'de çift mesai yaygındır
  (09:00–13:00 ve 16:00–21:00). Tek açılış/kapanış modeli yetmez.
- **Hafta sonu yapılandırmadan gelmez, programdan çıkar.** Kapalı gün = aralığı olmayan gün.
  Kuveyt'te Cuma–Cumartesi, Türkiye'de Pazar. Kodda hiçbir yerde sabit hafta sonu varsayımı yok.
- **Tarih aralıklı istisna**: Ramazan saatleri, bayram, geçici kapanış.
- Saat dilimi **şube bazında**. "Şu an açık mı?" uçta, şubenin saat diliminde hesaplanır.

**Coğrafi arama:** PostGIS yok. Koordinat `decimal` kolonlarda durur; "en yakın şube"
istemci tarafında hesaplanır. *Tetikleyici:* stok coğrafi olarak anlamlı biçimde yayılırsa.

#### `VehicleReference` — Araç Referans Verisi
Marka / model / kasa tipi / donanım paketi / motor-şanzıman sözlüğü.

Sahip olduğu: taksonomi ağacı, kanonik Latin ad + locale başına görünen ad,
eşanlamlılar, dış kaynaktan içe aktarma kayıtları.

Ayrı modül olmasının nedeni: üç tüketicisi var (`VehicleCatalog`, `Leads` takas formu,
vitrin filtreleri) ve yaşam döngüsü tamamen farklı — iş kuralı yok, dışarıdan beslenir.

### Çekirdek modüller

#### `VehicleCatalog` — Araç Stoğu ve Vitrini
Bir aracın galeriye girişinden satışına kadarki durumu ve vitrine çıkış biçimi.

İçinde iki agregat: `Vehicle` (fiziksel araç ve nitelikleri) ve `Listing` (yayın hâli).
*Bölme tetikleyicisi:* araç **alım** süreci (alış fiyatı, masraf, kâr marjı, konsinye)
sisteme girdiği gün `Inventory` + `Listing` olarak ayrılır.

Sahip olduğu: araç kimliği (VIN/şase, plaka — **iç kullanım**), teknik nitelikler,
kilometre, `BranchId`, fiyat ve fiyat geçmişi, araç durumu / ekspertiz bilgisi,
yayın durumu (taslak / yayında / rezerve / satıldı / arşiv), vitrin sıralama ağırlığı,
medya varlık kimliklerinin **sıralı listesi**, vitrin okuma modeli (denormalize),
ve **`VehicleFormSettings`** (aşağıda).

Sahip **olmadığı**: görsel dosyalar, görüntülenme sayaçları, müşteri talepleri, şube bilgisi.

#### `Leads` — Talep Yönetimi
Ziyaretçi ilgisini yakalamak, danışmana yönlendirmek, sonuca kadar takip etmek.

Sahip olduğu: talep kaydı ve tipi (bilgi, geri arama, test sürüşü, takas, kredi görüşmesi,
fiyat teklifi), müşteri iletişim bilgisi, **talep anındaki araç anlık görüntüsü**
(model adı + fiyat kopyası), `BranchId`, `SubmittedLocale`, danışman ataması, durum akışı,
notlar, sonuç ve kayıp nedeni.

Sahip **olmadığı**: aracın güncel fiyatı; personelin kim olduğu (yalnızca `StaffId`).

### Destek modülleri

| Modül | Sorumluluk ve sahip olduğu veri |
|---|---|
| `Identity` | Personel hesabı, roller (Yönetici / Satış Danışmanı / İçerik Editörü), oturum & refresh token, davet akışı, `PreferredLocale`, opsiyonel `BranchId`. |
| `Media` | Varlık kaydı, orijinal + türevler, işleme durumu, filigran/EXIF politikası, locale başına `alt` metni, depolama yolu. **Medya sahipsizdir** — "hangi araca ait" bilgisi `VehicleCatalog`'dadır. |
| `Content` | Statik sayfalar, SSS, blog, ana sayfa kampanya alanları ve öne çıkan araç **slotu**. Locale başına yayın durumu ve slug. |
| `Notifications` | Şablonlar (anahtar × locale), gönderim kuyruğu ve geçmişi, kanal sağlayıcıları, tekrar deneme, **iletişim izni & abonelikten çıkma**. Sistemden dışarı giden her mesajın tek kapısı. |
| `Audit` | Kim, ne zaman, neyi değiştirdi. Entegrasyon olaylarına jenerik abone. **Çevrilmez** — kod saklar, gösterim uçta çevrilir. |
| `Engagement` | Araç görüntülenme, telefon/WhatsApp tıklama, favori. Yüksek frekanslı, kayba toleranslı. Pazarlama analitiği 3rd party. |

### Modül olmayan: `BuildingBlocks`
Paylaşılan çekirdek — **bilinçli olarak fakir**: `Result<T>`, `Error`, `Entity`,
`AggregateRoot`, `IIntegrationEvent`, outbox altyapısı, saat soyutlaması, sayfalama,
ve değer tipleri: `Money`, `Locale`, `LocalizedText`, `PhoneNumber`, `Vin`, `Plaka`.

**İş kuralı, domain tipi, modüle özgü enum veya entity buraya konmaz.**
Burası büyümeye başladıysa modül sınırları yanlış çizilmiştir.

### Bağımlılık haritası

```
BuildingBlocks   ← herkes (yalnızca primitive)

ANA VERİ
  Dealership          ◄──── VehicleCatalog, Leads, Identity
  VehicleReference    ◄──── VehicleCatalog, Leads

ÇEKİRDEK
  VehicleCatalog  ──►  Dealership, VehicleReference, Media
  Leads           ──►  VehicleCatalog, VehicleReference, Dealership

YAPRAK (hiçbir modül onlara senkron bağımlı değil)
  Content · Notifications · Audit · Engagement
  Notifications   ──►  Leads, Dealership, VehicleCatalog   (yalnız OKUMA, tek atlama)
```

`Notifications` yaprak olmaya devam eder: ok **ondan dışarı** çıkar, ona doğru gelmez.
Okuduğu üç yüzeyin hiçbiri kendisi başka bir modüle uzanmaz — zincir tek atlamadır ve
bunu bir mimari test koruyor (0009/AC-31). Gönderilecek mesajın kişisel verisi olayın
gövdesinden değil kaynaktan okunur: olay gövdesinde taşınan kişisel veri, silinme
talebinden sonra outbox'ta yaşamaya devam ederdi.

Olay akışı (outbox, tek yön):

```
VehicleCatalog → VehiclePublished / VehicleUnpublished / VehicleReserved /
                 VehicleReturnedToShowcase / VehicleSold / VehicleArchived /
                 VehiclePriceChanged / VehicleRelocated
                   → Engagement, Audit, Content(slot geçersizleme), sitemap(host)
Dealership     → BranchOpened / BranchDetailsChanged / BranchDeactivated
                   → VehicleCatalog, Audit, sitemap(host)
Media          → MediaAssetReady / MediaAssetFailed        → VehicleCatalog
Leads          → LeadCaptured / Assigned / StatusChanged   → Notifications, Audit
Identity       → StaffDeactivated                          → Leads (açık talepleri yeniden ata), Audit
```

Döngü yok. İki atlamalı senkron zincir yok.

`VehicleCatalog`'un satırı **2026-10-11'de düzeltildi** (spec `0012b` / D11, Takım
Yöneticisi onayı). Üç şey değişti ve üçü de ölçümle bulundu: (1) altı addan beşi `Vehicle`
öneki **yazılmadan** sayılıyordu, oysa § 2.B'nin örneği `VehiclePriceChanged` diyor — önek
kararsızdı ve "adlar buradakiyle birebir aynı" diyen bir DoD maddesi doğrulanamazdı;
(2) `VehicleCatalog`'un agregatı `ReturnedToShowcase` ve `Archived` domain olaylarını
**zaten yayıyordu** ve ikisinin burada adı yoktu — listeye eklendi; (3) sekizinin de
yayıncısı `0012b`'dir. `VehiclePriceChanged` ve `VehicleRelocated`'ın tetikleyicisi araç
**düzenleme** komutudur: ikisi de bugün değişiyor ve **A7** duyuruyu istiyor.

---

## 2. Modüller Arası İletişim

### A. Senkron doğrudan çağrı
`X.Contracts` arayüzü üzerinden, DI ile. **Üç koşulun hepsi** sağlanmalı:
1. Salt okuma (query) veya çağıranın işlemi için zorunlu bir doğrulama,
2. Sonuç aynı HTTP isteği içinde gerekiyor,
3. **Tek atlama.** `A → B → C` yasaktır.

Örnekler: `Leads → VehicleCatalog` (araç var ve yayında mı, özet al),
`VehicleCatalog → Media` (görseller hazır mı, URL çöz),
`VehicleCatalog → VehicleReference` (marka/model/donanım geçerli mi),
`Identity → Dealership` (şube kimliği geçerli mi).

### B. Entegrasyon olayı
Yayıncı abonelerini bilmez. **Zorunlu olduğu durumlar:** yazma bitti ve başka modül(ler)
tepki vermeli; nihai tutarlılık kabul edilebilir; tüketici sayısı birden fazla olabilir;
tüketicinin başarısızlığı yayıncının işlemini iptal etmemeli.

**Domain event ≠ Integration event** — pazarlığa kapalı ayrım:
- **Domain event:** modülün *içinde* kalır, aynı transaction'da işlenir, iç tip taşıyabilir.
- **Integration event:** `X.Contracts` içinde `record`, geçmiş zaman adı (`VehiclePriceChanged`),
  **yalnızca primitive ve kendi DTO'ları**, sürüm uyumluluğu gözetilir.
  Modül sınırını yalnızca bu geçer.

### C. Teslimat — tek mekanizma: outbox
Yayıncı, olayı kendi şemasındaki `outbox` tablosuna **iş verisiyle aynı transaction'da**
yazar. Arka plan dağıtıcısı iletir.
- Garanti **en az bir kez** → her handler **idempotent** olmak zorundadır (olay kimliğine
  göre yinelenen işleme koruması).
- **Sıra garantisi yoktur.** Handler'lar sıraya bağımlı yazılmaz.
- V1'de broker yok; aynı süreç, veritabanı outbox'ı. Broker'a geçiş tek bir dağıtıcı sınıfını
  değiştirir.

### D. Transaction kuralı
> **Bir istek = bir modülde bir transaction.**

Bir HTTP isteği birden fazla modülün yazmasını kapsayamaz. İki modül de yazmalıysa:
biri yazar, olay yayar, diğeri tepki verir. Telafi gerekiyorsa telafi olayı yazılır.

### E. Anlık görüntü (snapshot) kuralı
Modül B, A'nın verisine bir **karar anında** ihtiyaç duyuyorsa ve bu veri sonradan da
o hâliyle anlamlıysa — **kopyala, sürekli sorma.**
Örnek: talep oluşurken araç 1.250.000 idi; araç sonra 1.180.000'e düşse bile talebin
"1.250.000'lik araç için geldi" bilgisi korunur. Bu veri tekrarı değil, **ayrı bir gerçeğin
kaydıdır.**

### F. Host (API) katmanının rolü
Modül kaydı, yapılandırma, kimlik doğrulama, hata haritalama, CORS, loglama.
Endpoint'ler modülün kendi içinde tanımlanır; host toplar.

**Kompozisyon istisnası:** birden fazla modülden okuyan ekranlar (ana sayfa, iletişim
sayfası) için host katmanında ince bir BFF endpoint'i yazılabilir — ancak yalnızca public
contract'ları **çağırıp birleştirebilir**; kendi iş kuralını yazamaz, veritabanına dokunamaz.

### G. Döngü tuzağı — referans çözüm
"Şube kapatılırken içinde araç varsa engelle" kuralı `Dealership → VehicleCatalog`
bağımlılığı doğurur ve döngü yaratır. Çözüm iki katmanlı:
1. **Host/admin seviyesinde ön kontrol** (UX): kapatma ekranı Catalog'un public query'siyle
   "bu şubede 14 araç var" uyarısını gösterir. Bu kompozisyondur, bağımlılık değil.
2. **`Dealership` `BranchDeactivated` yayar**; `VehicleCatalog` dinler ve o şubedeki araçları
   yayından kaldırıp "lokasyon ataması bekliyor" durumuna alır. Güvenlik ağı.

`Dealership` asla başka modüle senkron çağrı yapmaz.

---

## 3. Çok Dillilik — Kesişen Kısıt

Diller: **TR / AR / EN.** Varsayılan ve fallback dil kurulum yapılandırmasından gelir.

### Sahiplik kuralı
> **Çeviri, içeriğin sahibi olan modülde durur. Merkezi çeviri modülü veya merkezi
> `translations` tablosu yoktur.**

Her modül çevirilerini kendi şemasında, agregatının parçası olarak taşır
(EF Core *owned collection*): `catalog.vehicle_translations`, `content.page_translations`,
`reference.model_translations`.

Ayrı bir `Localization` **modülü yoktur**: etkin locale listesi dağıtım yapılandırmasıdır,
arayüz metinleri frontend deposundadır, kayıt çevirileri sahibindedir — geriye modülü hak
edecek veri kalmaz. *Yeniden değerlendirme tetikleyicisi:* galeri, dağıtım yapmadan arayüz
metinlerini kendi düzenlemek isterse.

### Katmanlar

| Katman | Sahibi | Saklama | Fallback |
|---|---|---|---|
| Arayüz metinleri | **frontend deposu** | `locales/{tr,ar,en}.json` | Varsayılan dil; CI'da eksik anahtar testi |
| Referans veri (marka, model, kasa, yakıt…) | `VehicleReference` | Locale başına satır + kanonik Latin ad | Zorunlu çeviri; eksikse admin uyarısı |
| Araç serbest metni | `VehicleCatalog` | Locale başına satır | **Sessiz fallback** |
| CMS içeriği | `Content` | Locale başına satır + **locale başına yayın durumu** | Yayınlanmamışsa o dilde menüden düşer |
| Bildirim şablonları | `Notifications` | (anahtar, locale) | Varsayılan dil + log uyarısı |
| Enum / durum adları | — | **Saklanmaz** | Kod saklanır, uçta çevrilir |
| Doğrulama / hata mesajları | — | **Saklanmaz** | API `errorCode` + parametre döner |

Yan etki: araç detay sayfasının ~%80'i referans veriden geldiği için galeri tek kelime
Arapça yazmasa bile sayfa Arapça görünür; yalnızca serbest metin fallback'e düşer.

### Locale çözümleme — üç ayrı kavram
1. **Ziyaretçi locale'i** — URL yol öneki: `/tr/…`, `/ar/…`, `/en/…`.
   Öneksiz `/` → varsayılan locale'e 301. Geçersiz önek → 404.
   **`Accept-Language`'a göre otomatik yönlendirme yapılmaz** (Google en-US ile tarar;
   yönlendirme Arapça sayfaların indekslenmemesine yol açar). İlk ziyarette kapatılabilir
   öneri çubuğu gösterilir.
2. **Personel locale'i** — `Identity`'de saklanır, ziyaretçi locale'inden bağımsız.
3. **Alıcı locale'i** — `Leads.SubmittedLocale`. Müşteriye giden e-posta bu dille,
   danışmana giden bildirim danışmanın diliyle gider.

### Para birimi
- **Kurulum başına tek para birimi**, `appsettings` içinde **değiştirilemez** dağıtım
  yapılandırması. Admin panelinden düzenlenemez.
- `Money` = tutar + ISO 4217 kodu + **minor unit üssü**.
- **KWD üç ondalıklıdır** (BHD, JOD, OMR de). `decimal(18,2)` ile saklamak Kuveyt'te
  sessiz veri kaybıdır. Şema `decimal(19,4)`.
- Arapça'da **Latin rakam varsayılan**, yapılandırılabilir.
- "Fiyat sorunuz" ayrı bir değerdir, `0` değil.

### Arama, sıralama, normalizasyon
- **Türkçe `i/İ/ı/I` tuzağı**: `lower()` / `ILIKE` Türkçe collation altında yanlış sonuç verir.
- **Arapça normalizasyon**: hemze varyantları (أ إ آ → ا), ta marbuta (ة → ه),
  elif maksura (ى → ي), hareke ve tatwil temizliği.
- **Kural:** her aranabilir metin için **yazma anında üretilen normalize arama kolonu**
  + locale'e uygun FTS konfigürasyonu (`turkish` / `arabic` / `english`) + `unaccent`.
  Sorgu da aynı normalizasyondan geçer.
- **Sıralama** locale collation'ı ile (`COLLATE "ar-KW-x-icu"`, `"tr-TR-x-icu"`).
- **Slug:** araç URL'i her dilde **aynı Latin slug** (`/ar/cars/toyota-camry-2022-a1b2c3`).
  CMS sayfalarında locale başına düzenlenebilir Latin slug.

### RTL
- Yön locale'in özelliğidir; `<html dir>` ve `lang` sunucudan basılır.
- CSS'te fiziksel yön özellikleri yasak → logical properties. Detay: `docs/frontend.md`.
- Arapça'da **altı çoğul biçimi** vardır; ICU MessageFormat zorunludur.

### SEO
- Her sayfa etkin her locale için `hreflang` + `x-default` yayar. Diller birbirinin
  kanoniği değildir; kanonik URL her zaman kendi locale'inin URL'idir.
- Locale başına sitemap; `VehiclePublished` / `BranchDetailsChanged` olayları günceller.
- JSON-LD locale'e göre üretilir: araç sayfalarında `Car` + `Offer`,
  şube sayfalarında `AutoDealer` + `LocalBusiness` (`openingHoursSpecification` çoklu aralık,
  `geo`, `telephone`, `sameAs`).
- **Tam SSR yok.** Host `index.html`'i servis ederken rota bazlı `<title>`, meta,
  Open Graph ve JSON-LD enjekte eder.

---

## 4. Yapılandırılabilir Araç Alanları

Ekspertiz modeli **Türkiye modelidir**; alanları galeri kendi açıp kapatır.
Ayar `VehicleCatalog` içinde tekil bir agregattır (`VehicleFormSettings`) — başka modüle
konmaz, çünkü `Vehicle`'ın doğrulamasını belirler. Yazma yetkisi yalnızca **Yönetici**.

### Üç grup alan
**Çekirdek — kapatılamaz:** marka, model, yıl, kasa tipi, donanım paketi, kilometre,
yakıt, vites, çekiş, renk, fiyat, **şube**, en az bir görsel, yayın durumu.

**Her zaman gizli — toggle değil:** şase/VIN, plaka, iç notlar. Public API yanıtında
hiç bulunmazlar.

**Opsiyonel — açılıp kapanabilir:**
tramer/hasar kayıt tutarı · boyalı–değişen parça haritası (13 parça) · ağır hasar kayıtlı ·
değişen parça sayısı · ekspertiz raporu dosyası · garanti durumu · yetkili servis bakım
geçmişi · muayene geçerlilik tarihi · kimden (galeriden/sahibinden) · takasa uygun ·
araç durumu (ikinci el/sıfır/klasik) · motor hacmi & gücü · plaka & menşe durumu
(Türk plakalı / yabancı plakalı / ithal / GCC spec) · donanım işaret listesi · video & 360° tur.

### Alan başına iki eksen
| Eksen | Değerler |
|---|---|
| **Form durumu** | Kapalı · Opsiyonel · Zorunlu |
| **Public görünürlük** | Gizli · Görünür |

İkinci eksen gereklidir: galeri bir bilgiyi kayıt altına alıp vitrinde göstermek
istemeyebilir. Tek eksenli toggle bu ihtiyacı karşılamaz ve galeriye alanı tamamen
kapattırır — veri kaybı doğurur.

**Filtre üçüncü eksen değildir, türetilir:** bir alan filtrede görünür ⇔ açık **ve** public
**ve** doğası gereği filtrelenebilir.

### Dört kural
1. **Kapatmak silmek değildir.** Saklı veri olduğu gibi kalır; alan geri açıldığında geri
   görünür. Veri silme yalnızca ayrı ve iki aşamalı onaylı bir eylemle olur.
2. **Geriye dönük doğrulama yapılmaz.** Bir alan "zorunlu" yapıldığında o alanı boş olan
   mevcut araçlar geçersiz sayılmaz ve düzenlemeye kapanmaz — "eksik bilgi" işaretlenir.
   Zorunluluk yalnızca o andan sonraki kayıt ve düzenlemelere uygulanır.
3. **Toggle okuma modelini yeniden kurdurmaz.** Vitrin okuma modeli tüm alanları taşır;
   ne gösterileceği render anında ayarlardan belirlenir.
4. **Bu bir özel alan tasarlayıcısı değildir.** Alan listesi kodda tanımlı, sabit ve
   versiyonludur; kullanıcı **yeni alan tanımlayamaz**. Bu sınır kalıcıdır — kullanıcı
   tanımlı alan, dinamik şema + dinamik doğrulama + dinamik çeviri + dinamik filtre demektir.
   Doğrudan sonucu: alan etiketleri sabit anahtar olduğu için TR/AR/EN çevirileri hazır gelir.

### API
- `GET /api/catalog/field-settings` → açık/zorunlu/public haritası, ETag'li, agresif önbellekli.
  Frontend formu ve filtre çubuğunu buradan kurar.
- Araç yanıtının **şekli sabittir**; kapalı veya gizli alanlar yanıtta bulunmaz (null da gelmez).
  Gizleme Catalog'un query tarafında uygulanır, sunum katmanında değil.
- Değişiklik `VehicleFormSettingsChanged` yayar → `Audit`, önbellek geçersizleme, JSON-LD.

---

## 5. YASAK LİSTESİ

Bunlar tercih değil, kuraldır. İhlal PR'ı reddeder.

### Veri erişimi
1. Başka modülün şemasındaki tabloya SQL/EF ile erişmek — **okuma dahil**.
2. Modüller arası foreign key kısıtı. Başka modülün kimliği kısıtsız `Guid` olarak tutulur.
3. Ortak `DbContext`, ortak migration zinciri, başka modülün migration'ını çalıştırmak.
4. Başka modülün blob/dosya depolama yoluna doğrudan erişmek.
5. İki modülün tablolarını birleştiren view veya stored procedure.

### Tipler ve kod
6. Başka modülün `internal` tipine erişmek. `InternalsVisibleTo` yalnızca o modülün kendi
   test projesine verilir.
7. Başka modülün `X` projesine referans vermek. **Tek izinli referans: `X.Contracts`.**
8. Entity, agregat, EF navigation property veya `DbContext`'in modül sınırını geçmesi.
9. Başka modülün `enum`'unu kullanmak. Gerekiyorsa kendi enum'unu tanımlar ve eşler.
10. "Tekrarı önlemek için" ortak DTO/entity çıkarmak. **Sınır boyunca tekrar bilinçlidir.**
11. `BuildingBlocks`'a iş kuralı, domain tipi, modüle özgü enum veya entity koymak.

### Çağrı ve akış
12. Çift yönlü bağımlılık (`A → B` ve `B → A`).
13. Zincirleme senkron çağrı (`A → B → C`).
14. Uygulamanın kendi HTTP endpoint'ini `HttpClient` ile çağırması.
15. Modül sınırını aşan transaction (`TransactionScope`, iki `SaveChanges` tek transaction).
16. Bir modülün başka modülün outbox/inbox tablosuna yazması.
17. `Notifications` dışından doğrudan e-posta/SMS/WhatsApp göndermek. İstisna yok.
18. Yayıncının abone listesini bilmesi (`if (module == "X")` tarzı dallanma).
19. Başka modülün DI kaydına veya servis implementasyonuna (arayüz değil) erişmek.
20. İş mantığının host/endpoint katmanında yaşaması.

### Çok dillilik ve biçimlendirme
21. Merkezi `translations` tablosu veya çevirileri tek modülde toplamak.
22. Veritabanında çevrilmiş enum/durum adı saklamak — kod saklanır, uçta çevrilir.
23. API'nin son kullanıcıya gösterilecek çevrilmiş hata/doğrulama metni dönmesi.
    (İstisna: modülün sahibi olduğu içerik, örn. blog gövdesi.)
24. Kullanıcıya görünen metni bileşene gömmek (hardcoded string).
25. CSS'te fiziksel yön özellikleri (`left`, `right`, `margin-left`, `text-align: left`).
26. Parayı `decimal(_,2)` ile saklamak veya `Money` yerine çıplak `decimal` taşımak.
27. Ham metin kolonu üzerinde `ILIKE`/`lower()` ile arama.
28. `Accept-Language`'a göre otomatik yönlendirme.
29. Locale önekini opsiyonel bırakmak veya geçersiz öneki kabul etmek.
30. `DateTime.Now` / sunucu yerel saati — UTC + saat soyutlaması.
31. Dize birleştirmeyle cümle kurmak (`"Toplam " + n + " araç"`). ICU mesaj formatı zorunlu.

### Galeri, şube, lokasyon
32. `Dealership`'in başka modüle senkron çağrı yapması.
33. Şube adı/adres/telefonunu başka modülde **gerçek kaynak gibi** saklamak. Vitrin okuma
    modelinde denormalizasyon serbesttir — yalnızca `Dealership` olaylarından beslenerek,
    ve o kopya düzenlenemez.
34. Kodda hafta sonu günü, çalışma günü sayısı veya tek açılış/kapanış saati varsaymak.
35. Sunucu saat dilimiyle "açık/kapalı" hesaplamak.
36. `TenantId` benzeri kiracı alanı eklemek.
37. Şubeyi opsiyonel bırakmak. "Şubesiz araç" durumu modelde yoktur.

### Alan yapılandırması
38. Kapalı bir alanın verisini silmek veya kapatmayı veri silmeyle birleştirmek.
39. Ayar değişikliğini mevcut kayıtlara geriye dönük doğrulama olarak uygulamak.
40. Kullanıcı tanımlı alan, dinamik şema veya form oluşturucu.
41. Gizli/kapalı alanı public yanıta koyup gizlemeyi frontend'e bırakmak.
42. Çekirdek alanları toggle edilebilir yapmak.
43. Alan etiketini kullanıcıdan almak.

### Sınır dayatması — zorunlu
- Modül başına **ayrı assembly**. Derleyici zorlamıyorsa kural değil temennidir.
- xUnit içinde **mimari testler** (NetArchTest/ArchUnitNET): yasak referansları CI'da kırar.
  **İlk gün yazılır, son gün değil.**
- CI kontrolü: modül X'in migration'ı yalnızca `x` şemasına dokunabilir.
- `X.Contracts` dışında hiçbir şey `public` değildir; varsayılan erişim `internal`.
- Şema başına ayrı PostgreSQL rolü — sınır veritabanı seviyesinde de dayatılır.

---

## 6. V1 Kapsam Dışı — Bilinçli Kararlar

Her satır bir karardır, bir eksiklik değil. Tetikleyicisi olan kararlarda tetikleyici yazılıdır.

| Karar | Gerekçe / tetikleyici |
|---|---|
| Mikroservis, message broker, ayrı okuma veritabanı | Tek galeri, tek ekip. Outbox soyutlaması geçişi ucuz tutar. |
| Ayrı `Discovery` (arama) modülü | Birkaç yüz araçta PostgreSQL FTS yeterli. *Tetikleyici:* EF ile karşılanamayan arama kalitesi / Elasticsearch. |
| Elasticsearch | Aynı gerekçe. |
| Public üyelik / hesap sistemi | Favoriler `localStorage`'da. Üyelik KVKK yükü getirir, V1'de değer üretmez. |
| Online ödeme, kapora, sanal POS | Gerçek para akışı; PCI ve muhasebe yükü. |
| Alıcı–satıcı mesajlaşma / canlı sohbet | Telefon + WhatsApp derin bağlantısı, galerinin gerçek çalışma biçimi. |
| Banka / kredi entegrasyonu | V1: istemci tarafı taksit hesaplayıcı; kredi ilgisi `Leads`'e düşer. |
| İlan besleme (sahibinden/arabam XML) | V1.1'de `CatalogSyndication` olarak, olay abonesi modül. Catalog sınırı buna hazır. |
| ERP / muhasebe / fatura / alış maliyeti & kâr takibi | Ayrı domain. Girerse `VehicleCatalog` ikiye ayrılır. |
| Çok kiracılı tek kurulum | Galeri başına ayrı kurulum kararıyla kapandı. *Tetikleyici:* 15–20 galeri. |
| Çok para birimli fiyatlandırma | Kur yönetimi ayrı domain. Kurulum başına tek para birimi. |
| Hicri takvim gösterimi | Körfez'de ticari işlemler Miladi takvimle yürür. Gerekirse uçta biçimlendirme kararı. |
| Makine çevirisi ("Arapçaya çevir" butonu) | V1: elle çeviri + "eksik çeviri" panosu. |
| Çeviri onay iş akışı | 2–3 kişilik ekipte sürtünme üretir. Locale başına yayın durumu yeterli. |
| Şube bazlı yetkilendirme / kayıt seviyesi izin | Basit rol modelini izin motoruna dönüştürür. *Tetikleyici:* "X şubesi danışmanı Y stoğunu düzenleyemesin". |
| PostGIS, mesafeye göre sıralama | Şube sayısı tek haneli; istemci tarafı hesap yeterli. |
| Şubeler arası transfer onay akışı | V1'de transfer tek adımlı alan güncellemesi (`VehicleRelocated`). |
| Şube bazlı fiyatlandırma | Fiyat araca aittir, şubeye değil. |
| Şube bazında farklı alan ayarları | Ayar kurulum genelidir. |
| **Kullanıcı tanımlı özel alanlar / form tasarlayıcı** | Kalıcı sınır. Bkz. § 4. |
| Toplu alan doldurma sihirbazı | V1: "eksik bilgi" rozeti + filtreli liste. |
| Alan ayarı sürüm geçmişi / geri alma | `Audit` kaydı yeterli. |
| Mobil uygulama, push bildirim | Mobil-öncelikli responsive web. |
| A/B testi, öneri motoru, analitik panosu | Üçüncü parti analitik (Plausible/GA). |
| Tam SSR | Sunucu tarafı meta/JSON-LD enjeksiyonu SEO değerinin çoğunu Node runtime'sız verir. |

### V1'de bilinçli olarak VAR olanlar
Araç ekspertiz/tramer detayı · KVKK aydınlatma + iletişim izni kaydı · medya filigranı ·
fiyat geçmişi · talep–danışman ataması · çok dil (TR/AR/EN) + tam RTL · çok şube ·
yapılandırılabilir alanlar.

Bunlar "sonra ekleriz" denildiğinde sonradan eklenmesi en pahalı olan şeylerdir.

---

## 7. Karar Defteri

Bu tablo kararın **ne olduğunu** söyler. Kararın **neden** öyle olduğu, hangi
alternatiflerin neden elendiği ve hangi bedelin bilerek ödendiği `docs/decisions/`
altındaki ADR'lerdedir.

| Konu | Karar |
|---|---|
| Kurulum modeli | Galeri başına ayrı kurulum + ayrı DB (white-label) |
| Şube | Kurulum içinde 1..N; araç bir şubede durur |
| Diller | TR / AR / EN; varsayılan yapılandırmadan |
| Para birimi | Kurulum başına tek, sabit, `decimal(19,4)` |
| `VehicleCatalog` | Tek modül, içinde `Vehicle` + `Listing` agregatları |
| `Discovery` | Ayrı modül yok |
| `Engagement` | İnce modül olarak var |
| `Localization` | Modül yok |
| Olay dağıtımı | Tek mekanizma: outbox, en-az-bir-kez, idempotent |
| Outbox yazıcısı | Modül başına **konkret** `OutboxWriter<TDbContext>`; paylaşılan soyutlama yok (silindi, #52) |
| `VehicleCatalog` olayları | Sekiz entegrasyon olayı, hepsi `Vehicle*` önekli; yayıncısı `0012b` |
| İlan adresi (slug) | Yazma modelinde; **ilk yayına alma** anında bir kez üretilir, bir daha değişmez |
| Toplu eylem sonucu | `200` + ilan başına sonuç listesi; kısmi başarı dördüncü ekran durumudur |
| Toplu eylemde okuma bütçesi | **Aynı yanıtı veren** okumalar ve **küme hâlinde sorulabilen** kapak adresleri istek başına bir kez, komuta verilir; **tazeliği gereken** okuma (medya hazırlığı) kayıt başına ve kilidin altında |
| Öne çıkarma | Vitrin sıralama ağırlığının **iki değerli** kullanımı; ayrı kolon yok, duyuru yok, arşiv dışında her durumda çalışır ve **idempotenttir** |
| Modülün DI kaydı | Modül **kendi** bağımlılığını kendi kaydeder; başka modülün kaydına yaşayan bir bağımlılık bırakılmaz (iki kez ödendi: #52 ve `0012b`'nin kaçan hatası) |
| Paylaşılan altyapı seçenekleri | Modül başına **tiplenir** (`InvariantGuardOptions<TContext>`, `OutboxWriter<TDbContext>`); non-generic paylaşılan bir kayıt yok — kap tek ve son kayıt kazanır |
| `VehicleCatalog` olaylarının gövdesi | `(EventId, OccurredAt, VehicleId, ListingId, Slug)`; **durum alanı yok** — durumu olayın tipi söyler |
| Vitrin okuma modeline yazma | **Görünürlük kararı** yalnız yansıtmada; kopyalanmış kolonları (kapak URL'i, şube adları) besleyen işleyiciler bunun dışında |
| Sistem tepkisiyle gelen toplu geçiş | Şube kapanması (A8) personelin seçim üst sınırına tabi değil; şube olayı başına tek işlem, geçişleri duyurulur |
| Çok kayıtlı eylemde çapraz modül okuması | **Aynı yanıtı veren** okumalar istek başına bir kez (yapılandırma, ad haritaları); **tazeliği gereken** okuma kayıt başına ve kilidin altında |
| Oturum öncesi ara durum | "Parola doğru, ikinci adım bekleniyor" hâli **oturum DEĞİLDİR**: kendi adı olan kısa ömürlü bir çerez + `identity` satırı taşır ve hiçbir yönetim isteği onunla çalışmaz. Kurulumu **olmayan** hesapta ise gerçek oturum verilir ve ikinci bir **kapı** yüzeyi üç yola daraltır — orada parola zaten tek faktördür |
| Proje sayısı | Modül başına 2 (`X`, `X.Contracts`) |
| Syndication | V1 dışı, V1.1'de ayrı modül |
| SSR | Yok; sunucu tarafı meta + JSON-LD enjeksiyonu |
| Admin paneli | Tek Vite uygulaması, `/admin` rota ağacı, lazy-load |
| Veritabanı | PostgreSQL, şema başına ayrı rol |
| Migration uygulama | Tek seferlik kurulum adımı (`--migrate`); çalışan uygulama yalnız doğrular |
| Medya | S3-uyumlu (R2/MinIO) + CDN, ilk günden |
| Medya işleme | Türevler (1600/800/400 WebP) ve filigran **sunucuda**; asenkron hat + işleme durumu |
| Filigran kaynağı | Yapılandırmadaki bir **nesne anahtarı** (`Media`'nın kendi kovasında); medya varlığı kimliği değil |
| Orijinal medya | **Ham saklanır**, `private/` önekinde; CDN'e bağlanmaz ve hiçbir yanıtta yolu dönmez |
| Teklif/pazarlık | Yalnızca bir `Lead` tipi; durum makinesi yok |
| Ekspertiz modeli | Türkiye modeli, alanlar yapılandırılabilir |
| Coğrafi arama | PostGIS yok |
| Şube bazlı yetki | Yok (V1) |
| Personel oturumu | Sunucu tarafı opak oturum çerezi; JWT yok (rol ve durum her istekte kaynaktan) |
| Eşzamanlılık | Invariant koruması: satır kilidi + tek transaction (`InvariantGuard`); iyimser eşzamanlılık yok |
| Şase tekilliği | `vehicles` üzerinde kısmi tekil indeks (`NOT is_archived`); araç kendi arşiv bayrağını taşır |
| Opsiyonel alan tipleri | Kapalı tip kümesi; alan → tip eşlemesi KODDA, değer tipli kolonlarda (tutar `numeric(19,4)` + kod + üs) |
| Panel sayfa boyutu | Varsayılan 25, üst sınır 100; dağıtım yapılandırmasından, vitrinin 24'ünden ayrı eksen |
| Bildirim gönderimi | Veritabanı kuyruğu + arka plan servisi; gönderici port arkasında (V1: SMTP/MailKit) |
| Tema (renk) | Dağıtım yapılandırmasından, **ya tam ya hiç**; panelden düzenlenmez |
| Kontrast güvencesi | Başlangıçta doğrulanır — karşılanmazsa uygulama ayağa kalkmaz; varsayılan palet için ayrıca CI testi |
| Tipografi | V1'de yapılandırılamaz; yazı tipleri kendi kaynağımızdan servis edilir |
