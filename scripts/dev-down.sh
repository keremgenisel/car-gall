#!/usr/bin/env bash
set -euo pipefail
pkill -f "CarGallery.Host" 2>/dev/null || true
docker rm -f cargall-dev >/dev/null 2>&1 || true
echo "Host durduruldu, veritabanı kabı kaldırıldı."
