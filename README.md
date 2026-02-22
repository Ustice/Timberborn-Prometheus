# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus mod assets and local deploy tooling.

## Quick commands

* `bash scripts/deploy_prometheus.sh` — test + deploy
* `bash scripts/deploy_prometheus.sh --test-only` — tests only
* `bash scripts/deploy_prometheus.sh --launch` — test + stop running Timberborn + wait for fresh/stable build + deploy + launch

The deploy script blocks stale builds (when source `Assets/Mods/Prometheus/Scripts/*.cs` files are newer than `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll`). When `--launch` is used, it waits for DLL freshness plus stability across polling before continuing.
When `--launch` is used, the script waits 5 seconds before launching.

## Notes

* Latest backup is saved to `.backups/Prometheus`.
* `.backups/` is ignored by Git.
* `steam_appid.txt` is local-only and ignored.
* Deploy now uses a symlink-first model by default:
  * non-`Scripts` mod content under `~/Documents/Timberborn/Mods/Prometheus` is symlinked to `Assets/Mods/Prometheus`
  * runtime `Scripts/Timberborn.ModExamples.Prometheus.(dll|pdb)` are symlinked to Unity build output

## Latest session status

* Last updated: 2026-02-22
* Current focus: execute tuned A/B comparison run after adding build-wait polling to deployment.
* Latest verified results:
  * Debug ignite flow works and structured fire events are logged to `Player.log` under `[Prometheus/Fire]`.
  * Clean baseline captured: `spread=0.097`, `quench=0.075`, intensity rising (`0.371 -> 0.478`) after single ignite.
  * Deploy test suite now reports `32 passed, 0 failed`.
  * `--launch` now implies stop-running + wait-for-build.
* Next steps:
  * Run `bash scripts/deploy_prometheus.sh --launch`.
  * Execute one tuned single-ignite pass and capture panel + log window.
  * Compare tuned telemetry against baseline and record outcome in design docs.
* Full handoff: `Assets/Mods/Prometheus/SESSION_HANDOFF.md`
