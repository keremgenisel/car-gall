# 0019 — İzin Anındaki Aydınlatma Metninin Kaydı

| | |
|---|---|
| **Durum** | Taslak |
| **Sahip** | — |
| **Onaylayan** | Takım Yöneticisi |
| **Modül(ler)** | `Leads` · `Content` (yalnız `Content.Contracts` üzerinden okuma) |
| **Bağımlı spec'ler** | `0005` (talep ve izin — tamamlandı) · `0017` (KVKK sayfası — tamamlandı; bu kart onun **X8**'idir) |
| **Dayanak** | `0017` **X8** ve **D5** — "ayrı kart olarak önerildi" |
| **Karar bekliyor** | **S1–S3** — § Açık sorular (3 soru, **3 öneri yazılı**). Cevaplar Acceptance Criteria'yı belirliyor; gelmeden spec onaya sunulmaz. |

> **Bu bir taslaktır.** `0017`'nin kapanışında DoD'un "ayrı kart açıldı" maddesi için açıldı.
> Kabul kriterleri yazılmadı — üç açık sorunun cevabı onları belirliyor.

---

## Intent

Ziyaretçi talep bırakırken kişisel verilerinin işlenmesine izin veriyor ve `0017`'den beri
izin satırı KVKK aydınlatma metnine bağlanıyor. Ama galeri o metni panelden **değiştirebiliyor**
ve bugün bir talebin kaydında yalnız "izin verildi" bilgisi duruyor: hangi metne izin
verildiği değil. Bir şikâyette ya da denetimde galeri, ziyaretçinin izin verdiği anda
hangi metni gördüğünü gösteremiyor. Başarı: her talebin kaydından, izin anında yayında
olan aydınlatma metni geri okunabiliyor.

## Context

- Bugün izin `Lead.ConsentGiven` `bool`'udur (`0005`; `0017` AC-134 bunu korudu).
- KVKK metni `privacy` sistem sayfasıdır (`0017` **D6**); yayındaki dilde kayıt hemen yayına
  girer (`0017` R7), yani metin sürüm tutmadan değişir.
- `Content` bugün sayfanın **geçmişini tutmuyor**: yalnız güncel hâl ve bir `Version` sayacı var.
  Sayaç her kayıtta artar ama eski metin geri okunamaz.

## Açık sorular

- **S1. Kayıt neyi tutar?** Seçenekler: (a) yalnız sayfa sürüm numarası, (b) metnin özeti
  (hash), (c) metnin tam kopyası. *Öneri:* **(c)** — metnin kendisi talep kaydına değil,
  `Content`'te değişmez bir "yayınlanmış sürüm" satırına yazılır; talep o satırın kimliğini
  tutar. (a) geçmiş olmadan anlamsız, (b) metni geri vermez.
- **S2. Metin o dilde yayında değilken izin nasıl kaydedilir?** (`0017` D5: bağlantı yoksa
  izin satırı yine görünür.) *Öneri:* kimlik boş kalır ve bu bilinçli bir durumdur;
  "bağlantı yoktu" talep kaydından okunabilir.
- **S3. Eski talepler?** *Öneri:* geriye dönük doldurulmaz; kimlikleri boş kalır.
  Bugünkü metnin o anda gösterildiği kanıtlanamaz.

## Requirements · Constraints · Acceptance Criteria

*S1–S3 karara bağlandıktan sonra yazılacak.*

## Kapsam dışı

- Çerez onayı ve çerez politikasının sürümü — çerez onayı bugün hiç yok.
