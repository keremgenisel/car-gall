# 0012c — Toplu Eylem ve Öne Çıkarma

| | |
|---|---|
| **Durum** | Onaylandı |
| **Sahip** | Developer |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `VehicleCatalog` (toplu komut yüzeyi) · `frontend/` yönetim paneli |
| **Bağımlı spec'ler** | **`0012b` ÖNKOŞUL — tamamlandı** (2026-10-11, `e67a61d`; tekil geçiş komutları, duyurular, yansıtma) · `0010b` (panel listesi) |
| **Dayanak** | `0012b` **D39** — `0012`'nin üçüncü dilimi |
| **Kararlar** | Taşınanlar `0012b`'de verildi (**D3–D7**, **D16**, **D18**, **D27–D29**, **D36**); **D43–D55** bu spec'in kendi CLARIFY turunda (2026-10-11, `2255370` üzerinde) **ölçülerek** eklendi; **D56–D57** kriter PR'ı hâlâ açıkken dış triyajın ölçümünden (2026-10-12) doğdu. **Açık soru yok.** |

> **R/C/AC bu PR'da `main`'e giriyor ve orada donuyor** (ADR `0008` / K2 ve K3'ün donma
> anı cümlesi — `0012b` **D38**). Bu commit'ten sonra Requirements, Constraints veya
> Acceptance Criteria'da yapılan her değişiklik **sayılan bir revizyondur**.
>
> **CLARIFY turu, 2026-10-11.** Bu dosya `0012b` **bitmeden** yazılmıştı ve Context'i
> *"`0012b` bittiğinde"* diye konuşuyordu; `0012b` bittikten sonra (#59→#61) hepsi bugünkü
> `main` üzerinde yeniden ölçüldü. On üç boşluk bulundu ve karara bağlandı (**D43–D55**).
> İkisi spec'i bugünkü hâliyle **uygulanamaz** kılıyordu: **C4, C2 ve C3 ile çelişiyordu**
> (**D46**) ve sonuç satırının şekli kısmi başarı kuralını karşılayamıyordu (**D45**).
> Ölçüm ayrıca `0012b`'nin bir **kaçan hatasını** buldu (**D55**).
>
> **Numaralandırma `0012` ailesinde devam ediyor** (D43'ten): bu kart R1–R5'te `0012b`'nin
> kararlarına **çıplak numarayla** atıf yapıyor (D3, D16, D28…); kendi kararlarını D1'den
> başlatmak aynı numarayı iki ayrı karara verir ve iki kartı birden okuyan geliştirici için
> ayırt edilemez hâle getirirdi.

---

## Intent

`0012b` bittiğinde galeri **bir aracı** yayına alabiliyor, rezerve edebiliyor, satıldı
işaretleyebiliyor ve arşivleyebiliyor. Ama günlük iş bir araç üzerinde değil **liste
üzerinde** yürüyor: sezon sonunda on beş aracı birlikte yayından kaldırmak, fuar öncesi
yirmi aracı birlikte yayına almak.

Bu iş bittiğinde personel **birden çok ilanı seçip** aynı eylemi hepsine uygulayabilir ve
bir ilanı **öne çıkarabilir**. Başarı ölçütü: **yirmi aracın durumunu değiştirmek yirmi
sayfa gezinmeyi gerektirmiyor.**

---

## Requirements

- **R1.** Personel **birden çok aracı seçip** aynı eylemi hepsine uygulayabilir. Toplu
  uygulanabilen eylemler: yayına al · yayından kaldır · satıldı işaretle · arşivle · öne
  çıkar · **öne çıkarmayı kaldır**. Rezerve etme ve rezervasyonu kaldırma **tek araç**
  üzerinde çalışır. Seçim **araç kimliğiyle** taşınır. Seçim **sayfaya aittir** ve sayfa
  değişince temizlenir. → `0012b` **D18**, **D36**, **D43**, **D44**
- **R2.** Toplu eylemde bazı araçlar için eylem geçersizse, **geçerli olanlar uygulanır** ve
  uygulanamayanlar **tek tek gerekçesiyle** bildirilir; işlem bir bütün olarak geri alınmaz.
  Bulunamayan araç isteği değil **o satırı** düşürür; zaten hedef **durumda** olan kayıt
  **başarısız** sayılır ve sonuç satırı kaydın **güncel durumunu** taşır. "Gerekçe" tekil
  uçtaki gerekçenin **aynısıdır**: hata kodu **ve** onun yapısal parametreleri.
  → `0012b` **D3**, **D27**, **D28**, **D45**
- **R3.** Seçim boş olamaz, yinelenen kimlikler teklenir ve seçim sayısı panelin sayfa üst
  sınırını aşamaz — aşan seçim **kırpılmaz, reddedilir**; sınır **dâhildir** ve
  tekilleştirme sınırdan **önce** gelir. Şube kapanışı yolu (`0012b` **D33**) bu sınıra
  tabi **değildir**. Bu maddenin sınır davranışı **API düzeyinde** doğrulanır; panel sayfa
  boyutu kontrolü kazanmaz. → `0012b` **D4**, **D5**, **D29**, **D33**, **D52**
- **R4.** Yanıt `200` ve gövdesi **araç başına sonuç listesidir**; hepsi başarısız olsa bile.
  Her satır aynı şekli taşır: araç kimliği · başarılı mı · başarısızsa hata kodu ve yapısal
  parametreleri · kaydın **güncel durum kodu** (başarılı satırlarda da).
  → `0012b` **D6**, **D45**, `docs/conventions.md` § 5
- **R5.** Personel bir ilanı **öne çıkarabilir** ve öne çıkarmayı kaldırabilir; öne çıkarılan
  ilan vitrin sıralamasının başında görünür. Eylem **arşiv dışındaki her ilanda** çalışır,
  zaman damgasını ilerletir ve geliştirme seed'i iki değerli anahtara hizalanır. Eylem
  **idempotenttir**: zaten o hâlde olan kayıt **başarılı** döner. Arşivlenmiş ilan
  **reddedilir** ve reddin kendi kodu vardır. Panel **hangi aracın öne çıkarıldığını
  gösterir**. Eylem **yalnız liste ekranında** yaşar. → `0012b` **D16**, **D36**,
  **D48**–**D51**
- **R6.** **Eylem sonucunun** iki durumu bu spec'in borcudur: **başarılı** ve **kısmi
  başarı**; ikisinin de kendi kriteri ve görsel kanıtı olur. Liste ekranının **yükleniyor**,
  **boş** ve **hata** durumları `0010b`'de kurulu ve burada yeniden sınanmaz.
  → `docs/frontend.md` § 3, `0012b` **D7**, **D54**
- **R7.** Panel personelin tercih ettiği dilde çalışır; Arapça'da RTL.
- **R8.** Toplu eylem satırları **gönderildiği sırada** ve **sırayla** işler; bir satırın
  eşzamanlılık kaybı ya da reddi diğerlerini düşürmez. Yarıda kesilen bir istek telafi
  edilmez ve kaldığı yerden devam etmez. → **D47**, `0012b` **D3**

---

## Constraints

### Uyulması zorunlu

- **C1.** Tüm eylemler **oturum gerektirir**.
- **C2.** Toplu eylem **tekil komutların üzerine** kurulur; durum geçişi mantığı `0012b`'nin
  agregatında kalır ve burada tekrarlanmaz.
- **C3.** İşlem sınırı **ilan başınadır** (`0012b` **D3**); tek transaction kullanılmaz.
- **C4.** **Aynı yanıtı veren** okumalar istek başına **bir kez** yapılır ve komuta verilir:
  zorunlu alan kümesi, public alan anahtarları, şube başına ad haritası, kanonik model
  adları. **Tazeliği gereken** okuma (medya hazırlığı) kayıt başına ve **kilidin altında**
  kalır. → **D46**
- **C5.** Sayı içeren cümleler ICU ile kurulur ("3 ilan yayına alındı"). → Yasak Listesi 31
- **C6.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner.
- **C7.** Her kabul kriteri en az bir testle karşılanır.
- **C8.** Öne çıkarmanın reddi `0012b` **C13**'ün eşlemesine uyar ve **kendi** kodunu taşır:
  arşivlenmiş ilan `listing.cannot_feature.archived` → **`409`**. Geçiş reddinin kodu
  (`listing.invalid_status_transition`) öne çıkarma için **kullanılmaz** — öne çıkarma bir
  geçiş değildir. → **D49**
- **C9.** Modül **kendi** bağımlılığını kendi kaydeder; başka modülün DI kaydına yaşayan
  bir bağımlılık bırakılmaz. → **D55**

### KAPSAM DIŞI

- **X1.** **Rezerve etme ve rezervasyonu kaldırmanın toplu hâli** — *neden:* rezervasyon bir
  müşteriye aittir; yirmi aracı kimseye rezerve etmek, `0012b` X1'in dışarıda bıraktığı
  kaydın yerine geçmeye çalışmaktır (`0012b` **D18**).
- **X2.** **Öne çıkarılmış ilanların kendi aralarında sıralanması** — *neden:* `0012b`
  **D16** anahtarı iki değerli tuttu; eşitlik yayın tarihiyle kırılıyor ve gerçek sıralama
  `0015`'in işi.
- **X3.** **Dışa aktarma ve toplu alan doldurma sihirbazı** — *neden:* `0012b` X5 ve
  `docs/architecture.md` § 6.
- **X4.** **"Eşleşen hepsine uygula"** (eylemi seçime değil **filtreye** uygulamak) —
  *neden:* geri alma yok; arşiv bu projenin silmesidir (`0010b` X19) ve "arşivi dâhil et"
  filtresiyle birleşince iki tıkla bütün envanteri arşivleyebilirdi. Seçim üst sınırı
  (`0012b` **D4**) da o anda anlamını yitirirdi. → **D53**
- **X5.** **Panelin sayfa boyutu kontrolü** — *neden:* liste eksenleri `0015`'in işi;
  bu spec sınırı **API düzeyinde** doğrular (**D52**). Bugün istemci `pageSize`
  göndermiyor ve sayfa her zaman yapılandırmadaki varsayılan.

---

## Context

### Mevcut davranış — doğrulandı (2026-10-11, `main` = `2255370`)

> Bu bölüm `0012b` **bitmeden** yazılmıştı ve *"`0012b` bittiğinde"* diye konuşuyordu.
> `0012b` bitti (#59→#61); aşağıdakilerin hepsi artık **ölçüm**.

- **Tekil geçiş komutları ve altı eylem hazır** ve hepsi **araç kimliğiyle** çalışıyor:
  `POST …/admin/vehicles/{vehicleId}/{publish|unpublish|reserve|unreserve|mark-sold|archive}`
  (`VehicleAdminEndpoints.cs:69`). Başarıda **`204`** dönüyor, **gövde yok**; HTTP sözleşmesi
  `0012b` **D26**/**C13** ile sabit. → **D44**, **D45**
- **Yönetim sözleşmesinde ilan kimliği diye bir yüzey yok.** Liste `VehicleAdminListItemDto`
  ile araç taşıyor; `ListingSnapshotContracts` `Leads`'in dar okuması. → **D44**
- **Yayın reddi TEK bir hata, gerekçeleri parametrede:** üst kod `vehicle.cannot_publish` +
  `reasons` (virgülle birleşik alt kodlar) + alt kodların kendi parametreleri
  (`ListingErrors.cs:58-77`). Panel bunu zaten böyle çeviriyor
  (`ListingActionBar.tsx:77-82`). → **D45**
- **Tekil komut araç başına BEŞE KADAR okuma yapıyor ve hepsi kilidin içinde:**
  `ReadyAmongAsync` (`Media`), adresi yoksa `ResolveCanonicalNamesAsync`
  (`VehicleReference`), zorunlu alan kümesi (kendi şeması) ve `ProjectAsync` → şube adları
  (`Dealership`) + kapak adresi (`Media`) + public alanlar
  (`ListingTransitionCommands.cs:48-91`, `ShowcaseProjectionWriter.cs:35-53`). 25 satırlık
  bir seçim ~125 okuma eder. → **D46**
- **Her geçiş satır kilidi alıyor ve kilit zaman aşımı 3 saniye.**
  `InvariantGuard.ExecuteAsync` + `ListingRow(vehicleId)`; `InvariantGuardOptions.Default`
  üç saniye (`InvariantGuard.cs:15`). 25 satır × 3 saniye = tek istekte ~75 saniyelik kötü
  durum. → **D47**
- **Duyuru altyapısı ve sekiz olay hazır** (`0012b` **D11**, **D14**, **D30**); vitrin
  yansıtması her yazma yolunda koşuyor (**D12**, **D41**).
- **Panel listesi arşiv filtresini (sayaçlar dâhil) ve "lokasyon ataması bekliyor" rozetini
  taşıyor** (`0012b` **D19**, **D35**, **D15**); filtre adreste yaşıyor
  (`VehicleListPage.tsx:43-48`). **Satır seçimi YOK** — onu bu spec getirecek.
- **Liste sözleşmesi öne çıkarma durumunu TAŞIMIYOR:** `VehicleAdminListItemDto`'nun on
  alanı var ve hiçbiri sıralama ağırlığı değil. Sorgu durum için `Listings`'i **zaten**
  okuyor (`VehicleAdminQueries.cs:185`). → **D50**
- **Öne çıkarma arayüzü iki yere birden konabilir durumda:** `ListingActionBar` bugün
  **düzenleme** ekranında (`VehicleEditPage.tsx:270`), seçim ise listede olacak. → **D51**
- **İstemci `pageSize` HİÇ göndermiyor** (`catalogAdmin.ts:46-47`: yalnız `locale`, `page`,
  `sort`, `archived`) ve arayüzde sayfa boyutu kontrolü yok → sayfa her zaman
  yapılandırmadaki varsayılan (**25**). Yani **D4**'ün 100 sınırı panelden
  tetiklenemiyor. → **D52**
- **`Listing.ChangeShowcaseSortWeight` hâlâ çağıransız** (`Listing.cs:243`) ve olay
  yaymıyor: bu spec onu saracak.
- **Geliştirme seed'i öne çıkarma kolonuna `1..N` yazıyor** (`ShowcaseSeeder.cs:139`) ve
  **D16**'nın iki değerli anahtar kararını veri tarafında deliyor; **D36** hizalamayı bu
  spec'e bağladı.
- **`VehicleCatalog` `InvariantGuardOptions`'ı KAYDETMİYOR.** `ListingTransitionCommands`
  onu kurucusunda istiyor (`:45`); kaydı yalnız `IdentityModule.cs:83` ve
  `DealershipModule.cs:50` yapıyor. Host'ta bütün modüller kayıtlı olduğu için **bugün
  çalışıyor**; entegrasyon testleri komutu elle kurduğu için DI yolunu denemiyor ve
  `EsZamanlilikKuraliTests` yalnız guard **çağrısını** denetliyor. → **D55**

### Taşınan kararlar

`0012b`'nin iki CLARIFY turunda verilen ve **burada tüketilecek** kararlar: **D3** (ilan
başına transaction) · **D4** (üst sınır = panelin sayfa üst sınırı) · **D5** (boş seçim
reddedilir, yineleme teklenir) · **D6** (`200` + satır listesi) · **D7** (kısmi başarı,
dördüncü ekran durumu) · **D16** (öne çıkar iki değerli) · **D18** (hangi eylemler toplu) ·
**D27** (bulunamayan ilan satırı düşürür) · **D28** (zaten hedef durumdaki kayıt başarısız
sayılır) · **D29** (seçim kırpılmaz, reddedilir) · **D36** (toplu kaldırma, arşiv ön koşulu,
seed'in hizalanması).

Bu kararlar `0012b`'de **verildi ve orada donmuş hâlde duruyor**; bu spec onları yeniden
tartışmaz, uygular.

### Bilinen tuzaklar

1. **Toplu eylemi tek işlem sanmak.** 20 ilandan 3'ü geçersizse "hepsi başarısız" demek
   personeli tek tek denemeye zorlar; "hepsi başarılı" demek yalan söyler. → `0012b` **D3**
2. **Başarıyı HTTP kodundan okumak.** `200` istek düzeyinde başarıdır, kayıt düzeyinde
   değil; istemci gövdeyi okumak zorundadır. → `docs/conventions.md` § 5
3. **Çapraz okumaları ilan başına yapmak.** 100 ilanlık bir seçimde 200 modüller arası okuma
   üretir; `0012b`'nin yayın komutu okumaları küme alacak biçimde yazdı.
4. **Seçimi sayfadan bağımsız sanmak.** "Hepsini seç" sayfadakileri seçer; sonraki sayfaya
   geçince seçimin ne olduğu tanımlanmak zorunda. → **D43**
5. **Toplu yanıtta gerekçeyi koda indirmek.** Tekil uç *"medya yok, zorunlu alan eksik"*
   derken toplu uç *"yayına alınamadı"* derse aynı personel aynı araç için iki farklı cevap
   alır; `docs/frontend.md` § 3'ün "**uygulanmayanlar tek tek, gerekçesiyle**" kuralı da
   karşılanmaz. Gerekçe **parametrelerde** yaşıyor. → **D45**
6. **Öne çıkarmayı durum geçişi gibi işletmek.** Öne çıkarma A3'te yok, olay yaymıyor ve
   idempotenttir; **D28**'i ona uygulamak 20 satırın 8'ini "zaten öne çıkarılmış" diye
   kırmızı gösterir ve personeli kırmızıyı görmezden gelmeye alıştırır. → **D48**
7. **Yalnızca yazan bir düğme teslim etmek.** Öne çıkarma durumu listede görünmezse
   personel ne eylemin işe yarayıp yaramadığını ne de hangi satırın öne çıkarıldığını
   görebilir; doğrulamanın tek yolu ziyaretçi vitrinini açmak olurdu. → **D50**

### Verilen kararlar

Hepsi 2026-10-11 CLARIFY turunda, `2255370` üzerinde **ölçülerek** ve Takım Yöneticisi
onayıyla verildi. Taşınan kararlar yukarıda; bunlar **bu spec'in kendi** kararları.

- **D43 — Seçim SAYFAYA aittir ve sayfa değişince temizlenir.** (eski **S1**, kapandı)
  *Gerekçe:* üst sınır zaten sayfanın üst sınırı (**D4**); sayfalar arası biriken bir seçim o
  sınırı anlamsız kılar ve personelin **görmediği** araçlara eylem uygular. İki ölçüm bunu
  destekliyor: istemci `pageSize` göndermediği için seçim bugün en çok 25 kayıt (**D52**) ve
  seçim sayfaya ait olduğu sürece panel kısmi başarı listesindeki araçların **adlarını**
  elinde tutar — biriken bir seçimde o adlar için ikinci bir okuma gerekirdi. *Bedel:* iki
  sayfadaki araçlara aynı eylemi uygulamak iki tur demektir.
- **D44 — İstek ve yanıt ARAÇ kimliğiyle anahtarlanır.** R1–R4'ün "ilan" sözcüğü "araç"a
  çevrildi. *Gerekçe:* `0012b`'nin uçları araç kimliğiyle çalışıyor
  (`VehicleAdminEndpoints.cs:69`), panel listesi araç taşıyor ve yönetim sözleşmesinde ilan
  kimliği diye bir yüzey **yok**; ilan kimliğine geçmek yeni bir okuma ve arayüzde ikinci
  bir kimlik sözlüğü demekti. *Bedel:* taşınan kararların metni (D3, D27) "ilan" demeye
  devam ediyor; iki kart iki sözcük kullanıyor ve fark bu satırda yazılı. D3'ün "ilan başına
  transaction" ifadesi doğru kalıyor — bir aracın tam olarak bir ilanı var.
- **D45 — Sonuç satırının şekli TEK: araç kimliği · başarılı mı · hata kodu **ve yapısal
  parametreleri** · kaydın güncel durum kodu (başarılı satırda da).** `docs/conventions.md`
  § 5 bu yarım cümleyi kazandı. *Gerekçe:* yayın reddi tek bir hata ve gerekçeleri
  `reasons` parametresinde (`ListingErrors.cs:58-77`); parametreler düşerse toplu uç
  *"yayına alınamadı"* der, tekil uç aynı araç için *"medya yok, zorunlu alan eksik"* der ve
  `docs/frontend.md` § 3'ün "gerekçesiyle" kuralı karşılanamaz. Güncel durumun başarılı
  satırda da gelmesi paneli listeyi **yeniden çekmeden** boyatır. *Bedel:* toplu satır tekil
  uçtan daha zengin bir gövde taşır (tekil uç `204` dönüyor) ve bu asimetri yazılmak
  zorunda; panel mevcut `publishRejectionKeys`'i yeniden kullandığı için yeni çeviri
  anahtarı doğmuyor.
- **D46 — C4 DARALTILDI: "aynı yanıtı veren" okumalar istek başına bir kez, tazeliği
  gereken okuma kayıt başına ve kilidin altında.** İstek başına bir kez: zorunlu alan
  kümesi, public alan anahtarları, şube başına ad haritası, kanonik model adları. Kayıt
  başına: medya hazırlığı. *Gerekçe:* C4 bugünkü yazımıyla **C2 ve C3 ile çelişiyordu** —
  ölçüldü: tekil komut araç başına beşe kadar okuma yapıyor ve hepsi kilidin içinde
  (`ListingTransitionCommands.cs:48-91`, `ShowcaseProjectionWriter.cs:35-53`). C2 daha güçlü
  kısıt: yayın uygunluğunu ikinci kez yazmak A2 için ikinci bir gerçek kaynağı üretir
  (`0010a` AC-42'nin dersi). Medya hazırlığını kilitlerden **önce** topluca okumak ise D8'in
  *"agregatı kilitten SONRA oku"* kuralının önlediği bayat-okuma hatasını geri getirir.
  *Bedel:* yeni kapanmış `0012b`'nin komutu ve yansıtma yazıcısı bir **ön okuma bağlamı**
  parametresi kazanır (**D41**'in emsali) ve kriterler hangi okumanın istek başına, hangisinin
  kayıt başına olduğunu adıyla sayar. *Elenen alternatif:* C4'ü harfiyen uygulamak.
- **D47 — Satırlar GÖNDERİLDİĞİ SIRADA ve sırayla işlenir; satır düzeyinde kayıp isteği
  düşürmez; yarıda kesilen istek telafi edilmez ve devam etmez.** `concurrency.lost` **satır
  düzeyinde** bir başarısızlıktır. İstek düzeyinde süre sınırı bu spec'te **konmaz**.
  *Gerekçe:* satırları paralelleştirmek kilit çekişmesini çoğaltır ve yanıt sırasını belirsiz
  kılar; tek bir kilit kaybında isteği iptal etmek D3'ün kısmi başarı kararını siler; telafi
  ise satır başına commit'in tam tersi. *Bedel, açıkça:* 25 satır × 3 saniyelik kilit
  penceresi (`InvariantGuard.cs:15`) tek bir isteği kötü durumda ~75 saniye tutabilir;
  çözüm yeni bir mekanizma değil **daha küçük bir sınır** (D4). Yarıda kesilen istekten
  sonra personel seçimi yeniden uygular ve uygulanmış satırlar D28 gereği "zaten o durumda"
  diye döner.
- **D48 — Öne çıkarma İDEMPOTENTTİR ve D28 ona uygulanmaz:** zaten o hâlde olan kayıt
  **başarılı** döner. *Gerekçe:* öne çıkarma bir geçiş değil — A3 onu tanımıyor ve
  `ChangeShowcaseSortWeight` olay yaymıyor (`Listing.cs:243`); D28'in gerekçesi personelin
  niyetinin **reddedilmesiydi**, burada niyet gerçekleşmiş durumda. 20 satırın 8'ini "zaten
  öne çıkarılmış" diye kırmızı göstermek personeli kırmızıyı görmezden gelmeye alıştırır.
  *Bedel:* aynı ekranda iki eylem iki farklı kurala uyar (geçiş katı, öne çıkarma
  idempotent) ve fark R1/R5'te yazılı olmazsa kusur sanılır.
- **D49 — Arşivlenmiş ilanda öne çıkarmanın KENDİ kodu var:**
  `listing.cannot_feature.archived` → **`409`** (**C8**). *Gerekçe:* D36 ön koşulu koymuştu
  ama kod yoktu; `listing.invalid_status_transition`'ı yeniden kullanmak yalan söyler —
  panel bir **sıralama** eylemi için "geçersiz durum geçişi" derdi. *Bedel:* bir kod, üç
  çeviri, bir test.
- **D50 — Liste sözleşmesi EKLEMELİ bir `IsFeatured` alanı kazanır** ve satırda rozetle
  görünür. *Gerekçe:* ölçüldü — `VehicleAdminListItemDto`'nun on alanından hiçbiri sıralama
  ağırlığı değil, yani eylem **yalnızca yazan** bir düğme olurdu: personel hangi satırın öne
  çıkarıldığını göremez, toplu eylemin bir şey yapıp yapmadığını anlayamaz ve D48'in
  "zaten o hâlde" cevabı ekranda gösterilemez. D35'in emsali: eylemin değiştirdiği durum,
  eylemin yaşadığı yüzeyde görünür olmalı. Sorgu durum için `Listings`'i zaten okuyor
  (`VehicleAdminQueries.cs:185`), ek sorgu doğmuyor. *Bedel:* kapanmış `0010b`'nin
  sözleşmesi bir alan kazanır ve PR'da **adıyla** yazılmak zorunda.
- **D51 — Öne çıkarma YALNIZ liste ekranında yaşar** (satır başına anahtar + toplu);
  düzenleme ekranının eylem çubuğuna eklenmez. *Gerekçe:* D16 onu bir **sıralama** işi
  sayıyor ve D39 onu toplu eylemle aynı ekrana koydu; `ListingActionBar` bugün düzenleme
  ekranında (`VehicleEditPage.tsx:270`) ve oraya eklemek bir sıralama kontrolünü durum
  geçişi gibi gösterir, yüzeyi de ikiye böler. *Bedel:* tek bir aracı öne çıkarmak için
  personel listeye döner — düzenleme ekranından bir tık fazla.
- **D52 — Sınır kriterleri API DÜZEYİNDE yazılır; panel sayfa boyutu kontrolü kazanmaz**
  (**X5**). *Gerekçe:* ölçüldü — istemci `pageSize` hiç göndermiyor
  (`catalogAdmin.ts:46-47`) ve arayüzde kontrol yok; sayfa her zaman varsayılan **25**, yani
  D29'un **100** sınırı ve D5'in "sınır aşımı" reddi panelden tetiklenemiyor. Bunu arayüz
  kriteri yapmaya çalışan geliştirici kimsenin istemediği bir kontrol ekler. *Bedel:* iki
  kriter yalnız testten doğrulanır, görsel kanıtı olmaz ve DoD'un görsel kanıt maddesinde
  bu adıyla yazılır. Ürünün kendi arayüzünden ulaşılamayan bir sınırın kodda durması
  **dürüst** hâldir, gizlenecek bir boşluk değil.
- **D53 — "Eşleşen hepsine uygula" KAPSAM DIŞI** (**X4**). *Gerekçe:* geri alma yok; arşiv
  bu projenin silmesidir (`0010b` X19) ve "arşivi dâhil et" filtresiyle birleşince iki tıkla
  bütün envanter arşivlenebilirdi. Seçim üst sınırı da (**D4**) o anda anlamını yitirir.
  *Bedel:* 400 araçlı bir galeride aynı eylem 16 sayfa × bir tur demek — D43'ün bedeliyle
  aynı aile ve onunla birlikte okunmalı.
- **D54 — R6 İKİYE ayrıldı:** bu spec'in borcu **eylem sonucunun** iki durumu (başarılı,
  kısmi başarı) ve ikisi de kendi kriterini + görsel kanıtını alır; listenin yükleniyor/boş/
  hata durumları `0010b`'de kurulu ve burada yeniden sınanmaz. *Gerekçe:* `docs/frontend.md`
  § 3 **dört** durum tanımlıyor ve "başarılı" onlardan biri değil; R6'nın eski yazımı
  `0010b`'nin kanıtladığı üç kriteri yeniden açıyor ve gerçekten yeni olan iki durumu kendi
  satırı olmadan bırakıyordu. *Bedel:* R6'nın metni değişti ve DoD'un görsel kanıt maddesi
  hangi iki durumun resmedileceğini adıyla yazıyor.
- **D55 — `VehicleCatalog` `InvariantGuardOptions`'ı KENDİ kaydeder ve bu `0012b`'nin
  KAÇAN HATASIDIR** (kartın hücresi `izleniyor` → **1**). Düzeltme bu spec'in PR'ında
  **adıyla** yazılır (**C9**). *Gerekçe:* ölçüldü — `ListingTransitionCommands` onu
  kurucusunda istiyor (`:45`) ama `VehicleCatalogModule` kaydetmiyor; kaydı yalnız
  `IdentityModule.cs:83` ve `DealershipModule.cs:50` yapıyor. `Identity`'nin o satırı
  kalkarsa `VehicleCatalog`'un **bütün** durum geçişleri çalışma zamanında düşer ve hiçbir
  test bunu yakalamaz: entegrasyon testleri komutu elle kuruyor, `EsZamanlilikKuraliTests`
  yalnız guard **çağrısını** denetliyor. Aynı aile #52'nin bedeli ödenen tuzağıydı — DI'da
  kimin neyi kaydettiği sessizce yanlış olabiliyor. *Bedel:* kapanmış bir kartın SCORECARD
  hücresi güncellenir; ADR `0008`/K3 kapanmış kartları düzeltmiyor ama `Kaçan hata` hücresi
  tam bu amaçla `izleniyor` bırakılıyor.
- **D56 — Kapak görselinin adresi de ÖN OKUMA BAĞLAMINA girer; D46'nın bütçesi beş
  okumadır.** *Gerekçe:* ölçüldü (dış triyaj, 2026-10-12) — yansıtma kapak adresini **kayıt
  başına** okuyor (`ShowcaseProjectionWriter.cs:66`) ve D46 onu hiç saymamıştı: 25 satırlık
  bir toplu yayın 25 hazırlık + 25 kapak = **50** modüller arası çağrı yapıyor, yani Tuzak
  3'ün adını koyduğu sayı. Kapak hazırlıktan **farklıdır** ve öne alınabilir: tazelik
  gerektirmiyor, kopyalanmış bir kolonu besliyor ve `MediaAssetReadyHandler` onu kendi
  yolundan tazeliyor (`0011`/AC-36, **D34**) — bayat bir kapak bir sonraki
  `MediaAssetReady`'de düzelir, oysa bayat bir "hazır" cevabı **yanlış bir yayın kararı**
  üretir. `FindManyAsync` zaten küme imzalı. *Bedel:* bağlam beşinci bir alan kazanır ve
  `Media` okuması artık iki ayrı gerekçeyle iki ayrı yerde yaşar (hazırlık kilidin altında,
  kapak kilidin dışında); ikisinin farkı AC-34 ile AC-35'te adıyla yazılı.
- **D57 — `InvariantGuardOptions` MODÜL BAŞINA tiplenir: `InvariantGuardOptions<TContext>`.**
  *Gerekçe:* D55 kaydı ekledi ama tuzak sınıfını kapatmadı — ölçüldü (dış triyaj,
  2026-10-12): üç modül aynı non-generic tipi singleton kaydediyor, kap tek ve **son kayıt
  kazanıyor**. Bugün zararsız (üçü de `.Default`), ama bağımlılık **yön değiştirdi, kalkmadı**:
  bir modül kendi kilit süresini ayarladığı gün — ki D47'nin bedel satırı bunu adıyla
  öneriyor — diğer ikisinin süresi sessizce onunla değişir. Bu, #52'de bedeli ödenen
  `IOutboxWriter` tuzağının birebir şekli ve çözümü de aynı: `OutboxWriter<TDbContext>`
  emsali. Tip parametresi `ExecuteAsync`'in imzasında da yaşıyor, yani **başka modülün
  seçeneğini geçirmek derleme hatası** olur. *Bedel:* `BuildingBlocks`'un tipi ve onu isteyen
  dört komut sınıfı imza değiştirir; ayrıca bir mimari kapı (`InvariantGuardSecenekleriTests`)
  paylaşılan non-generic seçenek kaydını yasaklar ve guard'ı çağıran her modülün **kendi**
  seçeneğini kaydettiğini ölçer.

---

## Açık sorular

**Yok.** Son açık soru (**S1** — "hepsini seç" sayfa değiştiğinde ne olur) 2026-10-11'de
**D43** ile kapandı: seçim sayfaya aittir ve sayfa değişince temizlenir.

Kabul kriterlerini bekleten bir karar kalmadı; kriterler bu PR'da yazıldı.

---

## Acceptance Criteria

Kırk dört kriter, yedi grupta. Bekleyen karar yoktu (**D43**); kapsamı **D52** (sınır
kriterleri API düzeyinde), **D53** (filtreye uygulama kapsam dışı) ve **D54** (R6 ikiye
ayrıldı) belirledi. Yapılandırmadan gelen her değer için kriter **yapılandırılan değeri**
doğrular, spec'e kopyalanmış bir sabiti değil.

**AC-43 ve AC-44 bu PR hâlâ açıkken eklendi, AC-34'ün ölçüsü de bu turda büyüdü** (dış
triyaj, 2026-10-12): satır başına öne çıkarmanın **hata yolunu** hiçbir kriter istemiyordu
ve okuma bütçesi kapak adresini saymıyordu. Donma anı AC'yi taşıyan PR'dır (K3 · `0012b`
**D38**), yani ikisi de **revizyon değil**. **Numaralar APPEND edilir:** AC-43 F grubunun
sonuna 40'tan sonra giriyor, araya değil — var olan bir numarayı kaydırmak testlerdeki
`0012c/AC-n` etiketlerini sessizce başka kriterlere bağlardı ve `0012b`'nin kaçan hatası
tam olarak buydu (yedi etiketin permütasyonu, hepsi "ölçüldü" görünüyordu).

### A. Seçim ve istek düzeyi reddi

- **AC-1.** Oturum açmış personel birden çok aracı seçip aynı eylemi hepsine uygular; yanıt `200` ve gövdesi araç başına sonuç listesidir.
- **AC-2.** Boş seçim istek düzeyinde reddedilir: yanıt `422` ve `listing.selection_empty` taşır.
- **AC-3.** Yinelenen araç kimlikleri teklenir: aynı araç iki kez gönderildiğinde sonuç listesi o aracı **bir kez** taşır.
- **AC-4.** Tekilleştirme sınır kontrolünden **önce** gelir: yalnız yinelemeler yüzünden sınırı aşan bir seçim kabul edilir.
- **AC-5.** Üst sınırı aşan seçim **kırpılmaz, reddedilir**: yanıt `422` ve `listing.selection_limit_exceeded` taşır.
- **AC-6.** Üst sınır **dâhildir**: tam sınır kadar araç taşıyan seçim kabul edilir.
- **AC-7.** Üst sınır **yapılandırmadan** okunur; kodda sabit değildir.
- **AC-8.** Oturumu olmayan biri hiçbir toplu eylemi çağıramaz.
- **AC-9.** Toplu **rezerve** ve toplu **rezervasyon kaldırma** uçları yoktur.

### B. Sonuç satırının şekli

- **AC-10.** Her sonuç satırı aynı şekli taşır: araç kimliği · başarılı mı · hata kodu ve yapısal parametreleri · kaydın güncel durum kodu.
- **AC-11.** **Başarılı** satır da kaydın **güncel durum kodunu** taşır.
- **AC-12.** Başarısız satır tekil ucun **aynı** gerekçesini taşır: kod **ve** yapısal parametreleri; toplu yayın reddinde eksiklerin listesi satırda durur.
- **AC-13.** Araçların **hepsi** başarısız olsa bile yanıt `200` döner.
- **AC-14.** Bulunamayan araç isteği değil **o satırı** düşürür: satır `vehicle.not_found` taşır ve istek `200` kalır.
- **AC-15.** Zaten hedef **durumda** olan kayıt **başarısız** sayılır ve satır kaydın güncel durumunu taşır.
- **AC-16.** Geçerli satırlar uygulanır; bir satırın reddi uygulanmış satırları **geri almaz**.

### C. Altı eylem, sıra ve dayanıklılık

- **AC-17.** Altı toplu eylemin her biri toplu yüzeyden uygulanır: yayına al · yayından kaldır · satıldı işaretle · arşivle · öne çıkar · öne çıkarmayı kaldır.
- **AC-18.** Satırlar **gönderildiği sırada** işlenir ve sonuç listesi aynı sırayı taşır.
- **AC-19.** Bir satırın eşzamanlılık kaybı **satır düzeyinde** kalır: `concurrency.lost` o satıra yazılır, diğer satırlar uygulanır.
- **AC-20.** Toplu eylem durum geçişi mantığını **tekrarlamaz**; karar agregatta kalır.
- **AC-21.** Toplu eylem aracın **son güncellenme** zamanını ilerletir.
- **AC-22.** Toplu eylem vitrin okuma modelini **aynı işlemde** günceller.
- **AC-23.** Toplu geçişler **duyurulur**: başarılı her geçiş kendi entegrasyon olayını yazar.

### D. Öne çıkarma

- **AC-24.** Öne çıkarılan ilan vitrin sıralamasının **başında** görünür.
- **AC-25.** Öne çıkarma **duyuru üretmez**: outbox'a satır yazılmaz.
- **AC-26.** Öne çıkarma **idempotenttir**: zaten öne çıkarılmış kayıt **başarılı** döner.
- **AC-27.** Öne çıkarmayı kaldırma da **idempotenttir**: zaten öne çıkarılmamış kayıt **başarılı** döner.
- **AC-28.** Arşivlenmiş ilan öne çıkarılamaz: yanıt `listing.cannot_feature.archived` ve `409` taşır.
- **AC-29.** Öne çıkarma **arşiv dışındaki her durumda** çalışır: taslak, yayında, rezerve ve satıldı.
- **AC-30.** Öne çıkarma aracın **son güncellenme** zamanını ilerletir.
- **AC-31.** Öne çıkarma anahtarı **iki değerlidir** ve geliştirme seed'i o iki değere hizalıdır.
- **AC-32.** Panel listesi **hangi aracın öne çıkarıldığını** gösterir.
- **AC-33.** Öne çıkarma **yalnız liste ekranında** yaşar; düzenleme ekranının eylem çubuğunda yoktur.

### E. Okuma bütçesi

- **AC-34.** Beş okuma istek başına **bir kez** yapılır: zorunlu alan kümesi · public alan anahtarları · şube başına ad haritası · kanonik model adları · seçimdeki araçların kapak görseli adresleri.
- **AC-35.** Medya hazırlığı **kayıt başına** ve **kilidin altında** okunur.

### F. Panel yüzeyi

- **AC-36.** Seçim **sayfaya aittir**: sayfa değişince temizlenir.
- **AC-37.** Eylem sonucunun **başarılı** durumu basılır ve kaç kaydın uygulandığı **ICU** ile söylenir.
- **AC-38.** **Kısmi başarı** dördüncü ekran durumu olarak basılır: uygulanmayanlar **tek tek gerekçesiyle** listelenir ve sonuç "başarılı" ya da "hata" olarak yuvarlanmaz.
- **AC-39.** Panel hiçbir teknik detay göstermez: hata kodu, HTTP durumu ve rota yolu ekranda görünmez.
- **AC-40.** Panel personelin tercih ettiği dilde çalışır; Arapça'da yüzey RTL ve metinler Arapçadır.
- **AC-43.** Arşivlenmiş satırda öne çıkarma düğmesi **yoktur**: panel, sunucunun reddedeceği bir eylemi göstermez.
- **AC-44.** Satır başına öne çıkarmanın reddi **o satırda** gerekçesiyle görünür; sessizce hiçbir şey yapmayan bir düğme yoktur.

### G. Sınır ve kayıt

- **AC-41.** Şube kapanışı yolu seçim üst sınırına **tabi değildir**.
- **AC-42.** `VehicleCatalog` `InvariantGuardOptions`'ı **kendi** kaydeder; başka modülün kaydına bağımlı değildir.

---

## Definition of Done

<!-- MADDE ÇIKARILAMAZ. Şablondaki her maddenin bir `<!-- DoD:Tn -->` işaretleyicisi vardır
     ve mimari test (ADR 0008 / K8) sayıyı şablondan okuyarak denetler. -->

- [x] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi
      (**D43–D55**; taşınan kararlar `0012b`'de donmuş hâlde). *Son açık soru S1,
      `d969dfb` turunda **D43** ile kapandı; kriter turuna giren bekleyen karar yok.*
- [x] `0012b` tamamlandı (tekil komutlar ve duyurular hazır).
      *2026-10-11, `e67a61d` (#60) ve kapanışı `2255370` (#61); kart `specs/done/` altında,
      58 kriter, 29 DoD kutusu. Ölçüldü: altı eylem ucu
      `VehicleAdminEndpoints.cs:69`'da, duyurular `VehicleCatalogUnitOfWork`'te.*
- [x] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
      *Kırk iki kriterin kırk ikisi; tablo PR'da.*
- [x] <!-- DoD:T2 --> Arayüz kriterlerinin görsel kanıtı eklendi (`docs/evidence/0012c-ac<n>.png`),
      **kısmi başarı** ve hata durumları dâhil, RTL için Arapça görüntü de var.
      *Beş görüntü, **hiçbiri stub değil**. Sınır kriterlerinin (AC-5, AC-6, AC-7) görsel
      kanıtı YOK ve gerekçesi **D52**: istemci `pageSize` göndermiyor, sınır panelden
      tetiklenemiyor.*
- [x] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`). *1097 test.*
- [x] <!-- DoD:T4 --> Mimari testler yeşil; durum geçişi mantığı agregatta kaldı. *50 test;
      `TopluEylemSiniriTests` toplu komutun agregat metodu çağırmadığını ve toplu SQL ile
      yazmadığını denetliyor.*
- [x] <!-- DoD:T5 --> Yeni migration varsa yalnızca `vehicle_catalog` şemasına dokunuyor.
      *Migration **YOK**: öne çıkarma mevcut `showcase_sort_weight` kolonunu iki değerli
      kullanıyor (**D16**) ve yeni kolon açmıyor.*
- [x] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
      *23 anahtar × 3 katalog; 386 anahtar eşit.*
- [x] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
      *«Öne Çıkarma» — sözlükte hiçbir dokümanda geçmiyordu, ölçüldü.*
- [x] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
      *Üç satır: okuma bütçesi (**D46**), öne çıkarma (**D16**/**D48**) ve modülün kendi
      DI kaydı (**D55**).*
- [x] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4). *`CI` ve `Performans`
      iş akışlarının ikisi de **ilk koşuda** başarılı (`23af1e7`).*
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [x] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
      *Karşılanmayan kriter yok.*
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.
- [x] Kısmi başarının **dördüncü ekran durumu** olarak basıldığı görsel kanıtla gösterildi.
      *`0012c-ac38.png` — gerçekten reddedilen bir satırla.*
- [x] Öne çıkarmanın duyuru **üretmediği** testle gösterildi (`0012b` **D16**).
      *`OneCikarmaKomutuTests` outbox TABLOSUNU sayıyor: iki eylem, sıfır satır.*
- [x] Seçim sınırının **dâhil** olduğu, **kırpılmadığı** ve tekilleştirmenin sınırdan
      **önce** geldiği testle gösterildi (`0012b` **D29**) — kriter **API düzeyinde** ve
      görsel kanıtı yok; gerekçesi **D52**. *`TopluSecimTests` + `TopluEylemZinciriTests`;
      uç testi sınırı yapılandırmadan okuyor ve üretim varsayılanını DEĞİŞTİREREK ölçüyor.*
- [x] Başarısız satırın tekil uçla **aynı gerekçeyi** (kod + yapısal parametreler) taşıdığı
      testle gösterildi; toplu yayın reddinde `reasons` parametresi satırda duruyor (**D45**).
      *`TopluEylemTests.Tasir_AyniGerekceyi_TekilUcla` + `bulkActionState.test.ts`.*
- [x] İstek başına bir kez okunan beş okumanın gerçekten **bir kez** okunduğu ve medya
      hazırlığının **kayıt başına, kilidin altında** kaldığı testle gösterildi (**D46**).
      *Sayan sahtelerle: dört araç tek şube → bir okuma; üç yayın → üç hazırlık okuması.*
- [x] Bir satırın eşzamanlılık kaybının diğer satırları **düşürmediği** ve satırların
      gönderildiği **sırada** işlendiği testle gösterildi (**D47**).
      *Ortadaki ilanın satırı başka bir bağlantıda `FOR UPDATE` ile tutuluyor; o satır
      `concurrency.lost` alıyor, komşuları uygulanıyor.*
- [x] Öne çıkarmanın **idempotent** olduğu (zaten o hâlde olan kayıt **başarılı**) ve
      arşivlenmiş ilanın `listing.cannot_feature.archived` ile **`409`** aldığı testle
      gösterildi (**D48**, **D49**). *İki yön de idempotent; `409` uç testinde.*
- [x] Öne çıkarma durumunun panel listesinde **göründüğü** görsel kanıtla gösterildi; eylem
      **yalnız liste ekranında** (**D50**, **D51**). `0010b`'nin sözleşmesine eklenen alan
      PR'da **adıyla** yazıldı. *`VehicleAdminListItemDto.IsFeatured`; `0012c-ac32.png`.*
- [x] Seçimin **sayfaya ait** olduğu ve sayfa değişince temizlendiği testle gösterildi
      (**D43**). *Sayfa VE arşiv filtresi için ayrı iki test.*
- [x] `VehicleCatalog`'un `InvariantGuardOptions`'ı **kendi** kaydettiği testle gösterildi;
      `0012b`'nin kartındaki `Kaçan hata` hücresi **1**'e çekildi ve düzeltme PR'da adıyla
      yazıldı (**D55**, **C9**). *`ModulKaydiTests` kayıt defterini okuyor; sonda yapıldı.*
- [ ] Satır başına öne çıkarmanın **hata yolu** gösterildi: arşivlenmiş satırda düğme
      **yok** ve sunucunun reddi **o satırda** gerekçesiyle görünüyor (**AC-43**, **AC-44**);
      ikisinin görsel kanıtı var.
- [ ] Modül başına tiplenen kilit seçeneği (**D57**) kuruldu ve paylaşılan non-generic bir
      seçenek kaydının geri gelmesini **bir mimari kapı** yasaklıyor.

---

## SCORECARD

<!--
İş bittikten SONRA doldurulur. Amaç suçlu bulmak değil, sürecin nerede sızdırdığını görmek.
Dürüst doldurulmayan scorecard işe yaramaz — abartılmış iyi sayılar en pahalı yalandır.
-->

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | evet / hayır | R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır. |
| **Kriter sayısı: önce → bugün** | → | O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. |
| **Spec revizyon sayısı** | | Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3). Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? **`0` ile `—` aynı şey değildir.** **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı. Dış bulguyla aynı sütunda toplanmaz. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır.** Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak (ADR 0008 / K3). |

**Bu spec'ten çıkarılan ders:**
