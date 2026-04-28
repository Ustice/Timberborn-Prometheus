# Prometheus Test Plan

This is the authoritative runbook for active Prometheus validation.

## Source Of Truth

| Topic | Source |
| --- | --- |
| Build and launch behavior | `bash scripts/build.sh --help` |
| Automated test behavior | `bash scripts/test.sh` and `tests/Prometheus.Tests` |
| Runtime telemetry names | `FireTelemetryEvents` and QA panel telemetry strings in source |
| Current live state | [HANDOFF.md](HANDOFF.md) |
| Durable design gates | [DESIGN.md](DESIGN.md) |

## Preflight

- [ ] Skip runtime verification for documentation-only updates when the diff only changes documentation and does not claim new runtime behavior.
- [ ] Run `bash scripts/test.sh`.
- [ ] Run `bash scripts/build.sh --launch` for in-game QA.
- [ ] Prefer `bash scripts/build.sh --qa` when you want tests, deployment, cleared logs, and a fresh Timberborn launch before Computer Use navigation.
- [ ] Let `scripts/build.sh` acquire the shared build/QA lock before deploy or launch work; do not bypass the script when another worktree is running build or QA.
- [ ] After `bash scripts/build.sh --qa`, release the persistent QA lock with `bash scripts/build.sh --release-qa-lock` once live evidence capture is complete.
- [ ] If Steam or Timberborn is slow after deployment, tune `LAUNCH_DELAY_SECONDS`; the default is 15 seconds.
- [ ] Use normal menu loading for live QA; CLI `-settlementName "<settlement>" -saveName "<save without .timber>"` uses Timberborn's instant scene-load path and currently crashes after Prometheus startup.
- [ ] Treat save loading as hung if it does not reach `Load time:` in `Player.log` within 15 seconds after the final load confirmation; current healthy `Prometheus QA` loads are under 15 seconds.
- [ ] If `world_load_state_changed ready=true stage=post_load` appears but the rendered screen remains on Timberborn's `LOADING` view, treat that as a save-load/display blocker and do not claim live gameplay proof from that run.
- [ ] Confirm Timberborn launches with Prometheus enabled.
- [ ] Confirm fresh logs are available for the measurement window.
- [ ] Keep each repro scoped to one intent when possible.

## Runtime Sanity

- [ ] `Player.log` contains `- Prometheus (v0.2)` or newer.
- [ ] No startup exception for Prometheus blueprint/type registration.
- [ ] Moddable Tool Groups shows `Prometheus` with `Actions`, `Visuals`, `Selection`, `QA`, and `Log`.
- [ ] Opening a Prometheus submenu returns Timberborn to normal selection after the panel opens.
- [ ] Selecting a Prometheus-profiled entity updates the panel.
- [ ] `Copy`, `Ignite`, `Reset Fire State`, `Stop Fires`, `Clear Beavers`, and `Clear Log` are manually QA'd through the current UI.

## In-Game QA Channel

- [ ] Write the next instruction to `~/Library/Application Support/Timberborn/PrometheusQA/instructions.md`.
- [ ] Open `Prometheus` -> `QA`.
- [ ] Confirm the instruction text refreshes in-game without restarting Timberborn.
- [ ] Click `Passed`, `Failed`, or `Blocked`.
- [ ] Confirm `~/Library/Application Support/Timberborn/PrometheusQA/results.md` receives a timestamped entry with the note and instruction text.
- [ ] Confirm `Fire.log` records the QA result event.

Last verified at 1920x1080 on 2026-04-25: `Prometheus` root around `632,1043`, `QA` child around `1024,970`, and `Passed` recorded `event=qa_result_recorded result=passed`.

Use Computer Use screenshots and clicks for coordinate-based in-game checks so the action and visual evidence stay together.

Source of truth: exact UI labels and control construction live in the debug UI source; this checklist defines behavior to verify, not an inventory to keep synchronized.

## Temporary Removal-Pass Regression

- [ ] Ignite one fire-profiled building.
- [ ] Confirm no old entity-neighbor/direct spread behavior occurs.
- [ ] Confirm `Stop Fires` extinguishes active fire.
- [ ] Drive one building to dead/ash.
- [ ] Confirm dead/ash does not keep burning.
- [ ] Click `Reset Fire State`.
- [ ] Confirm the entity is healthy/functioning again and can be re-ignited.

## 3D Grid Foundation Validation

Use this section as the next validation gate once the sparse grid lands.

| Scenario | Expected Result | Evidence |
| --- | --- | --- |
| Debug ignition writes grid state | Selected target creates an active grid fire snapshot | Panel screenshot + `Fire.log` |
| Configured source writes grid state | `FireProfileSpec` heat/ember/smoke source fields create attributed grid pressure without direct nearest-target ignition | Passing plain C# test + `grid_source_injected` live log when an emitting source is loaded |
| Cooling/decay update | Active cell intensity decreases deterministically | Test output + log sample |
| 27-direction neighbor pass | Fire pressure can evaluate adjacent cells in 3D | Passing plain C# test |
| Reset clears grid | `Reset Fire State` clears active grid, preview, damage, recovery, workplace, beaver, visual, and ash state | Panel screenshot + `runtime_reset_registry_completed failures=0` log sample |
| Chunk boundary propagation | Fire pressure can cross chunk boundaries without duplicate/missing cells | Passing plain C# test |

## Fertile Ash Recovery Validation

- [x] Spawn `FertileAsh` only from valid aftermath sources.
- [x] Confirm the visible recovered-good stack appears at valid coordinates.
- [x] Confirm builders can collect the stack.
- [x] Confirm collected `FertileAsh` enters normal storage that accepts the good.
- [x] Confirm field amendments reduce eligible crop `Growable` growth time in dependency-light control-vs-amended rules.
- [x] Confirm trees and bushes are excluded from the field-amendment growth rule.
- [x] Confirm `Reset Fire State` clears stale ash runtime state without deleting unrelated Timberborn entities unsafely.
- [x] Scan `Player.log` and `Fire.log` for Prometheus and recovered-good exceptions.
- [ ] Confirm farmhouse-driven `FertileAsh` consumption applies a field amendment in a live save.
- [ ] Confirm a farmhouse-amended crop grows faster than a nearby control crop in a live save.

## Phase 3 Intentional Burn Validation

- [ ] Run `git diff --check` and `bash scripts/test.sh` for every Phase 3 code, content, script, or behavior ticket.
- [ ] Run `bash scripts/build.sh --qa` for selected-target ignition, crop ash, containment, and runtime visual readability tickets.
- [ ] Release the QA lock with `bash scripts/build.sh --release-qa-lock` after Computer Use evidence capture.
- [ ] Use Computer Use screenshots/clicks as live evidence for selected-target ignition, visible ash stacks, storage, and containment outcomes.
- [ ] Scan `Player.log` and `Fire.log` for Prometheus exceptions during each live QA window.
- [ ] For crop ash, require `fertile_ash_*` telemetry, visible recovered-good stack proof, District Center storage proof, and no Prometheus exceptions.
- [ ] For carrot crop ash, first confirm selected carrots now have a Prometheus fire profile after rebuild/reload.
- [ ] For containment, require one prepared burn that stays bounded and one unprepared/control burn that spreads more aggressively.
- [ ] For visuals, require screenshots or video evidence that smoke, fire, steam, char, and ember feedback make the burn state understandable without reading debug logs.
- [ ] For pine tree visuals, require live evidence that native model children progress through `#Alive`, `#Dying`, `#Dead`, and `#Leftover`; flame stops at dead, smoke remains at dead, and stump smoke fades quickly.
- [ ] For char/desiccation visuals, confirm runtime surface tint remains disabled until a real darkening shader/material path is available; native `#Dying`, `#Dead`, and `#Leftover` model states are the current tree surface-state source of truth.

## Prepared Burn Containment Matrix

Dependency-light acceptance covers the rule inputs that must make prepared burns more bounded before live QA tuning starts. Live acceptance still requires Computer Use evidence from a comparable prepared burn and unprepared/control burn.

| Preparation Input | Dependency-Light Coverage | Live QA Status | Expected Containment Signal |
| --- | --- | --- | --- |
| Moisture | Pass | Not Run | Damp prepared fuel produces lower heat, ember pressure, and ignition progress at the boundary than dry control fuel. |
| Water | Pass | Not Run | A water firebreak plane prevents active pressure from reaching burnable fuel beyond the break. |
| Barriers | Pass | Not Run | Barrier resistance lowers boundary heat, ember pressure, and ignition progress compared with unprepared fuel. |
| Exposed faces | Pass | Not Run | Closed lateral faces block lateral transfer into the adjacent prepared target. |
| Spacing | Pass | Not Run | A spaced comparable target receives no one-step pressure while an adjacent control target receives active pressure. |
| Profile differences | Pass | Not Run | A low-risk ignition threshold keeps the same sampled pressure from igniting while a high-risk threshold can ignite. |

## Visual Authoring QA

- [ ] Open `Prometheus` -> `Visuals`.
- [ ] Confirm Timberborn object selection still works while the panel is open.
- [ ] Select a Bakery, platform, tree, and berry bush; confirm target summaries are readable.
- [ ] Apply one effect and the full preset to supported entities.
- [ ] Change native source/search/preset values and confirm the armed preview updates.
- [ ] `Clear Preview` removes temporary particles/material overrides.
- [ ] `Reset Fire State` clears active visual previews without changing selection behavior.
- [ ] `Copy JSON` / `Log JSON` include selected target context.

## Ember/Grid Spread Validation Matrix

Run across each profile once behavior is coherent.

Current QA caveat: CLI autoload reaches Prometheus startup but crashes Timberborn behavior/navigation ticks, including the clean `Prometheus QA` / `beginning` save. The CLI path calls Timberborn's instant scene loader, while normal menu loading uses the non-instant scene-loader path. As of 2026-04-28, use the normal Load Game menu and a known-good `Prometheus QA` autosave that reaches `Load time:` within 15 seconds; do not use the latest `Continue` target as proof until that path is revalidated.

| Profile | Dry fuel propagation | Moisture/steam dampening | Firebreak/barrier | High-risk source | Low-risk non-source | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| Low | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |
| Standard | Partial Pass | Not Run | Not Run | Not Run | Not Run | One forced Pine ignition verified moisture/fuel lifecycle and no neighbor cascade. |
| High | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |

Pass criteria:

- [ ] Propagation is visible and attributable.
- [ ] Moisture evaporates before full burning and produces readable dried-tree feedback.
- [ ] Fuel depletion behaves like fire health: trees die after sustained fuel loss, stop contributing fuel once burned out, and progress through native dried, dead, and stump models before settling as charred remnants.
- [ ] Ignition is stochastic from sampled local field strength, fuel, oxygen, moisture, and profile threshold rather than a deterministic all-neighbor cascade.
- [ ] Fuel, barriers, and thresholds behave consistently.
- [ ] Low/Standard/High profiles differ without runaway spread or visual spam.

## Worker And Beaver Exposure

- [ ] Assigned workers inside burning buildings receive intended exposure effects.
- [ ] Assigned worker exposure does not depend on the worker being physically near the building transform.
- [ ] Nearby beavers are affected by proximity without colony-wide spillover.
- [ ] Workers recover after fire pressure clears or `Reset Fire State` is used.

## Explosion Policy

Use [VALIDATION/explosion-policy.md](VALIDATION/explosion-policy.md) when explosion ignition behavior is active or gaps reappear.

## Evidence Template

| Date | Scenario | Command / Profile | Result | Evidence Path | Notes |
| --- | --- | --- | --- | --- | --- |
| 2026-04-28 | Lifecycle, loaded-scene guardrails, and QA command bridge | `Continue`, `Prometheus QA - 2026-04-28 17h18m, Day 6-17.autosave` | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --launch`, Computer Use, `Player.log`, `Fire.log` | Handoff review found more than singleton registration risk: early scene/component-cache work, unsafe CLI/Continue load paths, and bad autosave fixtures. Runtime lifecycle hooks now register through helper methods, world-updated Prometheus singletons wait for `WorldReady`, loaded-scene object scans share `PrometheusLoadedSceneObjectLookup`, and `PrometheusQaCommandSingleton` now owns the `command.txt` bridge. Tests passed with `120`; the current autosave loaded in `11886ms`; tree, crop, and building commands passed live. |
| 2026-04-28 | Post-load settle gate | Normal `Continue` load of current `Prometheus QA` autosave | Live Pass | `bash scripts/build.sh --test --launch`, Computer Use, `Player.log`, `Fire.log` | User suspected Prometheus entity systems were firing before the scene was fully loaded. Patch makes runtime `WorldReady` wait four seconds after Timberborn `PostLoad()` before entity updates touch component caches/model/yielder/recovery state. Verification passed with `120` tests. The `Continue` path loaded `Prometheus QA - 2026-04-28 09h08m, Day 6-11.autosave` in `11733ms`; logs show `world_load_state_changed ready=false stage=post_load_settling` at `09:38:12`, no component probes during the settle window, and component/Yielder probes starting at `09:38:16`. |
| 2026-04-28 | Tree Fertile Ash remnant harvest | Forced Pine burn through stump/aftermath | Live Pass | `bash scripts/build.sh --launch`, Computer Use, `Fire.log`, `Player.log`, `/tmp/prometheus-tree-ash-remnant-live.png` | User reported burned trees still drop Fertile Ash as visible Rubble. The forced Pine logged `fertile_ash_tree_remnant_yield_applied` at stump stage and terminal `fertile_ash_spawn_queued reason=charred_tree_remnant_harvest`, with no `fertile_ash_recovered_good_stack_queued` rows. This verifies the remnant-harvest path and no recovered-good Rubble queue for the sampled charred tree. |
| 2026-04-28 | Pre-ash stump reignition regression | Forced Pine burn through stump stage before ash marker | Live Pass | `bash scripts/build.sh --launch`, Computer Use, `Fire.log`, `Player.log` | User reported stumps can flare back to alive trees on fire before final burnout and ash-circle creation. The forced Pine logged `visual_surface_material_texture_applied stage=stumpandcharred` at fuel `0.447`, then continued burning until burnout without alive-tree resurrection observed. |
| 2026-04-28 | Crop Fertile Ash aftermath | Forced Carrot burn through aftermath | Live Pass | `bash scripts/build.sh --launch`, Computer Use, `Fire.log`, `Player.log` | `ignite-first-crop` selected `Carrot(Clone)`, consumed the forced ignition, burned through extinction, and logged `burned_ground_ash_deposit_created`, `burned_ground_ash_deposit_marker_created`, `fertile_ash_recovered_good_stack_queued`, and `fertile_ash_spawn_queued reason=charred_crop` with `sourceKind=charredcrop`, `damageCategory=crop`, and `cropContext=burned_crop`. |
| 2026-04-28 | Building Fertile Ash aftermath | Newly placed unfinished Bakery construction site in current `Prometheus QA` save | Live Pass | `bash scripts/build.sh --launch`, Computer Use, `Fire.log`, `Player.log` | The first building command found `Bakery.FolktailsPreview`, so QA targeting now excludes preview/template objects. A newly placed unfinished `Bakery.Folktails(Clone)` construction site then passed as a real fire-profiled building target: `qa_command_result result=success category=building`, `building_operations_disabled`, `burned_ground_ash_deposit_created sourceKind=charredbuilding damageCategory=building`, marker creation, `fertile_ash_recovered_good_stack_queued amount=4`, and `fertile_ash_spawn_queued reason=charred_building`. |
| 2026-04-28 | Soil-moisture sampler guard | Fire-grid environment sampling | Test Pass, deploy pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --launch` | The environment adapter no longer asks Timberborn `ISoilMoistureService` for air/out-of-terrain cells above the known terrain top surface, which was the likely source of the de-duplicated `environment_adapter_sample_failed input=soil_moisture detail="IndexOutOfRangeException"` warning. Plain tests passed with `121`; build/deploy completed, but Steam did not start Timberborn within the launch detection window, so the warning disappearance still needs a live log scan. |
| 2026-04-28 | First suppression slice | Pine stand in `Prometheus QA / beginning_safe` | Live Pass | `bash scripts/build.sh --test --launch`, Computer Use, `Fire.log`, `Player.log` | `Suppress Selected` applied a temporary radius field after a Pine burn was active. `Fire.log` captured `fire_suppression_area_queued`, `fire_suppression_area_applied`, and `fire_suppression_area_expired`. A tracked Pine dropped from `intensity=1.000 heat=0.650 ember=0.350 smoke=0.250` before suppression to `intensity=0.450 heat=0.450 ember=0.235 smoke=0.165` after suppression, and active grid cells later contracted to one. Preflight passed with `116` tests and the log scan had no Prometheus exception beyond the known soil-moisture sample warning. |
| 2026-04-28 | Tree stump lifecycle plus burned-ground ash | Pine stand in `Prometheus QA / beginning_safe` | Live Pass | `bash scripts/build.sh --test --launch`, Computer Use, `Fire.log`, `Player.log` | Live Pine ignition produced native dead/stump visuals without the prior resurrection loop in the clean fixture. Logs captured `burned_ground_ash_deposit_created`, `burned_ground_ash_deposit_marker_created`, `fertile_ash_recovered_good_stack_queued`, and `fertile_ash_spawn_queued` with `sourceKind=charredtree`. Visible burned-ground markers appeared around the burned stand. `Stop Fires` also passed live after the enumeration fix with `debug_stop_all_fires_result result=success count=33`. |
| 2026-04-28 | Current clean fixture | Normal Load Game menu, `Prometheus QA / beginning_safe` | Pass | `Player.log` | `beginning_safe` loaded in `12363ms` after the fresh deploy. The latest autosave `2026-04-28 02h15m, Day 7-1.autosave` hung on Timberborn's loading screen after Prometheus post-load probes, so it is not the current QA fixture. |
| 2026-04-28 | Stable fixture comparison and dead-tree reignition | Normal Load Game menu, `Prometheus QA / beginning cli-safe` | Live Pass | Computer Use, `Fire.log`, `Player.log` | `beginning cli-safe` loaded in `12922ms` after `beginning_safe` had shown a loading-screen/display blocker. Reigniting a previously dead Pine kept the visible tree in dead/stump presentation rather than alive state. Logs captured `visual_surface_material_texture_applied stage=stumpandcharred`, local burned-ground ash deposit marker creation, and recovered-good stack queueing for multiple `sourceKind=charredtree` sources without a new Prometheus exception. |
| 2026-04-28 | Pine native model-state switching | Ignite a healthy pine and observe dry/dead/stump model changes | Pending Live QA | `Fire.log`, `Player.log`, screenshot or video after fresh `bash scripts/build.sh --test --launch` | User confirmed rejected overlay blocks are gone and asked whether trees can have additional states. Patch drives native pine model children directly from Prometheus tree stages: `#Alive`, `#Dying`, `#Dead`, and `#Leftover`. Verification passed: `106` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and the scanned logs had no Prometheus exception. Retest should verify the pine model changes to dry, then dead, then stump without generated overlay geometry. |
| 2026-04-28 | Live tree visual progression | Ignite a healthy tree and observe through burnout | Pending Live QA | `Fire.log`, `Player.log`, screenshot or video after fresh `bash scripts/build.sh --test --launch` | User requested live trees progress visually as healthy, dried, dried-and-charred, dead-and-charred, then stump-and-charred. Patch adds explicit tree natural-resource visual staging: dried/dead-looking native resource flags apply before burnout, and the native stump/dead flag applies only when the exposure is burned out. Verification passed: `105` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and the scanned logs had no Prometheus exception. Retest should verify the tree does not jump straight from dried/charred to stump before burnout. |
| 2026-04-28 | Tree chain distance follow-up | Tree line or compact stand | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User said crops are good enough for now, but trees still only maybe light one other tree. Patch leaves crop tuning unchanged and adds a weak tree-only horizontal halo with radius `2`: immediate ring `0.18`, outer ring `0.32`, smoke multiplier `0.10`. Verification passed: `104` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and `git diff --check` was clean. Retest should verify tree fire chains past one neighbor without making first-hop spread too fast. |
| 2026-04-28 | Tree distance and crop outer-ring ignition | Trees plus contiguous carrot block | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User reported tree speed feels good but spread does not travel far enough, likely because trees lose heat too fast; crops spark at radius `2` but do not light. Patch adds tree-only fuel consumption multiplier `0.75` and raises crop outer-ring halo strength from `0.28` to `0.38` while keeping immediate crop strength `0.68`. Verification passed: `104` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and `git diff --check` was clean. Retest should verify trees carry fire farther without faster first-hop spread and radius-`2` carrot sparks can ignite. |
| 2026-04-28 | Tree spread plus reset-hang follow-up | Trees and contiguous carrot block after reset/relight | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User reported crops still spread fast, trees not fast enough, and a hang after reset plus relighting. Logs showed reset completed with `failures=0` and bounded grid counts, but CPU pegged during active fires. Patch throttles missing-NeedManager scans in `FireBeaverEffectApplier` to the shared 5s interval, raises global neighbor heat/ember weights to `0.15`/`0.12`, and trims crop halo strengths to `0.68` immediate / `0.28` outer. Verification passed: `104` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and `git diff --check` was clean. Retest should verify no post-reset CPU peg, faster tree spread, and crop spread not faster than the previous pass. |
| 2026-04-28 | Crop second-ring preheat trim | Contiguous carrot block | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User reported radius `1` still only lit initial neighbors. Patch restores crop halo radius `2` with lower strengths than the fast-spread pass: immediate neighbor `0.72`, outer ring `0.32`, crop fuel consumption `1.6`. Verification passed: `104` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and `git diff --check` was clean. Retest should verify second-ring handoff without the earlier too-fast block spread. |
| 2026-04-28 | Crop propagation sustain trim | Contiguous carrot block | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User reported the radius-`1`, strength-`0.70` crop halo made carrots go out again. Patch keeps radius `1`, restores immediate-neighbor strength to `0.78`, and slows crop fuel consumption from `1.8` to `1.6`. Verification passed: `104` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and `git diff --check` was clean. Retest should verify contiguous crop spread chains without the previous radius-`2` fast leapfrog. |
| 2026-04-28 | Crop propagation speed trim | Contiguous carrot block | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User confirmed the smoke wave is gone and crop fire now spreads through the block, but slightly too fast. Patch narrows the crop-only halo from radius `2` to radius `1` and trims immediate-neighbor strength from `0.78` to `0.70`. Retest should verify contiguous crop spread still chains but advances less abruptly. |
| 2026-04-28 | Crop propagation after air-relay removal | Compact carrot patch | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User confirmed the smoke wave is gone, but carrots only ignite the initial ring and do not continue spreading. Patch adds a crop-only horizontal near-field heat/ember halo with radius `2`, only into existing non-air/non-underwater grid environments, and ensures air cells cannot receive or relay non-self propagation. Retest should verify chained carrot ignitions without bounded-cell-count regression. |
| 2026-04-28 | Instrumented crop wave root-cause pass | Carrot patch near distant trees | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | Logs showed healthy no-exposure pines rendering `visualSmoke=0.120` from the terminal dead-building fallback, and grid active cells growing from `2` to `182475` after the carrot ignition. Patch changes the visual fallback to a cold snapshot and prevents non-self transfer into open air so empty cells cannot relay the wave. Retest should verify no untouched-tree smoke and bounded `grid_runtime_state` counts. |
| 2026-04-28 | Crop wave telemetry pass | Carrot patch near distant trees | Pending Live QA | `Fire.log` after fresh `bash scripts/build.sh --test --launch` | User reported the same visible wave after crop smoke suppression, while horizontal carrot ignition improved and four carrots burned in a square. Added capped `visual_runtime_intensity` telemetry and once-per-second `grid_runtime_state` telemetry to distinguish grid expansion from visual-channel effects before more tuning. |
| 2026-04-28 | Crop smoke applier suppression | Carrot patch near distant trees | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User still saw the smoke wave after crop smoke rule changes, and log counts were low enough that logging is not the likely slowdown cause. Patch suppresses smoke directly in `FireVisualEffectApplier` when the entity has a crop `FireProfile`, avoiding damage-state timing gaps during early ignition. |
| 2026-04-28 | Crop row-footprint follow-up | Carrot patch near distant trees | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User reported the same smoke wave, vertical pattern, and slowdown after smoke-only active-cell pruning. Patch uses transform-cell footprints for crop profiles instead of renderer bounds and disables crop smoke visuals, targeting line-shaped crop footprints and native smoke plumes. Retest should verify no row-shaped wave and no slowdown. |
| 2026-04-28 | Far smoke wave and slowdown follow-up | Carrot patch near distant trees | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --test --launch` | User still saw smoke reach distant trees and then the sim slowed down. Patch makes smoke decay faster, reduces neighbor/upward smoke propagation, removes healthy non-burning smoke visuals, and prevents smoke-only cells from keeping the fire grid active. Retest should verify no far smoke wave, no slowdown, and no tree ignition unless heat/ember actually reaches the tree line. |
| 2026-04-28 | Crop smoke-wave follow-up | Carrot patch | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --launch` | User reported a fast initial smoke wave across the field, with only row plants sustaining burn and other plants fading almost immediately. Patch suppresses low non-burning smoke visuals, lowers carrot ignition threshold, shifts crop burning output toward ember instead of smoke, and slows crop fuel consumption slightly. Retest for subdued smoke wave and sustained adjacent crop burn. |
| 2026-04-28 | Crop directional-bias follow-up | Carrot patch, multiple camera orientations | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --launch` | User reported crop fires spread vertically from the current viewpoint but not horizontally. Patch registers `FireGridSimulationSingleton` and removes per-entity controller stepping so grid propagation advances centrally once per frame instead of depending on entity update order. Retest a compact carrot patch from multiple camera orientations. |
| 2026-04-28 | Crop-to-tree overreach follow-up | Carrot patch near trees | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --launch` | User reported crops appeared to affect trees much farther away. Patch dampens crop-profile burning heat, ember, and smoke emissions, lowers carrot ignition threshold for local crop spread, and increases crop fuel consumption so crop burns end sooner. Retest with a small carrot patch near but not touching a tree line. |
| 2026-04-28 | Crop spread follow-up after live hang report | Carrot patch | Pending Live QA | `Fire.log`, `Player.log` after fresh `bash scripts/build.sh --launch` | User reported tree spread felt good, but crops did not work well and the game hung soon after carrot ignition. Log scan showed one carrot ignited and kept burning without adjacent carrot spread or crop ash before capture ended, while prior tree-fire activity was still heavy. Follow-up patch lowers carrot fuel and ignition threshold and speeds crop-profile moisture/fuel consumption. Retest from a quiet state before larger tree burns. |
| 2026-04-28 | Fire spread tuning | Grid/rules/visuals | Dependency-Light Pass | `bash scripts/test.sh` | Raised neighbor heat/ember transfer and burning emission, softened the marginal ignition probability curve, and rendered light smoke from non-burning exposed smoke pressure. Plain C# suite passed 99 tests. Live QA should compare tree-line and carrot-patch spread against containment controls. |
| 2026-04-26 | Forced Pine ignition resource lifecycle | Standard | Partial Pass | `/tmp/prometheus-throttled-ignite-24s.png` + `Fire.log` | Moisture reached zero, fuel crossed 0.25 death threshold, fuel reached burnout, and throttled telemetry emitted 16 burn rows with no scanned Player.log errors. |
| 2026-04-27 | Configured source dependency-light propagation | High-risk source | Pass | `bash scripts/test.sh` | Source fields produce attributed grid pressure, respect `RequiresOperation`, and can create nonzero stochastic ignition probability through grid propagation. Live menu startup had no emitting source rows because deployed authored profiles are zero-source at startup. |
| 2026-04-27 | Effect facade and reset registry startup | Workplace, beaver, damage, recovery | Pass | `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log` | Effect appliers resolve direct/cached Timberborn components through the integration facade; reset registry discovery uses the same facade lookup. Computer Use reached the main menu and startup logs showed Prometheus loaded with no scanned Prometheus exceptions. |
| 2026-04-27 | Fertile Ash recovered-good wrapper | Recovery | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Fire.log`, Computer Use | Native ash gatherable template was not confirmed; wrapper queues `FertileAsh` through Timberborn recovered-good stacks after good-registration validation. Live QA captured `fertile_ash_recovered_good_stack_queued`, `fertile_ash_spawn_queued`, visible Rubble with `Fertile ash 1`, and District Center storage with `Fertile ash 7`. |
| 2026-04-27 | Fertile Ash field amendment crop growth | Recovery | Dependency-Light Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log` | Eligible crop growables receive a 10% growth-speed buff from active field amendments; trees and bushes are excluded. Startup logs showed Prometheus loaded with no scanned Prometheus errors. Live farmhouse/farmer application remains owned by P2S-024/P2S-025. |
| 2026-04-27 | Fertile Ash recovered-good spawn and storage | Recovery | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Fire.log`, Computer Use | Valid charred Pine aftermath queued native recovered-good stacks at `49,3,7` and `23,4,11`; Computer Use confirmed visible Rubble with `Fertile ash 1`, and District Center storage showed `Fertile ash 7` after beaver pickup. Logs had one de-duplicated soil-moisture sample warning and no scanned recovered-good exception. |
| 2026-04-27 | Fertile Ash reset telemetry | Recovery | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log`, Computer Use | Ash recovered-good queue telemetry clears through `Reset Fire State` while leaving Timberborn-owned recovered-good entities alone. Live reset logged `fertile_ash_reset_state queuedStacks=0 queuedAmount=0 source=none sourceKind=none damageCategory=none nativeStacksDestroyed=0 reason=native_recovered_good_stack_owned_by_timberborn`, followed by `runtime_reset_registry_completed failures=0`. |
| 2026-04-27 | TKT-006 runtime visual readability rules | Visuals | Dependency-Light Pass | `bash scripts/test.sh` | Existing projection state now drives ember-field pressure, smoke, fire, steam, char, and desiccation policy. Tests cover ember pressure visibility on exposed non-burning targets, local burning sparks staying disabled, and low ember noise staying hidden to avoid spam. Live prepared-burn screenshot/video evidence and log scans remain required. |
| 2026-04-27 | Sprint guardrails | Repo | Pass | `git diff --check`, `bash scripts/test.sh` | Guardrails now fail the plain C# suite if internal Markdown ships under `Assets/Mods/Prometheus`, dependency-light compile items drift from `Prometheus.Tests.csproj`, or QA-facing telemetry event tokens disappear from `FireTelemetryEvents.All`. |
| 2026-04-27 | World-load readiness gate | Startup/save load | Live Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, Computer Use, `Player.log`, `Fire.log` | Prometheus now gates expensive runtime work behind Timberborn `IPostLoadableSingleton.PostLoad()`. `Prometheus QA - 2026-04-27 15h28m, Day 4-11.autosave` loaded in `12644ms`; logs recorded `world_load_state_changed ready=false stage=load`, `world_load_state_changed ready=true stage=post_load`, component-cache resolution through `_components`, native visual resolution, and no scanned Prometheus exceptions. Plain C# suite is now 91 passing tests. |
| 2026-04-27 | Carrot crop fire profile authoring | Content | Partial Live Pass | User live QA, `Player.log`, `Fire.log` | Carrot `Ignite Selected` now reaches `ignite_selected_queued`, `grid_ignition_seeded`, `ignited`, burn ticks, and `extinguished`; invalid-target rejection is also logged for `WeatherStation.Folktails(Clone)`. Burned carrots queued and spawned Fertile Ash with `sourceKind=charredcrop`, `damageCategory=crop`, and `cropContext=burned_crop`. User live QA confirmed District 1 storage reached `Fertile ash 18` after the carrot burns, but did not see ash goods on the ground. No Prometheus exception was scanned; one de-duplicated soil-moisture sample warning remains. TKT-004 still needs explicit visible crop stack proof or a visibility fix. |
| 2026-04-27 | Prepared burn containment matrix | Grid/rules | Dependency-Light Pass | `bash scripts/test.sh` | Added deterministic tests proving moisture, water firebreak planes, barriers, exposed-face limits, spacing, and profile thresholds make prepared burns more bounded than unprepared/control burns. Live prepared-vs-control burn evidence remains required for TKT-005 acceptance. |
| 2026-04-27 | Fertile Ash farmhouse amendment | Recovery | Blocked | `bash scripts/test.sh`, `bash scripts/build.sh --qa`, copied save fixture, `Player.log`, `Fire.log` | The discarded prototype passed 90 tests and built/deployed, but it was not kept as implementation. The copied `Prometheus P2S-025 QA` fixture crashed during `Timberborn.DwellingSystem.Dweller.Load`; a local repair removed `Dweller` components with `Home: null`, but the repaired copy hung during load after component-cache resolution. Required evidence remains ash consumption, `fertile_ash_farmhouse_amendment_applied`, and faster amended crop growth than a nearby control from a fresh loadable fixture. |
| 2026-04-27 | Example food-chain cleanup | Content | Pass | Reference scan, JSON parse scan, `bash scripts/test.sh`, `bash scripts/build.sh` | Removed the leftover example goods/need/recipes, dedicated stove registration/assets, related localization, and bakery recipe append. Remaining blueprints parse, 89 plain C# tests passed, and build/deploy completed. |
| 2026-04-27 | Stabilization closeout docs | Repo | Pass | `git diff --check`, `bash scripts/test.sh`, `bash scripts/build.sh --qa`, `Player.log`, `Fire.log` | P2S-027 passed closeout validation with 89 plain C# tests. `--qa` deployed the current branch, cleared logs, and launched Timberborn. `Player.log` showed `- Prometheus (v0.2)` and startup initialization; `Fire.log` recorded the compatibility summary. No scanned Prometheus exceptions were present. Source-driven spread remains dependency-light/startup-clean until a live emitting-source fixture exists. Faster crop growth remains dependency-light only, not live farmhouse-applied. |
| YYYY-MM-DD |  |  | Pass/Fail |  |  |

## Session Closeout

- [ ] Copy one representative debug snapshot into notes or handoff.
- [ ] Update [HANDOFF.md](HANDOFF.md) with new verified results, blockers, and next action.
- [ ] Update [DESIGN.md](DESIGN.md) only when a durable design decision, milestone, or accepted default changes.
- [ ] Add archive/changelog detail only when the history is useful after the next startup.

Documentation-only updates do not require `bash scripts/test.sh`, `bash scripts/build.sh`, or live QA. Verify source-of-truth links and claims instead; run `git diff --check` when practical.
