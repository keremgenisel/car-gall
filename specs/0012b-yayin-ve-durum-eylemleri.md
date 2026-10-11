# 0012b — Yayın ve Durum Eylemleri

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (komut yüzeyi, outbox, vitrin yansıtması) · `Dealership` (**eklemeli** bir okuma — **D40**) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | **`0012a` ÖNKOŞUL — tamamlandı** (2026-10-11, `8fc862c`; A2'nin medya koşulunu ölçülebilir kıldı) · `0010a` + `0010b` · `0011` · `0007` (invariant koruması) — hepsi tamamlandı |
| **Dayanak** | ADR 0003 Ek 1, sıra #5'in **ikinci dilimi** (→ `0012a` **D1**) |
| **Kararlar** | **D2–D42**. D2–D21 `0012`'nin CLARIFY turundan; **D22–D38** `0012b`'nin kendi CLARIFY turundan (`18e4adf` üzerinde); **D39–D42** kriter turunda **ölçülerek** eklendi. **Açık soru yok.** |

> **R/C/AC bu PR'da `main`'e giriyor ve orada donuyor** (ADR `0008` / K2 ve K3'ün donma
> anı cümlesi — **D38**). Bu commit'ten sonra Requirements, Constraints veya Acceptance
> Criteria'da yapılan her değişiklik **sayılan bir revizyondur**.
>
> **İkinci CLARIFY turu** (`3c72936`) on yedi kararı (**D22–D38**) bugünkü `main`
> (`18e4adf`) üzerinde ölçerek verdi ve son açık soruyu kapattı. **Bu tur** onun üzerine
> dört karar daha ekliyor (**D39–D42**) ve **elli sekiz kriteri** yazıyor.
>
> **İş ikiye bölündü** (**D39**): liste üzerinde çalışan **toplu eylem** ve **öne çıkarma**
> `0012c`'ye taşındı. Bu dosya **bir ilan üzerinde** çalışan eylemleri kurar.
>
> **R2 bir revizyon aldı** (**D42**): A2'nin fiyat koşulu da — çekirdek alan koşulu gibi
> (**D23**) — yayın anında **temsil edilemez**. Ölçüm kararın içinde.

---

## Intent

`0010a`/`0010b` ve `0012a` bitti: galeri araç kaydı oluşturabiliyor, fotoğrafını
yükleyip sıralayabiliyor — ama **hiçbirini vitrine çıkaramıyor.** Vitrin geliştirme
amaçlı bir seed'e bağlı kalmaya devam ediyor; yani ürün hâlâ satılamaz durumdadır. Bu spec o
son halkayı kapatır.

**Galeri yöneticisi** hazırladığı aracı vitrine çıkarmak, satılanı satıldı olarak
işaretlemek ve ilgisini çekmeyen ilanı yayından almak istiyor. Günlük iş bir araç üzerinde
değil **liste üzerinde** yürüdüğü için, aynı işlemi birden çok ilana birlikte uygulamak
istiyor.

Bu iş bittiğinde galeri bir aracı yayına alabilir, rezerve edebilir, rezervasyonu
kaldırabilir, satıldı işaretleyebilir, yayından kaldırabilir ve arşivleyebilir; bu geçişler
vitrine ve diğer modüllere **duyurulur**. Başarı ölçütü: **vitrindeki araçlar artık seed'den
değil galerinin kendi kararından geliyor.**

---

## Requirements

- **R1.** Oturum açmış personel bir taslak ilanı **yayına alabilir**. Eylemler **rol ayrımı
  yapmaz**; yetki tek bir şeydir: oturum. → **D22**, `0006` R7
- **R2.** Bir ilan yayına alınırken **A2**'nin yayın anında doğrulanabilen **iki** koşulu
  sağlanmış olmalıdır: en az bir medya varlığı **hazır** · aktif alan ayarlarında zorunlu
  işaretli opsiyonel alanlar dolu. **A2'nin çekirdek alan koşulu da fiyat koşulu da bu
  listede yoktur** — ikisi de kayıt anında zorunludur ve eksik hâlleri modelde **temsil
  edilemez** (→ **D23**, **D42**). Koşul sağlanmıyorsa yayına alma reddedilir ve
  **sağlanmayan koşulların HEPSİ birlikte** bildirilir — ilk eksik değil. Zorunlu opsiyonel
  alan reddi **hangi alanların** eksik olduğunu da taşır. → **D2**, **D23**, **D24**, **D42**
- **R3.** Yayın durumu geçişleri yalnızca **A3**'te yazılı olanlardır; başka geçiş yoktur.
- **R4.** `Satıldı` **terminaldir**: araç yeniden yayına alınamaz. → **A4**
- **R5.** Personel bir ilanı **rezerve** edebilir; rezerve ilan vitrinde rozetiyle görünmeye
  devam eder. → **R6** (domain)
- **R6.** Personel bir rezervasyonu **kaldırabilir** ve ilan `Yayında` durumuna döner.
  Rezervasyon **kaydı** (bitiş tarihi, iptal nedeni, bildirim) bu spec'te yoktur. → **D17**,
  **X1**
- **R7.** Personel bir ilanı **yayından kaldırabilir** (taslağa döndürebilir) ve
  **arşivleyebilir**. Taslak bir ilan da arşivlenebilir. → **A3**, **D10**
- **R8.** Arşivleme, aracın **arşiv bayrağını aynı işlemde** yazar; arşivlemeden sonra aynı
  şase yeniden kaydedilebilir. → **A5**, ADR 0006, **D9**
- **R9.** → **`0012c`.** Toplu eylem bu dilimde **yoktur** (**D39**). Numara serbest
  bırakılmadı: **D18** ve **D36** ona atıf yapıyor ve hedefsiz bir atıf, kararın kaybolması
  demekti.
- **R10.** → **`0012c`.** Kısmi başarı ve seçim sınırları toplu eylemle birlikte taşındı
  (**D3**–**D6**, **D27**–**D29** orada tüketiliyor).
- **R11.** Her durum geçişi **duyurulur** — personelin eylemiyle olanı da, sistemin kendi
  tepkisiyle olanı da (**A8**). Duyuru aracın ve ilanın kimliğini ve ilanın **adresini**
  taşır; **yeni durumu ayrı bir alan olarak taşımaz**, durumu olayın **tipi** söyler.
  Üretilen olaylar **sekizdir** ve adları `docs/architecture.md` § 1'deki adlarla birebir
  aynıdır. → **D11**, **D30**, **D33**
- **R12.** → **`0012c`.** Öne çıkarma bir **sıralama** işidir ve toplu eylemle aynı ekranda
  yaşıyor (**D39**). `Listing.ChangeShowcaseSortWeight` bu dilimde de çağıransız kalıyor;
  geliştirme seed'inin hizalanması da (**D36**) `0012c`'nin işi.
- **R13.** Şube kapatıldığında o şubedeki araçlar **yayından kalkar** ve "lokasyon ataması
  bekliyor" hâline geçer; bu araçlar panel listesinde **ayırt edilir** ve şubesi atanmadan
  yayına alınamaz. Yayından kalkma **`Rezerve` ilanları da kapsar**; `Satıldı` ve `Arşiv`
  kayıtlara dokunulmaz. Eylem şubedeki **bütün** araçlara uygulanır ve R10'un seçim üst
  sınırına tabi değildir. → **A8**, **D15**, **D32**, **D33**
- **R14.** Yayına alma, rezerve etme ve satıldı işaretleme eylemleri **eşzamanlı**
  çağrıldığında tek biri başarılı olur; kaybeden **kural gereği** reddedilir, "tekrar dene"
  denmez. → `0007`, **D8**
- **R15.** Bir ilanın vitrindeki görünürlüğü, onu değiştiren her yazma yolunda **aynı
  işlemde** güncellenir — yayına alma, her durum geçişi, öne çıkarma ve araç düzenlemesi
  dâhil — **ve aracın medya sırası** (**D41**): sıra değişince vitrindeki kapak değişir.
  → **D12**, **D41**
- **R16.** Bir ilanın vitrindeki **adresi ilk yayına alma anında** üretilir ve bir daha
  değişmez; yayından kaldırılıp yeniden yayınlanan ilan **aynı adrese** döner. → **D13**
- **R17.** Arşivlenen araç panel listesinin **varsayılan** görünümünde yer almaz; açık bir
  filtreyle görülebilir. Aynı koşul listenin **toplam sayısına ve özet sayaçlarına** da
  uygulanır — liste ile sayaç aynı evreni sayar. → **D19**, **D35**
- **R18.** Durum geçişi aracın **son güncellenme** zamanını da ilerletir; panel listesinin
  "son güncellenen" sıralaması geçişi görür. Aynı şey **araç düzenlemesi** için de
  geçerlidir: personelin her yazması o sıralamada görünür. (Öne çıkarmanın aynı borcu
  `0012c`'de — **D36**.) → **D20**, **D37**
- **R19.** Panel listesi ve eylem ekranları **yükleniyor**, **boş**, **başarılı** ve
  **hata** durumlarını ele alır. **Kısmi başarı** yalnız çok kayıtlı bir eylemin ekranında
  doğar ve `0012c`'ye taşındı (**D39**); `docs/frontend.md` § 3'ün dördüncü satırı yerinde
  kalıyor — o kural *her* toplu eylem ekranını bağlıyor, yalnız onu getireni değil.
  → `docs/frontend.md` § 3
- **R20.** Panel personelin tercih ettiği dilde çalışır; Arapça'da RTL.
- **R21.** Yayına alma **A2 dışında iki nedenle** daha reddedilebilir: aracın lokasyon
  ataması bekliyor olması (**R13**) ve ilanın **adresinin üretilememesi**. İkisi de kod
  olarak bildirilir ve ikincisi personelin düzeltemeyeceği bir reddir — panel bunu böyle
  söyler. → **D25**
- **R22.** Yayından kaldırılıp **yeniden yayınlanan** ilan vitrine **eski yerinden** döner:
  adresi de ilk yayın zamanı da değişmez. Duyuru yine yayılır ve tüketicinin "bu ilk yayın
  değil" diyebilmesi için ilk yayın zamanını taşır. → **D13**, **D31**

---

## Constraints

### Uyulması zorunlu

- **C1.** Tüm eylemler **oturum gerektirir**. → `0006` R7
- **C2.** Durum geçişi mantığı **agregatta** kalır; endpoint katmanına taşınmaz.
  → Yasak Listesi 20, **C7**
- **C3.** Duyurular outbox ile, en-az-bir-kez ve idempotent.
  → `docs/architecture.md` **§ 2.C** (§ 7 Karar Defteri'dir; eski atıf **D21** ile düzeltildi)
- **C4.** Veritabanında çevrilmiş durum adı saklanmaz; kod saklanır. → Yasak Listesi 22
- **C5.** Fiyat değişikliği olursa fiyat geçmişine yazılır. → **A6**, `0010a` R8
- **C6.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner — "hangi koşul
  eksik" bilgisi de kod olarak döner, metin olarak değil. Çalışma zamanında değişen bir
  kümeye (zorunlu alan listesi) kod üretilmez: o bilgi kodun **yapısal parametresidir**
  (`docs/conventions.md` § 5). → Yasak Listesi 23–24, **D24**
- **C7.** Mevcut agregat davranışı yeniden yazılmaz: `Listing.Publish`, `Reserve`,
  `ReturnToShowcase`, `MarkSold`, `Unpublish`, `Archive`, `ChangeShowcaseSortWeight` ve
  `Vehicle.MarkArchived`, `MarkAwaitingBranchAssignment`, `EnsurePublishable` bugün vardır.
  Bu spec onların üzerine komut katmanı kurar. **Adıyla yazılı ÜÇ istisnası vardır:**
  (a) araç düzenleme komutu vitrin yansıtmasını çağırır (**D12**), iki olay yayar (**D11**)
  ve eksik `Touch` çağrısını kazanır (**D37**); (b) `Listing.Archive` taslaktan da geçer
  (**D10**); (c) `0012a`'nın medya komutu da yansıtmayı çağırır (**D41**).
- **C8.** Sayı içeren cümleler ICU ile kurulur ("3 ilan yayına alındı"). → Yasak Listesi 31
- **C9.** Yeni migration yalnızca `vehicle_catalog` şemasına dokunur. → Yasak Listesi 3
- **C10.** Her kabul kriteri en az bir testle karşılanır. → `AGENTS.md` Altın Kural 5
- **C11.** Outbox yazıcısı **konkret** tipiyle kaydedilir:
  `OutboxWriter<VehicleCatalogDbContext>`. Paylaşılan bir soyutlama altında kayıt yasaktır ve
  `Architecture.Tests/OutboxYazicisiTests` bunu denetliyor — üçüncü iddiası
  `IOutboxDbContext` uygulayan **her** modülü taradığı için `VehicleCatalog` outbox'ı
  kurduğu anda kapıya dâhil olur. → **D14**, Yasak Listesi 16
- **C12.** **Görünürlük kararını** — hangi ilanın vitrinde satırı olduğunu — veren tek yer
  **yansıtmadır** ve bu karar başka hiçbir yerde tekrarlanmaz. Kopyalanmış kolonları
  (kapak URL'i, şube adları) besleyen olay işleyicileri bunun dışındadır; onlar görünürlük
  kararı vermez. → Yasak Listesi 41, `0001` C10, **D12**, **D34**
- **C13.** Reddin HTTP karşılığı sabittir: A2'nin ve R21'in yayın reddi **`422`**, A3'ün
  geçiş reddi (`listing.invalid_status_transition`) **`409`**, `concurrency.lost` **`409`**,
  bulunamayan araç **`404`**. Toplu eylem bunların hiçbirini istek düzeyine taşımaz (**D6**).
  → `docs/conventions.md` § 5, **D26**

### KAPSAM DIŞI

- **X1.** **Rezervasyon KAYDININ yaşam döngüsü** — bitiş tarihi, süresi dolunca otomatik
  yayına dönüş, danışmana bildirim, iptal nedeni (`docs/domain.md` **R1–R6**) — *neden:* bu
  spec yalnızca **durum geçişini** kurar (R6 ile elle kaldırma dâhil); rezervasyon kaydının
  kendisi bir zamanlayıcı ve bir bildirim akışı gerektirir ve kendi spec'ini hak eder.
- **X2.** **Satış sonrası iş akışı** (satış kaydı, kâr, fatura) — *neden:* ERP/muhasebe
  `docs/architecture.md` § 6'da kapsam dışıdır.
- **X3.** **Fiyat düşüşü rozeti ve fiyat geçmişinin ziyaretçiye gösterilmesi** — *neden:*
  `0002` § X6 bunu kapsam dışı bıraktı; vitrin kararıdır.
- **X4.** **Rol bazlı eylem kısıtı** ("danışman yayına alamasın") — *neden:* ADR 0003 unsur 3
  alan/kayıt düzeyi yetkiyi ayrı bir karara bağladı ve § 6 bunu "basit rol modelini izin
  motoruna dönüştürür" diye uyardı. **Karar verildi: bu spec'te rol ayrımı yok** (**D22**);
  madde geçerli ve `0012a` X2 ile aynı yerde duruyor.
- **X5.** **Dışa aktarma** ve **görüntülenme sütunu** — *neden:* biçim kararı ve `Engagement`
  modülünün yokluğu.
- **X6.** **Süresi geçmiş rezervasyon uyarısı** (mockup'taki "1 rezervasyon 3 günü geçti") —
  *neden:* X1'e bağlıdır.
- **X7.** **Şube transferi AKIŞI** (hedef şube seçimi, onay, transfer geçmişi) — *neden:*
  `0010b` X23 transferi kendi spec'ine bıraktı. **Ama şubenin değişmesi bugün mümkün**
  (araç düzenleme komutu şubeyi yazıyor) ve **A7** o değişimin duyurulmasını istiyor; bu
  spec yalnız **duyuruyu** ekler, akışı kurmaz. → **D11**
- **X8.** **Fotoğrafın araca bağlanması, sıralanması, kapak ve `alt` metni, tekrar deneme
  ekranı** — *neden:* `0012a`'nın konusu ve bu spec'in önkoşulu.
- **X9.** **İyimser eşzamanlılık / kayıp güncelleme koruması** — *neden:* `0010b` X20
  geçerliliğini koruyor; R14 **durum geçişi** yarışını kapsar, iki personelin aynı aracı
  düzenlemesini değil.
- **X10.** **Toplu eylem** (seçim, kısmi başarı, seçim sınırları) — *neden:* `0012c`'nin
  konusu (**D39**). Bu dilim o eylemin üzerine kurulacağı **tekil** komutları ve duyuru
  altyapısını kurar.
- **X11.** **Öne çıkarma** (işaretleme, kaldırma, seed'in hizalanması) — *neden:* `0012c`;
  sıralama işi toplu eylemle aynı ekranda yaşıyor (**D39**, **D36**).

---

## Context

### Mevcut davranış — doğrulandı (2026-10-11, `origin/main` = `18e4adf`)

> **Ölçüm tazelendi.** Bu bölüm `b99912a` üzerinde yazılmıştı; `0012a` arada bitti (#54→#57)
> ve **üç maddesi yanlışlandı**: modülün artık bir yazma ucu var, `EnsurePublishable`'ın
> imzası ve gövdesi değişti, A2'yi ölçen okuma `AreAllReadyAsync` değil `ReadyAmongAsync`.
> Üçü aşağıda düzeltildi. Bayat ölçüm, varsaymanın kibarca yazılmış hâlidir (AP-10) —
> kriterler bu bölümün üstüne yazılacak.

- **Agregat tarafı hazır.** `Listing` bugün `CreateDraft`, `Publish`, `Reserve`,
  `ReturnToShowcase`, `MarkSold`, `Unpublish`, `Archive`, `ChangeShowcaseSortWeight`
  taşıyor; `Vehicle` ise `EnsurePublishable`, `AttachMediaAsset`,
  `MarkAwaitingBranchAssignment`, `MarkArchived`, `AssignBranch`. **Eksik olan komut ve uç
  katmanıdır** — C7 bu yüzden var. `EnsurePublishable`'ın imzası `0012a` D11 ile değişti:
  hazırlık bilgisini parametre alıyor ve bağlı varlıklarla hazır varlıkların **kesişimine**
  bakıyor (`Vehicle.cs:353`, eski ölçüm `:293` diyordu).
- **Hiçbirinin üretim çağıranı yok.** Ölçüldü: `Publish`/`Reserve`'ün tek üretim çağıranı
  geliştirme seed'i (`ShowcaseSeeder.cs:148,155`); `MarkSold`, `Unpublish`,
  `ReturnToShowcase`, `Archive`, `MarkArchived`, `MarkAwaitingBranchAssignment` yalnız
  testlerden çağrılıyor; **`ChangeShowcaseSortWeight`'in hiçbir çağıranı yok** — üretimde de
  testte de sıfır.
- **Modülün yazma uçları üç tane ve hiçbiri EYLEM ucu değil.** `0012a`'dan sonra ölçüldü:
  `POST /` (kayıt), `PUT /{id}` (düzenleme) ve `PUT /{id}/media` (medya listesi) —
  `VehicleAdminEndpoints.cs:30,56,62`. Durum geçişi ve toplu eylem ucu **yok**; eski ölçüm
  "hiçbir yazma ucu yok" diyordu ve `0012a` bunu yanlışladı.
- **A2'nin medya koşulu artık ÖLÇÜLEBİLİR ama tüketicisi yok.** `0012a` D5 `Media`'ya
  `ReadyAmongAsync`'i ekledi (`MediaAssetContracts.cs:72`) ve D6 `EnsurePublishable`'ı
  genişletti; ikisinin de **üretim çağıranı yok** — `0012a` D18 bunu adıyla devretti ve
  tüketicisi bu spec'tir.
- **Modülün OUTBOX'ı yok.** `VehicleCatalogDbContext` yalnız `IInboxDbContext` uyguluyor
  (`:13`); outbox `DbSet`'i yok ve modülde domain → entegrasyon çevirisi yapan bir birim
  yok. Diğer üç modülde var: `IdentityUnitOfWork.cs:56`, `LeadsUnitOfWork.cs:56`,
  `BranchIntegrationEvents.cs:50` (`Media` da `MediaProcessingService` ile yazıyor). `Listing`'in yaydığı altı domain olayı (`ListingEvents.cs`)
  bir listeye giriyor, EF listeyi `Ignore` ediyor (`ListingConfiguration.cs:16`) ve **okuyan
  yok.** → **D14**
- **Vitrin okuma modelini yazan üretim yolu yok.** `ShowcaseListingProjector.Project`'ın tek
  üretim çağıranı geliştirme seed'i (`ShowcaseSeeder.cs:160`); hiçbir komut
  `ShowcaseListings`'e yazmıyor — araç düzenleme komutu da yazmıyor
  (`UpdateVehicleCommand.cs:231`). → **D12**
- **İlan adresi yalnız okuma modelinde.** `listings` tablosunda slug kolonu yok
  (`ListingConfiguration.cs`); `Project` görünmeyen ilan için `null` dönüyor ve çağıran
  satırı siliyor (`ShowcaseListingProjector.cs:17-18`). Oysa `ListingSlug` doc'u adresin
  "İLK YAYINA ALMA anında üretilip BİR DAHA DEĞİŞMEYECEĞİNİ" söylüyor
  (`ListingSlug.cs:10-11`). → **D13**
- **A8'in yazma tarafı yok.** `BranchDeactivatedHandler` yalnız okuma modeli satırını siliyor
  (`BranchEventHandlers.cs:50-52`); ilan `Yayında` kalıyor ve bayrağı kimse yazmıyor. Çıkış
  kapısı var: araç düzenleme komutu şube değişince bayrağı temizliyor
  (`UpdateVehicleCommand.cs:208-211`). → **D15**
- **Yayın reddinin kodu dokümanla uyuşmuyor.** `docs/conventions.md:121` ve
  `docs/frontend.md:102` örnek olarak `vehicle.cannot_publish.no_media` yazıyor; koddaki
  sabit `vehicle.media_required` (`Vehicle.cs:13`) ve bu dize başka **hiçbir** yerde yok —
  çeviri kataloglarında da. Bugün hiçbir uç onu dönmüyor. → **D2**
- **Eşzamanlılık mekanizması hazır.** `InvariantGuard` satır kilidi + tek transaction
  uyguluyor ve mekanizma kaybını `concurrency.lost`'a çeviriyor
  (`InvariantGuard.cs:88-93`); `ConcurrencyErrors.cs:11-13` kural gereği reddin ayrı
  kalmasını `0007`/AC-12 ile şart koşuyor. → **D8**
- **Vitrin tüketicisi hazır.** `ShowcaseListingProjector` ve `0001`/`0002`'nin okuma modeli
  yayın durumuna göre görünürlüğü zaten uyguluyor; bu spec yeni bir vitrin davranışı
  getirmiyor, var olanı **besliyor**.
- **Panelin sayfa sınırları yazılı:** varsayılan 25, üst sınır 100, ikisi de dağıtım
  yapılandırmasından (`CatalogAdminPagingOptions.cs:6`). → **D4**
- **Panel listesi arşivi filtrelemiyor:** `VehicleAdminQueries` `context.Vehicles`'ı arşiv
  koşulu olmadan okuyor (`:39-48`) ve `VehicleAdminListFilter`'da durum alanı yok. → **D19**

#### İkinci turun ölçümleri (2026-10-11, `18e4adf`)

- **A2'nin çekirdek alan koşulu REDDEDİLEMİYOR.** `Vehicle.Create` dokuz çekirdek alanı
  parametre olarak zorunlu alıyor, `0010a` D8 onları kayıt anında şart koşuyor ve kodun
  kendi yorumu bunu yazıyor: *"yani her zaman doludurlar"* (`VehicleAdminQueries.cs:207`).
  Çekirdek alanı boş bir araç kaydı kurulamıyor. → **D23**
- **Zorunlu opsiyonel alan kümesi ÇALIŞMA ZAMANINDA değişiyor.** Hangi alanın zorunlu
  olduğu operatör ayarı (`FieldFormState.Required`, `VehicleFieldSetting`); alan listesi
  kodda sabit (`VehicleFieldDefinitions`). Panel aynı olguyu bugün `EksikBilgiVar` ile
  hesaplayıp gösteriyor (`VehicleAdminQueries.cs:209`, `0010b` AC-16). → **D24**
- **Yayın iki ayrı nedenle daha düşebiliyor.** Yansıtma lokasyon ataması bekleyen aracı
  `null` ile reddediyor (`ShowcaseListingProjector.cs:32-35`); adres üretimi ise kanonik
  model adı Latin harfe inmiyorsa `listing_slug.invalid_source` dönüyor
  (`ListingSlug.cs:41-45`) ve kanonik ad başka modülün verisi
  (`IReferenceQueries.ResolveCanonicalNamesAsync`). → **D25**
- **Yayın yolu ÜÇ modülden okuyacak, iki değil.** D12'nin bedel satırı iki sayıyordu;
  yansıtmanın imzası üçünü istiyor: şube adları (`Dealership`), kapak URL'i (`Media`) ve
  adres için kanonik ad (`VehicleReference`) — `ShowcaseListingProjector.Project(…)`. Üçü
  de § 2.A'nın saydığı salt okuma, tek atlama. → **D25**
- **Üst sınır bugün KIRPILIYOR.** `VehicleAdminListRequest.Normalize` `pageSize`'ı
  `Math.Min` ile üst sınıra indiriyor ve yorumu *"hata dönmez… adres çubuğundan gelen bir
  ipucudur"* diyor (`CatalogAdminPagingOptions.cs:22-30`, `0010b` AC-26). D4 ise aşımda
  **red** istiyor: aynı sayı iki yüzeyde zıt davranacak. → **D29**
- **`Rezerve` ilan A8'de çıkışsız kalıyor.** `Unpublish()` yalnız `Yayında`dan geçiyor
  (`Listing.cs:179-183`) ve A3'te `Rezerve → Taslak` geçişi yok; yansıtma ise bayraklı aracı
  her durumda düşürüyor. Sonuç: vitrinden düşmüş, durumu `Rezerve` kalmış, tek çıkışı
  `Satıldı` olan bir kayıt. → **D32**
- **Vitrin okuma modeline yansıtmadan BAŞKA üç yazıcı var.** `MediaAssetReadyHandler` kapak
  URL'ini yazıyor (`:63`), `BranchDetailsChangedHandler` şube adlarını yazıyor
  (`BranchEventHandlers.cs:30`), `BranchDeactivatedHandler` satırı siliyor (`:52`). Üçü de
  `0001`/`0003`/`0011`'in kriterleriyle korunuyor. → **D34**
- **Yayın zamanı İLK yayında donuyor:** `PublishedAt ??= clock.UtcNow` (`Listing.cs:137`) ve
  vitrin sıralaması `ShowcaseSortWeight DESC, PublishedAt DESC`
  (`ShowcaseListingQueries.cs:38-40`). → **D31**
- **Liste ile özet aynı evreni saymıyor olacak.** `ListAsync` (`:39`) ve `GetSummaryAsync`
  (`:71`) ikisi de `context.Vehicles.CountAsync()` ile **koşulsuz** sayıyor; özetin "çeviri
  eksik" sayısı da tüm ilanlar üzerinden (`0010b` AC-23). → **D35**
- **`Vehicle` hiç domain olayı YAYMIYOR** ve `VehicleCatalog.Contracts` altında `Events`
  klasörü **yok** — sekiz entegrasyon kaydı sıfırdan yazılacak. Emsal çift yönlü:
  `BranchOpened` adları taşıyor ki *"alıcı ikinci bir sorgu yapmak zorunda kalmasın"*
  (`BranchIntegrationEvents.cs:50`), `LeadCaptured` ise bilerek yalnız istatistik taşıyor
  (L6). → **D30**
- **Düzenleme komutu "son güncellenme"yi ilerletmiyor.** `Vehicle.Touch`'ın çağıranları
  yalnız `UpdateVehicleMediaCommand.cs:82` (`0012a`) ve `ChangePrice` (`Vehicle.cs:274`);
  `UpdateVehicleCommand` onu çağırmıyor. Başlık, kilometre veya renk düzeltmesi bugün "son
  güncellenen" sıralamasında görünmüyor. → **D37**
- **Öne çıkarma kolonu bugün iki değerli DEĞİL.** `ChangeShowcaseSortWeight(int)` doğrulama
  yapmıyor, olay yaymıyor ve çağıranı yok (`Listing.cs:203`); geliştirme seed'i ise aynı
  kolona `1..N` yazıyor (`ShowcaseSeeder.cs:139`). → **D36**

### Geçerli domain kuralları

**A2** (yayın koşulları; çekirdek alan koşulu kayıt anında bağlar → **D23**) · **A3**
(geçiş tablosu — `Taslak → Arşiv` **D10**, `Rezerve → Taslak` **D32** ile eklendi) ·
**A4** (`Satıldı` terminal; aynı araç geri alınırsa **yeni kayıt**) · **A5** (şase tekilliği,
arşiv muaf) · **A6** (fiyat geçmişi) · **A7** (şube değişince `VehicleRelocated`) ·
**A8** (şube kapatılınca lokasyon ataması bekler) · **R1–R6** (rezervasyon; R6: rezerve araç
vitrinde rozetle görünür, iletişim formu kapalı) · **F2** (zorunluluk yalnızca o andan
sonrasına uygulanır — ama **A2** yayın anında bağlayıcıdır) · **M3** (yalnız `Hazır` varlık
A2'yi karşılar).

### Bilinen tuzaklar

1. **A2'yi yalnız medya üzerinden kontrol etmek.** `EnsurePublishable()` medya şartını
   kontrol ediyor (`0012a` D6 ile "hazır" şartına genişliyor); A2'nin diğer **üç** koşulu
   (çekirdek alanlar, fiyat/fiyat sorunuz, zorunlu opsiyonel alanlar) hiçbir yerde yayın
   anında kontrol edilmiyor. R2 bu boşluğu kapatmak zorunda.
2. **Eksiklerden yalnız birini söylemek.** Tek kod dönmek personeli tek tek denemeye zorlar;
   aynı hata `0010a` AC-42'de ölçüldü ve `Listing.TitleTooLong`/`DescriptionTooLong` tam bu
   yüzden ikiye ayrıldı (`Listing.cs:19-27`). → **D2**
3. **Toplu eylemi tek işlem sanmak.** 20 ilandan 3'ü geçersizse "hepsi başarısız" demek
   personeli tek tek denemeye zorlar; "hepsi başarılı" demek yalan söyler. → **D3**
4. **`Satıldı`dan geri dönüş.** A4 terminal diyor; arayüzde "geri al" düğmesi koymak kuralı
   deler. Yanlış işaretleme **yeni kayıt** açılarak düzeltilir.
5. **Arşivin şase tekilliğini serbest bırakmasını unutmak.** A5 muafiyeti arşivleme
   eyleminin yan etkisidir ve ADR 0006 bunu `0012`'nin borcu olarak yazdı; bayrak ayrı bir
   işlemde yazılırsa tekillik indeksi **yanlış kararı sessizce** verir. → **D9**
6. **Eşzamanlı satıldı işaretlemede "tekrar dene" demek.** Kaybeden kural gereği
   reddedilmiştir; ona tekrar denemesini söylemek sonsuza kadar denemesini söylemektir.
   → **D8**
7. **Vitrini olaya bırakmak.** Vitrin okuma modeli **aynı modülün** türetilmiş verisidir;
   outbox üzerinden güncellemek panelin kendi yazmasını nihai tutarlı yapar ve `0009`/D5
   slug'ı okuma modelinden okuduğu için yayından sonraki yönlendirme `404` verir. → **D12**
8. **Outbox yazıcısını paylaşılan bir soyutlama altında kaydetmek.** Bu tuzak kuruldu ve
   bedeli **ödendi**: non-generic `IOutboxWriter`'ı üç modül kendi `DbContext`'iyle
   kaydediyordu, DI'da son kayıt kazanıyordu ve `Identity`'nin duyurusu **hiç kalıcı
   olmuyordu** — `0006`'nın ikinci kaçan hatası, `7e8bdef` (#52) ile kapandı ve arayüz
   **silindi**. Dersi ölçümün kendisi: belirti "yanlış tabloya yazılıyor" sanılmıştı, gerçek
   "hiç yazılmıyor"du; bu aileden bir şüphede **tabloyu say**, kodu okumakla yetinme.
   `VehicleCatalog` outbox tutan **dördüncü** modül olacak. → **C11**
9. **A8'i yalnız `Yayında` ilan sanmak.** `Unpublish()` `Rezerve`den geçmiyor; kapalı
   şubedeki rezerve ilan vitrinden düşer ama durumu `Rezerve` kalır ve tek çıkışı `Satıldı`
   olur. Ölçüldü (`Listing.cs:179-183`); A8'in *"sessizce vitrinde kalmazlar"* amacı tam
   burada deliniyor. → **D32**
10. **Arşivi listeden gizleyip sayaçta tutmak.** İki sorgu da bugün koşulsuz sayıyor
    (`:39`, `:71`); yalnız listeye koşul eklenirse panel "12 araç" yazarken 10 satır
    gösterir ve kimse hangi sayının yanlış olduğunu bilmez. → **D35**
11. **Toplu seçimi `Normalize` gibi kırpmak.** En kısa yol o ve bugünkü kod onu
    öğretiyor (`Math.Min`, `0010b` AC-26). Kırpma, "17 ilan uygulanmadı"yı sessizce
    üretir; seçim adres çubuğundan gelen bir ipucu değil, personelin açık eylemidir.
    → **D29**

### Verilen kararlar

Hepsi Takım Yöneticisi onayıyla verildi, **iki turda**: **D2–D21** `0012`'nin 2026-10-11
CLARIFY turunda, **D22–D38** aynı gün `0012a` bittikten sonra yapılan ikinci turda. **D1**,
`0012a`'da yazılı bölme kararıdır ve burada tekrarlanmaz.

- **D2 — Yayın reddi koşul başına bir kod döner ve eksiklerin HEPSİ birlikte gelir.** Kodlar
  ortak önekli: `vehicle.cannot_publish.*`. Mevcut `vehicle.media_required` sabiti
  dokümanlarda iki yerde yazılı olan ada çevrilir. *Gerekçe:* konvansiyonun "yayınlanmış
  kodun anlamı değişmez" kuralı bağlamıyor — bu kodu bugün hiçbir uç dönmüyor. *Bedel:*
  yanıt bir **liste** taşır; uçta dört kod × üç dil = on iki çeviri anahtarı ve hata alanı
  bir cümle değil liste olur.
- **D3 — Toplu eylemde işlem sınırı İLAN BAŞINADIR.** *Gerekçe:* kısmi başarı ancak böyle
  mümkün; tek transaction, bir ilanın reddinde on dokuzunu geri alırdı. *Bedel:* 25 ilanlı
  bir eylem 25 transaction demektir ve süresi doğrusal büyür.
- **D4 — Seçim sayısının üst sınırı panelin sayfa üst sınırıdır (100).** Aşılırsa istek
  reddedilir. *Gerekçe:* "sayfadaki hepsini seç" doğal üst sınırdır ve sınır zaten
  yapılandırmadan okunuyor; tanımsız sınır, "hepsini seç"i sınırsız bir yazma turuna
  çevirirdi. *Bedel:* bir sayı iki anlam taşır (sayfa boyutu ve toplu eylem sınırı) ve bu
  bağ yapılandırmada görünür olmak zorunda.
- **D5 — Boş seçim REDDEDİLİR; yinelenen kimlikler teklenir.** *Gerekçe:* boş seçime `200`
  dönmek, hiçbir şey yapmamış bir isteği başarı gibi gösterir; aynı ilanı iki kez göndermek
  iki duyuru üretirdi. *Bedel:* arayüz düğmeyi seçim boşken kapatmak zorunda — yoksa
  personel reddi hata sanır.
- **D6 — Yanıt `200` + ilan başına sonuç listesidir; hepsi başarısız olsa bile.**
  *Gerekçe:* istek başarıyla işlendi; `ProblemDetails` istek düzeyi hatalar içindir ve
  "hangi ilan neden olmadı" bilgisi gövdeye aittir. *Bedel:* istemci artık HTTP koduna
  bakarak başarıyı anlayamaz, gövdeyi okumak zorundadır — karar `docs/conventions.md` § 5'e
  yazıldı ki bir sonraki spec aynı soruyu tekrar sormasın.
- **D7 — "Kısmi başarı" `docs/frontend.md` § 3'e DÖRDÜNCÜ durum olarak girdi.** *Gerekçe:*
  R19 beş durum istiyordu ama § 3 üç durum tanımlıyordu ve "kısmi başarı" terimi hiçbir
  belgede yoktu (ölçüldü: `grep -rn "kısmi başarı" docs/` → sıfır). Dayanağı olmayan bir
  kriter doğrulanamaz. *Bedel:* dört spec'in atıf yaptığı bir belge değişti; kural artık
  **her** toplu eylem ekranını bağlıyor.
- **D8 — Eşzamanlılıkta kaybeden KURAL GEREĞİ reddedilir.** Kaybeden kilidin altında yeni
  durumu okur ve `listing.invalid_status_transition` alır; `concurrency.lost` yalnız kilit
  zaman aşımı veya serileştirme hatasında döner. *Gerekçe:* `ConcurrencyErrors.cs:11-13`
  ayrımı `0007`/AC-12 ile şart koşuyor — kural reddi kalıcıdır, mekanizma kaybı geçicidir.
  *Bedel:* iki kod, iki mesaj, iki test; panel `409`'dan sonra satırı tazelemek zorunda.
- **D9 — Arşivleme aracın bayrağını AYNI işlemde yazar** ve bu kendi kriteriyle sınanır.
  *Gerekçe:* ADR 0006 bunu `0012`'nin borcu olarak yazdı (`0006-sase-tekilligi.md:69`) ama
  `0012`'de hiçbir R/C/AC/DoD maddesi onu istemiyordu; tuzak listesi hatırlatmadır, kısıt
  taahhüttür. `0010a` R11 tam bu farktan öksüz kaldı. *Bedel:* yok — bedeli ADR 0006 kabul
  etti.
- **D10 — `docs/domain.md` A3'e `Taslak → Arşiv` geçişi eklendi.** *Gerekçe:* `Archive()`
  bugün yalnız `Yayında|Satıldı`dan geçiyor (`Listing.cs:193`) ve `0010b` X19 silmeyi arşive
  bağladı; ikisi birlikte, yanlış şaseyle açılmış ve hiç yayınlanmamış bir taslağı **kalıcı
  olarak** silinemez ve arşivlenemez bırakıyordu — tekillik indeksi `NOT is_archived`
  filtreli olduğu için o şase bir daha kullanılamazdı. A5'in muafiyeti yanlış kaydın
  emekliye ayrılabilmesi için var; çıkışı olmayan muafiyet kâğıt üstünde kalır. *Bedel:*
  **sözlükteki bir kural değişti** — en pahalı değişiklik türü. Vitrin etkilenmez: yansıtma
  yalnız `Yayında|Rezerve` gösteriyor (`ShowcaseListingProjector.cs:36`).
- **D11 — Bu spec SEKİZ entegrasyon olayı üretir ve hepsi `Vehicle*` öneklidir.**
  § 1'in altısı (`VehiclePublished`, `VehicleUnpublished`, `VehiclePriceChanged`,
  `VehicleReserved`, `VehicleSold`, `VehicleRelocated`) + § 1'e eklenen ikisi
  (`VehicleReturnedToShowcase`, `VehicleArchived`). `PriceChanged` ve `Relocated`'ın
  tetikleyicisi **araç düzenleme komutudur** ve bu C7'nin adıyla yazılı istisnasıdır.
  *Gerekçe:* § 1 beş adı önek yazmadan sayıyordu, § 2.B örneği ise `VehiclePriceChanged`
  diyordu — önek kararsızdı. Agregatın yaydığı `ReturnedToShowcase` ve `Archived`'ın § 1'de
  adı yoktu. Fiyat ve şube **bugün** değişiyor (`UpdateVehicleCommand.cs:201-204` ve
  `:208-211`) ve A7 duyuruyu istiyor; iki olguyu "tüketicisi yok" diye ertelemek, onları
  sessizce kaybetmeye devam etmekti. *Bedel:* kapanmış bir spec'in komutuna dokunulur; iki
  olayın henüz tüketicisi yoktur (`0003`'ün tersi durum ve bilinçli) ve DoD maddesi
  "üretilen her olayın adı § 1'dekiyle birebir aynı" biçiminde yazılmak zorundaydı.
- **D12 — Vitrin okuma modeli, ilanı değiştiren HER yazma yolunda aynı işlemde güncellenir;
  yazan tek yer yansıtmadır.** Araç düzenleme komutu da yansıtmayı çağırır. *Gerekçe:*
  bugün hiçbir komut okuma modeline yazmıyor; yayına alma düğmesi hiçbir ziyaretçi sayfasını
  değiştirmezdi ve Intent'in başarı ölçütü ölçülemez kalırdı. Outbox'a bırakmak, panelin
  kendi yazmasını nihai tutarlı yapar (Tuzak 7). *Bedel:* yayın komutu iki modülden senkron
  okuma yapar (şube adları `Dealership`, kapak URL'i `Media` — ikisi de § 2.A'nın saydığı
  meşru okumalar), işlem uzar ve `Dealership` erişilemezken yayına alma düşer.
- **D13 — İlan adresi YAZMA modelinde yaşar ve ilk yayında bir kez yazılır.** İkinci kez
  yazılmaz; yansıtma onu kopyalar. *Gerekçe:* adres bugün yalnız okuma modelinde ve o satır
  yayından kalkınca siliniyor; "yayından kaldır → model yılını düzelt → yeniden yayınla"
  dizisi **farklı bir adres** üretir ve paylaşılmış bağlantı kırılır. Değişmezlik, silinen
  bir satırda yaşayamaz. *Bedel:* yeni kolon ve migration; kanonik model adının yayın anında
  çözülmesi gerekir.
- **D14 — `VehicleCatalog`'un outbox'ı ve domain → entegrasyon çevirisi bu spec'te kurulur.**
  *Gerekçe:* modülün outbox'ı yok ve altı domain olayı okuyansız duruyor; R11 bu altyapı
  olmadan karşılanamaz. *Bedel:* `vehicle_catalog` şemasına yeni migration (C9 serbest) ve
  bir dağıtıcı kaydı — R'lerin hiçbirinin ima etmediği, ilk tahminde görünmeyen bir iş.
  **Yazıcı konkret tiple kaydedilir** (C11): paylaşılan soyutlama 2026-10-11'de silindi
  (`7e8bdef`, #52) ve geri gelmesini `OutboxYazicisiTests` yasaklıyor.
- **D15 — A8'in yazma tarafı bu spec'te kurulur.** Şube kapatıldığında ilan yayından kalkar
  **ve** aracın "lokasyon ataması bekliyor" bayrağı aynı işlemde yazılır; bayraklı araç
  panel listesinde ayırt edilir (yönetim sözleşmesi **eklemeli** bir alan kazanır).
  *Gerekçe:* bugün A8 yalnız okuma modelinde yaşıyor — satır siliniyor ama ilan `Yayında`
  kalıyor; biri yeniden yayınladığında araç **ölü bir şubeyle** vitrine döner ve bu sessiz.
  *Bedel:* olay işleyicisi artık yazma modeline de yazar ve idempotentliği daha çok önem
  taşır; çıkış kapısı araç düzenleme ekranıdır (şube atanınca bayrak temizlenir).
- **D16 — "Öne çıkar" iki değerli bir anahtardır: öne çıkarıldı `1`, değil `0`.** Başka değer
  yazılmaz. Değişiklik vitrin satırına aynı işlemde yansır ve **durum geçişi olmadığı için**
  R11'in duyurusuna girmez. *Gerekçe:* serbest tamsayı paneli sıralama editörüne çevirir ve
  personele "hangi sayı daha önemli" sorusunu sordurur; mockup bir **düğme** gösteriyor, bir
  alan değil. `0015` aynı kolonu migration'sız sıralamaya çevirebilir. *Bedel:* öne
  çıkarılmış ilanlar kendi aralarında sıralanmaz — eşitlik yayın tarihiyle kırılır
  (`ShowcaseListingQueries.cs:38-40`) ve bu bilinçli.
- **D17 — Rezervasyonun ELLE kaldırılması kapsamdadır; kaydın kendisi X1'de kalır.**
  *Gerekçe:* A3 `Rezerve → Yayında` geçişini tanıyor ve `ReturnToShowcase()` zaten var; onu
  dışarıda bırakmak `Rezerve`'nin tek çıkışını `Satıldı` yapardı ve vazgeçen müşteriden
  sonra ilanı geri almanın yolu kalmazdı. *Bedel:* bir komut, bir olay ve bir kriter daha;
  X1'in sınırı "iptal nedeni hâlâ dışarıda" görünecek biçimde yeniden yazıldı.
- **D18 — Toplu uygulanabilen eylemler: yayına al · yayından kaldır · satıldı işaretle ·
  arşivle · öne çıkar.** Rezerve etme ve rezervasyonu kaldırma **tek ilan** üzerinde çalışır.
  *Gerekçe:* rezervasyon bir müşteriye aittir; yirmi aracı kimseye rezerve etmek, X1'in
  dışarıda bıraktığı kaydın yerine geçmeye çalışmaktır. *Bedel:* iki eylemin toplu menüde
  olmaması arayüzde söylenmek zorunda — yoksa eksiklik sanılır.
- **D19 — Arşivlenen araç panel listesinin varsayılan görünümünde YOK; açık bir filtreyle
  görülür.** *Gerekçe:* arşiv bu projenin "silme"sidir (`0010b` X19); varsayılan listede
  kalırsa liste sonsuz büyür ve eylem hiçbir şey yapmamış görünür. *Bedel:* yönetim
  sözleşmesi eklemeli bir filtre kazanır ve filtre denetimi kendi üç durumunu getirir.
- **D20 — Durum geçişi aracın "son güncellenme" zamanını da ilerletir.** *Gerekçe:* panel
  listesinin "son güncellenen" ekseni `Vehicle.UpdatedAt` (`0010b` D25) ve geçişler yalnız
  `Listing`'e yazıyor; personel bir aracı yayına alır, sıralar ve araç yerinden kıpırdamazdı.
  Listenin tek satırı iki agregattan besleniyor. *Bedel:* "son güncellenen"in tanımı
  "herhangi bir yazma" hâline gelir ve D25'in yanına yazılmak zorunda.
- **D21 — C3'ün atfı düzeltildi:** outbox kuralı `docs/architecture.md` **§ 2.C**'dedir;
  § 7 **Karar Defteri**'dir. *Gerekçe:* yanlış atıf, kuralı aramaya çıkan geliştiriciyi
  kararlar tablosuna gönderiyordu. *Bedel:* yok.

#### İkinci CLARIFY turu — D22–D38 (2026-10-11)

Takım Yöneticisi, `0012a` kapandıktan sonra, hepsi `18e4adf` üzerinde **ölçülerek**.

- **D22 — Eylemler ROL AYRIMI YAPMAZ; yetki tek bir şeydir: oturum.** (eski **S1**, kapandı)
  *Gerekçe:* ADR 0003 unsur 3 alan/kayıt düzeyi yetkiyi ayrı bir karara bağladı ve
  `architecture.md` § 6 bunu *"basit rol modelini izin motoruna dönüştürür"* diye uyardı;
  `0012a` da aynı kararla (X2) teslim edildi ve kapandı. İki dilimin aynı yetki modelini
  taşıması zorunlu. *Bedel, açıkça:* satış danışmanı da yayına alabilir, satıldı
  işaretleyebilir ve fiyat değiştirebilir. Küçük ekipte kabul edilebilir; aksi istenirse
  karar `0012a`'nın **kapanmış** uçlarını da açar — bedeli bugün daha büyük.
- **D23 — A2'nin çekirdek alan koşulu için KOD YAZILMAZ; reddedilebilen koşul sayısı ÜÇ.**
  *Gerekçe:* çekirdek alanı boş bir araç kaydı kurulamıyor — `Vehicle.Create` dokuzunu
  zorunlu parametre alıyor, `0010a` D8 kuralı kayıt anına koydu ve kodun yorumu *"her zaman
  doludurlar"* diyor (`VehicleAdminQueries.cs:207`). O kodu yazmak, testini yazmanın tek
  yolu agregata arka kapı açmak olan bir red üretirdi (Altın Kural 5). *Bedel:* sözlükteki
  A2 dört koşullu kalırken spec üç koşul doğruluyor; bu fark R2'de yazılı olmak zorunda,
  yoksa sonraki okuyucu eksik sanar.
- **D24 — Zorunlu opsiyonel alan reddi TEK KOD + ALAN ANAHTARLARI parametresidir.**
  Kod: `vehicle.cannot_publish.missing_required_fields`; gövdede eksik alanların anahtar
  listesi. Uç kodu kendi kataloğundan, alan adlarını kendi alan kataloğundan çevirir.
  *Gerekçe:* hangi alanın zorunlu olduğu **operatör ayarı**; alan başına kod üretmek
  `docs/conventions.md` § 5'in *"kod bir kez yayınlanınca anlamı değişmez"* kuralıyla
  çelişir. § 5 yapısal parametreyi zaten tanıyor (`{ "field": "price" }`). *Bedel:* D2'nin
  "dört kod × üç dil = on iki anahtar" hesabı **yanlış** — üç kod + alan adı anahtarları
  olur ve hata gövdesi iç içe bir yapı taşır.
- **D25 — Yayın reddi A2 ∪ {lokasyon ataması bekliyor, adres üretilemedi}'dir.** Birincisi
  personelin düzeltebileceği (şube ata), ikincisi düzeltemeyeceği bir reddir ve panel bunu
  böyle söyler. *Gerekçe:* ölçüldü — yansıtma bayraklı aracı `null` ile reddediyor
  (`ShowcaseListingProjector.cs:32-35`) ve `ListingSlug.Create` kanonik ad Latin harfe
  inmiyorsa `listing_slug.invalid_source` dönüyor (`ListingSlug.cs:41-45`). Tanımsız
  bırakılırsa bu `Result.Failure` ya uçta yutulur ya `500` olur; ikisi de Yasak 24'ü deler.
  *Bedel:* personelin çözemediği bir hata mesajı doğar (referans veriyi düzeltecek kişi
  başka) ve D12'nin bedel satırı düzeltilir: yayın yolu **üç** modülden senkron okur.
- **D26 — Reddin HTTP karşılığı sabitlendi:** A2/R21 reddi **`422`**, A3 geçiş reddi
  **`409`**, `concurrency.lost` **`409`**, bulunamayan araç **`404`** (→ **C13**).
  *Gerekçe:* merkezi eşleme yok, her modül kendi `Problems` switch'ini yazıyor
  (`VehicleAdminEndpoints.cs:303-305`) ve D8'in bedel satırı `409` varsayıyordu ama hiçbir
  yerde yazılı değildi. Panelin yapacağı iş farklı: `422`'de eksik koşul listesi gösterilir,
  `409`'da satır tazelenir. *Bedel:* aynı düğme iki ayrı hata yolu taşır ve ikisi de ayrı
  kriterle sabitlenir.
- **D27 — Toplu eylemde BULUNAMAYAN ilan, isteği değil o SATIRI düşürür** (`200` gövdesinde
  `vehicle.not_found`). Tek kayıtlı eylemde aynı durum **`404`**'tür. *Gerekçe:* D6 "istek
  başarıyla işlendi" diyor ve bu durumun normal sebebi **bayat liste** — başka personel
  arşivlemiştir. On dokuz geçerli kaydı bir bayat satır yüzünden reddetmek D3'ü anlamsız
  kılar. *Bedel:* aynı neden iki yüzeyde iki ayrı yanıt üretir; yazılmazsa geliştirici
  tutarlılık adına birini seçer ve yanlışını hiçbir kriter yakalamaz.
- **D28 — Zaten hedef durumda olan kayıt BAŞARISIZ sayılır; sonuç satırı kaydın GÜNCEL
  DURUMUNU taşır.** *Gerekçe:* A3'te kendine geçiş yok ve `Publish()` `Draft` değilse
  `listing.invalid_status_transition` dönüyor (`Listing.cs:131-135`); sessiz başarı sonuç
  listesini yalancı yapar. Ama tek başına o kod personele *"zaten yayında"* ile *"taslak ama
  medyası yok"*u ayırt ettirmiyor — ayrımı yapacak veri durumun kendisi ve cümleyi panel
  kurar (C6). *Bedel:* sonuç satırı bir alan daha kazanır; panel iki ayrı cümleyi aynı
  koddan kurar.
- **D29 — Toplu seçim KIRPILMAZ, reddedilir; sınır DÂHİLDİR; tekilleştirme sınırdan ÖNCE
  yapılır.** Yani 100 geçer, 101 düşer, 150 kimlikten 60'ı tekilse istek geçer.
  *Gerekçe:* bugünkü kod tersini öğretiyor — `Normalize` `pageSize`'ı `Math.Min` ile
  kırpıyor ve yorumu *"hata dönmez"* diyor (`CatalogAdminPagingOptions.cs:22-30`). Ama
  seçim adres çubuğundan gelen bir ipucu değil, personelin açık eylemidir; kırpmak
  *"17 ilan uygulanmadı"*yı sessizce üretir. Tekilleştirmeyi sonraya bırakmak, aynı kaydı
  iki kez göndereni sınır yüzünden reddeder. *Bedel:* aynı sayı iki yüzeyde zıt davranır ve
  üç cümlenin üçü de kriter olmak zorunda.
- **D30 — Sekiz olayın gövdesi `(EventId, OccurredAt, VehicleId, ListingId, Slug)`'dır;
  DURUM ALANI YOKTUR.** Durumu olayın **tipi** söyler. Tutar, fiyat ve şube adı girmez.
  *Gerekçe:* adres şart — yayından kaldırıldıktan sonra slug'ı okumanın yolu yok; bugün
  yalnız okuma modelinde ve o satır siliniyor (`ShowcaseListingProjector.cs:17`). Tüketici
  bugünden belli: `0004` R6 sitemap'i `VehiclePublished`/`VehicleUnpublished` ile
  güncelliyor. Ayrı bir "durum" alanı, tipiyle çelişebilen ikinci bir gerçek üretir; para
  alanı ise outbox satırında kalıcılaşır (`LeadCaptured`'ın L6 gerekçesi). *Bedel:* R11'in
  metni değişti; `VehiclePriceChanged`'ın tüketicisi fiyatı kendisi okumak zorunda.
- **D31 — Yeniden yayında TARİH DE DONAR ve duyuru YİNE yayılır; `VehiclePublished` ilk
  yayın zamanını taşır.** *Gerekçe:* `PublishedAt ??= clock.UtcNow` (`Listing.cs:137`) zaten
  ilk yayında donuyor ve vitrin sıralaması ona bakıyor (`ShowcaseListingQueries.cs:38-40`);
  R16'nın donmuş adresiyle tutarlı olan şey donmuş sıradır. Duyuruyu yaymamak ise tüketiciyi
  kör bırakır; `OccurredAt > FirstPublishedAt` karşılaştırması *"bu ilk yayın değil"*
  bilgisini bayrak eklemeden verir. *Bedel:* bir ay yayından kalkmış ilan geri döndüğünde
  vitrinde "yeni" görünmez ve `0018`'in "son gönderimden beri yayına alınanlar" penceresi
  **yeniden yayını da sayar** — o spec'in S3'üne bu not düşüldü.
- **D32 — A8 `Rezerve` ilanları da kapsar ve `docs/domain.md` A3'e `Rezerve → Taslak`
  geçişi eklendi.** Bayrak vitrine dönebilecek kayıtlara (`Taslak|Yayında|Rezerve`) yazılır;
  `Satıldı` ve `Arşiv` kayıtlara dokunulmaz. *Gerekçe:* `Unpublish()` yalnız `Yayında`dan
  geçiyor (`Listing.cs:179-183`) ve A3'te bu geçiş yoktu; kapalı şubedeki rezerve ilan
  vitrinden düşüyor ama `Rezerve` kalıyor, tek çıkışı `Satıldı` oluyordu — A8'in
  *"sessizce vitrinde kalmazlar"* amacının tam tersi, çıkışı olmayan bir hâl. *Elenen
  alternatif:* işleyicinin `ReturnToShowcase()` + `Unpublish()` zinciri — tek nedenden
  **iki** duyuru üretir ve § 2.C sıra garantisi vermediği için tüketici diziyi ters sırada
  görebilir. *Bedel:* **sözlükte ikinci bir kural değişikliği** (D10'dan sonra ikinci) ve
  rezervasyonun düştüğü bilgisi hiçbir yere yazılmaz — ama rezervasyon kaydı X1'de kapsam
  dışı.
- **D33 — A8 yolu D4'ün seçim sınırından MUAFTIR; şube olayı başına tek işlemde koşar ve
  geçişleri DUYURULUR.** *Gerekçe:* kısmi başarı bir **personel** kararının karşılığıdır
  (D3); sistemin kendi tepkisinde yarım uygulanmış bir şube kapanışı A8'i deler. Sınırın
  yazılmaması, D4'ü buraya da uygulayan bir okumaya kapı açıyordu. *Bedel, açıkça:* 300
  araçlı bir şube tek işlemde 300 geçiş, 300 bayrak ve 300 outbox satırı demektir; uzun
  transaction kabul edildi ve idempotentlik olay kimliğine bağlı kalır.
- **D34 — C12 GÖRÜNÜRLÜK KARARINA daraltıldı.** Hangi ilanın vitrinde satırı olduğunu yalnız
  yansıtma belirler; kopyalanmış kolonları besleyen işleyiciler (kapak URL'i, şube adları)
  bunun dışındadır. `BranchDeactivatedHandler` ise görünürlük kararı **verdiği** için
  istisna olmaktan çıkar: D32'nin yazma yolunu çağırır, satır yansıtmanın `null` sonucuyla
  düşer. *Gerekçe:* ölçüldü — bugün üç işleyici okuma modeline doğrudan yazıyor
  (`MediaAssetReadyHandler:63`, `BranchEventHandlers:30` ve `:52`) ve üçü de
  `0001`/`0003`/`0011`'in kriterleriyle korunuyor. C12'nin eski yazımıyla geliştiricinin iki
  seçeneği vardı: çalışan üç işleyiciyi yeniden yazmak ya da kısıtı sessizce ihlal etmek.
  Kısıtın koruduğu şey Yasak 41'dir (gizlemenin uca kaçması), kopyalanan kolonlar değil.
  *Bedel:* kısıt zayıflar; "hangi yazma görünürlük kararıdır" ayrımı kriterle sabitlenmek
  zorunda.
- **D35 — Arşiv koşulu listeye, TOPLAM SAYIYA ve ÖZET SAYAÇLARINA birlikte uygulanır.**
  Filtre eklemeli ve iki değerli ("arşivi dâhil et"); tanınmayan değer **yoksayılır**, hata
  dönmez (`pageSize` emsali, `docs/conventions.md` § 3). *Gerekçe:* `ListAsync` (`:39`) ve
  `GetSummaryAsync` (`:71`) bugün koşulsuz sayıyor; yalnız listeye koşul eklenirse panel
  "12 araç" yazarken 10 satır gösterir. *Bedel:* kapanmış `0010b`'nin iki kriterinin
  (AC-23 ve AC-26 komşusu) anlamı değişir ve bu PR'da **adıyla** yazılmak zorunda — D11'in
  emsali.
- **D36 — Öne çıkarma: toplu KALDIRMA da listede, ön koşul "arşiv değil", zaman damgasını
  ilerletir, geliştirme seed'i hizalanır.** *Gerekçe:* R12 iki eylem veriyor, D18'in toplu
  listesi yalnız birini sayıyordu — yirmi ilanı öne çıkarıp tek tek geri almak D3'ün çözdüğü
  sorunun aynısı. Durum ön koşulu koymak yeni bir red kodu ve yeni bir test demek,
  karşılığında kazanç yok; taslakta işaretlemenin etkisi yayınla görünür ve panel bunu
  söyler. Seed bugün aynı kolona `1..N` yazıyor (`ShowcaseSeeder.cs:139`) ve D16'nın iki
  değerli anahtar kararını **veri tarafında** deliyor. *Bedel:* etkisi hemen görünmeyen bir
  düğme doğar; geliştirme seed'ine dokunulur.
- **D37 — `UpdateVehicleCommand`'ın eksik `Touch` çağrısı bu spec'te tamamlanır** ve PR'da
  adıyla yazılır. *Gerekçe:* ölçüldü — `Touch`'ın çağıranları yalnız
  `UpdateVehicleMediaCommand.cs:82` ve `ChangePrice`; başlık, kilometre veya renk
  düzeltmesi "son güncellenen" sıralamasında görünmüyor. D20 geçişi görünür kılıyor;
  düzenleme görünmez kalırsa sıralama *"fiyatı ve medyası değişenler"* eksenine dönüşür ve
  tutarsızlığı D20'nin kendisi **yeni** fark edilir hâle getirir. *Bedel:* kapanmış
  `0010b`'nin komutu, o spec'in istemediği bir davranış kazanır — kapsam kararı Takım
  Yöneticisi'nin ve verildi.
- **D38 — ADR `0008` / K3'e donma anı cümlesi eklendi:** R/C ile AC ayrı PR'larda `main`'e
  giriyorsa donma anı **AC'yi taşıyan PR**'dır; ondan önceki R/C değişimi revizyon
  sayılmaz. *Gerekçe:* `0012a`'nın kapanışı bu boşluğu ölçtü — sıkı okuma **1**, gevşek
  okuma **0** veriyordu ve karta gerekçesiyle **1** yazıldı. `0012b` aynı dizilimle
  ilerliyor (R/C #54'te girdi, AC kendi turunda girecek); aynı kuralı iki kart iki farklı
  okumayla doldurursa ölçüm karşılaştırılamaz hâle gelir ve K3'ün hedefi *"yorum payı yok"*
  idi. *Bedel:* bir ADR revizyonu; karar `0012a`'nın kartına **geriye dönük uygulanmaz**
  (kart arşivde ve sayısı gerekçesiyle yazılı — K3'ün kendi "kapanmış kartlar düzeltilmez"
  kuralı).

#### Kriter turu — D39–D42 (2026-10-11)

Takım Yöneticisi, aynı gün, ikinci CLARIFY turunun (`3c72936`) **üzerine**. Dördü de
ölçülerek çıktı.

- **D39 — İş ikiye bölündü: `0012b` (bir ilan) + `0012c` (liste).** *Ölçüldü:* ikinci
  CLARIFY turunun kapsamı ~58–62 kriter üretiyordu; `0012a` 33, `0011` 47 kriterdi.
  *Gerekçe:* Intent'in başarı ölçütü — *"vitrindeki araçlar artık seed'den değil galerinin
  kendi kararından geliyor"* — **tekil eylemlerle** karşılanıyor; toplu eylem, çalışan bir
  yayın akışının üzerine gelen liste düzeyinde bir hızlandırmadır. Bölme sınırı Intent'in
  kendi iki cümlesinde yazılıydı: *"bir araç üzerinde değil liste üzerinde"*. *Bedel:*
  `0012` üçüncü kez bölündü ve toplu eylem bir dilim daha gecikti; **D3**–**D7**, **D16**,
  **D18**, **D27**–**D29**, **D36** burada verildi ama `0012c`'de tüketilecek — karar ile
  tüketim arasındaki mesafe iki kartı birden okumayı zorunlu kılıyor.
  **Sınır bir madde kaydırıldı:** `R17`/**D19**/**D35** (arşiv varsayılan listede, toplam
  sayıda ve özet sayaçlarında yok) `0012b`'de **kaldı**. *Gerekçe:* arşiv **filtresi** arşiv
  **eyleminin** sonucudur, liste eyleminin değil; eylemi buraya koyup sonucunu öteki karta
  bırakmak, arşivlenen aracın listede durduğu bir ara hâl üretirdi. *Bedel:* `0012b` üç
  kriter daha taşıyor.
- **D40 — `Dealership.Contracts` EKLEMELİ bir okuma kazanır:** şubenin **locale → ad**
  haritası. *Ölçüldü:* `IBranchQueries.FindAsync(branchId, locale)` tek bir `Name` döndürüyor
  (`Dealership.Contracts/Events/BranchEvents.cs:47`); vitrin okuma modelinin `BranchNames`
  kolonu ise locale → ad **sözlüğü** ve `BranchDetailsChangedHandler` ona olayın **bütün
  dillerini** yazıyor (`BranchEventHandlers.cs:28-33`). Yayın anında tek dille yazarsak satır
  tek anahtarlı kalır ve araç, bir sonraki `BranchDetailsChanged`'e kadar diğer iki dilde
  **şubesiz** görünür. *Gerekçe:* `0012a` D5/D10 aynı şeyi `Media.Contracts`'a yaptı; emsal
  kurulu. *Bedel:* spec'in `Modül(ler)` satırı `Dealership`'i kapsamak zorunda ve kapanmış
  bir spec'in sözleşmesine dokunuluyor. *Elenen alternatif:* etkin dil sayısı kadar
  `FindAsync` çağrısı — aynı veriyi üç kez okur.
- **D41 — `0012a`'nın medya komutu da yansıtmayı çağırır.** *Ölçüldü:* kapak
  `Vehicle.CoverAssetId` = sıralı listenin **ilk** elemanı (`Vehicle.cs:132`) ve yansıtma onu
  yazıyor; `UpdateVehicleMediaCommand` o listeyi değiştiriyor. R15'in saydığı yazma yolları
  arasında yoktu. *Gerekçe:* R15 *"onu değiştiren her yazma yolunda"* diyor; medya sırasını
  değiştirmek vitrindeki kapağı değiştirir. Dışarıda bırakmak, fotoğrafı yeniden sıralayan
  personelin vitrinde **eski kapağı** görmesi demek — sessiz ve bildirilmeyen bir
  tutarsızlık. *Bedel:* yeni kapanmış bir spec'in komutuna dokunuluyor ve o komut iki
  modülden senkron okuma yapmaya başlıyor.
- **D42 — A2'nin FİYAT koşulu da yayın anında TEMSİL EDİLEMEZ; **D23**'ün ölçümü ona da
  uygulanır.** *Ölçüldü:* `ListingPrice`'ın tek kurucusu `private` ve yalnız iki üretici var
  — `OnRequest()` ve `Of(money)` (`ListingPrice.cs:22-24`). Kalıcılık yolu da aynı kapalı
  küme: dönüştürücü `Amount is null` hâlini `onrequest` olarak **yazıyor** ve geri okurken
  yalnız o iki değeri **üretiyor** (`ValueConverters.cs:90-110`). Yazma yolu ayrıca
  `BuildPrice` ile dayatılıyor (`VehicleWriteRules.cs:131-150`). Yani "fiyatı belirlenmemiş
  ve fiyat sorunuz da olmayan araç" **kurulamıyor, kaydedilemiyor ve okunamıyor**.
  *Gerekçe:* D23 çekirdek alan koşulu için *"o kod yazılmaz"* dedi; aynı ölçüm fiyat için de
  geçerli ve iki koşulu farklı sayan bir R2 yanlış olurdu. Temsil edilemeyen bir koşulu
  yayın anında denetlemek **vakum bir kontrol** üretir — hiçbir zaman kırmızıya dönmeyen bir
  dal, olmayan bir kapıdır. *Bedel, açıkça:* **bu bir R2 revizyonudur** ve SCORECARD'da
  sayılır; R2'nin metni A2'den dar görünüyor ve farkı kapatan şey bir **tripwire** testidir
  — `ListingPrice` "fiyatsız" bir hâl üretebilir hâle gelirse test kırmızıya döner ve kapı o
  gün yazılır.

### Kapanan sorular

`0012`'nin beş açık sorusundan dördü ilk turda, beşincisi ikinci turda kapandı. **Açık soru
kalmadı.**

| Eski soru | Durum | Kapatan karar |
|---|---|---|
| **S1** — toplu eylemde kısmi başarı nasıl davranır | ✅ kapandı | **D3**–**D7**: önerisi aynen kabul edildi ve sınırları tanımlandı |
| **S2** — mockup'taki "Vitrine ekle" ne demek | ✅ kapandı | **D16**: ayrı bir eylem değil, **öne çıkarma**; etiketi "Öne çıkar" |
| **S3** — `0011` önkoşul mu, medya şartı gevşetilsin mi | ✅ kapandı | önkoşul; `0011` 2026-10-11'de tamamlandı (PR #51) ve gevşetme yapılmadı. Şartın ölçülebilir hâle gelmesi `0012a` **D5/D6** |
| **S5** — durum geçişi duyuruları hangi olaylardır | ✅ kapandı | **D11**: sekiz olay, `Vehicle*` önekli, § 1 iki adla güncellendi |
| **S4** → **S1** — eylemler rol ayrımı yapacak mı | ✅ kapandı | **D22**: rol ayrımı yok; `0012a` X2 ile aynı karar |

---

## Açık sorular

**Yok.** Son açık soru (**S1** — eylemler rol ayrımı yapacak mı) 2026-10-11'de **D22** ile
kapandı: rol ayrımı yok, yetki tek bir şeydir — oturum. Mockup'ın *"Personel rolündesiniz:
ilan taslak olarak kaydedilir, fiyat alanı kilitlidir"* cümlesinin karşılığı bu spec'te
**yoktur** ve X4 onu kapsam dışında tutuyor.

Kabul kriterlerini bekleten bir karar kalmadı; sıradaki tur **kriter turudur**.

---

## Acceptance Criteria

Elli sekiz kriter, on iki grupta. **D22** (rol ayrımı yok), **D39** (bölme) ve **D42**
(fiyat koşulu temsil edilemez) kapsamı belirledi; toplu eylem ve öne çıkarma `0012c`'dedir.
Yapılandırmadan gelen her değer için kriter **yapılandırılan değeri** doğrular, spec'e
kopyalanmış bir sabiti değil.

### A. Yayına alma ve red

- **AC-1.** Oturum açmış personel, koşulları sağlanan bir taslak ilanı yayına alır ve ilan `Yayında` durumuna geçer.
- **AC-2.** Hazır medya varlığı olmayan araç yayına alınamaz; yanıt `vehicle.cannot_publish.no_media` taşır.
- **AC-3.** Zorunlu işaretli bir opsiyonel alanı boş olan araç yayına alınamaz; yanıt `vehicle.cannot_publish.required_field` ve **eksik alanların anahtarlarını** taşır.
- **AC-4.** Birden çok koşul sağlanmıyorsa yanıt eksiklerin **hepsini birlikte** taşır; ilk eksikte durmaz.
- **AC-5.** Yayın koşulu yalnız `Hazır` medya varlığını sayar; `işleniyor` ve `başarısız` varlıklar koşulu karşılamaz.
- **AC-6.** Lokasyon ataması bekleyen araç yayına alınamaz; yanıt `vehicle.cannot_publish.branch_unassigned` taşır.
- **AC-7.** Adresi üretilemeyen ilan yayına alınamaz; yanıt `vehicle.cannot_publish.slug_unavailable` taşır.
- **AC-8.** Yayın reddi `422`, geçiş reddi `409`, bulunamayan araç `404` döner.
- **AC-9.** Reddedilen yayın isteği çevrilmiş metin değil **kod listesi** döner.
- **AC-10.** A2'nin çekirdek alan ve fiyat koşulları yayın anında **temsil edilemez**; temsil edilebilir hâle gelirlerse kapı kırmızıya döner.

### B. Geçişler

- **AC-11.** `Yayında` bir ilan rezerve edilir ve `Rezerve` durumuna geçer.
- **AC-12.** `Rezerve` bir ilanın rezervasyonu kaldırılır ve ilan `Yayında` durumuna döner.
- **AC-13.** `Yayında` ve `Rezerve` bir ilan satıldı olarak işaretlenir.
- **AC-14.** `Satıldı` terminaldir: araç yeniden yayına alınamaz.
- **AC-15.** `Yayında` bir ilan yayından kaldırılır ve `Taslak` durumuna döner.
- **AC-16.** `Yayında` ve `Satıldı` bir ilan arşivlenir.
- **AC-17.** `Taslak` bir ilan arşivlenir.
- **AC-18.** `Rezerve` bir ilan yayından kaldırılır ve `Taslak` durumuna döner.
- **AC-19.** A3'te yazılı olmayan her geçiş `listing.invalid_status_transition` ile reddedilir.
- **AC-20.** Durum geçişi aracın son güncellenme zamanını ilerletir.
- **AC-21.** Araç düzenlemesi de aracın son güncellenme zamanını ilerletir.
- **AC-22.** Oturumu olmayan biri hiçbir durum geçişini çağıramaz.

### C. Arşiv ve şase tekilliği

- **AC-23.** Arşivleme aracın arşiv bayrağını **aynı işlemde** yazar.
- **AC-24.** Arşivlenen aracın şasesiyle yeni bir araç kaydedilebilir.
- **AC-25.** Arşivlenmemiş bir aracın şasesi yeniden kaydedilemez.

### D. Şube kapanışının yazma tarafı

- **AC-26.** Şube kapatıldığında o şubedeki `Yayında` ilanlar yayından kalkar ve aracın "lokasyon ataması bekliyor" bayrağı **aynı işlemde** yazılır.
- **AC-27.** Aynı yol `Rezerve` ilanları da kapsar.
- **AC-28.** `Satıldı` ve `Arşiv` kayıtlara dokunulmaz.
- **AC-29.** Bayraklı araç yayına alınamaz.
- **AC-30.** Şube atandığında bayrak temizlenir ve araç yeniden yayına alınabilir.

### E. Eşzamanlılık

- **AC-31.** Aynı ilana eşzamanlı gelen iki durum geçişinden yalnız biri başarılı olur.
- **AC-32.** Kaybeden **kural gereği** reddedilir ve `listing.invalid_status_transition` alır; `concurrency.lost` yalnız kilit zaman aşımında döner.

### F. Duyurular

- **AC-33.** Her durum geçişi bir duyuru üretir.
- **AC-34.** Üretilen olay adları `docs/architecture.md` § 1'deki sekiz adla **birebir** aynıdır.
- **AC-35.** Olay gövdesi beş alan taşır ve **yeni durumu ayrı bir alan olarak taşımaz**.
- **AC-36.** Duyuru iş verisiyle **aynı transaction'da** outbox'a yazılır; yazma başarısız olursa duyuru da yazılmaz.
- **AC-37.** Outbox yazıcısı **konkret** `OutboxWriter<VehicleCatalogDbContext>` tipiyle kaydedilir.
- **AC-38.** Fiyat ve şube değişikliği araç düzenleme komutundan duyurulur.
- **AC-39.** Şube kapanışı yolundaki geçişler de duyurulur.
- **AC-40.** Yeniden yayınlanan ilan yine duyurulur ve duyuru **ilk yayın zamanını** taşır.

### G. Vitrin yansıtması

- **AC-41.** Yayına alma vitrin okuma modeline satır yazar ve ilan ziyaretçiye görünür olur.
- **AC-42.** Görünmez bir duruma geçen ilanın vitrin satırı silinir.
- **AC-43.** Araç düzenlemesi vitrin satırını **aynı işlemde** günceller.
- **AC-44.** Aracın medya sırası değiştiğinde vitrin satırının kapağı güncellenir.
- **AC-45.** Görünürlük kararını yalnız yansıtma verir; kopyalanmış kolonları besleyen olay işleyicileri bu kararı vermez.

### H. İlanın adresi ve yeniden yayın

- **AC-46.** Adres **ilk yayına alma** anında üretilir ve yazma modelinde saklanır.
- **AC-47.** Yayından kaldırılıp yeniden yayınlanan ilan **aynı adrese** döner.
- **AC-48.** Yeniden yayında ilk yayın zamanı **donar**; ilan vitrinde yerini değiştirmez.
- **AC-49.** Hiç yayınlanmamış bir ilanın adresi yoktur.

### I. Panel listesi

- **AC-50.** Arşivlenen araç panel listesinin varsayılan görünümünde yer almaz.
- **AC-51.** Açık bir filtreyle arşivlenen araçlar görülebilir.
- **AC-52.** Arşiv koşulu listenin **toplam sayısına ve özet sayaçlarına** da uygulanır.
- **AC-53.** Lokasyon ataması bekleyen araç panel listesinde ayırt edilir.

### J. Ekran durumları

- **AC-54.** Eylem ekranı yükleniyor, boş, başarılı ve hata durumlarını ele alır.
- **AC-55.** Ekranda hiçbir teknik detay görünmez: durum kodu, `errorCode`, `traceId`, yol.

### K. Çok dil ve RTL

- **AC-56.** Ekranın metinleri TR / AR / EN üçünde de vardır; eksik çeviri anahtarı yoktur.
- **AC-57.** Arapça'da ekran RTL yönünde çalışır.

### L. Sınır

- **AC-58.** Durum geçişi mantığı agregatta kalır; komut ve uç katmanı durum ataması yapmaz.

---

## Definition of Done

<!-- MADDE ÇIKARILAMAZ. Şablondaki her maddenin bir `<!-- DoD:Tn -->` işaretleyicisi vardır
     ve mimari test (ADR 0008 / K8) sayıyı şablondan okuyarak denetler. -->

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi
      (**D2–D21** ilk CLARIFY turu, **D22–D38** ikinci tur; açık soru kalmadı).
- [x] `0012a` tamamlandı (A2'nin medya koşulu ölçülebilir).
      *2026-10-11, `8fc862c` (#56) ve kapanışı `18e4adf` (#57); kart `specs/done/` altında.
      Ölçüldü: `ReadyAmongAsync` (`MediaAssetContracts.cs:72`) ve parametreli
      `EnsurePublishable` (`Vehicle.cs:353`) duruyor — ikisinin de üretim çağıranı bu spec'in
      işi (`0012a` D18).*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012b-ac<n>.png`),
      **boş** ve **hata** durumları dâhil, RTL için Arapça görüntü de var. (Kısmi başarı
      `0012c`'nin kanıtıdır — **D39**.)
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil; durum geçişi mantığı agregatta kaldı (C2).
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `vehicle_catalog` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.
- [ ] A2'nin **dört** koşulunun hepsinin kontrol edildiği ve eksiklerin **birlikte** döndüğü
      testle gösterildi (Tuzak 1, Tuzak 2).
- [ ] **Üretilen her olayın** adı `docs/architecture.md` § 1'deki adıyla birebir aynı (D11).
- [ ] Outbox yazıcısının **konkret** tiple kaydedildiği testle gösterildi (C11, Tuzak 8).
- [ ] Arşivlemenin aracın bayrağını **aynı işlemde** yazdığı ve aynı şasenin yeniden
      kaydedilebildiği testle gösterildi (D9).
- [ ] Vitrin satırının her yazma yolunda güncellendiği — **araç düzenlemesi ve medya sırası
      dâhil** (**D41**) — testle
      gösterildi (D12).
- [ ] A2'nin **iki** koşulunun reddedilebildiği; çekirdek alan **ve fiyat** koşulları için
      **kod yazılmadığı** ve ikisinin de temsil edilemez olduğu **tripwire** ile gösterildi
      (**D23**, **D42**).
- [ ] A2 dışındaki **iki** reddin (lokasyon bayrağı, adres üretilemedi) kendi kodunu
      döndürdüğü ve reddin HTTP karşılığının C13'teki eşlemeye uyduğu testle gösterildi
      (D25, D26).
- [ ] Toplu eylem bu dilimde **yok**; maddesi `0012c`'ye taşındı (**D39**).
- [ ] Sekiz olayın gövdesinin **durum alanı taşımadığı** ve adresi taşıdığı, yeniden yayının
      da duyurulduğu ve ilk yayın zamanını taşıdığı testle gösterildi (D30, D31).
- [ ] A8'in `Rezerve` ilanı da yayından kaldırdığı, `Satıldı`/`Arşiv` kayda dokunmadığı ve
      geçişlerini duyurduğu testle gösterildi (D32, D33).
- [ ] Görünürlük kararının yalnız yansıtmada olduğu, kopya kolon yazıcılarının (kapak URL'i,
      şube adları) **bozulmadığı** testle gösterildi (D34).
- [ ] Arşiv koşulunun liste, **toplam sayı ve özet sayaçlarında** birlikte uygulandığı
      testle gösterildi; `0010b`'nin etkilenen kriterleri PR'da **adıyla** yazıldı (D35).
- [ ] `UpdateVehicleCommand`'ın eksik `Touch` çağrısının tamamlandığı testle gösterildi ve
      PR'da **adıyla** yazıldı (**D37**). Öne çıkarma `0012c`'dedir (**D39**).
- [ ] `Dealership.Contracts`'a eklenen okumanın **eklemeli** olduğu ve mevcut imzaları
      kırmadığı testle gösterildi (**D40**).
      çağrısının tamamlandığı testle gösterildi; ikisi de PR'da adıyla yazıldı (D36, D37).

---

## SCORECARD

<!--
İş bittikten SONRA doldurulur. Amaç suçlu bulmak değil, sürecin nerede sızdırdığını görmek.
Dürüst doldurulmayan scorecard işe yaramaz — abartılmış iyi sayılar en pahalı yalandır.
-->

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | evet / hayır | R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır. |
| **Kriter sayısı: önce → bugün** | → | O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. |
| **Spec revizyon sayısı** | | Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3). Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? **`0` ile `—` aynı şey değildir.** **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı. Dış bulguyla aynı sütunda toplanmaz. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır.** Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak (ADR 0008 / K3). |

**Bu spec'ten çıkarılan ders:**
