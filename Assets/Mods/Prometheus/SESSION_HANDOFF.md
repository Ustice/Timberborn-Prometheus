# Prometheus Session Handoff

## Last updated

2026-04-24

## Why we are doing this

Current objective is to move from Phase 1 core fire simulation into Phase 2 ember-field spread and fire presentation.
Recent work closed the early fire-spread proof of concept and prioritized three outcomes:

1. Correctness fixes for burn lifecycle edge cases (dead buildings, placement previews, destroy cleanup, reset-to-healthy recovery).
2. Strong in-game observability (scrollable fire log, filters, colored severity rows, and entity jump helpers).
3. Compatibility fixes against the current installed Timberborn assemblies.

## What we are actively working on

1. Begin Phase 2 ember-field cellular spread, moisture dampening, and visual-state validation.
2. Keep worker/building exposure inside burning buildings as a later compatibility check after core spread is coherent.
3. Keep Unity EditMode regression tests aligned with real gameplay/system decisions.
4. Continue explosion request/apply lifecycle investigation with improved panel/log tooling if gaps reappear.
5. Preserve the simplified Phase 3 renewal direction: players contain burns with cleared terrain/firebreaks, then foresters collect Fertile Ash from burned vegetation and selected ruins.
6. Add in-world fire-risk presentation: embers, smoke, fire, steam, and charred materials should map directly to runtime fire state. Ember fields can come from active fires, fireworks, selected high-intensity fire-using buildings such as smelters, and explosive/unstable-core events.
7. Preserve the Phase 2 scope reduction: no fire-brigade/relay minigame, no direct beaver control, and no standalone bucket/foam/gear goods loops unless they prove necessary.

## Confirmed results so far

### Behavior fixes

- Fully burned (`Dead`) buildings now suppress workplace support and production-related operational behaviors.
- Dead/ash buildings are terminal for active fire and cannot continue burning.
- Placement previews/ghost entities are excluded from simulation ignition path.
- Fire runtime snapshots are purged on entity destroy via lifecycle cleanup component.
- `Reset Fire Sim` restores loaded fire entities to healthy/functioning Prometheus state and clears stale simulation, damage, recovery, registry, and pending ignition snapshots.
- Timberborn 1.0.13 compatibility pass removed stale `WorkplaceLightingSpec` from the Jam Stove blueprint; the save now loads again.
- Beaver fire effects now bind the current `NeedManager.GetNeed(string)` + `Need.AddPoints(float)` API.
- Beaver fire effects now use a shared scene-wide need-manager cache and apply nearby-fire `HeatStress`, moderate thirst pressure, and only slow high-pressure injury.
- Beaver fire effects use distance falloff; beavers near the edge of the radius receive less pressure.
- Nearby fire no longer applies vanilla `Injury`; the debug cleanup clears `HeatStress`/legacy injury debt from loaded beavers.
- Workplace fire slowdown now binds the current `Worker` + `BonusManager(WorkingSpeed)` API instead of the removed `WorkplaceBonuses.WorkingSpeedMultiplier` API.
- Worker slowdown validation initially showed no visible slowdown on a burning/dead Bakery because damage category detection returned `Unknown` for current cached Timberborn building components. Category detection now recognizes cached `Workplace`/building components, workplace-bearing entities are treated as buildings for dead-state shutdown, and worker slowdown sets `Worker.WorkingSpeedMultiplier` directly while preserving/restoring original values.
- Workplace slowdown telemetry now emits `workplace_speed_penalty_state` with assigned worker count, applied worker count, and penalty delta.
- Follow-up Bakery validation confirmed slowdown/shutdown path: burning Bakery logged `assignedWorkers=1 appliedWorkers=1`, penalty ramped to `-1.000`, then `workplace_support_suppressed` and `building_operations_suppressed` fired when the Bakery reached `Dead`.
- `Stop All Fires` after the Bakery was dead extinguished live fire and returned response state to `Stabilized`; full revive/healthy recovery is handled by `Reset Fire Sim`.
- `workplace_speed_penalty_state` logging is now thresholded to reduce per-tick spam while preserving key penalty changes and restore/dead milestones.
- `Clear Burned` crash was reproduced: raw `UnityEngine.Object.Destroy(gameObject)` removed live Timberborn entities while they remained in tick buckets, causing `Exception thrown while ticking entity ... '(destroyed)'`.
- The unsafe destroy path has been removed. The former burned-building demolition helper was replaced by `Reset Fire Sim`, which is the preferred QA clean-slate tool.
- Spread ignition was observed live with `spread_ignite_applied`.
- The latest QA screenshot cycle confirmed:
  - initial load showed no injured/overheated colony alerts,
  - `Clear Beavers` showed visible `Cleared 24 beavers` feedback,
  - unpausing did not immediately reintroduce visible injured/overheated alerts.

### Debug UX / observability

- The Prometheus debug UI is consolidated into one bottom-left panel, raised above the Timberborn bottom bar.
- The entity panel fragment is hidden and only forwards selection state into the global panel.
- The global panel supports:
  - selected-entity runtime details,
  - selected-entity copy output,
  - selected-entity debug ignite request,
  - runtime snapshot counts + **delta since selection**,
  - in-game fire log (scrollable, minimizable),
  - auto-scroll toggle,
  - severity filters (`All`/`Events`/`Warnings`/`Errors`),
  - colored severity labels,
  - search box.
- Prometheus debug panel admin buttons:
  - `Stop All Fires`,
  - `Reset Fire Sim`,
  - `Clear Beavers`.
- `Stop All Fires` resets live fire controllers, clears runtime stores, and suppresses ambient re-ignition for 60 real seconds while preserving manual debug ignition.
- `Reset Fire Sim` clears fire/damage/recovery state and restores loaded entities to healthy/functioning state for fast QA loops.
- `burning_tick` telemetry is throttled by real time, not game simulation time, so high-speed gameplay does not flood the panel as aggressively.
- Current debug-panel UI pass reorganizes the global panel into Timberborn-style status, command, filter, selection, and log sections; the behavior is unchanged and still intentionally manual-QA'd.
- Telemetry event names now live in `FireTelemetryEvents`, an iterable constant registry intended for future filters/docs/log tooling.
- Earlier faction quenching and dispatch scoring/lock decisions live in `FireSimulationRules`; these remain useful patterns, but current Phase 2 priority has shifted to ember-field spread before responder behavior.
- Old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding was pruned from active content. Ash fertilizer content was renamed to Fertile Ash.

### Build/deploy verification

- Repeated `bash scripts/build.sh` runs completed successfully after each incremental change.
- Plain C# regression harness added via `bash scripts/test.sh`; first targets are runtime stores, ignition request behavior, entity registry targeting, lifecycle thresholds, response-state thresholds, terminal dead snapshot behavior, reset clearing behavior, workplace component classification, and beaver exposure pressure rules.
- Unity EditMode testing was explored and can launch after license activation, but loading the full Timberborn assembly graph in this standalone repo pulled in fragile plugin/package dependencies. Future test work should prefer dependency-light rule/runtime classes first, with Unity tests reserved for lifecycle behavior that truly needs Unity.
- Debug panel UI is intentionally excluded from automated tests for now and remains manual QA because the workflow is still evolving.
- First Phase 2 worker/beaver exposure slice landed: assigned workers in burning workplaces receive indoor beaver need exposure through the same NeedManager path as proximity exposure, while proximity and indoor exposure magnitudes are computed by `FireBeaverExposureRules` and covered by plain C# tests.
- Latest debug-panel redesign build passed `bash scripts/test.sh` and `bash scripts/build.sh`; in-game layout still needs visual QA after launch/reload.
- Telemetry registry pass increased the plain C# suite to 17 tests and passed `bash scripts/test.sh`; `bash scripts/build.sh` also passed.
- Response-rule extraction increased the plain C# suite to 19 tests and passed both `bash scripts/test.sh` and `bash scripts/build.sh`.
- Latest resume build initially hit a missing VS Tools Unity analyzer path after VS Code updated `visualstudiotoolsforunity.vstuc` from `1.2.1` to `1.2.2`; adding a local compatibility symlink restored `dotnet build`.
- Latest `bash scripts/build.sh --launch` completed successfully, cleared fresh logs, deployed the symlinked payload, and launched Timberborn.
- Fresh startup logs confirmed `Prometheus (v0.2)` load, the Prometheus test autosave opened, and no exception/error lines were present in the scanned startup window.
- Fresh live logs confirmed current Timberborn API bindings:
  - `workplace_speed_api_resolved` via `Worker.BonusManager(WorkingSpeed)`,
  - `beaver_effect_api_resolved` via `NeedManager.GetNeed(string) + Need.AddPoints(float)`,
  - `beaver_effect_need_manager_scan` found 24 loaded need managers.
- Fresh explosion validation observed one complete request/apply chain:
  - `debug_ignite_applied` on Explosives Factory `-221754`,
  - `explosion_ignition_request_queued` for target `-236742`,
  - `explosion_ignition_request_consumed`,
  - `explosion_ignite_applied` on target `-236742`.
- User screenshot confirmed the global panel selection/copy/ignite flow works, but the `View` button was missing because the consolidated panel rendered logs as a multiline text field; this was fixed by switching the global log area to per-entry rows with `View` buttons and by refreshing capped 250-entry logs via content signature instead of entry count only.
- Follow-up screenshot confirmed row rendering loaded but the right-side `View` button was clipped by long wrapped log labels; the button is now a fixed-width leading control on each row.
- Follow-up validation showed the visible `View` button did not center the affected building when it used raw `Camera.main` transforms; `View` now calls Timberborn's `EntitySelectionService.SelectAndFocusOn(...)` for the target entity's `FireSimulationController`, with telemetry `debug_view_focus method=selection_service_*`.
- Latest post-fix `bash scripts/build.sh` completed successfully and deployed the refreshed DLL; a game restart/reload is needed for the running Timberborn process to use that DLL.
- Runtime payload symlinks (`dll`/`pdb`) refreshed successfully each run.
- Unity Hub and Unity `6000.3.6f1` are installed locally.
- Imported Timberborn assets and publicized DLLs were refreshed from the installed Steam game after discovering stale February DLLs in `../timberborn-modding`.
- `bash scripts/build.sh --launch` loaded the Prometheus test save and ran through autosave without a startup crash.

## Open issues / hypotheses

1. **Explosion apply path still needs broader focused re-validation**
   - Latest resume observed a complete `explosion_ignite_applied` chain in one fresh window.
   - Previous sessions still noted missing/rare `explosion_ignite_applied` in some windows despite queued requests.
   - Hypothesis if gaps return: target eligibility/timing/state interaction under spread request consumption.

2. **View button relies on camera + loaded scene availability**
   - If ID is stale/unloaded or camera is unavailable, fallback status message is shown.
   - Parser now supports negative Unity instance IDs.

3. **Worker/building exposure needs Phase 2 live validation**
   - API binding resolves in live logs as `workplace_speed_api_resolved`.
   - Assigned workers in burning workplaces now receive explicit indoor exposure attempts via `workplace_indoor_exposure`.
   - Validate worker production slowdown, worker exposure, and recovery together with Phase 2 responder/workplace behavior.

4. **Beaver fire effects need Phase 2 balance validation**
   - First live pass injured the whole colony, confirming the API worked but the numbers were too hot.
   - Current tuning is much gentler, no longer applies vanilla `Injury`, and uses distance falloff.
   - Continue with Phase 2 checks for nearby exposure and beavers/workers inside burning buildings.

5. **Operational behavior suppression uses type-name matching**
   - Conservative and practical, but additional production component names may surface in future content.

6. **Unity asset import workflow still needs attention**
   - Unity batchmode import is blocked until the editor has an active license.
   - Manual refresh worked for blueprints/UI/DLLs, but should become a documented script or be replaced by the official importer once Unity is activated.

## Next steps (priority order)

1. Implement and validate Phase 2 ember-field spread:
   - active fires emit ember fields,
   - dry fuel ignites when thresholds are met,
   - wet/soaked terrain and firebreaks dampen or block propagation,
   - configured high-risk operating buildings emit ember fields while low-risk warm buildings do not by default.
2. Add/update plain C# tests for any new real Phase 2 system decision before relying on it in tuning.
3. Add fire-state presentation adapters:
   - embers for spread pressure,
   - smoke for smoldering/scorched states,
   - fire for active burning,
   - light steam for moisture dampening,
   - charred shader/material/tint for terminal burned buildings and vegetation.
4. Add Fertile Ash source tagging for charred vegetation and selected ruined buildings, with foresters as the natural vegetation collection path.
5. Validate worker/building exposure on at least 3 production archetypes (e.g., Bakery/JamStove/Explosives Factory):
   - assigned workers slow under heat pressure,
   - beavers/workers inside burning buildings receive appropriate effects,
   - worker speed recovers after fire pressure clears,
   - workers suppressed,
   - production halted,
   - restored correctly when no longer dead.
6. Re-run explosion request/apply trace across multiple one-ignite windows and capture both logs if gaps reappear.
7. Add a repeatable setup refresh script or documentation for importing current game assets and publicized DLLs.
8. Run controlled tuning pass (`Low`/`Standard`/`High`) once Phase 2 ember behavior is coherent.
9. For Phase 3, implement the controlled-burn loop as terrain/fire management rather than managed-burn bookkeeping:
   - prepare containment with cleared terrain, water, and firebreaks,
   - ignite or allow ignition,
   - wait for fuel to burn out,
   - collect Fertile Ash via foresters.

## How to quickly resume

1. Build/deploy:
   - `bash scripts/build.sh --launch`
2. Run automated regression tests:
   - `bash scripts/test.sh`
3. In game:
   - select a fire-profiled building,
   - expand panel `Show fire log`,
   - trigger one ignition event,
   - use filters/search + `View` button for rapid triage.
4. Capture evidence:
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
3. Confirm the reorganized `Status`/`Commands`/`Filters`/`Selection`/`Log` sections are readable and do not cover core Timberborn controls.
4. Trigger one ignite event.
5. Verify: filtered/colored/searchable log rows + `View` button camera jump.
6. Confirm dead-building suppression/restore behavior and capture logs.
