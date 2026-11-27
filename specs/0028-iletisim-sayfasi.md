# 0028 — İletişim sayfası

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-27 — Proje Yöneticisi kararı) · QA CLARIFY turu bekleniyor |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (ziyaretçi okumasına e-posta ve harita adresleri) · `Content.Contracts` (yalnız ayrılmış ad listesine `contact`) · `Host` (yeni sayfa türü, belge etiketleri, yapısal verinin taşınması, sitemap) · Frontend (iletişim sayfası, altlığın sadeleşmesi, üst çubuk bağlantısı, boş vitrin eylemi) · `docs/` (`deploy.md`, Karar Defteri) |
| **Bağımlı spec'ler** | **`0027`** (**D1**, **D9**, **D11**, **D12**, **D15**: "şu an" satırı, yenileme, yerleşim, yapısal veri, tembel parça; bu kart altlık kararlarını tersine çevirir) · **`0025`** (haftalık liste, **D9** `openingHoursSpecification`, **D10**(a) bütçe) · `0014` (altlık, **D15** izin listesi, iskelet) · `0020` (**D15** izin listesi, **X4**: vitrinde e-posta/koordinat yok) · `0003` (**X3** harita, koordinat, e-posta alanları) · `0009` (**D2**: şube e-postası bildirim hedefi) · `0004` (**D1**, **D7**, **D8**, **D11**, **AC-52** `geo` yok, sitemap) · `0017` (ayrılmış adlar, slug kuralları) · `0024` (**AC-55** ilk paket tanımı, **X3** üçüncü taraf) · `0026` (üst çubukta logo) · `frontend-002` (**D8**: üçüncü taraf isteği yok) · `frontend-003` (**D1**, **D2**, **D8**, **D13**: ayrılmış ad, belge, üst çubuk, kritik yol) · `0022` (**AC-11** boş vitrin metni) |
| **Karar bekliyor** | yok — D1–D19 Proje Yöneticisi kararı (2026-11-27, dört ek şartla: (a)–(d)); SS1–SS4 sahip kararı (2026-11-27). |

> **Sahibin isteği (2026-11-27, kelimesi kelimesine):** "footer çalışma saatleri eklenince çok büyüdü.
> Çalışma saatlerini oradan kaldırıp yeni bir iletişim sayfası yaparak oraya taşıyalım; adres, tel,
> e-mail ve Google Haritalar'da adresi gösteren bir harita olsun."
>
> **Sahibin cevapları (sahip kararı, 2026-11-27):** dördü de önerilen yönde.
> - SS1: harita tıkla-yükle, her zaman "Google Haritalar'da aç" bağlantısı (D6). Google Cloud hesabı ve
>   anahtar sahibin ayrı işidir. Anahtar yokken yalnız bağlantı görünür (D7).
> - SS2: şubenin bugünkü e-posta alanı vitrinde gösterilir (D8). Sahip bunun talep bildirim adresi
>   olduğunu bilerek seçti.
> - SS3: her dilde `/{l}/contact` (D2).
> - SS4: altlıktaki "şu an" satırı kalkar (D9).
>
> **Proje Yöneticisi kararı (2026-11-27):** D1–D19 onaylandı. Dört ek şart işlendi:
> - **(a)** anahtar: D5, D7, D18 → AC-21 ve AC-28'in metni değişti, yeni kriterler **AC-81–AC-83**;
> - **(b)** üçüncü taraf istek ve onayın sıfırlanması: D6, D15 → **AC-84**;
> - **(c)** altlık: font bekletmeli CLS, C3 yan yana ve "Diller" sütununun alt satıra kayması: D9, D14
>   → AC-76'nın metni değişti, yeni kriterler **AC-85–AC-87**;
> - **(d)** çerez politikası notu: D18, Context 7.
>
> Var olan numaralar değişmedi.

---

## Intent

Ana sayfanın altlığı, çalışma saatleri ve özel günler eklenince uzadı. Şube sayısı arttıkça daha da
uzuyor. Altlık bir özet olmaktan çıktı ve ana sayfanın sonunu kalabalıklaştırıyor. Öte yandan
ziyaretçinin "nerede, nasıl ulaşırım, şimdi açık mı" sorusunun cevabı yalnız ana sayfanın en altında.
Araç listesinden ya da araç sayfasından oraya gidilemiyor. Boş vitrinin "Galeriye ulaşın" bağlantısı
da listede hiçbir yere gitmiyor. Ziyaretçi şubenin e-postasını göremiyor, şubenin yerini haritada
bulamıyor.

Bu iş bittiğinde her sayfanın üst çubuğunda bir "İletişim" bağlantısı olur. Bağlantı, her şubenin
adresini, telefonunu, e-postasını, "şu an açık" durumunu, haftalık saatlerini, özel günlerini ve
Google Haritalar'daki yerini gösteren tek bir sayfaya gider. Altlık yeniden kısalır: şube adı, adres,
telefon ve bu sayfaya giden bir bağlantı kalır. Ziyaretçi haritayı istemedikçe Google'a hiçbir istek
gitmez. Arama motoru şubenin saatlerini ve yerini bu sayfadan okur.

---

## Requirements

- **R1.** Vitrinde, her etkin dilde bir **iletişim sayfası** vardır. Sayfa vitrinde görünen her şubeyi
  listeler (D2, D3).
- **R2.** Sayfada her şube için ad, adres, telefon (aranabilir bağlantı), varsa e-posta (yazılabilir
  bağlantı), "şu an" durumu, haftalık saatler, "Bugün" vurgusu, yaklaşan özel günler ve harita bulunur
  (D3, D8). Saat ve özel gün kuralları `0025` ve `0027`'deki gibidir.
- **R3.** Harita Google Haritalar'da şubenin yerini gösterir. Her şubede ayrıca "Google Haritalar'da
  aç" bağlantısı bulunur (D6, D7, **SS1**).
- **R4.** Ziyaretçi haritayı açıkça istemeden sayfa Google'a ya da başka bir üçüncü tarafa istek
  yapmaz (D6, D15, **SS1**).
- **R5.** Haritanın yokluğu bilgiyi eksiltmez. Adres her durumda metin olarak okunur (D4, D16).
- **R6.** Ana sayfa altlığından çalışma saatleri ve özel günler kalkar. Altlıkta şube adı, adres,
  telefon ve iletişim sayfasına bağlantı kalır (D9, **SS4**).
- **R7.** Her vitrin sayfasından iletişim sayfasına ulaşılır (D10). Boş vitrinin "Galeriye ulaşın"
  eylemi bu sayfaya gider (D17).
- **R8.** Sayfa arama motoruna açıktır. Başlık, açıklama, kanonik adres, dil alternatifleri ve sitemap
  girdisi taşır (D12).
- **R9.** Şubenin saatleri, özel günleri, e-postası ve yeri iletişim sayfasının yapısal verisindedir.
  Ana sayfanın yapısal verisi altlıkta görünenle sınırlı kalır (D13).
- **R10.** Sayfa TR/AR/EN'de çevrilidir ve Arapçada sağdan sola dizilir. Telefon, e-posta, saat ve
  tarih her dilde Latin rakamla, soldan sağa basılır.
- **R11.** Sayfa yüklenirken, boşken ve okuma hata verdiğinde ayrı bir durum gösterir (`docs/frontend.md` § 3, D4).

---

## Constraints

### Uyulması zorunlu

- **C1.** Şube verisinin tek yazma sahibi `Dealership`'tir (`0003` C1). E-posta ve koordinat zaten
  şubede ve panelde (`0020` R15, D9). Bu kart yeni bir alan ya da yeni bir migration eklemez (D19).
- **C2.** Ziyaretçiye dönük okuma bir **izin listesidir** (`0014` **D15**, `0020` **D15**). Şube öğesine
  yalnız D5'in iki anahtarı eklenir. Saat dilimi, WhatsApp, slug ve ham koordinat ziyaretçiye
  gönderilmez.
- **C3.** Yapısal veri sayfada görünmeyen bilgi taşımaz (`0004` **D1**). Bir sayfanın JSON-LD'si
  yalnız o sayfada görünen şube bilgisini taşır. Her belge en çok bir JSON-LD bloğu taşır (D13).
- **C4.** Ziyaretçinin onayı olmadan vitrin üçüncü taraf alan adına istek yapmaz. Bu ilke bugün
  yazı tiplerinde var (`frontend-002` **D8**, AC-8) ve gömülü akışı dışarıda bıraktı (`0024` **X3**).
  Harita bu ilkenin ilk istisnasıdır ve yalnız ziyaretçinin eylemiyle çalışır (D6).
- **C5.** Performans: C3 (LCP < 2500 ms, CLS < 0.1) ana sayfa, liste, detay ve iletişim sayfasında
  yeşildir. İlk paket `0024` **AC-55**'in tanımıyla ölçülür ve `main` tabanına göre en çok **+512 B**
  büyür (D14).
- **C6.** "Şu an" hesabı, saat kuralları ve yenileme davranışı (`0027` D6–D10) **değişmez**. Yalnız
  gösterildiği yer değişir (D11).
- **C7.** Hata ve durum metinleri çeviri kataloğundadır. Kodda sabit metin yoktur (DoD T6, `frontend-003` D19).
- **C8.** Yeni rota üç yerde birlikte tanınır: `ContentReservedSlugs`, uçtaki ayna liste ve sunucunun
  `SeoPage` sınıflandırması. Biri eksik kalırsa sayfa 404 alır (`frontend-003` Context, tuzak 5).
- **C9.** Bu kart `main` @ `694e0b1`'i temel alır (`0027` kapanışı).

### KAPSAM DIŞI

- **X1.** **İletişim formu** — *neden:* sahibin isteğinde yok. İlan detayında talep formu zaten var
  (`0005`). Genel bir form; konu, alıcı, KVKK izni ve spam koruması demek. Ayrı kart.
- **X2.** **WhatsApp bağlantısı** — *neden:* sahip istemedi. Alan şubede var (`0003`), açılması tek
  satırlık bir istek olur ama `wa.me` bağlantısı da bir üçüncü taraf hedefi. Ayrı karar.
- **X3.** **Şube başına ayrı sayfa** (`/tr/contact/kadikoy`) — *neden:* `0004` X3, `0025` X6, `0027` X7.
  Tek sayfa bütün şubeleri taşır. Şube slug'ı ziyaretçiye açılmaz (C2).
- **X4.** **"En yakın şube", yol tarifi ve konum izni** — *neden:* `0003` X6 (`Coordinate.cs:7`).
  "Google Haritalar'da aç" bağlantısı yol tarifini Google'a bırakır.
- **X5.** **Statik harita görseli** (yer tutucuda harita resmi) — *neden:* Google'ın görseli de üçüncü
  taraf isteği (C4) ve ücretli bir API (Static Maps). Kendi sunucumuzdan harita karosu sunmak ayrı
  bir altyapı.
- **X6.** **OpenStreetMap ya da başka bir harita sağlayıcısı** — *neden:* sahip "Google Haritalar"
  dedi. OSM'in karo sunucusunun kullanım kuralları ticari siteye ayrı sağlayıcı ister.
- **X7.** **Harita onayının hatırlanması** (çerez ya da tarayıcı deposu) — *neden:* onayı saklamak bir
  onay yönetimi aracı demek: geri alma, süre ve politika metni. Tıklama sayfa başınadır (D6).
- **X8.** **Çerez politikası metninin yazılması** — *neden:* sistem sayfası içeriği sahibindir
  (`docs/domain.md:87`). Kurulum sayfayı gövdesiz açıyor (`InitialContent.cs:116-125` yalnız kimlik
  ve anahtar yazar). Bu kart sahibe bir not bırakır (D18).
- **X9.** **İlan detayında şube bağlantısı** — *neden:* detay şubenin yalnız adını `VehicleCatalog`
  kopyasından okur, kimliğini taşımaz (`0027` X3). Detaydaki ziyaretçi iletişime üst çubuktan ulaşır (D10).
- **X10.** **Sosyal hesapların iletişim sayfasında gösterilmesi** — *neden:* "Bizi takip edin" altlıkta
  kalır (`0024` D6). İletişim sayfası altlık taşımaz. Bu yüzden `sameAs` bu sayfanın yapısal verisine
  girmez (D13).
- **X11.** **Geçici kapalı ya da kapatılmış şubenin sayfada görünmesi** — *neden:* `0020` AC-48,
  `0025` X8, `0027` X12. Ziyaretçi okuması yalnız aktif şubeyi taşır.
- **X12.** **Site geneli altlık** — *neden:* `0024` X4. Altlık yalnız ana sayfada kalır.

---

## Context

> Her iddia `main` @ `694e0b1` üzerinde ölçüldü. Satır numaraları o commit'e aittir.

### Kod doğrulamaları

**1. E-posta nerede?** **Şubede var, galeri profilinde yok.**
- `Branch.Email` (`Domain/Branch.cs:49`), tipi `EmailAddress?` ve opsiyonel (`0003` AC-7).
  `dealership.branches.email varchar(254)` (`BranchConfiguration.cs:42-47`).
- Panelin şube formunda alan var (`BranchEditPage.tsx:48`, `:63`, `:204`, `:333` `type="email"`).
  Boşaltılabilir (`0020` D9, AC-39).
- `Gallery`'de e-posta yok. Alanları unvan, logo, vergi no, sicil no, görünen ad ve sürüm
  (`Domain/Gallery.cs:34-54`).
- **Bu adres bugün bir işe bağlı:** talep bildirimi şubenin e-postasına gider, yoksa kurulum adresine
  (`0009` **D2**; `BranchQueries.cs:18-47`, `BranchNotificationContactDto`). Vitrine açılırsa bildirim
  alan kutu herkese açık olur.
- Ziyaretçiye **bilerek kapalı**: `PublicGalleryQueries.cs:62-63` "e-posta, WhatsApp, koordinat ve slug
  okunmaz" (`0020` D15). `0020` **X4**: "vitrinde WhatsApp, e-posta, koordinat gösterimi — `0003` X3".
  → **SS2**, **D5**, **D8**.

**2. Koordinat var mı?** **Var, opsiyonel.**
- `Branch.Coordinate` (`Branch.cs:43`), `decimal(18,8)` iki kolon (`BranchConfiguration.cs:68-73`).
  Aralık denetimi `Coordinate.cs:23-31` (−90…90, −180…180). PostGIS yok (`0003` C8).
- Panelde enlem/boylam alanları var (`BranchEditPage.tsx:51-52`, `:191-208`).
- `0004` koordinatı yapısal veriden bilerek çıkardı: "koordinat vitrinde gösterilmiyor"
  (`0004:142`, **AC-52** `geo` yok; `SeoBelgeTests.cs:580`).
- → Harita **koordinattan, yoksa adresten** çizilir (**D7**). Koordinat yazım farkından etkilenmez.
  Serbest metin adresin Google'da bulunması adresin yazımına bağlıdır.

**3. Altlıkta bugün ne var?** (`App.tsx:139-160`, `HomeFooter.tsx`, `BranchList.tsx`)
- Sıra: logo (`0026`) · "Şubelerimiz" bloğu · "Keşfet" · "Kurumsal" · "Bizi takip edin" · "Diller".
- *(PM şartı (c), ölçüldü:)* Izgara `repeat(auto-fit, minmax(var(--card-min-inline-size), 1fr))`,
  boşluk 16 px (`styles.css:1318-1326`; `--card-min-inline-size` 240 px, `--layout-max-inline-size`
  1200 px, iç boşluk 2×16 px; `tokens.css:108`, `:145-146`). Logo bütün satırı kaplar (`:246-248`).
  Kullanılabilir genişlik 1168 px. 4 sütun 4×240 + 3×16 = 1008 px ile sığar, 5 sütun 1264 px ister.
  Beş blok da varken (kurumsal sayfa ve sosyal hesap girilmiş) **"Diller" ikinci satıra düşer.** Bu
  1280 px'te ve daha geniş her ekranda böyledir (→ **D9**, AC-85).
- Şube başına: ad · iki satırlık sabit "şu an" kutusu · adres · telefon (`tel:`) · haftalık yedi satır
  (aralık başına bir satır) · "Özel günler" (kayıt başına bir satır, aralık başına satır).
  `BranchList.tsx:58-109`.
- Saat bloğu tembel parçada (`branchClock.tsx`, `0027` D15). Evre B iskeleti veriden kuruluyor
  (`BranchList.tsx:16-27`). D9 yenileyicisi parçanın içinde, modül düzeyinde bir depo
  (`branchClock.tsx:22-60`).
- `id="contact"` altlığın kendisinde (`HomeFooter.tsx:37`). Boş vitrinin "Galeriye ulaşın" bağlantısı
  `href="#contact"` (`EmptyShowcase.tsx:22`). **`EmptyShowcase` listede de basılıyor**
  (`ShowcaseListPage.tsx:187`), ama listede altlık yok (`AppShell.tsx:10-11`). Yani listede bu bağlantı
  bugün hiçbir yere gitmiyor (→ **D17**). Testler yalnız bağlantı metnini ölçüyor
  (`AnaSayfaMetinleri.test.tsx:181-188`, `evidence-0022.spec.ts:290`). `#contact` hedefini ölçen test yok.

**4. Adres, ayrılmış adlar ve belge.**
- `ContentReservedSlugs.All` = `cars, admin, api, assets, newsletter, favorites, compare`
  (`ContentPageContracts.cs:60`). Ayna liste `RESERVED_SEGMENTS` (`shared/lib/route.ts:252`). Sunucu
  sınıflandırması `SeoPage.cs:25-80`.
- Kodda, tohumda ve testte `contact` ya da `iletisim` adlı bir içerik sayfası yok (arama: `src/`,
  `frontend/src/`, `tests/`, `frontend/e2e/`). Canlı kurulum yok. Ama sahibin yerel sitesinin (5080)
  veritabanında böyle bir sayfa olabilir → DoD'da bir salt-okur sorgu.
- `Referrer-Policy: no-referrer` yalnız `IsPrivate` sayfalarda (`SeoPage.cs:93`,
  `IndexHtmlRenderer.cs:134-137`). İletişim sayfası özel değildir.
- **CSP yok**: `src/Host` altında `Content-Security-Policy`, `frame-src` ya da `X-Frame-Options`
  geçmiyor. `docs/deploy.md`'de de yok. Yani bugün bir iframe'i engelleyen kural yok. Ters vekil bir
  CSP eklerse harita kırılır → **D18** notu.
- Belge başlığı `WithName(metin, ad)`, ayraç `" · "` (`SeoHeadBuilder.cs:43`, `:485`). Özel sayfalar
  `Private(...)` (`:86-87`). İçerik sayfası kanonik ve alternatif kurar (`:366-399`).
- Sitemap ana sayfa, liste, ilan ve içerik sayfalarını **açıkça** sayar (`SeoEndpoints.cs:61-66`).
  Yeni sayfa kendiliğinden girmez. `SitemapTests.cs:63` (`0004/AC-56`) tr adres kümesini **tam** ölçer.

**5. Ana sayfanın yapısal verisi.** `SeoHeadBuilder.cs:96-165`. Kök `AutoDealer`: `name`, `url`, varsa
`logo` (`0026`), varsa `sameAs` (`0024`). `department` öğesi: `@type`, `name`, `address`,
`telephone`. Programlıysa `openingHoursSpecification` (`0025` D9), penceredeki özel gün varsa
`specialOpeningHoursSpecification` (`0027` D12). Saatler altlıktan kalkınca bu iki anahtar ana sayfada
görünmeyen bilgi olur (C3) → **D13**.

**6. Üst çubuk.** `TopBar.tsx:20-60`: marka (logo + ad) ve satır sonu grubu (sayaç + dil değiştirici).
40rem altında **her zaman iki satır** (`styles.css:299-322`, `frontend-003` D8). Ölçülmüş paylar
(`frontend-003` D8):
- 320 px'te dil değiştirici tek başına 207 px. Sayaçla birlikte satır sonu grubu 259 px. Kullanılabilir
  genişlik ≈288 px. **İkinci satırda ≈29 px kalıyor**, metinli bir bağlantı sığmaz.
- Tek satırın sığdığı en dar genişlik tr 560, en 560, ar 576 px (logolu, 40 karakterlik ad, 24 favori).
  Eşik 640 px. Satır sonu grubuna metinli bir bağlantı eklenirse bu genişlikler büyür ve 640 px eşiği
  yeniden ölçülmelidir.
- Logo azami genişliği 160 px (`tokens.css:157`). Onaylı düşürme adımı: dar ekranda 120 px
  (`frontend-003` D8). Dokunma hedefi `--touch-target-min-size` = 44 px (`tokens.css:150`).
- → **D10**.

**7. Çerez ve üçüncü taraf.** "Örnek çerez politikası 'üçüncü taraf çerez yok' diyor" bilgisi **kodda
değil**. Kurulum çerez politikası sayfasını gövdesiz açıyor (X8). *(PM şartı (d):)* Metin, Proje
Yöneticisi'nin yerel veriye girdiği örnek içerik. Gerçek kurulumda da sahip benzer bir metin yazabilir.
Harita açıldığında bu cümle yanlış olur (D18). Kodda doğrulanan şunlar:
- Vitrin bugün hiçbir üçüncü taraf alan adına istek yapmıyor: yazı tipleri kendi kaynağımızdan
  (`frontend-002` D8, AC-8; `fonts.css:4-8`).
- Gömülü akış "üçüncü taraf betiği ve çerezi" diye dışarıda bırakıldı (`0024` X3).
- Vitrinin kendi çerezi yok. Oturum çerezi yalnız panelde (`docs/architecture.md:569`).

Harita gelirse ilk üçüncü taraf istek o olur. Sahip çerez politikasını buna göre yazmalıdır (D18).

### Harita seçenekleri (dış kaynak, 2026-11-27)

| | (a) Doğrudan gömme | (b) Tıkla-yükle (öneri) | (c) Yalnız bağlantı |
|---|---|---|---|
| Sayfa açılınca Google isteği | var (iframe, betikler, çerezler) | **yok**. Yalnız düğmeyle | yok |
| KVKK / çerez politikası | onaysız üçüncü taraf çerezi. Politika ve muhtemelen onay bandı gerekir | istek ziyaretçinin eylemiyle gider. Bilgi metni düğmenin yanında | değişmez |
| LCP / CLS | iframe ilk ekranda ağır bir alt belge. LCP'ye rakip olabilir | yer tutucu yerel ve sabit oranlı. Ölçülür | yok |
| Sahibin "harita olsun"u | karşılar | karşılar (bir tıkla) | **karşılamaz** |
| Anahtar | gerekir (aşağıda) | gerekir | gerekmez |

Kaynaktan doğrulananlar:
- Resmî yol **Maps Embed API** (`https://www.google.com/maps/embed/v1/place?key=…&q=…`). Her istek bir
  API anahtarı ister. Google'a göre kullanımı **ücretsiz ve sınırsızdır**, ama anahtar bir Google Cloud
  projesi ve **faturalandırma hesabı** ister
  ([Embed kullanım ve ücret](https://developers.google.com/maps/documentation/embed/usage-and-billing),
  [Embed genel bakış](https://developers.google.com/maps/documentation/embed)).
- Anahtarsız `maps.google.com/maps?q=…&output=embed` adresinin **resmî desteği yok**. Kaynaklar adresin
  bozulduğunu ve hata verdiğini bildiriyor
  ([vielhuber.de](https://vielhuber.de/en/blog/google-maps-without-api-key),
  [gist](https://gist.github.com/vielhuber/331190767bb32b2468632e609223c53e)). Önerilmez.
- "Google Haritalar'da aç" için resmî ve anahtarsız **Maps URLs** biçimi var:
  `https://www.google.com/maps/search/?api=1&query=…`.
- Anahtar zaten iframe adresinde, yani gizli değildir. Google konsolunda **HTTP yönlendiren** (site
  adresi) ve **yalnız Maps Embed API** ile kısıtlanır. Yönlendiren kısıtı tarayıcının `Referer`'ı
  göndermesini ister. → iframe `referrerpolicy="strict-origin-when-cross-origin"` taşır: yalnız köken
  gider, sayfanın yolu gitmez (D15).

### Ölçümler (`main` @ `694e0b1`, `vite build`, `gzip -9`)

- İlk paket: giriş `index-*.js` **107 129 B**, `index-*.css` **12 206 B**.
- Saat parçası (`branchClock-*`): JS **2052 B** + CSS **368 B** ≈ **2,4 KB**. Bugün ana sayfa her
  açılışta bu parçayı ilk boyamadan sonra indiriyor. Altlıktan saatler kalkınca ana sayfa bu isteği
  yapmaz (AC-60). Parça LCP öğesinden sonra geliyordu. Yani beklenen kazanç ana sayfanın ana iş
  parçacığı ve ağ yüküdür, LCP için garanti değildir. C3 medyanları PR'da `main` ile yan yana yazılır (AC-76).
- C3 ölçümü Host gerektirir. Analist Host başlatmaz, ana sayfanın LCP farkı DEV'in PR ölçümüdür.

### Bilinen tuzaklar

1. **Ayrılmış ad üç yerde** (C8). `0018` ve `frontend-003` aynı yolu izledi. Biri unutulursa sayfa
   404 alır ya da uç başka ekran basar (`SeoPage.cs:24-27`).
2. **Yapısal veri ile görünen bilgi ayrışır.** Saatler altlıktan kalkıp ana sayfanın JSON-LD'sinde
   kalırsa `0004` D1 kırılır. Taşınırken `0025` AC-55–AC-60 ve `0027` AC-62–AC-66'nın belgesi değişir (tablo).
3. **Bildirim adresi herkese açık olur** (Context 1). Şube e-postası talep bildiriminin hedefi.
   Vitrine açılması adresi toplayıcı botlara da açar (**SS2**).
4. **Google isteği sessizce sızabilir.** `<link rel="preconnect">`, `dns-prefetch`, bir favicon ya da
   yer tutucuda Google'dan bir görsel, düğmeye basılmadan üçüncü taraf isteği üretir. Ölçü ağ kaydıdır,
   kod okuması değil (AC-49).
5. **Anahtarsız kurulum.** Anahtar yoksa iframe Google'ın hata sayfasını basar. → Anahtar yoksa
   kutu hiç basılmaz (D7, AC-55).
6. **Ondalık ayırıcı kültüre bağlı.** `decimal.ToString()` `tr-TR`'de virgül üretir (`41,0123`).
   Sorgu `InvariantCulture` ile kurulur (AC-25).
7. **Üst çubuk eşiği bayatlar.** 640 px, bugünkü içerikle ölçüldü. Yeni bağlantı eşiği değiştirir
   (Context 6, AC-68).
8. **Tıklayınca odak kaybolur.** Düğme iframe'le değişince odak `body`'ye düşer. Klavye kullanıcısı
   sayfanın başına döner → odak iframe'e taşınır (AC-52).
9. **Tekil depo + StrictMode** (`0027` ders 1). D9 yenileyicisi yeni bir sayfaya taşınırken StrictMode'lu
   vitest yeniden koşturulur (AC-44).
10. **"Şu an" kutusu yeni sayfada da iki satır** (`0027` D11, AC-69). Sayfanın sütun genişliği altlıktan
    farklıdır; ölçüm yeni sayfada yapılır.

### Bilerek değişecek eski testler (kod taranarak çıkarıldı)

| Test | Bugün ölçtüğü | Bu kartla |
|---|---|---|
| `tests/Host.Tests/GaleriPublicUcTests.cs:34` (`0014/AC-14`, `0025/AC-41`, `0027/AC-42`) ve `CalismaSaatleriZinciriTests.cs:382-390` | şube öğesi `address, name, openNow, openingHours, phone, specialHours` | `address, email, map, name, openNow, openingHours, phone, specialHours` (**AC-21**). Çok etiketli |
| `PublicBranchDto` kuran testler (`tests` altında) | yedi alanlı kurucu | kurucu iki alan alır (`Email`, `Map`). Beklentiler değişmez |
| `tests/Host.Tests/CalismaSaatleriBelgeTests.cs:93`, `:105`, `:125` (`0025/AC-54`, `AC-56`, `AC-59`, `AC-60`) | `/tr/` ve `/ar/` belgesinin `department`'ında `openingHoursSpecification` ve 2304 B bütçe | aynı ölçüm **`/tr/contact`** ve **`/ar/contact`** belgesinde (**AC-14**, **AC-18**). Ana sayfada anahtar **yok** (**AC-17**). Çok etiketli: `0025/AC-…, 0028/AC-…` |
| `tests/Host.Tests/CalismaSaatleriZinciriTests.cs:226`, `:282`, `:326`, `:404` (`0025/AC-24`, `AC-26`, `AC-30`, `AC-52`, `AC-57`, `AC-58`, `AC-72`) | `DepartmentAsync` `/tr/` belgesini okur | yardımcı `/tr/contact` belgesini okur (`:507`). Beklenen değerler değişmez |
| `tests/Host.Tests/OzelGunZinciriTests.cs:563`, `:573`, `:588`, `:612` (`0027/AC-62–AC-66`) | `/tr/` ve `/ar/` belgesinde `specialOpeningHoursSpecification`, 4608 B | aynı ölçüm `/{l}/contact` belgesinde (`DepartmentAsync`, `:739`). AC-64'ün "kök `AutoDealer`" ve AC-65'in "şu an yok" ölçümü iki belgede de |
| `tests/Host.Tests/GaleriProfiliTohumuTests.cs:91`, `:114` | tohumlu `/tr/` belgesinde `department` öğelerinin anahtar sayısı (21, 36) | ana sayfada anahtar sayısı düşer. Aynı sayılar iletişim belgesinde, e-posta/`hasMap`/`geo` ile yeniden hesaplanır. Test bunu kodla ölçmeli, sabit sayıyla değil |
| `tests/Host.Tests/SeoBelgeTests.cs:580` (`0004/AC-52`, `0024/AC-45`, `0025/AC-55`, `0026/AC-52`) | ana sayfada `geo` yok | **ana sayfa için değişmez.** İletişim belgesinde `geo` koordinatlı şubede **var** (**AC-15**). Etiket ve yorum güncellenir |
| `tests/Host.Tests/SeoBelgeTests.cs:616` (`0004/AC-54`) | liste, detay ve içerik belgesinde `AutoDealer` yok | **değişmez.** İletişim yeni bir türdür, içerik sayfası değildir |
| `tests/Host.Tests/SitemapTests.cs:63` (`0004/AC-56`) | `sitemap-tr.xml` adres kümesi tam | küme `{kök}/tr/contact` ile genişler (**AC-11**). Çok etiketli |
| `tests/Host.Tests/FavoriKarsilastirmaBelgeTests.cs:90` (`frontend-003/AC-7`) | robots.txt yalnız `/api/`'yi kapatır | **değişmez** |
| `frontend/src/features/dealership/BranchList.test.tsx:88`, `:112`, `:143`, `:155` (`0025/AC-44–AC-48`, `0027/AC-48`) | altlıkta yedi satır, "şu an" | saat listesi **iletişim sayfasının** bileşeninde. Testler oraya taşınır, beklenen metinler değişmez. Altlık testi **saatin yokluğunu** ölçer (**AC-58**) |
| `frontend/src/features/dealership/BranchList.test.tsx:135`, `:163` (`0025/AC-46`, `AC-50`, `AC-51`) | programsız şube; okuma sürerken `0014` iskeleti, hata verince blok yok | **değişmez** (altlık) |
| `frontend/src/features/dealership/SuAnSatiri.test.tsx` (`0027/AC-48–AC-61`, `AC-79–AC-82`, `AC-100`, `AC-102`, `AC-105`, StrictMode) | `BranchList` üstünden altlık | aynı testler iletişim sayfasının şube bölümüyle koşar (**AC-44**). Beklentiler değişmez |
| `frontend/src/features/dealership/SuAnSatiri.test.tsx:274` (`0027/AC-68` birim yarısı, evre B) | altlıkta evre B iskeleti | **kalkar.** İletişim sayfası saat koduyla tek parçada gelir ve evre B yoktur (D11). `0027` AC-68, AC-75–AC-77 **geçersiz** olur. Karar defterine yazılır |
| `frontend/e2e/calisma-saatleri.spec.ts:173`, `:238` (`0025/AC-44–AC-49`) | `/tr/` altlığında `branch-hours` | aynı akış `/tr/contact`'ta. Beklentiler değişmez |
| `frontend/e2e/ozel-gunler.spec.ts:276` (`0027/AC-62`, `AC-65`, `AC-66` ikizi) | ilk `/tr/` belgesinin yapısal verisi | ilk `/tr/contact` belgesi |
| `frontend/e2e/ozel-gunler.spec.ts:402`, `:459`, `:633` (`0027/AC-48`, `AC-53–AC-58`, `AC-69`, `AC-101`) | altlıkta "şu an", "Bugün", "Özel günler", iki satırlık kutu | iletişim sayfasında. Kutu ölçümü yeni sütun genişliğiyle (tuzak 10) |
| `frontend/e2e/ozel-gunler.spec.ts:524` (`0027/AC-68`, `AC-75–AC-77`) | evre B iskeleti eşitliği | **kalkar** (yukarıdaki satır) |
| `frontend/e2e/ozel-gunler.spec.ts:720`, `:751` (`0027/AC-71`, `AC-78`) | ana sayfa altlığında layout-shift yok; altlık ilk ekranın altında | ana sayfa için **kalır** (altlık kısalır, ölçüm geçerli). İletişim sayfası için yeni ölçüm **AC-77** |
| `frontend/src/app/admin/GaleriPaneli.test.tsx:1048-1056` (`0025/AC-63`, `0027/AC-73`) | vitrin kataloğunda `home.*` içinde `/closed\|hours/` | `home.footer.*` saat anahtarları iletişim kümesine taşınırsa küme değişir. Uygulama hangi kümeye koyduğunu bu satıra yazar (D14) |
| `frontend/src/app/HomePage.test.tsx:261`, `:277`, `:299`, `frontend/e2e/evidence-0014.spec.ts:63` (`0014` iskeleti) | galeri okunurken `branch-skeleton` | **değişmez** |
| `frontend/src/app/AnaSayfaMetinleri.test.tsx:181-188`, `frontend/e2e/evidence-0022.spec.ts:290` (`0022/AC-11`) | "Galeriye ulaşın" metni | metin **değişmez**. Hedef `#contact` → `/{l}/contact` (**AC-72**) |
| `frontend/src/app/TopBar.test.tsx` ve `frontend-003`'ün üst çubuk e2e'leri (D8: iki satır, 320 px) | satır sonu grubu sayaç + dil | üst çubukta bir bağlantı daha (D10). Ölçüm bağlantıyla yeniden koşar (**AC-68**) |

> **e2e ikizi:** Yukarıdaki her Host satırının e2e karşılığı arandı. Belgenin yapısal verisini ölçen tek
> e2e `ozel-gunler.spec.ts:276` ve tabloda. Şube öğesinin anahtar kümesini ölçen e2e yok:
> `evidence-0020.spec.ts:18` yalnız kökü ölçüyor ve kök değişmiyor. Sitemap'i ölçen e2e yok. Uygulamada
> yeni bir küme testi eklenirse ikizi bu tabloya eklenir.

---

## Verilen kararlar (öneri — Proje Yöneticisi onayı bekliyor)

Biçim: **karar** · *gerekçe* · *bedel*.

**D1 — Kapsam.** (R1–R11, X1)
- Yeni bir iletişim sayfası açılır.
- Saat bloğu (`0027` D1'in "yalnız altlık" kapsamı) altlıktan bu sayfaya **taşınır**.
- Altlık sadeleşir.
- Bütün vitrin sayfalarına bir gezinme bağlantısı eklenir.
- İletişim formu yoktur.

*Gerekçe:* sahibin isteği. Saat ve harita bir aracı görmeye gitmeden önceki soruların cevabı ve tek
yerde durmalı. *Bedel:* `0027` D1'in "yalnız altlık" kararı ve altlığa bağlı dört kriteri (AC-68,
AC-75–AC-77) geçersiz olur (Context tablosu).

**D2 — Adres: `/{locale}/contact` (SS3).** (R1, C8, tuzak 1)
- Ad her dilde aynıdır ve çevrilmez (`cars`, `newsletter`, `favorites` gibi).
- `ContentReservedSlugs.All`'a ve uçtaki ayna listeye eklenir. Bu adla içerik sayfası açılamaz.
- Sunucu `SeoPageKind.Contact` tanır.
- Alt yolları (`/tr/contact/x`) `0004` D8 gereği 404 + "bulunamadı"dır.

*Gerekçe:* projenin kuralı sabit segment (frontend-003 D1). Dil başına ad (`/tr/iletisim`,
`/ar/اتصل-بنا`, `/en/contact`) üç ayrılmış ad, dil başına sınıflandırma ve yüzde kodlu bir Arapça yol
demek. Ayrıca dil değiştiricinin yolu koruması bozulur (`LocaleSwitcher.tsx:47` yolu aynen taşır).
*Bedel:* TR ve AR adreslerinde İngilizce bir sözcük görünür. Arama sonucunda adres, başlık kadar etkili değil.

**D3 — Sayfanın içeriği ve yerleşimi.** (R1, R2, R10)
- Tek `<h1>`: "İletişim" / "اتصل بنا" / "Contact".
- Her şube bir `<section>`, başlığı `<h2>` şube adı. Sıra sunucunun sırasıdır (`0014` D13).
- Bölüm içi sıra:
  1. "Şu an" satırı;
  2. adres;
  3. telefon (`tel:`);
  4. e-posta (`mailto:`, D8);
  5. harita kutusu (D6);
  6. "Google Haritalar'da aç";
  7. haftalık liste;
  8. "Özel günler".
- 64rem (1024 px) ve üstünde bölüm iki sütundur: bilgiler ve saatler satır başında, harita satır
  sonunda (ar'da solda). Altında tek sütundur.
- Çok şubede bölümler alt alta durur. Aralarında ayraç vardır, sekme ya da açılır liste yoktur.

*Gerekçe:* bütün bilgi bir bakışta görülür. Galerilerin çoğu 1–3 şubeli. Sekme bilgiyi gizler ve
ekran okuyucuda ek bir etkileşim ister. *Bedel:* 5+ şubede sayfa uzar. Harita kutuları uzunluğu artırır.

**D4 — Durumlar.** (R5, R11, X11)
- **Yükleniyor:** `<h1>` hemen basılır. Şube alanında iskelet ve `role="status"` vardır.
- **Hata:** `ErrorState` (`shared/ui`), çevrilmiş metin ve "Tekrar dene" düğmesi. Düğme okumayı
  tekrarlar. Altlıktan farklı olarak bu sayfanın tek içeriği şubelerdir, yani boş yer bırakmak yerine
  hata gösterilir.
- **Boş** (aktif şube yok): `EmptyState`, "İletişim bilgisi henüz eklenmedi".
- **Programsız şube:** "şu an" satırı, haftalık liste ve özel günler yoktur (`0027` D11, AC-57). Ad,
  adres, telefon, e-posta ve harita vardır.
- **E-postasız şube:** e-posta satırı yoktur.
- **Geçici kapalı ve kapatılmış şube:** ziyaretçi okumasında yoktur, sayfada da yoktur (X11).

*Gerekçe:* `docs/frontend.md` § 3. *Bedel:* yok.

**D5 — Ziyaretçi okuması: şube öğesine `email` ve `map`.** (R2, R3, C2)
- Aynı uç kullanılır: `GET /api/dealership/gallery`.
- `email`: kayıtlı adres ya da `null`.
- `map`: `{ "linkUrl": string, "embedUrl"?: string }`. İki adres de sunucuda, tek yerde kurulur (D7).
  *(PM şartı (a):)* Anahtar yokken `embedUrl` anahtarı **hiç yoktur**, `null` değildir.
- Ham koordinat, saat dilimi, WhatsApp ve slug gönderilmez.
- `no-store` kalır (`0027` D8).

*Gerekçe:* ikinci bir uç, ikinci bir izin listesi ve aynı "şu an" hesabının ikinci bir yolu demek.
`0027`'nin yenileyicisi bu okumayı zaten tekrarlıyor (`branchClock.tsx`). Adresleri sunucunun kurması,
anahtarı (D7) ve ondalık kuralını (tuzak 6) tek yerde tutar. *Bedel:* ana sayfa da şube başına
≈150 B fazla JSON alır (kullanmadığı iki alan). `0020` D15'in izin listesi ikinci kez genişler.

**D6 — Harita: tıkla-yükle + her zaman bağlantı (SS1 önerisi (b)+(c)).** (R3, R4, C4, tuzak 4, 8)
- `embedUrl` doluysa şube bölümünde sabit oranlı (**4:3**) bir **yer tutucu** basılır. İçinde şunlar
  vardır:
  - bir **"Haritayı göster"** düğmesi (`<button type="button">`);
  - kısa bir bilgi metni: "Harita Google Haritalar'dan yüklenir. Google çerez kullanabilir."
- Yer tutucu yalnız yerel CSS'tir: görsel, yazı tipi dışı kaynak ya da dış istek yoktur.
- Düğmeye basılınca **aynı kutuda** iframe açılır (`src` = `embedUrl`). Kutunun boyutu değişmez. Odak
  iframe'e geçer.
- Her şubenin düğmesi yalnız kendi haritasını açar.
- *(PM şartı (b):)* Tıklama **kalıcı bir onay değildir.** Yalnız o sayfa görüntülemesi için geçerlidir.
  Sayfa yenilenince ya da yeniden açılınca bütün kutular yer tutucuya döner. Çerez, `localStorage`,
  `sessionStorage` ya da IndexedDB'ye hiçbir şey yazılmaz (X7, AC-57).
- "Google Haritalar'da aç" bağlantısı her şubede, `embedUrl` olsun olmasın basılır (`href` = `linkUrl`).
  Aynı sekmede açılır. Bu, `0024`'ün sosyal bağlantılarıyla aynıdır (`SocialLinksSlot.tsx:57`, `target` yok).

*Gerekçe:* Context tablosu. Sahibin "harita olsun"u bir tıkla karşılanır. Ziyaretçinin onayı olmadan
Google'a istek gitmez (C4). Çerez politikası "harita açılınca Google çerez kullanabilir" diye
yazılabilir, onay bandı gerekmez. *Bedel:* harita ilk bakışta görünmez, bir tık ister. Her ziyarette
yeniden tıklanır.

**D7 — Harita adresleri.** (R3, tuzak 5, 6)
- **Sorgu:** şubenin koordinatı varsa `"{enlem},{boylam}"`. Değerler `InvariantCulture` ile, nokta
  ondalıkla ve sondaki sıfırlar atılarak yazılır. Koordinat yoksa **varsayılan dildeki** adres
  kullanılır. Sorgu URL kodlanır.
- `linkUrl` = `https://www.google.com/maps/search/?api=1&query={sorgu}`. Anahtar gerekmez.
- `embedUrl` = `https://www.google.com/maps/embed/v1/place?key={anahtar}&q={sorgu}&language={istek dili}`.
- **Anahtar** kurulum yapılandırmasından okunur. Adı `deploy.md`'de yazılır, ör. `GoogleMaps__EmbedApiKey`.
  Kodda, depoda ve tohumda anahtar yoktur.
- Anahtar yoksa ya da boşsa (yalnız boşluk da boş sayılır) yanıtta `embedUrl` anahtarı **yoktur**.
  Kutu ve düğme basılmaz, yalnız bağlantı kalır (tuzak 5).
- *(PM şartı (a):)* Anahtar sır değildir, ama yalnız `embedUrl`'in içinde bulunur. Bu adres de yalnız
  ziyaretçi düğmeye basınca ağa çıkar. Anahtar hiçbir belgeye (`<head>`, `<meta>`, JSON-LD, kabuk),
  hiçbir başka yanıta ve hiçbir günlük kaydına yazılmaz. Yapılandırma okunurken, adres kurulurken ya da
  bir hata olduğunda da yazılmaz (AC-82, AC-83).

*Gerekçe:* koordinat yazım farkından etkilenmez. Varsayılan dildeki adres her şubede zorunlu
(`0003` AC-6). Arapça adresin Google'da bulunması garanti değil. Resmî Embed API ve resmî Maps URLs
kullanılır, anahtarsız gömme kullanılmaz (Context). *Bedel:* koordinat girilmemişse iğne adresin
yazımına bağlıdır. Harita için sahibin bir Google Cloud projesi, faturalandırma hesabı ve kısıtlı
anahtar açması gerekir (**SS1**).

**D8 — E-posta: şubenin bugünkü alanı (SS2 önerisi (a)).** (R2)
- Şubenin e-postası iletişim sayfasında `mailto:` bağlantısıyla basılır. Metin adresin kendisidir,
  `dir="ltr"`.
- Altlıkta e-posta yoktur (altlık kısalıyor, D9).
- Panelde değişiklik yoktur. Alanın yanına bir yardım cümlesi eklenir: "Vitrinin iletişim sayfasında
  görünür; talep bildirimleri de bu adrese gider." Panel kataloğuna girer.

*Gerekçe:* sahip e-postayı adres ve telefonun yanında, yani şube bilgisi olarak sıraladı. Alan
zaten var ve doğrulanıyor (`0003` AC-10, `0020` AC-31). Migration yoktur. *Bedel:* bildirim kutusu ile
herkese açık adres aynıdır (tuzak 3). Ayrı bir "herkese açık e-posta" istenirse yeni alan ve
migration gerekir (SS2 (b)).

**D9 — Altlık sadeleşir.** (R6, R7, **SS4**)
- Şube öğesinde ad, adres ve telefon kalır (`0014` R7).
- Altlıktan şunlar kalkar:
  - "şu an" satırı ve iki satırlık kutusu (SS4 önerisi: **kalksın**);
  - haftalık liste;
  - özel günler;
  - evre B iskeleti;
  - saat parçasının bağı.
- Şube bloğunun sonuna bir bağlantı eklenir: "İletişim ve çalışma saatleri" /
  "التواصل وساعات العمل" / "Contact and opening hours". Hedefi `/{l}/contact`.
- Ana sayfada D9 yenileyicisi yoktur, galeri okuması bir kez yapılır.
- `0014`'ün iskeleti (evre A) ve hata/boş davranışı aynen kalır.
- `id="contact"` altlıktan kalkar (D17).
- *(PM şartı (c):)* **Sütun yerleşimi düzelir.**
  - Geniş ekranda (≥64rem), beş blok da varken şube bloğu ve dört gezinme bloğu ("Keşfet", "Kurumsal",
    "Bizi takip edin", "Diller") **tek satırda** durur. Şube bloğu satır başındadır. Logo kendi satırında
    kalır. Olmayan blok yer tutmaz. Bugün 1280 px'te "Diller" alt satıra düşüyor (Context 3).
  - Dar ekranda (320 px) bloklar DOM sırasıyla tek sütundur.
  - Izgaranın nasıl kurulacağı uygulamanın plan kararıdır (ör. şube sütunu + `auto-fit` gezinme
    sütunları ya da daha küçük bir sütun alt sınırı). Ölçü AC-85 ve AC-86'dır.
- *(PM şartı (c):)* Altlığın CLS'i fontlar bekletilerek ölçülür (`0027` dersi, AC-87). Ana sayfanın C3
  medyanı PR'da `main` ile yan yana yazılır (AC-76).

*Gerekçe:* sahibin şikâyeti altlığın boyu. "Şu an" satırı, şube başına iki satırlık sabit yer ve bir
yenileyici demek. Kalırsa ana sayfa saat parçasını (≈2,4 KB) indirmeye ve galeriyi yenilemeye devam
eder. *Bedel:* ana sayfadaki ziyaretçi açık/kapalı bilgisini görmek için bir bağlantıya tıklar.
**SS4 "kalsın" derse:** satır, kutusu ve yenileyici altlıkta kalır (`0027` D9–D11 aynen). Saat parçası
ana sayfada yüklenmeye devam eder. AC-58'in "şu an" yarısı, AC-60 ve AC-61 tersine döner. `0027`
AC-68/AC-75–AC-77'nin evre B'si "şu an" kutusuyla sınırlı kalır.

**D10 — Üst çubukta "İletişim" bağlantısı.** (R7, Context 6, tuzak 7)
- Bağlantı her vitrin sayfasında bulunur (ana sayfa, liste, detay, içerik, favoriler, karşılaştırma,
  bülten, bulunamadı). Panelde yoktur, çünkü panelin kabuğu ayrı.
- **Geniş ekranda** (iki satır eşiğinin üstünde) satır sonu grubunda, sayaçtan önce, metinli bir
  bağlantıdır.
- **Dar ekranda** birinci satırın sonunda, markanın yanında **simge** bir bağlantıdır. Erişilebilir
  adı "İletişim"dir, dokunma hedefi `--touch-target-min-size`'dır. İkinci satırda ≈29 px kalıyor
  (Context 6).
- İletişim sayfasında `aria-current="page"` taşır.
- Payı yetmezse sıra şöyledir:
  1. dar ekranda logonun azami genişliği 120 px'e iner (`frontend-003` D8'in onaylı adımı);
  2. iki satır eşiği 40rem'den büyütülür. Değeri ölçülür, en çok 48rem.

*Gerekçe:* altlık yalnız ana sayfada. Liste ve detaydaki ziyaretçinin iletişime ulaşacağı tek kalıcı
yer üst çubuk. Simge, dar ekranda metne yer olmadığı için seçildi. *Bedel:* dar ekranda markanın
adına ≈52 px daha az yer kalır. Geniş ekranda iki satıra geçiş eşiği büyüyebilir.

**D11 — Saat bloğunun taşınması.** (R2, C6, tuzak 9, 10)
- `0027`'nin şu davranışları iletişim sayfasının şube bölümünde **aynen** geçerlidir: "şu an" satırı
  ve metinleri (D10), iki satırlık sabit kutu (D11), "Bugün" vurgusu (D11), "Özel günler" listesi
  (D11), yenileme (D9). Yenilemenin kapsamı: tek okuma, `performance.now()`, görünürlük, hata
  durumunda metnin kalkması, 60 sn yeniden deneme, geçersiz süre, 90000 sınırı ve listelerin sabit
  kalması.
- İletişim sayfasının kodu ve saat kodu **tek tembel parçadadır**. Sayfa, parça ve veri hazır olunca
  bir kerede basılır. **Evre B yoktur.** `0027` AC-68 ve AC-75–AC-77 geçersizdir.

*Gerekçe:* C6. Davranış değişmez, yalnız yeri değişir. Sayfanın tek içeriği şubeler olduğu için ara
evre gereksiz. *Bedel:* sayfa ilk ziyarette bir parça isteği daha bekler. Bu parça ana sayfa için
indirilen parçanın yerini alır.

**D12 — Belge (SEO).** (R8, C8)
- Belge `200` döner.
- Başlık: `WithName(seo.contact.title, ad)`, ör. "İletişim · {görünen ad}". Ad okunamazsa yalnız
  katalog başlığıdır.
- Açıklama: `seo.contact.description` (sabit katalog metni: "Şubelerimizin adresi, telefonu,
  e-postası, çalışma saatleri ve haritası.").
- Kanonik `{kök}/{l}/contact`, sorgu dizesi taşımaz.
- Etkin her dil için `hreflang` ve `x-default` (varsayılan dil) vardır. `og:title`, `og:description`,
  `og:url`, `og:type website` ve logo varsa `og:image` vardır (`0026`).
- `robots` etiketi yoktur, `Referrer-Policy` başlığı yoktur (özel sayfa değil).
- Belge `no-store`'dur (`0004` AC-82).
- Üç dilin sitemap'i `/{l}/contact` girdisini taşır. `robots.txt` değişmez.

*Gerekçe:* `0004` D11, D15. Sayfa ziyaretçinin değil galerinin içeriği; aranabilir olmalı.
*Bedel:* yok.

**D13 — Yapısal veri ikiye ayrılır.** (R9, C3, tuzak 2)
- **İletişim belgesi** tam olarak bir JSON-LD bloğu taşır.
  - Kök: `@type AutoDealer`, `name` (görünen ad), `url` = `{kök}/{l}/` (galerinin ana sayfası),
    logo varsa `logo` (`0026` D7).
  - **`sameAs` yoktur** (X10).
  - `department[]` vitrindeki her şube içindir: `@type AutoDealer`, `name`, `address`, `telephone`.
    Ek anahtarlar:
    - e-posta varsa `email`;
    - programlıysa `openingHoursSpecification` (`0025` D9);
    - penceredeki özel gün varsa `specialOpeningHoursSpecification` (`0027` D12);
    - her zaman `hasMap` = `linkUrl`;
    - koordinat varsa `geo` (`@type GeoCoordinates`, `latitude`, `longitude` JSON sayısı).
  - "Şu an" bilgisi, `embedUrl` ve anahtar belgede yoktur.
- **Ana sayfa belgesi:** `department` öğeleri yalnız `@type`, `name`, `address`, `telephone` taşır.
  `openingHoursSpecification` ve `specialOpeningHoursSpecification` kalkar. Kök değişmez (`sameAs`,
  `logo` kalır). Ana sayfada `geo` yine yoktur (`0004` AC-52 ana sayfa için geçerli).
- **Bütçe** `0025` D10(a) ve `0027` D12'den taşınır. Tek şubeli kurulumda şu belge ölçülür: `/ar/contact`.
  - En kötü haftalık program, belgeyi programsız hâlinden en çok **2304 B** büyütür.
  - 12 özel gün × 3 aralık, özel günsüz hâlinden en çok **4608 B** büyütür.

*Gerekçe:* C3. Harita ve koordinat bu sayfada görünür, `0004`'ün `geo`'yu dışarıda bırakma gerekçesi
("koordinat vitrinde gösterilmiyor") burada kalkar. *Bedel:* aynı işletme iki belgede iki farklı
ayrıntıda anlatılır. İkisi de kendi sayfasında görüneni söyler.

**D14 — Paket ve performans.** (C5)
- İlk paket `main` tabanına göre en çok **+512 B** büyür (`0024` AC-55; ölçüm head commit'te).
  Girenler: rota, üst çubuk bağlantısı ve simgesi, altlık bağlantısı ve bunların metinleri. Çıkanlar:
  altlığın saat parçası bağı ve evre B iskeleti kodu.
- İletişim sayfası, harita kutusu ve saat kodu tek tembel parçadadır (D11). Ana sayfa, liste ve
  detay bu parçayı istemez.
- Sayfanın yeni metinleri (başlık, durumlar, harita bilgi metni, düğme, iframe başlığı) ilk paketi
  +512 B'ın üstüne çıkarırsa `frontend-003` D13'ün üçüncü küme yoluyla parçaya iner (`registerCatalogs`).
  Hangi kümede olduğu PR'a yazılır.
- Kritik yol değişmez: `index.html`'in `modulepreload` listesi ve girişin statik içe aktardığı dosya
  sayısı `main` ile aynıdır (`frontend-003` D13).
- `perf.yml` `/tr/contact`'ı da ölçer. C3 eşikleri geçerlidir.
  - Perf tohumu her aktif şubeye koordinat, e-posta ve `0027`'nin 12 özel gününü verir.
  - Harita anahtarı sahte bir değerle yapılandırılır. Yer tutucu görünür, ağa istek gitmez.
- Ana sayfa, liste, detay ve iletişim sayfasının C3 medyanları PR'da `main` ile yan yana yazılır.
- CLS fontlar bekletilerek ölçülür (`frontend-003` dersi).

*Gerekçe:* `0024`, `0027` D15 deseni. İletişim yeni bir indekslenen sayfa, C3'e girmeli. *Bedel:*
perf koşusu bir sayfa uzar.

**D15 — Gizlilik.** (R4, C4, tuzak 4)
- Düğmeye basılmadan hiçbir üçüncü taraf alan adına istek yapılmaz: `preconnect`, `dns-prefetch`,
  görsel, betik ve yazı tipi yoktur. Ölçü ağ kaydıdır.
- iframe `referrerpolicy="strict-origin-when-cross-origin"` taşır: Google'a yalnız köken gider. Bu,
  anahtarın yönlendiren kısıtı için gereklidir. Sayfa düzeyinde `Referrer-Policy` başlığı yoktur.
- iframe'de `allow` izni yoktur (konum, kamera vb.). `sandbox` uygulamanın plan kararıdır: haritanın
  çalıştığı ve "daha büyük haritada aç"ın açıldığı ölçülür.

*Gerekçe:* `frontend-002` D8'in ilkesi. *Bedel:* yok.

**D16 — Erişilebilirlik ve RTL.** (R5, R10)
- iframe `title`: "{şube adı} — Google Haritalar haritası" (çevrili).
- Adres her durumda metindir: yer tutucu, iframe, anahtarsız kurulum ve harita yüklenemese de.
- Düğme klavyeyle (Enter, Space) çalışır. Tıklamadan sonra odak iframe'dedir.
- `/ar/contact` `dir="rtl"` dizilir. Telefon, e-posta, saat ve tarih `dir="ltr"` ve Latin rakamla
  basılır (`0025` D7, `0027` R12). Ayın aynı günündeki aralıklar LRI…PDI ile yalıtılır (`0027` uygulama
  içi bulgu).
- Durum yalnız renkle verilmez (`0027` R13).

*Gerekçe:* `docs/frontend.md`. *Bedel:* yok.

**D17 — Boş vitrinin eylemi ve `#contact`.** (R7, Context 3)
- "Galeriye ulaşın" bağlantısı `/{l}/contact`'a gider. Ana sayfada ve listede aynıdır. Metin değişmez
  (`0022` AC-11).
- Altlığın `id="contact"`'ı kalkar.

*Gerekçe:* listede bugün ölü bir bağlantı (altlık yok). *Bedel:* yok.

**D18 — Belgeler.**
- `docs/architecture.md` Karar Defteri:
  - vitrin adresleri satırına `/{l}/contact` ve ayrılmış ad;
  - "Saat bloğu ve 'şu an' iletişim sayfasında; altlıkta ad, adres, telefon (`0028`; `0027` D1'in
    yerini alır)";
  - "Harita: tıkla-yükle, vitrinin tek üçüncü taraf isteği ve ziyaretçinin eylemiyle."
- `docs/deploy.md`:
  - **harita anahtarı**: yapılandırma adı; Google Cloud'da Maps Embed API'yi etkinleştirme; anahtarı
    **HTTP yönlendiren** (site kökü) ve **yalnız Maps Embed API** ile kısıtlama; anahtar yoksa
    haritanın basılmadığı;
  - **CSP**: ters vekil bir CSP eklerse `frame-src https://www.google.com` gerekir;
  - *(PM şartı (a):)* "Anahtar HTTP yönlendiren (referrer) ile site köküne kısıtlanır ve yalnız Maps
    Embed API'ye açılır." Anahtar sır değildir, ama kısıtsız anahtar başka sitelerde kullanılabilir;
  - *(PM şartı (d))* **sahibe not**: "Harita tıklanınca Google çerez bırakabilir. Çerez politikası
    buna göre güncellenmelidir (sahip işi)." Bugünkü örnek metin "üçüncü taraf çerez yok" diyor
    (Context 7, X8);
  - perf tohumunun koordinat ve e-posta cümlesi.
- `docs/domain.md`: yeni terim yok ("İletişim sayfası" bir sayfa, domain kavramı değil).

*Gerekçe:* tek gerçek kaynağı. *Bedel:* yok.

**D19 — Saklama: yeni alan, migration yok.** (C1) E-posta ve koordinat şubede ve panelde var (Context
1, 2). Harita anahtarı veritabanında değil, kurulum yapılandırmasındadır (D7). *Gerekçe:* SS2 (a) ve D7.
*Bedel:* SS2 (b) ya da (c) seçilirse bu karar değişir: yeni alan, migration ve panel değişikliği gerekir.

---

## Sahibe sorular

> **Cevaplandı (sahip kararı, 2026-11-27):** SS1 (b)+(c), SS2 (a), SS3 (a), SS4 (a). Dördü de önerilen
> seçenek. Aşağıdaki metin sorulduğu hâliyle korunur.

**SS1 — Harita nasıl gösterilsin?** (D6, D7) Bu kartın en kritik sorusu.
- **(b)+(c) — Tıklayınca yüklenen harita + her zaman "Google Haritalar'da aç" bağlantısı (öneri).**
  Sayfada önce haritanın yerini tutan bir kutu ve "Haritayı göster" düğmesi durur. Ziyaretçi düğmeye
  basınca Google haritası aynı yerde açılır. Basmadan Google'a hiçbir istek ve çerez gitmez.
  *Gerekçe:* "harita olsun" isteği karşılanır, KVKK ve çerez politikası basit kalır, sayfa hızı
  korunur. *Bedel:* harita bir tık sonra görünür.
- **(a) — Sayfa açılınca harita hemen görünsün.** Her ziyaretçi için Google'a istek ve Google
  çerezi gider. Çerez politikasında ve muhtemelen bir onay bandında yazılması gerekir. Sayfa yavaşlar.
- **(c) — Harita olmasın, yalnız "Google Haritalar'da aç" bağlantısı.** En basit ve en güvenli yol,
  ama "harita olsun" isteğini karşılamaz.
- **Her iki harita seçeneğinde (a, b) gereken:** Google'ın resmî gömme yolu için bir **Google Cloud
  hesabı, faturalandırma hesabı ve bir API anahtarı** açılır. Google bu kullanım için ücret almıyor ve
  sınır koymuyor, ama faturalandırma hesabı istiyor. Anahtar yokken sayfada yalnız bağlantı görünür.
  Anahtarsız gömme yolu resmî değil ve bozulabiliyor, önermiyoruz.
- **Not:** şubenin enlem/boylamı panelde girilirse harita tam noktayı gösterir. Girilmezse Google
  adresi arar.

**SS2 — E-posta hangi adres olsun?** (D8, D19)
- **(a) — Her şubenin panelde zaten girilen e-postası (öneri).** Yeni alan yok. *Bedel:* aynı adrese
  bugün talep bildirimleri de gidiyor. Bu adres herkese açık olur ve istenmeyen e-posta alabilir.
- **(b) — Her şubeye ayrı bir "sitede görünen e-posta" alanı.** Bildirim adresi gizli kalır.
  *Bedel:* yeni alan, panel ve veritabanı değişikliği.
- **(c) — Galerinin tek bir genel e-postası (ör. info@…).** Bütün şubelerde aynı adres görünür.
  *Bedel:* galeri profiline yeni alan. Şube başına adres gösterilmez.
- Her durumda adres tıklanınca e-posta programı açılır (`mailto:`).

**SS3 — Sayfanın adresi ne olsun?** (D2)
- **(a) — Her dilde `/tr/contact`, `/ar/contact`, `/en/contact` (öneri).** Sitenin diğer sabit
  sayfalarıyla aynı kural (`/tr/cars`, `/tr/favorites`).
- **(b) — Dile göre: `/tr/iletisim`, `/ar/…`, `/en/contact`.** Türkçe adres daha doğal okunur.
  *Bedel:* üç ayrılmış ad, Arapça adreste yüzde kodlu karakterler, dil değiştiricide ek kural.

**SS4 — Ana sayfa altlığında "Şu an açık · Kapanış 18:00" satırı kalsın mı?** (D9)
- **(a) — Kalksın. Altlıkta ad, adres, telefon ve "İletişim ve çalışma saatleri" bağlantısı (öneri).**
  Altlık en kısa hâline döner, ana sayfa daha az kod indirir. *Bedel:* açık/kapalı bilgisi bir tık uzakta.
- **(b) — Kalsın.** Her şubenin adının altında satır durmaya devam eder. *Bedel:* şube başına iki
  satır yer ve ana sayfada saat kodunun yüklenmesi sürer.

---

## Acceptance Criteria

> Saatler: **Saat** = Host testinde sabit `IClock`. Örnek şubeler: "Kadıköy Şubesi" (koordinatlı
> 40.98765, 29.0275; e-postalı `kadikoy@otogaleri.com.tr`), "Bostancı Şubesi" (koordinatsız,
> e-postasız). Harita anahtarı testte `test-anahtar` diye yapılandırılır.

### Adres ve belge (R1, R8 · D2, D12)

- **AC-1.** `/tr/contact`, `/ar/contact` ve `/en/contact` istekleri `200` döner. Uç iletişim sayfasını
  basar ve sayfanın tek `<h1>`'i sırasıyla "İletişim", "اتصل بنا", "Contact"tır.
- **AC-2.** `/tr/contact/x` ve `/tr/contact/kadikoy` istekleri `404` döner ve uç "bulunamadı" basar
  (`0004` D8).
- **AC-3.** Panelde tr slug'ı `contact` olan bir içerik sayfası kaydı, bugünkü ayrılmış ad hatasıyla
  reddedilir. `ContentReservedSlugs.All` `contact`'ı içerir.
- **AC-4.** `/tr/contact` belgesinin `<title>`'ı `{tr seo.contact.title} · {tr görünen ad}`'dır.
  Görünen ad okunamadığında yalnız katalog başlığıdır.
- **AC-5.** `/ar/contact` belgesinin `<meta name="description">` değeri ar katalogdaki
  `seo.contact.description` metnidir.
- **AC-6.** `/tr/contact?utm_source=x` belgesinin kanoniği `{kök}/tr/contact`'tır.
- **AC-7.** `/tr/contact` belgesi etkin her dil için bir `hreflang` alternatifi (`{kök}/{dil}/contact`)
  ve `x-default` = `{kök}/tr/contact` taşır. Etkin diller `tr, en` iken `ar` alternatifi yoktur.
- **AC-8.** `og:title` başlıkla, `og:description` açıklamayla, `og:url` kanonikle aynıdır ve
  `og:type` `website`'tır. Belgede `robots` etiketi yoktur.
- **AC-9.** Yanıt `Cache-Control: no-store` taşır ve `Referrer-Policy` başlığı taşımaz.
- **AC-10.** Belgenin `<title>`'ında ve `og:site_name`'inde `CarGallery` geçmez (`0004` AC-15'in
  listesine iletişim eklenir).
- **AC-11.** `sitemap-tr.xml`, `sitemap-ar.xml` ve `sitemap-en.xml` sırasıyla `{kök}/tr/contact`,
  `{kök}/ar/contact`, `{kök}/en/contact` girdisini taşır. `robots.txt` değişmez.

### Yapısal veri (R9 · C3 · D13)

- **AC-12.** `/tr/contact` belgesi tam olarak bir `<script type="application/ld+json">` taşır.
  `@type`'ı `AutoDealer`, `name`'i tr görünen ad, `url`'i `{kök}/tr/`'dir.
- **AC-13.** Kadıköy'ün `department` öğesi `@type AutoDealer`, tr ad, tr adres, telefon,
  `email` = `kadikoy@otogaleri.com.tr` ve `hasMap` = Kadıköy'ün `linkUrl`'i taşır.
- **AC-14.** Programlı Kadıköy'ün öğesi `0025` AC-52–AC-54'ün `openingHoursSpecification`'ını taşır.
  Penceredeki özel günleri `0027` AC-62'nin `specialOpeningHoursSpecification`'ıyla taşır. Beklenen
  değerler o kriterlerinkiyle aynıdır.
- **AC-15.** Kadıköy'ün öğesi `geo` = `{ "@type": "GeoCoordinates", "latitude": 40.98765,
  "longitude": 29.0275 }` taşır. Değerler JSON sayısıdır.
- **AC-16.** Bostancı'nın öğesi `geo` ve `email` taşımaz, `hasMap` taşır. Programsız bir şubenin
  öğesi `openingHoursSpecification` ve `specialOpeningHoursSpecification` taşımaz.
- **AC-17.** `/tr/` belgesinin her `department` öğesinin anahtar kümesi tam olarak `@type`, `name`,
  `address`, `telephone`'dur. Programlı ve özel günlü şubede de böyledir.
- **AC-18.** Tek şubeli kurulumda:
  - en kötü programlı şubenin `/ar/contact` belgesi, aynı şubenin programsız hâlinden en çok
    **2304 B** büyüktür;
  - 12 kayıt × 3 aralığın hepsi penceredeyken `/ar/contact` belgesi, özel günsüz hâlinden en çok
    **4608 B** büyüktür.
- **AC-19.** İletişim belgesinin kökü `sameAs` taşımaz, hesaplı kurulumda da. Logolu kurulumda
  `logo` taşır (`0026` D7).
- **AC-20.** İletişim belgesi "Şu an açık"/"Şu an kapalı" metinlerini, `openNow`'un hiçbir değerini,
  `embedUrl`'i ve `test-anahtar` dizesini taşımaz.

### Ziyaretçi okuması (R2, R3, C2 · D5, D7)

- **AC-21.** *(PM şartı (a) ile değişti.)* `/api/dealership/gallery` yanıtında her şube öğesinin
  anahtar kümesi tam olarak `address`, `email`, `map`, `name`, `openNow`, `openingHours`, `phone`,
  `specialHours`'tır. Anahtar yapılandırılmışken `map`'in anahtar kümesi tam olarak `embedUrl`,
  `linkUrl`'dir.
- **AC-22.** Kadıköy'ün `email`'i `kadikoy@otogaleri.com.tr`, Bostancı'nınki `null`'dır.
- **AC-23.** Yanıt hiçbir yerde ham koordinat (`40.98765` sayısı ya da `latitude` anahtarı), saat
  dilimi, WhatsApp ve slug taşımaz. Koordinat yalnız `map` adreslerinin sorgusunda geçer.
- **AC-24.** Kadıköy'ün `linkUrl`'i tam olarak
  `https://www.google.com/maps/search/?api=1&query=40.98765%2C29.0275`'tir.
- **AC-25.** Süreç kültürü `tr-TR` iken de AC-24'ün adresi aynıdır: virgül ondalık ve sondaki sıfır
  yoktur.
- **AC-26.** Bostancı'nın `linkUrl` sorgusu şubenin **tr** adresinin URL kodlamasıdır. `locale=ar`
  isteğinde de tr adresi kullanılır.
- **AC-27.** Anahtar yapılandırılmışken Kadıköy'ün `embedUrl`'i
  `https://www.google.com/maps/embed/v1/place?key=test-anahtar&q=40.98765%2C29.0275&language=tr`'dir.
  `locale=en` isteğinde `language=en` olur.
- **AC-28.** *(PM şartı (a) ile değişti.)* Anahtar yapılandırılmamışken, boşken ve yalnız boşluktan
  oluşurken her şubenin `map`'inin anahtar kümesi tam olarak `linkUrl`'dir. `embedUrl` anahtarı yoktur
  ve `linkUrl` doludur.
- **AC-29.** Koordinatsız şubenin adresi panelden değiştirildikten sonraki ilk okumada `linkUrl` yeni
  adresi kullanır.
- **AC-30.** Kadıköy'ün e-postası panelden boşaltıldıktan sonraki ilk okumada `email` `null`'dır.
  Ardından istenen ilk `/tr/contact` belgesinde `email` yoktur.
- **AC-31.** Geçici kapalı ve kapatılmış şube yanıtta yoktur (`0020` AC-48 aynen). İletişim
  sayfasında ve belgesinde de yoktur.

### İletişim sayfası (R1, R2, R5, R10, R11 · D3, D4, D8, D11)

- **AC-32.** İki şubeli kurulumda sayfa iki bölüm basar. Her bölümün `<h2>`'si şubenin adıdır, sıra
  yanıtın sırasıdır.
- **AC-33.** Programlı, koordinatlı ve e-postalı Kadıköy bölümünde öğe sırası şudur: ad · "şu an"
  satırı · adres · telefon · e-posta · harita kutusu · "Google Haritalar'da aç" · haftalık liste ·
  "Özel günler".
- **AC-34.** Telefon `href="tel:{E.164}"` taşıyan bir bağlantıdır ve `dir="ltr"`'dir.
- **AC-35.** E-posta `href="mailto:kadikoy@otogaleri.com.tr"` taşıyan bir bağlantıdır. Metni adrestir
  ve `dir="ltr"`'dir.
- **AC-36.** Bostancı bölümünde e-posta satırı yoktur. Boş bir bağlantı ya da etiket basılmaz.
- **AC-37.** Programsız şubenin bölümünde "şu an" satırı, haftalık liste ve "Özel günler" yoktur. Ad,
  adres, telefon ve "Google Haritalar'da aç" vardır.
- **AC-38.** Galeri okuması sürerken `<h1>` görünür. Şube alanında `role="status"` taşıyan bir iskelet
  vardır.
- **AC-39.** Galeri okuması `503` dönerse sayfa çevrilmiş bir hata metni ve "Tekrar dene" düğmesi
  basar. Düğmeye basılınca okuma bir kez tekrarlanır. Tekrar `200` dönerse şubeler basılır.
- **AC-40.** Aktif şube yokken (boş `branches`) sayfa çevrilmiş boş durum metnini basar. Şube bölümü
  ve harita yoktur.
- **AC-41.** 320 px genişlikte, iki şubeyle, tr/ar/en'de sayfa yatay kaymaz:
  `document.documentElement.scrollWidth` ≤ `clientWidth`.
- **AC-42.** 1280 px genişlikte harita kutusu bilgi sütununun satır sonu tarafındadır. tr ve en'de
  sağında, ar'da solundadır (kutuların `getBoundingClientRect` konumuyla ölçülür).
- **AC-43.** `/ar/contact` `dir="rtl"` dizilir. Telefon, e-posta, saatler ve tarihler Latin rakamla
  ve soldan sağa basılır.
- **AC-44.** `0027`'nin şu kriterleri iletişim sayfasının şube bölümünde aynen sağlanır. Testleri bu
  sayfanın bileşeniyle koşar, metinlerde "altlık" yerine "iletişim sayfası" okunur:
  - metin: AC-48–AC-57;
  - tarayıcı saati: AC-58;
  - yenileme: AC-59–AC-61, AC-79–AC-82, AC-100, AC-105;
  - "şu an" kutusu: AC-69, 320 px'te iletişim sayfasının sütununda;
  - AC-101 ve AC-102;
  - StrictMode'lu yenileme (QA 2 BULGU-3).
- **AC-45.** `/tr/contact` açılışında galeri okuması ve sayfanın tembel parçası dışında bir JS parçası
  istenmez: `branchClock-*` adlı ayrı bir istek yoktur (D11). Ağ kaydıyla ölçülür.

### Harita (R3, R4, R5 · D6, D7, D15, D16)

- **AC-46.** `embedUrl` doluyken Kadıköy bölümündeki harita kutusunun en/boy oranı 4:3'tür (±1 px).
  İçinde "Haritayı göster" düğmesi ve "Harita Google Haritalar'dan yüklenir. Google çerez kullanabilir."
  metni vardır. tr/ar/en çevrilidir.
- **AC-47.** Yer tutucu bir `<iframe>` ve `<img>` içermez.
- **AC-48.** Düğme `<button type="button">`'dır. Enter ile de Space ile de iframe'i açar.
- **AC-49.** Sayfa açılıp sona kadar kaydırıldığında ve 5 sn beklendiğinde, düğmeye basılmadan önce
  sayfanın köken dışı hiçbir alan adına isteği yoktur (`google.com`, `gstatic.com`, `googleapis.com`
  dâhil). Ağ kaydıyla ölçülür. Belgede `preconnect` ya da `dns-prefetch` yoktur.
- **AC-50.** Düğmeye basınca kutunun içinde bir `<iframe>` açılır. `src`'si `embedUrl`'dir,
  `referrerpolicy` değeri `strict-origin-when-cross-origin`'dır, `allow` özniteliği yoktur.
- **AC-51.** iframe'in `title`'ı tr'de "Kadıköy Şubesi — Google Haritalar haritası"dır. ar ve en'de
  çevrili karşılığı şube adını taşır.
- **AC-52.** Düğmeye klavyeyle basıldıktan sonra `document.activeElement` iframe'dir.
- **AC-53.** Kutunun yüksekliği düğmeye basmadan önce ve sonra 320 px ve 1280 px'te aynıdır (±1 px).
- **AC-54.** İki şubeli sayfada Kadıköy'ün düğmesine basınca yalnız Kadıköy'ün kutusunda iframe
  açılır. Bostancı'nın kutusu yer tutucuda kalır.
- **AC-55.** `embedUrl` yokken bölümde harita kutusu ve düğme yoktur, "Google Haritalar'da aç"
  bağlantısı vardır.
- **AC-56.** "Google Haritalar'da aç" bağlantısının `href`'i şubenin `linkUrl`'idir ve `target`
  özniteliği taşımaz. Metin tr/ar/en çevrilidir.
- **AC-57.** Haritayı açıp sayfayı yeniden yükleyince kutu yine yer tutucudadır. Tıklama
  `localStorage`'a, `sessionStorage`'a ya da çereze yazılmaz.

### Altlık (R6 · D9)

- **AC-58.** `/tr/` altlığında programlı ve özel günlü bir şubenin öğesi yalnız ad, adres ve telefonu
  taşır. "Şu an" satırı, haftalık liste, "Özel günler" ve saat iskeleti yoktur.
- **AC-59.** Şube bloğunun sonunda "İletişim ve çalışma saatleri" bağlantısı vardır, `href`'i
  `/tr/contact`'tır. ar'da `/ar/contact` ve "التواصل وساعات العمل", en'de `/en/contact` ve "Contact and
  opening hours" olur.
- **AC-60.** `/tr/` açılışında `branchClock-*` ya da iletişim sayfasının parçası istenmez. Ağ kaydıyla
  ölçülür.
- **AC-61.** `refreshInSeconds` = 2 taşıyan bir galeri yanıtından sonra ana sayfa 5 sn içinde
  `/api/dealership/gallery`'yi yeniden istemez.
- **AC-62.** Galeri okuması sürerken `0014`'ün `branch-skeleton`'ı aynen basılır. Okuma hata verirse
  ya da aktif şube yoksa şube bloğu ve bağlantısı basılmaz (`0014` AC-49, AC-54, AC-65 aynen).
- **AC-63.** Altlıkta `id="contact"` yoktur.

### Gezinme (R7 · D10, D17)

- **AC-64.** Şu sayfaların üst çubuğunda `/{l}/contact`'a giden bir bağlantı vardır: ana sayfa,
  liste, görünür bir ilanın detayı, bir içerik sayfası, favoriler, karşılaştırma, bülten teyidi,
  bulunamadı ve iletişim sayfası. Erişilebilir adı "İletişim"dir.
- **AC-65.** Panelin hiçbir ekranında bu bağlantı yoktur.
- **AC-66.** İki satır eşiğinin üstünde (1280 px) bağlantı metinlidir ve satır sonu grubunda sayaçtan
  önce gelir.
- **AC-67.** 320 px'te bağlantı birinci satırdadır. Yalnız simgedir, erişilebilir adı tam metindir,
  kutusu en az 44×44 px'tir.
- **AC-68.** Logolu kurulumda, 40 karakterlik adla, 24 favoriyle ve tr/ar/en'de, 320–1280 px arasında
  16 px adımla şunlar sağlanır:
  - üst çubuk taşmaz (`scrollWidth` ≤ `clientWidth`);
  - adın görünen kısmı en az 6 karakterdir (`frontend-003` D8 ölçüsü);
  - eşiğin altında satır sayısı tam 2, üstünde tam 1'dir;
  - eşik değeri PR'a yazılır.
- **AC-69.** İletişim sayfasında üst çubuğun bağlantısı `aria-current="page"` taşır. Diğer sayfalarda taşımaz.
- **AC-70.** Logosuz kurulumda ad bugünkü gibi kırılabilir (`0026` AC-34 değişmez). Web fontu gelirken
  üst çubuğun CLS'i ≤ 0.1'dir.
- **AC-71.** ar'da simge bağlantı birinci satırın sol ucundadır (satır sonu), tr'de sağ ucundadır.
- **AC-72.** Boş vitrinde "Galeriye ulaşın" bağlantısının `href`'i ana sayfada da listede de
  `/{l}/contact`'tır. Metni değişmez (`0022` AC-11).

### Performans ve paket (C5 · D14)

- **AC-73.** İlk paket (`0024` AC-55 tanımı) `main` tabanına göre en çok **512 B** büyür. Ölçüm PR'ın
  head commit'inde alınır ve PR'a yazılır.
- **AC-74.** `index.html`'in `modulepreload` listesi ve girişin statik içe aktardığı dosya sayısı
  `main` ile aynıdır.
- **AC-75.** Ana sayfa, liste ve detay açılışında iletişim sayfasının tembel parçası istenmez.
- **AC-76.** *(PM şartı (c) ile değişti: font bekletmeli CLS.)* `perf.yml` `/tr/contact`'ı ölçer. CLS
  fontlar bekletilerek ölçülür. Perf tohumuyla (koordinat, e-posta, 12 özel gün, sahte
  anahtar) LCP medyanı < 2500 ms ve CLS < 0.1'dir. Ana sayfa, liste ve detayın C3'ü yeşil kalır. Dört
  sayfanın medyanı PR'da `main` ile yan yana yazılır.
- **AC-77.** İletişim sayfası fontlar bekletilerek açılır. Şube bölümlerinde ve harita kutusunda
  `PerformanceObserver('layout-shift')` kaynağı yoktur.

### Çeviri ve şema (C1, C7 · DoD T5, T6)

- **AC-78.** Bu kartın eklediği her sabit `t('…')` anahtarı en az bir katalogda bulunur ve
  `check:locales` yeşildir. Kapsananlar: sayfa, üst çubuk, altlık bağlantısı, harita, durumlar ve
  panelin yardım cümlesi. Panelin yardım cümlesi panel kataloğundadır.
- **AC-79.** Panel şube formunda e-posta alanının yanında, D8'in yardım cümlesi tr/ar/en çevrili basılır.
- **AC-80.** Bu kart migration eklemez. `git diff --stat main...HEAD` `Migrations/` altında dosya göstermez.

### Proje Yöneticisi şartlarıyla eklenenler (2026-11-27)

- **AC-81.** *(a)* Anahtarsız kurulumda (e2e ikizi) `/tr/contact`'ta harita kutusu ve "Haritayı göster"
  düğmesi yoktur. Her şubede "Google Haritalar'da aç" vardır. Ağ kaydındaki galeri yanıtında
  `embedUrl` dizesi geçmez.
- **AC-82.** *(a)* Anahtar `test-anahtar` iken `/tr/`, `/tr/cars`, görünür bir ilanın detayı,
  `/tr/contact`, `/ar/contact` ve bir 404 belgesinin hiçbiri `test-anahtar` dizesini taşımaz.
  `<head>`, JSON-LD ve gövde dahil.
- **AC-83.** *(a)* Host'un başlangıcında, galeri okumasında ve `/tr/contact` belgesinde yakalanan günlük
  kayıtlarının hiçbiri `test-anahtar` dizesini taşımaz. Ölçülen kayıtlar: bütün kategoriler, en düşük
  düzey `Trace`, ileti ve kapsam değerleri. Okuma ucunun `500` döndüğü bir senaryo da ölçülür.
- **AC-84.** *(b)* Anahtarlı kurulumda, e2e ağ denetimiyle:
  - düğmeye basmadan önce köken dışı istek sayısı **0**'dır (AC-49);
  - düğmeye basınca köken dışı ilk istek, `embedUrl`'e giden iframe gezinmesidir;
  - köken dışı her istek iframe'in çerçevesinden gelir (`request.frame()` ana çerçeve değildir);
  - ana çerçeve köken dışı hiçbir istek yapmaz.
- **AC-85.** *(c)* 1280 px genişlikte, logolu, iki şubeli, kurumsal sayfası ve sosyal hesabı olan
  kurulumda, tr/ar/en'de:
  - şube bloğu, "Keşfet", "Kurumsal", "Bizi takip edin" ve "Diller" bloklarının üst kenarları
    aynıdır (±1 px), yani tek satırdadırlar;
  - şube bloğu satır başındadır: tr ve en'de en solda, ar'da en sağda;
  - logo bu satırın üstündedir.
- **AC-86.** *(c)* 320 px genişlikte aynı kurulumda bloklar tek sütundur ve DOM sırasıyla alt alta
  durur: şube, Keşfet, Kurumsal, Bizi takip edin, Diller. Hiçbir iki bloğun üst kenarı aynı değildir.
  Altlık yatay kaymaz.
- **AC-87.** *(c)* Ana sayfa fontlar bekletilerek (`.woff2` `page.route` ile bekletilip serbest
  bırakılarak) açılır ve altlığa kaydırılır. Altlıkta ve altlığın bloklarında
  `PerformanceObserver('layout-shift')` kaynağı yoktur. 320 px'te ve 1280 px'te ölçülür.

---

## Definition of Done

- [x] Önerilen kararlar (**D1–D19**) onaylandı (Proje Yöneticisi kararı, 2026-11-27; şartlar (a)–(d)
      → AC-81–AC-87). **SS1–SS4** cevaplandı (sahip kararı, 2026-11-27).
- [ ] QA CLARIFY turu işlendi.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0028-ac<n>.png`),
      boş ve hata durumları dâhil. İletişim sayfasının (yer tutucu, açık harita, anahtarsız, iki şube),
      sadeleşmiş altlığın ve üst çubuğun (320 px, 1280 px) Arapça görüntüsü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). "Şu an" kriterleri sabit `IClock` ile.
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor. — bu kartta migration **yok** (D19, AC-80).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi. — yeni terim **yok** (D18).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi
      (D18: adres, saat bloğunun yeri, tıkla-yükle harita) ve `docs/deploy.md` (anahtar, CSP, çerez notu, perf tohumu).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. Bilerek
      değişen eski testler (§ Context tablosu) PR'da listelendi ve **çok etiketli** kaldı. İlk paket
      ölçümü head commit'ten yapıldı. Sahibin yerel veritabanında `contact` slug'lı içerik sayfası
      olmadığı **salt-okur** bir sorguyla doğrulandı ve sonucu PR'a yazıldı (Context 4).
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı. Taşımadan sonra `tests/Architecture.Tests`'in TAMAMI koşturuldu.

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
