# 0018 — Bülten Aboneliği

| | |
|---|---|
| **Durum** | Onaya sunuldu (2026-11-19) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Notifications` (abonelik kaydı, teyit, çıkış, bülten dönemi, şablonlar, migration) · `frontend/` public vitrin (ana sayfadaki bülten bölümü, teyit ve çıkış sayfaları) · `docs/domain.md` (sözlük önerisi — **D19**) |
| **Bağımlı spec'ler** | `0009` (bildirim altyapısı — tamamlandı; **X2**'sini bu kart kapatır) · `0012b` (yayın olayları — tamamlandı; **D31**'in formülü bu kartta kullanılmaz, → **D4**) · `0017` (KVKK sayfası — tamamlandı) · `0020` (galeri adı — tamamlandı) · `0014` (ana sayfa — tamamlandı; **X2**'si bu kart) · `0019` (izin metni sürümü — taslak; ilişki **X12**) |
| **Dayanak** | ADR 0003 Ek 1, sıra #13 · depo sahibinin 2026-11-19 kararları (**D1–D3**) |
| **Karar bekliyor** | **Takım Yöneticisi:** S3 ve S4 önerileri (**D4**, **D5**) ile teknik kararlar **D6–D20**. **Sahip:** § Sahibe sorular **SS1–SS5** (yasal; kriterleri değiştirmez, canlıya çıkışı etkileyebilir). |

---

## Intent

Vitrine gelen ziyaretçinin çoğu **o gün almıyor.** Araç almak haftalar süren bir karardır ve
ziyaretçi bu süre boyunca birden çok galeriye bakar. Bugün o ziyaretçiyi geri getirecek
hiçbir yol yok: ya kendi aklına gelip adresi yeniden yazacak ya da başka bir galeride
karar verecek.

Galerinin elinde ise düzenli bir haber var: **stoğa yeni araç giriyor.** Bu haber bugün
yalnızca vitrine bakanlara ulaşıyor.

**Galeri** ilgilenen ziyaretçiye yeni araçları haber vermek istiyor; **ziyaretçi** her gün
site kontrol etmek yerine haberin kendisine gelmesini istiyor — ve **tek tıkla** bu listeden
çıkabilmek istiyor.

Bu iş bittiğinde ziyaretçi e-posta adresini bırakıp onaylar, haftada bir yeni araçların
özetini alır ve istediği an tek bağlantıyla çıkar. Başarı ölçütü: **onay vermemiş hiç kimseye
pazarlama mesajı gitmez, çıkmak isteyen tek adımda çıkar ve yeni araç olmayan hafta kimseye
mesaj gitmez.**

---

## Requirements

- **R1.** Ziyaretçi vitrinin ana sayfasında e-posta adresini bırakarak bültene abone olmak
  isteyebilir (**D16**).
- **R2.** Abonelik, ziyaretçi **adresini teyit etmeden** etkinleşmez ve teyit edilmemiş adrese
  bülten gitmez (**D1** — sahip kararı, 2026-11-19).
- **R3.** Abonelik kaydı, ziyaretçinin **hangi dilde** abone olduğunu taşır; teyit iletisi ve
  bülten o dilde gider. → `docs/domain.md` **L1** ile aynı ilke (**D14**).
- **R4.** Abone olmadan önce ziyaretçiye **ne alacağı** (haftalık yeni araç özeti) ve **nasıl
  çıkacağı** söylenir; KVKK aydınlatma metni o dilde yayındaysa ona bağlantı verilir
  (**D15**). → `0017` **D5**.
- **R5.** Her bülten iletisi **abonelikten çıkma bağlantısı** taşır; bağlantı **tek adımda**
  çalışır ve çıkmak için giriş veya onay istemez (**D6**, **D8**).
- **R6.** Abonelikten çıkan adrese bir daha bülten gönderilmez; çıkış kaydı **silinmez**,
  çıkmış olarak işaretlenir.
- **R7.** Aynı adres ikinci kez abone olmaya çalıştığında yeni bir kayıt oluşmaz ve
  ziyaretçiye adresin durumu **ifşa edilmez** — cevap her durumda aynıdır (**D5**).
- **R8.** Bülten, bir önceki bülten döneminden bu yana **ilk kez yayına alınan** ve gönderim
  anında hâlâ **Yayında** olan ilanların özetini taşır. Daha önce yayınlanmış bir ilanın
  yeniden yayına alınması "yeni araç" değildir (**D4**, **D12**).
- **R9.** Bir dönemde gönderilecek yeni araç yoksa **bülten gönderilmez**; boş bülten çıkmaz.
- **R10.** Bülten ve teyit iletileri `0009`'un **kuyruğunu ve tekrar deneme** davranışını
  kullanır; ikinci bir gönderim mekanizması kurulmaz.
- **R11.** Abonelik formu **yükleniyor**, **başarılı**, **doğrulama hatası**, **hız sınırı** ve
  **hata** durumlarını ele alır; teyit ve çıkış sayfaları **yükleniyor**, **başarılı**,
  **geçersiz bağlantı** ve **hata** durumlarını ele alır. → `docs/frontend.md` § 3.
- **R12.** Form, teyit sayfası, çıkış sayfası ve ileti şablonları üç locale'de çalışır;
  Arapça'da RTL.
- **R13.** Aynı kaynaktan kısa sürede gelen aşırı sayıda abonelik isteği reddedilir; aynı
  adrese kısa sürede çok sayıda teyit iletisi gönderilmez (**D9**). → `0005` **R16** / **C17**
  deseni.
- **R14.** Teyit bağlantısının **süresi** vardır ve **tek kullanımlıktır**; süresi dolmuş,
  kullanılmış ya da yerine yenisi gönderilmiş bağlantı aboneliği etkinleştirmez (**D7**).
- **R15.** Bülten **haftada bir** gider; gönderim günü ve saati **dağıtım yapılandırmasından**
  gelir ve panelden değiştirilemez (**D2** — sahip kararı, 2026-11-19; **D13**).
- **R16.** Bülten her araç için ziyaretçinin karar vermesine yetecek özeti ve ilanın
  bağlantısını taşır; bir bülten sınırsız uzamaz (**D11**).
- **R17.** Teyit iletisi ve bülten, gönderenin **galeri** olduğunu adıyla söyler (**D18**).
- **R18.** Talep formunda verilen iletişim izni bülten izni yerine **kullanılmaz** (**D3** —
  sahip kararı, 2026-11-19).

---

## Constraints

### Uyulması zorunlu

- **C1.** Dışarıya giden her mesaj **yalnızca `Notifications`** üzerinden gider.
  → Yasak Listesi 17 · `0009` **C1**.
- **C2.** **İletişim izni ve abonelikten çıkma `Notifications`'ın verisidir.**
  → `docs/architecture.md` § 1 (`Notifications` sorumluluk satırı).
- **C3.** İzni olmayana **pazarlama** mesajı gönderilmez; işlemsel mesaj bundan ayrıdır.
  Teyit iletisi işlemseldir, bülten pazarlamadır. → `docs/domain.md` **L7**.
- **C4.** **Talep izni ile bülten izni ayrı izinlerdir.** `0005`'in topladığı
  `ConsentGiven`, bülten aboneliği **değildir** (**D3**).
- **C5.** Kişisel veri (e-posta adresi, IP) ne loga ne gönderim geçmişine yazılır; adres
  gönderim anında kaynaktan okunur. Teyit ve çıkış bağlantıları adresi içermez.
  → `0009` **C5**, `0005` **C17**.
- **C6.** Şablonlar **(anahtar × locale)** olarak tutulur ve seed migration ile gelir;
  istenen dilde şablon yoksa varsayılana düşülür. → `0009` **R9**, **C13**.
- **C7.** Olay tüketimi outbox üzerinden ve **idempotent**'tir; aynı olay iki kez gelirse
  ilan bültende iki kez yer almaz. → `0009` **C3**.
- **C8.** Kullanıcıya görünen metin koda gömülmez; API kararlı bir `errorCode` döner.
  → Altın Kural 6.
- **C9.** Zaman damgaları UTC. → Yasak Listesi 30.
- **C10.** Yeni migration yalnızca `notifications` şemasına dokunur. → Yasak Listesi 3 ·
  `0009` **C12**.
- **C11.** Her kabul kriteri en az bir testle karşılanır.
- **C12.** `Notifications` **yaprak** modül olarak kalır: hiçbir modül ona senkron bağlanmaz,
  o da olay yayınlamaz. Başka modülden okuma tek atlama ve salt okumadır.
  → `0009` **C4**, **C11**.
- **C13.** **Vitrin performans kapısı (C3 eşikleri) kırmızıya dönmez:** ana sayfa, liste ve
  ilan detayı için Lighthouse mobil medyan LCP < 2,5 sn, CLS < 0,1. Bülten metinleri vitrin
  çeviri kümesine girer ve ilk paketi büyütür; büyüme **D17**'nin bütçesindedir.
  → ADR `0013`, `docs/frontend.md` § 5.
- **C14.** Zorunlu yapılandırma eksikse uygulama ayağa kalkmaz; koda gömülü bir varsayılan
  üretilmez. → `0009` **C9** deseni (**D13**).

### KAPSAM DIŞI

- **X1.** **Segmentasyon** ("yalnızca SUV ilgilenenlere gönder") — *neden:* ilgi alanı
  verisi toplanmıyor ve toplamak ayrı bir izin ve veri kararı.
- **X2.** **Açılma ve tıklama takibi** — *neden:* takip pikseli ve bağlantı sarmalama KVKK
  açısından ayrı bir aydınlatma gerektirir (`0009` **X7**); `docs/architecture.md` § 6
  analitiği üçüncü partiye bıraktı.
- **X3.** **Fiyat düşüşü ve favori aracı için bildirim** — *neden:* favori `localStorage`'da
  (§ 6) ve sunucu kimin neyi favorilediğini bilmiyor.
- **X4.** **Panelden bülten içeriği düzenleme / kampanya metni ekleme** — *neden:* kampanya
  `Content` tarafının işi (`0017` **X2**) ve bülten içeriği bu spec'te **otomatik** üretilir.
- **X5.** **SMS ve WhatsApp ile bülten** — *neden:* `0009` **X1** kanalları kapsam dışı
  bıraktı; ayrıca pazarlama SMS'i ayrı bir izin sınıfı.
- **X6.** **Abone listesinin panelden görüntülenmesi ve dışa aktarılması** — *neden:*
  kişisel veri listesini bir ekrana koymak kendi yetki ve KVKK kararını gerektirir.
- **X7.** **Talep formuna bülten onay kutusu** — *neden:* sahip kararı (**D3**): `0005`'in
  talep izni bülten için kullanılmaz; ayrı bir kutu `0005`'i değiştirir ve ayrı kart ister
  (ADR 0003 unsur 1).
- **X8.** **İYS (İleti Yönetim Sistemi) entegrasyonu** — onayların İYS'ye bildirilmesi,
  İYS'den gelen retlerin içeri alınması — *neden:* yükümlülüğün olup olmadığı hukuki bir
  sorudur ve sahibe sorulmuştur (**SS1**); gerekirse kendi kartını hak eder. Bu kart, böyle
  bir entegrasyonun dayanacağı kaydı (izin, zaman damgası, çıkış) üretir.
- **X9.** **Bülten bölümü metinlerinin panelden düzenlenmesi** — *neden:* `0022` ana sayfanın
  panelden düzenlenen altı alanını sabitledi; bülten metinleri çeviri kataloğundadır (**D16**).
- **X10.** **Gönderim sıklığının panelden değiştirilmesi** — *neden:* sahip kararı (**D2**).
- **X11.** **Kalıcı teslim hatasında (adres reddedildi) aboneliğin otomatik kapanması ve
  sağlayıcı şikâyet geri bildirimi** — *neden:* `0009` kalıcı hatayı **mesaj** başına işler;
  hatayı abonelik durumuna bağlamak sağlayıcıya özgü geri bildirim kanalı ister ve ayrı bir
  karardır. Kalıcı hata yine `0009`'daki gibi tekrar denenmez.
- **X12.** **Bülten izni anındaki aydınlatma metni sürümünün kaydı** — *neden:* `0019` bunu
  talep izni için ele alıyor ve henüz taslak; bülten için gerekip gerekmediği **SS5**.
- **X13.** **E-posta başlığında istemcinin "abonelikten çık" düğmesi** (List-Unsubscribe) —
  *neden:* vaat edilen tek adımlı çıkış gövdedeki bağlantıyla karşılanır; başlık bir teslim
  edilebilirlik iyileştirmesidir ve ayrı karar ister.
- **X14.** **Görselli / HTML bülten** — *neden:* `0009` **X6** şablonları düz metin tuttu;
  marka tasarımı ve istemci uyumluluğu ayrı bir iş.
- **X15.** **Teyit hatırlatma iletisi** — *neden:* teyit etmeyene ikinci kez yazmak, izni
  olmayana yazmaktır.

---

## Context

### Mevcut davranış — ölçüldü (2026-11-19, `main` @ `740c771`)

Her madde dosyasıyla birlikte; **koşturulmadan, koddan okunanlar** öyle işaretli.

- **Bu spec'i `0009` kendisi tetikledi.** `0009` **X2**: abonelikten çıkma *"pazarlama mesajı
  girdiği gün zorunlu hâle gelir ve o zaman kendi spec'ini hak eder."* Kodda karşılığı:
  `TemplateKeys.All` yalnız iki işlemsel anahtar taşıyor ve yorumu *"Üçüncüsü yoktur (X2:
  pazarlama yok)"* diyor (`Notifications/Domain/NotificationTemplate.cs:23-29`). Bu spec
  pazarlama sınıfını **ilk kez** var ediyor; **L7**'nin ayrımı ilk kez gerçek bir fark yaratacak.
- **`VehiclePublished` üretiliyor** — taslak "kodda yok" diyordu, artık var. Altı alan:
  `EventId`, `OccurredAt`, `VehicleId`, `ListingId`, `Slug`, `PublishedAt`
  (`VehicleCatalog.Contracts/Events/VehicleEvents.cs:26-32`). Model, fiyat, şube **taşımıyor**
  (`0012b` **D30**: değişken veri kaynaktan okunur). Üreten tek yer
  `VehicleCatalogUnitOfWork.cs:118`; her `ListingPublished` alan olayı için yazılıyor —
  **yeniden yayın da dâhil** (`0012b` **D31**).
- **`PublishedAt` ilk yayın zamanıdır ve donar:** `PublishedAt ??= clock.UtcNow`
  (`VehicleCatalog/Domain/Listing.cs:193`).
- **`0012b` D31'in formülü kodla tutmuyor** (koddan okundu, koşturulmadı). D31 "bu ilk yayın
  değil" bilgisini `OccurredAt > FirstPublishedAt` karşılaştırmasına bırakmıştı. Alanın adı
  `PublishedAt`'tır ve iki değer **ayrı** `clock.UtcNow` okumalarından gelir: `PublishedAt`
  `Listing.Publish` içinde (`Listing.cs:193`), `OccurredAt` ise kayıt sırasında
  `Translate` içinde (`VehicleCatalogUnitOfWork.cs:110`). Gerçek saatte `OccurredAt` **ilk
  yayında da** `PublishedAt`'tan büyüktür; formül her ilk yayını "yeniden yayın" sayar. Sahte
  saatle koşan testte iki değer eşit çıkar ve kusur görünmez. → **D4** pencereyi bu yüzden
  yalnız `PublishedAt` üzerine kurar.
- **Bugün `VehicleCatalog` olaylarının HİÇ tüketicisi yok.** `VehicleCatalog.Contracts.Events`'i
  kullanan tek dosya üreticinin kendisi. Outbox dağıtıcısı alıcısı olmayan olayı **işlenmiş**
  işaretliyor (`BuildingBlocks/Outbox/OutboxDispatcher.cs:67-91`: alıcı listesi boşsa hata
  yok, `ProcessedAt` yazılır — koddan okundu). Sonuç: bu kart devreye girmeden önce yayınlanan
  ilanların duyuruları **geri gelmez** → **D13**'ün "ilk dönem" kuralı.
- **İlan kimliğiyle vitrin özeti dönen bir okuma yok.** `VehicleCatalog.Contracts`'ta
  `IListingSlugQueries` yalnız adres, `IListingSnapshotQueries` adresle arayıp model
  **kimliği** döner (`Listings/ListingSnapshotContracts.cs`); model adı, yıl, km, fiyat ve şube
  adını taşıyan `ShowcaseListingDto` yalnız filtreli liste ve öne çıkanlar sorgularından gelir
  (`Showcase/ShowcaseListingContracts.cs`). R16'nın içeriği gönderim anında okunacaksa bu
  yüzeyde bir eksik var — çözümü geliştiricinin.
- **`Notifications`'ın gönderim yolu talebe bağlı.** Gönderim kaydı zorunlu bir `LeadId`
  taşıyor (`Notifications/Domain/NotificationMessage.cs:29`) ve dağıtıcı alıcıyı
  `ILeadNotificationQueries` ile talepten çözüyor (`Application/NotificationDispatcher.cs:96`).
  Kuyruk, tekrar deneme, hata sınıfları ve şablon düşüşü **yeniden kullanılabilir**; alıcının
  talepten başka bir kaynaktan gelmesi bugün mümkün değil. R10 bunu genişletmeyi gerektiriyor.
- **Alıcı adresi gönderim kaydında yok** (`NotificationMessage.cs:12-17`, `0009` **C5**) —
  bülten de aynı kurala tabi.
- **`Notifications`'ın HTTP ucu yok** (modülde `Map*` çağrısı sıfır) ve **zamanlanmış işi yok**
  — tek arka plan servisi kuyruğu yoklayan `NotificationQueueHostedService`. Abonelik formu
  modülün ilk public ucu, haftalık dönem ilk zamanlı işi olacak (**D20**).
- **Hız sınırı deseni `0005`'te:** **R16** + **C17** — aynı IP'den 10 dakikada 5 **kaydedilen**
  talep, `429` + `lead.rate_limited`; IP yalnız geçici sayaçta. Eşikler yapılandırmadan
  (`Host/Program.cs:118-128`). *(Taslak bunu `0005` R12 diye anıyordu; R12 "talep `Yeni`
  durumunda başlar" kuralıdır.)*
- **KVKK aydınlatma metni var.** `privacy` sistem sayfası (`0017` **D6**); vitrin onu slug'dan
  bağımsız **kimliğiyle** bulur ve `/{locale}/{slug}` adresine bağlar; o dilde yayında değilse
  bağlantı basılmaz (`frontend/src/features/leads/LeadForm.tsx:134-164`,
  `shared/lib/route.ts:213`). R4 aynı yolu kullanır (**D15**).
- **Galeri adı okunabiliyor:** `IGalleryDisplayNameQueries.FindDisplayNameAsync(locale)`
  (`Dealership.Contracts/Gallery/PublicGalleryContracts.cs:24-30`, `0020`). R17'nin kaynağı.
- **Ana sayfada bülten bölümü yok.** `HomePage.tsx` giriş bölümü + `HomeCatalog`'dan ibaret;
  altlık (`HomeFooter`) yalnız ana sayfa rotasında basılıyor (`App.tsx:88-96`). `0014` **X2**
  bülteni bu karta bıraktı; `0022`'nin panelden düzenlenen altı metni arasında bülten **yok**.
- **Mockup depoda değil.** ADR 0003 mockup'ı bir tasarım tuvali olarak anıyor; taslağın
  alıntıladığı metin (*"Yeni gelen araçları ilk siz görün … haftada bir … Tek tıkla
  çıkabilirsiniz. Adresiniz üçüncü kişilerle paylaşılmaz — KVKK aydınlatma metni."*) depoda
  arandı ve **bulunamadı** (yalnız bu dosyada geçiyor); **ölçülmedi**, taslaktan taşındı.
  Bölümün yeri `0014` Context'inin mockup sırasından: öne çıkanlar → marka listesi → bülten →
  altlık.
- **Vitrin LCP payı dar.** C3 eşiği 2500 ms. ADR `0013` öncesi `main`'de ana sayfa **2481 ms**
  ölçülmüştü (19 ms pay) ve yalnız panel ekranı ekleyen iki kart vitrini kırmızıya
  çevirmişti; çeviri kataloğu vitrin/panel diye bölündü, ilk paket gzip 122.818 → 100.952 B.
  Bugünkü son ölçüm (koşu `44505722622`, #118'in dalı, 2026-11-19): ana sayfa medyan
  **2298 ms** (koşular 2298 / 2273 / **2430**), liste **2121 ms**, ilan detayı **2405 ms**.
  Tek koşu içindeki yayılım 157 ms; ana sayfanın LCP öğesi giriş açıklaması
  (`p.home-hero__body`). Ana sayfanın tek koşusu 2430 ms'ye, detay sayfası medyanı 95 ms paya
  iniyor. Bu kart vitrine **form + iki sayfa + üç dilde metin** ekliyor ve her vitrin anahtarı
  üç dilde ilk pakete girer (ADR `0013` § Karar) → C13, **D17**.
- **Altyapı hazır:** `0009` tamamlandı — kuyruk, arka plan servisi, artan aralıkla tekrar
  deneme, geçici/kalıcı hata ayrımı, şablon (anahtar × locale) ve varsayılan dile düşme.

### Bilinen tuzaklar

1. **Teyitsiz aboneliğe gönderim.** Birinin başkasının adresini yazması, o adrese istenmeyen
   pazarlama gitmesi demektir (R2).
2. **Bağlantıyı kendiliğinden açan tarayıcılar.** Kurumsal e-posta güvenlik ürünleri gelen
   iletideki bağlantıları açar. Teyit bağlantısını **açmak** teyit sayılırsa, başkasının
   yazdığı adres insan eli değmeden teyit edilir ve çift onay boşa düşer (**D6**). Çıkış
   bağlantısında aynı tarayıcı aboneyi istemeden çıkarabilir — bu bedel bilerek kabul edilir.
3. **Çıkışı onaya bağlamak.** "Emin misiniz?" ekranı R5'in "tek adımda" vaadini bozar.
4. **Abone adresini gönderim geçmişine ya da loga yazmak** (C5).
5. **Talep iznini bülten izni sanmak** (C4, **D3**).
6. **Boş bülten** (R9).
7. **Adresin kayıtlı olup olmadığını ifşa etmek** (R7). Ayırt edici olan yalnız mesaj değil:
   durum kodu, gövde ve **cevabın teyit iletisini beklemesi** de ayırt eder.
8. **"Yeni araç"ı olay zamanıyla ayırmak.** `0012b` D31'in formülü ilk yayını da yeniden yayın
   sayar (yukarıdaki ölçüm). Sahte saatli test bunu göstermez (AC-41).
9. **Tek kullanımlık bağlantı ve eşzamanlılık.** Ardışık test ikinci kullanımı yakalar ama
   **aynı anda** gelen iki isteği yakalamaz; eşzamanlı test bir bariyerle kurulmazsa istekler
   fiilen ardışık koşar (AC-27).
10. **Teyit iletisiyle bir adresi bombalamak.** IP başına hız sınırı, IP değiştiren birinin
    aynı kurbana yüzlerce teyit iletisi göndermesini engellemez (**D9**).

### Verilen kararlar — sahip, 2026-11-19

**D1 — Çift onay (S1).** *Sahip kararı, 2026-11-19.* Adres bırakıldığında bir teyit iletisi
gider; bağlantıya tıklanmadan abonelik etkinleşmez ve bülten gitmez. → R2, R14.
*Bedel:* teyit oranı %100 olmadığı için abone sayısı düşer; akış bir adım uzar.

**D2 — Haftalık, dağıtım yapılandırmasından (S2).** *Sahip kararı, 2026-11-19.* Sıklık haftalık;
gün ve saat yapılandırmadan gelir, panelden düzenlenmez. Yeni araç yoksa o hafta gitmez (R9).
→ R15, X10.

**D3 — Talep izni bülten için kullanılmaz (S5).** *Sahip kararı, 2026-11-19.* Talep formuna
bülten kutusu bu kartın kapsamı dışında, ayrı karttır. → R18, C4, X7.

### Önerilen kararlar — Takım Yöneticisi onayına

**D4 — "Yeni araç": `VehiclePublished` olayı, pencere ilk yayın zamanı üzerinden (S3 önerisi).**
Kaynak `VehiclePublished` olayıdır. Bir ilan, olayın taşıdığı `PublishedAt` (ilk yayın zamanı)
bir önceki dönemin kesim anından **sonra** ve bu dönemin kesim anından **önce ya da ona eşit**
ise o dönemin adayıdır. Aynı ilan bir dönemde birden çok olay üretse de (yayınla → kaldır →
yayınla) bir kez sayılır. Adayın **gönderim anındaki** durumu ve özeti `VehicleCatalog`'un
public okuma yüzeyinden okunur; yalnız o an **Yayında** olan ilanlar bültene girer (**D12**).
*Doğrulama sonucu:* taslağın önerisi **özünde geçerli** — olay üretiliyor, yeniden yayında da
üretiliyor ve ilk yayın zamanını taşıyor. **Geçersiz olan** taslağın ve `0012b` D31'in ayırma
formülü (`OccurredAt > FirstPublishedAt`): yukarıdaki ölçüm gereği ilk yayını da dışlar. Bu
karar formülü kullanmaz; `0012b` D31'in Bedel cümlesi ("pencere yeniden yayını da sayar") bu
tanımla geçersizleşir, çünkü yeniden yayının `PublishedAt`'ı önceki bir döneme düşer.
*Gerekçe:* yayına alma galerinin kasıtlı eylemidir; kayıt tarihi değil (taslak haftalar önce
girilmiş olabilir). Olay, `Notifications`'ı tüketici konumunda tutar ve "şu tarihten beri
yayınlananlar" diye yeni bir sorgu yüzeyi açtırmaz.
*Bedel:* (1) Özeti gönderim anında okumak için yine `VehicleCatalog`'a tek atlamalık bir okuma
gerekir (bugün ilan kimliğiyle özet dönen okuma yok — Context). Taslağın elediği alternatifin
"senkron bağımlılık" gerekçesi bu yüzden zayıftı; iki yolda da okuma var, fark yalnız
"hangi ilanlar" sorusunun kaynağında. (2) Kart devreye girmeden önceki duyurular kayıp (**D13**).
*Elenen alternatif:* yalnız `PublishedAt` ile sorgulamak — olaysız çalışır ve ilk dönemin
kaybını önler, ama `VehicleCatalog`'a tarih filtreli yeni bir okuma açar ve olay tüketimi
yerine zamanlı sorguyla "yeni"nin ikinci bir tanımını doğurur.

**D5 — Her durumda aynı cevap (S4 önerisi).** Adres hiç yokken, teyit beklerken, etkinken ve
çıkmışken abonelik isteğinin cevabı **aynı durum kodu ve aynı gövdedir**: "adresinizi aldık,
teyit bağlantısı gönderdik; gelmezse istenmeyen e-posta klasörüne bakın." Arkadaki davranış:
- yeni adres → kayıt **teyit bekliyor**, teyit iletisi kuyruğa girer;
- teyit bekleyen → yeni kayıt yok, yeni teyit iletisi (önceki bağlantı geçersiz — **D7**);
- etkin → hiçbir şey değişmez, ileti gitmez;
- çıkmış → aynı kayıt **teyit bekliyor**'a döner, teyit iletisi gider; teyit edilene dek
  bülten gitmez.

Cevap teyit iletisinin **gönderilmesini beklemez** (Tuzak 7).
*Doğrulama sonucu:* geçerli. Dayandığı ilkeler doğrulandı: `0002` **C13** (bulunamadı cevabı
ayırt ettirmez — gövde, kod ve süre) ve `0006` **AC-5** (taslak **AC-4** diyordu; ayırt
edilemezlik AC-5'te). `0009`'un kuyruğu cevap ile gönderimi zaten ayırıyor.
*Bedel:* gerçekten yeni abone olan biri teyit gelmezse nedenini bilemez; etkin abone ikinci kez
yazdığında hiçbir şey almaz.

**D6 — Teyit sayfadaki düğmeyle, çıkış bağlantının açılmasıyla.** Teyit bağlantısı bir sayfa
açar; abonelik sayfadaki **tek düğmeye** ("Aboneliği onayla") basılınca etkinleşir. Çıkış
bağlantısı ise **açıldığı anda** çıkarır; ek düğme yoktur.
*Gerekçe:* Tuzak 2. İki yön asimetrik: istenmeyen **teyit**, izni olmayana pazarlama demektir;
istenmeyen **çıkış**, yalnız bir bültenin kaçması. D1'in "bağlantıya tıklanmadan etkinleşmez"
kararıyla çelişmez — bağlantı yine şarttır, üstüne bir düğme eklenir.
*Bedel:* teyit bir tık uzar. Kurumsal tarayıcısı bağlantıları açan bir abone ilk bültende
istemeden çıkabilir; çıkış sayfası yeniden abone olma yolu sunar (AC-37).
*Elenen:* çıkışta da düğme — R5'in "tek adım" vaadini ve mockup'ın "tek tıkla" sözünü bozar.

**D7 — Teyit bağlantısı: süreli, tek kullanımlık, en son gönderilen geçerli.** Süre dağıtım
yapılandırmasından gelir (öneri: **72 saat**). Bağlantı bir kez kullanılınca geçersizdir;
teyit bekleyen adrese yeni teyit iletisi gidince önceki bağlantı geçersiz olur. Süresi dolmuş,
kullanılmış, geçersizleşmiş ve hiç var olmamış bağlantının cevabı **aynıdır** ("bağlantı artık
geçerli değil" + yeniden abone olma yolu).
*Gerekçe:* süresiz teyit bağlantısı, aylar sonra eline geçen birine teyit yetkisi verir; dört
geçersiz hâli ayırmak bağlantının sahibine bir şey kazandırmaz.
*Bedel:* bağlantıyı iki kez kullanan kişi ikinci seferde "geçerli değil" görür, oysa aboneliği
etkindir — sayfa metni bunu söyler ("Aboneliğinizi daha önce onayladıysanız bir şey yapmanız
gerekmez").

**D8 — Çıkış bağlantısı süresiz, aboneye bağlı, döndürülmez.** Aynı adresin bütün bültenlerindeki
çıkış bağlantısı aynı abonelik kaydına çıkar; yeniden abone olmak eski bağlantıyı geçersiz
kılmaz. Çıkmış hâldeyken bağlantıyı yeniden açmak yine "çıktınız" der, durum ve çıkış zamanı
değişmez.
*Gerekçe:* eski bir bültenden çıkmaya çalışan kişinin çıkamaması R5'i bozar.
*Bedel:* sızan bir bağlantı o kişiyi listeden çıkarabilir; zararı bir bültenin kaçmasıdır.

**D9 — İki sınır: kaynak başına istek, adres başına teyit iletisi.** (R13)
- **Kaynak:** aynı IP'den yapılandırılan pencerede yapılandırılan eşik (öneri: `0005` C17 ile
  aynı, 10 dakikada 5) kadar **geçerli** abonelik isteği; sonraki istek `429` +
  `notification.subscription_rate_limited`. Biçimi geçersiz istek bütçeyi tüketmez (`0005` C17
  notu). IP yalnız geçici sayaçta, kalıcı kayda ve loga yazılmaz.
- **Adres:** aynı adrese 24 saatte en çok **3** teyit iletisi. Aşıldığında cevap **yine aynıdır**
  (D5) ama ileti gitmez.

*Gerekçe:* Tuzak 10; ikinci sınır ifşa etmeden bombalamayı keser.
*Bedel:* dördüncü kez deneyen dürüst ziyaretçi teyit alamaz ve nedenini görmez.

**D10 — Adresin biçimi ve kimliği.** En çok 254 karakter; boş ya da gönderimin kabul etmeyeceği
biçimdeki adres `400` + `notification.subscription_email_invalid` ile reddedilir. Adresler büyük/
küçük harf farkı gözetmeden **aynı adres** sayılır; baştaki/sondaki boşluk atılır.
*Gerekçe:* `Ali@Ornek.com` ile `ali@ornek.com` iki kayıt olursa R6 ve R7 delinir (aynı kişi
çıkar, ikinci kayıt bülten almaya devam eder). Biçim denetimi, `0009`'un gönderen adresini
denetlediği otoriteyle aynı olmalı (`NotificationsOptions.IsSendableAddress` — "gönderimin
kabul edeceği adres"); iki ayrı denetim ayrıştığı an kabul edilip gönderilemeyen adres doğar.
*Bedel:* yerel kısmı harf duyarlı nadir posta sunucularında ayrı iki adres tek sayılır.

**D11 — Bülten içeriği.** Her araç için: **model adı** (abonenin dilinde), **model yılı**, **km**,
**fiyat** (ya da "Fiyat sorunuz"), **şube adı** ve ilanın abonenin dilindeki **mutlak bağlantısı**.
En çok **20** araç, ilk yayını en yeni olan önce; fazlası için "ve N araç daha" (ICU çoğul,
`0009` C7) ve vitrin listesine bağlantı. Düz metin, görsel yok (X14).
*Gerekçe:* vitrin kartının taşıdığı bilgi; 20 sınırı, stoğun toplu girildiği bir haftada
iletiyi okunur tutar.
*Bedel:* 20'den sonraki araçlar yalnız listeye bağlantıyla görünür.

**D12 — Rezerve, satılmış, arşivlenmiş ve yayından kalkmış ilan bültene girmez.** Yalnız gönderim
anında **Yayında** olan ilan.
*Gerekçe:* rezerve araç vitrinde rozetiyle kalır ama ziyaretçiye "yeni geldi" diye duyurulması
yanıltıcıdır.
*Bedel:* dönem içinde rezerve edilip sonra vitrine dönen ilan hiçbir bültene girmez (ilk yayın
zamanı geçmiş döneme düşer).

**D13 — Bülten dönemi.** Dönem anı yapılandırmadan gelir: **haftanın günü + saat, UTC** (C9).
Yapılandırma eksikse uygulama ayağa kalkmaz (C14).
- Aynı dönem iki kez işlenmez: süreç yeniden başlasa da bir aboneye bir dönem için en çok **bir**
  bülten kaydı oluşur.
- Uygulama dönem anında kapalıysa açılışta kaçırılan dönem **bir kez** işlenir; birden çok
  kaçırılmış dönem **tek bültende** birleşir (pencere: son işlenen dönemden bugüne).
- **İlk dönem** kartın devreye girdiği andan başlar; öncesinde yayınlanan ilanlar ilk bültene
  girmez (Context: duyurular geri gelmez).
- Bülten, dönem anında **etkin** olan abonelere gider; o an teyit bekleyen ya da çıkmış olana
  gitmez.

*Gerekçe:* taslağın "son başarılı gönderimden beri" tanımı, bazı alıcılarda başarısız olan bir
gönderimde belirsizdi; pencere ileti teslimine değil **dönemin işlenmesine** bağlanır, teslim
hatası `0009`'un tekrar denemesine kalır.
*Bedel:* yaz saati uygulanan ülkelerde yerel gönderim saati yılda iki kez bir saat kayar.

**D14 — Dil.** Abonelik, isteğin geldiği vitrin dilini kaydeder. Teyit bekleyen adrese başka bir
dilden yeni istek gelirse dil güncellenir (yeni teyit iletisi o dilde); **etkin** aboneliğin dili
yeni istekle değişmez (D5). Abonenin dili kurulumda artık etkin değilse ileti varsayılan dilde
gider.
*Bedel:* etkin abone bülten dilini değiştirmek için çıkıp yeniden abone olmak zorunda.

**D15 — KVKK bağlantısı `0017` D5 deseniyle.** `privacy` o dilde yayındaysa form notu ona bağlanır;
değilse bağlantı basılmaz ve form çalışmaya devam eder.
*Gerekçe:* talep formuyla aynı davranış; metin `Content`'te, galerinin elinde.
*Bedel:* aydınlatma metnini yayınlamamış bir galeri bağlantısız izin toplar — bunun kabul
edilebilirliği sahibin sorusu (**SS3**).

**D16 — Bülten bölümü yalnız ana sayfada.** Öne çıkan araçların altında, altlıktan önce
(mockup sırası). Metinler vitrin çeviri kümesindedir ve panelden düzenlenmez (X9).
*Gerekçe:* mockup bölümü yalnız ana sayfada gösteriyor; her sayfaya koymak LCP payını her
sayfada tüketir.
*Bedel:* listeye ya da ilana doğrudan gelen ziyaretçi bülteni görmez.

**D17 — Performans bütçesi.** Vitrin ilk paketinin gzip boyutu `main` tabanına göre **en çok
3 kB** artar; iki sayı PR'da yazılır. C3 medyanı bir sayfada **2450 ms**'yi aşarsa ikinci bir
koşu istenir ve LCP öğesi yazılır.
*Gerekçe:* Context'teki ölçüm: tek koşu içi yayılım 157 ms, detay sayfasının payı 95 ms.
*Bedel:* bütçe, teyit ve çıkış sayfalarının ilk pakete girmemesini fiilen zorunlu kılar.

**D18 — Galeri adı.** Teyit iletisi ve bülten, galerinin abonenin dilindeki görünen adını taşır
(`0020`); o dilde ad yoksa `0020`'nin düşüş kuralı geçerlidir.
*Gerekçe:* alıcı iletinin kimden geldiğini bilmeli (R17); yasal kimlik bilgisi ayrı bir soru
(**SS2**).

**D19 — Sözlük önerisi** (`docs/domain.md` § Bildirim; karar Takım Yöneticisinin):
**Bülten** (haftalık yeni araç özeti — pazarlama iletisi) · **Bülten Aboneliği** (adres + dil +
durum) · **Abonelik Durumu** (`Teyit bekliyor` · `Etkin` · `Çıkmış`) · **Teyit Bağlantısı** ·
**Çıkış Bağlantısı** · **Bülten Dönemi** (bir haftalık pencere ve kesim anı).

**D20 — ADR gerekmez; Karar Defteri'ne bir satır.** `Notifications` ilk public ucunu ve ilk
zamanlı işini alıyor; yaprak konumu (C12) korunuyor: olay yayınlamıyor, kimse ona senkron
bağlanmıyor. Gönderim kaydının alıcısını talepten bağımsız hâle getirmek modül içi bir
değişiklik.
*Bedel:* Takım Yöneticisi bunu mimari karar sayarsa ADR açılır — kriterleri değiştirmez.

---

## Sahibe sorular

Bunlar **karar değildir**; hukuki ya da ticari cevap ister ve Analist karara bağlamaz. Kriterler
bu cevaplardan bağımsız yazıldı; cevaplar canlıya çıkışı etkileyebilir.

**Kodun bu kartla garanti ettiği:** her abonelik için adres, dil, istek zamanı ve **teyit zamanı**
(UTC) kaydı; teyitsiz adrese bülten gitmemesi; her bültende tek adımlı çıkış bağlantısı; **çıkış
zamanı** kaydı ve çıkıştan sonra gönderim olmaması; çıkış kaydının silinmemesi; adresin loga ve
gönderim geçmişine girmemesi. **Garanti etmediği:** dış bir sisteme (İYS) bildirim, saklama
süresi uygulaması, yasal gönderici kimliği metni, izin anındaki metnin sürümü.

- **SS1. Ticari elektronik ileti ve İYS.** Türkiye'de ticari elektronik ileti mevzuatı (6563
  sayılı Kanun ve ilgili yönetmelik) e-posta onaylarının İleti Yönetim Sistemi'ne (İYS)
  kaydını ve retlerin oradan da alınabilmesini öngörüyor olabilir. Bu galeri için yükümlülük
  var mı (gönderim hacmi, muafiyet)? Varsa entegrasyon ayrı karttır (X8). KWD gibi başka ülke
  kurulumlarında hangi mevzuat geçerli?
- **SS2. Gönderici kimliği.** Ticari iletide gönderenin hangi bilgileri (ticari unvan, adres,
  iletişim, MERSİS no.) taşıması gerekiyor? Kod yalnız galerinin görünen adını basacak (D18);
  fazlası şablon metni ya da kurulum verisi olarak eklenebilir.
- **SS3. Aydınlatma ve saklama.** (a) Aydınlatma metni yayında değilken izin toplanmasına izin
  verilsin mi (D15 izin veriyor)? (b) Çıkmış adresin kaydı ne kadar saklanır ve KVKK silme
  talebinde ne olur — silinirse "bu adres çıkmıştı" bilgisi de gider. (c) Hiç teyit edilmemiş
  kayıtlar ne kadar tutulur?
- **SS4. Mockup'ın vaadi.** "Adresiniz üçüncü kişilerle paylaşılmaz" cümlesi, adresi işleyen bir
  e-posta sağlayıcısı kullanılırken hukuken doğru mu? Cümle çeviri kataloğuna girecek; ifadesi
  sahibin onayıyla yazılmalı.
- **SS5. İzin metninin sürümü.** `0019` talep izni için "hangi metne izin verildi" kaydını
  tartışıyor. Bülten izni için de aynı kayıt gerekli mi? (X12)

---

## Acceptance Criteria

### Abone olma formu (R1, R4, R11 · D15, D16)

- **AC-1.** Ana sayfada öne çıkan araçlar bölümünün altında ve altlıktan önce bir bülten bölümü
  vardır: başlık, haftalık yeni araç özeti alınacağını söyleyen açıklama, e-posta alanı, gönder
  düğmesi ve tek tıkla çıkılabileceğini söyleyen not.
- **AC-2.** `privacy` o dilde yayındayken nottaki KVKK bağlantısı `/{locale}/{privacy slug}`
  adresine gider.
- **AC-3.** `privacy` o dilde yayında değilken notta bağlantı yoktur ve form abonelik isteği
  göndermeye devam eder.
- **AC-4.** Gönder'e basıldıktan sonra yanıt gelene kadar düğme yükleniyor durumunu gösterir ve
  devre dışıdır; bu sürede ikinci basış ikinci istek göndermez.
- **AC-5.** Geçerli bir adresle gönderimden sonra form yerinde başarı mesajını gösterir: teyit
  bağlantısının gönderildiği ve gelmezse istenmeyen e-posta klasörüne bakılması.
- **AC-6.** `ali@` ve boş adres alan hatasıyla reddedilir; API `400` ve
  `notification.subscription_email_invalid` döner; kayıt oluşmaz ve ileti kuyruğa girmez.
- **AC-7.** 255 karakterlik adres AC-6 gibi reddedilir; 254 karakterlik geçerli adres kabul edilir.
- **AC-8.** Sunucu `500` döndüğünde ya da ağ koptuğunda form teknik ayrıntı içermeyen bir hata ve
  yeniden deneme yolu gösterir; yazılan adres alanda kalır.
- **AC-9.** Aynı IP'den yapılandırılan pencerede eşik kadar geçerli istekten sonraki istek `429` ve
  `notification.subscription_rate_limited` alır; kayıt oluşmaz, ileti kuyruğa girmez ve form hız
  sınırı mesajı gösterir (D9).
- **AC-10.** Biçimi geçersiz istekler hız sınırı bütçesini tüketmez: eşik kadar geçersiz istekten
  sonra gelen geçerli istek kabul edilir (D9).
- **AC-11.** İstek yapan IP hiçbir kalıcı kayıtta ve abonelik yolunun hiçbir log çağrısında yer
  almaz.

### Ayırt edilemez cevap ve kayıt (R7, R13 · D5, D9, D10, D14)

- **AC-12.** Hiç kaydı olmayan, teyit bekleyen, etkin ve çıkmış dört adres için abonelik
  isteğinin cevabı aynı durum kodunu ve bayt bayt aynı gövdeyi taşır.
- **AC-13.** Yeni bir adres için istek tek bir **teyit bekliyor** kaydı oluşturur ve tek bir teyit
  iletisini isteğin dilinde kuyruğa koyar.
- **AC-14.** Etkin bir adres için istek yeni kayıt oluşturmaz, ileti kuyruğa koymaz; kaydın
  durumu, dili ve teyit zamanı değişmez.
- **AC-15.** Teyit bekleyen bir adres için başka bir dilden gelen istek yeni kayıt oluşturmaz,
  kaydın dilini günceller ve yeni dilde bir teyit iletisi kuyruğa koyar.
- **AC-16.** Çıkmış bir adres için istek yeni kayıt oluşturmaz, kaydı **teyit bekliyor**'a
  döndürür ve teyit iletisi kuyruğa koyar; teyit edilmeden gelen dönemde o adrese bülten
  kuyruğa girmez.
- **AC-17.** `Ali@Ornek.com ` (sonda boşlukla) ile `ali@ornek.com` aynı adres sayılır: ikinci
  istek yeni kayıt oluşturmaz (D10).
- **AC-18.** Aynı adres için 24 saat içindeki dördüncü istek, ilk üçüyle aynı cevabı alır ama
  teyit iletisi kuyruğa girmez (D9).
- **AC-19.** Taşıyıcı yanıt vermezken (gönderim askıda) abonelik isteğinin cevabı yine döner;
  cevap gönderimi beklemez.

### Teyit (R2, R14 · D1, D6, D7, D18)

- **AC-20.** Teyit iletisi, abonenin dilindeki teyit sayfasını açan mutlak bir bağlantı ve
  galerinin görünen adını taşır; araç, fiyat ya da başka bir tanıtım içeriği taşımaz.
- **AC-21.** Teyit bağlantısının açılması (sayfanın yüklenmesi) aboneliği etkinleştirmez: kayıt
  **teyit bekliyor** kalır ve sayfa "Aboneliği onayla" düğmesini gösterir.
- **AC-22.** Düğmeye basıldığında kayıt **etkin** olur, teyit zamanı UTC olarak yazılır ve sayfa
  başarı mesajını gösterir.
- **AC-23.** Teyit bekleyen abone, dönem anında yeni ilan varken bülten almaz.
- **AC-24.** Yapılandırılan süre dolduktan 1 dakika sonra kullanılan bağlantı aboneliği
  etkinleştirmez ve sayfa "bağlantı artık geçerli değil" mesajıyla yeniden abone olma yolunu
  gösterir.
- **AC-25.** Süre dolmadan 1 dakika önce kullanılan bağlantı aboneliği etkinleştirir.
- **AC-26.** Bir kez kullanılmış bağlantı ikinci kez kullanıldığında kaydın durumu ve teyit zamanı
  değişmez ve sayfa AC-24'ün mesajını gösterir.
- **AC-27.** Aynı bağlantıyla **aynı anda** gelen iki teyit isteğinden (istekler bir bariyerle
  eşzamanlı başlatılır) yalnız biri başarılı olur; teyit zamanı bir kez yazılır.
- **AC-28.** Teyit bekleyen adrese yeni teyit iletisi gittikten sonra önceki iletinin bağlantısı
  aboneliği etkinleştirmez.
- **AC-29.** Hiç var olmamış ya da bozuk bir teyit bağlantısının cevabı, süresi dolmuş ve
  kullanılmış bağlantınınkiyle aynı durum kodu ve aynı gövdedir.
- **AC-30.** Teyit sayfası istek sürerken yükleniyor durumunu, sunucu hatasında teknik ayrıntı
  içermeyen bir hata ve yeniden deneme yolunu gösterir.

### Çıkış (R5, R6 · D6, D8)

- **AC-31.** Her bülten iletisi aboneye özgü, mutlak bir çıkış bağlantısı taşır.
- **AC-32.** Çıkış bağlantısı oturum açılmamış bir tarayıcıda açıldığında başka bir düğme ya da
  onay istemeden kaydı **çıkmış** yapar, çıkış zamanını UTC olarak yazar ve sayfa "abonelikten
  çıktınız" mesajını gösterir.
- **AC-33.** Çıkıştan sonraki ilk dönemde, yeni ilan varken o adrese bülten kuyruğa girmez.
- **AC-34.** Çıkmış kayıt silinmez: çıkıştan sonra kayıt **çıkmış** durumunda ve çıkış zamanıyla
  durur.
- **AC-35.** Aynı çıkış bağlantısının ikinci kez açılması aynı mesajı gösterir; durum ve çıkış
  zamanı değişmez.
- **AC-36.** Çıkıp yeniden abone olan ve teyit eden bir adreste, **ilk** aboneliğin bültenindeki
  çıkış bağlantısı aboneliği yine çıkmış yapar.
- **AC-37.** Çıkış sayfası ana sayfadaki bülten bölümüne giden bir yeniden abone olma bağlantısı
  gösterir.
- **AC-38.** Geçersiz bir çıkış bağlantısı hiçbir kaydı değiştirmez ve sayfa "bağlantı artık
  geçerli değil" mesajını gösterir.
- **AC-39.** Çıkış sayfası istek sürerken yükleniyor durumunu, sunucu hatasında teknik ayrıntı
  içermeyen bir hata ve yeniden deneme yolunu gösterir.

### "Yeni araç" ve dönem (R8, R9, R15 · D4, D12, D13)

- **AC-40.** Dönem anında, önceki dönemin kesiminden sonra ilk kez yayına alınmış ve hâlâ
  **Yayında** olan her ilan, her etkin abonenin bülteninde tam bir kez yer alır.
- **AC-41.** İlk yayını önceki bir döneme ait olan, bu dönemde yayından kaldırılıp yeniden
  yayınlanan ilan bültende yer almaz; aynı akış saat dondurulmadan (ilk yayın ile olay zamanı
  farklıyken) yürütüldüğünde de, bu dönemde **ilk kez** yayınlanan ilan bültende yer alır.
- **AC-42.** Bu dönemde ilk kez yayınlanan, sonra kaldırılıp yeniden yayınlanan ilan bültende bir
  kez yer alır.
- **AC-43.** Bu dönemde ilk kez yayınlanıp dönem anından önce **satılan**, **arşivlenen**,
  **yayından kaldırılan** ya da **rezerve edilen** ilan bültende yer almaz (dört ayrı test).
- **AC-44.** Dönemde aday ilan yokken hiçbir aboneye ileti kuyruğa girmez ve gönderim geçmişinde
  o dönem için kayıt oluşmaz.
- **AC-45.** Dönemdeki tek aday dönem anından önce satılmışsa da (aday var, gönderilecek yok)
  hiçbir aboneye ileti kuyruğa girmez.
- **AC-46.** Dönem işleme iki kez tetiklendiğinde (örn. işlem ortasında yeniden başlatma) bir
  abone o dönem için en çok bir bülten kaydı alır.
- **AC-47.** Uygulama iki dönem anı boyunca kapalı kaldıysa açılışta her etkin aboneye tek bir
  bülten gider ve iki dönemin adaylarını birlikte taşır.
- **AC-48.** Kartın devreye girmesinden önce yayınlanmış ilanlar ilk bültende yer almaz.
- **AC-49.** Aynı `VehiclePublished` olayı iki kez teslim edildiğinde ilan bültende bir kez yer
  alır.

### Bülten içeriği (R3, R16, R17 · D11, D14, D18)

- **AC-50.** Bültendeki her araç satırı model adını abonenin dilinde, model yılını, km'yi, fiyatı,
  şube adını ve ilanın abonenin dilindeki mutlak bağlantısını taşır.
- **AC-51.** Fiyatı "sorunuz" olan ilanın satırı tutar yerine "Fiyat sorunuz" karşılığını taşır.
- **AC-52.** KWD kurulumunda `12500.750` tutarlı ilanın fiyatı üç ondalık basamakla ve para
  birimiyle yazılır; son basamak (`0`) düşmez.
- **AC-53.** 23 aday ilanlı bir dönemde bülten ilk yayını en yeni 20 ilanı sırayla, "ve 3 araç
  daha" ifadesini abonenin dilinin çoğul kuralıyla ve vitrin listesinin bağlantısını taşır.
- **AC-54.** Bülten galerinin abonenin dilindeki görünen adını taşır.
- **AC-55.** Arapça abone olunmuş adrese teyit iletisi ve bülten Arapça şablonla gider.
- **AC-56.** Abonenin dilinde şablon yoksa ileti varsayılan dilde gider ve düşüş loglanır.
- **AC-57.** Abonenin dili kurulumun etkin dilleri arasından çıkarıldıysa bülten varsayılan dilde
  gider.

### Kuyruk ve gönderim (R10)

- **AC-58.** Bülten iletisinde geçici hata yapılandırılan sayıda, artan aralıkla yeniden denenir;
  kalıcı hata yeniden denenmez.
- **AC-59.** Bir abonenin iletisinin başarısız olması aynı dönemdeki diğer abonelerin iletilerini
  engellemez.
- **AC-60.** `0009`'un kabul kriterleri regresyon olarak yeşildir.

### Kişisel veri (C5)

- **AC-61.** Teyit ve bülten iletilerinin gönderim geçmişi kaydı alıcının e-posta adresini hiçbir
  alanda taşımaz.
- **AC-62.** Abonelik, teyit, çıkış, dönem ve gönderim yolundaki log çağrıları e-posta adresini
  parametre olarak almaz — çağrı yüzeyini tarayan testle (`0009` AC-25 deseni).
- **AC-63.** Teyit ve çıkış bağlantıları adresi düz, URL kodlu ya da Base64 olarak içermez.
- **AC-64.** Abonelik, teyit ve çıkış uçlarının hiçbir hata yanıtı e-posta adresini içermez.

### Dil ve RTL (R12)

- **AC-65.** Bülten bölümü, teyit sayfası ve çıkış sayfasının metinleri TR/AR/EN vitrin
  kataloglarında vardır; eksik anahtar kontrolü (`check:locales`) yeşildir.
- **AC-66.** Arapça ana sayfadaki bülten bölümü ile Arapça teyit ve çıkış sayfaları sağdan sola
  düzendedir; metnin hizası Range ölçümüyle sağdır.
- **AC-67.** Teyit iletisi ve bülten şablonları TR, AR ve EN üçünde de seed edilmiştir; eksik
  şablon kontrolü backend testiyle yeşildir (`0009` C13).

### Yapılandırma ve sınır (C10, C12, C14 · D13)

- **AC-68.** Dönem günü/saati, teyit bağlantısı süresi ya da hız sınırı eşiklerinden biri
  yapılandırmada yokken uygulama ayağa kalkmaz.
- **AC-69.** Dönem anı, teyit süresi ve hız sınırını doğrulayan testler yapılandırılan değeri
  kullanır, koda gömülü bir sabiti değil.
- **AC-70.** Yeni migration'lar yalnız `notifications` şemasına dokunur; mimari testler (modül
  sınırı, `Notifications`'ın olay yayınlamaması) yeşildir.

### Performans (C13 · D17)

- **AC-71.** PR'ın C3 koşusunda ana sayfa, liste ve ilan detayı için medyan LCP < 2500 ms ve medyan
  CLS < 0,1'dir; bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşunun sonucu ve LCP öğesi PR'da
  yazılıdır.
- **AC-72.** Vitrin ilk paketinin gzip boyutu `main` tabanına göre en çok 3 kB artar; iki sayı PR'da
  yazılıdır.

---

## Definition of Done

- [ ] S3 ve S4 önerileri (**D4**, **D5**) ile **D6–D20** Takım Yöneticisi tarafından onaylandı;
      sahip kararları **D1–D3** işlendi.
- [ ] **SS1–SS5** sahibe iletildi; cevaplar ya bu karta ya da açılan ayrı karta işlendi.
- [x] `0012b`, `0017` ve `0020` tamamlandı (2026-11-19 ölçüldü — Context).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0018-ac<n>.png`),
      yükleniyor, hata, hız sınırı ve geçersiz bağlantı durumları dâhil; Arapça görüntü de var.
- [ ] Teyit edilmemiş adrese bülten gönderilmediği testle gösterildi (R2 · AC-23).
- [ ] Çıkış bağlantısının **tek adımda** ve oturumsuz çalıştığı testle gösterildi (R5 · AC-32).
- [ ] Kişisel verinin logda ve gönderim geçmişinde bulunmadığı testle gösterildi (C5 · AC-61–64).
- [ ] `0009`'un kabul kriterleri regresyon olarak hâlâ yeşil (R10 · AC-60).
- [ ] Şablonlar TR/AR/EN üçünde de seed edildi; eksik şablon kontrolü yeşil (C6 · AC-67).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `notifications` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimleri (**D19**) `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (**D20**).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4); C3 eşikleri dâhil (AC-71).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı; taşımadan sonra `tests/Architecture.Tests`'in
      tamamı koşturuldu.

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
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0009`'u bozup bozmadığı burada görünür. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
