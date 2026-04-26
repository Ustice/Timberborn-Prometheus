# Prometheus Stabilization Ticket Board

This directory is the file-based board for the Phase 2 stabilization sprint.

Move ticket files with `git mv` to update status.

## Status Flow

```text
todo -> ready -> in-progress -> verify -> integration -> done
                         \-> blocked
```

## Status Meaning

- `todo/` contains scoped work not yet dependency-ready.
- `ready/` contains work whose dependencies are complete and verified.
- `in-progress/` contains work owned by an active coding agent.
- `verify/` contains work a coding agent says is complete, pending orchestrator review.
- `integration/` contains verified work waiting to be merged or reconciled with the wave.
- `done/` contains integrated work with required checks complete.
- `blocked/` contains work that cannot proceed without new evidence or a decision.

## Orchestrator Rules

- A Medium agent owns wave orchestration.
- Assign disjoint write scopes before agents start.
- Low is the default agent level; tickets marked Medium or High should be assigned accordingly.
- Raise blockers early, especially when the blocker is likely something Jason can solve: missing live-game evidence, unclear product intent, Timberborn API uncertainty, local environment issues, screenshots/logs needed, or a choice between gameplay tradeoffs.
- Move blocked tickets to `blocked/` with a short note in the ticket describing what is blocked, what was tried, what evidence is needed, and the smallest concrete question or action for Jason.
- Verification happens before integration.
- Integration happens in dependency order.
- Each coding agent must report changed files, tests run, unknowns resolved or remaining, and live QA evidence when applicable.

## Required Checks

- Every ticket runs `git diff --check`.
- Every ticket runs `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa` after P2S-009, P2S-013, P2S-017, P2S-021, P2S-023, and P2S-025.

## Wave Order

- Wave A: P2S-001.
- Wave B: P2S-002, P2S-003, P2S-006, then P2S-004 and P2S-005.
- Wave C: P2S-007 through P2S-012.
- Wave D: P2S-013 through P2S-018.
- Wave E: P2S-019 through P2S-025.
- Wave F: P2S-026 and P2S-027.
