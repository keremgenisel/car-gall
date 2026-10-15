# car-gall

Tek bir galeri için araç vitrini ve yönetim paneli. Her galeri **kendi kurulumunu** alır:
ayrı uygulama, ayrı veritabanı, kendi teması ve kendi alan adı (white-label).

> **Bu depo açık kaynak DEĞİL.** Herkes görüntüleyebilir; kullanma, değiştirme ve dağıtma
> hakkı verilmemiştir. Ayrıntı: [`LICENSE`](LICENSE).

## Ne yapıyor

**Ziyaretçi tarafı:** ilan listesi ve kasa tipine göre daraltma, araç detayı, galeri
profili ve şubeler, talep (lead) oluşturma — üç dilde (`tr`, `ar`, `en`) ve Arapça için
tam RTL.

**Personel tarafı:** oturum ve roller, personel yönetimi, araç kaydı ve düzenleme, araç
listesi, fotoğraf yükleme (sunucuda türev üretimi + filigran + EXIF temizliği), yayın ve
durum eylemleri, toplu eylem ve öne çıkarma.

## Mimari

Modüler monolit: tek uygulama, modül başına ayrı PostgreSQL **şeması** ve modüller arası
iletişim yalnızca `*.Contracts` + outbox üzerinden olay.

| Modül | Ne tutar |
|---|---|
| `VehicleCatalog` | Araç ve ilan agregatları, vitrin okuma modeli |
| `Identity` | Personel kimliği, oturum, roller |
| `Dealership` | Galeri profili, şubeler |
| `Media` | Yükleme, türev üretimi, filigran, depolama |
| `Leads` | Talepler |
| `Notifications` | Sistemden dışarı giden her mesajın tek kapısı |
| `VehicleReference` | Marka/model/donanım referans verisi |

**Teknoloji:** .NET 10 (minimal API), EF Core, PostgreSQL · React + Vite + TypeScript ·
xUnit + Testcontainers, Vitest, Playwright.

Kararların **neden** öyle olduğu [`docs/decisions/`](docs/decisions/) altındaki ADR'lerde;
**ne** olduğu [`docs/architecture.md`](docs/architecture.md) § Karar Defteri'nde.

## Geliştirme ortamı

Docker ve .NET 10 SDK gerekir.

```bash
./scripts/dev-up.sh 200     # 200 ilanla seed eder
```

Betik PostgreSQL, Mailpit ve S3 (LocalStack) konteynerlerini kurar, şemayı **tek seferlik
kurulum adımıyla** uygular (çalışan uygulama migration uygulamaz — ADR `0002`), sonra
Host'u ayağa kaldırır:

| Yüzey | Adres |
|---|---|
| Uygulama | `http://localhost:5080` |
| Mailpit (gönderilen e-postalar) | `http://localhost:58025` |

Kurulum hesabının parolası **koşu başına üretilir** ve betik onu ekrana yazar; depoda
sabit bir kurulum parolası yoktur (ADR `0011`). E2E koşmak için betiğin bastığı `export`
satırını kullanın.

## Kapılar

Her kuralın bir komutu vardır; komutu olmayan kapı kapı değildir.

```bash
dotnet test                 # xUnit + mimari testler + migration şema kontrolü
cd frontend && npm run lint && npm run check:locales && npm run check:comments
cd frontend && npm run test && npm run test:e2e
```

Mimari testler modül sınırı ihlallerini, migration'ların yalnız kendi şemasına
dokunduğunu, spec arşivinin eksiksizliğini ve bu depoda tekrarlamış belirli kusurları
kırar. Tam liste: [`docs/testing.md`](docs/testing.md) ve
[`docs/frontend.md`](docs/frontend.md) § 8.

## Süreç

Bu depo kodla birlikte **nasıl çalıştığını** da taşıyor:

- [`AGENTS.md`](AGENTS.md) — altın kurallar ve oturum açılışı
- [`specs/`](specs/) — iş kartları; biten kartlar `specs/done/` altında SCORECARD'ıyla
- [`docs/roles/`](docs/roles/) — Analist · Developer · QA (rol bir unvan değil, kısıt kümesi)
- [`docs/git.md`](docs/git.md) — dal, commit ve PR şartları

Kart bittiğinde SCORECARD'ı doldurulur: kriterler önce `main`'de miydi, kaç revizyon,
kaç düzeltme turu, kaç dış bulgu, kaç regresyon, kaçan hata. Abartılmış iyi sayılar en
pahalı yalandır.
