# ADR 0003 — Mockup Kaynaklı Kapsam Farkının Yönetimi

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 2026-10-04 |
| **Karar veren** | Takım Yöneticisi |
| **İlgili** | `docs/architecture.md` § 4, § 6, § 7; `docs/domain.md` § 1, § 2.4; `specs/done/0001`, `0002`, `0005`, `0006`; ADR 0001 |

---

## Bağlam

Ürünün frontend mockup'ları tamamlandı: tek bir tasarım tuvalinde **10 artboard** —
ana sayfa, araç listesi, ilan detayı, mobil ilan detayı, Arapça (RTL) ana sayfa, panel girişi,
iki adımlı doğrulama, panel ilan listesi, panel yeni ilan formu ve panel talepler ekranı.

Mockup'lar incelendiğinde **ürün hedefinin, bugüne kadar teslim edilenin belirgin biçimde
ötesinde olduğu** görüldü. Daha önemlisi: mockup'lar, tamamlanmış dört spec'in **bilinçli
olarak kapsam dışı bıraktığı** yedi şeyi geri açıyor.

**Doğrulanmış çelişkiler**

| Mockup'ta olan | Hangi karara aykırı |
|---|---|
| Ziyaretçi seçimli sıralama (8 seçenek) | `0001` **X4** + **AC-4**: sıralama sabit |
| 8 filtre ekseni (marka, yıl, km, yakıt, vites, fiyat, tramer) | `0001` **X2**, **X3**: yalnızca kasa tipi |
| Marka arama kutusu | `0001` **X1**: serbest metin arama yok |
| Favori sayacı ve kart üzerinde favori düğmesi | `0001` **X5**; § 6: favoriler `localStorage` |
| Medya galerisi (1/18 + şerit) | `0002` **X2** |
| 13 parçalık boyalı/değişen görsel şeması | `0002` **X3** |
| Benzer araçlar bölümü | `0002` **X5** |
| Takas talebi formu | `0005` **D3**, **X1** |
| Talep paneli (liste, detay, iç not, durum) | `0005` **X5** |
| İki adımlı doğrulama (TOTP + kurtarma kodu) | `0006` **X2** |
| "Kullanıcı adı" ile giriş | `0006` **R1**: giriş kimliği e-posta |
| 8 hatalı denemede kilit | `0006` **D4**: 5 deneme |
| "Sahip" rolü; "fiyat alanı kilitli" | `0006` **R2**; architecture § 1: üç rol |
| Talep durumları: Okundu / Görüşüldü / Kapandı—satıldı / Kapandı—ilgilenmedi | `docs/domain.md` **L4**: `Yeni → Atandı → İletişimde → Sonuçlandı` |
| Müşterinin yazdığı mesaj alanı | `0005` **R4**: ad + iletişim kanalı; mesaj yok |
| "Pazar profili: Türkiye" | Sözlükte böyle bir terim yok |

**Doğrulanmış boşluk — en kritik olanı**

`VehicleCatalog`'un **tek bir yazma ucu yoktur.** Projedeki tüm `MapPost`/`MapPut` uçları
`Identity` (personel) ve `Leads` (talep) modüllerine ait; araç migration'ları arasında araç
seed'i de yok. **Galeri bugün sisteme araç ekleyemez.** Mockup'ın "yeni ilan" ve "panel ilan
listesi" artboard'ları tam bu boşluğu dolduruyor.

Spec'i hiç olmayan diğerleri: ana sayfa, bülten aboneliği, markalar sayfası, `Content`
sayfaları (Hakkımızda/İletişim/KVKK/Çerez), karşılaştırma, `Media` (filigran + 1600/800/400
WebP türevleri), toplu eylemler, çeviri eksiği sütunu, dışa aktar, şube çalışma saatleri.

**Asıl gerilim**

Dört spec tamamlandı, kodu merge edildi ve SCORECARD'ları dolduruldu. Mockup'lar o spec'lerin
kapsam dışı kararlarını geçersiz kılıyor. İki yanlış yol var: onaylı ve kapanmış spec'leri
geriye dönük değiştirip tarihi silmek, ya da mockup'ları yok sayıp ürün hedefini
tasarımdan koparmak.

---

## Karar

**Mockup'lar ürün hedefinin gerçek kaynağıdır. Tamamlanmış spec'ler geriye dönük
değiştirilmez; aradaki fark yeni spec'lerle kapatılır.**

Kararın bağlayıcı unsurları:

1. **Tamamlanmış spec dokunulmazdır.** `specs/done/` altındaki bir spec'in Requirements,
   Acceptance Criteria veya KAPSAM DIŞI bölümü, mockup'a uydurmak için değiştirilmez. Bir
   kapsam dışı karar geri açılıyorsa, onu açan **yeni spec** bunu kendi Context'inde
   *"şu spec'in şu X maddesini geri açıyor"* diye adıyla yazar.
2. **İki adımlı doğrulama kapsama girer.** `0006` § X2 kararı açılmıştır. 2FA (TOTP,
   kurtarma kodu, sahip tarafından sıfırlama) **ayrı bir spec**'tir ve `Notifications`
   modülünden **sonra** yapılır; kurtarma ve sıfırlama akışları bildirim gerektirir.
3. **Dördüncü rol eklenmez.** Mockup'taki "Sahip" rolünün işlevini mevcut **Yönetici**
   rolü taşır. Roller `docs/architecture.md` § 1'de yazılı üç tanedir. Mockup'taki
   "fiyat alanı kilitlidir" ise **alan düzeyi yetki** demektir ve bu **ayrı bir karardır** —
   § 6'nın "şube bazlı yetkilendirme / kayıt seviyesi izin" kararına komşudur, onunla
   birlikte ele alınır.
4. **Sözlük kazanır.** Talep durumları `docs/domain.md` **L4**'teki dörttür. Mockup'taki
   "Okundu" bir **durum değildir**; durumdan bağımsız bir **damga**dır (kim, ne zaman okudu) —
   mockup'ın kendisi de bunu "[KULLANICI ADI] tarafından okundu · 16:58" diye ayrı gösterir.
   Panelin durum seçicisi sözlüğe göre etiketlenir.
5. **Talebe mesaj alanı eklenir.** Müşterinin yazdığı serbest metin, geldiği dilde saklanır.
   Bu, `0005` tamamlandığı için **yeni bir spec**'te yapılır (unsur 1).
6. **"Pazar profili" bir terim değildir.** Mockup'ta o adla görünen şey, **Alan Ayarı**
   (`VehicleFormSettings`) ile kurulum yapılandırmasının birleşimidir; panelde "Alan ayarları"
   olarak etiketlenir. Aynı kavrama ikinci bir isim verilmez. Profil seçimiyle alan kümesinin
   değişmesi, § 4'ün *"kullanıcı yeni alan tanımlayamaz"* kalıcı sınırına fazla yaklaşır.
7. **Sıra şudur** ve kapsam kararı olmadan değişmez:

   | # | İş | Neden bu sırada |
   |---|---|---|
   | 1 | Talep bildirimi (`0009`, Taslak) | `0005`/D5'in borcu; talepler birikiyor, kimse görmüyor |
   | 2 | **Panel: araç/ilan yönetimi** (yeni ilan + ilan listesi) | Galeri araç ekleyemiyor; vitrin bu olmadan beslenemez |
   | 3 | `Media` (filigran, WebP türevleri, R2) | 2'nin fotoğraf akışı buna dayanır |
   | 4 | Ana sayfa | Vitrinin girişi |
   | 5 | Liste: sıralama + filtre eksenleri | `0001` X2/X3/X4'ü açar |
   | 6 | Medya galerisi + parça haritası şeması | `0002` X2/X3'ü açar |
   | 7 | `Content` sayfaları | Hakkımızda/İletişim/KVKK/Çerez |
   | 8 | Favori + karşılaştırma | `0001` X5'i açar |
   | 9 | Bülten aboneliği | `Notifications` + abonelikten çıkma |

---

## Sonuçlar

### İyi taraf

- **Tarih yalan söylemez.** Tamamlanmış spec'lerin SCORECARD'ları, o iş yapılırken neyin
  bilindiğini göstermeye devam eder. "0001 neden sıralama içermiyor" sorusunun cevabı
  spec'in kendisinde kalır.
- **Fark izlenebilir.** Her geri açılan kapsam dışı madde, onu açan spec'te adıyla yazılı
  olur; iki yıl sonra "bu neden değişti" sorusu tek bir dosyaya gider.
- **Sıra, görünür değere göre değil bağımlılığa göre kurulur.** Galeri araç ekleyemezken
  favori veya ana sayfa yapmak vitrini boş bırakır; unsur 7 bunu engeller.
- **Sözlük tek kalır.** Unsur 4 ve 6, mockup'ın getirdiği iki yeni ismi sözlüğe sokmadan
  karşılar.

### Bedel

- **Spec sayısı artar.** Aynı ekrana iki spec dokunur: biri onu kuran, diğeri genişleten.
  Bir ekranın bugünkü davranışını anlamak için iki dosya okumak gerekir.
- **`0006` iki kez açılır.** 2FA (unsur 2) giriş akışını yeniden test ettirir; `0006`'nın
  kabul kriterlerinin bir kısmı yeni spec'te tekrar doğrulanır.
- **Mockup ile ürün bir süre ayrışık kalır.** Unsur 7'nin sırası gereği ziyaretçi tarafındaki
  görünür iyileştirmeler (sıralama, filtre, galeri, favori) panel işinin arkasına düşer;
  mockup'ı gören biri ürünü eksik bulur. Bu bilerek ödeniyor.
- **Mockup'ın üç metni değişir.** "Sahip" (unsur 3), durum etiketleri (unsur 4) ve
  "Pazar profili" (unsur 6) tasarım tarafında güncellenmelidir; aksi hâlde tasarım,
  yapılmayacak bir şeyi vaat eder.

### Ne zaman yeniden değerlendirilir

- **Mockup yeniden çizilirse** ve bu ADR'nin unsur 3, 4 veya 6'sına aykırı bir şey getirirse:
  karar yeniden açılır, mockup sessizce kazanmaz.
- **Unsur 7'nin sırası bozulmak istenirse:** bu bir kapsam kararıdır, Takım Yöneticisi açar.
- **Alan düzeyi yetki (unsur 3) gerçekten gerekirse** — örneğin "danışman fiyatı
  değiştiremesin" somut bir talebe dönüşürse — § 6'nın izin motoru tetikleyicisiyle birlikte
  değerlendirilir.
- **Tamamlanmış bir spec'in kodu mockup'la uyumsuz çalışıyorsa** (kriterine aykırı davranış),
  bu kapsam değil **hata**dır: `fix/` branch'i ve `AP-06` yolu geçerlidir, bu ADR devreye girmez.

---

## Değerlendirilen Alternatifler

### 1. Tamamlanmış spec'leri revize etmek

`0001`, `0002`, `0005`, `0006` yeniden açılır; kapsam dışı maddeler silinir, yeni
Requirements ve kriterler eklenir.

**Neden elendi:** dördünün de SCORECARD'ı dolu ve kodu merge edilmiş. Revizyon, o
scorecard'ları anlamsız kılar — "spec revizyon sayısı" iş bittikten haftalar sonra artar ve
ölçüt neyi ölçtüğünü kaybeder. Ayrıca `specs/done/` arşiv olmaktan çıkar: bir dosya hem
"tamamlandı" hem "değişiyor" olamaz.

### 2. Mockup'ları kararlara uydurup kırpmak

Sıralama, filtreler, favori, galeri, 2FA mockup'tan çıkarılır; tasarım mevcut kapsama
indirilir.

**Neden elendi:** mockup'lar ürün hedefini taşıyor, mevcut teslimi değil. Tasarımı teslime
indirmek, hedefi teslimin boyuna kırpmak olur — ve bu ürünü bir daha büyütmez. Ayrıca
mockup'taki bazı şeyler (araç ekleme paneli) kırpılamaz: onlar olmadan ürün çalışmıyor.

### 3. Tek büyük "v2" spec'i

Tüm fark tek bir spec'te toplanır.

**Neden elendi:** dokuz ekran, altı modül, iki yeni modül (`Media`, `Content`) ve bir
altyapı işi. `AGENTS.md` § Çalışma Disiplini 4'ün "küçük dilimler" kuralını ihlal eder;
kabul kriterleri ortak bir hakeme bağlanamaz ve tek PR'a sığmaz. Bu, `0006`'nın
"destek modülleri" isteğinde zaten bir kez elenmiş bir biçim.

### 4. Mockup'ları keşif sayıp kayda geçirmemek

Tasarım ilham olarak kalır; spec'ler mevcut kararlarla yürür.

**Neden elendi:** mockup'lar somut vaatler içeriyor — giriş ekranındaki "ikinci adımda
doğrulama kodu istenir" metni gibi. Kayda geçirilmeyen vaat, altı ay sonra "bunu yapacaktık"
tartışmasına dönüşür ve o tartışmanın hakemi olmaz.

---

## Ek 1 — Sıranın tamamlanması ve ondan sapmalar

*2026-10-04. Unsur 7'nin dokuz adımı, o yazıldığında var olmayan üç spec'i kapsamıyordu.
ADR "kapsam kararı olmadan değişmez" dediği için yerleşimleri burada kayda geçiyor.
Ayrıca bir adım sırasının dışında yapıldı; onu gizlemek bu ADR'nin varlık sebebini
ortadan kaldırırdı.*

### Sıraya eklenen üç spec

| Spec | Yeri | Gerekçe |
|---|---|---|
| **`frontend-002`** görsel dil ve marka teması | Unsur 7 **#2'den ÖNCE** | Token katmanına dokunuyor; panel ekranları (#2) ondan sonra yazılmazsa aynı ekranlar iki kez boyanır ve `frontend-002` onları da taşımak zorunda kalır. Spec'in Intent'i bugünkü `#1f5fd0` vurgu rengini *"hiç kimsenin verdiği bir karar değil"* diye tanımlıyor ve `docs/frontend.md` § 4.1'in yasak saydığı türden olduğunu söylüyor. |
| **`0008`** personel yönetimi eylemleri | Unsur 7 #2'den **sonra** olmalıydı — **sıradan önce yapıldı**, aşağıya bkz. | Araç ekleyemeyen bir galeride personel pasifleştirme ikincil kalır; ayrıca #2 panelde yazma akışının desenini kurar ve `0008` onu yeniden kullanır. |
| **`0004`** SEO meta ve yapısal veri | Unsur 7 **#4'ten (ana sayfa) sonra** | SEO'nun değeri indekslenecek sayfa sayısıyla artıyor; ana sayfa ve liste eksenleri yoksa indekslenecek az şey var. Spec'in Intent'i hâlâ boş: *"başarı neye benziyor"* sorusu Takım Yöneticisi'nde. |

### Sapma — `0008` sıradan önce yapıldı

`0008` bu ADR'de #2'den sonraya konmuştu. Fiiliyatta **#2'den önce** uygulandı ve
`main`'e merge edildi (PR #25, 2026-10-04): 37 dosya, 42 kriter, CI 4/4 yeşil.

**Neden kayda geçiyor:** sıra bir tahmin değil, verilmiş bir karardı. Sapma zararsız
çıktı — `0008` yalnız `Identity` ve panel ekranına dokundu, `0010`'un dokunacağı
`VehicleCatalog` yazma yüzeyiyle hiç kesişmedi (37 dosyaya karşı `main`'in 81 dosyası,
ortak dosya yok) — ama zararsız olduğu **önceden bilinmiyordu**. Sıranın gerekçesi
bağımlılık değil değer önceliğiydi ve o gerekçe hâlâ geçerli: galeri bugün hâlâ araç
ekleyemiyor.

**Çıkarılan ders:** paralel çalışan oturumlar bu ADR'nin sırasını okumuyor. Sıra bir
belgede duruyor ama hiçbir mekanizma onu dayatmıyor. Bir sonraki sapma aynı şansı
taşımayabilir.

### `0010`'un kendi dilimlemesi — unsur 7 #2 ikiye bölündü

`0010`'un **D1** kararı, unsur 7'nin 2. maddesini üç dilime ayırıyor ve ilk dilimi kendisi
üstleniyor:

| # | Dilim | Durum |
|---|---|---|
| 1 | Araç kaydı, düzenleme, taslak ilan, ilan listesi (okuma) | `0010` (Taslak) |
| 2 | `Media`: yükleme, türev üretimi, filigran, depolama | spec yok |
| 3 | Yayın ve durum eylemleri + toplu eylemler | spec yok |

*Bu tablo **kararın verildiği andaki** durumu kaydediyor ve yeniden yazılmıyor. Bugünkü
karşılıkları: dilim 1 → `0010a` + `0010b` (ikisi de tamamlandı); dilim 2 → `0011`;
dilim 3 → `0012a` + `0012b` + `0012c` (2026-10-11'de **üçe** bölündü; **üçü de
tamamlandı**).
Güncel hâl aşağıdaki "Güncellenen sıra" tablosundadır.*

**Bu, unsur 7'yi reddetmez; 2. maddesini ikiye böler ve `Media`'yı (#3) yayın işinin
önüne çeker.** Gerekçesi doğrulanmış bir teknik kısıt: `Vehicle.EnsurePublishable()` en az
bir medya varlığı istiyor ve `MediaAssetReady` olayını yayan hiçbir şey yok. Yani panel
#2 olarak kalabilir ama **yalnızca taslak** teslim edebilir.

Sonucu: `0010` bittiğinde galeri araç girebilir ama **hiçbirini yayına alamaz**; vitrin
hâlâ seed'e bağlı kalır. `0010` bu bedeli açıkça yazıyor.

### Güncellenen sıra

**Durum sütunu üç kez güncellendi:** 2026-10-10'da önce her satırın hangi spec'e karşılık
geldiği yazıldı (`spec yok` satırı kalmadı — on bir işin de taslağı var), sonra `0010b`'nin
kapanışı işlendi; 2026-10-11'de `0011`'in kapanışı. **Sıra üçünde de DEĞİŞMEDİ.** İlk ikisinin
arasına bir boşluk girdi ve ölçüsü şu:
tablo `85373a0` ile 09:34'te güncellendi, `0010b` `710d3e8` ile 11:36'da arşive girdi ve
satır bundan **sonra** da "Taslak — sahip bekliyor" yazmaya devam etti; kapanış adımı bu
tabloya geri dönmedi. Bu tablo `specs/done/` kapısının (ADR `0008` / K8) **görmediği** bir
yerde duruyor — kapı yalnız kartın kendisini okur. Güncellenmesi kapanış adımının parçasıdır.
`0011`'in kapanışında bu satır **aynı commit'te** güncellendi; boşluk tekrarlanmadı.
*Bu tablo tek nüshadır: ikinci bir sıra tablosu yazmak yerine sütun güncelleniyor
(`CLAUDE.md`, tek gerçek kaynağı).*

| # | İş | Spec |
|---|---|---|
| 1 | Talep bildirimi | `0009` ✅ tamamlandı |
| — | Personel yönetimi eylemleri *(sıra dışı yapıldı)* | `0008` ✅ tamamlandı |
| 2 | **Görsel dil ve marka teması** | `frontend-002` ✅ tamamlandı |
| 3 | **Panel: araç kaydı ve taslak ilan** | `0010a` ✅ tamamlandı · `0010b` ✅ tamamlandı |
| 4 | `Media`: yükleme, türev, filigran | `0011` ✅ tamamlandı |
| 5 | Yayın ve durum eylemleri + toplu eylemler | `0012a` ✅ tamamlandı · `0012b` ✅ tamamlandı · `0012c` ✅ tamamlandı |
| 6 | İki adımlı doğrulama (unsur 2) | `0013` ✅ tamamlandı |
| 7 | Ana sayfa | **`0014`** (Taslak — S1–S4; **S1 bu sırayı sorguluyor**) |
| 8 | SEO meta ve yapısal veri | `0004` (**Beklemede** — sırası 7'den sonra) |
| 9 | Liste: sıralama + filtre eksenleri | **`0015`** (Taslak — S1–S5) |
| 10 | Medya galerisi + parça haritası şeması | **`0016`** (Taslak — S1–S4) |
| 11 | `Content` sayfaları | **`0017`** (Taslak — S1–S5) |
| 12 | Favori + karşılaştırma | **`frontend-003`** (Taslak — S1–S5) |
| 13 | Bülten aboneliği | **`0018`** (Taslak — S1–S5) |

`3`. satır `0010`'un **D9**'uyla ikiye bölündü ve **iki dilimi de kapandı**: `0010a` (araç
kaydı, 2026-10-05) ve `0010b` (düzenleme ve liste, 2026-10-10). `4`. satır `0011` ile kapandı
(2026-10-11). **`5`. satır da ikiye bölündü** — 2026-10-11 CLARIFY turu, `0012a` **D1**:
`0012a` (aracın medya yüzeyi) ve `0012b` (yayın ve durum eylemleri). Bölme sınırı **A2'nin
medya koşulu**: birinci dilim onu ölçülebilir kılar, ikinci dilim onu tüketir. **Birinci
dilim aynı gün kapandı** (`0012a`, 2026-10-11, `8fc862c`); sıra DEĞİŞMEDİ ve başlanmamış ilk
iş artık #5'in **ikinci dilimi** (`0012b`).

**İkinci dilim de aynı gün kapandı** (`0012b`, 2026-10-11, `e67a61d`) ve `5`. satır bölünmeye
**üçüncü kez** gitti: kriter turunda ölçülen **D39** ile toplu eylem ve öne çıkarma
`0012c`'ye ayrıldı. Bölme bu kez kriter yazılmadan yapıldı — kararlar `0012b`'den taşındı
(D3–D7, D16, D18, D27–D29, D36) — yani bedeli neredeyse sıfırdı. Sıra yine DEĞİŞMEDİ;
başlanmamış ilk iş artık #5'in **üçüncü dilimi** (`0012c`).

**Üçüncü dilim de kapandı** (`0012c`, 2026-10-12, `004c44c`) ve `5`. satır üçe
bölündükten sonra **ilk kez tamamen bitti**: altı toplu eylem ucu canlı (ölçüldü:
`bulk/publish|unpublish|sold|archive|feature|unfeature` → `401`; `bulk/reserve` ve
`bulk/unreserve` → `404`, **D18** gereği hiç tanımlanmadı). Üç dilimin toplamı 33 + 58 +
44 = **135 kriter** ve hiçbirinde karşılanmayan kriter kalmadı. Sıra bu kez **ilerledi**:
başlanmamış ilk iş artık **#6** (`0013` — iki adımlı doğrulama). Üç bölmenin ortak
ölçümü şu: ilk bölme kriterler yazıldıktan sonra yapıldı ve pahalıydı, sonraki ikisi
kriter yazılmadan yapıldı ve bedeli neredeyse sıfır oldu.

**`6`. satır kapandı ve bununla mockup farkının EN BÜYÜK kalemi kapsama girdi** (`0013`,
2026-10-16, `56c7c7d`). Yukarıdaki *"Mockup'ta olan / Hangi karara aykırı"* tablosunun
*"İki adımlı doğrulama (TOTP + kurtarma kodu) → `0006` **X2**"* satırı artık bir fark
değil: `0006`'nın kapsam dışı kararı **ADR 0003 unsur 2** ile zaten yeniden açılmıştı ve
bu kart onu bitirdi — **61 kriter**, **D1–D52** karar, iki CLARIFY turu + bir kriter turu.
Tablo satırı **yeniden yazılmıyor** (kararın verildiği anı kaydediyor); kapanışı bu
paragraf kaydediyor.

**Bu kart, yukarıdaki § Bedel'in iki maddesini ÖLÇÜLÜ hâle getirdi.** (1) *"`0006` iki kez
açılır"* gerçekleşti ve bedeli sayılabilir: giriş yanıtı ayrıştırıcılı oldu
(**D15**/**D43**), `CurrentSession` tipi ile `login()` imzası değişti, `0006`/AC-33'ün
modül testi uzatıldı, AC-22'ye üç yollu bir `Theory` eklendi, **on dört** e2e dosyası ve
**dokuz** `Host.Tests` zinciri ortak giriş yardımcısına geçti — hiçbirinin iddiası
değişmeden. (2) *"Mockup'ın üç metni değişir"* listesine bu kart iki metin daha ekledi ve
ikisi de **ölçümle** düştü: *"Sahip hesabı"* (rol yok — **D30**) ve *"8 hatalı deneme"*
(eşik **5** — **D8**, `IdentityOptions.DefaultFailedAttemptThreshold`).

Sıra yine **ilerledi**: başlanmamış ilk iş artık **#7** (`0014` — ana sayfa). `0013`'ün
kendi kapanış turunda ölçülen bir borç var ve kart onu yazılı bırakıyor: telefonunu
kaybeden **tek** Yöneticinin çıkış yolu yapılandırma değil veritabanı müdahalesidir
(**D37**), çünkü `0006` D2'nin kurulum hesabı yolu ikinci adım için çalışmıyor.

**Devir ikiden DÖRDE çıktı.** Bu paragraf 2026-10-11'de "`0011`'den iki iş devraldı (X7, X8)"
diyordu; CLARIFY turu ölçtü ve sayı yanlıştı: `0011` § **X3** (fotoğrafların araca
bağlanması, sıralanması ve kapak seçimi — "panel akışı `0012` ile gelir") ve **D16** (`alt`
metninin girişi) de aynı yere yazılıydı. Dördü `0012a`'ya düştü. Devrin kendisi yazılıydı
ama **toplamı** iki ayrı yerde iki farklı sayı veriyordu; sayıyı sonradan düzeltmek, devrin
yazılı olmasının neden yetmediğini gösteriyor.

`0011` bedelin **medya yarısını** ödedi: fotoğraf artık sisteme giriyor, türevleri ve
filigranı üretiliyor, `MediaAssetReady` yayılıyor ve duyuru dağıtıcı üzerinden vitrinin okuma
modeline ulaşıyor. **Yayına alma yine de yapılamıyor** ve sebebi değişti: artık "olayı yayan
yok" değil, (1) yüklenen varlığı bir araca bağlayan hiçbir akış yok — ölçüldü:
`Vehicle.AttachMediaAsset`'in tek üretim çağıranı geliştirme seed'i — ve (2) yayın eyleminin
kendisi yok; `EnsurePublishable()`'ın hiçbir üretim çağıranı bulunmuyor ve gövdesi hâlâ yalnız
`mediaAssetIds.Count > 0`'a bakıyor. Birinci sebep `0012a`'nın, ikincisi `0012b`'nin işi.
Vitrin bu yüzden hâlâ seed'e bağlı.

**`0012a` birinci sebebi kapattı** (2026-10-11). Yeniden ölçüldü: fotoğrafı araca bağlayan bir
üretim yolu artık **var** — `UpdateVehicleMediaCommand` → `Vehicle.ReplaceMediaAssets`, panelin
`PUT {admin}/vehicles/{id}/media` ucu. `EnsurePublishable`'ın gövdesi de genişledi: imzası
`EnsurePublishable(IReadOnlySet<Guid> readyMediaAssetIds)` ve kararı bağlı varlıklarla hazır
varlıkların **kesişimine** bakıyor (`Vehicle.cs:353`). **İkinci sebep aynen duruyor:** o metodun
hâlâ **hiçbir üretim çağıranı yok** — ölçüldü, tek çağıranı `VehicleTests.cs`'in beş testi — ve
yayın eyleminin kendisi `0012b`'nin işi (`0012a` D7, D18). Vitrin okuma modeline yazan üretim
yolu da hâlâ geliştirme seed'i (`0012a` D19).

**`0012b` ikinci sebebi de kapattı** (2026-10-11, `e67a61d`) ve bu ADR'ın "vitrin neden hâlâ
seed'e bağlı" gerekçesi **tümüyle geçersiz kaldı**. Yeniden ölçüldü: `EnsurePublishable`'ın
üretim çağıranı artık **var** — `PublishEligibility.cs:55`, yayın komutunun çağırdığı yol — ve
vitrin okuma modeline yazan üretim yolu da **var**: `ShowcaseProjectionWriter`, üç komuttan
çağrılıyor (`ListingTransitionCommands`, `UpdateVehicleCommand`, `UpdateVehicleMediaCommand`).
Yani vitrindeki araçlar bu commit'ten sonra geliştirme seed'inden değil **galerinin kendi
kararından** geliyor ve `docs/testing.md` § 3'ün üçüncü kritik akışı ilk kez uçtan uca
koşuyor. `0012a`'nın D18 ve D19 borçları böylece kapandı.

**Altı taslağın toplam 28 açık sorusu var ve hepsinin önerisi yazılı** (2026-10-16'da
yeniden ölçüldü: `0013` **0** — birinci CLARIFY turu S2'yi **D8** ile kapattı,
**ikincisi kalan dördünü** (**S1 · S3 · S4 · S5** → **D30–D33**) kapattı ve karar
bekleyen sorusu kalmadı · `0014` 4 · `0015` 5 · `0016` 4 · `0017` 5 · `0018` 5 · `frontend-003` 5; `0004` **Beklemede** ve sorusu yok, bu yüzden
taslak sayımına girmiyor. Taslak sayısı sekizden **yediye** indi — `0012c` kapandı — ama
**toplam değişmedi**: onun açık sorusu zaten `0`'dı, tek sorusu kendi CLARIFY turunda
**D43** ile kapanmıştı. Aynı şey bir kez daha oldu: taslak sayısı
yediden **altıya** indi — `0013` kapandı — ve **toplam yine değişmedi**, çünkü onun da
açık sorusu `0`'dı (S2 → **D8** ölçümle, S1 · S3 · S4 · S5 → **D30–D33**). İki kart üst
üste **sıfır açık soruyla** kapandı; sayımı düşüren şey soru cevaplamak değil, kartı
bitirmek oldu. Gün içindeki hareket: `0012a` ve `0012b` kapandı,
yerine `0012c` açıldı ve onun tek sorusu (**S1** — "hepsini seç" sayfa değiştiğinde ne olur)
aynı gün kendi CLARIFY turunda **D43** ile kapandı. Bir ara sürüm bu paragrafta taslak
sayısını dokuz yazıyordu; sayım `0004`'ü de katıyordu ve düzeltildi). `0012`'nin beş sorusundan dördü ilk
CLARIFY turunda kapandı; beşincisi (**rol ayrımı**) `0012b`'nin **ikinci** CLARIFY turunda
kapandı (**D22**: rol ayrımı yok, `0012a` X2 ile aynı karar). **`0012b`'nin karar bekleyen
sorusu kalmadı** ve sıradaki tur kriter turudur; o tur on yedi karar daha ekledi
(**D22–D38**), dördü spec'i bugünkü hâliyle yanlış kılan ölçümlerden. Hiçbir taslağın
kabul kriteri yok; cevaplar onları belirliyor. **`0013`'ün ikinci CLARIFY turu** yirmi
bir karar daha ekledi (**D13–D33**) ve ikisi spec'i uygulanamaz kılıyordu: ara durumu
taşıyan çerez kuralı ile kurulum uçlarının yeri **birbirini kilitliyordu** (**D13**) ve
bekleyen giriş yanıtı personelin adını, rolünü ve dilini ikinci adımdan önce veriyordu
(**D15**). Tur ayrıca **birinci turun bir ölçümünü düzeltti** (**D27**: dördüncü satır
eylemi hiçbir testi kırmıyor — iddia testin gövdesinde değil **başlığındaydı**); aynı
desen `0012b`'nin **D37**'sinde bir kez ödenmişti. `0013`'ün sıradaki turu **kriter
turudur**. `0014`'ün **S1**'i tablonun kendisini sorguluyor — üç bağımlılığı kendisinden
SONRA sıralanmış.

---

## Takip işleri

Bu ADR kararın **neden**ini taşır. Kararın **ne** olduğunu söyleyen tek satırlık kayıtlar
`docs/architecture.md` § 7 Karar Defteri'ne eklenmelidir:

| Konu | Karar |
|---|---|
| İki adımlı doğrulama | Kapsamda; `Notifications` sonrası ayrı spec (ADR 0003) |
| Personel rolleri | Üç rol; "Sahip" diye dördüncü rol yok (ADR 0003) |
| Alan düzeyi yetki | Ayrı karar; § 6'nın izin motoru tetikleyicisiyle birlikte (ADR 0003) |
| Tamamlanmış spec | Geriye dönük değiştirilmez; fark yeni spec'le kapanır (ADR 0003) |

`docs/domain.md` sözlüğünde **değişiklik gerekmez**: unsur 4 ve 6 yeni terim eklememe
kararıdır. Panelin durum etiketleri mevcut **Talep Durumu** değerlerine eşlenir.

*Bu tabloyu Analist yazmaz — § 7 güncellemesi, o kararı kapsayan spec'in
Definition of Done maddesidir.*
