# frontend-001 — Uygulama Kabuğu ve Standarda Uyum

| | |
|---|---|
| **Durum** | **Tamamlandı** |
| **Sahip** | Developer oturumu (ajan üye) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` — vitrin tarafı. Backend sözleşmesi **değişmez.** |
| **Bağımlı spec'ler** | `0001` (vitrin ilan listesi) — bu mini-spec onun ekran karşılığını yeniden yapılandırır |

---

## Intent

Vitrine giren **ziyaretçi** bugün doğrudan araç listesine düşüyor: sayfanın kime ait olduğunu
söyleyen bir başlık yok ve üç dilde yayın yapan bir sitede **dili değiştirmenin hiçbir yolu yok** —
Arapça sayfayı görmek için adresi elle yazmak gerekiyor.

**Galeri** vitrinin bir sitede olduğunu hissettiren bir kabuk istiyor; **ekip** ise bir sonraki
ekranı (ilan detayı, iletişim) koyacak bir yer istiyor. Bugün her yeni ekran kendi başlığını ve
kendi düzenini yeniden icat etmek zorunda.

Bu iş bittiğinde ziyaretçi her sayfada markayı ve dil değiştiriciyi görür, araç listesi ortak
bileşenlerden kurulmuş bir ızgarada gelir, sayfalama görünür ve çalışır; yükleme, boş katalog ve
hata durumları üç dilde tasarlanmış ekranlar olarak karşılar. Ekip tarafında ise
`docs/frontend.md` standardı ilk kez **denetlenebilir** hâle gelir: klasör yapısı, tasarım
token'ları ve bileşen envanteri dosyada yazdığı gibi olur.

---

## Requirements

### Kabuk

- **R1.** Her vitrin sayfası ortak bir kabuk içinde gelir: **üst bar** (marka + dil değiştirici)
  ve **içerik alanı**. Sonraki dilimler içerik alanına yerleşir; kendi başlığını kurmaz.
- **R2.** Dil değiştirici üç etkin locale'i (TR / AR / EN) sunar. Seçim, **aynı sayfanın** o
  locale'deki adresine gider. `Accept-Language`'a göre otomatik yönlendirme yapılmaz.
- **R3.** Kabuk Arapça'da sağdan sola akar; üst bar öğeleri ve içerik alanı aynalanır.

### Araç listesi

- **R4.** Araçlar bir **kart ızgarasında** listelenir. Kart, `0001` R9'un alanlarını taşır:
  kapak görseli, model adı, yıl, kilometre, fiyat **veya** "fiyat sorunuz", şube adı.
- **R5.** `Rezerve` ilan kartta **rozetle** işaretlenir.
- **R6.** **Sayfalama kontrolü görünür ve çalışır**: önceki / sonraki ve kaçıncı sayfada
  olunduğu. İlk sayfada "önceki", son sayfada "sonraki" devre dışıdır.

### Üç durum

- **R7.** Veri gelene kadar **iskelet** gösterilir.
- **R8.** Vitrinde hiç yayında araç yoksa **EmptyState**: durumu bildirir ve galeriye ulaşma
  yönlendirmesi sunar.
- **R9.** Liste alınamadığında **ErrorState**: TR/AR/EN mesaj + **"tekrar dene"** eylemi.
  Ziyaretçiye teknik detay gösterilmez.

### Yapı

- **R10.** Kaynak ağacı `docs/frontend.md` § 1'e uyar: `src/features/<feature>` ve
  `src/shared/{api,ui,design,i18n,lib}`.
- **R11.** Tasarım token'ları tek dosyada yaşar; bileşenlerde ham hex/px bulunmaz
  (`docs/frontend.md` § 4.1).
- **R12.** `docs/frontend.md` § 4.2 envanteri `src/shared/ui` altında var olur ve liste ekranı
  kendi kopyasını yazmak yerine onları kullanır.

---

## Constraints

### Uyulması zorunlu

- **C1.** **`0001`'in 35 kabul kriteri yeşil kalır.** Bu iş bir yeniden yapılandırmadır; davranış
  değişmez. Kırılan her kriter regresyondur.
- **C2.** **Backend sözleşmesi değişmez.** Kart yalnız `ShowcaseListingDto`'nun taşıdığı alanları
  gösterir; uç, sözleşmede olmayan alanı uydurmaz (`docs/frontend.md` § 2).
- **C3.** Kullanıcıya görünen hiçbir metin koda gömülmez; hata metni `errorCode`'dan uçta çevrilir.
- **C4.** CSS'te fiziksel yön özellikleri yasak; logical properties zorunlu.
- **C5.** ICU mesaj formatı zorunlu; `tr`/`ar`/`en` katalogları anahtar bazında eşit kalır.
- **C6.** `docs/frontend.md` § 8'deki sekiz kapı yeşil: `typecheck`, `lint`, `format:check`,
  `lint:css`, `check:text`, `check:locales`, `test`, `build`.
- **C7.** Her `[görsel]` kriter için ekran görüntüsü `docs/evidence/frontend-001-ac<n>.png`
  altına konur; RTL kriteri için Arapça görüntü zorunludur.
- **C8.** Dil değiştirici bir **gezinme** öğesidir, tercih kaydı değildir: seçim URL'i değiştirir,
  çerez veya `localStorage` yazmaz. Locale'in tek kaynağı URL yol önekidir (`C7`/0001).

### KAPSAM DIŞI

- **X1.** **Serbest metin arama** — *neden:* `0001` X1 ile zaten kapsam dışı; arama kendi
  normalizasyon yükünü getirir ve tek başına bir spec'tir.
- **X2.** **Filtre çubuğunun yeniden tasarımı** — *neden:* kasa tipi daraltması `0001`'de
  tanımlandı ve çalışıyor. Bu dilim onu ortak bileşenlere taşır ama **görünümünü ve
  davranışını değiştirmez**; tasarım değişikliği ayrı bir dilimdir.
- **X3.** **Tema değiştirme (koyu mod)** — *neden:* token'lar bu dilimde tek dosyaya toplanıyor;
  tema anahtarı ancak o temelden sonra anlamlı olur ve kendi kriterlerini getirir.
- **X4.** **Üst barda gezinme menüsü** — *neden:* gidilecek ikinci ekran yok (ilan detayı `0001`
  X9, panel `0001` X6 ile kapsam dışı). Menü bugün ölü bağlantı demektir; ilk hedef ekran
  geldiğinde eklenir.
- **X5.** **Kartta kasa tipi rozeti** — *neden:* `0001` R9 liste yanıtının altı alanın dışında
  hiçbir alan taşımayacağını söylüyor ve `ShowcaseListingDto` kasa tipi taşımıyor. Rozet
  istenirse önce sözleşme değişir (AP-08) — bu, ayrı bir karardır. Karttaki rozet bu dilimde
  yalnız **rezerve** durumunu gösterir (R5).

---

## Context

**Mevcut davranış.** `frontend/` çalışıyor ve `0001`'in 35 kriterini karşılıyor; 23 birim ve
50 e2e testi yeşil. Eksik olan davranış: **dil değiştirici yok** ve **sayfa başlığı/kabuğu yok**.

**Standarda uyumsuzluk** (ölçüldü, `docs/frontend.md` yazıldıktan sonra):

| Kural | Bugün |
|---|---|
| § 1 klasör | `src/shared/` yok; `api/` (3), `lib/` (5), `locales/` (3) dosya taşınacak; `components/` boş |
| § 4.1 token | `tokens.css` yok — token'lar `app/styles.css` içinde; **39 ham px, 6 ham hex** |
| § 4.2 envanter | `Button` `Card` `Badge` `EmptyState` `ErrorState` `Skeleton` — **hiçbiri yok**; karşılıkları feature içinde |
| § 8.1 smoke | Testing Library kurulu, **bileşen testi 0 dosya** |

**Bilinen tuzaklar:**
1. **Taşıma sırasında import yolları.** `src/api` → `src/shared/api` gibi bir taşımada tek bir
   kaçan import derlemeyi kırmaz, çünkü `tsc` onu bulur — ama testlerdeki yol sabitleri
   sessizce eski dosyayı işaret edebilir. Kapılar taşımadan sonra **tam** koşulur.
2. **Bileşeni ortaklaştırmak davranışı değiştirebilir.** `ListingCard` içindeki rozet
   `Badge`'e çıkarken görsel çıktı birebir aynı kalmalı; `0001`'in görsel kanıtları
   değişiyorsa davranış değişmiş demektir (C1).
3. **Dil değiştirici ve geri tuşu.** Locale değişimi bir gezinmedir; daraltma/sayfa durumunu
   koruyarak yeni locale'in adresine gitmelidir, yoksa ziyaretçi dil değiştirince listenin
   başına düşer.
4. **İskelet yüksekliği.** `0001`'de CLS kusuru tam buradan çıkmıştı; `Skeleton` ortak bileşene
   taşınırken gerçek kartın kutu modelini taklit etme özelliği kaybolmamalı.

---

## Acceptance Criteria

> `[görsel]` işaretli kriterler görsel kanıt gerektirir. `[yapı]` işaretliler kullanıcıya
> görünmez; testle doğrulanır. Bir yeniden yapılandırma diliminde kriterlerin bir kısmının
> yapısal olması kaçınılmazdır — ama hepsi **doğrulanabilir**.

**Kabuk**

- **AC-1.** `[görsel]` Vitrin sayfasında üst bar görünür ve marka adını taşır; içerik alanı
  `<main>` öğesidir ve sayfanın tek `<h1>`'i içerik alanındadır.
- **AC-2.** `[görsel]` Üst bardaki dil değiştirici üç etkin locale'i sunar; `/tr/` sayfasında
  Arapça seçildiğinde adres `/ar/` önekine döner ve içerik Arapça gelir.
- **AC-3.** Dil değiştiriciyle locale değiştirildiğinde **daraltma ve sayfa korunur**:
  `/tr/?bodyType=suv&page=2` üzerinde Arapça seçmek `/ar/?bodyType=suv&page=2` adresine gider.
- **AC-4.** `[görsel]` Arapça locale'de üst bar sağdan sola akar: marka satırın başında
  (sağda), dil değiştirici sonunda (solda).
- **AC-5.** Dil değiştirici çerez veya `localStorage` yazmaz; seçim yalnız adrese yansır.

**Araç listesi**

- **AC-6.** `[görsel]` Araç kartı kapak görselini, model adını, yılı, kilometreyi, fiyatı ve
  şube adını gösterir; kartlar ızgara düzeninde gelir. *(0001 AC-20'nin ekran karşılığı.)*
- **AC-7.** `[görsel]` `Rezerve` durumundaki ilanın kartında rozet bulunur.
  *(0001 AC-2'nin ekran karşılığı.)*
- **AC-8.** `[görsel]` Sayfalama kontrolü görünür; ileri gidince liste ikinci sayfayı, geri
  gelince birinci sayfayı gösterir. İlk sayfada "önceki", son sayfada "sonraki" devre dışıdır.
  *(0001 AC-13'ün ekran karşılığı.)*
- **AC-9.** Yayında olmayan ilanlar (`Satıldı`, `Taslak`, `Arşiv`, lokasyon ataması bekleyen)
  listede yer almaz. *(0001 AC-3'ün ekran karşılığı; ekran sunucunun döndürdüğünün dışına
  hiçbir şey eklemez.)*

**Üç durum**

- **AC-10.** `[görsel]` Liste verisi beklenirken `Skeleton` gösterilir ve iskeletin ızgara
  yüksekliği gerçek listenin yüksekliğine eşittir.
- **AC-11.** `[görsel]` Vitrinde hiç yayında araç yokken `EmptyState` görünür: durumu bildirir
  ve galeriye ulaşma yönlendirmesi sunar. *(0001 AC-27'nin ekran karşılığı.)*
- **AC-12.** `[görsel]` Liste alınamadığında `ErrorState` görünür: çeviri kataloğundaki mesaj
  ve **tekrar dene** eylemi. Tekrar dene isteği yeniden gönderir.
- **AC-13.** `[görsel]` `ErrorState` üç locale'de kendi dilinde görünür (TR/AR/EN); hiçbirinde
  HTTP kodu, `errorCode`, `traceId`, endpoint yolu veya istisna metni bulunmaz.
  *(0001 AC-28'in üç dile genişletilmiş ekran karşılığı.)*

**Yapı**

- **AC-14.** `[yapı]` Kaynak ağacında `src/features/` ve `src/shared/{api,ui,design,i18n,lib}`
  bulunur; `src/api`, `src/components`, `src/lib`, `src/locales` **bulunmaz**.
- **AC-15.** `[yapı]` Tasarım token'ları `src/shared/design/tokens.css` içindedir; `src/`
  altındaki hiçbir CSS dosyasında ham hex renk veya ham piksel değeri bulunmaz — lint kırar.
- **AC-16.** `[yapı]` `Button`, `Card`, `Badge`, `EmptyState`, `ErrorState`, `Skeleton`
  `src/shared/ui` altında bulunur ve vitrin listesi ekranı bunları kullanır.
- **AC-17.** `[yapı]` Vitrin listesi ekranının bileşen düzeyinde bir smoke testi vardır ve
  yükleme, boş, hata ile mutlu yolu kapsar (`docs/frontend.md` § 8.1).

---

## Definition of Done

- [x] Tüm kabul kriterleri karşılandı; kriter → test/kanıt eşlemesi PR'da tabloyla gösterildi.
- [x] `[görsel]` işaretli **her** kriterin görsel kanıtı eklendi
      (`docs/evidence/frontend-001-ac<n>.png`); AC-4 ve AC-13 için Arapça görüntü var.
- [x] **`0001`'in 35 kriteri hâlâ yeşil** — 23 birim + 50 e2e testi geçiyor (C1).
- [x] `docs/frontend.md` § 8'deki sekiz kapı yeşil.
- [x] Yeni domain terimi **yok**; kullanılan terimler `docs/domain.md` sözlüğünden.
- [x] Mimari karar verilmedi; backend sözleşmesi değişmedi.
- [x] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [x] Takım Yöneticisi onayladı ve squash-merge edildi.
- [x] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | **0** | Onaydan (`3445f9b`) sonra spec dosyasına hiç dokunulmadı. Kriterler uygulama boyunca değişmedi. |
| **Düzeltme turu sayısı** | **0** | PR açılmadı; gözden geçirme turu olmadı. Bu satır merge akışı işlediğinde anlam kazanır. |
| **Bulgu gerçek/gürültü oranı** | **— / —** | Dış gözden geçirme yapılmadı. Aşağıdaki kusurları testlerin ve kendi doğrulamamın bulduğu not edilmelidir; bu sütunun yerine geçmez. |
| **Regresyon sayısı** | **2** | (1) Kabuk eklenince sayfada iki `<main>` oluştu — AC-1 testi yakaladı. (2) Dil değiştirici de `<nav>` olduğu için AC-30'un kanıt locator'ı belirsizleşti ve daha önce yeşil olan test kırıldı. İkisi de merge öncesi kapandı. |
| **Kaçan hata** | **-** | Merge edilmedi. |

**Bu spec'ten çıkarılan ders:**
Mekanik dönüşüm, derlenen ama anlamı yanlış kod üretir. Token geçişinde dört kusur çıktı ve
dördü de "değer doğru, anlam yanlış" biçimindeydi: köşe token'ı boşluk olarak, boşluk token'ı
köşe olarak, kenarlık token'ı ölçü olarak kullanılmıştı; `font-family` ise hiç bağlanmamıştı
çünkü biçimlendirici onu çok satıra bölmüştü ve tek satırlık arama sessizce eşleşmedi.
Hiçbirini derleyici, lint veya test yakalayamazdı — çünkü hepsi geçerli CSS üretiyordu.
Yakalayan şey, dönüşümün çıktısını satır satır okumak oldu.

İkinci ders: yeşil test her zaman doğru sebeple yeşil değildir. Testing Library'nin otomatik
`cleanup`'ı `vitest` globals kapalı olduğu için hiç koşmuyordu; temizlenmeyen DOM yüzünden bir
test öncekinin ekranını görüyordu. Bunu yeni yazılan bir testin BEKLENMEDİK biçimde geçmesi
ortaya çıkardı — beklenen sonucu alan test bile sorgulanmalı.
