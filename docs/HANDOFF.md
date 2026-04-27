# Prometheus Handoff

## Current Focus

Last updated: 2026-04-27

Prometheus is moving into the 3D grid fire rewrite. The old entity-neighbor spread and responder-first runtime model has been removed from active source so the new sparse chunked cellular system can land without legacy behavior mixed in.

Phase 2 stabilization is now running from the file board under `docs/stabilization/tickets/`. Wave A, Wave B, Wave C, Wave D, P2S-019, and P2S-022 are integrated and done, including P2S-009 reset-registry live QA, P2S-013 configured-source startup QA, P2S-017 effect-facade startup QA, the native recovered-good wrapper for Fertile Ash, and dependency-light field amendment state. P2S-023 is implemented in its worker branch with dependency-light crop growth rules and startup QA evidence.

## Verified Since Last Checkpoint

| Date | Command / Evidence | Result | Notes |
| --- | --- | --- | --- |
| 2026-04-25 | `bash scripts/test.sh && bash scripts/build.sh --launch` | Pass | Removal pass launched Timberborn successfully. |
| 2026-04-25 | Source inspection and build | Pass | Direct spread registry, spread ignition queue, dispatch scoring store, water context probe/store, legacy suppression applier/store, response-state labels, and floating `FIRE`/`DEAD` markers are out of active source. |
| 2026-04-25 | Blueprint update | Pass | Blueprint components now use neutral `FireProfileSpec` data. |
| 2026-04-25 | Runtime bridge | Pass | Exposure controller projects grid activity into debug, damage, recovery, and visual snapshots while grid state becomes the source of truth. |
| 2026-04-25 | `bash scripts/test.sh` | Pass | Grid foundation tests increased the plain C# suite to 21 tests. |
| 2026-04-25 | `bash scripts/build.sh --launch` + startup log scan | Pass | Debug ignition now seeds grid state; startup logs showed `Prometheus (v0.2)` and no Prometheus errors in the scanned window. |
| 2026-04-25 | `bash scripts/test.sh` | Pass | Footprint sampling and aggregate grid reads increased the plain C# suite to 23 tests. |
| 2026-04-25 | `bash scripts/build.sh --launch` + startup log scan | Pass | Entity snapshots now sample grid state across renderer-derived footprints; startup logs remained clean in the scanned window. |
| 2026-04-25 | `bash scripts/test.sh` | Pass | Environment-rule coverage increased the plain C# suite to 26 tests: underwater ignition blocking, moisture/barrier dampening, and oxygen-driven ignition differences. |
| 2026-04-25 | `bash scripts/test.sh && bash scripts/build.sh --launch` + startup log scan | Pass | Script source is organized by feature area under `Scripts/Core`, `Scripts/Debug`, and `Scripts/Fire`; startup logs showed `Prometheus (v0.2)` with no scanned Prometheus errors. |
| 2026-04-25 | `bash scripts/test.sh && bash scripts/build.sh --launch` + startup log scan | Pass | Removed leftover response-profile filenames/helper names and unused debug snapshot factory; startup logs showed `Prometheus (v0.2)` with no scanned Prometheus errors. |
| 2026-04-25 | `bash scripts/test.sh && bash scripts/build.sh --launch` + startup log scan | Pass | Renamed the grid projection bridge to exposure and changed workplace operation logs to disabled/restored; startup logs showed `Prometheus (v0.2)` with no scanned Prometheus errors. |
| 2026-04-25 | `bash scripts/test.sh && bash scripts/build.sh --launch` + startup log scan | Pass | Renamed stale `Scripts/Fire/Simulation` folder to `Scripts/Fire/Profiles`; startup logs showed `Prometheus (v0.2)` with no scanned Prometheus errors. |
| 2026-04-25 | `bash -n scripts/build.sh && bash scripts/build.sh --help && bash scripts/test.sh` | Pass | Added `--test` and `--qa` startup workflows; plain C# suite remains at 26 passing tests. |
| 2026-04-25 | `bash scripts/build.sh --qa` | Superseded | The older QA workflow waited for Prometheus startup readiness after launch. That wait has been removed; `--qa` now exits after Timberborn is launched so Computer Use can drive startup dialogs and menu loading directly. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh --launch` + in-game inspection | Pass | Added `Prometheus` -> `QA` instruction/result panel backed by `~/Library/Application Support/Timberborn/PrometheusQA`; tests stayed at 26 passing, startup logs showed `Prometheus (v0.2)`, the panel rendered in-game, and a `Passed` result was appended/logged. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh` | Pass | Added the grid environment sampler/merge layer, moved profile-to-environment policy out of `FireExposureController`, and raised the plain C# suite to 29 passing tests. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh` | Pass | Added dependency-light terrain column sampling policy for terrain mass vs top-surface cells; plain C# suite is now 30 passing tests. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh --test` | Pass | Added active-cell heat/smoke/ember emission, exposed-face transfer limits, forest-line spread coverage, and vegetation profiles for common trees and bushes; plain C# suite is now 32 passing tests. |
| 2026-04-25 | CLI autoload + log scan | Blocked | `-settlementName "<settlement>" -saveName "<save>"` reaches Prometheus startup but crashes Timberborn behavior/navigation ticks, including the clean `Prometheus QA` / `beginning` save. Normal UI loading can still work; CLI autostart uses `LoadSceneInstantly(...)` while menu loading uses `LoadScene(...)`. |
| 2026-04-25 | `cliclick` menu automation | Superseded | The old coordinate automation proved the normal menu path once, but it is no longer the QA driver. Use Computer Use for startup dialogs, main menu loading, and in-game clicks. |
| 2026-04-25 | `bash scripts/build.sh --qa` + manual Prometheus QA panel | Superseded | Loaded `Prometheus QA`, opened `Prometheus` -> `QA`, confirmed the instruction/result buttons rendered, clicked `Passed`, and saw `event=qa_result_recorded result=passed` in `Fire.log`. Repeat this with Computer Use rather than `cliclick`. |
| 2026-04-25 | `cliclick` + `screencapture` tight QA loop | Superseded | Replaced by Computer Use screenshots and clicks. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh --qa` | Superseded | The live QA instruction file targeted forest-spread/grid validation. `--qa` now runs tests, deploys, clears logs, launches Timberborn, and exits for Computer Use navigation. |
| 2026-04-26 | `bash scripts/test.sh` + `bash scripts/build.sh --qa` + startup log scan | Pass | Reworked spread into a field-first resource model: grid transfer carries heat/ember/smoke only, entities own stochastic ignition, moisture evaporation, fuel depletion, tree death at 25% fuel loss, and burned-out char at zero fuel. Plain C# suite is now 36 passing tests and startup logs showed Prometheus loaded with no scanned exceptions. |
| 2026-04-26 | `bash scripts/build.sh --qa` + `cliclick` ignite pass + log scan | Pass | Live Pine ignition consumed moisture, crossed the 25% tree-death threshold, reached zero fuel, extinguished as burned out, and left a charred remnant. A follow-up build reduced high-speed `burning_tick` telemetry to 16 rows for the burn with no scanned Player.log errors. |
| 2026-04-26 | `git tag pre-phase-3-stabilization` | Pass | Tagged the pre-stabilization baseline before Phase 2 sprint orchestration began. |
| 2026-04-26 | Stabilization board integration | Pass | Integrated P2S-001 through P2S-008 plus P2S-010, P2S-011, and P2S-012. This added the architecture map, split grid/debug/test surfaces, centralized component-cache and reflection probes, named grid policies, source attribution, and the grid simulation coordinator. |
| 2026-04-26 | `git diff --check` + `bash scripts/test.sh` | Pass | Latest integrated stabilization check after P2S-008 passed with 53 plain C# tests. |
| 2026-04-26 | P2S-009 candidate: `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, startup log scan | Blocked | Candidate branch `codex/P2S-009-add-reset-registry` at `2576cf1` now passes diff check and 56 tests after merging current `main`. It adds reset hook failure isolation, stale Unity-reference pruning, visual reset null-safety, and deferred per-entity reset hook registration until `Awake`, but live QA is still blocked before reset can be exercised. |
| 2026-04-26 | Stabilization backlog review | Pass | Added [stabilization backlog](stabilization/BACKLOG.md) for ticket-sized follow-ups not yet represented by board files: runtime visual projection migration, grid telemetry bounds evidence, burst attribution injection, terrain/top-surface aftermath eligibility, and Phase 2 exit evidence matrix. |
| 2026-04-26 | P2S-009 live QA attempts | Blocked | Candidate branch reached persistent `LOADING` after the QA save load started, with logs stopping after `Good group Juice has no goods`. Current `main` reached the main menu after `--qa`, proving the readiness gate is startup-only and does not prove save-load completion. Manual `cliclick` and Computer Use input did not activate the visible `Continue` button in the observed run. |
| 2026-04-26 | `bash scripts/test.sh` + `bash scripts/build.sh --launch` + manual Continue path | Pass | Fixed the current `main` QA-save load lock by removing the global grid `IUpdatableSingleton` and stepping the shared grid coordinator from awakened fire-profiled entities instead. Manual `Return`, `Return`, `Continue`, `Yes` loaded `Prometheus QA - 2026-04-26 18h52m, Day 3-2.autosave` into the settlement; `Player.log` showed `Load time: 11776ms` plus native visual resolution logs and no Prometheus exception. |
| 2026-04-27 | `bash -n scripts/build.sh` + `bash scripts/build.sh --help` | Pass | Removed the old QA readiness wait and `cliclick` menu automation from `scripts/build.sh`. `--qa` now runs tests, deploys, clears logs, launches Timberborn, and exits for Computer Use navigation. |
| 2026-04-27 | P2S-009: `git diff --check` + `bash scripts/test.sh` + `bash scripts/build.sh --qa` + Computer Use reset QA | Pass | Reset registry is integrated. The QA save loaded through Computer Use with `Load time: 12000ms`; `Reset Fire State` reported 989 entities and logs recorded `runtime_reset_registry_started`, `runtime_reset_registry_completed failures=0`, and `debug_reset_fire_exposure result=success`. |
| 2026-04-27 | P2S-013: `git diff --check` + `bash scripts/test.sh` + `bash scripts/build.sh --qa` + Computer Use startup | Pass | Configured `FireProfileSpec` source fields now inject heat, embers, and smoke into the grid with `ConfiguredSource:<entityId>` attribution and conservative `RequiresOperation` gating. Plain C# suite is now 60 passing tests. Computer Use reached the main menu; startup logs showed Prometheus loaded with no scanned Prometheus errors. No live `grid_source_*` rows appeared during menu startup because the currently authored deployed profiles do not emit sources before a save is loaded. |
| 2026-04-27 | P2S-013 integration on `main`: `git diff --check` + `bash scripts/test.sh` + `bash scripts/build.sh --qa` + Computer Use startup | Pass | Configured source injection is integrated with P2S-014 environment sampling, P2S-015 runtime projection, and P2S-016 visual catalog changes. Plain C# suite is now 69 passing tests. Computer Use reached the main menu; startup logs showed Prometheus loaded and `environment=deferred:terrain/block/water/soil_runtime_probe` with no scanned Prometheus errors. |
| 2026-04-27 | P2S-017 integration on `main`: `git diff --check` + `bash scripts/test.sh` + `bash scripts/build.sh --qa` + Computer Use startup | Pass | Effect appliers now route direct/cached component assumptions through the integration facade, reset registry entity discovery uses the same facade lookup, and the beaver clear path no-ops when the reset setter API is missing instead of applying a compensating mutation. Plain C# suite is 74 passing tests. Computer Use reached the main menu; startup logs showed Prometheus loaded and the compatibility summary with no scanned Prometheus exceptions. |
| 2026-04-27 | P2S-022 integration on `main`: `git diff --check` + `bash scripts/test.sh` | Pass | Added Fertile Ash field amendment runtime state keyed by fire-grid coordinate with duration, charges, consume, expiry, reset, and debug count. Plain C# suite is now 78 passing tests. |
| 2026-04-27 | P2S-019 integration on `main`: `git diff --check` + `bash scripts/test.sh` + `bash scripts/build.sh` | Pass | Native gatherable discovery found no authored ash natural-resource template, so the safe wrapper uses Timberborn recovered-good stacks and validates `IGoodService.HasGood("FertileAsh")` before queueing. Plain C# suite is now 81 passing tests. Live collection proof remains for P2S-020/P2S-021. |
| 2026-04-27 | P2S-023 worker branch: `git diff --check` + `bash scripts/test.sh` + `bash scripts/build.sh --qa` + startup log scan | Pass | Fertile Ash field amendments now reduce eligible crop `Growable.GrowthTimeInDays` by 10%, exclude trees and bushes through component classification, tick amendment expiry from a runtime singleton, and restore base growth time on expiry/reset. Plain C# suite is now 83 passing tests. Startup logs showed Prometheus loaded with no scanned Prometheus errors. |

## Durable Context

- Phase 1 live QA previously validated ignition, spread, extinguish, damage, dead/ash terminal behavior, and `Reset Fire Sim` clean-slate recovery.
- The Prometheus debug UI uses TimberUi and Moddable Tool Groups through `Prometheus` -> `Actions`, `Visuals`, `Selection`, `QA`, and `Log`.
- The `QA` panel reads live instructions from `~/Library/Application Support/Timberborn/PrometheusQA/instructions.md` and appends `Passed` / `Failed` / `Blocked` results to `~/Library/Application Support/Timberborn/PrometheusQA/results.md`.
- Timberborn can autoload saves from the command line with `-settlementName "<settlement>" -saveName "<save without .timber>"`; experimental saves are used when the game is in experimental mode. Treat this as unsafe for live QA on the current mod stack because autostart uses `LoadSceneInstantly(...)` rather than the normal menu `LoadScene(...)` path.
- `bash scripts/build.sh --qa` runs tests, deploys, clears logs, launches Timberborn, then exits. It does not wait for Prometheus startup, menu state, or save-load completion.
- Normal menu loading is currently viable again on `main`: on 2026-04-26, Computer Use drove `Return`, `Return`, `Continue`, `Yes` and loaded the latest `Prometheus QA` autosave into the settlement.
- Current verified Prometheus toolbar coordinates at 1920x1080 can drift by active Timberborn tool groups; prefer screenshot-confirmed clicks before recording live QA evidence.
- Use Computer Use for in-game QA clicks, screenshots, and menu evidence.
- The visual authoring tool remains available for `Smoke`, `Ash`, `Steam`, `Fire`, `Sparks`, and `Char`, including selected-entity temporary preview and JSON/log export.
- `Reset Fire State` is now backed by `FireResetRegistry`: global runtime-state hooks are registered once, while entity reset hooks are discovered from loaded ComponentCache entries only when the reset command runs. Do not hold singleton delegates to transient entity components.
- `scripts/build.sh` rewrites external build-project Prometheus compile items to point at the active worktree source, avoiding stale sibling-worktree DLLs during ticket worktree validation.
- Old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding has been pruned from active content; Fertile Ash remains the core post-fire resource direction.
- Fertile Ash currently has a narrow recovered-good stack wrapper and a field-amendment crop growth effect, not an end-to-end production/application loop. The next proof must show visible stack spawn, builder collection, storage, and clean logs.

Source of truth: current UI labels and telemetry event names should be checked in source rather than copied here.

## Open Blockers

| Blocker | Status | Next Check |
| --- | --- | --- |
| Sparse 3D grid needs propagation/profile validation | Active | Configured source injection is dependency-light verified; next live slice should load a save with an authored emitting source and capture `grid_source_injected` plus downstream ignition evidence. |
| Fertile Ash recovered-good live proof | Active | P2S-020 should spawn one valid `FertileAsh` recovered-good stack, prove visible collection/storage, and scan `Player.log` / `Fire.log`. |
| CLI autoload crashes saves after Prometheus startup | Mitigated | Use normal menu loading for live QA. `--qa` launches Timberborn and then hands navigation to Computer Use. |
| Runtime visuals need reconnection to grid state | Active | Keep authoring tool intact, then map grid fire state into visual rules. |
| Timberborn menu automation map is missing | Active | Create a screenshot-backed map of main menu, Escape menu, in-game toolbar groups, Prometheus group entries, and keyboard controls before assigning more UI-heavy QA work to agents. |
| Explosion request/apply policy needs broader re-validation | Carryover | Use [VALIDATION/explosion-policy.md](VALIDATION/explosion-policy.md) if gaps reappear. |
| Worker/building exposure needs Phase 2 live validation | Carryover | Validate after the grid model stabilizes. |
| Unity asset import workflow is still manual | Carryover | Document or script after Unity license/import path is stable. |

## Next Exact Action

Continue Wave E with P2S-020 integration/QA and orchestrator verification of P2S-023. P2S-020 owns the live Fertile Ash spawn/collection/storage proof; P2S-024/P2S-025 own farmhouse/farmer application after the recovered-good path is proven.

## Resume Checklist

- [ ] Run `bash scripts/test.sh`.
- [ ] Run `bash scripts/build.sh --launch` for in-game QA loops.
- [ ] Use `bash scripts/build.sh --qa` when the next step benefits from tests, deployment, cleared logs, and a fresh Timberborn launch before Computer Use navigation.
- [ ] Use normal menu loading for live QA; CLI `-settlementName` / `-saveName` currently crashes after Prometheus startup.
- [ ] Open `Prometheus` -> `QA`; confirm the current instruction appears and result buttons are visible.
- [ ] Open `Prometheus` -> `Visuals`; confirm Timberborn object selection still works while the panel is open.
- [ ] Select a Bakery, platform, tree, and berry bush; confirm the Visuals target summary and JSON target kind are readable.
- [ ] Apply one particle effect and the full preset, then `Clear Preview`; confirm particles and material overrides are removed.
- [ ] Trigger `Reset Fire State`; confirm registry telemetry completes with `failures=0`.
- [ ] Record any new verified behavior here before ending meaningful work.

## References

| Need | Source |
| --- | --- |
| Build/deploy details | `bash scripts/build.sh --help` |
| Validation gates | [TEST_PLAN.md](TEST_PLAN.md) |
| Durable design | [DESIGN.md](DESIGN.md) |
| Repo map | [INDEX.md](INDEX.md) |
