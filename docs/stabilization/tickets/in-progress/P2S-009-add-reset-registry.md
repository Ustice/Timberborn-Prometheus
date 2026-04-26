# P2S-009 Add Reset Registry

Status: in-progress

Agent level: Medium

Dependencies: P2S-007

## Objective

Create a reset registry so debug and QA reset paths cannot miss runtime effects.

## Requirements

- Register reset hooks for grid state, source state, damage, workplace, beaver, recovery, visuals, preview state, and ash state.
- Emit reset telemetry.
- Replace broad duplicated reset scans where the registry can safely do so.
- Keep admin reset behavior safe for live Timberborn entities.

## Unknowns

- Hidden Timberborn state mutations may only be discoverable through live QA.
- Keep unknown reset gaps documented in the ticket handoff.

## Write Scope

- Runtime reset registry module.
- Debug reset command call sites.
- Runtime appliers that need reset hook registration.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Inspect `Player.log` and `Fire.log` for reset errors.

## Integration Notes

Do not integrate without QA evidence because reset paths have caused crashes before.
