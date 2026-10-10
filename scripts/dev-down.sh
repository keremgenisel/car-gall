#!/usr/bin/env bash
set -euo pipefail
pkill -f "CarGallery.Host" 2>/dev/null || true
docker rm -f cargall-dev >/dev/null 2>&1 || true
# 0011 — medya deposu. `cargall-dev-mail` de buraya 0011 ile eklendi: `dev-up.sh` onu
# 0009'da açmaya başlamış ama bu betiğe hiç girmemiş, yani her turda ayakta kalıyordu.
docker rm -f cargall-dev-s3 >/dev/null 2>&1 || true
docker rm -f cargall-dev-mail >/dev/null 2>&1 || true
echo "Host durduruldu; veritabanı, posta ve medya kapları kaldırıldı."
