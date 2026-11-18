# 0020 — Galeri Profili ve Şubeler: Panelden Yönetim

| | |
|---|---|
| **Durum** | Taslak — kriterler yazıldı, QA CLARIFY turunu bekliyor |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Dealership` (yazma uçları, galerinin görünen adı — yeni alan + migration) · `frontend/` panel (galeri ve şubeler ekranları) ve vitrin (üst çubuk, ana sayfa giriş satırı) · `Host` (yalnız kayıt) |
| **Bağımlı spec'ler** | `0003` (galeri profili ve şubeler — tamamlandı; bu kart onun **X1**'idir) · `0006` (roller — tamamlandı) · `0014` (ana sayfa — tamamlandı; giriş satırı ve altlık bu kartın verisini okur, **D6**'sı değişir — **D4**) · `0012b` (A8: şube kapanınca araçların yayından kalkması — tamamlandı) |
| **Dayanak** | `0003` **X1** ("yazma yüzeyi, `Identity` spec'inden sonra ayrı bir spec'tir") · ADR `0012` § Açık engeller 1 · depo sahibinin 2026-11-18 sırası (**D1**) |
| **Karar bekliyor** | yok — **D1–D12** verildi (§ Verilen kararlar). QA CLARIFY turu bekleniyor. |

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

### Vitrinde galeri adı

- **R4.** Vitrinin her sayfasının üst çubuğu, ziyaretçinin dilindeki **görünen adı** gösterir.
- **R5.** Üst çubuktaki ad, ziyaretçinin dilindeki **ana sayfaya** götüren bir bağlantıdır
  (**D5**).
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
- **R10.** Yönetici var olan bir şubenin **R9'daki bütün alanlarını** düzenler. Opsiyonel bir
  alan **boşaltılabilir** (**D9**).
- **R11.** Yönetici bir şubeyi **geçici kapatır** ve geçici kapalı bir şubeyi **yeniden
  açar.** Geçici kapatma son aktif şubede de mümkündür (`0003` **D3**).
- **R12.** Yönetici bir şubeyi **kalıcı olarak kapatır.** Kalıcı kapatma **geri alınamaz**
  (`0003` **R3**) ve şubedeki araçlar yayından kalkar (`docs/domain.md` **A8**); yönetici bu
  iki sonucu kapatmadan **önce** görür ve açıkça onaylar (**D8**).
- **R13.** **Son aktif şube** kalıcı olarak kapatılamaz (**B5**); deneme reddedilir ve yönetici
  nedenini görür.
- **R14.** Şube açma, şube adı değişikliği ve kalıcı kapatma `0003` **R7–R9**'daki duyuruları
  bugünkü gibi üretir; panelden yapılan değişiklik bunların hiçbirini atlamaz.

### Kurallar, eşzamanlılık, erişim

- **R15.** Şube alanları `0003`'ün kurallarıyla doğrulanır: telefon ve WhatsApp uluslararası
  biçimde, e-posta geçerli bir adres, saat dilimi bir **IANA bölge kimliği**, adres kısaltması
  küçük harfli ASCII ve galeri genelinde **tekil**. Kural dışı bir değer kaydedilmez ve
  yönetici hangi alanın neden reddedildiğini görür.
- **R16.** Aynı profili ya da şubeyi **eski bir sürüm üzerinden** kaydetmek reddedilir; yönetici
  bunu bir uyarıyla görür ve güncel hâli yükleyebilir (**D7**).
- **R17.** Galeri ve şube ekranları ile yazma uçları **yalnız Yönetici** içindir. Diğer roller
  ekranlara gezinme bağlantısı görmez, uçlar onlara `403` döner (**D2**).
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
- **C4.** Yeni migration yalnız `dealership` şemasına dokunur. Var olan kurulumlarda görünen
  adın varsayılan dildeki karşılığı **ticari unvandan** doldurulur; hiçbir kurulum görünen
  adsız kalmaz (**D4**).
- **C5.** Kullanıcıya görünen hiçbir metin koda gömülmez; uçlar hata **kodu** döner ve panel
  onu çevirir (Altın Kural 6).
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
  (`ContentPageConfiguration.cs:40`); bu kart aynı deseni izler (**D7**).

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

### Vitrinde galeri adı (R4, R5, R6, R7 · C6 · D4, D5, D6)

- **AC-8.** Vitrinin ana sayfasında, araç listesinde, ilan detayında ve bir içerik sayfasında üst çubuk ziyaretçinin dilindeki görünen adı gösterir.
- **AC-9.** Ziyaretçinin dilinde görünen ad yoksa üst çubuk varsayılan dildeki görünen adı gösterir.
- **AC-10.** Üst çubuktaki ada tıklamak ziyaretçiyi `/{locale}/` ana sayfasına götürür ve `{locale}` ziyaretçinin o anki dilidir.
- **AC-11.** Arapça sayfada üst çubuktaki ad Arapça görünen adı gösterir ve sağdan sola düzende doğru hizalanır.
- **AC-12.** Ana sayfanın giriş bölümündeki galeri adı satırı ziyaretçinin dilindeki görünen adı gösterir; ticari unvanı göstermez.
- **AC-13.** Yönetici görünen adı değiştirip kaydettikten sonra açılan ilk vitrin sayfası yeni adı gösterir.
- **AC-14.** Üst çubuk sayfanın ilk boyamasında görünen adla gelir: ad sonradan yüklenip üst çubuğu ya da içeriği kaydırmaz (ana sayfa ve liste için performans kapısının CLS eşiği yeşil kalır).
- **AC-15.** Ticari unvan, vergi numarası ve ticaret sicil numarası hiçbir vitrin sayfasında ve vitrinin okuduğu hiçbir public yanıtta yer almaz.

### Şube listesi (R8)

- **AC-16.** Şubeler ekranı aktif, geçici kapalı ve kapatılmış şubelerin hepsini, her birinin adı ve durumuyla listeler.
- **AC-17.** Listedeki şube adı yöneticinin panel dilindeki karşılığıdır; yoksa varsayılan dildekidir.

### Şube açma (R9, R14, R15 · D11, D12)

- **AC-18.** Yönetici zorunlu alanları doldurup kaydettiğinde yeni şube listede **aktif** olarak görünür.
- **AC-19.** Yeni şube açıldığında şube açılış duyurusu şubenin kimliği ve dil başına adlarıyla outbox'a yazılır.
- **AC-20.** Yeni aktif şube, ziyaretçinin bir sonraki ana sayfa açışında altlıkta adı, adresi ve telefonuyla görünür.
- **AC-21.** Varsayılan dildeki adı boş olan şube açılamaz ve hata ad alanının yanında gösterilir.
- **AC-22.** Varsayılan dildeki adresi boş olan şube açılamaz.
- **AC-23.** Uluslararası biçimde olmayan bir telefon (`02165554433`) reddedilir.
- **AC-24.** IANA bölge kimliği olmayan bir saat dilimi (`+03:00`) sunucuda reddedilir.
- **AC-25.** Panelin saat dilimi alanı serbest metin değil, bölge kimliklerinin listesidir.
- **AC-26.** Küçük harfli ASCII olmayan bir adres kısaltması (`kadıköy-şubesi`) reddedilir.
- **AC-27.** Başka bir şubenin kullandığı adres kısaltması reddedilir ve hata kısaltmanın zaten kullanıldığını söyler.
- **AC-28.** Geçersiz bir e-posta (`kadikoy@`) reddedilir.
- **AC-29.** Reddedilen bir kaydetmede hiçbir şube oluşmaz ve hiçbir duyuru yazılmaz.

### Şube düzenleme (R10, R14, R15 · D9)

- **AC-30.** Yönetici bir şubenin varsayılan dildeki adını değiştirip kaydettiğinde ad değişikliği duyurusu güncel adlarla outbox'a yazılır.
- **AC-31.** Adı değişmeden yalnız adresi, telefonu, saat dilimi ya da kısaltması değiştirilen kaydetme **hiçbir** duyuru yazmaz.
- **AC-32.** Değiştirilen adres ve telefon ziyaretçinin bir sonraki ana sayfa açışında altlıkta görünür.
- **AC-33.** WhatsApp numarası, e-posta ve koordinat boşaltılıp kaydedildiğinde üçü de boş kaydedilir.
- **AC-34.** Şubenin kendi kullandığı adres kısaltmasıyla kaydetmek tekillik hatası vermez.
- **AC-35.** AC-21–AC-28'deki doğrulamalar düzenlemede de aynı hatayla geçerlidir.
- **AC-36.** Kapatılmış bir şube düzenlenemez: ekran alanları salt okunur gösterir ve uç reddeder.

### Şube durumu (R11, R12, R13, R14 · C3 · D8)

- **AC-37.** Aktif bir şube geçici kapatıldığında listede **geçici kapalı** görünür ve altlıkta görünmez.
- **AC-38.** Galerinin tek aktif şubesi de geçici kapatılabilir.
- **AC-39.** Geçici kapatma şubedeki araçları yayından kaldırmaz.
- **AC-40.** Geçici kapalı bir şube yeniden açıldığında **aktif** olur ve altlıkta yeniden görünür.
- **AC-41.** Kalıcı kapatma, işlem yapılmadan önce "geri alınamaz" ve "bu şubedeki yayındaki araçlar yayından kalkar" diyen bir onay ister; onaydan vazgeçmek şubeyi değiştirmez.
- **AC-42.** Onaylanan kalıcı kapatmadan sonra şube **kapatılmış** olur ve şube kapanış duyurusu outbox'a yazılır.
- **AC-43.** Kalıcı kapatılan şubedeki yayındaki araçlar vitrinden kalkar (A8).
- **AC-44.** Galerinin son aktif şubesini kalıcı kapatma reddedilir; panel bunun nedenini söyler ve şube aktif kalır.
- **AC-45.** İki farklı aktif şubenin, başka aktif şube yokken eşzamanlı kalıcı kapatılması en fazla birinde başarılı olur.
- **AC-46.** Kapatılmış bir şube için yeniden açma ve geçici kapatma eylemleri sunulmaz ve uç onları reddeder.

### Eşzamanlı kaydetme (R16 · D7)

- **AC-47.** Galeri profilini, okunduktan sonra başka bir kaydetmeyle değişmiş bir sürüm üzerinden kaydetmek reddedilir ve kayıtlı değer değişmez.
- **AC-48.** Aynı kural şube düzenlemesi için de geçerlidir.
- **AC-49.** Reddedilen eski sürüm kaydetmesinde panel bir uyarı gösterir ve güncel hâli yükleme yolu sunar; yöneticinin yazdığı değerler uyarıyla birlikte ekranda kalır.

### Erişim (R17 · D2)

- **AC-50.** Satış Danışmanı ve İçerik Editörü panelde galeri ve şubeler ekranına giden bir bağlantı görmez.
- **AC-51.** Satış Danışmanı ve İçerik Editörü'nün galeri ve şube yazma uçlarına isteği `403` alır ve hiçbir değişiklik yapılmaz.
- **AC-52.** Oturumu olmayan isteğe yazma uçları `401` döner.
- **AC-53.** `GET /api/admin/branches`'in bugünkü okuyucuları (araç formu) değişmeden çalışır.

### Panel durumları ve dil (R18)

- **AC-54.** Galeri ve şubeler ekranı veri gelirken yükleniyor durumunu gösterir.
- **AC-55.** Veri okunamadığında ekran teknik ayrıntı göstermeyen bir hata durumu ve yeniden deneme yolu gösterir.
- **AC-56.** Kaydetme sunucu hatasıyla düştüğünde yöneticinin yazdığı değerler kaybolmaz.
- **AC-57.** Ekranların bütün metinleri TR/AR/EN çeviri kataloğundan gelir; eksik anahtar kontrolü yeşildir.
- **AC-58.** Arapça panelde galeri ve şube formları sağdan sola düzende çalışır; telefon, e-posta ve kısaltma alanları soldan sağa yazılır.

---

## Definition of Done

- [ ] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0020-ac<n>.png`),
      boş ve hata durumları dâhil; Arapça görüntü de var (AC-11, AC-58).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `dealership` şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**görünen ad** — D4).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4); performans kapısı ana sayfa ve listede yeşil (AC-14).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [ ] `docs/deploy.md` § 5 ve ADR `0012` § Açık engeller 1, bu kartın ekranlarına göre güncellendi (**D10**).

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
