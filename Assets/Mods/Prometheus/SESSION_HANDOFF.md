# Prometheus Session Handoff

## Last updated

2026-04-25

## Why we are doing this

Current objective is to move from Phase 1 core fire simulation into Phase 2 ember-field spread and fire presentation.
Recent work closed the early fire-spread proof of concept and prioritized three outcomes:

1. Correctness fixes for burn lifecycle edge cases (dead buildings, placement previews, destroy cleanup, reset-to-healthy recovery).
2. Strong in-game observability (scrollable fire log, filters, log counts, and entity jump helpers).
3. Compatibility fixes against the current installed Timberborn assemblies.

## What we are actively working on

1. Begin Phase 2 ember-field cellular spread, moisture dampening, and visual-state validation.
2. Keep worker/building exposure inside burning buildings as a later compatibility check after core spread is coherent.
3. Keep Unity EditMode regression tests aligned with real gameplay/system decisions.
4. Continue explosion request/apply lifecycle investigation with improved panel/log tooling if gaps reappear.
5. Preserve the simplified Phase 3 renewal direction: players contain burns with cleared terrain/firebreaks, then foresters collect Fertile Ash from burned vegetation and selected ruins.
6. Add in-world fire-risk presentation: embers, smoke, fire, steam, and charred materials should map directly to runtime fire state. Ember fields can come from active fires, fireworks, selected high-intensity fire-using buildings such as smelters, and explosive/unstable-core events.
7. Preserve the Phase 2 scope reduction: no fire-brigade/relay minigame, no direct beaver control, and no standalone bucket/foam/gear goods loops unless they prove necessary.
8. QA the first visual-effect adapter pass in game and tune particle/material intensity from observed readability.

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
- The global panel is behind the Moddable Tool Groups bottom-bar group `Prometheus`, whose submenu entries open the same TimberUi panel instance at `Actions`, `Visuals`, `Selection`, or `Log`.
- `TimberUi` and `ModdableToolGroups` are now required dependencies for the debug UI migration; local builds resolve them from the installed Steam Workshop payloads.
- The global panel supports:
  - selected-entity runtime details,
  - selected-entity copy output,
  - selected-entity debug ignite request,
  - runtime snapshot counts + **delta since selection** in the Selection view,
  - in-game fire log with counts merged into the Log filters,
  - auto-scroll toggle,
  - severity filters (`All`/`Events`/`Warnings`/`Errors`),
  - log severity counts,
  - search box.
- Prometheus debug panel admin buttons:
  - `Stop All Fires`,
  - `Reset Fire Sim`,
  - `Clear Beavers`.
- `Stop All Fires` resets live fire controllers, clears runtime stores, and suppresses ambient re-ignition for 60 real seconds while preserving manual debug ignition.
- `Reset Fire Sim` clears fire/damage/recovery state and restores loaded entities to healthy/functioning state for fast QA loops.
- `burning_tick` telemetry is throttled by real time, not game simulation time, so high-speed gameplay does not flood the panel as aggressively.
- Current debug-panel UI pass replaces in-panel tabs with Moddable Tool Groups bottom-bar submenu navigation; the behavior is unchanged and still intentionally manual-QA'd.
- Telemetry event names now live in `FireTelemetryEvents`, an iterable constant registry intended for future filters/docs/log tooling.
- Earlier faction quenching and dispatch scoring/lock decisions live in `FireSimulationRules`; these remain useful patterns, but current Phase 2 priority has shifted to ember-field spread before responder behavior.
- Old bucket-kit, firefighting-foam, fire-control-gear, fireworks-crate, and festival-risk scaffolding was pruned from active content. Ash fertilizer content was renamed to Fertile Ash.

### Build/deploy verification

- Repeated `bash scripts/build.sh` runs completed successfully after each incremental change.
- Plain C# regression harness added via `bash scripts/test.sh`; first targets are runtime stores, ignition request behavior, entity registry targeting, lifecycle thresholds, response-state thresholds, terminal dead snapshot behavior, reset clearing behavior, workplace component classification, and beaver exposure pressure rules.
- Unity EditMode testing was explored and can launch after license activation, but loading the full Timberborn assembly graph in this standalone repo pulled in fragile plugin/package dependencies. Future test work should prefer dependency-light rule/runtime classes first, with Unity tests reserved for lifecycle behavior that truly needs Unity.
- Debug panel UI is intentionally excluded from automated tests for now and remains manual QA because the workflow is still evolving.
- First Phase 2 worker/beaver exposure slice landed: assigned workers in burning workplaces receive indoor beaver need exposure through the same NeedManager path as proximity exposure, while proximity and indoor exposure magnitudes are computed by `FireBeaverExposureRules` and covered by plain C# tests.
- First fire presentation slice landed: `FireVisualEffectRules` computes tunable smoke/fire/steam/char intensities from fire, water, and damage snapshots, while `FireVisualEffectApplier` applies Unity particles plus char tint to loaded fire-profiled entities and clears them through `Reset Fire Sim`.
- Local object fire progression intentionally does not emit sparks: it should read as smoke to fire, then back to smoke/ash/char. Sparks belong to the separate ember-field spread visualization.
- Native-particle replacement pass landed: `FireVisualEffectApplier` now loads Timberborn Resource particle prefabs, clones the best native ember/smoke/fire/steam source by name/material scoring, controls each cloned effect group from Prometheus intensity, and logs `native_visual_effect_resolved` or `native_visual_effect_unavailable` once per channel.
- Latest launch resolved native sources as `Sparks_Trail` for embers, `SmelterSmoke` for smoke, `CampfireFire` for fire, and `SteamEngineSmoke` for steam.
- `Prometheus` -> `Visuals` now fully replaces the old tuning-slider panel with an effect authoring inspector for `Smoke`, `Ash`, `Steam`, `Fire`, `Sparks`, and `Char`.
- Particle authoring includes enabled, native source, intensity, emission, local position X/Y/Z, size, lifetime, speed, alpha, RGB color, spread/shape, and size-over-lifetime presets (`Constant`, `Grow`, `Shrink`, `Swell`, `Pop`). Advanced mode exposes velocity, gravity, noise, rotation, shape mode, and sorting/order.
- Native source selection shows recommended Timberborn particle prefabs first and can expand into a searchable all-native list; changing the source reclones the preview while keeping tuning values.
- Selected-entity preview can apply the selected effect or full preset to any selected loaded entity when supported. `Clear Preview` and `Reset Fire Sim` remove temporary preview particles/material overrides without changing fire simulation, damage, recovery, profiles, or saved state.
- Visuals panel target refresh is now gated to actual target identity changes; rebuilding it on every selected-entity runtime update caused visible flicker and made buttons hard to click.
- Visual preview is now live after `Apply Effect` or `Apply Preset`: later control/source/preset changes reapply the armed preview automatically without extra log spam. `Clear Preview` disarms live preview.
- Promoted current authoring defaults from the latest tuning pass: Smoke uses `FoodFactorySmoke` with longer lifetime and zero spread; Ash uses `BadwaterRigSmoke`; Steam uses `CoffeeBrewerySmoke` with +0.35 Y position and +0.7 Y velocity; Fire uses `CampfireFire` with offset `(0.25, 0, 0.15)`, 1.2 lifetime, zero spread, and -0.15 gravity; Sparks uses `Sparks_Trail` with reduced intensity/emission, 1.4 spread, -0.25 gravity, and 0.4 noise. These are tool defaults/reset values only until intentionally mapped into simulation-driven fire visuals.
- JSON copy/log export writes `version`, `selectedEffect`, `advancedEnabled`, `target`, `effects`, and `char`; `target` includes selected id, raw name, best-effort kind, and supported flag.
- Char authoring now includes cut amount, noise scale/contrast, edge width, edge depth, active glow, ash-edge brightness, black interior strength, seed, tint strength, darkening, and tint color. The current preview path deliberately uses safe material-property overrides; true shader clipping remains gated until Timberborn shader support is inspected.
- Screenshot QA showed the legacy `DEAD` text markers rendered as huge magenta blocks; they now default off and can be re-enabled/tuned from the debug panel.
- Previous panel pass converted the global debug panel from a tall stack into compact tabs; the current Moddable Tool Groups migration supersedes that navigation with bottom-bar submenu entries.
- Native-style pass now lets TimberUi own visible panel controls. The `IEntityPanelFragment` remains only as a selection forwarding hook so Timberborn selection changes can update the global panel.
- Latest blank-slate TimberUi pass removes the old hidden entity-panel UI, deletes the custom palette file, moves log counts into the Log filters, and removes visible debug-panel `style.*` overrides so TimberUi components/layout provide the baseline presentation.
- Follow-up screenshot pass gives the bottom-left panel a native fixed width/offset, reinitializes dynamically switched submenu views through TimberUi, and switches command/filter/selection strips to `AddHorizontalContainer()` rows so button backgrounds are not clipped or squeezed.
- Latest selection-panel screenshot cleanup switches `Copy` and `Ignite` from entity-fragment buttons to TimberUi `GameButton` controls because entity-fragment buttons rendered as plain text in the detached bottom-bar panel context.
- Detached custom-panel pass now follows the source `QuestPanel`/`TodoListPanel` examples: `NineSliceVisualElement` root, TimberUi `square-large--green` class, padding, width, and direct child controls. This replaces the entity-fragment root for the bottom-bar panel because it is not hosted inside Timberborn's entity-panel layout.
- Latest button cleanup follows source examples that add padding through TimberUi helpers by routing debug-panel command/filter/view buttons through `AddGameButtonPadded` with modest horizontal/vertical inset.
- Debug-panel hitbox cleanup constrains the detached panel root and log scroll view so the visible panel controls remain clickable without blocking Timberborn object selection across the rest of the screen.
- Debug submenu tools now inject `ToolService`, open the requested panel tab, and immediately call `SwitchToDefaultTool()` so Timberborn building selection remains active after using the Prometheus bottom-bar submenu.
- Do not raise the detached debug panel `UILayout.AddBottomLeft` priority aggressively or call `BringToFront()` as a selection-overlay workaround; testing on 2026-04-25 caused startup/save-load spinning. Restoring the normal priority allowed the usual Prometheus test save to load again.
- Latest visual-effect/default pass increased the plain C# suite to 23 tests.
- Latest replacement visual-inspector pass passed `bash scripts/test.sh && bash scripts/build.sh --launch`; fresh startup logs show `Prometheus (v0.2)` loading without Prometheus errors.
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
3. QA and tune first fire-state presentation adapter:
  - no local sparks during object fire progression; sparks belong to the ember-field spread layer,
   - smoke for smoldering/scorched states,
   - fire for active burning,
   - light steam for moisture dampening,
   - native Timberborn particle systems are resolved for the visible channels, or fallback gaps are documented from `Fire.log`,
   - height/Z/size/ember-spread sliders can tune native clone placement and footprint live,
   - charred material tint for terminal burned buildings and vegetation,
   - reset clears active effects and tint.
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

1. Run `bash scripts/test.sh && bash scripts/build.sh --launch`.
2. Open `Prometheus` -> `Visuals`; confirm Timberborn object selection still works while the panel is open.
3. Select a Bakery, platform, tree, and berry bush; confirm the Visuals target summary and JSON target kind are readable.
4. For `Smoke`, `Ash`, `Steam`, `Fire`, and `Sparks`, try recommended sources plus one searched native source, then `Apply Effect`.
5. Apply the full preset, then `Clear Preview`; confirm particles and material overrides are removed.
6. Log JSON and confirm `Fire.log` contains `visual_tuning_json` with target context.
7. Trigger `Reset Fire Sim` and confirm all active visual previews clear without changing selection behavior.
