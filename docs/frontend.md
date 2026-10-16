# Frontend Standardı

Bu dosya **arayüz tarafının tek gerçek kaynağıdır.** Bir frontend işi burada yazılana göre
denetlenir — kişiye, alışkanlığa veya "geçen sefer böyle yapmıştık"a göre değil.

**Burada yaşamayan kurallar** (tekrarlanmaz, yalnız atıf yapılır):

| Konu | Sahibi |
|---|---|
| Hata sözleşmesi (`errorCode`, `ProblemDetails`), para/sayı saklama | `docs/conventions.md` § 5, § 7 |
| Locale çözümleme, SEO, `hreflang`, sitemap, çeviri sahipliği | `docs/architecture.md` § 3 |
| Test stratejisi, görsel kanıt konumu, CI kapılarının listesi | `docs/testing.md` |
| Branch, commit, PR, merge | `docs/git.md` |
| Ortak dil (araç, ilan, vitrin, şube…) | `docs/domain.md` |

Buradaki bir kuralı **gevşetmek** öneri kuralına tabidir (`AGENTS.md` Altın Kural 2).
Kuralı sıkılaştırmak veya eksik bir alanı doldurmak serbesttir.

---

## 1. Dil ve Yapı

**TypeScript zorunludur.** `strict` açık, `any` yasak; bilinmeyen için `unknown` + daraltma.
`.js`/`.jsx` kaynak dosyası bulunmaz. *Gerekçe:* bu arayüz backend sözleşmesini tüketiyor;
sözleşme değiştiğinde kırılmanın derleme anında görünmesi, çalışma anında görünmesinden ucuzdur.

```
frontend/
  src/
    app/                    → uygulama kabuğu: rota ağacı, sağlayıcılar, giriş noktası
    features/<feature>/     → EKRANLAR. Klasör adı backend modülüyle aynıdır.
    shared/
      api/                  → TEK API client + çağrı grupları + DTO tipleri (§ 2)
      ui/                   → ortak bileşenler; envanter § 4.2'de
      design/               → tasarım token'ları (§ 4.1)
      i18n/                 → çeviri katalogları ve biçimlendirme yardımcıları (§ 6)
      lib/                  → saf yardımcılar. Yan etkisiz, test edilebilir.
  e2e/                      → uçtan uca akış testleri (docs/testing.md § 3)
  scripts/                  → kalite kapısı betikleri (§ 8)
```

Kurallar:

- **`features/` altındaki klasör adları backend modül adlarıyla aynıdır** (`catalog`, `leads`,
  `dealership`, `content`, `identity`). Bir ekranın hangi modüle ait olduğu klasöründen okunur.
- **Bir feature başka bir feature'ın iç dosyasını import etmez.** Paylaşılan şey `shared/`e çıkar.
  İki feature'ın aynı bileşene ihtiyacı varsa bu, bileşenin ortak olduğunun kanıtıdır.
- **`shared/` altındaki hiçbir şey `features/`ten import etmez.** Bu ok tek yönlüdür; çift yönlü
  olduğu gün "ortak" klasörü ikinci bir feature'a dönüşür.
- Bir dosya bir kavram. 300 satırı aşan bileşen dosyası gözden geçirilir.

---

## 2. API Disiplini

**Tüm ağ istekleri `shared/api` altındaki tek client'tan geçer.** Bileşenin içinde `fetch`
veya `axios` çağrısı **yasaktır**.

- **Endpoint yolu yalnızca `shared/api` içinde yazılır.** Bileşende URL dizesi bulunmaz.
  *Gerekçe:* rota değiştiğinde aranacak tek yer olsun; `grep` ile bulunamayan URL, taşınamayan
  URL'dir.
- **DTO tipleri backend sözleşmesinden türetilir** ve `shared/api/types.ts` altında yaşar.
  Uç, backend'in `X.Contracts` yüzeyinde olmayan bir alanı uydurmaz; alan gerekiyorsa önce
  sözleşme değişir. Tip ile sözleşmenin ayrıştığı yer, sessiz hata üreten yerdir.
- Client'ın tek yerde çözdüğü şeyler: taban URL, `Accept-Language`, HTTP hatasının **tek tip
  hata nesnesine** (`errorCode` + parametreler) dönüştürülmesi, `traceId` yakalama, iptal
  (abort) ve zaman aşımı.
- **Client ham hata metni döndürmez**, `errorCode` döndürür. Çeviri uçta yapılır (§ 5).
- Sunucu verisi için sorgu/önbellek katmanı kullanılır; **sunucu verisi global store'a
  kopyalanmaz.** İstemci durumu (form, seçim) yereldir.
- **Filtre ve sayfalama URL'de yaşar** — paylaşılabilir bağlantı ve çalışan geri tuşu bunun
  sonucudur, ayrıca yapılan bir iş değil.

---

## 3. Üç Durum Kuralı

Veri çeken **her** ekran ve her veri bölgesi üç durumu da **tasarlar**. Üçü de ele alınmamış
ekran tamamlanmış sayılmaz ve kabul kriteri karşılanmamıştır.

| Durum | Kural |
|---|---|
| **Yükleniyor** | İskelet (`Skeleton`). Boş ekran veya ani sıçrama yok. |
| **Boş** | "Sonuç yok" yeterli değil: neden boş ve kullanıcı **ne yapabilir** söylenir. |
| **Hata** | Anlamlı mesaj + **tekrar dene** eylemi. Teknik detay yok. |
| **Kısmi başarı** | Yalnız **çok kayıtlı bir eylemin** sonucunda doğar: bir kısmı uygulandı, bir kısmı uygulanmadı. Kaç kaydın uygulandığı ICU ile söylenir ve **uygulanmayanlar tek tek, gerekçesiyle** listelenir. "Başarılı" veya "hata" olarak yuvarlanmaz. |

**Dördüncü satır 2026-10-11'de eklendi** (spec `0012b` / D7, Takım Yöneticisi onayı) ve
bölümün adı bilinçli olarak "Üç Durum Kuralı" kaldı: üç durum **veri çeken her** ekranın
borcudur, kısmi başarı ise yalnız çok kayıtlı bir eylemin ekranında doğar. Eklenmesinin
nedeni ölçüldü: `0012b`'nin R19'u beş durum istiyor ve dayanak olarak bu bölümü gösteriyordu,
oysa burada üç durum yazılıydı ve "kısmi başarı" terimi dokümanların **hiçbirinde**
geçmiyordu (`grep -rn "kısmi başarı" docs/` → sıfır satır). Dayanağı olmayan bir kabul
kriteri doğrulanamaz; kuralın kendisi olmadan her ekran kendi yorumunu üretirdi.

- **İskelet, gerçek içeriğin kutu modelini taklit eder** — aynı en-boy oranı, aynı satır
  yükseklikleri, aynı sayıda öğe. *Gerekçe:* yer tutucunun işi boşluğu doldurmak değil, **doğru**
  boşluğu tutmaktır; yanlış yükseklik veri gelince düzeni kaydırır (CLS).
- **Boş durumlar ayrıştırılır.** "Filtre sonucu boş" ile "hiç kayıt yok" farklı ekranlardır ve
  farklı eylem sunarlar. İkisi aynı anda doğruysa **daha temel olan** kazanır — aksi hâlde
  "filtreyi temizle" düğmesi kullanıcıyı olmayan bir sonuca yollar.
- **Kısmi hata tüm sayfayı düşürmez**; o bölge kendi hata durumunu gösterir.
- Boş ve hata durumlarının **görsel kanıtı** PR'a eklenir (`docs/testing.md` § 3). Bu ikisi en
  sık atlanan ve en sık bozuk olan durumlardır.

### 3.1 Kullanıcıya teknik detay sızmaz

**Yasak:** HTTP durum kodu, `errorCode`'un kendisi, stack trace, SQL, endpoint yolu, sunucu
istisna metni, `traceId`.

1. API `errorCode` döner (`vehicle.cannot_publish.no_media`).
2. Uç, bu kodu çeviri kataloğundan kullanıcı diline çevirir.
3. Karşılığı yoksa genel bir mesaj gösterilir ve kod **konsola/loga** yazılır.
4. `traceId` destek için saklanır; istenirse "destek kodu" olarak kopyalanabilir sunulur —
   hata metninin içine gömülmez.

**Her hata mesajı üç etkin dilde (TR / EN / AR) ve eylem önerili yazılır:** ne olduğunu ve ne
yapılabileceğini söyler ("tekrar dene"). Tek dilde yazılmış bir hata mesajı, o dili konuşmayan
ziyaretçi için boş ekrandır.

---

## 4. Tasarım Sistemi

### 4.1 Token'lar

**Tasarım token'ları tek dosyada yaşar:** `src/shared/design/tokens.css`. Token grupları:
**renk**, **aralık (spacing)**, **tipografi**, **köşe yarıçapı (radius)**.

- **Bileşenlerde gömülü sabit değer YASAKTIR:** hex renk (`#1f5fd0`), ham piksel (`12px`),
  ham font boyutu. Değer token'dan gelir: `var(--color-accent)`, `var(--space-3)`.
- İstisna yoktur; yeni bir değer gerekiyorsa **önce token tanımlanır**, sonra kullanılır.
  *Gerekçe:* tema, koyu mod ve marka değişimi tek dosyadan yapılabilir olsun. Bileşene gömülen
  her hex, o değişimi bir sonraki kişinin borcuna yazar.
- Lint bunu kırar (§ 8).

### 4.2 Bileşen Envanteri

Ortak bileşenler `shared/ui` altında yaşar. **Başlangıç envanteri:**

| Bileşen | Sorumluluğu |
|---|---|
| `Button` | Birincil / ikincil / metin varyantları; `disabled` ve yükleniyor hâli |
| `Card` | İçerik kutusu: kenarlık, köşe, iç boşluk |
| `Badge` | Kısa durum etiketi (ör. "Rezerve") |
| `EmptyState` | Başlık + açıklama + **tek eylem** (§ 3) |
| `ErrorState` | Başlık + açıklama + **tekrar dene** (§ 3.1) |
| `Skeleton` | Gerçek içeriğin kutu modelini taklit eden yer tutucu (§ 3) |

**Her yeni ekran önce bu envantere bakar.** Envanterde karşılığı olan bir şeyi yeniden yazmak
gözden geçirmede reddedilir. Karşılığı yoksa **yeni bileşen önerilir** (öneri kuralı): ne, neden,
hangi iki ekranda kullanılacak. Tek ekranda kullanılan şey ortak bileşen değildir; o feature'ın
içinde kalır.

---

## 5. Metin ve Çok Dillilik

- **Kullanıcıya görünen hiçbir metin bileşene gömülmez.** İstisna yok — `placeholder`,
  `aria-label`, `title`, `alt`, boş durum metni, buton etiketi dâhil. Lint bunu kırar (§ 8).
- Çeviri katalogları: `src/shared/i18n/locales/{tr,ar,en}.json`. Üçü **anahtar bazında eşittir**;
  CI eksik anahtarı kırar.
- **ICU mesaj formatı zorunludur.** Dize birleştirmeyle cümle kurulmaz: Arapça'da **altı çoğul
  biçimi** vardır ve `count > 1` mantığı sessizce yanlış cümle üretir.
- **Arapça bir `plural` mesajı `one`, `two`, `few`, `many` ve `other` biçimlerinin hepsini
  tanımlar.** Yalnız `other` yazmak ICU'yu kullanmış ama kuralı atlamış olmaktır: cümle
  1, 2 ve 3–10 için dilbilgisi hatalı çıkar — yani gerçekçi her sayıda. `check:locales`
  bunu denetliyor ve kapı bir dış bulgunun bedeli (`0012c`): anahtar kümeleri eşitti,
  altı mesajın biçim kapsamı değildi.
- **Blok yorumunda iki referans `/` ile ayrılmaz; ayraç `·`'tır.** `**R4**` ile `**D6**`
  arasına `/` koymak kaynakta `**` + `/` üretir — yani blok yorumunun sonlandırıcısını — ve
  yorumun kalanı koda döner. `0012a`, `0012b` ve `0012c`'de **üç kez** tekrarladı; artık
  `check:comments` denetliyor (§ 8). Doğrusu: `**R4** · **D6**`.
- Tarih, sayı ve para biçimlendirmesi **uçta**, aktif locale ile yapılır. Sunucu ham sayı + para
  birimi kodu döner (`docs/conventions.md` § 7).
- **Rakam sistemi açıkça sabitlenir.** Varsayılana güvenilmez: `ar` locale'inin ICU varsayılanı
  ortama göre Hint-Arap rakamı olabilir ve hata yalnızca belirli bir tarayıcıda görünür.
- Locale URL yol önekinden gelir (`/tr`, `/ar`, `/en`). Geçersiz önek → 404.
  `Accept-Language` ile **otomatik yönlendirme yapılmaz**.

---

## 6. RTL

- **Yön locale'in özelliğidir**; `<html lang dir>` sunucudan basılı gelir, uçta yeniden
  hesaplanmaz.
- **CSS'te fiziksel yön özellikleri yasaktır** — lint kırar: `left`, `right`, `margin-left`,
  `margin-right`, `padding-left`, `padding-right`, `text-align: left|right`,
  `border-left|right`, `float`, `clear`.
  Yerine **logical properties**: `inset-inline-start`, `margin-inline-start`,
  `padding-inline-end`, `text-align: start|end`, `border-inline-start`.
- Yön duyarlı ikonlar (ok, geri, ileri) mantıksal olarak aynalanır.
  **Aynalanmaz:** marka logoları, medya oynatma kontrolleri, saat, grafik eksenleri.
- Karışık yönlü içerik (Arapça metin içinde Latin model adı veya rakam) için `dir="auto"`
  veya `<bdi>`.
- **Admin paneli de tam RTL.** Yarı-RTL bir panel bozuk görünür.
- RTL kriteri olan her ekranın **Arapça görsel kanıtı** zorunludur; LTR görüntü RTL kriterini
  kanıtlamaz.

---

## 7. Erişilebilirlik Tabanı

- **Semantik HTML.** Başlık hiyerarşisi (`h1` → `h2`) atlanmaz; liste `<ul>/<li>`, gezinme
  `<nav>`, sayfa gövdesi `<main>`. Tıklanabilir `<div>` yazılmaz — düğme `<button>`, bağlantı
  `<a>`.
- **Her form alanının `<label>`'ı vardır.** Yalnız `placeholder` etiket değildir.
- **Klavyeyle gezilebilirlik:** her etkileşimli öğeye Tab ile ulaşılır, Enter/Space ile
  çalıştırılır, **odak halkası görünür**. Odak tuzağı bırakılmaz.
- **Kontrast:** normal metin en az **4.5:1**, büyük metin ve arayüz öğesi en az **3:1**.
- **Renk tek başına anlam taşımaz**; durum ayrıca metin veya ikonla anlatılır.
- Her görselin `alt` metni vardır ve locale'e göre üretilir; dekoratif görselde `alt=""`.
- Araç görselleri `loading="lazy"`, açık `width`/`height` ile (CLS önlemi) ve responsive
  `srcset` ile gelir. **Katlama üstündeki kapak görseli hariç:** o `eager` yüklenir.

---

## 8. Kalite Kapıları

Aşağıdakiler **zorunludur** ve pipeline'da koşar. Hepsinin komutu `package.json`'da tanımlıdır;
komutu olmayan kapı, kapı değildir.

| Kapı | Komut | Ne kırar |
|---|---|---|
| Tip kontrolü | `npm run typecheck` | `tsc --noEmit`, hatasız — **`src/` ve `e2e/` birlikte**. `e2e/` 2026-10-16'ya kadar kapsam dışındaydı ve bu ölçülmüş bir boşluktu: `0013`'ün e2e yardımcısında eksik bir import ve `isVisible`'ın beklemediği bir çağrı, ancak üç e2e koşusu sonra göründü. Playwright spec'leri kendi derleyicisiyle (esbuild) çalıştırıyor ve **tip hatası bildirmiyor** |
| Lint (JS/TS) | `npm run lint` | ESLint — hatasız, **uyarı da kabul edilmez** |
| Biçim | `npm run format:check` | Prettier — biçim farkı yok |
| Lint (CSS) | `npm run lint:css` | Fiziksel yön özellikleri (§ 6) ve gömülü hex/px (§ 4.1) |
| Sert kodlanmış metin | `npm run check:text` | Bileşene gömülü kullanıcı metni (§ 5) |
| Eksik çeviri anahtarı | `npm run check:locales` | `tr`/`ar`/`en` anahtar bazında eşit değilse. **Üçünde de eksik olan anahtarı yakalamaz** — o kapı backend'de, `docs/testing.md` § 3 |
| Arapça çoğul biçimleri | `npm run check:locales` | Arapça bir `plural` mesajı `one`/`two`/`few`/`many`/`other` biçimlerinden birini tanımlamıyorsa (§ 5) |
| Blok yorumu ayracı | `npm run check:comments` | Blok yorumunun içinde `**/` varsa — yorumu erken kapatır (§ 5) |
| Birim + smoke test | `npm run test` | Vitest + Testing Library |
| Production build | `npm run build` | Build başarısız olursa |

**Uyarı bırakılmaz.** Sessiz bırakılan uyarı, bir sonraki gerçek uyarıyı gizler.

### 8.1 Smoke test — Vitest + Testing Library

Her feature'ın **kritik akışı** için bileşen düzeyinde bir smoke testi yazılır: ekran gerçek
API client'ı sahtelenmiş hâlde monte edilir, **üç durumun üçü de** (yükleniyor / boş / hata) ve
mutlu yol doğrulanır.

- Test, kullanıcının gördüğü şeye göre sorgular: rol, erişilebilir ad, metin. CSS sınıfına veya
  bileşen iç yapısına göre sorgulayan test, yeniden düzenlemede kırılır ve hiçbir şey kanıtlamaz.
- Saf mantık (biçimlendirme, URL durumu, hesap) için ayrıca birim testi yazılır.
- **Bu, `docs/testing.md` § 3'teki uçtan uca smoke testin yerine geçmez.** Orası ürünü baştan
  sona ayakta tutan dört akışı kapsar; burası tek ekranın kendi içinde doğru davrandığını
  kapsar. İki farklı kapsam, iki ayrı kapı.

---

## 9. Süreç

- **Her arayüz dilimi bir mini-spec'e dayanır:** `specs/frontend-XXX-<kebab-ad>.md`,
  `specs/TEMPLATE.md`'den üretilir, ekran odaklı ve kısadır. Mini-spec olmadan ekran yapılmaz
  (`AGENTS.md` Altın Kural 1).
- Bir arayüz mini-spec'i en az şunları taşır:
  - hangi ekran / hangi rota,
  - hangi API çağrıları (**var olan** endpoint'ler — yoksa backend spec'i önce gelir),
  - kabul kriterleri: her satır tek başına **görsel olarak doğrulanabilir**,
  - üç durumun beklenen davranışı,
  - RTL ve çok dil beklentisi,
  - envanterden kullanılacak bileşenler; yeni bileşen gerekiyorsa gerekçesi,
  - kapsam dışı olanlar.
- **Görsel kanıt VERIFY'ın parçasıdır**, süs değil: her `[görsel]` kriter için ekran görüntüsü
  `docs/evidence/<spec-no>-ac<n>.png` altına konur; boş ve hata durumları dâhil, RTL kriteri
  varsa Arapça görüntüyle. Kanıtı olmayan kriter karşılanmamıştır.
- **Branch, commit ve PR kuralları `docs/git.md` ile aynıdır.** Frontend işinin ayrıcalığı yoktur.
