#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILES=("-f" "$ROOT_DIR/docker-compose.yml" "-f" "$ROOT_DIR/docker-compose.debug.yml")

if docker compose version >/dev/null 2>&1; then
  COMPOSE_CMD=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE_CMD=(docker-compose)
else
  echo "Erro: Docker Compose não encontrado." >&2
  exit 1
fi

echo "Parando banco de dados (db)..."
(cd "$ROOT_DIR" && "${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" stop db)
echo "Banco parado."
