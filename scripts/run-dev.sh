#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
API_PROJECT="$ROOT_DIR/Backend/SantanderHnApi.Api/SantanderHnApi.Api.csproj"
CLIENT_PROJECT="$ROOT_DIR/Frontend/SantanderHnApi.Client/SantanderHnApi.Client.csproj"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI not found. Please install .NET 10 SDK and try again." >&2
  exit 1
fi

echo "Starting backend (API)..."
dotnet watch --project "$API_PROJECT" run &
API_PID=$!

echo "Starting frontend (Blazor WebAssembly)..."
dotnet watch --project "$CLIENT_PROJECT" run &
CLIENT_PID=$!

cleanup() {
  echo "Stopping dev servers..."
  kill "$API_PID" "$CLIENT_PID" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

wait "$API_PID" "$CLIENT_PID"
