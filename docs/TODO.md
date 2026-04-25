# Prometheus TODO

## 1. Clean Rewrite Baseline

- [x] Remove old direct entity-neighbor spread code.
- [x] Remove spread ignition queues and nearest-target spread helpers.
- [x] Remove responder/dispatch scoring runtime state.
- [x] Remove water context probe and entity water snapshots.
- [x] Remove suppression applier/runtime state.
- [x] Remove floating `FIRE` / `DEAD` text marker path.
- [x] Replace `FireResponseProfileSpec` blueprint keys with neutral `FireProfileSpec`.
- [x] Rewrite old tests so they only cover still-active code.
- [x] Validate with `bash scripts/test.sh && bash scripts/build.sh --launch`.
- [x] Confirm Timberborn loads with the cleaned baseline.

## 2. Sparse 3D Grid Foundation

- [x] Add integer `x/y/z` fire grid coordinates.
- [x] Add chunk coordinates and default `8x8x8` chunk indexing.
- [x] Store dynamic fire cell state separately from environment samples.
- [x] Add sparse active-cell storage and chunk cleanup.
- [x] Add double-buffered simulation stepping.
- [x] Add 27-direction kernel shape including self-retention.
- [x] Add foundation tests for coordinate lookup, boundary writes, cleanup, and order independence.

## 3. Environment Sampling

- [x] Sample entity footprints into grid cells.
- [ ] Sample terrain occupancy and terrain top surfaces.
- [ ] Sample block/building occupancy.
- [ ] Sample exposed face masks.
- [ ] Sample water columns/depth as read-only inputs.
- [ ] Sample soil moisture as read-only input.
- [x] Derive fuel, moisture dampening, barrier resistance, oxygen availability, and structure kind.

## 4. Combustion And Propagation

- [ ] Model heat, ember pressure, smoke, ignition progress, fuel consumption, and burn state.
- [x] Use oxygen only for combustion efficiency and flame sustainment.
- [x] Let smoke reduce effective combustion oxygen locally.
- [ ] Bias heat and smoke upward.
- [ ] Bias embers outward across exposed/reachable fuel.
- [x] Make moisture and barriers reduce transfer.
- [ ] Keep kernel definitions swappable for 27, 18, 6, or exposed-surface modes.

## 5. Source Injection

- [x] Make debug ignite seed grid cells instead of entity burn state.
- [ ] Make active burning cells emit into nearby cells.
- [ ] Add configured heat-source building injection.
- [ ] Add explosion/firework burst fields without direct nearest-target ignition.
- [ ] Add source telemetry for debugging attribution.

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
- [ ] Use `Fire.log` for runtime evidence.
- [ ] Update [HANDOFF.md](HANDOFF.md), [DESIGN.md](DESIGN.md), and [TEST_PLAN.md](TEST_PLAN.md) when milestone state changes.
