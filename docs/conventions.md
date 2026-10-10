# Konvansiyonlar

Bu dosya **nasıl yazılacağını** tanımlar. **Ne yazılacağı** `specs/`, **nereye yazılacağı**
`docs/architecture.md`, **arayüz tarafı** `docs/frontend.md`.

---

## 1. Çözüm ve Klasör Yapısı

```
src/
  Host/                         → Minimal API host. Modül kaydı, auth, hata haritalama, BFF kompozisyonu.
  BuildingBlocks/               → Primitive'ler. İş kuralı YOK.
  Modules/
    <Modül>/
      <Modül>/                  → Domain + Application + Infrastructure + Endpoints. TAMAMI internal.
      <Modül>.Contracts/        → Tek public yüzey: DTO, query/command arayüzü, entegrasyon olayı.
tests/
  <Modül>.Tests/                → Birim + entegrasyon testleri.
  Architecture.Tests/           → Sınır ihlallerini kıran mimari testler.
frontend/                       → React + Vite + TypeScript. → docs/frontend.md
docs/    specs/
```

Modül içi klasörler (proje değil, klasör):
```
<Modül>/
  Domain/            → Agregatlar, değer tipleri, domain event'leri, invariantlar.
  Application/       → Use-case handler'ları, doğrulama, contract arayüz implementasyonları.
  Infrastructure/    → DbContext, EF yapılandırması, migration'lar, dış sistem adaptörleri.
  Endpoints/         → Minimal API endpoint grupları.
  <Modül>Module.cs   → DI kaydı ve endpoint kaydı için tek giriş noktası.
```

---

## 2. Adlandırma

### C#
| Öğe | Kural | Örnek |
|---|---|---|
| Proje / namespace | `CarGallery.Modules.<Modül>` | `CarGallery.Modules.VehicleCatalog` |
| Contracts namespace | `CarGallery.Modules.<Modül>.Contracts` | — |
| Agregat / entity | PascalCase, tekil | `Vehicle`, `Branch`, `Lead` |
| Değer tipi | PascalCase, tekil | `Money`, `VehicleCondition` |
| Arayüz | `I` öneki | `IVehicleQueries` |
| Entegrasyon olayı | **Geçmiş zaman**, `record` | `VehiclePublished`, `LeadCaptured` |
| Komut | Emir kipi | `PublishVehicle`, `AssignLead` |
| Sorgu | `Get…` / `Find…` / `Search…` | `GetVehicleSummary` |
| Async metot | `Async` soneki | `PublishAsync` |
| Test sınıfı | `<Test edilen>Tests` | `VehiclePublishingTests` |

- Domain terimleri **`docs/domain.md`** sözlüğünden alınır. Sözlükte olmayan yeni bir terim
  koda girmeden önce sözlüğe girer.
- Kısaltma kullanılmaz (`veh`, `br`, `cfg` yok). İstisna: yaygın ve tek anlamlı olanlar
  (`Id`, `Url`, `Vin`).
- Türkçe/İngilizce karışımı yok: **kod İngilizce**, domain terimleri sözlükteki İngilizce
  karşılığıyla. Kullanıcıya görünen metin koda hiç girmez (§ 6).

### Veritabanı
| Öğe | Kural | Örnek |
|---|---|---|
| Şema | Modül adının snake_case hâli | `vehicle_catalog`, `dealership` |
| Tablo | snake_case, **çoğul** | `vehicles`, `vehicle_translations` |
| Kolon | snake_case | `branch_id`, `published_at` |
| Çeviri tablosu | `<tekil>_translations` | `page_translations` |
| Outbox | `outbox_messages` (her şemada kendi) | — |
| Migration | `<UTC timestamp>_<PascalCase açıklama>` | `20260115_AddBranchToVehicle` |

Bir modülün migration'ı **yalnızca kendi şemasına** dokunur; CI bunu kontrol eder.

### API
- Rota kökü modül başına: `/api/catalog`, `/api/leads`, `/api/dealership`, …
- Public site rotaları locale önekli: `/{locale}/…`. API rotaları locale öneki **taşımaz**;
  dil `Accept-Language` başlığı veya açık `locale` parametresiyle gelir.
- Kaynak adları çoğul ve kebab-case: `/api/catalog/vehicles`, `/api/catalog/field-settings`.
- Fiil rotada değil, HTTP metodunda. İstisna: gerçek durum geçişleri
  → `POST /api/catalog/vehicles/{id}/publish`.

---

## 3. Endpoint'ler — Minimal API

- Controller **kullanılmaz**. Tüm endpoint'ler Minimal API.
- Her modül kendi endpoint'lerini kendi içinde tanımlar; host yalnızca `MapXModule()` çağırır.
- Endpoint gövdesi **ince**: bağlama (binding), handler çağrısı, sonuç eşleme. İş mantığı yok.
- Her endpoint `TypedResults` döner ve `.Produces<T>()` ile şemasını bildirir.
- Sayfalama tek tip: `?page=&pageSize=` + `PagedResult<T>` (`items`, `page`, `pageSize`,
  `totalCount`). `pageSize` üst sınırı sabittir; aşan istek üst sınıra kırpılır, hata dönmez.
- Yazma endpoint'leri idempotency gerektiriyorsa `Idempotency-Key` başlığı kabul eder.

---

## 4. Katmanlar ve Bağımlılık Yönü

```
Endpoints → Application → Domain
Infrastructure → Application, Domain
Domain → (hiçbir şey; yalnızca BuildingBlocks primitive'leri)
```

- `Domain` EF Core'u, HTTP'yi, `IConfiguration`'ı bilmez.
- `Application` dış dünyayı arayüzle bilir, implementasyonla değil.
- Başka modülün `Contracts`'ı yalnızca `Application` katmanından tüketilir; `Domain`
  başka modülü hiç bilmez.

---

## 5. Hata Yönetimi

**Kural: istisna akış kontrolü için kullanılmaz.**

- Beklenen başarısızlıklar (doğrulama hatası, bulunamadı, geçersiz durum geçişi)
  → `Result<T>` / `Result` ile döner.
- Gerçekten beklenmeyen durumlar (veritabanı erişilemiyor, bozuk yapılandırma)
  → istisna. Yakalanmaz, global handler'a gider.
- `catch (Exception) { }` yasaktır. Yutulan istisna yoktur.

**Hata sözleşmesi:**
- Tüm hata yanıtları `ProblemDetails` (RFC 7807).
- Gövde **stabil bir `errorCode`** taşır (`vehicle.not_found`, `vehicle.cannot_publish.no_media`)
  ve gerekirse yapısal parametreler (`{ "field": "price" }`).
- **Çevrilmiş metin dönülmez.** Uç, `errorCode`'u kendi katalogundan çevirir.
  Bilinmeyen kod için uç genel bir mesaj gösterir.
- `500` yanıtı asla iç detay taşımaz: stack trace, SQL, dosya yolu, tip adı yok.
  Yalnızca bir `traceId` döner; detay loga yazılır.
- Doğrulama hataları `422`, bulunamadı `404`, yetki `403`, kimlik `401`,
  durum çakışması `409`, **hız sınırı `429`**.
- **Dosya yüklemede:** desteklenmeyen dosya türü `415`, boyut sınırı aşıldı `413`.
  İkisi `422`'ye toplanmaz — istemcinin yapacağı şey farklı: biri **başka bir dosya**
  seçmek, diğeri **aynı dosyayı küçültmek**. Tür, dosya adından veya istemcinin
  `Content-Type` başlığından değil **içerikten** belirlenir; adına güvenen bir kontrol
  `.jpg` diye adlandırılmış herhangi bir dosyayı kabul eder.
  *(Spec `0011` / D4, Takım Yöneticisi onayı 2026-10-10.)*

**`errorCode` adlandırması:** `<alan>.<durum>[.<ayrıntı>]`, hepsi snake_case, nokta ayraçlı.
Kod bir kez yayınlandıktan sonra **anlamı değişmez**; gerekirse yeni kod eklenir.

---

## 6. Metin, Dil, Biçimlendirme

- **Kullanıcıya görünen hiçbir metin koda gömülmez.** Sunucu tarafında bu şu anlama gelir:
  API metin değil **kod** döner (§ 5). Sunucunun ürettiği metin (e-posta, PDF) alıcının
  locale'iyle, `Notifications` şablonlarından üretilir.
- Çeviriler içeriğin sahibi modülde durur; merkezi çeviri tablosu yasaktır
  (`docs/architecture.md` § 3).
- Dize birleştirmeyle cümle kurulmaz. ICU mesaj formatı kullanılır (Arapça'da altı çoğul
  biçimi vardır).
- Metin karşılaştırma ve arama **ham kolon üzerinde yapılmaz**: yazma anında üretilen
  normalize arama kolonu + locale FTS konfigürasyonu kullanılır. `ILIKE`/`lower()` ile
  Türkçe/Arapça arama yasaktır.

---

## 7. Para ve Sayılar

- **Para ve yüzde her zaman `decimal`.** `double`/`float` yasaktır — istisnasız.
- Para `Money` ile taşınır: tutar + ISO 4217 kodu + minor unit üssü.
  Çıplak `decimal` bir parayı temsil edemez.
- Saklama `decimal(19,4)`. `decimal(_,2)` yasaktır: KWD/BHD/JOD/OMR üç ondalıklıdır.
- Para birimi kurulum yapılandırmasından gelir, veritabanından değil, panelden değiştirilemez.
- Yuvarlama açıkça belirtilir (`MidpointRounding`); varsayılana güvenilmez.
- Biçimlendirme **uçta** yapılır. Sunucu ham sayı + para birimi kodu döner.

---

## 8. Tarih ve Saat

- Saklama **her zaman UTC** (`timestamptz`).
- `DateTime.Now`, `DateTime.UtcNow`, `DateTimeOffset.Now` doğrudan kullanılmaz;
  `IClock` soyutlaması enjekte edilir (test edilebilirlik).
- Gösterim uçta, ilgili şubenin saat diliminde.
- "Şu an açık mı?" hesabı her zaman şubenin saat dilimiyle (`docs/domain.md` B4).

---

## 9. Kimlikler

- Agregat kimlikleri modül **içinde** güçlü tiplidir (`VehicleId`, `BranchId`).
- `X.Contracts` sınırında **çıplak `Guid`** olarak geçer — güçlü tipli id sızdırılmaz.
- Modüller arası referanslar **soft reference**'tır: foreign key kısıtı yoktur.
- Üretim: `Guid` v7 (sıralanabilir), veritabanı değil uygulama tarafında üretilir.

---

## 10. EF Core

- Her modülün kendi `DbContext`'i, kendi şeması, kendi migration zinciri.
- Yapılandırma `IEntityTypeConfiguration<T>` sınıflarıyla; data annotation kullanılmaz.
- Okuma sorgularında `AsNoTracking()` varsayılan.
- Lazy loading **kapalı**. `Include` açıkça yazılır.
- Ham SQL yalnızca **performans** veya **LINQ ile ifade edilemeyen doğruluk** gerekçesiyle
  (satır kilidi gibi) ve **kendi şeması üzerinde**; gerekçe kod yorumunda yazılır.
- `SaveChanges` çağrısı istek başına bir kez, bir modülde (`docs/architecture.md` § 2.D).
- Entegrasyon olayı **iş verisiyle aynı transaction'da** outbox'a yazılır.
- Çeviriler owned collection olarak agregatın parçasıdır; ayrı agregat değildir.

---

## 11. Yapılandırma

- Güçlü tipli options sınıfları + başlangıçta doğrulama (`ValidateOnStart`).
  Eksik yapılandırmayla uygulama **ayağa kalkmaz**, çalışma anında patlamaz.
- Sır (connection string, API anahtarı) repoda durmaz. Geliştirmede user-secrets,
  üretimde ortam değişkeni.
- Kurulum bazlı değerler (para birimi, varsayılan locale, etkin locale listesi, ülke kodu)
  **dağıtım yapılandırmasıdır** — veritabanı kaydı değildir.

---

## 12. Loglama

- Yapılandırılmış log (structured logging). Dize interpolasyonu ile log yazılmaz.
- Her istek `traceId` taşır; hata yanıtındaki `traceId` ile log eşleşir.
- **Loga kişisel veri yazılmaz**: telefon, e-posta, ad, şase/plaka. Kimlik yerine `Id`.
- Log seviyeleri: `Information` iş olayları, `Warning` beklenen ama dikkat gerektiren,
  `Error` beklenmeyen. `Debug` üretimde kapalı.

---

## 13. Kod Hijyeni

- `nullable` etkin, uyarılar hata. `TreatWarningsAsErrors` açık.
- `var` yalnızca tip sağ tarafta açıkça görünüyorsa.
- Yorum **neden**i anlatır, **ne**yi değil. Kodu tekrar eden yorum silinir.
- `TODO` yazılmaz. Yapılacak iş varsa spec'e girer (`AGENTS.md` Altın Kural 1).
- Ölü kod, kullanılmayan using, yoruma alınmış blok bırakılmaz. Geçmiş git'tedir.
- Bir dosya bir kavram. 400 satırı aşan dosya gözden geçirilir.
