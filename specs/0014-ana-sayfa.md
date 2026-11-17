# 0014 — Ana Sayfa

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin · `VehicleCatalog` (okuma yüzeyi) · `VehicleReference` (okuma yüzeyi) · `Dealership` (galeri profilinin public okuması — bkz. Açık soru **S5**) |
| **Bağımlı spec'ler** | `0001`, `0002`, `0003`, `0012c`, `frontend-001`, `frontend-002`. **Sıra çelişkisi için bkz. Açık soru S1.** |
| **Dayanak** | ADR 0003 Ek 1, sıra #7 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — beş açık sorunun cevabı
> onları belirliyor. **S1 bu spec'in sıradaki yerini sorguluyor.**
>
> **Context yeniden doğrulandı (2026-11-17).** Taslak 2026-10-05'te yazılmıştı; arada
> `0011`, `0012c` ve `0013` kapandı. Dört tespit işlendi (B1–B4, Takım Yöneticisi onayı
> 2026-11-17): **S3** yeniden yazıldı (B1), **S5** eklendi (B2), doğrulama uyarısı
> kaldırıldı (B3), **S4**'ün KVKK uyarısı cevaplandı (B4). R/C bölümlerine dokunulmadı.

---

## Intent

Vitrine gelen ziyaretçi bugün **doğrudan araç listesine düşüyor.** `frontend-001` bir kabuk
ve dil değiştirici kurdu, ama sitenin bir **girişi** yok: galerinin kim olduğunu, neyi iddia
ettiğini ve elindeki stoğun ne olduğunu söyleyen bir sayfa bulunmuyor.

Bu, ziyaretçinin ilk üç saniyesini boşa harcıyor. Araç almak, insanın birkaç gün içinde
birden çok galeriye baktığı bir iş; hatırlanan galeri, ne sattığını ilk ekranda söyleyen
galeri oluyor.

**Galeri sahibi** vitrinin bir siteye ait olduğunu hissettiren bir giriş istiyor; ziyaretçi
ise aradığı aracı listeye girmeden **daraltarak** başlamak istiyor.

Bu iş bittiğinde ziyaretçi ana sayfada galerinin iddiasını, öne çıkan araçları ve stoktaki
markaları görür; oradan arama/daraltma ile listeye geçer. Başarı ölçütü: **ziyaretçi ana
sayfadan ayrılmadan stoğun ne içerdiğini anlar.**

---

## Requirements

- **R1.** Ana sayfa, galerinin **adını ve logosunu** gösterir; ikisi galeri profilinden gelir.
  → `0003` R1.
- **R2.** Ana sayfa bir **giriş bölümü** (başlık + açıklama) taşır. Metnin kaynağı için
  bkz. Açık soru **S2**.
- **R3.** Ana sayfa **öne çıkan araçları** gösterir. Seçim ölçütü için bkz. Açık soru **S3**.
- **R4.** Ana sayfa, stoktaki **markaları** ve her markadaki **araç sayısını** gösterir;
  markaya tıklamak o markaya daraltılmış listeye gider.
- **R5.** Ana sayfa bir **daraltma formu** taşır; form doldurulup gönderildiğinde ziyaretçi
  o daraltmanın uygulandığı listeye gider. Formun eksenleri için bkz. Açık soru **S1**.
- **R6.** Form, yayındaki **toplam araç sayısını** gösterir.
- **R7.** Ana sayfa bir **altlık** taşır: galeri adresi, çalışma bilgisi, dil bağlantıları ve
  kurumsal sayfa bağlantıları. Kurumsal sayfaların varlığı için bkz. Açık soru **S4**.
- **R8.** Ana sayfa **yükleniyor**, **boş** (vitrinde hiç yayında araç yok) ve **hata**
  durumlarını ele alır; bölümler **kısmen** yüklenemezse sayfanın tamamı düşmez.
  → `docs/frontend.md` § 3.
- **R9.** Ana sayfa etkin üç locale'de çalışır; Arapça'da tam RTL. Latin marka adları karışık
  yönlü metin olarak doğru görünür.
- **R10.** Ana sayfa `/{locale}/` adresinde yaşar; öneksiz `/` varsayılan locale'e yönlenir,
  geçersiz önek **404** verir. → `docs/architecture.md` § 3.

---

## Constraints

### Uyulması zorunlu

- **C1.** Sayfa **public**'tir; oturum gerektirmez. Public üyelik yoktur. → § 6.
- **C2.** Gizli alanlar yanıtta **hiç bulunmaz**; gizleme sorgu tarafında uygulanır.
  → **F3**, Yasak Listesi 41.
- **C3.** Yalnızca vitrinde görünür ilanlar gösterilir: `Yayında` ve `Rezerve` (rozetli).
  → **R6** (domain), `0001` AC-2/AC-3.
- **C4.** Fiyat `Money` ile taşınır; "fiyat sorunuz" `0` değildir; üç ondalıklı para
  birimleri bozulmaz. → Altın Kural 4, **P1–P4**.
- **C5.** Kullanıcıya görünen metin koda gömülmez; sayı içeren cümleler ICU ile kurulur
  ("128 aracı gör"). → Altın Kural 6, Yasak Listesi 31.
- **C6.** CSS'te fiziksel yön özellikleri kullanılmaz. → Yasak Listesi 25.
- **C7.** Renk ve tipografi `frontend-002`'nin token katmanından gelir; ham hex/px yoktur.
  → `docs/frontend.md` § 4.1.
- **C8.** Bileşenler `docs/frontend.md` § 4.2 envanterinden kullanılır; yeni ortak bileşen
  gerekçesiyle önerilir.
- **C9.** Görseller `loading="lazy"`, açık `width`/`height` ile; **kapak/giriş görseli
  hariç**. → `docs/frontend.md` § 7 (CLS ve LCP).
- **C10.** `Accept-Language`'a göre otomatik yönlendirme yapılmaz. → Yasak Listesi 28.
- **C11.** Marka sayımı ve öne çıkan araç okuması, `VehicleCatalog`'un **kendi** okuma
  modelinden beslenir; başka modülün tablosuna erişilmez. → Yasak Listesi 1.
- **C12.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.

### KAPSAM DIŞI

- **X1.** **Markalar sayfası** (mockup'taki "Markalar" menüsü ve marka detay sayfası) —
  *neden:* ana sayfadaki marka listesi daraltılmış listeye gider (R4); ayrı bir marka
  sayfası kendi SEO ve içerik kararlarını getirir.
- **X2.** **Bülten aboneliği formu** — *neden:* ADR 0003 Ek 1 sıra #13'ün işi; abonelik
  kaydı, çift onay ve abonelikten çıkma kendi spec'ini hak eder. Ana sayfanın ilk sürümünde
  bu bölüm **yoktur**.
- **X3.** **Kampanya alanları** — *neden:* kampanya `Content` modülünün verisidir (sıra #11)
  ve `docs/domain.md` **K1–K5** kendi kurallarını taşır.
- **X4.** **Favori ve karşılaştırma** — *neden:* sıra #12'nin işi.
- **X5.** **Serbest metin arama kutusu** — *neden:* `0001` § X1 ile aynı gerekçe; normalizasyon
  yükü kendi spec'ini hak eder.
- **X6.** **Blog, SSS ve kurumsal sayfaların içeriği** — *neden:* `Content` modülü yok
  (sıra #11). Altlıktaki bağlantılar için bkz. **S4**.
- **X7.** **JSON-LD ve meta enjeksiyonu** — *neden:* `0004` (SEO) bu konunun sahibidir.
- **X8.** **"En çok görüntülenen" veya popülerlik ölçütü** — *neden:* `Engagement` modülü yok.

---

## Context

### Mevcut davranış — doğrulandı (2026-11-17, `main` @ `ab04cb5`)

- **Ana sayfa yok.** `frontend-001` bir uygulama kabuğu (`AppShell`) ve dil değiştirici kurdu;
  `/{locale}/` adresi doğrudan vitrin listesini açıyor (`frontend/src/shared/lib/route.ts`:
  liste ve detay dışında public bir rota türü yok).
- **Vitrin listesi ve ilan detayı hazır** (`0001`, `0002` tamamlandı); liste okuma modeli ve
  detay okuma yolu mevcut. Liste, **öne çıkarılmış ilanları önce**, eşitlikte en yeni
  yayınlananı önce sıralıyor (`ShowcaseListingQueries.cs`).
- **Öne çıkarma bir panel eylemi (B1).** `0012c` ilanı tek tek veya toplu olarak **öne
  çıkarma / öne çıkarmayı kaldırma** eylemini getirdi. Vitrin sıralama ağırlığı artık iki
  değerli: öne çıkarılmış veya değil (`Listing.cs`: `Feature()` / `Unfeature()`). Yani
  "öne çıkan araç" bugün **galerinin elle verdiği bir karar**, sıralamanın bir yan etkisi
  değil. Bu tespit **S3**'ü yeniden yazdırdı.
- **Galeri profilinin public okuması yok (B2).** `0003` galeri adını, şubeleri ve
  `Gallery.LogoMediaAssetId` alanını getirdi. Ama `Dealership`'in tek ucu panele ait
  (şube listesi); ziyaretçinin galeri adını, logosunu, adresini veya çalışma bilgisini
  okuyabileceği bir yüzey **yok**. Logo için ayrıca: `Media` artık yükleme ve türev üretimi
  yapıyor (`0011`), ama galeriye logo atayan **bir akış yok** — atama metodunun hiçbir
  çağıranı yok. Yani logo bugün ne yüklenebiliyor ne de gösterilebiliyor. R1 ve R7'nin
  profil kısmı bu yüzeye dayanıyor → **S5**.
- **Stoktaki marka ve marka başına araç sayısı hiçbir yerde sunulmuyor (B3).** 2026-10-05
  taslağının "eklenmekte olan okuma yüzeyi" uyarısı çözüldü: `VehicleReference` artık
  body-types, models ve dört terim türü (yakıt, şanzıman, çekiş, donanım) sunuyor. Ama bunlar
  **referans verisi** — kayıtlı bütün modeller, stoktan bağımsız; model kaydı da yalnız kimlik
  ve etiket taşıyor. "Stokta hangi marka var, kaç araç" sorusunu cevaplayan bir okuma **yok**.
  R4 bu okumaya dayanıyor.
- **Mockup ne gösteriyor:** giriş bölümü + daraltma formu (**Marka · Kasa tipi · Yıl · Fiyat
  aralığı** ve "128 aracı gör"), "Vitrin" başlıklı **6 öne çıkan araç**, numaralı **marka
  listesi** (marka + "14 araç"), bülten bölümü, dört kolonlu altlık (Keşfet / Kurumsal /
  Diller). Kartlarda "Tramersiz", "Yeni geldi", "Takasa açık", "Hibrit" rozetleri var.
- **Mockup'ın rozetleri `0001`'de yok.** `0001` R2 yalnızca **Rezerve** rozetini tanımladı;
  diğer dördü yeni kavramlar ve bu spec'te **kapsam dışıdır** (X3 ile ilişkili: "Yeni geldi"
  türetilebilir, "Takasa açık" opsiyonel alan, "Öne çıkan" `Content` slotu).

### Geçerli kurallar

**R6** (rezerve araç vitrinde rozetle görünür) · **A3** (yalnız `Yayında`/`Rezerve` görünür) ·
**F3/F4** (gizli alanlar yanıtta yok) · **P4** ("fiyat sorunuz" ≠ 0) ·
`docs/architecture.md` § 3 (locale öneki zorunlu, otomatik yönlendirme yok, her sayfa
`hreflang` yayar) · `docs/frontend.md` § 3, § 7.

### Bilinen tuzaklar

1. **Giriş görselini lazy yüklemek.** Ana sayfanın en büyük görseli LCP adayıdır; `0001`
   bu tuzağa düşüp düzeltmişti (`ListingCard`'ın `aboveTheFold` propu o dersin kalıntısı).
2. **Marka sayımını her istekte saymak.** Birkaç yüz araçta ucuzdur ama sayım sorgusu ana
   sayfanın her yüklenişinde çalışır; ölçülmeden "ucuz" denmemeli.
3. **Boş vitrinde ana sayfayı anlamsız bırakmak.** Yayında hiç araç yoksa "128 aracı gör"
   düğmesi "0 aracı gör" olur; R8'in boş durumu bunu ayrıca ele almalı.
4. **Marka adını çevirmeye çalışmak.** Marka adı kanonik Latin addır; Arapça sayfada
   çevrilmez, `dir="ltr"` ile korunur (R9).
5. **Altlıkta olmayan sayfaya bağlantı koymak.** `Content` yok; "Hakkımızda" bağlantısı
   404'e gider (S4).

---

## Açık sorular

**S1. Bu spec sıradaki yerinde kalabilir mi? Üç bağımlılığı kendisinden sonra.**
ADR 0003 Ek 1 ana sayfayı **#7**'ye koydu. Ama mockup'ın ana sayfası:
- **daraltma formu** → marka/yıl/fiyat eksenleri **#9**'da (`0001` X2/X3 bunları kapsam dışı
  bırakmıştı),
- **öne çıkan araçlar** → `Content` slotu **#11**'de,
- **bülten** → **#13**'te.

*Öneri:* ana sayfa **#7'de kalsın, ama kırpılmış sürümüyle**: daraltma formu yalnızca
**kasa tipi** ekseniyle (tek var olan eksen, `0001`'den), öne çıkan araçlar **`0012c`'nin
öne çıkarma eylemiyle** (panelde zaten var; bkz. S3), bülten **yok** (X2).
*Gerekçe:* ziyaretçi tarafında bir giriş sayfasının olması, eksiksiz olmasından önce gelir;
üç bağımlılığı beklemek ana sayfayı #13 sonrasına atar. Kırpılmış sürüm bugünkü veriyle
çalışır ve eksenler #9'da genişlediğinde form kendiliğinden büyür.
*Bedel:* ana sayfa iki kez elden geçer; mockup'ın dört eksenli formu ilk sürümde tek eksenli
olur ve mockup'ı gören biri eksik bulur.
*Elenen alternatif:* ana sayfayı #11 sonrasına taşımak — teknik olarak daha temiz ama vitrin
girişsiz kalmaya devam eder ve bu ziyaretçi tarafındaki en görünür eksiktir.

**S2. Giriş bölümünün metni nereden gelir?**
Mockup'ta somut bir iddia var: *"Geçmişi gizlenmeyen araçlar."* ve altında bir paragraf.
*Öneri:* ilk sürümde **çeviri kataloğundan** gelir (üç dilde sabit metin), `Content`'ten değil.
*Gerekçe:* `Content` modülü yok (#11) ve bu metin kurulum başına bir kez yazılan bir şeydir.
*Bedel:* galeri metnini kendi değiştiremez; değişiklik bir dağıtım ister. `Content` geldiğinde
bu bölüm oraya taşınabilir — o zaman bir taşıma işi doğar.

**S3. "Öne çıkan araçlar" nasıl seçilir — ve yeterince öne çıkarılmış ilan yoksa ne olur?**
*(2026-11-17'de yeniden yazıldı — B1.)* İlk sürümdeki öneri "sıralama ağırlığı en yüksek altı
ilan"dı ve bedeli "galeri elle seçemez"di. `0012c`'den sonra bu bedel **geçersiz**: galeri
panelde ilanı açıkça öne çıkarıyor. Soru artık iki parçalı.

*S3a — seçim.*
*Öneri:* ana sayfanın öne çıkan bölümü **öne çıkarılmış** ve vitrinde görünür (C3) ilanları
gösterir; sıra vitrin listesininkiyle aynıdır (en yeni yayınlanan önce); **en çok altı** ilan.
*Gerekçe:* `0012c` öne çıkarmayı galerinin bilinçli kararı olarak kurdu; ana sayfa o kararı
olduğu gibi göstermeli. Yeni bir kavram ("öne çıkan slot") eklemek `Content`'in işini bu
spec'e çeker. Listeyle aynı sıra, iki yüzeyin birbirini yalanlamamasını sağlar.
*Bedel:* galeri altıdan fazla ilanı öne çıkarırsa hangilerinin ana sayfada duracağını
**doğrudan** seçemez; en yeni altısı görünür. `Content` slotu geldiğinde (#11) bu ölçüt
yeniden açılabilir.

*S3b — öne çıkarılmış ilan altıdan az veya hiç yoksa.*
*Öneri:* bölüm **yalnız öne çıkarılmış ilanları** gösterir — altıdan azsa o kadarını; **hiç
yoksa bölüm görünmez.** Boşluk normal ilanlarla doldurulmaz.
*Gerekçe:* "öne çıkan" etiketi altında öne çıkarılmamış bir ilan göstermek, galerinin
vermediği bir kararı ona mal eder. Boş bölüm ise R8'in "anlamsız bölüm" tuzağıdır
(§ Bilinen tuzaklar 3).
*Bedel:* öne çıkarmayı hiç kullanmayan bir galerinin ana sayfasında bu bölüm hiç görünmez;
mockup'taki altı kartlık "Vitrin" bandı o kurulumda yoktur.
*Elenen alternatif:* eksik yerleri en yeni yayınlanan normal ilanlarla doldurmak — ana sayfa
her zaman dolu görünür ama etiket yalan söyler.

**S4. Altlıktaki kurumsal bağlantılar ne yapar?**
`Content` yok; "Hakkımızda", "KVKK", "Çerez politikası" sayfaları mevcut değil.
*Öneri:* ilk sürümde altlık **yalnızca var olan hedeflere** bağlantı verir: araç listesi,
dil bağlantıları, galeri iletişim bilgisi (profilden). Olmayan sayfaların bağlantısı
**konulmaz**.
*Gerekçe:* 404'e giden bağlantı, olmayan bir bağlantıdan kötüdür — hem ziyaretçi hem arama
motoru için.
*Bedel:* altlık mockup'takinden ince olur.
*KVKK uyarısının cevabı (2026-11-17 — B4):* taslak, `0005`'in talep formunun KVKK aydınlatma
metnine bağlantı verdiğini ve hedefinin doğrulanması gerektiğini söylüyordu. Doğrulandı:
formun onay alanı (`frontend/src/features/leads/LeadFields.tsx`) **bağlantı taşımıyor**,
yalnız onay metni var. 404'e giden bir bağlantı **yok**; bu yüzden DoD'daki kontrol maddesi
kaldırıldı. Aydınlatma metninin bir sayfası olmaması ayrı bir konudur ve bu spec'in kapsamında
değildir (X6).

**S5. Galeri profilinin public okuması bu spec'e mi girer? (B2)**
R1 (ad ve logo) ve R7'nin profil kısmı (adres, çalışma bilgisi) ziyaretçinin galeri
profilini okuyabilmesine dayanıyor. Bugün böyle bir okuma yok, logo da hiç atanamıyor
(§ Context).
*Öneri:* **ad, şube adresleri ve çalışma bilgisinin public okuması bu spec'e girer; logo
girmez.** Logo yoksa ana sayfa galeri adını metin olarak gösterir. Logo yükleme ve
gösterme ayrı bir kart olur.
*Gerekçe:* adı söylemeyen bir giriş sayfası Intent'in ilk cümlesini ("galerinin kim
olduğunu söyleyen bir sayfa") karşılamaz; ad ve adres `0003`'te zaten kayıtlı, eksik olan
yalnız ziyaretçinin onu okuyabilmesi. Logo ise iki eksik taşıyor (atama akışı ve gösterim)
ve bunlardan biri panel tarafında; onu çekmek spec'i iki yüzeye büyütür.
*Bedel:* spec `Dealership`'e de dokunur (Modül(ler) satırı buna göre güncellendi). Mockup'ın
logolu başlığı ilk sürümde yoktur; R1'in "logo" yarısı bu karar onaylanırsa kapsam dışına
(yeni bir X) yazılır.
*Elenen alternatifler:* (a) profili tamamen kapsam dışı bırakmak — spec küçülür ama ana
sayfa galerinin adını gösteremez ve R1 boşa düşer; (b) logoyu da almak — mockup'a tam uyar
ama bir panel akışını ana sayfa spec'ine çeker.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> galeri adı/logosu · giriş bölümü · öne çıkan araçların seçimi ve sırası · marka listesi ve
> sayımı · daraltma formunun listeye geçişi · toplam araç sayısı · boş vitrin · kısmi hata ·
> locale önekleri ve geçersiz önekte 404 · RTL ve karışık yönlü marka adı · LCP ve CLS ·
> gizli alanların yanıtta bulunmaması.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0014-ac<n>.png`), boş ve hata durumları dâhil,
      Arapça görüntü de var.
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — başka modülün tablosuna erişim yok (C11).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi yok.
- [ ] Altlıktaki her bağlantının **var olan** bir hedefe gittiği doğrulandı (S4).
- [ ] Ham piksel/hex eklenmedi; token katmanı kullanıldı (C7).
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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
