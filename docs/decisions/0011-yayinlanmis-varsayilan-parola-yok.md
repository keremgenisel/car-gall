# 0011 — Depoda yayınlanmış varsayılan parola yok

- **Durum:** Kabul edildi
- **Tarih:** 2026-10-15
- **Bağlam:** Depo **public** yapılıyor (Takım Yöneticisi kararı 2026-10-15). Bu, ADR
  `0010` / **K6'nın tersidir**; o ADR henüz `main`'de değil (açık PR #68) ve kararın
  tersine döndüğü kaydı oraya, kendi metninin içine yazılacak — bu ADR onu yeniden
  yazmıyor. Burada yazılan şey, public kararının **güvenlik ön koşulu**.

## Bağlam

Depo herkese açık hâle geliyor. Yayınlamanın **gerçekten yeni bir risk üretip
üretmediği** ölçüldü, varsayılmadı:

| Ölçüm | Sonuç |
|---|---|
| İzlenen dosyalarda `.env`, `.pem`, `.key`, credential dosyası | **yok** |
| Parola/anahtar deseni taraması (çalışma ağacı) | **0 eşleşme** |
| **Tüm geçmiş** (287 commit, 4363 blob) aynı taramayla | **0 eşleşme**; hiç sır dosyası eklenmemiş |
| `src/Host/appsettings.json` | yalnız `localhost` varsayılanları; seed parolası **dosyada değil** |
| Uygulamanın başlangıç kontrolü | **boş** parolayı reddediyor (`IdentityModule`, D2/AC-53) |

Yani sır sızıntısı yok. Buna rağmen **bir açık** bulundu: uygulama boş parolayı
reddediyordu ama **depoda sabit yazılı geliştirme parolalarını** reddetmiyordu. Üç dize
vardı ve public olduktan sonra hepsi kamuya açık olurdu:

- `gelistirme-parolasi` — `scripts/dev-up.sh` (iki yerde) ve `frontend/e2e/global-setup.ts`
- `ci-kurulum-parolasi` — `.github/workflows/ci.yml`
- `perf-kurulum-parolasi` — `.github/workflows/perf.yml`

Somut senaryo: biri `scripts/dev-up.sh`'ı bir sunucuda çalıştırır ve **parolası kamuya
açık** bir Yönetici hesabı kurar. D2/AC-53'ün tam engellemek istediği şey buydu —
"bilinen parolalı bir Yönetici hesabı" — ve kural boş parola için yazıldığı için bu hâli
kaçırıyordu.

## Kararlar

### K1 — Depoda SABİT kurulum parolası bulunmaz; değer koşuyu başlatandan gelir

- `scripts/dev-up.sh` parolayı **koşu başına üretir** (24 karakter) ve ekrana yazar —
  geliştirici panele girmek için ona ihtiyaç duyuyor. `SEED_ADMIN_PASSWORD` dışarıdan
  verilmişse ona saygı duyar.
- `.github/workflows/ci.yml` ve `perf.yml` işin ilk adımında `openssl rand -hex 16` ile
  üretir, `::add-mask::` ile kayıtlarda maskeler ve `$GITHUB_ENV` üzerinden paylaşır.
  **Tek kaynak kuralı korunuyor:** Host'un kurulum hesabı ile E2E'nin `globalSetup`'ı
  aynı değeri okumak zorunda; ikisi ayrı yazıldığında `globalSetup` 401 alıyor ve bütün
  paket düşüyor.
- `frontend/e2e/global-setup.ts`'in varsayılanı **kaldırıldı**: değer yoksa yüksek sesle
  düşüyor. Sessiz bir varsayılanla 401 almak, sebebi gizliyordu.

### K2 — Üç eski dize KOŞULSUZ reddedilir

`IdentityModule.AddIdentity` artık bu üç parolayla başlamıyor. Reddin **kalıcı** olmasının
sebebi git geçmişi: dizeler depodan silinse de geçmişte sonsuza kadar duruyor, dolayısıyla
"bilinen parola" olmaktan çıkmıyorlar. Liste kısalmaz, yalnızca uzar.

### K3 — Koşul **ortama bağlı değil**

Alternatif, standart .NET yolu olurdu: "üretimde reddet, `Development`'ta izin ver".
Elendi ve gerekçesi ölçüm:

- `src` ağacında **ortam kavramı hiç yok** — `IHostEnvironment`, `IsDevelopment`,
  `EnvironmentName` taraması **sıfır** sonuç. Güvenlik kuralını, kod tabanında hiç
  kullanılmayan bir kavrama bağlamak onu görünmez kılardı.
- CI `dotnet run --no-launch-profile` ile koşuyor, yani ortam **`Production`**. Kuralı
  ortama bağlamak, CI'ı "`Development`" yazmaya zorlardı ve o satır, güvenlik kapısını
  CI'da sessizce kapatan bir anahtar olurdu.

Ortama bağlı bir güvenlik kapısı, yanlış ortamda sessizce açılır. Koşulsuz ret, bedelini
peşin ödüyor: bu üç dizeyi bir daha hiç kimse kullanamaz.

### K4 — Yeni bir sabit parolanın doğmasını bir kapı engeller

`tests/Architecture.Tests/YayinlanmisParolaTests.cs`, `scripts/*.sh`,
`.github/workflows/*.yml`, `frontend/e2e/**/*.ts` ve `src/**/appsettings*.json` içinde
kurulum parolası atamalarını tarıyor ve değerin bir **değişkenden** geldiğini şart
koşuyor (`$VAR`, `${...}`, `$(...)`, `${{ env.* }}`, `process.env.*`). Sabit bir dize
kırmızı veriyor.

`src/**/*.cs` bilerek kapsam dışı: reddedilen eski dizelerin listesi `IdentityModule`'de
yaşıyor ve kapı kendi savunmasını ihlal saymamalı.

## Elenen alternatifler

| Alternatif | Neden elendi |
|---|---|
| **Ortama bağlı ret** (`Production`'da reddet) | K3: kod tabanında ortam kavramı yok ve CI zaten `Production`; kuralı CI'da kapatan bir anahtar üretirdi |
| **Dizeleri geçmişten silmek** (`filter-repo`) | 287 commit'in tamamını yeniden yazmak, her açık PR'ı ve her klonu kırar. Kazanç yok: parolayı reddetmek, varlığını silmekten daha güçlü |
| **Parola uzunluğu/entropi kuralı** | `gelistirme-parolasi` 19 karakter ve mevcut `MinimumPasswordLength: 12` eşiğini geçiyor; uzunluk bu kusuru göremez |
| **Sadece dokümana yazmak** | Bu depoda üç karttır ölçülen ders: notla durmayan kusuru yalnız kapı durduruyor |
| **GitHub Actions secret'ı kullanmak** | Public depoda fork'tan gelen PR'lar secret okuyamaz; CI'ın her PR'da koşması gerekiyor. Üretilen değer bu kısıttan tamamen bağımsız |

## Sonuç

Depo public yapıldığında yayınlanacak hiçbir sabit kurulum parolası kalmıyor; geçmişte
kalanlar ise uygulama tarafından reddediliyor. Geliştirici deneyiminin bedeli bir satır:
`scripts/dev-up.sh` parolayı ekrana yazıyor ve e2e için `export` satırını da basıyor.
