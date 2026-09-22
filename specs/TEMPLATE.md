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
| **Durum** | Taslak · Onaylandı · Geliştiriliyor · Tamamlandı |
| **Sahip** | <kim yürütüyor> |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | <hangi modüller — docs/architecture.md> |
| **Bağımlı spec'ler** | <numaralar, yoksa "yok"> |

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
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

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
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. |
| **Bulgu gerçek/gürültü oranı** | / | Gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
<!-- 1-2 cümle. Bir sonraki spec'i nasıl daha iyi yazarız? -->
