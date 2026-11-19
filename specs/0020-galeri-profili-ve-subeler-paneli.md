# 0020 — Galeri Profili ve Şubeler: Panelden Yönetim

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-18 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (B1–B11 → **D13–D23**) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (yazma uçları, görünen ad ve sürüm kolonları — migration, B5 kuralının sıkılaşması — **D13**) · `VehicleCatalog` (kapatılmış şubeye araç bağlanamaz — **D14**) · `frontend/` panel (galeri ve şubeler ekranları, araç formunun şube seçicisi) ve vitrin (üst çubuk, ana sayfa giriş satırı) · `Host` (görünen adın `index.html`'e basılması seçilirse — plan turu, **D16**) |
| **Bağımlı spec'ler** | `0003` (galeri profili ve şubeler — tamamlandı; bu kart onun **X1**'idir; **R4**'ü sıkılaşır — **D13**) · `0006` (roller — tamamlandı) · `0014` (ana sayfa — tamamlandı; giriş satırı ve altlık bu kartın verisini okur; **D6**'sı ve **D15**'in izin listesi değişir — **D4**, **D15**) · `0012b` (A8: şube kapanınca araçların yayından kalkması — tamamlandı) · `0010a` (araç formu şube seçicisi — tamamlandı; **D14**) |
| **Dayanak** | `0003` **X1** ("yazma yüzeyi, `Identity` spec'inden sonra ayrı bir spec'tir") · ADR `0012` § Açık engeller 1 · depo sahibinin 2026-11-18 sırası (**D1**) |
| **Karar bekliyor** | yok — **D1–D12** verildi; QA CLARIFY turunun on bir bulgusu (B1–B11) → **D13–D23** (§ Verilen kararlar). |

---

## Intent

Galeri, sitesinde kendisi hakkında görünen bilgiyi **değiştiremiyor.** Kurulum her yeni
siteye "Oto Galeri" adında örnek bir galeri ve iki uydurma İstanbul şubesi yazıyor; bunları
düzeltecek bir ekran yok ve bugün tek yol veritabanına elle komut yazmak. Bunun somut
sonucu: site bu hâliyle ziyaretçiye açılırsa ana sayfanın altında **başka birinin adresi ve
telefonu** görünür, galeri yeni bir şube açtığında ya da bir şubeyi kapattığında bunu sitede
gösteremez, ve sitenin üstündeki galeri adı galerinin değil yazılımın verdiği addır.

Bunu **galeri yöneticisi** istiyor. Başarı şuna benziyor: yönetici panelden galerinin adını
ve sicil bilgilerini düzeltir, şubeleri ekler, düzenler, geçici kapatır ya da kalıcı olarak
kapatır; değişiklik ziyaretçinin bir sonraki sayfa açışında sitede görünür. Sitenin üstündeki
galeri adı galerinin kendi adıdır ve ziyaretçiyi her sayfadan ana sayfaya götürür. Canlıya
çıkışın önündeki engel kalkar.

---

## Requirements

### Galeri profili

- **R1.** Yönetici panelde galerinin **profilini** görür ve düzenler: **ticari unvan**,
  **vergi numarası**, **ticaret sicil numarası** ve **görünen ad**.
- **R2.** **Ticari unvan** galerinin yasal adıdır; tek dildedir ve **zorunludur**. Vergi ve
  ticaret sicil numarası **opsiyoneldir** ve boşaltılabilir.
- **R3.** **Görünen ad** ziyaretçiye gösterilen addır ve **dil başına** tutulur. Varsayılan
  dildeki karşılığı **zorunludur**; diğer etkin dillerdeki karşılık opsiyoneldir ve yoksa
  varsayılan dildeki gösterilir (**D4**).
- **R3a.** Metin alanları sınırlıdır (**D22**): ticari unvan 200, vergi ve ticaret sicil
  numarası 40, görünen ad dil başına 120 karakter. Şubede ad 120, adres 300 karakter (dil
  başına); telefon ve WhatsApp 16, e-posta 254, saat dilimi 60, adres kısaltması 80 karakter.
  Sınırı aşan değer alan hatasıyla reddedilir.

### Vitrinde galeri adı

- **R4.** Vitrinin her sayfasının üst çubuğu, ziyaretçinin dilindeki **görünen adı** gösterir.
- **R5.** Üst çubuktaki ad, ziyaretçinin dilindeki **ana sayfaya** götüren bir bağlantıdır
  (**D5**).
- **R5a.** Görünen ad okunamazsa üst çubukta adın yeri **boş** kalır ve sayfanın geri kalanı
  çalışır; teknik bir metin ya da yazılımın kendi adı gösterilmez (**D16**). Ad **metin
  olarak** gösterilir: içindeki `<`, `"`, `&` gibi karakterler işaretleme olarak yorumlanmaz.
- **R6.** Ana sayfanın giriş bölümündeki galeri adı satırı da **görünen adı** gösterir
  (`0014` **D6**'yı değiştirir — **D4**).
- **R7.** Profilde yapılan bir değişiklik ziyaretçiye **bir sonraki sayfa açışında** görünür
  (**D6**).

### Şubeler

- **R8.** Yönetici panelde galerinin **bütün şubelerini** — aktif, geçici kapalı ve
  kapatılmış — durumlarıyla birlikte bir listede görür.
- **R9.** Yönetici **yeni şube açar.** Zorunlu alanlar `0003` **R2**'ninkilerdir: varsayılan
  dildeki ad ve adres, telefon, saat dilimi ve adres kısaltması (slug). Opsiyoneller:
  diğer dillerin ad ve adresi, WhatsApp numarası, e-posta, koordinat.
- **R10.** Yönetici aktif ya da geçici kapalı bir şubenin **R9'daki bütün alanlarını**
  düzenler. Opsiyonel bir alan **boşaltılabilir** (**D9**). **Kapatılmış** şube düzenlenemez:
  kapatılmış şube geçmiş kayıtların referansıdır (`0003` **R3**, **D21**).
- **R11.** Yönetici bir şubeyi **geçici kapatır** ve geçici kapalı bir şubeyi **yeniden
  açar.** Geçici kapatma son aktif şubede de mümkündür (`0003` **D3**).
- **R12.** Yönetici bir şubeyi **kalıcı olarak kapatır.** Kalıcı kapatma **geri alınamaz**
  (`0003` **R3**) ve şubedeki araçlar yayından kalkar (`docs/domain.md` **A8**); yönetici bu
  iki sonucu kapatmadan **önce** görür ve açıkça onaylar (**D8**).
- **R12a.** Kalıcı kapatma aktif ve **geçici kapalı** şubede sunulur (**D21**).
- **R13.** Galeride **başka aktif şube yoksa** hiçbir şube kalıcı olarak kapatılamaz — kapatılan
  şube aktif de olsa geçici kapalı da olsa (**B5**, **D13**). Deneme reddedilir ve yönetici
  nedenini görür.
- **R14.** Şube açma, şube adı değişikliği ve kalıcı kapatma `0003` **R7–R9**'daki duyuruları
  bugünkü gibi üretir; panelden yapılan değişiklik bunların hiçbirini atlamaz.

- **R13a.** Kapatılmış bir şubeye araç **bağlanamaz**: yeni araç kaydında ya da aracın şubesi
  değiştirilirken kapatılmış şube reddedilir ve panelin araç formu kapatılmış şubeyi seçenek
  olarak sunmaz. Şubesi değişmeden kaydedilen bir araç bu kurala takılmaz (**D14**).

### Kurallar, eşzamanlılık, erişim

- **R15.** Şube alanları `0003`'ün kurallarıyla doğrulanır: telefon ve WhatsApp uluslararası
  biçimde, e-posta geçerli bir adres, saat dilimi bir **IANA bölge kimliği**, adres kısaltması
  küçük harfli ASCII ve galeri genelinde — **kapatılmış şubeler dâhil** — **tekil**. Kural
  dışı bir değer kaydedilmez ve yönetici hangi alanın neden reddedildiğini görür; tekillik
  ihlali de bir alan hatasıdır, sunucu hatası değil (**D19**).
- **R16.** Aynı profili ya da şubeyi **eski bir sürüm üzerinden** kaydetmek reddedilir; yönetici
  bunu bir uyarıyla görür ve güncel hâli yükleyebilir (**D7**, **D17**). Durum eylemleri
  (geçici kapat, yeniden aç, kalıcı kapat) sürümü ilerletir.
- **R17.** Galeri ve şube ekranları ile yazma uçları **yalnız Yönetici** içindir. Diğer roller
  ekranlara gezinme bağlantısı görmez, adresi doğrudan açtıklarında "erişim yok" ekranını
  görür ve uçlar onlara `403` döner (**D2**, **D23**).
- **R18.** Panel ekranları yükleniyor, boş ve hata durumlarını ele alır
  (`docs/frontend.md` § 3), üç dilde ve Arapçada sağdan sola çalışır.

---

## Constraints

### Uyulması zorunlu

- **C1.** Galeri ve şube verisi yalnız `Dealership`'te yaşar; vitrin ve diğer modüller onu
  yalnız `Dealership.Contracts` üzerinden okur (Yasak Listesi 1, `0003` **R12**).
- **C2.** Duyurular değişiklikle **aynı işlemde** outbox'a yazılır (`0003` **R11**,
  `docs/architecture.md` § 2.C). Yazma uçları agregatın kendi yöntemlerini çağırır;
  durum ya da alan doğrudan atanmaz.
- **C3.** B5 kontrolü eşzamanlı iki kapatmada da tutar: iki farklı aktif şubenin aynı anda
  kalıcı kapatılması galeriyi aktif şubesiz bırakamaz (`0007`, `InvariantGuard` —
  bugünkü `BranchCommands.CloseAsync` zaten bunu yapıyor).
- **C4.** Yeni migration yalnız `dealership` şemasına dokunur: `galleries`'e görünen ad ve
  sürüm kolonu, `branches`'a sürüm kolonu ekler. Var olan kurulumlarda görünen adın varsayılan
  dildeki karşılığı **ticari unvandan** doldurulur; hiçbir kurulum görünen adsız kalmaz
  (**D4**, **D17**).
- **C5.** Kullanıcıya görünen hiçbir metin koda gömülmez; uçlar hata **kodu** döner ve panel
  onu çevirir (Altın Kural 6). Veritabanı kısıtı ihlali (tekillik, uzunluk) kullanıcıya bir
  alan hata kodu olarak döner; `500` dönmez (**D19**, **D22**).
- **C6.** Üst çubuktaki ad sayfa ilk boyandığında **yerinde** durur: ad gelirken üst çubuk
  yer değiştirmez ve içerik kaymaz (`0014` performans kapısı, CLS) (**D6**).

### KAPSAM DIŞI

- **X1.** **Galeri logosu** — *neden:* yükleme, türev, üst çubukta gösterme ve filigranla
  ilişkisi ayrı kararlar; ad bugünkü engeli tek başına kaldırıyor (**D3**).
- **X2.** **Sosyal hesaplar** — *neden:* hiçbir vitrin yüzeyi göstermiyor; düzenlemek,
  görünmeyen bir veriyi düzenlemek olurdu (**D3**).
- **X3.** **Çalışma saatleri** — `0003` **X2**'nin ayırdığı kart; bu kart onu açmaz.
- **X4.** **Şube sayfası** ve vitrinde WhatsApp, e-posta, koordinat gösterimi — *neden:*
  `0003` **X3**; bu kart yalnız veriyi düzenlenebilir kılar, yeni bir vitrin yüzeyi açmaz.
- **X5.** **Kurulum adımının örnek galeri ve şubeleri yazmayı bırakması** — *neden:* galeri
  şubesiz başlarsa **B5** ilk andan kırılır; bu kartla örnek veri ilk girişte düzeltilebilir
  hâle geliyor (**D10**).
- **X6.** **Kapatılmış şubenin silinmesi** — *neden:* kapatılmış şube geçmiş kayıtların
  (araç, talep) referansıdır; `0003` **R3** onu terminal tutuyor.
- **X7.** **Panelin kendi üst çubuğundaki ad** — *neden:* istek vitrin içindi; panel kendi
  başlığını taşıyor.
- **X8.** **Şubenin medya varlıkları** (`AttachMediaAsset`) — *neden:* hiçbir yüzey
  göstermiyor; X4 ile aynı gerekçe.

---

## Context

### Mevcut davranış — ölçüldü (2026-11-18, `main` @ `674011a`)

- **Yazma yüzeyi yok.** `Dealership`'in tek yönetim ucu `GET /api/admin/branches`
  (`BranchEndpoints.cs`); galeri için yalnız public okuma var (`GET /api/dealership/gallery`,
  `PublicGalleryEndpoints.cs`). Panelde galeri ya da şube ekranı yok.
- **Alan kuralları hazır.** `Branch.cs`: `Open` (varsayılan dilde ad ve adres zorunlu,
  `BranchWasOpened`), `ChangeDisplayNames` (`BranchDisplayNameWasChanged`), `ChangeAddresses`,
  `ChangePhone`, `ChangeTimeZone`, `ChangeSlug`, `CloseTemporarily` (son aktif şubede de
  serbest — `0003` D3), `Reopen` (yalnız geçici kapalıdan), `Close(otherActiveBranchCount)`
  (B5, `BranchWasClosed`). `Gallery.cs`: `Create`, `SetRegistration`, `SetLogo`,
  `AddSocialLink`. **Ticari unvanı değiştiren bir yöntem yok.**
- **Opsiyonel alanlar boşaltılamıyor.** WhatsApp, e-posta ve koordinat için yalnız `Set…`
  var; temizleyen yöntem yok (**D9**).
- **Kalıcı kapatma komutu var, ucu yok.** `BranchCommands.CloseAsync` satır kilidi ve B5
  kontrolüyle yazılmış ve `SubeKapatmaTests` ile sınanıyor, ama hiçbir uç onu çağırmıyor.
- **Kurulum adımı örnek veri yazıyor.** `SeedInitialDealership` migration'ı: "Oto Galeri",
  vergi no `1234567890`, sicil `TR-123456`, iki şube (Kadıköy, Ataşehir; `+90216…`
  telefonları, `kadikoy@otogaleri.com.tr`). ADR `0012` canlıya benzer koşuda ölçtü: boş bir
  veritabanında yalnız kurulum adımı koşunca ana sayfanın altlığında bu iki şube görünüyor.
- **Üst çubuktaki ad yazılımın.** `frontend/src/app/TopBar.tsx` adı `app.brandName` çeviri
  anahtarından alıyor ("Oto Galeri", Arapçada "أوتو غاليري") ve bağlantı değil. Dosyanın
  kendi yorumu doğru kaynağın galeri profili olduğunu söylüyor.
- **Ana sayfanın giriş satırı ticari unvanı gösteriyor** (`0014` **D6**, `GalleryName.tsx`).
  Ticari unvan tek dilde olduğu için Arapça sayfada Latin harfli "Oto Galeri" görünüyor
  (`0014` kanıt görüntüleri).
- **Roller.** `AdminSurface.ManagerPolicy` Yönetici'yi, `AuthenticatedPolicy` her oturumu
  karşılıyor; `GET /api/admin/branches` bugün `AuthenticatedPolicy` ile, çünkü araç formu
  şube listesini her rolde okuyor — o okuma bu kartta değişmez.
- **Eşzamanlılık deseni.** `0017` sürüm kolonunu `IsConcurrencyToken` ile kullanıyor
  (`ContentPageConfiguration.cs:40`); bu kart aynı deseni izler (**D7**). `galleries` ve
  `branches`'ta bugün sürüm kolonu **yok** (`GalleryConfiguration.cs`, `BranchConfiguration.cs`).
- **B5 iki adımda deliniyor** (QA B1). `Branch.Close` yalnız "aktif **ve** başka aktif yok"
  durumunu reddediyor; son aktif şube önce geçici kapatılır (serbest), sonra kalıcı kapatılırsa
  geçer ve galerinin bütün şubeleri `Kapatılmış` olur. Bugün bu yolu açan bir uç yok; bu kart
  açacaktı (**D13**).
- **Kapatılmış şubeye araç bağlanabiliyor** (QA B2). `VehicleWriteRules.ValidateBranchAsync`
  (`Application/Vehicles/VehicleWriteRules.cs:197`) yalnız şubenin var olup olmadığına bakıyor;
  araç formu kapatılmış şubeyi de seçenek olarak gösteriyor ve kendi yorumu "sunucu kapalı
  şubeye kaydı reddetmiyor" diyor (`VehicleFormPage.tsx:183`). A8 işleyicisi yalnız kapanış
  anındaki araçları işaretliyor (`BranchEventHandlers.cs`) (**D14**).
- **Public galeri yanıtının izin listesi ölçülüyor** (QA B3). `GaleriPublicUcTests.cs:23` yanıtın
  anahtarlarını `["branches","commercialName"]` olarak birebir sınıyor (`0014` **D15**);
  `GaleriPublicOkumaTests` aynı unvanı tr ve ar için bekliyor. Bu kart izin listesini
  `commercialName` → `displayName` olarak değiştirir ve o testler kartın parçası olarak
  güncellenir — gerileme değil (**D15**).
- **A8 işleyicisi** yayındaki **ve rezerve** ilanları yayından kaldırıyor, taslak dâhil bütün
  araçları "lokasyon ataması bekliyor" olarak işaretliyor; satılmış ve arşivlenmiş araçlara
  dokunmuyor (`BranchEventHandlers.cs:95-113`) (**D18**).
- **Adres kısaltmasının tekil indeksi** kapatılmış şubeleri de kapsıyor
  (`BranchConfiguration.cs:93`); ihlali önceden yakalayan bir alan kontrolü ve hata kodu yok,
  bugün veritabanı istisnası olarak düşüyor (**D19**).
- **Şema sınırları.** `galleries`: unvan 200, vergi/sicil 40; `branches`: telefon/WhatsApp 16,
  e-posta 254, saat dilimi 60, kısaltma 80; ad ve adres `jsonb` — sınırsız (**D22**).

### Geçerli kurallar

`docs/domain.md` **B5** (en az bir aktif şube), **A8** (kapanan şubenin araçları yayından
kalkar); `0003` **R2–R12**, **D3** (kapatılmış terminal; geçici kapalı A8'i tetiklemez).

### Bilinen tuzaklar

1. **İki duyuru yolu.** Ad değişikliği `BranchDisplayNameWasChanged` üretir, adres değişikliği
   üretmez (`0003` AC-22). Tek bir "şubeyi güncelle" ucu bütün alanları atarken adı değişmeyen
   bir kaydetmede de `ChangeDisplayNames` çağırırsa **boş yere** duyuru üretir; vitrin okuma
   modeli her kaydetmede yeniden yazılır. Ad yalnız değiştiyse çağrılmalı.
2. **B5 yarışı.** Kapatma bir satır kilidiyle sayıyor; düzenleme ucu durum alanına dokunmaz,
   durum yalnız üç eylem ucundan değişir.
3. **Üst çubuk her sayfada.** Adı her sayfada bir istekle çekmek her sayfaya bir gidiş-dönüş
   ekler ve ad gelene kadar üst çubuk boş kalır (C6). Etkin diller aynı sorunu `0014` **D22**'de
   sunucunun bastığı bir `<meta>` ile çözdü; plan turu aynı yolu değerlendirir.
4. **Varsayılan dil kuruluma göre değişir.** Görünen adın zorunlu karşılığı yapılandırmadaki
   varsayılan dilinkidir; kod `tr`'yi varsaymaz.

### Verilen kararlar — 2026-11-18

**D1 — Sıra (depo sahibi kararı, 2026-11-18).** Sahip beş işi şu sırayla onayladı: ikinci adım
kurulum hatası → **bu kart** → marka/model → ana sayfa metinleri → Satıldı'yı geri alma.
Üstteki galeri adının ana sayfaya bağlanması bu karta verildi.

**D2 — Yalnız Yönetici (Takım Yöneticisi (ajan) kararı, 2026-11-18).** *Gerekçe:* galerinin
yasal bilgisi ve lokasyonları kurulum düzeyinde veridir; kalıcı kapatma araçları yayından
kaldırır. İçerik Editörü'nün `0017`'deki metin yetkisi bunu kapsamaz.

**D3 — Logo ve sosyal hesaplar kapsam dışı (ajan, 2026-11-18).** (X1, X2) *Gerekçe:* engeli ad,
sicil ve şubeler kaldırıyor; logo kendi kararlarını getiriyor, sosyal hesapları gösteren bir
yüzey yok.

**D4 — Görünen ad, ticari unvandan ayrı ve dil başına (ajan, 2026-11-18).** (R3, R4, R6, C4)
Galeri profiline dil başına bir **görünen ad** eklenir; üst çubuk ve ana sayfanın giriş satırı
onu gösterir. Ticari unvan yasal ad olarak kalır ve vitrinde gösterilmez. *Gerekçe:* bugün üst
çubuk Arapçada "أوتو غاليري" yazıyor; ticari unvana geçmek Arapça sitede adı Latin harfe
düşürürdü — ölçülmüş bir gerilemedir. Ticari unvanı dil başına yapmak ise yasal adı
çevrilebilir bir metne çevirirdi. Depo sahibinin ana sayfa metinleri işinde seçtiği "üstteki
küçük galeri adı" bu alanla karşılanır; o kart bu satırı yeniden açmaz.

**D5 — Ad bağlantısı ziyaretçinin dilindeki ana sayfaya (ajan, 2026-11-18).** (R5) *Gerekçe:*
dil değiştirici dil seçimini korur; ad bağlantısı onu sıfırlarsa ziyaretçi seçtiği dili
kaybeder.

**D6 — Önbellek yok; değişiklik sonraki sayfa açışında (ajan, 2026-11-18).** (R7, C6)
*Gerekçe:* profil nadiren değişir ama değiştiğinde yanlış adı göstermek, adı geç göstermekten
pahalıdır. Adın nasıl taşınacağı (sunucunun bastığı değer ya da istek) plan turunun işidir;
şart C6'dır.

**D7 — Eski sürümle kaydetme reddedilir (ajan, 2026-11-18).** (R16) `0017` **D18**'in deseni.
*Gerekçe:* aynı şubeyi iki sekmede düzenleyen yönetici sessizce kendi değişikliğini ezerdi;
desen depoda hazır.

**D8 — Kalıcı kapatma iki sonucu söyleyen bir onayla (ajan, 2026-11-18).** (R12) Onay penceresi
"geri alınamaz" ve "bu şubedeki yayındaki araçlar yayından kalkar" der. Araç **sayısını**
göstermez. *Gerekçe:* sayı `VehicleCatalog`'tan bir okuma ister ve onay ile kapatma arasında
değişebilir; yanlış sayı söyleyen onay, sayı söylemeyen onaydan kötüdür.

**D9 — Opsiyonel alanlar boşaltılabilir (ajan, 2026-11-18).** (R10) `Branch`'e WhatsApp, e-posta
ve koordinatı temizleyen yöntemler eklenir. *Gerekçe:* örnek verideki
`kadikoy@otogaleri.com.tr` gibi değerler başka türlü kaldırılamaz.

**D10 — Kurulum adımı değişmez (ajan, 2026-11-18).** (X5) ADR `0012` / `docs/deploy.md` § 5,
"ziyaretçiye açmadan önce profil ve şubeler düzeltilir" adımını bu kartın ekranlarına
bağlayacak biçimde güncellenir.

**D11 — Saat dilimi listeden seçilir (ajan, 2026-11-18).** (R15) Panel, tarayıcının bildiği
IANA bölge kimliklerini bir liste olarak sunar; sunucu `0003` **D8**'in tzdata doğrulamasıyla
yine reddeder. *Gerekçe:* serbest metin `+03:00` gibi değerleri davet eder ve hatayı ancak
kaydetmede gösterir.

**D12 — Adres kısaltması elle girilir (ajan, 2026-11-18).** (R9, R15) Ad yazılırken öneri
üretilmez. *Gerekçe:* Türkçe ve Arapça adlardan ASCII kısaltma üretmek kendi kuralını ister
(Yasak Listesi 27); alan zaten zorunlu ve doğrulanıyor.

### Verilen kararlar — QA CLARIFY turu, 2026-11-18

QA spec'i `main` @ `674011a`'ya karşı okudu ve on bir bulgu getirdi; Context'in bütün ölçüm
iddialarını doğruladı. Önerilerin **tamamı** Takım Yöneticisi (ajan) tarafından kabul edildi.

**D13 — B5 tek koşulla: başka aktif şube yoksa kalıcı kapatma yok (B1).** (R13) Kapatılan
şubenin durumu ne olursa olsun, galeride **başka** aktif şube yoksa kapatma reddedilir;
`Branch.Close` buna göre sıkılaşır, kilit deseni aynı kalır. *Gerekçe:* B5'in amacı galerinin
en az bir yaşayan şubesi olması; "önce geçici kapat, sonra kapat" dizisi bugün onu deliyor ve
bu kart o diziyi panele açıyor.

**D14 — Kapatılmış şubeye araç bağlanamaz (B2).** (R13a) Yeni araç kaydı ve şube değişikliği
kapatılmış şubeyi reddeder; formun seçicisi onu sunmaz. Şubesi değişmeden kaydedilen araç
takılmaz — o araç A8 ile zaten "şube ataması bekliyor"dadır ve yöneticinin onu düzeltebilmesi
gerekir. *Gerekçe:* kapatmayı gerçek bir eylem hâline getiren bu kart; A8'in güvenlik ağı
sonradan bağlanan araçta delinirse kapatma yarım kalır.

**D15 — `0014` D15'in izin listesi değişir (B3).** Public galeri yanıtı `commercialName` yerine
`displayName` taşır; `GaleriPublicUcTests` ve `GaleriPublicOkumaTests` bu kartta güncellenir ve
PR'da "beklenen değişiklik" olarak gösterilir.

**D16 — Üst çubuğun yokluk durumu ve metin güvenliği (B4).** (R5a) Ad okunamazsa yer boş kalır
(`0014` AC-54'ün deseni). Ad nasıl taşınırsa taşınsın metin olarak basılır; özel karakterli bir
adla kriter yazılır. Adın taşıma yolu (sunucunun bastığı değer ya da istek) yine plan turunun.

**D17 — Sürüm kolonları ve `409` (B5).** (R16, C4) `galleries` ve `branches` sürüm kolonu alır;
eski sürümle kaydetme `409` ve bir hata koduyla reddedilir (`0017` **D18**). Durum eylemleri
sürümü ilerletir: düzenleme formu açıkken şube geçici kapatılırsa formun kaydı reddedilir ve
yönetici güncel durumu görür.

**D18 — Onay metni A8'in gerçek sonucunu söyler (B8).** (D8'i düzeltir, R12) Metin: "Bu işlem
geri alınamaz. Bu şubedeki araçlar vitrinden kalkar ve şube ataması bekler."

**D19 — Kısaltma tekilliği alan hatası (B7).** (R15, C5) Tekillik kapatılmış şubeleri de kapsar
(bugünkü indeks) ve ihlal `branch.slug_taken` gibi bir alan koduna çevrilir; `500` dönmez.

**D20 — Ad duyurusu dil başına (B6).** `0003` **R8**: herhangi bir dildeki ad değişikliği
duyurulur; ad ve adres birlikte değiştiğinde **bir** duyuru yazılır.

**D21 — Düzenleme ve kalıcı kapatma hangi durumda (B9).** (R10, R12a) Düzenleme aktif ve geçici
kapalı şubede; kalıcı kapatma aktif ve geçici kapalı şubede (D13'ün kuralıyla) sunulur.
Kapatılmış şube salt okunurdur.

**D22 — Uzunluk sınırları (B10).** (R3a) Var olan şema sınırları korunur; sınırı olmayan
alanlara (görünen ad, şube adı ve adresi) sınır konur. *Gerekçe:* bugün sınırı aşan değer
veritabanı hatasıyla düşer; sınırsız `jsonb` altlığı ve üst çubuğu taşırabilir (`0017` **D22**
emsali).

**D23 — Doğrudan adresle gelen yetkisiz rol (B11).** (R17) Satış Danışmanı ve İçerik Editörü
ekranın adresini doğrudan açtığında "erişim yok" ekranını görür (`AdminApp.tsx`'in var olan
`admin-no-access` deseni); `403` "getirilemedi" hatası olarak gösterilmez.

### Verilen kararlar — plan turu, 2026-11-18

> **P1–P7 onaylandı** — Takım Yöneticisi (ajan) kararı, 2026-11-19. P3 için AC-30'un metni
> `400` → `422` olarak düzeltildi; bu `main`'e girdikten sonraki ilk Acceptance Criteria
> değişikliğidir ve SCORECARD'da **revizyon 1** sayılır. Hata spec'in kendisindeydi:
> `docs/conventions.md` § 5 doğrulama hatasını `422` sayıyor ve kriter turu bunu okumadı.

**Developer (ajan) önerisi; Takım Yöneticisi onayı PR'da bekleniyor.** D1–D23'ü değiştirmez,
nasıl uygulanacaklarını sabitler. Her biri PR'ın "Açık konular" bölümünde de listelidir.

**P1 — Görünen ad sunucunun bastığı bir `<meta>` ile taşınır (D6, D16).** Host `index.html`'i
basarken `<meta name="gallery-display-name" content="…">` yazar (`0014` D22'nin
`enabled-locales` deseni); değer ziyaretçinin dilinde, yoksa varsayılan dildedir ve HTML
olarak kodlanır. Üst çubuk adı ilk boyamada senkron okur — ayrı istek yok, yer değişmez (C6).
Okuma düşerse etiket hiç basılmaz ve yer boş kalır (R5a). Dil değiştirici tam sayfa gezinmesi
yaptığı için (frontend-001 C8) etiket her zaman sayfanın dilindedir. *Gerekçe:* C6'yı istekle
karşılamak bir iskelet ve bir gidiş-dönüş ister; desen depoda hazır. *Bedel:* her vitrin
belgesi tek satırlık bir okuma yapar (önbellek yok — D6).

**P2 — Görünen adın ilk değerini kurulum adımı yazar, migration değil (C4).** Migration
`display_names`'i boş bir haritayla ekler; kurulum adımı (ADR `0002`) migration'lardan sonra
varsayılan dildeki adı ticari unvandan doldurur (`DealershipSchema`, idempotent: yalnız
varsayılan dilde adı olmayan satır). *Gerekçe:* varsayılan dil yapılandırmadır (Bilinen tuzak
4); SQL'e bir dil kodu yazmak Arapça varsayılanlı bir kurulumda adı yanlış dile koyardı.

**P3 — Alan hataları `422` döner; AC-30'daki `400` değil.** `docs/conventions.md` § 5
doğrulama hatasını `422` sayar ve depoda `400` dönen tek bir uç yok; tekillik ihlali öteki alan
hatalarıyla aynı gövdeyle (`dealership.validation_failed`, `fields`, alan başına kod) döner.
Eski sürüm, geçersiz geçiş, son aktif şube ve kapatılmış şubenin düzenlenmesi `409`.
*Öneri:* AC-30'un metni `422` olarak düzeltilir (sayılan bir revizyon); karar Takım
Yöneticisinin.

**P4 — Durum eylemleri de okunan sürümü taşır.** Geçici kapatma, yeniden açma ve kalıcı
kapatma gövdede `version` alır; eski sürümde `409`. Kalıcı kapatmada sürüm kilitten sonra
okunan agregata karşı denetlenir. *Gerekçe:* R16 eylemlerin sürümü ilerlettiğini söylüyor;
ilerlemiş sürüme karşı eski bir ekrandan yapılan eylem de aynı sessiz ezmedir.

**P5 — Şubeler ekranı var olan listeyi okur; ayrıntı ve yazma uçları yalnız Yönetici.** Liste
`GET /api/admin/branches`'tir (AC-68: değişmez); `GET/PUT /api/admin/branches/{id}`,
`POST /api/admin/branches`, üç eylem ucu ve `GET/PUT /api/admin/gallery`
`AdminSurface.ManagerPolicy`'li tek bir gruptadır. `0007`/AC-8'in mimari testi ("şube kapatma
komutunun HTTP ucu yoktur") `0003` X1'in bu kartı beklediğini söylüyordu; test, yazma uçlarının
yalnız Yönetici politikalı grupta açıldığını ölçen bir kapıya çevrildi.

**P6 — Etkin olmayan dil bir alan hatasıdır.** Görünen ad ve şube adı/adresi yalnız etkin
dillerde kabul edilir (`dealership.locale_not_enabled`); `AddDealership` etkin dil listesini
yapılandırmadan alır.

**P7 — Aracın kayıtlı şubesi kapatılmışsa form onu seçilemez bir seçenek olarak gösterir.**
Seçici kapatılmış şubeleri sunmaz (AC-58); aracın kendi şubesi kapatılmışsa o şube seçili ama
devre dışı bir seçenek olarak kalır ve altında "başka bir şube seçin" uyarısı çıkar. Böylece
şubeye dokunmadan kaydetme (AC-57) mümkün kalır.

### Uygulama planı (2026-11-18)

Uygulama dalı kriterler `main`'e girdikten (#103) sonra açıldı.

1. `Dealership` domain ve uygulama: görünen ad, sürüm kolonları, D9'un temizleme yöntemleri,
   D13'ün sıkılaşması, alan kuralları ve sınırları (D22), komutlar ve panel okumaları;
   migration (yalnız `dealership`) ve kurulum adımının doldurması (P2). `VehicleCatalog`'da
   D14. Uçlar (P3–P5) ve public yanıtın izin listesi (D15).
2. Host: görünen adın `<meta>` ile basılması (P1).
3. Testler: domain, PostgreSQL üzerinde komutlar, Host üzerinden roller, `409`, A8 zinciri.
4. Ön yüz: üst çubuk ve giriş satırı, panelde galeri ve şubeler ekranları, araç formu (P7),
   çeviriler; Vitest.
5. E2E, görsel kanıt, belgeler (`docs/domain.md`, `docs/deploy.md` § 5, ADR `0012`).

---

## Acceptance Criteria

### Galeri profili (R1, R2, R3 · D4)

- **AC-1.** Yönetici galeri ekranında ticari unvanı, vergi numarasını, ticaret sicil numarasını ve etkin her dil için görünen adı görür.
- **AC-2.** Yönetici ticari unvanı değiştirip kaydettiğinde ekran yeniden açıldığında yeni unvan görünür.
- **AC-3.** Boş ya da yalnız boşluktan oluşan bir ticari unvan reddedilir ve hata alanın yanında gösterilir; kayıtlı unvan değişmez.
- **AC-4.** Vergi numarası ve ticaret sicil numarası boşaltılıp kaydedildiğinde ikisi de boş kaydedilir.
- **AC-5.** Varsayılan dildeki görünen ad boş bırakılarak yapılan kaydetme reddedilir; kayıtlı ad değişmez.
- **AC-6.** Varsayılan olmayan bir dildeki görünen ad boş bırakılarak kaydedilebilir.
- **AC-7.** Migration'dan sonra var olan bir kurulumda varsayılan dildeki görünen ad ticari unvana eşittir ve diğer dillerin görünen adı boştur.
- **AC-8.** 200 karakteri aşan ticari unvan, 40 karakteri aşan vergi ya da sicil numarası ve 120 karakteri aşan görünen ad alan hatasıyla reddedilir; `500` dönmez (R3a, D22).

### Vitrinde galeri adı (R4, R5, R6, R7 · C6 · D4, D5, D6)

- **AC-9.** Vitrinin ana sayfasında, araç listesinde, ilan detayında ve bir içerik sayfasında üst çubuk ziyaretçinin dilindeki görünen adı gösterir.
- **AC-10.** Ziyaretçinin dilinde görünen ad yoksa üst çubuk varsayılan dildeki görünen adı gösterir.
- **AC-11.** Üst çubuktaki ada tıklamak ziyaretçiyi `/{locale}/` ana sayfasına götürür ve `{locale}` ziyaretçinin o anki dilidir.
- **AC-12.** Arapça sayfada üst çubuktaki ad Arapça görünen adı gösterir ve sağdan sola düzende doğru hizalanır.
- **AC-13.** Ana sayfanın giriş bölümündeki galeri adı satırı ziyaretçinin dilindeki görünen adı gösterir; ticari unvanı göstermez.
- **AC-14.** Yönetici görünen adı değiştirip kaydettikten sonra açılan ilk vitrin sayfası yeni adı gösterir.
- **AC-15.** Üst çubuk sayfanın ilk boyamasında görünen adla gelir: ad sonradan yüklenip üst çubuğu ya da içeriği kaydırmaz (ana sayfa ve liste için performans kapısının CLS eşiği yeşil kalır).
- **AC-16.** Ticari unvan, vergi numarası ve ticaret sicil numarası hiçbir vitrin sayfasında ve vitrinin okuduğu hiçbir public yanıtta yer almaz; public galeri yanıtının anahtar kümesi `displayName` ve `branches`'tır (D15).
- **AC-17.** Görünen ad okunamadığında üst çubukta adın yeri boş kalır, yazılımın adı ya da teknik bir metin gösterilmez ve sayfanın geri kalanı çalışır (R5a, D16).
- **AC-18.** `<b>Galeri & "Oto"</b>` görünen adı üst çubukta ve giriş satırında bu karakterlerle **metin olarak** görünür; işaretleme olarak yorumlanmaz (R5a, D16).

### Şube listesi (R8)

- **AC-19.** Şubeler ekranı aktif, geçici kapalı ve kapatılmış şubelerin hepsini, her birinin adı ve durumuyla listeler.
- **AC-20.** Listedeki şube adı yöneticinin panel dilindeki karşılığıdır; yoksa varsayılan dildekidir.

### Şube açma (R9, R14, R15 · D11, D12)

- **AC-21.** Yönetici zorunlu alanları doldurup kaydettiğinde yeni şube listede **aktif** olarak görünür.
- **AC-22.** Yeni şube açıldığında şube açılış duyurusu şubenin kimliği ve dil başına adlarıyla outbox'a yazılır.
- **AC-23.** Yeni aktif şube, ziyaretçinin bir sonraki ana sayfa açışında altlıkta adı, adresi ve telefonuyla görünür.
- **AC-24.** Varsayılan dildeki adı boş olan şube açılamaz ve hata ad alanının yanında gösterilir.
- **AC-25.** Varsayılan dildeki adresi boş olan şube açılamaz.
- **AC-26.** Uluslararası biçimde olmayan bir telefon (`02165554433`) reddedilir.
- **AC-27.** IANA bölge kimliği olmayan bir saat dilimi (`+03:00`) sunucuda reddedilir.
- **AC-28.** Panelin saat dilimi alanı serbest metin değil, bölge kimliklerinin listesidir.
- **AC-29.** Küçük harfli ASCII olmayan bir adres kısaltması (`kadıköy-şubesi`) reddedilir.
- **AC-30.** Başka bir şubenin — kapatılmış bir şube dâhil — kullandığı adres kısaltması `422` ve bir alan hata koduyla reddedilir; hata kısaltmanın zaten kullanıldığını söyler.
- **AC-31.** Geçersiz bir e-posta (`kadikoy@`) reddedilir.
- **AC-32.** Reddedilen bir kaydetmede hiçbir şube oluşmaz ve hiçbir duyuru yazılmaz.
- **AC-33.** 120 karakteri aşan şube adı ve 300 karakteri aşan adres alan hatasıyla reddedilir (R3a, D22).

### Şube düzenleme (R10, R14, R15 · D9)

- **AC-34.** Yönetici bir şubenin varsayılan dildeki adını değiştirip kaydettiğinde ad değişikliği duyurusu güncel adlarla outbox'a yazılır.
- **AC-35.** Yalnız Arapça adı değiştirilen kaydetme tam olarak **bir** ad değişikliği duyurusu yazar.
- **AC-36.** Ad ve adres birlikte değiştirilen kaydetme tam olarak **bir** ad değişikliği duyurusu yazar.
- **AC-37.** Adı değişmeden yalnız adresi, telefonu, saat dilimi ya da kısaltması değiştirilen kaydetme **hiçbir** duyuru yazmaz.
- **AC-38.** Değiştirilen adres ve telefon ziyaretçinin bir sonraki ana sayfa açışında altlıkta görünür.
- **AC-39.** WhatsApp numarası, e-posta ve koordinat boşaltılıp kaydedildiğinde üçü de boş kaydedilir.
- **AC-40.** Şubenin kendi kullandığı adres kısaltmasıyla kaydetmek tekillik hatası vermez.
- **AC-41.** AC-24–AC-31'deki doğrulamalar düzenlemede de aynı hatayla geçerlidir.
- **AC-42.** Kapatılmış bir şube düzenlenemez: ekran alanları salt okunur gösterir ve uç reddeder (R10, D21).

### Şube durumu (R11, R12, R13, R14 · C3 · D8)

- **AC-43.** Aktif bir şube geçici kapatıldığında listede **geçici kapalı** görünür ve altlıkta görünmez.
- **AC-44.** Galerinin tek aktif şubesi de geçici kapatılabilir.
- **AC-45.** Geçici kapatma şubedeki araçları yayından kaldırmaz.
- **AC-46.** Geçici kapalı bir şube yeniden açıldığında **aktif** olur ve altlıkta yeniden görünür.
- **AC-47.** Kalıcı kapatma, işlem yapılmadan önce D18'deki metni — "geri alınamaz" ve "bu şubedeki araçlar vitrinden kalkar ve şube ataması bekler" — gösteren bir onay ister; onaydan vazgeçmek şubeyi değiştirmez.
- **AC-48.** Onaylanan kalıcı kapatmadan sonra şube **kapatılmış** olur ve şube kapanış duyurusu outbox'a yazılır.
- **AC-49.** Kalıcı kapatılan şubedeki yayındaki ve rezerve araçlar vitrinden kalkar ve şubedeki araçlar şube ataması bekler olarak işaretlenir (A8).
- **AC-50.** Galerinin son aktif şubesini kalıcı kapatma reddedilir; panel bunun nedenini söyler ve şube aktif kalır.
- **AC-51.** Galerinin tek aktif şubesi önce geçici kapatılıp ardından kalıcı kapatılmak istendiğinde kalıcı kapatma reddedilir ve şube geçici kapalı kalır (D13).
- **AC-52.** Başka bir aktif şube varken geçici kapalı bir şube kalıcı kapatılabilir (D21).
- **AC-53.** İki farklı aktif şubenin, başka aktif şube yokken eşzamanlı kalıcı kapatılması en fazla birinde başarılı olur.
- **AC-54.** Kapatılmış bir şube için yeniden açma ve geçici kapatma eylemleri sunulmaz ve uç onları reddeder.

### Kapatılmış şube ve araçlar (R13a · D14)

- **AC-55.** Kapatılmış bir şubeyle yeni araç kaydı bir alan hata koduyla reddedilir ve araç oluşmaz.
- **AC-56.** Bir aracın şubesini kapatılmış bir şubeye değiştiren kaydetme reddedilir.
- **AC-57.** Şubesi kapatılmış bir aracın şubesine dokunmadan yapılan kaydetme kabul edilir.
- **AC-58.** Panelin araç formundaki şube seçicisi kapatılmış şubeleri seçenek olarak sunmaz; aracın mevcut şubesi kapatılmışsa form bunu gösterir ve başka bir şube seçilmesini ister.

### Eşzamanlı kaydetme (R16 · D7, D17)

- **AC-59.** Galeri profilini, okunduktan sonra başka bir kaydetmeyle değişmiş bir sürüm üzerinden kaydetmek `409` ve bir hata koduyla reddedilir; kayıtlı değer değişmez.
- **AC-60.** Aynı kural şube düzenlemesi için de geçerlidir.
- **AC-61.** Aynı sürümden yapılan iki eşzamanlı şube kaydetmesinden yalnız biri kabul edilir; diğeri `409` alır.
- **AC-62.** Düzenleme formu açıkken şube geçici kapatılırsa formdaki kaydetme `409` ile reddedilir (D17).
- **AC-63.** Reddedilen eski sürüm kaydetmesinde panel bir uyarı gösterir ve güncel hâli yükleme yolu sunar; yöneticinin yazdığı değerler uyarıyla birlikte ekranda kalır.

### Erişim (R17 · D2)

- **AC-64.** Satış Danışmanı ve İçerik Editörü panelde galeri ve şubeler ekranına giden bir bağlantı görmez.
- **AC-65.** Satış Danışmanı galeri ya da şubeler ekranının adresini doğrudan açtığında "erişim yok" ekranını görür; giriş ekranına yönlendirilmez ve "getirilemedi" hatası görmez (D23).
- **AC-66.** Satış Danışmanı ve İçerik Editörü'nün galeri ve şube yazma uçlarına isteği `403` alır ve hiçbir değişiklik yapılmaz.
- **AC-67.** Oturumu olmayan isteğe yazma uçları `401` döner.
- **AC-68.** `GET /api/admin/branches`'in bugünkü okuyucuları (araç formu) değişmeden çalışır.

### Panel durumları ve dil (R18)

- **AC-69.** Galeri ve şubeler ekranı veri gelirken yükleniyor durumunu gösterir.
- **AC-70.** Veri okunamadığında ekran teknik ayrıntı göstermeyen bir hata durumu ve yeniden deneme yolu gösterir.
- **AC-71.** Kaydetme sunucu hatasıyla düştüğünde yöneticinin yazdığı değerler kaybolmaz.
- **AC-72.** Ekranların bütün metinleri TR/AR/EN çeviri kataloğundan gelir; eksik anahtar kontrolü yeşildir.
- **AC-73.** Arapça panelde galeri ve şube formları sağdan sola düzende çalışır; telefon, e-posta ve kısaltma alanları soldan sağa yazılır.

---

## Definition of Done

- [x] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı. *(B1–B11 → D13–D23, 2026-11-18.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0020-ac<n>.png`),
      boş ve hata durumları dâhil; Arapça görüntü de var (AC-12, AC-73). *(51 görsel; `evidence-0020.spec.ts`.)*
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). *(PR'da: `dotnet test` çözüm geneli yeşil.)*
- [x] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok. *(`0007`/AC-8'in kapısı P5'e göre daraltıldı.)*
- [x] <!-- DoD:T5 --> Yeni migration yalnızca `dealership` şemasına dokunuyor. *(`AddGalleryDisplayNameAndVersions`.)*
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [x] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**görünen ad** — D4). *(Ticari Unvan, Görünen Ad.)*
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4); performans kapısı ana sayfa ve listede yeşil (AC-15).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [x] `docs/deploy.md` § 5 ve ADR `0012` § Açık engeller 1, bu kartın ekranlarına göre güncellendi (**D10**).

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
