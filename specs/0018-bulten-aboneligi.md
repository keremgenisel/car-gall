# 0018 — Bülten Aboneliği

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-19 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (B1–B20 → **D4**, **D9–D11**, **D13**, **D14**, **D16**, **D17**, **D20** yeniden yazıldı · **D21–D27** eklendi) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Notifications` (abonelik kaydı, izin olay kaydı, aday ve dönem kayıtları, teyit, çıkış, şablonlar, migration) · `Content.Contracts` (yalnız ayrılmış ad listesine `newsletter` — **D26**) · `VehicleCatalog.Contracts` (ilan özeti okuması — **D11**) · `frontend/` public vitrin (ana sayfadaki bülten bölümü, teyit ve çıkış sayfaları) · `docs/domain.md` (sözlük önerisi — **D19**) |
| **Bağımlı spec'ler** | `0009` (bildirim altyapısı — tamamlandı; **X2**'sini bu kart kapatır) · `0012b` (yayın olayları — tamamlandı; **D31**'in formülü bu kartta kullanılmaz, → **D4**) · `0017` (KVKK sayfası — tamamlandı) · `0020` (galeri adı — tamamlandı) · `0014` (ana sayfa — tamamlandı; **X2**'si bu kart) · `0019` (izin metni sürümü — onaylandı, `56fe70f`; ilişki **X12**) |
| **Dayanak** | ADR 0003 Ek 1, sıra #13 — **#8 (`0004`) ve #12 (`frontend-003`) başlanmadan öne alındı**: sahip kararı, 2026-11-19 (`0019` ile birlikte istendi; ADR 0003 § Sapma — `0018`) · depo sahibinin 2026-11-19 kararları (**D1–D3**) |
| **Karar bekliyor** | **Takım Yöneticisi:** yok — **D4–D20** onaylandı ve QA CLARIFY turu **D21–D27** ile işlendi (Takım Yöneticisi (ajan) kararı, 2026-11-19). **Sahip:** § Sahibe sorular **SS1–SS5** (yasal; kriterleri değiştirmez, canlıya çıkışı etkileyebilir). |

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
- **R8.** Bülten, henüz hiçbir bültende yer almamış, **ilk kez yayına alınmış** ve hem dönem
  işlenirken hem iletinin dağıtıldığı anda hâlâ **Yayında** olan ilanların özetini taşır.
  Daha önce yayınlanmış bir ilanın yeniden yayına alınması "yeni araç" değildir; yayın
  duyurusunun geç işlenmesi bir ilanı bültenden düşürmez (**D4**, **D12**, **D21**).
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
- **R19.** Abonelikle ilgili her olay — istek, teyit, çıkış — zamanıyla birlikte **silinmeyen
  ve değiştirilmeyen** bir kayda geçer; aynı adresin ikinci aboneliği ilkinin geçmişini
  silmez (**D27**).
- **R20.** Çıkmış bir aboneye, çıkıştan önce sıraya girmiş bir bülten de gitmez (**D22**).
- **R21.** Teyit ve çıkış bağlantıları tahmin edilemez; arama motorlarına açılmaz ve başka
  sitelere sızmaz (**D25**, **D26**).

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
  gönderim anında kaynaktan okunur. Teyit ve çıkış bağlantıları adresi içermez; bağlantı
  anahtarları da loga yazılmaz (**D25**). → `0009` **C5**, `0005` **C17**.
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
  o da olay yayınlamaz. Başka modülden okuma tek atlama ve salt okumadır; okunan yüzeyin
  arkası başka bir modüle uzanmaz — `Notifications → VehicleCatalog → VehicleReference`
  zinciri **yasaktır** (**D11**). → `0009` **C4**, **C11** · Yasak Listesi 13.
- **C13.** **Vitrin performans kapısı (C3 eşikleri) kırmızıya dönmez:** ana sayfa, liste ve
  ilan detayı için Lighthouse mobil medyan LCP < 2,5 sn, CLS < 0,1. Bülten metinleri vitrin
  çeviri kümesine girer ve ilk paketi büyütür; büyüme **D17**'nin bütçesindedir.
  → ADR `0013`, `docs/frontend.md` § 5.
- **C14.** Zorunlu yapılandırma eksikse uygulama ayağa kalkmaz; koda gömülü bir varsayılan
  üretilmez. → `0009` **C9** deseni (**D13**, **D25**).
- **C15.** Yeni bir dış bağımlılık (ICU MessageFormat kütüphanesi dâhil) eklenmez; bülten
  metni (anahtar × locale) şablonlarıyla, sayı ve para biçimi .NET `CultureInfo` ile kurulur
  (**D11**, **D24**).

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
  bir entegrasyonun dayanacağı kaydı — izin olay kaydı (**D27**): istek, teyit ve çıkış
  zamanları — üretir.
- **X9.** **Bülten bölümü metinlerinin panelden düzenlenmesi** — *neden:* `0022` ana sayfanın
  panelden düzenlenen altı alanını sabitledi; bülten metinleri çeviri kataloğundadır (**D16**).
- **X10.** **Gönderim sıklığının panelden değiştirilmesi** — *neden:* sahip kararı (**D2**).
- **X11.** **Kalıcı teslim hatasında (adres reddedildi) aboneliğin otomatik kapanması ve
  sağlayıcı şikâyet geri bildirimi** — *neden:* `0009` kalıcı hatayı **mesaj** başına işler;
  hatayı abonelik durumuna bağlamak sağlayıcıya özgü geri bildirim kanalı ister ve ayrı bir
  karardır. Kalıcı hata yine `0009`'daki gibi tekrar denenmez.
- **X12.** **Bülten izni anındaki aydınlatma metni sürümünün kaydı** — *neden:* `0019` bunu
  talep izni için ele alıyor (onaylandı, `56fe70f`) ve bülten iznini kapsamıyor; bülten için gerekip gerekmediği **SS5**.
- **X13.** **E-posta başlığında istemcinin "abonelikten çık" düğmesi** (List-Unsubscribe) —
  *neden:* vaat edilen tek adımlı çıkış gövdedeki bağlantıyla karşılanır; başlık bir teslim
  edilebilirlik iyileştirmesidir ve ayrı karar ister.
- **X14.** **Görselli / HTML bülten** — *neden:* `0009` **X6** şablonları düz metin tuttu;
  marka tasarımı ve istemci uyumluluğu ayrı bir iş.
- **X15.** **Teyit hatırlatma iletisi** — *neden:* teyit etmeyene ikinci kez yazmak, izni
  olmayana yazmaktır.
- **X16.** **Abonelik cevabının süresinin ölçülmesi** (`0002` C13'ün "süre farkı" bileşeni) —
  *neden:* zamanlama farkını güvenilir ölçen bir test CI'da gürültülüdür. Tasarım kuralı
  yine geçerli (**D5**): cevap gönderimi beklemez ve dört durumun hepsi aynı yazma yolundan
  geçer; kriter yalnız durum kodu ve gövdeyi ölçer (AC-16).

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
  (`Showcase/ShowcaseListingContracts.cs`). R16'nın içeriği için bu yüzeyde bir eksik var;
  okumanın biçimi **D11**'de karara bağlandı (model adı değil model kimliği döner).
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
- **Altyapı yarı hazır:** `0009` tamamlandı — kuyruk, arka plan servisi, artan aralıkla
  tekrar deneme, geçici/kalıcı hata ayrımı, şablon (anahtar × locale) ve varsayılan dile
  düşme var; `NotificationsDbContext` idempotent tüketim altyapısını taşıyor. **Bülten için
  eksik olanlar** (QA CLARIFY turu, 2026-11-19 — koddan okundu):
  - `TemplateRenderer` düz yer tutucu değiştirmesi yapıyor; döngü ve ICU yok. Yorumu
    (`Application/TemplateRenderer.cs:8-11`) sayı ya da çoğul içeren şablonun ICU
    gerektireceğini söylüyor → **D11** çoğul cümleyi hiç kurmuyor.
  - Backend'de para, km ve yıl biçimlendiricisi yok; biçim bugün yalnız uçta
    (`frontend/src/shared/i18n/format.ts`: `formatMoney`, `formatMileage`, `formatYear`,
    `Intl.NumberFormat`) → **D24**.
  - Vitrin özetindeki model adı `VehicleReference.Contracts`'tan çözülüyor
    (`VehicleCatalog/Infrastructure/VehicleModelNameResolver.cs:3,12`). `VehicleCatalog`'a
    açılacak bir özet okuması bu çözücüyü kullanırsa zincir iki atlama olur → **D11**.
  - Zincir bekçisi (`tests/Architecture.Tests/BildirimSinirTests.cs:180-190`) tüketilen
    arayüzleri **sabit üç adla** tarıyor; yorumu "liste kendiliğinden değişir" dese de
    yeni bir arayüz eklenince değişmez ve kapı yeşil kalır → AC-83.
  - `NotificationsOptions.IsSendableAddress`'in dayandığı `MailboxAddress.TryParse`
    (QA ölçümü, MimeKit 4.18.1) `ali`, `ali@ornek`, `Ali <ali@ornek.com>` biçimlerini
    **kabul** ediyor, yalnız `ali@`'yi reddediyor → **D10**.
  - `ContentReservedSlugs.All` yalnız `cars|admin|api|assets` taşıyor
    (`Content.Contracts/Pages/ContentPageContracts.cs:50`); tek parçalı yeni bir vitrin yolu
    içerik slug'larıyla çakışır → **D26**.
  - Sır üretim deseni: `Identity__TwoFactor__EncryptionKey` — `dev-up.sh` `openssl rand
    -base64 32` ile üretir, `docs/deploy.md` zorunlu ortam değişkeni olarak listeler → **D25**.
  - `0005` iki hız sınırı taşıyor: kayıt sayan `CapturedLeadLimit` (5) ve kaba
    `RequestLimit` (30), ikisi de 10 dakikalık pencerede (`Host/Program.cs:118-128`); dil
    istek gövdesinde gelir, etkin olmayan dil `lead.invalid_locale` ile reddedilir → **D9**,
    **D14**.

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
   sayar (yukarıdaki ölçüm). Sahte saatli test bunu göstermez (AC-58).
9. **Tek kullanımlık bağlantı ve eşzamanlılık.** Ardışık test ikinci kullanımı yakalar ama
   **aynı anda** gelen iki isteği yakalamaz; eşzamanlı test bir bariyerle kurulmazsa istekler
   fiilen ardışık koşar (AC-33, AC-70).
10. **Teyit iletisiyle bir adresi bombalamak.** IP başına hız sınırı, IP değiştiren birinin
    aynı kurbana yüzlerce teyit iletisi göndermesini engellemez (**D9**).
11. **Gecikmeli duyuru.** Aday `Notifications`'a outbox'la, gecikmeli gelir. Pencereyi
    `PublishedAt`'a bağlamak, kesimden hemen önce yayınlanıp duyurusu kesimden sonra işlenen
    ilanı **hiçbir** bültene sokmaz ve bunu hiçbir şey göstermez (QA B1 → **D4**).
12. **"Gönderim anı" iki şey.** Dönemin işlenmesi ile iletinin dağıtılması saatler
    ayrışabilir (tekrar deneme). İçerik dağıtımda baştan hesaplanırsa aynı dönemin iletileri
    farklı listeler taşır ve satışlar boş bülten doğurabilir (QA B3 → **D21**).
13. **Çıkış ile gönderim arasındaki pencere.** Dağıtıcı aboneliğin durumunu iletiyi kurarken
    okur (**D22**); okuma ile SMTP teslimi arasında — en çok yapılandırılan gönderim zaman aşımı
    (`SendTimeout`) kadar — gelen çıkış o iletiyi durdurmaz ve tek bir bülten çıkıştan hemen
    sonra ulaşabilir. Davranış bilerek değiştirilmedi (QA BULGU-2, PR #122 —
    Takım Yöneticisi (ajan) kararı, 2026-11-19): pencereyi kapatmak gönderimi aboneliğin
    kilidi altında yapmak, yani SMTP beklemesini veritabanı kilidine bağlamak demekti.

### Verilen kararlar — sahip, 2026-11-19

**D1 — Çift onay (S1).** *Sahip kararı, 2026-11-19.* Adres bırakıldığında bir teyit iletisi
gider; bağlantıya tıklanmadan abonelik etkinleşmez ve bülten gitmez. → R2, R14.
*Bedel:* teyit oranı %100 olmadığı için abone sayısı düşer; akış bir adım uzar.

**D2 — Haftalık, dağıtım yapılandırmasından (S2).** *Sahip kararı, 2026-11-19.* Sıklık haftalık;
gün ve saat yapılandırmadan gelir, panelden düzenlenmez. Yeni araç yoksa o hafta gitmez (R9).
→ R15, X10.

**D3 — Talep izni bülten için kullanılmaz (S5).** *Sahip kararı, 2026-11-19.* Talep formuna
bülten kutusu bu kartın kapsamı dışında, ayrı karttır. → R18, C4, X7.

### Verilen kararlar — Takım Yöneticisi (ajan), 2026-11-19

**D4–D20** Analistin önerisiydi ve onaylandı. QA CLARIFY turunun yirmi bulgusu (B1–B20) aynı gün,
QA'nin önerdiği biçimde ve aşağıdaki Takım Yöneticisi seçimleriyle işlendi. Bulgunun metni
değiştirdiği kararlarda `(Bn)` etiketi duruyor; eşleme:
B1 → **D4**, **D13** · B2 → **D11**, **D20**, AC-83 · B3 → **D21** · B4 → **D22** ·
B5 → **D23** · B6 → **D11**, **D24** · B7 → **D10** · B8 → **D25** · B9 → AC-25 ·
B10 → **D26** · B11 → **D27** · B12 → **D13** · B13 → **D9** · B14 → **D9**, **D14** ·
B15 → **D5**, X16 · B16 → AC-57/58 · B17 → **D16** · B18 → **D17** · B19 → **D20** ·
B20 → Dayanak satırı ve ADR 0003 § Sapma — `0018`.

**D4 — "Yeni araç": `notifications`'ta tekil aday kaydı (S3; B1).** Kaynak `VehiclePublished`
olayıdır, ama pencere olayın zamanına **bağlanmaz**:
- Her olay `ListingId` üzerinde **tekil** bir **aday** kaydına dönüşür. Aynı ilanın ikinci
  olayı (yeniden yayın, tekrar teslim) yeni aday oluşturmaz; tekil kayıt düşürür.
- Yalnız `PublishedAt ≥ devreye giriş anı` (**D13**) olan olay aday olur. Kart öncesi
  yayınlanmış bir ilanın yeniden yayını bu yüzden hiç aday olmaz.
- Bir dönem, **henüz bir döneme atanmamış** bütün adayları alır (**D21**). Dönem kesiminden
  önce yayınlanıp olayı kesimden sonra işlenen ilan, bir sonraki dönemde bir kez yer alır.
- `0012b` **D31**'in `OccurredAt > FirstPublishedAt` formülü **kullanılmaz** (Context: ilk
  yayını da yeniden yayın sayıyor); D31'in Bedel cümlesi bu tanımla geçersizleşir.
*Gerekçe:* Tuzak 11. Olay zamanına bağlı pencere, gecikmeli işlenen bir duyuruyu sessizce
kaybeder: outbox tekrar denemesi, açılışta birikmiş outbox ve çok örnekli çalışma aynı kayba
yol açar. "Atanmamış aday" tanımında geç gelen aday bir sonraki işlenen döneme kadar bekler,
kaybolmaz; o dönemde Yayında değilse elenir (**D21**).
*Bedel:* `notifications` şemasında yeni bir tablo (ilan kimliği ve ilk yayın zamanı — kişisel
veri değil). Kart öncesi yayınlanan ilanlar hiçbir bültene girmez (**D13**).
*Elenen alternatif:* yalnız `PublishedAt` ile `VehicleCatalog`'u sorgulamak — `VehicleCatalog`'a
tarih filtreli yeni bir okuma açar ve "yeni"nin ikinci bir tanımını doğurur.

**D5 — Her durumda aynı cevap (S4).** Adres hiç yokken, teyit beklerken, etkinken ve
çıkmışken abonelik isteğinin cevabı **aynı durum kodu ve aynı gövdedir**: "adresinizi aldık,
teyit bağlantısı gönderdik; gelmezse istenmeyen e-posta klasörüne bakın." Arkadaki davranış:
- yeni adres → kayıt **teyit bekliyor**, teyit iletisi kuyruğa girer;
- teyit bekleyen → yeni kayıt yok, yeni teyit iletisi (önceki bağlantı geçersiz — **D7**);
- etkin → hiçbir şey değişmez, ileti gitmez;
- çıkmış → aynı kayıt **teyit bekliyor**'a döner, teyit iletisi gider; teyit edilene dek
  bülten gitmez.

Cevap teyit iletisinin **gönderilmesini beklemez** (Tuzak 7) ve dört dalın hepsi **aynı yazma
yolundan** geçer (B15). Süre farkı kriterle ölçülmez (X16).
*Doğrulama:* `0002` **C13** ve `0006` **AC-5** (taslak AC-4 diyordu).
*Bedel:* gerçekten yeni abone olan biri teyit gelmezse nedenini bilemez; etkin abone ikinci kez
yazdığında hiçbir şey almaz.

**D6 — Teyit sayfadaki düğmeyle, çıkış bağlantının açılmasıyla.** Teyit bağlantısı bir sayfa
açar; abonelik sayfadaki **tek düğmeye** ("Aboneliği onayla") basılınca etkinleşir. Çıkış
bağlantısı ise **açıldığı anda** çıkarır; ek düğme yoktur.
*Gerekçe:* Tuzak 2. İki yön asimetrik: istenmeyen **teyit**, izni olmayana pazarlama demektir;
istenmeyen **çıkış**, yalnız bir bültenin kaçması. D1'in "bağlantıya tıklanmadan etkinleşmez"
kararıyla çelişmez — bağlantı yine şarttır, üstüne bir düğme eklenir.
*Bedel:* teyit bir tık uzar. Kurumsal tarayıcısı bağlantıları açan bir abone ilk bültende
istemeden çıkabilir; çıkış sayfası yeniden abone olma yolu sunar.
*Elenen:* çıkışta da düğme — R5'in "tek adım" vaadini ve mockup'ın "tek tıkla" sözünü bozar.

**D7 — Teyit bağlantısı: süreli, tek kullanımlık, en son gönderilen geçerli.** Süre dağıtım
yapılandırmasından gelir (öneri: **72 saat**). Bağlantı bir kez kullanılınca geçersizdir;
teyit bekleyen adrese yeni teyit iletisi gidince önceki bağlantı geçersiz olur. Süresi dolmuş,
kullanılmış, geçersizleşmiş ve hiç var olmamış bağlantının cevabı **aynıdır** ("bağlantı artık
geçerli değil" + yeniden abone olma yolu). Anahtarın üretimi ve saklanması **D25**'te.
*Bedel:* bağlantıyı iki kez kullanan kişi ikinci seferde "geçerli değil" görür, oysa aboneliği
etkindir — sayfa metni bunu söyler ("Aboneliğinizi daha önce onayladıysanız bir şey yapmanız
gerekmez").

**D8 — Çıkış bağlantısı süresiz, aboneye bağlı, döndürülmez.** Aynı adresin bütün bültenlerindeki
çıkış bağlantısı aynı abonelik kaydına çıkar; yeniden abone olmak eski bağlantıyı geçersiz
kılmaz. Çıkmış hâldeyken bağlantıyı yeniden açmak yine "çıktınız" der, durum ve çıkış zamanı
değişmez. Bağlantı saklanmaz; abonelik kimliğinden bir sırla üretilir (**D25**).
*Bedel:* sızan bir bağlantı o kişiyi listeden çıkarabilir; zararı bir bültenin kaçmasıdır.

**D9 — Üç sınır: kaynak başına kaba istek, kaynak başına geçerli istek, adres başına teyit
iletisi (B13, B14).** (R13) Hepsinin eşik ve pencereleri dağıtım yapılandırmasından gelir; eksikse
uygulama ayağa kalkmaz (C14, AC-97).
- **Kaba istek:** `0005`'in `RequestLimit` deseni — aynı IP'den pencerede (öneri: 10 dakikada
  30) **her** istek sayılır, geçersizler dâhil; aşan istek `429` +
  `notification.subscription_rate_limited`.
- **Geçerli istek:** aynı IP'den pencerede (öneri: 10 dakikada 5) **geçerli** abonelik isteği;
  aşan istek aynı `429`. Biçimi geçersiz istek bu bütçeyi tüketmez (`0005` C17 notu).
- **Adres:** aynı adrese pencerede (öneri: 24 saatte **3**) teyit iletisi. Aşıldığında cevap
  **yine aynıdır** (D5), ileti gitmez ve **kayıtta hiçbir şey değişmez** — teyit anahtarı,
  dil, durum ve izin olay kaydı (**D27**) olduğu gibi kalır. Son gönderilen bağlantı geçerli
  kalmaya devam eder.

IP yalnız geçici sayaçta tutulur; kalıcı kayda ve loga yazılmaz.
*Gerekçe:* Tuzak 10; adres sınırı ifşa etmeden bombalamayı keser. Sınırdayken anahtarı
yenilemek, ileti gitmediği hâlde geçerli tek bağlantıyı sessizce öldürürdü.
*Bedel:* dördüncü kez deneyen dürüst ziyaretçi teyit alamaz ve nedenini görmez.

**D10 — Adresin biçimi ve kimliği (B7).** Girdi ayrıştırılır ve **ayrıştırılmış adres**
(MimeKit `MailboxAddress.Address`) kullanılır; ham metin hiçbir yerde kimlik olmaz.
- Reddedilen biçimler (`422` + `notification.subscription_email_invalid`): boş; 254
  karakterden uzun; **görünen adlı** biçim (`Ali <ali@ornek.com>`, `"x y" <ali@ornek.com>`);
  **alan adsız** ya da alan adında nokta olmayan biçim (`ali`, `ali@ornek`); `ali@`.
- Baştaki ve sondaki boşluk atılır. Adresler büyük/küçük harf farkı gözetmeden **aynı adres**
  sayılır ve bu, büyük/küçük harf duyarsız **tekil** bir kuralla saklamada da korunur: aynı
  yeni adrese eşzamanlı iki istek tek kayıt üretir.
*Gerekçe:* QA ölçümü — `MailboxAddress.TryParse` alan adsız ve görünen adlı biçimleri kabul
ediyor. Ham metne göre normalleştirme `Ali <ali@ornek.com>` ile `ali@ornek.com`'u iki kayıt
yapar ve R6/R7 delinir.
*Bedel:* yerel kısmı harf duyarlı nadir posta sunucularında ayrı iki adres tek sayılır; tek
etiketli alan adı (yerel ağ adresi) kabul edilmez.

**D11 — Bülten içeriği ve kaynakları (B2, B6).**
- **Satır:** **model adı** (abonenin dilinde), **model yılı**, **km**, **fiyat** (ya da "Fiyat
  sorunuz"), **şube adı**, ilanın abonenin dilindeki **mutlak bağlantısı**. En çok **20** satır,
  ilk yayını en yeni olan önce. 20'den fazla ilan varsa sayı **taşımayan** bir cümle eklenir —
  "Diğer yeni araçlar için vitrine göz atın" — ve vitrin listesinin bağlantısı. Çoğul kuralı
  gerekmez; `0009` **C7**'nin ICU şartı yalnız sayı ya da çoğul içeren cümle için geçerli ve
  böyle bir cümle yok (biçimlendirilmiş değerler cümle değil, yer tutucu).
- **Şablonlar:** bülten **başlık**, **satır** ve **altlık** olarak ayrı (anahtar × locale)
  şablonlardır; satır şablonu kendi anahtarındadır. "Fiyat sorunuz" ve "Diğer yeni araçlar…"
  metinleri de şablon kataloğundadır (C8). Düz metin, görsel yok (X14). Yeni ICU bağımlılığı
  **yok** (C15).
- **Okuma — tek atlama:** `VehicleCatalog.Contracts`'a açılacak ilan özeti okuması model
  **adı** değil model **kimliği** döner; `Notifications` adı `VehicleReference.Contracts`'tan
  kendisi çözer. Şube adı `Dealership.Contracts`'tan, galeri adı `IGalleryDisplayNameQueries`'ten.
  Her okuma tek atlamadır (C12).
*Gerekçe:* B2 — model adını `VehicleCatalog` içinde çözmek `Notifications → VehicleCatalog →
VehicleReference` zinciri kurar (Yasak 13) ve bugünkü kapı bunu görmez (Context). B6 — şablon
motoru döngü ve çoğul bilmiyor; satırı ayrı şablon yapmak motoru değiştirmeden listeyi kurar.
*Bedel:* `Notifications` dört modülün yüzeyini okur; "kaç araç daha" bilgisi iletide yok.

**D12 — Rezerve, satılmış, arşivlenmiş ve yayından kalkmış ilan bültene girmez.** Yalnız
**Yayında** olan ilan — dönem işlenirken de, ileti dağıtılırken de (**D21**).
*Gerekçe:* rezerve araç vitrinde rozetiyle kalır ama ziyaretçiye "yeni geldi" diye duyurulması
yanıltıcıdır.
*Bedel:* dönem işlenirken Yayında olmayan aday **elenir** ve bir daha aday olmaz (**D21**) —
dönem anında geçici olarak Rezerve görünen, sonra vitrine dönen ilan da hiçbir bültene girmez.
Dönem işlendikten sonra rezerve edilen ilan o dönemin iletisinden düşer.

**D13 — Bülten dönemi (B1, B12).** Dönem anı yapılandırmadan gelir: **haftanın günü + saat,
UTC** (C9). Yapılandırma eksikse uygulama ayağa kalkmaz (C14).
- **Devreye giriş anı:** uygulamanın bu kartla ilk açılışında, yoksa yazılan **başlangıç
  dönemi kaydının** zamanı. Sonraki açılışlar bu kaydı değiştirmez. Aday eşiği (**D4**) budur.
- **İşleyici:** dönem işleyicisi `IClock` alan ve testten **doğrudan çağrılabilen** bir
  birimdir (`Notifications.Tests/Integration/KuyrukTests.cs` deseni); zamanlı servis yalnız
  onu tetikler.
- **Dönem anından önce işlem yapılmaz:** işleyici dönem anından önce çağrılırsa hiçbir
  dönem kaydı ve ileti üretmez.
- **Hedef kitle:** dönemin **işlendiği anda** etkin olan aboneler. Kaçırılmış bir dönem
  sonradan işlenirse o anda etkin olanlar alır.
- Aynı dönem iki kez işlenmez; bir aboneye bir dönem için en çok **bir** bülten kaydı oluşur
  (**D23**).
- Uygulama dönem anında kapalıysa açılışta kaçırılan dönem **bir kez** işlenir; birden çok
  kaçırılmış dönem **tek bültende** birleşir.
- Kart öncesi yayınlanan ilanlar hiçbir bültene girmez (Context: duyurular geri gelmez).
*Gerekçe:* "kartın devreye girdiği an" ve "dönem anında etkin" ölçülemiyordu (B12); tanımları
bir kayda ve doğrudan çağrılabilir bir birime bağlamak kriterleri test edilebilir yapar.
*Bedel:* yaz saati uygulanan ülkelerde yerel gönderim saati yılda iki kez bir saat kayar.

**D14 — Dil (B14).** Dil, isteğin gövdesinde `0005`'teki gibi taşınır; kurulumda etkin olmayan
ya da tanınmayan bir dil `422` + `notification.subscription_invalid_locale` ile reddedilir
(`0005`'in `lead.invalid_locale` cevabının karşılığı) ve kayıt oluşmaz. Teyit bekleyen adrese
başka bir dilden yeni istek gelirse dil güncellenir (yeni teyit iletisi o dilde; adres sınırı
aşılmışsa **D9** gereği güncellenmez); **etkin** aboneliğin dili yeni istekle değişmez (D5).
Abonenin dili kurulumda sonradan etkin olmaktan çıkarsa ileti varsayılan dilde gider.
*Bedel:* etkin abone bülten dilini değiştirmek için çıkıp yeniden abone olmak zorunda.

**D15 — KVKK bağlantısı `0017` D5 deseniyle.** `privacy` o dilde yayındaysa form notu ona bağlanır;
değilse bağlantı basılmaz ve form çalışmaya devam eder.
*Gerekçe:* talep formuyla aynı davranış; metin `Content`'te, galerinin elinde.
*Bedel:* aydınlatma metnini yayınlamamış bir galeri bağlantısız izin toplar — bunun kabul
edilebilirliği sahibin sorusu (**SS3**).

**D16 — Bülten bölümü yalnız ana sayfada, `HomeCatalog`'un altında (B17).** Bölüm ana sayfada
`HomeCatalog`'un hemen altında ve altlıktan önce durur. **Vitrin boşken de görünür**: `HomeCatalog`
o durumda yalnız boş stok mesajını basar (`HomeCatalog.tsx:93-99`) ve bülten bölümü onun altında
kalır. Metinler vitrin çeviri kümesindedir ve panelden düzenlenmez (X9).
*Gerekçe:* "yeni araç gelince haber verelim" vaadi tam vitrin boşken anlamlı; "öne çıkanların
altında" çapası boş vitrinde tanımsızdı.
*Bedel:* listeye ya da ilana doğrudan gelen ziyaretçi bülteni görmez.

**D17 — Performans bütçesi (B18).** Ölçüm ADR `0013`'ün protokolüyle yapılır: `vite build`
çıktısının **giriş JS'i + giriş CSS'i** (ana sayfa ilk yüklemede inen, `index.html`'in
başvurduğu dosyalar), her biri `gzip -9` ile sıkıştırılıp toplanır. Toplam `main` tabanına göre
**en çok 3072 B** artar; iki sayı ve fark PR'da yazılır. C3 medyanı bir sayfada **2450 ms**'yi
aşarsa ikinci bir koşu istenir ve LCP öğesi yazılır.
*Gerekçe:* Context'teki ölçüm: tek koşu içi yayılım 157 ms, detay sayfasının payı 95 ms.
*Bedel:* bütçe, teyit ve çıkış sayfalarının ilk pakete girmemesini fiilen zorunlu kılar.

**D18 — Galeri adı.** Teyit iletisi ve bülten, galerinin abonenin dilindeki görünen adını taşır
(`0020`); o dilde ad yoksa `0020`'nin düşüş kuralı geçerlidir.
*Gerekçe:* alıcı iletinin kimden geldiğini bilmeli (R17); yasal kimlik bilgisi ayrı bir soru
(**SS2**).

**D19 — Sözlük** (`docs/domain.md` § Bildirim): **Bülten** (haftalık yeni araç özeti —
pazarlama iletisi) · **Bülten Aboneliği** (adres + dil + durum) · **Abonelik Durumu**
(`Teyit bekliyor` · `Etkin` · `Çıkmış`) · **Teyit Bağlantısı** · **Çıkış Bağlantısı** ·
**Bülten Dönemi** (bir haftalık pencere ve kesim anı) · **Bülten Adayı** (henüz bir döneme
atanmamış, ilk kez yayınlanmış ilan — **D4**) · **İzin Olayı** (**D27**).

**D20 — ADR gerekmez; `docs/architecture.md` güncellenir (B2, B19).** `Notifications` ilk public
ucunu ve ilk zamanlı işini alıyor; yaprak konumu (C12) korunuyor: olay yayınlamıyor, kimse ona
senkron bağlanmıyor. Gönderim kaydının alıcısını talepten bağımsız hâle getirmek modül içi bir
değişiklik. **Koşulsuz** güncellenecekler (DoD T8):
- § Karar Defteri'ne bir satır;
- bağımlılık haritasındaki "Okuduğu üç yüzey" (`docs/architecture.md:124`) yeni okunan
  yüzeylerle (`VehicleCatalog` ilan özeti, `VehicleReference` model adı,
  `IGalleryDisplayNameQueries`);
- olay akışında `VehiclePublished`'ın tüketicisi olarak `Notifications`;
- § 2.A koşul 2'ye ("aynı HTTP isteği", `:170`) zamanlı işten senkron okumanın istisnası —
  `NotificationDispatcher` zaten emsal.

**D21 — İlan kümesi dönem işlenirken dondurulur (B3).** Dönem işlenirken atanmamış adayların
**hepsi** o dönemde karara bağlanır: o an **Yayında** olanlar o döneme atanır ve **dönem ↔ ilan**
kayıtlarına yazılır; Yayında olmayanlar (Taslak, Rezerve, Satıldı, Arşiv) **elendi** olarak
işaretlenir ve bir sonraki döneme kalmaz (**D12**; Takım Yöneticisi (ajan) kararı, 2026-11-19).
Bu kayıtlar kişisel veri taşımaz. İletinin **dağıtımında** küme yeniden hesaplanmaz;
yalnız değişken veri — model adı, fiyat, şube adı ve ilanın **durumu** — kaynaktan okunur.
Dağıtım anında hâlâ **Yayında** olan ilanlar satır olur; hiçbiri kalmadıysa ileti **kalıcı
başarısız** işaretlenir ve gönderilmez (R9). Dönem işlenirken hiç Yayında aday yoksa dönem
kaydı yazılır ama ileti üretilmez.
*Gerekçe:* Tuzak 12. Aynı dönemin iletileri aynı kümeden türer; tekrar denenen ileti saatler
sonra farklı bir liste taşımaz ve satışlar boş bülten doğurmaz. Eleme, satışı geri alınan
(`0023`) ya da aylar sonra yeniden yayına alınan bir ilanın bekleyen adaylığıyla "yeni araç"
diye bültene çıkmasını önler.
*Bedel:* dağıtımda satılan ilan iletiden düşer; ileti dönemdeki ilanlardan daha azını taşıyabilir.
Dönem anında geçici olarak Rezerve görünen ilan da bültene girmez.

**D22 — Dağıtım anında abonelik durumu okunur (B4).** Dağıtıcı her bülten iletisini göndermeden
önce aboneliğin **o anki** durumunu okur; `Çıkmış` (ya da kayıt yok) ise ileti gönderilmez ve
**kalıcı başarısız** işaretlenir — talepteki `lead is null → Permanent` deseni
(`NotificationDispatcher.cs:96`). Adres de bu okumada, kaynaktan alınır (C5).
*Gerekçe:* dönem işlendikten sonra çıkan abonenin tekrar denemede bekleyen iletisi aksi hâlde
gönderilirdi (R6, R20).
*Bedel:* yok denecek kadar küçük: her gönderimde bir okuma.

**D23 — Gönderim kaydının biçimi ve göç (B5).** Gönderim kaydı **ya** bir talebe **ya** bir
abonelik + döneme (bülten) **ya da** bir aboneliğe (teyit iletisi) bağlıdır; birden fazlasına
birden değil. Kural veritabanında bir **CHECK** kısıtıyla korunur. `(abonelik, dönem)` çifti
**tekildir**: aynı dönemin ikinci işlenmesi — ardışık ya da eşzamanlı — ikinci kayıt üretemez.
Migration **önce** kuyrukta duran talep iletileri migration'dan **sonra** da gönderilir.
*Gerekçe:* `LeadId` bugün zorunlu (Context); şema değişikliği kriterde yazılı değildi ve
"tam bir kez" garantisi kod yoluna değil kısıta dayanmalı.
*Bedel:* `0009`'un tek tablolu kaydı üç anlamlı hâle gelir; her okuyucu bağlamı ayırt eder.

**D24 — Sayı ve para biçimi vitrin kartıyla birebir (B6).** Bültendeki fiyat, km ve yıl .NET
`CultureInfo` ile biçimlenir ve aynı değerler için vitrinin `formatMoney`, `formatMileage`,
`formatYear` çıktısıyla (`frontend/src/shared/i18n/format.ts`) **üç dilde de bayt bayt aynıdır** —
Arapça'nın rakam kümesi, gruplama ayracı ve para birimi simgesinin yeri dâhil. Yıl gruplama
almaz; KWD üç ondalık taşır. Ölçüm uyuşmazlık gösterirse geliştirici **ön yüzün çıktısını
otorite sayan** bir biçim kuralı yazar ve bu karta karar olarak eklenir (AP-08) — uyuşmazlık
sessizce kabul edilmez.
*Gerekçe:* ziyaretçi aynı aracı e-postada ve vitrinde iki farklı biçimde görmemeli; ICU
(`Intl`) ile .NET `CultureInfo` aynı CLDR verisine dayansa da sürüm ve varsayılan farkı olabilir.
*Bedel:* ön yüz biçimi değişirse backend testi kırmızıya döner; iki taraf birlikte değişir.

**D25 — Bağlantı anahtarlarının güvenliği (B8).**
- Teyit anahtarı **CSPRNG** ile üretilir, **256 bit**. Veritabanında yalnız **SHA-256 özeti**
  tutulur; düz anahtar yalnız iletide yaşar. *(Üretim yolu **D28** ile değişti: anahtar
  gönderim kaydı kimliğinin HMAC-SHA256'sıdır — 256 bit, sırsız tahmin edilemez; "yalnız özet"
  ve "düz anahtar yalnız iletide" aynen geçerli. Takım Yöneticisi (ajan) kararı, 2026-11-19.)*
- Çıkış bağlantısı **saklanmaz**: abonelik kimliği üzerinden, dağıtım yapılandırmasındaki
  bir sırla **HMAC-SHA256** ile üretilir ve doğrulanır. Sır, diğer sırlar gibi üretilir
  (`dev-up.sh` `openssl rand -base64 32` · `docs/deploy.md` zorunlu ortam değişkeni — ADR 0011
  deseni, `Identity__TwoFactor__EncryptionKey` emsali); **sır yoksa uygulama ayağa kalkmaz**.
- Anahtarlar ve bağlantılar loga ve gönderim geçmişine girmez (AC-90 taraması).
*Gerekçe:* özetli saklama, veritabanı sızsa bile teyit yetkisi vermez; HMAC, D8'in süresiz ve
her bültende yeniden basılan bağlantısını düz saklamadan mümkün kılar.
*Bedel:* **sır değişirse eski bütün çıkış bağlantıları geçersiz olur** — eski bir bültenden
çıkmak isteyen kişi "bağlantı geçerli değil" görür. Sır yedeklenir ve döndürülmez;
döndürme gerekirse ayrı karar. `docs/deploy.md`'ye bu uyarı yazılır.

**D26 — Teyit ve çıkış sayfalarının adresleri (B10).** Yollar
`/{locale}/newsletter/confirm?token=…` ve `/{locale}/newsletter/unsubscribe?token=…`; anahtar
sorgu parametresinde taşınır. `newsletter` ilk segmenti `ContentReservedSlugs.All`'a eklenir —
bu adda içerik sayfası açılamaz. İki sayfa `noindex` taşır ve `Referrer-Policy: no-referrer`
ile sunulur. Backend'in kurduğu mutlak bağlantı (`PublicBaseUrl` + yol) ile ön yüzün rotası bu
iki kalıpla **aynıdır**.
*Gerekçe:* tek parçalı yol `0017`'nin slug'larıyla çakışırdı (Context); anahtar `Referer`
başlığıyla başka bir siteye ya da arama motoruna sızmamalı.
*Bedel:* `newsletter` İngilizce bir segment; TR/AR adreslerde de böyle görünür. Değişikliğe
`Content` modülü dokunur (yalnız ayrılmış ad listesi).

**D27 — İzin olay kaydı (B11).** `notifications` şemasında **yalnız eklenen** bir izin olay
kaydı tutulur: her **istek**, **teyit** ve **çıkış** için olay türü, zamanı (UTC) ve abonelik
kimliği. Adres bu kayıtta **yoktur**; abonelik kimliği üzerinden bağlanır. Kayıt silinmez ve
düzenlenmez. Aboneliğin kendisi yalnız **son** durumu taşır; "çıkmış → teyit bekliyor"
geçişinde geçmiş olay kaydında kalır. Adres sınırına takılan istek (D9) olay yazmaz.
*Gerekçe:* tek kaydın yeniden kullanılması (D5) eski teyit ve çıkış zamanlarını ezerdi; R19 ve
§ Sahibe sorular'daki "kodun garanti ettiği" ancak bu kayıtla doğru olur. Olası bir İYS
entegrasyonu (X8) bu kayda dayanır.
*Bedel:* yeni bir tablo; KVKK silme talebinde (SS3) adresin aboneliği silinse bile olaylar
kimlikle kalır — sahibin cevabı bunu değiştirebilir.

### Verilen kararlar — plan turu, 2026-11-19

**Developer (ajan) plan kararları**, Takım Yöneticisi görevlendirmesi kapsamında (R/C/AC'ye
dokunmaz; QA turunda gözden geçirilir). Her biri, kriterin bıraktığı bir uygulama boşluğunu
kapatır.

**D28 — Teyit anahtarı gönderim kaydından türetilir (deterministik).** *Takım Yöneticisi (ajan)
kararı, 2026-11-19 — ilk yazımın "dağıtımda CSPRNG ile üret" çözümünün bedeli kabul edilmedi.*
Anahtar = **HMAC-SHA256**(imza sırrı, `newsletter-confirm:` ‖ teyit iletisinin gönderim kaydı
kimliği) — 32 bayt, Base64Url. Kuyruk gövdeyi dağıtımda kurar (`0009` C10) ve anahtarı her
denemede aynı kayıttan YENİDEN türetir; veritabanına yazılmaz. Özeti (SHA-256) ve süresi
aboneliğe **kayıt oluşurken** (istek anında) yazılır. *Sonuç:* geçici hatayla yeniden denenen
iletinin bütün teslimleri AYNI bağlantıyı taşır ve hepsi ilk tıklamaya kadar geçerlidir; süre
istekten başlar. **D7**'nin "yalnız en son gönderilen geçerli" kuralı kayıt düzeyindedir: yeni
istek → yeni gönderim kaydı → yeni anahtar; önceki kaydın özeti abonelikten silinir ve o kayıt
henüz dağıtılmadıysa hiç gönderilmez (geçersiz bir bağlantı taşırdı). *Sır seçimi:* çıkış
bağlantısının sırrı (**D25**) ayrı bir amaç önekiyle — iki ayrı sır kurulumda iki zorunlu
değişken ve iki yedek demekti; önek bir türün imzasının öbür türde geçmesini engeller.
*Bedel:* sır değişirse bekleyen teyit bağlantıları da ölür. *Elenen:* düz anahtarı iletide
şifreli saklamak (D25'in "yalnız özet"i); dağıtımda üretmek (ilk teslimin bağlantısını öldürür).

**D29 — Adres sınırı (D9) gönderim kaydından sayılır.** Pencere içindeki teyit iletileri,
aboneliğin `messages` satırlarından sayılır; sayım ve yazma aboneliğin satır kilidi altında
yapılır (eşzamanlı iki istek sınırı birlikte aşamaz). *Gerekçe:* yeni bir sayaç durumu yok
ve sayım süreç yeniden başlasa da kaybolmaz. *Bedel:* her abonelik isteğinde bir sayım
sorgusu.

**D30 — "Yayında" = vitrin okuma modelinde rezerve olmayan satır.** Yeni ilan özeti okuması
(`IListingSummaryQueries`, **D11**) vitrin okuma modelinden okur: model yalnız Yayında ve
Rezerve ilanları taşır ve durum geçişiyle aynı transaction'da yazılır (`0012b` D12). Okuma
`VehicleReference`'a uzanmaz (C12). *Bedel:* yok — vitrinde görünmeyen bir ilan bültende de
görünmez.

**D31 — İletinin dili bir kez seçilir.** Teyit iletisinde teyit şablonu, bültende başlık
şablonu abonenin dilinde (kurulumda etkin değilse varsayılanda — AC-82) çözülür; o dilde yoksa
varsayılana düşülür ve **çözülen dil** satır, altlık ve "Fiyat sorunuz"/"Diğer yeni araçlar…"
parçalarında da kullanılır. *Gerekçe:* parça parça düşüş tek iletide iki dil üretirdi.

**D32 — D24'ün ölçümü ve biçim kuralı (ön yüz otoritedir).** Ölçüldü (2026-11-19; .NET 10
`CultureInfo`, Node 24.22 ICU 78.3 `Intl`): km ve yıl **birebir aynı** (`N0`/`D` ↔
`formatMileage`/`formatYear`: TR `125.000`, AR/EN `125,000`, yıl `2019`). **Para birebir
DEĞİL:** nötr kültürün para simgesi `¤`'dır (`1250000m.ToString("C2", tr)` → `¤1.250.000,00`),
`CultureInfo` para birimini kodla seçemez ve `Intl`'in boşlukları (U+00A0) ile Arapça yön
işaretlerini (U+200F) üretmez. `Intl`: TR `₺1.250.000,00` · `KWD 12.500,750`; AR
`‏1,250,000.00 TRY` · `‏12,500.750 د.ك.‏`; EN `TRY 1,250,000.00` · `KWD 12,500.750`. Bölgeli
kültür (`ar-KW`) ise Arap ayraçları (`12٬500٫750`) üretir. **Kural:** sayı kısmı
`CultureInfo(<dil>).NumberFormat` ile (`N<üs>`; ayraçlar ve rakamlar nötr kültürden), para
ekleri `Intl` çıktısından türetilmiş **(dil × para birimi)** tablosundan; tabloda olmayan
para birimi için dilin genel kalıbı (TR/EN `KOD` + U+00A0 + sayı, AR U+200F + sayı + U+00A0 +
`KOD`). İki taraf aynı fikstür dosyasını ölçer (`frontend/src/shared/i18n/format.fixtures.json`):
Vitest `Intl` çıktısını, xUnit bülten biçimini — ön yüz değişirse önce Vitest, fikstür
güncellenince backend testi kırmızıya döner (**D24**'ün bedeli). *Açık konu (PR):* tablo
TRY ve KWD'yi taşır; başka bir para birimi kurulduğunda ön yüzle aynı simgeyi garanti etmez.
*Takım Yöneticisi (ajan) onayı, 2026-11-19:* kural onaylandı; yeni para birimi ekleyen kart
tabloya satır ve fikstüre vaka ekler.

**D33 — HTTP sözleşmesi.** `POST /api/notifications/newsletter/subscriptions`
(`{ email, locale }`) → `202` + sabit gövde (**D5**); biçim/dil hatası `422` (AC-7, AC-10 —
`docs/conventions.md` § 5; kriter Revizyon 1 ile 400'den 422'ye çekildi). `POST …/confirmations` ve
`POST …/unsubscriptions` (`{ token }`) → `200`; geçersiz bağlantı `404` +
`notification.subscription_link_invalid` (AC-35, AC-44). Çıkış sayfası açılır açılmaz isteği
gönderir (**D6**); teyit sayfası yalnız düğmeyle.

**D34 — İzin olayı her istekte yazılır.** **D27**'nin "her istek" cümlesi etkin aboneliğe
gelen isteği de kapsar (durum değişmez, olay eklenir); adres sınırına takılan istek olay
yazmaz. Kaydın "yalnız eklenir" kuralı veritabanında bir tetikleyiciyle korunur
(`UPDATE`/`DELETE` reddedilir).

**D35 — `noindex` ve `Referrer-Policy` Host'tan.** İki sayfanın belgesi `IndexHtmlRenderer`'dan
geçer; Host `<meta name="robots" content="noindex">` basar ve `Referrer-Policy: no-referrer`
başlığını yazar (**D26**). Uç ayrıca bir şey yapmaz.

**D36 — Ana sayfada KVKK bağlantısı tek okumayla.** Altlığın kurumsal bağlantıları ile bülten
notunun `privacy` bağlantısı aynı okumadan gelir (`HomeRoute`); ana sayfaya ikinci bir
`Content` isteği eklenmez (LCP payı — C13).

**D37 — Teyit ve çıkış sayfaları ayrı (lazy) pakette.** Metinleri vitrin kataloğundadır
(AC-94, ADR `0013`); bileşenleri ilk pakete girmez (**D17**'nin bedeli).

**D38 — Yapılandırma anahtarları.** `Notifications:Newsletter:` altında `SendDayOfWeek`,
`SendTimeUtc` (`HH:mm`), `ConfirmationLinkHours`, `RequestLimit`/`RequestWindowMinutes`,
`ValidRequestLimit`/`ValidRequestWindowMinutes`, `AddressConfirmationLimit`/`AddressWindowHours`
ve sır `UnsubscribeSigningKey` (ortam değişkeni; Base64, en az 32 bayt). Hiçbirinin koda
gömülü varsayılanı yok (C14).

### Uygulama planı

| Adım | İş | Modül | Kriter |
|---|---|---|---|
| **P1** | Plan kararları ve bu tablo | spec | — |
| **P2** | `newsletter` ayrılmış ad | `Content.Contracts` | AC-52 |
| **P3** | İlan özeti okuması (model kimliği döner, tek atlama) | `VehicleCatalog(.Contracts)` | AC-55, AC-74, AC-83 |
| **P4** | Şema: abonelik, izin olayı, aday, dönem, dönem↔ilan; gönderim kaydı CHECK + tekil; şablon seed | `Notifications` | AC-84, AC-85, AC-93, AC-96, AC-99 |
| **P5** | Abone olma, teyit, çıkış; üç hız sınırı; yapılandırma | `Notifications` | AC-7–AC-25, AC-27–AC-35, AC-38–AC-44, AC-47–AC-50, AC-89–AC-93, AC-97, AC-98 |
| **P6** | Aday işleyicisi, dönem işleyicisi, zamanlı servis | `Notifications` | AC-29, AC-39, AC-55–AC-73 |
| **P7** | Dağıtım: teyit ve bülten iletisi, D21/D22, biçim (D24/D32) | `Notifications` | AC-26, AC-37, AC-46, AC-65, AC-66, AC-74–AC-82, AC-86–AC-88 |
| **P8** | Host bağlantısı, `noindex`/`Referrer-Policy`, boru hattı testleri | `Host` | AC-12–AC-15, AC-50, AC-53, AC-54, AC-97 |
| **P9** | Mimari kapılar: zincir (dinamik), log yüzeyi, olay yayınlamama | `Architecture.Tests` | AC-83, AC-90, AC-99 |
| **P10** | Uç: bülten bölümü, teyit ve çıkış sayfaları, katalog | `frontend` | AC-1–AC-6, AC-11, AC-27, AC-36, AC-43–AC-45, AC-51, AC-94 |
| **P11** | e2e + görsel kanıt (Arapça dâhil) | `frontend/e2e` | AC-2, AC-5, AC-6, AC-12, AC-28, AC-38, AC-95 |
| **P12** | Sözlük, mimari, dağıtım, `dev-up.sh`, iş akışları (sır) | `docs`, `scripts`, `ci` | D19, D20, D25 |
| **P13** | Performans ölçümü (gzip toplamı + C3) PR'da | — | AC-100, AC-101 |
| **P14** | Takım Yöneticisi kararları (PR #122): AC-7/AC-10 422 (Revizyon 1), D28 deterministik anahtar, D32 onayı | spec, `Notifications`, `frontend` | AC-7, AC-10, AC-34, AC-48, AC-49 |

---

## Sahibe sorular

Bunlar **karar değildir**; hukuki ya da ticari cevap ister ve Analist karara bağlamaz. Kriterler
bu cevaplardan bağımsız yazıldı; cevaplar canlıya çıkışı etkileyebilir.

**Kodun bu kartla garanti ettiği:** her abonelik için adres, dil ve son durum; her **istek**,
**teyit** ve **çıkış** için zamanıyla (UTC), yalnız eklenen ve silinmeyen bir **izin olay
kaydı** (**D27** — aynı adresin yeniden aboneliği eski olayları ezmez); teyitsiz adrese bülten
gitmemesi; her bültende tek adımlı çıkış bağlantısı; çıkıştan sonra — kuyrukta bekleyen dâhil —
**dağıtım anında okunan duruma göre** gönderim olmaması (**D22**; durum okunduktan sonra
gelen çıkış o gönderimi durdurmaz — Bilinen tuzak 13); çıkış kaydının silinmemesi; adresin ve bağlantı anahtarlarının loga
ve gönderim geçmişine girmemesi. **Garanti etmediği:** dış bir sisteme (İYS) bildirim, saklama
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

### Abone olma formu (R1, R4, R11 · D10, D14, D15, D16)

- **AC-1.** Ana sayfada `HomeCatalog`'un hemen altında ve altlıktan önce bir bülten bölümü vardır:
  başlık, haftalık yeni araç özeti alınacağını söyleyen açıklama, e-posta alanı, gönder düğmesi ve
  tek tıkla çıkılabileceğini söyleyen not.
- **AC-2.** Yayında hiç ilan yokken bülten bölümü yine görünür ve boş stok mesajının altında durur.
- **AC-3.** `privacy` o dilde yayındayken nottaki KVKK bağlantısı `/{locale}/{privacy slug}`
  adresine gider.
- **AC-4.** `privacy` o dilde yayında değilken notta bağlantı yoktur ve form abonelik isteği
  göndermeye devam eder.
- **AC-5.** Gönder'e basıldıktan sonra yanıt gelene kadar düğme yükleniyor durumunu gösterir ve
  devre dışıdır; bu sürede ikinci basış ikinci istek göndermez.
- **AC-6.** Geçerli bir adresle gönderimden sonra form yerinde başarı mesajını gösterir: teyit
  bağlantısının gönderildiği ve gelmezse istenmeyen e-posta klasörüne bakılması.
- **AC-7.** `ali@` ve boş adres alan hatasıyla reddedilir; API `422` ve
  `notification.subscription_email_invalid` döner; kayıt oluşmaz ve ileti kuyruğa girmez.
  *Revizyon 1 (2026-11-19, AP-08): kriter 400 diyordu; depo kuralı 422 (`docs/conventions.md`), `0020` AC-30 emsali — Takım Yöneticisi (ajan) kararı.*
- **AC-8.** `Ali <ali@ornek.com>`, `"x y" <ali@ornek.com>`, `ali` ve `ali@ornek` AC-7 gibi
  reddedilir (D10).
- **AC-9.** 255 karakterlik adres AC-7 gibi reddedilir; 254 karakterlik geçerli adres kabul edilir.
- **AC-10.** Kurulumda etkin olmayan ya da tanınmayan bir dille gelen istek `422` ve
  `notification.subscription_invalid_locale` alır; kayıt oluşmaz (D14).
  *Revizyon 1 (2026-11-19, AP-08): kriter 400 diyordu; depo kuralı 422 (`docs/conventions.md`), `0020` AC-30 emsali — Takım Yöneticisi (ajan) kararı.*
- **AC-11.** Sunucu `500` döndüğünde ya da ağ koptuğunda form teknik ayrıntı içermeyen bir hata ve
  yeniden deneme yolu gösterir; yazılan adres alanda kalır.
- **AC-12.** Aynı IP'den yapılandırılan pencerede geçerli istek eşiği kadar istekten sonraki geçerli
  istek `429` ve `notification.subscription_rate_limited` alır; kayıt oluşmaz, ileti kuyruğa girmez
  ve form hız sınırı mesajı gösterir (D9).
- **AC-13.** Biçimi geçersiz istekler geçerli istek bütçesini tüketmez: geçerli eşik kadar geçersiz
  istekten sonra gelen geçerli istek kabul edilir (D9).
- **AC-14.** Aynı IP'den yapılandırılan pencerede kaba istek eşiği kadar istekten (geçersizler
  dâhil) sonraki istek `429` alır (D9).
- **AC-15.** İstek yapan IP hiçbir kalıcı kayıtta ve abonelik yolunun hiçbir log çağrısında yer
  almaz.

### Ayırt edilemez cevap ve kayıt (R7, R13, R18, R19 · D3, D5, D9, D10, D14, D27)

- **AC-16.** Hiç kaydı olmayan, teyit bekleyen, etkin ve çıkmış dört adres için abonelik
  isteğinin cevabı aynı durum kodunu ve bayt bayt aynı gövdeyi taşır (süre ölçülmez — X16).
- **AC-17.** Yeni bir adres için istek tek bir **teyit bekliyor** kaydı, tek bir **istek** izin
  olayı oluşturur ve tek bir teyit iletisini isteğin dilinde kuyruğa koyar.
- **AC-18.** Etkin bir adres için istek yeni kayıt oluşturmaz, ileti kuyruğa koymaz; kaydın
  durumu, dili ve teyit zamanı değişmez.
- **AC-19.** Teyit bekleyen bir adres için başka bir dilden gelen istek yeni kayıt oluşturmaz,
  kaydın dilini günceller ve yeni dilde bir teyit iletisi kuyruğa koyar.
- **AC-20.** Çıkmış bir adres için istek yeni kayıt oluşturmaz, kaydı **teyit bekliyor**'a
  döndürür ve teyit iletisi kuyruğa koyar; önceki **teyit** ve **çıkış** izin olayları zamanlarıyla
  olay kaydında kalır; teyit edilmeden gelen dönemde o adrese bülten kuyruğa girmez.
- **AC-21.** `Ali@Ornek.com ` (sonda boşlukla) ile `ali@ornek.com` aynı adres sayılır: ikinci
  istek yeni kayıt oluşturmaz (D10).
- **AC-22.** Kaydı olmayan bir adrese, harf büyüklüğü farklı iki istek bir bariyerle **aynı anda**
  başlatıldığında tek abonelik kaydı oluşur (D10).
- **AC-23.** Aynı adres için adres penceresindeki dördüncü istek ilk üçüyle aynı cevabı alır;
  teyit iletisi kuyruğa girmez ve kayıtta hiçbir şey değişmez — teyit anahtarı özeti, dil, durum ve
  izin olayları aynı kalır; üçüncü iletinin bağlantısı aboneliği hâlâ etkinleştirir (D9).
- **AC-24.** Taşıyıcı yanıt vermezken (gönderim askıda) abonelik isteğinin cevabı yine döner;
  cevap gönderimi beklemez.
- **AC-25.** Talep formunda iletişim izni vererek e-postasıyla talep bırakan bir ziyaretçinin
  adresi, abone olmadıkça bülten almaz; `LeadCaptured` hiçbir abonelik kaydı oluşturmaz (D3).

### Teyit (R2, R14 · D1, D6, D7, D18, D27)

- **AC-26.** Teyit iletisi, abonenin dilindeki teyit sayfasını açan mutlak bir bağlantı ve
  galerinin görünen adını taşır; araç, fiyat ya da başka bir tanıtım içeriği taşımaz.
- **AC-27.** Teyit bağlantısının açılması (sayfanın yüklenmesi) aboneliği etkinleştirmez: kayıt
  **teyit bekliyor** kalır ve sayfa "Aboneliği onayla" düğmesini gösterir.
- **AC-28.** Düğmeye basıldığında kayıt **etkin** olur, teyit zamanı UTC olarak yazılır, bir
  **teyit** izin olayı eklenir ve sayfa başarı mesajını gösterir.
- **AC-29.** Teyit bekleyen abone, dönem işlenirken Yayında aday varken bülten almaz.
- **AC-30.** Yapılandırılan süre dolduktan 1 dakika sonra kullanılan bağlantı aboneliği
  etkinleştirmez ve sayfa "bağlantı artık geçerli değil" mesajıyla yeniden abone olma yolunu
  gösterir.
- **AC-31.** Süre dolmadan 1 dakika önce kullanılan bağlantı aboneliği etkinleştirir.
- **AC-32.** Bir kez kullanılmış bağlantı ikinci kez kullanıldığında kaydın durumu ve teyit zamanı
  değişmez, ikinci teyit olayı eklenmez ve sayfa AC-30'un mesajını gösterir.
- **AC-33.** Aynı bağlantıyla **aynı anda** gelen iki teyit isteğinden (istekler bir bariyerle
  eşzamanlı başlatılır) yalnız biri başarılı olur; teyit zamanı ve teyit olayı bir kez yazılır.
- **AC-34.** Teyit bekleyen adrese yeni teyit iletisi gittikten sonra önceki iletinin bağlantısı
  aboneliği etkinleştirmez.
- **AC-35.** Hiç var olmamış ya da bozuk bir teyit bağlantısının cevabı, süresi dolmuş ve
  kullanılmış bağlantınınkiyle aynı durum kodu ve aynı gövdedir.
- **AC-36.** Teyit sayfası istek sürerken yükleniyor durumunu, sunucu hatasında teknik ayrıntı
  içermeyen bir hata ve yeniden deneme yolunu gösterir.

### Çıkış (R5, R6, R20 · D6, D8, D22, D25, D27)

- **AC-37.** Her bülten iletisi aboneye özgü, mutlak bir çıkış bağlantısı taşır.
- **AC-38.** Çıkış bağlantısı oturum açılmamış bir tarayıcıda açıldığında başka bir düğme ya da
  onay istemeden kaydı **çıkmış** yapar, çıkış zamanını UTC olarak yazar, bir **çıkış** izin olayı
  ekler ve sayfa "abonelikten çıktınız" mesajını gösterir.
- **AC-39.** Çıkıştan sonraki ilk dönemde, Yayında aday varken o adrese bülten kuyruğa girmez.
- **AC-40.** Çıkmış kayıt silinmez: çıkıştan sonra kayıt **çıkmış** durumunda ve çıkış zamanıyla
  durur.
- **AC-41.** Aynı çıkış bağlantısının ikinci kez açılması aynı mesajı gösterir; durum ve çıkış
  zamanı değişmez, ikinci çıkış olayı eklenmez.
- **AC-42.** Çıkıp yeniden abone olan ve teyit eden bir adreste, **ilk** aboneliğin bültenindeki
  çıkış bağlantısı aboneliği yine çıkmış yapar.
- **AC-43.** Çıkış sayfası ana sayfadaki bülten bölümüne giden bir yeniden abone olma bağlantısı
  gösterir.
- **AC-44.** Geçersiz bir çıkış bağlantısı hiçbir kaydı değiştirmez ve sayfa "bağlantı artık
  geçerli değil" mesajını gösterir.
- **AC-45.** Çıkış sayfası istek sürerken yükleniyor durumunu, sunucu hatasında teknik ayrıntı
  içermeyen bir hata ve yeniden deneme yolunu gösterir.
- **AC-46.** Dönem işlendikten sonra geçici hatayla tekrar denemeye düşmüş bir bülten, abone
  çıktıktan sonraki denemede gönderilmez ve kalıcı başarısız işaretlenir (D22).
- **AC-47.** Çıkış bağlantılarını imzalayan sır değiştirildiğinde, önceki sırla üretilmiş bir
  çıkış bağlantısı AC-44'ün cevabını verir (D25'in bedeli).

### Bağlantı güvenliği ve adresler (R21 · D25, D26)

- **AC-48.** Teyit bağlantısındaki anahtar veritabanının hiçbir alanında düz olarak bulunmaz;
  aboneliğin kaydında yalnız anahtarın SHA-256 özeti durur.
- **AC-49.** Teyit anahtarı kodu çözüldüğünde 32 bayttır ve aynı adres için ardışık iki teyit
  iletisi farklı anahtar taşır.
- **AC-50.** Çıkış bağlantısının imza sırrı yapılandırmada yokken uygulama ayağa kalkmaz.
- **AC-51.** Teyit bağlantısı `{PublicBaseUrl}/{locale}/newsletter/confirm?token=…`, çıkış
  bağlantısı `{PublicBaseUrl}/{locale}/newsletter/unsubscribe?token=…` kalıbındadır ve ön yüz
  iki yolu da ilgili sayfayla açar.
- **AC-52.** Slug'ı `newsletter` olan bir içerik sayfası kaydı reddedilir.
- **AC-53.** Teyit ve çıkış sayfaları `noindex` taşır.
- **AC-54.** Teyit ve çıkış sayfalarının yanıtı `Referrer-Policy: no-referrer` başlığını taşır.

### "Yeni araç" ve dönem (R8, R9, R15 · D4, D12, D13, D21, D23)

- **AC-55.** Dönem işlenirken, henüz bir döneme atanmamış ve o an **Yayında** olan her aday ilan,
  her etkin abonenin bülteninde tam bir kez yer alır.
- **AC-56.** Dönem kesiminden önce yayınlanan ama `VehiclePublished` olayı kesimden **sonra**
  işlenen ilan, bir sonraki dönemin bülteninde bir kez yer alır.
- **AC-57.** Bir bültende yer almış ilan yayından kaldırılıp sonraki dönemde yeniden
  yayınlandığında sonraki bültende yer almaz.
- **AC-58.** Her okumada ilerleyen bir sahte saatle (`OccurredAt ≠ PublishedAt`) bu dönemde ilk kez
  yayınlanan ilan bültende yer alır.
- **AC-59.** Aynı dönemde ilk kez yayınlanan, sonra kaldırılıp yeniden yayınlanan ilan bültende bir
  kez yer alır.
- **AC-60.** İlk yayını devreye giriş anından önce olan bir ilan, devreye girişten sonra yeniden
  yayınlansa da hiçbir bültende yer almaz.
- **AC-61.** Dönem işlenirken **satılmış**, **arşivlenmiş**, **yayından kaldırılmış** ya da
  **rezerve** olan aday o dönemin bülteninde yer almaz (dört ayrı test).
- **AC-62.** Dönem işlenirken rezerve olduğu için elenen ilan, vitrine döndükten sonraki dönemlerin
  hiçbirinin bülteninde yer almaz (D12, D21).
- **AC-63.** Dönem işlenirken **Satıldı** olduğu için elenen ilan, satışı geri alınıp (`0023`)
  Yayında'ya döndükten sonraki dönemlerin hiçbirinin bülteninde yer almaz (D21).
- **AC-64.** Dönem işlenirken Yayında aday yokken hiçbir aboneye ileti kuyruğa girmez ve gönderim
  geçmişinde o dönem için kayıt oluşmaz.
- **AC-65.** Dönem işlendikten sonra, iletiler dağıtılmadan önce dönemin bütün ilanları satılırsa
  ileti gönderilmez ve kalıcı başarısız işaretlenir (D21).
- **AC-66.** Dönemin iki ilanından biri dağıtımdan önce satılırsa ileti yalnız Yayında kalan ilanı
  taşır; dağıtımdan önce fiyatı değişen ilanın satırı yeni fiyatı taşır (D21).
- **AC-67.** Dönem işleyicisi dönem anından önce çağrıldığında hiçbir dönem kaydı ve ileti üretmez.
- **AC-68.** Kaçırılmış bir dönem sonradan işlendiğinde bülteni **işlenme** anında etkin olan
  aboneler alır: arada teyit eden alır, arada çıkan almaz.
- **AC-69.** Dönem işleyicisi aynı dönem için **ardışık** iki kez çağrıldığında her abone o dönem
  için en çok bir bülten kaydı alır.
- **AC-70.** Dönem işleyicisi aynı dönem için bir bariyerle **eşzamanlı** iki kez çağrıldığında
  her abone o dönem için en çok bir bülten kaydı alır.
- **AC-71.** Uygulama iki dönem anı boyunca kapalı kaldıysa açılışta her etkin aboneye tek bir
  bülten gider ve iki dönemin adaylarını birlikte taşır.
- **AC-72.** İlk açılış bir başlangıç dönemi kaydı yazar; ikinci açılış bu kaydı ve zamanını
  değiştirmez.
- **AC-73.** Aynı `VehiclePublished` olayı iki kez teslim edildiğinde tek aday kaydı oluşur ve ilan
  bültende bir kez yer alır.

### Bülten içeriği (R3, R16, R17 · D11, D14, D18, D24)

- **AC-74.** Bültendeki her araç satırı model adını abonenin dilinde, model yılını, km'yi, fiyatı,
  şube adını ve ilanın abonenin dilindeki mutlak bağlantısını taşır.
- **AC-75.** Fiyatı "sorunuz" olan ilanın satırı tutar yerine şablon kataloğundaki "Fiyat sorunuz"
  karşılığını taşır.
- **AC-76.** KWD kurulumunda `12500.750` tutarlı ilanın fiyatı üç ondalık basamakla ve para
  birimiyle yazılır; son basamak (`0`) düşmez.
- **AC-77.** TRY ve KWD fiyatı, km ve model yılı için bültenin biçimi, aynı değerlerle vitrinin
  `formatMoney`, `formatMileage` ve `formatYear` çıktısıyla TR, AR ve EN'de bayt bayt aynıdır;
  Arapça'da rakam kümesi dâhil (D24).
- **AC-78.** 23 aday ilanlı bir dönemde bülten ilk yayını en yeni 20 ilanı sırayla taşır, ardından
  sayı içermeyen "Diğer yeni araçlar için vitrine göz atın" cümlesini ve vitrin listesinin
  bağlantısını taşır; 20 ya da daha az adayda bu cümle yoktur.
- **AC-79.** Bülten galerinin abonenin dilindeki görünen adını taşır.
- **AC-80.** Arapça abone olunmuş adrese teyit iletisi ve bülten Arapça şablonlarla gider.
- **AC-81.** Abonenin dilinde şablon yoksa ileti varsayılan dilde gider ve düşüş loglanır.
- **AC-82.** Abonenin dili kurulumun etkin dilleri arasından çıkarıldıysa bülten varsayılan dilde
  gider.

### Mimari, şema ve kuyruk (R10 · C12 · D11, D20, D23)

- **AC-83.** Zincir testi (`BildirimSinirTests`) `Notifications`'ın tükettiği **bütün** sorgu
  arayüzlerini tarar — yeni ilan özeti okuması, model adı okuması ve `IGalleryDisplayNameQueries`
  dâhil — ve gerçekleştirimlerinden biri başka bir modülün `Contracts`'ına uzandığında kırmızı
  verir (negatif kanıt DoD'da).
- **AC-84.** Hem bir talebe hem bir aboneliğe bağlı ya da hiçbirine bağlı olmayan bir gönderim
  kaydı veritabanı tarafından reddedilir (D23).
- **AC-85.** Migration'dan önce kuyrukta bekleyen bir talep iletisi, migration'dan sonra
  gönderilir ve gönderildi işaretlenir.
- **AC-86.** Bülten iletisinde geçici hata yapılandırılan sayıda, artan aralıkla yeniden denenir;
  kalıcı hata yeniden denenmez.
- **AC-87.** Bir abonenin iletisinin başarısız olması aynı dönemdeki diğer abonelerin iletilerini
  engellemez.
- **AC-88.** `0009`'un kabul kriterleri regresyon olarak yeşildir.

### Kişisel veri (C5 · D25, D27)

- **AC-89.** Teyit ve bülten iletilerinin gönderim geçmişi kaydı alıcının e-posta adresini hiçbir
  alanda taşımaz.
- **AC-90.** Abonelik, teyit, çıkış, dönem ve gönderim yolundaki log çağrıları e-posta adresini,
  teyit anahtarını ve çıkış bağlantısını parametre olarak almaz — çağrı yüzeyini tarayan testle
  (`0009` AC-25 deseni).
- **AC-91.** Teyit ve çıkış bağlantıları adresi düz, URL kodlu ya da Base64 olarak içermez.
- **AC-92.** Abonelik, teyit ve çıkış uçlarının hiçbir hata yanıtı e-posta adresini içermez.
- **AC-93.** İzin olay kaydı e-posta adresini hiçbir alanda taşımaz; bir istek → teyit → çıkış →
  istek → teyit dizisinden sonra beş olayın beşi de zamanlarıyla durur.

### Dil ve RTL (R12)

- **AC-94.** Bülten bölümü, teyit sayfası ve çıkış sayfasının metinleri TR/AR/EN vitrin
  kataloglarında vardır; eksik anahtar kontrolü (`check:locales`) yeşildir.
- **AC-95.** Arapça ana sayfadaki bülten bölümü ile Arapça teyit ve çıkış sayfaları sağdan sola
  düzendedir; metnin hizası Range ölçümüyle sağdır.
- **AC-96.** Teyit iletisi ile bültenin başlık, satır ve altlık şablonları ve "Fiyat sorunuz" ile
  "Diğer yeni araçlar…" metinleri TR, AR ve EN üçünde de seed edilmiştir; eksik şablon kontrolü
  backend testiyle yeşildir (`0009` C13).

### Yapılandırma ve sınır (C10, C14 · D9, D13, D25)

- **AC-97.** Dönem günü/saati, teyit bağlantısı süresi, üç hız sınırının (kaba istek, geçerli
  istek, adres başına teyit iletisi) eşik ve pencerelerinden ya da çıkış bağlantısı sırrından biri
  yapılandırmada yokken uygulama ayağa kalkmaz.
- **AC-98.** Dönem anı, teyit süresi ve üç hız sınırını doğrulayan testler yapılandırılan değeri
  kullanır, koda gömülü bir sabiti değil.
- **AC-99.** Yeni migration'lar yalnız `notifications` şemasına dokunur; mimari testler (modül
  sınırı, `Notifications`'ın olay yayınlamaması) yeşildir.

### Performans (C13 · D17)

- **AC-100.** PR'ın C3 koşusunda ana sayfa, liste ve ilan detayı için medyan LCP < 2500 ms ve medyan
  CLS < 0,1'dir; bir sayfanın medyanı 2450 ms'yi aşarsa ikinci koşunun sonucu ve LCP öğesi PR'da
  yazılıdır.
- **AC-101.** Ana sayfanın giriş JS'i ile giriş CSS'inin `gzip -9` boyutları toplamı `main`
  tabanına göre en çok 3072 B artar; iki toplam ve fark PR'da yazılıdır (D17).

---

## Definition of Done

- [x] S3 ve S4 (**D4**, **D5**) ile **D6–D20** Takım Yöneticisi (ajan) tarafından onaylandı
      (2026-11-19); sahip kararları **D1–D3** işlendi.
- [x] QA CLARIFY turu yapıldı ve yirmi bulgusu (B1–B20) **D4**, **D9–D11**, **D13**, **D14**,
      **D16**, **D17**, **D20** ve **D21–D27** ile kriterlere işlendi (2026-11-19).
- [ ] **SS1–SS5** sahibe iletildi; cevaplar ya bu karta ya da açılan ayrı karta işlendi.
- [x] `0012b`, `0017` ve `0020` tamamlandı (2026-11-19 ölçüldü — Context).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0018-ac<n>.png`),
      yükleniyor, hata, hız sınırı, boş vitrin ve geçersiz bağlantı durumları dâhil; Arapça
      görüntü de var.
- [ ] Teyit edilmemiş adrese bülten gönderilmediği testle gösterildi (R2 · AC-29).
- [ ] Çıkış bağlantısının **tek adımda** ve oturumsuz çalıştığı testle gösterildi (R5 · AC-38).
- [ ] Kişisel verinin logda, gönderim geçmişinde ve izin olay kaydında bulunmadığı testle
      gösterildi (C5 · AC-89–93).
- [ ] **Zincir testinin negatif kanıtı:** yeni tüketilen bir arayüzün gerçekleştirimine geçici
      olarak başka bir modülün `Contracts` çağrısı eklendiğinde AC-83'nin testinin kırmızı verdiği
      çıktısıyla PR'da gösterildi; ekleme geri alındı.
- [ ] `0009`'un kabul kriterleri regresyon olarak hâlâ yeşil (R10 · AC-88).
- [ ] Şablonlar TR/AR/EN üçünde de seed edildi; eksik şablon kontrolü yeşil (C6 · AC-96).
- [ ] `docs/deploy.md` çıkış bağlantısı sırrını zorunlu ortam değişkeni olarak ve "değişirse eski
      çıkış bağlantıları geçersiz olur" uyarısıyla listeliyor; `dev-up.sh` sırrı üretiyor (D25).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `notifications` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimleri (**D19**) `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T8 --> `docs/architecture.md` güncellendi: Karar Defteri satırı, bağımlılık haritası
      ("Okuduğu üç yüzey"), `VehiclePublished` tüketicisi ve § 2.A koşul 2 istisnası (**D20** —
      koşulsuz).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4); C3 eşikleri dâhil (AC-100).
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
