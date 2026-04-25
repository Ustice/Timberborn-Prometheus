# Prometheus Test Plan

This is the authoritative runbook for active Prometheus validation.

## Source Of Truth

| Topic | Source |
| --- | --- |
| Build and launch behavior | `bash scripts/build.sh --help` |
| Automated test behavior | `bash scripts/test.sh` and `tests/Prometheus.Tests` |
| Runtime telemetry names | `FireTelemetryEvents` |
| Current live state | [HANDOFF.md](HANDOFF.md) |
| Durable design gates | [DESIGN.md](DESIGN.md) |

## Preflight

- [ ] Run `bash scripts/test.sh`.
- [ ] Run `bash scripts/build.sh --launch` for in-game QA.
- [ ] Confirm Timberborn launches with Prometheus enabled.
- [ ] Confirm fresh logs are available for the measurement window.
- [ ] Keep each repro scoped to one intent when possible.

## Runtime Sanity

- [ ] `Player.log` contains `- Prometheus (v0.2)` or newer.
- [ ] No startup exception for Prometheus blueprint/type registration.
- [ ] Moddable Tool Groups shows `Prometheus` with `Actions`, `Visuals`, `Selection`, and `Log`.
- [ ] Opening a Prometheus submenu returns Timberborn to normal selection after the panel opens.
- [ ] Selecting a Prometheus-profiled entity updates the panel.
- [ ] `Copy`, `Ignite`, `Reset Fire Sim`, `Stop Fires`, `Clear Beavers`, and `Clear Log` are manually QA'd through the current UI.

Source of truth: exact UI labels and control construction live in the debug UI source; this checklist defines behavior to verify, not an inventory to keep synchronized.

## Temporary Removal-Pass Regression

- [ ] Ignite one fire-profiled building.
- [ ] Confirm no old entity-neighbor/direct spread behavior occurs.
- [ ] Confirm `Stop Fires` extinguishes active fire.
- [ ] Drive one building to dead/ash.
- [ ] Confirm dead/ash does not keep burning.
- [ ] Click `Reset Fire Sim`.
- [ ] Confirm the entity is healthy/functioning again and can be re-ignited.

## 3D Grid Foundation Validation

Use this section as the next validation gate once the sparse grid lands.

| Scenario | Expected Result | Evidence |
| --- | --- | --- |
| Debug ignition writes grid state | Selected target creates an active grid fire snapshot | Panel screenshot + `Fire.log` |
| Cooling/decay update | Active cell intensity decreases deterministically | Test output + log sample |
| 27-direction neighbor pass | Fire pressure can evaluate adjacent cells in 3D | Plain C# test |
| Reset clears grid | `Reset Fire Sim` clears active grid, preview, damage, and recovery state | Panel screenshot + log sample |
| Chunk boundary propagation | Fire pressure can cross chunk boundaries without duplicate/missing cells | Plain C# test |

## Visual Authoring QA

- [ ] Open `Prometheus` -> `Visuals`.
- [ ] Confirm Timberborn object selection still works while the panel is open.
- [ ] Select a Bakery, platform, tree, and berry bush; confirm target summaries are readable.
- [ ] Apply one effect and the full preset to supported entities.
- [ ] Change native source/search/preset values and confirm the armed preview updates.
- [ ] `Clear Preview` removes temporary particles/material overrides.
- [ ] `Reset Fire Sim` clears active visual previews without changing selection behavior.
- [ ] `Copy JSON` / `Log JSON` include selected target context.

## Ember/Grid Spread Validation Matrix

Run across each profile once behavior is coherent.

| Profile | Dry fuel propagation | Moisture/steam dampening | Firebreak/barrier | High-risk source | Low-risk non-source | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| Low | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |
| Standard | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |
| High | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |

Pass criteria:

- [ ] Propagation is visible and attributable.
- [ ] Moisture produces readable dampening feedback.
- [ ] Fuel, barriers, and thresholds behave consistently.
- [ ] Low/Standard/High profiles differ without runaway spread or visual spam.

## Worker And Beaver Exposure

- [ ] Assigned workers inside burning buildings receive intended exposure effects.
- [ ] Assigned worker exposure does not depend on the worker being physically near the building transform.
- [ ] Nearby beavers are affected by proximity without colony-wide spillover.
- [ ] Workers recover after fire pressure clears or `Reset Fire Sim` is used.

## Explosion Policy

Use [VALIDATION/explosion-policy.md](VALIDATION/explosion-policy.md) when explosion ignition behavior is active or gaps reappear.

## Evidence Template

| Date | Scenario | Command / Profile | Result | Evidence Path | Notes |
| --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD |  |  | Pass/Fail |  |  |

## Session Closeout

- [ ] Copy one representative debug snapshot into notes or handoff.
- [ ] Update [HANDOFF.md](HANDOFF.md) with new verified results, blockers, and next action.
- [ ] Update [DESIGN.md](DESIGN.md) only when a durable design decision, milestone, or accepted default changes.
- [ ] Add archive/changelog detail only when the history is useful after the next startup.
