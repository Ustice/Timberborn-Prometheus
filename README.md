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
  * Validate fire-system hardening changes in live gameplay (dead building suppression, preview exclusion, destroy cleanup).
  * Validate new in-game fire debug UX (filters/search/colored log rows/entity jump).
* Latest verified results:
  * Behavior fixes landed: dead buildings suppress workers + operational behaviors; placement previews excluded from ignition path; snapshots cleaned on entity destroy.
  * Debug panel now includes runtime count deltas, minimizable scrollable fire log, severity filters, colored labels, search, and per-line `View` (entity focus) button.
* Next steps:
  * Run one focused in-game pass to validate `View` button behavior across `id=`, `sourceId=`, and `targetId=` log lines.
  * Re-run explosion request/apply lifecycle capture with one-ignite-per-window guidance and archive evidence from `Fire.log` + `Player.log`.
  * Expand dead-building verification across additional building archetypes and tune if needed.
* Full handoff: `Assets/Mods/Prometheus/SESSION_HANDOFF.md`
