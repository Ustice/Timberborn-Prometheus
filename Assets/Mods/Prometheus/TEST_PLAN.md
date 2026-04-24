# Prometheus Test Plan

This runbook is the authoritative test flow for current Prometheus validation work.

## Scope

Validate:

- Prometheus runtime load + blueprint type resolution,
- Fire debug instrumentation visibility/copy workflow,
- Phase 1 regression safety after closure,
- Phase 2 dispatch stability and faction asymmetry,
- Phase 2 worker/beaver exposure inside burning buildings,
- Carryover Phase 5 balancing gates.

## Preflight checklist

- [ ] Unity scripts compile successfully.
- [ ] `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll` exists.
- [ ] Build and deploy to game mod folder with `scripts/build.sh`.
- [ ] `Player.log`/`Player-prev.log` are cleared before a fresh repro when debugging startup issues.
- [ ] Timberborn launches with Prometheus enabled.

## Runtime sanity checks

### 0) Automated regression checks

- [ ] `bash scripts/test.sh` completes successfully.
- [ ] Runtime store and decision-rule tests pass.
- [ ] Test results are available at `TestResults/Prometheus.Tests.trx`.
- [ ] Coverage output is available under `TestResults/*/coverage.cobertura.xml`.
- [ ] Any new real system decision has a corresponding regression test where feasible.
- [ ] Unity-specific components stay thin; dependency-light rule/runtime classes carry testable decisions where feasible.
- [ ] Telemetry event-name constants remain unique and iterable for docs, filters, and future log tooling.
- [ ] Faction quenching identity and dispatch lock/hysteresis rule tests pass before Phase 2 tuning changes land.
- [ ] Debug panel UI changes are manually QA'd; automated UI tests are intentionally out of scope for now.

Unity EditMode tests are deferred until the standalone repo has a clean Timberborn/Unity dependency story. The first automated lane is plain C# regression coverage because it catches decision drift without loading the full game assembly graph.

### A. Mod load and type registration

- [ ] `Player.log` contains `- Prometheus (v0.2)` (or newer).
- [ ] No startup exception containing `No type found for key FireResponseProfileSpec`.

### B. Prometheus panel instrumentation

- [ ] Bottom-left `Prometheus Debug` panel opens above the Timberborn bottom bar without overlapping the selected-building details panel.
- [ ] Panel sections are visually distinct (`Status`, `Commands`, `Filters`, `Selection`, `Log`) and remain readable at the default game UI scale.
- [ ] Primary QA commands are grouped together and visible when the panel is open: `Reset Fire Sim`, `Stop Fires`, `Clear Beavers`, and `Clear Log`.
- [ ] Selecting a Prometheus-profiled entity (e.g., Bakery or Explosives Factory) updates the panel `Selection` section.
- [ ] The selected-building details panel does not show a separate Prometheus debug fragment.
- [ ] `Copy` in the panel selection section copies the full selected-entity snapshot text.
- [ ] `Ignite` in the panel selection section queues ignition for the selected fire-profiled entity.

### C. Prometheus icon/assets pass

- [ ] Prometheus goods/recipes display custom gold outline icons consistent with base-game goods styling instead of borrowed placeholder stock icons.
- [ ] Asset import shows no missing sprite/icon warnings for ash fertilizer, firefighting foam, fireworks crates, bucket brigade kits, or fire-control gear.

## Core gameplay validation sequence

### 1) Smoke pass (single settlement)

- [ ] Place/activate Prometheus-targeted building(s).
- [ ] Let simulation run several in-game hours.
- [ ] Confirm no lockup, no runaway notification spam.

### 2) Single-front behavior pass

Create one manageable front near water/logistics and observe:

- `Intensity`,
- `Spread pressure`,
- `Quenching`,
- `Candidate score` vs `Assigned score`,
- `Assignment locked`, `Retarget suppressed`,
- `Response state` transitions.

Pass criteria:

- [ ] Assigned score changes are smooth.
- [ ] Lock/hysteresis reduces micro-retargeting.
- [ ] State transitions are sensible (`Overwhelmed` -> `Contained` -> `Stabilized`, where applicable).

### 2a) Phase 1 regression check

- [ ] Ignite one fire-profiled building.
- [ ] Confirm fire can spread or apply pressure to a nearby valid target.
- [ ] Confirm `Stop All Fires` extinguishes active fire.
- [ ] Drive one building to dead/ash.
- [ ] Confirm dead/ash does not keep burning.
- [ ] Click `Reset Fire Sim`.
- [ ] Confirm the entity is healthy/functioning again and can be re-ignited.

### 3) Faction asymmetry pass (short)

- [ ] Folktails: weaker suppression on farther/low-water front versus near-water front.
- [ ] Ironteeth: stronger suppression as fire intensity rises.

### 3a) Worker/beaver exposure pass

- [ ] Confirm assigned workers inside burning buildings receive the intended Phase 2 exposure effects.
- [ ] Confirm assigned worker exposure does not depend on the worker being physically near the building transform.
- [ ] Confirm nearby beavers are affected by proximity without colony-wide spillover.
- [ ] Confirm workers recover after fire pressure clears or `Reset Fire Sim` is used.

### 4) Dual-front validation pass (carryover gate)

Run for each faction (`Folktails`, `Ironteeth`) across each profile (`Low`, `Standard`, `High`):

- [ ] Front A near water/core logistics,
- [ ] Front B farther away,
- [ ] capture one representative responder per front.

Pass criteria:

- [ ] Anti-thrash behavior holds (lock windows + hysteresis suppression).
- [ ] Faction behavior remains distinct and fair.
- [ ] Response-state notifications are useful and not noisy.

## Tuning order when failing a gate

Apply in this order to avoid chasing symptoms:

1. `DispatchRetargetHysteresisThreshold`
2. `DispatchAssignmentLockDurationInSeconds`
3. Dispatch weights (`Severity` / `AssetRisk` / `TravelCost` / `ContainmentLeverage`)
4. Faction asymmetry coefficients (Folktails distance penalty, Ironteeth high-heat bonus)

## Results recording template

| Date | Faction | Profile | Front A | Front B | Anti-thrash | States | Outcome | Tuned values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD | Folktails | Low/Standard/High | | | Pass/Fail | O/C/S | Pass/Fail | |
| YYYY-MM-DD | Ironteeth | Low/Standard/High | | | Pass/Fail | O/C/S | Pass/Fail | |

Legend: `O/C/S` = `Overwhelmed` / `Contained` / `Stabilized`.

## Session handoff notes

Before ending a test session:

- [ ] Copy one representative debug snapshot into notes.
- [ ] Update `DESIGN.md` carryover checklist status.
- [ ] Add changelog row for any tuning changes with rationale.
- [ ] Record unresolved blockers as one-line bullets for next session pickup.
