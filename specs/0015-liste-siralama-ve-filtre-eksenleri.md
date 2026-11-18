# 0015 — Vitrin Listesi: Sıralama ve Filtre Eksenleri

| | |
|---|---|
| **Durum** | Taslak — kararlar verildi, kabul kriterleri yazılmadı |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (vitrin okuma tarafı) · `VehicleReference` (okuma yüzeyi; yakıt ve vites değer listesi — **D11**) · `frontend/` public vitrin |
| **Bağımlı spec'ler** | `0001` (tamamlandı) — bu spec onun **X2, X3 (şube hariç) ve X4** maddelerini geri açar (**D16**). `0014` (tamamlandı) — liste adresi `/{locale}/cars` (`0014` D9). |
| **Dayanak** | ADR 0003 unsur 1 ve Ek 1, sıra #9 |
| **Karar bekliyor** | yok — S1–S5 → **D1–D5**; QA CLARIFY turunun on bir bulgusu (B1–B11) → **D6–D16** (2026-11-18). |

> **QA CLARIFY turu (2026-11-18, `main` @ `1349713`).** Bir QA oturumu taslağı
> uygulamadan önce cevapsız sorular için okudu: S1–S5'in durumunu ölçtü ve on bir yeni boşluk
> buldu (B1–B11). Önerilerin **tamamı** (S1 ve S5'in Analist önerileri dâhil) Takım
> Yöneticisi (depo sahibi) tarafından onaylandı ve **D1–D16** olarak işlendi. En büyüğü
> **D6**: veride marka kavramı yok, marka ekseni kapsam dışına çıktı (**X8**). Context
> yeniden doğrulandı (§ Context, K1–K4).
>
> **Bu hâlâ bir taslaktır.** Kabul kriterleri yazılmadı; kararlar onları belirliyor ve
> kriter turu Analist'in bir sonraki işidir. Kriterler `main`'de olmadığı için bu tur
> SCORECARD'da revizyon **sayılmaz** (ADR 0008 / K3).

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
  değerlerini listeler; değerler ziyaretçiye açık bir okuma ucundan gelir. Değer başına
  sonuç sayısı gösterilmez (**X11**). → **D11**.

---

## Constraints

### Uyulması zorunlu

- **C1.** Ham metin kolonu üzerinde `ILIKE`/`lower()` ile eşleştirme yapılmaz (Türkçe
  `i/İ/ı/I` tuzağı). → Yasak Listesi 27, `docs/architecture.md` § 3.
- **C2.** Sıralama locale collation'ı ile yapılır. → `docs/architecture.md` § 3.
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
  | Geçersiz girdi | Tanınmayan `sort` → varsayılan sıralama; tanınmayan slug ve geçersiz aralık → yok sayılır (R15). Sunucu `200` döner, uç adresi `replaceState` ile düzeltir (`0001` C14) |

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
- **K2 — Yakıt ve vitesin değer listesi için public uç yok.** `VehicleReference`'ın HTTP
  uçları yalnız `body-types` ve `models`; `IReferenceTermQueries` yalnız etiket çözüyor
  (`ResolveLabelsAsync`), değer listesi döndürmüyor. 2026-10-05 taslağının "marka/model
  okuma yüzeyi ekleniyor" doğrulama uyarısı bu ölçümle **kapandı**. → **D11**.
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

**D11 — Yakıt ve vites için public bir değer listesi ucu bu spec'e girer; eksenler
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

> Kriter turunda yazılacak (D1–D16'nın üzerine). En az şu kümeleri kapsayacak:
> her eksen tek tek ve birlikte · aralık eksenlerinin sınır davranışı (dahil sınır, tek
> sınır, alt > üst, tam sayı olmayan ve negatif değer — R15) · **P4**/R4 (tek fiyat sınırı
> dâhil) · altı sıralama seçeneği, fiyatsız araçların yeri ve eşitlik kırıcısı · seçimli
> sıralamada öne çıkarmanın yok sayılması (D7) · varsayılan sıralamanın korunması ·
> C12'nin adres sözleşmesi ve kanonik biçimi · tanınmayan `sort` ve slug'ın sessizce yok
> sayılması · yakıt/vites değer listesi ucu (D11) · `0001`'in mevcut bağlantılarının
> çalışmaya devam etmesi · ilk sayfaya dönüş · seçim özeti (R10) · boş sonuç ekranı ·
> performans eşiğinin üç profili (C9) · üç locale ve RTL.

---

## Definition of Done

- [x] Açık sorular karara bağlandı; **S4 kodda doğrulandı** ve sonucu Context'e yazıldı.
      *(S1–S5 → D1–D5, B1–B11 → D6–D16, 2026-11-18.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0015-ac<n>.png`), boş sonuç dâhil, Arapça görüntü
      de var.
- [ ] `0001`'in kabul kriterleri **regresyon testi olarak** hâlâ yeşil (C10).
- [ ] `0001` C3'ün performans eşikleri yeni eksenlerle **ölçüldü** ve karşılandı (C9).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil; ham metin üzerinde `ILIKE`/`lower()` kullanılmadı (C1).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi.
- [ ] <!-- DoD:T7 --> Yeni domain terimi yok.
- [ ] Ham piksel/hex eklenmedi; token katmanı kullanıldı.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0001`'i bozup bozmadığı burada görünür. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
