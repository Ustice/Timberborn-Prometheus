# P2S-013 Wire Configured Sources

Status: integration

Agent level: High

Dependencies: P2S-008, P2S-011, P2S-012

## Objective

Use configured source fields from `FireProfileSpec` to inject heat, embers, and smoke into the grid.

## Requirements

- Consume profile heat source, ember source, smoke source, source radius, and operation requirement fields.
- Respect operation state through the integration facade.
- Log configured source events with attribution.
- Prove a configured source can ignite via grid/source behavior rather than nearest-target shortcuts.

## Unknowns

- Reliable Timberborn operation-state API needs integration probing.
- Some profile fields may need tuning after live validation.

## Write Scope

- Fire exposure/source injection code.
- Integration facade operation adapter.
- Source telemetry and tests.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa`.
- Inspect `Player.log` and `Fire.log`.

## Integration Notes

High agent recommended because this crosses profile data, Timberborn operation state, grid runtime, and live behavior.
