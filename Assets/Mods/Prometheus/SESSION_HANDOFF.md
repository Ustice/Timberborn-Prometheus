# Prometheus Session Handoff

## Last updated

2026-02-22

## Why we are doing this

Current objective is to harden the fire system so gameplay behavior is consistent and debugging is fast.
Recent work prioritized two outcomes:

1. Correctness fixes for burn lifecycle edge cases (dead buildings, placement previews, destroy cleanup).
2. Strong in-game observability (scrollable fire log, filters, colored severity rows, and entity jump helpers).

## What we are actively working on

1. Verify new debug UX end-to-end in live gameplay (search/filter/view flow under active fire).
2. Validate dead-building shutdown behavior across more production building variants.
3. Continue explosion request/apply lifecycle investigation with improved panel/log tooling.

## Confirmed results so far

### Behavior fixes

- Fully burned (`Dead`) buildings now suppress workplace support and production-related operational behaviors.
- Placement previews/ghost entities are excluded from simulation ignition path.
- Fire runtime snapshots are purged on entity destroy via lifecycle cleanup component.

### Debug UX / observability

- Entity panel fire debug section supports:
  - collapsible details,
  - copy output,
  - debug ignite request,
  - runtime snapshot counts + **delta since selection**,
  - in-game fire log foldout (scrollable, minimizable),
  - auto-scroll toggle,
  - severity filters (`All`/`Events`/`Warnings`/`Errors`),
  - colored severity labels,
  - search box,
  - per-line **View** button to focus camera on parsed entity ID (`id=`, `sourceId=`, `targetId=`).

### Build/deploy verification

- Repeated `bash scripts/build.sh` runs completed successfully after each incremental change.
- Runtime payload symlinks (`dll`/`pdb`) refreshed successfully each run.

## Open issues / hypotheses

1. **Explosion apply path still needs focused re-validation**
   - Previous sessions noted missing/rare `explosion_ignite_applied` in some windows despite queued requests.
   - Hypothesis: target eligibility/timing/state interaction under spread request consumption.

2. **View button relies on camera + loaded scene availability**
   - If ID is stale/unloaded or camera is unavailable, fallback status message is shown.

3. **Operational behavior suppression uses type-name matching**
   - Conservative and practical, but additional production component names may surface in future content.

## Next steps (priority order)

1. Run one focused gameplay validation pass for the new debug panel workflow:
   - generate events,
   - filter/search in panel,
   - click `View` on multiple rows,
   - confirm camera focus behavior.
2. Validate dead-building behavior on at least 3 production archetypes (e.g., Bakery/JamStove/Explosives Factory):
   - workers suppressed,
   - production halted,
   - restored correctly when no longer dead.
3. Re-run explosion request/apply trace with one-ignite-per-window guidance and capture both logs.
4. If `explosion_ignite_applied` gaps persist, add targeted telemetry for request-consume decision reasons.
5. After lifecycle correctness is stable, start controlled tuning pass (`Low`/`Standard`/`High`).

## How to quickly resume

1. Build/deploy:
   - `bash scripts/build.sh --launch`
2. In game:
   - select a fire-profiled building,
   - expand panel `Show fire log`,
   - trigger one ignition event,
   - use filters/search + `View` button for rapid triage.
3. Capture evidence:
   - panel screenshot(s),
   - `~/Library/Logs/Mechanistry/Timberborn/Fire.log` (preferred),
   - `~/Library/Logs/Mechanistry/Timberborn/Player.log` (full context).

## Important files / references

### Core docs

- [Design doc](./DESIGN.md)
- [Test plan](./TEST_PLAN.md)
- [This handoff note](./SESSION_HANDOFF.md)

### Runtime scripts (key)

- [FireSimulationController.cs](./Scripts/FireSimulationController.cs)
- [FireSimulationRuntimeState.cs](./Scripts/FireSimulationRuntimeState.cs)
- [FireEntityRegistryRuntimeState.cs](./Scripts/FireEntityRegistryRuntimeState.cs)
- [FireWorkplaceEffectApplier.cs](./Scripts/FireWorkplaceEffectApplier.cs)
- [PrometheusFireDebugFragment.cs](./Scripts/PrometheusFireDebugFragment.cs)
- [PrometheusLogger.cs](./Scripts/PrometheusLogger.cs)
- [PrometheusConfigurator.cs](./Scripts/PrometheusConfigurator.cs)

### Deploy/build and status docs

- [build.sh](../../../scripts/build.sh)
- [README](../../../README.md)
- [Project memory](../../../docs/PROJECT_MEMORY.md)

### Log location

- `~/Library/Logs/Mechanistry/Timberborn/Player.log`
- `~/Library/Logs/Mechanistry/Timberborn/Fire.log`
- Filter token: `[Prometheus/Fire]`

## Resume checklist

1. Run `bash scripts/build.sh --launch`.
2. Select a fire-profiled building and open the debug panel.
3. Trigger one ignite event.
4. Verify: filtered/colored/searchable log rows + `View` button camera jump.
5. Confirm dead-building suppression/restore behavior and capture logs.
