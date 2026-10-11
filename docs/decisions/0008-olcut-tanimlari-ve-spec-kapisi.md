# 0008 — Ölçüt tanımları, spec'in donma anı ve `specs/` kapısı

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-10
- **Bağlam:** `specs/done/SCORECARD-ANALIZ-002.md` — ikinci periyodik okuma, öneriler Ö6–Ö13
  (Takım Yöneticisi onayı 2026-10-10; analiz PR #43, bu ADR ile aynı turda)

## Bağlam

İkinci SCORECARD turu on iki kartı birlikte okudu ve 001'in teşhisinin yarısının tuttuğunu
ölçtü. Ö1–Ö5'in beşi de uygulanmıştı; yalnız **Ö5** ölçülebilir bir kazanç verdi ve **Ö1**
ölü bir sütunu diriltti. Geri kalanı kuralda kaldı, çünkü üçü de **tanımsız bir kelimeye**
ya da **hiçbir kapının okumadığı bir dosyaya** dayanıyordu.

İki ölçüm bu ADR'nin tamamını açıklıyor:

1. **On iki kartın dokuzunda `Spec revizyon sayısı` ölçülemez.** Her spec için
   implementasyon commit'inin ebeveyninde `main`'de kaç kriter olduğu sayıldı: yalnız üçünde
   taban var ve dokunulmadan kalmış (`frontend-001` 17→17, `0002` 25→25, `0007` 21→21).
   Altısında spec dosyası hiç yoktu; üçünde dosya vardı ama implementasyon commit'i
   kriterleri yeniden yazdı (`0008` **2→33**, `0009` 17→32, `0010b` 22→35). Ortalama 1,0
   "spec yazımı sağlam" demiyor, dokuz kart için **"ölçüm yapılmadı"** diyor.

2. **`specs/` altını okuyan tek bir CI kapısı yok** (`grep -rn specs .github/workflows/ scripts/`
   → sıfır eşleşme). `0010a`'nın kapanış ölçümleri bu yüzden beş gün kayıp durdu, iki kart
   `Tamamlandı` damgasıyla ama açık merge kutusuyla duruyor, ve zorunlu dış gözden geçirme
   maddesi **on açık spec'in onunda da yok**.

## Kararlar

Sekizi birlikte kabul edildi. Her biri analizdeki öneri numarasını taşır.

### K1 (Ö6) — `Spec revizyon sayısı` yerine ölçülebilir iki alan

Sütun kaldırılmıyor, **tanımı değişiyor**: "onaydan sonra kaç kez değişti" sorusu bir
commit'lenmiş taban varsayıyor ve dokuz kartta o taban yok. Yerine kartta iki şey yazılır:
kriterler implementasyon commit'inden **önce `main`'de miydi** (evet/hayır) ve **o anda kaç
kriter vardı → bugün kaç**. İkisi de tek komutla doğrulanabilir.

*Bedel:* Kart iki satır uzar ve **eğilim sıfırdan başlar** — eski on iki kart yeniden
yazılmayacağı için üçüncü turda ancak iki veri noktası olur.

### K2 (Ö7) — Spec, implementasyondan ayrı PR'da `main`'e girer ve orada donar

K1'in kök nedeni budur: ölçüt değil, sıralama bozuk. Spec'in R/C/AC bölümleri kendi PR'ında
`main`'e girer; o commit'ten sonra her R/C/AC değişikliği **sayılan bir revizyondur** ve
kendi commit'ine girer. Bunu kuralına uyarak yapan tek spec `0007` oldu (tam metin #14,
`ad0c221`) ve kartı on ikinin en güvenilir revizyon sayısını taşıyor.

Önek de yazılıyor: § 1 bugüne kadar yalnız `chore/`'u spec'siz önek sayıyordu ve `docs/`
önekini **hiç tanımıyordu** — buna karşın iki PR (#24, #28) o önekle açılmıştı. Spec ve
doküman işleri artık `docs/` önekini adıyla kullanır.

*Bedel:* Her spec için bir ek PR ve bir ek onay turu; hız düşer.

### K3 (Ö8) — Üç hücrenin yazımı teklenir

- `Kaçan hata` değer hücresine **yalnız** `izleniyor` veya bir sayı yazılır. `0` ve boş
  hücre yasak. (Bugün üç dialekt dolaşıyor ve `frontend-002`'nin hücresi **boş**.)
- `Düzeltme turu`nda `0`, **DoD'un dış gözden geçirme kutusu `[x]` ise** yazılır; kutu
  açıksa veya madde yoksa `—`. Ö2 `0` ile `—`'yi ayırmıştı ama *"gözden geçirildi"*nin kim
  tarafından olduğunu yazmamıştı; `0002`, `0007` ve `frontend-002` tam o boşlukta durdu ve
  iki okuma da metinden çıkıyordu. Kural artık var olan bir kutuya bağlı, yorum payı yok.
- `Spec revizyon sayısı`nda onay anındaki CLARIFY turunun sayılıp sayılmayacağı yazılır:
  **sayılır.** Bugün beş kart ikiye bölünmüş durumda (`0006`, `0009`, `frontend-002` **1**;
  `0010a`, `0010b` **0**).
- **R/C ile AC ayrı PR'larda `main`'e giriyorsa donma anı, AC'yi taşıyan PR'dır;** ondan
  önceki R/C değişimi revizyon **sayılmaz**. *(2026-10-11, spec `0012b` / D38, Takım
  Yöneticisi onayı.)* Boşluk `0012a`'nın kapanışında ölçüldü: R/C #54'te, R9 ve 33 kriter
  #55'te girdi; sıkı okuma **1**, gevşek okuma **0** veriyordu ve karta gerekçesiyle **1**
  yazıldı. Aynı kuralı iki kart iki ayrı okumayla doldurursa sayılar karşılaştırılamaz —
  K3'ün hedefi "yorum payı yok"tu. Kural **sonraki** karttan başlar: `0012a`'nın hücresi
  olduğu gibi kalır (bu ADR'nin "kapanmış kartlar düzeltilmez" kuralı).

*Bedel:* Yok sayılır. Kapanmış kartlar düzeltilmez — yeni yazım sonraki spec'ten başlar.

### K4 (Ö9) — Şablonun DoD maddeleri çıkarılamaz

Spec'e özel madde eklenebilir ve şablonun maddesi spec'e özel ayrıntıyla **genişletilebilir**;
ama **silinemez**. Uygulanmayan bir madde yerinde kalır ve *"yok"* diye cevaplanır — arşivdeki
kartların zaten yaptığı şey (`0008`: *"Yeni migration **yok**."*).

**Madde varlığı bir işaretleyiciyle ölçülür.** Şablonun her DoD maddesi bir
`<!-- DoD:Tn -->` yorumu taşır; spec'e kopyalanırken işaretleyici de kopyalanır. Metin
serbestçe yeniden yazılabilir — işaretleyici kalmak zorunda. Böylece K4'ün yasakladığı şey
(**silmek**) izin verdiği şeyden (**yeniden yazmak**) mekanik olarak ayrılır.

Ölçüm: on açık spec'in **onunda da** zorunlu dış gözden geçirme maddesi yok; madde
2026-09-22'de şablona girmişti (`1463d7e`). Aynı kayma kapanmış `0007` ve `0008`'de de var
ve `frontend-002` bunu kendi dış bulgusu olarak yazmış (*"dört madde eksikti"*).

*Bedel:* Şablona yeni madde girdiği gün açık spec'lerin DoD'u da güncellenmeli.

### K5 (Ö10) — Ölçümün commit'e girdiği doğrulanır

Commit'ten sonra `git show --stat` okunur ve düzenlemelerin gerçekten girdiği doğrulanır.
Paylaşılan çalışma ağacında **zorunludur**.

Bu öneri `0010a`'nın kartında zaten duruyordu ve kural hâlâ yoktu. `0010a`'nın kapanış
ölçümleri #36'da kayboldu — o PR spec dosyasında `0 0` taşıyor, yalnız bir `git mv` — ve
#40 ile beş gün sonra geri geldi. Commit mesajı "DoD 23/23" derken dosya 20/23 diyordu.

*Bedel:* Her commit'e bir adım.

### K6 (Ö11) — `main` üzerinde günlük zamanlı koşu

Ölçüldü: `2026-10-05T13:14` ile `2026-10-10T06:03` arasında bu depoda **hiçbir koşu yok**
(4 gün 16 saat 49 dakika) ve `main` o sürenin dördünde kırmızıydı. Kırmızılığı ne bir dal ne
bir birleşim üretti — **takvim** üretti (`0006`'nın sahte saati). Bulan şey, ilgisiz bir
chore dalının PR koşusu oldu.

*Bedel iki parçalı ve ikincisi mekanik değil:* CI dakikası, ve **kimsenin okumadığı bir gece
kırmızısı gürültüdür.**

**Okuyan karara bağlandı (Takım Yöneticisi, 2026-10-10): kırmızı bir gece koşusu, ertesi gün
ilk oturum açan üyenin İLK İŞİdir.** Üye, rolünden bağımsız olarak kendi işine başlamadan
önce onu raporlar — hangi iş, hangi test, hangi commit'ten beri. **Raporlamak yeterli;
düzeltmek ayrı bir karardır** ve üyenin kapsamını kendiliğinden genişletmez. Kural
`AGENTS.md` § Çalışma Disiplini'ne yazıldı: ADR bir kayıttır, oturumun işe başlarken
okuduğu yer orası.

*Gerekçe:* bu depoda sabit bir nöbetçi yok ve oturumlar tek seferlik. "Birine atamak"
atanacak kimse olmadığı için çalışmaz; "ilk bakan" çalışır.

**Kapsam — `perf.yml` bilinçli olarak dışarıda.** Zamanlı koşu yalnız `ci.yml`'ı tetikliyor.
`perf.yml` (C3 eşikleri) bugün ne `main` push'unda ne zamanlı koşuyor ve bu turda eklenmedi:
eşik ölçümünün kırmızısı koşucu gürültüsünden ayırt edilemez, **okunamayan bir gece kırmızısı
da tam olarak bu kararın reddettiği şeydir.** Sonuç olarak `main`'in performans durumu
"bilinmiyor" hâlinde kalıyor — kapatılması gerekiyorsa ayrı bir karardır.

**Ölçüm — ilk zamanlı pencere KAÇTI (2026-10-11).** Cron `main`'e 2026-10-10 11:20 UTC'de
girdi (`b97a320`); ilk `0 3 * * *` penceresi 2026-10-11 03:00 UTC'ydi ve o pencereden koşu
çıkmadı. Üç şey ayrı ayrı ölçüldü: iş akışının durumu `active`, dosya varsayılan dalda
(`.github/workflows/ci.yml`), ve API'ye göre `schedule` olayıyla koşu sayısı **0**
(`gh api .../workflows/ci.yml/runs?event=schedule` → `total_count: 0`, 05:26 UTC'de
okundu). Yani sebep devre dışı bir iş akışı ya da yanlış daldaki bir dosya DEĞİL.

Takım Yöneticisi kararıyla yol elle doğrulandı: `workflow_dispatch`, `main @ b99912a`,
koşu `38114347861`, 05:18 → 05:26 UTC, **üç işin üçü de `success`**. Bunun kanıtladığı şey
sınırlı ve sınırını yazmak gerekiyor: **iş akışı `main`'de bir PR'a bağlı olmadan koşabiliyor
ve yeşil.** Kanıtlamadığı şey **zamanlı tetikleyicinin kendisi** — o ancak ikinci pencerede
(2026-10-12 03:00 UTC) ölçülebilir. GitHub'ın zamanlı koşularını yoğunlukta geciktirdiği
bilinen bir davranış; ikinci pencere de boş çıkarsa K6'nın dayandığı mekanizma bu depoda
çalışmıyor demektir ve kararın yeniden açılması gerekir.

**Kuralın kör noktası bu ölçümle görünür oldu: K6 KIRMIZI bir koşu için yazılmıştı.**
"Koşu hiç yok" hâli sessizce "sorun yok" gibi görünür — oturum `gh run list` okur, son
koşuların hepsi yeşil `push` koşusudur ve raporlanacak bir şey bulmaz. Hiç havlamayan bir
bekçi, yanlış havlayan bekçiden daha tehlikelidir: `0006`'nın zaman bombasını dört gün
boyunca görünmez kılan şey tam olarak koşunun **yokluğu**ydu. Bu yüzden `AGENTS.md`'nin
oturum açılış maddesi aynı turda genişletildi: **son 24 saatte hiç zamanlı koşu yoksa bu da
raporlanır** (Takım Yöneticisi, 2026-10-11).

### K7 (Ö12) — Gürültü böleni teslim formatına girer

Dış turu veren oturum bulgularını teslim ederken *"n bulgu, bunların m'si gürültü"* satırını
da verir. Satır yoksa hücre `— (işaretlenmedi)` olur, **`0` olmaz**.

Ölçüm: 38 dış bulgunun **hiçbirinde** bölen yok ve iki kart sebebini yazıyor — `0009`:
*"işaretlenenler bana hiç ulaşmadı"*; `frontend-002`: *"beş maddelik bir iş listesi olarak
geldi ve üçü bulgu değil adımdı."*

*Bedel:* QA'in doğrulama özetine bir satır.

### K8 (Ö13) — `specs/done/` için RAPOR EDEN bir kapı

Kapı üç şey sorar: arşivlenmiş bir spec'te açık `- [ ]` kutusu var mı, SCORECARD'da boş
değer hücresi var mı, DoD şablonun `DoD:Tn` işaretleyicilerinin **hepsini** taşıyor mu.
İşaretleyici listesi **şablondan okunur**, testte sabit değil: şablona madde eklendiği an
kapı onu da arar ve güncellenecek ikinci bir liste olmaz.

**Kapı düzeltmez, bildirir.** Tarihli bir kaydı otomatik düzelten bir kapı, bu projenin
"kapanmış kayıt yeniden yazılmaz" ilkesini ihlal ederdi.

**Kapının bir kesim tarihi var:** yalnız **2026-10-10'dan sonra** `done/`'a taşınan spec'lere
bakar. Bugünkü on iki kart bu tarihten önce kapandı ve K1–K4 onları değiştirmiyor; kapı
onlara bakarsa ilk koşuda on iki kırmızı verir ve hiçbiri düzeltilemez.

*Bedel:* Kesim tarihi bir bakım yüküdür — listenin kendisi değil, **tarihin gerekçesi**
unutulursa kapı bir gün "neden bu tarih?" sorusuyla kaldırılır. Gerekçe bu yüzden kapının
kendi içinde yazılı.

**Muafiyet listesi gerekçeyle BÜYÜYEBİLİR.** İşaretleyici listesi şablondan canlı okunduğu
için, şablona sonradan eklenen bir madde kesimden sonra kapanmış kartları da kırmızıya
çevirir — sondayla ölçüldü (çıkış kodu **1**, mesaj: *"şablonun 1 DoD maddesi yok
(DoD:T14)"*). O kırmızıyı söndürmenin iki yolu var ve ilk yazımda **ikisi de yasaktı**:
kapanmış kartı düzenlemek (tarihli kayıt) ve listeyi büyütmek. İkincisi artık meşru — ama
**yalnız bu vakada** ve **yalnız satırın yanına gerekçe yazılarak** (hangi işaretleyici,
hangi tarihte eklendi). Gerekçesiz bir ad, muafiyetin sessizce genişlemesidir.

## Elenen alternatifler

**`Spec revizyon sayısı`nı tamamen kaldırmak (K1 yerine).** Elendi: ölçüt üç kartta
*çalıştı* ve üçünde de düşük haber verdi. Çalışan bir ölçütü, dokuz kartta ölçülemediği için
atmak, K2 ile sıralama düzeldiğinde elde kalmazdı.

**Kapanmış kartları yeni tanımlara göre düzeltmek (K3'ün kolay yolu).** Elendi: #19 bunu bir
kez yaptı ve iyi niyetliydi, ama tarihli bir kaydı sonradan yeniden yazmak neyin ne zaman
görüldüğünü siler — 001'in kendi metni bunu yazıyor ve #42 de aynı ilkeyi uyguladı.

**Kapıyı kırmızı verecek biçimde kurmak (K8'in sert hâli).** Elendi: ilk koşuda on iki
kırmızı verir, hiçbiri düzeltilemez ve kapı ya kaldırılır ya susturulur. Susturulmuş kapı,
olmayan kapıdan daha kötüdür — var sanılır.

**Madde varlığını metindeki anahtar ifadeyle ölçmek (K4/K8'in ilk tasarımı).** Yazıldı,
koşuldu ve **ölçülerek elendi:** beş kartta yanlış pozitif verdi. *"Arayüz kriteri **yok**;
görsel kanıt bu spec için uygulanabilir değildir"* diyen bir madde `docs/evidence` yolunu
hiç anmıyor; `0003` ve `0009` tam böyle yazmış ve kapı ikisini de "görsel kanıt maddesi
yok" diye bildirdi. Aynı sınıf `docs/domain.md` ve "Karar Defteri" imzalarında da çıktı.
Serbest metinde ifade aramak, maddeyi **silmekle** **yeniden yazmayı** ayırt edemez — oysa
K4'ün yasakladığı yalnızca silmek. İşaretleyici bu ölçümden sonra geldi; ilk tasarım
gönderilmedi.

## Sonuç

- `specs/TEMPLATE.md`: SCORECARD'ın revizyon satırı (K1), üç hücrenin yazımı (K3), DoD'un
  çıkarılamazlığı (K4).
- `docs/git.md`: § 1'e `docs/` öneki ve spec'in donma anı (K2), § 4'e ölçümün commit'e
  girdiğinin doğrulanması (K5).
- `docs/roles/qa.md` § 5: doğrulama özetine gürültü böleni satırı (K7).
- `.github/workflows/ci.yml`: `schedule` tetikleyicisi (K6).
- `tests/Architecture.Tests/ArsivKapisiTests.cs`: `done/` kapısı, **rapor eden**,
  2026-10-10 kesimli, dört iddia ve üçü **sondayla kırmızıya döndürülerek** doğrulandı (K8).
  Düzeltme turunda bir denetim daha eklendi: **§ SCORECARD bölümünün tamamen yokluğu** —
  bir hücrenin boş olmasından daha kötü olduğu hâlde kapı onu yeşil geçiriyordu.
- On açık spec'in DoD'una `DoD:Tn` işaretleyicileri ve eksik şablon maddeleri eklendi (K4).
  **Zorunlu dış gözden geçirme maddesi onunda da yoktu.**
- **Karar Defteri'ne satır eklenmedi:** bu kararlar süreç ve ölçüm kararlarıdır, mimari
  karar değil; § 7 kurulum modeli, modül sınırı ve veri deposu gibi mimari seçimleri
  taşıyor. ADR `0007` de aynı gerekçeyle satır eklememişti.
- `AGENTS.md` § Çalışma Disiplini: gece koşusunun kırmızısını ertesi günün **ilk oturumu**
  raporlar (K6'nın ikinci yarısı — Takım Yöneticisi kararı, 2026-10-10).
- `.github/workflows/ci.yml`: kapı raporunu **iş özetine** yazan adım. `ITestOutputHelper`
  çıktısı geçen testler için konsola basılmıyor; rapor yalnız trx artefaktının içinde
  kalıyordu, yani bildiren yarısı pratikte susturulmuştu.
- **Açık kalan:** `perf.yml`'ın zamanlı koşuya alınıp alınmayacağı (K6, kapsam notu).
