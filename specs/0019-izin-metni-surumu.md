# 0019 — İzin Anındaki Aydınlatma Metninin Kaydı

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-19 — Takım Yöneticisi (ajan) kararı) · QA CLARIFY turu işlendi (B1–B17 → **D14–D27**) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (yayınlanmış sürüm kaydı, okuma sözleşmesi, migration — `content` şeması) · `Leads` (talebin sürüm bağı, migration — `leads` şeması; `Content`'e yalnız `Content.Contracts` üzerinden okuma) · `docs/architecture.md` (bağımlılık haritası — **D8**) · `docs/domain.md` (yeni terim — **D12**; yeni iş kuralı **L8** — **D27**) · `docs/deploy.md` (dağıtım sırası notu — **D15**) |
| **Bağımlı spec'ler** | `0005` (talep ve izin — tamamlandı; **R11**, **L7** değişmez) · `0017` (KVKK sayfası — tamamlandı; bu kart onun **X8**'idir; **R7**, **R22**, **D5**, **D15**, AC-134 değişmez) |
| **Dayanak** | `0017` **X8** ve **D5** — "ayrı kart olarak önerildi"; sahibin 2026-11-19 kararı (**D1**) |
| **Karar bekliyor** | yok — **D1** sahip kararı; **D2–D13** Takım Yöneticisi (ajan) onayı; QA CLARIFY turunun on yedi bulgusu (B1–B17) → **D14–D27** (2026-11-19). |

---

## Intent

Ziyaretçi talep bırakırken kişisel verilerinin işlenmesine izin veriyor ve `0017`'den beri
izin satırı KVKK aydınlatma metnine bağlanıyor. Ama galeri o metni panelden **değiştirebiliyor**
ve bugün bir talebin kaydında yalnız "izin verildi / verilmedi" bilgisi duruyor: hangi metnin
yayında olduğu değil. Bir şikâyette ya da denetimde galeri, ziyaretçinin talep bıraktığı anda
hangi aydınlatma metninin yayında olduğunu gösteremiyor.

Bunu **galeri yöneticisi** (galerinin hukuki sorumluluğu adına) istiyor. Başarı: her yeni
talebin kaydından, talep anında o dilde yayında olan aydınlatma metni **kelimesi kelimesine**
geri okunabiliyor — metin sonradan değişse, yayından kalksa bile. Metin o dilde yayında
değilse kayıt bunu açıkça söylüyor; bu karttan önceki talepler için "kayıt tutulmuyordu"
deniyor, uydurma bir metin gösterilmiyor.

---

## Requirements

- **R1.** KVKK aydınlatma metni bir dilde **taslaktan yayına geçtiğinde** ve **yayındaki bir
  dilde başlığı ya da gövdesi değiştiğinde**, o dildeki metnin o anki tam hâli (dil, başlık,
  gövde) bir **yayınlanmış sürüm** olarak saklanır. Zaten yayındaki dili yeniden "yayına almak"
  sürüm doğurmaz (**D1**, **D5**, **D16**, **D17**).
- **R2.** Yayınlanmış sürüm sonradan **değiştirilemez ve silinemez**. Metin yeniden
  değiştirilse, o dil yayından kaldırılsa da sürüm okunabilir kalır (**D1**, **D11**).
- **R3.** Her yeni talep, **bırakıldığı dilde** ve **sunucunun talebi kaydettiği anda** yayında
  olan aydınlatma metni sürümüne bağlanır (**D3**). Bu bağ, ziyaretçi iletişim izni versin ya da
  vermesin kurulur (**D4**).
- **R4.** Bir talebin bağlı olduğu sürümden metnin dili, başlığı, gövdesi ve sürümün kayda
  geçtiği an **birebir** geri okunabilir. Bu kartta bunu gösteren bir ekran yoktur (**D7**).
- **R5.** Talebin dilinde aydınlatma metni **yayında değilse** talep yine kabul edilir (`0017`
  **D5**, `0005` **R11**) ve kaydı "metin bu dilde yayında değildi" der; başka bir dilin metnine
  bağlanmaz (**D2**).
- **R6.** Bu karttan **önce** bırakılmış talepler hiçbir sürüme bağlanmaz ve "kayıt
  tutulmuyordu" diye **R5**'in durumundan ayırt edilir (**D1**, **D2**).
- **R7.** Kart yayına girdiğinde aydınlatma metni hangi dillerde yayındaysa, her biri için o
  anki metin bir **başlangıç sürümü** olarak saklanır; başlangıç sürümü olduğu ayırt edilir
  (**D6**).
- **R8.** Talep ile metnin yayını aynı anda işlenirse talep iki sürümden **birine** bağlanır —
  gerçekten yayınlanmış ve tam bir sürüme; iki sürümün karışımına ya da hiç yayınlanmamış bir
  metne değil (**D3**).
- **R9.** Aynı talebin tekrarlanan gönderimi (`0005` **R17**) ilk kaydın bağlı olduğu sürümü
  değiştirmez.
- **R10.** Talep anındaki yayın durumu okunamıyorsa **ya da tutarsızsa** (aydınlatma metni
  sayfası yok; dil yayında görünüyor ama sürümü yok) talep **kaydedilmez**; yanlış bir durumla
  ("yayında değildi") kaydedilmez (**D9**, **D14**, **D25**).
- **R11.** Talebin kişisel alanlarının anonimleştirilmesi (`docs/domain.md` **L6**) talebin
  sürüm bağını ve durumunu değiştirmez.
- **R12.** Yalnız KVKK aydınlatma metni (`privacy` sistem sayfası) sürüm tutar; diğer içerik
  sayfaları bu karttan etkilenmez (**D10**).
- **R13.** Aydınlatma metni hangi dilde yayındaysa, o dilin **en son** yayınlanmış sürümü
  yayındaki metinle (başlık ve gövde) her an birebir aynıdır (**D15**).

---

## Constraints

### Uyulması zorunlu

- **C1.** `Leads`, `Content`'e **yalnız `Content.Contracts`** üzerinden ve yalnız **okuma**
  için bağlanır; `content` şemasındaki hiçbir tabloya okuma dahil erişmez (`AGENTS.md` Altın
  Kural 3; `docs/architecture.md` § 5 Yasak Listesi 1, 6, 7). Çağrı tek atlamadır: `Content`
  bu okumada başka modüle uzanmaz (§ 2.A, Yasak 13).
- **C2.** Migration'lar **yalnız kendi şemasına** dokunur: `Content`'inki `content`,
  `Leads`'inki `leads` — ham SQL (`Sql(...)`) içindeki nitelikli `şema.tablo` adları dâhil
  (**D20**). İki şema arasında foreign key kısıtı yoktur; sürüm kimliği talepte kısıtsız tutulur
  (Yasak 2). Bu kartın iki migration'ı **geri alınamaz** (**D23**).
- **C3.** **Bir istek = bir modülde bir transaction** (§ 2.D). Talep oluşturma `Content`'e
  yazmaz. Yayınlanmış sürüm, onu doğuran sayfa değişikliğiyle **aynı işlemde** yazılır: sürümü
  olmayan bir yayın ya da yayını olmayan bir sürüm oluşmaz.
- **C4.** **Aydınlatma metni kişisel veri içermez** — galerinin herkese açık yayımladığı
  metindir. Yayınlanmış sürüm yalnız metni ve dilini taşır: ziyaretçinin, talebin ya da metni
  yayınlayan personelin kimliğini **taşımaz**. Bu yüzden sürümler KVKK silme ve
  anonimleştirme kapsamına girmez ve C5 onlara güvenle uygulanabilir.
- **C5.** **Yayınlanmış sürümler silinmez ve düzenlenmez** — ne uygulama yolundan ne de
  doğrudan veritabanından (`DELETE` ve `UPDATE` reddedilir). Bir sürüm silinirse ona bağlı
  talepler kanıtsız kalır ve bu geri getirilemez (**D11**, **D18**).
- **C6.** `Lead.ConsentGiven` bugünkü anlamıyla bir `bool` olarak kalır (`0005` AC-24, AC-25;
  `0017` AC-134). Bu kart izni değiştirmez, yanına bir bağ ekler.
- **C7.** Talep oluşturma yoluna eklenen gecikme sınırlıdır: sürümün çözülmesi `Content`'te
  **tek bir SQL komutudur**, kabul edilen talepte bir kez çağrılır ve isteğin p95 medyanını
  **en fazla 10 ms** artırır — ölçüm protokolü **D21**'dedir (**D13**, **D21**, **D24**).
- **C8.** Vitrin davranışı değişmez: `privacy` sayfasının gösterimi, altlığın Kurumsal kolonu ve
  talep formundaki bağlantı `0017` **R21**, **R22** ile aynen kalır. Panelde içerik düzenleme
  akışı (`0017` **R7**, **D15** — yayındaki dilde kaydetmek hemen yayına alır) değişmez.
- **C9.** Kullanıcıya görünen hiçbir metin koda gömülmez; teknik hata metni ziyaretçiye
  sızmaz (Altın Kural 6).
- **C10.** **Dağıtım sırası:** bu sürümde eski uygulama `--migrate`'ten **önce durdurulur**.
  Eski uygulama migration'dan sonra çalışmaya devam ederse yaptığı yayınlar sürüm doğurmaz ve
  R13 kırılır. `docs/deploy.md` bunu bu sürüm için yazar (**D15**).

### KAPSAM DIŞI

- **X1.** **Çerez onayı ve çerez politikasının sürümü** — *neden:* çerez onayı bugün hiç yok.
- **X2.** **Talebin aydınlatma metnini gösteren bir panel ekranı ya da uç** — *neden:* panelde
  bugün **hiç talep ekranı yok** (talepler için tek uç `POST /api/leads`); metni göstermek için
  önce talep ekranı gerekir ve o başka bir kartın işidir. Bu kart veriyi ve okuma
  sözleşmesini bırakır (**D7**).
- **X3.** **Panelde aydınlatma metninin sürüm geçmişi ekranı** — *neden:* içerik editörü için
  ayrı bir ihtiyaç; sahip istemedi.
- **X4.** **Eski taleplerin geriye dönük doldurulması** — *neden:* bugünkü metnin o anda yayında
  olduğu kanıtlanamaz; doldurmak yanlış bir gerçeği kalıcı yazmak olurdu (**D1**, `0005` L2
  ilkesi).
- **X5.** **İzin satırının kendi cümlesi** (`lead.consent.text`, "…izin veriyorum") — *neden:*
  çeviri dosyasında yaşar, panelden değişmez; her değişikliği bir dağıtımdır ve depo
  geçmişinde zaten izlenir.
- **X6.** **Ziyaretçinin metni gerçekten açıp okuduğunun kaydı** — *neden:* kayıt yayında olanı
  söyler, okunanı değil (**D3**); tıklama izlemek yeni bir kişisel veri doğurur.
- **X7.** **Metni yayınlayan personelin kaydı** — *neden:* C4; personel kimliği sürümü kişisel
  veri taşıyan bir kayda çevirir. Gerekirse bir denetim kaydı kartında ele alınır.
- **X8.** **Diğer sistem sayfalarının (`about`, `cookies`, `faq`) ve serbest sayfaların sürümü**
  — *neden:* R12, **D10**.
- **X9.** **Talep duyurusuna (`LeadCaptured`) ya da bildirim gövdesine sürüm eklemek** —
  *neden:* tüketicisi yok; `0009` **D7**'nin dar yüzey kuralı.
- **X10.** **Ziyaretçinin formu açtığı andaki sürümün kaydı** — *neden:* **D3**.

---

## Context

### Mevcut davranış — ölçüldü (2026-11-19, `main` @ `740c771`)

- **İzin bir `bool` ve verilmeyebilir.** `Lead.ConsentGiven`
  (`src/Modules/Leads/Leads/Domain/Lead.cs:79`) istek gövdesinden gelir
  (`Endpoints/CaptureLeadRequest.cs:24` → `Application/CaptureLead.cs:148`) ve `0005` AC-25 gereği
  **izin verilmeden de** talep kaydedilir. Taslağın "izin anı" dediği an aslında **talebin
  kaydedildiği an**dır ve iki durumda da formda aynı aydınlatma bağlantısı durur (**D4**).
- **Sunucu talebin dilini biliyor.** `SubmittedLocale` gövdede açıkça gelir, etkin dillere karşı
  doğrulanır ve varsayılana düşmez (`Application/LeadCaptureOptions.cs:14-17`,
  `CaptureLead.cs:73`). Dil talep anında sunucu tarafında kesindir.
- **Hangi sayfanın aydınlatma metni olduğunu yalnız `Content` biliyor.** Sayfa `privacy` sistem
  kimliğiyle bulunur (`Content/Domain/ContentCodes.cs:10`, `internal`; `0017` **C13**).
  `Content.Contracts` bugün üç okuma sunuyor — `FindBySlugAsync`, `NavigationAsync`,
  `FindCurrentSlugAsync` (`Content.Contracts/Pages/ContentPageContracts.cs:24-40`) — hiçbiri bir
  sürüm ya da kimlik vermiyor. Sözleşme genişlemek zorunda.
- **`Leads` bugün `Content`'e bağlı değil.** `Leads.csproj` yalnız `VehicleCatalog.Contracts` ve
  `VehicleReference.Contracts`'a referans veriyor. `docs/architecture.md` § 1 Bağımlılık haritası
  `Content`'i **yaprak** sayıyor ("hiçbir modül onlara senkron bağımlı değil", `:117-118`). Bu
  kart o satırı değiştirir (**D8**). Host'un `LocalePrefixMiddleware`'i `IContentPublicQueries`'i
  zaten okuyor — o bir kompozisyondur, modül bağımlılığı değil.
- **Harita koddan sapmış.** § 1 `Leads ──► VehicleCatalog, VehicleReference, Dealership` ve
  `Dealership ◄──── VehicleCatalog, Leads, Identity` diyor (`docs/architecture.md:116`, `:111`);
  oysa `Leads.csproj` `Dealership.Contracts`'a referans vermiyor, `Notifications.csproj` veriyor.
  AC-55 bu sapmayı bu kartın harita değişikliğiyle birlikte düzeltir (**D27**).
- **`Content` `Identity.Contracts`'a bağlı** (`Content.csproj`; yönetim yüzeyinin politikası).
  Sürüm okumasının bu bağımlılığa uzanması `Leads → Content → Identity` iki atlamalı zinciri
  doğururdu (Yasak 13) — AC-46 bunu dosya düzeyinde yakalar (**D19**).
- **`Content`'in outbox'ı yok.** `ContentDbContext` yalnız `Pages`, `FaqEntries`, `HomeTexts`
  taşıyor; `Content` hiçbir duyuru yayımlamıyor. Olayla kopyalama alternatifi (**D8**) bu yüzden
  yeni bir outbox demek.
- **`Version` sayacı sayfa başınadır, dil başına değil.** `ContentPage.Version`
  (`Content/Domain/ContentPage.cs:22`) iyimser eşzamanlılık belirtecidir
  (`Infrastructure/Configurations/ContentPageConfiguration.cs:40`) ve **herhangi bir dilde**
  kaydetme, yayına alma ve yayından kaldırmada artar (`Application/ContentCommands.cs:72`,
  `:114`, `:133`). Taslağın "sayaç her kayıtta artar" cümlesi doğru ama eksikti: `en`'de bir
  kayıt `tr` metnini değiştirmeden sayacı artırır, yayından kaldırma da artırır. Sayaç geçmiş
  tutulsa bile bir dilin metnini tanımlamaz — S1'in (a) seçeneği bu yüzden de kullanılamazdı.
- **Metin yerinde üzerine yazılıyor.** `PageLocalization.Title` (≤1000 karakter) ve `Body`
  (sınırsız) `SetTexts` ile doğrudan değişir (`ContentPage.cs:85-87`); geçmiş yok. Yayındaki
  dilde kaydetmek hemen yayına girer (`0017` **R7**, **D15**). Tek bir kayıt isteği birden çok
  dilin metnini birlikte yazabilir (`SavePageAsync`, `ContentCommands.cs:46`). Panel her
  kayıtta formdaki bütün dilleri gönderebilir; değişmeyen dil de istekte bulunur (**D16**).
- **Metin karşılaştırması kırpmasız.** `ContentRules.Normalize` yalnız boşluktan ibaret değeri
  boşaltır, baştaki/sondaki boşluğu **kırpmaz** (`Domain/ContentRules.cs:25-26`). "Değişti mi"
  kararı bu yüzden saklanan değerlerin ordinal karşılaştırmasıdır (**D16**).
- **Slug çakışması iki yerde yakalanır:** ön denetim sorgusu (`ContentCommands.cs:391-393`) ve
  yarışta veritabanının tekil indeksi — `SaveChangesAsync` sonrası `UniqueViolation`
  (`:534-540`). İkincisinde metin değişikliği zaten belleğe uygulanmış ve işlem geri alınmıştır
  (Bilinen tuzak 5).
- **Yayın şartı:** başlık, gövde ve slug'ın üçü de dolu (`ContentCommands.cs:93-106`, `0017`
  **R6**). **Zaten yayındaki dile yayın çağrısı da başarılıdır** ve sayacı artırır
  (`ContentCommands.cs:113-114`) — bugün "yayına girme" ile "yayın çağrısı" ayrı değil (**D17**). Yayından kaldırma metni korur (`ContentPage.cs:94-100`). `privacy` sistem sayfasıdır,
  silinemez (`0017` **R3**).
- **Yeni kurulumda `privacy`'nin hiçbir dilde metni yok.** Sayfa `InitialContent` migration'ında
  sabit kimlikle ve metinsiz tohumlanıyor (`Migrations/20261118171338_InitialContent.cs:123`).
  Galeri yazıp yayına alana kadar her dil "yayında değil"dir.
- **İstemci sürümü bilmiyor.** Talep formu `privacy` bağlantısını bileşen açılırken **bir kez**
  altlık okumasından çözüyor (`frontend/src/features/leads/LeadForm.tsx:135-152`) — yalnız
  slug. Metnin kendisini ya da bir kimliği hiç almıyor; bağlantı sayfa yeniden yüklenene kadar
  değişmiyor. Ziyaretçinin metni gerçekten açıp açmadığı bilinmiyor.
- **Talep oluşturma sırası** (`CaptureLead.cs:45-197`): girdi doğrulama → tekrar anahtarı okuması
  (aynı anahtar ilk kaydı döndürür, `:94-101`) → ilan (`VehicleCatalog.Contracts`) → model adı
  (`VehicleReference.Contracts`) → `Lead.Capture` → hız bütçesi (`quota.TryAcquire`, `:161`) →
  yazma. Çözülemeyen fiyat ya da model adı talebi **reddediyor** (`ListingUnavailable`), yanlış
  değerle kaydetmiyor (`:117-140`) — **D9** aynı ilkedir, yanıt kodu farklıdır (**D25**).
- **Hız sınırı yapılandırılabilir.** `Leads:RateLimit:CapturedLeadLimit` (varsayılan 5),
  `Leads:RateLimit:RequestLimit` (30), `Leads:RateLimit:WindowMinutes` (10)
  (`src/Host/Program.cs:126-128`). Ölçüm bunları yükseltmeden 220 talep kaydedemez (**D21**).
- **Performans betiği yalnız GET ölçüyor.** `tests/Performance/ShowcaseListingBenchmark/Program.cs`
  `PERF_BASE_URL`'e karşı vitrin listesi profillerini koşuyor; `POST` profili yok (**D21**).
- **Migration testleri `Down`'a dayanmıyor — bir istisnayla.** Göç hedefli testler boş bir
  veritabanını önceki migration'a **yukarı** çıkararak kurar (`tests/VehicleReference.Tests/ReferenceDatabase.cs:34-48`,
  `MarkaAyrilmasiTests.cs:69`). Aynı dosyanın `GeriAlinir_TamAdlarGeriYazilir` testi ise
  (`:177`) `Down`'u **sınıyor** — o kartın kendi kararı. Bu kartın testleri `Down`'a ihtiyaç
  duymaz (**D23**).
- **Anonimleştirme yalnız iletişim alanlarını boşaltıyor** (`Lead.cs:145`).
- **Silme koruması emsali.** `leads` şemasında talep silmeyi veritabanı düzeyinde reddeden bir
  tetikleyici var (`Migrations/20260922080321_AddLeadDeleteGuard.cs`, `trg_leads_reject_delete`):
  deyim düzeyinde, hiçbir satıra denk gelmeyen `DELETE`'i de reddeder; `TRUNCATE`'i bilerek
  kapsamaz (`:18-21` — testlerin temizlik yolu, tablo sahipliği uygulama rolünde değil). Emsalin
  `Down`'u tetikleyiciyi düşürüyor. **D11**, **D18** aynı korumayı `UPDATE`'i de katarak öneriyor.
- **Şema adları ölçüldü:** `content` (`ContentDbContext.cs:9`), `leads` (`LeadsDbContext.cs:14`).

### Bilinen tuzaklar

1. **Sahte port, modüller arası kriteri kaçırır.** `Leads.Tests` bütünleştirme testleri ilanı ve
   model adını sahte portlarla veriyor (`tests/Leads.Tests/Integration/TalepOlusturmaHarness.cs`).
   Aynı desen burada kullanılırsa "metin değişti, eski talep eski metni gösteriyor" kriteri
   gerçek `Content` hiç koşmadan yeşil olur. AC-18…AC-35 gerçek `Content` ile — iki modülün de
   kayıtlı olduğu bir ana bilgisayarda — kanıtlanır.
2. **DI kaydı başka modülde.** `Leads`'in yeni okuması `Content`'in kaydettiği bir uygulamaya
   dayanır; `Content` kayıtlı olmayan bir test ana bilgisayarında istek çalışma anında patlar ya
   da sessizce sahte bir uygulamaya düşer. `Leads` kendi tarafında "boş" bir varsayılan
   uygulama **kaydetmez** (AC-49).
3. **"Yayında değildi" ile "okunamadı" aynı değer olmamalı.** İkisini tek bir boş değere indirmek,
   veritabanı hatasında talebi "metin yayında değildi" diye kalıcı ve yanlış kaydeder (**D9**).
4. **Kaydetme birden çok dili birlikte yazabilir** — sürüm dil başınadır, kayıt isteği başına
   değil; istekte bulunan ama değişmeyen dil sürüm doğurmaz (AC-6, AC-7).
5. **Slug çakışması işlemi sonradan geri alır.** Tekil indeks ihlali `SaveChangesAsync`
   sırasında yakalanır; sürüm kaydı sayfa değişikliğiyle **aynı** `SaveChangesAsync`'te yazılmazsa
   (örn. önce sürüm kaydedilip sonra sayfa) reddedilen bir kayıt geride sürüm bırakır ve R13
   kırılır (AC-14).
6. **Eşzamanlılık testi gerçek PostgreSQL ve açık işlem ister.** Bellek içi sağlayıcıda ya da
   `Task.WhenAll` ile "aynı anda" koşulan istekler ardışık işlenir ve yarışı hiç görmez (AC-35).

### Verilen kararlar — 2026-11-19

**D2–D13** Analist önerisiydi; Takım Yöneticisi (ajan) 2026-11-19'da hepsini onayladı. QA CLARIFY
turu bazılarını değiştirdi — değişiklik ilgili kararın içinde "→ **Dn**" diye işaretli.

**D1 — Kayıt metnin tam kopyasıdır (sahip kararı, 2026-11-19; S1 ve S3).** (R1, R2, R6, X4)
KVKK metni her yayımlandığında `Content`'te **değişmez bir "yayınlanmış sürüm" kaydı** olur;
talep o sürümün kimliğini tutar; şikâyet ve denetimde ziyaretçinin izin anındaki metin aynen
geri okunabilir. **Eski talepler geriye dönük doldurulmaz.** *Gerekçe (taslaktan):* yalnız sürüm
numarası geçmiş olmadan anlamsız, özet (hash) metni geri vermez. Sahip S3 önerisini bu seçenekle
birlikte okudu.

**D2 — S2: metin o dilde yayında değilken (Analist önerisi; TM onayladı).** (R5, R6)
Talep **kabul edilir** — `0017` **D5** ve `0005` **R11** bugün de bunu yapıyor ve bu kart talep
kabulünü değiştirmez. Talep hiçbir sürüme bağlanmaz ve kaydı **"bu dilde yayında değildi"**
durumunu taşır. **Başka bir dilin (örn. varsayılanın) metnine düşülmez.**
- *Taslaktaki öneri koda göre hâlâ geçerli, bir düzeltmeyle:* "kimlik boş kalır" tek başına
  yetmiyor, çünkü **D1** gereği eski talepler de boş kimlik taşıyacak. Boş kimlik iki farklı
  gerçeği — "kayıt tutulmuyordu" (R6) ve "metin yayında değildi" (R5) — aynı değere indirirdi;
  ikisini ayırmanın tek yolu talep anını kartın yayına girdiği anla karşılaştırmak olurdu
  (kırılgan; o an hiçbir kayıtta yok). Öneri: talep kaydı **üç durumu açıkça** ayırt eder —
  *kayıt tutulmuyordu* · *yayında değildi* · *şu sürüm* (→ **D15**, **D27**: veritabanı varsayılanı
  ve tutarlılık kısıtı).
- *Gerekçe (düşmeme):* ziyaretçi o dilde bağlantı görmedi (`0017` R22); başka dilin metnini ona
  bağlamak hiç sunulmamış bir metni "sunuldu" diye kaydeder.
- *Bedel:* "yayında değildi" durumu galerinin bir uyum açığını kalıcı olarak kayda geçirir —
  bilinçli: kayıt gerçeği söylemeli.

**D3 — Sunucunun talep anındaki yayın sürümü kaydedilir, istemcinin gördüğü değil (Analist
önerisi).** (R3, R8, X10) Sunucu talebi kaydederken talebin dilinde **o anda yayında olan**
sürümü okur ve talebe bağlar.
- *Neden istemcinin sürümü değil:* (1) istemci bugün metni ya da bir kimliği hiç almıyor, yalnız
  slug (Context); ziyaretçinin gördüğü şey bir **bağlantı**dır, metin değil — "gördüğü sürüm"
  çoğu talepte hiç açılmamış bir metnin kimliği olurdu. (2) İstemciden gelen kimlik ziyaretçinin
  değiştirebileceği bir girdidir; sunucu yine de doğrulamak zorunda kalır (o dilde mi, gerçekten
  yayınlandı mı, ne zaman yayındaydı) ve önbellekte bekleyen eski bir sayfa çok eski bir sürümü
  taşıyabilir. (3) Vitrin sözleşmesi, talep formu ve istek gövdesi değişmek zorunda kalırdı; bu
  seçenekle kart ziyaretçi tarafına hiç dokunmadan biter.
- *Bedel:* ziyaretçi formu açtıktan **sonra** metin değişirse kayıt **yeni** metni gösterir —
  ziyaretçi eskisini okumuş olabilir. Metin yayında değilken form açılıp talepten önce yayına
  alınırsa kayıt bir sürüm gösterir, oysa ziyaretçi bağlantı görmemişti. Kayıttan **yalnız**
  şu kurulabilir: talep anında hangi sürüm yayındaydı ve sürümlerin kayda geçtiği anlardan bir
  önceki sürümün hangi ana kadar yayında kaldığı. Ziyaretçinin formu ne zaman açtığı **hiçbir
  yerde kayıtlı değil**; bu yüzden ziyaretçinin ekranında hangi sürümün bağlantısının durduğu
  ya da hangi metni okuduğu **kurulamaz** (→ **D27**, QA B17 bu iddiayı daralttı). Kaydın
  söylediği cümle "ziyaretçi bunu okudu" değil, **"talep anında yayında olan metin buydu"**dur —
  Intent'in istediği budur.
- *Eşzamanlılık:* talep ile yayın aynı anda işlenirse talep iki sürümden birine bağlanır; hangisi
  olduğu `Content`'in okuduğu andaki **işlenmiş (commit)** duruma kalır, ama sürüm her zaman tam ve
  gerçekten yayınlanmış bir sürümdür (C3: sürüm yayınla aynı işlemde yazılır; → **D22**).

**D4 — Bağ izin verilmese de kurulur (Analist önerisi).** (R3) *Gerekçe:* aydınlatma metni,
iletişim izninden bağımsız olarak kişisel verinin işlendiği her talep için sunulur; bağlantı iki
durumda da formda aynı yerde duruyor. İzin `bool`'u "izin verdi mi", sürüm bağı "hangi metin
yayındaydı" sorusunu cevaplar — ikisi ayrı gerçektir. *Bedel:* kartın adı ("izin metni") bağı
izne bağlı sanılmaya açık; R3 bunu açıkça yazıyor.

**D5 — Sürüm ne zaman doğar (Analist önerisi; → D16, D17 ile hizalandı).** (R1)
- Bir dil **taslaktan yayına geçtiğinde** — metin öncekiyle aynı olsa da (yayından kaldırılıp
  yeniden yayına alma ayrı bir yayın anıdır).
- **Yayındaki** bir dilde **başlık ya da gövde** değiştiğinde — "değişti" saklanan değerin
  öncekiyle **ordinal** karşılaştırmasıdır (**D16**).
- **Doğmaz:** zaten yayındaki dile yayın çağrısında (**D17**) · yalnız slug değiştiğinde (adres
  değişti, metin değil) · istekte bulunan ama başlığı ve gövdesi değişmeyen dilde · taslak dilde
  kaydedildiğinde · yayından kaldırıldığında · istek reddedildiğinde (`409`, zorunlu alan, slug
  çakışması — **D26**).
- *Bedel:* metni değiştirmeden yayından kaldırıp yeniden yayına almak aynı metnin ikinci bir
  sürümünü doğurur. Kabul: yeni anı, yayının kesintiye uğradığını gösterir ve bu bir uyum
  bilgisidir.

**D6 — Başlangıç sürümü (Analist önerisi).** (R7) Kart yayına girdiğinde `privacy` hangi dillerde
yayındaysa her biri için o anki metin **bir başlangıç sürümü** olarak kaydedilir; kayda geçtiği an
kartın yayına girdiği andır ve kayıt başlangıç sürümü olduğunu söyler.
- *Gerekçe:* başlangıç sürümü olmasa, kart yayına girdikten sonra bırakılan her talep — editör
  metne dokunana kadar — hiçbir sürüme bağlanamaz ve yanlışlıkla "yayında değildi" görünürdü.
- *Bedel:* metnin **gerçek** ilk yayın anı bilinmiyor; başlangıç sürümünün anı "bu andan itibaren
  kayıtlı" demektir, "bu anda yayımlandı" değil. Eski taleplere bağlanmaz (D1).

**D7 — Okuma: veri ve sözleşme; ekran yok (Analist önerisi).** (R4, X2) Talep, sürümün kimliğini
ve D2'nin durumunu taşır; `Content.Contracts` bir sürüm kimliğinden dili, başlığı, gövdeyi, kayda
geçtiği anı ve başlangıç sürümü olup olmadığını veren bir okuma sunar. Panel ekranı, yeni uç ya da
dışa aktarma yoktur.
- *Gerekçe:* panelde bugün hiç talep ekranı yok (`POST /api/leads` tek uç); "talep detayında
  metin" önce talep ekranını kurmayı gerektirir ve kapsamı katlar. Sözleşme, o ekran geldiğinde
  metni `Content`'in tablosuna dokunmadan okumanın yolunu bugünden açar.
- *Bedel:* kart bittiğinde bir şikâyette metni geri okumak **teknik bir kişiyi** gerektirir
  (veritabanı sorgusu). Kanıt kaybolmaz ama galeri onu kendi başına gösteremez.

**D8 — `Leads` → `Content.Contracts` senkron okuma; `Content` yaprak olmaktan çıkar (Analist
önerisi; mimari etkili, TM kararı).** (C1, C7) Talep oluşturma, kaydetmeden önce `Content`'e tek
atlamalı bir okuma yapar: "bu dilde `privacy`'nin şu an yayındaki sürümü hangisi, ya da yayında
değil mi". § 2.A'nın üç koşulu da sağlanır (salt okuma, aynı istek, tek atlama — `Content` bu
okumada kimseye uzanmaz). `docs/architecture.md` § 1 bağımlılık haritası güncellenir
(`Leads ──► … , Content`) ve § 7 Karar Defteri'ne satır girer. **ADR gerekmediği** önerisi:
desen var olan desendir (`Leads → VehicleCatalog`); yeni olan yalnız hedef.
- *Değerlendirilen alternatif — olayla kopya:* `Content` her yayında bir duyuru yayar, `Leads`
  dildeki güncel sürümün yerel kopyasını tutar; `Content` yaprak kalır. *Reddedildi:* `Content`'in
  outbox'ı yok (yeni altyapı) ve teslimat **nihai tutarlı** — yayından hemen sonra gelen talep
  eski sürüme bağlanırdı. Bu kartın tek işi doğru sürümü kaydetmek.
- *Bedel:* `Content` artık bir çekirdek modülün senkron bağımlılığı; `Content`'in okuması
  bozulursa talep alınamaz (D9). Döngü yok: `Content` `Leads`'e bağlı değil.

**D9 — Okunamayan durum talebi reddeder (Analist önerisi; → D14, D25).** (R10) `Content`
okuması hata verirse talep kaydedilmez, ziyaretçi genel hata kodunu alır. *Gerekçe:* `0005`'in
çözülemeyen fiyat ve model adı için seçtiği yolla **aynı ilke, farklı yanıt kodu** (orada
`ListingUnavailable`, burada `500`) — yanlış bir gerçeği kalıcı yazmaktansa hiç yazmamak; "yayında
değildi" yazmak galerinin aleyhine yanlış bir kanıt olurdu. *Bedel:* `Content` okuması bozukken
talep alınamaz. İki modül aynı veritabanında olduğundan bu pratikte zaten birlikte bozulan bir
durumdur.

**D10 — Yalnız `privacy` (Analist önerisi).** (R12, X8) *Gerekçe:* izin satırının bağlandığı tek
metin odur (`0017` R22). Diğer sayfaların değişmez kopyası bir ihtiyaca dayanmıyor ve her içerik
kaydını büyütür.

**D11 — Sürüm silme ve güncelleme veritabanı düzeyinde de reddedilir (Analist önerisi; → D18).**
(C5) Sürümü düzenleyen ya da silen bir uygulama yolu yoktur; ek olarak doğrudan `DELETE` ve
`UPDATE` girişimi veritabanında **deyim düzeyinde** reddedilir — `leads` şemasındaki talep silme
korumasının emsali. `TRUNCATE` emsaldeki gerekçeyle kapsam dışıdır (`AddLeadDeleteGuard.cs:18-21`). *Gerekçe:* C5'in kaybı geri getirilemez; tek
koruma olarak "uç yok" demek bir bakım betiğini ya da gelecekteki bir temizlik işini durdurmaz.
*Bedel:* hatalı yayınlanmış bir metin de kalıcıdır — bilinçli: o metin o anda yayındaydı.

**D12 — Yeni domain terimi: Yayınlanmış Metin Sürümü (Analist önerisi; sözlük kararı TM'nin).**
(DoD T7) `docs/domain.md` § İçerik'e: *"KVKK aydınlatma metninin bir dilde yayına girdiği andaki
değişmez kopyası (dil, başlık, gövde, kayda geçtiği an). Talep, bırakıldığı anda yayında olan
sürüme bağlanır; sürüm silinmez ve düzenlenmez. (spec `0019`.)"*

**D13 — Performans sınırı (Analist önerisi; protokol → D21, sayım → D24).** (C7) Sürüm
çözümü `Content`'te tek SQL komutudur ve talep oluşturma isteğinin p95 medyanı bu kartın ebeveyni
olan `main`'e göre **en fazla 10 ms** artar. *Gerekçe:* bugünkü yol zaten iki modül okuması
yapıyor; tek indeksli bir okuma milisaniyenin altındadır ve 10 ms, "ikinci bir sorgu" ya da
"bütün geçmişi taşıyan okuma" hatasını yakalayacak kadar dardır.

### Verilen kararlar — QA CLARIFY turu, 2026-11-19

QA spec'i `main` @ `740c771`'e karşı okudu ve on yedi bulgu getirdi (B1–B17; B1, B2, B8
engelleyici). Takım Yöneticisi (ajan) **hepsini QA'nin önerdiği biçimde** kabul etti; B4 ve B10'da
yönü kendisi belirledi.

**D14 — Sürüm okumasının üç sonucu (B1, engelleyici).** (R10, AC-32, AC-33) `Content`'in "bu dilde
şu an yayındaki sürüm" okuması üç sonuçtan birini döner: **Yayında(sürüm kimliği)** ·
**YayındaDeğil** · **hata**. `privacy` sayfa satırının **yokluğu** ve "dil yayında ama sürümü yok"
hâli **hatadır**, "yayında değil" değildir; talep reddedilir (R10, D9). *Gerekçe:* iki hâl de
kurulumun bozuk olduğunu söyler; onları "yayında değildi"ye indirmek, sistemin kendi hatasını
galerinin uyum açığı diye kalıcı kayda geçirir. *Bedel:* bozuk kurulumda talep alınamaz — görünür
bir arıza, sessiz bir yanlış kayıttan iyidir.

**D15 — Dağıtım sırası, veritabanı varsayılanı ve canlı metin değişmezi (B2, engelleyici).**
(R6, R13, C10, AC-15, AC-23, AC-34)
- `leads`'e eklenen durum kolonunun **veritabanı varsayılanı "kayıt tutulmuyordu"**dur. Var olan
  talepler bu değeri migration'da bu yolla alır; eski uygulamanın dağıtım penceresinde yazabileceği
  talep de dürüst değeri alır.
- **Yeni kod durumu her zaman açıkça yazar**; varsayılana hiçbir yeni talepte güvenilmez
  (AC-23).
- **Eski uygulama `--migrate`'ten önce durdurulur** ve bu, `docs/deploy.md`'ye bu sürüm için not
  olarak düşülür (C10, DoD). *Neden:* migration'dan sonra çalışan eski uygulamanın `privacy`
  yayınları sürüm doğurmaz; canlı metin ile en son sürüm ayrışır.
- **Değişmez (R13):** `privacy` hangi dilde yayındaysa o dilin en son sürümünün başlığı ve gövdesi
  canlı metinle birebir aynıdır — migration sonrasında ve AC-1…AC-14'ün her işlem dizisinden
  sonra doğrulanır (AC-15).
- *Bedel:* bu sürümün dağıtımında kısa bir kesinti penceresi.

**D16 — "Değişti" ordinal karşılaştırmadır; değişmeyen dil sürüm doğurmaz (B3).** (R1, D5, AC-7,
AC-8) Panel kayıtta değişmeyen dilleri de gönderir. Bir dilde sürüm yalnız saklanan başlık ya da
gövde öncekinden **ordinal** olarak farklıysa doğar. `ContentRules.Normalize` yalnız boşluktan
ibaret değeri boşaltır, **kırpmaz** — sondaki bir boşluk farkı da değişikliktir. *Bedel:* görünmez
bir boşluk farkı da sürüm doğurur; kabul — saklanan metin gerçekten farklıdır.

**D17 — Yalnız taslak → yayında geçişi sürüm doğurur (B4; TM kararı).** (R1, D5, AC-11) Zaten
yayındaki dile yayın çağrısı başarılı döner (bugünkü davranış, `ContentCommands.cs:113-114`) ama
sürüm doğurmaz. *Gerekçe:* metin ve yayın durumu değişmedi; yeni sürüm, kesintisiz bir yayını
kesintiye uğramış gibi gösterirdi.

**D18 — `UPDATE` de reddedilir; `TRUNCATE` kapsam dışı (B5).** (C5, D11, AC-40) Sürüm satırını
doğrudan güncelleme girişimi deyim düzeyinde bir tetikleyiciyle reddedilir ve satır değişmez.
`TRUNCATE` emsaldeki gerekçeyle kapsam dışıdır: testlerin temizlik yolu ve tablo sahipliği
uygulamanın rolünde değil (`AddLeadDeleteGuard.cs:18-21`).

**D19 — Tek atlama dosya düzeyinde sınanır (B6).** (C1, AC-46) Sürüm okumasını gerçekleyen
`Content` dosyası başka modülün `Contracts` ad alanını kullanmaz — `BildirimSinirTests.cs:114-131`'in
"ikinci atlama" deseni. *Neden:* `Content` `Identity.Contracts`'a bağlı; proje referansı kapısı
bu zinciri göremez.

**D20 — Şema kapısı ham SQL'i de görür (B7).** (C2, AC-47) İki migration'ın `Sql(...)` içindeki
nitelikli `şema.tablo` adları yalnız kendi şemasıdır. Var olan kapı (`MigrationSchemaTests`)
bunu görmüyorsa genişletilir ya da karta özel test yazılır. *Neden:* D11/D18'in tetikleyicileri
ham SQL'dir ve EF'in şema denetimi onları görmez.

**D21 — Performans ölçüm protokolü (B8, engelleyici).** (C7, AC-52)
- `tests/Performance/ShowcaseListingBenchmark`'a **uygulama PR'ında** bir `POST /api/leads` profili
  eklenir; ısınma 20 + ölçüm 200, eşzamanlılık 1, ağ hariç (`0001` C3).
- **Aynı ikili** (aynı derleme) önce ebeveyn `main`'in, sonra kartın Host'una `PERF_BASE_URL` ile
  karşı koşulur.
- `Leads:RateLimit:CapturedLeadLimit` ve `Leads:RateLimit:RequestLimit` iki Host'ta da ölçüm
  sayısının (220) **üstüne** çekilir.
- Her istek **ayrı tekrar anahtarı** taşır ve **sabit, yayındaki bir ilana** gider; kart Host'unda
  `privacy` ölçüm dilinde **yayındadır** (en pahalı yol ölçülür).
- Her tarafta **3 koşu**, sıra **main → kart → main → kart → main → kart**; iki tarafın p95
  **medyanları** karşılaştırılır: kart − main ≤ 10 ms.
- Her koşuda **`201` sayısı** raporlanır ve 220'dir (`429` yok); eksikse koşu geçersizdir.
- *Bedel:* ölçüm C3 iş akışında değil; elle koşulur ve altı p95 + altı `201` sayısı PR'a yazılır.

**D22 — Eşzamanlılık kriteri deterministik (B9).** (R8, AC-35) "Bariyerde aynı anda" yerine:
gerçek PostgreSQL'de `Content`'te yeni bir `tr` sürümü **yazılmış ama işlenmemiş (commit
edilmemiş)** bir işlem açıkken bırakılan talep **önceki** sürüme, işlem işlendikten sonra
bırakılan talep **yeni** sürüme bağlanır; her ikisinde okunan metin bağlanılan sürümünkiyle
birebir aynıdır. *Neden:* "ya biri ya öteki" her sıralamada yeşil yanar ve hiçbir şey ölçmez.

**D23 — İki migration geri alınamaz (B10; TM kararı).** (C2, AC-48) Bu kartın `content` ve `leads`
migration'larının `Down`'u veri taşıyan tabloyu ya da kolonları **düşürmez**,
`NotSupportedException` atar. *Gerekçe:* `Down` sürüm satırlarını ve talep bağlarını siler — bu
kanıt kaybıdır ve C5'i arka kapıdan deler. *Bedel:* sürüm geri alınamaz; bir hata ileri düzeltme
migration'ıyla çözülür. *Doğrulandı:* göç hedefli testler boş veritabanını önceki migration'a
**yukarı** çıkararak kurar (`ReferenceDatabase.cs:34-48`, `MarkaAyrilmasiTests.cs:69`) ve bu kartın
testleri `Down`'a ihtiyaç duymaz. Not: `MarkaAyrilmasiTests.cs:177` bir **kurulum** satırı değil,
`0021`'in `Down`'u sınayan testidir (`GeriAlinir_TamAdlarGeriYazilir`); emsal kurulum `:69`'dadır.

**D24 — Sorgu sayımı iki yerde (B11).** (C7, AC-50, AC-51) (a) `Content.Tests`, gerçek
PostgreSQL'de: sürüm okuması **tek SQL komutu** çalıştırır (scalar ve non-query komutlar dâhil
sayılır) ve `content` şemasına yazmaz — onaya sunulan metnin AC-37'si ("talep isteği `content`'e yazmaz") bununla birleşti. (b) `Leads.Tests`: port kabul
edilen talepte **tam bir kez**, aynı tekrar anahtarıyla ilk talebi döndüren istekte **hiç**
çağrılmaz.

**D25 — Hata yolunun ayrıntısı ve okumanın yeri (B12).** (R10, D9, AC-44) Okuma, **tekrar anahtarı
okumasından sonra** ve **`quota.TryAcquire`'dan önce** yapılır. Okuma hata verirse hız bütçesi
tüketilmez; yanıt `500` `application/problem+json`'dur ve gövdede istisna metni ya da yığın izi
yoktur. D9'un "aynısıdır" cümlesi "aynı ilke, farklı yanıt kodu" olarak düzeltildi.

**D26 — QA'nin dört netleştirmesi (B13–B16).**
- **B13 — anın ölçümü:** "kayda geçtiği an" işlemi yürüten saatin (`IClock`) değeridir; kriter sabit
  saatle ölçülür, duvar saatiyle değil (AC-1).
- **B14 — slug çakışması:** ön denetimde ya da tekil indekste yakalanan slug çakışmasıyla reddedilen
  kayıt sürüm bırakmaz (AC-14, Bilinen tuzak 5).
- **B15 — izinli kolon listesi ve sözleşme adları:** sürüm kaydının kolonları yalnız **kimlik, dil,
  başlık, gövde, kayda geçtiği an, başlangıç işareti**dir (AC-41). Sözleşme
  `Content.Contracts`'ta `IPrivacyNoticeQueries` adını taşır; iki üyesi vardır:
  `FindPublishedAsync(locale)` → **D14**'ün üç sonucu (`PrivacyNoticeLookup`: `Published(VersionId)` ·
  `NotPublished`; hata istisnadır) ve `FindVersionAsync(versionId)` →
  `PrivacyNoticeVersionDto(VersionId, Locale, Title, Body, RecordedAt, IsInitial)` ya da `null`.
  Adlar kriterin parçasıdır: AC-26 ve AC-38 onlara bakar.
- **B16 — regresyon ve DI:** `0005`'in testleri **doğrulamaları değişmeden** yeşil kalır (yalnız
  kurulum değişebilir); `Content` kayıtlı olmayan kapta `Leads` **sessiz bir varsayılana düşmez**
  (AC-49, AC-53).

**D27 — QA B17'nin düzeltmeleri.**
- Context'teki satır numaraları `main` @ `740c771`'e karşı yeniden ölçüldü.
- **Harita sapması** (Context): AC-55 `Leads` satırını ve `Dealership`'in gelen listesini kodla
  hizalar.
- **D3**'ün "yeniden kurulabilir" iddiası daraltıldı.
- **`docs/domain.md`'ye yeni iş kuralı L8** (sözlük terimi D12'nin yanında): *"**L8.** Talep, bırakıldığı
  dilde ve kaydedildiği anda yayında olan KVKK aydınlatma metni sürümüne bağlanır; metin o dilde
  yayında değilse bu da kaydedilir. Bağ izinden bağımsızdır, sonradan değişmez ve anonimleştirmede
  korunur. Bu kural gelmeden önceki talepler 'kayıt tutulmuyordu' durumundadır. *(spec `0019`.)*"*
- **`leads`'te durum ↔ sürüm kimliği tutarlılık kısıtı:** durum "sürüm" ise kimlik doludur, diğer
  iki durumda boştur; aykırı satır veritabanında reddedilir (AC-43).

---

## Acceptance Criteria

### Sürümün doğması (R1, R12 · D5, D10, D16, D17, D26)

- **AC-1.** `privacy`'nin `tr` dili başlık, gövde ve slug'ı dolu taslakken yayına alındığında, dili `tr` olan ve başlığı ile gövdesi o anki metinle birebir aynı olan bir yayınlanmış sürüm oluşur; kayda geçtiği an, sabit saatle kurulan testte işlemi yürüten saatin (`IClock`) değeridir.
- **AC-2.** Yayındaki `tr`'de gövde değiştirilip kaydedildiğinde yeni bir sürüm oluşur ve önceki sürümün başlığı, gövdesi ve anı değişmeden kalır.
- **AC-3.** Yayındaki `tr`'de yalnız başlık değiştirildiğinde yeni bir sürüm oluşur.
- **AC-4.** Yayındaki `tr`'de yalnız slug değiştirildiğinde yeni sürüm oluşmaz.
- **AC-5.** Taslaktaki `ar`'da metin kaydedildiğinde sürüm oluşmaz.
- **AC-6.** `tr` ve `en` ikisi de yayındayken tek bir kayıt isteğiyle iki dilin gövdesi değiştirildiğinde `tr` için bir ve `en` için bir olmak üzere iki sürüm oluşur.
- **AC-7.** `tr` ve `en` ikisi de yayındayken istek iki dili de taşır ve yalnız `en` gövdesi değişirse (`tr` başlık ve gövdesi aynen gönderilir) yalnız bir `en` sürümü oluşur; `tr` sürümü oluşmaz (D16).
- **AC-8.** Yayındaki `tr` gövdesi yalnız sonuna eklenen bir boşlukla kaydedildiğinde değişmiş sayılır ve yeni sürüm oluşur; sürümün gövdesi sondaki boşluğu taşır (D16).
- **AC-9.** Yayındaki `tr` yayından kaldırıldığında sürüm oluşmaz ve var olan `tr` sürümleri okunabilir kalır.
- **AC-10.** Yayından kaldırılan `tr` metni değiştirilmeden yeniden yayına alındığında yeni bir sürüm oluşur.
- **AC-11.** Zaten yayındaki `tr`'ye yayına alma çağrısı başarılı döner ama sürüm oluşturmaz (D17).
- **AC-12.** `about`, `cookies`, `faq` sistem sayfalarında ve bir serbest sayfada yayına alma ve yayındaki dilde kaydetme sürüm oluşturmaz.
- **AC-13.** Bayat sürüm belirteciyle (`409`) ya da eksik zorunlu alanla reddedilen kayıt ve yayına alma sürüm oluşturmaz.
- **AC-14.** Yayındaki `tr`'de başka bir sayfanın slug'ıyla yapılan ve slug çakışmasıyla reddedilen kayıt — çakışma ister ön denetimde ister veritabanının tekil indeksinde yakalansın — sürüm oluşturmaz (D26, Bilinen tuzak 5).

### Canlı metin değişmezi (R13 · D15)

- **AC-15.** `privacy` hangi dilde yayındaysa o dilin en son sürümünün başlığı ve gövdesi canlı metinle birebir aynıdır; bu, kartın migration'ından sonra ve AC-1…AC-14'ün her işlem dizisinin sonunda doğrulanır.

### Başlangıç sürümü (R7 · D6)

- **AC-16.** Kartın migration'ı uygulandığında `privacy` `tr` ve `en`'de yayında, `ar`'da taslaksa `tr` ve `en` için o anki metinle birer başlangıç sürümü oluşur, `ar` için oluşmaz.
- **AC-17.** Başlangıç sürümü okunduğunda başlangıç sürümü olduğu ayırt edilir; editörün yayınıyla doğan sürüm başlangıç sürümü olarak görünmez.

### Talebe bağlanma — üç dil (R3 · D3, D4, D15)

- **AC-18.** `privacy` `tr`'de yayındayken `tr` dilinde bırakılan talep, o anki `tr` sürümüne bağlanır.
- **AC-19.** `privacy` `ar`'da yayındayken `ar` dilinde bırakılan talep, o anki `ar` sürümüne bağlanır.
- **AC-20.** `privacy` `en`'de yayındayken `en` dilinde bırakılan talep, o anki `en` sürümüne bağlanır.
- **AC-21.** `tr` ve `en`'de farklı metinler yayındayken `en` dilinde bırakılan talep `en` sürümüne bağlanır, `tr` sürümüne bağlanmaz.
- **AC-22.** İletişim izni verilmeden bırakılan talep de o dilin yayındaki sürümüne bağlanır ve izni "verilmedi" olarak kalır.
- **AC-23.** Uygulamanın kaydettiği hiçbir talep "kayıt tutulmuyordu" durumunu taşımaz: `privacy` yayındayken, yayında değilken ve hiç metni yokken bırakılan talepler durumlarını ("sürüm" ya da "yayında değildi") açıkça taşır (D15).

### Metin değiştikten sonra (R2, R4 · D1, D7)

- **AC-24.** `tr` metni "M1" iken talep A bırakılır, metin "M2" olarak kaydedilir, talep B bırakılır: talep A'nın sürümünden okunan başlık ve gövde M1'in birebir aynısıdır, talep B'ninki M2'ninki.
- **AC-25.** AC-24'ten sonra `tr` yayından kaldırıldığında talep A'nın ve talep B'nin sürümleri yine dil, başlık, gövde ve anla okunur.
- **AC-26.** `IPrivacyNoticeQueries.FindVersionAsync` var olan bir sürüm kimliği için dili, başlığı, gövdeyi, kayda geçtiği anı ve başlangıç sürümü olup olmadığını döner; var olmayan bir kimlik için `null` döner (D26).
- **AC-27.** Ziyaretçinin `privacy` sayfasında okuduğu `tr` başlığı ve gövdesi, metin değişmeden bırakılan bir `tr` talebinin sürümünden okunan başlık ve gövdeyle birebir aynıdır.

### Metin o dilde yayında değil (R5 · D2)

- **AC-28.** `privacy` `ar`'da taslakken `ar` dilinde bırakılan talep kabul edilir ve kaydı "yayında değildi" durumunu taşır; hiçbir sürüme bağlanmaz.
- **AC-29.** AC-28'de `privacy` `tr`'de yayında olsa da `ar` talebi `tr` sürümüne bağlanmaz.
- **AC-30.** `privacy`'nin hiçbir dilde metni yokken (yeni kurulum) bırakılan `tr` talebi "yayında değildi" durumunu taşır.
- **AC-31.** `tr` yayından kaldırıldıktan sonra bırakılan `tr` talebi "yayında değildi" durumunu taşır; kaldırmadan önceki sürüme bağlanmaz.

### Tutarsız durum (R10 · D14)

- **AC-32.** `privacy` sayfa satırı yokken bırakılan talep kaydedilmez ve hiçbir talebe "yayında değildi" yazılmaz.
- **AC-33.** `tr` yayındayken `tr` için hiç sürüm yoksa bırakılan `tr` talebi kaydedilmez.

### Eski talepler (R6 · D1, D2, D15)

- **AC-34.** Kartın migration'ından önce var olan talepler migration'dan sonra "kayıt tutulmuyordu" durumunu taşır, hiçbir sürüme — başlangıç sürümüne de — bağlanmaz ve bu durum "yayında değildi"den ayırt edilir.

### Eşzamanlılık ve tekrar (R8, R9 · D3, D22)

- **AC-35.** Gerçek PostgreSQL'de `Content`'te yeni bir `tr` sürümünü yazmış ama henüz işlenmemiş (commit edilmemiş) bir işlem açıkken bırakılan `tr` talebi önceki sürüme, işlem işlendikten sonra bırakılan `tr` talebi yeni sürüme bağlanır; iki talepte de sürümden okunan başlık ve gövde bağlanılan sürümünkiyle birebir aynıdır.
- **AC-36.** Aynı `privacy` sayfası için iki editör aynı sürüm belirteciyle aynı anda `tr` metnini kaydettiğinde yalnız biri başarılı olur ve yalnız bir yeni sürüm oluşur.
- **AC-37.** Aynı tekrar anahtarıyla gönderilen ikinci istek — araya yeni bir `tr` yayını girmiş olsa da — yeni talep oluşturmaz ve ilk talebin sürüm bağı değişmez.

### Değişmezlik ve kişisel veri (R2, R11 · C4, C5 · D11, D18, D26, D27)

- **AC-38.** Yayınlanmış sürümü düzenleyen ya da silen bir uç yoktur; `IPrivacyNoticeQueries`'in üyeleri yalnız `FindPublishedAsync` ve `FindVersionAsync`'tir.
- **AC-39.** `content` şemasında yayınlanmış sürüm tablosuna doğrudan `DELETE` girişimi — hiçbir satıra denk gelmese de — veritabanında reddedilir ve satırlar yerinde kalır.
- **AC-40.** Yayınlanmış sürüm satırını doğrudan `UPDATE` girişimi veritabanında reddedilir ve satır değişmez.
- **AC-41.** Yayınlanmış sürüm kaydının kolonları yalnız kimlik, dil, başlık, gövde, kayda geçtiği an ve başlangıç işaretidir; ziyaretçi, talep ya da personel kimliği taşıyan bir kolon yoktur.
- **AC-42.** Kişisel alanları anonimleştirilen talep, sürüm bağını ve durumunu (sürüm · yayında değildi · kayıt tutulmuyordu) korur.
- **AC-43.** `leads`'te durumu "sürüm" olup kimliği boş olan ya da durumu "yayında değildi" veya "kayıt tutulmuyordu" olup kimliği dolu olan bir talep satırı veritabanında reddedilir.

### Hata yolu (R10 · C9 · D9, D25)

- **AC-44.** Talep anında sürüm okuması hata verdiğinde talep kaydedilmez, hiçbir talebe "yayında değildi" yazılmaz, hız bütçesi tüketilmez (aynı istemciden ardından gelen geçerli talepler bütçenin tamamını kullanabilir) ve yanıt `500` `application/problem+json`'dur; gövdede istisna metni ve yığın izi yoktur.

### Sınır ve migration (C1, C2, C3 · D8, D19, D20, D23, D26)

- **AC-45.** `Leads` kaynağında `content` şemasına erişen kod yoktur ve `Leads` `Content`'e yalnız `Content.Contracts` projesi üzerinden bağlanır (`0017` AC-129'un kapısı bu kartın değişikliğiyle yeşil kalır).
- **AC-46.** `IPrivacyNoticeQueries`'i gerçekleyen `Content` dosyası başka bir modülün `Contracts` ad alanını kullanmaz (tek atlama — `BildirimSinirTests.cs:114-131` deseni).
- **AC-47.** `Content`'in yeni migration'ı yalnız `content`, `Leads`'in yeni migration'ı yalnız `leads` şemasına dokunur; ham SQL (`Sql(...)`) içindeki nitelikli `şema.tablo` adları dâhil.
- **AC-48.** Bu kartın iki migration'ının `Down`'u çağrıldığında `NotSupportedException` atar; sürüm satırları ve taleplerin sürüm bağları yerinde kalır.
- **AC-49.** `Content`'in kayıtlı olmadığı bir kapta `Leads`'in talep yolu sessiz bir varsayılana düşmez: `IPrivacyNoticeQueries` çözülemediği için hata verir ve hiçbir talebe "yayında değildi" yazılmaz.

### Performans (C7 · D13, D21, D24)

- **AC-50.** `Content.Tests`'te gerçek PostgreSQL'de `FindPublishedAsync` tek bir SQL komutu çalıştırır (scalar ve non-query komutlar dâhil sayılır) ve `content` şemasında hiçbir satırı eklemez, değiştirmez ya da silmez.
- **AC-51.** `Leads.Tests`'te sürüm portu kabul edilen talepte tam bir kez çağrılır; aynı tekrar anahtarıyla ilk talebi döndüren istekte hiç çağrılmaz.
- **AC-52.** D21'in protokolüyle (aynı ikili; ebeveyn `main` ve kart Host'u; hız sınırları 220'nin üstünde; ayrı tekrar anahtarları, sabit yayındaki ilan, kart tarafında `privacy` yayında; her tarafta 3 koşu, main → kart sırasıyla dönüşümlü) kartın `POST /api/leads` p95 medyanı ebeveyn `main`'inkinden en fazla 10 ms büyüktür; her koşuda `201` sayısı 220'dir ve altı p95 ile altı `201` sayısı PR'a yazılır.

### Regresyon ve belge (C6, C8 · D8, D12, D27)

- **AC-53.** `0005`'in kabul kriterlerinin testleri doğrulamaları değiştirilmeden yeşil kalır (yalnız kurulumları değişebilir) ve izin kaydı bir `bool` olarak kalır.
- **AC-54.** `0017`'nin kabul kriterlerinin testleri yeşil kalır: `privacy` sayfasının vitrin gösterimi, altlık kolonu ve talep formundaki bağlantı değişmez.
- **AC-55.** `docs/architecture.md` § 1 bağımlılık haritasında `Leads` satırı `VehicleCatalog, VehicleReference, Content`'i gösterir (`Dealership` sapması kaldırılır), `Dealership`'in gelen listesi koddaki referanslarla (`VehicleCatalog`, `Identity`, `Notifications`) aynıdır, `Content` "hiçbir modül onlara senkron bağımlı değil" satırından çıkar ve § 7 Karar Defteri'nde bu kartın satırı vardır.
- **AC-56.** `docs/domain.md` **L8** iş kuralını ve **Yayınlanmış Metin Sürümü** terimini D12 ve D27'deki anlamıyla taşır.

---

## Definition of Done

- [x] Kriterler QA CLARIFY turundan geçti ve bulgular karara bağlandı. *(B1–B17 → D14–D27, Takım Yöneticisi (ajan) kararı, 2026-11-19.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
      AC-18…AC-35 gerçek `Content` ile kanıtlandı (Bilinen tuzak 1).
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/<no>-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri varsa Arapça görüntü de var.
      — Bu kartta arayüz kriteri **yok** (D7, X2); görsel kanıt uygulanabilir değildir.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration'lar yalnızca kendi şemasına dokunuyor: `Content` → `content`, `Leads` → `leads` (ham SQL dâhil — AC-47).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
      — Yeni kullanıcı metni beklenmiyor; eklenirse üç dilde.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**Yayınlanmış Metin Sürümü** — D12; iş kuralı **L8** — D27).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (D8; bağımlılık haritası dâhil — AC-55).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [ ] `docs/deploy.md`'ye bu sürüm için "eski uygulama `--migrate`'ten önce durdurulur" notu düşüldü (C10, D15).
- [ ] AC-52'nin altı p95 ölçümü ve altı `201` sayısı PR'a yazıldı.
- [ ] `0017` **X8**'e "→ `0019`" notu düşüldü.

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
