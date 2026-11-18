# 0022 — Ana Sayfa Metinlerinin Panelden Düzenlenmesi

| | |
|---|---|
| **Durum** | Taslak — kriterler yazıldı, QA CLARIFY turunu bekliyor |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (ana sayfa metinleri — yeni kayıt, yazma ve okuma uçları, migration) · `Host` (metnin ilk boyamada hazır olması — plan turu, **D6**) · `frontend/` vitrin (ana sayfa, boş stok durumu) ve panel (İçerik bölümünde yeni ekran) |
| **Bağımlı spec'ler** | `0014` (ana sayfa — tamamlandı; metinleri bu kartla düzenlenebilir olur) · `0017` (Content modülü, İçerik Editörü yetkisi, eşzamanlılık deseni — tamamlandı) · `0020` (üstteki galeri adı — **D2**) |
| **Dayanak** | Depo sahibinin 2026-11-18 isteği ve seçimi (**D1**) |
| **Karar bekliyor** | yok — **D1–D10** verildi. QA CLARIFY turu bekleniyor. |

---

## Intent

Ana sayfanın yazıları yazılımın içinde sabit. "Geçmişi gizlenmeyen araçlar." başlığı, altındaki
paragraf, "Aracınızı bulun" ve "Öne çıkan araçlar" başlıkları ve stok boşken çıkan mesaj her
galeride aynı ve galeri onları değiştiremiyor; değiştirmek bir yazılım güncellemesi istiyor.
Galerinin kendi sesiyle konuşamayan bir vitrin, white-label bir ürünün vaadini tutmuyor.

Bunu **galeri yöneticisi ve içerik editörü** istiyor. Başarı şuna benziyor: panelde bu metinleri
üç dilde ayrı ayrı yazarlar; ziyaretçi ana sayfayı bir sonraki açışında yeni metni görür; bir
metin boş bırakılırsa ziyaretçi bugünkü varsayılan metni görür — sayfa hiçbir zaman boş başlıkla
açılmaz. Sayfa bugünkü kadar hızlı açılır.

---

## Requirements

- **R1.** Panelde **ana sayfa metinleri** ekranı şu alanları **etkin her dil için ayrı ayrı**
  sunar (**D1**):
  1. giriş başlığı,
  2. giriş açıklaması,
  3. "Aracınızı bulun" bölüm başlığı,
  4. "Öne çıkan araçlar" bölüm başlığı,
  5. stok boşken gösterilen mesajın başlığı,
  6. stok boşken gösterilen mesajın açıklaması.
- **R2.** Her alan **opsiyoneldir.** Bir dilde boş bırakılan alan için ziyaretçi o dildeki
  **varsayılan metni** — bugün gösterilen metni — görür (**D3**). Panel, boş alanın yerinde
  varsayılan metni ipucu olarak gösterir.
- **R3.** Metinler **düz metindir**: biçimlendirme, bağlantı ya da HTML yorumlanmaz ve girilen
  karakterler olduğu gibi görünür (**D4**). Açıklama alanlarında satır sonu korunur.
- **R4.** Başlık alanları en fazla **120**, açıklama alanları en fazla **500** karakterdir;
  sınırı aşan değer alan hatasıyla reddedilir (**D5**).
- **R5.** Kaydedilen metin ziyaretçiye **bir sonraki sayfa açışında** görünür.
- **R6.** Stok boşken gösterilen mesaj, bu mesajın görüldüğü **her yerde** düzenlenen metni
  kullanır: ana sayfa ve vitrin listesi (**D7**).
- **R7.** Metinler sayfanın **ilk boyamasında** hazırdır: ziyaretçi önce varsayılan metni görüp
  sonra değişen bir metin görmez, sayfa kaymaz ve hız eşikleri bugünkü gibi tutar (**D6**).
- **R8.** Aynı metinleri **eski bir sürüm üzerinden** kaydetmek reddedilir; editör bunu bir
  uyarıyla görür ve güncel hâli yükleyebilir (`0017` **D18** deseni).
- **R9.** Ekran ve yazma ucu **İçerik Editörü ve Yönetici** içindir; Satış Danışmanı ekrana
  bağlantı görmez, adresi doğrudan açınca "erişim yok" ekranını görür ve yazma ucu ona `403`
  döner (**D8**).
- **R10.** Ekran yükleniyor, hata durumlarını ele alır; üç dilde ve Arapçada sağdan sola çalışır.
  Arapça metin alanları sağdan sola yazılır.

---

## Constraints

### Uyulması zorunlu

- **C1.** Metinler `Content` modülünde yaşar; başka modül onları yalnız `Content.Contracts`
  üzerinden okur (Yasak Listesi 1). Merkezi bir "çeviriler" tablosu kurulmaz (`0017` **C2**).
- **C2.** Varsayılan metinler **çeviri kataloğunda** kalır (`home.intro.title` vb.); kayıt yalnız
  galerinin yazdığı değeri taşır. Kayıt hiç yokken (yeni kurulum) sayfa bugünkü gibi görünür.
- **C3.** Migration yalnız `content` şemasına dokunur.
- **C4.** `0014`'ün performans kapısı (LCP < 2,5 sn, CLS < 0,1) ana sayfa ve listede yeşil kalır.
- **C5.** Girilen metin hiçbir aşamada HTML olarak yorumlanmaz (`0017` **C** gövde kuralının
  düz metin karşılığı) — sunucu basıyorsa kodlanır, uç gösteriyorsa metin düğümü olarak.

### KAPSAM DIŞI

- **X1.** **Üstteki küçük galeri adı** — *neden:* depo sahibi bunu da seçti; `0020` **D4** onu
  galerinin dil başına **görünen adı** olarak getiriyor ve o alan galeri ekranında düzenleniyor
  (**D2**).
- **X2.** **Ana sayfa dışındaki metinler** (form etiketleri, düğmeler, altlık başlıkları, hata
  mesajları) — *neden:* istek ana sayfanın içerik metinleriydi; arayüz metinleri ürünün parçası.
- **X3.** **Biçimlendirme ve bağlantı** — **D4**.
- **X4.** **Ana sayfa görseli ya da bölüm sırası/gizleme** — *neden:* istek metindi.
- **X5.** **Taslak/yayın ayrımı ve geçmiş sürümler** — *neden:* `0017`'nin sayfalarındaki yayın
  akışı burada aşırı; metin kaydedilince yayındadır (R5).
- **X6.** **Boş stok mesajının bağlantı metni** ("Galeriye ulaşın") — *neden:* seçilen
  "stok boşken mesaj" başlık ve açıklamadır; bağlantı bir eylem etiketidir (X2).

---

## Context

### Mevcut davranış — ölçüldü (2026-11-18, `main` @ `0cd834d`)

- **Metinler çeviri kataloğundan.** `frontend/src/app/HomePage.tsx:21-22` giriş başlığı ve
  açıklamasını `home.intro.title` / `home.intro.body` anahtarlarından; form ve öne çıkanlar
  başlıkları `home.form.title`, `home.featured.title`; boş stok mesajı
  `showcase.empty.showcase.title` / `.body` / `.action` (`frontend/src/shared/i18n/locales/*.json`).
- **Boş stok bileşeni ortak.** `features/catalog/states/EmptyShowcase.tsx` hem ana sayfada
  (`HomeCatalog.tsx:83`) hem vitrin listesinde (`ShowcaseListPage.tsx`) kullanılıyor.
- **Giriş başlığı ilk boyanan metin.** Ana sayfa stoğu boşken ve görsel yüklenmeden önce giriş
  bölümü sayfanın en büyük metin bloğu; performans kapısı (`perf.yml`, `lighthouse-run.mjs`) ana
  sayfayı ölçüyor (`0014` **D10**). Metni ayrı bir istekle sonradan getirmek ya LCP'yi geciktirir
  ya da varsayılan metnin değişmesiyle kayma üretir.
- **Sunucunun bastığı değerler emsali.** Etkin diller `0014` **D22** ile `index.html`'e sunucudan
  basılıyor (`IndexHtmlRenderer`); tema da öyle (ADR `0005`).
- **Content modülü** sayfaları ve SSS'yi tutuyor (`Content/Domain/ContentPage.cs`); eşzamanlılık
  sürüm kolonuyla (`ContentPageConfiguration.cs:40`); panel yetkisi `AdminSurface.ContentPolicy`
  (İçerik Editörü + Yönetici).

### Verilen kararlar — 2026-11-18

**D1 — Sahibin seçimi (depo sahibi, 2026-11-18).** Panelden değiştirilecekler: giriş başlığı ve
açıklaması, bölüm başlıkları, stok boşken mesaj, üstteki küçük galeri adı — üç dilde ayrı ayrı.

**D2 — Galeri adı `0020`'de (Takım Yöneticisi (ajan) kararı).** (X1) *Gerekçe:* aynı ad hem üst
çubukta hem giriş satırında görünüyor; iki ayrı yerde düzenlenirse ikisi ayrışır.

**D3 — Boş alan varsayılana düşer (ajan).** (R2, C2) *Gerekçe:* üç dilin hepsini yazmayan bir
galerinin Arapça ana sayfası boş başlıkla açılmamalı; varsayılan metin bugün zaten üç dilde var.

**D4 — Düz metin (ajan).** (R3, X3) *Gerekçe:* başlık ve kısa paragraf biçim istemiyor; düz metin
enjeksiyon yüzeyini sıfırda tutar.

**D5 — Uzunluk sınırları 120 / 500 (ajan).** (R4) *Gerekçe:* başlık tek satırlık bir öğe,
açıklama kısa bir paragraf; sınırsız metin ana sayfanın düzenini ve LCP'yi bozar.

**D6 — Metin ilk boyamada hazır (ajan).** (R7, C4) Nasıl taşınacağı plan turunun işidir; `0014`
**D22**'nin sunucunun bastığı değer emsali tercih edilir. *Gerekçe:* ayrı bir istek ya LCP'yi
geciktirir ya da varsayılandan özele geçerken kayma üretir.

**D7 — Boş stok mesajı her yerde aynı (ajan).** (R6) *Gerekçe:* bileşen ortak; aynı durum için iki
farklı mesaj ziyaretçiyi şaşırtır.

**D8 — İçerik Editörü ve Yönetici (ajan).** (R9) *Gerekçe:* `0017`'nin içerik yetkisi; ana sayfa
metni içerik işidir. `0020`'deki galeri adı ise Yönetici'de kalır (yasal/kimlik bilgisiyle aynı
ekranda).

**D9 — Ekran İçerik bölümünde (ajan).** Panelin İçerik bölümünde sayfaların ve SSS'nin yanında
bir "Ana sayfa" girdisi. *Gerekçe:* İçerik Editörü'nün gördüğü tek bölüm orası.

**D10 — Yayın akışı yok (ajan).** (X5) Kaydetmek yayındır.

---

## Acceptance Criteria

### Ekran ve kaydetme (R1, R2, R5 · D3, D9)

- **AC-1.** İçerik Editörü panelin İçerik bölümünde "Ana sayfa" girdisinden metin ekranını açar; ekran altı alanı etkin her dil için ayrı ayrı gösterir.
- **AC-2.** Boş bir alanın yerinde o dilin varsayılan metni ipucu olarak görünür.
- **AC-3.** Türkçe giriş başlığı "Kadıköy'ün güvenilir galerisi" olarak kaydedildikten sonra açılan ilk Türkçe ana sayfa bu başlığı gösterir.
- **AC-4.** Türkçe giriş başlığı kaydedildiğinde İngilizce ve Arapça ana sayfa kendi varsayılan başlıklarını göstermeye devam eder.
- **AC-5.** Kaydedilmiş bir alan boşaltılıp kaydedildiğinde ana sayfa o dilde yeniden varsayılan metni gösterir.
- **AC-6.** Hiç kayıt yapılmamış bir kurulumda ana sayfanın altı metni bugünkü varsayılan metinlerle aynıdır.
- **AC-7.** "Aracınızı bulun" ve "Öne çıkan araçlar" başlıkları kaydedildikten sonra ana sayfada kaydedilen metinle görünür.

### Boş stok mesajı (R6 · D7)

- **AC-8.** Yayında hiç ilan yokken ana sayfa, kaydedilmiş boş stok başlığını ve açıklamasını gösterir.
- **AC-9.** Yayında hiç ilan yokken vitrin listesi aynı kaydedilmiş başlığı ve açıklamayı gösterir.
- **AC-10.** Boş stok mesajının bağlantı metni değişmez ("Galeriye ulaşın").

### Düz metin ve sınırlar (R3, R4 · C5 · D4, D5)

- **AC-11.** `<b>Kalın</b> & "tırnak"` olarak kaydedilen giriş başlığı ana sayfada bu karakterlerle metin olarak görünür; kalın yazılmaz ve sayfada yeni bir öğe oluşmaz.
- **AC-12.** `<script>` içeren bir açıklama kaydedildiğinde ana sayfa açılınca hiçbir betik çalışmaz.
- **AC-13.** Açıklamaya yazılan satır sonu ana sayfada yeni satır olarak görünür.
- **AC-14.** 121 karakterlik bir başlık ve 501 karakterlik bir açıklama alan hatasıyla reddedilir; kayıtlı metin değişmez.
- **AC-15.** 120 karakterlik başlık ve 500 karakterlik açıklama kabul edilir.

### İlk boyama ve hız (R7 · C4 · D6)

- **AC-16.** Kaydedilmiş bir giriş başlığı olan ana sayfanın sunucudan gelen ilk yanıtı ya da ilk boyaması kaydedilmiş metni içerir; ziyaretçi varsayılan metni hiç görmez.
- **AC-17.** Kaydedilmiş metinlerle ana sayfa için performans kapısı (LCP < 2,5 sn, CLS < 0,1) yeşildir.
- **AC-18.** Liste için performans kapısı yeşil kalır.

### Eşzamanlılık (R8)

- **AC-19.** Okunduktan sonra başka bir kaydetmeyle değişmiş sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı metin değişmez.
- **AC-20.** Reddedilen kaydetmede panel uyarı gösterir, güncel hâli yükleme yolu sunar ve editörün yazdıkları ekranda kalır.

### Erişim (R9 · D8)

- **AC-21.** Yönetici de ekranı açar ve kaydeder.
- **AC-22.** Satış Danışmanı İçerik bölümünde "Ana sayfa" girdisini görmez; adresi doğrudan açtığında "erişim yok" ekranını görür.
- **AC-23.** Satış Danışmanı'nın yazma ucuna isteği `403` alır ve metin değişmez.
- **AC-24.** Oturumsuz yazma isteği `401` alır.

### Durumlar ve dil (R10)

- **AC-25.** Ekran veri gelirken yükleniyor durumunu gösterir.
- **AC-26.** Veri okunamadığında teknik ayrıntı göstermeyen bir hata ve yeniden deneme gösterilir.
- **AC-27.** Kaydetme sunucu hatasıyla düştüğünde yazılanlar kaybolmaz.
- **AC-28.** Arapça alanlar sağdan sola yazılır; Arapça kaydedilen giriş başlığı Arapça ana sayfada sağdan sola düzende görünür.
- **AC-29.** Yeni ekranın metinleri TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.
- **AC-30.** Etkin dil listesinden çıkarılmış bir dilin alanları ekranda gösterilmez.

---

## Definition of Done

- [ ] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0022-ac<n>.png`),
      boş ve hata durumları dâhil; Arapça görüntü de var (AC-28).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `content` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**ana sayfa metinleri**).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4); performans kapısı kaydedilmiş metinlerle de ölçüldü (AC-17).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.

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
<!-- 1-2 cümle. Bir sonraki spec'i nasıl daha iyi yazarız? -->
