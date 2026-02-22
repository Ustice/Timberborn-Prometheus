#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SOURCE_DEPLOY_SCRIPT="$ROOT_DIR/scripts/deploy_prometheus.sh"

if [[ ! -f "$SOURCE_DEPLOY_SCRIPT" ]]; then
	echo "[test-deploy-prometheus] Missing deploy script at $SOURCE_DEPLOY_SCRIPT" >&2
	exit 1
fi

PASS_COUNT=0
FAIL_COUNT=0

fail() {
	local msg="$1"
	echo "[FAIL] $msg"
	FAIL_COUNT=$((FAIL_COUNT + 1))
}

pass() {
	local msg="$1"
	echo "[PASS] $msg"
	PASS_COUNT=$((PASS_COUNT + 1))
}

assert_file_exists() {
	local path="$1"
	local label="$2"
	if [[ -f "$path" ]]; then
		pass "$label"
	else
		fail "$label (missing: $path)"
	fi
}

assert_not_exists() {
	local path="$1"
	local label="$2"
	if [[ ! -e "$path" ]]; then
		pass "$label"
	else
		fail "$label (unexpected path: $path)"
	fi
}

assert_contains() {
	local file="$1"
	local needle="$2"
	local label="$3"
	if grep -q "$needle" "$file"; then
		pass "$label"
	else
		fail "$label (did not find '$needle' in $file)"
	fi
}

create_temp_repo() {
	local tmp_root
	tmp_root="$(mktemp -d)"
	local tmp_repo="$tmp_root/repo"
	mkdir -p "$tmp_repo/scripts"
	cp "$SOURCE_DEPLOY_SCRIPT" "$tmp_repo/scripts/deploy_prometheus.sh"
	chmod +x "$tmp_repo/scripts/deploy_prometheus.sh"
	echo "$tmp_root"
}

test_successful_deploy_creates_latest_backup() {
	echo "\n[test] successful deploy copies payload and saves latest backup in project .backups"
	local tmp_root
	tmp_root="$(create_temp_repo)"
	local repo="$tmp_root/repo"
	local home="$tmp_root/home"
	local log="$tmp_root/run.log"

	mkdir -p "$repo/Assets/Mods/Prometheus"
	mkdir -p "$repo/Library/ScriptAssemblies"
	mkdir -p "$home/Documents/Timberborn/Mods/Prometheus"

	cat > "$repo/Assets/Mods/Prometheus/manifest.json" <<'JSON'
{
	"Name": "Prometheus",
	"Version": "9.9.9",
	"Id": "ExampleBuilding.Prometheus"
}
JSON

	echo "payload" > "$repo/Assets/Mods/Prometheus/content.txt"
	echo "should_not_copy" > "$repo/Assets/Mods/Prometheus/ignore.meta"
	echo "should_not_copy" > "$repo/Assets/Mods/Prometheus/.DS_Store"

	echo "dll-bytes" > "$repo/Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll"
	echo "pdb-bytes" > "$repo/Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.pdb"

	echo "old-runtime-data" > "$home/Documents/Timberborn/Mods/Prometheus/old.txt"
	echo "old-backup-noise" > "$home/Documents/Timberborn/Mods/Prometheus/.DS_Store"

	if HOME="$home" bash "$repo/scripts/deploy_prometheus.sh" >"$log" 2>&1; then
		pass "deploy script exits successfully"
	else
		fail "deploy script should succeed"
	fi

	assert_file_exists "$home/Documents/Timberborn/Mods/Prometheus/manifest.json" "manifest deployed"
	assert_file_exists "$home/Documents/Timberborn/Mods/Prometheus/content.txt" "content deployed"
	assert_not_exists "$home/Documents/Timberborn/Mods/Prometheus/ignore.meta" "meta files excluded"
	assert_file_exists "$home/Documents/Timberborn/Mods/Prometheus/Scripts/Timberborn.ModExamples.Prometheus.dll" "dll deployed"
	assert_file_exists "$home/Documents/Timberborn/Mods/Prometheus/Scripts/Timberborn.ModExamples.Prometheus.pdb" "pdb deployed"

	assert_file_exists "$repo/.backups/Prometheus/old.txt" "latest backup captured previous destination state"
	assert_not_exists "$repo/.backups/Prometheus/.DS_Store" "backup DS_Store removed"
	assert_contains "$log" "Backup created: $repo/.backups/Prometheus" "backup path points to project .backups"

	if compgen -G "$home/Documents/Timberborn/Mods/Prometheus.backup-*" > /dev/null; then
		fail "legacy timestamped backups should not be created in home Mods directory"
	else
		pass "no legacy home backup folders created"
	fi

	assert_contains "$log" "Deployment complete" "success output includes completion message"

	rm -rf "$tmp_root"
}

test_missing_dll_fails() {
	echo "\n[test] missing dll fails with actionable message"
	local tmp_root
	tmp_root="$(create_temp_repo)"
	local repo="$tmp_root/repo"
	local home="$tmp_root/home"
	local log="$tmp_root/run.log"

	mkdir -p "$repo/Assets/Mods/Prometheus"
	cat > "$repo/Assets/Mods/Prometheus/manifest.json" <<'JSON'
{
	"Name": "Prometheus",
	"Version": "9.9.9",
	"Id": "ExampleBuilding.Prometheus"
}
JSON

	set +e
	HOME="$home" bash "$repo/scripts/deploy_prometheus.sh" >"$log" 2>&1
	local status=$?
	set -e

	if [[ "$status" -ne 0 ]]; then
		pass "deploy script fails when dll is missing"
	else
		fail "deploy script should fail when dll is missing"
	fi

	assert_contains "$log" "Missing built DLL" "error output mentions missing DLL"
	assert_contains "$log" "Build scripts in Unity first" "error output gives Unity build guidance"

	rm -rf "$tmp_root"
}

echo "[test-deploy-prometheus] Running deploy script tests..."
test_successful_deploy_creates_latest_backup
test_missing_dll_fails

echo "\n[test-deploy-prometheus] Completed: $PASS_COUNT passed, $FAIL_COUNT failed"
if [[ "$FAIL_COUNT" -gt 0 ]]; then
	exit 1
fi
