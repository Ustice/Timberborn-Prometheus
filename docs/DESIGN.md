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
- Prefer terrain, moisture, firebreaks, and grid shaping over direct beaver-control minigames.
- Keep gameplay decisions in dependency-light rules/runtime state; keep Unity/Timberborn adapters thin.
- Make every applied runtime effect define how `Reset Fire State` clears it.
- Keep fragile integration points centralized: type-name matching, entity identity assumptions, telemetry names, and reflection hooks.

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

Current recovery integration uses Timberborn recovered-good stacks as the safe native collection path for Fertile Ash. Native gatherable natural-resource spawning remains unchosen because no authored ash natural-resource template has been confirmed.

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

## Roadmap

| Phase | Goal | Status |
| --- | --- | --- |
| Phase 0 | Foundation IDs, economy primitives, status hooks | Done |
| Phase 1 | Functional fire behavior and resettable live QA loop | Done |
| Phase 2 | 3D grid spread, fire presentation, and Fertile Ash source tagging | In Progress |
| Phase 3 | Renewal and controlled burn economy | Planned |
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

## Active Changelog

| Date | Phase | Update | Status |
| --- | --- | --- | --- |
| 2026-04-25 | Docs | Moved internal docs out of deployed mod assets and compressed startup documentation around source-of-truth pointers | Done |
| 2026-04-25 | QA UX | Added an in-game `QA` panel for live Codex instructions and `Passed` / `Failed` / `Blocked` result capture through local Markdown exchange files | In Validation |
| 2026-04-25 | Phase 2 | Removed direct-spread/responder runtime scaffolding to prepare for sparse 3D grid rewrite | In Progress |
| 2026-04-26 | Phase 2 | Moved ignition/fuel lifecycle out of neighbor transfer and into entity-owned stochastic field sampling with moisture evaporation, fuel depletion, tree death, and burned-out char | Partial Live Pass |
| 2026-04-27 | Phase 2 | Wired configured `FireProfileSpec` heat, ember, and smoke sources into attributed grid injection with conservative operation-state gating | Dependency-Light Pass |
| 2026-04-27 | Phase 2 Recovery | Chose recovered-good stacks as the narrow native wrapper for visible Fertile Ash collection and added field amendment state for future crop buffs | Dependency-Light Pass |
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
