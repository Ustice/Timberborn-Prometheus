#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
TEST_PROJECT="$ROOT_DIR/tests/Prometheus.Tests/Prometheus.Tests.csproj"
RESULTS_DIR="$ROOT_DIR/TestResults"

echo "[test] Running Prometheus plain C# regression tests..."
dotnet test "$TEST_PROJECT" \
  --configuration Release \
  --results-directory "$RESULTS_DIR" \
  --logger "trx;LogFileName=Prometheus.Tests.trx" \
  --collect:"XPlat Code Coverage"
