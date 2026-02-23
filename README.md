# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus mod assets and local deploy tooling.

## Quick commands

* `bash scripts/build.sh` — compile + deploy
* `bash scripts/build.sh --launch` — compile + stop running Timberborn + wait for fresh/stable build + deploy + clear `Player.log` and `Fire.log` + launch

The deploy script now compiles `Timberborn.ModExamples.Prometheus.csproj` via `dotnet build` (if present) and promotes the generated DLL/PDB into `Library/ScriptAssemblies` before deployment.
It still blocks stale builds (when source `Assets/Mods/Prometheus/Scripts/*.cs` files are newer than `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll`) as a safety net. When `--launch` is used, it waits for DLL freshness plus stability across polling before continuing.
When `--launch` is used, the script clears:
* `~/Library/Logs/Mechanistry/Timberborn/Player.log`
* `~/Library/Logs/Mechanistry/Timberborn/Fire.log`

Then it waits 5 seconds before launching.

## Notes

* Latest backup is saved to `.backups/Prometheus`.
* `.backups/` is ignored by Git.
* `steam_appid.txt` is local-only and ignored.
* Deploy now uses a symlink-first model by default:
  * non-`Scripts` mod content under `~/Documents/Timberborn/Mods/Prometheus` is symlinked to `Assets/Mods/Prometheus`
  * runtime `Scripts/Timberborn.ModExamples.Prometheus.(dll|pdb)` are symlinked to Unity build output
* Dedicated fire log:
  * `~/Library/Logs/Mechanistry/Timberborn/Fire.log`
  * Mirrors `[Prometheus/Fire]` entries for low-noise analysis.

## Development loop (code -> verify -> logs)

Use this default loop for feature work and balancing validation:

* Make one focused code change.

* Run `bash scripts/build.sh --launch` (compile + deploy + clear `Player.log` and `Fire.log` + launch).

* In game, run the minimal scenario to verify behavior.

* Analyze outcomes:
  * Preferred: parse `~/Library/Logs/Mechanistry/Timberborn/Fire.log`.
  * Secondary: parse `~/Library/Logs/Mechanistry/Timberborn/Player.log` (`[Prometheus/Fire]` lines).
  * Fallback: if the behavior is not log-observable yet, capture panel evidence and tester notes.

* Repeat with one incremental change at a time.

Recommended guardrails:

* Keep each run scoped to one intent (single trigger when possible) so results are attributable.
* Record profile/policy context with every validation note or log snippet.
* If temporary deterministic/debug-only code is added for validation, remove or gate it after evidence capture.

## Latest session status

* Last updated: 2026-02-22
* Current focus:
  * Validate explosion request/apply lifecycle with low-noise `Fire.log` evidence.
  * Keep the renamed build/handoff workflow (`scripts/build.sh`, normalized skill names, `Fire.log`) stable for tight-loop iteration.
* Latest verified results:
  * Completed-building run confirms deterministic debug explosion path: `debug_ignite_request`, `debug_ignite_applied`, `explosion_ignition_request`, and `explosion_detonated` (with `forced=True`, `mode=Off`).
  * Build/deploy and handoff naming cleanup is in place: `scripts/build.sh`, `Fire.log`, and generic skill names (`deploy`, `build-deploy`, `session-handoff`).
* Next steps:
  * Run one `bash scripts/build.sh --launch` smoke pass post-rename to confirm expected `[build]` output and log clearing behavior.
  * Add targeted logs for spread-ignition request consumption/ignore reasons.
  * Re-run single-ignite capture and verify whether `explosion_ignite_applied` occurs.
  * After request/apply tracing is clear, resume mode-by-mode policy validation pass.
* Full handoff: `Assets/Mods/Prometheus/SESSION_HANDOFF.md`
