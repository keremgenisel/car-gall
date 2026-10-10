# 0011 — Media: Yükleme, Türev Üretimi ve Filigran

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Media` (yeni — bugün tek bir olay dosyası) |
| **Bağımlı spec'ler** | `0010a` (araç kaydı — Onaylandı). `0012` bu spec'i **bekler**. |
| **Dayanak** | ADR 0003 Ek 1, sıra #4 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Intent, Requirements, Constraints ve Context doğrulanmış
> bilgiden yazıldı. **Kabul kriterleri yazılmadı** — aşağıdaki beş açık sorunun cevabı onları
> belirliyor. Cevaplar gelmeden spec onaya sunulmaz.

---

## Intent

Galerinin elindeki araç fotoğrafı sisteme **hiçbir biçimde girmiyor.** Bunun görünür sonucu
şudur: `0010a` araç kaydını mümkün kılsa bile hiçbir araç yayına alınamaz, çünkü
`Vehicle.EnsurePublishable()` en az bir medya varlığı şartını koşuyor. Vitrin, fotoğrafı
olmayan ilanları gösteremez; `0001` kapak görselini kart alanı olarak zorunlu kıldı.

**Galeri çalışanı** aracın fotoğraflarını yükleyip sıralamak istiyor; **galeri sahibi**
fotoğrafların kendi logosuyla çıkmasını istiyor, çünkü vitrin fotoğrafı rakip sitelere
kopyalanıyor.

Bu iş bittiğinde bir görsel yüklenir, işlenir, farklı boyutlarda türevleri üretilir,
filigranı basılır ve hazır olduğu **duyurulur**. Başarı ölçütü: **bir aracın yayına
alınmasının önündeki medya engeli kalkar.**

---

## Requirements

- **R1.** Oturum açmış personel bir görsel **yükleyebilir**.
- **R2.** Yüklenen her görsel bir **varlık kaydı** olarak tutulur: kimliği, işleme durumu ve
  depolama yolu.
- **R3.** Her varlık için farklı boyutlarda **türevler** üretilir; vitrin ve panel bu
  türevleri kullanır, orijinali değil.
- **R4.** Türevlere **galerinin filigranı** basılır.
- **R5.** Görselin konum ve cihaz bilgisi (EXIF) **temizlenir**.
- **R6.** Varlığın bir **işleme durumu** vardır: yüklendi · işleniyor · hazır · başarısız.
- **R7.** Varlık **hazır** olduğunda sistem bunu **duyurur**; duyuru varlığın kimliğini ve
  erişim yolunu taşır.
- **R8.** İşleme **başarısız** olduğunda sistem bunu da duyurur; varlık sessizce yüklendi
  durumunda kalmaz.
- **R9.** Her varlık **locale başına `alt` metni** taşıyabilir.
- **R10.** **Medya sahipsizdir:** bir varlığın hangi araca ait olduğu bu modülde tutulmaz.
  → `docs/architecture.md` § 1.
- **R11.** Desteklenmeyen bir dosya türü veya boyut sınırını aşan dosya reddedilir; reddedilen
  dosya varlık kaydı oluşturmaz.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Media`, varlık verisinin tek yazma sahibidir; hangi aracın hangi görsele sahip
  olduğu `VehicleCatalog`'dadır. → `docs/architecture.md` § 1.
- **C2.** Başka modülün dosya deposu yoluna doğrudan erişilmez ve `Media` başka modülün
  deposuna yazmaz. → Yasak Listesi 4.
- **C3.** Depolama **S3-uyumludur** (R2/MinIO) ve CDN ilk günden kullanılır.
  → `docs/architecture.md` § 7 (Karar Defteri: Medya).
- **C4.** Duyurular outbox ile, **en-az-bir-kez** ve idempotent gider. → § 7.
- **C5.** `Media` bir **yaprak** modüldür: hiçbir modül ona senkron bağımlı olmaz.
  → `docs/architecture.md` § 1 (Bağımlılık haritası).
- **C6.** Sağlayıcı kimlik bilgileri (S3 anahtarı) repoda durmaz; yapılandırmadan gelir ve
  eksikse uygulama **ayağa kalkmaz**. → `0009` C8 deseni.
- **C7.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner.
  → `AGENTS.md` Altın Kural 6.
- **C8.** Zaman damgaları UTC'dir. → Yasak Listesi 30.
- **C9.** Yeni migration yalnızca `media` şemasına dokunur. → Yasak Listesi 3.
- **C10.** Her kabul kriteri en az bir testle karşılanır. → `AGENTS.md` Altın Kural 5.

### KAPSAM DIŞI

- **X1.** **Vitrindeki galeri görüntüleyici** (şerit, büyütme, klavye gezinmesi) — *neden:*
  ADR 0003 Ek 1 sıra #10'un işi; bu spec varlığı üretir, gösterimi değil.
- **X2.** **Video ve 360° tur** — *neden:* her biri kendi işleme hattını ve oynatıcısını
  getirir; `docs/architecture.md` § 4 bunları opsiyonel alan olarak sayıyor ama V1'de
  görsel yeterlidir.
- **X3.** **Fotoğrafların araca bağlanması, sıralanması ve kapak seçimi** — *neden:* bu
  bilgi `VehicleCatalog`'un verisidir (R10); panel akışı `0012` ile gelir.
- **X4.** **Galeri logosunun yüklenmesi** — *neden:* logo bir medya varlığıdır ve bu spec
  yükleme yolunu kuruyor; **hangi** varlığın logo olduğu `Dealership`'in verisidir
  (`Gallery.LogoMediaAssetId`, `0003`).
- **X5.** **Toplu yükleme sihirbazı ve sürükle-bırak çoklu yükleme** — *neden:* tek dosya
  yolu doğrulanmadan çoklu akış kurmak, hata durumlarını katlar.
- **X6.** **Medya kütüphanesi ekranı** (yüklenmiş tüm varlıkları gezme, yeniden kullanma) —
  *neden:* araç başına yükleme yeterlidir; kütüphane ayrı bir arayüz işidir.

---

## Context

### Mevcut davranış — doğrulandı

- **`Media` modülünün tamamı tek dosyadır:** `src/Modules/Media/Media.Contracts/Events/MediaEvents.cs`.
  Uygulama projesi yok, `DbContext` yok, migration yok.
- **O dosyada yalnızca bir olay var: `MediaAssetReady`.** `docs/architecture.md` § 1'in olay
  akışı `MediaAssetReady / MediaAssetFailed` diyor — **`MediaAssetFailed` sözleşmesi yoktur.**
  → Açık soru **S3**.
- **Dinleyen var, konuşan yok.** `VehicleCatalog`'un `MediaAssetReadyHandler`'ı kapak
  görselini bu olaydan dolduruyor; olayı yayan hiçbir şey yok. `0003` (şube olayları) ve
  `0011` aynı deseni paylaşıyor.
- **Yayın medyaya kilitli.** `Vehicle.EnsurePublishable()` gövdesi `mediaAssetIds.Count > 0`
  şartından ibaret ve doc comment'i *"A2'nin bu spec'te doğrulanabilen kısmı"* diyor.
- **Mockup ne diyor:** panel yeni ilan ekranı *"Yükleme sırasında her fotoğraf tarayıcıda
  1600 / 800 / 400 piksel WebP olarak küçültülür ve galeri logosu filigran eklenir; R2'ye
  yalnızca bu sürümler yazılır."* → Açık sorular **S1** ve **S2**.
- **Depolama kararı verilmiş:** Karar Defteri "Medya | S3-uyumlu (R2/MinIO) + CDN, ilk günden".

### Geçerli kurallar

**A2** (yayın için en az bir medya varlığı **hazır** olmalı) · `docs/architecture.md` § 1
(`Media` sorumluluğu: varlık kaydı, orijinal + türevler, işleme durumu, filigran/EXIF
politikası, locale başına `alt` metni, depolama yolu; **medya sahipsizdir**) ·
`docs/frontend.md` § 7 (her görselin `alt` metni vardır, medya `alt`'ı locale başına
backend'den).

### Bilinen tuzaklar

1. **Senkron işleme.** Yükleme isteği içinde türev üretmek, büyük dosyada zaman aşımı üretir;
   R6'nın işleme durumu tam bu yüzden var.
2. **Filigranı türevlere değil orijinale basmak.** Orijinal bozulursa politika değiştiğinde
   geri dönülemez (S2).
3. **`alt` metnini tek dilde tutmak.** `docs/frontend.md` § 7 locale başına istiyor; tek dil,
   Arapça vitrinde İngilizce `alt` demektir.
4. **EXIF'i unutmak.** Telefonla çekilen fotoğraf aracın durduğu adresin koordinatını taşır;
   vitrine konulduğunda galerinin stok lokasyonu dışarı sızar.
5. **Başarısız varlığın sessiz kalması.** R8 olmazsa personel fotoğrafın yüklendiğini sanır
   ve araç yayına alınamaz; hata ekranda değil, eksiklikte görünür.

---

## Açık sorular

**S1. Türevler ve filigran nerede üretilir — tarayıcıda mı sunucuda mı?**
Mockup "tarayıcıda küçültülür" diyor.
*Öneri:* **sunucuda.**
*Gerekçe:* filigranı istemcide uygulamak onu **atlanabilir** kılar; "R2'ye yalnızca bu
sürümler yazılır" garantisi istemciye bırakılamaz. Ayrıca türev boyutları politika
değişikliğiyle değişir ve o gün tüm istemcilerin güncel olması beklenemez.
*Bedel:* sunucu CPU'su ve asenkron bir işleme hattı gerekir; yükleme anında görsel hazır
olmaz — R6'nın işleme durumu bu yüzden zorunlu.

**S2. Orijinal dosya saklanır mı?**
Mockup "R2'ye **yalnızca** bu sürümler yazılır" diyor; `docs/architecture.md` § 1 ise
"**orijinal + türevler**" diyor. Çelişki var.
*Öneri:* **orijinal saklanır** (`docs/` kazanır — `AGENTS.md` açılış kuralı).
*Gerekçe:* filigran politikası, boyut kümesi veya kalite ayarı değişirse türevler yeniden
üretilebilir olmalı; orijinal atılırsa o kapı kalıcı olarak kapanır.
*Bedel:* depolama maliyeti belirgin biçimde artar (orijinaller türevlerin toplamından büyük
olur). İsterseniz orijinali "soğuk" saklama sınıfına koymak ayrı bir karardır.

**S3. `MediaAssetFailed` sözleşmesi eklenecek mi?**
Olay akışında yazılı, kodda yok.
*Öneri:* eklenir ve bu spec yayar (R8).
*Gerekçe:* `VehicleCatalog` fotoğrafı bekleyen aracı "eksik bilgi" olarak işaretleyebilsin;
aksi hâlde başarısızlık yalnızca `Media` içinde bilinir.
*Bedel:* yok — tüketicisi bu spec'te zorunlu değil.

**S4. Filigran kaynağı `Media`'ya nasıl verilir?**
Filigran galerinin logosudur ama logo `Dealership`'in verisidir ve C5 `Media`'yı yaprak
modül tutuyor.
*Öneri:* filigran kaynağı **bir medya varlığı kimliği olarak yapılandırmadan** verilir;
`Media`, `Dealership`'i çağırmaz.
*Gerekçe:* C5'i ve bağımlılık haritasını bozmaz. Logo kurulum başına bir kez belirlenen,
neredeyse hiç değişmeyen bir değerdir.
*Bedel:* logo değiştiğinde yapılandırma güncellenir ve **eski türevler eski filigranı
taşımaya devam eder** — yeniden üretim ayrı bir iştir.

**S5. `alt` metnini kim, nerede yazar?**
*Öneri:* bu spec `alt` metni **alanını** tutar (R9) ama panel girişi yapmaz; giriş `0012`
ile, fotoğrafın araca bağlandığı ekranda gelir.
*Gerekçe:* `alt` metni araç bağlamında anlamlı ("beyaz BMW 320i, önden"); medya yükleme
anında personel o bağlamı henüz kurmamıştır.
*Bedel:* `0012` bitene kadar `alt` metni boş kalır ve `docs/frontend.md` § 7'nin
erişilebilirlik tabanı o aralıkta eksik olur. Alternatif: yüklemede zorunlu kılmak —
personeli her fotoğrafta üç dilde metin yazmaya zorlar ve yükleme akışını ağırlaştırır.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. Kriterler en az şu kümeleri kapsayacak:
> yükleme ve reddetme · işleme durumu geçişleri · türev üretimi ve boyutları · filigran ·
> EXIF temizliği · hazır/başarısız duyuruları ve idempotentlik · locale başına `alt` metni ·
> depolama yolu ve CDN · yapılandırma eksikliğinde ayağa kalkmama.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — `Media` başka modülün tablosuna veya deposuna erişmiyor; yaprak
      modül kalıyor.
- [ ] Yeni migration yalnızca `media` şemasına dokunuyor.
- [ ] EXIF temizliğinin gerçekten uygulandığı testle gösterildi.
- [ ] Yapılandırma eksikken uygulamanın ayağa kalkmadığı testle gösterildi (C6).
- [ ] Yeni domain terimi yok — spec yalnızca sözlükteki terimleri kullanır.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra spec kaç kez değişti? |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? |
| **Bulgu gerçek/gürültü oranı** | / | Bulgulardan kaçı gerçek sorundu? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. |

**Bu spec'ten çıkarılan ders:**
