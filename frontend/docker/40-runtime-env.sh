#!/bin/sh
set -eu

if [ -z "${API_URL:-}" ]; then
  echo "ERROR: API_URL environment variable is required to generate runtime-env.js" >&2
  exit 1
fi

export API_URL
export OTEL_ENDPOINT="${OTEL_ENDPOINT:-}"

envsubst '${API_URL} ${OTEL_ENDPOINT}' \
  < /usr/share/nginx/html/runtime-env.template.js \
  > /usr/share/nginx/html/runtime-env.js

echo "runtime-env.js generated successfully"
