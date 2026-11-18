# 0016 — Medya Galerisi ve Parça Haritası

| | |
|---|---|
| **Durum** | Tamamlandı (2026-11-18) — kriterler #92 → `970e14f`, uygulama #94 → `dec382e` |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin · `frontend/` panel (opsiyonel alan formu — **D5**) · `VehicleCatalog` (parça haritası alanı, vitrin okuma modeli, detay sözleşmesi) · `Media` **tüketilir**, değişmez (**D17**) |
| **Bağımlı spec'ler** | `0002` (ilan detayı — tamamlandı; **X2** ve **X3**'ü geri açar) · `0011` (Media — tamamlandı) · `0012a` (aracın medya yüzeyi — tamamlandı) · `0010b` (opsiyonel alan tipleri — **X22**'nin bir kısmını geri açar, **D5**) · `0010a` (**X4**'ün veri yarısını üstlenir, **D5**) |
| **Dayanak** | ADR 0003 unsur 1 ve Ek 1, sıra #10 |
| **Karar bekliyor** | yok — S1–S4 → **D1–D4**; QA CLARIFY turunun on dokuz bulgusu (B1–B19) → **D5–D23**; plan turunun sekiz bulgusu (P1–P8) → **D24–D31**; kriter turunun dört sorusu S5–S8 → **D33–D36** (2026-11-18). |

> **Bölünme (2026-11-18, D31).** Bu dosya `0016`'nın **vitrin** dilimidir; veri ve panel
> **`0016a`**'ya ayrıldı (R8–R12, C11 ve C10'un kod tablosu). Numaralar korundu. `0016`'nın
> tam Context'i ve D1–D31 burada kalır.
>
> **Plan turu (2026-11-18, `8a22fb6` üzerinde).** Developer uygulama planını çıkarırken
> sekiz bulgu getirdi (P1–P8); önerilerin **tamamı** Takım Yöneticisi (depo sahibi)
> tarafından onaylandı ve **D24–D31** olarak işlendi. En büyüğü **D31**: kart iki dilime
> bölünür — `0016a` (parça haritası: veri ve panel) ve `0016b` (vitrin: galeri ve şema).
> Bölme ayrı bir `docs/` PR'ında yapılır; kriterler o dilimlerde ve **Analist** tarafından
> yazılır (**D24**). Context'in K1, K5, K6, K7 ölçümleri plan turunda kodla yeniden
> doğrulandı ve tutuyor.
>
> **QA CLARIFY turu (2026-11-18, `main` @ `aaf0cec`).** QA spec'i bugünkü koda karşı okudu
> ve on dokuz bulgu getirdi (B1–B19); önerilerin **tamamı** Takım Yöneticisi (depo sahibi)
> tarafından onaylandı ve **D5–D23** olarak işlendi. En büyüğü **B1**: parça haritası alanı
> kodda **hiç yok** — ne değeri, ne tipi, ne giriş yolu. Taslağın Context'i ve S4'ü verinin
> var olduğunu varsayıyordu; spec bu yüzden veri tarafını da üstlendi (**D5**). Kabul
> kriterleri kriter turunda (2026-11-18, **Analist**, **D24**) yazıldı; o turun soruları S5–S8.

---

## Intent

İlan detayı bugün **tek bir kapak görseli** gösteriyor. `0002` galeriyi (§ X2) ve parça
haritasının görsel şemasını (§ X3) bilinçli olarak dışarıda bıraktı. Parça haritasının
**değeri** ise bugün hiçbir yerde tutulmuyor (**D5**): alan kodda tanımlı değil.

İkinci el araç almak, insanın **fotoğrafa ve hasar geçmişine** baktığı bir iştir. Tek
görselle verilen karar, galeriyi aramakla sonuçlanmıyor — ziyaretçi yeterince bilgilenmeden
sayfadan çıkıyor. Hasar bilgisi düz liste olarak verildiğinde ise okunmuyor: "sağ ön çamurluk
boyalı, bagaj kapağı boyalı, sol arka kapı lokal boyalı" cümlesi, bir şemaya tek bakışta
eşdeğer değil.

**Galeri** bunu istiyor çünkü şeffaflık onun satış iddiası — mockup'ın giriş metni bile
*"Geçmişi gizlenmeyen araçlar"* diyor. Saklanmayan hasar, ancak **görülebildiğinde**
şeffaflık olur.

Bu iş bittiğinde galeri bir aracın 13 parçasının durumunu panelde girebilir; ziyaretçi
aracın tüm fotoğraflarını gezebilir ve hasar durumunu araç üzerinde bir şema olarak görür.
Başarı ölçütü: **ziyaretçi, aracın hangi parçasına ne olduğunu tek bakışta anlıyor.**

---

## Requirements

### Galeri

- **R1.** İlan detayında ziyaretçi, ilanın **`hazır` durumdaki tüm görsellerini** aracın
  medya sırasıyla gezebilir. `işleniyor` ve `başarısız` varlıklar galeride yer almaz
  (**D14**).
- **R2.** Kaçıncı görselde olduğu ve toplam görsel sayısı gösterilir; toplam yalnız R1'in
  gösterdiği görselleri sayar. Tek görselde sayaç gösterilmez (**D15**).
- **R3.** Ziyaretçi görseller arasında **düğmelerle, klavyeyle ve dokunarak kaydırmayla**
  gezebilir (**D16**):
  - İlk görselden geriye ve son görselden ileriye **dönülmez**; uçtaki düğme devre dışı
    kalır ve odak kaybolmaz.
  - Ok tuşları ve kaydırma **okuma yönünü** izler: Arapça'da ← tuşu *sonraki* görseldir.
    Kaydırma yönü **parmağın hareketiyle** tanımlanır: TR/EN'de sağdan sola, AR'da soldan
    sağa kaydırma *sonraki* görsele geçer (**D33**). *(2026-11-18: "Arapça'da sola kaydırma
    sonraki" yazıyordu; parmak hareketi olarak okununca okuma yönü ilkesiyle çelişiyordu.)*
  - Her etkileşimli öğe klavyeyle erişilebilir ve odak halkası görünür.
    → `docs/frontend.md` § 7.
- **R4.** Ziyaretçi bir görseli **büyütebilir** (**D16**):
  - Büyütülmüş görünüm `large` türevini kullanır ve içinde de R3'ün gezinmesi çalışır.
  - Esc ve kapatma düğmesi görünümü kapatır.
  - Görünüm açıkken odak içeride tutulur, arka plan etkileşimsizdir; kapanınca odak
    görünümü açan öğeye döner.
  - Seçili görsel **adrese yazılmaz** ve tarayıcı geçmişine giriş eklenmez; sayfa yeniden
    yüklenince galeri ilk görselden başlar.
- **R5.** Her görselin `alt` metni ziyaretçinin locale'inde **şablondan** üretilir: model,
  yıl, görselin sırası ve toplam (ör. *"Toyota Corolla 2019 — görsel 3/18"*). Sunucudan
  çevrilmiş metin gelmez (**D17**). → `0002` AC-25, `docs/frontend.md` § 7.
- **R6.** Görseller türevlerden yüklenir (**D18**, **D19**):
  - Satır içi görsel `srcset` ile `thumbnail` / `display` / `large` türevlerinden seçilir.
  - Her görsel, türevin **gerçek** `width`/`height` değeriyle basılır.
  - Galeri çerçevesi sabit **4:3** oranındadır; farklı en-boy oranlı görsel çerçeveye
    sığdırılır, sayfa düzenini kaydırmaz.
  - **İlk görsel** `eager` ve yüksek öncelikle yüklenir (LCP adayı); diğerleri `lazy`.
    Etkileşimden önce yalnız ilk görsel indirilir.
- **R7.** Detayın **ilk görseli, ilk `hazır` görseldir**. Sıranın ilk elemanı henüz hazır
  değilse veya başarısızsa detay bir sonraki hazır görselle açılır (**D14**). Liste kartının
  kapak davranışı **değişmez** (→ X10).

### Parça haritası — veri ve panel

- **R8–R12.** *(Bölündü, 2026-11-18 — **D31**.)* Parça haritasının değeri, değer tipi ve paneli **`0016a`**'dadır. Bu dilim `0016a`'nın sözleşmesine dayanır.

### Parça haritası — vitrin

- **R13.** İlan detayı, alan **açık, public ve değerli** ise aracın hasar durumunu **araç
  üzerinde bir şema** olarak gösterir (**D2**, **D9**).
- **R14.** Alan **kapalı veya gizliyse** ya da **değer girilmemişse** bölüm hiç basılmaz ve
  yanıtta bulunmaz — boş değer olarak da bulunmaz (**D8**, **D9**). → **F3**, Yasak
  Listesi 41.
- **R15.** 13 parçanın tamamı **orijinal** ise şema basılır ve yanında *"13 parçanın tamamı
  orijinal"* cümlesi bulunur (**D8**). Değerin olmaması ile tamamının orijinal olması
  ziyaretçiye **aynı gösterilmez**.
- **R16.** Şema **etkileşimsizdir** (**D11**):
  - `role="img"` taşır; erişilebilir adı okunabilir listeye (R17) işaret eden bir özettir.
  - Parçaların hiçbiri Tab durağı değildir.
- **R17.** Şemanın yanında haritanın **okunabilir listesi** bulunur (**D12**):
  - **Orijinal olmayan** parçaları C10'un sırasıyla, durumlarıyla yazar.
  - Ardından *"diğer N parça orijinal"* cümlesi gelir.
  - R15'in tamamı-orijinal durumunda listenin yerini R15'in cümlesi alır.
  - Liste parça durumunun **metin karşılığıdır**; şema anlaşılmasa da bilgi erişilebilir
    kalır. → `docs/frontend.md` § 7.
- **R18.** Şemanın yanında iki not bulunur (**D12**):
  - *"Galeri beyanı"*.
  - *"Diyagramın üstü aracın ön kısmıdır"*.
- **R19.** Şemada dört durum **renge ek olarak dolgu deseniyle** ayrılır ve bir **lejant**
  her zaman görünür (**D13**). → C2.

### Durumlar ve locale

- **R20.** Galeri ve şemanın **ayrı bir yükleme adımı yoktur**; ikisi de detay yanıtıyla
  gelir. Sayfa düzeyindeki yükleniyor, bulunamadı ve hata durumları `0002` R9'dan gelir
  (**D20**). Ek olarak:
  - **Sıfır görsel:** bugünkü davranış korunur — görsel alanı, sayaç ve gezinme düğmesi
    yoktur (**D15**).
  - **Tek görsel:** görsel ve büyütme vardır; sayaç ve gezinme düğmesi yoktur (**D15**).
  - **Tek bir görsel yüklenemezse:** o yuvada yer tutucu gösterilir; sayaç ve gezinme
    çalışmaya devam eder (**D20**).
- **R21.** Üç locale'de çalışır; Arapça'da RTL (**D10**):
  - Lejant, liste, notlar ve galeri denetimlerinin yerleşimi aynalanır.
  - **Araç çizimi aynalanmaz**: şema fiziksel bir üstten görünümdür ve aracın sol tarafı
    her locale'de çizimin sol tarafındadır.

---

## Constraints

### Uyulması zorunlu

- **C1.** Gizli, kapalı veya değersiz alan yanıtta **hiç bulunmaz**; eleme yansıtma veya
  sorgu tarafında uygulanır. Detay yanıtındaki parça haritası özelliğinin **JSON anahtarı**
  bu durumda yazılmaz — `null` olarak da yazılmaz (**D9**). → **F3**, Yasak Listesi 41,
  `0002` AC-12.
- **C2.** Renk **tek başına anlam taşımaz**. Dört durum dolgu deseniyle de ayrılır: boş,
  seyrek tarama, yoğun tarama, çapraz tarama (**D13**). Her durumun zemine karşı kontrastı
  **en az 3:1**'dir (arayüz öğesi eşiği). → `docs/frontend.md` § 7.
- **C3.** Şemanın renkleri `frontend-002`'nin **token katmanından** gelir; gömülü hex
  yoktur. → `docs/frontend.md` § 4.1.
- **C4.** CSS'te fiziksel yön özellikleri kullanılmaz. Bu kural **metin akışı** içindir:
  araç çiziminin kendisi aynalanmaz (R21, **D10**). → Yasak Listesi 25.
- **C5.** Kullanıcıya görünen metin koda gömülmez. Bunlar çeviri kataloğundan gelir:
  - 13 parça adı ve dört durum adı;
  - R15, R17 ve R18'in cümleleri;
  - lejant;
  - R5'in `alt` şablonu.

  → Altın Kural 6.
- **C6.** Veritabanında ve yanıtta çevrilmiş parça ya da durum adı bulunmaz; **kod**
  bulunur. → Yasak Listesi 22, 23.
- **C7.** Görseller `Media`'nın türevlerinden gelir; orijinal vitrine servis edilmez.
  → `0011` R3.
- **C8.** `0002`'nin mevcut kabul kriterleri **regresyon olarak** korunur — C12'nin
  değiştirdiği sözleşme alanı hariç. → ADR 0003 unsur 1.
- **C9.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.
- **C10.** 13 parçanın **kodları ve sırası** sabittir (**D6**); tablonun **tek evi `0016a` C10**'dur (**D31**). Okunabilir listenin sırası o tablonun sırasıdır.
- **C11.** *(Bölündü — **D31**.)* Yeni değer tipi ve ADR 0007 eki **`0016a`**'dadır.
- **C12.** Detay sözleşmesi değişir (**D9**, **D22**):
  - `CoverImageUrl` **kaldırılır**; yerine **sıralı görsel listesi** gelir. Her eleman üç
    türevin adresini ve gerçek boyutlarını taşır.
  - Parça haritası **ayrı, yapılandırılmış bir özellik** olarak gelir: yalnız kod taşıyan
    `[{part, state}]` listesi. Aynı veri `Attributes` içinde **tekrar edilmez**.
  - Liste kartının sözleşmesi (`0001`) **değişmez**.
- **C13.** Detay sayfası Lighthouse koşusuna eklenir ve `0001` **C3**'ün mobil eşikleriyle
  ölçülür (**D19**). → ADR 0010 (CI dakika bütçesi).

### KAPSAM DIŞI

- **X1.** **Video ve 360° tur** — *neden:* `0011` § X2 bunları üretmiyor; oynatıcı ayrı bir
  iştir.
- **X2.** **Panelde tıkla-seç parça haritası editörü** (şema üzerinde parçaya tıklayarak
  durum seçme) — *neden:* veri girişi R12'nin 13 seçim kutusuyla bu spec'te yapılıyor
  (**D5**); şema üzerinde düzenleme ayrı bir etkileşim işidir (**D4**).
- **X3.** **Ekspertiz raporu dosyasının görüntülenmesi** — *neden:* dosya opsiyonel bir
  alandır ve PDF görüntüleme ayrı bir karardır.
- **X4.** **Görsel karşılaştırma / yan yana görünüm** — *neden:* karşılaştırma sıra #12'nin
  işi.
- **X5.** **Tramer tutarının grafikle gösterimi** — *neden:* tutar bir sayıdır; `0002` onu
  R8 kapsamında, alan public ise zaten gösteriyor ve yeri değişmez (**D21**).
- **X6.** **Görsellerin indirilmesi veya paylaşılması** — *neden:* filigran politikası
  (`0011` R4) kopyalamayı zorlaştırmak içindir; indirme düğmesi onunla çelişir.
- **X7.** **`Media`'nın locale'li `alt` metninin vitrine taşınması** — *neden:* alt metin
  vitrin okuma modelinde yok ve değiştiğinde bir olay yayımlanmıyor; taşımak modüller arası
  yeni bir olay ister (**D17**).
- **X8.** **Ağır hasar kaydı** ve **değişen parça sayısı** alanları — *neden:* ikisi de
  kodda yok. Değişen parça sayısı ileride eklenirse ayrı bir alan **olmaz**, bu haritadan
  **türetilir** — aksi hâlde aynı bilginin iki kaynağı olur (**D21**).
- **X9.** **Galeri üst sınırı** — *neden:* bu spec görsel sayısına sınır koymaz; R6'nın
  gecikmeli yüklemesi 30 görsellik ilanla ölçülür (**D23**).
- **X10.** **Liste kartının kapak kuralı** ve **yayındaki aracın tüm görsellerini
  kaybedebilmesi** — *neden:* ikisi de `0012a`'nın davranışıdır (D14, sırası boşaltılabilen
  medya listesi), bu spec'in değil. Ayrı kart olarak önerildi (**D14**, **D15**).
- **X11.** **Galeri küçük resim şeridi** — *neden:* mockup yalnız sayaç gösteriyor; şerit
  ayrı bir yerleşim ve erişilebilirlik işidir.

---

## Context

### Mevcut davranış — yeniden doğrulandı (2026-11-18, `main` @ `aaf0cec`)

- **`0002` tamamlandı** ve iki şeyi açıkça dışarıda bıraktı:
  - § X2: *"Medya galerisi, görsel büyütme, video ve 360° tur"*.
  - § X3: *"Boyalı–değişen parça haritasının görsel şeması (13 parça)"*.
- **Detay sözleşmesi bugün tek görsel taşıyor.** `ShowcaseListingDetailDto` alanları:
  `Slug`, `IsReserved`, `CoverImageUrl`, `ModelName`, `ModelYear`, `MileageKilometers`,
  `Price`, `BranchName`, `Attributes`. Görsel listesi yok.
- **Parça haritası alanı kodda YOK** (QA ölçümü K1). Taslağın *"parça haritası bugün büyük
  olasılıkla bu düz listede görünüyor"* cümlesi yanlıştı. Kodda tanımlı tek opsiyonel alan
  tramer (`damage_record_amount`).
- **`Media` var** (`0011` tamamlandı) ve üç türev üretiyor: `large` 1600, `display` 800,
  `thumbnail` 400. `MediaAssetReady` her türevin gerçek boyutunu taşıyor.
  `0012a` aracın sıralı medya listesini kuruyor; kapak listenin ilk elemanı.
- **Mockup ne gösteriyor:**
  - Detayda "1 / 18" sayacı ve `[ARAÇ FOTOĞRAFI]` yer tutucusu.
  - Altında 13 parça adıyla bir şema: *"Galeri beyanı · diyagramın üstü aracın ön
    kısmıdır"*.
  - Tramer tutarı ve ağır hasar bayrağı.
  - Tasarımcı notu `s1`: *"üstten araç görünümü: 13 kaporta parçası kendi biçiminde,
    aralarında camlar, dışarıda tekerlek ve aynalar. **Dört durum: orijinal, lokal boyalı,
    boyalı, değişen.**"*
- **Çelişki — çözüldü (D1):** `docs/domain.md` **Parça Haritası**'nı üç durumla tanımlıyordu,
  mockup dört diyordu. Sözlük dört duruma güncellenecek.

### QA CLARIFY turu ölçümü (2026-11-18)

- **K1 — alan yok.**
  - `VehicleFieldKeys` yalnız `body_type` ve `damage_record_amount` tanımlıyor.
    `VehicleFieldDefinitions` yalnız tramer'i içeriyor.
  - `FieldValueKind` kapalı bir küme (ADR 0007) ve harita tipi içermiyor.
  - `0010a` X4 *"alanın değeri R9 kapsamında girilebilir"* diyordu; `0010b` X22 liste
    tiplerini kapsam dışı bıraktı. Sonuç: **alanın hiçbir spec'te sahibi yoktu** → **D5**.
- **K2 — kapak ve hazır olmayan varlık.**
  - `0012a` D14 `işleniyor` ve `başarısız` varlıkların da bağlanmasına izin veriyor.
  - `ShowcaseProjectionWriter.KapakAdresiAsync` sıranın ilk elemanı hazır değilse `null`
    dönüyor.
  - Bugün sıranın ilki başarısız, ikincisi hazır olan bir araç vitrine **görselsiz**
    çıkıyor → **D14**.
- **K3 — sıfır görselli yayında ilan mümkün.** `UpdateVehicleMediaCommand` yayındaki bir
  aracın medya listesini boşaltmayı engellemiyor (yayına alma şartı yalnız yayın anında
  bakılıyor) → **D15**, X10.
- **K4 — `alt` metni.**
  - Detay sayfası `alt` metnini şablondan üretiyor (`ListingDetailPage.tsx:103`).
  - `Media`'nın locale'li alt metni vitrin okuma modeline taşınmıyor.
  - `Media.Contracts/Events` yalnız `MediaAssetReady` ve `MediaAssetFailed` taşıyor; alt
    metin değişikliği duyurulmuyor → **D17**.
- **K5 — boyutlar.**
  - Okuma modeli yalnız `CoverMediaUrl` tutuyor; türev boyutu yok.
  - Detay sayfası boyutu `960×720` olarak sabit yazıyor, oysa `display` türevi 800 piksel
    genişliğinde ve yüksekliği fotoğrafa göre değişiyor → **D18**.
- **K6 — detayın LCP kapısı yok.**
  - `frontend/scripts/lighthouse-run.mjs` yalnız `/tr/` ve `/tr/cars`'ı ölçüyor.
  - `0002`'de LCP eşiği yok; taslağın Tuzak 4'ü var olmayan bir eşiğe atıf yapıyordu
    → **D19**.
- **K7 — `CoverImageUrl`'ün tüketicileri.**
  - Liste kartı kendi liste DTO'sunu kullanıyor (`frontend/src/shared/api/types.ts:15`).
  - Detaydaki alanın tek tüketicisi `ListingDetailPage`.
  - Taslağın S3'teki *"`0001`'in kart yanıtı onu kullanıyor"* gerekçesi detay DTO'su için
    geçerli değil → **D22**.

### Geçerli kurallar

- **Parça Haritası** (`PaintChangeMap`, opsiyonel alan)
- **Tramer** (`DamageRecordAmount`, opsiyonel)
- **F1**: kapatmak silmek değildir.
- **F3/F4**: gizli alan yanıtta yer almaz.
- **F5**: alan listesi kodda tanımlıdır.
- ADR 0007: kapalı tip kümesi; alan → tip eşlemesi kodda.
- `docs/architecture.md` § 4: opsiyonel alan listesi; iki eksen, form durumu ve public
  görünürlük.
- `docs/frontend.md` § 3 ve § 7.

### Bilinen tuzaklar

1. **Şemayı tek bilgi kaynağı yapmak.** Ekran okuyucuyla gezilirse veya şema anlaşılmazsa
   hasar bilgisi kaybolur; R17'nin listesi bu yüzden zorunlu.
2. **Durumları yalnızca renkle ayırmak.** Dört durumu yalnızca renk tonuyla göstermek § 7'yi
   kırar ve renk körü ziyaretçi için şemayı anlamsız yapar (C2, R19).
3. **Araç çizimini aynalamak.** *(D10 ile tersine çevrildi.)* Taslak RTL'de silüetin
   aynalanmasını istiyordu. Şema fiziksel bir üstten görünüm olduğu için aynalanırsa
   "sol ön çamurluk" çizimin sağına düşer. Aynalanan, metin akışıdır (R21, C4).
4. **İlk görseli gecikmeli yüklemeye düşürmek.** İlk görsel LCP adayıdır; galeriye geçerken
   onu `lazy` yapmak LCP'yi bozar. *(Taslak bunu `0002`'nin eşiğine bağlıyordu; o eşik yok
   — K6. Ölçüm C13 ile gelir.)*
5. **Parça adlarını koda gömmek.** 13 parça adı üç dilde çevrilmeli; parça ve durum kodu
   sabit anahtardır, çeviri katalogdadır (C5, C6).
6. **Sessizliği beyana çevirmek.** Panelde "orijinal"i varsayılan seçmek ya da değersiz alanı
   "hasarsız" göstermek, galerinin hiçbir şey söylemediği yerde bir iddia üretir (R11,
   R12, R15 · **D7**, **D8**).
7. **Haritayı iki yerde taşımak.** Yapılandırılmış özelliğin yanında `Attributes` içinde de
   düz metin olarak yer alırsa ikisi ayrışır (C12 · **D9**).

### Verilen kararlar — 2026-11-18

Hepsi Takım Yöneticisi (depo sahibi) onayıyla verildi.

**D1 — Dört durum; sözlük güncellenir (S1).** Durumlar: orijinal, lokal boyalı, boyalı,
değişen. `docs/domain.md` **Parça Haritası** tanımı dört duruma güncellenir.
*Gerekçe:* "lokal boyalı" Türkiye ikinci el pazarının fiyata etki eden gerçek bir
ayrımıdır; üç duruma sıkıştırmak aracın değerini olduğundan düşük gösterir.
*Bedel:* sözlük değişikliği; C2 gereği dört ayrı desen.

**D2 — Şema kodun içinde, satır içi çizilir (S2).** 13 parça ayrı ayrı adreslenebilir
öğelerdir.
*Gerekçe:* renkler token katmanından gelmeli (C3); yüklenen görsel dosyası bunu yapamaz.
`0011` SVG yüklemeyi de kapatıyor (D23).
*Bedel:* tasarım değişikliği bir kod değişikliğidir.

**D3 — Detay yanıtı sıralı görsel listesi taşır (S3).** Ayrıntısı **D22**.

**D4 — Tıkla-seç editörü ayrı kalır (S4).** Bu spec ziyaretçi tarafını ve **D5**'in asgari
veri girişini kapsar; şema üzerinde düzenleme X2.

**D5 — Veri tarafı bu spec'e girer (B1).**
- Kapsam:
  - `paint_change_map` alanı (tek alan, tek eksen çifti).
  - ADR 0007'ye ek kararla yeni değer tipi.
  - Panelin opsiyonel alan formunda 13 seçim kutusu.
  - Varsayılan ayar **Opsiyonel + Görünür**.
  - Seed verisi.
- `0010a` X4'ün veri yarısı ve `0010b` X22'nin bu alana düşen kısmı burada kapanır.
- *Gerekçe:* alan kodda yok (K1); yalnız vitrin yapılırsa özellik canlıda boş çıkar ve
  kriterler yalnız seed'le doğrulanır. 13 × 4 kod tablosu hem veri hem gösterimin ortak
  hakemi; iki spec'e bölmek aynı tabloyu iki yerde tanımlamaktı.
- *Bedel:* spec backend + panel + vitrin olarak büyür; ADR 0007'ye ek karar yazılır.
- *Elenen alternatif:* önkoşul olarak ayrı bir `0016a` spec'i — sıraya bir iş daha
  eklerdi.

**D6 — 13 parçanın kodu ve sırası sabit (B2).** C10'daki tablo.
- *Gerekçe:* listesi hiçbir belgede yazılı değildi (`docs/domain.md:44`,
  `docs/architecture.md:312` yalnız "13 parça" diyor). Kod, çeviri anahtarı ve test aynı
  listeye bağlanır.

**D7 — Değer tam ya da yok; varsayılan seçim yok (B3).** "Belirtilmedi" durumu yok; yarım
harita reddedilir; panelde hiçbir kutu önceden seçili gelmez (R10–R12).
- *Gerekçe:* varsayılan "orijinal", galerinin sessizliğini "hasarsız" beyanına çevirir;
  yarım harita beşinci bir görsel durum doğurur.

**D8 — Değersiz alan ≠ tamamı orijinal (B4).** Değer yoksa bölüm yok; tamamı orijinalse
şema ve *"13 parçanın tamamı orijinal"* cümlesi (R14, R15).
- *Gerekçe:* ilki bilgi yokluğu, ikincisi bir beyandır.

**D9 — Yapılandırılmış ayrı özellik; anahtar hiç yazılmaz (B5).** Yanıtta kod taşıyan
`[{part, state}]` listesi. Alan kapalı, gizli veya değersizse JSON anahtarı yok; veri
`Attributes`'ta tekrar edilmez (C1, C12).
- *Gerekçe:* `ShowcaseAttributeDto` yalnız düz metin taşıyor; `0002` AC-12'nin "boş değer
  olarak da bulunmaz" kuralı yeni özellik için de geçerli.

**D10 — Araç çizimi RTL'de aynalanmaz (B6).** Lejant, liste, notlar ve denetimler aynalanır
(R21, C4). Taslağın R13'ü, C4'ü ve tasarımcı notu bu yönde düzeltildi.
- *Gerekçe:* çizim fiziksel bir üstten görünümdür; aracın sol tarafı locale'e göre yer
  değiştirmez.

**D11 — Şema etkileşimsiz (B7).** `role="img"`, erişilebilir adı listeye işaret eder; Tab
durağı yok (R16).
- *Gerekçe:* 13 sekme durağı bilgiyi listeden daha zor verir; R17'nin listesi metin
  karşılığıdır.

**D12 — Liste yalnız orijinal olmayanları yazar; iki not (B8).** C10 sırasıyla, ardından
*"diğer N parça orijinal"*; yanında *"Galeri beyanı"* ve *"diyagramın üstü aracın ön
kısmıdır"* (R17, R18).
- *Gerekçe:* Intent'in "tek bakışta" hedefi; iki not mockup'ta var.

**D13 — Desen + lejant + 3:1 (B9).** Boş, seyrek tarama, yoğun tarama, çapraz tarama; lejant
her zaman görünür; her durum zemine karşı en az 3:1 (R19, C2). Kanıt gri tonlamalı ekran
görüntüsüyle verilir.
- *Gerekçe:* "renk tek başına anlam taşımaz" ancak böyle ölçülür.

**D14 — Galeri yalnız hazır görseller; detayın ilki ilk hazır görsel (B10).** (R1, R2, R7)
- *Gerekçe:* ziyaretçi bozuk türev görmez, sayaç gerçek sayıyı söyler (K2).
- *Bedel:* liste kartı ile detayın ilk görseli, sıranın ilki hazır değilken farklılaşabilir.
  Kartın kuralı `0012a`'nındır ve bu spec onu değiştirmez → X10, ayrı kart önerildi.

**D15 — Sıfır ve tek görsel (B11).** (R2, R20) Sıfırda görsel alanı, sayaç ve düğme yok;
tekte büyütme var, sayaç ve düğme yok.
- *Not:* "yayındaki araç tüm görsellerini kaybedebiliyor" (K3) bu spec'in değil, `0012a`'nın
  davranışı → X10, ayrı kart önerildi.

**D16 — Gezinme ve büyütme davranışı (B12).** (R3, R4)
- Başa dönme yok; uçtaki düğme devre dışı, odak kaybolmaz.
- Ok tuşları ve kaydırma okuma yönünü izler.
- Büyütülmüş görünüm: Esc kapatır, odak içeride tutulur, kapanınca açan öğeye döner, arka
  plan etkileşimsiz.
- Seçili görsel adrese ve tarayıcı geçmişine girmez.
- *Gerekçe:* her biri ayrı bir kabul kriteri olabilir. Adres sözleşmesi `0004` (SEO)
  gelmeden büyümemeli.

**D17 — `alt` metni şablondan (B13).** *"{model} {yıl} — görsel {n}/{toplam}"* (R5).
`Media`'nın alt metni vitrine taşınmaz → X7.
- *Gerekçe:* modüller arası yeni olay gerekmez (K4); `0002` AC-25'in deseni sürer.

**D18 — Okuma modeli türevleri ve gerçek boyutları taşır; çerçeve 4:3 (B14).** (R6)
- Her görsel için üç türevin adresi ve gerçek boyutu tutulur.
- `srcset` thumbnail / display / large türevlerinden oluşur; büyütülmüş görünüm `large`
  kullanır.
- Çerçeve 4:3, görsel çerçeveye sığdırılır.
- *Gerekçe:* sabit `960×720` gerçek boyutla uyuşmuyor (K5); farklı oranlı fotoğraflar düzeni
  kaydırmamalı (CLS).

**D19 — Detay sayfası Lighthouse'a girer (B15).** `0001` C3'ün mobil eşikleriyle (C13).
- *Gerekçe:* R6'nın LCP/CLS şartını ölçen bir kapı yoktu (K6).
- *Bedel:* CI dakikası (ADR 0010).
- *Elenen alternatif:* yalnız öznitelik düzeyinde kriter (`eager`/`lazy`) — davranışı değil
  biçimi ölçerdi.

**D20 — Ayrı hata durumu yok; görsel başına yer tutucu (B16).** (R20) Sayfa düzeyindeki
durumlar `0002` R9'dan gelir.
- *Gerekçe:* galeri ve şema detay yanıtıyla geliyor ve şema satır içi çiziliyor (D2);
  ikisinin kendi yükleme adımı yok. Taslağın R12'si bu yönde yeniden yazıldı.

**D21 — Ağır hasar ve değişen parça sayısı kapsam dışı; tramer yerinde (B17).** (X5, X8)
Değişen parça sayısı ileride eklenirse haritadan türetilir.
- *Gerekçe:* ikisi de kodda yok; `docs/architecture.md:313` değişen parça sayısını ayrı
  alan sayıyor ve bu aynı bilginin iki kaynağı olurdu.

**D22 — Detay DTO'sunda `CoverImageUrl` kaldırılır (B18).** Yerine sıralı görsel listesi;
liste DTO'su değişmez (C12).
- *Gerekçe:* detaydaki alanın tek tüketicisi detay sayfası (K7). `MediaAssetReady`'nin
  kendi notu, listenin yanında duran ayrı bir URL'in "iki kaynağın ayrışmasına davet"
  olduğunu söylüyor.
- *Bedel:* `0002`'nin sözleşme testi bu alan için değişir (C8'in istisnası).

**D23 — Üst sınır yok; 30 görselle ölçülür (B19).** (R6, X9) Etkileşimden önce yalnız ilk
görsel indirilir.
- *Gerekçe:* R6'nın gecikmeli yüklemesi ancak çok görselli bir ilanla ölçülür.

### Verilen kararlar — plan turu, 2026-11-18

**Takım Yöneticisi (depo sahibi) kararı**, Developer'ın plan bulguları P1–P8 üzerine.
Sekiz öneri **olduğu gibi** onaylandı.

**D24 — Kabul kriterlerini Analist yazar; kriterler uygulama dalı açılmadan `main`'e girer
(P1).** *Gerekçe:* `0015`'te kriterleri uygulayan yazdı, kriter PR'ı açık bekledi ve C10
uygulama sırasında değişti; "kriterler önce `main`'de" ölçütü bunu göremedi (`0015`'in
SCORECARD dersi). Bu kart daha büyük; aynı risk daha pahalı.

**D25 — Galerinin görsel verisi okuma modelinde ayrı bir tabloda tutulur (P2).** İlanın
görselleri sırasıyla, her birinin durumu (`hazır` / `işleniyor` / `başarısız`) ve türevlerinin
adresi ile gerçek boyutları; tabloyu yansıtıcı ve `MediaAssetReady` / `MediaAssetFailed`
işleyicileri günceller. Migration yalnız `vehicle_catalog` şemasına. *Gerekçe:* okuma modeli
zaten olaylarla doluyor (kapak öyle); detay sayfası her istekte `Media`'yı eşzamanlı çağırsaydı
`Media` vitrinin kritik yoluna girerdi. *Elenen alternatif:* detay sorgusunda `Media`'ya
eşzamanlı çağrı — migration'sız ama `Media` düşerse detay da düşer. → R1, R6, R7, C7.

**D26 — Parça haritası yazma ve okuma modelinde ayrı birer `jsonb` kolonda, yalnız kod
olarak saklanır (P3).** Biçim `[{part, state}]`; 13 parça sabit olduğu için ayrı bir tablo
gerekmez. ADR 0007'nin "her tipin kendi kolonu" ilkesiyle tutarlı. C11'in açık bıraktığı
depolama sorusunun cevabıdır. → C6, C11.

**D27 — Yönetim sözleşmesi haritayı yapılandırılmış ayrı bir alanla taşır (P4).** Bugün her
opsiyonel değer tek bir metin (`string? Value`); haritayı metne gömmek doğrulamayı
ayrıştırıcıya iter ve hata kodlarını anlamsızlaştırır. Eksik parçalar hata yanıtında **kod**
olarak döner (C6). → R11, R12.

**D28 — Büyütülmüş görünüm tarayıcının yerel `<dialog>` öğesiyle kurulur; ortak bir
`Dialog` bileşeni `shared/ui`'ya çıkarılır (P5).** Odak tutma, arka planın etkileşimsizliği
ve Esc tarayıcıdan gelir. `identity`'nin onay penceresi aynı deseni kullanıyor; ikinci
kullanıcı çıktığı için bileşen ortaktır (`docs/frontend.md` § 4.2, C8 gerekçesi). → R4.

**D29 — Araç şeması 13 parçalı sade bir üstten görünüm SVG'sidir ve Developer çizer (P6).**
Mockup'a görsel yakınlaştırma ayrı bir iştir. *Gerekçe:* şema etkileşimsiz (R16) ve bilgi
okunabilir listede (R17); sade çizim kriterleri karşılar. → R13, R21.

**D30 — Lighthouse detay sayfasının adresini ölçümden önce liste ucundan okur (P7).** Seed
ilanlarının adresi her kurulumda farklı. → C13.

**D31 — Kart ikiye bölünür (P8).** `0016a` — parça haritası: değer tipi, kalıcılık, alan
ayarı, panel (R8–R12, C6, C10, C11; D5–D7, D26, D27). `0016b` — vitrin: galeri, büyütme,
detay sözleşmesi, şema, performans (R1–R7, R13–R21, C1–C5, C7, C8, C12, C13; D25, D28–D30).
`0016a` önce gelir; sözleşmesi `0016b`'nin önkoşuludur. *Gerekçe:* `0010` ve `0012`'deki
gibi; her dilim kendi başına test edilir ve kriterlerin yazılmadan bölünmesi neredeyse
bedelsizdir (ADR 0003 Ek 1'in ölçümü).
*Not (bölünmede eklendi):* D5 ayrı bir `0016a` spec'ini **elenen alternatif** olarak yazmıştı;
gerekçesi 13 × 4 kod tablosunun iki yerde tanımlanmasıydı. D31 o alternatifi seçer ve
endişeyi tablonun tek evini `0016a` C10 yaparak karşılar.

### Verilen kararlar — kriter turu, 2026-11-18

**Takım Yöneticisi (depo sahibi) kararı**, Analist'in kriter turu soruları S5–S8 üzerine;
öneriler olduğu gibi onaylandı.

**D33 — Kaydırma yönü parmağın hareketiyle tanımlanır (S5).** TR/EN'de sağdan sola, AR'da
soldan sağa kaydırma *sonraki* görsele geçer. → R3, AC-43, AC-44.

**D34 — Hiçbir parça orijinal değilken "diğer N parça orijinal" cümlesi basılmaz (S6).**
*Gerekçe:* bilgi taşımaz ve bir beyan gibi okunur. → R17, AC-37.

**D35 — Büyütülmüş görünüm kapanınca satır içi galeri büyütmede kalınan görseldedir (S7).**
*Gerekçe:* R4 tek bir "seçili görsel"den söz ediyor. → R4, AC-54.

**D36 — Lighthouse en az bir hazır görseli olan ilk ilanı ölçer; yoksa koşu başarısızdır;
LCP öğesinin galerinin ilk görseli olduğu da doğrulanır (S8).** *Gerekçe:* görselsiz ilan
seçilirse ilk görselin LCP'si hiç ölçülmeden "geçti" yazılır. → C13, D30, AC-80, AC-83.

### Verilen kararlar — uygulama turu, 2026-11-18 (ajan)

Uygulama sırasında ölçümle ortaya çıkan dört karar. R/C/AC **değişmedi**.

**D37 — Kabuk, detay adresinde galerinin ilk hazır görselini ön yükler.** Ölçüldü: görsel
HTML → JS → detay isteği zincirinin sonunda keşfediliyordu. Detayın mobil LCP medyanı
**2552 ms** ile C13'ün eşiğini aşıyordu. Tek optimizasyon (AP-12): `IndexHtmlRenderer`,
`/{locale}/cars/{slug}` için `<link rel="preload" as="image">` basar. `imagesrcset` ve
`imagesizes` uçtakiyle birebir aynıdır. Sonrası **2402 ms**: altı koşunun beşi 2402 ms,
biri 2556 ms.
- *Bedel:* HTML isteği başına bir okuma. Bunun için hafif bir `FindFirstImageAsync`
  sözleşmesi eklendi; model adı, nitelik ve fiyat çözülmez.
- *Bedel:* `srcset` ve `sizes` iki dilde yazılı. Sapma görseli iki kez indirtir. Bunu
  `IlkGorselOnYuklemeTests` (`sizes`'ı kaynaktan okur) ve e2e AC-64 (etkileşimden önce
  tek istek) tutar.
- *Elenen alternatif:* daha küçük türev seçtirmek (`sizes`). Mobilde görseli bulanıklaştırır
  ve gerçek fotoğrafta kazancı sınırlıdır.

**D38 — Geliştirme tohumunun galeri kareleri JPEG'dir, SVG değil.** Chrome, bayt başına
bilgisi düşük görselleri LCP adayı saymıyor. Düz SVG kareyle detayın LCP öğesi fiyat
paragrafı çıkıyordu: AC-83 ölçülemezdi ve AC-81 görselsiz bir sayfayı ölçerdi. Kareler,
tam sayı kısıtlı aynı uçtan SkiaSharp ile üretilir (`SeedFrames`).
- *Not:* liste ve ana sayfanın kapakları hâlâ SVG. Onların LCP'si bugün görsel değil; bu
  kartın kapsamı dışında.

**D39 — Parça haritasının görünürlüğü yansıtmada VE okumada elenir (C1).** Alan ayarı
değişip satır henüz yeniden yansıtılmamışken harita sızmasın diye detay sorgusu ayarı
okuma anında da sorar (AC-16, AC-17'nin ilk yarısı).

**D40 — `identity`'nin onay kutusu ortak `Dialog`'a taşınmadı.** D28'in bileşeni
`shared/ui`'da. Ama `ConfirmDialog`'un odak testleri jsdom'da koşuyor ve jsdom
`showModal`'ı taşımıyor. Taşımak `0008`'in regresyon riskini bu karta eklerdi.
*Öneri:* ayrı kart.

### Uygulama planı (onaylı, 2026-11-18)

1. **`0016a`** — `FieldValueKind.PartMap` ve doğrulama (13 parça, 4 durum, yarım harita
   reddi) · `paint_change_map` alanı ve varsayılan ayarı · yazma modeli `jsonb` kolonu ve
   migration · ADR 0007 eki · yönetim sözleşmesi ve panelde 13 seçim kutusu.
2. **`0016b`** — okuma modelinin görsel tablosu ve harita kolonu (migration) · yansıtıcı ve
   medya olay işleyicileri · detay sözleşmesi · `shared/ui` `Dialog` · galeri, büyütme,
   şema, liste, lejant · Lighthouse detay ölçümü · e2e ve kanıt.

---

## Açık sorular

> **Çözüldü (2026-11-18).** S1–S4 → **D1–D4**. Taslağın sorularının metni git geçmişinde
> (`aaf0cec`).
>
> **Kriter turu (2026-11-18, Analist).** S5–S8 → **D33–D36**; öneriler olduğu gibi
> onaylandı (Takım Yöneticisi, depo sahibi). D33 R3'ün kaydırma cümlesini netleştirdi;
> kriterler henüz `main`'de olmadığı için revizyon sayılmaz.

S5. **"Sola kaydırma" parmağın yönü mü?** R3 *"Arapça'da ← ve sola kaydırma sonraki
    görseldir"* diyor. Parmağın sola (sağdan sola) kaydırılması soldan sağa okunan
    dillerde **zaten** "sonraki"dir; cümle bu okumayla Arapça'yı Türkçe'den ayırmaz ve
    "okuma yönünü izler" ilkesiyle çelişir. Ok tuşu kısmı açık (← sonraki), kaydırma kısmı
    iki türlü okunabilir.
    Öneri: kaydırma **parmağın hareket yönüyle** tanımlansın: `tr`/`en`'de sağdan sola
    kaydırma sonraki, `ar`'da soldan sağa kaydırma sonraki (AC-43, AC-44). Ok tuşlarıyla
    simetrik: içerik okuma yönünde "ileri" akar.
    Gerekçe: R3'ün ilkesi ("okuma yönünü izler") ancak bu okumayla iki dili ayırır; Arapça
    yerel görsel gezginleri de böyle davranır.
    Bedel: R3'teki "sola kaydırma" ifadesi bu okumada "içeriğin sola kayması" anlamına
    gelir; metin onaylanırsa netleştirilmesi önerilir (revizyon sayılır).

S6. **"Diğer N parça orijinal" N = 0 iken.** R17 cümlenin her zaman geldiğini söylüyor;
    13 parçanın hiçbiri orijinal değilse "diğer 0 parça orijinal" basılır mı yazılı değil.
    Öneri: N = 0 iken cümle **basılmaz** (AC-37).
    Gerekçe: "diğer 0 parça orijinal" bilgi taşımaz ve bir beyan gibi okunur; R15'in
    tersine (N = 13) zaten kendi cümlesi var, N = 0 için cümle gerekmez.
    Bedel: yok; çeviri kataloğunda ICU'nun `=0` dalı gerekmez.

S7. **Büyütmeden dönünce satır içi galeri hangi görselde?** R4 büyütülmüş görünümde
    gezinmeye izin veriyor ama kapanınca satır içi galerinin konumunu söylemiyor; iki
    makul okuma var (büyütmede kalınan görsel / büyütmenin açıldığı görsel).
    Öneri: **büyütmede kalınan görsel** — tek bir "seçili görsel" durumu (AC-54).
    Gerekçe: R4'ün kendisi "seçili görsel"den tekil söz ediyor; ziyaretçinin gördüğü son
    görselden devam etmesi beklentisi karşılar. Odak yine açan öğeye döner (R4, AC-52).
    Bedel: yok.

S8. **Lighthouse hangi ilanı ölçer?** D30 adresin liste ucundan okunacağını söylüyor ama
    hangi ilanın seçileceğini söylemiyor. Görselsiz bir ilan seçilirse LCP öğesi metin olur
    ve C13'ün ölçmek istediği şey (ilk görselin LCP'si, Tuzak 4) hiç ölçülmeden "geçti"
    yazılır.
    Öneri: ölçülen ilan **en az bir `hazır` görseli olan** ilk ilandır; liste ucunda böyle
    bir ilan yoksa koşu başarısız sayılır (AC-80) ve LCP öğesinin galerinin ilk görseli
    olduğu ayrıca doğrulanır (AC-83).
    Gerekçe: "kapı görmediği yerde yeşil kalır" — ölçmediği şeyi geçmiş saymayan kapı.
    Bedel: CI'ın tohum verisinde görselli bir ilan bulunması gerekir; yoksa koşu kırmızı
    olur (bu istenen davranıştır).

---

## Acceptance Criteria

> Her kriterin testi `DisplayName`'inde `0016b/AC-<n>` taşır (`docs/testing.md` § 1).
> Parça ve durum kodları **`0016a` C10**'daki gibidir. Bir varlığın durumu `hazır`,
> `işleniyor` ya da `başarısız`'dır (D14). "Alan açık" = form durumu `Opsiyonel` veya
> `Zorunlu`; "public" = görünürlük `Görünür`. Örnek veri kümeleri kriterin içinde verilir ve
> ayırt edici seçildi: hazır olmayan varlıklar sıranın başına, ortasına ve sonuna konuldu.
> `0016a` AC-34'ün tohum ilanları (dört durumlu, tamamı orijinal, haritasız) kanıt için
> kullanılabilir.
>
> *Kriter turu (2026-11-18) **Analist** tarafından yazıldı (**D24**). AC-43, AC-44, AC-37,
> AC-54 ve AC-80/AC-83, `## Açık sorular` S5–S8'in önerilen okumasıyla yazıldı.*

### Galeri — içerik ve sıra (R1, R2, R7 · C7 · D14, D25)

- **AC-1.** Medya sırası A (`hazır`), B (`işleniyor`), C (`hazır`), D (`başarısız`),
  E (`hazır`) olan ilanın detay galerisi yalnız A, C ve E'yi bu sırayla gösterir.
- **AC-2.** AC-1'in ilanında sayaç ilk görselde **"1 / 3"** gösterir; toplam 5 değildir.
- **AC-3.** AC-1'in B varlığı için `MediaAssetReady` işlendikten sonra, ilan yeniden
  yayımlanmadan, detay galerisi A, B, C, E'yi gösterir ve sayaç "1 / 4" olur.
- **AC-4.** AC-1'in B varlığı için `MediaAssetFailed` işlendiğinde detay galerisi A, C, E
  olarak kalır ve sayaç "1 / 3" olarak kalır.
- **AC-5.** Personel aracın medya sırasını E, C, A olarak değiştirdikten sonra detay
  galerisi görselleri E, C, A sırasıyla gösterir.
- **AC-6.** Medya sırası A (`başarısız`), B (`hazır`), C (`hazır`) olan ilanın detayı B ile
  açılır ve sayaç "1 / 2" gösterir (R7).
- **AC-7.** AC-6'nın ilanının liste kartı yanıtındaki kapak değeri bu spec'ten önceki
  davranışla aynıdır (`0012a`'nın kuralı; bugün: kapak yok) — R7 kartın kapağını
  değiştirmez (X10).
- **AC-8.** Detay yanıtındaki her görsel adresi `thumbnail`, `display` veya `large`
  türevlerinden birine aittir; varlığın orijinal dosyasının adresi yanıtta hiçbir yerde
  geçmez (C7).

### Detay sözleşmesi (C1, C12 · D9, D22 · F1, F3)

- **AC-9.** Detay yanıtında `coverImageUrl` anahtarı **yoktur**; yerine sıralı bir görsel
  listesi vardır.
- **AC-10.** Görsel listesinin her elemanı üç türevin adresini ve her türevin **gerçek**
  genişlik ve yüksekliğini taşır: 1200×900 kaynaktan üretilmiş varlık için `large`
  1200×900 (1600 değil), `display` 800×600, `thumbnail` 400×300.
- **AC-11.** Vitrin liste (kart) yanıtının şekli değişmez: kart kapak adresini taşımaya
  devam eder ve görsel listesi taşımaz (`0001`'in sözleşme testi yeşil kalır).
- **AC-12.** Alan `Opsiyonel + Görünür` ve araçta `0016a` AC-5'in haritası kayıtlıyken detay
  yanıtı parça haritasını **ayrı** bir özellik olarak taşır: 13 `{part, state}` elemanı,
  C10 sırasında, yalnız kodla.
- **AC-13.** Alan `Zorunlu + Görünür` ve değerliyken de detay yanıtı haritayı taşır
  ("açık" = Opsiyonel veya Zorunlu).
- **AC-14.** Aynı ilanın `ar` ve `tr` locale'li detay yanıtlarında parça haritası özelliği
  aynıdır; ikisi de çevrilmiş parça ya da durum adı taşımaz (C6).
- **AC-15.** Parça haritası `Attributes` içinde **tekrar edilmez**: AC-12'nin yanıtında
  `paint_change_map` anahtarlı ya da parça adı içeren bir nitelik satırı yoktur.
- **AC-16.** Alan **Kapalı** iken detay yanıtının JSON'unda parça haritası özelliğinin
  **anahtarı yoktur** — `null`, boş liste veya boş nesne olarak da yazılmaz.
- **AC-17.** Alan `Opsiyonel + Gizli` iken detay yanıtının JSON'unda parça haritası
  özelliğinin anahtarı yoktur — `null` veya boş olarak da yazılmaz.
- **AC-18.** Alan `Opsiyonel + Görünür` ama araçta değer yokken detay yanıtının JSON'unda
  parça haritası özelliğinin anahtarı yoktur — `null` veya boş olarak da yazılmaz.
- **AC-19.** Alan Kapalı'ya çekilip yeniden `Opsiyonel + Görünür` yapıldığında detay yanıtı
  aynı 13 çifti değişmeden yeniden taşır (F1).
- **AC-20.** Panelde haritada `hood` `original`'dan `painted`'a değiştirilip kaydedildikten
  sonra detay yanıtı `hood` için `painted` taşır.
- **AC-21.** Panelde harita temizlendikten sonra (`0016a` AC-20) detay yanıtında parça
  haritası özelliğinin anahtarı yoktur — vitrin tarafında eski harita kalmaz.

### Şema (R13, R16, R19 · C2, C3 · D2, D11, D13, D29)

- **AC-22.** Değerli ve public alanla detay sayfası araç şemasını basar; şemada C10'un 13
  parçasının her biri ayrı ayrı adreslenebilir bir öğedir ve kendi parça kodunu taşır.
- **AC-23.** 13 parçanın her biri dört durumun her birinde (52 birleşim) o durumun deseniyle
  basılır; şemadaki her parçanın durumu yanıttaki durumuyla eşleşir.
- **AC-24.** Dört durum dört farklı dolgu deseniyle basılır: `original` boş,
  `local_painted` seyrek tarama, `painted` yoğun tarama, `replaced` çapraz tarama; iki durum
  aynı deseni paylaşmaz.
- **AC-25.** Her durumun dolgu renginin şema zeminine karşı kontrast oranı, token
  katmanının değerlerinden hesaplandığında **en az 3:1**'dir (dört ölçüm, dördü de ≥ 3:1).
- **AC-26.** Dört durumlu tohum ilanının şeması **gri tonlamalı** ekran görüntüsünde dört
  durumu desenle ayırt ettirir (görsel kanıt).
- **AC-27.** Lejant dört durumu desen örneği ve etkin locale'deki adıyla gösterir; harita
  yalnız `original` ve `painted` içerse de lejant dört durumun **hepsini** gösterir.
- **AC-28.** Şemanın işaretlemesinde ve stillerinde gömülü renk değeri (hex, `rgb()`,
  `hsl()`) yoktur; renkler `frontend-002`'nin token katmanından gelir (C3).
- **AC-29.** Şema `role="img"` taşır ve erişilebilir adı, okunabilir listeye (R17) işaret
  eden, etkin locale'in kataloğundan gelen bir özettir.
- **AC-30.** Şemadan önceki odaklanabilir öğede Tab'a basılınca odak şemanın hiçbir
  parçasına uğramadan şemadan sonraki odaklanabilir öğeye geçer; şemanın içinde
  `tabindex` ≥ 0 olan öğe yoktur.

### Tamamı orijinal ve değersiz (R14, R15 · D8)

- **AC-31.** 13 parçası `original` olan ilanın detayında şema basılır, 13 parçanın hepsi
  `original` deseniyle görünür ve yanında **"13 parçanın tamamı orijinal"** cümlesi
  bulunur.
- **AC-32.** Haritasız ilanın detay sayfasında parça haritası bölümü — şema, lejant, liste,
  notlar ve başlığı — **hiç basılmaz**; "13 parçanın tamamı orijinal" cümlesi de görünmez.
- **AC-33.** Alan Kapalı veya Gizli olan ilanın detay sayfasında parça haritası bölümü hiç
  basılmaz.

### Okunabilir liste ve notlar (R17, R18 · D12)

- **AC-34.** `right_front_fender` = `painted`, `trunk_lid` = `painted`, `left_rear_door` =
  `local_painted`, diğer 10 parça `original` iken liste şu sırayla yazar: "Bagaj kapağı —
  boyalı", "Sol arka kapı — lokal boyalı", "Sağ ön çamurluk — boyalı" (C10 sırası, girilen
  sıra değil); ardından **"diğer 10 parça orijinal"** cümlesi gelir.
- **AC-35.** AC-34'ün listesinde orijinal parçalar tek tek yazılmaz: "Ön tampon" listede
  yoktur.
- **AC-36.** Yalnız `hood`'un `original` olduğu haritada cümle **"diğer 1 parça orijinal"**
  olur; sayı içeren cümle ICU ile kurulur ve Arapça'da sayının Arapça çoğul biçimini
  kullanır.
- **AC-37.** 13 parçanın hiçbiri `original` değilken liste 13 parçayı C10 sırasıyla yazar
  ve "diğer N parça orijinal" cümlesi **basılmaz** (S6).
- **AC-38.** Tamamı orijinal ilanda (AC-31) liste yoktur; yerinde R15'in cümlesi bulunur ve
  "diğer 13 parça orijinal" cümlesi basılmaz.
- **AC-39.** Şemanın yanında **"Galeri beyanı"** ve **"Diyagramın üstü aracın ön
  kısmıdır"** notları etkin locale'in kataloğundan gelen metinle görünür.

### Galeri — gezinme (R3 · D16)

- **AC-40.** 3 görselli galeride "sonraki" düğmesi "1 / 3"ten "2 / 3"e geçirir; "önceki"
  düğmesi "1 / 3"e geri getirir.
- **AC-41.** İlk görselde "önceki" düğmesi devre dışıdır ve etkinleştirilmeye çalışılınca
  galeri son görsele **dönmez**.
- **AC-42.** "Sonraki" ile son görsele gelindiğinde "sonraki" düğmesi devre dışı kalır ve
  odak `document.body`'ye düşmez; galerinin içinde bir öğede kalır.
- **AC-43.** `/tr/`'de galeri odaktayken → tuşu sonraki, ← tuşu önceki görsele geçer;
  parmağın sağdan sola kaydırılması sonraki görseldir (S5).
- **AC-44.** `/ar/`'de galeri odaktayken ← tuşu sonraki, → tuşu önceki görsele geçer;
  parmağın soldan sağa kaydırılması sonraki görseldir (S5).
- **AC-45.** Galerinin her etkileşimli öğesi (önceki, sonraki, büyüt) yalnız klavyeyle
  (Tab) ulaşılabilir ve odaktayken görünür bir odak halkası taşır (görsel kanıt).

### Büyütme (R4 · D16, D28)

- **AC-46.** Satır içi görselden büyütme açıldığında büyütülmüş görünüm aynı görselin
  `large` türevinin adresini gösterir.
- **AC-47.** Büyütülmüş görünümde "sonraki"/"önceki" düğmeleri, ok tuşları ve kaydırma R3
  gibi çalışır; uçlarda dönmez.
- **AC-48.** Büyütülmüş görünüm **Esc** ile kapanır.
- **AC-49.** Büyütülmüş görünüm kapatma düğmesiyle kapanır.
- **AC-50.** Görünüm açıkken Tab'a art arda 15 kez basıldığında odak her seferinde görünümün
  içindeki bir öğededir.
- **AC-51.** Görünüm açıkken arka plandaki öğeler etkileşimsizdir: arka plandaki bir
  bağlantıya tıklamak sayfayı değiştirmez ve arka plan erişilebilirlik ağacından gizlidir.
- **AC-52.** Görünüm kapandığında odak görünümü açan öğeye döner.
- **AC-53.** Görünüm açılıp kapatıldığında ve içinde gezinildiğinde adres **değişmez** ve
  `history.length` artmaz.
- **AC-54.** Büyütülmüş görünümde 3. görsele gidilip görünüm kapatıldığında satır içi
  galeri 3. görseli gösterir ve sayaç "3 / N" olur (S7).
- **AC-55.** 3. görseldeyken sayfa yeniden yüklendiğinde galeri 1. görselden başlar.

### `alt` metni (R5 · D17)

- **AC-56.** Toyota Corolla 2019 modelli, 18 hazır görselli ilanın `/tr/` detayında 3.
  görselin `alt` metni **"Toyota Corolla 2019 — görsel 3/18"**'dir.
- **AC-57.** Aynı ilanın `/ar/` ve `/en/` detayında `alt` metni o locale'in kataloğundaki
  şablondan üretilir ve Türkçe şablon metnini içermez.
- **AC-58.** `Media`'da varlığın kayıtlı alt metni "Ön görünüm" iken detaydaki `alt` metni
  bu metni içermez; yalnız R5'in şablonundan gelir (X7).
- **AC-59.** Büyütülmüş görünümdeki görselin `alt` metni aynı şablondan, aynı sıra ve
  toplamla üretilir.

### Yükleme ve yerleşim (R6 · D18, D19, D23)

- **AC-60.** Satır içi görselin `srcset`'i üç türevi **gerçek** genişlikleriyle listeler:
  1200×900 kaynak için `400w`, `800w` ve `1200w` (`1600w` değil).
- **AC-61.** Görselin `width`/`height` öznitelikleri satır içi türevin gerçek boyutudur:
  `display` türevi 600×800 (dikey) olan görsel `width="600" height="800"` ile basılır;
  `960×720` yazılmaz.
- **AC-62.** Galeri çerçevesi 4:3'tür: 4:3 yatay, 3:4 dikey ve 16:9 görseller arasında
  gezinirken galeri kutusunun boyutu değişmez ve galerinin altındaki içeriğin konumu
  kaymaz.
- **AC-63.** İlk görsel gecikmeli yüklenmez (`loading="lazy"` taşımaz) ve
  `fetchpriority="high"` taşır; diğer görsellerin her biri `loading="lazy"` taşır.
- **AC-64.** 30 hazır görselli ilanın detayı açıldığında, hiçbir etkileşim olmadan, ağ
  kaydında galeri görsellerinden yalnız **1** istek bulunur (D23).

### Durumlar (R20 · D15, D20)

- **AC-65.** Hiç hazır görseli olmayan ilanın detayında görsel alanı, sayaç, gezinme
  düğmeleri ve büyütme **yoktur**; model, fiyat ve nitelikler görünür (bugünkü davranış).
- **AC-66.** Medya sırası A (`işleniyor`), B (`başarısız`) olan ilan AC-65 ile aynı biçimde
  basılır (hazır görsel sayısı sıfır).
- **AC-67.** Medya sırası A (`başarısız`), B (`hazır`), C (`başarısız`) olan ilanda tek
  görsel (B) ve büyütme vardır; sayaç ve gezinme düğmesi **yoktur**.
- **AC-68.** Tek görselli ilanda büyütülmüş görünüm açılır; görünümde sayaç ve gezinme
  düğmesi yoktur.
- **AC-69.** 3 görselli ilanda 2. görselin adresi `404` döndüğünde o yuvada yer tutucu
  gösterilir (tarayıcının kırık görsel simgesi görünmez); sayaç "2 / 3" gösterir ve
  "sonraki" ile 3. görsele geçilir.
- **AC-70.** Galeri ve şema detay yanıtıyla gelir: detay sayfası açılırken galeri veya şema
  için detay isteğinden ayrı bir veri isteği yapılmaz ve galeriye ya da şemaya özgü ayrı
  bir yükleniyor veya hata göstergesi yoktur (sayfa düzeyindeki durumlar `0002` R9'dan).

### Locale ve RTL (R21 · C4, C5 · D10)

- **AC-71.** `/ar/` detayında lejant, okunabilir liste ve notlar sağdan sola akar (görsel
  kanıt).
- **AC-72.** `/ar/` detayında galeri denetimleri aynalanır: "önceki" düğmesi galerinin sağ
  kenarında, "sonraki" sol kenarındadır (`/tr/`'nin tersi).
- **AC-73.** Araç çizimi aynalanmaz: `/tr/` ve `/ar/` detaylarında `left_front_door`
  öğesi şemanın **sol** yarısında, `right_front_door` **sağ** yarısındadır; 13 parçanın her
  birinin şemaya göre konumu iki locale'de aynıdır.
- **AC-74.** Bu spec'in eklediği stillerde fiziksel yön özelliği (`margin-left`,
  `padding-right`, `left`, `right`, `text-align: left` vb.) yoktur; kural araç çiziminin
  iç koordinatlarına uygulanmaz (C4).
- **AC-75.** 13 parça adı, dört durum adı, R15, R17 ve R18'in cümleleri, lejant ve R5'in
  `alt` şablonu için çeviri anahtarları `tr`, `ar` ve `en` kataloglarının üçünde de
  doludur; eksik anahtar kontrolü yeşildir.
- **AC-76.** `/en/` detayında parça haritası bölümünün ve galerinin görünen bütün metinleri
  İngilizce'dir; Türkçe ya da Arapça metin görünmez.

### Regresyon (C8 · D22)

- **AC-77.** `0002`'nin 25 kabul kriterinin testleri yeşil kalır; değişen tek iddia
  `coverImageUrl` alanına ait sözleşme iddiasıdır ve görsel listesine çevrilir (C8).
- **AC-78.** `0002` AC-12 ve AC-13 yeni özellik için de geçerlidir: tramer (`Gizli`) detay
  yanıtında hiçbir biçimde bulunmaz ve parça haritasının eklenmesi bunu değiştirmez.

### Performans (C13 · D19, D30)

- **AC-79.** Lighthouse koşusu `/tr/` ve `/tr/cars`'a ek olarak detay sayfasını ölçer;
  ölçülen ilanın adresi ölçümden önce liste ucundan okunur, betikte sabit yazılmaz (D30).
- **AC-80.** Ölçüm için seçilen ilan en az bir `hazır` görsel taşır; liste ucunda böyle bir
  ilan yoksa koşu **başarısız** sayılır, "geçti" yazılmaz (S8).
- **AC-81.** Detay sayfası Lighthouse mobil profilinde (4× CPU, Slow 4G, 3 koşu, medyan)
  **LCP < 2,5 s** ölçülür.
- **AC-82.** Aynı ölçümde detay sayfasının **CLS < 0,1**'dir.
- **AC-83.** Aynı ölçümde LCP öğesi galerinin ilk görselidir (Tuzak 4; S8'in seçtiği ilan
  görselli olduğu için ölçülebilir).

---

## Definition of Done

- [x] Açık sorular karara bağlandı. *(S1–S4 → D1–D4, B1–B19 → D5–D23, P1–P8 → D24–D31, S5–S8 → D33–D36, 2026-11-18.)*
- [x] `0011` tamamlandı. *(`specs/done/0011-media-yukleme-turev-filigran.md`.)*
- [x] `0016a` tamamlandı (parça haritasının değeri ve kod tablosu — **D31**). *(#93 → `10268d5`.)*
- [x] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi. *(#94 gövdesi, tablo.)*
- [x] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0016b-ac<n>.png`); şemanın her durumu, boş ve hata
      durumları, Arapça (RTL) görünüm dâhil. *(`0016b-ac23` (dört durum), `-ac26-gri`, `-ac31`, `-ac45-odak`, `-ac46`, `-ac62-dikey-16-9`, `-ac65` (boş), `-ac67` (tek görsel), `-ac69` (hata: 404 yer tutucu), `-ac71-ar`, `-ac72-ar`; `0016b-performance.json`.)*
- [x] Şemanın renk dışı ayırt edilebilirliği kanıtla gösterildi (C2, R19). *(`0016b-ac26-gri`; AC-24 desen imzası ve AC-25 kontrast testi; tema kapısı 3:1 çiftleri — BULGU-2.)*
- [x] Klavyeyle tam gezinme ve odak halkası kanıtla gösterildi (R3). *(`0016b-ac45-odak`; e2e AC-42, AC-43/44, AC-45, AC-50.)*
- [x] `0002`'nin kabul kriterleri regresyon testi olarak hâlâ yeşil (C8). *(AC-77: değişen tek iddia `0002` AC-10'un kapak iddiası, görsel listesine çevrildi.)*
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi — 13 parça adı ve durum adları dâhil. *(532 anahtar, üç katalog eşit.)*
- [x] Ham piksel/hex eklenmedi; token katmanı kullanıldı (C3). *(AC-28 testi işaretleme ve stilleri tarıyor; dört durum rengi `tokens.css`'te.)*
- [x] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4). *(#94 son commit `23ee190` ve `main`'de `dec382e`: CI ve Performans yeşil; CI'da detay LCP 2404–2406 ms.)*
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). *(`VitrinGaleriVeHaritaTests` AC-1–21, AC-78; `IlkGorselOnYuklemeTests`; `TohumMedyasiKaydiTests`; medya zinciri Host testi.)*
- [x] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok. *(Architecture 56/56 — `0012a` AC-33 triyaj düzeltmesinde bir kez kırmızı verdi, düzeltildi.)*
- [x] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor. *(`AddShowcaseListingMedia` yalnız `vehicle_catalog`.)*
- [x] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi. *(Yeni terim yok.)*
- [x] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi. *(Karar Defteri: "Vitrin galerisi" ve "İlan detayı ilk görseli" satırları, "Vitrin okuma modeline yazma" satırı genişletildi — kapanış turu.)*
- [x] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez. *(Triyaj 2026-11-18, merge'den önce: BULGU-1 → `b02961f`, BULGU-2 → `ba3a365`, BULGU-3 → `2afefd6` + `6409d01`.)*
- [x] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı. *(#94; açık riskler gövdede.)*
- [x] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi. *(`dec382e`, 2026-11-18.)*
- [x] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı. *(Bu kapanış; taşımadan sonra `Architecture.Tests` tam takımı koşturuldu.)*

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
| **Kriterler önce `main`'de miydi?** | **evet** | **Fiilen de evet.** `git cat-file -e dec382e^:specs/0016b-…` → var. Kriterler (Analist, **D24**) #92 ile 12:35'te `main`'deydi; 0016b dalının ilk commit'i (`b2d45b0`) ondan sonra. R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır — düşük sayı iyi spec değil, karşılaştırılacak tabanı olmayan spec demektir. |
| **Kriter sayısı: önce → bugün** | **83 → 83** | Uygulama, triyaj ve D37–D40 kriter eklemedi ya da çıkarmadı. O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. İkisi arasındaki fark, revizyon sayısının söylemediği şeyi söyler: `0008` bu ölçümde **2 → 33** çıktı ve kartı revizyon **1** yazıyordu. |
| **Spec revizyon sayısı** | **0** | R, C ve AC bölümleri `970e14f` ile `main` arasında bayt bayt aynı. D37–D40 Context'e (Verilen kararlar) yazıldı; ölçüt onları saymaz. **Not:** D37 C13'ü karşılamak için kabuğa bir okuma ekledi — kriter değişmedi ama kriterin bedeli uygulamada ortaya çıktı. Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? Açıklayıcı not eklemek de sayılır: nete ihtiyaç duyan kural, eksik yazılmış kuraldır. Intent, Context, kapsam dışı gerekçeleri ve SCORECARD değişiklikleri **sayılmaz**. Yüksekse spec eksik yazılmıştır. **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3) — ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve kriter sayısının iki katına çıkması bunun en net cevabıdır. Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | **1** | Triyaj bir tur döndürdü: BULGU-1, -2, -3. T10 `[x]`. PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. **`0` ile `—` aynı şey değildir:** `0`, gözden geçirildi ve hiç tur dönmedi demektir; gözden geçirme hiç yapılmadıysa `—` yazılır. İkisini aynı göstermek, ölçülmemiş işi temiz işmiş gibi gösterir. **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). Bu, "gözden geçirildi"yi var olan bir kutuya bağlar: kural eskiden kimin gözden geçirdiğini söylemiyordu ve üç kart (`0002`, `0007`, `frontend-002`) tam o boşlukta `0` yazdı. |
| **Dış bulgu (gerçek/gürültü)** | **3 / 0** | Üçü de yeniden üretildi ve gerçek çıktı: BULGU-1'in testi ilk koşuda "3 / 5" verdi; BULGU-2'nin kapı testi paint token'larını listede bulamadı; BULGU-3'ün karşı ölçümü sarmalayıcısız yansıtmada galeriyi `işleniyor`a düşürdü. **Gürültü yarısı ölçülemedi**. **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz — kendi kodunu okuyan kişi kendi körlüğünü paylaşır. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. **Ölçtüğü şey: spec ve kod ne kadar açık yazıldı.** |
| **Uygulama içi bulgu** | **11** | **Ürün (7):** toplu yansıtmada hazır olmayan kapağı yeniden okuyan korumayı kaldırmıştım (kendi okumamda) · `Dialog`'un `close` olayı effect sırasında kayboluyordu (AC-52 kırmızı) · lejantın "tek tanım" yorumu kodla çelişiyordu · galeri çerçevesi odak halkasını kırpıyordu (kanıta bakınca) · detay LCP'si 2552 ms (D37) · `src` altında `float` bildirimi (Yasak Listesi 26 kapısı) · `Media.Contracts` modül kökünde (`0012a` AC-33 kapısı; triyaj düzeltmesi sırasında). **Ölçüm (2):** Lighthouse 13'te betik LCP öğesini hiç okumuyordu (bütün kartlarda "bilinmiyor") · düz SVG tohum görseli LCP adayı değildi (D38). **Kendi testlerim ve araçlarım (2):** AC-46 e2e'sinde kare numarası · Lighthouse betiğinde `**C13**/**D19**` yorumu kapattı (bellekteki desen). Yazarın kendi işini yaparken bulduğu kusur sayısı — kendi testi kırmızı verdiği, kendi kanıtına baktığı veya kendi sondası yakaladığı için. Dış bulguyla aynı sütunda toplanmaz: ikisi **farklı şey** ölçer. **Ölçtüğü şey: yazar kendi işini ne kadar sorguladı.** Sıfır, "hiç hata yapmadım" değil "hiçbirini kendim bulmadım" demektir ve dış bulgu yüksekse bu bir uyarıdır. |
| **Regresyon sayısı** | **0** | `main`'e ulaşan yok. `0002` AC-10'un kapak iddiası C8'in istisnası (D22), regresyon değil. **Not:** tema yapılandıran bir kurulum artık dört durum rengini de vermek zorunda (BULGU-2) — bugün yapılandırılmış tema yok; davranış değişikliği PR'da yazılı. Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0002`'yi bozup bozmadığı burada görünür. |
| **Kaçan hata** | **izleniyor** | Merge 2026-11-18 (`dec382e`). Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**

**Ölçüm araçları sessizce kördü ve yeşil veriyordu.** Lighthouse 13 LCP öğesinin
denetimini kaldırmıştı; betik her kartta "bilinmiyor" yazdı ve kimse sormadı. Tohumun
düz SVG görselleri de LCP adayı değil, yani liste ve ana sayfanın "LCP < 2,5 s"i bugün
görseli ölçmüyor. Detayın ilk ölçümü fiyat paragrafını ölçüyordu; gerçek raster ile
2552 ms çıktı ve eşik dışıydı. Ders: perf kriterinde sayının yanında **LCP öğesinin
kendisi** de doğrulanmalı; AC-83 bunu yaptığı için boşluk görüldü.

**Uçta ölçen test çift adımı gizler** (BULGU-1). AC-47'nin testleri 2/3 → 3/3'ü ölçüyordu;
iki adımlık hata sınırda kırpıldığı için ikisi de yeşildi. Gezinme ortadan ve tek
basışla ölçülmeli.

**Kapalı liste yeni bir token'la delinir** (BULGU-2). frontend-002'nin kapısı "her renk
token'ı listede" varsayımına dayanıyordu ama bunu ölçen test yoktu. Artık var
(`CiftTablosuTests.Kapsar_TokensCsstekiHerRenkTokenini`).
