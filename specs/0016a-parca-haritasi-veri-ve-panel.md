# 0016a — Parça Haritası: Veri ve Panel

| | |
|---|---|
| **Durum** | Onaylandı (2026-11-18 — Takım Yöneticisi, depo sahibi) — 34 kabul kriteri |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (değer tipi, kalıcılık, alan ayarı, yönetim sözleşmesi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0010a`, `0010b` (opsiyonel alan formu — tamamlandı) · **`0016b`** bu dilimin sözleşmesine dayanır |
| **Dayanak** | ADR 0003 Ek 1, sıra #10'un **birinci dilimi** (`0016` **D31**) · ADR 0007 (opsiyonel alan tipleri) |
| **Karar bekliyor** | yok — S1 → **D32** (kriter turu, 2026-11-18). Önceki kararlar `0016`'nın CLARIFY ve plan turlarından (D1–D31; bu dilime düşenler aşağıda birebir). |

> **Bölünme (2026-11-18, `0016` D31).** `0016` iki dilime ayrıldı: bu dilim parça
> haritasının **verisini ve panelini** taşır; vitrin (galeri, şema, detay sözleşmesi)
> **`0016b`**'dedir. Gereksinim ve kısıt numaraları `0016`'dakiyle **aynı** tutuldu ki
> kararlardaki atıflar bozulmasın. Tam Context ve D1–D31'in tamamı `0016b`'de durur; buraya
> yalnız bu dilimin maddeleri **birebir** kopyalandı. **13 parçanın kod tablosunun (C10) tek
> evi bu dosyadır** — `0016b` ona atıf yapar (D5'in "aynı tablo iki yerde" endişesi).
>
> Kabul kriterleri **Analist** tarafından yazılır ve uygulama dalı açılmadan `main`'e girer
> (**D24**).

---

## Intent

Parça haritasının **değeri** bugün hiçbir yerde tutulmuyor: alan kodda tanımlı değil
(`0016` D5, K1). Vitrinin şeması (`0016b`) ancak galerinin girdiği bir değeri gösterebilir.

Bu dilim bittiğinde galeri bir aracın 13 parçasının durumunu panelde girebilir, değiştirebilir
ve temizleyebilir; değer tipli ve doğrulanmış olarak saklanır. Başarı ölçütü: **galeri bir
aracın hasar durumunu yarım bırakmadan kaydedebiliyor.**

---

## Requirements

- **R8.** Parça haritası, anahtarı **`paint_change_map`** olan **tek bir opsiyonel alandır**:
  tek form durumu ve tek public görünürlük ekseni taşır. Varsayılan ayarı
  **Opsiyonel + Görünür**dür (**D5**). → `docs/architecture.md` § 4, **F1**–**F5**.
- **R9.** Harita **13 parçayı** C10'daki sabit kodlarla ve sabit sırayla tanır (**D6**).
- **R10.** Her parçanın durumu dört koddan biridir: **orijinal**, **lokal boyalı**,
  **boyalı**, **değişen** (**D1**). "Belirtilmedi" diye bir durum yoktur.
- **R11.** Değer ya **13 parçanın tamamını** taşır ya da **hiç yoktur**; yarım harita
  kaydedilemez ve reddedilir (**D7**).
- **R12.** Panelde, aracın opsiyonel alan formunda alan açıksa **13 seçim kutusu** bulunur
  (**D5**):
  - Hiçbiri önceden seçili gelmez; "orijinal" varsayılan **değildir** (**D7**).
  - Hiç seçim yapılmamışsa değer yazılmaz.
  - Bazı kutular seçilip bazıları boş bırakılırsa kayıt reddedilir ve eksik parçalar
    gösterilir.
  - Kayıtlı bir değer bütünüyle **temizlenebilir**.

---

## Constraints

### Uyulması zorunlu

- **C5.** Kullanıcıya görünen metin koda gömülmez. Bunlar çeviri kataloğundan gelir:
  - 13 parça adı ve dört durum adı;
  - R15, R17 ve R18'in cümleleri;
  - lejant;
  - R5'in `alt` şablonu.

  → Altın Kural 6.
  *(Bu dilimde: panelin 13 parça adı ve dört durum adı ile eksik parça hata metni.)*
- **C6.** Veritabanında ve yanıtta çevrilmiş parça ya da durum adı bulunmaz; **kod**
  bulunur. → Yasak Listesi 22, 23.
- **C9.** Her kabul kriteri en az bir testle karşılanır; görsel kriterler görsel kanıt taşır.
- **C10.** 13 parçanın **kodları ve sırası** sabittir (**D6**). Sıra hem okunabilir listenin
  sırası hem panel formunun sırasıdır:

  | # | Kod | Parça |
  |---|---|---|
  | 1 | `front_bumper` | Ön tampon |
  | 2 | `hood` | Motor kaputu |
  | 3 | `roof` | Tavan |
  | 4 | `trunk_lid` | Bagaj kapağı |
  | 5 | `rear_bumper` | Arka tampon |
  | 6 | `left_front_fender` | Sol ön çamurluk |
  | 7 | `left_front_door` | Sol ön kapı |
  | 8 | `left_rear_door` | Sol arka kapı |
  | 9 | `left_rear_fender` | Sol arka çamurluk |
  | 10 | `right_front_fender` | Sağ ön çamurluk |
  | 11 | `right_front_door` | Sağ ön kapı |
  | 12 | `right_rear_door` | Sağ arka kapı |
  | 13 | `right_rear_fender` | Sağ arka çamurluk |

  Durum kodları: `original`, `local_painted`, `painted`, `replaced`.
- **C11.** Parça haritası **yeni bir değer tipi**dir. ADR 0007'nin kapalı tip kümesine
  eklenir ve bu ekleme **ADR 0007'ye ek bir karar** olarak yazılır (**D5**). Tipin depolama
  biçimi plan turunun kararıdır.

### KAPSAM DIŞI

- **X2.** **Panelde tıkla-seç parça haritası editörü** (şema üzerinde parçaya tıklayarak
  durum seçme) — *neden:* veri girişi R12'nin 13 seçim kutusuyla bu spec'te yapılıyor
  (**D5**); şema üzerinde düzenleme ayrı bir etkileşim işidir (**D4**).
- **X8.** **Ağır hasar kaydı** ve **değişen parça sayısı** alanları — *neden:* ikisi de
  kodda yok. Değişen parça sayısı ileride eklenirse ayrı bir alan **olmaz**, bu haritadan
  **türetilir** — aksi hâlde aynı bilginin iki kaynağı olur (**D21**).
- **X12.** **Vitrin tarafı** — galeri, büyütme, detay sözleşmesi, şema, okunabilir liste,
  lejant ve performans ölçümü. *neden:* **D31**; `0016b`'nin işi.

---

## Context

`0016`'nın tam Context'i (mevcut davranış, K1–K7, geçerli kurallar, bilinen tuzaklar) ve
D1–D31'in tamamı `0016b`'de durur. Bu dilimin dayandığı ölçüm:

- **K1 — alan yok.**
  - `VehicleFieldKeys` yalnız `body_type` ve `damage_record_amount` tanımlıyor.
    `VehicleFieldDefinitions` yalnız tramer'i içeriyor.
  - `FieldValueKind` kapalı bir küme (ADR 0007) ve harita tipi içermiyor.
  - `0010a` X4 *"alanın değeri R9 kapsamında girilebilir"* diyordu; `0010b` X22 liste
    tiplerini kapsam dışı bıraktı. Sonuç: **alanın hiçbir spec'te sahibi yoktu** → **D5**.

### Verilen kararlar — kriter turu, 2026-11-18

**D32 — 13 kutunun tamamı boş kaydedilirse kayıtlı değer temizlenir (S1).** Kayıtlı değer
yoksa sonuç "değer yazılmaz"la aynıdır. *Gerekçe:* boşaltılan form kaydedilip değer dolu
kalırsa form yalan söyler; tek kural ("boş = yok") R12'nin iki cümlesini çelişkisiz birleştirir.
*Bedel:* yanlışlıkla boşaltıp kaydetmek kayıtlı haritayı siler; geri alma yok. → R12, AC-20,
AC-22, AC-31.

### Verilen kararlar — bu dilime düşenler (birebir, `0016`'dan)

**D1 — Dört durum; sözlük güncellenir (S1).** Durumlar: orijinal, lokal boyalı, boyalı,
değişen. `docs/domain.md` **Parça Haritası** tanımı dört duruma güncellenir.
*Gerekçe:* "lokal boyalı" Türkiye ikinci el pazarının fiyata etki eden gerçek bir
ayrımıdır; üç duruma sıkıştırmak aracın değerini olduğundan düşük gösterir.
*Bedel:* sözlük değişikliği; C2 gereği dört ayrı desen.

**D4 — Tıkla-seç editörü ayrı kalır (S4).** Bu spec ziyaretçi tarafını ve **D5**'in asgari
veri girişini kapsar; şema üzerinde düzenleme X2.

**D5 — Veri tarafı bu spec'e girer (B1).**
- Kapsam:
  - `paint_change_map` alanı (tek alan, tek eksen çifti).
  - ADR 0007'ye ek kararla yeni değer tipi.
  - Panelin opsiyonel alan formunda 13 seçim kutusu.
  - Varsayılan ayar **Opsiyonel + Görünür**.
  - Seed verisi.
- `0010a` X4'ün veri yarısı ve `0010b` X22'nin bu alana düşen kısmı burada kapanır.
- *Gerekçe:* alan kodda yok (K1); yalnız vitrin yapılırsa özellik canlıda boş çıkar ve
  kriterler yalnız seed'le doğrulanır. 13 × 4 kod tablosu hem veri hem gösterimin ortak
  hakemi; iki spec'e bölmek aynı tabloyu iki yerde tanımlamaktı.
- *Bedel:* spec backend + panel + vitrin olarak büyür; ADR 0007'ye ek karar yazılır.
- *Elenen alternatif:* önkoşul olarak ayrı bir `0016a` spec'i — sıraya bir iş daha
  eklerdi.

**D6 — 13 parçanın kodu ve sırası sabit (B2).** C10'daki tablo.
- *Gerekçe:* listesi hiçbir belgede yazılı değildi (`docs/domain.md:44`,
  `docs/architecture.md:312` yalnız "13 parça" diyor). Kod, çeviri anahtarı ve test aynı
  listeye bağlanır.

**D7 — Değer tam ya da yok; varsayılan seçim yok (B3).** "Belirtilmedi" durumu yok; yarım
harita reddedilir; panelde hiçbir kutu önceden seçili gelmez (R10–R12).
- *Gerekçe:* varsayılan "orijinal", galerinin sessizliğini "hasarsız" beyanına çevirir;
  yarım harita beşinci bir görsel durum doğurur.

**D24 — Kabul kriterlerini Analist yazar; kriterler uygulama dalı açılmadan `main`'e girer
(P1).** *Gerekçe:* `0015`'te kriterleri uygulayan yazdı, kriter PR'ı açık bekledi ve C10
uygulama sırasında değişti; "kriterler önce `main`'de" ölçütü bunu göremedi (`0015`'in
SCORECARD dersi). Bu kart daha büyük; aynı risk daha pahalı.

**D26 — Parça haritası yazma ve okuma modelinde ayrı birer `jsonb` kolonda, yalnız kod
olarak saklanır (P3).** Biçim `[{part, state}]`; 13 parça sabit olduğu için ayrı bir tablo
gerekmez. ADR 0007'nin "her tipin kendi kolonu" ilkesiyle tutarlı. C11'in açık bıraktığı
depolama sorusunun cevabıdır. → C6, C11.

**D27 — Yönetim sözleşmesi haritayı yapılandırılmış ayrı bir alanla taşır (P4).** Bugün her
opsiyonel değer tek bir metin (`string? Value`); haritayı metne gömmek doğrulamayı
ayrıştırıcıya iter ve hata kodlarını anlamsızlaştırır. Eksik parçalar hata yanıtında **kod**
olarak döner (C6). → R11, R12.

**D31 — Kart ikiye bölünür (P8).** `0016a` — parça haritası: değer tipi, kalıcılık, alan
ayarı, panel (R8–R12, C6, C10, C11; D5–D7, D26, D27). `0016b` — vitrin: galeri, büyütme,
detay sözleşmesi, şema, performans (R1–R7, R13–R21, C1–C5, C7, C8, C12, C13; D25, D28–D30).
`0016a` önce gelir; sözleşmesi `0016b`'nin önkoşuludur. *Gerekçe:* `0010` ve `0012`'deki
gibi; her dilim kendi başına test edilir ve kriterlerin yazılmadan bölünmesi neredeyse
bedelsizdir (ADR 0003 Ek 1'in ölçümü).

---

## Açık sorular

> **Çözüldü (2026-11-18).** S1 → **D32**, öneri olduğu gibi onaylandı (Takım Yöneticisi,
> depo sahibi). Sorunun metni gerekçe kaydı olarak duruyor.

S1. **"Temizleme" neyle ifade edilir?** R12 hem *"hiç seçim yapılmamışsa değer
    yazılmaz"* hem *"kayıtlı bir değer bütünüyle temizlenebilir"* diyor. Kayıtlı değerli
    bir araçta personel 13 kutunun tamamını boşaltıp kaydederse ne olacağı yazılı değil:
    ilk cümle "değer yazılmaz" (eski değer kalır) diye de, "temizlenir" diye de okunabilir.
    Bugünkü kod opsiyonel bir değeri silmenin hiçbir yolunu taşımıyor
    (`UpdateVehicleCommand` değeri olmayan girdiyi `KindMismatch` ile reddediyor).
    Öneri: **13 kutunun tamamı boş kaydedilmesi = değerin temizlenmesi.** Kayıtlı değer
    yoksa sonuç "değer yazılmaz"la aynıdır (AC-22); varsa değer silinir (AC-20). Formda
    13 kutuyu tek hareketle boşaltan bir eylem olup olmayacağı arayüz kolaylığıdır ve
    kriter onu şart koşmaz.
    Gerekçe: personel boşalttığı formu kaydedip değeri hâlâ dolu görürse form yalan
    söylemiş olur — D7'nin önlemek istediği "sessizliğin beyana dönmesi"nin aynası. Tek
    kural ("boş = yok") iki cümleyi çelişkisiz birleştirir.
    Bedel: yanlışlıkla 13 kutuyu boşaltıp kaydetmek kayıtlı haritayı siler; geri alma yok.
    Bunu bir onay adımıyla korumak bu spec'in kapsamında değildir.

---

## Acceptance Criteria

> Her kriterin testi `DisplayName`'inde `0016a/AC-<n>` taşır (`docs/testing.md` § 1).
> Parça ve durum kodları C10'daki gibidir. **Tam harita** = C10'un 13 parçasının her biri
> tam bir kez, dört durum kodundan biriyle. Örnek veri kümeleri kriterin içinde verilir;
> eksik parçalar C10'un ortasından ve sonundan seçildi ki "ilk N parça" varsayımı geçmesin.
>
> *Kriter turu (2026-11-18) **Analist** tarafından yazıldı (**D24**). AC-20, AC-22 ve
> AC-31, `## Açık sorular` S1'in önerilen okumasıyla yazıldı.*
>
> **Kriter değil, DoD maddesi:** `docs/domain.md` sözlük güncellemesi (**D1**) ve ADR 0007
> eki (**C11**) aşağıdaki DoD'da ayrı maddedir. *Gerekçe:* ikisi de belge değişikliğidir;
> sistemin davranışını değil belgenin içeriğini iddia eder ve bir test onu ancak metin
> arayarak ölçebilirdi — anahtarı sayan ama biçimi saymayan bir kapı. Davranış tarafı
> (dört durum, yeni tip) AC-3 ve AC-5–AC-12 ile ölçülür.

### Alan ve varsayılan ayar (R8 · C11 · D5)

- **AC-1.** Migration'ları sıfırdan uygulanmış bir kurulumda `paint_change_map` alanının
  ayarı form durumu **Opsiyonel**, public görünürlüğü **Görünür**dür.
- **AC-2.** Tramer'in ayarı `Opsiyonel + Gizli` olan mevcut bir kurulumda migration
  uygulandıktan sonra `paint_change_map` ayarı `Opsiyonel + Görünür` olarak eklenmiştir ve
  tramer'in ayarı `Opsiyonel + Gizli` kalır.
- **AC-3.** Yönetim formunun alan tanımı listesinde `paint_change_map` **tek bir** alan
  olarak, parça haritası tipiyle döner; listede parça başına ayrı bir alan (ör.
  `paint_change_map.hood`) yoktur ve tramer `Amount` tipiyle kalır.
- **AC-4.** `paint_change_map` ayarı **Zorunlu** iken haritası olmayan bir aracın ilanı
  yayına alınamaz; aynı araca tam harita girildikten sonra ilan yayına alınır (zorunlu
  alanın mevcut yayın kuralı yeni alan için de işler).

### Değer kuralları (R9, R10, R11 · C10 · D6, D7)

- **AC-5.** Tam harita (ör. `front_bumper`=`painted`, `trunk_lid`=`replaced`,
  `left_rear_door`=`local_painted`, diğer 10 parça `original`) kaydedilir ve yönetim detay
  yanıtı aynı 13 parça–durum çiftini döner.
- **AC-6.** AC-5'in haritası C10'un **ters** sırasıyla (`right_rear_fender` önce)
  gönderildiğinde de yönetim detay yanıtı parçaları C10 sırasıyla (`front_bumper` önce,
  `right_rear_fender` sonda) döner.
- **AC-7.** `trunk_lid` (C10 #4) eksik, 12 parçalı bir harita **reddedilir** ve araçta
  önceden kayıtlı tam harita **değişmeden** kalır.
- **AC-8.** `roof` (#3) ve `right_rear_door` (#12) eksik, 11 parçalı bir haritanın hata
  yanıtı eksik parçaları **kod** olarak ve C10 sırasıyla taşır: `roof`, `right_rear_door`.
  Yanıtta çevrilmiş parça adı (ör. "Tavan") yoktur.
- **AC-9.** 13 geçerli parçaya ek olarak tanınmayan bir parça kodu (`windshield`) taşıyan
  harita reddedilir.
- **AC-10.** Bir parçanın durumu dört koddan biri değilse harita reddedilir: `scratched` ve
  `unspecified` ayrı ayrı denenir ve ikisi de reddedilir (R10 — "belirtilmedi" durumu yok).
- **AC-11.** `hood` iki kez geçen 14 elemanlı harita ve `hood` iki kez geçip `roof` hiç
  geçmeyen 13 elemanlı harita ayrı ayrı reddedilir; ikincisinin hata yanıtı eksik parça
  olarak `roof`'u taşır.
- **AC-12.** Kodlar tam ve harfe duyarlı eşleşir: durumu `Painted` olan veya parçası
  `Front_Bumper` olan harita reddedilir.
- **AC-13.** Parça haritası opsiyonel alanların **metin değeri** yoluyla (tek bir dize)
  gönderildiğinde reddedilir; harita yalnız yapılandırılmış alanıyla kabul edilir (**D27**).
- **AC-14.** Tramer (`damage_record_amount`) için yapılandırılmış harita gönderilmesi
  reddedilir ve tramer'in kayıtlı tutarı değişmez.
- **AC-15.** `paint_change_map` ayarı **Kapalı** iken tam harita gönderilmesi
  `vehicle.field_closed` ile reddedilir.

### Kalıcılık ve sözleşme (C6 · D26, D27 · F1)

- **AC-16.** AC-5'in haritası kaydedildikten sonra veritabanında saklanan değer yalnız
  parça ve durum **kodlarını** taşır; `"Ön tampon"`, `"boyalı"`, `"Front bumper"` ya da
  herhangi bir Arapça parça/durum adı saklanan değerde geçmez.
- **AC-17.** Yönetim detay yanıtı haritayı kod listesi olarak taşır ve `ar` ile `tr`
  locale'li oturumlarda **aynıdır**.
- **AC-18.** Alan **Kapalı**ya çekildiğinde yönetim detay yanıtında `paint_change_map`
  değeri bulunmaz; alan yeniden **Opsiyonel** yapıldığında AC-5'in 13 çifti değişmeden geri
  döner (F1).
- **AC-19.** Alan `Opsiyonel + Gizli` iken tam harita kaydedilebilir ve yönetim detay
  yanıtında döner — public görünürlük panel girişini etkilemez.
- **AC-20.** Tam haritası kayıtlı bir araçta harita **temizlenip** (13 kutunun tamamı boş)
  kaydedildiğinde araçta `paint_change_map` değeri kalmaz ve yönetim detay yanıtında alanın
  değeri bulunmaz (R12 · S1).
- **AC-21.** AC-20'deki temizleme, aynı aracın tramer değerini (ör. `12500.00`) değiştirmez.
- **AC-22.** Haritası olmayan bir araç, harita için hiçbir seçim yapılmadan kaydedildiğinde
  araçta `paint_change_map` değeri **oluşmaz**; "13 parçası orijinal" bir değer de
  yazılmaz (D7).

### Panel (R12 · C5)

- **AC-23.** Alan açıkken düzenleme formu `paint_change_map` için **13 seçim kutusu**
  gösterir; kutular C10 sırasındadır (ilk `Ön tampon`, son `Sağ arka çamurluk`) ve
  etiketleri etkin locale'in kataloğundan gelir.
- **AC-24.** Her seçim kutusu dört durum seçeneğini (orijinal, lokal boyalı, boyalı,
  değişen) etkin locale'in kataloğundan gelen adlarla ve ayrıca **boş** bir seçeneği sunar.
- **AC-25.** Haritası olmayan bir aracın düzenleme formunda 13 kutunun **hiçbiri** seçili
  gelmez; hiçbir kutuda "orijinal" önceden seçili değildir (D7).
- **AC-26.** AC-5'in haritası kayıtlı bir aracın düzenleme formu açıldığında 13 kutu
  kayıtlı durumlarla gelir: `Ön tampon` = boyalı, `Bagaj kapağı` = değişen, `Sol arka kapı`
  = lokal boyalı, diğer 10 kutu = orijinal.
- **AC-27.** Alan **Kapalı** iken düzenleme formunda parça haritası kutularının hiçbiri
  bulunmaz.
- **AC-28.** Alan `Opsiyonel + Gizli` iken düzenleme formu 13 kutuyu gösterir.
- **AC-29.** 11 kutu seçilip `Tavan` ve `Sağ arka kapı` boş bırakıldığında kayıt
  reddedilir; form eksik parçaları etkin locale'in adıyla ve C10 sırasıyla söyler
  ("Tavan", "Sağ arka kapı") ve yalnız bu iki kutu hatalı (`aria-invalid="true"`)
  işaretlenir.
- **AC-30.** AC-29'daki ret sonrasında formdaki diğer 11 seçim **korunur**; personel iki
  kutuyu doldurup yeniden kaydettiğinde kayıt kabul edilir.
- **AC-31.** AC-20'deki temizlemeden sonra düzenleme formu yeniden açıldığında 13 kutu boş
  gelir.
- **AC-32.** `/ar/` panelinde 13 parça adı, dört durum adı ve AC-29'un hata metni
  Arapça'dır ve form sağdan sola akar (görsel kanıt).
- **AC-33.** 13 parça adı, dört durum adı ve eksik parça hata metni için çeviri anahtarları
  `tr`, `ar` ve `en` kataloglarının üçünde de doludur; eksik anahtar kontrolü yeşildir.

### Tohum verisi (D5)

- **AC-34.** Geliştirme tohumu en az üç **yayında** ilan üretir: biri dört durumun **hepsini**
  içeren tam haritalı, biri 13 parçası `original` olan, biri haritasız. (`0016b`'nin şema,
  tamamı-orijinal ve değersiz kriterlerinin kanıtı bu üç ilanla alınabilir.)

---

## Definition of Done

<!-- Hepsi işaretlenmeden spec "Tamamlandı" olmaz.

     ŞABLONUN MADDELERİ ÇIKARILAMAZ (ADR 0008 / K4). Spec'e özel madde EKLENİR ve şablonun
     bir maddesi spec'e özel ayrıntıyla GENİŞLETİLEBİLİR — ama silinemez. Uygulanmayan bir
     madde yerinde kalır ve "yok" diye cevaplanır: `- [x] Yeni migration **yok**.`

     Nedeni ölçüldü: zorunlu dış gözden geçirme maddesi 2026-09-22'de şablona girdi
     (`1463d7e`) ve bugün ON açık spec'in ONUNDA da yok; kapanmış `0007` ile `0008`'de de
     yok. frontend-002 aynı kaymayı kendi dış bulgusu olarak yazdı: "DoD'u TEMPLATE ile
     hizasızdı — dört madde eksikti." Elle kopyalanan liste sessizce kayar.

     HER MADDE BİR `<!-- DoD:Tn -->` İŞARETLEYİCİSİ TAŞIR ve spec'e kopyalanırken
     işaretleyici de kopyalanır. Metni serbestçe yeniden yazabilirsin — işaretleyici
     kalmak zorunda. `tests/Architecture.Tests/ArsivKapisiTests.cs` eksik işaretleyiciyi
     arşivlenmiş spec'lerde yakalar.

     İşaretleyici, ilk denenen yöntemin — maddenin metninde anahtar ifade aramanın —
     ölçülerek elenmesinden sonra geldi: "Arayüz kriteri yok; görsel kanıt bu spec için
     uygulanabilir değildir" diyen bir madde `docs/evidence` yolunu hiç anmıyor ve beş
     kartta yanlış pozitif veriyordu. Serbest metinde ifade aramak, maddeyi silmekle
     yeniden yazmayı ayırt edemez. -->

- [ ] `docs/domain.md` **Parça Haritası** tanımı dört duruma güncellendi (**D1**).
- [ ] ADR 0007'ye parça haritası değer tipi için ek karar yazıldı; depolama biçimi **D26**
      (**C11**, **D5**).
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/<no>-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri varsa Arapça görüntü de var.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
      <!-- Madde "PR açılmadan önce" diyordu ve üst üste ÜÇ spec'te tutmadı
           (frontend-002, 0010a, 0010b): üçünde de tur koştu, bulgular gerçekti ve
           düzeltildi, tutmayan tek şey zamanlamaydı. Nedeni yapısal, dikkatsizlik
           değil: docs/roles/qa.md § 1 QA'in okuyacağı ilk şeyi "diff — bu PR'da değişen
           ne varsa" diye tanımlıyor, yani rolün kendisi bir PR'ın varlığını varsayıyor.
           "PR açılmadan önce" okunacak bir PR olmadan QA istiyordu. Korunan şey
           değişmedi: bulgular main'e girmeden işlenir. Değişen şey, maddenin tutabildiği
           ana taşınması — Takım Yöneticisi onayı, 2026-10-10. -->
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.
      <!-- Taşıdıktan SONRA `tests/Architecture.Tests`in TAMAMI koşturulur, yalnız arşiv
      kapısı değil: `0012b`'nin kapanışında kartın yolu değişince BAŞKA bir kapı kırıldı
      (etiket kapısı depo kökünü denetlediği dosyanın varlığıyla arıyordu) ve yalnız
      `--filter ArsivKapisi` koşan kapanış turu bunu kaçırdı. `specs/` yolunu okuyan her
      kapı bu taşımadan etkilenebilir. -->

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
