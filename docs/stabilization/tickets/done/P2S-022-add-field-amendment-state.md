# P2S-022 Add Field Amendment State

Status: done

Agent level: Low

Dependencies: P2S-018

## Objective

Add state for Fertile Ash amendments on crop or planting tiles.

## Requirements

- Store amendment by planting or grid coordinate.
- Track duration or charges.
- Add reset behavior.
- Add debug visibility.
- Add dependency-light tests for set, expire, consume, and reset.

## Unknowns

- Final coordinate type may shift after environment and native ash discoveries.

## Write Scope

- Renewal or recovery runtime state files.
- Reset registry registration if available.
- Tests for amendment state.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This ticket should not consume ash yet. It only creates the field-state contract.
