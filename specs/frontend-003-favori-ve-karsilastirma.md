# frontend-003 — Favori ve Karşılaştırma

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-26 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (QA 1, PR #138 @ `8ecf9ae`: 17 bulgu — 0 engelleyici / 9 önemli / 8 küçük; B1–B17 → § Verilen kararlar) |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin (favori düğmesi, sayaç, favoriler ve karşılaştırma ekranları, tembel katalog kümesi) · `Host` (iki yeni sayfa türü, belge etiketleri, `Referrer-Policy` — **D2**) · `Content.Contracts` (yalnız ayrılmış ad listesine `favorites` ve `compare` — **D1**) · `tests/VehicleCatalog.Tests` (alan adı kapısının okuduğu dosya ve "tek yerde" ölçüsü — **D11**) · `frontend/scripts/check-locale-keys.mjs` (üçüncü küme **D13**, anahtar varlığı kapısı **D19**) · `docs/` (sözlük **D16**, karar defteri, ADR `0013` eki ve `deploy.md` günlük notu **D17**). **Backend sözleşmesi değişmez** (**D14**; S5 koddan doğrulandı). |
| **Bağımlı spec'ler** | `0001` (tamamlandı; **X5**'i bu kart açar) · `0002` (tamamlandı; R7/R8 alan kümesi ve alan adı eşlemesi) · `0004` (tamamlandı; D7 gerçek 404, D8 tanınmayan yol, D23 liste parametreleri) · `0024` (tamamlandı; AC-55'in ilk paket tanımı) · `0026` (tamamlandı; üst çubuk yerleşimi) · `frontend-002` (token katmanı) |
| **Dayanak** | ADR 0003 Ek 1, sıra #12 · sahibin sırası (2026-11-26): son iş |
| **Karar bekliyor** | **Proje Yöneticisi:** **D18** — ilk paket bütçesi: B5 ölçümü favori düğmesi + sayacın kodunu ~1,2 KB gzip buluyor, bütçe +512 B (Context § Paket). D1–D17 ve D19 onaylandı. **Sahip:** yok — S1–S3, D5, D6 sahip kararı (2026-11-26). |

---

## Intent

Vitrinde gezen ziyaretçi beğendiği aracı **hiçbir yere koyamıyor.** `0001` favorileri
bilinçli olarak dışarıda bıraktı (§ X5) ve gerekçesi doğruydu: üyelik yoktu ve hâlâ yok.

Ama ihtiyaç üyelikten bağımsız: ziyaretçi dört aracı beğeniyor, aralarından birini seçmek
için ikisini yan yana koymak istiyor ve sayfalar arasında gezinirken hangilerini beğendiğini
**hatırlamak** istiyor. Bugün bunu tarayıcının sekmelerinde yapıyor — ya da yapmıyor ve
çıkıyor.

**Galeri** bunu istiyor çünkü karşılaştıran ziyaretçi karar veren ziyaretçidir; karar
verdiğinde talep bırakır.

Bu iş bittiğinde ziyaretçi bir aracı favorilere ekler, favorilerini bir arada görür ve
seçtiği araçları yan yana karşılaştırır — hiçbir hesap açmadan. Başarı ölçütü: **ziyaretçi
beğendiği araçları kaybetmeden sayfalar arasında gezinebiliyor ve en çok üç tanesini tek
ekranda karşılaştırabiliyor.**

---

## Requirements

- **R1.** Ziyaretçi, liste kartından, ana sayfanın öne çıkan kartlarından ve ilan detayından
  bir aracı **favorilere ekleyebilir** ve çıkarabilir.
- **R2.** Favori sayısı vitrinin her sayfasında **görünür** ve favoriler ekranına götürür.
- **R3.** Ziyaretçi favorilerini **bir arada** görebilir; her favorinin **bugünkü** bilgisi
  (fiyat, rezerve hâli) gösterilir, eklendiği anki değil.
- **R4.** Favoriler **ziyaretçinin kendi tarayıcısında** tutulur; sunucuya gönderilmez ve
  hiçbir yerde kişiyle ilişkilendirilmez. → `docs/architecture.md` § 6 · **S3**.
- **R5.** Favoriler ekranı, favorilerin **bu tarayıcıya ait** olduğunu ziyaretçiye sade ve
  kalıcı bir satırla söyler. → **S2**.
- **R6.** Ziyaretçi seçtiği araçları **yan yana karşılaştırabilir**; karşılaştırmaya en fazla
  **üç** araç alınır. Sınıra ulaşıldığında ziyaretçiye nedeni söylenir. → **S1**.
- **R7.** Karşılaştırma, araçların **aynı alanlarını** aynı satırlarda gösterir; bir araçta
  olmayan bilgi boş değil, **belirtilmiş** olur.
- **R8.** Karşılaştırmada gösterilen alan kümesi, ilan detayında gösterilen kümeyle **aynıdır**:
  kapalı veya gizli alan karşılaştırmada **hiç görünmez** ve bir alanın adı detayda nasıl
  yazıyorsa karşılaştırmada da öyle yazar. → `docs/architecture.md` § 4, Yasak Listesi 41 · **S4**.
- **R9.** Karşılaştırma görünümü **paylaşılabilir bir adreste** yaşar; bağlantıyı açan kişi,
  favorisi olmasa da aynı araçları aynı sırayla karşılaştırmalı görür. → **S5**.
- **R10.** Favorilerine eklenmiş bir araç **vitrinden düştüğünde** (satıldı, arşivlendi,
  yayından kaldırıldı) ziyaretçiye durum açıkça gösterilir; sessizce kaybolmaz ve ziyaretçi
  onu kendisi kaldırır.
- **R11.** Favoriler ve karşılaştırma **yükleniyor**, **boş** ve **hata** durumlarını ele
  alır. Boş durumlar ayrıştırılır: "hiç favori yok" ile "favorilerin hepsi vitrinden düştü"
  aynı ekran değildir. → `docs/frontend.md` § 3.
- **R12.** Favori düğmesi **klavyeyle** erişilebilir, durumu ekran okuyucuya bildirilir ve
  renk tek başına anlam taşımaz. → `docs/frontend.md` § 7.
- **R13.** Üç locale'de çalışır; Arapça'da RTL. Favoriler dilden bağımsızdır: bir dilde eklenen
  favori öbür dillerde de görünür.
- **R14.** Mobilde karşılaştırma okunabilir kalır; sayfa yatay kaydırmaya zorlanmaz.
  → `docs/frontend.md` § 1, § 7.
- **R15.** Tarayıcı veri saklamaya izin vermediğinde vitrin **çalışmaya devam eder**; favori
  özelliği çalışmıyormuş gibi görünen bir düğme bırakmaz.
- **R16.** Favoriler ve karşılaştırma ekranları **arama motoruna açılmaz** ve site haritasına
  girmez; ziyaretçinin kendi seçimidir, galerinin içeriği değildir.

---

## Constraints

### Uyulması zorunlu

- **C1.** **Public üyelik yoktur**; favori bir hesap özelliği değildir. → § 6.
- **C2.** Favori verisi sunucuya gönderilmez. `Engagement` modülü yoktur ve bu spec onu
  **var etmez** (sahip kararı, 2026-11-26 — **S3**).
- **C3.** Gizli ve kapalı alanlar hiçbir yerde görünmez; eleme **sorgu tarafında**
  uygulanır — karşılaştırma uçta bir filtre yapmaz. → Yasak Listesi 41.
- **C4.** Sunucu verisi global store'a kopyalanmaz; istemci durumu yerel tutulur.
  → `docs/frontend.md` § 7.
- **C5.** Paylaşılabilir durum **adreste** yaşar. → `docs/frontend.md` § 5.
- **C6.** Kullanıcıya görünen metin koda gömülmez; sayı içeren cümleler ICU ile kurulur
  ("3 favori"; Arapça'nın altı çoğul biçimi). → Altın Kural 6, Yasak Listesi 31.
- **C7.** CSS'te fiziksel yön özellikleri kullanılmaz; renk ve tipografi `frontend-002`
  token katmanından gelir; ham hex/px yoktur.
- **C8.** Bileşenler `docs/frontend.md` § 4.2 envanterinden kullanılır; yeni ortak bileşen
  gerekçesiyle önerilir.
- **C9.** Tarayıcı deposuna erişim **hata verebilir** (gizli pencere, site verisi kapalı, kota
  dolu); her okuma ve yazma bu durumu karşılamalı ve arayüz çalışmaya devam etmeli.
- **C10.** `0001`, `0002`, `0004`, `0014`, `0015`, `0017`, `0018` (bülten sayfalarında sayaç),
  `0020`, `0024`, `0025`, `0026`'nın mevcut kabul kriterleri **regresyon olarak** korunur.
  → ADR 0003 unsur 1.
- **C11.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.
- **C12.** Vitrinin ilk paketi `0024` AC-55'in tanımıyla ölçülür ve **D18**'in sınırını aşmaz; C3
  performans kapısı (LCP < 2500 ms, CLS < 0.1) ana sayfa, liste ve detayda yeşil kalır.
  → ADR `0013`, `docs/frontend.md` § 7.

### KAPSAM DIŞI

- **X1.** **Favorilerin cihazlar ve tarayıcılar arası eşitlenmesi** — *neden:* üyelik
  gerektirir ve § 6'da kapsam dışı.
- **X2.** **Favori aracı için fiyat düşüşü / satıldı bildirimi** — *neden:* sunucu kimin neyi
  favorilediğini bilmiyor (C2) ve `0018` § X3 bunu aynı gerekçeyle dışarıda bıraktı.
- **X3.** **Favori sayılarının sunucuda toplanması ve "en çok favorilenen araçlar" bilgisi** —
  *neden:* sahip kararı (2026-11-26, **S3**): favori verisi sunucuya **hiç** gitmez ve
  `Engagement` bu kartta doğmaz. `Engagement`'ı yalnız favori sayacı için açmak, ince bir
  modülü yarım bir sorumlulukla doğururdu; o modül görüntülenme ve tıklama sayaçlarıyla
  birlikte, kendi kayba toleranslı yazma modeliyle gelmeli. Favori toplamak ziyaretçi davranışı
  toplamaktır ve KVKK aydınlatmasını etkiler. *Bedel:* galeri hangi aracın çok beğenildiğini
  **göremez** — ticari değeri olan bir bilgi, bilerek feda edildi. `Engagement` geldiğinde
  favori düğmesine bir yayın eklemek ayrı ve küçük bir iştir.
- **X4.** **Karşılaştırmanın yazdırılması veya PDF'e aktarılması** — *neden:* ayrı bir
  biçimlendirme işi.
- **X5.** **Favori listesinin ziyaretçiye e-postayla gönderilmesi** — *neden:* e-posta adresi
  toplamak bir izin işidir (**L7**) ve `0018`'in konusu.
- **X6.** **Karşılaştırmada fiyat farkı hesabı, farkları vurgulama veya puanlama** — *neden:*
  öneri motoru § 6'da kapsam dışı; "hangisi daha iyi" galerinin değil ziyaretçinin kararı.
- **X7.** **İlan detayında ayrı bir "Karşılaştır" düğmesi ve ayrı bir karşılaştırma listesi** —
  *neden:* **D6**: karşılaştırma favorilerden seçilir; ikinci bir yerel liste ikinci bir
  kaybolma, sınır ve boş durum kümesi demek. Mockup'taki düğme bu yüzden yapılmaz.
- **X8.** **Altlıkta "Karşılaştırma" bağlantısı** — *neden:* altlık yalnız ana sayfada var
  (Context); favoriler ekranına üst çubuktaki sayaç her sayfadan götürür (**D8**).
- **X9.** **Favoriler ekranında sıralama, süzme veya sayfalama** — *neden:* en çok 24 favori
  (**D5**) tek ekrana sığar; sıra eklenme sırasıdır.
- **X10.** **Sekmeler arası canlı eşitleme** (bir sekmedeki değişikliğin öbür açık sekmeye
  yenilemeden yansıması) — *neden:* vitrin tam sayfa gezinir; bir sonraki sayfa açılışı güncel
  durumu okur. Yalnız yazma doğruluğu korunur (**D4**, AC-12).
- **X11.** **Okuma zamanlamasının gizlenmesi** — favoriler ekranı aynı ziyaretçiden aynı anda
  24'e kadar detay ve görsel okuması yapar; bu zamanlama ters vekil ya da CDN erişim günlüğünde
  favori kümesini çıkarılabilir kılar (**D15**). *Neden dışarıda:* tek istekli toplu okuma
  backend sözleşmesini büyütür (**D14**'ü bozar); okumalara yapay gecikme eklemek ekranı
  yavaşlatır. Karşılığı günlüğün saklama süresidir (`docs/deploy.md`, **D17**).

---

## Context

### Mevcut davranış — doğrulandı (`main` @ `0c819fe`)

**Sözleşme ve kimlik (S5)**
- **Liste kartı slug taşıyor.** `ShowcaseListingDto`'nun ikinci alanı `string Slug`'dır
  (`VehicleCatalog.Contracts/Showcase/ShowcaseListingContracts.cs:17`, `0002`/E5 ile eklendi);
  kart detaya onunla gider (`ListingCard.tsx:45`). Taslağın "liste kartı yalnız `ListingId`
  taşıyor" cümlesi **eskiydi**. Kart, detay ve öne çıkanlar (`IFeaturedListingQueries`, aynı
  DTO) aynı kimliği taşır → **backend sözleşmesi değişmez** (**D14**).
- **Slug kalıcı.** İlk yayına almada bir kez üretilir, bir daha değişmez
  (`docs/architecture.md:532`, `Domain/ListingSlug.cs:7-12`). Biçim: `[a-z0-9-]`, 1–160
  karakter (`ListingSlug.cs:61-70`). Satışı geri alınan ilan (`0023`) aynı slug'la döner.
- **Detay okuması görünmeyen ile olmayanı ayırmaz.** `GET /api/catalog/showcase-listings/{slug}`
  ikisine de 404 döner (`Endpoints/ShowcaseEndpoints.cs:23`, `:92-104`; `0002` C13, `0004` D7).
  Uç 404'ü "bulunamadı", diğer hatayı "hata" sayar (`shared/api/catalog.ts:185-203`). Satılan,
  arşivlenen, yayından kalkan ilan 404'tür; rezerve ilan görünür kalır (`IsReserved`).
- **Birden çok slug'ı tek istekte okuyan bir vitrin uç noktası yok**; liste uç noktasının slug
  süzgeci yok (`ShowcaseEndpoints.cs:61-90`). Public okuma uç noktalarında hız sınırı yok —
  sınır yalnız talep (`Leads`) ve personel girişinde (`LeadEndpoints.cs:29`,
  `SessionEndpoints.cs:84`).

**Alan kümesi ve adı (S4)**
- Detay ekranı çekirdek satırları (model yılı, kilometre, şube) kendisi basar, sonra sunucunun
  gönderdiği nitelikleri sırasıyla basar (`ListingDetailPage.tsx:127-153`). Sunucu yalnız açık ve
  public alanları gönderir; uç süzmez (`:138-145`).
- **Alanın ADI vitrin kataloğundandır**, sunucunun `label`'ı değerin etiketidir:
  `ListingDetailPage.tsx:174-191`'deki `attributeName` dosyaya özel bir işlevdir (dışa
  aktarılmaz). Kapı `VitrindeAdiOlanAlanlarTests` bu **dosya yolunu** sabit okur
  (`tests/VehicleCatalog.Tests/Domain/VitrindeAdiOlanAlanlarTests.cs:56`) ve her nitelik
  anahtarı için `case '<anahtar>':` metnini arar (`:48`). Bugün anahtarlar çekirdek altısıdır
  (`body_type`, `fuel_type`, `transmission_type`, `drivetrain_type`, `trim_level`, `color`);
  `LocalizedText` tipinde opsiyonel alan tanımlı değil (`:14-17`). Bu, `0002`'nin
  2026-11-19'da bulunan kaçan hatasının ("Hatchback | Hatchback") dersidir → **D11**.
- **Yer kuralı:** `features/` altındaki klasör adları backend modül adlarıyla aynıdır
  (`docs/frontend.md:44`) → `features/favorites` açılamaz; ekranlar `features/catalog/`'a,
  sayfalar arası durum `shared/lib/`'e yerleşir (**D11**, **D13**).
- Detayın parça haritası ayrı bir özelliktir, niteliklerde tekrar edilmez
  (`ShowcaseListingDetailContracts.cs:24-30`).
- Bugün karşılaştırmada çıkabilecek satır sayısı: fiyat + model yılı + kilometre + şube + altı
  nitelik = **10**.

**Adres ve SEO (0004)**
- Tek parçalı tanınmayan yol içerik sayfası adayıdır; ayrılmış adlar `ContentReservedSlugs.All`
  = `cars`, `admin`, `api`, `assets`, `newsletter` (`Content.Contracts/Pages/ContentPageContracts.cs:60`).
  Uçta ayna liste `RESERVED_SEGMENTS` (`shared/lib/route.ts:234-240`). Bugün `/tr/favorites`
  bir içerik sayfası adayı olarak çözülür ve öyle bir sayfa olmadığı için **404** döner
  (`SeoPage.cs:59-64`, `0004` D7). `/tr/compare?…` de aynı. Yeni rota bu iki listeye ve sunucu
  `SeoPage` sınıflandırmasına (`Host/Seo/SeoPage.cs:6-15`, `:25-65`) girmezse 404 alır;
  `0018` D26 `newsletter` için aynı yolu izledi (`0004` AC-76).
- Kodda, tohumda ve testte `favorites` ya da `compare` adlı bir içerik sayfası yok (arama:
  `src/`, `frontend/src/`, `tests/`). Canlı kurulum yok (barındırma seçilmedi) → ayrılmış ada
  eklemek bir veriyi gölgelemez.
- `noindex` + `Referrer-Policy: no-referrer` deseni bülten sayfalarında var
  (`IndexHtmlRenderer.cs:125-131`). `robots.txt` yalnız `/api/`'yi kapatır; `noindex`'li
  sayfalar bilerek kapatılmaz (`SeoEndpoints.cs:23-26`). Sitemap ana sayfa, liste, ilan ve
  içerik sayfalarını açıkça sayar; yeni sayfa kendiliğinden girmez.
- Listenin parametre kümesi (`features/catalog/listQueryParams.json`, `0004` D23): `bodyType`,
  `fuel`, `transmission`, `brand`, `yearMin`, `yearMax`, `mileageMin`, `mileageMax`, `priceMin`,
  `priceMax`, `sort`, `page`. Küme yalnız liste belgesinde okunur (`SeoHeadBuilder.cs:205-226`).
- Dil değiştirici yolu ve **sorgu dizesini** korur (`LocaleSwitcher.tsx:47`) → karşılaştırma
  adresi dil değişince bozulmaz.

**Yerleşim (0014, 0024, 0026)**
- Üst çubuk her vitrin sayfasında var (`AppShell.tsx:15`, bülten sayfaları dâhil); panel kendi
  kabuğunu taşır (`App.tsx:51-66`). Üst çubukta logo + görünen ad ve dil değiştirici var
  (`TopBar.tsx:25-56`). **Ad iki biçimde davranır:** logoluyken tek satır, taşarsa üç nokta
  (`styles.css:215-223`); **logosuzken bilerek kırılır** (`overflow-wrap: anywhere`,
  `styles.css:203`; `0026` AC-34 bunu korur). Logo dar ekranda büzülmez: yükseklik sabit,
  genişlik en çok `--logo-max-inline-size` = 160 px (`tokens.css:154`, `styles.css:228-235`).
- **Altlık yalnız ana sayfada** (`AppShell.tsx:10-11`, `App.tsx:133-140`) → favori sayacının
  yeri üst çubuktur (**D8**, X8).
- Liste kartında bağlantı yalnız model adını sarar; kartın tamamını sarmak "kart içinde
  ileride başka bir eylem çıktığında iç içe etkileşim üretirdi" (`ListingCard.tsx:40-45`).
  Kart iki yerde: liste (`ShowcaseListPage.tsx:9`) ve ana sayfanın öne çıkanları
  (`home/FeaturedSection.tsx:5`).
- Vitrin tam sayfa gezinir; rota her yüklemede bir kez çözülür (`App.tsx:41-47`).

**Paket ve çeviri (C12)**
- Vitrin kataloğu üç dilin tamamıyla ilk pakete girer (`shared/i18n/i18n.ts:2-14`). Tembel
  parçanın kendi kataloğunu ekleme yolu **var**: `registerCatalogs` (`i18n.ts:21-25`); panel
  böyle iner (`shared/i18n/panel.ts`). `check:locales`'in dört denetimi **iki kümeye sabit
  bağlıdır**: küme listesi (`scripts/check-locale-keys.mjs:28-32`), "bir anahtar iki kümede
  birden" (`:98-104`, yalnız vitrin↔panel), Arapça çoğul birleşimi (`:170`, vitrin + panel) ve
  vitrin sınırı (`:216`, `:292`, yalnız `panel.ts`). Üçüncü küme bu dördüne ayrı ayrı girmezse
  kapı onu görmez (**D13**).
- Koddaki sabit `t('…')` anahtarının bir katalogda **var olduğunu** ölçen kapı yok; `0025`'te
  boş bir `aria-label` bu yüzden kaçtı (**D19**).
- Ölçüm (taslak metinlerle, üç dilin JSON'u üstüne marjinal `gzip -9`): favori düğmesi + sayaç +
  sınır + depo hatası anahtarları ≈ **184 B**; favoriler ve karşılaştırma ekranlarının ~20
  metni ≈ **710 B**. Ekran metinleri ilk pakete girerse bütçe tek başına aşılır → **D13**.
- **Kod payı ölçümü (B5).** `main`'in derlenmiş girişinin (`index-*.js` 105 449 B gzip,
  `index-*.css` 11 907 B gzip) sonuna, küçültülmüş biçimde elle yazılmış bir taslak eklendi ve
  marjinal `gzip -9` ölçüldü. Taslağın içeriği: depo okuma/yazma, deneme yazması, biçim doğrulaması,
  dinleyici, düğme, sayaç, iki kez aynı kalp yolu.
  - JS ≈ **923 B**
  - CSS ≈ **113 B**
  - metin anahtarları ≈ **184 B**
  - toplam ≈ **1,2 KB**, yani **+512 B'ın iki katından fazla**.

  Taslak gerçek kodun üst sınırı değil, büyüklük mertebesidir. Aşarsa sırayla düşenler:
  1. Tek simge: dolu ve boş hâl aynı yol, dolgu CSS'ten.
  2. Düğme ve sayaç aynı simge bileşenini kullanır.
  3. Canlı bölge metinleri tek ICU `select` anahtarı.
  4. Kayıt doğrulaması yalnız slug biçimi + tip.
  5. Detay düğmesinin metni sayaç anahtarlarıyla ortak.

  Tahmini kazanç 200–300 B; **bütçe yine aşılır** → **D18**.
- `0024` dersi: tembel parça giriş ile modül paylaşırsa paketleyici ortak modülü kritik yolda
  ayrı bir dosyaya çıkarabiliyor ve `modulepreload` ediyordu (`docs/frontend.md` § 7,
  `0024` QA 2 BULGU-1). Panel ve bülten sayfaları tembel `lazy(() => import(…))` ile iner
  (`App.tsx:22-39`).
- Ana sayfa LCP medyanı ~2270–2410 ms (eşik 2500); perf koşusu ana sayfa, liste ve detayı ölçer
  (`.github/workflows/perf.yml:159-162`).

**Günlük**
- Host istek yolu günlüğü yazmaz (`appsettings.json:5`, `Microsoft.AspNetCore: Warning`).
  Okuma zamanlaması riski ters vekil ya da CDN erişim günlüğünde doğar (**D15**, X11).

**Depo**
- Vitrin bugün tarayıcı deposuna hiç yazmıyor (`LocaleSwitcher.tsx:8`: dil çerez veya depo
  yazmaz). `check:session` kapısı depo anahtarında `token`, `session`, `auth` geçen okuma/yazmayı
  kırar (`scripts/check-no-session-storage.mjs:17-24`).

**Taslaktan farklı bulunanlar**
1. Liste kartı `Slug` taşıyor (taslak "taşımıyor olabilir" diyordu).
2. Altlık yalnız ana sayfada; mockup'taki altlık bağlantısı her sayfada görünmezdi.
3. Kart iki ekranda (liste ve ana sayfa); ana sayfa perf kapısına favori düğmesiyle girer.
4. Alan adı eşlemesi dosyaya özel; ikinci ekran onu ancak taşınırsa paylaşır ve kapı dosya
   yolunu sabit okur.
5. Yeni rotalar bugün 404; ayrılmış ad + `SeoPage` + uç aynası üçlü değişiklik.
6. Ekran metinleri ilk pakete girerse +512 B bütçesi metinle aşılır; tembel katalog yolu var
   ama bugün yalnız panel için.
7. Favoriler ekranının veri yolu: çoklu slug okuması yok → detay okuması slug başına (**D10**,
   **D14**).

### Bilinen tuzaklar

1. **Depoyu her zaman çalışır saymak.** Gizli pencerede, site verisi kapalıyken veya kota
   doluyken okuma/yazma **hata atar**; sarmalanmazsa üst çubuk tüm sayfayı düşürür — üst
   çubuk her sayfadadır (C9, R15).
2. **Favorideki aracın satılması.** Ziyaretçi üç gün sonra döndüğünde araç satılmış olabilir;
   sessizce silmek "ben bunu eklemiş miydim" dedirtir. Yalnız slug saklanırsa düşen favorinin
   **adı** da gösterilemez (R10, **D4**).
3. **404 ile ağ hatasını aynı saymak.** Geçici bir hata favoriyi "vitrinden düştü" diye
   işaretlerse ziyaretçi onu yanlışlıkla kaldırır (**D10**).
4. **Karşılaştırmada ikinci bir alan adı eşlemesi.** Detaydaki `attributeName`'i kopyalamak,
   kapının görmediği ikinci bir liste üretir; yeni bir alan detayda adlanır, karşılaştırmada
   "Değer | Değer" basar (`0002` kaçan hatası).
5. **Karşılaştırmada kapalı alanı göstermek** veya uçta süzmek — Yasak Listesi 41 (C3).
6. **Rotayı yalnız uçta tanımlamak.** Sunucu 404 + `noindex` verirken uç ekranı basar: arama
   motoru 404 görür, ziyaretçi sayfa görür, ama paylaşılan bağlantı ön izlemesi ve izleme 404
   der (`0004` tuzak 9).
7. **Ekran metinlerini vitrin kataloğuna yazmak.** Üç dilin metni ilk pakete girer ve ana
   sayfanın LCP payını yer (Context, C12).
8. **Tembel parçanın girişle ortak modülünün kritik yola çıkması** (`0024` BULGU-1).
9. **Kalp ikonunu tek anlam taşıyıcısı yapmak** veya her kartta aynı erişilebilir adı vermek —
   ekran okuyucu listede yirmi dört "Favorilere ekle" duyar, hangisi olduğunu bilmez (R12).
10. **Bayat sekmenin yazması.** İki açık sekmede, eski listeyi bellekte tutan sekme yazdığında
    öbür sekmenin eklediği favoriyi siler (**D4**).
11. **Geniş tabloyu sayfayı kaydırtarak çözmek** (R14).
12. **Depo anahtarına `session` benzeri bir ad vermek** — `check:session` kırılır; kapı
    kaldırılmaz, ad seçilir.
13. **Üst çubuğa eklenen öğenin adı kırması veya taşırması** — `0026`'da ad iki satıra kırılınca
    CLS 0.041'e çıkmıştı; dar ekranda sayaç, logo, ad ve dil değiştirici aynı satırdadır.

---

## Verilen kararlar

### Sahip kararları (2026-11-26)

- **S1 — Karşılaştırmaya en fazla üç araç alınır.** *Gerekçe:* mobil öncelik; dört sütun dar
  ekranda okunmaz. *Bedel:* dört araç arasında seçim yapan ziyaretçi iki tur yapar.
- **S2 — Favoriler ekranında sade, kalıcı tek satır not:** "Favorileriniz bu tarayıcıda
  saklanır; tarayıcı verisi silinirse kaybolur." Uyarı kutusu ya da açılır pencere yok.
  *Bedel:* ekranda kalıcı bir açıklama satırı.
- **S3 — Favori verisi sunucuya hiç gitmez; `Engagement` bu kartta doğmaz.** *Bedel:* galeri
  "en çok favorilenen"i göremez → X3.

### Proje Yöneticisi kararları (2026-11-26)

- **S4 — Karşılaştırma, detayla aynı alan kümesini gösterir** (`0002` R7/R8): çekirdek alanlar +
  açık ve public opsiyonel alanlar. Alan adı vitrin kataloğundaki **tek** eşlemeden gelir.
  Satır katlama kararı Analiste bırakıldı → **D11**.
- **S5 — Favori ve karşılaştırma slug tutar; karşılaştırma adresi slug listesi taşır.**
  Koddan doğrulandı: kart DTO'su slug taşıyor (Context) → **D14**.

### Proje Yöneticisi kararları — D1–D19 (2026-11-26)

D1–D17 onaylandı (Proje Yöneticisi kararı, 2026-11-26); **D5** ve **D6** ayrıca sahibe soruldu
(sahip kararı, 2026-11-26). QA CLARIFY turunun bulguları (B1–B17) kararların içine işlendi;
her değişiklik ilgili kararda `(Bn)` ile işaretli. **D19** turda eklendi ve onaylı; **D18**
karar bekliyor.

Biçim: **karar** · *gerekçe* · *bedel*.

**D1 — Adresler: `/{locale}/favorites` ve `/{locale}/compare`.** İki ad her dilde aynıdır ve
çevrilmez (`cars`, `newsletter` gibi). İkisi `ContentReservedSlugs.All`'a ve uçtaki ayna
listeye eklenir; bu adla içerik sayfası açılamaz. Bu iki yolun altındaki her yol
(`/tr/favorites/x`, `/tr/compare/x`) `0004` D8 gereği 404 + "bulunamadı"dır. Bugün bu adda içerik
sayfası olmadığı için veri göçü yoktur.
*Gerekçe:* tek parçalı yol içerik sayfası adayıdır; ayrılmazsa 404 alır ya da bir gün aynı adlı
içerik sayfası onu gölgeler (Context). *Bedel:* İngilizce iki segment TR/AR adreslerde de
görünür; `Content`'in ayrılmış listesi büyür.

**D2 — İki sayfanın belgesi: 200, `noindex`, kanoniksiz, `no-referrer`.** Sunucu iki yeni sayfa
türü tanır ve ikisine de 200 verir — karşılaştırma adresindeki slug'lar görünmeyen ilan olsa da
(ekran durumu uçta çözülür, **D3**). Belge `<meta name="robots" content="noindex">` taşır;
kanonik, dil alternatifi, Open Graph ve yapısal veri taşımaz. Başlık SEO kümesinden, `0004`
D11 biçiminde ("Favorilerim — {galeri adı}", "Araç karşılaştırma — {galeri adı}"); açıklama
yok. Sitemap'e girmez; `robots.txt` değişmez (`noindex` okunabilsin). Yanıt
`Referrer-Policy: no-referrer` taşır.
*Gerekçe:* sayfa ziyaretçinin kendi seçimidir, galerinin içeriği değil (R16); karşılaştırma
adresleri sonsuz bir küme üretir. `no-referrer`, favoriler ekranının yaptığı okumaların
`Referer` başlığında sayfa adresini taşımasını önler (S3). Okumaların **zamanlamasını**
gizlemez (B2 → **D15**, X11). *Bedel:* paylaşılan
karşılaştırma bağlantısının sosyal ağ ön izlemesi galerinin genel başlığıyla sınırlı kalır.

**D3 — Karşılaştırma adresi: `?listing=<slug>` tekrarlı, sıra = sütun sırası.** Örnek:
`/tr/compare?listing=toyota-corolla-2021-a1b2c3&listing=honda-civic-2020-d4e5f6`. Uç adresi
şöyle okur:
1. Biçimi slug biçimine uymayan değer (`[a-z0-9-]`, 1–160; büyük harf de uymaz) yok sayılır.
2. Yinelenen değer ikinci kez sayılmaz; ilk görülme sırası korunur.
3. İlk üç değer alınır, fazlası yok sayılır.
4. Okunan küme adresle aynı değilse adres **geçmişe kayıt eklemeden** okunan kümeye düzeltilir.
   Yalnız `listing` değerleri normalleşir; başka parametreler (`utm_source`) olduğu gibi korunur
   (B11).
5. İkiden az geçerli değer kalırsa "karşılaştırmak için en az iki araç" durumu basılır.

`listing` adı liste parametre kümesinde (`0004` D23) yoktur ve eklenmez.
*Gerekçe:* `0015`'in tekrarlı slug düzeni (`brand`, `fuel`); okunur, paylaşılır, dil
değiştirici korur (Context). *Bedel:* adres uzun olabilir (en çok ~500 karakter).

**D4 — Kayıt: slug + eklenme anı + görüntü özeti, dilden bağımsız tek kayıt.** Her favori
slug, eklenme anı ve **eklendiği andaki model adı ile model yılını** taşır. Özet yalnız düşen
favorinin adını göstermek içindir (R10); fiyat ve rezerve hâli saklanmaz, her açılışta güncel
okunur (R3). Kayıt tarayıcıda dilden bağımsız tek yerdedir (R13). Adı `check:session`
kapısının yasak sözcüklerini taşımaz. Her ekleme ve çıkarma **o anki kaydı yeniden okuyarak**
yapılır; aynı slug iki kez bulunmaz. **Düğmenin eylemi ekranda gösterdiği hâldir** (B8):
"ekle" görünüyorsa ekler, kayıtta zaten varsa hiçbir şey yapmaz; "çıkar" görünüyorsa çıkarır,
kayıtta yoksa hiçbir şey yapmaz. Eylem yeniden okunan kayıtla birleştirilir; eylemden sonra
düğme ve sayaç kayıttan yeniden çizilir. Okunamayan kayıt (bozuk biçim, tanınmayan sürüm) boş
sayılır ve sayfa düşmez; tek tek geçersiz öğeler atlanır.
*Gerekçe:* tuzak 2 ve 10. *Bedel:* bozuk bir kayıt sessizce boş görünür — kaydı bozan
vitrin değil, tarayıcı ya da elle müdahaledir.

**D5 — En çok 24 favori (sahip kararı, 2026-11-26).** Sınırdayken ekleme yapılmaz ve ziyaretçiye nedeni söylenir (canlı
bölge); en eski favori kendiliğinden silinmez. Vitrinden düşen favoriler, ziyaretçi kaldırana
kadar sayılır.
*Gerekçe:* favoriler ekranı slug başına bir okuma yapar (**D10**); sınırsız liste sınırsız
istek demek. 24, vitrinin sayfa boyutudur. *Bedel:* 24'ten fazla araç beğenen ziyaretçi önce
birini çıkarır.

**D6 — Karşılaştırma favoriler ekranından seçilir; ayrı karşılaştırma listesi yok (sahip
kararı, 2026-11-26: mockup sapması onaylandı).** Favoriler
ekranında her görünür favorinin bir "karşılaştırmaya seç" denetimi vardır. Üçüncüden sonraki
seçim yapılmaz ve nedeni söylenir. "Karşılaştır" eylemi iki ya da üç seçim varken çalışır ve
seçimleri ekrandaki sırayla taşıyan adrese gider. Seçim saklanmaz: karşılaştırmanın durumu
yalnız adrestedir (C5). Vitrinden düşmüş ya da okunamamış favori seçilemez. İlan detayında
"Karşılaştır" düğmesi **yoktur** (X7).
*Gerekçe:* tek yerel liste, tek kaybolma kuralı; karşılaştırma adresi zaten paylaşılabilir
durumun tek kaynağı. *Bedel:* mockup'tan sapma (sahip onayladı); karşılaştırmak isteyen ziyaretçi önce favorilere
ekler. Favorilerden karşılaştırmaya gidip geri dönen ziyaretçinin seçimi sıfırlanır.

**D7 — Favori düğmesi.** Üç yerde: kart (liste ve ana sayfa — kartın her kullanımı), ilan
detayı, karşılaştırma sütunu.
- Kartta yalnız simgeli bir **düğmedir**; bağlantının dışında durur (Context). Erişilebilir adı
  aracın model adını ve yılını içerir.
- Detayda görünür metinli düğmedir ("Favorilere ekle" / "Favorilerde").
- Durum "basılı" niteliğiyle bildirilir. Dolu/boş ayrımı **biçimle** de yapılır (dolgulu ve
  çizgili simge), yalnız renkle değil. Dokunma hedefi ≥ `--touch-target-min-size`; odak halkası
  görünür.
- Basınca kibar bir canlı bölge "eklendi" / "çıkarıldı" der.
- Ekleme ve çıkarma **hiçbir ağ isteği üretmez.**

*Gerekçe:* R1, R12, tuzak 9. *Bedel:* kartta yeni bir etkileşimli öğe; ana sayfa perf kapısına
girer.

**D8 — Sayaç üst çubukta, her vitrin sayfasında, sıfırda da.** Favoriler ekranına götüren bir
bağlantıdır; sayı ICU çoğuluyla kurulur. İlk boyamada depodan okunmuş sayıyı gösterir; sayfa
açıldıktan sonra kendiliğinden genişleyip daralmaz (yalnız ziyaretçinin eylemiyle değişir).
Panelde yoktur. Üst çubuk taşmaz; ad iki biçimde ele alınır (B7):
- **Logosuz:** ad bugünkü gibi kırılabilir (`0026` AC-34 değişmez); web fontu gelirken CLS ≤ 0.1.
- **Logolu:** ad tek satırdır; 320 px'te sayaç ve dil değiştirici görünür kalır, adın görünen
  kısmı en az 6 karakterdir.
- **Pay yetmezse sıra:** önce dar ekranda sayaç yalnız simge + sayı olur (erişilebilir ad tam
  cümle); bu da yetmezse dar ekranda logonun azami genişliği 160 px'ten **120 px**'e iner.
*Gerekçe:* altlık yalnız ana sayfada (Context, X8). Sıfırda gizlemek, ilk favoride üst çubuğu
oynatırdı. *Bedel:* üst çubukta kalıcı bir öğe daha; dar ekranda adın üç noktaya düşme ihtimali
artar ve dar ekranda logo küçülebilir.

**D9 — Depo kullanılamazsa favori arayüzü basılmaz.** Tespit sayfa açılışında bir **deneme
yazması + silmesi**dir. Üç biçim de "kullanılamaz" sayılır (B12): (1) depo erişicisinin kendisi
atar; (2) okuma çalışır, yazma kota hatası atar; (3) ikisi de atar. Depo kullanılamazsa kartta
ve detayda favori düğmesi, üst çubukta sayaç **yoktur**. Favoriler ekranı adresle açılırsa
"bu tarayıcı favori saklamaya izin vermiyor" durumunu basar. Karşılaştırma ekranı adresle
çalışmaya devam eder (favori düğmesi olmadan). Sayfa açıkken bir yazma başarısız olursa
(kota) durum değişmez, ziyaretçiye nedeni söylenir ve sayfa çalışmaya devam eder.
*Gerekçe:* R15; tam sayfa gezintide bellekte tutulan favori bir sonraki sayfada kaybolur ve
"kaydedildi" yalanı olur. *Bedel:* gizli penceredeki ziyaretçi özelliğin varlığını görmez.

**D10 — Favoriler ekranı.** Sıra: son eklenen önce.
- **Veri:** her favori mevcut ilan detayı okumasıyla, slug başına bir istekle ve paralel okunur
  (en çok 24, D5).
- **Görünür favori:** kart biçimi — ilk görsel, model, yıl, kilometre, **güncel** fiyat, şube,
  rezerve rozeti, detay bağlantısı, favoriden çıkar, karşılaştırmaya seç.
- **404 dönen favori:** "Artık vitrinde olmayanlar" kümesinde, saklanan model adı ve yılıyla,
  bağlantısız. Tek tek kaldırılır; küme için "hepsini kaldır" eylemi vardır. Kendiliğinden
  silinmez.
- **404 dışı hata dönen favori:** kendi satırında hata ve "tekrar dene" ile gösterilir; düşmüş
  sayılmaz.
- **Durumlar:** yükleniyor (iskelet) · hiç favori yok (vitrine götüren tek eylem) · hepsi düştü
  (ayrı ekran) · tamamı okunamadı (hata + tekrar dene) · depo kullanılamıyor (D9).
- **S2 notu** depo kullanılamıyor durumu hariç her durumda görünür.

*Gerekçe:* R3, R10, R11, tuzak 3. *Bedel:* ekran 24'e kadar istek atar; tek istekli bir okuma
backend sözleşmesini büyütürdü (D14).

**D11 — Karşılaştırma alan kümesi, adı ve satırları (S4).**
- **Sütun başı:** ilk görsel (varsa), model adı (detay bağlantısı), rezerve rozeti, favori
  düğmesi.
- **Satırlar:** fiyat, model yılı, kilometre, şube, sonra detay yanıtındaki nitelikler.
- **Satır kuralı:** bir nitelik satırı yalnız karşılaştırılan araçlardan **en az birinde**
  varsa basılır. Sıra, sütunlarda ilk görülme sırasıdır (sunucunun yazdığı sıra). Değeri
  olmayan hücre "Belirtilmemiş" yazar.
- **Ad tek yerden:** alanın adı detayla **aynı tek eşlemeden** gelir (B6). Eşleme
  `frontend/src/features/catalog/attributeName.ts`'e taşınır ve dışa aktarılır; detay ve
  karşılaştırma onu içe aktarır. `VitrindeAdiOlanAlanlarTests` bu dosyayı okur. Kapıya ikinci
  bir ölçü eklenir: `case 'body_type':` dizgesi `frontend/src` altında **tam olarak bir**
  dosyada geçer.
- **Yerler:** favoriler ve karşılaştırma ekranları `features/catalog/` altındadır (yer kuralı,
  Context).
- **Detayda kalanlar:** parça haritası karşılaştırmada yoktur (detayda kalır).
- **Satır katlama yok.** Bugün en çok 10 satır var (Context); nitelik sayısı 15'i geçtiğinde
  katlama yeniden açılır.

*Gerekçe:* R7, R8, tuzak 4; katlama 10 satırda okumayı kolaylaştırmaz, bir etkileşim ve bir
durum ekler. *Bedel:* bir kapı ve bir dosya taşınır; opsiyonel alan çoğalırsa tablo uzar.

**D12 — Mobil, RTL ve semantik.** Karşılaştırma gerçek bir tablodur: sütun başlıkları araçlar,
satır başlıkları alan adları. 360 px ve üstünde üç sütun **iç kaydırma olmadan** sığar (dar
ekranda alan adı değerlerin üstünde durabilir). 320 px'te tablo kendi içinde kayabilir, sayfa
kaymaz. Kayabilen kap klavyeyle odaklanabilir ve adlandırılmış bir bölgedir (B17, WCAG 2.1.1).
Kırılmaz uzun belirteçler (model adı, fiyat) hücre içinde kırılır (B10). Arapça'da ilk araç satır başı tarafında (sağda) durur. Model adı ve değerler karışık
yönlü metin olarak basılır.
*Gerekçe:* R13, R14. *Bedel:* dar ekranda tablo görünümü masaüstünden farklı dizilir.

**D13 — Paket bölünmesi ve ölçülebilir bütçe.**
- **İlk paket:** yalnız favori düğmesi, sayaç, depo erişimi ve bunların metinleri (ekle,
  çıkar, durum, sayaç, sınır, yazma hatası) girer. Depo ve sayaç durumu
  `frontend/src/shared/lib/favorites.ts`'tedir (iki feature ve kabuk kullanır; B6).
- **Tembel parça:** favoriler ve karşılaştırma ekranları, onların metinleri ve stilleri
  `lazy(() => import(…))` arkasında ayrı parça(lar)dadır. Ekran metinleri **üçüncü bir katalog
  kümesinde** yaşar ve parçayla iner (`registerCatalogs`). `check:locales` bu kümeyi dört denetimin
  **her birine** ayrı ayrı alır (B3): üç dil eşitliği · Arapça çoğul biçimleri · başka bir
  kümede tekrar yok · küme modülü ve anahtarları vitrinin statik ağacından ulaşılmaz.
- **Kritik yol değişmez:** `index.html`'in `modulepreload` listesi ve girişin statik içe
  aktardığı dosya sayısı `main` ile aynıdır.
- **Bütçe:** ilk paket `0024` AC-55'in tanımıyla ölçülür; sınırı **D18** belirler.

*Gerekçe:* Context ölçümü (ekran metinleri tek başına ~710 B), tuzak 7–8. *Bedel:* ADR
`0013`'e üçüncü bir küme eklenir (D17); favoriler ekranı ilk açılışta bir parça daha indirir.

**D14 — Backend sözleşmesi değişmez.** Kart DTO'su slug taşıyor; favoriler ve karşılaştırma
mevcut detay okumasını kullanır. `VehicleCatalog` kodu değişmez (yalnız kapı testi okuduğu
dosyayı izler, D11). Değişen sunucu kodu `Host` (sayfa türü, belge, başlık) ve
`Content.Contracts`'ın ayrılmış ad listesidir.
*Gerekçe:* S5 doğrulaması. *Bedel:* D10'un 24 isteği.

**D15 — "Sunucuya gitmez"in ölçüsü (S3).** Ekleme/çıkarma ağ isteği üretmez. Hiçbir istek
gövdesinde, sorgusunda veya başlığında favori listesi (birden çok favori slug'ı bir arada)
taşınmaz; detay okuması tek slug taşır — bir detay ziyaretinin taşıdığından fazlası değil.
Sunucu bu kartla yeni bir kayıt, tablo, günlük alanı veya olay yazmaz. Karşılaştırma adresinin
belge isteği en çok üç slug taşır (B1): bu, ziyaretçinin seçip paylaşabildiği karşılaştırmadır,
favori listesi değil. Sorgu biçimi kalır. Parça (`#`) biçimi bir şey kazandırmazdı, çünkü ekran
her slug için detay okuması zaten yapıyor.
*Gerekçe:* sahip kararını test edilebilir yapar.
*Kabul edilen bedeller:*
- Karşılaştırma adresinin belge isteği seçilen slug'ları sunucuya taşır.
- **Okuma zamanlaması (B2):** favoriler ekranı aynı IP ve tarayıcıdan aynı anda 24'e kadar
  detay ve görsel okur. Bu zamanlama, favori kümesini ters vekil ya da CDN erişim günlüğünden
  çıkarılabilir kılar. Uygulama bugün istek yolu günlüğü yazmaz (Context). Karşılığı `deploy.md`
  notudur (**D17**); zamanlamanın gizlenmesi kapsam dışıdır (X11).

**D16 — Sözlük.** `docs/domain.md`'ye iki terim: **Favori** (`Favorite`) — ziyaretçinin bu
tarayıcıda işaretlediği ilan; sunucuda yoktur, slug ile tutulur · **Karşılaştırma**
(`Comparison`) — adreste taşınan, en çok üç ilanın yan yana görünümü.
*Gerekçe:* DoD T7. *Bedel:* yok.

**D17 — Belgeler.**
- `docs/architecture.md` karar defteri: vitrin adresleri satırına iki adres, ayrılmış adlar
  satırına `newsletter`, `favorites`, `compare` (satır bugün `newsletter`'ı da saymıyor).
- ADR `0013`'e "tembel vitrin kümesi" eki.
- `docs/frontend.md` § 7: favori düğmesi ve depo kuralı.
- `docs/deploy.md` (B2): ters vekil ya da CDN erişim günlüğünde
  `/api/catalog/showcase-listings/*` yollarının saklama süresi kısa tutulur. Bu yolların
  ziyaretçinin hangi araçlarla ilgilendiğini gösterdiği ve KVKK açısından kişisel veriyle
  ilişkilendirilebileceği yazılır.

*Gerekçe:* DoD T8. *Bedel:* yok.

**D18 — İlk paket bütçesi (karar bekliyor — Proje Yöneticisi; B5).** Context ölçümü favori
düğmesi + sayaç + depo kodunu ~1,2 KB gzip buluyor; düşürme sırası uygulansa da ~900 B'ın altına
inmesi beklenmiyor. Seçenekler:
- **(a) Önerim — bu kart için sınır +1280 B; C3 asıl bekçi kalır.** Düşürme sırası (Context)
  zorunludur. Ölçüm head commit'te yapılır. Ana sayfa, liste ve detay medyanları PR'da `main`
  ile yan yana yazılır.
  *Gerekçe:* düğme kartın ve detayın ilk boyamasındadır; tembel inerse yer ayırsa da sayaç
  yanlış sayıyla başlar (AC-29) ve ilk etkileşim gecikir. 1,2 KB, yavaşlatılmış 4G'de birkaç
  ms'lik aktarımdır. Asıl risk LCP payıdır ve onu C3 ölçer.
  *Bedel:* `0024`'ün +512 B ilkesi bu kartta gevşer.
- **(b) +512 B kalır; düğme ve sayaç kodu ilk boyamadan sonra tembel iner.** Yer ayrılır, sayı
  ilk karede boştur. AC-29 "ilk karede 3"ten "yerleşince 3, kayma yok"a döner.
  *Bedel:* ilk etkileşim gecikir ve kod karmaşıklaşır.
- **(c) +512 B kalır; kapsam daralır** (ör. ana sayfa kartında düğme yok).
  *Bedel:* R1 daralır.

**D19 — Anahtar varlığı kapısı (B4).** `check-locale-keys.mjs`, bütün `frontend/src` altında
her sabit `t('…')` anahtarının en az bir kümede bulunduğunu ölçer. Şablonlu anahtarlar
(`` t(`app.locale.${x}`) ``) önekleriyle ölçülür: önekle başlayan en az bir anahtar vardır.
Mevcut kodda eksik anahtar çıkarsa gerçek bir kusurdur: kart onu düzeltir ve PR'da listeler.
*Gerekçe:* `0025`'in boş `aria-label`'ı (Context); bu kart ~25 yeni anahtar ekliyor.
*Bedel:* kapı yazmak; mevcut kodda bulunacak eksiklerin düzeltilmesi.

---

## Bilerek değişecek eski testler (B9)

| Test / kapı | Neden değişir | Not |
|---|---|---|
| `tests/VehicleCatalog.Tests/Domain/VitrindeAdiOlanAlanlarTests.cs:56` | Okuduğu yol `attributeName.ts`'e döner; "tek dosyada" ölçüsü eklenir (D11). | Teori satırları aynı kalır. |
| `frontend/scripts/check-locale-keys.mjs` | Üçüncü küme dört denetime girer (D13); anahtar varlığı kapısı (D19). | Negatif kanıtlar AC-69, AC-72. |
| `frontend/src/shared/lib/route.ts:234-240` + `src/Host/Seo/SeoPage.cs` | İki yeni ayrılmış ad ve iki sayfa türü (D1, D2). | `IcerikZinciriTests.cs:213-214` ve `KuralTests.cs:52/57` `Contains` ile ölçer, kırılmaz. `AdresVeSssTests.cs:93` `All`'ı gezer, iki yeni adı da reddetmeli ve yeşil kalmalı. |
| `frontend/e2e/galeri-logosu.spec.ts:121` | 320 px üst çubuğa sayaç girer; logo genişliği dar ekranda 120 px'e inebilir (D8). | Değişirse PR'da kriteriyle yazılır. |
| Kart yapısını sayan testler (`ShowcaseListPage.test.tsx`, `HomePage.test.tsx`) | Kartta yeni düğme (D7). | Yalnız düğmenin varlığı eklenir; mevcut iddialar silinmez. |

Her Host satırı için e2e ikizi aranır (`0024` dersi): sunucu testi yeşilken uçta aynı yol başka
ekran basabilir.

---

## Sahibe sorular

Yok. S1–S3, D5 ve D6 sahibe soruldu ve cevaplandı (2026-11-26).

---

## Acceptance Criteria

### Adres ve belge (D1, D2, D3 · R9, R16)

- **AC-1.** `/tr/favorites`, `/ar/favorites`, `/en/favorites` 200 döner ve favoriler ekranını
  basar; `/tr/compare?listing=a-2020-aaaaaa&listing=b-2021-bbbbbb` 200 döner ve karşılaştırma
  ekranını basar — slug'lar görünmeyen ilan olsa da.
- **AC-2.** `/tr/favorites/x`, `/tr/compare/x` ve `/tr/compare/x/y` 404 döner, `noindex` taşır ve
  uç "bulunamadı" ekranını basar (tr ve ar için görsel kanıt).
- **AC-3.** Slug'ı `favorites` ya da `compare` olan bir içerik sayfası kaydı üç dilde de
  `ContentErrors.SlugReserved` ile reddedilir; uçtaki ayna liste ile `ContentReservedSlugs.All`
  aynı kümedir (test iki listeyi karşılaştırır).
- **AC-4.** İki sayfanın belgesi `<meta name="robots" content="noindex">` taşır ve
  `rel="canonical"`, `hreflang` alternatifi, `og:` etiketi ve `application/ld+json` **taşımaz**.
- **AC-5.** İki sayfanın `<title>`'ı SEO kümesindeki başlık + galeri adıdır (`0004` D11 biçimi);
  üç dilde çevrilidir.
- **AC-6.** İki sayfanın yanıtı `Referrer-Policy: no-referrer` başlığını taşır.
- **AC-7.** `sitemap-tr.xml` (ve ar, en) ne favoriler ne karşılaştırma adresini içerir;
  `robots.txt` değişmemiştir.
- **AC-8.** Karşılaştırma adresi D3'e göre okunur:
  `?listing=b-2020-bbbbbb&listing=a-2020-aaaaaa&listing=b-2020-bbbbbb&listing=Abc-2020-cccccc&listing=c-2020-cccccc&listing=d-2020-dddddd&listing=x%20y&utm_source=z`
  → sütunlar `b-…`, `a-…`, `c-…` (bu sırayla). Yinelenen, büyük harfli, boşluklu ve dördüncü
  geçerli değer yok sayılır. Adres geçmişe yeni kayıt eklenmeden
  `?listing=b-2020-bbbbbb&listing=a-2020-aaaaaa&listing=c-2020-cccccc&utm_source=z`'ye düzeltilir
  (`history.length` değişmez; `utm_source` korunur).
- **AC-9.** Geçerli değeri ikiden az olan karşılaştırma adresi (`/tr/compare`,
  `?listing=a-2020-aaaaaa`, `?listing=%%%`) "en az iki araç seçin" durumunu ve favorilere
  götüren tek eylemi basar.
- **AC-10.** Karşılaştırma ekranında dil değiştirildiğinde hedef adres aynı `listing` değerlerini
  aynı sırayla taşır; ar'da aynı araçlar sütunlarda görünür.
- **AC-11.** Liste belgesinin `noindex, follow` kuralı (`0004` D23) değişmez:
  `listQueryParams.json` bu kartla değişmez ve `/tr/cars?listing=x` belgesi kanonik taşır.

### Kayıt ve depo (D4, D5, D9 · R4, R13, R15, C9)

- **AC-12.** İki sekme, ikisi de boş kayıtla açılmış (D4). A'da X eklenir. B yenilenmeden Y'yi
  ekler → kayıtta X ve Y vardır, B'nin sayacı **2**'dir. B'de X basılı görünmüyor; basılır →
  kayıtta X **tek** kez vardır (no-op birleşme), B'de X basılı ve sayaç 2'dir. A yenilenmeden X'i
  çıkarır → kayıtta yalnız Y kalır.
- **AC-13.** `/tr/cars`'ta eklenen favori `/ar/` ve `/en/favorites`'ta görünür; sayaç üç dilde
  aynı sayıyı gösterir.
- **AC-14.** Depodaki değer bozuk JSON, tanınmayan sürüm ya da yanlış biçimli öğe içerdiğinde
  sayfa düşmez: bozuk kayıt boş sayılır, geçerli öğeler korunur ve sayaç geçerli öğe sayısını
  gösterir.
- **AC-15.** Depo kullanılamazlığının üç biçiminde liste, ana sayfa ve detay hatasız basılır,
  favori düğmesi ve sayaç **yoktur** ve konsolda yakalanmamış hata yoktur (D9; e2e,
  `addInitScript` ile):
  (1) `Object.defineProperty(window, 'localStorage', { get() { throw … } })`;
  (2) `getItem` çalışır, `setItem` `QuotaExceededError` atar;
  (3) `getItem` ve `setItem` ikisi de atar.
- **AC-16.** Depo kullanılamazken `/tr/favorites` "bu tarayıcı favori saklamaya izin vermiyor"
  durumunu basar; S2 notu bu durumda yoktur. `/tr/compare?…` iki aracı karşılaştırır,
  sütunlarda favori düğmesi yoktur.
- **AC-17.** Sayfa açıkken yazma kota hatası verirse favorinin durumu değişmez, canlı bölge
  nedeni söyler ve sayfa çalışmaya devam eder.
- **AC-18.** 24 favori varken 25. ekleme yapılmaz: düğme basılı hâle geçmez, canlı bölge sınırı
  söyler (ICU, sayı metinde), kayıt 24 öğe kalır ve en eski favori silinmez.
- **AC-19.** Depo anahtarının adı `token`, `session` ya da `auth` içermez: birim testi dışa
  aktarılan anahtar sabitinin `/token|session|auth/i`'ye uymadığını ölçer (`check:session`
  yalnız dizge değişmezi görür). `check:session` yeşil.
- **AC-20.** Favori eklendiği anda kayıt slug, eklenme anı, model adı ve model yılını taşır;
  fiyat ve rezerve hâlini **taşımaz** (birim testi kayıt biçimini ölçer).

### Favori düğmesi (D7 · R1, R12)

- **AC-21.** Liste kartında, ana sayfanın öne çıkan kartında ve ilan detayında favori düğmesi
  vardır. Basmak ekler, yeniden basmak çıkarır; sayfa yenilenince durum korunur.
- **AC-22.** Kartta düğme model adı bağlantısının **içinde değildir**; kartta iç içe etkileşimli
  öğe yoktur (`a button`, `button a` seçicisi boş).
- **AC-23.** Kart düğmesinin erişilebilir adı model adını ve yılını içerir; aynı sayfadaki iki
  kartın düğme adları farklıdır. Detay düğmesinin görünür metni duruma göre "Favorilere ekle" /
  "Favorilerde"dir.
- **AC-24.** Düğme `aria-pressed` ile durumunu bildirir (`true`/`false`). Basıldığında kibar bir
  canlı bölge "favorilere eklendi" / "favorilerden çıkarıldı" der.
- **AC-25.** Düğmeye Tab ile ulaşılır, Enter ve Space ile çalışır; odak halkası görünür
  (görsel kanıt). Dokunma hedefi en az 44×44 CSS px'tir.
- **AC-26.** Dolu ve boş hâl renk dışı bir farkla da ayrılır: gri tonlamalı ekran görüntüsünde
  iki hâl ayırt edilir (görsel kanıt; simgenin dolgu/çizgi farkı).
- **AC-27.** Favori ekleme ve çıkarma hiçbir ağ isteği üretmez (e2e: işlem boyunca istek
  sayacı sabit).

### Sayaç (D8 · R2)

- **AC-28.** Üst çubukta, ana sayfa, liste, detay, içerik sayfası, bülten sayfaları, favoriler
  ve karşılaştırma ekranlarında favoriler ekranına götüren sayaç vardır; panelde yoktur.
- **AC-29.** Sayaç sıfırda da görünür. Sayı ilk boyamada doğrudur: üç favoriyle açılan liste
  sayfasında, `addInitScript` ile kurulan bir `MutationObserver` sayacın aldığı her metni
  kaydeder ve kayıt dizisi tam olarak `["3"]`'tür (0 → 3 geçişi yok).
- **AC-30.** Sayacın erişilebilir adı ICU ile kurulur; ar'da 0, 1, 2, 3, 11 ve 100 favori için
  doğru çoğul biçimi okunur (birim testi).
- **AC-31.** Ekleme ve çıkarma sayacı sayfa yenilenmeden günceller.
- **AC-32.** Üst çubuk iki fikstürle, üç dilde, 320 px ve 360 px'te ölçülür. Her ikisinde de
  sayfada yatay kaydırma yoktur (görsel kanıt, tr ve ar).
  (i) **Logosuz, 40 karakterlik ad:** ad kırılabilir (`0026` AC-34 değişmez); web fontu gelirken
  üst çubuğun CLS'i ≤ 0.1.
  (ii) **Logolu (160×40), 40 karakterlik ad, 320 px:** sayaç ve dil değiştirici görünür ve
  tıklanabilirdir; adın görünen kısmı en az 6 karakterdir.

### Favoriler ekranı (D10 · R3, R5, R10, R11)

- **AC-33.** Favoriler son eklenen önce sıralanır.
- **AC-34.** Görünür favori model adını, yılını, kilometreyi, **güncel** fiyatı (eklendikten sonra
  fiyat değiştirilmiş ilanda yeni fiyat), şubeyi ve rezerve ilanda rezerve rozetini gösterir;
  model adı detaya götürür.
- **AC-35.** Ekran her favori için yalnız o favorinin slug'ıyla bir detay okuması yapar; hiçbir
  istek birden çok favori slug'ı taşımaz.
- **AC-36.** Satılan (ya da arşivlenen, yayından kaldırılan) favori "artık vitrinde olmayanlar"
  kümesinde saklanan model adı ve yılıyla, bağlantısız görünür. Ekranı açmak onu kayıttan
  **silmez** (yeniden açınca hâlâ oradadır).
- **AC-37.** Düşen favori tek tek ve "hepsini kaldır" ile kaldırılır; kaldırınca sayaç azalır.
- **AC-38.** Satışı geri alınan ilan (`0023`) ekran yeniden açıldığında görünür favori olarak
  döner.
- **AC-39.** Detay okuması 404 dışı bir hata (500, ağ hatası) dönen favori, kendi satırında hata
  ve "tekrar dene" ile görünür; düşmüş kümede **değildir** ve "tekrar dene" yalnız o satırı
  yeniden okur.
- **AC-40.** Hiç favorisi olmayan ziyaretçi "hiç favori yok" boş durumunu ve vitrine götüren tek
  eylemi görür.
- **AC-41.** Favorilerinin hepsi düşmüş ziyaretçi AC-40'tan **farklı** bir boş durum görür
  (başlık ve metin farklı) ve "hepsini kaldır" eylemini kullanabilir.
- **AC-42.** Bütün okumalar 404 dışı hatayla dönerse ekran hata durumunu ve "tekrar dene"yi
  basar; teknik metin görünmez.
- **AC-43.** Okumalar sürerken iskelet görünür. İskeletten içeriğe geçişte CLS < 0.1: Playwright
  yavaşlatılmış ağla, `PerformanceObserver('layout-shift')` toplamını ölçer (`perf.yml` bu
  ekranı ölçmez).
- **AC-44.** S2 notu ("Favorileriniz bu tarayıcıda saklanır; tarayıcı verisi silinirse
  kaybolur." ve ar/en karşılığı) dolu, boş, hepsi düşmüş ve hata durumlarında görünür; uyarı
  kutusu, kapatma düğmesi veya açılır pencere değildir.
- **AC-45.** Favoriden çıkarma ekrandan öğeyi kaldırır, sayacı azaltır; son favori çıkınca AC-40
  durumu görünür.

### Karşılaştırma seçimi (D6 · R6)

- **AC-46.** Favoriler ekranında her görünür favorinin "karşılaştırmaya seç" denetimi vardır;
  düşmüş ya da hata veren favoride yoktur.
- **AC-47.** Üç seçim varken dördüncü seçim yapılmaz; canlı bölge "en fazla 3 araç" der (ICU).
- **AC-48.** "Karşılaştır" eylemi sıfır ya da bir seçimde çalışmaz (nedeni metinle görünür); iki
  ya da üç seçimde seçimleri ekrandaki sırayla taşıyan `/{locale}/compare?listing=…` adresine
  gider.
- **AC-49.** İlan detayında "Karşılaştır" düğmesi yoktur.

### Karşılaştırma ekranı (D11, D12 · R7, R8, R9, R13, R14)

- **AC-50.** Sütun başı ilk görseli (varsa; `alt` locale'de), detaya giden model adını, rezerve
  rozetini ve favori düğmesini taşır. Favorisi olmayan bir tarayıcıda açılan bağlantı aynı
  araçları aynı sırayla gösterir.
- **AC-51.** Satırlar fiyat, model yılı, kilometre, şube ve detay yanıtındaki niteliklerdir.
  Değerler aynı ilanın detay ekranındaki değerlerle birebir aynıdır (aynı biçimlendirme,
  "Fiyat sorunuz" dâhil).
- **AC-52.** Bir ilanda olmayan nitelik o hücrede "Belirtilmemiş" yazar; hiçbir ilanda olmayan
  nitelik için satır **yoktur**.
- **AC-53.** Kapalı ya da gizli alan detay yanıtında yoktur: `0002`'nin mevcut Host testi
  (AC-12/AC-13) değişmeden yeşildir.
- **AC-54.** Karşılaştırma ekranı yalnız yanıttaki nitelikleri basar (vitest). Yanıtta olmayan
  anahtar için satır yoktur. Karşılaştırma ekranının kaynağında alan ayarı okuması yoktur:
  `field-settings` / alan ayarı tipi grep'i boştur.
- **AC-55.** e2e: panelden bir alan kapatılır → karşılaştırmada o alanın ne satırı ne
  "Belirtilmemiş" hücresi vardır; alan açılınca satır geri gelir.
- **AC-56.** Satır başlıkları detay ekranındaki alan adlarıyla aynıdır; hiçbir satırda başlık
  değerle aynı metin değildir ("Hatchback | Hatchback" yok). Alan adı eşlemesi kodda **tek**
  yerdedir; detay ve karşılaştırma onu içe aktarır.
- **AC-57.** `VitrindeAdiOlanAlanlarTests` `features/catalog/attributeName.ts`'i okur ve yeşildir;
  eşlemeden bir `case` silinince kırılır. İkinci bir `[Fact]`, `case 'body_type':` dizgesinin
  `frontend/src` altında tam olarak **bir** dosyada geçtiğini ölçer; eşleme ikinci bir dosyaya
  kopyalanınca kırılır (iki negatif kanıt PR'da).
- **AC-58.** Parça haritası karşılaştırmada yoktur.
- **AC-59.** Sütunlardan biri 404 dönerse sütun "bu araç artık vitrinde değil" yazar ve hücre
  basmaz; öbür sütunlar karşılaştırılır. Hepsi 404 dönerse "bu bağlantıdaki araçların hiçbiri
  artık vitrinde değil" durumu ve vitrine götüren eylem basılır.
- **AC-60.** Bir sütunun okuması 404 dışı hata dönerse sütun hata ve "tekrar dene" gösterir;
  bütün okumalar hata dönerse ekran hata durumunu basar.
- **AC-61.** Karşılaştırma bir tablodur: araç sütun başlıkları `scope="col"`, alan adları
  `scope="row"`; ekran okuyucu her hücreyi satır ve sütun başlığıyla okur (erişilebilirlik ağacı
  testi).
- **AC-62.** Fikstür: en uzun fiyat (≥ 9 hane + para birimi), en uzun model adı (`#137` sonrası
  sınırlar: ad 80 + 120 karakter, slug ≤ 160), en uzun şube adı; üç dil, tr ve ar için görsel
  kanıt.
  - **360×640:** üç sütun iç kaydırma olmadan görünür (`table.scrollWidth ≤ kap.clientWidth`).
  - **320×640:** sayfada yatay kaydırma yoktur (`document.scrollingElement.scrollWidth ≤
    clientWidth`); tablo gerekiyorsa kendi kabında kayar.
  - **Kayabilen kap:** `tabindex="0"`, `role="region"` ve erişilebilir ad taşır (WCAG 2.1.1).
  - **Uzun belirteçler:** kırılmaz uzun belirteç hücre içinde kırılır.
- **AC-63.** ar'da ilk adres değerinin aracı sağdaki (satır başı) sütundadır; Latin model adı ve
  değerler doğru yönde dizilir (görsel kanıt).
- **AC-64.** Karşılaştırma ekranında favori düğmesi favoriyi ekler/çıkarır ve sayaç güncellenir;
  karşılaştırma adresi değişmez.

### Gizlilik (D15 · C2, S3)

- **AC-65.** Bir e2e oturumu boyunca (favori ekle, çıkar, favoriler ekranı, karşılaştırma) hiçbir
  yazma isteği (POST/PUT/PATCH/DELETE) yapılmaz. Sunucuya giden hiçbir istek birden çok favori
  slug'ını birlikte taşımaz. **Tek istisna** karşılaştırma ekranının belge isteğidir
  (`/{l}/compare?listing=…`): en çok 3 slug taşır (D15, B1). API istekleri tek slug taşır.
- **AC-66.** Bu kartla hiçbir modülde yeni tablo, migration, olay türü veya uç nokta yoktur.
  Ölçü PR'da yazılır: `git diff --stat main...HEAD -- 'src/Modules/**'` yalnız
  `ContentPageContracts.cs`'i (ayrılmış adlar) gösterir; `git diff --stat main...HEAD --
  '**/Migrations/**'` boştur. Architecture.Tests yeşildir.

### Paket ve performans (D13 · C12)

- **AC-67.** İlk paket — giriş JS + giriş CSS + girişin statik içe aktardığı ya da `index.html`'in
  `modulepreload` ettiği her JS, `gzip -9` — `main` tabanına göre en çok **D18'in sınırı**
  kadar büyür (önerilen: 1280 B). Ölçüm PR'ın **head** commit'inde yapılır. Context'teki
  düşürme sırasının her adımı uygulanmış ya da neden uygulanmadığı PR'da yazılıdır.
- **AC-68.** `index.html`'in `modulepreload` listesi ve girişin statik içe aktardığı JS dosyası
  sayısı `main` ile aynıdır; favoriler ve karşılaştırma ekranlarının kodu yalnız tembel
  parça(lar)dadır (derleme çıktısı denetimi CI'da).
- **AC-69.** Favoriler ve karşılaştırma ekranlarının metinleri vitrin kataloğunda (`locales/*.json`)
  **yoktur**; üçüncü kümededir. `check:locales` bu kümeyi dört denetimin her birinde ölçer.
  Her biri için bir negatif kanıt PR'dadır:
  (a) üç dil eşitliği — bir dilden anahtar silinince kırılır;
  (b) Arapça çoğul biçimleri — ar mesajından `one` silinince kırılır;
  (c) başka kümede tekrar yok — aynı anahtar vitrin kümesine de yazılınca kırılır;
  (d) vitrin sınırı — küme modülü vitrinin statik ağacından içe aktarılınca ya da vitrin bir küme
  anahtarını kullanınca kırılır.
- **AC-70.** Favoriler ve karşılaştırma ekranlarının stilleri giriş CSS'inde yoktur.
- **AC-71.** C3 ana sayfa, liste ve detayda yeşildir (LCP < 2500 ms, CLS < 0.1); perf tohumunda
  favori yoktur ve favori düğmesi basılır. Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşu ve
  LCP öğesi PR'da yazılıdır. Favori düğmesi hiçbir sayfada LCP öğesi değildir.

### Çeviri, RTL ve regresyon (C6, C7, C10 · R13)

- **AC-72.** Yeni her anahtar üç dilde vardır; `check:locales`, `check:text`, `check:tokens`
  yeşil. Sayı içeren her cümle ICU'dur.
  Anahtar varlığı kapısı (D19): `frontend/src` altındaki her sabit `t('…')` anahtarı en az bir
  kümede bulunur; şablonlu anahtarın önekiyle başlayan en az bir anahtar vardır. Negatif kanıt:
  olmayan bir anahtar kullanılınca kırılır. Mevcut kodda bulunan eksikler düzeltilir ve PR'da
  listelenir.
- **AC-73.** Yeni CSS'te fiziksel yön özelliği (`left`, `right`, `margin-left` …) ve ham hex/px
  yoktur.
- **AC-74.** Favoriler ekranı ar'da RTL basılır: sıra, simgeler ve "hepsini kaldır" doğru
  taraftadır (görsel kanıt; iki boş durum, dolu hâl, mobil).
- **AC-75.** `0001`, `0002`, `0004`, `0014`, `0015`, `0017`, `0018`, `0020`, `0024`, `0025`,
  `0026`'nın mevcut testleri yeşildir. § Bilerek değişecek eski testler tablosu dışındaki
  testler değişmez. Her Host testi satırının e2e ikizi aranır ve PR'da yazılır. Değişen test varsa PR'da hangi kriterin hangi nedenle güncellendiği yazılıdır
  (beklenen: kart yapısını sayan testler — kartta yeni düğme).

---

## Uygulama sırası (öneri)

1. Ayrılmış adlar, `SeoPage` türleri, belge etiketleri, `no-referrer` (AC-1–AC-7).
2. Depo katmanı ve kayıt biçimi (AC-12–AC-20).
3. Favori düğmesi ve sayaç; ilk paket ölçümü (AC-21–AC-32, AC-67).
4. Tembel katalog kümesi, dört denetim ve anahtar varlığı kapısı (AC-69, AC-72).
5. Favoriler ekranı ve seçim (AC-33–AC-49).
6. Alan adı eşlemesinin taşınması, kapı yolu; karşılaştırma ekranı (AC-50–AC-64).
7. Gizlilik e2e, perf koşusu, görsel kanıt (AC-65, AC-66, AC-68, AC-70, AC-71, AC-74).
8. Belgeler (D16, D17).

---

## Definition of Done

- [x] Açık sorular karara bağlandı; **S5 kodda doğrulandı** ve sonucu Context'e yazıldı.
- [x] Proje Yöneticisi D1–D17'yi onayladı; QA CLARIFY turu işlendi (B1–B17, 2026-11-26).
- [ ] D18 (ilk paket bütçesi) karara bağlandı.
- [ ] `docs/deploy.md`'ye ters vekil/CDN erişim günlüğü notu düşüldü (D17, B2).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/frontend-003-ac<n>.png`); iki boş durum, mobil
      görünüm ve Arapça (RTL) görünüm dâhil.
- [ ] `localStorage` erişilemezken arayüzün çalıştığı testle gösterildi (C9, AC-15, AC-16).
- [ ] Favori verisinin sunucuya **gitmediği** doğrulandı (C2, AC-27, AC-65).
- [ ] Kapalı ve gizli alanların karşılaştırmada bulunmadığı testle gösterildi (R8, C3, AC-53).
- [ ] İlk paket ölçümü head commit'te yapıldı ve PR'da yazılı (AC-67).
- [ ] Bilerek değişen eski testler tablodakilerle sınırlı; her değişiklik PR'da kriteriyle yazılı (AC-75).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor. (Bu kartta migration yok — AC-66.)
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (D16).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (D17; ADR `0013` eki).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD) ve spec `specs/done/` klasörüne taşındı.

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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0001`/`0002`'yi bozup bozmadığı burada görünür. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
