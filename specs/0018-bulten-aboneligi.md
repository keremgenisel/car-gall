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
- [ ] <!-- DoD:T1 --> Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] <!-- DoD:T2 --> Görsel kanıt eklendi (`docs/evidence/0018-ac<n>.png`), hata durumu dâhil, Arapça
      görüntü de var.
- [ ] Teyit edilmemiş adrese pazarlama gönderilmediği testle gösterildi (R2, S1).
- [ ] Çıkış bağlantısının **tek adımda** çalıştığı testle gösterildi (R5).
- [ ] Kişisel verinin logda ve gönderim geçmişinde bulunmadığı testle gösterildi (C5).
- [ ] `0009`'un kabul kriterleri regresyon olarak hâlâ yeşil (R10).
- [ ] Şablonlar TR/AR/EN üçünde de seed edildi; eksik şablon kontrolü yeşil (C6).
- [ ] <!-- DoD:T5 --> Yeni migration yalnızca `notifications` şemasına dokunuyor.
- [ ] <!-- DoD:T7 --> Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] <!-- DoD:T9 --> Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] <!-- DoD:T3 --> Backend testleri yazıldı ve yeşil (`docs/testing.md`).
- [ ] <!-- DoD:T4 --> Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] <!-- DoD:T6 --> Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] <!-- DoD:T8 --> Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] <!-- DoD:T10 --> **Dış gözden geçirme yapıldı:** **merge'den önce** bir QA oturumu diff + spec +
      testleri okudu ve bulgularını verdi (`docs/roles/qa.md`). Yazarın kendi triyajı
      bunun yerine geçmez.
- [ ] <!-- DoD:T11 --> PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] <!-- DoD:T12 --> Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] <!-- DoD:T13 --> **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

<!--
İş bittikten SONRA doldurulur. Amaç suçlu bulmak değil, sürecin nerede sızdırdığını görmek.
Dürüst doldurulmayan scorecard işe yaramaz — abartılmış iyi sayılar en pahalı yalandır.

Sayılar ancak TANIMLI oldukları sürece karşılaştırılabilir. Tanımsız bir sayı, spec'ten
spec'e farklı şey ölçer ve birikince eğilim değil gürültü üretir. Bu yüzden her ölçütün
neyi saydığı satırında yazılıdır; bir ölçüt belirsiz geldiyse sayıyı tahmin etme,
tanımın netleştirilmesini öner.
-->

| Ölçüt | Değer | Not |
|---|---|---|
| **Kriterler önce `main`'de miydi?** | evet / hayır | R/C/AC, **implementasyon commit'inden önce** `main`'de miydi (ADR 0008 / K2)? Tek komutla doğrulanır: `git cat-file -e <impl>^:specs/<ad>.md`. **`hayır` ise alttaki revizyon sayısı ÖLÇÜLEMEZ** ve `—` yazılır — düşük sayı iyi spec değil, karşılaştırılacak tabanı olmayan spec demektir. |
| **Kriter sayısı: önce → bugün** | → | O commit'in ebeveyninde kaç kabul kriteri vardı, bugün kaç? `git show <impl>^:specs/<ad>.md \| grep -c '^- \*\*AC-'`. İkisi arasındaki fark, revizyon sayısının söylemediği şeyi söyler: `0008` bu ölçümde **2 → 33** çıktı ve kartı revizyon **1** yazıyordu. |
| **Spec revizyon sayısı** | | Kriterler `main`'e girdikten **sonra** **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? Açıklayıcı not eklemek de sayılır: nete ihtiyaç duyan kural, eksik yazılmış kuraldır. Intent, Context, kapsam dışı gerekçeleri ve SCORECARD değişiklikleri **sayılmaz**. Yüksekse spec eksik yazılmıştır. **Onay anındaki CLARIFY turu SAYILIR** (ADR 0008 / K3) — ölçütün sorduğu şey "spec eksik mi yazılmıştı" ve kriter sayısının iki katına çıkması bunun en net cevabıdır. Üstteki satır `hayır` ise buraya `—` yazılır. |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? Yüksekse kriterler belirsizdir veya iş çok büyüktür. **`0` ile `—` aynı şey değildir:** `0`, gözden geçirildi ve hiç tur dönmedi demektir; gözden geçirme hiç yapılmadıysa `—` yazılır. İkisini aynı göstermek, ölçülmemiş işi temiz işmiş gibi gösterir. **`0` yalnız DoD'un dış gözden geçirme kutusu `[x]` ise yazılır; kutu açıksa veya madde yoksa `—`** (ADR 0008 / K3). Bu, "gözden geçirildi"yi var olan bir kutuya bağlar: kural eskiden kimin gözden geçirdiğini söylemiyordu ve üç kart (`0002`, `0007`, `frontend-002`) tam o boşlukta `0` yazdı. |
| **Dış bulgu (gerçek/gürültü)** | / | **Dış** gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu, kaçı gürültü? Yazarın kendi triyajı bu sütuna yazılmaz — kendi kodunu okuyan kişi kendi körlüğünü paylaşır. Dış gözden geçirme yapılmadıysa `— (dış gözden geçirme yok)`. **Ölçtüğü şey: spec ve kod ne kadar açık yazıldı.** |
| **Uygulama içi bulgu** | | Yazarın kendi işini yaparken bulduğu kusur sayısı — kendi testi kırmızı verdiği, kendi kanıtına baktığı veya kendi sondası yakaladığı için. Dış bulguyla aynı sütunda toplanmaz: ikisi **farklı şey** ölçer. **Ölçtüğü şey: yazar kendi işini ne kadar sorguladı.** Sıfır, "hiç hata yapmadım" değil "hiçbirini kendim bulmadım" demektir ve dış bulgu yüksekse bu bir uyarıdır. |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. `0009`'u bozup bozmadığı burada görünür. |
| **Kaçan hata** | | Merge'den **sonra** bulunan hata sayısı. En pahalı sütun budur. **Spec arşive taşınırken DEĞER hücresine `izleniyor` yazılır**; merge'den sonra bulunan her kusur bu satıra tarihiyle eklenir. Bu tek ölçüt DoD'nin şartı değildir — tanımı gereği merge anında bilinemez. **Değer hücresine yalnız `izleniyor` veya bir sayı girer; `0` ve BOŞ hücre yasak** (ADR 0008 / K3). Üçü de kullanıldı ve sütun üç dialekte bölündü; `frontend-002`'nin boş hücresi bir ortalamada da bir `grep`'te de sessizce kaybolur. |

**Bu spec'ten çıkarılan ders:**
