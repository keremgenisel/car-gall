# ADR 0001 — Modüler Monolit

| | |
|---|---|
| **Durum** | **Kabul** |
| **Tarih** | 2026-09-21 |
| **Karar mercii** | Takım Yöneticisi |
| **İlgili doküman** | `docs/architecture.md` |

---

## Bağlam

Tek bir araç galerisi için satış vitrini ve yönetim paneli kuruyoruz. Karar anında bilinenler:

**Ürün ve işletme**
- Kurulum başına **tek galeri** (white-label); ürün birden fazla galeriye satılacak, her biri
  **ayrı kurulum ve ayrı veritabanı** ile çalışacak. Yani işletilecek kurulum sayısı zamanla
  artar — operasyon maliyeti kişi başına değil, kurulum başına çarpanla büyür.
- Galerinin birden çok **şubesi** olabilir; her araç bir şubede durur.
- Farklı ülkelerde çalışacak: **TR / AR / EN**, tam RTL, kurulum başına tek para birimi
  (KWD gibi üç ondalıklı birimler dâhil).

**Domain**
- İki çekirdek alan var: **araç stoğu/vitrini** ve **talep (lead) dönüşümü**.
- Yanlarında yedi destek alanı: galeri & şubeler, referans veri, medya, içerik, bildirim,
  denetim, etkileşim sayaçları.
- Bu alanların **yaşam döngüleri belirgin biçimde farklı**: referans veri yılda birkaç kez
  değişir, sayaçlar saniyede onlarca kez yazılır, medya asenkron işlenir, talepler insan
  hızında ilerler.

**Ekip ve belirsizlik**
- Fiilen tek geliştirici + AI ajan üyeler. Operasyon için ayrılmış bir kapasite yok.
- Birkaç alan ileride girebilir ama **V1'de yok**: ilan besleme (syndication), araç alım/
  finans takibi, gelişmiş arama (Elasticsearch). Bunların gireceği yerin şimdiden belli
  olması, girdiklerinde her şeyi yeniden yazmamak için gerekli.
- Veri hacmi küçük: birkaç yüz araç, birkaç bin talep. Performans bir kısıt değil;
  **değiştirilebilirlik** kısıt.

**Asıl gerilim**
Sınırları olmayan bir uygulama altı ay içinde birbirine dolanır — özellikle kodun önemli
bir kısmını AI ajanların yazacağı bir düzende, çünkü ajan en kısa yolu bulur ve en kısa yol
neredeyse her zaman bir sınırın içinden geçer. Öte yandan sınırları **ağ üzerinden** çizmek
(mikroservis), tek kişilik bir ekibe ve kurulum başına çarpanla büyüyen bir operasyona
kaldıramayacağı bir yük bindirir.

Karar, bu iki başarısızlık biçiminin arasından geçmek zorunda.

---

## Karar

**Modüler monolit kuruyoruz.** Tek dağıtılabilir uygulama, tek veritabanı; içinde
**derleyici ve veritabanı tarafından dayatılan** modül sınırları.

Kararın bağlayıcı unsurları:

1. **Tek host, tek proses.** .NET Minimal API. Modüller aynı süreçte çalışır.
2. **Tek veritabanı, modül başına şema.** PostgreSQL. Her modülün kendi `DbContext`'i,
   kendi migration zinciri, **kendi DB rolü** — sınır veritabanı seviyesinde de dayatılır.
3. **Modül başına iki proje:** `Modules/X/X` (tamamı `internal`) ve `Modules/X/X.Contracts`
   (tek public yüzey). Bir modülün başka modülden görebildiği **tek** şey `X.Contracts`'tır.
4. **İki iletişim biçimi, net ayrım.** Senkron doğrudan çağrı yalnızca salt okuma/doğrulama
   için ve **tek atlama**; durum değişikliğine tepki her zaman **entegrasyon olayı** ile.
5. **Tek teslimat mekanizması: outbox.** Olay, iş verisiyle aynı transaction'da yazılır;
   en-az-bir-kez teslim edilir; tüketiciler idempotenttir. Broker yok — ama geçiş noktası
   tek bir dağıtıcı sınıfıdır.
6. **Bir istek = bir modülde bir transaction.** Modül sınırını aşan transaction yoktur.
7. **Sınır kod olarak dayatılır.** `Architecture.Tests` ilk gün yazılır; yasak referansları,
   `public` sızıntılarını, `double` ile para tanımını, `DateTime.Now` kullanımını CI'da kırar.
   CI ayrıca modül X'in migration'ının yalnızca `x` şemasına dokunduğunu doğrular.

Modül listesi, sahiplik, iletişim kuralları ve 43 maddelik yasak listesi
**`docs/architecture.md`** içindedir. Bu ADR o dosyanın özeti değil, **gerekçesidir**;
kural değiştiğinde `docs/architecture.md` güncellenir, bu dosya değil.

---

## Sonuçlar

### İyi taraf

- **Operasyon tek haneli kalır.** Bir galeri = bir uygulama + bir veritabanı. Yirmi galeri
  yirmi kurulum demek; yirmi galeri × sekiz servis demek değil. Kurulum başına çarpanla
  büyüyen operasyonda bu farkın tek başına kararı belirleyecek ağırlığı var.
- **Sınırlar gerçek, temenni değil.** Derleyici, DB rolü ve mimari testler üç ayrı katmanda
  dayatıyor. Bir ajan sınırı ihlal ettiğinde kod **derlenmez** veya **CI kırılır** — kimsenin
  gözden geçirmede yakalamasına gerek kalmaz. Bu, ajan üyelerle çalışırken kararın en değerli
  özelliği.
- **Modül içinde hayat basit.** Referans bütünlüğü, transaction, join, test kurulumu —
  hepsi tek veritabanında olduğu gibi çalışır. Dağıtık sistemlerin maliyetini yalnızca
  sınırlarda ödüyoruz, her yerde değil.
- **Belirsiz alanların yeri hazır.** Syndication, alım/finans, ayrı arama modülü girdiğinde
  nereye gireceği ve neyi dinleyeceği bugünden belli. Girdiklerinde mevcut modüller
  değişmiyor — yeni bir abone ekleniyor.
- **Geliştirme ve test ucuz.** Tek proses ayağa kalkar; entegrasyon testleri tek bir
  Testcontainers PostgreSQL ile koşar. Ağ sahtelemesi, servis orkestrasyonu, dağıtık iz
  sürme yok.
- **Mikroservise geçiş yolu kapalı değil.** Outbox, contracts ve şema ayrımı zaten yerinde;
  bir modülü ayırmak, dağıtıcıyı broker'a çevirmek ve o şemayı taşımak demek. Bunu **bugün
  yapmıyoruz**, ama yapamaz hâle de gelmiyoruz.

### Bedel

Bunlar kararın gizli maliyeti değil, **bilinen fiyatı**:

- **Tek hata alanı.** Bir modülün bellek sızıntısı, sonsuz döngüsü veya kilitlenmesi tüm
  uygulamayı etkiler. Medya işleme gibi ağır işler ana prosesi meşgul eder.
  *Kısmi azaltma:* ağır işler arka plan çalışanına alınır, ama aynı proseste kalırlar.
- **Bağımsız ölçekleme yok.** Vitrin trafiği artarsa admin paneli de birlikte ölçeklenir.
  Tek galeri ölçeğinde bu bir sorun değil — ama bir sınırlama.
- **Bağımsız dağıtım yok.** Bir içerik metni düzeltmesi tüm uygulamayı yeniden dağıttırır.
  Dağıtım riski her seferinde tüm yüzeye yayılır.
- **Paylaşılan veritabanı, paylaşılan kader.** Tek migration penceresi, tek yedekleme
  politikası, tek bağlantı havuzu. Bir modülün ağır sorgusu diğerlerinin bağlantılarını
  tüketebilir.
- **Proje sayısı ~20.** Dokuz modül × iki proje + host + BuildingBlocks + test projeleri.
  Çözüm büyük görünür, build süresi tek projeli bir uygulamaya göre uzundur.
  Bunu **bilinçli olarak** ödüyoruz: klasör ayrımı sınır dayatmaz, proje ayrımı dayatır.
- **Tek prosesteyken bile nihai tutarlılık.** Bir araç yayınlandığında vitrin okuma modeli
  birkaç saniye sonra güncellenir. Geliştiriciye ve kullanıcıya "neden hemen görünmedi?"
  dedirtir; admin arayüzünde açıkça gösterilmesi gerekir.
- **Bilinçli veri tekrarı.** Contracts DTO'ları, çeviri tabloları ve vitrin okuma modeli,
  "DRY" refleksine ters gelen kopyalar üretir. Sınır boyunca tekrar bir hata değil, kararın
  parçasıdır — ama her yeni geliştiriciye/ajana ayrıca anlatılması gerekir.
- **Mimari testlerin bakımı sürekli bir iştir.** Yazılmazsa veya çürümesine izin verilirse,
  bu ADR'nin dayandığı tek gerçek dayatma mekanizması kaybolur ve elimizde klasörlere
  bölünmüş bir katmanlı monolit kalır.

### Ne zaman yeniden değerlendirilir

Bu karar şu durumlardan biri gerçekleşene kadar geçerlidir:

- **İşletilen kurulum sayısı 15–20'yi geçerse** — o zaman tartışma "monolit mi mikroservis mi"
  değil, "kurulum başına ayrı dağıtım mı, çok kiracılı tek kurulum mu" olur.
- **Bir modül ölçekleme veya dağıtım açısından diğerlerini rehin alırsa** — en olası aday
  medya işleme. O modül önce ayrı bir arka plan çalışanına, gerekirse ayrı servise çıkar.
- **Ekip birden fazla bağımsız akışa bölünürse** — dağıtım kuyruğu darboğaz olduğunda
  bağımsız dağıtımın bedeli ödenmeye değer hâle gelir.
- **Mimari testler bakımsız kalırsa** — bu bir teknoloji değil disiplin sinyalidir ve
  kararın önkoşulunun çöktüğü anlamına gelir.

---

## Değerlendirilen Alternatifler

### 1. Katmanlı monolit (tek `DbContext`, klasör ayrımı)
Controllers / Services / Repositories / Entities; modüller yalnızca klasör.

**Reddedildi.** Sınır yalnızca **sosyal** bir anlaşma olurdu: hiçbir şey bir servisin başka
bir alanın tablosuna join atmasını engellemez. Küçük ekiplerde bile altı ay içinde dolanır;
kodun önemli kısmını AI ajanların yazdığı bir düzende çok daha hızlı dolanır, çünkü ajan
mevcut `DbContext` üzerinden en kısa yolu bulur ve o yol neredeyse her zaman bir sınırın
içinden geçer. Başlangıçta en ucuz, altı ay sonra en pahalı seçenek.

### 2. Mikroservisler
Her alan ayrı servis, ayrı veritabanı, broker üzerinden iletişim.

**Reddedildi.** Sınır dayatması mükemmel olurdu, fiyatı karşılanamaz: her galeri kurulumu
tek bir uygulama yerine bir servis kümesi anlamına gelir — kurulum sayısıyla çarpılan bir
operasyon yükü. Ayrıca dağıtık transaction, dağıtık iz sürme, sürüm uyumluluğu ve yerel
geliştirme karmaşıklığı; hepsi tek geliştiricili bir ekip için. Veri hacmi ve trafik bu
maliyeti haklı çıkaracak seviyenin çok altında. **Çözülecek bir ölçek sorunumuz yok;
çözülecek bir değiştirilebilirlik sorunumuz var.**

### 3. Sunum eksenine göre ikiye bölme (public site servisi + admin API)
İki dağıtılabilir uygulama: vitrin ve panel.

**Reddedildi.** Bölme ekseni yanlış. Bu ayrım **domain sınırından değil, kullanıcı tipinden**
geçiyor; sonuç, aynı araç verisine yazan iki uygulama olurdu. Yanlış yerden çizilen bir
sınır, sınırsızlıktan daha pahalıdır: hem dağıtık sistemin maliyetini ödersin hem de
izolasyonu elde edemezsin.

### 4. Modüler monolit, ama tek şema
Modül ayrımı yalnızca kod tarafında; veritabanında tek şema.

**Reddedildi.** Dayatmanın üç katmanından birini — en ucuz olanını — kaybederdik.
Tek şemada cross-module join hem teknik olarak mümkün hem de "sadece bu sefer" demek için
fazlasıyla kolay. Şema ayrımının maliyeti neredeyse sıfır; getirisi ise DB rolleriyle
gerçek bir okuma/yazma izolasyonu.

### 5. Modül başına ayrı veritabanı (tek proses)
Kod monolit, veri tamamen ayrı.

**Reddedildi.** İzolasyon şema ayrımının verdiğinden belirgin biçimde fazla değil, ama
maliyeti belirgin biçimde fazla: kurulum başına dokuz veritabanı, dokuz yedekleme
politikası, dokuz bağlantı dizesi — ve bunlar kurulum sayısıyla çarpılıyor. Ayrıca her
raporlama ve kompozisyon ihtiyacı anında dağıtık sorgu problemine dönüşüyor.
**Şema ayrımı, bu izolasyonun büyük kısmını çok daha ucuza veriyor.**
