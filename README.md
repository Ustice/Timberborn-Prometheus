# Prometheus Mod (Standalone)

This repository contains the standalone Prometheus mod assets and local deploy tooling.

## Quick commands

* `bash scripts/build.sh` — compile + deploy
* `bash scripts/build.sh --launch` — compile + stop running Timberborn + wait for fresh/stable build + deploy + clear `Player.log` and `Fire.log` + launch
* `bash scripts/test.sh` — run fast plain C# regression tests for Prometheus runtime stores and decision rules

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

## Automated tests

Use plain C# tests for gameplay decisions and regression-prone runtime state:

* Run `bash scripts/test.sh`.
* Tests live under `tests/Prometheus.Tests`.
* Test results are written to `TestResults/Prometheus.Tests.trx`.
* Coverage is written under `TestResults/*/coverage.cobertura.xml`.
* Keep Unity-specific components thin and move decision logic into dependency-light rule/runtime classes when feasible.
* Debug panel UI remains manually QA'd because it is actively changing.

When making a real system decision, add or update a regression test for that decision whenever feasible.

Unity EditMode testing was explored, but this standalone repo currently cannot reliably load the full Timberborn assembly graph without turning package/plugin resolution into the main problem. Reserve Unity tests for behavior that truly needs Unity lifecycle coverage.

Recommended guardrails:

* Keep each run scoped to one intent (single trigger when possible) so results are attributable.
* Record profile/policy context with every validation note or log snippet.
* If temporary deterministic/debug-only code is added for validation, remove or gate it after evidence capture.

## Latest session status

* Last updated: 2026-04-25
* Current focus:
  * Phase 2 ember-field spread and fire presentation.
  * Move suppression/responder complexity behind the core spread model.
* Latest verified results:
  * Phase 1 core fire loop is complete enough to close: ignition, spread, extinguish, damage, dead/ash terminal behavior, and reset-to-healthy flow have passed live QA.
  * `Reset Fire Sim` now provides a clean-slate recovery path for loaded fire entities and clears stale fire/damage/recovery snapshots.
  * The Prometheus debug panel has been reorganized into TimberUi command, filter, selection, and log sections for manual QA.
  * Telemetry event names are centralized in an iterable registry and covered by a uniqueness regression test.
  * Phase 2 design has been simplified around ember-field cellular spread: active fires, selected high-intensity buildings, fireworks, and unstable explosive events can emit ember pressure.
  * Fire presentation should map to runtime state with embers, smoke, active fire, steam from moisture, and charred material/shader treatment.
  * First visual effect adapter is implemented and deployed: fire-profiled entities now derive tunable ember, smoke, flame, steam, and char intensities from runtime snapshots.
  * The debug panel now includes `Visual Tuning` sliders for ember, smoke, fire, steam, char, and legacy text-marker scale; legacy `FIRE!`/`DEAD` text markers default off.
  * The debug panel now uses installable `TimberUi` plus `ModdableToolGroups` dependencies for native-style controls and a bottom-bar submenu (`Prometheus` -> `Actions`, `Visuals`, `Selection`, `Log`) instead of in-panel tabs.
  * Latest TimberUi pass removes the old hidden entity-panel UI, keeps only the selection forwarding hook, moves log counts into the Log filters, and removes custom visible `style.*` overrides so TimberUi controls/layout own the blank-slate presentation.
  * Follow-up screenshot pass gives the bottom-left panel a native fixed width/offset, reinitializes dynamically switched submenu views through TimberUi, and uses `AddHorizontalContainer()` rows for command/filter/selection controls so buttons render with their native styles.
  * Latest selection-panel cleanup switches `Copy` and `Ignite` to TimberUi `GameButton` controls after screenshot QA showed entity-fragment buttons rendering as plain text in the detached bottom-bar panel.
  * Detached custom-panel pass now matches the source examples (`QuestPanel`/`TodoListPanel`) by using a `NineSliceVisualElement` root with TimberUi `square-large--green`, padding, width, and direct child controls.
  * Latest button cleanup uses TimberUi `AddGameButtonPadded` for debug-panel command/filter/view buttons so button text gets the same horizontal inset used by source examples.
  * `bash scripts/test.sh` passes 22 decision-rule tests, including visual-state intensity coverage; `bash scripts/build.sh --launch` passes with the installed Steam Workshop `TimberUi` and `ModdableToolGroups` assemblies, and fresh startup logs show all three mods loading without Prometheus errors.
  * The only core post-fire resource is Fertile Ash; bucket/foam/gear loops and fire-brigade mechanics are deferred unless they prove necessary.
  * Old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding has been pruned from active content.
* Next steps:
  * QA the first in-world effects pass, then continue Phase 2 implementation around ember-field spread rules and Fertile Ash source tagging.
  * Re-run explosion request/apply lifecycle capture during Phase 2 validation if gaps reappear.
* Full handoff: `Assets/Mods/Prometheus/SESSION_HANDOFF.md`
