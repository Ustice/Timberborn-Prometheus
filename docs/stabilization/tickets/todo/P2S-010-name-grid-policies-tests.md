# P2S-010 Name Grid Policies Tests

Status: todo

Agent level: Low

Dependencies: P2S-002, P2S-006

## Objective

Promote grid coefficients into named policies and add deterministic propagation tests.

## Requirements

- Name policy values for upward heat, upward smoke, outward embers, oxygen, water, barriers, and bounds.
- Add deterministic tests for each policy.
- Avoid balance tuning beyond preserving current behavior.
- Keep policies dependency-light.

## Unknowns

- Final gameplay balance values are intentionally not decided here.

## Write Scope

- Grid policy and propagation files.
- Grid test files.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This ticket should land before source attribution and simulation coordination.

