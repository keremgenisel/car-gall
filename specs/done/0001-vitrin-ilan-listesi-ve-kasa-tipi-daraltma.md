# 0001 — Vitrin İlan Listesi ve Kasa Tipine Göre Daraltma

| | |
|---|---|
| **Durum** | **Tamamlandı** |
| **Sahip** | Developer oturumu (ajan üye) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (vitrin okuma tarafı) · `VehicleReference` (kasa tipi kanonik kümesi ve locale etiketleri — minimal) · `Dealership.Contracts` + `Media.Contracts` (yalnız olay sözleşmeleri) · `frontend/` public vitrin |
| **Bağımlı spec'ler** | yok — bu ilk spec'tir |

---

## Intent

Vitrini gezen **ziyaretçi**, yayındaki ilanları bugün tek bir uzun liste hâlinde görüyor ve
ilgilenmediği araçları gözle ayıklamak zorunda kalıyor; aradığı türdeki aracı bulamadan
sayfayı terk ediyor. **Galeri** bunun düzelmesini istiyor, çünkü vitrinin tek ticari çıktısı
**talep**tir ve doğru araca daha erken ulaşan ziyaretçi daha çok talep bırakır.

Bu iş bittiğinde ziyaretçi, vitrine girdiğinde yayındaki ilanları sayfalanmış bir listede
görür, **kasa tipine** göre bir veya birden çok seçim yaparak listeyi daraltır ve yalnızca
o seçime uyan ilanları görür. Listenin o anki hâli paylaşılabilir bir bağlantıdır ve
tarayıcının geri tuşu bir önceki hâle döner.

Başarı üç gözlemlenebilir kalemdir: daraltma doğru ilan kümesini gösterir; liste üç dilde de
(TR/AR/EN, Arapça'da tam RTL) aynı şekilde çalışır; ve liste, hedeflenen birkaç yüz ilanlık
vitrinde C3'te sabitlenen yanıt süresi eşiklerinin altında kalır.

---

## Requirements

- **R1.** Sistem, vitrini ziyaret eden kişiye yayındaki ilanların sayfalanmış bir listesini
  gösterir. Liste, kimlik doğrulaması olmadan erişilebilir.
- **R2.** Liste yalnızca `Yayında` ve `Rezerve` yayın durumundaki ilanları içerir.
  `Rezerve` ilanlar listede **rezerve rozetiyle** görünür.
- **R3.** Ziyaretçi listeyi **kasa tipine** göre daraltabilir. Birden çok kasa tipi aynı anda
  seçilebilir; liste, seçilenlerden **herhangi birine** uyan ilanları gösterir.
- **R4.** Seçili kasa tipi sayısı ve seçimi temizleme eylemi ziyaretçiye her zaman görünür.
- **R5.** Ziyaretçi listenin o anki hâlini (daraltma + sayfa) bağlantı olarak paylaşabilir;
  bağlantıyı açan kişi aynı daraltma ve aynı sayfayı görür. Tarayıcının geri tuşu bir önceki
  daraltma/sayfa hâline döner.
  **Geçmiş kaydı tanesi:** daraltma seçimindeki her değişiklik geçmişe **tek** bir kayıt
  ekler; daraltma değiştiğinde yapılan sayfa sıfırlaması **ayrı bir kayıt üretmez**.
  Sayfa değişimi de tek kayıttır.
- **R6.** Sayfa başına **24** ilan gösterilir. Liste yanıtı, uygulanan daraltmaya uyan
  **toplam ilan sayısını** taşır; sayfa sayısı ve sonuç sayısı metni bu toplamdan üretilir.
- **R7.** Liste sabit bir sıralamayla gelir: **vitrin sıralama ağırlığı**, eşitlik hâlinde
  **en yeni yayınlanan**, o da eşitse **ilan kimliği küçük olan** önce. Ziyaretçi sıralamayı
  değiştiremez.
- **R8.** Bu spec'te daraltmanın **tek ekseni kasa tipidir.** Alan ayarları eksen **üretmez**;
  yalnızca kasa tipi ekseninin **görünürlük kapısıdır**: kasa tipi alanı açık **ve** public
  değilse daraltma çubuğu hiç gösterilmez. Alan ayarlarından eksen türetmenin genel kuralı
  bu spec'te değil, çok eksenli spec'tedir. → X3
- **R9.** Her ilan kartı şunları taşır: kapak görseli, model adı, yıl, kilometre,
  fiyat **veya** "fiyat sorunuz", şube adı. Liste yanıtı bu alanların **dışında hiçbir alan
  taşımaz**; opsiyonel alanlar liste yanıtında bulunmaz (detay sayfası kapsam dışıdır → X9).
  *(Adres alanı `slug` sonradan eklendi: karttan detaya gidiş onsuz kurulamıyordu.
  → `specs/0002-arac-detay.md` **E5**. Bu ek, R9'un "opsiyonel alan taşımaz" kuralını
  değiştirmez.)*
- **R10.** Liste yanıtı hiçbir gizli alanı taşımaz — şase, plaka, iç notlar ve `Gizli`
  işaretli opsiyonel alanlar yanıtta **hiç bulunmaz**, boş değer olarak da gelmez.
- **R11.** Liste ekranı **yükleniyor**, **boş** ve **hata** durumlarının üçünü de ele alır.
- **R12.** İki ayrı boş durum ayırt edilir ve farklı eylem sunar:
  (a) daraltma sonucu boş, (b) vitrinde hiç yayında ilan yok. İkisi aynı anda doğruysa
  **(b) kazanır.**
- **R13.** Liste, etkin üç locale'in (TR/AR/EN) hepsinde çalışır; Arapça'da yön ve
  metin biçimlendirmesi RTL'dir.

---

## Constraints

### Uyulması zorunlu

- **C1.** Fiyat `Money` ile taşınır (tutar + ISO 4217 kodu + minor unit üssü); çıplak
  `decimal` taşınmaz, `double`/`float` kullanılmaz. Üç ondalıklı para birimleri
  (KWD/BHD/JOD/OMR) doğru gösterilir. → `AGENTS.md` Altın Kural 4, `docs/domain.md` **P1–P3**,
  Yasak Listesi 26.
- **C2.** "Fiyat sorunuz" ayrı bir durumdur, `0` değildir. → `docs/domain.md` **P4**.
- **C3.** Performans eşikleri: **500 yayındaki ilan** içeren veri setinde, daraltılmış liste
  için sunucu yanıtı **p95 < 400 ms** (ağ hariç); mobilde liste görünümü için
  **LCP < 2,5 s**.
  **Ölçüm protokolü** — CI'da, her koşuda aynı biçimde uygulanır:
  - *Sunucu:* 500 ilanlık sabit veri seti, iki kasa tipiyle daraltılmış istek;
    **20 istek ısınma + 200 istek ölçüm**, eşzamanlılık **1**; p95 bu 200 istek üzerinden
    hesaplanır. Toplam sayı sorgusu (R6) ölçüme **dâhildir**.
  - *Uç:* Lighthouse **mobil profili** (4× CPU kısıtı, Slow 4G), **3 koşu**, **medyan** alınır.

  Hedeflenen vitrin büyüklüğü **birkaç yüz ilandır**; bu sınır aşılırsa
  `docs/architecture.md` § 6'daki ayrı arama modülü tetikleyicisi yeniden değerlendirilir.
- **C4.** Kullanıcıya görünen hiçbir metin koda gömülmez; API hata kodu döner, çeviri uçta
  yapılır. → `AGENTS.md` Altın Kural 6, Yasak Listesi 23–24.
- **C5.** Sayı içeren cümleler dize birleştirmeyle kurulmaz; ICU mesaj formatı zorunludur
  (Arapça'da altı çoğul biçimi vardır). → Yasak Listesi 31, `docs/architecture.md` § 3.
- **C6.** CSS'te fiziksel yön özellikleri kullanılmaz; logical properties zorunludur.
  → Yasak Listesi 25, `docs/frontend.md`.
- **C7.** Locale öneki opsiyonel değildir ve `Accept-Language`'a göre otomatik yönlendirme
  yapılmaz. Öneksiz `/` isteği, kurulum yapılandırmasındaki **varsayılan locale'e `301` ile**
  yönlendirilir (yönlendirme `Accept-Language`'dan değil, yapılandırmadan gelir). Etkin
  olmayan bir locale öneki (`/de/`) **`404`** döner ve varsayılan locale'e yönlendirilmez.
  → Yasak Listesi 28–29.
- **C8.** Daraltma ve sayfalama durumu URL'de yaşar; sunucu verisi global store'a
  kopyalanmaz. URL'nin kanonik biçimi **C13**'tedir. → `docs/frontend.md` § 5.
- **C9.** Modül sınırı: liste yalnızca `VehicleCatalog`'un kendi vitrin okuma modelinden
  beslenir. Şube adı vitrin okuma modelinde denormalize tutulabilir, ancak **yalnızca
  `Dealership` olaylarından beslenerek** ve o kopya düzenlenmeden. → Yasak Listesi 1, 33.
- **C10.** Gizleme `VehicleCatalog`'un sorgu tarafında uygulanır, sunum katmanında değil.
  → Yasak Listesi 41, `docs/architecture.md` § 4.
- **C11.** Metin üzerinde `ILIKE`/`lower()` ile eşleştirme yapılmaz (Türkçe `i/İ/ı/I` tuzağı).
  → Yasak Listesi 27, `docs/architecture.md` § 3.
- **C12.** Her kabul kriteri en az bir testle karşılanır; test `DisplayName`'i
  `AC-<n>: <kriterin metni>` biçimindedir ve metin bu spec'ten **kopyalanır**.
  → `AGENTS.md` Altın Kural 5, `docs/testing.md` § 1.
- **C13.** **URL sözleşmesi.** Daraltma ve sayfa durumu aşağıdaki biçimde taşınır:

  | Öğe | Karar |
  |---|---|
  | Rota | `/<locale>/<liste rotası>` — locale öneki zorunlu (C7) |
  | Kasa tipi parametresi | `bodyType` — locale'den **bağımsız**, İngilizce ad |
  | Çoklu seçim | Tekrarlı parametre: `?bodyType=sedan&bodyType=suv`. Virgülle ayrılmış liste **kullanılmaz** |
  | Değer biçimi | Küçük harfli ASCII slug (`sedan`, `suv`, `hatchback`); kanonik küme kasa tipi alanının değer kümesinden gelir |
  | Eşleştirme | **Tam ve harfe duyarlı.** `?bodyType=SUV` tanınmayan değerdir. `ILIKE`/`lower()` kullanılmaz (C11) |
  | Sayfa parametresi | `page` — 1 tabanlı |
  | Kanonik biçim | `page=1` URL'ye yazılmaz; boş `bodyType` parametresi yazılmaz |

- **C14.** Ziyaretçiden gelen **geçersiz daraltma veya sayfa girdisi hata değildir**: sunucu
  her zaman `200` ve normalize edilmiş listeyi döner, hata kodu dönmez. Uç, adres çubuğunu
  C13'teki kanonik biçime `replaceState` ile düzeltir; bu düzeltme **geçmişe yeni kayıt
  eklemez**. Geçersiz **locale öneki** bunun istisnasıdır → C7.

### KAPSAM DIŞI

- **X1.** Serbest metin arama kutusu — *neden:* daraltma arama değildir; arama kendi
  normalizasyon yükünü getirir (TR `i/İ/ı/I`, AR hemze/ta marbuta/elif maksura, locale başına
  FTS konfigürasyonu — `docs/architecture.md` § 3) ve tek başına bir spec'tir.
- **X2.** Fiyat / yıl / kilometre aralık filtreleri — *neden:* aralık filtresi "fiyat
  sorunuz" durumunu (**P4**) ayrıca ele almayı gerektirir; ayrı kriter kümesidir.
- **X3.** Markaya, şubeye, yakıta, vitese veya başka bir eksene göre daraltma — *neden:*
  bu spec tek eksende (kasa tipi) doğrulanır. Kasa tipi çekirdek alandır, **A2** gereği
  yayındaki her araçta doludur; "eksensiz ilan" kenar durumu böylece hiç doğmaz.
  **Alan ayarlarından eksen türetmenin genel kuralı da bu spec'te değil, çok eksenli
  spec'tedir** (→ R8); burada alan ayarları yalnızca görünürlük kapısıdır.
- **X4.** Ziyaretçinin seçebildiği sıralama ("fiyata göre sırala") — *neden:* fiyatsız
  araçların sıralamadaki yerini (**P4**) ve locale collation'ını gündeme getirir; fiyat
  filtresiyle aynı spec'te ele alınması daha ucuzdur.
- **X5.** Favori, karşılaştırma, kaydedilmiş arama — *neden:* public üyelik V1 kapsam
  dışıdır; favoriler `localStorage` kararı zaten verilmiştir (`docs/architecture.md` § 6).
- **X6.** Yönetim panelindeki araç listesi ve filtreleri — *neden:* bu spec **vitrin**
  tarafıdır. Panel listesi farklı izin modeli ve farklı alan görünürlüğü demektir.
- **X7.** Sonsuz kaydırma — *neden:* geri tuşu, paylaşılabilir URL ve locale başına
  sitemap/SEO hedefleriyle (`docs/architecture.md` § 3) sayfa numarası uyumludur.
- **X8.** `Satıldı` ilanların "geçmiş satışlarımız" olarak gösterilmesi — *neden:* **A4**
  uyarınca `Satıldı` terminaldir; ulaşılamayacak aracı aynı listede göstermek ziyaretçiyi
  yanıltır. İstenirse ayrı bir spec olur.
- **X9.** İlan detay sayfası ve talep formu — *neden:* bu spec listeleme ve daraltmadır.
  Karttan detaya gidiş bağlantısı vardır; detay sayfasının kendisi bu spec'te değildir.
- **X10.** **Vitrin okuma modelini besleyen yazma yolu ve olay dağıtımı** — araç/ilan
  agregatlarının kalıcılaştırılması, outbox dağıtıcısı ve entegrasyon olayı işleyicilerinin
  DI'a bağlanması. *neden:* bu spec vitrin **okuma** tarafıdır; panel X6 ile zaten kapsam
  dışıdır ve yazma yolu olmadan okuma tarafının tüm kriterleri doğrulanabilir. Yansıtıcı
  (`ShowcaseListingProjector`) ve olay işleyicileri yazılmış ve testlerle kanıtlanmıştır;
  **bağlanmamıştır.**
  **Doğrudan sonucu:** bu spec tek başına merge edildiğinde vitrin okuma modelini üretimde
  dolduran bir yol yoktur — tablo yalnızca geliştirme seed'iyle dolar. 0001 tek başına
  çalışan bir ürün parçası değildir ve bunu takip eden spec tamamlar.
  *(Güncelleme: olay dağıtımı — outbox — ve şube dinleyicilerinin DI'a bağlanması
  `specs/0003-galeri-profili-ve-subeler.md`'ye devredildi. Araç yazma yolu hâlâ kapsam
  dışıdır ve kendi spec'ini bekler.)*
  *(QA D6 bulgusu; Takım Yöneticisi onayıyla eklendi.)*

---

## Context

**Mevcut davranış.** Depoda henüz uygulama kodu yoktur; bu, projenin ilk spec'idir
(`specs/` altında `TEMPLATE.md` dışında dosya, `specs/done/` altında `README.md` dışında
dosya yoktur). Dolayısıyla "mevcut davranış" yoktur — bu iş vitrin listesini **sıfırdan**
kurar. Bu spec yazılırken kod okunmamıştır; okunacak kod yoktur.

**Geçerli domain kuralları** (`docs/domain.md`):
- **A2** — yayına alınma koşulları: çekirdek alanlar dolu, fiyat belirlenmiş **veya**
  "fiyat sorunuz" işaretli, en az bir medya varlığı hazır, zorunlu opsiyonel alanlar dolu.
  Kasa tipi çekirdek alandır; yayındaki her araçta doludur.
- **A3 / A4** — yayın durumu geçişleri ve `Satıldı`'nın terminal oluşu.
- **A8** — şube kapatıldığında o şubedeki araçlar yayından kalkar ve "lokasyon ataması
  bekliyor" durumuna geçer; **sessizce vitrinde kalmazlar.**
- **R6** — `Rezerve` araç vitrinde görünmeye devam eder, "rezerve" rozetiyle ve iletişim
  formu kapalı olarak. *(Gerekçe: rezervasyon düşerse ilgi kaybedilmez.)*
- **F3** — `Gizli` işaretli alan public yanıtta **hiç bulunmaz** (null olarak da gelmez).
- **F4** — çekirdek alanlar kapatılamaz; şase, plaka ve iç notlar her zaman gizlidir.
- **P1–P4** — para `decimal` ve `Money`; KWD/BHD/JOD/OMR üç ondalıklı; para birimi kurulum
  başına sabit; "fiyat sorunuz" ≠ `0`.

**Daha önce verilmiş kararlar** (`docs/architecture.md` § 6–7, `docs/decisions/0001`):
- Ayrı `Discovery` (arama) modülü **yok**; birkaç yüz araçta PostgreSQL FTS yeterli görüldü.
- Public üyelik / hesap sistemi **yok**.
- Tam SSR **yok**; host `index.html` servis ederken rota bazlı meta ve JSON-LD enjekte eder.
- `VehicleCatalog` tek modüldür; içinde `Vehicle` + `Listing` agregatları ve **vitrin okuma
  modeli (denormalize)** bulunur. "Vitrin sıralama ağırlığı" bu modülde zaten tanımlı bir
  kavramdır (`docs/architecture.md` § 1).
- Alan ayarları `VehicleCatalog` içinde tekil bir agregattır; frontend formu **ve filtre
  çubuğunu** alan ayarları yanıtından kurar. **Filtre üçüncü eksen değildir, türetilir.**
  *(Bu spec bu kararı değiştirmez; tek eksenli kapsamda uygulamasını R8 ile sınırlar.)*

**Bilinen tuzaklar:**
1. **Daraltma ile sayfa numarasının etkileşimi.** Daraltma değiştiğinde eski sayfa numarası
   korunursa ziyaretçi var olmayan bir sayfaya düşer ve boş liste görür. → AC-8.
2. **İki boş durumun karıştırılması.** "Sonuç yok" metni, vitrinde hiç ilan olmayan galeride
   ziyaretçiye olmayan bir düğmeyi işaret eder. → R12, AC-26, AC-27.
3. **RTL'de liste ekranı.** Izgara akış yönü, sayfalama okları ve daraltma rozetlerinin
   hizası en sık kırılan yerlerdir; fiziksel yön özellikleri (C6) buranın kaynağıdır.
   → AC-30.
4. **Arapça çoğul.** "12 ilan bulundu" gibi sayı içeren metinler altı çoğul biçimi gerektirir;
   dize birleştirme sessizce yanlış cümle üretir (C5). → AC-31.
5. **Üç ondalıklı para birimi.** KWD fiyatı iki ondalıkla gösterilirse hata sessizdir ve
   yalnızca gözle fark edilir (C1). → AC-22.
6. **Kararsız sıralama.** Ağırlık ve yayın tarihi eşit olan ilanlarda üçüncü bir kırıcı
   yoksa aynı ilan iki sayfada birden görünebilir veya hiç görünmeyebilir; testi kararsız
   kılar (AP-03). → R7, AC-5.

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler **arayüz kriteridir** ve `docs/testing.md` § 3 uyarınca
> görsel kanıt gerektirir.

**Listeleme ve yayın durumu süzgeci**

- **AC-1.** Vitrinde 30 adet `Yayında` ilan varken liste ilk sayfada 24 ilan gösterir.
- **AC-2.** `[görsel]` `Rezerve` durumundaki bir ilan listede görünür ve üzerinde rezerve
  rozeti bulunur.
- **AC-3.** `Satıldı`, `Taslak`, `Arşiv` durumundaki ve "lokasyon ataması bekliyor"
  durumundaki ilanlar listede yer almaz.
- **AC-4.** Liste, vitrin sıralama ağırlığı azalan sırada gelir; ağırlığı eşit iki ilandan
  yayın tarihi daha yeni olan önce gelir; ağırlığı ve yayın tarihi de eşit iki ilandan
  ilan kimliği küçük olan önce gelir.
- **AC-5.** Vitrin sıralama ağırlığı ve yayın tarihi aynı olan 30 ilan varken birinci ve
  ikinci sayfa birlikte 30 farklı ilan içerir; tekrar eden veya kaybolan ilan yoktur.

**Kasa tipine göre daraltma**

- **AC-6.** Tek bir kasa tipi seçildiğinde liste yalnızca o kasa tipindeki yayında ilanları
  gösterir.
- **AC-7.** İki kasa tipi seçildiğinde liste, bu iki kasa tipinden herhangi birine uyan
  yayında ilanları gösterir.
- **AC-8.** İkinci sayfadayken bir kasa tipi seçildiğinde liste ilk sayfadan başlar.
- **AC-9.** Seçim temizlendiğinde liste, daraltma öncesindeki tüm yayında ilan kümesine döner.
- **AC-10.** `[görsel]` Seçili kasa tipi sayısı ekranda görünür; seçimi temizle eylemi
  klavyeyle odaklanabilir ve üç locale'de erişilebilir adı vardır.
- **AC-11.** Daraltma çubuğu yalnızca kasa tipi eksenini sunar; alan ayarları yanıtındaki
  başka hiçbir alan eksen olarak sunulmaz.
- **AC-12.** `[görsel]` Kasa tipi alanı alan ayarlarında public değilken daraltma
  çubuğu hiç gösterilmez; liste daraltmasız çalışmaya devam eder.
  *(Kriterin "kapalı" yarısı çıkarıldı: kasa tipi çekirdek alandır ve `docs/domain.md` F4
  uyarınca kapatılamaz — var olmayan bir durum test edilemez. Kapı kodu yine iki bayrağı
  da okur, yalnız kanıt public görünürlük yolundan üretilir. R8 değişmedi.)*

**Sayfalama**

- **AC-13.** Vitrinde 49 yayında ilan varken liste 3 sayfa üretir ve üçüncü sayfa 1 ilan
  gösterir.
- **AC-14.** Son sayfanın ötesinde bir sayfa istendiğinde liste ilk sayfayı gösterir;
  ziyaretçiye hata metni gösterilmez ve adres çubuğu C13'teki kanonik biçime düzeltilir.
- **AC-15.** Son sayfanın ötesinde bir sayfa istendiğinde API `200` ve ilk sayfanın içeriğini
  döner; `4xx` dönmez.
- **AC-16.** Sayfa numarası olarak `0`, negatif bir sayı veya sayı olmayan bir değer
  istendiğinde liste ilk sayfayı gösterir, API `200` döner ve ziyaretçiye hata metni
  gösterilmez.
- **AC-17.** Tanınmayan bir kasa tipi değeri istendiğinde o değer yok sayılır; tanınan
  değerler varsa onlarla daraltma yapılır, hiç tanınan değer yoksa daraltmasız liste
  gösterilir. Eşleştirme harfe duyarlıdır: `?bodyType=SUV` tanınmayan değerdir ve
  `?bodyType=SUV&bodyType=sedan` isteği yalnızca `sedan` ile daraltır.

**Paylaşılabilirlik**

- **AC-18.** Daraltılmış ve ikinci sayfadaki bir listenin bağlantısı yeni bir oturumda
  açıldığında aynı daraltma ve aynı sayfa görünür.
- **AC-19.** İkinci sayfadayken bir kasa tipi seçildikten sonra tarayıcının geri tuşu **tek
  hareketle** daraltmasız ikinci sayfa hâline döner; ara bir geçmiş kaydına uğranmaz.

**İlan kartı ve alan görünürlüğü**

- **AC-20.** `[görsel]` İlan kartı kapak görselini, model adını, yılı, kilometreyi, fiyatı ve
  şube adını gösterir. Kilometre locale'e göre gruplama ayracıyla biçimlenir (TR `125.000`,
  EN `125,000`); yıl hiçbir locale'de gruplama ayracı almaz (`2019`).
- **AC-21.** `[görsel]` "Fiyat sorunuz" işaretli ilanın kartında tutar yerine "fiyat sorunuz"
  karşılığı gösterilir; `0` veya boş tutar gösterilmez.
- **AC-22.** `[görsel]` Kurulum para birimi KWD iken `1250,500` tutarlı ilanın kartında tutar
  **üç ondalıkla** gösterilir; yuvarlama veya kesme yapılmaz. Beklenen tutar dizgesi:
  TR `1.250,500` · EN `1,250.500` · AR `1,250.500`. Üç locale'de de **Batı Arap rakamları**
  kullanılır; Hint-Arap rakamı (`١٢٥٠`) kullanılmaz. Para birimi simgesinin yeri locale
  verisinden gelir.
- **AC-23.** Liste yanıtı şase, plaka ve iç notlar alanlarını **hiç içermez**; bu alanlar
  boş değer olarak da bulunmaz.
- **AC-24.** `Gizli` işaretli bir opsiyonel alan liste yanıtında **hiç bulunmaz** — boş değer
  olarak da bulunmaz — ve daraltma ekseni olarak da sunulmaz; gizleme `VehicleCatalog`'un
  sorgu tarafında uygulanır (C10).

**Üç durum** (`docs/frontend.md` § 3)

- **AC-25.** `[görsel]` Liste verisi yüklenirken ekranda iskelet gösterilir; C3'teki uç ölçüm
  koşusunda CLS değeri **0,1'in altındadır**.
- **AC-26.** `[görsel]` Vitrinde en az bir yayında ilan varken daraltma sonucu hiçbir ilan
  kalmadığında ekran, seçime uyan ilan olmadığını bildirir ve **seçimi temizle** eylemini
  sunar.
- **AC-27.** `[görsel]` Vitrinde hiç yayında ilan yokken — daraltma seçili olsun ya da
  olmasın — ekran, şu anda yayında araç bulunmadığını bildirir ve galeriye ulaşma eylemini
  sunar; seçimi temizle eylemi gösterilmez.
- **AC-28.** `[görsel]` Liste verisi alınamadığında ekran, çeviri kataloğundaki hata mesajı
  anahtarının karşılığını ve **tekrar dene** eylemini gösterir; HTTP durum kodu, hata kodu,
  `traceId`, endpoint yolu veya istisna metni gösterilmez.

**Çok dillilik ve RTL**

- **AC-29.** `[görsel]` Daraltma çubuğundaki kasa tipi etiketleri TR, AR ve EN locale'lerinde
  kendi dilinde görünür; hiçbir locale'de çeviri anahtarı ham hâliyle görünmez.
- **AC-30.** `[görsel]` Arapça locale'de sayfa `dir="rtl"` ve `lang="ar"` ile basılır; liste
  ızgarası ve sayfalama denetimleri sağdan sola akar.
- **AC-31.** `[görsel]` Sonuç sayısını bildiren metin, daraltmaya uyan **toplam** ilan
  sayısını (R6) bildirir ve ICU mesaj formatıyla üretilir; Arapça'da 1, 2, 3, 11 ve 100
  sonuç için dilbilgisel olarak doğru çoğul biçimini kullanır.
- **AC-32.** `[görsel]` Listedeki her görselin `alt` metni, ziyaretçinin locale'inde
  "model adı + yıl" kalıbından ICU mesaj formatıyla üretilir; hiçbir locale'de çeviri
  anahtarı ham hâliyle görünmez.
- **AC-33.** Öneksiz `/` isteği kurulum varsayılan locale'ine `301` ile yönlendirilir;
  etkin olmayan bir locale öneki (`/de/`) `404` döner ve varsayılan locale'e yönlendirilmez.

**Performans**

- **AC-34.** 500 yayında ilan içeren veri setinde, iki kasa tipiyle daraltılmış liste
  isteğinin sunucu yanıt süresi, C3'teki ölçüm protokolüyle p95 **400 ms'nin altındadır**
  (ağ hariç).
- **AC-35.** Mobil görünümde liste sayfasının LCP değeri, C3'teki ölçüm protokolüyle
  **2,5 saniyenin altındadır**.

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] `[görsel]` işaretli **her** kriterin görsel kanıtı eklendi
      (`docs/evidence/0001-ac<n>.png`), boş ve hata durumları dâhil; AC-30 için Arapça
      görüntü de var.
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — modül sınırı ihlali yok.
- [x] Yeni migration varsa yalnızca `VehicleCatalog` şemasına dokunuyor.
- [x] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [x] Yeni domain terimi **yok** — spec yalnızca sözlükteki terimleri kullanır (doğrulandı).
- [x] Mimari karar verilmedi; `docs/architecture.md` § Karar Defteri değişmedi (doğrulandı).
- [x] C3'teki performans eşikleri, **C3'teki ölçüm protokolüyle** CI'da ölçülüyor ve
      raporlanıyor.
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [x] Takım Yöneticisi onayladı ve squash-merge edildi.
- [x] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **1** | Onay (`ef7051f`) sonrası tek değişiklik: X10'un eklenmesi (`0bc3191`). Yazma yolu hiçbir X maddesiyle dışlanmıyordu; "yapılmadı" ile "kapsam dışı" ayırt edilemiyordu. Kriterlerin kendisi hiç değişmedi. |
| **Düzeltme turu sayısı** | **1** | PR henüz açılmadı; tur PR öncesi QA gözden geçirmesidir. 12 bulgu, hepsi tek turda kapandı. |
| **Dış bulgu (gerçek/gürültü)** | **12 / 0** | PR öncesi **QA gözden geçirmesi**; on ikisi de tek turda kapandı. Gürültü bulgu yok. Yalnız D2'nin önerdiği NEDEN yanlıştı (katlama üstü lazy görsel); bulgunun kendisi — LCP eşiği kırmızı — gerçekti ve ölçümle kapandı. |
| **Uygulama içi bulgu** | **—** | Kayıtta ayrıca sayılmamış. Kartın yazıldığı sırada iki bulgu türü tek sütunda toplanıyordu; geriye dönük ayrıştırılabilen tek sayı QA turununki. |
| **Regresyon sayısı** | **1** | CLS düzeltmesi iskelete `.listing-card` sınıfını verince, daha önce yeşil olan e2e seçicileri iskeletle eşleşmeye başladı. Testler yakaladı, merge öncesi kapandı (`data-testid="listing-card"`). |
| **Kaçan hata** | **2** | *2026-09-22:* Şemalar yalnız `Development:SeedShowcase` bayrağıyla uygulanıyordu. Bayrak kapalı bir dağıtımda hiçbir tablo oluşmuyor, uygulama **sağlıklı görünerek** ayağa kalkıyor ve ilk ziyaretçinin ilk isteğinde "tablo yok" ile düşüyordu — `main` beş spec boyunca üretime çıkarılamaz hâldeydi ve bunu söyleyen tek bir test yoktu. ADR 0002 ile kapatıldı (#12). **·** *2026-10-15:* **C3'ün CLS eşiği aşıldı ve bunu söyleyen kapı rastgele davranıyordu.** Daraltma çubuğunun iskeleti 132px yer ayırıyordu (`--filter-bar-min-block-size`), gerçek çubuk Lighthouse mobil profilinde **238px** geliyordu ve 57. milisaniyede ızgarayı **+111px** aşağı itiyordu — tek başına CLS **0,08** (eşik 0,1). Bu kartın kapanış ölçümü `docs/evidence/0001-performance.json`de **CLS 0** diyor, yani kayma sonradan doğdu: altı kasa tipi çipi mobilde üç satır sarıyor ve ayrılan yükseklik tek bir sabit sayıyla verilmişti (ölçüldü: 412px → 238, 640px → 195, 1280px → 152; token her genişlikte eksik ayırıyordu). Kapı aynı kodda `{0,016 · 0,096 · 0,139}` savrulduğu için medyan bazen geçiyordu — #67 ve #71'de düştü, #68'de geçti. Düzeltildi: ayrılan yükseklik ölçülmüş kırılımlara bağlandı ve **gerçek çubuğa da** uygulandı; yeniden ölçüm dokuz kombinasyonda fark **0**, CLS 0,0909 → **0,0133**, resmî kapı medyanı **0,008**. |

**Bu spec'ten çıkarılan ders:**
Gözlemlenebilir sonucu yazan kriter, mekanizmayı yazan kriterden daha çok hata yakalıyor.
AC-17 "hiç tanınan değer yoksa daraltmasız liste gösterilir" dediği için, tip düzeyinde
doğru görünen bir kod (`BodyTypeSlug` biçim doğrulaması) kusurlu yakalandı: `banana`
geçerli bir slug biçimidir ama kanonik küme üyesi değildir. Aynı biçimde C3'ün ölçüm
protokolünü spec'e yazmış olmak, performans tartışmasını "sence hızlı mı" olmaktan
çıkarıp kapatılabilir bir kusura çevirdi — hem gerçek CLS kusurunu buldurdu hem de
LCP için ileri sürülen iki ayrı nedeni (görsel önceliği, ilk turda) çürüttü.
Bir sonraki spec'te kriterler yine davranışla yazılmalı; "şöyle uygulansın" diye yazılan
her kriter, uygulandığı an doğrulanmış sayılır ve kusuru gizler.
