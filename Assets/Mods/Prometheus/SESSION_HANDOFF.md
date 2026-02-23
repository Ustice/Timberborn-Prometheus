# Prometheus Session Handoff

## Last updated

2026-02-22

## Project brief

Prometheus is a Timberborn mod that adds a systemic fire gameplay loop across ignition, spread, suppression, and recovery.
It introduces faction-aware firefighting behavior (Folktails logistics-style response, Ironteeth industrial response), fire-related runtime simulation controllers, debug instrumentation, and tuning profiles (`Low`/`Standard`/`High`).
Current focus is validating and tuning the live simulation to be dangerous but manageable, with clear telemetry for debugging and balancing.

## Why we are doing this

Current sprint focus is **implementation-first system integration**, then tuning.
We are prioritizing complete interaction loops (spread, explosive hazard behavior, recovery, festival risk coupling) before heavy balancing so tuning happens against stable mechanics.

## What we are actively working on

1. Validate explosion telemetry end-to-end with low-noise evidence in `Fire.log`.
2. Explain why `explosion_ignite_applied` is absent despite `explosion_ignition_request` reaching `chance=1.000`.
3. Keep deploy workflow deterministic (compile-first + launch + auto-clear `Player.log` and `Fire.log`).

## Confirmed results so far

### Infrastructure / workflow

- Build/deploy entrypoint is now `scripts/build.sh` (old `scripts/deploy_prometheus.sh` removed).
- Deploy test harness `scripts/test_deploy_prometheus.sh` was intentionally removed for tight-loop iteration.
- Build flow supports safe restart and stale-build protection through `--launch` (safe stop + stale DLL guard with no bypass override).
- Build flow compiles via `dotnet build` before deployment (project-based compile-first pipeline) and promotes DLL/PDB to `Library/ScriptAssemblies`.
- `--launch` clears both `~/Library/Logs/Mechanistry/Timberborn/Player.log` and `~/Library/Logs/Mechanistry/Timberborn/Fire.log` before game launch.
- Build script prefixes/env names were normalized (`[build]`, `BUILD_SKIP_COMPILE`, `FIRE_LOG_PATH`, `TIMBERBORN_FIRE_LOG_PATH`).
- Skill folders and skill names were normalized (`deploy`, `build-deploy`, `session-handoff`) with updated shared checklist/template paths.

### Debug panel / usability

- Debug panel is now scrollable and copy-capable.
- Copy feedback/status added.
- Details section is collapsible by default.
- Added `Ignite` debug button to queue forced ignition requests.

### Runtime stability fixes

- Fixed multiple null-reference crashes caused by missing DI attributes.
- Added first-tick snapshot behavior (suppression/simulation no longer strictly gated on waiting 1 second after first attach).
- Added baseline explosion ignition policy support (`Off`/`HighOnly`/`Always`) with moisture-mitigated ignition checks and temporary suppression disruption after detonation.

### Logging / observability

- Structured fire event logs continue in `Player.log` with tag `[Prometheus/Fire]`.
- Added dedicated mirrored fire log at `~/Library/Logs/Mechanistry/Timberborn/Fire.log` for low-noise analysis.
- Confirmed event stream includes: `debug_ignite_request`, `debug_ignite_applied`, `ignited`, `burning_tick`, `response_state`, `explosion_detonated`, `explosion_ignition_request`, `explosion_ignite_applied`.

### Behavioral signal from captured runs

- Fire loop is alive and producing telemetry.
- In tested Standard/Bakery scenario, spread can exceed quench and trend to `Overwhelmed`.
- This is now a **tuning/balance** issue, not a wiring/visibility/crash issue.
- Captured clean single-ignite telemetry sequence in latest run window (`debug_ignite_request` -> `debug_ignite_applied` -> `ignited` -> `burning_tick`) with `spread=0.097`, `quench=0.075`, and rising intensity (`0.371 -> 0.478`).
- Confirmed active spread propagation is functioning in live gameplay (secondary target ignition occurred quickly); defer range/rate tuning until broader system interactions are implemented.
- Confirmed explosives factory is now fire-profiled and shows FIRE state/ignition behavior in live gameplay.
- Verified deterministic debug detonation on completed Explosives Factory:
   - `debug_ignite_request` and `debug_ignite_applied` present,
   - `explosion_ignition_request` present (`mode=Off forced=True`),
   - `explosion_detonated` present (`mode=Off forced=True`).
- Observed repeated `spread_propagation` to requested explosion target with rising chance to `1.000`, but still no `explosion_ignite_applied` in captured windows.

## Open issues / hypotheses

1. **`explosion_ignite_applied` remains absent in validated forced runs**
   - Hypothesis: target entity eligibility/state/consumption timing prevents apply event despite queued request.
2. **Mode-by-mode policy validation is still pending after temporary debug forcing changes**
   - Current proof confirms forced debug path, not final policy behavior under pure runtime gating.
3. **Balance harshness in Standard remains intentionally untuned**
   - Spread can still outpace quench in the current baseline; tuning deferred until interaction systems are complete.
4. **Damage categorization/timing may still be too aggressive in some cases**
   - Continue validating `FireDamageCategory` detection and severity ramp under repeated/manual ignites.
5. **Repeated ignite presses can distort comparison baselines**
   - Prefer one ignite event per measurement window when collecting comparison telemetry.

## Next steps (priority order)

1. Run one `bash scripts/build.sh --launch` smoke pass after naming changes and confirm expected output markers (`[build]` prefix + `Fire.log` clearing).
2. Add targeted instrumentation for spread-ignition request lifecycle:
   - log when explosion spread requests are consumed/ignored,
   - log target eligibility reasons when apply does not occur.
3. Re-run single-ignite Explosives Factory scenario and capture both logs:
   - `Player.log` for full engine context,
   - `Fire.log` for concise fire-event timeline.
4. Capture and archive representative windows containing:
   - `explosion_detonated`,
   - `explosion_ignition_request`,
   - `explosion_ignite_applied`.
5. After request/apply tracing is understood, run policy validation pass (`Off` / `HighOnly` / `Always`) without temporary forcing.
6. Implement **Phase 3 recovery + controlled-burn envelope** end-to-end:
   - controlled-burn eligibility gates,
   - ashen-soil lifecycle values exposed in runtime/debug,
   - reward differentiation between managed vs catastrophic burns.
7. Implement **Phase 4 fireworks risk coupling** baseline:
   - high-intensity fireworks mode with explicit risk multiplier,
   - safety/readiness modulation shared with suppression/water context.
8. Add integration-first validation scenarios (not tuning-heavy):
   - explosion-adjacent burn,
   - planned controlled burn near farms,
   - fireworks event under weak vs strong safety prep.
9. After all systems interact correctly, run one consolidated tuning pass across `Low`/`Standard`/`High`.

## How to quickly resume

1. From repository root, run:
   - `bash scripts/build.sh --launch`
2. In game:
   - Select profiled Explosives Factory (or Bakery/Coffee Brewery for baseline comparison).
   - Unpause.
   - Press `Ignite` once.
3. Collect:
   - panel snapshot,
   - `~/Library/Logs/Mechanistry/Timberborn/Fire.log` (preferred),
   - `~/Library/Logs/Mechanistry/Timberborn/Player.log` (fallback/full context).
4. Verify key markers for run objective:
   - `debug_ignite_request`,
   - `debug_ignite_applied`,
   - `explosion_detonated`,
   - `explosion_ignition_request`,
   - `explosion_ignite_applied`.

## Important files / references

### Core docs

- [Design doc](./DESIGN.md)
- [Test plan](./TEST_PLAN.md)
- [Explosion policy validation checklist](./EXPLOSION_POLICY_VALIDATION_CHECKLIST.md)
- [This handoff note](./SESSION_HANDOFF.md)

### Runtime scripts (key)

- [FireSimulationController.cs](./Scripts/FireSimulationController.cs)
- [FireSimulationRuntimeState.cs](./Scripts/FireSimulationRuntimeState.cs)
- [FireTuningRuntimeState.cs](./Scripts/FireTuningRuntimeState.cs)
- [FireSuppressionProfileApplier.cs](./Scripts/FireSuppressionProfileApplier.cs)
- [FireDamageStateController.cs](./Scripts/FireDamageStateController.cs)
- [FireDamageEffectApplier.cs](./Scripts/FireDamageEffectApplier.cs)
- [FireRecoveryController.cs](./Scripts/FireRecoveryController.cs)
- [FireRecoveryEffectApplier.cs](./Scripts/FireRecoveryEffectApplier.cs)
- [PrometheusFireDebugFragment.cs](./Scripts/PrometheusFireDebugFragment.cs)

### Deploy/build scripts

- [build.sh](../../../scripts/build.sh)
- [README quick commands](../../../README.md)
- [README development loop](../../../README.md#development-loop-code---verify---logs)
- [Project memory](../../../docs/PROJECT_MEMORY.md)

### Log location

- `~/Library/Logs/Mechanistry/Timberborn/Player.log`
- `~/Library/Logs/Mechanistry/Timberborn/Fire.log`
- Filter token: `[Prometheus/Fire]`

## Resume checklist

1. Run `bash scripts/build.sh --launch`.
2. Verify Explosives Factory is fully built.
3. Press `Ignite` once.
4. Inspect `Fire.log` first, then `Player.log` if needed.
5. Confirm whether `explosion_ignite_applied` appears; if not, proceed with request-lifecycle instrumentation step.
