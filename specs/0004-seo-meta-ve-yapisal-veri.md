# 0004 — SEO Meta Enjeksiyonu ve Yapısal Veri

| | |
|---|---|
| **Durum** | **Beklemede** (2026-10-05) — *sırası gelmedi; bkz. aşağıdaki not* |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Host` (rota bazlı enjeksiyon) · `VehicleCatalog.Contracts` (okuma) · `Dealership.Contracts` (şube sayfaları için) |
| **Bağımlı spec'ler** | `0001` (tamamlandı) · `0002` (detay sayfası — **önkoşul**) · `0003` (şubeler) |
| **Karar bekliyor** | yok — *bekleyen şey bir karar değil, SIRA* |

> Bu spec, `0002`'nin **E11** kararı gereğince, `0002` merge edilmeden açıldı.
> `0002` X4'ü kapsam dışı bıraktı; `docs/architecture.md` § 3 ise bunu zorunlu kılıyor.
> Borcun bir yeri olsun diye burası açıldı — açılmasaydı borç görünmez olur ve kimse ödemezdi.

> ### Neden `Beklemede` (2026-10-05)
>
> Bu spec'in Intent'i, Acceptance Criteria'sı ve KAPSAM DIŞI listesi boş ve **bilerek öyle
> bırakılıyor.** `ADR 0003` Ek 1'in sırasında **8. adım**; önünde `0010` (panelden araç
> kaydı), medya yükleme, yayına alma, iki adımlı doğrulama ve ana sayfa var.
>
> **Intent şimdi yazılırsa yeniden yazılacak.** Intent'in cevapladığı üçüncü soru "başarı
> neye benziyor?" — ve bu spec'in başarısı, var olmayan sayfaların arama sonuçlarında nasıl
> göründüğüdür. Bugün vitrin seed verisine bağlı, galeri kendi aracını giremiyor ve ana
> sayfa yok. Hakem olacak Intent'in zemini de yok.
>
> **Durumu `Taslak`'tan `Beklemede`'ye çevirmenin nedeni** budur: `Taslak` "yazılıyor"
> demek ve bu spec yazılmıyor. Üç ayrı durum sorgusunda "`0004`'ün Intent'i boş" diye
> karşımıza çıktı — her seferinde cevap aynıydı ve her seferinde yeniden bulundu.
>
> **Ne zaman açılır:** `ADR 0003` Ek 1'in 7. adımı (ana sayfa) tamamlandığında. O noktada
> vitrin gerçek veriyle çalışıyor, detay sayfası ve şube sayfaları yerinde olacak — Intent
> ölçülebilir bir başarı tanımı yazabilir.
>
> **Bu arada borç görünür kalıyor:** `R1`–`R6` ve `C1`–`C3` duruyor; park etmek silmek
> değildir.

---

## Intent

<!--
Bu bölüm Analist tarafından doldurulacak. Doldurulmadan spec onaya sunulmaz.

Yön verecek üç gözlem:
  1. Bir galeri için organik arama, pazaryerlerinden sonra en büyük ikinci trafik
     kanalıdır; araç detay sayfası bu kanalın giriş noktasıdır.
  2. Sosyal paylaşım önizlemeleri ve zengin sonuçlar (fiyat, km, yıl) JavaScript
     çalıştırmaz — sunucudan gelmek zorundadır.
  3. Çok dilli kurulumda diller birbirinin kanoniği değildir; hreflang olmadan üç dil
     birbirinin kopyası sanılır.
-->

---

## Requirements

<!-- Analist dolduracak. Aşağıdakiler docs/architecture.md § 3'ten gelen ham yükümlülüklerdir. -->

- **R1.** Araç detay sayfası sunucudan `Car` + `Offer` JSON-LD ile gelir; `priceCurrency`
  dağıtım yapılandırmasından okunur.
- **R2.** Şube sayfaları `AutoDealer` + `LocalBusiness` JSON-LD taşır:
  `openingHoursSpecification` (çoklu aralık), `geo`, `telephone`, `address`, `sameAs`.
- **R3.** Her sayfa, etkin her locale için `hreflang` alternatifleri ve `x-default` yayar.
- **R4.** Kanonik URL her zaman kendi locale'inin URL'idir; diller birbirinin kanoniği değildir.
- **R5.** Sayfa başlığı, meta description ve Open Graph etiketleri rota bazlı ve locale'e
  göre sunucudan basılır.
- **R6.** Locale başına sitemap üretilir; `VehiclePublished` / `VehicleUnpublished` /
  `BranchDetailsChanged` olayları sitemap'i günceller.

---

## Constraints

### Uyulması zorunlu
- **C1.** Tam SSR yok (`docs/architecture.md` § 3, Karar Defteri). Enjeksiyon `index.html`
  üzerinde rota bazlı yapılır.
- **C2.** Kullanıcıya görünen hiçbir metin koda gömülmez; başlık ve açıklama çeviri
  kaynaklarından üretilir.
- **C3.** `Host` iş mantığı yazamaz; yalnız modüllerin public contract'larını çağırıp
  birleştirebilir (`docs/architecture.md` § 2.F).

### KAPSAM DIŞI
<!-- Analist genişletecek. -->
- **X1.** Tam SSR'a geçiş — *neden:* C1 verilmiş bir karardır; bu spec onu delmez.
- **X2.** Arama konsolu entegrasyonu, sıralama takibi, anahtar kelime araştırması —
  *neden:* pazarlama işi, ürün işi değil.

---

## Context

- `docs/architecture.md` § 3 "SEO çok dilde" başlığı bu spec'in kaynağıdır.
- `src/Host/Localization/IndexHtmlRenderer.cs` bugün `lang`/`dir` ve temel meta enjekte
  ediyor; rota bazlı içerik enjeksiyonunun genişleyeceği yer burasıdır.
- `0002` detay sayfasını ve kalıcı slug'ı getirdi (`/{locale}/cars/{slug}`); JSON-LD'nin
  ihtiyaç duyduğu veri `IShowcaseListingDetailQueries` üzerinden okunabilir.
- `0003` şube okuma yüzeyini getirdi.
- Sitemap üretimi için `0003`'ün outbox/olay dağıtımı altyapısı kullanılabilir.

---

## Acceptance Criteria

<!--
Analist yazacak. Her satır tek başına test edilebilir olmalı; JSON-LD kriterleri
şema doğrulamasıyla, hreflang kriterleri sunucu yanıtının kendisiyle doğrulanır.
-->

- **AC-1.**
- **AC-2.**

---

## Definition of Done

- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T2 --> Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/<no>-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri varsa Arapça görüntü de var.
- [ ] <!-- DoD:T5 --> Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | |
| **Düzeltme turu sayısı** | | |
| **Bulgu gerçek/gürültü oranı** | / | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | |

**Bu spec'ten çıkarılan ders:**
