# AGENTS.md — İşaret Levhası

Bu dosya kural **içermez**, kuralın **yerini** gösterir. Tek gerçek kaynağı ilkesi:
bir kural yalnızca bir dosyada yaşar. Burada bulduğun bir cümle ile `docs/` altındaki
bir cümle çelişirse, **`docs/` kazanır** — ve bu dosya düzeltilir.

---

## Proje

Tek bir araç galerisi için satış vitrini ve yönetim paneli.

- **Kurulum başına tek galeri** (white-label). Galerinin **birden çok şubesi** olabilir;
  her araç bir şubede durur. Kodda `TenantId` yoktur, `BranchId` vardır.
- **Çok dilli, ilk sınıf**: TR / AR / EN. Arapça için tam RTL. Varsayılan dil kurulum
  yapılandırmasından gelir — kod hiçbir yerde bir dili varsaymaz.
- **Kurulum başına tek para birimi**, dağıtım yapılandırmasında sabit (TRY, KWD…).
- **Mimari: modüler monolit.** Tek uygulama, tek veritabanı; her modülün kendi şeması,
  kendi `DbContext`'i, kendi migration zinciri ve tek bir public yüzeyi (`X.Contracts`).
- **Stack:** .NET / Minimal API / EF Core / PostgreSQL / xUnit · `frontend/` React + Vite + TypeScript.

---

## Altın Kurallar (7)

1. **Spec'siz iş başlamaz.** `specs/` altında spec'i olmayan bir feature'a dokunulmaz,
   branch açılmaz. → `specs/TEMPLATE.md`, `docs/git.md`
2. **ÖNERİ KURALI.** Soru, seçenek, bulgu veya açık konu getiren; **kendi önerisini ve
   gerekçesini de getirir.** Kararı **Takım Yöneticisi** verir. Onaysız öneri uygulanmaz.
   Bu kural insan ve ajan üyeler için aynıdır.
3. **Modül sınırı ihlal edilmez.** Bir modüle erişim yalnızca `X.Contracts` üzerindendir.
   Başka modülün tablosu, internal tipi, enum'u, `DbContext`'i veya dosya deposu kullanılmaz.
   → `docs/architecture.md` (§ Yasak Listesi)
4. **Para ve yüzde her zaman `decimal`** — asla `double`/`float`. Para `Money` değer tipiyle
   taşınır (tutar + para birimi + minor unit). → `docs/conventions.md`
5. **Her kabul kriteri en az bir testle karşılanır.** Kriter → test eşlemesi izlenebilir olmalı.
   → `docs/testing.md`
6. **Kullanıcıya görünen hiçbir metin koda gömülmez** ve **teknik hata metni kullanıcıya
   sızmaz.** API hata kodu döner, uç çevirir. → `docs/conventions.md`, `docs/frontend.md`
7. **`main`'e doğrudan commit yok, force push yok, geçmiş silinmez.** Geri alma = `revert`.
   Branch: `feature/<spec-no>-<ad>`. → `docs/git.md`

---

## Neyi Nerede Bulursun

| Arıyorsan… | Dosya |
|---|---|
| Modül haritası, sahiplik, modüller arası iletişim, **yasak listesi**, kapsam dışı kararlar | `docs/architecture.md` |
| Ortak dil (sözlük), iş kuralları, araç/ilan/talep yaşam döngüleri | `docs/domain.md` |
| Adlandırma, katmanlar, hata yönetimi, `decimal`/`Money`, tarih-saat, id'ler, EF kuralları | `docs/conventions.md` |
| Branch stratejisi, commit formatı, PR şartları, merge kuralı, git yasakları | `docs/git.md` |
| Test stratejisi, xUnit adlandırma, kriter→test eşlemesi, görsel kanıt, CI kapıları | `docs/testing.md` |
| **Frontend standardı** — TS zorunluluğu, klasör yapısı, tek API client, tasarım token'ları ve bileşen envanteri, üç durum kuralı, erişilebilirlik tabanı, kalite kapıları, mini-spec süreci | `docs/frontend.md` |
| **Neden böyle** — mimari kararların gerekçesi ve değerlendirilen alternatifler | `docs/decisions/` |
| Spec yazma şablonu ve SCORECARD | `specs/TEMPLATE.md` |
| Aktif spec'ler | `specs/*.md` |
| Bitmiş spec'ler (arşiv) | `specs/done/` |

**Roller:** `docs/roles/` — oturum açılışında rolünü üstlen.
**Sorun protokolleri:** `docs/ap.md` — AP kodlarının çözümü.

---

## Çalışma Disiplini

**Oturumun ilk işi**
- `gh run list --branch main --limit 5` oku. Son **zamanlı (gece) koşu** kırmızıysa, kendi
  işine başlamadan önce onu raporla: hangi iş, hangi test, hangi commit'ten beri. Rolünden
  bağımsızdır. **Raporlamak yeterli** — düzeltmek ayrı bir karardır ve senin kapsamını
  kendiliğinden genişletmez. (ADR `0008` / K6, Takım Yöneticisi kararı 2026-10-10.)
  *Neden:* `0006`'nın zaman bombası `main`'de dört gün durdu; bu depoda sabit nöbetçi yok,
  bu yüzden kural "ilk bakan"a bağlı.
- **Son 24 saatte hiç zamanlı koşu YOKSA bunu da raporla.** Olayı ayrıca sor, listeye
  bakmakla yetinme — `push` koşuları listeyi yeşil gösterir ve zamanlı koşunun yokluğunu
  gizler:
  ```
  gh api "repos/<sahip>/<depo>/actions/workflows/ci.yml/runs?event=schedule&per_page=1" \
    --jq .total_count
  ```
  *Neden:* kural kırmızı bir koşu için yazılmıştı ve "koşu hiç yok" hâli sessizce "sorun yok"
  gibi görünüyordu. Hiç havlamayan bekçi, yanlış havlayan bekçiden daha tehlikelidir:
  `0006`'yı dört gün görünmez kılan şey koşunun **yokluğu**ydu. İlk zamanlı pencere
  2026-10-11'de kaçtı ve bu ölçüm ADR `0008` / K6'ya yazıldı.
  (Takım Yöneticisi kararı 2026-10-11.)
- **Pencere 24 değil 36 saattir, koşunun GECİKMESİ de raporlanır ve yokluk 12:00
  UTC'den önce ölçülmez.** Ölçüldü (2026-10-12): zamanlı koşu **her iki pencerede de**
  geldi ama cron'dan **+06:35** ve **+07:06** sonra — yani `0 3 * * *` yazan bir cron'un
  koşusu pratikte **10:00 UTC civarında** düşüyor. "Son 24 saatte koşu yok" cümlesi hem
  gecikmiş ama çalışan bir tetikleyiciyi "yok" sayıyordu, hem de sabah erken okunduğunda
  yanlış alarm üretiyordu. Bundan sonra: **36 saatte hiç koşu yoksa** yokluk raporlanır ·
  koşu varsa `created_at` ile cron saatinin farkı yazılır · **beklenen varış penceresi
  geçmeden (12:00 UTC) yokluk sonucuna varılmaz** — periyodik bir olayın yokluğu, varışı
  beklenmeden ölçülemez.
  `perf.yml`'da `schedule` tetikleyicisi **yoktur**; oradaki `total_count: 0` bir kaçan
  pencere değil, ADR `0008`'in bilinçli kapsam kararıdır ve raporlanmaz.
  (ADR `0008` / K6, 2026-10-12 ölçümü.)

**Bir işe başlamadan önce**
1. İlgili spec'i **tam** oku. Spec yoksa dur ve spec iste (Altın Kural 1).
2. Kabul kriterlerini işin tanımı olarak al. Kriterde olmayan şey kapsamda değildir.
3. Dokunacağın modülü belirle. Birden fazla modüle dokunuyorsan, önce
   `docs/architecture.md` § İletişim Kuralları'nı oku — muhtemelen yanlış yerden giriyorsun.

**Çalışırken**
4. **Küçük dilimler hâlinde ilerle.** Her dilim kendi başına derlenir, testleri geçer ve
   bir kabul kriterini karşılar.
5. **Tahmin etme, doğrula.** Bir dosyanın, alanın veya davranışın var olduğunu varsayma;
   bak. Var olmayan bir şeye referans veren öneri, öneri değildir.
6. Belirsizliğe çarptığında: işin **bağımsız** kısımlarını bitir, belirsiz kısım için
   Altın Kural 2'yi uygula (öneri + gerekçe), kararı bekle.
7. Kapsamı kendi başına daraltma da genişletme. Kapsam kararı Takım Yöneticisi'nindir.

**Bitirirken**
8. Kriter → test eşlemesini göster. Arayüz işlerinde her kritere **görsel kanıt** ekle.
9. Pipeline yeşil olmadan PR açılmaz. → `docs/git.md`
10. Ne yaptığını, neyi **yapmadığını** ve neden yapmadığını açıkça yaz. Eksik bırakılan
    bir kriter sessizce geçilmez.

**Raporlama dürüstlüğü**
Test kırmızıysa kırmızı denir, çıktısıyla birlikte. Atlanan adım atlandı denir.
Biten iş, doğrulandıktan sonra ve çekincesiz biçimde bitti denir.
