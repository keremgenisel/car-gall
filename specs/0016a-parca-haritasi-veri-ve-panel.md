# 0016a — Parça Haritası: Veri ve Panel

| | |
|---|---|
| **Durum** | Taslak — kararlar verildi, kriter turu (Analist) bekliyor |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (değer tipi, kalıcılık, alan ayarı, yönetim sözleşmesi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | `0010a`, `0010b` (opsiyonel alan formu — tamamlandı) · **`0016b`** bu dilimin sözleşmesine dayanır |
| **Dayanak** | ADR 0003 Ek 1, sıra #10'un **birinci dilimi** (`0016` **D31**) · ADR 0007 (opsiyonel alan tipleri) |
| **Karar bekliyor** | yok — kararlar `0016`'nın CLARIFY ve plan turlarından (D1–D31; bu dilime düşenler aşağıda birebir). |

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

> Açık soru yok.

---

## Acceptance Criteria

> Kriter turunda **Analist** yazar (**D24**).

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
