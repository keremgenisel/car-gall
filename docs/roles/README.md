# Roller

Bir oturum **tek bir rolle** açılır. Rol, o oturumda neyi okuyacağını, neyi yapabileceğini,
neyi **yapamayacağını** ve çıktısının hangi biçimde olacağını belirler.

Rol bir unvan değil, bir **kısıt kümesidir**. Kısıtı olmayan rol, rol değildir.

| Rol | Kart | Omurga rozetleri |
|---|---|---|
| **Analist** | `docs/roles/analist.md` | INTENT · CLARIFY · SPEC |
| **Developer** | `docs/roles/developer.md` | PLAN · BUILD · düzeltme |
| **QA** | `docs/roles/qa.md` | REVIEW · TEST · VERIFY · repro |

---

## Takım Yöneticisi

Takım Yöneticisi bir kod rolü değil, **karar merciidir** (`docs/domain.md` § 1). Rol kartı
yoktur; yetkisi kapsam, öncelik ve onaydır.

**2026-11-17'den beri bu rolü bir ajan taşıyor.** Depo sahibi rolü devretti ve iki şeyi
açıkça seçti:

- **Tam yetki.** Ajan-TM kapsam ve tasarım kararlarını verir ve PR'ları `main`'e **kendisi
  merge eder**; ek bir insan onayı beklenmez. `docs/git.md` § 4 madde 6 ve § 7'deki
  "ajan kendi PR'ını merge edemez" kuralının tek istisnası budur.
- **Kararlar ayrıca işaretlenir.** Ajan-TM'nin kararı belgelere
  `Takım Yöneticisi (ajan) kararı, <tarih>` diye yazılır. Bu tarihten önceki
  `Takım Yöneticisi kararı` kayıtları depo sahibinindir.

*Bedel, açıkça:* yazan ile onaylayan aynı oturum olabiliyor. Bu depoda kaçan hataları
yakalayan ikinci göz buydu; sahip bu riski bilerek kabul etti (önerilen alternatifler, PR
başına ayrı QA incelemesi ve merge'ün sahipte kalması, seçilmedi). Önlem olarak geri
kalan her kapı aynen geçerlidir: spec önce `main`'e girer, CI okunmadan merge yapılmaz ve
kendi yazdığı kodu merge eden Takım Yöneticisi onu "gözden geçirildi" diye **kaydetmez** —
QA'nın tek yön kuralı (Developer → QA yok) bu şapka için de geçerlidir.

Geri dönüşü olmayan veya depo dışına çıkan işler (depo ayarları, dal koruması, sırlar,
lisans) Takım Yöneticisi yetkisinde değildir; depo sahibine sorulur.

---

## Bir oturuma rol nasıl verilir

Takım Yöneticisi rolü oturum açılışında verir. Oturumu alan üye, ilk çıktısı olarak
**tek satırlık üstlenme** yazar ve ancak ondan sonra işe başlar:

```
ROL: <Analist|Developer|QA> · <spec no veya "spec yok"> · okundu: AGENTS.md + docs/roles/<rol>.md
```

```
ROL: Developer · 0001 · okundu: AGENTS.md + docs/roles/developer.md
ROL: QA · 0007 · okundu: AGENTS.md + docs/roles/qa.md
ROL: Analist · spec yok · okundu: AGENTS.md + docs/roles/analist.md
```

Kurallar:
- **Üstlenme satırı yazılmadan iş yapılmaz.** Rolsüz oturum, kısıtsız oturumdur.
- `okundu:` iddiası **gerçekten okunduğu anlamına gelir.** Okumadan yazılan bu satır,
  bu sistemdeki en pahalı yalandır — sonraki her şey ona dayanır.
- Rol verilmemişse üye rolünü kendi seçmez; sorar (öneri kuralı: bir rol **önerir**,
  gerekçesiyle).

---

## Şapka değişimi kuralı

Bir oturum rolünü değiştirebilir — ama **sessizce değil.**

Geçiş **ilan edilir** ve gerekçesi yazılır:

```
ŞAPKA: <eski rol> → <yeni rol> · gerekçe: <tek cümle>
```

```
ŞAPKA: Analist → Developer · gerekçe: spec 0001 onaylandı, uygulamaya geçiliyor
```

- **Dev oturumunda Analist → Developer geçişi İLAN EDİLİR.** Bir Developer oturumunun
  başında spec eksik çıkarsa üye Analist şapkasını takar, spec'i hazırlar, onay alır ve
  ancak ondan sonra ilan ederek Developer'a döner. İlan edilmeyen geçiş, spec'i yazan ile
  uygulayanın aynı kişi olduğunu gizler — bu sistemin en sık kaçırdığı hata biçimi.
- **Şapka değişimi yetkileri toplamaz.** Developer şapkasını takan kişi, Analist
  şapkasındayken yazdığı spec'i onaysız değiştiremez.
- **QA şapkası geçişlidir ama tek yönlüdür:** Developer → QA geçişi **yapılmaz.**
  Kendi kodunu gözden geçiren QA, QA değildir (bkz. `docs/roles/qa.md` § 8).

---

## Tüm rollerin ortak kuralları

Rol kartındaki kurallar bunların **üstüne** gelir, yerine geçmez.

**1. `AGENTS.md` okunur.**
Her oturum, hangi rol olursa olsun, `AGENTS.md` ile başlar. Yedi Altın Kural her rol için
bağlayıcıdır. Rol kartı hiçbir Altın Kuralı gevşetemez.

**2. Kanıtsız iddia yok.**
- "Çalışıyor" → çalıştırıldı mı, çıktısı ne?
- "Test geçiyor" → kaç test, hangi komut, çıktı nerede?
- "Bu dosyada şöyle yazıyor" → hangi dosya, hangi satır?
- "Tamamlandı" → hangi kriter, hangi kanıt?

Doğrulanmamış bir şey **doğrulanmamış olarak** raporlanır. Test kırmızıysa kırmızı denir,
çıktısıyla birlikte. Atlanan adım atlandı denir. Bu, `AGENTS.md` § Raporlama Dürüstlüğü'nün
rol düzeyindeki karşılığıdır.

**3. Belirsizlikte AP-10.**
Bir şey belirsizse veya dokümanla kod çelişiyorsa: **VARSAYMA.**
Seçenekleri bedelleriyle getir, Takım Yöneticisi'ne sor. → `docs/ap.md`

Bir AP tetiklendiğini düşünüyorsan **kodunu söyle ve yöneticiden promptu iste**;
AP'nin tam promptunu yönetici çalıştırır.

**4. ÖNERİ KURALI.**
Soru, bulgu, seçenek veya açık konu **önerisiz sunulmaz.** Her biri kendi **önerisi ve
gerekçesiyle** gelir. **Kararı Takım Yöneticisi verir; onaysız öneri uygulanmaz.**

Bu kural bir nezaket değil, bir iş bölümüdür: üye bağlamı taşır, yönetici kararı taşır.
Önerisiz gelen soru, kararı da yöneticiye yıkar — bu kuralın engellemek istediği şey tam
olarak budur.

```
Bulgu: …
Öneri: …
Gerekçe: …
Bedel: …          ← öneri bir şeyi feda ediyorsa neyi feda ettiği yazılır
```

**5. Kapsam üyeye ait değildir.**
Hiçbir rol kapsamı kendi başına genişletemez veya daraltamaz. Kapsam değişikliği
**önerilir, uygulanmaz** (AP-07 / AP-08).
