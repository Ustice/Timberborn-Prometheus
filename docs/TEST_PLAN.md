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
- [ ] Treat save loading as hung only after two minutes without `Load time:` in `Player.log`; some valid `Prometheus QA` loads take longer than one minute on this mod stack.
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

Current QA caveat: CLI autoload reaches Prometheus startup but crashes Timberborn behavior/navigation ticks, including the clean `Prometheus QA` / `beginning` save. The CLI path calls Timberborn's instant scene loader, while normal menu loading uses the non-instant scene-loader path. As of 2026-04-26, current `main` loads the latest `Prometheus QA` autosave through `Return`, `Return`, `Continue`, `Yes`; use that normal-menu path before treating forest-spread QA as a Prometheus failure.

| Profile | Dry fuel propagation | Moisture/steam dampening | Firebreak/barrier | High-risk source | Low-risk non-source | Outcome |
| --- | --- | --- | --- | --- | --- | --- |
| Low | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |
| Standard | Partial Pass | Not Run | Not Run | Not Run | Not Run | One forced Pine ignition verified moisture/fuel lifecycle and no neighbor cascade. |
| High | Not Run | Not Run | Not Run | Not Run | Not Run | Not Run |

Pass criteria:

- [ ] Propagation is visible and attributable.
- [ ] Moisture evaporates before full burning and produces readable brown/desiccated feedback.
- [ ] Fuel depletion behaves like fire health: trees die after 25% fuel loss, burn out at zero fuel, stop contributing fuel, and remain as charred stumps/remnants.
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
| 2026-04-28 | Pine surface tint fallback | Ignite a healthy pine and observe leaf/trunk tint during burn | Pending Live QA | `Fire.log`, `Player.log`, screenshot or video after fresh `bash scripts/build.sh --test --launch` | User reported pines showed no surface change beyond particle effects. Logs showed fire/damage states advanced, but `LivingNaturalResource.IsDying/IsDead` is missing, so native tree flags cannot drive pine visuals. Patch re-applies surface tint in `LateUpdate`, tints renderer material instances as well as property blocks, and adds `visualChar`, `visualDesiccation`, and capped `visual_surface_tint_applied` telemetry. Verification passed: `105` tests, deploy/launch completed, startup showed Prometheus v0.2 loaded, and the scanned logs had no Prometheus exception. Retest should verify visible brown/char darkening independent of fire/smoke particles. |
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
