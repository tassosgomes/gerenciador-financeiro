#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
FRONTEND_DIR="$ROOT_DIR/frontend"

if [[ -f "$ROOT_DIR/.env" ]]; then
  set -a
  # shellcheck disable=SC1091
  source "$ROOT_DIR/.env"
  set +a
fi

API_PORT="${API_PORT:-5156}"
FRONTEND_PORT="${FRONTEND_PORT:-5173}"

export VITE_API_URL="${VITE_API_URL:-http://localhost:${API_PORT}}"

echo "Iniciando frontend em debug (http://localhost:${FRONTEND_PORT})..."
cd "$FRONTEND_DIR"
npm run dev -- --host localhost --port "$FRONTEND_PORT"
