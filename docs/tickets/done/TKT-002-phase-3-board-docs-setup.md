---
ticket: TKT-002
status: done
agent_level: Low
requires_qa: false
doc_only: true
dependencies: []
write_scope:
   - README.md
   - docs/TODO.md
   - docs/DESIGN.md
   - docs/TEST_PLAN.md
   - docs/HANDOFF.md
   - docs/tickets/**
---

# TKT-002: Set Up Phase 3 Board And Docs

## Goal

Create the Phase 3 sprint tracking surface for intentional fire and ash harvest.

## Requirements

- Create active tickets for selected-target ignition, burned-crop Fertile Ash, containment validation, and runtime visual readability.
- Mark stale plan language so controlled burns are emergent containment, not a separate mechanic.
- Keep farmhouse ash application deferred under TKT-001 until fresh live fixture evidence exists.
- Update the durable docs that own current focus, design decisions, TODO state, and validation gates.

## Verification

- Run `git diff --check`.
- Skip runtime verification because this is documentation-only and does not claim new runtime behavior.

## Notes

- The implementation tickets created by this setup are TKT-003 through TKT-006.
