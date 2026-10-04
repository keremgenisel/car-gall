# 0004 — Bildirim gönderimi: veritabanı kuyruğu ve port arkasında taşıyıcı

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-04
- **Bağlam:** spec 0009 — Talep bildirimi (D4, D6)

## Bağlam

0009, sistemin ilk dışarıya çıkan yan etkisini getiriyor. O güne kadar her şey
uygulamanın kendi veritabanında kalıyordu: bir işlem başarısız olursa geri alınırdı ve
geri alınmayan hiçbir şey yoktu. Gönderilmiş bir e-posta geri alınamaz.

Bu iki ayrı karar gerektirdi ve ikisi birbirine bağlı:

1. Gönderim **nerede** yaşar — talep işleminin içinde mi, ayrı bir kuyrukta mı?
2. Sağlayıcıya **nasıl** bağlanılır — doğrudan mı, bir soyutlamanın arkasından mı?

## Karar

**Mesajlar veritabanında bir kuyrukta durur; gönderimi bir arka plan servisi yapar.
Gönderici `INotificationTransport` portunun arkasındadır ve V1'de MailKit ile SMTP
konuşur.**

Sıra kesindir: mesaj **önce** kaydedilir, **sonra** gönderilir, **sonra** sonucu
işaretlenir.

## Gerekçe

### Neden kuyruk — neden talep işleminin içinde değil

Talep işlemi ziyaretçinin beklediği istektir. Sağlayıcı yavaşsa veya erişilemezse:

- **İşlemin içinde gönderirsek** ziyaretçi sağlayıcıyı bekler. Sağlayıcı düştüğünde
  talep de düşer — galeri, hakkında hiç haberi olmadığı bir müşteriyi kaybeder. Bu, bu
  spec'in önlemek için yazıldığı kaybın tam olarak kendisidir.
- **İşlemden sonra "ateşle ve unut" yaparsak** süreç o anda düşerse mesaj buharlaşır ve
  kaybı gösteren hiçbir kayıt kalmaz.

Tekrar deneme bir kuyruk olmadan karşılanamaz: beşinci denemeyi yapacak olan, olayı
teslim eden dağıtıcı değildir. Kuyruğun **veritabanında** olmasının nedeni de bu —
bellekteki bir kuyruğu yeniden başlatma sessizce siler.

### Neden gönderim kaydın ARDINDAN

Üç sıralamadan ikisi yanlış:

| Sıra | Süreç ortada düşerse |
|---|---|
| Gönder → kaydet | Mesaj gitti, kaydı yok. Galeri aynı mesajı tekrar gönderir ve bunu bilemez. |
| Kaydet → gönder → işaretle | Mesaj gitmiş olabilir, kaydı `Queued` kalır. **Tekrar gönderilir.** |
| Tek transaction içinde ikisi | Mümkün değil: SMTP transaction'a katılmaz. |

İkincisi seçildi. **Tam-bir-kez teslim vaat EDİLMİYOR** — en-az-bir-kez teslim ile geri
alınamaz bir dış yan etki birlikteyken bu imkânsızdır. Vaat edilen şey, aynı olayın iki
kez işlenmesinde ikinci bir **kayıt** oluşmamasıdır. Bedel bilinçli: nadir bir çökme
penceresinde ziyaretçi aynı bilgilendirmeyi iki kez alabilir. Alternatifi, mesajın hiç
gitmemesi ve kimsenin fark etmemesiydi.

### Neden port arkasında taşıyıcı

Galeriler büyüdüğünde SMTP yerine bir sağlayıcı servisine (SendGrid, SES, Postmark)
geçilmesi beklenen bir adım. Port, o geçişin **tek bir sınıfı** değiştirmesini sağlıyor:
kuyruk, tekrar deneme, şablon çözümü ve geçmiş hiç dokunulmadan kalır.

Portun ikinci işi daha ince: **hatanın sınıfını taşıyıcı söyler.** Sağlayıcının `4xx`'i
geçici, `5xx`'i kalıcıdır ve bu ayrımı yalnız SMTP cevabını gören taraf bilebilir. Üst
katman tahmin etmeye çalışsaydı sağlayıcıya özgü bilgi üst katmana sızardı ve sağlayıcı
değişiminde sessizce yanlışlaşırdı.

**Zaman aşımını taşıyıcı değil çağıran koyar.** MailKit'in kendi `Timeout`'u soket
seviyesindedir; protokol seviyesinde asılı kalan bir oturumu kesmez. Sağlayıcı bağlantıyı
açık tutup hiç cevap vermezse kuyruk tek bir mesajda süresiz asılı kalırdı.

### Neden gerçek bir SMTP sunucusuna karşı tek bir test

Testlerin ezici çoğunluğu sahte taşıyıcıyla koşar — ölçülen şey kuyruğun davranışıdır,
sağlayıcının değil. Ama sahte taşıyıcı **taşıyıcının kendisini** kanıtlayamaz: yanlış
kurulan bir `MimeMessage`, eksik bir `From` başlığı veya hiç çağrılmayan bir `SendAsync`
sahte taşıyıcıyla sonsuza kadar yeşil kalır.

Bu yüzden Testcontainers ile **gerçek bir SMTP sunucusuna** (Mailpit) karşı koşan tek bir
entegrasyon testi var. Olaydan başlar, kuyruğu atlamaz ve mesajın teslim edildiğini
sunucunun kendi kaydından okur. Sonda doğruladı: `client.SendAsync` çıkarıldığında bu test
kırmızı, sahte taşıyıcıyla koşan karşılığı yeşil kaldı — aradaki fark testin varlık nedeni.

## Bedel

- **Gecikme.** Bildirim artık talepten sonra, en çok bir tur kadar gecikmeyle gider.
  Kabul edildi: bu, galeri için insan ölçeğinde bir gecikme.
- **Backend'in ilk üçüncü parti çalışma zamanı bağımlılığı** (MailKit). Port arkasında
  olduğu için kapsamı tek bir sınıf.
- **Üç dağıtım yolu da bir SMTP sunucusu istiyor.** `scripts/dev-up.sh` ve iki CI işi
  Mailpit kaldırıyor; bu olmadan kuyruk her turda geçici hatayla döner ve "e-posta gitti"
  iddiası hiçbir yerden doğrulanamazdı.
- **Kimlik doğrulaması olmayan relay desteklenmiyor.** Sağlayıcı kimlik bilgileri eksikken
  uygulama ayağa kalkmıyor (AC-28). Bedel bilinçli: üretimde kimliksiz relay, galerinin
  adresinden herkesin posta atabilmesi demek.

## Alternatifler

- **Mesaj kuyruğu altyapısı (RabbitMQ, SQS).** Elendi: V1'de broker yok ve outbox için
  verilen karar (ADR 0001) burada da geçerli — tek süreç, tek veritabanı. Kuyruk
  veritabanında yaşarken broker'a geçiş yalnız arka plan servisini değiştirir.
- **Sağlayıcının kendi kuyruğuna bırakmak** (SendGrid'e at, gerisini o halletsin).
  Elendi: gönderim geçmişi galerinin kendi verisidir ve "neden ulaşılmadı" sorusunun
  cevabı bir dış servisin arayüzünde aranmamalı. Ayrıca bu, sağlayıcı değişimini veri
  taşıma işine çevirirdi.
- **Gönderimi outbox dağıtıcısının içine koymak.** Elendi: dağıtıcı olayı teslim eder ve
  biter; tekrar deneme politikası bildirimin kendi iş kuralıdır (deneme sayısı, artan
  aralık, hata sınıfı) ve onu outbox'a taşımak her modülün outbox'ına taşımak olurdu.
