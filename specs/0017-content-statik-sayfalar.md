# 0017 — Content: Statik Sayfalar ve SSS

| | |
|---|---|
| **Durum** | Taslak — kararlar verildi, kabul kriterleri yazılmadı |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (yeni — bugün hiç yok) · `frontend/` public vitrin + panel · `Host` (eski slug yönlendirmesi — **D13**) · `frontend/` talep formu (KVKK bağlantısı — **D5**) ve altlık (Kurumsal kolonu — **D6**, **D7**) |
| **Bağımlı spec'ler** | `0006` (roller: İçerik Editörü — tamamlandı) · `0014` (ana sayfa — tamamlandı; altlığına kolon eklenir, **D28**'i değişir — **D9**) · `0005` (talep formu — tamamlandı; izin satırına bağlantı eklenir — **D5**) · `0002` ("bulunamadı" ve hata ekranı deseni — **D10**, **D23**) · `0007` (iyimser eşzamanlılık deseni — **D18**) · `0004` (SEO — gerçek HTTP 404 kararı oraya devredildi, **D10**) |
| **Dayanak** | ADR 0003 Ek 1, sıra #11 |
| **Karar bekliyor** | yok — S1–S5 → **D1–D4**; QA CLARIFY turunun yirmi bulgusu (B1–B20) → **D5–D24** (2026-11-18). |

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
> Kabul kriterleri bir sonraki turda yazılacak.

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

---

## Açık sorular

> **Çözüldü (2026-11-18).** S1–S5 → **D1–D5**. Taslağın sorularının metni git geçmişinde
> (`81e056c`).

---

## Acceptance Criteria

> Bir sonraki turda yazılacak. En az şu kümeleri kapsayacak:
>
> **Sayfa yaşam döngüsü**
> - Serbest sayfa oluşturma, düzenleme ve dil başına yayınlama (R1, R4, R5).
> - Yayına alma şartı ve eksik alanlar; yalnız etkin locale'lerin sayılması (R6, R9).
> - Yayındaki dilde kaydetmenin hemen görünmesi (R7).
> - Yayından kaldırma ve silmenin ayrılığı; sistem sayfasının silinememesi (R3, R8).
> - Eşzamanlı kaydetmede `409` (R10).
>
> **Adres ve slug**
> - Yayındaki sayfanın adresi; taslak, olmayan ve silinmiş slug'da "bulunamadı" (R11, R12).
> - `/tr/foo`'nun artık listeye düşmemesi (R12, D9).
> - Slug biçimi, uzunluğu, locale başına tekilliği ve ayrılmış slug'lar (R13, C11).
> - Eski slug'dan 301 ve eski slug'ın başka sayfaya verilememesi (R14).
> - Dil değiştiricinin hedef slug'a ya da ana sayfaya gitmesi (R15).
>
> **Gövde**
> - Desteklenen öğeler; `#` başlığın olmaması (R16).
> - Ham HTML, `javascript:` bağlantısı ve dış bağlantı `rel`'i için enjeksiyon testleri
>   (R17).
>
> **SSS**
> - Dil başına metin ve yayın; global sıra ve yukarı/aşağı düğmeleri (R18).
> - `faq` sayfasının altındaki gösterim; girdisiz SSS'in yayında sayılmaması (R19, R20).
>
> **Vitrin bağlantıları**
> - Altlığın Kurumsal kolonu: sıra, yalnız yayındakiler, hiçbiri yoksa kolonun olmaması
>   (R21).
> - Talep formunun KVKK bağlantısı: yayındaysa var, değilse yok (R22).
>
> **Durumlar, erişim, locale**
> - Ziyaretçi ve panel durumları (R23).
> - Rol kısıtı: gezinmede görünürlük ve Satış Danışmanına `403` (R24, C4).
> - Sınır değerler ve alan bazında hata kodları (C12).
> - Üç locale ve RTL (R25).

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
