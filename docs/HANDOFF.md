# Prometheus Handoff

## Current Focus

Last updated: 2026-04-25

Prometheus is moving into the 3D grid fire rewrite. The old entity-neighbor spread and responder-first runtime model has been removed from active source so the new sparse chunked cellular system can land without legacy behavior mixed in.

## Verified Since Last Checkpoint

| Date | Command / Evidence | Result | Notes |
| --- | --- | --- | --- |
| 2026-04-25 | `bash scripts/test.sh && bash scripts/build.sh --launch` | Pass | Removal pass launched Timberborn successfully. |
| 2026-04-25 | Source inspection and build | Pass | Direct spread registry, spread ignition queue, dispatch scoring store, water context probe/store, suppression applier/store, response-state labels, and floating `FIRE`/`DEAD` markers are out of active source. |
| 2026-04-25 | Blueprint update | Pass | Blueprint components now use neutral `FireProfileSpec` data instead of `FireResponseProfileSpec`. |
| 2026-04-25 | Runtime bridge | Pass | Temporary simulation controller supports debug ignition/cooling snapshots until grid state replaces it. |

## Durable Context

- Phase 1 live QA previously validated ignition, spread, extinguish, damage, dead/ash terminal behavior, and `Reset Fire Sim` clean-slate recovery.
- The Prometheus debug UI uses TimberUi and Moddable Tool Groups through `Prometheus` -> `Actions`, `Visuals`, `Selection`, and `Log`.
- The visual authoring tool remains available for `Smoke`, `Ash`, `Steam`, `Fire`, `Sparks`, and `Char`, including selected-entity temporary preview and JSON/log export.
- `Reset Fire Sim` must clear fire, damage, recovery, preview, and pending debug-ignition state without changing saved design data.
- Old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding has been pruned from active content; Fertile Ash remains the core post-fire resource direction.

Source of truth: current UI labels and telemetry event names should be checked in source rather than copied here.

## Open Blockers

| Blocker | Status | Next Check |
| --- | --- | --- |
| Sparse 3D grid not implemented yet | Active | Build dependency-light rules/state first. |
| Runtime visuals need reconnection to grid state | Active | Keep authoring tool intact, then map grid fire state into visual rules. |
| Explosion request/apply policy needs broader re-validation | Carryover | Use [VALIDATION/explosion-policy.md](VALIDATION/explosion-policy.md) if gaps reappear. |
| Worker/building exposure needs Phase 2 live validation | Carryover | Validate after the grid model stabilizes. |
| Unity asset import workflow is still manual | Carryover | Document or script after Unity license/import path is stable. |

## Next Exact Action

Implement the sparse chunked 3D fire grid foundation:

1. Add dependency-light grid coordinate/chunk/runtime state types.
2. Add plain C# tests for neighbor enumeration, chunk addressing, ignition/cooling persistence, and reset clearing.
3. Wire the temporary debug ignition bridge to write/read grid state.
4. Keep existing visual preview tooling functional while runtime visuals are reconnected.

## Resume Checklist

- [ ] Run `bash scripts/test.sh`.
- [ ] Run `bash scripts/build.sh --launch` for in-game QA loops.
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
