# 0016 — Medya Galerisi ve Parça Haritası Şeması

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` public vitrin · `VehicleCatalog` (detay okuma yüzeyi) |
| **Bağımlı spec'ler** | `0002` (ilan detayı — tamamlandı; **X2** ve **X3**'ü geri açar) · **`0011` (Media) ÖNKOŞUL** |
| **Dayanak** | ADR 0003 unsur 1 ve Ek 1, sıra #10 |
| **Karar bekliyor** | **S1–S4** — § Açık sorular (4 soru, **4 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — dört açık sorunun cevabı
> onları belirliyor. **S1 sözlükle mockup arasında bir çelişkiyi açıyor.**

---

## Intent

İlan detayı bugün **tek bir kapak görseli** gösteriyor. `0002` galeriyi (§ X2) ve parça
haritasının görsel şemasını (§ X3) bilinçli olarak dışarıda bıraktı; opsiyonel alanların
**değerleri** gösteriliyor ama hasar bilgisi düz bir liste olarak duruyor.

İkinci el araç almak, insanın **fotoğrafa ve hasar geçmişine** baktığı bir iştir. Tek
görselle verilen karar, galeriyi aramakla sonuçlanmıyor — ziyaretçi yeterince bilgilenmeden
sayfadan çıkıyor. Hasar bilgisi düz liste olarak verildiğinde ise okunmuyor: "sağ ön çamurluk
boyalı, bagaj kapağı boyalı, sol arka kapı lokal boyalı" cümlesi, bir şemaya tek bakışta
eşdeğer değil.

**Galeri** bunu istiyor çünkü şeffaflık onun satış iddiası — mockup'ın giriş metni bile
*"Geçmişi gizlenmeyen araçlar"* diyor. Saklanmayan hasar, ancak **görülebildiğinde**
şeffaflık olur.

Bu iş bittiğinde ziyaretçi aracın tüm fotoğraflarını gezebilir ve hasar durumunu araç
üzerinde bir şema olarak görür. Başarı ölçütü: **ziyaretçi, aracın hangi parçasına ne olduğunu
tek bakışta anlıyor.**

---

## Requirements

- **R1.** İlan detayında ziyaretçi, ilanın **tüm görsellerini** gezebilir.
- **R2.** Kaçıncı görselde olduğu ve toplam görsel sayısı gösterilir.
- **R3.** Ziyaretçi görseller arasında **klavyeyle** gezebilir; her etkileşimli öğe klavyeyle
  erişilebilir ve odak halkası görünür. → `docs/frontend.md` § 7.
- **R4.** Ziyaretçi bir görseli **büyütebilir** ve büyütülmüş görünümden çıkabilir.
- **R5.** Her görselin, ziyaretçinin locale'ine karşılık gelen `alt` metni vardır.
  → `0011` R9, `docs/frontend.md` § 7.
- **R6.** Görseller türevlerden yüklenir; ilk görsel dışındakiler **gecikmeli** yüklenir ve
  açık `width`/`height` ile basılır. → `docs/frontend.md` § 7 (CLS, LCP).
- **R7.** İlan detayı, **parça haritası** alanı açık ve public ise aracın hasar durumunu
  **araç üzerinde bir şema** olarak gösterir.
- **R8.** Şema **13 parçayı** ayrı ayrı gösterir ve her parçanın durumunu belirtir.
  Durum kümesi için bkz. Açık soru **S1**.
- **R9.** Parça durumu **yalnızca renkle** anlatılmaz; her parçanın durumu metinle de
  okunabilir. → `docs/frontend.md` § 7.
- **R10.** Şemanın yanında, parça haritasının **okunabilir listesi** bulunur; şema
  yüklenemese veya anlaşılmasa bile bilgi erişilebilir kalır.
- **R11.** Şema, parça haritası alanı **kapalı veya gizliyse** hiç gösterilmez ve yanıtta
  bulunmaz. → **F3**, Yasak Listesi 41.
- **R12.** Galeri ve şema **yükleniyor**, **boş** (tek görsel veya hasar kaydı yok) ve
  **hata** durumlarını ele alır. → `docs/frontend.md` § 3.
- **R13.** Üç locale'de çalışır; Arapça'da RTL — şemanın yönü ve parça adlarının hizalaması
  dâhil. → tasarımcı notu: *"araç silüeti"* de aynalanır.

---

## Constraints

### Uyulması zorunlu

- **C1.** Gizli ve kapalı alanlar yanıtta **hiç bulunmaz**; eleme sorgu tarafında uygulanır.
  → **F3**, Yasak Listesi 41.
- **C2.** Renk **tek başına anlam taşımaz**; ayırt edilmesi gereken durumlar açıklıkta da
  farklılaşır. → `docs/frontend.md` § 7.
- **C3.** Şemanın renkleri `frontend-002`'nin **token katmanından** gelir; gömülü hex
  yoktur. → `docs/frontend.md` § 4.1.
- **C4.** CSS'te fiziksel yön özellikleri kullanılmaz; şema RTL'de aynalanır.
  → Yasak Listesi 25.
- **C5.** Kullanıcıya görünen metin koda gömülmez; parça adları ve durum adları çeviri
  kataloğundan gelir. → Altın Kural 6.
- **C6.** Veritabanında çevrilmiş durum adı saklanmaz; kod saklanır. → Yasak Listesi 22.
- **C7.** Görseller `Media`'nın türevlerinden gelir; orijinal vitrine servis edilmez.
  → `0011` R3.
- **C8.** `0002`'nin mevcut kabul kriterleri **regresyon olarak** korunur.
  → ADR 0003 unsur 1.
- **C9.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.

### KAPSAM DIŞI

- **X1.** **Video ve 360° tur** — *neden:* `0011` § X2 bunları üretmiyor; oynatıcı ayrı bir
  iştir.
- **X2.** **Panelde parça haritası editörü** (tıkla-seç arayüzü) — *neden:* `0010a` § X4 bunu
  bu spec'le birlikte ele alınmak üzere ertelemişti; **bu spec yalnızca ziyaretçi tarafını
  kapsar.** Editör, panel tarafında ayrı bir dilimdir. → Açık soru **S4**.
- **X3.** **Ekspertiz raporu dosyasının görüntülenmesi** — *neden:* dosya opsiyonel bir
  alandır ve PDF görüntüleme ayrı bir karardır.
- **X4.** **Görsel karşılaştırma / yan yana görünüm** — *neden:* karşılaştırma sıra #12'nin
  işi.
- **X5.** **Tramer tutarının grafikle gösterimi** — *neden:* tutar bir sayıdır; `0002` onu
  zaten gösteriyor.
- **X6.** **Görsellerin indirilmesi veya paylaşılması** — *neden:* filigran politikası
  (`0011` R4) kopyalamayı zorlaştırmak içindir; indirme düğmesi onunla çelişir.

---

## Context

### Mevcut davranış — doğrulandı

- **`0002` tamamlandı** ve iki şeyi açıkça dışarıda bıraktı: § X2 *"Medya galerisi, görsel
  büyütme, video ve 360° tur"*, § X3 *"Boyalı–değişen parça haritasının görsel şeması
  (13 parça)"* — ikincisinin gerekçesi *"alanın değeri R8 kapsamında gösterilir; şemanın
  kendisi ayrı bir görselleştirme işidir"*.
- **Detay sözleşmesi bugün tek görsel taşıyor.** `ShowcaseListingDetailDto` alanları:
  `Slug`, `IsReserved`, `CoverImageUrl`, `ModelName`, `ModelYear`, `MileageKilometers`,
  `Price`, `BranchName`, `Attributes`. **Görsel listesi yok** — R1 sözleşmenin büyümesini
  gerektirir.
- **`Attributes` etiketi çözülmüş nitelik satırları taşıyor** (`ShowcaseAttributeDto(Key,
  Label, Value)`); parça haritası bugün büyük olasılıkla bu düz listede görünüyor. R10 bu
  listeyi **korumayı** şart koşuyor.
- **`Media` bugün yok** (tek olay dosyası) — bu yüzden `0011` **önkoşuldur**: galerinin
  göstereceği türevler orada üretilir.
- **Mockup ne gösteriyor:** detayda "1 / 18" sayacı ve `[ARAÇ FOTOĞRAFI]` yer tutucusu;
  altında 13 parça adıyla bir şema (*"Galeri beyanı · diyagramın üstü aracın ön kısmıdır"*),
  tramer tutarı ve ağır hasar bayrağı. Tasarımcı notu `s1`: *"üstten araç görünümü: 13
  kaporta parçası kendi biçiminde, aralarında camlar, dışarıda tekerlek ve aynalar. **Dört
  durum: orijinal, lokal boyalı, boyalı, değişen.**"*
- **Çelişki:** `docs/domain.md` **Parça Haritası**'nı *"13 parçanın **orijinal / boyalı /
  değişen** durumu"* diye tanımlıyor — **üç durum.** Mockup dört diyor. → Açık soru **S1**.

### Geçerli kurallar

**Parça Haritası** (`PaintChangeMap`, opsiyonel alan) · **Tramer** (`DamageRecordAmount`,
opsiyonel) · **F1** (kapatmak silmek değildir) · **F3/F4** (gizli alan yanıtta yok) ·
`docs/architecture.md` § 4 (opsiyonel alan listesi; iki eksen: form durumu ve public
görünürlük) · `docs/frontend.md` § 3, § 7.

### Bilinen tuzaklar

1. **Şemayı tek bilgi kaynağı yapmak.** Şema yüklenemezse veya ekran okuyucuyla gezilirse
   hasar bilgisi kaybolur; R10'un listesi bu yüzden zorunlu.
2. **Durumları yalnızca renkle ayırmak.** Dört (veya üç) durumu yalnızca renk tonuyla
   göstermek, § 7'yi kırar ve renk körü ziyaretçi için şemayı anlamsız yapar (C2, R9).
3. **Şemayı sabit yönlü çizmek.** RTL'de araç silüeti ve parça adları aynalanmalı; fiziksel
   yön özellikleri kullanılırsa Arapça sayfada şema ters durur (C4).
4. **Galeriyi kapak görselinin yerine koymak.** İlk görsel LCP adayıdır; galeriye geçerken
   onu gecikmeli yüklemeye düşürmek `0001`/`0002`'nin LCP eşiğini bozar (R6).
5. **Parça adlarını koda gömmek.** 13 parça adı üç dilde çevrilmeli; `docs/architecture.md`
   § 4 kural 4 alan etiketlerinin sabit anahtar olduğunu ve çevirilerin hazır geldiğini
   söylüyor — parça adları da aynı sınıfa girmeli (C5).

---

## Açık sorular

**S1. Parça durumu üç mü dört mü? "Lokal boyalı" sözlüğe girecek mi?**
`docs/domain.md` üç durum diyor; mockup dört diyor ve mockup'ın açıklama metni de
*"sol arka kapı **lokal** boyalıdır"* diye kullanıyor.
*Öneri:* **dördüncü durum eklensin** ve sözlükteki **Parça Haritası** tanımı güncellensin.
*Gerekçe:* "lokal boyalı" Türkiye ikinci el pazarının gerçek bir ayrımıdır ve fiyata etki
eder; ekspertiz modeli de *"Türkiye modelidir"* (`docs/architecture.md` § 4). Üç duruma
sıkıştırmak, galeriyi "boyalı" demeye zorlar ve aracın değerini olduğundan düşük gösterir —
yani sözlük burada pazarın gerisinde kalmış görünüyor.
*Bedel:* bu bir **sözlük değişikliğidir** ve kararı sizindir (rol kuralı: Analist sözlüğe
terim **önerir**). Ayrıca dört durum, C2 gereği dört ayrı açıklık/desen ayrımı demek —
görsel tasarım yükü artar.
*Elenen alternatif:* mockup'ı sözlüğe indirip üç durumla çizmek — daha az iş, ama galerinin
elindeki gerçek bilgiyi kaybeder.

**S2. Şema nasıl üretilir — kod içinde çizim mi, varlık olarak mı?**
*Öneri:* **kod içinde satır içi çizim**; 13 parça ayrı ayrı adreslenebilir öğeler olarak.
*Gerekçe:* durum renkleri token katmanından gelmek zorunda (C3) ve her parçanın kendi
erişilebilir adı olmalı (R9); yüklenen bir görsel dosyası ikisini de yapamaz. `0011` § X1'de
yüklenen SVG'lerin `<style>` ve animasyondan arındırıldığı not da bu yönü destekliyor.
*Bedel:* şema kaynak kodda yaşar; tasarım değişikliği bir kod değişikliğidir.

**S3. Galeri görsel listesi sözleşmeye nasıl eklenir?**
Detay sözleşmesi bugün yalnızca `CoverImageUrl` taşıyor.
*Öneri:* detay yanıtı **sıralı bir görsel listesi** taşır; kapak listenin ilk elemanıdır.
*Gerekçe:* `docs/domain.md` **Kapak Görseli** tanımı zaten *"sıralı medya listesinin ilk
elemanı"* diyor — yani model bunu öngörmüş; sözleşme onu yansıtmıyor.
*Bedel:* `0002`'nin sözleşme testi değişir; `CoverImageUrl`'ün korunup korunmayacağı bir
geri uyumluluk kararıdır (korunmasını öneririm — `0001`'in kart yanıtı onu kullanıyor).

**S4. Panel editörü bu spec'te mi, ayrı mı?**
`0010a` § X4 editörü *"görüntüleme şemasıyla birlikte ayrı bir spec"* diye ertelemişti; bu
spec onu § X2 ile yine dışarıda bırakıyor.
*Öneri:* **ayrı kalsın**; bu spec ziyaretçi tarafını bitirsin.
*Gerekçe:* ziyaretçi tarafı tek başına değer üretir (veri `0010a`'da girilebiliyor);
editör panel tarafının etkileşim yükünü (tıkla-seç, geri al, 13×4 durum) getirir ve iki
ekranı tek spec'te doğrulamak kabul kriterlerini ortak hakemden koparır.
*Bedel:* `0010a` § X4 ile bu spec § X2 birbirine işaret ediyor — **editörün hiçbir spec'te
sahibi yok.** Sıraya eklenmesi gerekir; bunu ADR 0003 Ek 1'e yeni bir madde olarak
öneriyorum ama **sıra kararı sizin**.

---

## Acceptance Criteria

> Dört açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> görseller arası gezinme (fare, klavye, dokunma) · sayaç · büyütme ve çıkış · `alt` metni
> ve locale · LCP/CLS davranışı · şemanın 13 parçasının her biri · her durum değeri ·
> renk dışı ayırt edilebilirlik · okunabilir liste · alan kapalı/gizliyken şemanın hiç
> bulunmaması · tek görsel ve hasar kaydı olmayan araç · hata durumu · RTL'de şema yönü ·
> `0002`'nin kriterlerinin regresyon olarak korunması.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı; **S1 bir sözlük kararı gerektiriyorsa**
      `docs/domain.md` güncellendi ve ADR gerekip gerekmediği değerlendirildi.
- [ ] `0011` tamamlandı.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0016-ac<n>.png`); şemanın her durumu, boş ve hata
      durumları, Arapça (RTL) görünüm dâhil.
- [ ] Şemanın renk dışı ayırt edilebilirliği kanıtla gösterildi (C2, R9).
- [ ] Klavyeyle tam gezinme ve odak halkası kanıtla gösterildi (R3).
- [ ] `0002`'nin kabul kriterleri regresyon testi olarak hâlâ yeşil (C8).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi — 13 parça adı ve durum adları dâhil.
- [ ] Ham piksel/hex eklenmedi; token katmanı kullanıldı (C3).
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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0002`'yi bozup bozmadığı burada görünür. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
