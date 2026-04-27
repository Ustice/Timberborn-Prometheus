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
- The default `Prometheus QA` save has stored `FertileAsh` but no finished farmhouse/crop fixture.
- A copied fixture save was prepared at `/Users/jasonkleinberg/Documents/Timberborn/ExperimentalSaves/Prometheus P2S-025 QA/P2S-025 farmhouse fixture.timber`.
- The copied fixture has an efficient farmhouse, carrot plots, and copied District Center `FertileAsh` storage.

Blocked on:

- User confirmation before clicking Timberborn's mod-version mismatch `Are you sure you want to load it?` warning for the copied fixture save.
- Live QA evidence after load: `fertile_ash_farmhouse_amendment_applied`, decreased stored ash, and amended crop growth faster than a nearby control crop.
