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
| Cooling/decay update | Active cell intensity decreases deterministically | Test output + log sample |
| 27-direction neighbor pass | Fire pressure can evaluate adjacent cells in 3D | Passing plain C# test |
| Reset clears grid | `Reset Fire State` clears active grid, preview, damage, recovery, workplace, beaver, visual, and ash state | Panel screenshot + `runtime_reset_registry_completed failures=0` log sample |
| Chunk boundary propagation | Fire pressure can cross chunk boundaries without duplicate/missing cells | Passing plain C# test |

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
| YYYY-MM-DD |  |  | Pass/Fail |  |  |

## Session Closeout

- [ ] Copy one representative debug snapshot into notes or handoff.
- [ ] Update [HANDOFF.md](HANDOFF.md) with new verified results, blockers, and next action.
- [ ] Update [DESIGN.md](DESIGN.md) only when a durable design decision, milestone, or accepted default changes.
- [ ] Add archive/changelog detail only when the history is useful after the next startup.
