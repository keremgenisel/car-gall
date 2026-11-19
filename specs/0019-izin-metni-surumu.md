# 0019 — İzin Anındaki Aydınlatma Metninin Kaydı

| | |
|---|---|
| **Durum** | Onaya sunuldu (2026-11-19) |
| **Sahip** | Analist (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (yayınlanmış sürüm kaydı, okuma sözleşmesi, migration — `content` şeması) · `Leads` (talebin sürüm bağı, migration — `leads` şeması; `Content`'e yalnız `Content.Contracts` üzerinden okuma) · `docs/architecture.md` (bağımlılık haritası — **D8**) · `docs/domain.md` (yeni terim önerisi — **D12**) |
| **Bağımlı spec'ler** | `0005` (talep ve izin — tamamlandı; **R11**, **L7** değişmez) · `0017` (KVKK sayfası — tamamlandı; bu kart onun **X8**'idir; **R7**, **R22**, **D5**, **D15**, AC-134 değişmez) |
| **Dayanak** | `0017` **X8** ve **D5** — "ayrı kart olarak önerildi"; sahibin 2026-11-19 kararı (**D1**) |
| **Karar bekliyor** | **Takım Yöneticisi** — spec onayı ve Analist önerileri **D2–D13** (S2 önerisi **D2**; mimari etkili **D8**: `Content` yaprak olmaktan çıkar). Sahip kararı **D1** verildi. |

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

- **R1.** KVKK aydınlatma metni bir dilde **yayına girdiğinde** ve **yayındaki bir dilde başlığı
  ya da gövdesi değiştiğinde**, o dildeki metnin o anki tam hâli (dil, başlık, gövde) bir
  **yayınlanmış sürüm** olarak saklanır (**D1**, **D5**).
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
- **R10.** Talep anındaki yayın durumu okunamıyorsa talep **kaydedilmez**; yanlış bir durumla
  ("yayında değildi") kaydedilmez (**D9**).
- **R11.** Talebin kişisel alanlarının anonimleştirilmesi (`docs/domain.md` **L6**) talebin
  sürüm bağını ve durumunu değiştirmez.
- **R12.** Yalnız KVKK aydınlatma metni (`privacy` sistem sayfası) sürüm tutar; diğer içerik
  sayfaları bu karttan etkilenmez (**D10**).

---

## Constraints

### Uyulması zorunlu

- **C1.** `Leads`, `Content`'e **yalnız `Content.Contracts`** üzerinden ve yalnız **okuma**
  için bağlanır; `content` şemasındaki hiçbir tabloya okuma dahil erişmez (`AGENTS.md` Altın
  Kural 3; `docs/architecture.md` § 5 Yasak Listesi 1, 6, 7). Çağrı tek atlamadır: `Content`
  bu okumada başka modüle uzanmaz (§ 2.A, Yasak 13).
- **C2.** Migration'lar **yalnız kendi şemasına** dokunur: `Content`'inki `content`,
  `Leads`'inki `leads`. İki şema arasında foreign key kısıtı yoktur; sürüm kimliği talepte
  kısıtsız tutulur (Yasak 2).
- **C3.** **Bir istek = bir modülde bir transaction** (§ 2.D). Talep oluşturma `Content`'e
  yazmaz. Yayınlanmış sürüm, onu doğuran sayfa değişikliğiyle **aynı işlemde** yazılır: sürümü
  olmayan bir yayın ya da yayını olmayan bir sürüm oluşmaz.
- **C4.** **Aydınlatma metni kişisel veri içermez** — galerinin herkese açık yayımladığı
  metindir. Yayınlanmış sürüm yalnız metni ve dilini taşır: ziyaretçinin, talebin ya da metni
  yayınlayan personelin kimliğini **taşımaz**. Bu yüzden sürümler KVKK silme ve
  anonimleştirme kapsamına girmez ve C5 onlara güvenle uygulanabilir.
- **C5.** **Yayınlanmış sürümler silinmez ve düzenlenmez** — ne uygulama yolundan ne de
  doğrudan veritabanından. Bir sürüm silinirse ona bağlı talepler kanıtsız kalır ve bu geri
  getirilemez (**D11**).
- **C6.** `Lead.ConsentGiven` bugünkü anlamıyla bir `bool` olarak kalır (`0005` AC-24, AC-25;
  `0017` AC-134). Bu kart izni değiştirmez, yanına bir bağ ekler.
- **C7.** Talep oluşturma yoluna eklenen gecikme sınırlıdır: sürümün çözülmesi `Content`'e en
  fazla **bir** veritabanı gidişi ekler ve isteğin p95'ini **en fazla 10 ms** artırır (**D13**).
- **C8.** Vitrin davranışı değişmez: `privacy` sayfasının gösterimi, altlığın Kurumsal kolonu ve
  talep formundaki bağlantı `0017` **R21**, **R22** ile aynen kalır. Panelde içerik düzenleme
  akışı (`0017` **R7**, **D15** — yayındaki dilde kaydetmek hemen yayına alır) değişmez.
- **C9.** Kullanıcıya görünen hiçbir metin koda gömülmez; teknik hata metni ziyaretçiye
  sızmaz (Altın Kural 6).

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
  dilin metnini birlikte yazabilir (`SavePageAsync`, `ContentCommands.cs:46`).
- **Yayın şartı:** başlık, gövde ve slug'ın üçü de dolu (`ContentCommands.cs:90-111`, `0017`
  **R6**). Yayından kaldırma metni korur (`ContentPage.cs:94-100`). `privacy` sistem sayfasıdır,
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
  (`VehicleReference.Contracts`) → `Lead.Capture` → hız bütçesi → yazma. Çözülemeyen fiyat ya da
  model adı talebi **reddediyor**, yanlış değerle kaydetmiyor (`:117-140`) — **D9** bu ilkenin
  aynısıdır.
- **Anonimleştirme yalnız iletişim alanlarını boşaltıyor** (`Lead.cs:145`).
- **Silme koruması emsali.** `leads` şemasında talep silmeyi veritabanı düzeyinde reddeden bir
  tetikleyici var (`Migrations/20260922080321_AddLeadDeleteGuard.cs`, `trg_leads_reject_delete`).
  **D11** aynı korumayı öneriyor.
- **Şema adları ölçüldü:** `content` (`ContentDbContext.cs:9`), `leads` (`LeadsDbContext.cs:14`).

### Bilinen tuzaklar

1. **Sahte port, modüller arası kriteri kaçırır.** `Leads.Tests` bütünleştirme testleri ilanı ve
   model adını sahte portlarla veriyor (`tests/Leads.Tests/Integration/TalepOlusturmaHarness.cs`).
   Aynı desen burada kullanılırsa "metin değişti, eski talep eski metni gösteriyor" kriteri
   gerçek `Content` hiç koşmadan yeşil olur. AC-13…AC-25 ve AC-27 gerçek `Content` ile — iki
   modülün de kayıtlı olduğu bir ana bilgisayarda — kanıtlanır.
2. **DI kaydı başka modülde.** `Leads`'in yeni okuması `Content`'in kaydettiği bir uygulamaya
   dayanır; `Content` kayıtlı olmayan bir test ana bilgisayarında istek çalışma anında patlar ya
   da sessizce sahte bir uygulamaya düşer.
3. **"Yayında değildi" ile "okunamadı" aynı değer olmamalı.** İkisini tek bir boş değere indirmek,
   veritabanı hatasında talebi "metin yayında değildi" diye kalıcı ve yanlış kaydeder (**D9**).
4. **Kaydetme birden çok dili birlikte yazabilir** — sürüm dil başınadır, kayıt isteği başına
   değil (AC-6).

### Verilen kararlar

**D1 — Kayıt metnin tam kopyasıdır (sahip kararı, 2026-11-19; S1 ve S3).** (R1, R2, R6, X4)
KVKK metni her yayımlandığında `Content`'te **değişmez bir "yayınlanmış sürüm" kaydı** olur;
talep o sürümün kimliğini tutar; şikâyet ve denetimde ziyaretçinin izin anındaki metin aynen
geri okunabilir. **Eski talepler geriye dönük doldurulmaz.** *Gerekçe (taslaktan):* yalnız sürüm
numarası geçmiş olmadan anlamsız, özet (hash) metni geri vermez. Sahip S3 önerisini bu seçenekle
birlikte okudu.

**D2 — S2: metin o dilde yayında değilken (Analist önerisi; TM onayı bekliyor).** (R5, R6)
Talep **kabul edilir** — `0017` **D5** ve `0005` **R11** bugün de bunu yapıyor ve bu kart talep
kabulünü değiştirmez. Talep hiçbir sürüme bağlanmaz ve kaydı **"bu dilde yayında değildi"**
durumunu taşır. **Başka bir dilin (örn. varsayılanın) metnine düşülmez.**
- *Taslaktaki öneri koda göre hâlâ geçerli, bir düzeltmeyle:* "kimlik boş kalır" tek başına
  yetmiyor, çünkü **D1** gereği eski talepler de boş kimlik taşıyacak. Boş kimlik iki farklı
  gerçeği — "kayıt tutulmuyordu" (R6) ve "metin yayında değildi" (R5) — aynı değere indirirdi;
  ikisini ayırmanın tek yolu talep anını kartın yayına girdiği anla karşılaştırmak olurdu
  (kırılgan; o an hiçbir kayıtta yok). Öneri: talep kaydı **üç durumu açıkça** ayırt eder —
  *kayıt tutulmuyordu* · *yayında değildi* · *şu sürüm*.
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
  alınırsa kayıt bir sürüm gösterir, oysa ziyaretçi bağlantı görmemişti. İkisi de sürümlerin
  kayda geçtiği anlar ve talebin anıyla **yeniden kurulabilir** (önceki sürüm şu ana kadar
  yayındaydı); pencere bir sayfa oturumu kadardır. Kaydın söylediği cümle bu yüzden "ziyaretçi
  bunu okudu" değil, **"talep anında yayında olan metin buydu"**dur — Intent'in istediği budur.
- *Eşzamanlılık:* talep ile yayın aynı anda işlenirse talep iki sürümden birine bağlanır; hangisi
  olduğu sıralamaya kalır, ama sürüm her zaman tam ve gerçekten yayınlanmış bir sürümdür (C3:
  sürüm yayınla aynı işlemde yazılır).

**D4 — Bağ izin verilmese de kurulur (Analist önerisi).** (R3) *Gerekçe:* aydınlatma metni,
iletişim izninden bağımsız olarak kişisel verinin işlendiği her talep için sunulur; bağlantı iki
durumda da formda aynı yerde duruyor. İzin `bool`'u "izin verdi mi", sürüm bağı "hangi metin
yayındaydı" sorusunu cevaplar — ikisi ayrı gerçektir. *Bedel:* kartın adı ("izin metni") bağı
izne bağlı sanılmaya açık; R3 bunu açıkça yazıyor.

**D5 — Sürüm ne zaman doğar (Analist önerisi).** (R1)
- Bir dil **yayına alındığında** — metin öncekiyle aynı olsa da (yeniden yayına alma ayrı bir
  yayın anıdır).
- **Yayındaki** bir dilde **başlık ya da gövde** değiştiğinde.
- **Doğmaz:** yalnız slug değiştiğinde (adres değişti, metin değil) · taslak dilde kaydedildiğinde
  · yayından kaldırıldığında · istek reddedildiğinde (`409`, zorunlu alan).
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

**D9 — Okunamayan durum talebi reddeder (Analist önerisi).** (R10) `Content` okuması hata verirse
talep kaydedilmez, ziyaretçi genel hata kodunu alır. *Gerekçe:* `0005`'in çözülemeyen fiyat ve
model adı için seçtiği yol — yanlış bir gerçeği kalıcı yazmaktansa hiç yazmamak; "yayında
değildi" yazmak galerinin aleyhine yanlış bir kanıt olurdu. *Bedel:* `Content` okuması bozukken
talep alınamaz. İki modül aynı veritabanında olduğundan bu pratikte zaten birlikte bozulan bir
durumdur.

**D10 — Yalnız `privacy` (Analist önerisi).** (R12, X8) *Gerekçe:* izin satırının bağlandığı tek
metin odur (`0017` R22). Diğer sayfaların değişmez kopyası bir ihtiyaca dayanmıyor ve her içerik
kaydını büyütür.

**D11 — Sürüm silme veritabanı düzeyinde de reddedilir (Analist önerisi).** (C5) Sürümü düzenleyen
ya da silen bir uygulama yolu yoktur; ek olarak doğrudan silme girişimi veritabanında reddedilir —
`leads` şemasındaki talep silme korumasının emsali. *Gerekçe:* C5'in kaybı geri getirilemez; tek
koruma olarak "uç yok" demek bir bakım betiğini ya da gelecekteki bir temizlik işini durdurmaz.
*Bedel:* hatalı yayınlanmış bir metin de kalıcıdır — bilinçli: o metin o anda yayındaydı.

**D12 — Yeni domain terimi: Yayınlanmış Metin Sürümü (Analist önerisi; sözlük kararı TM'nin).**
(DoD T7) `docs/domain.md` § İçerik'e: *"KVKK aydınlatma metninin bir dilde yayına girdiği andaki
değişmez kopyası (dil, başlık, gövde, kayda geçtiği an). Talep, bırakıldığı anda yayında olan
sürüme bağlanır; sürüm silinmez ve düzenlenmez. (spec `0019`.)"*

**D13 — Performans sınırı (Analist önerisi).** (C7) Sürüm çözümü `Content`'e **en fazla bir**
veritabanı gidişi ekler; talep oluşturma isteğinin p95'i, `0001` **C3**'ün protokolüyle (20 istek
ısınma + 200 istek ölçüm, eşzamanlılık 1, ağ hariç; her istek ayrı tekrar anahtarı ve hız
bütçesine takılmayan istemci anahtarıyla) bu kartın ebeveyni olan `main`'e göre **en fazla 10 ms**
artar. *Gerekçe:* bugünkü yol zaten iki modül okuması yapıyor; tek indeksli bir okuma
milisaniyenin altındadır ve 10 ms, "ikinci bir sorgu" ya da "bütün geçmişi taşıyan okuma"
hatasını yakalayacak kadar dardır. *Bedel:* ölçüm C3 iş akışında değil — elle koşulur ve PR'a
yazılır.

---

## Acceptance Criteria

### Sürümün doğması (R1, R12 · D5, D10)

- **AC-1.** `privacy`'nin `tr` dili başlık, gövde ve slug'ı dolu taslakken yayına alındığında, dili `tr` olan ve başlığı ile gövdesi o anki metinle birebir aynı olan bir yayınlanmış sürüm oluşur; kayda geçtiği an yayın işleminin anıdır.
- **AC-2.** Yayındaki `tr`'de gövde değiştirilip kaydedildiğinde yeni bir sürüm oluşur ve önceki sürümün başlığı, gövdesi ve anı değişmeden kalır.
- **AC-3.** Yayındaki `tr`'de yalnız başlık değiştirildiğinde yeni bir sürüm oluşur.
- **AC-4.** Yayındaki `tr`'de yalnız slug değiştirildiğinde yeni sürüm oluşmaz.
- **AC-5.** Taslaktaki `ar`'da metin kaydedildiğinde sürüm oluşmaz.
- **AC-6.** `tr` ve `en` ikisi de yayındayken tek bir kayıt isteğiyle iki dilin gövdesi değiştirildiğinde `tr` için bir ve `en` için bir olmak üzere iki sürüm oluşur.
- **AC-7.** Yayındaki `tr` yayından kaldırıldığında sürüm oluşmaz ve var olan `tr` sürümleri okunabilir kalır.
- **AC-8.** Yayından kaldırılan `tr` metni değiştirilmeden yeniden yayına alındığında yeni bir sürüm oluşur.
- **AC-9.** `about`, `cookies`, `faq` sistem sayfalarında ve bir serbest sayfada yayına alma ve yayındaki dilde kaydetme sürüm oluşturmaz.
- **AC-10.** Bayat sürüm belirteciyle (`409`) ya da eksik zorunlu alanla reddedilen kayıt ve yayına alma sürüm oluşturmaz.

### Başlangıç sürümü (R7 · D6)

- **AC-11.** Kartın migration'ı uygulandığında `privacy` `tr` ve `en`'de yayında, `ar`'da taslaksa `tr` ve `en` için o anki metinle birer başlangıç sürümü oluşur, `ar` için oluşmaz.
- **AC-12.** Başlangıç sürümü okunduğunda başlangıç sürümü olduğu ayırt edilir; editörün yayınıyla doğan sürüm başlangıç sürümü olarak görünmez.

### Talebe bağlanma — üç dil (R3 · D3, D4)

- **AC-13.** `privacy` `tr`'de yayındayken `tr` dilinde bırakılan talep, o anki `tr` sürümüne bağlanır.
- **AC-14.** `privacy` `ar`'da yayındayken `ar` dilinde bırakılan talep, o anki `ar` sürümüne bağlanır.
- **AC-15.** `privacy` `en`'de yayındayken `en` dilinde bırakılan talep, o anki `en` sürümüne bağlanır.
- **AC-16.** `tr` ve `en`'de farklı metinler yayındayken `en` dilinde bırakılan talep `en` sürümüne bağlanır, `tr` sürümüne bağlanmaz.
- **AC-17.** İletişim izni verilmeden bırakılan talep de o dilin yayındaki sürümüne bağlanır ve izni "verilmedi" olarak kalır.

### Metin değiştikten sonra (R2, R4 · D1, D7)

- **AC-18.** `tr` metni "M1" iken talep A bırakılır, metin "M2" olarak kaydedilir, talep B bırakılır: talep A'nın sürümünden okunan başlık ve gövde M1'in birebir aynısıdır, talep B'ninki M2'ninki.
- **AC-19.** AC-18'den sonra `tr` yayından kaldırıldığında talep A'nın ve talep B'nin sürümleri yine dil, başlık, gövde ve anla okunur.
- **AC-20.** `Content.Contracts`'ın sürüm okuması var olan bir sürüm kimliği için dili, başlığı, gövdeyi, kayda geçtiği anı ve başlangıç sürümü olup olmadığını döner; var olmayan bir kimlik için "yok" döner.
- **AC-21.** Ziyaretçinin `privacy` sayfasında okuduğu `tr` başlığı ve gövdesi, metin değişmeden bırakılan bir `tr` talebinin sürümünden okunan başlık ve gövdeyle birebir aynıdır.

### Metin o dilde yayında değil (R5 · D2)

- **AC-22.** `privacy` `ar`'da taslakken `ar` dilinde bırakılan talep kabul edilir ve kaydı "yayında değildi" durumunu taşır; hiçbir sürüme bağlanmaz.
- **AC-23.** AC-22'de `privacy` `tr`'de yayında olsa da `ar` talebi `tr` sürümüne bağlanmaz.
- **AC-24.** `privacy`'nin hiçbir dilde metni yokken (yeni kurulum) bırakılan `tr` talebi "yayında değildi" durumunu taşır.
- **AC-25.** `tr` yayından kaldırıldıktan sonra bırakılan `tr` talebi "yayında değildi" durumunu taşır; kaldırmadan önceki sürüme bağlanmaz.

### Eski talepler (R6 · D1, D2)

- **AC-26.** Kartın migration'ından önce var olan talepler migration'dan sonra "kayıt tutulmuyordu" durumunu taşır, hiçbir sürüme — başlangıç sürümüne de — bağlanmaz ve bu durum "yayında değildi"den ayırt edilir.

### Eşzamanlılık ve tekrar (R8, R9 · D3)

- **AC-27.** Bir `tr` talebi ile `tr` metninin yeni bir kaydı bir bariyerde bekletilip aynı anda bırakıldığında talep ya önceki ya yeni sürüme bağlanır; bağlandığı sürümün başlığı ve gövdesi o sürümün yayınlandığı hâliyle birebir aynıdır (bir sürümün başlığı ile diğerinin gövdesi değil).
- **AC-28.** Aynı `privacy` sayfası için iki editör aynı sürüm belirteciyle aynı anda `tr` metnini kaydettiğinde yalnız biri başarılı olur ve yalnız bir yeni sürüm oluşur.
- **AC-29.** Aynı tekrar anahtarıyla gönderilen ikinci istek — araya yeni bir `tr` yayını girmiş olsa da — yeni talep oluşturmaz ve ilk talebin sürüm bağı değişmez.

### Değişmezlik ve kişisel veri (R2, R11 · C4, C5 · D11)

- **AC-30.** Yayınlanmış sürümü düzenleyen ya da silen bir uç ya da `Content.Contracts` yöntemi yoktur.
- **AC-31.** `content` şemasında bir yayınlanmış sürüm satırını doğrudan silme girişimi veritabanında reddedilir ve satır yerinde kalır.
- **AC-32.** Yayınlanmış sürüm kaydı ziyaretçi, talep ya da personel kimliği taşıyan bir alan içermez.
- **AC-33.** Kişisel alanları anonimleştirilen talep, sürüm bağını ve durumunu (sürüm · yayında değildi · kayıt tutulmuyordu) korur.

### Hata yolu (R10 · C9 · D9)

- **AC-34.** Talep anında `Content`'in sürüm okuması hata verdiğinde talep kaydedilmez, "yayında değildi" durumu yazılmaz ve ziyaretçi teknik metin içermeyen genel hata karşılığını alır.

### Sınır ve migration (C1, C2, C3 · D8)

- **AC-35.** `Leads` kaynağında `content` şemasına erişen kod yoktur ve `Leads` `Content`'e yalnız `Content.Contracts` projesi üzerinden bağlanır (`0017` AC-129'un kapısı bu kartın değişikliğiyle yeşil kalır).
- **AC-36.** `Content`'in yeni migration'ı yalnız `content`, `Leads`'in yeni migration'ı yalnız `leads` şemasına dokunur.
- **AC-37.** Talep oluşturma isteği `content` şemasında hiçbir satırı eklemez, değiştirmez ya da silmez.

### Performans (C7 · D13)

- **AC-38.** Talep oluşturma isteği, sürümü çözmek için `Content`'e en fazla bir veritabanı gidişi yapar.
- **AC-39.** D13'ün protokolüyle talep oluşturma isteğinin p95'i, bu kartın ebeveyni olan `main`'deki ölçüme göre en fazla 10 ms artar; iki ölçüm de PR'a yazılır.

### Regresyon ve belge (C6, C8 · D8)

- **AC-40.** `0005`'in kabul kriterlerinin testleri yeşil kalır ve izin kaydı bir `bool` olarak kalır.
- **AC-41.** `0017`'nin kabul kriterlerinin testleri yeşil kalır: `privacy` sayfasının vitrin gösterimi, altlık kolonu ve talep formundaki bağlantı değişmez.
- **AC-42.** `docs/architecture.md` § 1 bağımlılık haritası `Leads → Content` okumasını gösterir, `Content`'i "hiçbir modül onlara senkron bağımlı değil" satırından çıkarır ve § 7 Karar Defteri'nde bu kartın satırı vardır.

---

## Definition of Done

- [ ] Takım Yöneticisi spec'i ve **D2–D13**'ü onayladı; kriterler implementasyondan önce `main`'de.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
      AC-13…AC-25 ve AC-27 gerçek `Content` ile kanıtlandı (Bilinen tuzak 1).
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/<no>-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri varsa Arapça görüntü de var.
      — Bu kartta arayüz kriteri **yok** (D7, X2); görsel kanıt uygulanabilir değildir.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration'lar yalnızca kendi şemasına dokunuyor: `Content` → `content`, `Leads` → `leads`.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
      — Yeni kullanıcı metni beklenmiyor; eklenirse üç dilde.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi (**Yayınlanmış Metin Sürümü** — D12).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (D8; bağımlılık haritası dâhil — AC-42).
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
- [ ] AC-39'un iki p95 ölçümü (ebeveyn `main` ve kart) PR'a yazıldı.
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
