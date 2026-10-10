# 0009 — Medya işleme hattı: nerede, neyle, neyi saklayarak

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-10
- **Bağlam:** `specs/done/0011-media-yukleme-turev-filigran.md` — D1, D2, D4, D7, D8, D12, D16
  (Takım Yöneticisi onayı 2026-10-10)

## Bağlam

`Media` modülünün tamamı bir dosyaydı: tek bir olay sözleşmesi, uygulama projesi yok,
`DbContext` yok, migration yok. Dinleyen vardı (`VehicleCatalog`'un kapak görseli alıcısı),
konuşan yoktu. Galerinin elindeki fotoğraf sisteme hiçbir biçimde girmiyordu ve bunun
görünür sonucu şuydu: `Vehicle.EnsurePublishable()` en az bir medya varlığı istediği için
**hiçbir araç yayına alınamıyordu.**

Spec'in taslağı beş açık soruyla geldi. Dördü doğrudan bu ADR'nin konusu; beşincisi
(`alt` metnini kim yazar) arayüz kapsamı kararıydı ve kodda bir iz bırakmadı.

## Kararlar

### K1 (D1) — Türevler ve filigran **sunucuda** üretilir

Mockup *"her fotoğraf tarayıcıda 1600 / 800 / 400 piksel WebP olarak küçültülür"* diyordu.

*Gerekçe:* filigranı istemcide uygulamak onu **atlanabilir** kılar. *"R2'ye yalnızca bu
sürümler yazılır"* bir garantidir ve garanti istemciye bırakılamaz; galeri sahibinin satın
aldığı şey tam olarak o garanti. İkinci sebep: türev boyutları politika değişikliğiyle
değişir ve o gün tüm istemcilerin güncel olması beklenemez.

*Bedel:* sunucu CPU'su ve asenkron bir işleme hattı. Yükleme anında görsel hazır olmaz —
işleme durumu (`Yüklendi · İşleniyor · Hazır · Başarısız`) bu yüzden zorunlu, bir süsleme
değil.

### K2 (D2 + D16) — Orijinal **ham** saklanır, `private/` önekinde durur

Mockup *"yalnızca bu sürümler yazılır"*, `docs/architecture.md` § 1 *"orijinal + türevler"*
diyordu. Çelişkide `docs/` kazandı (`AGENTS.md` açılış kuralı).

*Gerekçe:* filigran politikası, boyut kümesi veya kalite ayarı değişirse türevler yeniden
üretilebilir olmalı. Orijinal atılırsa o kapı **kalıcı olarak** kapanır.

Ama orijinali tutmak R5 ile (EXIF temizliği) bir gerilim üretiyor: ham bayt aracın durduğu
adresin koordinatını taşır. Çözüm iki önek: `public/assets/…` CDN'e bağlanır,
`private/originals/…` bağlanmaz ve API orijinalin yolunu hiçbir yanıtta döndürmez. Yol
üretimi bunu **kod seviyesinde** reddediyor (`MediaUrlResolver`), bir konvansiyon olarak
değil.

*Bedel — açıkça kabul edildi:* konum verisi depoda yaşar ve bir veri silme talebi
orijinalleri de kapsamak zorundadır. Bu, spec'in PR'ında açık konu olarak kaydedildi.

*Elenen alternatif:* orijinali de yönlendirmesi uygulanmış ve metadata'sı atılmış hâlde
saklamak. Elendi: K1'in açtığı "yeniden üretilebilir" kapısını yarı kapatır — kalite ayarı
değiştiğinde yeniden üretim artık kayıplı bir kaynaktan olur.

### K3 (D7) — Görüntü işleme bir portun arkasında; kütüphane **lisansı ölçülerek** seçildi

`IImageProcessor` portu `Application`'da, uygulaması `Infrastructure`'da. Seçilen kütüphane
**SkiaSharp (MIT)**.

*Gerekçe:* en yaygın seçenek ImageSharp, v3'ten beri "Six Labors Split License" taşıyor:
açık kaynakta ve cirosu eşiğin altındaki şirkette Apache-2.0, **diğer her kullanımda ticari
lisans satın alınır**. Bu ürün white-label, galeriye satılan ticari bir kurulum — yani
"diğer". Lisans bedelini işleme hattının içine gömmek, ürün satıldığı gün doğan bir
yükümlülüktür ve o gün kütüphaneyi değiştirmek bütün hattı yeniden yazmak demektir.

*Ölçüldü (2026-10-10, NuGet lisans alanları):* SkiaSharp **MIT**,
SkiaSharp.NativeAssets.Linux **MIT**, Magick.NET-Q8-AnyCPU **Apache-2.0** (yedek),
AWSSDK.S3 **Apache-2.0**, MetadataExtractor **Apache-2.0** (yalnız test).
SixLabors.ImageSharp'ın lisans alanı bir SPDX ifadesi **değil**, özel bir metin dosyası —
kaygının kendisini doğruluyor.

*Bedel:* port bir dolaylılık katmanıdır ve bugün tek uygulaması var.

### K4 (D8) — EXIF yönlendirmesi **piksele uygulanır**, sonra metadata atılır

Hat sırası: oku → yönlendirmeyi piksele uygula → ölçekle → filigranı bas → metadata'sız
WebP olarak encode et.

*Gerekçe ve ölçüm:* `SKBitmap.Decode` EXIF yönlendirmesini **uygulamıyor** — 600×900 dikey
bir fotoğraf 600×900 olarak çözülüyor ve `Orientation` etiketi atıldığında görsel yan
yatmış kalıyor. Yani yalnız *"EXIF'i temizle"* yazan bir hat R5'i karşılar ve
**kullanılamaz** çıktı üretir. Tuzak listesi EXIF'i silmeyi söylüyordu; silmenin kendisi bu
ikinci tuzağı üretiyor.

*Sonuç:* kriter ikiye bölündü. AC-19 metadata'nın yokluğunu, AC-20 yönün doğruluğunu
ölçüyor — çünkü *"EXIF yok"* testi yan yatmış bir görselde de yeşil verir.

*Ölçüldü:* WebP encode çıktısı metadata taşımıyor; **bağımsız** bir okuyucu
(MetadataExtractor) üç türevin hiçbirinde EXIF veya GPS bulamıyor. R5 bu yüzden ayrı bir
"strip" adımı gerektirmiyor — gereken şey onun testle kanıtlanması, ve testin üreten
kütüphaneden **başka** bir okuyucuyla yapılması.

### K5 (D4) — Filigran kaynağı yapılandırmadaki bir **nesne anahtarıdır**

Spec'in taslağı *"yapılandırmadan bir medya varlığı kimliği"* öneriyordu. **Elendi:**
çözülemeyen bir kurulum döngüsü üretiyor — yapılandırma zorunluysa (C6) uygulama ayağa
kalkmaz, kalkmazsa logo yüklenemez, yüklenmezse yapılandırılacak kimlik yoktur.

Kaynak artık `Media`'nın **kendi** kovasındaki bir nesne anahtarı
(`Media:WatermarkObjectKey`). Operatör onu kurulumda kovaya koyar; `Media` `Dealership`'i
çağırmaz.

*Bedel:* logo iki yerde durur — filigran nesnesi ve vitrinde gösterilecek medya varlığı
(`Gallery.LogoMediaAssetId`, `0003`). Logo değiştiğinde yapılandırma güncellenir ve **eski
türevler eski filigranı taşımaya devam eder**; yeniden üretim ayrı bir iştir.

*Elenen alternatif:* kimliği önceden üretip logoyu o kimlikle yüklemek ve "kimliği
yapılandırmadakine eşit olan varlık kendi filigranını almaz" kuralını eklemek. İşler, ama
kimlik atamasını istemciye açar.

### K6 (D12) — Depolama S3 uyumlu; **test ikizi MinIO değil**

Üretim kararı değişmedi: `docs/architecture.md` § 7 "Medya | S3-uyumlu (R2/MinIO) + CDN,
ilk günden" diyor ve öyle kalıyor. Değişen, geliştirme ve test ortamındaki ikiz.

*Ölçüldü (2026-10-10):* `Testcontainers.Minio` 4.15.0 paketi **var** (MIT) ama imajı anonim
olarak **çekilemiyor** — `pull access denied for minio/minio, repository does not exist`.
Bu bir Docker Hub erişim sorunu değil: aynı makinede `alpine:3.20` sorunsuz indi ve
`quay.io/minio/minio` 401 döndü. Çekilemeyen bir imaja bağlı test, CI'da yazı turadır.

*Seçilen:* LocalStack'in S3'ü (`localstack/localstack:3.8.1`, `Testcontainers.LocalStack`
4.16.0, MIT). Gidiş-dönüşü ölçüldü: kova oluşturma, PUT, GET (bayt ve `Content-Type`
birebir), olmayan nesnede 404.

*Bedel:* `Media.Tests`'in entegrasyon testleri **iki** konteyner açıyor (PostgreSQL +
LocalStack) ve LocalStack imajı büyük — CI süresi artıyor. Sahte bir depo ile sınamak
ucuzdu ama `docs/testing.md` § 1'in "SQLite in-memory ile EF testi yapılmaz" ilkesi
burada da geçerli: yeşil test yanlış güven verir.

## Elenen alternatifler

**Hattı `BuildingBlocks`'a koymak.** Elendi: türev kümesi, filigran oranı ve tekrar deneme
politikası `Media`'nın **iş kuralıdır** (Yasak Listesi 11). Outbox dağıtıcısı jeneriktir
çünkü her modülün aynı outbox'ı var; medya hattının eşi yok. `Notifications` kuyruğu aynı
gerekçeyle kendi modülünde duruyor.

**Yükleme ucunda senkron işleme.** Elendi ve spec'in tuzak listesi bunu zaten yazıyordu:
büyük dosyada zaman aşımı üretir. İşleme durumunun varlık nedeni bu.

**`MediaAssetReady`'nin şeklini büyütmek.** Elendi: vitrin bugün tek bir `cover_media_url`
kolonu tutuyor ve `srcset` `0016`'nın işi. Hiçbir şeyin okumadığı bir alan eklemek
spekülasyondur; olaya alan eklemek sonradan da uyumludur. Boyut kümesine ihtiyaç duyan
tüketici `IMediaAssetQueries`'ten çözer — `docs/architecture.md` § 2.A'nın `VehicleCatalog
→ Media` örneği tam olarak budur.

**Türev başına ayrı bir varyant tablosu yerine tek bir `jsonb` zarfı.** Elendi: türevin
GERÇEKLEŞEN genişlik ve yüksekliği sorgulanabilir olmalı (AC-14 büyütme yapılmadığını
iddia ediyor) ve bir `jsonb` içindeki sayıyı doğrulamak `integer` kolonu kadar kesin değil.

## Sonuç

- `src/Modules/Media/Media/` — modülün ilk uygulama projesi: `Domain`, `Application`,
  `Infrastructure`, `Endpoints`, `MediaModule.cs`.
- `media` şeması ve ilk migration: `assets`, `asset_derivatives`, `outbox_messages`.
  **Inbox yok** — `Media` konuşur, dinlemez (bir mimari test bunu koruyor).
- `Media.Contracts`: `MediaAssetFailed` eklendi (olay akışında yazılıydı, kodda yoktu);
  `MediaAssetReady`'nin şekli korundu; `MediaSetup` ve `IMediaAssetQueries` eklendi.
- `docs/domain.md`: sözlüğe `İşleme Durumu`, § 2.10'a Medya iş kuralları (M1–M3).
- `docs/architecture.md` § Karar Defteri: üç satır (medya işleme, filigran kaynağı,
  orijinal saklama).
- `tests/Architecture.Tests/MedyaSinirTests.cs`: dört iddia, üçü **sondayla kırmızıya
  döndürülerek** doğrulandı. Dördüncüsü (`Media` olay tüketmez) şemanın inbox taşımamasıyla
  birbirini doğruluyor.
- `tests/Architecture.Tests/SolutionLayout.cs`: şema sızıntısı deseni buraya taşındı.
  **Gerekçe bir sondadan geldi:** `0011` için ikinci bir kopya yazdım ve daha zayıf çıktı —
  `"SELECT * FROM dealership.branches"` satırını kaçırıyordu. İki kopyanın ayrışması tam
  olarak böyle görünüyor.
- `0011` `VehicleCatalog`'a **dokunmadı**: olay yayılıyor, alıcısı `0012`'de kablolanacak.
  Bugünkü alıcı olaya bağlanamaz hâlde (imzası olayda bulunmayan bir `vehicleId` istiyor).
- **Açık kalan:** orijinallerin veri silme kapsamına alınması (K2'nin bedeli). `specs/`
  altında bir veri silme akışı henüz yok.
