# Prometheus Test Plan

This runbook is the authoritative test flow for current Prometheus validation work.

## Scope

Validate:

- Prometheus runtime load + blueprint type resolution,
- Fire debug instrumentation visibility/copy workflow,
- Phase 1 regression safety after closure,
- Phase 2 ember-field spread, dampening, and visual-state readability,
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
- [ ] Ember-field emission, dampening, barrier, ignition-threshold, and Fertile Ash source rule tests pass before Phase 2 tuning changes land.
- [ ] Visual intensity rules for embers, smoke, fire, steam, and char pass before tuning the particle/material adapter.
- [ ] Debug panel UI changes are manually QA'd; automated UI tests are intentionally out of scope for now.

Unity EditMode tests are deferred until the standalone repo has a clean Timberborn/Unity dependency story. The first automated lane is plain C# regression coverage because it catches decision drift without loading the full game assembly graph.

### A. Mod load and type registration

- [ ] `Player.log` contains `- Prometheus (v0.2)` (or newer).
- [ ] No startup exception containing `No type found for key FireResponseProfileSpec`.

### B. Prometheus panel instrumentation

- [ ] Bottom-left `Prometheus Debug` panel opens above the Timberborn bottom bar without overlapping the selected-building details panel.
- [ ] Panel sections are visually distinct (`Status`, `Commands`, `Filters`, `Selection`, `Log`) and remain readable at the default game UI scale.
- [ ] Global panel tabs (`Actions`, `Visuals`, `Selection`, `Log`) hide inactive sections and keep the open panel short enough for normal QA.
- [ ] Buttons have readable fill/outline contrast against the panel background.
- [ ] Button hover state is visibly lighter than resting state, and mouse-down state is brighter until release.
- [ ] Primary QA commands are grouped together and visible when the panel is open: `Reset Fire Sim`, `Stop Fires`, `Clear Beavers`, and `Clear Log`.
- [ ] Selecting a Prometheus-profiled entity (e.g., Bakery or Explosives Factory) updates the panel `Selection` section.
- [ ] The selected-building details panel does not show a separate Prometheus debug fragment.
- [ ] `Copy` in the panel selection section copies the full selected-entity snapshot text.
- [ ] `Ignite` in the panel selection section queues ignition for the selected fire-profiled entity.

### C. Prometheus icon/assets pass

- [ ] Prometheus goods/buildings display custom gold outline icons consistent with base-game styling instead of borrowed placeholder stock icons.
- [ ] Asset import shows no missing sprite/icon warnings for Fertile Ash, ember/fire state effects, fireworks extensions, or current placeholder concepts.

## Core gameplay validation sequence

### 1) Smoke pass (single settlement)

- [ ] Place/activate Prometheus-targeted building(s).
- [ ] Let simulation run several in-game hours.
- [ ] Confirm no lockup, no runaway notification spam.

### 2) Single-front behavior pass

Create one manageable front near dry fuel, wet/soaked terrain, and a cleared/firebreak edge. Observe:

- `Intensity`,
- `Spread pressure`,
- ember source type/radius/intensity,
- moisture/steam dampening,
- fuel susceptibility and ignition threshold result,
- barrier/firebreak result,
- visual state transitions.

Pass criteria:

- [ ] Ember pressure changes are smooth and attributable.
- [ ] Moisture visibly reduces pressure and shows light steam.
- [ ] State transitions are sensible (embers -> smoke/smolder -> fire -> charred/extinguished, where applicable).

### 2a) Phase 1 regression check

- [ ] Ignite one fire-profiled building.
- [ ] Confirm fire can spread or apply pressure to a nearby valid target.
- [ ] Confirm `Stop All Fires` extinguishes active fire.
- [ ] Drive one building to dead/ash.
- [ ] Confirm dead/ash does not keep burning.
- [ ] Click `Reset Fire Sim`.
- [ ] Confirm the entity is healthy/functioning again and can be re-ignited.

### 3) Ember-field spread pass

- [ ] Active fires emit readable ember fields.
- [ ] Dry fuel can ignite from ember pressure when configured thresholds are met.
- [ ] Moisture/soaked terrain reduces ember intensity and shows light steam when dampening heat.
- [ ] Firebreaks/cleared terrain block or sharply reduce propagation.
- [ ] Configured high-intensity operating buildings (e.g., Smelter) can produce ember fields.
- [ ] Lower-risk heat buildings (e.g., Bakery) do not produce ember fields unless explicitly configured.
- [ ] Fireworks and unstable explosive events can create short-lived ember fields without adding a fireworks-control minigame.
- [ ] Smoke, active fire, steam, embers, and charred presentation match runtime states.
- [ ] Reset Fire Sim clears active particles and char tint on loaded fire-profiled entities.
- [ ] `Visual Tuning` sliders visibly change ember/smoke/fire/steam/char intensity without a rebuild.
- [ ] Legacy text markers stay off by default and only appear when `Text markers` is enabled.
- [ ] Valid charred vegetation/buildings expose Fertile Ash source state.

### 3a) Worker/beaver exposure pass

- [ ] Confirm assigned workers inside burning buildings receive the intended Phase 2 exposure effects.
- [ ] Confirm assigned worker exposure does not depend on the worker being physically near the building transform.
- [ ] Confirm nearby beavers are affected by proximity without colony-wide spillover.
- [ ] Confirm workers recover after fire pressure clears or `Reset Fire Sim` is used.

### 4) Ember-field validation pass (carryover gate)

Run across each profile (`Low`, `Standard`, `High`):

- [ ] dry vegetation spread lane,
- [ ] wet/soaked dampening boundary,
- [ ] firebreak/cleared-terrain boundary,
- [ ] Smelter-style ember source,
- [ ] Bakery-style non-emitter,
- [ ] fireworks or unstable explosive burst source.

Pass criteria:

- [ ] Ember intensity changes are visible and attributable.
- [ ] Moisture produces readable steam and meaningful dampening.
- [ ] Fuel, barriers, and thresholds behave consistently.
- [ ] Low/Standard/High profiles produce sensible differences without runaway spread or visual spam.

## Tuning order when failing a gate

Apply in this order to avoid chasing symptoms:

1. Ember emission intensity/radius/falloff
2. Fuel susceptibility and ignition thresholds
3. Moisture/steam dampening strength
4. Firebreak/barrier blocking strength
5. Visual-effect thresholds for smoke, fire, steam, embers, and char

## Results recording template

| Date | Profile | Ember spread | Moisture/steam | Source profiles | Visual states | Fertile Ash | Outcome | Tuned values |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD | Low/Standard/High | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | |

## Session handoff notes

Before ending a test session:

- [ ] Copy one representative debug snapshot into notes.
- [ ] Update `DESIGN.md` carryover checklist status.
- [ ] Add changelog row for any tuning changes with rationale.
- [ ] Record unresolved blockers as one-line bullets for next session pickup.
