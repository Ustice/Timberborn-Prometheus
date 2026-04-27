# Prometheus Architecture

This document describes how Prometheus should work as a system. It owns durable boundaries and contracts, not sprint sequence, ticket history, exact file lists, or current implementation inventory.

Source of truth: exact class names, telemetry names, file layout, and current implementation details live in `Assets/Mods/Prometheus/Scripts`, `tests/Prometheus.Tests`, and active QA logs.

## System Goals

Prometheus adds a readable fire and renewal loop to Timberborn:

- Fire risk is data-driven by entity profile, environment, source pressure, and local fire-field state.
- Spread is field-first: heat, ember pressure, smoke, moisture, oxygen, barriers, and fuel shape outcomes.
- Runtime effects are resettable and observable.
- Visuals explain the runtime state without becoming gameplay truth.
- Renewal systems consume valid aftermath facts instead of inferring rewards from particles, material tint, or debug controls.

## Ownership Layers

| Layer | Owns | Does Not Own |
| --- | --- | --- |
| Fire profiles | Static configuration: burnability, fuel, moisture resistance, barrier resistance, ignition threshold, source fields, structure kind, and operation requirements | Runtime fire state, world sampling, rewards, or visual effects |
| World adapters | Timberborn-facing facts: entity identity, footprints, operation state, terrain, water, soil, storage, workers, and fragile reflection or component-cache access | Fire rules, tuning policy, or long-lived gameplay state |
| Fire field | Sparse 3D field state: coordinates, chunks, environment, heat, ember pressure, smoke, ignition progress, cooling, decay, and aggregate sampling | Timberborn component lookup, entity lifecycle, UI commands, or economy outputs |
| Source injection | Translation of configured sources, debug ignition, explosions, and future controlled burns into attributed field pressure | Direct entity mutation, visual application, or reward creation |
| Entity exposure | Per-entity ignition, fuel, moisture, burn state, grid sampling, and published exposure snapshots | Global grid storage, source attribution policy, or Timberborn reflection |
| Effects | Damage, workplace state, beaver exposure, recovery queues, and other Timberborn-facing mutations from snapshots | Propagation rules, profile data, or hidden state without reset coverage |
| Visual projection | Mapping explicit field and entity facts into fire, smoke, steam, embers, desiccation, and char presentation | Gameplay facts, reward eligibility, or source attribution |
| Renewal | Valid aftermath classification, Fertile Ash production, field amendment state, and future controlled-burn rewards | Fire propagation, ignition probability, or debug-only behavior |
| Debug and QA | Admin commands, inspection, evidence capture, log viewing, QA exchange files, temporary visual authoring, and reset orchestration | Runtime source of gameplay truth |
| Tests and scripts | Dependency-light rule coverage, build/test/deploy entrypoints, QA launch workflow, and concurrency locking | In-game proof unless paired with logs, screenshots, or QA panel results |

## Data Flow

1. Profile data defines static fire behavior.

   - Profiles describe fuel, moisture resistance, barrier resistance, source fields, structure kind, thresholds, and operation requirements.
   - Profiles are read-only inputs. They do not mutate runtime state.

2. World adapters translate Timberborn state into Prometheus facts.

   - Entity footprint, operation state, terrain, water, soil, storage, and worker facts enter through explicit adapters.
   - Fragile reflection, component-cache traversal, and string type-name matching stay centralized behind adapter boundaries.
   - Scene-wide scans are allowed for debug/admin discovery, but normal fire rules should consume cached or adapter-provided facts.

3. Sources inject pressure into the fire field.

   - Debug ignition, configured heat sources, explosion/burst sources, and controlled burns use the same source-injection path.
   - Every injection preserves attribution: source kind, entity or coordinate when known, reason, and amount.
   - Source injection creates field pressure; it does not directly kill entities or grant rewards.

4. The fire field advances sparse 3D state.

   - Field state stores heat, ember pressure, smoke, ignition progress, environment facts, and active-cell metadata.
   - Propagation policy handles bounded transfer, upward heat and smoke bias, outward ember pressure, cooling, decay, water, barriers, exposed faces, and oxygen.
   - Stepping ownership is centralized so the simulation clock is not defined by whichever entity happened to update first.

5. Entities sample the field and update exposure.

   - Entity exposure owns stochastic ignition, fuel loss, moisture loss, burn state, burned-out state, and exposure snapshots.
   - Entities consume local field pressure and profile data. They do not own global propagation.
   - Burned-out state is a gameplay fact that downstream damage, visuals, and renewal systems may consume.

6. Effects apply snapshots into Timberborn.

   - Appliers translate exposure or projection snapshots into damage, workplace disablement/restoration, beaver effects, recovery queues, and other Timberborn-facing mutations.
   - Every applier defines what it mutates, how reset clears or restores it, and what telemetry proves the mutation occurred.
   - Appliers should be narrow and idempotent enough that repeated ticks do not duplicate durable side effects.

7. Visual projection explains current state.

   - Visual rules consume explicit field and entity facts, then choose flame, smoke, steam, ember, desiccation, and char presentation.
   - Visuals can lag or simplify the simulation for readability, but they must not invent gameplay facts.
   - Visual authoring previews are temporary debug artifacts and must not affect saved runtime state.

8. Renewal consumes valid aftermath.

   - Renewal starts from accepted aftermath facts such as valid burned vegetation, valid burned structures, or future terrain/top-surface eligibility.
   - Fertile Ash and field amendments are rewards or recovery state, not fire propagation state.
   - Farmhouse, beaver labor, storage, gatherable stacks, and application behavior enter through Timberborn adapters.

9. Debug, QA, tests, and docs observe the system.

   - Debug UI can issue admin commands and capture evidence, but source, spread, damage, recovery, and renewal facts must remain owned by runtime services.
   - Tests own dependency-light behavior guarantees.
   - Live QA owns claims about rendered UI, Timberborn behavior, save loading, and in-game evidence.
   - Docs preserve intent, contracts, verified behavior, and source-of-truth pointers.

## Runtime Contracts

| Contract | Requirement |
| --- | --- |
| Source attribution | Every source-driven field injection records source kind and enough context to explain what started or intensified fire pressure. |
| Dependency-light rules | Propagation, ignition probability, eligibility, and tuning decisions should be testable without Unity or Timberborn. |
| Thin adapters | Unity and Timberborn APIs should be isolated behind adapter boundaries wherever practical. |
| Reset coverage | Every runtime store and applier that mutates state participates in `Reset Fire State`. |
| Observable effects | Important runtime mutations emit stable telemetry or appear in QA evidence. |
| Visual separation | Visual state derives from runtime facts and never becomes the source of gameplay truth. |
| Debug isolation | Debug/admin controls can request changes, but they do not own production behavior. |

## Reset Contract

`Reset Fire State` is the broad admin reset. Its contract is a clean Prometheus fire simulation state for loaded fire entities without changing authored design data or unrelated Timberborn state.

Reset clears:

- Fire field cells, active-cell metadata, source pressure, and pending global grid activity.
- Exposure snapshots, forced ignition requests, per-entity ignition flags, fuel loss, moisture loss, and burned-out flags.
- Prometheus-applied damage snapshots and Timberborn-facing damage/death changes where a safe restoration path exists.
- Workplace disabled state and tracked worker effects.
- Beaver fire effects and static beaver-effect caches.
- Prometheus-owned recovery queues, ash telemetry state, and field amendment runtime state.
- Runtime visual effects and temporary visual previews.
- Debug selection or feedback only when needed to avoid stale admin UI.

Reset does not clear:

- Authored profile data.
- Timberborn design state unrelated to Prometheus fire effects.
- Timberborn-owned recovered-good entities that Prometheus did not create or cannot safely own.
- QA instruction/result exchange files unless the user explicitly invokes QA cleanup.
- Log history except through the explicit `Clear Log` command.

## Timberborn Integration Boundary

Timberborn integration is intentionally narrow because game internals can change.

Adapters should own:

- Component-cache traversal and scene object discovery.
- Type-name classification for Timberborn objects when no stable public type is available.
- Reflection probes and compatibility summaries.
- Selection/focus APIs for debug UI.
- Good/storage/recovered-stack/farmhouse/farm-worker interactions.
- Save-load and runtime lifecycle hooks.

Rules should receive normalized facts from adapters instead of calling Timberborn APIs directly. Missing or changed Timberborn APIs should degrade through one compatibility path with clear telemetry, not scattered failures.

## Renewal Boundary

Renewal systems consume fire aftermath, but they do not control fire behavior.

| Boundary | Contract |
| --- | --- |
| Eligibility | Decide whether a burned entity, structure, or terrain/top-surface cell can produce renewal value. |
| Attribution | Preserve whether aftermath came from debug ignition, configured source, burst/explosion, controlled burn, or unknown. |
| Output model | Keep Fertile Ash, gatherable stacks, storage, and field amendments explicit and narrow. |
| Application | Farmhouse/farmer application, beaver labor, and crop-growth amendments use Timberborn adapters and renewal state. |
| Reset | Prometheus-owned renewal runtime state resets with fire state until saved-game behavior is intentionally designed. |
| Tests | Eligibility and non-eligibility coverage comes before broad reward expansion. |

## Evidence And Validation

| Claim Type | Required Evidence |
| --- | --- |
| Dependency-light rule behavior | Plain C# tests through `bash scripts/test.sh`. |
| Build/deploy behavior | `bash scripts/build.sh --help` and successful script output. |
| Runtime startup | `Player.log`, `Fire.log`, and clean Prometheus startup scan. |
| Live game behavior | Computer Use screenshot/click evidence plus `Player.log` and `Fire.log`. |
| QA lock behavior | `scripts/build.sh` lock output and, for QA sessions, explicit `--release-qa-lock`. |
| Documentation-only changes | `git diff --check` and source-of-truth link review. |

## Source-Of-Truth Pointers

| Question | Source |
| --- | --- |
| What is currently verified in-game? | [HANDOFF.md](HANDOFF.md) |
| What must be validated next? | [TEST_PLAN.md](TEST_PLAN.md) |
| What are the durable product/design commitments? | [DESIGN.md](DESIGN.md) |
| How do orchestration runs work? | [ORCHESTRATION.md](ORCHESTRATION.md) |
| Where is active ticket state? | [tickets/README.md](tickets/README.md) |
| What are the exact runtime types and telemetry names? | `Assets/Mods/Prometheus/Scripts` |
| What behavior is covered by dependency-light tests? | `tests/Prometheus.Tests` |
| Where is Phase 2 sprint history? | [ARCHIVE/stabilization-sprint-2026-04/README.md](ARCHIVE/stabilization-sprint-2026-04/README.md) |
