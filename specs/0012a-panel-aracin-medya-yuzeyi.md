# 0012a — Panel: Aracın Medya Yüzeyi

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (komut ve sorgu yüzeyi) · `Media` (`alt` metni yazma ucu + iki okuma) + `Media.Contracts` · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0010a` + `0010b` (araç kaydı, düzenleme ve liste — tamamlandı) · `0011` (Media — tamamlandı) |
| **Dayanak** | ADR 0003 Ek 1, sıra #5'in **birinci dilimi** (→ **D1**) · `0011` § X3, § X7, § X8 ve **D16** |
| **Kararlar** | **D1–D19**. D1–D8 `0012`'nin 2026-10-11 CLARIFY turundan; **D9–D19** kriter turunda **ölçülerek** eklendi. Açık soru yok. |

> **R/C/AC bu PR'da `main`'e giriyor ve orada donuyor** (ADR `0008` / K2, `docs/git.md` § 1).
> Bu commit'ten sonra Requirements, Constraints veya Acceptance Criteria'da yapılan her
> değişiklik **sayılan bir revizyondur** ve kendi commit'ine girer.
>
> **Bu dosya `0012`'nin ikiye bölünmesiyle doğdu** — gerekçesi ve bedeli **D1**'de yazılı.
> CLARIFY turu kararları verdi ve kriterleri bilerek yazmadı; bu tur on bir karar daha
> **ölçerek** ekledi (**D9–D19**) ve otuz üç kriteri yazdı. `0011`'de kriterler
> implementasyondan **sonra** büyümüştü ve K2'nin ölçümü o yüzden delinmişti; bu kez
> sıralama doğru.

---

## Intent

`0011` bittiğinde fotoğraf sisteme giriyor: yükleniyor, türevleri ve filigranı üretiliyor,
`Hazır` oluyor ve duyurusu yayılıyor. **Ama hiçbir fotoğraf hiçbir araca bağlı değil.**
Ölçüldü: aracın sıralı medya listesine yazan tek üretim yolu geliştirme seed'i.

**Galeri çalışanı** yüklediği fotoğrafları aracın sayfasında sıralamak, hangisinin kapak
olacağını seçmek, görme engelli ziyaretçi için kısa tanım yazmak ve işlemesi başarısız olan
fotoğrafı yeniden denemek istiyor. Bugün bunların hiçbirini yapamıyor; yüklediği fotoğraf
sistemde sahipsiz duruyor.

Bu iş bittiğinde bir aracın fotoğrafları sıralı, kapağı seçili ve tanımları yazılmış olur.
Başarı ölçütü: **bir aracın "yayına uygun mu" sorusu ilk kez ölçülebilir** — çünkü "en az bir
hazır fotoğrafı var mı" sorusunun cevabı artık gerçek veriden geliyor. Yayına almanın
kendisi bu dilimin işi değildir (**D7**).

---

## Requirements

- **R1.** Personel yüklenmiş bir fotoğrafı **araca bağlar**; bağlı fotoğraflar **sıralıdır**
  ve sıra personelin verdiği sıradır.
- **R2.** Sıralı listenin **ilk** elemanı **kapak görselidir**; personel kapağı sırayı
  değiştirerek belirler. Ayrı bir "kapak" seçimi yoktur. → `docs/domain.md`, *Kapak Görseli*
- **R3.** Personel bağlı bir fotoğrafı araçtan **çıkarabilir**; çıkarmak varlığı **silmez**.
- **R4.** Personel her fotoğraf için **`alt` metnini** etkin her dilde yazabilir; bir dili boş
  bırakmak geçerli bir durumdur. → `0011` **D16**
- **R5.** İşlemesi **başarısız** olmuş bir fotoğraf panelde ayırt edilir ve personel **tekrar
  dene** eylemini çağırabilir. Tekrar deneme **açık** bir eylemdir, otomatik değildir.
  → `docs/domain.md` **M1**, `0011` **X8**
- **R6.** Bir aracın yayın koşullarından **medya koşulu** ölçülebilir hâle gelir: en az bir
  bağlı fotoğraf **`Hazır`** durumunda. → **A2**, **M3**, `0011` **X7**
- **R7.** Medya ekranı **yükleniyor**, **boş** ve **hata** durumlarını ele alır; bir varlığın
  **işleniyor** hâli ekranda ayrı görünür — o bir yükleme durumu değil, varlığın durumudur.
  → `docs/frontend.md` § 3, **M1**
- **R8.** Panel personelin tercih ettiği dilde çalışır; Arapça'da RTL.
- **R9.** Personel panelden **tek bir fotoğraf** yükleyebilir. → **D9**

---

## Constraints

### Uyulması zorunlu

- **C1.** Tüm eylemler **oturum gerektirir**. → `0006` R7
- **C2.** **Medya sahipsizdir** (**M2**): bir fotoğrafın hangi araca ait olduğu `Media`'da
  tutulmaz; sıralı liste `VehicleCatalog`'un verisidir.
- **C3.** `VehicleCatalog → Media` yalnız **okuma** ve **tek atlama**
  (`docs/architecture.md` § 2.A). `Media` hiçbir modüle senkron çağrı **yapmaz** —
  bağımlılık okları ondan dışarı çıkmaz (`0011` C5).
- **C4.** "Hazır" sorusu `Media`'nın **okuma yüzeyinden** sorulur, olay gövdesinden değil.
  → `0011` **D11**
- **C5.** `alt` metni `Media`'nın verisidir ve `VehicleCatalog` onun kopyasını **tutmaz**.
  → Yasak Listesi 1 ve 10, **D3**
- **C6.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner.
  → Yasak Listesi 23–24
- **C7.** Mevcut davranış yeniden yazılmaz. **Adıyla yazılı iki istisnası vardır:**
  (a) `Vehicle.EnsurePublishable()`'ın gövdesi **D6** ile genişletilir;
  (b) `Media` modülünde `alt` metni için bir yazma ucu açılır (**D3**). Başka hiçbir mevcut
  davranışa dokunulmaz.
- **C8.** Yeni migration yalnızca kendi şemasına dokunur. → Yasak Listesi 3
- **C9.** Her kabul kriteri en az bir testle karşılanır. → `AGENTS.md` Altın Kural 5

### KAPSAM DIŞI

- **X1.** **Yayına alma ve durum eylemleri** (yayına al, rezerve, satıldı, yayından kaldır,
  arşivle, toplu eylem, öne çıkar) — *neden:* `0012b`'nin konusu. Bu dilim yayına
  uygunluğu **ölçülebilir** kılar, yayına **almaz** (**D7**).
- **X2.** **Rol bazlı eylem kısıtı** — *neden:* ADR 0003 unsur 3 alan/kayıt düzeyi yetkiyi
  ayrı bir karara bağladı. Bu dilimde eylemler **rol ayrımı yapmadan** oturum açmış
  personele açıktır. Soru `0012b` § Açık sorular **S1**'de duruyor ve cevabı ikisini
  birlikte bağlar.
- **X3.** **Çoklu / sürükle-bırak yükleme ve toplu yükleme sihirbazı** — *neden:* `0011` X5
  geçerliliğini koruyor; tek dosya yolu doğrulanmadan çoklu akış hata durumlarını katlar.
- **X4.** **Medya kütüphanesi ekranı** (yüklenmiş tüm varlıkları gezme, yeniden kullanma) —
  *neden:* `0011` X6 geçerliliğini koruyor; araç başına yükleme yeterlidir.
- **X5.** **Vitrindeki galeri görüntüleyici** (şerit, büyütme, klavye gezinmesi) — *neden:*
  `0016`'nın işi. Bu dilim panel tarafını kurar, ziyaretçi tarafını değil.
- **X6.** **Galeri logosunun yüklenmesi** — *neden:* `0011` X4; **hangi** varlığın logo
  olduğu `Dealership`'in verisidir (`Gallery.LogoMediaAssetId`, `0003`).
- **X7.** **Video ve 360° tur** — *neden:* `0011` X2 geçerliliğini koruyor.
- **X8.** **Araç kaydının silinmesi** — *neden:* `0010b` X19 silmeyi `Arşiv` durumuna, onu da
  `0012b`'ye bağladı. R3'ün "çıkarmak silmek değildir" kuralı **F1**'in aynı ilkesidir.

---

## Context

### Mevcut davranış — doğrulandı (2026-10-11, `origin/main` = `b99912a`)

- **Fotoğrafı araca bağlayan hiçbir üretim yolu yok.** `Vehicle.AttachMediaAsset`'in tek
  üretim çağıranı geliştirme seed'i ve rastgele bir `Guid` yazıyor
  (`ShowcaseSeeder.cs:129`). Yazma modelinin `media_asset_ids` listesi üretimde **her zaman
  boş**.
- **`MediaAssetReady`'nin alıcısı yazma modeline dokunmuyor.** `MediaAssetReadyHandler`
  yalnız vitrin okuma modelinin `CoverMediaUrl` kolonunu yazıyor
  (`MediaAssetReadyHandler.cs:61-65`) — `0011` AC-36 bunu zaten böyle tanımladı.
- **`EnsurePublishable()` yalnız SAYIYA bakıyor:** `mediaAssetIds.Count > 0`
  (`Vehicle.cs:293`); varlığın durumunu hiç sormuyor. `0011` X7 bedeli açıkça kabul etti.
- **`Media`'nın uçları hazır:** yükleme `POST …/media/assets` ve **tekrar deneme**
  `POST …/media/assets/{id}/retry` (`MediaAdminEndpoints.cs:38` ve `:55`). Tekrar deneme
  komutu ve ucu **var**; eksik olan ekrandır (`0011` X8).
- **`alt` metninin alanı var, ucu yok:** `MediaAsset.AltText` (`MediaAsset.cs:81`) ve
  `MediaAsset.SetAltText` (`:180`) duruyor; `MediaAdminEndpoints` hiçbir yazma ucu açmıyor
  (`0011` D16 bunu bilerek böyle bıraktı).
- **Hazırlık sorusunun yüzeyi "hepsi" diye soruyor:** `IMediaAssetQueries.AreAllReadyAsync`
  (`MediaAssetContracts.cs:55-57`) kümenin **tamamının** hazır olmasını ölçüyor; **A2** ise
  **en az bir** hazır varlık istiyor. Üç varlıklı, biri hazır bir araçta ikisi **zıt** cevap
  verir. → **D5**

### Geçerli domain kuralları

**A2**'nin üçüncü koşulu (en az bir medya varlığı **hazır**) · **M1** (işleme durumu
geçişleri; `Başarısız` terminal değil, tekrar deneme **açık** komutla ve **aynı kimlikle**) ·
**M2** (medya sahipsizdir) · **M3** (yalnız `Hazır` varlık A2'yi karşılar; `İşleniyor` aracı
yayına yetiştirmez) · **F1** (kapatmak/çıkarmak silmek değildir) · sözlükteki **Kapak
Görseli** (sıralı medya listesinin ilk elemanı).

### Bilinen tuzaklar

1. **"Hepsi hazır" ile "en az bir hazır"ı aynı sanmak.** `AreAllReadyAsync` ikinci soruya
   cevap vermiyor; onunla A2'yi ölçen bir uygulama, iki fotoğrafı işlenirken bir fotoğrafı
   hazır olan aracı **reddeder**. → **D5**
2. **Kapağı olay sırasına bırakmak.** Teslimat sıra garantisi vermiyor; gelen her olayda
   kapağı güncelleyen bir alıcı, üç görselli araçta **en son hazır olanı** kapak yapar.
   `MediaAssetReadyHandler:56-60` bunu ölçüp reddetti: kapak kararı **domain'e** aittir,
   olay sırasına değil. R2 bu yüzden sırayı tek gerçek kaynak yapıyor.
3. **`alt` metnini `VehicleCatalog`'a kopyalamak.** Metin `Media`'nın verisidir; kopyası
   "tekrarı önlemek için ortak DTO" tuzağının tersi yönüdür ve iki yerde iki farklı metin
   üretir. → **C5**
4. **Bağlamayı okuma modeline yazmak.** Bağlama **yazma modelinin** verisidir
   (`vehicles.media_asset_ids`); vitrin okuma modeli yalnız kapak URL'ini taşıyor. Okuma
   modeline yazan bir bağlama, ilk yeniden yansıtmada kaybolur.
5. **Tekrar denemeyi otomatikleştirmek.** **M1** açık komut diyor ve gerekçesi kayıtlı:
   `0009`'un kaçan hatası otomatik tekrarın kuyruğu sessizce tıkamasıydı.

### Verilen kararlar

Hepsi 2026-10-11 CLARIFY turunda Takım Yöneticisi onayıyla verildi.

- **D1 — `0012` ikiye bölündü; bu dosya birinci dilim.** *Gerekçe:* CLARIFY turu `0012`'ye
  dört devralınmış iş (`0011` X3, X7, X8, D16), bir yansıtma yolu, modülün hiç olmayan
  outbox'ı, A8'in yazma tarafı, slug'ın kalıcılığı ve toplu eylem kurallarını ekledi.
  `0010` bundan **daha azı** için D9 ile ikiye bölünmüştü (`62a70f5`). Bölme sınırı **A2'nin
  medya koşulu**: bu dilim onu ölçülebilir kılar, `0012b` onu tüketir. *Bedel:* iki spec,
  iki PR yığını ve vitrin bir dilim daha geliştirme seed'ine bağlı kalır. Bölme sınırı
  burada yazılı olmazsa `0012b` aynı öksüzleri yeniden devralır.
- **D2 — Fotoğrafın araca bağlanması, sıralanması ve kapak seçimi bu dilimde** (`0011` X3).
  Kapak **ayrı bir alan değildir**, sıralı listenin ilk elemanıdır. *Gerekçe:* sözlük kapağı
  böyle tanımlıyor; ikinci bir "kapak" alanı aynı olguyu iki yerde tutar ve biri bayatlar.
  *Bedel:* "kapak yap" eylemi bir **sıra** işlemidir ve arayüzde böyle anlatılmak zorunda.
- **D3 — `alt` metninin yazma ucu `Media` modülünde açılır.** *Gerekçe:* metin
  `MediaAsset`'in alanıdır (`MediaAsset.cs:81`); başka modülün şemasına yazmak Yasak Listesi
  1'dir. `0011` D16 alanı tutup ucu bilerek açmadı. *Bedel:* bu spec **iki modüle** dokunur
  ve ikincisi kapanmış bir spec'in modülüdür — C7'nin adıyla yazılı istisnası.
- **D4 — Tekrar deneme EKRANI bu dilimde; komutu ve ucu `0011`'de zaten var**
  (`MediaAdminEndpoints.cs:55`). *Gerekçe:* `0011` X8 tam bunu söylüyor. *Bedel:* yok.
- **D5 — `Media`'nın okuma yüzeyi "hazır olanların kümesini" döndüren bir okuma kazanır.**
  A2 "küme boş değil" ile, kapak "sıralı listede hazır olan ilk varlık" ile kurulur.
  *Gerekçe:* `AreAllReadyAsync` "hepsi" diye soruyor ve A2'yi ölçmüyor (Tuzak 1). *Bedel:*
  `Media.Contracts` **eklemeli** bir okuma kazanır; spec'in modül listesi `Media`'yı kapsamak
  zorunda. *Elenen alternatif:* yalnız kapak kimliğini `AreAllReadyAsync`'e vermek — kapağı
  hazır olmayan ama ikinci fotoğrafı hazır olan aracı A2'ye aykırı biçimde reddederdi.
- **D6 — `Vehicle.EnsurePublishable()`'ın gövdesi "en az bir HAZIR varlık" şartına
  genişletilir** (`0011` X7). *Gerekçe:* A2 ve M3 bunu istiyor; `0011` şartı bilerek bu
  dilime bıraktı ve "orada yazılı olmazsa sahipsiz kalır" diye uyardı. *Bedel:* `0010a`'nın
  agregat testi (`VehicleTests.cs:26`) yeni şartı yansıtacak biçimde güncellenir — kapanmış
  bir spec'in testine dokunulur ve bu PR'da adıyla yazılır.
- **D7 — Yayın eyleminin kendisi bu dilimde YOK.** *Gerekçe:* yayına alma bir durum
  geçişidir ve `0012b`'nin konusu; ikisini birlikte yapmak D1'in bölme sınırını siler.
  *Bedel, açıkça:* bu dilim bittiğinde galeri hâlâ **hiçbir aracı vitrine çıkaramaz** ve
  vitrin seed'e bağlı kalır. Dilimin değeri, `0012b`'nin yayın koşulunu ölçebilir hâle
  getirmesidir.
- **D8 — "İşleniyor" ekranda ayrı bir hâl olarak görünür.** *Gerekçe:* **M1**'e göre o
  varlığın durumudur, ekranın yükleme durumu değil; ikisini aynı iskeletle göstermek,
  dakikalarca süren işlemeyi "sayfa yükleniyor" gibi gösterirdi.

#### Kriter turu — D9–D19 (2026-10-11)

Takım Yöneticisi, aynı gün. Hepsi `origin/main` (`418f7a6`) ve `docs/0012-clarify`
(`d8d16c4`) üzerinde **ölçülerek** çıktı.

- **D9 — Tek dosya yükleme kontrolü BU dilimde** (R9). *Ölçüldü:* `frontend/src/` içinde
  `input type="file"`, `FormData` ve `multipart` → **sıfır eşleşme**; panelden hiçbir
  fotoğraf yüklenemiyor. R1 *"yüklenmiş bir fotoğrafı bağlar"* diyor ve o ön koşul bugün
  hiçbir yoldan sağlanamıyor. *Gerekçe:* kontrol olmadan bu dilim, var olmayan
  fotoğrafları sıralayan bir ekran teslim eder ve *"bir aracın fotoğrafları sıralı"*
  başarı ölçütü ölçülemez. X3 yalnız **çoklu/sürükle-bırak ve sihirbazı** dışlıyor
  (`0011` X5'in aynısı), tek dosya yolunu değil. *Bedel:* bir kontrol daha ve onun üç
  durumu; `413`/`415` hatalarının çevirisi.
- **D10 — `Media.Contracts` TOPLU bir detay okuması kazanır.** D5 yalnız "hazır olanların
  kümesi"ni zorunlu kılıyordu; ekran varlık başına **durum + `alt` + türev yolları**
  istiyor. *Karar:* `Media` toplu detay okuması sunar ve **`VehicleCatalog`'un sorgusu
  sunucuda birleştirir**; panel tek HTTP çağrısı yapar. *Gerekçe:* istemci tarafı
  birleştirme varlık başına bir istek (N+1) ve sıralama/birleştirme kuralını tarayıcıya
  taşır; `docs/architecture.md` § 2.A'nın saydığı meşru okuma tam bu. *Bedel:*
  `Media.Contracts` D5'in istediğinden **bir okuma fazla** kazanıyor. *Elenen alternatif:*
  panelin `GET /media/assets/{id}`'yi varlık başına çağırması — sözleşme daha dar, gecikme
  daha kötü.
- **D11 — `EnsurePublishable` hazırlık bilgisini PARAMETRE olarak alır:**
  `EnsurePublishable(IReadOnlySet<Guid> readyMediaAssetIds)`. *Gerekçe:* `Domain` dış
  dünyayı bilmez (`docs/conventions.md` § 4) ve `Media`'yı hiç bilemez; `0011`'in
  `MediaAsset.HasAttemptsLeft(max)` deseni birebir aynı. *Ölçüldü:* metodun **tek** çağıranı
  `VehicleTests.cs:26`; başka çağıran yok, yani imza değişikliği tek testi etkiliyor ve
  **D6** bunu zaten kabul etti. *Bedel:* yok.
- **D12 — Yazma yüzeyi TAM LİSTE `PUT`.** `PUT {admin}/vehicles/{id}/media` sıralı kimlik
  listesinin tamamını alır ve yerine koyar. *Gerekçe:* R1 + R2 + R3 tek **idempotent**
  işleme iner ve kapak **sıranın sonucu** olur (**D2**); ayrı bir "kapak yap" eylemi, D2'nin
  reddettiği ikinci kaynağı geri getirirdi. *Bedel:* istemci her kaydetmede listenin
  tamamını gönderir ve iki personel eşzamanlı düzenlerse **son yazan kazanır** (→ D13).
- **D13 — Bu dilimde YENİ eşzamanlılık mekanizması eklenmez.** *Ölçüldü:*
  `UpdateVehicleCommand` hiçbir koruma taşımıyor (`SaveChangesAsync`, belirteç yok).
  *Gerekçe:* yalnız medya sıralamasına koruma koymak, **aynı agregatın** iki yazma
  yolundan birini korumalı diğerini korumasız bırakır — tutarsız ve yanıltıcı; kriterlerin
  hiçbiri istemiyor. *Bedel, açıkça:* eşzamanlı iki düzenlemede son yazan kazanır ve
  birinin sıralaması sessizce kaybolur. *Elenen alternatif:* `Vehicle`'a `xmin` belirteci
  (`MediaAsset`'in deseni) — o zaman `UpdateVehicleCommand` da kapsanmalı ve bu `0010b`'nin
  kapsamına dokunmak olurdu.
- **D14 — Bağlamanın DURUM ön koşulu yoktur:** `İşleniyor` ve `Başarısız` varlıklar da
  bağlanabilir. *Gerekçe:* R5 başarısız varlığın panelde görünmesini istiyor ve görünmesi
  için bağlı olması gerekir; **M3** yalnız **yayın koşulunu** `Hazır`a bağlıyor, bağlamayı
  değil. Yasaklamak, işlemenin sürdüğü dakikalarda paneli kullanılamaz kılardı. *Bedel:*
  yalnız başarısız fotoğrafı olan bir araç oluşabilir — ve medya koşulu onu doğru biçimde
  reddeder (dört sınır durumundan biri).
- **D15 — Aynı varlığın iki ayrı araca bağlanması KISITLANMAZ.** *Ölçüldü:*
  `AttachMediaAsset` yalnız **aynı araç içinde** tekilleştiriyor; araçlar arası paylaşımı
  hiçbir şey engellemiyor. *Gerekçe:* **M2** medyayı sahipsiz sayıyor ve paylaşımı
  yasaklamıyor; X4 kütüphane/yeniden kullanım ekranını dışladığı için böyle bir durumu
  **üreten arayüz yok**. *Bedel:* API seviyesinde mümkün kalır. Tekillik istenirse yeni bir
  kriter ve `Media` tarafında "bu varlık bağlı mı" sorusu gerekir — ki o **C2**'yi delerdi.
- **D16 — `alt` metni ucu bütünüyle yerine koyar.**
  `PUT {admin}/media/assets/{id}/alt-text`, gövde locale → metin haritası; **eksik locale
  boş sayılır** (R4). *Gerekçe:* R4 bir dilin boş kalmasını geçerli sayıyor; locale başına
  `PATCH` yüzeyi ikiye katlar ve "boşa çek" ile "dokunma" ayrımını API'ye taşır. *Bedel:*
  istemci her kaydetmede üç dili birlikte gönderir.
- **D17 — Medya yüzeyi AYRI bir ekran ve adresi dördüncü rota parçası:**
  `/admin/vehicles/{id}/media`. *Gerekçe:* düzenleme ekranı `0010b` ile zaten büyük ve medya
  yüzeyinin kendi üç durumu, kendi kaydetme akışı var; sekme yapmak iki kaydetme akışını
  birbirine bağlar. *Bedel:* `parseRoute` bir parça, `AdminView` bir değer kazanır ve
  `0010b`'nin rota testleri gözden geçirilir.
- **D18 — D5'in okumasının ve D6'nın yeni gövdesinin bu dilimde ÜRETİM ÇAĞIRANI YOKTUR.**
  A2'nin koşulunu tüketen `0012b`'dir (**D7**); burada ikisi de yalnız testlerden çağrılır.
  *Gerekçe:* D5 ve D6 ikisini de zorunlu kılıyor ve `0011` X7'nin bedeli birebir buydu —
  *"orada yazılı olmazsa sahipsiz kalır"* diye uyarmıştı. *Bedel:* PR'da **adıyla** yazılır;
  sessiz bırakmak aynı öksüzlüğü üretir.
- **D19 — Vitrin bu dilimde DEĞİŞMEZ.** *Ölçüldü:* `ShowcaseListingProjector.Project`'ın
  tek çağıranı geliştirme seed'i (`ShowcaseSeeder.cs:160`); üretimde vitrin okuma modeline
  yazan hiçbir yol yok. Fotoğraf bağlamak **yazma modelini** değiştirir, vitrini
  değiştirmez. *Gerekçe:* yansıtma yolu `0012b`'nin işi (onun `Modül(ler)` satırı, C12,
  D12) ve **D7** bu bedeli zaten kabul etti. *Bedel:* PR'da yazılmazsa *"fotoğraf ekledim,
  vitrinde görünmüyor"* bir kusur sanılır.

### Doğrulanmış bulgular — 2026-10-11 CLARIFY turu

Kararların dayandığı ölçümler, hepsi `origin/main` (`b99912a`) üzerinde:

- `AttachMediaAsset`'in üretim çağıranı yalnız seed → `ShowcaseSeeder.cs:129`. → **D2**
- `MediaAssetReadyHandler` yazma modeline dokunmuyor → `:61-65`. → Tuzak 4
- `EnsurePublishable` gövdesi `mediaAssetIds.Count > 0` → `Vehicle.cs:293`. → **D6**
- `SetAltText` var, yazma ucu yok → `MediaAsset.cs:180` + `MediaAdminEndpoints.cs:32-59`.
  → **D3**
- Tekrar deneme ucu var → `MediaAdminEndpoints.cs:55`. → **D4**
- `AreAllReadyAsync` "hepsi hazır mı" soruyor → `MediaAssetContracts.cs:55-57`. → **D5**
- `0011` dört maddeyi `0012`'ye yolladı (X3, X7, X8, D16); ADR 0003 Ek 1 **ikisini** sayıyor.
  → **D1**

---

## Acceptance Criteria

Otuz üç kriter, yedi grupta. CLARIFY turunun saydığı sekiz kümeyi ve D1–D19'u kapsar.
Yapılandırmadan ya da yapılandırılmış locale listesinden gelen her değer için kriter
**yapılandırılan değeri** doğrular, spec'e kopyalanmış bir sabiti değil.

### A. Yükleme, bağlama, sıra ve kapak

- **AC-1.** Oturum açmış personel panelden tek bir fotoğraf yükler ve varlık `yüklendi` durumunda oluşur.
- **AC-2.** Personel yüklenmiş bir fotoğrafı araca bağlar; aracın bağlı listesi onu içerir.
- **AC-3.** Bağlı fotoğraflar personelin verdiği sırada durur ve sıra kalıcıdır.
- **AC-4.** Sıralı listenin ilk elemanı kapak görselidir; ayrı bir kapak alanı yoktur.
- **AC-5.** Sıra değiştiğinde kapak da değişir.
- **AC-6.** Personel bağlı bir fotoğrafı çıkarabilir; aracın listesi onu artık içermez.
- **AC-7.** Çıkarmak varlığı silmez: varlık `Media`'da durur ve türevleri okunabilir.
- **AC-8.** Aynı varlık bir araca iki kez bağlanamaz.
- **AC-9.** Var olmayan bir varlık bağlanamaz; istek çevrilmiş metin değil `errorCode` döner.
- **AC-10.** Oturumu olmayan biri bağlama, sıralama veya çıkarma yapamaz.

### B. `alt` metni

- **AC-11.** Personel bir fotoğrafın `alt` metnini etkin her dilde yazabilir.
- **AC-12.** Bir dili boş bırakmak geçerli bir durumdur; varlık yine okunabilir.
- **AC-13.** `alt` metni `Media`'da saklanır; `VehicleCatalog` metnin kopyasını tutmaz.
- **AC-14.** Etkin olmayan bir locale için `alt` metni yazılamaz.
- **AC-15.** Oturumu olmayan biri `alt` metni yazamaz.

### C. Başarısız varlık ve tekrar deneme

- **AC-16.** İşlemesi başarısız olmuş fotoğraf panelde ayırt edilir.
- **AC-17.** Personel tekrar dene eylemini çağırır ve varlık **aynı kimlikle** yeniden kuyruğa alınır.
- **AC-18.** Tekrar deneme otomatik tetiklenmez; eylem çağrılmadan durum kendiliğinden değişmez.

### D. Medya koşulu — dört sınır durumu

- **AC-19.** Hiç bağlı varlığı olmayan araç medya koşulunu karşılamaz.
- **AC-20.** Bağlı varlıklarının hepsi `işleniyor` olan araç medya koşulunu karşılamaz.
- **AC-21.** Biri `hazır`, ikisi `işleniyor` olan araç medya koşulunu **karşılar**.
- **AC-22.** Bağlı varlıklarının hepsi `başarısız` olan araç medya koşulunu karşılamaz.
- **AC-23.** `Media`'nın okuma yüzeyi verilen kümeden **hazır olanların** kümesini döndürür.
- **AC-24.** Aracın medya sorgusu sıralı listeyi `Media`'nın durum, `alt` ve türev bilgisiyle birleştirir.

### E. Ekran durumları

- **AC-25.** Medya ekranı yükleniyor durumunda iskelet gösterir.
- **AC-26.** Bağlı fotoğrafı olmayan araçta boş durum neden boş olduğunu ve ne yapılabileceğini söyler.
- **AC-27.** Okuma başarısız olduğunda hata durumu anlamlı mesaj ve **tekrar dene** eylemi gösterir.
- **AC-28.** `işleniyor` bir varlık ekranda ayrı bir hâl olarak görünür ve ekranın yükleme iskeletiyle karışmaz.
- **AC-29.** Ekranda hiçbir teknik detay görünmez: durum kodu, `errorCode`, `traceId`, yol.

### F. Çok dil ve RTL

- **AC-30.** Ekranın metinleri TR / AR / EN üçünde de vardır; eksik çeviri anahtarı yoktur.
- **AC-31.** Arapça'da ekran RTL yönünde çalışır.

### G. Sınır

- **AC-32.** `Media` hiçbir modüle senkron çağrı yapmaz; bir fotoğrafın hangi araca bağlı olduğu `Media`'da tutulmaz.
- **AC-33.** `VehicleCatalog`'un `Media`'ya erişimi yalnız `Media.Contracts` üzerinden ve tek atlamadır.

---

## Definition of Done

<!-- MADDE ÇIKARILAMAZ. Şablondaki her maddenin bir `<!-- DoD:Tn -->` işaretleyicisi vardır
     ve mimari test (ADR 0008 / K8) sayıyı şablondan okuyarak denetler. -->

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi (D1–D8 CLARIFY turu, **D9–D19** kriter turu; açık soru kalmadı).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012a-ac<n>.png`),
      boş ve hata durumları dâhil, RTL için Arapça görüntü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor.
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
- [ ] Medya koşulunun **dört** sınır durumu ayrı ayrı testle gösterildi (Tuzak 1).
- [ ] `EnsurePublishable`'ın yeni şartı ile `0010a`'nın agregat testi birlikte güncellendi
      ve bu PR açıklamasında **adıyla** yazıldı (**D6**).
- [ ] `alt` metni yazma ucunun `Media` modülünde açıldığı ve `VehicleCatalog`'un metnin
      kopyasını tutmadığı testle gösterildi (**C5**, **D3**).

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
