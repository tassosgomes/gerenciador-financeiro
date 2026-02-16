#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
API_PROJECT="$ROOT_DIR/backend/1-Services/GestorFinanceiro.Financeiro.API/GestorFinanceiro.Financeiro.API.csproj"

if [[ -f "$ROOT_DIR/.env" ]]; then
  set -a
  # shellcheck disable=SC1091
  source "$ROOT_DIR/.env"
  set +a
fi

POSTGRES_DB="${POSTGRES_DB:-gestorfinanceiro_dev}"
POSTGRES_USER="${POSTGRES_USER:-postgres}"
POSTGRES_PASSWORD="${POSTGRES_PASSWORD:-postgres}"
DB_PORT="${DB_PORT:-5432}"
API_PORT="${API_PORT:-5156}"
FRONTEND_PORT="${FRONTEND_PORT:-5173}"

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS="http://localhost:${API_PORT}"
export ConnectionStrings__DefaultConnection="Host=localhost;Port=${DB_PORT};Database=${POSTGRES_DB};Username=${POSTGRES_USER};Password=${POSTGRES_PASSWORD}"
export JwtSettings__SecretKey="${JWT_SECRET:-DEV_ONLY_32_PLUS_CHARACTERS_SECRET_KEY_2026_NOT_FOR_PRODUCTION}"
export JwtSettings__Issuer="${JWT_ISSUER:-GestorFinanceiro}"
export JwtSettings__Audience="${JWT_AUDIENCE:-GestorFinanceiro}"
export JwtSettings__AccessTokenExpirationMinutes="${JWT_ACCESS_TOKEN_EXPIRATION_MINUTES:-1440}"
export JwtSettings__RefreshTokenExpirationDays="${JWT_REFRESH_TOKEN_EXPIRATION_DAYS:-7}"
export AdminSeed__Name="${ADMIN_NAME:-Administrador}"
export AdminSeed__Email="${ADMIN_EMAIL:-admin@gestorfinanceiro.local}"
export AdminSeed__Password="${ADMIN_PASSWORD:-mudar123}"
export Cors__AllowedOrigins__0="${FRONTEND_ORIGIN:-http://localhost:${FRONTEND_PORT}}"

echo "Iniciando backend em debug (http://localhost:${API_PORT})..."
dotnet watch --project "$API_PROJECT" run --no-launch-profile
