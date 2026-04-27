# Ticket Worker Instructions

Use these instructions for every Prometheus ticket worker unless the ticket says otherwise.

## Worktree

- Work in your assigned ticket worktree only.
- Use a worktree name that includes the ticket number, for example `/Users/jasonkleinberg/repos/Timberborn-Prometheus-TKT-001`.
- Do not touch ticket board status files unless the orchestrator explicitly assigns that as part of your scope.
- You are not alone in the codebase. Do not revert edits made by others, and adjust your implementation to accommodate already-integrated changes.

## Inputs

- Read `AGENTS.md`.
- Read `docs/INDEX.md`.
- Read `docs/tickets/README.md`.
- Read your assigned ticket file.
- Read `docs/HANDOFF.md`, `docs/DESIGN.md`, and `docs/TEST_PLAN.md` when your ticket changes verified behavior, architecture, or validation state.
- Read archived sprint notes only when the ticket points to them.

## Scope

- Stay inside the ticket's explicit write scope.
- Do not overlap another worker's write scope unless the orchestrator approves it.
- Keep behavior unchanged for split/refactor tickets unless the ticket explicitly asks for behavior changes.
- Prefer small reconciliation edits over broad refactors when compile or integration requires support outside the nominal scope.

## Verification

- Run `git diff --check`.
- Run `bash scripts/test.sh` for code, content, script, or behavior changes.
- Run `bash scripts/build.sh --qa` only when the ticket or board requires live QA.
- Skip runtime verification for documentation-only tickets marked `doc_only: true` when the diff only changes documentation.
- Capture log or QA evidence when behavior, runtime integration, or live-game state changes.
- Follow `AGENTS.md` Markdown style for documentation changes.

## Submission

- Commit your completed ticket work in your assigned worktree after verification passes.
- Keep the commit scoped to the ticket write scope and any approved support edits.
- Do not commit ticket board status moves unless the orchestrator explicitly assigned them to you.
- Report the commit SHA in your final response.

## Final Report

Report:

- Changed files.
- Commit SHA.
- Tests and checks run, with outcomes.
- Unresolved unknowns or blockers.
- QA/log evidence when applicable.
- Short behavior or architecture summary.
