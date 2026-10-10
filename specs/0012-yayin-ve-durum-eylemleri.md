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
- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012-ac<n>.png`), kısmi
      başarı ve hata durumları dâhil, RTL için Arapça görüntü de var.
- [ ] A2'nin **dört** koşulunun hepsinin kontrol edildiği testle gösterildi (Tuzak 1).
- [ ] Mimari testler yeşil; durum geçişi mantığı agregatta kaldı (C2).
- [ ] Yeni migration yalnızca `vehicle_catalog` şemasına dokunuyor.
- [ ] Olay adları `docs/architecture.md` § 1'deki adlarla birebir aynı (S5).
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi.
- [ ] Yeni domain terimi yok.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | |
| **Düzeltme turu sayısı** | | |
| **Bulgu gerçek/gürültü oranı** | / | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
