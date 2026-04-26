# P2S-011 Add Grid Simulation Coordinator

Status: done

Agent level: Medium

Dependencies: P2S-010

## Objective

Move once-per-frame grid stepping out of entity controllers and into one simulation coordinator.

## Requirements

- Add one runtime owner for grid stepping.
- Remove entity-controller calls that step the global grid.
- Preserve once-per-frame behavior.
- Preserve current source and sample behavior.

## Unknowns

- Best Bindito or Timberborn lifecycle hook must be confirmed against current startup behavior.

## Write Scope

- Grid runtime/coordinator files.
- `FireExposureController` or equivalent source-submitters.
- `PrometheusConfigurator` bindings if needed.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Medium review required because this changes runtime ownership.
