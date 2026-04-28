# Prometheus Handoff

## Current Focus

Last updated: 2026-04-28 17:55 EDT

Prometheus is in Phase 3: intentional fire and ash harvest. Controlled burns should remain an emergent strategy from preparation, spacing, water, moisture, barriers, and firebreaks, not a new controlled-burn subsystem.

The immediate live QA target is now Phase 3 aftermath parity:

- Tree stump/remnant aftermath is live-verified for one forced Pine burn.
- Crop aftermath is live-verified for one forced Carrot burn.
- Building aftermath is live-verified for one forced Bakery construction-site burn.
- Next: close out TKT-009 verification after a final test/build pass, then continue with the remaining active Phase 3 tickets.
- The current reliable fixture is the `Prometheus QA` autosave loaded through the normal Load Game menu or `Continue` after a fresh `bash scripts/build.sh --launch`; `2026-04-28 17h18m, Day 6-17.autosave` loaded in `11886ms` after moving QA commands into a world-ready singleton.

The detailed pre-compaction evidence log is archived at [ARCHIVE/handoff-2026-04-28-phase3-fire-qa.md](ARCHIVE/handoff-2026-04-28-phase3-fire-qa.md).

## Current Ticket State

- `verify`: [tickets/verify/TKT-003-ignite-selected-target.md](tickets/verify/TKT-003-ignite-selected-target.md), [tickets/verify/TKT-007-tree-dead-stump-lifecycle.md](tickets/verify/TKT-007-tree-dead-stump-lifecycle.md), [tickets/verify/TKT-008-unified-burned-ground-ash-recovery.md](tickets/verify/TKT-008-unified-burned-ground-ash-recovery.md), [tickets/verify/TKT-009-building-crop-aftermath-parity.md](tickets/verify/TKT-009-building-crop-aftermath-parity.md), [tickets/verify/TKT-011-fire-suppression-first-slice.md](tickets/verify/TKT-011-fire-suppression-first-slice.md)
- `in-progress`: [tickets/in-progress/TKT-004-fertile-ash-from-burned-crops.md](tickets/in-progress/TKT-004-fertile-ash-from-burned-crops.md), [tickets/in-progress/TKT-005-containment-validation-matrix.md](tickets/in-progress/TKT-005-containment-validation-matrix.md), [tickets/in-progress/TKT-006-runtime-visual-readability.md](tickets/in-progress/TKT-006-runtime-visual-readability.md)
- `blocked`: [tickets/blocked/TKT-010-agriculture-fertile-ash-application.md](tickets/blocked/TKT-010-agriculture-fertile-ash-application.md)
- `deferred`: [tickets/deferred/TKT-001-farmhouse-fertile-ash-application.md](tickets/deferred/TKT-001-farmhouse-fertile-ash-application.md)

Use [tickets/README.md](tickets/README.md) as the board source of truth and move ticket files with `git mv` or `scripts/tickets.sh`.

## Latest Verified State

| Area | Status | Evidence |
| --- | --- | --- |
| Startup and current autosave loading | Live Pass | `bash scripts/build.sh --launch`, `Continue`, `Player.log`, `Fire.log`; `Prometheus QA - 2026-04-28 17h18m, Day 6-17.autosave` loaded in `11886ms` after the QA command singleton move. |
| Post-load safety gate | Live Pass | Runtime `WorldReady` now waits four seconds after Timberborn `PostLoad()` before entity systems touch component caches, model state, yielders, visuals, recovery, grid ticking, field-amendment ticking, or QA command polling. Logs showed `post_load_settling` before component/Yielder probes. |
| Lifecycle and scene-readiness centralization | Test Pass, live load pass | Singleton lifecycle hooks now register through `RegisterSingletonLifecycleHooks()`, world-updated Prometheus singletons must go through the world-ready helper path, and loaded-scene object scans share `PrometheusLoadedSceneObjectLookup`. `bash scripts/test.sh` passes with `120` tests. |
| Soil-moisture world sampling | Test Pass, deploy pass | Air and out-of-terrain fire-grid cells no longer call Timberborn `ISoilMoistureService`, avoiding the known `environment_adapter_sample_failed input=soil_moisture detail="IndexOutOfRangeException"` warning. `bash scripts/test.sh` passes with `121` tests; `bash scripts/build.sh --launch` compiled and deployed, but Steam did not start Timberborn within the launch detection window for this pass. |
| QA command bridge | Live Pass | `PrometheusQaCommandSingleton` waits for `WorldReady`, ignores preview/template targets, and supports `ignite-first-tree`, `ignite-first-crop`, and `ignite-first-building`. Tree, crop, and building commands passed live. |
| Stump resurrection regression | Live Pass | The forced Pine reached `stage=stumpandcharred` and continued burning through stump fuel without alive-tree resurrection telemetry or visuals observed. |
| Tree ash as remnant harvest | Live Pass | The forced Pine logged `fertile_ash_tree_remnant_yield_applied`, then `burned_ground_ash_deposit_created`, `burned_ground_ash_deposit_marker_created`, and `fertile_ash_spawn_queued reason=charred_tree_remnant_harvest`; no `fertile_ash_recovered_good_stack_queued` rows appeared. Screenshot: `/tmp/prometheus-tree-ash-remnant-live.png`. |
| Crop ash aftermath | Live Pass | `ignite-first-crop` burned `Carrot(Clone)` through burnout and logged `sourceKind=charredcrop`, `damageCategory=crop`, `cropContext=burned_crop`, burned-ground marker creation, `fertile_ash_recovered_good_stack_queued`, and `fertile_ash_spawn_queued reason=charred_crop`. |
| Building ash aftermath | Live Pass | A newly placed unfinished `Bakery.Folktails(Clone)` construction site was a valid fire-profiled building target. `ignite-first-building` logged `qa_command_result result=success category=building`, `building_operations_disabled`, `burned_ground_ash_deposit_created sourceKind=charredbuilding damageCategory=building`, marker creation, `fertile_ash_recovered_good_stack_queued amount=4`, and `fertile_ash_spawn_queued reason=charred_building`. |
| First suppression slice | Live Pass | `Suppress Selected` dampened heat, ember pressure, smoke, ignition progress, and fuel consumption; logs captured queue/apply/expire telemetry. |
| Farmhouse ash use | Blocked | Compile-clean scaffold exists, but the workplace decorator is intentionally not registered until a fresh fixture proves live worker behavior. |

Current plain test count: `121` passing tests.

## Next Exact Action

TKT-009 is ready for orchestrator review in `verify`. The next useful live QA target is TKT-004 visible crop ash pickup proof: crop ash telemetry is clean, but a selectable recovered-good stack still has not been visually captured before pickup or overlap with crop visuals.

For future building aftermath QA, open the Food menu, select Bakery, place a construction site near a path, exit build mode, select the site, unpause, and run `ignite-first-building` through `~/Library/Application Support/Timberborn/PrometheusQA/command.txt`. The unfinished construction site is enough; using Timberborn's `Complete` button is optional for this proof. Keep tree-remnant ash separate: trees should remain remnant-harvest, while crops/buildings still use the recovered-good stack path unless a future ticket changes that design.

## Known Problem Saves

| Save | Status | Notes |
| --- | --- | --- |
| `Prometheus QA / beginning cli-safe` | Suspect | Hung after Timberborn save/mod mismatch warning in the current build, while `Continue` loaded the current autosave normally. |
| `Prometheus QA / beginning_safe` | Suspect | Previously reached Prometheus post-load while Timberborn remained visually stuck on `LOADING`. |
| Latest current autosave via `Continue` | Current known-good | `Prometheus QA - 2026-04-28 17h18m, Day 6-17.autosave` loaded in `11886ms` after `bash scripts/build.sh --launch`. |
| `Prometheus QA - 2026-04-28 15h18m, Day 6-11.autosave` | Current known-good | Loaded from the normal Load Game menu in `11926ms` after the lifecycle, scene-readiness, and QA command singleton guardrail changes. |

## Durable Rules

- Use `bash scripts/build.sh --launch` for in-game QA loops.
- Use `bash scripts/build.sh --qa` when the next step benefits from tests, deployment, cleared logs, and a fresh Timberborn launch before Computer Use navigation.
- Use normal menu loading or `Continue` for live QA; CLI `-settlementName` / `-saveName` remains unsafe on this mod stack.
- Treat a load as hung if it does not reach `Load time:` within 15 seconds after clicking through the final load confirmation.
- Register Timberborn lifecycle hooks through `RegisterSingletonLifecycleHooks()` and the typed helper methods in `PrometheusConfigurator`; do not add one-off `IUpdatableSingleton`, `ILoadableSingleton`, `IPostLoadableSingleton`, or `IUnloadableSingleton` bindings inline.
- Prometheus `IUpdatableSingleton` implementations must implement `IPrometheusWorldReadyUpdatableSingleton`, inject `PrometheusWorldLoadState`, and no-op until `WorldReady`.
- Runtime or debug code that scans scene objects should use `PrometheusLoadedSceneObjectLookup` or `TimberbornComponentCacheLookup`, so loaded-scene filtering is shared and future load fixes land in one place.
- Timberborn soil moisture should only be sampled for known terrain cells. Fire-grid air cells above the terrain top surface and cells without terrain context should treat soil moisture as unavailable instead of querying `ISoilMoistureService`.
- Do not register the farmhouse Fertile Ash workplace decorator until TKT-010 has live fixture evidence.
- Keep internal docs in root `docs/`, not under `Assets/Mods/Prometheus`, because the asset tree is deployed into the mod payload.
- Check exact current UI labels and telemetry event names in source rather than copying them into handoff.

## References

| Need | Source |
| --- | --- |
| Build/deploy details | `bash scripts/build.sh --help` |
| Validation gates | [TEST_PLAN.md](TEST_PLAN.md) |
| Durable design | [DESIGN.md](DESIGN.md) |
| Repo map | [INDEX.md](INDEX.md) |
| Ticket board | [tickets/README.md](tickets/README.md) |
| Multi-agent runbook | [ORCHESTRATION.md](ORCHESTRATION.md) |
