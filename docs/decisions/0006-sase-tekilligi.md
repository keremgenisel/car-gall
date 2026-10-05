# 0006 — Şase tekilliği: kısmi tekil indeks ve aracın kendi arşiv bayrağı

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-05
- **Bağlam:** spec 0010a — Panel: Araç Kaydı (D11, A5, AC-16, AC-17)

## Bağlam

`docs/domain.md` **A5** şöyle diyor: şase numarası galeri genelinde **tekildir**, ancak
`Arşiv` durumundaki kayıtlar bu kontrolden **muaftır**. İki yarısı da bu spec'te ilk kez
uygulanabilir hâle geldi, çünkü `Vehicle` agregatı `0010a`'ya kadar hiç kaydedilmiyordu.

Zorluk şurada: muafiyetin dayandığı **durum** `listings` tablosunda, tekilliğin konusu olan
**şase** ise `vehicles` tablosunda. `vehicles` üzerindeki bir veritabanı kısıtı
`listings.status`'u göremez.

`0010a`'nın onaylanan ilk kararı (**D11**'in ilk hâli) bu yüzden tekilliği `0007`'den
gelen `InvariantGuard` ile — satır kilidi + tek transaction — kurmayı söylüyordu.

## Karar

**Tekillik `vehicles` üzerinde `NOT is_archived` filtreli bir kısmi tekil indeksle kurulur.
`Vehicle` kendi `IsArchived` bayrağını taşır; bayrak ilan arşivlendiği anda, aynı
transaction içinde `Vehicle.MarkArchived()` ile yazılır.**

Uç, veritabanının `23505` hatasını `ux_vehicles_vin` kısıt adıyla eşleyip
`vehicle.duplicate_vin` koduna çevirir (`409`).

## Gerekçe

### `InvariantGuard` bu invariantı koruyamıyor

Ölçüldü ve karar uygulama sırasında **gözden geçirildi**. `InvariantGuard`'ın deseni şu:
ilgili agregatın satırını `SELECT … FOR UPDATE` ile kilitle, kilit altında yeniden oku,
karar ver. Bu desen `0007`'de çalışıyor çünkü kilitlenecek satır **vardır** — bir ilan
rezerve edilirken o ilanın satırı ortadadır.

Şase tekilliğinde kilitlenecek satır **yoktur**: henüz kaydedilmemiş bir aracın şasesini
sorguluyoruz. Aynı şaseyle gelen iki eşzamanlı istek, ikisi de "bu şaseden sıfır tane var"
sonucunu okur ve ikisi de yazar. Yani birinci karar, A5'i koruyormuş gibi görünen ama
koruMAyan bir mekanizma seçiyordu.

Tekillik, satırlar arasına yayılan bir sayım değil **bir kısıttır**; kısıtı veritabanı
uygular.

### Bayrağın kayma riski nasıl karşılanıyor

D11'in ilk hâli denormalize bayrağı *"kaymaya açık ve kayma sessizdir"* diye elemişti.
İtiraz gerçek ama tek giriş kapısıyla karşılanıyor:

- Bayrağı değiştiren tek yol `Vehicle.MarkArchived()`; `IsArchived`'ın ayarlayıcısı yok.
- Arşivleme `0012`'nin işi ve orada ilan durumu ile bayrak **aynı transaction'da** yazılır.
- `0010a`'da hiçbir şey arşivlemiyor (X2), yani bayrak her zaman `false` ve AC-17 doğrudan
  agregat üzerinden sınanıyor.

*Bilerek ödenen bedel:* aynı olgu iki yerde duruyor (`listings.status` ve
`vehicles.is_archived`). Ayrı transaction'larda yazıldığı gün indeks yanlış kararı verir ve
bu sessiz olur — `0012` bu kapıyı kapatmak zorunda.

*Elenen alternatif:* şaseyi `listings`'e taşımak. Şase aracın özelliğidir, ilanın değil;
bir araç yeniden ilanlandığında şase ilanla birlikte kopyalanırdı.

## Sonuç

- `vehicles` tablosunda `ux_vehicles_vin` kısmi tekil indeksi (`NOT is_archived`).
- `Vehicle.IsArchived` + `MarkArchived()`; başka giriş kapısı yok.
- `CreateVehicleCommand`, `23505` + `ux_vehicles_vin` eşleşmesini `vehicle.duplicate_vin`
  koduna çevirir; "önce bak sonra yaz" yolu **hiç yazılmadı**.
- `0012` ilanı arşivlerken bayrağı aynı transaction'da yazmak zorundadır.
