# 0011 — Media: Yükleme, Türev Üretimi ve Filigran

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Media` (yeni) · `VehicleCatalog` (yalnız **D2**'nin alıcı düzeltmesi) |
| **Bağımlı spec'ler** | `0010a` (araç kaydı — Onaylandı). `0012` bu spec'i **bekler**. |
| **Dayanak** | ADR 0003 Ek 1, sıra #4 |
| **Verilen kararlar** | **D1–D26** (2026-10-10). **D1–D12** CLARIFY turunun boşlukları; **D13–D16** açık soruların (S1, S2, S3, S5) cevabı; **D17–D26** uygulama planı turunda **ölçülerek** çıktı. Açık soru kalmadı. |

> **R/C/AC bu PR'da `main`'e giriyor ve orada donuyor** (ADR `0008` / K2, `docs/git.md` § 1).
> Bu commit'ten sonra Requirements, Constraints veya Acceptance Criteria'da yapılan her
> değişiklik **sayılan bir revizyondur** ve kendi commit'ine girer.
>
> **İki tur birikti.** 2026-10-10 **CLARIFY** turu on iki boşluk buldu (**D1–D12**) ve
> R12–R18, C5'in düzeltilmesi, C11–C14 ile X7–X8'i üretti — ama kabul kriterlerini
> **bilerek yazmadı**, çünkü dört soru (S1, S2, S3, S5) onların metnini belirliyordu. Aynı
> gün **uygulama planı** turu o dört soruyu karara bağladı (**D13–D16**), on karar daha
> ölçerek ekledi (**D17–D26**) ve kriterleri yazdı.
>
> **D1–D12 ve § Doğrulanmış bulgular CLARIFY turundan birebir korunuyor.** Hiçbir ölçüm
> yeniden yazılmadı.

---

## Intent

Galerinin elindeki araç fotoğrafı sisteme **hiçbir biçimde girmiyor.** Bunun görünür sonucu
şudur: `0010a` araç kaydını mümkün kılsa bile hiçbir araç yayına alınamaz, çünkü
`Vehicle.EnsurePublishable()` en az bir medya varlığı şartını koşuyor. Vitrin, fotoğrafı
olmayan ilanları gösteremez; `0001` kapak görselini kart alanı olarak zorunlu kıldı.

**Galeri çalışanı** aracın fotoğraflarını yükleyip sıralamak istiyor; **galeri sahibi**
fotoğrafların kendi logosuyla çıkmasını istiyor, çünkü vitrin fotoğrafı rakip sitelere
kopyalanıyor.

Bu iş bittiğinde bir görsel yüklenir, işlenir, farklı boyutlarda türevleri üretilir,
filigranı basılır ve hazır olduğu **duyurulur**. Başarı ölçütü: **bir aracın yayına
alınmasının önündeki medya engeli kalkar.**

---

## Requirements

- **R1.** Oturum açmış personel bir görsel **yükleyebilir**.
- **R2.** Yüklenen her görsel bir **varlık kaydı** olarak tutulur: kimliği, işleme durumu ve
  depolama yolu.
- **R3.** Her varlık için farklı boyutlarda **türevler** üretilir; vitrin ve panel bu
  türevleri kullanır, orijinali değil.
- **R4.** Türevlere **galerinin filigranı** basılır.
- **R5.** Görselin konum ve cihaz bilgisi (EXIF) **temizlenir**.
- **R6.** Varlığın bir **işleme durumu** vardır: yüklendi · işleniyor · hazır · başarısız.
- **R7.** Varlık **hazır** olduğunda sistem bunu **duyurur**; duyuru varlığın kimliğini ve
  erişim yolunu taşır.
- **R8.** İşleme **başarısız** olduğunda sistem bunu da duyurur; varlık sessizce yüklendi
  durumunda kalmaz.
- **R9.** Her varlık **locale başına `alt` metni** taşıyabilir.
- **R10.** **Medya sahipsizdir:** bir varlığın hangi araca ait olduğu bu modülde tutulmaz.
  → `docs/architecture.md` § 1.
- **R11.** Desteklenmeyen bir dosya türü veya boyut sınırını aşan dosya reddedilir; reddedilen
  dosya varlık kaydı oluşturmaz.
- **R12.** Kabul edilen tür kümesi **kapalıdır: JPEG, PNG, WebP.** Tür, dosya adından veya
  istemcinin `Content-Type` başlığından **değil, dosyanın içeriğinden** belirlenir. → **D4**
- **R13.** Reddetme sebebini durum kodu taşır: **desteklenmeyen tür `415`**, **boyut sınırı
  aşıldı `413`**. Gövde `errorCode` taşır (C7). → **D4**
- **R14.** Hazır duyurusu **türev kümesini** taşır: her türev için etiket, erişim yolu,
  **genişlik ve yükseklik**. Tek bir erişim yolu yeterli değildir. → **D3**
- **R15.** Başarısızlık duyurusu varlık kimliğini ve **kapalı bir kümeden** bir sebep kodu
  taşır. Serbest metin veya çevrilmiş metin taşımaz. → **D8**
- **R16.** İşleme durumunun geçişleri tanımlıdır ve `başarısız` **terminal değildir**: tekrar
  deneme **ayrı bir komutla** tetiklenir, otomatik değil, ve **aynı varlık kimliğini**
  korur. → **D6**
- **R17.** `Media`, bir varlık kümesinin **hazır olup olmadığını** yanıtlayan bir okuma
  yüzeyi sunar. → **D11**
- **R18.** Orijinal dosya **hiçbir zaman herkese açık sunulmaz**; erişimi yalnızca iç
  yoldandır. → **D9**

---

## Constraints

### Uyulması zorunlu

- **C1.** `Media`, varlık verisinin tek yazma sahibidir; hangi aracın hangi görsele sahip
  olduğu `VehicleCatalog`'dadır. → `docs/architecture.md` § 1.
- **C2.** Başka modülün dosya deposu yoluna doğrudan erişilmez ve `Media` başka modülün
  deposuna yazmaz. → Yasak Listesi 4.
- **C3.** Depolama **S3-uyumludur** (R2/MinIO) ve CDN ilk günden kullanılır.
  → `docs/architecture.md` § 7 (Karar Defteri: Medya).
- **C4.** Duyurular outbox ile, **en-az-bir-kez** ve idempotent gider. → § 7.
- **C5.** **`Media` hiçbir modüle senkron çağrı YAPMAZ** — bağımlılık okları ondan dışarı
  çıkmaz. Özellikle `Media → Dealership` yasaktır.
  → `docs/architecture.md` § 2.A, üçüncü koşul (**tek atlama**).

  *Bu kısıt 2026-10-10'da düzeltildi (**D1**).* Önceki hâli *"`Media` bir yaprak modüldür:
  hiçbir modül ona senkron bağımlı olmaz"* diyordu ve dayanak olarak § 1'i gösteriyordu —
  **o belge tersini yazıyor.** Yaprak listesi `Content · Notifications · Audit · Engagement`
  ve `Media` orada yok; § 1 `VehicleCatalog ──► … Media` okunu çiziyor, § 2.A
  *"`VehicleCatalog → Media` (görseller hazır mı, URL çöz)"* çağrısını sanksiyonlu
  örnek olarak sayıyor ve `VehicleCatalog.csproj` **bugün** `Media.Contracts`'a referans
  veriyor. Korunan özellik değişmedi, dayanağı düzeldi: `Media → Dealership`
  yasaktır çünkü `VehicleCatalog → Media → Dealership` **iki atlamalı senkron zincir**
  olurdu.
- **C6.** Sağlayıcı kimlik bilgileri (S3 anahtarı) repoda durmaz; yapılandırmadan gelir ve
  eksikse uygulama **ayağa kalkmaz**. → `0009` C8 deseni.
- **C7.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner.
  → `AGENTS.md` Altın Kural 6.
- **C8.** Zaman damgaları UTC'dir. → Yasak Listesi 30.
- **C9.** Yeni migration yalnızca `media` şemasına dokunur. → Yasak Listesi 3.
- **C10.** Her kabul kriteri en az bir testle karşılanır. → `AGENTS.md` Altın Kural 5.
- **C11.** **Depolamanın gözlem noktası:** depolama `Media` içinde bir **port** (arayüz)
  olarak durur; modül testleri sahte gerçekleştirim kullanır (`docs/testing.md` § 1 modül
  sınırında sahtelemeyi zorunlu kılıyor) ve **tek bir** `tests/Host.Tests` testi CI'a
  eklenen **gerçek bir nesne deposu servisine** karşı koşar. Sahte depoya yazan bir test
  "S3'e yazıldı"yı kanıtlamaz. → **D5**, `0009`'un `mailpit` emsali, `0006`'nın dersi
  (*"sahte bir komşu, kabloyu test etmez"*).
- **C12.** Türev erişim yolları `(varlık kimliği, etiket)`'ten **deterministik** üretilir ve
  işleme durumunun geçişi **tek commit noktasıdır**. İkinci bir koşu üzerine yazar, kopya
  üretmez. → **D10**
- **C13.** **Filigran kaynağı** dağıtım yapılandırmasından bir **dosya yolu/URI** olarak
  gelir — bir medya varlığı kimliği olarak **değil** — ve eksikse uygulama **ayağa
  kalkmaz** (C6 ile aynı desen). → **D7**
- **C14.** Tür kümesi ve boyut sınırı **dağıtım yapılandırmasından** gelir; kabul kriterleri
  **yapılandırılan değeri** doğrular, spec'e kopyalanmış bir sabiti değil. Sert kodlanmış
  bir sınır, yapılandırma değiştiği gün yalan söyler. → **D4**, `0006` C20 · `0009` C9
  kalıbı.
- **C15.** `Media` outbox yazıcısını **konkret tip** olarak kaydeder, paylaşılan
  `IOutboxWriter` arayüzü olarak **değil**. Bir modülün olayı başka modülün outbox
  tablosuna yazılamaz. → Yasak Listesi 16, **D26**.

### KAPSAM DIŞI

- **X1.** **Vitrindeki galeri görüntüleyici** (şerit, büyütme, klavye gezinmesi) — *neden:*
  ADR 0003 Ek 1 sıra #10'un işi; bu spec varlığı üretir, gösterimi değil.
- **X2.** **Video ve 360° tur** — *neden:* her biri kendi işleme hattını ve oynatıcısını
  getirir; `docs/architecture.md` § 4 bunları opsiyonel alan olarak sayıyor ama V1'de
  görsel yeterlidir.
- **X3.** **Fotoğrafların araca bağlanması, sıralanması ve kapak seçimi** — *neden:* bu
  bilgi `VehicleCatalog`'un verisidir (R10); panel akışı `0012` ile gelir.
- **X4.** **Galeri logosunun yüklenmesi** — *neden:* logo bir medya varlığıdır ve bu spec
  yükleme yolunu kuruyor; **hangi** varlığın logo olduğu `Dealership`'in verisidir
  (`Gallery.LogoMediaAssetId`, `0003`).
- **X5.** **Toplu yükleme sihirbazı ve sürükle-bırak çoklu yükleme** — *neden:* tek dosya
  yolu doğrulanmadan çoklu akış kurmak, hata durumlarını katlar.
- **X6.** **Medya kütüphanesi ekranı** (yüklenmiş tüm varlıkları gezme, yeniden kullanma) —
  *neden:* araç başına yükleme yeterlidir; kütüphane ayrı bir arayüz işidir.
- **X7.** **`Vehicle.EnsurePublishable()`'ın "hazır" şartına çevrilmesi** — *neden:* yayın
  eylemi `0012`'nin konusudur; bu spec R17 ile okuma yüzeyini **sunar**, tüketmez. → **D11**

  **Kabul edilen bedel, açıkça:** bu spec bittiğinde A2 hâlâ tam karşılanmaz.
  `EnsurePublishable()` gövdesi bugün yalnız `mediaAssetIds.Count > 0` ve varlığın durumunu
  hiç sormuyor; yani **yüklenmiş ama işlenmemiş** bir görseli olan araç yayına alınabilir
  görünür. Şartı R17'nin yüzeyine bağlamak `0012`'nin işidir ve orada yazılı olmazsa
  sahipsiz kalır — `0010a` R11'in öksüz kalması tam böyle olmuştu.
- **X8.** **Panelde "tekrar dene" eylemi** — *neden:* R16 komutu sunar, ekranı `0012` basar.
  → **D6**

---

## Context

### Mevcut davranış — doğrulandı

- **`Media` modülünün tamamı tek dosyadır:** `src/Modules/Media/Media.Contracts/Events/MediaEvents.cs`.
  Uygulama projesi yok, `DbContext` yok, migration yok.
- **O dosyada yalnızca bir olay var: `MediaAssetReady`.** `docs/architecture.md` § 1'in olay
  akışı `MediaAssetReady / MediaAssetFailed` diyor — **`MediaAssetFailed` sözleşmesi yoktur.**
  → Açık soru **S3**.
- **Dinleyen var, konuşan yok.** `VehicleCatalog`'un `MediaAssetReadyHandler`'ı kapak
  görselini bu olaydan dolduruyor; olayı yayan hiçbir şey yok. `0003` (şube olayları) ve
  `0011` aynı deseni paylaşıyor.
- **Yayın medyaya kilitli.** `Vehicle.EnsurePublishable()` gövdesi `mediaAssetIds.Count > 0`
  şartından ibaret ve doc comment'i *"A2'nin bu spec'te doğrulanabilen kısmı"* diyor.
- **Mockup ne diyor:** panel yeni ilan ekranı *"Yükleme sırasında her fotoğraf tarayıcıda
  1600 / 800 / 400 piksel WebP olarak küçültülür ve galeri logosu filigran eklenir; R2'ye
  yalnızca bu sürümler yazılır."* → Açık sorular **S1** ve **S2**.
- **Depolama kararı verilmiş:** Karar Defteri "Medya | S3-uyumlu (R2/MinIO) + CDN, ilk günden".

### Doğrulanmış bulgular — 2026-10-10 CLARIFY turu

Hepsi komutla ölçüldü; hiçbiri çıkarım değil.

- **`Media` yaprak modül DEĞİL.** `docs/architecture.md` § 1'in yaprak listesi
  `Content · Notifications · Audit · Engagement`; `Media` ÇEKİRDEK bloğunda
  `VehicleCatalog ──► Dealership, VehicleReference, Media` okunun hedefi. § 2.A
  *"`VehicleCatalog → Media` (görseller hazır mı, URL çöz)"* çağrısını sanksiyonlu örnek
  sayıyor ve **`VehicleCatalog.csproj` bugün `Media.Contracts`'a referans veriyor.** Yaprak
  listesini sınayan bir mimari test **yok**. → C5 düzeltildi (**D1**)
- **`MediaAssetReadyHandler` kablolanamaz hâlde.** İmzası
  `HandleAsync(MediaAssetReady, Guid vehicleId, CancellationToken)` — üç parametre; dağıtıcının
  istediği sözleşme `IIntegrationEventHandler<TEvent>.HandleAsync(TEvent, CancellationToken)`.
  Handler o arayüzü **uygulamıyor**, DI'ya **kayıtlı değil** (`VehicleCatalogModule.cs` yalnız
  iki şube işleyicisi kaydediyor) ve tek çağıranı bir test:
  `tests/VehicleCatalog.Tests/Integration/OlayIsleyiciTests.cs:131`, `vehicleId`'yi **elle**
  veriyor. O değer olayın gövdesinde yok ve R10 gereği `Media`'da da yok. → **D2**
- **`MediaAssetReady` tek bir `Url` taşıyor.** R3 türevler üretiyor, mockup 1600/800/400
  diyor; okuma modelinde `ShowcaseListing.CoverMediaUrl` **tek bir `string` kolonu**.
  `docs/frontend.md` § 7 `srcset` **ve** açık `width`/`height` istiyor; cephe bugün
  boyutları sabit yazıyor (`frontend/src/features/catalog/ListingDetailPage.tsx:103-104`
  → `width={960} height={720}`) ve hiçbir yerde `srcset` yok. → **D3**
- **Depolama kodu sıfır.** `grep -rniE 's3|minio|blobstorage|objectstorage' src/` → **0
  eşleşme.** CI'ın `services` bloğunda `postgres` ve `mailpit` var, nesne deposu yok. Yani
  C3'ün her kriteri bugün **yazılamaz** durumda. → C11 (**D5**)
- **`conventions.md` § 5 tablosunda `413` ve `415` yok.** Tablo `422 · 404 · 403 · 401 ·
  409 · 429` sayıyor; R11'in reddi için kod yok. → R13 (**D4**)
- **Filigran kaynağı ilk günde döngüsel olurdu.** `Gallery.LogoMediaAssetId` alanı var
  (`src/Modules/Dealership/Dealership/Domain/Gallery.cs:28`) ve `SetLogo` var (`:46`), ama
  **onu set eden hiçbir akış yok** ve X4 logo yüklemeyi kapsam dışına koyuyor. Temiz bir
  kurulumda hiç medya varlığı olmadığı için, filigranı "bir medya varlığı kimliği" olarak
  yapılandırmak hiçbir şeye işaret eden bir kimlik üretirdi. → C13 (**D7**)
- **A2'nin "hazır"ı bugün ölçülmüyor.** `Vehicle.EnsurePublishable()` gövdesi
  `mediaAssetIds.Count > 0`'dan ibaret (`Vehicle.cs:293`) ve `AttachMediaAsset` durumsuz bir
  `Guid` saklıyor; tek üretim çağıranı geliştirme seed'i. → X7 (**D11**)
- **SCORECARD eski biçimdeydi.** Kartta 5 ölçüt satırı vardı, şablonda 8 (ADR `0008` /
  K1, K3). `0011` kesim tarihinden sonra kapanacak ilk spec olduğu için arşiv kapısının
  muafiyet listesinde **değil**. → **D12**

### Geçerli kurallar

**A2** (yayın için en az bir medya varlığı **hazır** olmalı) · `docs/architecture.md` § 1
(`Media` sorumluluğu: varlık kaydı, orijinal + türevler, işleme durumu, filigran/EXIF
politikası, locale başına `alt` metni, depolama yolu; **medya sahipsizdir**) ·
`docs/frontend.md` § 7 (her görselin `alt` metni vardır, medya `alt`'ı locale başına
backend'den).

### Bilinen tuzaklar

1. **Senkron işleme.** Yükleme isteği içinde türev üretmek, büyük dosyada zaman aşımı üretir;
   R6'nın işleme durumu tam bu yüzden var.
2. **Filigranı türevlere değil orijinale basmak.** Orijinal bozulursa politika değiştiğinde
   geri dönülemez (S2).
3. **`alt` metnini tek dilde tutmak.** `docs/frontend.md` § 7 locale başına istiyor; tek dil,
   Arapça vitrinde İngilizce `alt` demektir.
4. **EXIF'i unutmak.** Telefonla çekilen fotoğraf aracın durduğu adresin koordinatını taşır;
   vitrine konulduğunda galerinin stok lokasyonu dışarı sızar.
5. **Başarısız varlığın sessiz kalması.** R8 olmazsa personel fotoğrafın yüklendiğini sanır
   ve araç yayına alınamaz; hata ekranda değil, eksiklikte görünür.
6. **Bir kısıtı yanlış gerekçeyle yazmak.** C5'in ilk hâli doğru şeyi koruyor ama olmayan
   bir belgeye dayanıyordu. Gerekçesi çürütülen kısıt, korudu­ğu şeyle birlikte düşer —
   kuralın doğru olması yetmez, **dayanağının da** doğru olması gerekir (D1).
7. **Kablolanmamış alıcıyı "var" saymak.** `0003`'ün kaçan hatası birebir buydu ve kartına
   yazıldı: *"test edilmiş bir alıcı, yayıncısı olmadan hiçbir şey kanıtlamaz."* Burada ayna
   görüntüsü duruyor — alıcı var, yayıncı yok, ve alıcı **dağıtılabilir bile değil**.
   Yalnız yayıncıyı yazmak kusuru kapatmaz, yer değiştirir (D2).
8. **Tek URL'yi yeterli sanmak.** Türevleri üretip duyuruda tek erişim yolu taşımak,
   `docs/frontend.md` § 7'nin `srcset` ve CLS borcunu ödenmemiş bırakır — ve ödenmediği
   `0001`'in CLS regresyonunda bir kez görüldü (D3).
9. **Teslimin idempotentliğini işlemenin idempotentliği sanmak.** C4 **teslimi** kapsıyor.
   En-az-bir-kez teslim, en-az-bir-kez **işleme** demektir: türevleri yazıp "hazır"
   damgasını vurmadan çöken bir işleyici, ikinci koşuda dosyaları kopyalar (D10).

### Verilen kararlar

Takım Yöneticisi, 2026-10-10. CLARIFY turunun on iki boşluğu.

- **D1 — `C5` düzeltildi: `Media` yaprak modül değil.** Korunan özellik aynı kaldı, dayanağı
  değişti: `Media` hiçbir modüle senkron çağrı **yapmaz**. `Media → Dealership` yasak, çünkü
  `VehicleCatalog → Media → Dealership` iki atlamalı senkron zincir olur (§ 2.A, üçüncü
  koşul) — "yaprak olduğu için" değil. *Bedel:* yok.
- **D2 — Bu spec alıcıyı da düzeltir.** `MediaAssetReadyHandler`
  `IIntegrationEventHandler<MediaAssetReady>`'i uygular, DI'ya kaydedilir ve `vehicleId`'yi
  parametre olarak almayı bırakır. Gözlem noktası `tests/Host.Tests` (`0003` C17 kalıbı):
  elle handler çağırmak değil, arka plan dağıtıcısının teslim etmesini beklemek.
  *Bedel:* `Modül(ler)` `Media · VehicleCatalog` olur ve `0010a`/`0010b`'nin kriterlerinin
  kesintisiz yeşil kalması DoD şartıdır.
- **D3 — Duyuru türev kümesini taşır.** Her türev için etiket, erişim yolu, genişlik ve
  yükseklik. Okuma modeli kümeyi saklamaya devam eder. *Elenen alternatif:* olayın yalnız
  kimlik taşıması ve URL'lerin § 2.A'nın senkron okumasıyla çözülmesi — vitrin kartı başına
  bir modül çağrısı demekti ve `0001` C3'ün ölçüm protokolü (20 ısınma + 200 ölçüm) o
  maliyeti yakalar. *Bedel:* outbox gövdesi büyür ve filigran politikası değiştiğinde
  **yeniden projeksiyon** gerekir — S4'ün "eski türevler eski filigranı taşır" bedeliyle
  aynı aileden.
- **D4 — Reddetmenin tam sözleşmesi.** Tür kümesi kapalı (JPEG, PNG, WebP) ve tür
  **içerikten** belirlenir; tür ve boyut sınırı yapılandırmadan gelir (C14); desteklenmeyen
  tür `415`, aşılan boyut `413`. İki kod `docs/conventions.md` § 5 tablosuna **eklenir**.
  *Gerekçe:* adına veya istemcinin başlığına güvenen bir kontrol, `.jpg` diye adlandırılmış
  herhangi bir dosyayı hattın içine alır. *Bedel:* paylaşılan bir doküman değişir.
- **D5 — Depolamanın gözlem noktası sabitlendi** (C11): port + sahte, artı CI'a eklenen
  gerçek bir nesne deposu servisine karşı **tek** bir Host testi. *Bedel:* CI'a bir servis
  daha ve daha yavaş bir iş. Eklenmezse C3 bir niyet olarak kalır.
- **D6 — İşleme durumu makinesi yazılır** (R16). `başarısız` terminal değil; tekrar deneme
  **ayrı bir komut**, otomatik değil, ve **aynı varlık kimliğini** korur. *Gerekçe:*
  `0009`'un kaçan hatası otomatik tekrarın kuyruğu sessizce tıkamasıydı; kimliğin korunması
  şart, çünkü `VehicleCatalog` kimliği zaten tutuyor ve yeni kimlik referansı öksüz bırakır.
  *Bedel:* panelde bir "tekrar dene" eylemi gerekir → **X8**, `0012`.
- **D7 — Filigran kaynağı yapılandırmadan bir DOSYA YOLU** (C13), medya varlığı kimliği
  değil; eksikse uygulama ayağa kalkmaz. **Bu karar S4'ü kapatır** — S4 aynı soruyu soruyor
  ve bir varlık kimliği öneriyordu; o öneri ilk günde döngüsel olduğu için değiştirildi.
  *Bedel:* X4'ün yükleme akışı geldiğinde logo iki yerde yaşar; tekilleştirme o günün kararı.
- **D8 — `MediaAssetFailed` kapalı bir sebep kümesi taşır** (R15); serbest metin yok.
  *Gerekçe:* Altın Kural 6 ve Yasak Listesi 23. Kümenin kapalı olması şart — `0010b`'nin
  "tek `InvalidSlug` dört alanı temsil ediyordu" hatası aksi hâlde tekrarlanır.
  *Bedel:* küme spec'te sayılır; sonradan kod eklemek sözleşme değişikliğidir.
- **D9 — EXIF türevlerden silinir VE orijinal herkese açık sunulmaz** (R18). *Gerekçe:*
  tuzak 4'ün zararı orijinalde duruyor; türevi temizleyip orijinali CDN'e koymak tuzağı
  kapatmaz, adresini değiştirir. *Bedel:* türev yeniden üretimi orijinali okuyan iç bir yol
  gerektirir. *Bağımlılık:* **S2 açık** — orijinal saklanmazsa bu kararın ikinci yarısı
  kendiliğinden düşer, zararsız biçimde.
- **D10 — İşleme idempotenttir** (C12): türev yolları `(kimlik, etiket)`'ten deterministik,
  durum geçişi tek commit noktası. *Gerekçe:* `0009` C10'un "kaydet → gönder → işaretle"
  sırasıyla aynı aile. *Bedel:* yeniden koşu CPU harcar, asla çift dosya üretmez.
  *Bağımlılık:* **S1 açık** — işleme sunucuda değilse bu karar konusuz kalır.
- **D11 — Sınır: bu spec okuma yüzeyini SUNAR, tüketmez** (R17 + X7). `EnsurePublishable()`
  düzeltmesi `0012`'nin işi ve X7'de **kabul edilen bedeliyle** yazılı. *Gerekçe:* yayın
  eylemi `0012`'nin konusu; bu spec üç modüle yayılırsa AP-06 sinyali verir.
- **D12 — SCORECARD şimdiki şablona göre yenilendi** (5 → 8 ölçüt satırı). *Gerekçe:*
  `0011` arşiv kapısının muafiyet listesinde değil ve eski kartla kapanırsa ölçtüğü şey
  şablonun sorduğu şey olmaz. *Ayrı iş:* diğer dokuz açık spec'in kartı da eski biçimde ve
  ayrı bir `docs/` turuyla yenilenir.

#### Uygulama planı turu — D13–D26 (2026-10-10)

Takım Yöneticisi, aynı gün. **D13–D16** CLARIFY turunun bıraktığı dört açık sorunun
cevabı; **D17–D26** uygulama planı yazılırken **ölçülerek** çıktı — hiçbiri çıkarım değil.

- **D13 (S1) — Türevler ve filigran SUNUCUDA üretilir.** *Gerekçe:* filigranı istemcide
  uygulamak onu **atlanabilir** kılar ve *"R2'ye yalnızca bu sürümler yazılır"* bir
  garantidir; garanti istemciye bırakılamaz. Türev boyutları politika değişikliğiyle
  değişir ve o gün tüm istemcilerin güncel olması beklenemez. *Bedel:* sunucu CPU'su ve
  asenkron bir hat; yükleme anında görsel hazır olmaz — R6'nın durumu bu yüzden zorunlu.
  Hattın bir turu **sınırlıdır** (en fazla 20 varlık, `OutboxDispatchOptions.BatchSize`
  ile aynı gerekçe): sınırsız bir döngü, sahiplenme kilidinin doğruluğuna bağımlı kalırdı.
  **Bu sınır bir sondayla bulundu** — kilidi kasten bozduğumda tur hiç bitmedi.
- **D14 (S2) — Orijinal SAKLANIR.** `docs/` kazanır (`AGENTS.md` açılış kuralı).
  *Gerekçe:* filigran politikası, boyut kümesi veya kalite ayarı değişirse türevler
  yeniden üretilebilir olmalı; orijinal atılırsa o kapı kalıcı olarak kapanır. **D9'un
  ikinci yarısı bu kararla ayakta kalıyor** (orijinal herkese açık sunulmaz, R18) ve nasıl
  saklandığı **D20**'de. *Bedel:* depolama maliyeti artar ve — açıkça kabul ediliyor —
  aracın durduğu adresin koordinatı ham baytta, yani **bizim depomuzda** yaşar; bir veri
  silme akışı yazıldığında orijinalleri de kapsamak zorundadır.
- **D15 (S3) — `MediaAssetFailed` eklenir ve bu spec yayar.** Gövdesi **D8**'in kapalı
  sebep kümesi (R15). *Gerekçe:* `VehicleCatalog` fotoğrafı bekleyen aracı "eksik bilgi"
  olarak işaretleyebilsin; aksi hâlde başarısızlık yalnızca `Media` içinde bilinir.
  *Bedel:* yok — tüketicisi bu spec'te zorunlu değil (**D2** yalnız `MediaAssetReady`'nin
  alıcısını düzeltiyor).
- **D16 (S5) — `alt` metni ALANI tutulur; girişi `0012`'de.** `0011` bu alan için **hiç
  HTTP yazma ucu açmaz**. *Gerekçe:* `alt` metni araç bağlamında anlamlı ("beyaz BMW 320i,
  önden"); yükleme anında personel o bağlamı kurmamıştır. *Bedel:* `0012` bitene kadar
  `alt` boş kalır ve `docs/frontend.md` § 7'nin erişilebilirlik tabanı o aralıkta eksik.
- **D17 — Görüntü işleme bir PORTUN arkasında; kütüphane LİSANSI ölçülerek seçilir.**
  `IImageProcessor` `Application`'da, uygulaması `Infrastructure`'da. *Gerekçe:* en yaygın
  seçenek ImageSharp, v3'ten beri "Six Labors Split License" taşıyor — açık kaynakta ve
  cirosu eşiğin altındaki şirkette Apache-2.0, **diğer her kullanımda ticari lisans**. Bu
  ürün white-label, galeriye satılan ticari bir kurulum. Lisans bedelini hattın içine
  gömmek, ürün satıldığı gün doğan bir yükümlülüktür ve o gün kütüphaneyi değiştirmek
  bütün hattı yeniden yazmak demektir. *Ölçüldü (NuGet lisans alanları, 2026-10-10):*
  SkiaSharp **MIT**, SkiaSharp.NativeAssets.Linux **MIT**, Magick.NET-Q8-AnyCPU
  **Apache-2.0** (yedek); SixLabors.ImageSharp'ın lisans alanı bir SPDX ifadesi **değil**,
  özel bir metin. **Seçilen: SkiaSharp.** *Bedel:* port bir dolaylılık katmanı ve bugün
  tek uygulaması var.
- **D18 — EXIF yönlendirmesi PİKSELE uygulanır, sonra metadata atılır.** Hat sırası: oku →
  yönlendirmeyi piksele uygula → ölçekle → filigranı bas → metadata'sız encode et.
  *Ölçüldü:* `SKBitmap.Decode` EXIF yönlendirmesini **uygulamıyor** — 600×900 dikey bir
  fotoğraf 600×900 olarak çözülüyor ve etiket atıldığında görsel **yan yatmış** kalıyor.
  Yani yalnız *"EXIF'i temizle"* yazan bir hat R5'i karşılar ve **kullanılamaz** çıktı
  üretir. Tuzak 4 silmeyi söylüyordu; silmenin kendisi bu ikinci tuzağı doğuruyor.
  *Sonuç:* kriter ikiye bölünür — metadata'nın yokluğu ve **yönün doğruluğu** ayrı ayrı
  ölçülür, çünkü "EXIF yok" testi yan yatmış bir görselde de yeşil verir.
- **D19 — Orijinal İSTEK İÇİNDE depoya yazılır; uç `202` döner.** Sıra: doğrula → orijinali
  yaz → kaydı `yüklendi` aç → `202`. *Gerekçe:* işleme asenkron olduğu için (D13) yanıt
  dönerken dosyanın dayanıklı bir yerde olması gerekir; süreç o an düşerse kaybolmaz.
  Orijinal D14 gereği zaten saklanıyor, bu yüzden ek maliyet değil. `201` dönmek "kaynak
  hazır" demekti ve türevler henüz yok. *Elenen alternatif:* baytları veritabanında tutup
  sonra taşımak — megabaytları Postgres'e sokar ve aynı dosyayı iki kez saklar.
- **D20 — Depolama yolu düzeni: `public/` ve `private/` önekleri.**
  `public/assets/{kimlik}/{etiket}.webp` ve `private/originals/{kimlik}.{uzantı}`. CDN
  **yalnız** `public/` önekine bağlanır; erişim yolu üretimi public olmayan bir anahtarı
  **reddeder** — sessizce `null` dönmez, patlar. *Gerekçe:* R18'i bir konvansiyon olarak
  değil **kod seviyesinde** dayatıyor. D9'un ikinci yarısı ancak böyle ölçülebilir hâle
  geliyor ve D12'nin deterministik yol kuralını da karşılıyor. *Bedel:* yeniden üretim
  orijinali okuyan iç bir yol gerektirir (D9'un yazdığı bedel).
- **D21 — `İşleme Durumu` sözlüğe girer.** R6 sözlükte olmayan bir terim getiriyor:
  `docs/domain.md` § 1'de `Medya Varlığı` ve `Kapak Görseli` var, **işleme durumu yok**. Bu
  spec sözlüğe bir satır ekler (`İşleme Durumu | MediaProcessingStatus`) ve geçişleri
  § 2.10'a (M1–M3) yazar — A3'ün ilan durumları gibi. *Gerekçe:* `docs/conventions.md` § 2
  terimin koda girmeden önce sözlüğe girmesini istiyor; DoD'un *"yeni domain terimi yok"*
  kutusunu öyle işaretlemek yanlış olurdu. M1, **D6** uyarınca `başarısız`ı terminal
  **saymaz**. *Bedel:* paylaşılan bir doküman değişir.
- **D22 — Yükleme ucu gövdeyi `HttpRequest` olarak bağlar, `IFormFile` olarak DEĞİL.**
  *Ölçüldü:* `IFormFile` parametresi uca `multipart/form-data` kabul meta verisi ekliyor ve
  o meta verinin `415` kısa devresi **yetkilendirmeden ÖNCE** koşuyor — oturumsuz bir istek
  JSON gövdeyle `415`, multipart gövdeyle `401` alıyordu. Yani yönetim kökü altındaki bir
  uç, oturumu olmayan birine kendi varlığını ve beklediği içerik tipini söylüyordu ve
  `0006`/C18'in rota TARAYAN testi bunu kırdı. *Karar:* `HttpRequest` bağlanır, kabul meta
  verisi hiç oluşmaz, içerik tipi denetimi oturum doğrulandıktan **sonraya** taşınır.
  *Gerekçe:* testi gevşetmek yerine ucu düzeltmek, kuralı bir sonraki panel için de ayakta
  tutar. *Bedel:* `IFormFile`'ın bildirimsel bağlaması ve OpenAPI meta verisi elle yazılır.
- **D23 — Tür kümesi yapılandırmadan gelir (C14) AMA SVG koddan reddedilir.**
  Yapılandırma yalnız **JPEG / PNG / WebP** arasından seçebilir; `image/svg+xml`
  yapılandırmayla **açılamaz** ve tanınır-reddedilir (`Unknown` sayılmaz, çünkü o zaman
  neden reddedildiği logdan okunamaz). *Gerekçe:* SVG aynı origin'de script çalıştırır ve
  bu depo o tuzağa `Program.cs`'in seed ucunda bir kez bakıp yüzeyi kapatmış. **Bir
  yapılandırma satırıyla geri açılabilen güvenlik kararı, verilmemiş karardır.** C14'ün
  istediği şey (sınırın yapılandırılabilir ve kriterin yapılandırılan değeri doğrulaması)
  korunuyor; daraltılan yalnız kümenin **üst sınırı**. *Bedel:* galeri bir gün başka bir
  raster formatı isterse kod değişir — ve bu, bir sözleşme değişikliği olarak görünür olur.
- **D24 — Test ikizi LocalStack'in S3'ü, MinIO DEĞİL.** C11'in istediği gözlem noktası
  korunuyor: depolama bir port, ve **gerçek** bir nesne deposu servisine karşı koşan
  testler var. *Ölçüldü:* `Testcontainers.Minio` 4.15.0 paketi var (MIT) ama imajı anonim
  olarak **çekilemiyor** — `pull access denied for minio/minio, repository does not
  exist`. Bu bir Docker Hub erişim sorunu değil: aynı makinede `alpine:3.20` sorunsuz indi
  ve `quay.io/minio/minio` 401 döndü. Çekilemeyen bir imaja bağlı test CI'da yazı turadır.
  LocalStack'in gidiş-dönüşü ölçüldü: kova, PUT, GET'te bayt ve `Content-Type` birebir,
  olmayan nesnede 404. **C3'ün ÜRETİM kararı değişmedi** (`S3-uyumlu (R2/MinIO) + CDN`);
  değişen yalnız geliştirme ve test ikizi. *Bedel:* LocalStack imajı büyük ve testler iki
  konteyner açıyor; CI süresi artar.
- **D25 — Yükleme ucuna hız sınırı KONMAZ.** *Gerekçe:* uç oturum arkasında ve kullanıcısı
  galeri personeli; D4'ün piksel bütçesi asıl CPU riskini zaten kesiyor. Oturumlu bir uca
  sınır koymak, X5'in toplu yüklemesini baştan engeller ve kapsamı genişletir. *Bedel:*
  tek bir personel hesabı, art arda yüklemeyle hattı meşgul edebilir — kötü niyet değil,
  kapasite sorunu; ölçülürse ayrı bir karardır.
- **D26 — `Media` outbox yazıcısını KONKRET tip olarak kaydeder** (C15). *Ölçüldü ve bu bir
  kırılmanın bedeliydi:* `IOutboxWriter` non-generic bir arayüz ve dört modül onu kendi
  `DbContext`'iyle kaydediyor; DI'da **son kayıt kazanıyor**. `AddMedia` Host'ta
  `AddLeads`'ten sonra çağrıldığı için `Leads`'in yazıcısı `Media`'nın oldu: `LeadCaptured`
  olayı `media` şemasının outbox'ına yazıldı, `Leads`'in dağıtıcısı kendi tablosunda
  hiçbir şey bulamadı ve **bildirim zinciri sessizce durdu** — `TalepBildirimiZinciriTests`
  30 saniye bekleyip **0** kayıt gördü. *Karar:* konkret tip kaydı ve bunu koruyan bir
  mimari kapı. *Ayrı iş — kapsam dışı:* aynı tuzak `Dealership` ve `Identity`'de de
  duruyor (ölçüldü: temiz `main`'de `IOutboxWriter` → `OutboxWriter<LeadsDbContext>`, üç
  kayıt). Kapı bilinçli olarak yalnız `Media`'ya bakıyor; dört modülü kapsayan bir kapı
  ilk koşuda kırmızı verir ve ADR `0008`/K8 tam olarak bunu tartışıyor.

---

## Açık sorular — **hepsi kapandı**

> Dördü de 2026-10-10'da karara bağlandı (**D13–D16**); S4 CLARIFY turunda **D7** ile
> kapanmıştı. Soru metinleri ve gerekçeler **korunuyor** — gerekçe izi silinmez.

**S1. Türevler ve filigran nerede üretilir — tarayıcıda mı sunucuda mı?** — ✅ **KAPANDI: D13** (2026-10-10)
Mockup "tarayıcıda küçültülür" diyor.
*Öneri (kabul edildi):* **sunucuda.**
*Gerekçe:* filigranı istemcide uygulamak onu **atlanabilir** kılar; "R2'ye yalnızca bu
sürümler yazılır" garantisi istemciye bırakılamaz. Ayrıca türev boyutları politika
değişikliğiyle değişir ve o gün tüm istemcilerin güncel olması beklenemez.
*Bedel:* sunucu CPU'su ve asenkron bir işleme hattı gerekir; yükleme anında görsel hazır
olmaz — R6'nın işleme durumu bu yüzden zorunlu.

**S2. Orijinal dosya saklanır mı?** — ✅ **KAPANDI: D14** (2026-10-10)
Mockup "R2'ye **yalnızca** bu sürümler yazılır" diyor; `docs/architecture.md` § 1 ise
"**orijinal + türevler**" diyor. Çelişki var.
*Öneri (kabul edildi):* **orijinal saklanır** (`docs/` kazanır — `AGENTS.md` açılış kuralı).
*Gerekçe:* filigran politikası, boyut kümesi veya kalite ayarı değişirse türevler yeniden
üretilebilir olmalı; orijinal atılırsa o kapı kalıcı olarak kapanır.
*Bedel:* depolama maliyeti belirgin biçimde artar (orijinaller türevlerin toplamından büyük
olur). İsterseniz orijinali "soğuk" saklama sınıfına koymak ayrı bir karardır.

**S3. `MediaAssetFailed` sözleşmesi eklenecek mi?** — ✅ **KAPANDI: D15** (2026-10-10)
Olay akışında yazılı, kodda yok.
*Öneri (kabul edildi):* eklenir ve bu spec yayar (R8).
*Gerekçe:* `VehicleCatalog` fotoğrafı bekleyen aracı "eksik bilgi" olarak işaretleyebilsin;
aksi hâlde başarısızlık yalnızca `Media` içinde bilinir.
*Bedel:* yok — tüketicisi bu spec'te zorunlu değil.

**S4. Filigran kaynağı `Media`'ya nasıl verilir?** — ✅ **KAPANDI: D7** (2026-10-10)
Sorulduğu hâlindeki öneri *"bir medya varlığı kimliği olarak yapılandırmadan"* idi ve
gerekçesi C5'in yaprak iddiasıydı. İkisi de değişti: C5'in iddiası **yanlıştı** (D1) ve
varlık kimliği **ilk günde döngüsel** oluyordu — `Gallery.LogoMediaAssetId`'yi set eden
hiçbir akış yok ve X4 logo yüklemeyi kapsam dışına koyuyor, yani temiz bir kurulumda
yapılandırılan kimlik hiçbir şeye işaret ederdi.
**Karar:** filigran kaynağı yapılandırmadan bir **dosya yolu/URI** olarak gelir ve eksikse
uygulama ayağa kalkmaz (C13). `Media` yine `Dealership`'i çağırmaz — ama artık doğru
gerekçeyle: iki atlamalı senkron zincir yasak.
*Bedel değişmedi:* logo değiştiğinde **eski türevler eski filigranı taşımaya devam eder**;
yeniden üretim ayrı bir iştir.

**S5. `alt` metnini kim, nerede yazar?** — ✅ **KAPANDI: D16** (2026-10-10)
*Öneri (kabul edildi):* bu spec `alt` metni **alanını** tutar (R9) ama panel girişi yapmaz;
giriş `0012` ile, fotoğrafın araca bağlandığı ekranda gelir. **`0011` `alt` metni için hiç
HTTP yazma ucu açmaz.**
*Gerekçe:* `alt` metni araç bağlamında anlamlı ("beyaz BMW 320i, önden"); medya yükleme
anında personel o bağlamı henüz kurmamıştır.
*Bedel:* `0012` bitene kadar `alt` metni boş kalır ve `docs/frontend.md` § 7'nin
erişilebilirlik tabanı o aralıkta eksik olur. Alternatif: yüklemede zorunlu kılmak —
personeli her fotoğrafta üç dilde metin yazmaya zorlar ve yükleme akışını ağırlaştırır.

---

## Acceptance Criteria

Kırk yedi kriter, on grupta. CLARIFY turunun saydığı on kümeyi ve D1–D26'yı kapsar.
Yapılandırmadan gelen her değer için kriter **yapılandırılan değeri** doğrular, spec'e
kopyalanmış bir sabiti değil (C14).

### A. Yükleme ve reddetme

- **AC-1.** Oturum açmış personel bir görsel yükler; yanıt varlığın kimliğini ve işleme durumunu taşır.
- **AC-2.** Oturumu olmayan biri görsel yükleyemez; istek **içerik tipinden bağımsız olarak** reddedilir.
- **AC-3.** Yapılandırılan tür kümesinde olmayan bir dosya `415` ile reddedilir ve varlık kaydı oluşmaz.
- **AC-4.** Dosyanın türü içeriğinden belirlenir; `image/jpeg` adıyla gönderilen bir SVG reddedilir.
- **AC-5.** Yapılandırmaya SVG eklenemez; eklenmişse uygulama ayağa kalkmaz.
- **AC-6.** Yapılandırılan boyut sınırını aşan dosya `413` ile reddedilir ve varlık kaydı oluşmaz.
- **AC-7.** Yapılandırılan piksel bütçesini aşan dosya reddedilir ve varlık kaydı oluşmaz.
- **AC-8.** Reddedilen istek çevrilmiş metin değil `errorCode` döner.
- **AC-9.** Kabul edilen tür kümesi ve boyut sınırı yapılandırmadan okunur; koda gömülü bir sabit yoktur.

### B. İşleme durumu ve tekrar deneme

- **AC-10.** Yüklenen varlık `yüklendi` durumunda, depolama yolu ve kimliğiyle kaydedilir.
- **AC-11.** İşleme başladığında durum `işleniyor`, başarıyla bittiğinde `hazır` olur.
- **AC-12.** Tanımlı olmayan durum geçişi yapılamaz.
- **AC-13.** İşleme başarısız olduğunda durum `başarısız` olur; varlık `yüklendi` durumunda kalmaz.
- **AC-14.** `başarısız` terminal değildir: tekrar deneme ayrı bir komutla tetiklenir ve aynı varlık kimliğini korur.
- **AC-15.** Tekrar deneme otomatik tetiklenmez; komut çağrılmadan durum kendiliğinden değişmez.
- **AC-16.** Bir işleme turu, başka bir turun sahiplendiği varlığı yeniden işlemez.

### C. Türev üretimi

- **AC-17.** Hazır varlık 1600 / 800 / 400 piksel genişlikte üç WebP türev taşır.
- **AC-18.** Kaynak görsel bir türev genişliğinden küçükse o türev kaynaktan büyütülmez.
- **AC-19.** Her türev kendi depolama yolunda durur ve yol `(varlık kimliği, etiket)`'ten deterministik üretilir.
- **AC-20.** "Hazır" damgasından önce kesilen bir koşu, ikinci turda kopya dosya üretmez.

### D. Filigran

- **AC-21.** Üç türevin hepsinde filigran bulunur.
- **AC-22.** Filigran kaynağı yapılandırmadan bir dosya yolu olarak gelir; `Media` bunun için başka bir modülü çağırmaz.
- **AC-23.** Filigran kaynağı erişilemezse varlık `başarısız` olur; filigransız türev üretilmez.

### E. EXIF ve orijinal

- **AC-24.** Yüklenen görselin konum ve cihaz bilgisi türevlerde bulunmaz.
- **AC-25.** EXIF yönlendirme etiketi taşıyan görselin türevi doğru yönde üretilir.
- **AC-26.** Orijinal dosya işleme bittikten sonra da depoda durur.
- **AC-27.** Orijinal dosya herkese açık sunulmaz; onun için erişim yolu üretilemez.

### F. Duyurular, gövdeleri ve idempotentlik

- **AC-28.** Varlık hazır olduğunda `MediaAssetReady` yayılır; duyuru varlığın kimliğini taşır.
- **AC-29.** Hazır duyurusu **türev kümesini** taşır: her türev için etiket, erişim yolu, genişlik ve yükseklik.
- **AC-30.** Duyuru, iş verisiyle aynı transaction'da outbox'a yazılır.
- **AC-31.** Aynı varlık için ikinci bir duyuru oluşmaz.
- **AC-32.** İşleme başarısız olduğunda `MediaAssetFailed` yayılır.
- **AC-33.** Başarısızlık duyurusunun sebebi kapalı bir kümedendir; serbest metin veya çevrilmiş metin taşımaz.
- **AC-34.** `MediaAssetReady` ve `MediaAssetFailed`'in şekli kırılmaz.
- **AC-35.** `Media`'nın olayı başka bir modülün outbox tablosuna yazılmaz.

### G. Duyurunun alıcıya ulaşması

- **AC-36.** `MediaAssetReadyHandler` `IIntegrationEventHandler<MediaAssetReady>`'i uygular ve DI'ya kayıtlıdır.
- **AC-37.** Duyuru, arka plan dağıtıcısı üzerinden alıcıya ulaşır; elle handler çağıran bir test bunun yerine geçmez.

### H. `alt` metni

- **AC-38.** Bir varlık etkin her locale için ayrı `alt` metni taşıyabilir.
- **AC-39.** `alt` metni boş olan bir varlık yine hazır olabilir.

### I. Depolama, CDN ve okuma yüzeyi

- **AC-40.** Türevin erişim yolu yapılandırılan taban adresten üretilir.
- **AC-41.** Türev, S3 uyumlu sağlayıcıya yazılır ve oradan okunabilir.
- **AC-42.** `Media`, bir varlık kümesinin hazır olup olmadığını yanıtlayan bir okuma yüzeyi sunar.

### J. Yapılandırma ve sınır

- **AC-43.** Depolama kimlik bilgileri eksikken uygulama ayağa kalkmaz.
- **AC-44.** Erişim taban adresi eksikken uygulama ayağa kalkmaz.
- **AC-45.** Filigran kaynağı yapılandırılmamışken uygulama ayağa kalkmaz.
- **AC-46.** `media` migration'ı yalnızca `media` şemasına dokunur.
- **AC-47.** `Media` hiçbir modüle senkron çağrı yapmaz ve başka modülün tablosuna veya dosya deposuna erişmez.

---

## Definition of Done

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi (D1–D12 CLARIFY turu, D13–D26 uygulama planı turu; S1–S5'in beşi de kapandı).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — `Media` başka modülün tablosuna veya deposuna
      erişmiyor ve **hiçbir modüle senkron çağrı yapmıyor** (C5, **D1**). *Maddenin önceki
      hâli "yaprak modül kalıyor" diyordu ve C5'in düzeltilen yanlışını tekrarlıyordu.*
- [ ] **Duyuru alıcısına DAĞITICI ÜZERİNDEN ulaştı** (**D2**): `MediaAssetReadyHandler`
      `IIntegrationEventHandler<MediaAssetReady>`'i uyguluyor, DI'ya kayıtlı ve kanıt
      `tests/Host.Tests`'te — elle handler çağıran bir test bunun yerine geçmez
      (`0003` C17 kalıbı).
- [ ] **`0010a` ve `0010b`'nin kriterleri kesintisiz yeşil kaldı** — `VehicleCatalog`
      tarafına dokunulduğu için öncesi ve sonrası koşu çıktıları PR'da (**D2**).
- [ ] **Türev kümesi uçtan uca taşındı** (**D3**): duyuru ve okuma modeli her türev için
      etiket, erişim yolu, **genişlik ve yükseklik** taşıyor.
- [ ] **CI'a nesne deposu servisi eklendi** ve **tek** bir `Host.Tests` testi ona karşı
      koşuyor (**D5**, C11); modül testleri sahte port kullanıyor.
- [ ] **`413` ve `415`, `docs/conventions.md` § 5 tablosuna eklendi** (**D4**).
- [ ] **İşlemenin idempotentliği sondayla gösterildi** (**D10**, C12): "hazır" damgasından
      önce kesilen bir koşu ikinci turda kopya dosya üretmiyor.
- [ ] **X7 ve X8'in kabul edilen bedelleri PR'da adıyla yazılı** — özellikle X7: bu spec
      bittiğinde A2 hâlâ tam karşılanmıyor.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `media` şemasına dokunuyor.
- [ ] EXIF temizliğinin gerçekten uygulandığı testle gösterildi.
- [ ] Yapılandırma eksikken uygulamanın ayağa kalkmadığı testle gösterildi (C6).
- [ ] <!-- DoD:T7 --> Yeni domain terimi yok — spec yalnızca sözlükteki terimleri kullanır.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/<no>-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri varsa Arapça görüntü de var.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
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
