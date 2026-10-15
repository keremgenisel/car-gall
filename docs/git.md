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
docs/<kisa-ad>                → spec metni, ADR, doküman (kod değişmez)
chore/<kisa-ad>               → bağımlılık, CI, araç zinciri
```

`docs/` ve `chore/` **spec'siz açılabilir**; dayanağı § 4 madde 1'in saydıklarından biridir —
bir **spec taslağı**, bir **ADR** (`docs/decisions/`) ya da **yazılı bir süreç maddesi**
(örn. `specs/done/README.md` § Periyodik okuma). Yalnız ADR saymak, bu öneki doğuran işleri
kural dışı bırakırdı: #24 bir spec taslağıydı, #43 bir periyodik okumaydı ve ikisinin de
arkasında ADR yok. `docs/` öneki bir kuralı kodlamadan önce iki PR'da kullanılmıştı
(#24, #28) ve listede yoktu — ADR `0008` / K2 ile yazıldı. Ayrımı şu: `docs/` **metni**
değiştirir, `chore/` **araç zincirini**. İkisi birden değişiyorsa `chore/` kullanılır.

### SPEC, İMPLEMENTASYONDAN ÖNCE `main`'E GİRER VE ORADA DONAR

Bir spec'in **Requirements, Constraints ve Acceptance Criteria** bölümleri, o spec'i uygulayan
commit'ten **önce** `main`'de olmak zorundadır — kendi `docs/` PR'ıyla. O commit'ten sonra her
R/C/AC değişikliği **sayılan bir revizyondur** (`specs/TEMPLATE.md` § SCORECARD) ve kendi
commit'ine girer; implementasyon commit'inin içine karıştırılmaz.

Bu kural ölçülmüş bir körlükten doğdu (`specs/done/SCORECARD-ANALIZ-002.md` § Bulgu 3,
ADR `0008` / K2). On iki kapanmış spec'in **dokuzunda** kriterler implementasyon
commit'inden önce `main`'de değildi: altısında spec dosyası hiç yoktu, üçünde dosya vardı
ama implementasyon commit'i kriterleri yeniden yazdı — `0008` **2 kriterden 33'e**. Sonuç:
"spec onaydan sonra kaç kez değişti" sorusunun karşılaştırılacak bir "önce"si yok ve
`Spec revizyon sayısı` sütunu dokuz kartta **ölçülemez**. Düşük sayı iyi spec değil,
ölçülemeyen spec demek.

Kuralına uyan tek spec `0007` oldu (tam metin #14, `ad0c221`) ve kartı on ikinin en
güvenilir revizyon sayısını taşıyor — bedeli bir ek PR ve bir ek onay turu.

### SPEC'SİZ BRANCH AÇILMAZ

`docs/` ve `chore/` dışındaki her branch, `specs/` altında **var olan** bir spec'e dayanır.
`docs/` bunun dışındadır, çünkü K2 gereği spec'i `main`'e sokan PR'ın kendisi odur: var
olmayan bir spec'e dayanması istenemez.
Spec yoksa:
1. Dur.
2. `specs/TEMPLATE.md`'den bir spec taslağı öner (öneri kuralı — `AGENTS.md` Altın Kural 2).
3. Takım Yöneticisi onaylayana kadar kod yazma.

"Küçük bir şey, spec'e gerek yok" geçerli bir gerekçe değildir. Küçükse spec de küçüktür.

### Branch ömrü
- Branch `main`'den açılır ve `main`'e döner.
- Bir branch **bir spec'e** hizmet eder. İki spec tek branch'te ilerlemez.
- Uzun süren branch'ler `main` ile **rebase** edilerek güncel tutulur (merge commit ile değil).
- **Bir günden fazla `main`'den kopmuş bir daldan PR açılmaz.** Önce `main`'e rebase edilir
  ve CI yeniden koşturulur.

  Bu kural ölçülmüş bir zarardan doğdu: ilk altı spec'in üç regresyonu **yalnız birleşimde**
  çıktı ve üç dalın üçü de `main`'den günlerce kopuktu. Dalın kendi CI'ı yeşildi, çünkü
  çarpışacağı kod o dalda yoktu — global bir kanca (`OnRejected`), eksik bir şema kaydı ve
  bir test fabrikasının eksik kurulumu ancak dallar birleşince göründü.
  Ayrıntı: `specs/done/SCORECARD-ANALIZ.md` § Bulgu 2.
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

1. **Bir spec'e ya da bir ADR'ye dayanır.** PR başlığı spec numarasını taşır.

   ADR istisnası § 1'in `chore/` iznini işler kılar: kabul kriteri üretmeyen ama mimariyi
   değiştiren işler (dağıtım stratejisi, araç zinciri, kural düzeltmesi) spec'e sığmaz.
   Onlara spec numarası açmak spec kavramını sulandırırdı — dayanakları
   `docs/decisions/` altındaki ADR'dir ve PR onu referans gösterir.

   **Hiçbir dayanağı olmayan PR açılmaz.** İstisna dayanağın biçimindedir, varlığında değil.
2. **PR şablonu doldurulmuştur** (§ 5). Boş bırakılan alan yoktur.
3. **Pipeline yeşildir.** Kırmızı pipeline ile merge yoktur — "sonra düzeltiriz" yoktur.

   **Hangi kapı koştu, değişikliğe bağlıdır (ADR `0010`).** Backend **her zaman** koşar;
   Frontend yalnız `frontend/**` değiştiyse, E2E yalnız çalışan yığın (`src/**`,
   `frontend/**`, `scripts/**`, `*.props`, `*.slnx`) değiştiyse.

   Metin-PR'ında iki ayrı hâl görülür ve **karıştırılmamalı**:

   - **Frontend ve E2E `skipped` görünür.** İş tanımlı, koşulu sağlanmadı.
   - **`C3 eşikleri` HİÇ GÖRÜNMEZ.** `Performans` iş akışı `paths:` ile workflow
     düzeyinde filtreli, yani kuyruğa hiç girmiyor — atlanmış bir iş değil, var olmayan
     bir koşu. "Kapı nerede?" diye arayan biri `skipped` bulmayı bekler ve bulamaz.

   *İkisi de karşılanmamış şart sayılmaz* — atlama koşulu yalnız o kapının ölçemeyeceği
   bir diff için geçerlidir ve ADR `0010`'da ölçümüyle yazılıdır.

   **CI doğrulaması merge'den AYRI bir adımdır; aynı komuta zincirlenmez.** Önce sonuç
   okunur, sonra merge kararı verilir. Bu kural bir hatadan doğdu: CI kontrolü ile
   `gh pr merge` tek satırda zincirlenmişti ve dört kapı da kırmızıyken merge gerçekleşti;
   sonuç ancak merge olduktan sonra okundu. O gün zarar yoktu — merge edilen şey bir dosya
   taşımaydı ve kırmızı da geçici bir altyapı sorunuydu — ama kod içeren bir PR'da aynı
   hata `main`'i kırardı.

   Aynı disiplin test çıktısı için de geçerlidir: **çıkış kodu ve özet satırı birlikte
   okunur.** Çıktıyı kısaltan bir boru (`tail -3` gibi), Playwright'ın özet satırını kesip
   altı kırmızı testi "130 passed" diye gösterdi. Yeşil görünen çıktı yeşil değildir;
   yeşil olan çıkış kodudur.

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
7. **Düzenlemelerin commit'e girdiği doğrulanmıştır.** Commit'ten sonra `git show --stat`
   okunur. **Paylaşılan çalışma ağacında zorunludur** ve `--stat`'ın `0 0` gösterdiği bir
   dosya, düzenlenmiş sayılmaz.

   Bu kural ölçülmüş bir kayıptan doğdu (ADR `0008` / K5). `0010a`'nın kapanış ölçümleri
   PR #36'da **hiç commit'e girmedi**: o PR spec dosyasında `0 0` taşıyor, yalnızca bir
   `git mv`. Düzenlemeler iki paralel oturumun aynı çalışma ağacındaki index'i çözülürken
   `git reset` ile silindi. Commit mesajı "DoD 23/23" diyordu, dosya 20/23 diyordu ve fark
   **beş gün** görünmedi — çünkü hiçbir CI kapısı `specs/` altını okumuyor.

   Kuralın tek cümlesi: **bir ölçüm commit'e girmediyse yapılmamıştır.**

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
