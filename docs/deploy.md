# Canlıya Alma

Bir galeriyi canlıya almanın tek yolu. Kararların gerekçesi ADR `0012`'de; burada
yalnız **ne yapılacağı** yazılı.

> **Bugün canlıya çıkışı engelleyen iki şey var** (ADR `0012` § Açık engeller):
> kurulum adımı her veritabanına bir **demo galeri ve iki uydurma şube** yazıyor ve
> bunları değiştirecek bir panel ekranı yok; barındırma da seçilmedi. Aşağıdaki adımlar
> paketin kendisini çalıştırır, ama siteyi ziyaretçiye açmadan önce § 5 okunmalı.

---

## 1. Gerekenler

| Bileşen | Not |
|---|---|
| Konteyner çalıştırabilen bir sunucu ya da platform | İmaj yalnız `linux/arm64`'te ölçüldü; `amd64` için § 7 |
| PostgreSQL 17 | Tek veritabanı; modüller kendi şemalarını kurar |
| S3-uyumlu nesne deposu + herkese açık okuma adresi (CDN) | R2, MinIO, S3. Kova önceden açılır |
| SMTP | Talep bildirimleri ve personel e-postaları buradan gider |
| TLS'i sonlandıran ters proxy | **Zorunlu** — oturum çerezi `Secure`, HTTP'de panele girilemez |

## 2. İmajı derle

Depo kökünde:

```bash
docker build -t cargall:<sürüm> .
```

İmajda sır ve galeriye özel değer **yoktur** (ADR `0012` / K2); aynı imaj her
kurulumda çalışır.

## 3. Yapılandırma

Bütün değerler ortam değişkenidir. `__` iki nokta yerine geçer
(`Identity__SeedAdmin__Password` = `Identity:SeedAdmin:Password`).

`appsettings.json`'daki değerler **geliştirme varsayılanlarıdır** (`localhost`). Aşağıdaki
"zorunlu" satırların bir kısmı orada bir değer taşıyor ve verilmezse uygulama
`localhost`'a bağlanmaya çalışır — o yüzden hepsi açıkça verilir.

### Sırlar — bir sır deposunda tutulur, hiçbir dosyaya yazılmaz

| Değişken | Değer |
|---|---|
| `ConnectionStrings__VehicleCatalog` | PostgreSQL bağlantı dizesi |
| `Identity__SeedAdmin__Password` | İlk Yönetici'nin parolası. Üretin: `openssl rand -hex 16`. İlk girişte değiştirilmesi zorunludur. Depodaki geliştirme parolaları reddedilir (ADR `0011`) |
| `Identity__TwoFactor__EncryptionKey` | 32 bayt, Base64: `openssl rand -base64 32`. **Kaybolursa bütün ikinci adım kurulumları çözülemez** — yedeklenir |
| `Notifications__Smtp__User`, `Notifications__Smtp__Password` | SMTP hesabı |
| `Notifications__Newsletter__UnsubscribeSigningKey` | Bülten çıkış bağlantılarının imza sırrı; 32 bayt, Base64: `openssl rand -base64 32`. **Değişirse eski bütün çıkış bağlantıları geçersiz olur** — eski bir bültenden çıkmak isteyen kişi "bağlantı geçerli değil" görür. Yedeklenir ve döndürülmez (`0018` D25) |
| `Media__Storage__AccessKey`, `Media__Storage__SecretKey` | Nesne deposu anahtarı |

### Kuruluma özel — zorunlu

| Değişken | Örnek |
|---|---|
| `Identity__SeedAdmin__LoginId` | `yonetici@galeri-alanadi.com` (giriş kimliği bir e-postadır) |
| `Notifications__PublicBaseUrl` | `https://galeri-alanadi.com` — e-postalardaki bağlantıların kökü |
| `Notifications__SetupNotificationAddress` | Yeni taleplerin bildirileceği adres |
| `Notifications__SenderAddress` | Gönderen adres; SMTP sağlayıcısında doğrulanmış olmalı |
| `Notifications__Smtp__Host`, `Notifications__Smtp__Port` | Sağlayıcının STARTTLS portu, genelde **587**. **Örtük TLS (465) desteklenmez** — uygulama yalnız STARTTLS ya da şifresiz bağlanır; 465 verilirse bağlantı kurulamaz ve bildirimler denemeleri tükenene kadar kuyrukta bekler |
| `Notifications__Smtp__UseStartTls` | `true` — geliştirme varsayılanı `false`; `false` ile kimlik bilgileri düz metin gider |
| `Media__Storage__ServiceUrl` | Deponun API adresi |
| `Media__Storage__Bucket`, `Media__Storage__Region` | |
| `Media__PublicBaseUrl` | Kovanın herkese açık okuma adresi, sonda `/` ile |
| `Media__WatermarkObjectKey` | Filigranın kovadaki anahtarı, ör. `private/brand/watermark.png` — **dosya önceden yüklenir** (galerinin logosu, saydam PNG) |
| `ForwardedHeaders__KnownProxies__0` ya da `ForwardedHeaders__KnownNetworks__0` | Ters proxy'nin adresi ya da ağı (`10.0.0.0/8`). Verilmezse panele girilemez, ve iki hız sınırı herkesi tek kişi sayar: talep sınırı bütün ziyaretçileri, giriş sınırı (10 dakikada 20 istek) bütün personeli (ADR `0012` / K4) |

### Kuruluma özel — varsayılanı olan

| Değişken | Varsayılan |
|---|---|
| `Localization__DefaultLocale` | `tr` |
| `Localization__EnabledLocales__0..n` | `tr`, `ar`, `en` |
| `Currency__Code`, `Currency__MinorUnitExponent` | `TRY`, `2` (KWD için `KWD`, `3`) |
| `Theme__…` | Varsayılan palet (ADR `0005`; ya tam ya hiç) |
| `Identity__TwoFactor__Issuer` | `CarGallery` — personelin kimlik doğrulayıcı uygulamasında görünen ad; galerinin adı verilir |
| `Media__Storage__ForcePathStyle` | `true` (MinIO/R2 için doğru); sanal-host adresleme isteyen sağlayıcıda `false` |
| `Notifications__Newsletter__SendDayOfWeek`, `…__SendTimeUtc` | `Monday`, `07:00` — bültenin haftalık gönderim anı, **UTC** (yerel saat yaz saatinde kayar). Panelden değiştirilmez (`0018` D2, D13) |
| `Notifications__Newsletter__ConfirmationLinkHours` | `72` — teyit bağlantısının süresi |
| `Notifications__Newsletter__RequestLimit`, `…__RequestWindowMinutes` | `30`, `10` — aynı IP'den abonelik ucuna her istek (`0018` D9) |
| `Notifications__Newsletter__ValidRequestLimit`, `…__ValidRequestWindowMinutes` | `5`, `10` — aynı IP'den geçerli abonelik isteği |
| `Notifications__Newsletter__AddressConfirmationLimit`, `…__AddressWindowHours` | `3`, `24` — aynı adrese teyit iletisi |

Bülten değerlerinin hepsi `appsettings.json`'da durur; biri **boşaltılırsa** uygulama ayağa
kalkmaz (koda gömülü varsayılan yok — `0018` C14).

**Asla verilmeyecekler:** `Development__SeedShowcase`, `Development__SeedListingCount`,
`Development__SeedHomeTexts`. İlk ikisi demo ilan ve `/seed-media` uçlarını açar;
`Development__SeedHomeTexts` her başlangıçta galerinin panelden yazdığı ana sayfa
metinlerini her dilde 120/500 karakterlik ölçüm dolgusuyla EZER (`0022` D11, yalnız `perf.yml`). `Identity__PasswordIterations`'ı da
düşürmeyin; geliştirme betiği bunu hız için 1000'e çeker, üretim değeri 210.000'dir.

## 4. Sıra: önce kurulum adımı, sonra uygulama

```bash
# 1) Şema — her yeni sürümde, uygulamadan ÖNCE. Biter ve 0 ile çıkar.
docker run --rm --env-file <sır-dosyası> cargall:<sürüm> --migrate

# 2) Uygulama — 8080'de dinler, kök olmayan kullanıcıyla çalışır.
docker run -d --restart unless-stopped -p 8080:8080 --env-file <sır-dosyası> cargall:<sürüm>
```

> **`0019` (izin metni sürümü) içeren sürüme geçerken: eski uygulama `--migrate`'ten ÖNCE
> durdurulur.** Bu sürümün migration'ı `privacy`'nin yayındaki her dili için bir başlangıç
> sürümü yazar ve canlı metni o sürüme bağlar (`published_version_id`). Migration'dan sonra
> çalışmaya devam eden ESKİ uygulama sürüm yazmaz ve iki ayrı arıza bırakır:
>
> - **Bir dili taslaktan yayına alırsa** o dilin işaretçisi boş kalır: yeni uygulama o dilde
>   bırakılan her talebi reddeder (`500`, `0019` D14 — yanlış bir "yayında değildi"
>   yazılmaz).
> - **Yayındaki metni düzenlerse** işaretçi hâlâ başlangıç sürümünü gösterir ama canlı metin
>   artık o değildir: yeni uygulama bu uyuşmazlığı okuma anında yakalar ve o dilde talebi
>   yine reddeder (`0019` P13 — talep canlı metinle aynı olmayan bir sürüme bağlanmaz).
>
> **Kurtarma:** yeni uygulama çalışırken panelden o dilin metnini değiştirip kaydet **ya da**
> dili yayından kaldırıp yeniden yayına al; ikisi de yeni bir sürüm yazar ve talepler yeniden
> kabul edilir. **Bu sürümden sonra eski uygulama imajına dönülmez:** migration'lar geri
> alınamaz (`0019` D23) ve eski imaj aynı iki arızayı yeniden üretir. Sıra: (1) eski uygulamayı
> durdur, (2) `--migrate`, (3) yeni uygulamayı başlat. Bedel kısa bir kesinti penceresidir
> (`0019` C10, D15).

Kurulum adımı atlanırsa uygulama **başlamaz** ve hangi modüllerin eksik olduğunu yazıp
134 ile çıkar (ADR `0002`). Bu bir arıza değil, korumadır: yeniden başlatmak çözmez,
`--migrate` çözer.

Ters proxy `Host` başlığını değiştirmeden ve `X-Forwarded-For` + `X-Forwarded-Proto`
ekleyerek 8080'e iletir. Caddy örneği:

```
galeri-alanadi.com {
    reverse_proxy app:8080
}
```

## 5. Ziyaretçiye açmadan önce

1. **Galeri profili ve şubeler.** Kurulum adımı "Oto Galeri" adında, `1234567890` vergi
   numaralı bir demo galeri ve iki uydurma İstanbul şubesi yazar (ADR `0012` § Açık
   engeller 1). Spec `0020`'den beri bunlar **panelden** düzeltilir; site ziyaretçiye
   açılmadan önce, aşağıdaki ilk Yönetici girişinin hemen ardından:
   - **Panel → Galeri** (`/admin/gallery`): ticari unvan, vergi ve ticaret sicil numarası;
     görünen ad (varsayılan dilde zorunlu, diğer diller opsiyonel). Görünen ad sitenin
     üstünde ve ana sayfada görünür; ticari unvan görünmez.
   - **Panel → Şubeler** (`/admin/branches`): galerinin gerçek şubelerini açın, sonra demo
     şubeleri ("Kadıköy Şubesi", "Ataşehir Şubesi") **kalıcı olarak kapatın**. Başka aktif
     şube yokken hiçbir şube kalıcı kapatılamaz (B5): önce gerçek şubeyi açın. Demo
     şubenin adresini düzenleyip gerçek şube olarak kullanmak da mümkündür; adres
     kısaltması (`kadikoy-subesi`) kapatılan şubede de kullanılmış sayılır.
   - Bu adım atlanırsa ana sayfanın altlığında başkasının adresi ve telefonu görünür.
2. **İlk Yönetici girişi.** `https://<alan-adı>/admin` → `Identity__SeedAdmin__LoginId`
   ve parola → parola değişikliği zorunlu → ikinci adım kurulumu zorunlu.
3. **Duman testi:**

   | İstek | Beklenen |
   |---|---|
   | `GET /tr/` | `200` |
   | `GET /ar/` | `200`, `<html lang="ar" dir="rtl">` |
   | `GET /seed-media/cover-1.svg` | `404` — geliştirme yüzeyi kapalı |
   | Tarayıcıdan panel girişi (`https`) | Parola değiştirme ekranı |

## 6. Yedek

- **Veritabanı:** düzenli `pg_dump`; geri yükleme en az bir kez denenmiş olmalı.
- **Nesne deposu:** orijinal fotoğraflar `private/` önekinde ham saklanır (`docs/architecture.md`
  § Karar Defteri); türevler onlardan yeniden üretilebilir, orijinaller üretilemez.
- **`Identity__TwoFactor__EncryptionKey`:** veritabanı yedeğiyle **ayrı** yerde. Anahtarsız
  yedek, ikinci adımı kurulmuş her hesabı kilitli geri getirir.
- **`Notifications__Newsletter__UnsubscribeSigningKey`:** aynı biçimde ayrı yerde. Yeni bir
  sırla geri yüklenen kurulumda gönderilmiş bütün bültenlerin çıkış bağlantısı çalışmaz.

## 7. Doğrulandı / doğrulanmadı

2026-11-18'de imaj yerelde canlıya benzer koşuldan geçirildi: boş veritabanı,
geliştirme bayrakları kapalı, üretim parola maliyeti, önde TLS'i sonlandıran Caddy.

**Doğrulandı:** kurulum adımı 0 ile çıkıyor · kurulum adımı atlanınca uygulama 2 sn'de
134 ile çıkıyor · `/tr/`, `/ar/`, `/en/`, `/tr/cars` 200 · `/seed-media` 404 · brotli
sıkıştırması açık · imajda tzdata ve ICU var · `https` üzerinden tarayıcıyla giriş ve
zorunlu parola değişikliği çalışıyor, oturum çerezi `Secure` + `SameSite=Strict`.

**Doğrulanmadı:** gerçek bir barındırma sağlayıcısı · gerçek SMTP ile e-posta teslimi ·
gerçek R2/S3 ile fotoğraf yükleme · `linux/amd64` derlemesi (ölçüm `arm64` makinede) ·
şema başına ayrı veritabanı rolü (`docs/architecture.md` § Karar Defteri; bugün iki adım
aynı bağlantı dizesiyle çalışıyor) · yük altında davranış.
