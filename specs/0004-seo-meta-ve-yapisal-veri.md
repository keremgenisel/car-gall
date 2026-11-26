# 0004 — SEO Meta Enjeksiyonu ve Yapısal Veri

| | |
|---|---|
| **Durum** | **Onaylandı** (2026-11-26 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (B1–B13 → **D22**, **D23** ve D2, D4, D7, D11, D12, D15, C13 ile on üç kriterin düzeltmesi, yedi yeni kriter) |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Host` (belge etiketleri, durum kodu, sitemap, robots) · `VehicleCatalog.Contracts` (detay okuması; görünür ilanların adresleri) · `Content.Contracts` (sayfa okuması; bir dilde yayındaki sayfaların adresleri) · `Dealership.Contracts` (görünen ad; ana sayfanın yapısal verisi için şube adı, adresi, telefonu) · `frontend/` (yalnız çeviri anahtarları ve iki parçalı tanınmayan yolun "bulunamadı" ekranı — **D8**) |
| **Bağımlı spec'ler** | `0001`, `0002` (detay, **C13** ayırt edilemezlik), `0003`, `0014` (ana sayfa, **D22**), `0016b` (**C13** ön yükleme), `0017` (içerik sayfaları, **D10** gerçek 404 devri, **D13**/**D27** eski slug), `0018` (**D26**/**D35** bülten sayfaları), `0020` (görünen ad), `0022` (ana sayfa metinleri, **D20** önbellek) — hepsi tamamlandı |
| **Dayanak** | ADR 0003 Ek 1, sıra #8 · `0002` **E11** · sahip kararı 2026-11-26 ("0004 ile başla") |
| **Karar bekliyor** | **Takım Yöneticisi:** yok — D1–D21 onaylandı, CLARIFY turu D22–D23 ile işlendi (Takım Yöneticisi (ajan) kararı, 2026-11-26). **Sahip:** yalnız **SS1** (alan adı) barındırma kararıyla birlikte açık; kod D2'nin yapılandırma değerini bekler. SS2–SS5 cevaplandı (sahip kararı, 2026-11-26). |

> **Analist taslağı, 2026-11-26.** Kart 2026-10-05'ten beri `Beklemede`'ydi. O tarihte
> yalnız ham R1–R6 ve C1–C3 vardı. Bu sürüm Intent, Requirements, Constraints, Context ve
> kabul kriterlerini yazıyor. Context'teki her iddia `main` @ `ad0c320`'a karşı ölçüldü. Ham
> R1–R6'nın bugünkü kodla karşılaştırması § Context → *Ham R1–R6 ile bugünkü kod* başlığında.
>
> **Onay ve QA CLARIFY turu (2026-11-26, PR #125 @ `c92c54c`).** D1–D21 onaylandı. QA 1'in on üç
> bulgusu (0 engelleyici, 8 önemli, 5 küçük) Takım Yöneticisi (ajan) kararlarıyla işlendi →
> § Context → *Verilen kararlar — QA CLARIFY turu*. Kriter numaraları **eklemeyle** büyüdü
> (AC-86–AC-92); var olan numaralar değişmedi. Sahibin SS2–SS5 cevapları § Sahibe sorular'da.

---

## Intent

Galeri bugün aracını arama motorlarında ve sosyal paylaşımda **tanıtamıyor.** Vitrinin her
sayfası, galerinin değil yazılımın adını başlık olarak taşıyor. Hiçbir sayfa kendini anlatan
bir açıklama taşımıyor. WhatsApp'ta paylaşılan bir ilan bağlantısı fotoğraf, fiyat ya da model
göstermiyor. Arama motoru üç dili birbirinin kopyası sanıyor ve satılmış bir aracın sayfası
"bulundu" yanıtı vermeye devam ediyor.

**Galeri sahibi**, ilanlarının arama sonuçlarında ve paylaşım önizlemelerinde kendi adıyla, doğru
dilde ve aracın kendisiyle görünmesini istiyor. Organik arama, pazaryerlerinden sonra bir galeri
için en büyük ikinci ziyaretçi kanalı. **Ziyaretçi** ise bir sonuca tıkladığında satılmış bir araca
ya da yanlış dildeki bir sayfaya düşmek istemiyor.

**Başarı:** JavaScript çalıştırmayan bir okuyucu (arama motoru, paylaşım önizlemesi), sunucunun
ilk yanıtında her vitrin sayfasının galeri adını, sayfaya özgü başlığını ve açıklamasını, kendi
dilindeki kanonik adresini ve diğer dillerdeki karşılıklarını bulur. İlan sayfasında aracın modeli,
yılı, kilometresi, fiyatı ve fotoğrafı da buna eklenir. Satılan ya da yayından kalkan bir ilan,
sonraki sitemap okumasında listede yer almaz ve adresi "bulunamadı" (404) yanıtı verir.

---

## Requirements

### Belge etiketleri

- **R1.** Her indekslenebilir vitrin belgesi, sunucunun ilk yanıtında, belgenin dilinde
  **sayfaya özgü bir başlık ve açıklama** taşır. Galerinin görünen adı başlığın parçasıdır.
  Yazılımın adı hiçbir başlıkta yer almaz. → **D11**, **D12**.
- **R2.** Her indekslenebilir vitrin belgesi **kendi dilinin mutlak kanonik adresini** taşır.
  Diller birbirinin kanoniği değildir. Kanonik adres sorgu dizesi taşımaz. → **D2**, **D9**.
- **R3.** Her indekslenebilir vitrin belgesi, sayfanın var olduğu her etkin dil için bir
  **dil alternatifi** ve bir **varsayılan alternatif** (`x-default`) taşır. → **D10**, **D20**.
- **R4.** Her indekslenebilir vitrin belgesi **paylaşım önizlemesi etiketleri** (Open Graph)
  taşır. İlan detayında önizleme görseli aracın ilk fotoğrafıdır. → **D13**.

### Yapısal veri

- **R5.** İlan detayı aracı ve teklifini **yapısal veri** olarak taşır (`Car` + `Offer`). Para
  birimi kurulumun para birimidir. "Fiyat sorunuz" ilanında fiyat yapısal veriye girmez. → **D14**,
  **D18**.
- **R6.** Ana sayfa galeriyi **yapısal veri** olarak taşır (`AutoDealer`): adı, adresi ve
  vitrinde görünen şubelerin adı, adresi, telefonu. *Ham R2'nin "şube sayfaları" bugün yok; yeri
  bir karardır* → **D1**, **D15**.

### Tarama

- **R7.** Her etkin dil için bir **sitemap** ve hepsini listeleyen bir **sitemap dizini** vardır.
  Sitemap okunduğu andaki vitrini yansıtır: yayından kalkan ilan bir sonraki okumada listede yer
  almaz. → **D3**, **D4**.
- **R8.** Site, tarayıcıları sitemap'e yönlendiren ve API'yi taramaya kapatan bir **robots.txt**
  yayar. → **D5**.
- **R9.** İndekslenmemesi gereken belgeler bunu açıkça bildirir (`noindex`): yönetim paneli,
  bülten teyit/çıkış sayfaları (bugün de öyle), listenin daraltılmış ya da sayfalanmış hâlleri
  ve "bulunamadı" belgeleri. → **D6**, **D7**, **D8**.

### Durum kodu

- **R10.** Var olmayan bir vitrin sayfası **HTTP 404** döner: görünmeyen ya da hiç olmamış bir
  ilan, o dilde yayında olmayan bir içerik sayfası ve tanınmayan bir yol. Gövde, ziyaretçiye
  bugünkü "bulunamadı" ekranını gösterir. → **D7**, **D8**, `0017` **D10**.

### Performans

- **R11.** Eklenen etiketler vitrinin yükleme eşiğini (C3: LCP < 2,5 s) korur. Etiketler için
  ölçülebilir bir bayt bütçesi vardır. → **D17**.

---

## Constraints

### Uyulması zorunlu

- **C1.** **Tam SSR yok** (`docs/architecture.md` § 3, § 7 Karar Defteri). Etiketler, `index.html`
  servis edilirken rota bazlı eklenir.
- **C2.** Kullanıcıya görünen hiçbir metin koda gömülmez. Sabit metinler (liste başlığı,
  açıklama kalıpları, "fiyat sorunuz") çeviri kaynağından, değişken metinler sahibi modülün
  verisinden gelir. → Altın Kural 6, **D12**.
- **C3.** `Host` iş mantığı yazmaz. Yalnız modüllerin public sözleşmelerini çağırır ve
  birleştirir. → `docs/architecture.md` § 2.F.
- **C4.** Modül sınırı: `Host` başka bir modülün tablosuna, internal tipine ya da `DbContext`'ine
  erişmez. Gereken veri sözleşmede yoksa sözleşme genişler. → Yasak Listesi 1.
- **C5.** Kurulumda gizlenmiş alanlar (**F3**) hiçbir etikete, açıklamaya ya da yapısal veriye
  girmez. Yapısal veri yalnız **D14**'ün saydığı alanları taşır.
- **C6.** "Bulunamadı" yanıtı, ilanın var olup gizlendiği durumla hiç olmadığı durumu **ayırt
  ettirmez**. Bu durum kodu, gövde ve etiketler için de geçerlidir. → `0002` **C13**.
- **C7.** `Accept-Language`'a göre yönlendirme yapılmaz. Alternatif etiketleri bunun yerini
  tutar. → Yasak Listesi 28.
- **C8.** Para `decimal`'dir. Yapısal verideki fiyat kurulumun minor unit üssü kadar ondalık
  taşır (KWD: 3). "Fiyat sorunuz" `0` değildir. → Altın Kural 4, **P1–P4**.
- **C9.** Serbest metin (galeri adı, model adı, sayfa başlığı ve gövdesi) belgeye **metin olarak**
  girer: özniteliği ya da `<script>` bloğunu kapatamaz, işaretleme olarak yorumlanmaz. → `0020`
  **R5a**, `0022` **C5**.
- **C10.** Mutlak adreslerin kökü **dağıtım yapılandırmasından** gelir. İstek başlıklarından
  (`Host`, `X-Forwarded-Host`) türetilmez. → **D2**.
- **C11.** Bugünkü belge davranışları bozulmaz: `0016b`'nin ilk görsel ön yüklemesi, `0014`
  **D22**'nin `enabled-locales`'ı, `0020`'nin görünen ad etiketi, `0022`'nin ana sayfa metinleri,
  `0018`'in `noindex` + `Referrer-Policy`'si ve `0022` **D20**'nin `Cache-Control: no-store`'u.
- **C12.** Performans eşiği **C3** (Lighthouse mobil, 3 koşu, medyan LCP < 2500 ms, CLS < 0,1)
  ana sayfa, liste ve detayda korunur. → **D17**, `docs/testing.md`.
- **C13.** Her kabul kriteri en az bir testle karşılanır. Belge kriterleri, **sunucu yanıtının
  kendisi** ayrıştırılarak doğrulanır (JavaScript çalıştırılmadan, bir HTML ayrıştırıcısıyla —
  **D22**). Yapısal veri kriterleri **D14**/**D15**'in kapalı anahtar listesine karşı **anahtar
  kümesi karşılaştırmasıyla** doğrulanır. Sitemap kriterleri yapısal iddiadır (**D22**).

### KAPSAM DIŞI

- **X1.** **Tam SSR'a geçiş** — *neden:* C1 verilmiş bir karardır; bu spec onu delmez.
- **X2.** **Arama konsolu entegrasyonu, site doğrulama etiketi, sıralama takibi, anahtar kelime
  araştırması** — *neden:* pazarlama işi, ürün işi değil.
- **X3.** **Şube sayfaları** (`/{locale}/branches/{slug}` benzeri) — *neden:* bugün vitrinde şube
  sayfası yok ve `0003` **X3** / `0020` **X4** onu bilinçli olarak açmadı. Bir sayfa türü açmak
  bu kartı SEO'dan ürün yüzeyine taşır (**D1**). Sahip **şimdilik istemiyor** (**SS4**, sahip
  kararı, 2026-11-26).
- **X4.** **`openingHoursSpecification` ve `sameAs`** — *neden:* **sahip istedi, ayrı kart**
  (**SS2**, **SS3**, sahip kararı, 2026-11-26): sosyal hesaplar ve şube çalışma saatleri, her biri
  panelden giriş + yapısal veri olarak **0004'ten sonra** ayrı küçük kartlarla gelir. Bugün ikisinin
  de verisi girilemiyor (`0003` **X2**, `0020` **X2**) ve yapısal veri sayfada görünmeyen bir bilgiyi
  iddia etmemeli (**D1**). **`geo`** ayrıca dışarıda: koordinat vitrinde gösterilmiyor (`0020`
  **X4**) ve sahip onu istemedi. *Not:* **D15**'in kapalı anahtar listesi ve **AC-52** bu kartın
  sınırını ölçer; o kartlar **D15**'i genişletir ve **AC-52**'yi kendi kapsamlarında değiştirir.
- **X5.** **Daraltılmış liste açılış sayfaları** (ör. "SUV araçlar", "Toyota" için indekslenen
  adresler) — *neden:* her eksen kombinasyonu bir adres üretir. Hangilerinin açılış sayfası
  olacağı bir içerik ve SEO stratejisi kararıdır (**D8**).
- **X6.** **`FAQPage`, `BreadcrumbList`, `ItemList` yapısal verisi** — *neden:* Google, SSS zengin
  sonucunu 2023'ten beri yetkili sağlık ve kamu siteleriyle sınırlıyor. Kırıntı ve liste verisi
  ayrı kararlar getirir. Ham R1–R6 bunları istemiyor.
- **X7.** **Twitter/X kart etiketleri** — *neden:* X, Open Graph etiketlerine düşüyor. İkinci bir
  etiket kümesi bayt bütçesini (**D17**) aynı bilgiyle harcar.
- **X8.** **Sitemap'te `lastmod`, görsel sitemap'i, 50.000 adresin üstünde bölme** — *neden:*
  ilanın son değişme anı izlenmiyor; yanlış `lastmod` hiç olmamasından kötüdür. Tek galerinin
  stoku sınırın çok altında (**D4**).
- **X9.** **Logo: ana sayfa, liste ve içerik sayfalarında önizleme görseli ve `AutoDealer.logo`**
  — *neden:* **sahip istedi, ayrı kart** (**SS5**, sahip kararı, 2026-11-26): logo panelden yükleme
  + yapısal veri olarak 0004'ten sonra gelir. Bugün logo atanamıyor (`0014` **X9**, `0020` **X1**).
  **D13**'ün "`og:image` yalnız detayda" kuralı ve **AC-33**/**AC-52** o kartta genişler.
- **X10.** **`Car` üzerinde marka, yakıt, vites gibi ek alanlar** — *neden:* detay okumasında
  marka ayrı bir alan değil (model adı birleşik etiket). Opsiyonel alanlar kurulum başına
  gizlenebilir (**F3**). Her ek alan bir gizlilik kriteri getirir (**D14**).
- **X11.** **Satılmış ilan için `410 Gone`** — *neden:* "satıldı" ile "hiç olmadı" arasındaki
  farkı ifşa eder (`0002` **C13**, **D7**).
- **X12.** **Uçta `document.title` güncellemesi** — *neden:* vitrinde gezinme tam sayfa
  yüklemesi. Rota bir kez, render anında çözülüyor (`frontend/src/app/App.tsx`). Sunucunun
  bastığı başlık yeterli.
- **X13.** **`og:locale`** — *neden:* değer bölge ister (`ar_KW` / `ar_SA`), kurulum bölge
  bilmiyor. Tahmin edilen bir bölge yanlış bir iddiadır (**D13**).

---

## Context

### Mevcut davranış — ölçüldü (2026-11-26, `main` @ `ad0c320`)

**Belgenin `<head>`'i bugün neyi taşıyor** (`src/Host/Localization/IndexHtmlRenderer.cs`):

| Etiket | Hangi belgede | Kaynak |
|---|---|---|
| `<html lang dir style>` | hepsi | `WriteAsync` — tema `0005`/`frontend-002` |
| `<meta name="enabled-locales">` | hepsi (panel dâhil) | `0014` **D22** |
| `<meta name="gallery-display-name">` | hepsi | `0020` **P1**/**D16**; okunamazsa basılmaz |
| `<meta name="home-text-*">` (6 ad) | yalnız `/{l}/` ve `/{l}/cars` | `0022` **D6**/**D21** |
| `<meta name="robots" content="noindex">` + `Referrer-Policy: no-referrer` | yalnız `/{l}/newsletter/confirm\|unsubscribe` | `0018` **D26**/**D35** |
| `<link rel="preload" as="image" … fetchpriority="high">` | yalnız `/{l}/cars/{slug}` (üç parçalı yol) | `0016b` **C13** / **D37** — `FindFirstImageAsync` |
| `Cache-Control: no-store` | hepsi | `0022` **D20** |

- **`<title>` her vitrin ve panel belgesinde `CarGallery`.** `frontend/index.html` bunu sabit
  taşıyor ve Host değiştirmiyor. Uç da `document.title`'a hiç yazmıyor (`frontend/src`'de sıfır
  sonuç). Bu, yazılımın adı. `0020` **R5a** onun ziyaretçiye gösterilmemesini ilke edindi.
- **Açıklama, kanonik, dil alternatifi, Open Graph ve JSON-LD hiçbir belgede yok.** `src/`,
  `frontend/src` ve `frontend/index.html`'de `canonical`, `hreflang`, `og:`, `ld+json` taramaları
  sıfır sonuç veriyor.
- **robots.txt ve sitemap yok.** `frontend/public`'te yalnız `favicon.svg` ve `icons.svg` var.
  Host'ta bu adresleri karşılayan bir rota yok. Statik dosya ara katmanı `Program.cs:264`'te.
- **Panel `noindex` taşımıyor.** `/admin` belgesi `AdminShellRenderer` → `IndexHtmlRenderer`
  yolundan geçiyor ve robots etiketi yalnız bülten sayfalarına basılıyor.

**Durum kodları** (`src/Host/Localization/LocalePrefixMiddleware.cs`):

- `/` → varsayılan locale'e **301**. Etkin olmayan önek → **404**, gövde boş.
- Geri kalan **her vitrin yolu 200** ile kabuğu alıyor: görünmeyen ya da olmayan ilan, olmayan
  içerik sayfası, tanınmayan yol. "Bulunamadı" bugün yalnız uçtaki bir ekran. `0017` **D10**/**X11**
  gerçek 404'ü tüm vitrin için tek karar olarak **bu karta devretti**.
- `/{l}/{x}` biçimindeki tek parçalı yol, ayrılmış değilse (`ContentReservedSlugs`: `cars`,
  `admin`, `api`, `assets`, `newsletter`), `Content`'e "eski slug mu?" diye soruluyor. Eşleşirse
  güncel adrese **301** veriliyor (`0017` **D13**/**D27**). Eşleşmezse kabuk 200 ile basılıyor.
- `/{l}/?bodyType=…` (liste parametresiyle eski liste adresi) → `/{l}/cars?…`'a **301**
  (`0014` **D17**/**D21**).
- **İki parçalı tanınmayan yol (`/tr/foo/bar`) uçta LİSTEYİ gösteriyor**
  (`frontend/src/shared/lib/route.ts`, son dal; `0017` **D9**/AC-38'in bilinçli davranışı). Yani
  sonsuz sayıda adres 200 ile listenin kopyasını veriyor → **D8**.

**Rotalar** (`frontend/src/shared/lib/route.ts`): `home` (`/{l}/`) · `list` (`/{l}/cars`) ·
`detail` (`/{l}/cars/{slug}`) · `content` (`/{l}/{slug}`) · `newsletter-confirm` ·
`newsletter-unsubscribe` · `admin` (`/admin/…`, locale öneki yok). **Şube sayfası rotası YOK.**

**Ham R2'nin istediği veri bugün ne durumda:**

- Şubenin domain'de `Slug`'ı, `Coordinate`'ı (opsiyonel), `Phone`'u, `WhatsApp`'ı, `Email`'i,
  `TimeZone`'u var (`Dealership/Domain/Branch.cs`). Ama **şube sayfası yok** (`0003` **X3**,
  `0020` **X4**).
- **Çalışma saati verisi yok.** Domain'de saat, aralık ya da istisna tipi yok. `0003` **X2** onu
  ayrı bir karta ayırdı, `0020` **X3** de açmadı.
- **Sosyal hesaplar** domain'de var (`Gallery.SocialLinks`, `AddSocialLink`), ama `AddSocialLink`'in
  domain dışında **hiç çağıranı yok** ve panelde giriş yüzeyi yok (`0020` **X2**).
- **Koordinat** panelde girilebiliyor (`0020`), vitrinde gösterilmiyor (`0020` **X4**).
- **Public okuma:** `Dealership.Contracts`'ta yalnız `IGalleryDisplayNameQueries`
  (`FindDisplayNameAsync`) public. Şubelerin ad/adres/telefonunu dönen `IPublicGalleryQueries`
  **internal** (`Dealership/Application/PublicGalleryQueries.cs`). Yalnız `Active` şubeleri dönüyor
  (`:56`) ve yalnız HTTP ucu (`/api/dealership/gallery`) üzerinden açık. → Host'un kullanabilmesi
  için sözleşmenin genişlemesi gerekiyor (C4).

**Detay ve fiyat verisi:**

- `IShowcaseListingDetailQueries` (`VehicleCatalog.Contracts/Detail`): `FindAsync(slug, locale)` →
  `ShowcaseListingDetailDto` (`Slug`, `IsReserved`, `Images` [her biri Thumbnail/Display/Large],
  `ModelName`, `ModelYear`, `MileageKilometers`, `Price`, `BranchName`, `Attributes`,
  `PaintChangeMap`). `FindFirstImageAsync(slug)` yalnız ön yükleme için. Host bugün **yalnız
  ikincisini** çağırıyor.
- `ShowcasePriceDto(OnRequest, Amount, CurrencyCode, MinorUnitExponent)`. Para birimi
  `Currency:Code` / `Currency:MinorUnitExponent` dağıtım ayarından `CatalogCurrencyOptions`'a
  giriyor (`Program.cs:69-70`, `VehicleCatalogModule.cs:98`) ve DTO onu taşıyor. → `priceCurrency`
  için ikinci bir kaynak gerekmez.
- `ModelName` belgenin dilinde, karşılığı yoksa kanonik Latin adda (`0021` **D24**). **Marka ayrı
  bir alan değil** (→ X10).
- Görünürlük: yalnız `Yayında` ve `Rezerve` (**A3**). Satılmış ve arşivlenmiş ilan görünmez ve
  `0002` **C13** gereği olmayan ilandan ayırt edilemez.

**İçerik sayfaları** (`Content.Contracts/Pages`):

- `FindBySlugAsync(locale, slug)` yalnız o dilde **yayında** olan sayfayı döner.
  `ContentPagePublicDto.Alternates` sayfanın yayında olduğu **diğer** dillerdeki slug'ları taşıyor
  (`0017` **R15**/**D14**: *"`0004`'ün `hreflang`'i de aynı veriyi ister"*).
- Slug **locale başına** (`0017` **D12**). Dil alternatifi adresi her dilde farklı olabilir.
- Gövde CommonMark alt kümesi (`0017` **R16**): `##`/`###`, liste, bağlantı, kalın, italik.
  Açıklamaya düz metin olarak girmesi için işaretlerin ayıklanması gerekir (→ **D11**).
- `NavigationAsync` yalnız **sistem sayfalarını** (altlık bağlantıları) dönüyor. Bir dilde
  yayındaki **bütün** sayfaların adreslerini dönen bir sözleşme **yok** (→ sitemap için C4).
- Görünür ilanlar için de "bütün adresler" sözleşmesi yok. Mevcut olan sayfalı
  `IShowcaseListingQueries.GetAsync`.

**Mutlak adres kökü:**

- Sitenin kök adresi bugün **yalnız `Notifications:PublicBaseUrl`** olarak var. Bu, e-postalardaki
  bağlantıların kökü (`docs/deploy.md:59`, `Program.cs:143`, `NotificationsOptions.cs:65` mutlak
  olmasını zorunlu tutuyor). Host'un kendi kök adres ayarı yok.
- `Media:PublicBaseUrl` medya kovasının adresi. Geliştirme tohumunda görseller **göreli**
  (`/seed-media/frame-10-800x600.jpg`, son perf koşusundan). → `og:image` ve yapısal verideki
  görsel mutlak olmalı (→ **D13**).
- `ForwardedHeadersSetup` yalnız `X-Forwarded-For | X-Forwarded-Proto` işliyor (ADR 0012 / K4),
  `X-Forwarded-Host`'u değil. `docs/deploy.md:128` ters proxy'nin `Host` başlığını değiştirmeden
  iletmesini istiyor. Yine de `Host` başlığı istemcinin denetimindedir → C10.

**Olaylar ve tüketicileri** (ham R6'nın varsayımı):

- `VehicleCatalog.Contracts.Events`'te `VehiclePublished`, `VehicleUnpublished`, `VehicleReserved`,
  `VehicleSold`, `VehicleArchived`, `VehiclePriceChanged` var. `Dealership.Contracts.Events`'te
  `BranchOpened`, `BranchDetailsChanged`, `BranchDeactivated` var.
- **Tüketiciler:** `VehiclePublished` → yalnız `Notifications` (`VehiclePublishedCandidateHandler`,
  `0018`). Şube olayları → `VehicleCatalog` (`BranchEventHandlers`). `VehicleUnpublished`, `VehicleSold`,
  `VehicleArchived` ve `VehiclePriceChanged`'in **hiç tüketicisi yok.**
- Outbox dağıtıcısı alıcısı olmayan olayı **işlenmiş** sayıyor (`0018` Context,
  `BuildingBlocks/Outbox/OutboxDispatcher.cs`). Olayla beslenen bir sitemap ilk açılışta geçmiş
  yayınları hiç görmez. Kaldırma olayları bugün yere düşüyor → **D3**.

**Performans tabanı** (son C3 koşusu: run `44549454484`, 2026-11-19, `0018`'in son başı — #122):

| Sayfa | Medyan LCP | Pay (2500'e) | LCP öğesi |
|---|---|---|---|
| Ana sayfa `/tr/` | 2279 ms | 221 ms | `<p class="home-hero__body">` |
| Liste `/tr/cars` | 2134 ms | 366 ms | `<button … ui-button--text disabled>` |
| Detay `/tr/cars/nissan-qashqai-2020-b81323` | **2411 ms** | **89 ms** | `<img class="gallery__image">` (ön yüklenen görsel) |

- Koşular arası gürültü yaklaşık 80 ms (`0018` döneminde ölçüldü). Detayın payı bunun hemen üstünde.
- **Detayın LCP öğesi ön yüklenen görsel.** Belgeye eklenen her bayt ve belgenin hazırlanmasını
  geciktiren her okuma, görselin keşfini geciktirir. Bugün Host detay belgesinde yalnız
  `FindFirstImageAsync`'i bekliyor. Başlık, açıklama ve yapısal veri `FindAsync`'in tam okumasını
  gerektiriyor → **D17**.
- `perf.yml` içerik sayfalarını ölçmüyor (üç adres: ana sayfa, liste, detay).

### Ham R1–R6 ile bugünkü kod

| Ham madde | Bugün | Bu sürümde |
|---|---|---|
| R1 `Car` + `Offer`, `priceCurrency` yapılandırmadan | Veri `FindAsync`'te var, para birimi DTO'da | **R5** (**D14**) |
| R2 Şube sayfaları `AutoDealer` + `LocalBusiness`, saatler, `geo`, `sameAs` | Şube sayfası yok, saat verisi yok, sosyal hesap girilemiyor | **R6**: ana sayfada galeri düzeyinde `AutoDealer` (**D1**, **D15**). Saat/`geo`/`sameAs` → X4 |
| R3 `hreflang` + `x-default` | Yok | **R3** (**D10**). İçerik sayfasında dil başına slug |
| R4 Kanonik kendi locale'i | Yok | **R2** (**D9**) |
| R5 Başlık, açıklama, OG rota bazlı | `<title>` sabit `CarGallery` | **R1**, **R4** (**D11**, **D13**) |
| R6 Locale başına sitemap, olaylar günceller | Sitemap yok. Kaldırma olaylarının tüketicisi yok | **R7**: istek anında (**D3**). `docs/architecture.md` § 3'ün cümlesi değişir |

### Geçerli kurallar

**A3** (yalnız `Yayında`/`Rezerve` görünür) · **F3** (gizli alanlar yanıtta yok) · **P1–P4**
(para, "fiyat sorunuz" ≠ 0) · `docs/architecture.md` § 3 (locale öneki zorunlu, `Accept-Language`
yönlendirmesi yok, SEO alt başlığı) · § 2.F (Host yalnız birleştirir) · Yasak Listesi 1, 25, 28,
31 · `0002` **C13** · ADR 0013 (çeviri kataloğu vitrin/panel ayrımı ve ölçüm protokolü).

### Bilinen tuzaklar

1. **Kök adresi istekten türetmek.** `Host` başlığıyla kurulan bir kanonik ya da `og:url`, sahte
   başlıklı tek bir istekle başka bir alan adına işaret eder. Belge `no-store` olduğu için
   önbellek zehirlenmesi bugün sınırlı, ama sitemap ve robots.txt da aynı kökü kullanacak (C10).
2. **Disallow edilen sayfa `noindex`'ini gösteremez.** robots.txt'te `/admin` ya da bülten
   sayfaları kapatılırsa tarayıcı belgeyi hiç okumaz. Etiketi göremez ve dışarıdan bağlantı
   alan adres "içeriksiz" olarak indekslenebilir (**D5**).
3. **`</script>` ile yapısal veriden kaçmak.** Model adı ve galeri adı serbest metin. JSON
   kodlaması `<`'i kaçırmazsa bir ad `<script type="application/ld+json">` bloğunu kapatır ve
   belgeye işaretleme ekler (C9). Bugünkü `AttributeEncode` öznitelik içindir, JSON için değildir.
4. **Gizli ilan ile olmayan ilanı süreyle ayırt ettirmek.** 404 kararı için tam okuma yapılıp
   gizli ilanda farklı bir yol izlenirse, yanıt süresi farkı `0002` **C13**'ü deler.
5. **Detayın payı 89 ms.** Belge hazırlanırken yapılan ikinci bir okuma ya da birkaç kilobaytlık
   yapısal veri tek başına eşiği aşabilir. Ölçüm yapılmadan "küçük" denmemeli (**D17**).
6. **Varsayılan dilde yayında olmayan içerik sayfası.** `x-default` var olmayan bir adrese
   işaret ederse arama motoru alternatif kümesini toptan reddedebilir (**D10**).
7. **Biçim işaretli açıklama.** Gövdenin ilk 160 karakteri `## Başlık **kalın** [bağ](…)` olarak
   kesilirse arama sonucunda işaretler görünür (**D11**).
8. **Olayla beslenen sitemap.** Alıcısı olmayan olay işlenmiş sayıldığı için tüketiciyi ilk kez
   ekleyen kart geçmişi alamaz (`0018`'in dersi). Kaldırma olayları bugün zaten kayboluyor (**D3**).
9. **Uç sayfa adresini yanlış sayfaya düşürüyor.** İki parçalı tanınmayan yol bugün listeyi
   gösteriyor. Sunucu 404 dönerken uç listeyi basarsa durum kodu ile ekran çelişir (**D8**).
10. **Fiyatı yerel biçimle yapısal veriye yazmak.** `1.250.000,00` ya da Arapça rakam şema
    doğrulamasını geçmez. Yapısal veri dilden bağımsız sayı biçimi ister, açıklama ise dilin
    biçimini (**D14**, **D11**).

### Verilen kararlar — 2026-11-26 (D1–D21)

**Takım Yöneticisi (ajan) kararı, 2026-11-26: D1–D21 onaylandı.** Her karar bugünkü koda karşı
ölçülerek önerilmişti. Biçim: **karar** · *gerekçe* · *bedel*. CLARIFY turunun değiştirdiği
kararlar (D2, D4, D7, D11, D12, D15, D17) yerinde güncellendi ve değişiklik "CLARIFY B<n>" diye
işaretlendi.

**D1 — Şube sayfası açılmaz. Ham R2'nin yapısal verisi ana sayfaya, galeri düzeyinde taşınır.**
(R6, X3, X4) Ana sayfa tek bir `AutoDealer` taşır. Vitrinde görünen her şube onun altında ad, adres
ve telefonla yer alır. `openingHoursSpecification`, `geo` ve `sameAs` basılmaz.
*Gerekçe:* şube sayfası yok ve iki kart (`0003` **X3**, `0020` **X4**) onu bilinçli olarak açmadı.
Bir sayfa türü açmak bu kartı ürün yüzeyine taşır. Saat verisi yok. Koordinat ve sosyal hesaplar
vitrinde görünmüyor, ve yapısal veri sayfada olmayan bilgiyi iddia etmemeli. `AutoDealer`
schema.org'da `LocalBusiness`'ın alt tipidir, yani ham R2'nin "`AutoDealer` + `LocalBusiness`"
isteği tek tiple karşılanır.
*Bedel:* yerel aramada şube başına ayrı sonuç ve "şu an açık" bilgisi yok. **SS4** cevaplandı
(şimdilik hayır, sahip kararı 2026-11-26): karar kalıyor. Saatler **SS3** ile ayrı kartta gelir.

**D2 — Mutlak adreslerin kökü tek bir dağıtım değeridir: e-postalardaki bağlantılarla aynı kök.**
(R2, C10) Kanonik, dil alternatifleri, `og:url`, `og:image`'in göreli adresleri, sitemap ve
robots.txt aynı kökü kullanır. Kök istek başlıklarından türetilmez. Değer eksik ya da mutlak
değilse uygulama ayağa kalkmaz. *(CLARIFY B5 ile kesinleşti:)* Kök yalnız **`http` ya da
`https` şemalı mutlak** bir adrestir ve **yol bölümü taşımaz**; sondaki `/` normalize edilir
(`https://x.test/` ile `https://x.test` aynı kökü üretir). Boş değer, `/galeri`, `galeri.com`,
`ftp://x.test` ve `https://x.test/yol` reddedilir. Bugünkü `Notifications:PublicBaseUrl`
doğrulaması (`Uri.TryCreate(…, UriKind.Absolute)`) Unix'te `/galeri`'yi `file:` adresi sayıp kabul
ediyor (QA repro'su) → bu doğrulama yeterli değil.
*Gerekçe:* bir sitenin tek bir adresi vardır. İki ayrı ayar er geç iki farklı adres olur (bülten
bağlantısı bir alana, kanonik başka bir alana). İstekten türetmek tuzak 1'dir.
*Bedel:* bugün `Notifications` adını taşıyan ayar artık siteye ait. Adının ve sahipliğinin nasıl
düzenleneceği Developer'ın kararıdır, `docs/deploy.md` buna göre güncellenir. **Alt dizinde
barındırma** (`https://alan.com/galeri/`) desteklenmez: kök yol taşımadığı için site alan adının
kökünde yaşamak zorundadır.

**D3 — Sitemap istek anında, vitrinin o anki hâlinden üretilir. Olaylarla beslenen bir depo
kurulmaz.** (R7) Ham R6'nın "olaylar günceller" cümlesi ve `docs/architecture.md` § 3'ün aynı
cümlesi bu kararla değişir. Karar Defteri'ne işlenir (DoD T8).
*Gerekçe:* kaldırma olaylarının (`VehicleUnpublished`, `VehicleSold`, `VehicleArchived`) bugün hiç
tüketicisi yok ve outbox onları işlenmiş sayıyor. Olayla beslenen bir depo ilk açılışta geçmişi
alamaz (tuzak 8). Depo bir modülün tablosu olmak zorunda, çünkü Host durum tutamaz (C3). Tek
galerinin stoku birkaç yüz ilan, sorgu ucuz. Arama motorları sitemap'i seyrek okur.
*Bedel:* her sitemap okuması bir sorgu. Sitemap'in yanıt süresi ayrı bir kriterle sınırlanmıyor
(perf C3 belge sayfalarını ölçüyor). Stok on binlere çıkarsa yeniden değerlendirilir.

**D4 — Sitemap yapısı: bir dizin ve dil başına bir dosya.** (R7, X8) Adresler `/sitemap.xml`
(dizin) ve `/sitemap-{locale}.xml`. Bir dilin dosyası şunları taşır: o dilin ana sayfası,
parametresiz liste, görünür her ilanın detayı (Rezerve dâhil) ve o dilde **görünür** her içerik
sayfası — `FindBySlugAsync`'in o dilde döndüreceği sayfa; girdisiz SSS yayında sayılmaz (`0017`
**R20**) ve sitemap'e girmez (CLARIFY B10). Bülten sayfaları, panel, parametreli liste ve "bulunamadı" belgeleri girmez. `lastmod`
basılmaz. Dil alternatifleri sitemap'e yazılmaz, belgede taşınır.
*Gerekçe:* dil başına dosya, bir dilin indekslenme durumunu ayrı izlemeyi sağlar. Alternatifler
belgede zaten var. Aynı bilgiyi iki yerde taşımak iki yerde çelişme fırsatıdır. Satılmış ilanın
son değişme anı izlenmiyor (X8).
*Bedel:* sitemap üzerinden dil kümesini okuyan araçlar onu belgeden okumak zorunda.

**D5 — robots.txt: her şeye izin, yalnız `/api/` kapalı, sitemap dizini bildirilir.** (R8) `/admin`
ve `/{l}/newsletter/…` **kapatılmaz**. Onlar `noindex` ile dışarıda tutulur.
*Gerekçe:* tuzak 2. Kapatılan sayfa `noindex`'ini gösteremez. API yanıtları belge değildir ve
tarama bütçesini harcar.
*Bedel:* tarayıcı panelin giriş ekranını okur (içerik yok, `noindex`).

**D6 — Panelin her belgesi `noindex` taşır.** (R9) `/admin` ve alt yolları.
*Gerekçe:* panel bağlantısı dışarıda paylaşılabilir (`docs/deploy.md`'nin kurulum adımı
`https://<alan-adı>/admin` diyor). Giriş ekranı arama sonucunda görünmemeli.
*Bedel:* yok.

**D7 — Gerçek HTTP 404: görünmeyen/olmayan ilan ve o dilde yayında olmayan/olmayan içerik
sayfası.** (R10, C6, X11) Gövde bugünkü kabuktur ve uç bugünkü "bulunamadı" ekranını basar (`0002`
R5, `0017` **D10**). 404 belgesi `noindex` taşır. Kanonik, dil alternatifi, Open Graph ve yapısal
veri taşımaz. Gizli ilan ile olmayan ilan aynı yanıtı alır: durum kodu, gövde ve etiketler aynıdır,
süre farkı yoktur. Eski içerik slug'ının 301'i (`0017` **D13**) önce gelir ve korunur.
*Gerekçe:* `0017` **D10** kararı tüm vitrin için tek kural olarak buraya devretti. "Bulundu" diyen
bir satılmış ilan sayfası arama sonucunda kalır (soft 404).
*Bedel:* içerik sayfasının belgesi artık `Content`'e bir okuma yapar. Bugün yalnız eski slug
okuması var (`0017` **D27**), detay belgesi ise tam okuma yapar (**D17**). *(CLARIFY B2:)* Tamamlanmış
kartların üç testi bu kararla değişir ve davranış değişikliği PR'da yazılır (DoD T11): `0017`
**AC-41** (`tests/Host.Tests/IcerikZinciriTests.cs:151-160`, `/tr/hic-olmayan` belgesi 200 → **404**),
`0017` **AC-51**'in `/en/kvkk` satırı (`:174-176`, → **404**) ve `0017` **AC-38**
(`frontend/src/features/content/IcerikVitrin.test.tsx:71`, **D8**). Ayrıca `0016b`'nin ön yükleme
testi (`tests/Host.Tests/IlkGorselOnYuklemeTests.cs:35-39`) bağlantının hemen ardından
`</head>` geldiğini ölçüyor; **AC-80** bağlantının SEO etiketlerinden **önce** gelmesini istediği
için bu konum iddiası da değişir.

**D8 — İki parçalı tanınmayan vitrin yolu (`/{l}/x/y`) 404 döner ve uç "bulunamadı" ekranını
basar.** (R10) `0017` **D9**/AC-38'in "listeye düşer" davranışı bu yolda kalkar. Üç ve daha çok
parçalı yol (detay dışında) da aynı kurala uyar.
*Gerekçe:* listeyi 200 ile veren sonsuz sayıda adres, listenin sonsuz kopyası demek (tuzak 9).
Sunucu 404 dönerken uç listeyi basarsa durum kodu ile ekran çelişir. `0017` **D9** bu kuralın
tek parçalı yarısını zaten kaldırmıştı.
*Bedel:* uçta küçük bir rota değişikliği ve `0017` AC-38'in testi bu kriterle değişir. Bu bir
**revizyon değildir**, `0017` tamamlanmış bir karttır. Ama davranış değişikliği PR'da açıkça yazılır.

**D9 — Kanonik biçim.** (R2) Ana sayfa `{kök}/{l}/` (sonda eğik çizgi), liste `{kök}/{l}/cars`,
detay `{kök}/{l}/cars/{slug}`, içerik `{kök}/{l}/{slug}`. Sorgu dizesi hiçbir zaman kanoniğe
girmez. `/tr` ile `/tr/` aynı kanoniği taşır.
*Gerekçe:* `utm_source` ve benzeri izleme parametreleri aynı sayfanın sonsuz adresini üretir.
Eğik çizgili biçim `/` yönlendirmesinin hedefidir (`LocalePrefixMiddleware`: `Location: /{l}/`).
*Bedel:* yok.

**D10 — Dil alternatifi kümesi.** (R3) Ana sayfa, liste ve detay **etkin her dil** için bir
alternatif taşır: detayın slug'ı her dilde aynıdır, kümede belgenin kendi dili de vardır. İçerik
sayfası yalnız **yayında olduğu dillerin** alternatifini taşır, her biri o dilin slug'ıyla.
`x-default` varsayılan dilin adresidir. İçerik sayfası varsayılan dilde yayında değilse `x-default`
basılmaz.
*Gerekçe:* `docs/architecture.md` § 3. İki dilli kurulumda üçüncü dil yoktur (`0014` **D22**'nin
dersi). Var olmayan adrese işaret eden alternatif kümeyi geçersiz kılar (tuzak 6).
*Bedel:* yok.

**D11 — Başlık ve açıklamanın içeriği.** (R1)

| Belge | Başlık | Açıklama |
|---|---|---|
| Ana sayfa | Görünen ad | Kaydedilmiş giriş açıklaması (`0022`), yoksa katalogdaki varsayılan |
| Liste | Katalogdaki liste başlığı · görünen ad | Katalogdaki liste açıklaması |
| Detay | `{model adı} {yıl}` · görünen ad | Yıl, kilometre, fiyat (ya da "fiyat sorunuz") ve şube adı, dilin sayı ve para biçimiyle |
| İçerik | Sayfa başlığı · görünen ad | Gövdenin biçim işaretlerinden arındırılmış düz metninin ilk en çok 160 karakteri, kelime ortasında kesilmeden |
| 404 (ilan ve içerik) | `{content.notFound.title}` · görünen ad — ilan ve içerik için **tek metin** (C6) | basılmaz |
| Bülten teyit/çıkış, panel | Görünen ad; okunamazsa katalogdaki yedek başlık | basılmaz |

Görünen ad okunamazsa ya da boşsa ` · görünen ad` eki düşer. Ana sayfanın başlığı o durumda
katalogdaki ana sayfa başlığı olur. Yazılımın adı (`CarGallery`) hiçbir durumda basılmaz;
`frontend/index.html:6`'daki `<title>CarGallery</title>` hiçbir belgede kalmaz (CLARIFY B7). 404
başlığı için `content.notFound.title` seçildi çünkü genel bir metin: ilanın "vitrinde değil"
metni (`detail.notFound.title`) ilan ile içerik arasında ayrım yapardı ve sayfa türünü ele verirdi.
*Gerekçe:* başlık ziyaretçinin arama sonucunda ilk okuduğu satır. Galerinin adı her sayfada
olmalı, sayfaya özgü kısım önce gelmeli. Ana sayfa açıklaması galerinin kendi yazdığı metin
(`0022`). Ad okunamazken sayfanın düşmemesi `0020` **R5a**'nın deseni.
*Bedel:* içerik sayfasının açıklamasını galeri ayrıca yazamaz (bir "meta açıklama" alanı yok).
Gerekirse ayrı kart.

**D12 — Sabit metinlerin kaynağı: ön yüz deposundaki çeviri dizini.** (C2) *(CLARIFY B6, seçenek
a:)* Ekranda **zaten görünen** metinler **mevcut vitrin anahtarından** okunur: `home.intro.body`,
`detail.priceOnRequest`, `detail.mileageValue`, `detail.notFound.*`, `content.notFound.*`. Yeni
ve **vitrin paketine girmeyen** ayrı bir kümeye yalnız **SEO'ya özgü** metinler girer: liste
başlığı ve açıklaması, ana sayfanın ve bülten/panel belgelerinin yedek başlığı, açıklama kalıpları.
Eksik anahtar kontrolü bu kümeyi de kapsar. Host kodunda metin tutulmaz.
*Gerekçe:* `docs/architecture.md` § 3: arayüz metinlerinin sahibi ön yüz deposu. İkinci bir
katalog (Host içinde) iki kaynak demek. Vitrin paketine girmemesi ilk yüklemeyi büyütmemek için
şart (ADR 0013, **D17**).
*Bedel:* Host hem vitrin kataloğunu hem bu kümeyi dağıtım paketinde okuyabilmeli. Nasıl
paketleneceği Developer'ın kararıdır. Ekrandaki metin değişirse belgenin metni de değişir —
bilinçli: ikisi aynı şeyi söylemeli.

**D13 — Open Graph kümesi.** (R4, X7, X9, X13) `og:title` ve `og:description` başlık ve açıklamayla
aynıdır. `og:url` kanoniktir. `og:type` her belgede `website`. `og:site_name` görünen addır ve ad
okunamazsa basılmaz. `og:image` yalnız detayda basılır: ilk görselin büyük türevi, mutlak adres
(göreli medya adresi **D2**'nin köküyle tamamlanır). Görselsiz ilanda `og:image` yoktur.
`og:locale` basılmaz.
*Gerekçe:* paylaşım önizlemesinin işe yarayan en küçük kümesi bu. Logo olmadığı için diğer
sayfaların görseli yok (X9). Bölgesiz `og:locale` geçersizdir (X13).
*Bedel:* ana sayfa paylaşıldığında önizleme görselsiz.

**D14 — `Car` + `Offer` yapısal verisinin alanları.** (R5, C5, C8, X10) Tam küme şudur:
`@context`, `@type: Car`, `name` (belgenin dilinde model adı), `vehicleModelDate` (yıl),
`mileageFromOdometer` (`QuantitativeValue`: `value` = km, `unitCode` = `KMT`), `image` (görsellerin
büyük türevleri, mutlak, sırayla), `url` (kanonik) ve `offers`. `offers` şunları taşır:
`@type: Offer`, `price` (nokta ondalıklı, binlik ayracı yok, Latin rakam, minor unit üssü kadar
ondalık), `priceCurrency` (yanıttaki para birimi kodu = dağıtım ayarı), `availability` ve `url`.
"Fiyat sorunuz" ilanında `offers` **hiç basılmaz**. Başka alan yoktur.
*Gerekçe:* her alan detay okumasında zaten görünür olan bir değer. Kapalı liste, gizlenebilir bir
alanın (**F3**) sızmasını yapısal olarak imkânsız kılar. Fiyatsız `Offer` şemada geçersizdir.
Yapısal veri dilden bağımsız sayı biçimi ister (tuzak 10).
*Bedel:* marka, yakıt, vites gibi zengin sonuç alanları yok (X10).

**D15 — `AutoDealer` yapısal verisinin alanları.** (R6, D1) `@context`, `@type: AutoDealer`, `name`
(görünen ad), `url` (ana sayfa kanoniği). Vitrinde görünen şube varsa `department` dizisi: her
öğe `@type: AutoDealer`, `name`, `address` (şubenin o dildeki adresi, metin olarak) ve `telephone`.
Görünen ad okunamazsa blok hiç basılmaz. Yalnız ana sayfada.
*Gerekçe:* bugün vitrinde görünen bilgi bu: altlık şubenin adını, adresini ve telefonunu
gösteriyor (`0014`). Adresin yapılandırılmış hâli (`PostalAddress` alanları) veride yok, metin
olarak saklanıyor.
*Bedel:* `address`'in yapısız metin olması zengin sonuç kalitesini düşürür. *Not (sahip kararı,
2026-11-26):* `sameAs`, `openingHoursSpecification` ve `logo` sahibin istediği ayrı kartlarla
gelecek (X4, X9). Bu kararın kapalı listesi o kartların **kapsamında** genişler; **AC-52**'nin
"bu anahtarlar yoktur" iddiası 0004'ün sınırını ölçer ve o kartlar onu kendi kriterleriyle değiştirir.

**D16 — Yapısal veri ve etiketlerdeki serbest metin kodlanır.** (C9) JSON-LD içinde `<`, `>`, `&`
kaçırılır. Hiçbir değer `</script>`'i kapatamaz. Özniteliklerde bugünkü öznitelik kodlaması
kullanılır.
*Gerekçe:* tuzak 3. Model adını panelde Yönetici giriyor (`0021`), sayfa başlığını İçerik Editörü
(`0017`).
*Bedel:* yok.

**D17 — Performans bütçesi.** (R11, C12)
(a) Detay belgesinde bu kartın eklediği etiketlerin (başlık, açıklama, kanonik, alternatifler,
Open Graph, JSON-LD) toplam boyutu, sıkıştırmasız, **en çok 4096 bayt**tır. Ölçüm *(CLARIFY
B12)* kurgulanmış en kötü durumda, **ar** belgesinde yapılır: alan sınırındaki uzunlukta model
adı, ilanın görsel üst sınırı kadar görsel, en uzun şube adı.
(b) ADR 0013'ün protokolüyle (giriş JS + giriş CSS, `gzip -9`) ilk paket `main` tabanına göre
**en çok 512 B** büyür.
(c) C3 ana sayfa, liste ve detayda yeşil kalır. Bir sayfanın medyanı **2450 ms**'yi aşarsa ikinci
bir koşu istenir ve LCP öğesi PR'a yazılır.
*Gerekçe:* detayın payı 89 ms ve gürültü yaklaşık 80 ms. Bayt sınırı ölçülebilir bir tavan
koyuyor. Yaklaşık 4 kB sıkıştırılmadan bile tek bir TCP penceresinin içinde kalır. Paket sınırı,
metinlerin vitrin paketine kaçmasını yakalar (**D12**). `0018` **D17**'nin deseni.
*Bedel:* `image` dizisi çok görselli ilanda bütçeyi zorlayabilir. Bütçe aşılırsa `image`'in ilk N
görselle sınırlanması önerilir (karar uygulamada, ölçümle).

**D18 — Rezerve ilan indekslenir. `availability`: Yayında → `InStock`, Rezerve → `Reserved`.**
(R5) Satılmış ve arşivlenmiş ilan görünmez, yani 404'tür (**D7**).
*Gerekçe:* **R6** (domain) rezerve aracı vitrinde rozetle gösteriyor. Görünen sayfa indekslenir.
`https://schema.org/Reserved`, `ItemAvailability`'nin bu durum için tanımlı değeri.
*Bedel:* rezerve araç arama sonucunda "rezerve" olarak görünür ve bazı ziyaretçi tıklamaz.

**D19 — Belge önbelleği değişmez (`no-store`). Sitemap ve robots.txt için ayrı bir önbellek
kuralı konmaz.** (C11)
*Gerekçe:* `0022` **D20**: belge her istekte veritabanından okunan değerler taşıyor. Sitemap'in
bayat kopyası kaldırılmış ilanı geri getirirdi.
*Bedel:* yok.

**D20 — Dil kodu olduğu gibi yazılır (`tr`, `ar`, `en`). Bölge eki eklenmez.** (R3)
*Gerekçe:* kurulum bölge bilmiyor (X13). Dil kodu tek başına geçerli bir `hreflang` değeri.
*Bedel:* aynı dili iki bölgeye ayrı sunmak mümkün değil. Bugün bir ihtiyaç da yok.

**D21 — Kabul kriterlerini Analist yazar. Kriterler uygulama dalı açılmadan `main`'e girer.**
Bu PR'ın kendisi. (`0017` **D28**'in deseni, ADR 0008 / K2.)

### Verilen kararlar — QA CLARIFY turu, 2026-11-26

QA 1, PR #125'i `c92c54c` üzerinde okudu: **13 bulgu — 0 engelleyici, 8 önemli, 5 küçük.** Hepsi
işlendi (Takım Yöneticisi (ajan) kararı, 2026-11-26).

| Bulgu | Öz | İşlendiği yer |
|---|---|---|
| **B1** | Kriterlerin ölçüm aracı yok: `Contains` ile "öğe oluşmaz" ölçülemez, XSD yok, "şema doğrulaması" tanımsız | **D22**, C13, AC-16, AC-46, AC-55, AC-56 |
| **B2** | Tamamlanmış kartların üç testi ve ön yükleme konumu değişiyor, yazılmamış | D7 Bedel, AC-80, DoD T11 |
| **B3** | Uç üçüncü parçayı yok sayıyor (`route.ts:205-209`); `/tr/cars/{slug}/x`, `/tr/admin`, `/tr/newsletter…` için durum kodu ile ekran çelişir | AC-76, AC-90 |
| **B4** | "Listenin okuduğu parametre" kümesi tanımsız; `sort` ve `page=1` belirsiz; alternatif ve `og:url` sayılmamış | **D23**, AC-68 |
| **B5** | Kök adres doğrulaması `/galeri`'yi kabul ediyor; yol bölümü ve sondaki `/` tanımsız | D2, AC-22, AC-86 |
| **B6** | Ekranda zaten olan metinler ikinci bir kümede çoğalıyor | D12, AC-3, AC-8 |
| **B7** | 404, bülten ve panel belgelerinin başlığı tanımsız; `CarGallery` o belgelerde kalabilir | D11, AC-15, AC-91, AC-92 |
| **B8** | AC-46'nın "ikinci `<script>` oluşmaz"ı gerçek pakette yanlış (modül script'i zaten var) | AC-46 |
| **B9** | AC-7'nin sayı biçimi örnekle tanımlı, kaynağa bağlı değil | AC-7 |
| **B10** | "Yayındaki" içerik sayfası `FindBySlugAsync`'le aynı şey değil (girdisiz SSS) | D4, AC-56, AC-89, AC-88 |
| **B11** | AC-26'da `x-default` eksik | AC-26 |
| **B12** | AC-83'ün ölçüm ilanı tohuma bağlı, en kötü durum değil | D17 (a), AC-83 |
| **B13** | 404 kararının gizli ilanda ek okuma yapmadığının kanıtı yok (`0002` **C13**/**E7**) | AC-87 |

**D22 — Ölçüm araçları.** (B1, C13)
(a) Belge kriterleri bir HTML ayrıştırıcısıyla ölçülür: **AngleSharp yalnız `Host.Tests`'e**
eklenir, üretim koduna girmez.
(b) Sitemap için XSD kopyalanmaz. AC-55/AC-56 **yapısal iddiadır**: belge `System.Xml` ile iyi
biçimli ayrıştırılır, kök öğenin adı (`sitemapindex` / `urlset`) ve ad alanı
(`http://www.sitemaps.org/schemas/sitemap/0.9`) doğrudur, yalnız izinli alt öğeler vardır
(`<sitemap><loc>` / `<url><loc>`).
(c) Yapısal veri "şema doğrulaması" değil, **D14**/**D15**'in kapalı anahtar listesine karşı
**anahtar kümesi karşılaştırmasıdır**.
*Gerekçe:* `Contains` bir dizenin varlığını ölçer, bir öğenin oluşup oluşmadığını ölçemez
(AC-16, AC-46). XSD kopyası bakımı olan bir ikinci kaynak. Kapalı liste zaten şemanın işini görüyor.
*Bedel:* bir test bağımlılığı.

**D23 — Listenin okuduğu parametreler belgeyi `noindex, follow` yapar.** (B4, R9, D8) Küme
`frontend/src/features/catalog/listUrlState.ts:53-69`'dakidir: `bodyType`, `fuel`, `transmission`,
`brand`, `yearMin`, `yearMax`, `mileageMin`, `mileageMax`, `priceMin`, `priceMax`, `sort`, `page`.
**`sort` ve `page=1` dâhil** — değerine bakılmaz. Bu belgede kanonik, dil alternatifi ve `og:url`
basılmaz. Kümede olmayan parametreler (`utm_source`) yok sayılır: belge indekslenir ve kanoniği
parametresiz adrestir (AC-69).
*Gerekçe:* `page=1` ve varsayılan `sort` parametresiz listenin kopyasıdır. Değere göre ayrım yapmak
listenin varsayılanlarını Host'a kopyalamak olurdu (C3). Kanoniği olmayan bir belgenin alternatifleri
ve `og:url`'i de tutarsız sinyal verir.
*Bedel:* küme ön yüzde yaşıyor; yeni bir liste ekseni eklendiğinde Host'un kümesi de güncellenmeli.
Kriter (AC-68) bunu yakalamaz, uygulama ikisini tek kaynaktan türetmeli.

### Verilen kararlar — plan turu, 2026-11-26 (P1–P10)

Developer'ın uygulama kararları (Proje Yöneticisi görevlendirmesi, 2026-11-26). R/C/AC'ye
dokunmaz; her biri spec'in Developer'a bıraktığı bir noktayı kapatır. Ek iki onay görevlendirmeden:
404 başlığı tek metin `content.notFound.title`; ön yükleme bağlantısı SEO etiketlerinden **önce**.

**P1 — Kök adresin anahtarı değişmez: `Notifications:PublicBaseUrl`. Doğrulaması ve sahipliği
Host'a geçer.** (D2) Host başlangıçta değeri **D2**'nin kuralıyla doğrular (yalnız `http`/`https`,
yol yok, sondaki `/` normalize) ve hata mesajı anahtarın adını söyler (AC-22). Normalize edilmiş
değer hem SEO'ya hem `Notifications`'a gider. *Gerekçe:* anahtarı yeniden adlandırmak `dev-up.sh`,
iki CI iş akışını, dağıtım ortam değişkenlerini ve `BildirimYapilandirmasiTests`'in iki satırını
değiştirirdi; tek değer kuralı (D2) adla değil sahiplikle sağlanır. `docs/deploy.md` satırı
"sitenin kökü" olarak yeniden yazılır. *Bedel:* ad yanıltıcı kalır; yeniden adlandırma ayrı bir öneri.

**P2 — Sabit metinler Host'a dosya olarak gider: vitrin kataloğu + yeni SEO kümesi.** (D12) SEO
kümesi `frontend/src/shared/i18n/locales/seo/{tr,ar,en}.json` (`seo.*` anahtarları); hiçbir
ön yüz modülü onu içe aktarmaz, yani vitrin paketine girmez (D17 b). `Host.csproj` vitrin ve SEO
katalog dosyalarını çıktıya `i18n/` altında bağlar; `Dockerfile`'ın arka yüz aşaması o dizini
kopyalar. Host başlangıçta okur; kullandığı bir anahtar herhangi bir dilde eksikse **başlamaz**.
`check:locales` SEO kümesini de ölçer (dil eşitliği, kümeler arası tekrar yok).

**P3 — Listenin okuduğu parametreler tek kaynakta: `frontend/src/features/catalog/listQueryParams.json`.**
(D23) `listUrlState.ts` adları oradan alır, Host aynı dosyayı P2'nin yoluyla okur. *Gerekçe:* D23'ün
bedeli ("uygulama ikisini tek kaynaktan türetmeli").

**P4 — Sayı ve para biçimi `BuildingBlocks`'a taşınır.** (AC-7, AC-9) `NewsletterFormat`
(`0018` D24/D32, `Notifications` içinde internal) `BuildingBlocks/Formatting/ShowcaseFormat`
olur; `Notifications` ve Host aynı kodu kullanır, Host testi aynı fikstürü ölçer. *Gerekçe:* Host
başka modülün internal tipine erişemez (C4); kopya iki kaynak olurdu.

**P5 — Detay belgesi iki okuma yapar: `FindFirstImageAsync` (ön yükleme, değişmez) ve `FindAsync`
(başlık, açıklama, yapısal veri ve 404 kararı).** (D7, D17, AC-87) Gizli ve olmayan ilan ikisinde de
aynı yoldan `null` döner (`ShowcaseListingDetailQueries`, E7). `FindAsync` hata verirse belge
bugünkü gibi 200 ile basılır, detaya özgü etiketler basılmaz ve hata loglanır (`0020` R5a deseni).
TTFB etkisi C3 ile ölçülür; eşik tutmazsa önerilen tek iyileştirme hafif bir okuma sözleşmesidir.

**P6 — 404 kararı ve etiketler tek yerde: `SeoDocument` (Host).** Kabuk çizici belgeyi yazmadan
önce sayfa türünü, durum kodunu ve etiketleri ondan alır; ara katman yalnız yönlendirmeleri yapar.

**P7 — Ön yüzde tek bir "bulunamadı" rotası.** (D8, AC-76) `parseRoute` iki ve daha çok parçalı
tanınmayan yolu, `/{l}/cars/{slug}/…`'yi ve `/{l}/newsletter/…`'nün confirm/unsubscribe dışını
`not-found` rotasına çevirir; ekran `content.notFound.*` metinli `EmptyState`'tir (404 başlığıyla
aynı metin, C6).

**P8 — `<title>` yalnız Host'tan.** `frontend/index.html`'deki `<title>CarGallery</title>` kaldırılır
(D11, AC-15); Host her belgeye tam bir `<title>` yazar.

**P9 — Sitemap ve robots.txt Host'un uç noktaları** (`/sitemap.xml`, `/sitemap-{locale}.xml`,
`/robots.txt`). Veri yeni üç sözleşmeden gelir: `VehicleCatalog` görünür ilan slug'ları,
`Content` bir dilde görünen sayfa slug'ları (`FindBySlugAsync`'in kuralıyla, girdisiz SSS dâhil),
`Dealership` galerinin public okuması (bugün internal olan `IPublicGalleryQueries` sözleşmeye taşınır).

**P10 — Bütçe aşılırsa `image` dizisi sınırlanır** (D17 Bedel). Ölçüm AC-83'ün kurgusuyla; sonuç PR'da.

### Uygulama planı (2026-11-26)

1. Plan ve kararlar (bu bölüm).
2. Kök adres: `SiteRoot` + başlangıç doğrulaması — AC-22, AC-86 (P1).
3. Sözleşmeler: `Dealership` public galeri, `VehicleCatalog` görünür slug'lar, `Content` görünen
   sayfalar — modül başına commit (P9).
4. Biçim `BuildingBlocks`'a (P4) — AC-7, AC-9'un dayanağı.
5. SEO kümesi, liste parametrelerinin tek kaynağı, Host'un katalog okuması, `check:locales` (P2, P3).
6. Belge etiketleri ve 404 kararı (P5, P6, P8) — AC-1–54, AC-67–82, AC-86–88, AC-90–92.
7. Sitemap ve robots.txt (P9) — AC-55–66, AC-89.
8. Ön yüz "bulunamadı" rotası (P7), e2e ve görsel kanıt — AC-76, DoD T2.
9. Belgeler: `docs/architecture.md` § 3 ve Karar Defteri, `docs/deploy.md` — DoD T8.
10. Ölçüm: AC-83, AC-84, AC-85 (C3); PR.

---

## Sahibe sorular

Bu sorular **ürün ve iş kararıdır.** Takım Yöneticisi bunları karara bağlamaz, kabul kriterleri
de onlara dayanmaz. Cevapları kapsam dışı maddelerin ne zaman açılacağını ve canlıya çıkışı
belirler.

> **Sahibin cevapları (sahip kararı, 2026-11-26):** **SS4** — şimdilik **hayır**, D1 kalır.
> **SS2**, **SS3**, **SS5** — **üçü de isteniyor**: her biri panelden giriş + yapısal veri olarak
> **0004'ten sonra ayrı küçük kartlar**. 0004'ün kapsamı değişmez (X4, X9). **SS1** barındırma
> kararıyla birlikte açık; kod D2'nin yapılandırma değerini bekler.

- **SS1. Canlı alan adı.** Kanonik, sitemap ve paylaşım önizlemesi bu adrese bağlı (**D2**).
  Barındırma ve alan adı henüz seçilmedi (ADR 0012, "Canlıya çıkış engelleri"). Kriterler yapılandırılan
  kökle doğrulanır. Asıl alan adı canlıya çıkışın şartıdır.
- **SS2. Sosyal hesaplar.** Galeri hangi platformlarda (Instagram, Facebook, YouTube…) hesap
  gösterilmesini istiyor? Bunlar yapısal veride `sameAs` olarak ve vitrinde bağlantı olarak
  görünebilir. Bugün girilebildiği bir yüzey yok (`0020` **X2**) → X4.
- **SS3. Çalışma saatleri.** Şubelerin çalışma saatleri vitrinde gösterilecek mi, ve verisini kim,
  hangi sıklıkta girecek (bayram ve tatil istisnaları dâhil)? `0003` **X2** bu kartı ayırmıştı.
  `openingHoursSpecification` buna bağlı → X4.
- **SS4. Şube başına sayfa.** Yerel aramada her şubenin ayrı görünmesi (ör. "Kadıköy galerisi")
  bir hedef mi? Hedefse şube sayfası bir ürün yüzeyi olarak ayrı bir kart olur → X3, **D1**.
- **SS5. Logo.** Paylaşım önizlemesinde ve galerinin yapısal verisinde logo isteniyor mu?
  `0014` **X9** ve `0020` **X1** logoyu ayrı bir karta bırakmıştı → X9.

---

## Acceptance Criteria

> Belge kriterleri sunucu yanıtının kendisiyle doğrulanır: belge JavaScript çalıştırılmadan
> ayrıştırılır (C13). "Kök" **D2**'nin yapılandırılmış değeridir. "Görünen ad" galerinin o
> dildeki görünen adıdır (`0020`). Kriterler aksi yazılmadıkça varsayılan kurulumda (tr/ar/en,
> TRY) koşar.

### Başlık ve açıklama (R1 · C2, C9 · D11, D12)

- **AC-1.** `/tr/` belgesinde tam olarak bir `<title>` vardır ve metni galerinin tr görünen adıdır.
- **AC-2.** Ana sayfa metinlerinde giriş açıklaması ar için kaydedilmişken `/ar/` belgesinin
  `<meta name="description">` değeri o kayıtlı metindir.
- **AC-3.** Giriş açıklaması en için kaydedilmemişken `/en/` belgesinin açıklaması en vitrin
  kataloğundaki `home.intro.body` anahtarının metnidir.
- **AC-4.** `/tr/cars` belgesinin başlığı `{katalogdaki tr liste başlığı} · {görünen ad}`'dır ve
  açıklaması katalogdaki tr liste açıklamasıdır.
- **AC-5.** Görünür bir ilanın `/tr/cars/{slug}` belgesinin başlığı `{tr model adı} {yıl} · {görünen ad}`'dır.
- **AC-6.** Modelin ar karşılığı kayıtlıyken `/ar/cars/{slug}` belgesinin başlığı ar model adını
  taşır. Karşılık kayıtlı değilse kanonik Latin adı taşır.
- **AC-7.** Detay açıklaması yılı, kilometreyi, fiyatı ve şube adını taşır. Kilometre ve fiyatın
  yazımı, üç dilde de `frontend/src/shared/i18n/format.fixtures.json`'daki beklenen değerlerle
  aynıdır (`0018` **D24**/**D32**'nin fikstürü).
- **AC-8.** "Fiyat sorunuz" ilanının detay açıklaması vitrin kataloğundaki `detail.priceOnRequest` metnini taşır ve
  hiçbir fiyat rakamı (`0` dâhil) içermez.
- **AC-9.** Para birimi KWD (minor unit 3) olan bir kurulumda `1250.500` KWD'lik ilanın detay
  açıklaması fiyatı üç ondalıkla yazar.
- **AC-10.** Yayındaki bir içerik sayfasının `/tr/{slug}` belgesinin başlığı `{sayfa başlığı} · {görünen ad}`'dır.
- **AC-11.** Gövdesi `## Başlık`, `**kalın**` ve `[bağlantı](https://ornek.test)` ile başlayan bir
  içerik sayfasının açıklamasında `#`, `**`, `[`, `](` dizileri yoktur, bağlantının metni vardır.
- **AC-12.** 400 karakterlik gövdeli bir içerik sayfasının açıklaması en çok 160 karakterdir ve
  gövdedeki bir kelimenin ortasında bitmez.
- **AC-13.** Görünen ad sorgusu hata verdiğinde `/tr/cars` belgesi 200 döner ve başlığı yalnız
  katalogdaki liste başlığıdır (` · ` eki yoktur).
- **AC-14.** Görünen ad sorgusu hata verdiğinde `/tr/` belgesinin başlığı katalogdaki tr ana sayfa
  başlığıdır.
- **AC-15.** Ana sayfa, liste, detay, içerik, 404, bülten teyit/çıkış ve panel belgelerinin
  hiçbirinin `<title>`'ında ya da `og:site_name`'inde `CarGallery` dizesi geçmez (görünen ad
  okunamadığı durum dâhil). Hiçbir belgede birden çok `<title>` yoktur.
- **AC-16.** Görünen adı `A <b>&"B"` olan galerinin `/tr/cars` belgesi ayrıştırıldığında
  `<title>`'ın metin değeri `{liste başlığı} · A <b>&"B"`'dir ve belgede `<b>` öğesi oluşmaz.

### Kanonik adres (R2 · C10 · D2, D9)

- **AC-17.** Ana sayfa, liste, görünür ilan detayı ve yayındaki içerik sayfası belgelerinin her
  biri tam olarak bir `<link rel="canonical">` taşır ve `href`'i sırasıyla `{kök}/tr/`,
  `{kök}/tr/cars`, `{kök}/tr/cars/{slug}`, `{kök}/tr/{slug}`'dır.
- **AC-18.** `/ar/cars/{slug}` belgesinin kanoniği `{kök}/ar/cars/{slug}`'dır.
- **AC-19.** `/tr/cars/{slug}?utm_source=x` belgesinin kanoniği sorgu dizesi taşımaz.
- **AC-20.** `/tr` ve `/tr/` isteklerinin belgeleri aynı kanoniği (`{kök}/tr/`) taşır.
- **AC-21.** `Host: kotu.test` ve `X-Forwarded-Host: kotu.test` başlıklarıyla yapılan istekte
  kanonik, dil alternatifleri ve `og:url` yine `{kök}` ile başlar ve `kotu.test` belgede geçmez.
- **AC-22.** Kök adres yapılandırması boş, `/galeri`, `galeri.com`, `ftp://x.test` ya da
  `https://x.test/yol` iken uygulama başlamaz ve hata anahtarın adını söyler.
- **AC-86.** Kök `https://x.test/` ve `https://x.test` olarak yapılandırılmış iki kurulumda
  `/tr/cars` belgesinin kanoniği aynıdır: `https://x.test/tr/cars`.

### Dil alternatifleri (R3 · C7 · D10, D20)

- **AC-23.** Ana sayfa, liste ve detay belgelerinin her biri, etkin her dil için bir
  `<link rel="alternate" hreflang="{dil}">` taşır. `href`'i o dilin mutlak adresidir ve küme
  belgenin kendi dilini de içerir.
- **AC-24.** Ana sayfa, liste ve detay belgelerinin `hreflang="x-default"` alternatifi varsayılan
  dilin (tr) aynı sayfasıdır.
- **AC-25.** Etkin diller `tr, en` iken `/tr/cars/{slug}` belgesinde `hreflang="ar"` alternatifi yoktur.
- **AC-26.** tr'de `kvkk`, en'de `privacy` slug'ıyla yayında, ar'da yayında olmayan bir içerik
  sayfasının `/tr/kvkk` belgesi tam olarak iki dil alternatifi (`{kök}/tr/kvkk`, `{kök}/en/privacy`)
  ve `x-default` = `{kök}/tr/kvkk` taşır.
- **AC-27.** Varsayılan dil tr'de yayında olmayan, en'de yayında olan bir içerik sayfasının
  `/en/{slug}` belgesi `x-default` alternatifi taşımaz.
- **AC-28.** `hreflang` değerleri yalnız dil kodudur (`tr`, `ar`, `en`, `x-default`). Bölge eki yoktur.

### Paylaşım önizlemesi (R4 · D13)

- **AC-29.** İndekslenebilir her belge türünde `og:title` başlıkla, `og:description` açıklamayla
  aynı metni taşır.
- **AC-30.** İndekslenebilir her belge türünde `og:url` kanonikle aynıdır ve `og:type` `website`'tır.
- **AC-31.** Görselli bir ilanın detay belgesindeki `og:image`, ilk görselin büyük türevinin mutlak
  adresidir. Medya adresi göreliyse `{kök}` ile başlar.
- **AC-32.** Görselsiz bir ilanın detay belgesinde `og:image` yoktur.
- **AC-33.** Ana sayfa, liste ve içerik belgelerinde `og:image` yoktur.
- **AC-34.** `og:site_name` görünen addır. Görünen ad okunamadığında `og:site_name` basılmaz.
- **AC-35.** Hiçbir belgede `og:locale` yoktur.

### Araç yapısal verisi — `Car` + `Offer` (R5 · C5, C8, C9 · D14, D16, D18)

- **AC-36.** Görünür bir ilanın detay belgesi tam olarak bir `<script type="application/ld+json">`
  taşır. İçeriği geçerli JSON'dur ve `@type`'ı `Car`'dır.
- **AC-37.** `Car` nesnesinin anahtarları **D14**'ün listesinin alt kümesidir (`@context`, `@type`,
  `name`, `vehicleModelDate`, `mileageFromOdometer`, `image`, `url`, `offers`). `offers`'ın anahtarları
  da `@type`, `price`, `priceCurrency`, `availability`, `url`'nin alt kümesidir. Başka anahtar yoktur.
- **AC-38.** `vehicleModelDate` ilanın yılıdır. `mileageFromOdometer`, `value`'su ilanın kilometresi
  ve `unitCode`'u `KMT` olan bir `QuantitativeValue`'dur.
- **AC-39.** `image`, ilanın görsellerinin büyük türevlerinin mutlak adresleridir, ilandaki sırayla.
  `url` kanoniktir.
- **AC-40.** TRY kurulumunda `1.250.000,00` TL'lik ilanın `offers.price`'ı `"1250000.00"`, `priceCurrency`'si `TRY`'dir.
- **AC-41.** KWD (minor unit 3) kurulumunda `1250.5` KWD'lik ilanın `offers.price`'ı `"1250.500"`, `priceCurrency`'si `KWD`'dir.
- **AC-42.** `/ar/cars/{slug}` belgesindeki `offers.price` Latin rakam ve nokta ondalık taşır.
  Arapça rakam ya da binlik ayracı yoktur.
- **AC-43.** Yayındaki ilanın `availability`'si `https://schema.org/InStock`, rezerve ilanınki
  `https://schema.org/Reserved`'dır.
- **AC-44.** "Fiyat sorunuz" ilanının `Car` nesnesinde `offers` anahtarı yoktur.
- **AC-45.** `/ar/cars/{slug}` belgesindeki `Car.name`, AC-6'daki ar model adıdır.
- **AC-46.** Model adı `X</script><script>alert(1)</script>` olan ilanın detay belgesi
  ayrıştırıldığında tek bir JSON-LD bloğu vardır, içeriği geçerli JSON'dur ve `name`'i bu dizedir.
  Belgedeki `<script>` öğesi sayısı, aynı belgenin bu kartın etiketleri olmadan basılmış hâlinden
  tam bir fazladır ve `alert(1)` hiçbir `<script>` öğesinin metni değildir.
- **AC-47.** Kurulumda gizlenmiş bir opsiyonel alanın (**F3**) değeri, detay belgesinin hiçbir
  etiketinde ve JSON-LD'sinde geçmez.
- **AC-48.** Detay belgesi dışındaki belgelerde `@type: Car` taşıyan JSON-LD yoktur.

### Galeri yapısal verisi — `AutoDealer` (R6 · D1, D15, D16)

- **AC-49.** `/tr/` belgesi tam olarak bir JSON-LD bloğu taşır. `@type`'ı `AutoDealer`, `name`'i tr
  görünen ad, `url`'i `{kök}/tr/`'dir.
- **AC-50.** Vitrinde görünen iki şubesi olan kurulumda `department` iki öğe taşır. Her öğe
  `@type: AutoDealer`, şubenin tr adı, tr adresi ve telefonudur.
- **AC-51.** Kapatılmış bir şube `department`'ta yer almaz.
- **AC-52.** `AutoDealer` bloğunda `openingHoursSpecification`, `geo`, `sameAs` ve `logo` anahtarları yoktur.
- **AC-53.** Görünen ad okunamadığında `/tr/` belgesi 200 döner ve `AutoDealer` bloğu taşımaz.
- **AC-54.** Liste, detay ve içerik belgelerinde `@type: AutoDealer` taşıyan JSON-LD yoktur.

### Sitemap (R7 · D3, D4, D19)

- **AC-55.** `/sitemap.xml` 200 döner, `Content-Type`'ı XML'dir ve iyi biçimli XML'dir. Kök öğesi
  `http://www.sitemaps.org/schemas/sitemap/0.9` ad alanında `sitemapindex`'tir, alt öğeleri yalnız
  `<sitemap><loc>`'tur. Etkin her dil için tam olarak bir `{kök}/sitemap-{dil}.xml` girdisi taşır.
- **AC-56.** `/sitemap-tr.xml` iyi biçimli XML'dir. Kök öğesi aynı ad alanında `urlset`'tir, alt
  öğeleri yalnız `<url><loc>`'tur. Adres kümesi tam olarak şudur: `{kök}/tr/`, `{kök}/tr/cars`,
  görünür her ilanın `{kök}/tr/cars/{slug}`'ı ve tr'de görünen (`FindBySlugAsync`'in döndüreceği)
  her içerik sayfasının `{kök}/tr/{slug}`'ı.
- **AC-57.** Bir ilan yayından kaldırıldıktan sonraki ilk `/sitemap-tr.xml` yanıtında o ilanın adresi yoktur.
- **AC-58.** Bir ilan satıldı olarak işaretlendikten sonraki ilk `/sitemap-tr.xml` yanıtında o
  ilanın adresi yoktur.
- **AC-59.** Rezerve bir ilanın adresi `/sitemap-tr.xml`'de vardır.
- **AC-60.** Uygulama açılmadan önce veritabanında yayında olan bir ilan, uygulamanın ilk
  `/sitemap-tr.xml` yanıtında vardır.
- **AC-61.** ar'da yayında olmayan bir içerik sayfası `/sitemap-ar.xml`'de yoktur. tr'de yayındaysa
  `/sitemap-tr.xml`'de tr slug'ıyla vardır.
- **AC-62.** Hiçbir sitemap girdisi `/admin`, `/newsletter/` ya da `?` içermez.
- **AC-63.** Etkin olmayan bir dilin sitemap'i (`/sitemap-de.xml`) 404 döner.
- **AC-64.** Hiçbir sitemap girdisi `lastmod` öğesi taşımaz.
- **AC-89.** tr'de yayına alınmış ama hiç girdisi olmayan bir SSS sayfası `/sitemap-tr.xml`'de yoktur.

### robots.txt (R8 · D5)

- **AC-65.** `/robots.txt` 200 döner, `Content-Type`'ı `text/plain`'dir ve `Sitemap: {kök}/sitemap.xml`
  satırını taşır.
- **AC-66.** robots.txt `/api/` için `Disallow` taşır. `/admin` ya da `/newsletter` için `Disallow`
  taşımaz. Başka bir yol kapatılmaz.

### İndekslenmeme (R9 · D6, D8)

- **AC-67.** `/admin`, `/admin/vehicles` ve `/admin/content/home` belgelerinin her biri
  `<meta name="robots" content="noindex">` taşır.
- **AC-92.** `/admin` ve `/tr/newsletter/confirm` belgelerinin `<title>`'ı görünen addır. Görünen ad
  okunamadığında katalogdaki yedek başlıktır.
- **AC-68.** `/tr/cars?bodyType=suv`, `/tr/cars?page=2`, `/tr/cars?page=1`, `/tr/cars?sort=newest`
  ve `/tr/cars?fuel=diesel` belgelerinin her biri `<meta name="robots" content="noindex, follow">`
  taşır. Kanonik, dil alternatifi ve `og:url` taşımaz (**D23**).
- **AC-69.** `/tr/cars?utm_source=x` belgesi robots etiketi taşımaz ve kanoniği `{kök}/tr/cars`'tır.
- **AC-70.** `/tr/newsletter/confirm?token=…` belgesi bugünkü `noindex` etiketini ve
  `Referrer-Policy: no-referrer` başlığını taşır. Kanonik, dil alternatifi ve JSON-LD taşımaz.
- **AC-71.** Ana sayfa, parametresiz liste, görünür ilan detayı ve yayındaki içerik sayfası
  belgelerinde robots etiketi yoktur.

### Durum kodu (R10 · C6 · D7, D8)

- **AC-72.** Hiç var olmamış bir slug'ın `/tr/cars/{slug}` isteği 404 döner. Gövde kabuktur ve uç
  "bulunamadı" ekranını basar.
- **AC-73.** Yayından kaldırılmış, satılmış ve arşivlenmiş bir ilanın `/tr/cars/{slug}` istekleri
  404 döner. Gövdeleri, hiç var olmamış bir slug'ın gövdesiyle bayt bayt aynıdır.
- **AC-87.** Detay belgesinin 404 kararı, vitrin okuma modelinin yolundan (`FindAsync` /
  `FindFirstImageAsync`) verilir: gizli bir ilanın isteği, hiç var olmamış bir slug'ın isteğinden
  fazla okuma yapmaz (`0002` **C13**/**E7**). Kanıt bir mimari test ya da sahte port testidir.
- **AC-74.** ar'da yayında olmayan bir içerik sayfasının ar slug'ıyla yapılan istek ve hiç var
  olmamış bir tek parçalı yol (`/tr/yok-boyle-sayfa`) 404 döner.
- **AC-75.** Eski bir içerik slug'ının isteği bugünkü gibi güncel adrese 301 döner (`0017` **D13**).
- **AC-76.** `/tr/foo/bar`, `/tr/cars/{görünür slug}/x`, `/tr/admin`, `/tr/newsletter` ve
  `/tr/newsletter/foo` istekleri 404 döner ve uç liste ya da detay değil "bulunamadı" ekranını basar
  (tr ve ar için görsel kanıt).
- **AC-90.** `/tr/newsletter/confirm` ve `/tr/newsletter/unsubscribe` belgeleri 200 döner ve
  `noindex` taşır (AC-70).
- **AC-77.** 404 belgeleri `<meta name="robots" content="noindex">` taşır. Kanonik, dil alternatifi,
  `og:` etiketi ve JSON-LD taşımaz.
- **AC-78.** Etkin olmayan önekin (`/de/`) isteği bugünkü gibi boş gövdeli 404 döner.
- **AC-91.** Hiç var olmamış bir ilanın, gizli bir ilanın ve hiç var olmamış bir içerik sayfasının
  404 belgelerinin `<title>`'ı aynıdır: `{tr content.notFound.title} · {görünen ad}`.
- **AC-88.** tr'de yayına alınmış ama hiç girdisi olmayan bir SSS sayfasının `/tr/{slug}` isteği 404 döner.
- **AC-79.** `/tr/`, `/tr/cars`, görünür bir ilanın detayı ve yayındaki bir içerik sayfası 200 döner.

### Korunan davranış (C11)

- **AC-80.** Görselli bir ilanın detay belgesindeki ön yükleme bağlantısı (`rel="preload"`, `href`,
  `imagesrcset`, `imagesizes`, `fetchpriority="high"`) `0016b`'nin kriterleriyle aynıdır ve belgede
  bu kartın eklediği tüm etiketlerden **önce** gelir.
- **AC-81.** `enabled-locales`, `gallery-display-name` ve `home-text-*` etiketleri bugünkü
  belgelerde bugünkü değerleriyle bulunur.
- **AC-82.** 200 ve 404 vitrin belgelerinin her biri `Cache-Control: no-store` taşır.

### Performans (R11 · C12 · D17)

- **AC-83.** Kurgulanmış en kötü durumdaki bir ilanın **ar** detay belgesinde — model adı alan
  sınırındaki uzunlukta, görsel sayısı ilanın üst sınırında, şube adı en uzun — bu kartın eklediği
  etiketlerin (`<title>`, açıklama, kanonik, dil alternatifleri, `og:*`, JSON-LD) sıkıştırmasız
  toplam boyutu en çok 4096 bayttır.
- **AC-84.** ADR 0013 protokolüyle ölçülen ilk paket (giriş JS + giriş CSS, `gzip -9`), `main`
  tabanına göre en çok 512 B büyür. İki sayı ve fark PR'da yazılır.
- **AC-85.** C3 ölçümünde ana sayfa, liste ve detayın medyan LCP'si 2500 ms'nin altındadır.
  Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci bir koşu yapılır ve iki koşunun LCP öğesi PR'da yazılır.

---

## Definition of Done

- [x] Önerilen kararlar (**D1–D21**) Takım Yöneticisi tarafından onaylandı ve "Verilen kararlar"
      olarak işaretlendi; QA CLARIFY turu (B1–B13 → **D22**, **D23** ve kriter düzeltmeleri)
      işlendi. *(Takım Yöneticisi (ajan) kararı, 2026-11-26.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok; Host yalnız `*.Contracts` kullanıyor (C3, C4).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil
      ve **D12**'nin SEO kümesini de kapsıyor. Ekrandaki metinler mevcut vitrin anahtarından okunuyor.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4), C3 dâhil.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0004-ac<n>.png`):
      AC-72 ve AC-76'nın "bulunamadı" ekranı, tr ve ar.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] <!-- DoD:T7 --> Yeni domain terimi yok (kanonik, sitemap, yapısal veri teknik terimlerdir).
- [ ] <!-- DoD:T8 --> `docs/architecture.md` § 3'ün sitemap cümlesi **D3**'e göre güncellendi ve
      § 7 Karar Defteri'ne işlendi. `docs/deploy.md` kök adres ayarı **D2**'ye göre güncellendi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
      Tamamlanmış kartların değişen davranışları açıkça yazıldı: `0017` **AC-38** (**D8**),
      `0017` **AC-41** ve **AC-51**'in `/en/kvkk` satırı (200 → 404, **D7**), `0016b`'nin ön
      yükleme konumu (**AC-80**).
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
