#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cleanup() {
  if [[ -n "${BACKEND_PID:-}" ]] && kill -0 "$BACKEND_PID" >/dev/null 2>&1; then
    kill "$BACKEND_PID" >/dev/null 2>&1 || true
  fi

  if [[ -n "${FRONTEND_PID:-}" ]] && kill -0 "$FRONTEND_PID" >/dev/null 2>&1; then
    kill "$FRONTEND_PID" >/dev/null 2>&1 || true
  fi
}

trap cleanup INT TERM EXIT

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
