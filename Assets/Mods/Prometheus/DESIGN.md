# Prometheus Mod Design Document

## Overview

**Prometheus** is a fire-focused Timberborn mod that adds a full gameplay loop around:

1. **Ignition and spread** (fire as a systemic hazard)
2. **Suppression and logistics** (faction-specific firefighting gameplay)
3. **Recovery and renewal** (positive post-fire ecology and productivity)

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
   - Prepared players can contain and extinguish fires through tools, logistics, and planning.

3. **Faction identity**
   - Folktails and Ironteeth solve the same problem through different strategies.

4. **Risk-reward ecology**
   - Fire can create temporary fertility and production opportunities.

5. **Readable simulation**
   - Players should always understand why fires started/spread, what is currently at risk, and how to respond.

---

## Design Goals

### Gameplay goals

- Add a systemic fire mechanic that integrates with water, dehydration, agriculture, forestry, and building safety.
- Make firefighting a strategic layer rather than a one-off gimmick.
- Enable intentional controlled burns for productivity gains.
- Introduce fireworks as fun content with non-zero risk.

### Player experience goals

- Preserve Timberborn's planning-first identity.
- Avoid constant chaos; keep fire events periodic and predictable enough to plan around.
- Ensure losses feel avoidable with good preparation.

### Technical goals

- Build in phases, keeping the mod playable after each phase.
- Reuse data-driven blueprints where possible.
- Add C# simulation hooks incrementally for advanced behavior (spread, quenching, exposure).

---

## System Concept: Fire Lifecycle

### 1) Ignition

Potential fire sources:

- Lightning/weather events (especially during drought-like conditions)
- Industrial accidents (high-heat buildings)
- Fireworks mishaps (small chance, tunable)
- Player-triggered controlled burns (late phase)

### 2) Spread

Fire spread should be influenced by:

- **Fuel type**: crops and trees ignite/spread faster than stone-heavy structures
- **Dryness/moisture**: drier environments spread faster
- **Barriers**: water bodies and prepared firebreaks slow or stop spread
- **Wind/event modifiers** (optional advanced tuning)

### 3) Damage and Status

- **Crops/Trees**: healthy -> scorched -> burning -> dead
- **Buildings**: intact -> smoldering (efficiency penalty) -> burning -> ruined
- **Beavers**: heat exposure -> dehydration acceleration -> injury risk/death at extremes

### 4) Suppression

- Water and suppression materials reduce fire intensity.
- Assigned firefighting jobs prioritize nearest/most dangerous fronts.
- Prepared infrastructure improves response speed and survival odds.

### 5) Recovery and Renewal

- Burnt ground can transition into **Ashen Soil** for a limited duration.
- Ashen Soil boosts growth speed and output.
- Best rewards come from managed burns, not uncontrolled disasters.

---

## Faction Asymmetry

## Folktails: Bucket Brigade Doctrine

- Focus: rapid local response and village defense.
- Mechanics:
  - chain logistics from water source to fire line,
  - relay-style suppression throughput,
  - low-tech, scalable emergency response.
- Candidate content:
  - Bucket Relay Post,
  - Emergency Cistern,
  - Fire Watch Tower.

## Ironteeth: Industrial Fire Control

- Focus: engineered suppression and hazard operations.
- Mechanics:
  - crafted protective gear,
  - specialized suppression materials,
  - better operation in high-heat zones.
- Candidate content:
  - Fire Gear Workshop,
  - Pressurized Suppressant Station,
  - Protective equipment production chain.

---

## Fireworks Design

Fireworks are a positive social feature with strategic caution:

- Base effect: wellbeing/festival bonus.
- Optional high-intensity show mode:
  - stronger wellbeing bonus,
  - increased ignition chance if local fire safety is weak.

Design intent: fireworks are mostly celebratory, but they naturally connect to the fire system instead of being isolated flavor.

---

## Positive Fire Mechanics

### Ashen Soil

- Temporary terrain/zone modifier after burns.
- Effects:
  - increased crop growth speed,
  - increased yield/output.
- Balance constraints:
  - duration-limited,
  - strongest after managed burns,
  - reduced value after catastrophic spread.

### Controlled Burns

- Mid/late-game strategic tool.
- Lets players intentionally trade short-term risk for long-term farm productivity.

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
- initial fire-economy goods (`AshFertilizer`, `FirefightingFoam`, `FireworksCrate`),
- initial recipes for those goods,
- initial `HeatStress` need and faction need-collection wiring,
- localization keys for new goods/status,
- integration into existing Prometheus production chain.

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

## Phase 1 — Core Fire Simulation (In Progress)

**Goal:** implement functional fire behavior.

Scope:

- ignition triggers,
- spread rules,
- quenching by water,
- damage ticks for crops/trees/buildings,
- beaver heat exposure interaction.

Exit criteria:

- Fires can start, spread, be extinguished, and cause/avoid losses predictably.

## Phase 2 — Firefighting Gameplay and Faction Identity

**Goal:** introduce differentiated suppression loops.

Scope:

- Folktails bucket brigade systems,
- Ironteeth fire-gear and suppression chain,
- task prioritization and response logistics tuning.

Exit criteria:

- Both factions can fight fires effectively in distinct ways.

### Phase 2 detailed delivery plan (next sprint)

This section turns Phase 2 into an implementation-ready sprint plan focused on **logistics depth, faction differentiation, and response quality**.

#### Objectives

1. Make Folktails and Ironteeth suppression loops feel mechanically distinct in minute-to-minute gameplay.
2. Improve fire-response reliability via explicit prioritization and dispatch behavior.
3. Add player-readable signals so responders feel understandable rather than random.
4. Keep simulation stable under load with bounded update work and telemetry.

#### Scope slices

##### Slice A — Folktails bucket brigade loop

- Introduce/expand low-tech suppression throughput mechanics around:
  - `BucketBrigadeKit` consumption,
  - relay distance/efficiency falloff,
  - emergency water buffering behavior.
- Add one dedicated Folktails response building workflow (first pass):
  - **Bucket Relay Post** runtime hooks and profile bonuses.
- Define balance intent:
  - high responsiveness close to water/village core,
  - weaker performance on long frontlines,
  - lower material complexity than Ironteeth.

##### Slice B — Ironteeth engineered suppression loop

- Expand industrial suppression chain around:
  - `FireControlGear` usage and upkeep,
  - `FirefightingFoam` throughput and application strength,
  - higher resilience in high-heat zones.
- Add one dedicated Ironteeth response building workflow (first pass):
  - **Pressurized Suppressant Station** runtime hooks and profile bonuses.
- Define balance intent:
  - slower setup and higher production dependency,
  - superior sustained suppression at severe fire intensity,
  - better performance in industrial districts.

##### Slice C — Task prioritization and dispatch

- Implement a shared firefront scoring model to rank targets by:
  - danger severity (burn state + spread pressure),
  - asset value proxy (building/crop/tree/beaver risk),
  - travel/response cost,
  - containment leverage (chance to stop propagation).
- Add faction bias terms on top of shared scoring:
  - Folktails prefer near/front-edge containment and village defense,
  - Ironteeth prefer high-intensity nodes and infrastructure protection.
- Add anti-thrashing behavior:
  - minimum assignment lock duration,
  - hysteresis threshold before retargeting.

##### Slice D — UX/debug readability for Phase 2 behavior

- Extend debug panel output with dispatch-level fields:
  - selected target score,
  - top scoring factors,
  - assignment lock/hysteresis state,
  - active faction profile multipliers.
- Add concise in-game notifications only for meaningful transitions:
  - response overwhelmed,
  - containment established,
  - firefront stabilized.

#### Proposed technical work breakdown

1. **Runtime state extensions**
    - Add dispatch/prioritization snapshots to existing fire runtime state containers.
2. **Profile schema evolution**
    - Extend fire-response profile specs with faction-tunable dispatch weights and lock/hysteresis parameters.
3. **Controller pass integration**
    - Introduce a deterministic dispatch pass between simulation update and suppression application.
4. **Effect application pass**
    - Apply selected assignments to suppression entities with faction-specific throughput/effectiveness multipliers.
5. **Instrumentation pass**
    - Emit telemetry used by the debug fragment and tuning verification.

#### Acceptance criteria (Phase 2 sprint gate)

- In equivalent fire scenarios, Folktails and Ironteeth produce measurably different suppression patterns.
- Dispatch chooses stable, high-value targets and avoids excessive retargeting.
- Player can inspect any responding entity and understand:
  - why this target was selected,
  - which multipliers are active,
  - whether the front is improving or deteriorating.
- No severe simulation regressions under large-fire scenarios (bounded per-tick cost, no runaway assignment churn).

#### Balance and tuning matrix (initial)

- **Folktails baseline**
  - suppression strength: medium,
  - response latency: low,
  - distance penalty: high,
  - material dependency: low-medium.
- **Ironteeth baseline**
  - suppression strength: high,
  - response latency: medium,
  - distance penalty: medium,
  - material dependency: medium-high.

All values remain data-tunable through runtime profile multipliers and global fire activity settings.

#### Test scenarios for this phase

1. **Village edge brush fire**
    - Expected: Folktails contain quickly near water relay; Ironteeth stabilize after setup.
2. **Industrial district ignition chain**
    - Expected: Ironteeth outperform under sustained high heat.
3. **Dual-front concurrent fires**
    - Expected: dispatch prioritizes containment leverage and avoids assignment thrash.
4. **Long-distance rural wildfire**
    - Expected: Folktails degrade with distance; Ironteeth sustain if supply chain is intact.

#### Out of scope for this sprint

- Full new building art/content pipelines beyond first-pass runtime hooks.
- Major festival/fireworks redesign (Phase 4 ownership).
- Large renewal-economy rebalance beyond Phase 2 compatibility checks.

## Phase 3 — Renewal and Controlled Burn Economy

**Goal:** add strong positive incentives to fire mastery.

Scope:

- Ashen Soil lifecycle,
- fertility/yield boosts,
- controlled burn mechanics and safeguards.

Exit criteria:

- Planned burns can be net-positive without overshadowing normal farming.

## Phase 4 — Fireworks and Festival Systems

**Goal:** add celebratory content connected to fire risk.

Scope:

- fireworks content and wellbeing effects,
- risk scaling by local safety/readiness,
- optional advanced festival modes.

Exit criteria:

- Fireworks are fun, useful, and meaningfully integrated with the fire ecosystem.

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

- Deploy the mod payload and runtime DLL via `scripts/deploy_prometheus.sh`.

- Launch Timberborn and confirm mod load in `Player.log` (`- Prometheus (v0.2)` or later).

- In-game, select a fire-profiled entity and use **Prometheus Fire Debug** (output is selectable, and **Copy** copies the full debug snapshot).

If blueprint load fails with `No type found for key FireResponseProfileSpec`, verify the installed mod contains:

- `~/Documents/Timberborn/Mods/Prometheus/Scripts/Timberborn.ModExamples.Prometheus.dll`

### Test plan reference

Primary QA runbook is in:

- `Assets/Mods/Prometheus/TEST_PLAN.md`

Use that file as the source of truth for smoke checks, dual-front validation matrix, and tuning sign-off criteria.

### Milestone checklist

- [x] Phase 0 — Foundation
- [ ] Phase 1 — Core Fire Simulation (In Progress)
- [ ] Phase 2 — Firefighting Gameplay and Faction Identity (In Progress)
- [ ] Phase 3 — Renewal and Controlled Burn Economy
- [ ] Phase 4 — Fireworks and Festival Systems
- [ ] Phase 5 — Tuning, UX, and Compatibility (In Progress)

### Current sprint focus (Completed)

- [x] Add faction starter goods and recipes (`BucketBrigadeKit`, `FireControlGear`)
- [x] Wire first faction-specific production entries (Bakery and Coffee Brewery)
- [x] Add phase-2 runtime behavior hooks (`FireResponseProfileSpec` + component decorator)
- [x] Implement first suppression consumer (`FireSuppressionProfileApplier`) using faction profile multipliers
- [x] Implement first runtime ignition/spread/quench simulation loop (`FireSimulationController`)
- [x] Implement first neighbor-aware spread propagation (`FireEntityRegistryRuntimeState` + neighbor spread pressure)
- [x] Implement ignition sources and spread rules
- [x] Implement water-aware quenching and spread reduction (`FireWaterContextProbe` + `FireWaterContextRuntimeState`)
- [x] Implement first crop/tree/building damage pressure and beaver dehydration/injury pressure layer (`FireImpactController`)
- [x] Implement crop/tree/building damage states and ticks
- [x] Implement first beaver-side need/health bridge (`FireBeaverEffectApplier` via `NeedManager.AddPoints` reflection)
- [x] Apply first gameplay effect bridge (workplace speed throttling from fire pressure)
- [x] Add first in-game debug/status signal (ignition/extinguish quick notifications)
- [x] Add first in-game debug panel (`PrometheusFireDebugFragment` in entity panel)
- [x] Implement fire damage state machine and first state-driven effects (`FireDamageStateController` + `FireDamageEffectApplier`)
- [x] Implement controlled burn and ashen fertility recovery loop (`FireRecoveryController` + `FireRecoveryEffectApplier`)
- [x] Implement festival/fireworks ignition-risk subsystem (`FireFestivalRiskController` + `FireFestivalRuntimeState`)
- [x] Implement global fire activity tuning profiles (`FireTuningRuntimeState`) and apply multipliers to simulation/impact/festival risk

### Carryover validation checklist (Phase 2 completion + Phase 5 polish)

- [ ] Run balancing pass across low/standard/high fire activity profiles for new Phase 2 behavior.

- [ ] Execute one full dual-front verification scenario pass and tune defaults *(protocol and template are ready below)*.

- [ ] Mark Phase 2 validation items complete in this document and append measured tuning outcomes to the change log.

### Archived implementation checklist (Phase 2 execution)

- [x] Implement `FireDispatchScoringRuntimeState` for per-entity firefront target scoring snapshots.

- [x] Add first scoring pass in `FireSimulationController` using severity/risk/travel/leverage factors.

- [x] Expose winning score factors in `PrometheusFireDebugFragment`.

- [x] Add `FireResponseProfileSpec` dispatch weights with safe defaults.

- [x] Add assignment lock duration and hysteresis threshold fields to runtime profile application.

- [x] Add assignment lock + hysteresis anti-thrashing behavior.

- [x] Implement first Folktails relay-distance efficiency model.

- [x] Implement first Ironteeth sustained high-heat suppression bonus model.

- [x] Add response-state notifications (overwhelmed, contained, stabilized).

### Dual-front verification protocol (ready to run)

Purpose: validate anti-thrashing dispatch, faction asymmetry, and response-state signaling under concurrent pressure.

#### Setup

- Use one settlement save with two independent firefronts: **Front A** near water/core logistics and **Front B** farther from water/infrastructure.

- Run once with a Folktails-focused response cluster and once with an Ironteeth-focused response cluster.

- Run each faction under all tuning profiles: `Low`, `Standard`, `High`.

- Observe at least one responding entity per front in `PrometheusFireDebugFragment`.

#### What to capture

- Dispatch scoring fields: `Candidate score`, `Assigned score`, `Assignment locked`, `Lock remaining (s)`, `Hysteresis threshold`, `Retarget suppressed`, `Top factor`, `Response state`.

- Simulation context fields: `Intensity`, `Spread pressure`, `Quenching`, `Neighbor spread pressure`.

- Notification transitions: overwhelmed, containment established, firefront stabilized.

#### Pass/fail thresholds (first balancing gate)

- **Retarget stability (anti-thrashing):** pass when `Assignment locked` holds for most of each lock window, `Assigned score` changes smoothly (no rapid oscillation spikes), and `Retarget suppressed` appears when candidate-score gains are small.

- **Folktails distance behavior:** pass when Folktails shows visibly weaker quenching on Front B than Front A in equivalent intensity bands.

- **Ironteeth high-heat behavior:** pass when Ironteeth quenching increases as intensity rises (especially in high-intensity windows).

- **Response-state readability:** pass when transitions align with telemetry — `Overwhelmed` when spread pressure persistently exceeds quenching, `Contained` when quenching clearly overtakes spread pressure, `Stabilized` as fronts settle/extinguish.

- **Profile robustness:** pass when behavior remains coherent in `Low` and `High` without runaway notification spam or assignment churn.

#### Tuning adjustment order (if fail)

- Increase/decrease `DispatchRetargetHysteresisThreshold`.

- Increase/decrease `DispatchAssignmentLockDurationInSeconds`.

- Rebalance dispatch weights (`Severity` / `AssetRisk` / `TravelCost` / `ContainmentLeverage`).

- Rebalance faction asymmetry modifiers: Folktails relay-distance penalty strength; Ironteeth high-heat quenching bonus scaling.

#### Completion rule

After one full dual-front pass per faction across `Low`/`Standard`/`High`, update:

- `Run balancing pass across low/standard/high fire activity profiles...` to checked,

- `Add one verification scenario pass (dual-front concurrent fires) and tune defaults` to checked,

- change log with measured outcomes and any tuned default values.

### Quick playtest results template (copy/fill)

| Date | Faction | Profile | Front A result | Front B result | Anti-thrash | Response states | Outcome | Tuning changes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| YYYY-MM-DD | Folktails | Low/Standard/High | e.g. contained in 2.5h | e.g. overwhelmed then contained | Pass/Fail | O/C/S observed | Pass/Fail | e.g. +0.01 hysteresis |
| YYYY-MM-DD | Ironteeth | Low/Standard/High | | | | | | |

Legend: `O/C/S` = `Overwhelmed` / `Contained` / `Stabilized`.

### Next sprint plan (Phase 2 completion + Phase 5 balance pass)

Sprint goal: move from implementation-complete behavior to validated, tuned, and documented gameplay defaults.

#### Sprint backlog

- [ ] Execute dual-front verification protocol for Folktails across `Low`/`Standard`/`High`.

- [ ] Execute dual-front verification protocol for Ironteeth across `Low`/`Standard`/`High`.

- [ ] Tune dispatch anti-thrash values (`DispatchRetargetHysteresisThreshold`, `DispatchAssignmentLockDurationInSeconds`) from measured runs.

- [ ] Tune dispatch score weights (`Severity` / `AssetRisk` / `TravelCost` / `ContainmentLeverage`) for stable prioritization under concurrent fronts.

- [ ] Tune faction asymmetry coefficients (Folktails relay-distance penalty, Ironteeth high-heat quenching bonus) until distinction is clear but fair.

- [ ] Verify notification readability and noise level under sustained fire events; adjust cooldown/transition rules if needed.

- [ ] Record final default values in the change log with before/after rationale.

- [ ] Mark remaining current sprint checklist items as done once validation gates pass.

#### Implementation order

- Run baseline captures first (no tuning changes).

- Adjust anti-thrash parameters second (stability first, then speed).

- Adjust faction asymmetry third (identity without overpowering).

- Apply final profile-wide balancing pass last.

#### Exit criteria for this next sprint

- Both factions pass dual-front scenarios on `Low`/`Standard`/`High` with no obvious assignment thrash.

- Response-state transitions are understandable and not spammy in long events.

- Tuned defaults are documented in `DESIGN.md` change log and reflected in checklist completion.

### Phase 3 kickoff checklist (prepared)

Focus: begin **Renewal and Controlled Burn Economy** work immediately after Phase 2 validation lock.

- [ ] Define `Ashen Soil` runtime-state schema for duration, fertility bonus, growth bonus, and yield bonus (with debug visibility fields).

- [ ] Add balancing envelope for controlled-burn reward versus catastrophic-fire reward (managed burns should be clearly superior).

- [ ] Specify controlled-burn enablement gates (technology/progression/safety prerequisites).

- [ ] Draft first implementation tasks for `FireRecoveryController`/`FireRecoveryEffectApplier` tuning pass against finalized Phase 2 fire intensity ranges.

- [ ] Add Phase 3 acceptance test scenarios (planned burn near farms, failed burn containment, post-burn recovery payoff window).

- [ ] Add first Phase 3 UX tasks (player-facing status text/tooltips for ashen fertility lifecycle).

### Sprint close-out (2026-02-21)

- Status: **Implementation complete; validation carryover active**
- Scope result: all implementation checklist items were delivered.
- Carryover blockers: requires in-game validation/tuning pass (dual-front + profile balancing).
- Next sprint handoff: finish Phase 2/5 validation checklist, then start Phase 3 kickoff checklist.

### Change log

| Date | Phase | Update | Status |
| --- | --- | --- | --- |
| 2026-02-21 | Phase 5 | Documented Prometheus dev loop (compile/deploy/log-check/debug-copy) and linked standalone QA runbook in `TEST_PLAN.md` | Done |
| 2026-02-21 | Phase 2/3/5 | Cleaned sprint tracking into carryover vs archived implementation checklists and added prepared Phase 3 kickoff checklist for immediate handoff | Done |
| 2026-02-21 | Phase 2/5 | Added quick-fill dual-front playtest result template and next sprint execution plan (validation, tuning sequence, and exit gates) | Planned |
| 2026-02-21 | Phase 5 | Added ready-to-run dual-front verification protocol with telemetry checklist, pass/fail thresholds, and tuning adjustment order for sprint close-out validation | Done |
| 2026-02-21 | Phase 2 | Implemented assignment lock+hysteresis scoring flow with stable assigned-score tracking and retarget suppression telemetry (`FireSimulationController` + `FireDispatchScoringRuntimeState` + debug panel fields) | Done |
| 2026-02-21 | Phase 2 | Added first faction asymmetry suppression hooks: Folktails relay-distance efficiency penalty (water-exposure proxy) and Ironteeth high-heat suppression bonus in quenching math | Done |
| 2026-02-21 | Phase 2/5 | Added response-state transition notifications (`overwhelmed`/`contained`/`stabilized`) with cooldown throttling to reduce spam | Done |
| 2026-02-21 | Phase 2 | Extended `FireResponseProfileSpec`/`FireResponseProfile` with dispatch weight + lock/hysteresis tuning defaults and applied them to runtime scoring/suppression snapshots | Done |
| 2026-02-21 | Phase 2 | Started active sprint implementation by adding `FireDispatchScoringRuntimeState`, wiring first dispatch score factors in `FireSimulationController`, and exposing dispatch telemetry in `PrometheusFireDebugFragment` | Done |
| 2026-02-21 | Phase 2/5 | Added detailed next-sprint Phase 2 delivery plan (faction logistics depth, dispatch scoring, hysteresis, telemetry, acceptance gates) and explicit sprint checklist | Planned |
| 2026-02-21 | Phase 5 | Extended `FireTuningRuntimeState` with per-difficulty source-level ignition multipliers (weather/industrial/fireworks/controlled-burn/neighbor) and spread-rule weighting (dryness/fuel/barrier), then applied them in `FireSimulationController` and exposed them in debug telemetry | Done |
| 2026-02-21 | Phase 1 | Added explicit ignition source modeling (`Weather`, `Industrial`, `Fireworks`, `ControlledBurn`, `NeighborSpread`) and spread-rule weighting (dryness/fuel/barrier) with debug telemetry in `FireSimulationController`/`FireSimulationRuntimeState`/`PrometheusFireDebugFragment` | Done |
| 2026-02-21 | Phase 1/5 | Added `FireTuningRuntimeState` global activity profiles (`Low/Standard/High`) and integrated multipliers into ignition/spread/quenching, impact pressure, and festival ignition risk | Done |
| 2026-02-21 | Phase 1 | Completed explicit crop/tree/building damage ticking with per-category tick accumulation, severity progression, and debug telemetry (`TickProgress`, `DamageTicksApplied`) | Done |
| 2026-02-21 | Phase 1 | Expanded state-driven damage effects to better handle natural resources via `LivingNaturalResource` lifecycle reflection (`IsDying`/`IsDead`) | Done |
| 2026-02-21 | Phase 4 | Added `FireFestivalRiskController` + `FireFestivalRuntimeState` with periodic festival windows and safety-based ignition risk bonuses | Done |
| 2026-02-21 | Phase 4 | Integrated festival risk into ignition math and exposed live festival telemetry in the debug panel | Done |
| 2026-02-21 | Phase 3 | Added `FireRecoveryController` for controlled burn detection and temporary ashen fertility bonuses (fertility, growth, yield, duration) | Done |
| 2026-02-21 | Phase 3 | Added `FireRecoveryEffectApplier` and debug panel recovery metrics to apply/observe growth speed boosts on growables | Done |
| 2026-02-21 | Phase 1/2 | Added `FireEntityRegistryRuntimeState` and neighbor-aware spread pressure propagation to `FireSimulationController` | Done |
| 2026-02-21 | Phase 1/2 | Extended debug panel with live neighbor spread pressure metrics | Done |
| 2026-02-21 | Phase 1/2 | Added `FireWaterContextProbe` + `FireWaterContextRuntimeState` and integrated local flood/moisture signals into ignition/spread/quenching math | Done |
| 2026-02-21 | Phase 1/2 | Extended `PrometheusFireDebugFragment` with live water-context metrics (flooding, exposure, quenching bonus, spread reduction) | Done |
| 2026-02-21 | Phase 1/2 | Added `FireDamageStateController` + `FireDamageEffectApplier` with state-driven transitions (`Healthy/Scorched/Burning/Dead`) and reflective integration to `Deteriorable`/`Growable` methods | Done |
| 2026-02-21 | Phase 1/2 | Extended `PrometheusFireDebugFragment` with live damage category/state/severity output | Done |
| 2026-02-21 | Phase 1/2 | Added `PrometheusFireDebugFragment` entity panel showing live suppression/simulation/impact snapshots for fire-profiled entities | Done |
| 2026-02-21 | Phase 1/2 | Added `FireBeaverEffectApplier` to apply nearby `Thirst` and `Injury` penalties from fire impact pressure through `NeedManager.AddPoints` | Done |
| 2026-02-21 | Phase 1/2 | Added `FireWorkplaceEffectApplier` to convert fire impact into real workplace production speed penalties | Done |
| 2026-02-21 | Phase 1/2 | Added ignition/extinguish quick notifications in `FireSimulationController` for live fire-state debugging | Done |
| 2026-02-21 | Phase 1/2 | Added `FireImpactController` + `FireImpactRuntimeState` to derive crop/tree/building damage pressure and beaver dehydration/injury pressure from fire simulation | Done |
| 2026-02-21 | Phase 1/2 | Added `FireSimulationController` and `FireSimulationRuntimeState` to run baseline ignition/spread/quenching simulation from faction suppression profiles | Done |
| 2026-02-21 | Phase 2 | Implemented `FireSuppressionProfileApplier` + runtime state snapshots for effective suppression/heat metrics | Done |
| 2026-02-21 | Phase 2 | Added `FireResponseProfile` runtime hook system and attached faction profiles to starter buildings | Done |
| 2026-02-21 | Phase 2 | Added faction starter firefighting economy assets and first production wiring | Done |
| 2026-02-21 | Phase 0 | Created design document and implementation roadmap | Done |
| 2026-02-21 | Phase 0 | Added initial Prometheus fire foundation assets (goods, recipes, `HeatStress`, wiring) | Done |

### How to use this section

- Update checklist items as tasks complete.
- Add one row to the change log for each meaningful implementation step.
- Keep statuses concise (`Done`, `In Progress`, `Blocked`).

---

## Risks and Mitigations

1. **Performance risk** from large-scale fire state updates
   - Mitigate with chunked updates, capped spread checks, and event-based evaluations.

2. **Readability risk** (players confused by spread causes)
   - Mitigate with clear status text, hazard overlays, and source attribution.

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
