# 0005 — Ziyaretçi Talebi Oluşturma

| | |
|---|---|
| **Durum** | Geliştiriliyor |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Leads` (yeni — bugün hiç yok) · `VehicleCatalog.Contracts` (talep anlık görüntüsü için genişler) · `frontend/` public vitrin |
| **Bağımlı spec'ler** | `0002` (ilan detayı, tamamlandı) · `0003` (şubeler, tamamlandı) — ikisi de önkoşul, ikisi de hazır |

---

## Intent

Vitrindeki bir aracı beğenen **ziyaretçi** bugün galeriye ulaşmak için telefonu eline almak
zorunda: ilan detay sayfasında ilgisini bırakabileceği hiçbir yer yok. Mesai dışında bakan,
yazmayı tercih eden veya Arapça konuşan ziyaretçi için bu, ilginin sessizce kaybolduğu nokta.

**Galeri** bu ilgiyi kaydetmek istiyor, çünkü `docs/domain.md`'nin sözlüğünde **Talep**
sistemin *"ikinci çekirdek kavramı"* olarak tanımlı: vitrinin tek ticari çıktısı odur.

Bu iş bittiğinde ziyaretçi, bir ilanın detayından talebini bırakabilir *(araca bağlı
olmayan genel talep için bkz. X11)*. Talep düştüğü anda üç şeyi **kendi içinde** taşır ve
bunlar sonradan değişmez:
hangi dilde bırakıldığı, o andaki araç bilgisi (model adı + fiyat kopyası) ve ziyaretçinin
iletişim izni. Talep, ilgilenilen aracın şubesine bağlanır.

Başarı, "kaç talep geldi" değil: **bırakılan her talebin, bırakıldığı andaki gerçeğini
kaybetmeden durması.** Araç sonradan satılsa, fiyatı değişse veya arşivlense bile talep
kendi gerçeğini korur.

---

## Requirements

- **R1.** Ziyaretçi, bir ilanın detay sayfasından o araçla ilgili talep bırakabilir.
- **R2.** `Rezerve` yayın durumundaki bir ilanın detayında talep formu **gösterilmez** ve
  o ilan için gönderilen talep **reddedilir**. Ziyaretçiye formun neden kapalı olduğu
  söylenir. → `docs/domain.md` **R6**; bu yarı spec `0002` § X1 ile bu spec'e devredilmiştir.
- **R3.** Talep bırakırken ziyaretçi bir **talep tipi** belirtir. Bu spec'te geçerli tipler:
  bilgi talebi, geri arama, test sürüşü, fiyat teklifi, kredi görüşmesi.
  **Takas** sözlükte tanımlı altıncı tiptir ve sistemde **tanınır**, ancak bu spec'te
  **kabul edilmez** — takasla gönderilen talep reddedilir. → `docs/domain.md` § Talep Tipi,
  Karar D3. *(Kapsam için bkz. X1.)*
- **R4.** Talep, ziyaretçinin **adını** ve en az bir iletişim kanalını (telefon veya e-posta)
  taşır. Adı olmayan veya hiçbir iletişim kanalı olmayan talep kaydedilmez. Verilen e-posta
  geçerli biçimde değilse talep kaydedilmez. Ad, e-posta ve mesaj alanlarının üst uzunluk
  sınırı vardır; sınırı aşan talep kaydedilmez.
- **R5.** Talep, ziyaretçinin yazdığı **serbest metin mesajı** taşıyabilir. Mesaj
  **opsiyoneldir**; mesajsız talep kaydedilir. Mesaj, R13 anlamında **kişisel alandır**.
- **R6.** Her talep oluşturulurken, talebin bırakıldığı **locale** kaydedilir.
  → `docs/domain.md` **L1**.
- **R7.** Her talep oluşturulurken, ilgilenilen aracın **anlık görüntüsü** — model adı ve
  fiyatı — talebe **kopyalanır**. Araç sonradan değişse, fiyatı güncellense veya arşivlense
  bile bu kopya değişmez. Model adı **kanonik Latin adı** olarak kopyalanır; ziyaretçinin
  locale'indeki ad ayrıca taşınabilir ama gösterim için esas olan kanonik addır.
  → `docs/domain.md` **L2**, `docs/architecture.md` § 3.
- **R8.** Talep, ilgilenilen aracın **şubesine** bağlanır. → `docs/domain.md` **L3**.
  *(Araçsız talebin galeri geneline düşmesi bu spec'te yoktur → X11.)*
- **R9.** İlgilenilen ilan **çözülemiyorsa** — hiç yoksa ya da vitrinde görünür değilse —
  talep **reddedilir** ve kayıt oluşmaz. İki durum ziyaretçi tarafından **ayırt edilemez**.
- **R10.** Sayfa açıldıktan sonra ilan vitrinden düşmüşse talep reddedilir; ziyaretçiye
  aracın artık mevcut olmadığı söylenir, **girdiği bilgiler kaybolmaz** ve ona listeye
  dönme eylemi sunulur.
- **R11.** Her talep oluşturulurken ziyaretçinin **iletişim izni** kaydedilir: verildi veya
  verilmedi. Neye izin verildiğini söyleyen metin üç etkin dilde yazılıdır.
  → `docs/domain.md` **L7**. *(Aydınlatma metni sayfası için bkz. X12.)*
- **R12.** Oluşturulan her talep `Yeni` durumunda başlar. → `docs/domain.md` **L4**.
- **R13.** Talep kaydı **silinmez.** Kişisel alanların sonradan anonimleştirilebilmesi için
  kayıt, kişisel alanları ile istatistiksel alanlarını ayırt edecek biçimde tutulur.
  → `docs/domain.md` **L6**. *(Anonimleştirme eyleminin kendisi için bkz. X4.)*
- **R14.** Talep kaydedildiğinde sistem bunu **duyurur** (`LeadCaptured`); duyuru talebin
  kaydıyla **birlikte** kalıcı olur. Talep kaydedilmediyse duyuru da kaydedilmemiştir.
  → `docs/architecture.md` § 1 (olay haritası), § 2.C.
- **R15.** Ziyaretçiye, talebinin **alındığı** kendi dilinde bildirilir.
  *(Galerinin ulaşacağına dair söz bu spec'te verilmez — bkz. Karar D2 ve D6.)*
- **R16.** Sistem, aynı kaynaktan kısa sürede gelen aşırı sayıda talebi reddeder; reddedilen
  istek kayıt oluşturmaz. Eşik, pencere ve "aynı kaynak"ın tanımı **C17**'dedir.
- **R17.** Aynı talebin iki kez gönderilmesi iki kayıt üretmez: istek bir **idempotentlik
  anahtarı** taşır ve aynı anahtarla gelen ikinci istek yeni kayıt oluşturmaz.
- **R18.** Talep formu **yükleniyor**, **başarılı** ve **hata** durumlarının üçünü de ele alır.
  → `docs/frontend.md` § 3.
- **R19.** Form etkin üç locale'de (TR/AR/EN) çalışır; Arapça'da yön ve biçimlendirme RTL'dir.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Leads`, talep verisinin **tek yazma sahibidir.** Aracın güncel fiyatına sahip
  değildir; yalnızca kendi kopyasına sahiptir. → `docs/architecture.md` § 1.
- **C2.** Modül sınırını yalnızca `X.Contracts` geçer. `Leads`, `VehicleCatalog`'un tablosuna,
  internal tipine veya `DbContext`'ine erişmez. → Yasak Listesi 1, 6–9.
- **C3.** Personelin kim olduğu `Leads`'in verisi değildir; yalnızca `StaffId` taşınır.
  → `docs/architecture.md` § 1. *(Atama için bkz. X2.)*
- **C4.** Sistemden dışarı giden e-posta / SMS / WhatsApp yalnızca `Notifications` üzerinden
  gider. **İstisna yoktur.** → Yasak Listesi 17. *(Sonucu için bkz. Karar D2.)*
- **C5.** Zincirleme senkron çağrı (`A → B → C`) ve modül sınırını aşan transaction yasaktır.
  → Yasak Listesi 13, 15.
- **C6.** Fiyat kopyası `Money` ile taşınır: tutar + ISO 4217 kodu + minor unit üssü.
  Üç ondalıklı para birimleri (KWD/BHD/JOD/OMR) bozulmadan saklanır.
  → `AGENTS.md` Altın Kural 4, `docs/domain.md` **P1–P2**, Yasak Listesi 26.
- **C7.** "Fiyat sorunuz" ayrı bir durumdur, `0` değildir; anlık görüntü bunu da kopyalar.
  → `docs/domain.md` **P4**.
- **C8.** Telefon numarası E.164 biçiminde taşınır (`BuildingBlocks.PhoneNumber`).
  → `docs/conventions.md`, `docs/architecture.md` § 1.
- **C9.** Zaman damgaları UTC'dir; `DateTime.Now` veya sunucu yerel saati kullanılmaz.
  → Yasak Listesi 30.
- **C10.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner, çeviri uçta yapılır.
  Doğrulama hata metni sunucudan çevrilmiş olarak gelmez.
  → `AGENTS.md` Altın Kural 6, Yasak Listesi 23–24, `docs/frontend.md` § 3.1.
- **C11.** Sayı içeren cümleler dize birleştirmeyle kurulmaz; ICU mesaj formatı zorunludur.
  → Yasak Listesi 31.
- **C12.** CSS'te fiziksel yön özellikleri kullanılmaz; logical properties zorunludur.
  → Yasak Listesi 25.
- **C13.** Ekran, `docs/frontend.md` § 4.2 bileşen envanterini kullanır; § 4.1 gereği gömülü
  ham piksel, hex renk veya ham font boyutu eklenmez.
- **C14.** Yeni migration yalnızca `Leads` şemasına dokunur. → Yasak Listesi 3.
- **C15.** Her kabul kriteri en az bir testle karşılanır; test `DisplayName`'i
  `AC-<n>: <kriterin metni>` biçimindedir ve metin bu spec'ten **kopyalanır**.
  → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 1.
- **C16.** **Kriterlerin gözlem noktası.** D1 uyarınca bu spec'te talep **okuma** yüzeyi
  yoktur; yalnızca oluşturma ucu vardır. "Kaydedilir / kaydedilemez / değişmez / okunabilir
  kalır" diyen her kriter `Leads`'in domain ve uygulama katmanında doğrulanır
  (`docs/testing.md` § 1). Endpoint testi yalnızca **oluşturma ucu** için geçerlidir.
- **C17.** **Hız sınırı ve hata sözleşmesi.** Eşik: **aynı IP'den 10 dakikada en çok 5
  talep**; altıncı istek reddedilir. Red karşılığı `429` durum kodu ve `lead.rate_limited`
  hata kodudur. IP **kişisel veridir**: yalnızca geçici sayaçta tutulur, kalıcı kayda ve
  loga yazılmaz (`docs/conventions.md` § 12). *(Not: `docs/conventions.md` § 5'in durum kodu
  listesinde `429` bulunmuyor; oraya eklenmesi ayrıca önerilmiştir.)*

  **Eşik TALEP sayar, istek değil.** "Aynı IP'den 10 dakikada 5" ifadesi *kaydedilen*
  talepler içindir: doğrulamaya takılan istek bu bütçeyi tüketmez. Aksi hâlde formu
  birkaç kez yanlış dolduran dürüst bir ziyaretçi hiç talep bırakamadan kilitlenirdi ve
  bu, Intent'in önlemek için yazıldığı kaybın ta kendisi olurdu. Kaba tekrarı kesen ayrı
  ve daha gevşek bir **istek** sınırı vardır; sayısı bu kriterin konusu değildir.

  **"Aynı kaynak" ziyaretçinin IP'sidir.** Uygulama, istekleri doğrudan istemciden
  aldığını varsayar. Ters proxy arkasına konursa gerçek adres yalnızca **açıkça
  listelenmiş** proxy'lere güvenilerek çözülür (`ForwardedHeaders:KnownProxies`);
  yapılandırılmazsa sayaç tüm ziyaretçileri tek bütçeye indirir ve bu sınır yeniden
  değerlendirilmelidir.

### KAPSAM DIŞI

- **X1.** **Takas talebi** (`docs/domain.md` **T1–T3**) — *neden:* takas, müşterinin kendi
  aracının `VehicleReference` sözlüğünden seçilmesini gerektirir; bu, marka/model/yıl seçici
  bir arayüz ve ayrı bir doğrulama kümesidir. `VehicleReference` modülü hazır olduğu için
  **teknik engel yok**, yalnızca kapsam kararıdır. → Karar D3.
- **X2.** **Danışman ataması ve durum akışı** (`Yeni → Atandı → İletişimde → Sonuçlandı`,
  **L4**), sonuç ve kayıp nedeni, notlar — *neden:* atama `Identity`'nin verisine (personel,
  rol, şube) dayanır ve `Identity` modülü yoktur. Bu spec talebi `Yeni` durumunda bırakır.
- **X3.** **Bildirimler** — ne ziyaretçiye ne danışmana bildirim gönderilir — *neden:*
  `Notifications` modülü yoktur ve Yasak Listesi 17 istisnasızdır. R11'in karşılığı
  ziyaretçiye **ekranda** verilir, mesajla değil. → Karar D2.
- **X4.** **KVKK anonimleştirme eylemi** — *neden:* eylemi çalıştıracak bir yönetim yüzeyi
  yok (X5). Bu spec verinin anonimleştirilebilir **biçimde** durmasını sağlar (R10), eylemi
  değil.
- **X5.** **Yönetim paneli: talep listesi, talep detayı, filtreler** — *neden:* `Identity` ve
  kimlik doğrulama yoktur; korumasız bir talep listesi ucu, ziyaretçilerin ad, telefon ve
  e-postasını internete açık hâle getirir. → Karar D1.
- **X6.** **L5** (personel pasifleştirildiğinde açık taleplerin yeniden atanması) —
  *neden:* atama yoksa yeniden atama da yoktur (X2).
- **X7.** Rezervasyon (`docs/domain.md` § 2.3) — *neden:* rezervasyon ayrı bir kavramdır ve
  `Yayında` araç üzerinde danışman eylemidir; talep formu değildir.
- **X8.** Kredi hesaplama, taksit tablosu, banka entegrasyonu — *neden:*
  `docs/architecture.md` § 6'da kapsam dışıdır; kredi **ilgisi** bir talep tipidir, hesabı
  değil.
- **X9.** Captcha / üçüncü parti bot koruması — *neden:* üçüncü parti bağımlılığı ve KVKK
  değerlendirmesi ayrı bir karardır. R12'nin hız sınırı bu spec'in koruması olarak kalır.
  → Karar D4.
- **X10.** `Engagement` sayaçları (görüntülenme, telefon/WhatsApp tıklama) — *neden:* ayrı
  modül, ayrı yaşam döngüsü, kayba toleranslı veri.
- **X11.** **Araca bağlı olmayan genel talep ve iletişim sayfası** — *neden:* genel talep
  yeni bir public rota, kabuk navigasyonunda bir giriş noktası ve kendi üç durumu demektir;
  bu spec zaten sıfırdan bir modül kuruyor. `docs/domain.md` **L3**'ün "araçsız talep galeri
  geneline düşer" yarısı bu yüzden burada uygulanmaz ve **ayrı bir spec'e** aittir.
- **X12.** **KVKK aydınlatma metni sayfası** — *neden:* metnin kendisi hukuk onayı ister ve
  sahibi `Content` modülüdür; `Content` yoktur. Bu spec izin **metnini** üç dilde taşır
  (R11), aydınlatma sayfasını değil.

---

## Context

### Mevcut davranış — doğrulandı

- **`Leads` modülü yoktur.** `src/Modules/` altında yalnızca `Dealership`, `Media`,
  `VehicleCatalog`, `VehicleReference` var. Bu spec modülü sıfırdan var eder.
- **İlan detay sayfası hazır ve talebe yer bırakılmış.** `0002` tamamlandı; spec 0002 § X1
  talep formunu açıkça kendi dışında bırakıp *"tek başına bir spec'tir"* demişti — bu spec
  o boşluğu kapatır.
- **Detay sözleşmesi araç kimliği taşımıyor.** `ShowcaseListingDetailDto` şu alanlarla
  geliyor: `Slug`, `IsReserved`, `CoverImageUrl`, `ModelName`, `ModelYear`,
  `MileageKilometers`, `Price`, `BranchName`, `Attributes`
  ([ShowcaseListingDetailContracts.cs:13](src/Modules/VehicleCatalog/VehicleCatalog.Contracts/Detail/ShowcaseListingDetailContracts.cs#L13)).
  `BranchName` bir **gösterim metnidir**, kimlik değildir; `BranchId` yoktur.
  **Sonucu:** R6 (anlık görüntü) ve R7 (şubeye bağlama) bugünkü sözleşmeyle karşılanamaz.
  `VehicleCatalog.Contracts`, talep anında ilanın kimliğini, model adını, fiyatını ve
  şube kimliğini veren bir okuma yüzeyi ile **genişlemelidir.** Bu genişleme 0005
  kapsamındadır; yüzeyin biçimi Developer'ın kararıdır.
- **Şube tarafı hazır.** `0003` tamamlandı; `Dealership` modülü, `Branch` agregatı ve
  `BranchId` artık gerçek (`src/Modules/Dealership/Dealership/Domain/Branch.cs`).
- **Outbox altyapısı artık var.** `0003`'ün D2 kararıyla kuruldu: `OutboxWriter`,
  `OutboxDispatcher`, `OutboxDispatcherHostedService`, `IdempotentIntegrationEventHandler`
  (`src/BuildingBlocks/Outbox/`). `Leads` ileride olay yayınlayacaksa mekanizma hazırdır.
- **Değer tipleri hazır:** `Money`, `Locale`, `LocalizedText`, `PhoneNumber`, `IClock`,
  `Result<T>`, `Error` (`src/BuildingBlocks/`). C6, C8 ve C9 için yeniden icat gerekmez.
- **`Identity` ve `Notifications` modülleri yoktur** ve Host'ta kimlik doğrulama yoktur —
  `0003` yazılırken de aynı durumdaydı ve o spec'in D1 kararı bu yüzden yazma uçlarını
  kapsam dışı bırakmıştı. → Karar D1, D2.

### Geçerli domain kuralları

- **L1** — `SubmittedLocale` kaydedilir; müşteriye giden bildirim bu dille, danışmana giden
  danışmanın diliyle gider. *(Bildirim kısmı X3'te.)*
- **L2** — araç anlık görüntüsü (model adı + fiyat) kopyalanır; talep kendi gerçeğini korur.
- **L3** — talep aracın şubesine bağlanır; araçsız talep galeri geneline düşer.
- **L4** — durum akışı `Yeni → Atandı → İletişimde → Sonuçlandı`. Bu spec `Yeni`de durur.
- **L6** — talep silinmez; KVKK silme talebinde kişisel alanlar anonimleştirilir, kayıt ve
  istatistik korunur.
- **L7** — iletişim izni talep anında kaydedilir; izin yoksa pazarlama bildirimi gönderilmez,
  yalnızca talebin kendisine dair işlemsel bildirim gönderilir.
- **P4** — "fiyat sorunuz" ≠ `0`; anlık görüntü bu durumu da taşır.

### Bilinen tuzaklar

1. **Anlık görüntü yerine referans tutmak.** Talebe `ListingId` koyup fiyatı okuma anında
   araçtan çekmek, **L2**'nin tam tersidir: araç satılınca veya fiyatı değişince talep geriye
   dönük olarak yalan söyler. Kopya, kopya olarak durmalıdır.
2. **"Fiyat sorunuz" aracın anlık görüntüsü.** Fiyatı olmayan araçta tutarı `0` kopyalamak
   sessiz bir veri bozulmasıdır (**P4**, C7).
3. **Üç ondalıklı para birimi.** KWD tutarı iki ondalığa yuvarlanarak kopyalanırsa hata
   kalıcıdır — kopya sonradan düzeltilemez, çünkü kaynak da değişmiş olabilir.
4. **İzni varsayılan olarak işaretli sunmak.** **L7** izni bir kayıt olarak ister; önceden
   işaretli kutu, alınmamış izni alınmış gibi gösterir.
5. **Doğrulama metnini sunucudan çevirip göndermek.** Yasak Listesi 23'ün tam ihlali; uç
   `errorCode` alır ve kendisi çevirir (C10).
6. **Çözülemeyen ilanı genel talebe çevirmek.** Araç satılmış veya şubesi kapanmışken
   talebi "araçsız talep" olarak kaydetmek **L3**'ü sessizce bozar: ziyaretçi bir araç için
   yazdığını sanır. R9/R10 reddetmeyi zorunlu kılar.
7. **Anlık görüntüyü ziyaretçinin dilinde dondurmak.** Arapça bırakılan talebin model adı
   yalnızca Arapça saklanırsa, o dili okumayan danışman için talep okunamaz hâle gelir ve
   kopya sonradan düzeltilemez (R7).
8. **"Tekrar dene"nin ikinci kayıt üretmesi.** Ağ hatası isteğin sunucuya ulaşmasından
   *sonra* olduysa, yeniden gönderim ikinci bir talep yaratır. Hız sınırı bunu yakalamaz —
   iki istek eşiğin çok altındadır (R17).

### Verilen kararlar

Altısı da Takım Yöneticisi tarafından onaylandı. Bundan sonra değişmeleri **spec revizyonudur**
(AP-08) ve SCORECARD'ın "spec revizyon sayısı" satırına yazılır.

**D1 — Yönetim paneli ve talep okuma yüzeyi bu spec'te yapılmaz.**
*Gerekçe:* `Identity` ve kimlik doğrulama yok (doğrulandı). Korumasız bir talep listesi ucu,
ziyaretçilerin adını, telefonunu ve e-postasını internete açık hâle getirir — bu, araç
verisinden farklı olarak **kişisel veridir** ve KVKK kapsamındadır. Bu, `0003`'ün D1
kararıyla aynı çizgidir. *Bilerek ödenen bedel:* galeri, biriken talepleri bu spec'le
göremez — sonucu D2'de yazılıdır.

**D2 — Bildirim gönderilmez; ziyaretçiye onay yalnızca ekranda verilir.**
*Gerekçe:* `Notifications` modülü yok ve Yasak Listesi 17 istisnasız.
**Bunun açıkça söylenmesi gereken sonucu şudur:** D1 ile birlikte, bırakılan talebi
**kimse görmez** — ne danışmana bildirim gider, ne galeri talebi listeleyebilir. Talepler
veritabanında doğru biçimde birikir ama okunmaz.
**Karar: 0005 buna rağmen yapılır.** *Gerekçe:* **L1/L2/L7 sonradan telafi edilemez** —
bugün kaydedilmeyen locale, anlık görüntü ve izin yarın geriye dönük üretilemez; okuma yüzeyi
ise her zaman eklenebilir. *Bilerek ödenen bedel:* 0005 tek başına ticari değer üretmez ve
tamamlandığında bile hiçbir talep okunamaz. Değer, D5'teki sıra tamamlandığında açılır.
*Elenen alternatif:* 0005'i `Identity` + `Notifications` sonrasına ertelemek — bu durumda ilan
detay sayfası o süre boyunca hiçbir ilgiyi yakalayamaz ve yakalanmayan ilgi geri gelmez.

**D3 — Takas talebi bu spec'in dışındadır.**
*Gerekçe:* takas, müşterinin aracını `VehicleReference`'tan seçtiren ayrı bir arayüz ve
doğrulama kümesi getirir (**T1**). Teknik engel yok; kapsam kararıdır.
*Bedel:* galerinin en değerli talep tiplerinden biri bir spec daha bekler.

**D4 — Bot koruması yalnızca hız sınırıdır; captcha yoktur.**
*Gerekçe:* captcha üçüncü parti bağımlılığı ve ayrı bir KVKK değerlendirmesidir.
*Bedel:* hız sınırı kararlı bir saldırganı durdurmaz; yalnızca kazara ve kaba tekrarı keser.

**D5 — Sıradaki iki spec `Identity` ve `Notifications` olur.**
D2'nin doğrudan sonucu: 0005 tamamlandığında talepler doğru biçimde birikir ama okunamaz.
Bu durum, `Identity` (personel, rol, oturum) ve `Notifications` (danışmana ve ziyaretçiye
bildirim) spec'leri tamamlanana kadar sürer. *Gerekçe:* biriken ve okunmayan talep, her gün
büyüyen bir kayıptır; 0005'in değeri ancak bu ikisiyle açılır. *Bilerek ödenen bedel:* takas
(D3), talep paneli ve durum akışı (X2, X5) bu ikisinin arkasına düşer.
**Bu karar 0005'in önkoşulu değildir** — 0005 kendi başına tamamlanabilir; sıra, sonraki
spec'lerin planını bağlar.

**D6 — Onay metni söz vermez; yalnızca "alındı" der.**
*Gerekçe:* D1 + D2 birlikte şu anlama geliyor ve spec bunu kendi yazıyor: bırakılan talebi
bu aşamada **kimse görmez.** Ziyaretçiye "galeri size ulaşacak" demek, sistemin yapısal
olarak tutamayacağı bir söz vermektir; bu bir kod hatası değil, spec'in doğurduğu bir
dürüstlük sorunudur. *Karar:* onay metni talebin **alındığını** bildirmekle sınırlı kalır;
ulaşma sözü `Identity` ve `Notifications` tamamlandığında (D5) eklenir.
*Bilerek ödenen bedel:* onay ekranı daha soğuk okunur ve ziyaretçi ne zaman dönüleceğini
bilmez. *Elenen alternatif:* sözü vermeye devam edip D5'i hızlandırmak.

---

## Acceptance Criteria

> Gözlem noktası **C16**'dadır. `[görsel]` işaretli kriterler arayüz kriteridir ve
> `docs/testing.md` § 3 uyarınca görsel kanıt gerektirir.

**Talebin oluşması**

- **AC-1.** İlan detay sayfasından, adı ve telefonu girilmiş bir bilgi talebi bırakıldığında
  talep kaydedilir.
- **AC-2.** Ne telefon ne e-posta girilmiş bir talep kaydedilmez.
- **AC-3.** Adı girilmemiş bir talep kaydedilmez.
- **AC-4.** E.164 biçiminde olmayan bir telefon numarasıyla talep kaydedilmez.
- **AC-5.** Geçerli biçimde olmayan bir e-posta ile talep kaydedilmez.
- **AC-6.** Ad, e-posta veya mesaj alanı üst uzunluk sınırını aşan bir talep kaydedilmez.
- **AC-7.** Mesaj alanı boş bırakılarak bir talep kaydedilebilir.
- **AC-8.** Oluşturulan talep `Yeni` durumundadır.
- **AC-9.** Talep tipi bilgi talebi, geri arama, test sürüşü, fiyat teklifi ve kredi görüşmesi
  değerlerinden birini alır; bu beşi dışında bir tiple talep kaydedilmez.
- **AC-10.** Talep tipi **takas** olarak gönderilen istek reddedilir; takas sistemde tanınan
  bir tiptir ama bu spec'te kabul edilmez.

**İlanın talebe uygunluğu**

- **AC-11.** `[görsel]` `Rezerve` durumundaki bir ilanın detay sayfasında talep formu
  gösterilmez; yerinde formun neden kapalı olduğu ziyaretçinin dilinde yazar.
- **AC-12.** `Rezerve` durumundaki bir ilan için gönderilen talep sunucuda reddedilir ve
  kayıt oluşmaz.
- **AC-13.** Vitrinde görünür olmayan bir ilan için gönderilen talep reddedilir ve kayıt
  oluşmaz.
- **AC-14.** Hiç var olmayan bir ilan için gönderilen talep ile görünür olmayan bir ilan için
  gönderilen talep **aynı** karşılığı alır: aynı hata kodu, aynı gövde.
- **AC-15.** `[görsel]` Sayfa açıldıktan sonra ilan vitrinden düştüğünde gönderilen talep
  reddedilir; ekran aracın artık mevcut olmadığını bildirir, girilen bilgiler formda kalır ve
  listeye dönme eylemi sunulur.

**Talebin kendi gerçeğini koruması**

- **AC-16.** Talep, bırakıldığı locale ile kaydedilir; Arapça sayfadan bırakılan talebin
  locale'i `ar` olur.
- **AC-17.** Talep, ilgilenilen aracın **o andaki** model adını ve fiyatını kendi içinde taşır.
- **AC-18.** Anlık görüntüdeki model adı **kanonik Latin adıdır**; Arapça sayfadan bırakılan
  talepte de kanonik ad saklanır.
- **AC-19.** Talep oluşturulduktan sonra aracın fiyatı değiştirildiğinde talepteki fiyat
  **değişmez**.
- **AC-20.** Talep oluşturulduktan sonra araç arşivlendiğinde talep okunabilir kalır ve model
  adı ile fiyatını göstermeye devam eder.
- **AC-21.** Fiyatı "fiyat sorunuz" olan bir araç için bırakılan talebin anlık görüntüsü
  "fiyat sorunuz" durumunu taşır; tutar `0` olarak kopyalanmaz.
- **AC-22.** Kurulum para birimi KWD iken `1250,500` tutarlı bir araç için bırakılan talebin
  anlık görüntüsü tutarı **üç ondalıkla** saklar; yuvarlama veya kesme yapılmaz.

**Şube ve izin**

- **AC-23.** Araca bağlı talep, o aracın şubesine bağlanır.
- **AC-24.** İletişim izni verilerek bırakılan talepte izin **verildi** olarak kaydedilir.
- **AC-25.** İletişim izni verilmeden bırakılan talepte izin **verilmedi** olarak kaydedilir;
  talep yine de kaydedilir.
- **AC-26.** `[görsel]` Talep formunda iletişim izni kutusu **önceden işaretli gelmez**.
- **AC-27.** `[görsel]` İzin kutusunun yanında neye izin verildiğini söyleyen metin bulunur ve
  bu metin TR, AR ve EN locale'lerinde kendi dilinde görünür.

**Kalıcılık ve kişisel veri**

- **AC-28.** Kaydedilmiş bir talep silinmez; silme girişimi reddedilir.
- **AC-29.** Talebin kişisel alanları (ad, telefon, e-posta, mesaj) ile istatistiksel alanları
  (tip, şube, locale, tarih, anlık görüntü) ayrı ayrı okunabilir; kişisel alanlar
  boşaltıldığında kayıt ve istatistiksel alanları bozulmadan kalır.

**Duyuru**

- **AC-30.** Talep kaydedildiğinde `LeadCaptured` duyurusu üretilir.
- **AC-31.** Talep kaydedilmediğinde kaydedilmiş bir `LeadCaptured` duyurusu kalmaz.

**Kötüye kullanım ve tekrar**

- **AC-32.** Aynı IP'den 10 dakika içinde 5 talep kaydedildikten sonra altıncı istek
  reddedilir ve bu istek için kayıt oluşmaz.
- **AC-33.** Hız sınırına takılan istek `429` durum kodu ve `lead.rate_limited` hata kodu ile
  reddedilir.
- **AC-34.** Reddedilen istekte ziyaretçiye teknik ayrıntı gösterilmez.
- **AC-35.** Aynı idempotentlik anahtarıyla gönderilen ikinci istek yeni bir talep kaydı
  oluşturmaz.

**Form: üç durum** (`docs/frontend.md` § 3)

- **AC-36.** `[görsel]` Talep gönderilirken form gönderim düğmesi yükleniyor hâline geçer ve
  aynı talep iki kez gönderilemez.
- **AC-37.** `[görsel]` Talep başarıyla kaydedildiğinde ziyaretçiye talebinin **alındığı**
  bildirilir; galerinin kendisine ulaşacağına dair bir söz verilmez.
- **AC-38.** `[görsel]` Talep kaydedilemediğinde ekran, çeviri kataloğundaki hata mesajı
  anahtarının karşılığını ve **tekrar dene** eylemini gösterir; girilen bilgiler kaybolmaz.
- **AC-39.** Hata ekranında HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya
  istisna metni görünmez.
- **AC-40.** `[görsel]` Doğrulama hatası olan alan ziyaretçiye işaretlenir ve hatanın ne
  olduğu ziyaretçinin dilinde yazılır.

**Çok dillilik ve RTL**

- **AC-41.** `[görsel]` Form etiketleri, talep tipi adları ve onay metni TR, AR ve EN
  locale'lerinde kendi dilinde görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.
- **AC-42.** `[görsel]` Arapça locale'de form `dir="rtl"` ile basılır; alan etiketleri, hata
  metinleri ve düğmeler sağdan sola hizalanır.
- **AC-43.** Onay ve hata metinleri üç etkin dilin hepsinde yazılıdır ve her birinde bir
  sonraki eylemi söyler.

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] `[görsel]` işaretli **her** kriterin görsel kanıtı eklendi
      (`docs/evidence/0005-ac<n>.png`), başarılı ve hata durumları dâhil; AC-42 için Arapça
      görüntü de var.
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — `Leads` başka modülün tablosuna, internal tipine veya
      `DbContext`'ine erişmiyor.
- [x] Yeni migration yalnızca `Leads` şemasına dokunuyor.
- [x] `VehicleCatalog.Contracts`'a eklenen okuma yüzeyi yalnızca talep anlık görüntüsü için
      gereken alanları taşıyor; gizli alan sızdırmıyor.
- [x] `LeadCaptured` outbox'a talep kaydıyla **aynı transaction'da** yazılıyor (AC-30, AC-31).
- [x] Hız sınırı sayacı IP'yi kalıcı kayda veya loga yazmıyor (C17,
      `docs/conventions.md` § 12).
- [x] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [x] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır.
- [x] Ekran `docs/frontend.md` § 4.2 envanterini kullanıyor; ham piksel/hex eklenmedi.
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **2** | (1) 2026-09-22, AP-08. Uygulama başlamadan önce yapılan CLARIFY turu: 16 açık soru, hepsi onaylanıp işlendi. Kriter sayısı 28 → 43. En pahalı bulgu: spec `0002` § X1'in bu spec'e devrettiği `docs/domain.md` **R6** kuralı ("rezerve araçta iletişim formu kapalı") hiçbir bölümde yoktu. (2) 2026-09-22, uygulama sırasında. QA'nın BULGU-1 ve BULGU-3'ü **C17**'ye iki açıklayıcı not ekletti: eşiğin *istek* değil **talep** saydığı ve "aynı kaynak"ın proxy topolojisine bağlı olduğu. Kriterler değişmedi; kural metni netleşti. |
| **Düzeltme turu sayısı** | **2** | (1) PR öncesi kendi triyajım: beş bulgu, hepsi tek turda kapandı (BULGU-1…5). (2) PR sonrası **entegrasyon turu**: dal güncel `main`'e alınınca üç sorun çıktı ve düzeltildi. Bu ikinci tur, işin büyüklüğünden değil dalın 17 saat `main`'den kopuk kalmasından doğdu. |
| **Bulgu gerçek/gürültü oranı** | 4 / 0 | Uygulama sırasında kendi çıkardığım dört bulgunun dördü de gerçekti: (1) AC-22'nin verdiği `1250,500` sayısı kesmeyi yakalayamaz — iki ondalığa kesilse de aynı sayıdır; (2) AC-19'un ilk testi "kaynak değişti" kısmını hiç canlandırmıyordu ve hiçbir şey kanıtlamıyordu; (3) `TypedResults.Problem` hata gövdesine İngilizce `title` koyuyordu (Yasak Listesi 23); (4) form giriş metni TR ve EN'de Karar D6'nın yasakladığı ulaşma sözünü veriyordu. |
| **Regresyon sayısı** | **1** | Kendi dalında 0'dı: var olan 219 backend ve 44 frontend testinin hiçbiri kırılmadı. Ama `0006` ile birleşince `Host.Tests` AC-10 kırıldı — giriş ucu `identity.rate_limited` yerine `lead.rate_limited` döndürüyordu. `RateLimiterOptions.OnRejected` global tek bir kancadır ve iki modül de onu kuruyordu; son kayıt hepsini bastırıyordu. Red politikaya bağlandı, her modül kendi hata kodunu kendi taşıyor. |
| **Kaçan hata** | - | Henüz bilinmiyor. |

**Bu spec'ten çıkarılan ders:**
**Bir kriterin verdiği örnek, o kriteri kanıtlayamayabilir.** AC-22 "üç ondalıkla saklar,
kesme yapılmaz" diyor ve örnek olarak `1250,500` veriyor — ama o sayı iki ondalığa
kesildiğinde de aynı sayıdır. Spec'in cümlesi doğru, örneği testi körleştiriyordu. Aynı
şey AC-37'de de oldu: "söz verilmez" kuralını yalnız onay metninde arayan bir test,
formun giriş cümlesindeki sözü göremedi ve hata ancak görsel kanıta bakarken çıktı.

İkisinin ortak dersi şu: **testi kriterin metnine değil, kriterin KORUDUĞU ŞEYE göre
yazmak gerekir.** Kriterin örneğini kopyalamak testi yazdırır, ama yeşil kalmasını
sağlayan şey çoğu zaman kuralın kendisi değil, örneğin zayıflığıdır.

**Entegrasyondan çıkan üçüncü ders:** İki dalın ayrı ayrı yeşil olması, birleşimlerinin
yeşil olacağını göstermez. `OnRejected` gibi **global tek kancalar** modüler bir sistemde
sessiz çarpışma noktalarıdır: her modül kendi içinde doğru davranır, son kayıt kazanır ve
kimse fark etmez. Bunu yakalayan şey bir test değil, dalların birleştirilmesi oldu —
dolayısıyla asıl ders şu: **dal `main`'den ne kadar uzun kopuk kalırsa, bu sınıftan hata o
kadar geç ve o kadar toplu çıkar.**
