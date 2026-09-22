# ADR 0002 — Migration Uygulama Stratejisi

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 2026-09-22 |
| **Karar veren** | Takım Yöneticisi |
| **İlgili** | `docs/architecture.md` § 1, § 5; ADR 0001 |

---

## Bağlam

Bu soru üç spec'te üst üste açık kaldı ve her seferinde "sonraki spec'e" devredildi:
`0003` `Dealership` şemasını, `0005` `Leads` şemasını, `0006` `Identity` şemasını üretti
ve hiçbiri **üretimde migration'ın kim tarafından uygulanacağını** söylemedi.

Sonuç, karar verilmediği için oluşan bir durum oldu ve üç farklı davranış birikti:

| Modül | Bugünkü davranış |
|---|---|
| `VehicleCatalog`, `VehicleReference` | Yalnızca `Development:SeedShowcase` bayrağı açıkken |
| `Dealership` | `MigrateDealershipAsync` **vardı ama hiçbir yerden çağrılmıyordu** |
| `Identity` (0006) | Uygulama başlangıcında, koşulsuz |

**Bunun bugünkü somut sonucu şudur:** geliştirme bayrağı kapalı bir dağıtımda hiçbir şema
oluşmaz. Uygulama ayağa kalkar, sağlıklı görünür ve **ilk ziyaretçinin ilk isteğinde**
"tablo yok" hatasıyla patlar. Yani sistem bugün üretime çıkarılamaz ve bunu söyleyen tek
bir test yoktur.

İkinci bağlam: `docs/architecture.md` § 5 sınırın **veritabanı seviyesinde de**
dayatılmasını istiyor ve bunu "şema başına ayrı PostgreSQL rolü" olarak yazıyor.

---

## Karar

**Migration'ı çalışan uygulama uygulamaz. Uygulayan, açıkça çalıştırılan tek seferlik bir
kurulum adımıdır; çalışan uygulama yalnızca şemanın güncel olduğunu DOĞRULAR ve güncel
değilse başlamayı REDDEDER.**

Üç parçası var:

1. **Kurulum adımı** — Host'un `--migrate` argümanıyla çalıştırılması. Aynı DI kabını
   kurar, kayıtlı her modülün şemasını uygular, çıkar. Dağıtım bunu uygulamayı
   başlatmadan önce çalıştırır.
2. **Doğrulama** — normal başlangıçta her modüle "bekleyen migration'ın var mı?" diye
   sorulur. Varsa uygulama, eksik modülleri ve çalıştırılacak komutu söyleyen bir
   mesajla durur.
3. **Kayıt** — modüller Host'ta elle sayılmaz. Her modül kendi `AddX`'inde bir
   `IModuleSchema` kaydeder; Host onları enjeksiyonla bulur.

Üçüncü madde kararın en önemli parçasıdır: Host'ta elle tutulan bir liste olsaydı, yeni
bir modül eklendiğinde o listeye eklenmeyi unutur ve şema sessizce oluşmazdı — sorun yine
ilk sorguda, üretimde görünürdü. Tam olarak bugün olan şey budur.

---

## Sonuçlar

### İyi taraf

- **Uygulama rolünün kalıcı DDL yetkisine ihtiyacı kalmaz.** § 5'in "şema başına ayrı
  rol" kuralı süs olmaktan çıkar: DDL yalnızca kurulum adımının rolüne verilir.
- **Bozuk bir migration dağıtım adımında patlar**, uygulama başlangıcında değil. Hata,
  hiçbir şey devreye alınmadan önce görünür.
- **Birden çok örnek yarışmaz.** Bugün tek örnek var; karar, çoğalma günü yeniden
  açılmak zorunda kalmaz.
- **Atlanan adım sessiz kalmaz.** Doğrulama, "tablo yok" hatasını "kurulum adımı
  atlandı" mesajına çevirir ve ne yapılacağını söyler.
- **Yeni modül kendiliğinden katılır**; unutulabilecek bir adım yoktur.

### Bedel

- **Dağıtım bir adım kazandı.** Atlanırsa uygulama başlamaz — yani bedel, sessiz bir
  bozulma değil gürültülü bir duruştur. Bu bilinçli bir takastır.
- **Başlangıçta modül başına bir veritabanı sorgusu** yapılır. Tek seferlik ve ucuz.
- **Geliştirme akışı iki komuta çıktı** (`--migrate`, sonra çalıştır). `scripts/dev-up.sh`
  ikisini de yapıyor; elle koşturan biri için ek bir satır.

### Ne zaman yeniden değerlendirilir

- **Sıfır kesintili dağıtıma geçilirse:** eski ve yeni sürüm bir süre aynı şemayla
  çalışacağı için migration'ların geriye dönük uyumlu olması gerekir. Bu, kurulum adımının
  *yerini* değiştirmez ama migration yazma kurallarına yeni bir madde ekler.
- **Birden çok kurulum tek komutla yönetilmeye başlanırsa:** kurulum adımı kurulum başına
  çalıştırılmalıdır; o gün bir orkestrasyon aracı gerekir.

---

## Değerlendirilen Alternatifler

### 1. Uygulama başlangıcında migrate etmek

0006'nın `Identity` için yaptığı buydu ve en az iş isteyen seçenekti.

**Elendi:** uygulama rolünün her şemada kalıcı DDL yetkisi olmasını gerektirir ve § 5'in
üzerine emek harcanmış sınırını süse çevirir. Ayrıca bozuk bir migration, dağıtım adımının
gürültülü hatası yerine "uygulama hiç açılmıyor"a dönüşür; ikisi aynı şey değildir, çünkü
ilkinde eski sürüm ayakta kalır.

### 2. Ayrı bir `Migrator` projesi

Temiz görünür ama modül kayıtlarının **ikinci bir kopyasını** ister. İki liste er ya da
geç ayrışır ve ayrıştığı gün bir modülün şeması sessizce uygulanmaz. Host zaten her
modülü tanıyor; ikinci bir yer açmak, çözdüğünden fazla sorun üretir.

### 3. Elle çalıştırılan SQL betikleri

EF'in migration zincirinden çıkmak demektir: `docs/architecture.md` § 5'in "modül X'in
migration'ı yalnızca `x` şemasına dokunabilir" CI kontrolü de dâhil, bugünkü bütün
otomatik korumalar kaybolur.

### 4. Hiçbir şey yapmamak (bugünkü durum)

Değerlendirildi ve **reddedildi**: bugünkü durum bir karar değil, kararsızlığın
birikmesidir. Sistem üretime çıkarılamaz ve bunu söyleyen bir test yoktur.
