# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus mod assets and local deploy tooling.

## Quick commands

* `bash scripts/deploy_prometheus.sh` — deploy only
* `bash scripts/deploy_prometheus.sh --test-only` — tests only
* `bash scripts/deploy_prometheus.sh --test` — test + deploy
* `bash scripts/deploy_prometheus.sh --launch` — deploy + launch Timberborn
* `bash scripts/deploy_prometheus.sh --launch-delay 10 --launch` — deploy, wait 10 seconds, then launch Timberborn
* `bash scripts/deploy_prometheus.sh --test --launch` — test + deploy + launch
* `bash scripts/deploy_prometheus.sh --stop-running --launch` — stop running Timberborn, deploy, then relaunch
* `bash scripts/deploy_prometheus.sh --wait-for-build --test --stop-running --launch` — wait for a fresh + stable Unity DLL, then test + deploy + relaunch

The deploy script blocks stale builds (when source `Assets/Mods/Prometheus/Scripts/*.cs` files are newer than `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll`) and `--wait-for-build` now waits for DLL freshness plus stability across polling before continuing.
When `--launch` is used, the script waits 5 seconds before launching by default (`--launch-delay <seconds>` can override, including `0` for immediate launch).

## Notes

* Latest backup is saved to `.backups/Prometheus`.
* `.backups/` is ignored by Git.
* `steam_appid.txt` is local-only and ignored.
* Deploy now uses a symlink-first model by default:
  * non-`Scripts` mod content under `~/Documents/Timberborn/Mods/Prometheus` is symlinked to `Assets/Mods/Prometheus`
  * runtime `Scripts/Timberborn.ModExamples.Prometheus.(dll|pdb)` are symlinked to the selected Unity build output (`PROMETHEUS_BUILD_PROJECT_DIR`, defaulting to `../timberborn-modding` when present)

## Latest session status

* Last updated: 2026-02-22
* Current focus: execute tuned A/B comparison run after adding build-wait polling to deployment.
* Latest verified results:
  * Debug ignite flow works and structured fire events are logged to `Player.log` under `[Prometheus/Fire]`.
  * Clean baseline captured: `spread=0.097`, `quench=0.075`, intensity rising (`0.371 -> 0.478`) after single ignite.
  * Deploy test suite now reports `22 passed, 0 failed`.
  * Deploy script `--wait-for-build` / `--wait-for-build-timeout` now requires a fresh + stable DLL (not changing between polls) before deployment continues.
* Next steps:
  * Run `bash scripts/deploy_prometheus.sh --wait-for-build --test --stop-running --launch`.
  * Execute one tuned single-ignite pass and capture panel + log window.
  * Compare tuned telemetry against baseline and record outcome in design docs.
* Full handoff: `Assets/Mods/Prometheus/SESSION_HANDOFF.md`
