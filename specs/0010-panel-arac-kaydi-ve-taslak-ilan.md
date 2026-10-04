# 0010 — Panel: Araç Kaydı ve Taslak İlan

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (yazma yüzeyi — bugün hiç yok) · `VehicleReference.Contracts` · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0006` (oturum ve roller — tamamlandı) · `0007` (invariant koruması — tamamlandı) · `frontend-001` (panel kabuğu — tamamlandı). **Yayın tarafı için bkz. D1.** |
| **Dayanak** | ADR 0003 unsur 7, sıra #2 |

---

## Intent

**Galeri bugün sisteme araç ekleyemiyor.** Vitrin çalışıyor, ilan detayı çalışıyor, talep
formu çalışıyor — ama bunların gösterdiği araçlar yalnızca geliştirme amaçlı bir seed'den
geliyor. Üretimde bir aracın sisteme girmesinin **hiçbir yolu yok**.

Bunun sonucu şudur: ürün bugün satılamaz. Galeriye kurulum yapıldığında vitrin boş açılır
ve boş kalır; galeri çalışanının elinde aracı sisteme sokacak tek bir ekran yoktur.

**Galeri yöneticisi ve danışmanı**, ellerindeki aracı sisteme kendi girmek istiyor: marka ve
modelini seçmek, kilometresini ve rengini yazmak, fiyatını belirlemek, ilan metnini üç dilde
yazmak ve işi yarıda bırakıp sonra dönebilmek.

Bu iş bittiğinde galeri çalışanı panele girip bir araç kaydı oluşturur, bilgilerini
girer, **taslak** olarak kaydeder ve kaydettiği araçları bir listede görür. Başarı ölçütü:
**bir aracın sisteme girmesi artık geliştiriciye bağlı değildir.**

Yayına alma bu spec'te **yoktur** ve bunun nedeni teknik bir engeldir, tercih değil (D1).

---

## Requirements

- **R1.** Oturum açmış personel panelde yeni bir **araç kaydı** oluşturabilir.
- **R2.** Araç kaydı, aracın **çekirdek alanlarını** taşır: marka, model, yıl, kasa tipi,
  donanım paketi, kilometre, yakıt, vites, çekiş, renk ve **şube**.
  → `docs/architecture.md` § 4 (Çekirdek — kapatılamaz).
- **R3.** Marka, model, kasa tipi ve donanım paketi **referans veriden seçilir**; personel
  bunları serbest metin olarak yazamaz. → `docs/domain.md` **Referans Veri**.
- **R4.** Her aracın **tam olarak bir şubesi** vardır; şubesiz araç kaydedilemez.
  → `docs/domain.md` **A1**.
- **R5.** Araç kaydı bir **şase numarası** taşır. Şase numarası galeri genelinde **tekildir**;
  `Arşiv` durumundaki kayıtlar bu kontrolden muaftır. → `docs/domain.md` **A5**. *(Bkz. D2.)*
- **R6.** Şase numarası, plaka ve iç notlar **her zaman gizlidir**: hiçbir public yanıtta
  bulunmazlar. → `docs/domain.md` **F4**, **F3**.
- **R7.** Aracın fiyatı belirlenir **veya** açıkça **"fiyat sorunuz"** olarak işaretlenir.
  Bu ikisi ayrı durumlardır; "fiyat sorunuz" `0` değildir. → `docs/domain.md` **P4**.
- **R8.** Fiyat değiştiğinde değişiklik **fiyat geçmişine** yazılır. Fiyat geçmişi silinemez
  ve düzeltilemez; yanlış giriş yeni bir kayıtla düzeltilir. → `docs/domain.md` **A6**.
  *(Bkz. D3.)*
- **R9.** Personel, **alan ayarlarında açık** olan opsiyonel alanları doldurabilir. Kapalı
  alanlar formda görünmez.
- **R10.** Alan ayarlarında **zorunlu** işaretli bir opsiyonel alan boş bırakılarak araç
  **taslak olarak kaydedilebilir**; zorunluluk yalnızca yayına alınırken bağlayıcıdır.
  → `docs/domain.md` **A2**, **F2**.
- **R11.** Bir alanın zorunlu yapılması, o alanı boş olan **mevcut** araçları geçersiz
  kılmaz ve düzenlemeye kapatmaz; bu araçlar **"eksik bilgi"** olarak işaretlenir.
  → `docs/domain.md` **F2**.
- **R12.** Personel, ilan başlığını ve açıklamasını **etkin her dilde** (TR/EN/AR) ayrı ayrı
  yazabilir. Bir dilde yazmamak geçerli bir durumdur.
- **R13.** Panel, her araç için **hangi dillerde metnin eksik olduğunu** gösterir.
- **R14.** Yeni oluşturulan araç kaydı bir **taslak ilan** ile başlar; taslak ilan vitrinde
  görünmez. → `docs/domain.md` **A3**.
- **R15.** Personel var olan bir araç kaydını **düzenleyebilir** ve yarıda bıraktığı işe
  dönebilir.
- **R16.** Panel, kayıtlı araçları bir **listede** gösterir: araç tanımı, fiyat, kilometre,
  yayın durumu, eksik dil göstergesi ve son güncelleme zamanı.
- **R17.** Liste sayfalanır ve personel listeyi **son güncellenen** veya **yeni eklenen**
  sırasına göre görebilir.
- **R18.** Liste, kaç ilanda **çeviri eksik** olduğunu özet olarak gösterir.
- **R19.** Araç kaydı ve düzenleme ekranı **yükleniyor**, **başarılı**, **doğrulama hatası**
  ve **hata** durumlarını ele alır. → `docs/frontend.md` § 3.
- **R20.** Liste ekranı **yükleniyor**, **boş** ve **hata** durumlarını ele alır.
- **R21.** Panel, personelin **tercih ettiği dilde** açılır. → `0006` R15.

---

## Constraints

### Uyulması zorunlu

- **C1.** Tüm yazma uçları **oturum gerektirir**; oturumu olmayan erişemez. → `0006` R7.
- **C2.** Para `Money` ile taşınır (tutar + ISO 4217 + minor unit üssü); çıplak `decimal`
  veya `double`/`float` kullanılmaz. Üç ondalıklı para birimleri (KWD/BHD/JOD/OMR)
  bozulmadan saklanır. → `AGENTS.md` Altın Kural 4, **P1–P2**, Yasak Listesi 26.
- **C3.** Para birimi **kurulum başına sabittir** ve panelden değiştirilemez. → **P3**.
- **C4.** Gizli alanlar **sorgu tarafında** elenir, sunum katmanında değil.
  → Yasak Listesi 41.
- **C5.** Referans veriye erişim `VehicleReference.Contracts` üzerinden, **tek atlama**
  senkron doğrulama olarak yapılır. → `docs/architecture.md` § 2.A, Yasak Listesi 13.
- **C6.** Şube kimliğinin geçerliliği `Dealership.Contracts` üzerinden doğrulanır; şube
  adı/adresi `VehicleCatalog`'da **gerçek kaynak gibi saklanmaz**. → Yasak Listesi 33.
- **C7.** **Geriye dönük doğrulama yapılmaz.** Ayar değişikliği mevcut kayıtlara
  uygulanmaz. → Yasak Listesi 39, **F2**.
- **C8.** **Kullanıcı tanımlı alan, dinamik şema veya form oluşturucu yapılmaz.** Alan
  listesi kodda tanımlıdır. → Yasak Listesi 40, `docs/architecture.md` § 4 (kalıcı sınır).
- **C9.** Alan etiketi kullanıcıdan alınmaz. → Yasak Listesi 43.
- **C10.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner, çeviri uçta
  yapılır. Doğrulama metni sunucudan çevrilmiş gelmez.
  → `AGENTS.md` Altın Kural 6, Yasak Listesi 23–24.
- **C11.** Veritabanında çevrilmiş durum veya enum adı saklanmaz; kod saklanır.
  → Yasak Listesi 22.
- **C12.** Zaman damgaları UTC'dir; `DateTime.Now` kullanılmaz. → Yasak Listesi 30.
- **C13.** Sayı içeren cümleler dize birleştirmeyle kurulmaz; ICU zorunludur.
  → Yasak Listesi 31.
- **C14.** CSS'te fiziksel yön özellikleri kullanılmaz; panel de RTL çalışır.
  → Yasak Listesi 25.
- **C15.** Ekranlar `docs/frontend.md` § 4.2 envanterini kullanır; ham piksel, hex renk
  veya ham font boyutu eklenmez. → § 4.1.
- **C16.** Yeni migration yalnızca `vehicle_catalog` şemasına dokunur. → Yasak Listesi 3.
- **C17.** Mevcut agregat davranışı **yeniden yazılmaz**: `Vehicle.Create`,
  `ChangeMileage`, `AssignBranch`, `SetOptionalFieldValue` ve `Listing.CreateDraft`
  bugün vardır ve `0007`'nin invariant korumasına tabidir. Bu spec onların **üzerine bir
  komut/uç katmanı** kurar.
- **C18.** Her kabul kriteri en az bir testle karşılanır; test `DisplayName`'i
  `AC-<n>: <kriterin metni>` biçimindedir ve metin bu spec'ten **kopyalanır**.
  → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 1.

### KAPSAM DIŞI

- **X1.** **Fotoğraf yükleme, sıralama, kapak seçimi, filigran ve türev üretimi** —
  *neden:* `Media` modülü bugün yalnızca tek bir olay dosyasından ibarettir. Bu, **D1**'in
  ayırdığı ikinci dilimdir.
- **X2.** **Yayına alma ve tüm yayın durumu geçişleri** (`Publish`, `Reserve`, `MarkSold`,
  `Unpublish`, `Archive`) — *neden:* `Vehicle.EnsurePublishable()` en az bir medya varlığı
  şartını koşuyor ve medya üretilemiyor (doğrulandı). **D1**'in üçüncü dilimi.
- **X3.** **Toplu eylemler** ("Yayına al", "Vitrine ekle", "Satıldı işaretle") — *neden:*
  hepsi X2'ye bağlıdır.
- **X4.** **13 parçalık boyalı/değişen parça haritası editörü** — *neden:* alanın **değeri**
  R9 kapsamında girilebilir; görsel şema editörü, `0002` § X3'teki görüntüleme şemasıyla
  birlikte ayrı bir spec'tir.
- **X5.** **"Diğer dillere çevir" düğmesi (makine çevirisi)** — *neden:*
  `docs/architecture.md` § 6 makine çevirisini V1 dışı bırakıp yerine *"elle çeviri +
  eksik çeviri panosu"* kararını vermiştir. R13 o kararın bu spec'teki karşılığıdır.
  → Karar **D5**.
- **X6.** **Alan düzeyi yetki** — mockup'taki "fiyat alanı kilitlidir" — *neden:*
  ADR 0003 unsur 3 bunu ayrı bir karara bağladı; § 6'nın izin motoru tetikleyicisiyle
  birlikte ele alınır. Bu spec'te fiyat alanı **role göre kilitlenmez**.
- **X7.** **SEO önizlemesi, `hreflang` listesi ve `Vehicle` JSON-LD** — *neden:*
  `specs/0004-seo-meta-ve-yapisal-veri.md` (Taslak) bu konunun sahibidir; iki spec aynı
  şeyi iki kez tanımlamaz.
- **X8.** **İşlem kaydı / değişiklik geçmişi paneli** — *neden:* `Audit` modülü yoktur.
  *(Fiyat geçmişi bunun dışındadır: o bir domain kuralıdır — R8.)*
- **X9.** **Görüntülenme sayısı sütunu** ve "en çok görüntülenen" sıralaması — *neden:*
  `Engagement` modülü yoktur.
- **X10.** **Dışa aktarma** — *neden:* biçim, kapsam ve kişisel veri sınırı ayrı bir karardır.
- **X11.** **"Önizle"** — *neden:* taslak ilan vitrinde görünmez (**A3**); önizleme, vitrin
  okuma yoluna taslak için ayrı bir kapı açmayı gerektirir ve o kapı yanlış kurulursa
  yayınlanmamış araç sızdırır.
- **X12.** **Rezervasyon ve "süresi geçmiş rezervasyon" uyarısı** — *neden:* rezervasyon
  (`docs/domain.md` § 2.3) kendi spec'ine sahip değildir ve X2'ye bağlıdır.
- **X13.** **Plaka ve iç notlar alanlarının girişi** — *neden:* ikisi de her zaman gizli
  alanlardır (**F4**) ve bu spec'in kabul kriterleri onları doğrulamaz; şase (R5) tekillik
  invariantı taşıdığı için kapsamdadır, diğer ikisi taşımaz.

---

## Context

### Mevcut davranış — doğrulandı

- **`VehicleCatalog`'un tek bir yazma ucu yoktur.** Projedeki tüm `MapPost`/`MapPut`/
  `MapDelete` uçları yalnızca `Identity` (personel, oturum) ve `Leads` (talep) modüllerine
  aittir.
- **Agregatlar ise hazırdır.** `Vehicle` şu yeteneklere bugün sahip: `Create`,
  `AttachMediaAsset`, `ChangePrice`, `SetOptionalFieldValue`, `ChangeMileage`,
  `AssignBranch`, `MarkAwaitingBranchAssignment`, `EnsurePublishable`
  ([Vehicle.cs:75](src/Modules/VehicleCatalog/VehicleCatalog/Domain/Vehicle.cs#L75)).
  `Listing` ise `CreateDraft`, `Publish`, `Reserve`, `ReturnToShowcase`, `MarkSold`,
  `Unpublish`, `Archive`, `ChangeShowcaseSortWeight` taşır
  ([Listing.cs:36](src/Modules/VehicleCatalog/VehicleCatalog/Domain/Listing.cs#L36)).
  **Eksik olan domain değil, komut ve uç katmanıdır.** C17 bu yüzden var.
- **Vitrindeki araçlar bir geliştirme seed'inden geliyor.** `ShowcaseSeeder`'ın kendi doc
  comment'i *"YALNIZCA geliştirme ve kanıt üretimi içindir; üretimde çağrılmaz"* diyor
  (`Application/Development/ShowcaseSeeder.cs`). Üretimde araç kaynağı yoktur.
- **Yayın, medyaya kilitli.** `Vehicle.EnsurePublishable()` gövdesi
  `mediaAssetIds.Count > 0` şartından ibarettir ve doc comment'i *"A2'nin bu spec'te
  doğrulanabilen kısmı: en az bir medya varlığı hazır olmalı"* der
  ([Vehicle.cs:153](src/Modules/VehicleCatalog/VehicleCatalog/Domain/Vehicle.cs#L153)).
- **Medya tarafında dinleyen var, konuşan yok.** `MediaAssetReadyHandler` kapak görselini
  `MediaAssetReady` olayından dolduruyor
  (`Application/EventHandlers/MediaAssetReadyHandler.cs`), ama `Media` modülünün tamamı tek
  bir dosyadır: `Media.Contracts/Events/MediaEvents.cs`. **O olayı yayan hiçbir şey yok** —
  `0003`'teki şube olaylarıyla aynı desen.
- **Fiyat geçmişi (A6) kodda yoktur.** `PriceHistory`, `price_history` veya `PriceChanged`
  için `src/` altında eşleşme yok. `Vehicle.ChangePrice` fiyatın **üzerine yazar**.
  → Karar **D3**.
- **Şase numarası (A5) kodda yoktur.** `Vin` veya `ChassisNumber` için eşleşme yok —
  `BuildingBlocks`'ta bir `Vin` değer tipi öngörülmüş olmasına rağmen
  (`docs/architecture.md` § 1) dosya mevcut değil. → Karar **D2**.
- **Referans veri tarafında yalnız kasa tipi açık:** tek uç
  `GET /api/reference/body-types`. Marka, model ve donanım paketi için okuma yüzeyi yok;
  R3 bunların da açılmasını gerektirir.
- **Panel kabuğu ve roller hazır:** `frontend-001` ve `0006` tamamlandı; `0008` (personel
  yönetimi ekranı eylemleri) PR'ı açık. Bu spec o kabuğun içine yeni bir rota ağacı koyar.

### Geçerli domain kuralları

**A1** (şube zorunlu) · **A2** (yayın koşulları — bu spec'te yalnız taslak tarafı) ·
**A3** (durum geçişleri; taslak vitrinde görünmez) · **A5** (şase tekilliği, arşiv muaf) ·
**A6** (fiyat geçmişi silinemez) · **F1** (kapatmak silmek değildir) · **F2** (geriye dönük
doğrulama yok, "eksik bilgi") · **F3/F4** (gizli alanlar yanıtta hiç bulunmaz; şase, plaka,
iç notlar her zaman gizli) · **F5** (alan listesi koddadır) · **P1–P4** (`Money`, üç
ondalık, kurulum başına tek para birimi, "fiyat sorunuz" ≠ `0`).

### Bilinen tuzaklar

1. **Zorunlu alanı kayıt anında zorlamak.** **F2** ve **A2** birlikte okunduğunda zorunluluk
   **yayına alma** şartıdır, kayıt şartı değil. Kayıtta zorlamak, yarıda bırakılan işi
   imkânsız kılar ve R15'i öldürür.
2. **Fiyat geçmişini sonradan kurmak.** Geçmiş üretilemez: bugün yazılmayan değişiklik
   yarın geriye dönük oluşturulamaz. D3 bu yüzden ertelenmiyor.
3. **Şase tekilliğini arşivi unutarak kurmak.** **A5** `Arşiv` kayıtlarını muaf tutar;
   muafiyet atlanırsa **A4**'ün "aynı araç geri alınırsa yeni kayıt açılır" kuralı
   uygulanamaz hâle gelir.
4. **"Fiyat sorunuz"u `0` ile temsil etmek.** **P4**; sessiz veri bozulması.
5. **Üç ondalıklı para birimi.** KWD tutarı iki ondalığa yuvarlanırsa hata kalıcıdır.
6. **Gizli alanı yanıta koyup arayüzde saklamak.** Yasak Listesi 41; şase yanıtta
   **hiç bulunmaz**.
7. **Referans veriyi serbest metne düşürmek.** "Marka yoksa yazsın" kolaylığı, kanonik ad
   ve slug üretimini bozar (`docs/architecture.md` § 3).
8. **Panelin ziyaretçi diliyle açılması.** Personel locale'i ayrı eksendir (`0006` R15).

### Önerilen kararlar — ONAY BEKLİYOR

**D1 — Panel işi üç dilime ayrılır; bu spec birincisidir.**

| # | Dilim | Neden bu sırada |
|---|---|---|
| **1** | **Bu spec:** araç kaydı, düzenleme, taslak ilan, ilan listesi (okuma) | Tek başına değer üretir: araç artık sisteme girer |
| 2 | `Media`: yükleme, türev üretimi, filigran, depolama | `EnsurePublishable()` medya şartını koşuyor |
| 3 | Yayın ve durum eylemleri + toplu eylemler | 2 olmadan hiçbir araç yayına alınamaz |

*Gerekçe:* ADR 0003 unsur 7 panel işini #2'ye, `Media`'yı #3'e koymuştu. Doğrulama
sırasında bu sıranın **yayın tarafı için tersine çevrilmesi gerektiği** görüldü:
`Vehicle.EnsurePublishable()` en az bir medya varlığı ister ve `MediaAssetReady` olayını
yayan hiçbir şey yok. Yani panel #2 olarak kalabilir ama **yalnızca taslak** teslim edebilir.
Bu, ADR 0003'ü reddetmez; unsur 7'nin 2. maddesini ikiye böler.
*Bilerek ödenen bedel:* bu spec bittiğinde galeri araç girebilir ama **hiçbirini yayına
alamaz**; vitrin hâlâ seed'e bağlı kalır. Değer, 2 ve 3 bittiğinde tamamlanır.
*Elenen alternatif:* yayını bu spec'e dahil edip medya şartını geçici olarak gevşetmek —
**A2**'yi delmek olur ve görselsiz ilan vitrine çıkar; `0001` AC-17 kapak görselini kart
alanı olarak zorunlu kıldığı için vitrin bozuk görünür.

**D2 — Şase numarası bu spec'te eklenir.**
*Gerekçe:* **A5** tekilliği, insan araç girmeye başladığı anda ihlal edilebilir hâle gelen
ilk invariant; seed veri bu riski hiç taşımıyordu. Alan bugün hiç yok ve **F4** gereği her
zaman gizli. *Bilerek ödenen bedel:* spec bir alan ve bir tekillik indeksi kadar büyür.
*Elenen alternatif:* şaseyi yayın spec'ine bırakmak — o zaman aynı fiziksel araç iki kez
girilebilir ve ikisi de taslakta kalır; tekillik ihlali yayından önce doğar.

**D3 — Fiyat geçmişi bu spec'te eklenir.**
*Gerekçe:* **A6** fiyat geçmişini silinemez bir kayıt olarak tanımlıyor ve bu kural insanın
fiyatı değiştirebildiği ilk anda yürürlüğe giriyor. Geçmiş **geriye dönük üretilemez**
(Tuzak 2). *Bilerek ödenen bedel:* spec bir tablo ve bir migration kadar büyür.
*Elenen alternatif:* geçmişi yayın spec'ine bırakmak — taslakta yapılan fiyat düzeltmeleri
kayıtsız kalır ve A6 ilk günden deliktir.

**D4 — Mockup'taki "Pazar profili" etiketi "Alan ayarları" olur.**
ADR 0003 unsur 6'nın bu spec'teki uygulamasıdır; yeni terim üretilmez.

**D5 — Makine çevirisi yapılmaz.**
Mockup'taki "Diğer dillere çevir" düğmesi **yoktur** (X5). Yerine R13'ün locale başına
eksiklik göstergesi gelir — `docs/architecture.md` § 6'nın *"elle çeviri + eksik çeviri
panosu"* kararı. *Bilerek ödenen bedel:* üç dilde metin yazmak elle emek ister ve Arapça
metin çoğu galeride gecikir; R13 bu gecikmeyi en az görünür kılar.

---

## Acceptance Criteria

**Araç kaydı oluşturma**

- **AC-1.** Oturum açmış personel, çekirdek alanları doldurarak bir araç kaydı oluşturur ve
  kayıt listede görünür.
- **AC-2.** Oturumu olmayan biri araç kaydı oluşturamaz; istek reddedilir.
- **AC-3.** Şubesi seçilmemiş bir araç kaydedilemez.
- **AC-4.** Var olmayan bir şube kimliğiyle araç kaydedilemez.
- **AC-5.** Marka, model, kasa tipi ve donanım paketi referans veride bulunmayan bir değerle
  kaydedilemez.
- **AC-6.** Yeni oluşturulan araç kaydının ilanı **taslak** durumundadır.
- **AC-7.** Taslak durumundaki ilan vitrin listesinde ve ilan detayında görünmez.

**Şase numarası**

- **AC-8.** Araç kaydı şase numarasıyla kaydedilir.
- **AC-9.** Galeride aynı şase numarasına sahip ikinci bir araç kaydedilemez.
- **AC-10.** `Arşiv` durumundaki bir aracın şase numarası, yeni bir araç kaydında yeniden
  kullanılabilir.
- **AC-11.** Şase numarası hiçbir public yanıtta bulunmaz; boş değer olarak da bulunmaz.

**Fiyat ve fiyat geçmişi**

- **AC-12.** Fiyatı belirlenmiş bir araç kaydedilir ve fiyatı listede görünür.
- **AC-13.** "Fiyat sorunuz" işaretli bir araç kaydedilir; tutar `0` olarak saklanmaz.
- **AC-14.** Kurulum para birimi KWD iken `1250,500` tutarı **üç ondalıkla** saklanır;
  yuvarlama veya kesme yapılmaz.
- **AC-15.** Fiyat değiştirildiğinde fiyat geçmişine yeni bir kayıt yazılır ve eski kayıt
  olduğu gibi kalır.
- **AC-16.** Fiyat geçmişindeki bir kayıt silinemez ve düzeltilemez; girişim reddedilir.
- **AC-17.** "Fiyat sorunuz"dan belirli bir fiyata geçiş de fiyat geçmişine yazılır.

**Opsiyonel alanlar ve alan ayarları**

- **AC-18.** Alan ayarlarında **açık** bir opsiyonel alan formda görünür ve değeri
  kaydedilir.
- **AC-19.** Alan ayarlarında **kapalı** bir opsiyonel alan formda görünmez.
- **AC-20.** Alan ayarlarında **zorunlu** işaretli bir opsiyonel alan boş bırakılmış araç,
  taslak olarak **kaydedilebilir**.
- **AC-21.** Bir alan zorunlu yapıldığında, o alanı boş olan mevcut araçlar düzenlemeye
  kapanmaz ve **"eksik bilgi"** olarak işaretlenir.
- **AC-22.** Kapatılan bir alanın daha önce girilmiş değeri silinmez; alan yeniden
  açıldığında eski değer görünür.

**Çok dilli metin**

- **AC-23.** Personel ilan başlığını ve açıklamasını TR, EN ve AR için ayrı ayrı yazar ve
  üçü bağımsız saklanır.
- **AC-24.** Yalnızca bir dilde metni olan araç kaydedilebilir.
- **AC-25.** Panel, metni eksik olan dilleri araç başına gösterir.
- **AC-26.** Liste, kaç ilanda çeviri eksik olduğunu özet olarak gösterir.

**Düzenleme ve liste**

- **AC-27.** Kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür.
- **AC-28.** Liste araç tanımını, fiyatı, kilometreyi, yayın durumunu, eksik dil
  göstergesini ve son güncelleme zamanını gösterir.
- **AC-29.** Liste sayfalanır; ikinci sayfa ilk sayfadan farklı kayıtlar gösterir.
- **AC-30.** Liste **son güncellenen** ve **yeni eklenen** sıralamalarıyla farklı sıralar
  üretir.
- **AC-31.** Kilometre negatif bir değerle kaydedilemez.

**Durumlar** (`docs/frontend.md` § 3)

- **AC-32.** Araç kaydedilirken kaydet düğmesi yükleniyor hâline geçer ve form iki kez
  gönderilemez.
- **AC-33.** Araç kaydedildiğinde personele kaydedildiği bildirilir.
- **AC-34.** Doğrulama hatası olan alan işaretlenir ve hata personelin dilinde yazılır.
- **AC-35.** Kayıt başarısız olduğunda girilen bilgiler kaybolmaz ve **tekrar dene** eylemi
  sunulur.
- **AC-36.** Liste yüklenirken iskelet gösterilir.
- **AC-37.** Hiç araç kaydı yokken liste, henüz araç olmadığını bildirir ve **araç ekle**
  eylemini sunar.
- **AC-38.** Liste yüklenemediğinde anlamlı bir mesaj ve **tekrar dene** eylemi gösterilir.
- **AC-39.** Hiçbir hata ekranında HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya
  istisna metni görünmez.

**Dil ve RTL**

- **AC-40.** Panel, giriş yapan personelin tercih ettiği dilde açılır.
- **AC-41.** Arapça tercih eden personel için form ve liste `dir="rtl"` ile basılır.
- **AC-42.** Form etiketleri, alan adları ve liste başlıkları TR, AR ve EN'de kendi dilinde
  görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0010-ac<n>.png`),
      boş, doğrulama hatası ve hata durumları dâhil; RTL kriteri için Arapça görüntü de var.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — `VehicleCatalog` başka modülün tablosuna erişmiyor;
      `VehicleReference` ve `Dealership` çağrıları tek atlama.
- [ ] Yeni migration yalnızca `vehicle_catalog` şemasına dokunuyor; şase tekillik indeksi
      `Arşiv` kayıtlarını muaf tutuyor (AC-10).
- [ ] Fiyat geçmişinin silinemez olduğu testle gösterildi (AC-16).
- [ ] Şasenin hiçbir public yanıtta bulunmadığı sözleşme testiyle gösterildi (AC-11).
- [ ] Mevcut agregat davranışı yeniden yazılmadı; komut katmanı onların üzerine kuruldu (C17).
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] Ekranlar `docs/frontend.md` § 4.2 envanterini kullanıyor; ham piksel/hex eklenmedi.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra spec kaç kez değişti? Yüksekse spec eksik yazılmıştır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. |
| **Bulgu gerçek/gürültü oranı** | / | Gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
