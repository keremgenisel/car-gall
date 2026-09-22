# SCORECARD Analizi — 001

**Tarih:** 2026-09-22 · **Kapsam:** ilk altı tamamlanmış spec
(`0001`, `0002`, `0003`, `0005`, `0006`, `frontend-001`) · **Rol:** QA

`specs/done/README.md` periyodik okumayı tarif ediyor: *"Tek bir spec'in skoru gürültüdür;
yirmi spec'in skoru eğilimdir."* Altı spec eğilim için az, ama **yapısal bir sorunu**
görmeye yetiyor.

---

## Tablo

| Spec | Revizyon | Düzeltme turu | Gerçek/gürültü | Regresyon | Kaçan hata |
|---|---|---|---|---|---|
| `0001` Vitrin listesi | 1 | 1 | 12 / 0 | 1 | *(aşağıda)* |
| `frontend-001` Kabuk | 0 | 0 | — / — | 2 | 0 |
| `0002` Araç detayı | 1 | 0 | 3 / 0 | 1 | 0 |
| `0003` Galeri & şubeler | 1 | 0 | — / — | 1 | *(aşağıda)* |
| `0005` Talep | 2 | 2 | 4 / 0 | 1 | 0 |
| `0006` Kimlik | 1 | 2 | 11 / 0 | 1 | 0 |

---

## Bulgu 1 — Beş ölçütün üçü ölü

**"Kaçan hata" hiçbirinde doldurulamadı.** Altısı da `-` ve gerekçesi aynı:
*"Merge edilmedi"* / *"Henüz bilinmiyor"*. Sebep yapısal: SCORECARD, Definition of Done'ın
**merge'den önceki** maddesi olarak dolduruluyor, ama bu ölçüt tanımı gereği merge'den
**sonra** bilinebilir. Doldurulması mümkün olmayan bir alan ölçüt değildir.

**"Düzeltme turu" yarısında 0** — üçünde gerekçe aynı: *"PR açılmadı; gözden geçirme turu
olmadı."* Yani 0, "iş temizdi" demek değil, "ölçüm yapılmadı" demek. İki farklı şey aynı
sayıyla gösteriliyor.

**"Gerçek/gürültü oranı" ikisinde `— / —`**, dördünde **gürültü sıfır** (12/0, 3/0, 4/0,
11/0). Otuz bulgunun otuzu gerçek. Bu, gözden geçirmenin mükemmel olduğunu değil,
**dışarıdan gözden geçiren kimse olmadığını** gösteriyor: bulan her seferinde yazarın
kendisi. İki spec bunu açıkça yazmış: *"Dış gözden geçirme yapılmadı… bu sütunun yerine
geçmez."* Ölçüt öz-triyajı ölçüyor, gözden geçirmeyi değil.

---

## Bulgu 2 — Her spec bir şey kırdı, ve kırılmalar sınırda kümeleniyor

Regresyon sayısı: **1, 2, 1, 1, 1, 1.** İstisnasız. Daha önemlisi **nerede** çıktıkları:

- `0005`'in regresyonu **kendi dalında yoktu** — 219 backend ve 44 frontend testi yeşildi.
  `0006` ile birleşince çıktı: iki modül de `RateLimiterOptions.OnRejected` global kancasını
  kuruyordu, son kayıt diğerini bastırıyordu.
- `0006`'nın regresyonu da birleşimde çıktı: üç Host test fabrikası kurulum adımını
  çağırmıyordu.
- `0003`'ün regresyonu bir testin **tespitinin** fazla geniş olmasıydı — yine sınırda.

**Kendi dalında yeşil olmak, birleşimde yeşil olacağını göstermiyor.** Altı spec'te bu
üç kez tekrarlandı ve üçünde de dal `main`'den günlerce kopuktu.

---

## Bulgu 3 — Kaçan hata gerçekte SIFIR DEĞİL

Alanlar `-` yazıyor ama merge sonrası bulunan kusurlar var. En az ikisi somut:

**(a) `0003` — `MigrateDealershipAsync` hiçbir yerden çağrılmıyordu.**
`0003` merge edildikten sonra, `0006` üzerinde çalışılırken çıktı: temiz bir kurulumda
şubeli personel ekleme `42P01: relation "dealership.branches" does not exist` ile
düşüyordu. Metot vardı, çağrısı yoktu ve bunu söyleyen test yoktu.

**(b) `0001` — şemalar yalnız geliştirme bayrağıyla uygulanıyordu.**
ADR 0002 hazırlanırken görüldü: `main` üretime çıkarılamıyordu. Geliştirme bayrağı kapalı
bir dağıtımda hiçbir şema oluşmuyor, uygulama **sağlıklı görünerek** ayağa kalkıyor ve ilk
ziyaretçinin ilk isteğinde patlıyordu. Beş spec boyunca kimse fark etmedi.

İkisinin ortak deseni: **çağrılmayan kod ve koşulmayan yol, testin göremediği yerdir.**
Var olan her testi geçen bir sistem, hiç koşulmamış bir yola sahip olabilir.

---

## Öneriler

Kararı Takım Yöneticisi verir.

**Ö1 — "Kaçan hata" DoD'den çıkarılsın, arşivde açık kalsın.**
Alan merge'den önce doldurulamaz. Önerilen: spec `done/`'a taşınırken `izleniyor` yazılır;
merge sonrası bulunan her kusur o spec'in satırına **tarihiyle** eklenir. Ölçüt ancak
böyle bilgi taşır.
*Bedel:* Arşivlenmiş bir dosyaya sonradan dokunmak gerekir.

**Ö2 — "Düzeltme turu"nda `0` ile `ölçülmedi` ayrılsın.**
`0` yerine `—` yazılsın ve `0` yalnız gerçekten gözden geçirilip sıfır tur dönen işlere
saklansın. Bugün ikisi aynı görünüyor ve ortalama yanıltıyor.

**Ö3 — Gürültü oranı, dış gözden geçirme olmadan doldurulmasın.**
Dördü de sıfır gürültü gösteriyor ve bu sayı öz-triyajdan geliyor. Ya ayrı bir QA turu
zorunlu olsun, ya alan `— (dış gözden geçirme yok)` ile kapatılsın. Bugünkü hâli, var
olmayan bir kaliteyi ölçüyor gibi duruyor.

**Ö4 — Dal ömrü kısaltılsın.**
Üç regresyonun üçü de birleşimde çıktı ve üç dal da `main`'den uzun süre koptu
(`0005` + `0006` 17 saat, aynı anda üç açık PR). Önerilen: bir dal `main`'den **bir günden
fazla** koptuysa, PR açmadan önce `main`'e rebase edip CI'ı yeniden koşmak zorunlu olsun.
*Bedel:* Paralel çalışan oturumlar daha sık rebase yapar.

**Ö5 — "Çağrılmayan kod" için bir mimari test.**
Bulgu 3'ün iki örneği de aynı sınıftan: tanımlanmış ama hiçbir yerden çağrılmayan
başlangıç/kurulum yolu. `Architecture.Tests` bunu yakalayabilir — örneğin her
`IModuleSchema` kaydı olan modülün kurulum adımında gerçekten uygulandığını doğrulayan
bir test.
*Bedel:* Testin kendisi de fazla geniş tespit yaparsa `0003`'ün regresyonunu tekrarlar.
