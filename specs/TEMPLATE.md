# Spec Şablonu

> Bu dosya **şablondur, doldurulmaz.** Yeni spec için kopyala:
> `specs/<dört-haneli-no>-<kebab-ad>.md` → `specs/0001-arac-listeleme.md`
>
> Numara sırayla verilir ve **yeniden kullanılmaz** (bitmiş spec'ler `specs/done/`e taşınır
> ama numaraları serbest kalmaz).
>
> **Spec'siz iş başlamaz** (`AGENTS.md` Altın Kural 1). Branch adı bu numarayı taşır
> (`feature/0001-arac-listeleme`), commit'ler bu numaraya atıf yapar (`docs/git.md` § 2).
>
> Kopyaladıktan sonra bu blok ve `<!-- … -->` yönergeleri silinir.

---

# <NO> — <Başlık>

| | |
|---|---|
| **Durum** | Taslak · Onaylandı · Geliştiriliyor · **Beklemede** · Tamamlandı |
| **Sahip** | <kim yürütüyor> |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | <hangi modüller — docs/architecture.md> |
| **Bağımlı spec'ler** | <numaralar, yoksa "yok"> |
| **Karar bekliyor** | <hangi karar, kimden — yoksa "yok"> |

<!--
İKİ ALAN BİR ARADA ÇALIŞIR.

**Durum = Beklemede**, spec'in ilerlemesi BAŞKA BİR ŞEYE bağlı olduğunda kullanılır:
bir karar, bir veri, ya da sırası gelmemiş bir önkoşul. `Taslak`'tan ayırt edilir —
`Taslak` "yazılıyor", `Beklemede` "yazılmıyor ve nedeni şu" demektir. Yarım kalmış bir
taslak ile bilinçli park edilmiş bir spec aynı şey değildir; ayırt edilmezse durum
sorgusunda ikisi de "eksik" görünür ve hangisine dokunulacağı belli olmaz.

**Karar bekliyor**, bir karar onaysız beklediği sürece DOLU kalır: hangi karar, kimden.
Boş geçilmez — bekleyen bir şey yoksa "yok" yazılır.

*Neden alan olarak var:* kararlar spec'in ortasında, "Verilen kararlar" bölümünde
yaşıyordu. Bir karar onaysız kaldığında bunu görmenin tek yolu spec'i baştan okumaktı ve
üç karar aynı deseni izleyerek birikti: spec yazıldı → karar bekledi → iş durdu → başka
bir şeye geçildi. Başlıktaki tek satır, "hangi spec neyi bekliyor" sorusunu okumadan
cevaplar.
-->

---

## Intent

<!--
3-5 cümle. İŞ DİLİNDE yaz — modül adı, tablo, endpoint, kütüphane geçmesin.
Üç soruyu cevapla:
  1. Bunu KİM istiyor? (ziyaretçi / satış danışmanı / galeri yöneticisi / içerik editörü)
  2. NEDEN istiyor? Şu anda hangi acıyı çekiyor, ne yapamıyor?
  3. BAŞARI neye benziyor? Bu iş bittiğinde ne değişmiş olacak — gözlemlenebilir biçimde?

Bu bölüm spec'in en önemli kısmıdır. Kabul kriterleri tartışıldığında hakem budur.
Teknik bir cümle yazdıysan, bu bölüm yanlış yazılmıştır.
-->

---

## Requirements

<!--
Numaralı liste. Her madde SİSTEMİN NE YAPACAĞINI söyler, nasıl yapacağını değil.
Domain terimleri docs/domain.md sözlüğünden alınır.
-->

- **R1.**
- **R2.**
- **R3.**

---

## Constraints

<!-- Sınırlar. "Kapsam dışı" bölümü boş bırakılmaz — "yok" bile yazılsa doldurulur. -->

### Uyulması zorunlu
<!-- Mimari kural, performans sınırı, yasal gereklilik, geriye dönük uyumluluk.
     docs/architecture.md § Yasak Listesi'nden ilgili maddelere atıf yapılabilir. -->
- **C1.**
- **C2.**

### KAPSAM DIŞI
<!-- Bu spec'te BİLİNÇLİ olarak yapılmayacaklar ve NEDEN.
     Kapsam kayması buradan engellenir. Sonradan "bunu da yapsaydık" tartışması
     buraya bakarak kapanır. -->
- **X1.** … — *neden:* …
- **X2.** … — *neden:* …

---

## Context

<!--
Geliştiricinin bilmesi gereken arka plan. Uydurma yok, doğrulanmış bilgi.
  - Mevcut davranış nedir?
  - Hangi dosya/modül/endpoint etkileniyor? (varsa yol ver)
  - Hangi domain kuralları geçerli? (docs/domain.md § madde numaraları)
  - Hangi kararlar daha önce verildi? (docs/architecture.md § Karar Defteri)
  - Bilinen tuzaklar, geçmiş hatalar, dikkat edilecek yan etkiler.
-->

---

## Acceptance Criteria

<!--
HER SATIR TEK BAŞINA TEST EDİLEBİLİR OLMALI.

Kurallar:
  - Bir kriter bir davranış. "ve" ile bağlanan iki davranış varsa iki kriterdir.
  - Gözlemlenebilir yaz: ne girdi, ne beklenen çıktı/durum.
  - Belirsiz kelime yok: "hızlı", "kullanıcı dostu", "düzgün", "uygun şekilde".
  - Mutlu yol kadar HATA YOLU da yazılır.
  - Arayüz kriterlerinde yükleniyor / boş / hata durumları ayrı kriterlerdir
    (docs/frontend.md § 3).
  - Çok dil ve RTL beklentisi varsa ayrı kriter olarak yazılır.
  - Para söz konusuysa en az bir kriter 3 ondalıklı para birimini (KWD) kapsar.

Her kriter bir testle karşılanır ve test DisplayName'i "AC-<n>: <metin>" taşır
(docs/testing.md § 1).
-->

- **AC-1.**
- **AC-2.**
- **AC-3.**
- **AC-4.**

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
