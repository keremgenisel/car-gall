# 0010 — CI dakika bütçesi: hangi kapı hangi değişiklikte koşar

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-12
- **Bağlam:** GitHub Actions dakikası tükendi (ölçüldü); depo **private kalıyor**
  (Takım Yöneticisi kararı 2026-10-12)

## Bağlam

2026-10-12'de GitHub Actions dakikası tükendi. Belirti ölçülerek doğrulandı, varsayılmadı:
`a19a340` üzerindeki dört işin dördü de **3–4 saniyede, sıfır adımla ve log üretmeden**
düştü (`10:45:28Z → 10:45:31Z`, `adim=0`, `log not found`). İş hiç başlamadı; bu, private
bir depoda dâhil dakikaların bitmesinin imzasıdır.

**Kalan dakika ve sıfırlanma günü bu ADR'ye yazılamadı:** billing API `user` kapsamı
istiyor, oturumun token'ında yok (`404`). Yani bu karar "ne kadar kaldı" bilgisi
*olmadan* alındı ve dayanağı tüketim tarafındaki ölçüm.

**Tüketim ölçümü.** Bir `CI` koşusunun makine zamanı üç işin toplamıdır:

| İş | Süre (ölçüldü, `38233473383`) |
|---|---|
| Frontend — lint, tip, test, build, çeviri | 49 s |
| Backend — build, xUnit, mimari testler, migration | 7 dk 53 s |
| E2E — kritik akışlar, üç durum ve RTL | 11 dk 37 s |
| **Toplam** | **≈ 20 makine dakikası** |

`Performans` (C3 eşikleri) ayrıca ≈ 3 dakika. **2026-10-12'de 26 koşu** oldu — kabaca
**300 makine dakikası, tek günde**.

**Asıl bulgu: bu dakikaların büyük kısmı etkilenmesi imkânsız işlere gitti.** O günün altı
PR'ından dördü yalnız metin değiştirdi — #62 (5 dosya), #63 (**1 dosya**), #65 (2 dosya),
#67 (3 dosya) — ve her biri Frontend + E2E + Performans'ın tamamını koşturdu. Bir `.md`
dosyası E2E'yi etkileyemez; o on bir buçuk dakika hiçbir şey ölçmedi.

## Kararlar

### K1 — Backend işi **her zaman** koşar, yol koşulu yok

*Gerekçe ölçümle:* mimari kapılar kendi klasörünün dışını okuyor ve bir metin değişikliği
onları gerçekten kırabiliyor.

- `ArsivKapisiTests` `specs/done/` ve `specs/TEMPLATE.md`'yi **canlı** okuyor.
- `YayinSiniriTests` kart metnini okuyor ve **#61'de tam bu yüzden kırıldı**: kart
  `specs/done/` altına taşınınca kapı depo kökünü bulamadı. O PR yalnız metin
  değiştiriyordu.
- `tests/Host.Tests/Theme/VarsayilanPaletTests.cs` `frontend/src/shared/design/tokens.css`'i,
  `tests/VehicleCatalog.Tests/Domain/AlanEtiketiKapisiTests.cs` `frontend/src/...` altını
  okuyor — yani **frontend'i değiştiren bir diff de Backend'i ilgilendirir.**
- `tests/VehicleCatalog.Tests/Integration/DuyuruTests.cs` `docs/architecture.md`'nin olay
  akışı bloğunu **canlı** okuyor. Bu, dış denetimin bulduğu beşinci okuyucu ve ilk dördü
  gibi `Architecture.Tests` içinde DEĞİL, bir **modül** test projesinde — yani "kapılar
  Architecture'da toplanmıştır" varsayımı da yanlış.

  *Ayrıca kayda değer:* o test depo kökünü **denetlediği dosyanın varlığıyla** arıyor
  (`while (… !File.Exists(…/docs/architecture.md))`). Aynı kalıp `#61`'de `YayinSiniriTests`'i
  kırmıştı. `docs/architecture.md` taşınacak bir dosya değil, ama kalıp aynı ve ayrı bir
  iş olarak düzeltilmeli — bu PR'ın kapsamı değil.

Backend'i koşullamak, bu üç kapıyı sessizce atlanabilir kılardı. 8 dakika, bu riskin
alınmayacağı kadar ucuz.

### K2 — Frontend işi yalnız `frontend/**` veya workflow değiştiğinde koşar

*Gerekçe ölçümle:* altı `check:*` betiğinin **hiçbiri** `frontend/` dışını okumuyor.
Taradım: okudukları yollar `../src`, `../e2e`, `../index.html`, `../package.json`.
`docs/` yalnızca hata mesajlarında ve yorumlarda anılıyor. Yani bir `docs/` ya da `specs/`
değişikliği bu işi kıramaz.

*Bedel:* betik ile doküman arasındaki tutarlılık (örneğin `check:structure`'ın envanteri
`docs/frontend.md` § 4.2 ile hizası) kod tarafında **sabit yazılı** olduğu için, dokümanı
değiştirip betiği değiştirmemek sessizce bir kayma üretir. Bu kayma zaten bugün de
koşuyla yakalanmıyordu — kapı dokümanı okumuyor.

### K3 — E2E yalnız **çalışan yığını** değiştiren bir yol değiştiğinde koşar

`src/**`, `frontend/**`, `scripts/**`, `*.slnx`, **`*.props`**, `.github/workflows/**`.

`*.props` dış denetimin bulgusu: `Directory.Build.props` hiçbir yol kümesinde yoktu, oysa
`TreatWarningsAsErrors` dâhil **bütün** projelerin derleme ayarını o dosya veriyor. Backend
her koşuda koştuğu için derleme kapısı yine denetlenirdi, ama E2E — Host'u derleyip ayağa
kaldıran iş — atlanırdı.

*Gerekçe:* E2E tüm yığını ayağa kaldırıyor (Postgres + Host + Vite + Playwright) ve
yığında yer almayan bir dosya sonucu değiştirmiyor.

**`tests/**` bu listede bilerek YOK.** Bir xUnit ya da mimari test dosyası E2E sonucunu
değiştiremez; onu zaten her koşuda koşan **Backend** ölçüyor. Ölçülmüş kazanç: #61 ve
benzeri kapanış PR'ları (kart + ADR + bir test dosyası) artık E2E'yi tetiklemiyor —
tek başına 11,5 dakika. `frontend/e2e/**` zaten `frontend/**` kapsamında, yani kanıt
spec'i değişince E2E koşmaya devam ediyor.

### K4 — `Performans` (C3) aynı yol kümesiyle **workflow düzeyinde** filtrelenir

PR'da `paths:` filtresi kullanılıyor; metin-PR'ında iş **hiç kuyruğa girmez** (sıfır
dakika, `changes` işi bile koşmaz) ve `C3 eşikleri` adlı kapı o PR'da **hiç görünmez**.

*Bunun kabul edilebilir olmasının 2026-10-12'de ölçülmüş sebebi:* `main` dalında **dal
koruması yoktu** (`gh api .../branches/main/protection` → `403`, private depoda Pro
gerekiyor), yani zorunlu durum kapısı diye bir şey yoktu ve atlanan iş hiçbir PR'ı
kilitlemiyordu.

**Tripwire:** Pro'ya geçilip dal koruması açılırsa, zorunlu kapı listesine **yalnız
Backend** konur. Frontend/E2E/Performans zorunlu işaretlenirse metin-PR'ları sonsuza kadar
"bekliyor" durumunda kalır.

**TRIPWIRE UYGULANDI (2026-10-15).** Depo public yapıldı, dal koruması kuruldu ve zorunlu
kapı listesine **yalnız Backend** kondu. Yani yukarıdaki paragrafın gerekçesi artık
geçersiz — ama sonucu aynı: atlanan iş hiçbir PR'ı kilitlemiyor, çünkü zorunlu olan tek
kapı her koşuda koşuyor. Ölçüm (`gh api .../branches/main/protection`):

| Ayar | Değer | Neden |
|---|---|---|
| `required_status_checks.contexts` | **1**: `Backend — build, xUnit, mimari testler, migration şema kontrolü` | K1 gereği her koşuda koşan tek iş |
| `strict` (main ile güncel olma) | **kapalı** | açık olsaydı yığın PR'lar her merge'den sonra yeniden taban alırdı; bu depoda o maliyet iki kez ödendi |
| `required_pull_request_reviews` | **yok** | QA turları GitHub dışında yapılıyor; zorunlu olsaydı hiçbir PR merge edilemezdi (beş PR'ın beşinde 0 inceleme). `docs/git.md` § 4.6 insanda kalıyor |
| `enforce_admins` | **kapalı** | TM istisnası gerektiğinde yol kapanmasın (#69/#70 tam öyle merge edildi) |
| `allow_force_pushes` · `allow_deletions` | **kapalı** | `docs/git.md` § 7'nin makine karşılığı |
| `required_linear_history` | **açık** | squash-merge-istisnasız kuralıyla aynı şey |

*Bu paragraf bir bayatlama düzeltmesidir:* yukarıdaki "koruma yok" cümlesi yazıldığı gün
doğruydu ve **silinmedi**; yanına bugünün ölçümü eklendi. Aynı dosyada biri doğru biri
bayat iki cümle bırakmak, ikisini de güvenilmez yapardı.

### K4.1 — Koşul **atlamaya değil koşmaya** meyleder (fail-safe)

Ağır işlerin koşulu şu biçimde yazılır:

```yaml
if: ${{ !cancelled() && needs.changes.outputs.<ad> != 'false' }}
```

Üç yazım denendi ve ikisi ölçümle elendi:

| Yazım | Neden olmaz |
|---|---|
| `== 'true'` | #68'in ilk koşusunda `changes` düştü (dakika bitmişti), çıktı boş kaldı ve Frontend ile E2E **SKIPPED** oldu — **tespitin kendi arızası kapıları sessizce atlatıyordu** |
| `always() && …` | GitHub'ın belgelediği davranışa göre `always()` **iptal sırasında da** `true` verir, yani iş iptal EDİLMEZ. `cancel-in-progress: true` kurulu olduğundan üst üste iki push'ta eski koşunun E2E'si (11,5 dk) sonuna kadar koşardı — dakika bedava olsa bile boşa akan zaman. *(Dış denetimin BULGU-1'i.)* |
| **`!cancelled() && …`** | Fail-safe'i korur: **düşen ya da atlanan** bir iş, iptal edilmiş bir iş değildir. İptalde durur. |

`!= 'false'` kısmı kasıtlı: boş çıktı "bilmiyorum" demektir ve bilmediğimizde koşarız. Bu,
`changes` işinin içindeki "taban okunamadı → hepsini koş" dalının dış kardeşi — aynı ilke,
iki yerde.

**YAML tuzağı, yazarken ölçüldü:** `if: !cancelled() && …` ayrıştırılamaz. Düz bir skalar
`!` ile başlayamaz, YAML onu **etiket** sayar ve iş akışının tamamı geçersiz olur. Koşul
bu yüzden `${{ }}` içinde; `ruby -ryaml` ile doğrulandı.

### K5 — `schedule` ve `workflow_dispatch` her şeyi koşar

Gece koşusu bütünü ölçer; orada diff yok ve olmamalı. K6'nın (ADR 0008) ölçtüğü şey tam
olarak bu koşu.

**`changes` işi o olaylarda hiç kuyruğa girmez** (dış denetimin BULGU-5'i: eskiden koşup
"hepsi true" diyerek çıkıyordu, yani boşa 15 saniye). İş artık
`if: github.event_name == 'pull_request' || github.event_name == 'push'` taşıyor. Ağır
işler yine koşuyor ve sebebi K4.1: **atlanan** bir bağımlılık `!cancelled()` koşulunu
engellemiyor, boş çıktı da "bilmiyorum" demek.

### K6 — Depo **private** kalıyor

Public olmak GitHub-hosted standart koşucularda dakika sorununu tümüyle bitirirdi; karar
buna rağmen private. Gerekçesi "sır sızar" DEĞİL — ölçüldü: izlenen dosyalarda `.env`,
`.pem`, `.key` ya da credential dosyası yok, parola/anahtar deseni taraması **0 eşleşme**,
`appsettings.json` yalnız `localhost` varsayılanı taşıyor ve seed yönetici parolası
dosyada değil yapılandırmada. CI'daki parolalar CI'ya özel. Gerekçe ürün tarafında:
satılacak olan şey kodun ve süreç külliyatının kendisi, `LICENSE` dosyası yok (GitHub
şartları herkese **görme ve fork** hakkı verir), güvenlik yüzeyi (kilitlenme eşiği, istek
limiti, iterasyon sayısı, hata kodları) okunur hâle gelir ve public dönemde alınan
fork'lar private'a dönünce yaşamaya devam eder.

### K6.1 — **K6 TERSİNE DÖNDÜ: depo public olacak** (2026-10-15)

K6'nın yukarıdaki gerekçesi **olduğu gibi duruyor ve silinmiyor** — o gün ölçülen şey
doğruydu. Değişen şey karar: Takım Yöneticisi 2026-10-15'te depoyu **public** yapmaya
karar verdi. Lisans: **tescilli, tüm hakları saklı** (`LICENSE`, ADR `0011` ile geldi).

*Kararı değiştiren ölçüm, güvenlik değil **duruş süresi** oldu.* K6 yazıldığında
"israfı kaldır, dakikalar 1 Kasım'da döner" dengesi makul görünüyordu. Sonra ölçüldü:
bu depo iki günde üç dilim kapattı (`0012a`, `0012b`, `0012c`) ve `0013`'ün uygulaması
bir-iki gün içinde hazır olur. Yani blok 20 gün değil, **~18 günlük tam duruş** demekti.
Public depoda GitHub-hosted standart koşucular ücretsiz; blok anında kalkıyor.

**K6'nın ürün gerekçeleri yanlışlanmadı, kabul edildi:** satılacak olan kodun ve süreç
külliyatının kendisi, fork'lar geri alınamıyor, güvenlik yüzeyi okunur hâle geliyor.
Bunların karşılığında alınan şey, 18 günlük duruşun kalkması.

**Bu kararın değiştirdiği ikinci şey bu ADR'ın kendi gerekçesi.** Yol filtreleri artık
bir **bütçe** önlemi değil — dakika bedava. Kaldılar, çünkü ikinci bir gerekçeleri var ve
o gerekçe ölçülü: metin-PR'ında E2E'yi koşturmak **11,5 dakika** geri bildirim gecikmesi
demek ve ölçtüğü hiçbir şey yok. *Bu ADR'ın adı "dakika bütçesi" olarak kalıyor; adını
değiştirmek, kararın hangi baskı altında alındığını silerdi.*

**Ön koşullar ADR `0011`'de** ve bu PR'dan ayrı bir PR'da teslim edildi: `LICENSE`,
depoda sabit kurulum parolasının kaldırılması, geçmişte yayınlanmış üç dizenin koşulsuz
reddi ve kapısı. Depo o iş merge edilmeden public yapılmamalı.

**K4'ün tripwire'ı artık canlı:** public depoda dal koruması ücretsiz geliyor. Açıldığı
gün zorunlu kapı listesine **yalnız Backend** konur; Frontend/E2E/`C3 eşikleri` zorunlu
işaretlenirse metin-PR'ları sonsuza kadar "bekliyor" kalır — çünkü ikisi `skipped`, C3 ise
hiç görünmüyor.

## Elenen alternatifler

| Alternatif | Neden elendi |
|---|---|
| **Depoyu public yapmak** | Dakika sorununu bitirirdi ama K6'daki ürün gerekçeleri ağır bastı; ayrıca geri dönüşü tam değil |
| **Harcama limitini yükseltmek** | Çalışır ve hâlâ masada — ama **önce kaldırılabilir israf** var: ölçüme göre tüketimin yarıdan fazlası etkilenmesi imkânsız işlerde. Para, ölçülmüş israfın üstüne harcanmaz |
| **Self-hosted runner (bu makine)** | Workflow'lar `services: postgres` kullanıyor; servis konteynerleri **yalnız Linux** koşucuda çalışır. macOS runner'da işler olduğu gibi koşmaz — yerel Postgres + workflow değişikliği gerekir, makine sürekli açık olmalı ve runner bakımı eklenir |
| **"Pipeline yeşil" kuralını kalıcı gevşetmek** (yerel ölçümle merge) | `docs/git.md` § 4'ün 3. maddesinin temelini kaldırır. Dakikalar tükendiği sürece **tek seferlik** bir TM istisnası olarak kullanılabilir, kalıcı politika değil |
| **Koşu sayısını elle azaltmak** (daha az push) | Davranış değişikliği, mekanizma değil; ilk unutulduğunda biter. `concurrency` + `cancel-in-progress` zaten kurulu ve doğru çalışıyor |

## Sonuç

Metin-PR'ında koşan tek ağır iş Backend kalır: **≈ 20 + 3 dakika yerine ≈ 8 dakika**.
Kod PR'ında hiçbir şey değişmez — her kapı eskisi gibi koşar, yalnız `changes` işi kadar
(~15 s) gecikme eklenir.

Doğrulama bu turda **yerelde** yapıldı, çünkü dakika yok. Tespit çekirdeği
workflow'dan **aynen çıkarılıp** dokuz gerçek PR aralığında koşturuldu ve dokuzu da
beklenen sonucu verdi:

| PR | Değişiklik | frontend | kod | Koşacak |
|---|---|---|---|---|
| #62 | 5 dosya, metin | `false` | `false` | yalnız Backend |
| #63 | **1** `.md` | `false` | `false` | yalnız Backend |
| #65 | kart + ADR | `false` | `false` | yalnız Backend |
| #67 | ADR + AGENTS + şablon | `false` | `false` | yalnız Backend |
| #61 | kart + ADR + **bir test** | `false` | `false` | yalnız Backend |
| #57 | kart + ADR + **bir `.cs`** | `false` | `true` | Backend + E2E |
| #52 | 12 dosya, `src/**` | `false` | `true` | Backend + E2E |
| #64 | 73 dosya | `true` | `true` | hepsi |
| #66 | test + betik + CI | `true` | `true` | hepsi |

**Sınama iki kez kendi girdimi yanlışladı ve bu kayda değer:** bir kez yanlış taban
commit'i verdim (#63 için `2255370`, oysa tabanı `d969dfb`), bir kez de #57'yi
"metin-PR'ı" sanıp beklentiyi yanlış yazdım — oysa o PR `MediaAssetContracts.cs`'e de
dokunuyordu ve kural haklı olarak `kod=true` dedi. Yanlış olan mantık değil, benim
beklentimdi; **sınamanın değeri tam olarak buydu.**

**CI'ın kendisi bu değişikliği henüz koşmadı.** Bu PR `.github/workflows/**`
değiştirdiği için kuralın kendi tanımı gereği tüm işleri tetikler — yani dakikalar
döndüğünde ilk koşu bu kararı baştan sona ölçer.
