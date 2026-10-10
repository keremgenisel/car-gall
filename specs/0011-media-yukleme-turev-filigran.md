# 0011 — Media: Yükleme, Türev Üretimi ve Filigran

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Media` (yeni — bugün tek bir olay dosyası) |
| **Bağımlı spec'ler** | `0010a` (araç kaydı — Onaylandı). `0012` bu spec'i **bekler**. |
| **Dayanak** | ADR 0003 Ek 1, sıra #4 |
| **Kararlar** | **D1–D17** — § Verilen kararlar. Beşi Analist'in açık sorularının (S1–S5) cevabı, on ikisi uygulama planı turunda çıktı. Takım Yöneticisi onayı **2026-10-10**. |

> **R/C/AC bu PR'da `main`'e giriyor ve orada donuyor** (ADR `0008` / K2, `docs/git.md` § 1).
> Bu commit'ten sonra Requirements, Constraints veya Acceptance Criteria'da yapılan her
> değişiklik **sayılan bir revizyondur** ve kendi commit'ine girer.

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
- **C5.** `Media` **bağımsızdır**: ondan dışarı ok çıkmaz — hiçbir modülü senkron çağırmaz
  ve hiçbir modülün verisini okumaz. **Ona doğru gelen ok meşrudur:** `VehicleCatalog → Media`
  `docs/architecture.md` § 2.A'nın adıyla saydığı örnektir ve § 1'in bağımlılık haritasında
  yazılıdır. `Media`, § 1'in yaprak listesinde (`Content · Notifications · Audit ·
  Engagement`) **yok**. → D6.
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
  → **D3** onu ekliyor.
- **Dinleyen var, konuşan yok.** `VehicleCatalog`'un `MediaAssetReadyHandler`'ı kapak
  görselini bu olaydan dolduruyor; olayı yayan hiçbir şey yok. `0003` (şube olayları) ve
  `0011` aynı deseni paylaşıyor. **Ölçüldü:** o alıcı bugünkü hâliyle olaya *bağlanamaz* —
  imzası olayda bulunmayan bir `vehicleId` istiyor ve dağıtıcıya kayıtlı değil. → **D17**.
- **Yayın medyaya kilitli.** `Vehicle.EnsurePublishable()` gövdesi `mediaAssetIds.Count > 0`
  şartından ibaret ve doc comment'i *"A2'nin bu spec'te doğrulanabilen kısmı"* diyor.
- **Mockup ne diyor:** panel yeni ilan ekranı *"Yükleme sırasında her fotoğraf tarayıcıda
  1600 / 800 / 400 piksel WebP olarak küçültülür ve galeri logosu filigran eklenir; R2'ye
  yalnızca bu sürümler yazılır."* → **D1** ve **D2** ikisini de karara bağlıyor.
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

## Verilen kararlar

On yedi karar. **D1–D5** Analist'in açık sorularının (S1–S5) cevabıdır; soruların metni ve
Analist'in önerisi her kararın içinde korunuyor — gerekçe izi silinmez. **D6–D17** uygulama
planı turunda çıktı ve kodda ölçülerek bulundu. Hepsi Takım Yöneticisi tarafından
**2026-10-10**'da onaylandı. Bundan sonra değişmeleri **spec revizyonudur** (AP-08).

### D1 (S1) — Türevler ve filigran **sunucuda** üretilir

Mockup *"tarayıcıda küçültülür"* diyordu.

*Gerekçe:* Filigranı istemcide uygulamak onu **atlanabilir** kılar; *"R2'ye yalnızca bu
sürümler yazılır"* garantisi istemciye bırakılamaz. Ayrıca türev boyutları politika
değişikliğiyle değişir ve o gün tüm istemcilerin güncel olması beklenemez.

*Bedel:* Sunucu CPU'su ve asenkron bir işleme hattı. Yükleme anında görsel hazır olmaz —
R6'nın işleme durumu bu yüzden zorunlu.

### D2 (S2) — Orijinal dosya **saklanır**

Mockup *"yalnızca bu sürümler yazılır"* diyordu, `docs/architecture.md` § 1 ise
*"orijinal + türevler"*. Çelişkide **`docs/` kazanır** (`AGENTS.md` açılış kuralı).

*Gerekçe:* Filigran politikası, boyut kümesi veya kalite ayarı değişirse türevler yeniden
üretilebilir olmalı; orijinal atılırsa o kapı kalıcı olarak kapanır.

*Bedel:* Depolama maliyeti belirgin biçimde artar. Orijinalin nasıl saklandığı → **D16**.

### D3 (S3) — `MediaAssetFailed` sözleşmesi eklenir ve bu spec onu yayar

Olay akışında yazılıydı, kodda yoktu.

*Gerekçe:* `VehicleCatalog` fotoğrafı bekleyen aracı "eksik bilgi" olarak işaretleyebilsin;
aksi hâlde başarısızlık yalnızca `Media` içinde bilinir.

*Bedel:* Yok — tüketicisi bu spec'te zorunlu değil (bkz. D17).

### D4 (S4 + plan bulgusu) — Filigran kaynağı **yapılandırmadaki bir nesne anahtarıdır**, varlık kimliği değil

Analist'in önerisi *"yapılandırmadan bir medya varlığı kimliği"*ydi. **Elendi:** o kurgu
çözülemeyen bir kurulum döngüsü üretiyor — yapılandırma C6 gereği zorunluysa uygulama ayağa
kalkmaz, kalkmazsa logo yüklenemez, yüklenmezse yapılandırılacak kimlik yoktur.

**Karar:** filigran kaynağı `Media`'nın **kendi** kovasındaki bir nesne anahtarı olarak
verilir (`Media:WatermarkObjectKey`); operatör onu kurulumda kovaya koyar. `Media`,
`Dealership`'i çağırmaz.

*Gerekçe:* Döngüyü kırar, C5'i ve bağımlılık haritasını bozmaz, filigranın yaşam döngüsünü
beslediği hattan ayırır. Başka modülün deposuna da dokunmaz — kova `Media`'nın (C2).

*Bedel:* Logo iki yerde durur: filigran nesnesi, ve vitrinde gösterilecek medya varlığı
(`Gallery.LogoMediaAssetId`, `0003`). Logo değiştiğinde yapılandırma güncellenir ve **eski
türevler eski filigranı taşımaya devam eder** — yeniden üretim ayrı bir iştir.

*Elenen alternatif:* Kimliği önceden üretip logoyu o kimlikle yüklemek ve "kimliği
yapılandırmadakine eşit olan varlık kendi filigranını almaz" kuralını eklemek. İşler, ama
kimlik atamasını istemciye açar.

### D5 (S5) — `alt` metni **alanı** tutulur; girişi `0012`'de gelir

Bu spec `alt` alanını taşır (R9) ama panel girişi yapmaz ve **hiç HTTP yazma ucu açmaz**.

*Gerekçe:* `alt` metni araç bağlamında anlamlı ("beyaz BMW 320i, önden"); yükleme anında
personel o bağlamı henüz kurmamıştır.

*Bedel:* `0012` bitene kadar `alt` metni boş kalır ve `docs/frontend.md` § 7'nin
erişilebilirlik tabanı o aralıkta eksik olur. Alternatif — yüklemede zorunlu kılmak —
personeli her fotoğrafta üç dilde metin yazmaya zorlar.

### D6 — C5 düzeltildi: `Media` yaprak değil, **bağımsız**

Taslaktaki C5 *"hiçbir modül ona senkron bağımlı olmaz"* diyordu. **Ölçüldü: `docs/` bunun
tersini söylüyor.** § 1'in yaprak listesi `Content · Notifications · Audit · Engagement` —
`Media` listede yok; bağımlılık haritası `VehicleCatalog ──► Dealership, VehicleReference,
Media` diyor; § 2.A `VehicleCatalog → Media (görseller hazır mı, URL çöz)` örneğini meşru
senkron çağrı olarak sayıyor.

*Gerekçe:* `AGENTS.md` açılış kuralı — `docs/` kazanır. Taslaktaki hâliyle C5, mimari testi
yazıldığında `VehicleCatalog`'un bugün var olan `Media.Contracts` referansını kırmızı yapardı.

### D7 — Görüntü işleme bir **portun arkasında**; kütüphane lisansı ölçülerek seçilir

`IImageProcessor` portu `Application`'da, uygulaması `Infrastructure`'da.

*Gerekçe:* En yaygın seçenek ImageSharp, v3'ten beri "Six Labors Split License" taşıyor:
açık kaynakta ve cirosu eşiğin altındaki şirkette Apache-2.0, **diğer her kullanımda ticari
lisans**. Bu ürün white-label, galeriye satılan ticari bir kurulum. Lisans bedelini hattın
içine gömmek, ürün satıldığı gün doğan bir yükümlülüktür — ve o gün kütüphaneyi değiştirmek
bütün işleme hattını yeniden yazmak demektir. Port arkasına koymak değişimi tek sınıfa indirir.

*Uygulama:* Lisans metni ve gereken yetenekler (WebP encode, EXIF okuma, yönlendirme)
**kod yazılmadan önce ölçülür**; sonuç PR'da raporlanır. Seçim MIT/Apache-2.0 bir kütüphanedir.

### D8 — EXIF yönlendirmesi **piksele uygulanır**, sonra metadata atılır

Hat sırası: oku → yönlendirmeyi piksele uygula → metadata'sız encode et.

*Gerekçe:* Dikey çekilmiş fotoğrafın dikeyliği piksellerde değil `Orientation` etiketindedir.
Etiketi atıp pikselleri döndürmezsek galeri her dikey fotoğrafı yan görür — yani R5'i
karşılayan ve kullanılamaz bir çıktı üretirdik. Tuzak listesi EXIF'i silmeyi söylüyor;
silmenin kendisi bu ikinci tuzağı üretiyor.

*Sonuç:* AC-20 ayrı bir kriterdir. "EXIF yok" testi yan yatmış bir görselde de yeşil verir.

### D9 — Orijinal **istek içinde** depoya yazılır; uç `202` döner

Doğrula → orijinali depoya yaz → kaydı `Yüklendi` olarak aç → `202` dön.

*Gerekçe:* İşleme asenkron olduğu için (D1) yanıt dönerken orijinalin dayanıklı bir yerde
olması gerekir; süreç o an düşerse dosya kaybolmaz. Orijinal D2 gereği zaten saklanıyor, bu
yüzden onu istek içinde yazmak ek maliyet değil.

*Elenen alternatif:* Baytları Postgres'te tutup sonra taşımak — megabaytları veritabanına
sokar ve aynı dosyayı iki kez saklar.

### D10 — Tür **içerikten** belirlenir; SVG reddedilir; piksel bütçesi vardır

Üçü birlikte: (a) tür sihirli baytlardan tespit edilir, bildirilen `Content-Type`'tan değil
ve **SVG açıkça reddedilir**; (b) boyut sınırı gövde tamponlanmadan uygulanır; (c) **çözülmüş
piksel sayısına** da üst sınır konur. Üçü de yapılandırmadan gelir ve başlangıçta doğrulanır.

*Gerekçe:* (a) `image/jpeg` adıyla gönderilen bir SVG aynı origin'de script çalıştırır; bu
depo o tuzağa `Program.cs`'teki seed ucunda bir kez bakıp yüzeyi kapatmış. (c) 2 MB'lık bir
PNG gigapiksele açılabilir ve sunucu CPU'sunu tek istekle tüketir — boyut sınırı bunu görmez.

### D11 — `MediaAssetReady`'nin **şekli korunur**

Olay tek `Url` taşımaya devam eder; `Url` = **gösterim türevi (800 px)**. Boyut kümesine
ihtiyaç duyan tüketici `Media.Contracts`'ın yeni, dar sorgu yüzeyinden çözer.

*Gerekçe:* Vitrin bugün tek `cover_media_url` kolonu taşıyor ve `srcset` `0016`'nın işi.
Hiçbir şeyin okumadığı bir alan eklemek spekülasyondur; olaya alan eklemek sonradan da
uyumludur.

### D12 — Depolama yolu düzeni ve erişim taban adresi

`public/assets/{assetId}/{variant}.webp` ve `private/originals/{assetId}.{ext}`. Erişim yolu
`Media:PublicBaseUrl` + anahtar olarak üretilir; CDN yalnız `public/` önekine bağlanır.

*Gerekçe:* CDN gerçek bir altyapı kararıdır (R2 kovası + alan adı) ve kodda tek bir taban
adresi olarak görünür; onu test edilebilir kılan tek şey budur. Önek ayrımı D16 ile anlam
kazanıyor.

### D13 — `İşleme Durumu` sözlüğe girer

R6 sözlükte olmayan bir terim getiriyor: `docs/domain.md` § 1'de `Medya Varlığı` ve
`Kapak Görseli` var, **işleme durumu yok**. Bu PR sözlüğe bir satır ekler —
`İşleme Durumu | MediaProcessingStatus | Yüklendi · İşleniyor · Hazır · Başarısız` — ve
geçişleri A3'ün ilan durumları gibi yazar.

*Gerekçe:* `docs/conventions.md` § 2 açık: *"Sözlükte olmayan yeni bir terim koda girmeden
önce sözlüğe girer."* DoD'un *"yeni domain terimi yok"* kutusunu işaretlemek yanlış olurdu;
terim var, yeri belli.

### D14 — Kuyruğu arka plan servisi işler; tur varlığı **durum geçişiyle sahiplenir**

`NotificationQueueHostedService` deseninin eşi. Deneme sayısı ve geri çekilme aralığı
yapılandırmadan gelir. Bir tur bir varlığı `Yüklendi → İşleniyor` geçişiyle sahiplenir ve o
geçiş **kendi transaction'ındadır**.

*Gerekçe:* Sahiplenme olmadan iki tur aynı varlığı işler ve iki kez duyurur. Geçişi işin
başına koymak R6'nın durumunu aynı zamanda bir kilit yapıyor — ayrı bir kilit mekanizması
gerekmez.

### D15 — Yükleme ucuna **hız sınırı konmaz**

*Gerekçe:* Uç oturum arkasında ve kullanıcısı galeri personeli. D10'un piksel bütçesi asıl
CPU riskini zaten kesiyor. Oturumlu bir uca sınır koymak personelin toplu yüklemesini (X5,
sonraki iş) baştan engeller ve kapsamı genişletir.

### D16 — Orijinal **ham** saklanır ama `private/` önekinde durur ve hiçbir yanıtta yolu dönmez

D2 orijinali yeniden üretim için tutuyor; R5 konum bilgisini temizliyor. İkisi birlikte şunu
gerektiriyor: ham bayt korunur, ama dışarı çıkan hiçbir şey onu göstermez. CDN `private/`
önekine bağlanmaz ve API orijinalin yolunu hiçbir yerde döndürmez.

*Gerekçe:* R5'in koruduğu şey dışarı **çıkanı** temizlemek. Ham baytı bozmak D2'nin açtığı
kapıyı yarı kapatır: kalite ayarı değişirse yeniden üretim artık kayıplı bir kaynaktan olur.

*Bedel — açıkça kabul ediliyor:* Aracın durduğu adresin koordinatı bizim depomuzda yaşar ve
bir silme talebi orijinalleri de kapsamak zorundadır.

### D17 — `0011` `VehicleCatalog`'a **hiç dokunmaz**

Olay yayılır; alıcısı `0012`'de kablolanır.

**Ölçüm:** `VehicleCatalog`'un `MediaAssetReadyHandler`'ı bugün olaya **bağlanamaz** hâlde —
imzası `HandleAsync(notification, vehicleId, ct)` ve `vehicleId` olayda yok; sınıf
`IIntegrationEventDispatchTarget` olarak da kayıtlı değil.

*Gerekçe:* Alıcıyı şimdi olaya bağlamak "bu varlık hangi araçta" sorgusunu gerektirir ve o
sorgunun cevabı, fotoğrafı araca bağlayan `0012` gelmeden **her zaman boş**. X3 bu bağlamayı
zaten kapsam dışı sayıyor.

*Doğrulandı:* Hedefi olmayan bir olay dağıtıcıda `failures.Count == 0` dalına girip
`ProcessedAt` alıyor — yani bekleyen kuyruk birikmiyor. `0003` bugün tam olarak bu şekilde
duruyor: dinleyen var, konuşan yok; burada tersi olacak.

---

## Acceptance Criteria

Otuz beş kriter. Spec'in taslağında sayılan dokuz kapsamı (yükleme ve reddetme · işleme
durumu geçişleri · türev üretimi ve boyutları · filigran · EXIF temizliği · hazır/başarısız
duyuruları ve idempotentlik · locale başına `alt` metni · depolama yolu ve CDN ·
yapılandırma eksikliğinde ayağa kalkmama) ve D1–D17'yi kapsar.

### A. Yükleme ve reddetme

- **AC-1.** Oturum açmış personel bir görsel yükler; yanıt varlığın kimliğini ve işleme durumunu taşır.
- **AC-2.** Oturumu olmayan biri görsel yükleyemez; istek reddedilir.
- **AC-3.** Desteklenmeyen türdeki bir dosya reddedilir ve varlık kaydı oluşmaz.
- **AC-4.** Dosyanın türü içeriğinden belirlenir; `image/jpeg` adıyla gönderilen bir SVG reddedilir.
- **AC-5.** Yapılandırılan boyut sınırını aşan dosya reddedilir ve varlık kaydı oluşmaz.
- **AC-6.** Çözülmüş piksel bütçesini aşan dosya reddedilir ve varlık kaydı oluşmaz.
- **AC-7.** Reddedilen istek çevrilmiş metin değil `errorCode` döner.

### B. İşleme durumu

- **AC-8.** Yüklenen varlık `Yüklendi` durumunda, depolama yolu ve kimliğiyle kaydedilir.
- **AC-9.** İşleme başladığında durum `İşleniyor`, başarıyla bittiğinde `Hazır` olur.
- **AC-10.** Tanımlı olmayan durum geçişi yapılamaz.
- **AC-11.** Denemeler tükendiğinde durum `Başarısız` olur; varlık `Yüklendi` durumunda kalmaz.
- **AC-12.** Bir işleme turu, başka bir turun sahiplendiği varlığı yeniden işlemez.

### C. Türev üretimi

- **AC-13.** Hazır varlık 1600 / 800 / 400 piksel genişlikte üç WebP türev taşır.
- **AC-14.** Kaynak görsel bir türev genişliğinden küçükse o türev kaynaktan büyütülmez.
- **AC-15.** Her türev kendi depolama yolunda durur ve yol varlığın kimliğinden türer.

### D. Filigran

- **AC-16.** Üç türevin hepsinde filigran bulunur.
- **AC-17.** Filigran kaynağı yapılandırmadan gelir; `Media` bunun için başka bir modülü çağırmaz.
- **AC-18.** Filigran kaynağı erişilemezse varlık `Başarısız` olur; filigransız türev üretilmez.

### E. EXIF

- **AC-19.** Yüklenen görselin konum ve cihaz bilgisi türevlerde bulunmaz.
- **AC-20.** EXIF yönlendirme etiketi taşıyan görselin türevi doğru yönde üretilir.
- **AC-21.** Orijinal dosya işleme bittikten sonra da depoda durur ve public yolda bulunmaz.

### F. Duyurular ve idempotentlik

- **AC-22.** Varlık hazır olduğunda `MediaAssetReady` yayılır; duyuru varlığın kimliğini ve erişim yolunu taşır.
- **AC-23.** Duyuru, iş verisiyle aynı transaction'da outbox'a yazılır.
- **AC-24.** Aynı varlık için ikinci bir duyuru oluşmaz.
- **AC-25.** İşleme başarısız olduğunda `MediaAssetFailed` yayılır.
- **AC-26.** `MediaAssetReady` ve `MediaAssetFailed`'in şekli kırılmaz.

### G. `alt` metni

- **AC-27.** Bir varlık etkin her locale için ayrı `alt` metni taşıyabilir.
- **AC-28.** `alt` metni boş olan bir varlık yine hazır olabilir.

### H. Depolama ve CDN

- **AC-29.** Türevin erişim yolu yapılandırılan taban adresten üretilir.
- **AC-30.** Türev, S3 uyumlu sağlayıcıya yazılır ve oradan okunabilir.

### I. Yapılandırma ve sınır

- **AC-31.** S3 kimlik bilgileri eksikken uygulama ayağa kalkmaz.
- **AC-32.** Erişim taban adresi eksikken uygulama ayağa kalkmaz.
- **AC-33.** Filigran kaynağı yapılandırılmamışken uygulama ayağa kalkmaz.
- **AC-34.** `media` migration'ı yalnızca `media` şemasına dokunur.
- **AC-35.** `Media`'dan başka bir modüle referans çıkmaz; `Media` başka modülün tablosuna veya dosya deposuna erişmez.

---

## Definition of Done

- [x] Açık sorular karara bağlandı ve § **Verilen kararlar** olarak işlendi (D1–D17).
- [x] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`) — dokuz projede **867 test, 0 kırmızı**.
- [x] <!-- DoD:T4 --> Mimari testler yeşil — `Media` başka modülün tablosuna veya deposuna erişmiyor;
      **bağımsız** kalıyor (taslaktaki "yaprak" ifadesi D6 ile düzeltildi: `docs/`'un yaprak
      listesinde `Media` yok ve § 2.A `VehicleCatalog → Media` okumasını meşru sayıyor).
      `MedyaSinirTests` beş iddia, dördü sondayla kırmızıya döndürülerek doğrulandı.
- [x] <!-- DoD:T5 --> Yeni migration yalnızca `media` şemasına dokunuyor (`MigrationSchemaTests` doğruluyor).
- [x] EXIF temizliğinin gerçekten uygulandığı testle gösterildi — AC-19, **bağımsız** bir
      okuyucuyla (MetadataExtractor; üreten kütüphane SkiaSharp). Test boş yere yeşil
      değil: girdide GPS ve cihaz bilgisinin VAR olduğunu da iddia ediyor ve fixture EXIF
      yazmayı bıraktığında kırmızı veriyor (sondayla doğrulandı). AC-20 ayrıca yönün
      doğruluğunu ölçüyor — "EXIF yok" testi yan yatmış bir görselde de yeşil verir (D8).
- [x] Yapılandırma eksikken uygulamanın ayağa kalkmadığı testle gösterildi (C6) —
      `MedyaYapilandirmasiTests`, GERÇEK Host başlangıcında, yedi anahtar için.
- [x] <!-- DoD:T7 --> **Bir yeni terim vardı ve sözlüğe girdi:** R6'nın getirdiği `İşleme Durumu`
      (`MediaProcessingStatus`). `docs/domain.md` § 1'e eklendi, geçişleri § 2.10'a (M1–M3)
      yazıldı. Kutuyu "yeni terim yok" diye işaretlemek yanlış olurdu — `docs/conventions.md`
      § 2 terimin koda girmeden önce sözlüğe girmesini istiyor (D13).
- [x] <!-- DoD:T8 --> `docs/architecture.md` § Karar Defteri'ne **üç satır** eklendi (medya işleme
      yeri, filigran kaynağı, orijinal saklama) ve gerekçeleri ADR `0009`'da.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] <!-- DoD:T2 --> **Yok — arayüz kriteri yok.** Bu spec hiçbir ekran üretmiyor: X5/X6 toplu
      yükleme sihirbazını ve medya kütüphanesi ekranını, D5 `alt` metni girişini kapsam
      dışı bırakıyor; panel yükleme akışı `0012`'nin işi. 35 kriterin hiçbiri arayüz
      kriteri değil, bu yüzden görsel kanıt hiçbir kriteri kapısız bırakmıyor.
- [x] <!-- DoD:T6 --> **Yok — kullanıcıya görünen metin üretilmedi.** API `errorCode` döner (C7) ve
      durum bir kod olarak geçer (`uploaded`); çeviri uçta, `0012`'nin ekranıyla gelir.
      `frontend/` altında tek dosya değişmedi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

<!-- Biçim `specs/TEMPLATE.md` § SCORECARD'dan; tanımlar ADR 0008 / K1, K3, K7. -->

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | | R/C/AC implementasyon commit'inden önce `main`'de miydi (K2)? |
| **Kriter sayısı: önce → bugün** | → | Implementasyon commit'inin ebeveyninde kaç kriter vardı, bugün kaç? |
| **Spec revizyon sayısı** | | Kriterler `main`'e girdikten SONRA R/C/AC kaç kez değişti? Onay turundaki CLARIFY sayılır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? `0` yalnız dış gözden geçirme kutusu `[x]` ise. |
| **Dış bulgu (gerçek/gürültü)** | / | Dış gözden geçirmede çıkan bulgular. Yapılmadıysa `— (dış gözden geçirme yok)`. |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den SONRA bulunan hata. Arşive taşınırken `izleniyor` yazılır; `0` ve boş hücre yasak. |

**Bu spec'ten çıkarılan ders:**
