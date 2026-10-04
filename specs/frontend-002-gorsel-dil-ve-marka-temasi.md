# frontend-002 — Görsel Dil ve Kuruluma Özel Tema

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
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
örneği* olarak yazılı hex'in aynısıdır: dokümandaki örnek, token dosyasına marka kararı
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
  → `docs/frontend.md` § 4.1 *(mevcut kural; bu spec onu bozmaz)*.
- **R2.** Token katmanının **varsayılan değerleri** mockup'ın görsel dilini verir: beyaz
  zemin, yüzey grisi, koyu metin, limon dolgu vurgusu ve koyu zeytin metin vurgusu.
- **R3.** **Dolgu vurgusu ile metin vurgusu ayrı token'lardır.** Dolgu olarak kullanılan
  renk metin rengi olarak kullanılamaz.
- **R4.** Tipografi token'ları **iki yığın** taşır: Latin metin için bir yığın, **Arapça
  metin için ayrı bir yığın**. Arapça sayfada Arapça yığını kullanılır.
- **R5.** Bir kurulumun renk ve tipografi değerleri **dağıtım yapılandırmasından** gelir ve
  arayüz kodunda değişiklik gerektirmez.
- **R6.** Yapılandırmada değer verilmemiş her token **varsayılanıyla** çalışır; eksik
  yapılandırma arayüzü bozmaz.
- **R7.** Yapılandırmadan gelen renkler **kontrast eşiğini** karşılamak zorundadır. Eşiği
  karşılamayan bir yapılandırmayla uygulama **ayağa kalkmaz** ve nedenini söyler.
- **R8.** Galerinin **adı ve logosu** tema değil **veridir**: galeri profilinden gelir.
  → `0003` R1.
- **R9.** Renk **tek başına anlam taşımaz**: durum, hata ve seçili olma hâli renkten
  bağımsız olarak metin veya biçimle de anlatılır. → `docs/frontend.md` § 7.
- **R10.** Mevcut tüm ekranlar — vitrin listesi, ilan detayı, panel girişi, panel personel
  ekranları — yeni token değerleriyle çalışır; hiçbirinde okunamayan metin veya kaybolmuş
  kenarlık kalmaz.
- **R11.** Arayüz, odak halkası görünürlüğünü yeni palette de korur. → `docs/frontend.md` § 7.

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
- **C5.** Yapılandırma eksik veya geçersizken **sessizce varsayılana düşülmez**:
  R6 eksik değeri, R7 geçersiz değeri tanımlar ve ikisi farklı davranır.
- **C6.** Token **adları anlamsaldır** (`--color-accent-fill`), değer temelli değil
  (`--color-lemon`). Marka değiştiğinde ad değişmez.
- **C7.** Metin kontrastı en az **4.5:1**, 24px ve üstü metinde **3:1**.
  → `docs/frontend.md` § 7.
- **C8.** Yazı tipi dosyaları veya `<link>`'leri **tek yerden** gelir; her bileşen kendi
  fontunu yüklemez.
- **C9.** Bu spec hiçbir ekranın **düzenini** değiştirmez. → Karar **D5**.
- **C10.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler **görsel kanıt**
  taşır. → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 3, `docs/frontend.md` § 9.

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
- **X7.** **Yazı tipi dosyalarının kendi kendine barındırılması** — *neden:* ayrı bir
  performans ve lisans kararıdır; V1'de yüklenen yığın yapılandırmadan gelir.
- **X8.** **Pazarlama/marka kılavuzu üretimi** (logo kullanım kuralları, ton) — *neden:*
  ürün işi değil.

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
- **Marka içeriği zaten veri.** `Gallery.LogoMediaAssetId` ve `SetLogo` mevcut
  (`src/Modules/Dealership/Dealership/Domain/Gallery.cs`); galeri adı da profilde. Yani
  eksik olan yalnızca renk ve tipografi. → Karar **D6**.
- **Yapılandırma eksikse ayağa kalkmama deseni kurulu.** `0009` C8 sağlayıcı kimlik
  bilgileri için aynı kuralı koymuş: *"yapılandırmadan gelir ve eksikse uygulama ayağa
  kalkmaz."* R7 bu deseni kontrast için kullanır, yeni bir şey icat etmez.
- **Düzen farkı büyük ama bu spec'in dışında.** Mockup 1440px kolon, 999px pill'ler,
  16–22px kart köşeleri ve hero bölümü kullanıyor; bugünkü `--layout-max-inline-size`
  1200px. X1 bunu kapsam dışı bırakıyor.

### Bilinen tuzaklar

1. **Tek vurgu token'ı.** `--color-accent`'i limon yapmak, birinin onu metin rengi olarak
   kullanmasına ve 4.5:1 eşiğinin sessizce kırılmasına yol açar. Tasarımcı bu tuzağı
   kendisi işaretlemiş; D2 onu token katmanına taşır.
2. **Değer temelli token adı.** `--color-lemon` yazılırsa, müşteri rengi mavi olduğu gün ad
   yalan söyler. C6 bunu yasaklar.
3. **Arapça fontu Latin yığınına bırakmak.** Space Grotesk'te Arapça karakter yoktur;
   tarayıcı sessizce başka bir fonta düşer ve Arapça sayfa tipografik olarak bozulur.
4. **Eksik yapılandırmayı geçersiz yapılandırmayla aynı saymak.** Eksik değer varsayılana
   düşer (R6), geçersiz değer uygulamayı durdurur (R7). İkisini birleştirmek, yanlış
   kontrastlı bir kurulumun sessizce açılmasına yol açar. C5 bunu ayırır.
5. **Token geçişinde "değer doğru, anlam yanlış" hatası.** `frontend-001`'in
   SCORECARD dersi aynen bu: *"köşe token'ı boşluk olarak, boşluk token'ı köşe olarak,
   kenarlık token'ı ölçü olarak kullanılmıştı; `font-family` ise hiç bağlanmamıştı."*
   Bu spec aynı dosyaya dokunuyor; aynı hata biçimi yine mümkündür.
6. **Odak halkasının yeni palette kaybolması.** Limon zemin üzerinde odak halkası
   görünmezse klavye kullanıcısı nerede olduğunu yitirir (R11).

### Önerilen kararlar — ONAY BEKLİYOR

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

**D4 — Varsayılan tema mockup'ın paletidir.**
*Gerekçe:* kutudan çıkınca ürün mockup gibi görünür; yeni kurulum renk vermeden çalışır (R6).
*Bedel:* yok.

**D5 — Düzen bu spec'te değişmez.**
*Gerekçe:* ADR 0003 unsur 1. Mockup'ın düzeni mevcut ekranlardan belirgin biçimde farklı
(1440 kolon, hero, pill'ler) ve bu, tamamlanmış spec'lerin kabul kriterlerine dokunur.
Düzen işi ekran başına ve ADR 0003 unsur 7'nin sırasına göre yapılır.
*Bilerek ödenen bedel:* bu spec bittiğinde arayüz mockup'ın **rengini** taşır ama
**düzenini** taşımaz; mockup'ı gören biri yarı yol görür.

**D6 — Marka içeriği veridir, tema değildir.**
Galeri adı ve logosu profilden gelir (`0003` R1, `Gallery.LogoMediaAssetId`); renk ve
tipografi yapılandırmadan. Sınır budur ve karıştırılmaz.
*Gerekçe:* ad ve logo galeri başına **içerik**, renk ise **sunum**; ikincisi kontrast
doğrulaması gerektirir, birincisi gerektirmez.

**D7 — Arapça için ayrı tipografi yığını token olarak tanımlanır.**
*Gerekçe:* tasarımcı notu `s4` ve `docs/architecture.md` § 3. *Bedel:* iki font yığını
yüklenir; Arapça olmayan kurulumda da Arapça yığını tanımlı kalır (yüklenmesi locale'e
bağlanır).

---

## Acceptance Criteria

**Token katmanı**

- **AC-1.** Dolgu vurgusu ve metin vurgusu ayrı iki token olarak tanımlıdır.
- **AC-2.** `frontend/src` altındaki hiçbir CSS dosyasında ham hex renk, ham piksel veya ham
  font boyutu bulunmaz; lint ihlali kırar.
- **AC-3.** Token adlarının hiçbiri renk adı içermez (`lemon`, `blue` gibi); adlar anlamsaldır.
- **AC-4.** Tipografi token'ları Latin ve Arapça için iki ayrı yığın tanımlar.
- **AC-5.** `[görsel]` Varsayılan yapılandırmayla açılan vitrin listesi mockup'ın renk
  dilini taşır: beyaz zemin, yüzey grisi kart alanı, limon dolgu, koyu zeytin fiyat metni.

**Kuruluma özel tema**

- **AC-6.** Yapılandırmada dolgu vurgusu farklı bir renge ayarlandığında arayüz o rengi
  kullanır ve **hiçbir bileşen dosyası değişmez**.
- **AC-7.** Yapılandırmada hiçbir tema değeri verilmediğinde arayüz varsayılan temayla
  çalışır ve hiçbir renk boş kalmaz.
- **AC-8.** Yapılandırmada yalnızca bir token verildiğinde geri kalanlar varsayılanıyla
  çalışır.
- **AC-9.** Tema değerleri mevcut `index.html` enjeksiyon yolundan gelir; ikinci bir
  mekanizma eklenmez.
- **AC-10.** Tema değerleri arayüz paketinin içine gömülmez; aynı paket iki farklı tema
  yapılandırmasıyla iki farklı görünüm verir.

**Kontrast kapısı**

- **AC-11.** Metin vurgusu, üzerine yazıldığı zemine karşı 4.5:1 kontrastı sağlamıyorsa
  uygulama ayağa kalkmaz.
- **AC-12.** Uygulama bu nedenle ayağa kalkmadığında hangi token çiftinin eşiği
  karşılamadığını söyler.
- **AC-13.** Kontrast eşiğini karşılayan bir yapılandırmayla uygulama normal açılır.
- **AC-14.** Dolgu vurgusu, kontrast eşiğine **metin rengi olarak** tabi tutulmaz; dolgu
  olarak kullanıldığı için ayrı değerlendirilir.

**Mevcut ekranlar**

- **AC-15.** `[görsel]` Vitrin listesi yeni token değerleriyle açılır; kart, rozet,
  daraltma çubuğu ve sayfalama okunur durumdadır.
- **AC-16.** `[görsel]` İlan detayı yeni token değerleriyle açılır.
- **AC-17.** `[görsel]` Panel girişi yeni token değerleriyle açılır.
- **AC-18.** `[görsel]` Panel personel listesi yeni token değerleriyle açılır.
- **AC-19.** `[görsel]` Boş durum ve hata durumu ekranları yeni palette okunur durumdadır.
- **AC-20.** Hiçbir ekranda yalnızca renkle anlatılan bir durum kalmaz; seçili, hatalı ve
  rezerve hâlleri metin veya biçimle de anlatılır.

**Erişilebilirlik ve dil**

- **AC-21.** `[görsel]` Klavyeyle gezinirken odak halkası, limon dolgu üzerinde de görünür.
- **AC-22.** `[görsel]` Arapça locale'de metin Arapça tipografi yığınıyla basılır; Latin
  yığınına düşmez.
- **AC-23.** `[görsel]` Arapça locale'de düzen `dir="rtl"` ile çalışmaya devam eder ve
  tema değişimi yönü bozmaz.
- **AC-24.** Latin marka adları ve fiyatlar Arapça metin içinde doğru yönde görünür.

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] `[görsel]` işaretli her kriterin kanıtı eklendi (`docs/evidence/frontend-002-ac<n>.png`),
      boş ve hata durumları dâhil, Arapça görüntüler de var.
- [ ] İki farklı tema yapılandırmasıyla alınmış **aynı ekranın iki görüntüsü** eklendi (AC-10).
- [ ] Kontrast kapısının ayağa kalkmayı engellediği testle gösterildi (AC-11, AC-12).
- [ ] Lint kapısı ham hex/px ihlalinde kırıyor (AC-2).
- [ ] `frontend-001`'in SCORECARD dersi gözetildi: token'lar **anlamına göre** bağlandı,
      mekanik değiştirme yapılmadı; `font-family` bağlantısı doğrulandı.
- [ ] Hiçbir ekranın düzeni değişmedi (C9) — düzen farkı varsa PR'da **açıkça** yazıldı.
- [ ] Çeviri anahtarı eklendiyse TR/AR/EN üçünde de var; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi yok.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi (D1, D3).
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra spec kaç kez değişti? Yüksekse spec eksik yazılmıştır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. |
| **Bulgu gerçek/gürültü oranı** | / | Gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
