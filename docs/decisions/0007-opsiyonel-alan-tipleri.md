# 0007 — Opsiyonel alan tipleri: kayıt defteri kodda, değer tipli kolonlarda

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-10
- **Bağlam:** spec 0010b — Panel: Araç Düzenleme ve Liste (D17, D21, D22, C23)

## Bağlam

`docs/architecture.md` § 4 opsiyonel alanları çok tipli sayıyor: tramer **tutarı**, muayene
**tarihi**, ağır hasar **evet/hayır**, kapı **sayısı**. `0001` bu alanlar için tek bir depo
kovası kurmuştu: `LocalizedText` — locale başına serbest metin.

`0010b`'ye gelindiğinde kodda tanımlı **tek** opsiyonel alan vardı:
`damage_record_amount` — yani bir **para** alanı, metin kovasında.

`AGENTS.md` Altın Kural 4 tip güvenliğini, Yasak Listesi 26 parayı kayan noktayla ya da
yaklaşık biçimlerle tutmayı yasaklıyor. Karar defteri parayı `decimal(19,4)` diye
tanımlıyor. Metin kovası bu üçünü birden deliyordu ve delme **sessizdi**: tutar bir dizge
olarak giriyor, hiçbir test kırılmıyordu.

## Karar

**Opsiyonel alanlar tipli hâle gelir. Tip kümesi kapalıdır** (`metin`, `locale'li metin`,
`tam sayı`, `tutar`, `evet/hayır`, `tarih`, `seçenek`). **Alan → tip eşlemesi KODDA yaşar**
(`VehicleFieldDefinitions`), ayar tablosunda değil. **Değer tipli KOLONLARDA saklanır**;
tutar `numeric(19,4)` + ISO kodu + minor unit üssü üçlüsüyle.

## Gerekçe

### Tip neden kodda, `vehicle_field_settings`'te değil (D21)

**F5** / Yasak Listesi 40 alan listesini *"kodda tanımlı, sabit ve versiyonlu"* ilan ediyor;
kullanıcı yeni alan tanımlayamıyor. Tip o listenin bir parçasıdır.

Operatör ekseni ile kod ekseni arasındaki fark şurada görünür: galeri bir alanı **kapatıp
açabilir** (veri durur, F1) ama bir alanın **tipini değiştiremez** — değiştirseydi o alanda
saklanmış bütün değerler anlamsızlaşırdı ve bozulma sessiz olurdu.

*Elenen alternatif:* `value_type` kolonu. İleride bir ayar ekranının onu düzenlemesini
mümkün kılardı; ayar ekranının yetki kriterleri bu tehlikeyi görmek zorunda kalmazdı.

### Değer neden tipli kolonlarda (D22)

Vitrin okuma modeli fiyatı **zaten** `PriceAmount` + `PriceCurrencyCode` +
`PriceMinorUnitExponent` üçlüsüyle tutuyor. Yazma modelinde aynı deseni tekrarlamak yeni bir
icat değil, kurulu desenin sürdürülmesi.

*Elenen alternatif:* tek `value jsonb` tipli zarf. Bir kolon yeterdi ama parayı JSON
sayısına çevirirdi; `numeric` kadar kesin değil ve gözden geçirenin ilk soracağı şey olurdu.

*Bilerek ödenen bedel:* tablo seyrek ve geniş — bugün tanımlı tek opsiyonel alan var ve
sekiz taşıyıcı kolonun yedisi her satırda `null`.

### EF sınırı ve `0010a` ile farkı

`Money?` sahipli bir tipin içinde eşlenemiyor; `0010a`'da `ListingPrice` tam burada
düşmüştü ve oradaki çözüm **tek metin kolonu** olmuştu. Burada aynı yolu izlemek Altın
Kural 4'ü delerdi (C23 bunun için yazıldı), bu yüzden tutar üç skaler taşıyıcıya açıldı ve
`Money` onlardan türetiliyor.

`ListingPrice`'ın saklanışı bu spec'te **değiştirilmedi**: `0010a` AC-22 ondalık ölçeğin
korunduğunu kanıtlıyor ve değiştirmek kapsam dışı bir yeniden yazım olurdu.

### Eski verinin taşınamaması

Migration eski `value` kolonunu **düşürüyor**, yeniden adlandırmıyor. Planda "veri kaybı
yok" yazıyordu; tutmadı ve nedeni migration'ın içinde kayıtlı: kolonda duran tek şey
locale'li metin hâlindeki bir **tutar** (`"12.500 TL"`) ve metni güvenilir biçimde
`Money`'ye çeviren bir dönüşüm yok — `12.500` hangi ayracın binlik olduğunu söylemiyor.
Yeniden adlandırmak, tipi `tutar` olan bir alanda locale'li metin bırakırdı: sessiz ve
kalıcı bir tutarsızlık. Bu kolona yazan tek yol bugüne kadar geliştirme seed'iydi (ölçüldü:
iki çağrı yeri, biri test).

## Sonuç

- `VehicleFieldDefinitions` alan → tip defteri; defterde olmayan anahtara değer yazılamaz.
- `vehicle_optional_field_values` tipli kolonlar taşır; tutar `numeric(19,4)` + kod + üs.
- `OptionalFieldValue.Parse` ham metni beyan edilen tipe çevirir; ayrıştırma her zaman
  `InvariantCulture` ve tarih yalnız ISO 8601.
- Vitrin okuma modeline YALNIZ `locale'li metin` tipi taşınır; tutar, tarih, tam sayı,
  evet/hayır ve seçenek tipleri biçimlendirme gerektirdiği için `0002`'nin sözleşmesi
  genişleyene kadar yansıtılmaz (0010b **D28**) ve bunu bir tripwire testi tutar.
