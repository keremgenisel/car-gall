# 0015 — Vitrin Listesi: Sıralama ve Filtre Eksenleri

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (vitrin okuma tarafı) · `VehicleReference` (okuma yüzeyi) · `frontend/` public vitrin |
| **Bağımlı spec'ler** | `0001` (tamamlandı) — bu spec onun **X1, X2, X3, X4** maddelerini geri açar |
| **Dayanak** | ADR 0003 unsur 1 ve Ek 1, sıra #9 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — beş açık sorunun cevabı
> onları belirliyor.

---

## Intent

`0001` vitrin listesini tek bir daraltma ekseniyle (**kasa tipi**) teslim etti ve bunu
bilinçli yaptı: fiyat, yıl, kilometre, marka, yakıt ve vites eksenleri ile ziyaretçi seçimli
sıralama o spec'in KAPSAM DIŞI bölümünde gerekçesiyle yazılıdır.

Bugünün gerçeği şu: **ziyaretçi aradığı aracı hâlâ bulamıyor.** Kasa tipi, bir galeride
genellikle üç-dört değere dağılır; 128 ilanın içinde "sedan" seçmek listeyi yarıya indirir ve
ziyaretçi geri kalanı gözle tarar. Araç aramanın gerçek ekseni **fiyat** ve **marka**dır;
ikisi de bugün yok.

**Ziyaretçi** bütçesine ve beğendiği markaya göre daraltmak, sonucu kendi istediği sıraya
dizmek istiyor. **Galeri** bunu istiyor çünkü doğru araca ulaşamayan ziyaretçi talep
bırakmıyor.

Bu iş bittiğinde ziyaretçi listeyi birden çok eksende daraltır ve sıralamayı kendisi seçer;
daraltma ve sıralama paylaşılabilir bir adreste yaşar. Başarı ölçütü: **ziyaretçi, bütçesine
uyan araçları tek adımda görebiliyor.**

---

## Requirements

- **R1.** Ziyaretçi listeyi şu eksenlerde daraltabilir: **marka**, **kasa tipi**, **yıl
  aralığı**, **kilometre aralığı**, **fiyat aralığı**, **yakıt**, **vites**.
- **R2.** Eksenler birlikte uygulanır: bir eksende seçilenlerden **herhangi birine** uyan,
  **ve** diğer eksenlerin hepsini sağlayan ilanlar listelenir.
- **R3.** Her eksende birden çok değer seçilebilir (aralık eksenlerinde alt ve üst sınır).
- **R4.** Fiyatı "**fiyat sorunuz**" olan araç, fiyat aralığı filtresine **takılmaz**:
  fiyat aralığı seçildiğinde listede yer almaz. → `docs/domain.md` **P4**.
- **R5.** Ziyaretçi sıralamayı seçebilir. Seçenekler: **önce en yeni yayınlanan**, **fiyat
  artan**, **fiyat azalan**, **kilometre artan**, **yıl azalan**, **yıl artan**.
- **R6.** Fiyatı olmayan araçların fiyat sıralamasındaki yeri **tanımlıdır** ve her iki
  yönde aynıdır. → Açık soru **S1**.
- **R7.** Varsayılan sıralama değişmez: **vitrin sıralama ağırlığı**, eşitlikte en yeni
  yayınlanan. → `0001` AC-4 (regresyon koruması).
- **R8.** Daraltma çubuğundaki eksenler **alan ayarlarından türetilir**: bir alan filtrede
  görünür ⇔ açık **ve** public **ve** doğası gereği filtrelenebilir.
  → `docs/architecture.md` § 4. Açık soru **S2**.
- **R9.** Marka seçiminde ziyaretçi listeyi **daraltarak** marka bulabilir; bu bir arama
  değil, yüklenmiş listenin süzülmesidir. → Açık soru **S3**.
- **R10.** Her eksende seçili değer sayısı ve **tümünü temizle** eylemi görünür.
- **R11.** Daraltma veya sıralama değiştiğinde liste **ilk sayfadan** başlar.
  → `0001` AC-7 ile aynı kural.
- **R12.** Daraltma, sıralama ve sayfa **adreste** yaşar; bağlantı paylaşılabilir ve geri
  tuşu çalışır. `0001`'in mevcut adres biçimi **bozulmaz**. → Açık soru **S5**.
- **R13.** Hiç sonuç kalmadığında ekran, hangi eksenlerin etkin olduğunu söyler ve
  **temizle** eylemini sunar; "hiç yayında araç yok" durumundan ayrıdır.
  → `docs/frontend.md` § 3, `0001` AC-23/AC-24.
- **R14.** Liste üç locale'de çalışır; Arapça'da RTL. Eksen adları ve değerleri çevrilir;
  marka adları kanonik Latin kalır.

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
  mobilde LCP < 2,5 s dedi. Eksen sayısı artarken bu eşik **gevşetilmez**.
- **C10.** `0001`'in mevcut kabul kriterleri **regresyon olarak** korunur; bu spec onları
  geçersiz kılmaz, üzerine ekler. → ADR 0003 unsur 1.
- **C11.** Her kabul kriteri en az bir testle karşılanır.

### KAPSAM DIŞI

- **X1.** **Serbest metin arama** (model adı, açıklama içinde arama) — *neden:* `0001` § X1
  ile aynı gerekçe: TR `i/İ/ı/I`, AR hemze/ta marbuta normalizasyonu ve locale başına FTS
  konfigürasyonu kendi spec'ini hak eder. R9'un marka süzmesi arama **değildir** (S3).
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

---

## Context

### Mevcut davranış — doğrulandı

- **Bugün tek eksen var: kasa tipi.** Vitrin ucu `GET /api/catalog/showcase-listings`
  `bodyType` (çoklu), `page`, `pageSize`, `locale` parametrelerini okuyor
  (`ShowcaseEndpoints.cs`). Sözleşme `ShowcaseListingFilter(BodyTypes, Page, Locale, PageSize)`.
- **Sıralama sabit.** `0001` AC-4: vitrin sıralama ağırlığı, eşitlikte en yeni yayınlanan.
  Ziyaretçi seçimli sıralama `0001` § X4'te kapsam dışıydı.

> **Doğrulama uyarısı (2026-10-05):** bu taslak yazılırken `VehicleReference`'a
> **marka/model okuma yüzeyi ekleniyordu** (`/api/reference/models` ve devamı, henüz
> commit edilmemiş). Aşağıdaki "okuma yüzeyi yok" tespiti o iş tamamlandığında
> geçersizleşir ve spec onaya sunulmadan önce **yeniden doğrulanmalıdır.**

- **Referans veri tarafında yalnız kasa tipi açık.** `VehicleReference`'ın tek ucu
  `GET /api/reference/body-types`; **marka listesi için okuma yüzeyi yok** — R1 bunu
  gerektirir.
- **Geçersiz girdi hata değil.** `0001` C14/AC-15/AC-16: tanınmayan `bodyType` ve geçersiz
  sayfa numarası sessizce yok sayılır, sunucu 200 döner. Endpoint bunu `page`/`pageSize`'ı
  **string olarak** okuyarak çözmüş ve kod yorumu bu kararı açıkça yazıyor. Yeni eksenler
  **aynı davranışı** sürdürmek zorunda (C10).
- **Okuma modeli alanları taşıyor.** `ShowcaseListing` denormalize okuma modelinde fiyat,
  kilometre, yıl ve kasa tipi mevcut; `docs/architecture.md` § 4 kural 3 *"vitrin okuma
  modeli tüm alanları taşır"* diyor. Yani eksenlerin çoğu için **yeni veri gerekmiyor**,
  sorgu ve arayüz gerekiyor. *(Yakıt ve vites alanlarının okuma modelinde bulunup bulunmadığı
  bu taslakta doğrulanmadı — spec onaylanmadan önce kontrol edilmeli.)*

### Geçerli kurallar

**P4** (fiyatı olmayan araç fiyat filtresine takılmaz — R4'ün doğrudan kaynağı) ·
**A3** (yalnız `Yayında`/`Rezerve`) · **F3/F4** · `docs/architecture.md` § 3 (collation,
normalizasyon) · § 4 (filtre türetilir) · `docs/frontend.md` § 3, § 5 (filtre ve sayfalama
adreste).

### Bilinen tuzaklar

1. **Fiyatsız aracı sıralamada `0` saymak.** "Fiyat sorunuz" bir değer değildir (**P4**);
   artan sıralamada başa gelirse liste yalan söyler (S1).
2. **Aralık eksenlerini tek değer gibi kurmak.** Yıl ve kilometre aralık; tek değerli filtre
   kurup sonradan aralığa çevirmek adres biçimini bozar (R12, S5).
3. **`0001`'in adres biçimini kırmak.** Paylaşılmış bağlantılar mevcut; `bodyType`
   parametresi korunmalı (S5).
4. **Marka listesini sunucuya aratmak.** Yirmi markalık bir listede sunucu tarafı arama,
   C1'in Türkçe collation tuzağını bedava davet eder (S3).
5. **Türetme kuralını atlamak.** "Tramersiz" filtresi tramer alanı kapatıldığında
   kaybolmalı; aksi hâlde filtre çubuğu alan ayarlarından bağımsız ikinci bir gerçek olur
   (R8, S2).
6. **Performans eşiğini sessizce gevşetmek.** Yedi eksen, tek eksenden pahalıdır; C9 eşiği
   sabit tutuyor ve bu ölçülmek zorunda.

---

## Açık sorular

**S1. Fiyatı olmayan araçlar fiyat sıralamasında nereye gider?**
*Öneri:* **her iki yönde de sona.**
*Gerekçe:* "fiyat sorunuz" bir fiyat değildir; artanda başa, azalanda başa koymak iki
farklı yalan üretir. Sona koymak tek ve tutarlı bir kural verir: *fiyatı belli olanlar
sıralanır, belli olmayanlar arkada durur.*
*Bedel:* fiyatını gizleyen pahalı araçlar, azalan sıralamada görünürlük kaybeder — galeri
için istenmeyen bir yan etki olabilir.

**S2. "Tramersiz" gibi opsiyonel alan filtreleri türetilecek mi?**
*Öneri:* **evet**, R8 uygulanır: alan kapalı veya gizliyse o eksen çubukta **görünmez**.
*Gerekçe:* `docs/architecture.md` § 4 bunu kural olarak yazdı ve `0002` AC-10 ile zaten bir
kez uygulandı.
*Bedel:* kapalı bir alanı adreste taşıyan eski bir bağlantı açıldığında o eksen sessizce
yok sayılır (`0001` AC-14 deseniyle tutarlı) — ziyaretçi daraltmanın uygulanmadığını fark
etmeyebilir.

**S3. Marka seçiminde "ara" kutusu olacak mı?**
Mockup, marka açılırının içinde bir "Marka ara" kutusu gösteriyor; `0001` § X1 serbest metin
aramayı kapsam dışı bırakmıştı.
*Öneri:* kutu **olsun ama istemci tarafı süzme olarak**; sunucuya arama isteği gitmez.
*Gerekçe:* marka listesi birkaç düzine değerdir ve zaten yüklüdür. İstemci tarafı süzme,
C1'in collation tuzağını hiç doğurmaz ve X1'i açmaz.
*Bedel:* Türkçe karakterli marka adlarında istemci süzmesi de normalizasyon ister
(örneğin "Citroën"); bu, uçta küçük bir yardımcı demektir.

**S4. Yakıt ve vites okuma modelinde var mı?**
Bu taslakta **doğrulanmadı.** `docs/architecture.md` § 4 yakıt ve vitesi **çekirdek alan**
sayıyor, § 4 kural 3 ise okuma modelinin tüm alanları taşıdığını söylüyor — yani olması
beklenir, ama bakılmadı.
*Öneri:* spec onaya sunulmadan önce kontrol edilsin; yoksa okuma modeline eklenmesi bu
spec'in kapsamına girer.
*Gerekçe:* "muhtemelen vardır" ile yazılan bir Requirements, Developer'ın planını kırar.
*Bedel:* yoksa bir migration ve bir projeksiyon değişikliği eklenir.

**S5. Adres biçimi ne olur ve `0001`'in bağlantıları ne olacak?**
*Öneri:* mevcut `bodyType` parametresi **aynen korunur**; yeni eksenler kendi
parametrelerini ekler; aralık eksenleri alt/üst sınır olarak iki değer taşır. Hiçbir
parametre zorunlu olmaz.
*Gerekçe:* `0001` AC-15 paylaşılabilir bağlantıyı kriter yaptı; var olan bağlantıların
çalışmaya devam etmesi bir regresyon sınırıdır (C10).
*Bedel:* adres uzar; yedi eksenli bir daraltma uzun bir bağlantı üretir. Kısaltma
(örneğin tek parametrede kodlama) okunabilirliği ve geri uyumluluğu bozar — önermiyorum.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> her eksen tek tek ve birlikte · aralık eksenlerinin sınır davranışı · **P4**/R4 ·
> altı sıralama seçeneği ve fiyatsız araçların yeri · varsayılan sıralamanın korunması ·
> türetme kuralı (kapalı/gizli alan) · geçersiz girdinin sessizce yok sayılması ·
> `0001`'in mevcut bağlantılarının çalışmaya devam etmesi · ilk sayfaya dönüş ·
> boş sonuç ekranı · performans eşiği · üç locale ve RTL.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı; **S4 kodda doğrulandı** ve sonucu Context'e yazıldı.
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
