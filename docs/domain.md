# Domain — Ortak Dil ve İş Kuralları

Bu dosya **ortak dili** (ubiquitous language) tanımlar. Koddaki tipler, tablolar,
endpoint'ler, spec'ler, commit mesajları ve arayüz metinleri **burada tanımlı terimleri**
kullanır. Aynı kavram için iki isim kullanılmaz.

> **Not — "sipariş / kupon / iade" hakkında.** Bu projede sipariş, kupon ve iade kavramı
> **yoktur**: online ödeme ve sepet akışı bilinçli olarak kapsam dışıdır
> (`docs/architecture.md` § 6). En yakın karşılıklar ve yerleri:
> sipariş → **Rezervasyon**, kupon → **Kampanya**, iade → **Rezervasyon İptali**.
> Bunların iş kuralları aşağıda tanımlıdır. Gerçekten bir e-ticaret akışı gerekiyorsa
> bu ayrı bir karardır ve mimariyi değiştirir.

---

## 1. Sözlük

### Organizasyon

| Terim | Kod karşılığı | Tanım |
|---|---|---|
| **Galeri** | `Dealership` | Kurulumun ait olduğu tek işletme. Her kurulum tam bir galeriye hizmet eder. |
| **Şube** | `Branch` | Galerinin fiziksel lokasyonu. Her aracın tam olarak bir şubesi vardır. |
| **Personel** | `Staff` | Galeride çalışan kullanıcı. Rolü vardır, opsiyonel olarak bir şubeye bağlıdır. |
| **Danışman** | `SalesAdvisor` | Talepleri yürüten personel rolü. |
| **Takım Yöneticisi** | — | Karar mercii. Kod içinde bir rol değildir; süreç rolüdür (`AGENTS.md` Altın Kural 2). |

### Araç ve vitrin

| Terim | Kod karşılığı | Tanım |
|---|---|---|
| **Araç** | `Vehicle` | Galerinin elindeki fiziksel araç. Şase numarasıyla tekildir. |
| **İlan** | `Listing` | Bir aracın vitrindeki yayın hâli. Araç ≠ ilan: araç fiziksel, ilan yayınsal gerçektir. |
| **Vitrin** | — | Sitenin ziyaretçiye açık araç listesi ve detay sayfaları. |
| **Yayın Durumu** | `ListingStatus` | `Taslak` · `Yayında` · `Rezerve` · `Satıldı` · `Arşiv` |
| **Referans Veri** | `VehicleReference` | Marka / model / kasa / donanım sözlüğü. Galeri yazmaz, sistem besler. |
| **Kanonik Ad** | `CanonicalName` | Referans verinin Latin harfli, dilden bağımsız adı. Slug bundan üretilir. |
| **Ekspertiz** | `VehicleCondition` | Aracın hasar/boya/bakım durumu. Alanları yapılandırılabilir. |
| **Tramer** | `DamageRecordAmount` | Hasar kayıt tutarı. Opsiyonel alan; kapatılabilir. |
| **Parça Haritası** | `PaintChangeMap` | 13 parçanın orijinal / boyalı / değişen durumu. Opsiyonel alan. |
| **Alan Ayarı** | `VehicleFormSettings` | Hangi opsiyonel alanın açık/zorunlu ve public olduğunu belirleyen tekil ayar. |
| **Medya Varlığı** | `MediaAsset` | Yüklenmiş ve işlenmiş görsel/video. Sahipsizdir; araca `VehicleCatalog` bağlar. |
| **İşleme Durumu** | `MediaProcessingStatus` | Bir medya varlığının hat üzerindeki hâli: `Yüklendi` · `İşleniyor` · `Hazır` · `Başarısız`. Kod saklanır, uçta çevrilir. |
| **Kapak Görseli** | `CoverAsset` | İlanın listede görünen ilk görseli. Sıralı medya listesinin ilk elemanı. |
| **Donanım Paketi** | `TrimLevel` | Bir modelin donanım seviyesi. Referans veridir; `VehicleReference` besler. |
| **Yakıt** | `FuelType` | Aracın yakıt türü. Referans veridir; kapalı kümedir ve locale başına çevrilir. |
| **Vites** | `TransmissionType` | Şanzıman türü. Referans veridir; kapalı kümedir. |
| **Çekiş** | `DrivetrainType` | Aracın çekiş düzeni. Referans veridir; kapalı kümedir. |
| **Renk** | `Color` | Aracın rengi. Taksonomi DEĞİLDİR ve galeriye göre değişir; `VehicleCatalog`'un kendi değeridir. |

### Talep tarafı

| Terim | Kod karşılığı | Tanım |
|---|---|---|
| **Talep** | `Lead` | Ziyaretçinin bıraktığı her ilgi kaydı. Sistemin ikinci çekirdek kavramı. |
| **Talep Tipi** | `LeadType` | `BilgiTalebi` · `GeriArama` · `TestSürüşü` · `Takas` · `KrediGörüşmesi` · `FiyatTeklifi` |
| **Talep Durumu** | `LeadStatus` | `Yeni` · `Atandı` · `İletişimde` · `Sonuçlandı` |
| **Sonuç** | `LeadOutcome` | `Satıldı` · `Kaybedildi` (+ kayıp nedeni) |
| **Araç Anlık Görüntüsü** | `VehicleSnapshot` | Talep anındaki model adı ve fiyatının kopyası. Sonradan değişmez. |
| **Takas** | `TradeIn` | Müşterinin kendi aracını mahsup etmek istemesi. Bir talep tipidir. |
| **Rezervasyon** | `Reservation` | Bir aracın belirli bir müşteri için vitrinde tutulması. |
| **Kampanya** | `Campaign` | Ana sayfada veya ilanda gösterilen tanıtım/indirim duyurusu. |

### Bildirim

| Terim | Kod karşılığı | Tanım |
|---|---|---|
| **Bildirim Şablonu** | `NotificationTemplate` | (anahtar, locale) çifti başına konu + gövde. Çeviri verisidir; kodda metin durmaz. |
| **Gönderim Kaydı** | `NotificationMessage` | Gönderilecek tek mesaj. Kuyruk satırı VE geçmiş kaydı aynı şeydir; iki tablo tutulmaz. |
| **Gönderim Durumu** | `NotificationStatus` | `Kuyrukta` · `Gönderildi` · `Başarısız` · `KanalYok` |
| **Kanal Yok** | `NoChannel` | Alıcının o kanaldan adresi olmadığı için gönderim HİÇ DENENMEDİ. Bir hata değildir. |
| **Geçici Hata** | `Transient` | Yeniden denemenin düzeltebileceği hata: ağ, zaman aşımı, sağlayıcının `4xx`'i. |
| **Kalıcı Hata** | `Permanent` | Yeniden denemenin düzeltemeyeceği hata: adres reddedildi, şablon yok, sağlayıcının `5xx`'i. |
| **Taşıyıcı** | `INotificationTransport` | Sağlayıcıyla konuşan port. Tekrar denemez; hatanın SINIFINI söyler. |
| **Kurulum Bildirim Adresi** | `SetupNotificationAddress` | Şubenin e-postası yokken galeri bildiriminin gittiği adres. Yapılandırmadan gelir. |

### Teknik ortak terimler

| Terim | Kod karşılığı | Tanım |
|---|---|---|
| **Locale** | `Locale` | Dil + yön + biçimlendirme birimi. `tr`, `ar`, `en`. |
| **Para** | `Money` | Tutar + ISO 4217 kodu + minor unit üssü. Çıplak `decimal` taşınmaz. |
| **Şube Saat Dilimi** | `Branch.TimeZone` | "Açık mı?" hesabının yapıldığı saat dilimi. |

---

## 2. İş Kuralları

Her kural test edilebilir bir ifadedir. Spec'ler bu kurallara atıf yapar.

### 2.1 Araç ve İlan

**A1.** Bir aracın her zaman tam olarak bir **şubesi** vardır. Şubesiz araç durumu yoktur.

**A2.** Bir araç **yayına alınabilmesi** için şu koşulların hepsini sağlar:
- çekirdek alanları dolu (marka, model, yıl, kasa, kilometre, yakıt, vites, renk, şube),
- fiyatı belirlenmiş **veya** açıkça "fiyat sorunuz" işaretli,
- en az **bir** medya varlığı **hazır** durumda,
- aktif alan ayarlarında **zorunlu** işaretli tüm opsiyonel alanları dolu.

**A3.** Yayın durumu geçişleri — başka geçiş yoktur:
```
Taslak   → Yayında | Arşiv
Yayında  → Rezerve | Satıldı | Taslak | Arşiv
Rezerve  → Yayında | Satıldı
Satıldı  → Arşiv
Arşiv    → (terminal)
```

`Taslak → Arşiv` **2026-10-11'de eklendi** (spec `0012b` / D10, Takım Yöneticisi onayı).
Gerekçe ölçülerek bulundu: silme bu projede `Arşiv` durumudur (`0010b` § X19) ve şase
tekilliği `Arşiv` kayıtlarını muaf tutuyor (**A5**). İkisi birlikte, yanlış şaseyle açılmış
ve **hiç yayınlanmamış** bir taslağı ne silinebilir ne arşivlenebilir bırakıyordu; o şase
galeri genelinde kalıcı olarak tutuluydu. A5'in muafiyeti yanlış kaydın emekliye
ayrılabilmesi için vardır ve çıkışı olmayan bir muafiyet kâğıt üstünde kalır.

**A4.** `Satıldı` bir **terminal ticari durumdur**: araç yeniden yayına alınamaz.
Aynı fiziksel araç geri alınırsa **yeni bir araç kaydı** açılır; eskisi arşivde kalır.
(Gerekçe: satış geçmişi ve fiyat geçmişi bozulmaz.)

**A5.** Şase numarası galeri genelinde **tekildir**, ancak `Arşiv` durumundaki kayıtlar bu
tekillik kontrolünden muaftır (A4'ün sonucu).

**A6.** Fiyat değişikliği **fiyat geçmişine** yazılır. Fiyat geçmişi silinemez ve
düzeltilemez; yanlış giriş yeni bir kayıtla düzeltilir.

**A7.** Aracın şubesi değiştiğinde `VehicleRelocated` olayı yayılır. Transfer tek adımlıdır;
onay akışı yoktur.

**A8.** Şube kapatıldığında (`BranchDeactivated`) o şubedeki araçlar **yayından kaldırılır**
ve "lokasyon ataması bekliyor" durumuna geçer. Sessizce vitrinde kalmazlar.

### 2.2 Alan Ayarları

**F1.** Bir alanın kapatılması **veriyi silmez**. Alan geri açıldığında eski değerler görünür.

**F2.** Bir alan "zorunlu" yapıldığında, o alanı boş olan **mevcut** araçlar geçersiz sayılmaz
ve düzenlemeye kapanmaz. "Eksik bilgi" olarak işaretlenirler. Zorunluluk yalnızca o andan
sonraki kayıt ve düzenlemelere uygulanır.

**F3.** `Gizli` işaretli bir alan public API yanıtında **hiç bulunmaz** (null olarak da gelmez).

**F4.** Çekirdek alanlar kapatılamaz. Şase, plaka ve iç notlar her zaman gizlidir.

**F5.** Alan listesi koddadır. Kullanıcı yeni alan tanımlayamaz.

### 2.3 Rezervasyon *(sipariş kavramının bu projedeki karşılığı)*

**R1.** Rezervasyon yalnızca `Yayında` durumundaki bir araca yapılır.

**R2.** Bir aracın aynı anda **en fazla bir** aktif rezervasyonu olabilir.

**R3.** Rezervasyonun bir **bitiş tarihi** vardır. Tarih geçtiğinde araç otomatik olarak
`Yayında` durumuna döner ve ilgili danışmana bildirim gider.

**R4.** Rezervasyon **ödeme içermez**. Kapora alınıyorsa sistem dışında alınır; sistem
yalnızca "rezerve" bilgisini taşır. *(Online ödeme kapsam dışıdır.)*

**R5.** Rezervasyon iptali *(iade kavramının karşılığı)* aracı `Yayında` durumuna döndürür,
iptal nedenini kaydeder ve ilgili talebi kapatmaz — talep `İletişimde` kalır.

**R6.** Rezerve araç vitrinde **görünmeye devam eder**, "rezerve" rozetiyle ve iletişim
formu kapalı olarak. (Gerekçe: rezervasyon düşerse ilgi kaybedilmez.)

### 2.4 Talep (Lead)

**L1.** Her talep oluşturulurken **`SubmittedLocale`** kaydedilir. Müşteriye giden bildirim
bu dille, danışmana giden bildirim danışmanın diliyle gider.

**L2.** Her talep oluşturulurken, ilgilenilen aracın **anlık görüntüsü** (model adı + fiyat)
kopyalanır. Araç sonradan değişse veya arşivlense bile talep kendi gerçeğini korur.

**L3.** Talep, ilgilenilen aracın **şubesine** bağlanır ve o şubenin danışmanlarına yönlendirilir.
Araçsız talep (genel iletişim) galeri geneline düşer.

**L4.** Talep durumu geçişleri:
```
Yeni → Atandı → İletişimde → Sonuçlandı
```
Geri dönüş yalnızca `Sonuçlandı → İletişimde` yönünde ve nedenle mümkündür.

**L5.** Bir personel pasifleştirildiğinde (`StaffDeactivated`) açık talepleri
**otomatik olarak** yeniden atanır. Sahipsiz açık talep bırakılmaz.

**L6.** Talep kaydı **silinmez**. KVKK silme talebinde kişisel alanlar anonimleştirilir,
kayıt ve istatistik korunur.

**L7.** İletişim izni (`ConsentGiven`) talep anında kaydedilir. İzin yoksa pazarlama
bildirimi gönderilmez; yalnızca talebin kendisine dair işlemsel bildirim gönderilir.

### 2.5 Personel ve Kimlik

**S1.** Galeride her zaman **en az bir aktif Yönetici** bulunur. Son aktif Yönetici
pasifleştirilemez ve rolü düşürülemez. *(**B5**'in kimlik tarafındaki karşılığıdır: son
aktif şube kapatılamazsa, son aktif Yönetici de düşürülemez — aksi hâlde galeri
yönetilemez hâle gelir.)*
**"Aktif" durumu aktif olmak demektir**; geçici kilitlenme aktifliği etkilemez. Bir
Yönetici kendi hesabını pasifleştirebilir ve kendi rolünü düşürebilir — son aktif
Yönetici olmadığı sürece.

**S2.** Giriş kimliği **kültüre duyarsız** biçimde küçük harfe normalize edilerek
saklanır; tekillik bu normalize değer üzerinden kurulur. *(Türkçe collation altında
`lower()` `I → ı` üretir; bedeli bir liste hatası değil, iki kişinin aynı hesaba
düşmesidir — Yasak Listesi 27.)*

**S3.** Başarısız her giriş denemesi **ayırt edilemez** bir karşılık üretir: kimliğin
kayıtlı olup olmadığı, parolanın yanlış olduğu, hesabın pasif olduğu veya hesabın
kilitli olduğu bilgisi sızmaz. Ayırt edilemezlik **tüm yanıt yüzeyinde** geçerlidir —
kilitlenme kanalı dâhil.

**S4.** Hesabın durumu ve rolü **her istekte kaynaktan** doğrulanır. Pasifleştirme, rol
değişikliği ve parola değişikliği bir sonraki istekte etkilidir; oturumun kalan süresi
bunu geciktiremez.

**S5.** Parola değiştiğinde o personelin **tüm** oturumları geçersizleşir. Çıkış ise
yalnızca **o cihazın** oturumunu kapatır.

### 2.6 Takas

**T1.** Takas bir **talep tipidir**, ayrı bir varlık değildir. Müşterinin aracı
`VehicleReference` sözlüğünden seçilir; galerinin stoğuna girmez.

**T2.** Takas talebinde **değerleme yapılmaz**. Sistem fiyat önermez; danışman iletişime geçer.

**T3.** Takasa konu araç galeriye alınırsa, bu **ayrı bir araç kaydı** olarak elle açılır.
Talepten araç kaydına otomatik dönüşüm yoktur.

### 2.7 Kampanya *(kupon kavramının bu projedeki karşılığı)*

**K1.** Kampanya bir **duyurudur**, hesaplanan bir indirim değildir. Fiyatı değiştirmez.

**K2.** Kampanyanın başlangıç ve bitiş tarihi vardır; süresi dolan kampanya otomatik olarak
vitrinden düşer.

**K3.** Kampanya metni **locale başına** yayınlanır. Bir dilde yayınlanmamış kampanya o dilde
görünmez — boş kutu gösterilmez.

**K4.** Kampanya bir veya birden çok araca bağlanabilir. Bağlı araç yayından kalktığında
kampanya o araç için otomatik geçersizleşir.

**K5.** İndirim uygulanacaksa **fiyat düşürülür** (ve fiyat geçmişine yazılır, A6).
Sistemde "indirim kodu", "kupon" veya hesaplanan indirim mekanizması yoktur.

### 2.8 Şube ve Çalışma Saatleri

**B1.** Bir şubenin çalışma programı **gün başına 0..N zaman aralığı** taşır.
Tek açılış/kapanış varsayımı yapılmaz (çift mesai yaygındır).

**B2.** Kapalı gün = o gün için zaman aralığı tanımlanmamış gündür. Hafta sonunun hangi
günler olduğu **veriden** okunur, koddan değil.

**B3.** Tarih aralıklı istisna programı (Ramazan, bayram, geçici kapanış) haftalık programı
ezer.

**B4.** "Şu an açık mı?" her zaman **şubenin kendi saat diliminde** hesaplanır.

**B5.** Bir galerinin en az bir şubesi vardır. Son aktif şube kapatılamaz.

### 2.9 Para ve Fiyat

**P1.** Tüm para ve yüzde alanları `decimal`'dir. `double`/`float` kullanılmaz.

**P2.** Para `Money` ile taşınır: tutar + ISO 4217 kodu + minor unit üssü.
KWD/BHD/JOD/OMR **üç ondalıklıdır**; saklama `decimal(19,4)`.

**P3.** Para birimi kurulum başına sabittir ve panelden değiştirilemez.

**P4.** "Fiyat sorunuz" ayrı bir durumdur, `0` değildir. Fiyatı olmayan araç sıralamada
fiyat filtresine takılmaz.

---

### 2.10 Medya

**M1.** Bir medya varlığının **işleme durumu** vardır ve geçişler şunlardır — başka geçiş
yoktur:
```
Yüklendi   → İşleniyor
İşleniyor  → Hazır
İşleniyor  → Başarısız
Başarısız  → Yüklendi        (AÇIK bir tekrar deneme komutuyla; otomatik değil)
Hazır      → (terminal)
```

**`Başarısız` terminal DEĞİLDİR.** Tekrar deneme ayrı bir komutla tetiklenir ve **aynı
varlık kimliğini korur** — yeni bir kimlik, `VehicleCatalog`'un tuttuğu referansı öksüz
bırakır. Otomatik tekrar bilinçli olarak yok: `0009`'un kaçan hatası otomatik tekrarın
kuyruğu sessizce tıkamasıydı.

**M2.** **Medya sahipsizdir.** Bir varlığın hangi araca ait olduğu `Media`'da tutulmaz;
`VehicleCatalog`'un sıralı medya listesinde durur.

**M3.** Yalnız **`Hazır`** durumdaki varlık A2'nin yayın şartını karşılar. `İşleniyor` bir
varlık aracı yayına yetiştirmez.

---

## 3. Terim Tuzakları

Yanlış kullanım dosyayı ve kafa karışıklığını çoğaltır:

| Yanlış | Doğru | Neden |
|---|---|---|
| "ilan eklemek" (araç kaydı için) | **araç eklemek** | Araç fiziksel, ilan yayınsal gerçektir. |
| "kullanıcı" (ziyaretçi için) | **ziyaretçi** / **personel** | Sistemde kullanıcı = personeldir; public üyelik yoktur. |
| "bayi" (şube için) | **şube** | "Bayi" franchise çağrıştırır; öyle bir kavram yok. |
| "kiracı" / `TenantId` | **galeri** / `BranchId` | Kurulum = galeri. Kiracı kavramı yoktur. |
| "sipariş" | **rezervasyon** | Sipariş/ödeme akışı yoktur. |
| "kupon" / "indirim kodu" | **kampanya** | Hesaplanan indirim mekanizması yoktur. |
| "iade" | **rezervasyon iptali** | Para iadesi yoktur. |
| "müşteri hesabı" | **talep** | Kimlik değil, ilgi kaydı tutulur. |
