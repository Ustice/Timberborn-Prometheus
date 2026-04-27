# Prometheus Orchestration Runbook

Use this runbook when starting a new multi-agent sprint or continuing ticket-board orchestration.

## Quick Kickoff Prompt

```text
Start a Prometheus orchestration run in /Users/jasonkleinberg/repos/Timberborn-Prometheus.

Read AGENTS.md, docs/INDEX.md, docs/HANDOFF.md, docs/tickets/README.md, docs/tickets/WORKER_INSTRUCTIONS.md, and active ticket files under docs/tickets/.

Use docs/tickets as the source of truth. Clear merged worktrees, respect the build/QA lock, then assign the next dependency-ready ticket. Keep the run boring, traceable, and evidence-first.
```

## Full Kickoff Prompt

```text
Start a Prometheus orchestration run in /Users/jasonkleinberg/repos/Timberborn-Prometheus.

Read:
1. AGENTS.md
2. docs/INDEX.md
3. docs/HANDOFF.md
4. docs/tickets/README.md
5. docs/tickets/WORKER_INSTRUCTIONS.md
6. Active ticket files under docs/tickets/

Use docs/tickets as the source of truth. Move tickets with git mv through:
todo -> ready -> in-progress -> verify -> integration -> done
or blocked/deferred when evidence, timing, or decisions are missing.

Before starting new work:
- Check git status and current branch.
- Check for stale worktrees and remove only merged worktrees.
- Check for an active build/QA lock using bash scripts/build.sh --help and the lock directory noted there.
- Do not bypass scripts/build.sh for build/deploy/launch/QA.

Assign tickets in dependency order:
- One ticket per worker.
- Each worker gets one worktree named with the ticket number.
- Give each worker explicit write scope, dependencies, and required verification.
- Do not overlap write scopes unless unavoidable.
- Require changed files, commit SHA, checks run, unresolved unknowns, and QA/log evidence when applicable.

Verification:
- Documentation-only tickets do not need runtime verification.
- Code/content/script/behavior tickets require git diff --check and bash scripts/test.sh.
- Runtime/live-game tickets require bash scripts/build.sh --qa.
- After --qa, keep the QA lock until evidence capture is complete, then release with bash scripts/build.sh --release-qa-lock.

Integration:
- Review diffs before merging.
- Merge accepted work in dependency order.
- Rerun required checks after integration.
- Move tickets to done only after integration.
- Tear down merged worktrees.
- Keep README.md, docs/HANDOFF.md, docs/DESIGN.md, and docs/TEST_PLAN.md current when status, verified behavior, design, or validation state changes.

Keep the run boring, traceable, and evidence-first.
```

## Startup Checklist

- [ ] Read `AGENTS.md`.
- [ ] Read [INDEX.md](INDEX.md).
- [ ] Read [HANDOFF.md](HANDOFF.md).
- [ ] Read [tickets/README.md](tickets/README.md).
- [ ] Read [tickets/WORKER_INSTRUCTIONS.md](tickets/WORKER_INSTRUCTIONS.md).
- [ ] Review active ticket files under `docs/tickets/`.
- [ ] Run `git status --short` and confirm the branch.
- [ ] Run `git worktree list` and remove only merged worktrees.
- [ ] Check the build/QA lock path from `bash scripts/build.sh --help`.

## Run Loop

1. Move dependency-ready tickets to `ready/`.
2. Assign one ticket per worker with explicit write scope, dependencies, and verification.
3. Move assigned tickets to `in-progress/`.
4. Wait for worker reports with changed files, commit SHA, checks run, unresolved unknowns, and QA/log evidence when applicable.
5. Move completed worker tickets to `verify/`.
6. Review diffs and required evidence.
7. Move accepted tickets to `integration/`.
8. Merge in dependency order and rerun required checks.
9. Move integrated tickets to `done/`.
10. Tear down merged worktrees.
11. Update `README.md`, [HANDOFF.md](HANDOFF.md), [DESIGN.md](DESIGN.md), and [TEST_PLAN.md](TEST_PLAN.md) when status, verified behavior, design, or validation state changes.

## Lock Rules

- Use `scripts/build.sh` for build, deploy, launch, and QA flows.
- Do not deploy, clear logs, stop Timberborn, or launch QA outside the shared lock.
- If `scripts/build.sh` reports another lock owner, wait.
- `bash scripts/build.sh --qa` keeps the QA lock after launch so evidence capture is protected.
- Release the QA lock with `bash scripts/build.sh --release-qa-lock` after QA is complete.
- Set `PROMETHEUS_BUILD_LOCK=0` only for isolated local debugging, never for orchestrated worker validation.

## Documentation-Only Tickets

Documentation-only tickets do not require runtime verification when the diff only changes documentation and does not claim new runtime behavior.

For documentation-only changes:

- Run `git diff --check` when practical.
- Verify links and source-of-truth references.
- Skip `bash scripts/test.sh`, `bash scripts/build.sh`, and `bash scripts/build.sh --qa`.
