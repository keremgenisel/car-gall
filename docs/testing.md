# Test Stratejisi

Temel ilke: **her kabul kriteri en az bir testle karşılanır** ve bu eşleme izlenebilirdir.
Bir kriterin testi yoksa o kriter karşılanmamıştır — kod çalışıyor görünse bile.

---

## 1. Backend — xUnit

### Test projeleri
```
tests/
  <Modül>.Tests/             → modül başına bir proje
  Architecture.Tests/        → sınır ihlallerini kıran mimari testler
```

Bir test projesi **yalnızca kendi modülüne** ve o modülün `Contracts`'ına referans verir.
Başka modülün iç tipine test üzerinden erişmek de yasaktır (`docs/architecture.md` § 5/6).

### "Her kriter bir test"

Spec'teki her kabul kriterinin (`AC-n`) karşılığı olan en az bir test bulunur ve test
kriteri **adıyla** belirtir:

```csharp
[Fact(DisplayName = "AC-3: Görseli olmayan araç yayına alınamaz")]
public void Yayinlanamaz_GorselYoksa() { … }
```

- `DisplayName` **`AC-<n>: <kriterin metni>`** biçimindedir. Metin spec'ten **kopyalanır**,
  yeniden yazılmaz — böylece spec değişince test çıktısı yalan söylemez.
- Bir kriter birden çok test gerektirebilir; hepsi aynı `AC-n` önekini taşır.
- Bir test birden çok kriteri karşılamaz. Karşılıyorsa kriterler yanlış yazılmıştır.

### Adlandırma deseni

```
<Sınıf>Tests.<Davranış>_<Koşul>
```

Metot adı **Türkçe**, sonuç önce koşul sonra:

```csharp
Yayinlanamaz_GorselYoksa
Yayinlanamaz_ZorunluAlanBossa
Yayinlanir_TumKosullarSaglandiginda
FiyatGecmisineYazilir_FiyatDegistiginde
TalepKapanmaz_RezervasyonIptalEdildiginde
AcikDenir_SubeSaatDiliminde_MesaiIcindeyse
```

- `Should`, `Test`, `Verify` gibi gürültü kelimeleri kullanılmaz.
- Metot adı **ne olduğunu** söyler; `DisplayName` **hangi kriteri** karşıladığını söyler.
- Arrange / Act / Assert blokları boş satırla ayrılır; yorumla etiketlenmez.

### Test türleri ve kapsam

| Tür | Nerede | Ne test eder |
|---|---|---|
| **Domain birim testi** | `<Modül>.Tests/Domain/` | Invariant'lar, durum geçişleri, değer tipleri. Veritabanı yok, mock yok. |
| **Use-case testi** | `<Modül>.Tests/Application/` | Handler davranışı, doğrulama, `Result` dönüşleri. Bağımlılıklar sahte. |
| **Entegrasyon testi** | `<Modül>.Tests/Integration/` | EF yapılandırması, migration, gerçek sorgular. **Testcontainers ile gerçek PostgreSQL.** |
| **Endpoint testi** | `<Modül>.Tests/Endpoints/` | Durum kodu, hata sözleşmesi (`errorCode`), yetki. |
| **Mimari test** | `Architecture.Tests/` | Yasak listesi (§ 3). |
| **Sözleşme testi** | `<Modül>.Tests/Contracts/` | Entegrasyon olayının şeklinin kırılmaması. |

**SQLite in-memory ile EF testi yapılmaz.** Şema davranışı, collation, `jsonb`, FTS ve
`decimal` hassasiyeti farklıdır — yeşil test yanlış güven verir.

### Öncelik

Her şey test edilmez; **kırılması pahalı olan** test edilir:
1. Domain invariant'ları ve durum geçişleri — **her zaman**.
2. Para, yüzde, yuvarlama hesapları — **her zaman**.
3. Çok dil / normalizasyon / collation davranışı — **her zaman** (sessizce bozulur).
4. Saat dilimi ve çalışma saati hesapları — **her zaman**.
5. Yetki kontrolleri — **her zaman**.
6. Basit CRUD eşlemesi — endpoint testi yeterli, birim testi gereksiz.

---

## 2. Mimari Testler

`Architecture.Tests` ilk gün yazılır, son gün değil. En az şunları kırar:

- Bir modül projesi başka modülün `X` projesine referans veremez (yalnızca `X.Contracts`).
- `X.Contracts` dışında `public` tip bulunamaz.
- `Domain` katmanı EF Core, HTTP veya `IConfiguration` tiplerine referans veremez.
- `BuildingBlocks` hiçbir modüle referans veremez.
- Entegrasyon olayları `record` olmalı ve yalnızca primitive/DTO taşımalı.
- `double` veya `float` ile tanımlanmış para/yüzde alanı bulunamaz.
- `DateTime.Now` / `DateTime.UtcNow` doğrudan kullanılamaz (`IClock` zorunlu).
- `catch (Exception)` boş gövdeyle bulunamaz.
- CSS'te fiziksel yön özellikleri (frontend lint tarafında — § 4).

Ayrıca CI'da: **migration şema kontrolü** — modül X'in migration'ı yalnızca `x` şemasına
dokunur.

---

## 3. Arayüz Testleri

Detaylı konvansiyonlar: **`docs/frontend.md`**. Burada yalnızca kanıt ve kapı kuralları.

### Görsel kanıt — her mini-spec kriteri için

Arayüz işlerinde her kabul kriteri için **ekran görüntüsü** üretilir ve PR'a eklenir.

- Konum: `docs/evidence/<spec-no>-ac<n>.png`
  (birden çok görsel gerekiyorsa `-a`, `-b` soneki).
- Görüntü **kriterin karşılandığını gösterir**, ekranın genel görünümünü değil.
- Bir ekranın **üç durumu** varsa (yükleniyor / boş / hata) ve kriter bunları kapsıyorsa,
  **üçünün de** görüntüsü gerekir — "boş durum" ve "hata durumu" en sık atlanan ve en sık
  bozuk olanlardır.
- **RTL kriteri olan her ekran için Arapça görüntü zorunludur.** LTR görüntü RTL kriterini
  kanıtlamaz.
- Kanıt PR açıklamasındaki kriter → kanıt tablosunda referanslanır (`docs/git.md` § 5).

### Smoke test — kritik akışlar

Uçtan uca otomatik test yalnızca **kritik akışlar** için yazılır. V1 kritik akış listesi:

1. Ziyaretçi vitrinde araç listeler → filtreler → detay sayfasını açar.
2. Ziyaretçi bir araç için talep bırakır → talep panelde görünür.
3. Personel giriş yapar → araç ekler → yayına alır → vitrinde görünür.
4. Dil değiştirme: `/tr` → `/ar` geçişi, sayfa RTL'e döner, içerik Arapça gelir.

Bu dört akış kırıldığında ürün kullanılamaz hâle gelir; gerisi görsel kanıtla yürür.
Smoke test sayısını artırma önerisi öneri kuralına tabidir.

### CI kapıları — frontend

Pipeline'da frontend için **lint + build** zorunludur:
- tip kontrolü (`tsc --noEmit`) — hatasız,
- lint — hatasız (fiziksel CSS yön özellikleri ve hardcoded metin kuralları dâhil),
- production build — başarılı,
- **eksik çeviri anahtarı kontrolü** — `tr`/`ar`/`en` katalogları anahtar bazında eşit.

Birim testi frontend tarafında zorunlu değildir; saf mantık (biçimlendirme, hesaplama,
durum makinesi) içeren yardımcılar için yazılır.

---

## 4. Test Verisi

- Test verisi **her testte kendi içinde** kurulur. Paylaşılan seed'e bağımlı test yazılmaz.
- Object mother / builder deseni kullanılır; nesne kurulumu testin gövdesini boğmaz.
- Rastgele veri (`Faker`) yalnızca alakasız alanlar için. Test edilen alan **açıkça yazılır**.
- Tarih ve saat testlerinde `IClock` sabitlenir; gerçek saate bağlı test yazılmaz.
- Para testlerinde **KWD (3 ondalık)** en az bir kez kullanılır — `decimal(_,2)` hatası
  yalnızca orada görünür.

---

## 5. Kırmızı Test

- Kırmızı test **atlanmaz** (`Skip` ile susturulmaz) ve geçici olarak silinmez.
- `Skip` kullanımı gerekçesini ve bir spec numarasını taşır; gerekçesiz `Skip` PR'ı reddeder.
- Kırmızı pipeline ile merge yapılmaz (`docs/git.md` § 4).
- Bir test kırıldıysa: önce **testin haklı olup olmadığı** sorulur. Test haklıysa kod
  düzeltilir; kriter değiştiyse **önce spec güncellenir**, sonra test, sonra kod.
  Test sessizce kodun yeni davranışına uydurulmaz.

---

## 6. Raporlama

Bir iş "bitti" denmeden önce PR açıklamasında şunlar bulunur:
- kriter → test eşleme tablosu,
- arayüz kriterleri için görsel kanıt bağlantıları,
- karşılanmayan kriterler ve nedenleri (varsa; "yok" yazılır, boş bırakılmaz),
- çalıştırılan test sayısı ve sonucu.

Test kırmızıysa **kırmızı denir, çıktısıyla birlikte.** Atlanan adım atlandı denir.
