# Prometheus Documentation Index

Read these in order when starting work.

| Need | Read | Why |
| --- | --- | --- |
| Repo instructions | [../AGENTS.md](../AGENTS.md) | Collaboration rules, markdown style, and QA preference |
| Quick commands | [../README.md](../README.md) | Fast build/test/deploy entrypoint |
| Current state | [HANDOFF.md](HANDOFF.md) | Latest verified result, blockers, and next action |
| Validation runbook | [TEST_PLAN.md](TEST_PLAN.md) | Smoke checks, QA matrices, and evidence templates |
| Product/design decisions | [DESIGN.md](DESIGN.md) | Durable mechanics, roadmap, and architecture choices |
| Architecture map | [ARCHITECTURE.md](ARCHITECTURE.md) | Durable module ownership, data flow, reset boundaries, and Phase 3 renewal boundaries |
| Ticket board | [tickets/README.md](tickets/README.md) | Permanent file-based ticket board for multi-agent execution |
| Stabilization sprint archive | [STABILIZATION_SPRINT.md](STABILIZATION_SPRINT.md) | Closed Phase 2 to Phase 3 architecture-risk sprint plan |
| Stable repo conventions | [PROJECT_MEMORY.md](PROJECT_MEMORY.md) | Long-lived boundaries and source-of-truth pointers |

## Source Of Truth Shortcuts

| Question | Source |
| --- | --- |
| What do I run first? | [../README.md](../README.md) and `bash scripts/build.sh --help` |
| What is currently verified? | [HANDOFF.md](HANDOFF.md) |
| What is next? | [HANDOFF.md](HANDOFF.md) |
| Where is the validation matrix? | [TEST_PLAN.md](TEST_PLAN.md) |
| Where is the active ticket board? | [tickets/README.md](tickets/README.md) |
| Where is the stabilization plan? | [STABILIZATION_SPRINT.md](STABILIZATION_SPRINT.md) |
| Where is the archived stabilization ticket board? | [ARCHIVE/stabilization-sprint-2026-04/README.md](ARCHIVE/stabilization-sprint-2026-04/README.md) |
| Where is the architecture map? | [ARCHITECTURE.md](ARCHITECTURE.md) |
| Where do design decisions live? | [DESIGN.md](DESIGN.md) |
| Where is history? | [ARCHIVE/](ARCHIVE/) |

## Documentation Ownership

- `README.md` stays compact and points here.
- `HANDOFF.md` is the only live status document.
- `TEST_PLAN.md` is the only active validation runbook.
- `DESIGN.md` is the durable product and architecture record.
- `ARCHITECTURE.md` is the durable module boundary and data-flow map for stabilization work.
- `tickets/` is the active file-based ticket board.
- `STABILIZATION_SPRINT.md` is the closed Phase 2 architecture-risk burn-down plan.
- `stabilization/` is a compatibility pointer to archived sprint process notes.
- `ARCHIVE/` keeps long chronological history out of startup docs.
