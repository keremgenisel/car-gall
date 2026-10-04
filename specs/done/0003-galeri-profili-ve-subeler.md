# 0003 — Galeri Profili ve Şubeler

| | |
|---|---|
| **Durum** | **Tamamlandı** |
| **Sahip** | Developer oturumu (ajan üye) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (yeni — bugün yalnızca `Dealership.Contracts` var) · `BuildingBlocks` (olay dağıtımı) |
| **Bağımlı spec'ler** | yok. `0001` ve `0002` bu spec'in **tüketicisidir**, önkoşulu değil. |

---

## Intent

Galerinin kendisi ve şubeleri bugün sistemde **hiçbir yerde kayıtlı değil.** Vitrin, araç
kartında bir şube adı gösteriyor ama o adı besleyen bir şube kaydı yok; şube adı değiştiğinde
veya bir şube kapatıldığında sistemin haberi olmuyor.

Bunun bedeli şu anda ödenmiş durumda ve görünmüyor: `VehicleCatalog` şube olaylarını
dinleyen kodu **yazdı ve test etti**, ama o olayları **yayan kimse yok.** Dolayısıyla
`docs/domain.md` **A8**'in güvenlik ağı — şube kapatıldığında o şubedeki araçların vitrinden
düşmesi — bugün hiç çalışmıyor. Kapatılan bir şubenin araçları vitrinde kalmaya devam eder
ve ziyaretçi, gidemeyeceği bir lokasyondaki aracı sorar.

**Galeri yöneticisi** şubelerinin sistemde doğru ve tek bir yerde durmasını istiyor;
**ekip** ise araç, talep ve vitrin taraflarının "şube" dediklerinde aynı şeyi kastettiği bir
gerçek kaynağı istiyor.

Bu iş bittiğinde galeri profili ve şubeler `Dealership` modülünde yaşar, şube bilgisi
değiştiğinde veya bir şube kapatıldığında sistem bunu **olayla duyurur**, ve `VehicleCatalog`
bugün boşa çalışan dinleyicileri ilk kez gerçek veri alır.

---

## Requirements

- **R1.** Sistem, kurulumun ait olduğu **galerinin profilini** tutar: ticari unvan, logo,
  vergi/sicil bilgisi ve sosyal hesapları. Kurulumda tam olarak **bir** galeri profili vardır.
- **R2.** Sistem, galerinin **şubelerini** tutar. Her şube şunları taşır: görünen ad
  *(çevrilebilir)*, serbest metin adres *(çevrilebilir)*, koordinat, telefon, WhatsApp
  numarası, e-posta, saat dilimi, durum ve URL slug'ı.
  **Zorunlu olanlar:** varsayılan locale'deki görünen ad ve adres, telefon, saat dilimi,
  durum, slug. **Opsiyonel olanlar:** koordinat, WhatsApp numarası, e-posta ve varsayılan
  dışındaki locale'lerin ad/adres karşılıkları.
  **Saat dilimi** bir **IANA bölge kimliğidir** (`Europe/Istanbul`, `Asia/Kuwait`); sabit
  saat farkı (`+03:00`) kabul edilmez. *(Gerekçe: yaz saati uygulaması ve kural değişiklikleri
  yalnızca bölge kimliğinde doğru izlenir; sabit fark B4'ü sessizce yanlış hesaplatır.)*
- **R3.** Şube durumu üç değerden biridir: **aktif**, **geçici kapalı**, **kapatılmış**.
  `Kapatılmış` **terminaldir**: kapatılmış bir şube yeniden aktif edilemez; aynı lokasyon
  yeniden açılacaksa yeni bir şube kaydı açılır. → Karar **D3**.
- **R4.** Bir galerinin **en az bir aktif şubesi** vardır; son aktif şube kapatılamaz.
  → `docs/domain.md` **B5**.
- **R5.** Şube adı ve adresi **locale başına** tutulur. Ziyaretçinin locale'inde karşılığı
  olmayan bir ad için varsayılan locale'deki karşılık gösterilir. Varsayılan locale'deki
  karşılık **her zaman vardır** — R2 onu zorunlu kılar — dolayısıyla okuma yolu hiçbir
  koşulda boş ad döndürmez.
- **R6.** Şube slug'ı galeri genelinde **tekildir** ve dilden bağımsız, küçük harfli ASCII
  slug biçimindedir (`kadikoy-subesi`). *(Gerekçe: slug bir adrestir; Türkçe `i/İ/ı/I` ve
  Arapça harfler adres tarafında sessiz eşleşme hatası üretir — Yasak Listesi 27.)*
- **R7.** Bir şube ilk kez açıldığında sistem bunu **duyurur**, duyuru şubenin kimliğini ve
  locale başına adlarını taşır.
- **R8.** Bir şubenin **görünen adı** değiştiğinde sistem bunu **duyurur**; duyuru şubenin
  kimliğini ve locale başına **güncel** adlarını taşır. Görünen ad dışındaki alanların
  (adres, telefon, koordinat, saat dilimi, slug) değişmesi bu duyuruyu üretmez.
  *(Gerekçe: duyurunun bugünkü tek tüketicisi vitrin okuma modelidir ve orada yalnızca şube
  **adı** kopyalanır. Kullanılmayan alanı duyuruya koymak, tüketicisi olmayan bir sözleşme
  yüzeyi yaratır. Şube sayfası geldiğinde — X3 — duyuru genişletilir.)*
- **R9.** Bir şube **kapatıldığında** sistem bunu duyurur; bu duyuru **A8**'in tetikleyicisidir.
- **R10.** Duyurular **en az bir kez** ulaşır ve alıcı tarafında **aynı duyurunun iki kez
  işlenmesi ek bir etki üretmez**. → `docs/architecture.md` § 7 (Olay dağıtımı).
- **R11.** Bir duyuru, onu doğuran değişiklikle **birlikte** kalıcı olur: değişiklik
  kaydedildiyse duyuru da kaydedilmiştir, değişiklik geri alındıysa duyuru da alınmamıştır.
- **R12.** Sistem, şube bilgisini okumak isteyen diğer modüllere bunu `Dealership.Contracts`
  üzerinden sunar.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Dealership`, galeri ve şube verisinin **tek yazma sahibidir.** Başka hiçbir modül
  bu veriyi yazmaz. → `docs/architecture.md` § 1.
- **C2.** Modül sınırını yalnızca `Dealership.Contracts` geçer; entegrasyon olayları record'dur,
  geçmiş zaman adı taşır ve yalnızca primitive + kendi DTO'larını içerir.
  → `docs/architecture.md` § 2, Yasak Listesi 6–10.
- **C3.** `Dealership` başka modüle **senkron çağrı yapmaz.** → Yasak Listesi 32.
- **C4.** Şube adı/adres/telefonu başka modülde **gerçek kaynak gibi** saklanmaz. Vitrin okuma
  modelindeki denormalizasyon serbesttir — yalnızca bu modülün olaylarından beslenerek ve o
  kopya düzenlenmeden. → Yasak Listesi 33.
- **C5.** Olay dağıtımı için **tek mekanizma outbox'tır**: en-az-bir-kez, idempotent.
  Başka bir yayın yolu eklenmez. → `docs/architecture.md` § 7.
- **C6.** Bir modül başka modülün outbox/inbox tablosuna yazmaz. → Yasak Listesi 16.
- **C7.** Yayıncı abone listesini bilmez; `if (module == "X")` tarzı dallanma yoktur.
  → Yasak Listesi 18.
- **C8.** Koordinat `decimal` kolonlarda durur; PostGIS kullanılmaz.
  → `docs/architecture.md` § 1, § 7.
- **C9.** Saat dilimi **şube bazındadır**, şubede **IANA bölge kimliği** olarak saklanır
  (R2) ve sabit saat farkı olarak saklanmaz. Sunucu yerel saati veya `DateTime.Now`
  kullanılmaz; UTC + saat soyutlaması. → Yasak Listesi 30, 35.
- **C10.** Kodda hafta sonu günü, çalışma günü sayısı veya tek açılış/kapanış saati
  **varsayılmaz**. → Yasak Listesi 34. *(Çalışma saatlerinin kendisi X2'de kapsam dışıdır;
  bu kısıt, saat dilimi alanının böyle bir varsayım getirmemesi için buradadır.)*
- **C11.** `TenantId` benzeri kiracı alanı eklenmez. Kurulum = galeri.
  → Yasak Listesi 36, `AGENTS.md` § Proje.
- **C12.** Çeviriler merkezi bir `translations` tablosunda toplanmaz; şube adı ve adresi bu
  modülün kendi verisidir. → Yasak Listesi 21.
- **C13.** Veritabanında çevrilmiş durum adı saklanmaz; durum kodu saklanır, uçta çevrilir.
  → Yasak Listesi 22.
- **C14.** **Her migration yalnız kendi modülünün şemasına dokunur**; ortak `DbContext` veya
  ortak migration zinciri kullanılmaz. → Yasak Listesi 3, `docs/architecture.md` § 5.
  *(Revizyon — QA/plan bulgusu R-1: madde önce "yalnızca `Dealership` şemasına" diyordu ve
  **D4 ile çelişiyordu.** D4 yinelenen işleme korumasını alıcı tarafında istiyor; alıcının
  inbox tablosu alıcının kendi şemasındadır ve Yasak 16 başka modülün inbox'ına yazmayı
  yasaklar. Kural, `architecture.md` § 5'teki gerçek hâline çevrildi.)*
- **C15.** Her kabul kriteri en az bir testle karşılanır; test `DisplayName`'i
  `AC-<n>: <kriterin metni>` biçimindedir ve metin bu spec'ten **kopyalanır**.
  → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 1.
- **C16.** **Kriterlerin gözlem noktası modül düzeyidir, HTTP değildir.** D1 uyarınca bu
  spec'te yazma ucu yoktur; "kaydedilir / kaydedilemez / reddedilir" diyen her kriter
  `Dealership`'in domain ve uygulama katmanında doğrulanır (`docs/testing.md` § 1 —
  domain birim testi, use-case testi, entegrasyon testi). Endpoint testi bu spec'te
  **uygulanabilir değildir**.
- **C17.** İki modülü birlikte kapsayan tek kriter **AC-32**'dir (uçtan uca A8 zinciri).
  Onun testi modül test projelerinde **yaşayamaz** — `docs/testing.md` § 1 bir test
  projesinin yalnızca kendi modülüne ve o modülün `Contracts`'ına referans vermesini
  zorunlu kılar. Bu test `tests/Host.Tests` altında yaşar.

### KAPSAM DIŞI

- **X1.** **Yönetim paneli ekranları ve yazma uçları** (şube ekleme/düzenleme/kapatma
  arayüzü) — *neden:* `Identity` modülü yoktur ve Host'ta kimlik doğrulama yoktur
  (doğrulandı, bkz. Context). Korumasız yazma ucu yayınlamak, galeri profilini ve şube
  verisini internete açık biçimde düzenlenebilir bırakır. Başlangıç verisi X1 yerine
  **kurulum migration'ı** ile gelir (bkz. Karar D1). Yazma yüzeyi, `Identity` spec'inden
  sonra ayrı bir spec'tir.
- **X2.** **Çalışma saatleri, istisna programı ve "şu an açık mı" hesabı** (`docs/domain.md`
  **B1–B4**) — *neden:* kapsam kararıyla **ayrı bir spec'e** ayrıldı (numarayı Takım
  Yöneticisi verir). Gün → 0..N aralık modeli, çift mesai,
  hafta sonunun veriden okunması ve tarih aralıklı istisnalar tek başına 10+ kriterdir.
  **Saat dilimi alanı bu spec'te vardır** (R2), çünkü şubenin kimliğine aittir; onu kullanan
  hesap 0004'tedir.
- **X3.** Ziyaretçiye dönük şube listesi / şube detay sayfası ve harita — *neden:* vitrin
  tarafı ayrı spec'tir; bu spec veriyi ve duyuruyu üretir, ekranı değil.
- **X4.** `AutoDealer` / `LocalBusiness` JSON-LD ve şube sayfası SEO'su — *neden:* X3'e
  bağlıdır; ekran olmadan enjekte edilecek sayfa yoktur.
- **X5.** Şube görsellerinin yüklenmesi ve işlenmesi — *neden:* `Media` modülü de bugün
  yalnızca `Contracts`'tan ibarettir. Şube, görsele **referans** taşır; görselin kendisi
  `Media` spec'ine aittir.
- **X6.** "En yakın şube" ve mesafeye göre sıralama — *neden:* PostGIS `docs/architecture.md`
  § 6'da kapsam dışıdır; hesap istemci tarafındadır ve bir ekran işidir (X3).
- **X7.** Personelin şubeye bağlanması — *neden:* bu `Identity`'nin verisidir, `Dealership`'in
  değil (`docs/architecture.md` § 1, "Sahip olmadığı").
- **X8.** Şubeler arası araç transferi ve `VehicleRelocated` — *neden:* **A7** uyarınca bu
  `VehicleCatalog`'un olayıdır.
- **X9.** Para/fiyat — *neden:* bu spec'te para taşıyan hiçbir alan yoktur; `specs/TEMPLATE.md`
  § Acceptance Criteria'nın KWD kriteri bu spec için **uygulanabilir değildir.**

---

## Context

### Mevcut davranış — doğrulandı

- `Dealership` modülünün **uygulama projesi yoktur.** Tüm modül şu iki dosyadan ibaret:
  `Dealership.Contracts.csproj` ve
  [BranchEvents.cs](src/Modules/Dealership/Dealership.Contracts/Events/BranchEvents.cs) —
  içinde `BranchOpened`, `BranchDetailsChanged`, `BranchDeactivated` ve `BranchNameDto`.
- **Dinleyen var, konuşan yok.** `VehicleCatalog` bu olayların iki tüketicisini yazmış:
  [BranchEventHandlers.cs:16](src/Modules/VehicleCatalog/VehicleCatalog/Application/EventHandlers/BranchEventHandlers.cs#L16)
  (`BranchDetailsChangedHandler`) ve `:32` (`BranchDeactivatedHandler`). İkisinin de entegrasyon
  testi var (`tests/VehicleCatalog.Tests/Integration/OlayIsleyiciTests.cs`) ve testler
  handler'ı **doğrudan** çağırıyor — yani dağıtım yolu test edilmiyor, çünkü yok.
  `BranchOpened`'ın ne yayıncısı ne tüketicisi var.
- **Outbox altyapısı yoktur.** `src/BuildingBlocks/Outbox/` içinde yalnızca
  `IIntegrationEvent.cs` (işaret arayüzü) var; outbox tablosu, yazıcı ve dağıtıcı yok.
  Commit `0bc3191` *"kullanılmayan outbox tiplerini çıkar"* bunları bilinçli olarak kaldırmış.
  → Karar D2.
- **`Identity` modülü ve kimlik doğrulama yoktur.** `src/Modules/` altında yalnızca
  `Dealership`, `Media`, `VehicleCatalog`, `VehicleReference` var; `src/Host` içinde
  `Authentication` / `Authorize` eşleşmesi yok. → Karar D1.
- **Şube adı bugün vitrin okuma modelinde yaşıyor.** `ShowcaseListing.BranchNames` locale →
  ad sözlüğü olarak tutuluyor ve `ShowcaseListingQueries.ResolveBranchName` ziyaretçinin
  locale'i yoksa **varsayılan locale'e düşüyor**
  ([ShowcaseListingQueries.cs:71](src/Modules/VehicleCatalog/VehicleCatalog/Application/Showcase/ShowcaseListingQueries.cs#L71)).
  R5 bu davranışı **kaynak tarafında** da aynı kılar.
- **Seed migration örneği mevcut:** `20260921104308_SeedHiddenOptionalField`. D1'in önerdiği
  başlangıç verisi yolu depoda zaten kullanılmış bir desendir, yeni bir icat değildir.

### Geçerli domain kuralları

- **A8** — şube kapatıldığında o şubedeki araçlar yayından kaldırılır ve "lokasyon ataması
  bekliyor" durumuna geçer. Bu spec o zincirin **ilk halkasını** üretir.
- **B5** — bir galerinin en az bir şubesi vardır; son aktif şube kapatılamaz.
- **B4** — "şu an açık mı" şubenin kendi saat diliminde hesaplanır. *(Hesap X2'de; saat
  dilimi alanı burada.)*
- **L3** — talep, ilgilenilen aracın şubesine bağlanır. Bu spec şubeyi var ederek o kuralın
  önkoşulunu sağlar.

### Bilinen tuzaklar

1. **Duyuru ile değişikliğin ayrı düşmesi.** Şube kaydedilip duyuru kaydedilmezse
   `VehicleCatalog` hiç haber almaz ve fark asla kapanmaz; tersi durumda olmayan bir
   değişiklik duyurulur. R11 bunu yasaklar.
2. **Aynı duyurunun iki kez işlenmesi.** En-az-bir-kez teslim, tekrarın **normal** olduğu
   anlamına gelir; alıcı tarafında ikinci işleme ek etki üretmemelidir (R10).
3. **Son şubenin kapatılması.** B5 ihlal edilirse galeri şubesiz kalır ve **A1** ("her aracın
   tam olarak bir şubesi vardır") ihlal edilebilir hâle gelir.
4. **Eksik çeviri.** Bir şube adı yalnızca TR'de girilmişse Arapça vitrinde boş kutu
   gösterilemez; R5 varsayılan locale'e düşmeyi zorunlu kılar.
5. **Telefonun serbest metin olması.** Numara biçimi ülkeye göre değişir; E.164 dışına
   çıkıldığında WhatsApp derin bağlantısı sessizce kırılır.

### Verilen kararlar — D1 ve D2

Bu ikisi spec'in kapsamını doğrudan belirler ve **Takım Yöneticisi tarafından onaylandı.**

**D1 — Başlangıç verisi migration ile gelir; yazma uçları bu spec'te yapılmaz.**
*Gerekçe:* `Identity` ve kimlik doğrulama yok (doğrulandı). Korumasız bir yazma ucu, galeri
profilini ve şube verisini internete açık biçimde düzenlenebilir bırakır; bunu "sonra
kilitleriz" diye ertelemek, bu sistemde en pahalı borç biçimidir. Seed migration deseni
depoda zaten var. *Bilerek ödenen bedel:* galeri panelden şube ekleyemez; her şube
değişikliği bir migration ve bir dağıtım ister. Bu, `Identity` spec'i gelene kadar geçicidir.
*Alternatif:* yazma uçlarını şimdi yapıp yetkilendirmeyi sonraya bırakmak — önermiyorum.

**D2 — Outbox mekanizmasının kurulması bu spec'in kapsamındadır.**
*Gerekçe:* bu spec'in ana çıktısı duyurudur (R7–R11) ve `docs/architecture.md` § 7 tek
mekanizma olarak outbox'ı sabitlemiştir. Outbox'ı ayrı bir spec'e ayırırsak 0003 hiçbir şey
teslim etmez: ne A8 çalışır ne şube adı güncellenir. *Bilerek ödenen bedel:* 0003 iki iş
taşır — modül + ortak altyapı — ve bu yüzden tek dilimde bitmez. *Alternatif:* önce ayrı bir
outbox altyapı spec'i, sonra 0003; bu sıra daha temiz ama 0003'ü tamamen bloke eder.



### Verilen kararlar

Beşi de Takım Yöneticisi tarafından onaylandı. Bundan sonra değişmeleri **spec revizyonudur**
(AP-08) ve SCORECARD'ın "spec revizyon sayısı" satırına yazılır.

**D3 — `Geçici kapalı` duyuru üretmez; `BranchDeactivated` yalnızca `Kapatılmış`'a geçişte doğar.**
*Gerekçe:* **A8** aracın "lokasyon ataması bekliyor" durumuna geçmesini ve vitrinden
düşmesini emrediyor; bu geri dönüşü elle atama gerektiren bir işlemdir. Tadilat için iki gün
kapanan bir şube için bunu tetiklemek, galerinin tüm stoğunu vitrinden düşürür ve geri
getirmek elle iş olur. `Geçici kapalı` bir **çalışma saati** gerçeğidir, bir **lokasyon**
gerçeği değil. *Sonucu:* `Geçici kapalı` şubenin araçları vitrinde kalır. *Bilerek ödenen
bedel:* ziyaretçi kapalı bir şubedeki aracı sorabilir; bunun karşılığı X2'deki "şu an açık
mı" bilgisidir, vitrinden düşürmek değil. *Elenen alternatif:* `Geçici kapalı`'nın da düşürmesi.
**İkinci yarısı:** `Kapatılmış` bir şube yeniden **açılamaz**:
`Kapatılmış` terminaldir (**A4**'ün `Satıldı` için kurduğu desen), yeniden açılış yeni bir
şube kaydıdır. Aksi hâlde `BranchReactivated` diye bir olay ve A8'in tersine çevrilmesi
gerekir — ikisi de bu spec'te yoktur.

**D4 — Yinelenen işleme koruması olay kimliğine göre yapılır ve bu spec'in kapsamındadır.**
*Gerekçe:* `docs/architecture.md` § 2.C idempotentliği "**olay kimliğine göre** yinelenen
işleme koruması" diye tanımlıyor. Bugünkü iki tüketici (`BranchDetailsChangedHandler`,
`BranchDeactivatedHandler`) **doğaları gereği** idempotent — `ExecuteUpdate` ve
`ExecuteDelete` ikinci kez çalışınca sonuç değişmiyor. Bu bir tasarım değil, **şans**:
sayaç artıran veya bildirim gönderen ilk tüketici geldiğinde kırılır. *Bilerek ödenen bedel:*
alıcı tarafında işlenmiş olay kaydı tutulur; 0003 bir tablo daha büyür.
*Elenen alternatif:* "handler'lar doğal olarak idempotent olsun" kuralıyla yetinmek —
kuralı çiğneyeni hiçbir test yakalamaz.

**D5 — Teslim edilemeyen duyurunun davranışı bu spec'te tanımlanır.**
*Gerekçe:* bu spec'in varlık nedeni A8'in çalışmasıdır; sessizce duran bir dağıtıcı A8'i
bugünkü durumdan farksız biçimde yine öldürür — üstelik bu kez çalıştığı sanılarak.
*Karar:* (a) bir alıcının hata vermesi duyuruyu teslim edilmiş saymaz ve yeniden denenir;
(b) yeniden denemeler artan aralıkla yapılır; (c) belirli bir denemeden sonra duyuru
**başarısız** olarak işaretlenir ve loglanır — sessizce düşürülmez; (d) bir alıcının
başarısızlığı diğer alıcıları engellemez. *Bilerek ödenen bedel:* dağıtıcı basit bir
döngüden fazlası olur. *Elenen alternatif:* "sonsuza dek yeniden dene" — zehirli bir olay kuyruğu kilitler.

**D6 — R12'nin okuma yüzeyi bu spec'te en az düzeyde kalır.**
*Gerekçe:* R12 bugün **tüketicisi olmayan** bir gereksinim. `VehicleCatalog` şube bilgisini
olaydan alıyor, senkron çağrıyla değil; `docs/architecture.md` § 2.A'nın saydığı
`Identity → Dealership` çağrısının sahibi olan `Identity` modülü yok. *Karar:* yüzey
"şube kimliği geçerli mi + şubenin ziyaretçi locale'indeki adı nedir" ile sınırlı tutulsun
ve bir kriterle doğrulansın (AC-30); genişletme, ihtiyacı doğuran spec'e bırakılsın.
*Elenen alternatif:* R12'yi tümden kapsam dışına almak — o zaman modülün hiçbir okuma
yüzeyi olmaz ve ilk tüketici sınırı zorlamaya başlar.

**D8 — Saat dilimi doğrulaması `TimeZoneInfo` ile yapılır ve tzdata bir kapıyla korunur.**
*Gerekçe:* AC-13 `Europe/Istanbul`'u kabul, `+03:00`'ü ret istiyor. `TimeZoneInfo` işletim
sisteminin tzdata'sına bakar; paket eksikse **geçerli bölge kimliği de reddedilir** ve hata
yalnızca üretimde görünür — sessiz ve pahalı bir kusur. *Karar:* doğrulama `TimeZoneInfo`
ile yapılır, dağıtım imajına `tzdata` eklenir ve varlığı bir testle korunur.
*Bilerek ödenen bedel:* bir kapı daha.

**D9 — Dağıtıcı `BuildingBlocks`'ta jeneriktir; her modül kendi `DbContext`'iyle kaydeder.**
*Gerekçe:* `architecture.md` § 2.C "arka plan dağıtıcısı" diyor ama yerini söylemiyor. Genel
bir dağıtıcının her modülün outbox tablosunu okuması Yasak 16'ya takılır. `OutboxDispatcher<TDbContext>`
jenerik olur, tablo modülün kendi şemasında kalır ve okuyan da modülün kendi context'idir —
§ 2.C ile Yasak 16'yı birlikte karşılayan tek düzen budur. *Bilerek ödenen bedel:*
`BuildingBlocks` büyür; ama yalnız altyapı alır, iş kuralı almaz.

**D7 — Şube görseli ve galeri logosu bu spec'te yalnızca **referans** olarak taşınır.**
*Gerekçe:* X5 "şube görsele **referans** taşır" diyor ama R2 böyle bir alan saymıyor;
R1 de "logo" diyor, AC ise "logo **referansı**" diyor. İkisi arasındaki fark bir alanın
var olup olmamasıdır. *Karar:* galeri profilinde bir logo medya referansı, şubede bir
görsel medya referansı listesi bulunsun; içerik, yükleme ve URL çözümü `Media` spec'ine
kalsın. *Bilerek ödenen bedel:* bugün çözülemeyen, yalnızca saklanan bir kimlik taşınır.
*Elenen alternatif:* alanları hiç koymamak ve `Media` spec'inde migration ile eklemek.

---

## Acceptance Criteria

> Gözlem noktası **C16**'dadır: bu spec'te HTTP ucu yoktur, kriterler modül düzeyinde
> doğrulanır.

**Galeri profili**

- **AC-1.** Kurulumda tam olarak bir galeri profili bulunur; ikinci bir profil oluşturulamaz.
- **AC-2.** Galeri profili ticari unvan, logo medya referansı, vergi/sicil bilgisi ve sosyal
  hesap bağlantılarını taşır.

**Şube kaydı — alanlar**

- **AC-3.** Bir şube; görünen ad, adres, koordinat, telefon, WhatsApp numarası, e-posta,
  saat dilimi, durum, slug ve görsel medya referansları alanlarıyla kaydedilir.
- **AC-4.** Şubenin görünen adı ve adresi locale başına saklanır; aynı şube için TR ve AR
  adları birbirinden bağımsız okunur.
- **AC-5.** Ziyaretçinin locale'inde karşılığı olmayan bir şube adı istendiğinde varsayılan
  locale'deki ad döner; boş değer dönmez.
- **AC-6.** Varsayılan locale'deki görünen adı veya adresi olmayan bir şube kaydedilemez.

**Şube kaydı — zorunluluk ve biçim**

- **AC-7.** Koordinat, WhatsApp numarası ve e-posta zorunlu değildir; üçü de boş bırakılarak
  bir şube kaydedilebilir.
- **AC-8.** Telefonu olmayan bir şube kaydedilemez.
- **AC-9.** Telefon ve WhatsApp numarası E.164 biçiminde değilse şube kaydedilemez.
- **AC-10.** Verilen e-posta geçerli biçimde değilse şube kaydedilemez.
- **AC-11.** Aynı slug'a sahip ikinci bir şube kaydedilemez.
- **AC-12.** Slug küçük harfli ASCII slug biçiminde değilse şube kaydedilemez
  (`Kadıköy-Şubesi` reddedilir, `kadikoy-subesi` kabul edilir).
- **AC-13.** Saat dilimi IANA bölge kimliği değilse şube kaydedilemez; `+03:00` reddedilir,
  `Europe/Istanbul` kabul edilir.
- **AC-14.** Şube durumu yalnızca **aktif**, **geçici kapalı** ve **kapatılmış** değerlerinden
  birini alır.
- **AC-15.** Veritabanında şube durumunun çevrilmiş adı saklanmaz; durum kodu saklanır.

**Durum geçişleri ve son şube koruması**

- **AC-16.** Galerideki son **aktif** şube kapatılamaz; girişim reddedilir.
- **AC-17.** İki aktif şubeden biri kapatıldığında işlem başarılı olur ve geriye bir aktif
  şube kalır.
- **AC-18.** Bir şubenin **geçici kapalı** yapılması, o şube tek aktif şube olsa bile
  reddedilmez.
- **AC-19.** **Kapatılmış** bir şube yeniden aktif edilemez; girişim reddedilir.

**Duyurular — üretim**

- **AC-20.** Yeni bir şube açıldığında `BranchOpened` duyurusu üretilir ve şube kimliğiyle
  locale başına adları taşır.
- **AC-21.** Şubenin görünen adı değiştiğinde `BranchDetailsChanged` duyurusu üretilir ve
  locale başına **güncel** adları taşır.
- **AC-22.** Şubenin adresi, telefonu, koordinatı, saat dilimi veya slug'ı değiştiğinde
  `BranchDetailsChanged` duyurusu **üretilmez**.
- **AC-23.** Bir şube **kapatılmış** durumuna geçtiğinde `BranchDeactivated` duyurusu
  üretilir.
- **AC-24.** Bir şube **geçici kapalı** yapıldığında `BranchDeactivated` duyurusu
  **üretilmez**.

**Duyurular — teslimat**

- **AC-25.** Şube değişikliği kaydedildiğinde duyurusu da kaydedilmiştir; değişiklik geri
  alındığında kaydedilmiş bir duyuru kalmaz.
- **AC-26.** Aynı duyuru alıcıya iki kez ulaştığında ikinci işleme ek bir etki üretmez;
  koruma duyurunun **kimliğine** dayanır, alıcının işleminin rastlantısal olarak tekrara
  dayanıklı olmasına değil.
- **AC-27.** İki duyuru üretildikleri sıranın tersine işlendiğinde sonuç yine yayıncının son
  hâline yakınsar; hiçbir alıcı sıraya bağımlı değildir.
- **AC-28.** Bir alıcı duyuruyu işlerken hata verdiğinde duyuru teslim edilmiş sayılmaz ve
  yeniden denenir; denemeler tükendiğinde duyuru **başarısız** olarak işaretlenir ve
  loglanır — sessizce düşürülmez.
- **AC-29.** Bir alıcının başarısızlığı diğer alıcıların aynı duyuruyu işlemesini
  engellemez.
- **AC-30.** Duyuru üretimi, alıcı modülün hazır olup olmamasından bağımsız çalışır;
  `Dealership` alıcıyı tanımaz.

**Okuma yüzeyi**

- **AC-31.** Başka bir modül, `Dealership.Contracts` üzerinden bir şube kimliğinin geçerli
  olup olmadığını ve şubenin verilen locale'deki adını sorabilir; `Dealership` bu sorguyu
  karşılarken başka modüle çağrı yapmaz.

**Uçtan uca**

- **AC-32.** `BranchDeactivated` duyurusu `VehicleCatalog` tarafından alındığında o şubedeki
  araçlar vitrinden düşer. *(Zincirin uçtan uca çalıştığının kanıtı; tüketici tarafı
  `0001`'de zaten yazılmıştır. Testin yeri **C17**'dedir.)*

**Sınır ve kalıcılık**

- **AC-33.** `Dealership` dışındaki hiçbir modül galeri veya şube tablosuna erişmez; mimari
  test bu ihlali kırar.
- **AC-34.** Her modülün migration'ları yalnızca kendi şemasına dokunur; `Dealership`
  `dealership` şemasına, alıcının inbox tablosu alıcının kendi şemasına yazılır (C14).
- **AC-35.** Mimari test, kod tabanında sabit hafta sonu günü adı, sabit çalışma günü sayısı
  ve varsayılan açılış/kapanış saati sabiti bulunmadığını doğrular.

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — modül sınırı ihlali yok; `Dealership` yalnızca kendi şemasına
      dokunuyor ve başka modüle senkron çağrı yapmıyor.
- [x] Outbox yolu uçtan uca test edildi: yayından tüketiciye, tekrar ve başarısızlık dâhil
      (AC-25, AC-26, AC-28, AC-29).
- [x] AC-32'nin uçtan uca testi `tests/Host.Tests` altında yaşıyor ve modül test projelerine
      sızmıyor (C17).
- [x] Yeni migration yalnızca `Dealership` şemasına dokunuyor.
- [x] Arayüz metni **yok**; çeviri anahtarı kontrolü bu spec için **uygulanabilir değildir**
      (X1, X3). Şube adı/adresi veridir, arayüz metni değildir.
- [x] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır.
- [x] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [x] Arayüz kriteri **yok**; görsel kanıt bu spec için uygulanabilir değildir (X1, X3).
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [x] Takım Yöneticisi onayladı ve squash-merge edildi.
- [x] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **1** | C14 ve AC-34, **onay anında** düzeltildi (R-1): "yalnızca `Dealership` şemasına" diyorlardı ve D4 ile çelişiyorlardı. Onaydan **sonra** spec'e hiç dokunulmadı; kriterler uygulama boyunca değişmedi. |
| **Düzeltme turu sayısı** | **—** | **Ölçülmedi** (`—`, 0 değil): PR bu kayıtla birlikte açıldı, gözden geçirme turu hiç olmadı. |
| **Dış bulgu (gerçek/gürültü)** | **— (dış gözden geçirme yok)** | Bu spec triyajdan geçmedi; PR bu kayıtla birlikte açıldı. |
| **Uygulama içi bulgu** | **3** | Üç kusuru testler ve kendi doğrulamam buldu; ayrıntıları aşağıdaki derste. |
| **Regresyon sayısı** | **1** | `MigrationTasir_HerDbContextSahibiModul` daha önce yeşildi ve `IOutboxDbContext.cs` eklenince kırmızıya döndü — testin kuralı değil TESPİTİ fazla genişti. Merge öncesi kapandı (`8502c84`). |
| **Kaçan hata** | **1** | *2026-09-22:* `MigrateDealershipAsync` tanımlıydı ama **hiçbir yerden çağrılmıyordu**. Temiz bir kurulumda şubeli personel ekleme `42P01: relation "dealership.branches" does not exist` ile düşüyordu. `0006` üzerinde çalışılırken bulundu; `Dealership` artık `IModuleSchema` üzerinden kurulum adımına kayıtlı. |

**Bu spec'ten çıkarılan ders:**
**Test edilmiş bir alıcı, yayıncısı olmadan hiçbir şey kanıtlamaz.** `VehicleCatalog` iki
şube dinleyicisini `0001`'de yazdı, entegrasyon testleri yeşildi ve iki spec boyunca yeşil
kaldı — ama o testler handler'ı **doğrudan** çağırıyordu. Aradaki dağıtım yolu hiç var
olmadığı için A8'in güvenlik ağı baştan beri ölüydü ve **yeşil testler bunu gizliyordu.**
Kapatılan bir şubenin araçları vitrinde kalmaya devam ederdi ve kimse fark etmezdi.

Bunun kuralı şu: bir modülün dışıyla konuştuğu her yolda, testin gözlem noktası **o yolun
iki ucunu da** kapsamalı. AC-32 bu yüzden `Host.Tests`'te yaşıyor ve elle handler çağırmak
yerine arka plan dağıtıcısının teslim etmesini bekliyor; dinleyicinin kaydı kaldırıldığında
kırmızıya düştüğü probe ile doğrulandı.

İkinci ders, `frontend-001`'inkinin tekrarı: **yeşil test her zaman doğru sebeple yeşil
değildir.** AC-27'nin testi "ters sırada işlenirse" diyordu ama ters sırayı hiç kurmuyordu;
üstelik iki duyuru aynı `OccurredAt`'i taşıdığı için sıralanacak bir şey de yoktu. Aynı
tuzak bu spec'te iki kez çıktı — bir kriteri karşıladığını söyleyen testin, karşılamadığında
gerçekten kırmızıya düştüğü ayrıca denenmeli.
