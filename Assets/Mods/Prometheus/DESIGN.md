# Prometheus Mod Design Document

## Overview

**Prometheus** is a fire-focused Timberborn mod that adds a full gameplay loop around:

1. **Ignition and spread** (fire as an ember-field cellular simulation)
2. **Readable fire states** (embers, steam, smoke, flame, and char)
3. **Recovery and renewal** (Fertile Ash harvested from charred sources)

The design target is a **balanced simulation**: fire should be dangerous enough to matter, but manageable with preparation and rewarding when used strategically.

---

## Vision and Pillars

### Vision

Create a coherent fire ecosystem where players can:

- fear uncontrolled wildfires,
- build fire-response infrastructure,
- and intentionally use controlled burns for long-term gain.

### Core pillars

1. **Meaningful danger**
   - Fire can spread and damage crops, trees, buildings, and beavers.

2. **Player agency**
   - Prepared players can contain and extinguish fires through terrain, moisture, firebreaks, and suppression fields.

3. **Faction identity**
   - Folktails and Ironteeth manipulate fire pressure differently without adding direct beaver-control minigames.

4. **Risk-reward ecology**
   - Fire can create temporary fertility and production opportunities.

5. **Readable simulation**
   - Players should always understand why fires started/spread, what is currently at risk, and how to respond.
   - Fire risk should be visible in the world, not only in panels: sparks and airborne embers around active fires should communicate spread pressure, similar to how badwater contamination makes soil risk legible.

---

## Design Goals

### Gameplay goals

- Add a systemic fire mechanic that integrates with water, dehydration, agriculture, forestry, and building safety.
- Make ember fields the core fire-spread mechanic before adding responder/suppression complexity.
- Enable intentional controlled burns for productivity gains.
- Extend base-game fireworks and unstable explosive content by adding ember-field risk.

### Player experience goals

- Preserve Timberborn's planning-first identity.
- Avoid constant chaos; keep fire events periodic and predictable enough to plan around.
- Ensure losses feel avoidable with good preparation.

### Technical goals

- Build in phases, keeping the mod playable after each phase.
- Reuse data-driven blueprints where possible.
- Add C# simulation hooks incrementally for advanced behavior (spread, quenching, exposure).

---

## Phase 2 Architecture Rules

Use these rules as guardrails before adding Phase 2 ember-field spread, fire presentation, and later suppression behavior.

1. **Keep Unity/Timberborn adapters thin**
   - Components that inherit `BaseComponent` or implement `IUpdatableComponent` should mostly read Unity/Timberborn state, call dependency-light `RuntimeState`/`Rules` code, and apply the resulting component changes.
   - Avoid putting durable gameplay decisions directly inside Appliers when the decision can live in plain C#.

2. **Make reset behavior a first-class contract**
   - Any Phase 2 effect that can be applied must define how it is cleared.
   - `Reset Fire Sim` should restore fire, damage, recovery, worker penalty, beaver exposure, and operational-suppression state back to a healthy/functioning baseline where feasible.
   - Dead/ash terminal behavior must remain terminal for burning, while reset must be able to explicitly restore it for QA and recovery flows.

3. **Keep ownership boundaries clear**
   - `RuntimeState` owns current facts and snapshots.
   - `Rules` compute decisions from facts without touching Unity or Timberborn components.
   - Appliers translate state-machine outcomes into Unity/Timberborn component changes and restore those changes when state clears.
   - Debug UI may command and observe systems, but should not become the owner of gameplay behavior.

4. **Test meaningful system decisions**
   - When a real decision lands, prefer extracting it into dependency-light rule/runtime code and adding a regression test.
   - Debug panel layout and workflow can remain manual QA while it is actively changing.

5. **Centralize fragile integration points**
   - Type-name matching, entity identity assumptions, and telemetry event names should be kept behind small helpers or registries.
   - Event names should use constants from an iterable registry so logs stay consistent and tests/docs can derive the available event set.

6. **Defer stable identity until persistence requires it**
   - Loaded-runtime Unity instance IDs remain acceptable for Phase 2 effects on loaded entities.
   - If effects need save/load continuity or unloaded-entity tracking, introduce a dedicated entity identity layer rather than scattering UUIDs or Timberborn-specific IDs through gameplay code.

---

## System Concept: Fire Lifecycle

### 1) Ignition

Potential fire sources:

### Building ignition risk profile (initial)

To keep fire readable and fair, building ignition risk should be data-driven and grouped by operational hazard profile.

- **Low risk** (early-game, normally supervised):
  - Campfire
- **Medium risk** (routine heat + fuel handling):
  - Grill
- **High risk** (industrial heat / combustion pressure):
  - Smelter
  - Ironteeth Engine
- **General rule:** wood-consuming industrial buildings should receive elevated ignition risk compared to non-combustion utility buildings.
- **Ember field rule:** only selected high-intensity fire-using buildings emit ambient ember fields while operating. A Bakery can have ignition risk without producing a field; a Smelter can produce a visible ember field because it represents industrial-scale heat and sparks.

Design constraints:

- Early-game essentials should remain mostly safe (avoid early frustration spikes).
- High-risk industry should reward safety investment (distance, suppression, alarm coverage).
- Risk values should be tuneable per difficulty profile (`Low` / `Standard` / `High`).
- Ember field sources should be data-driven per building profile, with separate tuning for radius, intensity, operating-state requirement, and whether the field is purely visual or contributes to ignition/spread pressure.

### Ember Field Sources

Ember fields are localized airborne spark/ember zones and the core fire-spread model. They make spread pressure visible and drive cellular-style propagation into nearby fuel. Each update evaluates local fuel, moisture, barriers, source intensity, and negative/suppressing fields before applying ignition or decay.

Candidate sources:

- active fires and high-risk firefronts,
- fireworks and fireworks-related shows,
- selected high-intensity fire-using buildings while operating,
- explosions or unstable-core events during their immediate aftermath.

Counter-fields and dampening:

- moisture reduces ember intensity and can produce light steam visuals,
- water bodies and soaked terrain strongly dampen propagation,
- Ironteeth suppression acts like a negative ember field that reduces local ember intensity,
- Folktail suppression primarily affects the burning target itself, lowering flame/smoke intensity and shortening burn duration.

Non-goals:

- Do not make every warm production building emit an ember field.
- Do not require player micromanagement of ember fields directly.
- Do not create separate UI beyond building placement/readability, visible particles, and existing debug/status surfaces.

### Fire Visual State Ladder

Fire state should be readable from in-world effects before the player opens a panel.

- **Embers**: airborne sparks around sources and spread-pressure fronts.
- **Steam**: light moisture reaction where water/soaked ground/suppression dampens heat.
- **Smoke**: smoldering/scorched state, worsening before open flame.
- **Fire**: active burning state with stronger light/flame effects.
- **Charred**: terminal burned state for buildings and vegetation, preferably represented by a shader/material tint so we avoid large new 3D asset sets.

The visual layer should be an adapter over fire runtime state, not a separate gameplay system.

### 2) Spread

Fire spread should be influenced by:

- **Ember field intensity**: local ember pressure is the primary propagation input
- **Fuel type**: crops and trees ignite/spread faster than stone-heavy structures
- **Dryness/moisture**: drier environments spread faster
- **Barriers**: water bodies and prepared firebreaks slow or stop spread
- **Wind/event modifiers** (optional advanced tuning)

### 3) Damage and Status

- **Crops/Trees**: healthy -> scorched -> burning -> dead
- **Buildings**: intact -> smoldering (efficiency penalty) -> burning -> ruined
- **Beavers**: heat exposure -> dehydration acceleration -> injury risk/death at extremes

### 4) Suppression

- Water, moisture, and suppression fields reduce fire/ember intensity.
- Ironteeth suppression primarily creates a negative ember field that dampens local propagation pressure.
- Folktail suppression primarily affects the thing on fire directly, lowering active burn intensity and duration.
- Dedicated fire-brigade/beaver relay systems are out of scope for now; Timberborn beavers are fungible enough that a pass-the-bucket chain does not currently justify a new system.
- Suppression materials, foam, helmets, suits, and gear are presentation/building flavor by default unless they earn a distinct gameplay role later.

### 5) Recovery and Renewal

- The only core post-fire resource is **Fertile Ash**.
- Players harvest Fertile Ash from charred vegetation and selected charred/ruined buildings.
- Emberpelts can later extend the resource loop by harvesting charcoal.
- Best outcomes come from physical containment: cleared terrain, water, and firebreaks.

---

## Faction Asymmetry (Later Suppression Layer)

## Folktails: Direct Dampening Doctrine

- Focus: rapid local response and village defense.
- Mechanics:
  - direct dampening of burning objects,
  - practical use of water/moisture at the fire target,
  - low-tech, scalable response without direct beaver control.
- Candidate content:
  - Emergency Cistern,
  - Fire Watch Tower.

## Ironteeth: Industrial Fire Control

- Focus: engineered suppression and hazard operations.
- Mechanics:
  - negative ember fields that reduce local propagation pressure,
  - stronger area denial around industrial districts,
  - suit/helmet/nozzle visuals as building flavor,
  - better operation in high-heat zones.
- Candidate content:
  - Pressurized Suppressant Station,
  - industrial fire watch/containment buildings.

## Emberpelts (Future extension)

- Focus: heat-adapted containment + post-fire resource conversion.
- Intended identity:
  - resilient near high-heat fronts,
  - effective at hotspot cleanup and re-ignition prevention,
  - strongest post-fire conversion from destruction into usable fuel.
- Candidate mechanics:
  - passive heat resistance bonus for responders,
  - close-range suppression action (e.g., tail-stamp extinguish animation/ability),
  - salvage charcoal from burnt trees/buildings (`Charred` state harvest),
  - optional ash-soil synergy from rapid Emberpelt salvage operations.
- Faction constraints to respect from source mod behavior:
  - wet-fur conditions hinder Emberpelt breeding,
  - avoid mechanics that force frequent water exposure without compensating tools.
- Balance intent:
  - lower raw front-wide suppression than Ironteeth,
  - less mass emergency manpower than Folktails,
  - superior stabilization and recovery value after contained fire events.

---

## Explosive Hazard Model

Explosive production/storage introduces a distinct hazard layer separate from baseline fire spread.

### Candidate explosive hazards

- Explosives Factory
- Explosives Warehouse / storage of explosive crates
- Placed/laid explosives (world objects)
- Unstable Cores
- Base-game fireworks

### Explosion behavior (design intent)

- Buildings containing explosives can **detonate when burning**.
- Explosive crates should behave like localized explosive payloads (conceptually similar to a dynamite stick per affected tile/stack segment).
- Placed explosives that catch fire should detonate.
- Fireworks keep their Timberborn 1.0 behavior and only gain Prometheus ember-field risk.

### Fire interaction policy

Default rule: **explosions do not create new fires**.

Rationale:

- Timberborn settlements commonly place explosives-adjacent infrastructure; auto-ignition from blasts may create excessive chain-frustration.
- Prevents accidental terrain-risk amplification in dense settlements where players use explosives as standard tools.
- Keeps explosion consequences strong but readable (blast damage and disruption) without runaway ignition cascades.

Optional advanced tuning (future): allow explosion/fireworks events to emit short-lived ember fields in higher-risk profiles without adding a separate fireworks-control system.

### Explosion ignition difficulty setting

Add a dedicated setting to control whether explosions can start fires:

- `ExplosionIgnitionMode = Off` (default)
- `ExplosionIgnitionMode = HighOnly` (enabled only under `High Fire Activity`)
- `ExplosionIgnitionMode = Always` (hardcore/custom)

When enabled, explosion ignition checks should:

- use low/moderate base chance,
- scale by explosive severity/source (e.g., crate < warehouse < factory/placed charge),
- be reduced by local moisture/flooding context.

Design intent: preserve settlement readability by default while allowing opt-in chain-reaction challenge for players who want higher risk.

### Blast consequence profile (first pass)

- Primary effect: local structural/health damage and suppression disruption.
- Secondary effect: temporary panic/load spike for response systems.
- No default terrain deformation from incidental building explosions unless explicitly tied to placed explosive entities.

---

## Positive Fire Mechanics

### Fertile Ash

- Recoverable post-burn material from burned vegetation and selected ruined structures.
- Effects:
  - usable as a fertility input,
  - supports post-fire recovery and renewal loops.
- Balance constraints:
  - available only after valid burned sources,
  - requires labor/logistics to collect,
  - should be useful without making uncontrolled fires profitable.

### Controlled Burns

- Player-authored land-management loop rather than a separate managed-burn ledger.
- Intended flow:

  1. Prepare containment with cleared terrain, water, and constructed firebreaks.

  2. Ignite selected vegetation or accept a natural/accidental ignition inside the prepared area.

  3. Wait for the fire to consume fuel and burn out.

  4. Use foresters to collect Fertile Ash from burned vegetation and eligible ruins.

- This mirrors Timberborn water management: players shape the environment, let the simulation run, then collect value from a successful setup.

---

## Balance Framework

Fire should be:

- **Threatening** enough to require preparation,
- **Recoverable** when players invest in response systems,
- **Profitable** only when managed correctly.

Suggested difficulty presets:

- **Low Fire Activity**: rare spread, gentle damage, longer response windows.
- **Standard**: baseline intended experience.
- **High Fire Activity**: frequent events, faster spread, stronger penalties.

---

## Current Implementation Status (in repo)

Implemented foundation in `Assets/Mods/Prometheus`:

- mod script assembly and startup logger,
- Fertile Ash good scaffold,
- initial `HeatStress` need and faction need-collection wiring,
- localization keys for new goods/status,
- pruned old bucket/foam/gear/fireworks goods and recipes from the active content path.

This is a scaffolding milestone and not yet full wildfire simulation.

---

## Phased Roadmap

## Phase 0 — Foundation (Completed)

**Goal:** establish IDs, economy primitives, and status hooks.

Delivered:

- data assets for fire-related goods/recipes,
- first adverse fire-related need (`HeatStress`),
- collection/localization wiring,
- script assembly entry point for future systems.

## Phase 1 — Core Fire Simulation (Done)

**Goal:** implement functional fire behavior.

Scope:

- ignition triggers,
- spread rules,
- quenching by water,
- damage ticks for crops/trees/buildings,
- beaver heat exposure interaction *(deferred to Phase 2 for beavers/workers inside burning buildings)*.

Exit criteria:

- Fires can start, spread, be extinguished, and cause/avoid losses predictably.

Status:

- Core ignition, spread, quenching, damage, terminal dead/ash behavior, and reset-to-healthy debug recovery have passed live QA.
- Remaining beaver/worker exposure behavior is owned by Phase 2 because it depends on workplace occupancy and responder gameplay.

## Phase 2 — Ember Field Spread and Fire Presentation

**Goal:** make the core fire mechanic coherent before adding suppression systems.

Scope:

- ember field propagation as the primary spread model,
- moisture and negative-field dampening,
- smoke/fire/steam/ember visual effects,
- charred-state presentation for buildings and vegetation,
- Fertile Ash source tagging on charred vegetation and selected ruined buildings.

Exit criteria:

- Fire spreads through visible ember pressure in a way that is readable, tuneable, and test-backed.
- Moisture visibly dampens fire pressure with light steam and meaningful spread reduction.
- Charred buildings/vegetation are visually distinct without requiring large new 3D asset sets.
- Fertile Ash can be harvested from valid charred sources.

### Phase 2 detailed delivery plan (next sprint)

This section turns Phase 2 into an implementation-ready sprint plan focused on **core spread behavior, visual readability, and post-burn resource output**.

#### Objectives

1. Replace direct neighbor-spread thinking with ember-field propagation rules.
2. Keep ember fields data-driven by source profile, fuel, moisture, barrier, and difficulty.
3. Add visual effects that communicate fire state: embers, steam, smoke, active fire, and char.
4. Keep suppression design thin for now: Ironteeth reduce ember fields; Folktails reduce the burning target.
5. Avoid new responder/brigade minigames until the fire-spread model itself is solid.

#### Scope slices

##### Slice A — Ember field cellular spread

- Add/update dependency-light rules for:
  - source emission intensity,
  - radius/falloff,
  - fuel susceptibility,
  - moisture dampening,
  - barrier/firebreak blocking,
  - ignition threshold and decay.
- Model ember fields as local spread pressure over nearby valid entities/tiles, not as a player-controlled tool.
- Candidate field sources:
  - active fires,
  - high-risk operating buildings such as Smelters,
  - fireworks,
  - explosions and Unstable Cores.

##### Slice B — Moisture and negative fields

- Moisture reduces ember intensity and spread chance.
- Moisture interaction should show light steam when heat is being dampened.
- Ironteeth suppression is modeled as a negative ember field.
- Folktail suppression affects the thing on fire directly instead of creating a brigade/relay system.

##### Slice C — Fire-state visual effects

- Add effects/adapters for:
  - embers around active spread-pressure sources,
  - smoke for smoldering/scorched states,
  - active flame for burning states,
  - steam when moisture or suppression dampens heat,
  - charred shader/material/tint for terminal burned buildings and vegetation.
- Prefer shaders/material overrides and reusable particle/effect adapters over bespoke replacement models.

##### Slice D — Fertile Ash recovery

- Treat **Fertile Ash** as the only core new resource.
- Valid sources:
  - charred vegetation,
  - selected charred/ruined buildings.
- Foresters should be the natural collection path for vegetation sources.
- Emberpelts can later harvest charcoal as an extension, but charcoal is not core Phase 2 scope.

#### Proposed technical work breakdown

1. **Rule extraction**
    - Add dependency-light ember-field rules for emission, decay, moisture, barrier blocking, and ignition thresholds.
2. **Runtime state extensions**
    - Add ember field snapshots/source metadata to runtime state, including visual intensity and gameplay intensity separately if needed.
3. **Controller integration**
    - Run ember-field propagation before applying ignition/damage transitions.
4. **Visual adapter pass**
    - Attach/update particle, material, or shader effects from fire runtime state.
5. **Recovery source pass**
    - Mark valid charred sources with Fertile Ash availability and collection eligibility.
6. **Instrumentation pass**
    - Emit telemetry for field source, intensity, dampening, ignition threshold result, and visual-state transitions.

#### Acceptance criteria (Phase 2 sprint gate)

- A fire creates an ember field that can ignite nearby valid fuel when thresholds are met.
- Moisture/firebreaks reduce or block ember propagation and show readable dampening feedback.
- High-risk operating buildings can emit ember fields when configured; low-risk buildings such as Bakeries do not by default.
- Fireworks and Unstable Core/explosive events can emit short-lived ember fields without changing base fireworks behavior.
- Smoke, fire, steam, embers, and charred presentation map cleanly to runtime fire states.
- Valid charred vegetation/buildings can produce Fertile Ash.
- Plain C# tests cover the new spread/dampening/resource decisions before tuning.

#### Test scenarios for this phase

1. **Vegetation spread line**
    - Expected: ember field propagates through dry fuel and stops/decays when fuel or threshold fails.
2. **Moisture dampening**
    - Expected: wet/soaked area lowers ember intensity, shows steam, and prevents or delays ignition.
3. **Industrial ember source**
    - Expected: configured Smelter emits ember pressure while operating; Bakery does not unless explicitly configured.
4. **Fireworks/explosive burst**
    - Expected: event emits a short-lived ember field without adding fireworks-control UI.
5. **Charred recovery**
    - Expected: valid charred vegetation/buildings expose Fertile Ash collection state.

#### Out of scope for this sprint

- Fire brigade/relay systems and direct responder-dispatch gameplay.
- Dedicated bucket/foam/gear goods loops.
- Major fireworks redesign beyond ember-field risk.
- Emberpelt charcoal economy beyond marking it as a future extension.

## Phase 3 — Renewal and Controlled Burn Economy

**Goal:** add strong positive incentives to fire mastery.

Scope:

- Fertile Ash recovery from burned vegetation and selected ruined structures,
- fertility/yield boosts,
- constructed firebreaks and cleared-terrain containment,
- forester-driven collection loop.

Exit criteria:

- Prepared burns can be net-positive without making uncontrolled fires desirable.
- Players can understand the loop as terrain/flame management: prepare, ignite or allow ignition, wait, collect.

## Phase 4 — Fireworks and Explosive Ember Sources

**Goal:** add Prometheus fire risk to existing explosive/fireworks content without redesigning fireworks.

Scope:

- ember fields from base-game fireworks,
- ember fields from Unstable Cores and explosive events,
- risk scaling by local ember profile, moisture, and cleared space.

Exit criteria:

- Fireworks and unstable explosive content can create ember pressure without adding a separate fireworks minigame.

## Phase 5 — Tuning, UX, and Compatibility

**Goal:** production-level polish.

Scope:

- balancing passes across game stages,
- clear visual/audio/tooltip feedback,
- compatibility and load-order testing.

Exit criteria:

- System is understandable, performant, and stable across typical playthroughs.

---

## Progress Tracking

### Developer workflow (build/deploy/debug)

Use this lightweight loop before each gameplay test session:

- Recompile scripts in Unity (ensure `Library/ScriptAssemblies/Timberborn.ModExamples.Prometheus.dll` is up to date).

- Build and deploy the mod payload and runtime DLL via `scripts/build.sh`.

- Launch Timberborn and confirm mod load in `Player.log` (`- Prometheus (v0.2)` or later).

- In-game, select a fire-profiled entity and use **Prometheus Fire Debug** (output is selectable, and **Copy** copies the full debug snapshot).

If blueprint load fails with `No type found for key FireResponseProfileSpec`, verify the installed mod contains:

- `~/Documents/Timberborn/Mods/Prometheus/Scripts/Timberborn.ModExamples.Prometheus.dll`

### Test plan reference

Primary QA runbook is in:

- `Assets/Mods/Prometheus/TEST_PLAN.md`

Use that file as the source of truth for smoke checks, ember-field validation matrix, and tuning sign-off criteria.

### Milestone checklist

- [x] Phase 1 — Core Fire Simulation (Done)
- [ ] Phase 2 — Ember Field Spread and Fire Presentation (In Progress)
- [ ] Phase 3 — Renewal and Controlled Burn Economy
- [ ] Phase 4 — Fireworks/Explosive Ember Integration
- [ ] Phase 5 — Tuning, UX, and Compatibility (In Progress)

### Carryover validation checklist (Phase 2 completion + Phase 5 polish)

- [ ] Keep plain C# regression tests green for gameplay decision logic before Phase 2 tuning changes land.

- [ ] When a real system decision lands, prefer extracting the decision into dependency-light rule/runtime code and adding a regression test for it.

- [ ] Run balancing pass across low/standard/high fire activity profiles for new Phase 2 behavior.

- [ ] Execute one full ember-field verification scenario pass and tune defaults *(protocol and template are ready below)*.

- [ ] Mark Phase 2 validation items complete in this document and append measured tuning outcomes to the change log.

### Ember-field verification protocol (ready to run)

Purpose: validate ember propagation, moisture dampening, visual state readability, and charred-resource output under representative fire pressure.

#### Setup

- Use one settlement save with dry vegetation, wet/soaked terrain, at least one firebreak/cleared gap, a Bakery, a Smelter, and a fireworks or explosive source.

- Run under all tuning profiles: `Low`, `Standard`, `High`.

- Observe at least one active fire source, one ember-field source building, and one dampened/wet boundary in the debug panel.

#### What to capture

- Ember fields: source ID/type, radius, intensity, falloff, remaining duration, and visual intensity.

- Dampening fields: moisture value, steam visual state, negative-field strength, barrier/firebreak result.

- Fire state transitions: embers, smoke/smoldering, active fire, charred, and extinguished.

- Recovery context: Fertile Ash source eligibility and amount.

#### Pass/fail thresholds (first balancing gate)

- **Propagation readability:** pass when dry vegetation ignites from sufficient ember pressure and the player can see why.

- **Moisture dampening:** pass when wet/soaked terrain reduces ember intensity, emits light steam, and prevents or delays ignition.

- **Source profile correctness:** pass when configured Smelters can emit ember fields while operating and Bakeries do not by default.

- **Visual-state mapping:** pass when smoke/fire/steam/embers/charred effects match runtime state transitions without requiring new replacement models.

- **Recovery output:** pass when valid charred vegetation/buildings expose Fertile Ash and invalid sources do not.

- **Profile robustness:** pass when behavior remains coherent in `Low` and `High` without runaway spread or visual spam.

#### Tuning adjustment order (if fail)

- Tune ember emission intensity/radius/falloff.

- Tune fuel susceptibility and ignition thresholds.

- Tune moisture/steam dampening strength.

- Tune firebreak/barrier blocking strength.

#### Completion rule

After one full ember-field pass across `Low`/`Standard`/`High`, update:

- `Run balancing pass across low/standard/high fire activity profiles...` to checked,

- `Execute one full ember-field verification scenario pass and tune defaults` to checked,

- change log with measured outcomes and any tuned default values.

### Quick playtest results template (copy/fill)

| Date | Profile | Ember spread | Moisture/steam | Source profiles | Visual states | Fertile Ash | Outcome | Tuning changes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD | Low/Standard/High | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | Pass/Fail | e.g. -10% radius |

Legend: source profiles should include at least one configured high-risk emitter and one explicitly non-emitting warm building.

### Next sprint plan (Phase 2 completion + Phase 5 balance pass)

Sprint goal: move from implementation-complete Phase 1 behavior to validated, tuned, and documented ember-field defaults.

#### Sprint backlog

- [ ] Replace remaining responder/dispatch-first assumptions with ember-field spread behavior.

- [ ] Add/test ember-field emission, radius, falloff, decay, and ignition thresholds.

- [ ] Add/test moisture dampening and light steam feedback.

- [ ] Add/test high-risk operating building ember sources and non-emitting warm buildings.

- [ ] Add/test fireworks/explosive short-lived ember sources.

- [ ] Add smoke/fire/steam/ember visual adapters from runtime fire state.

- [ ] Add charred shader/material/tint path for buildings and vegetation where feasible.

- [ ] Add Fertile Ash source tagging for valid charred vegetation/buildings.

- [ ] Execute ember-field verification protocol across `Low`/`Standard`/`High`.

- [ ] Verify visual readability and noise level under sustained fire events; adjust particle/effect thresholds if needed.

- [ ] Record final default values in the change log with before/after rationale.

- [ ] Mark remaining current sprint checklist items as done once validation gates pass.

#### Implementation order

- Implement dependency-light rules and tests first.

- Wire runtime state and debug telemetry second.

- Add visual adapters third.

- Apply final profile-wide balancing pass last.

#### Exit criteria for this next sprint

- Ember-field spread passes verification scenarios on `Low`/`Standard`/`High` with no runaway propagation.

- Fire visual-state transitions are understandable and not spammy in long events.

- Tuned defaults are documented in `DESIGN.md` change log and reflected in checklist completion.

### Phase 3 kickoff checklist (prepared)

Focus: begin **Renewal and Controlled Burn Economy** work immediately after Phase 2 validation lock.

- [ ] Define `Fertile Ash` as a recoverable resource state on burned vegetation and selected ruined structures, including source type, amount, and collection eligibility.

- [ ] Add balancing envelope for Fertile Ash rewards so contained burns are useful while escaped fires remain costly.

- [ ] Specify firebreak construction, cleared-terrain containment, and any ignition/tool prerequisites.

- [ ] Draft first implementation tasks for `FireRecoveryController`/`FireRecoveryEffectApplier` and forester collection integration against finalized Phase 2 fire intensity ranges.

- [ ] Add Phase 3 acceptance test scenarios (firebreak blocks spread, cleared terrain containment, burned vegetation becomes collectible Fertile Ash, foresters collect it, escaped fires remain net-negative).

- [ ] Add first Phase 3 UX tasks (player-facing status text/tooltips for Fertile Ash source and collection eligibility).

### Sprint close-out (2026-02-21)

- Status: **Implementation complete; validation carryover active**
- Scope result: all implementation checklist items were delivered.
- Carryover blockers: requires in-game validation/tuning pass (ember-field + profile balancing).
- Next sprint handoff: finish Phase 2/5 validation checklist, then start Phase 3 kickoff checklist.

### Change log

Active/planned entries only. Full historical log moved to `DESIGN_CHANGELOG_ARCHIVE.md` (project root).

| Date | Phase | Update | Status |
| --- | --- | --- | --- |
| 2026-04-24 | Phase 2 Content | Pruned old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding; renamed ash fertilizer content to Fertile Ash | Done |
| 2026-04-24 | Phase 2 Design | Reordered Phase 2 around ember-field cellular spread, moisture/steam dampening, fire-state visuals, charred presentation, and Fertile Ash before suppression/responder systems | Planned |
| 2026-04-24 | Phase 2 Architecture | Extracted faction quenching and dispatch lock/hysteresis decisions into dependency-light rules with regression coverage | Done |
| 2026-04-24 | Phase 2 Architecture | Centralized Prometheus telemetry event names into an iterable registry and added a regression test for uniqueness/key coverage | Done |
| 2026-04-24 | Phase 2 UX | Reorganized the Prometheus debug panel into Timberborn-style status, command, filter, selection, and log sections while keeping debug UI in the manual-QA lane | In Validation |
| 2026-04-24 | Phase 2 | Added test-backed beaver exposure rules and first indoor worker exposure path for assigned workers in burning workplaces | In Validation |
| 2026-04-24 | Phase 2 | Added architecture rules for thin adapters, reset contracts, RuntimeState/Rules ownership, testable decisions, telemetry constants, and deferred stable identity | Done |
| 2026-04-24 | Phase 2/5 | Added plain C# regression-test gate for gameplay decisions; Unity EditMode tests are deferred until the standalone dependency story is cleaner, and debug panel UI remains manual QA while it is actively evolving | Done |
| 2026-04-24 | Phase 1 | Closed core fire simulation after live QA confirmed ignition/spread/extinguish/damage/dead-ash terminal behavior and `Reset Fire Sim` clean-slate recovery; deferred beaver/worker exposure inside burning buildings to Phase 2 | Done |
| 2026-02-22 | Phase 2/3 | Added Emberpelts future extension concept (heat-adapted response, tail-stamp suppression style, charcoal salvage + Fertile Ash synergy, wet-fur breeding constraint awareness) | Planned |

### How to use this section

- Update checklist items as tasks complete.
- Add one row to the active change log for each meaningful **unfinished/planned** implementation step.
- Move completed change-log rows into `DESIGN_CHANGELOG_ARCHIVE.md` (project root).
- Keep statuses concise (`Done`, `In Progress`, `Blocked`).

---

## Risks and Mitigations

1. **Performance risk** from large-scale fire state updates
   - Mitigate with chunked updates, capped spread checks, and event-based evaluations.

2. **Readability risk** (players confused by spread causes)
   - Mitigate with clear status text, hazard overlays, source attribution, and in-world ember/spark cues around active fires.

3. **Balance risk** (either irrelevant or oppressive)
   - Mitigate with preset tuning profiles and iterative playtest metrics.

4. **Cross-mod overwrite conflicts**
   - Mitigate with append/optional blueprints where possible and narrow targeted patches.

---

## Success Criteria

The mod is successful when players can consistently answer:

- What started this fire?
- Why did it spread this way?
- What tools do I have right now to stop it?
- Was this preventable?
- Can I turn this disaster into a strategic advantage?

If the answer to all five is usually "yes," Prometheus is coherent and complete.
