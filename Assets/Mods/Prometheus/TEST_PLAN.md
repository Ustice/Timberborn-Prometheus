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
- [ ] `TimberUi` and `ModdableToolGroups` are installed and available as required dependencies.
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
- [ ] Visual intensity rules for local smoke, fire, steam, and char pass before tuning the particle/material adapter; sparks are reserved for ember fields.
- [ ] Debug panel UI changes are manually QA'd; automated UI tests are intentionally out of scope for now.

Unity EditMode tests are deferred until the standalone repo has a clean Timberborn/Unity dependency story. The first automated lane is plain C# regression coverage because it catches decision drift without loading the full game assembly graph.

### A. Mod load and type registration

- [ ] `Player.log` contains `- Prometheus (v0.2)` (or newer).
- [ ] No startup exception containing `No type found for key FireResponseProfileSpec`.

### B. Prometheus panel instrumentation

- [ ] Moddable Tool Groups bottom-bar group `Prometheus` appears with submenu entries `Actions`, `Visuals`, `Selection`, and `Log`.
- [ ] Each submenu entry opens the same TimberUi panel instance above the Timberborn bottom bar and switches to the matching view.
- [ ] No old in-panel tab row appears.
- [ ] Panel sections are visually distinct (`Commands`, `Effect Inspector`, `Selection`, `Filters`, `Log`) and remain readable at the default game UI scale.
- [ ] Buttons, close control, toggles, search fields, scrollbars, and sliders use TimberUi/base-game styling.
- [ ] Visible debug-panel controls are created through TimberUi parent extension methods, matching the upstream `TimberUiDemo` pattern.
- [ ] Button and submenu labels remain readable and match native Timberborn panels.
- [ ] No visible debug-panel text renders with raw black Unity styling.
- [ ] The close button is fully clickable and positioned by TimberUi without custom z-index/offset styling.
- [ ] Panel spacing reads as native Timberborn spacing from TimberUi containers, without doubled section padding or odd per-control gaps.
- [ ] The panel does not show a custom Prometheus frame/title strip behind the TimberUi sections.
- [ ] The panel baseline has no visible custom `style.*` overrides; any remaining layout issue should first be checked against TimberUi container/component choices.
- [ ] Filter buttons stay readable in both selected and unselected states.
- [ ] Log rows show a full native `View` button without clipping or single-letter fallback text.
- [ ] Log filters do not crowd or overlap search/autoscroll controls.
- [ ] Log entry counts appear in the Log filters area rather than in a shared Status section.
- [ ] Primary QA commands are grouped together and visible when the panel is open: `Reset Fire Sim`, `Stop Fires`, `Clear Beavers`, and `Clear Log`.
- [ ] Selecting a Prometheus-profiled entity (e.g., Bakery or Explosives Factory) updates the panel `Selection` section.
- [ ] Selecting a Prometheus-profiled entity still works while the detached debug panel is open, as long as the click is outside the visible panel bounds.
- [ ] After opening `Prometheus` -> `Selection`/`Visuals`/`Log`, the active tool returns to normal selection and building clicks still select entities.
- [ ] The selected-building details panel does not show a separate Prometheus debug fragment; the hidden selection hook only forwards state.
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
- [ ] Local smoke, active fire, steam, and charred presentation match runtime states, with no sparks from local object fire progression.
- [ ] `Fire.log` records `native_visual_effect_resolved` for Timberborn-sourced particle channels when visible effects are initialized; current expected sources are `Sparks_Trail`, `SmelterSmoke`, `CampfireFire`, and `SteamEngineSmoke`.
- [ ] Any `native_visual_effect_unavailable` fallback channel is captured with the selected entity, save, and visible effect state.
- [ ] Reset Fire Sim clears active particles and char tint on loaded fire-profiled entities.
- [ ] `Prometheus` -> `Visuals` opens the replacement effect inspector for `Smoke`, `Ash`, `Steam`, `Fire`, `Sparks`, and `Char`.
- [ ] With the Visuals panel open, Timberborn selection still works and the target summary updates for a Bakery, platform, tree, and berry bush.
- [ ] For particle effects, enabled, native source, intensity, emission, position X/Y/Z, size, lifetime, speed, alpha, RGB color, spread, and size-over-lifetime presets visibly affect temporary previews without a rebuild.
- [ ] Advanced particle controls expose velocity, gravity, noise, rotation, shape mode, and sorting/order.
- [ ] Recommended native sources appear first, expandable/searchable all-native sources appear second, and changing a source reclones the preview while preserving tuning values.
- [ ] `Reset` in the Visuals inspector restores the promoted defaults from the current tuning pass: `FoodFactorySmoke`, `BadwaterRigSmoke`, `CoffeeBrewerySmoke`, `CampfireFire`, and `Sparks_Trail` with their saved position/lifetime/spread/gravity/noise values.
- [ ] `Apply Effect`, `Apply Preset`, and `Clear Preview` work on selected supported entities without changing fire simulation, damage state, recovery state, or entity profiles.
- [ ] Unsupported selected entities report clear feedback and log `supported=false` or an unsupported result without errors.
- [ ] `Copy JSON` and `Log JSON` include `version`, `selectedEffect`, `advancedEnabled`, `target`, `effects`, and `char`; `target.kind` is human-readable when discoverable.
- [ ] Char controls include cut amount, noise scale/contrast, edge width/depth, active glow, ash-edge brightness, black interior strength, seed/offset, tint strength, darkening, and tint color.
- [ ] Char preview uses only safe material-property overrides until shader inspection proves clipping/edge bands are supported or a custom shader path is chosen.
- [ ] Ember spread prevents sparks from reading as a single fixed point at normal zoom.
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
