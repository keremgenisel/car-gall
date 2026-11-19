# 0012 — Dağıtım paketi: tek imaj, iki komut, sırsız

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 2026-11-18 |
| **Karar veren** | Takım Yöneticisi (ajan) kararı, 2026-11-18 — depo sahibinin "önce paketi hazırla" seçimi üzerine |
| **İlgili** | ADR `0002` (kurulum adımı), ADR `0011` (sabit parola yok), `0003` D8 (tzdata), `0005` C17 (hız sınırı), `0006` AC-30 (köken koruması); `docs/deploy.md` |

---

## Bağlam

Depo sahibi 2026-11-18'de "son hâlini canlıya al" dedi. Ölçüldü: **canlıya alınacak bir
yol yoktu.** Depoda Dockerfile, dağıtım iş akışı, barındırma yapılandırması ya da canlı
ortamın hangi değerlere ihtiyaç duyduğunu söyleyen bir belge yok; `.github/workflows`
altında yalnız `ci.yml` ve `perf.yml` var. Uygulamayı ayağa kaldıran tek yol
`scripts/dev-up.sh` ve o bir **geliştirme** ortamı kuruyor: tohum verisi, düşük parola
maliyeti, yerel ikizler.

Barındırma seçimi (sağlayıcı, alan adı, bütçe, sırların nerede tutulacağı) geri dönüşü
pahalı ve dışa dönük bir karardır; Takım Yöneticisi yetkisinde değil
(`docs/roles/README.md` § Takım Yöneticisi). Depo sahibi önce **paketin** hazırlanmasını
seçti: hiçbir yere dağıtım yapılmadan, her sağlayıcıda çalışacak bir imaj ve yönerge.

Paket hazırlanıp **canlıya benzer** biçimde koşturulunca — boş veritabanı, geliştirme
bayrakları kapalı, üretim parola maliyeti, TLS'i sonlandıran gerçek bir ters proxy —
dört şey ölçüldü. İkisi kararları biçimlendirdi (K3, K4), ikisi bu ADR'nin çözmediği
engellerdir (§ Açık engeller).

## Kararlar

### K1 — Tek imaj; ön yüz imajın içinde derlenir

Depo kökündeki `Dockerfile` üç aşamalıdır: Node ile ön yüz derlenir, SDK ile Host
yayımlanır, çalışma imajı (`mcr.microsoft.com/dotnet/aspnet:10.0`) yalnız çıktıyı taşır.
Ön yüz ayrı bir sunucuda **servis edilmez**: Host `index.html`'e `lang`/`dir`, tema ve
etkin dilleri sunucudan basıyor (`docs/architecture.md` § 3), ayrı bir statik sunucu
bunları kaybederdi.

*Alternatif:* önceden derlenmiş `wwwroot`'u imaja kopyalamak. Elendi — yerel bir
derlemenin artığı (ör. başka bir dalın paketi) sessizce imaja girerdi. `wwwroot`
`.dockerignore`'da; imaja yalnız ilk aşamanın çıktısı girer.

### K2 — Aynı imaj iki komutla çalışır; imajda sır ve kuruluma özel değer yoktur

- `<imaj> --migrate` → kurulum adımı (ADR `0002`): şemayı uygular, çıkar.
- `<imaj>` → uygulama: şemayı yalnız doğrular, eksikse başlamaz.

Bütün yapılandırma çalıştırma anında **ortam değişkeninden** gelir; zorunlu olanların
listesi `docs/deploy.md` § Yapılandırma'dadır. İmaj galeriden bağımsızdır — aynı imaj
her kurulumda çalışır ve white-label'ın "kurulum başına ayrı uygulama" ilkesi imajı
değil yapılandırmayı çoğaltır. ADR `0011`'in kuralı imaja da uygulanır: varsayılan bir
parola, anahtar ya da bağlantı dizesi imaja yazılmaz.

### K3 — İmaj bir init süreci (`tini`) taşır

**Ölçülmüş bir hatadan.** Şema eksikken uygulama "kurulum adımını çalıştırın" mesajını
basıp **sonsuza kadar takılı kaldı**: konteyner `running`, çıkış kodu 0, 40 saniye
boyunca. Neden: `dotnet` PID 1 olunca çekirdek ona varsayılan eylemli sinyalleri
iletmez ve işlenmemiş istisnanın `abort`'u boşa düşer. `docker run --init` ile aynı
hata **2 saniyede 134** ile çıktı.

Bunun canlıdaki karşılığı ADR `0002`'nin korumasının tersine dönmesidir: o ADR atlanan
kurulum adımını "gürültülü bir duruş"a çevirmek için yazıldı; takılı kalan bir konteyner
ise sağlıklı görünür, yeniden başlatılmaz ve isteklere cevap vermez. Çoğu platform
(`--init`'i bilmeyen orkestratörler dâhil) init eklemediği için init imajın içinde olmak
zorunda.

### K4 — Ters proxy şemayı da iletir (`X-Forwarded-Proto`)

**Ölçülmüş bir engelden.** Oturum çerezi `Secure` (0006), yani canlıda HTTPS zorunlu ve
standart kurulumda TLS ters proxy'de biter. Yönetim yüzeyinin köken koruması
(`CrossOriginGuard`, 0006 AC-30) beklenen kökeni `Request.Scheme://Host`'tan kuruyor;
uygulama isteği `http` gördüğü için tarayıcının `https://` Origin'i eşleşmiyordu ve
**giriş dâhil her yazma isteği 403** alıyordu — yani hiçbir personel panele giremezdi.
`ForwardedHeadersSetup` yalnız `X-Forwarded-For`'u işlediği için proxy doğru
yapılandırılsa bile sonuç aynıydı (ölçüldü: `X-Forwarded-Proto: https` ile 403).

Karar: `ForwardedHeadersSetup` `XForwardedFor | XForwardedProto` işler. **Güven sınırı
değişmedi:** ara katman yalnız `ForwardedHeaders:KnownProxies`/`KnownNetworks`
listelendiğinde eklenir ve başlıkları yalnız oradan gelirse okur. Regresyon testi
`tests/Host.Tests/TersProxyArkasindaGirisTests.cs` iki yönü de sınar: listelenmiş proxy
`https` iletince giriş 200, proxy listelenmemişken istemcinin başlığı okunmaz ve 403.

*Alternatif:* uygulamanın TLS'i kendisinin sonlandırması. Elendi — sertifika yenileme
uygulamanın işi olurdu ve barındırma platformlarının neredeyse hepsi TLS'i önde
sonlandırıyor.

### K5 — Çalışma imajı Debian tabanlıdır; Alpine ve chiseled seçilmedi

Şube saat dilimi doğrulaması (`0003` D8) tzdata olmadan **geçerli** bölge kimliğini de
reddeder; Türkçe/Arapça kültür biçimleri ICU ister. Debian tabanlı `aspnet` imajı
ikisini de taşıyor (ölçüldü: `/usr/share/zoneinfo/Europe/Istanbul`,
`Asia/Kuwait`, `libicuuc.so.74`). Alpine ve chiseled imajlar ikisini de varsayılan
olarak taşımaz; seçilselerdi D8'in koruduğu hata canlıda sessizce dönerdi. İmaj kök
olmayan `app` kullanıcısıyla (uid 1654) ve 8080 portunda çalışır.

### K6 — CI imajı derlemez (bu ADR'de)

İmaj derlemesi bir CI kapısına bağlanmadı. ADR `0010` dakika bütçesi koyuyor ve imaj
derlemesi ~1 dakikalık önbelleksiz bir iş; ölçmeden kapı eklemek o bütçeyi
tahminle harcamak olurdu. Bedeli açık: Dockerfile'ı kıran bir değişiklik (ör. yeni bir
proje dizini, `.dockerignore`'a takılan bir dosya) ancak bir sonraki dağıtımda görünür.
Barındırma seçildiğinde dağıtım iş akışı imajı zaten derleyecek; kapı o gün o iş
akışının kendisidir.

## Açık engeller — bu ADR'nin çözmediği

Paket hazır, ama ölçüm canlıya çıkışı engelleyen **iki** şey daha buldu. İkisi de
davranış değişikliğidir ve spec ister; burada yalnız kayda geçiyor.

1. **Kurulum adımı her canlı veritabanına bir demo galeri ve iki uydurma şube
   yazıyor.** `SeedInitialDealership` migration'ı (`0003` D1) "Oto Galeri" ticari adını,
   `1234567890` vergi numarasını ve iki İstanbul şubesini (adres, `+90216…` telefon,
   `kadikoy@otogaleri.com.tr` e-postası) veri olarak ekliyor. Ölçüldü: tohum bayrağı
   kapalı, boş bir veritabanında yalnız `--migrate` koşunca ana sayfanın altlığında bu
   iki şube görünüyor. **Galeri profilini ve şubeleri değiştirecek hiçbir yazma ucu ya
   da panel ekranı yok** — `0003` X1 bunları kapsam dışı bıraktı ve sonraki hiçbir kart
   üstlenmedi. Canlıda bu veriyi değiştirmenin bugünkü tek yolu veritabanına elle SQL
   yazmaktır.

   **Çözüldü — spec `0020` (2026-11-18).** Galeri profili ve şubeler artık panelden
   düzenleniyor (yalnız Yönetici); demo veri ilk girişte düzeltilir ya da kapatılır.
   Kurulum adımının örnek veriyi yazması bilinçli olarak değişmedi (`0020` X5 · D10:
   şubesiz başlayan galeri B5'i ilk andan kırardı). Adım `docs/deploy.md` § 5'te.
2. **Barındırma seçilmedi** (sağlayıcı, alan adı, PostgreSQL, S3-uyumlu depo, SMTP,
   sır deposu). Depo sahibinin kararı.

## Sonuçlar

### İyi taraf

- Canlıya çıkışın teknik yolu ilk kez var ve **her sağlayıcıda aynı**: bir imaj, iki
  komut, bir ortam değişkeni listesi.
- Canlıya benzer koşu, hiçbir testin görmediği iki hatayı (K3, K4) canlıdan **önce**
  yakaladı; ikisi de "uygulama ayakta görünüyor ama çalışmıyor" sınıfındandı.

### Bedel

- İmaj ~1 dakikada derleniyor ve ön yüz her derlemede baştan kuruluyor; katman önbelleği
  yalnız `package-lock.json` değişmediğinde `npm ci`'yi atlıyor.
- Dockerfile'ın bozulması CI'da görünmez (K6).
- `tini` için çalışma imajına bir `apt-get` katmanı eklendi; imaj tekrarlanabilirliği
  Debian deposunun o anki sürümüne bağlı.
