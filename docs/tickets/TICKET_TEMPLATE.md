---
ticket: TKT-000
status: todo
agent_level: Low
requires_qa: false
doc_only: false
dependencies: []
write_scope:
   - path/or/glob
---

# TKT-000: Short Imperative Title

## Goal

State the outcome in one or two sentences.

## Requirements

- Add concrete requirements.
- Keep the work scoped enough for one worker.

## Dependencies

- List ticket ids, evidence, or decisions that must exist first.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh` unless this is a documentation-only ticket.
- Run `bash scripts/build.sh --qa` when live runtime behavior changes.

## Notes

- Add implementation hints, unknowns, and evidence links.
