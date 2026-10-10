# 0014 — Ana Sayfa

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin · `VehicleCatalog` (okuma yüzeyi) · `VehicleReference` (okuma yüzeyi) |
| **Bağımlı spec'ler** | `0001`, `0002`, `frontend-001`, `frontend-002`. **Sıra çelişkisi için bkz. Açık soru S1.** |
| **Dayanak** | ADR 0003 Ek 1, sıra #7 |
| **Karar bekliyor** | **S1–S4** — § Açık sorular (4 soru, **4 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — dört açık sorunun cevabı
> onları belirliyor. **S1 bu spec'in sıradaki yerini sorguluyor.**

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

### Mevcut davranış — doğrulandı

- **Ana sayfa yok.** `frontend-001` bir uygulama kabuğu (`AppShell`) ve dil değiştirici kurdu;
  kabuk doğrudan vitrin listesini render ediyor.
- **Vitrin listesi ve ilan detayı hazır** (`0001`, `0002` tamamlandı); liste okuma modeli ve
  detay okuma yolu mevcut.
- **Galeri profili hazır:** ad ve `Gallery.LogoMediaAssetId` `0003` ile geldi. Ancak logonun
  **görseli** `Media` modülüne bağlı ve `Media` bugün tek bir olay dosyasından ibaret —
  yani logo bir kimlik olarak var, **gösterilebilir bir dosya olarak yok**.

> **Doğrulama uyarısı (2026-10-05):** bu taslak yazılırken `VehicleReference`'a
> **marka/model okuma yüzeyi ekleniyordu** (`/api/reference/models` ve devamı, henüz
> commit edilmemiş). Aşağıdaki "okuma yüzeyi yok" tespiti o iş tamamlandığında
> geçersizleşir ve spec onaya sunulmadan önce **yeniden doğrulanmalıdır.**

- **Marka sayımı için okuma yüzeyi yok.** `VehicleReference`'ın tek ucu
  `GET /api/reference/body-types`; marka listesi ve marka başına araç sayısı hiçbir yerde
  sunulmuyor.
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
**kasa tipi** ekseniyle (tek var olan eksen, `0001`'den), öne çıkan araçlar **vitrin sıralama
ağırlığıyla** (`VehicleCatalog`'da zaten var), bülten **yok** (X2).
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

**S3. "Öne çıkan araçlar" nasıl seçilir?**
*Öneri:* **vitrin sıralama ağırlığı** en yüksek olan ilk altı ilan; eşitlikte en yeni
yayınlanan.
*Gerekçe:* `VehicleCatalog`'da bu alan **zaten var** ve `0001` AC-4 onu liste sıralamasında
kullanıyor. Yeni bir kavram ("öne çıkan slot") eklemek `Content`'in işini bu spec'e çeker.
*Bedel:* galeri "ana sayfada şu altı araç dursun" diye **elle** seçemez; ağırlıkla dolaylı
yönetir. `Content` slotu geldiğinde (#11) bu ölçüt değişir.

**S4. Altlıktaki kurumsal bağlantılar ne yapar?**
`Content` yok; "Hakkımızda", "KVKK", "Çerez politikası" sayfaları mevcut değil.
*Öneri:* ilk sürümde altlık **yalnızca var olan hedeflere** bağlantı verir: araç listesi,
dil bağlantıları, galeri iletişim bilgisi (profilden). Olmayan sayfaların bağlantısı
**konulmaz**.
*Gerekçe:* 404'e giden bağlantı, olmayan bir bağlantıdan kötüdür — hem ziyaretçi hem arama
motoru için.
*Bedel:* altlık mockup'takinden ince olur. **Ayrıca bir uyarı:** `0005` zaten KVKK
aydınlatma metnine bağlantı veren bir talep formu yayınladı; o bağlantının bugün nereye
gittiği **bu spec'te doğrulanmadı** ve kontrol edilmesi gerekir.

---

## Acceptance Criteria

> Dört açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> galeri adı/logosu · giriş bölümü · öne çıkan araçların seçimi ve sırası · marka listesi ve
> sayımı · daraltma formunun listeye geçişi · toplam araç sayısı · boş vitrin · kısmi hata ·
> locale önekleri ve geçersiz önekte 404 · RTL ve karışık yönlü marka adı · LCP ve CLS ·
> gizli alanların yanıtta bulunmaması.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Görsel kanıt eklendi (`docs/evidence/0014-ac<n>.png`), boş ve hata durumları dâhil,
      Arapça görüntü de var.
- [ ] Mimari testler yeşil — başka modülün tablosuna erişim yok (C11).
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi yok.
- [ ] Altlıktaki her bağlantının **var olan** bir hedefe gittiği doğrulandı (S4).
- [ ] `0005`'in KVKK bağlantısının hedefi kontrol edildi ve PR'da raporlandı (S4 uyarısı).
- [ ] Ham piksel/hex eklenmedi; token katmanı kullanıldı (C7).
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
