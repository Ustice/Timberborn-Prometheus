# P2S-025 Implement Farmhouse Amendment

Status: ready

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

