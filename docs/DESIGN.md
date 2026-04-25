# Prometheus Design

## Overview

Prometheus is a fire-focused Timberborn mod built around three durable loops:

1. Ignition and spread through a readable fire simulation.
2. In-world fire state presentation through embers, steam, smoke, flame, and char.
3. Recovery and renewal through Fertile Ash from valid charred sources.

Design target: fire should be dangerous enough to matter, manageable with preparation, and strategically useful when contained.

## Design Principles

- Preserve Timberborn's planning-first identity.
- Make fire risk readable in the world, not only in debug panels.
- Prefer terrain, moisture, firebreaks, and simulation shaping over direct beaver-control minigames.
- Keep gameplay decisions in dependency-light rules/runtime state; keep Unity/Timberborn adapters thin.
- Make every applied runtime effect define how `Reset Fire Sim` clears it.
- Keep fragile integration points centralized: type-name matching, entity identity assumptions, telemetry names, and reflection hooks.

## Current Architecture Direction

The active rewrite replaces direct entity-neighbor spread and responder-first assumptions with a sparse chunked 3D fire grid.

| Layer | Owns |
| --- | --- |
| Runtime state | Current fire/grid facts, snapshots, and resettable state |
| Rules | Dependency-light decisions: propagation, dampening, thresholds, recovery eligibility |
| Appliers | Translation from runtime decisions into Unity/Timberborn effects |
| Debug UI | Commands, inspection, evidence capture, and temporary visual authoring |

Source of truth: exact runtime types and telemetry names live in source, especially `FireTelemetryEvents` and current `Scripts/**/*.cs` files.

## Fire Model

| Concept | Design Commitment |
| --- | --- |
| Ignition | Buildings and events can be data-driven fire sources with profile-tuned risk. |
| Spread | The next model is sparse 3D cellular propagation with local fuel, moisture, barriers, source intensity, and decay. |
| Visuals | Local object fire should read as smoke to fire to smoke/ash/char; sparks/embers belong to field/spread pressure. |
| Suppression | Water/moisture dampen pressure; faction-specific suppression stays behind core spread coherence. |
| Recovery | Fertile Ash is the only core post-fire resource unless future playtesting proves another loop is needed. |

## Durable Decisions

### ADR-001: Ember/Grid First, Responders Later

Status: Accepted.

Build the core spread model before adding fire-brigade, relay, or responder complexity. The earlier responder-first direction made debugging noisy before the fire behavior itself was coherent.

### ADR-002: Debug Visual Authoring Is Temporary And Non-Simulation

Status: Accepted.

The Visuals panel may apply temporary selected-entity previews and export JSON/log settings, but it must not change simulation, damage, recovery, entity profiles, or saved state.

### ADR-003: Source-Owned Facts Stay In Source

Status: Accepted.

Docs should preserve intent, decisions, evidence, and next actions. Current command behavior, test counts, UI control inventories, telemetry event names, and source file lists should point to source, scripts, tests, or logs instead of being copied.

### ADR-004: Internal Docs Stay Out Of Mod Assets

Status: Accepted.

`Assets/Mods/Prometheus/` is shippable mod content because the deploy script symlinks non-`Scripts` entries into the local Timberborn mod folder. Internal docs live under `docs/`.

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
| 2026-04-25 | Phase 2 | Removed direct-spread/responder runtime scaffolding to prepare for sparse 3D grid rewrite | In Progress |
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
