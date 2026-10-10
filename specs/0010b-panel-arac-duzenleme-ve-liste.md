# 0010b — Panel: Araç Düzenleme ve Liste

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer (ajan oturumu) — atandı 2026-10-10 |
| **Analist** | Metnin kaynağı `0010` (Analist oturumu); **bölünme kararı** `0010a` D9 (Developer oturumu). Sahip atandığında güncellenir. |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (yazma ve okuma yüzeyi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | **`0010a` (araç kaydı — önce bitmeli)** · `0006` · `0007` · `frontend-001` |
| **Dayanak** | `0010a` Karar **D9** |
| **Karar bekliyor** | yok — **D17–D20** spec yazılırken, **D21–D29** uygulama planıyla birlikte onaylandı (2026-10-10). Numaralandırma `0010a`'nın D16'sından devam eder; karar defteri iki spec arasında tek eksendir. |

---

## Intent

`0010a` bittiğinde galeri çalışanı araç **girebiliyor** ama **düzenleyemiyor**: yanlış
girilen bir araç öylece kalıyor, ilan metni hiçbir dilde yazılamıyor ve liste yalnızca
"kaydım göründü mü" sorusunu cevaplayacak kadar sade.

Bu spec o boşluğu kapatır: personel kaydettiği araca döner, opsiyonel alanları doldurur,
ilan başlığını ve açıklamasını üç dilde yazar, ve araçlarını **sayfalanan, sıralanan,
eksikleri gösteren** bir listede yönetir.

Başarı ölçütü: **yarıda bırakılan iş geri alınabilir ve neyin eksik olduğu görünür.**

---

## Requirements

- **R1.** Personel var olan bir araç kaydını **düzenleyebilir** ve yarıda bıraktığı işe
  dönebilir.
- **R2.** Düzenleme isteği **çekirdek alanların tamamını taşır** (tam değiştirme). Kısmi
  gönderim kabul edilmez. → **C25**, Karar **D19**.
- **R3.** Var olmayan bir araç kimliğiyle gelen düzenleme isteği **reddedilir** ve kararlı
  bir hata kodu döner.
- **R4.** **Şase numarası düzenlenemez**; yalnız kayıt anında girilir. → Karar **D20**.
- **R5.** Düzenlemede **fiyat değiştiğinde fiyat geçmişine yazılır** — "fiyat sorunuz"dan
  belirli bir fiyata (ve tersine) geçiş dâhil. → `docs/domain.md` **A6**, `0010a` R9.
  *(Gerekçe: geçmiş geriye dönük üretilemez ve insanın fiyatı değiştirebildiği yer **bu**
  spec'tir.)*
- **R6.** Her opsiyonel alan **tanımlı bir tipe** sahiptir. Tip kümesi kapalıdır: `metin`,
  `locale'li metin`, `tam sayı`, `tutar`, `evet/hayır`, `tarih`, `seçenek`. Tipi beyan
  edilmemiş bir alan kaydedilemez; **tutar** tipli alan `Money` ile saklanır.
  → Karar **D17**, **C23**. *(Dosya, liste ve medya tipleri → X22.)*
- **R7.** Personel, **alan ayarlarında açık** olan opsiyonel alanları doldurabilir. Kapalı
  alanlar formda görünmez.
- **R8.** Alan ayarlarında **zorunlu** işaretli bir opsiyonel alan boş bırakılarak araç
  **taslak olarak kaydedilebilir**; zorunluluk yalnızca yayına alınırken bağlayıcıdır.
  → `docs/domain.md` **A2**, **F2**.
- **R9.** Bir alanın zorunlu yapılması, o alanı boş olan **mevcut** araçları geçersiz
  kılmaz ve düzenlemeye kapatmaz; bu araçlar **"eksik bilgi"** olarak işaretlenir ve bu
  işaret listede görünür. → `docs/domain.md` **F2**. *(Ayarı değiştirecek yüzey bu spec'te
  yoktur → X21; gözlem noktası **C24**'tedir.)*
- **R10.** Personel, ilan başlığını ve açıklamasını **etkin her dilde** (TR/EN/AR) ayrı ayrı
  yazabilir. Bir dilde yazmamak geçerli bir durumdur.
- **R11.** İlan başlığının ve açıklamasının **locale başına üst uzunluk sınırı** vardır;
  sınırı aşan istek reddedilir.
  *(Gerekçe: `0010a` R11 bu sınırı koymuştu ama o spec alanları hiç yaratmadı — `Listing`'de
  başlık/açıklama yoktu. Alanları **bu** spec yaratıyor ve 0010b yalnız C1–C20 ile X1–X18'i
  devralıyor, R'leri devralmıyor; sınır bu yüzden burada tekrar yazılı. `jsonb` sınırsız
  metin kabul eder ve tek bir kayıt veritabanını şişirebilir — C22.)*
- **R12.** Panel, her araç için **hangi dillerde metnin eksik olduğunu** gösterir. Bir dil,
  **başlık ve açıklamasının ikisi de** doluysa tam sayılır; biri eksikse o dil **eksiktir**.
- **R13.** Panel, kayıtlı araçları bir **listede** gösterir: araç tanımı, fiyat, kilometre,
  yayın durumu, **eksik dil** göstergesi, **eksik bilgi** göstergesi ve son güncelleme zamanı.
- **R14.** Liste yanıtı **`PagedResult<T>`** biçimindedir ve **yapılandırılan sayfa
  boyutuyla** sayfalanır; personel listeyi **son güncellenen** veya **yeni eklenen** sırasına
  göre görebilir. Her iki sıralamada da eşitlik **araç kimliğiyle** kırılır.
  → `docs/conventions.md` § 3, **C26**.
  *(Gerekçe: kırıcı olmadan aynı dakikada güncellenmiş iki araç kararsız sıralanır ve aynı
  araç iki sayfada görünebilir — `0001`'in SCORECARD'ında kayıtlı ders.)*
- **R15.** Liste, kaç ilanda **çeviri eksik** olduğunu **tüm araçlar** üzerinden özetler;
  gösterilen sayı o anki sayfanın içeriğine bağlı değildir.
- **R16.** Liste ekranı **yükleniyor**, **boş** ve **hata** durumlarını ele alır.
  → `docs/frontend.md` § 3.
- **R17.** Düzenleme ekranı `0010a` R22'nin dört durumunu aynı biçimde ele alır.

---

## Constraints

### Uyulması zorunlu

`0010a`'nın **C1–C20'si olduğu gibi geçerlidir** ve burada tekrarlanmaz — bir kural
yalnızca bir dosyada yaşar. Bu spec'e özgü iki ek:

- **C21.** **"Eksik bilgi" saklanmaz, sorguda hesaplanır.** → `0010a` Karar **D15**.
  *(Gerekçe: C7 geriye dönük doğrulamayı yasaklıyor; bayrak saklamak tam olarak o olurdu ve
  ayar değiştiğinde hiçbir yazma olmadan değişmesi gerekiyor.)*
- **C22.** **İlan metni `jsonb` olarak saklanır**, locale başına satır olarak değil.
  → `0010a` Karar **D13**.
- **C23.** **C17'nin adlandırılmış istisnası — opsiyonel alan değerinin depo tipi.**
  `0010a` C17 mevcut agregat davranışının yeniden yazılmamasını söylüyor. `Vehicle`'ın
  `SetOptionalFieldValue` imzası bugün **tüm** opsiyonel değerleri locale'li metin olarak
  alıyor; bu spec onu **tipli** hâle getirir. İstisna buraya adıyla yazılıdır ve C17'nin
  geri kalanı yürürlüktedir. → Karar **D17**.
  *(Gerekçe: bugün tanımlı tek opsiyonel alan `damage_record_amount` — tramer, yani bir
  **tutar**. Parayı metin olarak saklamak `AGENTS.md` Altın Kural 4 ve Yasak Listesi 26'nın
  doğrudan ihlalidir. Tip güvenliği bir biçim tercihi değil, bir Altın Kural; C17 ona
  kapı kapatamaz.)*
- **C24.** **Gözlem noktası — alan ayarı değişikliği.** Alan ayarlarını **yazan** hiçbir uç
  yoktur ve bu spec yazmaz (X21). Bu yüzden "bir alan zorunlu/kapalı yapıldığında" diyen
  kriterlerin ön koşulu testte **veri katmanından** kurulur; endpoint testi uygulanabilir
  değildir (`0006` C16 kalıbı).
- **C25.** **Düzenleme tam değiştirmedir.** İstek gövdesi çekirdek alanların tamamını taşır;
  eksik alan "dokunulmadı" değil **geçersiz** sayılır.
  *(Gerekçe: kısmi semantik "boş geldi = silindi mi, dokunulmadı mı" belirsizliğini üretir ve
  `0010a` D8'in çekirdek alan invariantını test edilemez kılar.)*
- **C26.** **Panel sayfa boyutu** varsayılan **25**, üst sınır **100**'dür ve dağıtım
  yapılandırmasından okunur. Üst sınırı aşan `pageSize` isteği üst sınıra **kırpılır**, hata
  dönmez. → `docs/conventions.md` § 3. *(Vitrin tarafının 24'ü ayrı bir eksendir —
  `ShowcaseListingRequest.PageSize`; iki yüzey aynı sayıyı paylaşmak zorunda değildir.)*

### KAPSAM DIŞI

`0010a`'nın X1–X17'si geçerlidir. Ek olarak:

- **X19.** **Araç kaydının silinmesi** — *neden:* **F1** "kapatmak silmek değildir" diyor ve
  silme `Arşiv` durumuna aittir; o da `0012`'nin (yayın ve durum eylemleri) işidir.
- **X20.** **Eşzamanlı düzenlemede kaybolan güncelleme koruması** — *neden:* `0010a` X16 ile
  aynı gerekçe. **Bu spec'te risk `0010a`'dakinden büyüktür:** düzenleme ekranı gelince iki
  danışmanın aynı aracı açıp kaydetmesi sıradan bir senaryo olur. Kabul edilen bu risk PR
  açıklamasında yazılır (DoD) — bilerek ödenen bedelin görünür kalması, bir sonraki spec'in
  onu aramasını sağlar.
- **X21.** **Alan ayarları yönetim ekranı ve yazma ucu** — *neden:* bugün yalnız
  `GET /api/catalog/field-settings` var; ayarları **yazan** hiçbir uç yok ve ne `0010a` ne bu
  spec onu kuruyor. Ayar ekranı kendi yetki ve önizleme kriterlerini getirir ve kendi
  spec'ini hak eder. **Bilerek ödenen bedel — ve açıkça söylenmesi gerekiyor:** bu spec
  bittiğinde galeri bir alanı **zorunlu veya kapalı yapamaz**; R7–R9'un davranışı yalnız
  testte ve seed verisiyle görülebilir (C24).
- **X22.** **Dosya, liste ve medya tipli opsiyonel alanlar** (ekspertiz raporu dosyası,
  donanım işaret listesi, video & 360° tur) — *neden:* üçü de depolama, yükleme veya ayrı bir
  düzenleyici getirir; dosya ve medya `Media` modülüne (`0011`) bağlıdır. R6'nın kapalı tip
  kümesi bu üçünü **içermez**.
- **X23.** **Aracın şubesinin düzenlenmesi (transfer)** — *neden:* `docs/domain.md` **A7**
  şube değiştiğinde `VehicleRelocated` olayının yayılmasını istiyor; o olay kodda yoktur ve
  tüketicisi de yoktur. Yarım bir transfer akışı A7'yi kâğıt üstünde bırakır. Transfer kendi
  eylemidir ve kendi spec'ine aittir.

---

## Context

### Mevcut davranış — `0010a` sonrası

`0010a` şunları bırakır: `vehicles` / `listings` / `price_changes` tabloları, yazma modeli
kalıcılığı, altı referans kümesinin okuma yüzeyi, araç oluşturma komutu ve ucu, şase
tekilliği (`InvariantGuard`), fiyat geçmişi, ve **sade** bir liste (araç tanımı, fiyat,
kilometre).

Eksik bırakılanlar bu spec'in kapsamıdır: **düzenleme komutu ve ucu yok**, `Listing`'de
**başlık/açıklama alanı yok**, listede **sayfalama, sıralama ve göstergeler yok**.

### Doğrulanmış bulgular — `0010a`'nın bıraktığı yüzey

- **Liste sözleşmesi sayfalı değil.** `IVehicleAdminQueries.ListAsync(locale, ct)` düz
  `IReadOnlyList<VehicleAdminListItemDto>` dönüyor; sayfa, sayfa boyutu ve **sıralama
  parametresi yok**. DTO `(VehicleId, ModelName, ModelYear, MileageKilometers, Price,
  ListingStatus, Vin)` — **`UpdatedAt` ve göstergeler yok**. Sözleşmenin kendi doc comment'i
  de bunu yazıyor: *"Sayfalama, sıralama ve göstergeler `0010b`'dedir."*
  **Sonucu:** bu spec `IVehicleAdminQueries`'i ve `VehicleAdminListItemDto`'yu **değiştirir** —
  dönüş `PagedResult<T>`e, DTO `UpdatedAt` + iki göstergeye genişler, uç bir sıralama
  parametresi alır. `0010a`'nın liste kriterleri bu değişiklikten sonra da yeşil kalmak
  zorundadır (DoD).
- **Opsiyonel alan değerleri tipsiz.** `SetOptionalFieldValue(string fieldKey,
  LocalizedText value)` — tüm değerler locale'li metin kovasına giriyor. Ve `VehicleFieldKeys`
  yalnız iki anahtar taşıyor: `body_type` (çekirdek) ile **`damage_record_amount`** — tramer,
  bir **tutar**. Yani bugün kaydedilebilecek tek opsiyonel alan bir para alanı ve depo tipi
  metin. → Karar **D17**, **C23**.
- **Alan ayarlarını yazan uç yok.** Tek uç `GET /api/catalog/field-settings`.
  `0010a`'nın X1–X18'inde de bu eksiklik kapsam dışı ilan edilmemiş. → **X21**, **C24**.
- **`Listing`'de başlık/açıklama yok** (doğrulandı: yalnız `VehicleId`, `Status`,
  `ShowcaseSortWeight`, `PublishedAt`) ve **düzenleme ucu yok** (yalnız `POST /` ve `GET /`).
  Bu ikisi bu spec'in çekirdeğidir.
- **`0010a` R11 sahipsiz kaldı.** O spec ilan başlığı ve açıklaması için üst uzunluk sınırı
  koymuş ama alanları hiç yaratmamıştı; 0010b yalnız **C1–C20** ile **X1–X18**'i devralıyor,
  R'leri devralmıyor. Sınır bu yüzden R11 olarak burada tekrar yazılı.

### Bilinen tuzaklar

1. **"Eksik bilgi"yi bayrak olarak saklamak.** C21; ayar değiştiğinde bayrak bayatlar ve
   geriye dönük doğrulama yasağı (C7) delinmiş olur.
2. **Sıralama kırıcısını atlamak.** Son güncelleme zamanı aynı olan iki araçta kırıcı yoksa
   sayfalama kararsızlaşır ve aynı araç iki sayfada görünür — `0001`'in dersi.
3. **Düzenlemede çekirdek alanı gevşetmek.** `0010a` D8 "taslak = çekirdek alanların hepsi
   dolu" diyor; düzenleme bunu gevşetirse aynı invariant iki yerde iki farklı şey olur.
4. **Kapatılan alanın değerini silmek.** **F1**: alan kapatmak değeri silmez; yalnız yanıta
   girmez (F3).
5. **Parayı metin olarak saklamak.** Opsiyonel alanların tek kovası locale'li metin ve
   tanımlı tek opsiyonel alan bir tutar. En kolay yol — "hepsi metin" — Altın Kural 4'ü
   doğrudan deler ve hata sessizdir: tramer tutarı bir dizge olarak girer, hiçbir test
   kırılmaz (C23).
6. **Liste sözleşmesini sessizce kırmak.** `0010a` bir hafta önce merge edildi; dönüş tipini
   `PagedResult`e çevirmek onun testlerine dokunur. Yazılmayan sözleşme kırılması
   SCORECARD'a **regresyon** olarak döner.
7. **Kısmi güncelleme semantiği.** Eksik gelen alanı "dokunulmadı" saymak, çekirdek alan
   invariantını test edilemez kılar (C25).

### Verilen kararlar

Numaralandırma `0010a`'nın **D16**'sından devam eder: karar defteri iki spec arasında tek
eksendir ve `0010b`, `0010a`'nın D9'undan doğdu.

**D17 — Opsiyonel alanlar tipli hâle gelir; tip kümesi kapalıdır.**
`metin` · `locale'li metin` · `tam sayı` · `tutar` · `evet/hayır` · `tarih` · `seçenek`.
`damage_record_amount` **tutar** tipindedir ve `Money` ile saklanır.
*Gerekçe:* bugün tanımlı tek opsiyonel alan bir para alanı ve depo tipi locale'li metin;
parayı metin olarak saklamak Altın Kural 4 ve Yasak Listesi 26'nın ihlalidir. Ayrıca
`docs/architecture.md` § 4'ün opsiyonel alan listesi çok tipli — bool, tam sayı, tarih,
seçenek — ve hepsini tek metin kovasına koymak hiçbirini doğrulanabilir kılmaz.
*Bilerek ödenen bedel:* `SetOptionalFieldValue`'nun imzası değişir ve bu `0010a` C17'ye
dokunur; istisna **C23**'te adıyla yazılıdır. *Elenen alternatif:* tipleri ayrı bir spec'e
bırakmak — o zaman bu spec, bilinen tek opsiyonel alanı kuralı delerek kaydeder.

**D18 — Alan ayarları yazma yüzeyi bu spec'te yapılmaz.**
*Gerekçe:* ayar ekranı kendi yetki ve önizleme kriterlerini getirir. *Bilerek ödenen bedel:*
galeri bir alanı zorunlu veya kapalı **yapamaz**; R7–R9'un davranışı yalnız testte ve seed
verisiyle görülür. Bu bedel **X21**'de açıkça yazılıdır — aksi hâlde iki gereksinim hiç
tetiklenmeyen bir koşula bağlı kalır ve kimse fark etmez.

**D19 — Düzenleme tam değiştirmedir.**
*Gerekçe:* **C25**. *Bedel:* uç her istekte çekirdek alanların tamamını bekler; form kısmi
gönderim yapamaz.

**D20 — Şase numarası düzenlenemez.**
*Gerekçe:* şase fiziksel aracın kimliğidir; değişmesi "aynı kayıt, başka araç" demektir ve
**A4/A5**'in izlediği şeyi bozar. *Bilerek ödenen bedel:* yanlış girilen şase için kayıt
arşivlenip yenisi açılır — **A4**'ün zaten öngördüğü yol. *Elenen alternatif:* düzenlemeye
izin verip tekilliği yeniden sınamak — tekillik korunur ama araç kimliği oynar.



---

#### Uygulama planıyla birlikte verilen kararlar (D21–D29)

> Onaylandı 2026-10-10. Bu dokuzu planın *"Riskler ve emin olmadığım noktalar"* bölümünden
> çıktı; her biri bir uygulama sorusunun cevabıdır ve R/C/AC'ye dokunmaz.

**D21 — Opsiyonel alanın TİPİ kodda yaşar, ayar tablosunda değil.**
`VehicleFieldDefinitions` alan anahtarını tipe bağlar; `vehicle_field_settings` yalnız iki
operatör eksenini (form durumu, görünürlük) taşımaya devam eder.
*Gerekçe:* **F5** / Yasak Listesi 40 alan listesini "kodda tanımlı, sabit, versiyonlu" ilan
ediyor. Tip bir operatör ayarı DEĞİLDİR: galeri bir alanın tipini değiştirirse o alanda
saklanmış bütün değerler anlamsızlaşır ve bozulma sessizdir.
*Bilerek ödenen bedel:* yeni tip eklemek dağıtım ister.
*Elenen alternatif:* `value_type` kolonu — ileride bir ayar ekranının onu düzenlemesini
mümkün kılardı.

**D22 — Tipli değer TİPLİ KOLONLARDA saklanır.**
`value_localized` (bugünkü `value` kolonunun yeniden adlandırılması — veri kaybı yok),
`value_text`, `value_integer`, `value_amount numeric(19,4)` + `value_currency` +
`value_minor_unit`, `value_boolean`, `value_date`, `value_slug`.
*Gerekçe:* vitrin okuma modeli parayı **zaten tam olarak bu üçlüyle** tutuyor
(`PriceAmount` / `PriceCurrencyCode` / `PriceMinorUnitExponent`); aynı deseni yazma
modelinde tekrarlamak D17'nin "tutar `Money` ile saklanır" cümlesinin birebir karşılığıdır.
Karar defteri de para için `decimal(19,4)` diyor.
*Bilerek ödenen bedel:* seyrek ve geniş tablo — bugün tanımlı tek opsiyonel alan var.
*Elenen alternatif:* tek `value jsonb` tipli zarf — bir kolon, ama parayı JSON sayısına
çevirir ve `numeric` kadar kesin değildir.
*Kapsam dışı bırakılan:* `ListingPrice`'ın `0010a`'da seçilen **tek metin kolonu** biçimi
bu spec'te DEĞİŞTİRİLMEZ; `0010a` AC-22 ondalık ölçeğin korunduğunu kanıtlıyor ve
değiştirmek kapsam dışı bir yeniden yazım olurdu.

**D23 — Düzenleme: çekirdek alanlar TAM DEĞİŞTİRME, opsiyonel alanlar BİRLEŞTİRME, ilan
metni TAM DEĞİŞTİRME.**
*Gerekçe:* C25 çekirdek için tam değiştirmeyi zorunlu kılıyor. Opsiyonel alanlarda aynı
kuralı uygulamak **AC-17'yi düşürürdü**: kapalı alan formda görünmez (R7) → istekte
taşınmaz → "gelmeyen = silindi" demek **F1**'i deler ve kapatmayı silmeye çevirir. İlan
metni her zaman etkin dillerin tamamıyla gönderilir; bir dili boşaltmak ancak böyle ifade
edilebilir.
*Bilerek ödenen bedel:* bir opsiyonel değeri temizlemek için açık boş değer gerekir;
"hiç göndermemek" temizlemez.

**D24 — Şase istekte TAŞINIR ve farklıysa reddedilir.**
*Gerekçe:* AC-8 "değiştirme girişimi reddedilir" diyor; şase gövdede hiç yoksa bir girişim
**ifade edilemez** ve kriter ölçülemez hâle gelir. C25'in "çekirdek alanların tamamı"
kuralıyla da tutarlı. Form alanı salt okunur basar.
*Elenen alternatif:* şaseyi gövdeden çıkarmak — daha sade ama AC-8'i test edilemez kılar.

**D25 — `vehicles.updated_at` eklenir; "yeni eklenen" sırası kimlikten gelir.**
Sıralamalar: *son güncellenen* = `updated_at DESC, id DESC`; *yeni eklenen* = `id DESC`
(UUID v7 zaman sıralı). Kırıcı her ikisinde de araç kimliğidir (R14).
*Gerekçe:* ayrı bir `created_at` kolonu, kimliğin zaten taşıdığı bilgiyi ikinci kez saklamak
olurdu.

**D26 — "Çeviri eksik" genel sayacı dar projeksiyonla bellekte hesaplanır.**
*Gerekçe:* `LocalizedText` bir değer dönüştürücüsüyle saklanıyor; EF onu dize olarak görür
ve jsonb operatörlerine çeviremez. İki kolonluk dar bir projeksiyon bir galeri ölçeğinde
ucuzdur.
*Bilerek ödenen bedel — PR'da yazılır:* bu bir **tam taramadır**; ölçek büyüdüğünde ham SQL
(`title ? 'tr' AND description ? 'tr'`) gerekir.
*Elenen alternatif:* sayıyı saklamak — **C21**/**C7**'nin yasağı.

**D27 — Kapalı bir alana değer gönderen istek REDDEDİLİR.**
*Gerekçe:* formun o alanı göstermesi imkânsızdır (R7); isteği ancak bayat bir sekme veya
elle kurulmuş bir çağrı taşır. Sessizce yutmak bu depoda tekrar eden ders — `0009`'un
tanınmayan slug'ı, `0010a`'nın etkin olmayan locale'i.
*Not:* hiçbir kriter bunu söylemiyor; karar bilinçli olarak yazıya geçirildi.

**D28 — Vitrin yüzeyi bu spec'te DEĞİŞMEZ.**
(a) İlan başlığı/açıklaması vitrin okuma modeline taşınmaz. (b) `tutar` ve `tarih` tipli
**public** bir alan vitrine yansıtılmaz ve bunu bir **tripwire testi** tutar.
*Gerekçe:* biçimlendirme uçta yapılır (`docs/conventions.md` § 7); tipli bir değeri sunucuda
metne çevirmek Arapça'da yanlış rakam ve ayraç üretir. Doğru çözüm okuma modeline tipli bir
kanal açmaktır ve o `0002`'nin sözleşmesini değiştirir — kapsam dışı. Bugün public opsiyonel
alan yok, yani bedel sıfır; tripwire birisi bir para alanını public yaptığında sessiz
kalmamasını sağlar.
*Bilerek ödenen bedel — PR'da yazılır:* personelin yazdığı ilan metni `0012` (yayın) gelene
kadar hiçbir ziyaretçiye görünmez.

**D29 — Düzenleme adresi `/admin/vehicles/{id}`; panel rotası kimlik taşır.**
*Gerekçe:* liste `/admin/vehicles`, düzenleme onun altındaki kayıt. Bugünkü `AdminView`
kapalı birleşimi kimlik taşımıyor; `parseRoute`'un panel dalı üçüncü parçayı okuyacak.

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler görsel kanıt gerektirir (`docs/testing.md` § 3).
> Numaralandırma bu spec'te **AC-1'den** başlar; `0010a`'nın numaraları ayrı eksendir ve
> testler `0010b/AC-<n>` önekiyle ayrılır (`docs/testing.md` § 1).
> Alan ayarı değişikliği isteyen kriterlerin gözlem noktası **C24**'tedir.

### A. Bugün karşılanıyor — regresyon koruması

- **AC-1.** Oturumu olmayan biri araç düzenleyemez; istek reddedilir.
  *(Yönetim kökü grup düzeyinde yetki taşıyor ve `0006` C18'in rota tarayan testi her yeni
  ucu otomatik kapsıyor; bu kriter korumanın kaybolmadığını gösterir, yeni davranışın kanıtı
  değildir.)*

### B. Düzenleme

- **AC-2.** Kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür.
- **AC-3.** Düzenlemede de çekirdek alanlardan biri boşaltılarak kaydedilemez.
- **AC-4.** Var olmayan bir araç kimliğiyle gelen düzenleme isteği `404` ve kararlı bir hata
  koduyla reddedilir.
- **AC-5.** Çekirdek alanlardan birini **hiç göndermeyen** düzenleme isteği reddedilir;
  eksik alan "dokunulmadı" sayılmaz (C25).
- **AC-6.** Düzenlemede fiyat değiştirildiğinde fiyat geçmişine yeni bir kayıt yazılır ve
  eski kayıt olduğu gibi kalır.
- **AC-7.** Düzenlemede "fiyat sorunuz"dan belirli bir fiyata geçiş de fiyat geçmişine
  yazılır.
- **AC-8.** Şase numarası düzenlenemez; değiştirme girişimi reddedilir.

### C. Opsiyonel alanlar ve tipleri

- **AC-9.** Her opsiyonel alan R6'daki kapalı kümeden bir tipe sahiptir; tipi beyan
  edilmemiş bir alana değer kaydedilemez.
- **AC-10.** `damage_record_amount` **tutar** tipindedir ve `Money` ile saklanır; metin
  olarak saklanmaz.
- **AC-11.** Tutar tipli bir alana sayı olmayan bir değer verildiğinde istek reddedilir.
- **AC-12.** Tarih tipli bir alana geçerli bir tarih olmayan bir değer verildiğinde istek
  reddedilir.
- **AC-13.** `[görsel]` Alan ayarlarında **açık** bir opsiyonel alan formda görünür ve değeri
  kaydedilir.
- **AC-14.** `[görsel]` Alan ayarlarında **kapalı** bir opsiyonel alan formda görünmez.
- **AC-15.** Alan ayarlarında **zorunlu** işaretli bir opsiyonel alan boş bırakılmış araç,
  taslak olarak **kaydedilebilir**.
- **AC-16.** `[görsel]` Bir alan zorunlu yapıldığında, o alanı boş olan mevcut araçlar
  düzenlemeye kapanmaz ve listede **"eksik bilgi"** olarak işaretlenir.
- **AC-17.** Kapatılan bir alanın daha önce girilmiş değeri silinmez; alan yeniden
  açıldığında eski değer görünür.

### D. Çok dilli ilan metni

- **AC-18.** Personel ilan başlığını ve açıklamasını TR, EN ve AR için ayrı ayrı yazar ve
  üçü bağımsız saklanır.
- **AC-19.** Yalnızca bir dilde metni olan araç kaydedilebilir.
- **AC-20.** İlan başlığı veya açıklaması locale başına üst uzunluk sınırını aştığında istek
  reddedilir.
- **AC-21.** Bir dil, **başlık ve açıklamasının ikisi de** doluysa tam sayılır; yalnız biri
  dolu olan dil **eksik** sayılır.
- **AC-22.** `[görsel]` Panel, metni eksik olan dilleri araç başına gösterir.
- **AC-23.** Liste, çeviri eksik ilan sayısını **tüm araçlar** üzerinden gösterir; gösterilen
  sayı o anki sayfanın içeriğine bağlı değildir.

### E. Liste

- **AC-24.** `[görsel]` Liste araç tanımını, fiyatı, kilometreyi, yayın durumunu, eksik dil
  göstergesini, **eksik bilgi** göstergesini ve son güncelleme zamanını gösterir.
- **AC-25.** Liste yanıtı `PagedResult<T>` biçimindedir ve sayfa, sayfa boyutu ile toplam
  kayıt sayısını taşır.
- **AC-26.** Üst sınırı aşan bir `pageSize` isteği üst sınıra kırpılır ve hata dönmez (C26).
- **AC-27.** Yapılandırılan sayfa boyutunun **bir fazlası** kadar araç varken ikinci sayfa
  tam olarak bir araç gösterir.
- **AC-28.** Ekleme ve güncelleme sıraları birbirinden farklı olan **en az üç araçla**,
  **son güncellenen** ve **yeni eklenen** sıralamaları farklı sıralar üretir.
- **AC-29.** Son güncelleme zamanı aynı olan araçlarda sıralama **araç kimliğiyle** kırılır;
  birinci ve ikinci sayfa birlikte hiçbir aracı tekrarlamaz ve hiçbirini atlamaz.

### F. Durumlar

- **AC-30.** `[görsel]` Liste yüklenirken iskelet gösterilir.
- **AC-31.** `[görsel]` Hiç araç kaydı yokken liste, henüz araç olmadığını bildirir ve
  **araç ekle** eylemini sunar.
- **AC-32.** `[görsel]` Liste yüklenemediğinde ekran, çeviri kataloğundaki hata mesajı
  anahtarının karşılığını ve **tekrar dene** eylemini gösterir.
- **AC-33.** Hiçbir hata ekranında HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya
  istisna metni görünmez.

### G. Dil ve RTL

- **AC-34.** `[görsel]` Arapça tercih eden personel için düzenleme formu ve liste
  `dir="rtl"` ile basılır.
- **AC-35.** `[görsel]` Liste başlıkları ve göstergeler TR, AR ve EN'de kendi dilinde
  görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] `[görsel]` işaretli **her** kriterin kanıtı eklendi (`docs/evidence/0010b-ac<n>.png`),
      boş ve hata durumları dâhil; AC-34 için Arapça görüntü de var.
- [ ] **A grubundaki kriterin bu spec'ten önce de yeşil olduğu** PR'da yazıldı; yeni
      davranışın kanıtı B–G gruplarıdır.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] Yeni migration varsa yalnızca `vehicle_catalog` şemasına dokunuyor.
- [ ] **"Eksik bilgi" hesaplanıyor, saklanmıyor** (C21) ve ayar değişince hiçbir yazma
      olmadan değiştiği testle gösterildi.
- [ ] **Sayfalama kararlılığı** birinci ve ikinci sayfa BİRLİKTE alınarak gösterildi
      (AC-29): çakışma yok, atlama yok.
- [ ] **`0010a`'nın liste kriterleri kesintisiz yeşil kaldı** — `IVehicleAdminQueries` ve
      `VehicleAdminListItemDto` bu spec'te değişiyor; değişiklik öncesi ve sonrası koşu
      çıktıları PR'da (Context § Doğrulanmış bulgular).
- [ ] **Opsiyonel alan tipleri `Money` ve tarih için testle gösterildi** (AC-10, AC-11,
      AC-12); hiçbir para değeri metin olarak saklanmıyor (C23, Altın Kural 4).
- [ ] **C23'ün C17 istisnası** PR açıklamasında adıyla yazıldı ve kapsamı `0010a` C17'nin
      geri kalanını etkilemiyor.
- [ ] **X20'nin ve X21'in kabul edilen riskleri** PR açıklamasında yazıldı.
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi **yok**.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] Ekranlar `docs/frontend.md` § 4.2 envanterini kullanıyor; ham piksel/hex eklenmedi.
- [ ] **Dış gözden geçirme yapıldı:** PR açılmadan önce bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`).
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? **`0` ile `—` aynı şey değildir.** |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek, kaçı gürültü? |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
