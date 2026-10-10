# SCORECARD Analizi — 002

**Tarih:** 2026-10-10 · **Kapsam:** `specs/done/` altındaki **on iki** spec'in tamamı ·
**Rol:** QA · **Önceki tur:** [SCORECARD-ANALIZ.md](SCORECARD-ANALIZ.md) (2026-09-22, ilk altı)

001 bir teşhis koydu: *"beş ölçütün üçü ölü."* Bu turun işi sayıları tekrarlamak değil,
**o teşhisin tutup tutmadığını** söylemek. İki turun arasında yirmi beş PR (#18–#42), altı yeni kart ve
beş süreç önerisinin uygulanması var.

> **Bu dosyanın adı bir karar sorusuydu.** 001 `SCORECARD-ANALIZ.md` adıyla yaşıyor ve
> `README.md` ona bu adla link veriyor. Seçenekler: (a) 001'i `-001` olarak yeniden
> adlandırıp simetri kurmak, (b) yanına ayrı dosya koymak.
>
> **(b) seçildi.** Gerekçe ölçüldü: `SCORECARD-ANALIZ.md` yoluna üç canlı atıf var —
> `specs/done/README.md:27`, `docs/git.md:60` ve `tests/Architecture.Tests/MigrationSchemaTests.cs:89`
> (bir C# doc yorumu) — ve **dört commit mesajı** (#17, #19, #20 ve 001'in kendi gövdesi)
> o yolu anıyor. İlk üçü yeniden adlandırmayla birlikte düzeltilebilir; commit mesajları
> **düzeltilemez** — git geçmişi değişmez. Yeniden adlandırma, bu projenin kendi ilkesiyle
> de çelişir: tarihli bir kayıt sonradan yeniden yazılmaz, adı da o kaydın parçasıdır.
> *Bedel:* `SCORECARD-ANALIZ.md` numarasız kalır ve üçüncü turda asimetri büyür. Kabul
> edilen bedel budur; karşılığı, hiçbir atfın bayatlamaması.

---

## Tablo — on iki kart, bugünkü hâliyle

Kartlardan birebir okundu; hiçbiri yorumlanmadı. `—` **ölçülmedi**, `0` **ölçüldü, sıfır
çıktı** (001'in Ö2'si). `⚠` işareti, **sayının kendi tanımının belirsiz kaldığı** yeri
gösterir — kartın yanlış olduğunu değil → Bulgu 4.

| Spec | Revizyon | Düzeltme turu | Dış bulgu (gerçek/gürültü) | Uygulama içi | Regresyon | Kaçan hata |
|---|---|---|---|---|---|---|
| `0001` Vitrin listesi | 1 | 1 | 12 / 0 | — | 1 | **1** |
| `frontend-001` Kabuk | 0 | — | — (yok) | — | 2 | 0 · izleniyor |
| `0002` Araç detayı | 1 | 0 ⚠ | — (yok) | 3 | 1 | 0 · izleniyor |
| `0003` Galeri & şubeler | 1 | — | — (yok) | 3 | 1 | **1** |
| `0005` Talep | 2 | 2 | 5 / 0 | 4 | 1 | 0 · izleniyor |
| `0006` Kimlik | 1 | 2 | 4 / 0 | 7 | 1 | **1** |
| *— 001'in çizgisi —* | | | | | | |
| `0007` Eşzamanlı yazma | 1 | 0 ⚠ | — (yok) | 2 | 2 | 0 · izleniyor |
| `0008` Personel yönetimi | 1 | 1 | 2 / 0 | 5 | 1 | 0 · izleniyor |
| `0009` Talep bildirimi | 1 | 2 | 6 / — | 9 | 1 | **1** |
| `frontend-002` Görsel dil | 3 | 0 ⚠ | 2 / — | 11 | 1 | *(boş)* ⚠ · izleniyor |
| `0010a` Araç kaydı | 0 ⚠ | 1 | 4 / 0 | 9 | 1 | **1** |
| `0010b` Araç düzenleme | 0 ⚠ | 1 | 3 / 0 | 11 | **0** | izleniyor |

**Toplamlar:** dış bulgu **38** (sekiz kartta ölçüldü, dördünde `—`), uygulama içi bulgu
**64** (onunda ölçüldü), regresyon **13**, kaçan hata **5**, gürültü **hiçbirinde bir sayı
değil**.

---

## S1 — Ö1–Ö5 ne oldu?

Beşi de kabul edildi ve beşi de uygulandı. Repoda doğrulandı, varsayılmadı:

| Öneri | Nerede uygulandı | Bugün repoda |
|---|---|---|
| **Ö1** `Kaçan hata` DoD'den çıksın, arşivde açık kalsın | `1463d7e` (#19) | `specs/TEMPLATE.md:161` — DoD maddesi *"`Kaçan hata` hariç — o `izleniyor` kalır"* |
| **Ö2** `0` ile `ölçülmedi` ayrılsın | `1463d7e` (#19) | `specs/TEMPLATE.md:181` — *"`0` ile `—` aynı şey değildir"* |
| **Ö3** Gürültü yalnız dış turdan + zorunlu QA turu | `1463d7e` (#19), `047db11` (#26) | `specs/TEMPLATE.md:148` DoD maddesi; ölçüt **iki sütuna ayrıldı** |
| **Ö4** Bir günden fazla kopmuş daldan PR açılmaz | `1463d7e` (#19) | `docs/git.md:53` — gerekçesi ve 001'e atfı ile |
| **Ö5** "Çağrılmayan kurulum yolu" için mimari test | `cb0e253` (#20) | `tests/Architecture.Tests/MigrationSchemaTests.cs:100` — `KurulumAdiminaKayitlidir_HerModulDbContexti` |

**Ama uygulanmak işe yaramak değildir.** Sayılar hangi öneride değişti:

**Ö5 karşılığını verdi — ölçülebilir biçimde.** `0009`'un kartı uygulama içi bulgu (1)
olarak şunu yazıyor: *"mimari test kırmızıydı — `NotificationsDbContext` kurulum adımına
kayıtlı değildi."* Bu, Ö5'in yazıldığı hatanın **tam olarak aynı sınıfı**: 001'in Bulgu
3'ündeki iki kaçan hata (`0001`'in şemaları, `0003`'ün `MigrateDealershipAsync`'i) da
"tanımlı ama hiçbir yerden çağrılmayan kurulum yolu"ydu. Test, yazıldıktan on iki gün
sonra üçüncüsünü **merge'den önce** yakaladı. Beş önerinin tek kanıtlı kazancı budur.

**Ö1 sütunu diriltti.** 001'de altı kartın altısında `-` olan alan bugün **beş kayıt**
taşıyor: `0001`, `0003` (ikisi 001'in Bulgu 3'ünden geriye dönük yazıldı), `0006`
(2026-10-10, #38), `0009` (2026-10-05, #29) ve `0010a` (`0010b` buldu, #40). Sütun artık
bilgi taşıyor. Taşıdığı bilgi ise rahatsız edici → Bulgu 2.

**Ö4 için neden-sonuç kurulamıyor, ama desen değişti.** 001'in üç regresyonu birleşimde
çıkmıştı; yeni altının **altısı da kendi dalında** çıktı ve merge'den önce kapandı
(→ Bulgu 6). Kuralın payı ayrıştırılamaz: aynı dönemde dallar kısaldı, PR'lar saatler
içinde merge edildi ve `0009` ile `0008` yine sekiz dakika arayla girdi (#27 19:19 UTC, #25
19:27 UTC — ikisi de 2026-10-04) ama bu kez çarpışmadılar. Kurala **atfedemem**; yalnız
"ihlal edilmedi ve zarar tekrarlamadı" diyebilirim.

**Ö2 ve Ö3 kuralda var, kartlarda tutmuyor.** İkisi de kendi tanımının ihlal edildiği
kartlar üretti → Bulgu 4 ve Bulgu 5.

---

## S2 — 001'in teşhisi tuttu mu?

001: *"beş ölçütün üçü ölü"* — `Kaçan hata`, `Düzeltme turu`, `gürültü oranı`.

**`Kaçan hata`: artık ölü değil.** Beş kayıt, hepsi tarihli ve mekanizmalı. Teşhis tuttu,
öneri işe yaradı, ölçüt yaşıyor.

**`Düzeltme turu`: doluyor, ama Ö2'nin kuralı tanımsız bir kelimeye dayanıyor.** Yeni
altının altısında da bir sayı var (`—` yok) — Ö2 bu kadarını başardı. Fakat kuralın
kendisi şöyle: *"`0`, **gözden geçirildi** ve hiç tur dönmedi demektir; gözden geçirme hiç
yapılmadıysa `—` yazılır"* (`specs/TEMPLATE.md:181`). **"Gözden geçirildi"nin kim
tarafından olduğu yazılmamış** ve üç kart tam o fay hattında duruyor — `0002`, `0007`,
`frontend-002`. Üçünün de ortak şekli aynı:

| | PR açıldı mı | Takım Yöneticisi onayı | QA oturumu | kart ne yazdı |
|---|---|---|---|---|
| `0002` | evet (#7) | `[x]` | yok (`— (dış gözden geçirme yok)`) | `0` |
| `0007` | evet (#18) | `[x]` | yok (*"triyajdan geçmeden merge edildi"*) | `0` |
| `frontend-002` | evet (#30) | `[x]` | **merge'den sonra** | `0` |

"Gözden geçiren" = Takım Yöneticisi ise `0` doğrudur; "gözden geçiren" = QA oturumu ise
üçü de `—` olmalıydı. Metinden iki okuma da çıkıyor, dolayısıyla **sütun bu üç kart için
karşılaştırılamaz.** Bunu kartların hatası olarak yazmıyorum; kuralın eksik yazılmış olması
olarak yazıyorum — ve #19'un kendisi de aynı belirsizlikte kaldı: `0003` ile
`frontend-001`'i `—` yaptı, `0002`'yi `0` bıraktı (`1463d7e`'nin `0002` diffi yalnız gürültü
oranı ile `Kaçan hata`ya dokunuyor).

**Gürültü oranı: hâlâ ölü, ama sebebi değişti.** On iki kartın **hiçbirinde** bir gürültü
sayısı yok. 001'in teşhisi "dışarıdan bakan kimse yok"tu ve o kısım **düzeldi**: yeni
altının beşinde gerçek bir dış tur koştu ve **17** dış bulgu getirdi. Yeni sebep başka ve iki
kart bunu açıkça yazıyor:

- `0009`: *"gürültü işaretini Takım Yöneticisi yaptı ve işaretlenenler bana hiç ulaşmadı —
  oranın bölenini bilmiyorum ve tahmin etmek ölçütün amacını bozar."*
- `frontend-002`: *"gözden geçirme bana beş maddelik bir iş listesi olarak geldi ve üçü
  bulgu değil adımdı."*

Yani **bölen hiçbir zaman yazarın eline geçmiyor.** 001 "ölçen kimse yok" dedi; 002 "ölçen
var, sonucu geri dönmüyor" diyor. Aynı sütun, farklı boşluk.

**Dördüncü ölçüt — `Uygulama içi bulgu` — yeni ve çalışıyor.** #26 ile ayrıldı ve yeni
altının **altısında da** dolu. Ortalama yükseliyor: 001'in kapsamında ölçülen dört kartın
ortalaması **4,25**; yeni altının **7,83**. İki okuma mümkün — iş karmaşıklaştı, ya da
yazar kendi işini daha çok sorguluyor. Kartların kendisi ikincisini destekliyor: `0007`'den
sonra **sonda** (korumayı bilerek bozup hangi testin kırmızıya döndüğüne bakmak) bir DoD
şartı oldu ve `0009`, `0010a`, `0010b`, `frontend-002` kartlarının bulgularının büyük
kısmı sondadan çıkıyor.

---

## S3 — README'nin dört sorusu

**(1) Revizyon sayısı yüksek mi? → Dokuz kartta bu sütun ÖLÇÜLEMEZ.** Ortalama 1,0 ve
"spec yazımı sağlam" gibi duruyor. Ölçüm bunu yalnız üç kart için destekliyor. Her spec
için, implementasyon commit'inin **ebeveyninde** `main`'de kaç kabul kriteri olduğunu
saydım (`git show <sha>^:specs/<ad>.md | grep -c '^- \*\*AC-'`):

| Spec | implementasyon öncesi `main`'de AC | bugün | implementasyon commit'i | kart ne diyor |
|---|---|---|---|---|
| `0001` | *dosya yok* | 35 | `6b75305` | 1 |
| **`frontend-001`** | **17** | **17** | `f57e8f2` | **0** |
| **`0002`** | **25** | **25** | `57055e0` | **1** |
| `0003` | *dosya yok* | 35 | `bc54f3b` | 1 |
| `0005` | *dosya yok* | 43 | `ba96761` | 2 |
| `0006` | *dosya yok* | 61 | `2d233c1` | 1 |
| **`0007`** | **21** | **21** | `fbb5e80` | **1** |
| `0008` | 2 | 33 | `cc0adcd` | 1 |
| `0009` | 17 | 32 | `601e2de` | 1 |
| `frontend-002` | *dosya yok* | 34 | `31f1d09` | 3 |
| `0010a` | *dosya yok* | 34 | `90af789` | 0 |
| `0010b` | 22 | 35 | `0019f50` (spec dosyasına **+366/−70**) | 0 |

Üç grup çıkıyor:

- **Tabanı olan üç spec — `frontend-001`, `0002`, `0007`.** Kriterler implementasyondan
  önce `main`'e girdi ve implementasyon onlara dokunmadı (17→17, 25→25, 21→21). Kartları
  **0, 1, 1** yazıyor. **Arşivdeki tek karşılaştırılabilir revizyon sayıları bunlardır** ve
  üçü de düşük — yani ölçüt çalıştığı yerde iyi haber veriyor. Üçünden yalnız `0007` bunu
  bir kurala uyarak elde etti (tam metin kendi `docs/` PR'ıyla, #14); `frontend-001` ile
  `0002`'nin spec dosyası `0001`'in implementasyon squash'ında (`6b75305`) tesadüfen
  `main`'e girmişti.
- **Tabanı olup implementasyon commit'inde yeniden yazılan üç spec — `0008`, `0009`,
  `0010b`.** `0008` iki kriterden otuz üçe, `0009` on yediden otuz ikiye, `0010b` yirmi
  ikiden otuz beşe. Dosya `main`'deydi, kriterler değildi.
- **Spec dosyası hiç olmayan altı spec — `0001`, `0003`, `0005`, `0006`, `frontend-002`,
  `0010a`.** Spec metni ile kodu aynı commit `main`'e soktu.

Son dokuzunda, ölçütün sorduğu *"onaydan sonra kaç kez değişti"* sorusunun bir "önce"si
yok. Düşük sayı orada iyi spec değil, **ölçülemeyen spec** demek — ve `0010a` ile
`0010b`'nin **0**'ı, `0002`'nin aynı sütundaki **1**'i ile aynı anlama gelmiyor.

İki tur arasındaki fark burada ters yönde değil: ilk altıda iki spec'in tabanı vardı, yeni
altıda **dört** spec'in dosyası `main`'deydi (`0007`, `0008`, `0009`, `0010b`). İyileşen
şey dosyanın erken commit'lenmesi; **iyileşmeyen şey, kriterlerin hâlâ kodla birlikte
gelmesi.** → Ö6, Ö7

**(2) Düzeltme turu yüksek mi? → Hayır.** Ölçülen on kartın ortalaması 1,0; en yüksek 2
(`0005`, `0006`, `0009`). Kriterlerin belirsizliğine ya da dilimlerin büyüklüğüne işaret
eden bir eğilim yok — bu, dört sorudan **cevabı rahat olan** tek soru. Üç kartın `0`'ı
tanım boşluğunda duruyor (→ S2) ama tanım netleşse de ortalama değişmez; değişecek olan
yalnız okunabilirliği. → Ö8

**(3) Gürültü oranı yüksek mi? → Bilinmiyor, ve bilinemiyor.** 38 dış bulgunun hiçbirinin
yanında bölen yok. "Gözden geçirme odağı yanlış yerde mi" sorusu on iki kart birikmesine
rağmen hâlâ cevapsız. → Ö12

**(4) Kaçan hata sıfırdan büyük mü? → Evet, beş. Ve hiçbirini bir kapı bulmadı.** Beşinin
nasıl bulunduğu:

| Kaçan hata | Nasıl bulundu |
|---|---|
| `0001` — şemalar yalnız geliştirme bayrağıyla uygulanıyordu | ADR 0002 hazırlanırken, beş spec sonra |
| `0003` — `MigrateDealershipAsync` çağrılmıyordu | `0006` üzerinde çalışılırken |
| `0009` — arka plan servisi kapanışta iki yanıltıcı `crit` | logları okurken |
| `0010a` — opsiyonel alan değeri para, metin olarak saklanıyordu | `0010b` üzerinde çalışılırken |
| `0006` — testin sahte saati zaman bombasıydı | **İLGİSİZ bir dalın** CI koşusu, dört gün sonra |

Dördünü "bir sonraki iş" buldu. Beşincisini CI buldu ama **4 gün 16 saat 49 dakika**
gecikmeli: `gh run list` çıktısına göre `2026-10-05T13:14` ile `2026-10-10T06:03` arasında
bu depoda **hiçbir koşu yok** ve `main` 6 Ekim'den beri kırmızıydı (#38'in gövdesi:
*"kod beş gündür değişmemişti"*). Bulan koşu bir spec işine değil,
`chore/dokuz-taslagi-commitle` dalına aitti.

README'nin cevabı — *"testler kritik yolu kapsamıyor"* — yarısı doğru. Diğer yarısı:
**testler koşmuyorsa kapsam da bir varsayımdır.** → Ö11

---

## S4 — Kartların KENDİSİ güvenilir mi?

Bu soru atlanamazdı ve cevap: **kısmen değil.** Beş ayrı kayıp/tutarsızlık sınıfı ölçüldü
ve hiçbiri bir kapı tarafından görülmüyor; altıncı başlık bu analizin kendi sınırı. Önce o
kapı boşluğu:

**`specs/` altını okuyan tek bir CI kapısı yok.** `grep -rn 'specs' .github/workflows/ scripts/`
→ **sıfır eşleşme**. İki iş akışı var (`ci.yml`, `perf.yml`) ve ikisi de spec dosyasına
bakmıyor. Aşağıdaki beş sınıfın hepsi bu boşluktan geçti.

### (a) Ölçüm yapıldı, commit'e girmedi — `0010a`

Kartın kendisi olayı yazıyor (`specs/done/0010a-panel-arac-kaydi.md:603-612`) ve bağımsız
doğruladım:

- `db8582e` (#36, *"DoD 23/23"* diyen commit) spec dosyasında **`0 0`** taşıyor — yalnızca
  `specs/{ => done}/` yolu değişmiş, tek satır düzenleme yok.
- Ölçümler beş gün sonra `5923b3f` (#40) ile **+45/−8** olarak geri geldi.
- Sebep kartta yazılı: *"iki paralel oturumun aynı çalışma ağacındaki index'ini çözerken
  yapılan `git reset`."*

Commit mesajı 23/23 derken dosya 20/23 diyordu ve fark beş gün görünmedi. Kartın kendi
dersi doğru: *"bir ölçüm commit'e girmediyse yapılmamıştır."* O ders **henüz kural
değil** — `docs/git.md § 4`'ü okudum, altı şartın hiçbiri düzenlemelerin commit'e girdiğini
doğrulamayı istemiyor. Kart bunu *"bu PR'da öneri olarak duruyor"* diye yazmış; öneri hâlâ
açık. → Ö10

### (b) `Tamamlandı` yazan iki kartta açık DoD kutusu

`README.md` net: *"Bir spec `Definition of Done`'daki tüm maddeler işaretlendiğinde ...
buraya taşınır"* ve *"SCORECARD boş bırakılmış spec taşınmaz."* Saydım
(`grep -c '^- \[ \]'`):

- `0008-personel-yonetimi-ekrani-eylemler.md:341` → `- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.`
- `0009-talep-bildirimi.md:410` → aynı madde, bir gerekçe notuyla (*"merge'i ajan yapamaz"*)

**İkisi de merge edildi:** PR #25 ve PR #27, ikisi de 2026-10-04 (`gh pr list`). Yani
kutular yanlış değil, **bayat**: olay gerçekleşti, kayıt geri dönüp kapanmadı. `0009` bunu
öngörüp not düşmüş, `0008` hiç not düşmemiş. Aynı sınıf (a) ile: arşiv, gerçeğin henüz
doğru olmadığı bir anda yazılıyor ve kimse geri dönmüyor.

### (c) Zorunlu dış gözden geçirme maddesi iki kartta hiç yok

Madde `1463d7e` (#19) ile **2026-09-22 16:27**'de şablona girdi. Hangi kartlarda var,
`grep -l 'Dış gözden geçirme yapıldı'`:

- **Var:** `0009`, `0010a`, `0010b`, `frontend-002`
- **Yok:** `0007` (merge 2026-09-22 **16:57** — maddeden otuz dakika sonra) ve `0008`
  (merge 2026-10-04, maddeden on iki gün sonra)

`0008`'in dış turu **gerçekten koştu** (kartı `2 / 0` yazıyor) — eksik olan tur değil,
maddenin kendisi. Bu, `frontend-002`'nin kendi dış bulgusunun aynısı: *"bu spec'in DoD'u
`specs/TEMPLATE.md` ile hizasızdı — dört madde eksikti."* Üç ölçülmüş vaka, tek sınıf:
**DoD listeleri şablondan elle kopyalanıyor ve sessizce kayıyor.** → Ö9

### (d) `Kaçan hata` hücresinde üç dialekt

| Yazım | Kartlar |
|---|---|
| `0` + *"izleniyor"* notu | `0002`, `0005`, `0007`, `0008`, `frontend-001` |
| `izleniyor` (değer hücresinde) | `0010b` |
| **boş hücre** + *"izleniyor"* notu | `frontend-002` |

Ö1 `izleniyor`'u yarattı ama **hangi hücreye** yazılacağını söylemedi. Sonuç: `0` bu sütunda
geri döndü — Ö2'nin diğer sütun için yasakladığı tam o belirsizlikle. `frontend-002`'nin
boş hücresi bir ortalamada da, bir `grep`'te de sessizce kaybolur. → Ö8

### (e) Bir kırmızı `main` koşusu hiçbir kartta yazılı değil

`gh run list --branch main` → 42 koşunun biri kırmızı: `35757541064`, `2026-09-22T16:57`,
`fbb5e807` (`0007`'nin merge'i). Üç işin üçü de **5 saniyede, hiçbir adım koşmadan**
düştü ve aynı dakikada `chore/0007-spec-arsivle` dalında da aynı şey oldu — kod kaynaklı
değil, altyapı kaynaklı. Bir sonraki push (bir spec `git mv`'i) yeşil döndü. Zararsız, ama
`main` üzerinde kırmızı bir koşu olarak duruyor ve hiçbir kartta geçmiyor. `docs/git.md § 4.3`
zaten bu vakanın kardeşinden doğmuş; kayda geçmeyen altyapı kırmızısı, bir sonraki gerçek
kırmızıyı "yine altyapıdır" diye okutur.

### (f) Bu analizin kendi sınırı

İki şeyi açıkça yazıyorum:

1. **On iki spec'in on ikisinin `Sahip`i bir ajan Developer oturumu**
   (`grep '^| \*\*Sahip'`). Bu depoda "dış gözden geçirme", *başka bir ajan oturumu*
   demektir — insan bir gözden geçiren hiçbir kartta yok. Ölçütü geçersiz kılmaz; ne kadar
   "dış" olabildiğinin sınırını çizer.
2. **Bu analizi yazan oturum tarafsız değil.** `0003`, `0005`, `0006`, `0007`, `0008`,
   `0009`, `frontend-002`, `0010a` ve `0010b`'nin kabul kriterlerini Analist şapkasıyla
   yazan oturumla aynı bağlamı taşıyorum. Bulgu 3'ün okuduğu `Spec revizyon sayısı`
   sütunu, doğrudan o işin ölçüsüdür. Kartları başka oturumlar doldurdu ve ölçümlerin
   hepsi komut çıktısıyla verildi — ama **bu dokuz spec'in QA turu bu oturuma
   verilmemeli** ve bu analizin revizyon bölümü ikinci bir gözle okunmalı.

---

## S5 — 001'in Bulgu 2'si hâlâ geçerli mi?

001: *"Her spec bir şey kırdı, ve kırılmalar sınırda kümeleniyor."* Regresyon sayısı
1,2,1,1,1,1 — istisnasız — ve üçü yalnız birleşimde çıkmıştı.

**Birinci yarısı kırıldı.** `0010b` **0** regresyonla kapandı: on iki spec'in ilk sıfırı.
Kartın gerekçesi kabul edilebilir — sözleşme kırılması bilerek yapıldı, DoD'da yazılı ve
`0010a`'nın kriterleri aynı şeyi ölçmeye devam ediyor.

**İkinci yarısı — "sınırda kümelenme" — artık geçerli değil.** Yeni altının regresyonları
nerede çıktı:

| Spec | Regresyon | Nerede |
|---|---|---|
| `0007` | `0003/AC-15`, `0003/AC-31` — paylaşılan fikstür mutasyonu | kendi dalı |
| `0008` | `i18n.test.ts` değişken torbası | kendi dalı |
| `0009` | 31 Host testi, `TestSchema`'da eksik şema | kendi dalı, **commit'ten önce** |
| `frontend-002` | Arapça tipografi Latin yığınına düştü | kendi dalı, merge'den önce |
| `0010a` | `0007/AC-8`'in mimari testi okuma ucunu da reddediyordu | kendi dalı |
| `0010b` | — | — |

**Altısının altısı kendi dalında çıktı ve merge'den önce kapandı.** Bir kez bile dal
birleşimi beklemedi. 001'in gözlediği sınır (iki dalın birbirini bulduğu an) bu turda
hiç zarar üretmedi.

**Ama yeni bir sınır doğdu ve orada kimse bakmıyor.** `0006`'nın kaçan hatası ne bir dalda
ne bir birleşimde çıktı — **takvimde** çıktı. Testin sahte saati `2026-09-22`'ye sabitti,
ondan üretilen çerez `Expires`'ı gerçek saat değerlendiriyordu ve 6 Ekim geçilince sekiz
test, kod hiç değişmeden kırmızıya döndü. `main` bu hâlde dört gün durdu çünkü o dört gün
boyunca **hiçbir koşu yoktu**.

Desen şöyle kaydı: *"kendi dalında yeşil olmak birleşimde yeşil olacağını göstermiyor"* →
**"bir zamanlar yeşil olmak bugün yeşil olduğunu göstermiyor."** Birincisini rebase kuralı
(Ö4) ele aldı; ikincisinin hiçbir karşılığı yok. → Ö11

---

## Bulgular — özet

**Bulgu 1 — Beş önerinin biri ölçülebilir kazanç verdi, biri sütun diriltti, üçü kuralda
kaldı.** Ö5 on iki gün sonra aynı sınıftan üçüncü hatayı merge'den önce yakaladı
(`0009`, uygulama içi bulgu 1). Ö1 `Kaçan hata`yı `-`'den beş kayda çıkardı. Ö2, Ö3 ve Ö4
şablona ve `docs/git.md`'ye girdi ama Ö2'nin kuralı tanımsız bir kelime bıraktı ve üç kart
o boşlukta duruyor, Ö3'ün DoD maddesi iki kartta hiç yok, Ö4'ün payı ayrıştırılamıyor.

**Bulgu 2 — Beş kaçan hatanın beşini de bir kapı bulmadı.** Üçünü bir sonraki işin akışı
(`0001` ADR 0002 hazırlanırken, `0003` `0006` üzerinde, `0010a` `0010b` üzerinde), birini
log okuması (`0009`), beşincisini **ilgisiz bir dalın** CI koşusu buldu — dört gün
gecikmeli (`0006`). Hiçbiri, o hatayı yakalamak için kurulmuş bir kapıdan geçmedi. "En
pahalı sütun" artık dolu ve dolduğu şey, kapıların bu sınıfı görmediğini söylüyor.

**Bulgu 3 — `Spec revizyon sayısı` on iki kartın dokuzunda ölçülemez.** Yalnız üçünde
kriterler implementasyon commit'inden önce `main`'deydi ve dokunulmadan kaldı
(`frontend-001` 17→17, `0002` 25→25, `0007` 21→21) — arşivdeki güvenilir revizyon sayıları
bu üçü. Diğer dokuzda karşılaştırılacak taban yok: altısında spec dosyası hiç yoktu,
üçünde dosya vardı ama implementasyon commit'i kriterleri yeniden yazdı (`0008` 2→33,
`0009` 17→32, `0010b` 22→35). Ortalama 1,0 "spec yazımı sağlam" demiyor; dokuz kart için
**"ölçüm yapılmadı"** diyor — ve bu, 001'in `Düzeltme turu` için teşhis ettiği hatanın
(`0` ile `—`'nin karışması) başka bir sütundaki tekrarıdır.

**Bulgu 4 — Üç sütunda ölçüt tanımı dialekt üretiyor; üçünde de hata kartta değil
tanımda.** (a) `Düzeltme turu`nda Ö2 `0` ile `—`'yi ayırdı ama **"gözden geçirildi"nin kim
tarafından** olduğunu yazmadı; `0002`, `0007` ve `frontend-002` tam o boşlukta `0` yazdı ve
her iki okuma da metinden çıkıyor. (b) `Kaçan hata` hücresinde **üç ayrı yazım** dolaşıyor
(`0` + not, `izleniyor`, ve `frontend-002`'de **boş hücre**), çünkü Ö1 `izleniyor`'u
yarattı ama hangi hücreye yazılacağını söylemedi. (c) `Spec revizyon sayısı`nda aynı olay —
onay anındaki CLARIFY turu — üç kartta **1** sayıldı (`0006` 41→61, `0009` 17→32,
`frontend-002` 30→34; üçü de gerekçesini yazdı) ve iki kartta **0** (`0010a`, `0010b`).
Aynı olay iki farklı sayı veriyorsa sütun karşılaştırılamaz.

**Bulgu 5 — Arşiv, gerçeğin henüz doğru olmadığı anda yazılıyor ve kimse geri dönmüyor.**
Üç ölçülmüş vaka: `0010a`'nın ölçümleri `git reset` ile silindi ve beş gün görünmedi;
`0008` ile `0009` `Tamamlandı` damgasıyla ama açık merge kutusuyla duruyor; `0007` ile
`0008`'in DoD'u zorunlu dış gözden geçirme maddesini hiç taşımıyor. Hepsinin ortak sebebi
tek: **`specs/` altını okuyan hiçbir kapı yok** (`.github/workflows` + `scripts` → sıfır
eşleşme).

**Bulgu 6 — Regresyonlar sınırdan dala çekildi; yeni sınır `main`'in kendisi.** Yeni
altının altı regresyonunun altısı kendi dalında çıktı, biri bile birleşimi beklemedi. Buna
karşılık `main` 2026-10-06'dan 10-10'a kadar kırmızı durdu ve bunu ne bir dal ne bir
birleşim üretti — yalnızca takvim. 4 gün 16 saat 49 dakika boyunca tek bir koşu yok.

**Bulgu 7 — Kapılar bulmuyor; sonda ve dış tur buluyor.** 208 CI koşusunun 8'i kırmızı ve
yalnız **dördü** bir feature dalında (`feature/0006` ×2, `feature/0007`, `feature/0008`).
`0009`, `0010a`, `0010b` ve `frontend-002` dallarında **tek bir kırmızı koşu yok** — ama o
dört kart **40 uygulama içi** ve **15 dış bulgu** kaydediyor. Uygulama içi bulgu ortalaması
4,25'ten 7,83'e çıktı ve kartlar bunun kaynağını adıyla yazıyor: `0007`'den sonra DoD şartı
olan **sonda** turu. Bu depoda kaliteyi üreten şey CI değil, kriteri bilerek kırma
alışkanlığı.

---

## Öneriler

Numaralandırma 001'in Ö5'inden devam eder. Kararı Takım Yöneticisi verir
(`AGENTS.md` Altın Kural 2); QA düzeltmeyi yapmaz (`docs/roles/qa.md § 8`).

**Ö6 — `Spec revizyon sayısı` ya tanımını değiştirsin ya kalksın.**
Bugünkü tanım commit'lenmiş bir taban varsayıyor ve dokuz kartta o taban yok. Önerilen
yerine: iki ölçülebilir alan — *"kriterler implementasyon commit'inden önce `main`'de
miydi? (evet/hayır)"* ve *"o anda kaç kriter vardı → bugün kaç"*. İkisi de tek komutla
doğrulanabilir ve dialekt üretmez.
*Bedel:* Kart iki satır uzar; eski on iki kart yeniden yazılmayacağı için eğilim sıfırdan
başlar ve üçüncü turda ancak iki veri noktası olur.

**Ö7 — Spec, implementasyondan AYRI bir PR'da `main`'e girsin ve orada donsun.**
Bulgu 3'ün kök nedeni budur. Spec'i ayrı PR'da `main`'e sokmak iki kez denendi — `0007`
(#14, `chore/0007-spec-tamamla`) ve `0009` (#24, `docs/0009-bildirim-spec-taslagi`) — ama
`0009`'un kriterleri yine implementasyon commit'inde 17'den 32'ye çıktı. Yani ayrı PR tek
başına yetmiyor; kuralın ikinci yarısı
gerekli: **spec PR'ı merge edildikten sonra her R/C/AC değişikliği sayılan bir revizyondur
ve kendi commit'ine girer.** Bunu **kuralına uyarak** yapan tek spec `0007`: tam metni
kendi `docs/` PR'ıyla (#14, `ad0c221`) `main`'e girdi, implementasyon ona dokunmadı ve
kartı on ikinin en güvenilir revizyon sayısını taşıyor. `frontend-001` ile `0002`'nin
tabanı da vardı ama **tesadüfen**: spec dosyaları `0001`'in implementasyon squash'ında
(`6b75305`) birlikte `main`'e girmişti. Taban, kuralın değil sıralamanın armağanıydı.
Bu önerinin bir ön şartı var: **hangi önekle açılacağı yazılı değil.** `docs/git.md § 1`
yalnız `chore/`'u spec'siz önek sayıyor ve `docs/` önekini hiç tanımıyor — buna karşın iki
PR o önekle açıldı (#24, #28). Ö7 kabul edilirse § 1'in önek listesi de bunu söylemeli.
*Bedel:* Her spec için bir ek PR ve bir ek onay turu. Hızı düşürür; karşılığı, revizyon
ölçütünün ilk kez anlam taşıması — ve `0007`'nin kartı bu bedelin ödenebilir olduğunu
gösteriyor.

**Ö8 — Üç hücrenin yazımı teklenişsin.**
(a) `Kaçan hata` değer hücresine **yalnız** `izleniyor` veya bir sayı yazılır; `0` ve boş
hücre yasaklanır. (b) `Düzeltme turu`nda `0`, **DoD'un dış gözden geçirme kutusu `[x]`
ise** yazılır; kutu açıksa veya madde yoksa `—`. Bu, bugün tanımsız olan "gözden geçirildi"
kelimesini var olan bir kutuya bağlar ve yorum payı bırakmaz. (c) `Spec revizyon
sayısı`nda onay anındaki CLARIFY turunun sayılıp sayılmayacağı tek cümleyle yazılır —
bugün beş kart ikiye bölünmüş durumda. Üçü de şablonda birer cümle.
*Bedel:* Yok sayılır. Kapanmış kartlar tarihli kayıttır, **düzeltilmez** — yeni yazım
sonraki spec'ten başlar ve bu dosya farkı açıkladığı için eski kartlar okunabilir kalır.

**Ö9 — DoD listesi şablondan elle kopyalanmasın.**
Üç ölçülmüş kayma var (`0007`, `0008`, `frontend-002`). Önerilen: spec'e özel madde
eklenebilir ama **şablonun maddeleri çıkarılamaz**; spec yazılırken DoD bölümü
`specs/TEMPLATE.md`'den birebir alınır ve spec'e özel maddeler **altına** eklenir.
*Bedel:* Şablona yeni madde girdiği gün, açık spec'lerin DoD'u da güncellenmeli — bugün
dokuz açık spec var.

**Ö10 — "Ölçüm commit'e girdi mi" `docs/git.md § 4`'e yazılsın.**
Bu öneri `0010a`'nın kartında zaten duruyor (`specs/done/0010a-panel-arac-kaydi.md:611`)
ve kural hâlâ yok — § 4'ün altı şartını okudum, hiçbiri bunu istemiyor. Önerilen madde:
*commit'ten sonra `git show --stat` okunur ve düzenlemelerin gerçekten girdiği doğrulanır;
bu, paylaşılan çalışma ağacında zorunludur.*
*Bedel:* Her commit'e bir adım. Düşük; karşılığı `0010a`'nın beş günü.

**Ö11 — `main` üzerinde zamanlı bir koşu olsun.**
Ölçüldü: 4 gün 16 saat 49 dakika hiç koşu yok ve `main` o sürenin dördünde kırmızıydı.
Önerilen: `ci.yml`'a bir `schedule` tetikleyicisi (günde bir).
*Bedel:* CI dakikası, ve daha önemlisi **kimsenin okumadığı bir gece kırmızısı gürültüdür.**
Koşuyu kurmak, onu okumayı bir kişiye bağlamadan işe yaramaz — bu yüzden öneri iki
parçalıdır ve ikinci parçası bir süreç kararı.

**Ö12 — Gürültü böleni ya yazılsın ya sütun kapansın.**
İki kart bölenin yazara hiç ulaşmadığını yazıyor. Önerilen: dış turu veren oturum
bulgularını teslim ederken **"n bulgu, bunların m'si gürültü"** satırını da verir; satır
yoksa hücre `— (işaretlenmedi)` olur, `0` olmaz. `docs/roles/qa.md § 5`'in doğrulama
özetine bir satır.
*Bedel:* QA'in teslim formatına bir satır. Alternatifi, sütunun on iki karttır boş
kalmaya devam etmesi.

**Ö13 — `specs/done/` için RAPOR EDEN bir kapı.**
Bulgu 5'in üç vakası da `specs/` altını kimsenin okumamasından geçti. Önerilen kontrol üç
şey sorar: arşivlenmiş bir spec'te açık `- [ ]` kutusu var mı, SCORECARD'da boş değer
hücresi var mı, DoD şablonun maddelerini taşıyor mu.
*Bedel:* İki tanesi gerçek. (1) Şablon değiştiği gün kapı bütün arşivi kırar; bu yüzden
kapı yalnız **belirli bir tarihten sonra kapanan** spec'lere bakmalı ve öncesi için açık
bir muafiyet listesi tutmalı — liste bakım yükü. (2) Kapı **düzeltmemeli, raporlamalı**:
tarihli bir kaydı otomatik düzelten bir kapı, bu dosyanın başındaki ilkeyi ihlal eder.
Bu yüzden öneri "kır" değil "bildir" biçimindedir ve bu hâliyle bir CI adımı mı yoksa
periyodik okumanın bir maddesi mi olacağı Takım Yöneticisi'nin kararıdır.

---

## 001 yeniden yazılmadı

001'in tablosu bugünkü kartlarla çelişiyor ve kendi metni bunun bilinçli olduğunu yazıyor:
*"tarihli bir analizi sonradan yeniden yazmak o analizin ne zaman neyi gördüğünü siler."*
Aynı ilke bu dosyanın okuduğu on iki kart için de uygulandı — hiçbirine dokunulmadı, hiçbir
sayı düzeltilmedi. Bulgu 4'teki dialektler ve Bulgu 5'teki bayat kutular **bulgu olarak**
kaldı; düzeltilmeleri Ö8 ve Ö9'un kararına bağlı ve düzeltme bu turdan **sonraki**
spec'lerde başlar.
