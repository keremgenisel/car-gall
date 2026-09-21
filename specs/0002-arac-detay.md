# 0002 — Araç İlan Detayı

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | atanmadı |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (vitrin okuma tarafı) · `VehicleReference` (donanım paketi, yakıt, vites, çekiş — **E8**) · `frontend/` public vitrin |
| **Bağımlı spec'ler** | `0001` (tamamlandı) · **`frontend-001` (ÖNKOŞUL — önce tamamlanır, bkz. C14 ve Context § Verilen kararlar)** |

---

## Intent

Vitrin listesinde ilgisini çeken bir ilan bulan **ziyaretçi**, bugün o ilan hakkında karttaki
altı bilgiden fazlasını göremiyor: karta tıklayacak bir yer yok, gidecek bir sayfa yok. Bir
aracı sormaya değer bulup bulmadığına karar vermek için gereken bilgiler (kasa tipi, yakıt,
vites, renk ve galerinin vitrinde göstermeyi seçtiği diğer alanlar) hiçbir yerde görünmüyor.

**Galeri** bunu istiyor, çünkü ziyaretçinin **talep** bırakması ancak yeterince bilgilendiği
noktada olur; bugün vitrin, ziyaretçiyi tam o noktadan önce durduruyor.

Bu iş bittiğinde ziyaretçi, listedeki bir ilandan o ilanın kendi sayfasına gider ve orada
aracın vitrinde gösterilmesine izin verilen tüm bilgilerini görür. Sayfa paylaşılabilir bir
adreste yaşar. Vitrinde görünmemesi gereken bir ilanın adresi denendiğinde ziyaretçi,
"bulunamadı" karşılığını görür — ilanın hiç var olmadığı durumdan **ayırt edilemeyecek**
biçimde.

---

## Requirements

- **R1.** Ziyaretçi, vitrin listesindeki bir ilan kartından o ilanın detay sayfasına gidebilir.
- **R2.** Her ilanın kendi kalıcı ve paylaşılabilir adresi vardır. Adres, etkin her dilde
  **aynı Latin slug'ı** taşır; yalnızca dil öneki değişir.
  → `docs/architecture.md` § 3 (Arama, sıralama, normalizasyon → Slug).
- **R3.** Detay sayfası yalnızca vitrinde görünür olan ilanlar için açılır: `Yayında` ve
  `Rezerve` yayın durumundakiler.
- **R4.** `Rezerve` durumundaki ilanın detayı açılır ve üzerinde **rezerve rozeti** bulunur.
  → `docs/domain.md` **R6**.
- **R5.** Vitrinde görünmemesi gereken bir ilanın adresi istendiğinde sistem "bulunamadı"
  karşılığı verir ve ziyaretçiye bulunamadı ekranı gösterilir.
- **R6.** Hiç var olmayan bir ilan adresi istendiğinde sistem, **R5 ile aynı** karşılığı verir.
  İki durum ziyaretçi tarafından ayırt edilemez.
- **R7.** Detay sayfası aracın **çekirdek alanlarını** gösterir: model adı, yıl, kasa tipi,
  donanım paketi, kilometre, yakıt, vites, çekiş, renk, şube adı, kapak görseli ve
  fiyat **veya** "fiyat sorunuz".
- **R8.** Detay sayfası, alan ayarlarında **açık ve public** işaretli opsiyonel alanların
  değerlerini de gösterir. Kapalı veya `Gizli` işaretli hiçbir alan sayfada **ve** yanıtta
  bulunmaz — boş değer olarak da bulunmaz. → `docs/domain.md` **F3**, **F4**.
- **R9.** Detay sayfası **yükleniyor**, **bulunamadı** ve **hata** durumlarının üçünü de ele
  alır. → `docs/frontend.md` § 3.
- **R10.** Bulunamadı ve hata ekranlarında ziyaretçiye hiçbir teknik ayrıntı gösterilmez ve
  her iki ekran da bir sonraki eylemi önerir. → `docs/frontend.md` § 3.1.
- **R11.** Sayfa etkin üç locale'de (TR/AR/EN) çalışır; Arapça'da yön ve biçimlendirme RTL'dir.

---

## Constraints

### Uyulması zorunlu

- **C1.** Fiyat `Money` ile taşınır; çıplak `decimal` veya `double`/`float` kullanılmaz.
  Üç ondalıklı para birimleri (KWD/BHD/JOD/OMR) doğru gösterilir.
  → `AGENTS.md` Altın Kural 4, `docs/domain.md` **P1–P3**, Yasak Listesi 26.
- **C2.** "Fiyat sorunuz" ayrı bir durumdur, `0` değildir. → `docs/domain.md` **P4**.
- **C3.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner, çeviri uçta yapılır.
  → `AGENTS.md` Altın Kural 6, `docs/frontend.md` § 3.1, Yasak Listesi 23–24.
- **C4.** Gizleme `VehicleCatalog`'un sorgu tarafında uygulanır, sunum katmanında değil.
  → Yasak Listesi 41, `docs/architecture.md` § 4.
- **C5.** Modül sınırı: detay yalnızca `VehicleCatalog`'un kendi okuma modelinden beslenir.
  Şube adı denormalize tutulabilir, ancak yalnızca `Dealership` olaylarından beslenerek ve
  o kopya düzenlenmeden. → Yasak Listesi 1, 33.
- **C6.** CSS'te fiziksel yön özellikleri kullanılmaz; logical properties zorunludur.
  → Yasak Listesi 25, `docs/frontend.md` § 6.
- **C7.** Sayı içeren cümleler dize birleştirmeyle kurulmaz; ICU mesaj formatı zorunludur.
  → Yasak Listesi 31, `docs/frontend.md` § 5.
- **C8.** Locale öneki opsiyonel değildir; `Accept-Language`'a göre otomatik yönlendirme
  yapılmaz. → Yasak Listesi 28–29.
- **C9.** Yeni arayüz dosyaları `docs/frontend.md` § 1 klasör yapısına uyar; ekran
  `features/catalog/` altındadır.
- **C10.** Gömülü sabit değer yasaktır: hex renk, ham piksel, ham font boyutu. Değer
  token'dan gelir. → `docs/frontend.md` § 4.1.
- **C11.** Ekran önce **bileşen envanterine** bakar (`Badge`, `EmptyState`, `ErrorState`,
  `Skeleton`, `Card`, `Button`); envanterde karşılığı olanı yeniden yazmak reddedilir.
  → `docs/frontend.md` § 4.2. **Envanterin bugünkü durumu için bkz. Context.**
- **C12.** Her kabul kriteri en az bir testle karşılanır; test `DisplayName`'i
  `AC-<n>: <kriterin metni>` biçimindedir ve metin bu spec'ten **kopyalanır**.
  → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 1.
- **C13.** Bulunamadı karşılığı, ilanın var olup gizlendiği ile hiç var olmadığı durumu
  **ayırt ettirmez** — yanıt gövdesi, hata kodu ve süre farkı dâhil.
- **C14.** Bu spec'in uygulaması `frontend-001` tamamlanmadan başlamaz. Detay ekranı ortak
  bileşen envanterini (`Skeleton`, `ErrorState`, `Badge`) ve `tokens.css`'i **kullanır**;
  ikinci bir kopyasını yazmaz. → Karar E6, Context § Verilen kararlar.

### KAPSAM DIŞI

- **X1.** Talep (`Lead`) formu ve "bilgi al / geri arama / test sürüşü" eylemleri —
  *neden:* talep kendi domain kurallarını taşır (**L1** `SubmittedLocale`, **L2** araç anlık
  görüntüsü, **L3** şubeye yönlendirme, **L7** iletişim izni) ve tek başına bir spec'tir.
  **Sonucu:** `docs/domain.md` **R6**'nın "iletişim formu kapalı" yarısı bu spec'te
  uygulanamaz; kapatılacak form henüz yoktur. R6'nın bu yarısı talep formu spec'ine düşer.
- **X2.** Medya galerisi, görsel büyütme, video ve 360° tur — *neden:* detayın ilk sürümü
  kapak görseliyle çalışır; galeri kendi yükleme, önbellek ve erişilebilirlik kriterlerini
  getirir.
- **X3.** Boyalı–değişen parça haritasının **görsel şeması** (13 parça) — *neden:* alanın
  **değeri** R8 kapsamında gösterilir; şemanın kendisi ayrı bir görselleştirme işidir.
- **X4.** `Car` + `Offer` JSON-LD, `hreflang`, sayfa başlığı/meta enjeksiyonu —
  *neden:* `docs/architecture.md` § 3 bunu araç sayfaları için zorunlu kılıyor, ancak
  enjeksiyon host tarafında rota bazlı çalışır ve vitrinin tamamını (liste + detay + şube)
  birlikte ele almak gerekir. **Bu, bu spec'in en güçlü kapsam dışı adayıdır** — Context'e bkz.
- **X5.** Benzer/önerilen araçlar bölümü — *neden:* öneri motoru `docs/architecture.md`
  § 6'da kapsam dışıdır.
- **X6.** Fiyat geçmişinin ziyaretçiye gösterilmesi — *neden:* **A6** fiyat geçmişini iç
  kayıt olarak tanımlar; vitrinde gösterilmesi ayrı bir ticari karardır.
- **X7.** Taksit hesaplayıcı — *neden:* `docs/architecture.md` § 6'da V1 için ayrıca ele
  alınmış bir konudur.
- **X8.** Yönetim panelindeki araç detay/düzenleme ekranı — *neden:* bu spec vitrin tarafıdır.
- **X9.** `frontend/src/shared/` yapısının kurulması, `tokens.css` çıkarılması ve ortak
  bileşen envanterinin oluşturulması — *neden:* bu, 0002'nin değil `frontend-001`'in işidir.
  Bu spec envanteri **kullanır**, kurmaz. Bkz. Context § Sıralama riski.

---

## Context

### Mevcut davranış — doğrulandı

- Vitrin **liste** sayfası spec `0001` ile yapıldı ve çalışıyor. Tek vitrin endpoint'i
  `GET /api/catalog/showcase-listings`
  ([ShowcaseEndpoints.cs:19](src/Modules/VehicleCatalog/VehicleCatalog/Endpoints/ShowcaseEndpoints.cs#L19)).
  **Detay için bir okuma yolu yoktur.**
- `ShowcaseListingDto`, `ListingId` (`Guid`) alanını taşıyor ve dosyadaki açıklama bunu
  açıkça *"karttan detaya gidiş bağlantısı için (X9)"* diye gerekçelendiriyor
  ([ShowcaseListingContracts.cs:14](src/Modules/VehicleCatalog/VehicleCatalog.Contracts/Showcase/ShowcaseListingContracts.cs#L14)).
  Yani kimlik hazır, kullanan yok.
- **Kartta bağlantı yok.** [ListingCard.tsx](frontend/src/features/catalog/ListingCard.tsx)
  hiçbir `<a>` veya rota bağlantısı içermiyor; kart tıklanabilir değil.
- **Uygulamada rota ağacı yok.** [App.tsx](frontend/src/app/App.tsx) doğrudan
  `ShowcaseListPage`'i render ediyor; sayfa seçen bir katman bulunmuyor. İkinci bir sayfa,
  rota ağacını gerektirir. *(Bunun nasıl kurulacağı Developer'ın kararıdır; burada yalnızca
  mevcut durum tespiti olarak yazılmıştır.)*

### Geçerli domain kuralları

- **A2** — yayına alınma koşulları; çekirdek alanlar yayındaki her araçta doludur.
- **A3 / A4** — yayın durumu geçişleri; `Satıldı` terminaldir, araç yeniden yayına alınamaz.
- **A8** — şube kapatıldığında araçlar yayından kalkar ve "lokasyon ataması bekliyor"
  durumuna geçer; sessizce vitrinde kalmazlar.
- **R6** — `Rezerve` araç vitrinde görünmeye devam eder, rezerve rozetiyle ve iletişim formu
  kapalı olarak. *(İkinci yarısı için bkz. X1.)*
- **F3 / F4** — `Gizli` alan public yanıtta hiç bulunmaz; şase, plaka ve iç notlar her zaman
  gizlidir; çekirdek alanlar kapatılamaz.
- **P1–P4** — `Money`, üç ondalıklı para birimleri, kurulum başına tek para birimi,
  "fiyat sorunuz" ≠ `0`.

### Spec 0001'den devralınan bağlam

- 0001 § KAPSAM DIŞI **X9** ilan detay sayfasını açıkça 0001'in dışında bırakmıştı; bu spec
  o boşluğu kapatır.
- 0001, `Satıldı` ilanları listede göstermemeye karar verdi (0001 AC-3, X8). Bu spec detay
  tarafında **aynı** çizgiyi sürdürür (AC-6); karar E4 olarak onaylandı — bkz. aşağıdaki
  "Verilen kararlar".

### Sıralama riski — envanter henüz yok

`docs/frontend.md` § 4.2'nin başlangıç envanteri (`Button`, `Card`, `Badge`, `EmptyState`,
`ErrorState`, `Skeleton`) bugün **mevcut değildir.** Doğrulandı:

- `frontend/src/shared/` klasörü yok; § 1'in yapısı henüz kurulmamış.
- `frontend/src/components/` **boş.**
- Durum bileşenleri feature'a özel yaşıyor: `features/catalog/states/` altında
  `EmptyByFilter`, `EmptyShowcase`, `FilterBarSkeleton`, `ListError`, `ListSkeleton`.
- `shared/design/tokens.css` yok; tek stil dosyası `frontend/src/app/styles.css` ve içinde
  **39 adet ham piksel değeri** var (`grep -rnoE '[0-9]+px' frontend/src --include='*.css' | wc -l` → 39).

Bu, C10 ve C11'i bugün karşılanamaz hâle getirir. Detay ekranının `Skeleton`, `ErrorState` ve
`Badge`'e ihtiyacı vardır; envanter yoksa ya ikinci bir kopya yazılır (borç büyür) ya da
`frontend-001` önce biter. **Karar verildi: `frontend-001` önce biter** (E6).

`specs/frontend-001-uygulama-kabugu-ve-standarda-uyum.md` bu işi kapsıyor ve **Taslak**
durumunda: R12'si envanteri `src/shared/ui` altında var ediyor, AC-14/AC-15/AC-16'sı klasör
yapısını, `tokens.css`'i ve envanteri kriter hâline getiriyor. Ayrıca kendi Intent'inde
bu spec'i adıyla anıyor — "ekip bir sonraki ekranı (ilan detayı, iletişim) koyacak bir yer
istiyor". İki spec'in sırası bu yüzden gerçek bir bağımlılıktır, tercih değil.

### Bilinen tuzaklar

1. **Gizli ilanın varlığını sızdırmak.** "Bu ilan yayında değil" demek, taslak ve arşiv
   ilanların varlığını adres denemesiyle keşfedilebilir kılar. C13 bunu yasaklar.
2. **Alan görünürlüğünü uçta uygulamak.** Kapalı/gizli alanı yanıta koyup arayüzde saklamak
   Yasak Listesi 41'in tam ihlalidir; alan yanıta **hiç girmez**.
3. **Üç ondalıklı para birimi.** KWD tutarı iki ondalıkla gösterilirse hata sessizdir.
4. **Arapça'da karışık yönlü metin.** Latin model adı Arapça gövde içinde `dir="auto"`
   ister; liste kartında bu zaten yapıldı, detayda tekrarlanması gerekir.
5. **Slug ile kimlik karışıklığı.** `docs/architecture.md` § 3 adresin **slug** olmasını
   söylüyor; bugünkü liste sözleşmesi yalnızca `ListingId` taşıyor. Karar E5 § 3'ten yana
   verildi — sonucu için bkz. "Verilen kararlar".

### Verilen kararlar

Üçü de Takım Yöneticisi tarafından onaylandı. Bundan sonra değişmeleri **spec revizyonudur**
(AP-08) ve SCORECARD'ın "spec revizyon sayısı" satırına yazılır.

**E4 — `Satıldı` ilanın detayı: bulunamadı.**
`Satıldı` bir ilanın adresi istendiğinde AC-6 uygulanır; kalıcı "bu araç satıldı" sayfası
yapılmaz. *Gerekçe:* **A4** satışı terminal sayar ve spec `0001` aynı ilanları listeden
çıkarmıştı (0001 AC-3, X8); iki ekranın aynı ilan için farklı cevap vermesi tutarsızlıktır.
*Bilerek ödenen bedel:* o adres arama motorlarına verilmiş olabilir ve bulunamadı cevabı o
ziyaretçiyi vitrinde tutmaz.

**E5 — Adres, `docs/architecture.md` § 3'ün slug'ıdır.**
Detay adresi her dilde aynı Latin slug'ı taşır (AC-4); ham kimlik adres olarak kullanılmaz.
*Gerekçe:* § 3 verilmiş bir mimari karardır ve spec onu sessizce delmez. *Sonucu:* bugünkü
liste sözleşmesi yalnızca `ListingId` taşıdığı için, karttan detaya gidişin (R1, AC-2) adresi
nereden alacağı çözülmelidir; **sözleşmenin bu adresi taşıyacak biçimde büyümesi 0002
kapsamındadır.** Adresin nerede üretileceği ve nasıl saklanacağı Developer'ın kararıdır.
*Bilerek ödenen bedel:* 0002 bir sözleşme değişikliği kadar büyür.

**E6 — Sıra: önce `frontend-001`, sonra `0002`.**
C14 bunu kısıt hâline getirir. *Gerekçe:* aksi hâlde detay ekranı `Skeleton`/`ErrorState`/
`Badge`'in ikinci kopyasını yazar ve `frontend-001` sonra o kopyayı da taşımak zorunda kalır.
*Bilerek ödenen bedel:* 0002 gecikir.

---

Aşağıdakiler **PLAN gözden geçirmesinde** çıkan belirsizlikler için verilmiş kararlardır;
Takım Yöneticisi tarafından onaylandı.

**E7 — "Bulunamadı" ile "gizli" ayrımı TEK SORGUYLA kapatılır.**
Detay, doğrudan vitrin okuma modelinden okunur. O model 0001'de yalnız görünür ilanları
taşıyacak biçimde kuruldu; gizli ilanın satırı yoktur. Böylece "gizli" ile "hiç yok" aynı kod
yolundan geçer. *Gerekçe:* önce bul–sonra görünür mü bak biçiminde iki adımlı bir uygulama
**zamanlama yan kanalı** açar ve var olan gizli ilan ölçülebilir biçimde daha yavaş cevap
verir; C13 "süre farkı dâhil" diyerek tam bunu yasaklıyor. *Bilerek ödenen bedel:* detay
yalnızca okuma modelinden beslenir — yazma tarafı satırı üretmezse detay da yoktur (0001 X10).

**E8 — Alan sahipliği.**
Donanım paketi, yakıt, vites ve çekiş **`VehicleReference`**'a aittir (soft `Guid` + locale
etiketi). **Renk `VehicleCatalog`'un kendi değeridir.** *Gerekçe:* ilk dördü çevrilmesi gereken
kapalı kümelerdir ve `docs/architecture.md` § 1 onları "marka / model / kasa tipi / donanım
paketi / motor-şanzıman sözlüğü" diye adıyla sayar; renk ise galeriye göre değişir ve
taksonomi değildir. *Sonucu:* 0002 iki modüle dokunur ve Modül(ler) satırı güncellendi.

**E9 — Slug kararlıdır.**
Slug ilk yayına alma anında üretilir ve **bir daha değişmez**; tekilliği ilan kimliğinin ilk
altı karakterinden gelen sonek sağlar. *Gerekçe:* R2 adresin kalıcı ve paylaşılabilir olmasını
istiyor; model adı düzeltildiğinde slug değişirse paylaşılmış her adres kırılır.
*Bilerek ödenen bedel:* yanlış girilmiş bir model adı slug'da kalır.

**E10 — Opsiyonel alan değerleri geliştirme seed'iyle üretilir.**
AC-11, AC-12 ve AC-13 entegrasyon testinde gerçek veriyle doğrulanır. *Gerekçe:* opsiyonel alan
**değeri** saklayan bir yapı bugün yok ve onu üretecek yazma yolu 0001 X10, panel ise X8 ile
kapsam dışı. *Bilerek ödenen bedel:* üretimde detayın opsiyonel alanları, yazma yolu gelene
kadar boş kalır — bu, 0002'nin PR'ında açıkça yazılır.

**E11 — X4 için takip spec'i 0002 MERGE EDİLMEDEN açılır.**
`docs/architecture.md` § 3 araç sayfalarında `Car` + `Offer` JSON-LD ve `hreflang` zorunlu
kılıyor; X4 bunu kapsam dışı bıraktı. *Gerekçe:* § 3'ü karşılamayan bir sayfa yayına girecekse
borcun bir yeri olmalı; takip spec'i açılmazsa borç görünmez olur ve kimse ödemez.

**E12 — Beş alan terimi önce `docs/domain.md` sözlüğüne girer.**
Donanım paketi, yakıt, vites, çekiş ve renk `docs/architecture.md` § 4'te çekirdek alan olarak
geçiyor ama **sözlükte yok** (doğrulandı). `docs/conventions.md` § 2: "Sözlükte olmayan yeni bir
terim koda girmeden önce sözlüğe girer." *Sonucu:* sözlük eklemesi 0002'nin ilk adımıdır;
aksi hâlde DoD'deki "yeni domain terimi yok" maddesi dürüstçe işaretlenemez.

---

## Acceptance Criteria

**Detayın açılması**

- **AC-1.** `Yayında` durumundaki bir ilanın adresi istendiğinde o ilanın detayı döner.
- **AC-2.** Vitrin listesindeki bir ilan kartı, o ilanın detay adresine giden bir bağlantı taşır.
- **AC-3.** Detay adresi yeni bir oturumda doğrudan açıldığında aynı ilanın detayı görünür.
- **AC-4.** Aynı ilanın TR, AR ve EN adresleri **aynı Latin slug'ı** taşır; yalnızca dil
  öneki farklıdır.

**Görünmemesi gerekenler**

- **AC-5.** Var olmayan bir ilan adresi istendiğinde sistem "bulunamadı" karşılığı verir.
- **AC-6.** `Taslak`, `Arşiv` ve `Satıldı` durumundaki bir ilanın adresi istendiğinde sistem
  "bulunamadı" karşılığı verir.
- **AC-7.** "Lokasyon ataması bekliyor" durumundaki bir ilanın adresi istendiğinde sistem
  "bulunamadı" karşılığı verir.
- **AC-8.** Var olan ama gizlenen bir ilan ile hiç var olmayan bir ilan için dönen karşılık
  birbirinden ayırt edilemez: aynı hata kodu, aynı gövde.
- **AC-9.** `Rezerve` durumundaki ilanın detayı açılır ve sayfada rezerve rozeti görünür.

**Sayfa içeriği ve alan görünürlüğü**

- **AC-10.** Detay sayfası model adını, yılı, kasa tipini, donanım paketini, kilometreyi,
  yakıtı, vitesi, çekişi, rengi, şube adını ve kapak görselini gösterir.
- **AC-11.** Alan ayarlarında **açık ve public** işaretli bir opsiyonel alanın değeri detay
  sayfasında görünür.
- **AC-12.** Alan ayarlarında **kapalı** bir opsiyonel alan detay yanıtında **hiç bulunmaz**;
  boş değer olarak da bulunmaz.
- **AC-13.** Alan ayarlarında **`Gizli`** işaretli bir opsiyonel alan detay yanıtında
  **hiç bulunmaz**; boş değer olarak da bulunmaz.
- **AC-14.** Detay yanıtı şase, plaka ve iç notlar alanlarını **hiç içermez**; bu alanlar boş
  değer olarak da bulunmaz.
- **AC-15.** "Fiyat sorunuz" işaretli ilanın detayında tutar yerine "fiyat sorunuz" karşılığı
  gösterilir; `0` veya boş tutar gösterilmez.
- **AC-16.** Kurulum para birimi KWD iken `1250,500` tutarlı ilanın detayında tutar **üç
  ondalıkla** gösterilir; yuvarlama veya kesme yapılmaz.

**Üç durum** (`docs/frontend.md` § 3)

- **AC-17.** Detay verisi yüklenirken ekranda, gerçek içeriğin kutu modelini taklit eden
  iskelet gösterilir; boş ekran veya içerik sıçraması olmaz.
- **AC-18.** Bulunamadı karşılığı geldiğinde ekran, aracın artık vitrinde olmadığını bildirir
  ve **listeye dön** eylemini sunar.
- **AC-19.** Detay verisi alınamadığında ekran anlamlı bir mesaj ve **tekrar dene** eylemi
  gösterir.
- **AC-20.** Bulunamadı ve hata ekranlarının hiçbirinde HTTP durum kodu, hata kodu, `traceId`,
  endpoint yolu veya istisna metni görünmez.

**Çok dillilik ve RTL**

- **AC-21.** Detay sayfasındaki tüm etiketler TR, AR ve EN locale'lerinde kendi dilinde
  görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.
- **AC-22.** Arapça locale'de detay sayfası `dir="rtl"` ve `lang="ar"` ile basılır; bilgi
  listesi ve rozet sağdan sola hizalanır.
- **AC-23.** Arapça locale'de Latin model adı karışık yönlü metin olarak doğru görünür.
- **AC-24.** Bulunamadı ve hata mesajları üç etkin dilin hepsinde yazılıdır ve her birinde
  bir sonraki eylemi önerir.
- **AC-25.** Detaydaki kapak görselinin, ziyaretçinin locale'ine karşılık gelen `alt` metni
  vardır.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0002-ac<n>.png`),
      bulunamadı ve hata durumları dâhil; RTL kriteri için Arapça görüntü de var.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] Yeni migration varsa yalnızca `VehicleCatalog` şemasına dokunuyor.
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır.
- [ ] Sayfa, `docs/frontend.md` § 4.2 envanterindeki bileşenleri kullanıyor; yeni ortak
      bileşen eklendiyse gerekçesi PR'da yazılı.
- [ ] Ham piksel / hex renk eklenmedi; yeni değer gerektiyse önce token tanımlandı (§ 4.1).
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra spec kaç kez değişti? Yüksekse spec eksik yazılmıştır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. |
| **Bulgu gerçek/gürültü oranı** | / | Gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
