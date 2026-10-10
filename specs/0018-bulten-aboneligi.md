# 0018 — Bülten Aboneliği

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Notifications` · `frontend/` public vitrin |
| **Bağımlı spec'ler** | `0009` (bildirim altyapısı — tamamlandı; **X2**'yi geri açar) · `0012` (yayın olayları) · `0017` (KVKK metni) |
| **Dayanak** | ADR 0003 Ek 1, sıra #13 |
| **Karar bekliyor** | **S1–S5** — § Açık sorular (5 soru, **5 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir Analist taslağıdır.** Kabul kriterleri yazılmadı — beş açık sorunun cevabı
> onları belirliyor.

---

## Intent

Vitrine gelen ziyaretçinin çoğu **o gün almıyor.** Araç almak haftalar süren bir karardır ve
ziyaretçi bu süre boyunca birden çok galeriye bakar. Bugün o ziyaretçiyi geri getirecek
hiçbir yol yok: ya kendi aklına gelip adresi yeniden yazacak ya da başka bir galeride
karar verecek.

Galerinin elinde ise düzenli bir haber var: **stoğa yeni araç giriyor.** Bu haber bugün
yalnızca vitrine bakanlara ulaşıyor.

**Galeri** ilgilenen ziyaretçiye yeni araçları haber vermek istiyor; **ziyaretçi** her gün
site kontrol etmek yerine haberi kendisine gelmesini istiyor — ve **tek tıkla** bu listeden
çıkabilmek istiyor.

Bu iş bittiğinde ziyaretçi e-posta adresini bırakıp onaylar, yeni araçların özetini alır ve
istediği an tek bağlantıyla çıkar. Başarı ölçütü: **onay vermemiş hiç kimseye pazarlama
mesajı gitmez ve çıkmak isteyen tek adımda çıkar.**

---

## Requirements

- **R1.** Ziyaretçi e-posta adresini bırakarak bültene abone olmak isteyebilir.
- **R2.** Abonelik, ziyaretçi **adresini teyit etmeden** etkinleşmez. → Açık soru **S1**.
- **R3.** Abonelik kaydı, ziyaretçinin **hangi dilde** abone olduğunu taşır; bülten o dilde
  gider. → `docs/domain.md` **L1** ile aynı ilke.
- **R4.** Abone olmadan önce ziyaretçiye **ne alacağı** ve **nasıl çıkacağı** söylenir;
  KVKK aydınlatma metnine bağlantı verilir. → `0017`.
- **R5.** Her pazarlama mesajı **abonelikten çıkma bağlantısı** taşır; bağlantı **tek
  adımda** çalışır ve çıkmak için giriş veya onay istemez.
- **R6.** Abonelikten çıkan adrese bir daha pazarlama mesajı gönderilmez; çıkış kaydı
  **silinmez**, çıkmış olarak işaretlenir.
- **R7.** Aynı adres ikinci kez abone olmaya çalıştığında yeni bir kayıt oluşmaz ve
  ziyaretçiye adresin durumu **ifşa edilmez**. → Açık soru **S4**.
- **R8.** Bülten, son gönderimden sonra **yayına alınan** araçların özetini taşır. Hangi
  araçların "yeni" sayıldığı tanımlıdır. → Açık soru **S3**.
- **R9.** Gönderilecek yeni araç yoksa **bülten gönderilmez**; boş bülten çıkmaz.
- **R10.** Bülten gönderimi `0009`'un **kuyruğunu ve tekrar deneme** davranışını kullanır;
  yeni bir gönderim mekanizması kurulmaz.
- **R11.** Abonelik formu **yükleniyor**, **başarılı**, **doğrulama hatası** ve **hata**
  durumlarını ele alır. → `docs/frontend.md` § 3.
- **R12.** Form ve bülten şablonu üç locale'de çalışır; Arapça'da RTL.
- **R13.** Aynı kaynaktan kısa sürede gelen aşırı sayıda abonelik isteği reddedilir.
  → `0005` R12 deseni.

---

## Constraints

### Uyulması zorunlu

- **C1.** Dışarıya giden her mesaj **yalnızca `Notifications`** üzerinden gider.
  → Yasak Listesi 17.
- **C2.** **İletişim izni ve abonelikten çıkma `Notifications`'ın verisidir.**
  → `docs/architecture.md` § 1 (`Notifications` sorumluluk satırı).
- **C3.** İzni olmayana **pazarlama** mesajı gönderilmez; işlemsel mesaj bundan ayrıdır.
  → `docs/domain.md` **L7**.
- **C4.** **Talep izni ile bülten izni ayrı izinlerdir.** `0005`'in topladığı
  `ConsentGiven`, bülten aboneliği **değildir**. → Açık soru **S5**.
- **C5.** Kişisel veri ne loga ne gönderim geçmişine yazılır; adres gönderim anında
  kaynaktan okunur. → `0009` C5.
- **C6.** Şablonlar **(anahtar × locale)** olarak tutulur ve seed migration ile gelir;
  istenen dilde şablon yoksa varsayılana düşülür. → `0009` R6, C13.
- **C7.** Tüketim outbox üzerinden ve **idempotent**'tir; aynı olay iki kez gelirse ikinci
  mesaj gönderilmez. → `0009` C3.
- **C8.** Kullanıcıya görünen metin koda gömülmez; API `errorCode` döner.
  → Altın Kural 6.
- **C9.** Zaman damgaları UTC. → Yasak Listesi 30.
- **C10.** Yeni migration yalnızca `notifications` şemasına dokunur. → Yasak Listesi 3.
- **C11.** Her kabul kriteri en az bir testle karşılanır.

### KAPSAM DIŞI

- **X1.** **Segmentasyon** ("yalnızca SUV ilgilenenlere gönder") — *neden:* ilgi alanı
  verisi toplanmıyor ve toplamak ayrı bir izin ve veri kararı.
- **X2.** **Açılma ve tıklama takibi** — *neden:* takip pikseli ve bağlantı sarmalama, KVKK
  açısından ayrı bir aydınlatma gerektirir; `docs/architecture.md` § 6 analitiği üçüncü
  partiye bırakmıştı.
- **X3.** **Fiyat düşüşü ve favori aracı için bildirim** — *neden:* favori `localStorage`'da
  (§ 6) ve sunucu kimin neyi favorilediğini bilmiyor; sıra #12 ile de çelişir.
- **X4.** **Panelden bülten içeriği düzenleme / kampanya metni ekleme** — *neden:* kampanya
  `Content` tarafının işi (`0017` § X2) ve bülten içeriği bu spec'te **otomatik** üretilir.
- **X5.** **SMS ve WhatsApp ile bülten** — *neden:* `0009` § X1 kanalları kapsam dışı
  bıraktı; ayrıca pazarlama SMS'i ayrı bir izin sınıfı.
- **X6.** **Abone listesinin panelden görüntülenmesi ve dışa aktarılması** — *neden:*
  kişisel veri listesini bir ekrana koymak kendi yetki ve KVKK kararını gerektirir.

---

## Context

### Mevcut davranış — doğrulandı

- **Bu spec'i `0009` kendisi tetikledi.** `0009` § X2: *"Abonelikten çıkma (unsubscribe)
  akışı ve bağlantısı — yalnız işlemsel mesaj gönderildiği sürece gerekli değil; **pazarlama
  mesajı girdiği gün zorunlu hâle gelir ve o zaman kendi spec'ini hak eder.**"* Bu spec o gün.
- **`0009` § X2'nin ikinci yarısı da önemli:** *"Sonucu: bu spec'te pazarlama şablonu
  yoktur."* Yani `Notifications` bugün yalnızca işlemsel mesaj gönderiyor ve
  "pazarlama mesajı gönderilmez" kriteri `0009`'da **doğrulanacak bir şey olmadığı için**
  yazılmamıştı. Bu spec pazarlama sınıfını **ilk kez** var ediyor; **L7**'nin ayrımı
  ilk kez gerçek bir fark yaratacak.
- **Altyapı hazır:** `0009` tamamlandı — kuyruk, arka plan servisi, tekrar deneme, hata
  sınıfları, şablon (anahtar × locale) ve varsayılan dile düşme mevcut. R10 bunları
  **kullanıyor**, yeniden kurmuyor.
- **`Notifications` izni ve abonelikten çıkmayı sahiplenmiş:** `docs/architecture.md` § 1
  bu modülün verisi olarak *"iletişim izni & abonelikten çıkma"*yı sayıyor — C2'nin kaynağı.
- **Mockup ne vaat ediyor:** *"Yeni gelen araçları ilk siz görün. E-posta adresinizi bırakın;
  galeriye giren her yeni aracı **haftada bir** özetle gönderelim."* ve altında
  *"**Tek tıkla** çıkabilirsiniz. Adresiniz üçüncü kişilerle paylaşılmaz — KVKK aydınlatma
  metni."* Üç vaat: haftalık sıklık, tek tıkla çıkış, üçüncü kişiyle paylaşmama.
- **"Yeni araç" olayı henüz yok.** `docs/architecture.md` § 1 `VehiclePublished` olayını
  sayıyor ama kodda yok; `0012` § S5 bu olayı **üretmeyi öneriyor**. Yani bu spec `0012`'ye
  bağlı. → Açık soru **S3**.
- **KVKK metni yok.** R4'ün bağlantı verdiği sayfa `0017`'nin işi; `0017` § S5 bu metni
  `Content`'e koymayı öneriyor.

### Bilinen tuzaklar

1. **Teyitsiz aboneliğe gönderim.** Birinin başkasının adresini yazması, o adrese istenmeyen
   pazarlama gitmesi demektir — ve o kişi için bu spam'dir (S1).
2. **Çıkış bağlantısını onaya bağlamak.** "Çıkmak istediğinizden emin misiniz?" ekranı
   R5'in "tek adımda" vaadini bozar ve mockup'ın yazılı vaadiyle çelişir.
3. **Abone adresini gönderim geçmişine yazmak.** `0009` C5 bunu yasakladı; bülten de aynı
   yoldan geçtiği için aynı kural geçerli.
4. **Talep iznini bülten izni sanmak.** `0005` iletişim izni topluyor; o izin **talebin
   kendisine** dairdir. Bülten için ayrı izin gerekir (C4, S5).
5. **Boş bülten göndermek.** Yeni araç girmemiş bir haftada "bu hafta yeni araç yok"
   e-postası, abonelikten çıkmanın en hızlı yoludur (R9).
6. **Adresin kayıtlı olup olmadığını ifşa etmek.** "Bu adres zaten abone" mesajı, üçüncü
   bir kişinin bir adresin abone olup olmadığını öğrenmesine yol açar (R7, S4).

---

## Açık sorular

**S1. Çift onay (teyitli abonelik) olacak mı?**
*Öneri:* **evet.** Adres bırakıldığında bir teyit mesajı gider; bağlantıya tıklanmadan
abonelik etkinleşmez.
*Gerekçe:* Tuzak 1. Ayrıca **L7** izni bir **kayıt** olarak istiyor; teyit edilmemiş adres,
izni kanıtlanamayan bir adrestir.
*Bedel:* abone sayısı belirgin biçimde düşer (teyit oranı hiçbir zaman %100 değildir) ve
akış bir adım uzar. Bu bilinçli: izni kanıtlanamayan abone, galeriyi riske atar.

**S2. Gönderim sıklığını kim belirler?**
Mockup "haftada bir" diyor.
*Öneri:* sıklık **dağıtım yapılandırmasından** gelir, varsayılanı haftalık; panelden
düzenlenmez.
*Gerekçe:* `0009` D4 ve `frontend-002` D1'in deseni — operasyonel değerler yapılandırmada.
Panelden sıklık değiştirmek, yanlışlıkla günlük gönderim riskidir.
*Bedel:* galeri sıklığı kendi değiştiremez.

**S3. "Yeni araç" nasıl belirlenir?**
*Öneri:* son başarılı gönderimden sonra **yayına alınmış** ilanlar; kaynak `0012`'nin
üreteceği yayın olayı.
*Gerekçe:* "yayına alınma" galerinin kasıtlı eylemidir; araç kaydının oluşturulma tarihi
değil (taslak haftalar önce girilmiş olabilir).
*Bedel:* **bu spec `0012`'ye bağımlı hâle gelir.** `0012` olayı üretmezse bülten içeriği
sorgu ile hesaplanmak zorunda kalır ve "son gönderimden beri" penceresi kırılgan olur.
*Elenen alternatif:* ilanın `PublishedAt` alanını sorgulamak — `Listing` bu alanı taşıyor,
yani olaysız da mümkün; ama o zaman bülten `VehicleCatalog`'un okuma modeline senkron
bağımlı olur ve `Notifications`'ın yaprak konumu tartışmaya açılır.

**S4. Zaten abone olan adres ne görür?**
*Öneri:* **her durumda aynı mesaj:** "adresinizi aldık, teyit bağlantısını gönderdik."
Zaten aboneyse yeni bir teyit gönderilmez.
*Gerekçe:* Tuzak 6; `0002` C13 ve `0006` AC-4'ün "ayırt edilemez cevap" ilkesinin bu
spec'teki karşılığı.
*Bedel:* gerçekten yeni abone olan biri teyit gelmediğinde nedenini anlamaz; mesaj
"gelmezse spam klasörüne bakın" demek zorunda.

**S5. `0005`'in iletişim izni bülten için kullanılabilir mi?**
*Öneri:* **hayır.** İki izin ayrı tutulur (C4).
*Gerekçe:* **L7** izni *"talebin kendisine dair işlemsel bildirim"* için tanımlıyor ve
pazarlamayı açıkça ayırıyor. Talep bırakan birini bültene eklemek, vermediği bir izni
kullanmaktır.
*Bedel:* galeri, talep bırakan ziyaretçilere bülten gönderemez — ticari olarak cazip ama
izinsiz. Talep formuna **ayrı bir bülten onay kutusu** eklemek meşru bir yoldur, ama o
`0005`'i değiştirir (ADR 0003 unsur 1 gereği yeni bir spec) ve bu spec'in kapsamında değil.

---

## Acceptance Criteria

> Beş açık soru cevaplandıktan sonra yazılacak. En az şu kümeleri kapsayacak:
> abonelik isteği ve teyit akışı · teyitsiz adrese gönderim yapılmaması · tek adımda çıkış ·
> çıkmış adrese bir daha gönderilmemesi · zaten abone olan adresin ayırt edilemezliği ·
> locale başına gönderim ve şablon düşüşü · boş bülten gönderilmemesi · kuyruk ve tekrar
> deneme davranışının korunması · idempotentlik · kişisel verinin logda ve geçmişte
> bulunmaması · hız sınırı · form üç durumu · üç locale ve RTL.

---

## Definition of Done

- [ ] Açık sorular karara bağlandı ve Context'e **Verilen kararlar** olarak işlendi.
- [ ] `0012` ve `0017` ile olan bağımlılıklar çözüldü (S3, R4).
- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Görsel kanıt eklendi (`docs/evidence/0018-ac<n>.png`), hata durumu dâhil, Arapça
      görüntü de var.
- [ ] Teyit edilmemiş adrese pazarlama gönderilmediği testle gösterildi (R2, S1).
- [ ] Çıkış bağlantısının **tek adımda** çalıştığı testle gösterildi (R5).
- [ ] Kişisel verinin logda ve gönderim geçmişinde bulunmadığı testle gösterildi (C5).
- [ ] `0009`'un kabul kriterleri regresyon olarak hâlâ yeşil (R10).
- [ ] Şablonlar TR/AR/EN üçünde de seed edildi; eksik şablon kontrolü yeşil (C6).
- [ ] Yeni migration yalnızca `notifications` şemasına dokunuyor.
- [ ] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | |
| **Düzeltme turu sayısı** | | |
| **Bulgu gerçek/gürültü oranı** | / | |
| **Regresyon sayısı** | | `0009`'u bozup bozmadığı burada görünür. |
| **Kaçan hata** | | En pahalı sütun budur. |

**Bu spec'ten çıkarılan ders:**
