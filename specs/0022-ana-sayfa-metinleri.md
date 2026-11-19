# 0022 — Ana Sayfa Metinlerinin Panelden Düzenlenmesi

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-18 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (B1–B12 → **D11–D22**) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (ana sayfa metinleri — yeni kayıt, yazma ve okuma uçları, migration) · `Host` (metnin ilk boyamada hazır olması — plan turu, **D6**) · `frontend/` vitrin (ana sayfa, boş stok durumu) ve panel (İçerik bölümünde yeni ekran) |
| **Bağımlı spec'ler** | `0014` (ana sayfa — tamamlandı; metinleri bu kartla düzenlenebilir olur) · `0017` (Content modülü, İçerik Editörü yetkisi, eşzamanlılık deseni — tamamlandı) · `0020` (üstteki galeri adı — **D2**) |
| **Dayanak** | Depo sahibinin 2026-11-18 isteği ve seçimi (**D1**) |
| **Karar bekliyor** | yok — **D1–D10**; QA CLARIFY turunun on iki bulgusu (B1–B12) → **D11–D22**. |

---

## Intent

Ana sayfanın yazıları yazılımın içinde sabit. "Geçmişi gizlenmeyen araçlar." başlığı, altındaki
paragraf, "Aracınızı bulun" ve "Öne çıkan araçlar" başlıkları ve stok boşken çıkan mesaj her
galeride aynı ve galeri onları değiştiremiyor; değiştirmek bir yazılım güncellemesi istiyor.
Galerinin kendi sesiyle konuşamayan bir vitrin, white-label bir ürünün vaadini tutmuyor.

Bunu **galeri yöneticisi ve içerik editörü** istiyor. Başarı şuna benziyor: panelde bu metinleri
üç dilde ayrı ayrı yazarlar; ziyaretçi ana sayfayı bir sonraki açışında yeni metni görür; bir
metin boş bırakılırsa ziyaretçi bugünkü varsayılan metni görür — sayfa hiçbir zaman boş başlıkla
açılmaz. Sayfa bugünkü kadar hızlı açılır.

---

## Requirements

- **R1.** Panelde **ana sayfa metinleri** ekranı şu alanları **etkin her dil için ayrı ayrı**
  sunar (**D1**):
  1. giriş başlığı,
  2. giriş açıklaması,
  3. "Aracınızı bulun" bölüm başlığı,
  4. "Öne çıkan araçlar" bölüm başlığı,
  5. stok boşken gösterilen mesajın başlığı,
  6. stok boşken gösterilen mesajın açıklaması.
- **R2.** Her alan **opsiyoneldir.** Bir dilde boş bırakılan alan için ziyaretçi o dildeki
  **varsayılan metni** — bugün gösterilen metni — görür (**D3**). Panel, boş alanın yerinde
  varsayılan metni ipucu olarak gösterir. **Yalnız boşluktan oluşan** değer boş sayılır ve
  varsayılana düşer; boş olmayan değerin uçlarındaki boşluk kırpılmaz (`0017` ile aynı — **D14**).
- **R3.** Metinler **düz metindir**: biçimlendirme, bağlantı ya da HTML yorumlanmaz ve girilen
  karakterler olduğu gibi görünür (**D4**). Açıklama alanlarında satır sonu korunur.
- **R4.** Başlık alanları en fazla **120**, açıklama alanları en fazla **500** karakterdir;
  sınırı aşan değer alan hatasıyla reddedilir (**D5**). Karakter **Unicode kod noktası** olarak
  sayılır — `0017`'nin kuralı; panel aynı kuralla sayan bir sayaç gösterir, tarayıcının yerel
  sınırına güvenmez (**D15**).
- **R5.** Kaydedilen metin ziyaretçiye **kaydetmeden sonraki ilk sayfa isteğinde** görünür; metin
  sayfaya sunucudan basılıyorsa o yanıt önbelleklenmez ya da kayıtta geçersizlenir (**D20**).
- **R6.** Stok boşken gösterilen mesaj, bu mesajın görüldüğü **her yerde** düzenlenen metni
  kullanır: ana sayfa ve vitrin listesi (**D7**).
- **R7.** Metinler sayfanın **ilk boyamasında** hazırdır: ziyaretçi önce varsayılan metni görüp
  sonra değişen bir metin görmez, sayfa kaymaz ve hız eşikleri bugünkü gibi tutar (**D6**).
- **R8.** Aynı metinleri **eski bir sürüm üzerinden** kaydetmek reddedilir; editör bunu bir
  uyarıyla görür ve güncel hâli yükleyebilir (`0017` **D18** deseni).
- **R9.** Ekran ve yazma ucu **İçerik Editörü ve Yönetici** içindir; Satış Danışmanı ekrana
  bağlantı görmez, adresi doğrudan açınca "erişim yok" ekranını görür ve yazma ucu ona `403`
  döner (**D8**).
- **R10.** Ekran yükleniyor, hata durumlarını ele alır; üç dilde ve Arapçada sağdan sola çalışır.
  Arapça metin alanları sağdan sola yazılır.

---

## Constraints

### Uyulması zorunlu

- **C1.** Metinler `Content` modülünde yaşar; başka modül onları yalnız `Content.Contracts`
  üzerinden okur (Yasak Listesi 1). Merkezi bir "çeviriler" tablosu kurulmaz (`0017` **C2**).
- **C2.** Varsayılan metinler **çeviri kataloğunda** kalır (`home.intro.title` vb.); kayıt yalnız
  galerinin yazdığı değeri taşır. Kayıt hiç yokken (yeni kurulum) sayfa bugünkü gibi görünür.
- **C3.** Migration yalnız `content` şemasına dokunur.
- **C4.** `0014`'ün performans kapısı (LCP < 2,5 sn, CLS < 0,1) ana sayfa ve listede yeşil kalır.
- **C5.** Girilen metin hiçbir aşamada HTML olarak yorumlanmaz — sunucu basıyorsa `<`, `>`, `&`,
  `"`, `'` ve `</script>`, `</head>` dizileri dâhil **kodlanır**; uç gösteriyorsa metin düğümü
  olarak. *(Neden somut: `IndexHtmlRenderer.cs:59-68` `string.Replace("</head>", …)` kullanıyor ve
  metindeki **her** `</head>`'i değiştirir — **D16**.)*
- **C6.** Ana sayfa formunun iskeleti ve öne çıkanlar başlığı uzun bir başlıkla da sayfayı
  kaydırmaz: metin ilk boyamada hazır olduğu için iskelet gerçek başlığı basar (**D12**).

### KAPSAM DIŞI

- **X1.** **Üstteki küçük galeri adı** — *neden:* depo sahibi bunu da seçti; `0020` **D4** onu
  galerinin dil başına **görünen adı** olarak getiriyor ve o alan galeri ekranında düzenleniyor
  (**D2**).
- **X2.** **Ana sayfa dışındaki metinler** (form etiketleri, düğmeler, altlık başlıkları, hata
  mesajları) — *neden:* istek ana sayfanın içerik metinleriydi; arayüz metinleri ürünün parçası.
- **X3.** **Biçimlendirme ve bağlantı** — **D4**.
- **X4.** **Ana sayfa görseli ya da bölüm sırası/gizleme** — *neden:* istek metindi.
- **X5.** **Taslak/yayın ayrımı ve geçmiş sürümler** — *neden:* `0017`'nin sayfalarındaki yayın
  akışı burada aşırı; metin kaydedilince yayındadır (R5).
- **X6.** **Boş stok mesajının bağlantı metni** ("Galeriye ulaşın") — *neden:* seçilen
  "stok boşken mesaj" başlık ve açıklamadır; bağlantı bir eylem etiketidir (X2).

---

## Context

### Mevcut davranış — ölçüldü (2026-11-18, `main` @ `0cd834d`)

- **Metinler çeviri kataloğundan.** `frontend/src/app/HomePage.tsx:21-22` giriş başlığı ve
  açıklamasını `home.intro.title` / `home.intro.body` anahtarlarından; form ve öne çıkanlar
  başlıkları `home.form.title`, `home.featured.title`; boş stok mesajı
  `showcase.empty.showcase.title` / `.body` / `.action` (`frontend/src/shared/i18n/locales/*.json`).
- **Boş stok bileşeni ortak.** `features/catalog/states/EmptyShowcase.tsx` hem ana sayfada
  (`HomeCatalog.tsx:83`) hem vitrin listesinde (`ShowcaseListPage.tsx`) kullanılıyor.
- **Giriş başlığı ilk boyanan metinlerden.** Performans kapısı (`perf.yml`, `lighthouse-run.mjs`)
  ana sayfayı ölçüyor (`0014` **D10**). Ana sayfanın LCP öğesinin **hangisi olduğu ölçülmedi**
  (QA B12): 500 ilanlık perf ortamında büyük olasılıkla öne çıkan bir görsel, stok boşken giriş
  bölümü. Her iki durumda da metni ayrı bir istekle sonradan getirmek ya LCP'yi geciktirir ya da
  varsayılan metnin değişmesiyle kayma üretir.
- **SPA'da istemci tarafı gezinme yok** (`App.tsx:23-29`) ve dil değiştirme tam sayfa gezinmesi
  (`LocaleSwitcher.tsx:9-12`) — "sonraki istek" ile sunucunun bastığı metin uyumlu.
- **Form ve öne çıkanlar başlığı koşullu.** Form yalnız toplam > 0 iken (`HomeCatalog.tsx:79-86`),
  öne çıkanlar bölümü yalnız öne çıkan ilan varken çiziliyor (`FeaturedSection.tsx:44-45`).
  Form iskeleti başlığı tek satırlık bir iskelet çizgisiyle ayırıyor (`FormSkeleton.tsx:16-18`,
  `styles.css:1056`) — `0014` CLS'yi tam burada yakalamıştı.
- **`EmptyState`** açıklamayı düz `<p>` içinde basıyor (`EmptyState.tsx:19`); satır sonunu
  korumuyor ve başka durumlarca da kullanılıyor.
- **HTML yanıtında `Cache-Control` yok** (`src/Host`). Veritabanından okuyup HTML'e basan gerçek
  emsal `0016b` **C13**'ün önyüklemesi (`IndexHtmlRenderer.cs:89-101`); `0014` **D22** ve ADR
  `0005` değeri yapılandırmadan basıyor.
- **Perf ortamı** yalnız ilan tohumuyla kalkıyor (`perf.yml`: `Development__SeedShowcase`,
  `SeedListingCount: 500`); kaydedilmiş metin üretecek bir yol yok.
- **Sunucunun bastığı değerler emsali.** Etkin diller `0014` **D22** ile `index.html`'e sunucudan
  basılıyor (`IndexHtmlRenderer`); tema da öyle (ADR `0005`).
- **Content modülü** sayfaları ve SSS'yi tutuyor (`Content/Domain/ContentPage.cs`); eşzamanlılık
  sürüm kolonuyla (`ContentPageConfiguration.cs:40`); panel yetkisi `AdminSurface.ContentPolicy`
  (İçerik Editörü + Yönetici).

### Verilen kararlar — 2026-11-18

**D1 — Sahibin seçimi (depo sahibi, 2026-11-18).** Panelden değiştirilecekler: giriş başlığı ve
açıklaması, bölüm başlıkları, stok boşken mesaj, üstteki küçük galeri adı — üç dilde ayrı ayrı.

**D2 — Galeri adı `0020`'de (Takım Yöneticisi (ajan) kararı).** (X1) *Gerekçe:* aynı ad hem üst
çubukta hem giriş satırında görünüyor; iki ayrı yerde düzenlenirse ikisi ayrışır.

**D3 — Boş alan varsayılana düşer (ajan).** (R2, C2) *Gerekçe:* üç dilin hepsini yazmayan bir
galerinin Arapça ana sayfası boş başlıkla açılmamalı; varsayılan metin bugün zaten üç dilde var.

**D4 — Düz metin (ajan).** (R3, X3) *Gerekçe:* başlık ve kısa paragraf biçim istemiyor; düz metin
enjeksiyon yüzeyini sıfırda tutar.

**D5 — Uzunluk sınırları 120 / 500 (ajan).** (R4) *Gerekçe:* başlık tek satırlık bir öğe,
açıklama kısa bir paragraf; sınırsız metin ana sayfanın düzenini ve LCP'yi bozar.

**D6 — Metin ilk boyamada hazır (ajan).** (R7, C4) Nasıl taşınacağı plan turunun işidir; `0014`
**D22**'nin sunucunun bastığı değer emsali tercih edilir. *Gerekçe:* ayrı bir istek ya LCP'yi
geciktirir ya da varsayılandan özele geçerken kayma üretir.

**D7 — Boş stok mesajı her yerde aynı (ajan).** (R6) *Gerekçe:* bileşen ortak; aynı durum için iki
farklı mesaj ziyaretçiyi şaşırtır.

**D8 — İçerik Editörü ve Yönetici (ajan).** (R9) *Gerekçe:* `0017`'nin içerik yetkisi; ana sayfa
metni içerik işidir. `0020`'deki galeri adı ise Yönetici'de kalır (yasal/kimlik bilgisiyle aynı
ekranda).

**D9 — Ekran İçerik bölümünde (ajan).** Panelin İçerik bölümünde sayfaların ve SSS'nin yanında
bir "Ana sayfa" girdisi. *Gerekçe:* İçerik Editörü'nün gördüğü tek bölüm orası.

**D10 — Yayın akışı yok (ajan).** (X5) Kaydetmek yayındır.

### Verilen kararlar — QA CLARIFY turu, 2026-11-18

QA spec'i `main` @ `0cd834d`'ye karşı okudu ve on iki bulgu getirdi (11 gerçek, 1 belirsiz);
hepsi Takım Yöneticisi (ajan) tarafından kabul edildi.

**D11 — Perf ortamı en uzun metinlerle tohumlanır (B1).** Geliştirme bayrağıyla açılan bir tohum
(ör. `Development:SeedHomeTexts`) her alanı sınırında (120/500) ve satır sonlu açıklamayla doldurur;
`perf.yml` bunu açar. AC-21 en uzun metinlerle ölçer. *Gerekçe:* en kötü durum ölçülmezse D5'in
"düzeni ve LCP'yi korur" iddiası sınanmamış kalır.

**D12 — İskelet gerçek başlığı basar (B2).** (C6) Metin ilk boyamada hazır olduğundan form ve öne
çıkanlar iskeleti başlığı gerçek metinle çizer; mobilde en uzun başlıkla CLS ölçülür.

**D13 — AC-16 (şimdi AC-19) ölçülebilir hâle getirildi (B3).** Ölçüm: gezinme başından itibaren `h1`'in aldığı
**her** metin değeri kaydedilmiş metne eşittir (e2e, `MutationObserver`); aynısı boş stok başlığı
için.

**D14 — Yalnız boşluk boştur (B4).** (R2) `0017`'nin `ContentRules.cs:21-22` davranışı.

**D15 — Kod noktası sayımı (B5).** (R4) `0017`'nin `ContentRules.cs:25` kuralı; panel aynı kuralla
sayan sayaç kullanır. Sınır kriterleri Arapça ve emoji içerir.

**D16 — Sunucu basıyorsa kodlama `</script>` ve `</head>`'i de kapsar (B6).** (C5) Sınama sunucu
yanıtı üzerinde yapılır.

**D17 — Boş stok açıklamasında satır sonu (B7).** (R3) Satır sonu stili yalnız bu kullanıma
uygulanır; `EmptyState`'in diğer kullanımları değişmez.

**D18 — AC-7'nin (şimdi AC-8) ön koşulu (B8).** Yayında ilan ve öne çıkan ilan varken.

**D19 — AC-22 (şimdi AC-28) doğrudan adresi ölçer (B9).** Satış Danışmanı İçerik bölümünü zaten görmüyor; kriter
`/admin/content/home` adresinin "erişim yok" göstermesini ölçer.

**D20 — Önbellek (B10).** (R5) HTML'e basılan metin varsa yanıt önbelleklenmez (`Cache-Control:
no-store` ya da eşdeğeri) veya kayıtta geçersizlenir; AC-3 kaydetmeden hemen sonraki ilk isteği
ölçer.

**D21 — D6'nın emsali düzeltildi (B11).** Veritabanından okuyup HTML'e basmanın emsali `0016b`
**C13**; her HTML isteğine bir okuma ekler (TTFB). Plan turu önbellek/geçersizleme kararını D20 ile
birlikte verir.

**D22 — LCP öğesi ölçülmedi diye yazıldı (B12).** Plan turu `lighthouse-run.mjs`'in `lcpElement`
değerini ana sayfa için kanıta ekler.

---

## Acceptance Criteria

### Ekran ve kaydetme (R1, R2, R5 · D3, D9)

- **AC-1.** İçerik Editörü panelin İçerik bölümünde "Ana sayfa" girdisinden metin ekranını açar; ekran altı alanı etkin her dil için ayrı ayrı gösterir.
- **AC-2.** Boş bir alanın yerinde o dilin varsayılan metni ipucu olarak görünür.
- **AC-3.** Türkçe giriş başlığı "Kadıköy'ün güvenilir galerisi" olarak kaydedildikten **hemen sonraki ilk** Türkçe ana sayfa isteği bu başlığı gösterir (D20).
- **AC-4.** Türkçe giriş başlığı kaydedildiğinde İngilizce ve Arapça ana sayfa kendi varsayılan başlıklarını göstermeye devam eder.
- **AC-5.** Kaydedilmiş bir alan boşaltılıp kaydedildiğinde ana sayfa o dilde yeniden varsayılan metni gösterir.
- **AC-6.** Yalnız boşluktan oluşan bir giriş başlığı (`"   "`) boş sayılır ve ana sayfa varsayılan başlığı gösterir (D14).
- **AC-7.** Hiç kayıt yapılmamış bir kurulumda ana sayfanın altı metni bugünkü varsayılan metinlerle aynıdır.
- **AC-8.** Yayında ilan ve öne çıkan ilan varken, "Aracınızı bulun" ve "Öne çıkan araçlar" başlıkları kaydedildikten sonra ana sayfada kaydedilen metinle görünür (D18).

### Boş stok mesajı (R6 · D7)

- **AC-9.** Yayında hiç ilan yokken ana sayfa, kaydedilmiş boş stok başlığını ve açıklamasını gösterir.
- **AC-10.** Yayında hiç ilan yokken vitrin listesi aynı kaydedilmiş başlığı ve açıklamayı gösterir.
- **AC-11.** Boş stok mesajının bağlantı metni değişmez ("Galeriye ulaşın").

### Düz metin ve sınırlar (R3, R4 · C5 · D4, D5)

- **AC-12.** `<b>Kalın</b> & "tırnak"` olarak kaydedilen giriş başlığı ana sayfada bu karakterlerle metin olarak görünür; kalın yazılmaz ve sayfada yeni bir öğe oluşmaz.
- **AC-13.** `</script></head><script>alert(1)</script>` içeren bir açıklama kaydedildiğinde sunucunun ana sayfa yanıtında bu dizi kodlanmış durur, sayfanın `<head>` yapısı bozulmaz ve tarayıcıda hiçbir betik çalışmaz (D16).
- **AC-14.** Giriş açıklamasına yazılan satır sonu ana sayfada yeni satır olarak görünür.
- **AC-15.** Boş stok açıklamasına yazılan satır sonu ana sayfada ve listede yeni satır olarak görünür; `EmptyState`'in diğer kullanımlarının görünümü değişmez (D17).
- **AC-16.** 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.
- **AC-17.** 120 karakterlik başlık ve 500 karakterlik açıklama kabul edilir.
- **AC-18.** Arapça harf ve emoji içeren, tam 120 kod noktalı bir başlık kabul edilir; 121 kod noktalı olanı reddedilir; panelin sayacı iki durumda da sunucuyla aynı sayıyı gösterir (D15).

### İlk boyama ve hız (R7 · C4 · D6)

- **AC-19.** Kaydedilmiş bir giriş başlığı olan ana sayfada, gezinme başından itibaren `h1`'in aldığı her metin değeri kaydedilmiş başlığa eşittir; varsayılan başlık hiçbir an görünmez (D13).
- **AC-20.** Aynı ölçüm, yayında ilan yokken boş stok başlığı için de geçerlidir (D13).
- **AC-21.** Her alanı sınırında (120/500, satır sonlu) kaydedilmiş metinlerle ana sayfa için performans kapısı (LCP < 2,5 sn, CLS < 0,1) yeşildir (D11).
- **AC-22.** En uzun form başlığıyla mobil görünümde form iskeletinden gerçek forma geçiş sayfayı kaydırmaz; CLS < 0,1 (D12).
- **AC-23.** Ana sayfanın HTML yanıtı önbelleklenmeyi engelleyen bir başlık taşır ya da kayıt önbelleği geçersizler (D20).
- **AC-24.** Liste için performans kapısı yeşil kalır.

### Eşzamanlılık (R8)

- **AC-25.** Okunduktan sonra başka bir kaydetmeyle değişmiş sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı metin değişmez.
- **AC-26.** Reddedilen kaydetmede panel uyarı gösterir, güncel hâli yükleme yolu sunar ve editörün yazdıkları ekranda kalır.

### Erişim (R9 · D8)

- **AC-27.** Yönetici de ekranı açar ve kaydeder.
- **AC-28.** Satış Danışmanı'nın panel gezinmesinde İçerik ve "Ana sayfa" bağlantısı yoktur; `/admin/content/home` adresini doğrudan açtığında "erişim yok" ekranını görür (D19).
- **AC-29.** Satış Danışmanı'nın yazma ucuna isteği `403` alır ve metin değişmez.
- **AC-30.** Oturumsuz yazma isteği `401` alır.

### Durumlar ve dil (R10)

- **AC-31.** Ekran veri gelirken yükleniyor durumunu gösterir.
- **AC-32.** Veri okunamadığında teknik ayrıntı göstermeyen bir hata ve yeniden deneme gösterilir.
- **AC-33.** Kaydetme sunucu hatasıyla düştüğünde yazılanlar kaybolmaz.
- **AC-34.** Arapça alanlar sağdan sola yazılır; Arapça kaydedilen giriş başlığı Arapça ana sayfada sağdan sola düzende görünür.
- **AC-35.** Yeni ekranın metinleri TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.
- **AC-36.** Etkin dil listesinden çıkarılmış bir dilin alanları ekranda gösterilmez.

---

## Definition of Done

- [x] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı. *(B1–B12 → D11–D22, 2026-11-18.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0022-ac<n>.png`),
      boş ve hata durumları dâhil; Arapça görüntü de var (AC-34).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `content` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**ana sayfa metinleri**).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4); performans kapısı kaydedilmiş metinlerle de ölçüldü (AC-21).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.

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
