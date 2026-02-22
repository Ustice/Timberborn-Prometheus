# Prometheus Design Changelog Archive

This file preserves completed design/change-log history moved from `DESIGN.md` to keep the active design doc focused on upcoming work.

## Archived entries

| Date | Phase | Update | Status |
| --- | --- | --- | --- |
| 2026-02-21 | Phase 5 | Documented Prometheus dev loop (compile/deploy/log-check/debug-copy) and linked standalone QA runbook in `TEST_PLAN.md` | Done |
| 2026-02-21 | Phase 2/3/5 | Cleaned sprint tracking into carryover vs archived implementation checklists and added prepared Phase 3 kickoff checklist for immediate handoff | Done |
| 2026-02-21 | Phase 5 | Added ready-to-run dual-front verification protocol with telemetry checklist, pass/fail thresholds, and tuning adjustment order for sprint close-out validation | Done |
| 2026-02-21 | Phase 2 | Implemented assignment lock+hysteresis scoring flow with stable assigned-score tracking and retarget suppression telemetry (`FireSimulationController` + `FireDispatchScoringRuntimeState` + debug panel fields) | Done |
| 2026-02-21 | Phase 2 | Added first faction asymmetry suppression hooks: Folktails relay-distance efficiency penalty (water-exposure proxy) and Ironteeth high-heat suppression bonus in quenching math | Done |
| 2026-02-21 | Phase 2/5 | Added response-state transition notifications (`overwhelmed`/`contained`/`stabilized`) with cooldown throttling to reduce spam | Done |
| 2026-02-21 | Phase 2 | Extended `FireResponseProfileSpec`/`FireResponseProfile` with dispatch weight + lock/hysteresis tuning defaults and applied them to runtime scoring/suppression snapshots | Done |
| 2026-02-21 | Phase 2 | Started active sprint implementation by adding `FireDispatchScoringRuntimeState`, wiring first dispatch score factors in `FireSimulationController`, and exposing dispatch telemetry in `PrometheusFireDebugFragment` | Done |
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
