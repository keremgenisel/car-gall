# 0015 — Vitrin Listesi: Sıralama ve Filtre Eksenleri

| | |
|---|---|
| **Durum** | Tamamlandı (2026-11-18) — kriterler #88 → `5bf560c`, uygulama #89 → `bd0cde8` |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (vitrin okuma tarafı) · `frontend/` public vitrin. `VehicleReference`'ın mevcut public uçları **tüketilir**, değişmez (**D17**). |
| **Bağımlı spec'ler** | `0001` (tamamlandı) — bu spec onun **X2, X3 (şube hariç) ve X4** maddelerini geri açar (**D16**). `0014` (tamamlandı) — liste adresi `/{locale}/cars` (`0014` D9). |
| **Dayanak** | ADR 0003 unsur 1 ve Ek 1, sıra #9 |
| **Karar bekliyor** | yok — S1–S5 → **D1–D5**; QA CLARIFY turunun on bir bulgusu (B1–B11) → **D6–D16** (2026-11-18); plan turunun beş bulgusu (P1–P5) → **D17–D21** (2026-11-18). |

> **Plan turu (2026-11-18, `e1a440e` üzerinde).** Developer uygulama planını çıkarırken
> beş bulgu getirdi (P1–P5); önerilerin **tamamı** Takım Yöneticisi (depo sahibi) tarafından
> onaylandı ve **D17–D21** olarak işlendi. İkisi bu turun kendi ölçümünü düzeltiyor: **K2
> yanlıştı** — yakıt ve vitesin public değer listesi ucu `0010a`'dan beri var (**D17**) — ve
> C12'nin "tanınmayan slug'ı uç düzeltir" cümlesi `0001` AC-17 ile çelişiyordu (**D18**).
> R16, C2 ve C12 güncellendi; onaylı plan § Context'te. Kriterler hâlâ `main`'de olmadığı
> için revizyon **sayılmaz**.
>
> **QA CLARIFY turu (2026-11-18, `main` @ `1349713`).** Bir QA oturumu taslağı
> uygulamadan önce cevapsız sorular için okudu: S1–S5'in durumunu ölçtü ve on bir yeni boşluk
> buldu (B1–B11). Önerilerin **tamamı** (S1 ve S5'in Analist önerileri dâhil) Takım
> Yöneticisi (depo sahibi) tarafından onaylandı ve **D1–D16** olarak işlendi. En büyüğü
> **D6**: veride marka kavramı yok, marka ekseni kapsam dışına çıktı (**X8**). Context
> yeniden doğrulandı (§ Context, K1–K4).
>
> *(Bu turda spec hâlâ taslaktı ve kriterleri yoktu; kriterler `main`'de olmadığı için tur
> SCORECARD'da revizyon **sayılmaz**, ADR 0008 / K3.)* **Kriter turu (2026-11-18, #88):**
> 46 kabul kriteri D1–D21'den türetildi — Developer yazdı, Takım Yöneticisi onayına sunuldu
> (§ Acceptance Criteria). *(QA BULGU-2, 2026-11-18: bu paragraf kriterler eklendikten sonra da
> spec'i kriterleri yokmuş gibi anlatıyordu.)*

---

## Intent

`0001` vitrin listesini tek bir daraltma ekseniyle (**kasa tipi**) teslim etti ve bunu
bilinçli yaptı: fiyat, yıl, kilometre, marka, yakıt ve vites eksenleri ile ziyaretçi seçimli
sıralama o spec'in KAPSAM DIŞI bölümünde gerekçesiyle yazılıdır.

Bugünün gerçeği şu: **ziyaretçi aradığı aracı hâlâ bulamıyor.** Kasa tipi, bir galeride
genellikle üç-dört değere dağılır; 128 ilanın içinde "sedan" seçmek listeyi yarıya indirir ve
ziyaretçi geri kalanı gözle tarar. Araç aramanın gerçek ekseni **fiyat** ve **marka**dır;
ikisi de bugün yok. *(2026-11-18: marka ekseni bu spec'te gelmiyor — veride marka kavramı
yok, **D6** → X8. Başarı ölçütü fiyat ekseniyle karşılanır.)*

**Ziyaretçi** bütçesine ve beğendiği markaya göre daraltmak, sonucu kendi istediği sıraya
dizmek istiyor. **Galeri** bunu istiyor çünkü doğru araca ulaşamayan ziyaretçi talep
bırakmıyor.

Bu iş bittiğinde ziyaretçi listeyi birden çok eksende daraltır ve sıralamayı kendisi seçer;
daraltma ve sıralama paylaşılabilir bir adreste yaşar. Başarı ölçütü: **ziyaretçi, bütçesine
uyan araçları tek adımda görebiliyor.**

---

## Requirements

- **R1.** Ziyaretçi listeyi şu eksenlerde daraltabilir: **kasa tipi**, **yıl aralığı**,
  **kilometre aralığı**, **fiyat aralığı**, **yakıt**, **vites**. *(2026-11-18: marka
  ekseni **D6** ile çıktı → X8.)*
- **R2.** Eksenler birlikte uygulanır: bir eksende seçilenlerden **herhangi birine** uyan,
  **ve** diğer eksenlerin hepsini sağlayan ilanlar listelenir.
- **R3.** Her eksende birden çok değer seçilebilir (aralık eksenlerinde alt ve üst sınır).
  Aralığın sınır davranışı **R15**'tedir.
- **R4.** Fiyatı "**fiyat sorunuz**" olan araç, fiyat aralığı filtresine **takılmaz**:
  fiyat aralığının **en az bir sınırı** verildiğinde listede yer almaz. → `docs/domain.md`
  **P4**, **D10**.
- **R5.** Ziyaretçi sıralamayı seçebilir. Seçenekler: **önce en yeni yayınlanan**, **fiyat
  artan**, **fiyat azalan**, **kilometre artan**, **yıl azalan**, **yıl artan**. Seçimli
  sıralamalarda **öne çıkarma ağırlığı yok sayılır** (**D7**); eşitlikte önce **en yeni
  yayınlanan**, sonra **ilan kimliği** gelir (**D8**).
- **R6.** Fiyatı olmayan araçlar fiyat sıralamasında **her iki yönde de sona** gider.
  → **D1**.
- **R7.** Varsayılan sıralama değişmez: **vitrin sıralama ağırlığı**, eşitlikte en yeni
  yayınlanan, sonra ilan kimliği. Öne çıkarma ağırlığı **yalnız** varsayılan sıralamada
  geçerlidir (**D7**). Varsayılan sıralama R5'in seçeneklerinden ayrıdır ve adrese
  yazılmaz (**D9**). → `0001` AC-4 (regresyon koruması).
- **R8.** Bu sürümde **opsiyonel alan ekseni yoktur**; R1'in eksenlerinin tümü çekirdek
  alandır (fiyat ayrı bir kavramdır) ve **her zaman** görünür. Alan ayarlarından türetilen
  eksenler (tramer vb.) **X9**. → **D12**, `docs/architecture.md` § 4. *(2026-11-18: "eksenler
  alan ayarlarından türetilir" yazıyordu; uygulanacak bir ekseni yoktu.)*
- **R9.** *(Kapsam dışına çıktı, 2026-11-18 — **D6** → X8.)* *(Önceki metin: marka
  seçiminde istemci tarafı süzme — S3.)*
- **R10.** Her eksende seçim özeti ve **tümünü temizle** eylemi görünür. Çok değerli
  eksenlerde özet **seçili değer sayısıdır**; aralık eksenlerinde **seçilen sınırlardır**
  ("2018–2021", "≤ 500.000"), ICU ile kurulur. → **D15**.
- **R11.** Daraltma veya sıralama değiştiğinde liste **ilk sayfadan** başlar.
  → `0001` AC-7 ile aynı kural.
- **R12.** Daraltma, sıralama ve sayfa **adreste** yaşar; bağlantı paylaşılabilir ve geri
  tuşu çalışır. `0001`'in mevcut adres biçimi **bozulmaz**. Adres sözleşmesi **C12**'dedir.
  → **D5**, **D9**.
- **R13.** Hiç sonuç kalmadığında ekran, hangi eksenlerin etkin olduğunu söyler ve
  **temizle** eylemini sunar; "hiç yayında araç yok" durumundan ayrıdır.
  → `docs/frontend.md` § 3, `0001` AC-23/AC-24.
- **R14.** Liste üç locale'de çalışır; Arapça'da RTL. Eksen adları ve değerleri çevrilir.
  *(2026-11-18: "marka adları kanonik Latin kalır" cümlesi D6 ile çıktı.)*
- **R15.** Aralık eksenlerinin sınırları **dahildir** (`≥` alt, `≤` üst). Tek sınır geçerlidir
  (açık uçlu aralık). Alt sınır üst sınırdan büyükse, değer bir tam sayı değilse veya
  negatifse **o eksen yok sayılır** ve adres düzeltilir; sunucu `200` döner. Fiyat sınırları
  para biriminin **ana biriminde tam sayıdır**. → **D10**, `0001` C14.
- **R16.** Yakıt ve vites eksenleri, kasa tipinde olduğu gibi referans verinin **tüm**
  değerlerini listeler; değerler ziyaretçiye açık **mevcut** okuma uçlarından gelir
  (`/api/reference/fuel-types`, `/api/reference/transmission-types`). Değer başına sonuç
  sayısı gösterilmez (**X11**). → **D11**, **D17**.

---

## Constraints

### Uyulması zorunlu

- **C1.** Ham metin kolonu üzerinde `ILIKE`/`lower()` ile eşleştirme yapılmaz (Türkçe
  `i/İ/ı/I` tuzağı). → Yasak Listesi 27, `docs/architecture.md` § 3.
- **C2.** Sıralama locale collation'ı ile yapılır. → `docs/architecture.md` § 3. *(2026-11-18, **D21**: bu spec'in altı sıralamasının hepsi sayısal veya tarihsel; metin sıralaması yoktur. Kural, metin sıralaması getiren ilk spec'te uygulanır.)*
- **C3.** Para ve yüzde `decimal`; fiyat `Money` ile taşınır; üç ondalıklı para birimleri
  bozulmaz. → Altın Kural 4, **P1–P2**.
- **C4.** Yalnızca `Yayında` ve `Rezerve` ilanlar listelenir. → `0001` C/AC-2, AC-3.
- **C5.** Gizli ve kapalı alanlar yanıtta **hiç bulunmaz**; eleme sorgu tarafında.
  → **F3**, Yasak Listesi 41.
- **C6.** Filtre **üçüncü eksen değildir, türetilir.** → `docs/architecture.md` § 4.
- **C7.** Kullanıcıya görünen metin koda gömülmez; sayı içeren cümleler ICU ile kurulur.
  → Altın Kural 6, Yasak Listesi 31.
- **C8.** CSS'te fiziksel yön özellikleri kullanılmaz; renk ve tipografi `frontend-002`
  token katmanından gelir. → Yasak Listesi 25, `docs/frontend.md` § 4.1.
- **C9.** **Performans eşiği korunur:** `0001` C3, 500 yayındaki ilanda p95 < 400 ms ve
  mobilde LCP < 2,5 s dedi. Eksen sayısı artarken bu eşik **gevşetilmez**. p95, 500 ilanla
  **üç profilde ayrı ayrı** ölçülür: (1) filtresiz, (2) R1'in bütün eksenleri dolu,
  (3) `sort=price-asc`. → **D13**.
- **C10.** `0001`'in mevcut kabul kriterleri **regresyon olarak** korunur; bu spec onları
  geçersiz kılmaz, üzerine ekler. → ADR 0003 unsur 1.
  **İstisna — `0001` AC-11** ("daraltma çubuğu yalnızca kasa tipi eksenini sunar"): R1 ve
  **D16** onu bilerek genişletir; testi "çubuk tam olarak R1'in altı eksenini sunar, başka
  filtre grubu yoktur" iddiasına taşınır. *(2026-11-18, uygulama sırasında ölçüldü — tam e2e
  koşusunda kırmızı; Developer önerisi, Takım Yöneticisi onayına sunuldu.)*
- **C11.** Her kabul kriteri en az bir testle karşılanır.
- **C12.** **Adres sözleşmesi** — `0001` C13'ün genişlemesi; oradaki kurallar (locale'den
  bağımsız İngilizce ad, tekrarlı parametre, küçük harfli ASCII slug, tam ve harfe duyarlı
  eşleştirme) yeni parametrelere de uygulanır. → **D5**, **D9**.

  | Öğe | Karar |
  |---|---|
  | Rota | `/{locale}/cars` (`0014` D9) |
  | Kasa tipi | `bodyType` — **aynen korunur** (`0001` C13) |
  | Yakıt · vites | `fuel` · `transmission` — tekrarlı slug parametresi |
  | Yıl aralığı | `yearMin` · `yearMax` — tam sayı |
  | Kilometre aralığı | `mileageMin` · `mileageMax` — tam sayı |
  | Fiyat aralığı | `priceMin` · `priceMax` — para biriminin ana biriminde tam sayı (R15) |
  | Sıralama | `sort` — `newest` · `price-asc` · `price-desc` · `mileage-asc` · `year-desc` · `year-asc` |
  | Kanonik biçim | Hiçbir parametre zorunlu değil. Varsayılan sıralama `sort` yazmaz; `page=1` ve boş parametre yazılmaz |
  | Geçersiz girdi | Sunucu her durumda `200` döner ve geçersiz girdiyi yok sayar (`0001` C14). **Tanınmayan `sort` ve geçersiz aralık** (R15) → uç adresi `replaceState` ile düzeltir: kural sözdizimseldir. **Tanınmayan slug** (`bodyType`, `fuel`, `transmission`) → adreste **kalır**, sunucu yok sayar (`0001` AC-17 deseni, **D18**) |

### KAPSAM DIŞI

- **X1.** **Serbest metin arama** (model adı, açıklama içinde arama) — *neden:* `0001` § X1
  ile aynı gerekçe: TR `i/İ/ı/I`, AR hemze/ta marbuta normalizasyonu ve locale başına FTS
  konfigürasyonu kendi spec'ini hak eder.
- **X2.** **"En çok görüntülenen" sıralaması** — *neden:* `Engagement` modülü yoktur.
- **X3.** **"Önce hasarsız araçlar" sıralaması** — *neden:* mockup'ta var ama hasar bilgisi
  opsiyonel alandır ve kapatılabilir; kapalıyken sıralama seçeneği anlamsız kalır.
  R8'in türetme kuralı sıralamaya uygulanmamıştır ve bu ayrı bir karardır.
- **X4.** **Şubeye göre daraltma** — *neden:* `0001` § X3'teki gerekçe sürüyor; şube ekseni
  şube sayfalarıyla (`Dealership` vitrin tarafı) birlikte ele alınmalı.
- **X5.** **Kaydedilmiş arama / filtre hatırlama** — *neden:* public üyelik yok (§ 6);
  adres paylaşılabilirliği (R12) bu ihtiyacın büyük kısmını karşılar.
- **X6.** **Sonsuz kaydırma** — *neden:* `0001` § X7 kararı sürüyor.
- **X7.** **Haritada gösterme / mesafeye göre sıralama** — *neden:* PostGIS § 6'da kapsam
  dışı.
- **X8.** **Marka ekseni ve marka arama kutusu** (eski R9, S3). *neden:* veride **marka
  kavramı yok** (§ Context, K1); `0014` X13 aynı ölçümle ana sayfanın marka bölümünü
  çıkardı. Marka taksonomisi (`VehicleReference`) ayrı bir kart olarak önerilir; bu eksen,
  `0014` X11'in marka bağlantısı ve X13'ün marka bölümü ona bağlanır. → **D6**.
- **X9.** **Opsiyonel alan eksenleri** ("Tramersiz" vb.) ve onların alan ayarlarından
  türetilmesi. *neden:* R1'in eksenleri çekirdek; "tramersiz"in anlamı (alan boş mu, `0` mı)
  tanımsız. Bu eksen geldiğinde türetme kuralı (`docs/architecture.md` § 4) onunla gelir.
  → **D12**.
- **X10.** **Ana sayfa daraltma formunun genişlemesi** (`0014` X10). *neden:* ana sayfa
  formunun kendi kriterleri ve kendi performans ölçümü var; bu spec yalnız listeyi kapsar.
  Ayrı bir kart olarak önerilir. → **D14**.
- **X11.** **Değer başına sonuç sayısı** (facet sayımı: "Dizel (14)"). *neden:* her istekte
  eksen başına bir sayım sorgusu demek; C9'un eşiğini tehdit eder. → **D11**.

---

## Context

### Mevcut davranış — yeniden doğrulandı (2026-11-18, `main` @ `1349713`)

- **Bugün tek eksen var: kasa tipi.** Vitrin ucu `GET /api/catalog/showcase-listings`
  `bodyType` (çoklu), `page`, `pageSize`, `locale` parametrelerini okuyor
  (`ShowcaseEndpoints.cs`). Sözleşme `ShowcaseListingFilter(BodyTypes, Page, Locale, PageSize)`.
- **Liste `/{locale}/cars` adresinde.** `0014` D9 listeyi taşıdı; `/{locale}/` artık ana
  sayfa ve `bodyType`, `page` veya `pageSize` taşıyan eski bağlantıları listeye yönlendiriyor
  (`0014` D17). Eski bağlantılar yeni parametre taşımadığı için bu spec o yönlendirmeyi
  değiştirmez.
- **Sıralama sabit.** Öne çıkarma ağırlığı, sonra en yeni yayınlanan, sonra ilan kimliği
  (`ShowcaseListingQueries.cs:38-40`). Ağırlık `0012c`'den beri iki değerli: öne çıkarılmış
  veya değil. Ziyaretçi seçimli sıralama `0001` § X4'te kapsam dışıydı.
- **Geçersiz girdi hata değil.** `0001` C14/AC-15/AC-16: tanınmayan `bodyType` ve geçersiz
  sayfa numarası sessizce yok sayılır, sunucu 200 döner. Endpoint bunu `page`/`pageSize`'ı
  **string olarak** okuyarak çözmüş ve kod yorumu bu kararı açıkça yazıyor. Yeni eksenler
  **aynı davranışı** sürdürmek zorunda (C10).
- **Okuma modeli eksenlerin verisini taşıyor (S4 cevaplandı).** `ShowcaseListing`:
  `BodyTypeSlug`, `FuelTypeSlug` (`:41`), `TransmissionTypeSlug` (`:43`), `ModelYear`,
  `MileageKilometers` (zorunlu `int`), `PriceAmount` (`decimal?`) ve `PriceOnRequest`.
  **Yeni veri ve migration gerekmiyor**; sorgu, okuma ucu ve arayüz gerekiyor. → **D4**.

### QA CLARIFY turu ölçümü (2026-11-18)

- **K1 — Veride marka kavramı yok.** Kodda `Brand`/`Make` tipi yok; `VehicleModel` yalnız
  kimlik ve kanonik ad taşıyor ve marka adın içinde (`'Toyota Corolla'`,
  `SeedInitialVehicleModels.cs:17`). `0010a` X14 taksonomiyi `VehicleReference`'ın kendi
  spec'ine bıraktı; `0014` K5 aynı ölçümle marka bölümünü çıkardı. → **D6**.
- **K2 — ~~Yakıt ve vitesin değer listesi için public uç yok.~~** **Yanlış ölçüm, plan
  turunda düzeltildi (2026-11-18, D17):** `ReferenceEndpoints.cs` `0010a`'dan beri
  (`90af789`, 2026-10-05) `/api/reference/fuel-types` ve `/api/reference/transmission-types`
  uçlarını eşliyor (`MapTerm`); `IReferenceTermQueries.GetByKindAsync` türün tüm değerlerini
  kanonik ada göre sıralı döndürüyor ve kod yorumu "**Uçlar oturum GEREKTİRMEZ**" diyor.
  Panel onları zaten tüketiyor (`catalogAdmin.ts`). İlk ölçüm yalnız `body-types` ve
  `models`'in açık handler'larını saymış, `MapTerm` döngüsünü atlamıştı. → **D11**, **D17**.
- **K3 — R1'in eksenleri çekirdek alan.** Çekirdek alanlar: marka, model, yıl, kasa,
  kilometre, yakıt, vites, renk, şube (`docs/domain.md:100`); kapatılamazlar (**F4**). Fiyat
  ayrı bir kavram (**P4**, yayın şartı: fiyat **veya** "fiyat sorunuz"). Alan ayarlarında
  bugün tek opsiyonel alan `damage_record_amount` (tramer). R8'in türetme kuralının R1'de
  uygulanacağı bir eksen yok. → **D12**.
- **K4 — Seçimli sıralamaların eşitliği tanımsızdı.** Yıl ve fiyatta eşitlik olağan;
  kırıcısı olmayan sıralama sayfa geçişinde ilanı tekrarlar veya kaybeder. Varsayılanın
  kırıcısı (`ShowcaseListingQueries.cs:39-40`) referans alındı. → **D8**.

### Geçerli kurallar

**P4** (fiyatı olmayan araç fiyat filtresine takılmaz — R4'ün doğrudan kaynağı) ·
**A3** (yalnız `Yayında`/`Rezerve`) · **F3/F4** · `docs/architecture.md` § 3 (collation,
normalizasyon) · § 4 (filtre türetilir) · `docs/frontend.md` § 3, § 5 (filtre ve sayfalama
adreste).

### Bilinen tuzaklar

1. **Fiyatsız aracı sıralamada `0` saymak.** "Fiyat sorunuz" bir değer değildir (**P4**);
   artan sıralamada başa gelirse liste yalan söyler (D1).
2. **Aralık eksenlerini tek değer gibi kurmak.** Yıl ve kilometre aralık; tek değerli filtre
   kurup sonradan aralığa çevirmek adres biçimini bozar (R12, C12).
3. **`0001`'in adres biçimini kırmak.** Paylaşılmış bağlantılar mevcut; `bodyType`
   parametresi korunmalı (C12).
4. **Marka listesini sunucuya aratmak.** *(D6 ile marka ekseni çıktı; tuzak X8'in kartına
   devreder.)* Yirmi markalık bir listede sunucu tarafı arama, C1'in Türkçe collation
   tuzağını bedava davet eder.
5. **Türetme kuralını atlamak.** *(D12 ile opsiyonel alan eksenleri çıktı; tuzak X9'un
   kartına devreder.)* "Tramersiz" filtresi tramer alanı kapatıldığında kaybolmalı; aksi
   hâlde filtre çubuğu alan ayarlarından bağımsız ikinci bir gerçek olur.
6. **Performans eşiğini sessizce gevşetmek.** Altı eksen, tek eksenden pahalıdır; C9 eşiği
   sabit tutuyor ve bu üç profilde ölçülmek zorunda (D13).
7. **Öne çıkarmayı seçimli sıralamaya taşımak.** Varsayılan sıralamanın ilk anahtarı öne
   çıkarma ağırlığı; seçimli sıralamaya kopyalanırsa "fiyat artan"ın başında pahalı bir
   öne çıkan ilan durur (D7).

### Verilen kararlar — 2026-11-18

**Takım Yöneticisi (depo sahibi) kararı**, QA CLARIFY turu üzerine. Önerilerin tamamı
**olduğu gibi** onaylandı.

**D1 — Fiyatsız araçlar fiyat sıralamasında her iki yönde de sona gider (S1).**
*Gerekçe:* "fiyat sorunuz" bir fiyat değildir; artanda başa, azalanda başa koymak iki farklı
yalan üretir. *Bedel:* fiyatını gizleyen pahalı araçlar azalan sıralamada görünürlük
kaybeder. → R6.

**D2 — S2'nin önerisi D12 ile yer değiştirdi.** S2 türetme kuralını "Tramersiz" gibi bir
eksen için soruyordu; o eksen R1'de yok (K3). → D12.

**D3 — S3 düştü.** Marka ekseni yok (D6); marka arama kutusu X8'e gider.

**D4 — S4 kodda doğrulandı.** Yakıt ve vites okuma modelinde var (§ Context); yeni veri
gerekmiyor.

**D5 — Adres biçimi geriye uyumludur (S5).** `bodyType` aynen korunur, yeni eksenler kendi
parametrelerini ekler, aralıklar iki sınır taşır, hiçbir parametre zorunlu değildir.
*Bedel:* adres uzar; tek parametrede kodlama okunabilirliği ve geri uyumluluğu bozar,
seçilmedi. Parametre adları D9'da. → R12, C12.

**D6 — Marka ekseni bu spec'ten çıkar (B1).** R1'den marka, R9, R14'ün marka cümlesi ve S3
kapsam dışına (**X8**). Marka taksonomisi kartı bu eksenin önkoşuludur. *Gerekçe:* veride
marka yok (K1); Intent'in başarı ölçütü fiyat ekseniyle karşılanır; taksonomiyi buraya
almak `VehicleReference`'a varlık, migration ve model→marka bağı getirir ve spec'i ikiye
böler. *Elenen alternatif:* model ekseni — eksen sayısını büyütür, marka gelince atılır.
*Bedel:* Intent'in "gerçek eksen fiyat ve marka" cümlesinin yarısı ertelenir.

**D7 — Öne çıkarma ağırlığı yalnız varsayılan sıralamada geçerlidir (B2).** *Gerekçe:*
"fiyat artan" listesinin başında pahalı bir öne çıkan ilan durursa liste yalan söyler.
*Bedel:* galerinin öne çıkardığı ilan seçimli sıralamada öne çıkmaz. → R5, R7.

**D8 — Seçimli sıralamaların eşitlik kırıcısı: en yeni yayınlanan, sonra ilan kimliği
(B3).** *Gerekçe:* varsayılanın kırıcısıyla aynı; sayfa geçişinde deterministik ve test
edilebilir sıra (K4). → R5.

**D9 — Adres sözleşmesi parametre adlarıyla yazılır (B4).** `fuel`, `transmission`,
`yearMin`/`yearMax`, `mileageMin`/`mileageMax`, `priceMin`/`priceMax`, `sort`; varsayılan
sıralama adrese yazılmaz; tanınmayan `sort` varsayılana düşer ve adres düzeltilir.
*Gerekçe:* paylaşılabilir adres kriterlerin konusu; adlar yazılmazsa test edilecek bir şey
yok. → R7, R12, C12.

**D10 — Aralık sınırları dahildir; geçersiz aralık yok sayılır; fiyat sınırları ana
birimde tam sayıdır (B5).** Tek sınır geçerlidir. Alt > üst, tam sayı olmayan veya negatif
değer → o eksen yok sayılır, adres düzeltilir, sunucu `200`. Fiyatın tek bir sınırı bile
verilmişse "fiyat sorunuz" araç listelenmez. *Gerekçe:* yok saymak `0001` C14'ün geçersiz
girdi davranışı; sınırları yer değiştirmek niyet tahmini olur. Tam sayı, `,`/`.` ondalık
karmaşasını ve üç ondalıklı para birimlerinin ayrıştırma tuzağını yok eder. → R4, R15, C12.

**D11 — *("uç bu spec'e girer" yarısı **D17** ile düştü)* Yakıt ve vites için public bir değer listesi ucu bu spec'e girer; eksenler
referans verinin tüm değerlerini listeler; değer başına sayı yoktur (B6).** *Gerekçe:*
kasa tipiyle (`0014` D19) tutarlı; değer başına sayım her istekte bir facet sorgusu demek
ve C9'u tehdit eder (K2). → R16, X11, Modül(ler).

**D12 — Bu sürümde opsiyonel alan ekseni yoktur (B7).** R8 daraltıldı; tramer ekseni ve
türetme kuralının uygulanması **X9**. *Gerekçe:* R8 ölçülecek bir şeyi olmayan bir kuraldı
(K3); "tramersiz"in anlamı tanımsız. → R8, X9.

**D13 — Performans eşiği üç profilde ölçülür (B8).** Filtresiz · R1'in bütün eksenleri dolu
· `sort=price-asc`; 500 ilan, her profilde p95 < 400 ms. *Gerekçe:* profil yazılmazsa en
ucuz sorgu ölçülür ve kriter boşa yeşil yanar. → C9.

**D14 — Ana sayfa formunun genişlemesi ayrı bir karttır (B9).** *Gerekçe:* spec zaten büyük;
ana sayfa formunun kendi kriterleri ve ölçümü var. → X10.

**D15 — Aralık eksenlerinin özeti sınırlarıdır (B10).** *Gerekçe:* bir aralığın "seçili
değer sayısı" ziyaretçiye bir şey söylemez. → R10.

**D16 — Başlık düzeltildi: bu spec `0001`'in X2, X3 (şube hariç) ve X4'ünü açar (B11).**
*Gerekçe:* başlık X1–X4 diyordu; X1 (serbest metin) ve X3'ün şube kısmı bu spec'te de
kapsam dışı. → Bağımlı spec'ler.

### Verilen kararlar — plan turu, 2026-11-18

**Takım Yöneticisi (depo sahibi) kararı**, Developer'ın plan bulguları P1–P5 üzerine.
Beş öneri **olduğu gibi** onaylandı.

**D17 — Yakıt ve vites değer listesi için mevcut public uçlar kullanılır; yeni uç açılmaz
(P1).** D11'in "public bir değer listesi ucu bu spec'e girer" yarısı **düşer**; "tüm
değerleri listeler, değer başına sayı yok" yarısı kalır. `VehicleReference` Modül(ler)
satırından çıkar. *Gerekçe:* K2 yanlış ölçümdü; düzeltilmeseydi kriter turu zaten var olan
bir davranışa "yeni" kriter yazar ve plan hayali bir iş içerirdi. → K2, R16, D11.

**D18 — Tanınmayan slug adreste kalır; aralık ve `sort` adresini uç düzeltir (P2).**
*Gerekçe:* uç bugün tanınmayan `bodyType`'ı bilerek silmiyor (`listUrlState.ts`: "kanonik
kümeyi sunucu bilir") ve `0001` AC-17 bunu ölçüyor; C10 o kriterin korunmasını istiyor. Uç
değer listesi yüklenmeden hangi slug'ın geçerli olduğunu bilemez. Aralık ve `sort`'un
geçerliliği ise sözdizimseldir; uç sunucuyu beklemeden aynı kuralı uygular. *Bedel:*
ziyaretçi tanınmayan slug'ın uygulanmadığını adresten anlayamaz — `0001`'den beri öyle.
→ C12.

**D19 — Aralık eksenleri Enter veya "Uygula" ile uygulanır; çoklu seçim eksenleri anında
(P3).** Bir aralığın uygulanması geçmişe **tek** kayıt ekler. *Gerekçe:* her tuş vuruşunu
adrese yazmak geçmişe kayıt yığar ve R12'nin "geri tuşu çalışır" şartını bozar. → R3, R12.

**D20 — Çubuğun düzeni: çok değerli eksenler çip, her aralık iki sayı girişi; çubuğun
ayrılan yüksekliği ölçülerek kurulur (P4).** Katlanır bir "Filtreler" paneli bu sürümde
yoktur. *Gerekçe:* `0001` ve `0014` aynı CLS tuzağına düştü (iskelet gerçek içerikten
kısaydı); ayrılan yükseklik genişlik kırılımlarında ölçülmeden yazılmaz. *Bedel:* mobilde
çubuk uzun; liste katlamanın altına iner. → C8, C9.

**D21 — C2 bu spec'te uygulanacak yer bulmaz (P5).** Altı sıralamanın hepsi sayısal veya
tarihsel; kural metin sıralaması getiren ilk spec'e kalır. → C2.

### Uygulama planı (onaylı, 2026-11-18)

Dal `feature/0015-liste-eksenleri`; tek PR, adım başına commit (`[plan 0015/<n>]`).
Migration yalnız C9 ölçümü bir indeks gerektirirse ve yalnız `catalog` şemasına.

1. **Catalog — istek modeli** (R3, R15, C12): `ShowcaseListingFilter` yeni alanlar;
   `ShowcaseListingRequest` aralık ve `sort` normalizasyonu; `ShowcaseSort`; yakıt ve vitesin
   kanonik kümesi `BodyTypeCatalog` deseniyle (`IReferenceTermQueries.GetByKindAsync`).
2. **Catalog — sorgu ve sıralama** (R1, R2, R4–R7): eksenler sorguda; `ShowcaseOrdering`
   sort'a göre (D1, D7, D8); öne çıkan okuması varsayılanda kalır; uç yeni parametreleri
   string okur.
3. **Performans** (C9): benchmark üç profil (D13); indeks yalnız ölçüm gerektirirse.
4. **Uç — durum ve API** (R11, R12, C12, D18): `listUrlState`, `useListUrlState`,
   `shared/api/catalog.ts`.
5. **Uç — filtre arayüzü** (R1, R10, R13, R14, R16, D19, D20): `features/catalog/filters/`,
   `ShowcaseListPage`, `EmptyByFilter`; çeviriler; ayrılan yükseklik ölçümü.
6. **e2e ve kanıt**: `daraltma`, `paylasilabilirlik`, `evidence-0015`, RTL.

---

## Açık sorular

> **Tüm sorular karara bağlandı** (2026-11-18): S1–S5 → **D1–D5**, QA bulguları B1–B11 →
> **D6–D16** (§ Context). Soruların metni gerekçe kaydı olarak aşağıda duruyor.
> **Açık soru yok.**

**S1. Fiyatı olmayan araçlar fiyat sıralamasında nereye gider?** → **D1**
*Öneri:* **her iki yönde de sona.**
*Gerekçe:* "fiyat sorunuz" bir fiyat değildir; artanda başa, azalanda başa koymak iki
farklı yalan üretir. Sona koymak tek ve tutarlı bir kural verir: *fiyatı belli olanlar
sıralanır, belli olmayanlar arkada durur.*
*Bedel:* fiyatını gizleyen pahalı araçlar, azalan sıralamada görünürlük kaybeder — galeri
için istenmeyen bir yan etki olabilir.

**S2. "Tramersiz" gibi opsiyonel alan filtreleri türetilecek mi?** → **D2**, **D12**
*Öneri:* **evet**, R8 uygulanır: alan kapalı veya gizliyse o eksen çubukta **görünmez**.
*Gerekçe:* `docs/architecture.md` § 4 bunu kural olarak yazdı ve `0002` AC-10 ile zaten bir
kez uygulandı.
*Bedel:* kapalı bir alanı adreste taşıyan eski bir bağlantı açıldığında o eksen sessizce
yok sayılır (`0001` AC-14 deseniyle tutarlı) — ziyaretçi daraltmanın uygulanmadığını fark
etmeyebilir.

**S3. Marka seçiminde "ara" kutusu olacak mı?** → **D3** (düştü, X8)
Mockup, marka açılırının içinde bir "Marka ara" kutusu gösteriyor; `0001` § X1 serbest metin
aramayı kapsam dışı bırakmıştı.
*Öneri:* kutu **olsun ama istemci tarafı süzme olarak**; sunucuya arama isteği gitmez.
*Gerekçe:* marka listesi birkaç düzine değerdir ve zaten yüklüdür. İstemci tarafı süzme,
C1'in collation tuzağını hiç doğurmaz ve X1'i açmaz.
*Bedel:* Türkçe karakterli marka adlarında istemci süzmesi de normalizasyon ister
(örneğin "Citroën"); bu, uçta küçük bir yardımcı demektir.

**S4. Yakıt ve vites okuma modelinde var mı?** → **D4** (var)
*Öneri:* spec onaya sunulmadan önce kontrol edilsin; yoksa okuma modeline eklenmesi bu
spec'in kapsamına girer.
*Gerekçe:* "muhtemelen vardır" ile yazılan bir Requirements, Developer'ın planını kırar.
*Bedel:* yoksa bir migration ve bir projeksiyon değişikliği eklenir.

**S5. Adres biçimi ne olur ve `0001`'in bağlantıları ne olacak?** → **D5**, **D9**
*Öneri:* mevcut `bodyType` parametresi **aynen korunur**; yeni eksenler kendi
parametrelerini ekler; aralık eksenleri alt/üst sınır olarak iki değer taşır. Hiçbir
parametre zorunlu olmaz.
*Gerekçe:* `0001` AC-15 paylaşılabilir bağlantıyı kriter yaptı; var olan bağlantıların
çalışmaya devam etmesi bir regresyon sınırıdır (C10).
*Bedel:* adres uzar; yedi eksenli bir daraltma uzun bir bağlantı üretir. Kısaltma
(örneğin tek parametrede kodlama) okunabilirliği ve geri uyumluluğu bozar — önermiyorum.

---

## Acceptance Criteria

> Her kriterin testi `DisplayName`'inde `0015/AC-<n>` taşır (`docs/testing.md` § 1).
> **Görünür ilan** = `Yayında` veya `Rezerve` (C4). Adresler `/tr/cars` üzerinden yazıldı;
> kural her locale'de aynıdır. Örnek veri kümeleri kriterin içinde verilir.
>
> *Kriter turu (2026-11-18) Developer tarafından yazıldı, Takım Yöneticisi onayına sunuldu:
> Analist turunun yerine geçtiği için yazan = uygulayan riski taşıyor ve dış gözden geçirme
> (DoD T10) bunu dengelemek için zorunlu.*

### Eksenler (R1, R2, R3, R16)

- **AC-1.** 3 `diesel` ve 2 `petrol` görünür ilan varken `?fuel=diesel` yalnız 3 dizel ilanı
  listeler; toplam sayı 3'tür.
- **AC-2.** Aynı veriyle `?fuel=diesel&fuel=petrol` 5 ilanı listeler (eksen içinde **veya**).
- **AC-3.** 2 `manual` ve 4 `automatic` görünür ilan varken `?transmission=manual` yalnız 2
  ilanı listeler.
- **AC-4.** Model yılları 2017, 2018, 2019, 2020, 2021 olan beş ilan varken
  `?yearMin=2018&yearMax=2020` 2018, 2019 ve 2020'yi listeler; sınırlar **dahildir** (R15).
- **AC-5.** Kilometreleri 49.999, 50.000 ve 50.001 olan üç ilan varken `?mileageMax=50000`
  ilk ikisini listeler.
- **AC-6.** Fiyatları 499.999, 500.000, 1.000.000 ve 1.000.001 olan dört ilan varken
  `?priceMin=500000&priceMax=1000000` ortadaki ikisini listeler.
- **AC-7.** Tek sınır geçerlidir: `?yearMin=2020` yalnız 2020 ve sonrasını, `?priceMax=500000`
  yalnız 500.000 ve altını listeler (açık uçlu aralık).
- **AC-8.** Eksenler **ve** ile birleşir: `?bodyType=suv&fuel=diesel&yearMin=2019` yalnız
  üç koşulun **hepsini** sağlayan ilanları listeler; bir koşulu sağlamayan ilan (dizel ama
  sedan, ya da SUV ama 2018) listede yoktur.
- **AC-9.** Ziyaretçiye açık `/api/reference/fuel-types` ve
  `/api/reference/transmission-types` uçları oturumsuz `200` döner ve türün **tüm**
  değerlerini (yakıt 4, vites 2) etkin locale'in adıyla verir (D17). Uç bu değerleri
  vitrinde bir ilana sahip olup olmamalarından bağımsız listeler; değerin yanında sonuç
  sayısı yazmaz (X11).

### Fiyat sorunuz (R4 · P4, D10)

- **AC-10.** "Fiyat sorunuz" bir ilan varken `?priceMax=1000000` ve ayrıca yalnız
  `?priceMin=1` o ilanı **listelemez**.
- **AC-11.** Fiyat sınırı verilmemişken (`?fuel=diesel`) "fiyat sorunuz" dizel ilan listede
  **görünür**.

### Geçersiz girdi (R15 · C12 · D10, D18)

- **AC-12.** `?yearMin=2021&yearMax=2018` (alt > üst) yıl eksenini yok sayar, diğer eksenleri
  uygular ve sunucu `200` döner.
- **AC-13.** `?mileageMax=abc`, `?mileageMax=-5` ve `?priceMax=500000.5` ilgili ekseni yok
  sayar; sunucu `200` döner.
- **AC-14.** Uç, AC-12 ve AC-13'teki geçersiz aralık parametrelerini adresten
  `replaceState` ile **kaldırır**; tarayıcı geçmişine kayıt eklenmez ve geçerli diğer
  parametreler adreste kalır.
- **AC-15.** `?sort=cheapest` (tanınmayan) varsayılan sıralamayı uygular; sunucu `200`
  döner ve uç `sort` parametresini adresten `replaceState` ile kaldırır.
- **AC-16.** `?fuel=kerosene` (tanınmayan slug) sunucu tarafında yok sayılır ve liste
  filtresiz döner; parametre adreste **kalır** (D18, `0001` AC-17 deseni).
- **AC-17.** Eşleştirme tam ve harfe duyarlıdır: `?fuel=Diesel` tanınmaz ve yok sayılır
  (C1, `0001` C13).

### Sıralama (R5, R6, R7 · D1, D7, D8)

- **AC-18.** `?sort=newest` ilanları yayın tarihi yeniden eskiye sıralar.
- **AC-19.** `?sort=price-asc` fiyatı artan sıralar; "fiyat sorunuz" ilanlar **sonda** durur.
- **AC-20.** `?sort=price-desc` fiyatı azalan sıralar; "fiyat sorunuz" ilanlar yine **sonda**
  durur (D1).
- **AC-21.** `?sort=mileage-asc` kilometreyi artan sıralar.
- **AC-22.** `?sort=year-desc` model yılını azalan, `?sort=year-asc` artan sıralar.
- **AC-23.** Öne çıkarılmış ve **en pahalı** ilan, `?sort=price-asc` ile **sonda**,
  `?sort=newest` ile yayın tarihine göre yerinde durur; öne çıkarma ağırlığı seçimli
  sıralamada yok sayılır (D7).
- **AC-24.** Eşitlik kırıcı: aynı fiyatlı iki ilandan yayın tarihi yeni olan önce gelir;
  fiyat ve yayın tarihi de eşitse ilan kimliği küçük olan önce gelir (D8).
- **AC-25.** Fiyatı ve yayın tarihi aynı 30 ilan varken `?sort=price-asc` ile birinci ve
  ikinci sayfa birlikte 30 **farklı** ilan içerir; tekrar eden veya kaybolan yoktur.
- **AC-26.** `sort` verilmediğinde sıralama `0001` AC-4'teki gibidir (öne çıkarma ağırlığı,
  yayın tarihi, ilan kimliği) ve uç varsayılan sıralamayı adrese **yazmaz** (R7, D9).

### Adres (R11, R12 · C12 · D5, D9, D19)

- **AC-27.** Ekranda yakıt `diesel` ve `hybrid`, vites `automatic`, yıl 2018–2021,
  kilometre ≤ 80.000, fiyat 500.000–1.500.000 ve "fiyat artan" seçildiğinde adres
  `/tr/cars?fuel=diesel&fuel=hybrid&transmission=automatic&yearMin=2018&yearMax=2021&mileageMax=80000&priceMin=500000&priceMax=1500000&sort=price-asc`
  biçimindedir: parametre adları C12'deki gibi, tekrarlı parametre, boş parametre ve
  `page=1` yok.
- **AC-28.** AC-27'nin adresi yeni bir sekmede açıldığında aynı seçimler ekranda işaretli
  ve aynı liste görünür.
- **AC-29.** `0001`'in biçimindeki `/tr/cars?bodyType=suv&page=2` aynı listeyi açmaya
  devam eder (C10).
- **AC-30.** 2. sayfadayken herhangi bir eksen veya sıralama değiştiğinde liste 1. sayfadan
  başlar ve bu **tek** bir geçmiş kaydıdır (R11).
- **AC-31.** Bir daraltmadan sonra tarayıcıda **geri** demek önceki daraltma hâlini ve
  listesini geri getirir.
- **AC-32.** Bir aralık alanına yazarken adres **değişmez**; aralık Enter'a basınca veya
  "Uygula"ya tıklanınca uygulanır ve geçmişe **tek** kayıt ekler (D19).

### Arayüz (R10, R13, R14, R16 · D15, D20)

- **AC-33.** Filtre çubuğu yakıt ve vitesin tüm değerlerini (AC-9) etkin locale'in adıyla
  ve uçtan gelen sırada çip olarak gösterir; çiplerin yanında sayı yoktur.
- **AC-34.** Çok değerli bir eksende 2 değer seçiliyken eksenin özeti seçili değer sayısını
  ICU ile söyler ("2 seçili").
- **AC-35.** Aralık ekseninin özeti seçilen sınırlardır: yıl 2018–2021 için "2018–2021",
  yalnız üst fiyat sınırı 500.000 için "≤ 500.000" (etkin locale'in sayı biçimiyle).
- **AC-36.** Bir eksenin temizle eylemi yalnız **o eksenin** değerlerini kaldırır; diğer
  eksenler ve sıralama korunur.
- **AC-37.** Sıralama seçici varsayılanla birlikte yedi seçeneği etkin locale'in metniyle
  sunar ve seçili olanı gösterir.
- **AC-38.** Seçili eksenlerle hiç sonuç yokken ekran **etkin eksenlerin adlarını** söyler ve
  tüm daraltmaları kaldıran bir temizle eylemi sunar; sıralama korunur.
- **AC-39.** Vitrinde hiç görünür ilan yokken `0001`'in "hiç yayında araç yok" durumu
  görünür, AC-38'in ekranı görünmez (`0001` AC-27 korunur).
- **AC-40.** `/ar/cars` sayfasında eksen adları, değerleri, özetler ve sıralama seçenekleri
  Arapça'dır ve çubuk sağdan sola akar (görsel kanıt).
- **AC-41.** Gizli veya kapalı opsiyonel alanlar ve şase, plaka, iç not liste yanıtında hiç
  bulunmaz; kart yanıtının şekli değişmez (`0001` AC-23/AC-24 korunur, C5).
- **AC-42.** `Taslak`, `Satıldı` ve `Arşiv` ilanlar hiçbir eksen ve sıralama birleşiminde
  listelenmez (C4).

### Performans (C9 · D13, D20)

- **AC-43.** 500 görünür ilanla filtresiz liste isteğinin p95'i < 400 ms'dir.
- **AC-44.** 500 görünür ilanla R1'in bütün eksenleri dolu istek (kasa tipi, yakıt, vites,
  yıl, kilometre, fiyat aralığı) p95 < 400 ms'dir.
- **AC-45.** 500 görünür ilanla `?sort=price-asc` isteğinin p95'i < 400 ms'dir.
- **AC-46.** `/tr/cars` Lighthouse mobil profilinde (3 koşu, medyan) LCP < 2,5 s ve
  CLS < 0,1 ölçülür; filtre çubuğu yüklenince altındaki liste kaymaz (D20).

---

## Definition of Done

- [x] Açık sorular karara bağlandı; **S4 kodda doğrulandı** ve sonucu Context'e yazıldı.
      *(S1–S5 → D1–D5, B1–B11 → D6–D16, 2026-11-18.)*
- [x] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi. *(#89: 46 kriterin 46'sı; karşılanmayan yok.)*
- [x] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0015-ac<n>.png`), boş sonuç dâhil, Arapça görüntü
      de var. *(`0015-ac33`, `ac37`, boş sonuç `ac38`, Arapça `ac40-ar`; gerçek kapak görselleriyle yeniden çekildi ve açılıp okundu.)*
- [x] `0001`'in kabul kriterleri **regresyon testi olarak** hâlâ yeşil (C10). *(Backend 1287 test ve e2e 291/291 yeşil. Tek istisna C10'da yazılı: `0001` AC-11 testinin iddiası R1'in eksen kümesine taşındı, onaylı #88.)*
- [x] `0001` C3'ün performans eşikleri yeni eksenlerle **ölçüldü** ve karşılandı (C9). *(Üç profil + `0001` profili, p95 ≤ 4,8 ms; `/tr/cars` LCP 2107 ms, CLS 0,022; CI'ın C3 kapısı yeşil.)*
- [x] <!-- DoD:T4 --> Mimari testler yeşil; ham metin üzerinde `ILIKE`/`lower()` kullanılmadı (C1). *(56/56 — `SpecDurumNotuTests` bu kartla eklendi. Eşleştirme `Contains` ve tam küme üyeliği; AC-17 harfe duyarlılığı ölçüyor.)*
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi. *(24 anahtar; `check:locales` 492 anahtar, eşit.)*
- [x] <!-- DoD:T7 --> Yeni domain terimi yok. *(`docs/domain.md` değişmedi.)*
- [x] Ham piksel/hex eklenmedi; token katmanı kullanıldı. *(Bileşen stillerinde ham değer yok; ayrılan yükseklik token katmanında, ölçümüyle.)*
- [x] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4). *(#89'un son koşusu beş kapıda yeşil; merge'den önce ağaç eşitliği kanıtlandı.)*
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). *(`VitrinEksenleriTests` 17, `VitrinSiralamaTests` 9, `EksenUcuTests` 9, `VitrinEksenDegerleriTests` 3.)*
- [x] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor. *(Migration yok; ölçüm indeks gerektirmedi — AP-12.)*
- [x] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi. *("Vitrin liste parametreleri" satırı.)*
- [x] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez. *(QA triyajının iki bulgusu repro'larıyla Takım Yöneticisi tarafından iletildi ve merge'den önce kapandı. Gözden geçirenin QA oturumu olduğunu yazar kendisi göremez; kayıt TM'nin aktarımına dayanıyor.)*
- [x] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. *(#89: "Karşılanmayan kriterler: Yok", AC-11 istisnası ayrıca yazılı.)*
- [x] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi. *(2026-11-18: #88 → `5bf560c`, #89 → `bd0cde8`.)*
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
| **Kriterler önce `main`'de miydi?** | **evet** | **Biçimsel olarak evet, fiilen hayır.** `git cat-file -e bd0cde8^:specs/…` → var; ebeveyn `5bf560c` (#88) 46 kriter taşıyor. Ama uygulama commit'leri dalda kriterler `main`'e girmeden başladı (ilk commit 10:30, #88 merge 11:31) ve **C bölümü uygulama sırasında değişti** (C10 istisnası `7bf6362`, 10:46; uygulamanın e2e'si onu ölçtü). Ölçüt squash commit'ine baktığı için bunu görmüyor — kriter PR'ı açık tutulup uygulamayla birlikte merge edildiğinde hep "evet" der. R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır — düşük sayı iyi spec değil, karşılaştırılacak tabanı olmayan spec demektir. |
| **Kriter sayısı: önce → bugün** | **46 → 46** | Kriterler `main`'e ilk kez `5bf560c`'de 46 olarak girdi ve uygulama değiştirmedi. O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. İkisi arasındaki fark, revizyon sayısının söylemediği şeyi söyler: `0008` bu ölçümde **2 → 33** çıktı ve kartı revizyon **1** yazıyordu. |
| **Spec revizyon sayısı** | **1** | **Harfiyen tanımla 0, yazılan 1.** Bölüm özetleri `5bf560c` ve `bd0cde8`'de bayt bayt aynı (R 3787/`bda226e7ece0` · C 5693/`f47b05223115` · AC 7880/`25ec9d8462e8`), yani `main`'e girdikten sonra değişiklik yok. Ama C10 istisnası uygulama başladıktan sonra, uygulamanın bir ölçümü üzerine eklendi; ölçütün sorduğu "spec eksik miydi" sorusunun cevabı **evet**. Kriter turu (#88) ve iki CLARIFY turu (#87) kriterler `main`'e girmeden geldi ve sayılmadı. Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? Açıklayıcı not eklemek de sayılır: nete ihtiyaç duyan kural, eksik yazılmış kuraldır. Intent, Context, kapsam dışı gerekçeleri ve SCORECARD değişiklikleri **sayılmaz**. Yüksekse spec eksik yazılmıştır. **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3) — ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve kriter sayısının iki katına çıkması bunun en net cevabıdır. Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | **1** | QA triyajı PR'ı bir kez geri döndürdü: `3c39181` (BULGU-1), `d238c87` + `438889d` (BULGU-2; ikincisi benim düzeltmemin kendi hatası). T10 `[x]`. PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. **`0` ile `—` aynı şey değildir:** `0`, gözden geçirildi ve hiç tur dönmedi demektir; gözden geçirme hiç yapılmadıysa `—` yazılır. İkisini aynı göstermek, ölçülmemiş işi temiz işmiş gibi gösterir. **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). Bu, "gözden geçirildi"yi var olan bir kutuya bağlar: kural eskiden kimin gözden geçirdiğini söylemiyordu ve üç kart (`0002`, `0007`, `frontend-002`) tam o boşlukta `0` yazdı. |
| **Dış bulgu (gerçek/gürültü)** | **2 / 0** | İkisi de yeniden üretildi ve gerçek çıktı: BULGU-1'in birim testi 2 yerine 4 kayıt gösterdi; BULGU-2'nin yeni kapısı 0015'te kırmızı verdi. **Gürültü yarısı yine ölçülemedi** — triyaj yalnız iki bulgu iletti (`0014` ile aynı durum). **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz — kendi kodunu okuyan kişi kendi körlüğünü paylaşır. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. **Ölçtüğü şey: spec ve kod ne kadar açık yazıldı.** |
| **Uygulama içi bulgu** | **10** | **Spec (3):** K2 yanlış ölçümdü — yakıt/vites uçları vardı (D17) · C12 `0001` AC-17 ile çelişiyordu (D18) · `0001` AC-11 R1 ile çelişiyordu (C10 istisnası). **Ürün (4):** altı aynı adlı "Seçimi temizle" düğmesi (erişilebilirlik; `0001` AC-9'u kırdı) · LCP 2107 → 2403 ms (panel değer listesini bekliyordu) · bu düzeltmenin ürettiği CLS 0,237 (grid satırları geriliyordu) · ayrılan yükseklik iki kez eskidi (23–35px ve 2–23px kayma; tek bulgu sayıldı). **Kendi testlerim (3):** RTL testi blok kutusunu ölçüyordu (`0014`'teki desenin aynısı) · AC-30 testi sahte sunucu yüzünden 2. sayfadan hiç başlamıyordu · AC-32 testinin başlangıç adresi aynı sebeple yanlıştı. Yazarın kendi işini yaparken bulduğu kusur sayısı — kendi testi kırmızı verdiği, kendi kanıtına baktığı veya kendi sondası yakaladığı için. Dış bulguyla aynı sütunda toplanmaz: ikisi **farklı şey** ölçer. **Ölçtüğü şey: yazar kendi işini ne kadar sorguladı.** Sıfır, "hiç hata yapmadım" değil "hiçbirini kendim bulmadım" demektir ve dış bulgu yüksekse bu bir uyarıdır. |
| **Regresyon sayısı** | **0** | `main`'e ulaşan yok. Dal içinde iki tane doğdu ve merge'den önce kapandı: LCP gerilemesi (yazar buldu) ve geri tuşunun aynı adresi üç kez kaydetmesi (QA buldu, dış bulgu #1). `0001`'in AC-11'i bir regresyon değil onaylı bir genişleme (C10). Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0001`'i bozup bozmadığı burada görünür. |
| **Kaçan hata** | **izleniyor** | Merge 2026-11-18 (`bd0cde8`). Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**

**"Kriterler önce `main`'de" ölçütü, kriter PR'ı açık tutulduğunda kör kalıyor.** Bu kartta
kriterleri uygulayan yazdı; Takım Yöneticisi içeriği görmeden merge edilmesin diye #88 açık
bekledi, uygulama onun üstünde ilerledi ve C10 uygulamanın bir ölçümü üzerine **uygulama
sırasında** değişti. Ölçüt squash commit'inin ebeveynine bakıyor ve "evet" diyor; korumak
istediği şey — kriterlerin uygulamadan önce donması — sağlanmadı. İki öneri: (1) ölçüt
uygulama **dalının ilk commit'inin** zamanına da baksın, (2) kriter turu Analist'e kalsın;
yazan = uygulayan olduğunda kriterler uygulamanın ihtiyacına göre biçimlenmeye açık.

İkinci ders ölçümün kendisiyle ilgili: bir CSS token'ında duran **ölçülmüş** bir sayı,
ölçtüğü içerik değiştiği anda eskir. Ayrılan yükseklik bu kartta iki kez eskidi ve ikisini de
yalnız yeniden ölçmek yakaladı; kapılar (stylelint, CI'ın C3'ü) ikisinde de yeşildi.
Panelin içeriğine dokunan her değişiklik bu ölçümü yeniden koşturmalı.
