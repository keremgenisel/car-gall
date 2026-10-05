# frontend-002 — Görsel Dil ve Kuruluma Özel Tema

| | |
|---|---|
| **Durum** | **Tamamlandı** (2026-10-05) — *D4 dâhil; bkz. D4 Ek* |
| **Sahip** | Developer (ajan) |
| **Analist** | Analist oturumu (ajan üye) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` (token katmanı, tüm ekranlar) · `src/Host` (tema enjeksiyonu) |
| **Bağımlı spec'ler** | `frontend-001` (token dosyası ve bileşen envanteri — tamamlandı) · `0003` (galeri profili: ad ve logo — tamamlandı) |
| **Dayanak** | Mockup tuvali "Araba Galeri — Mockuplar"; ADR 0003 |

---

## Intent

Arayüzün bugünkü renkleri ve tipografisi **hiç kimsenin verdiği bir karar değil.**
`frontend-001` token mekanizmasını doğru kurdu — değerler tek dosyada yaşıyor, bileşende
gömülü hex yasak ve lint bunu kırıyor — ama **hangi değerler** olacağını hiçbir spec
söylemedi. Bugün vurgu rengi olarak duran `#1f5fd0`, `docs/frontend.md` § 4.1'de *yasak
örneği* olarak yazılı hex'in aynısıdır: dokümandaki örnek, token dosyasına galeri kimliği kararı
gibi yerleşmiş.

Mockup'lar ise net bir görsel dil taşıyor ve tasarımcı bu dilin **neden** öyle olduğunu da
yazmış: beyaz zemin, tek tipografi, limon vurgu **yalnızca dolgu olarak** — çünkü beyaz
üzerinde metin olarak okunmuyor — ve metin vurgusu için ayrı bir koyu zeytin.

Ayrıca ürün **white-label**: `AGENTS.md` kurulum başına tek galeri diyor ve mockup'taki
"MERİDYEN" yalnızca bir örnek. Dolayısıyla asıl gereksinim bir palet seçmek değil:
**renk ve tipografinin kurulum başına değişebilmesi.**

Bu iş bittiğinde arayüzün görsel dili kasıtlı bir karardır, kutudan çıkınca mockup gibi
görünür, ve yeni bir galeriye kurulurken rengi **tek bir yapılandırmadan** değişir.
Başarı ölçütü: **bir kurulumun rengini değiştirmek hiçbir bileşene dokunmayı gerektirmez.**

---

## Requirements

- **R1.** Arayüzün tüm renk, tipografi, köşe yarıçapı ve aralık değerleri **tek bir token
  katmanında** yaşar; hiçbir bileşen ham hex, ham piksel veya ham font boyutu taşımaz.
  → `docs/frontend.md` § 4.1 *(mevcut kural; bu spec onu bozmaz)*. **İstisna:** token'ların
  **tanımlandığı** dosya ham değer taşır ve lint onu muaf tutar — bu muafiyet bugün
  `frontend/stylelint.config.cjs`'te zaten kurulu.
- **R2.** Token katmanının **varsayılan değerleri** Context § **Token tablosu**'nda kapalı
  bir liste olarak verilir; tablonun dışında varsayılan yoktur.
- **R3.** **Dolgu vurgusu, metin vurgusu, dolgu üzerindeki metin ve odak halkası dört ayrı
  token'dır.** Dolgu olarak kullanılan renk metin rengi olarak kullanılamaz; dolgu üzerine
  yazılan metnin rengi de odak halkasının rengi de dolgudan türetilmez.
  *(Gerekçe: bugün `var(--color-accent)` yedi çağrı yerinde ve üç farklı anlamda kullanılıyor
  — metin, dolgu, kenarlık ve **odak halkası**. Dolguyu limon yapıp odak halkasını ona
  bağlamak, halkanın beyaz zeminde kaybolması demektir: tuzak 6.)*
- **R4.** Tipografi token'ları **iki yığın** taşır: Latin metin için bir yığın, **Arapça
  metin için ayrı bir yığın**. Arapça sayfada Arapça yığını kullanılır.
- **R5.** Bir kurulumun **renk** değerleri **dağıtım yapılandırmasından** gelir ve arayüz
  kodunda değişiklik gerektirmez. **Tipografi, köşe ve aralık yapılandırılamaz** — bkz. X9
  ve Karar **D9**.
- **R6.** Tema yapılandırması **ya tam verilir ya hiç verilmez.** "Tam"ın tanımı Context
  § Token tablosu'nun **yapılandırılabilir** sütunudur: o sütundaki her anahtar verilmek
  zorundadır. Şu üç durumda uygulama **ayağa kalkmaz** ve nedenini token adıyla söyler:
  eksik anahtar, tabloda **olmayan** anahtar, ve **biçime uymayan** değer (C11).
  Hiç tema verilmediğinde arayüz **varsayılan temayla** çalışır.
  *(Gerekçe: varsayılanlar `tokens.css`'te yaşıyor; kısmi temaya izin vermek host'un
  karşılaştırma için varsayılanları da bilmesini — paletin **iki yerde** yaşamasını —
  gerektirirdi. `AGENTS.md`: bir kural yalnızca bir dosyada yaşar.)*
- **R7.** Kontrast eşikleri **iki ayrı kapıdan** doğrulanır:
  - **Yapılandırılan tema** → uygulama **başlangıcında**, host tarafında. Karşılamayan
    yapılandırmayla uygulama ayağa kalkmaz ve nedenini söyler.
  - **Varsayılan palet** → **CI'da koşan bir testle**. İhlalde test kırmızıya döner.

  *(Gerekçe: R6 host'un varsayılanları bilmemesini doğru nedenle şart koşuyor — ama sonucu,
  tema verilmediğinde çalışan paletin runtime kapısına hiç girmemesidir. Tek kapı varsayılanı
  kapsamaz; iki kapı hem tek-kaynak gerekçesini korur hem varsayılanı denetimsiz bırakmaz.)*
- **R8.** Galerinin **adı** tema değil **veridir**: galeri profilinden gelir. → `0003` R1.
  *(Logo gösterimi bu spec'te yoktur → X10.)*
- **R9.** Renk **tek başına anlam taşımaz**: durum, hata ve seçili olma hâli renkten
  bağımsız olarak metin veya biçimle de anlatılır. → `docs/frontend.md` § 7.
- **R10.** **Onay anında var olan tüm ekranlar** yeni token değerleriyle çalışır;
  hiçbirinde okunamayan metin veya kaybolmuş kenarlık kalmaz. Ekranların listesi spec'e
  gömülmez — onay anında sayılır ve DoD'de kanıtlanır.
- **R11.** Odak halkası **kendi token'ıyla** tanımlanır ve görünürlüğü yeni palette de
  korunur: zemin, yüzey grisi ve dolgu vurgusunun **her birine** karşı eşiği karşılar.
  → `docs/frontend.md` § 7.
- **R12.** Yazı tipi dosyaları **uygulamanın kendi kaynağından** servis edilir; arayüz
  üçüncü taraf bir yazı tipi sağlayıcısına istek yapmaz. → Karar **D8**.
- **R13.** Hangi renk token'larının **24 piksel ve üstü** metinle kullanıldığı spec'te
  **beyan edilir** (Context § Token tablosu, *büyük metin* sütunu). Kontrast kapısı 3:1
  eşiğini yalnız beyan edilen çiftlere, 4.5:1'i geri kalanına uygular.
  *(Gerekçe: kapı yalnız renkleri görür, boyutu görmez. Beyan yoksa ya her şeye 4.5:1
  uygulanır — yanlış kırmızı — ya 3:1 — sessiz geçiş.)*

---

## Constraints

### Uyulması zorunlu

- **C1.** Bileşenlerde gömülü sabit değer **yasaktır**; lint kırar.
  → `docs/frontend.md` § 4.1, § 8.
- **C2.** CSS'te fiziksel yön özellikleri kullanılmaz; logical properties zorunludur.
  → Yasak Listesi 25.
- **C3.** Kullanıcıya görünen hiçbir metin koda gömülmez. → `AGENTS.md` Altın Kural 6.
- **C4.** Tema enjeksiyonu **mevcut** `index.html` enjeksiyon yoluna eklenir; ikinci bir
  mekanizma kurulmaz. → `docs/architecture.md` § 3 (Tam SSR yok), `src/Host/Localization/IndexHtmlRenderer.cs`.
- **C5.** **Dört** durum birbirinden ayrıdır ve karıştırılmaz: **hiç tema verilmemesi**
  varsayılanla çalışır; **kısmi tema** uygulamayı durdurur (R6); **tanınmayan anahtar veya
  biçime uymayan değer** uygulamayı durdurur (R6, C11); **kontrastı yetersiz tema**
  uygulamayı durdurur (R7). İlki dışında hiçbiri sessizce varsayılana düşmez.
- **C6.** Token **adları anlamsaldır** (`--color-accent-fill`), değer temelli değil
  (`--color-lemon`). Galeri kimliği değiştiğinde ad değişmez.
- **C7.** Metin kontrastı en az **4.5:1**, 24px ve üstü metinde **3:1**.
  → `docs/frontend.md` § 7.
- **C8.** Yazı tipi dosyaları veya `<link>`'leri **tek yerden** gelir; her bileşen kendi
  fontunu yüklemez.
- **C9.** Bu spec hiçbir ekranın **düzenini** değiştirmez. → Karar **D5**.
- **C10.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler **görsel kanıt**
  taşır. → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 3, `docs/frontend.md` § 9.
- **C11.** **Renk değerlerinin biçimi tek tiptir:** altı haneli hex (`#RRGGBB`); alfa taşıyan
  token'lar için sekiz haneli hex (`#RRGGBBAA`) ve bu, Token tablosunda işaretlidir. Başka
  hiçbir biçim (`rgb()`, `oklch()`, renk adı) kabul edilmez. Biçime uymayan bir değer
  **enjekte edilmez** ve uygulama ayağa kalkmaz.
  *(Gerekçe: iki iş birden yapıyor. Birincisi, kontrast hesabı ayrıştırma belirsizliğinden
  kurtulur. İkincisi, bu değer `index.html`'e enjekte ediliyor ve bugünkü enjeksiyon hiçbir
  kaçış yapmıyor — bugün gerek yok, çünkü yalnız doğrulanmış `locale.Code` giriyor. Serbest
  metin girdiği gün durum değişir; `0005`'in seed-media ucu aynı riski "aynı origin'de
  yansıtılmış XSS" diye yazmıştı.)*
- **C12.** Varsayılan paletin ve yapılandırılan temanın kontrast doğrulaması **aynı eşik
  tablosunu** okur; eşik iki yerde tanımlanmaz (R7'nin iki kapısı, tek kaynak).

### KAPSAM DIŞI

- **X1.** **Ekran düzeninin mockup'a göre yeniden kurulması** (ızgara genişliği, hero
  bölümü, kart ölçüleri, pill biçimli düğmeler, araç silüeti) — *neden:* ADR 0003 unsur 1
  tamamlanmış spec'lerin geriye dönük değiştirilmemesini söylüyor; her ekranın düzeni kendi
  spec'ine aittir. Bu spec **yalnızca token katmanıdır**. → Karar **D5**.
- **X2.** **Koyu mod / tema değiştirici** — *neden:* `frontend-001` § X3 bunu zaten kapsam
  dışı bıraktı. Bu spec token'ları anlamsal adlarla kurduğu için koyu mod sonradan
  **ucuzlar**, ama bu spec'te yok.
- **X3.** **Panelden renk seçici** — *neden:* galeriye kontrast garantisini bırakmak
  erişilebilirlik tabanını (`docs/frontend.md` § 7) yapılandırma hatasına açar; ayrıca
  `docs/architecture.md` § 6'nın "basit modeli motora dönüştürme" uyarısının aynı biçimi.
  → Karar **D1**.
- **X4.** **Kurulum başına ayrı derleme çıktısı** — *neden:* tema çalışma anında
  enjekte edilir (C4); kurulum başına build, dağıtım sayısını ve CI süresini çarpar.
- **X5.** **Logo yükleme ve işleme** — *neden:* logo `Media` referansıdır ve `Media` modülü
  bugün yalnızca `Contracts`'tan ibarettir. Bu spec logoyu **gösterir**, yüklemez.
- **X6.** **Mockup'taki araç silüeti ve ikon seti** — *neden:* ikonlar bir varlık kümesidir
  ve `Media` ile birlikte ele alınır; bu spec renk ve tipografi katmanıdır.
- **X7.** **E-posta şablonlarının tema alması** — *neden:* tema `index.html` enjeksiyonuna
  bağlıdır (C4) ve e-posta istemcilerinin çoğunda CSS değişkeni çalışmaz. `0009`'un
  şablonları galeri kimliğinden bağımsız kalır; e-postaya tema rengi taşımak kendi
  spec'ini hak eder.
- **X8.** **Pazarlama ve galeri kimliği kılavuzu üretimi** (logo kullanım kuralları, ton) — *neden:*
  ürün işi değil.
- **X9.** **Tipografinin yapılandırmaya açılması** — *neden:* yapılandırma bir aile **adı**
  verebilir ama **dosyasını** veremez; D8 yazı tiplerini kendi kaynağımızdan servis ediyor.
  Dosyası olmayan bir aile adında tarayıcı sessizce başka fonta düşer — tuzak 3'ün tam
  kendisi. Tipografiyi yapılandırmaya açmak, font dosyası dağıtımını da açmayı gerektirir ve
  bu X4 (kurulum başına build) ile çakışır. İki varsayılan yığın **sabittir**.
  → Karar **D9**. *(Beyaz etiketlemenin tipografi yarısı kendi spec'ini hak eder.)*
- **X12.** **Zemin ve kart yüzeylerinin mockup'a göre yeniden atanması** (bugünkü gri
  gövde + beyaz kart düzeninin takas edilmesi) — *neden:* X1 ile aynı gerekçe. Bu spec'in
  işi **hangi token'ın hangi anlamı taşıdığı**; hangi yüzeyin hangi token'ı alacağı ekranın
  kendi kararıdır ve ADR 0003 unsur 7'nin sırasına göre ekran başına yapılır. Token'lar
  (`--color-surface`, `--color-surface-muted`) bu spec'te doğru tanımlanıyor ve kontrast
  kapısından geçiyor; atama değişimi hiçbir token eklemeden yapılabilir.
- **X11.** **`--color-border`'ın kontrast kapısına alınması** — *neden:* token hem
  dekoratif kart kenarlığı hem form denetimi sınırı olarak kullanılıyor ve kapı ikisini
  ayırt edemez. WCAG 1.4.11 3:1'i yalnız denetimi tanımlayan sınır için istiyor; dekoratif
  kenarlığa uygulamak **yanlış kırmızı** üretir. Ölçüldü: `--color-border`'ın mockup'tan
  okunan değeri `#E4E5E1` beyaz zemine karşı **1.27:1** — yani blanket 3:1 kuralı
  tasarımcının kendi kenarlığını anında reddederdi. *(D4 öncesi değer `#d9dde4` ile 1.36:1
  ölçülmüştü; karar değişmiyor, pay büyüyor.)*
  `--color-border` yapılandırılabilir kalır ama kapısı yoktur; düşük kontrastlı bir
  kenarlık metin gibi okunamaz hâle gelmez, zarif bozulur. Denetim sınırlarının kendi
  token'ına ayrılması kendi spec'ini hak eder.
- **X10.** **Galeri logosunun gösterilmesi** — *neden:* gösterecek bir URL **yok**
  (doğrulandı): `Dealership.Contracts` yalnız `BranchSummaryDto(BranchId, Name, Status)`
  taşıyor, logo alanı dışa açılmamış ve `Media` modülü bugün yalnız `Contracts`'tan ibaret.
  R8 bu yüzden **ada** indirildi; logo gösterimi `Media` spec'ine aittir.

---

## Context

### Mevcut davranış — doğrulandı

- **Token mekanizması kurulu.** `frontend/src/shared/design/tokens.css` var; dosyanın kendi
  yorumu *"Değerler YALNIZCA burada yaşar... tema, koyu mod ve marka değişimi tek dosyadan
  yapılabilsin diye"* diyor. `frontend-001` AC-15 bunu lint kapısına bağladı.
- **Ama değerler yer tutucu.** Bugünkü değerler mockup'la hiçbir yerde örtüşmüyor:

  | Token | Bugün | Mockup (tasarımcı notu `s4`) |
  |---|---|---|
  | vurgu | `#1f5fd0` | `#CDF564` dolgu + `#4F6B00` metin |
  | yüzey grisi | `#f4f5f7` | `#F3F4F1` |
  | metin | `#16191d` | `#0B0C0E` |
  | tipografi | `system-ui, -apple-system, 'Segoe UI'` | Space Grotesk; Arapça: Readex Pro + IBM Plex Sans Arabic |

  `#1f5fd0`, `docs/frontend.md` § 4.1'de **yasak örneği** olarak yazılı hex'in aynısıdır.
- **Tek bir `--color-accent` var.** Dolgu/metin ayrımı bugün yok; mockup'ın kuralı
  (*"limon yalnızca DOLGU"*) token katmanında temsil edilemiyor. → Karar **D2**.
- **Tipografi tek yığın.** `--font-family-base` yalnızca `system-ui` zinciridir; Arapça
  için ayrı yığın yok. Tasarımcı notu `s4` bunu gerekçesiyle yazmış: *"Arapça ekranda Space
  Grotesk'ün Arapça karakteri olmadığı için Readex Pro + IBM Plex Sans Arabic kullanılıyor."*
- **Enjeksiyon mekanizması zaten var.** `src/Host/Localization/IndexHtmlRenderer.cs` host'ta
  duruyor ve kendi yorumu *"Tam SSR yok; host index.html'i servis ederken rota bazlı
  lang/dir ve meta enjekte eder"* diyor. `frontend/index.html` bugün boş `lang=""` ve
  `dir=""` ile geliyor — yani yer tutucu enjeksiyon deseni kurulu. Tema aynı kapıdan girer;
  C4 bu yüzden ikinci mekanizma yasaklıyor.
- **Galeri kimliği zaten veri.** `Gallery.LogoMediaAssetId` ve `SetLogo` mevcut
  (`src/Modules/Dealership/Dealership/Domain/Gallery.cs`); galeri adı da profilde. Yani
  eksik olan yalnızca renk ve tipografi. → Karar **D6**.
- **Yapılandırma eksikse ayağa kalkmama deseni kurulu.** `0009` C8 sağlayıcı kimlik
  bilgileri için aynı kuralı koymuş: *"yapılandırmadan gelir ve eksikse uygulama ayağa
  kalkmaz."* R7 bu deseni kontrast için kullanır, yeni bir şey icat etmez.
- **Düzen farkı büyük ama bu spec'in dışında.** Mockup 1440px kolon, 999px pill'ler,
  16–22px kart köşeleri ve hero bölümü kullanıyor; bugünkü `--layout-max-inline-size`
  1200px. X1 bunu kapsam dışı bırakıyor.

### Token tablosu — kapalı liste

R2, R5, R6 ve R13 bu tabloya dayanır. **Tablonun dışında varsayılan yoktur ve tabloda
olmayan anahtar yapılandırılamaz.** `yapılandırılabilir = hayır` olan satırlar
`tokens.css`'te sabit kalır.

| Token | Varsayılan | Yapılandırılabilir | Büyük metin (≥24px) |
|---|---|---|---|
| `--color-surface` (zemin) | `#FFFFFF` | evet | — |
| `--color-surface-muted` (yüzey grisi) | `#F3F4F1` | evet | — |
| `--color-ink` (gövde metni) | `#0B0C0E` | evet | **evet** |
| `--color-accent-fill` (dolgu vurgusu) | `#CDF564` | evet | — |
| `--color-accent-text` (metin vurgusu) | `#4F6B00` | evet | **evet** |
| `--color-on-accent-fill` (dolgu üzerindeki metin) | `var(--color-ink)` | hayır (dolgudan **türetilmez**, gövde metnine bağlı) | — |
| `--color-focus-ring` (odak halkası) | `var(--color-accent-text)` | hayır | — |
| `--color-ink-inverse` | `#FFFFFF` | evet **(D13)** | — |
| `--color-ink-muted` (yardımcı metin) | `#5F656C` *(okundu)* | evet | — |
| `--color-border` | `#E4E5E1` *(okundu)* | evet | — *(kontrast kapısı dışında → X11)* |
| `--color-danger` (hata metni) | `#B8412A` *(okundu)* | evet **(D13)** | — |
| `--color-danger-surface` | `#FBECE9` *(**türetildi** — D4 Ek)* | evet **(D13)** | — |
| `--color-accent-surface` | `#EDF6D6` *(okundu)* | evet **(D13)** | — |
| `--color-backdrop` | `#0B0C0E8C` *(**türetildi** — D4 Ek; alfa taşır → `#RRGGBBAA`, C11)* | hayır *(metin taşımaz)* | — |
| `--font-family-base` (Latin) | Space Grotesk + sistem yedeği | hayır (X9) | — |
| `--font-family-arabic` | Readex Pro, IBM Plex Sans Arabic + sistem yedeği | hayır (X9) | — |

### Çift tablosu — kontrast kapısının tek kaynağı

C12 eşiğin iki yerde tanımlanmamasını şart koşuyor; bu tablo o tek yerdir. **R7'nin iki
kapısı da — başlangıçtaki yapılandırma kapısı ve CI'daki varsayılan palet testi — aynı
satırları okur.** Çiftler spec'ten değil, CSS'te **gerçekten var olan** kullanımlardan
türetildi.

| Çift | Eşik | Kriter |
|---|---|---|
| `--color-accent-text` ↔ `--color-surface` | 4.5:1 | AC-17 |
| `--color-accent-text` ↔ `--color-surface-muted` | 4.5:1 | AC-17 |
| `--color-ink` ↔ `--color-surface` | 4.5:1 | AC-18 |
| `--color-ink` ↔ `--color-surface-muted` | 4.5:1 | AC-18 |
| `--color-on-accent-fill` ↔ `--color-accent-fill` | 4.5:1 | AC-19 |
| `--color-focus-ring` ↔ `--color-surface` | 3:1 | AC-20 |
| `--color-focus-ring` ↔ `--color-surface-muted` | 3:1 | AC-20 |
| `--color-focus-ring` ↔ `--color-accent-fill` | 3:1 | AC-20 |
| `--color-ink-muted` ↔ `--color-surface` | 4.5:1 | **AC-31** |
| `--color-ink-muted` ↔ `--color-surface-muted` | 4.5:1 | **AC-31** |
| `--color-accent-text` ↔ `--color-accent-surface` | 4.5:1 | **AC-32** |
| `--color-danger` ↔ `--color-surface` | 4.5:1 | **AC-33** |
| `--color-danger` ↔ `--color-danger-surface` | 4.5:1 | **AC-33** |
| `--color-ink-inverse` ↔ `--color-ink` | 4.5:1 | **AC-34** |

Son altı satır ilk taslakta yoktu ve üçü CSS'te bugün kullanılıyor: `--color-ink-muted`
on yerde yardımcı metin, `--color-danger` beş yerde hata metni, `--color-ink-inverse`
rozet metni. **`--color-ink-muted` yapılandırılabilirdi ve hiçbir kapısı yoktu** — bir
kurulum onu açık griye ayarlasa uygulama açılır, kapı yeşil kalır ve bütün yardımcı
metinler okunmaz olurdu. R7'nin vaadi tam orada boşa çıkıyordu.

**Tablo tamamlandı (2026-10-05, D4 Ek).** On dört renk token'ının on dördü değer taşıyor.
Mockup'ın palet NOTU yalnız dördünü veriyordu; kalan altısı mockup'ın KENDİSİNDEN —
on bir artboard'ın satır içi stillerinden — okundu ya da tasarımcının kendi tarifiyle
türetildi. *(Gerekçe: R6'nın "ya tam ya hiç" kuralı kapalı bir liste olmadan tanımlanamaz;
"tam" tanımsızken AC-11 ve AC-12 test edilemez.)*

### Çağrı yeri sayımı — D2 bir göçtür

`var(--color-accent)` bugün **yedi** çağrı yerinde ve **üç** farklı anlamda kullanılıyor:

| Anlam | Yer | Yeni token |
|---|---|---|
| metin rengi | `styles.css:77`, `:198`, `:568`, `ui.css:29` | `--color-accent-text` |
| dolgu | `ui.css:16` | `--color-accent-fill` |
| kenarlık | `styles.css:76` | `--color-accent-text` *(metin taşıyan öğenin kenarlığı)* |
| odak halkası | `styles.css:88` | `--color-focus-ring` |
| **dolgu üzerindeki metin** | **`ui.css:17`** | **`--color-on-accent-fill`** |

**Göç `var(--color-accent)` çağrılarıyla sınırlı DEĞİLDİR; dolgu üzerine yazan her yeri
kapsar.** Sekizinci satır ilk sayımda yoktu, çünkü sayım `--color-accent` dizesini arıyor
ve o satırda o dize geçmiyor: `.ui-button--primary` bugün **mavi dolgu + beyaz metin**
(`color: var(--color-ink-inverse)`). Dolgu limon olduğunda beyaz metin kalır ve ölçüldü:
**#FFFFFF / #CDF564 = 1.26:1.** Okunamaz.

AC-19'un kapısı bunu **göremez**: kapı token çiftini denetler, çağrı yerini denetlemez —
`--color-on-accent-fill` ↔ `--color-accent-fill` çifti yeşil kalır ve düğme yine okunmaz.
Bu, `frontend-001`'in SCORECARD dersinin ("değer doğru, anlam yanlış") birebir aynı biçimi.

Yani bu spec "yalnızca token katmanı" değil, aynı zamanda bir **çağrı yeri göçüdür**.
`frontend-001`'in SCORECARD dersi aynen buraya bakıyor: *"değer doğru, anlam yanlış."*
Her çağrı yerinin hangi yeni token'a gittiği PR'da tabloyla gösterilir (DoD).

### Bilinen tuzaklar

1. **Tek vurgu token'ı.** `--color-accent`'i limon yapmak, birinin onu metin rengi olarak
   kullanmasına ve 4.5:1 eşiğinin sessizce kırılmasına yol açar. Tasarımcı bu tuzağı
   kendisi işaretlemiş; D2 onu token katmanına taşır.
2. **Değer temelli token adı.** `--color-lemon` yazılırsa, müşteri rengi mavi olduğu gün ad
   yalan söyler. C6 bunu yasaklar.
3. **Arapça fontu Latin yığınına bırakmak.** Space Grotesk'te Arapça karakter yoktur;
   tarayıcı sessizce başka bir fonta düşer ve Arapça sayfa tipografik olarak bozulur.
4. **Dört durumu birbirine karıştırmak.** **Hiç** tema verilmemesi varsayılana düşer;
   **kısmi** tema uygulamayı durdurur (R6); **tanınmayan anahtar veya biçime uymayan değer**
   durdurur (C11); **kontrastı yetersiz** tema durdurur (R7). Bunları birleştirmek, yanlış
   kontrastlı bir kurulumun sessizce açılmasına yol açar. C5 dördünü ayırır.
   *(Not: bu maddenin önceki hâli "eksik değer varsayılana düşer" diyordu — R6'nın tam
   tersi. Düzeltildi.)*
5. **Token geçişinde "değer doğru, anlam yanlış" hatası.** `frontend-001`'in
   SCORECARD dersi aynen bu: *"köşe token'ı boşluk olarak, boşluk token'ı köşe olarak,
   kenarlık token'ı ölçü olarak kullanılmıştı; `font-family` ise hiç bağlanmamıştı."*
   Bu spec aynı dosyaya dokunuyor; aynı hata biçimi yine mümkündür.
6. **Odak halkasının yeni palette kaybolması.** Limon zemin üzerinde odak halkası
   görünmezse klavye kullanıcısı nerede olduğunu yitirir (R11).

### Verilen kararlar

**D1–D3 ve D5–D9 onaylandı** (2026-10-04). Uygulama planından çıkan dört karar daha
eklendi ve aynı turda onaylandı: **D10–D13**.

**D4 bir tur sonra, 2026-10-05'te onaylandı** (PR #32). Onay turunda ayrı tutulmuştu:
Token tablosunun altı satırı değer taşımıyordu ve karar doğrulanamıyordu. **Ayrı tutmak
işe yaradı** — diğerlerinin onaylanması token katmanının, enjeksiyonun, iki kontrast
kapısının ve çağrı yeri göçünün tabloyu beklemeden kurulmasına izin verdi; yalnız
**varsayılan renk değerleri** en sona kaldı ve altısı birlikte kapandı. Nasıl bulundukları
aşağıda, **D4 Ek**'te.

**D1 — Tema değerleri dağıtım yapılandırmasından gelir; panelden düzenlenmez.**
*Gerekçe:* bu, projenin zaten kurduğu desen — varsayılan dil *"kurulum yapılandırmasından"*,
para birimi *"dağıtım yapılandırmasında sabit ve panelden düzenlenemez"* (**P3**). Renk de
aynı sınıfa girer. Panelden renk seçici, kontrast garantisini galeriye devreder ve
`docs/frontend.md` § 7'nin erişilebilirlik tabanını bir renk seçicinin arkasına koyar.
*Bilerek ödenen bedel:* galeri rengini kendi değiştiremez; değişiklik bir yapılandırma ve
bir yeniden dağıtım ister. Tek galeriye tek kurulum yapılan bir üründe bu seyrek bir iştir.
*Elenen alternatif:* renkleri galeri profiline (veriye) koymak — o zaman R7'nin kontrast
kapısı çalışma anında, galeri kaydet düğmesine bastığında devreye girmek zorunda kalır ve
"kaydedemiyorum" sorunu galeri çalışanının önüne düşer.

**D2 — Dolgu vurgusu ve metin vurgusu ayrı token'lardır.**
`--color-accent-fill` ve `--color-accent-text` gibi iki ayrı anlamsal token.
*Gerekçe:* tasarımcı notu `s4` limonu *"yalnızca DOLGU"* diye kısıtlıyor ve metin için ayrı
bir koyu zeytin veriyor. Tek token bu ayrımı taşıyamaz. *Bilerek ödenen bedel:* token sayısı
artar ve bir kurulum iki rengi birbirine uyumsuz seçebilir — R7 bunu yakalar.

**D3 — Kontrast eşiği uygulama başlangıcında doğrulanır; karşılanmazsa ayağa kalkılmaz.**
*Gerekçe:* `0009` C8 aynı deseni kurmuş. Erişilebilirlik tabanının bir yapılandırma
hatasıyla sessizce kırılması, kırıldığının fark edilmemesi demektir — ve renk hatası
gözle yıllarca fark edilmez. *Bilerek ödenen bedel:* yanlış renk verilen bir kurulum
açılmaz. Bu bilinçlidir: açılan ama okunamayan bir vitrinden iyidir.
*Elenen alternatif:* uyarı loglayıp devam etmek — logu kimse okumaz.
**Dağıtım kaydı:** yapılandırma doğrulaması **dağıtım kesilmeden önce** koşar ve başarısız
bir örnek **trafik almaz**. Kademeli dağıtımda yeni örnek açılmaz, eski örnek çalışmaya devam
eder — istenen davranış budur. Doğrudan değiştirilen bir dağıtımda ise aynı karar çalışan
vitrini indirir; bu yüzden kesme öncesi doğrulama kararın ayrılmaz parçasıdır.

**D4 — Varsayılan tema mockup'ın paletidir.** *(**ONAYLANDI** — 2026-10-05.)*
*Gerekçe:* kutudan çıkınca ürün mockup gibi görünür; yeni kurulum renk vermeden çalışır (R6).
*Bedel — ödendi:* altı token değeri mockup'ın palet NOTUNDA yoktu ve karar bu yüzden
doğrulanamıyordu. Altısı da artık tabloda; nasıl bulundukları aşağıda.

**D4 Ek — altı değerin kaynağı.**
Beklenen şey bir evet/hayır değil, **eksik veriydi**. Değerler tasarımcıdan ayrıca
istenmedi çünkü mockup'ta zaten vardı: palet notu dört satır veriyor, ama on bir
artboard'ın satır içi stilleri on dört rengin tamamını fiilen kullanıyor. **Dördü oradan
okundu, ikisi tasarımcının kendi tarifiyle türetildi.** Bu spec türetmeye izin veriyor:
"türetilmiş değer de geçerli bir satırdır, boş satır değildir."

| Token | Değer | Nasıl bulundu |
|---|---|---|
| `--color-ink-muted` | `#5F656C` | **Okundu.** Gezinme bağlantıları, hero paragrafı, kart etiketi, alt bilgi — 45 kullanım. *Mockup'ın en SIK yardımcı metin rengi bu değil (`#6C7178`, 80 kullanım); o ölçüldü ve `--color-surface-muted` üzerinde **4.45:1** verip AC-31'in eşiğini kırıyor. Seçilen değer de tasarımcının kendi değeri ve iki zeminde de geçiyor.* |
| `--color-border` | `#E4E5E1` | **Okundu.** Mockup'ın en sık kenarlığı: 94 `solid` bildiriminin 32'si (girdi, hap, açılır kutu). |
| `--color-danger` | `#B8412A` | **Okundu.** Tasarımcının hata **metni** rengi: panel ilan satırlarındaki tehlike durumu, "· boş" dil uyarısı. *Gövde parçası dolgusu `#D9543C` DEĞİL — o beyaz zeminde 3.97:1 verir ve metin olarak kullanılamaz.* |
| `--color-danger-surface` | `#FBECE9` | **Türetildi.** Mockup'ta açık kırmızı bir yüzey hiç yok. Tasarımcının kendi yüzey tarifi uygulandı: kehribar çifti `#FBEFD6` ⇄ `#7A5510`, limon çifti `#EDF6D6` ⇄ `#3E5400` — ikisi de aynı hue'da ~%90 açıklık. Aynı tarif tehlike hue'suna (10°) uygulandı ve açıklık **%95'e** çekildi: %91'de çift 4.31:1 ile kapıyı kırıyordu. |
| `--color-accent-surface` | `#EDF6D6` | **Okundu.** Tasarımcının limon tonlu yüzeyi; panelde `--color-accent-text` ile birlikte kullanılıyor. |
| `--color-backdrop` | `#0B0C0E8C` | **Türetildi.** Mockup'ta modal yok, dolayısıyla perde de yok. Ink tasarımcının (`#0B0C0E`); alfa onun iki örtüsünün ortası — kaydırıcı noktası 0.28, fotoğraf sayacı 0.78 → 0.53 ≈ **0.55** (`8C`). *0.78 bir modal perdesi için fazla ağır, 0.28 altındaki tabloyu ayırmıyor.* |

**Kontrast kapısı ölçüldü, iddia edilmedi** (AC-24, 14/14 çift geçti). Yeni değerleri
ilgilendiren beş satır:

| Çift | Oran | Eşik | Kriter |
|---|---|---|---|
| `--color-ink-muted` ↔ `--color-surface` | **5.89:1** | 4.5 | AC-31 |
| `--color-ink-muted` ↔ `--color-surface-muted` | **5.34:1** | 4.5 | AC-31 |
| `--color-accent-text` ↔ `--color-accent-surface` | **5.46:1** | 4.5 | AC-32 |
| `--color-danger` ↔ `--color-surface` | **5.49:1** | 4.5 | AC-33 |
| `--color-danger` ↔ `--color-danger-surface` | **4.77:1** | 4.5 | AC-33 |

*En dar pay son satırda: 0.27. Daha açık bir yüzey payı büyütürdü ama yüzey beyazdan
ayırt edilemez hâle gelir ve hata durumu görsel olarak kaybolur.*

**Tasarımcı sonradan farklı bir ton isterse bedel tek commit'tir:** değerler yalnız
`tokens.css`'te yaşıyor ve aynı kapı onları yeniden ölçer.

**D5 — Düzen bu spec'te değişmez.**
*Gerekçe:* ADR 0003 unsur 1. Mockup'ın düzeni mevcut ekranlardan belirgin biçimde farklı
(1440 kolon, hero, pill'ler) ve bu, tamamlanmış spec'lerin kabul kriterlerine dokunur.
Düzen işi ekran başına ve ADR 0003 unsur 7'nin sırasına göre yapılır.
*Bilerek ödenen bedel:* bu spec bittiğinde arayüz mockup'ın **rengini** taşır ama
**düzenini** taşımaz; mockup'ı gören biri yarı yol görür.

**D6 — Galeri kimliği veridir, tema değildir.**
Galeri **adı** profilden gelir ve bu spec onu gösterir (`0003` R1, R8); renk yapılandırmadan
gelir. Sınır budur ve karıştırılmaz. **Logo** da profilin verisidir
(`Gallery.LogoMediaAssetId`) ama bu spec onu **göstermez** — gösterecek bir URL yok
(`Dealership.Contracts` logo alanını dışa açmıyor, `Media` modülü yalnız `Contracts`'tan
ibaret). → X10.
*Gerekçe:* ad ve logo galeri başına **içerik**, renk ise **sunum**; ikincisi kontrast
doğrulaması gerektirir, birincisi gerektirmez.

**D7 — Arapça için ayrı tipografi yığını token olarak tanımlanır.**
*Gerekçe:* tasarımcı notu `s4` ve `docs/architecture.md` § 3. *Bedel:* iki font yığını
yüklenir; Arapça olmayan kurulumda da Arapça yığını tanımlı kalır (yüklenmesi locale'e
bağlanır).

**D8 — Yazı tipleri kendi kaynağımızdan servis edilir; üçüncü taraf sağlayıcı kullanılmaz.**
*Gerekçe:* mockup'lar yazı tiplerini Google Fonts `<link>`'i ile yüklüyor ve `docs/` altında
dış kaynak kullanımına dair **hiçbir kural yok** (arandı). Üçüncü taraf font sağlayıcısı,
vitrini açan her ziyaretçinin IP adresini o sağlayıcıya gönderir. Bu projede KVKK bir ek
değil, ürünün içinde: aydınlatma metni, iletişim izni kaydı (**L7**) ve talep
anonimleştirmesi (**L6**) hep kapsamda. Ziyaretçinin IP'sini font indirmek için üçüncü
tarafa göndermek, o özenin yanında tutarsız kalır.
*Bilerek ödenen bedel:* yazı tipi dosyaları depoya ve dağıtıma girer; lisans uygunluğu bir
kez kontrol edilmek zorundadır ve yeni bir ağırlık eklemek dosya eklemek demektir.
*Elenen alternatif:* Google Fonts kullanıp bunu bilinçli bir karar olarak yazmak — daha az
iş, ama sessiz bir KVKK yüzeyi bırakır ve o yüzey hiçbir spec'te görünmez.
*Not:* bu karar yalnızca **nereden** servis edildiğini bağlar; **hangi** yazı tipi
kullanıldığını D4 (varsayılan tema) belirler.

**D9 — Tipografi V1'de yapılandırılamaz; yalnız renk yapılandırılabilir.**
*Gerekçe:* yapılandırma bir aile **adı** verebilir ama **dosyasını** veremez — D8 yazı
tiplerini kendi kaynağımızdan servis ediyor. Dosyası olmayan bir aile adında tarayıcı
sessizce başka fonta düşer ve Arapça sayfa tipografik olarak bozulur: tuzak 3. Tipografiyi
yapılandırmaya açmak font dosyası dağıtımını da açmayı gerektirir ve bu X4 ile çakışır.
*Bilerek ödenen bedel:* beyaz etiketlemenin tipografi yarısı bir sonraki spec'e kalır; R5
renge indirilir. *Elenen alternatif:* aile adını yapılandırmaya açıp dosya yüklemeyi
sonraya bırakmak — sessiz fallback üretir ve o bozulma yalnız Arapça sayfada, gözle görülür.

**D10 — Tema, mevcut `<html>` etiketine `style` özniteliği olarak enjekte edilir.**
Ayrı bir `<style>` bloğu veya ikinci bir yer tutucu eklenmez.
*Gerekçe:* iki nedenle. **Birincisi C4:** bu tam olarak mevcut enjeksiyon noktası —
`IndexHtmlRenderer` bugün `<html lang="" dir="">` yer tutucusunu zaten değiştiriyor — yani
ikinci bir mekanizma kurulmuyor. **İkincisi sıra riski:** Vite, CSS `<link>`'ini build
sırasında `<head>`'e kendisi ekliyor. `<style>:root{…}` bloğu o link'ten ÖNCE düşerse
`tokens.css` temayı ezer ve bu **sessizce** olur; aynı özgüllükte son bildirim kazanır.
Inline `style` özniteliği `:root` kuralını her zaman yener ve sıraya hiç bakmaz.
C11'in hex kısıtı öznitelik kaçışı sorununu da kapatıyor.
*Bilerek ödenen bedel:* `<html>` etiketi uzar ve kaynağı görüntüleyen biri on küsur
özel özellik görür.

**D11 — Eşik ve çift tablosu host tarafında, C#'ta yaşar; varsayılan palet testi
`tokens.css`'i OKUR ve aynı tabloyu kullanır.**
*Gerekçe:* R7 iki kapı istiyor, C12 eşiğin tek yerde tanımlanmasını istiyor. İki kapı iki
dilde yazılırsa eşik iki yerde yaşar. Host tarafı seçildi çünkü başlangıç kapısı orada
zorunlu olarak yaşıyor (C4). **R6'nın gerekçesi korunur:** uygulama varsayılan değerleri
bilmemeye devam eder — `tokens.css`'i okuyan şey **test**, uygulama değil. Bir testin
spec'i bilmesi zaten testin işidir.

**D12 — Tema doğrulaması `src/Host/Theme/` altında yapılandırma doğrulaması olarak yaşar;
bunun için modül açılmaz.**
*Gerekçe:* emsal zaten host'ta — `LocalizationOptions` ve `ForwardedHeadersSetup` kurulum
yapılandırmasını host'ta doğruluyor (`docs/conventions.md` § 11). Tema enjeksiyonu C4 ile
host'un işi. Bunun için modül açmak, veri sahibi olmayan bir sunum modülü üretirdi ve
Yasak Listesi 20'nin yasakladığı şey iş mantığıdır, kurulum yapılandırması değil.

**D13 — Kontrast çiftine giren her token yapılandırılabilirdir; sabit kalan yalnız metin
taşımayan token'lar ve türetilmişlerdir.**
`--color-ink-inverse`, `--color-danger`, `--color-danger-surface` ve
`--color-accent-surface` bu yüzden "yapılandırılabilir" sütununa geçti.
*Gerekçe:* kapı bir çifti ancak **iki ucunu da biliyorsa** denetleyebilir. Bir ucu
`tokens.css`'te sabit kalırsa host onu bilemez (R6: varsayılanlar host'ta yaşamaz) ve çift
denetimsiz kalır. Alternatif, sabit değerleri host'a da yazmaktı — paletin iki yerde
yaşaması, yani R6'nın önlemek için var olduğu şey.
*Bilerek ödenen bedel:* tema veren bir dağıtım altı değil **on** renk anahtarı vermek
zorunda. R6 "ya tam ya hiç" derken bu bedeli zaten seçmişti; D13 onu büyütüyor, türünü
değiştirmiyor.

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler görsel kanıt gerektirir (`docs/testing.md` § 3). Varsayılan
> değer içeren kriterler Context § **Token tablosu**'na bakar; eşik içeren kriterler **R7**'nin
> iki kapısını ayırır.

### A. Bugün karşılanıyor — regresyon koruması

- **AC-1.** Latin marka adları ve fiyatlar Arapça metin içinde doğru yönde görünür.
  *(`dir="auto"` ile `0001`/`0002`'de kuruldu; bu kriter font değişiminin onu bozmadığını
  gösterir, yeni davranışın kanıtı değildir.)*

### B. Token katmanı

- **AC-2.** Dolgu vurgusu, metin vurgusu, dolgu üzerindeki metin ve odak halkası **dört ayrı
  token** olarak tanımlıdır.
- **AC-3.** `frontend/src` altındaki CSS dosyalarında ham hex renk, ham piksel veya ham font
  boyutu bulunmaz; **token'ların tanımlandığı dosya hariçtir** ve lint bu muafiyeti taşır.
  Muafiyet dışındaki bir ihlalde lint kırar.
- **AC-4.** Token adlarının hiçbiri renk adı içermez (`lemon`, `blue` gibi); adlar anlamsaldır.
- **AC-5.** Tipografi token'ları Latin ve Arapça için iki ayrı yığın tanımlar.
- **AC-6.** Context § Token tablosundaki **her** token tanımlıdır ve varsayılan temadaki
  hesaplanan değeri tablodaki değerle aynıdır.
- **AC-7.** `[görsel]` Varsayılan yapılandırmayla açılan vitrin listesinde **fiyat metni
  metin vurgusuyla** (koyu zeytin) basılır ve **dolgu vurgusu yalnız dolgu olarak**
  kullanılır — hiçbir yerde metin rengi değildir.
  *(İlk hâli dört şey sayıyordu ve ikisi bu spec'in işi değildi: "beyaz zemin, yüzey grisi
  kart alanı" bugünün tersi — gri gövde, beyaz kart — ve düzeltmesi gövde ile kart
  zeminini takas etmek, yani sekiz kuralı etkileyen görsel bir yeniden kurgu. "Kart alanı"
  ifadesi ayrıca belirsiz: tek tek kartlar mı, ızgaranın oturduğu panel mi? Mockup'ı
  göremeyen biri bunu tahmin edemez ve yanlış tahminin bedeli beş ekranda görsel
  regresyon. Yüzey ataması → **X12**.)*
- **AC-8.** Sayfa yüklenirken üçüncü taraf bir alan adına yazı tipi isteği yapılmaz; yazı
  tipleri uygulamanın kendi kaynağından gelir.

### C. Kuruluma özel tema

- **AC-9.** Yapılandırmada dolgu vurgusu farklı bir renge ayarlandığında arayüz o rengi
  kullanır.
- **AC-10.** Hiçbir tema değeri verilmediğinde arayüz varsayılan temayla çalışır ve hiçbir
  renk boş kalmaz.
- **AC-11.** Tema **kısmen** verildiğinde — tablonun yapılandırılabilir sütunundaki bir
  anahtar eksikken — uygulama ayağa kalkmaz ve **hangi token'ın eksik olduğunu** söyler.
- **AC-12.** Token tablosunda **olmayan** bir anahtar verildiğinde uygulama ayağa kalkmaz ve
  tanınmayan anahtarı söyler.
- **AC-13.** Altı haneli hex biçiminde olmayan bir renk değeri verildiğinde uygulama ayağa
  kalkmaz, hangi token olduğunu söyler ve **hiçbir değer enjekte edilmez**.
- **AC-14.** Tema değerleri mevcut `index.html` enjeksiyon yolundan gelir; ikinci bir
  mekanizma eklenmez.
- **AC-15.** Tema değerleri arayüz paketinin içine gömülmez; aynı paket iki farklı tema
  yapılandırmasıyla iki farklı görünüm verir.
- **AC-16.** Yalnız renk yapılandırılabilir: yapılandırmaya tipografi, köşe veya aralık
  anahtarı verilmesi AC-12 kapsamında reddedilir.

### D. Kontrast kapısı — yapılandırılan tema (uygulama başlangıcında)

- **AC-17.** Metin vurgusu, zemin ve yüzey grisinin **her birine** karşı en az 4.5:1 kontrast
  sağlar; sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-18.** Gövde metin rengi, zemin ve yüzey grisinin **her birine** karşı en az 4.5:1
  kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-19.** Dolgu üzerindeki metin token'ı, dolgu vurgusuna karşı en az 4.5:1 kontrast
  sağlar; sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-20.** Odak halkası, zemin, yüzey grisi ve dolgu vurgusunun **her birine** karşı en az
  3:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-21.** Kapı eşiği **Context § Çift tablosundan** okur; tabloda yazılı olmayan bir
  eşik uygulanmaz ve tabloda yazılı her çift denetlenir.
  *(İlk hâli "büyük metin işaretli çiftlere 3:1" diyordu ve AC-17/AC-18 ile çelişiyordu:
  tablo token'ları işaretliyor, eşik ise çift bazlı ve `--color-ink` gövde metnidir — on
  yerde normal boyutta kullanılıyor. Bir token'ın "bazen 24px kullanılıyor" olması, normal
  boyuttaki kullanımının eşiğini düşürmez; tersi AC-18'i geçersiz kılardı. **Büyük metin
  sütunu bilgilendirici kaldı**; eşiği çift tablosu verir.)*
- **AC-22.** Uygulama kontrast nedeniyle ayağa kalkmadığında hangi **token çiftinin** hangi
  eşiği karşılamadığını söyler.
- **AC-23.** Tüm eşikleri karşılayan bir yapılandırmayla uygulama normal açılır.

- **AC-31.** Yardımcı metin rengi, zemin ve yüzey grisinin **her birine** karşı en az
  4.5:1 kontrast sağlar; sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-32.** Metin vurgusu, vurgu yüzeyine karşı en az 4.5:1 kontrast sağlar;
  sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-33.** Hata rengi, zemine ve hata yüzeyine karşı en az 4.5:1 kontrast sağlar;
  sağlamıyorsa uygulama ayağa kalkmaz.
- **AC-34.** Ters metin rengi, gövde metin rengine karşı en az 4.5:1 kontrast sağlar;
  sağlamıyorsa uygulama ayağa kalkmaz. *(Rozet: koyu zemin üzerine ters metin.)*

### E. Kontrast kapısı — varsayılan palet (CI'da)

- **AC-24.** Varsayılan palet, yapılandırılan temayla **aynı eşik tablosunu** okuyan ve CI'da
  koşan bir testle doğrulanır; eşiklerden biri ihlal edilirse test kırmızıya döner.

### F. Onay anında var olan ekranlar

- **AC-25.** `[görsel]` Onay anında **sayılan her ekran** yeni token değerleriyle açılır ve
  okunur durumdadır; sayımın kendisi DoD'de listelenir (R10).
  **Onay anındaki sayım (2026-10-04) — beş ekran:** vitrin listesi
  (`ShowcaseListPage`), ilan detayı (`ListingDetailPage`), panel girişi (`LoginPage`),
  personel listesi (`StaffListPage`), zorunlu parola değişimi
  (`ForcePasswordChangePage`). *(Sayı onay anında çivilendi: `0010` paralel giderse ekran
  sayısı artar ve AC-25 hareketli bir hedefe dönerdi.)*
- **AC-26.** `[görsel]` Boş durum ve hata durumu ekranları yeni palette okunur durumdadır.
- **AC-27.** Hiçbir ekranda yalnızca renkle anlatılan bir durum kalmaz; seçili, hatalı ve
  rezerve hâlleri metin veya biçimle de anlatılır.

### G. Erişilebilirlik ve dil

- **AC-28.** `[görsel]` Klavyeyle gezinirken odak halkası, hem zemin hem **limon dolgu**
  üzerinde görünür.
- **AC-29.** `[görsel]` Arapça locale'de metin Arapça tipografi yığınıyla basılır; Latin
  yığınına düşmez.
- **AC-30.** `[görsel]` Arapça locale'de düzen `dir="rtl"` ile çalışmaya devam eder ve tema
  değişimi yönü bozmaz.

---

## Definition of Done

- [x] **Context § Token tablosu tamamlandı** — "tasarımcıdan bekleniyor" satırı kalmadı
      (R2, AC-6). *(**Kapandı 2026-10-05**, PR #32 — bkz. § Verilen kararlar **D4 Ek**.
      Bu kutu bu PR'ın ilk hâlinde AÇIK yazılmıştı ve o doğruydu: altı satır eski MAVİ
      ailenin değerleriyle duruyordu. Değerler sonradan tasarımcıdan gelmedi — mockup'ın
      on bir artboard'ının satır içi stillerinden okundu (dördü) ve tasarımcının kendi
      yüzey/örtü tarifiyle türetildi (ikisi); 14/14 kontrast çifti ölçüldü ve geçti.
      `VarsayilanPaletTests` artık tablonun on dört satırının tamamını karşılaştırıyor,
      yani **AC-6'nın ikinci yarısı da** karşılandı.)*
- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] **A grubundaki kriterin bu spec'ten önce de yeşil olduğu** PR'da yazıldı; yeni
      davranışın kanıtı B–G gruplarıdır.
- [x] `[görsel]` işaretli her kriterin kanıtı eklendi (`docs/evidence/frontend-002-ac<n>.png`),
      boş ve hata durumları dâhil, Arapça görüntüler de var.
- [x] İki farklı tema yapılandırmasıyla alınmış **aynı ekranın iki görüntüsü** eklendi
      (**AC-15**).
- [x] Kontrast kapısının ayağa kalkmayı engellediği testle gösterildi
      (**AC-17…AC-23, AC-31…AC-34**); varsayılan paletin CI testi de yeşil (**AC-24**).
- [x] **Çift tablosunun her satırı kapıda denetleniyor** ve kapı tabloda olmayan bir eşik
      uygulamıyor (**AC-21**, C12).
- [x] Lint kapısı ham hex/px ihlalinde kırıyor ve token dosyasının muafiyeti duruyor (AC-3).
- [x] **Çağrı yeri göçü tabloyla gösterildi:** *(doğrulandı: `frontend/src` altında
      `var(--color-accent)` kullanımı KALMADI; merge commit'inin CSS diff'i sekiz göçü ve
      AC-7 için eklenen iki fiyat atamasını gösteriyor.)* **sekiz** çağrı yerinin her birinin hangi
      yeni token'a gittiği PR'da yazılı (Context § Çağrı yeri sayımı). Sekizincisi
      `ui.css:17` — dolgu üzerindeki metin; `--color-accent` dizesini aramakla bulunmaz.
- [x] `frontend-001`'in SCORECARD dersi gözetildi: token'lar **anlamına göre** bağlandı,
      mekanik değiştirme yapılmadı; `font-family` bağlantısı doğrulandı.
- [x] Hiçbir ekranın düzeni değişmedi (C9) — düzen farkı varsa PR'da **açıkça** yazıldı.
      *(Doğrulandı, iddia edilmedi: merge commit'inin CSS diff'i `display`, `position`,
      `width`, `height`, `margin`, `padding`, `gap`, `grid`, `flex`, `inset`, `z-index`,
      `overflow` özelliklerinden HİÇBİRİNE dokunmuyor — yalnız renk, tipografi importu ve
      yorum satırları değişti.)*
- [x] Onay anında var olan ekranlar **sayıldı** ve her biri için kanıt eklendi (R10, AC-25);
      sayım PR'da listelendi.
- [x] Yazı tipi isteklerinin üçüncü taraf alan adına gitmediği doğrulandı (AC-8, D8).
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — modül sınırı ihlali yok.
- [x] Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] **Dış gözden geçirme yapıldı:** PR açılmadan önce bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`).
      **YAPILMADI.** Madde bu spec'in DoD'unda yoktu ve şablonla hizalanırken sonradan
      eklendi; PR o sırada çoktan merge edilmişti. `docs/roles/qa.md` § 8 gereği kendi
      kodumu gözden geçirip işaretleyemem. Merge SONRASI bir süreç gözden geçirmesi yapıldı
      ve iki gerçek bulgu verdi (SCORECARD'da) — ama o, DoD'un istediği diff + spec + test
      okuması değil.
- [x] Çeviri anahtarı eklendiyse TR/AR/EN üçünde de var; eksik anahtar kontrolü yeşil.
- [x] Yeni domain terimi yok.
- [x] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (D1, D3, D9).
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [x] Takım Yöneticisi onayladı ve squash-merge edildi.
- [x] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **3** | (1) **Onay anında:** D10–D13 eklendi, AC-21 yeniden yazıldı (AC-17/18 ile çelişiyordu), AC-31…AC-34 eklendi ve çağrı yeri tablosuna sekizinci satır girdi — kriter sayısı 30 → 34. (2) **Onaydan SONRA:** AC-7 daraltıldı ve X12 eklendi. (3) **D4 onaylanırken (2026-10-05):** `X11`'in ölçümü mockup'tan okunan kenarlık değeriyle güncellendi (1.36:1 → 1.27:1); kararın kendisi değişmedi, payı büyüdü. Üçüncüsü bir Constraint'e eklenen açıklama notudur ve `specs/TEMPLATE.md` bunu **sayıyor** — 0005'te aynı karar verilmişti, ölçüt kendi tanımına göre uygulanıyor. *(D4'ün onayı tek başına revizyon değil: Token tablosu ve "Verilen kararlar" Context'tedir, DoD de sayılmaz.)* İkincisi şablonun harfiyen saydığı türden; birincisi 0009'daki okumayla aynı — ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve dört yeni kriterin üçü yapılandırılabilir bir token'ı denetimsiz bırakan gerçek bir delikten geldi. |
| **Düzeltme turu sayısı** | **0** | PR #30 gözden geçirmeden geri DÖNMEDİ; dört kapı ilk koşuda yeşildi ve merge edildi. Merge SONRASI bir süreç gözden geçirmesi yapıldı (aşağıda) ama o bir tur değil, kapanış denetimi. |
| **Dış bulgu (gerçek/gürültü)** | **2 / —** | Merge sonrası süreç gözden geçirmesinden iki GERÇEK bulgu: (1) bu spec'in DoD'u `specs/TEMPLATE.md` ile hizasızdı — dört madde eksikti (backend testleri, mimari testler, migration şeması, dış gözden geçirme); (2) üç doğrulama kutusu ölçülmeden işaretlenecekti (token tablosu, çağrı yeri göçü, düzen değişmemesi). İkisi de gerçekti: ölçünce token tablosu kutusunun KAPANMAMASI gerektiği çıktı. *(Kutu bir
tur sonra, D4'ün altı değeri mockup artboard'larından okunup türetilince kapandı — PR #32.
Bulgu yine de gerçekti: kutu ÖLÇÜLMEDEN işaretlenecekti ve o anda kapanamazdı.)* Gürültü sayısı yazılamaz — gözden geçirme bana beş maddelik bir iş listesi olarak geldi ve üçü bulgu değil adımdı. |
| **Uygulama içi bulgu** | **11** | **Planlarken (3):** (1) göç tablosu sekizinci çağrı yerini kaçırıyordu — `.ui-button--primary` mavi dolgu + BEYAZ metindi ve limon dolguyla 1.26:1 verirdi; (2) AC-21, AC-17/18 ile çelişiyordu; (3) çift tablosu üç çifti atlıyordu ve biri — `--color-ink-muted` — hem on yerde yardımcı metin hem YAPILANDIRILABİLİRDİ, yani kapısız. **Uygularken (3):** (4) Arapça sayfa Latin yığınına düşüyordu (tuzak 3, kendi düzeltmemin içinde) — e2e yakaladı; (5) varsayılan palet ayrıştırıcım `:root` dışını da okuyup Arapça override'ı Latin yığınının üstüne yazıyordu — testin kendisi yakaladı; (6) AC-7'nin adını testten geri almak zorunda kaldım, kriterin dörtte birini ölçüyordu. **Test yazarken (3):** (7) AC-8'in host karşılaştırması `page.url()` kullanıyordu ve belgenin kendisini yabancı sayıyordu; (8) iki assert `if (count > 0)` korumasının arkasındaydı ve hiç koşmuyordu; (9) AC-25 iki ekran ölçüp "her ekran" diyordu. **Sonda yazarken (2):** (10) `--no-build` bayat assembly koşturdu; (11) kabuk tırnaklaması yüzünden `str.replace` hiç eşleşmedi ve sonda hiçbir şeyi değiştirmeden "geçti" dedi. |
| **Regresyon sayısı** | **1** | Arapça tipografi. Değişiklikten ÖNCE Arapça sayfa `system-ui` ile doğru basılıyordu; Latin yığınına Space Grotesk eklenince — ve `body` kendi `font-family`'sini bildirdiği için `html[lang='ar']` kuralı ona ulaşmadığından — Arapça metin kapsamayan bir fonta düşüyordu. Merge'den önce yakalandı ve token'ı yeniden tanımlayarak düzeltildi. Üretime çıkmadı. |
| **Kaçan hata** | | *2026-10-05 itibarıyla* — **izleniyor**. Merge sonrası bulunan her kusur buraya tarihiyle eklenir (`specs/TEMPLATE.md` § SCORECARD). |

**Bu spec'ten çıkarılan ders:**
**Bir kapı, denetlediği şeyin TAMAMINI görmüyorsa güvence değil, güvence hissidir.**
Bu spec'in merkezinde çalışan bir kontrast kapısı var: on dört çift, iki ayrı kapıdan,
tek eşik tablosundan. Yine de üç ayrı yerden sızdırabiliyordu ve üçünü de kapı DEĞİL,
kapının dışına bakmak buldu:

1. **Kapı token ÇİFTİNİ denetler, ÇAĞRI YERİNİ denetlemez.** `.ui-button--primary` beyaz
   metni limon dolgunun üstüne yazsa bütün eşikler yeşil kalırdı — çünkü o çift tabloda
   yok. Çözüm kapıyı büyütmek değil, ikinci bir kural oldu: dolgu vurgusu yalnız dolgu
   olarak kullanılabilir (`check:tokens`).
2. **Kapı yalnız TABLODAKİ çiftleri denetler.** Tablo CSS'ten değil spec'ten türetilmişti
   ve üç çifti atlıyordu; biri yapılandırılabilir bir token'dı, yani bir kurulum onu
   okunamaz yapabilir ve uygulama açılırdı. Çözüm: çiftleri gerçekten kullanılan
   kombinasyonlardan türetmek.
3. **Kapı rengi denetler, TİPOGRAFİYİ denetlemez.** Arapça sayfa Latin yığınına düştü ve
   bütün kontrast eşikleri yeşil kaldı — okunabilirlik rengin değil fontun sorunuydu.

Genelleştirmesi: bir kapı kurarken "neyi denetliyor" kadar **"neyi denetlemiyor"** da
yazılmalı. Üçünün de ortak kör noktası aynıydı — kapı **değerleri** görüyor, **kullanımı**
görmüyor.
