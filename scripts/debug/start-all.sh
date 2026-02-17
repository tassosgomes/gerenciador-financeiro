#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/../.." && pwd)"

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
    return
  fi

  echo "Encerrando processos na porta $port: $pids"
  kill $pids >/dev/null 2>&1 || true
}

cleanup() {
  if [[ -n "${BACKEND_PID:-}" ]] && kill -0 "$BACKEND_PID" >/dev/null 2>&1; then
    kill "$BACKEND_PID" >/dev/null 2>&1 || true
  fi

  if [[ -n "${FRONTEND_PID:-}" ]] && kill -0 "$FRONTEND_PID" >/dev/null 2>&1; then
    kill "$FRONTEND_PID" >/dev/null 2>&1 || true
  fi

  kill_port_processes "$API_PORT"
  kill_port_processes "$FRONTEND_PORT"
}

trap cleanup INT TERM EXIT

kill_port_processes "$API_PORT"
kill_port_processes "$FRONTEND_PORT"

"$SCRIPT_DIR/start-db.sh"

"$SCRIPT_DIR/start-backend.sh" &
BACKEND_PID=$!

"$SCRIPT_DIR/start-frontend.sh" &
FRONTEND_PID=$!

echo "Debug ativo: backend e frontend iniciados. Pressione Ctrl+C para encerrar ambos."

set +e
wait -n "$BACKEND_PID" "$FRONTEND_PID"
EXIT_CODE=$?
set -e

exit "$EXIT_CODE"
