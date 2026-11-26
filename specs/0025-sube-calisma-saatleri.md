# 0025 — Şube çalışma saatleri

| | |
|---|---|
| **Durum** | **Onaya sunuldu (2026-11-26)** |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (şube) · `Host` (ana sayfanın yapısal verisi) · Frontend (panel şube formu, vitrin altlığı) |
| **Bağımlı spec'ler** | `0003` (**X2**, **C9**, **C10**, **B4**) · `0004` (**D1**, **D15**, **D16**, **D17**, **AC-52**) · `0014` (altlık, **AC-14**) · `0020` (şube formu, **R16** sürüm, **D21** kapatılmış şube) · **`0024`** (okuma sözleşmesinin ve altlığın o kartla aldığı hâl). **Sonra gelen:** `0026`. |
| **Karar bekliyor** | **Takım Yöneticisi:** D1–D15 (öneri). **Sahip:** SS1–SS3 (önerili). |

> **Sahibin isteği (sahip kararı, 2026-11-26):** `0004` **SS3**'e "evet": şube çalışma saatleri
> panelden girilsin, vitrinde ve yapısal veride (`openingHoursSpecification`) görünsün; `0004`'ten
> sonra **ayrı küçük kart**. `0003` **X2** bu işi ayırmıştı. `0020` **X3** de açmamıştı.

---

## Intent

Bir aracı görmeye gitmek isteyen ziyaretçi, şubenin hangi gün ve hangi saatlerde açık olduğunu
bilmek istiyor. Bugün vitrinde şubenin yalnız adı, adresi ve telefonu var ve ziyaretçi saatleri
telefonla sormak zorunda. Arama motorları da şubenin saatlerini bilmiyor. Bu iş bittiğinde
yönetici her şube için haftalık programı panelden girer. Öğle arası olan çift mesai ve kapalı
günler de bu programda yer alır. Kaydettikten sonra açılan ilk ana sayfada her şubenin altında
yedi günün saatleri görünür ve aynı saatler şubenin yapısal verisinde de yer alır.

---

## Requirements

- **R1.** Yönetici her şube için bir **haftalık çalışma programı** girer: haftanın her günü için
  sıfır ya da daha fazla saat aralığı (`docs/domain.md` **B1**).
- **R2.** Aralığı olmayan gün o şubenin **kapalı** günüdür. Hangi günlerin kapalı olduğu veriden
  okunur ve kodda varsayılmaz (**B2**).
- **R3.** Program opsiyoneldir. Programı girilmemiş şubenin saatleri vitrinde gösterilmez.
  "Girilmemiş" ile "her gün kapalı" aynı anlamı taşımaz (D4).
- **R4.** Saatler şubenin **kendi yerel saatidir** (şubenin saat dilimi) ve vitrinde öyle gösterilir.
- **R5.** Kurallara uymayan bir program (biçim, sıra, çakışma, üst sınır) alan hatasıyla reddedilir
  ve şubenin hiçbir alanı değişmez.
- **R6.** Program şube formunun parçasıdır. Şubeyle aynı kaydetmede yazılır ve şubenin eşzamanlı
  yazma korumasına (`0020` **R16**) tabidir. Kapatılmış şubenin programı değiştirilemez.
- **R7.** Ana sayfanın altlığı, programı girilmiş her şubenin altında o şubenin yedi gününü gösterir.
- **R8.** Ana sayfanın yapısal verisi, şubenin programını o şubenin öğesinde taşır.
- **R9.** Ziyaretçiye dönük galeri okuması şube başına programı taşır.
- **R10.** Programı yalnız **Yönetici** düzenler. Satış Danışmanı ve İçerik Editörü panel adresini
  doğrudan açtıklarında "erişim yok" görürler.
- **R11.** Panel bölümü ve vitrin gösterimi TR/AR/EN'de çevrilidir; Arapçada sağdan sola dizilir.

---

## Constraints

### Uyulması zorunlu

- **C1.** Kodda hafta sonu günü, çalışma günü sayısı ya da tek açılış/kapanış saati
  **varsayılmaz**. → Yasak Listesi 34, `docs/domain.md` **B1**, **B2**, `docs/architecture.md` § 1
  "Çalışma saatleri modeli".
- **C2.** Bu kart "şu an" bilgisini **okumaz** ve "açık mı" hesaplamaz (X2). Saklanan değer günün
  saatidir, bir an değildir (**D2**). Sunucu yerel saati ya da `DateTime.Now` kullanılmaz.
  → Yasak Listesi 30, 35.
- **C3.** Ziyaretçiye dönük okuma bir **izin listesidir** (`0014` **D15**). Şube öğesine yalnız
  program eklenir.
- **C4.** Yapısal veri sayfada görünmeyen bir bilgiyi iddia etmez (`0004` **D1**). Serbest metin
  `0004` **D16**'ya göre kodlanır. Ana sayfa tam olarak bir JSON-LD bloğu taşır.
- **C5.** Performans: C3 (LCP < 2500 ms) ana sayfa, liste ve detayda yeşil kalır. İlk paket ADR
  0013'ün protokolüyle ölçülür. Panel metinleri panel kataloğuna girer.
- **C6.** Migration yalnız `dealership` şemasına dokunur.
- **C7.** Programın tek yazma sahibi `Dealership`'tir. Program başka bir modüle kopyalanmaz ve
  entegrasyon olayı üretmez (D11). → Yasak Listesi 33.
- **C8.** Hata yanıtları kararlı bir `errorCode` ve alan anahtarı taşır (`0020` **C5**).
- **C9.** Rakam sistemi açıkça sabittir: saatler her dilde Latin rakamla basılır
  (`docs/frontend.md` § 5).
- **C10.** Bu kart **`0024`'ten sonra** uygulanır. Okuma yanıtının kök anahtar kümesi ve altlığın
  blok sırası `0024`'ün hâlini temel alır.

### KAPSAM DIŞI

- **X1.** **İstisna programı** (bayram, resmî tatil, Ramazan saatleri, belirli bir günde kapanış;
  **B3**) — *neden:* tarih aralıklı ayrı bir model, haftalık programı ezme kuralı, geçmiş
  istisnanın düşmesi ve yapısal verideki geçerlilik tarihleri tek başına bir kart (**D1**, **SS1**).
- **X2.** **"Şu an açık / kapalı" göstergesi ve "bugün"ün vurgulanması** (**B4**) — *neden:*
  istisnasız bir hesap bayramda "açık" der, yani X1 ile birlikte gelmelidir. Hesap şubenin saat
  diliminde ve saat soyutlamasıyla yapılır (`0003` **B4**). Bu kart onun girdisini hazırlar (**D2**).
- **X3.** **Gece yarısını geçen aralık** (ör. 22:00–02:00) — *neden:* galerilerde yaygın değil. İki
  güne bölünerek girilebilir (**D3**).
- **X4.** **Haftanın ilk gününün kuruluma ya da dile göre değişmesi** — *neden:* **D6**, **SS3**.
- **X5.** **Saatlerin ziyaretçinin saat dilimine çevrilmesi** — *neden:* şubeye gidilen saat
  şubenin yerel saatidir (**D2**).
- **X6.** **Şube sayfası** — *neden:* `0004` **X3**, **SS4** (şimdilik hayır).
- **X7.** **Aynı saatli günleri gruplayan özet gösterim** ("Pzt–Cum 09:00–18:00") — *neden:*
  gün aralığı söz dizimi dil başına farklı. Yedi satırlık liste her dilde tek anlamlıdır (**D7**).
- **X8.** **Geçici kapalı şubenin vitrinde "geçici kapalı" diye görünmesi** — *neden:* bugün
  vitrinde hiç görünmüyor (`PublicGalleryQueries.cs:49`). Bu kart bunu değiştirmez.
- **X9.** **Programın başka modüllerde kullanılması** (talep saatleri, bildirim zamanlaması) —
  *neden:* ham isteklerde yok (C7).
- **X10.** **12 saat biçimi (ÖÖ/ÖS, AM/PM)** — *neden:* **D7**.
- **X11.** **Galeri düzeyinde (şubeden bağımsız) program** — *neden:* domain programı şubeye
  bağlıyor (**B1**).

---

## Context

> Her iddia `main` @ `bcbe520` üzerinde ölçüldü. Satır numaraları o commit'e aittir. `0024`
> merge edildikten sonra uygulamaya geçilir (C10). O kartın değiştirdiği yerler aşağıda ayrıca
> işaretli.

### Domain ve mimari kurallar

- **B1–B4** (`docs/domain.md:302-313`): programda gün başına 0..N aralık olur; kapalı gün aralığı
  olmayan gündür; istisnalar haftalık programı ezer; "şu an açık mı" şubenin saat diliminde
  hesaplanır. Bu kart B1 ile B2'yi uygular. B3 (X1) ve B4 (X2) dışarıda.
- `docs/architecture.md:36-40`: "Körfez'de çift mesai yaygındır (09:00–13:00 ve 16:00–21:00)" ve
  "Kuveyt'te Cuma–Cumartesi, Türkiye'de Pazar" kapalıdır. Kriterlerin örnekleri bu ikisini kapsar.
- `0003` **C10** saat dilimi alanını eklerken aynı varsayım yasağını koydu. Saat diliminin
  kendisi şubede **IANA kimliği** olarak duruyor (`Domain/TimeZoneId.cs:15-43`): "Bölge/Şehir"
  biçimi zorunlu ve kimlik tzdata'da doğrulanıyor. Belge yorumu (`:8-10`) sabit farkın "şu an
  açık mı" hesabını yılda iki kez bozacağını söylüyor. **D2** bu uyarının çalışma saatine
  uygulanmış hâli.

### Bugün şubede ne var

- **Çalışma saati verisi yok.** `Branch` (`Domain/Branch.cs:22-65`) ad, adres, koordinat, telefon,
  WhatsApp, e-posta, saat dilimi, durum, slug ve medya kimliklerini taşıyor. Saat, aralık ya da
  gün tipi yok. `BranchConfiguration.cs` buna karşılık gelen bir kolon tanımlamıyor. → **D13**:
  `dealership` şemasında migration.
- **Kaydetme yolu.** `Branch.Update` (`Branch.cs:132-163`) kapatılmış şubede `ClosedReadOnly`
  döndürüyor (`:134-137`, `0020` **D21**, `409`). Başarıda sürümü bir artırıyor (`:160`). Durum
  eylemleri de sürümü artırıyor (`:256`, `:269`). Opsiyonel alanlarda gövdedeki `null` alanı
  **boşaltıyor** (`ApplyOptionals`, `:165-193`, `0020` **D9**). **D11** programda bundan bilerek
  ayrılıyor.
- **Duyuru.** Yalnız ad değişikliği `BranchDetailsChanged` üretiyor (`Branch.cs:139-147`;
  `Application/BranchIntegrationEvents.cs:55`). Adres, telefon ve saat dilimi duyuru üretmiyor
  (`Branch.cs:226`, "AC-22 — bunların hiçbiri duyuru üretmez"). Program da üretmez (**D11**).
- **Uçlar.** `POST /api/admin/branches` ve `PUT /api/admin/branches/{id}`
  (`Endpoints/DealershipAdminEndpoints.cs:40-42`), gövdeleri `OpenBranchBody` / `SaveBranchBody`
  (`:120-142`), Yönetici grubunda. Panel okuması `AdminBranchDto`
  (`Application/DealershipAdminQueries.cs:19-31`). Şube **listesi** (`GET /api/admin/branches`)
  bu grupta değil ve her rol okuyor (`DealershipAdminEndpoints.cs:19-21`, `0020` AC-68). → **D14**:
  liste değişmez.
- **Panel formu.** `frontend/src/features/dealership/admin/BranchEditPage.tsx` (574 satır), açma
  ve düzenleme aynı bileşen. Saat dilimi seçimi `:396-410`'da.

### Bugün vitrin

- **Okuma:** `PublicBranchDto(Name, Address, Phone)` (`Dealership.Contracts/Gallery/PublicGalleryContracts.cs:17`).
  Sorgu yalnız `Active` şubeleri (`PublicGalleryQueries.cs:49`) ve yalnız üç alanı (`:50`) okuyor.
  Sıra varsayılan dildeki adın ordinal sırası (`:59`). Şube öğesinin anahtar kümesi
  `tests/Host.Tests/GaleriPublicUcTests.cs:31`'de birebir ölçülüyor: `["address", "name", "phone"]`.
- **Altlık:** `frontend/src/features/dealership/BranchList.tsx:43-60` her şube için ad, adres ve
  `tel:` bağlantısı basıyor. Telefon `dir="ltr"` (`:56`). Okuma sürerken iskelet var, hata ve boş
  durumda blok yok (`:14-31`).
- **Yapısal veri:** `src/Host/Seo/SeoHeadBuilder.cs:111-120` her `department` öğesine `@type`,
  `name`, `address` ve `telephone` yazıyor.
- **Biçimlendirme:** biçimlendirme uçta yapılıyor ve rakam sistemi sabitleniyor
  (`frontend/src/shared/i18n/format.ts`, `intlLocale`; `docs/frontend.md:190-191`).

### `0024`'ün bu karta bıraktığı hâl (C10)

- Okuma yanıtının kökü `branches`, `displayName`, `socialLinks` olur (`0024` AC-33). Bu kart kökü
  değiştirmez, yalnız şube öğesini değiştirir.
- Altlıkta şube bloğu ilk bloktur. `0024` "Bizi takip edin" bloğunu "Kurumsal"ın ardına ekler. Bu
  kartın saat listesi şube bloğunun **içindedir**.
- `0004` AC-52'nin yokluk listesi `0024`'ten sonra `openingHoursSpecification`, `geo`, `logo`'dur.
  Bu kart `openingHoursSpecification`'ı çıkarır (**AC-55**). `0026` `logo`'yu çıkarır.

### Bilerek değişecek eski testler (kod taranarak çıkarıldı)

| Test | Bugün ölçtüğü | Bu kartla |
|---|---|---|
| `tests/Host.Tests/GaleriPublicUcTests.cs:31` (`0014/AC-14`) | şube öğesi `address, name, phone` | `address, name, openingHours, phone` (**AC-41**) |
| `tests/Dealership.Tests/Integration/GaleriPublicOkumaTests.cs:76-77` | `PublicBranchDto` üç parametreli eşitlik | kurucu değişir. Programsız şubede beklenen değer `openingHours: null` ile aynı |
| `tests/Host.Tests/SeoBelgeTests.cs:547-551` (`0004/AC-50`) ve `PublicBranchDto` kuran diğer testler | üç parametreli kurucu | kurucu değişir, beklenen değer değişmez |
| `tests/Host.Tests/SeoBelgeTests.cs:561-573` (`0004/AC-52`, `0024`'ten sonra `0024` AC-45 hâli) | `openingHoursSpecification` yok | listeden çıkar (**AC-55**) |
| `frontend/src/app/HomePage.test.tsx`, `BultenBolumu.test.tsx`, `AnaSayfaMetinleri.test.tsx` | `PublicGallery` sahte şubeleri | şube tipi değişir. Programsız şubede altlık beklentileri değişmez |
| `0020`'nin şube kaydetme/açma testleri (`tests/Host.Tests/GaleriYonetimiZinciriTests.cs` ve `tests/Dealership.Tests`) | programsız gövde | **değişmez** (**D11**, **AC-25**) |

### Bilinen tuzaklar

1. **UTC'ye çevrilmiş çalışma saati yaz saatinde kayar.** 09:00'u bir an gibi saklamak, yaz saati
   geçişinde vitrine 08:00 ya da 10:00 bastırır. Hata yılda iki kez ortaya çıkar (`TimeZoneId.cs:8-10`).
   **D2** saatleri duvar saati olarak saklar.
2. **"Hepsi kapalı" ile "girilmemiş" aynı veri olabilir.** Yedi boş gün iki anlam taşırsa vitrin
   ya hiç saati olmayan şubeye "her gün kapalı" basar ya da tersini yapar. **D4** bunu tek anlama
   bağlar.
3. **Gövdede alanın olmaması ile boş olması farklıdır** (**D11**). Ayrım ölçülmezse `0020`'nin
   programsız gövde gönderen istemcisi ve testleri kayıtlı programı siler.
4. **Tarayıcının saat girişi her dilde aynı görünmez.** İşletim sisteminin diline göre 12 saatlik
   görünebilir, ama değeri her zaman `HH:mm`'dir. Kriterler değeri ölçer, görünümü ölçmez (**D5**).
5. **Uç uca değen iki aralık** (09:00–13:00 ve 13:00–18:00) tek bir aralıktır. Kabul edilirse
   yapısal veri iki öğe basar ve özet okunmaz hâle gelir (**D3**).

---

## Verilen kararlar (öneri — Takım Yöneticisi onayı bekliyor)

Biçim: **karar** · *gerekçe* · *bedel*.

**D1 — Kapsam yalnız haftalık programdır. İstisnalar ve "şu an açık" ayrı karttadır.** (R1, X1, X2)
*Gerekçe:* istisna programı ayrı bir model ve bir öncelik kuralı getirir. "Şu an açık"
istisnasız yanlış sonuç verir. İkisi birlikte bir kart olur (öneri: bu kartın hemen ardından).
Haftalık program tek başına ziyaretçinin bugünkü sorusunu cevaplıyor.
*Bedel:* bayram ve tatillerde vitrin ve arama motoru haftalık saati gösterir. Galeri bunu başka
kanaldan duyurur (**SS1**).

**D2 — Saat dilimi kuralı: program şubenin duvar saatidir.** (R4, C2, X5)
- "09:00", şubenin kendi saat diliminde 09:00 demektir. Değer UTC'ye çevrilmez ve bir an
  (tarih + saat + fark) olarak saklanmaz.
- Vitrinde ziyaretçinin saat dilimine çevrilmez ve saat diliminin adı gösterilmez.
- **Şubenin saat dilimini değiştirmek kayıtlı saatleri değiştirmez**: 09:00 yine 09:00'dır.
- Bu kart "şimdi"yi okumaz, yani Yasak Listesi 30 ve 35 bu kartta tetiklenmez.
- X2'nin hesabı bu değerleri şubenin `TimeZoneId`'si ve saat soyutlamasıyla "o andaki yerel
  saate" bağlayacak (`0003` **B4**).
- Yapısal verideki `opens`/`closes` saat farkı taşımayan yerel saattir (schema.org `Time`).

*Gerekçe:* çalışma saati bir an değil, yerel bir kuraldır (tuzak 1). Ziyaretçi şubeye yerel saatle gider.
*Bedel:* başka saat diliminden bakan ziyaretçi saati kendisi çevirir.

**D3 — Aralık kuralları.** (R1, R5, X3)
- Saat `HH:mm` biçimindedir: saat 00–23, dakika 00–59.
- Başlangıç bitişten önce gelir. Aralık aynı gün içindedir (en geç 23:59).
- Bir günde en çok **3** aralık olur.
- Aynı günün aralıkları çakışmaz ve **uç uca değmez** (tuzak 5).
- Aralıklar başlangıca göre sıralı saklanır ve döner. Girilen sıra korunmaz.

*Gerekçe:* çift mesai iki aralık ister, üçüncü aralık bir pay bırakır. Üst sınır bir sınırdır,
"tek aralık" varsayımı değildir (C1). Sınır bütçeyi de (D10) sınırlar.
*Bedel:* 24 saat açık şube 00:00–23:59 girer. Gece yarısını geçen aralık iki güne bölünür (X3).

**D4 — Program opsiyoneldir. Yedi boş gün "girilmemiş" demektir.** (R2, R3, tuzak 2)
- Hiçbir günde aralık yoksa program **girilmemiş** sayılır ve saklanmaz. Vitrin ve yapısal veri onu
  hiç göstermez.
- En az bir aralık varsa, aralıksız günler **kapalıdır** ve öyle gösterilir.

*Gerekçe:* aktif olup hiç açılmayan şube "geçici kapalı" durumudur (`0003` **D3**). İki anlamı
ayırmanın en küçük yolu bu ve **B2**'yi korur.
*Bedel:* "her gün kapalı" bilgisi programla verilemez. Onun yolu durum eylemidir.

**D5 — Panel: şube formunda "Çalışma saatleri" bölümü.** (R1, R6, R11, tuzak 4)
- Bölüm açma ve düzenleme formunda bulunur. Yedi gün D6'nın sırasıyla dizilir.
- Her gün ya "Kapalı"dır (aralık yok) ya da 1–3 aralık taşır. Her aralığın başlangıç ve bitiş
  alanı vardır ve alanın değeri `HH:mm`'dir.
- "Aralık ekle" günün üçüncü aralığından sonra görünmez. "Kaldır" aralığı siler. Son aralık
  kaldırılınca gün "Kapalı" olur.
- Pazartesi dışındaki her günde **"Önceki günle aynı"** eylemi vardır: önceki günün aralıklarını
  bu güne kopyalar. Kopyalama formda yapılır, kaydetme ayrıca gerekir.
- Bölümün yardım metni "Hiç saat girilmezse vitrinde saat gösterilmez" der (D4).
- Alan hatası ilgili günün ya da aralığın yanında çevrilmiş metinle görünür.

*Gerekçe:* aynı saatli günler çoğunluktadır. Kopyalama eylemi yedi günü tek tek yazmayı önler.
*Bedel:* yok.

**D6 — Gün sırası her dilde Pazartesi → Pazar'dır.** (R7, X4, **SS3**) Panelde, vitrinde, okuma
yanıtında ve yapısal veride aynı sıra kullanılır (ISO 8601). Gün adları ekranın dilinde,
tarayıcının yerel ad tablosundan basılır ve çeviri kataloğuna girmez. Gün kodları `monday` …
`sunday`'dir.
*Gerekçe:* tek, deterministik bir sıra. Ad tablosu paket büyütmez (D10).
*Bedel:* Körfez'de hafta çoğunlukla Cumartesi ya da Pazar başlar. İlk gün ayarı ayrı bir karar (X4).

**D7 — Vitrin: altlıkta her şubenin altında yedi satır.** (R4, R7, R11, X7, X10)
- Program girilmiş şubede, adres ve telefondan sonra yedi satır gelir. Her satır gün adını ve o
  günün aralıklarını birlikte taşır. Kapalı günde "Kapalı" yazar.
- Aralık `09:00–13:00` biçimindedir: 24 saat, Latin rakam, her dilde aynı metin ve soldan sağa.
  Birden çok aralık sırayla yan yana basılır.
- Program girilmemiş şubede saat listesi yoktur.
- Galeri okumasının yükleniyor, boş ve hata durumları bugünkü gibidir (`BranchList.tsx:14-31`).
  Saat listesinin ayrı bir yükleme durumu yoktur, çünkü aynı okumadan gelir.
- Vitrine eklenen metinler yalnız "Kapalı" ve listenin erişilebilir etiketidir.

*Gerekçe:* `0004` **D1**: yapısal veri görünen bilgiyi taşır, altlık da şubenin görünen yeri.
24 saat biçimi üç dilde aynı ve belirsizlik taşımıyor.
*Bedel:* iki şubeli galeride altlık 14 satır uzar (**SS2**). İngilizce ziyaretçi 24 saat biçimi görür.

**D8 — Okuma yanıtı: şube öğesine `openingHours`.** (R9, C3)
- Program girilmemişse değer `null`'dır.
- Girilmişse tam olarak yedi gün kodunu anahtar olarak taşıyan bir nesnedir. Kapalı gün boş
  dizidir. Her aralık `{ "start": "HH:mm", "end": "HH:mm" }` biçimindedir ve aralıklar başlangıca
  göre sıralıdır.
- Yanıt dilden bağımsızdır. Şube öğesinin anahtar kümesi bilerek değişir (Context tablosu).

*Gerekçe:* yedi anahtarın hepsi bulunduğunda "kapalı gün" açıkça okunur, eksik anahtardan
çıkarılmaz (B2).
*Bedel:* yok.

**D9 — Yapısal veri: `department` öğesinde `openingHoursSpecification`.** (R8, C4)
- Program girilmiş şubenin öğesi bir dizi taşır. Her öğe `@type: OpeningHoursSpecification`,
  `dayOfWeek`, `opens` ve `closes` taşır.
- `dayOfWeek` schema.org gün adlarının (`Monday` … `Sunday`) bir dizisidir.
- Aynı (`opens`, `closes`) çiftini taşıyan günler tek öğede birleşir ve günleri D6'nın sırasıyla
  dizilir.
- Öğeler, çiftin ilk göründüğü güne (D6 sırası), aynı günde de aralık sırasına göre dizilir.
- **Kapalı günler** (program girilmişken) `opens` ve `closes` `00:00` olan tek bir öğede toplanır.
  Bu, arama motorunun "bütün gün kapalı" gösterimidir.
- Program girilmemiş şubenin öğesinde anahtar yoktur. Kök `AutoDealer`'da hiç yoktur.
- Bu kartla `0004` **AC-52**'nin yokluk listesi `geo`, `logo` olur.

*Gerekçe:* gruplama baytı azaltır (D10). Tek gün bile bir dizi olarak yazılır ve biçim her
öğede aynı kalır.
*Bedel:* gruplama kuralı kriterle sabitlenir (AC-53).

**D10 — Bütçe.** (C5)
- **(a)** `/ar/` ana sayfa belgesinde **tek bir şubenin** programının eklediği bayt **en çok 2304 B**'tır
  (sıkıştırmasız). Kurgulanmış en kötü durum: yedi günün her birinde üç aralık ve 21 aralığın
  hiçbiri başka bir günle aynı (`opens`, `closes`) çiftini taşımıyor. Şube sayısıyla doğrusal büyür.
- **(b)** İlk paket ADR 0013'ün protokolüyle `main` tabanına göre **en çok 512 B** büyür.
- **(c)** C3 ana sayfa, liste ve detayda yeşil kalır. Perf koşusu her aktif şubeye (a)'nın
  programını tohumlar. Bir sayfanın medyanı **2450 ms**'yi aşarsa ikinci koşu istenir ve LCP öğesi
  PR'a yazılır.

*Gerekçe:* bir öğe yaklaşık 95 B, 21 öğe yaklaşık 2 kB. Liste altlıkta, ilk ekranın altında.
*Bedel:* çok şubeli galeride belge şube başına büyür.

**D11 — Kaydetme, sürüm ve duyuru.** (R5, R6, C7, tuzak 3)
- Program, şube açma ve kaydetme gövdelerinde D8'in biçimiyle taşınır. Aynı sürüm geçerlidir:
  eski sürüm `409` döner (`0020` **D17**).
- Hata varsa şubenin **hiçbir alanı** değişmez.
- Başarılı değişiklik sürümü bir artırır.
- Gövdede alan **yoksa** kayıtlı program değişmez. Açmada program yok demektir. Yedi günü boş bir
  program (ya da boş nesne) programı **siler** (D4).
- Kapatılmış şubede değişiklik `409` `branch.closed_read_only` döner (`0020` **D21**). Geçici kapalı
  şubenin programı değiştirilebilir.
- Program değişikliği entegrasyon olayı üretmez.

*Gerekçe:* tek "Kaydet", tek sürüm. Alan yoksa değişmeme kuralı `0024` **D4** ile aynı gerekçeye dayanır.
*Bedel:* `0020`'nin opsiyonel alanlarındaki "`null` boşaltır" kuralından farklı (Context).

**D12 — Hata kodları ve alan anahtarları.** (C8) `422`, alan anahtarı gün düzeyinde
`openingHours.<gün>` ve aralık düzeyinde `openingHours.<gün>.<sıra>.start|end` (sıra 0'dan başlar).
- `opening_hours.invalid_time`: biçim. Aralık alanında.
- `opening_hours.end_not_after_start`: bitiş başlangıçtan sonra değil. Aralığın `end` alanında.
- `opening_hours.too_many_intervals`: günde üçten fazla aralık. Gün alanında.
- `opening_hours.overlap`: çakışma ya da uç uca değme. Gün alanında.
- `opening_hours.unknown_day`: bilinmeyen gün kodu. Gönderilen kodun alanında.

*Gerekçe:* `0020`'nin alan hatası deseni. Sıralı saklama (D3) hatanın girilen sıraya göre
gösterilmesini engellemez, çünkü doğrulama sıralamadan önce yapılır.
*Bedel:* yok.

**D13 — Saklama `dealership` şemasındadır.** (C6) Biçim (ayrı tablo ya da yapılandırılmış kolon)
uygulamanın kararıdır. Kriterler davranışı ölçer.
*Gerekçe:* C6.
*Bedel:* yok.

**D14 — Panel okuması programı taşır, şube listesi taşımaz.** (R6) `GET /api/admin/branches/{id}`
programı D8'in biçimiyle döner. Her rolün okuduğu şube listesinin anahtar kümesi değişmez
(`0020` AC-68).
*Gerekçe:* araç formu listeyi okuyor ve program o ekranda kullanılmıyor.
*Bedel:* yok.

**D15 — Yeni domain terimi: "Çalışma Programı".** (DoD T7) Şubenin haftanın her günü için 0..N
yerel saat aralığından oluşan programı. Aralıksız gün kapalıdır. İstisnaları kapsamaz (X1).
*Gerekçe:* sözlükte "çalışma saati" kuralları var (B1–B4), terim yok.
*Bedel:* yok.

---

## Sahibe sorular

> Yalnız sahibin verebileceği kararlar. Her birinin altında öneri var. Cevap gelene kadar öneri
> uygulanır. Cevap öneriden farklıysa ilgili D ve kriterler kart onaylanmadan güncellenir.

- **SS1. Bayram ve tatil saatleri canlıya çıkıştan önce gerekli mi?** Bu kart yalnız haftalık
  programı getiriyor. Bayramda vitrin ve arama motoru haftalık saati gösterir (D1).
  **Öneri:** istisna programı ve "şu an açık" göstergesi bu kartın hemen ardından ayrı bir kart
  olsun. Canlıya çıkış için gerekliyse sıradaki iş o olur.
- **SS2. Saatler vitrinde nerede ve nasıl görünsün?**
  **Öneri:** ana sayfa altlığında, her şubenin adres ve telefonunun altında yedi satır (D7).
  Altlık bugün yalnız ana sayfada var (`0024` X4).
- **SS3. Hafta hangi günle başlasın?**
  **Öneri:** her dilde Pazartesi (D6). Körfez'deki ziyaretçi Cumartesi ya da Pazar başlayan hafta
  bekleyebilir. Bu ayar ayrı bir karar olur (X4).

---

## Acceptance Criteria

> Panel kriterleri Yönetici oturumuyla, aksi yazılmadıkça varsayılan kurulumda (tr/ar/en, şube
> saat dilimi `Europe/Istanbul`) koşar. "Programlı şube" aksi yazılmadıkça şu programı taşır:
> Pazartesi–Cuma 09:00–18:00, Cumartesi 10:00–14:00, Pazar kapalı. Belge kriterleri sunucu
> yanıtıyla, JavaScript çalıştırılmadan ölçülür (`0004` C13).

### Panel: giriş (R1, R6 · D5, D6)

- **AC-1.** Şube düzenleme ekranının "Çalışma saatleri" bölümü yedi gün gösterir. Sırası Pazartesi,
  Salı, Çarşamba, Perşembe, Cuma, Cumartesi, Pazar'dır ve gün adları ekranın dilindedir.
- **AC-2.** Programı olmayan şubenin bölümünde yedi gün "Kapalı" görünür. "Hiç saat girilmezse
  vitrinde saat gösterilmez" yardım metni görünür.
- **AC-3.** Yönetici Pazartesiye 09:00–18:00 girip kaydettikten sonra ekran yeniden açıldığında,
  Pazartesi 09:00–18:00 taşır ve diğer altı gün "Kapalı" görünür.
- **AC-4.** Bir güne 09:00–13:00 ve 16:00–21:00 girip kaydetmek iki aralığı da saklar. Ekran yeniden
  açıldığında ikisi bu sırayla görünür.
- **AC-5.** Üç aralığı olan bir günde "Aralık ekle" eylemi yoktur.
- **AC-6.** İki aralıklı bir günden bir aralığı kaldırıp kaydetmek o aralığı siler. Son aralığı da
  kaldırıp kaydetmek günü "Kapalı" yapar.
- **AC-7.** Salı'nın "Önceki günle aynı" eylemi, Pazartesinin aralıklarını Salı'ya formda kopyalar.
  Kaydetmeden ekrandan çıkmak programı değiştirmez.
- **AC-8.** Yeni şube açma formunda aynı bölüm vardır. Programla açılan şubenin programı saklanır.
- **AC-9.** Program girilmeden açılan şubenin okuma yanıtında `openingHours` `null`'dır.

### Doğrulama (R5 · D3, D12)

- **AC-10.** Başlangıcı `9:00` olan aralık `422` döner. Yanıt `openingHours.monday.0.start`
  alanında `opening_hours.invalid_time` taşır.
- **AC-11.** Bitişi `24:00` olan aralık `422` ve `openingHours.monday.0.end` alanında
  `opening_hours.invalid_time` döner.
- **AC-12.** Başlangıcı `12:60` olan aralık `422` ve `opening_hours.invalid_time` döner.
- **AC-13.** 18:00–09:00 aralığı `422` döner. Yanıt `openingHours.monday.0.end` alanında
  `opening_hours.end_not_after_start` taşır.
- **AC-14.** 09:00–09:00 aralığı `422` ve `opening_hours.end_not_after_start` döner.
- **AC-15.** Pazartesiye dört aralık gönderen kaydetme `422` döner. Yanıt `openingHours.monday`
  alanında `opening_hours.too_many_intervals` taşır.
- **AC-16.** Aynı günde 09:00–13:00 ve 12:00–18:00 `422` döner. Yanıt `openingHours.monday`
  alanında `opening_hours.overlap` taşır.
- **AC-17.** Aynı günde 09:00–13:00 ve 13:00–18:00 `422` ve `opening_hours.overlap` döner.
- **AC-18.** `funday` gün koduyla kaydetme `422` döner. Yanıt `openingHours.funday` alanında
  `opening_hours.unknown_day` taşır.
- **AC-19.** 16:00–21:00 ve 09:00–13:00 bu sırayla gönderildiğinde kabul edilir. Okuma yanıtında
  aralıklar 09:00–13:00, 16:00–21:00 sırasıyla döner.
- **AC-20.** Hatalı programla yapılan kaydetmede şubenin hiçbir alanı değişmez. Aynı kaydetmedeki
  geçerli adres değişikliği de yazılmaz.
- **AC-21.** Pazartesi ve Çarşamba'da hatası olan kaydetmenin yanıtı iki günün hatasını da taşır.
- **AC-22.** Panelde alan hatası ilgili aralığın ya da günün yanında, ekranın dilinde görünür.
  Girilen değerler formda kalır.
- **AC-23.** Pazartesi 00:00–23:59 kabul edilir.

### Opsiyonellik, saat dilimi ve sürüm (R2, R3, R4, R6 · D2, D4, D11)

- **AC-24.** Yedi günü boş program gönderen kaydetme kayıtlı programı siler. Okuma yanıtında
  `openingHours` `null`'dır ve belge o şubenin öğesinde `openingHoursSpecification` taşımaz.
- **AC-25.** Gövdesinde program alanı hiç olmayan kaydetme (`0020`'nin gövdesi) kayıtlı programı
  değiştirmez.
- **AC-26.** Şubenin saat dilimi `Europe/Istanbul`'dan `Asia/Kuwait`'e değiştirildiğinde, kayıtlı
  Pazartesi 09:00–18:00 okuma yanıtında ve belgede 09:00–18:00 olarak kalır.
- **AC-27.** Eski sürümle gönderilen ve programı değiştiren kaydetme `409` döner. Program değişmez.
- **AC-28.** Programı değiştiren başarılı bir kaydetme şube sürümünü bir artırır ve yanıt yeni
  sürümü döner.
- **AC-29.** Kapatılmış şubenin programını değiştiren kaydetme `409` ve `branch.closed_read_only`
  döner. Program değişmez.
- **AC-30.** Geçici kapalı şubenin programı değiştirilebilir. Şube yeniden açıldıktan sonraki ilk
  `/tr/` belgesi yeni programı taşır.
- **AC-31.** Programı değiştiren kaydetme outbox'a hiçbir entegrasyon olayı yazmaz.

### Yetki (R10)

- **AC-32.** Satış Danışmanı oturumuyla programı değiştiren şube kaydetmesi `403` döner ve program
  değişmez.
- **AC-33.** İçerik Editörü oturumuyla aynı istek `403` döner ve program değişmez.
- **AC-34.** Oturumsuz aynı istek `401` döner.
- **AC-35.** Satış Danışmanı panelin şube düzenleme adresini doğrudan açtığında "erişim yok"
  ekranını görür. Çalışma saatleri bölümü basılmaz.
- **AC-36.** Satış Danışmanının okuduğu şube listesi yanıtındaki öğelerin anahtar kümesi bu
  karttan önceki kümeyle aynıdır.

### Panel durumları ve dil (R11 · D5)

- **AC-37.** Şube okunurken ekranın yükleniyor durumu görünür ve çalışma saatleri bölümü basılmaz.
- **AC-38.** Şube okuması hata verirse ekranın hata durumu görünür. Çalışma saatleri bölümü basılmaz.
- **AC-39.** `ar` panelinde bölümün gün adları ve metinleri Arapçadır ve bölüm sağdan sola dizilir.
  Saat alanlarının değeri `HH:mm` biçimindedir.
- **AC-40.** `GET /api/admin/branches/{id}` yanıtı programı D8'in biçimiyle taşır. Programsız
  şubede değer `null`'dır.

### Ziyaretçiye dönük okuma (R9 · C3, D8)

- **AC-41.** `/api/dealership/gallery` yanıtında her şube öğesinin anahtar kümesi tam olarak
  `address`, `name`, `openingHours`, `phone`'dur. Kök anahtar kümesi `0024` AC-33'teki gibidir.
- **AC-42.** Programlı şubenin `openingHours`'u tam olarak `monday` … `sunday` yedi anahtarını taşır.
  `sunday` boş dizidir ve `saturday` `[{"start":"10:00","end":"14:00"}]`'dir.
- **AC-43.** `locale=tr`, `locale=ar` ve `locale=en` yanıtlarında aynı şubenin `openingHours`'u aynıdır.

### Vitrin altlığı (R4, R7, R11 · D6, D7)

- **AC-44.** Programlı şubenin `/tr/` altlığındaki öğesinde, adres ve telefondan sonra yedi satır
  vardır. Sırası Pazartesi … Pazar'dır. Pazartesi–Cuma satırları "09:00–18:00", Cumartesi
  "10:00–14:00", Pazar "Kapalı" gösterir.
- **AC-45.** Bir günü 09:00–13:00 ve 16:00–21:00 olan şubenin o satırı iki aralığı bu sırayla gösterir.
- **AC-46.** Programsız şubenin öğesinde saat listesi yoktur. Ad, adres ve telefon bugünkü gibi görünür.
- **AC-47.** `/ar/` altlığında gün adları ve "Kapalı" Arapçadır. Saatler Latin rakamla basılır ve
  aralık soldan sağa dizilir.
- **AC-48.** `/en/` altlığında gün adları İngilizcedir ve saatler 24 saat biçimindedir ("09:00–18:00").
- **AC-49.** Şubenin saat dilimi `Asia/Kuwait` ve tarayıcının saat dilimi `Europe/Istanbul` iken
  altlık Pazartesi için "09:00–18:00" gösterir.
- **AC-50.** Galeri okuması sürerken şube bloğunun bugünkü iskeleti görünür. Saat listesinin ayrı
  bir yükleniyor göstergesi yoktur.
- **AC-51.** Galeri okuması hata verdiğinde şube bloğu ve saat listesi basılmaz ve hata metni yoktur.

### Yapısal veri (R8 · C4 · D9, D16)

- **AC-52.** Programlı şubenin `department` öğesi `openingHoursSpecification` taşır. Dizinin tam
  olarak üç öğesi vardır, sırasıyla:
  `["Monday","Tuesday","Wednesday","Thursday","Friday"]` 09:00–18:00;
  `["Saturday"]` 10:00–14:00; `["Sunday"]` 00:00–00:00.
- **AC-53.** Pazartesi 09:00–13:00 ve 16:00–21:00, Salı 09:00–13:00, diğer günleri kapalı bir
  şubenin dizisi sırasıyla şunlardır: `["Monday","Tuesday"]` 09:00–13:00; `["Monday"]` 16:00–21:00;
  `["Wednesday","Thursday","Friday","Saturday","Sunday"]` 00:00–00:00.
- **AC-54.** Her öğenin anahtar kümesi tam olarak `@type`, `dayOfWeek`, `opens`, `closes`'tur ve
  `@type` `OpeningHoursSpecification`'dır.
- **AC-55.** `AutoDealer` bloğunda (kökte ve `department` öğelerinde) `geo` ve `logo` anahtarları
  yoktur. (`0004` AC-52'nin bu karttaki hâli.)
- **AC-56.** Programsız şubenin öğesinde `openingHoursSpecification` anahtarı yoktur. Kök `AutoDealer`
  hiçbir kurulumda `openingHoursSpecification` taşımaz.
- **AC-57.** Geçici kapalı şube, programı olsa bile `department`'ta yer almaz.
- **AC-58.** Yönetici programı kaydettikten sonra istenen ilk `/tr/` belgesi yeni programı taşır.
- **AC-59.** Programlı şubeler varken `/tr/` belgesi tam olarak bir JSON-LD bloğu taşır.

### Performans ve paket (C5 · D10)

- **AC-60.** Tek şubeli kurulumda, kurgulanmış en kötü programlı şubenin `/ar/` ana sayfa belgesi,
  aynı şubenin programsız hâlinden en çok **2304 B** büyüktür. Kurgu: yedi günde üçer aralık ve
  21 aralığın hepsinin (`opens`, `closes`) çifti farklı.
- **AC-61.** İlk paket (giriş JS + giriş CSS, `gzip -9`, ADR 0013) `main` tabanına göre en çok
  **512 B** büyür.
- **AC-62.** Perf koşusu her aktif şubeye AC-60'ın programını tohumlar. C3 ana sayfa, liste ve
  detayda yeşildir. Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşunun sonucu ve LCP öğesi
  PR'da yazılıdır.
- **AC-63.** Bu kartla vitrin kataloğuna yalnız "Kapalı" ve saat listesinin erişilebilir etiketi
  eklenir. Panelin metinleri panel kataloğundadır.

### Şema ve çeviri (C6 · DoD T5, T6)

- **AC-64.** Bu kartın migration'ı yalnız `dealership` şemasına dokunur.
- **AC-65.** Panel bölümünün metinleri (başlık, yardım metni, "Aralık ekle", "Kaldır", "Önceki günle
  aynı", "Kapalı" ve beş hata kodu) ile vitrinin iki metni tr, ar ve en'de vardır. Eksik anahtar
  kontrolü yeşildir.

---

## Definition of Done

- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0025-ac<n>.png`),
      boş ve hata durumları dâhil; panel bölümünün ve altlığın Arapça görüntüsü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor (`dealership`, D13).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi ("Çalışma Programı", D15).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. Bilerek
      değişen eski testler (§ Context tablosu ve uygulamada bulunan ekler) PR'da listelendi.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı. Taşımadan sonra `tests/Architecture.Tests`'in
      TAMAMI koşturuldu.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | | `git cat-file -e <impl>^:specs/0025-sube-calisma-saatleri.md` |
| **Kriter sayısı: önce → bugün** | → | |
| **Spec revizyon sayısı** | | Onay anındaki CLARIFY turu sayılır (ADR 0008 / K3). |
| **Düzeltme turu sayısı** | | `0` yalnız T10 `[x]` ise. |
| **Dış bulgu (gerçek/gürültü)** | / | |
| **Uygulama içi bulgu** | | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | Arşive taşınırken `izleniyor`. |

**Bu spec'ten çıkarılan ders:**
