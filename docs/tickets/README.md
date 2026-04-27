# Prometheus Ticket Board

This directory is the permanent file-based board for Prometheus work. It is not tied to a single sprint.

Move ticket files with `git mv` so status changes are traceable in git history. Use `scripts/tickets.sh` for common moves and worktree cleanup when helpful.

## Status Flow

```text
todo -> ready -> in-progress -> verify -> integration -> done
                         \-> blocked
                         \-> deferred
```

## Status Meaning

- `todo/` contains scoped work that is not dependency-ready.
- `ready/` contains work whose dependencies are complete enough to assign.
- `in-progress/` contains work owned by an active worker.
- `verify/` contains work a worker says is complete, pending orchestrator review.
- `integration/` contains verified work waiting to be merged or reconciled.
- `done/` contains integrated work with required checks complete, unless the ticket is documentation-only.
- `blocked/` contains work that cannot proceed without evidence, environment access, or a decision.
- `deferred/` contains real work that should be revisited later because the timing, fixture, or product shape is not ready.

## Ticket Format

Use one Markdown file per ticket. Keep the filename stable after creation except for status-directory moves.

```markdown
---
ticket: TKT-000
status: todo
agent_level: Low
requires_qa: false
doc_only: false
dependencies: []
write_scope:
   - docs/**
---

# TKT-000: Short Imperative Title

## Goal

State the user-facing or engineering outcome.

## Requirements

- Keep requirements testable and scoped.

## Verification

- List required checks and evidence.

## Notes

- Capture unknowns, decisions, and links to prior evidence.
```

## Orchestrator Rules

- Use this board as the source of truth for active ticket state.
- Assign one ticket per worker and give each worker an explicit write scope, dependencies, and verification requirements.
- Workers should use their own worktree named with the ticket number, for example `/Users/jasonkleinberg/repos/Timberborn-Prometheus-TKT-001`.
- Prefer GPT-5.5 Low workers for Low tickets and Medium or High workers for tickets marked Medium or High.
- Do not overlap worker write scopes unless unavoidable.
- Require each worker to report changed files, commit SHA, checks run, unresolved unknowns, and QA or log evidence when applicable.
- Review the diff before integration.
- Integrate accepted work in dependency order.
- Remove worktrees after their work is merged into `main`.
- Keep `docs/HANDOFF.md`, `docs/DESIGN.md`, and `docs/TEST_PLAN.md` current when verified behavior, durable design, or validation state changes.

## Verification Rules

- For code, content, script, or behavior tickets, run `git diff --check` and `bash scripts/test.sh` before integration.
- Run `bash scripts/build.sh --qa` when a ticket changes runtime behavior, live-game integration, deploy behavior, or explicitly requires QA.
- Documentation-only tickets do not need runtime verification steps. If `doc_only: true` and the diff only changes documentation, skip `bash scripts/test.sh`, `bash scripts/build.sh`, and `bash scripts/build.sh --qa`.
- For documentation-only tickets, still verify that claims point to the right source of truth. Run `git diff --check` when practical.

## Blocked And Deferred Work

- Move blocked tickets to `blocked/` with a short note describing what was tried, what evidence is missing, and the smallest concrete question or action needed.
- Move deferred tickets to `deferred/` when the work is valid but should wait for a future milestone, fixture, or clearer product decision.
- Do not keep discarded prototypes as hidden dependencies. Recreate deferred implementation work from current source and archived discovery notes.
