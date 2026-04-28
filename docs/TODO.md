# Prometheus TODO

## 1. Clean Rewrite Baseline

- [x] Remove old direct entity-neighbor spread code.
- [x] Remove spread ignition queues and nearest-target spread helpers.
- [x] Remove responder/dispatch scoring runtime state.
- [x] Remove water context probe and entity water snapshots.
- [x] Remove legacy suppression applier/runtime state.
- [x] Remove floating `FIRE` / `DEAD` text marker path.
- [x] Replace legacy response-profile blueprint keys with neutral `FireProfileSpec`.
- [x] Rewrite old tests so they only cover still-active code.
- [x] Validate with `bash scripts/test.sh && bash scripts/build.sh --launch`.
- [x] Confirm Timberborn loads with the cleaned baseline.

## 2. Sparse 3D Grid Foundation

- [x] Add integer `x/y/z` fire grid coordinates.
- [x] Add chunk coordinates and default `8x8x8` chunk indexing.
- [x] Store dynamic fire cell state separately from environment samples.
- [x] Add sparse active-cell storage and chunk cleanup.
- [x] Add double-buffered grid stepping.
- [x] Add 27-direction kernel shape including self-retention.
- [x] Add foundation tests for coordinate lookup, boundary writes, cleanup, and order independence.

## 3. Environment Sampling

- [x] Sample entity footprints into grid cells.
- [x] Add a Timberborn-facing environment sampling adapter owned outside the dependency-light grid rules.
- [x] Add dependency-light terrain column policy for terrain mass vs top-surface cells.
- [x] Add vegetation `FireProfileSpec` overlays for common trees and bushes.
- [ ] Sample terrain occupancy and terrain top surfaces.
- [ ] Sample block/building occupancy.
- [ ] Sample exposed face masks.
- [ ] Sample water columns/depth as read-only inputs.
- [ ] Sample soil moisture as read-only input.
- [x] Merge entity profile values with terrain, block, water, and moisture inputs into one cell environment.
- [ ] Keep all Timberborn world inputs read-only; write only to Prometheus runtime grid state.
- [x] Derive fuel, moisture dampening, barrier resistance, oxygen availability, and structure kind.
- [x] Add plain C# coverage for merged environment samples where Unity/Timberborn adapters can be isolated.
- [ ] Validate environment sampling in-game with `bash scripts/build.sh --launch` or `bash scripts/build.sh --qa`.

## 4. Combustion And Propagation

- [x] Model initial heat, ember pressure, smoke, ignition progress, fuel consumption, and burn state.
- [x] Use oxygen only for combustion efficiency and flame sustainment.
- [x] Let smoke reduce effective combustion oxygen locally.
- [ ] Bias heat and smoke upward.
- [ ] Bias embers outward across exposed/reachable fuel.
- [x] Make moisture and barriers reduce transfer.
- [x] Use exposed face masks to limit or weight propagation between cells.
- [ ] Keep kernel definitions swappable for 27, 18, 6, or exposed-surface modes.
- [x] Add tests for forest-line spread and face-mask-limited transfer.
- [ ] Add tests for upward heat/smoke bias and outward ember bias.

## 5. Emitting And Source Injection

- [x] Make debug ignite seed grid cells instead of entity burn state.
- [x] Make active burning cells emit heat into nearby cells.
- [x] Make active burning cells emit smoke through exposed neighboring cells.
- [x] Make active burning cells emit ember pressure outward across exposed/reachable fuel.
- [ ] Keep emitted heat, smoke, and embers attributable to grid/source telemetry.
- [ ] Add configured heat-source building injection.
- [ ] Add explosion/firework burst fields without direct nearest-target ignition.
- [ ] Add source telemetry for debugging attribution.
- [x] Add tests proving emission can carry fire through vegetation and is limited by exposed faces.
- [ ] Add tests proving emission is deterministic, bounded, and affected by moisture, barriers, water, and oxygen.
- [ ] Validate visible, attributable spread from one debug-ignited source in game.

## 6. Outcomes

- [ ] Sample entity footprints against nearby grid state.
- [ ] Drive damage from grid heat/ember/smoke pressure.
- [ ] Drive workplace effects from heat/smoke exposure.
- [ ] Drive beaver effects from heat/smoke/fire exposure, not oxygen.
- [ ] Drive vegetation/crop effects from heat, ember pressure, moisture, fuel, and burn state.
- [ ] Keep `Reset Fire Sim` clearing all grid and applied runtime state.

## 7. Visuals

- [ ] Map grid state into smoke, fire, steam, ash, char, and ember-field visuals.
- [ ] Keep local object sparks disabled; sparks come from the ember field.
- [ ] Preserve the current selected-entity visual authoring tool for tuning.
- [ ] Replace entity text markers with a future grid-native overlay.
- [ ] Add overlay modes for heat, ember pressure, smoke, moisture dampening, source contribution, and ignition progress.

## 8. Validation

- [ ] Run plain C# tests for each new grid rule.
- [ ] Run `bash scripts/test.sh && bash scripts/build.sh --launch` after implementation slices.
- [ ] Prefer `bash scripts/build.sh --qa` when a slice needs tests, fresh launch, cleared logs, and Computer Use navigation.
- [x] Verify the in-game `Prometheus` -> `QA` panel can record a `Passed` result.
- [ ] Build a Computer Use screenshot map for startup dialogs, menus, and in-game Prometheus tool groups.
- [ ] Use normal menu loading for live QA until Timberborn's CLI instant-load crash is understood or bypassed.
- [x] Gate expensive Prometheus runtime scans/binding behind Timberborn post-load readiness.
- [x] Use a two-minute save-load threshold before treating a Timberborn load as hung.
- [ ] Use `Fire.log` for runtime evidence.
- [ ] Update [HANDOFF.md](HANDOFF.md), [DESIGN.md](DESIGN.md), and [TEST_PLAN.md](TEST_PLAN.md) when milestone state changes.

## 9. Phase 3 Intentional Fire And Ash Harvest

- [x] Create the Phase 3 ticket set under [tickets/](tickets/).
- [x] Document controlled burns as emergent containment, not a separate runtime mechanic.
- [ ] Add a player-facing `Ignite Selected` action that reuses the forced-ignition/grid-seeding path.
- [ ] Reject invalid selected targets without fire profiles and report UI feedback plus telemetry.
- [ ] Extend aftermath eligibility so valid burned crops can produce Fertile Ash.
- [ ] Add crop/source-kind classification, ash amount policy, and dependency-light tests.
- [ ] Live-verify crop ash with `fertile_ash_*` telemetry, visible recovered-good stack, District Center storage, and clean Prometheus logs.
- [ ] Prove containment across moisture, water, barriers, exposed faces, spacing, and profile differences.
- [ ] Capture at least one prepared burn that stays bounded and one unprepared/control burn that spreads more aggressively.
- [ ] Map enough runtime grid/exposure state into smoke, fire, steam, char, and ember visuals for prepared burns to be readable without debug logs.
- [ ] Keep farmhouse ash application deferred until [tickets/deferred/TKT-001-farmhouse-fertile-ash-application.md](tickets/deferred/TKT-001-farmhouse-fertile-ash-application.md) has fresh fixture evidence.
- [x] Stabilize tree dead and stump lifecycle under [tickets/verify/TKT-007-tree-dead-stump-lifecycle.md](tickets/verify/TKT-007-tree-dead-stump-lifecycle.md).
- [x] Decide and implement unified burned-ground Fertile Ash recovery under [tickets/verify/TKT-008-unified-burned-ground-ash-recovery.md](tickets/verify/TKT-008-unified-burned-ground-ash-recovery.md).
- [x] Extend aftermath parity to buildings and crops under [tickets/verify/TKT-009-building-crop-aftermath-parity.md](tickets/verify/TKT-009-building-crop-aftermath-parity.md).
- [ ] Prove and enable the farmhouse Fertile Ash application scaffold under [tickets/blocked/TKT-010-agriculture-fertile-ash-application.md](tickets/blocked/TKT-010-agriculture-fertile-ash-application.md).

## 10. Fire Suppression

- [x] Add the first suppression slice under [tickets/verify/TKT-011-fire-suppression-first-slice.md](tickets/verify/TKT-011-fire-suppression-first-slice.md).
- [x] Slow suppression-era fire response at the heartbeat or pacing layer rather than undoing accepted tree and crop tuning.
- [x] Capture a live suppressed-vs-unsuppressed comparison.

## 11. Standing Project Habit

- [ ] Keep this TODO updated before and after each meaningful implementation slice.
- [ ] Move completed milestone facts into [HANDOFF.md](HANDOFF.md) when they are verified.
- [ ] Update [DESIGN.md](DESIGN.md) when a durable design decision changes.
- [ ] Update [TEST_PLAN.md](TEST_PLAN.md) when validation gates or QA workflows change.
- [ ] Keep final session closeouts anchored to outcome, verification, and the next unchecked TODO item.
