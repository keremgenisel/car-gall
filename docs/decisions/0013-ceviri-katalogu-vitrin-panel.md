# 0013 — Çeviri kataloğu ikiye ayrılır: vitrin ilk pakette, panel lazy pakette

| | |
|---|---|
| **Durum** | Kabul edildi |
| **Tarih** | 2026-11-19 |
| **Karar veren** | Takım Yöneticisi (ajan) görevlendirmesi, 2026-11-19 — dayanak `docs/ap.md` AP-12 |
| **İlgili** | ADR `0010` (C3 eşikleri yalnız kod PR'ında koşar), `0001` AC-35 · `0014` AC-59 · `0016b` C13 (LCP < 2,5 s), `docs/frontend.md` § 5 |

---

## Bağlam

`C3 eşikleri` (Lighthouse mobil, 3 koşu, medyan) `main` @ `8688aab` üzerinde yeşildi ama
ana sayfanın **19 ms** payı kalmıştı (koşu `44458828531`):

| Sayfa | `main` | PR #107 (0020) | PR #108 (0023) |
|---|---|---|---|
| `/tr/` | 2481 ms | **2561 ms** | **2558 ms** |
| `/tr/cars` | 2266 ms | — | — |
| ilan detayı | 2408 ms | **2556 ms** | 2411 ms |

İki kart da yalnız panel ekranı ekliyor, vitrine dokunmuyor — yine de vitrin kırmızıya
döndü. **Ölçüldü** (`vite build`, kaynak haritasıyla bayt atfı, `gzip -9`):

| | ilk paket (ham) | ilk paket (gzip) | katalogların payı (ham · gzip) |
|---|---|---|---|
| `main` | 433.828 B | 122.818 B | 122.171 B · 29.955 B (600 anahtar × 3 dil) |
| 0020 dalı | 456.230 B (**+22.402**) | 127.343 B (+4.525) | 143.951 B (**+21.780**) · 34.259 B |
| 0023 dalı | 441.031 B (**+7.203**) | 124.469 B (+1.651) | 129.330 B (**+7.159**) · 31.535 B |

İlk paketin büyümesinin **tamamı** çeviri kataloğundan geliyor. Sebep: `i18n.ts` üç dilin
kataloğunu **statik** içe aktarıyordu, yani paneli lazy yüklemek (0006 C13) panelin
*kodunu* ilk paketten çıkarmış ama *metinlerini* bırakmıştı. Panel kodu ilk pakette
**yok** — kaynak haritası `features/*/admin` ve `features/identity`'den sıfır bayt
gösteriyor; hipotezin o yarısı doğrulanmadı. 600 anahtarın 430'u yalnız panelde
kullanılıyor ve her yeni panel kartı (0020, 0021, 0022) bu payı ziyaretçinin indirdiği
pakete ekliyordu.

## Karar

AP-12 gereği **tek** optimizasyon: katalog iki kümeye ayrılır.

- **Vitrin** — `locales/{tr,ar,en}.json`, ilk pakette, 170 anahtar. Vitrinin statik import
  ağacının kullandığı her anahtar (panelin de kullandıkları dâhil).
- **Panel** — `locales/panel/{tr,ar,en}.json`, 430 anahtar. `shared/i18n/panel.ts`
  bunları `registerCatalogs` ile vitrin kataloğuna ekler; o modülü yalnız `AdminApp`
  içe aktarır, yani panelin lazy paketine girer. Çevirmen kataloğu çağrı anında okur;
  `App`'in kurduğu çevirmen panel anahtarlarını kayıttan sonra görür.

Ziyaretçi ve personel için davranış değişmez: vitrin aynı metinleri, panel aynı birleşik
kataloğu görür; yeni istek yok (panel kataloğu zaten indirilen panel paketinin içinde).

**Seçilmeyen:** yalnız aktif dilin kataloğunu `import()` ile yüklemek. Kazancı benzer
(~85 kB ham) ama ilk render'ın önüne bir ağ gidiş-dönüşü koyar — LCP öğesi API'den gelen
bir görsel olduğu için o bekleme doğrudan LCP'ye eklenir. Ayrıca her panel anahtarı
vitrini büyütmeye (üçte bir oranla) devam ederdi.

## Kapı

`check:locales` üç şeyi ölçer (`docs/frontend.md` § 8): her küme kendi içinde dil
eşitliği; bir anahtarın iki kümede birden olmaması; **vitrin sınırı** — `src/main.tsx`'ten
statik import ile ulaşılan modüller `panel.ts`'i içe aktarmaz ve hiçbir panel anahtarını
(şablon anahtarlarda sabit önekiyle) kullanmaz. Son kapı zorunlu, çünkü birim testleri
birleşik kataloğu yükler (`vite.config.ts` `setupFiles`) ve vitrinde boş kalan bir metni
göremez. Kaynak `typescript` ayrıştırıcısıyla okunur: ilk sürüm düzenli ifade kullanıyordu
ve noktalı virgülsüz kodda bir yan etki importunu (`import '…/panel'`) sonraki satırın
`from`'uyla birleştirip **kaçırıyordu** (QA BULGU-1, #109) — `panel.ts` hiçbir şey dışa
aktarmadığı için sızmanın en gerçekçi yolu tam oydu.

Kapı şu biçimlerde kırılıyor (elle denendi, `exit 1`): yan etki importu ilk satırda ve bir
import satırının altında; `export … from`; düz panel anahtarı; şablon öneki
(`` `vehicle.list.${x}` ``); `.` ile biten dizge (`'paintChange.input.' + part`); panel
kümesinde eksik anahtar. Kırılmaması gerekenler: yorumdaki anahtar adı, `import()`.

**Kapının görmediği** (bugün vitrinde örneği yok):

- Değişkenle kurulan önek: `` `${prefix}.generic` ``, `prefix + '.x'` (`prefix` başka bir
  satırda tanımlıysa ve `.` ile bitmiyorsa).
- `.` ile bitmeyen parçalardan birleştirme: `'login' + '.field.' + ad` yalnız `.field.`'ı,
  yani önek olmayan bir parçayı görür.
- Anahtarın vitrin dışından (API yanıtı, `localStorage`) gelmesi.
- `src/` dışındaki ya da `@/` gibi göreli olmayan yollarla içe aktarılan modüller (proje
  bugün yalnız göreli yol kullanıyor).

`AlanEtiketiKapisiTests` (backend) bir dilin kataloğunu artık iki dosyanın birleşimi olarak
okur; `vehicle.field.*` panel kümesine taşındı.

## Sonuçlar

| | ilk paket (ham) | ilk paket (gzip) | `AdminApp` paketi (ham · gzip) |
|---|---|---|---|
| önce (`main`) | 433.828 B | 122.818 B | 111.417 B · 25.242 B |
| sonra | 342.743 B (**−91.085, −%21**) | 100.952 B (**−21.866, −%18**) | 202.637 B · 45.834 B |

CI `C3 eşikleri` medyanları bu ADR'nin PR'ında raporlanır.

### Bedel

- Panelin ilk açılışı ~20 kB gzip daha fazla indirir (panel metinleri artık onun paketinde).
  Panel C3 kapsamında değil.
- Yeni anahtar yazan herkes kümeyi seçmek zorunda. Yanlış seçimin bir yönü (vitrinde panel
  anahtarı) kapıda kırılır; diğer yönü (panelin anahtarı vitrin kümesinde) yalnız paket
  boyutunu büyütür ve **kapı onu görmez**.
- Vitrin sınırı statik bir taramadır; görmediği biçimler § Kapı'da listeli.

## Ek — tembel vitrin kümesi (`frontend-003` D13, 2026-11-26)

Üçüncü bir küme: **favoriler** (`locales/favorites/*.json`). Vitrinin ziyaretçiye açık ama ilk pakete
girmeyen ekranlarının (favoriler ve karşılaştırma) metinleri burada yaşar ve ekranların tembel
parçasıyla iner; kaydı `shared/i18n/favoritesCatalog.ts` yapar ve onu yalnız o parça içe aktarır.
`check:locales` tembel her kümeyi dört denetimin her birine alır (üç dil eşitliği, Arapça çoğul
biçimleri, başka kümede tekrar yok, vitrin sınırı); küme yalnız `TEMBEL_KUMELER`'e eklenir. Aynı
kartla kapıya **anahtar varlığı** eklendi (D19): `src` altındaki her `t('…')` sabit anahtarı bir
kümede, şablonlu anahtarın sabit öneki bir anahtarın başında olmalı. Sabit öneki olmayan şablon
(`` t(`${prefix}.x`) ``) ve `t` dışında adlandırılmış çevirmenler statik olarak ölçülmez.
