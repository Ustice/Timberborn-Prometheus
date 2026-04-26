# P2S-021 Ash Reset Telemetry QA

Status: todo

Agent level: Low

Dependencies: P2S-020

## Objective

Add reset, telemetry, and QA coverage for Fertile Ash gatherables.

## Requirements

- Reset removes stale ash state.
- Reset does not destroy unrelated Timberborn entities unsafely.
- Logs include ash source attribution.
- QA can show ash spawn, collection, and reset evidence.

## Unknowns

- Exact reset hook depends on the native ash representation from P2S-020.

## Write Scope

- Reset registry ash hooks.
- Ash telemetry.
- QA/debug display if needed.
- Tests where dependency-light.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Inspect `Player.log` and `Fire.log`.

## Integration Notes

This closes the native ash loop before field amendments consume ash.

