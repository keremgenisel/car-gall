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

`src/**`, `frontend/**`, `scripts/**`, `*.slnx`, `.github/workflows/**`.

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

*Bunun kabul edilebilir olmasının ölçülmüş sebebi:* `main` dalında **dal koruması yok**
(`gh api .../branches/main/protection` → `403`, private depoda Pro gerekiyor), yani zorunlu
durum kapısı diye bir şey yok ve atlanan iş hiçbir PR'ı kilitlemez.

**Tripwire:** Pro'ya geçilip dal koruması açılırsa, zorunlu kapı listesine **yalnız
Backend** konur. Frontend/E2E/Performans zorunlu işaretlenirse metin-PR'ları sonsuza kadar
"bekliyor" durumunda kalır.

### K5 — `schedule` ve `workflow_dispatch` her şeyi koşar

Gece koşusu bütünü ölçer; orada diff yok ve olmamalı. K6'nın (ADR 0008) ölçtüğü şey tam
olarak bu koşu.

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
