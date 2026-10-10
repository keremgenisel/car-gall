# frontend-003 — Favori ve Karşılaştırma

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin. **Backend sözleşmesi değişmez** (bkz. S3). |
| **Bağımlı spec'ler** | `0001` (tamamlandı; **X5**'i geri açar) · `0002` (tamamlandı) · `frontend-002` (token katmanı) |
| **Dayanak** | ADR 0003 Ek 1, sıra #12 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — beş açık sorunun cevabı
> onları belirliyor.

---

## Intent

Vitrinde gezen ziyaretçi beğendiği aracı **hiçbir yere koyamıyor.** `0001` favorileri
bilinçli olarak dışarıda bıraktı (§ X5) ve gerekçesi doğruydu: üyelik yoktu ve hâlâ yok.

Ama ihtiyaç üyelikten bağımsız: ziyaretçi dört aracı beğeniyor, aralarından birini seçmek
için ikisini yan yana koymak istiyor ve sayfalar arasında gezinirken hangilerini beğendiğini
**hatırlamak** istiyor. Bugün bunu tarayıcının sekmelerinde yapıyor — ya da yapmıyor ve
çıkıyor.

**Galeri** bunu istiyor çünkü karşılaştıran ziyaretçi karar veren ziyaretçidir; karar
verdiğinde talep bırakır.

Bu iş bittiğinde ziyaretçi bir aracı favorilere ekler, favorilerini bir arada görür ve
seçtiği araçları yan yana karşılaştırır — hiçbir hesap açmadan. Başarı ölçütü: **ziyaretçi
beğendiği araçları kaybetmeden sayfalar arasında gezinebiliyor.**

---

## Requirements

- **R1.** Ziyaretçi, liste kartından ve ilan detayından bir aracı **favorilere ekleyebilir**
  ve çıkarabilir.
- **R2.** Favori sayısı vitrinin her sayfasında **görünür**.
- **R3.** Ziyaretçi favorilerini **bir arada** görebilir.
- **R4.** Favoriler **ziyaretçinin kendi tarayıcısında** tutulur; sunucuya gönderilmez ve
  hiçbir yerde kişiyle ilişkilendirilmez. → `docs/architecture.md` § 6
  (*"Favoriler `localStorage`'da"*).
- **R5.** Arayüz, favorilerin **bu cihaza ait** olduğunu ziyaretçiye açıkça söyler.
  → Açık soru **S2**.
- **R6.** Ziyaretçi seçtiği araçları **yan yana karşılaştırabilir**; karşılaştırmaya alınacak
  azami araç sayısı tanımlıdır. → Açık soru **S1**.
- **R7.** Karşılaştırma, araçların **aynı alanlarını** aynı satırlarda gösterir; bir araçta
  olmayan bilgi boş değil, **belirtilmiş** olur.
- **R8.** Karşılaştırmada gösterilen alan kümesi **alan ayarlarından türetilir**: kapalı veya
  gizli alan karşılaştırmada **hiç görünmez**. → `docs/architecture.md` § 4,
  Yasak Listesi 41. Açık soru **S4**.
- **R9.** Karşılaştırma görünümü **paylaşılabilir bir adreste** yaşar; bağlantıyı açan aynı
  araçları karşılaştırmalı görür. → Açık soru **S5**.
- **R10.** Favorilerine eklenmiş bir araç **vitrinden düştüğünde** (satıldı, arşivlendi,
  yayından kaldırıldı) ziyaretçiye durum açıkça gösterilir; sessizce kaybolmaz.
- **R11.** Favoriler ve karşılaştırma **yükleniyor**, **boş** ve **hata** durumlarını ele
  alır. Boş durumlar ayrıştırılır: "hiç favori yok" ile "favorilerin hepsi vitrinden düştü"
  aynı ekran değildir. → `docs/frontend.md` § 3.
- **R12.** Favori düğmesi **klavyeyle** erişilebilir, durumu metinle de anlatılır ve
  ekran okuyucuya durumunu bildirir; renk tek başına anlam taşımaz.
  → `docs/frontend.md` § 7.
- **R13.** Üç locale'de çalışır; Arapça'da RTL. Karşılaştırma tablosu RTL'de doğru yönde
  akar.
- **R14.** Mobilde karşılaştırma okunabilir kalır; geniş tablo **kendi içinde** kaydırılır,
  sayfayı yatay kaydırmaya zorlamaz. → `docs/frontend.md` § 1, § 7.

---

## Constraints

### Uyulması zorunlu

- **C1.** **Public üyelik yoktur**; favori bir hesap özelliği değildir. → § 6.
- **C2.** Favori verisi sunucuya gönderilmez. `Engagement` modülü yoktur ve bu spec onu
  **var etmez**. → Açık soru **S3**.
- **C3.** Gizli ve kapalı alanlar hiçbir yerde görünmez; eleme **sorgu tarafında**
  uygulanır — karşılaştırma uçta bir filtre yapmaz. → Yasak Listesi 41.
- **C4.** Sunucu verisi global store'a kopyalanmaz; istemci durumu yerel tutulur.
  → `docs/frontend.md` § 7.
- **C5.** Paylaşılabilir durum **adreste** yaşar. → `docs/frontend.md` § 5.
- **C6.** Kullanıcıya görünen metin koda gömülmez; sayı içeren cümleler ICU ile kurulur
  ("3 favori"). → Altın Kural 6, Yasak Listesi 31.
- **C7.** CSS'te fiziksel yön özellikleri kullanılmaz; renk ve tipografi `frontend-002`
  token katmanından gelir; ham hex/px yoktur.
- **C8.** Bileşenler `docs/frontend.md` § 4.2 envanterinden kullanılır; yeni ortak bileşen
  gerekçesiyle önerilir.
- **C9.** `localStorage` erişimi **hata verebilir** (gizli pencere, site verisi kapalı);
  her okuma ve yazma bu durumu karşılamalı ve arayüz çalışmaya devam etmeli.
- **C10.** `0001` ve `0002`'nin mevcut kabul kriterleri **regresyon olarak** korunur.
  → ADR 0003 unsur 1.
- **C11.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.

### KAPSAM DIŞI

- **X1.** **Favorilerin cihazlar arası eşitlenmesi** — *neden:* üyelik gerektirir ve § 6'da
  kapsam dışı.
- **X2.** **Favori aracı için fiyat düşüşü / satıldı bildirimi** — *neden:* sunucu kimin neyi
  favorilediğini bilmiyor (C2) ve `0018` § X3 bunu aynı gerekçeyle dışarıda bıraktı.
- **X3.** **Favori ve karşılaştırma sayaçlarının toplanması / analitiği** — *neden:*
  `Engagement` modülü yok; § 6 pazarlama analitiğini üçüncü partiye bıraktı.
- **X4.** **Karşılaştırmanın yazdırılması veya PDF'e aktarılması** — *neden:* ayrı bir
  biçimlendirme işi.
- **X5.** **Favori listesinin ziyaretçiye e-postayla gönderilmesi** — *neden:* e-posta adresi
  toplamak bir izin işidir (**L7**) ve `0018`'in konusu.
- **X6.** **Karşılaştırmada fiyat farkı hesabı veya puanlama** — *neden:* öneri motoru § 6'da
  kapsam dışı; "hangisi daha iyi" galerinin değil ziyaretçinin kararı.

---

## Context

### Mevcut davranış — doğrulandı

- **`0001` § X5 favorileri kapsam dışı bırakmıştı:** *"Favori, karşılaştırma, kaydedilmiş
  arama — public üyelik V1 kapsam dışıdır; favoriler `localStorage` kararı zaten
  verilmiştir."* ADR 0003 unsur 1 bu maddenin yeni bir spec'le açılmasını öngörüyor.
- **Karar zaten `docs/architecture.md` § 6'da:** *"Public üyelik / hesap sistemi | Favoriler
  `localStorage`'da. Üyelik KVKK yükü getirir, V1'de değer üretmez."* Yani **favorinin
  nerede duracağı tartışma konusu değil**; bu spec onu uygular.
- **`Engagement` modülü yok.** `docs/architecture.md` § 1 onu *"araç görüntülenme,
  telefon/WhatsApp tıklama, **favori**"* sorumluluğuyla sayıyor ve § 7 *"ince modül olarak
  var"* diyor — ama kodda yok. Bu, S3'ü gerçek bir soru yapıyor.
- **Mockup ne gösteriyor:** liste başlığında *"Favoriler · 3"* düğmesi, her kartın köşesinde
  kalp düğmesi (`aria-label="Favorilere ekle"`), ilan detayında **Favori** ve
  **Karşılaştır** düğmeleri, altlıkta *"Karşılaştırma"* bağlantısı. Karşılaştırma ekranının
  kendisi mockup'ta **çizilmemiş** — yani biçimi tasarım tarafında henüz kararlı değil.
- **İlan detayı slug ile adreslenir.** `0002` E5 kararı gereği adres kanonik Latin slug'dır
  ve `ShowcaseListingDetailDto` `Slug` taşıyor — R9'un paylaşılabilir karşılaştırma adresi
  için kullanılabilir kimlik budur (S5).
- **Liste kartı `ListingId` taşıyor**, detay ise `Slug`. İkisi farklı kimlikler; favori
  deposunun hangisini tutacağı bir tutarlılık sorusudur (S5).

### Bilinen tuzaklar

1. **`localStorage`'ı her zaman çalışır saymak.** Gizli pencerede veya site verisi kapalıyken
   okuma/yazma **hata atar**; sarmalanmazsa favori düğmesi tüm sayfayı düşürür (C9).
2. **Favorideki aracın satılması.** Ziyaretçi üç gün sonra döndüğünde favorisindeki araç
   satılmış olabilir; sessizce listeden silmek, ziyaretçiye "ben bunu eklemiş miydim"
   dedirtir (R10).
3. **Favori sayısını sunucuya göndermek.** Masum görünür ama `Engagement` yokken bunun
   gideceği bir yer yok ve açmak C2'yi deler (S3).
4. **Karşılaştırmada kapalı alanı göstermek.** Alan ayarları kapalı bir alanı karşılaştırma
   tablosunda göstermek, Yasak Listesi 41'in tam ihlali — hem de ziyaretçiye açık biçimde
   (R8).
5. **Geniş tabloyu sayfayı kaydırtarak çözmek.** `docs/frontend.md` mobil önceliklidir ve
   geniş tablonun **kendi içinde** kaydırılmasını istiyor (R14).
6. **Kalp ikonunu tek anlam taşıyıcısı yapmak.** Dolu/boş kalp ayrımı renk ve biçime dayanır;
   § 7 metin karşılığı ve ekran okuyucu bildirimi istiyor (R12).

---

## Açık sorular

**S1. Karşılaştırmaya en fazla kaç araç alınır?**
*Öneri:* **üç.**
*Gerekçe:* `docs/frontend.md` mobil öncelikli; dar ekranda dört sütun okunmaz hâle gelir ve
R14'ün "sayfayı yatay kaydırtmama" kuralı dört sütunda tutulamaz. Üç araç, gerçek bir
seçimin de pratik sınırı.
*Bedel:* dört araç arasında seçim yapan ziyaretçi iki tur yapar.

**S2. Favorilerin "bu cihaza ait" olduğu nasıl anlatılır?**
*Öneri:* favoriler ekranında **kalıcı ve sade bir not**: favorilerin bu tarayıcıda tutulduğu
ve tarayıcı verisi silinirse kaybolacağı yazılır. Uyarı kutusu veya kapatılabilir bildirim
kullanılmaz.
*Gerekçe:* beklentiyi önceden kurmak, sonradan "favorilerim nerede" sorusunu önler. Üyelik
olmadığı için bunu çözmenin başka yolu yok (X1).
*Bedel:* arayüzde kalıcı bir açıklama satırı; sade tutulmazsa gürültü olur.

**S3. Favori etkileşimi sunucuya hiç gitmeyecek mi? `Engagement` burada doğsun mu?**
*Öneri:* **hiç gitmesin;** `Engagement` bu spec'te **doğmaz.**
*Gerekçe:* C2 ve § 6. `Engagement`'ı "sadece favori sayacı için" var etmek, ince bir modülü
yarım bir sorumlulukla açar; o modül görüntülenme ve tıklama sayaçlarıyla birlikte, kendi
kayba toleranslı yazma modeliyle gelmeli. Ayrıca favori sayacını toplamak, ziyaretçi davranışı
toplamaktır ve KVKK aydınlatmasını etkiler.
*Bedel:* galeri "hangi araç en çok favorilendi" bilgisini **göremez** — ticari olarak değerli
bir bilgi, bilinçli olarak feda ediliyor. `Engagement` geldiğinde bu spec'in uç tarafına
bir yayın eklemek küçük bir iştir.

**S4. Karşılaştırmada hangi alanlar gösterilir?**
*Öneri:* **çekirdek alanlar** (model, yıl, kilometre, yakıt, vites, çekiş, renk, kasa tipi,
fiyat, şube) **artı** alan ayarlarında **açık ve public** opsiyonel alanlar — yani
`0002` R7/R8 ile aynı küme.
*Gerekçe:* ziyaretçinin detayda gördüğü bilgiyle karşılaştırmada gördüğü bilgi aynı olmalı;
ayrı bir küme tanımlamak iki gerçek üretir.
*Bedel:* çok sayıda opsiyonel alan açıksa tablo uzar; satırları katlamak (önce çekirdek,
sonra "daha fazla") bir arayüz kararı olarak gerekebilir.

**S5. Favori ve karşılaştırma hangi kimliği tutar; karşılaştırma adresi nasıl olur?**
Liste kartı `ListingId` (Guid), detay `Slug` taşıyor.
*Öneri:* ikisi de **slug** tutar; karşılaştırma adresi slug listesi taşır.
*Gerekçe:* slug kalıcı ve paylaşılabilir bir kimliktir (`0002` E5); Guid'li bir
karşılaştırma bağlantısı hem okunmaz hem de `0002`'nin adres kararıyla çelişir.
*Bedel:* liste kartı bugün slug taşımıyorsa sözleşmeye eklenmesi gerekir — **bu, backend
sözleşmesinin değişmesi demektir** ve başlıktaki "backend sözleşmesi değişmez" iddiasını
bozar. Bu taslakta `ShowcaseListingDto`'nun slug taşıyıp taşımadığı **doğrulanmadı**;
spec onaylanmadan önce kontrol edilmeli.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> favori ekleme/çıkarma (kart ve detay) · sayacın her sayfada görünmesi · favorilerin sayfa
> gezintisinde korunması · `localStorage` erişilemediğinde arayüzün çalışmaya devam etmesi ·
> azami karşılaştırma sayısı ve sınıra ulaşıldığında davranış · karşılaştırma alan kümesinin
> türetilmesi · kapalı/gizli alanın hiç görünmemesi · vitrinden düşmüş favori · iki ayrı boş
> durum · paylaşılabilir karşılaştırma adresi · klavye erişimi ve renk dışı ayırt
> edilebilirlik · mobilde yatay kaydırma olmaması · üç locale ve RTL ·
> `0001`/`0002` kriterlerinin regresyon olarak korunması.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı; **S5 kodda doğrulandı** ve sonucu Context'e yazıldı.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/frontend-003-ac<n>.png`); iki boş durum, mobil
      görünüm ve Arapça (RTL) görünüm dâhil.
- [ ] `localStorage` erişilemezken arayüzün çalıştığı testle gösterildi (C9).
- [ ] Favori verisinin sunucuya **gitmediği** doğrulandı (C2).
- [ ] Kapalı ve gizli alanların karşılaştırmada bulunmadığı testle gösterildi (R8, C3).
- [ ] `0001` ve `0002`'nin kabul kriterleri regresyon olarak hâlâ yeşil (C10).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Ham piksel/hex eklenmedi; token katmanı kullanıldı (C7).
- [ ] Yeni ortak bileşen eklendiyse gerekçesi PR'da yazılı (C8).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0001`/`0002`'yi bozup bozmadığı burada görünür. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
