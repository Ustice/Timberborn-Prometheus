# P2S-005 Split Visual Authoring Tab

Status: verify

Agent level: Low

Dependencies: P2S-003

## Objective

Extract visual authoring and preview tab logic from the debug panel.

## Requirements

- Preserve preview controls.
- Preserve native particle source selection behavior.
- Preserve authoring workflow and displayed values.
- Do not unify runtime and preview catalogs in this ticket.

## Unknowns

- Catalog duplication remains until P2S-016.

## Write Scope

- `Assets/Mods/Prometheus/Scripts/Debug/`
- `Assets/Mods/Prometheus/Scripts/Fire/Visuals/` only if compile-required.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Keep this separate from particle catalog behavior changes.
