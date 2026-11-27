# 0027 — Çalışma saati istisnaları ve "şu an açık"

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-27 — Proje Yöneticisi kararı) · QA CLARIFY turu işlendi (QA 1, PR #141 @ `c51e167`: 12 bulgu, 0 engelleyici / 8 önemli / 4 küçük) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (özel gün, "şu an" hesabı, okumalar) · `Host` (ana sayfanın yapısal verisi) · Frontend (panel şube formu, vitrin altlığı) |
| **Bağımlı spec'ler** | **`0025`** (**X1**, **X2**, **X3**, **D1**–**D12**: haftalık program, okuma biçimi, altlık, yapısal veri) · `0003` (**B4**, **C10**, saat dilimi) · `0004` (**D1**, **D15**, **D16**, **C13**) · `0014` (altlık, **D15** izin listesi) · `0020` (şube formu, **R16** sürüm, **D21**) · `0024` (**AC-55** ilk paket tanımı, okuma kökü) · `0026` (üst çubuk ve CLS dersleri) · `frontend-003` (**D18**, ilk paket ölçüm yöntemi) |
| **Karar bekliyor** | yok — D1–D18 Proje Yöneticisi kararı (2026-11-27, üç ek şartla: (a)–(c)); SS1–SS4 sahip kararı (2026-11-27). CLARIFY B1–B12 Proje Yöneticisi kararı (2026-11-27). |

> **Sahibin isteği (sahip kararı, 2026-11-26 ve 2026-11-27):** `0025` **SS1** → bayram ve tatil
> istisnaları ile "şu an açık" göstergesi `0025`'in hemen ardından gelsin ve ikisi aynı kartta olsun
> (`0025` X1, X2). 2026-11-27: "tatil günleri ve şu an açık ile başla".
>
> **Sahibin cevapları (sahip kararı, 2026-11-27):** SS1–SS4'ün dördü de önerilen yönde: resmî
> tatiller elle girilir (D5), özel gün şube başınadır (D2), gösterge yalnız ana sayfa altlığındadır
> (D1), metin ek almaz (D10).
>
> **Proje Yöneticisi kararı (2026-11-27):** D1–D18 onaylandı. Üç ek şart işlendi:
> - **(a)** D15: tembel parçanın iskeleti ve `0025`'in etkilenen testleri → D15, D11, Context tablosu,
>   AC-68, **AC-75–AC-78**;
> - **(b)** D9'un sınırları (başarısız yeniden okuma, geçersiz süre, yaz saati günü) → D8, D9, AC-61,
>   **AC-79–AC-85**;
> - **(c)** yaz saati geçişinde "şu an" ve `next` → D7, **AC-86–AC-91**.
> Var olan numaralar değişmedi. Yalnız AC-61 ve AC-68'in metni şartlarla değişti.
>
> **QA CLARIFY turu (QA 1, 2026-11-27; Proje Yöneticisi kararları):** B1–B12 işlendi.
> - Değişen kararlar "CLARIFY" diye işaretli: **D2**, **D4**, **D7**, **D9**, **D11**, **D14**,
>   **D15**, **D17**.
> - Metni değişen kriterler: AC-10, AC-41, AC-61, AC-68, AC-69, AC-71.
> - Yeni kriterler: **AC-92–AC-107**.
> - Proje Yöneticisi iki çakışmada `c51e167`'nin kuralını korudu:
>   - B3(ii): `next.time` gerçek anı verir ("03:00").
>   - B5: AC-79/80'in kuralı geçerli; süre `performance.now()` ile ölçülür.

---

## Intent

Bir aracı görmeye gitmek isteyen ziyaretçi bugün şubenin haftalık saatlerini görüyor. Ancak
bayramda, yılbaşında ya da Ramazan'da vitrin yine haftalık saati gösteriyor. Ziyaretçi kapalı bir
şubeye gidebiliyor, arama motoru da yanlış saat yayıyor. Ziyaretçi ayrıca saat listesine bakıp
"şimdi gidebilir miyim?" sorusunu kafasında, şubenin saatine göre hesaplamak zorunda.

Bu iş bittiğinde yönetici her şube için tatil ve özel saatli günleri (ör. tek gün kapalı, bayram
boyunca kapalı, Ramazan boyunca kısa mesai) panelden tarihleriyle girer. Ana sayfanın altlığında
her şubenin yanında "Şu an açık · Kapanış 18:00" ya da "Şu an kapalı · Açılış yarın 10:00" yazar.
Bu yazı özel günleri de hesaba katar. Yaklaşan özel günler de saat listesinin altında görünür.
Arama motoru da aynı özel günleri şubenin yapısal verisinden okur.

---

## Requirements

- **R1.** Yönetici her şube için **özel gün** kayıtları girer. Bir kaydın başlangıç ve bitiş tarihi
  vardır; tek günlük kayıtta ikisi aynıdır. Kayıt ya bu tarihler boyunca tam gün kapalıdır ya da her
  gün aynı olan 1–3 saat aralığı taşır (D2).
- **R2.** Özel günün kapsadığı tarihte haftalık program **geçmez**. O günün saatleri özel günün
  saatleridir (`docs/domain.md` **B3**, D3).
- **R3.** Özel günler şubenin kendi takvim günleridir. Saatler şubenin yerel saatidir (`0025` **D2**).
- **R4.** Kurallara uymayan bir özel gün listesi alan hatasıyla reddedilir. Bu durumda şubenin
  hiçbir alanı değişmez ve sunucu hiçbir girdide `500` dönmez.
- **R5.** Özel günler şube formunun parçasıdır. Şubeyle aynı kaydetmede yazılır ve aynı eşzamanlı
  yazma korumasına tabidir (`0020` **R16**). Bunları yalnız **Yönetici** düzenler.
- **R6.** Bitmiş özel günler (son günü şubenin bugününden önce olan) hiçbir yerde gösterilmez ve
  saklanmaya devam etmez (D4).
- **R7.** Ana sayfa altlığında, haftalık programı girilmiş her şubenin yanında **"şu an" satırı**
  bulunur. Satır şubenin şu an açık mı kapalı mı olduğunu ve bir sonraki değişimin saatini söyler.
  Bu bilgi şubenin saat diliminde, özel günler dahil hesaplanır (**B4**).
- **R8.** Ziyaretçinin cihazının saati ya da saat dilimi yanlış olsa da "şu an" satırı doğru kalır (D6).
- **R9.** Sayfa açık kalırken şube açılır ya da kapanırsa satır, sayfa yenilenmeden yeni durumu gösterir (D9).
- **R10.** Haftalık listede **bugün** belirgindir. Altlık yaklaşan özel günleri listeler (D11).
- **R11.** Ana sayfanın yapısal verisi yaklaşan özel günleri şubenin öğesinde taşır (D12).
- **R12.** Panel bölümü ve vitrin metinleri TR/AR/EN'de çevrilidir. Arapçada sağdan sola dizilir.
  Tarih ve saat her dilde Latin rakamla, Miladi takvimle basılır.
- **R13.** Açık ya da kapalı bilgisi yalnız renkle verilmez.

---

## Constraints

### Uyulması zorunlu

- **C1.** Kodda hafta sonu, tatil günü, bayram tarihi, ülke takvimi ya da tek açılış/kapanış saati
  **varsayılmaz** (Yasak Listesi 34; `0025` C1). Sabit resmî tatiller de koda yazılmaz (D5, SS1).
- **C2.** "Şimdi" yalnız saat soyutlamasından (`IClock`) okunur. Sunucunun yerel saati ya da saat
  dilimiyle açık/kapalı hesaplanmaz (Yasak Listesi 30, 35). Hesap şubenin IANA saat diliminde yapılır.
- **C3.** Ziyaretçiye dönük okuma bir **izin listesidir** (`0014` **D15**). Şube öğesine yalnız
  D8'in iki anahtarı eklenir. Şubenin saat dilimi ziyaretçiye gönderilmez (D6).
- **C4.** Yapısal veri sayfada görünmeyen bir bilgi taşımaz (`0004` **D1**). Altlıkta görünmeyen özel
  gün yapısal veride de yoktur (D12). Ana sayfa tam olarak bir JSON-LD bloğu taşır.
- **C5.** Performans: C3 (LCP < 2500 ms, CLS < 0.1) ana sayfa, liste ve detayda yeşil kalır. İlk
  paket `0024` **AC-55**'in tanımıyla ölçülür. Panel metinleri panel kataloğuna girer (D15).
- **C6.** Migration yalnız `dealership` şemasına dokunur.
- **C7.** Özel günlerin tek yazma sahibi `Dealership`'tir. Başka modüle kopyalanmazlar ve entegrasyon
  olayı üretmezler (Yasak Listesi 33; `0025` C7).
- **C8.** Hata yanıtları kararlı bir `errorCode` ve alan anahtarı taşır (`0020` **C5**). Biçim
  doğrulaması **tek bir kapıdan** geçer. Değeri ayrıştıran çağrı aynı zamanda doğrulayan çağrıdır
  (`0025` ders: düzenli ifade + `ParseExact` iki kapıydı ve `500` üretti).
- **C9.** Haftalık programın davranışı (`0025` AC-1–AC-72) değişmez. Yalnız Context tablosundaki
  testler bilerek değişir.
- **C10.** Bu kart `0025`'in `main`'deki hâlini (`9912ade`) temel alır.

### KAPSAM DIŞI

- **X1.** **Sabit resmî tatillerin otomatik gelmesi ve dini bayramların hesaplanması** — *neden:*
  tatil takvimi ülkeye ve yıla göre değişiyor. Dini bayramlar ay gözlemine bağlı ve resmî ilanla
  kesinleşiyor, projede takvim kaynağı ya da kütüphane yok. Bazı galeriler resmî tatilde açık (D5, **SS1**).
- **X2.** **Tek kayıtla bütün şubelere özel gün girmek** — *neden:* domain programı şubeye bağlıyor
  (**B1**, **B3**). Çok şubeli toplu yazma birden çok sürüm ve kısmi başarı demek (D2, **SS2**).
- **X3.** **İlan detayında ve başka sayfalarda "şu an açık"** — *neden:* altlık yalnız ana sayfada
  (`0024` X4). İlan detayı şubenin yalnız adını `VehicleCatalog`'un kopyasından okuyor
  (`ShowcaseListingDetailContracts.cs:40`) ve şubenin kimliğini taşımıyor. Detayda durum göstermek
  modüller arası yeni bir okuma ister (**SS3**).
- **X4.** **Özel günün adı** ("Kurban Bayramı") — *neden:* üç dilde girilecek bir metin alanı, yani
  `0003`'ün dil başına metin kuralları demek. Tarih ve saat ziyaretçinin sorusunu cevaplıyor (D2).
- **X5.** **Tekrarlayan kural** ("her yıl 1 Ocak", "her ayın ilk Pazartesisi") — *neden:* X1 ile aynı.
  Kayıt tarih aralığıdır.
- **X6.** **Haftalık programın bir tarihten sonra değişmesi** (ör. "1 Mart'tan itibaren yeni saatler")
  — *neden:* bu bir istisna değil, yeni bir haftalık programdır ve kaydetme anında değiştirilir. Uzun
  süreli istisna olarak girilebilir (D4: en çok 60 gün).
- **X7.** **Şube sayfası** — *neden:* `0004` **X3**, `0025` X6.
- **X8.** **Saatin ziyaretçinin saat dilimine çevrilmesi** — *neden:* `0025` **X5**, **D2**.
- **X9.** **Hicri takvim gösterimi** — *neden:* `docs/architecture.md:486` (Körfez'de ticari işlem
  Miladi takvimle yürür).
- **X10.** **"Şu an" bilgisinin yapısal veride ya da belgede yer alması** — *neden:* schema.org'da
  karşılığı yok ve belge değişmeyen bir an taşır. Bilgi yalnız altlıkta, istemcide basılır (D8).
- **X11.** **Haftanın ilk günü, 12 saat biçimi ve gün gruplama** — *neden:* `0025` X4, X7, X10 aynen geçerli.
- **X12.** **Geçici kapalı şubenin vitrinde görünmesi** — *neden:* `0025` X8. Geçici kapalı şube
  vitrinde yoktur, "şu an" satırı da yoktur.

---

## Context

> Her iddia `main` @ `9912ade` üzerinde ölçüldü. Satır numaraları o commit'e aittir.

### Domain ve mimari kurallar

- **B3** (`docs/domain.md:309-310`): "Tarih aralıklı istisna programı (Ramazan, bayram, geçici
  kapanış) haftalık programı ezer." **B4** (`:312`): "'Şu an açık mı?' her zaman şubenin kendi saat
  diliminde hesaplanır." `docs/architecture.md:41-42` de aynı iki kuralı yazıyor ve ayrıca "'Şu an
  açık mı?' **uçta** ... hesaplanır" diyor. Bu belgede "uçta" sunum ucu demek
  (`docs/conventions.md:198`, `docs/architecture.md:94`, `:284`). **D6** bu cümleden ölçülmüş bir
  gerekçeyle ayrılıyor ve cümleyi günceller (D17).
- Körfez'de çift mesai (09:00–13:00 ve 16:00–21:00) ve Cuma–Cumartesi tatili; Türkiye'de Pazar
  (`docs/architecture.md:36-40`). Kriter örnekleri ikisini de kapsar.
- Yasak Listesi (`docs/architecture.md:438`, `:446-447`): 30 (`DateTime.Now`, sunucu yerel saati),
  34 (hafta sonu ve tek açılış varsayımı), 35 (sunucu saat dilimiyle açık/kapalı hesabı).

### Kod doğrulamaları

**1. Şube saat dilimi nerede?** `Branch.TimeZone` (`Domain/Branch.cs:52`), tipi `TimeZoneId`.
`dealership.branches.time_zone` kolonunda saklanıyor (`BranchConfiguration.cs:49-52`). Değer bir IANA
kimliği: "Bölge/Şehir" biçimi zorunlu ve tzdata'da doğrulanıyor (`Domain/TimeZoneId.cs:22-41`).
Belge yorumu (`:8-10`) sabit farkın "şu an açık mı" hesabını yılda iki kez bozacağını zaten yazıyor.
Değişiklik `ChangeTimeZone` ile yapılıyor (`Branch.cs:163`, `:253`). **Saat dilimi bugün hiçbir public
okumada yok**: `PublicBranchDto(Name, Address, Phone, OpeningHours)`
(`Dealership.Contracts/Gallery/PublicGalleryContracts.cs:34-38`).

**2. Saat soyutlaması nasıl kullanılıyor?** `IClock` yalnız `DateTimeOffset UtcNow` taşıyor
(`src/BuildingBlocks/IClock.cs:7-15`). Her modül `SystemClock`'u kendisi kaydediyor; `Dealership` de
`DealershipModule.cs:71`'de kaydediyor. `Dealership` saati bugün yalnız duyuru yazarken okuyor
(`Application/BranchIntegrationEvents.cs:21`). Okuma tarafında (`PublicGalleryQueries`) saat yok. Host
testleri saati `ConfigureTestServices(services => services.AddSingleton<IClock>(clock))` ile değiştirebiliyor
(`tests/Host.Tests/OturumBoruHattiTests.cs:76-78`; yorum: "saat en SONDA değiştirilir, modüller kendi
`IClock`'larını kaydediyor"). Yani "şu an" kriterleri sabit bir saatle **deterministik** ölçülebilir.
Bir saat diliminin yerel tarihini veren yardımcı yok. `Dealership`'te yeni olacak.

**3. Çalışma saatleri hangi yüzeylerde basılıyor?** Üç yerde:
- Panel şube formu, `frontend/src/features/dealership/admin/OpeningHoursFieldset.tsx` (`BranchEditPage.tsx` içinde).
- Ana sayfa altlığı, `frontend/src/features/dealership/BranchList.tsx:62` ve `:76-109`. Yedi satır
  `<dl>`, gün adı `weekdayName` ile UTC sabit Pazartesi'den türetiliyor (`shared/i18n/format.ts:66-74`).
- Ana sayfanın JSON-LD'si, `src/Host/Seo/SeoHeadBuilder.cs:139-143` ve `:168-206`
  (`openingHoursSpecification`).

Altlık **yalnız ana sayfada** var: `App.tsx:139-160` `HomeRoute` `HomeFooter`'a `BranchList` veriyor.
Liste ve detay sayfalarının altlığı şube taşımıyor. İlan detayı şubenin yalnız adını basıyor
(`ListingDetailPage.tsx:140`, `detail.branchName`). Bu ad `VehicleCatalog`'un projeksiyonundan geliyor
(`ShowcaseListingDetailContracts.cs:40`) ve şube kimliği ya da slug'ı taşımıyor (→ X3).

**4. Vitrin bu veriyi nasıl alıyor?** **Ayrı istekle, ilk boyamada değil.**
- `useGallery` (`features/dealership/useGallery.ts:15-37`) `HomeRoute` açılınca
  `GET /api/dealership/gallery?locale=…` çağırıyor (`shared/api/dealership.ts:10`). Önbellek yok
  (yorum `:12-13`) ve okuma yalnız dil değişince tekrarlanıyor.
- Altlık okuma sürerken iskelet basıyor (`BranchList.tsx:17-30`).
- Belge (`IndexHtmlRenderer`) galeri okumasını yalnız `<head>` için kullanıyor: `SeoHeadBuilder` aynı
  `IPublicGalleryQueries`'ten JSON-LD kuruyor (`PublicGalleryContracts.cs:57-68`, `0004` P9).
  Gövdeye şube verisi basılmıyor.

**5. Önbellek / ETag?**
- Belge `Cache-Control: no-store` (`IndexHtmlRenderer.cs:150-154`, `0022` D20). Bayat bir belge
  kopyası yok.
- `GET /api/dealership/gallery` (`Endpoints/PublicGalleryEndpoints.cs:19-35`) hiçbir önbellek başlığı
  koymuyor. `ETag`, `Last-Modified` ya da çıktı önbelleği yok (`src` altında `ETag`, `OutputCache`,
  `ResponseCaching` geçmiyor). Tarayıcı `Last-Modified` olmayan bir yanıtı sezgisel olarak önbelleğe
  almaz. Yine de yanıt artık zamana bağlı olacağı için bu bir **varsayım değil, kural** olmalı (D8: `no-store`).

**Tasarım sorusu: "şu an"ı sunucu mu hesaplasın, istemci mi?** Seçenekler ölçüldü:

| | Sunucu hesaplar (öneri, **D6**) | İstemci hesaplar (`architecture.md:42`'nin "uçta"sı) |
|---|---|---|
| Doğruluğun dayandığı saat | sunucunun saati (`IClock`, NTP) | ziyaretçi cihazının saati. Yanlışsa yanlış durum basar |
| Saat dilimi verisi | sunucunun tzdata'sı. Saat dilimi zaten onunla doğrulanıyor (`TimeZoneId.cs:35`) | tarayıcının ICU verisi. Sunucuyla farklı sürümde olabilir |
| Public yanıta eklenen | durum, sonraki değişim, yenileme süresi | şubenin saat dilimi ve tüm özel günler (izin listesi genişler) |
| B3 öncelik kuralı | tek yerde, `Dealership` domain'inde | TypeScript'te ikinci bir kopya |
| İlk paket | biçimlendirme | biçimlendirme + saat dilimi hesabı + "sonraki açılış" araması |
| Test | Host testi sabit `IClock` ile deterministik (soru 2) | jsdom'da sahte saat. Sunucuyla çelişki ölçülmez |
| Sayfa açık kalırsa | değişim anında galeri yeniden okunur (D9) | istemci kendisi yeniden hesaplar, istek yok |

**Tarayıcı saati ve saat dilimi yanlışsa ne olur (D6 ile):**
- Saat dilimi farklıysa hiçbir şey değişmez. Gösterilen saatler şubenin duvar saatidir (`0025` AC-49).
- Saat yanlışsa da durum değişmez. Durumu sunucu hesaplar. Yenileme süresi sunucudan **saniye
  cinsinden göreli** gelir ve istemcinin duvar saatiyle karşılaştırılmaz.

### Bugünkü veri ve okuma biçimi (`0025`)

- Haftalık program `dealership.branches.opening_hours jsonb NULL` (`0025` P3). `null` = girilmemiş (`0025` D4).
- Kaydetme gövdesinde alan yoksa ya da açık `null`'sa kayıt değişmez. Harita bütünüyle yerine geçer
  (`0025` D11, P1). Bu kart özel günlerde aynı kuralı izler (D13).
- Saat biçimi `HH:mm`, aralık kuralları `0025` D3, hata kodları `opening_hours.*` (`Domain/OpeningHours.cs:30-35`).
  Özel günün aralıkları **aynı kuralları ve aynı kodları** kullanır (D14).
- JSON-LD'de kapalı gün `00:00–00:00` ile kodlanıyor. Değer `TimeOnly.MinValue`'dan türetiliyor,
  çünkü Yasak 34 kapısı `"HH:mm"` sabitini arıyor (`SeoHeadBuilder.cs:170-173`; `0025` uygulama içi bulgu 1).
  Özel günün kapalı kodlaması da aynı yoldan geçmelidir.

### Ölçümler

- **İlk paket kod payı (B5 deseni, `frontend-003` D18):** `main` @ `9912ade` derlendi. Taslak bir
  "şu an" satırı + özel gün listesi + yenileme zamanlayıcısı küçültülüp giriş `index-*.js`'inin
  sonuna eklendi ve marjinal `gzip -9` alındı. Sonuç: JS ≈805 B, üç dilin metinleri ≈284 B, CSS
  ≈102 B, toplam **≈1,2 KB**. `0024` AC-55'in +512 B bütçesi bu kod ilk pakete girerse aşılıyor.
  → **D15**: saat bloğu (haftalık liste dahil) tembel parçaya taşınır.
- **Yapısal veri baytı:** bir özel gün aralığı öğesi
  (`{"@type":"OpeningHoursSpecification","validFrom":"2026-12-01","validThrough":"2026-12-03","opens":"10:00","closes":"13:00"}`,
  virgülüyle) **124 B**. Anahtar ve dizi 38 B. En kötü durum 12 özel gün × 3 aralık = 36 öğe
  ≈ **4502 B** (→ D12, AC-63). `0004` **AC-83**'ün 6144 B'ı **ilan detayı** belgesini ölçer.
  `department` yalnız ana sayfada var (`SeoHeadBuilder.cs:128-146`), yani bu kart AC-83'ü etkilemez.
  Ana sayfanın şube başına bütçesi `0025` **D10(a)** deseninde ayrı tanımlanır.

### Bilinen tuzaklar

1. **Türkçe ek saate göre değişir.** "19:00'da", "20:00'de", "13:00'te", "09:45'te" (ünlü uyumu ve
   sert ünsüz okunuşa bağlı). ICU mesajı bunu çözmez. Ekli bir kalıp her saat için ayrı ek ister.
   → **D10**: ek almayan biçim ("Kapanış 19:00"), **SS4**.
2. **23:59 gün sonudur.** `0025` D3 aralığı en geç 23:59'da bitiriyor ve 24 saat açık şube
   00:00–23:59 giriyor. Aralık `[başlangıç, bitiş)` diye okunursa her gece 23:59–00:00 arası "kapalı"
   görünür ve "Kapanış 23:59" yazılır. → **D7**.
3. **Gece yarısını geçen aralık iki güne bölünmüş girilir** (`0025` X3: Cuma 20:00–23:59 + Cumartesi
   00:00–02:00). Bölünme fark edilmezse Cuma 23:30'da "Kapanış 23:59" yazılır, doğrusu "Kapanış yarın
   02:00"dır. → **D7**.
4. **Yaz saati.** `Europe/Istanbul` ve Körfez bölgelerinde yaz saati yok, ama şube herhangi bir IANA
   bölgesinde olabilir. Sabit fark ya da "bugün + 24 saat" hesabı geçiş gününde bir saat kayar. → **AC-39**.
5. **"Bugün" de zamanla değişir.** Gece yarısı "yarın 10:00", "10:00"a dönmeli ve vurgulanan gün
   değişmelidir. Yenileme yalnız açılış ve kapanışta yapılırsa vurgu bayat kalır. → **D9**: yenileme
   anı, sıradaki değişim ile şubenin gece yarısının erken olanıdır.
6. **Geçmiş özel gün, gidiş-dönüşte hata üretebilir.** Panel okumasından gelen bir kayıt, yönetici
   formu açık tutarken gece yarısını geçerse kaydetme anında "geçmiş" olur. Bu hata sayılırsa ilgisiz
   bir adres değişikliği `422` alır. → **D4**: geçmiş kayıt hata değildir, sessizce düşer.
7. **Arka plandaki sekmede zamanlayıcı kısılır.** `setTimeout` gecikebilir ya da hiç çalışmaz. →
   **D9**: sekme görünür olunca süre dolmuşsa okuma tekrarlanır.
8. **`ar` takvimi.** `intlLocale` `ar-u-nu-latn` üretiyor (`shared/lib/locale.ts:18-20`). Rakam
   sistemini sabitliyor, **takvimi sabitlemiyor**. Bölgeli bir Arapça yerel (ör. `ar-SA`) varsayılan
   olarak Hicri takvimle biçimlendirir. Tarih biçimi Miladi takvimi **açıkça** istemelidir (R12, X9, AC-49).
9. **e2e sunucunun saatini değiştiremez.** Playwright tarayıcının saatini değiştirir (`page.clock`),
   Host'unkini değiştirmez. Durum metninin tam ölçümü Host testinde sabit `IClock` ile yapılır. e2e
   veriyi şubenin **o anki** tarihine göre kurar ve tarayıcı saatinin etkisizliğini ölçer (AC-58).
10. **Tembel parça CLS doğurabilir.** Galeri okuması parçadan önce gelirse önce adres basılır, sonra
    saat bloğu araya girer. → **D15**: iskelet ikisi de hazır olana kadar durur (AC-68).

### Bilerek değişecek eski testler (kod taranarak çıkarıldı)

| Test | Bugün ölçtüğü | Bu kartla |
|---|---|---|
| `tests/Host.Tests/GaleriPublicUcTests.cs:34` (`0014/AC-14`, `0025/AC-41`) | şube öğesi `address, name, openingHours, phone` | `address, name, openNow, openingHours, phone, specialHours` (**AC-42**). Çok etiketli: `0014/AC-14, 0025/AC-41, 0027/AC-42` |
| `tests/Host.Tests/CalismaSaatleriZinciriTests.cs:382-390` (`0025/AC-41–43`) | aynı küme | aynı yeni küme, etiket eklenir |
| `PublicBranchDto` kuran testler (`tests` altında 10 çağrı) | dört parametreli kurucu | kurucu değişir. Programsız şubede beklenen değer `openNow: null`, `specialHours: []` ile aynı |
| `frontend/src/features/dealership/BranchList.test.tsx` | saat listesi eşzamanlı basılıyor | saat bloğu tembel parçada (D15): `findBy…` ile beklenir, beklenen metin değişmez |
| `frontend/src/app/HomePage.test.tsx`, `GaleriPaneli.test.tsx`, `BultenBolumu.test.tsx`, `AnaSayfaMetinleri.test.tsx` | `PublicGallery` sahte şubeleri | şube tipi değişir (`openNow`, `specialHours`). Beklentiler değişmez |
| `frontend/e2e/calisma-saatleri.spec.ts:135`, `:208` | panel okuması ve `branch-hours` | panel okuması `specialHours` taşır. `branch-hours` tembel parçada, Playwright beklemesi yeterli. Beklenti değişmez |
| `0025`'in kaydetme testleri (`CalismaSaatleriZinciriTests.cs`, `GaleriYonetimiZinciriTests.cs`) | özel günsüz gövde | **değişmez** (D13: alan yoksa değişmez, **AC-25**) |
| *(PM şartı (a))* `frontend/src/features/dealership/BranchList.test.tsx:47-51` (`satirlar`), `:88`, `:110` (`0025/AC-44–AC-48`) | `dt` metni = gün adı, saat listesi eşzamanlı | "Bugün" etiketi `dt`'de ayrı öğe (D11). Testler gün adı öğesini okur, yoksa sonuç takvime bağlanır. Liste tembel parçada, `findBy…`. Beklenen değerler değişmez |
| *(PM şartı (a))* `frontend/e2e/calisma-saatleri.spec.ts:188-207`, `:212-228` (`0025/AC-44`, `AC-47`, `AC-48`) | şube öğesinin **tümünde** `dt`/`dd` (`tr.locator('dt')`, `ar.locator('dd').last()`) | özel gün listesi de `dt`/`dd` taşır ve "Bugün" gerçek tarihe göre bir `dt`'ye girer. Sorgular `branch-hours` ve gün adı öğesiyle daraltılır. Beklenen değerler değişmez |
| *(PM şartı (a))* `frontend/src/app/admin/GaleriPaneli.test.tsx:1048-1056` (`0025/AC-63`) | vitrin kataloğunda `home.*` içinde `/closed\|hours/` eşleşen anahtarlar tam `[closed, hours]` | `home.footer.closedNow` da eşleşir. Küme bu kartın anahtarlarıyla genişler. Çok etiketli: `0025/AC-63, 0027/AC-73` |
| *(PM şartı (a))* `frontend/src/app/HomePage.test.tsx:261`, `:277`, `:299` ve `frontend/e2e/evidence-0014.spec.ts:63` (`0014` iskeleti) | galeri okunurken `branch-skeleton` | **değişmez** (D15 evre A). Evre B'nin iskeleti ayrı `data-testid` taşır |

> *(PM şartı (a))* **İlk boyamayı ya da ilk paketi ölçen `0025` testi yok:** `0025` AC-61 (ilk paket)
> PR #133'te elle ölçüldü ve test olarak yazılmadı. Haftalık listeyi belge HTML'inde arayan bir Host
> testi de yok, altlık istemcide basılıyor. Host ikizi: altlığı ölçen Host testi yok. e2e ikizi
> yukarıdaki iki satır. Görsel kanıtlar `0025-ac44.png` ve `0025-ac47-ar.png` yeniden üretilirse
> "Bugün" etiketini taşır. Kanıt dosyaları test değildir ve yeniden üretilmeleri gerekmez.

> **e2e ikizi:** Kapalı anahtar kümesi ölçen her Host satırının e2e ikizi arandı. Şube **öğesini**
> ölçen e2e yok. `evidence-0020.spec.ts:18` yalnız kökü ölçüyor ve kök değişmiyor. Uygulamada
> yeni bir küme testi eklenirse ikizi bu tabloya eklenir.

---

## Verilen kararlar (Proje Yöneticisi kararı, 2026-11-27)

Biçim: **karar** · *gerekçe* · *bedel*.

**D1 — Kapsam: özel günler + "şu an" satırı + bugün vurgusu + yaklaşan özel günler, yalnız ana sayfa
altlığında.** (R1–R13, X3, **SS3**)
*Gerekçe:* "şu an" bilgisi istisnasız yanlış olur (`0025` X2). İkisi birlikte gelmelidir. Altlık
şubenin vitrindeki tek yeri. İlan detayına taşımak modüller arası yeni bir okuma ister (X3).
*Bedel:* araç sayfasındaki ziyaretçi durumu görmek için ana sayfaya iner.

**D2 — Özel gün modeli.** (R1, R3, X2, X4, **SS2**)
- Kayıt = `from` ve `through` (ikisi dahil, `YYYY-MM-DD`, şubenin yerel takvim günü) + `intervals`.
- `from` = `through` tek gündür. *(CLARIFY B7:)* `through` yoksa ya da `null`'sa tek gündür
  (`through` = `from`), panelin "boş Bitiş" kuralıyla aynı (AC-3). Boş metin (`""`) geçersizdir.
- `intervals` **açık boş dizi** (`[]`) ise kayıt **tam gün kapalıdır**. *(CLARIFY B7:)* `intervals`
  yoksa ya da `null`'sa kayıt reddedilir: yok ≠ boş (`0024` X1 dersi). Doluysa 1–3 aralık taşır. Aralıklar `0025` D3'ün
  kurallarına uyar (çift mesai dahil) ve kaydın her gününe aynen uygulanır (ör. Ramazan boyunca
  10:00–16:00).
- Kayıt **şube başınadır**. Adı yoktur (X4).

*Gerekçe:* tek bir tarih aralığı tek günü, bayramı (3–4 gün) ve Ramazan'ı (≈30 gün) aynı biçimde
taşır. Haftalık programın aralık kuralları yeniden kullanılır.
*Bedel:* aynı tatil her şubeye ayrı girilir (iki şubeli kurulumda iki kayıt). Ramazan içinde
hafta sonu farklıysa iki ayrı kayıt gerekir.

**D3 — Öncelik: özel gün kapsadığı günü tamamen ezer. Çakışan özel gün reddedilir.** (R2, B3)
- Bir gün bir özel günün içindeyse o günün saatleri yalnız özel günün aralıklarıdır.
- Haftalık programla birleştirme ya da kısmi ezme yoktur.
- Aynı şubenin iki özel günü ortak bir tarih taşırsa kaydetme `422` döner (D14). Öncelik sırası yoktur.

*Gerekçe:* B3'ün en basit okunuşu bu. Çakışmaya izin vermek bir öncelik kuralı ve onun panelde
açıklanmasını gerektirir.
*Bedel:* "Ramazan içinde bayram" iki ayrı ve çakışmayan kayıt olarak girilir (Ramazan'ın sonu bayramın
arifesine çekilir).

**D4 — Sınırlar ve geçmiş özel günler.** (R6, tuzak 6)
- Şubenin **bugünü** (`IClock` + şubenin saat dilimi) ölçüt alınır. `through` < bugün olan kayıt
  **geçmiştir**.
- Geçmiş kayıt hiçbir okumada dönmez: panel okuması, ziyaretçi okuması, yapısal veri ve "şu an" hesabı.
- Kaydetme gövdesindeki geçerli ama geçmiş kayıt **hata değildir**. Sessizce düşer ve saklanmaz.
  Arka plan işi yoktur.
- *(CLARIFY B10:)* Kayıtlı geçmiş kayıtlar yalnız gövdesinde **`specialHours` alanı bulunan** başarılı
  bir kaydetmede silinir. Alan yoksa ya da `null`'sa saklanan hiçbir kayıt değişmez, geçmiş olanlar da
  (D13, AC-25, AC-106).
- *(CLARIFY B11:)* Aynı gövde saat dilimini de değiştiriyorsa "bugün", geçmiş kaydın düşmesi ve
  `too_far` **yeni** saat dilimine göre hesaplanır (AC-107).
- Bir şubede en çok **12** güncel ya da gelecek özel gün olur. Geçmiş kayıtlar sayılmaz.
- Bir kayıt en çok **60** gün sürer (`through − from + 1 ≤ 60`).
- `through` bugünden en çok **366** gün ileride olabilir.

*Gerekçe:* Türkiye'de bir yıllık resmî ve dini tatil yaklaşık 10–12 kayıttır. Ramazan 30 gün sürer.
Sınırlar bütçeyi (D12) ve panel uzunluğunu sınırlar. Geçmişi sessizce düşürmek gidiş-dönüşü güvenli
yapar (tuzak 6).
*Bedel:* geçmiş özel günlerin dökümü tutulmaz. Bir yıldan uzun ileriye kayıt girilemez.

**D5 — Tekrarlayan tatiller otomatik gelmez, hepsi elle girilir.** (C1, X1, X5, **SS1**)
- Sabit resmî tatiller (1 Ocak, 23 Nisan, 15 Temmuz…) koda ya da yapılandırmaya yazılmaz.
- Dini bayramlar (Ramazan, Kurban) ve Ramazan saatleri her yıl yönetici tarafından girilir.
- Panelin yardım metni bunu söyler.

*Gerekçe:* kurulum TR, KW, AE… olabilir ve tatil listesi ülkeye bağlı. Hicri bayramlar ay gözlemiyle
ilan ediliyor ve projede takvim kaynağı yok. Galerilerin bir kısmı resmî tatilde açık, yani otomatik
kapanış yanlış "kapalı" yazar. Yasak Listesi 34'ün ruhu da bu yönde.
*Bedel:* yönetici yılda ≈10 kayıt girer. Girmezse vitrin o gün haftalık saati gösterir (bugünkü
davranış).

**D6 — "Şu an" sunucuda, `Dealership`'te hesaplanır.** (R7, R8, C2, C3, Context tablosu)
- Durum, ziyaretçi okumasının (`GET /api/dealership/gallery`) her yanıtında `IClock.UtcNow` ve
  şubenin `TimeZoneId`'siyle hesaplanır.
- İstemci durumu hesaplamaz, yalnız basar.
- Tarayıcının saat dilimi ve saati sonucu etkilemez.
- Şubenin saat dilimi yanıtta yoktur.
- `docs/architecture.md:42`'deki "uçta" cümlesi bu kararla güncellenir ve Karar Defteri'ne
  yazılır (D17).

*Gerekçe:* B3'ün öncelik kuralı tek yerde ve sabit saatle test edilebilir kalır (Context soru 2).
Yanlış cihaz saati yanlış durum bastırmaz. İzin listesi saat dilimiyle genişlemez. İlk pakete hesap
kodu girmez (D15).
*Bedel:* her galeri okuması şube başına küçük bir hesap yapar. Sayfa açık kalırsa değişim anında
istek atılır (D9). `architecture.md`'nin bir cümlesi değişir.

**D7 — Hesap kuralları.** (R7, tuzak 2, 3, 4, `0025` X3)
- Bir **günün etkin aralıkları**: gün bir özel günün içindeyse onun aralıklarıdır, değilse haftalık
  programın o günüdür (D3).
- Aralık `[başlangıç, bitiş)` okunur. **23:59 bitiş gün sonu (24:00) sayılır**.
- **Açık:** şubenin yerel anı, o günün bir etkin aralığının içindedir.
- **Kapanış saati:** içinde bulunulan aralığın bitişidir. Bitiş gün sonuysa ve ertesi günün ilk
  etkin aralığı 00:00'da başlıyorsa açık süre ertesi güne devam eder. Bu kural zincirlenir.
- *(CLARIFY B2:)* Kapanış gün sonundaysa (bitiş 23:59 ve zincir orada bitiyorsa) `next` =
  `{ertesi gün, "00:00"}`. Metin D10'un kuralıyla basılır: "Kapanış yarın 00:00", "Kapanış Pazar
  00:00". Yeni anahtar yoktur (AC-93, AC-94).
- Zincir **7 günü** aşarsa kapanış saati yoktur. Sürekli açık şube yalnız "Şu an açık" der.
- **Kapalı:** sonraki açılış, şimdiden sonraki ilk etkin aralığın başlangıcıdır. *(CLARIFY B4(d):)*
  Arama sınırı **max(bugün, en son özel günün `through`'u) + 7 gün**dür (o gün dahil). Haftalık
  programda en az bir aralık olduğundan (`0025` D4) açılış her zaman bu sınırın içinde bulunur (AC-99).
- *(CLARIFY B4(c):)* Sınır anları: aralığın başlangıç anı **açıktır**, bitiş anı **kapalıdır**.
  `refreshInSeconds` kesirli saniyede yukarı yuvarlanır ve en az 1'dir (AC-98).
- Yerel saat → an çevirisi bölge kurallarıyla yapılır. Sabit fark ve "+24 saat" yoktur.
- *(PM şartı (c):)* Yaz saati geçişinde iki kural geçerlidir:
  - **Var olmayan yerel saat** (ileri geçiş): ondan sonraki ilk geçerli ana kayar. `next.time` o anın
    gerçek yerel saatini verir. Ör. `Europe/Berlin` 2027-03-28'de 02:30 açılış "03:00" olarak döner.
  - **İki kez yaşanan yerel saat** (geri geçiş): başlangıç da bitiş de **ilk** (erken, yaz saati) anı
    alır.
  - *(CLARIFY B3:)* Üç durum da deterministik Host testiyle ölçülür:
    - 23 saatlik günde `refreshInSeconds` (AC-84, AC-95);
    - var olmayan saat, `next.time` = gerçek an "03:00" (AC-87; Proje Yöneticisi `c51e167`'nin
      kuralını korudu);
    - iki kez yaşanan saat, ilk geçiş esas (AC-89, AC-90).
  Var olmayan gece yarısı (ör. `Asia/Beirut`, geçiş 00:00'da) günün ilk geçerli anıdır. Hem "bugün"ün
  değişimi hem 23:59 zinciri o ana bağlanır.
- Haftalık programı girilmemiş şubede (`openingHours: null`) "şu an" **hesaplanmaz**. Özel günler
  vitrinde ve yapısal veride gösterilmez (`0025` D4: saat bilgisi yok).

*Gerekçe:* `0025`'in 23:59 ve "iki güne bölme" kararlarının doğal okunuşu. Aksi her gece bir
dakikalık sahte kapanış ve yanlış kapanış saati üretir.
*Bedel:* 23:59'da gerçekten kapanan şube yok sayılır. Bir dakikalık fark kabul edilir.
*(CLARIFY B2, kabul edilen yan etki:)* zincir 7 günü aşarsa kapanış saati gösterilmez. Ör. 8 gün
kesintisiz açık bir şube (haftalık 00:00–23:59 + özel gün) "Şu an açık" der, kapanışını söylemez.

**D8 — Ziyaretçi okuması: şube öğesine `openNow` ve `specialHours`.** (R7, R10, R11, C3, X10)
- `openNow`, haftalık programı yoksa `null`'dır. Varsa şu nesnedir:
  `{ "open": bool, "today": "YYYY-MM-DD", "next": { "date": "YYYY-MM-DD", "time": "HH:mm" } | null, "refreshInSeconds": int }`.
  - `today` şubenin yerel tarihidir.
  - `next` açıkken kapanışı, kapalıyken açılışı verir. Tarih ve saat şubenin yereline göredir.
    Sürekli açık şubede `null`'dır.
  - `refreshInSeconds` sıradaki değişime ya da şubenin bir sonraki yerel gece yarısına (erken olanına)
    kalan **gerçek** saniyedir, yukarı yuvarlanır ve en az 1'dir.
  - *(PM şartı (b):)* Değer her zaman **1–90000** aralığındadır. Yaz saati gününde gün 23 ya da 25
    saattir, yani gece yarısına kalan süre 82800 ya da 90000 olabilir.
- `specialHours` dizidir ve hiç `null` olmaz. Haftalık programı girilmemiş şubede boştur.
  - Öğeleri `{ "from", "through", "intervals": [{ "start", "end" }] }` biçimindedir ve `from`'a
    göre sıralıdır.
  - Yalnız **görünür pencere** içindeki kayıtları taşır: `through` ≥ bugün ve `from` ≤ bugün + 30 gün.
- Yanıt `Cache-Control: no-store` taşır.
- `openNow` ve `specialHours` dilden bağımsızdır.
- Belge (`<head>`) "şu an" bilgisini taşımaz.

*Gerekçe:* yenileme süresinin göreli olması cihaz saatinden bağımsızlığı sağlar (R8). Pencere,
altlığı ve yapısal veriyi kısa tutar. Bayramlar genellikle birkaç hafta önce ilan edilir.
*Bedel:* 30 günden ileriye girilmiş kayıt, penceresine girene kadar vitrinde görünmez.

**D9 — Sayfa açıkken güncelleme.** (R9, tuzak 5, 7)
- Altlık, yanıttaki en küçük `refreshInSeconds` dolunca galeri okumasını **bir kez** tekrarlar.
  Süre, yanıtın alındığı andan itibaren ölçülür. *(CLARIFY B5:)* Süre monoton saatle
  (`performance.now()`) ölçülür, cihazın duvar saatiyle (`Date`) ölçülmez. Cihaz saatini ileri ya da
  geri almak tekrar okuma anını değiştirmez (AC-100).
- Sekme görünür olduğunda süre dolmuşsa okuma hemen tekrarlanır.
- Bunun dışında periyodik okuma yoktur.
- Tekrarlanan okuma sürerken eski durum ekranda kalır ve iskelete dönülmez.
- *(CLARIFY B8:)* Otomatik tekrar okuma **yalnız "şu an" satırını ve "Bugün" vurgusunu** günceller.
  Haftalık liste ve özel gün listesi sayfa yenilenene kadar ilk okumadaki hâliyle **sabit kalır**,
  yeni yanıt farklı kayıt taşısa da. Böylece yenileme blok yüksekliğini değiştirmez. Bloğun altında
  akışta öğe var: tek sütunlu altlıkta "Kurumsal" ve "Bizi takip edin" blokları onun ardından gelir
  (AC-105).
- *(PM şartı (b):)* **Tekrarlanan okuma başarısız olursa:**
  - Eski durum **gösterilmez**, çünkü geçerlilik süresi dolmuştur ve "açık" yazmaya devam etmek
    yanlış bilgi verir. "Şu an" satırının metni kaldırılır ama kutusu aynı yükseklikte kalır.
  - Haftalık liste, özel günler ve "Bugün" vurgusu kalır. Hata metni basılmaz (`0014` AC-54'ün deseni).
  - Okuma, sekme görünürken **60 saniyede bir** yeniden denenir. Başarılı olunca satır yeni durumla
    geri gelir.
  - Yani bayat durum, sürenin dolmasından ilk tekrar okumanın sonucuna kadar ekranda kalır. Bu en
    fazla bir istek süresidir.
- *(PM şartı (b):)* **Geçersiz süre:**
  - `refreshInSeconds` pozitif bir tam sayı değilse (0, negatif, `null`, sayı olmayan) okuma hemen
    değil, **60 saniye** sonra tekrarlanır. Böylece döngü oluşmaz.
  - 90000'den büyük değer 90000 sayılır.
  - Ardışık iki otomatik okuma arasında en az 1 saniye vardır.
- *(PM şartı (b):)* **Yaz saati günü:** süre gerçek saniyedir (D8). İstemci yerel saatle hesap yapmaz.

*Gerekçe:* gösterge en çok değişim başına bir istek atar. Gece yarısı vurguyu ve "yarın"ı tazeler.
Süresi dolmuş durumu basmamak, yanlış "açık" yazmaktan iyidir.
*Bedel:* açık sekme başına günde birkaç istek. Sunucu düşükse dakikada bir istek. Bu sürede satır boş kalır.

**D10 — Metinler.** (R7, R12, R13, tuzak 1, **SS4**)
- **Açıkken:** "Şu an açık · Kapanış {gün} {saat}".
- **Kapalıyken:** "Şu an kapalı · Açılış {gün} {saat}".
- `next` yoksa satır yalnız "Şu an açık"tır.
- **{gün}** kuralı (`next.date`'in `today`'den farkına göre):
  - 0 gün: boş, ör. "Kapanış 18:00";
  - 1 gün: "yarın";
  - 2–6 gün: haftanın günü ("Pazartesi", `0025` D6'nın ad tablosu);
  - 7 gün ve fazlası: gün ve ay ("21 Ocak").
- EN: "Open now · Closes 18:00", "Closed now · Opens tomorrow 10:00".
  AR: "مفتوح الآن · يغلق 18:00", "مغلق الآن · يفتح غدًا 10:00".
- Saat `HH:mm` biçiminde ve Latin rakamla basılır, soldan sağa okunur (`0025` D7). Tarih Miladi
  takvimle ve Latin rakamla basılır (tuzak 8).
- Durum **metinle** verilir. Renk yalnız ek işarettir ve kontrastı `frontend-002` belirteçleriyle sağlanır.
- Satır `aria-live` değildir. Yenilemede ekran okuyucuya duyurulmaz.

*Gerekçe:* ek almayan biçim Türkçede her saat için doğru. Ekli biçim saate göre beş farklı ek ister
(tuzak 1). Üç dilde aynı kalıp.
*Bedel:* sahibin örneğindeki "19:00'da kapanır"dan daha az doğal (**SS4**).

**D11 — Vitrin yerleşimi.** (R7, R10, R12)
- Şube öğesinin sırası: ad · "şu an" satırı · adres · telefon · haftalık yedi satır · "Özel günler".
- *(CLARIFY B8, ölçüldü:)* "Şu an" satırı **sabit iki satırlık** yer taşır. Kısa metin tek satırda
  durur, alt satır boş kalır. Metin kesilmez ve "…" kullanılmaz.
  - Ölçüm: `main` @ `9912ade` derlemesinin fontlarıyla, 16 px, kalın durum sözcüğüyle. 320 px'te
    kullanılabilir genişlik ≈288 px (16 px kenar boşluğu).
  - "Şu an kapalı · Açılış Çarşamba 09:00" 272 px, "… Açılış 21 Ağustos 09:00" 280 px.
  - "Closed now · Opens Wednesday 09:00" 291 px, "… Opens 21 September 09:00" 307 px.
  - `ar` "مغلق الآن · يفتح 21 سبتمبر 09:00" 231 px.
  - Yani İngilizce tek satıra sığmıyor. "…" de cümlenin sonundaki saati, yani asıl bilgiyi keserdi.
    En uzun metin 307 px, yani iki satırı aşmaz (AC-69).
- *(CLARIFY B8:)* D9 yenilemesi listeleri değiştirmez (D9). Satırın metni değişir, kutusu değişmez.
- **Bugün vurgusu:**
  - Bugün bir özel günün içinde değilse haftalık listede bugünün satırı (`today`'in haftanın günü)
    görünür bir "Bugün" etiketi ve `aria-current="date"` taşır.
  - Bugün bir özel günün içindeyse etiket ve `aria-current` o özel günün satırındadır. Haftalık
    listede hiçbir satır vurgulanmaz.
  - *(PM şartı (a), test etkisi:)* "Bugün" etiketi gün adından **ayrı bir öğedir**. Gün adının kendi
    öğesi vardır. Gün adını okuyan testler takvime bağlı kalmaz (Context tablosu).
- **"Özel günler"** başlıklı liste D8'in penceresindeki kayıtları basar. Her satır tarihi (tek gün
  "1 Ocak", aralık "10–12 Mart") ve aralıkları ya da "Kapalı"yı gösterir. Pencerede kayıt yoksa liste
  ve başlığı basılmaz.
- Haftalık programı girilmemiş şubede "şu an" satırı ve özel gün listesi yoktur (D7).

*Gerekçe:* durum, adın hemen altında ilk okunan bilgidir. Haftalık liste haftanın gününe, özel gün
tarihe göre dizilir. İki anlamı tek listede karıştırmamak için ayrı liste. Vurgu tek yerde olur.
*Bedel:* altlık özel gün sayısı kadar uzar (en çok 12 satır, pratikte 1–3). Kısa durumda bir satırlık
boşluk kalır. Sayfa açıkken biten bir özel gün, sayfa yenilenene kadar listede görünür (D9).

**D12 — Yapısal veri: `department` öğesinde `specialOpeningHoursSpecification`.** (R11, C4, X10)
- Haftalık programı girilmiş şubenin öğesi, penceresinde (D8) kayıt varsa bir dizi taşır.
- Her kaydın her aralığı bir öğedir: `@type: OpeningHoursSpecification`, `validFrom`, `validThrough`,
  `opens`, `closes`.
- Tam gün kapalı kayıt `opens` = `closes` = `00:00` olan tek öğedir (`0025` D9'un kodlaması).
- Sıra `from`, sonra aralık sırasıdır. Gruplama yapılmaz.
- Pencere dışındaki, geçmiş ya da haftalık programı olmayan şubenin kaydı basılmaz.
- Kök `AutoDealer` bu anahtarı hiçbir kurulumda taşımaz.
- **Bütçe:** tek şubeli kurulumda en kötü durum (12 kayıt × 3 aralık, hepsi pencerede) `/ar/` ana
  sayfa belgesini, aynı şubenin özel günsüz hâline göre **en çok 4608 B** büyütür (sıkıştırmasız).
  Ölçülen tahmin ≈4502 B. Şube sayısıyla doğrusal büyür.

*Gerekçe:* `0004` D1 (görünen bilgi). Pencere altlıkla aynı olduğu için iddia ile görünen bir.
Gruplama yapılmaz, çünkü tarih aralıkları nadiren tekrarlar.
*Bedel:* iki şubeli en kötü durumda ana sayfa belgesi ≈9 kB büyür. `0025`'in 2304 B'ıyla birlikte
şube başına ≈7 kB.

**D13 — Panel: şube formunda "Özel günler ve tatiller" bölümü. Kaydetme şubeyle birlikte.** (R4, R5, R6)
- Bölüm "Çalışma saatleri"nin ardından gelir ve açma ile düzenleme formunda bulunur.
- Her kayıtta "Başlangıç" ve "Bitiş" tarih alanı vardır (`type="date"`, değer `YYYY-MM-DD`).
  "Bitiş" boş bırakılırsa başlangıçla aynı sayılır.
- Her kayıtta bir "Tam gün kapalı" seçimi ya da 1–3 aralık bulunur. Aralık düzenlemesi `0025`
  D5'in eylemlerini kullanır ("Aralık ekle", "Kaldır").
- "Özel gün ekle" 12. kayıttan sonra görünmez. "Kaydı kaldır" kaydı formdan siler.
- Kayıtlar `from`'a göre sıralı görünür.
- Yardım metni: "Bayram ve tatil tarihleri her yıl elle girilir. Geçmiş günler kaydedildiğinde silinir."
- **Gövde:**
  - `specialHours` alanı yoksa ya da açık `null`'sa kayıtlar değişmez (`0025` D11 ile aynı kural).
  - Dizi gelirse bütün kayıtların yerine geçer. Boş dizi bütün kayıtları siler.
- Panel okuması (`GET /api/admin/branches/{id}`) güncel ve gelecek bütün kayıtları taşır (pencere
  sınırı yoktur). Şube listesi değişmez.
- Sürüm, `409`, kapatılmış şubede `branch.closed_read_only` ve yalnız Yönetici kuralları `0025`
  D11 ile aynıdır.
- Değişiklik entegrasyon olayı üretmez (C7).

*Gerekçe:* tek "Kaydet", tek sürüm. `0025`'in kurduğu deseni tekrarlar ve yeni uç açmaz.
*Bedel:* tek bir özel gün eklemek bütün şube formunu kaydeder.

**D14 — Doğrulama ve hata kodları (tek kapı).** (R4, C8)
- Tarih **tek bir ayrıştırma çağrısıyla** doğrulanır: `yyyy-MM-dd`, değişmez kültür, boşluk ve
  satır sonu kabul edilmez. NUL, `\n`, `2027-02-30`, `2027-13-01`, `27.11.2026`, `2027-1-1` aynı
  kodla `422` alır. **Hiçbir girdi `500` dönmez.**
- Alan anahtarları `specialHours.<i>.from|through` ve `specialHours.<i>.intervals.<j>.start|end`.
  `<i>` gönderilen sıradır ve 0'dan başlar. Doğrulama sıralamadan önce yapılır.
- *(CLARIFY B7:)* **Şekil ve `null` girdileri:**
  - `null` öğe (`specialHours: [null]`) → `specialHours.<i>` alanında `special_hours.invalid_entry`.
  - `intervals` yok ya da `null` → `specialHours.<i>.intervals` alanında `special_hours.intervals_required`.
  - `null` aralık (`intervals: [null]`) → `specialHours.<i>.intervals.<j>` alanında
    `special_hours.invalid_entry`.
  - Aralıkta `start` ya da `end` yok veya `null` → o alanda `opening_hours.invalid_time`.
  - `from` yok ya da `null` → `from` alanında `special_hours.invalid_date`.
  - `through` yok ya da `null` → hata değil, tek gün (D2). `""` → `special_hours.invalid_date`.
- *(CLARIFY B7:)* **Doğrulama, geçmiş kayıtların düşürülmesinden ÖNCE yapılır.**
  - Kayıt başına kurallar (şekil, tarih biçimi, `end_before_start`, `too_long`, aralık kuralları)
    gönderilen **her** kayda uygulanır. Geçmiş ama hatalı kayıt da `422` alır.
  - Küme kuralları (`overlap`, `too_many`) ve `too_far` düşürmeden sonra kalan kayıtlara uygulanır.
  - Hata anahtarındaki `<i>` her zaman gönderilen dizinin indeksidir. Düşürme indeksi kaydırmaz (AC-104).
- `special_hours.invalid_date`: tarih biçimi ya da var olmayan gün. Alanın kendisinde.
- `special_hours.end_before_start`: `through` < `from`. `through` alanında.
- `special_hours.too_long`: 60 günden uzun kayıt. `through` alanında.
- `special_hours.too_far`: `through` bugünden 366 günden fazla ileride. `through` alanında.
- `special_hours.overlap`: başka bir kayıtla ortak tarih. Sonraki kaydın (`<i>` büyük olan) `from`
  alanında.
- `special_hours.too_many`: güncel ya da gelecek 12'den fazla kayıt. `specialHours` alanında.
- `special_hours.invalid_entry`, `special_hours.intervals_required`: yukarıdaki şekil kuralları *(CLARIFY B7)*.
- Aralık hataları `0025` D12'nin kodlarıyla (`opening_hours.invalid_time`, `end_not_after_start`,
  `too_many_intervals`, `overlap`) aralığın ya da kaydın alanında döner.
- Özel gün hatası varsa şubenin hiçbir alanı değişmez. Bütün hatalar tek yanıtta toplanır (`0025` P2).

*Gerekçe:* `0024` (NUL → 500) ve `0025` (`$` → 500) dersleri. Aralık kurallarının kodları
haftalık programla aynı kalır.
*Bedel:* yok.

**D15 — İlk paket: saat bloğu tembel parçaya taşınır.** (C5, Ölçümler, tuzak 10)
- Şubenin "şu an" satırı, haftalık listesi (`0025`'ten bugün ilk pakette olan `OpeningHoursList`
  dahil) ve özel gün listesi ilk paketin dışında, tembel bir parçadadır.
- *(CLARIFY B9:)* Vitrin metinleri **vitrin kataloğundadır** (AC-73), yani ilk paketle gelir. Ölçülen
  payları ≈284 B'tır (üç dil). Bütçenin geri kalanı tembel parça bağına (≈80 B) ve parçayı yükleyen
  koda kalır.
- İlk paket `main` tabanına göre **en çok 512 B** büyür (`0024` AC-55 tanımı).
- *(PM şartı (a):)* İki bekleme evresi vardır:
  - **A — galeri okunmadı:** şube sayısı bilinmez. `0014`'ün iskeleti aynen kalır (`0014` AC-49,
    `evidence-0014.spec.ts:63`). Altlık ilk ekranın altında olduğundan bu geçiş görünür alanda
    olmaz (AC-78).
  - **B — galeri hazır, parça yüklenmedi:** her şubenin adı, adresi ve telefonu basılır. Saat alanının
    yerinde bir iskelet durur. İskeletin yüksekliği okunan veriden (satır sayısı, aralık sayısı, özel
    gün sayısı) **açılacak alanın yüksekliğine eşit** kurulur. Bunun için saat alanının satırları
    sabit yükseklikte olur ve satır kırılması veriden belirlenir (ör. her aralık ayrı satırda).
- Eşitlik ölçülür: en kötü durumda (12 özel gün, her gün ve her kayıt çift aralıklı, `ar`, 320 px)
  ve fontlar bekletilerek ±1 px (AC-75, AC-76). Parça yüklenince saat alanının altındaki hiçbir öğe
  kaymaz (AC-77).
- Perf koşusu her aktif şubeye penceresinde 12 özel gün tohumlar (D12'nin en kötü durumu, `0025`
  P9'un tohum yolundan). Koşu `0024` ve `0025`'in tohumlarını korur.
- C3 eşikleri geçerlidir. Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşu yapılır ve LCP öğesi
  PR'a yazılır.
- CLS fontlar bekletilerek ölçülür (`frontend-003` dersi). Satırın yeri ayrılır: açık/kapalı geçişi ve
  yenileme, satırın kutusunun yüksekliğini 320 px'te üç dilde değiştirmez.

*Gerekçe:* ölçülen ≈1,2 KB bütçenin iki katı. Altlık ilk ekranın altında, LCP'ye girmiyor. `0025`
P7 bu yolu zaten öngörmüştü. Haftalık listeyi de taşımak ilk paketi küçültür ve bloğu tek parça yapar.
Sabit "en kötü durum" yüksekliği yerine veriden kurulan yükseklik seçildi. Sebebi: tipik şube (0–2
özel gün) 12 satırlık boş bir alan taşımasın. Şube sayısı A evresinde bilinmediği için orada eşit
yükseklik zaten kurulamaz.
*Bedel:* altlıktaki saat bloğu bir parça isteği kadar geç gelir. Bir kez daha tembel parça bağı (+≈80 B).

**D16 — Sözlük: "Özel Gün" ve "Şu An Durumu".** (DoD T7)
- **Özel Gün:** şubenin bir tarih aralığı için haftalık programı ezen kaydı. Ya tam gün kapalıdır ya
  da her gün aynı 1–3 yerel saat aralığını taşır (B3).
- **Şu An Durumu:** şubenin, şubenin saat diliminde ve özel günler dahil hesaplanan açık/kapalı
  durumu ve bir sonraki değişimi (B4).
- `Çalışma Programı` satırının "İstisnaları kapsamaz" cümlesi "Özel Gün"e bağlanır.

*Gerekçe:* sözlükte B3 kuralı var, terimi yok.
*Bedel:* yok.

**D17 — Belgeler.**
- `docs/domain.md`: D16'nın iki terimi.
- `docs/architecture.md:42`: "'Şu an açık mı?' sunucuda, `Dealership`'te, şubenin saat diliminde ve
  saat soyutlamasıyla hesaplanır; uç yalnız basar (`0027` D6)."
- Karar Defteri'ne bir satır (D6).
- `docs/deploy.md`: perf tohumunun cümlesi (D15).
- *(CLARIFY B12:)* `docs/deploy.md`'ye CDN kuralı: "`/api/dealership/gallery` kenarda önbelleğe
  alınmaz. `Cache-Control: no-store` korunur." Yanıt zamana bağlıdır (D8). Kenar önbelleği bayat bir
  "açık" basar.

*Gerekçe:* tek gerçek kaynağı. Kod belgeyle çelişmesin.
*Bedel:* yok.

**D18 — Saklama `dealership` şemasındadır.** (C6) Biçim (ayrı tablo ya da yapılandırılmış kolon)
uygulamanın plan kararıdır. Kriterler davranışı ölçer. Kayıtlar şubenin sürümüyle aynı satırda
korunmalıdır (R5).
*Gerekçe:* C6, `0025` D13.
*Bedel:* yok.

---

## Sahibe sorular

> **Cevaplandı (sahip kararı, 2026-11-27):** dördü de önerilen yönde.
> - **SS1** → resmî tatiller otomatik değil, hepsi elle girilir.
> - **SS2** → şube başına. "Tümüne uygula" sonra ayrı iş.
> - **SS3** → gösterge bu kartta yalnız ana sayfa altlığında. İlan detayı ayrı kart.
> - **SS4** → "Şu an açık · Kapanış 19:00" / "Şu an kapalı · Açılış yarın 09:00".
> Kriter değişmedi.

- **SS1. Sabit resmî tatiller (1 Ocak, 23 Nisan, 1 Mayıs, 19 Mayıs, 15 Temmuz, 30 Ağustos, 29 Ekim)
  otomatik "kapalı" gelsin mi?**
  **Öneri: hayır, hepsi elle girilsin (D5).** Kurulum Körfez'de de olabilir ve tatil listesi ülkeye
  göre değişiyor. Galerilerin bir kısmı resmî tatilde açık, yani otomatik kapanış yanlış "kapalı"
  yazar. Ramazan ve Kurban Bayramı'nın tarihi her yıl kayıyor ve resmî ilanla kesinleşiyor. Projede
  takvim kaynağı yok, bu yüzden bayramlar her durumda elle girilir.
  *Bedeli:* yönetici yılda yaklaşık 10 kayıt girer. Girmezse o gün vitrin haftalık saati gösterir.
- **SS2. Bir tatil, şube başına mı girilsin, yoksa tek kayıtla bütün şubelere mi?**
  **Öneri: şube başına (D2).** Şubelerin tatil günleri farklı olabilir (ör. biri bayramın ikinci günü
  açık). Bugün iki şube var. "Bütün şubelere uygula" sonradan ayrı bir iş olabilir (X2).
- **SS3. "Şu an açık" göstergesi nerede görünsün?**
  **Öneri: bu kartta yalnız ana sayfa altlığında, her şubenin adının altında (D1).** İlan detayı
  sayfasında aracın şubesi için göstermek değerli. Ancak araç sayfası şubeyi bugün yalnız adıyla
  biliyor, yani bu iş ayrı bir kart olur (X3).
- **SS4. Gösterge metni hangi biçimde olsun?**
  **Öneri: "Şu an açık · Kapanış 19:00" ve "Şu an kapalı · Açılış yarın 09:00" (D10).** Örnekteki
  "19:00'da kapanır" Türkçede saate göre ek değiştiriyor ("19:00'da", "20:00'de", "13:00'te"). Ek
  almayan biçim her saatte doğru ve üç dilde aynı kalıp. Ekli biçim istenirse saat başına ek tablosu
  gerekir (bedel: kriter ve test sayısı artar).

---

## Acceptance Criteria

> **Varsayılanlar** (aksi yazılmadıkça):
> - Panel kriterleri Yönetici oturumuyla koşar. Kurulum tr/ar/en'dir.
> - **"Programlı şube"** `0025`'in programını taşır: Pazartesi–Cuma 09:00–18:00, Cumartesi
>   10:00–14:00, Pazar kapalı. Saat dilimi `Europe/Istanbul`'dur (UTC+3, yaz saati yok).
> - **"Saat T"**, Host'un `IClock`'unun sabit `T` döndürdüğü anlamına gelir. Anlar UTC, kriter
>   metinlerindeki yerel saatler şubenindir. Tarihler örnektir. Bayram tarihleri gerçek ilanla aynı
>   olmak zorunda değildir.
> - Belge kriterleri sunucu yanıtıyla, JavaScript çalıştırılmadan ölçülür (`0004` C13).

### Panel: giriş (R1, R5 · D2, D13)

- **AC-1.** Şube düzenleme ekranında "Çalışma saatleri"nin ardından bir "Özel günler ve tatiller"
  bölümü vardır. Özel günü olmayan şubede bölüm boştur ve D13'ün yardım metnini gösterir.
- **AC-2.** Yönetici 2027-01-01 için "Tam gün kapalı" bir kayıt girip kaydettikten sonra ekran yeniden
  açıldığında kayıt `2027-01-01`–`2027-01-01`, "Tam gün kapalı" olarak görünür.
- **AC-3.** "Bitiş" boş bırakılıp kaydedilen kayıt, başlangıçla aynı bitişle saklanır.
- **AC-4.** 2027-02-08–2027-03-09 için 10:00–16:00 girilen kayıt saklanır. Panel okuması aralığı bu
  biçimde döner.
- **AC-5.** Bir kayda 09:00–13:00 ve 16:00–21:00 girilir ve kaydedilir. Panel okuması iki aralığı
  bu sırayla döner.
- **AC-6.** Şubede 12 güncel/gelecek kayıt varken "Özel gün ekle" eylemi görünmez.
- **AC-7.** "Kaydı kaldır" ile silinen kayıt kaydetmeden sonra panel okumasında yoktur. Kaydetmeden
  ekrandan çıkmak kayıtları değiştirmez.
- **AC-8.** Yeni şube açma formunda aynı bölüm vardır. Özel günle açılan şubenin kaydı saklanır.
- **AC-9.** Kayıtlar panelde girilme sırasından bağımsız olarak `from` sırasıyla görünür.

### Doğrulama (R4 · D2, D3, D4, D14)

- **AC-10.** Aşağıdaki her kaydetme `422` döner, belirtilen alan anahtarını ve kodu taşır. Hiçbiri
  `500` dönmez:
  - `from` `2027-02-30`, `2027-13-01`, `27.11.2026`, `2027-1-1`, `""`, `"2027-01-01\n"` ya da NUL içeren
    bir metin → `specialHours.0.from`, `special_hours.invalid_date`.
  - *(CLARIFY B7:)* `from` yok ya da `null` → `specialHours.0.from`, `special_hours.invalid_date`.
  - `through` `""` → `specialHours.0.through`, `special_hours.invalid_date`.
  - `specialHours: [null]` → `specialHours.0`, `special_hours.invalid_entry`.
  - `intervals` yok ya da `null` → `specialHours.0.intervals`, `special_hours.intervals_required`.
  - `intervals: [null]` → `specialHours.0.intervals.0`, `special_hours.invalid_entry`.
  - Aralıkta `start` ya da `end` `null` veya yok → `specialHours.0.intervals.0.start|end`,
    `opening_hours.invalid_time`.
- **AC-11.** `through` < `from` olan kayıt `422` döner ve `specialHours.0.through` alanında
  `special_hours.end_before_start` taşır.
- **AC-12.** 61 gün süren kayıt `422` ve `special_hours.too_long` döner. 60 gün süren kayıt kabul edilir.
- **AC-13.** Saat 2026-11-27T09:00Z'de `through` = 2027-11-29 olan kayıt `422` ve
  `special_hours.too_far` döner. `through` = 2027-11-28 olan kayıt kabul edilir.
- **AC-14.** 2027-03-08–2027-03-10 ve 2027-03-10–2027-03-12 kayıtları birlikte `422` döner.
  `specialHours.1.from` alanında `special_hours.overlap` taşır.
- **AC-15.** Uç uca değen 2027-03-08–2027-03-09 ve 2027-03-10–2027-03-12 kabul edilir.
- **AC-16.** 13 güncel/gelecek kayıtla kaydetme `422` döner ve `specialHours` alanında
  `special_hours.too_many` taşır.
- **AC-17.** Aralığı `9:00` olan kayıt `422` döner. `specialHours.0.intervals.0.start` alanında
  `opening_hours.invalid_time` taşır.
- **AC-18.** Bir kayıtta 09:00–13:00 ve 13:00–18:00 `422` döner. `specialHours.0` alanında
  `opening_hours.overlap` taşır.
- **AC-19.** Bir kayıtta dört aralık `422` ve `opening_hours.too_many_intervals` döner.
- **AC-20.** Hatalı özel günle yapılan kaydetmede şubenin hiçbir alanı değişmez. Aynı gövdedeki
  geçerli adres değişikliği ve haftalık program değişikliği de yazılmaz.
- **AC-21.** İki kaydında farklı hata olan kaydetmenin yanıtı iki hatayı da taşır.
- **AC-22.** Panelde hata ilgili kaydın ya da aralığın yanında, ekranın dilinde görünür. Girilen
  değerler formda kalır.

### Geçmiş, gidiş-dönüş, sürüm ve yetki (R5, R6 · D4, D13)

- **AC-23.** Saat 2027-01-02T09:00Z'de, `through` = 2027-01-01 olan bir kayıt ve geçerli bir kayıt
  taşıyan kaydetme `200` döner. Panel okuması yalnız geçerli kaydı taşır.
- **AC-24.** 2027-01-01'lik kaydı olan şubenin panel okuması saat 2027-01-01T20:59Z'de (yerel 23:59)
  kaydı taşır. Saat 2027-01-01T21:00Z'de (yerel 2027-01-02 00:00) taşımaz.
- **AC-25.** Gövdesinde `specialHours` alanı olmayan kaydetme (`0025`'in gövdesi) kayıtları
  değiştirmez. `"specialHours": null` taşıyan kaydetme de değiştirmez.
- **AC-26.** `"specialHours": []` taşıyan kaydetme bütün kayıtları siler.
- **AC-27.** `GET /api/admin/branches/{id}` yanıtını değiştirmeden `PUT` olarak geri göndermek
  kayıtları değiştirmez.
- **AC-28.** Eski sürümle gönderilen, özel günleri değiştiren kaydetme `409` döner. Özel günleri
  değiştiren başarılı kaydetme sürümü bir artırır. Kapatılmış şubede `409` ve `branch.closed_read_only`
  döner. Hiçbirinde outbox'a entegrasyon olayı yazılmaz.
- **AC-29.** Satış Danışmanı ve İçerik Editörü oturumuyla özel günleri değiştiren kaydetme `403`,
  oturumsuz istek `401` döner. Kayıtlar değişmez. Satış Danışmanının şube listesi yanıtının anahtar
  kümesi bu karttan öncekiyle aynıdır.

### "Şu an" hesabı (R2, R7, R8 · D3, D6, D7, D8)

- **AC-30.** Programlı şube, Saat 2026-12-04T09:00Z (Cuma, yerel 12:00):
  `openNow` = `{"open":true,"today":"2026-12-04","next":{"date":"2026-12-04","time":"18:00"},"refreshInSeconds":21600}`.
- **AC-31.** Programlı şube, Saat 2026-12-04T16:00Z (Cuma 19:00):
  `open` false, `next` `{"date":"2026-12-05","time":"10:00"}`. `refreshInSeconds` yerel gece
  yarısına kalan 18000'dir.
- **AC-32.** Programlı şube, Saat 2026-12-05T12:00Z (Cumartesi 15:00): `open` false, `next`
  `{"date":"2026-12-07","time":"09:00"}` (Pazar kapalı).
- **AC-33.** `Asia/Kuwait` saat dilimli, Pazar–Perşembe 09:00–13:00 ve 16:00–21:00, Cuma–Cumartesi
  kapalı bir şube, Saat 2026-12-06T11:00Z (Pazar yerel 14:00): `open` false, `next`
  `{"date":"2026-12-06","time":"16:00"}`.
- **AC-34.** Programlı şubede 2027-01-01 (Cuma) "Tam gün kapalı" iken, Saat 2027-01-01T09:00Z'de
  `open` false ve `next` `{"date":"2027-01-02","time":"10:00"}`'dır.
- **AC-35.** Programlı şubede 2027-02-08–2027-03-09 10:00–16:00 ve 2027-03-10–2027-03-12 tam gün
  kapalı iken, Saat 2027-03-09T14:00Z'de (Salı 17:00) `open` false'tur (haftalık 18:00'i ezer).
  `next` `{"date":"2027-03-13","time":"10:00"}`'dır.
- **AC-36.** Aynı şube Saat 2027-03-09T08:00Z'de (Salı 11:00) `open` true ve `next.time` `16:00`'dır.
- **AC-37.** Cuma 20:00–23:59, Cumartesi 00:00–02:00 ve 10:00–14:00 olan şube:
  - Saat 2026-12-04T20:30Z (Cuma 23:30): `open` true, `next` `{"date":"2026-12-05","time":"02:00"}`.
  - Saat 2026-12-04T20:59:30Z (Cuma 23:59:30): `open` true.
- **AC-38.** Her gün 00:00–23:59 olan şubede `open` true ve `next` `null`'dır.
- **AC-39.** `Europe/Berlin` saat dilimli, Pazartesi 09:00–18:00 bir şube, Saat 2027-03-29T07:30Z
  (yaz saati sonrası yerel 09:30): `open` true. Saat 2027-03-29T07:30Z'yi sabit `+01:00` farkıyla
  okuyan bir hesap aynı anda "kapalı" der. Kriter, yaz saati geçişinden sonraki ilk Pazartesinin
  bölge kuralıyla hesaplandığını ölçer.
- **AC-40.** Programsız şubenin `openNow`'u `null` ve `specialHours`'u `[]`'dir. Özel günleri olsa
  bile bu böyledir.
- **AC-41.** *(CLARIFY B1 ile değişti.)* Saat dilimi `Europe/Istanbul`'dan `Asia/Tokyo`'ya
  değiştirilen programlı şubenin kayıtları (tarih ve saat) değişmez. Saat 2026-12-04T09:00Z'de
  (Tokyo'da Cuma 18:00) `today` `2026-12-04`, `open` false ve `next` `{"date":"2026-12-05","time":"10:00"}`'dır.
  Aynı anda İstanbul'da şube açık olurdu.

### Ziyaretçiye dönük okuma (R10, C3 · D8)

- **AC-42.** `/api/dealership/gallery` yanıtında her şube öğesinin anahtar kümesi tam olarak
  `address`, `name`, `openNow`, `openingHours`, `phone`, `specialHours`'tur. Kök anahtar kümesi
  `0024` AC-33'teki gibidir.
- **AC-43.** `openNow`'un anahtar kümesi tam olarak `next`, `open`, `refreshInSeconds`, `today`'dir.
  Yanıtta hiçbir yerde şubenin saat dilimi yoktur.
- **AC-44.** Saat 2026-12-01T09:00Z'de `specialHours` 2026-12-31'lik kaydı taşır, 2027-01-01'liği
  taşımaz (pencere bugün + 30 gün).
- **AC-45.** Saat 2027-03-01T09:00Z'de 2027-02-08–2027-03-09 kaydı `specialHours`'tadır (sürüyor).
- **AC-46.** `locale=tr`, `ar` ve `en` yanıtlarında aynı şubenin `openNow`'u ve `specialHours`'u aynıdır.
- **AC-47.** Yanıt `Cache-Control: no-store` başlığını taşır.

### Vitrin altlığı (R7, R9, R10, R12, R13 · D9, D10, D11)

- **AC-48.** AC-30'un yanıtıyla `/tr/` altlığında şubenin adının hemen altında "Şu an açık ·
  Kapanış 18:00" yazar. AC-31'inkiyle "Şu an kapalı · Açılış yarın 10:00" yazar.
- **AC-49.** `next` 2 gün sonraysa {gün} haftanın günüdür ("Şu an kapalı · Açılış Pazartesi
  09:00"). 7 gün ya da daha sonraysa gün ve aydır ("… Açılış 21 Ocak 09:00"). `/ar/`'da tarih Miladi
  takvimle ve Latin rakamla basılır.
- **AC-50.** `next` `null` iken satır yalnız "Şu an açık" yazar.
- **AC-51.** `/en/` satırları "Open now · Closes 18:00" ve "Closed now · Opens tomorrow 10:00"dır.
  `/ar/` satırları "مفتوح الآن · يغلق 18:00" ve "مغلق الآن · يفتح غدًا 10:00"dır. Arapçada satır sağdan
  sola dizilir ve saat soldan sağa okunur.
- **AC-52.** Durum, rengi kaldırılmış (renksiz) görünümde de metinden okunur: açık ve kapalı
  satırların metni farklıdır. Satırın `aria-live` özniteliği yoktur.
- **AC-53.** Bugün özel gün değilken haftalık listede yalnız `today`'in gününün satırı "Bugün"
  etiketini ve `aria-current="date"`'i taşır.
- **AC-54.** Bugün bir özel günün içindeyken "Bugün" etiketi ve `aria-current` o özel günün satırındadır.
  Haftalık listede hiçbir satır taşımaz.
- **AC-55.** Saat 2027-02-25T09:00Z'de, 2027-03-15 için 10:00–16:00, 2027-03-01 tam gün kapalı ve
  2027-03-10–2027-03-12 tam gün kapalı kayıtları (bu sırayla girilmiş) olan şubenin `/tr/` "Özel
  günler" listesi sırasıyla "1 Mart — Kapalı", "10–12 Mart — Kapalı" ve "15 Mart — 10:00–16:00"
  satırlarını gösterir. Aralık `0025` D7 biçimindedir.
- **AC-56.** Penceresinde kaydı olmayan şubede "Özel günler" başlığı ve listesi yoktur.
- **AC-57.** Programsız şubede "şu an" satırı ve özel gün listesi yoktur. Ad, adres ve telefon
  `0025` AC-46'daki gibidir.
- **AC-58.** Tarayıcının saat dilimi `Asia/Tokyo` ve saati gerçek zamandan 3 gün ileri iken altlığın
  "şu an" satırı, aynı anda Host yanıtının `openNow`'unun gösterdiğini yazar (e2e, `page.clock`).
- **AC-59.** `refreshInSeconds` = 2 olan bir yanıttan sonra altlık galeri okumasını en geç 3 saniye
  içinde tam olarak bir kez tekrarlar ve yeni yanıtın satırını basar (birim testi, sahte zamanlayıcı).
- **AC-60.** Süresi dolmuş bir yanıtla gizli kalan sekme görünür olduğunda okuma bir kez tekrarlanır.
  Süresi dolmamışsa tekrarlanmaz.
- **AC-61.** Tekrarlanan okuma sürerken eski satır görünür kalır ve iskelet basılmaz. *(PM şartı (b)
  ve CLARIFY B5 ile değişti:)* Okuma 5 saniyede sonuçlanmayacak şekilde bekletildiğinde bu 5 saniye
  boyunca satırın metni ilk yanıtınkidir. Başarısız tekrar okuma için AC-79 ve AC-80 geçerlidir.

### Yapısal veri (R11 · C4 · D12)

- **AC-62.** Saat 2026-12-20T09:00Z'de, programlı şubenin penceresinde 2026-12-31 tam gün kapalı ve 2027-01-02 için 10:00–13:00
  varken `department` öğesinin `specialOpeningHoursSpecification` dizisi sırasıyla şunlardır:
  - `{"@type":"OpeningHoursSpecification","validFrom":"2026-12-31","validThrough":"2026-12-31","opens":"00:00","closes":"00:00"}`;
  - `{"@type":"OpeningHoursSpecification","validFrom":"2027-01-02","validThrough":"2027-01-02","opens":"10:00","closes":"13:00"}`.
  Her öğenin anahtar kümesi tam olarak bu beştir.
- **AC-63.** Tek şubeli kurulumda 12 kaydın her birinde üç aralık bulunan ve hepsi pencerede olan
  şubenin `/ar/` ana sayfa belgesi, aynı şubenin özel günsüz hâlinden en çok **4608 B** büyüktür.
- **AC-64.** Pencere dışındaki, geçmiş ya da programsız şubenin kaydı belgede yoktur. Kök `AutoDealer`
  hiçbir kurulumda `specialOpeningHoursSpecification` taşımaz.
- **AC-65.** Belge "Şu an açık", "Şu an kapalı" metinlerini ve `openNow`'un hiçbir değerini taşımaz.
  Ana sayfa tam olarak bir JSON-LD bloğu taşır.
- **AC-66.** Yönetici özel günü kaydettikten sonra istenen ilk `/tr/` belgesi yeni kaydı taşır.

### Performans ve paket (C5 · D15)

- **AC-67.** İlk paket (`0024` AC-55 tanımı) `main` tabanına göre en çok **512 B** büyür. Ölçüm
  PR'ın **head** commit'inden yapılır.
- **AC-68.** *(PM şartı (a) ve CLARIFY B8 ile değişti.)* Galeri okuması tamamlanmış ama saat parçası
  yüklenmemişken her şubenin adı, adresi ve telefonu basılır. Saat alanının yerinde evre B iskeleti
  görünür (D15). Parça yüklenince iskelet → blok geçişinde bloğun **dışındaki** hiçbir öğe kaymaz:
  fontlar bekletilmiş e2e ölçümünde `layout-shift` girdilerinin `sources` düğümlerinden hiçbiri şube
  bloğunun dışında değildir.
- **AC-69.** *(CLARIFY B8 ile değişti.)* "Şu an" satırının kutusu, fontlar bekletilmiş ölçümde, 320 px
  genişlikte aşağıdaki dokuz metnin hepsinde **aynı yükseklikte** ve iki satır yüksekliğindedir.
  Metin kesilmez:
  - "Şu an açık · Kapanış 18:00", "Şu an kapalı · Açılış Çarşamba 09:00",
    "Şu an kapalı · Açılış 21 Ağustos 09:00";
  - "Open now · Closes 18:00", "Closed now · Opens Wednesday 09:00", "Closed now · Opens 21 September 09:00";
  - `ar`'da aynı üç biçim ("مفتوح الآن · يغلق 18:00", "مغلق الآن · يفتح الأربعاء 09:00",
    "مغلق الآن · يفتح 21 سبتمبر 09:00").
- **AC-70.** Perf koşusu her aktif şubeye penceresinde 12 kayıt (üçer aralık) tohumlar. `0024` ve
  `0025`'in tohumları korunur. C3 ana sayfa, liste ve detayda yeşildir. Bir sayfanın medyanı
  2450 ms'yi aşarsa ikinci koşunun sonucu ve LCP öğesi PR'dadır.
- **AC-71.** *(CLARIFY B8 ile değişti.)* Ana sayfa fontlar bekletilerek yüklenir ve altlığa kadar
  kaydırılır. `PerformanceObserver('layout-shift')` girdilerinin `sources` düğümlerinin hiçbiri şube
  bloğunun ya da altlıkta ondan sonra gelen öğelerin içinde değildir (e2e). Lighthouse öğe bazlı
  pay vermediği için ölçüm bu yoldan yapılır.

### Şema ve çeviri (C6 · DoD T5, T6)

- **AC-72.** Bu kartın migration'ı yalnız `dealership` şemasına dokunur.
- **AC-73.** Panel bölümünün metinleri ve D14'ün altı hata kodu panel kataloğunda, vitrin metinleri
  ("Şu an açık", "Şu an kapalı", kapanış ve açılış kalıpları, "yarın", "Bugün", "Özel günler") vitrin
  kataloğundadır. Hepsi tr, ar ve en'de vardır ve eksik anahtar kontrolü yeşildir.
- **AC-74.** `frontend/src` altında bu kartın eklediği her sabit `t('…')` anahtarı en az bir katalogda
  bulunur (`frontend-003` D19 kapısı).


### Proje Yöneticisi şartlarıyla eklenenler (2026-11-27)

**(a) Tembel parçanın iskeleti (D15)**

- **AC-75.** Saat parçası `page.route` ile bekletilir ve fontlar yüklenmiştir (`.woff2` bekletilip
  serbest bırakılarak, `frontend-003` dersi). En kötü durumdaki şubede (penceresinde 12 özel gün; yedi
  günün ve 12 kaydın her biri iki aralıklı) `/ar/` altlığı 320 px genişlikte ölçülür. Evre B
  iskeletinin yüksekliği, parça yüklendikten sonraki saat alanının yüksekliğine ±1 px eşittir.
- **AC-76.** Aynı ölçüm, programlı şubede (özel gün yok), `/tr/` ve 1280 px genişlikte de ±1 px eşittir.
- **AC-77.** AC-75'in kurgusunda parça serbest bırakıldığında saat alanının altındaki öğenin
  (altlığın bir sonraki bloğu) dikey konumu değişmez (±1 px). Şube bloğundan kaynaklanan
  `layout-shift` girdisi yoktur.
- **AC-78.** Perf tohumuyla `/tr/` ana sayfası 412×823 ve 1350×940 görünümde açıldığında altlığın
  üst kenarı ilk görünüm alanının altındadır. Evre A'dan içeriğe geçiş görünür alanda olmaz. Evre A
  iskeleti `0014` AC-49'daki gibidir.

**(b) Yeniden okumanın sınırları (D8, D9)**

- **AC-79.** Süre dolduktan sonraki tekrar okuma `503` dönerse:
  - "şu an" satırının metni kaldırılır ve kutusunun yüksekliği değişmez;
  - haftalık liste, özel günler ve "Bugün" vurgusu kalır;
  - altlıkta hata metni yoktur
  (birim testi, sahte zamanlayıcı).
- **AC-80.** AC-79'dan sonra sekme görünürken okuma 60 saniyede bir tekrarlanır. İkinci deneme `200`
  dönünce satır yeni yanıtın durumunu basar. Sekme gizliyken deneme yapılmaz.
- **AC-81.** `refreshInSeconds` değeri `0`, `-5`, `null` ya da `"abc"` olan yanıttan sonra altlık
  ilk 59 saniyede tekrar okuma yapmaz. 60. saniyede tam olarak bir okuma yapar.
- **AC-82.** `refreshInSeconds` = 200000 olan yanıttan sonra tekrar okuma 90000. saniyede yapılır.
- **AC-83.** AC-30–AC-41 ve AC-84–AC-91'in her yanıtında `refreshInSeconds` 1 ile 90000 arasındadır
  (Host).
- **AC-84.** `Europe/Berlin` saat dilimli, yalnız Pazartesi 09:00–18:00 açık bir şube. Saat
  2027-03-27T23:00Z'de (Pazar 2027-03-28, 00:00 CET; ileri geçiş günü):
  - `open` false, `today` `2027-03-28`;
  - `next` `{"date":"2027-03-29","time":"09:00"}`;
  - `refreshInSeconds` 82800 (23 saatlik gün).
- **AC-85.** Aynı şube, Saat 2027-10-30T22:00Z'de (Pazar 2027-10-31, 00:00 CEST; geri geçiş günü):
  `refreshInSeconds` 90000 (25 saatlik gün).

**(c) Yaz saati geçişinde "şu an" ve `next` (D7) — Host, sabit `IClock`**

> Türkiye ve Körfez bölgelerinde (`Asia/Kuwait`, `Asia/Dubai`, `Asia/Riyadh`) yaz saati yok. Örnekler
> `Europe/Berlin` (geçiş 01:00Z: ileri 2027-03-28, geri 2027-10-31) ve `Asia/Beirut` (ileri geçiş
> yerel 00:00'da, 2027-03-28) bölgeleridir. Programlar kurgudur. Beklenen anlar tzdata ile
> doğrulandı.

- **AC-86.** Berlin, Pazar 01:00–04:00 (ileri geçişi içine alan aralık). Saat 2027-03-28T00:30Z'de
  (01:30 CET):
  - `open` true;
  - `next` `{"date":"2027-03-28","time":"04:00"}`;
  - `refreshInSeconds` 5400 (gerçek 1,5 saat, duvar saatindeki 2,5 saat değil).
- **AC-87.** Berlin, Pazar 02:30–05:00 (başlangıç var olmayan saatte):
  - Saat 2027-03-28T00:45Z'de (01:45 CET): `open` false, `next` `{"date":"2027-03-28","time":"03:00"}`,
    `refreshInSeconds` 900.
  - Saat 2027-03-28T01:00Z'de (03:00 CEST): `open` true, `next.time` `05:00`, `refreshInSeconds` 7200.
- **AC-88.** Berlin, Pazar 01:00–04:00 (geri geçişi içine alan aralık). Saat 2027-10-31T00:30Z'de
  (02:30 CEST):
  - `open` true;
  - `next.time` `04:00`;
  - `refreshInSeconds` 9000 (gerçek 2,5 saat).
- **AC-89.** Berlin, Pazar 02:30–05:00 (başlangıç iki kez yaşanan saatte):
  - Saat 2027-10-31T00:15Z'de (ilk 02:15, CEST): `open` false, `next.time` `02:30`, `refreshInSeconds` 900.
  - Saat 2027-10-31T01:15Z'de (ikinci 02:15, CET): `open` true, `next.time` `05:00`,
    `refreshInSeconds` 9900.
- **AC-90.** Berlin, Pazar 01:00–02:30 (bitiş iki kez yaşanan saatte):
  - Saat 2027-10-31T00:15Z'de `open` true ve `refreshInSeconds` 900. Bitiş ilk 02:30'dur (00:30Z).
  - Saat 2027-10-31T01:15Z'de (ikinci 02:15) `open` false.
- **AC-91.** `Asia/Beirut`, Cumartesi 20:00–23:59 ve Pazar 00:00–02:00 (var olmayan gece yarısı):
  - Saat 2027-03-27T21:30Z'de (Cumartesi 23:30 EET): `open` true, `today` `2027-03-27`,
    `next` `{"date":"2027-03-28","time":"02:00"}`, `refreshInSeconds` 1800 (gün 22:00Z'de değişir).
  - Saat 2027-03-27T22:00Z'de (Pazar 01:00 EEST): `open` true, `today` `2027-03-28`, `next.time`
    `02:00`, `refreshInSeconds` 3600.


### QA CLARIFY turuyla eklenenler (2026-11-27)

- **AC-92.** *(B1)* `America/New_York` saat dilimli programlı şube, Saat 2026-12-05T02:00Z (yerelde
  Cuma 2026-12-04 21:00; UTC tarihi 12-05):
  - `today` `2026-12-04`, `open` false;
  - `next` `{"date":"2026-12-05","time":"10:00"}`;
  - `refreshInSeconds` 10800 (yerel gece yarısı 05:00Z).
- **AC-93.** *(B2(a))* Cuma 20:00–23:59 açık, Cumartesi 00:00'da başlayan aralığı olmayan şube, Saat
  2026-12-04T18:00Z'de (Cuma 21:00):
  - `open` true, `next` `{"date":"2026-12-05","time":"00:00"}`, `refreshInSeconds` 10800;
  - altlık "Şu an açık · Kapanış yarın 00:00" yazar.
- **AC-94.** *(B2(b))* Pazartesi–Cumartesi 00:00–23:59, Pazar kapalı şube, Saat 2026-12-07T09:00Z'de
  (Pazartesi 12:00):
  - `open` true, `next` `{"date":"2026-12-13","time":"00:00"}`, `refreshInSeconds` 43200;
  - altlık "Şu an açık · Kapanış Pazar 00:00" yazar.
- **AC-95.** *(B3(i))* AC-84'ün `Europe/Berlin` şubesi, Saat 2027-03-27T23:30Z'de (Pazar 00:30 CET):
  `refreshInSeconds` 81000.
- **AC-96.** *(B4(a))* Programlı şubede 2026-12-05 (Cumartesi) tam gün kapalıyken, Saat
  2026-12-05T09:00Z'de `open` false ve `next` `{"date":"2026-12-07","time":"09:00"}`'dır (Pazar haftalık
  kapalı).
- **AC-97.** *(B4(b))* Programlı şubede 2027-01-04–2027-01-13 (10 gün) tam gün kapalıyken, Saat
  2027-01-03T09:00Z'de (Pazar) `next` `{"date":"2027-01-14","time":"09:00"}`'dır (11 gün ileride).
  Altlık "Şu an kapalı · Açılış 14 Ocak 09:00" yazar.
- **AC-98.** *(B4(c))* Programlı şube, Cuma 2026-12-04:
  - Saat 15:00:00Z (yerel 18:00:00) → `open` false.
  - Saat 06:00:00Z (yerel 09:00:00) → `open` true.
  - Saat 14:59:59.5Z (yerel 17:59:59.5) → `open` true, `refreshInSeconds` 1.
- **AC-99.** *(B4(d))* Yalnız Pazartesi 09:00–10:00 açık şube:
  - Saat 2026-12-07T08:00Z'de (Pazartesi 11:00): `next` `{"date":"2026-12-14","time":"09:00"}` (bugün + 7).
  - Aynı şubede 2026-12-14–2026-12-20 tam gün kapalıyken aynı anda: `next`
    `{"date":"2026-12-21","time":"09:00"}` (en son `through` + 1, sınır + 7'nin içinde).
- **AC-100.** *(B5)* Birim testi, sahte zamanlayıcı:
  - `refreshInSeconds` = 60 olan yanıttan 10 saniye sonra cihaz saati (`Date`) 1 saat ileri alınır.
    Tekrar okuma 60. saniyeden önce yapılmaz.
  - Cihaz saati 1 saat geri alınır. Okuma 60. saniyede yapılır.
- **AC-101.** *(B6)* AC-58'in ikizi, tarayıcı saat dilimi `America/Los_Angeles`: aynı yanıtla altlık
  aynı "şu an" metnini basar. "Bugün" etiketi Host yanıtındaki `today`'in haftanın günü olan satırdadır.
  {gün} kelimesi ("yarın", gün adı ya da tarih) `next.date` − `today` farkına göre aynı çıkar.
- **AC-102.** *(B6)* Birim testi: `"2026-12-04"` tarih dizgesinin haftanın günü, süreç saat dilimi
  (`TZ`) `America/Los_Angeles`, `Pacific/Kiritimati` ve `UTC` iken aynı (Cuma) türetilir. `"2026-12-04"`
  ile `"2026-12-14"` arasındaki fark üçünde de 10 gündür.
- **AC-103.** *(B7)* `through` alanı olmayan ve `through: null` taşıyan iki kaydetme de kabul edilir.
  Panel okuması iki durumda da `through` = `from` döner.
- **AC-104.** *(B7)* Saat 2027-01-05T09:00Z'de iki kayıtlı gövde: `[{ from 2027-01-01, through 2027-01-01 }`
  (geçmiş, geçerli)`, { from 2027-02-10, through 2027-02-01 }]` (hatalı). Yanıt `422` ve
  `specialHours.1.through` alanında `special_hours.end_before_start` taşır. Geçmiş ama hatalı tek kayıt
  `[{ from 2027-01-03, through 2027-01-01 }]` de `422` ve `specialHours.0.through` alır.
- **AC-105.** *(B8)* İlk yanıtında iki özel gün bulunan altlık, D9 yenilemesinde üç özel gün taşıyan bir
  yanıt alır. Özel gün listesi iki satırlı kalır, "şu an" satırı yeni durumu basar ve şube bloğunun
  yüksekliği değişmez.
- **AC-106.** *(B10)* `dealership` şemasında 2027-01-01'lik kaydı saklı olan şube için Saat
  2027-01-05T09:00Z'de:
  - Gövdesinde `specialHours` alanı olmayan bir adres kaydetmesinden sonra saklı kayıt sayısı değişmez
    (veritabanında sayılır).
  - Ardından yalnız 2027-02-10'luk kaydı taşıyan bir `specialHours` listesiyle yapılan kaydetmeden
    sonra 2027-01-01'lik kayıt veritabanında yoktur. 2027-02-10'luk kayıt vardır.
- **AC-107.** *(B11)* Saat 2026-11-27T22:30Z'de (İstanbul'da 11-28 01:30, New York'ta 11-27 17:30)
  `Europe/Istanbul` şubesine tek gövdeyle iki değişiklik gönderilir: saat dilimi `America/New_York`
  ve `through` = 2026-11-27 olan bir kayıt. Kayıt saklanır ve panel okumasında görünür, çünkü yeni
  bölgede bugün 2026-11-27'dir.

---

## Definition of Done

- [x] Önerilen kararlar (**D1–D18**) onaylandı (Proje Yöneticisi kararı, 2026-11-27; şartlar (a)–(c)
      → AC-75–AC-91). **SS1–SS4** cevaplandı (sahip kararı, 2026-11-27).
- [x] QA CLARIFY turu işlendi (QA 1, 12 bulgu → D2, D4, D7, D9, D11, D14, D15, D17; AC-92–AC-107).
      *(Proje Yöneticisi kararı, 2026-11-27.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0027-ac<n>.png`),
      boş ve hata durumları dâhil; panel bölümünün ve altlığın (açık, kapalı, özel gün) Arapça
      görüntüsü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). "Şu an" kriterleri sabit `IClock` ile.
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor (`dealership`, D18).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi ("Özel Gün", "Şu An Durumu", D16).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (D6, D17: `:42` cümlesi).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. Bilerek
      değişen eski testler (§ Context tablosu) PR'da listelendi ve **çok etiketli** kaldı. İlk paket
      ölçümü head commit'ten yapıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı. Taşımadan sonra `tests/Architecture.Tests`'in
      TAMAMI koşturuldu.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | | |
| **Kriter sayısı: önce → bugün** | → | |
| **Spec revizyon sayısı** | | |
| **Düzeltme turu sayısı** | | |
| **Dış bulgu (gerçek/gürültü)** | / | |
| **Uygulama içi bulgu** | | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | |

**Bu spec'ten çıkarılan ders:**
