#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_NAME="Prometheus"
LAUNCH_AFTER_BUILD=false
STOP_RUNNING_BEFORE_BUILD=false
WAIT_FOR_BUILD=false
WAIT_FOR_BUILD_TIMEOUT_SECONDS=10
WAIT_FOR_BUILD_POLL_SECONDS=2
WAIT_FOR_BUILD_STABLE_POLLS=2
DEFAULT_TIMBERBORN_APP_ID="1062090"
DEFAULT_PLAYER_LOG_PATH="$HOME/Library/Logs/Mechanistry/Timberborn/Player.log"
DEFAULT_FIRE_LOG_PATH="$HOME/Library/Logs/Mechanistry/Timberborn/Fire.log"
BUILD_CONFIGURATION="${BUILD_CONFIGURATION:-Debug}"
DEFAULT_BUILD_PROJECT_DIR="$ROOT_DIR"
if [[ -d "$ROOT_DIR/../timberborn-modding" ]]; then
  DEFAULT_BUILD_PROJECT_DIR="$(cd "$ROOT_DIR/../timberborn-modding" && pwd)"
fi
BUILD_PROJECT_DIR="${BUILD_PROJECT_DIR:-$DEFAULT_BUILD_PROJECT_DIR}"
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
PLAYER_LOG_PATH="${TIMBERBORN_PLAYER_LOG_PATH:-$DEFAULT_PLAYER_LOG_PATH}"
FIRE_LOG_PATH="${TIMBERBORN_FIRE_LOG_PATH:-$DEFAULT_FIRE_LOG_PATH}"

usage() {
  cat <<'USAGE'
Usage: bash scripts/build.sh [--launch]

Options:
  --launch      Launch Timberborn via Steam after a successful build/deploy.
                Implies stopping any running Timberborn process and waiting
                for a fresh + stable Unity DLL before build/deploy.
                Also clears Timberborn Player.log and Fire.log before launch.

Common combos:
  bash scripts/build.sh            # build + deploy
  bash scripts/build.sh --launch   # build + stop running + wait for fresh/stable build + deploy + wait + launch

Build/deploy model:
  The deployed mod directory is recreated as symlinks on each run:
  - Non-Scripts content links to $ROOT_DIR/Assets/Mods/Prometheus/*
  - Scripts/Timberborn.ModExamples.Prometheus.(dll|pdb) link to build output

Compilation model:
  - If $BUILD_PROJECT_DIR/Timberborn.ModExamples.Prometheus.csproj exists,
    the script runs 'dotnet build' and promotes output into Library/ScriptAssemblies.
  - If the project file is missing, the script falls back to existing Unity DLL checks.
USAGE
}

compile_if_possible() {
  if [[ "${BUILD_SKIP_COMPILE:-0}" == "1" ]]; then
    echo "[build] Skipping compile (BUILD_SKIP_COMPILE=1)."
    return 0
  fi

  if [[ ! -f "$PROJECT_CSPROJ" ]]; then
    echo "[build] Build project file not found at $PROJECT_CSPROJ; using existing DLL workflow."
    return 0
  fi

  if ! command -v dotnet >/dev/null 2>&1; then
    echo "[build] .NET SDK not found ('dotnet' missing)." >&2
    echo "[build] Install .NET SDK or set BUILD_SKIP_COMPILE=1 to use existing DLL workflow." >&2
    return 1
  fi

  sync_prometheus_compile_items

  echo "[build] Compiling project with dotnet build ($BUILD_CONFIGURATION)..."
  if ! dotnet build "$PROJECT_CSPROJ" -c "$BUILD_CONFIGURATION" >/tmp/build-dotnet.log 2>&1; then
    echo "[build] dotnet build failed. Tail of build output:" >&2
    tail -n 40 /tmp/build-dotnet.log >&2 || true
    return 1
  fi

  if [[ ! -f "$DOTNET_DLL" ]]; then
    echo "[build] dotnet build succeeded but output DLL missing: $DOTNET_DLL" >&2
    return 1
  fi

  mkdir -p "$(dirname "$SRC_DLL")"
  cp "$DOTNET_DLL" "$SRC_DLL"
  if [[ -f "$DOTNET_PDB" ]]; then
    cp "$DOTNET_PDB" "$SRC_PDB"
  fi

  echo "[build] Compile complete; refreshed $SRC_DLL"
}

sync_prometheus_compile_items() {
  add_missing_prometheus_compile_items
  prune_stale_prometheus_compile_items
}

add_missing_prometheus_compile_items() {
  local source_file
  while IFS= read -r source_file; do
    local relative_source
    relative_source="${source_file#$BUILD_PROJECT_DIR/}"
    if grep -Fq "<Compile Include=\"$relative_source\" />" "$PROJECT_CSPROJ"; then
      continue
    fi

    perl -0pi -e "s#(\\s*</ItemGroup>\\s*<ItemGroup>\\s*<None Include=\"Assets/Mods/Prometheus/Scripts/Timberborn\\.ModExamples\\.Prometheus\\.asmdef\")#    <Compile Include=\"$relative_source\" />\\n\\1#" "$PROJECT_CSPROJ"
  done < <(find "$BUILD_PROJECT_DIR/Assets/Mods/Prometheus/Scripts" -maxdepth 1 -type f -name '*.cs' | sort)
}

prune_stale_prometheus_compile_items() {
  local relative_source
  while IFS= read -r relative_source; do
    if [[ -z "$relative_source" ]]; then
      continue
    fi

    if [[ -f "$BUILD_PROJECT_DIR/$relative_source" ]]; then
      continue
    fi

    perl -0pi -e "s#\\s*<Compile Include=\"\\Q$relative_source\\E\" />\\r?\\n##g" "$PROJECT_CSPROJ"
  done < <(grep -o 'Assets/Mods/Prometheus/Scripts/[^"]*\.cs' "$PROJECT_CSPROJ" | sort -u)
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

  echo "[build] Unity reported build/import errors while waiting for fresh DLL." >&2
  echo "[build] Recent Unity errors:" >&2
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

  echo "[build] Waiting for Unity build output to become fresh..."
  echo "[build] Newest source: $newest_source_file"
  echo "[build] Timeout: ${WAIT_FOR_BUILD_TIMEOUT_SECONDS}s (poll ${WAIT_FOR_BUILD_POLL_SECONDS}s)"

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
          echo "[build] Fresh and stable DLL detected. Continuing deployment."
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
      echo "[build] Timed out after ${WAIT_FOR_BUILD_TIMEOUT_SECONDS}s waiting for fresh DLL." >&2
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

  echo "[build] Stale build detected: source scripts are newer than DLL." >&2
  echo "[build] Newest source: $newest_source_file" >&2
  echo "[build] Rebuild scripts in Unity first, then run build again." >&2
  exit 1
}

stop_running_timberborn_if_requested() {
  if [[ "$STOP_RUNNING_BEFORE_BUILD" != "true" ]]; then
    return 0
  fi

  if ! command -v pgrep >/dev/null 2>&1 || ! command -v kill >/dev/null 2>&1; then
    echo "[build] Launch mode requires process tools (pgrep/kill), but they are unavailable." >&2
    return 1
  fi

  local pids
  pids="$(pgrep -f '/Timberborn\.app/|\bTimberborn\b' || true)"
  if [[ -z "$pids" ]]; then
    echo "[build] No running Timberborn process detected."
    return 0
  fi

  echo "[build] Stopping running Timberborn process(es): $pids"
  kill $pids || true

  for _ in {1..20}; do
    if ! pgrep -f '/Timberborn\.app/|\bTimberborn\b' >/dev/null 2>&1; then
      echo "[build] Timberborn stopped."
      return 0
    fi
    sleep 0.25
  done

  pids="$(pgrep -f '/Timberborn\.app/|\bTimberborn\b' || true)"
  if [[ -n "$pids" ]]; then
    echo "[build] Timberborn is still running after graceful stop; forcing termination: $pids"
    kill -9 $pids || true
  fi

  if pgrep -f '/Timberborn\.app/|\bTimberborn\b' >/dev/null 2>&1; then
    echo "[build] Unable to stop Timberborn. Please close it manually and rerun build." >&2
    return 1
  fi

  echo "[build] Timberborn stopped."
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
    echo "[build] Dry run: would launch Timberborn via $steam_url"
    return 0
  fi

  if ! command -v open >/dev/null 2>&1; then
    echo "[build] Cannot launch Timberborn automatically: 'open' command not found." >&2
    return 1
  fi

  open "$steam_url"
  echo "[build] Launch requested: $steam_url"

  if wait_for_timberborn_launch 20; then
    local launch_pid
    launch_pid="$(pgrep -f '/Timberborn\.app/|\bTimberborn\b' | head -n 1 || true)"
    if [[ -n "$launch_pid" ]]; then
      echo "[build] Timberborn process detected (pid: $launch_pid)."
    else
      echo "[build] Timberborn process detected."
    fi
    return 0
  fi

  echo "[build] Launch URL opened, but Timberborn process was not detected within 20s." >&2
  echo "[build] If Steam is not focused, try opening this URL manually: $steam_url" >&2
  return 1
}

clear_player_log_for_launch() {
  if [[ "$LAUNCH_AFTER_BUILD" != "true" ]]; then
    return 0
  fi

  local log_dir
  log_dir="$(dirname "$PLAYER_LOG_PATH")"
  mkdir -p "$log_dir"

  if ! : > "$PLAYER_LOG_PATH"; then
    echo "[build] Failed to clear Player.log at $PLAYER_LOG_PATH" >&2
    return 1
  fi

  echo "[build] Cleared Player.log: $PLAYER_LOG_PATH"
}

clear_fire_log_for_launch() {
  if [[ "$LAUNCH_AFTER_BUILD" != "true" ]]; then
    return 0
  fi

  local log_dir
  log_dir="$(dirname "$FIRE_LOG_PATH")"
  mkdir -p "$log_dir"

  if ! : > "$FIRE_LOG_PATH"; then
    echo "[build] Failed to clear Fire.log at $FIRE_LOG_PATH" >&2
    return 1
  fi

  echo "[build] Cleared Fire.log: $FIRE_LOG_PATH"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --launch)
      LAUNCH_AFTER_BUILD=true
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "[build] Unknown argument: $1" >&2
      usage >&2
      exit 1
      ;;
  esac
done

if [[ "$LAUNCH_AFTER_BUILD" == "true" ]]; then
  STOP_RUNNING_BEFORE_BUILD=true
  WAIT_FOR_BUILD=true
fi

echo "[build] Build project: $BUILD_PROJECT_DIR"
echo "[build] Build DLL source: $SRC_DLL"

if ! compile_if_possible; then
  exit 1
fi

stop_running_timberborn_if_requested

if [[ ! -d "$SRC_MOD_DIR" ]]; then
  echo "[build] Missing source mod directory: $SRC_MOD_DIR" >&2
  exit 1
fi

if [[ ! -f "$SRC_DLL" ]]; then
  if [[ "$WAIT_FOR_BUILD" == "true" ]]; then
    if ! wait_for_fresh_build_if_requested; then
      exit 1
    fi
  fi

  if [[ ! -f "$SRC_DLL" ]]; then
    echo "[build] Missing built DLL: $SRC_DLL" >&2
    echo "[build] Build scripts in Unity first, then run this build script again." >&2
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
  echo "[build] Backup created: $BACKUP_DIR"
fi

mkdir -p "$DST_MOD_DIR"
rebuild_symlinked_mod_directory

echo "[build] Deployment complete (symlinked content + runtime links)."
echo "[build] Manifest:"
grep -n '"Version"\|"Id"' "$DST_MOD_DIR/manifest.json" || true

echo "[build] Runtime payload:"
ls -l "$DST_SCRIPTS_DIR" | grep -E 'Timberborn\.ModExamples\.Prometheus\.(dll|pdb)' || true

if [[ "$LAUNCH_AFTER_BUILD" == "true" ]]; then
  clear_player_log_for_launch
  clear_fire_log_for_launch
  echo "[build] Waiting 5s before launch..."
  sleep 5
  launch_timberborn
fi
