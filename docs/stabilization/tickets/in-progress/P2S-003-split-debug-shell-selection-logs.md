# P2S-003 Split Debug Shell Selection Logs

Status: in-progress

Agent level: Low

Dependencies: P2S-001

## Objective

Split debug panel shell, selection state, and logs from the large debug fragment without changing behavior.

## Requirements

- Preserve visible debug UI behavior.
- Preserve selected entity display and `View` behavior.
- Preserve log display and filtering behavior.
- Keep admin actions and visual authoring behavior untouched except for compile-required references.

## Unknowns

- No feature unknowns.
- Merge risk is high because the source file is large.

## Write Scope

- `Assets/Mods/Prometheus/Scripts/Debug/`

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Coordinate with P2S-004 and P2S-005. They should not start until this split is integrated.
