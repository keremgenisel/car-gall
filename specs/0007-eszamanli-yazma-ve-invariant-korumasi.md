# 0007 — Eşzamanlı Yazma ve Invariant Koruması

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` · `VehicleCatalog` · `Identity` · `Leads` · `BuildingBlocks` |
| **Bağımlı spec'ler** | `0003` (şubeler, tamamlandı) · `0006` (kimlik — bir örneği orada nokta çözümle kapatıldı) |

---

## Intent

Sistemde "her zaman en az bir X kalır" biçiminde kurallar var ve bunlar bugün **tek bir
kullanıcı varmış gibi** korunuyor: kod önce sayıyor, sonra karar veriyor, sonra yazıyor —
aradaki pencerede başka kimsenin aynı şeyi yapmadığı varsayılıyor.

**Galeri yöneticisi**, iki kişinin aynı anda yaptığı işlemin sistemi kullanılamaz hâle
getirmemesini istiyor. Bugün çektiği acı görünmez olanı: iki yönetici aynı dakikada iki
farklı kişiyi pasifleştirdiğinde galeride hiç yönetici kalmayabiliyor ve **sisteme bir daha
kimse giremiyor** — kurulum hesabı var olan hesaba dokunmadığı için oradan dönüş de yok.

Bu iş bittiğinde, "her zaman en az bir aktif yönetici/şube kalır" gibi kurallar **eşzamanlı
istekler altında da** tutar; bunu her modülün kendi yöntemiyle değil, evin tek bir deseniyle
yapar; ve deseni izlemeyen yeni bir yazma yolu eklendiğinde bunu **bir test söyler**.

---

## Requirements

- **R1.** Sistem, "en az bir N kalır" biçimindeki kuralları eşzamanlı istekler altında da
  korur: iki istek aynı anda son elemanı kaldırmaya çalıştığında **yalnızca biri** geçer.
- **R2.** Koruma, okuma–karar–yazma üçlüsünü **bölünemez** kılar. Kararın dayandığı sayı,
  yazma anında hâlâ geçerli olmak zorundadır.
- **R3.** Koruma **evin tek bir deseniyle** kurulur ve o desen `BuildingBlocks`'ta yaşar;
  her modül kendi çözümünü yazmaz. *(Gerekçe: bugün üç modülde aynı kural üç kez yazılmış
  olsaydı, üçü de ayrı ayrı yanlış olabilirdi.)*
- **R4.** Desen, geriye dönük olarak **var olan tüm invariant korumalarına** uygulanır:
  `Identity`'nin son aktif Yönetici koruması (R12) ve `Dealership`'in son aktif şube
  koruması (B5) dâhil.
- **R5.** Deseni izlemeyen bir yazma yolu eklendiğinde bunu bir test bildirir; koruma
  gözden geçirmeye bırakılmaz.
- **R6.** Çakışma hâlinde kaybeden istek, ziyaretçiye/personele **anlaşılır ve kararlı bir
  hata kodu** ile döner; teknik ayrıntı sızdırmaz ve "tekrar deneyin" dışında bir şey
  vaat etmez.

---

## Constraints

### Uyulması zorunlu

- **C1.** Gerçek PostgreSQL üzerinde sınanır; SQLite in-memory ile eşzamanlılık testi
  yapılmaz — kilit ve izolasyon davranışı farklıdır (`docs/testing.md` § 1).
- **C2.** Modül sınırı korunur: ortak desen `BuildingBlocks`'ta, kullanımı her modülün
  kendi Application katmanında (`docs/architecture.md` § 2.B).
- **C3.** Yeni migration yalnız kendi şemasına dokunur.
- **C4.** Çözüm, uygulama rolünün kalıcı DDL yetkisine ihtiyaç duymamalı — `architecture.md`
  § 5 şema başına ayrı rol istiyor.
- **C5.** Hata gövdesi çevrilmiş metin taşımaz; uç kendi kataloğundan çevirir
  (Yasak Listesi 23).

### KAPSAM DIŞI

- **X1.** Genel bir "iyimser eşzamanlılık" (her tabloya `RowVersion`) getirmek — *neden:*
  bu spec'in derdi **invariant**'lar, her satırın kaybolan-güncelleme sorunu değil.
  İkisini birlikte çözmek işi ölçülemez hâle getirir.
- **X2.** Dağıtık kilit / harici koordinasyon (Redis vb.) — *neden:* sistem tek veritabanlı
  bir modüler monolit; veritabanının kendi kilidi yeterli ve bir bileşen daha eklemek
  `architecture.md`'nin sadeliğini bozar.
- **X3.** Outbox dağıtıcısının eşzamanlılığı — *neden:* teslimat zaten en-az-bir-kez ve
  idempotent; ayrı bir konu.
- **X4.** `Identity`'de 0006 ile kapatılan noktanın yeniden yazılması **zorunlu değildir** —
  *neden:* orada satır kilidiyle çalışan bir çözüm var ve testi yeşil. Bu spec onu ortak
  desene taşır, ama taşımanın bedeli faydasını aşarsa olduğu gibi bırakmak da bir sonuçtur;
  karar burada, ölçüyle verilir.

---

## Context

**Mevcut davranış.** Depoda hiçbir modülde `Serializable` izolasyon, `RowVersion` veya
`IsConcurrencyToken` yok. İnvariant korumaları şu desende yazılmış:

```csharp
int otherActiveManagers = await StaffQueries...CountAsync(...);   // oku
Result result = staff.Deactivate(otherActiveManagers);            // karar ver
await unitOfWork.SaveChangesAsync(ct);                            // yaz
```

**Etkilenen yerler:**

| Kural | Yer | Bugünkü durum |
|---|---|---|
| R12 — en az bir aktif Yönetici | `Identity/Application/StaffCommands.cs`, `StaffQueries.cs` | **0006'da nokta çözümle kapatıldı**: `SELECT … FOR UPDATE` + tek transaction |
| B5 — en az bir aktif şube | `Dealership/Domain/Branch.cs:163` (`Close`) | **Açık, ama bugün ulaşılamaz**: `D1` gereği `Dealership`'in yazma ucu yok; `Close` üretim yolundan çağrılmıyor |

Yani bugün **canlı** bir açık yok; ama `Dealership`'e yazma ucu geldiği gün (şube yönetimi
spec'i) aynı hata onunla birlikte doğar. Bu spec, o gün gelmeden deseni yerine koymak için.

**Önceki kararlar.** `docs/architecture.md` § 7 Karar Defteri'nde eşzamanlılık üzerine bir
satır **yok** — bu spec'in çıktılarından biri o satırdır.

**Bilinen tuzak.** Yalnız "diğer" satırları kilitlemek işe yaramaz: iki istek iki farklı
satırı kilitler ve hiç serileşmezler. Kilit kümesi, karara giren **tüm** satırları kapsamalı
ve sabit bir sırada alınmalı (`ORDER BY id`), yoksa karşılıklı kilitlenme oluşur. 0006'daki
nokta çözüm bunu böyle yapıyor; ortak desen de yapmak zorunda.

---

## Acceptance Criteria

- **AC-1.** İki eşzamanlı istek son aktif Yöneticiyi pasifleştirmeye çalıştığında yalnızca
  biri başarılı olur; diğeri `identity.last_active_manager` ile reddedilir.
- **AC-2.** AC-1'in ardından galeride **tam olarak bir** aktif Yönetici kalır.
- **AC-3.** İki eşzamanlı istek son aktif Yöneticinin rolünü düşürmeye çalıştığında yalnızca
  biri başarılı olur.
- **AC-4.** Pasifleştirme ve rol düşürme **birbirine karşı da** serileşir: biri
  pasifleştirirken diğeri rol düşürüyorsa ikisi birden geçemez.
- **AC-5.** İki eşzamanlı istek son aktif şubeyi kapatmaya çalıştığında yalnızca biri
  başarılı olur; diğeri `branch.last_active_branch` ile reddedilir.
- **AC-6.** AC-5'in ardından galeride **tam olarak bir** aktif şube kalır.
- **AC-7.** Reddedilen istek, başarısız olduğunu **kararlı bir hata koduyla** bildirir;
  gövdede yığın izi, SQL metni veya kilit/transaction terimi geçmez.
- **AC-8.** Karara giren satırlar sabit bir sırada kilitlenir; eşzamanlı iki istek karşılıklı
  kilitlenmeye (deadlock) düşmez — test bunu tekrarlı turlarla doğrular.
- **AC-9.** Korumanın olmadığı hâlde test **kırmızıya döner**: kilit kaldırıldığında en az
  bir kriter düşer. *(Testin doğru sebeple yeşil olduğunun kanıtı.)*
- **AC-10.** Ortak desen `BuildingBlocks`'ta tek bir yerde yaşar; modüller onu çağırır,
  kendi kopyalarını yazmaz — mimari test bunu doğrular.
- **AC-11.** Invariant koruyan bir komut ortak deseni kullanmadan eklendiğinde bir test
  bunu bildirir.
- **AC-12.** Koruma, işlem başarısız olduğunda **hiçbir yan etki bırakmaz**: reddedilen
  istekte ne satır değişir ne outbox'a duyuru düşer.
- **AC-13.** Eşzamanlılık testleri gerçek PostgreSQL üzerinde koşar ve rastgele kırılmaz
  (AP-03): aynı test art arda beş turda da aynı sonucu verir.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriterlerinin **görsel kanıtı** eklendi — *bu spec'te arayüz kriteri yoksa
      "yok" yazılır.*
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
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
