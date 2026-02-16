#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
COMPOSE_FILES=("-f" "$ROOT_DIR/docker-compose.yml" "-f" "$ROOT_DIR/docker-compose.debug.yml")

if [[ -f "$ROOT_DIR/.env" ]]; then
  set -a
  # shellcheck disable=SC1091
  source "$ROOT_DIR/.env"
  set +a
fi

POSTGRES_DB="${POSTGRES_DB:-gestorfinanceiro_dev}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"

if docker compose version >/dev/null 2>&1; then
  COMPOSE_CMD=(docker compose)
elif command -v docker-compose >/dev/null 2>&1; then
  COMPOSE_CMD=(docker-compose)
else
  echo "Erro: Docker Compose não encontrado." >&2
  exit 1
fi

echo "Subindo banco de dados (db)..."
(
  cd "$ROOT_DIR"
  POSTGRES_DB="$POSTGRES_DB" POSTGRES_USER="$POSTGRES_USER" POSTGRES_PASSWORD="$POSTGRES_PASSWORD" \
    "${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" up -d db
)

echo "Aguardando PostgreSQL ficar pronto..."
for _ in {1..40}; do
  if (
    cd "$ROOT_DIR"
    "${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" exec -T db pg_isready -U "$POSTGRES_USER" -d postgres >/dev/null 2>&1
  ); then
    break
  fi
  sleep 1
done

echo "Garantindo que o database '$POSTGRES_DB' exista..."
(
  cd "$ROOT_DIR"
  "${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" exec -T db psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 \
    -c "SELECT 1 FROM pg_database WHERE datname = '$POSTGRES_DB';" \
    | grep -q 1 || "${COMPOSE_CMD[@]}" "${COMPOSE_FILES[@]}" exec -T db psql -U "$POSTGRES_USER" -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE \"$POSTGRES_DB\";"
)

echo "Banco pronto em localhost:5432 (database: $POSTGRES_DB)."
