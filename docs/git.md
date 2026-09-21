# Git Disiplini

Bu kurallar **insan ve ajan üyeler için aynıdır.** Bir ajanın açtığı branch, yazdığı commit
ve açtığı PR, insan üyeninkiyle aynı standarda tabidir. Ayrıcalık yoktur, istisna yoktur.

---

## 1. Branch Stratejisi

**Tek kalıcı dal: `main`.** Her zaman yeşil, her zaman dağıtılabilir.

### Branch adı
```
feature/<spec-no>-<kisa-ad>
```
- `<spec-no>`: `specs/` altındaki spec'in dört haneli numarası — `0001`, `0014`.
- `<kisa-ad>`: kebab-case, 2–4 kelime, işin ne olduğunu söyler.

```
feature/0001-arac-listeleme
feature/0007-sube-calisma-saatleri
feature/0012-lead-atama
```

**Frontend mini-spec'leri** (`specs/frontend-XXX-<ad>.md`, → `docs/frontend.md` § 9) dört haneli
numara taşımaz; branch adında spec dosya adının önekiyle numarası birlikte kullanılır:

```
feature/frontend-<XXX>-<kisa-ad>     → specs/frontend-001-standarda-uyum.md
                                       ↳ feature/frontend-001-standarda-uyum
```

Diğer önekler:
```
fix/<spec-no>-<kisa-ad>       → kabul kriterine aykırı davranışın düzeltilmesi
chore/<kisa-ad>               → bağımlılık, CI, araç zinciri (spec'siz açılabilecek TEK önek)
```

### SPEC'SİZ BRANCH AÇILMAZ

`chore/` dışındaki her branch, `specs/` altında **var olan** bir spec'e dayanır.
Spec yoksa:
1. Dur.
2. `specs/TEMPLATE.md`'den bir spec taslağı öner (öneri kuralı — `AGENTS.md` Altın Kural 2).
3. Takım Yöneticisi onaylayana kadar kod yazma.

"Küçük bir şey, spec'e gerek yok" geçerli bir gerekçe değildir. Küçükse spec de küçüktür.

### Branch ömrü
- Branch `main`'den açılır ve `main`'e döner.
- Bir branch **bir spec'e** hizmet eder. İki spec tek branch'te ilerlemez.
- Uzun süren branch'ler `main` ile **rebase** edilerek güncel tutulur (merge commit ile değil).
- Merge edilen branch silinir.

---

## 2. Commit Formatı

**Conventional Commits + plan atıfı.**

```
<tip>(<kapsam>): <özet> [plan <spec-no>/<adım>]
```

```
feat(catalog): sayfalama endpoint'i [plan 0001/3]
fix(leads): atanmamış talep bildirimi gönderilmiyordu [plan 0012/5]
test(dealership): çalışma saatleri kabul kriterleri [plan 0007/2]
refactor(catalog): fiyat geçmişini değer tipine taşı [plan 0004/6]
chore(ci): mimari testleri pipeline'a ekle
```

### Tipler
| Tip | Ne zaman |
|---|---|
| `feat` | Yeni davranış |
| `fix` | Kabul kriterine aykırı davranışın düzeltilmesi |
| `refactor` | Davranış değişmeden yapı değişikliği |
| `test` | Yalnızca test ekleme/düzeltme |
| `docs` | Yalnızca dokümantasyon |
| `chore` | Bağımlılık, CI, araç zinciri |
| `perf` | Ölçülmüş performans iyileştirmesi |

### Kapsam
Kapsam **modül adının kısa hâlidir**: `catalog`, `leads`, `dealership`, `identity`,
`media`, `content`, `notifications`, `audit`, `engagement`, `reference`,
`frontend`, `host`, `ci`, `docs`.

Bir commit **birden çok modüle dokunuyorsa** — bu genellikle bir sınır ihlalinin işaretidir.
Gerçekten gerekiyorsa commit bölünür; bölünemiyorsa PR açıklamasında gerekçesi yazılır.

### Plan atıfı
`[plan <spec-no>/<adım>]` — hangi spec'in kaçıncı adımını karşıladığını söyler.
Bir spec'e ait her commit bu atfı taşır. `chore` commit'leri muaftır.

### Özet kuralı
- Emir kipi, küçük harfle başlar, nokta ile bitmez.
- En fazla 72 karakter.
- **Ne yapıldığını** söyler, **nasıl** yapıldığını değil.
- Türkçe yazılır (ekip dili), teknik terimler olduğu gibi kalır.

### Gövde (opsiyonel ama teşvik edilir)
Boş satırdan sonra: **neden** bu değişiklik yapıldı, hangi alternatif neden seçilmedi,
dikkat edilmesi gereken yan etki. Ne yapıldığı diff'te zaten görünür.

---

## 3. YASAKLAR

| Yasak | Neden |
|---|---|
| **`main`'e doğrudan commit** | Her değişiklik gözden geçirilir. İstisna yok. |
| **Force push** (`--force`, `--force-with-lease`) paylaşılan branch'e | Başkasının işini sessizce siler. |
| **Geçmiş silme / yeniden yazma** — `main` üzerinde `rebase`, `reset --hard`, `filter-branch` | Geçmiş ortak hafızadır. |
| **Geri alma için commit silme** | Geri alma = **`revert`**. Yanlış gideni yeni bir commit ile geri al; izi bırak. |
| Üretilmiş dosya, `bin/`, `obj/`, `node_modules/`, `.env` commit'lemek | `.gitignore` bunun içindir. |
| Sır (connection string, API anahtarı, token) commit'lemek | Sızan sır rotasyon gerektirir; geçmişten silmek geçmiş yeniden yazmaktır. |
| Boş veya anlamsız commit mesajı (`fix`, `wip`, `.`) | Mesaj bir kayıttır, formalite değil. |
| Devasa commit ("her şeyi yaptım") | Gözden geçirilemeyen commit gözden geçirilmemiş demektir. |

**Kendi feature branch'inde**, `main`'e rebase etmek ve `--force-with-lease` ile push etmek
serbesttir — branch senin ve PR açılmamışsa. PR açıldıktan ve gözden geçirme başladıktan
sonra force push edilmez.

---

## 4. Pull Request Şartları

Bir PR şu şartların **hepsini** sağlamadan merge edilmez:

1. **Bir spec'e dayanır.** PR başlığı spec numarasını taşır.
2. **PR şablonu doldurulmuştur** (§ 5). Boş bırakılan alan yoktur.
3. **Pipeline yeşildir.** Kırmızı pipeline ile merge yoktur — "sonra düzeltiriz" yoktur.
   Pipeline şunları içerir:
   - backend build + xUnit (`docs/testing.md`)
   - **mimari testler** (modül sınırı ihlalleri)
   - migration şema kontrolü (modül X yalnızca `x` şemasına dokunur)
   - frontend lint + build (`docs/frontend.md`)
   - eksik çeviri anahtarı kontrolü
4. **Her kabul kriteri karşılanmış ve kanıtlanmıştır.** PR açıklamasında kriter → test
   eşlemesi vardır. Arayüz işlerinde her kritere **görsel kanıt** (ekran görüntüsü) eklenir.
5. **Karşılanmayan kriter varsa açıkça yazılmıştır** — neden karşılanmadığı ve ne önerildiği
   ile birlikte. Sessizce atlanan kriter PR'ı reddeder.
6. **En az bir onay.** Ajan üyenin açtığı PR'ı **Takım Yöneticisi onaylar**; ajan kendi
   PR'ını merge edemez.

---

## 5. PR Şablonu

`.github/pull_request_template.md` olarak yaşar. İçeriği:

```markdown
## Spec
specs/<no>-<ad>.md

## Ne değişti
(2-4 cümle, İŞ dilinde — teknik değil. Ne yapılabilir hâle geldi?)

## Kabul kriteri → kanıt
| Kriter | Kanıt |
|---|---|
| AC-1 … | `VehiclePublishingTests.Yayinlanamaz_GorselYoksa` |
| AC-2 … | ekran görüntüsü: docs/evidence/0001-ac2.png |

## Karşılanmayan kriterler
(yoksa "yok" yaz — boş bırakma)

## Mimari etkisi
- Dokunulan modüller:
- Yeni entegrasyon olayı / contract değişikliği: (varsa)
- Yeni migration: (varsa, hangi şema)

## Açık konular / öneriler
(öneri kuralı: her açık konu kendi önerisi ve gerekçesiyle gelir — AGENTS.md Altın Kural 2)
```

---

## 6. Merge Kuralı

**Squash merge — istisnasız.**

- `main` üzerinde her PR **tek bir commit**tir. `main` geçmişi feature listesi gibi okunur.
- Squash commit mesajı **PR başlığından** üretilir ve § 2'deki formata uyar.
  Otomatik üretilen mesaja güvenilmez; merge eden kişi mesajı düzeltir.
- Merge commit ve rebase-merge kullanılmaz.
- Merge sonrası branch silinir.

---

## 7. Ajan Üyeler İçin Ek Kural

Ajan üyeler bu dosyanın tamamına uyar. Ayrıca:

- **Commit mesajını üye yazar, Takım Yöneticisi onaylar.** Ajan kendi işini kendi merge etmez.
- Ajan, mesajda **ne yaptığını olduğu gibi** yazar. Yapılmayan iş "yapıldı" diye yazılmaz;
  atlanan test "geçti" denmez.
- Ajan **spec'siz branch açamaz** ve spec kapsamını kendi başına genişletemez.
  Kapsam değişikliği önerilir, uygulanmaz (öneri kuralı).
- Ajan `main`'e push etmez, force push yapmaz, geçmişe dokunmaz.
- Bir ajan commit'i insan commit'inden ayırt edilebilir olmalıdır: `Co-Authored-By` satırı
  veya eşdeğeri commit'te bulunur.
