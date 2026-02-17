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

API_PORT="${API_PORT:-5156}"
FRONTEND_PORT="${FRONTEND_PORT:-5173}"

kill_port_processes() {
  local port="$1"
  local pids
  pids="$(lsof -t -i:"$port" 2>/dev/null || true)"

  if [[ -z "$pids" ]]; then
    echo "Nenhum processo encontrado na porta $port."
    return
  fi

  echo "Encerrando processos na porta $port: $pids"
  kill $pids >/dev/null 2>&1 || true
}

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

kill_port_processes "$API_PORT"
kill_port_processes "$FRONTEND_PORT"

echo "Stop-all concluído."