# 0014 — Ana Sayfa

| | |
|---|---|
| **Durum** | Tamamlandı (2026-11-17) — uygulama #85 → `90b4e98` |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin · `VehicleCatalog` (okuma yüzeyi) · `VehicleReference` (okuma yüzeyi) · `Dealership` (galeri profilinin public okuması — **D6**) |
| **Bağımlı spec'ler** | `0001`, `0002`, `0003`, `0012c`, `frontend-001`, `frontend-002`. Sıra kararı: **D1**. |
| **Dayanak** | ADR 0003 Ek 1, sıra #7 |
| **Karar bekliyor** | yok — S1–S6 → **D1–D7** (CLARIFY turu); kriter turunun dört bulgusu → **D8–D10** (2026-11-17); QA CLARIFY turunun on bulgusu → **D11–D20** (2026-11-17); plan turunun sekiz sorusu → **D21–D28** (2026-11-17). |

> **Plan turu (2026-11-17, `5ddeeb9` üzerinde).** Developer uygulama planını çıkarırken
> sekiz açık nokta buldu (P2–P9, § Verilen kararlar — plan turu). Önerilerin sekizi de
> Takım Yöneticisi (depo sahibi) tarafından **olduğu gibi** onaylandı ve **D21–D28** olarak
> işlendi. **AC-72** yeniden yazıldı (D24), **AC-73** ve **AC-74** eklendi (D24, D25).
> Kriterler `main`'de olduğu için bu tur **sayılan bir revizyondur** (ADR 0008 / K3).
>
> **QA CLARIFY turu (2026-11-17, `14a04bd` üzerinde).** Uygulama başlamadan önce bir QA
> oturumu spec'i cevapsız sorular için okudu ve on boşluk buldu (B1–B10, § Verilen
> kararlar — QA CLARIFY turu). Önerilerin onu da Takım Yöneticisi (depo sahibi) tarafından
> onaylandı ve **D11–D20** olarak işlendi. En büyüğü **D11**: veride marka kavramı yok,
> marka bölümü (R4, AC-29–AC-36) kapsam dışına çıktı (**X13**). R4, R2, R9 ve C9, C11
> değişti; AC-8, AC-14, AC-37, AC-46, AC-49, AC-50, AC-52, AC-53 yeniden yazıldı;
> **AC-62–AC-72** eklendi (yeni numaralar konu başlığının altına yerleşir, sırayla değil). Kriterler `main`'de olduğu için bu tur **sayılan bir
> revizyondur** (ADR 0008 / K3).
>
> **Kriter turu (2026-11-17, `3cf58d8` üzerinde).** Kabul kriterleri yazılırken Context
> yeniden ölçüldü ve dört bulgu çıktı (K1–K4, § Context). Üçü bir Requirement'ı ya
> değiştirdi ya da ekledi: **R7** daraldı (çalışma bilgisinin verisi yok), **R11** ve
> **R12** eklendi. Kararlar **D8–D10**, Takım Yöneticisi (ajan) kararı. Bu tur R'ye
> dokunduğu için **sayılan bir revizyondur** (ADR 0008 / K3).
>
> **Context yeniden doğrulandı (2026-11-17).** Taslak 2026-10-05'te yazılmıştı; arada
> `0011`, `0012c` ve `0013` kapandı. Dört tespit işlendi (B1–B4, Takım Yöneticisi onayı
> 2026-11-17): **S3** yeniden yazıldı (B1), **S5** eklendi (B2), doğrulama uyarısı
> kaldırıldı (B3), **S4**'ün KVKK uyarısı cevaplandı (B4).
>
> **CLARIFY turu (2026-11-17).** S1–S5'in beş önerisi Takım Yöneticisi tarafından
> onaylandı ve **D1–D6** olarak işlendi; R1, R2, R3, R5, R7 kararlara göre yazıldı, **X9** ve
> **X10** eklendi. Kararlar işlenirken **R4** ile **D1** arasında bir çelişki çıktı → **S6**;
> önerisi onaylandı ve **D7** olarak işlendi (R4 yeniden yazıldı, X1 düzeltildi, **X11** eklendi).

---

## Intent

Vitrine gelen ziyaretçi bugün **doğrudan araç listesine düşüyor.** `frontend-001` bir kabuk
ve dil değiştirici kurdu, ama sitenin bir **girişi** yok: galerinin kim olduğunu, neyi iddia
ettiğini ve elindeki stoğun ne olduğunu söyleyen bir sayfa bulunmuyor.

Bu, ziyaretçinin ilk üç saniyesini boşa harcıyor. Araç almak, insanın birkaç gün içinde
birden çok galeriye baktığı bir iş; hatırlanan galeri, ne sattığını ilk ekranda söyleyen
galeri oluyor.

**Galeri sahibi** vitrinin bir siteye ait olduğunu hissettiren bir giriş istiyor; ziyaretçi
ise aradığı aracı listeye girmeden **daraltarak** başlamak istiyor.

Bu iş bittiğinde ziyaretçi ana sayfada galerinin iddiasını, öne çıkan araçları ve stoğun
büyüklüğünü görür; oradan arama/daraltma ile listeye geçer. *(2026-11-17: "stoktaki
markaları" yazıyordu; marka bölümü **D11** ile kapsam dışına çıktı → X13.)* Başarı ölçütü: **ziyaretçi ana
sayfadan ayrılmadan stoğun ne içerdiğini anlar.**

---

## Requirements

- **R1.** Ana sayfa, galerinin **adını** gösterir; ad galeri profilinden gelir. Logo bu
  sürümde yoktur (**X9**). → `0003` R1, **D6**.
- **R2.** Ana sayfa bir **giriş bölümü** (başlık + açıklama) taşır. Metin üç dilde
  **çeviri kataloğundan** gelir; galeri onu panelden değiştiremez. Giriş bölümünde bu
  sürümde **görsel yoktur**. → **D2**, **D18**.
- **R3.** Ana sayfa **öne çıkan araçları** gösterir: galerinin panelde **öne çıkardığı** ve
  vitrinde görünür (C3) ilanlar, vitrin listesiyle **aynı sırada**, **en çok altı** tane.
  Öne çıkarılmış ilan altıdan azsa yalnız onlar gösterilir; boşluk öne çıkarılmamış
  ilanla **doldurulmaz**. Hiç yoksa bölüm **görünmez**. → **D3**, **D4**.
- **R4.** *(Kapsam dışına çıktı, 2026-11-17 — **D11** → **X13**.)* Ana sayfa bu sürümde
  marka bölümü **taşımaz**: veride marka kavramı yoktur. *(Önceki metin: stoktaki markalar
  ve her markadaki araç sayısı, tıklanamaz — D7.)*
- **R5.** Ana sayfa bir **daraltma formu** taşır; form doldurulup gönderildiğinde ziyaretçi
  o daraltmanın uygulandığı listeye gider. Formun bu sürümdeki tek ekseni **kasa
  tipidir**; diğer eksenler **X10**. → **D1**.
- **R6.** Form, yayındaki **toplam araç sayısını** gösterir.
- **R7.** Ana sayfa bir **altlık** taşır: galerinin **aktif** şubelerinin adı, adresi ve
  telefonu (galeri profilinden), araç listesi ve dil bağlantıları. Altlık **yalnız var
  olan hedeflere** bağlantı verir; var olmayan kurumsal sayfaların bağlantısı konulmaz.
  Çalışma bilgisi bu sürümde yoktur (**X12**). → **D5**, **D6**, **D8**.
- **R8.** Ana sayfa **yükleniyor**, **boş** (vitrinde hiç yayında araç yok) ve **hata**
  durumlarını ele alır; bölümler **kısmen** yüklenemezse sayfanın tamamı düşmez.
  → `docs/frontend.md` § 3.
- **R9.** Ana sayfa etkin locale'lerde çalışır; Arapça'da tam RTL. Çevrilmeyen galeri adı
  karışık yönlü metin olarak doğru görünür. → **D20**. *(2026-11-17: "Latin marka adları"
  yazıyordu; marka bölümü D11 ile çıktı.)*
- **R10.** Ana sayfa `/{locale}/` adresinde yaşar; öneksiz `/` varsayılan locale'e yönlenir,
  geçersiz önek **404** verir. → `docs/architecture.md` § 3.
- **R11.** Araç listesi kendi adresine taşınır: `/{locale}/cars`. Listenin eski adresiyle
  paylaşılmış bağlantılar — daraltma ve sayfa bilgisi dâhil — aynı listeyi açmaya devam
  eder; ziyaretçiyi vitrine geri gönderen bağlantılar listeye gider. → **D9**.
- **R12.** Ana sayfa ve liste performans eşiklerini (`0001` C3) **ayrı ayrı** karşılar; ana
  sayfanın gelişi listenin ölçümünü ortadan kaldırmaz. → **D10**.

---

## Constraints

### Uyulması zorunlu

- **C1.** Sayfa **public**'tir; oturum gerektirmez. Public üyelik yoktur. → § 6.
- **C2.** Gizli alanlar yanıtta **hiç bulunmaz**; gizleme sorgu tarafında uygulanır.
  → **F3**, Yasak Listesi 41.
- **C3.** Yalnızca vitrinde görünür ilanlar gösterilir: `Yayında` ve `Rezerve` (rozetli).
  → **R6** (domain), `0001` AC-2/AC-3.
- **C4.** Fiyat `Money` ile taşınır; "fiyat sorunuz" `0` değildir; üç ondalıklı para
  birimleri bozulmaz. → Altın Kural 4, **P1–P4**.
- **C5.** Kullanıcıya görünen metin koda gömülmez; sayı içeren cümleler ICU ile kurulur
  ("128 aracı gör"). → Altın Kural 6, Yasak Listesi 31.
- **C6.** CSS'te fiziksel yön özellikleri kullanılmaz. → Yasak Listesi 25.
- **C7.** Renk ve tipografi `frontend-002`'nin token katmanından gelir; ham hex/px yoktur.
  → `docs/frontend.md` § 4.1.
- **C8.** Bileşenler `docs/frontend.md` § 4.2 envanterinden kullanılır; yeni ortak bileşen
  gerekçesiyle önerilir.
- **C9.** Görseller `loading="lazy"`, açık `width`/`height` ile; **öne çıkan bölümün ilk
  kartının kapağı hariç** (AC-27). Giriş bölümünde görsel yoktur (**D18**).
  → `docs/frontend.md` § 7 (CLS ve LCP). *(2026-11-17: "kapak/giriş görseli hariç"
  yazıyordu; giriş görselinin kaynağı yoktu.)*
- **C10.** `Accept-Language`'a göre otomatik yönlendirme yapılmaz. → Yasak Listesi 28.
- **C11.** Öne çıkan araç okuması ve toplam sayı, `VehicleCatalog`'un **kendi** okuma
  modelinden beslenir *(2026-11-17: marka sayımı D11 ile çıktı)*; başka modülün tablosuna erişilmez. → Yasak Listesi 1.
- **C12.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.

### KAPSAM DIŞI

- **X1.** **Markalar sayfası** (mockup'taki "Markalar" menüsü ve marka detay sayfası) —
  *neden:* ayrı bir marka sayfası kendi SEO ve içerik kararlarını getirir. Ana sayfadaki
  marka listesi bu sürümde yalnız gösterir (R4, D7). *(2026-11-17: gerekçe ilk yazıldığında
  "marka listesi daraltılmış listeye gider" diyordu; D7 ile düzeltildi. Aynı gün **D11**
  ana sayfanın marka listesini de çıkardı → X13.)*
- **X2.** **Bülten aboneliği formu** — *neden:* ADR 0003 Ek 1 sıra #13'ün işi; abonelik
  kaydı, çift onay ve abonelikten çıkma kendi spec'ini hak eder. Ana sayfanın ilk sürümünde
  bu bölüm **yoktur**.
- **X3.** **Kampanya alanları** — *neden:* kampanya `Content` modülünün verisidir (sıra #11)
  ve `docs/domain.md` **K1–K5** kendi kurallarını taşır.
- **X4.** **Favori ve karşılaştırma** — *neden:* sıra #12'nin işi.
- **X5.** **Serbest metin arama kutusu** — *neden:* `0001` § X1 ile aynı gerekçe; normalizasyon
  yükü kendi spec'ini hak eder.
- **X6.** **Blog, SSS ve kurumsal sayfaların içeriği** — *neden:* `Content` modülü yok
  (sıra #11). Altlıktaki bağlantılar için bkz. **S4**.
- **X7.** **JSON-LD ve meta enjeksiyonu** — *neden:* `0004` (SEO) bu konunun sahibidir.
- **X8.** **"En çok görüntülenen" veya popülerlik ölçütü** — *neden:* `Engagement` modülü yok.
- **X9.** **Galeri logosu** — yüklenmesi, galeriye atanması ve ana sayfada gösterilmesi.
  *neden:* logo bugün ne atanabiliyor ne gösterilebiliyor (§ Context, B2); bunu kapatmak bir
  panel akışını bu spec'e çeker. Ayrı bir kart olarak önerilir. Ana sayfa galeri adını
  metin olarak gösterir. → **D6**.
- **X10.** **Daraltma formunun kasa tipi dışındaki eksenleri** (mockup'taki marka, yıl,
  fiyat aralığı). *neden:* bu eksenler `0015`'in işi (sıra #9); `0001` X2/X3 onları kapsam
  dışı bırakmıştı. Eksenler `0015` ile geldiğinde formun genişlemesi o spec'in ya da ayrı
  bir kartın işidir. → **D1**.
- **X11.** **Ana sayfadaki markadan o markaya daraltılmış listeye geçiş.** *neden:* vitrin
  listesinde marka ekseni yok (`0001` X3); `0015` R1 onu getirecek. Eksen gelmeden bağlantının
  gidecek bir hedefi yoktur ve süzülmemiş listeye gitmek tıklamanın anlamını bozar. Eksen
  geldiğinde bağlantı `0015`'in ya da ayrı bir kartın işidir. → **D7**. *(2026-11-17:
  **D11** ile marka bölümünün kendisi X13'e çıktı; bu madde X13'ün içinde kalır.)*
- **X12.** **Şubelerin çalışma saatleri ve "şu an açık mı"** — *neden:* çalışma programının
  **verisi yok**: `0003` onu kapsam dışı bıraktı (X2) ve `Branch`'te alanı bulunmuyor
  (K1). Okunacak bir şey olmadan okuma yüzeyi yazılamaz; programın kaydı ve paneli
  `docs/domain.md` **B1–B4**'ü taşıyan ayrı bir kartın işidir. → **D8**.
- **X13.** **Ana sayfanın marka bölümü** — stoktaki markalar ve her markadaki araç sayısı
  (eski R4, AC-29–AC-36). *neden:* veride **marka kavramı yok** (§ Context, K5):
  `VehicleModel` yalnız kimlik ve kanonik ad taşıyor ve marka adın içinde
  (`'Toyota Corolla'`); `0010a` X14 marka taksonomisini `VehicleReference`'ın kendi spec'ine
  bıraktı. Sayılacak bir marka olmadan marka sayan okuma yazılamaz. Marka taksonomisi ayrı
  bir kart olarak önerilir; ana sayfanın marka bölümü ve `0015` R1'in marka ekseni ona
  bağlanır. → **D11**. *(Marka taksonomisi ve "Aracınızı bulun" formunun marka alanı →
  `0021`, 2026-11-19; marka bölümü ve marka başına araç sayısı `0021` X3 ile yine kapsam dışı.)*
- **X14.** **Üst çubuktaki dil değiştiricinin etkin locale'lere uyması** — *neden:* aynı
  kusur (D22) ama ana sayfanın değil kabuğun; `frontend-001`'in yüzeyi. Ayrı kart. → D23.

---

## Context

### Mevcut davranış — doğrulandı (2026-11-17, `main` @ `ab04cb5`)

- **Ana sayfa yok.** `frontend-001` bir uygulama kabuğu (`AppShell`) ve dil değiştirici kurdu;
  `/{locale}/` adresi doğrudan vitrin listesini açıyor (`frontend/src/shared/lib/route.ts`:
  liste ve detay dışında public bir rota türü yok).
- **Vitrin listesi ve ilan detayı hazır** (`0001`, `0002` tamamlandı); liste okuma modeli ve
  detay okuma yolu mevcut. Liste, **öne çıkarılmış ilanları önce**, eşitlikte en yeni
  yayınlananı önce sıralıyor (`ShowcaseListingQueries.cs`).
- **Öne çıkarma bir panel eylemi (B1).** `0012c` ilanı tek tek veya toplu olarak **öne
  çıkarma / öne çıkarmayı kaldırma** eylemini getirdi. Vitrin sıralama ağırlığı artık iki
  değerli: öne çıkarılmış veya değil (`Listing.cs`: `Feature()` / `Unfeature()`). Yani
  "öne çıkan araç" bugün **galerinin elle verdiği bir karar**, sıralamanın bir yan etkisi
  değil. Bu tespit **S3**'ü yeniden yazdırdı.
- **Galeri profilinin public okuması yok (B2).** `0003` galeri adını, şubeleri ve
  `Gallery.LogoMediaAssetId` alanını getirdi. Ama `Dealership`'in tek ucu panele ait
  (şube listesi); ziyaretçinin galeri adını, logosunu, adresini veya çalışma bilgisini
  okuyabileceği bir yüzey **yok**. Logo için ayrıca: `Media` artık yükleme ve türev üretimi
  yapıyor (`0011`), ama galeriye logo atayan **bir akış yok** — atama metodunun hiçbir
  çağıranı yok. Yani logo bugün ne yüklenebiliyor ne de gösterilebiliyor. R1 ve R7'nin
  profil kısmı bu yüzeye dayanıyor → **S5**.
- **Stoktaki marka ve marka başına araç sayısı hiçbir yerde sunulmuyor (B3).** 2026-10-05
  taslağının "eklenmekte olan okuma yüzeyi" uyarısı çözüldü: `VehicleReference` artık
  body-types, models ve dört terim türü (yakıt, şanzıman, çekiş, donanım) sunuyor. Ama bunlar
  **referans verisi** — kayıtlı bütün modeller, stoktan bağımsız; model kaydı da yalnız kimlik
  ve etiket taşıyor. "Stokta hangi marka var, kaç araç" sorusunu cevaplayan bir okuma **yok**.
  R4 bu okumaya dayanıyor.
- **Mockup ne gösteriyor:** giriş bölümü + daraltma formu (**Marka · Kasa tipi · Yıl · Fiyat
  aralığı** ve "128 aracı gör"), "Vitrin" başlıklı **6 öne çıkan araç**, numaralı **marka
  listesi** (marka + "14 araç"), bülten bölümü, dört kolonlu altlık (Keşfet / Kurumsal /
  Diller). Kartlarda "Tramersiz", "Yeni geldi", "Takasa açık", "Hibrit" rozetleri var.
- **Mockup'ın rozetleri `0001`'de yok.** `0001` R2 yalnızca **Rezerve** rozetini tanımladı;
  diğer dördü yeni kavramlar ve bu spec'te **kapsam dışıdır** (X3 ile ilişkili: "Yeni geldi"
  türetilebilir, "Takasa açık" opsiyonel alan). *(2026-11-17 notu: bu satır ilk yazıldığında
  "öne çıkan" bir `Content` slotu sayılıyordu; `0012c`'den sonra öne çıkarma bir panel
  eylemidir — B1, D3.)*

### Kriter turu ölçümü (2026-11-17, `main` @ `3cf58d8`)

Kriterler bu dört bulgunun üzerine yazıldı:

- **K1 — Çalışma bilgisinin verisi yok.** `Branch` ad, adres, koordinat, telefon, WhatsApp,
  e-posta, saat dilimi, durum ve slug taşıyor (`Branch.cs:36-53`); **çalışma programı
  alanı yok.** `0003` X2 programı, istisnaları ve "şu an açık mı"yı kapsam dışı bırakmıştı.
  R7'nin "çalışma bilgisi" kısmının okunacak bir verisi bulunmuyor → **D8**.
  Şube durumları: `Active`, `TemporarilyClosed`, `Closed` (`BranchStatus.cs`).
- **K2 — `/{locale}/` bugün LİSTENİN adresi.** `parseRoute`
  (`frontend/src/shared/lib/route.ts:96-107`) slug'sız her public yolu listeye çözüyor;
  `/{locale}/cars` de **zaten** listeyi açıyor. `0001`'den beri paylaşılan daraltılmış
  liste bağlantıları `/{locale}/?bodyType=…` biçiminde → **D9**.
- **K3 — Üç "Vitrine dön" bağlantısı `/{locale}/`'e gidiyor:** bulunamayan ilan
  (`ListingDetailPage.tsx:76`), talep başarısı (`LeadForm.tsx:60`) ve ilanı yayından kalkmış
  talep (`LeadForm.tsx:110`). Metinleri "Vitrine dön" (`detail.notFound.action`,
  `lead.success.action`); ana sayfa geldiğinde üçü de vaat ettiği listeyi değil ana sayfayı
  açar → **D9**.
- **K4 — Performans kapısı `/tr/`'yi ölçüyor:** `frontend/scripts/lighthouse-run.mjs:9`
  (`PERF_URL ?? 'http://localhost:5080/tr/'`). Ana sayfa o adrese yerleştiğinde `0001`
  C3'ün LCP/CLS ölçümü **sessizce** listeyi değil ana sayfayı ölçmeye başlar → **D10**.

Ayrıca ölçüldü, karar gerektirmedi: liste ucu `bodyType`, `page`, `pageSize` (üst sınır 24)
okuyor ve `totalCount` döndürüyor (`ShowcaseEndpoints.cs:38-42`) — R6'nın toplamı ve R5'in
hedefi bugünkü uçla karşılanıyor. Üst çubuktaki galeri adı çeviri kataloğundan geliyor
(`app.brandName`, `TopBar.tsx:17`); bu spec onu **değiştirmez** — R1 ana sayfanın kendi
gösterimidir.

### QA CLARIFY turu ölçümü (2026-11-17, `main` @ `14a04bd`)

- **K5 — Veride marka kavramı yok.** Kodda `Brand`/`Make` tipi, kolonu veya okuma ucu yok.
  `VehicleModel` yalnız kimlik ve `CanonicalName` taşıyor, marka adın içinde
  (`'Toyota Corolla'`, `SeedInitialVehicleModels.cs:17`: *"Marka kavramı YOK (D6)"*;
  `ReferenceContracts.cs:47` aynı notu taşıyor). `0010a` X14 taksonomiyi `VehicleReference`'ın
  kendi spec'ine bıraktı. B3 "stokta marka sayan okuma yok" demişti; okumanın altındaki
  **verinin** de olmadığını söylemiyordu → **D11**.
- Karar gerektirmeyen ölçümler: geçersiz `bodyType` ve sayfa girdisi `0001` C14 ile
  normalize edilir (hata değil, `200`) · `/` 301 ve etkin olmayan önek 404,
  `LocalePrefixMiddleware`'de var · `Branch.Phone` zorunlu, varsayılan dildeki ad ve adres
  zorunlu (`Branch.cs`) — telefonsuz veya varsayılan dilde adsız şube durumu oluşmaz ·
  `Gallery.CommercialName` tek dilli bir metin (`Gallery.cs:25`) · performans seed'i 500
  ilanın her yetmişincisini öne çıkarıyor (`perf.yml:84`, `ShowcaseSeeder.cs:152`) — 8 öne
  çıkarılmış ilan, yani ana sayfa altı kartla ölçülür.

### Geçerli kurallar

**R6** (rezerve araç vitrinde rozetle görünür) · **A3** (yalnız `Yayında`/`Rezerve` görünür) ·
**F3/F4** (gizli alanlar yanıtta yok) · **P4** ("fiyat sorunuz" ≠ 0) ·
`docs/architecture.md` § 3 (locale öneki zorunlu, otomatik yönlendirme yok, her sayfa
`hreflang` yayar) · `docs/frontend.md` § 3, § 7.

### Bilinen tuzaklar

1. **Görünen ilk görseli lazy yüklemek.** Ana sayfanın en büyük görseli LCP adayıdır; `0001`
   bu tuzağa düşüp düzeltmişti (`ListingCard`'ın `aboveTheFold` propu o dersin kalıntısı).
   *(2026-11-17: "giriş görseli" yazıyordu; giriş bölümünde görsel yok — D18. Aday,
   öne çıkan bölümün ilk kartının kapağıdır — AC-27.)*
2. **Marka sayımını her istekte saymak.** *(D11 ile marka bölümü çıktı; tuzak X13'ün
   kartına devreder.)* Birkaç yüz araçta ucuzdur ama sayım sorgusu ana
   sayfanın her yüklenişinde çalışır; ölçülmeden "ucuz" denmemeli.
3. **Boş vitrinde ana sayfayı anlamsız bırakmak.** Yayında hiç araç yoksa "128 aracı gör"
   düğmesi "0 aracı gör" olur; R8'in boş durumu bunu ayrıca ele almalı.
4. **Çevrilmeyen adı yalıtmadan basmak.** Galeri adı tek dilli bir metindir; Arapça sayfada
   çevrilmez, yön yalıtımıyla korunur (R9, D20). *(2026-11-17: "marka adı" yazıyordu; marka
   bölümü D11 ile çıktı, aynı tuzak galeri adında yaşıyor.)*
5. **Altlıkta olmayan sayfaya bağlantı koymak.** `Content` yok; "Hakkımızda" bağlantısı
   404'e gider (S4).

### Verilen kararlar — CLARIFY turu, 2026-11-17

S1–S5'in beş önerisi Takım Yöneticisi tarafından **olduğu gibi onaylandı** (2026-11-17).
Önerilerin gerekçe ve bedelleri § Açık sorular'da duruyor; burada yalnız kararın kendisi ve
dokunduğu madde yazılı.

**D1 — Ana sayfa sıra #7'de, kırpılmış sürümüyle kalır (S1).** Daraltma formu yalnız
**kasa tipi** ekseniyle gelir; diğer eksenler `0015`'e kalır (**X10**). Öne çıkan araçlar
`0012c`'nin öne çıkarma eylemine dayanır (D3). Bülten yoktur (X2). *Bedel kabul edildi:*
ana sayfa iki kez elden geçer, ilk sürümdeki form mockup'tan eksik görünür. → R5, X10.

**D2 — Giriş metni çeviri kataloğundan gelir (S2).** Üç dilde sabit metindir. *Bedel kabul
edildi:* galeri metni kendisi değiştiremez, değişiklik bir dağıtım ister. `Content` (#11)
geldiğinde bir taşıma işi doğar. → R2.

**D3 — Öne çıkan araçlar: öne çıkarılmış ilanlardan en çok altısı, liste sırasıyla (S3a).**
Vitrinde görünen (C3) ve galerinin panelde öne çıkardığı ilanlar, vitrin listesiyle aynı
sırada gösterilir. *Bedel kabul edildi:* galeri altıdan fazlasını öne çıkarırsa hangi altısının
görüneceğini doğrudan seçemez. → R3.

**D4 — Öne çıkarılmış ilan yoksa bölüm görünmez; boşluk doldurulmaz (S3b).** *Bedel kabul
edildi:* öne çıkarmayı kullanmayan bir galerinin ana sayfasında "Vitrin" bandı yoktur.
→ R3. R8'in "boş vitrin" durumu (hiç yayında araç yok) bundan **ayrıdır**: vitrinde araç
olup öne çıkarılmış ilan olmaması bir hata ya da boş durum değil, bölümün yokluğudur.

**D5 — Altlık yalnız var olan hedeflere bağlantı verir (S4).** Araç listesi, dil
bağlantıları ve galeri iletişim bilgisi. Kurumsal sayfa bağlantısı yoktur. → R7.

**D6 — Galeri profilinin public okuması bu spec'e girer; logo girmez (S5).** Galeri adı,
şube adresleri ve çalışma bilgisi ziyaretçiye okunur hâle gelir; spec `Dealership`'e de
dokunur. Logo ayrı bir kart olur ve ana sayfa adı metin olarak gösterir. → R1, R7, X9.

**D7 — Marka bölümü kalır, bu sürümde yalnız gösterir (S6).** Takım Yöneticisi onayı,
2026-11-17. Stokta görünür ilanı olan markalar ve her markadaki araç sayısı listelenir;
markalar tıklanamaz. Stokta marka sayan okuma bu spec'e girer (§ Context, B3). *Bedel kabul
edildi:* mockup'ta bağlantı gibi duran liste bu sürümde bağlantı değildir; marka bağlantısı
`0015`'le birlikte ana sayfanın ikinci kez elden geçtiği üçüncü nokta olur (form, öne çıkan,
marka). → R4, X1, X11. *(2026-11-17: **D11** bu kararın yerini aldı — marka bölümü kapsam
dışında, X13.)*

### Verilen kararlar — kriter turu, 2026-11-17

**Takım Yöneticisi (ajan) kararı** (`docs/roles/README.md` § Takım Yöneticisi), Analist
bulgusu K1–K4 üzerine.

**D8 — Altlık aktif şubelerin adını, adresini ve telefonunu gösterir; çalışma bilgisi
yoktur (K1).** Yalnız `aktif` şubeler; geçici kapalı ve kapatılmış şube altlıkta yer almaz.
Şubenin adı veya adresi ziyaretçinin dilinde yoksa kurulumun **varsayılan dilindeki**
metin gösterilir (araç serbest metninin sessiz fallback'iyle aynı davranış,
`docs/architecture.md` § 3). Telefon bir `tel:` bağlantısıdır. *Gerekçe:* D6 profil
okumasını bu spec'e aldı, ama R7'nin istediği çalışma bilgisinin kaynağı hiç yazılmadı;
olmayan veriye okuma yüzeyi açılamaz. Geçici kapalı şubeyi saatsiz göstermek ziyaretçiyi
kapalı kapıya gönderir. *Elenen alternatif:* çalışma programını bu spec'e almak — `B1–B4`'ün
tamamını (çift mesai, istisna, saat dilimi) ve bir panel akışını getirir; spec'i ikiye
büyütür. *Bedel:* altlık "açık mı" sorusunu cevaplamaz. → R7, X12.

**D9 — Liste `/{locale}/cars`'a taşınır; eski bağlantılar ve "Vitrine dön" listeye gider
(K2, K3).** `/{locale}/` liste parametresi (`bodyType` veya `page`) taşıyorsa ziyaretçi
**aynı parametrelerle** listeye geçer ve tarayıcı geçmişine ara bir kayıt eklenmez.
Üç "Vitrine dön" bağlantısı `/{locale}/cars`'a gider. *Gerekçe:* paylaşılmış daraltılmış
liste bağlantısını ana sayfaya düşürmek ziyaretçinin seçimini sessizce kaybeder; "Vitrine
dön" metni listeyi vaat ediyor. *Bedel:* ana sayfa `bodyType` ve `page` parametrelerini
kendi işine kullanamaz. → R11. *(2026-11-17: tetikleyen parametreler ve taşınan sorgu
**D17** ile netleşti.)*

**D10 — Performans kapısı iki adresi ölçer (K4).** Ana sayfa `0001` C3'ün LCP ve CLS
eşiklerini aynı protokolle karşılar; listenin ölçümü `/{locale}/cars`'a taşınır.
*Gerekçe:* yalnız adresi değiştirmek listeyi, yalnız bırakmak ana sayfayı ölçümsüz bırakır.
*Bedel:* Lighthouse koşusu ikiye çıkar (ADR `0010` dakika bütçesi; iş yalnız kod
değiştiğinde koşuyor). → R12.

### Verilen kararlar — QA CLARIFY turu, 2026-11-17

**Takım Yöneticisi (depo sahibi) kararı**, QA bulgusu B1–B10 üzerine (`docs/roles/qa.md`
§ 6: test edilemez veya eksik yazılmış kriter). B1'de ana öneri, B2–B10'da öneriler
**olduğu gibi** onaylandı.

**D11 — Marka bölümü bu sürümden çıkar (B1).** R4 ve AC-29–AC-36 kapsam dışına alınır
(**X13**); D7'nin yerini alır. *Gerekçe:* veride marka kavramı yok (K5). Taksonomiyi bu spec'e
almak `VehicleReference`'a varlık, migration, çeviri ve model→marka bağı getirir ve hangi
modellerin besleneceği (`0010a` D16) verilmemiş bir ürün kararıdır; markayı kanonik adın ilk
kelimesinden türetmek "Land Rover", "Alfa Romeo" gibi adlarda bozulur ve referans veriyi
serbest metne düşürür (`0010a` tuzak 7). S6 bu seçeneği "spec küçültülmek istenirse geçerli
ikinci seçenek" olarak kaydetmişti. *Elenen alternatifler:* (a) taksonomiyi bu spec'e almak;
(b) marka yerine model sayımı ("Toyota Corolla 3") — Intent'i karşılar ama marka gelince
yeniden yazılacak bir bölüm üretir. *Bedel:* Intent'in başarı ölçütü öne çıkan ilanlara ve
toplam sayıya yaslanır; mockup'ın marka listesi ilk sürümde yoktur. → R4, X13.

**D12 — Formun kendi hata durumu var; boş vitrin toplam sayıdan anlaşılır (B2).** Boş
vitrin kararı (AC-50) **görünür ilanların toplam sayısından** verilir: toplam `0` ise boş
durum. Toplam sayı veya kasa tipi okuması başarısızsa form kendi hata durumunu ve tekrar
dene eylemini gösterir; diğer bölümler kendi verisine göre görünür ve boş durum gösterilmez.
*Gerekçe:* diğer bölümlerin kısmi hata davranışıyla (AC-52–AC-55) aynı; karar kaynağı
yazılmazsa her uygulama farklı bir okumaya bakar ve test yanlış şeyi ölçer. → R8, AC-50,
AC-52, AC-53, AC-62–AC-64.

**D13 — Aktif şube yoksa şube bloğu görünmez; şubeler ada göre sıralıdır (B3).** Aktif
şube yoksa altlığın şube bloğu **hiç görünmez** (boş durum veya hata yok), altlığın
bağlantıları kalır. Şubeler **varsayılan dildeki adın ordinal** sırasıyla dizilir.
*Gerekçe:* D4'ün "yokluk boş durum değildir" mantığı; ordinal sıra marka kriterindeki
(eski AC-32) ilkeyle aynı ve deterministik. → R7, AC-65, AC-66.

**D14 — Altlığın dil bağlantıları etkin locale'lerden türetilir (B4).** Sabit üç dil
yazılmaz. *Gerekçe:* iki dille kurulmuş bir galeride sabit üçüncü bağlantı 404'e gider ve
D5'i deler. → R7, AC-46, AC-67.

**D15 — Galerinin ziyaretçiye dönük okuması bir izin listesidir (B5).** Yanıt yalnız
galerinin adını ve aktif şubelerin adını, adresini ve telefonunu taşır. *Gerekçe:* yasak
listesi sonradan eklenen bir alanı yakalamaz; izin listesi yakalar (C2). `Gallery` sosyal
bağlantı ve logo kimliği, `Branch` e-posta, WhatsApp, koordinat, slug ve durum da taşıyor;
altlık bunların hiçbirini göstermiyor. → C2, AC-14.

**D16 — Şube adının varsayılan dile düşüşü de test edilir (B6).** D8 ad ve adres için
fallback'i karara bağladı; kriter yalnız adresi taşıyordu. → AC-68.

**D17 — Eski liste adresinin yönlendirmesini listenin okuduğu üç parametre tetikler; sorgu
olduğu gibi taşınır (B7).** `/{locale}/` sorgusunda `bodyType`, `page` veya `pageSize`
varsa yönlendirme olur ve hedef adres sorgunun **tamamını değiştirmeden** taşır; bu üçünden
hiçbiri yoksa (`?utm_source=…` gibi yalnız başka parametreler) ana sayfa görünür,
yönlendirme olmaz. *Gerekçe:* liste ucu bu üçünü okuyor (§ Context); normalizasyon `0001`
C14'ün işi, burada yinelenmez. → D9, AC-8, AC-69, AC-70.

**D18 — Giriş bölümünde bu sürümde görsel yoktur (B8).** *Gerekçe:* C9 ve tuzak 1 bir
"giriş görseli" varsayıyordu ama R2 bölümü başlık + açıklama olarak tanımlıyor ve görselin
kaynağı yok (`Content` yok, logo X9). → R2, C9, AC-71.

**D19 — Formun kasa tipleri liste ekranının filtre çubuğuyla aynıdır (B9).** Aynı kasa
tipleri, aynı sıra, etkin locale'in adı. *Gerekçe:* formdan gidilen liste, formun
gösterdiğinden farklı seçenekler göstermemeli. → AC-37.

**D20 — Galeri adı Arapça sayfada yön yalıtımıyla gösterilir (B10).** *Gerekçe:*
`CommercialName` tek dilli bir metin; Latin harfli bir ad RTL satırda eski marka kuralının
(AC-33) önlediği bozulmayı üretir. → R9, AC-72.

### Verilen kararlar — plan turu, 2026-11-17

**Takım Yöneticisi (depo sahibi) kararı**, Developer'ın plan bulguları P2–P9 üzerine.
Sekiz öneri **olduğu gibi** onaylandı. (P1, `#83`'ün merge'iydi ve `5ddeeb9` ile kapandı.)

**D21 — Eski liste adresini sunucu yönlendirir: `301` (P2).** `LocalePrefixMiddleware`,
`/{locale}/` sorgusunda `bodyType`, `page` veya `pageSize` görürse `/{locale}/cars`'a
**301** ile yönlendirir; sorgu dizgesi bayt bayt taşınır (D17). *Gerekçe:* ana sayfa hiç
yüklenmez — fazladan istek ve LCP etkisi yok; HTTP yönlendirmesi tarayıcı geçmişine kayıt
eklemediği için AC-7 kendiliğinden sağlanır; arama motoru eski adresi yenisiyle birleştirir.
*Bedel:* tarayıcılar 301'i kalıcı önbelleğe alır; D9 kalıcı bir karar olduğu için kabul
edildi. *Elenen alternatif:* istemcide `history.replaceState` — ana sayfa bir an görünür ve
gereksiz okuma yapar. → D9, AC-6, AC-7, AC-69, AC-70.

**D22 — Uç etkin locale'leri sunucunun bastığı `<meta name="enabled-locales">`
etiketinden okur (P3).** `IndexHtmlRenderer`, yapılandırmadaki etkin locale'leri
`</head>`'den önce boşlukla ayrılmış liste olarak basar; *(ilk yazımda `<html
data-locales>` özniteliğiydi; `<html>` etiketini birebir ölçen tema testlerini
(`TemaEnjeksiyonuTests`) bozmamak için meta etikete taşındı — amaç aynı)*; uç altlığın dil bağlantılarını bu listeden türetir (D14). *Gerekçe:* uç bugün
locale listesini koda sabit üç dil olarak taşıyor (`SUPPORTED_LOCALES`); tema özniteliği
de sunucudan basılıyor ve fazladan bir istek gerekmiyor. *Not:* üst çubuktaki dil
değiştiricinin (`LocaleSwitcher.tsx`) aynı kusuru taşıdığı ölçüldü — iki dilli kurulumda da
`/ar/` bağlantısı basıyor. **Bu spec'in kapsamında değildir** ve ayrı bir kart olarak
önerilir. → AC-67.

**D23 — İzlenen yollar: üst çubuk dokunulmaz (P3'ün kapsam yarısı).** D22'deki kusur bu
spec'te düzeltilmez; `LocaleSwitcher` olduğu gibi kalır. → X14.

**D24 — Galeri adı `<bdi>` ile yalıtılır; yön içerikten çözülür (P4).** D20 "soldan sağa"
diyordu; ama `CommercialName` Arapça harfli olabilir (KWD kurulumları) ve zorunlu `ltr`
o adı ters dizer. `<bdi>` Latin adı soldan sağa, Arapça adı sağdan sola gösterir; ikisinde
de yanındaki metinden yalıtılır. *Bedel:* yön içeriğin ilk güçlü karakterine bağlıdır;
rakamla veya noktalamayla başlayan bir ad o karaktere göre çözülür. → R9, D20, AC-72, AC-73.

**D25 — Kasa tipi alanı filtrelenemez ise form ekseni göstermez; düğme kalır (P5).** Liste
ekranı kasa tipi alanı `isFilterable` değilken filtre çubuğunu hiç göstermiyor (`0001`
AC-12); form aynı kapıdan geçer ve o durumda yalnız "N aracı gör" düğmesini taşır. Kasa
tipi **okuması başarısızsa** form, listenin sessiz gizlemesinden farklı olarak kendi hata
durumunu gösterir (AC-63); listenin davranışı değişmez. → D19, AC-74.

**D26 — Ana sayfa `app/` katmanında birleşir (P6).** Öne çıkanlar ve form
`features/catalog/`'da, galeri adı ve şube bloğu yeni `features/dealership/` klasöründe
yaşar; `app/HomePage.tsx` onları birleştirir (talep formunun `renderLead` deseni). *Gerekçe:*
bir feature başka feature'ın iç dosyasını import etmez (`docs/frontend.md` § 1); yeni ortak
bileşen gerekmez (C8).

**D27 — Yeni uçlar: `/api/catalog/featured-listings` ve `/api/dealership/gallery`; toplam
sayı mevcut liste ucundan (P7, P8).** Öne çıkan okuma `/showcase-listings/featured` olarak
açılmaz: ilan detayının `{slug}` rotasıyla aynı yolu paylaşırdı. Toplam sayı için yeni uç
açılmaz; liste ucu `pageSize=1` ile çağrılır ve `totalCount` okunur (§ Context).

**D28 — Tanınmayan alt yol (`/tr/foo`) listeyi göstermeye devam eder (P9).** `0001`'den
gelen davranış; değiştirmek için kriter yok.

### Uygulama planı (onaylı, 2026-11-17)

Dal `feature/0014-ana-sayfa`; tek PR, adım başına commit (`[plan 0014/<n>]`). Migration yok.

1. `Dealership` — galerinin public okuması (D15, D27): sözleşme, sorgu, uç. AC-12–15,
   42–44, 48, 66, 68.
2. `VehicleCatalog` — öne çıkan okuması; liste sıralaması tek yere çıkarılır
   (`ShowcaseOrdering`) ki iki yüzey aynı sırayı paylaşsın (AC-19). AC-17–25.
3. `Host` — eski adres yönlendirmesi (D21) ve `enabled-locales` meta etiketi (D22). AC-2–4, 6, 8, 69, 70.
4. Uç rotaları — `home` rotası, `/{locale}/cars`, üç "Vitrine dön" (D9). AC-5, 9–11.
5. Uç — API çağrıları, ana sayfa bölümleri, altlık, durumlar, çeviri anahtarları.
   AC-1, 12, 13, 16–28, 37–47, 49–56, 62–68, 71–74.
6. Performans kapısı iki adresi ölçer (D10). AC-59–61.
7. e2e ve görsel kanıt; `docs/architecture.md` § Karar Defteri (D9). AC-1, 6, 7, 26, 38,
   39, 57, 72, 73.

---

## Açık sorular

> **Tüm sorular karara bağlandı** (2026-11-17, öneriler olduğu gibi onaylandı:
> S1–S5 → **D1–D6**, S6 → **D7**; QA CLARIFY turunun B1–B10'u → **D11–D20**, § Context). Metinleri gerekçe ve bedel kaydı olarak
> aşağıda duruyor. **Açık soru yok.**

**S6. Marka bölümü (R4) bu sürümde ne yapar? — D1 ile çelişki.**
*(2026-11-17, kararlar işlenirken çıktı.)* R4 iki şey istiyor: stoktaki markaları sayılarıyla
**göstermek** ve markaya tıklayınca **o markaya daraltılmış listeye** gitmek. D1 ise marka
eksenini `0015`'e bıraktı (X10). Vitrin listesi bugün yalnız kasa tipine göre daraltılabiliyor
(`0001` X3); `0015` R1 marka eksenini getirecek ama taslak hâlinde. Yani R4'ün ikinci yarısı
**bugün gidecek bir hedef bulamıyor.** Ayrıca birinci yarısı için de stokta marka sayan bir
okuma yok (§ Context, B3), yani marka bölümü hangi seçenekte kalırsa kalsın bu spec'e bir okuma
yüzeyi ekliyor.

*Öneri:* marka bölümü **kalır, ama yalnız gösterir**: stoktaki markalar ve her markadaki
araç sayısı listelenir; bu sürümde **tıklanamaz**. Marka ekseni `0015` ile geldiğinde
bağlantı olma işi ya `0015`'in kapsamına ya da ayrı bir karta yazılır. X1'in "marka listesi
daraltılmış listeye gider" gerekçesi buna göre düzeltilir.
*Gerekçe:* Intent'in başarı ölçütü *"ziyaretçi ana sayfadan ayrılmadan stoğun ne içerdiğini
anlar"* — bunu doğrudan karşılayan bölüm marka listesidir; listeyi çıkarmak ölçütü zayıflatır.
Tıklanabilirlik ise gezinme kolaylığıdır, ölçütün kendisi değil. D1'in mantığıyla da aynı:
form tek eksenle gelip sonra büyüyor, marka listesi de önce gösterip sonra bağlanıyor.
*Bedel:* mockup'ta bağlantı gibi duran bir liste bu sürümde bağlantı değildir; ziyaretçi
tıklamayı deneyebilir. Ana sayfa üçüncü bir noktada daha (form, öne çıkan, marka) ikinci kez
elden geçer. Stokta marka sayan okuma bu spec'e girer.
*Elenen alternatifler:* (a) **marka bölümünü bu sürümden çıkarmak** (X'e yazmak) — spec en çok
bununla küçülür ve marka okuması `0015`'e kalır; ama Intent'in başarı ölçütü yalnız öne çıkan
altı ilana ve toplam sayıya yaslanır. Takım Yöneticisi spec'i küçültmeyi öncelerse geçerli
ikinci seçenek budur. (b) **Marka eksenini `0015`'ten bu spec'e çekmek** — mockup'a tam uyar
ama D1'i deler ve `0015`'in R1'ini iki spec'e böler. (c) **Markaya tıklamak süzülmemiş listeye
gider** — elendi: tıklamanın sonucu tıklanan şeyle ilgisiz olur.

---

**S1. Bu spec sıradaki yerinde kalabilir mi? Üç bağımlılığı kendisinden sonra.**
ADR 0003 Ek 1 ana sayfayı **#7**'ye koydu. Ama mockup'ın ana sayfası:
- **daraltma formu** → marka/yıl/fiyat eksenleri **#9**'da (`0001` X2/X3 bunları kapsam dışı
  bırakmıştı),
- **öne çıkan araçlar** → `Content` slotu **#11**'de,
- **bülten** → **#13**'te.

*Öneri:* ana sayfa **#7'de kalsın, ama kırpılmış sürümüyle**: daraltma formu yalnızca
**kasa tipi** ekseniyle (tek var olan eksen, `0001`'den), öne çıkan araçlar **`0012c`'nin
öne çıkarma eylemiyle** (panelde zaten var; bkz. S3), bülten **yok** (X2).
*Gerekçe:* ziyaretçi tarafında bir giriş sayfasının olması, eksiksiz olmasından önce gelir;
üç bağımlılığı beklemek ana sayfayı #13 sonrasına atar. Kırpılmış sürüm bugünkü veriyle
çalışır ve eksenler #9'da genişlediğinde form kendiliğinden büyür.
*Bedel:* ana sayfa iki kez elden geçer; mockup'ın dört eksenli formu ilk sürümde tek eksenli
olur ve mockup'ı gören biri eksik bulur.
*Elenen alternatif:* ana sayfayı #11 sonrasına taşımak — teknik olarak daha temiz ama vitrin
girişsiz kalmaya devam eder ve bu ziyaretçi tarafındaki en görünür eksiktir.

**S2. Giriş bölümünün metni nereden gelir?**
Mockup'ta somut bir iddia var: *"Geçmişi gizlenmeyen araçlar."* ve altında bir paragraf.
*Öneri:* ilk sürümde **çeviri kataloğundan** gelir (üç dilde sabit metin), `Content`'ten değil.
*Gerekçe:* `Content` modülü yok (#11) ve bu metin kurulum başına bir kez yazılan bir şeydir.
*Bedel:* galeri metnini kendi değiştiremez; değişiklik bir dağıtım ister. `Content` geldiğinde
bu bölüm oraya taşınabilir — o zaman bir taşıma işi doğar.

**S3. "Öne çıkan araçlar" nasıl seçilir — ve yeterince öne çıkarılmış ilan yoksa ne olur?**
*(2026-11-17'de yeniden yazıldı — B1.)* İlk sürümdeki öneri "sıralama ağırlığı en yüksek altı
ilan"dı ve bedeli "galeri elle seçemez"di. `0012c`'den sonra bu bedel **geçersiz**: galeri
panelde ilanı açıkça öne çıkarıyor. Soru artık iki parçalı.

*S3a — seçim.*
*Öneri:* ana sayfanın öne çıkan bölümü **öne çıkarılmış** ve vitrinde görünür (C3) ilanları
gösterir; sıra vitrin listesininkiyle aynıdır (en yeni yayınlanan önce); **en çok altı** ilan.
*Gerekçe:* `0012c` öne çıkarmayı galerinin bilinçli kararı olarak kurdu; ana sayfa o kararı
olduğu gibi göstermeli. Yeni bir kavram ("öne çıkan slot") eklemek `Content`'in işini bu
spec'e çeker. Listeyle aynı sıra, iki yüzeyin birbirini yalanlamamasını sağlar.
*Bedel:* galeri altıdan fazla ilanı öne çıkarırsa hangilerinin ana sayfada duracağını
**doğrudan** seçemez; en yeni altısı görünür. `Content` slotu geldiğinde (#11) bu ölçüt
yeniden açılabilir.

*S3b — öne çıkarılmış ilan altıdan az veya hiç yoksa.*
*Öneri:* bölüm **yalnız öne çıkarılmış ilanları** gösterir — altıdan azsa o kadarını; **hiç
yoksa bölüm görünmez.** Boşluk normal ilanlarla doldurulmaz.
*Gerekçe:* "öne çıkan" etiketi altında öne çıkarılmamış bir ilan göstermek, galerinin
vermediği bir kararı ona mal eder. Boş bölüm ise R8'in "anlamsız bölüm" tuzağıdır
(§ Bilinen tuzaklar 3).
*Bedel:* öne çıkarmayı hiç kullanmayan bir galerinin ana sayfasında bu bölüm hiç görünmez;
mockup'taki altı kartlık "Vitrin" bandı o kurulumda yoktur.
*Elenen alternatif:* eksik yerleri en yeni yayınlanan normal ilanlarla doldurmak — ana sayfa
her zaman dolu görünür ama etiket yalan söyler.

**S4. Altlıktaki kurumsal bağlantılar ne yapar?**
`Content` yok; "Hakkımızda", "KVKK", "Çerez politikası" sayfaları mevcut değil.
*Öneri:* ilk sürümde altlık **yalnızca var olan hedeflere** bağlantı verir: araç listesi,
dil bağlantıları, galeri iletişim bilgisi (profilden). Olmayan sayfaların bağlantısı
**konulmaz**.
*Gerekçe:* 404'e giden bağlantı, olmayan bir bağlantıdan kötüdür — hem ziyaretçi hem arama
motoru için.
*Bedel:* altlık mockup'takinden ince olur.
*KVKK uyarısının cevabı (2026-11-17 — B4):* taslak, `0005`'in talep formunun KVKK aydınlatma
metnine bağlantı verdiğini ve hedefinin doğrulanması gerektiğini söylüyordu. Doğrulandı:
formun onay alanı (`frontend/src/features/leads/LeadFields.tsx`) **bağlantı taşımıyor**,
yalnız onay metni var. 404'e giden bir bağlantı **yok**; bu yüzden DoD'daki kontrol maddesi
kaldırıldı. Aydınlatma metninin bir sayfası olmaması ayrı bir konudur ve bu spec'in kapsamında
değildir (X6).

**S5. Galeri profilinin public okuması bu spec'e mi girer? (B2)**
R1 (ad ve logo) ve R7'nin profil kısmı (adres, çalışma bilgisi) ziyaretçinin galeri
profilini okuyabilmesine dayanıyor. Bugün böyle bir okuma yok, logo da hiç atanamıyor
(§ Context).
*Öneri:* **ad, şube adresleri ve çalışma bilgisinin public okuması bu spec'e girer; logo
girmez.** Logo yoksa ana sayfa galeri adını metin olarak gösterir. Logo yükleme ve
gösterme ayrı bir kart olur.
*Gerekçe:* adı söylemeyen bir giriş sayfası Intent'in ilk cümlesini ("galerinin kim
olduğunu söyleyen bir sayfa") karşılamaz; ad ve adres `0003`'te zaten kayıtlı, eksik olan
yalnız ziyaretçinin onu okuyabilmesi. Logo ise iki eksik taşıyor (atama akışı ve gösterim)
ve bunlardan biri panel tarafında; onu çekmek spec'i iki yüzeye büyütür.
*Bedel:* spec `Dealership`'e de dokunur (Modül(ler) satırı buna göre güncellendi). Mockup'ın
logolu başlığı ilk sürümde yoktur; R1'in "logo" yarısı bu karar onaylanırsa kapsam dışına
(yeni bir X) yazılır.
*Elenen alternatifler:* (a) profili tamamen kapsam dışı bırakmak — spec küçülür ama ana
sayfa galerinin adını gösteremez ve R1 boşa düşer; (b) logoyu da almak — mockup'a tam uyar
ama bir panel akışını ana sayfa spec'ine çeker.

---

## Acceptance Criteria

> Her kriterin testi `DisplayName`'inde `0014 AC-<n>` taşır (`docs/testing.md` § 1).
> **Görünür ilan** = vitrinde görünen ilan: `Yayında` veya `Rezerve` (C3). Örnek veri
> kümeleri kriterin içinde verilir; sayılar ayırt edici seçildi.

### Adres (R10, R11 · D9)

- **AC-1.** `/tr/`, `/ar/` ve `/en/` her biri o dilde **ana sayfayı** gösterir.
- **AC-2.** `/` istenince varsayılan locale'in ana sayfasına **301** ile yönlenir.
- **AC-3.** Etkin olmayan bir önek (`/xx/`) **404** verir.
- **AC-4.** İstek `Accept-Language: ar` taşısa da `/` **varsayılan** locale'e yönlenir (C10).
- **AC-5.** `/{locale}/cars` araç listesini gösterir; `bodyType` ve `page` parametreleri
  `0001`'deki anlamıyla çalışır.
- **AC-6.** `/tr/?bodyType=suv&page=2` açıldığında ziyaretçi `/tr/cars?bodyType=suv&page=2`
  adresindeki listeyi görür.
- **AC-7.** AC-6'daki geçişten sonra tarayıcıda **geri** demek ziyaretçiyi
  `/tr/?bodyType=suv&page=2`'ye değil, ondan önceki sayfaya götürür.
- **AC-8.** `/tr/` liste parametresi (`bodyType`, `page`, `pageSize`) taşımıyorsa
  yönlendirme olmaz; ana sayfa görünür. `/tr/?utm_source=x` de ana sayfayı gösterir (D17).
- **AC-69.** `/tr/?pageSize=12` açıldığında ziyaretçi `/tr/cars?pageSize=12` adresine
  yönlenir (D17).
- **AC-70.** `/tr/?bodyType=suv&utm_source=x` için yönlendirmenin hedefi
  `/tr/cars?bodyType=suv&utm_source=x`'tir: sorgu değiştirilmeden taşınır; sonraki
  normalizasyon `0001` C14'ün işidir (D17).
- **AC-9.** Bulunamayan ilan ekranının "Vitrine dön" bağlantısı `/{locale}/cars`'a gider.
- **AC-10.** Talep başarı ekranının "Vitrine dön" bağlantısı `/{locale}/cars`'a gider.
- **AC-11.** İlanı yayından kalkmış talep hatasının "Vitrine dön" bağlantısı
  `/{locale}/cars`'a gider.

### Galeri adı (R1 · D6)

- **AC-12.** Ana sayfa galeri profilindeki **ticari unvanı** gösterir: profil `"Örnek
  Otomotiv"` iken sayfada bu ad görünür.
- **AC-13.** Profilin ticari unvanı `"Başka Ad"` yapıldığında ana sayfa bir sonraki
  yüklemede yeni adı gösterir — ad çeviri kataloğundan gelmez.
- **AC-14.** Galerinin ziyaretçiye dönük okuma yanıtı **yalnız** galerinin adını ve aktif
  şubelerin adını, adresini ve telefonunu taşır. Vergi numarası, ticaret sicil numarası,
  sosyal bağlantılar, logo kimliği ve şubenin e-postası, WhatsApp'ı, koordinatı, slug'ı ve
  durumu yanıtta **bulunmaz** (null olarak da gelmez). *(2026-11-17, D15: yalnız iki yasak
  alanı sayıyordu.)*
- **AC-15.** Galeri okuması oturum çerezi olmadan **200** döner (C1).

### Giriş bölümü (R2 · D2)

- **AC-16.** Giriş başlığı ve açıklaması etkin locale'in kataloğundan gelir; `tr`, `ar`,
  `en` kataloglarının üçünde de iki anahtar dolu.
- **AC-71.** Giriş bölümü bir `<img>` öğesi veya arka plan görseli taşımaz (D18).

### Öne çıkan araçlar (R3 · D3, D4)

- **AC-17.** 8 öne çıkarılmış görünür ilan varken bölüm **tam altı** kart gösterir.
- **AC-18.** AC-17'deki altı kart, öne çıkarılmış ilanlar arasında **yayın tarihi en
  yeni** altı ilandır ve yeniden eskiye sıralıdır.
- **AC-19.** Yayın tarihi eşit iki öne çıkarılmış ilandan ilan kimliği küçük olan önce gelir
  (vitrin listesiyle aynı sıra).
- **AC-20.** 3 öne çıkarılmış ve 10 öne çıkarılmamış görünür ilan varken bölüm **yalnız
  üç** kart gösterir; öne çıkarılmamış hiçbir ilan bölümde yer almaz.
- **AC-21.** Hiç öne çıkarılmış görünür ilan yokken (10 öne çıkarılmamış görünür ilan var)
  bölüm başlığıyla birlikte **hiç görünmez**; yerinde boş durum veya hata durumu da yoktur.
- **AC-22.** Öne çıkarılmış bir `Rezerve` ilan bölümde **Rezerve rozetiyle** görünür.
- **AC-23.** Öne çıkarılmış `Taslak`, `Satıldı` ve `Arşiv` ilanların hiçbiri bölümde
  görünmez.
- **AC-24.** "Fiyat sorunuz" ilanın kartı fiyat yerine "fiyat sorunuz" metnini gösterir;
  `0` göstermez.
- **AC-25.** Para birimi KWD olan kurulumda `12345.500` tutarlı ilanın kartı tutarı **üç
  ondalıkla** gösterir.
- **AC-26.** Karta tıklamak o ilanın detayına (`/{locale}/cars/{slug}`) gider.
- **AC-27.** İlk kartın kapak görseli `loading="lazy"` **taşımaz**; diğer kartlarınki taşır.
- **AC-28.** Her kartın kapak görseli açık `width` ve `height` taşır.

### Markalar — kapsam dışı (D11 → X13)

> **AC-29–AC-36 kaldırıldı** (2026-11-17, **D11**): veride marka kavramı yok (K5). Numaralar
> yeniden kullanılmaz. Kriterlerin metni bu dosyanın `14a04bd` hâlinde duruyor ve X13'ün
> kartına devreder.

### Daraltma formu (R5, R6 · D1)

- **AC-74.** Kasa tipi alanı filtrelenemez (`isFilterable: false`) iken form kasa tipi
  seçeneği göstermez; "N aracı gör" düğmesi görünür ve parametresiz `/{locale}/cars`'a
  gider (D25).
- **AC-37.** Form, liste ekranının kasa tipi filtre çubuğuyla **aynı kasa tiplerini, aynı
  sırada**, etkin locale'in adıyla listeler. *(2026-11-17, D19: "referans veriden"
  yazıyordu; hangi alt kümenin listeleneceğini söylemiyordu.)*
- **AC-38.** `sedan` ve `suv` seçilip gönderilince ziyaretçi
  `/{locale}/cars?bodyType=sedan&bodyType=suv` adresine gider (`0001` C13'ün kanonik biçimi).
- **AC-39.** Hiçbir şey seçilmeden gönderilince ziyaretçi parametresiz `/{locale}/cars`'a
  gider.
- **AC-40.** Gönderme düğmesi görünür ilanların **toplam** sayısını ICU ile gösterir: 128
  görünür ilanla Türkçe sayfada "128 aracı gör".
- **AC-41.** Düğmedeki sayı kasa tipi seçimiyle **değişmez**.

### Altlık (R7 · D5, D6, D8)

- **AC-42.** İki `aktif` şube varken altlık her ikisinin adını, adresini ve telefonunu
  gösterir.
- **AC-43.** `geçici kapalı` ve `kapatılmış` şubeler altlıkta **yer almaz**.
- **AC-44.** Arapça adresi girilmemiş bir şube, Arapça sayfada adresini varsayılan dildeki
  metniyle gösterir; satır boş kalmaz.
- **AC-68.** Arapça adı girilmemiş bir şube, Arapça sayfada adını varsayılan dildeki
  metniyle gösterir; satır boş kalmaz (D8, D16).
- **AC-45.** Şube telefonu E.164 biçimindeki numaraya giden bir `tel:` bağlantısıdır.
- **AC-46.** Altlığın bağlantıları **yalnız** şunlardır: `/{locale}/cars`, **etkin
  locale'lerin** ana sayfaları ve şube telefonları. Başka bağlantı yoktur. *(2026-11-17,
  D14: "üç dilin ana sayfası (`/tr/`, `/ar/`, `/en/`)" yazıyordu.)*
- **AC-67.** Kurulumda yalnız `tr` ve `en` etkinken altlıkta `/ar/` bağlantısı **yoktur**
  (D14).
- **AC-65.** Hiç `aktif` şube yokken altlığın şube bloğu **görünmez**; yerinde boş durum veya
  hata durumu yoktur ve altlığın bağlantıları görünmeye devam eder (D13).
- **AC-66.** Şubeler varsayılan dildeki adın **ordinal** sırasıyla dizilir: önce
  `"Bakırköy"`, sonra `"Ataşehir"` adlı şube kaydedilmişken altlıkta `"Ataşehir"` önce gelir
  (D13).
- **AC-47.** Etkin dilin bağlantısı `aria-current` ile işaretlidir.
- **AC-48.** Ziyaretçiye dönük okuma yanıtı `geçici kapalı` ve `kapatılmış` şubeleri
  **hiç taşımaz**; süzme sunucuda yapılır, uçta değil (C2'nin "gizleme sorgu tarafında"
  ilkesi).

### Durumlar (R8)

- **AC-49.** Veri gelene kadar öne çıkanlar, form ve altlığın şube bloğu kendi
  iskeletini gösterir. *(2026-11-17: "markalar" D11 ile çıktı.)*
- **AC-50.** Görünür ilanların toplam sayısı `0` iken öne çıkanlar ve form yerine **tek**
  bir boş durum görünür ve "0 aracı gör" hiçbir yerde yazmaz. Karar **toplam sayıdan**
  verilir (D12). *(2026-11-17: "markalar" D11 ile çıktı; karar kaynağı D12 ile yazıldı.)*
- **AC-62.** Toplam 1 görünür ilan varken ve hiç öne çıkarılmış ilan yokken boş durum
  **görünmez**; form "1 aracı gör" ile görünür, öne çıkan bölümü görünmez (D4, D12).
- **AC-51.** AC-50'nin durumunda giriş bölümü ve altlık görünmeye devam eder.
- **AC-52.** Toplam sayı okuması başarısız, öne çıkan okuması başarılıyken öne çıkanlar
  görünür; form kendi hata durumunu ve **tekrar dene** eylemini gösterir ve AC-50'nin boş
  durumu **gösterilmez** (D12). *(2026-11-17: marka okuması üzerine yazılıydı; D11.)*
- **AC-63.** Kasa tipi okuması başarısız, toplam sayı ve öne çıkan okuması başarılıyken
  öne çıkanlar görünür; form kendi hata durumunu ve tekrar dene eylemini gösterir (D12).
- **AC-53.** Öne çıkan okuması başarısız, formun okumaları başarılıyken form görünür; öne
  çıkanlar bölümü kendi hata durumunu ve tekrar dene eylemini gösterir. *(2026-11-17:
  "marka okuması / markalar" yazıyordu; D11.)*
- **AC-64.** Formun tekrar dene eylemi toplam sayıyı ve kasa tiplerini yeniden ister, öne
  çıkan ve galeri okumalarını istemez (AC-55'in iki okumalı bölüm için hâli, D12).
- **AC-54.** Galeri okuması başarısızken sayfanın geri kalanı görünür; adın ve şube
  bloğunun yeri boş kalır ve hata metni basılmaz.
- **AC-55.** Bir bölümün tekrar dene eylemi **yalnız o bölümün** verisini yeniden ister.
- **AC-56.** Sunucu `500` döndüğünde hiçbir bölüm teknik ayrıntı (durum kodu, iz kimliği,
  istisna metni) göstermez (`docs/frontend.md` § 3.1).

### Dil ve yön (R9)

- **AC-57.** `/ar/` sayfası `<html dir="rtl" lang="ar">` ile gelir ve bölümlerin sırası
  ile hizası aynalanmış görünür (görsel kanıt, Arapça).
- **AC-72.** Arapça sayfada galeri adı (`"Örnek Otomotiv"`) çevrilmez; `<bdi>` ile
  yalıtılmış olarak soldan sağa görünür ve yanındaki metinle yer değiştirmez (görsel kanıt,
  D20, D24). *(2026-11-17, D24: "soldan sağa yönlü yalıtılmış" yazıyordu; yön artık
  içerikten çözülüyor.)*
- **AC-73.** Galeri adı Arapça harfli (`"معرض المثال"`) iken ad `<bdi>` içinde sağdan sola
  görünür; zorunlu `dir="ltr"` taşımaz (D24).
- **AC-58.** Ana sayfanın stillerinde fiziksel yön özelliği (`left`, `right`,
  `margin-left`, `text-align: left` …) yoktur; stil kapısı yeşildir (C6).

### Performans (R12 · D10)

- **AC-59.** `/tr/` (ana sayfa) `0001` C3 protokolüyle (Lighthouse mobil, 3 koşu, medyan)
  **LCP < 2,5 s** ölçülür.
- **AC-60.** `/tr/` aynı protokolle **CLS < 0,1** ölçülür.
- **AC-61.** Liste ölçümü `/tr/cars` adresinde koşar ve `0001`'in LCP ve CLS eşiklerini
  karşılamaya devam eder.

---

## Definition of Done

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
      *(S1–S6 → D1–D7, 2026-11-17.)*
- [x] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
      *(#85: 66 kriterin 66'sı; AC-29–AC-36 D11 ile kaldırıldı. Karşılanmayan kriter yok.)*
- [x] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0014-ac<n>.png`), boş ve hata durumları dâhil,
      Arapça görüntü de var. *(13 görsel: boş `ac50`, iskelet `ac49`, kısmi hata `ac52`/`ac53`/`ac54`,
      Arapça `ac57`/`ac72`/`ac73`/`ac47`. Görseller tek tek açılıp okundu.)*
- [x] <!-- DoD:T4 --> Mimari testler yeşil — başka modülün tablosuna erişim yok (C11). *(55/55; öne çıkan okuması `VehicleCatalog`'un kendi okuma modelinden, galeri okuması `Dealership`'in kendi tablolarından.)*
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil. *(20 `home.*` anahtarı; `check:locales` → 3 katalog, 468 anahtar, eşit.)*
- [x] <!-- DoD:T7 --> Yeni domain terimi yok. *(`docs/domain.md` `0254da6..90b4e98` arasında değişmedi; "öne çıkarma" `0012c`'den.)*
- [x] Altlıktaki her bağlantının **var olan** bir hedefe gittiği doğrulandı (S4). *(AC-46 testi altlığın bağlantı kümesini birebir ölçüyor: `tel:` × şube, `/{locale}/cars`, etkin dillerin ana sayfaları; AC-67 iki dilli kurulumda `/ar/`'ın yokluğunu. Boş vitrin eyleminin `#contact` hedefi altlığın kendisi.)*
- [x] Ham piksel/hex eklenmedi; token katmanı kullanıldı (C7). *(Bileşen stillerinde ham px/hex yok; tek yeni sayı token katmanında: `--narrowing-form-min-block-size`, ölçümüyle.)*
- [x] Performans iş akışı iki adresi ölçüyor ve koşu raporu ikisini de yazıyor (D10). *(CI koşusu 44141121819: `/tr/` LCP 2288 ms · CLS 0,026 · `/tr/cars` LCP 2117 ms · CLS 0,016; iki rapor `0014-performance.json` ve `0001-performance.json` olarak yükleniyor.)*
- [x] `docs/architecture.md` § Karar Defteri: liste adresi `/{locale}/cars` (D9). *("Vitrin adresleri" ve "Etkin diller (uç)" satırları.)*
- [x] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4). *(#85'in son koşusu beş kapıda yeşil: Backend, Frontend, E2E, C3, Değişen yollar.)*
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). *(Yerel tam koşu 1245 test, 0 kırmızı; QA turu dört satır ekledi ve CI'da yeşil.)*
- [x] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor. *(Migration yok.)*
- [x] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi. *(D9 ve D22.)*
- [x] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez. *(İki bulgu — BULGU-1 AC-8, BULGU-2 AC-66 — repro ve mutasyon
      kanıtıyla Takım Yöneticisi tarafından iletildi; ikisi de merge'den önce kapandı
      (`fix(host)` + `test(dealership)`). Gözden geçirenin bir QA oturumu olduğunu yazar
      kendisi göremez; bu kayıt TM'nin aktarımına dayanıyor.)*
- [x] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. *(#85: "Karşılanmayan kriterler: Yok".)*
- [x] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi. *(2026-11-17, `90b4e98`.)*
- [x] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

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
| **Kriterler önce `main`'de miydi?** | **evet** | **Ölçüldü:** `git cat-file -e 90b4e98^:specs/0014-ana-sayfa.md` → var; ebeveyn `0254da6` (#84) **66** kriter taşıyor ve uygulama `90b4e98` (#85) ondan sonra geldi. Kriterler `main`'e ilk kez #82'de (`14a04bd`, 61 kriter) girdi. R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır — düşük sayı iyi spec değil, karşılaştırılacak tabanı olmayan spec demektir. |
| **Kriter sayısı: önce → bugün** | **61 → 66** | Kriterlerin `main`'e ilk girdiği `14a04bd`'de **61**, uygulamanın ebeveyni `0254da6`'da ve bugün **66**. Ara: #83 sekiz kriteri kaldırdı (AC-29–AC-36, D11), on biri ekledi (AC-62–AC-72) → 64; #84 ikisini ekledi (AC-73, AC-74) → 66. Uygulama commit'i spec'e **dokunmadı** (`git diff 0254da6 90b4e98 -- specs/` boş). O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. İkisi arasındaki fark, revizyon sayısının söylemediği şeyi söyler: `0008` bu ölçümde **2 → 33** çıktı ve kartı revizyon **1** yazıyordu. |
| **Spec revizyon sayısı** | **2** | **Bölüm özetleriyle ölçüldü** (bayt / sha256 ilk 12): `14a04bd` R 2634/`cfcf0b44b98c` · C 4260/`a4013fc6dcc3` · AC 8191/`4c5a29e5df76` → `5ddeeb9` (#83, QA CLARIFY) **R, C ve AC değişti** → `0254da6` (#84, plan turu) R aynı, **C** (X14) **ve AC** (AC-72/73/74) değişti → `90b4e98` **üçü de bayt bayt aynı**. İki tur da kriterler `main`'e girdikten sonra ve uygulamadan önce geldi; ikisi de kendi `docs/` PR'ında. Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? Açıklayıcı not eklemek de sayılır: nete ihtiyaç duyan kural, eksik yazılmış kuraldır. Intent, Context, kapsam dışı gerekçeleri ve SCORECARD değişiklikleri **sayılmaz**. Yüksekse spec eksik yazılmıştır. **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3) — ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve kriter sayısının iki katına çıkması bunun en net cevabıdır. Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | **1** | QA triyajı PR'ı bir kez geri döndürdü; tur iki commit doğurdu (`fix(host)` BULGU-1, `test(dealership)` BULGU-2). T10 kutusu `[x]` — K3'ün şartı sağlanıyor, kaydın dayanağı kutuda yazılı. PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. **`0` ile `—` aynı şey değildir:** `0`, gözden geçirildi ve hiç tur dönmedi demektir; gözden geçirme hiç yapılmadıysa `—` yazılır. İkisini aynı göstermek, ölçülmemiş işi temiz işmiş gibi gösterir. **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). Bu, "gözden geçirildi"yi var olan bir kutuya bağlar: kural eskiden kimin gözden geçirdiğini söylemiyordu ve üç kart (`0002`, `0007`, `frontend-002`) tam o boşlukta `0` yazdı. |
| **Dış bulgu (gerçek/gürültü)** | **2 / 0** | İletilen iki bulgunun ikisi de **yeniden üretildi ve gerçek çıktı**: BULGU-1'in üç satırı önce kırmızı verdi; BULGU-2'nin `tr-TR` mutasyonu eski testte geçti, yeni testte kırmızı verdi. **Gürültü yarısı ölçülemedi:** triyajın gürültü işaretlediği bir bulgu varsa bana iletilmedi — `0012c` ve `0013`'te iki kez gürültü etiketi ölçülünce gerçek çıkmıştı. `0`, "gürültü yoktu" değil "gürültü etiketli bulgu bu karta ulaşmadı" demektir. **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz — kendi kodunu okuyan kişi kendi körlüğünü paylaşır. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. **Ölçtüğü şey: spec ve kod ne kadar açık yazıldı.** |
| **Uygulama içi bulgu** | **5** | (1) **D22'nin ilk biçimi** (`<html data-locales>`) `<html>` etiketini birebir ölçen tema testlerini bozacaktı — #84 merge edilmeden meta etikete çevrildi. (2) **CLS**: ana sayfanın koşuları 0,007 · **0,098** · 0,023 — form iskeleti 126px, gerçek form 296px; düzeltildi, CI'da üç koşu da 0,026. (3) **AC-57 e2e testi** blok `h1`'in kutusunu ölçüyordu (16 = 16), metnin hizasını değil; Range ile düzeltildi. (4) **AC-67 sunucu testi** yapılandırma dizisinden eleman silemiyordu (`InvalidOperationException`); renderer'ın kendi bağımlılığıyla kuruldu. (5) **AC-11 birim testi** Arapça etiketleri tahmin ediyordu; doğrulanmış Türkçe yardımcılara çevrildi. (1) ve (2) ürün kusuru, (3)–(5) kendi testimin kusuru. Yazarın kendi işini yaparken bulduğu kusur sayısı — kendi testi kırmızı verdiği, kendi kanıtına baktığı veya kendi sondası yakaladığı için. Dış bulguyla aynı sütunda toplanmaz: ikisi **farklı şey** ölçer. **Ölçtüğü şey: yazar kendi işini ne kadar sorguladı.** Sıfır, "hiç hata yapmadım" değil "hiçbirini kendim bulmadım" demektir ve dış bulgu yüksekse bu bir uyarıdır. |
| **Regresyon sayısı** | **0** | `main`'e ulaşan yok. Liste adresinin değişmesi (D9) bilinçli bir davranış değişikliği: eski adresler 301 ile çalışmaya devam ediyor (AC-6, AC-69, AC-70), 63 e2e ziyareti yeni adrese taşındı ve hiçbir iddia gevşetilmedi. Tam e2e takımı (iki profil) CI'da yeşil. Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | **izleniyor** | Merge 2026-11-17 (`90b4e98`). Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**

**Medyanı geçen bir ölçüm, kapının güvende olduğunu söylemez.** Ana sayfanın ilk
Lighthouse ölçümü üç koşuda CLS 0,007 · 0,098 · 0,023 verdi: medyan 0,023, kapı yeşil.
Ama tek bir koşu eşiğin 0,002 altındaydı — `0001`'in kaçan hatası (#67/#71 kırmızı, #68
yeşil) tam bu desendi ve kapı aynı kodda rastgele davranıyordu. Ölçünce sebep basitti:
iskelet gerçek formun yarısından azını ayırıyordu. Kural: bir perf koşusunu okurken
yalnız medyana değil **en kötü koşuya** bak; iskeletlerin ayırdığı yüksekliği gerçek
içerikle birkaç genişlikte ölç ve fontlar yüklendikten **sonra** ölç — yoksa font
kaymasını veri kayması sanarsın (14px'lik kalıntı buydu).

İkinci ders kanıtın kendisiyle ilgili: QA'nın BULGU-2'si üretim kodunda değil **testin
örneğinde** bir zayıflık buldu — "Ataşehir/Bakırköy" çifti ordinal ve kültür sırasında
aynı sonucu veriyordu. Örnek spec'ten geliyordu (AC-66). Bir kriter "ordinal" gibi bir
**ayrım** iddia ediyorsa, örneği de o ayrımı yapan bir çift olmalı; aksi hâlde test
sıranın **varlığını** ölçer, **türünü** ölçmez.
