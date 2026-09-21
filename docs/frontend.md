# Frontend Konvansiyonları — İSKELET

> **Bu dosya bilinçli olarak iskelettir.** Yalnızca ilk dilimlerin ihtiyaç duyduğu, ihlal
> edilirse sonradan pahalıya mal olacak kuralları içerir.
>
> **Dilim 1.8'de profesyonel standarda genişletilecek:** tasarım token'ları, bileşen
> envanteri, erişilebilirlik ölçütleri, performans bütçesi ve kalite kapıları.
> O zamana kadar buraya yeni kural eklemek serbesttir; **var olan kuralı gevşetmek
> öneri kuralına tabidir** (`AGENTS.md` Altın Kural 2).

---

## 1. Stack ve Yapı

**React + Vite + TypeScript.** Tek uygulama, iki rota ağacı:
`/{locale}/…` public vitrin, `/{locale}/admin/…` yönetim paneli (lazy-load).

```
frontend/
  src/
    api/            → TEK API client dosyası ve modül başına çağrı grupları. (§ 2)
    app/            → Rota ağacı, sağlayıcılar (provider), uygulama kabuğu.
    features/       → Modül başına ekran ve bileşenler. Backend modülleriyle AYNI isimler.
      catalog/  leads/  dealership/  content/  identity/
    components/     → Özellikten bağımsız paylaşılan bileşenler.
    lib/            → Saf yardımcılar (biçimlendirme, hesap). Yan etkisiz, test edilebilir.
    locales/        → tr.json · ar.json · en.json
  public/
```

- `features/` altındaki klasör adları **backend modül adlarıyla aynıdır.** Bir ekranın hangi
  modüle ait olduğu klasöründen okunur.
- Bir feature başka bir feature'ın iç bileşenini import etmez. Paylaşılan şey
  `components/` veya `lib/` altına çıkar.
- TypeScript `strict` açık. `any` yasaktır; bilinmeyen için `unknown` + daraltma.

---

## 2. API Çağrıları — Tek Client

**Tüm ağ istekleri `src/api/` altındaki tek client'tan geçer.** Bileşenin içinde
`fetch`/`axios` çağrısı yasaktır.

Client'ın tek sorumluluğu ve tek yerde çözdüğü şeyler:
- taban URL ve kimlik başlığı,
- `Accept-Language` başlığının aktif locale ile gönderilmesi,
- HTTP hatalarının **tek tip bir hata nesnesine** dönüştürülmesi (`errorCode` + parametreler),
- `traceId`'nin yakalanması (kullanıcıya gösterilmez, destek için saklanır),
- iptal (abort) ve zaman aşımı,
- yanıt tiplerinin tek yerde tanımlanması.

Kurallar:
- Endpoint yolları **yalnızca** `src/api/` içinde yazılır. Bileşende URL dizesi bulunmaz.
- Backend DTO tipleri `src/api/types.ts` altında tanımlanır; bileşenler bu tipleri kullanır.
- Client **ham hata mesajı döndürmez**; `errorCode` döndürür. Çeviri uçta yapılır (§ 4).

---

## 3. Her Ekranda Üç Durum — Zorunlu

Veri çeken **her** ekran ve her veri bölgesi şu üç durumu **açıkça** ele alır:

| Durum | Kural |
|---|---|
| **Yükleniyor** | Boş ekran veya ani sıçrama yok. İskelet (skeleton) tercih edilir; en azından bir gösterge. |
| **Boş** | "Sonuç yok" yeterli değil. Neden boş ve kullanıcı ne yapabilir söylenir (filtre temizle, araç ekle). |
| **Hata** | Kullanıcıya anlamlı bir mesaj + **tekrar dene** eylemi. Teknik detay yok. |

- Üçü de ele alınmamış ekran **tamamlanmış sayılmaz** ve kabul kriteri karşılanmamıştır.
- Boş ve hata durumlarının **görsel kanıtı** PR'a eklenir (`docs/testing.md` § 3).
- Kısmi hata (sayfanın bir bölümü yüklenemedi) tüm sayfayı düşürmez; o bölge kendi hata
  durumunu gösterir.

---

## 4. Hata Metni — Kullanıcıya Teknik Detay Sızmaz

**Yasak:** HTTP durum kodu, `errorCode`'un kendisi, stack trace, SQL, endpoint yolu,
sunucu istisna metni, `traceId` — hiçbiri kullanıcıya gösterilmez.

Akış:
1. API `errorCode` döner (`vehicle.cannot_publish.no_media`).
2. Uç, bu kodu çeviri katalogundan **kullanıcı diline** çevirir.
3. Katalogda karşılığı **yoksa** genel bir mesaj gösterilir
   ("Bir sorun oluştu, lütfen tekrar deneyin.") ve kod **konsola/loga** yazılır.
4. `traceId` destek için saklanır; istenirse "destek kodu" olarak kopyalanabilir biçimde
   sunulur — hata metninin içine gömülmez.

Kullanıcıya gösterilen her hata mesajı **ne olduğunu ve ne yapılabileceğini** söyler.

---

## 5. Metin ve Çok Dillilik

- **Kullanıcıya görünen hiçbir metin bileşene gömülmez.** İstisna yok — placeholder,
  `aria-label`, `title`, boş durum metni, buton etiketi dâhil. Lint bunu kırar.
- Çeviri katalogları: `src/locales/{tr,ar,en}.json`. Üçü **anahtar bazında eşittir**;
  CI eksik anahtarı kırar (`docs/testing.md` § 3).
- **ICU mesaj formatı zorunludur.** Dize birleştirmeyle cümle kurulmaz —
  Arapça'da altı çoğul biçimi vardır ve `count > 1` mantığı yanlıştır.
- Tarih, sayı ve para biçimlendirmesi **uçta**, aktif locale ile yapılır.
  Sunucu ham sayı + para birimi kodu döner (`docs/conventions.md` § 7).
- Arapça'da **Latin rakam varsayılan**, yapılandırılabilir.
- Locale URL yol önekinden gelir (`/tr`, `/ar`, `/en`). Geçersiz önek → 404.
  `Accept-Language` ile **otomatik yönlendirme yapılmaz**.

---

## 6. RTL

- Yön locale'in özelliğidir; `<html lang dir>` sunucudan basılı gelir, uçta yeniden hesaplanmaz.
- **CSS'te fiziksel yön özellikleri yasaktır** — lint kırar:
  `left`, `right`, `margin-left`, `margin-right`, `padding-left`, `padding-right`,
  `text-align: left|right`, `border-left|right`, `float: left|right`.
  Yerine **logical properties**: `inset-inline-start`, `margin-inline-start`,
  `padding-inline-end`, `text-align: start|end`, `border-inline-start`.
- Yön duyarlı ikonlar (ok, geri, ileri, "sonraki araç") mantıksal olarak aynalanır.
  **Aynalanmaz:** marka logoları, medya oynatma kontrolleri, saat, grafik eksenleri.
- Karışık yönlü içerik (Arapça metin içinde Latin model adı veya rakam) için `dir="auto"`
  veya `<bdi>` kullanılır.
- **Admin paneli de tam RTL.** Yarı-RTL bir panel bozuk görünür.

---

## 7. Dilimler Mini-Spec'le Koşar

Her arayüz dilimi bir **mini-spec**e dayanır (`specs/TEMPLATE.md`'den üretilir, ekran
odaklı ve kısa). Mini-spec olmadan ekran yapılmaz (`AGENTS.md` Altın Kural 1).

Bir arayüz mini-spec'i en az şunları taşır:
- hangi ekran / hangi rota,
- hangi API çağrıları (var olan endpoint'ler — yoksa backend spec'i önce gelir),
- kabul kriterleri: her satır tek başına **görsel olarak doğrulanabilir**,
- üç durumun (yükleniyor / boş / hata) beklenen davranışı,
- RTL ve çok dil beklentisi,
- kapsam dışı olanlar.

Dilim tamamlandığında: her kritere **görsel kanıt** (`docs/testing.md` § 3), lint + build
yeşil, PR şablonu dolu (`docs/git.md` § 5).

---

## 8. Şimdilik Yeterli Olan Asgari Kurallar

Bunlar 1.8'de genişletilecek; şimdilik tabanı tutar:

- **Erişilebilirlik tabanı:** her etkileşimli öğe klavyeyle erişilebilir ve odak halkası
  görünür; her görselin `alt` metni vardır (medya `alt`'ı locale başına, backend'den);
  form alanlarının `<label>`'ı vardır; renk tek başına anlam taşımaz.
- **Görsel:** araç görselleri `loading="lazy"`, açık `width`/`height` ile (CLS önlemi),
  responsive `srcset` ile. Kapak görseli hariç.
- **Durum yönetimi:** sunucu verisi için sorgu/cache katmanı; global store'a sunucu verisi
  kopyalanmaz. İstemci durumu (form, filtre) yerel tutulur.
- **Rota:** filtre ve sayfalama **URL'de** yaşar (paylaşılabilir, geri tuşu çalışır).
- **Mobil öncelikli.** Tasarım önce dar ekranda doğrulanır.
- **`console.log` üretime gitmez.** Lint kırar.
