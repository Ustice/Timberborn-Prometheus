#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_NAME="Prometheus"
LAUNCH_AFTER_DEPLOY=false
RUN_TESTS_BEFORE_DEPLOY=true
TEST_ONLY=false
STOP_RUNNING_BEFORE_DEPLOY=false
WAIT_FOR_BUILD=false
WAIT_FOR_BUILD_TIMEOUT_SECONDS=10
WAIT_FOR_BUILD_POLL_SECONDS=2
WAIT_FOR_BUILD_STABLE_POLLS=2
DEFAULT_TIMBERBORN_APP_ID="1062090"
BUILD_CONFIGURATION="${PROMETHEUS_BUILD_CONFIGURATION:-Debug}"
DEFAULT_BUILD_PROJECT_DIR="$ROOT_DIR"
if [[ -d "$ROOT_DIR/../timberborn-modding" ]]; then
  DEFAULT_BUILD_PROJECT_DIR="$(cd "$ROOT_DIR/../timberborn-modding" && pwd)"
fi
BUILD_PROJECT_DIR="${PROMETHEUS_BUILD_PROJECT_DIR:-$DEFAULT_BUILD_PROJECT_DIR}"
SRC_MOD_DIR="$ROOT_DIR/Assets/Mods/$MOD_NAME"
PROJECT_CSPROJ="$BUILD_PROJECT_DIR/Timberborn.ModExamples.Prometheus.csproj"
SRC_DLL="$BUILD_PROJECT_DIR/Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll"
SRC_PDB="$BUILD_PROJECT_DIR/Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.pdb"
DOTNET_OUTPUT_DIR="$BUILD_PROJECT_DIR/Temp/bin/$BUILD_CONFIGURATION"
DOTNET_DLL="$DOTNET_OUTPUT_DIR/Timberborn.ModExamples.Prometheus.dll"
DOTNET_PDB="$DOTNET_OUTPUT_DIR/Timberborn.ModExamples.Prometheus.pdb"
DST_MOD_DIR="$HOME/Documents/Timberborn/Mods/$MOD_NAME"
DST_SCRIPTS_DIR="$DST_MOD_DIR/Scripts"
BACKUP_ROOT_DIR="$ROOT_DIR/.backups"
BACKUP_DIR="$BACKUP_ROOT_DIR/$MOD_NAME"

usage() {
  cat <<'USAGE'
Usage: bash scripts/deploy_prometheus.sh [--test-only] [--launch]

Options:
  --test-only   Run automated deploy tests and exit without deploying.
  --launch      Launch Timberborn via Steam after a successful deploy.
                Implies stopping any running Timberborn process and waiting
                for a fresh + stable Unity DLL before deploy.

Common combos:
  bash scripts/deploy_prometheus.sh                 # test + deploy
  bash scripts/deploy_prometheus.sh --test-only     # tests only
  bash scripts/deploy_prometheus.sh --launch        # test + stop running + wait for fresh/stable build + deploy + wait + launch

Deployment model:
  The deployed mod directory is recreated as symlinks on each run:
  - Non-Scripts content links to $ROOT_DIR/Assets/Mods/Prometheus/*
  - Scripts/Timberborn.ModExamples.Prometheus.(dll|pdb) link to build output

Compilation model:
  - If $BUILD_PROJECT_DIR/Timberborn.ModExamples.Prometheus.csproj exists,
    deploy runs 'dotnet build' and promotes output into Library/ScriptAssemblies.
  - If the project file is missing, deploy falls back to existing Unity DLL checks.
USAGE
}

compile_prometheus_if_possible() {
  if [[ "${PROMETHEUS_DEPLOY_SKIP_COMPILE:-0}" == "1" ]]; then
    echo "[deploy-prometheus] Skipping compile (PROMETHEUS_DEPLOY_SKIP_COMPILE=1)."
    return 0
  fi

  if [[ ! -f "$PROJECT_CSPROJ" ]]; then
    echo "[deploy-prometheus] Build project file not found at $PROJECT_CSPROJ; using existing DLL workflow."
    return 0
  fi

  if ! command -v dotnet >/dev/null 2>&1; then
    echo "[deploy-prometheus] .NET SDK not found ('dotnet' missing)." >&2
    echo "[deploy-prometheus] Install .NET SDK or set PROMETHEUS_DEPLOY_SKIP_COMPILE=1 to use existing DLL workflow." >&2
    return 1
  fi

  echo "[deploy-prometheus] Compiling Prometheus project with dotnet build ($BUILD_CONFIGURATION)..."
  if ! dotnet build "$PROJECT_CSPROJ" -c "$BUILD_CONFIGURATION" >/tmp/prometheus-dotnet-build.log 2>&1; then
    echo "[deploy-prometheus] dotnet build failed. Tail of build output:" >&2
    tail -n 40 /tmp/prometheus-dotnet-build.log >&2 || true
    return 1
  fi

  if [[ ! -f "$DOTNET_DLL" ]]; then
    echo "[deploy-prometheus] dotnet build succeeded but output DLL missing: $DOTNET_DLL" >&2
    return 1
  fi

  mkdir -p "$(dirname "$SRC_DLL")"
  cp "$DOTNET_DLL" "$SRC_DLL"
  if [[ -f "$DOTNET_PDB" ]]; then
    cp "$DOTNET_PDB" "$SRC_PDB"
  fi

  echo "[deploy-prometheus] Compile complete; refreshed $SRC_DLL"
}

rebuild_symlinked_mod_directory() {
  rm -rf "$DST_MOD_DIR"
  mkdir -p "$DST_MOD_DIR"

  local src_item
  for src_item in "$SRC_MOD_DIR"/*; do
    local item_name
    item_name="$(basename "$src_item")"

    if [[ "$item_name" == "Scripts" ]]; then
      continue
    fi

    if [[ "$item_name" == ".DS_Store" ]] || [[ "$item_name" == *.meta ]]; then
      continue
    fi

    ln -s "$src_item" "$DST_MOD_DIR/$item_name"
  done

  mkdir -p "$DST_SCRIPTS_DIR"
  ln -s "$SRC_DLL" "$DST_SCRIPTS_DIR/Timberborn.ModExamples.Prometheus.dll"
  if [[ -f "$SRC_PDB" ]]; then
    ln -s "$SRC_PDB" "$DST_SCRIPTS_DIR/Timberborn.ModExamples.Prometheus.pdb"
  fi
}

get_newest_source_epoch() {
  local src_script_dir="$SRC_MOD_DIR/Scripts"
  if [[ ! -d "$src_script_dir" ]]; then
    echo ""
    return 0
  fi

  find "$src_script_dir" -type f -name '*.cs' -exec stat -f '%m' {} + | sort -nr | head -n 1 || true
}

get_newest_source_file() {
  local src_script_dir="$SRC_MOD_DIR/Scripts"
  if [[ ! -d "$src_script_dir" ]]; then
    echo "(unknown source file)"
    return 0
  fi

  local newest_source_file
  newest_source_file="$(find "$src_script_dir" -type f -name '*.cs' -print0 | xargs -0 stat -f '%m %N' | sort -nr | head -n 1 | cut -d' ' -f2- || true)"
  if [[ -z "$newest_source_file" ]]; then
    newest_source_file="(unknown source file)"
  fi
  echo "$newest_source_file"
}

get_dll_signature() {
  if [[ ! -f "$SRC_DLL" ]]; then
    echo ""
    return 0
  fi

  stat -f '%m:%z' "$SRC_DLL" 2>/dev/null || true
}

unity_build_errors_detected_since() {
  local start_line="${1:-0}"
  local editor_log_path="${UNITY_EDITOR_LOG_PATH:-$HOME/Library/Logs/Unity/Editor.log}"

  if [[ ! -f "$editor_log_path" ]]; then
    return 1
  fi

  local from_line=$(( start_line + 1 ))
  local log_delta
  log_delta="$(tail -n +"$from_line" "$editor_log_path" 2>/dev/null || true)"
  if [[ -z "$log_delta" ]]; then
    return 1
  fi

  local error_pattern='error CS[0-9]+|Script Compilation Error|build failed|Asset import failed|ReflectionTypeLoadException|Could not load type'
  if ! echo "$log_delta" | grep -Eiq "$error_pattern"; then
    return 1
  fi

  echo "[deploy-prometheus] Unity reported build/import errors while waiting for fresh DLL." >&2
  echo "[deploy-prometheus] Recent Unity errors:" >&2
  echo "$log_delta" | grep -Ein "$error_pattern" | tail -n 12 >&2 || true
  return 0
}

wait_for_fresh_build_if_requested() {
  if [[ "$WAIT_FOR_BUILD" != "true" ]]; then
    return 0
  fi

  local newest_source_epoch
  newest_source_epoch="$(get_newest_source_epoch)"
  if [[ -z "$newest_source_epoch" ]]; then
    return 0
  fi

  local newest_source_file
  newest_source_file="$(get_newest_source_file)"

  local start_epoch
  start_epoch="$(date +%s)"

  local editor_log_path="${UNITY_EDITOR_LOG_PATH:-$HOME/Library/Logs/Unity/Editor.log}"
  local editor_log_line_cursor=0
  if [[ -f "$editor_log_path" ]]; then
    editor_log_line_cursor="$(wc -l < "$editor_log_path" | tr -d '[:space:]')"
  fi

  echo "[deploy-prometheus] Waiting for Unity build output to become fresh..."
  echo "[deploy-prometheus] Newest source: $newest_source_file"
  echo "[deploy-prometheus] Timeout: ${WAIT_FOR_BUILD_TIMEOUT_SECONDS}s (poll ${WAIT_FOR_BUILD_POLL_SECONDS}s)"

  local last_signature=""
  local stable_poll_count=0

  while true; do
    if [[ -f "$SRC_DLL" ]]; then
      local dll_epoch
      dll_epoch="$(stat -f '%m' "$SRC_DLL")"
      if [[ "$newest_source_epoch" -le "$dll_epoch" ]]; then
        local current_signature
        current_signature="$(get_dll_signature)"

        if [[ -n "$current_signature" ]] && [[ "$current_signature" == "$last_signature" ]]; then
          stable_poll_count=$((stable_poll_count + 1))
        else
          stable_poll_count=1
          last_signature="$current_signature"
        fi

        if (( stable_poll_count >= WAIT_FOR_BUILD_STABLE_POLLS )); then
          echo "[deploy-prometheus] Fresh and stable DLL detected. Continuing deployment."
          return 0
        fi
      else
        stable_poll_count=0
        last_signature=""
      fi
    fi

    if [[ -f "$editor_log_path" ]]; then
      local current_editor_log_line_count
      current_editor_log_line_count="$(wc -l < "$editor_log_path" | tr -d '[:space:]')"
      if (( current_editor_log_line_count > editor_log_line_cursor )); then
        if unity_build_errors_detected_since "$editor_log_line_cursor"; then
          return 1
        fi

        editor_log_line_cursor="$current_editor_log_line_count"
      fi
    fi

    local now_epoch
    now_epoch="$(date +%s)"
    local elapsed=$(( now_epoch - start_epoch ))
    if (( elapsed >= WAIT_FOR_BUILD_TIMEOUT_SECONDS )); then
      echo "[deploy-prometheus] Timed out after ${WAIT_FOR_BUILD_TIMEOUT_SECONDS}s waiting for fresh DLL." >&2
      return 1
    fi

    sleep "$WAIT_FOR_BUILD_POLL_SECONDS"
  done
}

ensure_build_not_stale() {
  local newest_source_epoch
  newest_source_epoch="$(get_newest_source_epoch)"
  if [[ -z "$newest_source_epoch" ]]; then
    return 0
  fi

  local dll_epoch
  dll_epoch="$(stat -f '%m' "$SRC_DLL")"
  if [[ "$newest_source_epoch" -le "$dll_epoch" ]]; then
    return 0
  fi

  local newest_source_file
  newest_source_file="$(get_newest_source_file)"

  echo "[deploy-prometheus] Stale build detected: source scripts are newer than DLL." >&2
  echo "[deploy-prometheus] Newest source: $newest_source_file" >&2
  echo "[deploy-prometheus] Rebuild scripts in Unity first, then run deploy again." >&2
  exit 1
}

stop_running_timberborn_if_requested() {
  if [[ "$STOP_RUNNING_BEFORE_DEPLOY" != "true" ]]; then
    return 0
  fi

  if ! command -v pgrep >/dev/null 2>&1 || ! command -v kill >/dev/null 2>&1; then
    echo "[deploy-prometheus] Launch mode requires process tools (pgrep/kill), but they are unavailable." >&2
    return 1
  fi

  local pids
  pids="$(pgrep -f '/Timberborn\.app/|\bTimberborn\b' || true)"
  if [[ -z "$pids" ]]; then
    echo "[deploy-prometheus] No running Timberborn process detected."
    return 0
  fi

  echo "[deploy-prometheus] Stopping running Timberborn process(es): $pids"
  kill $pids || true

  for _ in {1..20}; do
    if ! pgrep -f '/Timberborn\.app/|\bTimberborn\b' >/dev/null 2>&1; then
      echo "[deploy-prometheus] Timberborn stopped."
      return 0
    fi
    sleep 0.25
  done

  pids="$(pgrep -f '/Timberborn\.app/|\bTimberborn\b' || true)"
  if [[ -n "$pids" ]]; then
    echo "[deploy-prometheus] Timberborn is still running after graceful stop; forcing termination: $pids"
    kill -9 $pids || true
  fi

  if pgrep -f '/Timberborn\.app/|\bTimberborn\b' >/dev/null 2>&1; then
    echo "[deploy-prometheus] Unable to stop Timberborn. Please close it manually and rerun deploy." >&2
    return 1
  fi

  echo "[deploy-prometheus] Timberborn stopped."
}

is_timberborn_running() {
  pgrep -f '/Timberborn\.app/|\bTimberborn\b' >/dev/null 2>&1
}

wait_for_timberborn_launch() {
  local timeout_seconds="${1:-20}"
  local poll_seconds=1
  local waited=0

  while (( waited < timeout_seconds )); do
    if is_timberborn_running; then
      return 0
    fi

    sleep "$poll_seconds"
    waited=$(( waited + poll_seconds ))
  done

  return 1
}

determine_timberborn_app_id() {
  if [[ -n "${TIMBERBORN_APP_ID:-}" ]]; then
    echo "$TIMBERBORN_APP_ID"
    return
  fi

  local repo_app_id_file="$ROOT_DIR/steam_appid.txt"
  if [[ -f "$repo_app_id_file" ]]; then
    local repo_app_id
    repo_app_id="$(tr -d '[:space:]' < "$repo_app_id_file" || true)"
    if [[ "$repo_app_id" =~ ^[0-9]+$ ]] && [[ "$repo_app_id" != "480" ]]; then
      echo "$repo_app_id"
      return
    fi
  fi

  echo "$DEFAULT_TIMBERBORN_APP_ID"
}

launch_timberborn() {
  local app_id
  app_id="$(determine_timberborn_app_id)"
  local steam_url="steam://rungameid/$app_id"

  if [[ "${TIMBERBORN_LAUNCH_DRY_RUN:-0}" == "1" ]]; then
    echo "[deploy-prometheus] Dry run: would launch Timberborn via $steam_url"
    return 0
  fi

  if ! command -v open >/dev/null 2>&1; then
    echo "[deploy-prometheus] Cannot launch Timberborn automatically: 'open' command not found." >&2
    return 1
  fi

  open "$steam_url"
  echo "[deploy-prometheus] Launch requested: $steam_url"

  if wait_for_timberborn_launch 20; then
    local launch_pid
    launch_pid="$(pgrep -f '/Timberborn\.app/|\bTimberborn\b' | head -n 1 || true)"
    if [[ -n "$launch_pid" ]]; then
      echo "[deploy-prometheus] Timberborn process detected (pid: $launch_pid)."
    else
      echo "[deploy-prometheus] Timberborn process detected."
    fi
    return 0
  fi

  echo "[deploy-prometheus] Launch URL opened, but Timberborn process was not detected within 20s." >&2
  echo "[deploy-prometheus] If Steam is not focused, try opening this URL manually: $steam_url" >&2
  return 1
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --test-only)
      TEST_ONLY=true
      shift
      ;;
    --launch)
      LAUNCH_AFTER_DEPLOY=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "[deploy-prometheus] Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ "${PROMETHEUS_DEPLOY_SKIP_TESTS:-0}" == "1" ]]; then
  RUN_TESTS_BEFORE_DEPLOY=false
fi

if [[ "$LAUNCH_AFTER_DEPLOY" == "true" ]]; then
  STOP_RUNNING_BEFORE_DEPLOY=true
  WAIT_FOR_BUILD=true
fi

if [[ "$RUN_TESTS_BEFORE_DEPLOY" == "true" ]]; then
  echo "[deploy-prometheus] Running automated tests..."
  bash "$ROOT_DIR/scripts/test_deploy_prometheus.sh"
fi

echo "[deploy-prometheus] Build project: $BUILD_PROJECT_DIR"
echo "[deploy-prometheus] Build DLL source: $SRC_DLL"

if [[ "$TEST_ONLY" == "true" ]]; then
  echo "[deploy-prometheus] Test-only mode complete. Skipping deployment."
  exit 0
fi

if ! compile_prometheus_if_possible; then
  exit 1
fi

stop_running_timberborn_if_requested

if [[ ! -d "$SRC_MOD_DIR" ]]; then
  echo "[deploy-prometheus] Missing source mod directory: $SRC_MOD_DIR" >&2
  exit 1
fi

if [[ ! -f "$SRC_DLL" ]]; then
  if [[ "$WAIT_FOR_BUILD" == "true" ]]; then
    if ! wait_for_fresh_build_if_requested; then
      exit 1
    fi
  fi

  if [[ ! -f "$SRC_DLL" ]]; then
    echo "[deploy-prometheus] Missing built DLL: $SRC_DLL" >&2
    echo "[deploy-prometheus] Build scripts in Unity first, then run this deploy script again." >&2
    exit 1
  fi
fi

if ! wait_for_fresh_build_if_requested; then
  exit 1
fi

ensure_build_not_stale

if [[ -d "$DST_MOD_DIR" ]]; then
  mkdir -p "$BACKUP_ROOT_DIR"
  rm -rf "$BACKUP_DIR"
  cp -R "$DST_MOD_DIR" "$BACKUP_DIR"
  find "$BACKUP_DIR" -type f -name '.DS_Store' -delete
  echo "[deploy-prometheus] Backup created: $BACKUP_DIR"
fi

mkdir -p "$DST_MOD_DIR"
rebuild_symlinked_mod_directory

echo "[deploy-prometheus] Deployment complete (symlinked content + runtime links)."
echo "[deploy-prometheus] Manifest:"
grep -n '"Version"\|"Id"' "$DST_MOD_DIR/manifest.json" || true

echo "[deploy-prometheus] Runtime payload:"
ls -l "$DST_SCRIPTS_DIR" | grep -E 'Timberborn\.ModExamples\.Prometheus\.(dll|pdb)' || true

if [[ "$LAUNCH_AFTER_DEPLOY" == "true" ]]; then
  echo "[deploy-prometheus] Waiting 5s before launch..."
  sleep 5
  launch_timberborn
fi
