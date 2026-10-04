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
ASPNETCORE_URLS="http://localhost:5080" \
  dotnet run --project src/Host/Host.csproj --no-launch-profile -c Release
