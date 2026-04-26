# P2S-014 Add Environment Adapters

Status: todo

Agent level: High

Dependencies: P2S-008, P2S-011

## Objective

Add read-only Timberborn environment adapters for grid sampling.

## Requirements

- Expose terrain top surface.
- Expose block and building occupancy.
- Expose exposed face masks.
- Expose water depth.
- Expose soil moisture.
- Wire only verified inputs.
- Keep adapters read-only.

## Unknowns

- Exact Timberborn services and properties for each input must be confirmed from DLLs and live behavior.

## Write Scope

- Integration facade environment adapters.
- Grid environment sampling call sites.
- Tests for adapter normalization where dependency-light.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

High agent recommended because wrong world reads can destabilize live simulation.

