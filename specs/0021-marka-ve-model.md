# 0021 — Marka ve Model: Taksonomi, Panel ve Vitrinde Marka

| | |
|---|---|
| **Durum** | Taslak — kriterler yazıldı, QA CLARIFY turunu bekliyor |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleReference` (marka varlığı, model→marka bağı, yazma uçları, migration) · `VehicleCatalog` (vitrin okuma modelinde marka, liste marka ekseni, ana sayfa form markaları) · `frontend/` panel (marka/model ekranı, araç formunda marka → model) ve vitrin (kart, detay, liste filtresi, ana sayfa formu) |
| **Bağımlı spec'ler** | `0010a` (araç kaydı — tamamlandı; **X14**'ü bu karttır) · `0014` (ana sayfa — tamamlandı; **X13**/**X11**'in bir kısmını bu kart karşılar) · `0015` (liste eksenleri — tamamlandı; **X8** marka ekseni bu karttır) · `0012b` (ilan adresi kanonik addan bir kez üretilir — **D13**) · `0020` (sıra) |
| **Dayanak** | Depo sahibinin 2026-11-18 isteği ve kararları (**D1**) · `0010a` **X14** · `0015` **X8** · `0014` **X13** |
| **Karar bekliyor** | yok — **D1–D14** verildi (§ Verilen kararlar). QA CLARIFY turu bekleniyor. |

---

## Intent

Galeride araçlar **markasız** duruyor. Sistem "Toyota Corolla"yı tek parça bir model adı
olarak biliyor; "Toyota" diye bir şey bilmiyor. Bunun bugünkü sonuçları:

- **Ziyaretçi markaya göre arayamıyor.** Araç aramanın gerçek eksenlerinden biri marka; ne
  listede ne ana sayfadaki "Aracınızı bulun" formunda marka seçilebiliyor.
- **Galeri model ekleyemiyor.** Model listesi kurulumda yazılmış on iki kayıt; listede olmayan
  bir aracı (ör. bir Fiat Egea) sisteme girmenin yolu yok.
- **Galeri ana sayfada hangi markaları öne çıkaracağını seçemiyor.**

Bunu **galeri yöneticisi** (marka ve model eklemek, öne çıkacak markaları seçmek) ve
**ziyaretçi** (markaya göre daraltmak) istiyor. Başarı şuna benziyor: yönetici panelde marka
ve o markanın modellerini ekler; araç girerken önce markayı sonra modeli seçer; ziyaretçi ana
sayfadaki formda yönetici tarafından seçilmiş markalardan birini işaretleyip o markanın
araçlarına, listede de herhangi bir markaya göre daraltır.

---

## Requirements

### Marka ve model

- **R1.** Sistem **markaları** tutar. Her markanın bir **kanonik adı** (Latin, zorunlu,
  galeri genelinde tekil), dil başına opsiyonel **görünen adı** ve bir **adres kısaltması**
  vardır (**D6**, **D10**).
- **R2.** Her **model** tam olarak bir **markaya** aittir. Modelin kanonik adı markasızdır
  ("Corolla") ve **aynı marka içinde** tekildir; dil başına opsiyonel görünen adı vardır (**D2**,
  **D11**).
- **R3.** Görünen adı olmayan bir dilde marka ve model kanonik adıyla gösterilir.
- **R4.** Bugünkü on iki model kurulum güncellemesinde markasına ayrılır: "Toyota Corolla" →
  marka **Toyota**, model **Corolla**. Bu ayrılma hiçbir aracın modelini değiştirmez ve hiçbir
  ilanın adresini değiştirmez (**D3**).

### Panelde yönetim

- **R5.** Yönetici panelde markaları ve her markanın modellerini görür, **yeni marka** ve bir
  markaya **yeni model** ekler, adlarını düzenler (**D4**).
- **R6.** Marka ve model **silinmez**; **pasif** yapılır ve yeniden etkinleştirilebilir. Pasif
  marka ya da model araç formunda yeni seçim olarak sunulmaz; ona bağlı var olan araçlar ve
  ilanlar etkilenmez (**D5**). Pasif bir markanın modelleri de yeni seçim olarak sunulmaz.
- **R7.** Yönetici bir markayı **"ana sayfada göster"** olarak işaretler ya da işaretini kaldırır
  (**D7**).

### Araç kaydı

- **R8.** Panelin araç formunda personel önce **markayı**, sonra **o markanın modellerinden**
  birini seçer. Marka değiştirildiğinde seçili model temizlenir.
- **R9.** Var olan bir aracın düzenleme formu aracın markasını ve modelini seçili gösterir —
  marka ya da model pasif olsa bile.

### Vitrinde marka

- **R10.** Vitrinde ilan kartı, ilan detayı ve öne çıkan araçlar aracı **marka + model** olarak
  gösterir ("Toyota Corolla"), ziyaretçinin dilindeki görünen adlarla (**D12**).
- **R11.** Vitrin listesinde **marka** bir daraltma eksenidir: ziyaretçi bir ya da birden çok
  marka seçer; eksen içinde "veya", diğer eksenlerle "ve" geçerlidir (`0015` **R2**). Eksen
  **en az bir yayındaki ilanı olan** markaları sunar (**D9**).
- **R12.** Marka seçimi adres satırında taşınır (`/{locale}/cars?brand=toyota`); paylaşılan
  bağlantı aynı daraltmayı açar ve bilinmeyen bir marka değeri yok sayılır (`0015` deseni).
- **R13.** Ana sayfanın **"Aracınızı bulun"** formunda, yöneticinin **"ana sayfada göster"**
  işaretlediği ve **en az bir yayındaki ilanı olan** markalar seçilebilir; birden çok seçilebilir.
  Formun düğmesi seçime uyan araç sayısını gösterir ve listeyi seçimle açar (**D8**).
- **R14.** Ana sayfada gösterilecek marka yoksa formun marka alanı görünmez; form kasa tipiyle
  bugünkü gibi çalışır.

### Erişim, durumlar

- **R15.** Marka ve model yönetimi **yalnız Yönetici** içindir; diğer roller ekrana bağlantı
  görmez, adresi doğrudan açınca "erişim yok" ekranını görür, yazma uçları `403` döner (**D4**).
  Araç formundaki marka ve model okuması bugünkü model okuması gibi her personel rolüne açıktır.
- **R16.** Yeni ekran ve alanlar yükleniyor, boş ve hata durumlarını ele alır; üç dilde ve
  Arapçada sağdan sola çalışır.

---

## Constraints

### Uyulması zorunlu

- **C1.** Marka ve model `VehicleReference`'a aittir; `VehicleCatalog` onları yalnız
  `VehicleReference.Contracts` üzerinden okur (Yasak Listesi 1). Vitrin okuma modeli markayı
  bugünkü model adı gibi **kendi kopyası** olarak taşır (`docs/architecture.md` § 2.E).
- **C2.** İlan adresleri (slug) değişmez: ilk yayında kanonik **model** adından üretilmiş bir
  adres bu kartla yeniden üretilmez (`0012b` **D13**). Yeni ilanların adresi marka + model
  kanonik adından üretilir ("toyota-corolla-2021-…") — bugünkü adreslerin biçimiyle aynı (**D3**).
- **C3.** Kanonik ad ve kısaltma ASCII'dir; karşılaştırma ve tekillik büyük-küçük harf
  duyarsızdır ve Türkçe `İ/ı` kuralına göre değil, değişmez kültürle yapılır (Yasak Listesi 27).
- **C4.** Migration'lar yalnız kendi şemasına dokunur (`reference` ve `catalog` ayrı
  migration'lar). Marka ayrılması **sabit bir eşleme listesiyle** yapılır — her model için
  kanonik ad ve her dildeki görünen ad marka ve model parçasına elle yazılır — "ilk boşluk"
  kuralıyla değil (**D3**). *(Neden: "Mercedes-Benz C-Serisi" boşlukla değil, Arapça görünen
  adlar hiçbir kuralla bölünemez.)*
- **C5.** Liste ve ana sayfa marka ekseni performans kapısının eşiklerini korur (`0014`, `0015`).
- **C6.** Kullanıcıya görünen hiçbir metin koda gömülmez; uçlar hata kodu döner (Altın Kural 6).

### KAPSAM DIŞI

- **X1.** **Model ekseni** listede — *neden:* istek markaydı; model ekseni marka seçimine bağlı
  ikinci bir seçici ister ve ayrı bir karardır.
- **X2.** **Marka sayfası ve marka logosu** — `0014` **X1**; görsel ve SEO kararları ayrı.
- **X3.** **Ana sayfada marka bölümü ve marka başına araç sayısı** — `0014` **X13**; bu kart formu
  karşılar, ayrı bölüm açmaz.
- **X4.** **Dış kaynaktan marka/model içe aktarma ve eşanlamlılar** — `docs/architecture.md` § 1
  bunları modüle yazıyor; bu kart elle girişi getirir.
- **X5.** **Marka/model silme** — *neden:* geçmiş araç ve ilanların referansıdır (**D5**).
- **X6.** **Ana sayfa marka sırasının elle belirlenmesi** — sıra kanonik ada göre alfabetiktir (**D7**).
- **X7.** **Satış Danışmanı'nın model ekleyebilmesi** — **D4**.

---

## Context

### Mevcut davranış — ölçüldü (2026-11-18, `main` @ `0cd834d`)

- **Marka kodda yok.** `VehicleReference/Domain/ReferenceEntities.cs`: `VehicleModel` yalnız
  `Id`, `CanonicalName` ve çevirileri taşıyor. Uçlar yalnız okuma: `GET /api/reference/models`,
  `/body-types` ve terim uçları; yazma ucu yok.
- **Modeller kurulumda sabit.** `SeedInitialVehicleModels` migration'ı **on iki** model yazıyor,
  **dokuz** markaya ait: Toyota Corolla, Toyota Yaris, Volkswagen Passat, Volkswagen Golf,
  Renault Clio, Renault Megane, Ford Focus, Hyundai i20, Honda Civic, Nissan Qashqai,
  **BMW 3 Serisi**, **Mercedes-Benz C-Serisi** (kanonik adlar; son ikisi Türkçe yazılmış).
- **Çeviriler marka ile modeli tek parça tutuyor.** Her model için `tr`/`en`/`ar` görünen adı
  var ve hepsi tam ad: `ar` "تويوتا كورولا", "بي إم دبليو الفئة الثالثة", "مرسيدس بنز الفئة C";
  `en` "BMW 3 Series", "Mercedes-Benz C-Class". Ayrılma yalnız kanonik adı değil **her dildeki
  görünen adı** da marka ve model parçasına bölmek zorunda; Arapçada hiçbir genel kural bunu
  yapamaz — **C4**'ün sabit eşlemesi dil başına yazılır.
- **Catalog modeli kimlikle tutuyor.** `Vehicle.VehicleModelId`; vitrin okuma modeli
  `vehicle_model_id` taşıyor (`ShowcaseListingConfiguration.cs:30`) ve adı
  `IVehicleModelNameResolver` ile `VehicleReference.Contracts` üzerinden çözüyor.
- **İlan adresi kanonik model adından** (`ListingSlug.Create(canonicalModelName, …)`,
  `0012b` **D13**). Bugünkü kanonik ad markayı içerdiği için adresler zaten "toyota-corolla-…"
  biçiminde; ayrılmadan sonra adres marka + model kanonik adından üretilirse biçim aynı kalır.
- **Liste eksenleri** `ShowcaseListingQueries.cs`'te ardışık `Where` ile (eksen içi `Contains`).
- **Ana sayfa formu** `frontend/src/features/catalog/home/NarrowingForm.tsx` — bugün yalnız kasa
  tipi.
- **Mimari belge** `VehicleReference`'ı "iş kuralı yok, dışarıdan beslenir" diye tanımlıyor
  (`docs/architecture.md` § 1). Sahibin "panelden eklenir" kararı bunu değiştirir; belge
  güncellenir (**D4**, DoD).

### Verilen kararlar — 2026-11-18

**D1 — Sahibin kararları (depo sahibi, 2026-11-18).** Araçlar marka + model taşır; marka ve
model listesi **panelden eklenir**, hazır başlangıç listesiyle; araç formunda önce marka sonra
model; "Aracınızı bulun" formunda **panelden seçilen** markalar görünür. Sıra: `0020`'den sonra.

**D2 — Marka `VehicleReference`'ta, model markaya bağlı (Takım Yöneticisi (ajan) kararı).**
*Gerekçe:* `docs/architecture.md` § 1 taksonomiyi bu modüle yazıyor; araç kaydı modeli zaten
oradan alıyor. Araç **modeli** tutmaya devam eder; marka modelden türer — araçta ayrı bir marka
alanı, model ile çelişebilecek ikinci bir doğru kaynak olurdu.

**D3 — Var olan modeller sabit bir eşlemeyle ayrılır; adresler değişmez (ajan).** (R4, C2, C4)
*Gerekçe:* on iki kayıt bilinen bir liste; "ilk boşluk" kuralı genel değildir. Adres kalıcıdır
(`0012b` **D13**) ve paylaşılmış bağlantılar kırılmamalı.

**D4 — Yönetim yalnız Yönetici (ajan).** (R5, R15, X7) *Gerekçe:* `0020` **D2** ile tutarlı;
marka/model kurulum düzeyinde referans veridir ve vitrinin filtrelerini doğrudan etkiler.
`docs/architecture.md` § 1'in "dışarıdan beslenir" cümlesi "panelden ve kurulum verisiyle
beslenir" olur.

**D5 — Silme yok, pasif var (ajan).** (R6, X5) *Gerekçe:* geçmiş araçlar ve ilanlar modele
bağlı; yanlış girilmiş bir kaydı yeni seçimlerden kaldırmak yeter.

**D6 — Kanonik Latin ad + dil başına görünen ad (ajan).** (R1, R2, R3) `docs/architecture.md`
§ 1'in modeli. Arapça sitede "تويوتا" gösterilebilir; adres ve tekillik kanonik addan.

**D7 — "Ana sayfada göster" marka üzerinde bir işaret; sıra alfabetik (ajan).** (R7, X6)

**D8 — Ana sayfa formunda çoklu seçim, düğmede sayı (ajan).** (R13) Kasa tipinin bugünkü
deseni (`0014`); seçim listeye `brand` parametresiyle taşınır.

**D9 — Eksen yalnız stoğu olan markaları sunar (ajan).** (R11, R13) *Gerekçe:* `0015`'in kasa,
yakıt ve vites eksenleri kapalı ve kısa kümelerdir; marka kümesi galerinin eklemesiyle büyür.
Stoğu olmayan markayı sunmak ziyaretçiyi boş sonuca götürür.

**D10 — Marka kısaltması kanonik addan bir kez üretilir ve değişmez (ajan).** (R1, R12)
*Gerekçe:* kısaltma paylaşılan liste bağlantısının parçasıdır; kanonik ad düzeltilse bile
bağlantı çalışmaya devam etmeli. Kanonik ad ASCII olduğu için üretim kuralı basittir (küçük
harf, boşluk → `-`).

**D11 — Model tekilliği marka içinde (ajan).** (R2) İki markanın "Civic" adlı modeli olabilir.

**D12 — Vitrin "marka + model" gösterir (ajan).** (R10) Bugünkü görünüm ("Toyota Corolla")
korunur; fark, iki parçanın ayrı ayrı çevrilebilmesi.

**D13 — Okuma modeli markanın kimliğini, kısaltmasını ve adını taşır (ajan).** (C1, R11) Marka
ekseni ve kart başlığı başka modülün tablosuna dokunmadan çalışır; marka adı değişince okuma
modeli `0003`'ün şube adı desenindeki gibi bir duyuruyla güncellenir — ya da plan turu
gerekçesiyle başka bir yolu seçer. Şart: ad değişikliği vitrine yansır.

**D14 — Model başka markaya taşınamaz (ajan).** *Gerekçe:* aracın markası modelden türüyor; model
taşınırsa geçmiş araçların markası sessizce değişir. Yanlış markaya eklenmiş model pasif
yapılır, doğru markaya yenisi eklenir.

---

## Acceptance Criteria

### Kurulum güncellemesi (R4 · C2, C4 · D3)

- **AC-1.** Güncellemeden sonra on iki model dokuz markaya bağlıdır: BMW (3 Serisi), Ford (Focus), Honda (Civic), Hyundai (i20), Mercedes-Benz (C-Serisi), Nissan (Qashqai), Renault (Clio, Megane), Toyota (Corolla, Yaris), Volkswagen (Golf, Passat).
- **AC-2.** Güncellemeden sonra her modelin kanonik adı markasızdır ("Corolla", "3 Serisi", "C-Serisi").
- **AC-3.** Güncellemeden önce var olan araçların hepsi aynı model kimliğini taşır.
- **AC-4.** Güncellemeden önce yayında olan ilanların adresleri değişmez.
- **AC-5.** Güncellemeden önce ve sonra vitrin her ilan için **aynı tam adı** gösterir, üç dilde de: Türkçe "BMW 3 Serisi", İngilizce "BMW 3 Series", Arapça "تويوتا كورولا" ve "مرسيدس بنز الفئة C".

### Marka yönetimi (R1, R5, R6, R7 · D4–D7, D10)

- **AC-6.** Yönetici marka ekranında bütün markaları — aktif ve pasif — kanonik adları, durumları ve "ana sayfada göster" işaretleriyle görür.
- **AC-7.** Yönetici kanonik adı "Fiat" olan bir marka eklediğinde marka listede aktif görünür ve kısaltması `fiat` olur.
- **AC-8.** Var olan bir markanın kanonik adıyla — büyük-küçük harf farkı gözetmeden ("toyota") — marka eklemek reddedilir ve hata adın zaten kullanıldığını söyler.
- **AC-9.** ASCII olmayan ya da boş bir kanonik ad ("Škoda", "") reddedilir.
- **AC-10.** "Alfa Romeo" kanonik adlı markanın kısaltması `alfa-romeo` olur.
- **AC-11.** Markanın kanonik adı düzeltildiğinde kısaltması değişmez.
- **AC-12.** Yönetici bir markaya Arapça görünen ad ("فيات") verdiğinde Arapça vitrin onu gösterir; Türkçe vitrin kanonik adı gösterir.
- **AC-13.** Pasif yapılan marka araç formunun marka seçicisinde yeni seçim olarak görünmez.
- **AC-14.** Pasif yapılan markanın ilanları vitrinde kalır ve marka ekseninde süzülebilir.
- **AC-15.** Pasif marka yeniden etkinleştirildiğinde araç formunda yeniden seçilebilir.

### Model yönetimi (R2, R5, R6 · D11, D14)

- **AC-16.** Yönetici "Fiat" markasına "Egea" modelini eklediğinde model o markanın altında aktif görünür.
- **AC-17.** Aynı markada aynı kanonik adla ikinci bir model reddedilir.
- **AC-18.** Farklı markalarda aynı kanonik adlı modeller eklenebilir.
- **AC-19.** Modelin markası değiştirilemez: düzenleme ekranında marka seçimi sunulmaz ve uç markayı değiştiren isteği reddeder.
- **AC-20.** Pasif yapılan model araç formunda yeni seçim olarak sunulmaz; o modelle kayıtlı araçlar ve ilanlar değişmez.
- **AC-21.** Pasif bir markanın aktif modelleri de araç formunda yeni seçim olarak sunulmaz.

### Araç formu (R8, R9)

- **AC-22.** Yeni araç formunda model seçicisi bir marka seçilene kadar devre dışıdır.
- **AC-23.** Marka seçildiğinde model seçicisi yalnız o markanın aktif modellerini sunar.
- **AC-24.** Seçili model varken marka değiştirildiğinde model seçimi temizlenir.
- **AC-25.** Pasif bir modelle kayıtlı aracın düzenleme formu markayı ve modeli seçili gösterir ve araç model değiştirilmeden kaydedilebilir.
- **AC-26.** Yeni kayıtta pasif bir model sunucuda reddedilir.

### Vitrin (R3, R10 · D12, D13)

- **AC-27.** İlan kartı, ilan detayı ve öne çıkan araçlar aracı "Toyota Corolla" biçiminde, marka ve model görünen adlarıyla gösterir.
- **AC-28.** Görünen adı olmayan bir dilde marka ve model kanonik adlarıyla gösterilir.
- **AC-29.** Yönetici bir markanın görünen adını değiştirdikten sonra vitrin yeni adı gösterir.
- **AC-30.** Yeni yayına alınan bir Fiat Egea ilanının adresi `fiat-egea-<yıl>-…` biçimindedir.

### Liste marka ekseni (R11, R12 · C5 · D9)

- **AC-31.** Liste filtresinde marka ekseni yalnız en az bir yayındaki ilanı olan markaları sunar.
- **AC-32.** Toyota seçildiğinde liste yalnız Toyota ilanlarını gösterir ve toplam sayı ona göre değişir.
- **AC-33.** Toyota ve Honda birlikte seçildiğinde liste iki markanın ilanlarını gösterir.
- **AC-34.** Marka ve kasa tipi birlikte seçildiğinde liste ikisine de uyan ilanları gösterir.
- **AC-35.** Seçim adres satırında `brand=toyota` olarak görünür; bu adres yeni bir sekmede açıldığında aynı daraltma gelir.
- **AC-36.** Bilinmeyen bir marka değeri (`brand=yok`) yok sayılır ve liste hata vermez.
- **AC-37.** Arapça listede marka ekseni Arapça görünen adları gösterir ve sağdan sola düzende çalışır.

### Ana sayfa formu (R13, R14 · D8, D9)

- **AC-38.** "Aracınızı bulun" formu yalnız "ana sayfada göster" işaretli ve en az bir yayındaki ilanı olan markaları sunar.
- **AC-39.** İşaretli ama yayında ilanı olmayan bir marka formda sunulmaz.
- **AC-40.** İşaretsiz bir markanın yayındaki ilanı olsa da formda sunulmaz.
- **AC-41.** Formda Toyota seçildiğinde düğme Toyota ilanlarının sayısını gösterir ve tıklanınca `/{locale}/cars?brand=toyota` açılır.
- **AC-42.** Formda marka ve kasa tipi birlikte seçildiğinde düğme ikisine uyan sayıyı gösterir ve liste ikisiyle açılır.
- **AC-43.** Gösterilecek marka yoksa formun marka alanı hiç görünmez ve form kasa tipiyle çalışır.

### Erişim ve durumlar (R15, R16 · D4)

- **AC-44.** Satış Danışmanı ve İçerik Editörü panelde marka/model ekranına bağlantı görmez; adresi doğrudan açtıklarında "erişim yok" ekranını görür.
- **AC-45.** Yönetici olmayan rolün marka ve model yazma uçlarına isteği `403` alır.
- **AC-46.** Satış Danışmanı araç formunda markaları ve modelleri okuyabilir.
- **AC-47.** Marka/model ekranı veri gelirken yükleniyor, okunamazken teknik ayrıntı göstermeyen hata durumu ve yeniden deneme gösterir; hiç marka yokken boş durumu gösterir.
- **AC-48.** Yeni metinler TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.
- **AC-49.** Arapça panelde marka/model ekranı sağdan sola çalışır; kanonik ad alanı soldan sağa yazılır.
- **AC-50.** Ana sayfa ve liste için performans kapısı yeşil kalır.

---

## Definition of Done

- [ ] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0021-ac<n>.png`),
      boş ve hata durumları dâhil; Arapça görüntü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration'lar yalnızca kendi şemasına dokunuyor (`vehicle_reference`, `catalog`).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**marka**, **pasif** referans kaydı).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi; § 1'in `VehicleReference` tanımı **D4**'e göre düzeltildi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [ ] `0010a` X14, `0015` X8 ve `0014` X13'e "→ `0021`" notu düşüldü.

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
<!-- 1-2 cümle. Bir sonraki spec'i nasıl daha iyi yazarız? -->
