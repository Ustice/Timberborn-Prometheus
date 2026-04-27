# Prometheus Stabilization Notes

The Phase 2 stabilization sprint is closed.

The archived sprint ticket board and backlog live under [../ARCHIVE/stabilization-sprint-2026-04/](../ARCHIVE/stabilization-sprint-2026-04/). Active ticket tracking now lives under [../tickets/README.md](../tickets/README.md).

Use this directory only as a compatibility pointer for older links.

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

- Use [../tickets/README.md](../tickets/README.md) for new work.
- A Medium agent owns wave orchestration.
- Assign disjoint write scopes before agents start.
- Give workers [WORKER_INSTRUCTIONS.md](WORKER_INSTRUCTIONS.md) plus the assigned ticket instead of repeating the full standard handoff in every prompt.
- Low is the default agent level; tickets marked Medium or High should be assigned accordingly.
- Raise blockers early, especially when the blocker is likely something Jason can solve: missing live-game evidence, unclear product intent, Timberborn API uncertainty, local environment issues, screenshots/logs needed, or a choice between gameplay tradeoffs.
- Move blocked tickets to `blocked/` with a short note in the ticket describing what is blocked, what was tried, what evidence is needed, and the smallest concrete question or action for Jason.
- Verification happens before integration.
- Integration happens in dependency order.
- Each coding agent must report changed files, tests run, unknowns resolved or remaining, and live QA evidence when applicable.

## Required Checks

- Every ticket runs `git diff --check`.
- Every ticket runs `bash scripts/test.sh`.
- Run `bash scripts/build.sh --qa` after P2S-009, P2S-013, P2S-017, P2S-021, P2S-023, and P2S-025, then use Computer Use for startup dialogs, menu loading, and in-game evidence.
- Documentation-only ticket updates do not require runtime verification steps.

## Wave Order

- Wave A: P2S-001.
- Wave B: P2S-002, P2S-003, P2S-006, then P2S-004 and P2S-005.
- Wave C: P2S-007 through P2S-012.
- Wave D: P2S-013 through P2S-018.
- Wave E: P2S-019 through P2S-025.
- Wave F: P2S-026 and P2S-027.
