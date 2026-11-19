# 0021 — Marka ve Model: Taksonomi, Panel ve Vitrinde Marka

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-18 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (B1–B11 → **D15–D24**) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleReference` (marka varlığı, model→marka bağı, yazma uçları, migration) · `VehicleCatalog` (liste marka ekseni ve ana sayfa form markaları — okuma modeli **değişmez**, **D15**) · `Leads`/`Notifications` (yalnız davranış korunur: talep kopyası ve e-posta tam adı taşır — **D17**) · `frontend/` panel (marka/model ekranı, araç formunda marka → model) ve vitrin (kart, detay, liste filtresi, ana sayfa formu) |
| **Bağımlı spec'ler** | `0010a` (araç kaydı — tamamlandı; **X14**'ü bu karttır) · `0014` (ana sayfa — tamamlandı; **X13**/**X11**'in bir kısmını bu kart karşılar) · `0015` (liste eksenleri — tamamlandı; **X8** marka ekseni bu karttır) · `0012b` (ilan adresi kanonik addan bir kez üretilir — **D13**) · `0020` (sıra) |
| **Dayanak** | Depo sahibinin 2026-11-18 isteği ve kararları (**D1**) · `0010a` **X14** · `0015` **X8** · `0014` **X13** |
| **Karar bekliyor** | yok — **D1–D14**; QA CLARIFY turunun on bir bulgusu (B1–B11) → **D15–D24**. |

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
  `VehicleReference.Contracts` üzerinden okur (Yasak Listesi 1). Vitrin okuma modeli **bugünkü
  gibi yalnız model kimliğini** taşır; marka ve adlar okuma anında sözleşmeden çözülür — model
  adının bugünkü deseni (**D15**).
- **C2.** İlan adresleri (slug) değişmez: ilk yayında kanonik **model** adından üretilmiş bir
  adres bu kartla yeniden üretilmez (`0012b` **D13**). Yeni ilanların adresi marka + model
  kanonik adından üretilir ("toyota-corolla-2021-…") — bugünkü adreslerin biçimiyle aynı (**D3**).
- **C3.** Kanonik ad ve kısaltma ASCII'dir. **Tekillik denetimi** büyük-küçük harf duyarsızdır
  ve değişmez kültürle yapılır. Adres parametresi (`brand=`) ise `0015` **C12** gereği **tam ve
  harfe duyarlı** eşleşir (**D20**).
- **C4.** Migration yalnız `vehicle_reference` şemasına dokunur; `catalog` şeması bu kartta
  değişmez (**D15**). Marka ayrılması **sabit bir eşleme listesiyle** yapılır — her model için
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
- **Catalog modeli kimlikle tutuyor, adı okuma anında çözüyor.** `Vehicle.VehicleModelId`;
  vitrin okuma modeli yalnız `vehicle_model_id` taşıyor (`ShowcaseListingConfiguration.cs:30`);
  kart adı her istekte `IVehicleModelNameResolver` ile çözülüyor (`ShowcaseCards.cs:18`) ve panel
  listesi de adı saklamıyor (`VehicleAdminQueries.cs:190`, Yasak Listesi 33). Kasa tipi filtresi
  kanonik kümeyi sözleşmeden okuyor (`ShowcaseListingQueries.cs:21`).
- **Kanonik adın başka tüketicileri** (QA B3): yeni talebin araç kopyası
  (`CanonicalModelNameSource.cs:16`, `VehicleSnapshot.cs:42`), bildirim e-postasının `modelAdi`
  alanı (`NotificationDispatcher.cs:132`), panel araç listesi (`VehicleAdminQueries.cs:191`) ve
  ilan adresi üretimi (`ListingTransitionCommands.cs:205`). Hepsi bugün "Toyota Corolla" alıyor.
- **Ana sayfa düğmesindeki sayı seçimle değişmiyor** — `0014` AC-41, `NarrowingForm.tsx:9`;
  toplam yükleme anında bir kez okunuyor (`HomeCatalog.tsx:53`).
- **Referans okuma uçları oturum istemiyor** (`ReferenceEndpoints.cs:13-15`) ve kanonik ada
  düşüyor (`ReferenceContracts.cs:26`); `docs/architecture.md:241` ise "zorunlu çeviri; eksikse
  admin uyarısı" diyor — belge koddan geride.
- **Geliştirme tohumu tek markalı.** `VehicleReferenceModule.cs:50` "Toyota Corolla" yazıyor,
  `ShowcaseSeeder.cs:185` adres metnini sabitliyor.
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

**D8 — Ana sayfa formunda çoklu seçim; düğmedeki sayı değişmez (ajan, D18 ile düzeltildi).**
(R13) Kasa tipinin bugünkü deseni (`0014` AC-41): düğme toplam yayındaki araç sayısını gösterir,
seçimle değişmez; seçim listeye `brand` parametresiyle taşınır.

**D9 — Eksen yalnız stoğu olan markaları sunar (ajan).** (R11, R13) *Gerekçe:* `0015`'in kasa,
yakıt ve vites eksenleri kapalı ve kısa kümelerdir; marka kümesi galerinin eklemesiyle büyür.
Stoğu olmayan markayı sunmak ziyaretçiyi boş sonuca götürür.

**D10 — Marka kısaltması kanonik addan bir kez üretilir, değişmez ve tekildir (ajan, D19 ile
genişletildi).** (R1, R12) *Gerekçe:* kısaltma paylaşılan liste bağlantısının parçasıdır;
kanonik ad düzeltilse bile bağlantı çalışmaya devam etmeli. Kural: küçük harf; harf ve rakam
dışındaki her karakter dizisi tek `-` olur; baştaki ve sondaki `-` atılır.

**D11 — Model tekilliği marka içinde (ajan).** (R2) İki markanın "Civic" adlı modeli olabilir.

**D12 — Vitrin "marka + model" gösterir (ajan).** (R10) Bugünkü görünüm ("Toyota Corolla")
korunur; fark, iki parçanın ayrı ayrı çevrilebilmesi.

**D13 — (D15 ile değiştirildi.)** *(Önceki metin: okuma modeli markanın kimliğini, kısaltmasını
ve adını taşır ve ad değişikliği bir duyuruyla yansır. QA B1: model adı okuma modelinde kopya
olarak durmuyor, okuma anında çözülüyor — karar yanlış bir olguya dayanıyordu.)*

**D14 — Model başka markaya taşınamaz (ajan).** *Gerekçe:* aracın markası modelden türüyor; model
taşınırsa geçmiş araçların markası sessizce değişir. Yanlış markaya eklenmiş model pasif
yapılır, doğru markaya yenisi eklenir.

### Verilen kararlar — QA CLARIFY turu, 2026-11-18

QA spec'i `main` @ `0cd834d`'ye karşı okudu, on bir bulgu getirdi (10 gerçek, 1 belirsiz) ve
Context'in sayım iddialarını doğruladı. Hepsi Takım Yöneticisi (ajan) tarafından kabul edildi.

**D15 — Okuma modeli değişmez; marka okuma anında çözülür (B1, B2).** (C1, C4, R10, R11) Marka
ekseni, seçilen markaları sözleşme üzerinden **model kimliği kümesine** çevirip okuma modelini
o kümeyle süzer — kasa tipinin bugünkü yolu. Stoğu olan markalar, yayındaki ilanların model
kimliklerinden sözleşmeyle türetilir. Kart ve detay adı bugünkü gibi okuma anında çözülür.
*Gerekçe:* modelin markası hiç değişmez (D14), dolayısıyla model kimliği markayı taşır; `catalog`
şemasına kolon, eski satırları doldurma işi ve ad değişikliği için duyuru gerekmez. Ad değişikliği
vitrine kendiliğinden yansır (AC-31).

**D16 — (D15'in sonucu.)** Eski satırların doldurulması sorusu ortadan kalkar; yine de
güncellemeden önce yayında olan ilanların marka ekseninde süzülebildiği kriterle ölçülür.

**D17 — Tam kanonik ad sözleşmenin sözüdür (B3).** Bugünkü `ResolveCanonicalNamesAsync` ve
`ResolveNamesAsync` **marka + model** tam adını döndürmeye devam eder ("Toyota Corolla");
markasız model adı ayrı bir sözleşme üyesidir. *Gerekçe:* talep kopyası, bildirim e-postası,
panel listesi ve ilan adresi bugün tam adı kullanıyor; ayrılma onları sessizce markasız ada
geriletmemeli. Eski taleplerin kopyası olduğu gibi kalır (`docs/architecture.md` § 2.E).

**D18 — Düğme sayısı `0014` gibi kalır (B4).** D8 düzeltildi; `0014` AC-41 geçerli.

**D19 — Kısaltma tekil; çakışma ayrı hata (B5).** Kısaltma galeri genelinde tekildir; çakışan
ekleme `brand.slug_taken` gibi ayrı bir kodla reddedilir (ad tekilliğinden ayrı).

**D20 — Adres parametresi harfe duyarlı (B6).** `brand=Toyota` bilinmeyen değerdir ve yok sayılır
(`0015` C12). Tekillik denetimi duyarsız kalır.

**D21 — Seçili marka eksende her zaman görünür (B7).** Adresle gelen, var olan ama stoğu kalmamış
bir marka uygulanır (sonuç boş olabilir) ve eksende işaretli seçenek olarak görünür — ziyaretçi
seçimi kaldırabilsin. Var olmayan değer yok sayılır.

**D22 — Durumlar (B8).** Ana sayfa formunda marka okuması düşerse marka alanı görünmez ve form
kasa tipiyle çalışır (`0014` AC-63 deseni). Liste ekseninin yükleniyor/hata hâli diğer eksenlerle
aynıdır. Araç formunda aktif modeli olmayan bir marka seçilince model seçicisi "bu markada model
yok" der.

**D23 — Pasif model kuralı düzenlemeyi de kapsar (B9).** Model **değiştiren** düzenleme pasif
modeli reddeder; aracın zaten kayıtlı olduğu pasif modelle kaydetme kabul edilir (AC-26).

**D24 — Okuma uçları ve belge (B10, B11).** Vitrin ve araç formu bugünkü oturumsuz referans
uçlarından **yalnız aktif** kayıtları okur; pasifleri ve "ana sayfada göster" işaretini gösteren
yönetim okuması Yönetici'ye kapalıdır. Araç formu aracın kendi (belki pasif) marka/modelini
araç verisinden gösterir. `docs/architecture.md`'nin "zorunlu çeviri" satırı koda göre düzeltilir.
Geliştirme ve performans tohumu birden çok markaya yayılır.

### Verilen kararlar — plan turu, 2026-11-19

**Developer (ajan) önerisi**, uygulama sırasında; R/C/AC'ye dokunmaz, nasıl uygulandıklarını
sabitler. Takım Yöneticisi onayı PR'da bekleniyor.

**P1 — Yönetim uçları `VehicleReference`'ta, Yönetici politikalı tek grupta.**
`/api/admin/reference/{brands,models}…` `AdminSurface.ManagerPolicy` taşır (`0020` D2 deseni);
modül bunun için `Identity.Contracts`'a referans verir (`Dealership` emsali). Oturumsuz okuma
uçları `/api/reference/brands` ve `/api/reference/models` yalnız aktif kayıt döndürür (D24);
yönetim okuması pasifleri ve "ana sayfada göster" işaretini taşır.

**P2 — İyimser eşzamanlılık ve tekillik yarışı.** Marka ve model bir sürüm kolonu taşır; eski
sürümle yazma `409 concurrency.lost` (`0020` D17 deseni). Tekillik ön denetimle **ve** tekil
indeksle korunur (kanonik anahtar, kısaltma, marka içi model anahtarı); yarışta ikinci ekleme
indeksten düşer ve yine alan hatası döner, `500` değil. Anahtar kanonik adın değişmez kültürle
büyük harfidir (C3).

**P3 — Kanonik ad kuralı ve hata kodları.** Kırpılmış, yazdırılabilir ASCII (0x20–0x7E), en az bir
harf ya da rakam; marka ≤ 80, model ≤ 120, görünen ad ≤ 120 karakter. Kodlar
`brand.canonical_name_{required,invalid,too_long}`, `brand.name_taken`, `brand.slug_taken` (D19),
`vehicle_model.*` karşılıkları, `vehicle_model.brand_immutable` (AC-20),
`reference.locale_not_enabled`; hepsi `field` parametresiyle `422`.

**P4 — Tam ad = marka parçası + boşluk + model parçası.** Her parça kendi dilindeki görünen ada,
yoksa kanonik ada düşer (R3). C4'ün sabit eşlemesi her dili bu kurala göre böldü; AC-5'in testi on
iki modelin üç dildeki adını güncelleme öncesiyle **birebir** karşılaştırır.

**P5 — Eşlemede olmayan model varsa güncelleme hata ile durur.** Bugüne kadar yazma yolu yoktu ve
geliştirme tohumu aynı kimliği kullanıyordu; listede olmayan bir satırı tahminle bölmek C4'ün
yasakladığı şeydir. Geri alma (`Down`) tam adları geri yazar.

**P6 — Düzenleme model değişikliğini artık UYGULAR.** Ölçüldü: `UpdateVehicleCommand` istekteki
modeli doğruluyor ama araca yazmıyordu — araç formunda değiştirilen model sessizce eskisinde
kalıyordu. D23 "model değiştiren düzenleme"yi varsaydığı için `Vehicle.ChangeModel` eklendi;
okuma modeli aynı yansıtmayla güncellenir, adres değişmez (C2). Kapsam genişlemesi değil, D23'ün
ön koşulu; yine de açıkça yazılıyor.

**P7 — Vitrin marka ekseni ucu `GET /api/catalog/showcase-brands`.** Stoklu markalar okuma
modelinin tekil model kimliklerinden sözleşmeyle türetilir (D15); `brand` parametreleri D21 için
uca da verilir (var olan, stoksuz seçili marka eksende kalır); `scope=home` ana sayfanın süzgecidir
(R13). Ziyaretçi yanıtı yalnız `slug` + `label` taşır.

**P8 — Ana sayfa marka okuması formun okumalarıyla PARALEL.** Hata formu düşürmez, alan görünmez
(D22); ilk boyamanın önüne ek bir gidiş-dönüş girmez. Liste ekseni diğer eksenlerin okumasıyla
aynı `Promise.all`'da: yükleniyor ve hata hâli birebir aynıdır (AC-43); seçenekler yalnız dil
değişince ya da adres eksende olmayan bir marka getirdiğinde yeniden okunur.

**P9 — Liste paneli: marka ekseni SONDA, ayrılan yükseklik yeniden ölçüldü.** `0015` D20'nin
yöntemiyle (320–1400 px, 8 px adım, tr/ar/en); yedinci eksen 3 ve 4 sütunu ayrıştırdı ve bir bant
eklendi. Eksenin yüksekliği stoklu marka sayısına bağlıdır; ölçüm tohumun markalarıyla yapıldı.

**P10 — Tohum (D24).** İlanlar on iki modele altılı bloklarla dağılır (her model altı kasa tipini
gezer); "ana sayfada göster" tohumda Honda, Renault ve Toyota — yalnız hiç işaret yoksa.
Performans ölçümüne iki marka profili eklendi (`0021-performance.json`): iki marka ve iki marka +
iki kasa tipi.

**P11 — AC-4'ün ölçümü.** Adres yazma modelindedir (`0012b` D13) ve bu kart o şemaya dokunmaz;
test güncellemenin yalnız `vehicle_reference` şemasında çalıştığını ve adresin kaynağı olan kanonik
tam adın güncelleme öncesiyle aynı olduğunu ölçer. Var olan `YayinZinciriTests` yeni ilanın
`toyota-corolla-2019-` kalıbını ölçmeye devam eder.

### Uygulama planı (2026-11-19)

1. `VehicleReference`: marka varlığı, model → marka, eşleme migration'ı, sözleşme (tam ad, parçalar,
   seçilebilirlik, marka yüzeyi), komutlar, yönetim ve okuma uçları. `VehicleCatalog`: marka ekseni,
   `showcase-brands`, pasif model kuralı (D23), düzenlemede model değişikliği (P6), tohum (P10).
2. Testler: gerçek PostgreSQL'de ayrılma, komutlar, eksen; `Host.Tests`'te rol kapısı, yalnız aktif
   okuma, Fiat Egea adresi, talep kopyası, panel listesi; performans profili.
3. Ön yüz: panelde "Markalar ve modeller" (yalnız Yönetici), araç formunda marka → model; vitrinde
   liste marka ekseni ve ana sayfa marka alanı; katalog anahtarları ADR 0013'e göre.
4. E2E ve görsel kanıt (`evidence-0021.spec.ts`), var olan form akışlarına "Marka" adımı, belge
   güncellemeleri (DoD T7, T8) ve bu bölüm.

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
- **AC-9.** "Alfa Romeo" varken "Alfa-Romeo" eklemek, ad farklı olsa da kısaltma çakıştığı için ayrı bir hata koduyla reddedilir (D19).
- **AC-10.** ASCII olmayan ya da boş bir kanonik ad ("Škoda", "") reddedilir.
- **AC-11.** "Alfa Romeo" kanonik adlı markanın kısaltması `alfa-romeo`, "Rolls-Royce & Co." kanonik adlınınki `rolls-royce-co` olur (D10).
- **AC-12.** Markanın kanonik adı düzeltildiğinde kısaltması değişmez.
- **AC-13.** Yönetici bir markaya Arapça görünen ad ("فيات") verdiğinde Arapça vitrin onu gösterir; Türkçe vitrin kanonik adı gösterir.
- **AC-14.** Pasif yapılan marka araç formunun marka seçicisinde yeni seçim olarak görünmez.
- **AC-15.** Pasif yapılan markanın ilanları vitrinde kalır ve marka ekseninde süzülebilir.
- **AC-16.** Pasif marka yeniden etkinleştirildiğinde araç formunda yeniden seçilebilir.

### Model yönetimi (R2, R5, R6 · D11, D14)

- **AC-17.** Yönetici "Fiat" markasına "Egea" modelini eklediğinde model o markanın altında aktif görünür.
- **AC-18.** Aynı markada aynı kanonik adla ikinci bir model reddedilir.
- **AC-19.** Farklı markalarda aynı kanonik adlı modeller eklenebilir.
- **AC-20.** Modelin markası değiştirilemez: düzenleme ekranında marka seçimi sunulmaz ve uç markayı değiştiren isteği reddeder.
- **AC-21.** Pasif yapılan model araç formunda yeni seçim olarak sunulmaz; o modelle kayıtlı araçlar ve ilanlar değişmez.
- **AC-22.** Pasif bir markanın aktif modelleri de araç formunda yeni seçim olarak sunulmaz.

### Araç formu (R8, R9)

- **AC-23.** Yeni araç formunda model seçicisi bir marka seçilene kadar devre dışıdır.
- **AC-24.** Marka seçildiğinde model seçicisi yalnız o markanın aktif modellerini sunar.
- **AC-25.** Seçili model varken marka değiştirildiğinde model seçimi temizlenir.
- **AC-26.** Pasif bir modelle kayıtlı aracın düzenleme formu markayı ve modeli seçili gösterir ve araç model değiştirilmeden kaydedilebilir.
- **AC-27.** Yeni kayıtta ve modeli değiştiren düzenlemede pasif bir model sunucuda reddedilir (D23).
- **AC-28.** Araç formunda aktif modeli olmayan bir marka seçildiğinde model seçicisi markada model olmadığını söyler ve kayıt yapılamaz (D22).

### Vitrin (R3, R10 · D12, D13)

- **AC-29.** İlan kartı, ilan detayı ve öne çıkan araçlar aracı "Toyota Corolla" biçiminde, marka ve model görünen adlarıyla gösterir.
- **AC-30.** Görünen adı olmayan bir dilde marka ve model kanonik adlarıyla gösterilir.
- **AC-31.** Yönetici bir markanın görünen adını değiştirdikten sonra vitrin yeni adı gösterir.
- **AC-32.** Yeni yayına alınan bir Fiat Egea ilanının adresi `fiat-egea-<yıl>-…` biçimindedir.
- **AC-33.** Güncellemeden sonra Toyota Corolla için bırakılan talebin araç kopyası ve bildirim e-postası "Toyota Corolla" der (D17).
- **AC-34.** Panelin araç listesi aracı marka + model olarak gösterir (D17).

### Liste marka ekseni (R11, R12 · C5 · D9)

- **AC-35.** Liste filtresinde marka ekseni yalnız en az bir yayındaki ilanı olan markaları sunar.
- **AC-36.** Toyota seçildiğinde liste yalnız Toyota ilanlarını gösterir ve toplam sayı ona göre değişir.
- **AC-37.** Toyota ve Honda birlikte seçildiğinde liste iki markanın ilanlarını gösterir.
- **AC-38.** Marka ve kasa tipi birlikte seçildiğinde liste ikisine de uyan ilanları gösterir.
- **AC-39.** Seçim adres satırında `brand=toyota` olarak görünür; bu adres yeni bir sekmede açıldığında aynı daraltma gelir.
- **AC-40.** Bilinmeyen bir marka değeri (`brand=yok`) ve büyük harfli bir değer (`brand=Toyota`) yok sayılır ve liste hata vermez (D20).
- **AC-41.** Adresle gelen, var olan ama yayında ilanı kalmamış bir marka uygulanır, liste boş sonucu gösterir ve marka eksende işaretli görünür; işareti kaldırınca liste genişler (D21).
- **AC-42.** Güncellemeden önce yayında olan bir Toyota ilanı marka ekseninde Toyota seçilince listelenir (D16).
- **AC-43.** Marka ekseninin yüklenme ve hata hâli diğer eksenlerinkiyle aynıdır (D22).
- **AC-44.** Arapça listede marka ekseni Arapça görünen adları gösterir ve sağdan sola düzende çalışır.

### Ana sayfa formu (R13, R14 · D8, D9)

- **AC-45.** "Aracınızı bulun" formu yalnız "ana sayfada göster" işaretli ve en az bir yayındaki ilanı olan markaları sunar.
- **AC-46.** İşaretli ama yayında ilanı olmayan bir marka formda sunulmaz.
- **AC-47.** İşaretsiz bir markanın yayındaki ilanı olsa da formda sunulmaz.
- **AC-48.** Formda Toyota seçilip düğmeye tıklanınca `/{locale}/cars?brand=toyota` açılır; düğmedeki sayı seçimle değişmez (`0014` AC-41, D18).
- **AC-49.** Formda marka ve kasa tipi birlikte seçildiğinde liste ikisiyle açılır.
- **AC-50.** Gösterilecek marka yoksa formun marka alanı hiç görünmez ve form kasa tipiyle çalışır.
- **AC-51.** Formun marka okuması düştüğünde marka alanı görünmez, teknik ayrıntı gösterilmez ve form kasa tipiyle çalışır (D22).

### Erişim ve durumlar (R15, R16 · D4)

- **AC-52.** Satış Danışmanı ve İçerik Editörü panelde marka/model ekranına bağlantı görmez; adresi doğrudan açtıklarında "erişim yok" ekranını görür.
- **AC-53.** Yönetici olmayan rolün marka ve model yazma uçlarına isteği `403` alır.
- **AC-54.** Satış Danışmanı araç formunda aktif markaları ve modelleri okuyabilir.
- **AC-55.** Vitrinin ve araç formunun okuduğu referans uçları pasif marka ve modelleri döndürmez; pasifleri ve "ana sayfada göster" işaretini döndüren yönetim okuması Yönetici olmayan role `403` döner (D24).
- **AC-56.** Marka/model ekranı veri gelirken yükleniyor, okunamazken teknik ayrıntı göstermeyen hata durumu ve yeniden deneme gösterir; hiç marka yokken boş durumu gösterir.
- **AC-57.** Yeni metinler TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.
- **AC-58.** Arapça panelde marka/model ekranı sağdan sola çalışır; kanonik ad alanı soldan sağa yazılır.
- **AC-59.** Ana sayfa ve liste için performans kapısı yeşil kalır.

---

## Definition of Done

- [x] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı. *(B1–B11 → D15–D24, 2026-11-18.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0021-ac<n>.png`),
      boş ve hata durumları dâhil; Arapça görüntü de var.
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [x] <!-- DoD:T5 --> Yeni migration'lar yalnızca kendi şemasına dokunuyor (`vehicle_reference`, `catalog`).
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [x] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**marka**, **pasif** referans kaydı).
- [x] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi; § 1'in `VehicleReference` tanımı **D4**'e, § 3'ün "zorunlu çeviri" satırı **D24**'e göre düzeltildi.
- [x] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [x] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [x] `0010a` X14, `0015` X8 ve `0014` X13'e "→ `0021`" notu düşüldü.
- [x] Geliştirme ve performans tohumu birden çok markaya yayılıyor (**D24**).

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
