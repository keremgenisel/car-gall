# 0026 — Galeri logosu

| | |
|---|---|
| **Durum** | **Onaya sunuldu (2026-11-26)** · D1–D15 onaylandı (Proje Yöneticisi kararı, 2026-11-26) · sahibin SS1–SS2 cevapları işlendi · QA 1 CLARIFY turu bekliyor |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Media` (logo işleme türü) · `Dealership` (galeri profilinin logo bağı) · `Host` (belge: üst çubuk logosu, `og:image`, `AutoDealer.logo`) · Frontend (panel Galeri ekranı, vitrin üst çubuğu) |
| **Bağımlı spec'ler** | `0011` (yükleme, türev, filigran) · `0012a` (panelin medya yüzeyi) · `0004` (**D2**, **D13**, **D15**, **D16**, **D17**, **AC-32**, **AC-33**, **AC-52**, **AC-80**, **AC-83**) · `0020` (galeri profili, **R16** sürüm, **C6** üst çubuk) · **`0024`** (profil formunun sürümü) · **`0025`** (`AC-52` zinciri). |
| **Karar bekliyor** | yok — D1–D15 onaylandı (Proje Yöneticisi kararı, 2026-11-26). SS1–SS2 cevaplandı (sahip kararı, 2026-11-26). |

> **Sahibin isteği (sahip kararı, 2026-11-26):** `0004` **SS5**'e "evet": logo panelden yüklensin,
> paylaşım önizlemesinde ve galerinin yapısal verisinde görünsün; `0004`'ten sonra **ayrı küçük
> kart**. `0004` **X9**, `0014` **X9** ve `0020` **X1** logoyu bu karta bırakmıştı.
>
> **Sahibin cevapları (sahip kararı, 2026-11-26):** **SS1 → üst çubuk + ana sayfa altlığı (öneriden
> farklı).** SS2 → adın yanında, ad metin olarak kalır (öneri kabul). SS1'in cevabıyla **R4a**
> eklendi, **D5** altlığı kapsayacak biçimde genişledi, **X2** ve **D11** değişti, **AC-58** yerinde
> değişti. Yeni kriterler numara **eklenerek** geldi (**AC-62–AC-68**), var olan numaralar değişmedi.

---

## Intent

Galeri yöneticisi markasının vitrinde görünmesini istiyor. Bugün her sayfanın üst çubuğunda
yalnız galerinin adı yazı olarak duruyor. Ana sayfa, araç listesi ya da bir içerik sayfası sosyal
ağda paylaşıldığında önizleme görselsiz çıkıyor. Arama motoru da galerinin logosunu bilmiyor. Bu
iş bittiğinde yönetici logoyu panelden yükler, değiştirir ya da kaldırır. Logo işlendikten sonra
her vitrin sayfasının üst çubuğunda adın yanında görünür. Kendi görseli olmayan sayfalar
paylaşıldığında önizlemede logo çıkar ve galerinin yapısal verisi logoyu taşır. Sayfa hızı eşiğin
altında kalır.

---

## Requirements

- **R1.** Yönetici galeri profiline bir logo yükler, logoyu değiştirir ya da kaldırır.
- **R2.** Logo araç fotoğraflarından farklı işlenir: üzerine filigran basılmaz ve saydam zemini
  saydam kalır. Araç fotoğraflarının işlenmesi değişmez.
- **R3.** Logonun işlenme durumu (işleniyor, hazır, başarısız) panelde görünür. Başarısız logo
  yeniden denenebilir.
- **R4.** Hazır logo her vitrin sayfasının üst çubuğunda, galerinin adının yanında görünür. İlk
  boyamada yerini alır ve sayfayı kaydırmaz.
- **R4a.** Hazır logo ana sayfanın altlığında da görünür (**SS1**, sahip kararı, 2026-11-26).
- **R5.** Hazır logo, kendi görseli olmayan belgelerin paylaşım önizlemesinde görsel olarak kullanılır.
- **R6.** Ana sayfanın yapısal verisi hazır logoyu taşır.
- **R7.** Logo atanmamışken, hazır değilken ya da okunamadığında vitrin bugünkü gibi davranır.
- **R8.** Logoyu yalnız **Yönetici** atar ve kaldırır. Satış Danışmanı ve İçerik Editörü panel
  adresini doğrudan açtıklarında "erişim yok" görürler.
- **R9.** Logo ana sayfa, liste ve detayın yükleme hızını eşiğin üstüne çıkarmaz.
- **R10.** Panel bölümü TR/AR/EN'de çevrilidir. Üst çubukta logo Arapçada adın sağında durur.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Dealership` başka bir modüle senkron çağrı yapmaz. → Yasak Listesi 32,
  `docs/architecture.md:254`. Logonun türevlerini `Dealership` okumaz (**D3**).
- **C2.** Senkron okuma § 2.A'nın üç koşuluna uyar: salt okuma, aynı istek, tek atlama. Host
  yalnız `*.Contracts`'ı birleştirir.
- **C3.** SVG kabul edilmez. Kabul edilen türler `Media`'nın yapılandırılmış kümesidir (`jpeg`,
  `png`, `webp`; `0011` **D23**). Orijinalin yolu hiçbir yanıtta dönmez (`0011` **D16**).
- **C4.** `Media` sahipsizdir: "bu varlık kime ait" bilgisi `Media`'da yoktur (`0011` **R10**).
  İşleme türü "nasıl işlenir"dir, sahiplik değildir (**D1**).
- **C5.** Mutlak adresler `0004` **D2**'nin köküyle kurulur. Yapısal veri ve öznitelikler `0004`
  **D16**'ya göre kodlanır. Tam SSR yoktur (`0004` **C1**).
- **C6.** Üst çubuk adı ilk boyamada, ayrı istek olmadan alır (`0020` **C6**, **D16**). Logo da
  aynı yoldan gelir ve yer kaydırmaz (CLS).
- **C7.** Performans: C3 ana sayfa, liste ve detayda yeşil kalır (LCP < 2500 ms, CLS < 0,1).
  Detayın ilk görsel ön yüklemesi bütün etiketlerden önce kalır (`0004` AC-80, `0016b` **C13**).
  İlk paket ADR 0013'ün protokolüyle ölçülür.
- **C8.** Her modülün migration'ı yalnız kendi şemasına dokunur.
- **C9.** Logo işlemi galeri profilinin eşzamanlı yazma korumasına (`0020` **R16**) tabidir.

### KAPSAM DIŞI

- **X1.** **Panelin kendi üst çubuğunda logo** — *neden:* `0020` **X7**. İstek vitrin içindi.
- **X2.** **Altlığın, dolayısıyla altlık logosunun, ana sayfa dışındaki sayfalara taşınması** —
  *neden:* altlık yalnız ana sayfada var (`frontend/src/app/App.tsx:133`, `0024` X4). Sahip altlıkta
  logo istedi (**SS1**), site geneli altlık istemedi.
- **X3.** **Site simgesi (favicon) ve uygulama simgesi üretimi** — *neden:* ayrı bir türev kümesi
  ve belge etiketleri gerektiriyor. Ham istekte yok.
- **X4.** **Logonun filigran olarak kullanılması** — *neden:* filigran dağıtım yapılandırmasından
  geliyor (`MediaOptions.cs:36`, `WatermarkObjectKey`). İkisini birleştirmek filigranın satın
  alınma gerekçesini değiştirir (`MediaOptions.cs:11-13`).
- **X5.** **Açık ve koyu zemin için iki ayrı logo, dil başına logo** — *neden:* kurulumun tek bir
  teması var (`frontend-002`). Ham istekte yok.
- **X6.** **SVG logo** — *neden:* C3.
- **X7.** **Panelde kırpma ya da düzenleme aracı** — *neden:* yönetici dosyayı hazırlayıp yükler.
- **X8.** **E-posta şablonlarında logo** (`Notifications`) — *neden:* ayrı bir yüzey. Ham istekte yok.
- **X9.** **`og:image:width`, `og:image:height`, `og:image:alt`** — *neden:* `0004` **D13**'ün
  kümesi değişmez. Bu kart yalnız `og:image`'in değerini genişletir.
- **X10.** **Kaldırılan ya da değiştirilen logonun depodan silinmesi** — *neden:* `Media`'da silme
  yolu yok. Sahipsiz varlıkların temizliği ayrı bir iş (**D8**'in bedeli).
- **X11.** **Logonun yükleme yetkisinin Yönetici ile sınırlanması** — *neden:* **D4**.

---

## Context

> Her iddia `main` @ `bcbe520` üzerinde ölçüldü. Satır numaraları o commit'e aittir. Bu kart
> `0024` ve `0025` merge edildikten sonra uygulanır. O kartların değiştirdiği yerler aşağıda
> ayrıca işaretli.

### Bugün galeri profilinde logo

- **Alan var, yolu yok.** `Gallery.LogoMediaAssetId` (`src/Modules/Dealership/Dealership/Domain/Gallery.cs:35-36`,
  "yalnızca referans; görselin kendisi `Media` spec'ine aittir") ve `SetLogo` (`:145`) duruyor.
  Kolon kurulu (`logo_media_asset_id`, `InitialDealership.cs:48`, `GalleryConfiguration.cs:18`).
  `src/` ve `tests/` altında `SetLogo`'yu çağıran **hiçbir yer yok**. Kaldıran bir yöntem de yok.
  → `Dealership`'te migration beklenmiyor (**D2**).
- Panel okuması (`AdminGalleryDto`, `Application/DealershipAdminQueries.cs:11-16`) logo taşımıyor.
  Ziyaretçiye dönük okuma da taşımıyor. Belge yorumu "logo kimliği … burada YOKTUR" diyor
  (`PublicGalleryContracts.cs:4-8`). Bu kart o okumaya **dokunmaz** (**D3**).

### Bugün `Media`'nın işleme hattı — logoya olduğu gibi uygulanamaz

- **Her türeve filigran basılıyor.** `SkiaImageProcessor.Render(source, watermark)`
  (`src/Modules/Media/Media/Infrastructure/SkiaImageProcessor.cs:66-126`) üç türevin her birinde
  tuvali **siyaha boyuyor** (`:110`, `tuval.Clear(SKColors.Black)`), görseli çiziyor ve **filigranı
  basıyor** (`:112`). Filigran türev genişliğinin beşte biri (`:35`). Bir türü ayırt eden bir
  parametre yok.
- **Sonuç:** saydam zeminli bir PNG logo bugünkü hattan **siyah bir kutu içinde ve üzerinde
  galerinin filigranıyla** çıkar. Proje Yöneticisi'nin taslağı "Media'nın yükleme/türev
  altyapısı" diyordu. Altyapı yükleme, durum, tekrar deneme ve türev adresleri için kullanılabilir
  ama işleme adımı logo için ayrı bir tür ister (**D1**).
- **Türevler sabit:** `large` 1600, `display` 800, `thumbnail` 400 (`Domain/MediaVariant.cs`). Kaynak
  hedeften küçükse büyütülmüyor (`SkiaImageProcessor.cs:99-104`). WebP, kalite 82 (`:40`, `:115`).
- **Yükleme ucu** `POST /api/admin/media/assets` (`Endpoints/MediaAdminEndpoints.cs:40-48`)
  `AuthenticatedPolicy` taşıyor (`:42`): her oturumlu personel yükleyebilir. Durum okuması
  `GET …/{id}` (`:50`), tekrar deneme `POST …/{id}/retry` (`:74`). **Silme ucu yok.**
- **Okuma sözleşmesi:** `MediaAssetDto(Id, Status, FailureReason, AltText, Derivatives)`
  (`Media.Contracts/Assets/MediaAssetContracts.cs`) türü taşımıyor. `IMediaAssetQueries.FindAsync`
  tek varlık okuyor.
- **Duyuru:** `MediaAssetReady`'nin tek tüketicisi `VehicleCatalog`
  (`docs/architecture.md:158`). Alıcı (`VehicleCatalog/Application/EventHandlers/MediaAssetReadyHandler.cs`)
  varlığın kimliğiyle eşleşen yuvaları güncelliyor. Yuva yoksa hiçbir satıra dokunmuyor. Yani
  logo varlığının duyurusu ilanlara sızmaz (**AC-8** bunu ölçer).

### Mimari: `Dealership` → `Media` yasak

- `docs/architecture.md:254` ve Yasak Listesi 32: `Dealership` başka bir modüle senkron çağrı
  yapmaz. Bağımlılık haritasında `Media`'yı okuyan tek modül `VehicleCatalog` (`:115`).
- **Sonuç:** `Dealership` logonun türevlerini ya da hazır olup olmadığını okuyamaz. Okuyan Host
  olur: Host'un kompozisyon kökü olarak `*.Contracts`'ı birleştirmesi zaten belgenin
  kuruluşudur (`SeoHeadBuilder`, `IndexHtmlRenderer`). → **D3**.

### Bugün vitrin

- **Üst çubuk** (`frontend/src/app/TopBar.tsx:19-37`): ad, sunucunun bastığı
  `<meta name="gallery-display-name">`'den ilk boyamada okunuyor (`shared/lib/galleryName.ts:11`).
  Ad okunamazsa bağlantısız boş bir yer basılıyor (`:26-27`, `0020` AC-17). Ad `<bdi>` içinde.
  Etiketi `IndexHtmlRenderer.cs:88-96` basıyor. → **D5**: logo aynı yoldan gelir.
- **`og:image`** (`src/Host/Seo/SeoHeadBuilder.cs`): ana sayfada `null` (`:98`), listede `null`
  (`:145`, `:151`), içerikte `null` (`:289`). Detayda ilk görselin büyük türevi (`:200`, `:211`).
  Görselsiz ilanda yok. Göreli adres kökle tamamlanıyor (`:373-376`). 404, bülten ve panel
  belgeleri Open Graph taşımıyor (`:72-73`, `:80-81`).
- **`AutoDealer`** (`SeoHeadBuilder.cs:100-123`) yalnız ana sayfada.
- **Ön yükleme önce gelir** (`SeoHeadBuilder.cs:24-27`, `0004` AC-80).
- **Performans tabanı:** `0004`'ün son koşusunda C3 ana sayfa 2277, liste 2125, detay **2408** ms
  (PR #126). Detayın payı 92 ms ve koşular arası gürültü yaklaşık 80 ms. Logo **her sayfaya**
  girdiği için bu kartın en dar yeri detaydır (**D11**).
- **Perf tohumu `Media`'da kayıtlı değil:** tohumun görselleri deterministik kimliklerle
  `SeedAwareMediaAssetQueries`'ten geliyor (`VehicleCatalog/Application/Development/SeedMediaAssets.cs:83-95`).
  O sınıf, tohum bayrağı açıkken `IMediaAssetQueries`'in önüne konuyor (`VehicleCatalogModule.cs:197`).
  Host'un logo okuması da aynı arayüzden geçtiği için, logolu perf ölçümünün tohum logosu aynı
  desenle verilebilir (**AC-59**).

### `0024` ve `0025`'in bu karta bıraktığı hâl

- **Profilin sürümü:** `0024` **D4** sosyal hesapları profilin kaydetmesine bağladı. Bu kartın
  logo işlemi sürümü artırır (**D8**). Panelde logo işleminden sonra aynı ekrandaki profil ve
  sosyal hesap kaydetmesi `409` vermemelidir (**AC-25**).
- **`0004` AC-52 zinciri:** `0025`'ten sonra yokluk listesi `geo`, `logo`. Bu kart `logo`'yu
  çıkarır (**AC-52**). Son hâl: yalnız `geo` yoktur (`0004` **X4**, koordinat vitrinde gösterilmiyor).

### Bilerek değişecek eski testler (kod taranarak çıkarıldı)

| Test | Bugün ölçtüğü | Bu kartla |
|---|---|---|
| `tests/Host.Tests/SeoBelgeTests.cs:373` (`0004/AC-32`) | görselsiz ilanda `og:image` yok | logolu kurulumda logo (**AC-47**). Logosuz kurulumda değişmez (**AC-48**) |
| `tests/Host.Tests/SeoBelgeTests.cs:381` (`0004/AC-33`) | ana sayfa, liste, içerikte `og:image` yok | logolu kurulumda logo (**AC-44**, **AC-45**). Logosuz kurulumda değişmez (**AC-48**) |
| `tests/Host.Tests/SeoBelgeTests.cs:561-573` (`0004/AC-52`, `0025` AC-55 hâli) | `geo`, `logo` yok | `logo` listeden çıkar (**AC-52**) |
| `tests/VehicleCatalog.Tests/Integration/FakeMediaAssetQueries.cs:25`, `SeedMediaAssets.cs:51` | `MediaAssetDto` beş parametreli kurucu | sözleşmeye tür eklenince kurucu değişir (**D3**). Fotoğraf türü beklenen değeri değiştirmez |
| `0011`'in işleme testleri (filigran ve türev testleri, `tests/Media.Tests`) | türsüz yükleme | **değişmez**: türsüz yükleme fotoğraftır (**D1**, **AC-6**) |
| `frontend/src/app/TopBar.test.tsx`, `tests/Host.Tests/GorunenAdEtiketiTests.cs` | logosuz üst çubuk ve ad etiketi | **değişmez** (logosuz kurulum, **AC-34**). Logolu durum yeni testlerle ölçülür |

### Bilinen tuzaklar

1. **Siyah zemin** (`SkiaImageProcessor.cs:110`): saydamlığı ölçmeyen bir test logonun siyah
   kutuda çıktığını görmez. **AC-4** bir pikselin saydamlığını ölçer.
2. **Atanmış ama hazır olmayan kimlik.** `Dealership` hazır olup olmadığını bilemez (C1). Host
   kontrol etmezse üst çubuk işlenmekte olan bir varlığın olmayan türevine istek atar.
3. **Fotoğraf türündeki varlığın logo diye atanması.** El yapımı bir istekle filigranlı bir araç
   fotoğrafının kimliği atanabilir. Host türü kontrol eder (**D3**, **AC-36**).
4. **Logoyu ön yüklemek detayın LCP'sini geciktirir.** Detayın LCP öğesi ilk görseldir ve keşfi
   bayt sırasına bağlıdır (`0004` **D17**). Logo ön yüklenmez ve düşük öncelikle istenir (**D5**).
5. **Kapsayıcısı ölçüsüz görsel yer kaydırır.** Logo `width`/`height` özniteliği olmadan basılırsa
   görsel geldiğinde ad yana kayar. CLS her sayfada ölçülür (`0014` performans kapısı).

---

## Verilen kararlar (öneri — Takım Yöneticisi onayı bekliyor)

Biçim: **karar** · *gerekçe* · *bedel*.

**D1 — `Media`'ya bir işleme türü: "logo".** (R2, C4)
- Yükleme isteği türü seçer. Tür verilmezse varlık **fotoğraftır** ve bugünkü gibi işlenir:
  üç türev, filigran, siyah zemin (C8, **AC-6**).
- **Logo türü filigran basmaz ve saydam pikselleri saydam bırakır.** İki türev üretir: `logo-small`
  (hedef genişlik 320) ve `logo-large` (hedef genişlik 1200). Çıktı WebP'dir (alfa kanallı).
- Büyütme ve EXIF yönlendirmesi fotoğrafla aynıdır (kaynak hedeften küçükse büyütülmez).
  Konum ve metadata temizliği de fotoğrafla aynıdır (`0011` **R5**).
- Tür varlıkla saklanır: `media` şemasında migration.
- Tür okuma sözleşmesinde (`MediaAssetDto`) ve panelin durum okumasında görünür.

*Gerekçe:* Context: bugünkü hat logoyu siyah kutuya koyup üzerine filigran basıyor. Tür,
varlığın sahibini değil nasıl işleneceğini söyler, yani `Media` sahipsiz kalır (C4). 320,
üst çubuğun en çok 160 CSS piksellik logosunun iki kat yoğunluklu ekrandaki karşılığıdır (D5).
1200 paylaşım önizlemesinin önerilen genişliğidir.
*Bedel:* `media` şemasında migration. İşleme hattı türe göre dallanır.

**D2 — Logonun bağı `Dealership`'tedir. Mevcut alan kullanılır.** (R1, C1) Panel önce dosyayı
logo türüyle `Media`'ya yükler (`202`), sonra dönen kimliği galeri profiline bağlar. `Dealership`
kimliğin var olduğunu, hazır olduğunu ya da türünü **doğrulamaz** (C1). Bu kontroller vitrinde,
Host'ta yapılır (D3). `Dealership`'te migration yoktur.
*Gerekçe:* alan ve kolon zaten var (Context). Doğrulama için `Media`'yı çağırmak Yasak Listesi 32'dir.
*Bedel:* el yapımı bir istekle var olmayan ya da yanlış türde bir kimlik bağlanabilir. Sonucu
"logo yok" davranışıdır (R7).

**D3 — Vitrinde logoyu Host çözer.** (R4–R7, C2, tuzak 2, tuzak 3)
- Belge isteğinde Host, galerinin logo kimliğini `Dealership.Contracts`'tan, görünen adla **aynı
  okumada** alır. Türevleri `Media.Contracts`'tan (`IMediaAssetQueries.FindAsync`) okur. İki okuma
  da tek atlamalıdır.
- Logo yalnız varlık **Hazır** ve türü **logo** ise kullanılır. Aksi hâlde logo yoktur.
- `Media` okuması hata verirse belge 200 ile logosuz basılır ve hata loglanır (`0020` **R5a** deseni).
- Logo atanmamışsa `Media`'ya okuma yapılmaz. Atanmışsa belge başına tam olarak bir ek okuma yapılır.
- Ziyaretçiye dönük `/api/dealership/gallery` okuması değişmez.

*Gerekçe:* C1 ve C2'nin birlikte izin verdiği tek yol bu. Görünen adla aynı okuma `Dealership`'e
ek bir sorgu eklemez.
*Bedel:* logolu kurulumda her vitrin belgesine bir okuma eklenir.

**D4 — Yükleme yetkisi değişmez. Bağlama yalnız Yönetici'dir.** (R8, X11) Logo türüyle yükleme
`Media`'nın bugünkü ucundan yapılır ve her oturumlu personele açıktır. Atama ve kaldırma galeri
yönetim grubundadır (Yönetici politikası, `0020` **D2**). Satış Danışmanı ve İçerik Editörü `403`,
oturumsuz istek `401` alır.
*Gerekçe:* varlık sahipsizdir ve bağlanmadıkça vitrinde görünmez. Yükleme ucuna türe göre ikinci
bir politika eklemek `Media`'ya "kim için" bilgisini sokar (C4).
*Bedel:* Satış Danışmanı logo türünde bir varlık yükleyebilir ama atayamaz (**AC-18**).

**D5 — Üst çubuk: logo adın yanında, yer ayrılmış ve düşük öncelikli.** (R4, R10, C6, C7,
tuzak 4, tuzak 5, **SS1**, **SS2**)
- Logo adın satır başı tarafındadır: tr/en'de solunda, ar'da sağında. Ad metin olarak kalır.
- Logo dekoratiftir (boş `alt`). Bağlantının erişilebilir adı görünen addır.
- Logo `logo-small` türevidir. `width` ve `height` öznitelikleri türevin gerçek boyutudur. Görünen
  boyut oran korunarak **en çok 40 CSS piksel yükseklik ve 160 CSS piksel genişlik**tir.
- Logo ön yüklenmez. Düşük getirme önceliği ve eşzamansız çözümleme taşır.
- Bilgi (adres, genişlik, yükseklik) belgeye sunucudan, görünen adın etiketi gibi basılır. Ayrı
  istek yoktur.
- **Ad okunamazsa logo da gösterilmez:** üst çubuk bugünkü boş hâliyle kalır (`0020` AC-17).
- Logo; ana sayfa, liste, detay, içerik ve 404 belgelerinin üst çubuğunda bulunur. Bülten teyit
  ve çıkış sayfaları da aynı kabuğu kullanır.
- **Altlık logosu** *(SS1'in cevabıyla eklendi, sahip kararı, 2026-11-26)*:
  - **Yer:** altlık yalnız ana sayfada var (`App.tsx:133`). Logo altlığın **başında** durur,
    şube bloğundan önce, satır başına (tr/en solda, ar sağda) yaslı.
  - **Görsel:** üst çubukla **aynı** `logo-small` türevi ve aynı adres. Tarayıcı onu ikinci kez
    indirmez. Boyutu üst çubukla aynıdır (oran korunarak en çok 40 × 160 CSS piksel). `width` ve
    `height` türevin gerçek boyutudur.
  - **`alt`:** üst çubuktakinden **farklı**: altlıktaki logonun yanında ad yazmaz, bu yüzden logo
    bilgi taşır ve `alt`'ı **görünen addır**. Bağlantı değildir: ana sayfadadır ve üst çubuğun
    bağlantısı zaten ana sayfaya gider.
  - **Hız:** `loading="lazy"` ve eşzamansız çözümleme taşır. Ön yükleme ve yüksek öncelik yoktur.
    Katlamanın altındadır ve LCP adayı değildir.
  - **Bilgi:** altlık üst çubuğun belgeye basılmış logo bilgisini okur. Belgeye ek bayt ve ayrı
    istek eklenmez.
  - Üst çubukta logo yoksa (atanmamış, hazır değil, ad okunamıyor) altlıkta da yoktur. Altlığın
    diğer blokları değişmez.

*Gerekçe:* ad Arapça ziyaretçi, ekran okuyucu ve arama motoru için metin olarak kalmalı. Öznitelik
yer ayırır, yani CLS olmaz. Düşük öncelik LCP görseliyle bant genişliği yarışını önler. Altlıkta aynı
türevi kullanmak ikinci bir indirmeyi önler. `alt` kuralı WCAG'nin "bilgi taşıyan görsel /
dekoratif görsel" ayrımıdır: üst çubukta ad hemen yanında yazılı, altlıkta değil.
*Bedel:* çok geniş bir logo 160 pikselde küçük görünür. Panelin yardım metni oranı önerir (D12).

**D6 — `og:image` yedeği logodur.** (R5, X9) `logo-large`'ın mutlak adresi şu belgelerde
`og:image` olur:
- ana sayfa,
- liste (süzgeçli, `noindex` olan liste dâhil),
- içerik,
- **görselsiz** ilanın detayı.

Görselli ilanın detayı ilk görseli kullanır (`0004` **D13** sürer). 404, bülten ve panel belgeleri
`og:image` taşımaz (`0004` **D7**). Logo yoksa `0004` AC-32 ve AC-33 aynen geçerlidir.
*Gerekçe:* sahibin SS5 isteği. Paylaşılan her vitrin sayfası görselli önizleme verir.
*Bedel:* saydam logo bazı platformlarda siyah ya da beyaz zeminde görünür. 1200'den dar
logolarda önizleme küçük kalır.

**D7 — `AutoDealer.logo`.** (R6) Kök `AutoDealer` nesnesinde `logo`, `logo-large`'ın mutlak
adresidir (metin olarak). Yalnız ana sayfada bulunur ve `department` öğelerinde yoktur. Logo
yoksa anahtar yoktur. Görünen ad okunamazsa blok hiç basılmaz (`0004` **D15**). Bu kartla `0004`
**AC-52**'nin yokluk listesi yalnız `geo` olur.
*Gerekçe:* `0004` **D15**'in notu. Metin adres `ImageObject`'ten az bayt harcar ve geçerlidir.
*Bedel:* yok.

**D8 — Panel: profil ekranında "Logo" bölümü.** (R1, R3, C9)
- **Logo yokken:** "Logo yükle" dosya seçicisi gösterilir. Seçici yalnız kabul edilen türleri önerir.
- Dosya seçilince yükleme başlar. Yükleme kabul edilince (`202`) kimlik **hemen** profile bağlanır
  ve sonucun hazır olması beklenmez.
- **İşlenirken:** "işleniyor" durumu görünür ve bölüm durumu kendiliğinden yeniler.
- **Hazır olunca:** önizleme (`logo-small`) ile "Değiştir" ve "Kaldır" görünür.
- **İşleme başarısız olursa:** çevrilmiş neden ve "Tekrar dene" görünür (`0011`'in tekrar deneme ucu).
- **Kaldırmak** bağı siler, `Media`'daki varlığı silmez (X10).
- Atama ve kaldırma okunan profil sürümüyle gönderilir ve sürümü bir artırır. Eski sürüm `409`
  döner. Ekran dönen yeni sürümü benimser: aynı ekrandaki profil ve sosyal hesap kaydetmesi `409`
  vermez ve kaydedilmemiş form değerleri kaybolmaz.

*Gerekçe:* hemen bağlamak, işleme sürerken ekrandan çıkan yöneticinin logosunu kaybetmemesini
sağlar. Sürümü benimsemek güvenlidir: işlem başarılıysa aradan başka bir yazma geçmemiştir.
*Bedel:* logo değiştirilirken birkaç saniye vitrin logosuz kalır. İşleme başarısız olursa
tekrar deneme ya da yeni yüklemeye kadar logosuz kalır. Kaldırılan varlıklar depoda birikir (X10).

**D9 — Atama ve kaldırma uçları.** (R1, R8) Galeri yönetim grubunda: logo atama (gövde: varlık
kimliği ve sürüm) ve logo kaldırma (sürüm). İkisi de yeni sürümü döner. Panelin profil okuması
logo kimliğini taşır (yoksa `null`). Panel durumu ve önizlemeyi `Media`'nın bugünkü okuma
ucundan alır.
*Gerekçe:* logo profil formunun metin alanlarından ayrı bir eylem. Dosya seçilince kaydetme
düğmesini beklemeden uygulanır (D8).
*Bedel:* yok.

**D10 — Kodlama.** (C5) Logo adresi öznitelikte bugünkü öznitelik kodlamasıyla, JSON-LD'de `0004`
**D16** ile basılır. Göreli adres `0004` **D2**'nin köküyle tamamlanır. Mutlak adres olduğu gibi
kalır (`SeoHeadBuilder.cs:373-376`).
*Gerekçe:* adres yapılandırmadan (`Media:PublicBaseUrl`) gelir ve kurulumcu yazar.
*Bedel:* yok.

**D11 — Bütçe ve hız.** (R9, C7)
- **(a)** `/ar/` ana sayfa belgesinde bu kartın eklediği bayt (üst çubuk logo bilgisi, `og:image`,
  `AutoDealer.logo`) test yapılandırmasının medya köküyle **en çok 768 B**'tır. *(SS1'den sonra
  değişmedi: altlık logosu belgeye bayt eklemez, üst çubuğun bilgisini okur.)*
- **(b)** `0004` AC-83'ün kurgusu logolu kurulumda da en çok 6144 B'tır. Görselli detayın Open
  Graph kümesi değişmez.
- **(c)** İlk paket ADR 0013'ün protokolüyle `main` tabanına göre **en çok 512 B** büyür. Üst çubuk
  ve altlık logosunun kodu bu payın **içindedir**.
- **(d)** C3 ana sayfa, liste ve detayda **logolu kurulumda** yeşildir (LCP < 2500 ms, CLS < 0,1).
  Perf koşusu bir logo tohumlar. Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşu istenir ve
  LCP öğesi PR'a yazılır.
- **(e)** Üç sayfanın hiçbirinde LCP öğesi logo değildir: ne üst çubuk logosu ne altlık logosu.
- **(f)** Logolu ana sayfa, logonun ağ isteğini bir kez yapar. Altlık logosu ikinci bir istek üretmez.

*Gerekçe:* logo her sayfaya giriyor ve detayın payı 92 ms (Context). (e) logonun LCP adayını
değiştirmediğini doğrudan ölçer.
*Bedel:* yok.

**D12 — Dosya kuralları `Media`'nın bugünkü yapılandırmasıdır.** (C3) Kabul edilen türler, en
büyük boyut (15 MB) ve piksel sınırı (50 MP; `src/Host/appsettings.json:77-78`) logoya da
uygulanır. Logoya özgü bir alt sınır yoktur. Panelin yardım metni "Saydam zeminli PNG, en az 640
piksel genişlik önerilir" der.
*Gerekçe:* yeni bir doğrulama kuralı yeni hata kodları ve kriterler demek. Öneri metni yeterli.
*Bedel:* çok küçük bir logo bulanık görünür.

**D13 — Hata yolları panelde çevrilir.** (R3) Reddedilen dosyanın (`415`, `422`) nedeni ve işleme
başarısızlığının nedeni (`MediaFailureReason`) `0012a`'nın metinleriyle gösterilir. Reddedilen
yüklemede bağ değişmez.
*Gerekçe:* aynı neden kümesi araç görselinde zaten çevrili.
*Bedel:* yok.

**D14 — Migration'lar.** (C8) `media` şemasında tür alanı (D1). `dealership` şemasında migration
yoktur (D2).
*Gerekçe:* C8.
*Bedel:* yok.

**D15 — Yeni domain terimi: "Galeri Logosu".** (DoD T7) Galeri profiline bağlı, logo türünde
işlenmiş medya varlığı. Filigransızdır ve saydam zemini korunur. Vitrinin üst çubuğunda ve
paylaşım önizlemesinde kullanılır.
*Gerekçe:* sözlükte yok.
*Bedel:* yok.

---

## Sahibe sorular

> **Cevaplandı (sahip kararı, 2026-11-26).** SS1 öneriden farklı, SS2 öneriyle aynı.

- **SS1. Logo vitrinde nerede görünsün?**
  **Öneri:** her sayfanın üst çubuğunda (D5). Altlıkta tekrar etmesin (X2). Altlık yalnız ana
  sayfada var ve logo zaten üstte görünüyor.
  **Cevap (sahip kararı, 2026-11-26): üst çubuk + sayfa altı (altlık).** → R4a, D5, D11, X2.
- **SS2. Logo adın yerine mi, yanında mı?**
  **Öneri:** yanında, ad yazı olarak kalsın (D5). Logo çoğunlukla Latin harfli. Arapça sayfada ad
  Arapça okunur, ekran okuyucu ve arama motoru adı yazıdan alır. Ad yerine yalnız logo istenirse
  logonun alternatif metni görünen ad olur ve AC-30 değişir.
  **Cevap (sahip kararı, 2026-11-26): öneri kabul — yanında.**

---

## Acceptance Criteria

> Panel kriterleri Yönetici oturumuyla, aksi yazılmadıkça varsayılan kurulumda (tr/ar/en) koşar.
> "Hazır logo" logo türüyle yüklenmiş, işlenmesi bitmiş ve galeriye atanmış varlıktır. Belge
> kriterleri sunucu yanıtıyla, JavaScript çalıştırılmadan ölçülür (`0004` C13).

### Logo işleme türü (R2 · C3, C4 · D1)

- **AC-1.** Logo türüyle yüklenen bir PNG'nin hazır varlığı tam olarak iki türev taşır: `logo-small`
  ve `logo-large`.
- **AC-2.** 2000 piksel genişlikteki kaynaktan `logo-small` 320, `logo-large` 1200 piksel
  genişliktedir. 200 piksel genişlikteki kaynaktan ikisi de 200 piksel genişliktedir. Oran korunur.
- **AC-3.** Logo türevlerinde filigran yoktur. Türev, aynı kaynağın filigransız ölçeklenmiş hâline
  kayıplı sıkıştırma toleransı içinde eşittir.
- **AC-4.** Kaynakta saydam (alfa 0) olan köşe pikseli iki türevde de saydamdır.
- **AC-5.** Logo türevleri WebP'dir.
- **AC-6.** Tür verilmeden yüklenen bir görsel bugünkü gibi işlenir: üç türev (`large`, `display`,
  `thumbnail`), filigran basılı ve saydam piksel siyah.
- **AC-7.** Logo türüyle yüklenen bir SVG `415` ile reddedilir ve varlık oluşmaz.
- **AC-8.** Logo türündeki bir varlığın hazır duyurusu işlendikten sonra hiçbir vitrin ilanının
  görsel satırı değişmez.
- **AC-9.** Varlığın okuma yanıtı türünü taşır: logo türünde `logo`, türsüz yüklemede `photo`.
- **AC-10.** Konum bilgisi taşıyan bir JPEG'in logo türevlerinde EXIF konum bilgisi yoktur.

### Atama, değiştirme, kaldırma (R1, C9 · D2, D8, D9)

- **AC-11.** Yönetici logo varlığını atadıktan sonra panelin profil okuması o kimliği taşır.
  Profil sürümü bir artar ve yanıt yeni sürümü döner.
- **AC-12.** Eski sürümle gönderilen logo ataması `409` döner. Logo değişmez.
- **AC-13.** Logoyu kaldırmak profil okumasında logo kimliğini `null` yapar ve sürümü bir artırır.
  Kaldırılan varlığın `Media` okuması `200` dönmeye devam eder.
- **AC-14.** Logolu galeriye başka bir logo varlığı atamak eskisinin yerine yenisini koyar.

### Yetki (R8 · D4)

- **AC-15.** Satış Danışmanı oturumuyla logo ataması ve kaldırması `403` döner. Logo değişmez.
- **AC-16.** İçerik Editörü oturumuyla aynı istekler `403` döner. Oturumsuz istekler `401` döner.
- **AC-17.** Satış Danışmanı panelin galeri profili adresini doğrudan açtığında "erişim yok"
  ekranını görür. Logo bölümü basılmaz.
- **AC-18.** Satış Danışmanı oturumuyla logo türünde yükleme `202` döner, ama varlık galeriye atanmaz.

### Panel (R1, R3, R10 · D8, D12, D13)

- **AC-19.** Logosuz galerinin bölümünde "Logo yükle" seçicisi ve D12'nin yardım metni görünür.
  Seçici yalnız kabul edilen türleri önerir.
- **AC-20.** Dosya seçildikten sonra bölüm "işleniyor" durumunu gösterir. İşleme bitince önizleme,
  ekran yeniden açılmadan görünür.
- **AC-21.** Hazır logonun önizlemesi `logo-small` türevidir. "Değiştir" ve "Kaldır" eylemleri görünür.
- **AC-22.** İşlemesi başarısız olan logonun bölümünde çevrilmiş neden ve "Tekrar dene" görünür.
  "Tekrar dene" bölümü "işleniyor" durumuna döndürür.
- **AC-23.** Türü reddedilen bir dosya (`415`) çevrilmiş hatayı gösterir. Kayıtlı logo değişmez.
- **AC-24.** Boyut sınırını aşan bir dosya (`422`) çevrilmiş hatayı gösterir. Kayıtlı logo değişmez.
- **AC-25.** Logo atandıktan sonra aynı ekranda yapılan profil kaydetmesi (görünen ad ve `0024`'ün
  sosyal hesapları dâhil) `409` vermez.
- **AC-26.** Formda kaydedilmemiş bir görünen ad değişikliği varken logo atamak o değişikliği
  formdan silmez.
- **AC-27.** Profil okunurken ekranın yükleniyor durumu görünür. Logo bölümü basılmaz.
- **AC-28.** Profil okuması hata verirse ekranın hata durumu görünür. Logo bölümü basılmaz.
- **AC-29.** `ar` panelinde bölümün metinleri Arapçadır ve bölüm sağdan sola dizilir.

### Vitrin üst çubuğu (R4, R7, R10 · C6 · D3, D5)

- **AC-30.** Hazır logolu kurulumda `/tr/` üst çubuğunda logo görseli adın solundadır. Görselin
  `alt`'ı boştur ve bağlantının erişilebilir adı görünen addır.
- **AC-31.** `/tr/` belgesi, JavaScript çalıştırılmadan, `logo-small`'un adresini, genişliğini ve
  yüksekliğini taşır.
- **AC-32.** Üst çubuktaki logo görselinin `width` ve `height` öznitelikleri `logo-small`'un gerçek
  boyutlarıdır.
- **AC-33.** `/ar/` üst çubuğunda logo adın sağındadır.
- **AC-34.** Logo atanmamış kurulumda üst çubuk ve belge bu karttan önceki hâlindedir: logo görseli
  ve logo bilgisi yoktur.
- **AC-35.** Atanmış logo işleniyor ya da başarısız durumdayken üst çubukta logo yoktur.
- **AC-36.** Atanmış kimlik fotoğraf türünde bir varlıksa üst çubukta logo yoktur.
- **AC-37.** Atanmış kimlik `Media`'da yoksa üst çubukta logo yoktur ve belge 200 döner.
- **AC-38.** Logo okuması hata verdiğinde belge 200 döner ve logo yoktur.
- **AC-39.** Görünen ad okunamadığında logo atanmış olsa bile üst çubukta logo yoktur.
- **AC-40.** Ana sayfa, liste, detay, içerik ve 404 belgelerinin hepsi logo bilgisini taşır.
- **AC-41.** Logo değiştirildikten ve yeni varlık hazır olduktan sonra istenen ilk belge yeni
  logoyu taşır.
- **AC-42.** Hiçbir belge logo için ön yükleme bağlantısı taşımaz. Logo görseli düşük getirme
  önceliği taşır. Detay belgesinde ilk görselin ön yüklemesi bütün etiketlerden önce kalır
  (`0004` AC-80).
- **AC-43.** Logo atanmamış kurulumda belge başına `Media`'ya hiç okuma yapılmaz. Logolu kurulumda
  belge başına tam olarak bir okuma yapılır.

### Paylaşım önizlemesi (R5 · D6, D10)

- **AC-44.** Hazır logolu kurulumda `/tr/` belgesinin `og:image`'i `logo-large`'ın mutlak adresidir.
- **AC-45.** Hazır logolu kurulumda liste, süzgeçli liste ve içerik belgelerinin `og:image`'i
  `logo-large`'ın mutlak adresidir.
- **AC-46.** Görselli ilanın detay belgesinin `og:image`'i logolu kurulumda da ilk görselin büyük
  türevidir.
- **AC-47.** Hazır logolu kurulumda görselsiz ilanın detay belgesinin `og:image`'i `logo-large`'ın
  mutlak adresidir.
- **AC-48.** Logo atanmamışken ya da hazır değilken `0004` AC-32 ve AC-33 aynen geçerlidir: ana
  sayfa, liste, içerik ve görselsiz detay belgelerinde `og:image` yoktur.
- **AC-49.** 404 belgesi hazır logolu kurulumda da `og:image` taşımaz.

### Yapısal veri (R6 · D7, D10)

- **AC-50.** Hazır logolu kurulumda `/tr/` belgesindeki `AutoDealer`'ın `logo`'su `logo-large`'ın
  mutlak adresidir.
- **AC-51.** `department` öğelerinin hiçbiri `logo` taşımaz. Logo yokken kök `logo` anahtarı taşımaz.
- **AC-52.** `AutoDealer` bloğunda (kökte ve `department` öğelerinde) `geo` anahtarı yoktur.
  (`0004` AC-52'nin bu karttaki hâli.)
- **AC-53.** Medya kökü `&` içeren bir yapılandırmada `logo` JSON-LD'nin ham metninde `\u0026` ile
  kodlanmıştır. Üst çubuk bilgisinin ve `og:image`'in özniteliği bozulmaz. Ayrıştırılan değerler
  kurulan adrese eşittir.

### Performans ve paket (R9, C7 · D11)

- **AC-54.** Hazır logolu `/ar/` ana sayfa belgesi, test yapılandırmasının medya köküyle, aynı
  kurulumun logosuz `/ar/` belgesinden en çok **768 B** büyüktür.
- **AC-55.** `0004` AC-83'ün kurgusu hazır logolu kurulumda da en çok **6144 B**'tır.
- **AC-56.** İlk paket (giriş JS + giriş CSS, `gzip -9`, ADR 0013) `main` tabanına göre en çok
  **512 B** büyür.
- **AC-57.** Perf koşusu bir logo tohumlar. C3 ana sayfa, liste ve detayda yeşildir: LCP < 2500 ms
  ve CLS < 0,1. Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşunun sonucu ve LCP öğesi PR'da
  yazılıdır.
- **AC-58.** Logolu perf koşusunda ana sayfa, liste ve detayın LCP öğesi ne üst çubuk logosu ne
  altlık logosudur.
- **AC-59.** Perf koşusunun logosu üst çubukta görünür. Koşunun belgesi logo bilgisini taşır ve
  ölçüm logosuz bir belgeyle yapılmamıştır.

### Şema ve çeviri (C8 · D14 · DoD T5, T6)

- **AC-60.** Bu kartın `Media` migration'ı yalnız `media` şemasına dokunur. Bu kart `dealership`
  şemasına migration eklemez.
- **AC-61.** Logo bölümünün metinleri (başlık, yardım metni, "Logo yükle", "işleniyor", "Değiştir",
  "Kaldır", "Tekrar dene") tr, ar ve en'de panel kataloğundadır. Vitrin kataloğuna anahtar
  eklenmez. Eksik anahtar kontrolü yeşildir.

### Altlık logosu (R4a · D5, D11 — sahibin SS1 cevabıyla eklendi)

- **AC-62.** Hazır logolu kurulumda `/tr/` altlığının ilk öğesi logodur ve şube bloğundan önce
  gelir. Görselin adresi üst çubuktaki logo görseliyle aynı `logo-small` adresidir.
- **AC-63.** Altlık logosunun `alt`'ı o dildeki görünen addır ve görsel bir bağlantının içinde değildir.
- **AC-64.** Altlık logosu `loading="lazy"` taşır. `width` ve `height` öznitelikleri `logo-small`'un
  gerçek boyutlarıdır.
- **AC-65.** `/ar/` altlığında logo satırın sağ ucundadır. `/tr/` ve `/en/`'de sol ucundadır.
- **AC-66.** Logo atanmamışken, hazır değilken ya da görünen ad okunamadığında altlıkta logo yoktur.
  Altlığın diğer blokları bu karttan önceki hâlindedir.
- **AC-67.** Hazır logolu ana sayfa açılışında logo adresine tam olarak bir ağ isteği yapılır.
- **AC-68.** Hazır logolu `/ar/` ana sayfa belgesi, altlık logosu eklendikten sonra da AC-54'ün
  sınırındadır. Belge altlık için ayrı bir logo bilgisi taşımaz.

---

## Definition of Done

- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0026-ac<n>.png`),
      boş, işleniyor ve hata durumları dâhil; üst çubuğun ve panel bölümünün Arapça görüntüsü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok (`Dealership` → `Media` referansı yok).
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor (`media`, D14).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi ("Galeri Logosu", D15).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi
      (Host'un `Media.Contracts` okuması ve logo işleme türü).
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
| **Kriterler önce `main`'de miydi?** | | `git cat-file -e <impl>^:specs/0026-galeri-logosu.md` |
| **Kriter sayısı: önce → bugün** | → | |
| **Spec revizyon sayısı** | | Onay anındaki CLARIFY turu sayılır (ADR 0008 / K3). |
| **Düzeltme turu sayısı** | | `0` yalnız T10 `[x]` ise. |
| **Dış bulgu (gerçek/gürültü)** | / | |
| **Uygulama içi bulgu** | | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | Arşive taşınırken `izleniyor`. |

**Bu spec'ten çıkarılan ders:**
