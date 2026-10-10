# 0017 — Content: Statik Sayfalar ve SSS

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Content` (yeni — bugün hiç yok) · `frontend/` public vitrin + panel |
| **Bağımlı spec'ler** | `0006` (roller: İçerik Editörü — tamamlandı) · `0014` (ana sayfa) bu spec'i **bekler** (bkz. `0014` S2, S4) |
| **Dayanak** | ADR 0003 Ek 1, sıra #11 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — beş açık sorunun cevabı
> onları belirliyor.

---

## Intent

Vitrinde **galerinin kendisi hakkında tek bir sayfa yok.** "Hakkımızda", "İletişim",
"KVKK aydınlatma metni", "Çerez politikası" — hiçbiri mevcut değil. Bunun iki somut sonucu
var ve ikisi de bugün ödeniyor:

- **Hukuki metin eksik.** `0005` ziyaretçiden ad, telefon ve e-posta topluyor ve iletişim
  izni kaydediyor (**L7**). Mockup'ın bülten bölümü *"KVKK aydınlatma metni"*ne bağlantı
  veriyor. **O metnin gideceği bir sayfa yok.**
- **`0014` iki yerde bu spec'e dayanıyor:** giriş bölümünün metni (S2) ve altlıktaki
  kurumsal bağlantılar (S4). `0014` bu yüzden kırpılmış bir ana sayfa teslim etmek zorunda.

**Galeri sahibi** kendi hikâyesini ve sıkça sorulan soruları kendi yazmak istiyor — her
değişiklik için bize gelmek zorunda kalmadan. `0006` zaten bunun için bir rol tanımlamış:
**İçerik Editörü.** O rol bugün hiçbir şey düzenleyemiyor.

Bu iş bittiğinde galeri, vitrindeki metin sayfalarını panelden yazıp dil başına yayınlar.
Başarı ölçütü: **bir hukuki metnin veya SSS girdisinin değişmesi bir dağıtım gerektirmiyor.**

---

## Requirements

- **R1.** İçerik Editörü veya Yönetici, bir **içerik sayfası** oluşturabilir, düzenleyebilir
  ve yayınlayabilir.
- **R2.** Bir içerik sayfası **locale başına** metin taşır: başlık, gövde ve **düzenlenebilir
  Latin slug**. → `docs/architecture.md` § 3.
- **R3.** Yayın durumu **locale başınadır**: bir sayfa Türkçe yayında olup Arapça taslakta
  kalabilir.
- **R4.** Bir dilde yayınlanmamış sayfa, **o dilde menüden ve altlıktan düşer**; boş sayfa
  veya boş bağlantı gösterilmez. → `docs/architecture.md` § 3 (CMS içeriği satırı).
- **R5.** Ziyaretçi, yayınlanmış bir sayfayı `/{locale}/{slug}` adresinde görür; yayınlanmamış
  veya olmayan bir slug **404** verir.
- **R6.** Sistem bir **SSS** bölümü tutar: locale başına soru–cevap girdileri, sıralı.
- **R7.** İçerik sayfaları ve SSS girdileri **silinmek** yerine yayından kaldırılabilir;
  silme ayrı ve açık bir eylemdir.
- **R8.** Panel, her sayfa için **hangi dillerde metnin eksik olduğunu** gösterir.
  → `0010b` R13 ile aynı desen.
- **R9.** Ziyaretçi tarafı sayfalar **yükleniyor**, **boş** ve **hata** durumlarını ele alır;
  panel ekranları ayrıca **doğrulama hatası** durumunu ele alır. → `docs/frontend.md` § 3.
- **R10.** Üç locale'de çalışır; Arapça'da RTL.

---

## Constraints

### Uyulması zorunlu

- **C1.** `Content`, içerik verisinin tek yazma sahibidir. → `docs/architecture.md` § 1.
- **C2.** **Merkezi bir `translations` tablosu kurulmaz** ve çeviriler tek modülde
  toplanmaz; içerik metni bu modülün **kendi** verisidir. → Yasak Listesi 21.
- **C3.** Veritabanında çevrilmiş durum adı saklanmaz; kod saklanır. → Yasak Listesi 22.
- **C4.** Yazma uçları **oturum gerektirir** ve İçerik Editörü ile Yöneticiye açıktır;
  okuma public'tir. → `0006` R7, R8.
- **C5.** Ziyaretçiye gösterilen içerik gövdesi, bu modülün **sahibi olduğu içeriktir** —
  Yasak Listesi 23'ün istisnası budur; ama **hata metinleri** yine `errorCode` ile gelir.
- **C6.** Locale öneki opsiyonel değildir; `Accept-Language`'a göre yönlendirme yapılmaz.
  → Yasak Listesi 28–29.
- **C7.** Slug kanonik Latin'dir ve locale başına düzenlenebilir. → `docs/architecture.md` § 3.
- **C8.** CSS'te fiziksel yön özellikleri kullanılmaz; renk ve tipografi `frontend-002`
  token katmanından gelir.
- **C9.** Yeni migration yalnızca `content` şemasına dokunur. → Yasak Listesi 3.
- **C10.** Her kabul kriteri en az bir testle karşılanır.

### KAPSAM DIŞI

- **X1.** **Blog** — *neden:* `docs/architecture.md` § 1 blogu `Content`'in sorumluluğunda
  sayıyor ama blog kendi listeleme, etiketleme, tarih arşivi ve SEO kararlarını getirir.
  → Açık soru **S1**.
- **X2.** **Kampanya alanları ve öne çıkan araç slotu** — *neden:* kampanya kendi domain
  kurallarını taşıyor (**K1–K5**: tarih aralığı, locale başına yayın, araca bağlanma,
  bağlı araç yayından kalkınca geçersizleşme) ve bu kurallar bir metin sayfasından farklı
  bir yaşam döngüsüdür. → Açık soru **S2**.
- **X3.** **Zengin metin editörü** (WYSIWYG, görsel yükleme, tablo) — *neden:* `Media`
  bağımlılığı ve güvenlik yüzeyi (HTML enjeksiyonu) getirir. → Açık soru **S3**.
- **X4.** **Sürüm geçmişi ve geri alma** — *neden:* `Audit` modülü yok; `docs/architecture.md`
  § 6 alan ayarları için aynı kararı vermişti (*"`Audit` kaydı yeterli"*).
- **X5.** **Çeviri onay iş akışı** — *neden:* § 6'da bilinçli kapsam dışı
  (*"2–3 kişilik ekipte sürtünme üretir; locale başına yayın durumu yeterli"*) — R3 zaten o.
- **X6.** **Makine çevirisi** — *neden:* § 6'da V1 dışı; `0010a` D5 ile aynı çizgi.
- **X7.** **Menü yapısının panelden düzenlenmesi** — *neden:* menü sıralaması bir yapı
  kararıdır; V1'de sayfa kümesi küçük ve sabittir.

---

## Context

### Mevcut davranış — doğrulandı

- **`Content` modülü yoktur.** `src/Modules/` altında `Dealership`, `Identity`, `Leads`,
  `Media`, `Notifications`, `VehicleCatalog`, `VehicleReference` var.
- **`0006` İçerik Editörü rolünü tanımladı** (`docs/architecture.md` § 1: Yönetici / Satış
  Danışmanı / İçerik Editörü) — rol var, düzenleyeceği içerik yok.
- **`docs/architecture.md` § 3 davranışı yazmış:** *"CMS içeriği | `Content` | Locale başına
  satır + **locale başına yayın durumu** | Yayınlanmamışsa o dilde menüden düşer"* — R4
  bunun doğrudan karşılığı.
- **Slug kararı verilmiş:** *"CMS sayfalarında locale başına düzenlenebilir Latin slug"*
  (§ 3). Araç slug'ından farklı: araçta her dilde **aynı** slug, CMS'te locale başına.
- **`0014` bu spec'i iki yerde bekliyor:** S2 (giriş metni çeviri kataloğundan geliyor,
  `Content` gelince taşınacak) ve S4 (altlık yalnızca var olan hedeflere bağlanıyor).
- **`0005` bir KVKK bağlantısı yayınladı.** Talep formu KVKK aydınlatmasına atıf yapıyor;
  o bağlantının bugün nereye gittiği `0014` S4'te de uyarı olarak işaretlendi ve
  **bu taslakta da doğrulanmadı** — kontrol edilmesi gerekiyor.

### Bilinen tuzaklar

1. **Yayınlanmamış dili boş sayfayla göstermek.** R4 menüden düşmeyi şart koşuyor; boş kutu
   göstermek `docs/domain.md` **K3**'ün kampanya için koyduğu kuralla aynı mantığı ihlal eder.
2. **Gövdeyi ham HTML olarak kabul etmek.** Panelden gelen HTML, ziyaretçi tarayıcısında
   çalışacak bir enjeksiyon yüzeyidir (S3).
3. **Slug çakışması.** İki sayfa aynı locale'de aynı slug'ı alırsa biri erişilemez olur;
   tekillik locale başına kurulmalı.
4. **Slug değişince eski adresi kırmak.** Yayınlanmış bir sayfanın slug'ı değiştiğinde eski
   bağlantı 404 olur — arama motoru ve paylaşılmış bağlantılar için bir kayıp. Yönlendirme
   bu taslakta ele alınmadı.
5. **Hukuki metni "sonra ekleriz" diye bırakmak.** `0005` kişisel veri topluyor; KVKK
   metninin olmaması bir eksiklik değil, bir **uyum sorunudur.** → Açık soru **S5**.

---

## Açık sorular

**S1. Blog V1'de var mı?**
*Öneri:* **yok.** İlk sürüm statik sayfalar + SSS.
*Gerekçe:* blog listeleme, etiket, tarih arşivi, yazar ve kendi SEO kurgusunu getirir; bu
spec'in asıl acil işi hukuki metin ve kurumsal sayfalar.
*Bedel:* içerik pazarlaması (SEO için blog) bir spec daha bekler.

**S2. Kampanya ve öne çıkan araç slotu bu spec'te mi?**
*Öneri:* **hayır, ayrı spec.**
*Gerekçe:* **K1–K5** kampanyayı *"duyuru, hesaplanan indirim değil"* diye tanımlıyor, tarih
aralığı veriyor, locale başına yayın istiyor ve bağlı araç yayından kalkınca otomatik
geçersizleşmeyi şart koşuyor — bu, bir metin sayfasının yaşam döngüsü değil. Aynı spec'te
ele almak kabul kriterlerini iki ayrı hakeme böler.
*Bedel:* `0014`'ün "öne çıkan araçlar" ölçütü (S3'te vitrin sıralama ağırlığı) uzun süre
dolaylı kalır; galeri elle seçemez.

**S3. İçerik gövdesinin biçimi ne?**
*Öneri:* **sınırlı bir işaretleme** (başlık, paragraf, liste, bağlantı, kalın/italik);
ham HTML kabul edilmez ve gövde ziyaretçiye basılırken arındırılır.
*Gerekçe:* Tuzak 2. Zengin editör `Media` bağımlılığı da getirir ve `Media` yeni geldi.
*Bedel:* galeri tablo veya gömülü video koyamaz; hukuki metin ve SSS için fazlasıyla yeterli.

**S4. Panelden düzenlenebilir sayfa kümesi sabit mi, galeri yeni sayfa açabilir mi?**
*Öneri:* galeri **yeni sayfa açabilir** (slug vererek), ama **menüde görünecek** kümeyi
belirleyemez (X7).
*Gerekçe:* hukuki metinler zamanla çoğalır (çerez, aydınlatma, açık rıza); sabit bir liste
her yeni metinde dağıtım ister. Menü yapısını serbest bırakmak ise ayrı bir iştir.
*Bedel:* menüde görünmeyen bir sayfa yalnızca doğrudan bağlantıyla erişilir.

**S5. KVKK ve Çerez metinleri `Content`'te mi, kodda sabit mi?**
*Öneri:* **`Content`'te.**
*Gerekçe:* bu metinler mevzuat ve galeri pratiğine göre değişir; kodda sabit olurlarsa her
değişiklik bir dağıtım ve bizim müdahalemiz demektir. `0005` zaten kişisel veri topluyor —
metnin güncel kalması galerinin sorumluluğu, bizim dağıtım takvimimiz değil.
*Bedel:* galeri metni yanlış veya eksik yazarsa uyum riski galeriye geçer; bunu bir
kurulum kontrol listesiyle karşılamak ayrı bir iş. **Ayrıca:** `0005`'in bugünkü KVKK
bağlantısının nereye gittiği kontrol edilmeli (Tuzak 5).

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> sayfa oluşturma/düzenleme/yayınlama · locale başına yayın durumu ve menüden düşme ·
> slug tekilliği ve geçersiz slug'da 404 · SSS girdileri ve sıralama · eksik dil göstergesi ·
> rol kısıtı (İçerik Editörü + Yönetici) · yayından kaldırma ve silmenin ayrılığı ·
> gövde arındırması · üç durum + panel doğrulama hatası · üç locale ve RTL.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0017-ac<n>.png`), boş ve hata durumları dâhil,
      Arapça görüntü de var.
- [ ] Gövde arındırmasının enjeksiyon girişimini kırdığı testle gösterildi (S3).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — merkezi `translations` tablosu kurulmadı (C2).
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `content` şemasına dokunuyor.
- [ ] `0005`'in KVKK bağlantısının hedefi kontrol edildi ve PR'da raporlandı (S5).
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi (arayüz metinleri; içerik gövdesi veri).
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | |
| **Düzeltme turu sayısı** | | |
| **Bulgu gerçek/gürültü oranı** | / | |
| **Regresyon sayısı** | | |
| **Kaçan hata** | | En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
