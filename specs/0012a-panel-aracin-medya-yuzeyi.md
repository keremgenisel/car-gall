# 0012a — Panel: Aracın Medya Yüzeyi

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (komut yüzeyi) · `Media` (yalnız `alt` metni yazma ucu) + `Media.Contracts` (okuma) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0010a` + `0010b` (araç kaydı, düzenleme ve liste — tamamlandı) · `0011` (Media — tamamlandı) |
| **Dayanak** | ADR 0003 Ek 1, sıra #5'in **birinci dilimi** (→ **D1**) · `0011` § X3, § X7, § X8 ve **D16** |
| **Karar bekliyor** | yok — `0012`'nin 2026-10-11 CLARIFY turunda sekiz karar verildi (**D1–D8**). Kabul kriterleri ayrı bir turda yazılır. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri henüz yazılmadı; kararların hepsi verilmiş
> durumdadır ve yalnız kriter turu bekliyor. **Bu dosya `0012`'nin ikiye bölünmesiyle
> doğdu** — gerekçesi ve bedeli **D1**'de yazılı.

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

> **Kriterler ayrı bir turda yazılacak** — kararların hepsi verilmiş durumda (D1–D8) ve
> kriter yazımı bu dosyanın bir sonraki revizyonudur. En az şu kümeleri kapsayacak:
>
> bağlama ve **sıra** · kapağın sıralı listenin ilk elemanı olması · çıkarmanın varlığı
> silmemesi · `alt` metninin üç dilde yazılması ve bir dilin boş kalabilmesi · başarısız
> varlığın ayırt edilmesi ve tekrar denemenin **aynı kimlikle** dönmesi · medya koşulunun
> **dört** sınır durumu (hiç varlık yok · hepsi işleniyor · biri hazır ikisi işleniyor ·
> hepsi başarısız) · üç ekran durumu ve "işleniyor" hâli · çok dil ve RTL.

---

## Definition of Done

<!-- MADDE ÇIKARILAMAZ. Şablondaki her maddenin bir `<!-- DoD:Tn -->` işaretleyicisi vardır
     ve mimari test (ADR 0008 / K8) sayıyı şablondan okuyarak denetler. -->

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi (D1–D8).
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
