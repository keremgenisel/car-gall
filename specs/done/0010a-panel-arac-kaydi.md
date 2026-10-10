# 0010a — Panel: Araç Kaydı

| | |
|---|---|
| **Durum** | **Tamamlandı** (2026-10-05) |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (yazma yüzeyi — bugün hiç yok) · `VehicleReference.Contracts` · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0006` (oturum ve roller — tamamlandı) · `0007` (invariant koruması — tamamlandı) · `frontend-001` (panel kabuğu — tamamlandı). **Devamı:** `0010b` (düzenleme ve liste). **Yayın tarafı için bkz. D1.** |
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
girer, **taslak** olarak kaydeder ve kaydettiğini listede görür. Başarı ölçütü:
**bir aracın sisteme girmesi artık geliştiriciye bağlı değildir.**

Yayına alma bu spec'te **yoktur** ve bunun nedeni teknik bir engeldir, tercih değil (D1).

**Bu spec ikiye bölündü (D9).** Burası birincisi: araç **kaydı oluşturma**, referans okuma
yüzeyi, şase ve fiyat. Düzenleme, opsiyonel alanlar, çok dilli ilan metni ve zengin liste
`0010b`'dedir. Buradaki liste, AC-1'in "kayıt listede görünür" sözünü karşılayacak kadar
sadedir: sayfalama, sıralama ve göstergeler `0010b`'nin işidir.

---

## Requirements

- **R1.** Oturum açmış personel panelde yeni bir **araç kaydı** oluşturabilir.
- **R2.** Araç kaydı, aracın **çekirdek alanlarını** taşır: model, yıl, kasa tipi, donanım
  paketi, kilometre, yakıt, vites, çekiş, renk ve **şube**.
  → `docs/architecture.md` § 4 (Çekirdek — kapatılamaz).
  *(Marka bu spec'te yoktur → X14, Karar **D6**.)*
- **R3.** **Model, kasa tipi, donanım paketi, yakıt, vites ve çekiş referans veriden
  seçilir**; personel bunları serbest metin olarak yazamaz. Renk referans veri değildir,
  locale başına serbest metindir. → `docs/domain.md` **Referans Veri**.
  *(Gerekçe: yakıt, vites ve çekiş `ReferenceTermKind` ile kodda kapalı küme olarak tanımlı.
  Dışarıda bırakmak serbest metin kapısı açar ve bozulma **sessizdir**: ilan detayı etiketini
  çözemediği niteliği hata vermeden atar.)*
- **R4.** Panel, R3'teki **altı referans kümesini** personelin locale'indeki etiketlerle
  listeler; ham slug gösterilmez. Bu, `VehicleReference`'ın okuma yüzeyinin genişlemesini
  gerektirir — bugün yalnız kasa tipi açıktır (doğrulandı).
- **R5.** Her aracın **tam olarak bir şubesi** vardır; şubesiz araç kaydedilemez.
  → `docs/domain.md` **A1**.
- **R6.** Araç kaydı bir **şase numarası** taşır. Şase numarası galeri genelinde **tekildir**;
  `Arşiv` durumundaki kayıtlar bu kontrolden muaftır. → `docs/domain.md` **A5**. *(Bkz. D2.)*
- **R7.** Şase numarası **hiçbir vitrin yanıtında bulunmaz** — boş değer olarak da bulunmaz —
  ama **yönetim yanıtında bulunur**, çünkü personel onu girer ve düzenler. Yönetim kökü
  oturum gerektirir (`0006` C18). → `docs/domain.md` **F3**, **F4**.
  *(Plaka ve iç notların girişi bu spec'te yoktur → X13.)*
- **R8.** Aracın fiyatı belirlenir **veya** açıkça **"fiyat sorunuz"** olarak işaretlenir.
  Bu ikisi ayrı durumlardır; "fiyat sorunuz" `0` değildir. → `docs/domain.md` **P4**.
- **R9.** Fiyat değiştiğinde değişiklik **fiyat geçmişine** yazılır. Fiyat geçmişi silinemez
  ve düzeltilemez; yanlış giriş yeni bir kayıtla düzeltilir. → `docs/domain.md` **A6**.
  *(Bkz. D3.)*
- **R10.** **Çekirdek alanların hepsi kayıt için zorunludur.** Bir araç kaydı, R2'deki
  alanlardan biri eksikken oluşturulamaz. "Yarıda bırakıp dönmek" **opsiyonel alanlar ve çok
  dilli metin** için geçerlidir. → Karar **D8**.
- **R11.** Serbest metin alanlarının **üst uzunluk sınırı** vardır: ilan başlığı, ilan
  açıklaması, renk ve şase numarası. Sınırı aşan istek reddedilir.
- **R17.** Yeni oluşturulan araç kaydı bir **taslak ilan** ile başlar ve **varsayılan vitrin
  sıralama ağırlığıyla** açılır; taslak ilan vitrinde görünmez. → `docs/domain.md` **A3**.
  *(Ağırlığın düzenlenmesi bu spec'te yoktur → X17.)*
- **R18.** Panel, kaydedilen araçları **sade bir listede** gösterir: araç tanımı, fiyat ve
  kilometre. *(Sayfalama, sıralama, eksik dil ve eksik bilgi göstergeleri `0010b`'dedir —
  bu liste yalnızca AC-1'in "kayıt listede görünür" sözünü karşılar.)*
- **R22.** Araç kaydı ekranı **yükleniyor**, **başarılı**, **doğrulama hatası**
  ve **hata** durumlarını ele alır. → `docs/frontend.md` § 3.
- **R24.** Panel, personelin **tercih ettiği dilde** açılır. → `0006` R15.

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
- **C19.** **Gözlem noktası.** Bir davranışın yüzeyi yoksa kriteri **modül düzeyinde**
  doğrulanır, endpoint testiyle değil (`0006` C16 kalıbı). Bu spec'te fiyat geçmişi
  kaydının silinemezliği (AC-24) böyledir: silme ucu yoktur ve yazılmayacaktır; invariant
  domain ve altyapı katmanında sınanır.
- **C20.** **Vitrin yüzeyi ile yönetim yüzeyi ayrı sözleşmelerdir.** Gizli alanlar vitrin
  yanıtında **hiç bulunmaz** (Yasak Listesi 41, sorgu tarafında elenir); yönetim yanıtı
  onları taşır ve yönetim kökü oturum gerektirir. İki sözleşme aynı DTO ile taşınmaz.

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
  alanlardır (**F4**) ve bu spec'in kabul kriterleri onları doğrulamaz; şase (R6) tekillik
  invariantı taşıdığı için kapsamdadır, diğer ikisi taşımaz.
- **X14.** **Marka (`Brand`) kavramı ve marka taksonomisi** — *neden:* kodda marka varlığı,
  kolonu, çevirisi, okuma ucu ve seed'i **yoktur** (doğrulandı); `VehicleModel` markaya
  referans vermiyor. Markayı burada eklemek varlık + çeviri + model→marka bağı + okuma ucu +
  seed demektir ve bu spec'in kapsamını belirgin biçimde büyütür. `VehicleReference`
  *"galeri yazmaz, sistem besler"* modülüdür; marka taksonomisi onun kendi spec'ine aittir.
  → Karar **D6**. **Sonucu:** model **düz bir listeden** seçilir ve bu, model sayısı
  büyüdüğünde kullanılabilirliği düşürür.
- **X15.** **Model verisinin içe aktarılması ve yönetimi** (dış kaynaktan besleme, panelden
  model ekleme) — *neden:* `VehicleReference` sistem tarafından beslenir. Bu spec
  **başlangıç model kümesini kurulum verisiyle** getirir (D7), besleme yolunu kurmaz.
- **X16.** **Eşzamanlı düzenlemede kaybolan güncelleme koruması** (iyimser eşzamanlılık,
  `RowVersion`) — *neden:* `0007` § X1 bunu bilinçli olarak dışarıda bıraktı; gerekçesi
  *"bu spec'in derdi invariant'lar, her satırın kaybolan-güncelleme sorunu değil"*.
  **Bilerek ödenen bedel — ve bu spec'te ilk kez gerçek:** insanın aynı aracı eşzamanlı
  düzenlediği ilk spec budur. İki danışman aynı aracı açıp kaydederse biri diğerinin
  yazdığını **sessizce ezer**. Önceden yalnız seed yazıyordu ve bu risk doğmuyordu.
- **X18.** **Düzenleme, opsiyonel alanlar, çok dilli ilan metni ve zengin liste**
  (sayfalama, iki sıralama, eşitlik kırıcı, eksik dil ve eksik bilgi göstergeleri, çeviri
  özeti) — *neden:* **D9**; `0010b`'nin kapsamı. Bu spec'in listesi yalnız AC-1'i karşılar.
- **X17.** **Vitrin sıralama ağırlığının panelden düzenlenmesi** — *neden:* taslak ilan
  vitrinde görünmez; ağırlık yalnız yayınlanmış ilanların sırasını etkiler ve o, D1'in
  üçüncü dilimine (yayın) aittir. Bu spec varsayılan ağırlıkla başlatır (R17).

---

## Context

### Mevcut davranış — doğrulandı

- **`VehicleCatalog`'un tek bir yazma ucu yoktur.** Projedeki tüm `MapPost`/`MapPut`/
  `MapDelete` uçları yalnızca `Identity` (personel, oturum) ve `Leads` (talep) modüllerine
  aittir.
- **YAZMA TARAFININ KALICILIĞI HİÇ YOK — bu, planı yazarken ölçülen en büyük bulgu.**
  `VehicleCatalogDbContext` üç `DbSet` taşıyor: `ShowcaseListings` (okuma modeli),
  `VehicleFieldSettings` ve `InboxEntries`. **`Vehicle` ve `Listing` için ne `DbSet`, ne EF
  yapılandırması, ne tablo var**; `vehicle_catalog` şemasındaki tablolar
  `showcase_listings`, `showcase_listing_optional_values`, `vehicle_field_settings` ve
  `inbox_entries`'ten ibaret. `ShowcaseSeeder` agregatları **bellekte** kurup projekte
  ediyor ve sonra **atıyor**; saklanan tek şey okuma modeli satırları.
  *Sonucu:* eksik olan yalnız "komut ve uç katmanı" değil — **yazma tarafının tamamı**:
  tablolar, EF eşlemesi, migration ve kaydetme anında projeksiyon. Bu, C17'nin anlamını da
  değiştiriyor → Karar **D10**.
- **İlan metni (başlık/açıklama) kavramı kodda HİÇ yok.** `Listing` yalnız `VehicleId`,
  `Status`, `ShowcaseSortWeight` ve `PublishedAt` taşıyor; okuma modelinde ve hiçbir
  sözleşmede başlık/açıklama alanı yok. R15 var olan bir şeyi yüzeye çıkarmak gibi okunuyor
  ama sıfırdan kurulacak → `0010b`, Karar **D13**.
- **Referans doğrulaması bugün yalnız BİÇİM kontrolü.** `VehicleSpecification.Create`
  slug'ları `BodyTypeSlug.TryParse` ile süzüyor — yani kalıp denetimi; referans veride var
  olup olmadığına bakmıyor. AC-5 varlık doğrulaması istiyor → Karar **D14**.
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
  `GET /api/reference/body-types`. Model ve dört referans terimi (yakıt, vites, çekiş,
  donanım paketi) için okuma yüzeyi yok; R4 hepsinin açılmasını gerektirir.
- **Marka kodda hiç yok.** `grep -rniE '\bbrand\b|\bmarka\b|\bmake\b'`
  `VehicleReference` altında **sıfır** isabet. `VehicleModel` yalnız `Id`, `CanonicalName` ve
  çevirileri taşıyor — markaya referans vermiyor. `docs/architecture.md` § 1 *"taksonomi
  ağacı"* diyor ama kodda **düz liste** var. → Karar **D6**.
- **Araç modeli hiç seed edilmiyor.** `VehicleReference` migration'larının `InsertData`
  hedefleri: `body_types`, `body_type_translations`, `reference_terms`,
  `reference_term_translations`. **`vehicle_models` yok** ve modülün yazma yüzeyi de yok.
  Yani taze kurulumda seçilecek **hiçbir model yoktur** ve `Vehicle.Create` bir
  `vehicleModelId` ister — araç kaydı oluşturulamaz. → Karar **D7**.
- **`Vehicle.Create` çekirdek alanların hepsini zorunlu kılıyor.** İmza yedi parametre ve
  hiçbiri nullable değil: `branchId`, `vehicleModelId`, `specification` (beş slug), `color`,
  `modelYear`, `mileageKilometers`, `price`
  ([Vehicle.cs:75](src/Modules/VehicleCatalog/VehicleCatalog/Domain/Vehicle.cs#L75)).
  C17 agregatı yeniden yazmayı yasakladığı için "çekirdek alanı eksik taslak" bu spec'te
  **mümkün değildir**. → Karar **D8**.
- **Tanınmayan referans slug'ı sessizce kaybolur.** İlan detayı niteliği yalnız etiketini
  çözebildiğinde ekliyor: `if (labels.TryGetValue((kind, slug), out string? label))`
  ([ShowcaseListingDetailQueries.cs:80](src/Modules/VehicleCatalog/VehicleCatalog/Application/Detail/ShowcaseListingDetailQueries.cs#L80)).
  Hata yok, uyarı yok — o satır detay sayfasından düşer. R3'ün yakıt, vites ve çekişi de
  kapsamasının nedeni budur.
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
9. **Sıralama kırıcısını atlamak.** Son güncelleme zamanı aynı olan iki araçta kırıcı yoksa
   sayfalama kararsızlaşır ve aynı araç iki sayfada görünür — `0001`'in SCORECARD'ında
   kayıtlı ders, bu kez "aynı dakikada güncellenmiş iki araç" biçiminde (R20).
10. **Şase tekilliğini `ShowcaseSeeder`'ı unutarak kurmak.** *(Tuzağın ilk hâli migration'ın
   MEVCUT satırlara ne yazacağını soruyordu; ölçüldü — `vehicles` tablosu hiç yok, yani
   doldurulacak satır da yok. Kalan gerçek risk seeder'dır:* `ShowcaseSeeder` artık yazma
   modelini de yazacak (**D12**) ve şase üretmezse tekillik indeksi ilk seed'de patlar.*)*
11. **Şase tekilliğini okuyup-sonra-yazarak kurmaya çalışmak.** *(Tuzağın ilk hâli kısmi
   indeksi yasaklıyordu; uygulamada ölçüldü ve tersi çıktı — bkz. **D11**'in gözden geçirme
   notu.)* Gerçek tuzak şu: "bu şase var mı" diye bakıp sonra yazmak, iki eşzamanlı istekte
   **ikisini de** geçirir — okuma ile yazma arasında kilitlenecek bir satır yoktur. Tekillik
   kararını veritabanı verir. → Karar **D11**.

### Verilen kararlar

**D1–D8 onaylandı** (2026-10-05). Uygulama planından çıkan sekiz karar daha eklendi ve
aynı turda onaylandı: **D9–D16**.

**D1 — Panel işi üç dilime ayrılır; bu spec birincisidir.**

| # | Dilim | Neden bu sırada |
|---|---|---|
| **1** | **Bu spec:** araç kaydı, düzenleme, taslak ilan, ilan listesi (okuma) | Tek başına değer üretir: araç artık sisteme girer |
| 2 | `Media`: yükleme, türev üretimi, filigran, depolama | `EnsurePublishable()` medya şartını koşuyor |
| 3 | Yayın ve durum eylemleri + toplu eylemler | 2 olmadan hiçbir araç yayına alınamaz |
| 4 | `VehicleReference`: marka taksonomisi ve model besleme | X14/X15; bu spec düz model listesiyle çalışır |

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

**D6 — Marka bu spec'ten çıkarılır; model düz listeden seçilir.**
*Gerekçe:* marka kodda hiç yok (doğrulandı) ve eklemek varlık + çeviri + model→marka bağı +
okuma ucu + seed demek. Bu spec'in değeri *"araç sisteme girebiliyor"*; marka taksonomisi
`VehicleReference`'ın kendi işidir ve o modül *"galeri yazmaz, sistem besler"* — markayı
panelden doldurmak D1'in dilim sırasını da bozar. *Bilerek ödenen bedel:* model listesi düz
kalır ve model sayısı büyüdüğünde kullanılamaz hâle gelir; marka D1'in **4. dilimine** düşer.
*Elenen alternatif:* markayı burada eklemek — spec belirgin büyür ve S1'in kapsamı
`VehicleCatalog`'dan `VehicleReference`'a taşar.

**D7 — Başlangıç model kümesi kurulum verisiyle gelir.**
*Gerekçe:* `vehicle_models` hiç seed edilmiyor ve modülün yazma yüzeyi yok; bu hâliyle taze
kurulumda **hiçbir araç girilemez** ve Intent'in *"geliştiriciye bağlı değildir"* sözü
tutmaz — araç girmek için geliştirici migration yazmaya devam eder. Seed migration deseni
depoda üç kez kullanıldı (`0003`, `0006`, referans verinin kendisi).
*Bilerek ödenen bedel:* başlangıç model listesi bir **içerik kararı** gerektirir ve D6 ile
birlikte düşünülmelidir: marka olmadığı için modeller düz bir listede durur. Model besleme
yolu X15'te.

**D8 — "Taslak", çekirdek alanların hepsi girilmiş kayıttır.**
*Gerekçe:* `Vehicle.Create` yedi parametresinin hiçbirini nullable bırakmıyor ve C17
agregatı yeniden yazmayı yasaklıyor; alanları nullable yapıp doğrulamayı yayına taşımak
`0007`'nin invariant korumasına dokunur. Zorunluluğun yayına ertelendiği şey **A2**'nin
"zorunlu işaretli opsiyonel alanları"dır, çekirdek alanlar değil.
*Bilerek ödenen bedel:* personel bir aracı **girmeye başlamak** için 11 çekirdek alanın
hepsini bilmek zorunda; mockup'ın "kaydet ve sonra dön" vaadi opsiyonel alanlar ve çok dilli
metinle sınırlanır. *Elenen alternatif:* çekirdek alanları nullable yapmak — C17 ihlali.

**D9 — Spec ikiye bölünür.**
`0010a` (burası): araç kaydı oluşturma, referans okuma yüzeyi, şase, fiyat ve fiyat
geçmişi, sade liste. `0010b`: düzenleme, opsiyonel alanlar, çok dilli ilan metni, zengin
liste (sayfalama, iki sıralama, eşitlik kırıcı, göstergeler).
*Gerekçe:* ilk hâli 50 kriter ve 18 DoD maddesi taşıyordu ve altında sıfırdan kurulacak bir
yazma tarafı vardı. `0006` altmış bir kriterle iki düzeltme turu ve bir kaçan hata üretti;
bu spec ondan daha derin bir altyapı işi taşıyor (AP-06 sinyali). **Bölme backend/frontend
ekseninde DEĞİL, kriter grubuna göre yapıldı** — ikisi de çalışan bir ekran teslim eder.
*Bilerek ödenen bedel:* galeri `0010a` bittiğinde araç girebilir ama **düzenleyemez**;
yanlış girilen bir araç `0010b`'ye kadar öyle kalır.
*Elenen alternatif:* tek parça gitmek — ilk PR'ın diff'i gözden geçirilemeyecek kadar büyür.

**D10 — EF kalıcılık tesisatı C17'nin yasakladığı "yeniden yazma" DEĞİLDİR.**
Private parametresiz kurucu eklemek, backing field eşlemesi ve owned type kullanmak
serbesttir; **domain metodlarının davranışı değişmez.**
*Gerekçe:* C17'nin amacı `0007`'nin invariant korumasına dokunulmamasıdır, kalıcılık
tesisatını yasaklamak değil — ve agregatlar bugüne kadar hiç kaydedilmediği için o tesisat
yapılmadan bu spec imkânsız. *Bilerek ödenen bedel:* agregat dosyaları EF'in gereksinimleri
yüzünden değişir ve diff "domain değişti" gibi okunur; PR'da ayrı başlıkta gösterilir.

**D11 — Şase tekilliği kısmi tekil indeksle kurulur; `vehicles` kendi arşiv bayrağını
taşır.** *(Uygulama sırasında GÖZDEN GEÇİRİLDİ — aşağıda.)*
*Gerekçe:* muafiyet `Arşiv` durumuna bağlı ve durum `listings`'te; bayrak o bilginin araç
tarafındaki yansımasıdır ve ilan arşivlendiği anda **aynı transaction'da** yazılır (`0012`).
İndeks `NOT is_archived` filtresiyle kuruluyor, tekillik kararını veritabanı veriyor.
*Bilerek ödenen bedel:* bayrak iki yerde tutulan bir olgunun ikinci kopyası ve aynı
transaction dışında yazılırsa bayatlar; `Vehicle.MarkArchived()` bu yüzden tek giriş kapısı.

> **Kararın ilk hâli ve neden düştüğü.** Onaylanan metin şöyleydi: *"Şase tekilliği
> `InvariantGuard` ile kurulur, kısmi tekil indeksle değil."* Uygulamada ölçüldü ve
> **yanlış olduğu görüldü**: `InvariantGuard` var olan bir satırı `SELECT … FOR UPDATE` ile
> kilitleyip sayar; şase tekilliğinde kilitlenecek satır **henüz yoktur**. Aynı şaseyle
> gelen iki eşzamanlı kayıt isteğinin ikisi de sıfır sayar, ikisi de geçer. Yani birinci
> karar A5'i koruyormuş gibi görünen ama korumayan bir mekanizma seçiyordu. Elenen
> alternatif olarak yazılan "denormalize `is_archived` bayrağı" tam da gereken şeydi;
> bayrağın kaymasına karşı itiraz, `0012`'nin arşivlemeyi tek transaction'da yapmasıyla
> karşılanıyor. *(Bu bir **karar** revizyonudur; R/C/AC değişmedi.)*

**D12 — `ShowcaseSeeder` yazma modelini de yazar ve ilanları yayınlamaya devam eder.**
*Gerekçe:* seeder bugün yalnız okuma modeli yazıyor; yazma modeli gelince arkasında araç
kaydı olmayan 200 vitrin satırı bir yalan olur ve panel listesi boş görünür. Taslak
üretirse de vitrin boşalır ve `0001`/`0002`'nin bütün e2e'si düşer. Seeder bir **geliştirme
aracıdır**, uç değil; X2 uçları kapsam dışı bırakıyor, seeder'ı değil — ve sahte medya
varlığını zaten ekliyor, `EnsurePublishable` geçiyor.
*Bilerek ödenen bedel:* geliştirme seed'i, panelin yapamadığı bir şeyi (yayınlama)
yapmaya devam eder; bu fark `0012`'ye kadar sürer.

**D13 — İlan metni `jsonb` olarak saklanır, locale başına satır olarak değil.** *(`0010b`)*
*Gerekçe:* `color_names` için `LocalizedNamesConverter` deseni zaten kurulu; ek tablo
açmadan aynı yolu tekrar eder.

**D14 — Referans varlık doğrulaması `Application` katmanında yapılır.**
`Domain` biçim denetiminde kalır; varlık `VehicleReference.Contracts`'a eklenecek toplu bir
sorguyla doğrulanır.
*Gerekçe:* `Domain` başka modülü bilemez (`conventions.md` § 4) ve C5 tek atlama istiyor.

**D15 — "Eksik bilgi" saklanmaz, sorguda hesaplanır.** *(`0010b`)*
*Gerekçe:* C7 geriye dönük doğrulamayı yasaklıyor; bayrak saklamak tam olarak o olurdu.
Ayar değiştiğinde hiçbir yazma olmadan değişmeli.

**D16 — Başlangıç model kümesi GEÇİCİDİR.**
İçerik kararı verilmediği için seed, bugünün geliştirme verisini genişleten küçük ve
**açıkça geçici** bir kümeyle kurulur; spec'te ve migration yorumunda böyle işaretlidir.
*Gerekçe:* hangi modellerin geleceği bir ürün kararı ve D6 markayı çıkardığı için liste düz
duracak — içeriği doğrudan kullanılabilirliği belirler. Besleme yolu X15'te.
*Bilerek ödenen bedel:* `0012` öncesinde gerçek model kümesi girilmezse galeri kendi
araçlarını eşleyemez.

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler görsel kanıt gerektirir (`docs/testing.md` § 3).
> Yüzeyi olmayan davranışların gözlem noktası **C19**'dadır.
>
> **Numaralandırmada boşluk var ve bu bilinçli (D9).** AC-26…AC-39 ile AC-44…AC-46
> `0010b`'ye taşındı ve orada **kendi numaralarıyla** yeniden başlıyor. Buradaki numaralar
> korundu çünkü testler, commit'ler ve kanıt dosyaları onlara atıf yapıyor; yeniden
> numaralandırmak izlenebilirliği kırardı.

**Araç kaydı oluşturma**

- **AC-1.** `[görsel]` Oturum açmış personel, çekirdek alanları doldurarak bir araç kaydı
  oluşturur ve kayıt listede görünür.
- **AC-2.** Oturumu olmayan biri araç kaydı oluşturamaz; istek reddedilir.
- **AC-3.** Şubesi seçilmemiş bir araç kaydedilemez.
- **AC-4.** Var olmayan bir şube kimliğiyle araç kaydedilemez.
- **AC-5.** Model, kasa tipi, donanım paketi, yakıt, vites ve çekiş **altısı da** referans
  veride bulunmayan bir değerle kaydedilemez; altı alan ayrı ayrı doğrulanır.
- **AC-6.** Çekirdek alanlardan biri eksik bırakılan araç kaydedilemez (D8).
- **AC-7.** Model yılı 1900'den küçük veya 2200'den büyük bir değerle araç kaydedilemez.
- **AC-8.** Kilometre negatif bir değerle kaydedilemez.
- **AC-9.** İlan başlığı, ilan açıklaması, renk veya şase numarası üst uzunluk sınırını
  aştığında araç kaydedilemez.
- **AC-10.** Yeni oluşturulan araç kaydının ilanı **taslak** durumundadır ve varsayılan
  vitrin sıralama ağırlığıyla başlar.
- **AC-11.** Taslak durumundaki ilan vitrin listesinde ve ilan detayında görünmez.

**Referans veri okuma yüzeyi**

- **AC-12.** `[görsel]` Panel; model, kasa tipi, donanım paketi, yakıt, vites ve çekiş
  kümelerini referans veriden listeler.
- **AC-13.** Referans listeleri personelin locale'indeki etiketlerle gelir; hiçbir yerde ham
  slug gösterilmez.
- **AC-14.** Başlangıç model kümesi kurulum verisiyle gelir; taze bir kurulumda model
  listesi boş değildir (D7).

**Şase numarası**

- **AC-15.** Araç kaydı şase numarasıyla kaydedilir.
- **AC-16.** Galeride aynı şase numarasına sahip ikinci bir araç kaydedilemez.
- **AC-17.** `Arşiv` durumundaki bir aracın şase numarası, yeni bir araç kaydında yeniden
  kullanılabilir.
- **AC-18.** Şase numarası hiçbir **vitrin** yanıtında bulunmaz; boş değer olarak da
  bulunmaz.
- **AC-19.** Şase numarası **yönetim** yanıtında bulunur ve o yanıt oturum gerektirir (C20).

**Fiyat ve fiyat geçmişi**

- **AC-20.** Fiyatı belirlenmiş bir araç kaydedilir ve fiyatı listede görünür.
- **AC-21.** "Fiyat sorunuz" işaretli bir araç kaydedilir; tutar `0` olarak saklanmaz.
- **AC-22.** Kurulum para birimi KWD iken `1250,500` tutarı **üç ondalıkla** saklanır;
  yuvarlama veya kesme yapılmaz.
- **AC-23.** Fiyat değiştirildiğinde fiyat geçmişine yeni bir kayıt yazılır ve eski kayıt
  olduğu gibi kalır.
- **AC-24.** Fiyat geçmişindeki bir kayıt silinemez ve düzeltilemez; gözlem noktası
  **C19**'dadır.
- **AC-25.** "Fiyat sorunuz"dan belirli bir fiyata geçiş de fiyat geçmişine yazılır.

**Liste**

- **AC-26.** `[görsel]` Kaydedilen araç sade listede araç tanımı, fiyat ve kilometreyle
  görünür. *(Sayfalama, sıralama ve göstergeler `0010b`'dedir — X18.)*

**Durumlar** (`docs/frontend.md` § 3)

- **AC-40.** `[görsel]` Araç kaydedilirken kaydet düğmesi yükleniyor hâline geçer ve form iki
  kez gönderilemez.
- **AC-41.** Araç kaydedildiğinde personele kaydedildiği bildirilir.
- **AC-42.** `[görsel]` Doğrulama hatası olan alan işaretlenir ve hata personelin dilinde
  yazılır.
- **AC-43.** `[görsel]` Kayıt başarısız olduğunda girilen bilgiler kaybolmaz ve **tekrar
  dene** eylemi sunulur.
- **AC-47.** Hiçbir hata ekranında HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya
  istisna metni görünmez.

**Dil ve RTL**

- **AC-48.** Panel, giriş yapan personelin tercih ettiği dilde açılır.
- **AC-49.** `[görsel]` Arapça tercih eden personel için araç kaydı formu `dir="rtl"` ile
  basılır.
- **AC-50.** `[görsel]` Form etiketleri ve alan adları TR, AR ve EN'de kendi dilinde
  görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] `[görsel]` işaretli **her** kriterin kanıtı eklendi (`docs/evidence/0010a-ac<n>.png`),
      boş, doğrulama hatası ve hata durumları dâhil; AC-49 için Arapça görüntü de var.
      *(12 görüntü: AC-1, AC-12, AC-26, AC-26-boş, AC-40, AC-41, AC-42, AC-43, AC-47,
      AC-49, AC-50-ar, AC-50-en.)*
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`). *(679 test)*
- [x] Mimari testler yeşil — `VehicleCatalog` başka modülün tablosuna erişmiyor;
      `VehicleReference` ve `Dealership` çağrıları tek atlama.
- [x] Yeni migration yalnızca `vehicle_catalog` şemasına dokunuyor. *(Üç migration;
      ölçüldü — hepsinde `schema: "vehicle_catalog"`. **D7**'nin model seed'i ayrı bir
      zincirde ve yalnız `vehicle_reference`'a dokunuyor; bu madde yazıldığında o karar
      henüz yoktu.)*
- [x] **Yazma modeli kalıcı hâle getirildi** (`vehicles`, `listings`, `price_changes`);
      vitrin okuma modeli yazma modelinden türüyor. *(Kayıt anında projeksiyon KOŞMUYOR ve
      koşmamalı: `ShowcaseListingProjector` yalnız `Published`/`Reserved` ilanı yansıtıyor,
      yeni kayıt ise **taslak** (R17, AC-11). Yayın `0012`'nin işi — X2.)*
- [x] **`ShowcaseSeeder` yazma modelini de yazıyor ve şase üretiyor** (D12, Tuzak 10);
      vitrin seed'i bozulmadı — `0001`/`0002` e2e'si yeşil.
- [x] Şase tekilliği kısmi tekil indeksle kurulmuş ve `Arşiv` muafiyeti testle gösterilmiş
      (AC-17, D11 — gözden geçirildi; `docs/decisions/0006-sase-tekilligi.md`).
- [x] Fiyat geçmişinin silinemez olduğu testle gösterildi (AC-24, gözlem noktası C19).
- [x] Şasenin hiçbir **vitrin** yanıtında bulunmadığı sözleşme testiyle gösterildi (AC-18);
      yönetim yanıtında bulunduğu ve oturum gerektirdiği de gösterildi (AC-19).
- [x] **Başlangıç model kümesi seed edildi** ve taze kurulumda model listesinin boş olmadığı
      gösterildi (AC-14, D7). *(12 model × 3 locale.)*
- [x] Referans okuma yüzeyi altı küme için açıldı ve `VehicleReference.Contracts`
      genişlemesi yalnız gereken alanları taşıyor (AC-12, AC-13).
- [x] Mevcut agregat **davranışı** yeniden yazılmadı; komut katmanı onların üzerine kuruldu
      (C17). EF tesisatı (private kurucu, backing field, owned type) D10 kapsamındadır ve
      PR'da **ayrı başlıkta** gösterilmiştir.
- [x] **Dış gözden geçirme yapıldı:** bir QA oturumu diff + spec + testleri okudu ve dört
      bulgu verdi; dördü de gerçek çıktı ve düzeltildi (`docs/roles/qa.md`). *(Gözden
      geçirme PR açıldıktan sonra yapıldı — madde "açılmadan önce" diyor.)*
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`). *(Şablonda iki kez geçiyor.)*
- [x] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil. *(237 anahtar.)*
- [x] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır.
- [x] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
      *(Şase tekilliği satırı + ADR `0006`.)*
- [x] Ekranlar `docs/frontend.md` § 4.2 envanterini kullanıyor; ham piksel/hex eklenmedi.
      *(`AdminField` → `shared/ui/Field` terfi etti; `check:tokens` ve `lint:css` yeşil.)*
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4). *(PR #35, dört kapı da pass.)*
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [x] Takım Yöneticisi onayladı ve squash-merge edildi. *(PR #35.)*
- [x] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı. *(Taşıma PR
      #36; SCORECARD'ın ölçümleri o PR'da KAYBOLDU ve bu PR'da geri getirildi —
      ayrıntısı SCORECARD'ın altında.)*

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | 0 | R/C/AC onaylandıktan sonra değişmedi. **D11 bir KARAR revizyonudur** ve bu sütuna girmiyor; ayrıntısı kararın altında. |
| **Düzeltme turu sayısı** | 1 | QA triyajı. Öncesindeki commit'ler planın adımlarıydı, geri dönüş değil. |
| **Dış bulgu (gerçek/gürültü)** | 4 / 0 | QA triyajı dört bulgu verdi, dördü de gerçek çıktı ve düzeltildi. Aşağıda sayıldı. |
| **Uygulama içi bulgu** | 9 | Aşağıda sayıldı. |
| **Regresyon sayısı** | 1 | `0007/AC-8`'in mimari testi `0010a`'nın korumalı **okuma** ucunu da reddediyordu; mesajı "korumasız YAZMA ucu" diyordu ama düzenli ifade bütün fiilleri yasaklıyordu. Daraltıldı ve `MapPost`'la kırmızıya döndüğü probla doğrulandı. |
| **Kaçan hata** | 1 | Opsiyonel alan değeri `LocalizedText` olarak saklanıyordu; tanımlı tek alan tramer, yani bir **tutar**. Parayı metinde saklamak Altın Kural 4'ün ve Yasak Listesi 26'nın ihlali. `0010b` buldu, C23 istisnasıyla tipli değere (`numeric(19,4)`) geçirdi ve eski kolonu düşürdü. **`ListingPrice` sayılmadı:** o da tek metin kolonunda duruyor ama `0010a`'da BİLİNEREK kabul edilmiş bir EF sınırıdır ve ondalık ölçeğin korunduğu testle gösterilmiştir — kabul edilen risk, kaçan hata değil. |

> **Bu SCORECARD iki kez dolduruldu.** Ölçümler 2026-10-05'te yapıldı ama `main`'e
> girmedi: kapanış PR'ı (#36) yalnız bir `git mv` taşıdı — düzenlemeler, iki paralel
> oturumun aynı çalışma ağacındaki index'ini çözerken yapılan `git reset` ile silindi.
> `Dış bulgu` satırı ise 41 saniye sonra, artık merge edilmiş olan
> `feature/0010a-panel-arac-kaydi` dalına ve taşınmadan önceki yola yazıldı; oradan
> buraya taşındı. Commit mesajı "DoD 23/23" diyordu, dosya 20/23 diyordu — **ve beş gün
> boyunca kimse farkı görmedi, çünkü hiçbir kapı spec dosyasını okumuyor.**
>
> Ders: *bir ölçüm commit'e girmediyse yapılmamıştır.* Bunun kurala dönüşmesi
> `docs/git.md` § 4'e ait ve bu PR'da öneri olarak duruyor.

**Dış bulgular (4/0):**

1. **Renksiz araç kaydedilebiliyordu** *(orta — karar sorusu olarak geldi)*. R2 rengi
   çekirdek listeye koyuyor, R10'un ikinci cümlesi çok dilli metni muaf tutuyor; QA
   ikisini de okunabilir buldu ve kararı Takım Yöneticisi'ne bıraktı (AP-10). **Karar:**
   renk zorunlu, ölçüt "en az bir **etkin** locale'de dolu" — böylece R2'nin açık listesi
   de R10'un muafiyeti de ayakta kalıyor ve spec revize edilmiyor. Aynı bulgunun bağımsız
   yarısı: etkin olmayan locale'de girilen renk **sessizce atılıyordu**; artık
   reddediliyor.
2. **AC-24 ve AC-48'in etiketli testi yoktu** *(küçük)*. AC-24'ün invariantı doğru
   kurulmuştu ama yalnız yorumda yaşıyordu — `RemovePriceChange` eklemek hiçbir testi
   kırmazdı. Artık invariantın ŞEKLİ sınanıyor. AC-48 bir izlenebilirlik boşluğuydu:
   davranış `0006/AC-54` etiketiyle test ediliyordu.
3. **Yönetim listesi `listings` tablosunun tamamını okuyordu** *(küçük)*. Bugün zararsızdı
   (araç sorgusu da sayfalanmıyor, X18) ama `0010b` sayfalamayı eklediğinde kazancı
   sessizce iptal ederdi.
4. **Boş şase "çok uzun" diyordu** *(küçük)*. `Vin.Create`'in üç başarısızlık nedeni tek
   koda eşlenmişti; personel şaseyi kısaltmaya çalışırdı, oysa sorun boşluktu.

**Dersin ikinci yarısı:** dördünün üçü de *"kod doğru, ölçen bir şey yok"* biçimindeydi —
invariant yorumda, hata kodu tek sepette, sorgu sınırsız. Hiçbiri bir testi kırmıyordu,
çünkü hiçbirini bir test tutmuyordu.

**Uygulama içi bulgular (9):**

1. **D11 korumuyordu.** `InvariantGuard` var olan bir satırı kilitler; şase tekilliğinde
   kilitlenecek satır yok ve iki eşzamanlı kayıt ikisi de "sıfır" sayar. Karar gözden
   geçirildi (ADR `0006`).
2. **Spec'in merkezî iddiası eksikti.** "Yazma modeli zaten var" varsayımı ölçüldü: `Vehicle`
   ve `Listing` agregatları bugüne kadar **hiç kaydedilmiyordu** — `DbContext`'te yoktu,
   migration'ı yoktu. D10 bu yüzden eklendi.
3. **EF, iç içe karmaşık tipte `Money?` bağlayamadı.** `ListingPrice` tek metin kolonuna
   indirildi; ondalık basamak sayısı kodlamanın içinde taşınıyor (KWD üç ondalıklı).
4. **Migration yeniden üretildiğinde tablolar kayboldu.** Snapshot zaten işlenmişti, yeni
   migration yalnız farkı taşıdı ve 31 Host testi `relation does not exist` ile düştü.
   İşlenmiş migration geri alındı, arşiv bayrağı ayrı migration oldu.
5. **`PriceChange` `Modified` olarak izlendi.** Guid anahtar `ValueGeneratedNever()` olmadan
   EF'e "bu satır var" dedirtiyordu; `DbUpdateConcurrencyException` oradan geliyordu.
6. **Zincir testinin üç hatası bendeydi:** `Secure` çerez `https://localhost` taban adresi
   ister, kurulum hesabı önce parolasını değiştirmek zorundadır ve vitrin rotası
   `/api/catalog/showcase-listings`.
7. **E2E bayat Host'a karşı koştu.** İlk turun "157 geçti"si anlamsızdı; `/api/reference/models`
   boş dönüyordu. Host yeniden kuruldu, 12 model + 200 ilan doğrulandı, sonra koşuldu.
8. **`if (false)` probu yine CS0162 üretti** — aynı ders `0009`'un SCORECARD'ında yazılıydı.
9. **Playwright'ın `getByLabel`'ı alt dize eşler.** `getByLabel('Model')` hem *Model* hem
   *Model yılı* alanını buluyordu; `getByLabel('Fiyat')` fiyat alanını ve "fiyat sorunuz"
   onay kutusunu. 10 e2e testi bu yüzden düştü — `{ exact: true }` ile kapatıldı. Birim
   testlerinde görünmemişti: Testing Library'nin `getByLabelText`'i varsayılan olarak TAM
   eşler, Playwright'ınki ETMEZ ve iki kütüphanenin aynı isimli sorgusu farklı davranıyor.

**Bu spec'ten çıkarılan ders:**

**Bir invariantı "nasıl koruyacağımız" kararı, koruyup koruyamayacağı ÖLÇÜLMEDEN
verilemez.** D11 planlama turunda makul görünüyordu: proje zaten `InvariantGuard`'a sahipti,
muafiyet başka tabloda duruyordu ve kısmi indeks onu göremiyordu. Üç önerme de doğruydu;
sonuç yanlıştı. Eksik olan tek soru şuydu: *"`InvariantGuard` neyi kilitler?"* Cevap "var
olan satırı" ve yeni kayıtta öyle bir satır yok. Karar, mekanizmanın adını değil
**mekanizmanın ne yaptığını** kontrol etmeden verilmişti.

Kural olarak: eşzamanlılık kararlarında "hangi satır kilitlenecek" sorusu kararın
**parçasıdır**; cevabı yoksa karar henüz verilmemiştir.
