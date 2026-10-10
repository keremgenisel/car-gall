# 0012 — Yayın ve Durum Eylemleri

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (komut yüzeyi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0010a` + `0010b` (araç kaydı, düzenleme ve liste) · **`0011` (Media) ÖNKOŞUL** · `0007` (invariant koruması — tamamlandı) |
| **Dayanak** | ADR 0003 Ek 1, sıra #5 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — aşağıdaki beş açık sorunun
> cevabı onları belirliyor.

---

## Intent

`0010a` bittiğinde galeri araç kaydı oluşturabilecek ama **hiçbirini vitrine çıkaramayacak.**
Vitrin, geliştirme amaçlı bir seed'e bağlı kalmaya devam eder — yani ürün hâlâ satılamaz
durumdadır. Bu spec o son halkayı kapatır.

**Galeri yöneticisi** hazırladığı aracı vitrine çıkarmak, satılanı satıldı olarak
işaretlemek ve ilgisini çekmeyen ilanı yayından almak istiyor. Günlük iş bir araç üzerinde
değil **liste üzerinde** yürüdüğü için, aynı işlemi birden çok ilana birlikte uygulamak
istiyor.

Bu iş bittiğinde galeri bir aracı yayına alabilir, rezerve edebilir, satıldı işaretleyebilir,
yayından kaldırabilir ve arşivleyebilir; bu geçişler vitrine ve diğer modüllere **duyurulur**.
Başarı ölçütü: **vitrindeki araçlar artık seed'den değil galerinin kendi kararından geliyor.**

---

## Requirements

- **R1.** Oturum açmış yetkili personel bir taslak ilanı **yayına alabilir**.
- **R2.** Bir ilan yayına alınırken **A2**'nin tüm koşulları sağlanmış olmalıdır: çekirdek
  alanlar dolu, fiyat belirlenmiş **veya** "fiyat sorunuz", en az bir medya varlığı **hazır**,
  aktif alan ayarlarında zorunlu işaretli opsiyonel alanlar dolu. Koşul sağlanmıyorsa yayına
  alma reddedilir ve **hangi koşulun eksik olduğu** söylenir.
- **R3.** Yayın durumu geçişleri yalnızca **A3**'te yazılı olanlardır; başka geçiş yoktur.
- **R4.** `Satıldı` **terminaldir**: araç yeniden yayına alınamaz. → **A4**.
- **R5.** Personel bir ilanı **rezerve** edebilir; rezerve ilan vitrinde rozetiyle görünmeye
  devam eder. → **R6** (domain).
- **R6.** Personel bir ilanı **yayından kaldırabilir** (taslağa döndürebilir) ve
  **arşivleyebilir**.
- **R7.** Personel **birden çok ilanı seçip** aynı eylemi hepsine uygulayabilir.
- **R8.** Toplu eylemde bazı ilanlar için eylem geçersizse, **geçerli olanlar uygulanır** ve
  uygulanamayanlar gerekçesiyle ayrı ayrı bildirilir. → Açık soru **S1**.
- **R9.** Her durum geçişi **duyurulur**; duyuru ilanın kimliğini ve yeni durumunu taşır.
- **R10.** Personel bir ilanın **vitrin sıralama ağırlığını** değiştirebilir (öne çıkarma).
  → Açık soru **S2**.
- **R11.** "Lokasyon ataması bekliyor" durumundaki araçlar listede **ayırt edilir** ve şubesi
  atanmadan yayına alınamaz. → **A8**.
- **R12.** Yayına alma, rezerve etme ve satıldı işaretleme eylemleri **eşzamanlı** çağrıldığında
  tek biri başarılı olur. → `0007`.
- **R13.** Panel listesi ve eylem ekranları **yükleniyor**, **boş**, **başarılı**, **kısmi
  başarı** ve **hata** durumlarını ele alır. → `docs/frontend.md` § 3.
- **R14.** Panel personelin tercih ettiği dilde çalışır; Arapça'da RTL.

---

## Constraints

### Uyulması zorunlu

- **C1.** Tüm eylemler **oturum gerektirir**. → `0006` R7.
- **C2.** Durum geçişi mantığı **agregatta** kalır; endpoint katmanına taşınmaz.
  → Yasak Listesi 20, C7.
- **C3.** Duyurular outbox ile, en-az-bir-kez ve idempotent. → `docs/architecture.md` § 7.
- **C4.** Veritabanında çevrilmiş durum adı saklanmaz; kod saklanır. → Yasak Listesi 22.
- **C5.** Fiyat değişikliği olursa fiyat geçmişine yazılır. → **A6**, `0010a` R8.
- **C6.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner — "hangi koşul
  eksik" bilgisi de kod olarak döner, metin olarak değil. → Yasak Listesi 23–24.
- **C7.** Mevcut agregat davranışı yeniden yazılmaz: `Listing.Publish`, `Reserve`,
  `ReturnToShowcase`, `MarkSold`, `Unpublish`, `Archive`, `ChangeShowcaseSortWeight` ve
  `Vehicle.EnsurePublishable` bugün vardır. Bu spec onların üzerine komut katmanı kurar.
- **C8.** Sayı içeren cümleler ICU ile kurulur ("3 ilan yayına alındı"). → Yasak Listesi 31.
- **C9.** Yeni migration yalnızca `vehicle_catalog` şemasına dokunur. → Yasak Listesi 3.
- **C10.** Her kabul kriteri en az bir testle karşılanır. → `AGENTS.md` Altın Kural 5.

### KAPSAM DIŞI

- **X1.** **Rezervasyonun yaşam döngüsü** — bitiş tarihi, süresi dolunca otomatik yayına
  dönüş, danışmana bildirim, iptal nedeni (`docs/domain.md` **R1–R6**) — *neden:* bu spec
  yalnızca **durum geçişini** kurar; rezervasyon kaydının kendisi bir zamanlayıcı ve bir
  bildirim akışı gerektirir ve kendi spec'ini hak eder.
- **X2.** **Satış sonrası iş akışı** (satış kaydı, kâr, fatura) — *neden:* ERP/muhasebe
  `docs/architecture.md` § 6'da kapsam dışıdır.
- **X3.** **Fiyat düşüşü rozeti ve fiyat geçmişinin ziyaretçiye gösterilmesi** — *neden:*
  `0002` § X6 bunu kapsam dışı bıraktı; vitrin kararıdır.
- **X4.** **Rol bazlı eylem kısıtı** ("danışman yayına alamasın") — *neden:* ADR 0003 unsur 3
  alan/kayıt düzeyi yetkiyi ayrı bir karara bağladı. Bu spec'te eylemler **rol ayrımı
  yapmadan** oturum açmış personele açıktır. → Açık soru **S4**.
- **X5.** **Dışa aktarma** ve **görüntülenme sütunu** — *neden:* biçim kararı ve `Engagement`
  modülünün yokluğu.
- **X6.** **Süresi geçmiş rezervasyon uyarısı** (mockup'taki "1 rezervasyon 3 günü geçti") —
  *neden:* X1'e bağlıdır.

---

## Context

### Mevcut davranış — doğrulandı

- **Agregat tarafı hazır.** `Listing` bugün `CreateDraft`, `Publish`, `Reserve`,
  `ReturnToShowcase`, `MarkSold`, `Unpublish`, `Archive`, `ChangeShowcaseSortWeight`
  taşıyor; `Vehicle` ise `EnsurePublishable`, `AttachMediaAsset`, `MarkAwaitingBranchAssignment`.
  **Eksik olan komut ve uç katmanıdır** — C7 bu yüzden var.
- **`0007` eşzamanlı yazma ve invariant korumasını zaten kurdu** (tamamlandı); R12 yeni bir
  mekanizma istemiyor, var olanı kullanıyor.
- **`VehicleCatalog`'un hiçbir yazma ucu yok.** Projedeki tüm `MapPost`/`MapPut` uçları
  `Identity` ve `Leads`'e ait.
- **Yayın medyaya kilitli.** `EnsurePublishable()` en az bir medya varlığı istiyor ve
  `MediaAssetReady` olayını yayan hiçbir şey yok — bu yüzden `0011` **önkoşuldur**, tercih
  değil.
- **Vitrin tüketicisi hazır.** `ShowcaseListingProjector` ve `0001`/`0002`'nin okuma modeli
  yayın durumuna göre görünürlüğü zaten uyguluyor; bu spec yeni bir vitrin davranışı
  getirmiyor, var olanı **besliyor**.

### Geçerli domain kuralları

**A2** (yayın koşulları) · **A3** (geçiş tablosu) · **A4** (`Satıldı` terminal; aynı araç
geri alınırsa **yeni kayıt**) · **A5** (şase tekilliği, arşiv muaf) · **A6** (fiyat geçmişi) ·
**A8** (şube kapatılınca lokasyon ataması bekler) · **R6** (rezerve araç vitrinde rozetle
görünür, iletişim formu kapalı) · **F2** (zorunluluk yalnızca o andan sonrasına uygulanır —
ama **A2** yayın anında bağlayıcıdır).

### Bilinen tuzaklar

1. **A2'yi yalnız medya üzerinden kontrol etmek.** `EnsurePublishable()` bugün **yalnızca**
   medya şartını kontrol ediyor ve doc comment'i bunu açıkça söylüyor. A2'nin diğer üç
   koşulu (çekirdek alanlar, fiyat/fiyat sorunuz, zorunlu opsiyonel alanlar) **henüz hiçbir
   yerde kontrol edilmiyor.** R2 bu boşluğu kapatmak zorunda.
2. **Toplu eylemi tek işlem sanmak.** 20 ilandan 3'ü geçersizse "hepsi başarısız" demek
   personeli tek tek denemeye zorlar; "hepsi başarılı" demek yalan söyler (S1).
3. **`Satıldı`dan geri dönüş.** A4 terminal diyor; arayüzde "geri al" düğmesi koymak kuralı
   deler. Yanlış işaretleme **yeni kayıt** açılarak düzeltilir.
4. **Arşivin şase tekilliğini serbest bırakması.** A5 muafiyeti arşivleme eyleminin yan
   etkisidir; arşivlerken bu unutulursa aynı araç bir daha girilemez.
5. **Eşzamanlı satıldı işaretleme.** İki danışman aynı aracı aynı anda satabilir; `0007`
   bunu kurdu ama komut katmanı onu **kullanmak** zorunda (R12).

---

## Açık sorular

**S1. Toplu eylemde kısmi başarı nasıl davranır?**
*Öneri:* geçerli olanlar uygulanır, uygulanamayanlar **tek tek gerekçesiyle** bildirilir;
işlem bir bütün olarak geri alınmaz.
*Gerekçe:* 20 ilandan 3'ü eksik fotoğraflı diye 17'sini yayına almamak, günlük işi durdurur.
*Bedel:* personel kısmi sonucu okumak zorunda; arayüz "3 ilan yayına alınamadı" listesini
göstermek zorunda (R13'ün "kısmi başarı" durumu).

**S2. Mockup'taki "Vitrine ekle" eylemi ne demek?**
İlan listesinde "Yayına al" ve "Vitrine ekle" ayrı iki düğme olarak duruyor.
*Öneri:* "Vitrine ekle" **ayrı bir eylem değildir**; `ChangeShowcaseSortWeight` ile
**öne çıkarma** anlamına gelir ve etiketi "Öne çıkar" olur.
*Gerekçe:* sözlükte "vitrin" zaten yayındaki ilanların listesidir; "yayına al" ile "vitrine
ekle" iki ayrı şey olursa aynı kavram iki isim alır.
*Bedel:* mockup'taki etiket değişir.

**S3. `0011` önkoşul mu, yoksa medya şartı geçici olarak gevşetilsin mi?**
*Öneri:* **önkoşul**; gevşetme yapılmaz.
*Gerekçe:* `0001` AC-17 kapak görselini kart alanı olarak zorunlu kıldı; görselsiz ilan
vitrine çıkarsa kart bozuk görünür. A2'yi delmek, bir kuralı "geçici olarak" kapatmanın
kalıcı hâle geldiği klasik yoldur.
*Bedel:* `0012`, `0011` bitene kadar başlayamaz.

**S4. Eylemler rol ayrımı yapacak mı?**
Mockup "Personel rolündesiniz: ilan taslak olarak kaydedilir, fiyat alanı kilitlidir" diyor.
*Öneri:* bu spec'te **rol ayrımı yok** (X4); tüm eylemler oturum açmış personele açık.
*Gerekçe:* ADR 0003 unsur 3 alan/kayıt düzeyi yetkiyi ayrı bir karara bağladı ve § 6 bunu
"basit rol modelini izin motoruna dönüştürür" diye uyardı.
*Bedel:* satış danışmanı da yayına alabilir ve fiyat değiştirebilir. Küçük ekipte kabul
edilebilir; değilse bu kararı önce vermek gerekir.

**S5. Durum geçişi duyuruları hangi olaylardır?**
`docs/architecture.md` § 1 olay akışı `VehiclePublished / Unpublished / PriceChanged /
Reserved / Sold / Relocated` sayıyor; kodda **hiçbiri yok** (doğrulandı: `VehicleCatalog`
yalnızca tüketici tarafı yazılmış).
*Öneri:* bu spec § 1'de yazılı adlarla **altısını** üretir.
*Gerekçe:* abonelerinin (`Engagement`, `Audit`, `Content` slot geçersizleme, sitemap)
hiçbiri henüz yok, ama olay adları dokümanda sabitlenmiş; sonradan ad değiştirmek
tüketicileri kırar.
*Bedel:* tüketicisi olmayan altı olay üretilir — `0003`'ün tersi durum ve bilinçli.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> A2'nin dört koşulunun ayrı ayrı reddi · A3'teki her geçiş ve her **geçersiz** geçiş ·
> A4 terminal kuralı · toplu eylem ve kısmi başarı · A8 lokasyon bekleyen araç ·
> eşzamanlılık · altı olayın üretimi ve idempotentlik · üç durum + kısmi başarı ekranı ·
> çok dil ve RTL.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] `0011` tamamlandı (S3).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012-ac<n>.png`), kısmi
      başarı ve hata durumları dâhil, RTL için Arapça görüntü de var.
- [ ] A2'nin **dört** koşulunun hepsinin kontrol edildiği testle gösterildi (Tuzak 1).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil; durum geçişi mantığı agregatta kaldı (C2).
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `vehicle_catalog` şemasına dokunuyor.
- [ ] Olay adları `docs/architecture.md` § 1'deki adlarla birebir aynı (S5).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi.
- [ ] <!-- DoD:T7 --> Yeni domain terimi yok.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
