#!/usr/bin/env bash
# Kanıt ve performans ölçümlerinin üretildiği ortamı birebir kurar.
# Bu betik olmadan "ölçtüm" iddiası tekrarlanabilir değildir.
set -euo pipefail

SEED_COUNT="${1:-200}"
CONTAINER="cargall-dev"
PORT=55432

echo "==> PostgreSQL ($CONTAINER, port $PORT)"
docker rm -f "$CONTAINER" >/dev/null 2>&1 || true
docker run -d --name "$CONTAINER" \
  -e POSTGRES_PASSWORD=devonly -e POSTGRES_DB=cargallery \
  -p "$PORT:5432" postgres:17-alpine >/dev/null
docker exec "$CONTAINER" sh -c 'for i in $(seq 1 30); do pg_isready -U postgres -q && exit 0; sleep 1; done; exit 1'

echo "==> Frontend build (çıktı: src/Host/wwwroot)"
(cd frontend && npm ci --silent && npm run build --silent)

echo "==> Host ($SEED_COUNT ilanla seed edilerek)"
ConnectionStrings__VehicleCatalog="Host=localhost;Port=$PORT;Database=cargallery;Username=postgres;Password=devonly" \
Development__SeedShowcase=true \
Development__SeedListingCount="$SEED_COUNT" \
ASPNETCORE_URLS="http://localhost:5080" \
  dotnet run --project src/Host/Host.csproj --no-launch-profile -c Release
