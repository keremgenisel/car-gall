# 0023 — Yanlışlıkla "Satıldı" Yapılan İlanın Geri Alınması

| | |
|---|---|
| **Durum** | Taslak — kriterler yazıldı, QA CLARIFY turunu bekliyor |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (yeni geçiş, satış öncesi durumun saklanması, geri alma kaydı, migration) · `frontend/` panel (ilan eylem çubuğu, onay penceresi, geri alma geçmişi) · `docs/domain.md` (**A3**, **A4** değişir) |
| **Bağımlı spec'ler** | `0012b` (yayın ve durum eylemleri — tamamlandı; **R4**'ü ve Bilinen tuzak 4'ü bu kartla değişir) · `0012c` (öne çıkarma — tamamlandı) · `0003`/A8 (şube ataması bekleyen araç) · `0006` (roller) |
| **Dayanak** | Depo sahibinin 2026-11-18 isteği ve kararı (**D1**) |
| **Karar bekliyor** | yok — **D1–D11** verildi. QA CLARIFY turu bekleniyor. |

---

## Intent

Bir ilan yanlışlıkla **"Satıldı"** yapıldığında bugün geri alınamıyor. Kural (`docs/domain.md`
**A4**) bunu bilerek terminal tuttu: satılmış bir araç geri gelirse yeni bir kayıt açılır. Ama
yanlış bir tıklama satış değildir. Bugünkü tek çıkış — ilanı arşivleyip aynı aracı baştan
girmek, fotoğrafları yeniden yüklemek, ilanın adresini ve vitrindeki sırasını kaybetmek —
bir hatayı düzeltmek için orantısız ve araç vitrinden günlerce düşebilir.

Bunu **galeri yöneticisi** istiyor. Başarı şuna benziyor: yönetici yanlışlıkla satılmış
işaretlenen ilanda "Satışı geri al" der, bir gerekçe yazar ve ilan satıştan önceki hâline —
aynı adres, aynı fotoğraflar, aynı sıra — döner. Ne yapıldığı, kimin yaptığı ve neden yapıldığı
kayıtta kalır; sıradan personel bu kapıyı kullanamaz.

---

## Requirements

- **R1.** **Yönetici**, durumu **Satıldı** olan bir ilanın satışını **geri alabilir** (**D1**).
- **R2.** Geri alma bir **gerekçe** ister: boş olamaz, en az 10, en fazla 500 karakterdir
  (**D4**).
- **R3.** Geri alınan ilan **satıştan önceki durumuna** döner — **Yayında** ya da **Rezerve**
  (**D2**). Satıştan önceki durumu bilinmeyen (bu kart gelmeden önce satılmış) ilan **Yayında**ya
  döner (**D3**).
- **R4.** Aracı bugün vitrine çıkamayacak bir ilan — şube ataması bekleyen araç (A8) — geri
  alındığında **Taslak**'a döner, yayına girmez (**D5**).
- **R5.** Geri alma ilanın **adresini**, **ilk yayın tarihini** (dolayısıyla vitrindeki sırasını),
  fotoğraflarını, fiyatını ve **öne çıkarma** durumunu değiştirmez (**D6**).
- **R6.** Her geri alma **kayda geçer**: ilan, geri alan personel, an, gerekçe ve dönülen durum.
  Kayıt silinmez ve düzenlenmez (**D7**).
- **R7.** Yönetici bir ilanın geri alma kayıtlarını panelde o ilanın ekranında görür.
- **R8.** Geri alma, satışın da kendisinin olduğu gibi sistemde **duyurulur** (**D8**).
- **R9.** Geri alma bir **onay** ister: yönetici ilanın hangi duruma döneceğini görür ve gerekçeyi
  aynı pencerede yazar.
- **R10.** **Arşiv**deki ilan geri alınamaz; geri alma yalnız **Satıldı** durumundan yapılır.
- **R11.** Aynı ilan için eşzamanlı iki geri alma ya da geri alma ile arşivleme yarışında yalnız
  biri başarılı olur (`0012b` eşzamanlılık deseni, `0007`).
- **R12.** Satış Danışmanı ve İçerik Editörü geri alma eylemini görmez; uç onlara `403` döner.
- **R13.** Geri alma **toplu eylem** değildir; ilan ilan yapılır (**D9**).

---

## Constraints

### Uyulması zorunlu

- **C1.** Geçiş agregatın kendi yöntemiyle yapılır; durum doğrudan atanmaz. Duyuru değişiklikle
  aynı işlemde outbox'a yazılır (`docs/architecture.md` § 2.C).
- **C2.** Migration yalnız `catalog` şemasına dokunur.
- **C3.** `docs/domain.md` **A3** geçiş tablosu ve **A4** aynı PR'da güncellenir; bu kart onlarla
  çelişen bir davranış bırakmaz.
- **C4.** Kullanıcıya görünen hiçbir metin koda gömülmez (Altın Kural 6).
- **C5.** Gerekçe kişisel veri taşıyabilir; kayıt dışında hiçbir loga yazılmaz
  (`docs/conventions.md` § 12).

### KAPSAM DIŞI

- **X1.** **Süre sınırı** ("satıştan sonra 30 gün içinde") — *neden:* sahip belirtmedi; gerekçe ve
  kayıt kötüye kullanımı görünür kılıyor (**D10**).
- **X2.** **Arşivden geri dönüş** — *neden:* arşiv silmenin karşılığıdır ve A5'in şase
  muafiyetini doğurur; geri dönüş tekillik çakışması üretebilir.
- **X3.** **Satış bilgisinin kaydı** (satış fiyatı, alıcı, satış tarihi) — *neden:* bugün tutulmuyor;
  bu kart yalnız geri almayı getirir.
- **X4.** **Satış Danışmanı'nın geri alma talebi göndermesi** — *neden:* onay akışı ayrı bir karar.
- **X5.** **"Satıldı" düğmesine onay eklemek** — *neden:* sahip geri almayı seçti (onay seçeneğini
  değil); `0012b`'nin bugünkü onay davranışı değişmez.

---

## Context

### Mevcut davranış — ölçüldü (2026-11-18, `main` @ `0cd834d`)

- **Satış yalnız bir durum.** `Listing.MarkSold()` (`Domain/Listing.cs:213`) `Yayında` ya da
  `Rezerve`'den `Satıldı`ya geçiriyor ve `ListingSold` üretiyor; satış öncesi durum, satış anı
  ya da satan kişi **saklanmıyor**.
- **Duyuru var, dinleyeni yok.** `ListingSold` outbox'a `VehicleSold` olarak yazılıyor
  (`VehicleCatalogUnitOfWork.cs:123`, `VehicleCatalog.Contracts/Events/VehicleEvents.cs`); başka
  hiçbir modül dinlemiyor.
- **Geçiş tablosu** `docs/domain.md` **A3**: `Satıldı → Arşiv` tek çıkış. **A4**: "Satıldı
  terminal; aynı araç geri alınırsa yeni kayıt".
- **Panel bilerek düğme koymuyor.** `ListingActionBar.tsx:133` yorumu: "A4 — Satıldı terminaldir;
  geri al düğmesi kuralı delerdi". `0012b` Bilinen tuzak 4 aynı gerekçeyi yazıyor.
- **İlk yayın tarihi donuk.** `Publish` `PublishedAt ??=` ile ilk yayında donduruyor ve adres de
  ilk yayında bir kez yazılıyor (`Listing.cs`, `0012b` **D13**, **D31**); geri alma bunları
  korumak için yayına alma yolunu **kullanmamalı** (yeni bir geçiş).
- **Öne çıkarma satışla silinmiyor.** `MarkSold` `ShowcaseSortWeight`'e dokunmuyor
  (`Listing.cs:274-300`); geri alınan ilan satıştan önceki öne çıkarma durumuyla döner.
- **Denetim kaydı emsali.** `0013` ikinci adımın kaydını `identity` şemasında kendi tablosunda
  tutuyor (`docs/architecture.md` § Karar Defteri, "İkinci adımın kaydı") — bu depodaki ilk
  denetim kaydı.

### Verilen kararlar — 2026-11-18

**D1 — Sahibin kararı (depo sahibi, 2026-11-18).** Yalnız **Yönetici** geri alır; gerekçe
zorunlu; işlem kayda geçer; satış geçmişi silinmez, üstüne "geri alındı" yazılır.

**D2 — Satış öncesi durum saklanır (Takım Yöneticisi (ajan) kararı).** (R3) `MarkSold` satıştan
önceki durumu ilan üzerinde saklar; geri alma ona döner. *Gerekçe:* sahip "önceki durumuna döner"
dedi; rezerve bir aracın rezervasyonu yanlış bir tıklamayla kaybolmamalı.

**D3 — Bilinmeyen önceki durum → Yayında (ajan).** (R3) *Gerekçe:* bu karttan önce satılan ilan
ya Yayında ya Rezerve'den geldi; ikisi de vitrindeydi. Rezervasyon bilgisi zaten kayıtlı değil.

**D4 — Gerekçe 10–500 karakter (ajan).** (R2) *Gerekçe:* "x" gibi bir gerekçe kaydı anlamsız
kılar; üst sınır kaydı ve ekranı korur.

**D5 — Vitrine çıkamayacak araç Taslak'a döner (ajan).** (R4) *Gerekçe:* A8'in "şube ataması
bekliyor" bayrağı taşıyan araç yayına alınamıyor; geri alma bu kuralı arka kapıdan delmemeli.

**D6 — Yeni bir geçiş; yayına alma yolu kullanılmaz (ajan).** (R5) *Gerekçe:* adres, ilk yayın
tarihi ve sıra korunmalı; `Publish` yalnız `Taslak`'tan çalışıyor ve kendi olayını üretiyor.

**D7 — Kayıt `catalog` şemasında kendi tablosunda (ajan).** (R6, R7) `0013`'ün denetim kaydı
deseni. Kayıt ekleme dışında değişmez. *Gerekçe:* satış geçmişinin "silinmemesi, üstüne geri
alındı yazılması" — sahibin kararı — bir kayıt satırıdır.

**D8 — Yeni duyuru: satış geri alındı (ajan).** (R8) `VehicleSold` gibi outbox'a yazılır.
*Gerekçe:* bugün `VehicleSold`'u dinleyen yok, ama yarın satışları sayan bir tüketici (rapor)
geri almayı görmezse yanlış sayar. Simetri ucuz ve sözleşmenin yarım kalmasını önler.

**D9 — Toplu eylem yok (ajan).** (R13) *Gerekçe:* her geri alma kendi gerekçesini ister.

**D10 — Süre sınırı yok (ajan).** (X1)

**D11 — `docs/domain.md` A3/A4 güncellenir (ajan).** (C3) A3: `Satıldı → Arşiv | Yayında | Rezerve |
Taslak` — son üçü **yalnız** "satışı geri al" geçişiyle ve yalnız Yönetici'ye. A4: "Satıldı
ticari olarak terminaldir; yanlış işaretleme Yönetici'nin gerekçeli geri almasıyla düzeltilir.
Aynı fiziksel araç **gerçekten** geri alınırsa yine yeni kayıt açılır."

---

## Acceptance Criteria

### Geri alma ve dönülen durum (R1, R3, R4 · D2, D3, D5)

- **AC-1.** Yayındayken satıldı işaretlenen ilanın satışı Yönetici tarafından geri alındığında ilan **Yayında** olur ve vitrinde yeniden görünür.
- **AC-2.** Rezervedeyken satıldı işaretlenen ilanın satışı geri alındığında ilan **Rezerve** olur ve vitrinde rezerve olarak görünür.
- **AC-3.** Bu kart gelmeden önce satıldı işaretlenmiş (önceki durumu kayıtlı olmayan) ilanın satışı geri alındığında ilan **Yayında** olur.
- **AC-4.** Şube ataması bekleyen bir aracın satılmış ilanı geri alındığında ilan **Taslak** olur ve vitrinde görünmez.
- **AC-5.** Geri alınan ilan, onay penceresinde gösterilen duruma döner.

### Korunanlar (R5 · D6)

- **AC-6.** Geri alınan ilanın adresi değişmez; eski adres ilanı açar.
- **AC-7.** Geri alınan ilanın ilk yayın tarihi değişmez ve vitrin sırasındaki yeri satıştan öncekiyle aynıdır.
- **AC-8.** Geri alınan ilanın fotoğrafları ve fiyatı değişmez.
- **AC-9.** Satıştan önce öne çıkarılmış ilan geri alındığında yine öne çıkarılmış olarak döner.

### Gerekçe ve onay (R2, R9 · D4)

- **AC-10.** Geri alma onay penceresi ilanın döneceği durumu ve bir gerekçe alanı gösterir; gerekçesiz onay düğmesi etkin değildir.
- **AC-11.** 10 karakterden kısa gerekçe sunucuda reddedilir ve ilan Satıldı kalır.
- **AC-12.** 500 karakterden uzun gerekçe reddedilir.
- **AC-13.** Yalnız boşluktan oluşan gerekçe boş sayılır ve reddedilir.
- **AC-14.** Onaydan vazgeçmek ilanı değiştirmez ve kayıt yazmaz.

### Kayıt ve duyuru (R6, R7, R8 · D7, D8)

- **AC-15.** Her başarılı geri alma bir kayıt yazar: ilan, geri alan personel, an, gerekçe ve dönülen durum.
- **AC-16.** Reddedilen bir geri alma kayıt yazmaz.
- **AC-17.** Yönetici ilanın panel ekranında geri alma kayıtlarını en yenisi üstte, personel adı, an, gerekçe ve dönülen durumla görür.
- **AC-18.** Geri alma kaydını düzenleyen ya da silen bir uç yoktur.
- **AC-19.** Başarılı geri alma "satış geri alındı" duyurusunu değişiklikle aynı işlemde outbox'a yazar.
- **AC-20.** Aynı ilan geri alınıp yeniden satıldı işaretlenip yeniden geri alınabilir; iki kayıt da durur.

### Geçersiz durumlar ve yarış (R10, R11)

- **AC-21.** Arşivdeki ilan için geri alma reddedilir.
- **AC-22.** Yayında, Rezerve ya da Taslak bir ilan için geri alma reddedilir.
- **AC-23.** Aynı ilan için eşzamanlı iki geri alma isteğinden yalnız biri başarılı olur ve yalnız bir kayıt yazılır.
- **AC-24.** Geri alma ile arşivleme eşzamanlı istendiğinde yalnız biri başarılı olur.

### Erişim (R12, R13 · D9)

- **AC-25.** Satış Danışmanı ve İçerik Editörü satılmış ilanda "Satışı geri al" eylemini görmez.
- **AC-26.** Yönetici olmayan rolün geri alma ucuna isteği `403` alır ve ilan değişmez.
- **AC-27.** Toplu eylem çubuğunda "Satışı geri al" seçeneği yoktur.

### Panel durumları ve dil

- **AC-28.** Geri alma sunucu hatasıyla düştüğünde pencere teknik ayrıntı göstermeyen bir hata gösterir ve yazılan gerekçe kaybolmaz.
- **AC-29.** Yeni metinler TR/AR/EN kataloglarında var; eksik anahtar kontrolü yeşil.
- **AC-30.** Arapça panelde onay penceresi ve kayıt listesi sağdan sola çalışır.

### Belge (C3 · D11)

- **AC-31.** `docs/domain.md` A3 geçiş tablosu ve A4 bu kartın davranışını anlatır; `ListingActionBar.tsx`'teki "A4 — geri al düğmesi kuralı delerdi" yorumu kaldırılır.

---

## Definition of Done

- [ ] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0023-ac<n>.png`),
      hata durumu dâhil; Arapça görüntü de var (AC-30).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `catalog` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**satışı geri alma**); A3 ve A4 güncellendi.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [ ] `0012b` R4 ve Bilinen tuzak 4'e "→ `0023`" notu düşüldü.

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
