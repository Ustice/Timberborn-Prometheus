# Prometheus Session Handoff

## Last updated

2026-02-22

## Project brief

Prometheus is a Timberborn mod that adds a systemic fire gameplay loop across ignition, spread, suppression, and recovery.
It introduces faction-aware firefighting behavior (Folktails logistics-style response, Ironteeth industrial response), fire-related runtime simulation controllers, debug instrumentation, and tuning profiles (`Low`/`Standard`/`High`).
Current focus is validating and tuning the live simulation to be dangerous but manageable, with clear telemetry for debugging and balancing.

## Why we are doing this

Current sprint focus is **runtime validation and tuning** for fire simulation/dispatch (Phase 2 completion + Phase 5 validation).
We added debug controls and logs to accelerate testing, then fixed runtime stability/injection issues that blocked playtest loops.

## What we are actively working on

1. Validate single-front behavior using deterministic debug ignition.
2. Gather log-backed telemetry (`[Prometheus/Fire]`) to evaluate spread/quench/response-state transitions over time.
3. Use captured evidence to tune suppression vs spread and damage progression.

## Confirmed results so far

### Infrastructure / workflow

- Deploy flow now supports safe restart and stale-build protection through `--launch` (safe stop + stale DLL guard with no bypass override).
- Verified build+deploy hash checks in repeated runs.
- Re-ran deploy tests on 2026-02-22: `32 passed, 0 failed`.
- Confirmed stale-build guard is actively blocking deploy when Unity DLL lags source script timestamps.
- Added deploy polling support to reduce Unity compile race conditions (automatically enabled by `--launch`).

### Debug panel / usability

- Debug panel is now scrollable and copy-capable.
- Copy feedback/status added.
- Details section is collapsible by default.
- Added `Ignite` debug button to queue forced ignition requests.

### Runtime stability fixes

- Fixed multiple null-reference crashes caused by missing DI attributes.
- Added first-tick snapshot behavior (suppression/simulation no longer strictly gated on waiting 1 second after first attach).

### Logging / observability

- Added structured fire event logs in `Player.log` with tag:
  - `[Prometheus/Fire]`
- Confirmed event stream includes:
  - `debug_ignite_request`
  - `debug_ignite_applied`
  - `ignited`
  - `burning_tick`
  - `response_state`
- Latest sampled `Player.log` currently shows sparse recent telemetry only (mainly `response_state` lines), so a fresh controlled capture is still required.

### Behavioral signal from captured runs

- Fire loop is alive and producing telemetry.
- In tested Standard/Bakery scenario, spread can exceed quench and trend to `Overwhelmed`.
- This is now a **tuning/balance** issue, not a wiring/visibility/crash issue.
- Captured clean single-ignite telemetry sequence in latest run window (`debug_ignite_request` -> `debug_ignite_applied` -> `ignited` -> `burning_tick`) with `spread=0.097`, `quench=0.075`, and rising intensity (`0.371 -> 0.478`).

## Open issues / hypotheses

1. **Balance harshness in Standard**
   - Observed spread pressure (~0.097) > quenching (~0.075), causing intensity climb and Overwhelmed transitions.
2. **Damage categorization/timing may still be too aggressive in some cases**
   - Continue validating `FireDamageCategory` detection and severity ramp under repeated/manual ignites.
3. **Repeated ignite presses can distort comparison baselines**
   - Prefer one ignite event per measurement window when running A/B tuning.
4. **Compile timing ambiguity can masquerade as stale-build failures**
   - Use `--launch` mode to wait for Unity outputs before stale check (no stale bypass option is available).

## Next steps (priority order)

1. Ensure Unity compile completes in this repo, then run deploy with polling:
   - `bash scripts/deploy_prometheus.sh --launch`
2. Execute post-tuning comparison pass (same scenario, single `Ignite` press).
3. Capture panel snapshot + matching `[Prometheus/Fire]` lines for the tuned window.
4. Compare baseline vs tuned metrics (`spread`, `quench`, intensity slope, response state evolution).
5. If behavior improves but remains too harsh, apply one additional micro-adjustment only.
6. Record accepted defaults and rationale in design docs.

## How to quickly resume

1. From repository root, run:
   - `bash scripts/deploy_prometheus.sh --launch`
2. In game:
   - Select profiled Bakery/Coffee Brewery.
   - Unpause.
   - Press `Ignite` once.
3. Collect:
   - panel snapshot,
   - `[Prometheus/Fire]` lines from `Player.log`.
4. Compare against current baseline reference:
   - first burning ticks showed `spread=0.097`, `quench=0.075`, intensity rising.

## Important files / references

### Core docs

- [Design doc](./DESIGN.md)
- [Test plan](./TEST_PLAN.md)
- [This handoff note](./SESSION_HANDOFF.md)

### Runtime scripts (key)

- [FireSimulationController.cs](./Scripts/FireSimulationController.cs)
- [FireSimulationRuntimeState.cs](./Scripts/FireSimulationRuntimeState.cs)
- [FireSuppressionProfileApplier.cs](./Scripts/FireSuppressionProfileApplier.cs)
- [FireDamageStateController.cs](./Scripts/FireDamageStateController.cs)
- [FireDamageEffectApplier.cs](./Scripts/FireDamageEffectApplier.cs)
- [FireRecoveryController.cs](./Scripts/FireRecoveryController.cs)
- [FireRecoveryEffectApplier.cs](./Scripts/FireRecoveryEffectApplier.cs)
- [PrometheusFireDebugFragment.cs](./Scripts/PrometheusFireDebugFragment.cs)

### Deploy/build scripts

- [deploy_prometheus.sh](../../../scripts/deploy_prometheus.sh)
- [test_deploy_prometheus.sh](../../../scripts/test_deploy_prometheus.sh)
- [README quick commands](../../../README.md)
- [Project memory](../../../docs/PROJECT_MEMORY.md)

### Log location

- `~/Library/Logs/Mechanistry/Timberborn/Player.log`
- Filter token: `[Prometheus/Fire]`
