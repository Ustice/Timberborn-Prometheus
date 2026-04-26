# Prometheus Handoff

## Current Focus

Last updated: 2026-04-26

Prometheus is moving into the 3D grid fire rewrite. The old entity-neighbor spread and responder-first runtime model has been removed from active source so the new sparse chunked cellular system can land without legacy behavior mixed in.

Phase 2 stabilization is now running from the file board under `docs/stabilization/tickets/`. Wave A, Wave B, and the unblocked Wave C tickets through P2S-012 are integrated; P2S-009 is intentionally blocked until the reset action is exercised in live QA.

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
| 2026-04-25 | `bash scripts/build.sh --qa` | Pass | QA workflow ran tests, compiled/deployed, stopped the previous Timberborn process, cleared logs, launched Steam app 1062090, and reached `ready` after Prometheus startup was detected. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh --launch` + in-game inspection | Pass | Added `Prometheus` -> `QA` instruction/result panel backed by `~/Library/Application Support/Timberborn/PrometheusQA`; tests stayed at 26 passing, startup logs showed `Prometheus (v0.2)`, the panel rendered in-game, and a `Passed` result was appended/logged. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh` | Pass | Added the grid environment sampler/merge layer, moved profile-to-environment policy out of `FireExposureController`, and raised the plain C# suite to 29 passing tests. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh` | Pass | Added dependency-light terrain column sampling policy for terrain mass vs top-surface cells; plain C# suite is now 30 passing tests. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh --test` | Pass | Added active-cell heat/smoke/ember emission, exposed-face transfer limits, forest-line spread coverage, and vegetation profiles for common trees and bushes; plain C# suite is now 32 passing tests. |
| 2026-04-25 | CLI autoload + log scan | Blocked | `-settlementName "<settlement>" -saveName "<save>"` reaches Prometheus startup but crashes Timberborn behavior/navigation ticks, including the clean `Prometheus QA` / `beginning` save. Normal UI loading can still work; CLI autostart uses `LoadSceneInstantly(...)` while menu loading uses `LoadScene(...)`. |
| 2026-04-25 | `cliclick` menu automation | Pass | Verified `osascript` activation plus `cliclick` Return/Return/click events can drive Timberborn's normal menu path; `bash scripts/build.sh --qa` now uses that path when `cliclick` is installed. |
| 2026-04-25 | `bash scripts/build.sh --qa` + `cliclick` Prometheus QA panel | Pass | Loaded `Prometheus QA`, opened `Prometheus` -> `QA`, confirmed the instruction/result buttons rendered, clicked `Passed`, and saw `event=qa_result_recorded result=passed` in `Fire.log`. |
| 2026-04-25 | `cliclick` + `screencapture` tight QA loop | Pass | Verified a single shell command can activate Timberborn, click a coordinate, wait briefly, and capture the result image for immediate inspection. |
| 2026-04-25 | `bash scripts/test.sh` + `bash scripts/build.sh --qa` | Pass | QA launcher rebuilt/deployed and reached Prometheus readiness; the live QA instruction file now targets forest-spread/grid validation. The pre-launch pause is now configurable with `LAUNCH_DELAY_SECONDS` and defaults to 15 seconds to give Steam/Timberborn more room after deployment. |
| 2026-04-26 | `bash scripts/test.sh` + `bash scripts/build.sh --qa` + startup log scan | Pass | Reworked spread into a field-first resource model: grid transfer carries heat/ember/smoke only, entities own stochastic ignition, moisture evaporation, fuel depletion, tree death at 25% fuel loss, and burned-out char at zero fuel. Plain C# suite is now 36 passing tests and startup logs showed Prometheus loaded with no scanned exceptions. |
| 2026-04-26 | `bash scripts/build.sh --qa` + `cliclick` ignite pass + log scan | Pass | Live Pine ignition consumed moisture, crossed the 25% tree-death threshold, reached zero fuel, extinguished as burned out, and left a charred remnant. A follow-up build reduced high-speed `burning_tick` telemetry to 16 rows for the burn with no scanned Player.log errors. |
| 2026-04-26 | `git tag pre-phase-3-stabilization` | Pass | Tagged the pre-stabilization baseline before Phase 2 sprint orchestration began. |
| 2026-04-26 | Stabilization board integration | Pass | Integrated P2S-001 through P2S-008 plus P2S-010, P2S-011, and P2S-012. This added the architecture map, split grid/debug/test surfaces, centralized component-cache and reflection probes, named grid policies, source attribution, and the grid simulation coordinator. |
| 2026-04-26 | `git diff --check` + `bash scripts/test.sh` | Pass | Latest integrated stabilization check after P2S-008 passed with 53 plain C# tests. |
| 2026-04-26 | P2S-009 candidate: `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, startup log scan | Blocked | Candidate branch `codex/P2S-009-add-reset-registry` at `16bcc3d` passed diff check, 45 tests, QA readiness, and clean startup log scan, but the live `Reset Fire State` action has not been clicked and verified after the registry change. |
| 2026-04-26 | Stabilization backlog review | Pass | Added [stabilization backlog](stabilization/BACKLOG.md) for ticket-sized follow-ups not yet represented by board files: runtime visual projection migration, grid telemetry bounds evidence, burst attribution injection, terrain/top-surface aftermath eligibility, and Phase 2 exit evidence matrix. |

## Durable Context

- Phase 1 live QA previously validated ignition, spread, extinguish, damage, dead/ash terminal behavior, and `Reset Fire Sim` clean-slate recovery.
- The Prometheus debug UI uses TimberUi and Moddable Tool Groups through `Prometheus` -> `Actions`, `Visuals`, `Selection`, `QA`, and `Log`.
- The `QA` panel reads live instructions from `~/Library/Application Support/Timberborn/PrometheusQA/instructions.md` and appends `Passed` / `Failed` / `Blocked` results to `~/Library/Application Support/Timberborn/PrometheusQA/results.md`.
- Timberborn can autoload saves from the command line with `-settlementName "<settlement>" -saveName "<save without .timber>"`; experimental saves are used when the game is in experimental mode. Treat this as unsafe for live QA on the current mod stack because autostart uses `LoadSceneInstantly(...)` rather than the normal menu `LoadScene(...)` path.
- `bash scripts/build.sh --qa` can drive the normal menu path with `cliclick`: wait `LAUNCH_DELAY_SECONDS` after deployment/log clearing, activate Timberborn, wait, press Return twice, wait, then click `Continue` at `960,323`. Tune launch/menu timing with `LAUNCH_DELAY_SECONDS` and `QA_MENU_*` environment variables, or disable menu automation with `QA_MENU_AUTOMATION=0`.
- Current verified Prometheus toolbar coordinates at 1920x1080 can drift by active Timberborn tool groups; prefer screenshot-confirmed clicks before recording live QA evidence.
- Use this tight click-and-see loop for in-game QA:
  `osascript -e 'tell application id "com.mechanistry.timberborn" to activate' && sleep 0.2 && cliclick c:<x>,<y> && sleep 0.7 && screencapture -x /tmp/timberborn-tight-loop.png`
- The visual authoring tool remains available for `Smoke`, `Ash`, `Steam`, `Fire`, `Sparks`, and `Char`, including selected-entity temporary preview and JSON/log export.
- `Reset Fire Sim` must clear fire, damage, recovery, preview, and pending debug-ignition state without changing saved design data.
- Old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding has been pruned from active content; Fertile Ash remains the core post-fire resource direction.

Source of truth: current UI labels and telemetry event names should be checked in source rather than copied here.

## Open Blockers

| Blocker | Status | Next Check |
| --- | --- | --- |
| Sparse 3D grid needs propagation/profile validation | Active | Resource lifecycle now has one live Pine pass; next slice should validate stochastic field ignition from a separate heat source, profile differences, and readable dry-brown feedback on a visible target. |
| CLI autoload crashes saves after Prometheus startup | Mitigated | Use normal menu loading for live QA. `--qa` now attempts the verified `cliclick` menu path instead of relying on CLI instant load. |
| Runtime visuals need reconnection to grid state | Active | Keep authoring tool intact, then map grid fire state into visual rules. |
| P2S-009 reset registry needs live reset evidence | Blocked | On candidate branch `codex/P2S-009-add-reset-registry` at `16bcc3d`, load the QA save, open `Prometheus` -> `Actions`, click `Reset Fire State`, then check logs for `runtime_reset_registry_started`, `runtime_reset_registry_completed`, `runtime_reset_hook_failed`, and any exceptions or crash. |
| Explosion request/apply policy needs broader re-validation | Carryover | Use [VALIDATION/explosion-policy.md](VALIDATION/explosion-policy.md) if gaps reappear. |
| Worker/building exposure needs Phase 2 live validation | Carryover | Validate after the grid model stabilizes. |
| Unity asset import workflow is still manual | Carryover | Document or script after Unity license/import path is stable. |

## Next Exact Action

Unblock P2S-009 before continuing Wave D:

1. Switch to candidate branch `codex/P2S-009-add-reset-registry` at `16bcc3d`.
2. Build and launch with `bash scripts/build.sh --qa`.
3. Open `Prometheus` -> `Actions` in the loaded QA save.
4. Click `Reset Fire State`.
5. Check `Player.log` and `Fire.log` for reset registry start/completion/failure telemetry and crashes.
6. If clean, move P2S-009 back through `verify` and `integration`, merge it, rerun checks, and only then proceed to Wave D.

## Resume Checklist

- [ ] Run `bash scripts/test.sh`.
- [ ] Run `bash scripts/build.sh --launch` for in-game QA loops.
- [ ] Use `bash scripts/build.sh --qa` when the next step benefits from automated test + launch + normal-menu continue automation + startup readiness waiting.
- [ ] Use normal menu loading for live QA; CLI `-settlementName` / `-saveName` currently crashes after Prometheus startup.
- [ ] Open `Prometheus` -> `QA`; confirm the current instruction appears and result buttons are visible.
- [ ] Open `Prometheus` -> `Visuals`; confirm Timberborn object selection still works while the panel is open.
- [ ] Select a Bakery, platform, tree, and berry bush; confirm the Visuals target summary and JSON target kind are readable.
- [ ] Apply one particle effect and the full preset, then `Clear Preview`; confirm particles and material overrides are removed.
- [ ] Trigger `Reset Fire Sim`; confirm active visual previews clear without breaking selection behavior.
- [ ] Record any new verified behavior here before ending meaningful work.

## References

| Need | Source |
| --- | --- |
| Build/deploy details | `bash scripts/build.sh --help` |
| Validation gates | [TEST_PLAN.md](TEST_PLAN.md) |
| Durable design | [DESIGN.md](DESIGN.md) |
| Repo map | [INDEX.md](INDEX.md) |
