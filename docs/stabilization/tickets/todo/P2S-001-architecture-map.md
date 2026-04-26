# P2S-001 Architecture Map

Status: todo

Agent level: Low

Dependencies: None

## Objective

Create the durable architecture map for the stabilization sprint.

## Requirements

- Preserve existing draft documentation.
- Create `docs/ARCHITECTURE.md`.
- Link the architecture map from `docs/INDEX.md`.
- Document module ownership, data flow, reset boundaries, and Phase 3 renewal boundaries.
- Keep `docs/STABILIZATION_SPRINT.md` as the narrative sprint plan.

## Unknowns

- Final module names may change after refactors land.
- Use current intended ownership names and update later if implementation chooses different names.

## Write Scope

- `docs/ARCHITECTURE.md`
- `docs/INDEX.md`

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.
- Confirm all Markdown headings and lists follow `AGENTS.md`.

## Integration Notes

This ticket should be integrated before implementation tickets so fresh agents have the map.

