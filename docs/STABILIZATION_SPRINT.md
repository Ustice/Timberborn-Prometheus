# Prometheus Stabilization Sprint

Last analyzed: 2026-04-26

Status: closed and archived. Active ticket tracking now lives in [tickets/README.md](tickets/README.md), and the closed sprint ticket board lives in [ARCHIVE/stabilization-sprint-2026-04/](ARCHIVE/stabilization-sprint-2026-04/).

## Purpose

This sprint is the bridge between the Phase 2 grid rewrite and Phase 3 renewal economy work. The goal is not broad polish. The goal is to reduce architectural risk, make live fire behavior easier to reason about, and put enough guardrails in place that Phase 3 can build on verified systems instead of debug-only scaffolding.

Source of truth: current code owns exact APIs, telemetry names, and file layout. This document owns the stabilization priorities and acceptance gates.

## Current Architectural Shape

| Area | Current Shape | Main Concern |
| --- | --- | --- |
| Grid core | `FireGridRuntimeState.cs` owns coordinates, chunks, footprints, kernels, stepping, transfer rules, and sampling | Too many reasons to change in one file |
| Entity fire lifecycle | `FireExposureController.cs` bridges profile data, entity fuel/moisture, grid sampling, debug ignition, stepping, and telemetry | Entity-owned lifecycle and grid-owned field state are still tightly coupled |
| Debug UI | `PrometheusFireDebugFragment.cs` owns selection bridge, bottom panel shell, commands, QA, logs, visual authoring, scene scans, and reset orchestration | Largest module and highest accidental breakage risk |
| Visuals | Runtime visual applier, visual authoring, preview runtime state, and native source discovery are separate files but repeat source catalog and particle logic | Authoring and runtime paths can drift |
| Timberborn integration | Reflection, cached-component scans, string component names, and scene-wide lookups live in several modules | Fragile integration points are not centralized enough |
| QA scripts | `scripts/build.sh` owns build, deploy, launch, menu automation, and readiness | Useful, but large and hard to test in pieces |
| Tests | Plain C# suite covers rule-heavy code, grid propagation basics, visual rules, and some string classifiers | Tests are concentrated in one file and do not cover integration rakes well |

## Needlessly Verbose Or Repetitive Code

- `PrometheusFireDebugFragment.cs` is doing panel layout, tab state, log filtering, QA file IO presentation, visual authoring controls, selection state, entity focus, reset commands, and scene scanning. The 1,767-line size is not the problem by itself; the problem is that a small UI change can accidentally touch admin reset behavior or selection focus.
- Reset logic repeats direct `GetComponent<T>()` and `ComponentCache.TryGetCachedComponent<T>()` calls for each fire component. This appears in `HasFireResetComponent` and `ResetLoadedFireEntity`, and it is easy to forget one component when adding a new runtime effect.
- Component-cache reflection is duplicated between debug reset/focus and beaver need manager discovery. The code has at least two versions of "get cached components out of Timberborn's `ComponentCache`", each with its own failure behavior.
- Native particle source discovery exists in both runtime visual application and visual preview/catalog code. The lists and scoring are close relatives but live separately, so a native asset choice can validate in authoring and still differ at runtime.
- Grid rules use many embedded coefficients in `FireGridKernel`, `Transfer`, `FinalizeCell`, `FireExposureController`, and visual rules. Some are tested, but they are not named as tuneable policy yet.
- The tests are effective but monolithic. `tests/Prometheus.Tests/Program.cs` is now a compact test runner plus all scenarios plus helpers, which makes it harder to spot coverage gaps by subsystem.

## Unclear Or Overlarge Module Purposes

- `FireGridRuntimeState.cs` should become a package, not a single file. Split it into grid value types, chunk storage, footprint sampling, kernel policy, propagation rules, and runtime state.
- `FireExposureController.cs` should stop being the place where environment sampling, stochastic ignition, fuel lifecycle, source emission, grid stepping, and telemetry meet. A better boundary is: entity adapter reads Timberborn/profile state, lifecycle rules decide entity state, grid runtime stores field state, and source injectors emit into the grid.
- `PrometheusDebugPanel` should be split into a panel shell plus tab controllers: actions, logs, QA, visuals, and selection. The shell should only manage open/close/tab state and shared feedback.
- Fire effects on buildings, workers, beavers, damage, recovery, and visuals all depend on runtime snapshots but do not share a single "applied effect contract." Reset behavior is currently implicit in each applier and debug command.
- `FireProfileSpec` mixes burnability, ignition thresholds, configured source fields, and operation requirements. That may be the right data surface eventually, but the runtime currently consumes only part of it, which makes it easy to misread Phase 2 readiness.

## Footguns And Rakes

- Scene-wide scans such as `Resources.FindObjectsOfTypeAll<GameObject>()` and `Object.FindObjectsByType<Component>()` are useful for debug tools but risky as normal runtime dependencies. Keep them behind debug/admin services or cached integration indexes.
- Reflection against Timberborn internals is spread out: need manager methods, growable/deteriorable methods, natural-resource state properties, and cached component internals. Each can silently stop working after a game update.
- String component-name classification drives damage category and workplace operation disabling. It is tested at the string-rule level, but it remains a compatibility risk because it relies on Timberborn type names staying stable.
- Debug/admin actions mutate live entities broadly. The previous direct-destroy crash was fixed, but reset and clear paths still deserve explicit safety tests and live QA gates before Phase 3 depends on them.
- `FireGridRuntimeState.StepOncePerFrame(Time.frameCount, FireGridKernel.Full27)` is called from each entity controller. The frame gate prevents duplicate steps, but stepping ownership is implicit and spread across entity updates.
- Configured source fields already exist in `FireProfileSpec`, but active source injection is not implemented. This is a "looks wired" trap for high-risk buildings, explosion sources, and Phase 3 controlled-burn triggers.
- The QA script's menu automation depends on coordinates and app focus. It is valuable, but any claim from it should still be tied to `Player.log`, `Fire.log`, and screenshots.

## Shortcuts To Address Before Phase 3

- Runtime visuals are still mostly entity-snapshot driven. They need a clear mapping from grid heat, ember pressure, smoke, moisture, ignition progress, and burn state into visual rules.
- Environment sampling is not yet a real Timberborn world adapter. Terrain top surface, block/building occupancy, exposed faces, water depth, and soil moisture remain planned inputs.
- Source attribution exists as telemetry names and snapshot fields, but the grid does not preserve contribution/source identity. Phase 3 will need attribution for controlled burns, valid ash sources, and player-readable "what started this" answers.
- Fertile Ash is directionally present as design/content, but valid charred-source tagging and collection behavior are not stabilized.
- Worker/beaver exposure logic still leans on reflection and scene scans. It needs an integration boundary and at least one current live validation pass after grid behavior settles.
- Build and test tooling has good one-command UX, but there is no architectural regression check for accidental docs shipping, missing compile items, stale telemetry constants, or untested new rule files.

## Phase 2 To Phase 3 Risks

| Risk | Why It Matters | Stabilization Response |
| --- | --- | --- |
| Grid behavior looks correct only for forced ignition | Phase 3 needs controlled burns and source-driven fires, not only debug ignition | Add configured source injection and live source-attribution QA |
| Grid, entity lifecycle, and visuals drift apart | Players may see char/smoke/fire that no longer matches actual fire state | Make visual rules consume explicit grid/entity projection data |
| Reset/admin actions miss one applied effect | QA becomes unreliable and saved games can keep stale state | Add a reset contract and component registry for every applier |
| Reflection breaks silently | Timberborn updates can disable damage, beaver, workplace, or recovery effects | Centralize reflection probes and log one compatibility summary |
| Performance degrades with more active cells | Sparse grid can become expensive once forest spread works | Add active-cell/chunk telemetry and bounded propagation tests |
| Phase 3 economy builds on untagged aftermath | Fertile Ash and renewal loops can become arbitrary rewards | Stabilize valid char source tagging before adding economy flows |

## What Needs To Be In Place

- A small architecture map in code: grid storage, propagation policy, source injection, entity lifecycle, effects, visuals, debug UI, and Timberborn integration.
- A central Timberborn integration service for cached component traversal, scene object indexing, reflection probes, and type-name classification.
- A reset/apply contract for every runtime effect: what it mutates, how reset clears it, and what telemetry proves reset happened.
- Source injection and attribution contracts: debug ignition, configured heat source, burst/explosion source, and eventual controlled-burn source should use the same path.
- Plain C# tests split by subsystem, with named coverage gaps visible from file names.
- A live QA evidence matrix for Phase 2 exit: forest spread, profiles, source injection, moisture/water/firebreaks, visuals, reset, worker/building/beaver effects, and Fertile Ash tagging.
- Performance/telemetry counters for active chunks, active cells, source injections, propagation bounds, and reset counts.

## Stabilization Sprint Plan

Timebox: one focused sprint before substantial Phase 3 feature work.

### 1. Make The Architecture Smaller Without Changing Behavior

- Split `FireGridRuntimeState.cs` into value types, chunk storage, kernel policy, propagation rules, footprint sampling, and runtime state.
- Split `PrometheusFireDebugFragment.cs` into panel shell plus actions, log, QA, visual authoring, and selection components.
- Move component-cache traversal and loaded-entity lookup into one integration helper.
- Keep behavior equivalent; use existing tests and live startup as the acceptance gate.

Acceptance: no feature behavior changes, tests pass, Timberborn starts cleanly, and reset/selection/debug panels still work.

### 2. Centralize Fragile Timberborn Integration

- Create one home for reflection probes and string type-name policies.
- Log one compatibility summary for damage, recovery, beaver need, workplace, cached component, and selection/focus APIs.
- Replace duplicated cached-component reflection in debug and beaver code.
- Add plain C# tests for classification policies and reflection probe result normalization where possible.

Acceptance: reflection and type-name assumptions are searchable from one module, and a missing API produces one clear log/warning path.

### 3. Stabilize The Grid Contract

- Add named propagation/source policy objects instead of embedding coefficients only inside methods.
- Add tests for upward heat/smoke bias, outward ember bias, bounded transfer, source injection, and water/barrier/oxygen interactions.
- Add active chunk/cell/source telemetry so live QA can tell whether spread is bounded or runaway.
- Define stepping ownership explicitly, ideally from one runtime coordinator rather than every entity controller.

Acceptance: grid behavior has deterministic tests for every Phase 2 acceptance criterion that can be dependency-light, and live logs identify source, active cells, and propagation bounds.

### 4. Wire Configured Sources And Attribution

- Implement configured heat/ember/smoke source injection from `FireProfileSpec`.
- Respect `RequiresOperation` with a thin Timberborn-facing operation adapter.
- Preserve attribution through debug ignition, configured source injection, and burst injection.
- Add live QA for one high-risk source and one low-risk non-source.

Acceptance: a configured source can cause stochastic field ignition without direct nearest-target ignition, and logs can answer what started the fire.

### 5. Reconnect Runtime Visuals To The New Model

- Define one projection from grid/entity state to visual state.
- Use the promoted authoring defaults without duplicating source catalog behavior.
- Keep object fire/smoke/steam/char separate from future ember-field overlays.
- Add live QA screenshots for wet, dry, burning, burned-out, and reset states.

Acceptance: visual state matches runtime telemetry for representative vegetation and building targets.

### 6. Prepare Phase 3 Renewal Contracts

- Define valid charred source tagging: vegetation, building, terrain/top-surface, excluded objects.
- Decide whether Fertile Ash is dropped, stored, generated as a gatherable, or represented as terrain fertility first.
- Add reset behavior for all renewal state.
- Add tests for eligibility and non-eligibility before adding economy outputs.

Acceptance: Phase 3 starts with a narrow, tested "valid aftermath" contract instead of broad reward logic.

### 7. Upgrade Validation And Closeout Habits

- Split `tests/Prometheus.Tests/Program.cs` into subsystem files while keeping `bash scripts/test.sh` as the entrypoint.
- Add a lightweight docs/deploy guard that confirms no internal Markdown files live under `Assets/Mods/Prometheus`.
- Add a telemetry constant test for QA panel events, not only `FireTelemetryEvents`.
- Keep `docs/HANDOFF.md`, `docs/TODO.md`, and `docs/TEST_PLAN.md` updated after each stabilization slice.

Acceptance: a future session can run one command, see current sprint state, and know the next unchecked stabilization item.

## Sprint Exit Gates

- `bash scripts/test.sh` passes.
- `bash scripts/build.sh --qa` passes and Computer Use reaches the normal menu or target save without Prometheus startup errors.
- Live QA records at least one pass for source-driven spread, moisture/firebreak dampening, runtime visuals, reset, and Fertile Ash eligibility.
- No Prometheus exceptions are present in the scanned `Player.log` or `Fire.log` window.
- Debug/admin actions remain safe: `Stop Fires`, `Reset Fire State`, `Clear Beavers`, `View`, visual preview, and QA result recording.
- Phase 3 has a tested aftermath contract and does not need to infer it from debug visuals or one-off live behavior.
