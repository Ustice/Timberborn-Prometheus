# P2S-002 Split Grid Runtime

Status: todo

Agent level: Low

Dependencies: P2S-001

## Objective

Split the overlarge grid runtime file without changing behavior.

## Requirements

- Split `FireGridRuntimeState.cs` into focused grid value types, chunk storage, footprint sampling, kernel policy, propagation rules, and runtime state.
- Preserve all existing behavior and call sites.
- Keep dependency-light grid code testable by the plain C# test suite.
- Avoid tuning or propagation behavior changes in this ticket.

## Unknowns

- None known.

## Write Scope

- `Assets/Mods/Prometheus/Scripts/Fire/Grid/`
- `tests/Prometheus.Tests/Prometheus.Tests.csproj` only if compile includes need updates.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Integrate before tickets that add grid policies, source attribution, or simulation coordination.

