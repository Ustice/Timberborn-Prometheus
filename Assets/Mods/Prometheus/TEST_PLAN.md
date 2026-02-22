# Prometheus Test Plan

This runbook is the authoritative test flow for current Prometheus validation work.

## Scope

Validate:

- Prometheus runtime load + blueprint type resolution,
- Fire debug instrumentation visibility/copy workflow,
- Phase 2 dispatch stability and faction asymmetry,
- Carryover Phase 5 balancing gates.

## Preflight checklist

- [ ] Unity scripts compile successfully.
- [ ] `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll` exists.
- [ ] Deploy to game mod folder with `scripts/deploy_prometheus.sh`.
- [ ] `Player.log`/`Player-prev.log` are cleared before a fresh repro when debugging startup issues.
- [ ] Timberborn launches with Prometheus enabled.

## Runtime sanity checks

### A. Mod load and type registration

- [ ] `Player.log` contains `- Prometheus (v0.2)` (or newer).
- [ ] No startup exception containing `No type found for key FireResponseProfileSpec`.

### B. Entity panel instrumentation

- [ ] Selecting a Prometheus-profiled entity (e.g., Bakery override target) shows `Prometheus Fire Debug`.
- [ ] Debug output is selectable.
- [ ] **Copy** button copies full snapshot text.

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

### 3) Faction asymmetry pass (short)

- [ ] Folktails: weaker suppression on farther/low-water front versus near-water front.
- [ ] Ironteeth: stronger suppression as fire intensity rises.

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
