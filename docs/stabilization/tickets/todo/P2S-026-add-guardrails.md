# P2S-026 Add Guardrails

Status: todo

Agent level: Low

Dependencies: P2S-006

## Objective

Add lightweight checks that prevent known sprint regressions.

## Requirements

- Test that internal Markdown does not ship under `Assets/Mods/Prometheus`.
- Test telemetry constants that QA depends on.
- Guard compile-item sync drift.
- Keep checks available through existing test/build entrypoints.

## Unknowns

- None known.

## Write Scope

- Test project.
- Scripts only if needed for a non-mutating guard.
- Docs if the command surface changes.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

This can run late, but it should land before sprint closeout.

