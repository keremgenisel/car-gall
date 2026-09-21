# specs/done/ — Tamamlanmış Spec'ler

Bir spec `Definition of Done`'daki tüm maddeler işaretlendiğinde ve **SCORECARD**
doldurulduğunda buraya taşınır.

## Taşıma kuralları

- Dosya adı **değişmez**. `specs/0001-arac-listeleme.md` → `specs/done/0001-arac-listeleme.md`
- Taşıma `git mv` ile yapılır; geçmiş korunur.
- Spec'in üstündeki **Durum** alanı `Tamamlandı` olarak güncellenir.
- **SCORECARD boş bırakılmış spec taşınmaz.** Eksik scorecard, bitmemiş iş demektir.
- Taşıma işi genellikle merge sonrası ayrı bir `chore` commit'idir
  (`chore(docs): 0001 spec'ini done'a taşı`).

## Neden arşivleniyor, silinmiyor

- **Numaralar serbest kalmaz.** `0007` bir kez kullanıldıysa bir daha kullanılmaz;
  branch adları ve commit atıfları (`[plan 0007/3]`) geçmişte kalıcıdır.
- Bir davranışın **neden öyle olduğu** sorulduğunda cevap burada: Intent, Constraints
  ve özellikle **KAPSAM DIŞI** bölümü. "Bu neden yapılmamış?" sorusunun cevabı
  genellikle bir `X<n>` maddesidir.
- **SCORECARD'lar birikince süreç görünür olur.** Tek bir spec'in skoru gürültüdür;
  yirmi spec'in skoru eğilimdir — nerede tekrar tekrar aynı hataya düştüğümüzü gösterir.

## Periyodik okuma

Belirli aralıklarla scorecard'lar birlikte okunur ve şu sorular sorulur:

- Spec revizyon sayısı sürekli yüksek mi? → Spec yazma aşaması çok ince.
- Düzeltme turu sayısı sürekli yüksek mi? → Kabul kriterleri yeterince kesin değil
  veya dilimler çok büyük.
- Gürültü oranı yüksek mi? → Gözden geçirme odağı yanlış yerde.
- Kaçan hata sıfırdan büyük mü? → Testler kritik yolu kapsamıyor (`docs/testing.md` § 1).

Çıkan sonuç bir **öneri** olarak Takım Yöneticisi'ne getirilir
(`AGENTS.md` Altın Kural 2); süreç kuralı tek taraflı değiştirilmez.
