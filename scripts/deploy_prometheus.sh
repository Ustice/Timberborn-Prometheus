#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
MOD_NAME="Prometheus"
LAUNCH_AFTER_DEPLOY=false
RUN_TESTS_BEFORE_DEPLOY=false
TEST_ONLY=false
DEFAULT_TIMBERBORN_APP_ID="1062090"
SRC_MOD_DIR="$ROOT_DIR/Assets/Mods/$MOD_NAME"
SRC_DLL="$ROOT_DIR/Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll"
SRC_PDB="$ROOT_DIR/Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.pdb"
DST_MOD_DIR="$HOME/Documents/Timberborn/Mods/$MOD_NAME"
DST_SCRIPTS_DIR="$DST_MOD_DIR/Scripts"
BACKUP_ROOT_DIR="$ROOT_DIR/.backups"
BACKUP_DIR="$BACKUP_ROOT_DIR/$MOD_NAME"

usage() {
  cat <<'USAGE'
Usage: bash scripts/deploy_prometheus.sh [--test] [--test-only] [--launch] [--no-launch]

Options:
  --test        Run automated deploy tests before deployment.
  --test-only   Run automated deploy tests and exit without deploying.
  --launch      Launch Timberborn via Steam after a successful deploy.
  --no-launch   Do not launch Timberborn after deploy (default).

Common combos:
  bash scripts/deploy_prometheus.sh                 # deploy only
  bash scripts/deploy_prometheus.sh --test-only     # tests only
  bash scripts/deploy_prometheus.sh --test          # test + deploy
  bash scripts/deploy_prometheus.sh --launch        # deploy + launch
  bash scripts/deploy_prometheus.sh --test --launch # test + deploy + launch
USAGE
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
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --test)
      RUN_TESTS_BEFORE_DEPLOY=true
      shift
      ;;
    --test-only)
      RUN_TESTS_BEFORE_DEPLOY=true
      TEST_ONLY=true
      shift
      ;;
    --launch)
      LAUNCH_AFTER_DEPLOY=true
      shift
      ;;
    --no-launch)
      LAUNCH_AFTER_DEPLOY=false
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

if [[ "$RUN_TESTS_BEFORE_DEPLOY" == "true" ]]; then
  echo "[deploy-prometheus] Running automated tests..."
  bash "$ROOT_DIR/scripts/test_deploy_prometheus.sh"
fi

if [[ "$TEST_ONLY" == "true" ]]; then
  echo "[deploy-prometheus] Test-only mode complete. Skipping deployment."
  exit 0
fi

if [[ ! -d "$SRC_MOD_DIR" ]]; then
  echo "[deploy-prometheus] Missing source mod directory: $SRC_MOD_DIR" >&2
  exit 1
fi

if [[ ! -f "$SRC_DLL" ]]; then
  echo "[deploy-prometheus] Missing built DLL: $SRC_DLL" >&2
  echo "[deploy-prometheus] Build scripts in Unity first, then run this deploy script again." >&2
  exit 1
fi

if [[ -d "$DST_MOD_DIR" ]]; then
  mkdir -p "$BACKUP_ROOT_DIR"
  rm -rf "$BACKUP_DIR"
  cp -R "$DST_MOD_DIR" "$BACKUP_DIR"
  find "$BACKUP_DIR" -type f -name '.DS_Store' -delete
  echo "[deploy-prometheus] Backup created: $BACKUP_DIR"
fi

mkdir -p "$DST_MOD_DIR"
rsync -a --delete --exclude '*.meta' --exclude '.DS_Store' "$SRC_MOD_DIR/" "$DST_MOD_DIR/"

mkdir -p "$DST_SCRIPTS_DIR"
cp "$SRC_DLL" "$DST_SCRIPTS_DIR/"
if [[ -f "$SRC_PDB" ]]; then
  cp "$SRC_PDB" "$DST_SCRIPTS_DIR/"
fi

echo "[deploy-prometheus] Deployment complete."
echo "[deploy-prometheus] Manifest:"
grep -n '"Version"\|"Id"' "$DST_MOD_DIR/manifest.json" || true

echo "[deploy-prometheus] Runtime payload:"
ls -1 "$DST_SCRIPTS_DIR" | grep -E 'Timberborn\.ModExamples\.Prometheus\.(dll|pdb)' || true

if [[ "$LAUNCH_AFTER_DEPLOY" == "true" ]]; then
  launch_timberborn
fi
