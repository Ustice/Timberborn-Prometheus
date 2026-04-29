# Prometheus Design

## Overview

Prometheus is a fire-focused Timberborn mod built around three durable loops:

1. Ignition and spread through a readable fire exposure.
2. In-world fire state presentation through embers, steam, smoke, flame, and char.
3. Recovery and renewal through Fertile Ash from valid charred sources.

Design target: fire should be dangerous enough to matter, manageable with preparation, and strategically useful when contained.

## Design Principles

- Preserve Timberborn's planning-first identity.
- Make fire risk readable in the world, not only in debug panels.
- Prefer terrain, moisture, spacing, firebreaks, and grid shaping over direct beaver-control minigames.
- Keep gameplay decisions in dependency-light rules/runtime state; keep Unity/Timberborn adapters thin.
- Make every applied runtime effect define how `Reset Fire State` clears it.
- Keep fragile integration points centralized: type-name matching, entity identity assumptions, loaded-scene scans, lifecycle registration, telemetry names, and reflection hooks.

## Current Architecture Direction

The active rewrite replaces direct entity-neighbor spread and responder-first assumptions with a sparse chunked 3D fire grid.

| Layer | Owns |
| --- | --- |
| Runtime state | Current fire/grid facts, snapshots, and resettable state |
| Reset registry | Central reset path for global runtime state plus loaded-entity reset hooks discovered at command time |
| Rules | Dependency-light decisions: propagation, dampening, thresholds, recovery eligibility |
| Appliers | Translation from runtime decisions into Unity/Timberborn effects |
| Debug UI | Commands, inspection, evidence capture, in-game QA coordination, and temporary visual authoring |

Source of truth: exact runtime types and telemetry names live in source, especially `FireTelemetryEvents` and current `Scripts/**/*.cs` files.

## Fire Model

| Concept | Design Commitment |
| --- | --- |
| Ignition | Buildings and events can be data-driven fire sources with profile-tuned risk. |
| Spread | The grid is a sparse 3D heat/ember/smoke field; entities sample their local field and ignite stochastically from heat strength, fuel, oxygen, moisture, and profile threshold. |
| Fuel | Entity fuel is fire health over time: burn hits consume fuel, trees die after 25% fuel loss, and zero fuel becomes burned out. |
| Visuals | Local object fire should read as green to dry brown to smoke/fire to charred remnant; sparks/embers belong to field/spread pressure. |
| Dampening | Water/moisture dampen pressure; any future containment mechanics stay behind core spread coherence. |
| Recovery | Fertile Ash is the only core post-fire resource unless future playtesting proves another loop is needed. |

Future fuel, moisture, and ash should converge on a packet-based material model rather than unrelated smooth meters. Fuel should be measured in log-equivalent material mass where possible: harvested tree logs, crop biomass, and authored building ingredients convert into one comparable structure-fuel pool. Planks, gears, treated planks, and later stored cargo can contribute burnable cargo fuel through explicit conversion rules, but cargo fuel is separate from structure fuel so cargo loss does not become repairable structural damage. Fire damage consumes structure fuel; repair should replace consumed authored materials; ash yield should be scaled and capped from useful residue rather than copied directly from total fuel mass.

Moisture should follow the same design language. Water, soil moisture, rain, suppression, and future Fire Warden actions add moisture resistance as discrete or stochastic packets. Heat evaporates those packets before efficient fuel consumption, creating steam/readability moments and letting wet terrain, irrigated land, and firebreak preparation matter without adding a separate controlled-burn subsystem.

Burned ground should become a Prometheus-owned ground state rendered through resettable overlays, not direct terrain texture mutation. Each burned coordinate can hold ash/scorch levels that darken from faint brown soot to black ash as burn exposure accumulates. Neighboring ash overlays should blend across shared edges and corners so clustered burned cells read as one organic burn scar instead of separate disks. While ash remains in the ground, eligible crops and cultivated trees can grow faster; each plant phase advancement should consume one ash level so the benefit fades through regrowth.

Smoke remains a field output first, but future hazard design may let prolonged smoke exposure affect beavers, plants, and ground condition. The design identity should stay distinct: heat kills quickly, smoke controls space, and toxic smoke poisons recovery. Beavers can accumulate smoke dose while inside smoky cells, with low dose causing discomfort or work slowdown, medium dose triggering avoidance or sickness risk, and high dose becoming dangerous only after clear visual warning. Plants can accumulate smoke exposure separately from heat; prolonged exposure can wither crops or young trees, while mature trees tolerate more before dying.

Toxic smoke is the future badwater escalation path. Smoke passing through badwater-contaminated ground, contaminated fuel, or explicitly toxic fire sources can become more hazardous, accumulate dose faster, sicken beavers sooner, kill or contaminate plants, and apply contamination pressure to nearby ground after prolonged exposure. Normal smoke, steam-heavy smoke, and toxic smoke should be visually distinct enough to read in the world, but smoke hazards are future design direction rather than part of the current Phase 3 harvest-first slice.

Current recovery integration uses Timberborn recovered-good stacks as the safe native collection path for Fertile Ash. Native gatherable natural-resource spawning remains unchosen because no authored ash natural-resource template has been confirmed. Valid burned-out trees and buildings can queue `FertileAsh` recovered-good stacks after aftermath eligibility passes; Timberborn then handles visible stacks, beaver pickup, and normal District Center storage. Prometheus reset clears its own ash queue telemetry and field-amendment state, but it does not destroy Timberborn-owned recovered-good entities. Active field amendments can reduce eligible crop growable time in dependency-light rules. The farmhouse-first application scaffold selects an in-range planting coordinate before consuming one stored `FertileAsh`, but the live `FarmHouse` workplace decorator stays unregistered until TKT-010 has reliable save-load and live worker evidence.

Burned-ground ash recovery now separates tree remnants from loose aftermath. Burned trees should become native remnant harvest targets by rewriting the stump/remnant yielder to `FertileAsh`; they should not spawn visible recovered-good Rubble. Crops and buildings still use the Timberborn recovered-good stack path, with live proof from a Carrot and an unfinished Bakery construction-site burn. The Prometheus-owned local ash deposit marker remains a reset-safe readability layer, while any Timberborn-owned recovered-good entities remain outside Prometheus reset destruction.

Phase 3 keeps "controlled burn" as a player-preparation outcome, not a new runtime object. Players should prepare containment with terrain, moisture, water, spacing, barriers, and firebreaks, then intentionally ignite one selected valid target through the existing Prometheus tool surface. The ignite action reuses the forced-ignition/grid-seeding path and must reject targets without a fire profile. Do not add controlled-burn zones, permits, source types, scheduling, new labor paths, or burn-specific buildings for this slice.

The first Phase 3 economy milestone is harvest and storage only. Burned crops become valid Fertile Ash aftermath sources alongside trees and buildings, with a crop-specific source classification and telemetry context. Farmhouse ash application stays behind TKT-010 until a fresh fixture proves live ash consumption, `fertile_ash_farmhouse_amendment_applied`, and amended-vs-control crop growth.

The first suppression slice is a targeted damping field applied from the existing Prometheus selection tool. Suppression is intentionally separate from baseline fire tuning: it lowers local grid heat, ember pressure, smoke, ignition progress, and burning fuel consumption while active, then expires. Future suppression should turn this debug-facing proof into a player-facing tool or beaver/water workflow without changing accepted unsuppressed crop and tree spread behavior by default.

Fire Wardens are the preferred long-term owner for player-facing fire management. The design target is a single coherent service that can fight fires, apply moisture/suppression, and eventually collect marked ash through normal worker jobs. Until ground ash, overlays, passive growth consumption, and simple collection are proven, Fire Wardens should remain a future destination rather than a dependency for current implementation.

## Durable Decisions

### ADR-001: Ember/Grid First

Status: Accepted.

Build the core spread model before adding containment, mitigation, or colony-response complexity. The earlier responder-first direction made debugging noisy before the fire behavior itself was coherent.

### ADR-002: Debug Visual Authoring Is Temporary And Non-Runtime

Status: Accepted.

The Visuals panel may apply temporary selected-entity previews and export JSON/log settings, but it must not change runtime fire state, damage, recovery, entity profiles, or saved state.

### ADR-003: Source-Owned Facts Stay In Source

Status: Accepted.

Docs should preserve intent, decisions, evidence, and next actions. Current command behavior, test counts, UI control inventories, telemetry event names, and source file lists should point to source, scripts, tests, or logs instead of being copied.

### ADR-004: Internal Docs Stay Out Of Mod Assets

Status: Accepted.

`Assets/Mods/Prometheus/` is shippable mod content because the deploy script symlinks non-`Scripts` entries into the local Timberborn mod folder. Internal docs live under `docs/`.

### ADR-005: QA Coordination Uses Local Exchange Files

Status: Accepted.

In-game QA instructions and tester results use Markdown exchange files under `~/Library/Application Support/Timberborn/PrometheusQA`. This keeps live validation visible inside Timberborn without requiring Steam chat, and it keeps the mod-owned action limited to reading instructions and appending explicit tester results.

### ADR-006: Controlled Burns Are Emergent Containment

Status: Accepted.

Phase 3 treats controlled burns as a strategy created by existing fire-system inputs and player preparation. The mod may expose a narrow selected-target ignition tool, but containment must come from terrain, water, moisture, exposed faces, barriers, spacing, and profile differences rather than a separate controlled-burn mechanic.

### ADR-007: Scene-Touching Work Waits For WorldReady

Status: Accepted.

Prometheus runtime work that ticks globally, scans loaded scene objects, mutates Timberborn model/yielder/component state, or drives QA commands must wait for `PrometheusWorldLoadState.WorldReady`. Lifecycle bindings go through `PrometheusConfigurator.RegisterSingletonLifecycleHooks()`, globally updated Prometheus singletons use the world-ready helper path, and loaded-scene object enumeration goes through `PrometheusLoadedSceneObjectLookup` or `TimberbornComponentCacheLookup`.

## Roadmap

| Phase | Goal | Status |
| --- | --- | --- |
| Phase 0 | Foundation IDs, economy primitives, status hooks | Done |
| Phase 1 | Functional fire behavior and resettable live QA loop | Done |
| Phase 2 | 3D grid spread, fire presentation, and Fertile Ash source tagging | Closeout Verified For Integrated Scope |
| Phase 3 | Intentional fire and ash harvest | Active |
| Phase 4 | Fireworks and explosive ember/grid sources | Planned |
| Phase 5 | Tuning, UX, compatibility, and polish | In Progress |

## Phase 2 Acceptance Criteria

- Sparse 3D fire state can ignite, cool, decay, and reset through dependency-light rules.
- Admin reset remains safe for live Timberborn entities by avoiding singleton-held delegates to transient components.
- Propagation across neighboring cells is visible, attributable, and tuneable.
- Moisture, barriers, and firebreaks reduce or block propagation in ways players can read.
- High-risk configured sources can emit fire pressure; low-risk warm buildings do not by default.
- Smoke, fire, steam, embers, and char map cleanly to runtime state without requiring large replacement model sets.
- Valid charred vegetation/buildings can produce Fertile Ash.
- Plain C# tests cover meaningful decisions before tuning.

## Phase 3 Acceptance Criteria

- `Ignite Selected` is available under the existing Prometheus tool surface and only succeeds for selected fire-profiled targets.
- Ignite success and invalid-target attempts produce UI feedback and stable telemetry.
- Burned crops can queue visible `FertileAsh` recovered-good stacks with crop/source-kind telemetry.
- District Center storage can receive Fertile Ash from burned crops after beaver pickup.
- Prepared containment makes at least one intentional burn stay bounded.
- An unprepared/control burn spreads more aggressively than a prepared burn under comparable conditions.
- Runtime visuals expose enough grid/exposure state through smoke, fire, steam, char, and ember feedback for the burn to be understandable without debug logs.

## Active Changelog

| Date | Phase | Update | Status |
| --- | --- | --- | --- |
| 2026-04-25 | Docs | Moved internal docs out of deployed mod assets and compressed startup documentation around source-of-truth pointers | Done |
| 2026-04-25 | QA UX | Added an in-game `QA` panel for live Codex instructions and `Passed` / `Failed` / `Blocked` result capture through local Markdown exchange files | In Validation |
| 2026-04-25 | Phase 2 | Removed direct-spread/responder runtime scaffolding to prepare for sparse 3D grid rewrite | In Progress |
| 2026-04-26 | Phase 2 | Moved ignition/fuel lifecycle out of neighbor transfer and into entity-owned stochastic field sampling with moisture evaporation, fuel depletion, tree death, and burned-out char | Partial Live Pass |
| 2026-04-27 | Phase 2 | Wired configured `FireProfileSpec` heat, ember, and smoke sources into attributed grid injection with conservative operation-state gating | Dependency-Light Pass |
| 2026-04-27 | Phase 2 Recovery | Chose recovered-good stacks as the narrow native wrapper for visible Fertile Ash collection and added field amendment state for future crop buffs | Dependency-Light Pass |
| 2026-04-27 | Phase 2 Recovery | Applied active Fertile Ash field amendments as a 10% crop growable speed buff while excluding trees and bushes | Dependency-Light Pass |
| 2026-04-27 | Phase 2 Recovery | Queued Fertile Ash from valid charred aftermath through native recovered-good stacks and proved District Center storage after beaver pickup | Live Pass |
| 2026-04-27 | Phase 2 Recovery | Added source-attributed Fertile Ash queue telemetry and reset evidence without deleting Timberborn-owned recovered-good stacks | Live Pass |
| 2026-04-27 | Phase 2 Closeout | Closed the integrated stabilization scope with tests, `--qa` launch, clean Prometheus startup logs, and P2S-025 farmhouse amendment explicitly deferred for fresh live fixture evidence | Done |
| 2026-04-27 | Process | Archived the sprint-specific ticket board and moved active tracking to permanent `docs/tickets/` with a documentation-only verification exception | Done |
| 2026-04-27 | Docs | Reframed `ARCHITECTURE.md` as durable system architecture instead of stabilization-sprint work planning | Done |
| 2026-04-27 | Phase 3 Planning | Opened the intentional fire and ash harvest sprint around selected-target ignition, crop ash, containment validation, and runtime visual readability | Active |
| 2026-04-28 | Phase 3 Recovery | Added a compile-clean farmhouse-first Fertile Ash amendment scaffold with stable telemetry and dependency-light tests; live decorator registration remains disabled until save-load QA and worker evidence are reliable | Blocked Live Proof |
| 2026-04-28 | Runtime Safety | Centralized lifecycle registration and loaded-scene object lookup after save-load hangs showed Prometheus can still fire too early or through a bad fixture path | Live Load Pass |
| 2026-04-28 | Phase 3 Recovery | Proved crop and building aftermath parity live: Carrot and unfinished Bakery construction-site burns both created local burned-ground ash markers and queued recovered Fertile Ash stacks, while tree ash stayed remnant-harvest | Live Pass |
| 2026-04-25 | Phase 2 Visuals | Replaced the old Visual Tuning sliders with an effect authoring inspector, selected-entity temporary preview, native source picker/search, and JSON/log target context export | In Validation |
| 2026-04-24 | Phase 2 UX | Migrated debug navigation to required TimberUi and ModdableToolGroups dependencies with native-style controls and bottom-bar submenu entries | In Validation |
| 2026-04-24 | Phase 2 Content | Pruned old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding; renamed ash fertilizer content to Fertile Ash | Done |

Historical entries live in [ARCHIVE/design-changelog.md](ARCHIVE/design-changelog.md).

## Success Criteria

Prometheus is coherent when players can usually answer:

- What started this fire?
- Why did it spread this way?
- What tools do I have right now to stop it?
- Was this preventable?
- Can I turn this disaster into a strategic advantage?
