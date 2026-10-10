#!/usr/bin/env bash
# Kanıt ve performans ölçümlerinin üretildiği ortamı birebir kurar.
# Bu betik olmadan "ölçtüm" iddiası tekrarlanabilir değildir.
set -euo pipefail

SEED_COUNT="${1:-200}"
CONTAINER="cargall-dev"
PORT=55432
MAIL_CONTAINER="cargall-dev-mail"
MAIL_SMTP_PORT=51025
MAIL_UI_PORT=58025
S3_CONTAINER="cargall-dev-s3"
S3_PORT=54566
S3_BUCKET="cargallery-media"
S3_WATERMARK_KEY="private/brand/watermark.png"

echo "==> PostgreSQL ($CONTAINER, port $PORT)"
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" \
  -e POSTGRES_PASSWORD=devonly -e POSTGRES_DB=cargallery \
  -p "$PORT:5432" postgres:17-alpine >/dev/null
docker exec "$CONTAINER" sh -c 'for i in $(seq 1 30); do pg_isready -U postgres -q && exit 0; sleep 1; done; exit 1'

# 0009 — bildirimler gerçekten bir SMTP sunucusuna gidiyor. Mailpit olmadan kuyruk her
# turda geçici hatayla dönerdi ve "e-posta gitti" iddiası hiçbir yerden doğrulanamazdı.
# Gönderilenler: http://localhost:$MAIL_UI_PORT
echo "==> Mailpit ($MAIL_CONTAINER, SMTP $MAIL_SMTP_PORT, arayüz $MAIL_UI_PORT)"
docker rm -f "$MAIL_CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$MAIL_CONTAINER" \
  -e MP_SMTP_AUTH_ACCEPT_ANY=true -e MP_SMTP_AUTH_ALLOW_INSECURE=true \
  -p "$MAIL_SMTP_PORT:1025" -p "$MAIL_UI_PORT:8025" axllent/mailpit:v1.21 >/dev/null

# 0011 — medya hattı gerçekten bir S3 uyumlu depoya yazıyor. Depo olmadan her yükleme
# başarısız olur ve "türev üretildi" iddiası hiçbir yerden doğrulanamaz.
#
# MinIO DEĞİL ve bunun ölçülmüş bir nedeni var: `minio/minio` imajı anonim olarak
# çekilemiyor (`pull access denied ... repository does not exist`). Aynı makinede
# `alpine:3.20` sorunsuz indi, yani Docker Hub erişimi sorunu değil. C3'ün ÜRETİM kararı
# değişmedi (S3-uyumlu, R2/MinIO); değişen yalnız yerel ikizdir.
echo "==> LocalStack S3 ($S3_CONTAINER, port $S3_PORT)"
docker rm -f "$S3_CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$S3_CONTAINER" \
  -e SERVICES=s3 -e DEBUG=0 \
  -p "$S3_PORT:4566" localstack/localstack:3.8.1 >/dev/null
for i in $(seq 1 60); do
  curl -sf "http://localhost:$S3_PORT/_localstack/health" >/dev/null 2>&1 && break
  sleep 1
done

docker exec "$S3_CONTAINER" awslocal s3 mb "s3://$S3_BUCKET" >/dev/null 2>&1 || true

# Filigran kaynağı yapılandırmadaki bir NESNE ANAHTARIDIR (D4), bir medya varlığı kimliği
# değil — kimlik olsaydı kurulum döngüsü doğardı: yapılandırma zorunlu → uygulama kalkmaz
# → logo yüklenemez → kimlik yok.
#
# Bu PNG burada ÜRETİLİYOR, repoda durmuyor: repoda duran bir filigran, galerinin logosu
# yerine sessizce kullanılabilecek bir varsayılan olurdu. Geliştirme filigranı yarı
# saydam beyaz bir şerittir; galerinin logosu kurulumda operatör tarafından konur.
python3 - "$PWD/.dev-watermark.png" <<'PYEOF'
import struct, sys, zlib

genislik, yukseklik = 240, 80
satirlar = b"".join(
    b"\x00" + bytes([255, 255, 255, 180]) * genislik for _ in range(yukseklik))


def parca(tur, govde):
    return (struct.pack(">I", len(govde)) + tur + govde
            + struct.pack(">I", zlib.crc32(tur + govde) & 0xFFFFFFFF))


png = (b"\x89PNG\r\n\x1a\n"
       + parca(b"IHDR", struct.pack(">IIBBBBB", genislik, yukseklik, 8, 6, 0, 0, 0))
       + parca(b"IDAT", zlib.compress(satirlar, 9))
       + parca(b"IEND", b""))

with open(sys.argv[1], "wb") as dosya:
    dosya.write(png)
PYEOF

docker cp .dev-watermark.png "$S3_CONTAINER:/tmp/watermark.png" >/dev/null
docker exec "$S3_CONTAINER" awslocal s3 cp /tmp/watermark.png \
  "s3://$S3_BUCKET/$S3_WATERMARK_KEY" >/dev/null
rm -f .dev-watermark.png
echo "    filigran yüklendi: s3://$S3_BUCKET/$S3_WATERMARK_KEY"

echo "==> Frontend build (çıktı: src/Host/wwwroot)"
(cd frontend && npm ci --silent && npm run build --silent)

# Migration'ı ÇALIŞAN uygulama değil, bu tek seferlik kurulum adımı uygular
# (docs/decisions/0002). Uygulama yalnız doğrular ve şema eksikse hiç başlamaz.
# Kurulum adımı da uygulamayı KURDUĞU için Identity yapılandırmasını ister; parola
# olmadan DI kaydı hata verir ve kurulum adımı hiç koşamaz (D2/AC-53).
echo "==> Kurulum adımı (şema)"
ConnectionStrings__VehicleCatalog="Host=localhost;Port=$PORT;Database=cargallery;Username=postgres;Password=devonly" \
Identity__SeedAdmin__Password="${SEED_ADMIN_PASSWORD:-gelistirme-parolasi}" \
Identity__PasswordIterations="${SEED_ADMIN_ITERATIONS:-1000}" \
Notifications__Smtp__Host=localhost \
Notifications__Smtp__Port="$MAIL_SMTP_PORT" \
Notifications__Smtp__User=gelistirme \
Notifications__Smtp__Password=gelistirme \
Media__Storage__ServiceUrl="http://localhost:$S3_PORT" \
Media__Storage__Bucket="$S3_BUCKET" \
Media__Storage__AccessKey=test \
Media__Storage__SecretKey=test \
Media__Storage__Region=us-east-1 \
Media__PublicBaseUrl="http://localhost:$S3_PORT/$S3_BUCKET/" \
Media__WatermarkObjectKey="$S3_WATERMARK_KEY" \
Media__AllowedImageFormats__0=jpeg \
Media__AllowedImageFormats__1=png \
Media__AllowedImageFormats__2=webp \
  dotnet run --project src/Host/Host.csproj --no-launch-profile -c Release -- --migrate

# D2/AC-53 — başlangıç Yönetici parolası YAPILANDIRMADAN gelir ve repoda durmaz
# (docs/conventions.md § 11). Yoksa uygulama hiç başlamaz.
# Parola özetleme maliyeti geliştirmede düşük tutulur: her giriş için 210.000
# iterasyon beklemek kanıt üretimini yavaşlatır. Üretim değeri appsettings.json'dadır.
echo "==> Host ($SEED_COUNT ilanla seed edilerek)"
ConnectionStrings__VehicleCatalog="Host=localhost;Port=$PORT;Database=cargallery;Username=postgres;Password=devonly" \
Development__SeedShowcase=true \
Development__SeedListingCount="$SEED_COUNT" \
Identity__SeedAdmin__Password="${SEED_ADMIN_PASSWORD:-gelistirme-parolasi}" \
Identity__PasswordIterations="${SEED_ADMIN_ITERATIONS:-1000}" \
Identity__LoginRequestLimit="${LOGIN_REQUEST_LIMIT:-500}" \
Leads__RateLimit__CapturedLeadLimit="${LEAD_CAPTURED_LIMIT:-500}" \
Leads__RateLimit__RequestLimit="${LEAD_REQUEST_LIMIT:-500}" \
Notifications__PublicBaseUrl="http://localhost:5080" \
Notifications__Smtp__Host=localhost \
Notifications__Smtp__Port="$MAIL_SMTP_PORT" \
Notifications__Smtp__User=gelistirme \
Notifications__Smtp__Password=gelistirme \
Media__Storage__ServiceUrl="http://localhost:$S3_PORT" \
Media__Storage__Bucket="$S3_BUCKET" \
Media__Storage__AccessKey=test \
Media__Storage__SecretKey=test \
Media__Storage__Region=us-east-1 \
Media__PublicBaseUrl="http://localhost:$S3_PORT/$S3_BUCKET/" \
Media__WatermarkObjectKey="$S3_WATERMARK_KEY" \
Media__AllowedImageFormats__0=jpeg \
Media__AllowedImageFormats__1=png \
Media__AllowedImageFormats__2=webp \
ASPNETCORE_URLS="http://localhost:5080" \
  dotnet run --project src/Host/Host.csproj --no-launch-profile -c Release
