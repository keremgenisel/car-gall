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
| React/Vite/TS konvansiyonları, API client, yükleme/boş/hata durumları, i18n & RTL | `docs/frontend.md` |
| **Neden böyle** — mimari kararların gerekçesi ve değerlendirilen alternatifler | `docs/decisions/` |
| Spec yazma şablonu ve SCORECARD | `specs/TEMPLATE.md` |
| Aktif spec'ler | `specs/*.md` |
| Bitmiş spec'ler (arşiv) | `specs/done/` |

**Roller:** `docs/roles/` — oturum açılışında rolünü üstlen.
**Sorun protokolleri:** `docs/ap.md` — AP kodlarının çözümü.

---

## Çalışma Disiplini

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
