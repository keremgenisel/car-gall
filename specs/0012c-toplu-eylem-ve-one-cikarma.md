# 0012c — Toplu Eylem ve Öne Çıkarma

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (toplu komut yüzeyi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | **`0012b` ÖNKOŞUL** (tekil geçiş komutları, duyurular, yansıtma) · `0010b` (panel listesi) |
| **Dayanak** | `0012b` **D39** — `0012`'nin üçüncü dilimi |
| **Karar bekliyor** | Yok. Kararlar `0012b`'de verildi ve buraya **taşındı** (D3–D7, D16, D18, D27–D29, D36). Kriterler yazılmadı. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı; `0012b` merge edildikten sonra
> kendi kriter turunda yazılacak.

---

## Intent

`0012b` bittiğinde galeri **bir aracı** yayına alabiliyor, rezerve edebiliyor, satıldı
işaretleyebiliyor ve arşivleyebiliyor. Ama günlük iş bir araç üzerinde değil **liste
üzerinde** yürüyor: sezon sonunda on beş aracı birlikte yayından kaldırmak, fuar öncesi
yirmi aracı birlikte yayına almak.

Bu iş bittiğinde personel **birden çok ilanı seçip** aynı eylemi hepsine uygulayabilir ve
bir ilanı **öne çıkarabilir**. Başarı ölçütü: **yirmi aracın durumunu değiştirmek yirmi
sayfa gezinmeyi gerektirmiyor.**

---

## Requirements

- **R1.** Personel **birden çok ilanı seçip** aynı eylemi hepsine uygulayabilir. Toplu
  uygulanabilen eylemler: yayına al · yayından kaldır · satıldı işaretle · arşivle · öne
  çıkar · **öne çıkarmayı kaldır**. Rezerve etme ve rezervasyonu kaldırma **tek ilan**
  üzerinde çalışır. → `0012b` **D18**, **D36**
- **R2.** Toplu eylemde bazı ilanlar için eylem geçersizse, **geçerli olanlar uygulanır** ve
  uygulanamayanlar **tek tek gerekçesiyle** bildirilir; işlem bir bütün olarak geri alınmaz.
  Bulunamayan ilan isteği değil **o satırı** düşürür; zaten hedef durumda olan kayıt
  **başarısız** sayılır ve sonuç satırı kaydın **güncel durumunu** taşır.
  → `0012b` **D3**, **D27**, **D28**
- **R3.** Seçim boş olamaz, yinelenen kimlikler teklenir ve seçim sayısı panelin sayfa üst
  sınırını aşamaz — aşan seçim **kırpılmaz, reddedilir**; sınır **dâhildir** ve
  tekilleştirme sınırdan **önce** gelir. Şube kapanışı yolu (`0012b` **D33**) bu sınıra
  tabi **değildir**. → `0012b` **D4**, **D5**, **D29**, **D33**
- **R4.** Yanıt `200` ve gövdesi **ilan başına sonuç listesidir**; hepsi başarısız olsa bile.
  → `0012b` **D6**, `docs/conventions.md` § 5
- **R5.** Personel bir ilanı **öne çıkarabilir** ve öne çıkarmayı kaldırabilir; öne çıkarılan
  ilan vitrin sıralamasının başında görünür. Eylem **arşiv dışındaki her ilanda** çalışır,
  zaman damgasını ilerletir ve geliştirme seed'i iki değerli anahtara hizalanır.
  → `0012b` **D16**, **D36**
- **R6.** Ekran **yükleniyor**, **boş**, **başarılı**, **kısmi başarı** ve **hata**
  durumlarını ele alır. → `docs/frontend.md` § 3, `0012b` **D7**
- **R7.** Panel personelin tercih ettiği dilde çalışır; Arapça'da RTL.

---

## Constraints

### Uyulması zorunlu

- **C1.** Tüm eylemler **oturum gerektirir**.
- **C2.** Toplu eylem **tekil komutların üzerine** kurulur; durum geçişi mantığı `0012b`'nin
  agregatında kalır ve burada tekrarlanmaz.
- **C3.** İşlem sınırı **ilan başınadır** (`0012b` **D3**); tek transaction kullanılmaz.
- **C4.** Çapraz modül okumaları **istek başına bir kez** yapılır; ilan başına değil.
- **C5.** Sayı içeren cümleler ICU ile kurulur ("3 ilan yayına alındı"). → Yasak Listesi 31
- **C6.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner.
- **C7.** Her kabul kriteri en az bir testle karşılanır.

### KAPSAM DIŞI

- **X1.** **Rezerve etme ve rezervasyonu kaldırmanın toplu hâli** — *neden:* rezervasyon bir
  müşteriye aittir; yirmi aracı kimseye rezerve etmek, `0012b` X1'in dışarıda bıraktığı
  kaydın yerine geçmeye çalışmaktır (`0012b` **D18**).
- **X2.** **Öne çıkarılmış ilanların kendi aralarında sıralanması** — *neden:* `0012b`
  **D16** anahtarı iki değerli tuttu; eşitlik yayın tarihiyle kırılıyor ve gerçek sıralama
  `0015`'in işi.
- **X3.** **Dışa aktarma ve toplu alan doldurma sihirbazı** — *neden:* `0012b` X5 ve
  `docs/architecture.md` § 6.

---

## Context

### Mevcut davranış — `0012b` bittiğinde

- Tekil geçiş komutları ve altı uç hazır (`0012b` **D26**'nın HTTP sözleşmesiyle).
- Duyuru altyapısı ve sekiz olay hazır (`0012b` **D11**, **D14**, **D30**).
- Vitrin yansıtması her yazma yolunda koşuyor (`0012b` **D12**, **D41**).
- Panel listesi arşiv filtresini (sayaçlar dâhil) ve "lokasyon ataması bekliyor" rozetini
  taşıyor (`0012b` **D19**, **D35**, **D15**).
- `Listing.ChangeShowcaseSortWeight` **hâlâ çağıransız**: bu spec onu `Feature()`/
  `Unfeature()` ile saracak.
- Geliştirme seed'i öne çıkarma kolonuna `1..N` yazıyor (`ShowcaseSeeder.cs:139`) ve
  **D16**'nın iki değerli anahtar kararını veri tarafında deliyor; **D36** hizalamayı
  bu spec'e bağladı.

### Taşınan kararlar

`0012b`'nin iki CLARIFY turunda verilen ve **burada tüketilecek** kararlar: **D3** (ilan
başına transaction) · **D4** (üst sınır = panelin sayfa üst sınırı) · **D5** (boş seçim
reddedilir, yineleme teklenir) · **D6** (`200` + satır listesi) · **D7** (kısmi başarı,
dördüncü ekran durumu) · **D16** (öne çıkar iki değerli) · **D18** (hangi eylemler toplu) ·
**D27** (bulunamayan ilan satırı düşürür) · **D28** (zaten hedef durumdaki kayıt başarısız
sayılır) · **D29** (seçim kırpılmaz, reddedilir) · **D36** (toplu kaldırma, arşiv ön koşulu,
seed'in hizalanması).

Bu kararlar `0012b`'de **verildi ve orada donmuş hâlde duruyor**; bu spec onları yeniden
tartışmaz, uygular.

### Bilinen tuzaklar

1. **Toplu eylemi tek işlem sanmak.** 20 ilandan 3'ü geçersizse "hepsi başarısız" demek
   personeli tek tek denemeye zorlar; "hepsi başarılı" demek yalan söyler. → `0012b` **D3**
2. **Başarıyı HTTP kodundan okumak.** `200` istek düzeyinde başarıdır, kayıt düzeyinde
   değil; istemci gövdeyi okumak zorundadır. → `docs/conventions.md` § 5
3. **Çapraz okumaları ilan başına yapmak.** 100 ilanlık bir seçimde 200 modüller arası okuma
   üretir; `0012b`'nin yayın komutu okumaları küme alacak biçimde yazdı.
4. **Seçimi sayfadan bağımsız sanmak.** "Hepsini seç" sayfadakileri seçer; sonraki sayfaya
   geçince seçimin ne olduğu tanımlanmak zorunda.

---

## Açık sorular

**S1. "Hepsini seç" sayfa değiştiğinde ne olur?**
*Öneri:* seçim **sayfaya aittir** ve sayfa değişince temizlenir.
*Gerekçe:* üst sınır zaten sayfa üst sınırı (`0012b` **D4**); sayfalar arası biriken bir
seçim o sınırı anlamsız kılar ve personelin görmediği ilanlara eylem uygular.
*Bedel:* iki sayfadaki araçlara aynı eylemi uygulamak iki tur demektir.

---

## Acceptance Criteria

> **Kriter turunda yazılacak.** En az şu kümeleri kapsayacak:
>
> kısmi başarı · boş seçim · yinelenen kimlik · üst sınır aşımı · hepsi başarısız olsa da
> `200` · her başarısız satırın kendi `errorCode`'u · beş toplu eylemin her biri · öne
> çıkarmanın vitrin sıralamasına yansıması · öne çıkarmanın duyuru **üretmemesi** · beş
> ekran durumu · çok dil ve RTL.

---

## Definition of Done

<!-- MADDE ÇIKARILAMAZ. Şablondaki her maddenin bir `<!-- DoD:Tn -->` işaretleyicisi vardır
     ve mimari test (ADR 0008 / K8) sayıyı şablondan okuyarak denetler. -->

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] `0012b` tamamlandı (tekil komutlar ve duyurular hazır).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012c-ac<n>.png`),
      **kısmi başarı** ve hata durumları dâhil, RTL için Arapça görüntü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil; durum geçişi mantığı agregatta kaldı.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca `vehicle_catalog` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.
- [ ] Kısmi başarının **dördüncü ekran durumu** olarak basıldığı görsel kanıtla gösterildi.
- [ ] Öne çıkarmanın duyuru **üretmediği** testle gösterildi (`0012b` **D16**).
- [ ] Seçim sınırının **dâhil** olduğu, **kırpılmadığı** ve tekilleştirmenin sınırdan
      **önce** geldiği testle gösterildi (`0012b` **D29**).

---

## SCORECARD

<!--
İş bittikten SONRA doldurulur. Amaç suçlu bulmak değil, sürecin nerede sızdırdığını görmek.
Dürüst doldurulmayan scorecard işe yaramaz — abartılmış iyi sayılar en pahalı yalandır.
-->

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | evet / hayır | R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır. |
| **Kriter sayısı: önce → bugün** | → | O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. |
| **Spec revizyon sayısı** | | Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3). Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? **`0` ile `—` aynı şey değildir.** **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı. Dış bulguyla aynı sütunda toplanmaz. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır.** Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak (ADR 0008 / K3). |

**Bu spec'ten çıkarılan ders:**
