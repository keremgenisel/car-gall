# 0008 — Personel Yönetimi Ekranı: Eylemler ve Çakışma Davranışı

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | Developer (ajan) |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `frontend/` personel yönetimi ekranı · `Identity` (yalnız var olan uçlar) |
| **Bağımlı spec'ler** | `0006` (kimlik, tamamlandı) · `0007` (eşzamanlılık — sunucu tarafı koruma ve hata sözleşmesi burada üretildi) |

---

## Intent

Galeri yöneticisi bugün panelde personel **ekleyebiliyor** ama işten ayrılan birini
**pasifleştiremiyor**, rolünü değiştiremiyor, parolasını sıfırlayamıyor. Liste salt
okunur. Bir personel ayrıldığında galeri yöneticisinin elinde hiçbir şey yok — hesabı
kapatmak için kimseyi arayacağı bir yer de yok.

Sunucu tarafı bu işlemleri **zaten yapabiliyor**: uçlar `0006`'da yazıldı, korumaları
`0007`'de eşzamanlılığa karşı sağlamlaştırıldı ve iki red türünün üç dildeki metni de
hazır. Eksik olan tek şey, o uçlara basacak ekran.

Bu iş bittiğinde galeri yöneticisi personel listesinden doğrudan pasifleştirme, rol
değiştirme ve parola sıfırlama yapabilir; bir işlem başka birinin aynı anda yaptığı
değişiklik yüzünden geçmediğinde ekran **ne olduğunu** söyler ve elindeki veriyi
kaybettirmez.

---

## Requirements

- **R1.** Yönetici, personel listesinden bir personeli **pasifleştirebilir**.
- **R2.** Yönetici, bir personelin **rolünü değiştirebilir**.
- **R3.** Yönetici, bir personelin **parolasını sıfırlayabilir**; yeni parola ona bir kez
  gösterilir ve personelden ilk girişinde değiştirmesi istenir (`0006` R10).
- **R4.** Geri alınamayan işlemler (pasifleştirme, parola sıfırlama) **onay ister**;
  onay ekranı ne olacağını açıkça yazar.
- **R5.** Bir işlem reddedildiğinde ekran, `0007`'nin **iki red türünü ayırt eder**:
  kural gereği reddin metni tekrar denemeyi önermez, eşzamanlılık kaybınınki önerir ve
  eylemi tekrar denenebilir bırakır.
- **R6.** Başarılı bir işlemden sonra liste **güncel hâliyle** görünür; ziyaretçi sayfayı
  elle yenilemek zorunda kalmaz.
- **R7.** Reddedilen bir işlemden sonra ziyaretçinin ekrandaki **seçimi ve konumu korunur**;
  liste başa sarmaz.
- **R8.** Satış Danışmanı ve İçerik Editörü bu eylemleri **görmez** (`0006` R9, AC-25/26).

---

## Constraints

### Uyulması zorunlu

- **C1.** Yeni sunucu ucu **yazılmaz**. `0006`'nın uçları yeterlidir; bu spec arayüz işidir.
  *(Eksik bir uç bulunursa bu spec durur ve önerilir — sessizce uç eklenmez.)*
- **C2.** Her ekran üç durumunu taşır: yükleniyor / boş / hata (`docs/frontend.md` § 3).
- **C3.** Ziyaretçiye teknik ayrıntı gösterilmez: durum kodu, `errorCode`, `traceId`,
  endpoint yolu, istisna metni (`docs/frontend.md` § 3.1).
- **C4.** Metin koda gömülmez; TR/AR/EN üçünde de anahtar bulunur (Yasak Listesi 24).
- **C5.** RTL: Arapça tercih eden personel için ekran `dir="rtl"` ile çalışır
  (Yasak Listesi 25 — fiziksel CSS yönü yok).
- **C6.** Her etkileşimli öğe klavyeyle (Enter/Space) çalışır (`docs/frontend.md` § 7).

### KAPSAM DIŞI

- **X1.** Personel **silme** — *neden:* `0006` D4 pasifleştirmeyi kalıcı iz bırakan tek
  yol olarak seçti; silme o kararı deler.
- **X2.** Toplu işlem (çoklu seçim) — *neden:* iki–üç kişilik bir ekipte tek tek yapılır;
  toplu işlem, onay ekranını da eşzamanlılık davranışını da katmerler.
- **X3.** Denetim kaydı ekranı ("kim ne zaman pasifleştirdi") — *neden:* `Audit` modülü
  henüz yok; kendi spec'ini hak ediyor.
- **X4.** Şube kapatma ekranı — *neden:* `0007` D2 komutu yazdı ama HTTP ucu açmadı;
  `Dealership`'in yazma yüzeyi kendi spec'ine ait (`0003` D1).

---

## Context

**Mevcut durum — doğrulandı.**

| Uç | Durum |
|---|---|
| `POST /api/admin/staff` | Var; ekranda **var** (`AddStaffForm`) |
| `POST /api/admin/staff/{id}/deactivate` | Var; ekranda **YOK** |
| `POST /api/admin/staff/{id}/role` | Var; ekranda **YOK** |
| `POST /api/admin/staff/{id}/password-reset` | Var; ekranda **YOK** |

`frontend/src/features/identity/StaffListPage.tsx` salt okunur bir tablo; yanında
`AddStaffForm` duruyor.

**`0007`'nin bıraktığı zemin.** Sunucu iki red türünü ayrı kodlarla döndürüyor ve ikisi
de `409`:

| Kod | Anlamı | Ekran ne demeli |
|---|---|---|
| `identity.last_active_manager` | Kural gereği red — **kalıcı** | Tekrar denemeyi önerme |
| `concurrency.lost` | Eşzamanlılık kaybı — **geçici** | Tekrar denemeyi öner |

Üçünün de TR/AR/EN karşılığı katalogda hazır (`0007`/AC-17); `messageKeyFor` noktalı
kodları anahtara çeviriyor.

**Bilinen tuzak.** `0007`/X5 bu ekranı kapsam dışı bırakırken şunu yazdı: kaybeden
istekten sonra listenin tazelenmesi ve seçimin korunması **kendi kriterlerini hak ediyor**.
Naif bir "hata aldık, listeyi baştan çek" davranışı, ziyaretçinin bulunduğu yeri ve
seçimini kaybettirir — ve bu, kuralın kendisinden daha sık karşılaşılan bir sorundur.

**İkinci tuzak.** Bir Yönetici **kendi hesabını** pasifleştirebilir veya rolünü
düşürebilir — son aktif Yönetici olmadığı sürece (`docs/domain.md` S1). Ekran bu durumu
onay metninde açıkça söylemeli: "bu işlemden sonra panele erişemeyeceksiniz."

---

## Acceptance Criteria

<!-- Uygulama planı onaylandığında doldurulur; her satır tek başına test edilebilir olmalı
     ve arayüz kriterlerinin görsel kanıtı gerekecek (docs/testing.md § 3). -->

- **AC-1.**
- **AC-2.**

---

## Definition of Done

- [ ] Tüm kabul kriterleri karşılandı; kriter → test eşlemesi PR'da tabloyla gösterildi.
- [ ] Arayüz kriterlerinin **görsel kanıtı** eklendi (`docs/evidence/0008-ac<n>.png`),
      boş ve hata durumları dâhil; RTL kriteri için Arapça görüntü de var.
- [ ] Backend testleri yazıldı ve yeşil (`docs/testing.md`) — **yeni uç yoksa "yok" yazılır.**
- [ ] Mimari testler yeşil — modül sınırı ihlali yok.
- [ ] Yeni migration varsa yalnızca kendi şemasına dokunuyor.
- [ ] Çeviri anahtarları TR/AR/EN üçünde de eklendi; eksik anahtar kontrolü yeşil.
- [ ] Yeni domain terimi varsa `docs/domain.md` sözlüğüne eklendi.
- [ ] Mimari karar verildiyse `docs/architecture.md` § Karar Defteri güncellendi.
- [ ] Pipeline tamamen yeşil (`docs/git.md` § 4).
- [ ] PR şablonu dolduruldu; karşılanmayan kriter varsa **açıkça** yazıldı.
- [ ] Takım Yöneticisi onayladı ve squash-merge edildi.
- [ ] **SCORECARD dolduruldu** ve spec `specs/done/` klasörüne taşındı.

---

## SCORECARD

| Ölçüt | Değer | Not |
|---|---|---|
| **Spec revizyon sayısı** | | Onaylandıktan sonra **Requirements, Constraints veya Acceptance Criteria** kaç kez değişti? |
| **Düzeltme turu sayısı** | | PR kaç kez geri döndü? |
| **Bulgu gerçek/gürültü oranı** | / | Gözden geçirmede çıkan bulgulardan kaçı gerçek sorundu? |
| **Regresyon sayısı** | | Bu iş yüzünden bozulan, daha önce çalışan davranış sayısı. |
| **Kaçan hata** | | Merge'den sonra bulunan hata sayısı. |

**Bu spec'ten çıkarılan ders:**
