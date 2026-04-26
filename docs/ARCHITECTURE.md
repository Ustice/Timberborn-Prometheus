# Prometheus Architecture Map

This document is the durable boundary map for the stabilization sprint. It describes ownership and contracts; the sprint narrative, sequence, and risk burn-down remain in [STABILIZATION_SPRINT.md](STABILIZATION_SPRINT.md).

Source of truth: exact class names, telemetry names, file layout, and current implementation details live in `Assets/Mods/Prometheus/Scripts`, `tests/Prometheus.Tests`, and the active logs.

## Ownership Layers

| Layer | Owns | Does Not Own |
| --- | --- | --- |
| Fire profiles | Static entity fire configuration: burnability, fuel, moisture resistance, barrier resistance, ignition threshold, configured source fields, and operation requirements | Runtime fire state, world sampling, economy rewards, or visual side effects |
| Grid runtime | Sparse 3D field state: coordinates, chunks, cell environment, heat, ember pressure, smoke, ignition progress, propagation, decay, and aggregate sampling | Timberborn component lookup, entity fuel lifecycle, UI commands, or Fertile Ash economy behavior |
| Source injection | Translation of debug ignition, configured heat sources, burst/explosion sources, and future controlled-burn sources into grid field pressure | Directly killing entities, applying visuals, or bypassing attribution |
| Entity exposure lifecycle | Per-entity fuel, moisture, ignition, burned-out state, profile sampling, grid footprint sampling, and published exposure snapshots | Long-lived grid storage, global stepping policy, Timberborn reflection probes, or renewal outputs |
| Effects and appliers | Applying snapshots into Timberborn-facing consequences: damage/death, workplace disablement, beaver exposure, recovery effects, runtime visuals, and preview cleanup | Deciding propagation rules or retaining hidden state outside their reset contract |
| Visual projection | Mapping explicit grid and entity state into object fire, smoke, steam, desiccation, embers, and char intensity | Gameplay facts, source attribution, or Phase 3 economy eligibility |
| Timberborn integration | Fragile adapters: component-cache traversal, scene indexing, selection/focus, type-name classification, and reflection probes | Fire rules, tuning policy, or debug-only scene scans leaking into normal runtime |
| Debug and QA UI | Commands, selection bridge, evidence capture, log viewing, QA exchange files, visual authoring, and admin reset orchestration | Runtime-only ownership of fire behavior or durable data model changes |
| Tests and scripts | Dependency-light rule coverage, build/test/deploy entrypoints, and QA launch readiness | In-game proof unless paired with `Player.log`, `Fire.log`, screenshots, or QA panel results |

## Current Module Map

| Area | Current Home | Stabilization Direction |
| --- | --- | --- |
| Profiles | `Scripts/Fire/Profiles` | Keep profile data descriptive and Timberborn-facing; make runtime consumers explicit about which fields are active. |
| Grid | `Scripts/Fire/Grid/FireGridRuntimeState.cs` and `FireGridEnvironmentSampler.cs` | Split into value types, chunk storage, footprint sampling, environment policy, propagation policy, and runtime coordinator. |
| Exposure | `Scripts/Fire/Exposure` | Keep entity-owned fuel/moisture/ignition lifecycle here, but move world adapters, source injection, and global stepping out. |
| Damage | `Scripts/Fire/Damage` | Apply damage/death from exposure snapshots and centralize Timberborn natural-resource reflection behind integration probes. |
| Workplace effects | `Scripts/Fire/Workplace` | Apply and restore operation effects through a resettable applier contract. |
| Beaver effects | `Scripts/Fire/Beavers` | Keep proximity and need effects behind one integration adapter for beaver need manager discovery. |
| Recovery | `Scripts/Fire/Recovery` | Treat current Fertile Ash fields as provisional until valid aftermath eligibility and collection/storage behavior are defined. |
| Visuals | `Scripts/Fire/Visuals` | Use one runtime projection and one native particle catalog so authoring previews and runtime effects cannot drift. |
| Debug UI | `Scripts/Debug/PrometheusFireDebugFragment.cs` | Split into shell, selection, actions/reset, logs, QA, and visual authoring components. |
| Core support | `Scripts/Core` | Keep generic runtime stores, logging, and tick gates dependency-light and shared. |

## Data Flow

1. Profile data enters through `FireProfile` and `FireProfileSpec`.

   - Profiles describe an entity's fuel, moisture resistance, barrier resistance, structure kind, ignition threshold, configured source fields, and operation requirements.
   - Profile data must not mutate runtime state directly.

2. Timberborn/world inputs are sampled through adapters.

   - Entity footprints come from renderer bounds or world position.
   - Environment facts should flow through a Timberborn integration boundary before becoming `FireCellEnvironment`.
   - Scene-wide scans and reflection belong to debug/admin or cached integration services, not normal fire rules.

3. Sources inject field pressure into the grid.

   - Debug ignition, configured sources, burst/explosion sources, and future controlled burns should use the same source-injection contract.
   - Each injection must preserve enough attribution for logs, QA, and player-readable "what started this" answers.

4. The grid advances dependency-light field state.

   - Grid state stores heat, ember pressure, smoke, ignition progress, fuel consumed at cell level, and burn state.
   - Propagation policy decides bounded transfer, upward heat/smoke bias, outward ember bias, cooling, decay, water, barriers, exposed faces, and oxygen effects.
   - Global stepping ownership should be explicit and centralized; per-entity update calls may request work but should not define the simulation clock.

5. Entities sample the grid and publish snapshots.

   - Exposure lifecycle owns stochastic ignition, remaining fuel, remaining moisture, burned-out state, and per-entity exposure snapshots.
   - Entity lifecycle consumes grid pressure; it does not own global propagation rules.

6. Appliers consume snapshots and mutate Timberborn-facing state.

   - Damage, workplace, beaver, recovery, and visual appliers read snapshots or projections and apply effects.
   - Appliers must define what they mutate, how reset clears it, and what telemetry proves reset happened.

7. Debug, QA, tests, and docs observe the system.

   - Debug UI may issue admin commands and capture evidence, but it should not become the source of gameplay truth.
   - Tests own dependency-light behavioral guarantees.
   - Live QA evidence owns claims about in-game behavior.

## Reset Boundaries

`Reset Fire State` is the broad admin reset. Its contract is a clean Prometheus fire simulation state for all loaded fire entities without changing authored design data.

Reset must clear:

- Grid cells, environment-derived active state, and pending global grid activity.
- Exposure snapshots, forced ignition requests, per-entity ignition flags, fuel loss, moisture loss, and burned-out flags.
- Damage snapshots and Timberborn-facing damage/death changes applied by Prometheus.
- Workplace disabled state and any tracked workers that need restoration.
- Beaver fire need effects and static beaver-effect caches.
- Recovery snapshots and any Phase 3 renewal state once that state exists.
- Runtime visual effects and temporary visual previews.
- Debug selection/feedback only as needed to avoid stale admin UI.

Reset must not clear:

- `FireProfileSpec` or authored entity profile data.
- Saved Timberborn design state unrelated to Prometheus fire effects.
- QA instruction/result exchange files unless the user explicitly invokes QA cleanup.
- Log history except through the explicit `Clear Log` command.

Every new applier or runtime store must add one reset path before it is considered integrated. The preferred shape is a registry or component contract so reset coverage is searchable and testable instead of duplicated through ad hoc component checks.

## Phase 3 Renewal Boundaries

Phase 3 renewal work starts only from valid aftermath facts, not from visual appearance alone.

| Boundary | Contract |
| --- | --- |
| Eligibility | Valid charred source tagging must decide whether vegetation, buildings, terrain/top-surface cells, or excluded objects can produce renewal value. |
| Attribution | Fertile Ash and controlled-burn rewards must preserve source identity: debug ignition, configured source, burst/explosion source, controlled burn, or unknown. |
| Output model | Fertile Ash must choose one explicit first representation before broad economy work: dropped item, stored resource, gatherable object, or terrain fertility/amendment state. |
| Collection/application | Beaver labor, farmhouse application, gatherable behavior, and field amendment effects stay outside the fire grid and enter through integration adapters. |
| Reset | Renewal state must reset with fire state during admin reset until saved-game behavior is intentionally designed. |
| Tests | Eligibility and non-eligibility tests come before economy outputs so Phase 3 cannot reward arbitrary char. |

Phase 3 may consume grid/entity aftermath facts, but it must not reimplement fire propagation, infer eligibility from particles/material tint, or depend on debug-only controls.

## Source-Of-Truth Pointers

| Question | Source |
| --- | --- |
| What is the sprint sequence and why? | [STABILIZATION_SPRINT.md](STABILIZATION_SPRINT.md) |
| What is currently verified in-game? | [HANDOFF.md](HANDOFF.md) |
| What must be validated next? | [TEST_PLAN.md](TEST_PLAN.md) |
| What are the durable product/design commitments? | [DESIGN.md](DESIGN.md) |
| What are the exact runtime types and telemetry names? | `Assets/Mods/Prometheus/Scripts` |
| What behavior is covered by dependency-light tests? | `tests/Prometheus.Tests` |
