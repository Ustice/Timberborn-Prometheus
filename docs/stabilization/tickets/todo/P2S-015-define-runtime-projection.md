# P2S-015 Define Runtime Projection

Status: todo

Agent level: Medium

Dependencies: P2S-011, P2S-012

## Objective

Define one projection from grid and entity state for downstream effects.

## Requirements

- Create one projection consumed by visuals, damage, workplace, beavers, recovery, and debug.
- Preserve current behavior where consumers are not migrated yet.
- Avoid each consumer reinterpreting raw grid/entity state differently.
- Document any consumers intentionally left for later.

## Unknowns

- Which consumers can be safely migrated in one ticket depends on current post-refactor shape.

## Write Scope

- Projection model/module.
- Consumer call sites migrated in this ticket.
- Tests for projection rules.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh`.

## Integration Notes

Medium review required because this sets the contract for visuals, effects, and renewal.

