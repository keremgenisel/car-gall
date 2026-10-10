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

<!-- Hepsi işaretlenmeden spec "Tamamlandı" olmaz. Gerekirse spec'e özel madde eklenir. -->

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/<no>-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri varsa Arapça görüntü de var.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
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
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** (`Kaçan hata` hariç — o `izleniyor` kalır, § SCORECARD)
      ve spec `specs/done/` klasörüne taşındı.

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
| **Spec revizyon sayısı** | | Onaylandıktan sonra **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? Açıklayıcı not eklemek de sayılır: nete ihtiyaç duyan kural, eksik yazılmış kuraldır. Intent, Context, kapsam dışı gerekçeleri ve SCORECARD değişiklikleri **sayılmaz**. Yüksekse spec eksik yazılmıştır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. **`0` ile `—` aynı şey değildir:** `0`, gözden geçirildi ve hiç tur dönmedi demektir; gözden geçirme hiç yapılmadıysa `—` yazılır. İkisini aynı göstermek, ölçülmemiş işi temiz işmiş gibi gösterir. |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz — kendi kodunu okuyan kişi kendi körlüğünü paylaşır. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. **Ölçtüğü şey: spec ve kod ne kadar açık yazıldı.** |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı — kendi testi kırmızı verdiği, kendi kanıtına baktığı veya kendi sondası yakaladığı için. Dış bulguyla aynı sütunda toplanmaz: ikisi **farklı şey** ölçer. **Ölçtüğü şey: yazar kendi işini ne kadar sorguladı.** Sıfır, "hiç hata yapmadım" değil "hiçbirini kendim bulmadım" demektir ve dış bulgu yüksekse bu bir uyarıdır. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. |

**Bu spec'ten çıkarılan ders:**
<!-- 1-2 cümle. Bir sonraki spec'i nasıl daha iyi yazarız? -->
