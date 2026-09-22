# 0007 — Eşzamanlı Yazma ve Invariant Koruması

| | |
|---|---|
| **Durum** | **Tamamlandı** |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `BuildingBlocks` (ortak desen) · `Identity` · `Dealership` |
| **Bağımlı spec'ler** | `0003` (şubeler, tamamlandı) · `0006` (kimlik — bir örneği orada nokta çözümle kapatıldı) |

---

## Intent

Sistemde "her zaman en az bir X kalır" biçiminde kurallar var ve bunlar bugün **tek bir
kullanıcı varmış gibi** korunuyor: kod önce sayıyor, sonra karar veriyor, sonra yazıyor —
aradaki pencerede başka kimsenin aynı şeyi yapmadığı varsayılıyor.

**Galeri yöneticisi**, iki kişinin aynı anda yaptığı işlemin sistemi kullanılamaz hâle
getirmemesini istiyor. Bugün çektiği acı görünmez olanı: iki yönetici aynı dakikada iki
farklı kişiyi pasifleştirdiğinde galeride hiç yönetici kalmayabiliyor ve **sisteme bir daha
kimse giremiyor** — kurulum hesabı var olan hesaba dokunmadığı için oradan dönüş de yok.

Bu iş bittiğinde, "her zaman en az bir aktif yönetici/şube kalır" gibi kurallar **eşzamanlı
istekler altında da** tutar; bunu her modülün kendi yöntemiyle değil, evin tek bir deseniyle
yapar; ve deseni izlemeyen yeni bir yazma yolu eklendiğinde bunu **bir test söyler**.

---

## Requirements

- **R1.** Sistem, "en az bir N kalır" biçimindeki kuralları eşzamanlı istekler altında da
  korur: iki istek aynı anda son elemanı kaldırmaya çalıştığında **yalnızca biri** geçer.
- **R2.** Koruma, okuma–karar–yazma üçlüsünü **bölünemez** kılar. Kararın dayandığı sayı,
  yazma anında hâlâ geçerli olmak zorundadır.
- **R3.** Koruma **tek bir yerde tanımlanır** ve her modül onu kullanır; hiçbir modül kendi
  kopyasını yazmaz. *(Gerekçe: aynı kural üç yerde yazılsaydı, üçü de ayrı ayrı yanlış
  olabilirdi. Tanımın nerede duracağı **C2**'dedir.)*
- **R4.** Koruma, Context § Invariant envanteri tablosunda sayılan **her** kurala uygulanır.
  Envanter kapalıdır: orada olmayan bir kural bu spec'in kapsamında değildir.
  *(Veritabanı tekil indeksiyle korunan kurallar için bkz. X4.)*
- **R5.** Invariant kararına giren sayı **yalnızca ortak desenin ürettiği değer** üzerinden
  elde edilebilir. Deseni atlayan bir yazma yolu eklendiğinde bunu bir test bildirir; koruma
  gözden geçirmeye bırakılmaz. Testin arayacağı işaret budur — ham sayım ile yazmanın aynı
  komutta yan yana gelmesi.
- **R6.** Reddedilen istek, **iki farklı red türünü ayırt eden** kararlı bir hata koduyla
  döner:
  - **Kural gereği red** — kural gerçekten ihlal ediliyor (gerçekten son Yönetici/şube).
    Kalıcıdır; kullanıcıya "tekrar deneyin" **denmez**.
  - **Eşzamanlılık kaybı** — karar anındaki sayı yazma anında değişmiş. Tekrar denemek işe
    yarayabilir; kullanıcıya "tekrar deneyin" denir.

  İkisi de teknik ayrıntı sızdırmaz. *(Gerekçe: tek kod kullanılırsa, kural gereği reddedilen
  kullanıcıya sonsuza kadar denemesi söylenmiş olur.)*

---

## Constraints

### Uyulması zorunlu

- **C1.** Gerçek PostgreSQL üzerinde sınanır; SQLite in-memory ile eşzamanlılık testi
  yapılmaz — kilit ve izolasyon davranışı farklıdır (`docs/testing.md` § 1).
- **C2.** Modül sınırı korunur: ortak desen `BuildingBlocks`'ta, kullanımı her modülün
  kendi Application katmanında (`docs/architecture.md` § 2.B).
- **C3.** Yeni migration yalnız kendi şemasına dokunur.
- **C4.** Çözüm **çalışma zamanında DDL üretmez**; şema değişikliği yalnızca migration ile
  yapılır. Uygulama rolünün kalıcı DDL yetkisine ihtiyacı olmaz — `architecture.md` § 5 şema
  başına ayrı rol istiyor. *(C3 ile çelişmez: C3 migration'ın kapsamını, C4 çalışma zamanını
  sınırlar.)*
- **C5.** Hata gövdesi çevrilmiş metin taşımaz; uç kendi kataloğundan çevirir
  (Yasak Listesi 23).
- **C6.** **Hata kodu bir API sözleşmesidir, bir gösterim değildir.** Yanıt gövdesi kararlı
  `errorCode` taşır; ziyaretçiye ne kod ne de teknik ayrıntı gösterilir, uç kendi
  kataloğundan çevirir. → `docs/frontend.md` § 3.1.
- **C7.** Her iki red türü de `409` (durum çakışması) ile döner.
  → `docs/conventions.md` § 5.

### KAPSAM DIŞI

- **X1.** Genel bir "iyimser eşzamanlılık" (her tabloya `RowVersion`) getirmek — *neden:*
  bu spec'in derdi **invariant**'lar, her satırın kaybolan-güncelleme sorunu değil.
  İkisini birlikte çözmek işi ölçülemez hâle getirir.
- **X2.** Dağıtık kilit / harici koordinasyon (Redis vb.) — *neden:* sistem tek veritabanlı
  bir modüler monolit; veritabanının kendi kilidi yeterli ve bir bileşen daha eklemek
  `architecture.md`'nin sadeliğini bozar.
- **X3.** Outbox dağıtıcısının eşzamanlılığı — *neden:* teslimat zaten en-az-bir-kez ve
  idempotent; ayrı bir konu.
- **X4.** **Tekillik kuralları** — aynı slug'a sahip ikinci şube (`0003`), aynı giriş
  kimliğiyle ikinci personel (`0006`) — *neden:* bunlar sayma–karar–yazma deseni değildir;
  tek bir yazmada veritabanı tekil indeksiyle kırılırlar. Farklı bir mekanizma, farklı bir
  kriter kümesi.
- **X5.** **Kaybeden istekten sonra ekranın davranışı** (listenin tazelenmesi, seçimin
  korunması) — *neden:* bu spec sunucu tarafındaki korumayı ve hata sözleşmesini üretir;
  ekran davranışı `0006`'nın personel yönetimi ekranının kendi işidir.
  *(Hata kodlarının üç dildeki karşılığı bu spec'tedir — AC-17.)*

---

## Context

**Mevcut davranış — doğrulandı.** Depoda hiçbir modülde `Serializable` izolasyon,
`RowVersion` veya `IsConcurrencyToken` yok. İnvariant korumalarının klasik deseni şudur:

```csharp
int otherActiveManagers = await StaffQueries...CountAsync(...);   // oku
Result result = staff.Deactivate(otherActiveManagers);            // karar ver
await unitOfWork.SaveChangesAsync(ct);                            // yaz
```

### Invariant envanteri — kapalı liste

R4 yalnızca bu tabloya uygulanır.

| Kural | Yer | Bugünkü durum |
|---|---|---|
| **R12** — en az bir aktif Yönetici (pasifleştirme) | `Identity/Application/StaffCommands.cs` `DeactivateAsync` | **Yarı korumalı** — kilit var, ama agregat kilitten ÖNCE okunuyor (AC-3) |
| **R12** — en az bir aktif Yönetici (rol düşürme) | `Identity/Application/StaffCommands.cs` `ChangeRoleAsync` | **Korumalı** — aynı kilidi kullanıyor |
| **B5** — en az bir aktif şube | `Dealership/Domain/Branch.cs` `Close` | **Korumasız ve ulaşılamaz** — `Close` hiçbir yerden çağrılmıyor (`grep '.Close('` → sıfır isabet); `0003` § D1 gereği `Dealership`'in yazma ucu yok |

Tekillik kuralları bu tabloda **yoktur** ve kapsam dışıdır (X4).

### Bu spec'in gerçek teslimatı

Envanterin ilk iki satırı **bugün zaten yeşildir**: `0006`'nın nokta çözümü hem
pasifleştirmeyi hem rol düşürmeyi kilitle koruyor (doğrulandı:
[StaffCommands.cs:114-126](src/Modules/Identity/Identity/Application/StaffCommands.cs#L114-L126),
[StaffQueries.cs:52](src/Modules/Identity/Identity/Application/StaffQueries.cs#L52)).
Dolayısıyla bu spec'in yeni davranışı dört kalemdir:

1. **Bayat agregat** — kilit doğru alınıyor ama karar, kilitten ÖNCE okunmuş bayat bir
   kopyayla veriliyor; aynı hedefe gelen ikinci istek kaydı yeniden yazıyor (AC-3).
   *Ölçüldü: A grubunun referans koşusunda kırmızı.*
2. **Şube tarafı** — bugün korumasız; kapatma komutu hiç yok (D2).
3. **Ortak desen** — üç yerde üç çözüm olmasın diye (D1).
4. **Mimari test** — deseni atlayan yolu bildiren kapı (R5).

Kabul kriterleri bu ayrımı korur: **A grubu** var olan davranışın regresyon koruması,
**B–E grupları** yeni davranıştır.

### Bu daldaki durum

Bu spec `feature/0006-personel-kimligi-ve-oturum` dalında yazıldı. **`Leads` modülü bu dalda
yoktur** (yalnız `feature/0005-talep-olusturma` dalında; `main`'de de yok) ve bu spec'te
`Leads`'e dokunan tek bir gereksinim veya kriter yoktur — bu yüzden başlıktaki modül
listesinden çıkarıldı. `VehicleCatalog` da aynı nedenle çıkarıldı.

**Önceki kararlar.** `docs/architecture.md` § 7 Karar Defteri'nde eşzamanlılık üzerine bir
satır **yok** — bu spec'in çıktılarından biri o satırdır.

**Bilinen tuzak.** Yalnız "diğer" satırları kilitlemek işe yaramaz: iki istek iki farklı
satırı kilitler ve hiç serileşmezler. Kilit kümesi, karara giren **tüm** satırları kapsamalı
ve sabit bir sırada alınmalı (`ORDER BY id`), yoksa karşılıklı kilitlenme oluşur. 0006'daki
nokta çözüm bunu böyle yapıyor; ortak desen de yapmak zorunda.

### Verilen kararlar

İkisi de Takım Yöneticisi tarafından onaylandı.

**D1 — `Identity`'nin nokta çözümü ortak desene taşınır.**
*Gerekçe:* R3'ün tek gerekçesi "aynı kural birden çok yerde birden çok kez yazılmasın";
istisnayı ilk günden açmak deseni daha doğmadan deler. *Bilerek ödenen bedel:* çalışan ve
testi yeşil bir kodu değiştirme riski — bu yüzden taşıma, A grubundaki kriterlerin
**kesintisiz yeşil kalmasıyla** yapılır ve bu DoD'de aranır.
*Elenen alternatif:* "taşımanın bedeli faydasını aşarsa olduğu gibi bırakmak" — ölçüsü
tanımlanmamış bir kapsam kararını uygulama anına bırakır; kapsam kararı üyeye ait değildir.

**D2 — `Dealership`'e iç bir şube kapatma komutu eklenir; HTTP ucu yoktur.**
*Gerekçe:* B5'in koruması Application katmanında yaşar, ama bugün şube kapatan bir komut
yok — `Close` hiçbir yerden çağrılmıyor. Komut olmadan AC-6/AC-7 yalnızca testin kendi
kurduğu düzeneği doğrular, korumayı değil. *Sınır:* komut modül içinden ve testten
çağrılabilir; **HTTP ucu yayınlanmaz**, böylece `0003` § D1 (korumasız yazma ucu yok)
korunur. *Bilerek ödenen bedel:* üretim kodunda bugün hiçbir yerden çağrılmayan bir komut
durur; karşılığında şube yönetimi spec'i geldiğinde koruma hazır olur.

---

## Acceptance Criteria

> **A grubu bugün zaten karşılanıyor** ve regresyon koruması olarak durur; yeni davranışın
> kanıtı **B–E** gruplarıdır (Context § Bu spec'in gerçek teslimatı). Bu spec'te arayüz
> kriteri yoktur.

### A. Bugün karşılanıyor — regresyon koruması

> **AC-3 buradan çıkarıldı.** Ölçüldü ve karşılanmıyordu: aynı Yöneticiyi hedefleyen iki
> eşzamanlı istekte ikisi de başarılı oluyordu. B grubuna taşındı — bu spec'in yeni
> davranışı. Numarası korundu; kriter numaraları yeniden kullanılmaz.


- **AC-1.** İki eşzamanlı istek son iki aktif Yöneticiden **farklı** ikisini pasifleştirmeye
  çalıştığında yalnızca biri başarılı olur; diğeri `identity.last_active_manager` ile
  reddedilir.
- **AC-2.** AC-1'in ardından galeride **tam olarak bir** aktif Yönetici kalır.
- **AC-4.** İki eşzamanlı istek son iki aktif Yöneticiden farklı ikisinin rolünü düşürmeye
  çalıştığında yalnızca biri başarılı olur.
- **AC-5.** Pasifleştirme ve rol düşürme **birbirine karşı da** serileşir: biri
  pasifleştirirken diğeri rol düşürüyorsa ikisi birden geçemez.

### B. Yeni davranış — bayat agregat ve şube tarafı

- **AC-3.** İki eşzamanlı istek **aynı** Yöneticiyi pasifleştirmeye çalıştığında ikincisi
  hiçbir kaydı değiştirmez ve kararlı bir hata koduyla döner.

- **AC-6.** İki eşzamanlı istek son iki aktif şubeden farklı ikisini kapatmaya çalıştığında
  yalnızca biri başarılı olur; diğeri `branch.last_active_branch` ile reddedilir.
- **AC-7.** AC-6'nın ardından galeride **tam olarak bir** aktif şube kalır.
- **AC-8.** Şube kapatma komutunun HTTP ucu yoktur; komut yalnızca modül içinden
  çağrılabilir — mimari test bunu doğrular (D2).

### C. Yeni davranış — ortak desen

- **AC-9.** Ortak desen tek bir yerde yaşar; modüller onu çağırır, kendi kopyalarını yazmaz
  — mimari test bunu doğrular.
- **AC-10.** `Identity`'nin son aktif Yönetici koruması ortak deseni kullanır; modül içinde
  ayrı bir kopyası kalmaz (D1).
- **AC-11.** Invariant kararına giren sayı yalnızca ortak desenin ürettiği değer üzerinden
  elde edilir; modüllerin Application katmanında ham sayım ile yazmanın aynı komutta yan yana
  geldiği bir yol bulunmaz — mimari test bunu doğrular.

### D. Çakışma ve hata sözleşmesi

- **AC-12.** Kural gereği red ile eşzamanlılık kaybı **farklı** hata kodlarıyla döner; iki
  durum hata kodundan ayırt edilebilir.
- **AC-13.** Her iki red de `409` durum koduyla döner.
- **AC-14.** Reddedilen yanıtın gövdesi kararlı `errorCode` taşır; çevrilmiş metin, yığın
  izi, SQL metni veya kilit/transaction terimi taşımaz.
- **AC-15.** Ziyaretçiye/personele HTTP durum kodu, hata kodu, `traceId`, endpoint yolu veya
  istisna metni gösterilmez; gösterilen mesaj çeviri kataloğundan gelir.
- **AC-16.** Kural gereği reddin kullanıcıya gösterilen metni "tekrar deneyin" demez;
  eşzamanlılık kaybınınki der.
- **AC-17.** Bu spec'in ürettiği her hata kodunun TR, AR ve EN karşılığı vardır; hiçbir
  locale'de çeviri anahtarı ham hâliyle görünmez.

### E. Yan etkisizlik ve kilit davranışı

- **AC-18.** Reddedilen istekte hiçbir yan etki kalmaz: ne satır değişir ne outbox'a duyuru
  düşer.
- **AC-19.** Karara giren satırlar sabit bir sırada kilitlenir; eşzamanlı iki istek karşılıklı
  kilitlenmeye (deadlock) düşmez. Tur sayısı **AC-21**'dedir.
- **AC-20.** Test, çakışmanın gerçekten kurulduğunu gözler: ikinci isteğin birinci commit
  edene kadar **beklediği** doğrulanır.
- **AC-21.** Eşzamanlılık testleri gerçek PostgreSQL üzerinde koşar ve art arda **beş** turda
  da aynı sonucu verir (AP-03).

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [x] **A grubundaki kriterlerin bu spec'ten önce de yeşil olduğu** PR'da açıkça yazıldı;
      yeni davranışın kanıtı B–E gruplarıdır.
- [x] **D1 taşıması sırasında A grubu kesintisiz yeşil kaldı** — taşımadan önceki ve sonraki
      koşu çıktıları PR'da.
- [x] **Korumanın doğru sebeple yeşil olduğunun kanıtı:** koruma geçici olarak devre dışı
      bırakılarak koşulan turun çıktısı PR'a eklendi ve hangi kriterlerin kırmızıya döndüğü
      yazıldı. *(Korumasız yol depoda kalıcı olarak bırakılmaz.)*
- [x] Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [x] Mimari testler yeşil — modül sınırı ihlali yok; AC-8, AC-9 ve AC-11 mimari testle
      doğrulanıyor.
- [x] Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [x] Bu spec'in ürettiği hata kodlarının çeviri anahtarları TR/AR/EN üçünde de eklendi;
      eksik anahtar kontrolü yeşil (AC-17).
- [x] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [x] **Eşzamanlılık kararı `docs/architecture.md` § Karar Defteri'ne yazıldı.**
- [x] Görsel kanıt: bu spec'te **arayüz kriteri yoktur**; görsel kanıt maddesi
      **uygulanabilir değildir**. AC-17 bir çeviri kapısıdır, arayüz kriteri değildir.
- [x] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [x] Takım Yöneticisi onayladı ve squash-merge edildi.
- [x] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **1** | AC-3 A grubundan B grubuna taşındı: "bugün karşılanıyor" diye işaretlenmişti, ölçüldü ve karşılanmıyordu. Kriterin metni değişmedi, sınıflandırması değişti. Onaylanan 2. karar. — Onaylandıktan sonra spec kaç kez değişti? Yüksekse spec eksik yazılmıştır. |
| **Düzeltme turu sayısı** | **0** | PR #18 gözden geçirmeden henüz geri dönmedi. CI bir kez kırmızı verdi (Prettier kapısı) ama bu bir gözden geçirme turu değil, benim atladığım bir kapının bedeli. |
| **Bulgu gerçek/gürültü oranı** | — / — | **Henüz ölçülemez:** gözden geçirme yapılmadı. Ölçüt "gözden geçirmede çıkan bulgular" diyor; uygulama sırasında kendi bulduğum iki hata (aşağıda) bu tanıma girmiyor. 0006'nın kartında bu ayrım yapılmamış ve uygulama içi bulgular sayılmıştı — iki spec'in sayısı bu yüzden karşılaştırılabilir değil. Tanımın netleşmesini öneriyorum (bkz. Açık konular). |
| **Regresyon sayısı** | **2** | `0003/AC-15` ve `0003/AC-31`. `SubeKapatmaTests` ilk hâlinde paylaşılan fikstürde koşuyor ve seed şubelerini kapatıyordu. **Üretim davranışı bozulmadı** — bozulan test seviyesiydi; yine de daha önce yeşil olan iki kriter kırmızıya döndü ve sayılması gerekiyor (0006'da `LocaleYonlendirmeTests` aynı biçimde sayılmıştı). Merge'den önce düzeltildi. |
| **Kaçan hata** | **0** | *2026-09-22 itibarıyla* — **izleniyor**. Merge sonrası bulunan her kusur buraya tarihiyle eklenir (`specs/TEMPLATE.md` § SCORECARD). |

**Bu spec'ten çıkarılan ders:**
**Bir testin yeşil olması, ölçtüğünü sandığın şeyi ölçtüğü anlamına gelmez.** Bu spec'te
üç kez aynı yere düştük ve üçü de farklı kılıkta geldi:

1. 0006'nın kilidi doğruydu ve testi yeşildi — ama karar, kilitten **önce** okunmuş bayat
   bir kopyayla veriliyordu. Test bunu göremiyordu çünkü hiç *aynı* hedefe iki istek
   göndermemişti (AC-3).
2. `SubeKapatmaTests` yeşildi — ama yalnız tek başına koşarken. Paylaşılan seed'i
   mutasyona uğratıyor ve başka iki testi düşürüyordu.
3. AC-11'in mimari testi yeşildi — ve **hiçbir zaman kırmızıya dönemezdi**: dosyada
   `InvariantGuard` alt dizesini arıyordu, ama `InvariantGuardOptions` parametresi o
   dizeyi zaten geçiriyordu.

Üçünü de yeşil test değil, **sonda** yakaladı: korumayı bilerek bozup hangi kriterlerin
kırmızıya döndüğüne bakmak. Yeşil bir test bir iddiadır; sonda o iddianın kanıtıdır. Bu
yüzden sonda turu bu spec'te DoD şartıydı ve öyle kalmalı.

İkinci ders, birincinin ucuz hâli: **kapı listesinin tamamı koşulmadan "yerelde yeşil"
denmez.** Lint, tip kontrolü, çeviri anahtarı ve birim testleri koştum; `format:check`'i
atladım ve CI onu buldu.
