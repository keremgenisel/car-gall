# 0010b — Panel: Araç Düzenleme ve Liste

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | atanmadı — **sırası:** `0010a` merge → sahip ataması |
| **Analist** | Developer (ajan) — `0010`'un bölünmesinden çıktı |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (yazma ve okuma yüzeyi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | **`0010a` (araç kaydı — önce bitmeli)** · `0006` · `0007` · `frontend-001` |
| **Dayanak** | `0010a` Karar **D9** |

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
- **R2.** Personel, **alan ayarlarında açık** olan opsiyonel alanları doldurabilir. Kapalı
  alanlar formda görünmez.
- **R3.** Alan ayarlarında **zorunlu** işaretli bir opsiyonel alan boş bırakılarak araç
  **taslak olarak kaydedilebilir**; zorunluluk yalnızca yayına alınırken bağlayıcıdır.
  → `docs/domain.md` **A2**, **F2**.
- **R4.** Bir alanın zorunlu yapılması, o alanı boş olan **mevcut** araçları geçersiz
  kılmaz ve düzenlemeye kapatmaz; bu araçlar **"eksik bilgi"** olarak işaretlenir ve bu
  işaret listede görünür. → `docs/domain.md` **F2**.
- **R5.** Personel, ilan başlığını ve açıklamasını **etkin her dilde** (TR/EN/AR) ayrı ayrı
  yazabilir. Bir dilde yazmamak geçerli bir durumdur.
- **R6.** Panel, her araç için **hangi dillerde metnin eksik olduğunu** gösterir.
- **R7.** Panel, kayıtlı araçları bir **listede** gösterir: araç tanımı, fiyat, kilometre,
  yayın durumu, **eksik dil** göstergesi, **eksik bilgi** göstergesi ve son güncelleme zamanı.
- **R8.** Liste **yapılandırılan sayfa boyutuyla** sayfalanır
  (`docs/conventions.md` § 3 — `?page=&pageSize=` + `PagedResult<T>`, sabit üst sınır) ve
  personel listeyi **son güncellenen** veya **yeni eklenen** sırasına göre görebilir.
  Her iki sıralamada da eşitlik **araç kimliğiyle** kırılır.
  *(Gerekçe: kırıcı olmadan aynı dakikada güncellenmiş iki araç kararsız sıralanır ve aynı
  araç iki sayfada görünebilir — `0001`'in SCORECARD'ında kayıtlı ders.)*
- **R9.** Liste, kaç ilanda **çeviri eksik** olduğunu özet olarak gösterir.
- **R10.** Liste ekranı **yükleniyor**, **boş** ve **hata** durumlarını ele alır.
  → `docs/frontend.md` § 3.
- **R11.** Düzenleme ekranı `0010a` R22'nin dört durumunu aynı biçimde ele alır.

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

### KAPSAM DIŞI

`0010a`'nın X1–X17'si geçerlidir. Ek olarak:

- **X19.** **Araç kaydının silinmesi** — *neden:* **F1** "kapatmak silmek değildir" diyor ve
  silme `Arşiv` durumuna aittir; o da `0012`'nin (yayın ve durum eylemleri) işidir.
- **X20.** **Eşzamanlı düzenlemede kaybolan güncelleme koruması** — *neden:* `0010a` X16 ile
  aynı gerekçe. **Bu spec'te risk `0010a`'dakinden büyüktür:** düzenleme ekranı gelince iki
  danışmanın aynı aracı açıp kaydetmesi sıradan bir senaryo olur.

---

## Context

### Mevcut davranış — `0010a` sonrası

`0010a` şunları bırakır: `vehicles` / `listings` / `price_changes` tabloları, yazma modeli
kalıcılığı, altı referans kümesinin okuma yüzeyi, araç oluşturma komutu ve ucu, şase
tekilliği (`InvariantGuard`), fiyat geçmişi, ve **sade** bir liste (araç tanımı, fiyat,
kilometre).

Eksik bırakılanlar bu spec'in kapsamıdır: **düzenleme komutu ve ucu yok**, `Listing`'de
**başlık/açıklama alanı yok**, listede **sayfalama, sıralama ve göstergeler yok**.

### Bilinen tuzaklar

1. **"Eksik bilgi"yi bayrak olarak saklamak.** C21; ayar değiştiğinde bayrak bayatlar ve
   geriye dönük doğrulama yasağı (C7) delinmiş olur.
2. **Sıralama kırıcısını atlamak.** Son güncelleme zamanı aynı olan iki araçta kırıcı yoksa
   sayfalama kararsızlaşır ve aynı araç iki sayfada görünür — `0001`'in dersi.
3. **Düzenlemede çekirdek alanı gevşetmek.** `0010a` D8 "taslak = çekirdek alanların hepsi
   dolu" diyor; düzenleme bunu gevşetirse aynı invariant iki yerde iki farklı şey olur.
4. **Kapatılan alanın değerini silmek.** **F1**: alan kapatmak değeri silmez; yalnız yanıta
   girmez (F3).

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler görsel kanıt gerektirir (`docs/testing.md` § 3).
> Numaralandırma bu spec'te **AC-1'den** başlar; `0010a`'nın numaraları ayrı eksendir ve
> testler `0010b/AC-<n>` önekiyle ayrılır (`docs/testing.md` § 1).

**Düzenleme**

- **AC-1.** Kaydedilmiş bir araç düzenlenir ve değişiklik listede görünür.
- **AC-2.** Oturumu olmayan biri araç düzenleyemez; istek reddedilir.
- **AC-3.** Düzenlemede de çekirdek alanlardan biri boşaltılarak kaydedilemez.

**Opsiyonel alanlar ve alan ayarları**

- **AC-4.** `[görsel]` Alan ayarlarında **açık** bir opsiyonel alan formda görünür ve değeri
  kaydedilir.
- **AC-5.** `[görsel]` Alan ayarlarında **kapalı** bir opsiyonel alan formda görünmez.
- **AC-6.** Alan ayarlarında **zorunlu** işaretli bir opsiyonel alan boş bırakılmış araç,
  taslak olarak **kaydedilebilir**.
- **AC-7.** `[görsel]` Bir alan zorunlu yapıldığında, o alanı boş olan mevcut araçlar
  düzenlemeye kapanmaz ve listede **"eksik bilgi"** olarak işaretlenir.
- **AC-8.** Kapatılan bir alanın daha önce girilmiş değeri silinmez; alan yeniden
  açıldığında eski değer görünür.

**Çok dilli metin**

- **AC-9.** Personel ilan başlığını ve açıklamasını TR, EN ve AR için ayrı ayrı yazar ve
  üçü bağımsız saklanır.
- **AC-10.** Yalnızca bir dilde metni olan araç kaydedilebilir.
- **AC-11.** `[görsel]` Panel, metni eksik olan dilleri araç başına gösterir.
- **AC-12.** Liste, kaç ilanda çeviri eksik olduğunu özet olarak gösterir.

**Liste**

- **AC-13.** `[görsel]` Liste araç tanımını, fiyatı, kilometreyi, yayın durumunu, eksik dil
  göstergesini, **eksik bilgi** göstergesini ve son güncelleme zamanını gösterir.
- **AC-14.** Liste yapılandırılan sayfa boyutuyla sayfalanır; ikinci sayfa ilk sayfadan
  farklı kayıtlar gösterir.
- **AC-15.** Liste **son güncellenen** ve **yeni eklenen** sıralamalarıyla farklı sıralar
  üretir.
- **AC-16.** Son güncelleme zamanı aynı olan araçlarda sıralama **araç kimliğiyle** kırılır;
  birinci ve ikinci sayfa birlikte hiçbir aracı tekrarlamaz ve hiçbirini atlamaz.

**Durumlar**

- **AC-17.** `[görsel]` Liste yüklenirken iskelet gösterilir.
- **AC-18.** `[görsel]` Hiç araç kaydı yokken liste, henüz araç olmadığını bildirir ve
  **araç ekle** eylemini sunar.
- **AC-19.** `[görsel]` Liste yüklenemediğinde ekran, çeviri kataloğundaki hata mesajı
  anahtarının karşılığını ve **tekrar dene** eylemini gösterir.
- **AC-20.** Hiçbir hata ekranında HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya
  istisna metni görünmez.

**Dil ve RTL**

- **AC-21.** `[görsel]` Arapça tercih eden personel için düzenleme formu ve liste
  `dir="rtl"` ile basılır.
- **AC-22.** `[görsel]` Liste başlıkları ve göstergeler TR, AR ve EN'de kendi dilinde
  görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] `[görsel]` işaretli **her** kriterin kanıtı eklendi (`docs/evidence/0010b-ac<n>.png`),
      boş ve hata durumları dâhil; AC-21 için Arapça görüntü de var.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] Yeni migration varsa yalnızca `vehicle_catalog` şemasına dokunuyor.
- [ ] **"Eksik bilgi" hesaplanıyor, saklanmıyor** (C21) ve ayar değişince hiçbir yazma
      olmadan değiştiği testle gösterildi.
- [ ] **Sayfalama kararlılığı** birinci ve ikinci sayfa BİRLİKTE alınarak gösterildi
      (AC-16): çakışma yok, atlama yok.
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
