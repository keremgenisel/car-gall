# 0012b — Yayın ve Durum Eylemleri

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (komut yüzeyi, outbox, vitrin yansıtması) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | **`0012a` ÖNKOŞUL** (aracın medya yüzeyi — A2'nin medya koşulunu ölçülebilir kılar) · `0010a` + `0010b` · `0011` (tamamlandı) · `0007` (invariant koruması — tamamlandı) |
| **Dayanak** | ADR 0003 Ek 1, sıra #5'in **ikinci dilimi** (→ `0012a` **D1**) |
| **Karar bekliyor** | **S1** — eylemler rol ayrımı yapacak mı (§ Açık sorular, öneri yazılı). Cevap gelmeden kabul kriterleri yazılmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı. `0012`'nin beş açık sorusunun
> **dördü** 2026-10-11 CLARIFY turunda kapandı (§ Kapanan sorular); geriye **S1** kaldı ve
> yirmi bir karar Context'e işlendi.

---

## Intent

`0010a`/`0010b` bittiğinde galeri araç kaydı oluşturabiliyor, `0012a` bittiğinde
fotoğraflarını sıralayabiliyor — ama **hiçbirini vitrine çıkaramıyor.** Vitrin geliştirme
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

- **R1.** Oturum açmış yetkili personel bir taslak ilanı **yayına alabilir**.
- **R2.** Bir ilan yayına alınırken **A2**'nin tüm koşulları sağlanmış olmalıdır: çekirdek
  alanlar dolu, fiyat belirlenmiş **veya** "fiyat sorunuz", en az bir medya varlığı **hazır**,
  aktif alan ayarlarında zorunlu işaretli opsiyonel alanlar dolu. Koşul sağlanmıyorsa yayına
  alma reddedilir ve **sağlanmayan koşulların HEPSİ birlikte** bildirilir — ilk eksik değil.
  → **D2**
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
- **R9.** Personel **birden çok ilanı seçip** aynı eylemi hepsine uygulayabilir. Toplu
  uygulanabilen eylemler: yayına al · yayından kaldır · satıldı işaretle · arşivle · öne
  çıkar. Rezerve etme ve rezervasyonu kaldırma **tek ilan** üzerinde çalışır. → **D18**
- **R10.** Toplu eylemde bazı ilanlar için eylem geçersizse, **geçerli olanlar uygulanır** ve
  uygulanamayanlar **tek tek gerekçesiyle** bildirilir; işlem bir bütün olarak geri alınmaz.
  Seçim boş olamaz, yinelenen kimlikler teklenir ve seçim sayısı panelin sayfa üst sınırını
  aşamaz. → **D3**, **D4**, **D5**, **D6**
- **R11.** Her durum geçişi **duyurulur**; duyuru ilanın kimliğini ve yeni durumunu taşır.
  Üretilen olaylar **sekizdir** ve adları `docs/architecture.md` § 1'deki adlarla birebir
  aynıdır. → **D11**
- **R12.** Personel bir ilanı **öne çıkarabilir** ve öne çıkarmayı kaldırabilir; öne
  çıkarılan ilan vitrin sıralamasının başında görünür. → **D16**
- **R13.** Şube kapatıldığında o şubedeki araçlar **yayından kalkar** ve "lokasyon ataması
  bekliyor" hâline geçer; bu araçlar panel listesinde **ayırt edilir** ve şubesi atanmadan
  yayına alınamaz. → **A8**, **D15**
- **R14.** Yayına alma, rezerve etme ve satıldı işaretleme eylemleri **eşzamanlı**
  çağrıldığında tek biri başarılı olur; kaybeden **kural gereği** reddedilir, "tekrar dene"
  denmez. → `0007`, **D8**
- **R15.** Bir ilanın vitrindeki görünürlüğü, onu değiştiren her yazma yolunda **aynı
  işlemde** güncellenir — yayına alma, her durum geçişi, öne çıkarma ve araç düzenlemesi
  dâhil. → **D12**
- **R16.** Bir ilanın vitrindeki **adresi ilk yayına alma anında** üretilir ve bir daha
  değişmez; yayından kaldırılıp yeniden yayınlanan ilan **aynı adrese** döner. → **D13**
- **R17.** Arşivlenen araç panel listesinin **varsayılan** görünümünde yer almaz; açık bir
  filtreyle görülebilir. → **D19**
- **R18.** Durum geçişi aracın **son güncellenme** zamanını da ilerletir; panel listesinin
  "son güncellenen" sıralaması geçişi görür. → **D20**
- **R19.** Panel listesi ve eylem ekranları **yükleniyor**, **boş**, **başarılı**, **kısmi
  başarı** ve **hata** durumlarını ele alır. → `docs/frontend.md` § 3, **D7**
- **R20.** Panel personelin tercih ettiği dilde çalışır; Arapça'da RTL.

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
  eksik" bilgisi de kod olarak döner, metin olarak değil. → Yasak Listesi 23–24
- **C7.** Mevcut agregat davranışı yeniden yazılmaz: `Listing.Publish`, `Reserve`,
  `ReturnToShowcase`, `MarkSold`, `Unpublish`, `Archive`, `ChangeShowcaseSortWeight` ve
  `Vehicle.MarkArchived`, `MarkAwaitingBranchAssignment`, `EnsurePublishable` bugün vardır.
  Bu spec onların üzerine komut katmanı kurar. **Adıyla yazılı iki istisnası vardır:**
  (a) araç düzenleme komutu vitrin yansıtmasını çağırır (**D12**) ve iki olay yayar
  (**D11**); (b) `Listing.Archive` taslaktan da geçer (**D10**).
- **C8.** Sayı içeren cümleler ICU ile kurulur ("3 ilan yayına alındı"). → Yasak Listesi 31
- **C9.** Yeni migration yalnızca `vehicle_catalog` şemasına dokunur. → Yasak Listesi 3
- **C10.** Her kabul kriteri en az bir testle karşılanır. → `AGENTS.md` Altın Kural 5
- **C11.** Outbox yazıcısı **konkret** tipiyle kaydedilir:
  `OutboxWriter<VehicleCatalogDbContext>`. Paylaşılan bir soyutlama altında kayıt yasaktır ve
  `Architecture.Tests/OutboxYazicisiTests` bunu denetliyor — üçüncü iddiası
  `IOutboxDbContext` uygulayan **her** modülü taradığı için `VehicleCatalog` outbox'ı
  kurduğu anda kapıya dâhil olur. → **D14**, Yasak Listesi 16
- **C12.** Vitrin okuma modeline yazan tek yer **yansıtmadır**; görünürlük kararı başka hiçbir
  yerde tekrarlanmaz. → Yasak Listesi 41, `0001` C10, **D12**

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
  alan/kayıt düzeyi yetkiyi ayrı bir karara bağladı. **Karar S1'de bekliyor**; `hayır`
  gelirse bu madde geçerli kalır, `evet` gelirse R1'in "yetkili" sözü tanımlanır.
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

---

## Context

### Mevcut davranış — doğrulandı (2026-10-11, `origin/main` = `b99912a`)

- **Agregat tarafı hazır.** `Listing` bugün `CreateDraft`, `Publish`, `Reserve`,
  `ReturnToShowcase`, `MarkSold`, `Unpublish`, `Archive`, `ChangeShowcaseSortWeight`
  taşıyor; `Vehicle` ise `EnsurePublishable`, `AttachMediaAsset`,
  `MarkAwaitingBranchAssignment`, `MarkArchived`, `AssignBranch`. **Eksik olan komut ve uç
  katmanıdır** — C7 bu yüzden var.
- **Hiçbirinin üretim çağıranı yok.** Ölçüldü: `Publish`/`Reserve`'ün tek üretim çağıranı
  geliştirme seed'i (`ShowcaseSeeder.cs:148,155`); `MarkSold`, `Unpublish`,
  `ReturnToShowcase`, `Archive`, `MarkArchived`, `MarkAwaitingBranchAssignment` yalnız
  testlerden çağrılıyor; **`ChangeShowcaseSortWeight`'in hiçbir çağıranı yok** — üretimde de
  testte de sıfır.
- **`VehicleCatalog`'un hiçbir yazma UCU yok.** Projedeki tüm `MapPost`/`MapPut` uçları
  `Identity`, `Leads` ve `Media`'ya ait; `VehicleAdminEndpoints` araç kaydı/düzenlemesi
  dışında eylem ucu taşımıyor.
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
  koşulu olmadan okuyor (`:42-44`) ve `VehicleAdminListFilter`'da durum alanı yok. → **D19**

### Geçerli domain kuralları

**A2** (yayın koşulları) · **A3** (geçiş tablosu — `Taslak → Arşiv` **D10** ile eklendi) ·
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

### Verilen kararlar

Hepsi 2026-10-11 CLARIFY turunda Takım Yöneticisi onayıyla verildi. **D1**, `0012a`'da
yazılı bölme kararıdır ve burada tekrarlanmaz.

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

### Kapanan sorular

`0012`'nin beş açık sorusundan dördü, onaylanan kararların doğal sonucu olarak kapandı.
**Yorumum buysa ve aksini söylerseniz geri alınır:**

| Eski soru | Durum | Kapatan karar |
|---|---|---|
| **S1** — toplu eylemde kısmi başarı nasıl davranır | ✅ kapandı | **D3**–**D7**: önerisi aynen kabul edildi ve sınırları tanımlandı |
| **S2** — mockup'taki "Vitrine ekle" ne demek | ✅ kapandı | **D16**: ayrı bir eylem değil, **öne çıkarma**; etiketi "Öne çıkar" |
| **S3** — `0011` önkoşul mu, medya şartı gevşetilsin mi | ✅ kapandı | önkoşul; `0011` 2026-10-11'de tamamlandı (PR #51) ve gevşetme yapılmadı. Şartın ölçülebilir hâle gelmesi `0012a` **D5/D6** |
| **S5** — durum geçişi duyuruları hangi olaylardır | ✅ kapandı | **D11**: sekiz olay, `Vehicle*` önekli, § 1 iki adla güncellendi |
| **S4** → **S1** | ⬜ **açık** | aşağıda |

---

## Açık sorular

**S1. Eylemler rol ayrımı yapacak mı?** *(eski S4; metni korunuyor)*
Mockup "Personel rolündesiniz: ilan taslak olarak kaydedilir, fiyat alanı kilitlidir" diyor.
*Öneri:* bu spec'te **rol ayrımı yok** (X4); tüm eylemler oturum açmış personele açık.
*Gerekçe:* ADR 0003 unsur 3 alan/kayıt düzeyi yetkiyi ayrı bir karara bağladı ve § 6 bunu
"basit rol modelini izin motoruna dönüştürür" diye uyardı.
*Bedel:* satış danışmanı da yayına alabilir ve fiyat değiştirebilir. Küçük ekipte kabul
edilebilir; değilse bu kararı önce vermek gerekir — **R1'in "yetkili" sözünün tanımı buna
bağlı** ve kabul kriterleri bu cevap gelmeden yazılmaz.

---

## Acceptance Criteria

> **S1 cevaplandıktan sonra yazılacak.** En az şu kümeleri kapsayacak:
>
> A2'nin dört koşulunun ayrı ayrı reddi **ve birlikte reddi** (D2) · A3'teki her geçiş ve her
> **geçersiz** geçiş (`Taslak → Arşiv` dâhil) · A4 terminal kuralı · arşivlemenin şase
> muafiyetini açması (D9) · toplu eylem: kısmi başarı, boş seçim, yinelenen kimlik, üst sınır
> (D3–D6) · A8'in yazma tarafı ve bayraklı aracın yayına alınamaması (D15) ·
> eşzamanlılıkta kaybedenin **kural reddi** alması (D8) · sekiz olayın üretimi, adları ve
> idempotentlik (D11) · vitrin satırının her yazma yolunda güncellenmesi (D12) · adresin ilk
> yayında donması ve yeniden yayında **aynı** kalması (D13) · öne çıkarmanın sıralamaya
> yansıması (D16) · arşivin varsayılan listede görünmemesi (D19) · geçişin "son güncellenen"i
> ilerletmesi (D20) · beş ekran durumu (D7) · çok dil ve RTL.

---

## Definition of Done

<!-- MADDE ÇIKARILAMAZ. Şablondaki her maddenin bir `<!-- DoD:Tn -->` işaretleyicisi vardır
     ve mimari test (ADR 0008 / K8) sayıyı şablondan okuyarak denetler. -->

- [ ] **S1** karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] `0012a` tamamlandı (A2'nin medya koşulu ölçülebilir).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012b-ac<n>.png`),
      **kısmi başarı** ve hata durumları dâhil, RTL için Arapça görüntü de var.
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
- [ ] Vitrin satırının her yazma yolunda güncellendiği, **araç düzenlemesi dâhil**, testle
      gösterildi (D12).

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
