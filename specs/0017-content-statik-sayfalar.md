# 0017 — Content: Statik Sayfalar ve SSS

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-18 — Takım Yöneticisi (ajan)) — 134 kabul kriteri |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (yeni — bugün hiç yok) · `frontend/` public vitrin + panel · `Host` (eski slug yönlendirmesi — **D13**) · `frontend/` talep formu (KVKK bağlantısı — **D5**) ve altlık (Kurumsal kolonu — **D6**, **D7**) |
| **Bağımlı spec'ler** | `0006` (roller: İçerik Editörü — tamamlandı) · `0014` (ana sayfa — tamamlandı; altlığına kolon eklenir, **D28**'i değişir — **D9**) · `0005` (talep formu — tamamlandı; izin satırına bağlantı eklenir — **D5**) · `0002` ("bulunamadı" ve hata ekranı deseni — **D10**, **D23**) · `0007` (iyimser eşzamanlılık deseni — **D18**) · `0004` (SEO — gerçek HTTP 404 kararı oraya devredildi, **D10**) |
| **Dayanak** | ADR 0003 Ek 1, sıra #11 |
| **Karar bekliyor** | yok — S1–S5 → **D1–D4**; QA CLARIFY turunun yirmi bulgusu (B1–B20) → **D5–D24**; plan turu → **D25–D28**; kriter turu S6–S13 → **D29–D36** (2026-11-18). |

> **QA CLARIFY turu (2026-11-18, `main` @ `81e056c`).** QA spec'i bugünkü koda karşı okudu
> ve yirmi bulgu getirdi (B1–B20); önerilerin **tamamı** Takım Yöneticisi (depo sahibi)
> tarafından onaylandı ve **D5–D24** olarak işlendi.
>
> İki olgu düzeltildi:
> - **`0005`'in yayınladığı bir KVKK bağlantısı yok** (K1) — talep formunda hiç bağlantı
>   yok ve izin yalnız bir `bool` olarak kaydediliyor.
> - **`0014` bu spec'i beklemiyor** (K2) — tamamlandı ve iki bağımlılığını kendi içinde
>   çözdü.
>
> Kabul kriterleri kriter turunda (2026-11-18, **Analist**, **D28**) yazıldı; o turun
> soruları S6–S13 → **D29–D36**.

---

## Intent

Vitrinde **galerinin kendisi hakkında tek bir sayfa yok.** "Hakkımızda", "KVKK aydınlatma
metni", "Çerez politikası", "Sıkça sorulan sorular" — hiçbiri mevcut değil. Bunun somut
sonucu bugün ödeniyor:

- **Hukuki metin eksik.** `0005` ziyaretçiden ad, telefon ve e-posta topluyor ve iletişim
  izni kaydediyor (**L7**). Ama izin satırı hiçbir aydınlatma metnine bağlanmıyor; **o metnin
  gideceği bir sayfa yok** (K1).
- **Altlığın kurumsal bağlantıları yok.** `0014` altlığa yalnız var olan hedefleri koydu
  (`0014` R7); kurumsal sayfa olmadığı için o kolon hiç basılmadı.

**Galeri sahibi** kendi hikâyesini ve sıkça sorulan soruları kendi yazmak istiyor — her
değişiklik için bize gelmek zorunda kalmadan. `0006` zaten bunun için bir rol tanımlamış:
**İçerik Editörü.** O rol bugün hiçbir şey düzenleyemiyor.

Bu iş bittiğinde galeri, vitrindeki metin sayfalarını panelden yazıp dil başına yayınlar.
Başarı ölçütü: **bir hukuki metnin veya SSS girdisinin değişmesi bir dağıtım gerektirmiyor.**

---

## Requirements

### Sayfalar

- **R1.** İçerik Editörü veya Yönetici, bir **içerik sayfası** oluşturabilir, düzenleyebilir
  ve dil başına yayınlayabilir.
- **R2.** İki tür sayfa vardır (**D6**):
  - **Sistem sayfaları** kodda sabit dört kimlikle gelir: `about` (Hakkımızda), `privacy`
    (KVKK aydınlatma metni), `cookies` (Çerez politikası), `faq` (SSS).
  - **Serbest sayfalar** galerinin açtığı sayfalardır (**D4**).
- **R3.** Sistem sayfaları kurulumda **metinsiz taslak** olarak vardır. Oluşturulmaz ve
  **silinemez**; düzenlenir ve dil başına yayınlanır ya da yayından kaldırılır (**D6**,
  **D17**).
- **R4.** Bir sayfa **locale başına** metin taşır: başlık, gövde ve **elle girilen Latin
  slug**. → `docs/architecture.md` § 3.
- **R5.** Yayın durumu **locale başınadır**: `taslak` veya `yayında`. Bir sayfa Türkçe
  yayında olup Arapça taslakta kalabilir.
- **R6.** Bir dilde yayına almanın şartı, o dilde **başlık, gövde ve slug'ın üçünün de dolu**
  olmasıdır (**D16**). Eksikse istek reddedilir ve eksik alanlar alan bazında gösterilir.
- **R7.** Yayındaki bir dilde **kaydetmek değişikliği hemen yayına alır**; ayrı bir
  "yayındaki sürümün taslağı" yoktur (**D15**).
- **R8.** **Yayından kaldırma** o dili `taslak`'a döndürür ve metni korur. **Silme** ayrı ve
  açık bir eylemdir (**D17**):
  - Onay penceresi ister.
  - **Kalıcıdır**; çöp kutusu yoktur.
  - Yalnız serbest sayfalara ve SSS girdilerine uygulanır.
  - Silinen sayfanın eski slug'ları da silinir (**D13**).
- **R9.** Panel, her sayfa ve her SSS girdisi için **hangi etkin dillerde** metnin eksik
  olduğunu gösterir. Etkin olmayan locale **sayılmaz** (**D16**). → `0010b` R13 ile aynı
  desen.
- **R10.** Aynı sayfayı eski bir sürüm üzerinden kaydetmek **reddedilir** (**D18**):
  - Uç `409` döner.
  - Panel *"sayfa başkası tarafından değiştirildi"* der ve güncel hâli yeniden yükler.

  SSS girdileri için de aynı kural geçerlidir.

### Adres ve slug

- **R11.** Ziyaretçi, bir sayfanın o dilde yayındaki hâlini `/{locale}/{slug}` adresinde
  görür.
- **R12.** Tek parçalı bir adres (`/{locale}/{x}`) şu durumlarda **"bulunamadı" ekranını**
  gösterir (**D9**, **D10**):
  - `x` o dilde yayındaki bir sayfanın slug'ı değilse;
  - sayfa o dilde taslaksa;
  - sayfa silinmişse.

  Ekran `0002`'ninkiyle aynı desendedir; içerik ucu `404` döner. `0014` **D28**'in
  "tanınmayan alt yol listeye düşer" davranışı tek parçalı yollar için **kalkar**.
- **R13.** Slug kuralları (**D11**, **D12**):
  - Biçim `[a-z0-9]+(-[a-z0-9]+)*`, uzunluk 2–80 karakter.
  - Her dilde **zorunludur** ve **elle girilir**; başlıktan otomatik üretilmez.
  - Tekillik **locale başınadır**; aynı slug farklı dillerde kullanılabilir.
  - **Ayrılmış** bir slug alınamaz (C11).
- **R14.** Yayındaki bir dilde slug değişirse eski slug o dil için **kaydedilir** (**D13**):
  - Eski adres yeni adrese **301** ile yönlenir.
  - Kaydedilmiş eski slug o dilde başka bir sayfaya verilemez.
  - Sayfa silinince eski slug'lar da silinir ve eski adres R12'ye düşer.
- **R15.** Sayfa yanıtı, sayfanın **yayında olduğu diğer dillerdeki slug'larını** da taşır
  (**D14**). Dil değiştirici içerik sayfasındayken:
  - hedef dilin slug'ına gider;
  - sayfa o dilde yayında değilse o dilin **ana sayfasına** gider.

### Gövde

- **R16.** Gövde, **CommonMark'ın bir alt kümesi** olarak saklanır ve basılır (**D19**):
  - Desteklenen öğeler: `##` ve `###` başlık, paragraf, sıralı ve sırasız liste, bağlantı,
    kalın, italik.
  - `#` başlık **yoktur**; sayfanın `h1`'i başlık alanıdır.
  - Desteklenmeyen sözdizimi düz metin olarak basılır.
- **R17.** **Ham HTML işlenmez**; metin olarak basılır (**D19**):
  - Bağlantılarda yalnız `http`, `https`, `mailto`, `tel` ve göreli adres kabul edilir;
    başka bir şemadaki bağlantı (ör. `javascript:`) bağlantı olarak basılmaz.
  - Dış bağlantılar `rel="noopener noreferrer"` taşır.
  - Arındırma, ziyaretçi tarafında **basarken** uygulanır.

### SSS

- **R18.** Bir **SSS girdisi** tek bir birimdir (**D20**):
  - Soru ve cevap metni **dil başınadır**; yayın durumu da dil başınadır.
  - Girdilerin sırası **tek ve globaldir**.
  - Panelde sıra **yukarı/aşağı düğmeleriyle** değişir.
- **R19.** SSS, `faq` sistem sayfasının gövdesinin **altında**, o dilde yayındaki girdilerle
  sırasıyla gösterilir (**D21**).
- **R20.** `faq` sayfası o dilde yayında ama o dilde **hiç yayındaki girdi yoksa** sayfa
  yayında değilmiş gibi davranır (**D21**):
  - altlıktan düşer;
  - adresi R12'nin "bulunamadı" ekranını gösterir.

### Vitrinde görünme

- **R21.** Altlığa bir **"Kurumsal"** kolonu eklenir (**D6**, **D7**):
  - Kolon `about`, `faq`, `privacy`, `cookies` sistem sayfalarını bu sırayla gösterir.
  - Yalnız o dilde yayında olanlar görünür; R20'nin boş SSS'i görünmez.
  - Hiçbiri yayında değilse kolon basılmaz.
  - Serbest sayfalar altlıkta **görünmez**; yalnız doğrudan bağlantıyla erişilir (**D4**).
- **R22.** Talep formunun izin satırı, `privacy` sayfası **o dilde yayındaysa** ona bağlantı
  verir; yayında değilse bağlantı **basılmaz** (**D5**). Boş ya da kırık bağlantı
  gösterilmez.

### Durumlar, erişim, locale

- **R23.** Ziyaretçi tarafı (**D23**):
  - **Yükleniyor:** `0002`'nin iskelet deseni.
  - **Bulunamadı:** R12.
  - **Hata:** `0002`'nin deseninde, yeniden deneme düğmeli ekran.

  Panel ekranları yükleniyor, boş (henüz serbest sayfa veya SSS girdisi yok), hata,
  **doğrulama hatası** ve **çakışma** (R10) durumlarını ele alır. → `docs/frontend.md` § 3.
- **R24.** Panelin **"İçerik"** bölümü yalnız **İçerik Editörü** ve **Yöneticiye** görünür
  (**D24**):
  - Satış Danışmanı bölümü görmez; yazma ucuna doğrudan istek atarsa `403` alır.
  - Bu spec, İçerik Editörünün diğer panel ekranlarındaki görünürlüğüne **dokunmaz**.
- **R25.** Üç locale'de çalışır; Arapça'da RTL.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Content`, içerik verisinin tek yazma sahibidir. → `docs/architecture.md` § 1.
- **C2.** **Merkezi bir `translations` tablosu kurulmaz** ve çeviriler tek modülde
  toplanmaz; içerik metni bu modülün **kendi** verisidir. → Yasak Listesi 21.
- **C3.** Veritabanında çevrilmiş durum adı saklanmaz; kod saklanır (`draft`, `published`,
  sistem sayfası kimlikleri). → Yasak Listesi 22.
- **C4.** Yazma uçları **oturum gerektirir** ve İçerik Editörü ile Yöneticiye açıktır; okuma
  public'tir ve **yalnız yayındaki dili** döner. → `0006` R7, R8.
- **C5.** Ziyaretçiye gösterilen içerik gövdesi, bu modülün **sahibi olduğu içeriktir** —
  Yasak Listesi 23'ün istisnası budur. **Hata metinleri** yine `errorCode` ile gelir ve
  alan bazındadır.
- **C6.** Locale öneki opsiyonel değildir; `Accept-Language`'a göre yönlendirme yapılmaz.
  → Yasak Listesi 28–29.
- **C7.** Slug kanonik Latin'dir ve locale başına düzenlenebilir (R13). → `docs/architecture.md`
  § 3.
- **C8.** CSS'te fiziksel yön özellikleri kullanılmaz; renk ve tipografi `frontend-002`
  token katmanından gelir.
- **C9.** Yeni migration yalnızca `content` şemasına dokunur. → Yasak Listesi 3.
- **C10.** Her kabul kriteri en az bir testle karşılanır.
- **C11.** **Ayrılmış slug listesi kodda yaşar** (**D11**): `cars`, `admin`, `api`,
  `assets`. Vitrine ileride eklenen her tek parçalı bölüm bu listeye eklenir.
- **C12.** Sınır değerler (**D22**):

  | Alan | Sınır |
  |---|---|
  | Başlık | 1–120 karakter |
  | Gövde | en fazla 50.000 karakter |
  | Slug | 2–80 karakter, R13'ün biçimi |
  | SSS sorusu | 1–200 karakter |
  | SSS cevabı | 1–2.000 karakter |

  Sınır aşılırsa alan bazında hata kodu döner. SSS cevabı R16–R17'nin biçimini taşır.
- **C13.** Sistem sayfalarının **kimlikleri** (`about`, `privacy`, `cookies`, `faq`) ve
  altlıktaki **sırası** kodda sabittir (**D6**, **D7**). Kimlik slug'dan bağımsızdır: talep
  formu ve altlık sayfayı kimlikle bulur, slug'la bulmaz.
- **C14.** Eşzamanlı yazma `0007`'nin iyimser eşzamanlılık deseniyle korunur (**D18**).

### KAPSAM DIŞI

- **X1.** **Blog** — *neden:* `docs/architecture.md` § 1 blogu `Content`'in sorumluluğunda
  sayıyor ama blog kendi listeleme, etiketleme, tarih arşivi ve SEO kararlarını getirir
  (**D1**).
- **X2.** **Kampanya alanları ve öne çıkan araç slotu** — *neden:* kampanya kendi domain
  kurallarını taşıyor (**K1–K5**) ve bu bir metin sayfasından farklı bir yaşam döngüsüdür
  (**D2**).
- **X3.** **Zengin metin editörü** (WYSIWYG, görsel yükleme, tablo, gömülü video) —
  *neden:* `Media` bağımlılığı ve güvenlik yüzeyi getirir; gövde R16'nın alt kümesidir
  (**D3**, **D19**).
- **X4.** **Sürüm geçmişi ve geri alma** — *neden:* `Audit` modülü yok; `docs/architecture.md`
  § 6 alan ayarları için aynı kararı vermişti (*"`Audit` kaydı yeterli"*).
- **X5.** **Çeviri onay iş akışı** — *neden:* § 6'da bilinçli kapsam dışı; R5 zaten locale
  başına yayın durumu.
- **X6.** **Makine çevirisi** — *neden:* § 6'da V1 dışı; `0010a` D5 ile aynı çizgi.
- **X7.** **Menü ve altlık yapısının panelden düzenlenmesi** — *neden:* altlığın Kurumsal
  kolonu kodda sabit dört sistem sayfasıdır (C13); serbest sayfayı altlığa koymak ayrı bir
  yapı kararıdır (**D4**).
- **X8.** **İzin anında hangi aydınlatma metni sürümünün gösterildiğinin kaydı** — *neden:*
  bugün izin yalnız `ConsentGiven` `bool`'u (K1); metnin sürümüyle bağlamak bir `Leads`
  kararıdır, `Content` kararı değil. **Ayrı kart olarak önerildi** (**D5**).
- **X9.** **Ana sayfa giriş metninin `Content`'e taşınması** — *neden:* giriş metni bir sayfa
  değil bir **alan**dır; X2'nin kampanya alanlarıyla birlikte ele alınır. Bugün çeviri
  kataloğundan geliyor (`0014`'ün giriş metni kararı) (**D7**).
- **X10.** **"İletişim" sayfası** — *neden:* şubenin telefon ve adresi `Dealership`'in verisi
  ve `0014`'ün altlığında zaten basılıyor; serbest metinde tekrar edilmesi, telefon
  değiştiğinde bayat bir ikinci kaynak demek (**D8**).
- **X11.** **Gerçek HTTP 404 durum kodu** (belgenin kendisi için) — *neden:* host bugün her
  vitrin rotasına `200` dönüyor ve `0002`'nin "bulunamadı"sı yalnız ekran (K4); tek bir
  sayfa türü için ayrı kural koymak iki dialekt doğurur. Tüm vitrin için tek karar olarak
  `0004`'e devredildi (**D10**).
- **X12.** **Panelde gövde önizlemesi** ve **sürükle-bırak sıralama** — *neden:* R16'nın alt
  kümesi küçük; yukarı/aşağı düğmeleri klavyeyle erişilebilir ve test edilebilir (**D19**,
  **D20**).
- **X13.** **Çöp kutusu / geri getirilebilir silme** — *neden:* `Audit` yok; silme kalıcıdır
  ve onay penceresi ister (**D17**).

---

## Context

### Mevcut davranış — yeniden doğrulandı (2026-11-18, `main` @ `81e056c`)

- **`Content` modülü yoktur.** `src/Modules/` altında `Dealership`, `Identity`, `Leads`,
  `Media`, `Notifications`, `VehicleCatalog`, `VehicleReference` var.
- **İçerik Editörü rolü kodda var**, ama düzenleyeceği içerik yok:
  - `StaffRole.ContentEditor = 3`;
  - rol kodu `content_editor` (`StaffRoleCode.cs:18`).
- **`docs/architecture.md` § 3 davranışı yazmış:** *"CMS içeriği | `Content` | Locale başına
  satır + **locale başına yayın durumu** | Yayınlanmamışsa o dilde menüden düşer"*.
- **Slug kararı verilmiş:** *"CMS sayfalarında locale başına düzenlenebilir Latin slug"*
  (§ 3). Araçta her dilde **aynı** slug, CMS'te locale başına.

### QA CLARIFY turu ölçümü (2026-11-18)

- **K1 — KVKK bağlantısı yok; izin metne bağlı değil.**
  - `frontend/src/features/leads/LeadFields.tsx`'te hiç bağlantı yok.
  - İzin metni çeviri kataloğunda tek cümle (`lead.consent.text`, `tr.json:143`).
  - `Lead.ConsentGiven` yalnız `bool` (`Lead.cs:79`).
  - Taslağın *"`0005` bir KVKK bağlantısı yayınladı"* cümlesi yanlıştı → **D5**.
- **K2 — `0014` tamamlandı.**
  - Giriş metni çeviri kataloğundan geliyor.
  - Altlık yalnız var olan hedeflere bağlantı veriyor (`0014` R7).
  - Taslağın *"`0014` bu spec'i bekler"* cümlesi bugün geçerli değil → **D7**.
- **K3 — tanınmayan alt yol listeye düşüyor.**
  - `frontend/src/shared/lib/route.ts:111` → `0014` D28: *"değiştirmek için kriter yok"*.
  - `/{locale}/{slug}` aynı ad alanını kullanıyor → **D9**.
- **K4 — vitrin "bulunamadı"sı yalnız ekran.** `IndexHtmlRenderer` her vitrin rotasına
  `200` dönüyor; `0002` R5'in "bulunamadı"sı istemci ekranı → **D10**.
- **K5 — dil değiştirici yalnız öneki değiştiriyor.** `LocaleSwitcher.tsx:37` adresin geri
  kalanını aynen taşıyor; locale başına slug'da hedef dilde başka bir adres çıkar → **D14**.
- **K6 — şube iletişimi `Dealership`'te.** Telefon ve adres `0003`'ün verisi ve `0014`'ün
  altlığında basılıyor → **D8**.

### Geçerli kurallar

- `docs/architecture.md` § 1 (`Content`'in sorumluluğu), § 3 (CMS satırı, slug), § 6
  (sürüm geçmişi, onay akışı, makine çevirisi kapsam dışı).
- **L7** (iletişim izni) ve **K1–K5** (kampanya — bu spec'in dışında).
- `0006` R7, R8 (rol kısıtı).
- `docs/frontend.md` § 3 (durumlar) ve § 7 (erişilebilirlik).

### Bilinen tuzaklar

1. **Yayınlanmamış dili boş sayfayla göstermek.** Altlıkta boş bağlantı, boş SSS sayfası ve
   başlıksız gövde aynı ihlalin üç biçimidir (R6, R20, R21, R22).
2. **Gövdeyi ham HTML olarak kabul etmek.** Panelden gelen HTML, ziyaretçi tarayıcısında
   çalışacak bir enjeksiyon yüzeyidir (R17).
3. **Slug çakışması.** Aynı locale'de aynı slug, eski slug kaydıyla çakışan yeni slug veya
   ayrılmış bir slug: üçü de bir sayfayı ya da bölümü erişilemez yapar (R13, R14, C11).
4. **Slug değişince eski adresi kırmak.** *(D13 ile çözüldü.)* Eski slug kaydedilir ve 301
   ile yönlenir.
5. **Hukuki metni "sonra ekleriz" diye bırakmak.** `0005` kişisel veri topluyor; KVKK
   metninin olmaması bir eksiklik değil, bir **uyum sorunudur** (R22, **D5**).
6. **Sayfayı slug'la bulmak.** Talep formu ve altlık KVKK sayfasını slug'la arasaydı, galeri
   slug'ı değiştirdiğinde bağlantı sessizce kopardı. Kimlik sabittir, slug değil (C13).
7. **Dil değiştiricinin öneki değiştirip slug'ı bırakması.** `/tr/hakkimizda` →
   `/ar/hakkimizda` bulunamadı ekranıdır (R15, K5).

### Verilen kararlar — 2026-11-18

Hepsi Takım Yöneticisi (depo sahibi) onayıyla verildi.

**D1 — Blog V1'de yok (S1).** İlk sürüm statik sayfalar ve SSS → X1.
- *Bedel:* içerik pazarlaması bir spec daha bekler.

**D2 — Kampanya ve öne çıkan araç slotu ayrı spec (S2).** → X2.
- *Gerekçe:* **K1–K5** bir metin sayfasının yaşam döngüsü değil; aynı spec'te kabul
  kriterlerini iki hakeme bölerdi.

**D3 — Gövde sınırlı işaretleme; ham HTML yok (S3).** Ayrıntısı **D19**.

**D4 — Galeri serbest sayfa açabilir, altlıkta göremez (S4).** (R2, R21, X7)
- *Bedel:* serbest sayfa yalnız doğrudan bağlantıyla erişilir.

**D5 — KVKK ve Çerez metinleri `Content`'te; talep formu KVKK sayfasına bağlanır (S5, B1).**
(R22, X8)
- `privacy` sayfası o dilde yayındaysa izin satırı ona bağlantı verir; değilse bağlantı
  basılmaz.
- İzin anındaki metin sürümünün kaydı → X8, ayrı kart.
- Taslağın DoD'undaki *"bağlantının hedefi kontrol edildi"* kutusu kaldırıldı: ölçüldü,
  bağlantı yok (K1).
- *Gerekçe:* metin mevzuata göre değişir; kodda sabit olsa her değişiklik bir dağıtım olurdu.

**D6 — Dört sistem sayfası (B2).** `about`, `privacy`, `cookies`, `faq` kodda sabit; kurulumda
metinsiz taslak; silinemez, yayından kaldırılabilir (R2, R3, C13).
- *Gerekçe:* talep formu ve altlık sayfayı slug'dan bağımsız bir kimlikle bulmalı; slug locale
  başına ve düzenlenebilir.

**D7 — Altlığa Kurumsal kolonu; giriş metni yerinde kalır (B3).** (R21, X9)
- *Gerekçe:* giriş metni bir sayfa değil bir alan; kampanya alanlarıyla birlikte ele alınmalı.

**D8 — "İletişim" sayfası yok (B4).** (X10)
- *Gerekçe:* şube iletişimi `Dealership`'te (K6); serbest metin ikinci ve bayatlayan bir kaynak
  olurdu.

**D9 — Tek parçalı tanınmayan yol "bulunamadı" ekranını gösterir; `0014` D28 değişir (B5).**
(R12)
- *Gerekçe:* `/{locale}/{slug}` ile "tanınmayan yol listeye düşer" kuralı birlikte doğru
  olamaz. D28'e bağlı bir kabul kriteri yok (K3).

**D10 — "Bulunamadı" ekrandır; gerçek HTTP 404 `0004`'e (B6).** (R12, X11) Ekran `0002` ile
aynı desende, içerik ucu `404` döner.
- *Gerekçe:* detay sayfası 200, içerik sayfası 404 dönseydi vitrinde iki farklı kural olurdu
  (K4).

**D11 — Ayrılmış slug listesi (B7).** `cars`, `admin`, `api`, `assets`; kodda (C11).
- *Gerekçe:* `cars` slug'ını alan bir sayfa araç listesini erişilemez yapardı.

**D12 — Slug biçimi, uzunluğu, elle girişi (B8).** (R13)
- *Gerekçe:* Arapçadan güvenilir bir Latin çeviri yok; C7 Latin slug istiyor.

**D13 — Eski slug kaydı ve 301 (B9).** (R14) Yönlendirme host'ta, `LocalePrefixMiddleware`'in
zaten 301 ürettiği yerde.
- *Gerekçe:* KVKK bağlantısı e-postalarda ve dış sitelerde paylaşılmış olabilir.

**D14 — Sayfa yanıtı diğer dillerin slug'larını taşır; dil değiştirici onu kullanır (B10).**
(R15)
- *Gerekçe:* K5; `0004`'ün `hreflang`'i de aynı veriyi ister.

**D15 — Yayındaki dilde kaydetmek hemen yayına alır (B11).** (R7)
- *Gerekçe:* X4 sürüm geçmişini dışarıda bırakıyor; 2–3 kişilik ekip için yeterli.
- *Bedel:* yazım hatası doğrudan canlıya çıkar.

**D16 — Yayına alma şartı; eksik dil yalnız etkin locale'de (B12).** (R6, R9)
- *Gerekçe:* iki dilli kurulumda üçüncü dil için sürekli "eksik" uyarısı gürültüdür.

**D17 — Silme kalıcı ve onaylı; sistem sayfası silinemez (B13).** (R3, R8, X13)
- *Gerekçe:* `Audit` yok; kalıcı silme dışındaki her biçim bir çöp kutusu işi olurdu.

**D18 — İyimser eşzamanlılık, `409` (B14).** (R10, C14)
- *Gerekçe:* `0007`'nin deseni; sessiz veri kaybı yerine görünür bir hata.

**D19 — CommonMark alt kümesi; ham HTML metin olarak; bağlantı şeması listesi (B16).**
(R16, R17, X12)
- *Gerekçe:* DoD'un "enjeksiyon girişimi kırıldı" kutusu ancak somut bir listeyle test
  edilebilir.

**D20 — SSS girdisi tek birim; global sıra; yukarı/aşağı düğmeleri (B17).** (R18, X12)
- *Gerekçe:* sayfa modeliyle aynı; R9'un eksik dil göstergesi aynı biçimde çalışır.

**D21 — SSS `faq` sayfasının altında; girdisiz SSS yayında sayılmaz (B18).** (R19, R20)
- *Gerekçe:* başlıklı ama boş bir SSS sayfası, Tuzak 1'in yasakladığı boş sayfanın kendisi.

**D22 — Sınır değerler (B15).** (C12)
- *Gerekçe:* sınırsız metin istek gövdesi ve veritabanı için açık bir kapı; `0010b`'nin
  500 karakterlik metin sınırıyla aynı sınıf.

**D23 — Ziyaretçi durumları `0002`'nin deseninde (B19).** (R23)
- *Gerekçe:* vitrinde tek desen.

**D24 — "İçerik" bölümü yalnız İçerik Editörü ve Yöneticiye (B20).** (R24)
- *Gerekçe:* `0006` R7/R8 deseni; gezinmede görünüp tıklayınca 403 veren bağlantı kötü bir
  deneyimdir.

### Verilen kararlar — plan turu, 2026-11-18

**Takım Yöneticisi (ajan) kararı**, Developer'ın plan bulguları üzerine; öneriler olduğu gibi
onaylandı (depo sahibinin onayıyla). D1–D24'ü değiştirmez, nasıl uygulanacaklarını sabitler.

**D25 — D24'ün rol kısıtı `Identity`'nin yayınladığı yetki yüzeyinde yeni bir politikadır.**
Bugün iki politika var: "oturum açmış her rol" ve "yalnız Yönetici"; C4'ü ikisi de karşılamıyor
(ölçüldü). Yönetici + İçerik Editörü için üçüncü politika, diğer ikisinin yanında tanımlanır;
`Content` kendi rol kontrolünü yazmaz. *Gerekçe:* politikaların tek kaynağı. *Bedel:* `Identity`'ye
küçük bir dokunuş. *Yan bulgu (kapsam dışı, ayrı kart önerisi):* araç yönetim uçları "oturum
açmış her rol" politikasında — İçerik Editörü bugün araç da düzenleyebiliyor; `0006` R8 ile
çelişebilir.

**D26 — D19'un gövdesi hiçbir aşamada HTML'e çevrilmez.** Ziyaretçi tarafı alt kümeyi kendi
küçük ayrıştırıcısıyla **doğrudan arayüz öğelerine** çizer; ham HTML ve izinsiz şema bu yüzden
yapı gereği çalıştırılamaz. Sunucu kaydederken **bağlantı şeması listesini** doğrular ve izinsiz
şemayı `errorCode` ile reddeder. **Ham HTML reddedilmez** — R17 gereği kaydedilir ve metin
olarak basılır; kayıtlı veride izinsiz şema bulunsa bile çizici onu bağlantı yapmaz. *(Kriter
turunda netleştirildi: ilk yazım "aynı listeyi doğrular" diyordu ve R17'yle iki türlü
okunabiliyordu.)* *Gerekçe:* "HTML üret, sonra arındır" yerine "hiç HTML üretme"; iki yığında da Markdown
veya arındırma kütüphanesi yok (ölçüldü) ve yeni bağımlılık + lisans kararı gerekmez. *Bedel:*
ayrıştırıcının bakımı bizde.

**D27 — D13'ün 301'i yalnız tanınmayan tek parçalı yolda bir okuma yapar.** Host, `/{locale}/{x}`
ayrılmış bir slug değilse `Content`'in sözleşmesine "bu dilde eski bir slug mu" diye sorar;
eşleşirse 301, eşleşmezse kabuk her zamanki gibi basılır. *Gerekçe:* `/{locale}/cars…` ve ana
sayfa okuma maliyeti ödemez. *Bedel:* tanınmayan her tek parçalı yol bir okuma — `0016b` D37'nin
ön yüklemesiyle aynı yer.

**D28 — Kabul kriterlerini Analist yazar.** Kriterler uygulama dalı açılmadan `main`'e girer
(`0016` D24'ün deseni; o kartın iki dilimi de revizyon 0 verdi).

### Verilen kararlar — kriter turu, 2026-11-18

**Takım Yöneticisi (ajan) kararı**, Analist'in kriter turu soruları S6–S13 üzerine; sekiz öneri
**olduğu gibi** onaylandı. Gerekçeleri ve bedelleri `## Açık sorular`'da.

- **D29 — "Eksik dil" = R6'nın şartını karşılamayan dil (S6).** Eksiksiz ama taslakta bekleyen
  dil eksik sayılmaz; SSS girdisi soru ve cevabın ikisi de doluyken yayına alınır. → AC-15–AC-18.
- **D30 — Taslakta boş alan kaydedilebilir; dolu her alan her kayıtta doğrulanır (S7).** Slug
  tekilliği o dildeki bütün sayfaları (taslaklar dahil) ve kayıtlı eski slug'ları kapsar.
  → AC-11, AC-48.
- **D31 — Yayındaki dilde zorunlu alanı boşaltan kayıt reddedilir (S8).** Dil sessizce
  taslağa düşmez. → AC-12.
- **D32 — Yalnız boşluktan oluşan değer boştur (S9).** → AC-10.
- **D33 — Her eski slug tek bir `301` ile güncel slug'a yönlenir; zincir yok (S10).** → AC-56.
- **D34 — Girdisiz SSS, dil değiştirici için de "yayında değil"dir (S11).** → AC-61.
- **D35 — Altlık bağlantısının metni sayfanın o dildeki başlığıdır (S12).** Kolon başlığı
  katalogdan. → AC-86.
- **D36 — SSS sırasının uçlarında düğme devre dışı, sıra dönmez; yeni girdi sona eklenir
  (S13).** → AC-78, AC-79.

### Uygulama planı (onaylı, 2026-11-18)

Uygulama dalı kriterler `main`'e girdikten (#98) sonra açıldı.

1. `Identity`: D25'in politikası (`AdminSurface.ContentPolicy`: Yönetici + İçerik Editörü).
2. `Content` modülü: `Content.Contracts` (public okuma, ayrılmış slug listesi), domain
   (sayfa, dil başına metin ve durum, eski slug'lar, SSS girdisi), tek kural yeri (C12, R13,
   R17), şema ve migration (yalnız `content`; dört sistem sayfası tohumu), Host'a bağlanma.
3. Application: komutlar (iyimser eşzamanlılık, alan bazında çoklu hata), panel ve public
   sorgular; PostgreSQL üzerinde entegrasyon testleri.
4. Uçlar ve Host: public ve yönetim uçları; kabukta eski slug 301'i (D27); Host testleri
   (roller, yönlendirme, belge durumları).
5. Ön yüz, public: rota (D9), içerik sayfası ve üç durumu, gövde çizici (D26), SSS, dil
   değiştiricinin hedefi (R15), altlığın Kurumsal kolonu (R21), talep formu bağlantısı (R22).
6. Ön yüz, panel: "İçerik" bölümü (rol görünürlüğü), sayfa düzenleme (dil başına, eksik
   dil, çakışma, onaylı silme), SSS düzenleyici (yukarı/aşağı).
7. Çeviriler, e2e, görsel kanıt (Arapça dahil), belgeler.

---

## Açık sorular

> **Çözüldü (2026-11-18).** S1–S5 → **D1–D5**. Taslağın sorularının metni git geçmişinde
> (`81e056c`).
>
> **Kriter turu (2026-11-18, Analist) → D29–D36** (Takım Yöneticisi (ajan) kararı; öneriler
> olduğu gibi onaylandı). Requirements'ı kritere çevirirken iki türlü
> okunabilen sekiz nokta çıktı: S6–S13. Her birinin kriteri önerilen okumayla yazıldı ve
> kriterin notunda belirtildi. Karar Takım Yöneticisinindir; öneri reddedilirse yalnız
> anılan kriterler değişir.

S6. **"Eksik dil" neyi sayar; SSS girdisinin yayın şartı ne?** R9 *"hangi etkin dillerde
    metnin eksik olduğunu"* gösterilmesini istiyor ama "eksik"i tanımlamıyor: (a) R6'nın
    yayın şartını karşılamayan dil, (b) yayında olmayan dil. R6 ise yalnız sayfadan söz
    ediyor; SSS girdisinin bir dilde yayına alınma şartı yazılı değil.
    Öneri: **eksik = R6'nın şartını karşılamayan dil** — sayfada başlık, gövde veya
    slug'dan biri boş; SSS girdisinde soru veya cevaptan biri boş. Metni tam ama `taslak`
    olan dil eksik sayılmaz. SSS girdisi bir dilde ancak soru ve cevabı doluysa yayına
    alınır (AC-15–AC-18).
    Gerekçe: `0010b` R13'ün deseni (R9 ona atıf yapıyor) eksik **çeviriyi** sayar, yayın
    durumunu değil; yayın durumu panelde zaten ayrıca görünüyor (R5). D20 *"sayfa modeliyle
    aynı; R9'un eksik dil göstergesi aynı biçimde çalışır"* diyor — aynı şart girdiye de
    uygulanmadan gösterge aynı biçimde çalışamaz. Boş cevaplı bir girdiyi yayına almak
    Tuzak 1'in kendisi.
    Bedel: bilerek taslakta tutulan tam bir çeviri panelde uyarı üretmez; "çevrildi ama
    yayına alınmadı" ayrımı yalnız yayın durumundan okunur.

S7. **Taslakta kaydedilen değer ne zaman doğrulanır?** R6 taslağın eksik kaydedilebildiğini
    ima ediyor (yoksa R9'un göstereceği eksik dil olmazdı), ama C12 başlığa "1–120
    karakter" diyor ve R13 slug'ı "her dilde zorunlu" sayıyor. Ayrıca R13'ün "tekillik
    locale başınadır" cümlesi yalnız yayındaki slug'ları mı, taslaktakileri de mi kapsıyor,
    yazılı değil.
    Öneri: taslakta **boş alan kaydedilebilir**; **dolu** her alan her kayıtta C12'nin ve
    R13'ün kurallarıyla doğrulanır; "zorunlu" ve C12'nin alt sınırı yayına alma anında
    uygulanır (R6). Slug tekilliği o dildeki **bütün** sayfaları — taslak ve yayında — ve
    kaydedilmiş eski slug'ları kapsar (AC-11, AC-48).
    Gerekçe: aksi hâlde iki taslak aynı slug'ı taşır ve ikincisi ancak yayına alınırken,
    belki haftalar sonra reddedilir; Tuzak 3 çakışmayı geç fark etmeyi değil önlemeyi
    istiyor.
    Bedel: editör, henüz yayında olmayan bir taslağın tuttuğu slug'ı kullanamaz; önce o
    taslağın slug'ını değiştirmesi gerekir.

S8. **Yayındaki dilde zorunlu bir alan boşaltılırsa?** R7 yayındaki dilde kaydetmenin
    değişikliği hemen yayına aldığını söylüyor; R6 yayına almanın şartını koyuyor. Başlığı
    boşaltılan yayındaki dil için iki okuma var: (a) kayıt reddedilir, (b) kayıt kabul
    edilir ve dil kendiliğinden `taslak`'a düşer.
    Öneri: **kayıt reddedilir**, alan bazında `errorCode` döner ve dil eski metniyle
    yayında kalır (AC-12).
    Gerekçe: R7'de kaydetmek yayına almaktır, öyleyse R6'nın şartı her kayıtta geçerlidir.
    (b) okuması bir yazım hatasını sessiz bir yayından kaldırmaya çevirir; KVKK metni tek
    bir kayıtla vitrinden ve talep formundan (R22) düşerdi.
    Bedel: metni boşaltarak yayından kaldırmak isteyen editör önce "yayından kaldır"
    eylemini kullanır.

S9. **Yalnız boşluktan oluşan metin "dolu" mu?** R6 "üçünün de dolu" olmasını, C12 başlığa
    en az 1 karakter istiyor; `"   "` üç karakterdir ama ziyaretçiye başlıksız bir sayfa
    gösterir.
    Öneri: başlık, gövde, slug, soru ve cevapta **yalnız boşluktan oluşan değer boş
    sayılır** (AC-10).
    Gerekçe: Tuzak 1'in *"başlıksız gövde"* ihlalinin en kolay yolu bu.
    Bedel: yok.

S10. **Slug iki kez değişirse eski adres nereye yönlenir?** R14 *"eski adres yeni adrese
    301 ile yönlenir"* diyor. `a-1` → `a-2` → `a-3` sonrası `/tr/a-1` için iki okuma var:
    (a) tek atlamada güncel slug'a (`a-3`), (b) zincirle (`a-1` → `a-2` → `a-3`).
    Öneri: **her eski slug tek bir `301` ile güncel slug'a** yönlenir (AC-56).
    Gerekçe: "yeni adres" sayfanın bugünkü adresidir; zincir her değişiklikte bir atlama
    daha ekler ve tarayıcıların ve arama motorlarının izlediği atlama sayısı sınırlıdır.
    D13'ün gerekçesi — e-postalarda paylaşılmış eski KVKK bağlantısı açılmalı — kaç
    değişiklik olursa olsun geçerli kalmalı.
    Bedel: yok; sayfanın birden çok eski slug'ı aynı hedefe gider.

S11. **Girdisiz SSS dil değiştiricide de "yayında değil" mi?** R20 girdisiz SSS'in
    *"yayında değilmiş gibi davrandığını"* söylüyor ama iki sonucunu sayıyor (altlık,
    adres); R15'in dil değiştiricisini anmıyor.
    Öneri: R20'nin "yayında değilmiş gibi" hâli R15 için de geçerlidir: sayfa yanıtı o
    dilin slug'ını taşımaz, dil değiştirici o dilin ana sayfasına gider (AC-61).
    Gerekçe: aksi hâlde dil değiştirici ziyaretçiyi R20'nin "bulunamadı" ekranına götürür —
    Tuzak 7'nin aynısı.
    Bedel: yok.

S12. **Altlık bağlantısının metni ne?** R21 Kurumsal kolonunun hangi sayfaları ve hangi
    sırayla gösterdiğini söylüyor, bağlantının metnini söylemiyor: (a) sayfanın o dildeki
    başlığı, (b) çeviri kataloğundan sabit bir etiket ("Hakkımızda").
    Öneri: **sayfanın o dildeki başlığı** (AC-86). Kolonun başlığı ("Kurumsal") katalogdan
    gelir.
    Gerekçe: galeri sayfaya "KVKK Aydınlatma Metni" ya da "Gizlilik Politikası" diyebilir;
    altlıkta başka, sayfada başka ad görünmesi metnin tek kaynağını ikiye böler. Başlık
    yayın şartıdır (R6), boş olamaz.
    Bedel: 120 karaktere kadar uzun bir başlık altlıkta uzun bir bağlantı olur; kısa
    tutmak editörün işi.

S13. **SSS sırasının uçları ve yeni girdinin yeri.** R18 sıranın yukarı/aşağı düğmeleriyle
    değiştiğini söylüyor ama (a) ilk girdinin "yukarı" ve son girdinin "aşağı" düğmesinin
    ne yaptığını, (b) yeni girdinin sıranın neresine eklendiğini söylemiyor.
    Öneri: uçtaki düğme **devre dışıdır**, sıra dönmez; yeni girdi **sıranın sonuna**
    eklenir (AC-78, AC-79).
    Gerekçe: `0016b`'nin galeri gezintisi uçlarda dönmüyor — panelde de tek desen. Sona
    eklemek mevcut sırayı bozmaz.
    Bedel: en üste konacak yeni bir girdi için düğmeye birkaç kez basılır.

---

## Acceptance Criteria

> Her kriterin testi `DisplayName`'inde `0017/AC-<n>` taşır (`docs/testing.md` § 1).
> Kriterleri kriter turunda (2026-11-18) **Analist** yazdı (**D28**). Requirements,
> Constraints, KAPSAM DIŞI ve kararlar bu turda değişmedi.
>
> **Ortak dil.** "Kurulum" etkin locale'leri `tr`, `ar`, `en` olan kurulumdur; aksi kriterde
> yazılır. "Bulunamadı ekranı" R12'nin ekranıdır (`0002` R5'in deseni). "Public okuma"
> oturumsuz içerik okumasıdır. "Reddedilir" = uç 2xx dönmez, yanıt ilgili alan için bir
> `errorCode` taşır ve kayıtlı veri değişmez. Karakter sayısı bayt değil karakterdir.
>
> **Örnek veri** (kriterler aksini söylemedikçe):
>
> | Sayfa | `tr` başlık / slug | `ar` başlık / slug | `en` başlık / slug |
> |---|---|---|---|
> | `about` | Hakkımızda / `hakkimizda` | من نحن / `man-nahnu` | About us / `about-us` |
> | `privacy` | KVKK Aydınlatma Metni / `kvkk` | سياسة الخصوصية / `al-khususiya` | Privacy Notice / `privacy` |
> | `cookies` | Çerez Politikası / `cerezler` | سياسة الكوكيز / `al-kukiz` | Cookie Policy / `cookies` |
> | `faq` | Sıkça Sorulan Sorular / `sss` | الأسئلة الشائعة / `al-asila` | FAQ / `faq` |
> | serbest sayfa G | Garanti Koşulları / `garanti-kosullari` | شروط الضمان / `shurut-al-daman` | Warranty Terms / `warranty-terms` |
>
> SSS girdileri global sırayla A, B, C: A "Takas yapıyor musunuz?" (`tr` ve `ar` yayında),
> B "Kredi imkânı var mı?" (`tr` yayında, `ar` taslak), C "Ekspertiz raporu veriyor
> musunuz?" (`tr` ve `ar` yayında). Ayırt edici seçildi: B ortada ve yalnız bir dilde
> yayında.
>
> *AC-15–AC-18 (S6) · AC-11, AC-48 (S7) · AC-12 (S8) · AC-10 (S9) · AC-56 (S10) ·
> AC-61 (S11) · AC-86 (S12) · AC-78, AC-79 (S13), `## Açık sorular`'daki önerilen okumayla
> yazıldı. C10 süreç kuralıdır ve kendi kriterini gerektirmez: bu bölümün varlığı ve
> `DisplayName` eşlemesi onu karşılar.*

### Sayfa yaşam döngüsü — oluşturma ve dil başına metin (R1, R2, R3, R4, R5 · D6)

- **AC-1.** Yeni bir kurulumda panel tam dört sistem sayfası listeler — `about`,
  `privacy`, `cookies`, `faq` —; her birinin üç dilde başlığı, gövdesi ve slug'ı boştur, üç
  dilin durumu `taslak`'tır ve hiç serbest sayfa yoktur.
- **AC-2.** İçerik Editörü `tr` başlığı "Garanti Koşulları", gövdesi "Araçlarımız
  12 ay garantilidir." ve slug'ı `garanti-kosullari` olan bir sayfa oluşturur; sayfa panelde
  **serbest sayfa** olarak görünür ve üç dilin durumu `taslak`'tır.
- **AC-3.** Yönetici AC-2'nin sayfasını aynı biçimde oluşturabilir
  ve aynı sonucu alır.
- **AC-4.** G'nin `en` dilinde başlık "Warranty Terms", gövde "Our cars carry a 12-month
  warranty." ve slug `warranty-terms` kaydedildikten sonra `tr` metni değişmez: `tr`
  başlığı "Garanti Koşulları", slug'ı `garanti-kosullari` kalır.
- **AC-5.** İçerik Editörü G'yi `tr`'de yayına alır: `/tr/garanti-kosullari`
  sayfanın `h1`'ini "Garanti Koşulları" ve gövdesini basar; `en` ve `ar`'ın durumu `taslak`
  kalır.
- **AC-6.** AC-5'den sonra, `en` metni dolu ama `taslak` iken
  `/en/warranty-terms` bulunamadı ekranını gösterir.
- **AC-7.** İki serbest sayfa oluşturulup biri silindikten sonra panel yine tam
  dört sistem sayfası ve bir serbest sayfa listeler.

### Yayına alma şartı ve eksik diller (R6, R9 · C5 · D16)

- **AC-8.** `tr` başlığı "Garanti Koşulları", gövdesi ve slug'ı boş olan dili yayına
  alma isteği reddedilir: yanıt gövde ve slug alanlarının her biri için bir `errorCode`
  taşır, başlık için taşımaz; dil `taslak` kalır.
- **AC-9.** Panelde AC-8'nin reddi gövde ve slug alanlarının yanında,
  etkin arayüz dilinin kataloğundan gelen metinle gösterilir; başlık alanının yanında hata
  yoktur. `ar` arayüzde metin Arapça'dır.
- **AC-10.** Başlığı yalnız boşluklardan (`"   "`) oluşan, gövdesi ve slug'ı dolu bir dili
  yayına alma isteği reddedilir ve başlık alanı için `errorCode` döner (S9).
- **AC-11.** `taslak` bir dil, başlığı "Garanti Koşulları" ve gövdesi ile slug'ı
  boşken kaydedilebilir: kayıt başarılıdır ve dil `taslak` kalır (S7).
- **AC-12.** `tr`'de yayındaki G'nin başlığı boşaltılıp kaydedilirse istek
  reddedilir ve başlık için `errorCode` döner; `/tr/garanti-kosullari` "Garanti
  Koşulları" başlığıyla yayında kalır (S8).
- **AC-13.** `tr` ve `en` metni tam, `ar` metni boş olan sayfa için panel eksik dil olarak
  yalnız `ar`'ı gösterir.
- **AC-14.** Etkin locale'leri `tr`, `en` olan bir kurulumda AC-13'nın sayfası
  için panel hiç eksik dil göstermez: `ar` sayılmaz.
- **AC-15.** Üç dilde başlığı, gövdesi ve slug'ı dolu, `ar`'ı `taslak` olan sayfa
  için panel `ar`'ı eksik göstermez — yayında olmamak eksiklik değildir (S6).
- **AC-16.** Üç dilde başlığı ve gövdesi dolu, `en` slug'ı boş sayfa için panel eksik
  dil olarak yalnız `en`'i gösterir (S6).
- **AC-17.** Sorusu dolu, `ar` cevabı boş bir SSS girdisini `ar`'da yayına alma isteği
  reddedilir ve cevap alanı için `errorCode` döner (S6).
- **AC-18.** AC-17'nın girdisi için panel eksik dil olarak `ar`'ı gösterir
  (S6).

### Yayındaki dilde kaydetme (R7 · D15)

- **AC-19.** `tr`'de yayındaki G'nin gövdesi "Araçlarımız 24 ay garantilidir." olarak
  kaydedilir; ayrı bir yayına alma eylemi olmadan, kayıttan sonraki ilk public okuma "24 ay"
  metnini döner ve "12 ay" metnini içermez.
- **AC-20.** AC-19'nin kaydından sonra `tr`'nin durumu `yayında`'dır ve panelin
  okuduğu `tr` metni public okumanın döndüğü metinle aynıdır: bekleyen ikinci bir metin
  yoktur.

### Yayından kaldırma ve silme (R3, R8 · D17)

- **AC-21.** `tr`'de yayındaki G yayından kaldırılınca `tr`'nin durumu `taslak` olur,
  `/tr/garanti-kosullari` bulunamadı ekranını gösterir ve panel `tr` başlığını, gövdesini
  ve slug'ını değişmeden gösterir.
- **AC-22.** AC-21'daki dil yeniden yayına alınınca `/tr/garanti-kosullari`, metin
  yeniden girilmeden, kaldırmadan önceki başlık ve gövdeyi basar.
- **AC-23.** `privacy`'nin yayındaki `tr` dili yayından kaldırılabilir: durumu
  `taslak` olur.
- **AC-24.** Panelde serbest sayfalar ve SSS girdileri için bir silme eylemi vardır;
  dört sistem sayfasının hiçbiri için yoktur.
- **AC-25.** `about` için uca gönderilen silme isteği reddedilir; sayfa ve üç dilin
  metni değişmeden kalır.
- **AC-26.** Serbest sayfa G'de silme seçilince bir onay penceresi açılır; pencerede
  vazgeçilirse G panelde kalır ve `/tr/garanti-kosullari` sayfayı göstermeye devam eder.
- **AC-27.** Onay penceresinde onaylanınca G panelden kalkar ve `/tr/garanti-kosullari`,
  `/en/warranty-terms` ve `/ar/shurut-al-daman` bulunamadı ekranını gösterir.
- **AC-28.** Silinen G geri getirilemez: panelde silinmiş sayfaları listeleyen ya da geri
  getiren bir eylem yoktur ve G'nin kimliğiyle yapılan panel okuması `404` döner.
- **AC-29.** A, B, C girdilerinden B silinince `/tr/sss` A ve C'yi bu sırayla gösterir;
  panelde de sıra A, C'dir.

### Eşzamanlı kaydetme (R10 · C14 · D18)

- **AC-30.** İki editör G'yi aynı sürümde açar; birincisi `tr` başlığını "Garanti"
  olarak kaydeder; ikincisi aynı eski sürüm üzerinden "Garanti Şartları"nı kaydetmeye
  çalışır → uç `409` döner ve kayıtlı başlık "Garanti" kalır.
- **AC-31.** AC-30'un ikinci editörünün panelinde *"sayfa başkası tarafından
  değiştirildi"* uyarısı etkin arayüz dilinin kataloğundan görünür ve form güncel hâli
  yeniden yükler: başlık alanında "Garanti" yazar.
- **AC-32.** Aynı sürüm üzerinden iki kaydetme isteği **eşzamanlı** gönderildiğinde
  tam biri başarılı olur, diğeri `409` alır (ardışık değil, paralel gönderim).
- **AC-33.** SSS girdisi A için AC-30'un senaryosu: eski sürüm üzerinden kaydetme `409`
  döner ve kayıtlı soru metni değişmez.

### Adres ve "bulunamadı" (R11, R12 · D9, D10, D27)

- **AC-34.** `about` `tr`'de yayındayken `/tr/hakkimizda` sayfanın `tr` başlığını `h1`
  olarak ve `tr` gövdesini basar.
- **AC-35.** `/tr/hic-olmayan` bulunamadı ekranını gösterir ve içerik ucu bu slug için `404`
  döner.
- **AC-36.** `/ar/hakkimizda` bulunamadı ekranını gösterir: `hakkimizda` `about`'ın
  yalnız `tr` slug'ıdır.
- **AC-37.** `/tr/foo` araç listesini **göstermez**; bulunamadı ekranını gösterir (D9 —
  `0014` D28'in tek parçalı yol davranışı kalkar).
- **AC-38.** İki parçalı tanınmayan yol `/tr/foo/bar` bu spec'ten önceki davranışını
  korur: araç listesine düşer (R12 yalnız tek parçalı yolu değiştirir).
- **AC-39.** `/tr/`, `/tr/cars` ve yayındaki bir ilanın `/tr/cars/{slug}`
  adresi bu spec'ten önceki ekranlarını gösterir: ana sayfa, liste, detay.
- **AC-40.** Bulunamadı ekranı `0002` R5'in "bulunamadı" ekranıyla aynı bileşenle basılır
  (görsel kanıt).
- **AC-41.** `/tr/hic-olmayan` belge isteğinin HTTP durumu bugünkü gibi `200`'dür; gerçek
  `404` bu spec'te yoktur (X11, D10).
- **AC-42.** `/tr/` ve `/tr/cars` belge istekleri `Content`'e eski slug okuması yapmaz
  (D27).

### Slug kuralları (R13 · C7, C11, C12 · D11, D12)

- **AC-43.** Şu slug'lar kabul edilir: `ab` (2 karakter), `kvkk`,
  `kvkk-aydinlatma-metni`, `sss-2026` ve 80 karakterlik bir slug.
- **AC-44.** Şu slug'ların her biri ayrı bir istekte reddedilir ve slug alanı
  için `errorCode` döner: `a` (1 karakter), 81 karakterlik bir slug, `Hakkimizda`,
  `hakkımızda`, `من-نحن`, `-kvkk`, `kvkk-`, `kvkk--metni`, `kvkk_metni`, `kvkk metni`.
- **AC-45.** Başlık "Garanti Koşulları" girilip slug boş bırakılarak kaydedildiğinde
  slug boş kalır; başlıktan bir slug üretilmez.
- **AC-46.** Sayfa A'nın `tr` slug'ı `garanti` iken sayfa B'nin `tr` slug'ını `garanti`
  yapmak reddedilir; B'nin `en` slug'ını `garanti` yapmak kabul edilir.
- **AC-47.** Aynı sayfanın `tr` ve `en` slug'ı aynı olabilir: ikisi de `garanti`.
- **AC-48.** Tekillik taslakları da kapsar: A'nın `tr`'si `taslak` ve slug'ı
  `garanti` iken B'nin `tr` slug'ını `garanti` yapmak reddedilir (S7).
- **AC-49.** `cars`, `admin`, `api` ve `assets` slug'ları üç dilin her birinde
  reddedilir; `cars-ve-biz` kabul edilir.
- **AC-50.** Ayrılmış slug listesi `cars`, `admin`, `api`, `assets`'i içerir ve
  vitrinin tanıdığı her tek parçalı bölüm adı (bugün `cars`) bu listededir.

### Eski slug ve 301 (R14 · D13, D27)

- **AC-51.** `privacy` `tr`'de yayındayken slug'ı `kvkk` → `kvkk-aydinlatma` değiştirilince
  `/tr/kvkk` belge isteği `301` ve `Location: /tr/kvkk-aydinlatma` döner.
- **AC-52.** AC-51'den sonra `/en/kvkk` `301` dönmez ve bulunamadı ekranını gösterir:
  eski slug yalnız `tr` için kaydedilmiştir.
- **AC-53.** `taslak` bir dilde slug değişikliği kaydedilmez: `privacy`'nin `tr`'si
  taslakken slug `kvkk` → `kvkk-metni` değiştirilir, sonra `tr` yayına alınır; `/tr/kvkk`
  `301` dönmez ve bulunamadı ekranını gösterir.
- **AC-54.** AC-51'den sonra başka bir sayfanın `tr` slug'ını `kvkk` yapmak
  reddedilir; aynı sayfanın `en` slug'ını `kvkk` yapmak kabul edilir.
- **AC-55.** Aynı sayfa kendi eski slug'ına dönebilir: AC-51'den sonra `privacy`'nin
  `tr` slug'ı yeniden `kvkk` yapılınca `/tr/kvkk` sayfayı gösterir ve
  `/tr/kvkk-aydinlatma` `301` ile `/tr/kvkk`'ya yönlenir.
- **AC-56.** G'nin `tr` slug'ı `a-1` → `a-2` → `a-3` değiştirildikten sonra `/tr/a-1` ve
  `/tr/a-2`'nin her biri **tek** bir `301` ile doğrudan `/tr/a-3`'e yönlenir (S10).
- **AC-57.** Eski slug'ı `eski-garanti` olan G silinince `/tr/eski-garanti` `301`
  dönmez ve bulunamadı ekranını gösterir; `eski-garanti` artık başka bir sayfanın `tr`
  slug'ı yapılabilir.

### Dil değiştirici (R15 · D14)

- **AC-58.** `about` `tr`'de ve `en`'de yayında, `ar`'da `taslak` iken `tr` sayfa
  yanıtı `en` için `about-us` slug'ını taşır; `ar` için slug taşımaz ve `man-nahnu` yanıtta
  hiçbir yerde geçmez.
- **AC-59.** `/tr/hakkimizda`'da dil değiştiriciden English seçilince adres
  `/en/about-us` olur.
- **AC-60.** Aynı durumda العربية seçilince adres `/ar/` (ana sayfa) olur;
  `/ar/hakkimizda` olmaz.
- **AC-61.** `faq` `ar`'da yayında ama `ar`'da yayındaki girdisi yokken `/tr/sss`
  yanıtı `ar` slug'ı taşımaz ve dil değiştiriciden العربية seçilince adres `/ar/` olur
  (S11).
- **AC-62.** İçerik sayfası dışındaki ekranlarda dil değiştirici bugünkü davranışını
  korur: `/tr/cars`'ta English seçilince adres `/en/cars` olur.

### Gövde — desteklenen öğeler (R16 · D19, D26)

- **AC-63.** Şu gövde basıldığında gövde bölümünde bir `h2` ("Garanti"), bir `h3`
  ("Kapsam"), iki paragraf, bir `strong` ("kalın"), bir `em` ("italik"), iki maddeli bir
  `ul`, iki maddeli bir `ol` ve `href`'i `https://example.com` olan bir bağlantı vardır:

  ```
  ## Garanti
  ### Kapsam
  Birinci paragraf.

  İkinci paragraf **kalın** ve *italik*.

  - motor
  - şanzıman

  1. başvur
  2. bekle

  [Bize yazın](https://example.com)
  ```
- **AC-64.** Gövdesi `# Büyük başlık` satırını içeren sayfada tam bir `h1` vardır ve metni
  başlık alanıdır; "# Büyük başlık" düz metin olarak görünür.
- **AC-65.** Desteklenmeyen sözdiziminin her biri düz metin olarak basılır:
  `> alıntı` için `blockquote` yoktur ve "> alıntı" görünür; `` `kod` `` için `code`
  yoktur; `![resim](https://example.com/a.png)` için `img` yoktur; `---` için `hr` yoktur.

### Gövde — enjeksiyon (R17 · D19, D26)

- **AC-66.** Gövdesi `<script>alert(1)</script>` olan sayfada bu metin sayfada görünür;
  gövde bölümünde `script` öğesi yoktur ve `alert` çağrılmaz.
- **AC-67.** Gövdesi `<img src=x onerror=alert(1)>` olan sayfada bu metin görünür; gövde
  bölümünde `img` öğesi yoktur ve `alert` çağrılmaz.
- **AC-68.** `<b>kalın</b>` içeren gövde kaydedilir (reddedilmez) ve "<b>kalın</b>"
  metni olarak basılır; gövde bölümünde `b` öğesi yoktur.
- **AC-69.** Kayıtlı veride bulunsa bile `[tıkla](javascript:alert(1))`,
  `[tıkla](JavaScript:alert(1))`, `[tıkla](data:text/html,x)` ve `[tıkla](vbscript:x)`
  bağlantı olarak basılmaz: gövde bölümünde `href`'i bu şemalardan biriyle başlayan öğe
  yoktur.
- **AC-70.** AC-69'nın dört bağlantısından birini içeren gövdeyi kaydetme
  isteği reddedilir ve gövde alanı için `errorCode` döner (D26).
- **AC-71.** `http://example.com`, `https://example.com`, `mailto:info@example.com`,
  `tel:+905551112233` ve `/tr/cars` bağlantıları kaydedilir ve her biri bir `a` öğesi
  olarak basılır.
- **AC-72.** `http://example.com` ve `https://example.com` bağlantıları
  `rel="noopener noreferrer"` taşır.
- **AC-73.** Gövdeyi basan ziyaretçi kodunda `dangerouslySetInnerHTML` ve `innerHTML`
  geçmez: gövde hiçbir aşamada HTML dizgesine çevrilmez (D26).
- **AC-74.** SSS cevabı `**24 ay** garanti` `strong` öğesiyle basılır ve
  `[tıkla](javascript:alert(1))` içeren cevabı kaydetme isteği reddedilir.

### SSS (R18, R19, R20 · D20, D21)

- **AC-75.** Örnek veride `/tr/sss` A, B, C'yi; `/ar/al-asila` yalnız A ve C'yi bu
  sırayla gösterir. Arapça sayfada girdilerin `ar` soru ve cevabı görünür.
- **AC-76.** Panelde C'nin "yukarı" düğmesine bir kez basılınca sıra A, C, B olur;
  `/tr/sss` A, C, B'yi, `/ar/al-asila` A, C'yi gösterir.
- **AC-77.** Panelde A'nın "aşağı" düğmesine bir kez basılınca sıra B, A, C olur.
- **AC-78.** İlk girdinin "yukarı" ve son girdinin "aşağı" düğmesi devre dışıdır; ilk
  girdide "yukarı" etkinleştirilmeye çalışılınca sıra değişmez (S13).
- **AC-79.** Yeni oluşturulan girdi D, sıranın sonuna eklenir: A, B, C, D (S13).
- **AC-80.** Sıra yalnız klavyeyle değiştirilebilir: "yukarı"/"aşağı" düğmelerine Tab
  ile ulaşılır ve Enter ya da Space ile etkinleştirilir.
- **AC-81.** `/tr/sss`'te ilk girdi, belge sırasında `faq` gövdesinin son öğesinden sonra
  gelir.
- **AC-82.** `faq` `tr`'de yayında ama A, B, C'nin üçü de `tr`'de taslakken
  `tr` altlığında SSS bağlantısı yoktur.
- **AC-83.** AC-82'in durumunda `/tr/sss` bulunamadı ekranını
  gösterir ve içerik ucu `404` döner.
- **AC-84.** AC-82'in durumunda A `tr`'de yayına alınınca `/tr/sss`
  sayfayı ve A'yı gösterir; `tr` altlığında SSS bağlantısı görünür.
- **AC-85.** `faq`'ın `tr`'si `taslak` iken, yayındaki girdiler olsa da
  `/tr/sss` bulunamadı ekranını gösterir.

### Altlığın Kurumsal kolonu (R21 · C13 · D4, D6, D7)

- **AC-86.** Dört sistem sayfası `tr`'de yayında ve `faq`'ın `tr`'de yayındaki
  girdisi varken `tr` altlığının "Kurumsal" kolonu dört bağlantıyı bu sırayla gösterir:
  "Hakkımızda" → `/tr/hakkimizda`, "Sıkça Sorulan Sorular" → `/tr/sss`, "KVKK Aydınlatma
  Metni" → `/tr/kvkk`, "Çerez Politikası" → `/tr/cerezler`. Bağlantı metni sayfanın o
  dildeki başlığıdır (S12).
- **AC-87.** Sayfalar `cookies`, `privacy`, `faq`, `about` sırasıyla yayına
  alınmış olsa da kolonun sırası `about`, `faq`, `privacy`, `cookies`'dir.
- **AC-88.** `tr`'de yalnız `about` ve `cookies` yayındayken kolon iki bağlantı
  gösterir: "Hakkımızda", "Çerez Politikası".
- **AC-89.** `tr`'de dört sistem sayfasının hiçbiri yayında değilken altlıkta
  "Kurumsal" başlığı ve kolonu yoktur; altlığın diğer kolonları bu spec'ten önceki
  hâlindedir.
- **AC-90.** `ar`'da yalnız `privacy` yayındayken `/ar/` altlığının kolonu yalnız
  "سياسة الخصوصية" bağlantısını gösterir; `tr` altlığı kendi yayın durumunu izler.
- **AC-91.** `tr`'de yayındaki serbest sayfa G altlıkta görünmez;
  `/tr/garanti-kosullari` doğrudan açıldığında sayfayı gösterir.
- **AC-92.** `about`'ın `tr` slug'ı `hakkimizda` → `biz-kimiz` değiştirilince
  altlık bağlantısı doğrudan `/tr/biz-kimiz`'e gider; eski adrese ve `301`'e uğramaz.

### Talep formunun KVKK bağlantısı (R22 · C13 · D5)

- **AC-93.** `privacy` `tr`'de yayındayken bir ilanın `/tr/cars/{slug}` sayfasındaki
  talep formunun izin satırında `/tr/kvkk`'ya giden bir bağlantı vardır.
- **AC-94.** `privacy` `ar`'da `taslak` iken `/ar/cars/{slug}`'taki izin satırında hiç
  bağlantı öğesi yoktur — boş, `#` ya da `/ar/`'e giden bağlantı da yoktur; izin metni ve
  onay kutusu görünür.
- **AC-95.** AC-94'un durumunda form doldurulup gönderilebilir ve talep
  kaydedilir (`0005`'in davranışı).
- **AC-96.** `privacy`'nin `tr` slug'ı `kvkk` → `kvkk-aydinlatma` değiştirilince
  izin satırının bağlantısı doğrudan `/tr/kvkk-aydinlatma`'ya gider.
- **AC-97.** `privacy`'nin `tr`'si yayından kaldırılınca bir sonraki yüklemede
  `/tr/cars/{slug}`'taki izin satırında bağlantı yoktur.

### Ziyaretçi durumları (R23 · D10, D23)

- **AC-98.** İçerik isteği yanıtlanana kadar `/tr/hakkimizda` `0002`'nin iskelet
  desenini gösterir; bulunamadı ve hata ekranı görünmez.
- **AC-99.** İçerik ucu `500` döndüğünde `/tr/hakkimizda` yeniden deneme düğmeli hata
  ekranını gösterir; bulunamadı ekranını göstermez.
- **AC-100.** AC-99'nın ekranında yeniden deneme düğmesine basılınca içerik ucu
  yeniden çağrılır ve başarılı yanıtla sayfa basılır.
- **AC-101.** İçerik ucu `404` döndüğünde bulunamadı ekranı görünür ve yeniden deneme
  düğmesi yoktur.

### Panel durumları (R23 · `docs/frontend.md` § 3)

- **AC-102.** Panelin "İçerik" bölümü, sayfa listesi isteği yanıtlanana kadar bir
  yükleniyor göstergesi gösterir.
- **AC-103.** Hiç serbest sayfa yokken panel dört sistem sayfasını listeler ve serbest
  sayfalar için katalogdan gelen bir boş durum metni gösterir.
- **AC-104.** Hiç SSS girdisi yokken panelin SSS alanı katalogdan gelen bir boş durum
  metni gösterir.
- **AC-105.** Panelin sayfa listesi isteği `500` döndüğünde "İçerik" bölümü katalogdan
  gelen bir hata durumu gösterir; boş durum metnini göstermez.

### Erişim (R24 · C4 · D24, D25)

- **AC-106.** İçerik Editörü oturumuyla panel gezinmesinde "İçerik" bölümü vardır.
- **AC-107.** Yönetici oturumuyla panel gezinmesinde "İçerik" bölümü vardır.
- **AC-108.** Satış Danışmanı oturumuyla panel gezinmesinde "İçerik" bölümü yoktur.
- **AC-109.** Satış Danışmanı oturumuyla içeriğin her yazma ucuna (oluşturma, kaydetme,
  yayına alma, yayından kaldırma, silme, SSS sırası) gönderilen istek `403` döner ve veri
  değişmez.
- **AC-110.** Oturumsuz gönderilen aynı yazma istekleri `401` döner ve veri değişmez.
- **AC-111.** İçerik Editörünün panel gezinmesi, "İçerik" dışında, bu spec'ten önceki
  hâliyle aynıdır.
- **AC-112.** `about`'ın `ar` başlığı "مسودة سرية" ve `ar`'ı `taslak` iken oturumsuz `ar`
  okuması `404` döner; `tr` okumasının yanıtında "مسودة سرية" geçmez.
- **AC-113.** `Content`'te rol kodu (`content_editor`, `admin`, `sales_advisor`)
  karşılaştıran kod yoktur; rol kısıtı `Identity`'nin yayınladığı politikadan gelir
  (D25).

### Sınır değerler (C12 · C5 · D22)

- **AC-114.** 120 karakterlik başlık kabul edilir; 121 karakterlik başlık reddedilir
  ve başlık alanı için `errorCode` döner.
- **AC-115.** 120 Arapça harften (`ب` × 120) oluşan başlık kabul edilir.
- **AC-116.** 50.000 karakterlik gövde kabul edilir; 50.001 karakterlik gövde
  reddedilir ve gövde alanı için `errorCode` döner.
- **AC-117.** 200 karakterlik SSS sorusu kabul edilir; 201 karakterlik soru reddedilir
  ve soru alanı için `errorCode` döner.
- **AC-118.** 2.000 karakterlik SSS cevabı kabul edilir; 2.001 karakterlik cevap
  reddedilir ve cevap alanı için `errorCode` döner.
- **AC-119.** 121 karakterlik başlık ve `A` slug'ıyla kaydetme isteği tek yanıtta
  iki ayrı hata döner: biri başlık, biri slug alanına bağlı.
- **AC-120.** Doğrulama hatası yanıtındaki her hata yalnız bir alan adı ve bir
  `errorCode` taşır; yanıtta kullanıcıya gösterilecek bir hata cümlesi yoktur.

### Locale ve RTL (R25 · C6, C8)

- **AC-121.** `/ar/man-nahnu` belgesi `lang="ar"` ve `dir="rtl"` taşır; gövdenin
  başlıkları, paragrafları ve listeleri sağdan sola akar (görsel kanıt).
- **AC-122.** `/ar/` altlığının "Kurumsal" kolonu sağdan sola akar (görsel kanıt).
- **AC-123.** Arapça arayüzlü personel için panelin "İçerik" bölümü sağdan sola akar ve
  arayüz metinleri Arapça'dır (görsel kanıt).
- **AC-124.** Bu spec'in eklediği stillerde fiziksel yön özelliği (`margin-left`,
  `padding-right`, `left`, `right`, `text-align: left` vb.) ve gömülü renk değeri (hex,
  `rgb()`, `hsl()`) yoktur; renk ve tipografi `frontend-002`'nin token katmanından gelir.
- **AC-125.** Bu spec'in eklediği arayüz metinlerinin (Kurumsal başlığı, panelin
  "İçerik" bölümü, hata kodlarının metinleri, çakışma uyarısı, onay penceresi, boş
  durumlar, eksik dil göstergesi, izin satırının bağlantı metni) anahtarları `tr`, `ar` ve
  `en` kataloglarının üçünde de doludur; eksik anahtar kontrolü yeşildir.
- **AC-126.** `/en/about-us`'ta sayfanın ve altlığın arayüz metinleri İngilizce'dir; Türkçe ya
  da Arapça arayüz metni görünmez.
- **AC-127.** Öneksiz `/hakkimizda` isteği `Accept-Language: ar` ile gönderildiğinde
  `/ar/…`'e yönlenmez: yanıt bu spec'ten önceki gibi `404`'tür ve `Location` başlığı
  taşımaz.
- **AC-128.** `/tr/hakkimizda` isteği `Accept-Language: en` ile gönderildiğinde `tr`
  sayfası basılır; `/en/…`'e yönlendirme yoktur.

### Mimari (C1, C2, C3, C9)

- **AC-129.** `content` şemasının tablolarına `Content` dışında yazan kod yoktur; Host
  ve diğer modüller `Content`'e yalnız `Content.Contracts` üzerinden erişir.
- **AC-130.** Hiçbir migration `translations` adlı bir tablo oluşturmaz; içerik metni
  yalnız `content` şemasının tablolarında saklanır.
- **AC-131.** Veritabanında yayın durumu `draft` / `published` ve sistem sayfası kimliği
  `about` / `privacy` / `cookies` / `faq` kodu olarak saklanır; "Taslak", "Yayında",
  "Hakkımızda" gibi çevrilmiş bir ad saklanmaz.
- **AC-132.** `content` migration'ı yalnız `content` şemasına dokunur.

### Regresyon (D5, D9)

- **AC-133.** `0014`'ün kabul kriterlerinin testleri yeşil kalır (D28'e bağlı kriter
  yoktur — K3).
- **AC-134.** `0005`'in kabul kriterlerinin testleri yeşil kalır; izin kaydı bu spec'ten
  önceki gibi bir `bool`'dur (X8).

---

## Definition of Done

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
      *(S1–S5 → D1–D5, B1–B20 → D5–D24, 2026-11-18.)*
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0017-ac<n>.png`), boş ve hata durumları dâhil,
      Arapça görüntü de var.
- [ ] Gövde arındırmasının enjeksiyon girişimini kırdığı testle gösterildi (R17 · **D19**):
      ham HTML, `javascript:` bağlantısı ve dış bağlantının `rel`'i.
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — merkezi `translations` tablosu kurulmadı (C2).
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `content` şemasına dokunuyor.
- [ ] Talep formunun izin satırındaki KVKK bağlantısı, `privacy` yayındayken ve değilken
      kanıtla gösterildi (R22 · **D5**). *(Eski "hedefi kontrol edildi" kutusu kaldırıldı:
      ölçüldü, bağlantı yok — K1.)*
- [ ] İzin anındaki metin sürümünün kaydı için ayrı kart açıldı (X8).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi (arayüz metinleri; içerik gövdesi veri).
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

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
