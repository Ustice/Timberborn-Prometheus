# Prometheus Test Plan

This is the authoritative runbook for active Prometheus validation.

## Source Of Truth

| Topic | Source |
| --- | --- |
| Build and launch behavior | `bash scripts/build.sh --help` |
| Automated test behavior | `bash scripts/test.sh` and `tests/Prometheus.Tests` |
| Runtime telemetry names | `FireTelemetryEvents` and QA panel telemetry strings in source |
| Current live state | [HANDOFF.md](HANDOFF.md) |
| Durable design gates | [DESIGN.md](DESIGN.md) |

## Preflight

- [ ] Run `bash scripts/test.sh`.
- [ ] Run `bash scripts/build.sh --launch` for in-game QA.
- [ ] Prefer `bash scripts/build.sh --qa` when you want tests, deployment, cleared logs, and a fresh Timberborn launch before Computer Use navigation.
- [ ] If Steam or Timberborn is slow after deployment, tune `LAUNCH_DELAY_SECONDS`; the default is 15 seconds.
- [ ] Use normal menu loading for live QA; CLI `-settlementName "<settlement>" -saveName "<save without .timber>"` uses Timberborn's instant scene-load path and currently crashes after Prometheus startup.
- [ ] Confirm Timberborn launches with Prometheus enabled.
- [ ] Confirm fresh logs are available for the measurement window.
- [ ] Keep each repro scoped to one intent when possible.

## Runtime Sanity

- [ ] `Player.log` contains `- Prometheus (v0.2)` or newer.
- [ ] No startup exception for Prometheus blueprint/type registration.
- [ ] Moddable Tool Groups shows `Prometheus` with `Actions`, `Visuals`, `Selection`, `QA`, and `Log`.
- [ ] Opening a Prometheus submenu returns Timberborn to normal selection after the panel opens.
- [ ] Selecting a Prometheus-profiled entity updates the panel.
- [ ] `Copy`, `Ignite`, `Reset Fire State`, `Stop Fires`, `Clear Beavers`, and `Clear Log` are manually QA'd through the current UI.

## In-Game QA Channel

- [ ] Write the next instruction to `~/Library/Application Support/Timberborn/PrometheusQA/instructions.md`.
- [ ] Open `Prometheus` -> `QA`.
- [ ] Confirm the instruction text refreshes in-game without restarting Timberborn.
- [ ] Click `Passed`, `Failed`, or `Blocked`.
- [ ] Confirm `~/Library/Application Support/Timberborn/PrometheusQA/results.md` receives a timestamped entry with the note and instruction text.
- [ ] Confirm `Fire.log` records the QA result event.

Last verified at 1920x1080 on 2026-04-25: `Prometheus` root around `632,1043`, `QA` child around `1024,970`, and `Passed` recorded `event=qa_result_recorded result=passed`.

Use Computer Use screenshots and clicks for coordinate-based in-game checks so the action and visual evidence stay together.

Source of truth: exact UI labels and control construction live in the debug UI source; this checklist defines behavior to verify, not an inventory to keep synchronized.

## Temporary Removal-Pass Regression

- [ ] Ignite one fire-profiled building.
- [ ] Confirm no old entity-neighbor/direct spread behavior occurs.
- [ ] Confirm `Stop Fires` extinguishes active fire.
- [ ] Drive one building to dead/ash.
- [ ] Confirm dead/ash does not keep burning.
- [ ] Click `Reset Fire State`.
- [ ] Confirm the entity is healthy/functioning again and can be re-ignited.

## 3D Grid Foundation Validation

Use this section as the next validation gate once the sparse grid lands.

| Scenario | Expected Result | Evidence |
| --- | --- | --- |
| Debug ignition writes grid state | Selected target creates an active grid fire snapshot | Panel screenshot + `Fire.log` |
| Configured source writes grid state | `FireProfileSpec` heat/ember/smoke source fields create attributed grid pressure without direct nearest-target ignition | Passing plain C# test + `grid_source_injected` live log when an emitting source is loaded |
| Cooling/decay update | Active cell intensity decreases deterministically | Test output + log sample |
| 27-direction neighbor pass | Fire pressure can evaluate adjacent cells in 3D | Passing plain C# test |
| Reset clears grid | `Reset Fire State` clears active grid, preview, damage, recovery, workplace, beaver, visual, and ash state | Panel screenshot + `runtime_reset_registry_completed failures=0` log sample |
| Chunk boundary propagation | Fire pressure can cross chunk boundaries without duplicate/missing cells | Passing plain C# test |

## Fertile Ash Recovery Validation

- [x] Spawn `FertileAsh` only from valid aftermath sources.
- [x] Confirm the visible recovered-good stack appears at valid coordinates.
- [x] Confirm builders can collect the stack.
- [x] Confirm collected `FertileAsh` enters normal storage that accepts the good.
- [x] Confirm field amendments reduce eligible crop `Growable` growth time in dependency-light control-vs-amended rules.
- [x] Confirm trees and bushes are excluded from the field-amendment growth rule.
- [x] Confirm `Reset Fire State` clears stale ash runtime state without deleting unrelated Timberborn entities unsafely.
- [x] Scan `Player.log` and `Fire.log` for Prometheus and recovered-good exceptions.
- [ ] Confirm farmhouse-driven `FertileAsh` consumption applies a field amendment in a live save.
- [ ] Confirm a farmhouse-amended crop grows faster than a nearby control crop in a live save.

## Visual Authoring QA

- [ ] Open `Prometheus` -> `Visuals`.
- [ ] Confirm Timberborn object selection still works while the panel is open.
- [ ] Select a Bakery, platform, tree, and berry bush; confirm target summaries are readable.
- [ ] Apply one effect and the full preset to supported entities.
- [ ] Change native source/search/preset values and confirm the armed preview updates.
- [ ] `Clear Preview` removes temporary particles/material overrides.
- [ ] `Reset Fire State` clears active visual previews without changing selection behavior.
- [ ] `Copy JSON` / `Log JSON` include selected target context.

## Ember/Grid Spread Validation Matrix

Run across each profile once behavior is coherent.

Current QA caveat: CLI autoload reaches Prometheus startup but crashes Timberborn behavior/navigation ticks, including the clean `Prometheus QA` / `beginning` save. The CLI path calls Timberborn's instant scene loader, while normal menu loading uses the non-instant scene-loader path. As of 2026-04-26, current `main` loads the latest `Prometheus QA` autosave through `Return`, `Return`, `Continue`, `Yes`; use that normal-menu path before treating forest-spread QA as a Prometheus failure.

| Profile | Dry fuel propagation | Moisture/steam dampening | Firebreak/barrier | High-risk source | Low-risk non-source | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| Low | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |
| Standard | Partial Pass | Not Run | Not Run | Not Run | Not Run | One forced Pine ignition verified moisture/fuel lifecycle and no neighbor cascade. |
| High | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |

Pass criteria:

- [ ] Propagation is visible and attributable.
- [ ] Moisture evaporates before full burning and produces readable brown/desiccated feedback.
- [ ] Fuel depletion behaves like fire health: trees die after 25% fuel loss, burn out at zero fuel, stop contributing fuel, and remain as charred stumps/remnants.
- [ ] Ignition is stochastic from sampled local field strength, fuel, oxygen, moisture, and profile threshold rather than a deterministic all-neighbor cascade.
- [ ] Fuel, barriers, and thresholds behave consistently.
- [ ] Low/Standard/High profiles differ without runaway spread or visual spam.

## Worker And Beaver Exposure

- [ ] Assigned workers inside burning buildings receive intended exposure effects.
- [ ] Assigned worker exposure does not depend on the worker being physically near the building transform.
- [ ] Nearby beavers are affected by proximity without colony-wide spillover.
- [ ] Workers recover after fire pressure clears or `Reset Fire State` is used.

## Explosion Policy

Use [VALIDATION/explosion-policy.md](VALIDATION/explosion-policy.md) when explosion ignition behavior is active or gaps reappear.

## Evidence Template

| Date | Scenario | Command / Profile | Result | Evidence Path | Notes |
| --- | --- | --- | --- | --- | --- |
| 2026-04-26 | Forced Pine ignition resource lifecycle | Standard | Partial Pass | `/tmp/prometheus-throttled-ignite-24s.png` + `Fire.log` | Moisture reached zero, fuel crossed 0.25 death threshold, fuel reached burnout, and throttled telemetry emitted 16 burn rows with no scanned Player.log errors. |
| 2026-04-27 | Configured source dependency-light propagation | High-risk source | Pass | `bash scripts/test.sh` | Source fields produce attributed grid pressure, respect `RequiresOperation`, and can create nonzero stochastic ignition probability through grid propagation. Live menu startup had no emitting source rows because deployed authored profiles are zero-source at startup. |
| 2026-04-27 | Effect facade and reset registry startup | Workplace, beaver, damage, recovery | Pass | `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log` | Effect appliers resolve direct/cached Timberborn components through the integration facade; reset registry discovery uses the same facade lookup. Computer Use reached the main menu and startup logs showed Prometheus loaded with no scanned Prometheus exceptions. |
| 2026-04-27 | Fertile Ash recovered-good wrapper | Recovery | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Fire.log`, Computer Use | Native ash gatherable template was not confirmed; wrapper queues `FertileAsh` through Timberborn recovered-good stacks after good-registration validation. Live QA captured `fertile_ash_recovered_good_stack_queued`, `fertile_ash_spawn_queued`, visible Rubble with `Fertile ash 1`, and District Center storage with `Fertile ash 7`. |
| 2026-04-27 | Fertile Ash field amendment crop growth | Recovery | Dependency-Light Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log` | Eligible crop growables receive a 10% growth-speed buff from active field amendments; trees and bushes are excluded. Startup logs showed Prometheus loaded with no scanned Prometheus errors. Live farmhouse/farmer application remains owned by P2S-024/P2S-025. |
| 2026-04-27 | Fertile Ash recovered-good spawn and storage | Recovery | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Fire.log`, Computer Use | Valid charred Pine aftermath queued native recovered-good stacks at `49,3,7` and `23,4,11`; Computer Use confirmed visible Rubble with `Fertile ash 1`, and District Center storage showed `Fertile ash 7` after beaver pickup. Logs had one de-duplicated soil-moisture sample warning and no scanned recovered-good exception. |
| 2026-04-27 | Fertile Ash reset telemetry | Recovery | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log`, Computer Use | Ash recovered-good queue telemetry clears through `Reset Fire State` while leaving Timberborn-owned recovered-good entities alone. Live reset logged `fertile_ash_reset_state queuedStacks=0 queuedAmount=0 source=none sourceKind=none damageCategory=none nativeStacksDestroyed=0 reason=native_recovered_good_stack_owned_by_timberborn`, followed by `runtime_reset_registry_completed failures=0`. |
| 2026-04-27 | Sprint guardrails | Repo | Pass | `git diff --check`, `bash scripts/test.sh` | Guardrails now fail the plain C# suite if internal Markdown ships under `Assets/Mods/Prometheus`, dependency-light compile items drift from `Prometheus.Tests.csproj`, or QA-facing telemetry event tokens disappear from `FireTelemetryEvents.All`. |
| 2026-04-27 | Fertile Ash farmhouse amendment | Recovery | Blocked | `bash scripts/test.sh`, `bash scripts/build.sh --qa`, copied save fixture, `Player.log`, `Fire.log` | Prototype branch `codex/P2S-025-implement-farmhouse-amendment` at `e2c70ac` passed 90 tests and built/deployed. The copied `Prometheus P2S-025 QA` fixture crashed during `Timberborn.DwellingSystem.Dweller.Load`; a local repair removed `Dweller` components with `Home: null`, but the repaired copy hung during load after component-cache resolution. Required evidence remains ash consumption, `fertile_ash_farmhouse_amendment_applied`, and faster amended crop growth than a nearby control from a fresh loadable fixture. |
| 2026-04-27 | Stabilization closeout docs | Repo | Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log` | P2S-027 passed closeout validation with 89 plain C# tests. `--qa` deployed the current branch, cleared logs, and launched Timberborn. `Player.log` showed `- Prometheus (v0.2)` and startup initialization; `Fire.log` recorded the compatibility summary. No scanned Prometheus exceptions were present. Source-driven spread remains dependency-light/startup-clean until a live emitting-source fixture exists. Faster crop growth remains dependency-light only, not live farmhouse-applied. |
| YYYY-MM-DD |  |  | Pass/Fail |  |  |

## Session Closeout

- [ ] Copy one representative debug snapshot into notes or handoff.
- [ ] Update [HANDOFF.md](HANDOFF.md) with new verified results, blockers, and next action.
- [ ] Update [DESIGN.md](DESIGN.md) only when a durable design decision, milestone, or accepted default changes.
- [ ] Add archive/changelog detail only when the history is useful after the next startup.
