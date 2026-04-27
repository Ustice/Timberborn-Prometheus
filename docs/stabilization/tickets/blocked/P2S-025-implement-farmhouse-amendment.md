# P2S-025 Implement Farmhouse Amendment

Status: blocked

Agent level: High

Dependencies: P2S-024

## Objective

Implement farmhouse-driven Fertile Ash field amendments.

## Requirements

- Consume stored `FertileAsh`.
- Select unfertilized eligible crop planting spots in farmhouse range.
- Apply field amendments.
- Show amended crops growing faster than adjacent control crops.
- Add reset and telemetry coverage.

## Unknowns

- UX controls and status messages depend on the hook chosen in P2S-024.

## Write Scope

- Farmhouse/farmer integration implementation.
- Field amendment application.
- Telemetry, debug display, and tests where possible.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Capture live QA evidence for ash consumption and crop growth speed.

## Integration Notes

Do not integrate without live evidence. This is the first end-to-end crop production loop.

## Blocker

P2S-025 has a compile-clean partial implementation in worktree `/Users/jasonkleinberg/repos/Timberborn-Prometheus-P2S-025`, but it is not verified enough to integrate.

Evidence collected:

- `bash scripts/test.sh` passed with 90 tests in the P2S-025 worktree.
- `bash scripts/build.sh --qa` passed preflight tests, compiled, deployed, cleared logs, and launched Timberborn.
- The compile-clean prototype is committed on branch `codex/P2S-025-implement-farmhouse-amendment` at `e2c70ac`.
- The default `Prometheus QA` save has stored `FertileAsh` but no finished farmhouse/crop fixture.
- A copied fixture save was prepared at `/Users/jasonkleinberg/Documents/Timberborn/ExperimentalSaves/Prometheus P2S-025 QA/P2S-025 farmhouse fixture.timber`.
- The copied fixture has an efficient farmhouse, carrot plots, and copied District Center `FertileAsh` storage.
- Loading the copied fixture through the known Timberborn mod-version mismatch warning crashed during save deserialization before amendment QA could run.
- Crash evidence: `/Users/jasonkleinberg/Documents/Timberborn/Error reports/error-report-2026-04-27-04h47m00s.zip`; `Player.log` recorded `NullReferenceException` in `Timberborn.WorldPersistence.ReferenceSerializer+TypedReferenceSerializer.Parse` from `Timberborn.DwellingSystem.Dweller.Load`.
- Prometheus startup evidence before the crash: `Fire.log` recorded `event=timberborn_compatibility_probe area=recovery status=resolved detail="PlantingSpotFinder.FindClosest(Vector3)"`.
- A local repair removed stale `Dweller` components with `Home: null` from the copied fixture after backing it up as `/Users/jasonkleinberg/Documents/Timberborn/ExperimentalSaves/Prometheus P2S-025 QA/P2S-025 farmhouse fixture.before-dweller-repair.timber`.
- The repaired copied fixture no longer hit the same `Dweller.Load` exception, but it hung during load after `event=timberborn_compatibility_probe area=cache status=resolved detail="ComponentCache._components"` and never reached `Load time` or farmhouse amendment telemetry. Timberborn was stopped after the hung load.

Blocked on:

- A loadable live QA fixture with finished farmhouse, eligible crop planting spots, and stored `FertileAsh`. The current copied fixture should not be reused for acceptance evidence.
- Live QA evidence after load: `fertile_ash_farmhouse_amendment_applied`, decreased stored ash, and amended crop growth faster than a nearby control crop.

Sprint decision:

- P2S-025 is pushed out of the current stabilization sprint.
- Keep this ticket blocked as the follow-up record and keep branch `codex/P2S-025-implement-farmhouse-amendment` for the compile-clean prototype.
- P2S-027 may close the sprint without farmhouse amendment acceptance evidence, as long as the deferral and evidence gap stay documented.
