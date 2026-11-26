# 0024 — Galerinin sosyal hesapları

| | |
|---|---|
| **Durum** | **Onaylandı (2026-11-26 — Takım Yöneticisi (ajan) kararı)** · QA CLARIFY turu işlendi (B1–B5, kartlar arası X1–X3 → D2, D4, D6, D9 güncellendi, **D13** eklendi; AC-71–AC-82) · **QA 2 revizyonu (2026-11-26, AP-08, Proje Yöneticisi kararı):** D2, D6, D9(b), D11, AC-55, AC-70 değişti; **AC-83** eklendi |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (galeri profili) · `Host` (ana sayfanın yapısal verisi) · Frontend (panel Galeri ekranı, vitrin altlığı) |
| **Bağımlı spec'ler** | `0004` (**D1**, **D15**, **D16**, **D17**, **AC-52**) · `0014` (altlık, **AC-14**, **AC-46**) · `0020` (galeri profili formu, **R16** sürüm, **D2** yetki). **Sonra gelenler:** `0025` → `0026` aynı dosyalara dokunur (§ Context › Sıradaki kartlarla kesişme). |
| **Karar bekliyor** | yok — D1–D12 onaylandı (Proje Yöneticisi kararı, 2026-11-26), D13 onaylandı (Proje Yöneticisi kararı, 2026-11-26). SS1–SS3 cevaplandı (sahip kararı, 2026-11-26). |

> **Sahibin isteği (sahip kararı, 2026-11-26):** `0004` **SS2**'ye "evet": sosyal hesaplar panelden
> girilsin, yapısal veride (`sameAs`) ve vitrinde görünsün; `0004`'ten sonra **ayrı küçük kart**.
> Bu kart onu karşılar. `0004` **X4** ve **D15**'in notu bu kartın dayanağıdır.
>
> **Sahibin cevapları (sahip kararı, 2026-11-26):** SS1 → altı platform (D1 aynen). SS2 → yalnız ana
> sayfa altlığı (öneri kabul). **SS3 → platform simgeleri (öneriden farklı).** Bu cevapla **D6** ve
> **D9** yeniden yazıldı, **X2** değişti ve simge kaynağı Context'e girdi (Proje Yöneticisi kararı:
> Simple Icons, CC0). **AC-37** ve **AC-43** yerinde değişti; yeni kriterler numara **eklenerek**
> geldi (**AC-60–AC-70**), var olan numaralar değişmedi.
>
> **QA CLARIFY turu (QA 1, 2026-11-26; Proje Yöneticisi kararları):** B1–B5 ve kartlar arası X1–X3
> işlendi. **D2**, **D4**, **D6**, **D9** yerinde güncellendi ve "CLARIFY" diye işaretlendi. **D13**
> eklendi (ölçümle bulundu: LinkedIn simgesi Simple Icons'ta yok). **AC-14**, **AC-54**, **AC-61**,
> **AC-66**, **AC-67**, **AC-69**, **AC-70** düzeltildi. Yeni kriterler **AC-71–AC-82**. Var olan
> numaralar değişmedi.

---

## Intent

Galeri yöneticisi, galerinin Instagram, Facebook, YouTube gibi hesaplarını vitrinde göstermek
istiyor. Bugün bu hesapları girebileceği bir yer yok ve ziyaretçi galerinin hesaplarına sitenin
içinden ulaşamıyor. Arama motorları da sitenin ve hesapların aynı işletmeye ait olduğunu bilmiyor.
Bu iş bittiğinde yönetici hesap adreslerini panelden girer, değiştirir ya da siler. Kaydettikten
sonra açılan ilk ana sayfada hesaplar altlıkta bağlantı olarak görünür ve aynı hesaplar galerinin
yapısal verisinde de yer alır.

---

## Requirements

- **R1.** Yönetici, galeri profilinde **sabit bir platform listesinin** (D1) her platformu için en
  çok bir hesap adresi girer, değiştirir ya da siler.
- **R2.** Girilen adres o platforma ait bir hesap adresi değilse kaydetme alan hatasıyla reddedilir
  ve profilin hiçbir alanı değişmez.
- **R3.** Sosyal hesaplar galeri profilinin parçasıdır. Profille aynı kaydetmede yazılır ve
  profilin eşzamanlı yazma korumasına (`0020` **R16**) tabidir.
- **R4.** Ana sayfanın altlığı girilmiş hesapları sabit platform sırasıyla bağlantı olarak gösterir.
  Hiç hesap yoksa blok görünmez.
- **R5.** Ana sayfanın yapısal verisi, altlıkta görünen hesapları galerinin hesapları olarak
  (`sameAs`) taşır.
- **R6.** Ziyaretçiye dönük galeri okuması her hesap için yalnız platformu ve adresi taşır.
- **R7.** Sosyal hesapları yalnız **Yönetici** görür ve düzenler. Satış Danışmanı ve İçerik Editörü
  yazamaz; panel adresini doğrudan açtıklarında "erişim yok" görürler.
- **R8.** Panel bölümü ve vitrin bloğu TR/AR/EN'de çevrilidir; Arapçada sağdan sola dizilir.

---

## Constraints

### Uyulması zorunlu

- **C1.** Galeri profilinin tek yazma sahibi `Dealership`'tir. Hesaplar başka bir modülde gerçek
  kaynak gibi saklanmaz. → Yasak Listesi 33.
- **C2.** Ziyaretçiye dönük okuma bir **izin listesidir** (`0014` **D15**). Yanıta yalnız R6'nın
  iki alanı eklenir.
- **C3.** Yapısal veri sayfada görünmeyen bir bilgiyi iddia etmez (`0004` **D1**). `sameAs` tam
  olarak altlığın gösterdiği hesaplardır.
- **C4.** JSON-LD'deki serbest metin `0004` **D16**'ya göre kodlanır. Ana sayfa tam olarak bir
  JSON-LD bloğu taşır (`0004` AC-49).
- **C5.** Performans: C3 (Lighthouse mobil, LCP < 2500 ms) ana sayfa, liste ve detayda yeşil kalır.
  İlk paket ADR 0013'ün protokolüyle ölçülür. Panel metinleri panel kataloğuna girer, vitrin
  kataloğuna girmez (ADR 0013). Simgeler giriş paketine girmez ve ayrı ölçülür (**D9**).
- **C6.** Migration varsa yalnız `dealership` şemasına dokunur.
- **C7.** Yazma, galeri yönetim uçlarının mevcut **Yönetici** politikasındadır (`0020` **D2**).
  Rol kodu uçta karşılaştırılmaz.
- **C8.** Veritabanında platformun **kodu** saklanır, çevrilmiş adı saklanmaz. → Yasak Listesi 22.
- **C9.** Hata yanıtları kararlı bir `errorCode` ve alan anahtarı taşır, çevrilmiş metin taşımaz
  (`0020` **C5**, `docs/conventions.md` § 5).

### KAPSAM DIŞI

- **X1.** **Şube başına sosyal hesap** — *neden:* domain hesapları galeri düzeyinde tutuyor
  (`docs/architecture.md` § 1, "Galeri profili … sosyal hesaplar"). Şube sayfası da yok (`0004`
  **X3**, **SS4**: şimdilik hayır).
- **X2.** **Marka renkleri ve markaların kendi resmî logo dosyaları** — *neden:* simgeler
  **kapsam içinde** (**SS3**, sahip kararı, 2026-11-26), ama temanın metin rengiyle tek renk basılır
  (**D6**). Marka renkleri ve markaların dağıttığı resmî dosyalar her markanın kullanım kurallarına
  bağlı. Tek renkli, değiştirilmemiş simge bu kurallara takılmayan en dar seçenek.
- **X3.** **Gömülü akış** (Instagram gönderileri, YouTube oynatıcısı) — *neden:* üçüncü taraf
  betiği ve çerezi demek. LCP'yi ve KVKK çerez kapsamını değiştirir.
- **X4.** **Altlığın ana sayfa dışındaki sayfalarda görünmesi** — *neden:* bugün altlık yalnız ana
  sayfada var (`frontend/src/app/App.tsx:133`). Site geneli altlık ayrı bir kabuk kararı (**SS2**).
- **X5.** **Listede olmayan bir platformun eklenmesi** (serbest platform adı) — *neden:* **D1**.
  Liste kodda yaşar, platform eklemek kod değişikliğidir.
- **X6.** **Galeri düzeyinde WhatsApp** — *neden:* WhatsApp şubenin alanıdır (`0020`). Vitrinde
  gösterimi `0020` **X4**'te kapsam dışı.
- **X7.** **Hesabın gerçekten var olduğunun ya da galeriye ait olduğunun doğrulanması** — *neden:*
  dış bir ağ isteği gerektirir. Doğrulama biçimseldir (**D2**).
- **X8.** **İlanı sosyal ağda paylaşma düğmeleri** — *neden:* ayrı bir istek. Ham R'lerde yok.
- **X9.** **Kurulum adımının örnek hesap yazması** — *neden:* tohum hesap yazmıyor
  (`SeedInitialDealership.cs:21`). Galeri ilk girişte boş başlar.

---

## Context

> Her iddia `main` @ `bcbe520` üzerinde ölçüldü. Satır numaraları o commit'e aittir.

### Bugün domain'de ne var

- **Değer tipi ve koleksiyon var, çağıranı yok.** `SocialLink(string Platform, string Url)`
  (`src/Modules/Dealership/Dealership/Domain/Gallery.cs:5`), galerinin listesi (`:28`, `:42`) ve
  `AddSocialLink` (`:153-159`) duruyor. `AddSocialLink`, aynı platform zaten varsa **sessizce hiçbir
  şey yapmıyor**. Değiştiren ya da silen bir yöntem yok. `src/` ve `tests/` altında domain dışında
  `AddSocialLink`'i çağıran **hiçbir yer yok** (`grep -rn AddSocialLink`). `0004` Context'teki not
  doğru. R1'in "değiştir" ve "sil" istekleri bugünkü yöntemle karşılanamaz.
- **Tablo kurulu.** `gallery_social_links` (`InitialDealership.cs:59-71`;
  `GalleryConfiguration.cs:32-39`): `platform` en çok 40 karakter, `url` en çok 400 karakter,
  birincil anahtar `(gallery_id, platform)`. Yani **platform başına tek satır şemada zaten
  dayatılıyor**. Sıra kolonu yok. Tohum yalnız `galleries` satırını yazıyor (`SeedInitialDealership.cs:21`),
  bugün tabloda satır yok. → **D3**: yeni migration beklenmiyor.
- **Profil yazma yolu.** `Gallery.UpdateProfile` (`Gallery.cs:73-137`) alan hatalarını toplar. Hata
  varsa hiçbir alanı değiştirmez ve başarıda sürümü bir artırır (`:134`). `GalleryCommands.UpdateProfileAsync`
  (`Application/GalleryCommands.cs:23-72`) okunan sürümü karşılaştırır. Paralel yazma da
  `DbUpdateConcurrencyException` ile `concurrency.lost` (`409`) olur. R3 bu yolu genişletir.

### Bugün panel ve uçlar

- **Yönetim uçları** (`Endpoints/DealershipAdminEndpoints.cs:29-37`) tek grupta ve grup yalnız
  `ManagerPolicy` taşıyor. `PUT /api/admin/gallery` gövdesi `SaveGalleryBody` (`:112-117`) dört
  alan ve sürüm taşıyor, sosyal hesap alanı yok. `GET /api/admin/gallery` `AdminGalleryDto`'yu
  döndürüyor (`Application/DealershipAdminQueries.cs:11-16`), o da sosyal hesap taşımıyor.
- **Panel ekranı:** `frontend/src/features/dealership/admin/GalleryProfilePage.tsx` (309 satır).
  Değerleri unvan, vergi no, sicil no ve dil başına görünen addan oluşuyor (`:25-30`). Sürüm
  çatışmasında uyarı veriyor, kendiliğinden yeniden yüklemiyor (`0020` AC-63). Satış Danışmanı ve
  İçerik Editörü adresi doğrudan açınca "erişim yok" görüyor (`frontend/src/app/admin/AdminApp.tsx:274`,
  `0020` **D23**).
- **Panel kataloğu ayrı:** `frontend/src/shared/i18n/panel.ts` (ADR 0013). Vitrin kataloğu ilk
  pakete giriyor.

### Bugün vitrin

- **Okuma bir kez yapılıyor:** `HomeRoute` (`frontend/src/app/App.tsx:121-140`) galeriyi tek okumayla
  alıp adı giriş bölümüne, şubeleri altlığa veriyor (`/api/dealership/gallery`,
  `Endpoints/PublicGalleryEndpoints.cs:19`).
- **İzin listesi:** `PublicGalleryDto(DisplayName, Branches)`
  (`Dealership.Contracts/Gallery/PublicGalleryContracts.cs:14`). Belge yorumunda (`:4-8`) "sosyal
  bağlantılar … burada YOKTUR" yazıyor, bu kartla değişir. Sorgu yalnız `DisplayNames`'i okuyor
  (`Application/PublicGalleryQueries.cs:29-32`). Yanıtın anahtar kümesi `tests/Host.Tests/GaleriPublicUcTests.cs:24`'te
  birebir ölçülüyor: `["branches", "displayName"]`.
- **Altlık** (`frontend/src/app/HomeFooter.tsx`) yalnız ana sayfada (`App.tsx:133`). Bloklar sırayla:
  şubeler, "Keşfet" (`/tr/cars`), "Kurumsal" (`0017`, yoksa basılmaz), "Diller". Okuma düşerse
  şube bloğu basılmaz ve hata metni yoktur (`BranchList.tsx:29-31`, `0014` AC-54). Altlığın
  bağlantı kümesi `frontend/src/app/HomePage.test.tsx:177-187`'de birebir ölçülüyor (`0014` AC-46).
- **Yapısal veri:** `src/Host/Seo/SeoHeadBuilder.cs:100-123` ana sayfada tek bir `AutoDealer` kuruyor:
  `@context`, `@type`, `name`, `url` ve şube varsa `department`. Galeri okuması yalnız ana sayfa
  için yapılıyor (`:70`). Okuma düşerse blok basılmıyor (`:294-311`). Kodlama `:399-407`'de
  (`0004` **D16**). `AC-52` testi (`tests/Host.Tests/SeoBelgeTests.cs:561-573`) `sameAs`'ın kökte
  ve şube öğelerinde **yokluğunu** ölçüyor.
- **Performans tabanı:** `0004`'ün son koşusunda C3 ana sayfa 2277, liste 2125, detay 2408 ms (PR #126).
  Ana sayfa metinleri perf koşusunda **sınır uzunlukta** tohumlanıyor (`.github/workflows/perf.yml:90-94`,
  `0022` **D11**). Bu kartın en kötü durumu da aynı desenle ölçülür (**D9**).

### Simge kaynağı (Proje Yöneticisi kararı, 2026-11-26)

- Simgeler **Simple Icons**'tan (simpleicons.org) alınır: `instagram`, `facebook`, `youtube`,
  `tiktok`, `x` — **beş simge**; LinkedIn pakette yok (aşağıda, **D13**). Lisans **CC0 1.0**. Yalnız SVG yolları kaynağa kopyalanır.
  **Yeni çalışma zamanı bağımlılığı yoktur** (`package.json`'a paket eklenmez). Kaynak ve lisans
  notu simgelerin durduğu dosyada yorum olarak yazılır.
- CC0 telif hakkından vazgeçmedir, marka hakkını kapsamaz: simgeler markaların ticari markasıdır.
  Bu yüzden biçimleri değiştirilmez ve marka renkleri kullanılmaz (`currentColor`, **D6**, X2).
- **Ölçüldü (CLARIFY B4, 2026-11-26), `simple-icons@16.38.1` (npm'deki son sürüm):**
  - Yol uzunlukları (karakter): instagram 1988, tiktok 597, facebook 430, youtube 348, x 194.
  - Beş yolun toplamı 3557 karakter, `gzip -9` ile **1671 B**. Yolları taşıyan tek bir JS modülü olarak
    3628 karakter ve **1750 B**. Simge bileşeninin kendi kodu bunun üstüne eklenir.
  - **LinkedIn simgesi pakette YOK.** `icons/linkedin.svg` 13.x'te var (13.21.0'da yol 498 karakter),
    **14.0.0'dan beri yok** (`slugs.md`'de de yok). LinkedIn dahil altı yol modül olarak **2003 B** gzip
    olurdu. → **D13**.
  - Paketin veri dosyasında (`data/simple-icons.json`) beş simgenin `license` alanı boş, yani paketin
    CC0 1.0'ı geçerli. Paketin `DISCLAIMER.md`'si "her simgenin CC0 olduğu anlamına gelmez, simge
    başına lisansa bakın" diyor. Facebook, Instagram, X ve YouTube için bir marka kuralı bağlantısı
    var, TikTok için yok. Bu bağlantılar simgenin kaynak notunda anılır.
  - Sonuç: 1750 B, ADR 0013'ün +512 B'lık giriş paketi payını tek başına aşar → **D9**(b).

### Bilerek değişecek eski testler (kod taranarak çıkarıldı)

`0004`'ün dersi: liste spec yazılırken değil, kodda o davranışa dayanan testler aranarak çıkarılır.

| Test | Bugün ölçtüğü | Bu kartla |
|---|---|---|
| `tests/Host.Tests/GaleriPublicUcTests.cs:24` (`0014/AC-14`) | kök anahtarlar `branches, displayName` | `branches, displayName, socialLinks` (**AC-33**) |
| `tests/Host.Tests/SeoBelgeTests.cs:561-573` (`0004/AC-52`) | `openingHoursSpecification, geo, sameAs, logo` yok | `sameAs` listeden çıkar (**AC-45**) |
| `tests/Host.Tests/SeoBelgeTests.cs:547` (`0004/AC-50`) ve aynı dosyada `PublicGalleryDto` kuran diğer testler | iki parametreli kurucu | kurucu değişir, beklenen değer değişmez |
| `frontend/src/app/HomePage.test.tsx:177-187` (`0014/AC-46`), `BultenBolumu.test.tsx`, `AnaSayfaMetinleri.test.tsx` | `PublicGallery` sahte yanıtı | sahte yanıtın tipi değişir. Hesapsız yanıtta beklenen bağlantı dizisi **değişmez** (**AC-38** hesaplı durumu ayrıca ölçer) |
| `tests/Host.Tests/GaleriYonetimiZinciriTests.cs:320` (`0020/AC-16`, gizlilik testi) | public yanıtın kök anahtarları `branches, displayName` | `branches, displayName, socialLinks` (**AC-33**). *(CLARIFY B2: tablo bu dosya için yalnız "değişmez" diyordu.)* |
| `0020`'nin `PUT /api/admin/gallery` testleri (`tests/Host.Tests/GaleriYonetimiZinciriTests.cs`) | sosyal alansız gövde | **değişmez** (**D4**, **AC-22**) |

### Sıradaki kartlarla kesişme (uygulama sırası 0024 → 0025 → 0026)

- **`0025`** aynı okuma sözleşmesine şube başına çalışma programını ekler. Kök anahtar kümesi
  **AC-33**'un hâlini temel alır. Altlıkta şube bloğu bu kartın bloğundan önce kalır.
- **`0026`** galeri profiline logo ekler. Logo işlemi profilin **sürümünü** artırır (`0026` **D8**).
  Bu kartın formu aynı sürümü taşıdığı için `0026`, logo işleminden sonra bu formun 409 vermemesini
  kendi kriteriyle ölçer.
- **`0004` AC-52** zinciri: bu kart `sameAs`'ı listeden çıkarır (**AC-45**: kalan
  `openingHoursSpecification`, `geo`, `logo`). `0025` `openingHoursSpecification`'ı, `0026` `logo`'yu çıkarır.

### Bilinen tuzaklar

1. **`AddSocialLink` sessizce yok sayıyor** (`Gallery.cs:155`): değiştirme isteği bu yöntemle
   yazılırsa eski adres kalır ve kaydetme "başarılı" döner. R1'in "değiştir" kriteri (AC-4) bunu yakalar.
2. **Gövdede alanın olmaması ile boş olması farklı şeydir** (**D4**). JSON'da ikisi de `null`'a
   düşebilir. Ayrım ölçülmezse `0020`'nin istemcisi, ve alansız gövde gönderen her test, kayıtlı
   hesapları siler.
3. **`&` JSON-LD'de altı bayta büyür** (`\u0026`, `SeoHeadBuilder.cs:404`). Bütçe kurgusu
   (**AC-54**) bu yüzden `&` içermeyen adresle kurulur ve `&` ayrıca ölçülür (**AC-49**, **D9**'un bedeli).
4. **Kapalı küme, yasak listesinden güçlüdür** (`0014` **D15**). Yanıtın anahtar kümesi birebir
   ölçülür, yalnız "yeni alan var mı" diye bakılmaz.

### Verilen kararlar — plan turu (DEV, 2026-11-26)

> R/C/AC'ye dokunmaz. `main` @ `12c2043` üzerinde ölçüldü. `0025` ve `0026` aynı yapıları
> kullanacak biçimde seçildi (Proje Yöneticisi görevlendirmesi).

- **P1 — "Alan yok = açık `null` = değişmez" mekanizması: düz nullable koleksiyon (D4, X1).**
  `SaveGalleryBody.SocialLinks` `Dictionary<string, string?>?`'dir. D4 açık `null`'u alanın
  yokluğuyla **aynı** anlama bağladığı için ayrılması gereken iki durum kalıyor: `null` (değişmez)
  ve harita (bütünüyle yerine koy; boş harita hepsini siler). İkisini düz nullable tip zaten
  ayırıyor. `Optional<T>` ya da `JsonElement` yalnız "yok" ile "`null`"un **farklı** anlam taşıdığı
  bir kural için gerekirdi; üç kartta da böyle bir kural yok. Komut katmanında kural tek yerdedir:
  `null` koleksiyon domain'e "dokunma" olarak geçer. `0025`'in programı aynı deseni kullanır
  (`null` = değişmez, yedi boş gün = sil). Haritadaki `null` ya da boşluktan ibaret değer D2'nin
  "bu platformda hesap yok"udur.
- **P2 — Platform listesi domain'de tek kaynaktır (D1, D2, D3).** `SocialPlatform` (kod, sıra,
  ana makine listesi) `Dealership/Domain`'dedir. Doğrulama `SocialLink.Validate`'tedir ve
  `Gallery.UpdateProfile` aynı `FieldErrors`'a toplar: sosyal hata da görünen ad hatası da tek
  yanıtta gelir ve hiçbir alan yazılmaz (AC-17, AC-18). Sıra: önce bayt sınırı (`too_long`), sonra
  biçim (`invalid`). Port, ayrıştırılmış adres yerine **yazılan otoritenin** içinde `:` aranarak
  bulunur: `Uri.Port` varsayılan `:443`'ü yazılmamışla aynı gösterir (AC-76).
- **P3 — Değiştirme yerinde yapılır; `AddSocialLink` kaldırılır (tuzak 1, D3).**
  `Gallery.ReplaceSocialLinks` kümede olmayanı siler, var olanın adresini günceller, yeniyi ekler.
  `SocialLink` bu yüzden değiştirilebilir adresli bir sınıf olur: sahip olunan koleksiyonda aynı
  anahtarla sil-ve-ekle, EF'in izleyicisinde anahtar çakışmasıdır. Çağıranı olmayan
  `AddSocialLink` silinir: "varsa yok say" yolu kodda kalmaz.
- **P4 — Okumalar (D7, AC-32).** Panel: `AdminGalleryDto.SocialLinks` (kod → adres, D1 sırası).
  Ziyaretçi: `PublicGalleryDto(DisplayName, Branches, SocialLinks)` ve `PublicSocialLinkDto(Platform, Url)`.
  Sorgu D1'de olmayan kodu düşürür ve D1'e göre sıralar. `0025` aynı DTO'nun şube öğesini
  genişletir, kök değişmez.
- **P5 — `sameAs` aynı okumadan (D8).** `SeoHeadBuilder.Home` `gallery.SocialLinks`'in adreslerini
  sırasıyla yazar; boşsa anahtar yoktur. İkinci okuma yok.
- **P6 — Simgeler (D6, D9(b), D13).** `frontend/src/features/dealership/socialIcons.ts` beş yolu,
  kaynak/lisans/sürüm/marka kuralı notunu taşır. `SocialLinks.tsx` onu dinamik `import()` ile
  ister; modül düzeyinde tek bir söz (promise) tutulur, yani sayfa başına tam bir istek (AC-78).
  Parçanın adını `vite.config.ts`'in `manualChunks`'ı sabitler (`social-icons`). Durumlar:
  `pending` (44×44 kutu, simgesiz, erişilebilir ad `aria-label`), `ready` (simge), `failed`
  (görünür ad). Red yakalanır (AC-79). Platform adları ve sırası `socialPlatforms.ts`'te koddadır
  (D1: çevrilmez). Ölçüm: `scripts/check-social-icons.mjs` derleme çıktısında parçanın tekliğini,
  3072 B sınırını ve giriş JS'inde yolların yokluğunu denetler; CI'ın frontend işine eklenir.
  *(QA 2 BULGU-1 ile değişti, Proje Yöneticisi kararı:)* `manualChunks` yerine `chunkFileNames`;
  parça (`socialIconLinks.ts`) bloğun simgeli bağlantılarını kuran işlevi ve yolları taşır, girişle
  hiçbir modül paylaşmaz. Girişte `SocialLinksSlot` (yükleme kararı, kabuk, red yedeği) ve
  `socialLinksLoader.ts` kalır; parça gelene kadar blok yoktur (AC-70'in revize metni). Kapı
  `modulepreload` ve girişin statik içe aktarmasını da reddeder.
- **P7 — Altlık yuvası.** `HomeFooter` şube bloğu gibi bir `social` yuvası alır; blok "Kurumsal"dan
  sonra, "Diller"den önce basılır. `HomeRoute` aynı `useGallery` okumasını verir (D7: ikinci istek
  yok). `0025`'in saatleri şube bloğunun içinde kalır, `0026`'nın altlık logosu yuvaların önündedir.
- **P8 — Panel (D5).** `GalleryProfilePage`'e "Sosyal hesaplar" `fieldset`'i; altı alan
  `type="url"`, `inputMode="url"`, `dir="ltr"`. Kaydetme altı kodun hepsini gönderir (boş alan = sil),
  yani panel her zaman "bütünüyle yerine koy"u kullanır; `null` yolu `0020` istemcisi ve testleri
  içindir. Hata metni `fieldErrorKey` ile (`dealership.fieldError.gallery_social_link_invalid` …).
- **P9 — Perf tohumu tek bayrakta, üç kart için (D9(c), X2).** `Development:SeedGalleryProfile`
  → `SeedGalleryProfileForDevelopmentAsync` (`DealershipModule`), gerçek yazma yolundan
  (`GalleryCommands.UpdateProfileAsync`) altı hesabı AC-54'ün kurgusuyla yazar. `0025` programı,
  `0026` logoyu aynı yönteme ekler; `perf.yml` tek bayrağı açar. e2e ortamı açmaz.
- **P10 — Testler (X3).** Etiketi değişen eski testler çok etiketli kalır
  (`0014/AC-14, 0024/AC-33: …`, `0004/AC-52, 0024/AC-45: …`, `0020/AC-16, 0024/AC-33: …`).
  HTTP kriterleri `tests/Host.Tests/SosyalHesapZinciriTests.cs`'te gerçek PostgreSQL ile; biçim
  kuralları ayrıca `Dealership.Tests`'te domain düzeyinde. Belge kriterleri `SeoOrtami` ile. Her
  fabrika `Dispose` edilir (0004 inotify dersi).

### Uygulama planı

1. Plan (bu bölüm).
2. Domain + komut + uç: `SocialPlatform`, `SocialLink` doğrulaması, `ReplaceSocialLinks`, gövde,
   panel okuması. Domain testleri ve Host zincir testleri (AC-3–AC-26, AC-32, AC-71–AC-77).
3. Ziyaretçi okuması: DTO, sorgu, eski testlerin çok etiketli hâli (AC-33–AC-36).
4. Yapısal veri: `sameAs` ve belge testleri (AC-45–AC-53), bütçe testi (AC-54).
5. Ön yüz vitrin: tipler, `socialPlatforms.ts`, `socialIcons.ts`, `SocialLinks.tsx`, altlık,
   `manualChunks`, katalog, birim testleri (AC-37–AC-44, AC-60–AC-70, AC-79, AC-81, AC-82).
6. Ön yüz panel: bölüm, katalog, testler (AC-1, AC-2, AC-19, AC-28–AC-31, AC-57, AC-59).
7. Paket denetimi (`check-social-icons.mjs`, CI) ve perf tohumu (AC-55, AC-56, AC-66, AC-67).
8. e2e ve görsel kanıt (AC-3, AC-27, AC-43, AC-62, AC-64, AC-68, AC-70, AC-78, AC-80).
9. Belgeler: `docs/domain.md` (D12), `docs/architecture.md` karar defteri, `docs/frontend.md`.

---

## Verilen kararlar (Proje Yöneticisi kararı, 2026-11-26)

Biçim: **karar** · *gerekçe* · *bedel*.

**D1 — Platform listesi kapalı ve sıralıdır: Instagram, Facebook, YouTube, TikTok, X, LinkedIn.**
(R1, R4, X5) Kodlar `instagram`, `facebook`, `youtube`, `tiktok`, `x`, `linkedin`. Bu sıra panelde,
altlıkta, okuma yanıtında ve `sameAs`'ta aynıdır ve girilme sırasından bağımsızdır. Görünen ad
markanın kendi adıdır ("Instagram", "Facebook", "YouTube", "TikTok", "X", "LinkedIn") ve
çevrilmez. Sahip listeyi **SS1** ile daraltabilir ya da genişletebilir.
*Gerekçe:* serbest platform, serbest bağlantı demektir: vitrinde galeri adına herhangi bir siteye
yönlendirme olur (oltalama) ve `sameAs` spam'e açılır. Sabit sıra saklama gerektirmez (D3).
*Bedel:* yeni bir platform kod değişikliği ister.

**D2 — Adres biçimsel olarak doğrulanır.** (R2, X7)
- Şema `https`'tir.
- Ana makine platformun alan adı listesindedir. *(CLARIFY B1:)* Karşılaştırma ayrıştırılmış adresin
  ana makinesiyle (`System.Uri.Host`, `IdnHost` değil) **ordinal, harf büyüklüğüne duyarsız ve
  birebir** yapılır: alt alan adı, son ek ya da benzer harf eşleşmez. Liste:
  - instagram: `instagram.com`, `www.instagram.com`
  - facebook: `facebook.com`, `www.facebook.com`, `m.facebook.com`
  - youtube: `youtube.com`, `www.youtube.com`, `m.youtube.com`
  - tiktok: `tiktok.com`, `www.tiktok.com`
  - x: `x.com`, `www.x.com`, `twitter.com`, `www.twitter.com`
  - linkedin: `linkedin.com`, `www.linkedin.com`
- Yol `/`'dan ibaret değildir.
- Kullanıcı bilgisi taşımaz. **Yazılmış her port reddedilir**, varsayılan `:443` dâhil *(CLARIFY B1)*.
- Kırpıldıktan sonra **UTF-8 olarak en çok 200 bayttır** *(CLARIFY B3; önce "200 karakter" idi)*.
- Boş ya da yalnız boşluk olan değer "bu platformda hesap yok" demektir.
- *(QA 2 BULGU-2:)* RFC 3986'nın bir adreste izin vermediği karakterleri **taşımaz**: kontrol
  karakterleri (NUL dâhil), her türlü boşluk, `"` `<` `>` `\` `^` `` ` `` `{` `|` `}` ve yön
  denetimleri (U+200E, U+200F, U+202A–U+202E, U+2066–U+2069). Bunlar `gallery.social_link_invalid`
  (`422`) alır. *Revizyon (2026-11-26, AP-08): ilk metin bu karakterleri saymıyordu; `Uri` onları
  kaçırıp kabul ediyor, değer ise ham saklanıyordu ve NUL veritabanında `500`'e dönüşüyordu (QA 2
  BULGU-2) — Proje Yöneticisi kararı.*

Sorgu dizesi serbesttir: Facebook'un adsız sayfaları `profile.php?id=…` biçimindedir. Değer
kırpılarak, başka bir değişiklik yapılmadan saklanır.
*Gerekçe:* D1'in gerekçesi. 200 bayt kolonun 400 karakterinin içinde kalır ve bütçeyi (D9)
sınırlar. Bayt, karakter değil: Arapça harf UTF-8'de 2 bayttır ve JSON-LD kaçışsız UTF-8 yazılıyor
(`SeoHeadBuilder.cs:60`), yani 200 karakterlik Arapça yollu adres D9(a)'yı aşardı. Birebir ana
makine karşılaştırması `instagram.com.kotu.site`, kodlanmış `@` ve Kiril homografı gibi
yanıltmaları ayrıştırma düzeyinde düşürür. Gerçek profil adresleri 100 karakterin altındadır.
*Bedel:* kısa alan adları (`fb.com`, `youtu.be`, `instagr.am`) reddedilir. Bir platform alan adı
değiştirirse kod değişir.

**D3 — Saklama mevcut tablodadır, yeni migration yoktur.** (R1, C6) Platform başına tek satır
şemada zaten dayatılıyor. Sıra D1'den gelir ve saklanmaz. Kaydetme listeyi **bütünüyle yerine
koyar**: gövdedeki kümede olmayan platform silinir. `AddSocialLink`'in "varsa yok say" davranışı
bu kartın yolunda kullanılamaz (tuzak 1). Okumada D1'de olmayan kodla saklanmış bir satır yok
sayılır.
*Gerekçe:* Context: tablo, sınırlar ve anahtar R1'i karşılıyor.
*Bedel:* yok.

**D4 — Hesaplar profilin kaydetmesiyle yazılır. Alanın olmaması "değişmez", boş küme "hepsini sil" demektir.**
(R3, tuzak 2)
- `PUT /api/admin/gallery` gövdesi sosyal hesapları platform kodundan adrese bir harita olarak
  taşır. Aynı sürüm geçerlidir: eski sürüm `409` döner (`0020` **D17**).
- Hata varsa **profilin hiçbir alanı** değişmez; aynı kaydetmedeki görünen ad değişikliği de yazılmaz.
- Değişiklik sürümü bir artırır.
- Gövdede alan **yoksa** kayıtlı hesaplar değişmez. *(CLARIFY X1:)* **Açık `null`, alanın yokluğuyla
  aynıdır: değişmez. Silmenin tek yolu boş haritadır.** Gövdeler bugün nullable kayıtlar
  (`DealershipAdminEndpoints.cs:111-142`), yani ikisi kodda zaten ayrışmıyor. Ayrımı nasıl
  kuracağı (ör. `JsonElement`, `Optional<T>`) DEV'in plan kararıdır. **Boş harita** kayıtlı
  hesapların hepsini siler.
- Haritada D1'de olmayan bir kod alan hatasıdır.

*Gerekçe:* yönetici profil ekranında tek "Kaydet" görür. İki ayrı kaydetme, aynı sürüm üzerinde
iki ayrı çatışma yolu açardı. "Alan yoksa değişmez" kuralı, `0020`'nin alansız gövde gönderen
istemcisinin ve testlerinin veri silmemesi içindir.
*Bedel:* `0020`'nin opsiyonel şube alanlarından farklı bir kural: orada `null` "boşalt" demek
(`Branch.cs:165-193`). Hesaplar bir koleksiyondur ve kaybı yeniden girilmesi pahalı bir veridir.

**D5 — Panel yüzeyi: profil ekranında "Sosyal hesaplar" bölümü.** (R1, R8) Her platform için,
D1'in sırasıyla, platform adını etiket alan bir adres alanı vardır. "Ekle" ya da "kaldır" düğmesi
yoktur: alanı boşaltmak siler. Alanlar soldan sağa yazılır (`dir="ltr"`) ve adres klavyesi açar.
Bölümün yardım metni "Boş bırakılan platform vitrinde görünmez" der. Alan hatası alanın altında,
çevrilmiş metinle görünür.
*Gerekçe:* altı sabit alan, dinamik bir listeden daha az durum ve kriter üretir.
*Bedel:* ekranda her zaman altı alan durur.

**D6 — Vitrin: ana sayfa altlığında simgeli "Bizi takip edin" bloğu.** (R4, SS2, SS3) *(SS3'ün
cevabıyla yeniden yazıldı, sahip kararı, 2026-11-26.)*
- Blok "Kurumsal"dan sonra, "Diller"den önce gelir. Bağlantılar D1'in sırasıyla dizilir. Arapçada
  satır sağdan sola akar, yani ilk simge sağdadır. Belgedeki sıra her dilde D1'dir.
- **Bağlantı görsel olarak platformun simgesidir** (satır içi SVG, Context › Simge kaynağı).
  Simge `aria-hidden="true"` taşır ve odaklanamaz.
- **Bağlantının erişilebilir adı platformun adıdır** ("Instagram"). Ad markanın kendi adıdır,
  çevrilmez ve dilden bağımsızdır. Görünür metin yoktur.
- Simge temanın metin rengiyle (`currentColor`) basılır ve SVG'de sabit renk yoktur. Görünen boyutu
  24 CSS pikseldir. **Dokunma hedefi en az 44×44 CSS pikseldir.** Klavye odağı görünür.
- Hedef kayıtlı adrestir. Bağlantı `rel="me"` taşır ve `target` taşımaz (aynı sekmede açılır).
- Simgeler ayrı bir parçadan gelir (D9(b)). *(QA 2 revizyonu:)* Parça gelene kadar blok **hiç
  basılmaz**; parça geldiğinde simgeli bağlantılar son boyutlarıyla (44×44) bir kez basılır ve
  sonra yer değiştirmez. **Parça yüklenemezse** (*CLARIFY B5:* parçanın dinamik içe
  aktarması reddedilirse) her bağlantı platformun adını görünür metin olarak gösterir ve hedefi
  değişmez. Bu durumda kutu metne göre genişleyebilir. Altlık C3'ün mobil görüntü alanında ilk
  ekranın altında olduğu için bu kayma C3'ün ölçtüğü ilk ekran CLS'ine girmez (**AC-80** bunu ölçer).
- **LinkedIn** için simge yoktur (**D13**). Bağlantı aynı kutuda platformun adını görünür metin
  olarak gösterir.
- Hiç hesap yoksa blok, başlığıyla birlikte hiç basılmaz.
- Okuma sürerken ve okuma hata verdiğinde de blok basılmaz, hata metni yoktur. Altlık bir hata
  ekranının yeri değildir (`0014` AC-54).
- Vitrin kataloğuna eklenen tek metin bloğun başlığıdır. Platform adları koddadır (D1).

*Gerekçe:* altlık bugün yalnız ana sayfada var ve yapısal veri de yalnız ana sayfada (C3). Sahip
simge istedi. Erişilebilir ad, ekran okuyucunun simgeyi "Instagram" diye okumasını sağlar. Tek
renk, marka kullanım kurallarına takılmamak içindir. 44 piksel dokunma hedefi WCAG 2.5.5'in
değeridir. Yeni sekme açmamak erişilebilirlik varsayılanıdır.
*Bedel:* liste, detay ve içerik sayfalarında hesaplar görünmez (X4). Simgeler marka renginde
değildir. Simge parçası hesaplı ana sayfaya bir istek ekler. *(QA 2 revizyonu:)* Blok parça gelene
kadar hiç basılmadığı için ziyaretçi parça gelmeden altlığa kaydırırsa "Diller" bölümü aşağı kayar.
Bu ekranın altında kaldığından C3'ün CLS'ine girmez (AC-80); görüntü alanı yaklaşık 2271 px'ten
uzun ekranlarda ilk ekranda görünebilir — kabul edilmiş risk. *Revizyon (2026-11-26, AP-08): blok
bütünüyle tembel parçaya taşındı, bekleyen kutuları girişte çizmek ilk paket bütçesini aşıyordu
(QA 2 BULGU-1) — Proje Yöneticisi kararı.*

**D7 — Ziyaretçiye dönük okuma `socialLinks` dizisini taşır.** (R6, C2) Her öğe yalnız `platform`
(kod) ve `url` taşır ve öğeler D1'in sırasıyla dizilir. Hiç hesap yoksa dizi **boştur**, `null`
değildir. Yanıt dilden bağımsızdır. Altlık bu okumayı kullanır, ikinci bir istek eklenmez.
`GaleriPublicUcTests`'in anahtar kümesi bilerek değişir (Context tablosu).
*Gerekçe:* `0014` **D15**'in izin listesi ilkesi. Kod, uçta platform adına çevrilir.
*Bedel:* yok.

**D8 — `sameAs` kök `AutoDealer` nesnesindedir.** (R5, C3, C4) Değeri adreslerin dizisidir. Sıra
altlıkla ve D1'le aynıdır. Hiç hesap yoksa anahtar yoktur. `department` öğelerinde `sameAs`
yoktur. Görünen ad okunamazsa blok hiç basılmaz (`0004` **D15**), `sameAs` de basılmaz. Bu kartla
`0004` **AC-52**'nin yokluk listesi `openingHoursSpecification`, `geo`, `logo` olur.
*Gerekçe:* `0004` **D15**'in notu: kapalı liste bu kartın kapsamında genişler.
*Bedel:* yok.

**D9 — Bütçe.** (C5)
- **(a)** `/ar/` ana sayfa belgesinde bu kartın eklediği bayt **en çok 1536 B**'tır (sıkıştırmasız).
  Kurgulanmış en kötü durum: altı platformun hepsi girilmiş, her adres 200 karakter ve hiçbirinde
  `&` yok.
- **(b)** *(SS3'ün cevabıyla yeniden yazıldı.)* **Simgeler ne ilk pakete ne belgeye girer: ayrı,
  tembel yüklenen bir parçadadırlar.**
  - İlk paket `main` tabanına göre **en çok 512 B** büyür. *(QA 2 revizyonu:)* "İlk paket" = giriş
    JS + giriş CSS + girişin statik içe aktardığı ya da `index.html`'in `modulepreload` ettiği her
    JS, `gzip -9`. Giriş JS'i simge yollarını taşımaz. *Revizyon (2026-11-26, AP-08): protokol tek dosyalı girişe göre yazılmıştı; ikinci kritik dosya sayılmıyordu (QA 2 BULGU-1) — Proje Yöneticisi kararı.*
  - Simge parçası **sabit adlı tek bir dosyadır** (`social-icons`): dinamik içe aktarmayla yüklenir ve
    adı derleme yapılandırmasında belirlenir *(CLARIFY B4)*. `gzip -9` ile **en çok 3072 B**'tır.
    Beş yolun ölçülen modül boyutu 1750 B (Context).
  - Parça yalnız **en az bir hesabı olan galerinin ana sayfasında** istenir. Hesapsız ana sayfa ile
    liste, detay ve içerik sayfaları onu hiç istemez.
- **(c)** C3 ana sayfa, liste ve detayda yeşil kalır. Perf koşusu (a)'nın altı hesabını sınır
  uzunlukta tohumlar (`0022` **D11** deseni). Bir sayfanın medyanı **2450 ms**'yi aşarsa ikinci
  koşu istenir ve LCP öğesi PR'a yazılır (`0004` **D17**(c)).

*Gerekçe:* altı adresin kodlanmış hâli yaklaşık 1230 B'tır (CLARIFY B3: her adres en çok 200
bayt). Simgelerin yolları ölçüldü: 1750 B gzip (Context), +512 B'lık payı tek başına aşar. Belgeye basmak, arayüzü Host'a taşımak demek
(tam SSR yok, `0004` **C1**; altlık uçta, galeri okumasından basılıyor) ve her ana sayfa belgesini
hesap olsa da olmasa da büyütür. Tembel parça yalnız simge gösterecek sayfada iner. Blok altlıkta,
ilk ekranın altında ve galeri okumasından sonra basılır, yani LCP öğesine dokunmaz.
*Bedel:* her `&` beş bayt ekler. Sorgu dizesi `&` ile dolu, el yapımı bir adres sınırı aşabilir.
Bunu ancak Yönetici yazabilir ve asıl kapı C3'tür. Hesaplı ana sayfaya bir istek eklenir. Parça
gelene kadar bağlantılar simgesizdir.

**D10 — Yetki galeri yönetim grubunun politikasıdır.** (R7, C7) Satış Danışmanı ve İçerik Editörü
`403`, oturumsuz istek `401` alır. Panelin "erişim yok" ekranı `0020` **D23**'ten gelir.
*Gerekçe:* hesaplar profilin parçası (D4).
*Bedel:* yok.

**D11 — Hata kodları ve alan anahtarları.** (C9) Alan anahtarı `socialLinks.<kod>`.
- `gallery.social_link_invalid`: D2'nin biçim kuralları.
- `gallery.social_link_too_long`: kırpıldıktan sonra UTF-8 olarak 200 baytı aşan adres *(QA 2: metin
  "200 karakter" diyordu, D2 ve kod bayt sayıyor)*.
- `gallery.social_platform_unknown`: D1'de olmayan kod. Bu kodun alan anahtarı gövdedeki kodu taşır.

Panel üçünü de çevirir. Alan hatası `422`'dir (`DealershipProblems`, `DealershipAdminEndpoints.cs:160-168`).
*Gerekçe:* `0020`'nin alan hatası deseni.
*Bedel:* yok.

**D12 — Yeni domain terimi: "Sosyal Hesap".** (DoD T7) Galerinin D1'deki platformlarda tuttuğu,
vitrinde gösterilen hesap adresi. Galeri başına platform başına en çok bir tane.
*Gerekçe:* sözlükte yok.
*Bedel:* yok.

**D13 — LinkedIn simgesiz gösterilir. (CLARIFY B4 ölçümüyle bulundu — Proje Yöneticisi kararı,
2026-11-26: öneri kabul, alternatifler reddedildi.)** (D1, D6) Simple Icons LinkedIn simgesini 14.0.0'da kaldırdı ve güncel pakette
yok (Context). LinkedIn bağlantısı diğerleriyle aynı 44 px kutuda **platformun adını görünür metin
olarak** gösterir. Erişilebilir adı bu metindir. Diğer beş platform simgelidir. D1'in listesi
değişmez (sahip kararı, SS1).
*Gerekçe:* eski bir sürümden (13.x) yol kopyalamak CC0 açısından mümkün, ama simgenin kaldırılması
marka sahibinin talebiyle ilişkili olabilir ve Proje Yöneticisi'nin "marka kurallarına takılmamak"
gerekçesine ters düşer. Başka bir simge kümesi (ör. MIT lisanslı) ikinci bir kaynak ve lisans notu
getirir, marka sorununu da çözmez.
*Bedel:* altlıkta simgeler arasında bir metin bağlantısı durur. *Alternatifler (Proje Yöneticisi
seçerse):* (a) 13.21.0'daki yol; altı yol 2003 B gzip ölçüldü, 3072 B'ın içinde kalır.
(b) LinkedIn'in kendi marka kitindeki simge, LinkedIn'in kurallarıyla. **İkisi de reddedildi**
(Proje Yöneticisi kararı, 2026-11-26): (a) kaldırılmanın muhtemel nedeni olan marka talebine ters
düşer, (b) ayrı kullanım şartları getirir. İkisi de "marka kurallarına takılmamak" gerekçesine aykırı.

---

## Sahibe sorular

> **Cevaplandı (sahip kararı, 2026-11-26):** **SS1** → altı platform, D1 aynen. **SS2** → yalnız
> ana sayfa altlığı, öneri kabul. **SS3** → **platform simgeleri**, öneriden farklı. D6, D9 ve X2 bu
> cevapla değişti. Simge kaynağı Proje Yöneticisi kararıdır (Context).

- **SS1. Hangi platformlar?** Galeri bugün hangi platformlarda hesap tutuyor ya da tutacak? Listede
  olmayan bir platform kod değişikliği ister (D1). **Cevap (sahip kararı, 2026-11-26): öneri kabul.**
  **Öneri:** Instagram, Facebook, YouTube, TikTok, X, LinkedIn. Türkiye ve Körfez'de araç galerileri
  için yaygın olanlar bunlar. Kullanılmayan platformun alanı boş kalır ve vitrinde görünmez.
- **SS2. Hesaplar vitrinde nerede görünsün?**
  **Öneri:** yalnız ana sayfanın altlığında, "Bizi takip edin" başlığıyla (D6). Altlık bugün yalnız
  ana sayfada var. Hesapları her sayfada göstermek, her sayfaya altlık eklemek demek ve ayrı bir
  karar (X4). **Cevap (sahip kararı, 2026-11-26): öneri kabul.**
- **SS3. Simge mi, ad mı?** Bağlantılar platform simgesiyle mi, platformun adıyla mı görünsün?
  **Öneri:** ad (D6, X2). Simgeler sayfa paketini büyütür ve her markanın kullanım kuralı var. Ad
  her dilde okunur.
  **Cevap (sahip kararı, 2026-11-26): simge.** → D6, D9, X2.

---

## Acceptance Criteria

> Panel kriterleri Yönetici oturumuyla, aksi yazılmadıkça varsayılan kurulumda (tr/ar/en) koşar.
> "Geçerli adres" D2'yi sağlayan bir adrestir (ör. `https://www.instagram.com/otogaleri`). Belge
> kriterleri sunucu yanıtının kendisiyle, JavaScript çalıştırılmadan ölçülür (`0004` C13).

### Panel: giriş, değiştirme, silme (R1, R3 · D3, D4, D5)

- **AC-1.** Galeri profili ekranının "Sosyal hesaplar" bölümü altı adres alanı gösterir. Sırası
  Instagram, Facebook, YouTube, TikTok, X, LinkedIn'dir. Her alanın etiketi platformun adıdır.
- **AC-2.** Kayıtlı hesabı olan platformun alanı kayıtlı adresle dolu gelir. Hesabı olmayan
  platformun alanı boş gelir.
- **AC-3.** Yönetici boş Instagram alanına geçerli bir adres yazıp kaydettikten sonra ekran yeniden
  açıldığında, Instagram alanı o adresi taşır.
- **AC-4.** Kayıtlı Instagram adresini başka bir geçerli adresle değiştirip kaydetmek, eski adresin
  yerine yenisini koyar. Okuma yanıtında tek bir `instagram` öğesi vardır ve o öğe yeni adresi taşır.
- **AC-5.** Kayıtlı bir adresin alanını boşaltıp kaydetmek o hesabı siler. Okuma yanıtında o
  platform yoktur.
- **AC-6.** Yalnız boşluktan oluşan değerle kaydetmek, alanı boşaltmakla aynı sonucu verir.
- **AC-7.** Başında ve sonunda boşluk olan geçerli bir adres kırpılarak saklanır.

### Doğrulama (R2 · D2, D11)

- **AC-8.** `http://www.instagram.com/otogaleri` ile kaydetme `422` döner. Yanıt `socialLinks.instagram`
  alanında `gallery.social_link_invalid` taşır.
- **AC-9.** Instagram alanına `https://www.facebook.com/otogaleri` ile kaydetme `422` ve
  `gallery.social_link_invalid` döner.
- **AC-10.** `https://www.instagram.com/` (yolsuz) ile kaydetme `422` ve `gallery.social_link_invalid` döner.
- **AC-11.** `https://kullanici@www.instagram.com/otogaleri` ile kaydetme `422` ve
  `gallery.social_link_invalid` döner.
- **AC-12.** `https://www.instagram.com:8443/otogaleri` ile kaydetme `422` ve
  `gallery.social_link_invalid` döner.
- **AC-13.** `https://WWW.Instagram.COM/otogaleri` kabul edilir ve girildiği gibi saklanır.
- **AC-14.** Kırpıldıktan sonra UTF-8 olarak 200 bayt olan geçerli bir ASCII adres kabul edilir.
  201 baytlık adres `422` ve `socialLinks.instagram` alanında `gallery.social_link_too_long` döner.
- **AC-15.** `https://www.facebook.com/profile.php?id=100012345` Facebook alanında kabul edilir.
- **AC-16.** Gövdede `myspace` koduyla kaydetme `422` döner. Yanıt `socialLinks.myspace` alanında
  `gallery.social_platform_unknown` taşır.
- **AC-17.** Hatalı bir adresle yapılan kaydetmede profilin hiçbir alanı değişmez. Aynı
  kaydetmedeki geçerli görünen ad değişikliği ve başka bir platformun geçerli adresi de yazılmaz.
- **AC-18.** İki platformun adresi hatalı olan bir kaydetmenin yanıtı iki alanın da hatasını taşır.
- **AC-19.** Panelde alan hatası ilgili adres alanının altında, ekranın dilinde görünür ve yazılan
  değer alanda kalır.

### Eşzamanlı yazma (R3 · D4)

- **AC-20.** Eski sürümle gönderilen ve sosyal hesap değiştiren bir kaydetme `409` döner. Kayıtlı
  hesaplar değişmez.
- **AC-21.** Sosyal hesap değiştiren başarılı bir kaydetme profil sürümünü bir artırır ve yanıt yeni
  sürümü döner.
- **AC-22.** Gövdesinde sosyal hesap alanı hiç olmayan bir kaydetme (`0020`'nin gövdesi) kayıtlı
  hesapları değiştirmez.
- **AC-23.** Gövdesinde boş sosyal hesap haritası olan bir kaydetme kayıtlı hesapların hepsini siler.

### Yetki (R7 · D10)

- **AC-24.** Satış Danışmanı oturumuyla sosyal hesap taşıyan profil kaydetmesi `403` döner ve
  hesaplar değişmez.
- **AC-25.** İçerik Editörü oturumuyla aynı istek `403` döner ve hesaplar değişmez.
- **AC-26.** Oturumsuz aynı istek `401` döner.
- **AC-27.** Satış Danışmanı panelin galeri profili adresini doğrudan açtığında "erişim yok"
  ekranını görür. Sosyal hesap alanları basılmaz.

### Panel durumları ve dil (R8 · D5)

- **AC-28.** Profil okunurken ekranın yükleniyor durumu görünür ve sosyal hesap alanları basılmaz.
- **AC-29.** Profil okuması hata verirse ekranın hata durumu ve yeniden deneme eylemi görünür.
  Sosyal hesap alanları basılmaz.
- **AC-30.** Hiç hesabı olmayan galeride bölüm altı boş alanı ve "Boş bırakılan platform vitrinde
  görünmez" yardım metnini gösterir.
- **AC-31.** `ar` panelinde bölümün başlığı ve yardım metni Arapçadır ve bölüm sağdan sola dizilir.
  Adres alanları soldan sağa yazılır.
- **AC-32.** `GET /api/admin/gallery` yanıtı kayıtlı hesapları platform kodundan adrese bir harita
  olarak taşır. Hiç hesap yoksa harita boştur.

### Ziyaretçiye dönük okuma (R6 · C2, D7)

- **AC-33.** `/api/dealership/gallery` yanıtının kök anahtar kümesi tam olarak `branches`,
  `displayName`, `socialLinks`'tir. `socialLinks`'in her öğesinin anahtar kümesi tam olarak
  `platform`, `url`'dir.
- **AC-34.** Önce YouTube, sonra Instagram adresi girilmiş bir galerinin yanıtında `socialLinks`
  sırası `instagram`, `youtube`'dur.
- **AC-35.** Hiç hesabı olmayan galerinin yanıtında `socialLinks` boş bir dizidir.
- **AC-36.** `locale=tr`, `locale=ar` ve `locale=en` yanıtlarının `socialLinks` dizileri aynıdır.

### Vitrin altlığı (R4, R8 · D6)

- **AC-37.** Instagram, YouTube ve LinkedIn hesabı olan galerinin ana sayfa altlığında "Bizi takip
  edin" bloğu üç bağlantı taşır. Sırası Instagram, YouTube, LinkedIn'dir. Her bağlantı o platformun
  simgesini satır içi SVG olarak taşır ve hedefi kayıtlı adrestir.
- **AC-38.** Kurumsal sayfası olmayan, iki şubeli ve iki hesaplı bir kurulumda altlığın bağlantı
  hedefleri sırasıyla şunlardır: iki şube telefonu, `/tr/cars`, iki hesap adresi, `/tr/`, `/ar/`,
  `/en/`. (`0014` AC-46'nın hesaplı hâli.)
- **AC-39.** Hesap bağlantıları `rel="me"` taşır ve `target` özniteliği taşımaz.
- **AC-40.** Hiç hesabı olmayan galerinin altlığında blok yoktur, başlığı da yoktur. Altlığın diğer
  blokları görünmeye devam eder.
- **AC-41.** Galeri okuması sürerken altlıkta blok yoktur.
- **AC-42.** Galeri okuması hata verdiğinde altlıkta blok ve hata metni yoktur.
- **AC-43.** `/ar/` ana sayfasında blok başlığı Arapçadır. Bağlantıların belgedeki sırası D1'dir ve
  ilk bağlantı (Instagram) satırın sağ ucundadır. `/en/`'de başlık İngilizcedir.
- **AC-44.** Liste, detay ve içerik sayfalarında sosyal hesap bağlantısı yoktur.

### Yapısal veri (R5 · C3, C4 · D8, D16)

- **AC-45.** `AutoDealer` bloğunda (kökte ve `department` öğelerinde) `openingHoursSpecification`,
  `geo` ve `logo` anahtarları yoktur. (`0004` AC-52'nin bu karttaki hâli.)
- **AC-46.** Instagram, YouTube ve LinkedIn hesabı olan galerinin `/tr/` belgesinde `AutoDealer`'ın
  `sameAs`'ı tam olarak bu üç adrestir. Sırası Instagram, YouTube, LinkedIn'dir.
- **AC-47.** Hiç hesabı olmayan galerinin `/tr/` belgesinde `AutoDealer` `sameAs` anahtarı taşımaz.
- **AC-48.** `department` öğelerinin hiçbiri `sameAs` taşımaz.
- **AC-49.** `https://www.facebook.com/profile.php?id=1&sk=about` adresi JSON-LD'nin ham metninde
  `\u0026` ile kodlanmıştır. Ayrıştırılan değer kayıtlı adrese eşittir.
- **AC-50.** Görünen ad okunamadığında, hesap kayıtlı olsa bile `/tr/` belgesi `AutoDealer` bloğu
  taşımaz.
- **AC-51.** Liste, detay ve içerik belgelerinde `sameAs` anahtarı taşıyan JSON-LD yoktur.
- **AC-52.** Yönetici bir hesabı kaydettikten sonra istenen ilk `/tr/` belgesinin `sameAs`'ı o
  adresi taşır.
- **AC-53.** `/tr/` belgesi hesap kayıtlıyken de tam olarak bir JSON-LD bloğu taşır.

### Performans ve paket (C5 · D9)

- **AC-54.** Kurgulanmış en kötü durumda (altı platformun hepsi, her adres **tam 200 bayt** ve yolu
  Arapça harflerle doldurulmuş, hiçbirinde `&` yok; CLARIFY B3) `/ar/` ana sayfa belgesi, aynı kurulumun hesapsız `/ar/` belgesinden
  en çok **1536 B** büyüktür.
- **AC-55.** İlk paket — giriş JS + giriş CSS + girişin statik içe aktardığı ya da `index.html`'in
  `modulepreload` ettiği her JS, `gzip -9` — `main` tabanına göre en çok **512 B** büyür.
  *Revizyon (2026-11-26, AP-08): protokol tek dosyalı girişe göre yazılmıştı; ikinci kritik dosya sayılmıyordu (QA 2 BULGU-1) — Proje Yöneticisi kararı.*
- **AC-56.** Perf koşusu altı hesabı AC-54'ün kurgusuyla tohumlar. C3 ana sayfa, liste ve detayda
  yeşildir. Bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşunun sonucu ve LCP öğesi PR'da yazılıdır.
- **AC-57.** Bu kartla vitrin kataloğuna yalnız blok başlığının anahtarı eklenir. Panelin metinleri
  (bölüm başlığı, yardım metni, üç hata kodu) panel kataloğundadır.

### Şema ve çeviri (C6 · DoD T5, T6)

- **AC-58.** Bu kart `dealership` şeması dışında hiçbir şemaya dokunan migration eklemez.
- **AC-59.** Panelin bölüm başlığı, yardım metni ve üç hata kodunun metni ile vitrin bloğunun başlığı
  tr, ar ve en'de vardır. Eksik anahtar kontrolü yeşildir.

### Simgeler (R4, R8 · D6, D9 — sahibin SS3 cevabıyla eklendi)

- **AC-60.** Her hesap bağlantısının erişilebilir adı platformun adıdır ("Instagram", "Facebook",
  "YouTube", "TikTok", "X", "LinkedIn"). Ad `/tr/`, `/ar/` ve `/en/`'de aynıdır.
- **AC-61.** Simge parçası yüklendikten sonra LinkedIn dışındaki bağlantıların görünür metni yoktur. Simge SVG'si
  `aria-hidden="true"` taşır ve odaklanamaz.
- **AC-62.** Her hesap bağlantısının dokunma hedefi en az 44×44 CSS pikseldir.
- **AC-63.** Simge bağlantının metin rengiyle (`currentColor`) boyanır. SVG'nin hiçbir öğesi sabit
  bir renk değeri taşımaz.
- **AC-64.** Hesap bağlantısına klavyeyle odaklanıldığında görünür bir odak göstergesi vardır.
- **AC-65.** Simgelerin bulunduğu kaynak dosya Simple Icons'ı ve CC0 1.0 lisansını adıyla anan bir
  not taşır. Bu kart `package.json`'a çalışma zamanı bağımlılığı eklemez.
- **AC-66.** Giriş JS'i beş simgenin hiçbirinin yol verisini taşımaz.
- **AC-67.** Derleme çıktısında simgeler adı `social-icons` ile başlayan tek bir JS dosyasındadır ve
  o dosya `gzip -9` ile en çok **3072 B**'tır.
- **AC-68.** Simge parçası yalnız en az bir hesabı olan galerinin ana sayfasında istenir. Hesapsız
  ana sayfada ve liste, detay, içerik sayfalarında istenmez.
- **AC-69.** Simge parçasının dinamik içe aktarması reddedildiğinde her hesap bağlantısı platformun adını görünür metin
  olarak gösterir ve hedefi kayıtlı adres olarak kalır.
- **AC-70.** Simge parçası gelene kadar "Bizi takip edin" bloğu basılmaz. Parça geldiğinde simgeli
  bağlantılar son boyutlarıyla (44×44) basılır ve sayfa yüklemesi bitene kadar konumları ve
  boyutları değişmez. (Parça reddedildiğinde kutu metne göre genişleyebilir; AC-80.)
  *Revizyon (2026-11-26, AP-08): ilk metin "gelmeden önceki ve sonraki bağlantıların konumu aynıdır"
  diyordu; blok bütünüyle tembel parçaya taşınınca (QA 2 BULGU-1) "önce" bağlantı yok — Proje
  Yöneticisi kararı.*

### QA CLARIFY turuyla eklenenler (2026-11-26)

- **AC-71.** `"socialLinks": null` taşıyan bir profil kaydetmesi kayıtlı hesapları değiştirmez (CLARIFY X1).
- **AC-72.** Instagram alanına `https://instagram.com.kotu.site/x` ile kaydetme `422` ve
  `gallery.social_link_invalid` döner.
- **AC-73.** Instagram alanına `https://www.instagram.com%2F@kotu.site/x` ile kaydetme `422` ve
  `gallery.social_link_invalid` döner.
- **AC-74.** Instagram alanına Kiril `а` (U+0430) içeren `https://www.instagrаm.com/x` ile kaydetme
  `422` ve `gallery.social_link_invalid` döner.
- **AC-75.** Instagram alanına `javascript:alert(1)` ile kaydetme `422` ve `gallery.social_link_invalid` döner.
- **AC-76.** Instagram alanına `https://www.instagram.com:443/x` ile kaydetme `422` ve
  `gallery.social_link_invalid` döner.
- **AC-77.** `https://www.instagram.com/` ve ardından 87 Arapça harf (UTF-8 olarak 200 bayt) kabul
  edilir. 88 Arapça harfli adres (202 bayt) `422` ve `gallery.social_link_too_long` döner.
- **AC-78.** Hesapsız galerinin ana sayfasında `social-icons` dosyası istenmez. En az bir hesabı olan
  galerinin ana sayfasında tam olarak bir kez istenir.
- **AC-79.** Simge parçasının dinamik içe aktarması reddedildiğinde konsola yakalanmamış hata
  düşmez ve altlığın diğer blokları görünür kalır.
- **AC-80.** C3'ün mobil profilinde (Lighthouse mobil görüntü alanı) altı hesaplı `/tr/` ana sayfanın
  altlığı ilk ekranın altındadır: ilk boyamada altlığın üst kenarı görüntü alanının yüksekliğinden
  aşağıdadır.
- **AC-81.** LinkedIn bağlantısı simge taşımaz. Görünür metni ve erişilebilir adı "LinkedIn"dir.
  Dokunma hedefi diğer bağlantılarla aynı, en az 44×44 CSS pikseldir (**D13**).
- **AC-82.** Simge kaynak dosyasının notu Simple Icons'ın sürümünü (`16.38.1`) ve beş simgenin
  marka kuralı bağlantılarını anar.

### QA 2 revizyonuyla eklenenler (2026-11-26, AP-08, Proje Yöneticisi kararı)

- **AC-83.** NUL, satır sonu, U+202E ya da `"><script>` içeren bir adresle kaydetme `422` ve
  `gallery.social_link_invalid` döner; hiçbir hesap değişmez (D2, QA 2 BULGU-2).


---

## Definition of Done

- [x] Önerilen kararlar (**D1–D12**) onaylandı (Proje Yöneticisi kararı, 2026-11-26) ve QA CLARIFY
      turu (B1–B5, kartlar arası X1–X3 → **D13** ve kriter düzeltmeleri, AC-71–AC-82) işlendi.
      *(Takım Yöneticisi (ajan) kararı, 2026-11-26.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0024-ac<n>.png`),
      boş ve hata durumları dâhil; panel bölümünün ve altlık bloğunun Arapça görüntüsü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor. (Beklenen: **yok**, D3.)
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi ("Sosyal Hesap", D12).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. Bilerek
      değişen eski testler (§ Context tablosu ve uygulamada bulunan ekler) PR'da listelendi.
      *(CLARIFY X3:)* Etiketi değişen eski testler **çok etiketli** kalır ve eski etiketini
      kaybetmez: `0004/AC-52, 0024/AC-45: …`, `0014/AC-14, 0024/AC-33: …` (mevcut desen
      `0017/AC-51, AC-52, AC-56`).
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı. Taşımadan sonra `tests/Architecture.Tests`'in
      TAMAMI koşturuldu.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | | `git cat-file -e <impl>^:specs/0024-galerinin-sosyal-hesaplari.md` |
| **Kriter sayısı: önce → bugün** | → | |
| **Spec revizyon sayısı** | | Onay anındaki CLARIFY turu sayılır (ADR 0008 / K3). |
| **Düzeltme turu sayısı** | | `0` yalnız T10 `[x]` ise. |
| **Dış bulgu (gerçek/gürültü)** | / | |
| **Uygulama içi bulgu** | | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | Arşive taşınırken `izleniyor`. |

**Bu spec'ten çıkarılan ders:**
